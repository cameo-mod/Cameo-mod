"""Regression checks for the first under-100 physical-weapon checkpoint."""

from __future__ import annotations

import json
import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))

import classify_remaining_weapons as classifier
from miniyaml import Ruleset


ROOTS = {
    "120mm_td", "25mm", "AsianLynxTankCannon", "CabalArtilleryWalkerShell",
    "DiabloCannon", "FutureMechGatling", "LightTank2Cannon",
    "MortarTeamArtilleryShell", "RA2APCFlakCannon", "RA2LasherCannon",
    "ReconRangerRecoillessGun", "SteelCruiserCannons", "SteelTwisterMissiles",
    "TS70mmTurChem", "TSScoopDualTurChem", "WhiteRabbitGatling", "ra120mm2",
}

HELPERS = {
    "^Compatibility_Bullet_MediumFlat",
    "^Compatibility_CannonHE_HeavyFlat",
    "^Compatibility_CannonHE_MediumFlat",
    "^Compatibility_Flak_MediumFlat",
    "^Compatibility_MissileAP_HeavyFlat",
    "^Compatibility_MissileAP_MediumFlat",
}

ALLOWED_COMPARATOR_FINDINGS = {
    "blast_shape",
    "invalid_target_damage",
    "physical_state_bindings",
    "relationship_stat_damage",
}


def child(node, key):
    return next((item for item in node.children if item.key == key), None)


class Under100PhysicalProfileTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = Ruleset(ROOT)

    def test_selected_roots_are_retired_and_checkpoint_count_is_exact(self):
        remaining = classifier.classify(self.rules)
        names = {row["weapon"] for row in remaining}
        self.assertFalse(names & ROOTS, names & ROOTS)
        self.assertEqual(180, len(remaining))

    def test_flat_compatibility_helpers_cannot_add_percentage_damage(self):
        for name in HELPERS:
            helper = self.rules.resolve_weapon(name)
            self.assertIsNotNone(helper, name)
            mains = [node for node in helper.children
                     if node.key.startswith("Warhead@") and node.value == "AreaDamage"]
            self.assertEqual(1, len(mains), name)
            self.assertEqual("0", child(mains[0], "PercentageScale").value, name)

    def test_baseline_comparison_contains_only_accepted_standardization(self):
        path = ROOT / "docs" / "audit" / "latest" / "under100_checkpoint1_diff.json"
        report = json.loads(path.read_text(encoding="utf-8"))
        self.assertEqual([], report["added"])
        self.assertEqual([], report["removed"])
        self.assertTrue(report["changed"])
        for weapon, findings in report["changed"].items():
            kinds = {finding[0] for finding in findings}
            self.assertLessEqual(kinds, ALLOWED_COMPARATOR_FINDINGS,
                                 f"{weapon}: {sorted(kinds)}")


if __name__ == "__main__":
    unittest.main()
