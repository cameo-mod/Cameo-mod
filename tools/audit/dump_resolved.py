#!/usr/bin/env python3
"""dump_resolved.py — the refactor safety net (MASTER_REPORT §10.4 step 5).

Fully resolves inheritance (+ removals, @-merging) and emits canonical
sorted JSON of every requested actor's final trait tree.

Usage:
  dump_resolved.py --faction cabal          > before.json
  dump_resolved.py --actor tsobl2 --actor tsttnkcabal
  dump_resolved.py --all                    (every non-template actor)

Refactor workflow: dump before, refactor, dump after, `diff` — every
non-empty diff line is a behavior change (intended or a found bug).
"""

from __future__ import annotations

import argparse
import json
import sys

from cameo_model import Model
from miniyaml import Node

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")


def node_to_obj(node: Node) -> dict | str:
    if not node.children:
        return node.value
    obj: dict = {}
    for c in sorted(node.children, key=lambda n: n.key):
        obj[c.key] = node_to_obj(c)
    if node.value:
        obj["__value"] = node.value
    return obj


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--faction", action="append", default=[])
    ap.add_argument("--actor", action="append", default=[])
    ap.add_argument("--all", action="store_true")
    args = ap.parse_args()

    m = Model()
    names: set[str] = set()
    for fac in args.faction:
        names |= m.buildable_roster(fac)
    for a in args.actor:
        names.add(a.lower())
    if args.all:
        names |= {n.lower() for n in m.rs.actors if not n.startswith("^")}
    if not names:
        ap.error("give --faction, --actor, or --all")

    out = {}
    for lname in sorted(names):
        res = m.rs.resolve(lname)
        if res is None:
            out[lname] = None
            continue
        out[lname] = node_to_obj(res)
    json.dump(out, sys.stdout, indent=1, sort_keys=True, ensure_ascii=False)
    print()
    return 0


if __name__ == "__main__":
    sys.exit(main())
