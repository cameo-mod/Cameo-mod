"""A3 registry-load test: BW_observed_air_threats arms ObservedAirThreats for genericbot only."""
import pathlib
import re
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/ai"))
import apply_increment_switches as switches

GROUP = "BW_observed_air_threats"


class ObservedAirThreatsRegistryTests(unittest.TestCase):
    def test_group_loads_and_arms_genericbot_through_switch_condition(self):
        _, groups, _ = switches.load_spec(switches.SPEC)
        self.assertEqual(groups[GROUP],
                         {"UnitBuilderBotModuleCA": {"SwitchCondition": "genericbot",
                                                     "ObservedAirThreats": "true"}})

    def test_group_is_one_new_restraint(self):
        _, groups, _ = switches.load_spec(switches.SPEC)
        classes = switches.load_effect_classes(switches.SPEC, groups)
        self.assertEqual(classes[GROUP], "restraint")
        self.assertEqual(switches.restraint_budget([GROUP], classes)["new_restraint_count"], 1)

    def test_default_stays_off_in_yaml_and_info(self):
        info = (ROOT / "OpenRA.Mods.CA/Traits/BotModules/UnitBuilderBotModuleCA.cs").read_text(encoding="utf-8")
        self.assertIn("readonly bool ObservedAirThreats = false;", info)
        ai = (ROOT / "mods/cameo/ai/ai.yaml").read_text(encoding="utf-8")
        blocks = re.findall(r"^\tUnitBuilderBotModuleCA@generic:\n((?:\t\t[^\n]*\n)+)", ai, re.M)
        self.assertEqual(len(blocks), 1)
        self.assertIn("#ObservedAirThreats: false", blocks[0])
        self.assertNotRegex(blocks[0], r"^\t\tObservedAirThreats:", "no live override — the switch arms it")
        for yaml in (ROOT / "mods/cameo/ai").glob("*.yaml"):
            self.assertNotRegex(yaml.read_text(encoding="utf-8"), r"ObservedAirThreats:\s*true")


if __name__ == "__main__":
    unittest.main()
