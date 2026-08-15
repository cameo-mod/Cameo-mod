"""W17 — `FirepowerMultiplier` is retired as a fine-tuning knob.

The knob existed to absorb the remainder left by a coarse damage grid. The grid
is now 20x finer (2000 -> 100), so the pipeline solves Damage exactly and must
neither PRESCRIBE a multiplier nor WRITE one. These tests pin both halves, plus
the property that made the retirement safe in the first place.

Conditional (upgrade) FirepowerMultiplier traits are design and are untouched —
nothing here asserts anything about them.
"""
import pathlib
import sys
import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))
import apply_balance  # noqa: E402
import formula  # noqa: E402
import propose_class_rebalance as pcr  # noqa: E402


class DecomposeSolvesExactly(unittest.TestCase):
    """The prescription is Damage alone; the second return value is always 1.0."""

    def test_never_prescribes_a_multiplier(self):
        for target in (10.0, 137.5, 1000.0, 12345.6):
            with self.subTest(target=target):
                _, fp = pcr.decompose_dps(target, base_dps=100.0, cur_sum=2000.0, n_wh=1)
                self.assertEqual(fp, 1.0)

    def test_dead_ends_also_return_unity(self):
        # No positive DPS prices the unit: park at the grid floor, never at a
        # 0.05 multiplier of a 2000 main (the old `2000, 0.05` dead-end).
        for bad in (dict(base_dps=0.0, cur_sum=2000.0),
                    dict(base_dps=100.0, cur_sum=0.0)):
            with self.subTest(**bad):
                D, fp = pcr.decompose_dps(1000.0, n_wh=1, **bad)
                self.assertEqual(fp, 1.0)
                self.assertEqual(D, formula.DAMAGE_STEP)

    def test_floor_is_unchanged_by_the_retirement(self):
        """The old dead-end delivered 2000 x 0.05 = 100 effective damage; one
        grid step at fp=1 is also 100. Retiring the knob did not raise the
        weakest weapon the pipeline can express."""
        D, fp = pcr.decompose_dps(0.0, 100.0, 2000.0, 1)
        self.assertAlmostEqual(D * fp, 2000 * 0.05)

    def test_lands_on_the_grid(self):
        for target in (1.0, 55.0, 617.3, 9001.0):
            D, _ = pcr.decompose_dps(target, base_dps=100.0, cur_sum=2000.0, n_wh=3)
            with self.subTest(target=target):
                self.assertEqual(D % formula.DAMAGE_STEP, 0)
                self.assertGreaterEqual(D, formula.DAMAGE_STEP)

    def test_solves_the_target_within_half_a_step(self):
        """Why the knob is not needed: above the floor the residual is bounded
        by half a grid step per warhead, not by the old 2000-damage half-step."""
        base_dps, cur_sum, n_wh = 100.0, 2000.0, 2
        per_unit = base_dps / cur_sum
        half_step = per_unit * formula.DAMAGE_STEP * n_wh / 2
        for target in (41.9, 250.5, 1234.5):
            D, fp = pcr.decompose_dps(target, base_dps, cur_sum, n_wh)
            achieved = per_unit * D * n_wh * fp
            with self.subTest(target=target):
                self.assertLessEqual(abs(achieved - target), half_step + 1e-9)

    def test_a_target_under_the_floor_clamps_up_and_does_not_underflow(self):
        """The floor is not a rounding error to hide. A target weaker than one
        grid step across the weapon's warheads cannot be expressed, so the
        solver clamps UP to one step rather than emitting 0 — a 0 main warhead
        is not "a little damage", it is a weapon that does nothing."""
        base_dps, cur_sum, n_wh = 100.0, 2000.0, 2
        per_unit = base_dps / cur_sum
        floor_dps = per_unit * formula.DAMAGE_STEP * n_wh
        D, fp = pcr.decompose_dps(floor_dps / 3, base_dps, cur_sum, n_wh)
        self.assertEqual((D, fp), (formula.DAMAGE_STEP, 1.0))
        self.assertGreater(per_unit * D * n_wh * fp, 0)


