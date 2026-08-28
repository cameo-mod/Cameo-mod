import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))

import classify_remaining_weapons as classifier
from miniyaml import Ruleset


class RemainingWeaponClassificationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rows = classifier.classify(Ruleset(ROOT))

    def test_active_central_files_are_included_without_template_bleed(self):
        names = {row["weapon"] for row in self.rows}
        active_files = {path.resolve() for path in classifier.weapon_files()}
        self.assertIn((ROOT / "mods/cameo/weapons/d2k.yaml").resolve(), active_files)
        self.assertIn("NaxisBlackBombSmaller", names)  # active RA2Mod central file
        self.assertIn("edenRailgun", names)             # active Outpost 2 central file
        self.assertNotIn("ts_nod_mobilerepairvehicle", names)

    def test_every_root_has_a_review_bucket_and_flat_ledger(self):
        allowed = {
            "one inherited destination",
            "corroborated suggestion",
            "legacy-only suggestion",
            "human decision required",
        }
        self.assertTrue(self.rows)
        for row in self.rows:
            self.assertIn(row["bucket"], allowed)
            self.assertTrue(row["old_families"])
            self.assertTrue(row["flat_hits"])

    def test_ambiguous_examples_stay_in_human_review(self):
        rows = {row["weapon"]: row for row in self.rows}
        for name in ("DuelistTankCannon", "LatinSmokerCannon", "AsianPhotonCannon",
                     "Future_MultiMissile_Sigma", "PhotonCannon",
                     "RA2GrandCannonWeapon"):
            self.assertEqual("human decision required", rows[name]["bucket"], name)

    def test_multiple_tiers_are_preserved_as_distinct_destinations(self):
        rows = {row["weapon"]: row for row in self.rows}
        for name in ("DuelistTankCannon", "LatinSmokerCannon"):
            destinations = {item["destination"] for item in rows[name]["canonical_templates"]}
            self.assertGreater(len(destinations), 1, name)

    def test_folded_percentage_hit_is_recorded_beside_its_flat_hit(self):
        weapon = Ruleset(ROOT).resolve_weapon("120mm_td")
        self.assertIn("CannonHE_Medium", {hit["tag"] for hit in classifier.flat_ledger(weapon)})
        folded = [hit for hit in classifier.percentage_ledger(weapon)
                  if hit["tag"] == "CannonHE_Medium" and hit["kind"] == "pct_folded"]
        self.assertEqual(1, len(folded))
        self.assertEqual(10000, folded[0]["scale"])
        self.assertEqual(10000, folded[0]["denominator"])
        self.assertIn("Shield", folded[0]["percentage_versus"])

    def test_percentage_physical_state_map_is_recorded(self):
        weapon = Ruleset(ROOT).resolve_weapon("120mm_td")
        states = [state for hit in classifier.percentage_ledger(weapon)
                  for state in hit["physical_states"]]
        self.assertIn({"name": "Corrosion", "scale": "100", "source": "map"}, states)

    def test_exception_families_are_never_automatic(self):
        for row in self.rows:
            if set(row["old_families"]) & classifier.EXCEPTION_FAMILIES:
                self.assertEqual("human decision required", row["bucket"])


if __name__ == "__main__":
    unittest.main()
