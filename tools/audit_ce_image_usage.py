#!/usr/bin/env python3
"""Check if CreateEffect Image: values are also referenced by non-CreateEffect traits."""
import os, re, collections

root = 'mods/cameo'
# Images from the audit
images = [
    'tsdig', 'tsioncannon', 'ionsfx', 'tspodring', 'tsmcnealmechdrop',
    'tsdroppod', 'hakurei_giphy', 'hakurei_dream', 'ra2corpse',
    'wc2_effect_sparkle', 'wc2_effect_sparkle_circle', 'wc2_effect_heal',
    'wc2_exorcism', 'wc2_catapult_impact', 'wc2_building_collapse',
    'wc2_lightng', 'wc2_effect_blizzard', 'wc2_catapult_stone_projectile_medium',
    'wc2_effect_death_and_decay', 'wc2_effect_daemon_attack', 'wc2_cannon_impact',
    'wh40kcapsule',
]

for img in images:
    ce_refs = 0
    other_refs = 0
    other_locations = []
    for dirpath, _, filenames in os.walk(root):
        for fname in filenames:
            if not fname.endswith(('.yaml', '.yml')):
                continue
            fpath = os.path.join(dirpath, fname)
            try:
                with open(fpath, 'r', encoding='utf-8') as f:
                    lines = f.readlines()
            except:
                continue
            in_ce = False
            for i, line in enumerate(lines):
                # Track CreateEffect blocks
                if 'CreateEffect' in line:
                    in_ce = True
                    continue
                if re.match(r'^[A-Za-z_^]', line) and in_ce:
                    in_ce = False
                
                # Look for references to this image
                if re.search(r'\b' + re.escape(img) + r'\b', line):
                    stripped = line.strip()
                    if stripped.startswith('#'):
                        continue
                    if in_ce:
                        ce_refs += 1
                    else:
                        # Check if it's a sequence definition (top-level image key)
                        if re.match(r'^' + re.escape(img) + r':', line):
                            continue  # skip the definition itself
                        other_refs += 1
                        other_locations.append(f"  {os.path.relpath(fpath)}:{i+1}: {line.rstrip()}")
    
    status = "CE-ONLY" if other_refs == 0 else f"ALSO-USED ({other_refs})"
    print(f"{img}: {status}")
    for loc in other_locations[:5]:
        print(loc)
