#!/usr/bin/env python3
"""audit_outliers.py — B9 detector (systemic numeric drift).

For every (trait base, field) pair, collects the numeric leaf values across
all resolved non-template actors and reports robust outliers
(|value - median| / MAD-scale > threshold). This is how the next
"selection boxes 42x too large" class of drift is caught before players do.

Also runs hard-coded sanity screens with known engine bounds:
  Selectable.Bounds / DecorationBounds components must be <= 5120 (5 cells).
"""

from __future__ import annotations

import re
import statistics
import sys
from collections import defaultdict

from cameo_model import Model
from report import h1, h2, table

NUMERIC_FIELDS = {
    "Health": ["HP"],
    "Valued": ["Cost"],
    "Mobile": ["Speed", "TurnSpeed"],
    "Aircraft": ["Speed", "TurnSpeed", "CruiseAltitude"],
    "RevealsShroud": ["Range"],
    "Selectable": ["Bounds", "DecorationBounds"],
    "Armament": [],
    "AttackCharges": ["ChargeLevel"],
    "Power": ["Amount"],
    "ChangesHealth": ["Step", "PercentageStep"],
    "Repairable": ["HpPerStep"],
    "GivesExperience": ["Experience"],
    "Buildable": ["BuildDuration", "BuildLimit"],
}
Z_THRESHOLD = 8.0
_num = re.compile(r"-?\d+")


def cell_value(v: str) -> list[int]:
    """Parse plain ints and WDist 'Nc0' notation; return list of components."""
    out = []
    for part in v.split(","):
        part = part.strip()
        mcell = re.fullmatch(r"(-?\d+)c(\d+)", part)
        if mcell:
            out.append(int(mcell.group(1)) * 1024 + int(mcell.group(2)))
            continue
        if re.fullmatch(r"-?\d+", part):
            out.append(int(part))
    return out


def main() -> int:
    m = Model()
    rs = m.rs
    samples: dict[tuple[str, str], list[tuple[int, str]]] = defaultdict(list)

    for name in rs.actors:
        if name.startswith("^"):
            continue
        res = rs.resolve(name)
        if res is None:
            continue
        for trait in res.children:
            base = trait.key.split("@", 1)[0]
            fields = NUMERIC_FIELDS.get(base)
            if fields is None:
                continue
            for f in fields:
                v = trait.get(f)
                if not v:
                    continue
                for comp in cell_value(v):
                    samples[(base, f)].append((comp, name))

    outlier_rows = []
    for (base, f), vals in sorted(samples.items()):
        nums = [v for v, _ in vals]
        if len(nums) < 20:
            continue
        med = statistics.median(nums)
        mad = statistics.median([abs(x - med) for x in nums]) or 1
        scale = 1.4826 * mad
        flagged = {}
        for v, actor in vals:
            z = abs(v - med) / scale
            if z > Z_THRESHOLD:
                prev = flagged.get(actor)
                if prev is None or z > prev[1]:
                    flagged[actor] = (v, z)
        for actor, (v, z) in sorted(flagged.items(), key=lambda kv: -kv[1][1])[:25]:
            outlier_rows.append([f"{base}.{f}", actor, str(v),
                                 f"{med:g}", f"{z:.1f}"])

    bounds_rows = []
    for name in rs.actors:
        if name.startswith("^"):
            continue
        res = rs.resolve(name)
        if res is None:
            continue
        for trait in res.children_named("Selectable"):
            for f in ("Bounds", "DecorationBounds"):
                v = trait.get(f)
                if not v:
                    continue
                comps = cell_value(v)
                if comps and max(abs(c) for c in comps[:2]) > 5120:
                    bounds_rows.append([name, f, v])

    print(h1("audit_outliers — systemic numeric drift (B9)"))
    print(f"(trait,field) distributions sampled: **{len(samples)}** — "
          f"robust outliers (top 25 per field): **{len(outlier_rows)}**, "
          f"selection bounds > 5120: **{len(bounds_rows)}**\n")
    print(h2("Hard screen — Selectable bounds above the 5x5-cell maximum"))
    print(table(["actor", "field", "value"], bounds_rows))
    print(h2(f"Robust outliers per (trait, field), |z| > {Z_THRESHOLD:g}"))
    print(table(["trait.field", "actor", "value", "median", "robust z"], outlier_rows))
    print("\n_Outliers are leads, not verdicts: epic units are legitimately "
          "extreme. Scan for CLUSTERS of similar z-scores — those are unit "
          "systems using a stale scale convention._\n")
    return 1 if bounds_rows else 0


if __name__ == "__main__":
    sys.exit(main())
