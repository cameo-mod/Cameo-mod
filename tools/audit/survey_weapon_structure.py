#!/usr/bin/env python3
"""Inventory stacked-main weapons by how live rules can reach them.

The three sets are deliberately separate:

* ``all_concrete``: every non-template weapon definition in the active ruleset;
* ``direct_actor_armament``: named by an actor Armament's Weapon field;
* ``transitive_weapon_graph``: named anywhere in a resolved actor through a
  weapon-reference field, then closed over the same fields in resolved weapons.

This is a reachability inventory, not deletion authorization.  Definitions outside
the modeled graph are reported as ``unreached`` rather than declared dead.

Usage:
  python tools/audit/survey_weapon_structure.py
  python tools/audit/survey_weapon_structure.py --write
  python tools/audit/survey_weapon_structure.py --check
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys


ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "docs" / "audit" / "latest" / "weapon_structure_inventory.json"
sys.path.insert(0, str(ROOT / "tools" / "audit"))

from audit_three_way_split import main_warheads  # noqa: E402
from miniyaml import Ruleset  # noqa: E402


WEAPON_REF_FIELDS = {
    "Weapon", "Weapons", "TriggeredWeapon", "FallbackWeapon",
    "EmptyWeapon", "Explosion", "CasingWeapon", "ImpactWeapon",
    "TeleportWeapon", "DemolishWeapon", "HelixWeapon", "TriggerWeapon",
    "ThumpDamageWeapon", "DetonationWeapon", "MissileWeapon",
}
WEAPON_REF_MAP_FIELDS = {"MissileWeapons"}


def walk(node):
    for child in node.children:
        yield child
        yield from walk(child)


def weapon_references(node, known: set[str]) -> set[str]:
    refs = set()
    for child in walk(node):
        field = child.key.split("@", 1)[0]
        values = []
        if field in WEAPON_REF_FIELDS and child.value:
            values.append(child.value)
        elif field in WEAPON_REF_MAP_FIELDS:
            if child.value:
                values.append(child.value)
            values.extend(descendant.value for descendant in walk(child)
                          if descendant.value)
        for raw in values:
            for value in str(raw).split(","):
                name = value.strip()
                if name in known:
                    refs.add(name)
    return refs


def direct_armament_references(rules: Ruleset, known: set[str]) -> set[str]:
    refs = set()
    for name in rules.actors:
        if name.startswith("^"):
            continue
        resolved = rules.resolve(name)
        if resolved is None:
            continue
        for armament in resolved.children_named("Armament"):
            weapon = str(armament.get("Weapon") or "").strip()
            if weapon in known:
                refs.add(weapon)
    return refs


def weapon_reference_sets(rules: Ruleset, concrete: set[str]) -> tuple[set[str], set[str]]:
    """Return direct Armament references and the full modeled weapon closure."""
    direct_refs = direct_armament_references(rules, concrete)
    actor_refs = set()
    for name in rules.actors:
        if name.startswith("^"):
            continue
        resolved = rules.resolve(name)
        if resolved is not None:
            actor_refs.update(weapon_references(resolved, concrete))

    graph = {
        name: weapon_references(rules.resolve_weapon(name), concrete)
        for name in concrete
    }
    reachable = set(actor_refs)
    pending = list(actor_refs)
    while pending:
        name = pending.pop()
        for target in graph.get(name, ()):
            if target not in reachable:
                reachable.add(target)
                pending.append(target)
    return direct_refs, reachable


def inventory(rules: Ruleset) -> dict[str, object]:
    concrete = {
        name for name in rules.weapons
        if not name.startswith("^") and rules.resolve_weapon(name) is not None
    }
    violations = {
        name for name in concrete
        if len(main_warheads(rules.resolve_weapon(name))) > 1
    }
    direct_refs, reachable = weapon_reference_sets(rules, concrete)

    direct = violations & direct_refs
    transitive = violations & reachable
    indirect = transitive - direct
    unreached = violations - transitive
    assert violations == direct | indirect | unreached
    assert not (direct & indirect or direct & unreached or indirect & unreached)

    return {
        "predicate": (
            "positive Damage on AreaDamage, SpreadDamage, HealthPercentageDamage, or "
            "TargetDamage; excludes designed companions and friendly-fire twins"
        ),
        "reference_fields": sorted(WEAPON_REF_FIELDS),
        "reference_map_fields": sorted(WEAPON_REF_MAP_FIELDS),
        "counts": {
            "concrete_weapons": len(concrete),
            "stacked_main_all_concrete": len(violations),
            "stacked_main_direct_actor_armament": len(direct),
            "stacked_main_indirect_weapon_graph": len(indirect),
            "stacked_main_transitive_weapon_graph": len(transitive),
            "stacked_main_unreached": len(unreached),
        },
        "sets": {
            "direct_actor_armament": sorted(direct),
            "indirect_weapon_graph": sorted(indirect),
            "unreached": sorted(unreached),
        },
    }


def serialized(data: dict[str, object]) -> str:
    return json.dumps(data, indent=2, sort_keys=True) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser()
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--write", action="store_true")
    mode.add_argument("--check", action="store_true")
    args = parser.parse_args()

    data = inventory(Ruleset(ROOT))
    text = serialized(data)
    if args.write:
        OUT.parent.mkdir(parents=True, exist_ok=True)
        OUT.write_text(text, encoding="utf-8")
        print(f"wrote {OUT.relative_to(ROOT)}")
        return 0
    if args.check:
        if not OUT.exists() or OUT.read_text(encoding="utf-8") != text:
            print(f"FAIL {OUT.relative_to(ROOT)} is stale; run with --write")
            return 1
        print(f"PASS {OUT.relative_to(ROOT)} matches live rules")
        return 0

    print(text, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
