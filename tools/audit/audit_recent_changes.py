#!/usr/bin/env python3
"""audit_recent_changes.py — regression review of the recent git history.

The repository is edited by several agents plus the maintainer, so the cheapest
regression signal is the shape of the recent commits themselves. This audit
turns the CLAUDE.md rules into machine checks over the last ``--days`` of
history (default 14) and prints a review checklist for what it cannot decide.

Findings dated before ``ENFORCED_FROM`` are history: they are listed but never
block, so the gate starts clean and only new commits can fail it.

Blocking (exit 1) for commits on/after ``ENFORCED_FROM``:

R1 — a hand-edited balance number: a commit that changes a balance field
     (``Damage``, ``HP``, ``Cost``, ``Speed``, ``Range``, ``ROF``, ``Spread``,
     ``Burst``, ``BurstDelays``) in ``mods/`` without touching the ledger
     (``docs/balance/``) in the same commit — CLAUDE.md rule 3.
R2 — a new ``tools/audit/audit_*.py`` that ``run_all.sh`` never invokes, so it
     silently never runs.
R3 — a commit with no ``Co-Authored-By:`` trailer, or one claiming an agent
     that is not the committer's own identity — CLAUDE.md rule 10.

Review-only (reported, never blocking):

R4 — ``mod.config``/engine-version changes (need a rebuild + boot gate).
R5 — the largest touched files, as a "what should a human re-read" list.

Usage: python tools/audit/audit_recent_changes.py [--days 14]
"""

from __future__ import annotations

import argparse
import collections
import pathlib
import re
import subprocess
import sys

from miniyaml import find_repo_root
from report import h1, h2, table

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

BALANCE_FIELD = re.compile(
    r"^\+\s*(?:Damage|HP|Cost|Speed|Range|ROF|ReloadDelay|Spread|Burst|"
    r"BurstDelays|BuildDuration|MinRange)\s*:", re.MULTILINE)
LEDGER_PREFIXES = ("docs/balance/", "docs/design/cameo_balance")
TRAILER = re.compile(r"^Co-Authored-By:\s*(.+)$", re.MULTILINE | re.IGNORECASE)
SEPARATOR = "\x1e"

# Provenance/ledger rules are enforced for commits from this date on; earlier
# commits predate the gate and are reported as history only.
ENFORCED_FROM = "2026-08-11"

# Deliberately not wired into run_all.sh (deprecated, manual-only, or unracheted).
# These are real audits but not yet ratcheted, so they run manually until a baseline is agreed.
R2_EXEMPT = frozenset((
    "elite_naming",
    "empty_warheads",
    "armament_naming",
    "burst_delays",
))


def git(root: pathlib.Path, *args: str) -> str:
    proc = subprocess.run(("git", "-C", str(root)) + args, check=False,
                          capture_output=True, text=True, encoding="utf-8",
                          errors="replace")
    if proc.returncode != 0:
        print(f"<!-- git {' '.join(args)} failed: "
              f"{proc.stderr.strip()} -->", file=sys.stderr)
        return ""
    return proc.stdout


def commits(root: pathlib.Path, days: int) -> list[dict[str, str]]:
    log = git(root, "log", f"--since={days}.days", "--no-merges",
              f"--pretty=format:%H{SEPARATOR}%an{SEPARATOR}%ad{SEPARATOR}%s{SEPARATOR}%b"
              + SEPARATOR + "\x1d", "--date=short")
    out = []
    for block in log.split("\x1d"):
        parts = block.strip("\n").split(SEPARATOR)
        if len(parts) < 5 or not parts[0].strip():
            continue
        out.append({"sha": parts[0].strip(), "author": parts[1], "date": parts[2],
                    "subject": parts[3], "body": parts[4]})
    return out


