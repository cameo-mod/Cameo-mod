#!/usr/bin/env python3
"""Remove ChangesHealth@SelfHealing Step-only nodes from Dawn's ContentPacks.

Dawn's packs: D2k/*, TiberianDawn/*
(D2k/Shared already done by Aurora — 2 nodes removed)

Only removes nodes whose only non-comment child is `Step`. Nodes with
RequiresCondition, StartIfBelow, DamageCooldown, or other fields are skipped
and reported for maintainer review.

Usage:
  python tools/regen/remove_selfheal_dawn.py

Then commit and push to devin/regen/conversion:
  git add mods/cameo/ContentPacks/D2k/**/yaml/*.yaml mods/cameo/ContentPacks/TiberianDawn/**/yaml/*.yaml
  git commit -m "regen(d2k/td): remove N ChangesHealth@SelfHealing Step overrides"
  git push origin devin/dawn/<branch>:devin/regen/conversion
"""
import pathlib
import re

ROOT = pathlib.Path("mods/cameo/ContentPacks")
MY_PACKS = ["D2k", "TiberianDawn"]
# D2k/Shared already done by Aurora
SKIP_SUBPACKS = {"D2k/Shared"}

total_removed = 0
files_changed = 0
skipped = []

for pack in MY_PACKS:
    pack_path = ROOT / pack
    if not pack_path.exists():
        continue
    for subpack_dir in sorted(pack_path.iterdir()):
        if not subpack_dir.is_dir():
            continue
        subpack_key = f"{pack}/{subpack_dir.name}"
        if subpack_key in SKIP_SUBPACKS:
            continue
        yaml_dir = subpack_dir / "yaml"
        if not yaml_dir.exists():
            continue
        for yaml_file in sorted(yaml_dir.glob("*.yaml")):
            lines = yaml_file.read_text(encoding="utf-8").splitlines()
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
                        skipped.append((str(yaml_file.relative_to(ROOT)), i+1, actor, non_step))
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
                yaml_file.write_text("\n".join(new_lines) + "\n", encoding="utf-8")
                files_changed += 1
                print(f"  {yaml_file.relative_to(ROOT)}: removed {removed_in_file}")

print(f"\nTotal removed: {total_removed}")
print(f"Files changed: {files_changed}")
if skipped:
    print(f"\n⚠ SKIPPED (non-Step children — bring to maintainer):")
    for fname, line, actor, keys in skipped:
        print(f"  {fname}:{line} ({actor}): {keys}")
else:
    print("\n✅ No skips — all nodes had only Step as child")
