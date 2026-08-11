#!/usr/bin/env python3
"""audit_duplicate_keys.py — duplicate keys inside one node (silent overrides).

MiniYaml does NOT reject a key that appears twice in the same node: it merges
the second node over the first (engine MiniYaml.cs MergeSelfPartial ->
MergePartial), and the merged value is ``overrideNodes.Value ?? existingNodes.Value``.
So the LAST occurrence wins silently and neither ``--check-yaml`` nor the boot
gate complains.

Two severities:

D1 (BLOCKING) — duplicate ``Inherits``/``Inherits@X`` keys with DIFFERENT values.
    One whole template is silently dropped: the actor/weapon never inherits it,
    so traits, armaments and effects the author expected simply do not exist.
    Fix by giving each inherit a unique ``@suffix``.

D2 — every other duplicate key (same trait or field declared twice, or a
    duplicated ``Inherits`` with an identical value). The nodes merge, so the
    result is usually what the author wanted, but any field set by both copies
    resolves to the last one. Hygiene: collapse them into one node.

Exit code 1 when the D1 count RISES ABOVE ``D1_BASELINE``. The pre-existing
findings are a ratchet, not a green light: resolving one changes resolved
behaviour (the dropped template starts applying again), which needs a
maintainer decision per case (CLAUDE.md rules 3-4). Lower the baseline as
they are fixed; never raise it.

Usage: python tools/audit/audit_duplicate_keys.py
"""

from __future__ import annotations

import collections
import pathlib
import sys

from miniyaml import Node, find_repo_root, load
from report import h1, h2, relpath, table

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

# Directories whose yaml the engine loads as rules/weapons/sequences/audio.
SCAN_DIRS = ("mods/cameo",)
SKIP_PARTS = ("maps", "bits")

# Ratchet: D1 findings measured on 2026-08-11 against the unmodified tree.
# Lower it as duplicates are resolved; never raise it without a note.
D1_BASELINE = 90


def duplicate_children(node: Node) -> dict[str, list[Node]]:
    groups: dict[str, list[Node]] = collections.defaultdict(list)
    for child in node.children:
        if child.key and not child.key.startswith("-"):
            groups[child.key].append(child)
    return {k: v for k, v in groups.items() if len(v) > 1}


def walk(node: Node, path: str, out: list[tuple[str, str, list[Node]]]) -> None:
    for key, nodes in duplicate_children(node).items():
        out.append((path, key, nodes))
    for child in node.children:
        walk(child, f"{path} > {child.key}", out)


def main() -> int:
    root = find_repo_root()
    d1_rows: list[list[str]] = []
    d2_counts: collections.Counter[str] = collections.Counter()
    d2_rows: list[list[str]] = []

    files = []
    for scan in SCAN_DIRS:
        for path in sorted((root / scan).rglob("*.yaml")):
            if any(part in SKIP_PARTS for part in path.parts):
                continue
            files.append(path)

    for path in files:
        rel = relpath(str(path), root)
        try:
            doc = load(path)
        except Exception as exc:  # noqa: BLE001 — report, never abort the suite
            d2_rows.append([rel, "-", "load error", str(exc)])
            continue
        for top in doc:
            findings: list[tuple[str, str, list[Node]]] = []
            walk(top, top.key, findings)
            for owner, key, nodes in findings:
                lines = ", ".join(str(n.line) for n in nodes)
                values = [n.value for n in nodes]
                is_inherit = key == "Inherits" or key.startswith("Inherits@")
                if is_inherit and len(set(values)) > 1:
                    d1_rows.append([rel, lines, owner, key,
                                    " vs ".join(v or "(empty)" for v in values)])
                else:
                    d2_counts[key] += 1
                    d2_rows.append([rel, lines, owner, key])

    print(h1("audit_duplicate_keys — duplicate keys in one node (silent override)"))
    print(f"Files scanned: **{len(files)}** — D1 dropped inherits: "
          f"**{len(d1_rows)}**, D2 merged duplicates: **{len(d2_rows)}**\n")

    print(h2("D1 — duplicate Inherits key with different values (one template is dropped)"))
    print(table(["file", "lines", "node", "key", "values"], d1_rows))

    print(h2("D2 — duplicate keys by key name (top 40)"))
    print(table(["key", "occurrences"],
                [[k, str(c)] for k, c in d2_counts.most_common(40)]))

    print(h2("D2 — full list"))
    print(table(["file", "lines", "node", "key"], d2_rows))

    if len(d1_rows) > D1_BASELINE:
        print(f"\n**FAIL** — D1 count {len(d1_rows)} exceeds the baseline "
              f"{D1_BASELINE}: a new duplicate Inherits key was introduced.\n")
        return 1

    if len(d1_rows) < D1_BASELINE:
        print(f"\nD1 count {len(d1_rows)} is below the baseline {D1_BASELINE} — "
              f"lower D1_BASELINE in this script to lock the fix in.\n")

    return 0


if __name__ == "__main__":
    sys.exit(main())
