#!/usr/bin/env python3
"""W13 step 4b — Versus profiles for the families Cameo INVENTED.

Ten `^Warhead_*` families have no cross-mod equivalent, so `propose_family_profiles.py`
refuses to speak for them: `Flak`, `Chemical`, `Melee`, `Arrow`, `Magic`,
`Demolition`, `Concussion`, `Sonic`, `Railgun`, `Nuclear`. Three are already
designed by other means — `Sonic` is FLAT mode, `Magic` is PCT mode, `Nuclear` is
`HAND_TUNED` — leaving **seven sloped ladders** that were still shipping the even
ramp the shape law abolished.

    python tools/balance/design_invented_profiles.py            # print
    python tools/balance/design_invented_profiles.py --write    # + json + markdown

**Invented is not arbitrary.** Only two numbers per family are a design choice, and
both are constrained by something measured:

  1. **SHARPNESS** — where the family sits in the field's `2x / 4x / 8x` target band
     (DESIGN.md §12.0 rule 5). The seven are placed so their own median lands on the
     field's centre (4x) and none exceeds what the MEASURED families reached (6.1x),
     so "the families we invented" cannot quietly be sharper than "the families we
     measured".
  2. **CLIFF POSITION + WIDTH** — where the profile falls off. Measured over 1350
     reference profiles carrying 6+ armors:

         step regularity (CV of consecutive gaps; 0.00 = a perfectly even ramp)
             p10 0.78 · p25 0.97 · MEDIAN 1.25 · p75 1.58 · p90 2.06
             profiles with CV < 0.30 (even-ish):  0%   <- not one, in 1350
             profiles with CV > 1.00 (one cliff): 73%

     The even ramp these families ship today scores **0.00** — the single shape the
     field never produces, in any mod, at any tier. That is the finding that makes
     this item worth doing rather than a matter of taste.

The ORDER is not a choice at all: `gen_weapon_template.build_order()` already fixes
which armor takes which rank from the family's macro priority and light/heavy
direction. This only decides the magnitude curve laid onto that order.

Everything else comes from the shared laws in `aggregate_archetype`: the level slope
is measured (Light 1.00 · Medium 1.07 · Heavy 0.75 — heavier platforms are flatter,
which DESIGN §12.0a found independently), the derived armors are products, the
window is `[10, 200]`, and no two values may be equal.
"""
from __future__ import annotations

import argparse
import json
import math
import pathlib
import statistics
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "reference"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))
import aggregate_archetype as ag  # noqa: E402
import gen_weapon_template as gwt  # noqa: E402

OUT_JSON = ROOT / "docs" / "design" / "invented_family_profiles.json"
OUT_MD = ROOT / "docs" / "design" / "INVENTED_WARHEAD_FAMILIES.md"

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

# Measured on the 10 families the corpus DOES cover: a family's spread relative to
# its own Light tier. Heavier platforms are flatter — the same effect DESIGN §12.0a
# measured from the other direction (laser lethality vs heavy armour climbing with
# the platform). Derived, not chosen, so the invented families inherit the real one.
LEVEL_SLOPE = {"Light": 1.00, "Medium": 1.07, "Heavy": 0.75, "Super": 0.70}

