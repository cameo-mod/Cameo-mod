#!/usr/bin/env python3
"""audit_basebuilder_crates.py — verify every real faction has a crate
that grants its base builder (MCV / equivalent) when the player has no base.

Cameo uses `GiveBaseBuilderCrateAction` on the `CRATE` actor in
mods/cameo/rules/misc.yaml. Each faction needs an entry with:
  - `ValidFactions: <internal>`
  - `Units: <mobile-construction-actor>`
  - `NoBaseSelectionShares: > 0` (only offered when base is lost)
"""

from __future__ import annotations

import sys

from cameo_model import Model
from report import h1, h2, table


def main() -> int:
    m = Model()
    rs = m.rs

    crate = rs.resolve("CRATE")
    if crate is None:
        print(h1("audit_basebuilder_crates — CRATE actor missing"))
        print("Could not resolve `CRATE` in the ruleset.\n")
        return 1

    covered: dict[str, dict[str, str]] = {}
    problems: list[list[str]] = []

    for child in crate.children_named("GiveBaseBuilderCrateAction"):
        suffix = child.key.split("@", 1)[1] if "@" in child.key else ""
        valid_factions = (child.get("ValidFactions") or "").strip()
        units = (child.get("Units") or "").strip()
        no_base_shares = (child.get("NoBaseSelectionShares") or "").strip()

        if not valid_factions:
            problems.append([child.key, "missing ValidFactions", "", ""])
            continue
        if not units:
            problems.append([child.key, "missing Units", valid_factions, ""])
            continue

        for faction in [f.strip() for f in valid_factions.split(",") if f.strip()]:
            if faction in covered:
                problems.append([child.key, f"duplicate coverage for faction '{faction}'", valid_factions, units])
            covered[faction] = {
                "suffix": suffix,
                "units": units,
                "no_base_shares": no_base_shares,
            }

            # Verify the unit actor exists.
            for unit in [u.strip() for u in units.split(",") if u.strip()]:
                if rs.resolve(unit) is None:
                    problems.append([child.key, f"unit actor '{unit}' does not exist", faction, units])

    real_factions = {f.internal for f in m.real_factions()}
    missing = sorted(real_factions - covered.keys())

    rows = []
    for faction in sorted(covered.keys()):
        info = covered[faction]
        rows.append([faction, info["suffix"], info["units"], info["no_base_shares"]])

    print(h1("audit_basebuilder_crates — faction MCV crate coverage"))
    print(f"Real factions: **{len(real_factions)}** — covered by crate: **{len(covered)}** — missing: **{len(missing)}**\n")

    print(h2("Covered factions"))
    print(table(["faction", "crate suffix", "granted unit", "NoBaseSelectionShares"], rows))

    if missing:
        print(h2("Missing crate coverage"))
        print(table(["faction"], [[f] for f in missing]))

    if problems:
        print(h2("Problems"))
        print(table(["crate entry", "problem", "faction", "units"], problems))

    print()
    return 1 if missing or problems else 0


if __name__ == "__main__":
    sys.exit(main())
