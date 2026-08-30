"""Regression tests for bash_guard rule 1b — the abandoned upstream fork.

PRIOR ART: extends tools/hooks/bash_guard.py, which already owns every Bash denial in
this project; this file is the test for one of its rules, not a second guard.

⛔ THE INCIDENT IT PINS. `Zeruel87/Cameo-mod` is the ORIGINAL upstream fork of this
project and it is abandoned. It is also still reachable and still answers `git fetch`,
which is precisely what makes it dangerous — it reads as a live upstream. On 2026-08-11
it was re-added as an `upstream` remote and a session went into reconciling two stray
commits against a tree nobody publishes to (DEVELOPMENT_LOG.md 657-670). Anything read
from it is history; anything written to it is lost. One remote: origin -> cameo-mod/Cameo-mod.

⚠ AND THE MIRROR-IMAGE TRAP, which these tests pin just as hard. Two appearances of the
old author name are ART CREDIT, not repository pointers: `Zeruel87 Urban` is a TILESET
CATEGORY id in mods/cameo/tilesets/*.yaml (every map placing those tiles resolves it by
name, so renaming it breaks them) and mods/cameo/credits.txt names a human being. A guard
that fires on the NAME rather than on the remote operation would invite exactly the sweep
that destroys both. Match the URL, never the name.

The narrowing test is not hypothetical either: the first draft of this rule matched any
`git` verb, and denied its own author's `git diff --stat` because the same shell line also
wrote the fork's name into a documentation file. A read-only command cannot contact a
remote, so the rule keys on the verbs that actually reach one.

Note: this file never spells the dead fork's name literally — it is assembled at runtime,
so the file does not become a grep hit for a repository nobody should be looking up.
"""

import json
import pathlib
import subprocess
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
HOOK = ROOT / "tools/hooks/bash_guard.py"

FORK = "Zer" + "uel87"
URL = "https://github.com/" + FORK + "/Cameo-mod.git"


def denied(command):
    """True if bash_guard denies `command`."""
    out = subprocess.run(
        [sys.executable, str(HOOK)],
        input=json.dumps({"tool_input": {"command": command}}),
        capture_output=True, text=True, cwd=str(ROOT)).stdout
    return '"deny"' in out


class ReachesTheDeadFork(unittest.TestCase):
    """Every git verb that can actually contact the fork is denied."""

    def test_remote_add(self):
        self.assertTrue(denied("git remote add upstream " + URL))

    def test_fetch(self):
        self.assertTrue(denied("git fetch " + URL + " master"))

    def test_push(self):
        self.assertTrue(denied("git push " + URL + " master"))

    def test_clone(self):
        self.assertTrue(denied("git clone " + URL + " /tmp/x"))

    def test_ls_remote(self):
        self.assertTrue(denied("git ls-remote " + URL))

    def test_global_flag_before_the_verb(self):
        self.assertTrue(denied("git -c protocol.version=2 fetch " + URL))

    def test_denied_after_a_shell_separator(self):
        self.assertTrue(denied("cd /tmp && git remote add up " + URL))


class TheArtCreditSurvives(unittest.TestCase):
    """The name is not the offence — reaching the remote is."""

    def test_reading_the_tileset_category(self):
        self.assertFalse(denied("grep -rn '" + FORK + " Urban' mods/cameo/tilesets/"))

    def test_editing_credits_is_not_this_guards_business(self):
        # Not endorsed — just not THIS rule's job. Sweeping the credit is a review
        # question, not a remote operation, and conflating the two is how it gets lost.
        self.assertFalse(denied("sed -i s/" + FORK + "//g mods/cameo/credits.txt"))


class ReadOnlyGitIsNotBlocked(unittest.TestCase):
    """The narrowing that the first draft of the rule got wrong."""

    def test_diff_on_a_line_that_mentions_the_fork(self):
        self.assertFalse(denied(
            "python3 patch_docs.py  # writes " + FORK + " into a doc\ngit diff --stat"))

    def test_log(self):
        self.assertFalse(denied("git log --oneline -3 -- docs/  # mentions " + FORK))

    def test_commit_message_quoting_the_rule(self):
        self.assertFalse(denied(
            "git commit -m 'docs: record that " + FORK + " is abandoned'"))


class TheLiveOrgRepositoriesAreNotTouched(unittest.TestCase):
    """cameo-mod/OpenRA is the engine soft-fork and is very much alive."""

    def test_engine_fork_fetch(self):
        self.assertFalse(denied(
            "git fetch https://github.com/cameo-mod/OpenRA.git cameo-engine"))

    def test_canonical_origin_push(self):
        self.assertFalse(denied("git push -u origin claude/docs-audit-reorganize-xgzwhr"))


if __name__ == "__main__":
    unittest.main()
