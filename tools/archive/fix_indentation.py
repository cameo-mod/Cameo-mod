#!/usr/bin/env python3
"""
Fix indentation of Description lines added by fix_missing_descriptions.py.
The script added lines with space indentation but the YAML files use tabs.
This script finds lines starting with "  Description:" (space-indented) in
Buildable sections and converts them to use the same indentation as surrounding lines.
"""

import os
import re
import sys

MOD_ROOT = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), "mods", "cameo")

LOADED_PATH_PREFIXES = [
    'ContentPacks/TiberianDawn',
    'ContentPacks/RedAlert',
    'ContentPacks/TiberianSun',
    'ContentPacks/RedAlert2',
    'ContentPacks/RedAlert2Mod',
    'ContentPacks/TKM',
    'ContentPacks/D2k',
    'ContentPacks/StarCraft',
    'ContentPacks/Warcraft2',
    'ContentPacks/Outpost2',
    'ContentPacks/Core',
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

def fix_indentation(filepath):
    """Fix space-indented Description lines in Buildable sections to use tabs."""
    with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    changes = []
    in_buildable = False
    buildable_indent_str = ''
    
    for i in range(len(lines)):
        line = lines[i]
        stripped = line.rstrip('\n\r')
        
        if not stripped.strip() or stripped.strip().startswith('#'):
            continue
        
        # Detect Buildable section
        if re.match(r'^\s+Buildable:', stripped) or re.match(r'^\s+Buildable@[^:]+:', stripped):
            in_buildable = True
            buildable_indent_str = stripped[:len(stripped) - len(stripped.lstrip())]
            continue
        
        # Check if leaving Buildable section
        if in_buildable and stripped.strip():
            current_indent_str = stripped[:len(stripped) - len(stripped.lstrip())]
            if len(current_indent_str) <= len(buildable_indent_str):
                in_buildable = False
                continue
        
        # Fix Description lines that use spaces instead of tabs
        if in_buildable and re.match(r'^  Description:', stripped):
            # Find the proper indentation from neighboring lines
            proper_indent = None
            # Look at the next non-empty, non-comment line
            for j in range(i + 1, min(i + 5, len(lines))):
                next_line = lines[j].rstrip('\n\r')
                if next_line.strip() and not next_line.strip().startswith('#'):
                    next_indent = next_line[:len(next_line) - len(next_line.lstrip())]
                    if '\t' in next_indent:
                        proper_indent = next_indent
                        break
            
            if proper_indent is None:
                # Look at previous lines in Buildable
                for j in range(i - 1, max(i - 5, 0), -1):
                    prev_line = lines[j].rstrip('\n\r')
                    if prev_line.strip() and not prev_line.strip().startswith('#'):
                        prev_indent = prev_line[:len(prev_line) - len(prev_line.lstrip())]
                        if '\t' in prev_indent:
                            proper_indent = prev_indent
                            break
            
            if proper_indent is None:
                # Default: buildable_indent + one tab
                proper_indent = buildable_indent_str + '\t'
            
            # Extract the content after the space indentation
            content = stripped.lstrip()
            new_line = proper_indent + content + '\n'
            
            if new_line != lines[i]:
                changes.append((i + 1, stripped, new_line.rstrip('\n\r')))
                lines[i] = new_line
    
    if changes:
        with open(filepath, 'w', encoding='utf-8', newline='\n') as f:
            f.writelines(lines)
    
    return changes

def main():
    # Find all YAML files in loaded factions
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
        changes = fix_indentation(yf)
        if changes:
            rel = os.path.relpath(yf, MOD_ROOT)
            print(f"\n{rel}:")
            for line_num, old, new in changes:
                print(f"  Line {line_num}:")
                print(f"    OLD: {repr(old)}")
                print(f"    NEW: {repr(new)}")
            total_fixes += len(changes)
    
    print(f"\nTotal indentation fixes: {total_fixes}")

if __name__ == '__main__':
    main()
