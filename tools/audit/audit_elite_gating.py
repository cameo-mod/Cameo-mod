#!/usr/bin/env python
"""Find Armament@*ELITE* blocks that lack RequiresCondition: rank-elite."""
import os, re, sys

root = sys.argv[1] if len(sys.argv) > 1 else "mods/cameo"
yaml_exts = (".yaml",)
results = []

for dirpath, _, filenames in os.walk(root):
    for fn in filenames:
        if not fn.endswith(yaml_exts):
            continue
        fpath = os.path.join(dirpath, fn)
        try:
            lines = open(fpath, encoding="utf-8").readlines()
        except Exception:
            continue
        i = 0
        while i < len(lines):
            line = lines[i]
            # Skip commented-out lines
            if line.strip().startswith('#'):
                i += 1
                continue
            # Match any Armament@*ELITE* or Armament@*elite* (case-insensitive)
            m = re.match(r'^\tArmament@\w*[Ee][Ll][Ii][Tt][Ee]\w*\s*:', line)
            if m:
                # Scan the trait body (indented further than the trait itself)
                j = i + 1
                has_rank_elite = False
                has_any_cond = False
                while j < len(lines):
                    bl = lines[j]
                    # Stop if we hit another trait at same or lower indent, or a new actor
                    if re.match(r'^\t\S', bl) or re.match(r'^[^\t\s]', bl):
                        break
                    if 'RequiresCondition:' in bl:
                        has_any_cond = True
                        if 'rank-elite' in bl.lower():
                            has_rank_elite = True
                    j += 1
                if not has_rank_elite:
                    # Find the actor name by scanning backwards
                    actor_name = "?"
                    for k in range(i - 1, -1, -1):
                        am = re.match(r'^(\S+):', lines[k])
                        if am and not lines[k].startswith('\t') and not lines[k].strip().startswith('#'):
                            actor_name = am.group(1)
                            break
                    trait_name = m.group(0).strip().rstrip(':')
                    cond = "no RequiresCondition" if not has_any_cond else "RequiresCondition but NOT rank-elite"
                    results.append((fpath, i + 1, actor_name, trait_name, cond))
            i += 1

print(f"# Elite weapon gating audit (E2)\n")
print(f"Armament@*ELITE* blocks without RequiresCondition: rank-elite: **{len(results)}**\n")
if results:
    print("| File | Line | Actor | Trait | Issue |")
    print("|---|---|---|---|---|")
    for fpath, line, actor, trait, issue in sorted(results):
        short = fpath.replace("\\", "/").replace("mods/cameo/", "")
        print(f"| {short} | {line} | {actor} | {trait} | {issue} |")
