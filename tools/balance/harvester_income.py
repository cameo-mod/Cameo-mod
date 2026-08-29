#!/usr/bin/env python3
"""harvester_income.py — derive every harvester economy parameter FROM THE TREE
and model steady-state income at two mining distances.

Replaces the hardcoded table in ``harvester_table.py`` (whose Cap for
``schwarzermond_noidharvester`` was 100 against a yaml value of 50, and which
predates both the ``HarvesterBalancer`` speed boost and the dock/fleet terms).

Nothing here is hand-entered: Capacity, BaleLoadDelay, BaleUnloadDelay,
BaleUnloadAmount, FullyLoadedSpeed, Speed, Locomotor, the per-refinery
harvester count and the refinery dock count are all read through
``miniyaml.Ruleset.resolve``.

THE SIX PARAMETERS
------------------
1. ``StoresResources.Capacity``      bales carried per trip
2. ``Harvester.BaleLoadDelay``       ticks per bale at the ore field
3. ``Harvester.BaleUnloadDelay``     ticks per batch at the refinery
4. ``Harvester.FullyLoadedSpeed``    percent of Speed on the loaded leg
5. refinery ``DockHost`` count       how many harvesters unload AT ONCE
6. harvesters spawned per refinery   ``FreeActor*`` + ``SlaveMinerSpawnerMaster``

(5) and (6) are refinery properties, not harvester properties. A swarm faction
is fast because its refinery hands out four workers and, for the StarCraft and
Warcraft factions, opens three docks — not because the worker itself is good.

THE MODEL
---------
For one harvester at mining distance D cells::

    T_load   = Cap * (BaleLoadDelay + 1)
    T_unload = ceil(Cap / BaleUnloadAmount) * BaleUnloadDelay + 1
    T_travel = leg(D, v_empty) + leg(D, v_loaded)
    T_cycle  = T_load + T_unload + T_travel

``leg`` accounts for ``HarvesterBalancer``: every harvester in the mod carries
``SpeedMultiplier@HARVBALANCE Modifier: 138`` gated on a condition granted
within ``MaxDistanceFromRefinery`` (CA default 5 cells), so the first and last
5 cells of every trip are travelled 38% faster.

A refinery's fleet is limited by whichever binds first — the cycle or the dock::

    income = min(N / T_cycle, docks / T_unload) * Cap * ResourceValue

Travel-bound fleets scale with distance; dock-bound fleets do not, which is
exactly why short-range and long-range mining have to be measured separately.

ENGINE PROVENANCE (from a complete tree; ``engine/`` is not in this repo, so
these line refs cannot be re-checked here — see docs/LESSONS_LEARNED.md
"environment-bound evidence"):
  Harvester.cs:284-287   GetSpeedModifier = 100 - (100 - FullyLoadedSpeed) * Fullness / 100
  Mobile.cs:756-761      MovementSpeedForCell = Speed * terrain% * modifier%
  Move.cs:471-473        one cell costs 1024 progress
  HarvestResource.cs:95  each bale = 1 harvest tick + BaleLoadDelay wait
  Harvester.cs:182-202   first batch immediate, then BaleUnloadDelay each, +1
"""

from __future__ import annotations

import argparse
import collections
import math
import os
import statistics
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "audit"))

from cameo_model import Model  # noqa: E402

CELL = 1024                 # WDist units per cell
TICKS_PER_SECOND = 25       # mod.yaml GameSpeeds default Timestep: 40ms
BALANCER_CELLS = 5          # CA HarvesterBalancerInfo.MaxDistanceFromRefinery default
BALANCER_BONUS = 1.38       # ^Harvester SpeedMultiplier@HARVBALANCE Modifier: 138

# world.yaml locomotor Speed on Clear terrain.
TERRAIN_SPEEDS = {
    "foot": 90, "swimsuit": 90, "chem": 90, "wheeled": 80, "heavywheeled": 80,
    "lighttracked": 80, "tracked": 80, "heavytracked": 80, "hover": 90,
}


