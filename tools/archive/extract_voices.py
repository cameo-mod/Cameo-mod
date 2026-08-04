#!/usr/bin/env python3
"""Find TD GDI actors that are missing Voiced traits in current vs release."""
import re
import os

base = r"mods\cameo\ContentPacks\TiberianDawn\GDI\yaml"
release_base = r"C:\Users\AedisToru\AppData\Local\Cameo-IFV\instances\cameo\main\mods\cameo\ContentPacks\TiberianDawn\GDI\rules"

files = ["vehicles", "infantry", "aircraft", "naval", "buildings"]

actor_re = re.compile(r'^([a-z_][a-z0-9_]*):$')
voice_re = re.compile(r'VoiceSet:\s*(\S+)')
inherit_re = re.compile(r'Inherits:\s*(\S+)')

def extract_actors(path):
    actors = {}
    current = None
    has_voiced = False
    inherits = None
    if not os.path.exists(path):
        return actors
    with open(path, 'r', encoding='utf-8') as f:
        for line in f:
            stripped = line.strip()
            m = actor_re.match(stripped)
            if m:
                if current:
                    actors[current] = {'voiced': has_voiced, 'inherits': inherits}
                current = m.group(1)
                has_voiced = False
                inherits = None
            if voice_re.search(stripped):
                has_voiced = True
            m2 = inherit_re.search(stripped)
            if m2:
                inherits = m2.group(1)
        if current:
            actors[current] = {'voiced': has_voiced, 'inherits': inherits}
    return actors

print("=== CURRENT BUILD: Actors WITHOUT Voiced ===")
for fname in files:
    p = os.path.join(base, f"{fname}.yaml")
    if not os.path.exists(p):
        continue
    actors = extract_actors(p)
    missing = {k: v for k, v in actors.items() if not v['voiced']}
    print(f"\n--- {fname}.yaml ({len(missing)}/{len(actors)} missing Voiced) ---")
    for actor, info in sorted(missing.items()):
        inh = f" (Inherits: {info['inherits']})" if info['inherits'] else ""
        print(f"  {actor}{inh}")

print("\n\n=== RELEASE BUILD: Actors WITHOUT Voiced ===")
for fname in files:
    p = os.path.join(release_base, f"{fname}.yaml")
    if not os.path.exists(p):
        continue
    actors = extract_actors(p)
    missing = {k: v for k, v in actors.items() if not v['voiced']}
    print(f"\n--- {fname}.yaml ({len(missing)}/{len(actors)} missing Voiced) ---")
    for actor, info in sorted(missing.items()):
        inh = f" (Inherits: {info['inherits']})" if info['inherits'] else ""
        print(f"  {actor}{inh}")
