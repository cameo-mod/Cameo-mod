"""THE TARGET CURVE MUST STAY TIED TO THE BUILD LIMITS IT CLAIMS TO BE DERIVED FROM.

⛔ WHAT THIS DEFENDS. `bot_difficulty_curve.py` justifies its per-difficulty asymptotes by saying
they come from `BotLimits.HarvesterLimit` and its time constants from `ProductionTimeMultiplier`.
That justification is worth exactly as much as the coupling behind it: the moment either ladder is
retuned in yaml and the curve keeps its own copy, the derivation becomes a story rather than a
measurement — the dead-knob antipattern this repo keeps paying for, in a new place.

So these tests assert the coupling is REAL (the module reads the tree), the ladders are still the
shape the derivation assumes, and the curve has the properties that make it usable as a "par" line.
"""

from __future__ import annotations

import math
import pathlib
import sys

import pytest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))

import bot_difficulty_curve as c  # noqa: E402


# ------------------------------------------------------- the coupling is real

def test_the_limits_are_read_from_the_tree_not_hardcoded():
    src = (ROOT / "tools" / "balance" / "bot_difficulty_curve.py").read_text(encoding="utf-8")
    assert "mods/cameo/ai/ai.yaml" in src
    assert "mods/cameo/rules/defaults.yaml" in src
    assert c.HARVESTER_LIMIT and c.PRODUCTION_TIME


def test_every_difficulty_has_a_limit_and_a_multiplier():
    for d in c.DIFFICULTIES:
        assert d in c.HARVESTER_LIMIT, f"{d} has no HarvesterLimit in ai.yaml"
        assert d in c.PRODUCTION_TIME, f"{d} has no ProductionTimeMultiplier in defaults.yaml"


def test_the_harvester_ladder_is_still_the_exact_1x_to_10x_the_derivation_assumes():
    """3, 6, 9 ... 30. If this changes, the asymptote scale changes with it — by design."""
    limits = [c.HARVESTER_LIMIT[d] for d in c.DIFFICULTIES]
    assert limits == [3 * (i + 1) for i in range(len(c.DIFFICULTIES))], limits
    assert limits[-1] == 10 * limits[0], "the 1x..10x span the asymptote scale rests on is gone"


def test_the_production_time_ladder_still_descends():
    times = [c.PRODUCTION_TIME[d] for d in c.DIFFICULTIES]
    assert times == sorted(times, reverse=True), times


# ------------------------------------------------------- the curve behaves

def test_the_curve_starts_at_the_opening_bank_exactly():
    """A par line that does not equal starting cash at t=0 makes every bot look behind on tick 1."""
    for d in c.DIFFICULTIES:
        assert c.logistic(d, 0) == c.START_CASH


def test_the_curve_rises_and_saturates_at_its_asymptote():
    for d in c.DIFFICULTIES:
        assert c.logistic(d, 90) <= c.asymptote(d)
        assert c.logistic(d, 90) >= 0.99 * c.asymptote(d)


def test_the_curve_is_monotonic_in_time_and_in_difficulty():
    for d in c.DIFFICULTIES:
        vals = [c.logistic(d, m) for m in range(0, 60, 2)]
        assert vals == sorted(vals), d
    for m in (10, 20, 40):
        vals = [c.logistic(d, m) for d in c.DIFFICULTIES]
        assert vals == sorted(vals), (m, vals)


def test_it_really_is_a_sigmoid_and_not_an_exponential_approach():
    """⭐ The distinction the maintainer asked about, asserted rather than described.

    A logistic ACCELERATES first: its steepest climb is at the midpoint, not at t=0. The
    Mitscherlich curve people usually reach for is steepest at t=0 and only ever decelerates.
    """
    d = "medium"
    slopes = [c.logistic(d, m + 1) - c.logistic(d, m) for m in range(0, 30)]
    assert slopes[0] < slopes[len(slopes) // 3], "the logistic is not accelerating — not a sigmoid"
    assert max(slopes) > slopes[0] * 5, "no steep middle phase"

    mit = [c.mitscherlich(d, m + 1) - c.mitscherlich(d, m) for m in range(0, 30)]
    assert mit == sorted(mit, reverse=True), "the Mitscherlich curve should only ever decelerate"
    assert mit[0] > slopes[0], "the two curves are indistinguishable early — the point is lost"


# ------------------------------------------------------- combining the ratios

@pytest.mark.parametrize(("a", "b"), [(0.5, 0.5), (0.25, 0.25), (0.8, 0.8), (2.0, 2.0)])
def test_the_product_double_counts_correlated_evidence(a, b):
    """⛔ Why the two ratios must not be multiplied.

    They measure the same underlying failure from two angles, so a product squares one piece of
    evidence. The geometric mean keeps the result on the scale of its inputs — two observations
    that each say "half par" combine to "half par", not "a quarter of par".
    """
    assert c.combine(a, b, "product") < c.combine(a, b, "geometric") or a >= 1.0
    assert math.isclose(c.combine(a, a, "geometric"), a, rel_tol=1e-9)


def test_the_geometric_mean_is_neutral_when_the_signals_disagree():
    """Twice the field but half the curve is PAR, and should read as exactly 1.0."""
    assert math.isclose(c.combine(0.5, 2.0, "geometric"), 1.0, rel_tol=1e-9)


def test_combining_is_symmetric():
    for how in ("product", "geometric", "min"):
        assert math.isclose(c.combine(0.4, 0.9, how), c.combine(0.9, 0.4, how), rel_tol=1e-9)