def _int(node, key, default=None):
    if node is None:
        return default
    v = node.get(key)
    if v is None:
        return default
    try:
        return int(str(v).strip())
    except ValueError:
        return default


def harvesters(rs):
    """Every actor carrying a Harvester trait, with its six parameters."""
    out = {}
    for name in rs.actors:
        if name.startswith("^") or name.startswith("_"):
            continue
        n = rs.resolve(name)
        if n is None or n.child("Harvester") is None:
            continue
        h = n.child("Harvester")
        mob = n.child("Mobile") or n.child("Aircraft")
        balancer = n.child("HarvesterBalancer") is not None
        boost = 1.0
        for c in n.children:
            if c.key == "SpeedMultiplier@HARVBALANCE":
                boost = (_int(c, "Modifier", 100) or 100) / 100.0
        out[name] = {
            "actor": name,
            "cost": _int(n.child("Valued"), "Cost", 0),
            "cap": _int(n.child("StoresResources"), "Capacity", 0),
            "bld": _int(h, "BaleLoadDelay", 4),
            "bud": _int(h, "BaleUnloadDelay", 4),
            "bua": _int(h, "BaleUnloadAmount", 1),
            "fls": _int(h, "FullyLoadedSpeed", 100),
            "speed": _int(mob, "Speed", 0),
            "loco": (mob.get("Locomotor") if mob else None) or "tracked",
            "aircraft": mob is not None and mob.key.split("@")[0] == "Aircraft",
            "boost": boost if balancer else 1.0,
            "boost_cells": BALANCER_CELLS if balancer else 0,
        }
    return out


def refineries(rs, harv):
    """Every resource-accepting building, with its dock count and free fleet."""
    out = {}
    for name in rs.actors:
        if name.startswith("^") or name.startswith("_"):
            continue
        n = rs.resolve(name)
        if n is None:
            continue
        store = n.child("StoresPlayerResources")
        if store is None or _int(store, "Capacity", 0) < 1000:
            continue          # harvesters themselves grant +50; silos +2000
        docks = [c for c in n.children
                 if c.key.split("@")[0] in ("DockHost", "ProximityDockHost")]
        if not docks:
            continue
        fleet = collections.Counter()
        for c in n.children:
            base = c.key.split("@")[0]
            if base in ("FreeActor", "FreeActorWithDelivery"):
                a = (c.get("Actor") or "").strip()
                if a in harv:
                    fleet[a] += 1
            elif base == "SlaveMinerSpawnerMaster":
                for a in (c.get("Actors") or "").split(","):
                    a = a.strip()
                    if a in harv:
                        fleet[a] += 1
        if not fleet:
            continue
        out[name] = {
            "refinery": name,
            "docks": len(docks),
            "queue": max((_int(d, "MaxQueueLength", 0) or 0) for d in docks),
            "capacity": _int(store, "Capacity", 0),
            "fleet": fleet,
        }
    return out


def leg(cells, speed_units, boost, boost_cells):
    """Ticks to cross `cells`, with the first `boost_cells` run at `boost`x."""
    if speed_units <= 0:
        return float("inf")
    near = min(boost_cells, cells)
    far = cells - near
    return near * CELL / (speed_units * boost) + far * CELL / speed_units


def cycle(h, cells):
    terrain = 100 if h["aircraft"] else TERRAIN_SPEEDS.get(h["loco"], 80)
    v_empty = h["speed"] * terrain / 100.0
    v_full = h["speed"] * h["fls"] / 100.0 * terrain / 100.0
    t_load = h["cap"] * (h["bld"] + 1)
    t_unload = math.ceil(h["cap"] / max(1, h["bua"])) * h["bud"] + 1
    t_travel = (leg(cells, v_empty, h["boost"], h["boost_cells"])
                + leg(cells, v_full, h["boost"], h["boost_cells"]))
    return t_load, t_unload, t_travel, t_load + t_unload + t_travel


