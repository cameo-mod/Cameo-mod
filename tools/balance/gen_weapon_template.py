#!/usr/bin/env python3
"""gen_weapon_template.py — generate weapon-class template families.

THE TWO-LEVEL ORDERING LAW (maintainer 2026-08-01 — the most important part):
A weapon's Versus ORDER is built, never hand-typed, from two decisions:

  1. MACRO-TYPE PRIORITY — which unit TYPE it is strong against, best->worst:
     Infantry / Vehicle / Building / Aircraft. Types may be TIED (combined,
     interleaved) when a weapon is equally good vs several.
  2. LIGHT<->HEAVY DIRECTION — within every type, is it better vs LIGHT or
     HEAVY armor? Applied to the armor SUB-LADDERS (lightest -> heaviest):
        Infantry : None < Flak < Plate < Heroic
        Vehicle  : Scout < Light < Medium < Heavy < Superheavy
        Building : Wood < Steel < Concrete
        Aircraft : Fighter < Bomber < Helicopter < Spaceship
     anti-LIGHT weapons (HE, flame, bullets) hit None > ... > Heroic;
     anti-HEAVY weapons (AP, tesla, railgun) hit Heroic > ... > None.

The 16-armor order = concatenate the macro blocks in priority order; inside
each block list the sub-ladder in the chosen direction; interleave tied blocks
round-robin. Then LEVEL (Light/Medium/Heavy = step 6/5/4 = WC 0.75/1.0/1.25)
only changes the falloff slope — ONE order per weapon TYPE, shared by L/M/H.

Naming: UNIFIED `^<Family>_<Level>` (family-first, underscore).
Usage:  gen_weapon_template.py [family ...] | --list | --orders
"""
from __future__ import annotations
import sys

LADDERS = {  # lightest -> heaviest
    "INF": ["None", "Flak", "Plate", "Heroic"],
    "VEH": ["Scout", "Light", "Medium", "Heavy", "Superheavy"],
    "BLD": ["Wood", "Steel", "Concrete"],
    "AIR": ["Fighter", "Bomber", "Helicopter", "Spaceship"],
}
CANON16 = {a for arms in LADDERS.values() for a in arms}
# Level = step (falloff slope) = WeaponClass. Super (step 3, floor 55, WC 1.5, Shield 155)
# is the superweapon band for Nuclear + charged Tesla — one notch above Heavy (maintainer 2026-08-02).
LEVELS = {"Light": (6, 10, 16), "Medium": (5, 25, 20), "Heavy": (4, 40, 25), "Super": (3, 55, 30)}
WC = {"Light": 0.75, "Medium": 1.0, "Heavy": 1.25, "Super": 1.5}
# FLAT / "ignores armor" (Sonic): flat SpreadDamage, same value vs every armor. Tunable.
FLAT_VALUES = {"Light": 45, "Medium": 55, "Heavy": 65}   # main SpreadDamage vs ALL armors
FLAT_PCT = {"Light": 5, "Medium": 8, "Heavy": 10}        # its modest % chip
# PCT / "%-equalizer" (Magic): tiny flat + a LARGE uniform % of max HP (ignores armor) = giant-killer.
PCT_MAIN = 20                                            # token flat main (uniform vs all)
PCT_VALUES = {"Light": 4, "Medium": 6, "Heavy": 9}       # % of max HP vs ALL armors (ground only)


def block_seq(group, direction):
    """One priority block -> ordered armor list. group = macro str or tuple of
    macro strs (combined = interleaved round-robin).

    Interleave rule (maintainer 2026-08-02): the LONGEST sub-ladder LEADS, then
    round-robin, so the categories alternate evenly and the extra entries of the
    longer ladder are spread out. inf(4)+veh(5) heavy ->
    Superheavy Heroic Heavy Plate Medium Flak Light None Scout (V I V I V I V I V).
    Ties (inf 4 vs air 4) keep the tuple order (stable sort)."""
    macs = (group,) if isinstance(group, str) else group
    ladders = [list(LADDERS[m]) for m in macs]
    ladders.sort(key=len, reverse=True)  # longest leads; stable keeps tie order
    if direction == "heavy":
        for l in ladders:
            l.reverse()
    out, i = [], 0
    while any(i < len(l) for l in ladders):
        for l in ladders:
            if i < len(l):
                out.append(l[i])
        i += 1
    return out


