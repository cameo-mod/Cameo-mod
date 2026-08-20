#!/usr/bin/env python3
"""Check which orphan weapons are inherited FROM by other weapons."""
import os
import re
import subprocess

MODS = os.path.join(os.path.dirname(__file__), "..", "mods", "cameo")
CWD = os.path.dirname(__file__)

result = subprocess.run(['python', 'tools/audit/audit_orphans.py'], capture_output=True, text=True, cwd=CWD)
output = result.stdout + result.stderr

orphans = []
for line in output.split('\n'):
    if line.startswith('| ') and len(line.split('|')) >= 3:
        name = line.split('|')[1].strip()
        if name and name != 'weapon' and not name.startswith('---'):
            orphans.append(name)

print(f'Total orphans: {len(orphans)}')

inherited_from = {}
for root, dirs, files in os.walk(MODS):
    for f in files:
        if not f.endswith('.yaml'):
            continue
        fp = os.path.join(root, f)
        with open(fp, 'r', encoding='utf-8') as fh:
            content = fh.read()
        for o in orphans:
            pattern = r'Inherits(?:@\w+)?:\s*' + re.escape(o) + r'\s'
            if re.search(pattern, content):
                inherited_from.setdefault(o, []).append(os.path.relpath(fp, MODS))

print(f'Orphans inherited FROM by other weapons: {len(inherited_from)}')
for name, files in sorted(inherited_from.items()):
    print(f'  {name}: {files}')

# Also check if any orphan is referenced in Inherits with @suffix
print(f'\nOrphans safe to delete (not inherited from): {len(orphans) - len(inherited_from)}')
