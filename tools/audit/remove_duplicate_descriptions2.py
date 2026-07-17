#!/usr/bin/env python3
"""
Remove duplicate Description lines that were incorrectly added by the second run
of fix_missing_descriptions.py. These duplicates have LESS indentation than the
correct Description line and appear right after it.
"""

import os
import re
import sys

MOD_ROOT = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), "mods", "cameo")

LOADED_PATH_PREFIXES = [
    'ContentPacks/TiberianDawn', 'ContentPacks/RedAlert', 'ContentPacks/TiberianSun',
    'ContentPacks/RedAlert2', 'ContentPacks/RedAlert2Mod', 'ContentPacks/TKM',
    'ContentPacks/D2k', 'ContentPacks/StarCraft', 'ContentPacks/Warcraft2',
    'ContentPacks/Outpost2', 'ContentPacks/Core',
]

LOADED_RULES_FILES = [
    'rules/misc.yaml', 'rules/player.yaml', 'rules/world.yaml',
    'rules/map_generators.yaml', 'rules/music.yaml', 'rules/palettes.yaml',
    'rules/defaults.yaml', 'rules/shared.yaml', 'rules/trees.yaml',
    'rules/civilian.yaml', 'rules/civilian_desert.yaml', 'rules/tech.yaml',
    'rules/husks.yaml', 'rules/promotions.yaml', 'ai/ai.yaml',
    'rules/starcraft.yaml', 'rules/warcraft2.yaml', 'rules/tkm.yaml',
    'rules/redalert.yaml', 'rules/tiberiansun.yaml', 'rules/redalert2.yaml',
    'rules/redalert2mod.yaml', 'rules/d2k.yaml', 'rules/outpost2.yaml',
]

def is_loaded(filepath):
    rel = os.path.relpath(filepath, MOD_ROOT).replace('\\', '/')
    for prefix in LOADED_PATH_PREFIXES:
        if rel.startswith(prefix):
            return True
    for rf in LOADED_RULES_FILES:
        if rel == rf:
            return True
    return False

def get_indent(line):
    """Get the indentation string of a line."""
    stripped = line.rstrip('\n\r')
    return stripped[:len(stripped) - len(stripped.lstrip())]

def remove_duplicates(filepath):
    """Remove duplicate Description lines with wrong indentation."""
    with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    changes = []
    lines_to_remove = set()
    
    for i in range(1, len(lines)):
        line = lines[i]
        stripped = line.rstrip('\n\r')
        
        if not stripped.strip():
            continue
        
        # Check if this line is a Description line
        if not re.match(r'^\s+Description:', stripped):
            continue
        
        current_indent = get_indent(line)
        
        # Look at the previous non-empty line
        j = i - 1
        while j >= 0 and not lines[j].strip():
            j -= 1
        
        if j < 0:
            continue
        
        prev_line = lines[j].rstrip('\n\r')
        prev_indent = get_indent(lines[j])
        
        # If the previous line is also a Description line with MORE indentation,
        # this is a duplicate that should be removed
        if re.match(r'^\s+Description:', prev_line) and len(current_indent) < len(prev_indent):
            lines_to_remove.add(i)
            changes.append((i + 1, stripped[:80]))
    
    if lines_to_remove:
        lines = [l for i, l in enumerate(lines) if i not in lines_to_remove]
        with open(filepath, 'w', encoding='utf-8', newline='\n') as f:
            f.writelines(lines)
    
    return changes

def main():
    yaml_files = []
    for dirpath, dirnames, filenames in os.walk(MOD_ROOT):
        if '.git' in dirpath:
            continue
        for f in filenames:
            if f.endswith('.yaml') or f.endswith('.yml'):
                fp = os.path.join(dirpath, f)
                if is_loaded(fp):
                    yaml_files.append(fp)
    
    total_fixes = 0
    for yf in sorted(yaml_files):
        changes = remove_duplicates(yf)
        if changes:
            rel = os.path.relpath(yf, MOD_ROOT)
            print(f"\n{rel}:")
            for line_num, content in changes:
                print(f"  Removed line {line_num}: {content}")
            total_fixes += len(changes)
    
    print(f"\nTotal duplicates removed: {total_fixes}")

if __name__ == '__main__':
    main()
