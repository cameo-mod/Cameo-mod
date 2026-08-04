#!/usr/bin/env python3
"""Remove stale actor references from ai.yaml BuildingLimits, BuildingFractions,
and UnitsToBuild lists. Only removes IDs that are truly undefined (not defined
as top-level actors in any rules YAML file)."""
import os
import re

TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.join(TOOLS_DIR, "..")
AI_FILE = os.path.join(REPO_ROOT, "mods", "cameo", "ai", "ai.yaml")
RULES_DIR = os.path.join(REPO_ROOT, "mods", "cameo")

# Lists in ai.yaml that reference actor IDs
TARGET_LISTS = {'BuildingLimits', 'BuildingFractions', 'UnitsToBuild',
                'CapturingActorTypes', 'UnitLimits'}

def collect_defined_actors():
    """Collect all top-level actor IDs defined in rules YAML files.
    Only scans rules/ and ContentPacks/ directories (which contain actor
    definitions), excluding sequences/, audio/, maps/, etc."""
    defined = set()
    scan_dirs = [
        os.path.join(RULES_DIR, "rules"),
        os.path.join(RULES_DIR, "ContentPacks"),
    ]
    for scan_dir in scan_dirs:
        if not os.path.isdir(scan_dir):
            continue
        for root, dirs, files in os.walk(scan_dir):
            for f in files:
                if not f.endswith('.yaml'):
                    continue
                filepath = os.path.join(root, f)
                try:
                    with open(filepath, 'r', encoding='utf-8') as fh:
                        for line in fh:
                            m = re.match(r'^(\w[\w.]*)\s*:', line)
                            if m:
                                defined.add(m.group(1))
                except Exception:
                    pass
    return defined

def remove_stale_from_ai(defined_actors):
    """Remove lines from target lists in ai.yaml where the actor ID is undefined."""
    with open(AI_FILE, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    current_list = None
    list_indent = 0
    lines_to_remove = set()
    removed_by_id = {}

    for i, line in enumerate(lines):
        # Check if this line starts a new list section
        stripped = line.lstrip('\t')
        indent = len(line) - len(stripped)

        # Detect list headers (e.g. "\t\tBuildingLimits:")
        m = re.match(r'^(\t+)(\w+)\s*:\s*$', line)
        if m:
            list_name = m.group(2)
            if list_name in TARGET_LISTS:
                current_list = list_name
                list_indent = len(m.group(1))
            else:
                current_list = None
            continue

        # If we're inside a target list, check entries
        if current_list is None:
            continue

        # Entries in target lists are indented one level deeper than the list header
        # Format: \t\t\t<id>: <value>  or  \t\t\t<id>
        entry_match = re.match(r'^(\t{' + str(list_indent + 1) + r',})(\w[\w.]*)\s*[:\n]', line)
        if not entry_match:
            # If we hit a line at the same or lesser indent, we've left the list
            if indent <= list_indent and stripped and not stripped.startswith('#'):
                current_list = None
            continue

        actor_id = entry_match.group(2)

        # Skip comments
        if actor_id.startswith('#'):
            continue

        if actor_id not in defined_actors:
            lines_to_remove.add(i)
            removed_by_id.setdefault(actor_id, []).append(i + 1)

    # Remove stale lines
    new_lines = []
    for i, line in enumerate(lines):
        if i not in lines_to_remove:
            new_lines.append(line)

    with open(AI_FILE, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)

    return len(lines_to_remove), removed_by_id

if __name__ == '__main__':
    print("Collecting all defined actors from rules...")
    defined = collect_defined_actors()
    print(f"Found {len(defined)} defined actors")

    print("\nRemoving stale references from target lists...")
    removed, by_id = remove_stale_from_ai(defined)

    print(f"\nRemoved {removed} lines across {len(by_id)} unique stale IDs:")
    for aid in sorted(by_id.keys()):
        print(f"  {aid} (lines: {', '.join(str(l) for l in by_id[aid])})")
