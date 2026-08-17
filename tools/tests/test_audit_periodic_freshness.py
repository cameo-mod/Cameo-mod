"""Unit tests for tools/audit/audit_periodic_freshness.py.

Pins the two-severity contract (2026-08-11): BROKEN (a registered script or its
evidence file is missing) blocks unconditionally, while OVERDUE (a scheduled scan
is merely late) blocks only in the strict form — run_all.sh, the per-commit gate,
passes --warn-only so the calendar can never fail an unrelated commit.
"""

from __future__ import annotations

import contextlib
import datetime as dt
import io
import json
import pathlib
import tempfile
import unittest
from unittest import mock

import _bootstrap  # noqa: F401 — sys.path side effect

import audit_periodic_freshness as freshness


def build_repo(root: pathlib.Path, *, age_days: int, grace: int = 7,
               cadence: int = 14, script: str = "tools/audit/audit_security.py",
               evidence: str = "docs/audit/latest/security.md",
               create_evidence: bool = True) -> None:
    """Write a synthetic repo: the registry plus the files it points at.

    `script=""` registers a command whose script does not exist; passing
    `create_evidence=False` registers an evidence path that is never written.
    A URL evidence is never materialised (the audit must not stat it).
    """
    (root / "docs" / "audit" / "latest").mkdir(parents=True, exist_ok=True)
    (root / "tools" / "audit").mkdir(parents=True, exist_ok=True)
    wanted = [script]
    if create_evidence and not evidence.startswith(("http://", "https://")):
        wanted.append(evidence)
    for rel in wanted:
        if rel:
            path = root / rel
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("stub\n", encoding="utf-8")
    last_run = (dt.date.today() - dt.timedelta(days=age_days)).strftime("%Y-%m-%d")
    registry = {
        "grace_days": grace,
        "audits": [{
            "id": "security_scan",
            "title": "Security scan",
            "command": f"python {script}" if script else "python missing.py",
            "cadence_days": cadence,
            "last_run": last_run,
            "evidence": evidence,
            "owner": "unassigned",
        }],
    }
    (root / "docs" / "audit" / "periodic.json").write_text(
        json.dumps(registry, indent=2) + "\n", encoding="utf-8")


@contextlib.contextmanager
def run(root: pathlib.Path, argv: list[str]):
    """Run main() against `root`, yielding (exit_code, stdout)."""
    buffer = io.StringIO()
    with mock.patch.object(freshness, "find_repo_root", return_value=root), \
            mock.patch("sys.argv", ["audit_periodic_freshness.py", *argv]), \
            contextlib.redirect_stdout(buffer):
        code = freshness.main()
    yield code, buffer.getvalue()


class FreshnessStateTest(unittest.TestCase):
    def check(self, *, age_days, argv, expected_code, expected_state, **kwargs):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            build_repo(root, age_days=age_days, **kwargs)
            with run(root, argv) as (code, out):
                self.assertEqual(code, expected_code, out)
                self.assertIn(expected_state, out)
            return out

    def test_fresh_scan_is_ok(self):
        self.check(age_days=1, argv=[], expected_code=0, expected_state="ok")

    def test_past_cadence_but_within_grace_is_due_and_passes(self):
        out = self.check(age_days=16, argv=[], expected_code=0,
                         expected_state="DUE")
        self.assertIn("run these next", out)

    def test_past_cadence_and_grace_blocks_the_strict_form(self):
        self.check(age_days=30, argv=[], expected_code=1,
                   expected_state="OVERDUE")

    def test_overdue_does_not_block_warn_only(self):
        """run_all.sh must not go red because a scheduled scan is late."""
        out = self.check(age_days=30, argv=["--warn-only"], expected_code=0,
                         expected_state="OVERDUE")
        self.assertIn("does NOT block", out)

    def test_missing_script_blocks_even_warn_only(self):
        self.check(age_days=1, argv=["--warn-only"], expected_code=1,
                   expected_state="BROKEN", script="")

    def test_missing_evidence_blocks_even_warn_only(self):
        self.check(age_days=1, argv=["--warn-only"], expected_code=1,
                   expected_state="BROKEN", evidence="docs/audit/latest/gone.md",
                   create_evidence=False)

    def test_http_evidence_is_not_treated_as_a_missing_file(self):
        self.check(age_days=1, argv=[], expected_code=0, expected_state="ok",
                   evidence="https://example.invalid/report")


class RecordTest(unittest.TestCase):
    def test_record_stamps_today_and_stores_evidence(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            build_repo(root, age_days=99)
            with run(root, ["--record", "security_scan",
                            "--evidence", "docs/audit/latest/other.md"]) as (code, _):
                self.assertEqual(code, 0)
            data = json.loads((root / "docs" / "audit" / "periodic.json")
                              .read_text(encoding="utf-8"))
            entry = data["audits"][0]
            self.assertEqual(entry["last_run"], dt.date.today().strftime("%Y-%m-%d"))
            self.assertEqual(entry["evidence"], "docs/audit/latest/other.md")

    def test_unknown_id_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            build_repo(root, age_days=1)
            with run(root, ["--record", "nope"]) as (code, _):
                self.assertEqual(code, 2)


if __name__ == "__main__":
    unittest.main()
