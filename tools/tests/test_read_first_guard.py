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
# ⭐ IMPORTED, never restated. The tiers are owned by the DOCS MAXING AUDIT; a second
# copy in the test is a copy that goes stale and then pins the OLD contract — which is
# how a test starts defending the bug. This list grew from 5 to 7 on 2026-08-30 and the
# only reason nothing broke silently is that it is read from the source.
sys.path.insert(0, str(ROOT / "tools/audit"))
from audit_docs_maxing import TIER1, TIER2, authored_docs  # noqa: E402

ALWAYS = list(TIER1)


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

    def test_the_TOPIC_gate_only_guards_docs_and_tools(self):
        """Tier 2 is scoped to docs/ and tools/. Tier 1 is not scoped to anything —
        so the yaml edit still needs the reading order open, and that is the point."""
        self.assertTrue(edit("mods/cameo/rules/x.yaml", ALWAYS)[0])
        self.assertFalse(edit("mods/cameo/rules/x.yaml", [])[0])

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

    def test_the_read_gate_sees_every_tool_not_only_edits(self):
        """The TIER 1 gate is worthless behind a `Write|Edit` matcher: the actions it
        exists to stop are mostly Bash. Pins the widened matcher."""
        cfg = json.loads((ROOT / ".claude/settings.json").read_text(encoding="utf-8"))
        entries = [e for e in cfg["hooks"]["PreToolUse"]
                   if any("read_first_guard.py" in h.get("command", "")
                          for h in e.get("hooks", []))]
        self.assertTrue(entries)
        self.assertEqual("*", entries[0].get("matcher"))


class TheTopicalMapCoversMoreThanOneIncident(unittest.TestCase):
    """⚠ A GUARD WRITTEN FROM ONE INCIDENT COVERS ONE INCIDENT.

    `TOPICAL` held a single entry — the anchor decisions log — because that was the failure that
    prompted the guard. On 2026-08-30 the identical class of mistake recurred in a topic the map
    did not name: a full armor-tilt investigation ran without `WEAPON_HEAVINESS.md`, whose §9.4
    had ALREADY ruled the 2x-8x band with a 4x target and already recorded 37 of 42 families
    inside it. A law was re-derived from scratch, and a measurement was reported as a defect when
    the law it supposedly broke was being met exactly. In the same pass two external reviews
    asserted `Jumpjet = Plate x Scout`, while `ARMOR_LAYERS.md` says `jumpjet = fighter x scout`,
    and nothing required that file to be open either.

    These pin the TOPICS rather than the mechanism, so the map cannot silently shrink back to one.
    """

    def test_armor_tilt_work_requires_the_spread_law(self):
        allowed, reason = edit("docs/DESIGN.md", ALWAYS,
                               "raise the armor tilt spread band toward 4x")
        self.assertFalse(allowed)
        self.assertIn("WEAPON_HEAVINESS", reason)

    def test_reading_BOTH_armor_docs_unblocks_it(self):
        """`armor tilt` legitimately triggers two documents, and both are required.

        The first draft of this test expected WEAPON_HEAVINESS alone to unblock it and failed —
        correctly. The phrase carries the spread law AND the armor vocabulary, and an agent who
        has read only one of them is exactly the agent who re-derives §9.4 while getting a
        derived armor row wrong. The guard was right; the expectation was wrong.
        """
        allowed, _ = edit("docs/DESIGN.md",
                          ALWAYS + ["docs/design/WEAPON_HEAVINESS.md",
                                    "docs/design/ARMOR_LAYERS.md"],
                          "raise the armor tilt spread band toward 4x")
        self.assertTrue(allowed)

    def test_derived_armor_work_requires_the_armor_layers_doc(self):
        allowed, reason = edit("docs/DESIGN.md", ALWAYS,
                               "jumpjet armor is fighter x scout, heroic is plate x scout")
        self.assertFalse(allowed)
        self.assertIn("ARMOR_LAYERS", reason)

    def test_weapon_structure_work_requires_the_program_plan(self):
        allowed, reason = edit("docs/DESIGN.md", ALWAYS,
                               "W24 multi-main collapse and the 3-way split order")
        self.assertFalse(allowed)
        self.assertIn("BALANCE_PROGRAM_PLAN", reason)

    def test_an_unrelated_edit_is_caught_by_none_of_it(self):
        allowed, _ = edit("docs/README.md", ALWAYS, "fix a typo in the orientation page")
        self.assertTrue(allowed)




