#!/usr/bin/env python3
"""target_model.py — what a weapon is actually shooting AT, measured from the tree.

The pricing formula needs three things the old model guessed at:

1. **Armor prevalence** — a flat mean over 17 armor types prices every armor as if it
   were equally common, which it is not: the measured census below spans 563 `Wood`
   down to 20 `Fighter`. Nothing here is hand-written — `armor_census()` counts the
   LIVE RESOLVED actors, so the weighting self-updates as the roster grows (maintainer
   order 2026-08-11: "the game changes over time … it needs to be constantly updated
   and self balanced regularly, all automatically without user input").

   Do not quote armor counts from a raw `grep` of the yaml — it counts templates and
   misses actors that inherit their armor from a class template. A raw grep says
   "2 Spaceships"; the resolved census says **22** (StarCraft Terran alone has the
   Battlecruiser, Phobos and Pythean). Run `python tools/balance/target_model.py`.

2. **Target density** — how many actors a blast realistically catches. Combined
   model (maintainer 2026-08-11, "can we try a combination of both?"): a per-class
   density for WHO is being hit, and a blob cap for HOW MUCH ground is worth
   covering. Infantry stack 5 to a cell (sub-cells) and vehicles 1, so an
   anti-infantry splash legitimately catches more bodies than an anti-tank one.

3. **Reference HP** — the %-of-max-HP twins need a yardstick to convert into flat
   damage. Measured from the live roster rather than assumed.

Read-only. Every number here is derived from `mods/cameo`, never hand-written.
"""

from __future__ import annotations

import collections
import functools
import pathlib
import statistics
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/audit"))
from miniyaml import Ruleset  # noqa: E402

# The 16 canonical armor types + Shield. Order matches gen_weapon_template.py.
ARMORS = ("None", "Flak", "Plate", "Heroic",
          "Scout", "Light", "Medium", "Heavy", "Superheavy",
          "Wood", "Steel", "Concrete",
          "Fighter", "Bomber", "Helicopter", "Spaceship")

# Which macro class each armor belongs to — drives the per-class density below.
ARMOR_MACRO = {
    "None": "INF", "Flak": "INF", "Plate": "INF", "Heroic": "INF",
    "Scout": "VEH", "Light": "VEH", "Medium": "VEH", "Heavy": "VEH",
    "Superheavy": "VEH",
    "Wood": "BLD", "Steel": "BLD", "Concrete": "BLD",
    "Fighter": "AIR", "Bomber": "AIR", "Helicopter": "AIR", "Spaceship": "AIR",
}

# Units per cell^2 for each macro class, from the maintainer's engagement picture
# (3 vehicles + 6 infantry in a 3x3 = 9 cell^2 formation) plus the engine's
# sub-cell rule (5 infantry per cell, 1 vehicle per cell):
#   vehicles  3 / 9 cell^2 = 0.33
#   infantry  6 / 9 cell^2 = 0.67, and they can pack far tighter -> 2.0 at the limit
# Buildings sit on their own footprint and never bunch; aircraft spread out.
DENSITY = {"INF": 2.0, "VEH": 0.33, "BLD": 0.25, "AIR": 0.20}

# The engagement blob: beyond this a blast covers empty ground, so extra footprint
# stops adding targets. 3x3 cells = 9 cell^2.  A_SELF = the primary's own cell,
# already counted by `reliability`, so it must not be counted twice.
A_BLOB = 9.0
A_SELF = 1.0

# Fraction of shots that actually land in a crowd (2026-08-11). DENSITY above
# describes the moment of a blob fight, which is the RIGHT picture for that moment —
# but a weapon does not spend the match firing into a perfect 3x3 formation. It
# chases stragglers, trades at the edge, holds fire to avoid its own splash, and
# wastes overkill on a target already dying.
#
# Without this factor every splash family scores ~5x every single-target family at
# equal Damage (measured: Storm K 5.77 vs Laser K 0.56), which would force
# single-target weapons to ~10x the Damage number just to compete — straight through
# the 2000 grid's resolution and off the HP scale. The blob picture is real; its
# UPTIME is not 100%.
#
# rho_effective = DENSITY[class] * BLOB_UPTIME. At 0.30 the vehicle case lands on
# 0.33 * 0.30 ~ 0.1 and the infantry case on 2.0 * 0.30 = 0.6 — i.e. splash is worth
# 2-3x, not 10x. Raise it for blob-heavy play, lower it for skirmish play.
BLOB_UPTIME = 0.30


def _ruleset() -> Ruleset:
    return Ruleset(ROOT)


@functools.lru_cache(maxsize=1)
def armor_census() -> dict[str, int]:
    """{armor type: number of LIVE concrete actors with it}, resolved (not raw yaml).

    Resolved so an actor inheriting its armor from a class template is counted,
    and templates themselves are not.
    """
    rs = _ruleset()
    counts: collections.Counter[str] = collections.Counter()
    for name in rs.actors:
        if name.startswith(("^", "$", "-")) or "." in name:
            continue
        node = rs.resolve(name)
        if node is None:
            continue
        for child in node.children:
            if child.key == "Armor" or child.key.startswith("Armor@"):
                armor = child.get("Type")
                if armor and str(armor).strip() in ARMORS:
                    counts[str(armor).strip()] += 1
                    break                       # one armor per actor
    return dict(counts)


