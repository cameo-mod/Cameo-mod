#!/usr/bin/env python3
"""Check which old-style bb files are actually referenced in yaml files."""

from pathlib import Path
import re

ROOT = Path("C:/Users/AedisToru/Documents/GitHub/Cameo-mod")
MOD = ROOT / "mods/cameo"
BITS = MOD / "bits"

SKIP_EXTENSIONS = {".wav", ".aud", ".mp3", ".ogg"}

# Find old-style bb files (no underscore prefix pattern, or ra2_ prefix with compressed bb)
old_bb_files = []
for p in sorted(BITS.rglob("*")):
    if not p.is_file() or p.suffix.lower() in SKIP_EXTENSIONS:
        continue
    stem = p.stem
    if stem.endswith("bb") and not re.match(r"^[a-z]+_[a-z]+_.*_bib$", stem):
        # Exclude new-style files that already end with _bib
        if not stem.endswith("_bib"):
            old_bb_files.append(p)

print(f"Found {len(old_bb_files)} old-style bb files\n")

# Check references
referenced = []
unreferenced = []
for p in old_bb_files:
    stem = p.stem
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
