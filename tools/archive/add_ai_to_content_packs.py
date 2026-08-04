#!/usr/bin/env python3
"""Add ai.yaml references to all ContentPack content.yaml files.
Also create stub ai.yaml files for packs that don't have one yet."""
import os
import re

TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.join(TOOLS_DIR, "..")
CONTENT_PACKS = os.path.join(REPO_ROOT, "mods", "cameo", "ContentPacks")

STUB_TEMPLATE = """# ai.yaml — {faction_name} faction AI configuration
#
# ARCHITECTURE NOTE
# =================
# All AI bot modules (BaseBuilderBotModuleCA, UnitBuilderBotModuleCA,
# SquadManagerBotModuleCA, SupportPowerBotASModule, etc.) are defined
# as single trait instances on the Player: actor in the global
# mods/cameo/ai/ai.yaml. Their sub-sections (BuildingLimits,
# BuildingFractions, UnitsToBuild, UnitLimits) contain ALL faction
# data in single dictionaries that CANNOT be split across files —
# OpenRA's YAML merge replaces trait instances with the same @name,
# it does not deep-merge their sub-sections.
#
# This file is loaded as a Rules: entry but contains no traits —
# it is a placeholder ready for future per-faction bot module
# splitting (see ROADMAP Phase E backlog).
#
# REFERENCE DATA (from global ai.yaml)
# ====================================
# (No faction-specific entries found in global ai.yaml for this faction.
#  This faction may share AI data with a parent theme section.)
Player:

"""

def find_content_yamls():
    """Find all content.yaml files and their corresponding pack directories."""
    result = []
    for root, dirs, files in os.walk(CONTENT_PACKS):
        for f in files:
            if f == "content.yaml":
                result.append(os.path.join(root, f))
    return sorted(result)

def get_pack_name(content_yaml_path):
    """Get a human-readable pack name from the path."""
    rel = os.path.relpath(content_yaml_path, CONTENT_PACKS)
    parts = rel.replace('\\', '/').split('/')
    if len(parts) == 1:
        return parts[0]
    elif parts[-1] == 'content.yaml':
        return '/'.join(parts[:-1])
    return rel

def has_ai_yaml(content_yaml_path):
    """Check if content.yaml already references an ai.yaml."""
    with open(content_yaml_path, 'r', encoding='utf-8') as f:
        content = f.read()
    return 'ai.yaml' in content

def add_ai_to_content_yaml(content_yaml_path, pack_name):
    """Add ai.yaml reference to content.yaml under Rules: section."""
    with open(content_yaml_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Determine the pack-relative path for the ai.yaml
    pack_dir = os.path.dirname(content_yaml_path)
    yaml_dir = os.path.join(pack_dir, "yaml")
    ai_file = os.path.join(yaml_dir, "ai.yaml")
    
    # Create stub ai.yaml if it doesn't exist
    if not os.path.exists(ai_file):
        os.makedirs(yaml_dir, exist_ok=True)
        with open(ai_file, 'w', encoding='utf-8', newline='\n') as f:
            f.write(STUB_TEMPLATE.format(faction_name=pack_name))
        print(f"  Created stub: {os.path.relpath(ai_file, REPO_ROOT)}")
    
    # Determine the ContentPacks-relative path
    rel_path = os.path.relpath(ai_file, CONTENT_PACKS).replace('\\', '/')
    
    # Build the Rules line
    rules_line = f"\tContentPacks|{rel_path}"
    
    # Check if already present
    if rules_line in content:
        return False
    
    # Add after the last Rules: entry
    lines = content.split('\n')
    new_lines = []
    rules_section = False
    last_rules_line = -1
    in_rules = False
    
    for i, line in enumerate(lines):
        stripped = line.strip()
        if stripped == 'Rules:':
            in_rules = True
            new_lines.append(line)
            continue
        
        if in_rules:
            # Check if this line is a rules entry (starts with tab)
            if line.startswith('\t') and stripped and not stripped.endswith(':'):
                last_rules_line = len(new_lines)
                new_lines.append(line)
            elif line.startswith('\t') and stripped.endswith(':'):
                # New section header
                in_rules = False
                # Insert ai.yaml before this section
                new_lines.append(rules_line)
                new_lines.append(line)
            elif not line.strip():
                new_lines.append(line)
            else:
                in_rules = False
                new_lines.append(line)
        else:
            new_lines.append(line)
    
    # If we were still in rules at the end, append
    if in_rules and last_rules_line >= 0:
        new_lines.insert(last_rules_line + 1, rules_line)
    
    new_content = '\n'.join(new_lines)
    with open(content_yaml_path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(new_content)
    
    return True

if __name__ == '__main__':
    content_yamls = find_content_yamls()
    print(f"Found {len(content_yamls)} content.yaml files")
    
    added = 0
    skipped = 0
    for cy in content_yamls:
        pack_name = get_pack_name(cy)
        if has_ai_yaml(cy):
            print(f"  Skip (already has ai.yaml): {pack_name}")
            skipped += 1
            continue
        
        if add_ai_to_content_yaml(cy, pack_name):
            print(f"  Added: {pack_name}")
            added += 1
    
    print(f"\nAdded ai.yaml to {added} content.yaml files, skipped {skipped}")
