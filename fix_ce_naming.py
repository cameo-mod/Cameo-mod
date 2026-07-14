import re, os, glob

base = 'mods/cameo'
canonical = {'@Effect', '@EffectAir', '@EffectWater', '@ShieldHitEffect'}
skip_names = {'@Effect1', '@Effect2', '@Ricochet', '@Reload', '@Beam', '@EffectWeld'}

def to_canonical(wh_name):
    if wh_name in canonical or wh_name in skip_names:
        return None
    name_lower = wh_name.lower()
    if 'water' in name_lower:
        return '@EffectWater'
    if 'air' in name_lower:
        return '@EffectAir'
    if 'shield' in name_lower:
        return '@ShieldHitEffect'
    return '@Effect'

# Pass 1: Find collision weapons
collision_keys = set()

for yaml_path in sorted(glob.glob(f'{base}/**/*.yaml', recursive=True)):
    with open(yaml_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    current = None
    canonical_counts = {}
    for i, line in enumerate(lines):
        stripped = line.strip()
        if not line.startswith('\t') and not line.startswith(' ') and stripped.endswith(':') and not stripped.startswith('-') and not stripped.startswith('#'):
            for cname, count in canonical_counts.items():
                if count > 1:
                    collision_keys.add((os.path.relpath(yaml_path, base), current, cname))
            current = stripped[:-1]
            canonical_counts = {}
        m = re.match(r'\tWarhead@(\w+):\s*(.*)', line)
        if m and 'CreateEffect' in line:
            wh_name = '@' + m.group(1)
            new_name = to_canonical(wh_name)
            if new_name:
                canonical_counts[new_name] = canonical_counts.get(new_name, 0) + 1
            elif wh_name in canonical:
                canonical_counts[wh_name] = canonical_counts.get(wh_name, 0) + 1
    for cname, count in canonical_counts.items():
        if count > 1:
            collision_keys.add((os.path.relpath(yaml_path, base), current, cname))

print(f"Collision weapons to skip: {len(collision_keys)}")

# Pass 2: Rename, skipping collision weapons
changes = []
files_changed = set()
skipped = 0

for yaml_path in sorted(glob.glob(f'{base}/**/*.yaml', recursive=True)):
    with open(yaml_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    modified = False
    current = None
    for i, line in enumerate(lines):
        stripped = line.strip()
        if not line.startswith('\t') and not line.startswith(' ') and stripped.endswith(':') and not stripped.startswith('-') and not stripped.startswith('#'):
            current = stripped[:-1]
        m = re.match(r'(\tWarhead@)(\w+)(:\s*CreateEffect)', line)
        if m:
            wh_name = '@' + m.group(2)
            new_name = to_canonical(wh_name)
            if new_name:
                rel_path = os.path.relpath(yaml_path, base)
                if (rel_path, current, new_name) in collision_keys:
                    skipped += 1
                    continue
                lines[i] = m.group(1) + new_name[1:] + m.group(3) + '\n'
                changes.append((rel_path, i+1, wh_name, new_name))
                modified = True
                files_changed.add(yaml_path)
    if modified:
        with open(yaml_path, 'w', encoding='utf-8', newline='') as f:
            f.writelines(lines)

print(f"Files modified: {len(files_changed)}")
print(f"Warheads renamed: {len(changes)}")
print(f"Warheads skipped (collision): {skipped}")
