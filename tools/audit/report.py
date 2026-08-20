#!/usr/bin/env python3
"""report.py — tiny markdown helpers shared by the audit scripts."""

from __future__ import annotations

import pathlib
import sys

# Windows consoles default to cp1252; all audit output is UTF-8.
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")


def relpath(path: str, root: pathlib.Path) -> str:
    try:
        return str(pathlib.Path(path).relative_to(root)).replace("\\", "/")
    except ValueError:
        return str(path).replace("\\", "/")


def table(headers: list[str], rows: list[list[str]]) -> str:
    if not rows:
        return "_none found_\n"
    out = ["| " + " | ".join(headers) + " |",
           "|" + "|".join("---" for _ in headers) + "|"]
    for r in rows:
        out.append("| " + " | ".join(str(c).replace("|", "\\|") for c in r) + " |")
    return "\n".join(out) + "\n"


def h1(t): return f"# {t}\n"
def h2(t): return f"\n## {t}\n"
def h3(t): return f"\n### {t}\n"
