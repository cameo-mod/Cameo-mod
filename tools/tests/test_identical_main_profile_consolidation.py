import pathlib
import sys
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))

from audit_three_way_split import main_warhead_nodes, main_warheads
from consolidate_identical_main_profiles import (
    LOCKDOWN_PINS,
    SELECTED,
    remaining_groups,
)
from miniyaml import Ruleset


class IdenticalMainProfileConsolidationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = Ruleset(ROOT)

    def test_selected_identical_groups_are_fully_consolidated(self):
        self.assertEqual([], remaining_groups(self.rules))

    def test_unsafe_percentage_and_state_cases_remain_separate(self):
        freedom_expected = {
            "RA2FreedomRocket": {
                "MissileAP_MediumFlatCompatibility": ("120000", "0"),
                "MissileAP_Medium": ("60000", "10000"),
            },
            "RA2FreedomRocket_elite": {
                "MissileAP_MediumFlatCompatibility": ("240000", "0"),
                "MissileAP_Medium": ("120000", "10000"),
            },
        }
        for name, expected in freedom_expected.items():
            nodes = {
                node.key.removeprefix("Warhead@"): node
                for node in main_warhead_nodes(self.rules.resolve_weapon(name))
            }
            self.assertEqual(set(expected), set(nodes))
            for key, (damage, scale) in expected.items():
                self.assertEqual(damage, nodes[key].get("Damage"))
                self.assertEqual(scale, nodes[key].get("PercentageScale"))

        fireball_nodes = {
            node.key.removeprefix("Warhead@"): node
            for node in main_warhead_nodes(
                self.rules.resolve_weapon("SyndicateFireballLauncherExplode"))
        }
        preserved = {
            "PreservedFlat_HeavyFlameWeapon",
            "PreservedFlat_LightFlameWeapon",
            "PreservedFlat_MediumFlameWeapon",
        }
        self.assertTrue(preserved <= set(fireball_nodes))
        for key in preserved:
            node = fireball_nodes[key]
            self.assertEqual("4000", node.get("Damage"))
            self.assertEqual("Temperature", node.get("PhysicalStateName"))
            self.assertEqual("100", node.get("PhysicalStateScale"))

    def test_lockdown_descendants_keep_their_original_routes(self):
        for name, (flak_damage, chaingun_damage) in LOCKDOWN_PINS.items():
            resolved = self.rules.resolve_weapon(name)
            flak = resolved.child("Warhead@SniperFlak")
            chaingun = resolved.child("Warhead@SniperChaingun")
            self.assertEqual("AreaDamage", flak.value)
            self.assertEqual(str(flak_damage), flak.get("Damage"))
            self.assertEqual("Ground, Water, Air", flak.get("ValidTargets"))
            self.assertEqual("Enemy, Neutral", flak.get("ValidRelationships"))
            self.assertEqual(str(chaingun_damage), chaingun.get("Damage"))

    def test_selected_sniper_profiles_remain_point_like(self):
        for name in SELECTED:
            if "Sniper" not in name:
                continue
            for node in self.rules.resolve_weapon(name).children:
                if not node.key.startswith("Warhead@"):
                    continue
                if node.value not in {"AreaDamage", "SpreadDamage"}:
                    continue
                if "Sniper" not in node.key and "Bullet" not in node.key:
                    continue
                self.assertEqual("1", node.get("Spread"), (name, node.key))
                self.assertEqual("100, 0", node.get("Falloff"), (name, node.key))


if __name__ == "__main__":
    unittest.main()
