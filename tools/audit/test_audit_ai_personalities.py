import json
import unittest

from audit_ai_personalities import (
    LADDER_TIERS, ROOT, ZERO_ALLOWLIST, check_ladder_row, difficulty_scale_failures, ladder_failures, on_line, whole_values)
from miniyaml import Ruleset, load_text

# LADDER-AUDIT-02 LOW 3 (Code Monkey): the audit runs end to end on the REAL ai.yaml. These are the rows that are still open
# on master 7c6ba7a97 (LADDER-FIX owns clearing them). The test is a ratchet in both directions: a NEW failing row fails it, and
# so does a row that got fixed but is still listed here. When LADDER-FIX clears the last row this set is empty and the test
# asserts a fully clean audit.
KNOWN_OPEN_ROWS = {
    "Player.BotLimits.AdaptiveCounterWeight has a zero endpoint without a cited allowance",
    "Player.BotLimits.BuildingDelayModifier has a zero endpoint without a cited allowance",
    "Player.BotLimits.BuildingIntervalModifier has a zero endpoint without a cited allowance",
    "Player.BotLimits.InitialAttackDelay has a zero endpoint without a cited allowance",
    "Player.BotLimits.PrioritizeBarracksBeforeRefinery missing on noob, easiest, veryeasy, easy, medium",
    "Player.BotLimits.RetreatRatioPct has a zero endpoint without a cited allowance",
    "Player.BotLimits.UnitDelayModifier has a zero endpoint without a cited allowance",
    "Player.BotLimits.UnitIntervalModifier has a zero endpoint without a cited allowance",
}


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

    def test_suffixed_forms_that_are_not_tier_names_are_flagged_in_a_ladder_family(self):
        rows = ["Player:"] + [f"\tBotLimits@{tier}:\n\t\tRefineryLimit: {2 + rank}" for rank, tier in enumerate(LADDER_TIERS)]
        clean = "\n".join(rows)
        self.assertEqual(ladder_failures(load_text(clean), {}), [])
        typo = clean + "\n\tBotLimits@hrad:\n\t\tRefineryLimit: 9"
        self.assertEqual(ladder_failures(load_text(typo), {}),
                         ["Player.BotLimits has suffixed forms that are not tier names: ['hrad']"])
        extra = clean + "\n\tBotLimits@brutalbot2:\n\t\tRefineryLimit: 9"
        self.assertEqual(ladder_failures(load_text(extra), {}),
                         ["Player.BotLimits has suffixed forms that are not tier names: ['brutalbot2']"])

    def test_families_without_tier_forms_and_exempt_families_are_left_alone(self):
        text = ("Player:\n\tExample@a:\n\t\tValue: 1\n\tExample@b:\n\t\tValue: 2\n"
                "\tProvidesPrerequisite@botplayer:\n\t\tPrerequisite: botplayer\n"
                "\tProvidesPrerequisite@anything:\n\t\tPrerequisite: x\n"
                "\tModularBot@HardAI:\n\t\tName: x\n")
        self.assertEqual(ladder_failures(load_text(text), {}), [])

    def test_non_integer_tier_values_fail_instead_of_riding_the_half_step_tolerance(self):
        self.assertTrue(on_line([0, 1, 2, 3]))
        self.assertTrue(on_line([0, 1, 1, 2, 2, 3]))  # whole numbers still get the 0.5 rounding tolerance (0, .6, 1.2, 1.8, 2.4, 3)
        self.assertFalse(on_line([0, 1.5, 3]))        # exactly on the line, but fractional: never accepted
        values = [str(rank) for rank in range(1, 12)]
        values[3] = "4.5"
        failures = check_ladder_row("Player.Example.Rate", values, {})
        self.assertEqual(len(failures), 1)
        self.assertIn("non-integer tier values ['4.5']", failures[0])

    def test_whole_values_reports_instead_of_crashing_or_truncating(self):
        failures = []
        self.assertEqual(whole_values("X.y", ["1", " 2 ", "3"], failures), [1, 2, 3])
        self.assertEqual(failures, [])
        self.assertIsNone(whole_values("X.y", ["1", "2.5"], failures))
        self.assertIsNone(whole_values("X.z", ["1", None], failures))
        self.assertIsNone(whole_values("X.w", ["1", "abc"], failures))
        self.assertEqual(len(failures), 3)
        self.assertIn("non-integer", failures[0])
        self.assertIn("non-numeric", failures[1])


class RealAiYamlTest(unittest.TestCase):
    """End to end on the real rules (LADDER-AUDIT-02 LOW 3): the allowlist and the difficulty scale audit against ai.yaml."""

    @classmethod
    def setUpClass(cls):
        cls.rules = Ruleset(ROOT)
        cls.player = cls.rules.resolve("player")
        cls.behavior = cls.rules.resolve("^BotProductionBehavior")
        cls.allowlist = json.loads(ZERO_ALLOWLIST.read_text(encoding="utf-8"))

    def test_every_allowlist_entry_cites_a_reason_and_is_live(self):
        without = set(ladder_failures((self.player, self.behavior), {}))
        for path, reason in self.allowlist.items():
            self.assertTrue(reason.strip(), f"{path}: empty allowance")
            self.assertIn(f"{path} has a zero endpoint without a cited allowance", without,
                          f"{path} is allowlisted but is no longer a zero-endpoint row: remove the stale entry")
        with_list = set(ladder_failures((self.player, self.behavior), self.allowlist))
        self.assertEqual(without - with_list, {f"{path} has a zero endpoint without a cited allowance" for path in self.allowlist})

    def test_difficulty_scale_on_real_ai_yaml_matches_the_known_open_rows(self):
        failures = set(difficulty_scale_failures())
        self.assertEqual(failures - KNOWN_OPEN_ROWS, set(), "new failing rows on the real ai.yaml")
        self.assertEqual(KNOWN_OPEN_ROWS - failures, set(), "row cleared: remove it from KNOWN_OPEN_ROWS in the same change set")


if __name__ == "__main__":
    unittest.main()
