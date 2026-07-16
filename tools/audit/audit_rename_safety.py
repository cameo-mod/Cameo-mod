#!/usr/bin/env python3
"""Verify that the unsafe legacy rename applicator fails closed."""

from __future__ import annotations

import ast
import pathlib

from report import h1, h2, table


ROOT = pathlib.Path(__file__).resolve().parents[2]
APPLICATOR = ROOT / "tools" / "rename" / "apply.py"


def main() -> int:
    tree = ast.parse(APPLICATOR.read_text(encoding="utf-8"), filename=str(APPLICATOR))
    functions = [
        node for node in tree.body
        if isinstance(node, ast.FunctionDef) and node.name == "main"
    ]
    safe = False
    if len(functions) == 1 and len(functions[0].body) >= 2:
        message, stop = functions[0].body[:2]
        safe = (
            isinstance(message, ast.Expr) and
            isinstance(message.value, ast.Call) and
            isinstance(message.value.func, ast.Name) and
            message.value.func.id == "print" and
            isinstance(stop, ast.Return) and
            isinstance(stop.value, ast.Constant) and
            stop.value.value == 2
        )

    print(h1("audit_rename_safety — legacy applicator guard"))
    print(h2("R1 — context-blind rename applicator must fail closed"))
    if safe:
        print("_guard present: main prints an error and returns 2 before legacy code_\n")
        return 0

    print(table(
        ["location", "problem"],
        [["tools/rename/apply.py", "main is not guarded by an immediate return 2"]],
    ))
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
