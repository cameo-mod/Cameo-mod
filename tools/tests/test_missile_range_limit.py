"""Resolved-rules contract for projectile range-limit percent.

The standard fuel limit is `RangeLimitPercent: 150` on the missile projectile
templates; deliberately tuned weapons keep a fixed `RangeLimit` which wins when
both resolve (engine `IRangeLimitedProjectileInfo.EffectiveRangeLimit`).
"""

from __future__ import annotations

import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

from cameo_model import Model


class MissileRangeLimitTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rs = Model().rs

    def _projectile(self, weapon_name: str):
        node = self.rs.resolve_weapon(weapon_name)
        self.assertIsNotNone(node)
        proj = node.child("Projectile")
        self.assertIsNotNone(proj)
        return proj

    def test_templates_resolve_percent(self):
        for template in ("^Projectile_Missile_Medium", "^Projectile_Missile_Heavy",
                         "^Projectile_Missile_Heavy_D2K_OMissile", "^AntiAirMissile",
                         "^D2KMissile"):
            proj = self._projectile(template)
            self.assertEqual("150", proj.get("RangeLimitPercent"), template)
            self.assertIsNone(proj.child("RangeLimit"), template)

    def test_standard_consumer_inherits_percent(self):
        # RA2SCUD (range 20000) -> effective fuel 30000 via the Heavy template.
        proj = self._projectile("RA2SCUD")
        self.assertEqual("Missile", proj.value)
        self.assertEqual("150", proj.get("RangeLimitPercent"))
        self.assertIsNone(proj.child("RangeLimit"))

    def test_hand_tuned_exceptions_keep_fixed_limit(self):
        for name, limit in (("mtank_pri2", "45000"),
                            ("RA2SCUDELITE", "30000"),
                            ("oTowerMissile", "6758"),
                            ("PhoenixRocketShrapnel", "33333")):
            proj = self._projectile(name)
            self.assertEqual(limit, proj.get("RangeLimit"), name)

    def test_unlimited_fuel_stays_fixed_negative(self):
        proj = self._projectile("TSTacticalMissile")
        self.assertEqual("-1", proj.get("RangeLimit"))

    def test_fixed_limit_wins_when_both_resolve(self):
        proj = self._projectile("RA2SCUDELITE")
        self.assertEqual("30000", proj.get("RangeLimit"))
        self.assertEqual("150", proj.get("RangeLimitPercent"))


if __name__ == "__main__":
    unittest.main()
