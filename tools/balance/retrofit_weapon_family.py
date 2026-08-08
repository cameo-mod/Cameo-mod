#!/usr/bin/env python3
"""retrofit_weapon_family.py — the CANONICAL weapon 3-way-split retrofit
(docs/design/WEAPON_3WAY_SPLIT.md "CONVERSION RUNBOOK").

Converts SINGLE-inherit weapons/intermediates from an old full-stack warhead
template to the 3-inherit model (@wh + @proj + @fx), STRUCTURALLY:
  - only blocks whose ONLY old-warhead-template inherit is the target old
    template (mixed = 2+ old templates of ANY family are SKIPPED -> Phase B);
  - the old template DEFINITION block (^SmallArms:) is never touched;
  - PRESERVES every Damage verbatim — only Inherits lines + Warhead@ KEY names
    change (Warhead@<Old> / <Old>Percentage / <Old>FriendlyFire -> new key);
  - naming (docs/design/WEAPON_3WAY_SPLIT.md underscore-section law):
    @wh -> ^Warhead_<Family>_<Level>, @proj -> ^Projectile_<Fam>_<Lvl>,
    @fx -> ^Effect_<Fam>_<Lvl>; the warhead KEY keeps the bare profile name
    (Warhead@Bullet_Light), twin keys gain an underscore (Bullet_Light_Percentage).
  - BOM-safe (utf-8-sig); LF output.

The skipped-block REPAIR is resolution-based: a leftover `Warhead@T` / `-Warhead@T`
in a non-converted block is renamed to the new key ONLY when the block's resolved
parent chain no longer provides `Warhead@T` but does provide the new key (i.e. T
flowed through a now-converted intermediate). A `-Warhead@T` that still resolves
against an UNCONVERTED provider (e.g. a mixed double-base template like
^TSDefaultMissile) is left untouched — renaming it would orphan the removal.

Usage:
  retrofit_weapon_family.py --old SmallArms,Chaingun            # dry run (report)
  retrofit_weapon_family.py --old SmallArms,Chaingun --apply    # write
"""
from __future__ import annotations
import argparse, os, re, sys

ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "mods", "cameo")

# old template -> (warhead KEY base, projectile template|None, effect template)
TRIPLE = {
    "SmallArms": ("Bullet_Light", "Projectile_Bullet_Light", "Effect_Bullet_Light"),
    "Chaingun": ("Bullet_Medium", "Projectile_Bullet_Medium", "Effect_Bullet_Medium"),
    "TankDestroyerCannon": ("CannonAP_Light", "Projectile_Shell_Light", "Effect_Cannon_Light"),
    "MediumCannon": ("CannonHE_Medium", "Projectile_Shell_Medium", "Effect_Cannon_Medium"),
    "HeavyCannon": ("CannonHE_Heavy", "Projectile_Shell_Heavy", "Effect_Cannon_Heavy"),
    "LightMissile": ("MissileAP_Light", "Projectile_Missile_Light", "Effect_Missile_Light"),
    "MediumMissile": ("MissileAP_Medium", "Projectile_Missile_Medium", "Effect_Missile_Medium"),
    "HeavyMissile": ("MissileAP_Heavy", "Projectile_Missile_Heavy", "Effect_Missile_Heavy"),
    "FlakWeapon": ("Flak_Medium", "Projectile_Flak_Medium", "Effect_Flak_Medium"),
    "HeavyAAWeapon": ("MissileAA_Heavy", "Projectile_Flak_Heavy", "Effect_Flak_Heavy"),
    "LightFlameWeapon": ("Flame_Light", "Projectile_Flame_Light", "Effect_Flame_Light"),
    "MediumFlameWeapon": ("Flame_Medium", "Projectile_Flame_Medium", "Effect_Flame_Medium"),
    "HeavyFlameWeapon": ("Flame_Heavy", "Projectile_Flame_Heavy", "Effect_Flame_Heavy"),
    "LightChemicalWeapon": ("Chemical_Light", "Projectile_Chem_Light", "Effect_Chem_Light"),
    "MediumChemicalWeapon": ("Chemical_Medium", "Projectile_Chem_Medium", "Effect_Chem_Medium"),
    "HeavyChemicalWeapon": ("Chemical_Heavy", "Projectile_Chem_Heavy", "Effect_Chem_Heavy"),
    "Grenade": ("Demolition_Light", "Projectile_Grenade_Light", "Effect_Demolition_Light"),
    "ShrapnelWeapon": ("Concussion_Medium", None, "Effect_Concussion_Medium"),
    "HeavyBomb": ("Demolition_Heavy", None, "Effect_Demolition_Heavy"),
    "NuclearWarhead": ("Nuclear_Super", None, "Effect_Nuclear_Super"),
    "SwordWeapon": ("Melee_Medium", "Projectile_Melee_Medium", "Effect_Melee_Medium"),
    "ArrowWeapon": ("Arrow_Light", "Projectile_Arrow_Light", "Effect_Arrow_Light"),
    "MagicWeapon": ("Magic_Heavy", "Projectile_Magic_Heavy", "Effect_Magic_Heavy"),
    "LaserWeapon": ("Laser_Heavy", "Projectile_Laser_Heavy", "Effect_Laser_Heavy"),
    "RailgunWeapon": ("Railgun_Heavy", "Projectile_Railgun_Heavy", "Effect_Railgun_Heavy"),
    "TeslaWeapon": ("Tesla_Heavy", "Projectile_Lightning_Heavy", "Effect_Tesla_Heavy"),
    "TeslaChargedWeapon": ("TeslaCharged_Super", "Projectile_Lightning_Super", "Effect_Tesla_Super"),
    "SniperWeapon": ("Sniper_Light", "Projectile_Sniper_Light", "Effect_Sniper_Light"),
}
STAY = {"ToxicWeapon", "HealingWeapon", "RepairWeapon"}
ALL_OLD = set(TRIPLE) | STAY  # for mixed detection
# the NEW warhead templates count as warhead-carrying too, so mixed-detection is
# ORDER-INDEPENDENT: once family A is converted (a weapon now inherits ^Warhead_A),
# an A+B cross-family weapon is still seen as 2-warhead when family B runs -> Phase B.
NEW_WARHEADS = {"Warhead_" + t[0] for t in TRIPLE.values()}

