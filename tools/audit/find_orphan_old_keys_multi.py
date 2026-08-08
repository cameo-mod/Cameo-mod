"""Detect bug B (multi-variant refinement): children of converted weapons
that keep OLD warhead keys where the converted parent has a matching
NEW key in the same family.

Some old keys collapsed into multiple new templates (e.g. LightMissile
-> MissileAP_Light, MissileHE_Light, MissileAA_Light, etc.). The
one-to-one map in fix_orphan_old_keys.py is too rigid for these. This
script finds cases where the child's old key logically shadows a new
key that the converted parent actually has (e.g. parent has
Warhead@MissileHE_Light, child still has Warhead@LightMissile). Those
are additional double-fire bugs that need manual resolution.

Prints every case with recommended action."""
import re
from pathlib import Path

MOD = Path(__file__).resolve().parents[2] / "mods" / "cameo"
CENTRAL = ["weapons/weapons.yaml", "weapons/redalert2.yaml",
           "weapons/redalert2mod.yaml", "weapons/tiberiansun.yaml",
           "weapons/tiberiandawn.yaml", "weapons/warcraft2.yaml",
           "weapons/missiles.yaml"]
FILES = [MOD / p for p in CENTRAL] + sorted((MOD / "ContentPacks").glob("*/*/yaml/weapons.yaml"))

# old key -> tuple of possible new key bases (the new template might be any of these)
OLD_KEY_FAMILIES = {
    "SmallArms": ["Bullet_Light"],
    "Chaingun": ["Bullet_Medium"],
    "TankDestroyerCannon": ["CannonAP_Light"],
    "MediumCannon": ["CannonHE_Medium"],
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
                old_keys_found.append((m.group(2), m.group(3), start + j + 1, raw.strip()))
            m2 = re.match(r"^Warhead@(\w+?)((?:FriendlyFire|Percentage|ExtraDamage)?)\s*:", stripped)
            if m2:
                new_keys_present.add(m2.group(1))
        nodes.append({"name": name, "parents": parents, "has_new_wh": has_new_wh,
                      "old_keys": old_keys_found, "new_keys_present": new_keys_present,
                      "file": str(path.relative_to(MOD))})
    return nodes

all_nodes = {}
for path in FILES:
    if not path.exists():
        continue
    for nd in parse_file(path):
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

suspicious = []
for cv, kids in children_of.items():
    for kid in kids:
        for nd in all_nodes.get(kid, []):
            for oldkey, suffix, ln, txt in nd["old_keys"]:
                chain = {cv} | {a for a in ancestors_of(kid) if a in converted}
                matches = []
                for anc in chain:
                    for new_key in all_nodes[anc][0]["new_keys_present"]:
                        if new_key in OLD_KEY_FAMILIES[oldkey]:
                            matches.append((anc, new_key))
                if matches:
                    suspicious.append((nd["file"], ln, kid, oldkey, suffix, matches, txt))

print(f"Suspicious child old keys (parent has a same-family new key): {len(suspicious)}")
for rel, ln, kid, ok, suf, matches, txt in sorted(suspicious):
    rec = ", ".join(f"{anc} has {nk}" for anc, nk in matches)
    if suf == "":
        suffix = "(main; remove SpreadDamage and rename)"
    elif suf == "Percentage":
        suffix = "(percentage; rename)"
    elif suf == "FriendlyFire":
        suffix = "(FF twin; consider deleting)"
    elif suf == "ExtraDamage":
        suffix = "(ExtraDamage; rename)"
    print(f"  {rel}:{ln}  {kid}  {txt} -> {suffix}; {rec}")
