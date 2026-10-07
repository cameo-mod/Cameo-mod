"""Migration invariants: resolve inheritance and reject lost/duplicate stations."""
import unittest
import _bootstrap
from miniyaml import load_text
from multi_turret_targeting import audit, audit_actor, audit_removals

class MultiTurretMigrationTest(unittest.TestCase):
    def test_mounted_rules_cover_examples_and_all_shared_multi_turrets(self):
        result = audit(_bootstrap.REPO_ROOT)
        self.assertEqual(result["errors"], [])
        self.assertIn("japan_oitank", result["migrated"])
        self.assertIn("japan_yamatobattleship", result["migrated"])
        self.assertEqual(result["specialized"], ["terran_siegetank"])

    def test_local_attack_rename_must_not_remove_nonexistent_inherited_trait(self):
        actor = load_text("unit:\n\t-AttackTurreted:\n\tAttackMultiTurreted:\n")[0]
        class Rules:
            def inherits_of(self, actor): return []
        self.assertEqual(audit_removals(actor, Rules()), ["unit: removes absent AttackTurreted"])

    def test_missing_station_and_duplicate_station_are_rejected(self):
        actor = load_text("unit:\n\tTurreted:\n\tAutoTarget:\n\t\tDynamicWeaponPriority: true\n\tAttackMultiTurreted:\n\t\tTurrets: primary,primary\n\tArmament:\n\t\tTurret: rear\n")[0]
        errors = audit_actor(actor)
        self.assertTrue(any("duplicate" in e for e in errors))
        self.assertTrue(any("no independent station" in e for e in errors))

    def test_shared_attack_and_missing_dynamic_flag_are_rejected(self):
        actor = load_text("unit:\n\tTurreted:\n\tTurreted@rear:\n\t\tTurret: rear\n\tAutoTarget:\n\tAttackTurreted:\n")[0]
        errors = audit_actor(actor)
        self.assertTrue(any("dynamic" in e for e in errors))
        self.assertTrue(any("shared targeting" in e for e in errors))

if __name__ == "__main__":
    unittest.main()
