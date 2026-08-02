#!/usr/bin/env python3
"""retrofit_weapon_family.py — the CANONICAL weapon 3-way-split retrofit
(docs/design/WEAPON_3WAY_SPLIT.md "CONVERSION RUNBOOK").

Converts SINGLE-inherit weapons/intermediates from an old full-stack warhead
template to the 3-inherit model (@wh + @proj + @fx), STRUCTURALLY:
  - only blocks whose ONLY old-warhead-template inherit is the target old
    template (mixed = 2+ old templates of ANY family are SKIPPED → Phase B);
  - the old template DEFINITION block (^SmallArms:) is never touched;
  - PRESERVES every Damage verbatim — only Inherits lines + Warhead@ KEY names
    change (Warhead@<Old> / <Old>Percentage / <Old>FriendlyFire → new key);
  - BOM-safe (utf-8-sig); LF output.

Usage:
  retrofit_weapon_family.py --old SmallArms,Chaingun            # dry run (report)
  retrofit_weapon_family.py --old SmallArms,Chaingun --apply    # write
"""
from __future__ import annotations
import argparse, os, re, sys

ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "mods", "cameo")

# old template -> (warhead, projectile|None, effect)
TRIPLE = {
    "SmallArms": ("Bullet_Light", "ProjectileBullet_Light", "EffectBullet_Light"),
    "Chaingun": ("Bullet_Medium", "ProjectileBullet_Medium", "EffectBullet_Medium"),
    "TankDestroyerCannon": ("CannonAP_Light", "ProjectileShell_Light", "EffectCannon_Light"),
    "MediumCannon": ("CannonHE_Medium", "ProjectileShell_Medium", "EffectCannon_Medium"),
    "HeavyCannon": ("CannonHE_Heavy", "ProjectileShell_Heavy", "EffectCannon_Heavy"),
    "LightMissile": ("MissileAP_Light", "ProjectileMissile_Light", "EffectMissile_Light"),
    "MediumMissile": ("MissileAP_Medium", "ProjectileMissile_Medium", "EffectMissile_Medium"),
    "HeavyMissile": ("MissileAP_Heavy", "ProjectileMissile_Heavy", "EffectMissile_Heavy"),
    "FlakWeapon": ("Flak_Medium", "ProjectileFlak_Medium", "EffectFlak_Medium"),
    "HeavyAAWeapon": ("MissileAA_Heavy", "ProjectileFlak_Heavy", "EffectFlak_Heavy"),
    "LightFlameWeapon": ("Flame_Light", "ProjectileFlame_Light", "EffectFlame_Light"),
    "MediumFlameWeapon": ("Flame_Medium", "ProjectileFlame_Medium", "EffectFlame_Medium"),
    "HeavyFlameWeapon": ("Flame_Heavy", "ProjectileFlame_Heavy", "EffectFlame_Heavy"),
    "LightChemicalWeapon": ("Chemical_Light", "ProjectileChem_Light", "EffectChem_Light"),
    "MediumChemicalWeapon": ("Chemical_Medium", "ProjectileChem_Medium", "EffectChem_Medium"),
    "HeavyChemicalWeapon": ("Chemical_Heavy", "ProjectileChem_Heavy", "EffectChem_Heavy"),
    "Grenade": ("Demolition_Light", "ProjectileGrenade_Light", "EffectExplosion_Light"),
    "ShrapnelWeapon": ("Concussion_Medium", None, "EffectExplosion_Medium"),
    "HeavyBomb": ("Demolition_Heavy", None, "EffectExplosion_Heavy"),
    "NuclearWarhead": ("Nuclear_Super", None, "EffectNuclear_Super"),
    "SwordWeapon": ("Melee_Medium", "ProjectileMelee_Medium", "EffectMelee_Medium"),
    "ArrowWeapon": ("Arrow_Light", "ProjectileArrow_Light", "EffectArrow_Light"),
    "MagicWeapon": ("Magic_Heavy", "ProjectileMagic_Heavy", "EffectMagic_Heavy"),
    "LaserWeapon": ("Laser_Heavy", "ProjectileLaser_Heavy", "EffectLaser_Heavy"),
    "RailgunWeapon": ("Railgun_Heavy", "ProjectileRailgun_Heavy", "EffectRailgun_Heavy"),
    "TeslaWeapon": ("Tesla_Heavy", "ProjectileLightning_Heavy", "EffectTesla_Heavy"),
    "TeslaChargedWeapon": ("TeslaCharged_Super", "ProjectileLightning_Super", "EffectTesla_Super"),
}
STAY = {"SniperWeapon", "ToxicWeapon", "HealingWeapon", "RepairWeapon"}
ALL_OLD = set(TRIPLE) | STAY  # for mixed detection

TOP = re.compile(r"^(﻿?)(\^?[\w.]+):\s*$")
INH = re.compile(r"^(\t+)Inherits(@[\w.]+)?:\s*\^(\w+)\s*(?:#.*)?$")


