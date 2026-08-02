#!/usr/bin/env python3
"""
Fix CreateEffect warhead naming across the mod.

Canonical names: @Effect, @EffectAir, @EffectWater, @ShieldHitEffect
A concrete weapon's CreateEffect warhead is a VIOLATION if:
  - Its name is NOT one of the canonical names
  - AND its name does NOT match any template's (^-prefixed) CreateEffect warhead name
    (legitimate override case)

The fix renames violations to canonical names based on ValidTargets:
  - Air -> @EffectAir
  - Water -> @EffectWater
  - Shielded -> @ShieldHitEffect
  - (default) -> @Effect

If a weapon already has a canonical @Effect warhead, the rename uses a suffix
(@Effect2, @Effect3, etc.) to avoid collision.

Usage:
  python tools/audit/fix_effect_warhead_names.py          # dry-run (show changes)
  python tools/audit/fix_effect_warhead_names.py --apply   # apply changes
"""

import os
import re
import sys
from collections import defaultdict

MOD_ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "mods", "cameo")
CANONICAL_NAMES = {"@Effect", "@EffectAir", "@EffectWater", "@ShieldHitEffect"}

def find_yaml_files(root):
    yaml_files = []
    for dirpath, _, filenames in os.walk(root):
        for f in filenames:
            if f.endswith(".yaml") or f.endswith(".yml"):
                yaml_files.append(os.path.join(dirpath, f))
    return yaml_files

def parse_file(filepath):
    """
    Parse a yaml file and return:
    - template_effect_names: set of warhead names with CreateEffect from ^-prefixed defs
    - weapons: list of dicts with weapon_name, is_template, start_line, end_line, warheads
      each warhead: {name, line, create_effect, valid_targets}
    """
    template_effect_names = set()
    weapons = []
    
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    weapon_re = re.compile(r'^(\^?[A-Za-z_][A-Za-z0-9_\-\.]*):$')
    warhead_re = re.compile(r'^\s*Warhead@([^:]+):\s*(\S+)')
    valid_targets_re = re.compile(r'^\s*ValidTargets:\s*(.+)')
    
    current_weapon = None
    current_is_template = False
    current_weapon_start = 0
    current_warheads = []
    current_warhead_name = None
    current_warhead_type = None
    current_warhead_line = 0
    current_valid_targets = None
    
    def flush_warhead():
        nonlocal current_warhead_name, current_warhead_type, current_warhead_line, current_valid_targets
        if current_warhead_name is not None:
            current_warheads.append({
                'name': current_warhead_name,
                'line': current_warhead_line,
                'type': current_warhead_type,
                'valid_targets': current_valid_targets,
            })
        current_warhead_name = None
        current_warhead_type = None
        current_warhead_line = 0
        current_valid_targets = None
    
    def flush_weapon():
        nonlocal current_weapon, current_is_template, current_weapon_start, current_warheads
        flush_warhead()
        if current_weapon is not None:
            weapons.append({
                'name': current_weapon,
                'is_template': current_is_template,
                'start': current_weapon_start,
                'warheads': current_warheads[:],
            })
        current_weapon = None
        current_is_template = False
        current_warheads = []
    
    for i, line in enumerate(lines, 1):
        m = weapon_re.match(line)
        if m:
            flush_weapon()
            current_weapon = m.group(1)
            current_is_template = current_weapon.startswith('^')
            current_weapon_start = i
            continue
        
        m = warhead_re.match(line)
        if m:
            flush_warhead()
            current_warhead_name = m.group(1).strip()
            current_warhead_type = m.group(2).strip()
            current_warhead_line = i
            current_valid_targets = None
            continue
        
        if current_warhead_name is not None:
            m = valid_targets_re.match(line)
            if m:
                current_valid_targets = m.group(1).strip()
    
    flush_weapon()
    
    # Collect template effect names
    for w in weapons:
        if w['is_template']:
            for wh in w['warheads']:
                if wh['type'] == 'CreateEffect':
                    template_effect_names.add('@' + wh['name'])
    
    return template_effect_names, weapons, lines

