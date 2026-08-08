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

Naming: UNIFIED `^Warhead_<Family>_<Level>` (family-first, underscore).
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


# --- PAID-FOR ExtraDamage chips (AREADAMAGE_WARHEAD_REBALANCE.md §3 REVISION 2026-08-08) ---
# Only energy weapons carry a chip, and only because each PAYS for it (K, a charge delay, or a
# structural handicap). Chip = SpreadDamage, Damage = 50% of main, EXCLUDED from price (suffix
# _ExtraDamage). Bespoke per-family Versus (NOT formula-generated). Armors omitted => floor 10.
# Per-family floor for armors not listed in the chip ladder (buildings/air/Shield).
CHIP_FLOOR = {"Laser": 9, "Railgun": 10, "Tesla": 10, "TeslaCharged": 10}
CHIPS = {
    # Laser: anti-LIGHT (inf+veh), reversed ladder — floor 9 (bldg/air), Superheavy 12, +3/step toward light.
    # Pays for: thin energy spread + 4 air ladder-slots diluting its ground damage.
    "Laser": {"Scout": 36, "None": 33, "Light": 30, "Flak": 27, "Medium": 24,
              "Plate": 21, "Heavy": 18, "Heroic": 15, "Superheavy": 12},
    # Railgun: anti-BUILDING + superheavy siege. Pays for: a charge delay (per-weapon, = 50% reload).
    "Railgun": {"Concrete": 200, "Steel": 175, "Wood": 150, "Superheavy": 125, "Heavy": 100,
                "Medium": 75, "Light": 50, "Scout": 25},
    # Tesla: anti-armored-inf + shield (restored old TeslaExtraDamage). Pays for: K=1.25 (weak EMP).
    "Tesla": {"REFLECTOR": 50, "Shield": 300, "Heroic": 200, "Plate": 175, "Flak": 150, "None": 125,
              "Superheavy": 100, "Heavy": 75, "Medium": 50, "Light": 25},
    # TeslaCharged: STRONGER (restored old TeslaChargedExtraDamage). Pays for: Super tier + K.
    "TeslaCharged": {"REFLECTOR": 50, "Shield": 400, "Heroic": 300, "Plate": 275, "Flak": 250,
                     "None": 225, "Superheavy": 200, "Heavy": 175, "Medium": 150, "Light": 125,
                     "Scout": 100, "Steel": 75, "Concrete": 50, "Wood": 25},
}
CHIP_SPREAD = {"Tesla": 200, "TeslaCharged": 400, "Laser": 200, "Railgun": 200}
# Energy mains thinned to near single-target = 50% of the chip spread (the "low spread" the
# chip/utility compensates for). Prism has no chip -> nominal 100.
ENERGY_THIN_SPREAD = {f: s // 2 for f, s in CHIP_SPREAD.items()}
ENERGY_THIN_SPREAD["Prism"] = 100


def emit_chip(tag, family_name, damage, vt):
    """Emit the paid-for ExtraDamage chip (SpreadDamage, 50% of main, bespoke Versus)."""
    d = CHIPS[family_name]
    order = ["REFLECTOR", "Shield", "None", "Flak", "Plate", "Heroic",
             "Scout", "Light", "Medium", "Heavy", "Superheavy",
             "Wood", "Steel", "Concrete", "Fighter", "Bomber", "Helicopter", "Spaceship"]
    rows = "\n".join(f"\t\t\t{a}: {d.get(a, CHIP_FLOOR[family_name])}" for a in order if a != "REFLECTOR" or "REFLECTOR" in d)
    return "\n".join([
        f"\tWarhead@{tag}_ExtraDamage: SpreadDamage",
        f"\t\tValidTargets: {vt}",
        f"\t\tSpread: {CHIP_SPREAD[family_name]}",
        f"\t\tDamage: {damage // 2}",
        f"\t\tFalloff: 100, 75, 50, 25",
        f"\t\tVersus:",
        rows,
        f"\t\tDamageTypes: Prone75Percent, TriggerProne, ExplosionDeath"])


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


def family(name, order16, vt, levels, *, mode=None, damage=2000,
           spreads=(400, 600, 800, 1000),
           falloffs=("100, 50, 33, 25, 20", "100, 50, 30, 18, 10",
                     "100, 50, 25, 10, 5", "100, 50, 20, 8, 3"),
           damage_types="Prone75Percent, TriggerProne, ExplosionDeath",
           hazmat=50, reload=25, rng=5120):
    """mode: None = sloped (from order16); 'flat' = Sonic (uniform flat, small %);
    'pct' = Magic (tiny uniform flat + LARGE uniform % of max HP).
    Every main warhead is AreaDamage with baked UNIVERSAL friendly fire
    (ValidRelationships: Ally, Neutral, Enemy + FriendlyFireDamage/Spread 50) —
    the old separate _FriendlyFire twin is retired. See cameo-expanding-damage-trait
    and docs/design/AREADAMAGE_WARHEAD_REBALANCE.md."""
    blocks = []
    allr = sorted(CANON16)
    for level in levels:
        li = list(LEVELS).index(level)
        pct_damage = damage // 2000              # 1% chip per 2000 main flat damage
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
        # Energy families are thinned to near single-target; the chip/utility pays for the low spread.
        main_spread = ENERGY_THIN_SPREAD.get(name, spreads[li])
        main_wh = [f"^Warhead_{tag}:",
             f"\tValidTargets: {vt}",
             f"\tReloadDelay: {reload}",
             f"\tRange: {rng}",
             f"\tTargetActorCenter: true",
             f"\tWarhead@{tag}: AreaDamage",
             f"\t\tValidRelationships: Ally, Neutral, Enemy",
             f"\t\tFriendlyFireDamage: 50",
             f"\t\tFriendlyFireSpread: 50",
             f"\t\tValidTargets: {vt}",
             f"\t\tSpread: {main_spread}",
             f"\t\tDamage: {damage}",
             f"\t\tFalloff: {falloffs[li]}",
             f"\t\tVersus:",
             emit_versus(main, hazmat=hz),
             f"\t\tDamageTypes: {damage_types}"]
        if name in FAMILY_PHYSICAL_STATE:  # heat/cold/corrosion meter, scaled by main damage
            psn, pss = FAMILY_PHYSICAL_STATE[name]
            main_wh += [f"\t\tPhysicalStateName: {psn}", f"\t\tPhysicalStateScale: {pss}"]
        pct_wh = [f"\tWarhead@{tag}_Percentage: HealthPercentageDamage",
             f"\t\tValidTargets: {vt}",
             f"\t\tSpread: {main_spread // 2}",
             f"\t\tDamage: {pct_damage}",
             f"\t\tFalloff: {falloffs[li]}",
             f"\t\tVersus:",
             emit_versus(pct),
             f"\t\tUpdatesUnitStatistics: false"]
        parts = main_wh + pct_wh
        if name in CHIPS:  # paid-for ExtraDamage chip (energy families only)
            parts.append(emit_chip(tag, name, damage, vt))
        blocks.append("\n".join(parts))
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

# Friendly fire is now UNIVERSAL and baked into every main warhead (AreaDamage +
# FriendlyFireDamage/Spread 50), so there is no longer a per-family FF twin.
# ^Warhead_Nuclear_Super is HAND-TUNED (10 expanding rings + AreaDamagePercentage
# subclass) — the generator cannot reproduce it, so it is excluded from emission
# (regenerating it would revert the hand-tuned superweapon). See the memory
# cameo-expanding-damage-trait.
HAND_TUNED = {"Nuclear"}

# Per-family spread overrides. Default is (400, 600, 800, 1000) for
# Light/Medium/Heavy/Super. Indices beyond a family's level count are ignored.
FAMILY_SPREADS = {
    "MissileAA": (200, 300, 400),
}

# Per-family PhysicalState wiring (docs/design/PHYSICAL_STATE_SYSTEM.md): the MAIN AreaDamage warhead
# adds `damage x Scale%` to a named meter on hit (Temperature = heat/cold, Corrosion = acid).
# Emitted on the main warhead ONLY (the _ExtraDamage chip is auto-excluded; the HealthPercentageDamage
# %-twin cannot carry the field until it is converted to AreaDamagePercentage — a later refinement).
# Cryo is a separate thin child of Prism (Temperature -100), not listed here.
FAMILY_PHYSICAL_STATE = {
    "Flame":    ("Temperature", 100),   # heat -> overheat/pop
    "Laser":    ("Temperature", 75),    # laser overheats (main only, chip excluded)
    "Chemical": ("Corrosion", 100),     # acid -> corrosion meter
    # Plasma (Temperature 50 + Corrosion 50) needs two states on one warhead -> handled at family build.
}


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
            print(f"#   ^Warhead_{nm}_{level}: {WC[level]}")
    print()
    for nm, (bl, d, air, lv) in WEAPONS.items():
        if wanted and nm.lower() not in wanted:
            continue
        if nm in HAND_TUNED:  # hand-authored; never regenerate (would revert)
            continue
        vt = valid_targets(air, ground_only=(nm == "Melee"))
        spreads = FAMILY_SPREADS.get(nm, (400, 600, 800, 1000))
        if isinstance(bl, str) and bl in SPECIAL_MODE:
            print(f"###### {nm}: {macro_summary(bl)} ######")
            print(family(nm, None, vt, lv, mode=SPECIAL_MODE[bl], spreads=spreads))
            print()
            continue
        order = build_order(bl, d)
        print(f"###### {nm}: {macro_summary(bl)} ({d}, air={air}) ######")
        print(family(nm, order, vt, lv, spreads=spreads))
        print()