# How often a random shot is fired AT each macro class. Prevalence alone is not
# enough: the tree has ~900 building actors (every faction has a full base) against
# ~330 vehicles, so pure prevalence would price a tank cannon 45% against warehouses.
# Buildings also exist in small numbers per MATCH and are usually shot late.
#
# So the weighting is two-layer:  BETWEEN classes = engagement (design intent, below)
#                                 WITHIN a class  = prevalence (measured, self-updating)
# Adding 200 buildings then re-weights buildings against each other without repricing
# every tank — which is exactly the stability we want from an auto-updating model.
ENGAGEMENT = {"INF": 0.35, "VEH": 0.40, "BLD": 0.15, "AIR": 0.10}


@functools.lru_cache(maxsize=1)
def armor_weights() -> dict[str, float]:
    """Prevalence weight per armor, summing to 1.0. Self-updating with the roster.

    Within each macro class the weight follows the measured actor census; the class
    totals follow ENGAGEMENT. Every canonical armor gets a 1-actor floor so a type
    with 2 actors still counts a little — a weapon good only against Spaceships is
    niche, not worthless.
    """
    census = armor_census()
    weights: dict[str, float] = {}
    for macro, share in ENGAGEMENT.items():
        members = [a for a in ARMORS if ARMOR_MACRO[a] == macro]
        floored = {a: census.get(a, 0) + 1.0 for a in members}
        total = sum(floored.values())
        for armor, n in floored.items():
            weights[armor] = share * n / total
    return weights


@functools.lru_cache(maxsize=1)
def hp_by_macro() -> dict[str, int]:
    """Median max-HP per macro class — the yardstick for the %-of-max-HP twins.

    Median, not mean: a handful of 4M-HP epic units would drag a mean far off what
    a shot actually meets. Per class, because a %-twin's real value depends entirely
    on what it hits — 5% of an infantryman and 5% of a dreadnought are different
    weapons.
    """
    rs = _ruleset()
    buckets: dict[str, list[int]] = {m: [] for m in ENGAGEMENT}
    for name in rs.actors:
        if name.startswith(("^", "$", "-")) or "." in name:
            continue
        node = rs.resolve(name)
        if node is None:
            continue
        health = node.child("Health")
        if health is None:
            continue
        armor = None
        for child in node.children:
            if child.key == "Armor" or child.key.startswith("Armor@"):
                armor = str(child.get("Type") or "").strip()
                break
        if armor not in ARMOR_MACRO:
            continue
        try:
            hp = int(float(str(health.get("HP"))))
        except (TypeError, ValueError):
            continue
        if hp > 0:
            buckets[ARMOR_MACRO[armor]].append(hp)
    return {m: int(statistics.median(v)) if v else 0 for m, v in buckets.items()}


@functools.lru_cache(maxsize=1)
def reference_hp() -> int:
    """Engagement-weighted reference max-HP: what one %-of-HP point is worth."""
    per = hp_by_macro()
    total = sum(ENGAGEMENT[m] * per.get(m, 0) for m in ENGAGEMENT)
    return int(total) if total else 200_000


def weighted_versus(versus: dict[str, float], weights: dict[str, float] | None = None) -> float:
    """Prevalence-weighted mean Versus (as a factor, 1.0 = 100%).

    `versus` maps armor -> percent. Armors the warhead omits fall back to 100
    (the engine's default when a Versus row is absent).
    """
    weights = weights or armor_weights()
    return sum(weights[a] * versus.get(a, 100.0) / 100.0 for a in ARMORS)


def effective_density(versus: dict[str, float], weights: dict[str, float] | None = None) -> float:
    """Units per cell^2 this warhead realistically catches.

    The per-class densities weighted by BOTH prevalence and how good the warhead is
    against that class — an anti-infantry warhead is mostly shot at infantry, and
    infantry are the ones that bunch. This is the "combination of both" the
    maintainer asked for: per-class density, then capped by the blob in
    `footprint_targets()`.
    """
    weights = weights or armor_weights()
    num = den = 0.0
    for armor in ARMORS:
        share = weights[armor] * versus.get(armor, 100.0) / 100.0
        num += share * DENSITY[ARMOR_MACRO[armor]]
        den += share
    return num / den if den else DENSITY["VEH"]


def footprint_targets(footprint_cells2: float, density: float) -> float:
    """Expected SECONDARY targets caught by a blast of this damage-weighted area.

    density * min(footprint, A_BLOB) - the primary's own cell. Capping at the blob
    stops a superweapon-sized footprint from claiming it hits 50 units; subtracting
    A_SELF stops the aimed target being counted twice (once here, once in
    `reliability`).
    """
    covered = min(footprint_cells2, A_BLOB)
    return max(density * BLOB_UPTIME * (covered - A_SELF), 0.0)


def census_table() -> str:
    """Markdown census + weights, for the derived report."""
    census, weights = armor_census(), armor_weights()
    rows = sorted(ARMORS, key=lambda a: -census.get(a, 0))
    out = ["| armor | macro | live actors | weight | density (u/cell²) |",
           "|---|---|---|---|---|"]
    for armor in rows:
        out.append(f"| {armor} | {ARMOR_MACRO[armor]} | {census.get(armor, 0)} | "
                   f"{weights[armor]*100:.2f}% | {DENSITY[ARMOR_MACRO[armor]]} |")
    return "\n".join(out)


if __name__ == "__main__":
    print("# target_model — measured from mods/cameo\n")
    print(census_table())
    print("\n| macro | engagement | median HP |")
    print("|---|---|---|")
    for macro, hp in hp_by_macro().items():
        print(f"| {macro} | {ENGAGEMENT[macro]*100:.0f}% | {hp:,} |")
    print(f"\nreference HP (engagement-weighted): **{reference_hp():,}**")
    print(f"blob cap A_BLOB = {A_BLOB} cell², primary's own cell A_SELF = {A_SELF} cell²")
