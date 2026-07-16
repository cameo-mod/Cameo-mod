#!/usr/bin/env python3
"""Analyze ai.yaml and generate per-faction ai.yaml files for each ContentPack.

Creates per-faction ai.yaml files with:
- Descriptive comments explaining the AI architecture
- Reference data extracted from the global ai.yaml
- Player: header (no-op, ready for future trait additions)

The actual bot module data (BuildingLimits, BuildingFractions, UnitsToBuild,
UnitLimits) CANNOT be split yet — see ROADMAP backlog for details."""
import os
import re

TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.join(TOOLS_DIR, "..")
AI_FILE = os.path.join(REPO_ROOT, "mods", "cameo", "ai", "ai.yaml")
CONTENT_PACKS = os.path.join(REPO_ROOT, "mods", "cameo", "ContentPacks")

# Map faction section names to (ContentPack path, prefix) pairs
FACTION_MAP = {
    'CNC': [('TiberianDawn/GDI', 'td_gdi'), ('TiberianDawn/Nod', 'td_nod')],
    'RA': [('RedAlert', 'ra1_allies'), ('RedAlert', 'ra1_soviets'), ('RedAlert', 'japan')],
    'RA2': [('RedAlert2', 'ra2_allies'), ('RedAlert2', 'ra2_soviets'), ('RedAlert2', 'yuri')],
    'TS GDI': [('TiberianSun/GDI', 'ts_gdi')],
    'TS Nod': [('TiberianSun/Nod', 'ts_nod')],
    'TS Forgotten': [('TiberianSun/Forgotten', 'forgotten')],
    'TS CABAL': [('TiberianSun/CABAL', 'cabal')],
    'Asian': [('RedAlert2Mod/AsianAlliance', 'asianalliance')],
    'Consortium': [('RedAlert2Mod/Consortium', 'steelconsortium')],
    'Syndicate': [('RedAlert2Mod/Syndicate', 'latinsyndicate')],
    'Naxis': [('RedAlert2Mod/Naxis', 'naxis')],
    'Schwarzer Mond': [('RedAlert2Mod/SchwarzerMond', 'schwarzermond')],
    'Future Tech': [('RedAlert2Mod/FutureTech', 'futuretech')],
    'Ixian': [('D2k/Ixian', 'ixian')],
    'Ordos': [('D2k/Ordos', 'ordos')],
    'Atreides': [('D2k/Atreides', 'atreides')],
    'Harkonnen': [('D2k/Harkonnen', 'harkonnen')],
    'TERRAN': [('StarCraft', 'terran')],
    'PROTOSS': [('StarCraft', 'protoss')],
    'ZERG': [('StarCraft', 'zerg')],
    'Warcraft 2 Humans': [('Warcraft2', 'wc2_humans')],
    'Warcraft 2 Orcs': [('Warcraft2', 'wc2_orcs')],
    'TKM': [('TKM', 'tkm')],
    'Dawn of the Tomorrow': [('Outpost2', 'tomorrow')],
    'GLA': [('Outpost2', 'gla')],
    'CHINA': [('Outpost2', 'china')],
    'USA': [('Outpost2', 'usa')],
}

