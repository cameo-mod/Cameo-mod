#!/usr/bin/env python
"""Add Inherits@decoration: ^DuneRankDecoration to D2k actors that have GainsExperience but lack it."""
import os, re, sys

root = "mods/cameo/ContentPacks/D2k"
changes = []

for dirpath, _, filenames in os.walk(root):
    for fn in filenames:
        if not fn.endswith(".yaml"):
            continue
        fpath = os.path.join(dirpath, fn)
        try:
            lines = open(fpath, encoding="utf-8").readlines()
        except Exception:
            continue
        
        modified = False
        i = 0
        while i < len(lines):
            line = lines[i]
            if line.strip().startswith('#'):
                i += 1
                continue
            if re.match(r'^Inherits@\w+:\s*\^GainsExperience\w*\s*$', line.strip()):
                actor_start = i
                for k in range(i - 1, -1, -1):
                    if re.match(r'^[^\t\s]', lines[k]) and not lines[k].strip().startswith('#'):
                        actor_start = k
                        break
                has_decoration = False
                j = actor_start + 1
                while j < len(lines):
                    bl = lines[j]
                    if re.match(r'^[^\t\s]', bl) and not bl.strip().startswith('#'):
                        break
                    if 'DuneRankDecoration' in bl:
                        has_decoration = True
                        break
                    j += 1
                if not has_decoration:
                    indent = '\t'
                    new_line = f"{indent}Inherits@decoration: ^DuneRankDecoration\n"
                    lines.insert(i + 1, new_line)
                    modified = True
                    changes.append((fpath, i + 2, line.strip()))
            i += 1
        
        if modified:
            with open(fpath, 'w', encoding='utf-8', newline='\n') as f:
                f.writelines(lines)

print(f"Added ^DuneRankDecoration to {len(changes)} actors:\n")
for fpath, line, after in sorted(changes):
    short = fpath.replace("\\", "/").replace("mods/cameo/", "")
    print(f"  {short}:{line} (after {after})")
