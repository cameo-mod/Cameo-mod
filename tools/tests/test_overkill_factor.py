"""MT-OVERKILL: an exact-multiple shot must not gain a phantom shot to float noise."""

from __future__ import annotations

import pathlib
import sys
import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))

import weapon_efficiency as we  # noqa: E402


class OverkillFactorTests(unittest.TestCase):
    def test_exact_multiple_with_float_noise_is_not_overkill(self):
        # LeechJump / dog bite: 100000 damage, reference HP 200000 -> exactly 2 shots -> 1.0.
        # The Python per-shot value is 99999.99999999999; without rounding ceil gave 3 shots.
        self.assertAlmostEqual(we.overkill_factor(99999.99999999999, 200000), 1.0, places=9)


if __name__ == "__main__":
    unittest.main()
