"""coverage_report.py + audit T4 ratchet: parsing, scoping, and the ratchet.

These tools are the AR-T1 coverage gate: coverage_report.py turns
dotnet-coverage Cobertura XML into a per-file table + baseline, and
audit_test_coverage.py's T4 fails the audit when a baselined bot-module
file loses line coverage. The two parsers must agree on multi-type files
(several <class> records per filename) — the bug that made the first
ratchet draft report phantom 100%->0% regressions.
"""
import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path[:0] = [str(ROOT / "tools" / "ai"), str(ROOT / "tools" / "audit")]

import coverage_report  # noqa: E402
import audit_test_coverage  # noqa: E402

COBERTURA = """<?xml version="1.0" ?>
<coverage line-rate="0.5" branch-rate="0.5" version="1.9">
 <packages><package name="OpenRA.Mods.Cameo">
  <classes>
   <class name="ModularBot" filename="{root}/OpenRA.Mods.Cameo/Traits/ModularBot.cs">
    <methods><method name="Tick"><lines><line number="10" hits="1"/></lines></method></methods>
    <lines>
     <line number="10" hits="1"/>
     <line number="11" hits="0" branch="True" condition-coverage="50% (1/2)">
      <conditions><condition number="0" type="jump" coverage="100%"/></conditions>
     </line>
     <line number="12" hits="1"/>
    </lines>
   </class>
   <class name="ModularBot+Inner" filename="{root}/OpenRA.Mods.Cameo/Traits/ModularBot.cs">
    <lines><line number="30" hits="0"/></lines>
   </class>
   <class name="Squad" filename="{root}/OpenRA.Mods.CA/Traits/BotModules/Squads/SquadCA.cs">
    <lines><line number="1" hits="1"/><line number="2" hits="1"/></lines>
   </class>
   <class name="WithAlpha" filename="{root}/OpenRA.Mods.Cameo/Traits/Render/WithAlpha.cs">
    <lines><line number="1" hits="1"/></lines>
   </class>
   <class name="Engine" filename="{root}/engine/OpenRA.Game/Game.cs">
    <lines><line number="1" hits="1"/></lines>
   </class>
  </classes>
 </package></packages>
</coverage>
"""


def write_xml(tmp: pathlib.Path) -> pathlib.Path:
    """Fixture class filenames live under the real repo root so both parsers
    produce repo-relative keys (files need not exist on disk)."""
    xml = tmp / "cov.xml"
    xml.write_text(COBERTURA.format(
        root=str(coverage_report.REPO_ROOT).replace("\\", "/")),
        encoding="utf-8")
    return xml


class ScopeTests(unittest.TestCase):
    def test_ca_botmodules_in_scope(self):
        self.assertTrue(coverage_report.in_scope(
            r"X:\repo\OpenRA.Mods.CA\Traits\BotModules\Squads\SquadCA.cs"))

    def test_cameo_botmodules_in_scope(self):
        self.assertTrue(coverage_report.in_scope(
            r"X:\repo\OpenRA.Mods.Cameo\Traits\BotModules\ScoutBotModule.cs"))

    def test_cameo_traits_root_bot_file_in_scope(self):
        self.assertTrue(coverage_report.in_scope(
            r"X:\repo\OpenRA.Mods.Cameo\Traits\ModularBot.cs"))

    def test_cameo_nonbot_traits_out_of_scope(self):
        self.assertFalse(coverage_report.in_scope(
            r"X:\repo\OpenRA.Mods.Cameo\Traits\Render\WithAlpha.cs"))

    def test_engine_out_of_scope(self):
        self.assertFalse(coverage_report.in_scope(
            r"X:\repo\engine\OpenRA.Game\Traits\BotModules\X.cs"))


