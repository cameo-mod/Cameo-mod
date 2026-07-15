#!/usr/bin/env python3
"""Convert AttackGarrisoned and AttackOpenTopped trait names to AttackGarrisonedSP."""
import os
import re

MODS_DIR = os.path.join(os.path.dirname(__file__), "..", "mods", "cameo", "rules")

def convert_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original = content
    
    # Replace trait name lines (indented trait declarations)
    content = re.sub(r'^(\s+)AttackGarrisoned:', r'\1AttackGarrisonedSP:', content, flags=re.MULTILINE)
    content = re.sub(r'^(\s+)AttackOpenTopped:', r'\1AttackGarrisonedSP:', content, flags=re.MULTILINE)
    # Also handle trait removals like -AttackGarrisoned:
    content = re.sub(r'^(\s+)-AttackGarrisoned:', r'\1-AttackGarrisonedSP:', content, flags=re.MULTILINE)
    content = re.sub(r'^(\s+)-AttackOpenTopped:', r'\1-AttackGarrisonedSP:', content, flags=re.MULTILINE)
    
    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        return True
    return False

changed = []
for filename in os.listdir(MODS_DIR):
    if filename.endswith('.yaml'):
        filepath = os.path.join(MODS_DIR, filename)
        if convert_file(filepath):
            changed.append(filename)

print(f"Converted {len(changed)} files:")
for f in sorted(changed):
    print(f"  {f}")
