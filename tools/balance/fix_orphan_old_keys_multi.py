"""Fix multi-variant bug B: child old keys that shadow a parent's specific
new-variant key. This is a targeted follow-up to fix_orphan_old_keys.py.

For each child old key where a converted parent has exactly one same-family
new key, rename the child's old key to the parent's new key:
- main: strip SpreadDamage, keep bare
- Percentage: rename to *_Percentage
- FriendlyFire: delete block
- ExtraDamage: rename to *_ExtraDamage"""
import re
import sys
from pathlib import Path
from collections import defaultdict

MOD = Path(__file__).resolve().parents[2] / "mods" / "cameo"
CENTRAL = ["weapons/weapons.yaml", "weapons/redalert2.yaml",
           "weapons/redalert2mod.yaml", "weapons/tiberiansun.yaml",
           "weapons/tiberiandawn.yaml", "weapons/warcraft2.yaml",
           "weapons/missiles.yaml"]
FILES = [MOD / p for p in CENTRAL] + sorted((MOD / "ContentPacks").glob("*/*/yaml/weapons.yaml"))

OLD_KEY_FAMILIES = {
    "SmallArms": ["Bullet_Light"], "Chaingun": ["Bullet_Medium"],
    "TankDestroyerCannon": ["CannonAP_Light"], "MediumCannon": ["CannonHE_Medium"],
    "HeavyCannon": ["CannonHE_Heavy"],
    "LightMissile": ["MissileAP_Light", "MissileHE_Light", "MissileAA_Light", "MissileAS_Light", "MissileAT_Light"],
    "MediumMissile": ["MissileAP_Medium", "MissileHE_Medium", "MissileAA_Medium"],
    "HeavyMissile": ["MissileAP_Heavy", "MissileHE_Heavy", "MissileAA_Heavy"],
    "FlakWeapon": ["Flak_Medium", "Flak_Heavy"],
    "HeavyAAWeapon": ["MissileAA_Heavy", "Flak_Heavy"],
    "Grenade": ["Demolition_Light", "Chemical_Light", "Flame_Light"],
    "ShrapnelWeapon": ["Concussion_Medium", "Concussion_Light", "Concussion_Heavy"],
    "HeavyBomb": ["Demolition_Heavy", "Chemical_Heavy", "Flame_Heavy"],
    "LaserWeapon": ["Laser_Heavy", "Laser_Light", "Laser_Medium"],
    "RailgunWeapon": ["Railgun_Heavy", "Railgun_Light"],
    "TeslaWeapon": ["Tesla_Heavy"],
    "TeslaChargedWeapon": ["TeslaCharged_Super"],
    "SwordWeapon": ["Melee_Medium"],
    "ArrowWeapon": ["Arrow_Light"],
    "MagicWeapon": ["Magic_Heavy"],
    "LightFlameWeapon": ["Flame_Light"],
    "MediumFlameWeapon": ["Flame_Medium"],
    "HeavyFlameWeapon": ["Flame_Heavy"],
    "LightChemicalWeapon": ["Chemical_Light"],
    "MediumChemicalWeapon": ["Chemical_Medium"],
    "HeavyChemicalWeapon": ["Chemical_Heavy"],
    "NuclearWarhead": ["Nuclear_Super"],
}
RE_OLD_WH = re.compile(r"^(\s*)Warhead@(\w+?)((?:FriendlyFire|Percentage|ExtraDamage)?)\s*:\s*(\S*)\s*$")
RE_INHERITS_WH = re.compile(r"^\s*Inherits@wh\d?\s*:\s*\^Warhead_")
RE_TOPNAME = re.compile(r"^(\w+):\s*$")
RE_INHERITS_PLAIN = re.compile(r"^\s*Inherits(?:@(\w+))?\s*:\s*(\S+)")

def indent_of(s):
    return len(s) - len(s.lstrip("\t "))

def parse_file(path):
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines(keepends=True)
    nodes = []
    headers = []
    for idx, ln in enumerate(lines):
        raw = ln.rstrip("\r\n")
        if not raw.strip() or raw.lstrip().startswith("#"):
            continue
        if indent_of(raw) == 0 and RE_TOPNAME.match(raw):
            headers.append(idx)
    for h_i, start in enumerate(headers):
        end = headers[h_i + 1] if h_i + 1 < len(headers) else len(lines)
        name = RE_TOPNAME.match(lines[start].rstrip("\r\n")).group(1).strip()
        block = lines[start:end]
        parents = []
        has_new_wh = False
        old_keys_found = []
        new_keys_present = set()
        for j, ln in enumerate(block):
            raw = ln.rstrip("\r\n")
            mi = RE_INHERITS_PLAIN.match(raw)
            if mi:
                parent = mi.group(2)
                if not parent.startswith("^"):
                    parents.append(parent)
            if RE_INHERITS_WH.match(raw):
                has_new_wh = True
            stripped = raw.lstrip()
            m = RE_OLD_WH.match(raw)
            if m and m.group(2) in OLD_KEY_FAMILIES:
                old_keys_found.append((m.group(2), m.group(3), start + j, m.group(1), m.group(4)))
            m2 = re.match(r"^Warhead@(\w+?)((?:FriendlyFire|Percentage|ExtraDamage)?)\s*:", stripped)
            if m2:
                new_keys_present.add(m2.group(1))
        nodes.append({"name": name, "start": start, "end": end, "parents": parents,
                      "has_new_wh": has_new_wh, "old_keys": old_keys_found,
                      "new_keys_present": new_keys_present,
                      "file": str(path)})
    return lines, nodes

