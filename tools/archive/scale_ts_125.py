#!/usr/bin/env python3
"""Scale TS buildings to 1.25x and TS GDI walkers/vehicles to 1.25x,
remove/scale child overrides, keep bib/ground/platform/underlay unscaled.
"""

import re
import sys
from pathlib import Path

TARGET = 1.25
BIB_NAMES = {'bib', 'bib2', 'bib3', 'ground', 'platform', 'underlay',
             'dead-bib', 'dead-bib2', 'dead-bib3', 'dead-ground', 'dead-platform', 'dead-underlay',
             'damaged-bib', 'damaged-bib2', 'damaged-bib3', 'damaged-ground', 'damaged-platform', 'damaged-underlay',
             'idle-bib', 'idle-bib2', 'idle-bib3', 'idle-ground', 'idle-platform', 'idle-underlay',
             'idle-platform', 'damaged-idle-platform'}

INFANTRY_KEYWORDS = [
    'infantry', 'cyborg', 'commando', 'medic', 'engineer', 'mutant',
    'ghoststalker', 'discthrower', 'zonetrooper', 'riottrooper', 'falconenforcer',
    'militant', 'rocketinfantry', 'sniper', 'technician', 'devout', 'ascended',
    'rocketcyborg', 'hackercyborg', 'enlighted', 'berserker', 'eliminator',
    'assassin', 'reaper', 'ravager', 'dissolver', 'drone'
]

BUILDING_KEYWORDS = [
    'powerplant', 'plant', 'turbine', 'barracks', 'warfactory', 'radar', 'techcenter',
    'helipad', 'refinery', 'silo', 'missilesilo', 'wastefacility', 'obelisk',
    'stealthgenerator', 'handof', 'laserfence', 'laserturret', 'samsite',
    'temple', 'tmpl', 'dept', 'puls', 'const', 'constructionyard', 'depot',
    'emp', 'plasmaturret', 'outpost', 'bunker', 'gate', 'wall', 'station',
    'dome', 'generator', 'facility', 'center', 'pad', 'tower', 'pillbox'
]

AIRCRAFT_KEYWORDS = ['orca', 'hammerhead', 'carryall', 'gunship', 'mothership', 'commandship', 'kodiak']
PROMOTION_KEYWORDS = ['mkii', 'elite', 'promotion', 'unlock']

FILES = [
    'mods/cameo/ContentPacks/TiberianSun/GDI/yaml/sequences.yaml',
    'mods/cameo/ContentPacks/TiberianSun/Nod/yaml/sequences.yaml',
    'mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/sequences.yaml',
    'mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/sequences.yaml',
    'mods/cameo/sequences/tiberiansun.yaml',
]


def is_infantry(aid, text):
    low = aid.lower()
    if any(k in low for k in INFANTRY_KEYWORDS):
        return True
    if re.search(r'^\tInherits:.*Infantry', text, re.M):
        return True
    if re.search(r'^\tInherits@.*:.*Infantry', text, re.M):
        return True
    return False


def is_building(aid, text):
    if re.search(r'^\tInherits: ?\^(WithBuildingBib|Building|TSBuilding|TSDefense|Defense|BaseBuilding|CivBuilding)', text, re.M):
        return True
    low = aid.lower()
    if any(k in low for k in BUILDING_KEYWORDS):
        return True
    return False


def is_aircraft(aid):
    low = aid.lower()
    return any(k in low for k in AIRCRAFT_KEYWORDS)


def is_promotion(aid, text):
    low = aid.lower()
    if any(k in low for k in PROMOTION_KEYWORDS):
        return True
    if re.search(r'promotion_|_unlock', text):
        return True
    return False


def is_gdi_vehicle_walker(aid, text):
    low = aid.lower()
    if low != 'tsmcv' and not low.startswith('ts_gdi_'):
        return False
    if is_infantry(aid, text) or is_building(aid, text) or is_aircraft(aid) or is_promotion(aid, text):
        return False
    return True


def classify(aid, text):
    if aid.startswith('^'):
        return None
    if is_building(aid, text):
        return 'building'
    if is_gdi_vehicle_walker(aid, text):
        return 'vehicle'
    return None


