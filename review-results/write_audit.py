from pathlib import Path
root=Path('C:/cameo-wt/codex-multi-turret')
(root/'tools/audit/multi_turret_targeting.py').write_text('''"""Audit resolved turret bindings and dynamic target priority after migration."""
from __future__ import annotations
import argparse
import json
from pathlib import Path
from miniyaml import Ruleset


def audit_actor(actor):
    errors = []
    turrets = {t.get("Turret") or "primary" for t in actor.children_named("Turreted")}
    for auto in actor.children_named("AutoTarget"):
        if auto.get("DynamicWeaponPriority") != "true":
            errors.append(f"{actor.key}: {auto.key} lacks dynamic weapon priority")
    attacks = actor.children_named("AttackMultiTurreted")
    if attacks and actor.children_named("AttackTurreted"):
        errors.append(f"{actor.key}: shared AttackTurreted survives migration")
    for attack in attacks:
        selected = (attack.get("Turrets") or "primary").replace(" ", "").split(",")
        if len(set(selected)) != len(selected) or not set(selected) <= turrets:
            errors.append(f"{actor.key}: duplicate or missing turret station")
        names = (attack.get("Armaments") or "primary,secondary").replace(" ", "").split(",")
        for arm in actor.children_named("Armament"):
            if (arm.get("Name") or "primary") in names and (arm.get("Turret") or "primary") not in selected:
                errors.append(f"{actor.key}: {arm.key} has no independent station")
    if len(turrets) > 1 and actor.children_named("AttackTurreted"):
        errors.append(f"{actor.key}: multi-turret actor still uses shared targeting")
    return errors


def audit(root):
    rules = Ruleset(root)
    errors, migrated, specialized = [], [], []
    for name in sorted(rules.actors):
        if name.startswith("^"):
            continue
        actor = rules.resolve(name)
        errors.extend(audit_actor(actor))
        if actor.children_named("AttackMultiTurreted"):
            migrated.append(name)
        elif len(actor.children_named("Turreted")) > 1:
            # Charged siege tank: mutually exclusive undeployed/deployed turret,
            # not concurrent batteries. Preserve its charge behavior.
            specialized.append(name)
    return dict(errors=errors, migrated=migrated, specialized=specialized)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    result = audit(args.root)
    print(json.dumps(result, indent=2))
    return bool(result["errors"])

if __name__ == "__main__":
    raise SystemExit(main())
''',encoding='utf-8')
(root/'tools/tests/test_multi_turret_targeting.py').write_text('''"""Migration invariants: resolve inheritance and reject lost/duplicate stations."""
import unittest
import _bootstrap
from miniyaml import load_text
from multi_turret_targeting import audit, audit_actor

class MultiTurretMigrationTest(unittest.TestCase):
    def test_mounted_rules_cover_examples_and_all_shared_multi_turrets(self):
        result = audit(_bootstrap.REPO_ROOT)
        self.assertEqual(result["errors"], [])
        self.assertIn("japan_oitank", result["migrated"])
        self.assertIn("japan_yamatobattleship", result["migrated"])
        self.assertEqual(result["specialized"], ["terran_siegetank"])

    def test_missing_station_and_duplicate_station_are_rejected(self):
        actor = load_text("unit:\\n\\tTurreted:\\n\\tAutoTarget:\\n\\t\\tDynamicWeaponPriority: true\\n\\tAttackMultiTurreted:\\n\\t\\tTurrets: primary,primary\\n\\tArmament:\\n\\t\\tTurret: rear\\n")[0]
        errors = audit_actor(actor)
        self.assertTrue(any("duplicate" in e for e in errors))
        self.assertTrue(any("no independent station" in e for e in errors))

    def test_shared_attack_and_missing_dynamic_flag_are_rejected(self):
        actor = load_text("unit:\\n\\tTurreted:\\n\\tTurreted@rear:\\n\\t\\tTurret: rear\\n\\tAutoTarget:\\n\\tAttackTurreted:\\n")[0]
        errors = audit_actor(actor)
        self.assertTrue(any("dynamic" in e for e in errors))
        self.assertTrue(any("shared targeting" in e for e in errors))

if __name__ == "__main__":
    unittest.main()
''',encoding='utf-8')
