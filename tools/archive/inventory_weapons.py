#!/usr/bin/env python3
"""Inventory all weapon IDs and their carrier actors."""

from pathlib import Path
import re

ROOT = Path('C:/Users/AedisToru/Documents/GitHub/Cameo-mod/mods/cameo')

# Find all top-level weapon definitions and references
top_level = {}
for p in ROOT.rglob('*.yaml'):
    try:
        text = p.read_text(encoding='utf-8', errors='replace')
    except Exception as e:
        print(f'Error reading {p}: {e}')
        continue
    for line_no, line in enumerate(text.splitlines(), 1):
        if not line.strip() or line.startswith('#'):
            continue
        if line[0] not in (' ', '\t'):
            m = re.match(r'^([A-Za-z_][A-Za-z0-9_\.]*)', line)
            if m:
                name = m.group(1)
                top_level.setdefault(name, []).append((str(p.relative_to(ROOT)), line_no))

# Find all weapon references in Armament blocks
armament_refs = {}
for p in ROOT.rglob('*.yaml'):
    try:
        text = p.read_text(encoding='utf-8', errors='replace')
    except Exception as e:
        continue
    current_actor = None
    for line_no, line in enumerate(text.splitlines(), 1):
        if not line.strip() or line.strip().startswith('#'):
            continue
        if line[0] not in (' ', '\t'):
            current_actor = line.split(':')[0].strip()
        if 'Weapon:' in line:
            m = re.search(r'Weapon:\s*([A-Za-z_][A-Za-z0-9_\.]*)', line)
            if m:
                w = m.group(1)
                armament_refs.setdefault(w, []).append((current_actor, str(p.relative_to(ROOT)), line_no))

# Print weapons that are referenced by armaments
print('=== Weapons with actor references ===')
referenced = set(armament_refs.keys())
for w in sorted(referenced):
    carriers = set(c[0] for c in armament_refs[w] if c[0])
    files = set(c[1] for c in armament_refs[w])
    print(f'{w} -> {sorted(carriers)} (files: {sorted(files)})')

print(f'\nTotal referenced weapons: {len(referenced)}')
print(f'Total top-level IDs: {len(top_level)}')
