#!/usr/bin/env python3
"""How exposed is each armor type to the weapons that actually exist?

Maintainer's hypothesis (2026-08-15): *"if there exist a lot of weapons with high
anti-heavy armor values that means the average versus value against heavy is
high … which means classes with Heavy armor should get a low K multiplier
because they are being countered by a lot of weapons. The more a certain class is
countered by overall weapons the cheaper the unit class should be."*

The logic is sound and this measures it. For every armor type, the average
`Versus` across the weapons the game actually fields is that armor's **exposure**:
high exposure = more of the roster hurts you = you are worth less.

    python tools/balance/armor_exposure.py
    python tools/balance/armor_exposure.py --unweighted    # per weapon DEFINITION

⚠ **Weighted by DEPLOYMENT, not by weapon count.** Counting weapon definitions
lets a one-off superweapon warhead outvote the rifle every faction builds. Each
weapon is weighted by the number of actors that field it, so the average answers
"what does a unit with this armor actually meet on the field?" — which is the only
version of the question that affects balance. `--unweighted` shows the other view
for comparison; where the two disagree, the armor is exposed to many RARE weapons
or a few common ones, and that difference is itself worth seeing.

⚠ **Where this belongs in the formula.** Not in K. K is the WEAPON-quality
coefficient — offence. Armor exposure is defence, and it is mathematically a
multiplier on effective health: a unit meeting an average `Versus` of 50 has
twice the effective HP of one meeting 100. So the clean placement is an armor
factor on the **HP term**, `effective_hp = hp * 100 / exposure`, which the price
formula already consumes. Folding it into K instead would conflate how good a
unit's gun is with how tough its hull is, and the two are independent.

⚠ **Run this AFTER the warhead rebuild (W13), never before.** Exposure is
measured from the very Versus values W13 rewrites, so deriving a price factor now
would bake in the profiles we are about to replace.

⚠ **THE AIR COLUMNS ARE FILTERED, the ground ones are not — and they must be.**
Only **43%** of live weapons (990 of 2323) can target Air at all. A ground-only
weapon still *declares* a Versus against Fighter/Bomber/Helicopter/Spaceship, and
counting those declarations drags the air average down until aircraft look almost
untouchable. What a flyer actually meets is the average over weapons that CAN
shoot it, so air exposure is computed over `ValidTargets`-eligible weapons only.
Without this filter the air armors measured 39-48 against a roster mean of 62,
which would have priced aircraft as if the whole roster could barely scratch them.
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import statistics
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

LADDERS = {
    "INF": ["None", "Flak", "Plate"],
    "HYBRID": ["Heroic"],
    "VEH": ["Scout", "Light", "Medium", "Heavy", "Superheavy"],
    "BLD": ["Wood", "Steel", "Concrete"],
    "AIR": ["Fighter", "Bomber", "Helicopter", "Spaceship"],
}
ARMORS = [a for lad in LADDERS.values() for a in lad]
# Not armor: the shield layer and the gating pseudo-types.
NON_ARMOR = {"shield", "hazmat", "reflector"}
# Twins are not what a unit "meets" — they are fractions of the main warhead.
TWIN_SUFFIXES = ("_percentage", "_extradamage", "_friendlyfire")


def weapon_versus(rs, name: str) -> dict[str, float] | None:
    """The MAIN warhead's Versus for one weapon, resolved through inheritance."""
    try:
        node = rs.resolve_weapon(name)
    except Exception:
        return None
    if node is None:
        return None
    for child in node.children:
        key = child.key
        if not key.startswith("Warhead@") or key.lower().endswith(TWIN_SUFFIXES):
            continue
        for grand in child.children:
            if grand.key != "Versus":
                continue
            out = {}
            for leaf in grand.children:
                if leaf.key.lower() in NON_ARMOR:
                    continue
                try:
                    out[leaf.key] = float(leaf.value)
                except (TypeError, ValueError):
                    continue
            if out:
                return out
    return None


def weapon_targets_air(rs, name: str) -> bool:
    """Can this weapon shoot up at all? Absent `ValidTargets` = ground default."""
    try:
        node = rs.resolve_weapon(name)
    except Exception:
        return False
    if node is None:
        return False
    for child in node.children:
        if child.key == "ValidTargets":
            return "Air" in (child.value or "")
    return False


def deployment_counts(rs, model) -> collections.Counter:
    """How many actors field each weapon — the weight."""
    counts = collections.Counter()
    # `Model.roster(faction)` is per-faction; the union over every real faction is
    # the deployed set. Actors shared by several factions are counted once —
    # prevalence here means "how many distinct units carry this gun", not how many
    # factions can build them.
    actors = set()
    for faction in model.real_factions():
        actors |= model.roster(faction.internal)
    for actor in sorted(actors):
        try:
            resolved = rs.resolve(actor)
        except Exception:
            continue
        for child in resolved.children:
            if child.key.split("@")[0] != "Armament":
                continue
            for grand in child.children:
                if grand.key == "Weapon" and grand.value:
                    counts[grand.value.strip()] += 1
    return counts


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--unweighted", action="store_true",
                    help="one vote per weapon DEFINITION instead of per deployment")
    args = ap.parse_args()

    from cameo_model import Model
    model = Model()
    rs = model.rs

    weights = collections.Counter() if args.unweighted else deployment_counts(rs, model)
    samples: dict[str, list[float]] = {a: [] for a in ARMORS}
    weapons_used = 0
    for name in rs.weapons:
        if name.startswith("^"):
            continue
        versus = weapon_versus(rs, name)
        if not versus:
            continue
        weight = 1 if args.unweighted else weights.get(name, 0)
        if weight <= 0:
            continue                       # defined but nothing fields it
        weapons_used += 1
        can_hit_air = weapon_targets_air(rs, name)
        for armor in ARMORS:
            if armor not in versus:
                continue
            # A weapon that cannot shoot up is not part of what a flyer meets.
            if armor in LADDERS["AIR"] and not can_hit_air:
                continue
            samples[armor].extend([versus[armor]] * weight)

    if not weapons_used:
        print("no weapon resolved a Versus block")
        return 1

    mode = "per definition" if args.unweighted else "weighted by deployment"
    print(f"armor exposure — mean Versus across {weapons_used} live weapons ({mode})\n")
    means = {a: statistics.fmean(v) for a, v in samples.items() if v}
    overall = statistics.fmean(means.values())
    print(f"{'armor':12} {'n':>7} {'mean':>7} {'median':>7}  {'vs roster':>10}  "
          f"{'HP factor':>9}")
    for macro, ladder in LADDERS.items():
        for armor in ladder:
            vals = samples[armor]
            if not vals:
                continue
            mean = means[armor]
            rel = mean / overall
            # effective_hp = hp * 100 / exposure -> a factor relative to the roster
            print(f"{armor:12} {len(vals):7} {mean:7.1f} "
                  f"{statistics.median(vals):7.1f}  {rel:9.2f}x  {1 / rel:8.2f}x")
        print()
    print(f"roster-wide mean Versus: {overall:.1f}")
    hi = max(means, key=means.get)
    lo = min(means, key=means.get)
    print(f"most exposed : {hi} ({means[hi]:.1f})  -> cheapest per HP")
    print(f"least exposed: {lo} ({means[lo]:.1f})  -> most expensive per HP")
    print("\n⚠ This is measured from the CURRENT Versus values, which W13 is about "
          "to rewrite.\n  Derive a price factor from it only AFTER the warhead "
          "rebuild lands.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
