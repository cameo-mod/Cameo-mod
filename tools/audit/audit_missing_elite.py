#!/usr/bin/env python
"""E1: Find buildable actors with GainsExperience that lack Armament@*ELITE* blocks."""
import os, re, sys

root = "mods/cameo"
results = []

for dirpath, _, filenames in os.walk(root):
    for fn in filenames:
        if not fn.endswith(".yaml"):
            continue
        fpath = os.path.join(dirpath, fn)
        rel = fpath.replace("\\", "/").replace("mods/cameo/", "")
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
            am = re.match(r'^(\S+):', line)
            if am and not line.startswith('\t') and not line.strip().startswith('#'):
                actor_name = am.group(1)
                if actor_name.startswith('^'):
                    i += 1
                    continue
                # Scan actor body
                j = i + 1
                has_gains_exp = False
                has_elite_armament = False
                has_buildable = False
                while j < len(lines):
                    bl = lines[j]
                    if re.match(r'^[^\t\s]', bl) and not bl.strip().startswith('#'):
                        break
                    stripped = bl.strip()
                    if not stripped.startswith('#'):
                        if 'GainsExperience' in stripped:
                            has_gains_exp = True
                        if re.match(r'^Armament@\w*[Ee][Ll][Ii][Tt][Ee]\w*\s*:', stripped):
                            has_elite_armament = True
                        if stripped == 'Buildable:':
                            has_buildable = True
                    j += 1

                if has_gains_exp and not has_elite_armament and has_buildable:
                    faction = "?"
                    parts = rel.split('/')
                    if 'ContentPacks' in parts:
                        idx = parts.index('ContentPacks')
                        if idx + 2 < len(parts):
                            faction = f"{parts[idx+1]}/{parts[idx+2]}"
                        elif idx + 1 < len(parts):
                            faction = parts[idx+1]
                    elif 'rules' in parts:
                        faction = f"rules/{parts[-1].replace('.yaml','')}"
                    results.append((rel, i+1, actor_name, faction))
            i += 1

results.sort(key=lambda x: (x[3], x[2]))

print(f"# E1: Missing elite weapons audit\n")
print(f"Buildable actors with GainsExperience but NO Armament@*ELITE*: **{len(results)}**\n")

faction_counts = {}
for _, _, _, faction in results:
    faction_counts[faction] = faction_counts.get(faction, 0) + 1

print("## By faction\n")
print("| Faction | Count |")
print("|---|---|")
for f, c in sorted(faction_counts.items(), key=lambda x: -x[1]):
    print(f"| {f} | {c} |")

print(f"\n## Detail ({len(results)} actors)\n")
print("| File | Line | Actor | Faction |")
print("|---|---|---|---|")
for rel, line, actor, faction in results:
    print(f"| {rel} | {line} | {actor} | {faction} |")
