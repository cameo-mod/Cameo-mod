#!/usr/bin/env python3
"""Rename old-style mk.shp -> make.shp and bb.shp -> bib.shp with YAML reference updates."""
import os
import re

MODS_DIR = os.path.join(os.path.dirname(__file__), "..", "mods", "cameo")
BITS_DIR = os.path.join(MODS_DIR, "bits")

# Build rename map: old_basename -> new_basename (without extension)
renames = {}
for root, dirs, files in os.walk(BITS_DIR):
    for f in files:
        name, ext = os.path.splitext(f)
        if ext.lower() != '.shp':
            continue
        # mk -> make (but not mk2/mk3 which are variant markers)
        if name.endswith('mk') and not name.endswith('mk2') and not name.endswith('mk3'):
            new_name = name[:-2] + 'make'
            renames[name] = new_name
        # bb -> bib
        elif name.endswith('bb'):
            new_name = name[:-2] + 'bib'
            renames[name] = new_name

print(f"Found {len(renames)} files to rename")

# 1. Rename files on disk
renamed_files = []
for root, dirs, files in os.walk(BITS_DIR):
    for f in files:
        name, ext = os.path.splitext(f)
        if name in renames:
            old_path = os.path.join(root, f)
            new_path = os.path.join(root, renames[name] + ext)
            if os.path.exists(new_path):
                print(f"SKIP (target exists): {old_path}")
                continue
            os.rename(old_path, new_path)
            renamed_files.append((os.path.relpath(old_path, MODS_DIR), os.path.relpath(new_path, MODS_DIR)))

print(f"Renamed {len(renamed_files)} files on disk")

# 2. Update all YAML references
# We need to replace both the bare name and the name with extension
changed_files = []
for root, dirs, files in os.walk(MODS_DIR):
    for f in files:
        if not f.endswith(('.yaml', '.ftl')):
            continue
        filepath = os.path.join(root, f)
        with open(filepath, 'r', encoding='utf-8') as fh:
            content = fh.read()
        
        original = content
        for old_name, new_name in renames.items():
            # Replace bare name (e.g. in sequence keys or Filename without extension)
            # Be careful: only replace whole tokens, not substrings
            # Use word boundary - but filenames can have special chars
            # Safe approach: replace exact matches of the old filename (with or without .shp)
            content = content.replace(old_name + '.shp', new_name + '.shp')
            # Also replace bare references (sequence names, RenderVoxels Image, etc.)
            # But only if the old name is a complete token (surrounded by non-alphanumeric)
            content = re.sub(r'(?<![A-Za-z0-9_])' + re.escape(old_name) + r'(?![A-Za-z0-9_])', new_name, content)
        
        if content != original:
            with open(filepath, 'w', encoding='utf-8') as fh:
                fh.write(content)
            changed_files.append(os.path.relpath(filepath, MODS_DIR))

print(f"\nUpdated {len(changed_files)} YAML/FTL files")
for f in sorted(changed_files):
    print(f"  {f}")
