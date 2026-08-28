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
    "CabalCommandoPlasma", "CabalCommandoPlasmaMk2", "CabalSubmarinePlasma",
    "HeavyAATankCannontkm", "PlasmaFlamer", "SkyshieldCannon",
    "TSDestroyerMissiles", "TSHoverMissile", "TSSAPCMissiles",
    "ChemicalHonestJohn", "JapanSuperBomb", "MammothTusk2Thermobaric",
    "MammothTuskThermobaric", "MonsterTankTuskThermobaric", "OrcaMissiles",
    "TowerMissile", "TSBikeTibMissile", "TSHellfireTwin",
    "wc2mageBlizzard_Projectile",
    "APTusk", "MammothTusk2", "MissileSoldierWeapon", "Naxis_Komet",
    "Spore_AA", "wc2_tower_arrow",
    "HindMissilesNuclear", "NaxiMeteor", "RA2TOPOLCuba",
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

    def test_selected_roots_are_retired_and_checkpoint_count_does_not_regress(self):
        remaining = classifier.classify(self.rules)
        names = {row["weapon"] for row in remaining}
        self.assertFalse(names & ROOTS, names & ROOTS)
        self.assertLessEqual(len(remaining), 152)

    def test_flat_compatibility_helpers_cannot_add_percentage_damage(self):
        helpers = [name for name in self.rules.weapons if name.startswith("^Compatibility_")]
        self.assertTrue(helpers)
        for name in helpers:
            helper = self.rules.resolve_weapon(name)
            self.assertIsNotNone(helper, name)
            damage_nodes = [node for node in helper.children
                            if node.key.startswith("Warhead@") and "Damage" in str(node.value)]
            self.assertTrue(damage_nodes, name)
            for node in damage_nodes:
                self.assertNotIn("Percentage", str(node.value), f"{name}/{node.key}")
                scale = child(node, "PercentageScale")
                if scale is not None:
                    self.assertEqual("0", scale.value, f"{name}/{node.key}")

    def test_baseline_comparison_contains_only_accepted_standardization(self):
        paths = sorted((ROOT / "docs" / "audit" / "latest").glob(
            "under100_checkpoint*_diff.json"))
        self.assertTrue(paths)
        for path in paths:
            report = json.loads(path.read_text(encoding="utf-8"))
            self.assertEqual([], report["added"], path.name)
            self.assertEqual([], report["removed"], path.name)
            self.assertTrue(report["changed"], path.name)
            for weapon, findings in report["changed"].items():
                kinds = {finding[0] for finding in findings}
                self.assertLessEqual(kinds, ALLOWED_COMPARATOR_FINDINGS,
                                     f"{path.name}/{weapon}: {sorted(kinds)}")


if __name__ == "__main__":
    unittest.main()