# family -> (sharpness at Light, RUNGS it really works against, cliff width, why)
#
# ⚠ **The cliff position is DERIVED, not picked.** It is `rungs / 16` — the share of
# the 16-armor order the weapon genuinely threatens. That turns the shape parameter
# into a statable claim about the weapon ("a fist works on unarmoured infantry, and
# that is two rungs") instead of a number tuned until the table looked nice, which is
# what an earlier draft of this file did. An arrow came out dealing 190 against Plate
# there — 95% of its peak against the armour it is least able to defeat — purely
# because the cliff had been placed past the whole infantry block by eye.
#
# `width` is the one remaining feel knob: how BINARY the weapon is. Fragmentation and
# penetration either defeat the armour or do nothing (narrow); blast and corrosion
# fall away gradually (wide).
DESIGNS: dict[str, tuple[float, int, float, str]] = {
    "Concussion": (2.2, 12, 0.06,
                   "A shockwave does not care what it is hitting: overpressure crushes "
                   "infantry, vehicles and structures on the ground at much the same "
                   "rate. Only aircraft escape it, by not being on the ground. So the "
                   "profile is a long plateau across all twelve ground rungs and then "
                   "ONE wall at the air block — the flattest of the seven by design, "
                   "and the set's deliberate generalist."),
    "Chemical":   (3.0, 9, 0.12,
                   "Corrosive agent: attrition rather than a hard counter. It works on "
                   "the nine interleaved infantry+vehicle rungs, is resisted by sealed "
                   "structures, and aircraft simply fly through it. Plateau over the "
                   "top block, then a moderate fall — a weapon that grinds rather than "
                   "one that deletes."),
    "Demolition": (3.5, 6, 0.10,
                   "Satchel charges and shaped demolition: it levels structures and "
                   "kills infantry caught in the blast, but a moving vehicle is a poor "
                   "target for something you have to place. The building block is only "
                   "three rungs, so the top plateau is short and the cliff arrives "
                   "mid-profile. Cannot touch aircraft at all."),
    "Arrow":      (4.0, 3, 0.08,
                   "Bolts and arrows: lethal to unarmoured infantry, able to bring down "
                   "a light flyer, and they glance off plate, hulls and stone. An early "
                   "cliff — the transition from flesh to armour is exactly where a "
                   "projectile with no explosive stops working."),
    "Flak":       (4.5, 7, 0.06,
                   "Timed shrapnel bursts. Devastating to the four aircraft rungs and to "
                   "exposed infantry, then a sharp wall: fragmentation either defeats "
                   "the armour or it does nothing at all, with very little in between. "
                   "Cliff dead centre and narrow, because that binary is the weapon's "
                   "whole character."),
    "Railgun":    (5.5, 3, 0.06,
                   "A hypervelocity slug punches through the heaviest armour there is — "
                   "and OVERPENETRATES everything soft. A rod that passes clean through "
                   "a rifleman spends almost none of its energy on him, which is why "
                   "this is anti-heavy rather than universally lethal. Early narrow "
                   "cliff, and it cannot lead an aircraft. Heavy tier only."),
    "Melee":      (6.0, 2, 0.06,
                   "Claws, blades and fists: the most specialised weapon in the game. "
                   "Lethal to unarmoured infantry, effectively useless against a hull, "
                   "and it cannot reach aircraft in any sense. The earliest and "
                   "narrowest cliff of the seven, and the sharpest sharpness the target "
                   "band allows without a maintainer ruling."),
}

# Families that are invented but NOT designed here, and why — recorded so a reader
# does not read their absence as an oversight.
EXCLUDED = {
    "Sonic": "FLAT mode — uniform against every armor by design (the anti-low-HP "
             "generalist). Its 1.0x spread is a deliberate exception to the target band.",
    "Magic": "PCT mode — tiny uniform flat plus a large percentage of max HP (the "
             "%-equalizer / giant-killer). Also deliberately 1.0x.",
    "Nuclear": "HAND_TUNED — ten expanding rings and a bespoke AreaDamagePercentage "
               "subclass the generator does not emit at all.",
}


def curve(ranks: int, cliff: float, width: float) -> list[float]:
    """A monotone 1 -> 0 shape with a plateau, a cliff and a tail.

    Logistic in the rank axis, renormalised so it starts at exactly 1 and ends at
    exactly 0. `cliff` slides the fall along the profile and `width` controls how
    abrupt it is, which is precisely the pair of knobs the corpus measurement says
    a real profile has and an even ramp does not.
    """
    def s(t: float) -> float:
        return 1.0 / (1.0 + math.exp(-t / width))
    hi, lo = s(cliff), s(cliff - 1.0)
    span = hi - lo or 1.0
    return [(s(cliff - i / (ranks - 1)) - lo) / span for i in range(ranks)]


