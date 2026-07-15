#!/usr/bin/env python3
"""Find where each granted-never-consumed condition is granted."""
import os
import re

MODS = os.path.join(os.path.dirname(__file__), "..", "mods", "cameo")
CONDITIONS = [
    "!aircraft-turning", "armory-rank", "chaosgas && !untargetable", "defensebot",
    "disable_movement", "emptesla", "harkonnenexplode", "hnavyshield_upg",
    "littlebuilderenable", "ordos_upgrade_lightfactory", "propaganda",
    "ra2_soviets_doctrine_conscription", "shade-ready", "up_tsunami.asian",
    "yuri_doctrine_psioniclegion"
]

# Clean up the compound condition
CONDITIONS = [
    "aircraft-turning", "armory-rank", "defensebot",
    "disable_movement", "emptesla", "harkonnenexplode", "hnavyshield_upg",
    "littlebuilderenable", "ordos_upgrade_lightfactory", "propaganda",
    "ra2_soviets_doctrine_conscription", "shade-ready", "up_tsunami.asian",
    "yuri_doctrine_psioniclegion"
]

for cond in CONDITIONS:
    print(f"\n=== {cond} ===")
    for root, dirs, files in os.walk(MODS):
        for f in files:
            if not f.endswith(('.yaml',)):
                continue
            fp = os.path.join(root, f)
            with open(fp, 'r', encoding='utf-8') as fh:
                lines = fh.readlines()
            for i, line in enumerate(lines, 1):
                if cond in line and ('GrantCondition' in line or 'GrantedCondition' in line or 'Condition:' in line):
                    rel = os.path.relpath(fp, MODS)
                    print(f"  {rel}:{i}: {line.rstrip()}")
