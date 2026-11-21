#!/usr/bin/env python3
"""Fail if a tracked repository-root entry is not on tools/ci/root_allowlist.txt.

Reads `git ls-tree --name-only -z HEAD` (tracked top-level entries only, so
untracked build output does not count). Exit 0 = every root entry is allowed,
1 = one or more unknown root entries, 2 = git or allowlist error.
"""
import pathlib
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
ALLOWLIST = REPO / "tools" / "ci" / "root_allowlist.txt"


def read_allowlist(path: pathlib.Path) -> set:
    entries = set()
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if line and not line.startswith("#"):
            entries.add(line)
    return entries


def tracked_root_entries(repo: pathlib.Path) -> set:
    proc = subprocess.run(
        ["git", "-C", str(repo), "ls-tree", "--name-only", "-z", "HEAD"],
        capture_output=True,
    )
    if proc.returncode != 0:
        raise RuntimeError("git ls-tree failed: " + proc.stderr.decode("utf-8", "replace"))
    return {name for name in proc.stdout.decode("utf-8").split("\0") if name}


def main() -> int:
    try:
        allowed = read_allowlist(ALLOWLIST)
        tracked = tracked_root_entries(REPO)
    except (OSError, RuntimeError) as exc:
        print(f"check_root_allowlist: ERROR {exc}", file=sys.stderr)
        return 2
    unknown = sorted(tracked - allowed)
    if unknown:
        print("check_root_allowlist: FAIL unknown repository-root entries:", file=sys.stderr)
        for name in unknown:
            print("  " + name, file=sys.stderr)
        print("Move the file under a subdirectory, or add it to tools/ci/root_allowlist.txt "
              "with a DEVELOPMENT_LOG reason.", file=sys.stderr)
        return 1
    print(f"check_root_allowlist: PASS ({len(tracked)} root entries, all allowed)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