class TheDocsMaxingAuditGate(unittest.TestCase):
    """TIER 1 — the maintainer's 2026-08-30 order: "make it illegal for any AI agent to
    perform any actions before loading the entire documentation into the context".

    ⛔ The literal order is unsatisfiable and saying so is part of the implementation:
    the authored set is 117 files / ~92,700 lines / ~1.9M tokens. What IS enforced is
    the strongest true version — no action at all until the seven reading-order
    documents are open — plus two exemptions without which the gate could never be
    satisfied: reading (you cannot open a document without a tool) and
    `git status`/`log`/`diff` (an agent that cannot orient cannot even report why it
    is stuck).
    """

    def call(self, payload, opened):
        payload = dict(payload)
        payload["cwd"] = str(ROOT)
        payload["transcript_path"] = fake_transcript(opened)
        out = subprocess.run([sys.executable, str(READ_FIRST)], input=json.dumps(payload),
                             text=True, capture_output=True).stdout.strip()
        if not out:
            return True, ""
        return False, json.loads(out)["hookSpecificOutput"]["permissionDecisionReason"]

    def bash(self, cmd, opened):
        return self.call({"tool_name": "Bash", "tool_input": {"command": cmd}}, opened)

    def test_a_mutating_command_is_denied_until_the_order_is_open(self):
        allowed, reason = self.bash("git commit -m x", [])
        self.assertFalse(allowed)
        self.assertIn("DOCS MAXING AUDIT", reason)
        for doc in TIER1:
            self.assertIn(doc, reason)

    def test_reading_is_exempt_because_otherwise_the_gate_is_a_deadlock(self):
        for cmd in ("sed -n '1,400p' docs/DESIGN.md", "cat docs/HANDOFF.md",
                    "grep -n foo docs/README.md", "git status --short",
                    "git log --oneline -3"):
            self.assertTrue(self.bash(cmd, [])[0], cmd)

    def test_a_read_that_smuggles_a_mutation_is_not_a_read(self):
        self.assertFalse(self.bash("cat x && rm -rf y", [])[0])

    def test_the_gate_opens_once_every_tier1_document_is_read(self):
        self.assertTrue(self.bash("git commit -m x", ALWAYS)[0])

    def test_it_fails_open_without_a_transcript(self):
        out = subprocess.run(
            [sys.executable, str(READ_FIRST)], text=True, capture_output=True,
            input=json.dumps({"tool_name": "Bash", "cwd": str(ROOT),
                              "tool_input": {"command": "git commit -m x"}})).stdout
        self.assertEqual("", out.strip())


class TheDocsMaxingAuditItself(unittest.TestCase):
    def test_the_manifest_is_derived_not_hand_listed(self):
        """A hand-maintained file list goes stale the first time someone adds a
        document, and a stale manifest is how "I did not know it existed" comes back."""
        docs = authored_docs()
        self.assertIn("docs/DESIGN.md", docs)
        self.assertIn("docs/design/WEAPON_HEAVINESS.md", docs)
        for excluded in ("docs/history/", "docs/audit/latest/", "docs/audit/degraded/",
                         "docs/audit/baseline/"):
            self.assertFalse([d for d in docs if d.startswith(excluded)], excluded)

    def test_every_tier_document_actually_exists(self):
        for d in list(TIER1) + list(TIER2):
            self.assertTrue((ROOT / d).is_file(), d)

    def test_it_is_registered_in_the_suite(self):
        self.assertIn("docs_maxing",
                      (ROOT / "tools/audit/run_all.sh").read_text(encoding="utf-8"))

    def test_the_session_start_hook_emits_the_manifest(self):
        out = subprocess.run([sys.executable, str(ROOT / "tools/hooks/session_checklist.py")],
                             input="{}", text=True, capture_output=True).stdout
        ctx = json.loads(out)["hookSpecificOutput"]["additionalContext"]
        self.assertIn("DOCS MAXING AUDIT", ctx)
        for d in TIER1:
            self.assertIn(d, ctx)


if __name__ == "__main__":
    unittest.main()
