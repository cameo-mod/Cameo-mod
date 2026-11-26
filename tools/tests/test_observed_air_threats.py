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

    def test_default_off_in_info_armed_in_generic_yaml_only(self):
        # FOG-AIR-01 (Opus 11-26): the code default stays false; @generic arms it live (row fog_air_observed), @classic keeps the world count.
        info = (ROOT / "OpenRA.Mods.CA/Traits/BotModules/UnitBuilderBotModuleCA.cs").read_text(encoding="utf-8")
        self.assertIn("readonly bool ObservedAirThreats = false;", info)
        ai = (ROOT / "mods/cameo/ai/ai.yaml").read_text(encoding="utf-8")
        generic = re.findall(r"^\tUnitBuilderBotModuleCA@generic:\n((?:\t\t[^\n]*\n)+)", ai, re.M)
        self.assertEqual(len(generic), 1)
        self.assertRegex(generic[0], r"(?m)^\t\tObservedAirThreats: true$")
        classic = re.findall(r"^\tUnitBuilderBotModuleCA@classic:\n((?:\t\t[^\n]*\n)+)", ai, re.M)
        self.assertEqual(len(classic), 1)
        self.assertNotRegex(classic[0], r"(?m)^\t\tObservedAirThreats:", "classic: no live override")
        self.assertIn("#ObservedAirThreats: false", classic[0])
        self.assertEqual(len(re.findall(r"ObservedAirThreats:\s*true", ai)), 1, "only the @generic block")
        for yaml in (ROOT / "mods/cameo/ai").glob("*.yaml"):
            if yaml.name != "ai.yaml":
                self.assertNotRegex(yaml.read_text(encoding="utf-8"), r"ObservedAirThreats:\s*true")


if __name__ == "__main__":
    unittest.main()