def parse_blocks(lines):
    """-> list of (start_idx, end_idx_exclusive, keyname)."""
    idxs = []
    for i, ln in enumerate(lines):
        m = TOP.match(ln)
        if m and not ln.startswith((" ", "\t")):
            idxs.append((i, m.group(2)))
    blocks = []
    for j, (i, name) in enumerate(idxs):
        end = idxs[j + 1][0] if j + 1 < len(idxs) else len(lines)
        blocks.append((i, end, name))
    return blocks


def build_wh_closure():
    """Every warhead-CARRYING template = the 30 bases + every INTERMEDIATE that
    inherits one (transitively). Needed so a base+intermediate mix (e.g.
    ^SmallArms + ^RA2Chaingun) is detected as MIXED, not single. Returns the set
    of bare template names."""
    tmpl_parents = {}  # bare name -> set of bare parent names
    for dp, _, fs in os.walk(ROOT):
        for fn in fs:
            if not fn.endswith(".yaml"):
                continue
            lines = open(os.path.join(dp, fn), encoding="utf-8-sig").read().split("\n")
            for (s, e, name) in parse_blocks(lines):
                if not name.startswith("^"):
                    continue
                bare = name.lstrip("^")
                pars = set()
                for i in range(s, e):
                    m = INH.match(lines[i])
                    if m:
                        pars.add(m.group(3))
                tmpl_parents.setdefault(bare, set()).update(pars)
    carrying = set(ALL_OLD)
    changed = True
    while changed:
        changed = False
        for t, pars in tmpl_parents.items():
            if t not in carrying and (pars & carrying):
                carrying.add(t)
                changed = True
    return carrying


def wh_inherits(lines, s, e, carrying):
    """direct warhead-carrying-template inherits in a block -> [(idx, name, indent)]."""
    out = []
    for i in range(s, e):
        m = INH.match(lines[i])
        if m and m.group(3) in carrying:
            out.append((i, m.group(3), m.group(1)))
    return out


def retrofit(targets, apply):
    carrying = build_wh_closure()
    changed_files = 0
    converted = 0
    per_old = {t: 0 for t in targets}
    skipped_mixed = 0
    for dp, _, fs in os.walk(ROOT):
        for fn in fs:
            if not fn.endswith(".yaml"):
                continue
            p = os.path.join(dp, fn)
            raw = open(p, encoding="utf-8-sig").read()  # BOM-safe
            lines = raw.split("\n")
            blocks = parse_blocks(lines)
            edits = {}  # lineidx -> replacement list (or [] to delete); key-rename via in-place
            file_touched = False
            for (s, e, name) in blocks:
                bare = name.lstrip("^")
                if name.startswith("^") and bare in ALL_OLD:
                    continue  # never touch an old base template DEFINITION block
                whs = wh_inherits(lines, s, e, carrying)
                if len(whs) != 1:
                    if len(whs) >= 2 and any(o[1] in targets for o in whs):
                        skipped_mixed += 1
                    continue
                idx, old, indent = whs[0]
                if old not in targets:  # single, but an intermediate/other-family base → skip
                    continue
                wh, proj, fx = TRIPLE[old]
                repl = [f"{indent}Inherits@wh: ^{wh}"]
                if proj:
                    repl.append(f"{indent}Inherits@proj: ^{proj}")
                repl.append(f"{indent}Inherits@fx: ^{fx}")
                edits[idx] = repl
                # rename warhead override KEYS within this block (preserve values)
                for i in range(s, e):
                    lines[i] = re.sub(rf"^(\s*)Warhead@{old}(Percentage|FriendlyFire)?:",
                                      rf"\1Warhead@{wh}\2:", lines[i])
                per_old[old] += 1
                converted += 1
                file_touched = True
            if edits:
                out = []
                for i, ln in enumerate(lines):
                    out.append("\n".join(edits[i]) if i in edits else ln)
                newtext = "\n".join(out)
                if apply:
                    open(p, "w", encoding="utf-8", newline="\n").write(newtext)
                changed_files += 1
    mode = "APPLIED" if apply else "DRY RUN"
    print(f"[{mode}] converted {converted} single-inherit blocks across {changed_files} files")
    for t in targets:
        print(f"    ^{t} -> ^{TRIPLE[t][0]}: {per_old[t]}")
    print(f"    skipped (mixed, 2+ warhead templates incl. a target): {skipped_mixed} -> Phase B")
    print(f"    warhead-carrying templates in closure: {len(carrying)} (bases + intermediates)")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--old", required=True, help="comma-separated old template name(s), e.g. SmallArms,Chaingun")
    ap.add_argument("--apply", action="store_true")
    a = ap.parse_args()
    tg = [x.strip() for x in a.old.split(",") if x.strip()]
    bad = [t for t in tg if t not in TRIPLE]
    if bad:
        sys.exit(f"unknown/non-convertible old template(s): {bad}")
    retrofit(tg, a.apply)
