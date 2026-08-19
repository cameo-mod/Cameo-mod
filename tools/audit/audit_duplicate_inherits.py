#!/usr/bin/env python3
"""audit_duplicate_inherits.py — detect actors/templates that inherit the same
parent through more than one Inherits@ path.

The engine merges inherited subtrees by key. When the same parent is reached via
two different Inherits@ lines, its children can be merged twice, leading to
order-dependent overrides or, in the worst case, a boot-time YAML parse crash.

This is especially dangerous when adding a parent to a base template: the
children that already inherited that parent through another base now get it
from two paths. Grepping `Inherits:` cannot see it because the collision only
appears after resolution.
"""
from __future__ import annotations

import argparse
import collections
import sys
from pathlib import Path

sys.path[:0] = [str(Path(__file__).parent.parent / "tools" / "audit"),
                str(Path(__file__).parent.parent / "tools" / "balance")]

from cameo_model import Model


def collect_paths(rs, name, path_so_far: tuple[str, ...] = ()) -> list[tuple[str, tuple[str, ...]]]:
    """Return [(parent_target, inheritance_chain), ...] for name."""
    node = rs.actor(name)
    if node is None:
        return []
    if name.lower() in path_so_far:
        return []  # cycle guard
    current = path_so_far + (name.lower(),)
    out: list[tuple[str, tuple[str, ...]]] = []
    seen_here: set[str] = set()
    for key, target in rs.inherits_of(node):
        out.append((target.lower(), current + (f"{key}:{target}",)))
        seen_here.add(target.lower())
        out.extend(collect_paths(rs, target, current))
    return out


def audit(rs, templates_too: bool = False) -> dict[str, list[list[tuple[str, tuple[str, ...]]]]]:
    """For every actor/template, find any parent reached by more than one path."""
    dupes: dict[str, list[list[tuple[str, tuple[str, ...]]]]] = collections.defaultdict(list)
    names = sorted(rs.actors)
    if not templates_too:
        names = [n for n in names if not n.startswith("^")]
    for name in names:
        paths = collect_paths(rs, name)
        by_parent: dict[str, list[tuple[str, tuple[str, ...]]]] = collections.defaultdict(list)
        for target, chain in paths:
            by_parent[target].append((target, chain))
        for parent, chains in by_parent.items():
            if len(chains) > 1:
                dupes[name].append(chains)
    return dupes


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--templates", action="store_true", help="include ^ templates too")
    ap.add_argument("--parent", default="", help="only report a specific parent (e.g. ^Corrodible)")
    args = ap.parse_args()

    m = Model()
    dupes = audit(m.rs, templates_too=args.templates)
    if args.parent:
        dupes = {n: [chains for chains in chains_list if chains[0][0] == args.parent.lower()]
                 for n, chains_list in dupes.items()}
        dupes = {n: c for n, c in dupes.items() if c}

    if not dupes:
        print("_clean_ — no duplicate inheritance paths found.")
        return 0

    print(f"# audit_duplicate_inherits — {len(dupes)} actor(s)/template(s) reach a parent through more than one path")
    for name in sorted(dupes):
        print(f"\n{name}:")
        for chains in dupes[name]:
            parent = chains[0][0]
            print(f"  parent ^^ {parent}  ({len(chains)} paths)")
            for _, chain in chains:
                print(f"    -> {' -> '.join(chain)}")

    # Report-only by default: the tree currently contains many historical
    # case-different and base/direct duplicates that boot successfully, so this
    # is a diagnostic pass for now. Use --parent for a targeted gate.
    return 0


if __name__ == "__main__":
    sys.exit(main())
