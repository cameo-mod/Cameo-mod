#!/usr/bin/env python3
"""
Audit actor descriptions across all factions.
Checks:
1. Every buildable actor has a Description (either inline or fluent key)
2. Every fluent key referenced in YAML exists in the corresponding .ftl file
3. Every actor has a Name (either inline or fluent key)
4. Lists actors with inline descriptions vs fluent references vs missing descriptions

Outputs a report per faction.
"""

import os
import re
import sys
import glob
from pathlib import Path
from collections import defaultdict

MOD_ROOT = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), "mods", "cameo")

def find_yaml_files(root):
    """Find all YAML files in the mod directory."""
    yaml_files = []
    for dirpath, dirnames, filenames in os.walk(root):
        # Skip .git
        if '.git' in dirpath:
            continue
        for f in filenames:
            if f.endswith('.yaml') or f.endswith('.yml'):
                yaml_files.append(os.path.join(dirpath, f))
    return yaml_files

def parse_yaml_actors(filepath):
    """Parse YAML file and extract actor definitions with their Tooltip Name and Buildable Description."""
    actors = []
    current_actor = None
    current_indent = 0
    in_buildable = False
    in_tooltip = False
    buildable_indent = 0
    tooltip_indent = 0

    with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
        for line_num, line in enumerate(f, 1):
            stripped = line.rstrip('\n\r')
            if not stripped.strip():
                continue
            if stripped.strip().startswith('#'):
                continue

            indent = len(stripped) - len(stripped.lstrip())

            # Top-level actor definition (no indent, ends with :)
            if indent == 0 and stripped.endswith(':') and not stripped.startswith('-') and not stripped.startswith(' '):
                if current_actor:
                    actors.append(current_actor)
                actor_name = stripped.rstrip(':').strip()
                current_actor = {
                    'name': actor_name,
                    'file': filepath,
                    'line': line_num,
                    'tooltip_name': None,
                    'tooltip_name_line': None,
                    'buildable_description': None,
                    'buildable_description_line': None,
                    'buildable_prerequisites': None,
                    'buildable_queue': None,
                    'has_buildable': False,
                    'has_tooltip': False,
                    'selectable': None,
                }
                in_buildable = False
                in_tooltip = False
                continue

            if current_actor is None:
                continue

            # Check for Buildable section
            if re.match(r'^\s+Buildable:', stripped) or re.match(r'^\s+Buildable@[^:]+:', stripped):
                in_buildable = True
                buildable_indent = indent
                current_actor['has_buildable'] = True
                continue

            # Check for Tooltip section
            if re.match(r'^\s+Tooltip:', stripped) or re.match(r'^\s+Tooltip@[^:]+:', stripped):
                in_tooltip = True
                tooltip_indent = indent
                current_actor['has_tooltip'] = True
                continue

            # Check if we're leaving Buildable or Tooltip section
            if in_buildable and indent <= buildable_indent and stripped.strip() and not stripped.strip().startswith('#'):
                in_buildable = False
            if in_tooltip and indent <= tooltip_indent and stripped.strip() and not stripped.strip().startswith('#'):
                in_tooltip = False

            # Extract Name from Tooltip
            if in_tooltip and re.match(r'^\s+Name:', stripped):
                name_val = stripped.split('Name:', 1)[1].strip()
                current_actor['tooltip_name'] = name_val
                current_actor['tooltip_name_line'] = line_num

            # Extract Description from Buildable
            if in_buildable and re.match(r'^\s+Description:', stripped):
                desc_val = stripped.split('Description:', 1)[1].strip()
                current_actor['buildable_description'] = desc_val
                current_actor['buildable_description_line'] = line_num

            # Extract Prerequisites from Buildable
            if in_buildable and re.match(r'^\s+Prerequisites:', stripped):
                prereq_val = stripped.split('Prerequisites:', 1)[1].strip()
                current_actor['buildable_prerequisites'] = prereq_val

            # Extract Queue from Buildable
            if in_buildable and re.match(r'^\s+Queue:', stripped):
                queue_val = stripped.split('Queue:', 1)[1].strip()
                current_actor['buildable_queue'] = queue_val

    if current_actor:
        actors.append(current_actor)

    return actors

def parse_ftl_keys(ftl_path):
    """Parse a .ftl file and extract all defined keys."""
    keys = set()
    if not os.path.exists(ftl_path):
        return keys

    with open(ftl_path, 'r', encoding='utf-8', errors='replace') as f:
        for line in f:
            line = line.strip()
            # Fluent keys look like: actor_xxx = or .name = or .description =
            # We want the top-level keys like: actor_xxx
            match = re.match(r'^([a-zA-Z0-9_]+)\s*=', line)
            if match:
                keys.add(match.group(1))
            # Also match sub-keys: .name, .description
            match2 = re.match(r'^\.(\w+)\s*=', line)
            if match2:
                # This is a sub-key, we'll track it separately
                pass

    return keys

