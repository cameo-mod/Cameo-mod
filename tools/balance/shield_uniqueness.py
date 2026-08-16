#!/usr/bin/env python3
"""Global Shield uniqueness pass — the SECOND phase of Shield generation.

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

Two rules, applied in order:

1. **Within a family, Shield ascends** `Trace < Light < Medium < Heavy < Super` — the old
   "heavier hits shields harder" intuition, preserved inside every family.
2. **Across families, ties break by that same level order**, Super winning and Light losing.

Only the FIRST `Shield:` row under each block header is touched: the `_Percentage` twin and
the `_ExtraDamage` chip carry their own Shield rows and are deliberately left alone.
"""
from __future__ import annotations

import re

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
    lines = text.split("\n")
    found = find_main_shields(lines)
    if not found:
        return text
    final = assign(found, lo, hi)
    for i, fam, lv, _ in found:
        indent = lines[i][: len(lines[i]) - len(lines[i].lstrip())]
        lines[i] = f"{indent}Shield: {final[(fam, lv)]}"
    return "\n".join(lines)
