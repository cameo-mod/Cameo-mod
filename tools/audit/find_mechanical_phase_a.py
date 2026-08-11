#!/usr/bin/env python3
"""find_mechanical_phase_a.py — clean Phase A candidate list.

Phase A = concrete weapons with exactly ONE old full-stack family Inherits,
no new ^Warhead_* inherits, no new-style damage warheads, and no blocked
energy/magic/nuclear families. Output is a JSON and MD report in
docs/audit/latest/.

The new-family suggestion is taken from the cluster-convert skill mapping.
Any 'needs_confirm' entry requires maintainer sign-off before conversion.
"""
from __future__ import annotations

import json
import re
import sys
from collections import defaultdict
from pathlib import Path

MOD = Path(__file__).resolve().parents[2] / "mods" / "cameo"
OUT_DIR = Path(__file__).resolve().parents[2] / "docs" / "audit" / "latest"

CENTRAL = ["weapons/weapons.yaml", "weapons/redalert2.yaml",
           "weapons/redalert2mod.yaml", "weapons/tiberiansun.yaml",
           "weapons/tiberiandawn.yaml", "weapons/warcraft2.yaml",
           "weapons/missiles.yaml"]
FILES = [MOD / p for p in CENTRAL] + sorted((MOD / "ContentPacks").glob("*/*/yaml/weapons.yaml"))

OLD_FAMILIES = {
    "^SmallArms", "^Chaingun", "^TankDestroyerCannon", "^MediumCannon",
    "^HeavyCannon", "^LightMissile", "^MediumMissile", "^HeavyMissile",
    "^FlakWeapon", "^HeavyAAWeapon", "^Grenade", "^ShrapnelWeapon",
    "^HeavyBomb", "^LaserWeapon", "^RailgunWeapon", "^TeslaWeapon",
    "^TeslaChargedWeapon", "^SwordWeapon", "^ArrowWeapon", "^MagicWeapon",
    "^LightFlameWeapon", "^MediumFlameWeapon", "^HeavyFlameWeapon",
    "^LightChemicalWeapon", "^MediumChemicalWeapon", "^HeavyChemicalWeapon",
    "^NuclearWarhead", "^SniperWeapon", "^LightArms",
}

# old family -> suggested new warhead family (from cluster-convert skill)
FAMILY_MAP = {
    "^SmallArms": ("Bullet_Light", False),
    "^Chaingun": ("Bullet_Medium", False),
    "^Grenade": ("Demolition_Light", False),
    "^ShrapnelWeapon": ("Concussion_Medium", False),
    "^HeavyBomb": ("Demolition_Heavy", False),
    "^MediumCannon": ("CannonHE_Medium", False),
    "^HeavyCannon": ("CannonHE_Heavy", False),
    "^TankDestroyerCannon": ("CannonAP_Light", False),
    "^LightMissile": ("MissileAP_Light", True),
    "^MediumMissile": ("MissileAP_Medium", True),
    "^HeavyMissile": ("MissileHE_Heavy", True),
    "^FlakWeapon": ("Flak_Medium", False),
    "^HeavyAAWeapon": ("Flak_Heavy", False),
    "^LightFlameWeapon": ("Flame_Light", False),
    "^MediumFlameWeapon": ("Flame_Medium", False),
    "^HeavyFlameWeapon": ("Flame_Heavy", False),
    "^LightChemicalWeapon": ("Chemical_Light", False),
    "^MediumChemicalWeapon": ("Chemical_Medium", False),
    "^HeavyChemicalWeapon": ("Chemical_Heavy", False),
    "^SniperWeapon": ("Sniper_Light", True),
    # energy / magic / nuclear / melee — blocked, need per-weapon order
    "^LaserWeapon": (None, True),
    "^RailgunWeapon": (None, True),
    "^TeslaWeapon": (None, True),
    "^TeslaChargedWeapon": (None, True),
    "^MagicWeapon": (None, True),
    "^SwordWeapon": (None, True),
    "^ArrowWeapon": ("MissileAP_Light", True),
    "^NuclearWarhead": (None, True),
    "^LightArms": ("Bullet_Light", True),
}

# warhead types that are actual damage warheads (exclude effects/EMP/conditions)
DAMAGE_TYPES = {"SpreadDamage", "AreaDamage", "HealthPercentageDamage",
                "AreaDamagePercentage", "TargetDamage"}

RE_INHERITS = re.compile(r"^\s*Inherits(?:@\w+)?\s*:\s*(\w+)")
RE_WARHEAD = re.compile(r"^\s*Warhead@(\w+)\s*:\s*(\S*)")
RE_DAMAGE = re.compile(r"^\s*Damage\s*:\s*(\d+)")


def indent_of(s):
    return len(s) - len(s.lstrip("\t "))