def parse_ftl_all_keys(ftl_path):
    """Parse a .ftl file and extract all keys including sub-keys with their full path."""
    keys = {}  # key -> set of sub-keys
    current_key = None

    if not os.path.exists(ftl_path):
        return keys

    with open(ftl_path, 'r', encoding='utf-8', errors='replace') as f:
        for line in f:
            stripped = line.strip()
            if not stripped or stripped.startswith('#'):
                continue

            # Top-level key: actor_xxx =
            match = re.match(r'^([a-zA-Z0-9_]+)\s*=', stripped)
            if match:
                current_key = match.group(1)
                if current_key not in keys:
                    keys[current_key] = set()
                continue

            # Sub-key: .name = or .description =
            match2 = re.match(r'^\.(\w+)\s*=', stripped)
            if match2 and current_key:
                keys[current_key].add(match2.group(1))

    return keys

def is_fluent_ref(value):
    """Check if a value is a fluent reference (contains a dot and no spaces in the first part)."""
    if not value:
        return False
    # Fluent refs look like: actor_xxx.description or template_power.description
    # They don't contain spaces (except maybe after the ref)
    # But inline descriptions have spaces
    if value.startswith('"') or value.startswith("'"):
        return False
    # Check if it looks like a fluent key reference
    parts = value.split('.')
    if len(parts) >= 2 and ' ' not in parts[0]:
        return True
    return False

def get_faction_from_path(filepath, mod_root):
    """Extract faction name from file path."""
    rel = os.path.relpath(filepath, mod_root)
    parts = rel.replace('\\', '/').split('/')

    # ContentPacks/<Game>/<Faction>/yaml/...
    if len(parts) >= 4 and parts[0] == 'ContentPacks':
        return parts[2]  # Faction name

    # rules/<game>.yaml
    if len(parts) >= 2 and parts[0] == 'rules':
        return parts[1].replace('.yaml', '')

    return 'unknown'

LOADED_FACTIONS = [
    'GDI', 'Nod',  # TD
    'Allies', 'Soviets', 'Japan',  # RA1
    'GDI', 'Nod', 'Forgotten', 'CABAL',  # TS - same names as TD, need to use path
    'Allies', 'Soviets', 'Yuri',  # RA2
    'AsianAlliance', 'Consortium', 'Syndicate', 'Naxis', 'SchwarzerMond', 'FutureTech',  # RA2Mod
    'TKM',  # TKM
    'Ordos', 'Ixian',  # D2k (Atreides/Harkonnen are commented out)
    'StarCraft',  # StarCraft
    'Warcraft2',  # Warcraft2
    'Outpost2',  # Outpost2
    'Shared', 'Core',  # Shared content
]

LOADED_PATH_PREFIXES = [
    'ContentPacks/TiberianDawn',
    'ContentPacks/RedAlert',
    'ContentPacks/TiberianSun',
    'ContentPacks/RedAlert2',
    'ContentPacks/RedAlert2Mod',
    'ContentPacks/TKM',
    'ContentPacks/D2k',
    'ContentPacks/StarCraft',
    'ContentPacks/Warcraft2',
    'ContentPacks/Outpost2',
    'ContentPacks/Core',
]

# Rules files loaded from mod.yaml Rules: section
LOADED_RULES_FILES = [
    'rules/misc.yaml', 'rules/player.yaml', 'rules/world.yaml',
    'rules/map_generators.yaml', 'rules/music.yaml', 'rules/palettes.yaml',
    'rules/defaults.yaml', 'rules/shared.yaml', 'rules/trees.yaml',
    'rules/civilian.yaml', 'rules/civilian_desert.yaml', 'rules/tech.yaml',
    'rules/husks.yaml', 'rules/promotions.yaml', 'ai/ai.yaml',
    'rules/starcraft.yaml', 'rules/warcraft2.yaml', 'rules/tkm.yaml',
    'rules/redalert.yaml', 'rules/tiberiansun.yaml', 'rules/redalert2.yaml',
    'rules/redalert2mod.yaml', 'rules/d2k.yaml', 'rules/outpost2.yaml',
]

# Also include rules files that are loaded via ContentPacks includes
# These are loaded through content.yaml includes
LOADED_RULES_VIA_PACKS = [
    'rules/starcraft.yaml',
    'rules/warcraft2.yaml',
    'rules/tkm.yaml',
]

def is_loaded_faction(filepath, mod_root):
    """Check if the file belongs to a loaded faction."""
    rel = os.path.relpath(filepath, mod_root).replace('\\', '/')
    for prefix in LOADED_PATH_PREFIXES:
        if rel.startswith(prefix):
            return True
    # Check specific rules files
    for rf in LOADED_RULES_FILES:
        if rel == rf:
            return True
    return False

