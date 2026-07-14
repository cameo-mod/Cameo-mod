#!/usr/bin/env python3
"""
Detailed CABAL weapon analysis: reads all warhead damages, burst counts,
and computes the expected sheet damage = sum(all SpreadDamage) × burst.
"""
import re
import os

MOD_ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "mods", "cameo")
CABAL_DIR = os.path.join(MOD_ROOT, "ContentPacks", "TiberianSun", "CABAL")

def read_weapon_details(fpath):
    """Read a weapons file and extract all weapon definitions with all their properties."""
    weapons = {}
    if not os.path.exists(fpath):
        return weapons
    
    with open(fpath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    current_weapon = None
    current_warhead = None
    current_warhead_type = None
    
    for i, line in enumerate(lines):
        # Top-level weapon definition
        if re.match(r'^[A-Za-z\^][A-Za-z0-9_]*:', line) and not line.startswith(' '):
            current_weapon = line.split(':')[0].strip()
            weapons[current_weapon] = {
                'line': i + 1,
                'range': None,
                'reload': None,
                'burst': 1,
                'burst_delays': [],
                'warheads': [],  # list of (name, type, damage)
            }
            current_warhead = None
            current_warhead_type = None
        elif current_weapon:
            m = re.match(r'^\s+Range:\s*(\d+)', line)
            if m and weapons[current_weapon]['range'] is None:
                weapons[current_weapon]['range'] = int(m.group(1))
            
            m = re.match(r'^\s+ReloadDelay:\s*(\d+)', line)
            if m and weapons[current_weapon]['reload'] is None:
                weapons[current_weapon]['reload'] = int(m.group(1))
            
            m = re.match(r'^\s+Burst:\s*(\d+)', line)
            if m:
                weapons[current_weapon]['burst'] = int(m.group(1))
            
            m = re.match(r'^\s+BurstDelays:\s*(.+)', line)
            if m:
                # Could be a single number or comma-separated
                delays = [int(x.strip()) for x in m.group(1).split(',')]
                weapons[current_weapon]['burst_delays'] = delays
            
            # Warhead definition: Warhead@Name: Type
            m = re.match(r'^\s+Warhead@[^:]+:\s*(\S+)', line)
            if m:
                current_warhead = line.strip()
                current_warhead_type = m.group(1)
                weapons[current_weapon]['warheads'].append({
                    'name': line.strip(),
                    'type': current_warhead_type,
                    'damage': None,
                    'line': i + 1,
                })
            
            # Damage under a warhead
            m = re.match(r'^\s+Damage:\s*(\d+)', line)
            if m and weapons[current_weapon]['warheads']:
                if weapons[current_weapon]['warheads'][-1]['damage'] is None:
                    weapons[current_weapon]['warheads'][-1]['damage'] = int(m.group(1))
    
    return weapons

def main():
    # Read CABAL weapons
    cabal_weapons = read_weapon_details(os.path.join(CABAL_DIR, 'weapons', 'weapons.yaml'))
    
    # Read shared weapons
    shared_weapons = {}
    for wpath in [
        os.path.join(MOD_ROOT, 'weapons', 'weapons.yaml'),
        os.path.join(MOD_ROOT, 'ContentPacks', 'TiberianSun', 'Shared', 'weapons', 'weapons.yaml'),
    ]:
        shared_weapons.update(read_weapon_details(wpath))
    
    all_weapons = {**shared_weapons, **cabal_weapons}
    
    # Print weapon details for CABAL weapons
    print("=== CABAL Weapon Details ===")
    print(f"{'Weapon':<35} {'Rng':>6} {'Rld':>5} {'Brst':>5} {'TotalDmg':>9} {'Warheads'}")
    print("-" * 120)
    
    # List of CABAL weapon names (those defined in CABAL weapons.yaml)
    cabal_weapon_names = set()
    fpath = os.path.join(CABAL_DIR, 'weapons', 'weapons.yaml')
    if os.path.exists(fpath):
        with open(fpath, 'r', encoding='utf-8') as f:
            for line in f:
                if re.match(r'^[A-Za-z][A-Za-z0-9_]*:', line) and not line.startswith(' '):
                    cabal_weapon_names.add(line.split(':')[0].strip())
    
    for wname in sorted(cabal_weapon_names):
        w = all_weapons.get(wname, {})
        if not w:
            continue
        
        # Sum all SpreadDamage warhead damages
        total_dmg = 0
        warhead_info = []
        for wh in w.get('warheads', []):
            if wh['damage'] is not None:
                if 'SpreadDamage' in wh['type']:
                    total_dmg += wh['damage']
                warhead_info.append(f"{wh['type']}:{wh['damage']}")
        
        burst = w.get('burst', 1)
        sheet_dmg = total_dmg * burst
        
        print(f"{wname:<35} {w.get('range') or 0:>6} {w.get('reload') or 0:>5} {burst:>5} {sheet_dmg:>9} {' | '.join(warhead_info[:4])}")

if __name__ == "__main__":
    main()
