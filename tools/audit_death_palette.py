import re, os, glob

base = 'mods/cameo'

def find_yaml_files(base):
    results = []
    for root, dirs, files in os.walk(base):
        for f in files:
            if f.endswith('.yaml'):
                results.append(os.path.join(root, f))
    return results

mismatches = []

for fpath in find_yaml_files(base):
    with open(fpath, encoding='utf-8') as f:
        lines = f.readlines()
    
    current_actor = None
    render_palette = None
    death_palette = None
    death_line = None
    render_line = None
    
    for i, line in enumerate(lines, 1):
        m = re.match(r'^(\w+):', line)
        if m and not line.startswith('\t'):
            # Check previous actor
            if current_actor and death_palette and render_palette and death_palette != render_palette:
                mismatches.append((fpath, current_actor, render_palette, render_line, death_palette, death_line))
            
            current_actor = m.group(1)
            render_palette = None
            death_palette = None
            render_line = None
            death_line = None
        
        if 'PlayerPalette:' in line and 'DeathPaletteIsPlayerPalette' not in line:
            # Could be in RenderSprites or WithDeathAnimation
            # We need to track which trait this belongs to
            pass
        
        if 'DeathSequencePalette:' in line:
            death_palette = line.split('DeathSequencePalette:')[1].strip().split()[0] if ':' in line.split('DeathSequencePalette:')[1] else line.split('DeathSequencePalette:')[1].strip()
            death_line = i
        
        if 'PlayerPalette:' in line and 'DeathPaletteIsPlayerPalette' not in line:
            render_palette = line.split('PlayerPalette:')[1].strip().split()[0] if ':' in line.split('PlayerPalette:')[1] else line.split('PlayerPalette:')[1].strip()
            render_line = i
    
    # Check last actor
    if current_actor and death_palette and render_palette and death_palette != render_palette:
        mismatches.append((fpath, current_actor, render_palette, render_line, death_palette, death_line))

if not mismatches:
    print("No mismatches found!")
else:
    print(f"Found {len(mismatches)} mismatches:")
    for fpath, actor, rp, rl, dp, dl in mismatches:
        short = fpath.replace('mods/cameo/', '')
        print(f"  {short}:{dl} - {actor}: RenderSprites PlayerPalette={rp} (line {rl}) vs DeathSequencePalette={dp} (line {dl})")
