#!/usr/bin/env python3
"""Check that all actor types in delivery maps exist in rules."""
import re, os, glob

# Collect actor definitions from all rules yaml + ContentPacks
rules = set()
for f in glob.glob('mods/cameo/rules/*.yaml') + glob.glob('mods/cameo/ContentPacks/**/*.yaml', recursive=True):
    with open(f, encoding='utf-8', errors='ignore') as fh:
        for line in fh:
            if re.match(r'^[A-Za-z_^]', line):
                m = re.match(r'^(\S.+?):', line)
                if m:
                    rules.add(m.group(1))

# Known non-actor values that appear as map actor types
SKIP = {'EnemyIdle', 'Enemy2Idle', 'Neutral', 'Creeps', 'North', 'South',
        'East', 'West', 'Center', 'Player1', 'Player2', 'Player3', 'Player4',
        'Player5', 'Player6', 'Player7', 'Player8', 'Multi0', 'Multi1'}

# Parse map files: actor entries are "Name: type" at tab indentation
# but we need to skip property lines like "Owner: X", "Location: X"
PROPERTIES = {'Owner', 'Location', 'Facing', 'Scale', 'Palette', 'Image',
              'Power', 'Amount', 'Cash', 'Condition', 'String', 'Type',
              'Radius', 'Bot', 'Difficulty', 'Class', 'Name', 'Value',
              'InitialActivity', 'TargetLocation', 'Waypoint', 'RallyPoint'}

actors = {}
for f in glob.glob('mods/cameo/maps/delivery*/map.yaml'):
    mapname = os.path.basename(os.path.dirname(f))
    with open(f, encoding='utf-8') as fh:
        for line in fh:
            # Actor entries: tab-indented "Something: value" where Something is not a property
            m = re.match(r'^\t(\S+):\s+(\S+)\s*$', line)
            if m:
                key, val = m.group(1), m.group(2)
                if key in PROPERTIES:
                    continue
                if val in SKIP:
                    continue
                # Only check values that look like actor names (lowercase, no spaces)
                if re.match(r'^[a-z][a-z0-9_.-]*$', val):
                    if val not in rules:
                        actors.setdefault(mapname, set()).add(val)

for mapname in sorted(actors):
    missing = actors[mapname]
    if missing:
        print(f"\n=== {mapname} — MISSING ({len(missing)}) ===")
        for a in sorted(missing):
            print(f"  {a}")
    else:
        print(f"\n=== {mapname} — all actors defined ===")

if not actors:
    print("All actor types found in rules.")
