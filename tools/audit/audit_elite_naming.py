#!/usr/bin/env python
"""Find elite weapons that don't follow <baseWeapon>E naming, only for rank-elite gated armaments."""
import os, re, sys

root = "mods/cameo"
results = []

for dirpath, _, filenames in os.walk(root):
    for fn in filenames:
        if not fn.endswith(".yaml"):
            continue
        fpath = os.path.join(dirpath, fn)
        try:
            lines = open(fpath, encoding="utf-8").readlines()
        except Exception:
            continue
        i = 0
        while i < len(lines):
            line = lines[i]
            if line.strip().startswith('#'):
                i += 1
                continue
            m = re.match(r'^\tArmament@\w*[Ee][Ll][Ii][Tt][Ee]\w*\s*:', line)
            if m:
                j = i + 1
                weapon_name = None
                has_rank_elite = False
                while j < len(lines):
                    bl = lines[j]
                    if re.match(r'^\t\S', bl) or re.match(r'^[^\t\s]', bl):
                        break
                    wm = re.match(r'^\t\tWeapon:\s*(\S+)', bl)
                    if wm:
                        weapon_name = wm.group(1)
                    if 'rank-elite' in bl.lower():
                        has_rank_elite = True
                    j += 1
                if weapon_name and has_rank_elite:
                    is_valid = weapon_name.endswith('E') or weapon_name.endswith('_elite')
                    if not is_valid:
                        actor_name = "?"
                        for k in range(i - 1, -1, -1):
                            am = re.match(r'^(\S+):', lines[k])
                            if am and not lines[k].startswith('\t') and not lines[k].strip().startswith('#'):
                                actor_name = am.group(1)
                                break
                        trait_name = m.group(0).strip().rstrip(':')
                        results.append((fpath, i+1, actor_name, trait_name, weapon_name))
            i += 1

print(f"# Elite weapon naming audit (E3 — rank-elite only)\n")
print(f"Non-standard rank-elite weapon names: **{len(results)}**\n")
if results:
    print("| File | Line | Actor | Trait | Weapon |")
    print("|---|---|---|---|---|")
    for fpath, line, actor, trait, weapon in sorted(results):
        short = fpath.replace("\\", "/").replace("mods/cameo/", "")
        print(f"| {short} | {line} | {actor} | {trait} | {weapon} |")
