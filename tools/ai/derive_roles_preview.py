#!/usr/bin/env python3
"""Preview AI_ARCHITECTURE §2.8 (ruled: roles on actors): can a trait-derived role reproduce a
central ai.yaml actor LIST? Nothing is changed; this only measures.

For each (module field, derivation rule) pair it compares the list as written in
mods/cameo/ai/ai.yaml with the set of resolved actors the rule selects:
  both      - in the list and derived          (the derivation reproduces these)
  list-only - written by hand but not derived   (the rule misses them: widen it, or tag BotRoles)
  derived-only - derived but not in the list    (the rule over-selects, or the list forgot them)

Usage: python tools/ai/derive_roles_preview.py [--show 8]
"""
from __future__ import annotations

import argparse
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / "tools" / "audit"))
import miniyaml  # noqa: E402

# (trait instance prefix, field)  ->  rule: all traits in `has` present, none in `not`, producible (Buildable with a Queue)
RULES = [
    (("HarvesterBotModuleCA", "HarvesterTypes"), {"has": ["Harvester"]}),
    (("ResourceMapBotModule", "HarvesterTypes"), {"has": ["Harvester"]}),
    (("HarvesterBotModuleCA", "RefineryTypes"), {"has": ["Refinery"]}),
    (("BaseBuilderBotModuleCA", "RefineryTypes"), {"has": ["Refinery"]}),
    (("ResourceMapBotModule", "RefineryTypes"), {"has": ["Refinery"]}),
    (("BaseBuilderBotModuleCA", "PowerTypes"), {"has": ["Power"], "building": True}),
    (("McvExpansionManagerBotModule", "McvTypes"), {"has": ["Transforms"], "not": ["Building"]}),
    (("BaseBuilderBotModuleCA", "ConstructionYardTypes"), {"has": ["BaseBuilding"], "building": True}),
    (("SquadManagerBotModuleCA", "AirUnitsTypes"), {"has": ["Aircraft"], "not": ["Building"]}),
    (("SquadManagerBotModuleCA", "NavalUnitsTypes"), {"naval": True}),
    (("BaseBuilderBotModuleCA", "DefenseTypes"), {"has": ["Armament"], "building": True}),
]


def trait_types(node) -> set[str]:
    return {c.key.split("@", 1)[0] for c in node.children if not c.key.startswith("-")}


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--show", type=int, default=6)
    a = ap.parse_args()
    rs = miniyaml.Ruleset(REPO)
    real = [k for k in rs.actors if not k.startswith("^")]
    resolved = {}
    for k in real:
        try:
            n = rs.resolve(k)
        except Exception:
            continue
        if n is not None:
            resolved[k] = n
    types = {k: trait_types(n) for k, n in resolved.items()}

    ai = miniyaml.load(REPO / "mods" / "cameo" / "ai" / "ai.yaml")
    player = next(n for n in ai if n.key == "Player")

    for (prefix, field), rule in RULES:
        written: set[str] = set()
        for t in player.children:
            if t.key.split("@", 1)[0] == prefix:
                v = t.get(field)
                if v:
                    written |= {x.strip() for x in v.split(",") if x.strip()}
        if not written:
            print(f"{prefix}.{field}: not set centrally")
            continue

        def selected(k):
            ts = types[k]
            # producible = Buildable WITH a Queue (spawned slaves carry a queueless Buildable), as BotRoleSets.cs
            if "Buildable" not in ts or not (resolved[k].get("Buildable", "Queue") or "").strip():
                return False
            if rule.get("building") and "Building" not in ts:
                return False
            if any(h not in ts for h in rule.get("has", [])):
                return False
            if any(n in ts for n in rule.get("not", [])):
                return False
            if rule.get("naval"):
                loc = (resolved[k].get("Mobile", "Locomotor") or "").lower()
                return "naval" in loc or "water" in loc or "ship" in loc
            return True

        derived = {k for k in types if selected(k)}
        both, lo, do = written & derived, sorted(written - derived), sorted(derived - written)
        pct = 100 * len(both) / len(written)
        print(f"{prefix}.{field}: written {len(written)}, derived {len(derived)}, "
              f"reproduced {len(both)} ({pct:.0f}%), list-only {len(lo)}, derived-only {len(do)}")
        if lo:
            print(f"    list-only e.g. {', '.join(lo[:a.show])}")
        if do:
            print(f"    derived-only e.g. {', '.join(do[:a.show])}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
