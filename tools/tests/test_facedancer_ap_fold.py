"""Exact regression contract for the Facedancer AP compatibility fold."""

from __future__ import annotations

import hashlib
import json
import pathlib
import sys
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
REPORT = ROOT / "docs/audit/latest/facedancer_ap_fold_comparison.json"
sys.path[:0] = [str(ROOT / "tools/audit"), str(ROOT / "tools/balance")]

from audit_three_way_split import main_warhead_nodes, main_warheads  # noqa: E402
from miniyaml import Ruleset  # noqa: E402
from percentage_damage import folded_units  # noqa: E402


class FacedancerApFoldTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = Ruleset(ROOT)
        cls.weapon = cls.rules.resolve_weapon("facedancer_grenade")
        cls.report = json.loads(REPORT.read_text(encoding="utf-8"))

    def test_only_the_identical_ap_slices_are_folded(self):
        self.assertEqual(
            ["MissileAP_HeavyFlatCompatibility", "CannonHE_Heavy"],
            main_warheads(self.weapon),
        )
        nodes = {node.key.split("@", 1)[1]: node
                 for node in main_warhead_nodes(self.weapon)}
        ap = nodes["MissileAP_HeavyFlatCompatibility"]
        self.assertEqual("160000", ap.get("Damage"))
        self.assertEqual("1250", ap.get("PercentageScale"))
        self.assertEqual("20000", nodes["CannonHE_Heavy"].get("Damage"))

    def test_flat_and_percentage_output_are_exact(self):
        self.assertEqual(160000, 140000 + 20000)
        self.assertEqual(
            folded_units(20000, 10000)[1],
            folded_units(160000, 1250)[1],
        )
        self.assertEqual(1000, folded_units(160000, 1250)[1])

    def test_special_payloads_remain_separate(self):
        companions = {
            "LightFlameWeaponPercentage", "MediumChemicalWeaponPercentage",
            "HeavyBombPercentage", "GrenadePercentage",
            "ShrapnelWeaponPercentage", "LightMissilePercentage",
            "MediumMissilePercentage",
        }
        keys = {node.key.split("@", 1)[1] for node in self.weapon.children
                if node.key.startswith("Warhead@")}
        self.assertTrue(companions <= keys)
        self.assertEqual(
            "Temperature",
            self.weapon.child("Warhead@LightFlameWeaponPercentage").get(
                "PhysicalStateName"),
        )
        self.assertEqual(
            "Corrosion",
            self.weapon.child("Warhead@MediumChemicalWeaponPercentage").get(
                "PhysicalStateName"),
        )

    def test_whole_ruleset_comparison_has_no_runtime_damage_delta(self):
        self.assertEqual([], self.report["added"])
        self.assertEqual([], self.report["removed"])
        self.assertEqual({"facedancer_grenade"}, set(self.report["changed"]))
        expected = {
            "armor_profile":
                "dca8dc985ace4f70c47a3d07eff718335545cd203e084ce26dc741ac719f20e1",
            "blast_shape":
                "3cdd05bc9f4544fbe21b910950eabf549ff8c2a081c4ceafba718568665ac026",
        }
        changes = self.report["changed"]["facedancer_grenade"]
        self.assertEqual(set(expected), {change[0] for change in changes})
        for kind, *payload in changes:
            digest = hashlib.sha256(json.dumps(
                payload, sort_keys=True, separators=(",", ":")
            ).encode()).hexdigest()
            self.assertEqual(expected[kind], digest, kind)


if __name__ == "__main__":
    unittest.main()
