#!/usr/bin/env python3
"""Check that all actor types in delivery maps exist in rules."""
import re, os, glob

# Collect actor types from maps
actors = set()
for f in glob.glob('mods/cameo/maps/delivery*/map.yaml'):
    with open(f, encoding='utf-8') as fh:
        for line in fh:
            m = re.match(r'^\t\w+: (.+)$', line)
            if m:
                actors.add(m.group(1).strip())

# Collect actor definitions from all rules yaml
rules = set()
for f in glob.glob('mods/cameo/rules/*.yaml') + glob.glob('mods/cameo/ContentPacks/**/*.yaml', recursive=True):
    with open(f, encoding='utf-8', errors='ignore') as fh:
        for line in fh:
            if re.match(r'^[A-Za-z_^]', line):
                m = re.match(r'^(\S.+?):', line)
                if m:
                    rules.add(m.group(1))

missing = actors - rules
if missing:
    print(f"MISSING ({len(missing)}):")
    for a in sorted(missing):
        print(f"  {a}")
else:
    print(f"All {len(actors)} actor types found in rules.")
