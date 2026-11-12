#!/usr/bin/env python3
"""MCV-SAFE-01: the evidence tool accepts the mcv_lost player event (its kind allowlist is fail-closed)."""
from __future__ import annotations

import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))

import replay_health as health  # noqa: E402


class McvLostKindTests(unittest.TestCase):
    def test_both_versions_know_mcv_lost(self):
        self.assertIn("mcv_lost", health.PLAYER_EVENT_KINDS)
        self.assertIn("mcv_lost", health.PLAYER_EVENT_V2_KINDS)

    def test_an_unknown_kind_is_still_not_known(self):
        self.assertNotIn("mcv_gained", health.PLAYER_EVENT_KINDS)


if __name__ == "__main__":
    unittest.main()
