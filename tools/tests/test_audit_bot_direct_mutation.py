"""Unit tests for tools/audit/audit_bot_direct_mutation.py.

The audit is a multiplayer-desync ratchet: bots run host-only and may touch
actors ONLY through the order stream. The original scan pinned
`CancelActivity`/`QueueActivity`/`SetStance` at zero; the 2026-10-04 review
extended it to `GrantCondition`/`RevokeCondition` after finding tick-path
condition grants that would desync the moment any yaml consumes them.

The load-bearing property is the ALLOWLIST's *count cap*: a NEW call site in an
allowlisted file must still FAIL. A test that only checks "allowlisted files
pass" would bless an audit that silently swallows new hazards.
"""

from __future__ import annotations

import contextlib
import io
import pathlib
import tempfile
import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

import audit_bot_direct_mutation as audit


def _write(root: pathlib.Path, rel: str, body: str) -> pathlib.Path:
    p = root / rel
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(body, encoding="utf-8")
    return p


class Pattern(unittest.TestCase):
    """The regex is the tripwire — pin exactly which call shapes it catches."""

    def test_matches_all_five_mutations(self):
        line = "self.CancelActivity(); bot.QueueOrder(); x.QueueActivity(a); autoTarget.SetStance(a, s); playerActor.GrantCondition(c); playerActor.RevokeCondition(t);"
        calls = [m.group(0) for m in audit.PATTERN.finditer(line)]
        self.assertEqual(
            calls,
            [".CancelActivity(", ".QueueActivity(", ".SetStance(",
             ".GrantCondition(", ".RevokeCondition("],
        )

    def test_ignores_order_stream_and_reads(self):
        for safe in (
            'bot.QueueOrder(new Order("Stop", actor, false));',
            "bool has = actor.HasCondition(c);",
            "GrantConditionOnBotOwner@inc3f1:",  # yaml key, not a call
        ):
            self.assertIsNone(audit.PATTERN.search(safe), safe)


class FindSites(unittest.TestCase):
    def test_line_comments_are_not_sites(self):
        with tempfile.TemporaryDirectory() as td:
            f = _write(
                pathlib.Path(td), "M.cs",
                "void Tick() { // actor.CancelActivity() still allowed in comments\n"
                "    actor.CancelActivity();\n"
                "}\n",
            )
            sites = list(audit.find_sites(f))
            self.assertEqual(len(sites), 1)
            self.assertEqual(sites[0][0], 2)

    def test_no_match_yields_nothing(self):
        with tempfile.TemporaryDirectory() as td:
            f = _write(pathlib.Path(td), "M.cs", "void Tick() { bot.QueueOrder(o); }\n")
            self.assertEqual(list(audit.find_sites(f)), [])


class EndToEnd(unittest.TestCase):
    """Drive main() against a synthetic tree — patches REPO/SCAN_DIRS/ALLOWLIST."""

    def _run(self, root: pathlib.Path, allowlist=None):
        old_repo, old_dirs, old_globs, old_allow = (
            audit.REPO, audit.SCAN_DIRS, audit.SCAN_GLOBS, audit.ALLOWLIST)
        try:
            audit.REPO = root
            audit.SCAN_DIRS = ["Bots"]
            audit.SCAN_GLOBS = []
            audit.ALLOWLIST = allowlist if allowlist is not None else {}
            buf = io.StringIO()
            with contextlib.redirect_stdout(buf):
                code = audit.main()
            return code, buf.getvalue()
        finally:
            audit.REPO, audit.SCAN_DIRS, audit.SCAN_GLOBS, audit.ALLOWLIST = (
                old_repo, old_dirs, old_globs, old_allow)

    def test_clean_tree_passes(self):
        with tempfile.TemporaryDirectory() as td:
            _write(pathlib.Path(td), "Bots/M.cs",
                   "void Tick() { bot.QueueOrder(new Order(\"Move\", a, false)); }\n")
            code, out = self._run(pathlib.Path(td))
            self.assertEqual(code, 0)
            self.assertIn("PASS", out)
            self.assertIn("GrantCondition", out)  # report names the full scan set

    def test_unallowlisted_grant_fails(self):
        with tempfile.TemporaryDirectory() as td:
            _write(pathlib.Path(td), "Bots/M.cs",
                   "void Tick() { playerActor.GrantCondition(\"x\"); }\n")
            code, out = self._run(pathlib.Path(td))
            self.assertEqual(code, 1)
            self.assertIn("FAIL", out)
            self.assertIn("GrantCondition", out)

    def test_allowlist_cap_lets_new_sites_fail(self):
        """The count cap is the ratchet: sites beyond the cap must still trip."""
        with tempfile.TemporaryDirectory() as td:
            _write(pathlib.Path(td), "Bots/M.cs",
                   "void A() { actor.GrantCondition(\"a\"); }\n"
                   "void B() { actor.GrantCondition(\"b\"); }\n"
                   "void C() { actor.GrantCondition(\"c\"); }\n")
            allow = {"Bots/M.cs": (2, "documented existing sites")}
            code, out = self._run(pathlib.Path(td), allow)
            self.assertEqual(code, 1)
            self.assertIn("FAIL — 1 direct-activity", out)  # only the 3rd fails
            self.assertIn("Allowlisted sites (2)", out)
            self.assertIn("documented existing sites", out)

    def test_pass_report_discloses_allowlisted_sites(self):
        """Zero unallowlisted != zero sites — the inventory must stay visible."""
        with tempfile.TemporaryDirectory() as td:
            _write(pathlib.Path(td), "Bots/M.cs",
                   "void A() { actor.RevokeCondition(tok); }\n")
            allow = {"Bots/M.cs": (1, "synced: order path")}
            code, out = self._run(pathlib.Path(td), allow)
            self.assertEqual(code, 0)
            self.assertIn("Allowlisted sites (1)", out)
            self.assertIn("zero unaudited", out)
            self.assertNotIn("zero direct-activity sites", out)


class RealRepo(unittest.TestCase):
    """The committed tree must satisfy its own ratchet."""

    def test_repo_passes_with_all_sites_allowlisted(self):
        buf = io.StringIO()
        with contextlib.redirect_stdout(buf):
            code = audit.main()
        out = buf.getvalue()
        self.assertEqual(code, 0, out)
        # The dormant P2 hazards stay enumerated, not hidden.
        self.assertIn("FransEconomicSaturationBotModule.cs", out)
        self.assertIn("FransMcvExpansionManagerBotModule.cs", out)


if __name__ == "__main__":
    unittest.main()
