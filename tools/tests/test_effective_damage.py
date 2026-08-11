"""Unit tests for tools/balance/effective_damage.py.

Pins the parts of the metric that must track the ENGINE rather than taste: the
falloff geometry (including the single-`Range` footgun that makes a warhead deal
zero damage) and the scatter model used for hit reliability.
"""

from __future__ import annotations

import math
import pathlib
import sys
import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[2] / "tools" / "balance"))
import effective_damage as ed  # noqa: E402


class Node:
    """Minimal stand-in for a resolved yaml node (only `get` is used)."""

    def __init__(self, fields):
        self.fields = fields

    def get(self, key):
        return self.fields.get(key)


class ParseTest(unittest.TestCase):
    def test_plain_and_cell_notation(self):
        self.assertEqual(ed.parse_wdist("512"), 512)
        self.assertEqual(ed.parse_wdist("1c0"), 1024)
        self.assertEqual(ed.parse_wdist("1c512"), 1536)

    def test_negative_and_averaged_ranges(self):
        self.assertEqual(ed.parse_wdist("-256"), -256)
        self.assertEqual(ed.parse_wdist("180, 240"), 210)


class GeometryTest(unittest.TestCase):
    def test_spread_grid_when_no_range(self):
        fo, radii, live = ed.falloff_and_radii(Node({"Falloff": "100, 50, 0"}), 400)
        self.assertTrue(live)
        self.assertEqual((fo, radii), ([100, 50, 0], [0, 400, 800]))

    def test_explicit_ring_list_is_used_verbatim(self):
        fo, radii, live = ed.falloff_and_radii(Node({"Range": "0, 32", "Falloff": "100, 50"}), 500)
        self.assertTrue(live)
        self.assertEqual(radii, [0, 32])

    def test_single_range_is_a_dead_warhead(self):
        """Engine: effectiveRange has 1 entry -> GetDamageFalloff always returns 0."""
        _fo, _radii, live = ed.falloff_and_radii(
            Node({"Range": "500", "Falloff": "111, 33, 11, 3"}), 500)
        self.assertFalse(live)

    def test_footprint_grows_with_spread(self):
        small = ed.footprint_cells2([100, 0], [0, 400])
        big = ed.footprint_cells2([100, 0], [0, 800])
        self.assertGreater(big, small)

    def test_footprint_of_a_flat_disc_matches_the_circle_area(self):
        """Falloff 100,100 over 0..1024 is a full-strength disc of radius 1 cell."""
        self.assertAlmostEqual(ed.footprint_cells2([100, 100], [0, 1024]), math.pi, places=4)


class ReliabilityTest(unittest.TestCase):
    FO = [100, 50, 25, 10, 0]
    RADII = [0, 400, 800, 1200, 1600]

    def test_perfect_accuracy_is_one(self):
        self.assertEqual(ed.reliability(self.FO, self.RADII, 0), 1.0)

    def test_reliability_falls_as_scatter_grows(self):
        values = [ed.reliability(self.FO, self.RADII, s) for s in (100, 400, 800, 1600, 3200)]
        self.assertEqual(values, sorted(values, reverse=True))

    def test_reliability_stays_a_fraction(self):
        for sigma in (0, 50, 500, 5000, 50000):
            self.assertTrue(0.0 <= ed.reliability(self.FO, self.RADII, sigma) <= 1.0)

    def test_scatter_matches_the_engine_two_axis_triangular_pdf(self):
        """Mean miss radius must be ~0.52*sigma, NOT the 0.67 of a uniform disc."""
        step = 0.001
        mean = sum(t * ed.scatter_pdf(t) for t in
                   [i * step for i in range(1, int(math.sqrt(2) / step))]) * step
        self.assertAlmostEqual(mean, 0.52, delta=0.02)

    def test_scatter_pdf_is_normalised(self):
        step = 0.001
        total = sum(ed.scatter_pdf(t) for t in
                    [i * step for i in range(0, int(math.sqrt(2) / step))]) * step
        self.assertAlmostEqual(total, 1.0, delta=0.02)

    def test_scatter_pdf_is_zero_outside_its_support(self):
        self.assertEqual(ed.scatter_pdf(-0.1), 0.0)
        self.assertEqual(ed.scatter_pdf(math.sqrt(2) + 0.1), 0.0)


class DamageValueTest(unittest.TestCase):
    def test_numeric_forms_parse(self):
        self.assertEqual(ed.damage_value("2000"), 2000)
        self.assertEqual(ed.damage_value(2000), 2000)
        self.assertEqual(ed.damage_value("2000.7"), 2000)

    def test_non_numeric_is_none_not_an_exception(self):
        for bad in (None, "", "abc", "inherit"):
            self.assertIsNone(ed.damage_value(bad))


if __name__ == "__main__":
    unittest.main()