TOP = re.compile(r"^(﻿?)(\^?[\w.]+):\s*$")
INH = re.compile(r"^(\t+)Inherits(@[\w.]+)?:\s*\^(\w+)\s*(?:#.*)?$")
INH_ANY = re.compile(r"^(\t+)Inherits(@[\w.]+)?:\s*\^?([\w.]+)\s*(?:#.*)?$")
SUFFIX = "(Percentage|FriendlyFire|ExtraDamage)"


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
    """Every warhead-CARRYING template = the bases + every INTERMEDIATE that
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
    carrying = set(ALL_OLD) | NEW_WARHEADS
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


def rename_key_line(line, old, wh):
    """rename `Warhead@<old>[suffix]:` / `-Warhead@<old>[suffix]:` -> new key,
    inserting an underscore before the twin suffix (Bullet_Light_Percentage)."""
    return re.sub(
        rf"^(\s*)(-?)Warhead@{re.escape(old)}{SUFFIX}?:",
        lambda m: f"{m.group(1)}{m.group(2)}Warhead@{wh}" + (f"_{m.group(3)}" if m.group(3) else "") + ":",
        line)


def build_provides(filelines):
    """resolved warhead-KEY set each template provides, over the POST-conversion
    graph. provides[t] = (union of parents' provides - own removals) | own defs."""
    own_defs, own_removes, parents = {}, {}, {}
    for _p, lines in filelines.items():
        for (s, e, name) in parse_blocks(lines):
            bare = name.lstrip("^")
            pd = own_defs.setdefault(bare, set())
            pr = own_removes.setdefault(bare, set())
            par = parents.setdefault(bare, [])
            for i in range(s, e):
                m = INH_ANY.match(lines[i])
                if m:
                    par.append(m.group(3))
                md = re.match(r"^\s*Warhead@([\w.]+):", lines[i])
                if md:
                    pd.add(md.group(1))
                mr = re.match(r"^\s*-Warhead@([\w.]+):", lines[i])
                if mr:
                    pr.add(mr.group(1))
    provides = {n: set() for n in own_defs}
    for _ in range(64):  # fixpoint (inheritance depth is small)
        changed = False
        for n in own_defs:
            acc = set()
            for q in parents.get(n, []):
                acc |= provides.get(q, set())
            acc = (acc - own_removes[n]) | own_defs[n]
            if acc != provides[n]:
                provides[n] = acc
                changed = True
        if not changed:
            break
    return provides, parents


def retrofit(targets, apply):
    carrying = build_wh_closure()
    files = [os.path.join(dp, fn) for dp, _, fs in os.walk(ROOT)
             for fn in fs if fn.endswith(".yaml")]
    originals = {p: open(p, encoding="utf-8-sig").read() for p in files}
    filelines = {p: originals[p].split("\n") for p in files}
    converted_names = set()
    per_old = {t: 0 for t in targets}
    converted = 0
    skipped_mixed = 0

    # ---- PASS 1: convert single-inherit blocks (in-memory) ----
    for p in files:
        lines = filelines[p]
        blocks = parse_blocks(lines)
        edits = {}  # lineidx -> replacement list (Inherits expansion)
        for (s, e, name) in blocks:
            bare = name.lstrip("^")
            if name.startswith("^") and bare in ALL_OLD:
                continue  # never touch an old base template DEFINITION block
            whs = wh_inherits(lines, s, e, carrying)
            if len(whs) == 1 and whs[0][1] in targets:
                idx, old, indent = whs[0]
                wh, proj, fx = TRIPLE[old]
                repl = [f"{indent}Inherits@wh: ^Warhead_{wh}"]
                if proj:
                    repl.append(f"{indent}Inherits@proj: ^{proj}")
                repl.append(f"{indent}Inherits@fx: ^{fx}")
                edits[idx] = repl
                for i in range(s, e):
                    lines[i] = rename_key_line(lines[i], old, wh)
                per_old[old] += 1
                converted += 1
                converted_names.add(bare)
            elif len(whs) >= 2 and any(o[1] in targets for o in whs):
                skipped_mixed += 1
        if edits:
            out = []
            for i, ln in enumerate(lines):
                out.append("\n".join(edits[i]) if i in edits else ln)
            filelines[p] = "\n".join(out).split("\n")

    # ---- PASS 2 repair, iterated to a FIXPOINT ----
    # provides must be rebuilt after each sweep: renaming an intermediate's own key
    # changes what its grandchildren see (a one-shot pass leaves grandchildren with a
    # stale old key that becomes a stray second warhead). Loop until no rename fires.
    repaired = 0
    while True:
        provides, parents = build_provides(filelines)
        pass_fixes = 0
        for p in files:
            lines = filelines[p]
            for (s, e, name) in parse_blocks(lines):
                bare = name.lstrip("^")
                if bare in converted_names:
                    continue  # own keys already renamed in pass 1
                if name.startswith("^") and bare in ALL_OLD:
                    continue
                parprov = set()
                for q in parents.get(bare, []):
                    parprov |= provides.get(q, set())
                for T in targets:
                    wh = TRIPLE[T][0]
                    for i in range(s, e):
                        m = re.match(rf"^(\s*)(-?)Warhead@{re.escape(T)}{SUFFIX}?:(.*)$", lines[i])
                        if not m:
                            continue
                        suf = m.group(3) or ""
                        oldkey = T + suf
                        newkey = wh + ("_" + suf if suf else "")
                        # rename ONLY if the parent chain lost the old key but gained the new one
                        if oldkey not in parprov and newkey in parprov:
                            lines[i] = f"{m.group(1)}{m.group(2)}Warhead@{newkey}:{m.group(4)}"
                            pass_fixes += 1
        repaired += pass_fixes
        if pass_fixes == 0:
            break

    changed_files = 0
    for p in files:
        newtext = "\n".join(filelines[p])
        if newtext != originals[p]:
            changed_files += 1
            if apply:
                open(p, "w", encoding="utf-8", newline="\n").write(newtext)

    mode = "APPLIED" if apply else "DRY RUN"
    print(f"[{mode}] converted {converted} single-inherit blocks across {changed_files} files")
    for t in targets:
        print(f"    ^{t} -> ^Warhead_{TRIPLE[t][0]}: {per_old[t]}")
    print(f"    skipped (mixed, 2+ warhead templates incl. a target): {skipped_mixed} -> Phase B")
    print(f"    resolution-based key repairs in skipped/child blocks: {repaired}")
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
