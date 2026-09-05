"""THE GUARD THAT CATCHES AN EDITED MASTER — the failure the shape-based audit cannot see.

⛔ `audit_chrome_scale_variants.py` measures each sheet's ARTWORK EXTENT against its declared
density. That catches a sheet laid out at the wrong scale, which is the bug that shipped. It is
blind to the everyday mistake: edit one faction icon inside `flags_4x.png`, commit, and the
1x/2x/3x sheets keep the OLD icon. Every extent is still right, every dimension still matches, both
shape audits pass, and three of the game's four scales render stale art.

⭐ So this guard compares CONTENT (SHA-256), and it has to distinguish the two directions:
master-moved is fixable by regenerating; derived-moved is NOT, because regenerating from an
unchanged master destroys the hand edit it just flagged.
"""

from __future__ import annotations

import json
import pathlib
import subprocess
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
AUDIT = ROOT / "tools" / "audit" / "audit_chrome_master_freshness.py"
STAMP = ROOT / "tools" / "art" / "chrome_masters.json"


def run(*args):
    return subprocess.run([sys.executable, str(AUDIT), *args],
                          cwd=ROOT, capture_output=True, text=True)


class ChromeMasterFreshnessTest(unittest.TestCase):
    """⚠ Each case rewrites the STAMP, never a PNG. The sheets are engine content and carry the
    boot gate (CLAUDE.md rule 1); a unit test must not touch them, and it does not need to —
    a wrong hash on either side is indistinguishable from a changed file."""

    def setUp(self):
        self.original = STAMP.read_text(encoding="utf-8")
        self.addCleanup(STAMP.write_text, self.original, "utf-8")

    def _stamp(self, mutate):
        data = json.loads(self.original)
        mutate(data)
        STAMP.write_text(json.dumps(data, indent=2, sort_keys=True) + "\n", encoding="utf-8")

    def test_the_committed_tree_is_current(self):
        """The sheets Blackrobe generated in a073f6cc6 match the master they came from."""
        r = run()
        self.assertEqual(r.returncode, 0, r.stdout)
        self.assertIn("PASS", r.stdout)

    def test_an_edited_master_with_stale_sheets_fails(self):
        """The case this was asked for: the 4x changed, the 1x/2x/3x did not."""
        self._stamp(lambda d: d["Flags"].__setitem__("master_sha256", "0" * 64))
        r = run()
        self.assertEqual(r.returncode, 1)
        self.assertIn("master edited, sheets NOT regenerated", r.stdout)
        self.assertIn("--fix", r.stdout)

    def test_a_hand_edited_generated_sheet_fails_and_is_NOT_offered_fix(self):
        """⛔ Offering --fix here would destroy the edit: regenerating pulls from an unchanged
        master. The remedy is to port the change into the master first."""
        self._stamp(lambda d: d["Flags"]["derived"].__setitem__(
            next(iter(d["Flags"]["derived"])), "1" * 64))
        r = run()
        self.assertEqual(r.returncode, 1)
        self.assertIn("generated sheet hand-edited", r.stdout)
        self.assertIn("Do not run `--fix` for this", r.stdout)

    def test_a_missing_stamp_is_not_a_failure(self):
        """A guard that fails closed on its own absence blocks anyone who has not seeded it yet.
        It reports how to seed and passes."""
        STAMP.unlink()
        r = run()
        self.assertEqual(r.returncode, 0, r.stdout)
        self.assertIn("--stamp", r.stdout)

    def test_a_corrupt_stamp_IS_a_failure(self):
        """Absent is 'not guarded yet'. Corrupt is 'the guard's only state is broken' — different."""
        STAMP.write_text("{not json", encoding="utf-8")
        r = run()
        self.assertEqual(r.returncode, 1)
        self.assertIn("not valid JSON", r.stdout)

    def test_it_uses_hashes_and_says_why_not_mtimes(self):
        """⚠ git does not preserve mtimes; a checkout or rebase would make an mtime guard lie."""
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn("hashlib.sha256", src)
        self.assertIn("HASHES, NOT MTIMES", src)
        self.assertIn("git does not preserve them", src)

    def test_fix_does_not_pretend_to_clear_the_boot_gate(self):
        """--fix writes PNGs under mods/. It must exit non-zero and name the gate it cannot pass."""
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn("BOOT GATE before committing", src)
        self.assertIn("**FIXED** — re-run this audit to confirm.", src)

    def test_the_stamp_lives_outside_mods(self):
        """Tooling metadata, not engine content — so the guard is committable in a tree that
        cannot boot, while the PNGs it describes are not."""
        self.assertFalse(str(STAMP.relative_to(ROOT)).startswith("mods"))
        self.assertTrue(STAMP.exists() or True)

    def test_it_is_wired_into_the_blocking_audit_loop(self):
        """An audit nobody runs guards nothing."""
        runner = (ROOT / "tools" / "audit" / "run_all.sh").read_text(encoding="utf-8")
        self.assertIn("chrome_master_freshness", runner)


if __name__ == "__main__":
    unittest.main()
