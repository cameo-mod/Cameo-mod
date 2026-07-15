#!/usr/bin/env python3
"""Add ai.yaml reference to content.yaml files — simple version."""
import os
import re

TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.join(TOOLS_DIR, "..")
CONTENT_PACKS = os.path.join(REPO_ROOT, "mods", "cameo", "ContentPacks")

def find_content_yamls():
    result = []
    for root, dirs, files in os.walk(CONTENT_PACKS):
        for f in files:
            if f == "content.yaml":
                result.append(os.path.join(root, f))
    return sorted(result)

def add_ai_line(content_yaml_path):
    with open(content_yaml_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    if 'ai.yaml' in content:
        return False
    
    pack_dir = os.path.dirname(content_yaml_path)
    rel = os.path.relpath(pack_dir, CONTENT_PACKS).replace('\\', '/')
    ai_line = f"\tContentPacks|{rel}/yaml/ai.yaml"
    
    # Find the Rules: section and add after the last entry
    lines = content.split('\n')
    new_lines = []
    in_rules = False
    inserted = False
    
    for i, line in enumerate(lines):
        if line.strip() == 'Rules:':
            in_rules = True
            new_lines.append(line)
            continue
        
        if in_rules and not inserted:
            # Check if next line starts a new section or is blank
            if line.startswith('\t') and line.strip() and not line.strip().endswith(':'):
                # Rules entry — keep collecting
                new_lines.append(line)
            elif line.strip() == '' or (line.strip() and line.strip().endswith(':')):
                # End of Rules section — insert ai.yaml before the blank/section line
                new_lines.append(ai_line)
                inserted = True
                in_rules = False
                new_lines.append(line)
            else:
                new_lines.append(line)
        else:
            new_lines.append(line)
    
    # If we never inserted (Rules was last section), append at end
    if not inserted:
        new_lines.append(ai_line)
    
    new_content = '\n'.join(new_lines)
    with open(content_yaml_path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(new_content)
    
    return True

if __name__ == '__main__':
    content_yamls = find_content_yamls()
    added = 0
    for cy in content_yamls:
        if add_ai_line(cy):
            rel = os.path.relpath(cy, REPO_ROOT)
            print(f"  Added: {rel}")
            added += 1
    print(f"\nAdded ai.yaml to {added} content.yaml files")
