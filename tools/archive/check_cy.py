#!/usr/bin/env python3
import pathlib, re

root = pathlib.Path(r'c:\Users\AedisToru\Documents\GitHub\Cameo-mod\mods\cameo')
for faction_dir in ['RedAlert2/Allies', 'RedAlert2/Soviets', 'RedAlert2/Yuri',
                     'RedAlert2Mod/AsianAlliance', 'RedAlert2Mod/Consortium',
                     'RedAlert2Mod/FutureTech', 'RedAlert2Mod/Naxis',
                     'RedAlert2Mod/SchwarzerMond', 'RedAlert2Mod/Syndicate']:
    for yml in sorted((root / 'ContentPacks' / faction_dir).rglob('*.yaml')):
        text = yml.read_text(encoding='utf-8-sig', errors='replace')
        for m in re.finditer(r'^(\w*construction\w*):\s*\n((?:\s.*\n)*)', text, re.M):
            actor = m.group(1)
            block = m.group(2)
            inh = re.search(r'Inherits:\s*(\S+)', block)
            rel = yml.relative_to(root)
            inh_val = inh.group(1) if inh else 'NONE'
            print(f'{rel}: {actor} Inherits: {inh_val}')
            for pm in re.finditer(r'ProvidesPrerequisite@(\w+):\s*\n\s+Prerequisite:\s*(\S+)', block):
                print(f'  ProvidesPrerequisite@{pm.group(1)}: {pm.group(2)}')
