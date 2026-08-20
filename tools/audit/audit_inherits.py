#!/usr/bin/env python3
"""audit_inherits.py — B2 detector; enforces MASTER_REPORT §10.3 invariants.

Violations:
  V1 Inherits target is a concrete actor (not a ^Template)
  V2 Inherits crosses faction ownership (concrete targets only)
  V3 Inherits target defined nowhere (dangling) — blocking
  V4 inherit chain depth > 3
  V5 more than 2 `-Trait:` removals on one actor (warning)
"""

from __future__ import annotations

import sys
from collections import Counter

from cameo_model import Model
from report import h1, h2, relpath, table


def main() -> int:
    m = Model()
    rs = m.rs
    v1, v2, v3, v4, v5 = [], [], [], [], []

    for name, node in sorted(rs.actors.items()):
        is_template = name.startswith("^")
        owner = None if is_template else m.owner_of(name)
        for key, target in rs.inherits_of(node):
            if not target:
                v3.append([name, key, "(empty)", relpath(node.file, m.root)])
                continue
            target_node = rs.actor(target)
            if target_node is None:
                v3.append([name, key, target, relpath(node.file, m.root)])
                continue
            if not target.startswith("^"):
                towner = m.owner_of(target)
                v1.append([name, target, owner or "?", towner or "?",
                           relpath(node.file, m.root)])
                if owner and towner and owner != towner:
                    v2.append([name, owner, target, towner,
                               relpath(node.file, m.root)])
        if not is_template:
            depth = rs.inherit_depth(name)
            if depth > 3:
                v4.append([name, str(depth), relpath(node.file, m.root)])
            removals = [c.key for c in node.children if c.key.startswith("-")]
            if len(removals) > 2:
                v5.append([name, str(len(removals)),
                           ", ".join(r[:24] for r in removals[:6]),
                           relpath(node.file, m.root)])

    print(h1("audit_inherits — §10.3 invariant violations (B2)"))
    print(f"Actors+templates scanned: **{len(rs.actors)}**\n")
    counts = Counter(V1=len(v1), V2=len(v2), V3=len(v3), V4=len(v4), V5=len(v5))
    print(table(["violation", "meaning", "count"], [
        ["V1", "concrete actor inherits from concrete actor", counts["V1"]],
        ["V2", "inherit crosses faction ownership", counts["V2"]],
        ["V3", "dangling inherit target (BLOCKING)", counts["V3"]],
        ["V4", "chain depth > 3", counts["V4"]],
        ["V5", "> 2 -Trait removals (warning)", counts["V5"]],
    ]))
    print(h2("V3 — dangling inherit targets (blocking)"))
    print(table(["actor", "inherit key", "missing target", "file"], v3))
    print(h2("V2 — cross-faction inherits (concrete targets)"))
    print(table(["actor", "actor faction", "target", "target faction", "file"], v2))
    print(h2("V1 — concrete → concrete inherits"))
    print(table(["actor", "target", "actor faction", "target faction", "file"], v1))
    print(h2("V4 — inherit chains deeper than 3"))
    print(table(["actor", "depth", "file"], v4))
    print(h2("V5 — actors with > 2 trait removals"))
    print(table(["actor", "removals", "keys", "file"], v5))
    return 1 if (v1 or v2 or v3) else 0


if __name__ == "__main__":
    sys.exit(main())
