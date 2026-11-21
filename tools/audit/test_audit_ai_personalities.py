import unittest

from audit_ai_personalities import LADDER_TIERS, check_ladder_row, ladder_failures
from miniyaml import load_text


class LadderAuditTest(unittest.TestCase):
    def test_grants_require_uniform_coverage_but_merge_split_grants(self):
        line = "Player:\n\tGrantConditionOnBotOwner@a:\n\t\tCondition: feature\n\t\tBots: "
        partial = line + ", ".join(LADDER_TIERS[1:])
        self.assertEqual(ladder_failures(load_text(partial), {}), ["Player.feature grant missing on noob"])
        full = partial + "\n\tGrantConditionOnBotOwner@b:\n\t\tCondition: feature\n\t\tBots: noob"
        self.assertEqual(ladder_failures(load_text(full), {}), [])

    def test_missing_use_flag_fails(self):
        rows = ["Player:"] + [
            f"\tExample@{tier}:\n\t\tUseExample: true" for tier in LADDER_TIERS[1:]
        ]
        self.assertEqual(
            ladder_failures(load_text("\n".join(rows)), {}),
            ["Player.Example.UseExample missing on noob"],
        )

    def test_nonlinear_and_integer_rounding(self):
        values = [str(2 + (rank + 2) // 5) for rank in range(11)]
        self.assertEqual(check_ladder_row("Player.Example.Count", values, {}), [])
        values[5] = "5"
        self.assertIn("not on one equal-step line", check_ladder_row("Player.Example.Count", values, {})[0])

    def test_zero_endpoint_requires_exact_citation(self):
        path = "Player.Example.Rate"
        values = [str(rank) for rank in range(11)]
        self.assertEqual(check_ladder_row(path, values, {}), [f"{path} has a zero endpoint without a cited allowance"])
        self.assertEqual(check_ladder_row(path, values, {path: "Pinned exception"}), [])

    def test_mixed_boolean_fails_even_with_one_monotonic_transition(self):
        values = ["false"] * 5 + ["true"] * 6
        self.assertIn("inconsistent boolean", check_ladder_row("Player.Example.Enabled", values, {})[0])
        self.assertEqual(check_ladder_row("Player.Example.Enabled", ["true"] * 11, {}), [])


if __name__ == "__main__":
    unittest.main()
