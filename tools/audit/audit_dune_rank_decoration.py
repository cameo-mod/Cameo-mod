#!/usr/bin/env python
"""Find D2k actors with GainsExperience but without DuneRankDecoration."""
import os, re, sys

root = "mods/cameo/ContentPacks/D2k"
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
            am = re.match(r'^(\S+):', line)
            if am and not line.startswith('\t') and not line.strip().startswith('#'):
                actor_name = am.group(1)
                j = i + 1
                has_gains_exp = False
                has_rank_decoration = False
                while j < len(lines):
                    bl = lines[j]
                    if re.match(r'^[^\t\s]', bl) and not bl.strip().startswith('#'):
                        break
                    if 'GainsExperience' in bl and not bl.strip().startswith('#'):
                        has_gains_exp = True
                    if 'DuneRankDecoration' in bl and not bl.strip().startswith('#'):
                        has_rank_decoration = True
                    j += 1
                if has_gains_exp and not has_rank_decoration:
                    results.append((fpath, i + 1, actor_name))
            i += 1

print(f"# D2k rank decoration audit\n")
print(f"D2k actors with GainsExperience but WITHOUT DuneRankDecoration: **{len(results)}**\n")
if results:
    print("| File | Line | Actor |")
    print("|---|---|---|")
    for fpath, line, actor in sorted(results):
        short = fpath.replace("\\", "/").replace("mods/cameo/", "")
        print(f"| {short} | {line} | {actor} |")
