import re, os

base = 'mods/cameo'

def find_yaml_files(base):
    results = []
    for root, dirs, files in os.walk(base):
        for f in files:
            if f.endswith('.yaml'):
                results.append(os.path.join(root, f))
    return results

# Common palette name corrections:
# ra2player -> playerra2 (most common mismatch)
# raplayer -> playerra (for RA1 units)
# ra2infantry -> playerra2 (for RA2 dogs/infantry)
# player_rgba -> playerra2 (for CABAL/Forgotten units that should use playerra2)

# Strategy: For each actor, find RenderSprites PlayerPalette and WithDeathAnimation DeathSequencePalette.
# If they differ, set DeathSequencePalette = PlayerPalette value.

changes = []

for fpath in find_yaml_files(base):
    with open(fpath, encoding='utf-8') as f:
        lines = f.readlines()
    
    current_actor = None
    # Track all PlayerPalette and DeathSequencePalette occurrences with their context
    render_palette = None
    render_line = None
    death_palette = None
    death_line = None
    in_rendersprites = False
    in_deathanim = False
    
    modified = False
    
    for i, line in enumerate(lines):
        m = re.match(r'^(\w+):', line)
        if m and not line.startswith('\t'):
            # Process previous actor
            if current_actor and death_palette and render_palette and death_palette != render_palette:
                # Fix: set death palette to match render palette
                old_line = lines[death_line - 1]
                new_line = re.sub(r'DeathSequencePalette:\s*\S+', f'DeathSequencePalette: {render_palette}', old_line)
                if new_line != old_line:
                    lines[death_line - 1] = new_line
                    modified = True
                    short = fpath.replace('mods/cameo\\', '').replace('mods/cameo/', '')
                    changes.append(f"  {short}:{death_line} - {current_actor}: {death_palette} -> {render_palette}")
            
            current_actor = m.group(1)
            render_palette = None
            render_line = None
            death_palette = None
            death_line = None
            in_rendersprites = False
            in_deathanim = False
        
        stripped = line.strip()
        if stripped == 'RenderSprites:':
            in_rendersprites = True
            in_deathanim = False
        elif stripped == 'WithDeathAnimation:':
            in_deathanim = True
            in_rendersprites = False
        elif re.match(r'^\w', stripped) and ':' in stripped and not stripped.startswith('-'):
            # New trait at same indent level
            in_rendersprites = False
            in_deathanim = False
        
        if 'PlayerPalette:' in line and 'DeathPaletteIsPlayerPalette' not in line:
            val = line.split('PlayerPalette:')[1].strip().split()[0] if line.split('PlayerPalette:')[1].strip() else ''
            if val and val != 'true' and val != 'True':
                if in_rendersprites or (not in_deathanim and not in_rendersprites):
                    render_palette = val
                    render_line = i + 1
                elif in_deathanim:
                    # PlayerPalette in WithDeathAnimation - this is the death palette
                    death_palette = val
                    death_line = i + 1
        
        if 'DeathSequencePalette:' in line:
            val = line.split('DeathSequencePalette:')[1].strip().split()[0] if line.split('DeathSequencePalette:')[1].strip() else ''
            if val:
                death_palette = val
                death_line = i + 1
    
    # Process last actor
    if current_actor and death_palette and render_palette and death_palette != render_palette:
        old_line = lines[death_line - 1]
        new_line = re.sub(r'DeathSequencePalette:\s*\S+', f'DeathSequencePalette: {render_palette}', old_line)
        if new_line != old_line:
            lines[death_line - 1] = new_line
            modified = True
            short = fpath.replace('mods/cameo\\', '').replace('mods/cameo/', '')
            changes.append(f"  {short}:{death_line} - {current_actor}: {death_palette} -> {render_palette}")
    
    if modified:
        with open(fpath, 'w', encoding='utf-8') as f:
            f.writelines(lines)

if changes:
    print(f"Applied {len(changes)} fixes:")
    for c in changes:
        print(c)
else:
    print("No changes needed.")