def main():
    yaml_files = find_yaml_files(MOD_ROOT)
    # Filter to only loaded factions
    yaml_files = [f for f in yaml_files if is_loaded_faction(f, MOD_ROOT)]

    all_actors = []
    for yf in yaml_files:
        actors = parse_yaml_actors(yf)
        all_actors.extend(actors)

    # Filter to only buildable actors
    buildable_actors = [a for a in all_actors if a['has_buildable']]

    # Group by faction
    by_faction = defaultdict(list)
    for a in buildable_actors:
        faction = get_faction_from_path(a['file'], MOD_ROOT)
        by_faction[faction].append(a)

    # Find all fluent files
    ftl_files = {}
    for dirpath, dirnames, filenames in os.walk(MOD_ROOT):
        for f in filenames:
            if f == 'en.ftl':
                ftl_files[dirpath] = os.path.join(dirpath, f)

    # Parse all fluent files and merge their keys
    all_ftl_keys = {}
    for dirpath, ftl_path in ftl_files.items():
        keys = parse_ftl_all_keys(ftl_path)
        all_ftl_keys.update(keys)

    # Also check fluent/rules/*.ftl files
    fluent_rules_dir = os.path.join(MOD_ROOT, 'fluent', 'rules')
    if os.path.exists(fluent_rules_dir):
        for f in os.listdir(fluent_rules_dir):
            if f.endswith('.ftl'):
                keys = parse_ftl_all_keys(os.path.join(fluent_rules_dir, f))
                all_ftl_keys.update(keys)

    # Generate report
    print("=" * 80)
    print("ACTOR DESCRIPTION AUDIT REPORT")
    print("=" * 80)
    print(f"Total YAML files scanned: {len(yaml_files)}")
    print(f"Total actors found: {len(all_actors)}")
    print(f"Buildable actors: {len(buildable_actors)}")
    print(f"Fluent keys loaded: {len(all_ftl_keys)}")
    print()

    total_missing = 0
    total_fluent = 0
    total_inline = 0
    total_no_desc = 0

    for faction in sorted(by_faction.keys()):
        actors = by_faction[faction]
        actors.sort(key=lambda a: a['name'])

        missing_desc = []
        fluent_refs = []
        inline_descs = []
        missing_name = []
        fluent_name_refs = []
        inline_names = []
        broken_fluent = []

        for a in actors:
            # Check description
            desc = a['buildable_description']
            if desc is None:
                missing_desc.append(a)
                total_no_desc += 1
            elif is_fluent_ref(desc):
                fluent_refs.append(a)
                total_fluent += 1
                # Check if the fluent key exists
                key_parts = desc.split('.')
                key = key_parts[0]
                subkey = key_parts[1] if len(key_parts) > 1 else None
                if key not in all_ftl_keys:
                    broken_fluent.append((a, f"Key '{key}' not found in any .ftl file"))
                elif subkey and subkey not in all_ftl_keys[key]:
                    broken_fluent.append((a, f"Key '{key}.{subkey}' not found"))
            else:
                inline_descs.append(a)
                total_inline += 1

            # Check name
            name = a['tooltip_name']
            if name is None:
                missing_name.append(a)
            elif is_fluent_ref(name):
                fluent_name_refs.append(a)
            else:
                inline_names.append(a)

        print(f"\n{'=' * 60}")
        print(f"FACTION: {faction}")
        print(f"  Buildable actors: {len(actors)}")
        print(f"  Descriptions: {len(fluent_refs)} fluent, {len(inline_descs)} inline, {len(missing_desc)} MISSING")
        print(f"  Names: {len(fluent_name_refs)} fluent, {len(inline_names)} inline, {len(missing_name)} missing")

        if missing_desc:
            total_missing += len(missing_desc)
            print(f"\n  --- MISSING DESCRIPTIONS ({len(missing_desc)}) ---")
            for a in missing_desc:
                print(f"    {a['name']}  ({os.path.relpath(a['file'], MOD_ROOT)}:{a['line']})")

        if broken_fluent:
            print(f"\n  --- BROKEN FLUENT REFERENCES ({len(broken_fluent)}) ---")
            for a, msg in broken_fluent:
                print(f"    {a['name']}: {msg}  (desc='{a['buildable_description']}')")

        if inline_descs:
            print(f"\n  --- INLINE DESCRIPTIONS ({len(inline_descs)}) ---")
            for a in inline_descs:
                desc_preview = a['buildable_description'][:80]
                print(f"    {a['name']}: {desc_preview}")

    print(f"\n{'=' * 80}")
    print(f"SUMMARY")
    print(f"  Total buildable actors: {len(buildable_actors)}")
    print(f"  Fluent descriptions: {total_fluent}")
    print(f"  Inline descriptions: {total_inline}")
    print(f"  Missing descriptions: {total_no_desc}")
    print(f"  Total missing: {total_missing}")
    print(f"{'=' * 80}")

if __name__ == '__main__':
    main()
