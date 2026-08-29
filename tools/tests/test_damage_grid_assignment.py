"""Regression tests for the converter's lever ORDER and its slot assignment.

⛔ THE BUG THESE PIN. `propose_class_rebalance` ran the three price levers in the
wrong order: the COARSE one (warhead Damage, on the 100 grid) went LAST, after
the fine ones (Range on a 10 grid, Speed by 1). One Damage step is a whole shot,
so the final pass threw away everything the fine-tuners had achieved. Measured on
class `scout`, worst |Δ| across that single call went **15.6 -> 66.5** — the
uniqueness pass, not the pricing, was the dominant error in the whole report.

The pass was also a greedy first-fit in ledger order: whoever came first in the
file took the slot, and a later member got shoved several steps away.
`forgotten_mutant` was displaced 500 -> 200 and `td_nod_minigunner` 700 -> 1200
for no reason but filename sort order.
"""

import pathlib
import sys
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "balance"))

import formula  # noqa: E402
import propose_class_rebalance as P  # noqa: E402

STEP = formula.DAMAGE_STEP


def row(actor, cost, per_wh, per_unit, n_wh=1, **kw):
    r = {"actor": actor, "cost": cost, "per_wh": per_wh, "n_wh": n_wh,
         "per_unit": per_unit, "protected": False, "soft": False,
         "dmg_shot": per_wh * n_wh, "dmg_eff": per_wh * n_wh,
         "dps_eff": per_unit * per_wh * n_wh}
    r.update(kw)
    return r


def linear_price(slope):
    """A stand-in for `class_baseline_price`: affine in effective damage, which
    is what makes each row's |Δ| a V and the assignment DP exact."""
    def price(r, dmg_shot):
        return slope * r["per_unit"] * dmg_shot
    return price


class AssignmentIsPriceAware(unittest.TestCase):
    def test_colliding_rows_split_around_the_shared_ideal(self):
        """Two rows wanting slot 800 end adjacent to it, not shoved down the grid."""
        rows = [row("a", 100, 800, 0.05), row("b", 100, 800, 0.05)]
        P.unique_dmg_per_shot(rows, price_of=linear_price(2.5))
        got = sorted(r["dmg_eff"] for r in rows)
        self.assertEqual(got, [700, 800])

    def test_the_row_a_step_costs_most_keeps_the_contested_slot(self):
        """Both want slot 800, but one grid step is 12.5 credits to one of them
        and 125 to the other. Minimax gives the contested slot to the row that
        can least afford to leave it — the old ledger-order greedy gave it to
        whichever file sorted first."""
        cheap = row("cheap", 100, 800, 0.125)     # one step = 12.5 credits
        dear = row("dear", 1000, 800, 1.25)       # one step = 125 credits
        rows = [cheap, dear]                      # cheap first: greedy would
        P.unique_dmg_per_shot(rows, price_of=linear_price(1.0))
        self.assertEqual(dear["dmg_eff"], 800)    # ...have handed it 800
        self.assertNotEqual(cheap["dmg_eff"], 800)

    def test_result_does_not_depend_on_row_order(self):
        """The greedy it replaced gave a different answer per ledger sort order."""
        def solve(order):
            rows = [row(a, 100, 800, 0.05) for a in order]
            P.unique_dmg_per_shot(rows, price_of=linear_price(2.5))
            return sorted(r["dmg_eff"] for r in rows)
        self.assertEqual(solve(["a", "b", "c"]), solve(["c", "b", "a"]))

    def test_every_member_still_ends_unique(self):
        rows = [row(f"u{i}", 100, 800, 0.05) for i in range(6)]
        P.unique_dmg_per_shot(rows, price_of=linear_price(2.5))
        vals = [r["dmg_eff"] for r in rows]
        self.assertEqual(len(set(vals)), len(vals))

    def test_protected_and_soft_members_are_never_moved(self):
        keep = row("anchor", 100, 800, 0.05, protected=True)
        soft = row("spawn", 100, 800, 0.05, soft=True)
        rows = [keep, soft, row("free", 100, 800, 0.05)]
        P.unique_dmg_per_shot(rows, price_of=linear_price(2.5))
        self.assertEqual(keep["dmg_eff"], 800)
        self.assertEqual(soft["dmg_eff"], 800)

    def test_mixed_warhead_counts_fall_back_instead_of_mis_assigning(self):
        """Effective damage is `per_wh x n_wh`, so rows with different warhead
        counts sit on grids of different pitch and one DP cannot interleave them.
        The helper must say so rather than return a wrong plan."""
        rows = [row("a", 100, 800, 0.05, n_wh=1), row("b", 100, 400, 0.05, n_wh=2)]
        self.assertIsNone(
            P.DamageGridAssignment(rows, STEP, linear_price(2.5)).solve())
        P.unique_dmg_per_shot(rows, price_of=linear_price(2.5))   # greedy path
        self.assertNotEqual(rows[0]["dmg_eff"], rows[1]["dmg_eff"])

    def test_ideal_is_solved_from_the_price_line_not_read_off_the_row(self):
        """Reading the ideal off `per_wh` made the row ORDER depend on the last
        pass's output, so re-running walked the ideals away from the prices."""
        r = row("a", 100, 12345, 0.05)          # current value is nonsense
        helper = P.DamageGridAssignment([r], STEP, linear_price(2.5))
        self.assertEqual(helper.ideal(0), 800)  # 100 = 2.5 * 0.05 * 800


class LeverOrderIsCoarseFirst(unittest.TestCase):
    def test_uniqueness_runs_before_the_fine_tuners(self):
        """The whole 15.6 -> 66.5 regression was this ordering."""
        import inspect
        body = inspect.getsource(P.rebalance_class)
        self.assertLess(body.index("unique_dmg_per_shot"),
                        body.index("fine_tune_range"))
        self.assertLess(body.index("unique_dmg_per_shot"),
                        body.index("fine_tune_speed"))


class OnlyTheAnchorIsFrozen(unittest.TestCase):
    """Maintainer ruling 2026-08-29: the verifier is a RATIO, not a frozen actor.

    It used to be exempt from balancing alongside the anchor. Measured across 23
    classes, freezing it moved the other members' worst |Δ| by 0.0 in 17 of them,
    only 8 of 23 sat at the 2.5x cost0 the law names, and — because exempt rows are
    excluded from the report's worst-|Δ| line — a verifier off by 3779.9 credits
    was invisible in the report meant to catch exactly that.
    """

    def test_load_class_rows_protects_the_anchor_alone(self):
        import inspect
        src = inspect.getsource(P.load_class_rows)
        head = src[src.index("protected = "):src.index("protected.discard(None)")]
        self.assertIn("anchor_actor", head)
        self.assertNotIn("verifier_actor", head)

    def test_the_verifier_stays_in_the_roster_when_not_buildable(self):
        """Released is not the same as dropped — it is still the class's named
        reference unit, so it survives the buildable filter."""
        import inspect
        src = inspect.getsource(P.load_class_rows)
        self.assertIn('reference = protected | {anchor.get("verifier_actor")}', src)
        self.assertIn('actor not in reference', src)


if __name__ == "__main__":
    unittest.main()
