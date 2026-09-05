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
    r = {"actor": actor, "cost": cost, "per_wh": per_wh, "n_wh": n_wh, "hp": 10000,
         "spd": 60, "hp_step": 1000, "spd_step": 1,
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


class ThereIsNoVerifier(unittest.TestCase):
    """Maintainer ruling 2026-08-29: *"we no longer have to have those verifiers —
    they should be regular units like anything else and not have those stiff
    rules."*

    A second actor used to be frozen alongside the anchor as a 2.5x cost0
    calibration point. Measured across 23 classes, freezing it moved the other
    members' worst |Δ| by 0.0 in 17 of them, only 8 of 23 sat at the 2.5x the law
    names, and — because exempt rows are excluded from the report's worst-|Δ| line
    — a verifier off by 3779.9 credits was invisible in the report meant to catch
    exactly that.
    """

    def test_only_the_anchor_is_protected(self):
        import inspect
        src = inspect.getsource(P.load_class_rows)
        head = src[src.index("protected = "):src.index("protected.discard(None)")]
        self.assertIn("anchor_actor", head)
        self.assertNotIn("verifier_actor", head)

    def test_no_code_path_reads_a_verifier_any_more(self):
        """A dead knob that LOOKS like it enforces a law is worse than no knob —
        it answers "is this handled?" with a lie. Same reason `spd_step` and
        `VEHICLE_TYPE_CLASSES` were removed."""
        src = pathlib.Path(P.__file__).read_text(encoding="utf-8")
        code = [ln for ln in src.splitlines()
                if 'verifier_actor' in ln and not ln.lstrip().startswith("#")]
        self.assertEqual(code, [])

    def test_the_anchor_file_no_longer_carries_the_field(self):
        anchors = P.load_anchors()
        carriers = [c for c, a in anchors.items()
                    if isinstance(a, dict) and "verifier_actor" in a]
        self.assertEqual(carriers, [])


class StatGridsComeFromOneRegistry(unittest.TestCase):
    """The grids were literals scattered across the quantisers and three had
    drifted from the law by 2026-08-29 — HP quantised at 1000 for EVERY class
    against a law of 2500 for vehicles, the Speed probe reaching 0 of 168
    aircraft, and a class-level `spd_step` nothing read."""

    def test_documented_steps(self):
        self.assertEqual(formula.stat_step("hp", "infantry"), 1000)
        self.assertEqual(formula.stat_step("hp", "vehicle"), 2500)
        self.assertEqual(formula.stat_step("speed", "infantry"), 1)
        self.assertEqual(formula.stat_step("speed", "vehicle"), 5)
        self.assertEqual(formula.stat_step("range"), 10)
        self.assertEqual(formula.stat_step("damage"), formula.DAMAGE_STEP)
        self.assertEqual(formula.stat_step("cost"), 10)

    def test_every_grid_cites_its_source(self):
        for stat, grids in formula.STAT_GRIDS.items():
            for platform, (step, source) in grids.items():
                self.assertTrue(source.strip(), f"{stat}/{platform} has no citation")
                self.assertGreater(step, 0)

    def test_an_unknown_stat_raises_rather_than_defaulting(self):
        """A quantiser reaching for a grid that does not exist is a bug."""
        with self.assertRaises(KeyError):
            formula.stat_step("morale")

    def test_aircraft_take_the_speed_5_grid_without_a_turn_rate(self):
        """NOT ONE of the 168 aircraft in the tree defines Mobile.TurnSpeed, so
        the turn-rate probe alone stepped them by 1 against a law that says 5."""
        self.assertEqual(formula.speed_platform("aircraft", None), "vehicle")
        self.assertEqual(formula.speed_platform("naval", None), "vehicle")

    def test_a_droid_filed_under_infantry_drives_by_5_and_heals_by_1000(self):
        """The two grids key off DIFFERENT things and collapsing them is a real,
        measurable error: Speed's step is turn rate (locomotion), HP's step is
        self-heal (unit kind). Keying HP off locomotion put
        `futuretech_scoutdroid` on the 2500 grid and pushed `scout` from worst
        |Δ| 22.8 to 32.1 on its own."""
        self.assertEqual(formula.speed_platform("infantry", 100), "vehicle")
        self.assertEqual(formula.hp_platform("infantry"), "infantry")
        self.assertEqual(formula.stat_step("speed", formula.speed_platform("infantry", 100)), 5)
        self.assertEqual(formula.stat_step("hp", formula.hp_platform("infantry")), 1000)

    def test_foot_infantry_takes_both_infantry_grids(self):
        self.assertEqual(formula.speed_platform("infantry", None), "infantry")
        self.assertEqual(formula.hp_platform("vehicles"), "vehicle")

    def test_a_class_may_override_only_its_hp_grid(self):
        """HP is the one grid whose step is a tuning judgement rather than a
        mechanical consequence, so a class may override it. `scout_vehicle` is on
        the infantry grid by maintainer ruling 2026-08-29 even though it drives on
        the vehicle SPEED grid."""
        self.assertEqual(formula.hp_platform("vehicles", "scout_vehicle"), "infantry")
        self.assertEqual(formula.stat_step("hp", formula.hp_platform("vehicles", "scout_vehicle")), 1000)
        self.assertEqual(formula.stat_step("speed", formula.speed_platform("vehicles", None)), 5)
        # an unlisted vehicle class keeps its section's grid
        self.assertEqual(formula.hp_platform("vehicles", "mbt"), "vehicle")

    def test_hp_is_snapped_to_its_grid_not_merely_stepped_by_it(self):
        """The pass only STEPPED HP by the grid when breaking a tie, so a value
        that was never tied kept whatever off-grid number it had — 7 of the 28
        scout vehicles sat on 22500/27500/37500 against a 1000 grid."""
        rows = [row("a", 100, 800, 0.05, hp=22500, hp_step=1000, spd=60, spd_step=1),
                row("b", 100, 800, 0.05, hp=27500, hp_step=2500, spd=61, spd_step=5)]
        P.nudge_hp_spd(rows)
        self.assertEqual(rows[0]["hp"] % 1000, 0)
        self.assertEqual(rows[1]["hp"] % 2500, 0)


class FrozenRowsStillOccupyTheirSlot(unittest.TestCase):
    """Maintainer 2026-08-30: *"give each of the scouts their own unique damage
    numbers."* Protected and soft rows used to be filtered out of the collision
    set entirely, so a movable member could be handed the damage the ANCHOR
    already had — `naxis_naxiriflerecruit` and `naxis_naxiriflesoldier` both sat on
    4000, the only collision left in `scout`, precisely because the second is the
    anchor. Not moving a row and not seeing it are different things."""

    def test_a_movable_row_cannot_take_the_anchors_damage(self):
        anchor = row("anchor", 100, 800, 0.05, protected=True)
        free = row("free", 100, 800, 0.05)
        P.unique_dmg_per_shot([anchor, free], price_of=linear_price(2.5))
        self.assertEqual(anchor["dmg_eff"], 800)
        self.assertNotEqual(free["dmg_eff"], 800)

    def test_soft_rows_block_too(self):
        soft = row("spawn", 100, 800, 0.05, soft=True)
        free = row("free", 100, 800, 0.05)
        P.unique_dmg_per_shot([soft, free], price_of=linear_price(2.5))
        self.assertNotEqual(free["dmg_eff"], 800)

    def test_the_greedy_fallback_blocks_them_as_well(self):
        """No price objective -> the nearest-free-slot walk, which must honour
        the same frozen slots."""
        anchor = row("anchor", 100, 800, 0.05, protected=True)
        free = row("free", 100, 800, 0.05)
        P.unique_dmg_per_shot([anchor, free])
        self.assertNotEqual(free["dmg_eff"], 800)


if __name__ == "__main__":
    unittest.main()
