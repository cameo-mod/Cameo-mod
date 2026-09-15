"""Passenger-sum closure after the accepted classic-four infantry prices."""

from __future__ import annotations

import pathlib
import sys
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))

from cameo_model import Model  # noqa: E402
import cargo_pricing  # noqa: E402


VALID_CARRIERS = {
    "ra1_allies_alliedapc": 1740,
    "ra1_allies_alliedchinooktransport": 4360,
    "ra1_allies_phasetransport": 2180,
    "ra1_soviets_btr80": 1770,
    "ra1_soviets_flaktruck": 950,
    "ra1_soviets_hiptransport": 2730,
    "td_gdi_apc": 1740,
    "td_gdi_assaultapc": 4090,
    "td_gdi_chinooktransport": 4090,
    "td_gdi_humveemkii": 870,
    "td_nod_buggymkii": 880,
}


class AcceptedClassicFourCargoPriceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = Model(ROOT).rs

    def test_valid_authored_loads_equal_purchase_price(self):
        for actor_name, expected in VALID_CARRIERS.items():
            with self.subTest(actor=actor_name):
                load = cargo_pricing.authored_load(self.rules, actor_name)
                self.assertEqual([], load["issues"])
                self.assertEqual(expected, load["passenger_sum"])
                self.assertEqual(
                    str(expected), self.rules.resolve(actor_name).child("Valued").get("Cost")
                )

    def test_invalid_nod_chinook_remains_held(self):
        load = cargo_pricing.authored_load(self.rules, "td_nod_chinooktransport")
        self.assertIn("filled weight 11/8", load["issues"])
        self.assertIsNone(load["passenger_sum"])
        self.assertEqual(
            "3100",
            self.rules.resolve("td_nod_chinooktransport").child("Valued").get("Cost"),
        )


if __name__ == "__main__":
    unittest.main()
