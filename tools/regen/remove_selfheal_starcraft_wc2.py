#!/usr/bin/env python3
"""Remove ChangesHealth@SelfHealing nodes (and their children) from StarCraft and Warcraft2 yaml.

This is the regen conversion task: the new ScaledSelfHeal trait derives the heal amount
from MaxHP, so the per-actor Step overrides are dead weight.

Only removes nodes whose ONLY child is `Step`. Nodes with StartIfBelow, DamageCooldown,
or other fields are left for maintainer review.

Writes the removal script alongside the change for auditability.
"""
import pathlib
import re
import sys

ROOT = pathlib.Path("mods/cameo/ContentPacks")
PACKS = ["StarCraft", "Warcraft2"]

total_removed = 0
files_changed = 0
skipped = []

for pack in PACKS:
    for path in sorted(ROOT.joinpath(pack).rglob("*.yaml")):
        lines = path.read_text(encoding="utf-8").splitlines()
        new_lines = []
        i = 0
        removed_in_file = 0
        while i < len(lines):
            line = lines[i]
            # Match active ChangesHealth@SelfHealing (not commented)
            if re.match(r"^\t*ChangesHealth@SelfHealing:", line):
                base_indent = len(line) - len(line.lstrip())
                # Collect children
                children = []
                j = i + 1
                while j < len(lines):
                    child_line = lines[j]
                    if child_line.strip() == "":
                        j += 1
                        continue
                    child_indent = len(child_line) - len(child_line.lstrip())
                    if child_indent <= base_indent:
                        break
                    children.append((j, child_line))
                    j += 1
                # Check if ALL children are Step (or comments)
                child_keys = [c[1].split(":")[0].strip() for c in children
                              if not c[1].lstrip().startswith("#")]
                non_step = [k for k in child_keys if k != "Step"]
                if non_step:
                    # Skip — bring to maintainer
                    actor = "unknown"
                    for k in range(i-1, -1, -1):
                        if re.match(r"^[A-Za-z0-9_^.]+:", lines[k]):
                            actor = lines[k].split(":")[0]
                            break
                    skipped.append((str(path), i+1, actor, non_step))
                    new_lines.append(line)
                    i += 1
                    continue
                # Remove the node and its children
                removed_in_file += 1
                total_removed += 1
                i = j  # Skip past children
                continue
            new_lines.append(line)
            i += 1
        if removed_in_file > 0:
            path.write_text("\n".join(new_lines) + "\n", encoding="utf-8")
            files_changed += 1
            print(f"  {path}: removed {removed_in_file} nodes")

print(f"\nTotal removed: {total_removed}")
print(f"Files changed: {files_changed}")
if skipped:
    print(f"\n⚠ SKIPPED (non-Step children — bring to maintainer):")
    for path, line, actor, keys in skipped:
        print(f"  {path}:{line} ({actor}): {keys}")
else:
    print("\n✅ No skips — all nodes had only Step as child")
