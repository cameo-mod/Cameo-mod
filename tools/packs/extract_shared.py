#!/usr/bin/env python3
"""
extract_shared.py — Move remaining shared content from a monolith rules/weapons/sequences
file into a ContentPacks/<Theme>/Shared/ pack.

Usage:
    python tools/packs/extract_shared.py RedAlert

This splits rules/redalert.yaml, weapons/redalert.yaml, sequences/redalert.yaml
into ContentPacks/RedAlert/Shared/yaml/*.yaml files, categorized by concern.
"""

import sys
import os
import re
from pathlib import Path

def parse_blocks(filepath):
    """Parse a YAML file into top-level blocks (key + indented body)."""
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    blocks = []
    current_key = None
    current_lines = []
    
    for i, line in enumerate(lines):
        # Top-level key: starts at column 0 with a non-whitespace, non-comment char
        stripped = line.rstrip('\n')
        if stripped and not stripped.startswith(' ') and not stripped.startswith('\t') and not stripped.startswith('#'):
            # Save previous block
            if current_key is not None:
                blocks.append((current_key, current_lines))
            current_key = stripped
            current_lines = [line]
        else:
            if current_key is not None:
                current_lines.append(line)
    
    # Don't forget the last block
    if current_key is not None:
        blocks.append((current_key, current_lines))
    
    return blocks

def categorize_rules_block(key):
    """Categorize a rules block by its key name."""
    # Remove trailing colon
    name = key.rstrip(':')
    
    # World block (factions, starting units)
    if name == 'World':
        return 'faction'
    
    # Starting units
    if name.startswith('StartingUnits'):
        return 'faction'
    
    # Templates (start with ^)
    if name.startswith('^'):
        # Doctrine/upgrade templates
        if any(x in name for x in ['Doctrine', 'Upgrade', 'TeamUpgrade', 'Promotion']):
            return 'upgrades'
        return 'templates'
    
    # Proxy actors (team upgrade proxies)
    if '_proxy_actor' in name:
        return 'upgrades'
    
    # Team upgrade actors
    if name.startswith('team_upgrade.'):
        return 'upgrades'
    
    # Infantry actors
    infantry_names = {'RAE1', 'rae1', 'RAE3', 'rae3', 'RAE6', 'rae6', 
                      'EINSTEIN', 'einstein', 'DELPHI', 'delphi', 
                      'RACHAN', 'rachan', 'GNRL', 'gnrl', 'TECH1', 'tech1',
                      'ra_cons_molo'}
    if name in infantry_names or name.startswith('^RAE'):
        return 'infantry'
    
    # Vehicle actors
    vehicle_names = {'RAAPC', 'raapc', 'alliedcybertank', 
                     'RATRAN.Husk', 'modhip.husk', 'ROCKETANGEL.husk'}
    if name in vehicle_names:
        return 'vehicles'
    
    # Aircraft actors
    aircraft_names = {'U2', 'u2', 'U3', 'u3', 'BADR', 'BADR.Soviet', 'BADR.Bomber',
                      'BADR.Allies', 'BADR.Japan', 'C17.Bomber', 'C17.Paradrop',
                      'jsuperbomber', 'rapierjumpjet.Husk'}
    if name in aircraft_names:
        return 'aircraft'
    
    # Naval actors
    naval_names = {'PT', 'pt', 'DD', 'dd', 'CA', 'ca', 'SS', 'ss', 'MSUB', 'msub',
                   'RALST', 'ralst', 'japanspeedboat', 'yamatobattleship',
                   'japancarrier', 'zerofighter'}
    if name in naval_names:
        return 'naval'
    
    # Building actors
    building_names = {'POWR', 'powr', 'APWR', 'apwr', 'KENN', 'kenn',
                      'RASILO', 'rasilo', 'RA1SYRD', 'ra1syrd', 
                      'RA1JSYRD', 'ra1jsyrd', 'RASPEN', 'raspen'}
    if name in building_names:
        return 'buildings'
    
    # Special/misc actors
    misc_names = {'ChronoVortex', 'chronovortex', 'ChronoVortexFade', 'chronovortexfade'}
    if name in misc_names:
        return 'misc'
    
    # Default: templates
    print(f"  WARNING: Unknown block '{name}' — defaulting to templates")
    return 'templates'

