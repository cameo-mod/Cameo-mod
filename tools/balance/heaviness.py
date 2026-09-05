#!/usr/bin/env python3
"""heaviness.py — DESIGN §12.0i's continuous heaviness bell, as one implementation.

    from heaviness import belled, mu_of, centre_of_mass, BUCKET, LO, SIGMA

⭐ WHY THIS MODULE EXISTS. The bell was written and proven inside
`tools/audit/audit_heaviness_bell.py`, deliberately BEFORE the generator used it, so
that step 5 of `WEAPON_HEAVINESS.md` §9.6 would land against a test that already
existed. Wiring the generator up by COPYING those functions would create two
implementations of a binding law that can silently diverge — precisely the failure
the project keeps finding elsewhere. So the model moves here and both sides import
it: the audit that checks the law and the generator that applies it now cannot
disagree.

THE MODEL (DESIGN §12.0i, derivation in WEAPON_HEAVINESS.md §9)

    x(armor)      = ONE GLOBAL 13-slot scale, 0..2, step 1/6
    mu(family, h) = ( h + centre_of_mass(base_profile) ) / 2
    curve(x)      = LO + (1 - LO) * exp( -(x - mu)^2 / (2 * SIGMA^2) )
    Versus(a, h)  = base(a) * curve(x(a))   then renormalised   then RANK-RESTORED

⛔ THE RANK RESTORE IS NOT OPTIONAL. It is what makes §12.0d's "can never invert"
true: the bell is applied to the VALUES and each armor is then given back the RANK
it held within its own ladder. An earlier version of the audit skipped it and
compared only a ladder's first and last rung, missing **127** internal reorderings
across 60 family/ladder pairs and reporting two endpoint flips as permanent
exceptions. With the restore there are zero reorderings anywhere.

The restore permutes values WITHIN a ladder, so the multiset is unchanged and the
weighted mean survives it — which is what keeps §12.0i's price invariance.

⛔ DIRECTION IS ONLY MEANINGFUL WITHIN ONE LADDER. Comparing `None` (INF) to
`Superheavy` (VEH) is a cross-ladder relation the tilt is DESIGNED to change.
"""

from __future__ import annotations

import math
import statistics

# --------------------------------------------------------------------------- #
# Constants — DESIGN §12.0i, re-ruled 2026-08-24
# --------------------------------------------------------------------------- #

# 0.667 = 1/TILT_RATIO, the same 1.5x span the discrete `class_tilt` already uses,
# so collapsing three templates into one PRESERVES today's differentiation instead
# of flattening it. The earlier 0.80 was measured against the retired
# family-anchored peak and came out gentler than the shipped tilt.
LO = 0.667

# 0.75 gives the strongest consistent tilt. Below ~0.5 the effect starts to INVERT,
# because only the rung nearest the peak still moves and the ladder's spread stops
# changing.
SIGMA = 0.75

# Off the axis by §12.0i's fourth ruling, each for its own reason:
#   §12.0c  Shield is its own compressed ladder, not a normal armor
#   §12.0e  the ALL-CAPS platings REPLACE the class armor rather than sit on the axis
#   §12.0b  Heroic is DERIVED, recomputed from the finished profile
OFF_AXIS = frozenset({"Shield", "Heroic", "HAZMAT", "COMPOSITE", "BLAST",
                      "REFLECTOR", "ARMOR"})

# Lightest -> heaviest. The canonical set, matching gen_weapon_template.LADDERS.
LADDERS = {
    "INF": ["None", "Flak", "Plate"],
    "VEH": ["Scout", "Light", "Medium", "Heavy", "Superheavy"],
    "BLD": ["Wood", "Steel", "Concrete"],
    "AIR": ["Fighter", "Bomber", "Helicopter", "Spaceship"],
}

