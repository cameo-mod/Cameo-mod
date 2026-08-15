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

    def test_tesla_now_joins_the_discount(self):
        """W16 SUPERSEDES the old exclusion.

        `AttackTesla` used to return 1.0, because a flat 0.75 would have paid a
        Tesla Coil (a fifth of its cycle spent charging) the same as an Obelisk (a
        third) — compensating one weakness twice. The discount is now proportional
        to the measured share, so there is nothing left to exclude and the
        exclusion set is retired empty.
        """
        self.assertEqual(formula.CHARGE_UP_EXCLUDED_TRAITS, frozenset())
        self.assertIn("AttackTesla", formula.CHARGE_UP_TRAITS)

    def test_discount_scales_with_the_measured_charge_share(self):
        """The VERIFY table from W16, using each actor's real resolved values."""
        obelisk = formula.charge_price_multiplier(
            {"v": "AttackCharges", "ticks": 50}, 96)          # 34.2% -> the anchor
        ra1_tesla = formula.charge_price_multiplier(
            {"v": "AttackTesla", "ticks": 25, "cycle": 100})  # 20.0%
        ra2_tesla = formula.charge_price_multiplier(
            {"v": "AttackTesla", "ticks": 22, "cycle": 75})   # 22.7% (engine default 22)
        railtower = formula.charge_price_multiplier(
            {"v": "AttackTesla", "ticks": 12, "cycle": 120})  # 9.1%

        self.assertAlmostEqual(obelisk, 0.75, places=3)       # anchors the ruling
        self.assertGreater(railtower, ra2_tesla)              # least charge, least discount
        self.assertGreater(railtower, ra1_tesla)
        for m in (ra1_tesla, ra2_tesla, railtower):
            self.assertGreater(m, 0.75)                       # all lighter than the Obelisk
            self.assertLess(m, 1.0)                           # but all still discounted
        # A LONGER charge than the anchor cannot buy more than the anchor's discount.
        self.assertEqual(formula.charge_price_multiplier(
            {"v": "AttackCharges", "ticks": 50, "cycle": 60}), 0.75)   # 45.5%, clamped

    def test_unmeasurable_charge_falls_back_to_the_flat_rate(self):
        """It charges; we just cannot see by how much.

        Pricing it as if it did NOT charge would be the larger error, because a
        price cut is a BUFF in value terms — over-paying a unit is not a safe
        default.
        """
        self.assertEqual(formula.charge_price_multiplier("AttackTesla"), 0.75)
        self.assertEqual(formula.charge_price_multiplier(
            {"v": "AttackCharges", "ticks": None, "cycle": None}), 0.75)

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

    def test_tesla_actor_is_discounted_in_proportion(self):
        """W16: a real Tesla Coil pays LESS discount than the Obelisk, not none.

        The unit carries its own cycle (`AttackTesla.ReloadDelay`), so the weapon's
        reload is not consulted — which matters, because a Tesla Coil's armaments
        reload every 3 ticks and using that would read as a 90% charge share.
        """
        plain = self._price({})
        tesla = self._price({"charge_up": {"v": "AttackTesla", "ticks": 25,
                                           "cycle": 100, "src": "inherited"}})
        obelisk = self._price({"charge_up": {"v": "AttackCharges", "ticks": 50,
                                             "cycle": 96, "src": "inherited"}})
        self.assertLess(tesla, plain)                       # it IS discounted now
        self.assertGreater(tesla, obelisk)                  # but less than the anchor
        self.assertAlmostEqual(obelisk / plain, 0.75)


if __name__ == "__main__":
    unittest.main()