def write_blocks_to_files(blocks, output_dir, concern):
    """Write blocks to a single YAML file for the given concern."""
    if not blocks:
        return
    
    filepath = output_dir / f'{concern}.yaml'
    with open(filepath, 'w', encoding='utf-8', newline='\n') as f:
        for key, lines in blocks:
            f.writelines(lines)
            # Ensure blank line between blocks
            if lines and lines[-1].strip():
                f.write('\n')
    
    print(f"  Wrote {filepath} ({len(blocks)} blocks)")

def main():
    if len(sys.argv) < 2:
        print("Usage: python tools/packs/extract_shared.py <Theme>")
        print("Example: python tools/packs/extract_shared.py RedAlert")
        sys.exit(1)
    
    theme = sys.argv[1]
    mod_root = Path(__file__).resolve().parent.parent.parent / 'mods' / 'cameo'
    
    # Source files
    rules_src = mod_root / 'rules' / 'redalert.yaml'
    weapons_src = mod_root / 'weapons' / 'redalert.yaml'
    sequences_src = mod_root / 'sequences' / 'redalert.yaml'
    
    # Output directory
    shared_dir = mod_root / 'ContentPacks' / theme / 'Shared' / 'yaml'
    shared_dir.mkdir(parents=True, exist_ok=True)
    
    # Process rules
    print(f"Processing {rules_src}...")
    if rules_src.exists():
        blocks = parse_blocks(rules_src)
        categories = {}
        for key, lines in blocks:
            cat = categorize_rules_block(key)
            if cat not in categories:
                categories[cat] = []
            categories[cat].append((key, lines))
        
        for cat, cat_blocks in sorted(categories.items()):
            write_blocks_to_files(cat_blocks, shared_dir, cat)
    
    # Process weapons — all go to weapons.yaml
    print(f"Processing {weapons_src}...")
    if weapons_src.exists():
        blocks = parse_blocks(weapons_src)
        write_blocks_to_files(blocks, shared_dir, 'weapons')
    
    # Process sequences — all go to sequences.yaml
    print(f"Processing {sequences_src}...")
    if sequences_src.exists():
        blocks = parse_blocks(sequences_src)
        write_blocks_to_files(blocks, shared_dir, 'sequences')
    
    # Generate content.yaml
    content_yaml = shared_dir.parent / 'content.yaml'
    
    # Determine which rule files exist
    rule_files = []
    concern_order = ['faction', 'templates', 'infantry', 'vehicles', 'aircraft', 
                     'naval', 'buildings', 'defenses', 'upgrades', 'promotions', 'misc']
    for concern in concern_order:
        f = shared_dir / f'{concern}.yaml'
        if f.exists():
            rule_files.append(f'ContentPacks|{theme}/Shared/yaml/{concern}.yaml')
    
    with open(content_yaml, 'w', encoding='utf-8', newline='\n') as f:
        f.write('Rules:\n')
        for rf in rule_files:
            f.write(f'\t{rf}\n')
        
        weapons_file = shared_dir / 'weapons.yaml'
        sequences_file = shared_dir / 'sequences.yaml'
        
        if weapons_file.exists():
            f.write('\nWeapons:\n')
            f.write(f'\tContentPacks|{theme}/Shared/yaml/weapons.yaml\n')
        
        if sequences_file.exists():
            f.write('\nSequences:\n')
            f.write(f'\tContentPacks|{theme}/Shared/yaml/sequences.yaml\n')
    
    print(f"\nWrote {content_yaml}")
    print(f"\nDone! Shared pack created at {shared_dir.parent}")
    print(f"\nNext steps:")
    print(f"  1. Update mod.yaml: replace 'Include: ContentPacks/{theme}/content.yaml' with 'Include: ContentPacks/{theme}/Shared/content.yaml'")
    print(f"  2. Remove monolith references from mod.yaml (rules/redalert.yaml, weapons/redalert.yaml, sequences/redalert.yaml)")
    print(f"  3. Verify with audit suite + boot test")

if __name__ == '__main__':
    main()
