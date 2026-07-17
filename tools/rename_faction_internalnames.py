#!/usr/bin/env python3
"""
Renames faction InternalNames to match actor prefixes, and renames WC2 actors.

Faction InternalName changes (actors already use these prefixes):
  gdi           -> td_gdi
  nod           -> td_nod
  allies        -> ra1_allies
  soviets       -> ra1_soviets
  ra2allies     -> ra2_allies
  ra2soviets    -> ra2_soviets
  tsgdi         -> ts_gdi
  tsnod         -> ts_nod
  asianalliance -> asian_alliance
  consortium    -> steelconsortium
  syndicate     -> latinsyndicate

WC2 faction + actor rename:
  warcraft_humans -> wc2_humans  (all warcraft_humans_ -> wc2_humans_ in actors)
  warcraft_orcs   -> wc2_orcs    (all warcraft_orcs_ -> wc2_orcs_ in actors)

Already consistent (no change needed):
  schwarzermond, naxis, futuretech, japan, yuri, forgotten, cabal, terran, protoss, zerg, tkm, etc.

Usage:
  python tools/rename_faction_internalnames.py --dry-run   # preview
  python tools/rename_faction_internalnames.py             # apply
"""

import os
import re
import sys

TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.join(TOOLS_DIR, "..")

DRY_RUN = "--dry-run" in sys.argv

# ── Faction InternalName renames ──────────────────────────────────────────────
FACTION_RENAMES = [
    ("asianalliance", "asian_alliance"),
    ("ra2soviets", "ra2_soviets"),
    ("ra2allies", "ra2_allies"),
    ("tsgdi", "ts_gdi"),
    ("tsnod", "ts_nod"),
    ("warcraft_humans", "wc2_humans"),
    ("warcraft_orcs", "wc2_orcs"),
    ("soviets", "ra1_soviets"),
    ("allies", "ra1_allies"),
    ("gdi", "td_gdi"),
    ("nod", "td_nod"),
    ("consortium", "steelconsortium"),
    ("syndicate", "latinsyndicate"),
]

# WC2 actor prefix renames — global replacement in YAML content
WC2_ACTOR_RENAMES = [
    ("warcraft_humans_", "wc2_humans_"),
    ("warcraft_orcs_", "wc2_orcs_"),
]

def get_files(root, extensions):
    result = []
    for dirpath, dirnames, filenames in os.walk(root):
        if ".git" in dirpath:
            continue
        for f in filenames:
            for ext in extensions:
                if f.endswith(ext):
                    result.append(os.path.join(dirpath, f))
                    break
    return result

def replace_in_yaml_line(line, old_name, new_name):
    """Replace faction name in a YAML line, but only in specific contexts."""
    # InternalName: <name>
    m = re.match(r'^(\s*InternalName:\s*)(.+)$', line)
    if m:
        prefix, value = m.group(1), m.group(2).strip()
        if value == old_name:
            return prefix + new_name + '\n'

    # FactionCA@<name>:
    m = re.match(r'^(\s*FactionCA@)([^\s:]+)(:.*)$', line)
    if m:
        prefix, label, suffix = m.group(1), m.group(2), m.group(3)
        if label == old_name:
            return prefix + new_name + suffix + '\n'

    # Factions: <name> or <name>, <name2>, ...
    m = re.match(r'^(\s*Factions:\s*)(.+)$', line)
    if m:
        prefix, value = m.group(1), m.group(2)
        new_value = re.sub(r'(?<![a-zA-Z0-9_])' + re.escape(old_name) + r'(?![a-zA-Z0-9_])',
                          new_name, value)
        if new_value != value:
            return prefix + new_value.rstrip() + '\n'

    # RandomFactionMembers: <name>, <name2>, ...
    m = re.match(r'^(\s*RandomFactionMembers:\s*)(.+)$', line)
    if m:
        prefix, value = m.group(1), m.group(2)
        new_value = re.sub(r'(?<![a-zA-Z0-9_])' + re.escape(old_name) + r'(?![a-zA-Z0-9_])',
                          new_name, value)
        if new_value != value:
            return prefix + new_value.rstrip() + '\n'

    return line

