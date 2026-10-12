#!/usr/bin/env python3
"""Inventory: which TraitInfo types can occur multi-instance in resolved rules."""
import collections
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).parent))
import miniyaml

root = miniyaml.find_repo_root()
rs = miniyaml.Ruleset(root, "cameo")

multi = collections.defaultdict(list)  # base -> [actors carrying >=2]
skipped = 0
for name in sorted(rs.actors):
    node = rs.resolve(name)
    if node is None:
        skipped += 1
        continue
    counts = collections.Counter()
    for c in node.children:
        base = c.key.split("@")[0]
        if base in ("Inherits", "InheritsAbstract") or base.startswith("-"):
            continue
        counts[base] += 1
    for base, n in counts.items():
        if n >= 2:
            multi[base].append((name, n))

print(f"resolved {len(rs.actors) - skipped}/{len(rs.actors)} actors")
print(f"multi-instance trait bases: {len(multi)}\n")
for base in sorted(multi, key=lambda b: -len(multi[b])):
    actors = multi[base]
    ex = ", ".join(f"{a}x{n}" for a, n in actors[:4])
    print(f"{base + 'Info':<42} {len(actors):>5} actors   e.g. {ex}")
