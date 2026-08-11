#!/usr/bin/env python3
"""weapon_efficiency.py — the K coefficient, and the family comparison table.

K is the whole pricing model in one dimensionless number:

    effective_dps = Damage_total x (burst / eff_reload) x FirepowerMultiplier x K

    K = SUM over warheads   share_w x versus_w x ( reliability_w + secondary_w )

      share_w      the warhead's fixed fraction of the weapon's total damage
                   (distribute_damage law: mains equal, chip = 1/2, %-twin = 1/2000)
      versus_w     Versus averaged over armors, weighted by how common each armor
                   actually is (target_model.armor_weights)
      reliability  P-weighted falloff at the impact point, over the ENGINE's scatter
      secondary_w  expected extra bodies caught = density x min(footprint, blob) - own cell

**K does not depend on the Damage magnitude** — every term multiplies the base — so
the pricing inversion is exact and the yaml grid is never violated:

    Damage_required = target_effective_dps x eff_reload / (burst x FP x K)
    Damage_yaml     = round(Damage_required / 2000) x 2000      # the 2000 grid
    FirepowerMult   = Damage_required / Damage_yaml             # absorbs the remainder

That is the answer to "how does 2351.85 get into the yaml": it does not. The
designer sets geometry for FEEL, K measures it, and the pipeline solves for Damage.

Usage:
  python tools/balance/weapon_efficiency.py --families      # the family table
  python tools/balance/weapon_efficiency.py NAME [NAME...]  # concrete weapons
"""

from __future__ import annotations

import functools
import math
import pathlib
import statistics
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/audit"))
sys.path.insert(0, str(ROOT / "tools/balance"))
from miniyaml import Ruleset  # noqa: E402
import effective_damage as ed  # noqa: E402
import target_model as tm  # noqa: E402

# The families the maintainer asked to compare, all at the Heavy level.
FAMILIES = ["CannonHE", "CannonAP", "MissileAP", "MissileHE", "Railgun", "Tesla",
            "Laser", "Prism", "Magic", "Sonic", "Flame", "Chemical", "Demolition",
            "Concussion", "Bullet", "Flak", "Plasma", "Thermobaric", "Quantum", "Storm"]

PCT_SUFFIX = "_Percentage"
CHIP_SUFFIX = "_ExtraDamage"

# ---------------------------------------------------------------------------
# W5 CONTEXT FACTORS — what K alone cannot see.
#
# Each is a SEPARATE named factor, never one blended fudge: a price that moved
# has to be explainable by pointing at the factor that moved it.
#
# Three of them (targets / range / deadzone) are INDEPENDENT OF DAMAGE, so they
# fold into `k_context` and the pricing inversion stays exact:
#     Damage_required = target_dps x eff_reload / (burst x FP x k_context)
# `overkill` is NOT: it compares per-shot damage against target HP, so it moves
# when Damage moves. Folding it into K would turn the closed-form inversion into
# a fixed-point iteration, so it is reported ALONGSIDE K, never inside it.
# ---------------------------------------------------------------------------

# A weapon that cannot hit air still fights 90% of the game. The floor stops a
# specialist being priced into irrelevance: AA units are separately class-anchored,
# so a raw share (Air-only = 0.10) would penalise them twice. Raise the floor for a
# more forgiving model, lower it to punish narrow weapons harder.
TARGETS_FLOOR = 0.5

# Outranging is worth more than DPS, but not without limit — a siege weapon already
# pays for its range in cost and mobility. Bounded so one long-range outlier cannot
# dominate the price. NOTE: at weight 0.25 the LOW bound is the asymptote (a range of
# 0 gives 1-0.25 = 0.75), so only the high bound actually clamps today; raising the
# weight past 0.25 makes the low one bite too.
RANGE_WEIGHT = 0.25
RANGE_BOUNDS = (0.75, 1.50)

# A MinRange dead zone costs the ANNULUS you cannot cover, which goes as the square
# of the radius ratio — a MinRange of half the range loses a quarter of the circle.
DEADZONE_WEIGHT = 1.0

_INJECTED = None


def use_ruleset(rs) -> None:
    """Reuse a caller's Ruleset for the weapon-side census (see target_model)."""
    global _INJECTED
    _INJECTED = rs
    median_weapon_range.cache_clear()


@functools.lru_cache(maxsize=1)
def median_weapon_range() -> float:
    """Median `Range` over every live weapon — the yardstick for range advantage.

    Measured, not assumed, so it self-updates as the roster grows (the same rule
    target_model follows). Weapons without a Range are skipped rather than counted
    as zero, which would drag the median down.
    """
    rs = _INJECTED if _INJECTED is not None else Ruleset(ROOT)
    ranges = []
    for name in rs.weapons:
        resolved = rs.resolve_weapon(name)
        if resolved is None:
            continue
        raw = resolved.get("Range")
        if raw:
            value = ed.parse_wdist(raw)
            if value > 0:
                ranges.append(value)
    return statistics.median(ranges) if ranges else 6000.0


