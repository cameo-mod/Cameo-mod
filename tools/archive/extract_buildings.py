#!/usr/bin/env python3
"""Extract building actor names per faction from ContentPacks YAML files."""
import os
import re
import json

CONTENT_PACKS = os.path.join(os.path.dirname(__file__), "..", "mods", "cameo", "ContentPacks")

# Keywords to identify building types
POWER_KEYWORDS = ["powerplant", "power", "reactor", "bioreactor", "thermal", "crystalpowerextractor", "pigfarm", "farm", "solar"]
BARRACKS_KEYWORDS = ["barracks", "handofnod", "handof", "cyborgfactory", "racks"]
WARFACTORY_KEYWORDS = ["warfactory", "weaponsfactory", "weap", "airstrip", "starport", "factory"]
REFINERY_KEYWORDS = ["refinery", "orerefinery", "tiberiumrefinery", "slaveminer", "refin"]

def is_building_line(line):
    """Check if a line defines a building actor (top-level YAML key)."""
    match = re.match(r'^([a-z0-9_]+):\s*$', line)
    if not match:
        return None
    return match.group(1)

def classify_building(name):
    """Classify a building name into a category."""
    lower = name.lower()
    # Skip non-building actors
    if any(x in lower for x in ["constructionyard", "mobileconstructionvehicle", "construction",
                                 "wall", "gate", "fence", "barrier", "sandbag",
                                 "turret", "tower", "coil", "cannon", "obelisk", "sam",
                                 "bunker", "pillbox", "gun", "defense", "defensive",
                                 "superweapon", "nuke", "chronosphere", "ironcurtain",
                                 "gap", "radar", "comms", "tower", "uplink", "dropod",
                                 "silo", "techcenter", "techlab", "lab", "temple",
                                 "upgrade", "icon", "sequence", "decorations",
                                 "light", "lamp", "crate", "civilian", "neutral",
                                 "tree", "rock", "tiberium", "ore", "gem",
                                 "shipyard", "navalyard", "subpen", "seaport",
                                 "helipad", "airfield", "airport", "radar",
                                 "service", "repair", "depot",
                                 "cloning", "vats", "mindcontrol", "psychic",
                                 "battle", "bunker", "pillbox", "gun",
                                 "church", "silo", "dropzone", "supply",
                                 "missilesilo", "weatherset", "geneticmutator",
                                 "dominator", "wonder", "vault", "banshee",
                                 "stealthgenerator", "droppod", "seeker",
                                 "ioncannon", "hunterseeker", "templ", "select",
                                 "powerup", "crate", "flag", "beacon",
                                 "hospital", "hut", "tent", "guardtower",
                                 "advguardtower", "sentry", "turret",
                                 "obelisk", "laser", "sam", "rocket",
                                 "emp", "firewall", "cybernetic",
                                 "mine", "oil", "derrick",
                                 "condition", "trigger", "spawn",
                                 "conyard", "mcv", "deploy"]):
        return None

    for kw in POWER_KEYWORDS:
        if kw in lower and "upgrade" not in lower:
            return "power"
    for kw in BARRACKS_KEYWORDS:
        if kw in lower and "upgrade" not in lower:
            return "barracks"
    for kw in WARFACTORY_KEYWORDS:
        if kw in lower and "upgrade" not in lower:
            return "warfactory"
    for kw in REFINERY_KEYWORDS:
        if kw in lower and "upgrade" not in lower and "slaveminer_deployed" not in lower:
            return "refinery"
    if "slaveminer_deployed" in lower:
        return "refinery"
    return None

def scan_yaml_file(filepath):
    """Scan a YAML file for building actor definitions."""
    buildings = {}
    try:
        with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
            for line in f:
                name = is_building_line(line)
                if name:
                    category = classify_building(name)
                    if category:
                        buildings[category] = name
    except Exception as e:
        print(f"Error reading {filepath}: {e}")
    return buildings

def get_faction_from_path(filepath):
    """Extract faction name from file path."""
    parts = filepath.replace('\\', '/').split('/')
    # Find the ContentPacks-relative path
    for i, part in enumerate(parts):
        if part == 'ContentPacks' and i + 1 < len(parts):
            remaining = parts[i+1:]
            if len(remaining) >= 2:
                game = remaining[0]  # e.g., TiberianDawn
                faction = remaining[1]  # e.g., GDI
                return f"{game}/{faction}"
    return None

def main():
    results = {}

    for root, dirs, files in os.walk(CONTENT_PACKS):
        for filename in files:
            if filename == "buildings.yaml":
                filepath = os.path.join(root, filename)
                faction = get_faction_from_path(filepath)
                if faction:
                    buildings = scan_yaml_file(filepath)
                    if buildings:
                        results[faction] = buildings

    # Print results as Lua table
    print("-- FactionBuildings: maps MCV actor name to key buildings for base construction")
    print("-- Auto-generated by tools/extract_buildings.py")
    print("FactionBuildings = {")

    # Also load the wave definitions to map MCV names
    # We'll print by faction path
    for faction in sorted(results.keys()):
        buildings = results[faction]
        print(f"  -- {faction}")
        parts = buildings.get("power", "nil")
        barracks = buildings.get("barracks", "nil")
        warfactory = buildings.get("warfactory", "nil")
        refinery = buildings.get("refinery", "nil")
        print(f"  power = \"{parts}\", barracks = \"{barracks}\", warfactory = \"{warfactory}\", refinery = \"{refinery}\",")

    print("}")

    # Also print as JSON for reference
    print("\n--- JSON ---")
    print(json.dumps(results, indent=2))

if __name__ == "__main__":
    main()
