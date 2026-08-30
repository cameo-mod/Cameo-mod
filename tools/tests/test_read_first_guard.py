"""Regression tests for the read-first guard and the two bash_guard checks added
after the 2026-08-30 incidents.

⛔ WHAT THESE PIN. `docs/README.md` defines the reading order, `CLAUDE.md` repeats
it, and the SessionStart hook injects it every session. All three were in context
and an edit still went ahead without opening `docs/AGENT_WORKSPACE.md`. Reading it
afterwards surfaced two rules already broken:

  * git rule 1 — "always fetch, pull and merge before any commit". Skipping it is
    why the branch drifted 16 commits behind master and came one merge away from
    reverting another contributor's weapon-consolidation work.
  * fit_class.py step 4 — signing an anchor is the MAINTAINER's. Three were
    self-signed on an agent's own fit tables, including `scout` at worst |Δ| 22.8
    against a ≤1 bar.

Advice that can be read and skipped is what failed. These checks look at what the
session and the index ACTUALLY contain.
"""

import json
import os
import pathlib
import subprocess
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
READ_FIRST = ROOT / "tools/hooks/read_first_guard.py"
BASH = ROOT / "tools/hooks/bash_guard.py"
ALWAYS = ["docs/README.md", "docs/LESSONS_LEARNED.md", "docs/AGENT_WORKSPACE.md",
          "docs/HANDOFF.md", "docs/DESIGN.md"]


def fake_transcript(paths):
    f = tempfile.NamedTemporaryFile("w", suffix=".jsonl", delete=False)
    for p in paths:
        f.write(json.dumps({"message": {"content": [
            {"type": "tool_use", "input": {"file_path": p}}]}}) + "\n")
    f.close()
    return f.name


def edit(target, opened, content="", transcript=True):
    payload = {"tool_name": "Edit", "cwd": str(ROOT),
               "tool_input": {"file_path": target, "new_string": content}}
    if transcript:
        payload["transcript_path"] = fake_transcript(opened)
    out = subprocess.run([sys.executable, str(READ_FIRST)], input=json.dumps(payload),
                         text=True, capture_output=True).stdout.strip()
    if not out:
        return True, ""
    return False, json.loads(out)["hookSpecificOutput"]["permissionDecisionReason"]


class TheReadGateChecksWhatTheSessionActuallyOpened(unittest.TestCase):
    def test_editing_a_doc_with_nothing_read_is_blocked(self):
        allowed, reason = edit("docs/DESIGN.md", [])
        self.assertFalse(allowed)
        for doc in ALWAYS:
            self.assertIn(doc, reason)

    def test_once_the_required_set_is_open_the_edit_proceeds(self):
        self.assertTrue(edit("docs/DESIGN.md", ALWAYS)[0])

    def test_anchor_work_additionally_requires_the_decisions_log(self):
        """README says class_anchors.json is "maintained via" that log, which makes
        it the source of truth for every baseline. A session spent on class anchors
        never opened it and re-derived a defense formula ruled on 2026-07-26."""
        allowed, reason = edit("tools/balance/fit_class.py", ALWAYS, "signed_off cost0")
        self.assertFalse(allowed)
        self.assertIn("anchor_decisions_log.md", reason)
        self.assertTrue(edit("tools/balance/fit_class.py",
                             ALWAYS + ["docs/balance/anchor_decisions_log.md"],
                             "signed_off cost0")[0])

    def test_a_bash_read_counts_as_reading(self):
        """Most reading here is `sed -n`/`head`/`grep`, not the Read tool."""
        f = tempfile.NamedTemporaryFile("w", suffix=".jsonl", delete=False)
        for p in ALWAYS:
            f.write(json.dumps({"message": {"content": [
                {"type": "tool_use", "input": {"command": f"sed -n '1,400p' {p}"}}]}}) + "\n")
        f.close()
        out = subprocess.run([sys.executable, str(READ_FIRST)], text=True, capture_output=True,
                             input=json.dumps({"tool_name": "Edit", "cwd": str(ROOT),
                                               "transcript_path": f.name,
                                               "tool_input": {"file_path": "docs/DESIGN.md"}})).stdout
        self.assertEqual(out.strip(), "")

    def test_it_only_guards_docs_and_tools(self):
        self.assertTrue(edit("mods/cameo/rules/x.yaml", [])[0])

    def test_it_fails_open_without_a_transcript(self):
        """A guard that blocks blindly gets disabled, and a disabled guard protects
        nothing."""
        self.assertTrue(edit("docs/DESIGN.md", [], transcript=False)[0])


class TheCommitGateCatchesTheTwoBrokenRules(unittest.TestCase):
    def guard(self, cmd):
        out = subprocess.run([sys.executable, str(BASH)], text=True, capture_output=True,
                             input=json.dumps({"tool_name": "Bash", "cwd": str(ROOT),
                                               "tool_input": {"command": cmd}})).stdout.strip()
        return (True, "") if not out else (
            False, json.loads(out)["hookSpecificOutput"]["permissionDecisionReason"])

    def test_the_sign_off_check_is_present_and_escapable_only_by_order(self):
        src = BASH.read_text(encoding="utf-8")
        self.assertIn("signed_off", src)
        self.assertIn("MAINTAINER-ORDERED SIGN-OFF", src)

    def test_the_stale_branch_check_cites_the_rule_it_enforces(self):
        src = BASH.read_text(encoding="utf-8")
        self.assertIn("HEAD..origin/master", src)
        self.assertIn("AGENT_WORKSPACE", src)

    def test_the_stale_branch_check_never_reaches_the_network(self):
        """It must use already-fetched refs only: a guard that can hang on a bad
        link is a guard someone will remove."""
        src = BASH.read_text(encoding="utf-8")
        start = src.index("# (3b) STALE BRANCH")
        end = src.index("engine_prefixes =", start)
        for forbidden in ("git\", \"fetch", "fetch\"", "ls-remote"):
            self.assertNotIn(forbidden, src[start:end])


class EveryGuardStaysWired(unittest.TestCase):
    def test_all_four_hooks_are_registered(self):
        """Never weaken a guard while adding one."""
        blob = (ROOT / ".claude/settings.json").read_text(encoding="utf-8")
        for hook in ("session_checklist.py", "bash_guard.py",
                     "prior_art_guard.py", "read_first_guard.py"):
            self.assertIn(hook, blob)


if __name__ == "__main__":
    unittest.main()
