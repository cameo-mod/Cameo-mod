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
import json
import math
import pathlib
import statistics
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
LEVELS = {"Light": (6, 10, 16), "Medium": (5, 25, 20), "Heavy": (4, 40, 25), "Super": (3, 55, 30),
          # `Trace` is the SUB-LIGHT tier (WC 0.5) that WEAPON_TYPE_SYSTEM.md specifies for
          # Toxic: a lingering field a delivery weapon leaves behind, not an armament.
          # ⚠ **It MUST stay last in this dict.** `li = list(LEVELS).index(level)` indexes the
          # `spreads` and `falloffs` TUPLES positionally, so inserting a level anywhere but the
          # end silently shifts every other family's spread and falloff by one slot. Its own
          # index is therefore 4, and any family using it needs 5-long tuples.
          # Body is Light's: a family on this tier is expected to carry a designed profile, so
          # the even-ramp fallback is never reached, and Light's is the sane default if it is.
          "Trace": (6, 10, 16)}
WC = {"Light": 0.75, "Medium": 1.0, "Heavy": 1.25, "Super": 1.5, "Trace": 0.5}
# FLAT / "ignores armor" (Sonic): flat SpreadDamage, same value vs every armor. Tunable.
FLAT_VALUES = {"Light": 45, "Medium": 55, "Heavy": 65}   # main SpreadDamage vs ALL armors
FLAT_PCT = {"Light": 5, "Medium": 8, "Heavy": 10}        # its modest % chip
# PCT / "%-equalizer" (Magic): tiny flat + a LARGE uniform % of max HP (ignores armor) = giant-killer.
MAGIC_MAIN = {"Light": 22, "Medium": 27, "Heavy": 32, "Super": 38}   # Magic flat = 1/2 Sonic flat (uniform)
MAGIC_PCT  = {"Light": 25, "Medium": 40, "Heavy": 50, "Super": 65}   # Magic %-of-maxHP Versus = 5x Sonic %-chip (Damage stays 1-per-2000 grid)


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
    # Toxic = the ANTI-INFANTRY GAS, and a DIFFERENT weapon from Chemical, which is CORROSION
    # (PHYSICAL_STATE_SYSTEM.md maps Chemical to the Corrosion meter — "pure corrosion" — and
    # W9 notes "corrosion eats vehicles"; SPREAD_FALLOFF_PLAN.md calls Chemical a green blast
    # and explicitly "NOT the gas cloud"). That is exactly why the two point OPPOSITE ways:
    # Chemical is anti-HEAVY because acid eats armour, Toxic is anti-LIGHT because gas kills
    # people. Order and direction are MEASURED from the mod's own 28 gas/toxin weapons, not
    # invented — see tools/balance/design_invented_profiles.py.
    # Hits air (low, non-zero) because the legacy ^ToxicWeapon always did; W13 rule 8 wants
    # "cannot really fight air" expressed by ranking air LAST, never by omitting it.
    "Toxic":      (["INF", "BLD", "VEH", "AIR"],        "light", True,  ["Trace", "Light", "Medium"]),
    "Melee":      (["INF", "VEH", "BLD", "AIR"],        "light", False, L3),
    "Arrow":      (["INF", "AIR", "VEH", "BLD"],        "light", True,  L3),
    # Magic = %-EQUALIZER (maintainer 2026-08-02): ground-only, tiny flat + big uniform %
    # of max HP (ignores armor) = giant-killer (melts high-HP units, useless vs swarms).
    # The mirror of Sonic (Sonic = flat/anti-low-HP; Magic = %/anti-high-HP).
    "Magic":      ("PCT", "pct", False, L3 + ["Super"]),
    # Demolition = BUILDINGS first, infantry second (maintainer 2026-08-02).
    "Demolition": (["BLD", "INF", "VEH", "AIR"],        "light", False, L3),
    "Concussion": ([("INF", "VEH", "BLD"), "AIR"],      "light", False, L3),
    # Sonic = FLAT / "ignores armor" (maintainer 2026-08-02): every armor takes the
    # same per-level value — no light/heavy gradient, no macro preference. A pure
    # generalist (never great, never useless). Values in FLAT_VALUES (tunable).
    "Sonic":      ("FLAT", "flat", False, L3),
    # tier-locked (late-game only) -------------------------------------------
    "Railgun":     (["VEH", "INF", "BLD", "AIR"],       "heavy", False, ["Heavy"]),
    "Tesla":       ([("INF", "VEH"), "BLD", "AIR"],     "heavy", False, L3 + ["Super"]),  # 4-tier (L/M/H/Super); was TeslaCharged at Super
    # Nuclear = BUILDING-first heavy (levels structures+heavy units+air, weak vs inf) — distinct
    # from Chemical/Tesla (inf+veh). Super tier (step 3, WC 1.5). Maintainer 2026-08-02.
    "Nuclear":     (["BLD", "VEH", "AIR", "INF"],       "heavy", True,  ["Super"]),
}


