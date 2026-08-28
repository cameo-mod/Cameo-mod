import pathlib
import sys
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))

from miniyaml import Ruleset
from survey_weapon_structure import inventory


class WeaponStructureInventoryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data = inventory(Ruleset(ROOT))

    def test_partition_is_complete_and_disjoint(self):
        sets = self.data["sets"]
        direct = set(sets["direct_actor_armament"])
        indirect = set(sets["indirect_weapon_graph"])
        unreached = set(sets["unreached"])
        self.assertFalse(direct & indirect)
        self.assertFalse(direct & unreached)
        self.assertFalse(indirect & unreached)
        self.assertEqual(
            self.data["counts"]["stacked_main_all_concrete"],
            len(direct | indirect | unreached),
        )

    def test_direct_is_a_subset_of_transitive_reachability(self):
        counts = self.data["counts"]
        self.assertEqual(
            counts["stacked_main_transitive_weapon_graph"],
            counts["stacked_main_direct_actor_armament"]
            + counts["stacked_main_indirect_weapon_graph"],
        )

    def test_current_corrected_baseline(self):
        self.assertEqual(2345, self.data["counts"]["concrete_weapons"])
        self.assertEqual(693, self.data["counts"]["stacked_main_all_concrete"])
        self.assertEqual(494, self.data["counts"]["stacked_main_direct_actor_armament"])


if __name__ == "__main__":
    unittest.main()
