#!/usr/bin/env python3
"""
Build a comprehensive mapping of CABAL units to their correct formula inputs.
Reads yaml to determine:
- Actual weapon total damage (sum SpreadDamage × burst)
- Actual weapon ReloadDelay + (burst-1) × BurstDelay
- Actual weapon Range
- Unit HP, Speed, Cost from rules yaml
- Special abilities (for K)
- Tech tier (for M)
- Unit class (for L)
"""
import re
import os

MOD_ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "mods", "cameo")
CABAL_DIR = os.path.join(MOD_ROOT, "ContentPacks", "TiberianSun", "CABAL")

def read_weapon_full(fpath):
    """Read weapon file and extract complete weapon definitions with inheritance."""
    weapons = {}
    if not os.path.exists(fpath):
        return weapons
    
    with open(fpath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    current_weapon = None
    for i, line in enumerate(lines):
        if re.match(r'^[A-Za-z\^][A-Za-z0-9_]*:', line) and not line.startswith(' '):
            current_weapon = line.split(':')[0].strip()
            if current_weapon not in weapons:
                weapons[current_weapon] = {
                    'line': i + 1,
                    'range': None,
                    'reload': None,
                    'burst': 1,
                    'burst_delays': [],
                    'spread_damages': [],
                    'inherits': None,
                }
        elif current_weapon:
            m = re.match(r'^\s+Inherits:\s*(\S+)', line)
            if m and weapons[current_weapon]['inherits'] is None:
                weapons[current_weapon]['inherits'] = m.group(1)
            
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
                delays = [int(x.strip()) for x in m.group(1).split(',')]
                weapons[current_weapon]['burst_delays'] = delays
            
            # Track Warhead@xxx: SpreadDamage and their Damage values
            m = re.match(r'^\s+Warhead@[^:]+:\s*SpreadDamage', line)
            if m:
                # Next few lines should have Damage
                for j in range(i+1, min(i+5, len(lines))):
                    dm = re.match(r'^\s+Damage:\s*(\d+)', lines[j])
                    if dm:
                        weapons[current_weapon]['spread_damages'].append(int(dm.group(1)))
                        break
    
    return weapons

def main():
    # Read CABAL weapons
    cabal_weapons = read_weapon_full(os.path.join(CABAL_DIR, 'weapons', 'weapons.yaml'))
    
    # Read shared weapons  
    shared_weapons = {}
    for wpath in [
        os.path.join(MOD_ROOT, 'weapons', 'weapons.yaml'),
        os.path.join(MOD_ROOT, 'ContentPacks', 'TiberianSun', 'Shared', 'weapons', 'weapons.yaml'),
    ]:
        if os.path.exists(wpath):
            shared_weapons.update(read_weapon_full(wpath))
    
    all_weapons = {**shared_weapons, **cabal_weapons}
    
    # Read CABAL rules to get actor -> weapon mapping
    actor_weapons = {}
    actor_stats = {}
    for fname in ['infantry.yaml', 'vehicles.yaml', 'aircraft.yaml']:
        fpath = os.path.join(CABAL_DIR, 'rules', fname)
        if not os.path.exists(fpath):
            continue
        with open(fpath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        current_actor = None
        for line in content.split('\n'):
            if re.match(r'^[a-z][a-z0-9_]*:', line):
                current_actor = line.split(':')[0].strip()
                actor_weapons[current_actor] = []
                actor_stats[current_actor] = {}
            elif current_actor:
                m = re.match(r'^\s+Weapon:\s*(\S+)', line)
                if m:
                    weapon_name = m.group(1)
                    # Skip animation weapons and non-damage weapons
                    actor_weapons[current_actor].append(weapon_name)
                
                m = re.match(r'^\s+HP:\s*(\d+)', line)
                if m and 'hp' not in actor_stats[current_actor]:
                    actor_stats[current_actor]['hp'] = int(m.group(1))
                
                m = re.match(r'^\s+Speed:\s*(\d+)', line)
                if m and 'speed' not in actor_stats[current_actor]:
                    actor_stats[current_actor]['speed'] = int(m.group(1))
                
                m = re.match(r'^\s+Cost:\s*(\d+)', line)
                if m and 'cost' not in actor_stats[current_actor]:
                    actor_stats[current_actor]['cost'] = int(m.group(1))
    
    # Print comprehensive weapon analysis for each CABAL unit
    print("=== CABAL Unit Weapon Analysis ===")
    print(f"{'Actor':<35} {'Weapon':<30} {'Rng':>6} {'Rld':>5} {'Brst':>5} {'TotDmg':>8} {'SheetRld':>9} {'AllSpreadDmgs'}")
    print("-" * 140)
    
    # List of CABAL actors from the sheet (ordered)
    cabal_actors = [
        'cabal_artilleryspider', 'cabal_avatar', 'cabal_berserker', 'cabal_coredefender',
        'cabal_cyborgcommando', 'cabal_cyborgcommandov2', 'cabal_cyborginfantry',
        'cabal_cyborgreaper', 'cabal_devout', 'cabal_dissolver', 'cabal_eliminator800',
        'cabal_hunterkillermk1', 'cabal_hunterkillermk2', 'cabal_laserspider',
        'cabal_legion', 'cabal_manticore', 'cabal_mantis', 'cabal_mothership',
        'cabal_overkillcarryall', 'cabal_ravager', 'cabal_rocketcyborg',
        'cabal_spidercnc4', 'cabal_tarantula', 'cabal_wasp', 'cabal_wasp_striker',
        'cabal_hunterkillermk1_elite', 'cabal_heavyreaper', 'cabal_widow',
        'cabal_ascended', 'cabal_beholder', 'cabal_hackercyborg',
        'cabal_hunterkillermk2_drone', 'cabal_scarabapc', 'cabal_tiberiumharvester',
    ]
    
    for actor in cabal_actors:
        weapons = actor_weapons.get(actor, [])
        stats = actor_stats.get(actor, {})
        
        if not weapons:
            print(f"{actor:<35} {'(no weapons)':<30}")
            continue
        
        # Find the primary combat weapon (first one that has damage)
        for wname in weapons:
            w = all_weapons.get(wname, {})
            if not w:
                print(f"{actor:<35} {wname:<30} NOT FOUND")
                continue
            
            total_spread = sum(w.get('spread_damages', []))
            burst = w.get('burst', 1)
            burst_delays = w.get('burst_delays', [])
            reload = w.get('reload', 0)
            
            # Sheet ReloadDelay = weapon ReloadDelay + (burst-1) × BurstDelay
            if burst > 1 and burst_delays:
                sheet_reload = reload + (burst - 1) * burst_delays[0]
            else:
                sheet_reload = reload
            
            # Sheet Damage = total SpreadDamage × burst
            sheet_damage = total_spread * burst
            
            spread_info = str(w.get('spread_damages', []))
            
            print(f"{actor:<35} {wname:<30} {w.get('range') or 0:>6} {reload:>5} {burst:>5} {sheet_damage:>8} {sheet_reload:>9} {spread_info}")

if __name__ == "__main__":
    main()
