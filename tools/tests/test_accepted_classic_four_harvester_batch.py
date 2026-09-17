"""Focused contract for the accepted classic-four harvester durability batch."""

from __future__ import annotations

import pathlib
import sys
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))

from cameo_model import Model  # noqa: E402


# actor -> (cost, hp, speed, self-healing step, hp per repair step, turn speed, armor)
HARVESTER_DURABILITY = {
    "td_gdi_tiberiumharvester": (1670, 240000, 69, 96, 12000, 14, "Heavy"),
    "td_nod_tiberiumharvester": (1670, 240000, 69, 96, 12000, 14, "Heavy"),
    "td_nod_stealthharvester": (1520, 175000, 77, 70, 8750, 15, "Heavy"),
    "ra1_allies_alliedoretruck": (1560, 210000, 81, 84, 10500, 16, "Medium"),
    "ra1_soviets_oretruck": (1560, 210000, 81, 84, 10500, 16, "Medium"),
}

# Shared templates must keep their old inherited values.
SHARED_TEMPLATE_DEFAULTS = {
    "ts_gdi_tiberiumharvester": (150000, 60, 1000),
    "ts_nod_tiberiumharvester": (150000, 60, 1000),
    "forgotten_tiberiumharvester": (150000, 60, 1000),
    "cabal_tiberiumharvester": (150000, 60, 1000),
    "japan_japaneseoretruck": (75000, 120, 1000),
    "ra1_soviets_heavyindustrialminer": (135000, 80, 1200),
}


class AcceptedClassicFourHarvesterBatchTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = Model(ROOT).rs

    def test_chassis_and_prices(self):
        for name, values in HARVESTER_DURABILITY.items():
            cost, hp, speed, step, hp_per_step, turn_speed, armor = values
            with self.subTest(actor=name):
                actor = self.rules.resolve(name)
                self.assertEqual(str(cost), actor.child("Valued").get("Cost"))
                self.assertEqual(str(hp), actor.child("Health").get("HP"))
                self.assertEqual(str(speed), actor.child("Mobile").get("Speed"))
                self.assertEqual(
                    str(step), actor.child("ChangesHealth@SelfHealing").get("Step")
                )
                self.assertEqual(
                    str(hp_per_step), actor.child("Repairable").get("HpPerStep")
                )
                self.assertEqual(
                    str(turn_speed), actor.child("Mobile").get("TurnSpeed")
                )
                self.assertEqual(armor, actor.child("Armor").get("Type"))

    def test_shared_templates_unmoved(self):
        for name, (hp, speed, cost) in SHARED_TEMPLATE_DEFAULTS.items():
            with self.subTest(actor=name):
                actor = self.rules.resolve(name)
                self.assertEqual(str(hp), actor.child("Health").get("HP"))
                self.assertEqual(str(speed), actor.child("Mobile").get("Speed"))
                self.assertEqual(str(cost), actor.child("Valued").get("Cost"))


if __name__ == "__main__":
    unittest.main()
