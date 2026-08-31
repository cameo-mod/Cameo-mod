"""THE BAND LAW — that BALANCE_PIPELINE §8.1's rings are DERIVED, not preferred.

⛔ WHAT THIS EXISTS TO PIN. `check_band.FLOOR/SWEET_LO/SWEET_HI/CEIL` read like four
taste decisions, and for most of their life they were documented as such ("practical
floor ~75% — formula breaks down below"). They are not. Hold speed and range at the
anchor's, write h and d for the HP and DPS multipliers, and
`formula.class_baseline_price` collapses to a closed form in which three of the four
rings are EXACT stat windows:

    price(h, d) = (3(h + d) + 4hd + 2) / 12        symmetric in h and d
    price(x, x) = (2x + 1)(x + 1) / 6
    x(P)        = (sqrt(1 + 48P) - 3) / 4

    x = 0.50 -> 0.500 FLOOR      x = 1.00 -> 1.000 SWEET_LO = the anchor
    x = 2.00 -> 2.500 SWEET_HI   x = 2.50 -> 3.500 CEIL

⭐ ALL FOUR RINGS ARE EXACT IN BOTH SPACES AT ONCE, which no earlier candidate managed.
Rings are declared in COST — the space a player reads off the build palette — and the
stat window is the derived reading; here the two agree at every ring.

The maintainer derived SWEET_HI independently — *"cost from 100% to 250% makes sense
because in the balance formula that is exactly true when a unit has 2x HP and 2x DPS"* —
and that is the property the first test here asserts.

⛔ THE TRAP THIS GUARDS. A closed form written into a document is a claim about code that
nothing checks; the moment `class_baseline_estimators` changes shape, every band constant
silently stops meaning what §8.1a says it means, and the band law becomes four magic
numbers again with a derivation attached that is no longer true. So these tests call the
REAL module — never a re-implementation of it — at the exact points the docs cite.
"""

from __future__ import annotations

import math
import pathlib
import sys

import pytest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))

import check_band  # noqa: E402
import formula  # noqa: E402

# An arbitrary anchor. The law is about RATIOS, so nothing here may depend on these.
C0, HP0, SPEED0, RANGE0, DPS0 = 800.0, 100_000.0, 100.0, 5_000.0, 200.0


def price(h: float, d: float, s: float = 1.0, r: float = 1.0) -> float:
    """The REAL pricing function, as a multiple of cost0."""
    return formula.class_baseline_price(
        HP0 * h, SPEED0 * s, RANGE0 * r, DPS0 * d,
        HP0, SPEED0, RANGE0, DPS0, C0) / C0


# --------------------------------------------------------------------------------------
# 1. The maintainer's own derivation of SWEET_HI.
# --------------------------------------------------------------------------------------

def test_double_hp_and_double_dps_costs_exactly_the_sweet_ceiling():
    """*"2x HP and 2x DPS"* is EXACTLY 250% — the ruling SWEET_HI encodes."""
    assert price(2.0, 2.0) == pytest.approx(2.50, abs=1e-12)
    assert check_band.SWEET_HI == pytest.approx(2.50, abs=1e-12)


def test_half_hp_and_half_dps_costs_exactly_the_hard_floor():
    """The same argument DOWNWARD lands on FLOOR — which is why 0.50 is not a taste."""
    assert price(0.5, 0.5) == pytest.approx(0.50, abs=1e-12)
    assert check_band.FLOOR == pytest.approx(0.50, abs=1e-12)


def test_two_and_a_half_stats_cost_exactly_the_hard_ceiling():
    """The ruled CEIL. x2.5 on both stats is exactly 3.5x cost, which is what makes 3.50
    a better ceiling than the 4.00 it replaced (4.00 = x2.7231, round in neither space)."""
    assert price(2.5, 2.5) == pytest.approx(3.50, abs=1e-12)
    assert check_band.CEIL == pytest.approx(3.50, abs=1e-12)


def test_SWEET_LO_IS_THE_ANCHOR_and_both_rejected_values_stay_rejected():
    """⛔ THE REGRESSION THIS FILE EXISTS TO PREVENT — and SWEET_LO has now been wrong
    twice in one week, both times for a reason that looked principled at the time:

      0.7292 (= 35/48)  the cost of x0.75 STATS. Rejected: *"The 75% referred to the unit
                        price not the stats."*
      0.75              a 75% PRICE. Superseded by the four-point ruling: *"we make the
                        1.0x to 2.5x the regular Band ... the baseline actor being exactly
                        at 1.0x."*

    Neither is a bug awaiting re-fix. The target floor IS the anchor, which is what makes
    all four rings exact in both spaces at once."""
    assert check_band.SWEET_LO == pytest.approx(1.00, abs=1e-12)
    assert price(1.0, 1.0) == pytest.approx(1.00, abs=1e-12)
    # both superseded values, pinned so a "restoration" fails loudly
    assert price(0.75, 0.75) == pytest.approx(35.0 / 48.0, abs=1e-12)
    assert price(0.7707, 0.7707) == pytest.approx(0.75, abs=1e-3)
    assert check_band.SWEET_LO not in (pytest.approx(35.0 / 48.0, abs=1e-6),
                                      pytest.approx(0.75, abs=1e-6))


