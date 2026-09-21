#!/usr/bin/env python3
"""Collapse a source's compression groups into ONE ROW PER CAMEO WARHEAD.

Maintainer, 2026-09-21: *"change the map so that everything that shares the same cameo warhead
type is only listed once in one list ... no more several groups that all map to the same cameo
warhead but now compress them to one group per cameo warhead mapping."*

`compress_warheads.py` answers "what distinct weapon behaviours does this mod ship?" and lands on
126 groups for Combined Arms. That is the measurement. This answers the next question — "what does
each of OUR warheads have to be, to reproduce them?" — and there is exactly one answer per Cameo
warhead, so the 126 groups collapse onto the ~32 families they were assigned to.

The collapse is a WEIGHTED GEOMETRIC MEAN over the contributing groups, weighted by `uses` (the
weapon-slot votes behind each). Geometric because these are multipliers, and usage-weighted for
the reason R20 already gives: a profile carried by twenty units should set the family's shape more
than one carried by none.

⚠ A per-weapon override in `warhead_family_assignment.yaml` BEATS its group, so a group can
contribute to several families -- and it contributes only the weapons that actually stayed with
it. Splitting a group's usage across its destinations is what keeps `DepthCharge` out of CannonAP
and `TripodLaser` out of Laser.
"""
from __future__ import annotations

import argparse
import collections
import json
import math
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "reference"))

import warhead_matrix as wm  # noqa: E402

DOC = ROOT / "docs" / "reference" / "warhead_family_assignment.yaml"
GROUPS = ROOT / "docs" / "reference" / "warhead_groups.json"
OUT = ROOT / "docs" / "reference" / "warhead_families.json"


def load_assignment() -> dict:
    import yaml
    return yaml.safe_load(DOC.read_text(encoding="utf-8"))


def family_of(weapon: str, group: dict, overrides: dict) -> str:
    entry = overrides.get(weapon)
    return entry["family"] if entry else group["family"]


def source_rows(source: str) -> dict[str, dict]:
    """Each weapon's OWN measured row, keyed by weapon name."""
    spec = next((s for s in wm.OPENRA_SOURCES if s[0] == source), None)
    if spec is None:
        raise SystemExit(f"{source} is not an OpenRA source; the INI dialects need their reader")
    entry = wm.build_matrix(wm.read_openra(spec[2], spec[3]), source)
    return {r["weapon"]: r for r in entry["rows"] if r.get("weapon")}


def collapse(source: str) -> dict:
    assignment = load_assignment()
    if assignment["source"] != source:
        raise SystemExit(f"{DOC.name} holds {assignment['source']}, not {source}")
    overrides = assignment["overrides"]
    groups = {g["name"]: g for g in json.load(GROUPS.open(encoding="utf-8"))[source]["groups"]}
    armors = next(iter(groups.values()))["armors"]
    measured = source_rows(source)

    # ⛔ POOL EACH WEAPON'S OWN ROW, NEVER ITS GROUP'S PROFILE.
    # A group is a REVIEW device — it exists so a human can read 120 rows instead of 550 — and a
    # per-weapon override routinely sends one member of a group to a different family than the
    # rest. Pooling the group's shape then drags the whole group's profile along with that one
    # weapon: `MissileAA` came out with a full ground profile (Heavy 105.6, Light 182.7) on the
    # strength of ONE of its 33 members, `InvaderLauncher`, that happened to sit in a
    # ground-capable group. Every other member is air-only and had `n/a` in those columns.
    pooled: dict[str, list] = collections.defaultdict(list)
    for name, row in assignment["groups"].items():
        group = groups.get(name)
        if group is None:
            raise SystemExit(f"group {name} is in the assignment but not in the compression run")
        for weapon in group["weapons"]:
            measured_row = measured.get(weapon)
            if measured_row is None:
                continue
            family = family_of(weapon, row, overrides)
            pooled[family].append((float(measured_row["uses"]),
                                   measured_row["scaled"],
                                   measured_row["factor"], weapon, name))

    families = []
    for family, rows in sorted(pooled.items()):
        profile = []
        for i in range(len(armors)):
            # Positive and finite only: a zero is an absence (it carries no multiplier) and a
            # NaN from upstream must never be averaged into a family's shape.
            cells = [(r[1][i], r[0]) for r in rows
                     if r[1][i] is not None and math.isfinite(r[1][i]) and r[1][i] > 0]
            # Weights are usage votes and a group with zero uses must not vanish from the shape
            # entirely -- it still describes a behaviour the mod ships. Fall back to unweighted
            # when nothing in the family is fired by anything.
            if not cells:
                profile.append(None)
                continue
            if not any(w > 0 for _, w in cells):
                cells = [(v, 1.0) for v, _ in cells]
            profile.append(round(wm.weighted_gmean(cells), 1))
        weights = [r[0] for r in rows]
        factors = [(r[2], r[0]) for r in rows if r[2] > 0]
        if not any(w > 0 for _, w in factors):
            factors = [(f, 1.0) for f, _ in factors]
        families.append({
            "family": family,
            "uses": round(sum(weights), 1),
            "weapons": sorted({r[3] for r in rows}),
            "groups": sorted({r[4] for r in rows}),
            "armors": armors,
            "profile": profile,
            "factor": round(wm.weighted_gmean(factors), 3) if factors else 0.0,
        })

    fill_air_only(families, armors, groups, pooled)
    families.sort(key=lambda f: (f["family"].startswith("("), -f["uses"]))
    return {"source": source, "armors": armors, "families": families,
            "live": sum(1 for f in families if not f["family"].startswith("("))}


