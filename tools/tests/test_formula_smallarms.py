"""Regression test for the small-arms tag filter (formula.is_smallarms_tag).

⛔ THE BUG THIS PINS. `spread_damage_sum(..., smallarms_only=True)` used to test
`tag.startswith("smallarms")`. The 3-way split renamed warhead tags to FAMILY
names, so a rifle that was `SmallArmsWarhead` became `Bullet_Light` and only 120
of 7618 damage warheads still carried the legacy string. The filter therefore
matched NOTHING for every scout under the 1.5x cost0 threshold: the sum returned
0, and `propose_class_rebalance` reported eff DPS 0.0 for 15 of the 24 scouts and
priced them at 32-63 against costs of 100-200.

The data was never wrong — reload 50, damage 4000, plainly in the ledger. A
literal string in a filter went stale under a migration, silently, and the only
symptom was a number that looked like a balance problem.
"""

import pathlib
import sys
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "balance"))

import formula  # noqa: E402


class SmallArmsTagSurvivesTheSplit(unittest.TestCase):
    def test_post_split_family_tags_count_as_small_arms(self):
        """`Bullet_*` is what a rifle is called AFTER the 3-way split."""
        for tag in ("Bullet_Light", "Bullet_Medium",
                    "Bullet_LightFlatCompatibility"):
            self.assertTrue(formula.is_smallarms_tag(tag), tag)

    def test_legacy_tags_still_count(self):
        """120 warheads still carry the pre-split name; they must keep pricing."""
        for tag in ("SmallArmsWarhead", "smallarms", "SmallArms_Light"):
            self.assertTrue(formula.is_smallarms_tag(tag), tag)

    def test_other_families_are_excluded(self):
        """The rule exists to price a cheap scout on its RIFLE, not its grenade."""
        for tag in ("CannonHE_Heavy", "Demolition_Light", "MissileAP_Medium",
                    "Tesla_Heavy", "Sniper_Light"):
            self.assertFalse(formula.is_smallarms_tag(tag), tag)

    def test_missing_tag_is_not_small_arms(self):
        self.assertFalse(formula.is_smallarms_tag(None))
        self.assertFalse(formula.is_smallarms_tag(""))

    def test_a_bullet_warhead_actually_survives_the_sum(self):
        """End to end: the filter must not zero a real post-split rifle."""
        warheads = [{"tag": "Bullet_Light", "damage": "4000",
                     "type": "SpreadDamage"}]
        got = formula.spread_damage_sum(warheads, smallarms_only=True)
        self.assertEqual(got, 4000.0,
                         "a Bullet_Light rifle priced as 0 — the stale-string "
                         "bug is back")


if __name__ == "__main__":
    unittest.main()
