#!/usr/bin/env python3
"""effective_damage.py — READ-ONLY area-integrated damage metric per weapon.

Ranks weapons on ONE comparable axis so single-target energy warheads (laser /
tesla / railgun + their extra-damage chips) sit next to wide-spread AoE warheads
(HE / cannon / nuke). Integrates each flat-damage warhead over its spread+falloff
curve, weights it by single-target hit reliability (accuracy + travel), and sums
the main + every *_ExtraDamage chip.

FORMULA (locked with maintainer 2026-08-11):
  effective = SUM over(main + *_ExtraDamage)  base * ( reliability + SWARM_W * footprint_cells2 )
    footprint   = 2*pi * INT (F(r)/100) * r dr  / CELL^2                 (cell^2, damage-weighted area)
    reliability = average F over a scatter disc of radius sigma          (= 1.0 at sigma 0)
    sigma       = Inaccuracy + LEAD * TARGET_SPEED * Range / min(Speed, SPEED_CAP)
    clamps      : Spread >= MIN_SPREAD (100);  Falloff at least [100, 0]
    instant hits (InstantHit / LaserZap / Railgun / no Speed) -> Speed = SPEED_CAP -> ~0 drift.

Excludes the %-twin (AreaDamagePercentage / HealthPercentageDamage — a different
currency), the baked *FriendlyFire twins, EMP (AffectsIntegrity) and effects.

Writes/edits NOTHING. Usage:
  python tools/balance/effective_damage.py                 # full table, sorted desc
  python tools/balance/effective_damage.py --top 40        # only the top 40
  python tools/balance/effective_damage.py NAME [NAME...]   # just these weapons, verbose
"""
from __future__ import annotations
import math
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/audit"))
from cameo_model import Model  # noqa: E402

CELL = 1024.0
SWARM_W = 0.25          # anti-swarm area weight (you rarely hit a full blob)
LEAD = 0.20             # engine leads/tracks -> real miss is ~20% of raw displacement
TARGET_SPEED = 100      # typical dodging vehicle speed (WDist/tick)
SPEED_CAP = 10000       # ~10 cells/tick: at/above this a projectile is "basically instant"
MIN_SPREAD = 100        # absolute floor: no weapon integrates below Spread 100 / Falloff 100,0
INSTANT_PROJECTILES = {"InstantHit", "LaserZap", "Railgun", "InstantHitLine", "InstantHitAS"}


def parse_wdist(s) -> int:
    s = str(s).strip()
    if "," in s:                               # random range e.g. Speed "180, 240" -> average
        vals = [parse_wdist(x) for x in s.split(",") if x.strip() != ""]
        return sum(vals) // len(vals) if vals else 0
    neg = s.startswith("-")
    if neg:
        s = s[1:]
    if "c" in s:
        a, _, b = s.partition("c")
        v = int(a or 0) * 1024 + int(b or 0)
    else:
        v = int(float(s))
    return -v if neg else v


def parse_ints(s) -> list[int]:
    return [int(float(x.strip())) for x in str(s).split(",") if x.strip() != ""]


def falloff_and_radii(node, spread: int):
    """(falloff percents, radii in WDist). Warhead Range overrides Spread geometry."""
    fo = node.get("Falloff")
    fo = parse_ints(fo) if fo else [100, 0]
    if len(fo) < 2:
        fo = [100, 0]
    rng = node.get("Range")
    if rng:
        radii = [parse_wdist(x) for x in str(rng).split(",") if x.strip() != ""]
        if len(radii) == 1:                    # single value = per-step spacing
            radii = [i * radii[0] for i in range(len(fo))]
        elif len(radii) != len(fo):            # mismatch -> fall back to spread grid
            radii = [i * spread for i in range(len(fo))]
    else:
        radii = [i * spread for i in range(len(fo))]
    return fo, radii


def footprint_cells2(fo, radii) -> float:
    """2*pi * INT (F/100) r dr over the piecewise-linear curve, in cell^2."""
    tot = 0.0
    for k in range(len(fo) - 1):
        a, b = radii[k], radii[k + 1]
        if b <= a:
            continue
        pa, pb = fo[k] / 100.0, fo[k + 1] / 100.0
        m = (pb - pa) / (b - a)
        tot += pa * (b * b - a * a) / 2 + m * ((b ** 3 - a ** 3) / 3 - a * (b * b - a * a) / 2)
    return (2 * math.pi * tot) / (CELL * CELL)


def _F(fo, radii, r) -> float:
    if r >= radii[-1]:
        return 0.0
    for k in range(len(fo) - 1):
        if radii[k] <= r < radii[k + 1]:
            span = radii[k + 1] - radii[k]
            return (fo[k] + (fo[k + 1] - fo[k]) * (r - radii[k]) / span) / 100.0 if span else fo[k] / 100.0
    return 0.0


