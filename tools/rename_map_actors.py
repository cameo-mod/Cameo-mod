import os, re

RENAME_MAP = {
    # NOD actors (from rename_map_nod.yaml)
    "afld": "td_nod_airstrip",
    "arty": "td_nod_artillery",
    "bggy": "td_nod_buggy",
    "bike": "td_nod_reconbike",
    "e1.nod": "td_nod_minigunner",
    "e3.nod": "td_nod_rocketsoldier",
    "e4": "td_nod_flamethrower",
    "e5": "td_nod_chemicalwarrior",
    "fact.nod": "td_nod_constructionyard",
    "fix.nod": "td_nod_repairfacility",
    "ftnk": "td_nod_flametank",
    "gun": "td_nod_gunturret",
    "hand": "td_nod_handofnod",
    "heli": "td_nod_apacheattackhelicopter",
    "hpad.nod": "td_nod_helipad",
    "hq.nod": "td_nod_communicationscenter",
    "ltnk": "td_nod_lighttank",
    "obli": "td_nod_obeliskoflight",
    "proc.nod": "td_nod_tiberiumrefinery",
    "rmbo.nod": "td_nod_commando",
    "sam": "td_nod_samsite",
    # GDI actors (from rename_map_gdi.yaml)
    "hq.gdi": "td_gdi_communicationscenter",
    "rmbo.gdi": "td_gdi_commando",
    "tran.gdi": "td_gdi_chinooktransport",
    # Soviet actors (from rename_map_soviet.yaml)
    "barr": "ra1_soviets_barracks",
    # Japan actors (from rename_map_modjapan.yaml)
    "cycl": "japan_chainlinkfence",
    # Zerg actors (from rename_map_zerg.yaml)
    "scevolutionchamber": "zerg_evolutionchamber",
    "scextractor": "zerg_extractor",
    "schatchery": "zerg_hatchery",
    "schydraliskden": "zerg_hydraliskden",
    "scspawningpool": "zerg_spawningpool",
    "scsporecolony": "zerg_sporecolony",
    "scsunkencolony": "zerg_sunkencolony_2",
    "sczergling": "zerg_zergling",
    "schydralisk": "zerg_hydralisk",
    "scultralisk": "zerg_ultralisk",
}

# Sort by length descending so longer names match first (e.g. "e1.nod" before "e1")
SORTED_KEYS = sorted(RENAME_MAP.keys(), key=len, reverse=True)

def replace_actor_names_in_line(line):
    """Replace old actor names with new names in a single line."""
    for old in SORTED_KEYS:
        # Use word boundary that works with dots in names
        # Match the old name as a whole word (not partial)
        pattern = r'\b' + re.escape(old) + r'\b'
        line = re.sub(pattern, RENAME_MAP[old], line)
    return line

def process_yaml_file(filepath):
    """Process a map.yaml file, replacing actor names in Actor lines."""
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    changed = 0
    for i, line in enumerate(lines):
        # Actor definition lines look like: \tActorNN: oldname
        # Only replace the actor type (after the colon), not the ActorNN identifier
        m = re.match(r'^(\tActor\w+:\s*)(\S+)', line)
        if m:
            prefix = m.group(1)
            actor_type = m.group(2)
            if actor_type in RENAME_MAP:
                new_type = RENAME_MAP[actor_type]
                # Replace just the actor type part
                lines[i] = line[:m.start(2)] + new_type + line[m.end(2):]
                changed += 1

    with open(filepath, 'w', encoding='utf-8', newline='') as f:
        f.writelines(lines)
    print(f"  {filepath}: {changed} actor replacements")

def process_lua_file(filepath):
    """Process a lua file, replacing actor name strings."""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    changed = 0
    for old in SORTED_KEYS:
        # In Lua, actor names appear as quoted strings: "oldname"
        pattern = '"' + re.escape(old) + '"'
        count = content.count(pattern)
        if count > 0:
            content = content.replace(pattern, '"' + RENAME_MAP[old] + '"')
            changed += count

    with open(filepath, 'w', encoding='utf-8', newline='') as f:
        f.write(content)
    print(f"  {filepath}: {changed} string replacements")

base = os.path.dirname(os.path.abspath(__file__))
mod_root = os.path.join(base, '..', 'mods', 'cameo', 'maps')

print("Processing map.yaml files:")
process_yaml_file(os.path.join(mod_root, 'delivery', 'map.yaml'))
process_yaml_file(os.path.join(mod_root, 'deliverycoop', 'map.yaml'))

print("Processing lua files:")
process_lua_file(os.path.join(mod_root, 'delivery', 'delivery.lua'))
process_lua_file(os.path.join(mod_root, 'delivery', 'campaign.lua'))
process_lua_file(os.path.join(mod_root, 'deliverycoop', 'deliverycoop.lua'))
process_lua_file(os.path.join(mod_root, 'deliverycoop', 'campaign.lua'))

print("Done!")
