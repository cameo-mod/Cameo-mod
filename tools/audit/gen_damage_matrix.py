#!/usr/bin/env python3
"""gen_damage_matrix.py — §8.1: damage-vs-armor matrix generated from warheads.

Collects every armor type referenced by Armor.Type across live actors and
every SpreadDamage-family warhead's Versus table; emits the aggregate view
(count of warheads and mean effectiveness per armor type) plus the full
per-warhead table for docs/design/damage_model.md consumption.
"""

from __future__ import annotations

import sys
from collections import defaultdict

from cameo_model import Model
from report import h1, h2, table


def main() -> int:
    m = Model()
    rs = m.rs

    armor_types: set[str] = set()
    for name in rs.actors:
        if name.startswith("^"):
            continue
        res = rs.resolve(name)
        if res is None:
            continue
        for a in res.children_named("Armor"):
            t = a.get("Type")
            if t:
                armor_types.add(t)

    # per-warhead Versus rows
    per_armor: dict[str, list[int]] = defaultdict(list)
    warhead_count = 0
    for wname in sorted(rs.weapons):
        w = rs.resolve_weapon(wname)
        if w is None:
            continue
        for c in w.children:
            if not c.key.startswith("Warhead"):
                continue
            versus = c.child("Versus")
            if versus is None:
                continue
            warhead_count += 1
            for v in versus.children:
                try:
                    per_armor[v.key].append(int(v.value))
                except ValueError:
                    pass

    print(h1("gen_damage_matrix — armor classes & Versus aggregates (§8.1)"))
    print(f"Armor types in live actors: **{len(armor_types)}**, "
          f"warheads with Versus tables: **{warhead_count}**\n")
    print(h2("Armor types referenced by actors"))
    print(", ".join(sorted(armor_types)))
    print()
    print(h2("Versus aggregate per armor type (across all warheads)"))
    rows = []
    for armor in sorted(set(per_armor) | armor_types):
        vals = per_armor.get(armor, [])
        if vals:
            rows.append([armor, str(len(vals)),
                         f"{sum(vals)/len(vals):.0f}%",
                         str(min(vals)), str(max(vals))])
        else:
            rows.append([armor, "0", "—", "—", "—"])
    print(table(["armor type", "#warheads naming it", "mean Versus",
                 "min", "max"], rows))
    print("\n_Armor types with 0 warhead references are either default-100% "
          "targets everywhere or orphaned armor classes — cross-check with "
          "audit_orphans. Full per-warhead dump: run with --full._\n")

    if "--full" in sys.argv:
        print(h2("Full per-warhead Versus table"))
        for wname in sorted(rs.weapons):
            w = rs.resolve_weapon(wname)
            if w is None:
                continue
            for c in w.children:
                versus = c.child("Versus") if c.key.startswith("Warhead") else None
                if versus is None or not versus.children:
                    continue
                cells = ", ".join(f"{v.key}={v.value}" for v in versus.children)
                print(f"- **{wname}** {c.key}: {cells}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
