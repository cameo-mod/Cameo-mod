"""Regression checks for the post-consolidation weapon correctness fixes."""

from __future__ import annotations

import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))

import audit_impact_glow_preservation as glow_audit
from miniyaml import Ruleset, load


def child(node, key):
    return next((item for item in node.children if item.key == key), None)


class WeaponCorrectnessFollowupTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = Ruleset(ROOT)

    def test_steel_runner_base_and_resonance_armaments_are_exclusive(self):
        actor = self.rules.resolve("steelconsortium_steelrunner")
        armaments = [node for node in actor.children if node.key.startswith("Armament@")]
        runner = [node for node in armaments
                  if (node.get("Weapon") or "").startswith("SteelRunnerPistols")]
        self.assertEqual(8, len(runner))

        for armament in runner:
            weapon = armament.get("Weapon")
            condition = armament.get("RequiresCondition") or ""
            if "Resonance" in weapon:
                self.assertIn("steelconsortium_upgrade_resonanceammo", condition)
                self.assertNotIn("!steelconsortium_upgrade_resonanceammo", condition)
            else:
                self.assertIn("!steelconsortium_upgrade_resonanceammo", condition)

    def test_quasar_weapons_have_one_definition_and_keep_merged_values(self):
        path = (ROOT / "mods" / "cameo" / "ContentPacks" / "RedAlert2Mod" /
                "AsianAlliance" / "yaml" / "weapons.yaml")
        keys = [node.key for node in load(path)]
        self.assertEqual(1, keys.count("AsianQuasarBoatAG"))
        self.assertEqual(1, keys.count("AsianQuasarBoat_AA"))

        expected = {
            "AsianQuasarBoatAG": ("7168", "3", "512", "192"),
            "AsianQuasarBoatAG_EMP": ("7168", "3", "512", "192"),
            "AsianQuasarBoat_AA": ("7168", "3", "250", "256"),
            "AsianQuasarBoat_EMP_AA": ("7168", "3", "250", "256"),
        }
        for name, (range_value, burst_delay, inaccuracy, launch_speed) in expected.items():
            weapon = self.rules.resolve_weapon(name)
            projectile = child(weapon, "Projectile")
            self.assertEqual(range_value, weapon.get("Range"), name)
            self.assertEqual(burst_delay, weapon.get("BurstDelays"), name)
            self.assertEqual(inaccuracy, projectile.get("Inaccuracy"), name)
            self.assertEqual(launch_speed, projectile.get("MaximumLaunchSpeed"), name)

    def test_new_effect_roots_are_explicitly_non_emissive(self):
        names = {
            "^Effect_Cryo",
            "^Effect_Demolition_Heavy_D2K_Orni",
            "^Effect_MissileAP_Heavy_D2K_ORocket",
        }
        self.assertTrue(names <= glow_audit.NON_EMISSIVE_EFFECT_ROOTS)
        effects = {
            name for name in self.rules.weapons if name.startswith("^Effect")
        }
        sprite_effects = {
            name for name in effects
            if glow_audit.has_impact_sprite(self.rules.resolve_weapon(name))
        }
        tier_memo = {}
        for name in names:
            resolved = self.rules.resolve_weapon(name)
            self.assertFalse(glow_audit.has_impact_glow(resolved), name)
            self.assertEqual(
                name, glow_audit.sprite_root(self.rules, name, sprite_effects), name)
            self.assertEqual(
                0, glow_audit.tier_path_count(self.rules, name, tier_memo), name)


if __name__ == "__main__":
    unittest.main()
