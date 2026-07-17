#!/usr/bin/env python3
"""
Remove duplicate Description lines in Buildable sections.
The fix_missing_descriptions.py script was run twice, creating duplicates.
This script removes the second (incorrectly indented) duplicate.
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

def remove_duplicate_descriptions(filepath):
    """Remove duplicate Description lines in Buildable sections."""
    with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    changes = []
    in_buildable = False
    buildable_indent = 0
    description_count = 0
    description_lines = []
    
    i = 0
    while i < len(lines):
        line = lines[i]
        stripped = line.rstrip('\n\r')
        
        if not stripped.strip() or stripped.strip().startswith('#'):
            i += 1
            continue
        
        indent = len(stripped) - len(stripped.lstrip())
        
        # Top-level actor definition
        if indent == 0 and stripped.endswith(':') and not stripped.startswith('-') and not stripped.startswith(' '):
            in_buildable = False
            description_count = 0
            description_lines = []
            i += 1
            continue
        
        # Detect Buildable section
        if re.match(r'^\s+Buildable:', stripped) or re.match(r'^\s+Buildable@[^:]+:', stripped):
            in_buildable = True
            buildable_indent = indent
            description_count = 0
            description_lines = []
            i += 1
            continue
        
        # Check if leaving Buildable section
        if in_buildable and stripped.strip() and indent <= buildable_indent:
            in_buildable = False
            # If we have duplicates, remove the ones with wrong indentation
            if description_count > 1:
                # Keep the first one (which was fixed by fix_indentation), remove others
                for line_num in description_lines[1:]:
                    changes.append((line_num, lines[line_num].rstrip('\n\r')))
                    lines[line_num] = None  # Mark for removal
            description_count = 0
            description_lines = []
            i += 1
            continue
        
        # Count Description lines in Buildable
        if in_buildable and re.match(r'^\s+Description:', stripped):
            description_count += 1
            description_lines.append(i)
        
        i += 1
    
    # Handle end of file
    if in_buildable and description_count > 1:
        for line_num in description_lines[1:]:
            changes.append((line_num, lines[line_num].rstrip('\n\r')))
            lines[line_num] = None
    
    if changes:
        # Remove marked lines
        lines = [l for l in lines if l is not None]
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
        changes = remove_duplicate_descriptions(yf)
        if changes:
            rel = os.path.relpath(yf, MOD_ROOT)
            print(f"\n{rel}:")
            for line_num, content in changes:
                print(f"  Removed line {line_num + 1}: {content[:80]}")
            total_fixes += len(changes)
    
    print(f"\nTotal duplicates removed: {total_fixes}")

if __name__ == '__main__':
    main()