class ParseTests(unittest.TestCase):
    def test_multitype_file_accumulates_and_scopes(self):
        with tempfile.TemporaryDirectory() as td:
            tmp = pathlib.Path(td)
            xml = write_xml(tmp)
            rows = coverage_report.parse(str(xml))
        self.assertIn("OpenRA.Mods.Cameo/Traits/ModularBot.cs", rows)
        self.assertIn("OpenRA.Mods.CA/Traits/BotModules/Squads/SquadCA.cs", rows)
        self.assertNotIn("OpenRA.Mods.Cameo/Traits/Render/WithAlpha.cs", rows)
        self.assertNotIn("engine/OpenRA.Game/Game.cs", rows)
        mb = rows["OpenRA.Mods.Cameo/Traits/ModularBot.cs"]
        # 3 class-level lines + 1 method-level line + 1 from Inner = 5 total,
        # hits: 10(twice),12 = 3 -> 60%
        self.assertEqual(mb["lines"], 5)
        self.assertEqual(mb["lines_hit"], 3)
        self.assertEqual(mb["branches"], 2)
        self.assertEqual(mb["branches_hit"], 1)

    def test_audit_parser_agrees_on_multitype_files(self):
        with tempfile.TemporaryDirectory() as td:
            tmp = pathlib.Path(td)
            xml = write_xml(tmp)
            rows = coverage_report.parse(str(xml))
            audit_rows = audit_test_coverage.parse_cobertura(
                xml, pathlib.Path(coverage_report.REPO_ROOT))
        for rel, rec in rows.items():
            expected = 100.0 * rec["lines_hit"] / rec["lines"]
            self.assertAlmostEqual(audit_rows[rel], expected, places=2,
                                   msg=f"parsers disagree on {rel}")

    def test_baseline_doc_rounds_and_sorts(self):
        doc = coverage_report.baseline_doc({
            "b.cs": {"lines": 3, "lines_hit": 1, "branches": 0, "branches_hit": 0},
            "a.cs": {"lines": 1, "lines_hit": 1, "branches": 0, "branches_hit": 0},
        })
        self.assertEqual(list(doc["files"].keys()), ["a.cs", "b.cs"])
        self.assertEqual(doc["files"]["a.cs"]["line_rate"], 100.0)
        self.assertAlmostEqual(doc["files"]["b.cs"]["line_rate"], 33.33, places=2)


class RatchetTests(unittest.TestCase):
    def test_hold_passes(self):
        baseline = {"f.cs": {"line_rate": 50.0, "branch_rate": 0, "lines": 10}}
        reg, removed = audit_test_coverage.ratchet_regressions(baseline, {"f.cs": 50.0})
        self.assertEqual(reg, [])
        self.assertEqual(removed, [])

    def test_drop_beyond_epsilon_fails(self):
        baseline = {"f.cs": {"line_rate": 50.0, "branch_rate": 0, "lines": 10}}
        reg, _ = audit_test_coverage.ratchet_regressions(baseline, {"f.cs": 49.0})
        self.assertEqual(len(reg), 1)
        self.assertEqual(reg[0][0], "f.cs")

    def test_drop_within_epsilon_passes(self):
        baseline = {"f.cs": {"line_rate": 50.0, "branch_rate": 0, "lines": 10}}
        reg, _ = audit_test_coverage.ratchet_regressions(baseline, {"f.cs": 49.95})
        self.assertEqual(reg, [])

    def test_missing_file_is_removed_not_regressed(self):
        baseline = {"gone.cs": {"line_rate": 50.0, "branch_rate": 0, "lines": 10}}
        reg, removed = audit_test_coverage.ratchet_regressions(baseline, {})
        self.assertEqual(reg, [])
        self.assertEqual(removed, ["gone.cs"])

    def test_new_files_ignored_by_ratchet(self):
        reg, removed = audit_test_coverage.ratchet_regressions({}, {"new.cs": 1.0})
        self.assertEqual(reg, [])
        self.assertEqual(removed, [])


if __name__ == "__main__":
    unittest.main()