def _build(order: list[str], shape: list[float], nominal: float) -> dict[str, float]:
    # 1 .. nominal, so max/min is exactly `nominal` before anything else moves.
    raw = {armor: 1.0 + f * (nominal - 1.0) for armor, f in zip(order, shape)}
    # The normalisation law: a profile's MEDIAN is 100 (DESIGN §12.0 rule 1).
    centre = statistics.median(raw.values()) or 1.0
    shaped = ag.fit_window({a: v * ag.NORMALISE_REFERENCE / centre for a, v in raw.items()})
    shaped.update(ag.derive_armors(shaped))
    return ag.enforce_distinct(shaped)


def achieved(profile: dict[str, float]) -> float:
    """Spread of the 16 LADDER armors — the number the band actually governs.

    Derived armors are excluded: they are products and DESIGN §12.0 rule 5 waives
    the band for them specifically.
    """
    values = [v for k, v in profile.items() if k not in ag.DERIVED_ARMORS]
    return max(values) / min(values) if values and min(values) > 0 else 1.0


def profile_for(family: str, level: str) -> tuple[dict[str, float], float]:
    """One designed profile, through exactly the same laws a measured one obeys.

    ⚠ **The design ratio and the SHIPPED ratio are different quantities**, and an
    earlier version of this file reported the first while shipping the second.
    `enforce_distinct` has to manufacture separation wherever a profile's tail is
    packed, and a sharp early cliff packs it hard: at a nominal 6.0x, ten of Melee's
    sixteen values sat within 2 points of the bottom, so the gap-2 rule pushed the
    floor from 40 down to 21 and shipped **9.4x** — outside the band, from a design
    that claimed to be inside it.

    So the nominal is SOLVED rather than set: bisect it until what survives the
    no-ties rule equals the intended sharpness. Monotone in the nominal, so the
    search is well behaved.
    """
    sharpness, rungs, width, _why = DESIGNS[family]
    intended = max(ag.FIELD_RATIO_LOW,
                   min(ag.FIELD_RATIO_HIGH, sharpness * LEVEL_SLOPE[level]))
    blocks, direction, _air, _levels = gwt.WEAPONS[family]
    order = gwt.build_order(blocks, direction)
    shape = curve(len(order), rungs / len(order), width)

    low, high = 1.02, 20.0
    best = _build(order, shape, intended)
    for _ in range(40):
        mid = (low + high) / 2
        candidate = _build(order, shape, mid)
        got = achieved(candidate)
        best = candidate
        if abs(got - intended) < 0.02:
            break
        if got > intended:
            high = mid
        else:
            low = mid
    return best, round(achieved(best), 2)


def build() -> dict:
    families: dict[str, dict] = {}
    for family in DESIGNS:
        levels = gwt.WEAPONS[family][3]
        out = {}
        for level in levels:
            sharpness, rungs, width, why = DESIGNS[family]
            profile, got = profile_for(family, level)
            out[level] = {
                "origin": "designed",
                "sharpness_intended": round(max(ag.FIELD_RATIO_LOW,
                                                min(ag.FIELD_RATIO_HIGH,
                                                    sharpness * LEVEL_SLOPE[level])), 2),
                "sharpness_shipped": got,
                "rungs": rungs,
                "width": width,
                "profile": {a: round(v, 1) for a, v in sorted(profile.items())},
            }
        families[family] = out
    return {
        "_generated_by": "tools/balance/design_invented_profiles.py --write",
        "_what": ("Versus profiles for the families Cameo INVENTED — no cross-mod "
                  "equivalent exists, so these are designed, not measured. Kept in a "
                  "SEPARATE file from docs/reference/family_profiles.json so the two "
                  "provenances can never be confused by a later reader."),
        "_constrained_by": {
            "target_band": [ag.FIELD_RATIO_LOW, ag.FIELD_RATIO_HIGH],
            "window": [ag.ABSOLUTE_FLOOR, ag.NORMALISE_CEILING],
            "level_slope": LEVEL_SLOPE,
            "order": "gen_weapon_template.build_order() — not a choice",
        },
        "_excluded": EXCLUDED,
        "families": families,
    }