# ⛔ NO CAMEO WARHEAD IS AIR-ONLY — the landed-aircraft rule
# ══════════════════════════════════════════════════════════════════════════════════════════════
#
# Maintainer, 2026-09-21: *"MissileAA should still be able to damage ground targets but have the
# highest versus value against air. What if the aircraft lands? Then it counts as ground, and when
# the missile is still in flight and the aircraft lands it should of course still deal damage."*
#
# The MEASUREMENT is right to say `n/a`: Combined Arms' SAM sites genuinely state no opinion about
# ground armour, and inventing a 100 there is the Aircraft-148 defect pointed the other way. But a
# Cameo warhead is not a measurement — it has to fire at whatever it is pointed at. So an air-only
# family's ground rows are DERIVED, and marked as derived so nobody mistakes them for data.
#
# The donor is the same DELIVERY, measured: a SAM is a missile, so against ground it should behave
# like this mod's other missiles. `AA_GROUND_RATIO` is the one free parameter and it is deliberately
# in the open — it sets where the derived ground rows sit relative to the family's measured air
# value, so the air row stays the peak.
#
# ⚠ This is a FALLBACK, not a law. Most reference mods DO give their flak cannons and SAM sites
# real ground rows, so once the other nineteen sources are averaged in these cells should be
# replaced by measurement. Any family still carrying `derived_ground` at that point is one no
# source had an opinion about.
AA_GROUND_RATIO = 0.5


def fill_air_only(families: list[dict], armors: list[str], groups: dict, pooled: dict) -> None:
    air_i = next((i for i, a in enumerate(armors) if a.lower() == "aircraft"), None)
    if air_i is None:
        return
    ground_i = [i for i in range(len(armors)) if i != air_i]

    def delivery_of(family: str) -> str:
        names = {g for _, _, _, _, g in pooled.get(family, [])}
        tally = collections.Counter(groups[n]["delivery"] for n in names if n in groups)
        return tally.most_common(1)[0][0] if tally else ""

    donors: dict[str, list] = collections.defaultdict(list)
    for f in families:
        if f["family"].startswith("(") or any(f["profile"][i] is None for i in ground_i):
            continue
        donors[delivery_of(f["family"])].append(f)

    for f in families:
        if f["family"].startswith("("):
            continue
        if not all(f["profile"][i] is None for i in ground_i):
            continue
        air = f["profile"][air_i]
        pool = donors.get(delivery_of(f["family"])) or [
            d for ds in donors.values() for d in ds]
        if air is None or not pool:
            continue
        shape = [wm.weighted_gmean([(d["profile"][i], d["uses"] or 1.0) for d in pool])
                 for i in ground_i]
        peak = max(v for v in shape if v and math.isfinite(v))
        scale = (air * AA_GROUND_RATIO) / peak if peak > 0 else 1.0
        for slot, value in zip(ground_i, shape):
            f["profile"][slot] = round(value * scale, 1)
        f["derived_ground"] = {"donor_delivery": delivery_of(f["family"]),
                               "donors": sorted(d["family"] for d in pool),
                               "ratio": AA_GROUND_RATIO}


def report(result: dict) -> str:
    armors = result["armors"]
    out = [f"## {result['source']} — one row per Cameo warhead  "
           f"(**{result['live']} live families**, "
           f"{len(result['families']) - result['live']} drop/park buckets)", "",
           "| Cameo warhead | uses | weapons | factor | " + " | ".join(armors) + " |",
           "|---|--:|--:|--:|" + "--:|" * len(armors)]
    for f in result["families"]:
        cells = " | ".join("n/a" if v is None else f"{v:g}" for v in f["profile"])
        out.append(f"| `{f['family']}` | {f['uses']:g} | {len(f['weapons'])} | "
                   f"{f['factor']:g} | {cells} |")
    return "\n".join(out)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--source", default="combined_arms")
    ap.add_argument("--write", action="store_true")
    args = ap.parse_args()

    result = collapse(args.source)
    print(report(result))
    if args.write:
        OUT.write_text(json.dumps({args.source: result}, indent=1), encoding="utf-8")
        print(f"\nwrote {OUT.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
