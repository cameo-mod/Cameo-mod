#!/usr/bin/env python3
"""faction_profile.py — what is each reference faction actually FOR?

Implements maintainer rulings R5 and R7 (`docs/design/REFERENCE_EXTRACTION_PLAN.md`):

    R5  Profiles are computed per TYPE — infantry, vehicle, aircraft, naval, defense — AND
        overall, each compared against THE SAME GROUP in the faction's own source game, and
        each reported as an independent separate value. Geometric mean.
    R7  Variance is reported three ways: coefficient of variation, min/max spread, and the
        percentile position of the faction's mean within its own game.

THE QUESTION THIS ANSWERS: "is this faction more about speed, HP, firepower...?" A raw average
cannot say — every game sets its own power level, so 12,500 HP is heavy in one mod and light in
another. The answer is always a RATIO against the same group in the same game: Soviet vehicles
against ALL vehicles in that game. 1.00 is exactly average for its game; 1.30 means 30% above.

WHY GEOMETRIC MEAN. These are ratio quantities spanning orders of magnitude — a 10x-HP epic unit
drags an arithmetic mean far more than it should, and the result would describe the outlier
rather than the faction. The geometric mean is the right centre for ratios and it makes the
comparison symmetric: faction/game and game/faction are reciprocals.

⚠ Only COSTED units count. An uncosted entry is scenery, a husk or an unbuildable — including
them would let a faction's profile be set by things no player can field.
"""

from __future__ import annotations

import argparse
import collections
import json
import math
import pathlib
import statistics
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

# The stats worth profiling. Anything absent or non-positive is skipped per stat, not per unit,
# so a unit missing `sight` still contributes its HP.
STATS = ["hp", "cost", "speed", "w_dps", "w_range", "sight"]
TYPES = ["infantry", "vehicle", "aircraft", "naval", "defense"]


def geomean(vals: list[float]) -> float | None:
    vals = [v for v in vals if isinstance(v, (int, float)) and v > 0]
    if not vals:
        return None
    return math.exp(sum(math.log(v) for v in vals) / len(vals))


def cv(vals: list[float]) -> float | None:
    """Coefficient of variation — unitless, so HP and speed are directly comparable."""
    vals = [v for v in vals if isinstance(v, (int, float)) and v > 0]
    if len(vals) < 2:
        return None
    m = statistics.fmean(vals)
    return (statistics.stdev(vals) / m) if m else None


def spread(vals: list[float]) -> float | None:
    vals = [v for v in vals if isinstance(v, (int, float)) and v > 0]
    if len(vals) < 2:
        return None
    lo = min(vals)
    return (max(vals) / lo) if lo else None


def percentile_of(value: float, population: list[float]) -> float | None:
    """Where the faction's mean sits inside its own game's distribution, 0-100."""
    pop = sorted(v for v in population if isinstance(v, (int, float)) and v > 0)
    if not pop or value is None:
        return None
    below = sum(1 for v in pop if v < value)
    return round(100.0 * below / len(pop), 1)


def load(path: pathlib.Path) -> list[dict]:
    return [json.loads(l) for l in path.read_text(encoding="utf-8").splitlines() if l.strip()]


def build(rows: list[dict]) -> dict:
    # (source, type) -> all costed units in that game+group; the comparison population
    game: dict[tuple, list[dict]] = collections.defaultdict(list)
    # (source, faction, type) -> that faction's units in the group
    fac: dict[tuple, list[dict]] = collections.defaultdict(list)

    for r in rows:
        if not r.get("cost"):
            continue
        t = r.get("type")
        if t not in TYPES:
            continue
        src = r["source"]
        game[(src, t)].append(r)
        game[(src, "overall")].append(r)
        # A unit owned by six countries belongs to six rosters — Owner= is a comma list.
        for owner in r.get("owners") or []:
            fac[(src, owner, t)].append(r)
            fac[(src, owner, "overall")].append(r)

    out: dict = {}
    for (src, faction, t), units in sorted(fac.items()):
        if len(units) < 3:          # too few to characterise; recorded as skipped, not guessed
            continue
        pop = game[(src, t)]
        entry = out.setdefault(src, {}).setdefault(faction, {}).setdefault(t, {
            "n": len(units), "game_n": len(pop), "stats": {}})
        for stat in STATS:
            fv = [u.get(stat) for u in units]
            gv = [u.get(stat) for u in pop]
            fg, gg = geomean(fv), geomean(gv)
            if fg is None or gg is None:
                continue
            entry["stats"][stat] = {
                "faction_geomean": round(fg, 2),
                "game_geomean": round(gg, 2),
                # THE headline number: 1.00 = exactly average for this game and group
                "ratio": round(fg / gg, 3),
                "cv": round(cv(fv), 3) if cv(fv) is not None else None,
                "game_cv": round(cv(gv), 3) if cv(gv) is not None else None,
                "spread": round(spread(fv), 2) if spread(fv) is not None else None,
                "game_spread": round(spread(gv), 2) if spread(gv) is not None else None,
                "percentile": percentile_of(fg, gv),
            }
    return out


def describe(stats: dict) -> str:
    """A one-line reading of what the faction leans toward, from the ratios themselves."""
    lean = {k: v["ratio"] for k, v in stats.items() if v.get("ratio")}
    if not lean:
        return ""
    hi = sorted(lean.items(), key=lambda kv: -kv[1])[:2]
    lo = sorted(lean.items(), key=lambda kv: kv[1])[:1]
    bits = [f"+{k} {v:.2f}" for k, v in hi if v > 1.05]
    bits += [f"-{k} {v:.2f}" for k, v in lo if v < 0.95]
    return "  ".join(bits)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--corpus", default="docs/reference/ini_corpus.json")
    ap.add_argument("--json", help="write the profiles here")
    ap.add_argument("--source", help="print one source's table")
    ap.add_argument("--top", type=int, default=12)
    args = ap.parse_args()

    rows = load(pathlib.Path(args.corpus))
    prof = build(rows)

    nf = sum(len(v) for v in prof.values())
    print(f"profiled {nf} factions across {len(prof)} sources "
          f"(geometric mean, costed units only, >=3 per group)")

    show = [args.source] if args.source else list(prof)
    for src in show:
        if src not in prof:
            continue
        print(f"\n=== {src} ===")
        for faction, types in sorted(prof[src].items())[:args.top]:
            ov = types.get("overall")
            if not ov:
                continue
            print(f"  {faction:<18} n={ov['n']:<4} of {ov['game_n']:<5} {describe(ov['stats'])}")
    if args.json:
        out = pathlib.Path(args.json)
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text(json.dumps(prof, indent=1, sort_keys=True), encoding="utf-8")
        print(f"\n  wrote {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