def markdown(payload: dict) -> str:
    armors = ag.CAMEO16
    lines = [
        "# The INVENTED warhead families — design sheet",
        "",
        "Generated by `tools/balance/design_invented_profiles.py --write`. These seven",
        "families have **no cross-mod equivalent**, so unlike the ten in",
        "`docs/reference/family_profiles.md` their numbers are designed rather than",
        "measured. This sheet is the reasoning, so a later reader can tell the two apart.",
        "",
        "## What is a choice here, and what is not",
        "",
        "| | source |",
        "|---|---|",
        "| armor ORDER | `gen_weapon_template.build_order()` — the ordering law, not a choice |",
        "| target band `2x / 4x / 8x` | measured, 2402 individual reference warheads |",
        "| level slope (Light 1.00 · Medium 1.07 · Heavy 0.75) | measured on the 10 corpus families |",
        "| window `[10, 200]` | maintainer ruling |",
        "| derived armors (`Heroic`, `Airborne`) | products, DESIGN §12.0b |",
        "| **sharpness + cliff position/width** | **the design decision — this file** |",
        "",
        "## Why the even ramp had to go",
        "",
        "Step regularity across 1350 reference profiles carrying 6+ armors, as the",
        "coefficient of variation of consecutive gaps (`0.00` = a perfectly even ramp):",
        "",
        "| p10 | p25 | median | p75 | p90 |",
        "|--:|--:|--:|--:|--:|",
        "| 0.78 | 0.97 | **1.25** | 1.58 | 2.06 |",
        "",
        "**0% of them score under 0.30**, and 73% score over 1.00 — a profile dominated",
        "by one cliff. The even ramp these families shipped scores **0.00**: the single",
        "shape no mod produces at any tier. That is the measurement behind this change.",
        "",
        "## The seven",
        "",
        "| family | sharpness (Light) | rungs it threatens | cliff at | width | reasoning |",
        "|---|--:|--:|--:|--:|---|",
    ]
    for family, (sharp, rungs, width, why) in DESIGNS.items():
        lines.append(f"| `{family}` | {sharp}x | {rungs} of 16 | {rungs/16:.2f} | {width} | {why} |")
    median = statistics.median([d[0] for d in DESIGNS.values()])
    lines += [
        "",
        f"Their own median sharpness is **{median}x** — the field's centre — and the "
        "sharpest is 6.0x, which does not exceed what the MEASURED families reached "
        "(6.1x). The families we invented therefore cannot be quietly more specialised "
        "than the families we measured.",
        "",
        "## Not designed here",
        "",
        "| family | why |",
        "|---|---|",
    ]
    for family, why in EXCLUDED.items():
        lines.append(f"| `{family}` | {why} |")
    lines += ["", "## The profiles", ""]
    for family, levels in payload["families"].items():
        lines += [f"### {family}", "",
                  "| level | sharpness | " + " | ".join(armors) + " |",
                  "|---|--:|" + "--:|" * len(armors)]
        for level, entry in levels.items():
            cells = [f"{entry['profile'].get(a, float('nan')):g}" for a in armors]
            lines.append(f"| {level} | {entry['sharpness_shipped']}x | " + " | ".join(cells) + " |")
        lines.append("")
    return chr(10).join(lines)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--write", action="store_true", help="write the json + markdown sheet")
    args = ap.parse_args()

    payload = build()
    key = ("None", "Flak", "Plate", "Heroic", "Scout", "Medium", "Superheavy",
           "Concrete", "Helicopter")
    for family, levels in payload["families"].items():
        print(f"\n{family}")
        for level, entry in levels.items():
            profile = entry["profile"]
            shown = " ".join(f"{a[:4]}={profile[a]:.0f}" for a in key if a in profile)
            print(f"   {level:7} want {entry['sharpness_intended']:.1f}x "
                  f"got {entry['sharpness_shipped']:.1f}x  {shown}")
    if args.write:
        OUT_JSON.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
        OUT_MD.write_text(markdown(payload) + "\n", encoding="utf-8")
        print(f"\nwrote {OUT_JSON.relative_to(ROOT)}")
        print(f"wrote {OUT_MD.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
