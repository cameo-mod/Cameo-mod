#!/usr/bin/env python3
"""Extract all upgrade actor names per faction and output as Lua table."""
import os
import re
import json

CONTENT_PACKS = os.path.join(os.path.dirname(__file__), "..", "mods", "cameo", "ContentPacks")

# Map faction directory to MCV name (from the Waves table in script.lua)
FACTION_TO_MCV = {
    "TiberianDawn/GDI": "td_gdi_mobileconstructionvehicle",
    "TiberianDawn/Nod": "td_nod_mobileconstructionvehicle",
    "RedAlert/Allies": "ra1_allies_alliedmobileconstructionvehicle",
    "RedAlert/Soviets": "ra1_soviets_mobileconstructionvehicle",
    "RedAlert/Japan": "japan_japanesemobileconstructionvehicle",
    "RedAlert2/Allies": "ra2_allies_alliedmobileconstructionvehicle",
    "RedAlert2/Soviets": "ra2_soviets_mobileconstructionvehicle",
    "RedAlert2/Yuri": "yuri_mobileconstructionvehicle",
    "RedAlert2Mod/AsianAlliance": "asianalliance_asianmobileconstructionvehicle",
    "RedAlert2Mod/Consortium": "steelconsortium_consortiummobileconstructionvehicle",
    "RedAlert2Mod/Naxis": "naxis_naximobileconstructionvehicle",
    "RedAlert2Mod/SchwarzerMond": "schwarzermond_naxismobileconstructionvehicle",
    "RedAlert2Mod/Syndicate": "latinsyndicate_syndicatemobileconstructionvehicle",
    "RedAlert2Mod/FutureTech": "futuretech_mobileconstructionvehicle",
    "RedAlert2Mod/TKM": "tkm_mobileconstructionvehicletkm",
    "D2k/Ordos": "ordos_mobileconstructionvehicle",
    "D2k/Ixian": "ixian_mobileconstructionvehicle",
    "TiberianSun/GDI": "ts_gdi_mobileconstructionvehicle",
    "TiberianSun/Nod": "ts_nod_mobileconstructionvehicle",
    "TiberianSun/CABAL": "cabal_mobileconstructionvehicle",
    "TiberianSun/Forgotten": "forgotten_mobileconstructionvehicle",
    "Warcraft2/Humans": "wc2_humans_mobileconstructionvehiclehuman",
    "Warcraft2/Orcs": "wc2_orcs_mobileconstructionvehicleorc",
    "StarCraft/Terran": "terran_mobileconstructionvehicle",
    "StarCraft/Protoss": "protoss_nexus",
    "StarCraft/Zerg": "zerg_hatchery",
}

def get_faction_from_path(filepath):
    parts = filepath.replace('\\', '/').split('/')
    for i, part in enumerate(parts):
        if part == 'ContentPacks' and i + 1 < len(parts):
            remaining = parts[i+1:]
            if len(remaining) >= 2:
                return f"{remaining[0]}/{remaining[1]}"
    return None

def scan_upgrades_file(filepath):
    upgrades = []
    try:
        with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
            for line in f:
                m = re.match(r'^([a-z0-9_]+_upgrade_[a-z0-9_]+):\s*$', line)
                if m:
                    name = m.group(1)
                    # Skip proxy actors
                    if "_proxy_actor" in name:
                        continue
                    if name not in upgrades:
                        upgrades.append(name)
    except Exception as e:
        print(f"Error: {e}")
    return upgrades

def main():
    results = {}
    for root, dirs, files in os.walk(CONTENT_PACKS):
        for filename in files:
            if filename == "upgrades.yaml":
                filepath = os.path.join(root, filename)
                faction = get_faction_from_path(filepath)
                if faction:
                    upgrades = scan_upgrades_file(filepath)
                    if upgrades and faction not in results:
                        results[faction] = upgrades
                    elif upgrades:
                        # Merge (some factions have upgrades in shared files)
                        for u in upgrades:
                            if u not in results[faction]:
                                results[faction].append(u)

    # Also check Shared directories and merge into their parent factions
    # RA1 Shared upgrades belong to both allies and soviets
    shared_map = {
        "RedAlert/Shared": ["RedAlert/Allies", "RedAlert/Soviets", "RedAlert/Japan"],
        "RedAlert2/Shared": ["RedAlert2/Allies", "RedAlert2/Soviets", "RedAlert2/Yuri"],
    }
    
    for shared, targets in shared_map.items():
        if shared in results:
            for target in targets:
                if target in results:
                    for u in results[shared]:
                        if u not in results[target]:
                            results[target].append(u)
            del results[shared]
    
    # Output as Lua table
    print("FactionUpgrades = {")
    for faction_dir in sorted(results.keys()):
        mcv = FACTION_TO_MCV.get(faction_dir)
        if mcv is None:
            continue
        upgrades = results[faction_dir]
        print(f'\t["{mcv}"] = {{')
        for u in upgrades:
            print(f'\t\t"{u}",')
        print(f"\t}},")
    print("}")
    
    # Print summary
    print("\n--- Summary ---")
    for faction_dir in sorted(results.keys()):
        mcv = FACTION_TO_MCV.get(faction_dir)
        if mcv:
            print(f"{faction_dir} ({mcv}): {len(results[faction_dir])} upgrades")

if __name__ == "__main__":
    main()
