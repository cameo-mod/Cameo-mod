#!/usr/bin/env python3
"""audit_periodic_freshness.py — staleness gate for the mandatory recurring audits.

Some checks are too slow, too network-bound or too judgement-heavy to run on
every commit (code duplication, test coverage, recent-changes review, error
handling, security scan). They are registered in ``docs/audit/periodic.json``
with a cadence; this audit makes "run it again after a while" enforceable:

- OVERDUE (cadence + grace exceeded)  -> exit 1.
- DUE (cadence exceeded, within grace) -> reported, exit 0.
- BROKEN (missing script or missing evidence file) -> exit 1 ALWAYS.

Two severities, deliberately separated (2026-08-11):

- **BROKEN** is a real defect in the tree right now — a registered audit script or
  its evidence file is gone. It blocks unconditionally, including under
  ``--warn-only``.
- **OVERDUE** is only a calendar fact: the tree is fine, a scheduled scan is simply
  late. ``run_all.sh`` is the PER-COMMIT gate, so blocking a commit on the calendar
  ambushes whoever happens to be working that day for a reason unrelated to their
  change. run_all.sh therefore calls this with ``--warn-only``; the strict form
  (no flag) is what a scheduled/CI enforcement run uses.

Stamp a completed run (updates ``last_run`` in the registry):

    python tools/audit/audit_periodic_freshness.py --record security_scan \
        --evidence docs/audit/latest/security.md

Enforce (exit 1 when a scan is late) — the scheduled run, not the commit gate:

    python tools/audit/audit_periodic_freshness.py

Usage: python tools/audit/audit_periodic_freshness.py [--warn-only]
                                                      [--record ID [--evidence P]]
"""

from __future__ import annotations

import argparse
import datetime as dt
import json
import pathlib
import sys

from miniyaml import find_repo_root
from report import h1, h2, table

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

REGISTRY = "docs/audit/periodic.json"
DATE_FMT = "%Y-%m-%d"


def load_registry(root: pathlib.Path) -> dict:
    path = root / REGISTRY
    if not path.exists():
        raise SystemExit(f"missing registry: {REGISTRY}")
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise SystemExit(f"{REGISTRY} is not valid JSON: {exc}") from exc


def save_registry(root: pathlib.Path, data: dict) -> None:
    path = root / REGISTRY
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n",
                    encoding="utf-8")


def parse_date(value: str, audit_id: str) -> dt.date:
    try:
        return dt.datetime.strptime(value, DATE_FMT).date()
    except (TypeError, ValueError) as exc:
        raise SystemExit(f"audit '{audit_id}': bad last_run {value!r} "
                         f"(expected {DATE_FMT})") from exc


def record(root: pathlib.Path, audit_id: str, evidence: str | None) -> int:
    data = load_registry(root)
    for entry in data["audits"]:
        if entry["id"] == audit_id:
            entry["last_run"] = dt.date.today().strftime(DATE_FMT)
            if evidence:
                entry["evidence"] = evidence
            save_registry(root, data)
            print(f"recorded {audit_id} as run on {entry['last_run']}")
            return 0
    known = ", ".join(e["id"] for e in data["audits"])
    print(f"unknown audit id {audit_id!r}; known ids: {known}", file=sys.stderr)
    return 2


def main() -> int:
    parser = argparse.ArgumentParser(add_help=True)
    parser.add_argument("--record", metavar="ID",
                        help="stamp ID as run today and exit")
    parser.add_argument("--evidence", metavar="PATH",
                        help="evidence path/url to store with --record")
    parser.add_argument("--warn-only", action="store_true",
                        help="do not exit 1 on calendar staleness (BROKEN entries "
                             "still fail); used by run_all.sh, the per-commit gate")
    args, _unknown = parser.parse_known_args()

    root = find_repo_root()
    if args.record:
        return record(root, args.record, args.evidence)

    data = load_registry(root)
    grace = int(data.get("grace_days", 7))
    today = dt.date.today()

    rows: list[list[str]] = []
    overdue: list[str] = []
    due: list[str] = []
    broken: list[str] = []

    for entry in data["audits"]:
        audit_id = entry["id"]
        last = parse_date(entry.get("last_run"), audit_id)
        cadence = int(entry["cadence_days"])
        age = (today - last).days
        due_in = cadence - age

        problems = []
        script = entry["command"].split()[1] if " " in entry["command"] else ""
        if script and not (root / script).exists():
            problems.append(f"script missing: {script}")
        evidence = entry.get("evidence", "")
        if evidence and not evidence.startswith(("http://", "https://")) \
                and not (root / evidence).exists():
            problems.append(f"evidence missing: {evidence}")

        if problems:  # a real defect in the tree, not a calendar fact -> always blocks
            state = "BROKEN"
            broken.append(f"{audit_id} ({'; '.join(problems)})")
        elif age > cadence + grace:
            state = "OVERDUE"
            overdue.append(f"{audit_id} ({age}d old, cadence {cadence}d)")
        elif age > cadence:
            state = "DUE"
            due.append(f"{audit_id} ({age}d old, cadence {cadence}d)")
        else:
            state = "ok"

        rows.append([audit_id, entry["title"], str(cadence), str(age),
                     str(due_in), state, entry.get("owner", "unassigned")])

    print(h1("audit_periodic_freshness — mandatory recurring audits"))
    print(f"Registry: `{REGISTRY}` — grace **{grace}** days. "
          f"BROKEN: **{len(broken)}**, OVERDUE: **{len(overdue)}**, "
          f"DUE: **{len(due)}**\n")
    print(table(["id", "title", "cadence (d)", "age (d)", "due in (d)",
                 "state", "owner"], rows))

    if due:
        print(h2("DUE — run these next"))
        for item in due:
            print(f"- {item}")
        print()

    if overdue:
        print(h2("OVERDUE — a scheduled scan is late"))
        for item in overdue:
            print(f"- {item}")
        print("\nRun the command from the registry, then stamp it with "
              "`--record <id>`.")
        print("The tree itself is fine — this is a calendar fact, so it does "
              "NOT block the per-commit suite.\n" if args.warn_only else "")

    if broken:
        print(h2("BROKEN — blocking"))
        for item in broken:
            print(f"- {item}")
        print("\nA registered audit's script or evidence file is missing: fix the "
              "registry or restore the file.\n")
        return 1

    if overdue and not args.warn_only:
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
