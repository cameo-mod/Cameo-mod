#!/usr/bin/env python3
"""Remove ChangesHealth@SelfHealing Step-only nodes from Aurora's rules/ files.

Aurora's rules files: outpost2.yaml, starcraft.yaml, warcraft2.yaml, xcom.yaml
NOT: defaults.yaml (Claude flips last), redalert2.yaml (Nova), tiberiansun.yaml (Ember)

Only removes nodes whose only non-comment child is `Step`. Nodes with
RequiresCondition, StartIfBelow, DamageCooldown, or other fields are skipped
and reported for maintainer review.
"""
import pathlib
import re

ROOT = pathlib.Path("mods/cameo/rules")
MY_FILES = ["outpost2.yaml", "starcraft.yaml", "warcraft2.yaml", "xcom.yaml"]

total_removed = 0
files_changed = 0
skipped = []

for fname in MY_FILES:
    path = ROOT / fname
    if not path.exists():
        continue
    lines = path.read_text(encoding="utf-8").splitlines()
    new_lines = []
    i = 0
    removed_in_file = 0
    while i < len(lines):
        line = lines[i]
        if re.match(r"^\s*ChangesHealth@SelfHealing:", line):
            base_indent = len(line) - len(line.lstrip())
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
            child_keys = [c[1].split(":")[0].strip() for c in children
                          if not c[1].lstrip().startswith("#")]
            non_step = [k for k in child_keys if k != "Step"]
            if non_step:
                actor = "unknown"
                for k in range(i-1, -1, -1):
                    if re.match(r"^[A-Za-z0-9_^.]+:", lines[k]):
                        actor = lines[k].split(":")[0]
                        break
                skipped.append((fname, i+1, actor, non_step))
                new_lines.append(line)
                i += 1
                continue
            removed_in_file += 1
            total_removed += 1
            i = j
            continue
        new_lines.append(line)
        i += 1
    if removed_in_file > 0:
        path.write_text("\n".join(new_lines) + "\n", encoding="utf-8")
        files_changed += 1
        print(f"  {fname}: removed {removed_in_file} nodes")

print(f"\nTotal removed: {total_removed}")
print(f"Files changed: {files_changed}")
if skipped:
    print(f"\n⚠ SKIPPED (non-Step children — bring to maintainer):")
    for fname, line, actor, keys in skipped:
        print(f"  {fname}:{line} ({actor}): {keys}")
else:
    print("\n✅ No skips — all nodes had only Step as child")