class UniquenessNudgesDamageNotTheKnob(unittest.TestCase):

    def _row(self, actor, per_wh, n_wh=1):
        return {"actor": actor, "protected": False, "soft": False,
                "per_wh": per_wh, "n_wh": n_wh, "dmg_shot": per_wh * n_wh,
                "dmg_eff": per_wh * n_wh, "per_unit": 0.01, "fp": 1.0, "fp0": 1.0}

    def test_collision_is_broken_by_moving_damage(self):
        rows = [self._row("a", 2000), self._row("b", 2000), self._row("c", 2000)]
        pcr.unique_dmg_per_shot(rows)
        self.assertEqual(len({r["dmg_eff"] for r in rows}), 3)
        for r in rows:
            self.assertEqual(r["per_wh"] % formula.DAMAGE_STEP, 0)
            self.assertEqual(r["fp"], 1.0, "uniqueness must not touch the knob")

    def test_damage_never_nudges_below_one_step(self):
        rows = [self._row(str(i), formula.DAMAGE_STEP) for i in range(4)]
        pcr.unique_dmg_per_shot(rows)
        for r in rows:
            self.assertGreaterEqual(r["per_wh"], formula.DAMAGE_STEP)

    def test_protected_rows_keep_their_legacy_multiplier(self):
        """A protected row is the yaml's own state — including a real FP that
        still sits on the actor. The pass must not restate it."""
        keep = self._row("anchor", 2000)
        keep.update(protected=True, fp=0.5, fp0=0.5, dmg_eff=1000)
        rows = [keep, self._row("member", 2000)]
        pcr.unique_dmg_per_shot(rows)
        self.assertEqual(keep["fp"], 0.5)
        self.assertEqual(keep["per_wh"], 2000)


class WritePathIsClosed(unittest.TestCase):

    def test_firepower_is_not_an_appliable_field(self):
        self.assertNotIn("firepower_multiplier", apply_balance.UNIT_FIELDS)
        self.assertIn("firepower_multiplier", apply_balance.RETIRED_UNIT_FIELDS)

    def test_no_trait_block_can_be_minted(self):
        """`set_field` used to INSERT a whole `FirepowerMultiplier:` block on an
        actor that had none — the one path that could create the knob from
        nothing. Applying to an actor without the trait must now be a no-op."""
        src = "\n".join(["someactor:", "\tHealth:", "\t\tHP: 100", ""])
        tmp = ROOT / "docs" / "balance" / "_test_apply_balance_tmp.yaml"
        tmp.write_text(src, encoding="utf-8")
        try:
            ed = apply_balance.YamlEditor(tmp)
            res = ed.set_field("someactor", "FirepowerMultiplier", "Modifier", 89)
            self.assertIn("not written locally", res)
            self.assertNotIn("FirepowerMultiplier", "\n".join(ed.lines))
        finally:
            tmp.unlink(missing_ok=True)


class ReportTellsYouToDeleteTheTrait(unittest.TestCase):
    """Damage is solved at fp=1, so a surviving multiplier would apply twice.
    The report must say so rather than silently leaving the debt."""

    def test_prescription_orders_the_deletion(self):
        rows = [{
            "actor": "legacy_unit", "faction": "f", "hp": 1, "spd": 1, "rng": 1,
            "cost": 100, "rl": 20, "burst": 1, "dmg": 0, "per_wh": 2000,
            "n_wh": 1, "dmg_shot": 2000, "dmg_eff": 2000, "dps_eff": 1.0,
            "price": 100, "delta": 0.0, "note": "", "protected": False,
            "fp": 1.0, "fp0": 0.5,
        }]
        try:
            out = pcr.render_report(rows, "mbt")
        except (KeyError, FileNotFoundError) as exc:      # no anchor spec here
            self.skipTest(f"render_report needs class_anchors.json: {exc}")
        self.assertIn("DELETE the unconditional FirepowerMultiplier", out)
        self.assertIn("fp-debt", out)
        self.assertNotIn("FirepowerMultiplier@LEGACYUNIT", out)


if __name__ == "__main__":
    sys.exit(unittest.main())
