#!/usr/bin/env python3
"""Rename old-style mk->make for referenced files and update references."""

from pathlib import Path
import subprocess

ROOT = Path("C:/Users/AedisToru/Documents/GitHub/Cameo-mod")
MOD = ROOT / "mods/cameo"
BITS = MOD / "bits"

# Only referenced files (excluding tsdlimpmk which is not a make suffix,
# and tsgtartymk which is only in comments)
RENAMES = [
    # (bits_subdir, old_name, new_name, ref_files_relative_to_mod)
    ("ts", "tsnttmplmk.shp", "tsnttmplmake.shp", ["sequences/tiberiansun.yaml"]),
    ("ra2", "ra2_ntyardmk.shp", "ra2_ntyardmake.shp", ["sequences/redalert2.yaml"]),
    ("ra2", "ra2_cgoildmk.shp", "ra2_cgoildmake.shp", ["sequences/redalert2.yaml"]),
    ("ts", "tampowrmk.shp", "tampowrmake.shp", ["ContentPacks/TiberianSun/Forgotten/yaml/sequences.yaml"]),
    ("ts", "tambarmk.shp", "tambarmake.shp", ["ContentPacks/TiberianSun/Forgotten/yaml/sequences.yaml"]),
    ("ts", "tamradrmk.shp", "tamradrmake.shp", ["ContentPacks/TiberianSun/Forgotten/yaml/sequences.yaml"]),
    ("ts", "tamtechmk.shp", "tamtechmake.shp", ["ContentPacks/TiberianSun/Forgotten/yaml/sequences.yaml"]),
    ("ts", "tamrefmk.shp", "tamrefmake.shp", ["ContentPacks/TiberianSun/Forgotten/yaml/sequences.yaml"]),
]

def git(*args):
    result = subprocess.run(["git", "-C", str(ROOT)] + list(args),
                          capture_output=True, text=True)
    if result.returncode != 0:
        print(f"  GIT ERROR: {result.stderr.strip()}")
        return False
    return True

for subdir, old_name, new_name, ref_files in RENAMES:
    old_path = BITS / subdir / old_name
    new_path = BITS / subdir / new_name
    
    if not old_path.exists():
        print(f"SKIP (not found): {old_path.relative_to(MOD)}")
        continue
    
    # git mv the file
    src_rel = old_path.relative_to(ROOT).as_posix()
    dst_rel = new_path.relative_to(ROOT).as_posix()
    if git("mv", src_rel, dst_rel):
        print(f"RENAMED {src_rel} -> {dst_rel}")
    
    # Update references
    old_stem = old_name.rsplit(".", 1)[0]
    new_stem = new_name.rsplit(".", 1)[0]
    old_full = old_name
    new_full = new_name
    
    for ref_file in ref_files:
        ref_path = MOD / ref_file
        text = ref_path.read_text(encoding="utf-8")
        count = text.count(old_full)
        if count > 0:
            text = text.replace(old_full, new_full)
            ref_path.write_text(encoding="utf-8", data=text)
            print(f"  UPDATED {ref_file}: {count} refs")

print("\nDone.")