def parse_blocks(lines):
    blocks = []
    current = None
    actor_re = re.compile(r'^([A-Za-z_][A-Za-z0-9_\.]*):$')
    for line in lines:
        m = actor_re.match(line)
        if m and not line.startswith('\t') and not line.startswith(' '):
            if current:
                blocks.append(current)
            current = [m.group(1), [line]]
        else:
            if current is None:
                # stray line before first actor? keep as-is
                blocks.append(('', [line]))
            else:
                current[1].append(line)
    if current:
        blocks.append(current)
    return blocks


def is_bib_sequence(name):
    n = name.lower()
    # check if any bib token appears as a whole sequence name component
    if n in BIB_NAMES:
        return True
    for token in BIB_NAMES:
        if token in n:
            return True
    return False


def scale_value(s):
    """Parse a Scale line value and return (line_before_number, number, line_after_number)."""
    m = re.match(r'^(\t+)Scale:\s*([0-9.]+)(.*)$', s)
    if not m:
        return None
    return m.group(1), float(m.group(2)), m.group(3) or '\n'


def process_actor(aid, lines, kind):
    if kind is None:
        return lines

    out = [lines[0]]  # actor key line
    i = 1
    while i < len(lines):
        line = lines[i]
        # top-level section inside actor: one tab, not two
        if line.startswith('\t') and not line.startswith('\t\t') and line.strip():
            key = line.strip().split(':')[0]
            if key.startswith('Inherits'):
                out.append(line)
                i += 1
                continue
            if key == 'Defaults':
                default_lines = [line]
                i += 1
                while i < len(lines) and (lines[i].startswith('\t\t') or lines[i] == '\n'):
                    default_lines.append(lines[i])
                    i += 1
                out.extend(process_defaults(default_lines))
                continue
            else:
                seq_name = key
                seq_lines = [line]
                i += 1
                while i < len(lines) and (lines[i].startswith('\t\t') or lines[i] == '\n'):
                    seq_lines.append(lines[i])
                    i += 1
                if kind == 'building':
                    out.extend(process_building_sequence(seq_lines, seq_name))
                else:
                    out.extend(process_vehicle_sequence(seq_lines))
                continue
        else:
            out.append(line)
            i += 1
    return out


def process_defaults(default_lines):
    header = default_lines[0]
    # find and remove existing Scale line(s)
    rest = [l for l in default_lines[1:] if not re.match(r'^\t\tScale:', l)]
    # keep any blank or comment lines that were after removed scale
    new_lines = [header, f'\t\tScale: {TARGET}\n']
    new_lines.extend(rest)
    return new_lines


def process_building_sequence(seq_lines, seq_name):
    header = seq_lines[0]
    if is_bib_sequence(seq_name):
        # keep bib sequences at scale 1
        rest = [l for l in seq_lines[1:] if not re.match(r'^\t\tScale:', l)]
        return [header, '\t\tScale: 1\n'] + rest
    else:
        # remove child scale so Defaults 1.25 applies
        return [header] + [l for l in seq_lines[1:] if not re.match(r'^\t\tScale:', l)]


def process_vehicle_sequence(seq_lines):
    header = seq_lines[0]
    out = [header]
    for line in seq_lines[1:]:
        sv = scale_value(line)
        if sv:
            indent, val, suffix = sv
            new_val = round(val * TARGET, 3)
            # avoid trailing .0 for 1.0 etc
            if new_val == int(new_val):
                new_val = int(new_val)
            out.append(f'{indent}Scale: {new_val}{suffix}')
        else:
            out.append(line)
    return out


def main():
    root = Path(__file__).resolve().parents[2]
    changed = 0
    for rel in FILES:
        path = root / rel
        if not path.exists():
            print(f'missing {path}', file=sys.stderr)
            continue
        text = path.read_text(encoding='utf-8')
        lines = text.splitlines(keepends=True)
        blocks = parse_blocks(lines)
        new_blocks = []
        for aid, block_lines in blocks:
            if aid:
                kind = classify(aid, ''.join(block_lines))
                new_block = process_actor(aid, block_lines, kind)
            else:
                new_block = block_lines
            new_blocks.append(new_block)
        # flatten
        new_lines = []
        for nb in new_blocks:
            new_lines.extend(nb)
        new_text = ''.join(new_lines)
        if new_text != text:
            path.write_text(new_text, encoding='utf-8')
            changed += 1
            print(f'updated {rel}')
        else:
            print(f'no changes {rel}')
    print(f'done, {changed} file(s) changed')


if __name__ == '__main__':
    main()
