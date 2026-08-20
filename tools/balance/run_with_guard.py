#!/usr/bin/env python3
"""run_with_guard.py — run a Python script with syntax pre-check + timeout.

Usage:
    python tools/balance/run_with_guard.py tools/balance/scout_rebalance_proposal.py

What it does:
1. py_compile the target file and print syntax errors immediately.
2. Run the script with a 60s timeout.
3. Print stdout/stderr and the exit code.
4. If it times out, say so clearly instead of hanging forever.
"""
from __future__ import annotations

import pathlib
import py_compile
import subprocess
import sys
import tempfile


def run(target: pathlib.Path, timeout: float = 60.0) -> int:
    if not target.exists():
        print(f"ERROR: {target} does not exist", file=sys.stderr)
        return 2

    print(f"[guard] syntax-checking {target} ...")
    try:
        py_compile.compile(str(target), doraise=True)
    except py_compile.PyCompileError as e:
        print(f"[guard] SYNTAX ERROR in {target}:")
        print(e)
        return 1
    print("[guard] syntax OK")

    print(f"[guard] running {target} (timeout={timeout}s) ...")
    try:
        # Stream stdout/stderr live so progress is visible.
        proc = subprocess.Popen(
            [sys.executable, str(target)] + sys.argv[2:],
            stdout=sys.stdout,
            stderr=sys.stderr,
        )
        try:
            exit_code = proc.wait(timeout=timeout)
        except subprocess.TimeoutExpired:
            proc.kill()
            proc.wait()
            print(f"[guard] TIMEOUT after {timeout}s — script did not finish", file=sys.stderr)
            return 124
    except Exception as e:
        print(f"[guard] failed to launch {target}: {e}", file=sys.stderr)
        return 2

    print(f"[guard] exit code: {exit_code}")
    return exit_code


if __name__ == "__main__":
    target = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else None
    if not target:
        print("Usage: run_with_guard.py <script.py>", file=sys.stderr)
        sys.exit(2)
    sys.exit(run(target))