def targets_factor(resolved) -> float:
    """How much of the game this weapon can legally shoot at.

    `ValidTargets` maps onto the engagement weights: `Ground` (with `Water`/`Ship`)
    covers infantry + vehicles + buildings, `Air` covers aircraft. A ground-only
    weapon reaches 90% of the engagement mass, an AA-only weapon 10%.
    """
    raw = resolved.get("ValidTargets")
    if not raw:
        return 1.0                      # engine default = everything
    tokens = {t.strip().lower() for t in str(raw).split(",") if t.strip()}
    ground = bool(tokens & {"ground", "water", "ship", "trees", "wall"})
    air = "air" in tokens
    if not ground and not air:
        return 1.0                      # exotic set (Shielded, ...) — do not guess
    share = 0.0
    if ground:
        share += tm.ENGAGEMENT["INF"] + tm.ENGAGEMENT["VEH"] + tm.ENGAGEMENT["BLD"]
    if air:
        share += tm.ENGAGEMENT["AIR"]
    return TARGETS_FLOOR + (1.0 - TARGETS_FLOOR) * share


def range_factor(resolved, median: float | None = None) -> float:
    """Bounded reward for outranging the median weapon."""
    raw = resolved.get("Range")
    if not raw:
        return 1.0
    rng = ed.parse_wdist(raw)
    median = median or median_weapon_range()
    if rng <= 0 or median <= 0:
        return 1.0
    lo, hi = RANGE_BOUNDS
    return min(max(1.0 + RANGE_WEIGHT * (rng / median - 1.0), lo), hi)


def deadzone_factor(resolved) -> float:
    """Cost of a `MinRange` hole: the fraction of the engagement disc lost."""
    raw_min, raw_max = resolved.get("MinRange"), resolved.get("Range")
    if not raw_min or not raw_max:
        return 1.0
    lo, hi = ed.parse_wdist(raw_min), ed.parse_wdist(raw_max)
    if lo <= 0 or hi <= 0 or lo >= hi:
        return 1.0
    return 1.0 - DEADZONE_WEIGHT * (lo / hi) ** 2


def overkill_factor(per_shot: float, target_hp: float | None = None) -> float:
    """Fraction of dealt damage that is NOT wasted on an already-dead target.

    A shot only overkills on the LAST hit, so the waste is the remainder of the
    final shot: `ceil(HP/dmg)` shots deal `ceil(HP/dmg)*dmg` to remove `HP`.
    A 200k burst on a 50k target keeps 25%; anything that needs several shots
    tends to ~1.0.

    DAMAGE-DEPENDENT — reported next to K, never folded into it.
    """
    target_hp = target_hp or tm.reference_hp()
    if per_shot <= 0 or target_hp <= 0:
        return 1.0
    shots = math.ceil(target_hp / per_shot)
    return target_hp / (shots * per_shot)


def versus_of(node) -> dict[str, float]:
    """{armor: percent} from a warhead node's Versus block; missing armors = 100."""
    versus = node.child("Versus")
    if versus is None:
        return {}
    out = {}
    for child in versus.children:
        value = ed.number(child.value)
        if value is not None:
            out[child.key] = value
    return out


def warhead_terms(node, wtype: str, sigma: float, is_instant: bool):
    """(versus_factor, reliability, secondary, footprint) for one warhead node."""
    vs = versus_of(node)
    versus = tm.weighted_versus(vs)
    density = tm.effective_density(vs)
    if wtype == "TargetDamage":
        fo, radii, live = [100, 0], [0, ed.MIN_SPREAD], True
    else:
        spread = node.get("Spread")
        spread = max(ed.parse_wdist(spread) if spread else ed.MIN_SPREAD, ed.MIN_SPREAD)
        fo, radii, live = ed.falloff_and_radii(node, spread)
    if not live:
        return versus, 0.0, 0.0, 0.0
    footprint = ed.footprint_cells2(fo, radii)
    rel = 1.0 if is_instant else ed.reliability(fo, radii, sigma)
    return versus, rel, tm.footprint_targets(footprint, density), footprint


