#!/usr/bin/env python3
"""
Audit CreateEffect warhead naming across the mod.

Canonical names: @Effect, @EffectAir, @EffectWater, @ShieldHitEffect
A concrete weapon's CreateEffect warhead is a VIOLATION if:
  - Its name is NOT one of the canonical names
  - AND its name does NOT match any template's (^-prefixed) CreateEffect warhead name
    (legitimate override case)

Reports violations grouped by file with line numbers.
"""

import os
import re
import sys
from collections import defaultdict

MOD_ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "mods", "cameo")
CANONICAL_NAMES = {"@Effect", "@EffectAir", "@EffectWater", "@ShieldHitEffect"}
CANONICAL_PREFIXES = ("@Effect", "@EffectAir", "@EffectWater", "@ShieldHitEffect")

def is_canonical(name):
    """Check if a warhead name is canonical or a suffixed variant (e.g. @Effect2)."""
    if name in CANONICAL_NAMES:
        return True
    # Check for suffixed variants: @Effect2, @EffectAir2, @ShieldHitEffect2, etc.
    for prefix in CANONICAL_PREFIXES:
        if name.startswith(prefix):
            suffix = name[len(prefix):]
            if suffix.isdigit():
                return True
    return False

def find_yaml_files(root):
    """Recursively find all .yaml files under root."""
    yaml_files = []
    for dirpath, _, filenames in os.walk(root):
        for f in filenames:
            if f.endswith(".yaml") or f.endswith(".yml"):
                yaml_files.append(os.path.join(dirpath, f))
    return yaml_files

def parse_warheads(filepath):
    """
    Parse a yaml file and return:
    - template_effect_names: set of warhead names with CreateEffect from ^-prefixed defs
    - concrete_effect_entries: list of (line_no, weapon_name, warhead_name, is_template)
    
    A warhead line looks like:
        Warhead@SomeName: CreateEffect
    """
    template_effect_names = set()
    concrete_effect_entries = []
    
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    current_weapon = None
    current_is_template = False
    
    weapon_re = re.compile(r'^(\^?[A-Za-z_][A-Za-z0-9_\-\.]*):$')
    # Match "Warhead@Name: CreateEffect" or "Warhead@Name: CreateEffect  # comment"
    warhead_re = re.compile(r'^\s*Warhead@([^:]+):\s*CreateEffect\b')
    # Also match inherits lines to track template inheritance
    inherits_re = re.compile(r'^\s*Inherits[@A-Za-z]*:\s*(\^?\S+)')
    
    for i, line in enumerate(lines, 1):
        # Top-level weapon definition (no leading tab/space)
        m = weapon_re.match(line)
        if m:
            current_weapon = m.group(1)
            current_is_template = current_weapon.startswith('^')
            continue
        
        # Warhead with CreateEffect
        m = warhead_re.match(line)
        if m:
            warhead_name = "@" + m.group(1).strip()
            if current_is_template:
                template_effect_names.add(warhead_name)
            concrete_effect_entries.append((i, current_weapon, warhead_name, current_is_template))
    
    return template_effect_names, concrete_effect_entries

def main():
    yaml_files = find_yaml_files(MOD_ROOT)
    
    # Phase 1: Collect all template CreateEffect warhead names across all files
    all_template_effect_names = set()
    all_entries = []  # (filepath, line_no, weapon_name, warhead_name, is_template)
    
    for fpath in yaml_files:
        tnames, entries = parse_warheads(fpath)
        all_template_effect_names.update(tnames)
        for (lineno, weapon, whname, is_tmpl) in entries:
            all_entries.append((fpath, lineno, weapon, whname, is_tmpl))
    
    # Phase 2: Find violations in concrete weapons
    violations_by_file = defaultdict(list)
    total_violations = 0
    total_concrete = 0
    
    for (fpath, lineno, weapon, whname, is_tmpl) in all_entries:
        if is_tmpl:
            continue  # Skip template definitions
        total_concrete += 1
        if not is_canonical(whname) and whname not in all_template_effect_names:
            rel_path = os.path.relpath(fpath, MOD_ROOT)
            violations_by_file[rel_path].append((lineno, weapon, whname))
            total_violations += 1
    
    # Report
    print(f"=== Effect Warhead Naming Audit ===")
    print(f"Template CreateEffect warhead names found: {sorted(all_template_effect_names)}")
    print(f"Total concrete CreateEffect warheads: {total_concrete}")
    print(f"Total violations: {total_violations}")
    print(f"Files with violations: {len(violations_by_file)}")
    print()
    
    for fpath in sorted(violations_by_file.keys()):
        violations = violations_by_file[fpath]
        print(f"--- {fpath} ({len(violations)} violations) ---")
        for (lineno, weapon, whname) in violations:
            print(f"  L{lineno}: {weapon} -> Warhead{whname}: CreateEffect")
        print()
    
    return 1 if total_violations > 0 else 0

if __name__ == "__main__":
    sys.exit(main())
