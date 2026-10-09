"""Resolved weapon rules contract for Missile feel-test projectile defaults."""

from __future__ import annotations

import unittest

import _bootstrap  # noqa: F401 — sys.path side effect
from cameo_model import Model


class MissileFeelDefaultsTest(unittest.TestCase):
	@classmethod
	def setUpClass(cls):
		cls.rs = Model().rs

	def projectile(self, name):
		weapon = self.rs.resolve_weapon(name)
		self.assertIsNotNone(weapon, name)
		projectile = weapon.child("Projectile")
		self.assertIsNotNone(projectile, name)
		return projectile

	def test_core_missile_templates_use_percent_speed_threshold_and_snap_off(self):
		for name in ("^LightMissile", "^MediumMissile", "^HeavyMissile",
					 "^Projectile_Missile_Light", "^Projectile_Missile_Medium",
					 "^Projectile_Missile_Heavy"):
			with self.subTest(template=name):
				projectile = self.projectile(name)
				self.assertEqual("150", projectile.get("RangeLimitPercent"))
				self.assertEqual("true", projectile.get("CloseEnoughFromSpeed"))
				self.assertIsNone(projectile.get("SnapImpactToTarget"))
				self.assertIsNone(projectile.child("RangeLimit"))
				self.assertIsNone(projectile.child("CloseEnough"))

	def test_only_aa_projectile_template_enables_snap(self):
		projectile = self.projectile("^Projectile_Missile_AA")
		self.assertEqual("true", projectile.get("SnapImpactToTarget"))
		for name in ("^LightMissile", "^MediumMissile", "^HeavyMissile",
					 "^Projectile_Missile_Light", "^Projectile_Missile_Medium",
					 "^Projectile_Missile_Heavy", "^Projectile_TS_Missile_Medium"):
			with self.subTest(template=name):
				self.assertIsNone(self.projectile(name).get("SnapImpactToTarget"))
		self.assertEqual("true", self.projectile("RA2PatriotThunderboltMissile").get("SnapImpactToTarget"))
		self.assertIsNone(self.projectile("RA2MultiThunderboltMissile").get("SnapImpactToTarget"))

	def test_unlimited_tactical_missiles_keep_negative_percentage_sentinel(self):
		for name in ("TSTacticalMissile", "TSTacticalChemMissile"):
			with self.subTest(weapon=name):
				projectile = self.projectile(name)
				self.assertEqual("-1", projectile.get("RangeLimitPercent"))
				self.assertIsNone(projectile.child("RangeLimit"))

	def test_migrated_weapon_inherits_new_template_fields(self):
		projectile = self.projectile("RA2SCUD")
		self.assertEqual("150", projectile.get("RangeLimitPercent"))
		self.assertEqual("true", projectile.get("CloseEnoughFromSpeed"))
		self.assertIsNone(projectile.child("RangeLimit"))
		self.assertIsNone(projectile.child("CloseEnough"))


if __name__ == "__main__":
	unittest.main()