# --- PAID-FOR ExtraDamage chips (AREADAMAGE_WARHEAD_REBALANCE.md §3 REVISION 2026-08-08) ---
# Only energy weapons carry a chip, and only because each PAYS for it (K, a charge delay, or a
# structural handicap). Chip = SpreadDamage, Damage = 50% of main, EXCLUDED from price (suffix
# _ExtraDamage). Bespoke per-family Versus (NOT formula-generated). Armors omitted => floor 10.
# Per-family floor for armors not listed in the chip ladder (buildings/air/Shield).
CHIP_FLOOR = {"Laser": 9, "Railgun": 10, "Tesla": 10}
CHIPS = {
    # Laser: anti-LIGHT (inf+veh), reversed ladder — floor 9 (bldg/air), Superheavy 12, +3/step toward light.
    # Pays for: thin energy spread + 4 air ladder-slots diluting its ground damage.
    "Laser": {"Scout": 36, "None": 33, "Light": 30, "Flak": 27, "Medium": 24,
              "Plate": 21, "Heavy": 18, "Heroic": 15, "Superheavy": 12},
    # Railgun: anti-BUILDING + superheavy siege. Pays for: a charge delay (per-weapon, = 50% reload).
    "Railgun": {"Concrete": 200, "Steel": 175, "Wood": 150, "Superheavy": 125, "Heavy": 100,
                "Medium": 75, "Light": 50, "Scout": 25},
    # Tesla L/M/H: anti-armored-inf + shield (restored old TeslaExtraDamage). Pays for: K=1.25 (weak EMP).
    "Tesla": {"REFLECTOR": 50, "Shield": 300, "Heroic": 200, "Plate": 175, "Flak": 150, "None": 125,
              "Superheavy": 100, "Heavy": 75, "Medium": 50, "Light": 25},
}
# Tesla_Super uses a STRONGER chip (was TeslaCharged), keyed by level. Pays for: Super tier + K.
CHIPS_LEVEL = {
    ("Tesla", "Super"): {"REFLECTOR": 50, "Shield": 400, "Heroic": 300, "Plate": 275, "Flak": 250,
                         "None": 225, "Superheavy": 200, "Heavy": 175, "Medium": 150, "Light": 125,
                         "Scout": 100, "Steel": 75, "Concrete": 50, "Wood": 25},
}
CHIP_SPREAD = {"Tesla": 200, "Laser": 200, "Railgun": 200}
# Tesla_Super has wider chip spread (was TeslaCharged 400).
CHIP_SPREAD_LEVEL = {("Tesla", "Super"): 400}
# Energy mains thinned to near single-target = 50% of the chip spread (the "low spread" the
# chip/utility compensates for). Prism has no chip -> nominal 100.
ENERGY_THIN_SPREAD = {f: s // 2 for f, s in CHIP_SPREAD.items()}
ENERGY_THIN_SPREAD["Prism"] = 100
ENERGY_THIN_SPREAD["Inferno"] = 100
ENERGY_THIN_SPREAD["Cryo"] = 100
# Level-specific thin-spread (same rule: half the chip spread).
ENERGY_THIN_SPREAD_LEVEL = {k: v // 2 for k, v in CHIP_SPREAD_LEVEL.items()}


def emit_chip(tag, family_name, damage, vt, level=None):
    """Emit the paid-for ExtraDamage chip (SpreadDamage, 50% of main, bespoke Versus).
    Uses CHIPS_LEVEL[(family, level)] if present, else CHIPS[family].
    For integrity-affecting families, the chip carries Tesla in DamageTypes so the passive
    INotifyDamage drain fires. SpreadDamage has no IntegrityScale field — the passive drain
    from the Tesla DamageType is the only mechanism for the chip."""
    d = CHIPS_LEVEL.get((family_name, level), CHIPS[family_name])
    floor = CHIP_FLOOR[family_name]
    spread = CHIP_SPREAD_LEVEL.get((family_name, level), CHIP_SPREAD[family_name])
    order = ["HAZMAT", "REFLECTOR", "Shield", "None", "Flak", "Plate", "Heroic",
             "Scout", "Light", "Medium", "Heavy", "Superheavy",
             "Wood", "Steel", "Concrete", "Fighter", "Bomber", "Helicopter", "Spaceship"]
    # Through `emit_versus` so the chip obeys the same descending rule as the main and the
    # %-twin — the overlay armors and Shield stay pinned at the front, the armors sort by
    # value. The overlays come from `overlay_rows`, not from the CHIPS table: the chip is
    # part of the same family and a hazmat suit cannot care which warhead of a weapon hit
    # it. The hand-set `REFLECTOR: 50` the Tesla chip used to carry was a second source for
    # one cell, which is the same trap that let Tesla's Shield be contested for months.
    overlays = dict(overlay_rows(family_name))
    rows = emit_versus([(a, overlays.get(a, d.get(a, floor))) for a in order
                       if a not in OVERLAY_ARMORS or a in overlays])
    dt = "Prone75Percent, TriggerProne, ExplosionDeath"
    if family_name in FAMILY_INTEGRITY_SCALE:
        dt += ", Tesla"
    return "\n".join([
        f"\tWarhead@{tag}_ExtraDamage: SpreadDamage",
        f"\t\tValidTargets: {vt}",
        f"\t\tSpread: {spread}",
        f"\t\tDamage: {damage // 2}",
        f"\t\tFalloff: 100, 75, 50, 25",
        f"\t\tVersus:",
        rows,
        f"\t\tDamageTypes: {dt}"])


# THE VERSUS WINDOW (maintainer 2026-08-15, DESIGN.md §12.0) — every shipped Versus value
# sits in [10, 200], a 20:1 maximum span. Declared HERE, ahead of its own commentary further
# down, because the Shield damping below is derived from the window at import time and a
# module-level constant cannot be used before it is bound.
VERSUS_CEILING = 200
VERSUS_FLOOR = 10


# --------------------------------------------------------------------------- #
# Shield — a rock-paper-scissors axis, NOT a function of the profile's shape
# --------------------------------------------------------------------------- #
# Maintainer 2026-08-16: "shields have their own armor type so they feel unique. Energy
# weapons deal more damage to shields than physical weapons but physical weapons deal more
# damage to vehicle armor than energy weapons."
#
# The OLD law was `Shield = top + floor`, written when every profile peaked at exactly 100,
# so it produced the ceiling + floor (110/125/140). W13 renormalised to median-100, "top"
# became a function of each family's SHARPNESS, and the rule silently started rewarding
# sharpness instead of anti-shield design — Melee read 200 while Tesla read 151. A sword
# out-damaged a Tesla coil against an energy shield.
#
# ⚠ MEASURED (see SHIELD_AND_NORMALISATION_PLAN.md §5b): NO structural formula can carry the
# identity. `floor` and `top` are ANTI-CORRELATED by construction — a normalised profile that
# is sharp necessarily has a low floor and a high top, and a flat one the reverse — so any
# product of them cancels out to an invariant of the normalisation rather than a property of
# the weapon (`200+floor` spans just 1.26x, the geometric mean 1.54x, both with >50% ties).
#
# So the structural term sets the SCALE and the physics rank sets the ORDER:
#
#     Shield = PHYSICS_RANK[family] x SHIELD_LEVEL[level] x sqrt((200+floor)(100+top)) x K
#
# K is calibrated so `Tesla_Super` lands on exactly 400 — the value Tesla carried before its
# anti-shield `ExtraDamage` chip was merged into the main warhead by the AreaDamage
# conversion, which is what deleted the identity's carrier in the first place.
#
# ⚠ Shield is exempt from the [10,200] window in BOTH directions now. It used to be "the one
# value always ABOVE the cap" because shields were assumed uniformly soft; under the ruling
# above they are soft to ENERGY and HARD to kinetics, so physical families land below 100
# (Melee 76 — a blade is the canonical thing a shield stops). The old invariant was a
# consequence of the old assumption, not a law in its own right.
PHYSICS_RANK = {
    # direct electrical / EM — current couples straight into the field; the shield IS the conductor
    "Tesla": 1.00, "Storm": 0.95,
    # coherent energy — delivers energy the emitter must absorb; scales with coherence
    "Quantum": 0.82, "Railgun": 0.78, "Prism": 0.76, "Laser": 0.74,
    # blended energy — part field-coupling, part thermal
    "Waveforce": 0.70, "Plasma": 0.68, "Inferno": 0.64,
    # exotic / field-adjacent
    "Sonic": 0.60, "Magic": 0.58, "Nuclear": 0.56,
    # thermal / chemical — a shield stops heat and reagents well; little field coupling
    "FireCannon": 0.52, "FireMissile": 0.52, "Flame": 0.50, "ChemCannon": 0.50,
    "ChemMissile": 0.50, "Chemical": 0.48, "Toxic": 0.46, "Thermobaric": 0.44,
    # kinetic / explosive — momentum is exactly what a shield is designed for
    "Flak": 0.38, "Concussion": 0.36, "Demolition": 0.35, "Bullet": 0.34,
    "MissileAA": 0.34, "CannonHE": 0.33, "MissileHE": 0.33, "CannonAP": 0.32,
    "MissileAP": 0.32, "Sniper": 0.30,
    # physical contact — the canonical thing a shield stops
    "Arrow": 0.24, "Melee": 0.22, "Cryo": 0.66,
}
# A shield is an ENERGY BUDGET, so a bigger discharge depletes proportionally more of it.
SHIELD_LEVEL = {"Trace": 0.80, "Light": 0.90, "Medium": 1.00, "Heavy": 1.12, "Super": 1.25}
SHIELD_DEFAULT_RANK = 0.40  # unlisted family: mid-kinetic, so a new family is never silently strong
SHIELD_FLOOR_TARGET = 100   # maintainer 2026-08-16: floor 100, ceiling 400, exactly 4.00x
SHIELD_CEIL_TARGET = 400

# --------------------------------------------------------------------------- #
# DAMPING the structural term (W25 S1, 2026-08-16)
# --------------------------------------------------------------------------- #
# §5b promised the structural term would "anchor the band without fighting the physics rank
# for control of the ordering". Post-S1 that was MEASURABLY FALSE at the very top: the scale
# swings 1.198x across the set while `Tesla -> Storm` is only a 1.053x rank gap, so
# `Storm_Super` (rank 0.95) overtook `Tesla_Super` (1.00) — 425.3 against 420.5. It did the
# same at Heavy and Medium. That inverts the maintainer's anchor law ("the only thing that
# should deal extreme damage to shields is tesla") through a term that was only ever meant
# to set the band.
#
# So the term is DAMPED to the one job §5b actually left it: separating families whose
# physics rank is EQUAL (`ChemCannon`/`ChemMissile` both 0.50, `CannonHE`/`MissileHE` both
# 0.33). The exponent is derived, not chosen — it is exactly the largest damping under which
# the SMALLEST genuine rank gap still wins:
#
#     damp = ln(smallest distinct rank ratio) / ln(the scale term's own swing)
#
# Both inputs come from things that do not move when profiles do: `PHYSICS_RANK` is a design
# table, and the swing is bounded by the WINDOW — with the mean pinned at 100 the profile's
# floor cannot exceed 100 and its top cannot exceed 200, so
# `sqrt((200+floor)(100+top))` lives in `sqrt(210x200) .. sqrt(300x300)` = 204.9 .. 300.0.
# Using the window bound rather than the measured spread slightly OVER-damps, which is the
# safe direction: it can only make a real rank gap more decisive, never less.
_SCALE_LO = math.sqrt((VERSUS_CEILING + VERSUS_FLOOR) * (100 + 100))
_SCALE_HI = math.sqrt((VERSUS_CEILING + 100) * (100 + VERSUS_CEILING))
SHIELD_SCALE_CENTRE = math.sqrt(_SCALE_LO * _SCALE_HI)
_RANKS = sorted(set(PHYSICS_RANK.values()))
SHIELD_SCALE_DAMP = (math.log(min(b / a for a, b in zip(_RANKS, _RANKS[1:])))
                     / math.log(_SCALE_HI / _SCALE_LO))

# Phase 1 emits Shield in CENTI-UNITS (x100) because the final value is not knowable here.
# The compression to [100, 400] is a property of the whole SET, so it belongs to phase 2
# (`shield_uniqueness.apply`) — and moving it there is what retires the three hand-calibrated
# constants (`SHIELD_GEOMEAN`/`ALPHA`/`ANCHOR`) that this very step would have invalidated.
# They were correct for the pre-S1 profiles and silently wrong the moment those moved, which
# is a hazard no comment can fix; derived-every-run cannot go stale at all.
#
# The x100 is not cosmetic: after damping, two equal-rank families differ by ~1%, which at a
# raw value of 50 rounds to the SAME integer and would lose the separation the damping exists
# to preserve. Centi-units keep it. Phase 2 is therefore MANDATORY — it asserts it converted
# every block, so a missed one fails loudly instead of shipping a Shield of 4900.
SHIELD_RAW_SCALE = 100


def shield_for(family, level, rows):
    """RAW Shield (centi-units) for a finished profile — phase 2 sets the final value.

    Applied LAST, overriding every other path. Both the measured path (`reference_main`)
    and the designed path (`table`) used to compute their own Shield, so two rules
    contested one cell and the measured one won — which is why Tesla's hand-set 300/400
    never took effect. This is now the single source.
    """
    vals = [v for a, v in rows if a not in NON_ARMOR_ROWS]
    if not vals:
        return None
    scale = math.sqrt((VERSUS_CEILING + min(vals)) * (100 + max(vals)))
    damped = SHIELD_SCALE_CENTRE * (scale / SHIELD_SCALE_CENTRE) ** SHIELD_SCALE_DAMP
    rank = PHYSICS_RANK.get(family, SHIELD_DEFAULT_RANK)
    return max(1, round(rank * SHIELD_LEVEL.get(level, 1.0) * damped * SHIELD_RAW_SCALE))


# --------------------------------------------------------------------------- #
# HAZMAT and REFLECTOR — the two OVERLAY armors (maintainer, 2026-08-16)
# --------------------------------------------------------------------------- #
# *"reflector armor is basically the same as HAZMAT but for energy weapons ... why is it
#  missing from the waveforce when it is combining plasma and quantum which is an energy
#  weapon? the values need to somehow reflect that"*
#
# These are not ladder rungs. They are CONDITIONAL overlay armors granted by upgrades —
# `Armor@HAZMAT` (hazmat suits, Soviet reactive armor: 329 actors) and `Armor@REFLECTOR`
# (Allied reflective plating: 16 actors) — carried IN ADDITION to the actor's real armor.
#
# ⚠ **THE ARITHMETIC CHANGED UNDER W21 AND THE VALUES DID NOT.** When armor types
# MULTIPLIED, a row of 50 meant exactly "halve the incoming damage", independent of the
# target. They now AVERAGE, so the same 50 gives
#
#     effective = (base + 50) / 2   ->   at base 100, that is 75, a 25% cut, NOT 50%.
#
# **Averaging silently halved every overlay's effect.** And it caps the mechanic: with one
# overlay the best possible is `(base + 10) / 2`, i.e. ~45% — no row value can ever reach
# the old 50%, because that would need a row of 0, which is immunity.
#
# So the row is solved from the REDUCTION it should produce rather than written directly:
#
#     effective = (100 + x) / 2 = 100 - R x 100      ->      x = 100 - 200 R
#
# The reference base of 100 is not an assumption — W25 S1 pinned every family's Versus MEAN
# to exactly 100, so 100 IS the average armor row a weapon writes. That is the second thing
# S1 bought: overlay armors became solvable.
#
# With `OVERLAY_DEPTH = 0.45` (the ceiling above), `x = 100 - 90 x share`. A share of 1.0
# lands on the window floor of 10 and reproduces the old multiplicative feel as closely as
# averaging permits; a share of 0 OMITS the row.
#
# ⚠ **OMITTING IS NOT THE SAME AS WRITING 100.** Both the engine and Cameo's override filter
# on `Versus.ContainsKey(armorType)`, so an absent row drops the overlay OUT of the average
# entirely, while `100` pulls the result toward 100. Omission is the only way to say "this
# weapon does not care about the plating", and it is what a zero share must produce.
OVERLAY_DEPTH = 0.45
OVERLAY_ARMORS = ("HAZMAT", "REFLECTOR")
OVERLAY_MIN_EFFECT = 0.05          # below a 5% cut, omit the row entirely

# What a SEALED SUIT stops. Maintainer 2026-08-16, narrowing an earlier draft of this table:
# *"hazmat is really only for flame / chemical / radiation ... both lasers and prisms are
# energy weapons so they should be reduced by the reflector armor but not the hazmat armor"*.
#
# So the axis is AGENTS AND THERMAL LOAD, not "anything that burns a bit". Laser and Prism
# dropped to zero (their heat is a side effect of an energy weapon, and REFLECTOR is their
# counter); Demolition and Concussion dropped to zero as well — blast is a mechanical
# overpressure, and its counter is `BlastProtection` in the plating taxonomy, not a suit.
#
# ⚠ Removing those four also removed the four worst cells in the whole matrix: every one of
# the top offenders in the "plating INCREASES damage" audit was a marginal ~86 row on exactly
# these families (`Laser_Medium` HAZMAT 86 vs `Heroic` 32 = 1.84x MORE damage). A share too
# small to matter is not harmless — averaged against a resistant armor it INVERTS.
CHEM_SHARE = {
    "Toxic": 1.00, "Chemical": 0.95,                       # the agent itself
    "Flame": 0.70, "Inferno": 0.70, "Nuclear": 0.65,       # heat + combustion / fallout
    "Cryo": 0.60,                                          # thermal too — insulation helps
}
# What REFLECTIVE PLATING stops: directed energy. Coherent light is the canonical case;
# electrical discharge arcs and CONDUCTS rather than reflecting, so Tesla sits well below
# Laser even though `PHYSICS_RANK` puts it on top for shields. The two tables rank different
# physics and must not be merged — a shield is a field, plating is a mirror.
ENERGY_SHARE = {
    "Laser": 1.00, "Prism": 1.00,                          # coherent light
    "Tesla": 0.60, "Storm": 0.55,                          # electrical — arcs, conducts
    "Nuclear": 0.30,                                       # the thermal flash IS radiant
    "Sonic": 0.20, "Magic": 0.20, "Railgun": 0.15,         # railgun is a SLUG: kinetic delivery
    "Cryo": 0.70, "Inferno": 0.70,                         # Prism-delivered (they inherit its beam)
}
# A blend's shares are the mean of its parents — the same rule its Versus profile uses, so a
# new blend is correct for free. Overrides are for the cases where the parent list understates
# what the weapon physically IS.
OVERLAY_OVERRIDE = {
    # Plasma's parents are Flame + Chemical, which gives it no energy share at all — but
    # plasma is ionised, radiating gas and plating does turn it. Stated, not derived.
    "Plasma": {"energy": 0.45},
    # Thermobaric derives 0.32 from Demolition+Concussion+Flame; fuel-air is specifically an
    # oxygen-consuming aerosol, which is the thing a sealed suit is FOR.
    "Thermobaric": {"chem": 0.50},
}


def overlay_shares(name):
    """(chem_share, energy_share) for a family, blends averaged from their parents."""
    over = OVERLAY_OVERRIDE.get(name, {})
    if name in BLEND_FAMILIES:
        parents = BLEND_FAMILIES[name][0]
        chem = sum(CHEM_SHARE.get(p, 0.0) for p in parents) / len(parents)
        energy = sum(ENERGY_SHARE.get(p, 0.0) for p in parents) / len(parents)
    else:
        chem = CHEM_SHARE.get(name, 0.0)
        energy = ENERGY_SHARE.get(name, 0.0)
    return over.get("chem", chem), over.get("energy", energy)


def overlay_rows(name):
    """`[(armor, value)]` for HAZMAT / REFLECTOR — omitting either where it does nothing."""
    rows = []
    for armor, share in zip(OVERLAY_ARMORS, overlay_shares(name)):
        value = max(VERSUS_FLOOR, min(100, round(100 - 200 * OVERLAY_DEPTH * share)))
        # A share so small the row would change damage by less than OVERLAY_MIN_EFFECT is
        # dropped rather than written. It is not free to keep: by the rule above, a present
        # row joins the average and moves the result, so a 4% row is a real-but-invisible
        # change that still costs a line and a reader's attention. Rows should be deliberate.
        if share <= 0 or value > 100 - 200 * OVERLAY_MIN_EFFECT:
            continue
        rows.append((armor, value))
    return rows


def table(order16, step, top, floor, shield):
    rows = [("Shield", shield)]
    for i, a in enumerate(order16):
        rows.append((a, top - i * step))
    assert rows[-1][1] == floor, (rows[-1], floor)
    return rows


# --------------------------------------------------------------------------- #
# W13 — MEASURED Versus profiles (the reference corpus), replacing the even ramp
# --------------------------------------------------------------------------- #
# `table()` above is a LINEAR ladder: 100, 100-step, 100-2*step ... Equal steps
# were a generator artifact and produce exactly the "moderate middle" the warhead
# rebuild exists to escape (DESIGN.md §12.0 rule 3). Where the reference corpus
# measured a family at a platform tier, its profile is used INSTEAD — plateaus,
# cliffs and all — and `table()` remains the fallback for the families Cameo
# invented, which have no cross-mod equivalent to learn from.
#
# The data is FROZEN into a committed JSON rather than computed here on purpose:
# deriving it needs `tools/reference/survey_platforms.py`, which traces the source
# mods' INI files out of `~/Downloads`. Nobody else has those, so a generator that
# imported the derivation would only run on one machine. Regenerate the JSON with
#   python tools/reference/propose_family_profiles.py --json
# and the ORDER still comes from `build_order()` here — the corpus supplies
# magnitudes, the law supplies order (DESIGN.md §12.0).
# THE VERSUS WINDOW (maintainer, 2026-08-15, DESIGN.md §12.0): every shipped Versus
# value sits in [10, 200] — a 20:1 maximum span, which is the SAME extreme the old
# peak-100 law wrote as "100 against a floor of 5", re-expressed on the median-100
# scale. A legal maximum, not a target: the reference mods' own profiles span only
# 1.3x-7.2x, so anything beyond that is Cameo's design choice, not the field's.
# (`VERSUS_CEILING` / `VERSUS_FLOOR` are bound near the top of the file — see the note there.)
REFERENCE_JSON = (pathlib.Path(__file__).resolve().parents[2]
                  / "docs" / "reference" / "family_profiles.json")
# The families Cameo INVENTED, which have no cross-mod equivalent to measure. Kept in
# a separate file under docs/design/ rather than merged into the reference JSON, so
# the two provenances can never be confused: docs/reference/ is what the field said,
# docs/design/ is what we decided. Regenerate with
#   python tools/balance/design_invented_profiles.py --write
DESIGNED_JSON = (pathlib.Path(__file__).resolve().parents[2]
                 / "docs" / "design" / "invented_family_profiles.json")


def _load(path):
    try:
        return json.loads(path.read_text(encoding="utf-8"))["families"]
    except (OSError, ValueError, KeyError):   # missing/damaged data must not break the generator
        return {}


REFERENCE_PROFILES = _load(REFERENCE_JSON)
DESIGNED_PROFILES = _load(DESIGNED_JSON)


def distinct_ints(rows):
    """No two values identical, on INTEGERS — the last step of the shape law.

    The frozen profiles are floats separated by at least 2, but `Versus` is an
    `int` dictionary in the engine (`DamageWarhead.Versus`), and rounding can
    re-create a tie the float profile had already separated. Walks DOWN from the
    peak, which is the direction with room: nudging up pins ties against the top
    and silently re-creates the duplicate (the bug that shipped Tesla with
    `Plate 100` AND `Superheavy 100`). `sorted` is stable, so armors that tie
    keep the order the ordering law gave them.
    """
    ranked = sorted(range(len(rows)), key=lambda i: -rows[i][1])
    fixed, previous = {}, None
    for i in ranked:
        armor, value = rows[i]
        if previous is not None and value >= previous:
            value = previous - 1
        fixed[armor] = value
        previous = value
    # THE VERSUS WINDOW (maintainer 2026-08-15): 10 <= Versus <= 200, a 20:1 span,
    # the same maximum specialisation the peak-100 law expressed as 100-against-5.
    # The descent above can breach the floor, so repair from the BOTTOM up: raise
    # only what fell through and leave the measured shape above it alone.
    if fixed and min(fixed.values()) < VERSUS_FLOOR:
        previous = None
        for i in reversed(ranked):
            armor = rows[i][0]
            lowest = VERSUS_FLOOR if previous is None else previous + 1
            fixed[armor] = min(max(fixed[armor], lowest), VERSUS_CEILING)
            previous = fixed[armor]
    return [(armor, fixed[armor]) for armor, _ in rows]


BAND_LOW = 2.0                      # DESIGN.md §12.0 rule 5 — the target band's flat end
DERIVED_ARMORS = ("Heroic", "Airborne")
# Rows that live on a Versus node but are not armor classes, so they never enter a
# profile statistic: the shield LAYER, the HAZMAT gate, Tesla's REFLECTOR.
NON_ARMOR_ROWS = ("Shield", "HAZMAT", "REFLECTOR")


# --------------------------------------------------------------------------- #
# W25 S1 — THE MEAN-100 LAW (maintainer, 2026-08-16)
# --------------------------------------------------------------------------- #
# *"all warheads average all versus values at 100 to make them comparable"*
#
# W13 normalised each profile to its MEDIAN. That left every family's MEAN free, and
# a family's mean is not a shape statistic — it is a MAGNITUDE. `K` is a share-weighted
# average of the profile, so the mean IS the family's contribution to priced DPS, and
# picking a family silently changed a weapon's total output as well as its shape.
# Measured across the 94 shipped templates before this change: means ran 22.0
# (`Magic_Light`) to 106.1 (`MissileAA_Light`), averaging 75.0 — i.e. up to a 4.8x
# hidden multiplier between two families that both looked "normalised".
#
# Pinning the mean at 100 makes:
#   * `K` SHAPE-ONLY — choosing a family redistributes output across armors without
#     changing how much of it there is;
#   * `Damage` the sole magnitude knob, which is what the balance pipeline wants;
#   * families directly comparable, which is the maintainer's stated goal.
#
# The mean is UNWEIGHTED over the 16 armor rows, deliberately. A share-weighted mean
# would be a better predictor of realised DPS, but it would make a profile's legality
# depend on the current unit roster — every roster change would silently move every
# template. Uniform weighting is a property of the profile alone.
#
# ⚠ **The window makes this a constraint, not just a rescale.** With the mean pinned at
# 100, `max <= 200` becomes `max <= 2 x mean`: a profile that is brilliant against three
# armors and useless against thirteen CANNOT keep its peak, because thirteen low values
# drag the mean down. Measured: 11 of 94 templates breach the ceiling under a plain
# rescale, worst `Melee_Medium` at 316 (median 35.5 against a peak of 174 — the extreme
# bottom-heavy profile). Those 11 are brought back by the POWER LAW about the geometric
# mean, `v' = G x (v/G) ** alpha`, never by clamping. Three reasons it is the right
# instrument, the third specific to this step:
#
#   1. it is monotone, so the ordering law's sequence survives untouched;
#   2. it preserves the geometric mean, the correct centre for a set of MULTIPLIERS;
#   3. **it preserves the derived-armor relation exactly.** §12.0b sets
#      `Heroic = Plate x Scout / peak`, and the power law satisfies
#      `G(P/G)^a x G(S/G)^a / G(peak/G)^a == G(H/G)^a` identically — so Heroic stays
#      derived without being recomputed. A clamp or an affine squeeze breaks that.
#
# The cost is real and is reported by `report_versus_change.py`: the 11 compressed
# templates lose sharpness (Melee 6.69x -> 2.86x, Arrow 4.71x -> 3.01x; the other eight
# move by less than 1.0x). Median spread across all 94 barely moves, 3.06x -> 3.00x, so
# the field band (2/4/8) still holds. If the maintainer wants Melee's skew back, the
# lever is the CEILING, not this function — under mean-100 a peak of 2x the average is
# arithmetic, not policy.
#
# ⚠ **Scope: the MAIN warhead only.** The `_Percentage` twin encodes its magnitude IN
# its Versus rows (`Damage` is a fixed 1-per-2000 grid), so normalising it would multiply
# every %-effect by ~5x. Rebasing the twins is W18's atomic job.
MEAN_TARGET = 100.0


# --------------------------------------------------------------------------- #
# W25 S2 — THE CLASS TILT (maintainer, 2026-08-16)
# --------------------------------------------------------------------------- #
# *"light weapons have a bigger damage to light armor types while heavy weapons have a
#  bigger damage to heavy armor types with medium weapons having bigger damage to medium
#  armor types ... all inside their own family (compared for example light, medium and
#  heavy flame weapons) ... and super just deals a more flat damage to everything so it's
#  overall good (but still doesn't have the same values, just a more flat curve)"*
#
# With the mean pinned at 100 by S1, a tilt is FREE: it costs nothing in total output, it
# only moves where the output lands. That is what makes this expressible at all.
#
# The maintainer wrote the tilt as three armor SETS (SHIELD_AND_NORMALISATION_PLAN §6c):
#
#     Light  -> None · Wood · Scout · Light · Fighter
#     Medium -> Flak · Steel · Medium · Bomber · Helicopter
#     Heavy  -> Plate · Concrete · Heavy · Superheavy · Spaceship
#
# Implemented as ladder POSITION rather than as a hard-coded set, because position is what
# those sets ARE: the lightest rung of every ladder, the middle rung of every ladder, the
# heaviest rung of every ladder. Reading them off `LADDERS` reproduces all three sets
# exactly and keeps working if a ladder ever gains a rung, where a literal set would
# silently leave the new armor untilted.
#
# ⚠ **THE TILT MUST NEVER REORDER A LADDER.** The two-level ordering law (maintainer
# 2026-08-01, "the most important part") fixes which armor in a ladder takes the biggest
# value, and a Heavy-level tilt on an anti-LIGHT family pushes exactly the wrong way — left
# alone it would invert `None > Flak > Plate` and make a rifle best against plate. So the
# tilt is applied to the VALUES, and then each armor is given back the RANK it held before:
#
#   * where the tilt agrees with the family's direction it SHARPENS the ladder;
#   * where it disagrees it FLATTENS it;
#   * it can never invert it, and it needs no `direction` argument — the profile's own
#     order is the authority, which is what makes this work for the blends too (they have
#     no `build_order` at all, their shape comes from averaging their parents).
#
# The relative statement the maintainer asked for survives intact: `Scout` takes a larger
# share of Flame_Light's output than of Flame_Heavy's, and `Superheavy` the reverse.
#
# `TILT_RATIO` is the weight span across a ladder, so 1.5 means the favoured end is pulled
# 1.5x harder than the disfavoured end BEFORE renormalisation. Chosen to stay inside the
# measured field band (2/4/8) after the sharpening it causes — see the audit below.
TILT_RATIO = 1.5
# Super is not merely untilted, it is actively FLATTENED to the band's flat end: it is the
# generalist, so it should have the shallowest curve of any level. Never to EQUAL values —
# the no-ties law still binds; "flat" here means "lowest spread", not "uniform".
SUPER_RATIO = BAND_LOW


def tilt_exponent(level, pos, n):
    """Where position `pos` of `n` sits in this level's favour, in [-0.5, +0.5].

    All three tilts share one range, so `TILT_RATIO` means the same thing at every level.
    `Trace` is the sub-Light tier and tilts with Light.
    """
    if n <= 1:
        return 0.0
    t = pos / (n - 1)
    if level in ("Light", "Trace"):
        return 0.5 - t                       # favours the lightest rung
    if level == "Medium":
        return 0.5 - 2 * abs(t - 0.5)        # favours the middle rungs
    if level == "Heavy":
        return t - 0.5                       # favours the heaviest rung
    return 0.0                               # Super: no tilt, flattened instead


def class_tilt(rows, level):
    """Apply the level's class tilt to a MAIN profile, preserving every ladder's order."""
    vals = dict(rows)
    live = [v for a, v in rows if a not in NON_ARMOR_ROWS]
    if not live or max(live) <= min(live):
        return rows          # Sonic / Magic are flat BY DESIGN; a tilt would destroy that
    out = dict(vals)
    for ladder in LADDERS.values():
        # Derived armors are excluded: `Heroic` is a PRODUCT of two other cells (§12.0b),
        # so it has to be recomputed from the finished profile rather than tilted like an
        # independent rung. That is also why §6c assigns it to no tier.
        rungs = [a for a in ladder if a in vals and a not in DERIVED_ARMORS]
        if len(rungs) < 2:
            continue
        n = len(rungs)
        tilted = [vals[a] * TILT_RATIO ** tilt_exponent(level, i, n)
                  for i, a in enumerate(rungs)]
        # Give each armor back the rank it held BEFORE the tilt (see the warning above).
        # `-vals[a]` ranks descending; the index breaks ties stably, so a ladder that
        # already had two equal values keeps the ordering law's sequence between them.
        order = sorted(range(n), key=lambda i: (-vals[rungs[i]], i))
        for slot, i in enumerate(order):
            out[rungs[i]] = sorted(tilted, reverse=True)[slot]
    if level == "Super":
        # The generalist: compress toward the band's flat end about the geometric mean.
        body = [v for a, v in out.items() if a not in NON_ARMOR_ROWS and v > 0]
        lo, hi = min(body), max(body)
        if lo > 0 and hi / lo > SUPER_RATIO:
            g = statistics.geometric_mean(body)
            alpha = math.log(SUPER_RATIO) / math.log(hi / lo)
            out = {a: (v if a in NON_ARMOR_ROWS else g * (max(v, 1.0) / g) ** alpha)
                   for a, v in out.items()}
    # Re-derive the products LAST, from the finished profile (§12.0b) — a derived value
    # computed before the last cell moves is not derived, it is stale.
    peak = max(v for a, v in out.items()
               if a not in NON_ARMOR_ROWS and a not in DERIVED_ARMORS)
    for name, (first, second) in (("Heroic", ("Plate", "Scout")),
                                  ("Airborne", ("Helicopter", "Scout"))):
        if name in out and first in out and second in out and peak > 0:
            out[name] = out[first] * out[second] / peak
    return [(a, out[a]) for a, _ in rows]


def _powerlaw(vals, alpha):
    g = statistics.geometric_mean([max(v, 1.0) for v in vals])
    return [g * (max(v, 1.0) / g) ** alpha for v in vals]


def _to_mean(vals, target):
    m = statistics.fmean(vals)
    return [v * target / m for v in vals] if m > 0 else list(vals)


def mean_normalise(rows, target=MEAN_TARGET):
    """Rescale a MAIN profile so the MEAN of its armor rows is `target` (see above).

    Returns rows in the SAME ORDER — the emit order is the ordering law's output and
    `shield_for` overwrites `Shield` immediately after, so nothing here may reshuffle.
    """
    idx = [i for i, (a, _) in enumerate(rows) if a not in NON_ARMOR_ROWS]
    vals = [float(rows[i][1]) for i in idx]
    if not vals:
        return rows
    if max(vals) <= min(vals):
        # Sonic / Magic are flat BY DESIGN ("ignores armor"). Flat at 100 says exactly
        # that and nothing else; the level ladder they used to carry here (45/55/65,
        # 22/27/32/38) was magnitude, which now belongs to Damage and WC.
        fitted = [target] * len(vals)
    else:
        def fits(alpha):
            out = _to_mean(_powerlaw(vals, alpha), target)
            return max(out) <= VERSUS_CEILING + 1e-9 and min(out) >= VERSUS_FLOOR - 1e-9
        if fits(1.0):
            fitted = _to_mean(vals, target)
        else:
            lo, hi = 0.0, 1.0            # largest alpha = least distortion
            for _ in range(60):
                mid = (lo + hi) / 2.0
                if fits(mid):
                    lo = mid
                else:
                    hi = mid
            fitted = _to_mean(_powerlaw(vals, lo), target)
    armor = [(rows[i][0], int(round(v))) for i, v in zip(idx, fitted)]
    if max(vals) > min(vals):
        armor = distinct_ints(armor)     # rounding can re-tie what the floats separated
    fixed = dict(armor)
    return [(a, fixed.get(a, v)) for a, v in rows]


def finish_blend(rows):
    """Repair a BLEND profile: re-derive its derived armors, then re-sharpen it.

    A blend is the per-armor AVERAGE of its parents, and averaging does two things
    that have to be undone before it ships:

    1. **It computes the derived armors instead of deriving them.** §12.0b says
       `Heroic = Plate x Scout / peak` **of the profile it belongs to** — and the
       average of the parents' Heroic is not the product of the blend's own Plate
       and Scout (`avg(ab/p) != avg(a)avg(b)/avg(p)`). Measured: 5 of 21 blend
       levels were off, `FireCannon_Light` by 12 points. Same failure as the
       `/100` divisor bug — a derived value has to be derived LAST, from the
       finished profile.
    2. **It flattens.** Averaging profiles that disagree cancels the
       disagreement — the identical effect that makes a per-family aggregate mush
       (DESIGN §12.0 rule 5). `ChemMissile_Heavy` came out at 1.8x, under the
       band. It is re-sharpened back to the band floor with the same POWER LAW the
       reference side uses (`v' = G * (v/G) ** alpha` about the geometric mean),
       never by clamping: clamping would move two cells and change the shape,
       where the power law moves every cell proportionally and preserves both the
       ordering and the geometric centre.
    """
    values = dict(rows)
    peak = max(v for a, v in rows if a != "Shield" and a not in DERIVED_ARMORS)
    for name, (first, second) in (("Heroic", ("Plate", "Scout")),
                                  ("Airborne", ("Helicopter", "Scout"))):
        if name in values and first in values and second in values and peak > 0:
            values[name] = values[first] * values[second] / peak

    ladder = [v for a, v in values.items()
              if a != "Shield" and a not in DERIVED_ARMORS and v > 0]
    if len(ladder) >= 2:
        hi, lo = max(ladder), min(ladder)
        if lo > 0 and 1.0 < hi / lo < BAND_LOW:
            centre = statistics.geometric_mean(ladder)
            alpha = math.log(BAND_LOW) / math.log(hi / lo)
            values = {a: centre * (max(v, 1.0) / centre) ** alpha
                      for a, v in values.items()}

    # Back inside the window, multiplicatively so the spread just set survives.
    top = max(values.values())
    scale = min(1.0, VERSUS_CEILING / top) if top > 0 else 1.0
    out = [(a, int(round(values[a] * scale))) for a, _ in rows]
    return sorted(distinct_ints(out), key=lambda r: -r[1])


def reference_main(name, order16, level):
    """The measured main-warhead rows for a family+level, or None if unmeasured.

    `Shield` keeps the rule it always had — one floor above the profile's best
    target (`table()` passes `100 + floor`, i.e. top + floor). Expressed against
    the profile's OWN top and floor it is the same rule, and it reduces to exactly
    the old constant whenever the profile peaks at 100. That matters: shields are
    the softest layer by design, so pinning them to a constant while the profile
    moves would make a specialist's best target tougher than a shield.
    """
    # Measured wins over designed: if the corpus ever gains coverage for a family we
    # had to invent, the evidence should displace the design without anyone having to
    # remember to delete the old entry.
    entry = (REFERENCE_PROFILES.get(name, {}).get(level)
             or DESIGNED_PROFILES.get(name, {}).get(level))
    if entry is None:
        return None
    profile = entry["profile"]
    if not all(a in profile for a in order16):
        return None                       # partial data is not usable data
    values = [profile[a] for a in order16]
    shield = max(values) + min(values)
    # `Shield` is a Versus row like any other, so the window binds on it too — and it
    # is the row most likely to breach, because its rule puts it one floor ABOVE the
    # profile's best target and the best target may already be at the ceiling. Scale
    # the whole set down together rather than clamping Shield alone: clamping would
    # tie it with the top armor, and a shield that is no softer than the toughest
    # thing the weapon can hit is not a shield.
    scale = min(1.0, VERSUS_CEILING / shield) if shield > 0 else 1.0
    rows = ([("Shield", int(round(shield * scale)))]
            + [(a, int(round(profile[a] * scale))) for a in order16])
    rows = distinct_ints(rows)
    # Emit DESCENDING. The law's order produced the assignment, but `Heroic` is
    # DERIVED (`Plate x Scout / peak`) and lands wherever its product falls, not in
    # the infantry slot the order gave it — so the law's sequence is no longer a
    # descending list and a reader can no longer check the ladder by eye. Sorting
    # is display only: for the even-ramp families it is exactly the order they
    # already emit, so nothing moves except the derived armors.
    return sorted(rows, key=lambda r: -r[1])


def emit_versus(rows, indent="\t\t\t"):
    """Emit a `Versus:` node: pseudo-rows first, then armors DESCENDING by value.

    Maintainer 2026-08-16: *"the percentage versus values are not ordered by power like
    they are for the normal variants ... enforce this rule so percentage values are also
    always ordered by descending value (except for hazmat and shield which are always
    first)"*.

    The main warhead already arrived sorted (`reference_main` / `finish_blend` end with a
    descending sort), but the `_Percentage` twin and the `_ExtraDamage` chip did not: they
    were emitted in the ORDERING LAW's sequence — macro blocks INF, VEH, BLD, AIR, each
    ascending for a light-favouring family — which reads as `9 10 11 13 · 7 9 10 12 13`,
    restarting at every block. The law decides which armor gets which VALUE; it was never
    meant to decide the print order, and a reader cannot check a ladder that restarts.

    Sorting HERE rather than at each call site makes the invariant unconditional: every
    Versus node the generator emits is descending, whoever built the rows. The sort is
    STABLE, so armors that tie keep the ordering law's sequence — which is the right
    tiebreak, because the law's order is the design statement about them.

    `HAZMAT` and `Shield` are pinned first and excluded from the sort: neither is an armor
    class (one is a gate, the other the shield LAYER), and `Shield` is deliberately outside
    the [10, 200] window in both directions, so sorting it in would drag it to an end and
    hide the ladder it is not part of.
    """
    out = []
    lead = [r for r in rows if r[0] in NON_ARMOR_ROWS]
    body = sorted((r for r in rows if r[0] not in NON_ARMOR_ROWS), key=lambda r: -r[1])
    for a, v in lead + body:
        out.append(f"{indent}{a}: {v}")
    return "\n".join(out)


def valid_targets(hits_air, ground_only=False):
    if hits_air:
        return "Ground, Water, Air"
    return "Ground" if ground_only else "Ground, Water"


# Damage falloff profiles (maintainer 2026-08-11): 6-wide, ALL end in 0 so damage reaches 0 at the
# outer ring. One profile per level (Light/Medium/Heavy/Super); higher tiers fall off STEEPER (more
# concentrated). Radius = (len-1) x Spread; damage LERPs between points. Per-family overrides below.
DEFAULT_FALLOFFS = ("100, 50, 33, 25, 20, 0", "100, 60, 30, 15, 5, 0",
                    "100, 50, 25, 10, 5, 0", "100, 40, 20, 10, 5, 0")
# Even / linear ramp (opt-in via FAMILY_FALLOFFS): equal steps to 0 = a flat line.
EVEN_FALLOFFS = ("100, 80, 60, 40, 20, 0",) * 4


def family(name, order16, vt, levels, *, mode=None, damage=2000,
           spreads=(400, 600, 800, 1000),
           falloffs=DEFAULT_FALLOFFS,
           damage_types="Prone75Percent, TriggerProne, ExplosionDeath",
           overlays=True, reload=25, rng=5120, versus_override=None, physical_states=None,
           profile_family=None):
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
        if versus_override is not None:          # blend family (e.g. Plasma = avg of Flame + Chemical)
            main, pct = versus_override(level)
            main = finish_blend(main)            # re-derive Heroic/Airborne, un-flatten
            hz = overlays
        elif mode == "flat":                       # Sonic: ignores armor on FLAT
            fv, fp = FLAT_VALUES[level], FLAT_PCT[level]
            main = [("Shield", fv)] + [(a, fv) for a in allr]
            pct = [("Shield", fp)] + [(a, fp) for a in allr]
            hz = None
        elif mode == "pct":                      # Magic: 1/2 Sonic flat + 5x Sonic %-of-maxHP (giant-killer)
            mv = MAGIC_MAIN[level]
            main = [("Shield", mv)] + [(a, mv) for a in allr]
            pv = MAGIC_PCT[level]                 # %-magnitude in VERSUS (scales with main); Damage stays 1
            pct = [("Shield", pv)] + [(a, pv) for a in allr]
            hz = None
        else:                                    # standard sloped profile
            step, mfloor, ptop = LEVELS[level]
            pfloor = ptop - 15
            # Measured profile if the corpus has one, otherwise the even ramp.
            # `profile_family` lets an INHERITING family (Cryo, Inferno) read its
            # PARENT's measured profile — the whole premise of those families is
            # that they reuse the parent's ladder and only add a PhysicalState, so
            # looking the profile up under their own name silently fell through to
            # the even ramp and split them off from the parent they inherit.
            main = (reference_main(profile_family or name, order16, level)
                    or table(order16, step, 100, mfloor, 100 + mfloor))
            # ⚠ The %-twin stays the 1-step ladder, deliberately. Its window is only
            # 16 wide (`ptop` down to `ptop-15`), so 16 armors that must all differ
            # can ONLY be the even ramp — there is no room left for a shape. Giving
            # it one needs W18's x5 rebase to open the window, and W18 must land as
            # one change (denominator + values) or every %-twin deals a fifth or
            # five times. Until then the twin carries the ORDER, not the shape.
            pct = table(order16, 1, ptop, pfloor, ptop + pfloor)
            hz = overlays
        # W25 S2 — the class tilt, BEFORE the mean is pinned: the tilt moves output between
        # armors and would otherwise leave the mean off 100. Order-preserving by
        # construction (see class_tilt), so the two-level ordering law is untouched.
        main = class_tilt(main, level)
        # W25 S1 — pin the profile's MEAN to 100 before anything reads it. Must run on
        # EVERY branch and BEFORE `shield_for`: Shield's structural term is
        # `sqrt((200+floor)(100+top))`, so it has to see the final ladder, not the
        # pre-normalisation one. See MEAN_TARGET above for why, and for what it costs.
        main = mean_normalise(main)
        # SINGLE SOURCE for Shield, applied to EVERY branch (flat, pct and standard):
        # overrides whatever the measured or designed path put there, so the two can never
        # contest the cell again (see shield_for). Placed after the if/else on purpose —
        # scoping it to one branch left the FLAT/PCT families on their old value.
        sv = shield_for(name, level, main)
        if sv is not None:
            main = [("Shield", sv)] + [(a, v) for a, v in main if a != "Shield"]
        # The OVERLAY armors, derived from the family's composition (see overlay_rows).
        # Carried inside `main` rather than passed separately so they are automatically
        # excluded from the mean, the tilt and the Shield scale — all three key off
        # NON_ARMOR_ROWS — and pinned ahead of the ladder by `emit_versus`. Suppressed for
        # the flat/pct families, whose whole identity is that they ignore armor.
        main = [r for r in main if r[0] not in OVERLAY_ARMORS]
        if hz:
            main = overlay_rows(name) + main
        tag = f"{name}_{level}"
        # Energy families are thinned to near single-target; the chip/utility pays for the low spread.
        main_spread = ENERGY_THIN_SPREAD_LEVEL.get((name, level), ENERGY_THIN_SPREAD.get(name, at(spreads, li)))
        invalid = FAMILY_INVALID_TARGETS.get(name)
        inv_weapon = [f"\tInvalidTargets: {invalid}"] if invalid else []
        inv_warhead = [f"\t\tInvalidTargets: {invalid}"] if invalid else []
        main_wh = [f"^Warhead_{tag}:",
             f"\tValidTargets: {vt}",
             *inv_weapon,
             f"\tReloadDelay: {reload}",
             f"\tRange: {rng}",
             f"\tTargetActorCenter: true",
             f"\tWarhead@{tag}: AreaDamage",
             f"\t\tValidRelationships: Ally, Neutral, Enemy",
             f"\t\tFriendlyFireDamage: 50",
             f"\t\tFriendlyFireSpread: 50",
             f"\t\tValidTargets: {vt}",
             *inv_warhead,
             f"\t\tSpread: {main_spread}",
             f"\t\tDamage: {damage}",
             f"\t\tFalloff: {at(falloffs, li)}",
             f"\t\tVersus:",
             emit_versus(main),
             f"\t\tDamageTypes: {damage_types}"]
        if name in FAMILY_PHYSICAL_STATE:  # heat/cold/corrosion meter, scaled by main damage
            psn, pss = FAMILY_PHYSICAL_STATE[name]
            main_wh += [f"\t\tPhysicalStateName: {psn}", f"\t\tPhysicalStateScale: {pss}"]
        if physical_states:  # multi-state blend (e.g. Plasma: Temperature 50 + Corrosion 50)
            main_wh.append("\t\tPhysicalStates:")
            main_wh += [f"\t\t\t{k}: {v}" for k, v in physical_states.items()]
        integ = FAMILY_INTEGRITY_SCALE.get(name)  # shield/EMP auto-drain
        if integ:
            main_wh.append(f"\t\tIntegrityScale: {integ}")
        percentage_state = FAMILY_PHYSICAL_STATE.get(name) if name in {"Flame", "Chemical", "Inferno", "Cryo"} else None
        # All %-twins use the Cameo AreaDamagePercentage warhead (unified 2026-08-10): same expanding-ring
        # spatial pass + baked-FF plumbing as the AreaDamage main, and it can carry PhysicalStateScale.
        # Behaviour-preserving drop-in for HealthPercentageDamage (no ValidRelationships: Ally => no FF).
        # AreaDamagePercentage extends AreaDamageWarhead, so it inherits IntegrityScale. For integrity-
        # affecting families, the %-twin MUST also drain integrity (otherwise HP dies before integrity
        # depletes). The %-twin also carries DamageTypes: Tesla for the passive INotifyDamage drain.
        percentage_type = "AreaDamagePercentage"
        pct_wh = [f"\tWarhead@{tag}_Percentage: {percentage_type}",
             f"\t\tValidTargets: {vt}",
             *inv_warhead,
             f"\t\tSpread: {main_spread // 2}",
             f"\t\tDamage: {pct_damage}",
             f"\t\tFalloff: {at(falloffs, li)}",
             f"\t\tVersus:",
             emit_versus(pct)]
        if integ:
            pct_wh.append(f"\t\tDamageTypes: Tesla")
            pct_wh.append(f"\t\tIntegrityScale: {integ}")
        pct_wh.append(f"\t\tUpdatesUnitStatistics: false")
        if percentage_state:
            psn, pss = percentage_state
            pct_wh += [f"\t\tPhysicalStateName: {psn}", f"\t\tPhysicalStateScale: {pss}"]
        parts = main_wh + pct_wh
        if name in CHIPS:  # paid-for ExtraDamage chip (energy families only)
            parts.append(emit_chip(tag, name, damage, vt, level=level))
        if name in FAMILY_CONDITION:  # on-hit status mark (Sonic -> SonicDebuff)
            cname, dmul, rmul = FAMILY_CONDITION[name]
            parts.append(emit_condition(tag, cname, reload * dmul, main_spread * rmul, vt))
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
# ⚠ Indexed POSITIONALLY by `list(LEVELS).index(level)`, i.e.
#     0 Light · 1 Medium · 2 Heavy · 3 Super · 4 Trace
# so any family using `Trace` needs five entries. `at()` below tolerates a short tuple rather
# than raising IndexError, but a family should still state its own value instead of inheriting
# whatever happens to sit last.
FAMILY_SPREADS = {
    "MissileAA": (200, 300, 400),
    # A gas FIELD is wide and thin by nature — it is area denial, not a hit. Trace (slot 4) is
    # the faint wisp, Light the standard cloud, Medium the heavy/Large variants.
    "Toxic": (900, 1100, 800, 800, 700),
}

# Per-family falloff overrides (default = DEFAULT_FALLOFFS, indexed by level). Bullets are a single
# max-radius linear ramp (near single-target); EVEN_FALLOFFS is available for any family that wants a
# flat line. Nuclear is HAND_TUNED (11-wide, hand-set in its yaml template).
FAMILY_FALLOFFS = {
    "Bullet": ("100, 0", "100, 0", "100, 0"),   # pure max-radius linear
    # A drifting cloud has no blast centre: concentration is near-even across the field and
    # only tails off at the edge. Slot order as in FAMILY_SPREADS (Trace is slot 4).
    "Toxic": ("100, 90, 75, 55, 30, 0",) * 5,
}


def at(seq, index):
    """`seq[index]`, tolerating a tuple shorter than the level count.

    The per-family spread/falloff tuples are indexed positionally by the level's place in
    `LEVELS`, so adding a level made every 4-long tuple a latent IndexError for any family
    that used it. Falling back to the last entry keeps the generator running; the fix for a
    family that cares is to state its own value.
    """
    return seq[index] if index < len(seq) else seq[-1]

# Per-family PhysicalState wiring (docs/design/PHYSICAL_STATE_SYSTEM.md): the main AreaDamage warhead
# adds `damage x Scale%` to a named meter on hit (Temperature = heat/cold, Corrosion = acid).
# Flame and Chemical also emit meter-aware AreaDamagePercentage twins so their percentage damage
# contributes to the same meter. The _ExtraDamage chip remains excluded.
# Cryo is a separate thin child of Prism (Temperature -100), not listed here.
FAMILY_PHYSICAL_STATE = {
    "Flame":    ("Temperature", 100),   # heat -> overheat/pop
    "Laser":    ("Temperature", 75),    # laser overheats (main only, chip excluded)
    "Chemical": ("Corrosion", 100),     # acid -> corrosion meter
    "Cryo":     ("Temperature", -100),  # prism beam that freezes
    "Inferno":  ("Temperature", 100),   # prism beam that burns
    # Plasma (Temperature 50 + Corrosion 50) needs two states on one warhead -> handled at family build.
}

# Per-family Integrity (shield/EMP) auto-scale: the C# AreaDamage.IntegrityScale drains the victim's
# shield by `damage x Scale%` on hit, EXACTLY like PhysicalStateScale (auto-tracks the real post-armor
# damage, so no flat EMP number is ever hand-set and the ordering can't drift). Tesla-content law:
# Scale = round(100 x Tesla-parents / total-parents) -> pure Tesla 100, Storm (Tesla+Magic) 50,
# Quantum (Railgun+Laser+Tesla) 33. Emitted on BOTH the main AreaDamage and the _Percentage
# AreaDamagePercentage warheads (both extend AreaDamageWarhead and support IntegrityScale). The
# _ExtraDamage chip (SpreadDamage) has no IntegrityScale field but carries Tesla in DamageTypes for
# the passive INotifyDamage drain. The flat AffectsIntegrity warhead stays UPGRADE-only (a concrete
# bonus on top), so no template emits it. See PHYSICAL_STATE_SYSTEM.md.
FAMILY_INTEGRITY_SCALE = {
    "Tesla": 100,                          # pure Tesla = full shield-drain (the disable specialist)
    "Storm": 50,                          # Tesla+Magic -> 1/2
    "Quantum": 33,                        # Railgun+Laser+Tesla -> 1/3
    # ⚠ `Waveforce: 20` DELETED 2026-08-16 (maintainer order) — it could never fire.
    #
    # The drain rate is `(1 if the damage carries the `Tesla` type else 0) + IntegrityScale/100`
    # per point of damage, against a pool of 100% of max HP. Waveforce is the one integrity
    # family that never received `Tesla` in its DamageTypes, so it lost the 1:1 passive drain and
    # kept only its 20% scale — needing **5x the target's max HP** to reach the disable. The
    # target dies five times over first. Measured, not inferred (PSEUDO_ARMOR_AND_INTEGRITY §B).
    #
    # Maintainer: *"waveforce should remove the integrity damage entirely because it can never
    # actually reach a full integrity damage, so that it is not calculated in the balance formula
    # without any effect."* Exactly right, and the second half is the important half: a knob that
    # does nothing in play but is still read by the pricing model is worse than no knob at all,
    # because the weapon is charged for an effect it cannot deliver.
    #
    # The alternative — granting Waveforce the `Tesla` damage type — was NOT taken: that would
    # give a 3/5-kinetic blend the same EMP status as a Tesla coil, which is a design claim
    # nobody made. If Waveforce should have an EMP role it needs both halves, deliberately.
}

# Per-family DamageTypes override. Every family that affects Integrity (has IntegrityScale) MUST carry
# the `Tesla` DamageType so the Integrity trait's passive INotifyDamage drain fires. IntegrityScale
# values already account for the passive drain stacking. `ElectricityDeath` = tesla death animation.
# Families NOT listed use the default (Prone75Percent, TriggerProne, ExplosionDeath).
FAMILY_DAMAGE_TYPES = {
    "Tesla":   "Prone75Percent, TriggerProne, ElectricityDeath, Tesla",
    "Quantum": "Prone75Percent, TriggerProne, ElectricityDeath, Tesla",
    "Inferno": "Prone75Percent, TriggerProne, FireDeath, Incendiary",
    # Storm is handled at its own call site (Prone100Percent + Tesla).
}

# Per-family STATUS CONDITION (PHYSICAL_STATE_SYSTEM.md §6 decision 4). Some families mark the target
# with a short external condition on every hit instead of (or as well as) filling a PhysicalState meter.
# Sonic = `SonicDebuff` (^SonicDebuff in defaults.yaml: +50% incoming damage, -25% speed, blue tint) —
# the resonance that softens what the beam is standing on. Both numbers are DERIVED, never hand-picked:
#   Duration = duration_x_reload x ReloadDelay  -> continuous fire keeps the mark up, and it lapses a
#              couple of shots after the beam stops (the maintainer's "short duration, on hit only").
#   Range    = range_x_spread x the main warhead Spread -> the half-damage radius of the same blast.
# {family: (condition, duration_x_reload, range_x_spread)}
FAMILY_CONDITION = {
    "Sonic": ("SonicDebuff", 2, 2),
}

# Per-family InvalidTargets, emitted on the weapon AND its damaging warheads.
# WEAPON_TYPE_SYSTEM.md specifies Toxic as "no-op vs robotic": a gas kills people, so a drone
# or a robot walks through it untouched. That is a TARGETING rule, not a Versus value — W13
# rule 8 forbids expressing immunity as a zero multiplier, and `ToxinImmune` is the existing
# target-type the legacy ^ToxicWeapon already used for exactly this.
FAMILY_INVALID_TARGETS = {
    "Toxic": "wall, Mine, ToxinImmune",
}


def emit_condition(tag, cname, duration, rng, vt):
    """Emit the on-hit status-condition warhead (never damages, so it is price-neutral)."""
    return "\n".join([
        f"\tWarhead@{tag}_Debuff: GrantExternalCondition",
        f"\t\tCondition: {cname}",
        f"\t\tDuration: {duration}",
        f"\t\tRange: {rng}",
        f"\t\tValidRelationships: Enemy, Neutral",
        f"\t\tValidTargets: {vt}, Structure, wall"])


# Inheriting families: a thin child that inherits a parent family template and overrides ONLY the main
# warhead to add a PhysicalState (e.g. Cryo = Prism's anti-LIGHT beam + cold). Keeps the parent's Versus
# + warhead key. {name: (parent, PhysicalStateName, PhysicalStateScale, levels)}.
INHERIT_FAMILIES = {
    "Cryo":    ("Prism", "Temperature", -100, L3),   # a prism beam that also freezes (its "utility")
    "Inferno": ("Prism", "Temperature", +100, L3),   # a prism beam that also burns (heat ray)
}


def emit_inherit_family(name, parent, psn, pss, levels):
    """A full family that reuses the parent's ladder and adds a PhysicalState to the main + %-twin."""
    parent_cfg = WEAPONS[parent]
    order16 = build_order(parent_cfg[0], parent_cfg[1])
    vt = valid_targets(parent_cfg[2])
    dt = FAMILY_DAMAGE_TYPES.get(name)
    return family(name, order16, vt, levels, profile_family=parent,
                  **({"damage_types": dt} if dt else {}))


# Blend families: a NEW family whose Versus is the per-armor AVERAGE of parent families, plus a
# multi-state. Plasma = Flame x Chemical Versus + Temperature 50 + Corrosion 50 ("as close as possible
# to the flame + chemical combo"). {name: (parents, {StateName: Scale}, levels)}.
BLEND_FAMILIES = {
    "Plasma": (["Flame", "Chemical"], {"Temperature": 50, "Corrosion": 50}, L3),
    # Thermobaric = fuel-air incendiary blast: the per-armor AVERAGE of Demolition + Concussion +
    # Flame ("demolition + concussion + fire"). Heat = Flame 100 / 3 parents = 33 (per-parent-average
    # rule, Plasma-consistent). Collapses the thermobaric frankenstein weapons onto one warhead.
    "Thermobaric": (["Demolition", "Concussion", "Flame"], {"Temperature": 33}, L3),
    # Quantum = high-tech energy blend: per-armor AVERAGE of Railgun + Laser + Tesla (Heavy-only
    # parents extrapolated to L/M via the level step). Heat = Laser's 75 / 3 parents = 25 (only Laser
    # contributes a meter; Plasma-consistent per-parent averaging). EMP auto-scales via IntegrityScale
    # 33 (Tesla = 1/3 parents, FAMILY_INTEGRITY_SCALE); the ExtraDamage chip stays per-weapon.
    "Quantum": (["Railgun", "Laser", "Tesla"], {"Temperature": 25}, L3),
    # Element + delivery blends (maintainer 2026-08-10): per-armor AVERAGE of the element family and the
    # delivery family + the element's meter / 2 parents (50). FIRE = anti-light -> pairs with HE delivery
    # (better vs infantry/buildings); CHEMICAL = anti-armor -> pairs with AP delivery (better vs armor).
    "FireCannon":  (["Flame", "CannonHE"],    {"Temperature": 50}, L3),
    "FireMissile": (["Flame", "MissileHE"],   {"Temperature": 50}, L3),
    "ChemCannon":  (["Chemical", "CannonAP"], {"Corrosion": 50}, L3),
    "ChemMissile": (["Chemical", "MissileAP"],{"Corrosion": 50}, L3),
    # Waveforce = a resonant energy weapon: "a bit like a mix of the plasma warhead and the
    # quantum warheads" (maintainer 2026-08-16), adopted for the Japanese energy rifles —
    # which inherit `^WaveforceBulletWarhead` and were never railguns — and for the Protoss
    # photon cannons ("the protoss photon cannons should behave like the waveforce").
    #
    # Declared as the five UNDERLYING primitives rather than as a blend-of-blends, because
    # the blend machinery averages PARENT FAMILIES and nesting would need the parents built
    # first. Plasma = Flame+Chemical, Quantum = Railgun+Laser+Tesla, so the union is exact;
    # only the weighting differs (20% each here vs 25/25/16.7/16.7/16.7 for a true 50/50 of
    # the two blends). The thermal half gives it anti-infantry reach, the coherent-energy
    # half gives armour piercing and the anti-shield coupling.
    #
    # Meters follow the documented per-parent-average rule, same as Quantum's comment above:
    # Temperature = (Flame 100 + Laser 75) / 5 parents = 35; Corrosion = (Chemical 100) / 5 = 20.
    "Waveforce": (["Flame", "Chemical", "Railgun", "Laser", "Tesla"],
                  {"Temperature": 35, "Corrosion": 20}, L3),
}
# Fixed emission order for a blend (it has no single light/heavy direction).
BLEND_ARMOR_ORDER = ["None", "Flak", "Plate", "Heroic", "Scout", "Light", "Medium", "Heavy",
                     "Superheavy", "Wood", "Steel", "Concrete", "Fighter", "Bomber", "Helicopter", "Spaceship"]


def _family_main_pct(pname, level):
    """(main_dict, pct_dict) armor -> value for a standard family at a level (incl Shield).

    Reads the measured profile through the same path `family()` does, so a blend
    (Plasma, Thermobaric, Quantum, the Fire*/Chem* pairs, Storm) averages the
    parents' REAL profiles and cannot drift from what the parents themselves emit.
    """
    bl, d, air, lv = WEAPONS[pname]
    order = build_order(bl, d)
    step, mfloor, ptop = LEVELS[level]
    pfloor = ptop - 15
    main = reference_main(pname, order, level) or table(order, step, 100, mfloor, 100 + mfloor)
    # Blends average their PARENTS' profiles, and averaging their parents' Shield rows would
    # average the physics too — a Plasma (Flame+Chemical) would inherit a kinetic Shield.
    # Recompute from the blend's OWN rank instead, same single source as everything else.
    sv = shield_for(pname, level, main)
    if sv is not None:
        main = [("Shield", sv)] + [(a, v) for a, v in main if a != "Shield"]
    return (dict(main), dict(table(order, 1, ptop, pfloor, ptop + pfloor)))


def blend_versus(parents):
    """-> function(level) -> (main_rows, pct_rows) averaging the parents' Versus per armor."""
    def fn(level):
        mains, pcts = zip(*(_family_main_pct(p, level) for p in parents))
        n = len(parents)
        keys = ["Shield"] + BLEND_ARMOR_ORDER
        return ([(a, sum(m[a] for m in mains) // n) for a in keys],
                [(a, sum(p[a] for p in pcts) // n) for a in keys])
    return fn


# Storm = Ixian Tesla + Magic superweapon blend (maintainer 2026-08-10). The SUPER tier is the 3-way
# AVERAGE of Tesla_Super main + its full ExtraDamage chip + the new Magic — for BOTH the flat main and
# the %-twin. Every lower tier is that SUPER profile SCALED by WC[level]/WC[Super]. The %-magnitude lives
# in Versus (Damage stays the 1-per-2000 grid, so it scales with the weapon).
STORM_LEVELS = ["Light", "Medium", "Heavy", "Super"]


def storm_versus(level):
    tc_main, tc_pct = _family_main_pct("Tesla", "Super")
    extra, ef = CHIPS_LEVEL[("Tesla", "Super")], CHIP_FLOOR["Tesla"]
    keys = ["Shield"] + BLEND_ARMOR_ORDER
    base_main = {a: (tc_main[a] + extra.get(a, ef) + MAGIC_MAIN["Super"]) // 3 for a in keys}
    base_pct = {a: (tc_pct[a] + extra.get(a, ef) + MAGIC_PCT["Super"]) // 3 for a in keys}
    f = WC[level] / WC["Super"]                     # Super 1.0, Heavy .833, Medium .667, Light .5
    return ([(a, int(base_main[a] * f)) for a in keys],
            [(a, int(base_pct[a] * f)) for a in keys])


def _generate():
    argv = sys.argv[1:]
    if "--list" in argv:
        for nm, (bl, d, air, lv) in WEAPONS.items():
            print(f"{nm:11s} {macro_summary(bl):26s} dir={d:5s} air={str(air):5s} {','.join(lv)}")
        sys.exit(0)
    if "--orders" in argv:
        for nm, (bl, d, air, lv) in WEAPONS.items():
            if isinstance(bl, str) and bl in SPECIAL_MODE:
                extra = (", ".join(f"{x}:{FLAT_VALUES[x]}" for x in lv) if bl == "FLAT"
                         else "%-Versus " + ", ".join(f"{x}:{MAGIC_PCT[x]}" for x in lv))
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
    for nm, (parent, psn, pss, lv) in INHERIT_FAMILIES.items():
        if wanted and nm.lower() not in wanted:
            continue
        for level in lv:
            print(f"#   ^Warhead_{nm}_{level}: {WC[level]}  (inherits {parent})")
    for nm, (parents, states, lv) in BLEND_FAMILIES.items():
        if wanted and nm.lower() not in wanted:
            continue
        for level in lv:
            print(f"#   ^Warhead_{nm}_{level}: {WC[level]}  (blend of {'+'.join(parents)})")
    if not wanted or "storm" in wanted:
        for level in STORM_LEVELS:
            print(f"#   ^Warhead_Storm_{level}: {WC[level]}  (Tesla+Magic superweapon blend)")
    print()
    for nm, (bl, d, air, lv) in WEAPONS.items():
        if wanted and nm.lower() not in wanted:
            continue
        if nm in HAND_TUNED:  # hand-authored; never regenerate (would revert)
            continue
        vt = valid_targets(air, ground_only=(nm == "Melee"))
        spreads = FAMILY_SPREADS.get(nm, (400, 600, 800, 1000))
        falloffs = FAMILY_FALLOFFS.get(nm, DEFAULT_FALLOFFS)
        if isinstance(bl, str) and bl in SPECIAL_MODE:
            print(f"###### {nm}: {macro_summary(bl)} ######")
            print(family(nm, None, vt, lv, mode=SPECIAL_MODE[bl], spreads=spreads, falloffs=falloffs))
            print()
            continue
        order = build_order(bl, d)
        dt = FAMILY_DAMAGE_TYPES.get(nm)
        print(f"###### {nm}: {macro_summary(bl)} ({d}, air={air}) ######")
        print(family(nm, order, vt, lv, spreads=spreads, falloffs=falloffs, **({"damage_types": dt} if dt else {})))
        print()
    for nm, (parent, psn, pss, lv) in INHERIT_FAMILIES.items():
        if wanted and nm.lower() not in wanted:
            continue
        print(f"###### {nm}: inherits {parent} + PhysicalState {psn} {pss} ######")
        print(emit_inherit_family(nm, parent, psn, pss, lv))
        print()
    for nm, (parents, states, lv) in BLEND_FAMILIES.items():
        if wanted and nm.lower() not in wanted:
            continue
        vt = valid_targets(False)  # plasma is a ground weapon (like flame/chem)
        dt = FAMILY_DAMAGE_TYPES.get(nm)
        print(f"###### {nm}: blend of {'+'.join(parents)} + PhysicalStates {states} ######")
        print(family(nm, None, vt, lv, versus_override=blend_versus(parents), physical_states=states,
                     **({"damage_types": dt} if dt else {})))
        print()
    if not wanted or "storm" in wanted:
        print("###### Storm: Tesla_Super + Magic + TeslaSuperExtraDamage/5 (Super-anchored, scaled down) ######")
        print(family("Storm", None, valid_targets(False), STORM_LEVELS,
                     versus_override=storm_versus,
                     damage_types="Prone100Percent, TriggerProne, ElectricityDeath, Tesla"))
        print()



if __name__ == "__main__":
    # TWO-PHASE generation. Phase 1 emits every family independently (per-family logic,
    # which is all `shield_for` can see) with Shield in RAW centi-units; phase 2 compresses
    # the finished set onto [100, 400] and reassigns so no two templates share a value.
    # Both the band and uniqueness are properties of the whole SET, so neither can be
    # decided while generating one family at a time — and deriving the band here each run
    # is what keeps it from going stale when the profiles move (as S1 just did).
    import io
    from contextlib import redirect_stdout
    import shield_uniqueness
    _buf = io.StringIO()
    with redirect_stdout(_buf):
        _generate()
    sys.stdout.write(shield_uniqueness.apply(
        _buf.getvalue(), SHIELD_FLOOR_TARGET, SHIELD_CEIL_TARGET))
