#!/usr/bin/env python
"""E3: Rename non-standard elite weapons to <base>E convention.
Only handles clear cases where the base weapon name is obvious.
Skips doctrine variants, upgrade combos, and gatling spin-ups."""
import os, re, sys

# Rename map: old_name → new_name
RENAME_MAP = {
    # Naxis aircraft
    "NaxPlanegun": "NaxPlanegunE",
    "NaxPlaneRockets": "NaxPlaneRocketsE",
    # Naxis infantry
    "NaxiWW2MachinegunnerElite": "NaxiWW2MachinegunnerE",
    # Schwarzer Mond (inherits Naxis weapons but has own elite versions)
    "NaxiBeetleLaser": "NaxiBeetleLaserE",
    "NaxiBeetleLaserAA": "NaxiBeetleLaserAAE",
    "NaxCorrosionRocketTrooper": "NaxCorrosionRocketTrooperE",
    # Heroes
    "TSBikeMissileNashwaElite": "TSBikeMissileNashwaE",
    # RA2 Soviet vehicles
    "V3LaunchElite": "V3LaunchE",
    "RA2KirovBomb_nuclear_Elite": "RA2KirovBomb_nuclear_E",
    # Valentine
    "CuteKirovBombElite": "CuteKirovBombE",
}

root = "mods/cameo"
changes = []

for dirpath, _, filenames in os.walk(root):
    for fn in filenames:
        if not fn.endswith(".yaml"):
            continue
        fpath = os.path.join(dirpath, fn)
        try:
            content = open(fpath, encoding="utf-8").read()
        except Exception:
            continue
        
        original = content
        for old_name, new_name in RENAME_MAP.items():
            # Match whole word boundaries (weapon names are alphanumeric + underscore)
            # Use regex with word boundary that works with underscores
            pattern = r'(?<![A-Za-z0-9_])' + re.escape(old_name) + r'(?![A-Za-z0-9_])'
            matches = list(re.finditer(pattern, content))
            if matches:
                content = re.sub(pattern, new_name, content)
                for m in matches:
                    changes.append((fpath, old_name, new_name))
        
        if content != original:
            with open(fpath, 'w', encoding='utf-8', newline='\n') as f:
                f.write(content)

print(f"E3 weapon renames: {len(changes)} references updated across {len(set(c[0] for c in changes))} files\n")
for fpath, old, new in sorted(changes):
    short = fpath.replace("\\", "/").replace("mods/cameo/", "")
    print(f"  {short}: {old} → {new}")
