#!/usr/bin/env python3
"""Extract all upgrade actor names per faction from ContentPacks YAML files."""
import os
import re
import json

CONTENT_PACKS = os.path.join(os.path.dirname(__file__), "..", "mods", "cameo", "ContentPacks")

def get_faction_from_path(filepath):
    """Extract faction name from file path."""
    parts = filepath.replace('\\', '/').split('/')
    for i, part in enumerate(parts):
        if part == 'ContentPacks' and i + 1 < len(parts):
            remaining = parts[i+1:]
            if len(remaining) >= 2:
                game = remaining[0]
                faction = remaining[1]
                return f"{game}/{faction}"
    return None

def scan_upgrades_file(filepath):
    """Scan a YAML file for upgrade actor definitions."""
    upgrades = []
    try:
        with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
            for line in f:
                # Match top-level keys that contain "upgrade" in the name
                m = re.match(r'^([a-z0-9_]+_upgrade_[a-z0-9_]+):\s*$', line)
                if m:
                    name = m.group(1)
                    if name not in upgrades:
                        upgrades.append(name)
    except Exception as e:
        print(f"Error reading {filepath}: {e}")
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
                    if upgrades:
                        results[faction] = upgrades
    
    # Print as JSON
    print(json.dumps(results, indent=2))

if __name__ == "__main__":
    main()