def build_order(blocks, direction):
    order = []
    for g in blocks:
        order += block_seq(g, direction)
    assert set(order) == CANON16, ("order not a permutation", set(order) ^ CANON16)
    return order


# --- THE WEAPON-TYPE MATRIX: (macro-priority blocks, direction, hits_air, levels) ---
# blocks: list of macro names / tuples (tuple = combined+interleaved). direction
# applies to ALL blocks. hits_air controls ValidTargets. Grounded in the extracted
# "how they used to be" orders, corrected to the clean sub-ladders.
L3 = ["Light", "Medium", "Heavy"]
WEAPONS = {
    "Bullet":     (["INF", ("VEH", "AIR", "BLD")],      "light", True,  L3),
    "CannonAP":   (["VEH", "BLD", "INF", "AIR"],        "heavy", False, L3),
    "CannonHE":   ([("VEH", "BLD"), "INF", "AIR"],      "light", False, L3),
    "MissileAP":  (["VEH", "AIR", "BLD", "INF"],        "heavy", True,  L3),
    "MissileHE":  ([("VEH", "BLD"), "AIR", "INF"],      "light", True,  L3),
    "MissileAA":  (["AIR", "VEH", "BLD", "INF"],        "heavy", True,  L3),
    "Flak":       (["AIR", "INF", "VEH", "BLD"],        "light", True,  L3),
    # Laser = equally good vs ALL 4 types, anti-HEAVY (starts Superheavy), HITS air.
    "Laser":      ([("VEH", "INF", "AIR", "BLD")],      "heavy", True,  L3),
    # Prism = Laser mirrored to anti-LIGHT (starts Scout), GROUND-ONLY (air last) —
    # keeps AP/TankDestroyers relevant (prism = anti-light, AP = anti-heavy).
    "Prism":      ([("VEH", "INF", "BLD"), "AIR"],      "light", False, L3),
    "Flame":      ([("INF", "BLD"), "VEH", "AIR"],      "light", False, L3),
    "Chemical":   ([("INF", "VEH"), "BLD", "AIR"],      "heavy", False, L3),
    "Melee":      (["INF", "VEH", "BLD", "AIR"],        "light", False, L3),
    "Arrow":      (["INF", "AIR", "VEH", "BLD"],        "light", True,  L3),
    # Magic = %-EQUALIZER (maintainer 2026-08-02): ground-only, tiny flat + big uniform %
    # of max HP (ignores armor) = giant-killer (melts high-HP units, useless vs swarms).
    # The mirror of Sonic (Sonic = flat/anti-low-HP; Magic = %/anti-high-HP).
    "Magic":      ("PCT", "pct", False, L3),
    # Demolition = BUILDINGS first, infantry second (maintainer 2026-08-02).
    "Demolition": (["BLD", "INF", "VEH", "AIR"],        "light", False, L3),
    "Concussion": ([("INF", "VEH", "BLD"), "AIR"],      "light", False, L3),
    # Sonic = FLAT / "ignores armor" (maintainer 2026-08-02): every armor takes the
    # same per-level value — no light/heavy gradient, no macro preference. A pure
    # generalist (never great, never useless). Values in FLAT_VALUES (tunable).
    "Sonic":      ("FLAT", "flat", False, L3),
    # tier-locked (late-game only) -------------------------------------------
    "Railgun":     (["VEH", "INF", "BLD", "AIR"],       "heavy", False, ["Heavy"]),
    "Tesla":       ([("INF", "VEH"), "BLD", "AIR"],     "heavy", False, ["Heavy"]),  # +bonus vs Shield
    "TeslaCharged":([("INF", "VEH"), "BLD", "AIR"],     "heavy", False, ["Super"]),  # Super tier + bigger Shield bonus
    # Nuclear = BUILDING-first heavy (levels structures+heavy units+air, weak vs inf) — distinct
    # from Chemical/Tesla (inf+veh). Super tier (step 3, WC 1.5). Maintainer 2026-08-02.
    "Nuclear":     (["BLD", "VEH", "AIR", "INF"],       "heavy", True,  ["Super"]),
}


