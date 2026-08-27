"""Regression checks for the consolidated named heavy-laser batch."""

from __future__ import annotations

import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))

from miniyaml import Ruleset


ROOT_LASERS = {
    "BlackHandLaser": (96000, 48000, 3),
    "CabalHunterKillerLasers": (16000, 0, 2),
    "CabalHunterKillerLasers_elite": (30000, 0, 3),
    "TSLaser25mmDep": (2000, 2000, 2),
    "edenMobileLaser": (8000, 0, 4),
    "ordos_lasertank": (40000, 0, 4),
}
RESOLVED_LASERS = tuple(ROOT_LASERS) + (
    "edenMobileLaserTiger",
    "edenMobileDefenceLaser",
)
RETIRED_FLAT_TAGS = {
    "TankDestroyerCannon",
    "Chaingun",
    "FlakWeapon",
    "MediumMissile",
    "HeavyMissile",
    "LaserWeapon",
    "RailgunWeapon",
    "LaserExtraDamage",
    "RailgunExtraDamage",
}


def child(node, key):
    return next((item for item in node.children if item.key == key), None)


class LaserBulkProfileTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = Ruleset(ROOT)

    def test_roots_use_one_heavy_laser_destination(self):
        for name, (damage, ground_remainder, percentage_count) in ROOT_LASERS.items():
            weapon = self.rules.resolve_weapon(name)
            self.assertIsNotNone(weapon, name)

            laser = child(weapon, "Warhead@Laser_Heavy")
            self.assertIsNotNone(laser, name)
            self.assertEqual("AreaDamage", laser.value, name)
            self.assertEqual(str(damage), child(laser, "Damage").value, name)
            self.assertEqual("0", child(laser, "PercentageScale").value, name)
            self.assertEqual("64", child(laser, "Spread").value, name)
            self.assertEqual("100, 0", child(laser, "Falloff").value, name)
            self.assertEqual("Temperature", child(laser, "PhysicalStateName").value, name)
            self.assertEqual("75", child(laser, "PhysicalStateScale").value, name)

            remainder = child(weapon, "Warhead@LaserHeavyGroundRemainder")
            if ground_remainder:
                self.assertIsNotNone(remainder, name)
                self.assertEqual(str(ground_remainder), child(remainder, "Damage").value, name)
                self.assertEqual("Ground, Water", child(remainder, "ValidTargets").value, name)
            else:
                self.assertIsNone(remainder, name)

            percentages = [
                item for item in weapon.children
                if item.key.startswith("Warhead@") and item.value == "AreaDamagePercentage"
            ]
            self.assertEqual(percentage_count, len(percentages), name)

    def test_retired_flat_keys_do_not_survive_resolution(self):
        for name in RESOLVED_LASERS:
            weapon = self.rules.resolve_weapon(name)
            tags = {
                item.key.split("@", 1)[1]
                for item in weapon.children
                if item.key.startswith("Warhead@")
                and item.value in {"AreaDamage", "SpreadDamage", "TargetDamage"}
            }
            self.assertFalse(tags & RETIRED_FLAT_TAGS, f"{name}: {tags & RETIRED_FLAT_TAGS}")

    def test_shield_chips_are_preserved_under_compatibility_names(self):
        for name in RESOLVED_LASERS:
            weapon = self.rules.resolve_weapon(name)
            laser_chip = child(weapon, "Warhead@LegacyLaserExtraDamage")
            self.assertIsNotNone(laser_chip, name)
            self.assertEqual("600", child(laser_chip, "Damage").value, name)
            self.assertEqual("false", child(laser_chip, "UpdatesUnitStatistics").value, name)

        for name in ("CabalHunterKillerLasers_elite", "ordos_lasertank"):
            weapon = self.rules.resolve_weapon(name)
            rail_chip = child(weapon, "Warhead@LegacyRailgunExtraDamage")
            self.assertIsNotNone(rail_chip, name)
            self.assertEqual("1000", child(rail_chip, "Damage").value, name)


if __name__ == "__main__":
    unittest.main()
