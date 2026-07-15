#!/usr/bin/env python3
"""Extract missing fluent keys and their actor Tooltip names from rules."""
import re
import os
import glob

MODS = os.path.join(os.path.dirname(__file__), "..", "mods", "cameo")

# Missing keys from audit
MISSING_KEYS = [
    "consumer_items_impulse.description",
    "consumer_items_luxury_wares.description",
    "consumer_items_wares.description",
    "ra_doctrine_conscription.description",
    "ra_doctrine_heavyarmor.description",
    "ra_doctrine_industrialefficiency.description",
    "ra_doctrine_inferno.description",
    "ra_doctrine_nuclearwar.description",
    "ra_doctrine_teslatech.description",
    "ra_promotion_hurricanerocketpod.description",
    "ra_promotion_superoptics.description",
    "ra_promotion_targetingcomputer.description",
    "ra_upgrade_afterburners.description",
    "ra_upgrade_autoloaders.description",
    "ra_upgrade_hazmatsuits.description",
    "ra_upgrade_highexplosiverockets.description",
    "ra_upgrade_incendiarybullets.description",
    "ra_upgrade_massproduction.description",
    "ra_upgrade_menofsteel.description",
    "ra_upgrade_nuclearshells.description",
    "ra_upgrade_reactoroverload.description",
    "ra_upgrade_scorchedearth.description",
    "ra_upgrade_shtoradefensesystem.description",
    "ra_upgrade_stalinium.description",
    "ra_upgrade_teslaarcing.description",
    "ra_upgrade_teslarockets.description",
    "ra_upgrade_nuclearrockets.description",
    "ra_upgrade_unstableisotopes.description",
    "ra_upgrade_vengeance.description",
    "ra_upgrade_wareconomy.description",
]

# Search for each key in rules to find the actor and its Tooltip Name
results = {}
for f in glob.glob(os.path.join(MODS, "rules", "*.yaml")) + glob.glob(os.path.join(MODS, "ContentPacks", "**", "*.yaml"), recursive=True):
    with open(f, 'r', encoding='utf-8', errors='ignore') as fh:
        content = fh.read()
    for key in MISSING_KEYS:
        if key in content:
            # Find the actor block containing this key
            # Look backwards from the key position to find the actor name and Tooltip Name
            idx = content.find(key)
            # Get surrounding context
            start = max(0, idx - 500)
            chunk = content[start:idx + len(key) + 50]
            # Find actor name (last top-level definition before the key)
            actor_matches = list(re.finditer(r'^([A-Za-z][A-Za-z0-9_.-]*):', chunk, re.MULTILINE))
            actor_name = actor_matches[-1].group(1) if actor_matches else "?"
            # Find Tooltip Name
            name_match = re.search(r'Name:\s*(.+)', chunk)
            tooltip_name = name_match.group(1).strip() if name_match else "?"
            results[key] = (actor_name, tooltip_name)

for key in MISSING_KEYS:
    if key in results:
        actor, name = results[key]
        print(f"{key} = {name}")
    else:
        print(f"{key} = ???")
