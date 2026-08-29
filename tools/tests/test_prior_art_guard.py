"""Regression tests for the prior-art guard (tools/hooks/prior_art_guard.py).

⛔ THE INCIDENT IT PINS. On 2026-08-30 `tools/audit/audit_turn_rate.py` was written
to check the vehicle/aircraft TurnSpeed law. `tools/audit/audit_stat_formulas.py`
had enforced that exact law for months (F8/F9/F10/F17/F19), it was already in
`run_all.sh`, `gen_derived_stats.py` already FIXED violations from its output, and
all five checks read 0 findings. The duplicate scoped by "has a Mobile or Aircraft
trait" instead of unit-type-plus-template, applied the GROUND law to aircraft in no
air template, and reported 340 violations against a roster with none — numbers that
reached DESIGN.md and HANDOFF.md before the real audit was ever run.

The SessionStart checklist already said "grep DESIGN.md before designing anything".
Advice that can be read and skipped is what failed; this guard runs the grep.
"""

import json
import pathlib
import subprocess
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
HOOK = ROOT / "tools/hooks/prior_art_guard.py"


def run(file_path, content="x"):
    """(allowed, reason) for a Write of `file_path`."""
    payload = json.dumps({"tool_name": "Write", "cwd": str(ROOT),
                          "tool_input": {"file_path": file_path, "content": content}})
    out = subprocess.run([sys.executable, str(HOOK)], input=payload,
                         capture_output=True, text=True).stdout.strip()
    if not out:
        return True, ""
    return False, json.loads(out)["hookSpecificOutput"]["permissionDecisionReason"]


class TheGuardCatchesTheIncidentItWasWrittenFor(unittest.TestCase):
    def test_the_real_duplicate_is_blocked(self):
        allowed, reason = run("tools/audit/audit_turn_rate.py")
        self.assertFalse(allowed)
        self.assertIn("PRIOR ART CHECK", reason)

    def test_it_names_a_file_that_leads_to_the_answer(self):
        """One precise pointer beats a list. `gen_derived_stats.py` documents
        F8/F9/F10/F17/F19 and names `audit_stat_formulas` — following it finds the
        law already implemented and passing."""
        _, reason = run("tools/audit/audit_turn_rate.py")
        self.assertIn("gen_derived_stats.py", reason)

    def test_it_does_not_bury_the_answer_in_noise(self):
        """⚠ Substring matching ranked 148 unrelated tools above the right one —
        `turn` hides in `return`, `rate` in `generate`. A guard that cries wolf
        gets skipped, which is the failure it exists to prevent."""
        _, reason = run("tools/audit/audit_turn_rate.py")
        self.assertLessEqual(sum(1 for ln in reason.splitlines()
                                 if ln.strip().startswith("- tools/")), 3)


class ItBlocksOnlyWhatItShould(unittest.TestCase):
    def test_editing_an_existing_tool_is_never_duplication(self):
        self.assertTrue(run("tools/audit/audit_stat_formulas.py")[0])

    def test_paths_outside_tools_are_not_its_business(self):
        self.assertTrue(run("docs/anything.py")[0])
        self.assertTrue(run("tools/audit/notes.md")[0])

    def test_a_cited_prior_art_line_lets_the_write_through(self):
        """The escape hatch is one line, on purpose: the guard forces the CHECK,
        not obedience. The citation stays in the file for the next reader."""
        allowed, _ = run("tools/audit/audit_turn_rate.py",
                         "PRIOR ART: gen_derived_stats.py fixes F17; this checks X.")
        self.assertTrue(allowed)

    def test_a_second_real_case(self):
        """`heaviness.py` already holds the continuous-heaviness implementation."""
        allowed, reason = run("tools/balance/heaviness_scale.py")
        self.assertFalse(allowed)
        self.assertIn("heaviness.py", reason)


class ItIsRegistered(unittest.TestCase):
    def test_settings_wires_it_to_write(self):
        cfg = json.loads((ROOT / ".claude/settings.json").read_text(encoding="utf-8"))
        wired = [m for m in cfg["hooks"]["PreToolUse"]
                 if any("prior_art_guard" in h.get("command", "") for h in m["hooks"])]
        self.assertEqual(len(wired), 1)
        self.assertEqual(wired[0]["matcher"], "Write")

    def test_the_existing_guards_are_still_wired(self):
        """Never weaken a guard while adding one."""
        cfg = json.loads((ROOT / ".claude/settings.json").read_text(encoding="utf-8"))
        blob = json.dumps(cfg)
        self.assertIn("bash_guard.py", blob)
        self.assertIn("session_checklist.py", blob)


if __name__ == "__main__":
    unittest.main()
