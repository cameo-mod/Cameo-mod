"""Coverage and freshness contracts for the compact maintainer review queue."""

from __future__ import annotations

import pathlib
import sys
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path[:0] = [str(ROOT / "tools/audit")]

import report_remaining_weapon_decisions as report  # noqa: E402


class WeaponDecisionBundleTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rows, cls.data = report.decision_rows()

    def test_every_unreviewed_reachable_weapon_occurs_once(self):
        names = [member for row in self.rows for member in row["members"]]
        self.assertEqual(77, len(names))
        self.assertEqual(len(names), len(set(names)))
        self.assertEqual(58, len(self.rows))

    def test_reviewed_composites_are_not_presented_as_open_decisions(self):
        names = {member for row in self.rows for member in row["members"]}
        families = {row["family"] for row in self.rows}
        self.assertNotIn("TSHellfireSonic", names)
        self.assertNotIn("WaveforceCannon", names)
        self.assertNotIn("RA2Virusgun2", families)
        self.assertNotIn("WaveArtilleryImpact", families)
        self.assertNotIn("RA2Virusgun3", families)
        self.assertNotIn("WaveTurretImpact", families)
        self.assertIn("SteelVulcan", families)
        self.assertIn("RA160mmE_rad_elite", families)
        self.assertEqual(210, self.data["reviewed_reachable"])

    def test_buckets_use_engine_defaults_and_mechanical_labels(self):
        counts = {
            name: (
                sum(row["bucket"] == name for row in self.rows),
                sum(len(row["members"]) for row in self.rows
                    if row["bucket"] == name),
            )
            for name in report.BUCKETS
        }
        self.assertEqual({
            "target and state routing": (2, 2),
            "target routing": (16, 21),
            "state delivery": (12, 19),
            "legacy compatibility": (1, 1),
            "numbered warhead key": (1, 1),
            "no special mechanical signal": (26, 33),
        }, counts)

    def test_every_member_is_covered_by_one_exact_main_fingerprint(self):
        multi_rows = 0
        multi_definitions = 0
        for row in self.rows:
            fingerprint_members = [
                member for fingerprint in row["fingerprints"]
                for member in fingerprint["members"]
            ]
            self.assertEqual(
                (len(row["members"]), set(row["members"])),
                (len(fingerprint_members), set(fingerprint_members)),
                row["family"],
            )
            for fingerprint in row["fingerprints"]:
                self.assertGreaterEqual(len(fingerprint["mains"]), 2)
            if len(row["fingerprints"]) > 1:
                multi_rows += 1
                multi_definitions += len(row["members"])
        self.assertEqual((8, 18), (multi_rows, multi_definitions))

    def test_report_is_fresh(self):
        self.assertEqual(
            report.rendered(self.data),
            report.OUT.read_text(encoding="utf-8"),
        )


if __name__ == "__main__":
    unittest.main()