def test_all_FOUR_rings_are_exact_in_both_cost_and_stat_space():
    """⭐ The property that makes 0.50 / 1.00 / 2.50 / 3.50 the right ring set: each is a
    round COST and the preimage is a round STAT multiplier. No earlier candidate managed
    it -- 4.00 was x2.7231, 0.729 was round in stats only, 0.75 in cost only."""
    for cost, stat in ((0.50, 0.50), (1.00, 1.00), (2.50, 2.00), (3.50, 2.50)):
        assert price(stat, stat) == pytest.approx(cost, abs=1e-12)
    assert (check_band.FLOOR, check_band.SWEET_LO,
            check_band.SWEET_HI, check_band.CEIL) == (0.50, 1.00, 2.50, 3.50)


# --------------------------------------------------------------------------------------
# 2. The closed forms themselves, against the real module.
# --------------------------------------------------------------------------------------

@pytest.mark.parametrize("x", [0.25, 0.5, 0.75, 1.0, 1.5, 2.0, 2.5, 3.0, 4.0])
def test_diagonal_closed_form_matches_formula_py(x):
    assert price(x, x) == pytest.approx((2 * x + 1) * (x + 1) / 6, abs=1e-12)


@pytest.mark.parametrize("h,d", [(2, 2), (4, 1), (1, 4), (0.5, 0.5), (3, 0.5), (6, 0.37)])
def test_two_stat_closed_form_matches_formula_py(h, d):
    assert price(h, d) == pytest.approx((3 * (h + d) + 4 * h * d + 2) / 12, abs=1e-12)


@pytest.mark.parametrize("p", [0.5, 0.729166, 0.75, 1.0, 2.5, 3.5, 4.0])
def test_the_inverse_recovers_the_stat_window(p):
    x = (math.sqrt(1 + 48 * p) - 3) / 4
    assert price(x, x) == pytest.approx(p, abs=1e-6)


# --------------------------------------------------------------------------------------
# 3. The properties the band law RESTS on, which no single number states.
# --------------------------------------------------------------------------------------

def test_hp_and_dps_are_interchangeable_in_pricing():
    """⭐ The formula is SYMMETRIC in h and d. This is what licenses the maintainer's
    *"one of the stats can also be higher if the other one is a bit lower"* — a class
    member may trade HP for DPS along a ring at no cost, which is the whole reason a
    2.5x-wide band can hold units that play nothing alike."""
    for h, d in ((4, 1), (3, 0.5), (2.5, 1.25), (6, 0.37)):
        assert price(h, d) == pytest.approx(price(d, h), abs=1e-12)


def test_the_ceiling_is_a_CURVE_not_a_box():
    """`3(h+d) + 4hd = 28` is the ENTIRE 250% iso-cost line. Reading the band as a box
    ("no more than 2x HP AND no more than 2x DPS") would wrongly exclude every one of
    these, all of which cost exactly the ceiling."""
    for h in (1.0, 1.5, 2.0, 3.0, 4.0, 6.0):
        d = (28 - 3 * h) / (3 + 4 * h)
        assert d > 0
        assert price(h, d) == pytest.approx(2.50, abs=1e-9)


def test_a_class_spread_does_not_depend_on_which_member_anchors_it():
    """⛔ THE PROPERTY THAT MAKES RE-ANCHORING USELESS ON A TOO-WIDE CLASS, and the
    reason `band_granularity.py` may price a class off its first member. Members are
    priced as RATIOS to the anchor, so changing the anchor SLIDES the whole class along
    the band and can never NARROW it. Anyone proposing to fix occupancy by re-anchoring
    a class wider than the band is contradicting this test."""
    members = [(1.0, 1.0), (2.0, 1.5), (0.6, 3.0), (4.0, 0.9), (1.3, 1.3)]

    def spread_from(anchor):
        ah, ad = anchor
        ps = [price(h / ah, d / ad) for h, d in members]
        return max(ps) / min(ps)

    ref = spread_from(members[0])
    for m in members[1:]:
        # the spread moves (the formula is not a pure power law) but never COLLAPSES:
        # no anchor choice brings a class inside a band it does not fit.
        assert spread_from(m) > 1.0
    assert ref > 1.0


def test_the_band_rings_are_ordered_and_the_target_sits_inside_the_hard_band():
    """⚠ The target floor now EQUALS the anchor, so this is `<=`, not `<`. The strict form
    encoded the old four-ring layout and failed the moment the ruling landed — which is
    the test doing its job, not a licence to loosen it further: FLOOR must stay strictly
    below the anchor, or the extended band has no lower skirt at all."""
    assert check_band.FLOOR < check_band.SWEET_LO
    assert check_band.SWEET_LO <= 1.0 < check_band.SWEET_HI < check_band.CEIL
    assert check_band.SOFT_FLOOR == check_band.SWEET_LO


def test_speed_and_range_are_held_at_the_anchor_in_every_claim_above():
    """The closed forms are only true with s = r = 1. Stated as a test so nobody quotes
    price(x, x) at a member whose range differs — range carries `special` and moves the
    price on its own."""
    assert price(2.0, 2.0, s=1.0, r=1.0) == pytest.approx(2.50, abs=1e-12)
    assert price(2.0, 2.0, s=1.0, r=2.0) != pytest.approx(2.50, abs=1e-3)
