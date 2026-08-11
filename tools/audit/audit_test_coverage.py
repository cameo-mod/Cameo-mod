#!/usr/bin/env python3
"""audit_test_coverage.py — test-coverage floor for the C# mod code and the tooling.

Cameo's only cheap correctness signal is this audit suite, and the suite itself
is untested Python. This audit measures what has a test at all and ratchets the
numbers so coverage can only go up.

Metrics:

T1 (BLOCKING) — number of NUnit ``[Test]`` cases in ``OpenRA.Mods.Cameo.Test``
    must be >= ``MIN_CS_TESTS``.
T2 (BLOCKING) — number of tests for the Python tooling (``tools/tests/test_*.py``,
    counted as ``def test_*`` functions) must be >= ``MIN_PY_TESTS``.
T3 — untested-but-testable modules: pure-logic modules with no matching test.
    A ``tools/`` module counts as tested when ``tools/tests/`` contains a test
    file naming it, or when it imports nothing but the stdlib and is itself a
    test. C# classes count as tested when a ``*Test.cs`` mentions the type.

The suite runs this statically (no build). The periodic run must ALSO execute
the real suites and paste the result into the evidence file:

    dotnet test OpenRA.Mods.Cameo.Test/OpenRA.Mods.Cameo.Test.csproj -c Release
    python -m unittest discover -s tools/tests -t tools/tests

Exit code 1 when T1/T2 fall below their floors, or T3 rises above its baseline.

Usage: python tools/audit/audit_test_coverage.py
"""

from __future__ import annotations

import pathlib
import re
import sys

from miniyaml import find_repo_root
from report import h1, h2, relpath, table
from scanning import iter_files

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

CS_TEST_DIR = "OpenRA.Mods.Cameo.Test"
CS_SOURCE_DIRS = ("OpenRA.Mods.Cameo",)
PY_TEST_DIR = "tools/tests"
PY_SOURCE_DIRS = ("tools/audit", "tools/balance", "tools/packs", "tools/rename")

# Floors/ratchet re-measured 2026-08-11 (was 24/51/221 on 2026-08-10). Raise the
# floors as tests are added; lower T3 as modules get covered. The counts only
# consider git-TRACKED files (scanning.tracked_under), so a scratch script left in
# tools/ can no longer move them.
MIN_CS_TESTS = 24
MIN_PY_TESTS = 105      # +9: test_ledger_split (W3)
T3_BASELINE = 218

CS_TEST_ATTR = re.compile(r"^\s*\[(?:Test|TestCase|TestCaseSource)\b", re.MULTILINE)
CS_TYPE = re.compile(r"^\s*(?:public|internal)\s+(?:sealed\s+|abstract\s+|static\s+|partial\s+)*"
                     r"(?:class|struct|record)\s+(\w+)", re.MULTILINE)
PY_TEST_DEF = re.compile(r"^\s*def\s+(test_\w+)", re.MULTILINE)


def main() -> int:
    root = find_repo_root()

    cs_test_text = ""
    cs_tests = 0
    cs_test_files = 0
    for path in iter_files(root / CS_TEST_DIR, ".cs"):
        cs_test_files += 1
        text = path.read_text(encoding="utf-8", errors="replace")
        cs_test_text += text
        cs_tests += len(CS_TEST_ATTR.findall(text))

    py_test_text = ""
    py_tests = 0
    py_test_files = 0
    for path in iter_files(root / PY_TEST_DIR, ".py"):
        py_test_files += 1
        text = path.read_text(encoding="utf-8", errors="replace")
        py_test_text += text
        py_tests += len(PY_TEST_DEF.findall(text))

    untested: list[list[str]] = []

    for rel_dir in CS_SOURCE_DIRS:
        for path in iter_files(root / rel_dir, ".cs"):
            rel = relpath(str(path), root)
            text = path.read_text(encoding="utf-8", errors="replace")
            types = [t for t in CS_TYPE.findall(text) if not t.endswith("Info")]
            if not types:
                continue
            if not any(re.search(rf"\b{re.escape(t)}\b", cs_test_text) for t in types):
                untested.append(["C#", rel, ", ".join(types[:3])])

    for rel_dir in PY_SOURCE_DIRS:
        for path in iter_files(root / rel_dir, ".py"):
            rel = relpath(str(path), root)
            module = path.stem
            if not re.search(rf"\b{re.escape(module)}\b", py_test_text):
                untested.append(["python", rel, module])

    print(h1("audit_test_coverage — test floors and untested modules"))
    print(table(["metric", "meaning", "value", "floor/baseline"], [
        ["T1", f"NUnit [Test] cases in {CS_TEST_DIR} ({cs_test_files} file(s))",
         cs_tests, f">= {MIN_CS_TESTS}"],
        ["T2", f"`def test_*` in {PY_TEST_DIR} ({py_test_files} file(s))",
         py_tests, f">= {MIN_PY_TESTS}"],
        ["T3", "modules with no test mentioning them", len(untested),
         f"<= {T3_BASELINE}"],
    ]))

    print(h2("How to run the real suites (periodic run must paste output here)"))
    print("```\n"
          "dotnet test OpenRA.Mods.Cameo.Test/OpenRA.Mods.Cameo.Test.csproj -c Release\n"
          "python -m unittest discover -s tools/tests -t tools/tests\n"
          "```\n")

    print(h2(f"T3 — untested modules ({len(untested)})"))
    print(table(["kind", "file", "type(s)/module"], untested))

    failures = []
    if cs_tests < MIN_CS_TESTS:
        failures.append(f"T1: {cs_tests} NUnit tests < floor {MIN_CS_TESTS}")
    if py_tests < MIN_PY_TESTS:
        failures.append(f"T2: {py_tests} python tests < floor {MIN_PY_TESTS}")
    if len(untested) > T3_BASELINE:
        failures.append(f"T3: {len(untested)} untested > baseline {T3_BASELINE}")

    if failures:
        print(h2("FAIL"))
        for line in failures:
            print(f"- {line}")
        print()
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