def parse_file(path):
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines()
    # top-level weapons (skip lines that are comments or templates? include ^ for other tools)
    headers = [i for i, ln in enumerate(lines) if indent_of(ln) == 0 and
               re.match(r"^(\w+)\s*:\s*$", ln.rstrip("\r\n"))]
    nodes = []
    for h_i, start in enumerate(headers):
        end = headers[h_i + 1] if h_i + 1 < len(headers) else len(lines)
        name = re.match(r"^(\w+)\s*:\s*$", lines[start].rstrip("\r\n")).group(1).strip()
        if name.startswith("^"):
            continue
        block = lines[start:end]
        old_inherits = set()
        has_new_wh = False
        new_style_warheads = []  # (tag, type)
        old_style_warheads = []  # (tag, type, damage)
        cur_wh = None
        cur_type = ""
        cur_dmg = None
        concrete_parent = False
        for ln in block:
            raw = ln.rstrip("\r\n")
            m_inh = RE_INHERITS.match(raw)
            if m_inh:
                val = m_inh.group(1)
                if val in OLD_FAMILIES:
                    old_inherits.add(val)
                if re.match(r"^\^Warhead_", val):
                    has_new_wh = True
                # inheriting from another concrete weapon means this is a child variant,
                # not a top-level Phase A candidate; the parent should be converted instead.
                if not val.startswith("^"):
                    concrete_parent = True
            m_w = RE_WARHEAD.match(raw)
            if m_w:
                if cur_wh is not None:
                    (new_style_warheads if cur_wh.count("_") > 0
                     else old_style_warheads).append((cur_wh, cur_type, cur_dmg))
                cur_wh = m_w.group(1)
                cur_type = m_w.group(2)
                cur_dmg = None
            if cur_wh is not None and RE_DAMAGE.match(raw):
                cur_dmg = int(RE_DAMAGE.match(raw).group(1))
        if cur_wh is not None:
            (new_style_warheads if cur_wh.count("_") > 0
             else old_style_warheads).append((cur_wh, cur_type, cur_dmg))
        if concrete_parent:
            continue
        nodes.append({
            "name": name,
            "file": str(path.relative_to(MOD)),
            "old_inherits": old_inherits,
            "has_new_wh": has_new_wh,
            "old_style_warheads": old_style_warheads,
            "new_style_warheads": new_style_warheads,
        })
    return nodes


def main():
    weapons = []
    for path in FILES:
        if not path.exists():
            continue
        weapons.extend(parse_file(path))

    groups = defaultdict(list)
    for w in weapons:
        if w["old_inherits"]:
            groups[tuple(sorted(w["old_inherits"]))].append(w)

    candidates = []
    for w in weapons:
        if len(w["old_inherits"]) != 1:
            continue
        if w["has_new_wh"]:
            continue
        if w["new_style_warheads"]:
            continue
        old = next(iter(w["old_inherits"]))
        if old not in FAMILY_MAP:
            continue
        new_family, needs_confirm = FAMILY_MAP[old]
        main_dmg = None
        for tag, typ, dmg in w["old_style_warheads"]:
            if dmg is not None and typ in DAMAGE_TYPES and "FriendlyFire" not in tag:
                main_dmg = dmg
                break
        candidates.append({
            "weapon": w["name"],
            "file": w["file"],
            "old_family": old,
            "suggested_new_family": new_family,
            "needs_confirm": needs_confirm,
            "main_damage": main_dmg,
            "old_warheads": [tag for tag, _, _ in w["old_style_warheads"]],
        })

    candidates.sort(key=lambda x: (x["file"], x["weapon"]))

    # write JSON
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    jpath = OUT_DIR / "find_mechanical_phase_a.json"
    jpath.write_text(json.dumps(candidates, indent=2, sort_keys=True), encoding="utf-8")

    # write MD
    mpath = OUT_DIR / "find_mechanical_phase_a.md"
    lines = ["# Mechanical Phase A candidates (clean list)", ""]
    lines.append(f"Total clean Phase A candidates: {len(candidates)}")
    lines.append("")
    for c in candidates:
        confirm = " (needs maintainer confirm)" if c["needs_confirm"] else ""
        md = f"- `{c['weapon']}` in `{c['file']}` | old: {c['old_family']}"
        if c["suggested_new_family"]:
            md += f" → `^Warhead_{c['suggested_new_family']}`"
        md += f" | Damage: {c['main_damage']}{confirm}"
        lines.append(md)
    mpath.write_text("\n".join(lines), encoding="utf-8")

    print(f"Wrote {jpath} and {mpath}")
    print(f"Total clean Phase A candidates: {len(candidates)}")
    print(f"  ready to convert (needs_confirm=False): {sum(1 for c in candidates if not c['needs_confirm'])}")
    print(f"  needs confirm (needs_confirm=True): {sum(1 for c in candidates if c['needs_confirm'])}")


if __name__ == "__main__":
    main()