def table(order16, step, top, floor, shield):
    rows = [("Shield", shield)]
    for i, a in enumerate(order16):
        rows.append((a, top - i * step))
    assert rows[-1][1] == floor, (rows[-1], floor)
    return rows


def emit_versus(rows, indent="\t\t\t", hazmat=None):
    out = [] if hazmat is None else [f"{indent}HAZMAT: {hazmat}"]
    for a, v in rows:
        out.append(f"{indent}{a}: {v}")
    return "\n".join(out)


def valid_targets(hits_air, ground_only=False):
    if hits_air:
        return "Ground, Water, Air"
    return "Ground" if ground_only else "Ground, Water"


def family(name, order16, vt, levels, *, mode=None, aoe=False, damage=1000,
           spreads=(400, 600, 800, 1000),
           falloffs=("100, 50, 33, 25, 20", "100, 50, 30, 18, 10",
                     "100, 50, 25, 10, 5", "100, 50, 20, 8, 3"),
           damage_types="Prone75Percent, TriggerProne, ExplosionDeath",
           hazmat=50, reload=25, rng=5120):
    """mode: None = sloped (from order16); 'flat' = Sonic (uniform flat, small %);
    'pct' = Magic (tiny uniform flat + LARGE uniform % of max HP).
    aoe: True = add the FriendlyFire twin (AoE rule — half spread, half damage,
    ValidRelationships: Ally, same Versus). See cameo-weapon-differentiation §13.4."""
    blocks = []
    allr = sorted(CANON16)
    for level in levels:
        li = list(LEVELS).index(level)
        pct_damage = 1
        if mode == "flat":                       # Sonic: ignores armor on FLAT
            fv, fp = FLAT_VALUES[level], FLAT_PCT[level]
            main = [("Shield", fv)] + [(a, fv) for a in allr]
            pct = [("Shield", fp)] + [(a, fp) for a in allr]
            hz = None
        elif mode == "pct":                      # Magic: ignores armor on % of max HP
            main = [("Shield", PCT_MAIN)] + [(a, PCT_MAIN) for a in allr]
            pct = [("Shield", 100)] + [(a, 100) for a in allr]  # uniform; magnitude via Damage
            pct_damage = PCT_VALUES[level]
            hz = None
        else:                                    # standard sloped profile
            step, mfloor, ptop = LEVELS[level]
            pfloor = ptop - 15
            main = table(order16, step, 100, mfloor, 100 + mfloor)
            pct = table(order16, 1, ptop, pfloor, ptop + pfloor)
            hz = hazmat
        tag = f"{name}_{level}"
        main_wh = [f"^{tag}:",
             f"\tValidTargets: {vt}",
             f"\tReloadDelay: {reload}",
             f"\tRange: {rng}",
             f"\tTargetActorCenter: true",
             f"\tWarhead@{tag}: SpreadDamage",
             f"\t\tValidRelationships: Neutral, Enemy",
             f"\t\tValidTargets: {vt}",
             f"\t\tSpread: {spreads[li]}",
             f"\t\tDamage: {damage}",
             f"\t\tFalloff: {falloffs[li]}",
             f"\t\tVersus:",
             emit_versus(main, hazmat=hz),
             f"\t\tDamageTypes: {damage_types}"]
        ff_wh = []
        if aoe:  # AoE FriendlyFire twin: half spread, half damage, Ally-only, same Versus
            ff_wh = [f"\tWarhead@{tag}FriendlyFire: SpreadDamage",
             f"\t\tValidRelationships: Ally",
             f"\t\tValidTargets: {vt}",
             f"\t\tSpread: {spreads[li] // 2}",
             f"\t\tDamage: {damage // 2}",
             f"\t\tFalloff: {falloffs[li]}",
             f"\t\tVersus:",
             emit_versus(main, hazmat=(hz // 2 if hz else None)),
             f"\t\tDamageTypes: {damage_types}"]
        pct_wh = [f"\tWarhead@{tag}Percentage: HealthPercentageDamage",
             f"\t\tValidTargets: {vt}",
             f"\t\tSpread: {spreads[li] // 2}",
             f"\t\tDamage: {pct_damage}",
             f"\t\tFalloff: {falloffs[li]}",
             f"\t\tVersus:",
             emit_versus(pct),
             f"\t\tUpdatesUnitStatistics: false"]
        blocks.append("\n".join(main_wh + ff_wh + pct_wh))
    return "\n\n".join(blocks)


def macro_summary(blocks):
    if blocks == "FLAT":
        return "FLAT (ignores armor)"
    if blocks == "PCT":
        return "PCT (%-equalizer)"
    def lbl(g):
        return "+".join(g) if isinstance(g, tuple) else g
    return " > ".join(lbl(g) for g in blocks)


SPECIAL_MODE = {"FLAT": "flat", "PCT": "pct"}

# AoE (splash) families get the FriendlyFire twin (½ spread, ½ damage, Ally-only).
# Direct-fire (Bullet/Cannon/Missile/Flak/Laser/Prism/Arrow/Railgun/Tesla) and the
# targeted Magic %-equalizer do NOT. Matches the old-template precedent
# (Grenade/Shrapnel/Flame/Chem/Sword had FF) + the AoE-FF rule (§13.4).
AOE_FAMILIES = {"Demolition", "Concussion", "Flame", "Chemical", "Nuclear", "Sonic", "Melee"}


if __name__ == "__main__":
    argv = sys.argv[1:]
    if "--list" in argv:
        for nm, (bl, d, air, lv) in WEAPONS.items():
            print(f"{nm:11s} {macro_summary(bl):26s} dir={d:5s} air={str(air):5s} {','.join(lv)}")
        sys.exit(0)
    if "--orders" in argv:
        for nm, (bl, d, air, lv) in WEAPONS.items():
            if isinstance(bl, str) and bl in SPECIAL_MODE:
                extra = (", ".join(f"{x}:{FLAT_VALUES[x]}" for x in lv) if bl == "FLAT"
                         else "% of maxHP " + ", ".join(f"{x}:{PCT_VALUES[x]}%" for x in lv))
                print(f"\n{nm:11s} [{macro_summary(bl)}] air={air}\n   {extra}")
                continue
            order = build_order(bl, d)
            print(f"\n{nm:11s} [{macro_summary(bl)}] dir={d} air={air}")
            print("   " + " > ".join(order))
        sys.exit(0)
    wanted = {a.lower() for a in argv if not a.startswith("--")}
    print("# GENERATED by gen_weapon_template.py (two-level ordering law). DO NOT hand-edit rows.")
    print("# Sidecar WeaponClass entries for docs/balance/weapon_classes.yaml:")
    for nm, (bl, d, air, lv) in WEAPONS.items():
        if wanted and nm.lower() not in wanted:
            continue
        for level in lv:
            print(f"#   ^{nm}_{level}: {WC[level]}")
    print()
    for nm, (bl, d, air, lv) in WEAPONS.items():
        if wanted and nm.lower() not in wanted:
            continue
        vt = valid_targets(air, ground_only=(nm == "Melee"))
        aoe = nm in AOE_FAMILIES
        if isinstance(bl, str) and bl in SPECIAL_MODE:
            print(f"###### {nm}: {macro_summary(bl)} ######")
            print(family(nm, None, vt, lv, mode=SPECIAL_MODE[bl], aoe=aoe))
            print()
            continue
        order = build_order(bl, d)
        print(f"###### {nm}: {macro_summary(bl)} ({d}, air={air}) ######")
        print(family(nm, order, vt, lv, aoe=aoe))
        print()
