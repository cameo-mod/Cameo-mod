#!/usr/bin/env python3
"""Fix stale actor references in ai.yaml using all rename maps."""
import os
import re

TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
RENAME_DIR = os.path.join(TOOLS_DIR, "rename")
AI_FILE = os.path.join(TOOLS_DIR, "..", "mods", "cameo", "ai", "ai.yaml")

def load_all_rename_maps():
    """Load all rename_map_*.yaml files and build old_id -> new_id mapping.
    Rename maps use tabs for indentation, so we parse manually."""
    mapping = {}
    for filename in os.listdir(RENAME_DIR):
        if not filename.startswith("rename_map_") or not filename.endswith(".yaml"):
            continue
        filepath = os.path.join(RENAME_DIR, filename)
        with open(filepath, 'r', encoding='utf-8') as f:
            lines = f.readlines()

        in_actors = False
        for line in lines:
            stripped = line.strip()
            if stripped == 'actors:':
                in_actors = True
                continue
            if stripped == 'files:':
                in_actors = False
                continue
            if not in_actors or not stripped or stripped.startswith('#'):
                continue
            # Parse "old_id: new_id" (only single-line mappings)
            if ':' in stripped:
                parts = stripped.split(':', 1)
                old_id = parts[0].strip()
                new_id = parts[1].strip()
                if old_id and new_id and old_id != new_id:
                    mapping[old_id] = new_id
    return mapping

def fix_ai_file(mapping):
    """Apply mapping to ai.yaml, replacing stale actor IDs with current names."""
    with open(AI_FILE, 'r', encoding='utf-8') as f:
        content = f.read()

    # Sort by length descending so longer IDs are replaced first (avoid partial matches)
    sorted_mapping = sorted(mapping.items(), key=lambda x: len(x[0]), reverse=True)

    replacements = []

    # We need to be careful to only replace whole words (actor IDs)
    # Actor IDs in ai.yaml appear as list items, typically indented with tabs
    # They can appear in various lists: BuildingLimits, BuildingFractions, UnitsToBuild, etc.
    # Format is usually: \t\told_id: value  or  \t\told_id\n

    for old_id, new_id in sorted_mapping:
        # Match the old_id as a whole word in YAML list contexts
        # Pattern: tab(s) + old_id + (colon or newline or end)
        pattern = re.compile(r'(\t+)' + re.escape(old_id) + r'(?=[\s:\n]|$)', re.MULTILINE)
        matches = pattern.findall(content)
        if matches:
            count = len(matches)
            content = pattern.sub(r'\1' + new_id, content)
            replacements.append((old_id, new_id, count))

    with open(AI_FILE, 'w', encoding='utf-8') as f:
        f.write(content)

    return replacements

if __name__ == '__main__':
    mapping = load_all_rename_maps()
    print(f"Loaded {len(mapping)} actor rename mappings from {len(os.listdir(RENAME_DIR))} files")

    replacements = fix_ai_file(mapping)

    total = sum(r[2] for r in replacements)
    print(f"\nApplied {total} replacements across {len(replacements)} unique IDs:")
    for old, new, count in sorted(replacements, key=lambda x: -x[2]):
        print(f"  {old} -> {new}: {count} occurrences")