def get_canonical_name(valid_targets):
    """Determine canonical warhead name from ValidTargets."""
    if not valid_targets:
        return '@Effect'
    vt = valid_targets.lower()
    if 'air' in vt and 'ground' not in vt and 'water' not in vt:
        return '@EffectAir'
    if vt.strip() == 'water' or (vt.startswith('water') and ',' not in vt):
        return '@EffectWater'
    if 'shielded' in vt and ',' not in vt:
        return '@ShieldHitEffect'
    return '@Effect'

def compute_renames(filepath, template_effect_names, weapons, lines):
    """
    Compute all renames needed for this file.
    Returns list of (line_no, old_name, new_name, weapon_name)
    """
    renames = []
    all_template_names = template_effect_names
    
    for w in weapons:
        if w['is_template']:
            continue
        
        # Track existing canonical names in this weapon
        existing_names = set()
        for wh in w['warheads']:
            full_name = '@' + wh['name']
            existing_names.add(full_name)
        
        # Track names we're renaming to (to avoid collisions within the weapon)
        used_names = set(existing_names)
        
        for wh in w['warheads']:
            if wh['type'] != 'CreateEffect':
                continue
            full_name = '@' + wh['name']
            if full_name in CANONICAL_NAMES:
                continue
            if full_name in all_template_names:
                continue  # legitimate override
            
            # This is a violation - compute canonical name
            canonical = get_canonical_name(wh['valid_targets'])
            
            # Avoid collision with existing canonical names
            if canonical in used_names:
                # Try suffixing
                suffix = 2
                while f"{canonical}{suffix}" in used_names:
                    suffix += 1
                canonical = f"{canonical}{suffix}"
            
            used_names.add(canonical)
            renames.append((wh['line'], full_name, canonical, w['name']))
    
    return renames

def apply_renames(filepath, renames, lines):
    """Apply renames to lines in-place."""
    # Sort by line number descending so line shifts don't affect earlier renames
    for (lineno, old_name, new_name, weapon) in sorted(renames, key=lambda r: -r[0]):
        old_str = f"Warhead{old_name}:"
        new_str = f"Warhead{new_name}:"
        # Also need to rename any -Warhead@X: removal lines
        old_remove = f"-Warhead{old_name}:"
        new_remove = f"-Warhead{new_name}:"
        line = lines[lineno - 1]
        if old_str in line:
            lines[lineno - 1] = line.replace(old_str, new_str)
        elif old_remove in line:
            lines[lineno - 1] = line.replace(old_remove, new_remove)
        else:
            print(f"  WARNING: could not find '{old_str}' on line {lineno} in {filepath}")

def main():
    apply = '--apply' in sys.argv
    
    yaml_files = find_yaml_files(MOD_ROOT)
    
    # Phase 1: Collect all template CreateEffect warhead names across all files
    all_template_effect_names = set()
    file_data = []
    
    for fpath in yaml_files:
        tnames, weapons, lines = parse_file(fpath)
        all_template_effect_names.update(tnames)
        file_data.append((fpath, weapons, lines))
    
    # Phase 2: Compute renames
    total_renames = 0
    files_changed = 0
    
    for (fpath, weapons, lines) in file_data:
        renames = compute_renames(fpath, all_template_effect_names, weapons, lines)
        if not renames:
            continue
        
        rel_path = os.path.relpath(fpath, MOD_ROOT)
        total_renames += len(renames)
        files_changed += 1
        
        print(f"--- {rel_path} ({len(renames)} renames) ---")
        for (lineno, old_name, new_name, weapon) in renames:
            print(f"  L{lineno}: {weapon}  {old_name} -> {new_name}")
        
        if apply:
            apply_renames(fpath, renames, lines)
            with open(fpath, 'w', encoding='utf-8', newline='') as f:
                f.writelines(lines)
            print(f"  [APPLIED]")
        print()
    
    mode = "APPLIED" if apply else "DRY RUN"
    print(f"=== {mode}: {total_renames} renames across {files_changed} files ===")
    if not apply:
        print("Run with --apply to make changes.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
