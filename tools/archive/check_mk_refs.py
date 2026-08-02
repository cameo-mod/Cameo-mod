#!/usr/bin/env python3
"""Check which old-style mk files are actually referenced in yaml files."""

from pathlib import Path
import re

ROOT = Path("C:/Users/AedisToru/Documents/GitHub/Cameo-mod")
MOD = ROOT / "mods/cameo"
BITS = MOD / "bits"

SKIP_EXTENSIONS = {".wav", ".aud", ".mp3", ".ogg"}

# Find old-style mk files (no underscore prefix pattern)
old_mk_files = []
for p in sorted(BITS.rglob("*")):
    if not p.is_file() or p.suffix.lower() in SKIP_EXTENSIONS:
        continue
    stem = p.stem
    if stem.endswith("mk") and not re.match(r"^[a-z]+_[a-z]+", stem):
        if not any(stem.endswith(s) for s in ["mkii", "mk2", "mk3", "mk1"]):
            old_mk_files.append(p)

print(f"Found {len(old_mk_files)} old-style mk files\n")

# Check references
referenced = []
unreferenced = []
for p in old_mk_files:
    stem = p.stem
    # Search for the stem in all yaml files
    found = False
    for ref_p in MOD.rglob("*.yaml"):
        try:
            text = ref_p.read_text(encoding="utf-8")
        except Exception:
            continue
        if stem in text:
            found = True
            referenced.append((p, ref_p.relative_to(MOD)))
            break
    if not found:
        unreferenced.append(p)

print(f"Referenced: {len(referenced)}")
for p, ref in referenced:
    print(f"  {p.name} <- {ref}")

print(f"\nUnreferenced: {len(unreferenced)}")
for p in unreferenced:
    print(f"  {p.relative_to(BITS)}")
