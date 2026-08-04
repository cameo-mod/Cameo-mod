#!/usr/bin/env python3
"""Fix faction names in .oramap files (zip archives containing map.yaml)."""
import sys
import zipfile
import shutil
import os
import re
import tempfile

def fix_oramap(path):
    # Read the zip
    tmpfile = tempfile.mktemp(suffix='.oramap')
    modified = False
    
    with zipfile.ZipFile(path, 'r') as zin:
        names = zin.namelist()
        
        with zipfile.ZipFile(tmpfile, 'w', zipfile.ZIP_DEFLATED) as zout:
            for name in names:
                data = zin.read(name)
                if name.endswith('map.yaml') or name.endswith('rules.yaml'):
                    content = data.decode('utf-8')
                    original = content
                    
                    # Faction InternalName renames (old -> new)
                    renames = [
                        ("warcraft_humans", "wc2_humans"),
                        ("warcraft_orcs", "wc2_orcs"),
                        ("asian_alliance", "asianalliance"),
                        ("steel_consortium", "steelconsortium"),
                        ("latin_syndicate", "latinsyndicate"),
                    ]
                    for old, new in renames:
                        content = content.replace(old, new)
                    
                    # Word-boundary replacements for old InternalNames used as faction references
                    internal_renames = [
                        (r'\bgdi\b', 'td_gdi'),
                        (r'\bnod\b', 'td_nod'),
                        (r'\ballies\b', 'ra1_allies'),
                        (r'\bsoviets\b', 'ra1_soviets'),
                        (r'\btsgdi\b', 'ts_gdi'),
                        (r'\btsnod\b', 'ts_nod'),
                        (r'\bra2allies\b', 'ra2_allies'),
                        (r'\bra2soviets\b', 'ra2_soviets'),
                        (r'\bconsortium\b', 'steelconsortium'),
                        (r'\bsyndicate\b', 'latinsyndicate'),
                    ]
                    for pattern, new in internal_renames:
                        content = re.sub(pattern, new, content, flags=re.IGNORECASE)
                    
                    # ra1_soviet_ -> ra1_soviets_ (singular to plural)
                    content = content.replace("ra1_soviet_", "ra1_soviets_")
                    
                    if content != original:
                        modified = True
                        print(f"  Modified {name} in {path}")
                    
                    data = content.encode('utf-8')
                
                # Preserve the exact entry name (including leading / if present)
                zout.writestr(name, data)
    
    if modified:
        shutil.move(tmpfile, path)
        print(f"  Updated: {path}")
    else:
        os.unlink(tmpfile)
        print(f"  No changes needed: {path}")

if __name__ == '__main__':
    if len(sys.argv) < 2:
        # Fix all .oramap files in mods/cameo/maps
        maps_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'mods', 'cameo', 'maps')
        for f in sorted(os.listdir(maps_dir)):
            if f.endswith('.oramap'):
                fix_oramap(os.path.join(maps_dir, f))
    else:
        for path in sys.argv[1:]:
            fix_oramap(path)
