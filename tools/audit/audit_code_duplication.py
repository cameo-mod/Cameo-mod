#!/usr/bin/env python3
"""audit_code_duplication.py — copy-paste detector for the tooling and the C# mods.

Duplicated code is the reason audits drift apart: two copies of the same
miniyaml walk, one of them fixed. This audit finds the copies so they can be
folded into ``tools/audit/miniyaml.py`` / ``report.py`` / a shared helper.

Findings:

C1 — identical Python function/method bodies across (or within) files.
    Compared on the normalised AST (names of locals and arguments erased,
    constants kept), so reformatting and renamed variables still match.
    Only bodies with >= ``MIN_STATEMENTS`` statements count.
C2 — identical C# method bodies in ``OpenRA.Mods.Cameo`` / ``OpenRA.Mods.CA``,
    compared on brace-matched, comment- and whitespace-normalised text.
C3 — identical Python module-level constant tables (dict/list/tuple literals
    of >= ``MIN_ELEMENTS`` elements) defined in more than one file, e.g. the
    faction -> decoration maps repeated by several audits.

Exit code 1 when a group count rises above its baseline (ratchet: lower the
baselines as clones are folded away, never raise them).

Usage: python tools/audit/audit_code_duplication.py [--min-statements N]
"""

from __future__ import annotations

import argparse
import ast
import collections
import hashlib
import pathlib
import re
import sys

from miniyaml import find_repo_root
from report import h1, h2, relpath, table
from scanning import iter_dirs

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

PY_DIRS = ("tools",)
CS_DIRS = ("OpenRA.Mods.Cameo", "OpenRA.Mods.CA")
MIN_STATEMENTS = 5
MIN_ELEMENTS = 4
MIN_CS_LINES = 8

# Ratchet baselines, measured 2026-08-10. Lower as clones are removed.
BASELINES = {"C1": 10, "C2": 14, "C3": 10}


class Normalise(ast.NodeTransformer):
    """Erase identifier names so renamed copies still compare equal."""

    def visit_Name(self, node: ast.Name) -> ast.AST:  # noqa: N802
        return ast.copy_location(ast.Name(id="_", ctx=node.ctx), node)

    def visit_arg(self, node: ast.arg) -> ast.AST:
        node.arg = "_"
        node.annotation = None
        return node

    def visit_Attribute(self, node: ast.Attribute) -> ast.AST:  # noqa: N802
        self.generic_visit(node)
        return node


def body_fingerprint(node: ast.AST) -> str | None:
    body = getattr(node, "body", [])
    statements = sum(1 for _ in ast.walk(node) if isinstance(_, ast.stmt))
    if len(body) < MIN_STATEMENTS or statements < MIN_STATEMENTS:
        return None
    clone = Normalise().visit(ast.parse(ast.unparse(ast.Module(body=body, type_ignores=[]))))
    return hashlib.sha1(ast.dump(clone).encode()).hexdigest()[:16]


def literal_fingerprint(node: ast.AST) -> str | None:
    if isinstance(node, (ast.Dict,)):
        size = len(node.keys)
    elif isinstance(node, (ast.List, ast.Tuple, ast.Set)):
        size = len(node.elts)
    else:
        return None
    if size < MIN_ELEMENTS:
        return None
    try:
        return hashlib.sha1(ast.dump(node).encode()).hexdigest()[:16]
    except RecursionError:
        return None


CS_KEYWORDS = frozenset((
    "if", "else", "for", "foreach", "while", "do", "switch", "case", "using",
    "lock", "try", "catch", "finally", "fixed", "unsafe", "return", "get",
    "set", "new", "where", "select", "from",
))

CS_METHOD = re.compile(
    r"^[ \t]*(?:public|private|protected|internal|static|override|virtual|sealed|async|\s)+"
    r"[\w<>\[\],\s\.]+\s+(\w+)\s*\([^;{]*\)\s*$", re.MULTILINE)


