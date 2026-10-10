import contextlib
import copy
import hashlib
import io
import json
import pathlib
import tempfile
import unittest

from tools.ai import ab_summary, insurance_summary as subject


def fixture():
    base = dict(schema="cameo-insurance", version=1, game_uid="fixture", slot=1, difficulty="hard",
                faction="td_nod", complete=True, income_basis=subject.BASIS, timestep=1)
    return [dict(base, seq=0, tick=0, kind="start", reason="none", requested=0, credited=0,
                 cumulative=0, income=None, income_share=None),
            dict(base, seq=1, tick=1, kind="payout", reason="dynamic_rescue", requested=20, credited=20,
                 cumulative=20, income=None, income_share=None),
            dict(base, seq=2, tick=10, kind="end", reason="none", requested=0, credited=0,
                 cumulative=20, income=100, income_share=0.2)]


class InsuranceSummaryTest(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.path = pathlib.Path(self.tmp.name) / "cameo-ai-insurance-fixture.jsonl"

    def write(self, rows):
        self.path.write_text("".join(json.dumps(r) + "\n" for r in rows), encoding="utf-8")

    def test_valid_totals_and_each_reason(self):
        for reason in subject.REASONS:
            rows = fixture()
            rows[1]["reason"] = reason
            self.write(rows)
            result = subject.read(self.path)[0]
            self.assertEqual((result["total"], result["income"], result["share"]), (20, 100, .2))
            self.assertEqual(result["reasons"], {reason: 20})

    def test_missing_truncated_late_and_incomplete_are_unknown(self):
        self.assertEqual(subject.read(self.path)[0]["status"], "UNKNOWN")
        for rows in (fixture()[:-1], fixture()[1:], [dict(r, complete=False) for r in fixture()]):
            self.write(rows)
            self.assertEqual(subject.read(self.path)[0]["status"], "UNKNOWN")
        self.write(fixture())
        self.path.write_bytes(self.path.read_bytes()[:-1])
        self.assertEqual(subject.read(self.path)[0]["status"], "UNKNOWN")

    def test_changed_identity_sequence_totals_and_nonfinite_share_are_unknown(self):
        for index, field, value in ((1, "seq", 3), (1, "slot", 2), (1, "cumulative", 21),
                                    (2, "income_share", float("nan")), (2, "income", 19),
                                    (2, "faction", "changed"), (2, "complete", 1), (0, "version", True),
                                    (1, "credited", 1 << 64), (2, "income_share", 10 ** 400)):
            rows = fixture()
            rows[index][field] = value
            self.write(rows)
            self.assertEqual(subject.read(self.path)[0]["status"], "UNKNOWN", field)

    def test_duplicates_and_bounds_fail_closed(self):
        self.write(fixture())
        self.path.write_text(self.path.read_text().replace('"slot": 1', '"slot": 1, "slot": 1'), encoding="utf-8")
        self.assertEqual(subject.read(self.path)[0]["status"], "UNKNOWN")
        self.path.write_bytes(b" " * (subject.MAX_LINE + 1) + b"\n")
        self.assertEqual(subject.read(self.path)[0]["status"], "UNKNOWN")
        self.write(fixture())
        duplicate = self.path.with_name("cameo-ai-insurance-copy.jsonl")
        duplicate.write_bytes(self.path.read_bytes())
        self.assertTrue(all(r["status"] == "UNKNOWN" for r in subject.summaries([self.tmp.name])))

    def test_zero_recorded_is_distinct_from_missing_and_timestep_filters(self):
        rows = [fixture()[0], fixture()[2]]
        rows[1].update(seq=1, cumulative=0, income=0, income_share=None)
        self.write(rows)
        self.assertEqual(subject.read(self.path)[0]["total"], 0)
        self.assertEqual(list(subject.summaries([self.tmp.name], 10)), [])

    def test_ab_summary_prints_insurance_without_changing_match_records(self):
        self.write(fixture())
        before = self.path.read_bytes()
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            self.assertEqual(ab_summary.main([self.tmp.name]), 0)
        self.assertIn("20 insurance credits / 100 income (20.00%)", out.getvalue())
        self.assertEqual(before, self.path.read_bytes())

    def test_actual_csharp_generated_schema_file(self):
        root = pathlib.Path(__file__).resolve().parents[2]
        pointer = root / "engine/bin/insurance-fixture-path.txt"
        self.assertTrue(pointer.exists(), "run focused InsuranceTelemetry C# tests first")
        path = pathlib.Path(pointer.read_text().strip())
        before = hashlib.sha256(path.read_bytes()).hexdigest()
        result = subject.read(path)[0]
        self.assertEqual((result["status"], result["total"], result["income"]), ("RECORDED", 20, 100))
        self.assertEqual(before, hashlib.sha256(path.read_bytes()).hexdigest())

    def test_yaml_mount_and_legacy_settings_preserved(self):
        root = pathlib.Path(__file__).resolve().parents[2]
        player = (root / "mods/cameo/rules/player.yaml").read_text(encoding="utf-8")
        block = player.split("\tInsuranceCashTrickler@secondaryinsurance:\n", 1)[1].split("\tGrantConditionOnPlayerTotalCash:", 1)[0]
        self.assertIn("\t\tInterval: 1\n\t\tAmount: 1\n\t\tShowTicks: False\n", block)
        self.assertIn("RequiresCondition: secondaryinsurance && nobase && !genericbot", block)
        self.assertIn("\tCashTrickler@comeback:\n", player)
        world = (root / "mods/cameo/rules/world.yaml").read_text(encoding="utf-8")
        self.assertEqual(world.count("\tInsuranceTelemetry:\n"), 1)


if __name__ == "__main__":
    unittest.main()