def refinery_income(ref, harv, cells, value):
    """Credits per second for one refinery's free fleet at `cells` distance.

    Also returns the dock saturation point: the number of harvesters past
    which the dock, not the cycle, sets the ceiling. Building beyond it adds
    idle harvesters and no income.
    """
    total = 0.0
    bound = "cycle"
    saturate = 0.0
    for actor, count in ref["fleet"].items():
        h = harv[actor]
        _t_load, t_unload, _t_travel, t_cycle = cycle(h, cells)
        by_cycle = count / t_cycle
        by_dock = ref["docks"] / t_unload
        trips = min(by_cycle, by_dock)
        if by_dock < by_cycle:
            bound = "dock"
        saturate = max(saturate, ref["docks"] * t_cycle / t_unload)
        total += trips * h["cap"] * value
    return total * TICKS_PER_SECOND, bound, saturate


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--short", type=int, default=6,
                    help="short mining distance in cells (default 6)")
    ap.add_argument("--long", type=int, default=30,
                    help="long mining distance in cells (default 30)")
    ap.add_argument("--weight-short", type=float, default=0.5,
                    help="weight of the short-range figure in the aggregate")
    ap.add_argument("--value", type=int, default=25,
                    help="ResourceValues entry for the test map's resource "
                         "(player.yaml: Ore 25, Tiberium 30, Spice 75)")
    ap.add_argument("--per-unit", action="store_true",
                    help="report per-harvester instead of per-refinery")
    args = ap.parse_args()

    rs = Model().rs
    harv = harvesters(rs)
    refs = refineries(rs, harv)

    ws, wl = args.weight_short, 1.0 - args.weight_short
    rows = []
    if args.per_unit:
        for h in harv.values():
            s = h["cap"] * args.value / cycle(h, args.short)[3] * TICKS_PER_SECOND
            l = h["cap"] * args.value / cycle(h, args.long)[3] * TICKS_PER_SECOND
            rows.append((h["actor"], 1, "-", s, l, ws * s + wl * l, 0.0))
    else:
        for r in refs.values():
            s, bs, sat = refinery_income(r, harv, args.short, args.value)
            l, _bl, _satl = refinery_income(r, harv, args.long, args.value)
            n = sum(r["fleet"].values())
            rows.append((r["refinery"], n, f"{r['docks']}d/{bs}", s, l,
                         ws * s + wl * l, sat))

    rows.sort(key=lambda x: -x[5])
    print(f"{'':46}{'N':>3} {'docks':>8} {'short':>8} {'long':>8} "
          f"{'aggregate':>10} {'l/s':>6} {'sat':>6}")
    for name, n, docks, s, l, agg, sat in rows:
        print(f"{name:46}{n:3} {docks:>8} {s:8.2f} {l:8.2f} {agg:10.2f} "
              f"{(l / s if s else 0):6.2f} {sat:6.1f}")
    print("\nsat = harvesters the refinery supports before the DOCK binds; "
          "past it, extra harvesters idle.")

    agg = [r[5] for r in rows]
    med = statistics.median(agg)
    print(f"\nn={len(agg)}  credits/sec  min {min(agg):.2f}  median {med:.2f}  "
          f"max {max(agg):.2f}  SPREAD {max(agg) / min(agg):.2f}x")
    print(f"(short={args.short} cells, long={args.long} cells, "
          f"weight {ws:.2f}/{wl:.2f}, resource value {args.value})")

    off = [(r[0], r[5] / med) for r in rows if not 0.75 <= r[5] / med <= 1.25]
    print(f"\noutside +/-25% of the median: {len(off)} of {len(rows)}")
    for name, ratio in off:
        print(f"   {name:46} {ratio:5.2f}x median")


if __name__ == "__main__":
    main()
