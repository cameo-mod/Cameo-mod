"""Unit tests for W4: the retired weapon-class weight + the charge-up discount.

Two rulings are pinned here (maintainer, 2026-08-11):

1. `formula.dps()` no longer takes a `weapon_class` weight. The K coefficient
   measures weapon quality from the weapon's own geometry, so keeping the tier
   weight as well would charge a weapon twice for the same property.
2. Charge-up is an ACTOR property worth a flat 0.75x on the PRICE — the delay
   inflates the effective reload AND leaves the unit helpless while it winds up,
   neither of which the weapon's own stats can show.
"""

from __future__ import annotations

import inspect
import pathlib
import sys
import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))
import fit_class  # noqa: E402
import formula  # noqa: E402


class WeaponClassRetiredTest(unittest.TestCase):
    def test_dps_has_no_weapon_class_parameter(self):
        """The real VERIFY for W4 — the signature, not a grep over prose."""
        self.assertNotIn("weapon_class", inspect.signature(formula.dps).parameters)

    def test_dps_is_damage_over_effective_reload(self):
        self.assertEqual(formula.dps(10000, 50), 200.0)
        self.assertEqual(formula.dps(10000, 50, firepower_multiplier=1.5), 300.0)

    def test_burst_still_positional_third(self):
        """Third positional is now `burst`; a caller that forgot to drop the old
        weapon_class argument would silently pass 1.25 as a burst count."""
        self.assertEqual(formula.dps(1000, 10, 2, 5), 1000 * 2 / (10 + 5))


class ChargeMultiplierTest(unittest.TestCase):
    def test_each_charge_trait_discounts(self):
        for trait in ("AttackCharged", "AttackTurretedCharged",
                      "AttackFrontalCharged", "AttackCharges"):
            self.assertEqual(formula.charge_price_multiplier(trait), 0.75, trait)

    def test_obelisk_trait_is_covered(self):
        """AttackCharges is the Obelisk of Light — the case the ruling cites.
        It is NOT one of the three `*Charged` traits, so naming only those would
        have left the cited precedent undiscounted."""
        self.assertEqual(formula.charge_price_multiplier("AttackCharges"), 0.75)

    def test_tesla_is_excluded(self):
        """The Tesla Coil is already priced as a special case; the generic discount on
        top would compensate one weakness twice, leaving it cost-efficient — a price
        cut is a BUFF in value terms, so over-paying a unit is not a safe error."""
        self.assertEqual(formula.charge_price_multiplier("AttackTesla"), 1.0)
        self.assertIn("AttackTesla", formula.CHARGE_UP_EXCLUDED_TRAITS)

    def test_suffixed_trait_still_matches(self):
        self.assertEqual(formula.charge_price_multiplier("AttackCharged@primary"), 0.75)

    def test_no_trait_is_neutral(self):
        for value in (None, "", "AttackTurreted"):
            self.assertEqual(formula.charge_price_multiplier(value), 1.0, repr(value))


class ChargedUnitPricingTest(unittest.TestCase):
    """The fixture the ruling asks for: identical units, one charges."""

    INPUTS = (100_000, 100, 5000, 200.0, 1.0, 1.0, 1.0)   # the Tiger anchor

    def _price(self, unit):
        o0, p0, q0 = formula.estimators(*self.INPUTS)
        return fit_class.price_unit(unit, self.INPUTS, o0, p0, q0, 800)

    def test_charged_actor_prices_three_quarters_of_an_identical_one(self):
        plain = self._price({})
        charged = self._price({"charge_up": {"v": "AttackTurretedCharged",
                                             "src": "inherited"}})
        self.assertAlmostEqual(plain, 800.0)          # anchor identity holds
        self.assertAlmostEqual(charged / plain, 0.75)

    def test_tesla_actor_is_not_discounted(self):
        plain = self._price({})
        tesla = self._price({"charge_up": {"v": "AttackTesla", "src": "inherited"}})
        self.assertAlmostEqual(tesla, plain)


if __name__ == "__main__":
    unittest.main()
