#!/usr/bin/env python
"""Add Inherits@decoration: ^AlienRankDecoration to SC actors with GainsExperienceTD."""
import os, re, sys

fpath = "mods/cameo/rules/starcraft.yaml"
try:
    lines = open(fpath, encoding="utf-8").readlines()
except Exception as e:
    print(f"Error: {e}")
    sys.exit(1)

changes = []
i = 0
while i < len(lines):
    line = lines[i]
    if line.strip().startswith('#'):
        i += 1
        continue
    if re.match(r'^Inherits@\w+:\s*\^GainsExperienceTD\w*\s*$', line.strip()):
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
            if 'AlienRankDecoration' in bl:
                has_decoration = True
                break
            j += 1
        if not has_decoration:
            indent = '\t'
            new_line = f"{indent}Inherits@decoration: ^AlienRankDecoration\n"
            lines.insert(i + 1, new_line)
            changes.append(i + 2)
    i += 1

with open(fpath, 'w', encoding='utf-8', newline='\n') as f:
    f.writelines(lines)

print(f"Added ^AlienRankDecoration to {len(changes)} SC actors")