def replace_in_python(content, old_name, new_name):
    """Replace faction name in Python string literals only."""
    pattern = r'(["\'])' + re.escape(old_name) + r'\1'
    return re.sub(pattern, r'\g<1>' + new_name + r'\g<1>', content)

def replace_in_md(content, old_name, new_name):
    """Replace faction name in markdown in specific contexts."""
    # Replace in backtick-quoted code: `old_name`
    pattern = r'(`)' + re.escape(old_name) + r'(`)'
    content = re.sub(pattern, r'\g<1>' + new_name + r'\g<1>', content)

    # Replace in fenced code blocks
    in_code_block = False
    lines = content.split('\n')
    new_lines = []
    for line in lines:
        if line.strip().startswith('```'):
            in_code_block = not in_code_block
            new_lines.append(line)
            continue
        if in_code_block:
            new_line = re.sub(r'(?<![a-zA-Z0-9_])' + re.escape(old_name) + r'(?![a-zA-Z0-9_])',
                             new_name, line)
            new_lines.append(new_line)
        else:
            new_lines.append(line)
    content = '\n'.join(new_lines)

    # Replace in specific patterns
    for pat in [r'(Faction:\s*)', r'(faction:\s*)', r'(InternalName:\s*)',
                r'(FactionCA@)']:
        pattern = pat + re.escape(old_name) + r'(?![a-zA-Z0-9_])'
        content = re.sub(pattern, r'\g<1>' + new_name, content)

    return content

def process_yaml_file(filepath, dry_run=False):
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            lines = f.readlines()
    except (UnicodeDecodeError, PermissionError):
        return False, []

    original_lines = lines[:]
    changes = []

    # Phase 1: Faction InternalName renames (targeted, line-by-line)
    for i, line in enumerate(lines):
        new_line = line
        for old_name, new_name in FACTION_RENAMES:
            new_line = replace_in_yaml_line(new_line, old_name, new_name)
        if new_line != line:
            lines[i] = new_line

    # Phase 2: WC2 actor prefix renames (global, on entire content)
    content = ''.join(lines)
    original_content = content
    for old_prefix, new_prefix in WC2_ACTOR_RENAMES:
        if old_prefix in content:
            count = content.count(old_prefix)
            content = content.replace(old_prefix, new_prefix)
            changes.append(f"  actor prefix: {old_prefix} -> {new_prefix} ({count} occurrences)")

    if content != original_content:
        lines = content.splitlines(keepends=True)

    changed = lines != original_lines
    if changed and not dry_run:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.writelines(lines)

    # Collect faction change descriptions
    faction_changes = set()
    for i in range(min(len(original_lines), len(lines))):
        if lines[i] != original_lines[i]:
            for old_name, new_name in FACTION_RENAMES:
                if old_name in original_lines[i] and new_name in lines[i]:
                    faction_changes.add(f"  faction: {old_name} -> {new_name}")

    all_changes = sorted(faction_changes) + [c for c in changes if "actor prefix" in c]
    return changed, all_changes

def process_python_file(filepath, dry_run=False):
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
    except (UnicodeDecodeError, PermissionError):
        return False, []

    original = content
    changes = []

    for old_name, new_name in FACTION_RENAMES:
        new_content = replace_in_python(content, old_name, new_name)
        if new_content != content:
            count = len(re.findall(r'(["\'])' + re.escape(old_name) + r'\1', content))
            changes.append(f"  faction: {old_name} -> {new_name} ({count} occurrences)")
            content = new_content

    # Also apply WC2 actor prefix renames in Python files
    for old_prefix, new_prefix in WC2_ACTOR_RENAMES:
        if old_prefix in content:
            count = content.count(old_prefix)
            content = content.replace(old_prefix, new_prefix)
            changes.append(f"  actor prefix: {old_prefix} -> {new_prefix} ({count} occurrences)")

    if content != original:
        if not dry_run:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
        return True, changes
    return False, []

