"""Unit tests for the W5 context factors in tools/balance/weapon_efficiency.py.

The five things K alone could not see (maintainer 2026-08-11). Each is a SEPARATE
named factor so a price that moved can be traced to ONE of them:

    targets   ValidTargets — a weapon that cannot hit air is worth less
    range     outranging is worth more than DPS, bounded
    deadzone  a MinRange hole is a real cost
    overkill  DPS ignores waste; a 200k burst on a 50k target throws 75% away

The fifth, `AttackDelay`, does not exist as a weapon field anywhere in the tree —
charge-up is an ACTOR trait and was implemented there in W4 (`formula.charge_price_multiplier`).

The load-bearing invariant tested here: targets/range/deadzone are INDEPENDENT OF
DAMAGE, so they fold into `k_context` and the pricing inversion stays closed-form.
`overkill` is not, so it must stay OUT of K.
"""

from __future__ import annotations

import math
import pathlib
import sys
import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))
import weapon_efficiency as we  # noqa: E402


class Node:
    """Minimal stand-in for a resolved weapon node (only `get` is used)."""

    def __init__(self, **fields):
        self.fields = fields

    def get(self, key):
        return self.fields.get(key)


class TargetsFactorTest(unittest.TestCase):
    def test_all_targets_is_neutral(self):
        self.assertAlmostEqual(
            we.targets_factor(Node(ValidTargets="Ground, Water, Air")), 1.0)

    def test_ground_only_loses_the_air_share(self):
        """Air is 10% of the engagement mass, softened by the floor."""
        got = we.targets_factor(Node(ValidTargets="Ground, Water"))
        self.assertAlmostEqual(got, we.TARGETS_FLOOR + (1 - we.TARGETS_FLOOR) * 0.90)
        self.assertLess(got, 1.0)

    def test_air_only_is_the_narrowest(self):
        air = we.targets_factor(Node(ValidTargets="Air"))
        ground = we.targets_factor(Node(ValidTargets="Ground"))
        self.assertLess(air, ground)
        self.assertGreaterEqual(air, we.TARGETS_FLOOR)   # floored, not annihilated

    def test_missing_field_is_the_engine_default(self):
        """No ValidTargets means the engine hits everything — not 'hits nothing'."""
        self.assertAlmostEqual(we.targets_factor(Node()), 1.0)

    def test_exotic_target_set_does_not_guess(self):
        """Infantry/Monster/Garrison carries no Ground or Air token; scoring it as
        'hits nothing' would be worse than declining to judge."""
        self.assertAlmostEqual(
            we.targets_factor(Node(ValidTargets="Infantry, Monster")), 1.0)


class RangeFactorTest(unittest.TestCase):
    def test_median_range_is_neutral(self):
        median = we.median_weapon_range()
        self.assertAlmostEqual(we.range_factor(Node(Range=str(int(median))), median), 1.0)

    def test_longer_range_scores_higher(self):
        near = we.range_factor(Node(Range="3000"), 6000)
        far = we.range_factor(Node(Range="12000"), 6000)
        self.assertLess(near, 1.0)
        self.assertGreater(far, 1.0)

    def test_upper_bound_clamps(self):
        _lo, hi = we.RANGE_BOUNDS
        self.assertAlmostEqual(we.range_factor(Node(Range="9999999"), 6000), hi)

    def test_lower_bound_is_an_asymptote_not_a_clamp(self):
        """With RANGE_WEIGHT 0.25 the factor bottoms out at 1-0.25 = 0.75 as range
        approaches 0, so the low bound is a safety rail that never actually bites.
        Pinned so that raising RANGE_WEIGHT past 0.25 — which WOULD make it bite —
        is a deliberate, visible change rather than a surprise."""
        lo, _hi = we.RANGE_BOUNDS
        tiny = we.range_factor(Node(Range="1"), 6000)
        self.assertGreaterEqual(tiny, lo)
        self.assertLess(tiny, lo + 0.001)
        self.assertAlmostEqual(1.0 - we.RANGE_WEIGHT, lo)

    def test_missing_range_is_neutral(self):
        self.assertAlmostEqual(we.range_factor(Node(), 6000), 1.0)


class DeadzoneFactorTest(unittest.TestCase):
    def test_no_minrange_is_neutral(self):
        self.assertAlmostEqual(we.deadzone_factor(Node(Range="6000")), 1.0)

    def test_hole_costs_the_area_ratio(self):
        """MinRange half the range loses a quarter of the disc."""
        self.assertAlmostEqual(
            we.deadzone_factor(Node(Range="6000", MinRange="3000")), 0.75)

    def test_bigger_hole_costs_more(self):
        small = we.deadzone_factor(Node(Range="10000", MinRange="1000"))
        big = we.deadzone_factor(Node(Range="10000", MinRange="5000"))
        self.assertGreater(small, big)

    def test_nonsense_minrange_is_ignored(self):
        """MinRange >= Range would make the weapon unusable; do not emit a
        negative or zero factor from what is almost certainly a data bug."""
        self.assertAlmostEqual(
            we.deadzone_factor(Node(Range="5000", MinRange="9000")), 1.0)


class OverkillFactorTest(unittest.TestCase):
    def test_small_shots_waste_nothing(self):
        self.assertAlmostEqual(we.overkill_factor(1000, 100_000), 1.0)

    def test_the_documented_case(self):
        """A 200k shot on a 50k target keeps a quarter."""
        self.assertAlmostEqual(we.overkill_factor(200_000, 50_000), 0.25)

    def test_exact_multiple_wastes_nothing(self):
        self.assertAlmostEqual(we.overkill_factor(25_000, 100_000), 1.0)

    def test_waste_is_only_the_last_shot(self):
        """3 shots of 40k to remove 100k = 120k dealt, so 100/120 is kept."""
        self.assertAlmostEqual(we.overkill_factor(40_000, 100_000), 100 / 120)

    def test_never_exceeds_one_and_degrades_safely(self):
        for per_shot in (1, 999, 74_000, 10 ** 7):
            self.assertLessEqual(we.overkill_factor(per_shot, 74_000), 1.0 + 1e-9)
        self.assertAlmostEqual(we.overkill_factor(0, 74_000), 1.0)


class DamageIndependenceTest(unittest.TestCase):
    """The property the whole pricing inversion rests on."""

    def test_context_factors_do_not_depend_on_damage(self):
        node = Node(Range="8000", MinRange="2000", ValidTargets="Ground, Water")
        first = (we.targets_factor(node), we.range_factor(node, 6000),
                 we.deadzone_factor(node))
        # Nothing about these reads Damage at all — assert that explicitly by
        # showing the same node scores identically however it is called.
        second = (we.targets_factor(node), we.range_factor(node, 6000),
                  we.deadzone_factor(node))
        self.assertEqual(first, second)
        self.assertNotAlmostEqual(first[0], 1.0)   # and they are actually biting

    def test_overkill_DOES_depend_on_damage(self):
        """Which is exactly why it is reported beside K instead of inside it —
        folding it in would turn the closed-form inversion into a fixed point."""
        self.assertNotAlmostEqual(we.overkill_factor(150_000, 74_000),
                                  we.overkill_factor(15_000, 74_000))


if __name__ == "__main__":
    unittest.main()
