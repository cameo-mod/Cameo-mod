#!/usr/bin/env python3
"""audit_error_handling.py — error-handling lint for the Python tooling.

The audit/balance tooling is the safety net for a mod that cannot be
boot-tested cheaply, so a tool that swallows an error is worse than a tool
that crashes: a silently skipped file reports "0 findings" and the bug ships.

Findings (all AST-based, so comments and strings never trigger them):

E1 (BLOCKING) — ``except:`` / ``except BaseException`` bare handler.
E2 (BLOCKING) — handler whose body is only ``pass``/``continue``/``...``:
    the error is discarded without a message or a recorded finding.
E3 — ``open()`` without ``encoding=`` (cp1252 vs utf-8 mangles the reports;
    see CLAUDE.md rule 8) in read/write text mode.
E4 — ``subprocess`` call without a checked result (no ``check=``,
    not ``run``/``check_output`` result inspection) — a failed child process
    reads as empty output.

Exit code 1 when any count rises above its baseline (ratchet: lower the
baselines as findings are fixed, never raise them).

Usage: python tools/audit/audit_error_handling.py
"""

from __future__ import annotations

import ast
import pathlib
import sys

from miniyaml import find_repo_root
from report import h1, h2, relpath, table

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

SCAN_DIRS = ("tools",)
SKIP_PARTS = ("archive", "__pycache__")

# Ratchet baselines, measured 2026-08-10. Lower as findings are fixed.
BASELINES = {"E1": 2, "E2": 30, "E3": 90, "E4": 9}

TEXT_MODES = ("r", "w", "a", "r+", "w+", "a+", "rt", "wt", "at")


class Visitor(ast.NodeVisitor):
    def __init__(self, rel: str) -> None:
        self.rel = rel
        self.findings: list[tuple[str, str, int, str]] = []

    def add(self, code: str, line: int, detail: str) -> None:
        self.findings.append((code, self.rel, line, detail))

    def visit_ExceptHandler(self, node: ast.ExceptHandler) -> None:  # noqa: N802
        if node.type is None:
            self.add("E1", node.lineno, "bare `except:`")
        elif isinstance(node.type, ast.Name) and node.type.id == "BaseException":
            self.add("E1", node.lineno, "`except BaseException`")

        body = node.body
        swallowed = all(
            isinstance(stmt, (ast.Pass, ast.Continue))
            or (isinstance(stmt, ast.Expr) and isinstance(stmt.value, ast.Constant)
                and stmt.value.value is Ellipsis)
            for stmt in body)
        if swallowed:
            self.add("E2", node.lineno, "handler body discards the error")
        self.generic_visit(node)

    def visit_Call(self, node: ast.Call) -> None:  # noqa: N802
        name = _callee(node.func)
        kwargs = {kw.arg for kw in node.keywords if kw.arg}

        short = name.split(".")[-1]
        if short in ("open", "read_text", "write_text"):
            mode = None
            if short == "open" and len(node.args) > 1 and isinstance(node.args[1], ast.Constant):
                mode = node.args[1].value
            binary = isinstance(mode, str) and "b" in mode
            if not binary and "encoding" not in kwargs:
                self.add("E3", node.lineno, f"`{name}()` without encoding=")

        if name.startswith("subprocess."):
            base = name.split(".")[-1]
            if base in ("run", "call", "Popen") and "check" not in kwargs:
                self.add("E4", node.lineno, f"`{name}()` without check=")
        self.generic_visit(node)


def _callee(func: ast.expr) -> str:
    if isinstance(func, ast.Name):
        return func.id
    if isinstance(func, ast.Attribute):
        return f"{_callee(func.value)}.{func.attr}" if isinstance(
            func.value, (ast.Name, ast.Attribute)) else func.attr
    return ""


def main() -> int:
    root = find_repo_root()
    findings: list[tuple[str, str, int, str]] = []
    scanned = 0
    unparsed: list[list[str]] = []

    for scan in SCAN_DIRS:
        for path in sorted((root / scan).rglob("*.py")):
            if any(part in SKIP_PARTS for part in path.parts):
                continue
            scanned += 1
            rel = relpath(str(path), root)
            source = path.read_text(encoding="utf-8", errors="replace")
            try:
                tree = ast.parse(source, filename=rel)
            except SyntaxError as exc:
                unparsed.append([rel, str(exc.lineno or 0), str(exc.msg)])
                continue
            visitor = Visitor(rel)
            visitor.visit(tree)
            findings.extend(visitor.findings)

    counts = {code: sum(1 for f in findings if f[0] == code)
              for code in BASELINES}

    print(h1("audit_error_handling — Python tooling error handling"))
    print(f"Files scanned: **{scanned}**\n")
    print(table(["code", "meaning", "count", "baseline"], [
        ["E1", "bare except / except BaseException", counts["E1"], BASELINES["E1"]],
        ["E2", "handler discards the error", counts["E2"], BASELINES["E2"]],
        ["E3", "open() without encoding=", counts["E3"], BASELINES["E3"]],
        ["E4", "subprocess call without check=", counts["E4"], BASELINES["E4"]],
    ]))

    if unparsed:
        print(h2("Files that do not parse"))
        print(table(["file", "line", "error"], unparsed))

    for code in ("E1", "E2", "E3", "E4"):
        rows = [[f[1], str(f[2]), f[3]] for f in findings if f[0] == code]
        print(h2(f"{code} — {len(rows)} finding(s)"))
        print(table(["file", "line", "detail"], rows))

    regressions = [f"{code}: {counts[code]} > baseline {BASELINES[code]}"
                   for code in BASELINES if counts[code] > BASELINES[code]]
    if unparsed:
        regressions.append(f"{len(unparsed)} file(s) do not parse")

    if regressions:
        print(h2("FAIL"))
        for line in regressions:
            print(f"- {line}")
        print()
        return 1

    improved = [f"{code}: {counts[code]} < baseline {BASELINES[code]}"
                for code in BASELINES if counts[code] < BASELINES[code]]
    if improved:
        print(h2("Baselines can be lowered"))
        for line in improved:
            print(f"- {line}")
        print()
    return 0


if __name__ == "__main__":
    sys.exit(main())