def process_md_file(filepath, dry_run=False):
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
    except (UnicodeDecodeError, PermissionError):
        return False, []

    original = content
    changes = []

    for old_name, new_name in FACTION_RENAMES:
        new_content = replace_in_md(content, old_name, new_name)
        if new_content != content:
            changes.append(f"  faction: {old_name} -> {new_name}")
            content = new_content

    # Also apply WC2 actor prefix renames in MD files
    for old_prefix, new_prefix in WC2_ACTOR_RENAMES:
        if old_prefix in content:
            count = content.count(old_prefix)
            content = content.replace(old_prefix, new_prefix)
            if count > 0:
                changes.append(f"  actor prefix: {old_prefix} -> {new_prefix} ({count} occurrences)")

    if content != original:
        if not dry_run:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
        return True, changes
    return False, []

def process_shell_file(filepath, dry_run=False):
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
    except (UnicodeDecodeError, PermissionError):
        return False, []

    original = content
    changes = []

    for old_name, new_name in FACTION_RENAMES:
        pattern = r'(["\'])' + re.escape(old_name) + r'\1'
        new_content = re.sub(pattern, r'\g<1>' + new_name + r'\g<1>', content)
        if new_content != content:
            count = len(re.findall(pattern, content))
            changes.append(f"  faction: {old_name} -> {new_name} ({count} occurrences)")
            content = new_content

    if content != original:
        if not dry_run:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
        return True, changes
    return False, []

def main():
    print(f"Mode: {'DRY RUN' if DRY_RUN else 'APPLY'}")
    print(f"Repository: {REPO_ROOT}")
    print()

    total_changed = 0

    for root_subdir in ["mods", "tools"]:
        root = os.path.join(REPO_ROOT, root_subdir)
        if not os.path.exists(root):
            continue
        yaml_files = get_files(root, [".yaml", ".yml"])
        for filepath in sorted(yaml_files):
            changed, changes = process_yaml_file(filepath, DRY_RUN)
            if changed:
                rel = os.path.relpath(filepath, REPO_ROOT)
                print(f"{'[DRY] ' if DRY_RUN else ''}{rel}")
                for c in changes:
                    print(c)
                total_changed += 1

    py_root = os.path.join(REPO_ROOT, "tools")
    if os.path.exists(py_root):
        py_files = get_files(py_root, [".py"])
        for filepath in sorted(py_files):
            if os.path.basename(filepath) == "rename_faction_internalnames.py":
                continue
            changed, changes = process_python_file(filepath, DRY_RUN)
            if changed:
                rel = os.path.relpath(filepath, REPO_ROOT)
                print(f"{'[DRY] ' if DRY_RUN else ''}{rel}")
                for c in changes:
                    print(c)
                total_changed += 1

    docs_root = os.path.join(REPO_ROOT, "docs")
    if os.path.exists(docs_root):
        md_files = get_files(docs_root, [".md"])
        for filepath in sorted(md_files):
            changed, changes = process_md_file(filepath, DRY_RUN)
            if changed:
                rel = os.path.relpath(filepath, REPO_ROOT)
                print(f"{'[DRY] ' if DRY_RUN else ''}{rel}")
                for c in changes:
                    print(c)
                total_changed += 1

    sh_root = os.path.join(REPO_ROOT, "tools")
    if os.path.exists(sh_root):
        sh_files = get_files(sh_root, [".sh"])
        for filepath in sorted(sh_files):
            changed, changes = process_shell_file(filepath, DRY_RUN)
            if changed:
                rel = os.path.relpath(filepath, REPO_ROOT)
                print(f"{'[DRY] ' if DRY_RUN else ''}{rel}")
                for c in changes:
                    print(c)
                total_changed += 1

    print()
    print(f"Total files {'that would be ' if DRY_RUN else ''}changed: {total_changed}")

if __name__ == "__main__":
    main()