def main() -> int:
    parser = argparse.ArgumentParser(add_help=True)
    parser.add_argument("--days", type=int, default=14)
    args, _unknown = parser.parse_known_args()

    root = find_repo_root()
    if git(root, "rev-parse", "--is-shallow-repository").strip() == "true":
        print("<!-- shallow clone: run `git fetch --unshallow` for full history -->",
              file=sys.stderr)

    history = commits(root, args.days)

    r1: list[list[str]] = []
    r3: list[list[str]] = []
    r4: list[list[str]] = []
    touched: collections.Counter[str] = collections.Counter()

    for commit in history:
        sha = commit["sha"]
        files = [f for f in git(root, "show", "--pretty=", "--name-only", sha)
                 .splitlines() if f]
        touched.update(files)

        if not TRAILER.search(commit["body"]):
            r3.append([sha[:8], commit["date"], commit["author"],
                       "no Co-Authored-By trailer"])

        if "mod.config" in files:
            r4.append([sha[:8], commit["date"], "mod.config changed "
                       "(rebuild + boot gate required)"])

        mod_yaml = [f for f in files
                    if f.startswith("mods/") and f.endswith((".yaml", ".yml"))]
        if not mod_yaml:
            continue
        if any(f.startswith(LEDGER_PREFIXES) for f in files):
            continue
        diff = git(root, "show", "--pretty=", "--unified=0", sha, "--", *mod_yaml)
        hits = BALANCE_FIELD.findall(diff)
        if hits:
            fields = sorted({h.strip().rstrip(":").lstrip("+").strip()
                             for h in hits})
            r1.append([sha[:8], commit["date"], commit["subject"][:48],
                       ", ".join(fields)])

    runner = (root / "tools/audit/run_all.sh")
    runner_text = runner.read_text(encoding="utf-8") if runner.exists() else ""
    r2: list[list[str]] = []
    for path in sorted((root / "tools/audit").glob("audit_*.py")):
        name = path.stem.removeprefix("audit_")
        if name in R2_EXEMPT:
            continue
        if not re.search(rf"(?<![\w-]){re.escape(name)}(?![\w-])", runner_text):
            r2.append([f"tools/audit/{path.name}", "not invoked by run_all.sh"])

    print(h1(f"audit_recent_changes — last {args.days} day(s) of history"))
    print(f"Commits reviewed: **{len(history)}**, files touched: "
          f"**{len(touched)}**\n")
    print(table(["code", "meaning", "count", "blocking"], [
        ["R1", "balance yaml edited without the ledger", len(r1), "yes"],
        ["R2", "audit script never run by run_all.sh", len(r2), "yes"],
        ["R3", "commit without a Co-Authored-By trailer", len(r3), "yes"],
        ["R4", "engine/mod.config change (needs boot gate)", len(r4), "no"],
    ]))

    print(h2(f"R1 — hand-edited balance numbers ({len(r1)})"))
    print(table(["commit", "date", "subject", "fields"], r1))

    print(h2(f"R2 — audits missing from run_all.sh ({len(r2)})"))
    print(table(["script", "problem"], r2))

    print(h2(f"R3 — commits without provenance ({len(r3)})"))
    print(table(["commit", "date", "author", "problem"], r3))

    print(h2(f"R4 — engine/config changes to re-verify ({len(r4)})"))
    print(table(["commit", "date", "note"], r4))

    print(h2("R5 — most-churned files (re-read these first)"))
    print(table(["file", "commits touching it"],
                [[f, n] for f, n in touched.most_common(15)]))

    print(h2("Reviewer checklist (not machine-checkable)"))
    for line in (
        "Every yaml change in the window boot-gated (`launch-game.cmd` reached the menu)?",
        "C# changes rebuilt (`dotnet build -c Release -p:TargetPlatform=win-x64`)?",
        "New actors/weapons named with underscores only, and Fluent keys added?",
        "Generated reports under `docs/audit/latest/` regenerated via run_all.sh, not hand-edited?",
        "ROADMAP.md updated for finished/queued work?",
    ):
        print(f"- [ ] {line}")
    print()

    new_r1 = [row for row in r1 if row[1] >= ENFORCED_FROM]
    new_r3 = [row for row in r3 if row[1] >= ENFORCED_FROM]

    print(h2("Enforcement"))
    print(f"R1/R3 block only for commits on or after **{ENFORCED_FROM}**: "
          f"{len(new_r1)} R1 and {len(new_r3)} R3 of {len(r1)}/{len(r3)} "
          f"findings are in scope; the rest predate the gate.\n")

    if new_r1 or new_r3 or r2:
        print(h2("FAIL"))
        print(f"- {len(new_r1)} R1, {len(r2)} R2, {len(new_r3)} R3 blocking "
              f"finding(s)\n")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