def parse_ai_sections():
    """Parse ai.yaml and extract per-faction sections.
    Sections are delimited by ####...<ListName> <Faction>... comments.
    Data lines are everything between one section header and the next."""
    with open(AI_FILE, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    # Find all section header positions
    headers = []
    for i, line in enumerate(lines):
        s = line.strip()
        if s.startswith('#') and '####' in s:
            for list_name in ['BuildingLimits', 'BuildingFractions', 'UnitsToBuild', 'UnitLimits']:
                if list_name in s:
                    faction = s.replace(list_name, '').replace('#', '').strip()
                    headers.append((i, list_name, faction))
                    break

    # Extract data between headers
    sections = {}
    for idx, (line_i, list_name, faction) in enumerate(headers):
        if idx + 1 < len(headers):
            end_i = headers[idx + 1][0]
        else:
            end_i = len(lines)

        data_lines = []
        for j in range(line_i + 1, end_i):
            line = lines[j]
            s = line.strip()
            if not s:
                continue
            if s.startswith('#') and '####' in s:
                continue
            data_lines.append(line.rstrip())

        if faction not in sections:
            sections[faction] = {}
        sections[faction][list_name] = data_lines

    return sections

def filter_lines_by_prefix(data_lines, prefixes):
    """Filter data lines to only those matching one of the given prefixes."""
    result = []
    for line in data_lines:
        s = line.strip()
        if s.startswith('#'):
            continue
        for prefix in prefixes:
            if prefix in s:
                result.append(line)
                break
    return result

def get_faction_prefixes(faction_prefix):
    """Get all prefix variants for a faction."""
    prefixes = [faction_prefix]
    if faction_prefix == 'td_gdi':
        prefixes.append('td_gdi_')
    elif faction_prefix == 'td_nod':
        prefixes.append('td_nod_')
    elif faction_prefix == 'ra1_allies':
        prefixes.extend(['ra1_allies_', 'allies_'])
    elif faction_prefix == 'ra1_soviets':
        prefixes.extend(['ra1_soviets_', 'soviets_'])
    elif faction_prefix == 'ra2_allies':
        prefixes.append('ra2_allies_')
    elif faction_prefix == 'ra2_soviets':
        prefixes.append('ra2_soviets_')
    elif faction_prefix == 'ts_gdi':
        prefixes.append('ts_gdi_')
    elif faction_prefix == 'ts_nod':
        prefixes.append('ts_nod_')
    return prefixes

def generate_ai_content(faction_header, faction_prefix, faction_data):
    """Generate ai.yaml content for a faction."""
    prefixes = get_faction_prefixes(faction_prefix)

    lines = []
    lines.append(f"# ai.yaml — {faction_header} faction AI configuration")
    lines.append("#")
    lines.append("# ARCHITECTURE NOTE")
    lines.append("# =================")
    lines.append("# All AI bot modules (BaseBuilderBotModuleCA, UnitBuilderBotModuleCA,")
    lines.append("# SquadManagerBotModuleCA, SupportPowerBotASModule, etc.) are defined")
    lines.append("# as single trait instances on the Player: actor in the global")
    lines.append("# mods/cameo/ai/ai.yaml. Their sub-sections (BuildingLimits,")
    lines.append("# BuildingFractions, UnitsToBuild, UnitLimits) contain ALL faction")
    lines.append("# data in single dictionaries that CANNOT be split across files —")
    lines.append("# OpenRA's YAML merge replaces trait instances with the same @name,")
    lines.append("# it does not deep-merge their sub-sections.")
    lines.append("#")
    lines.append("# This file is loaded as a Rules: entry but contains no traits —")
    lines.append("# it is a placeholder ready for future per-faction bot module")
    lines.append("# splitting (see ROADMAP Phase E backlog).")
    lines.append("#")
    lines.append("# REFERENCE DATA (from global ai.yaml)")
    lines.append("# ====================================")

    for list_name in ['BuildingLimits', 'BuildingFractions', 'UnitsToBuild', 'UnitLimits']:
        if list_name not in faction_data:
            continue
        all_lines = faction_data[list_name]
        faction_lines = filter_lines_by_prefix(all_lines, prefixes)
        if not faction_lines:
            continue

        lines.append(f"#")
        lines.append(f"# --- {list_name} ({len(faction_lines)} entries for {faction_prefix}) ---")
        for entry in faction_lines:
            lines.append(f"# {entry}")

    lines.append("")
    lines.append("# Player: actor — no traits defined yet.")
    lines.append("# When the engine supports per-faction bot conditions, move the")
    lines.append("# faction-specific BuildingLimits/BuildingFractions/UnitsToBuild")
    lines.append("# entries here as a BaseBuilderBotModuleCA@<faction> /")
    lines.append("# UnitBuilderBotModuleCA@<faction> trait with RequiresCondition.")
    lines.append("Player:")
    lines.append("")

    return '\n'.join(lines) + '\n'

def generate_additional_faction_content(faction_header, faction_prefix, faction_data):
    """Generate additional faction content for multi-faction packs."""
    prefixes = get_faction_prefixes(faction_prefix)

    lines = []
    has_data = False
    for list_name in ['BuildingLimits', 'BuildingFractions', 'UnitsToBuild', 'UnitLimits']:
        if list_name not in faction_data:
            continue
        all_lines = faction_data[list_name]
        faction_lines = filter_lines_by_prefix(all_lines, prefixes)
        if not faction_lines:
            continue
        has_data = True
        lines.append(f"#")
        lines.append(f"# --- Additional faction: {faction_header} ({faction_prefix}) — {list_name} ({len(faction_lines)} entries) ---")
        for entry in faction_lines:
            lines.append(f"# {entry}")

    if has_data:
        return '\n'.join(lines) + '\n'
    return ''

def generate_faction_ai_files(sections):
    """Generate per-faction ai.yaml files."""
    generated = []
    seen_paths = {}

    for faction_header, pack_list in FACTION_MAP.items():
        if faction_header not in sections:
            continue

        faction_data = sections[faction_header]

        for pack_path, faction_prefix in pack_list:
            pack_dir = os.path.join(CONTENT_PACKS, pack_path)
            if not os.path.isdir(pack_dir):
                continue

            ai_file = os.path.join(pack_dir, "yaml", "ai.yaml")

            if ai_file in seen_paths:
                # Append additional faction data
                existing = seen_paths[ai_file]
                additional = generate_additional_faction_content(faction_header, faction_prefix, faction_data)
                if additional:
                    seen_paths[ai_file] = existing.rstrip() + '\n' + additional
                continue

            content = generate_ai_content(faction_header, faction_prefix, faction_data)
            seen_paths[ai_file] = content

    # Write all files
    for ai_file, content in seen_paths.items():
        os.makedirs(os.path.dirname(ai_file), exist_ok=True)
        with open(ai_file, 'w', encoding='utf-8', newline='\n') as f:
            f.write(content)
        pack_path = os.path.relpath(ai_file, CONTENT_PACKS)
        generated.append((pack_path, ai_file, len(content)))

    return generated

if __name__ == '__main__':
    print("Parsing ai.yaml sections...")
    sections = parse_ai_sections()
    print(f"Found {len(sections)} faction sections:")
    for faction in sorted(sections.keys()):
        data = sections[faction]
        counts = {k: len([l for l in v if l.strip() and not l.strip().startswith('#')]) for k, v in data.items()}
        total = sum(counts.values())
        if total > 0:
            print(f"  {faction}: {counts} ({total} total data lines)")

    print("\nGenerating per-faction ai.yaml files...")
    generated = generate_faction_ai_files(sections)
    print(f"\nGenerated {len(generated)} files:")
    for pack_path, filepath, size in generated:
        rel = os.path.relpath(filepath, REPO_ROOT)
        print(f"  {rel} ({size} bytes)")