def analyse(resolved, damage_total: float = 20000.0):
    """Full breakdown for one resolved weapon at a nominal total damage."""
    whs = ed.flat_damage_warheads(resolved)
    pcts = [c for c in resolved.children
            if c.key.startswith("Warhead@") and c.key.endswith(PCT_SUFFIX)
            and c.value in ("AreaDamagePercentage", "HealthPercentageDamage")]
    if not whs and not pcts:
        return None
    is_instant, sigma = ed.weapon_reliability_ctx(resolved)

    parts = []
    flat_total = sum(base for _t, _w, base, _n in whs) or 1.0
    for tag, wtype, base, node in whs:
        versus, rel, secondary, footprint = warhead_terms(node, wtype, sigma, is_instant)
        parts.append({"tag": tag, "share": base / flat_total, "versus": versus,
                      "rel": rel, "secondary": secondary, "footprint": footprint,
                      "kind": "chip" if tag.endswith(CHIP_SUFFIX.lstrip("_")) or
                              CHIP_SUFFIX in f"_{tag}" else "flat"})

    # %-of-max-HP twins converted to flat-damage equivalents through the reference HP.
    ref_hp = tm.reference_hp()
    for node in pcts:
        vs = versus_of(node)
        versus = tm.weighted_versus(vs)
        density = tm.effective_density(vs)
        spread = node.get("Spread")
        spread = max(ed.parse_wdist(spread) if spread else ed.MIN_SPREAD, ed.MIN_SPREAD)
        fo, radii, live = ed.falloff_and_radii(node, spread)
        if not live:
            continue
        footprint = ed.footprint_cells2(fo, radii)
        rel = 1.0 if is_instant else ed.reliability(fo, radii, sigma)
        pct_damage = ed.number(node.get("Damage"))
        if pct_damage is None:
            continue
        # HealthPercentageDamage: %HP dealt = Damage x Versus/100. Convert to HP via ref.
        hp_equiv = ref_hp * pct_damage / 100.0
        parts.append({"tag": node.key.split("@", 1)[1],
                      "share": hp_equiv / flat_total, "versus": versus, "rel": rel,
                      "secondary": tm.footprint_targets(footprint, density),
                      "footprint": footprint, "kind": "pct"})

    k = sum(p["share"] * p["versus"] * (p["rel"] + p["secondary"]) for p in parts)

    # W5 context factors. The damage-independent three multiply into k_context, so
    # the pricing inversion stays closed-form; overkill is reported separately
    # because it moves with Damage (see the constants block).
    factors = {"targets": targets_factor(resolved),
               "range": range_factor(resolved),
               "deadzone": deadzone_factor(resolved)}
    k_context = k * factors["targets"] * factors["range"] * factors["deadzone"]
    overkill = overkill_factor(damage_total * k, ref_hp)

    return {"k": k, "k_context": k_context, "factors": factors,
            "overkill": overkill, "sigma": sigma, "instant": is_instant,
            "parts": parts, "effective": damage_total * k_context, "ref_hp": ref_hp}


def family_table(damage_total: float = 20000.0, level: str = "Heavy") -> str:
    rs = Ruleset(ROOT)
    rows = []
    for fam in FAMILIES:
        name = f"^Warhead_{fam}_{level}"
        node = rs.resolve_weapon(name)
        if node is None:
            continue
        res = analyse(node, damage_total)
        if res is None:
            continue
        flat = [p for p in res["parts"] if p["kind"] == "flat"]
        chip = [p for p in res["parts"] if p["kind"] == "chip"]
        pct = [p for p in res["parts"] if p["kind"] == "pct"]
        main = flat[0] if flat else None
        # Ranked on the CONTEXT-adjusted number — that is what the weapon is
        # actually worth; the bare K stays visible as its own column.
        rows.append((res["k_context"] * damage_total, fam, res, main, chip, pct))
    rows.sort(key=lambda r: (-r[0], r[1]))

    out = [f"| # | family (Heavy) | avgVersus | reliab | footprint cell² | secondary | "
           f"chip | %-twin | **K** | targets | range | deadzone | overkill | "
           f"**K ctx** | **effective @ {damage_total:,.0f}** |",
           "|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|"]
    for i, (eff, fam, res, main, chip, pct) in enumerate(rows, 1):
        f = res["factors"]
        out.append(
            f"| {i} | {fam} | {main['versus']:.2f} | {main['rel']:.2f} | "
            f"{main['footprint']:.2f} | {main['secondary']:.2f} | "
            f"{'yes' if chip else '—'} | {'yes' if pct else '—'} | "
            f"**{res['k']:.2f}** | {f['targets']:.2f} | {f['range']:.2f} | "
            f"{f['deadzone']:.2f} | {res['overkill']:.2f} | "
            f"**{res['k_context']:.2f}** | **{eff:,.0f}** |")
    return "\n".join(out)


def main() -> int:
    args = sys.argv[1:]
    if not args or "--families" in args:
        print(f"# Weapon efficiency K — all families at Heavy, 20 000 total damage\n")
        print(f"reference HP {tm.reference_hp():,} · blob {tm.A_BLOB} cell² · "
              f"own cell {tm.A_SELF} cell²\n")
        print(family_table())
        return 0
    rs = Ruleset(ROOT)
    for name in args:
        node = rs.resolve_weapon(name)
        if node is None:
            print(f"{name}: UNRESOLVED")
            continue
        res = analyse(node)
        if res is None:
            print(f"{name}: no damage warheads")
            continue
        print(f"\n## {name}   K = {res['k']:.3f}   sigma = {res['sigma']:.0f}"
              f"{'  (instant)' if res['instant'] else ''}")
        print("| warhead | kind | share | avgVersus | reliab | footprint | secondary |")
        print("|---|---|---|---|---|---|---|")
        for p in res["parts"]:
            print(f"| {p['tag']} | {p['kind']} | {p['share']:.3f} | {p['versus']:.2f} | "
                  f"{p['rel']:.2f} | {p['footprint']:.2f} | {p['secondary']:.2f} |")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
