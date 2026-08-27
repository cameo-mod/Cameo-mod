"""Regression tests for the whole-tree weapon behavior comparator."""

from __future__ import annotations

import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

import review_batch_diff as review


def weapon(**overrides):
    value = {
        "damage": 1000,
        "percentage_damage": {20: 0, 200_000: 2000, 3_750_000: 37_500},
        "shape": ["100|-|-|-"],
        "valid_target_damage": (("Ground", 1000),),
        "invalid_target_damage": (("wall", 1000),),
        "Range": "5000",
        "ReloadDelay": "25",
        "Burst": None,
        "ValidTargets": "Ground",
        "InvalidTargets": "wall",
        "Report": "shot.aud",
        "StartBurstReport": None,
        "top_level": (("Range", "5000", ()),),
        "projectile": ("Projectile", "Bullet", ()),
        "non_damage_warheads": (),
    }
    value.update(overrides)
    return value


class ReviewBatchDiffTests(unittest.TestCase):
    def test_percentage_difference_at_non_reference_health_is_detected(self):
        before = weapon()
        after = weapon(percentage_damage={20: 0, 200_000: 2000, 3_750_000: -10})

        changed, removed, added = review.compare({"Gun": before}, {"Gun": after})

        self.assertEqual(removed, [])
        self.assertEqual(added, [])
        self.assertIn(["percentage_damage", [[3_750_000, 37_500, -10]]], changed["Gun"])

    def test_projectile_and_non_damage_changes_are_detected(self):
        before = weapon()
        after = weapon(
            projectile=("Projectile", "Bullet", (("Blockable", "true", ()),)),
            non_damage_warheads=(("Warhead@Effect", "CreateEffect", ()),),
        )

        changed, _, _ = review.compare({"Gun": before}, {"Gun": after})
        kinds = {finding[0] for finding in changed["Gun"]}

        self.assertIn("projectile", kinds)
        self.assertIn("non_damage_warheads", kinds)

    def test_damage_target_exclusion_change_is_detected(self):
        before = weapon()
        after = weapon(invalid_target_damage=())

        changed, _, _ = review.compare({"Gun": before}, {"Gun": after})

        self.assertIn("invalid_target_damage", {finding[0] for finding in changed["Gun"]})


if __name__ == "__main__":
    unittest.main()
