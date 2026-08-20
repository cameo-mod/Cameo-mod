#!/usr/bin/env python3
"""Audit all CreateEffect warheads for explicit Image: fields.
Per DESIGN.md §8, CreateEffect must NOT carry an Image: field —
the engine defaults to the 'explosion' image in misc.yaml.
"""
import os, re, sys

root = 'mods/cameo'
findings = []

for dirpath, dirnames, filenames in os.walk(root):
    for fname in filenames:
        if not fname.endswith(('.yaml', '.yml')):
            continue
        fpath = os.path.join(dirpath, fname)
        try:
            with open(fpath, 'r', encoding='utf-8') as f:
                lines = f.readlines()
        except:
            continue

        # Track when we're inside a CreateEffect warhead block
        in_createeffect = False
        createeffect_indent = 0
        createeffect_line = 0
        weapon_name = ""

        for i, line in enumerate(lines):
            # Detect weapon/actor definition at column 0
            if re.match(r'^[A-Za-z_^]', line):
                m = re.match(r'^([A-Za-z_][A-Za-z0-9_\-\.]*):', line)
                if m:
                    weapon_name = m.group(1)
                in_createeffect = False

            # Detect CreateEffect warhead
            if re.search(r'CreateEffect\s*$', line.rstrip()) or re.search(r'CreateEffect:', line.rstrip()):
                in_createeffect = True
                createeffect_indent = len(line) - len(line.lstrip())
                createeffect_line = i + 1
                continue

            # If inside a CreateEffect block, look for Image:
            if in_createeffect:
                # Check if we've left the block (dedent to same or less indent)
                stripped = line.strip()
                if stripped and not stripped.startswith('#'):
                    line_indent = len(line) - len(line.lstrip())
                    if line_indent <= createeffect_indent and not line.strip().startswith('-'):
                        in_createeffect = False
                        continue

                # Check for Image: field (but NOT TrailImage or other fields)
                m = re.match(r'^\s+Image:\s*(\S+)', line)
                if m and not line.strip().startswith('#'):
                    image_val = m.group(1)
                    # Skip if it's 'explosion' (redundant but harmless)
                    severity = "REDUNDANT" if image_val == "explosion" else "VIOLATION"
                    findings.append((severity, fpath, i + 1, weapon_name, image_val, line.rstrip()))

if not findings:
    print("No CreateEffect Image: fields found — all clean!")
else:
    violations = [f for f in findings if f[0] == "VIOLATION"]
    redundant = [f for f in findings if f[0] == "REDUNDANT"]
    print(f"=== {len(violations)} VIOLATIONS + {len(redundant)} REDUNDANT (Image: explosion) ===\n")
    for sev, fpath, lineno, weapon, img, line in findings:
        rel = os.path.relpath(fpath)
        print(f"[{sev}] {rel}:{lineno}  {weapon}  Image: {img}")
