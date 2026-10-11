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
    Scans both ``OpenRA.Mods.Cameo`` and ``OpenRA.Mods.CA``.
T4 — per-file line-coverage ratchet (only with ``--coverage-xml``): every
    bot-module file recorded in ``tools/tests/coverage_baseline.json`` must
    hold its baseline line-rate within ``T4_EPSILON`` points. Produce the XML
    and baseline with ``tools/ai/coverage_report.py`` (dotnet-coverage).

The suite runs this statically (no build). The periodic run must ALSO execute
the real suites and paste the result into the evidence file:

    dotnet test OpenRA.Mods.Cameo.Test/OpenRA.Mods.Cameo.Test.csproj -c Release
    python -m unittest discover -s tools/tests -t tools/tests

Exit code 1 when T1/T2 fall below their floors, T3 rises above its baseline,
or (when --coverage-xml is given) any baselined file's coverage regresses.

Usage:
    python tools/audit/audit_test_coverage.py
    python tools/audit/audit_test_coverage.py --coverage-xml TestResults/coverage.cobertura.xml
"""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
import xml.etree.ElementTree as ET

from miniyaml import find_repo_root
from report import h1, h2, relpath, table
from scanning import iter_files

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

CS_TEST_DIR = "OpenRA.Mods.Cameo.Test"
CS_SOURCE_DIRS = ("OpenRA.Mods.Cameo", "OpenRA.Mods.CA")
PY_TEST_DIR = "tools/tests"
PY_SOURCE_DIRS = ("tools/audit", "tools/balance", "tools/packs", "tools/rename")

# Floors/ratchet re-measured 2026-10-04 at their true values (stale placeholders
# 24/177 since 2026-08-11, originally 24/51/221). Raise the floors as tests are
# added; a test legitimately retired lowers the floor in the same commit with
# justification. The counts only consider git-TRACKED files
# (scanning.tracked_under), so a scratch script left in tools/ cannot move them.
MIN_CS_TESTS = 933
MIN_PY_TESTS = 3076
# T3 re-baselined 224 -> 496 on 2026-10-04 when the scan grew to cover
# OpenRA.Mods.CA (previously only OpenRA.Mods.Cameo). Same rule as before: the
# baseline follows reality rather than holding the suite red, and the CA debt
# is recorded — bot modules there owe tests. History: 215 -> 218 (b6fb33bb9)
# -> 224 (2026-08-15, W20/W21/W11) -> 496 (2026-10-04, CA scan).
T3_BASELINE = 496

# T4: a baselined file may lose at most this many line-rate points before the
# ratchet fails. 0.1 absorbs float rounding in the stored baseline only.
T4_EPSILON = 0.1
COVERAGE_BASELINE = "tools/tests/coverage_baseline.json"

CS_TEST_ATTR = re.compile(r"^\s*\[(?:Test|TestCase|TestCaseSource)\b", re.MULTILINE)
CS_TYPE = re.compile(r"^\s*(?:public|internal)\s+(?:sealed\s+|abstract\s+|static\s+|partial\s+)*"
                     r"(?:class|struct|record)\s+(\w+)", re.MULTILINE)
PY_TEST_DEF = re.compile(r"^\s*def\s+(test_\w+)", re.MULTILINE)


def parse_cobertura(xml_path: pathlib.Path, root: pathlib.Path) -> dict[str, float]:
    """Cobertura XML -> {repo-relative path: line-rate percent}.

    A source file with several CLR types yields several <class> records —
    accumulate lines per filename, never overwrite (last-type-wins hides hits).
    """
    totals: dict[str, list[int]] = {}
    for cls in ET.parse(xml_path).getroot().findall(".//class"):
        fn = cls.get("filename")
        if not fn:
            continue
        p = pathlib.PurePath(fn)
        try:
            rel = p.relative_to(root).as_posix()
        except ValueError:
            rel = p.as_posix()
        hit = total = 0
        for ln in cls.findall(".//line"):
            total += 1
            if int(ln.get("hits", "0")) > 0:
                hit += 1
        rec = totals.setdefault(rel, [0, 0])
        rec[0] += hit
        rec[1] += total
    return {rel: 100.0 * h / t for rel, (h, t) in totals.items() if t}


def ratchet_regressions(baseline: dict, current: dict,
                        epsilon: float = T4_EPSILON) -> tuple[list, list]:
    """Compare per-file line-rates: returns (regressions, removed_files).

    A regression is a baselined file whose current line-rate dropped more than
    ``epsilon`` points below its baseline. Files absent from ``current`` are
    reported as removed, not regressed (they may be renames — human call).
    """
    regressions, removed = [], []
    for rel, rec in baseline.items():
        if rel not in current:
            removed.append(rel)
        elif current[rel] < rec["line_rate"] - epsilon:
            regressions.append([rel, f"{rec['line_rate']:.2f}", f"{current[rel]:.2f}"])
    return regressions, removed


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--coverage-xml", help="Cobertura XML for the T4 per-file ratchet")
    ap.add_argument("--coverage-baseline", default=COVERAGE_BASELINE)
    args = ap.parse_args()
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

    regressions: list[list[str]] = []
    new_files: list[str] = []
    removed_files: list[str] = []
    t4_state = "not run (no --coverage-xml)"
    baseline: dict = {}
    current: dict = {}
    if args.coverage_xml:
        baseline_path = root / args.coverage_baseline
        if not baseline_path.is_file():
            print(h2("FAIL"))
            print(f"- T4: baseline {args.coverage_baseline} missing — generate it with")
            print(f"  python tools/ai/coverage_report.py --xml <xml> --write-baseline {args.coverage_baseline}")
            return 1
        baseline = json.loads(baseline_path.read_text(encoding="utf-8"))["files"]
        current = parse_cobertura(pathlib.Path(args.coverage_xml), root)
        # Only bot-module-scope files can ever enter the baseline — the "new"
        # list filters out the engine's other ~2000 covered files.
        sys.path.insert(0, str(root / "tools" / "ai"))
        from coverage_report import in_scope  # noqa: E402
        regressions, removed_files = ratchet_regressions(baseline, current)
        new_files = sorted(r for r in set(current) - set(baseline)
                           if in_scope(r.replace("/", "\\")))
        t4_state = (f"{len(regressions)} regression(s), {len(new_files)} new, "
                    f"{len(removed_files)} removed, {len(baseline)} baselined")

    print(h1("audit_test_coverage — test floors and untested modules"))
    print(table(["metric", "meaning", "value", "floor/baseline"], [
        ["T1", f"NUnit [Test] cases in {CS_TEST_DIR} ({cs_test_files} file(s))",
         cs_tests, f">= {MIN_CS_TESTS}"],
        ["T2", f"`def test_*` in {PY_TEST_DIR} ({py_test_files} file(s))",
         py_tests, f">= {MIN_PY_TESTS}"],
        ["T3", "modules with no test mentioning them", len(untested),
         f"<= {T3_BASELINE}"],
        ["T4", "per-file line-coverage ratchet (bot modules)", t4_state,
         "no baselined file loses coverage"],
    ]))

    print(h2("How to run the real suites (periodic run must paste output here)"))
    print("```\n"
          "dotnet test OpenRA.Mods.Cameo.Test/OpenRA.Mods.Cameo.Test.csproj -c Release\n"
          "python -m unittest discover -s tools/tests -t tools/tests\n"
          "# line/branch coverage (needs: dotnet tool install -g dotnet-coverage)\n"
          "python tools/ai/coverage_report.py --collect\n"
          "python tools/audit/audit_test_coverage.py --coverage-xml TestResults/coverage.cobertura.xml\n"
          "```\n")

    print(h2(f"T3 — untested modules ({len(untested)})"))
    print(table(["kind", "file", "type(s)/module"], untested))

    if args.coverage_xml:
        print(h2("T4 — per-file line-coverage ratchet"))
        if regressions:
            print(table(["file", "baseline %", "current %"], regressions))
        else:
            print("No baselined file lost coverage.")
        if new_files:
            print(f"New covered files ({len(new_files)}) — fold into the baseline:")
            for rel in new_files:
                print(f"- `{rel}`")
        if removed_files:
            print(f"Removed files ({len(removed_files)}) — drop from the baseline:")
            for rel in removed_files:
                print(f"- `{rel}`")

    failures = []
    if cs_tests < MIN_CS_TESTS:
        failures.append(f"T1: {cs_tests} NUnit tests < floor {MIN_CS_TESTS}")
    if py_tests < MIN_PY_TESTS:
        failures.append(f"T2: {py_tests} python tests < floor {MIN_PY_TESTS}")
    if len(untested) > T3_BASELINE:
        failures.append(f"T3: {len(untested)} untested > baseline {T3_BASELINE}")
    if regressions:
        failures.append(f"T4: {len(regressions)} file(s) lost coverage beyond {T4_EPSILON}pt")

    if failures:
        print(h2("FAIL"))
        for line in failures:
            print(f"- {line}")
        print()
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
