"""Tests for tools/balance/k_price_delta.py — the PRICING-DEFAULT delta report."""
import _bootstrap  # noqa: F401  (sys.path setup)
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "balance"))

import k_price_delta as kpd  # noqa: E402


def arm(**kw):
    return kw


class DominantFactorTest(unittest.TestCase):
    def test_accuracy_is_named_when_dominant(self):
        rows = kpd.dominant_factor([arm(reliability=0.06, footprint=0.5,
                                        factor_range=1.0, effective_dps=100.0)])
        self.assertEqual(rows[0][1], "accuracy")
        self.assertAlmostEqual(rows[0][2], 0.06)

    def test_splash_is_named_on_flat_accuracy(self):
        rows = kpd.dominant_factor([arm(reliability=1.0, footprint=4.2,
                                        effective_dps=50.0)])
        self.assertEqual(rows[0][1], "splash")
        self.assertAlmostEqual(rows[0][2], 4.2)

    def test_runner_up_joins_when_comparable(self):
        rows = kpd.dominant_factor([arm(overkill=0.5, footprint=3.0,
                                        reliability=1.0, effective_dps=10.0)])
        labels = [r[1] for r in rows]
        self.assertEqual(len(rows), 2)
        self.assertIn("splash", labels)
        self.assertIn("overkill", labels)

    def test_same_label_runner_up_is_skipped(self):
        darms = [
            arm(reliability=0.3, factor_range=1.4, effective_dps=60.0),
            arm(reliability=0.5, factor_range=1.0, effective_dps=40.0),
        ]
        rows = kpd.dominant_factor(darms)
        labels = [r[1] for r in rows]
        self.assertEqual(len(labels), len(set(labels)),
                         f"duplicate reason label in {labels}")

    def test_weak_secondary_never_names_reason(self):
        darms = [
            arm(reliability=1.0, factor_range=1.02, effective_dps=990.0),
            arm(reliability=0.001, effective_dps=10.0),  # 1% of the unit's dps
        ]
        rows = kpd.dominant_factor(darms)
        self.assertTrue(all(r[1] != "accuracy" or r[2] > 0.5 for r in rows))

    def test_flat_armament_returns_no_reason(self):
        self.assertEqual(kpd.dominant_factor([arm(reliability=1.0)]), [])


class ReasonTextTest(unittest.TestCase):
    def test_missing_reason_shows_fallback_or_mixed(self):
        self.assertEqual(kpd.reason_text({"reasons": [], "fb": 1}), "raw fallback")
        self.assertEqual(kpd.reason_text({"reasons": [], "fb": 0}),
                         "mixed small factors")

    def test_tiny_value_prints_bound_not_zero(self):
        row = {"reasons": [(3.0, "accuracy", 0.001,
                            "scatter/travel miss chance at point targets")]}
        self.assertIn("<0.01", kpd.reason_text(row))


class Movers25Test(unittest.TestCase):
    def row(self, **kw):
        base = {"actor": "a1", "faction": "td_gdi", "cls": "mbt",
                "cost": 800.0, "raw": 800.0, "k": 400.0, "delta": -400.0,
                "pct": -0.5, "reasons": [], "fb": 0, "prov": False}
        base.update(kw)
        return base

    def test_only_big_moves_are_listed(self):
        big = self.row()
        small = self.row(actor="a2", k=780.0, pct=-0.025)
        text = "\n".join(kpd.render_movers_25([big, small]))
        self.assertIn("`a1`", text)
        self.assertNotIn("`a2`", text)
        self.assertIn("(1 actors)", text)

    def test_unexplained_move_is_flagged(self):
        text = "\n".join(kpd.render_movers_25([self.row()]))
        self.assertIn("UNEXPLAINED", text)

    def test_explained_move_carries_term_not_flag(self):
        row = self.row(reasons=[(2.0, "accuracy", 0.1, "miss chance")])
        text = "\n".join(kpd.render_movers_25([row]))
        self.assertIn("accuracy", text)
        self.assertNotIn("UNEXPLAINED", text)


class SpecPriceTest(unittest.TestCase):
    def test_anchor_prices_exactly_cost0(self):
        spec = dict(hp0=100, speed0=50, range0_wdist=5000, dps0=60, cost0=800)
        inp = (100, 50, 5000, 60, 1.0, 1.0, 1.0)  # identical stats -> price == cost0
        self.assertAlmostEqual(kpd.spec_price(inp, spec, 1.0), 800.0, places=1)

    def test_ability_priced_class_returns_none(self):
        spec = dict(hp0=100, speed0=50, range0_wdist=0, dps0=0, cost0=500)
        self.assertIsNone(kpd.spec_price((100, 50, 0, 0, 1, 1, 1), spec, 1.0))

    def test_k_input_moves_price_monotone(self):
        spec = dict(hp0=100, speed0=50, range0_wdist=5000, dps0=60, cost0=800)
        raw = (100, 50, 5000, 60, 1.0, 1.0, 1.0)
        ked = (100, 50, 5000, 30, 1.0, 1.0, 1.0)  # K halves the measured dps
        self.assertLess(kpd.spec_price(ked, spec, 1.0),
                        kpd.spec_price(raw, spec, 1.0))


if __name__ == "__main__":
    unittest.main()
