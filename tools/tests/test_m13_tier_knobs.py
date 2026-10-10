"""Maintainer difficulty contract and inert M13 switch checks; no game launches."""
import pathlib
import re
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/ai"))
import apply_increment_switches as switches


class M13TierKnobsTests(unittest.TestCase):
    def test_every_tier_explicitly_writes_linear_delays_and_fixed_capacity(self):
        text = (ROOT / "mods/cameo/ai/ai.yaml").read_text(encoding="utf-8")
        tiers = ["easiest", "veryeasy", "easy", "medium", "hard", "veryhard",
                 "brutal", "challenger", "unbeatable", "god"]
        for index, tier in enumerate(tiers):
            with self.subTest(tier=tier):
                blocks = re.findall(r"^\tBotLimits@" + tier + r":\n((?:\t\t[^\n]*\n)+)", text, re.M)
                self.assertEqual(len(blocks), 1)
                expected = {"AttackLivenessMaxNoLaunchTicks": 9500 - 500 * index,
                            "TeamResponseMaxLeaseTicks": 1900 - 100 * index,
                            "TeamResponseRearmCooldownTicks": 950 - 50 * index,
                            "TeamResponseLeaseLimit": 1}
                for field, value in expected.items():
                    values = re.findall(r"^\t\t" + field + r": (\d+)$", blocks[0], re.M)
                    self.assertEqual(values, [str(value)])
        self.assertIn("RequiresCondition: cameogodbot", blocks[0])

    def test_one_default_off_arm_is_one_new_restraint_and_classic_is_skipped(self):
        skip, groups, _ = switches.load_spec(switches.SPEC)
        classes = switches.load_effect_classes(switches.SPEC, groups)
        self.assertIn("SquadManagerBotModuleCA@classic", skip)
        self.assertEqual(groups["BV_m13_liveness"],
                         {"SquadManagerBotModuleCA": {"AttackLivenessEnabled": "true"}})
        self.assertEqual(switches.restraint_budget(["BV_m13_liveness"], classes)["new_restraint_count"], 1)
        info = (ROOT / "OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.cs").read_text()
        self.assertIn("readonly bool AttackLivenessEnabled = false;", info)
        for yaml in (ROOT / "mods/cameo/ai").glob("*.yaml"):
            self.assertNotRegex(yaml.read_text(encoding="utf-8"), r"AttackLivenessEnabled:\s*true")


if __name__ == "__main__":
    unittest.main()