def reliability(fo, radii, sigma) -> float:
    """Average falloff over a uniform scatter disc of radius sigma (single POINT target)."""
    if sigma <= 0:
        return 1.0
    n = 400
    acc = 0.0
    for i in range(n):
        r = (i + 0.5) * sigma / n
        acc += _F(fo, radii, r) * 2 * math.pi * r * (sigma / n)
    return acc / (math.pi * sigma * sigma)


def flat_damage_warheads(resolved):
    """Main + extra-damage flat warheads (exclude %-twin, FriendlyFire, EMP, effects)."""
    out = []
    for c in resolved.children:
        if not c.key.startswith("Warhead@"):
            continue
        tag = c.key.split("@", 1)[1]
        if "FriendlyFire" in tag:
            continue
        if c.value not in ("AreaDamage", "SpreadDamage", "TargetDamage"):
            continue
        d = c.get("Damage")
        if d is None:
            continue
        try:
            base = int(float(d))
        except ValueError:
            continue
        if base <= 0:
            continue
        out.append((tag, c.value, base, c))
    return out


def weapon_sigma(resolved) -> float:
    """Inaccuracy + travel drift, capped speed. Instant projectiles -> ~0 drift."""
    proj = resolved.child("Projectile")
    ptype = proj.value if proj is not None else None
    inacc = resolved.get("Projectile", "Inaccuracy")
    inacc = parse_wdist(inacc) if inacc else 0
    rng = resolved.get("Range")
    rng = parse_wdist(rng) if rng else 0
    speed = resolved.get("Projectile", "Speed")
    if ptype in INSTANT_PROJECTILES or not speed:
        eff_speed = SPEED_CAP
    else:
        eff_speed = min(parse_wdist(speed), SPEED_CAP)
        eff_speed = max(eff_speed, 1)
    drift = LEAD * TARGET_SPEED * rng / eff_speed if rng else 0.0
    return inacc + drift


def effective_damage(resolved):
    """Return (effective, base_total, footprint_total, avg_reliability) or None."""
    whs = flat_damage_warheads(resolved)
    if not whs:
        return None
    sigma = weapon_sigma(resolved)
    eff = base_total = foot_total = 0.0
    rel_weighted = 0.0
    for _tag, wtype, base, node in whs:
        if wtype == "TargetDamage":
            fo, radii = [100, 0], [0, MIN_SPREAD]
        else:
            spread = node.get("Spread")
            spread = max(parse_wdist(spread) if spread else MIN_SPREAD, MIN_SPREAD)
            fo, radii = falloff_and_radii(node, spread)
        fp = footprint_cells2(fo, radii)
        rel = reliability(fo, radii, sigma)
        contrib = base * (rel + SWARM_W * fp)
        eff += contrib
        base_total += base
        foot_total += fp
        rel_weighted += rel * base
    avg_rel = rel_weighted / base_total if base_total else 0.0
    return eff, base_total, foot_total, avg_rel, sigma


def main() -> int:
    args = [a for a in sys.argv[1:]]
    top = None
    if "--top" in args:
        i = args.index("--top")
        top = int(args[i + 1])
        del args[i:i + 2]
    names = args
    rs = Model().rs
    verbose = bool(names)
    targets = names if names else [n for n in rs.weapons if not n.startswith("^")]

    rows = []
    for wname in targets:
        resolved = rs.resolve_weapon(wname)
        if resolved is None:
            if verbose:
                print(f"{wname}: UNRESOLVED")
            continue
        r = effective_damage(resolved)
        if r is None:
            if verbose:
                print(f"{wname}: no flat-damage warheads")
            continue
        eff, base_total, foot_total, avg_rel, sigma = r
        rows.append((eff, wname, base_total, foot_total, avg_rel, sigma))

    rows.sort(reverse=True)
    if top:
        rows = rows[:top]

    print(f"# effective_damage  (SWARM_W={SWARM_W} LEAD={LEAD} TARGET_SPEED={TARGET_SPEED} "
          f"SPEED_CAP={SPEED_CAP} MIN_SPREAD={MIN_SPREAD})  read-only")
    print(f"# {'weapon':30} {'effective':>11} {'base':>9} {'reliab':>6} {'footprint':>9} {'sigma':>6}")
    for eff, wname, base_total, foot_total, avg_rel, sigma in rows:
        print(f"  {wname:30} {eff:11.0f} {base_total:9.0f} {avg_rel:6.2f} {foot_total:9.2f} {sigma:6.0f}")
    print(f"# {len(rows)} weapons")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
