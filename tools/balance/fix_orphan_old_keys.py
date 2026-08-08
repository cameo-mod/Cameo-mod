"""Fix bug B: rename orphaned old warhead keys in children of converted
weapons to the new key names, and delete orphaned FriendlyFire twins
(FF is now baked into the new template mains).

For each bug-B candidate (child of a converted weapon with an orphaned
old warhead key):
- Warhead@<OldKey>: SpreadDamage  ->  Warhead@<NewKey>:  (bare, inherits AreaDamage)
- Warhead@<OldKey>Percentage:     ->  Warhead@<NewKey>_Percentage:
- Warhead@<OldKey>FriendlyFire:   ->  DELETE the entire block (FF baked in)
- Warhead@<OldKey>ExtraDamage:    ->  Warhead@<NewKey>_ExtraDamage:

Damage values and other fields are PRESERVED verbatim."""
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

OLD_TO_NEW = {
    "SmallArms": "Bullet_Light", "Chaingun": "Bullet_Medium",
    "TankDestroyerCannon": "CannonAP_Light", "MediumCannon": "CannonHE_Medium",
    "HeavyCannon": "CannonHE_Heavy", "LightMissile": "MissileAP_Light",
    "MediumMissile": "MissileAP_Medium", "HeavyMissile": "MissileAP_Heavy",
    "FlakWeapon": "Flak_Medium", "HeavyAAWeapon": "MissileAA_Heavy",
    "Grenade": "Demolition_Light", "ShrapnelWeapon": "Concussion_Medium",
    "HeavyBomb": "Demolition_Heavy", "LaserWeapon": "Laser_Heavy",
    "RailgunWeapon": "Railgun_Heavy", "TeslaWeapon": "Tesla_Heavy",
    "TeslaChargedWeapon": "TeslaCharged_Super", "SwordWeapon": "Melee_Medium",
    "ArrowWeapon": "Arrow_Light", "MagicWeapon": "Magic_Heavy",
    "LightFlameWeapon": "Flame_Light", "MediumFlameWeapon": "Flame_Medium",
    "HeavyFlameWeapon": "Flame_Heavy", "LightChemicalWeapon": "Chemical_Light",
    "MediumChemicalWeapon": "Chemical_Medium", "HeavyChemicalWeapon": "Chemical_Heavy",
    "NuclearWarhead": "Nuclear_Super",
}
OLD_KEYS = set(OLD_TO_NEW)
RE_OLD_WH = re.compile(r"^(\s*)Warhead@(\w+?)((?:FriendlyFire|Percentage|ExtraDamage)?)\s*:\s*(\S*)\s*$")
RE_NEW_WH = re.compile(r"^Warhead@(\w+?)((?:FriendlyFire|Percentage|ExtraDamage)?)\s*:")
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
            if m and m.group(2) in OLD_KEYS:
                old_keys_found.append((m.group(2), m.group(3), start + j))
            m2 = RE_NEW_WH.match(stripped)
            if m2:
                new_keys_present.add(m2.group(1))
        nodes.append({"name": name, "start": start, "end": end, "parents": parents,
                      "has_new_wh": has_new_wh, "old_keys": old_keys_found,
                      "new_keys_present": new_keys_present,
                      "file": str(path.relative_to(MOD))})
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
stats = {"renamed_main": 0, "renamed_pct": 0, "renamed_extra": 0, "deleted_ff": 0}

for cv, kids in children_of.items():
    for kid in kids:
        for nd in all_nodes.get(kid, []):
            path_key = None
            for p in FILES:
                if str(p) in file_data and nd in file_data[str(p)][1]:
                    path_key = str(p)
                    break
            if not path_key:
                continue
            lines = file_data[path_key][0]
            for oldkey, suffix, abs_line in nd["old_keys"]:
                newkey = OLD_TO_NEW[oldkey]
                chain = {cv} | {a for a in ancestors_of(kid) if a in converted}
                has_new = any(newkey in all_nodes[a][0]["new_keys_present"] for a in chain if all_nodes.get(a))
                if not has_new:
                    continue  # false positive
                idx = abs_line  # 0-based line index
                raw = lines[idx].rstrip("\r\n")
                eol = lines[idx][len(raw):]
                m = RE_OLD_WH.match(raw)
                if not m:
                    continue
                indent, ok, suf, wtype = m.group(1), m.group(2), m.group(3), m.group(4)
                if suf == "FriendlyFire":
                    # delete the entire block (header + indented children)
                    base = indent_of(raw)
                    dels[path_key].add(idx)
                    k = idx + 1
                    while k < len(lines):
                        r2 = lines[k].rstrip("\r\n")
                        if not r2.strip():
                            dels[path_key].add(k)
                            k += 1
                            continue
                        if indent_of(r2) <= base:
                            break
                        dels[path_key].add(k)
                        k += 1
                    stats["deleted_ff"] += 1
                elif suf == "Percentage":
                    new_name = f"Warhead@{newkey}_Percentage"
                    repl[path_key][idx] = f"{indent}{new_name}:{(' ' + wtype) if wtype else ''}{eol}"
                    stats["renamed_pct"] += 1
                elif suf == "ExtraDamage":
                    new_name = f"Warhead@{newkey}_ExtraDamage"
                    repl[path_key][idx] = f"{indent}{new_name}:{(' ' + wtype) if wtype else ''}{eol}"
                    stats["renamed_extra"] += 1
                else:
                    # main: rename + strip SpreadDamage type (bug A fix too)
                    new_name = f"Warhead@{newkey}"
                    repl[path_key][idx] = f"{indent}{new_name}:{eol}"
                    stats["renamed_main"] += 1

apply = "--apply" in sys.argv
print(f"Bug B fix -- {'APPLYING' if apply else 'DRY RUN'}")
print(f"  renamed main warheads:    {stats['renamed_main']}")
print(f"  renamed percentage:       {stats['renamed_pct']}")
print(f"  renamed extra damage:     {stats['renamed_extra']}")
print(f"  deleted FF twin blocks:   {stats['deleted_ff']}")
print(f"  files affected:           {len(set(list(repl.keys()) + list(dels.keys())))}")

if apply:
    for path_key in set(list(repl.keys()) + list(dels.keys())):
        path = Path(path_key)
        lines = file_data[path_key][0]
        out = []
        for idx, ln in enumerate(lines):
            if idx in dels.get(path_key, set()):
                continue
            out.append(repl.get(path_key, {}).get(idx, ln))
        path.write_text("".join(out), encoding="utf-8", newline="")
    print("\nAPPLIED.")
