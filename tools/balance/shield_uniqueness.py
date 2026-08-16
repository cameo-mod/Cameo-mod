#!/usr/bin/env python3
"""Global Shield pass — the SECOND phase of Shield generation: COMPRESS, then UNIQUE.

Maintainer's rule (2026-08-16): *"no weapon should share the same versus value against
shields"*, with a defined DIRECTION — *"if something has the same value always prioritize it
like that light->medium->heavy->super with super dealing the most damage to shields and
light the least"*.

**Why this cannot live in `shield_for()`.** That function is per-family: it sees one family's
finished profile and nothing else, so it structurally cannot detect that a different family
landed on the same number. Uniqueness is a property of the finished SET, so it needs a second
phase over all of it. This module is that phase, kept separate from
`gen_weapon_template.py` so the generator's own logic stays per-family and readable.

With 96 templates in the closed range [100, 400] there are 301 integer slots, so a
collision-free assignment always exists and the range never has to widen.

**Why the COMPRESSION lives here too** (moved 2026-08-16, W25 S1). Phase 1 emits a RAW
Shield in centi-units; mapping the set onto [100, 400] needs the set's own extremes, which
`shield_for` cannot see either. It used to be done per-family with three hand-calibrated
constants (`SHIELD_GEOMEAN` / `SHIELD_ALPHA` / `SHIELD_ANCHOR`), and those were correct for
exactly one profile set: the moment S1 renormalised every profile, the ladder silently
drifted to 110..420 = 3.82x against its stated targets of 100..400 = 4.00x. Deriving the
compression from the data on every run cannot go stale, so the drift hazard is gone rather
than merely documented.

The compression is a POWER LAW about the geometric mean, never a clamp: a clamp moves only
the two extreme cells and deforms the ladder, while `v' = G x (v/G) ** alpha` moves every
value proportionally and preserves the ORDER exactly.

Two rules, applied in order:

1. **Within a family, Shield ascends** `Trace < Light < Medium < Heavy < Super` — the old
   "heavier hits shields harder" intuition, preserved inside every family.
2. **Across families, ties break by that same level order**, Super winning and Light losing.

Only the FIRST `Shield:` row under each block header is touched: the `_Percentage` twin and
the `_ExtraDamage` chip carry their own Shield rows and are deliberately left alone.
"""
from __future__ import annotations

import math
import re
import statistics

LEVEL_RANK = {"Trace": 0, "Light": 1, "Medium": 2, "Heavy": 3, "Super": 4}
HEADER = re.compile(r"^\^Warhead_(\w+?)_(\w+):$")


def find_main_shields(lines: list[str]) -> list[tuple[int, str, str, int]]:
    """(line index, family, level, value) for each block's MAIN-warhead Shield row."""
    out: list[tuple[int, str, str, int]] = []
    cur: tuple[str, str] | None = None
    seen = False
    for i, ln in enumerate(lines):
        m = HEADER.match(ln)
        if m:
            cur, seen = (m.group(1), m.group(2)), False
            continue
        if cur and not seen and ln.strip().startswith("Shield:"):
            try:
                out.append((i, cur[0], cur[1], int(ln.split(":", 1)[1])))
            except ValueError:
                pass
            seen = True
    return out


def compress(raw: list[float], lo: int, hi: int) -> list[float]:
    """Map the raw ladder onto exactly `[lo, hi]`, order intact.

    `alpha = ln(hi/lo) / ln(raw_hi/raw_lo)` hits the target ratio exactly; the anchor then
    slides the floor onto `lo`, so BOTH endpoints land by construction on every run. A raw
    ladder that is already flat (or a single entry) has no ratio to fit — it is placed at
    the floor rather than divided by zero.
    """
    r_lo, r_hi = min(raw), max(raw)
    if len(raw) < 2 or r_hi <= r_lo or r_lo <= 0:
        return [float(lo)] * len(raw)
    g = statistics.geometric_mean(raw)
    alpha = math.log(hi / lo) / math.log(r_hi / r_lo)
    out = [g * (v / g) ** alpha for v in raw]
    anchor = lo / min(out)
    return [v * anchor for v in out]


def assign(found: list[tuple[int, str, str, int]], lo: int, hi: int) -> dict:
    """Collision-free Shield values, preserving order and the level direction."""
    # Sort by value, then by level so equal values resolve light -> ... -> super
    # rather than by whatever order the generator happened to emit them in.
    ordered = sorted(found, key=lambda r: (r[3], LEVEL_RANK.get(r[2], 9), r[1]))
    used: set[int] = set()
    out: dict[tuple[str, str], int] = {}
    for _, fam, lv, val in ordered:
        v = max(lo, min(hi, val))
        while v in used and v < hi:
            v += 1
        while v in used and v > lo:
            v -= 1
        used.add(v)
        out[(fam, lv)] = v

    # Repair any within-family inversion the spreading may have introduced: re-sort each
    # family's own values onto its own levels in ascending level order.
    fams: dict[str, list[tuple[int, str]]] = {}
    for (fam, lv) in out:
        fams.setdefault(fam, []).append((LEVEL_RANK.get(lv, 9), lv))
    for fam, items in fams.items():
        items.sort()
        vals = sorted(out[(fam, lv)] for _, lv in items)
        for (_, lv), v in zip(items, vals):
            out[(fam, lv)] = v
    return out


def apply(text: str, lo: int = 100, hi: int = 400) -> str:
    """Compress every raw Shield onto [lo, hi], then make the values unique.

    ⚠ **This pass is MANDATORY, not an optimisation.** Phase 1 emits centi-units, so a
    block that reached the file without passing through here would ship a Shield of ~4900.
    It therefore refuses to run on text whose main-warhead Shield rows it cannot all read,
    rather than silently converting the ones it understood.
    """
    lines = text.split("\n")
    found = find_main_shields(lines)
    if not found:
        return text
    headers = sum(1 for ln in lines if HEADER.match(ln))
    if len(found) != headers:
        raise ValueError(f"shield_uniqueness: {headers} template blocks but "
                         f"{len(found)} readable Shield rows — refusing to half-convert")
    scaled = compress([float(v) for *_h, v in found], lo, hi)
    found = [(i, fam, lv, int(round(v))) for (i, fam, lv, _), v in zip(found, scaled)]
    final = assign(found, lo, hi)
    for i, fam, lv, _ in found:
        indent = lines[i][: len(lines[i]) - len(lines[i].lstrip())]
        lines[i] = f"{indent}Shield: {final[(fam, lv)]}"
    return "\n".join(lines)