all_nodes = {}
file_data = {}
for path in FILES:
    if not path.exists():
        continue
    lines, nodes = parse_file(path)
    file_data[str(path)] = (lines, nodes)
    for nd in nodes:
        all_nodes.setdefault(nd["name"], []).append(nd)

converted = {nm for nm, lst in all_nodes.items() if any(n["has_new_wh"] for n in lst)}

def find_children(name, visited):
    result = set()
    for nm, lst in all_nodes.items():
        if nm in visited or nm == name:
            continue
        for nd in lst:
            if any(p == name for p in nd["parents"]):
                if nm not in visited:
                    result.add(nm)
                    visited.add(nm)
                    result |= find_children(nm, visited)
    return result

children_of = {cv: find_children(cv, {cv}) for cv in converted}

def ancestors_of(name, visited=None):
    if visited is None:
        visited = set()
    result = set()
    for nd in all_nodes.get(name, []):
        for p in nd["parents"]:
            if p in visited:
                continue
            visited.add(p)
            result.add(p)
            result |= ancestors_of(p, visited)
    return result

# Build edits: file -> {line_idx_to_replace: newtext, line_idx_to_delete: set}
repl = defaultdict(dict)
dels = defaultdict(set)
stats = defaultdict(int)
actions = []

for cv, kids in children_of.items():
    for kid in kids:
        for nd in all_nodes.get(kid, []):
            path = nd["file"]
            lines = file_data[path][0]
            for oldkey, suffix, idx, indent, wtype in nd["old_keys"]:
                chain = {cv} | {a for a in ancestors_of(kid) if a in converted}
                matches = set()
                for anc in chain:
                    for new_key in all_nodes[anc][0]["new_keys_present"]:
                        if new_key in OLD_KEY_FAMILIES[oldkey]:
                            matches.add(new_key)
                if len(matches) != 1:
                    continue  # skip ambiguous
                newkey = matches.pop()
                raw = lines[idx].rstrip("\r\n")
                eol = lines[idx][len(raw):]
                if suffix == "FriendlyFire":
                    # delete the entire block
                    base = indent_of(raw)
                    k = idx + 1
                    dels[path].add(idx)
                    while k < len(lines):
                        r2 = lines[k].rstrip("\r\n")
                        if not r2.strip():
                            dels[path].add(k)
                            k += 1
                            continue
                        if indent_of(r2) <= base:
                            break
                        dels[path].add(k)
                        k += 1
                    stats["delete_ff"] += 1
                    actions.append((Path(path).relative_to(MOD), idx + 1, kid, oldkey, newkey, "delete"))
                elif suffix == "Percentage":
                    repl[path][idx] = f"{indent}Warhead@{newkey}_Percentage: {wtype}{eol}"
                    stats["rename_pct"] += 1
                    actions.append((Path(path).relative_to(MOD), idx + 1, kid, oldkey, newkey, "rename_pct"))
                elif suffix == "ExtraDamage":
                    repl[path][idx] = f"{indent}Warhead@{newkey}_ExtraDamage: {wtype}{eol}"
                    stats["rename_extra"] += 1
                    actions.append((Path(path).relative_to(MOD), idx + 1, kid, oldkey, newkey, "rename_extra"))
                else:
                    repl[path][idx] = f"{indent}Warhead@{newkey}:{eol}"
                    stats["rename_main"] += 1
                    actions.append((Path(path).relative_to(MOD), idx + 1, kid, oldkey, newkey, "rename_main"))

apply = "--apply" in sys.argv
print(f"Multi-variant bug B -- {'APPLYING' if apply else 'DRY RUN'}")
print(f"  rename main: {stats['rename_main']}")
print(f"  rename pct:  {stats['rename_pct']}")
print(f"  rename extra: {stats['rename_extra']}")
print(f"  delete FF:   {stats['delete_ff']}")
print(f"  files:       {len(set(list(repl.keys()) + list(dels.keys())))}")
for rel, ln, kid, ok, nk, act in sorted(actions):
    print(f"  {rel}:{ln} {kid} {ok} -> {nk} ({act})")

if apply:
    for path in set(list(repl.keys()) + list(dels.keys())):
        lines = file_data[path][0]
        out = []
        for idx, ln in enumerate(lines):
            if idx in dels.get(path, set()):
                continue
            out.append(repl.get(path, {}).get(idx, ln))
        Path(path).write_text("".join(out), encoding="utf-8", newline="")
    print("\nAPPLIED.")
