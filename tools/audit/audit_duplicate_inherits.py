#!/usr/bin/env python3
"""audit_duplicate_inherits.py — the boot crash `Parent type X was already inherited`.

⚠ WHY THIS EXISTS. The engine rejects inheriting the same parent TWICE along one ancestor
chain, and until now the ONLY detector was the boot itself — which throws on the FIRST
collision and then stops, so a refactor that introduces N of them costs N launch cycles to
find. `audit_inherits` does not cover it (it checks actors for dangling/cross-faction/depth,
never repetition) and neither does the Python resolver, which silently merges a parent twice.

The engine rule, replicated from `engine/OpenRA.Game/MiniYaml.cs` `ResolveInherits`:

    inherited = inherited.Add(parentName, location)   # throws if already present
    ResolveInherits(parent, tree, inherited)          # by value: child additions do NOT escape

Three consequences, and all three are counter-intuitive enough to be worth spelling out:

  * ⭐ **The `@suffix` does NOT make it legal.** `Inherits@4:` and `Inherits@fx:` are distinct
    KEYS (which is what the suffix is for — two bare `Inherits:` keys in one node collide as
    duplicate keys), but the guard is keyed on the parent TYPE, so suffixing changes nothing.
  * **A DIAMOND is legal.** Two sibling parents that each inherit a common grandparent are
    fine, because the additions made inside one sibling's recursion do not escape it.
  * ⚠ **It is ORDER-DEPENDENT.** `Inherits: A` followed by `Inherits@2: B` where `B` inherits
    `A` CRASHES; the same two lines in the opposite order do not. So a working weapon can be
    broken by reordering its inherit block and nothing else — which is D2 below.

Findings:
  D1 the same parent twice along one chain — BLOCKING, this is the boot crash
  D2 a parent that is ALSO reachable through another parent — legal only by line order
     (a latent D1: the same redundancy, currently saved by where the line sits)
  D3 an inherit target that is defined nowhere — BLOCKING (`Parent type not found`)
"""

from __future__ import annotations

import sys

from cameo_model import Model
from report import h1, h2, relpath, table

MAX_ROWS = 60


class Kind:
    """One namespace (weapons or actors) with the lookups the walk needs."""

    def __init__(self, label: str, nodes: dict, lookup):
        self.label = label
        self.nodes = nodes
        self.lookup = lookup


def inherit_children(node):
    """[(key, target, line)] in document order — the engine iterates node order."""
    out = []
    for c in node.children:
        if c.key == "Inherits" or c.key.startswith("Inherits@"):
            out.append((c.key, (c.value or "").strip(), c.line))
    return out


def walk(root_name, kind, model):
    """Replicate ResolveInherits for one root node; return (d1, d3) rows."""
    d1, d3 = [], []
    root = kind.lookup(root_name)

    def site(node, line):
        return f"{relpath(node.file, model.root)}:{line}"

    def recurse(node, inherited, depth):
        # `inherited` is rebound as we go, exactly as the engine rebinds its local — so a
        # later sibling sees what an earlier one added, while a child sees only the path.
        if depth > 24:
            return inherited
        for key, target, line in inherit_children(node):
            if not target:
                continue
            parent = kind.lookup(target)
            if parent is None:
                d3.append([root_name, key, target, site(node, line)])
                continue
            if target in inherited:
                d1.append([root_name, target, site(node, line), inherited[target]])
                continue          # the engine throws here; keep going to report them ALL
            inherited = {**inherited, target: site(node, line)}
            recurse(parent, inherited, depth + 1)
        return inherited

    recurse(root, {}, 0)
    return d1, d3


def reachable(target, kind, depth=0, seen=None):
    """Every parent reachable FROM `target`, transitively (not including target)."""
    seen = seen if seen is not None else set()
    node = kind.lookup(target)
    if node is None or depth > 24:
        return seen
    for _key, parent, _line in inherit_children(node):
        if parent and parent not in seen:
            seen.add(parent)
            reachable(parent, kind, depth + 1, seen)
    return seen


def latent(root_name, kind, model):
    """D2 — a direct parent that another direct parent already brings in."""
    rows = []
    root = kind.lookup(root_name)
    direct = [(k, t, ln) for k, t, ln in inherit_children(root) if t]
    for key, target, line in direct:
        for other_key, other, _ol in direct:
            if other == target and other_key == key:
                continue
            if target in reachable(other, kind):
                rows.append([root_name, target, f"{other_key}: {other}",
                             f"{relpath(root.file, model.root)}:{line}"])
                break
    return rows


def main() -> int:
    m = Model()
    rs = m.rs
    kinds = [Kind("weapon", rs.weapons, rs.weapon), Kind("actor", rs.actors, rs.actor)]

    print(h1("audit_duplicate_inherits — `Parent type X was already inherited` (boot crash)"))
    blocking = 0
    for kind in kinds:
        d1, d2, d3 = [], [], []
        for name in sorted(kind.nodes):
            a, c = walk(name, kind, m)
            d1 += a
            d3 += c
            d2 += latent(name, kind, m)
        # D2 rows for a node that is ALREADY a D1 are noise — the crash outranks the warning.
        crashed = {r[0] for r in d1}
        d2 = [r for r in d2 if r[0] not in crashed]
        blocking += len(d1) + len(d3)

        print(h2(f"{kind.label}s — {len(kind.nodes)} nodes scanned"))
        print(table(["finding", "meaning", "count"], [
            ["D1", "same parent twice on one chain (BLOCKING — boot crash)", len(d1)],
            ["D2", "redundant parent, legal only by line ORDER (latent D1)", len(d2)],
            ["D3", "inherit target defined nowhere (BLOCKING)", len(d3)],
        ]))
        if d1:
            print(h2(f"D1 — {kind.label}s the engine will refuse to load"))
            print("Fix: DELETE the redundant direct inherit — the other parent already "
                  "provides it. (Reordering the lines also silences the crash, but leaves "
                  "the same redundancy behind as a D2.)\n")
            print(table([kind.label, "parent inherited twice", "second site (engine reports this)",
                         "first site"], d1[:MAX_ROWS]))
        if d3:
            print(h2(f"D3 — dangling inherit targets ({kind.label}s)"))
            print(table([kind.label, "key", "missing target", "site"], d3[:MAX_ROWS]))
        if d2:
            print(h2(f"D2 — redundant parents that only line ORDER is saving ({kind.label}s)"))
            print("Each of these inherits a parent it ALSO gets through another parent. It "
                  "loads today because the direct line sits AFTER the one that brings it in; "
                  "swap them and it is a D1 crash.\n")
            print(table([kind.label, "redundant parent", "already provided by", "site"],
                        d2[:MAX_ROWS]))
            if len(d2) > MAX_ROWS:
                print(f"\n_{len(d2) - MAX_ROWS} further D2 row(s) omitted._")

    return 1 if blocking else 0


if __name__ == "__main__":
    sys.exit(main())
