#!/usr/bin/env python3
"""Rename futu_ prefix to futuretech_ across all assets, YAML, and FTL files."""
import os
import re

MODS_DIR = os.path.join(os.path.dirname(__file__), "..", "mods", "cameo")

# 1. Rename files on disk: any file with 'futu_' in the name
renamed_files = []
for root, dirs, files in os.walk(MODS_DIR):
    for filename in files:
        if 'futu_' in filename:
            old_path = os.path.join(root, filename)
            new_filename = filename.replace('futu_', 'futuretech_')
            new_path = os.path.join(root, new_filename)
            if os.path.exists(new_path):
                print(f"SKIP (target exists): {old_path}")
                continue
            os.rename(old_path, new_path)
            renamed_files.append((os.path.relpath(old_path, MODS_DIR), os.path.relpath(new_path, MODS_DIR)))

print(f"Renamed {len(renamed_files)} files on disk")
for old, new in renamed_files:
    print(f"  {old} -> {new}")

# 2. Update all YAML and FTL references
# Safe because 'futu_' is NOT a substring of 'futuretech_'
# (futu_ = f-u-t-u-_, futuretech_ = f-u-t-u-r-e-t-e-c-h-_ — after 'futu' comes 'r' not '_')
changed_files = []
for root, dirs, files in os.walk(MODS_DIR):
    for filename in files:
        if not filename.endswith(('.yaml', '.ftl')):
            continue
        filepath = os.path.join(root, filename)
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        if 'futu_' not in content:
            continue
        original = content
        content = content.replace('futu_', 'futuretech_')
        if content != original:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
            changed_files.append(os.path.relpath(filepath, MODS_DIR))

print(f"\nUpdated {len(changed_files)} YAML/FTL files")
for f in sorted(changed_files):
    print(f"  {f}")