def cs_methods(text: str) -> list[tuple[str, int, str]]:
    """(name, line, normalised body) for brace-delimited method bodies."""
    out: list[tuple[str, int, str]] = []
    for match in CS_METHOD.finditer(text):
        if match.group(1) in CS_KEYWORDS:
            continue
        idx = text.find("{", match.end())
        if idx == -1:
            continue
        depth, end = 0, None
        for i in range(idx, len(text)):
            if text[i] == "{":
                depth += 1
            elif text[i] == "}":
                depth -= 1
                if depth == 0:
                    end = i
                    break
        if end is None:
            continue
        body = text[idx + 1:end]
        lines = [re.sub(r"\s+", " ", ln).strip() for ln in body.splitlines()]
        lines = [ln for ln in lines
                 if ln and not ln.startswith("//") and not ln.startswith("/*")]
        if len(lines) < MIN_CS_LINES:
            continue
        out.append((match.group(1), text.count("\n", 0, match.start()) + 1,
                    "\n".join(lines)))
    return out


def group_rows(groups: dict[str, list[str]]) -> list[list[str]]:
    rows = []
    for fingerprint, sites in sorted(groups.items(),
                                     key=lambda kv: (-len(kv[1]), kv[0])):
        if len(sites) > 1:
            rows.append([str(len(sites)), fingerprint, "; ".join(sorted(sites))])
    return rows


def main() -> int:
    global MIN_STATEMENTS  # noqa: PLW0603 — CLI override of the threshold

    parser = argparse.ArgumentParser(add_help=True)
    parser.add_argument("--min-statements", type=int, default=MIN_STATEMENTS)
    args, _unknown = parser.parse_known_args()
    MIN_STATEMENTS = args.min_statements

    root = find_repo_root()
    unparsed: list[list[str]] = []
    c1: dict[str, list[str]] = collections.defaultdict(list)
    c2: dict[str, list[str]] = collections.defaultdict(list)
    c3: dict[str, list[str]] = collections.defaultdict(list)
    py_files = cs_files = 0

    for path in iter_dirs(root, PY_DIRS, ".py"):
        py_files += 1
        rel = relpath(str(path), root)
        try:
            tree = ast.parse(path.read_text(encoding="utf-8", errors="replace"),
                             filename=rel)
        except SyntaxError as exc:
            unparsed.append([rel, str(exc.lineno or 0), str(exc.msg)])
            continue
        for node in ast.walk(tree):
            if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
                fingerprint = body_fingerprint(node)
                if fingerprint:
                    c1[fingerprint].append(f"{rel}:{node.lineno} {node.name}()")
        for node in tree.body:
            targets = node.targets if isinstance(node, ast.Assign) else []
            for target in targets:
                fingerprint = literal_fingerprint(node.value)
                if fingerprint and isinstance(target, ast.Name):
                    c3[fingerprint].append(f"{rel}:{node.lineno} {target.id}")

    for path in iter_dirs(root, CS_DIRS, ".cs"):
        cs_files += 1
        rel = relpath(str(path), root)
        text = path.read_text(encoding="utf-8", errors="replace")
        for name, line, body in cs_methods(text):
            fingerprint = hashlib.sha1(body.encode()).hexdigest()[:16]
            c2[fingerprint].append(f"{rel}:{line} {name}()")

    rows = {"C1": group_rows(c1), "C2": group_rows(c2), "C3": group_rows(c3)}
    counts = {k: len(v) for k, v in rows.items()}

    print(h1("audit_code_duplication — copy-paste clone groups"))
    print(f"Python files: **{py_files}** (min {MIN_STATEMENTS} statements), "
          f"C# files: **{cs_files}** (min {MIN_CS_LINES} lines)\n")
    if unparsed:
        print(h2("Files that do not parse (not scanned)"))
        print(table(["file", "line", "error"], unparsed))

    print(table(["code", "meaning", "clone groups", "baseline"], [
        ["C1", "identical Python function bodies", counts["C1"], BASELINES["C1"]],
        ["C2", "identical C# method bodies", counts["C2"], BASELINES["C2"]],
        ["C3", "identical module-level literal tables", counts["C3"], BASELINES["C3"]],
    ]))

    for code, title in (("C1", "Python function clones"),
                        ("C2", "C# method clones"),
                        ("C3", "Duplicated constant tables")):
        print(h2(f"{code} — {title} ({counts[code]} group(s))"))
        print(table(["copies", "fingerprint", "sites"], rows[code]))

    regressions = [f"{code}: {counts[code]} > baseline {BASELINES[code]}"
                   for code in BASELINES if counts[code] > BASELINES[code]]
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
