#!/usr/bin/env python3
"""audit_ai.py — B5 detector (AI wiring drift).

Checks mods/cameo/ai/ai.yaml:
  A1 every actor ID referenced in build/unit lists exists in the ruleset (BLOCKING)
  A2 every buildable combat unit of a Random-pool faction appears in at least
     one AI list (warning — the Forgotten-helipad class of bug)
  A3 IDs in exclusion lists (ExcludedUnitTypes/ExcludeFromSquadsTypes) exist
"""

from __future__ import annotations

import sys

from cameo_model import Model
from miniyaml import Node, load as load_yaml
from report import h1, h2, table

LIST_KEYS = {"UnitsToBuild", "UnitLimits", "BuildingFractions", "BuildingLimits",
             "UnitsCommonNames", "BuildingCommonNames"}
CSV_KEYS = {"ExcludedUnitTypes", "ExcludeFromSquadsTypes", "ConstructionYardTypes",
            "VehiclesQueues", "InfantryQueues", "NavyUnitsTypes", "AirUnitsTypes",
            "ProtectionTypes", "SiegeTypes", "McvTypes", "ExcludeFromAirStrikeTypes"}


def walk(node: Node):
    yield node
    for c in node.children:
        yield from walk(c)


def unloaded_actor_ids(m: Model) -> set[str]:
    """Actor ids defined in yaml files on disk but NOT in the live manifest —
    lets us label AI refs as 'unloaded content' vs 'defined nowhere'."""
    live = {p.resolve() for p in m.rs.manifest.rules}
    ids: set[str] = set()
    for base in (m.root / "mods/cameo/rules", m.root / "mods/cameo/ContentPacks"):
        for p in base.rglob("*.yaml"):
            if p.resolve() in live or p.name in ("content.yaml", "mod.yaml"):
                continue
            try:
                for top in load_yaml(p):
                    if top.key and not top.key.startswith(("^", "-")):
                        ids.add(top.key.lower())
            except Exception:
                continue
    return ids


def main() -> int:
    m = Model()
    rs = m.rs
    ai_path = m.root / "mods/cameo/ai/ai.yaml"
    doc = load_yaml(ai_path)

    unloaded = unloaded_actor_ids(m)
    referenced: dict[str, int] = {}
    missing_rows, unloaded_rows = [], []

    def check(name: str, listname: str, line: int) -> None:
        lname = name.strip().lower()
        if not lname or lname.startswith("#"):
            return
        referenced[lname] = line
        if rs.actor(lname) is not None:
            return
        if lname in unloaded:
            unloaded_rows.append([name, listname, str(line)])
        else:
            missing_rows.append([name, listname, str(line)])

    for top in doc:
        for node in walk(top):
            if node.key in LIST_KEYS:
                for entry in node.children:
                    check(entry.key, node.key, entry.line)
            if node.key in CSV_KEYS and node.value:
                for tok in node.value.split(","):
                    check(tok, node.key, node.line)

    # A2: pool factions' combat buildables absent from every AI list
    unwired_rows = []
    pool = sorted(m.random_pool() | m.tournament_pool() | {"cabal"})
    for fac in pool:
        if fac not in {f.internal for f in m.real_factions()}:
            continue
        roster = m.buildable_roster(fac)
        missing = []
        for lname in sorted(roster):
            res = rs.resolve(lname)
            if res is None or res.child("Health") is None:
                continue
            ut = m.unit_type(lname)
            if ut not in {"inf", "veh", "air", "nav"}:
                continue
            if lname not in referenced:
                missing.append(lname)
        if missing:
            unwired_rows.append([fac, str(len(missing)), ", ".join(missing)])

    print(h1("audit_ai — ai.yaml wiring (B5)"))
    print(f"IDs referenced by ai.yaml: **{len(referenced)}** — "
          f"defined NOWHERE (BLOCKING): **{len(missing_rows)}**, "
          f"defined only in unloaded files (hygiene): **{len(unloaded_rows)}**, "
          f"pool factions with unwired combat units: **{len(unwired_rows)}**\n")
    print(h2("A1 — ai.yaml references defined nowhere (blocking: helipad-bug class)"))
    print(table(["referenced id", "list", "ai.yaml line"], missing_rows))
    print(h2("A2 — combat units the AI never builds (Random/Tournament pool factions)"))
    print(table(["faction", "count", "unwired units"], unwired_rows))
    print(h2("A3 — ai.yaml references to unloaded content (dead sections, hygiene)"))
    print(table(["referenced id", "list", "ai.yaml line"], unloaded_rows))
    return 1 if missing_rows else 0


if __name__ == "__main__":
    sys.exit(main())
