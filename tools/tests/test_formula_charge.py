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
        """The VERIFY table from W16, using each actor's real resolved values.

        The second argument is the WEAPON's reload: the burst delay for AttackTesla,
        and the whole cycle for the ChargeLevel family.
        """
        obelisk = formula.charge_price_multiplier(
            {"v": "AttackCharges", "ticks": 50}, 96)           # 34.2% -> the anchor
        ra1_tesla = formula.charge_price_multiplier(
            {"v": "AttackTesla", "ticks": 25, "cycle_reload": 100, "burst": 3}, 3)
        ra2_tesla = formula.charge_price_multiplier(
            {"v": "AttackTesla", "ticks": 20, "cycle_reload": 75, "burst": 1}, 3)
        railtower = formula.charge_price_multiplier(
            {"v": "AttackTesla", "ticks": 12, "cycle_reload": 120, "burst": 5}, 10)

        self.assertAlmostEqual(obelisk, 0.75, places=3)        # anchors the ruling
        self.assertGreater(railtower, ra1_tesla)               # least charge, least discount
        self.assertGreater(railtower, ra2_tesla)
        for m in (ra1_tesla, ra2_tesla, railtower):
            self.assertGreater(m, 0.75)                        # all lighter than the Obelisk
            self.assertLess(m, 1.0)                            # but all still discounted
        # A LONGER charge than the anchor cannot buy more than the anchor's discount.
        self.assertEqual(formula.charge_price_multiplier(
            {"v": "AttackCharges", "ticks": 50}, 60), 0.75)    # 45.5%, clamped

    def test_attack_tesla_overrides_the_weapon_reload(self):
        """Maintainer 2026-08-15, the rule that makes AttackTesla different.

        "If you have the AttackTesla trait, ReloadDelay is taken from that instead
        of from the weapon, and the reload delay from the weapon counts as the burst
        delay." So the cycle is the TRAIT's reload, the zaps are a burst, and the
        WEAPON's reload is the gap between them.

        ⚠ `ChargeDelay` is NOT the gap. It is 3 by default, and both Tesla Coils
        happen to use weapons that also reload in 3 — so an earlier draft using
        ChargeDelay got those two right by coincidence and the AA railtower wrong,
        whose weapon reloads in 10 (a 160-tick cycle, not 132).
        """
        ra1 = formula.charge_attack_cycle(
            {"v": "AttackTesla", "cycle_reload": 100, "burst": 3}, 3)
        rail = formula.charge_attack_cycle(
            {"v": "AttackTesla", "cycle_reload": 120, "burst": 5}, 10)
        ra2 = formula.charge_attack_cycle(
            {"v": "AttackTesla", "cycle_reload": 75, "burst": 1}, 3)
        self.assertEqual(ra1, (106, 3))    # 100 + 3x(3-1)
        self.assertEqual(rail, (160, 5))   # 120 + 10x(5-1)  <- NOT 132
        self.assertEqual(ra2, (75, 1))     # one charge, no burst

        spec = formula.CHARGE_FIELDS["AttackTesla"]
        self.assertEqual(spec["cycle_reload"], ("ReloadDelay", 120))
        self.assertEqual(spec["burst"], ("MaxCharges", 1))

    def test_charge_level_family_does_not_override_the_weapon(self):
        """The Obelisk's gun keeps its own reload; the charge merely delays it."""
        self.assertIsNone(formula.charge_attack_cycle(
            {"v": "AttackCharges", "ticks": 50}, 96))

    def test_tesla_dps_is_zaps_per_trait_cycle_not_per_weapon_reload(self):
        """The pricing consequence, and the reason this matters at all.

        A Tesla Coil's weapon reloads every 3 ticks. Priced off the weapon it looks
        like it fires 20 times a second; it actually fires 3 zaps per 106 ticks.
        """
        cycle, shots = formula.charge_attack_cycle(
            {"v": "AttackTesla", "cycle_reload": 100, "burst": 3}, 3)
        real = 10_000 * shots / cycle
        naive = formula.dps(10_000, 3)
        self.assertAlmostEqual(real, 283.0, places=0)
        self.assertGreater(naive / real, 11)      # ~11.8x overstated

    def test_the_three_coils_after_the_burst_correction(self):
        """The live values, and the ordering flip the burst law causes.

        RA1 charges LONGER (25 vs 20) yet ends up with the SMALLER share, because
        its three zaps stretch the cycle to 106 while the single-charge RA2 coil
        stays at 75. Charge share is a ratio, not a duration.
        """
        ra1 = formula.charge_price_multiplier(
            {"v": "AttackTesla", "ticks": 25, "cycle_reload": 100, "burst": 3}, 3)
        ra2 = formula.charge_price_multiplier(
            {"v": "AttackTesla", "ticks": 20, "cycle_reload": 75, "burst": 1}, 3)
        rail = formula.charge_price_multiplier(
            {"v": "AttackTesla", "ticks": 12, "cycle_reload": 120, "burst": 5}, 10)
        self.assertGreater(ra1, ra2)      # RA1 discounted LESS despite charging longer
        self.assertGreater(rail, ra1)     # railtower least of all
        for m in (ra1, ra2, rail):
            self.assertTrue(0.75 < m < 1.0)

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

        The coil brings its OWN cycle (`AttackTesla.ReloadDelay`), so the weapon's
        reload is never the cycle — which matters, because a Tesla Coil's armaments
        reload every 3 ticks and reading that as the cycle would show a ~90% charge
        share. Here the unit has no armaments at all, so the burst delay is 0 and
        the cycle is the trait's 100 flat.
        """
        plain = self._price({})
        tesla = self._price({"charge_up": {"v": "AttackTesla", "ticks": 25,
                                           "cycle_reload": 100, "burst": 3,
                                           "src": "inherited"}})
        # No armaments -> no measurable cycle for the ChargeLevel family -> flat rate.
        obelisk = self._price({"charge_up": {"v": "AttackCharges", "ticks": 50,
                                             "src": "inherited"}})
        self.assertLess(tesla, plain)                       # it IS discounted now
        self.assertGreater(tesla, obelisk)                  # but less than the anchor
        self.assertAlmostEqual(obelisk / plain, 0.75)


if __name__ == "__main__":
    unittest.main()
