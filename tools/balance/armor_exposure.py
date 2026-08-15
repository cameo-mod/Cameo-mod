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

⚠ **EXPOSURE HAS TWO FACTORS AND YOU NEED BOTH.** Maintainer, 2026-08-15:
*"Aircraft is inherently BETTER! Just because almost nothing can shoot it! So it
deserves this higher cost multiplier."* — correct, and it exposed a mistake in an
earlier version of this file, which filtered air down to `ValidTargets`-eligible
weapons and reported only the second factor:

    COVERAGE  — what share of the roster can target you AT ALL
    INTENSITY — among those that can, how hard they hit (mean Versus)
    EXPOSURE  = coverage x intensity

Only **43%** of live weapons can target Air. Reporting intensity alone answers
"when something CAN shoot me, how bad is it?" and throws away the survivability
that comes from three-fifths of the roster being unable to reach you at all —
which is most of why aircraft feel strong. Reporting the raw average instead is
also wrong, because a ground-only weapon still *declares* an air Versus it can
never apply. The product is the honest figure, and it is the one the price
formula should consume.
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


def weapon_reach(rs, name: str) -> tuple[bool, bool]:
    """(can hit ground, can hit air). OpenRA's default `ValidTargets` is
    `Ground, Water` — a weapon that never states the field is ground-only."""
    try:
        node = rs.resolve_weapon(name)
    except Exception:
        return (True, False)
    if node is None:
        return (True, False)
    for child in node.children:
        if child.key == "ValidTargets":
            value = child.value or ""
            return ("Ground" in value or "Water" in value, "Air" in value)
    return (True, False)


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
    reach_weight: collections.Counter = collections.Counter()
    total_weight = 0
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
        hits_ground, hits_air = weapon_reach(rs, name)
        total_weight += weight
        for armor in ARMORS:
            if armor not in versus:
                continue
            # COVERAGE: a weapon that cannot reach you is not part of your
            # exposure at all — but it still counts toward the roster total, which
            # is what makes being hard to reach worth something.
            reachable = hits_air if armor in LADDERS["AIR"] else hits_ground
            if not reachable:
                continue
            reach_weight[armor] += weight
            samples[armor].extend([versus[armor]] * weight)

    if not weapons_used:
        print("no weapon resolved a Versus block")
        return 1

    mode = "per definition" if args.unweighted else "weighted by deployment"
    print(f"armor exposure across {weapons_used} live weapons ({mode})")
    print("exposure = coverage x intensity — see the header for why both\n")

    intensity = {a: statistics.fmean(v) for a, v in samples.items() if v}
    coverage = {a: reach_weight[a] / total_weight for a in intensity}
    exposure = {a: coverage[a] * intensity[a] for a in intensity}
    overall = statistics.fmean(exposure.values())

    print(f"{'armor':12} {'coverage':>9} {'intensity':>10} {'EXPOSURE':>9} "
          f"{'vs roster':>10} {'HP factor':>10}")
    for ladder in LADDERS.values():
        for armor in ladder:
            if armor not in exposure:
                continue
            rel = exposure[armor] / overall
            # effective_hp = hp * 100 / exposure -> a factor relative to the roster
            print(f"{armor:12} {coverage[armor]:8.0%} {intensity[armor]:10.1f} "
                  f"{exposure[armor]:9.1f} {rel:9.2f}x {1 / rel:9.2f}x")
        print()
    print(f"roster-wide mean exposure: {overall:.1f}")
    hi = max(exposure, key=exposure.get)
    lo = min(exposure, key=exposure.get)
    print(f"most exposed : {hi} ({exposure[hi]:.1f})  -> cheapest per HP")
    print(f"least exposed: {lo} ({exposure[lo]:.1f})  -> most expensive per HP")
    print("\n⚠ This is measured from the CURRENT Versus values, which W13 is about "
          "to rewrite.\n  Derive a price factor from it only AFTER the warhead "
          "rebuild lands.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
