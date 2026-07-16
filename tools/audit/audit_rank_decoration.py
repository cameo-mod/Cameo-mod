#!/usr/bin/env python
"""Verify every GainsExperienceTD actor has the correct ^*RankDecoration for its faction.
Verify GainsExperienceRA2 actors do NOT have a separate ^*RankDecoration.
Check that rank image sequences exist in misc.yaml."""
import os, re, sys

root = "mods/cameo"
results = []
missing_seq = []

# Known rank decoration templates and their sequence images
RANK_DECORATIONS = {
    'GDIRankDecoration': 'gdirank',
    'NodRankDecoration': 'nodrank',
    'CABALRankDecoration': 'cabalrank',
    'ForgottenRankDecoration': 'forgotrank',
    'DuneRankDecoration': 'dunerank',
    'AlienRankDecoration': 'alienrank',
}

# Faction → expected rank decoration (by ContentPack path prefix)
FACTION_DECORATION = {
    'ContentPacks/TiberianDawn/GDI/': 'GDIRankDecoration',
    'ContentPacks/TiberianDawn/Nod/': 'NodRankDecoration',
    'ContentPacks/TiberianSun/CABAL/': 'CABALRankDecoration',
    'ContentPacks/TiberianSun/Forgotten/': 'ForgottenRankDecoration',
    'ContentPacks/TiberianSun/GDI/': 'GDIRankDecoration',
    'ContentPacks/TiberianSun/Nod/': 'NodRankDecoration',
    'ContentPacks/D2k/': 'DuneRankDecoration',
}

# Check rank image sequences exist
seq_root = os.path.join(root, "sequences")
for dec_name, seq_name in RANK_DECORATIONS.items():
    found = False
    for dirpath, _, filenames in os.walk(seq_root):
        for fn in filenames:
            if not fn.endswith(".yaml"):
                continue
            fpath = os.path.join(dirpath, fn)
            try:
                content = open(fpath, encoding="utf-8").read()
            except Exception:
                continue
            if re.search(rf'^{seq_name}:', content, re.MULTILINE):
                found = True
                break
        if found:
            break
    if not found:
        missing_seq.append((dec_name, seq_name))

# Scan all actors
for dirpath, _, filenames in os.walk(root):
    for fn in filenames:
        if not fn.endswith(".yaml"):
            continue
        fpath = os.path.join(dirpath, fn)
        rel = fpath.replace("\\", "/").replace("mods/cameo/", "")
        try:
            lines = open(fpath, encoding="utf-8").readlines()
        except Exception:
            continue
        i = 0
        while i < len(lines):
            line = lines[i]
            am = re.match(r'^(\S+):', line)
            if am and not line.startswith('\t') and not line.strip().startswith('#'):
                actor_name = am.group(1)
                j = i + 1
                has_gains_td = False
                has_gains_ra2 = False
                has_gains_ra = False
                rank_decoration = None
                while j < len(lines):
                    bl = lines[j]
                    if re.match(r'^[^\t\s]', bl) and not bl.strip().startswith('#'):
                        break
                    stripped = bl.strip()
                    if stripped.startswith('#'):
                        j += 1
                        continue
                    if 'GainsExperienceTD' in stripped:
                        has_gains_td = True
                    if 'GainsExperienceRA2' in stripped:
                        has_gains_ra2 = True
                    if 'GainsExperienceRA' in stripped and 'GainsExperienceRA2' not in stripped:
                        has_gains_ra = True
                    for dec in RANK_DECORATIONS:
                        if dec in stripped:
                            rank_decoration = dec
                            break
                    j += 1
                
                # Determine expected decoration from path
                expected = None
                for prefix, dec in FACTION_DECORATION.items():
                    if prefix in rel:
                        expected = dec
                        break
                
                if has_gains_ra2 and rank_decoration:
                    results.append((rel, i+1, actor_name, "RA2 actor has RankDecoration (should not)", rank_decoration))
                elif has_gains_td and not rank_decoration:
                    results.append((rel, i+1, actor_name, "TD actor missing RankDecoration", expected or "?"))
                elif has_gains_td and expected and rank_decoration != expected:
                    results.append((rel, i+1, actor_name, f"Wrong decoration (expected {expected})", rank_decoration))
            i += 1

print("# Rank decoration audit\n")
if missing_seq:
    print(f"## Missing rank image sequences: **{len(missing_seq)}**\n")
    for dec, seq in missing_seq:
        print(f"- {dec} → `{seq}` not found in sequences/")
    print()

print(f"## Actor decoration issues: **{len(results)}**\n")
if results:
    print("| File | Line | Actor | Issue | Found |")
    print("|---|---|---|---|---|")
    for fpath, line, actor, issue, found in sorted(results):
        print(f"| {fpath} | {line} | {actor} | {issue} | {found} |")