# ⭐ THE x-AXIS — one global scale, 13 evenly spaced slots, step 1/6.
#
# EVERY LADDER IS CENTRED EXACTLY ON 1.000, which is the property the whole model
# rests on: h=1 means "medium" in all four domains at once, h=0 the lightest rung
# of every ladder, h=2 the heaviest.
#
# ⛔ The three-way tie at 1.0 is DELIBERATE and is the only tie. Flak, Medium and
# Steel sit in three DIFFERENT ladders and the restore is per-ladder, so they never
# compete; de-tying them moves no row by more than 0.89%. A tie WITHIN one ladder
# stays forbidden — that was the 2026-08-24 bucket bug, where Bomber and Helicopter
# shared a coordinate and heaviness could not tell them apart at all.
AXIS_ORDER = [["Scout"], ["None"], ["Fighter"], ["Light"], ["Wood"], ["Bomber"],
              ["Medium", "Flak", "Steel"],
              ["Helicopter"], ["Concrete"], ["Heavy"], ["Spaceship"], ["Plate"],
              ["Superheavy"]]

BUCKET = {a: round(i * 2.0 / (len(AXIS_ORDER) - 1), 4)
          for i, slot in enumerate(AXIS_ORDER) for a in slot}

# Which ladder each armor belongs to. Derived from LADDERS so the two cannot drift.
LADDERS_OF = {a: name for name, rungs in LADDERS.items() for a in rungs}


def centre_of_mass(profile):
    """Where on the 0..2 axis the family's strength sits, weighted by its Versus."""
    pairs = [(BUCKET[a], v) for a, v in profile.items() if a in BUCKET and v > 0]
    total = sum(v for _x, v in pairs)
    return sum(x * v for x, v in pairs) / total if total else None


def mu_of(profile, h):
    """§12.0i: the peak is the BLEND of requested heaviness and the family's own mass.

    Ruled 2026-08-24, replacing `centre_of_mass + SHIFT * (h - 1)`. That form
    anchored the peak to the family and let `h` nudge it by an eighth of the scale,
    so h=1 did not mean "medium" — it meant "wherever this family already sits".
    A pure `mu = h` was rejected for inverting 26 of 42 families, but that was
    measured BEFORE the rank restore existed; re-measured with the restore, `mu = h`
    reorders nothing at any sigma. The blend was ruled anyway, so a family keeps a
    formal say in where its peak sits on top of the restore.
    """
    com = centre_of_mass(profile)
    return None if com is None else (h + com) / 2.0


def curve(x, mu):
    """The bell at `x` for a peak at `mu`. Bottoms out at LO, never at zero."""
    return LO + (1 - LO) * math.exp(-((x - mu) ** 2) / (2 * SIGMA ** 2))


def belled(profile, mu):
    """The FULL §12.0i pipeline: bell at `mu`, renormalise, restore rank per ladder.

    `profile` is {armor: versus} and is not mutated. Armors absent from the axis
    pass through untouched, which is how the off-axis set keeps its own laws.
    """
    out = {}
    for armor, value in profile.items():
        x = BUCKET.get(armor)
        out[armor] = value if x is None else value * curve(x, mu)

    # Renormalise to the profile's own mean — this is what preserves MEAN-100 and,
    # with it, §12.0i's price invariance.
    before = statistics.mean(profile.values()) if profile else 0.0
    after = statistics.mean(out.values()) if out else 0.0
    if after:
        out = {a: v * before / after for a, v in out.items()}

    # ⛔ The rank restore. Each armor is given back the RANK it held within its own
    # ladder, so the bell can reshape magnitudes but can never reorder a ladder.
    for rungs in LADDERS.values():
        present = [a for a in rungs if a in profile and a in out]
        if len(present) < 2:
            continue
        for armor, value in zip(sorted(present, key=lambda a: profile[a]),
                                sorted(out[a] for a in present)):
            out[armor] = value
    return out


def profile_at(profile, h):
    """Convenience: the family's profile at heaviness `h`, or None if unjudgeable."""
    mu = mu_of(profile, h)
    return None if mu is None else belled(profile, mu)
