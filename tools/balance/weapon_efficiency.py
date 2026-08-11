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

import pathlib
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


def versus_of(node) -> dict[str, float]:
    """{armor: percent} from a warhead node's Versus block; missing armors = 100."""
    versus = node.child("Versus")
    if versus is None:
        return {}
    out = {}
    for child in versus.children:
        try:
            out[child.key] = float(str(child.value).strip())
        except (TypeError, ValueError):
            continue
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
        try:
            pct_damage = float(str(node.get("Damage")))
        except (TypeError, ValueError):
            continue
        # HealthPercentageDamage: %HP dealt = Damage x Versus/100. Convert to HP via ref.
        hp_equiv = ref_hp * pct_damage / 100.0
        parts.append({"tag": node.key.split("@", 1)[1],
                      "share": hp_equiv / flat_total, "versus": versus, "rel": rel,
                      "secondary": tm.footprint_targets(footprint, density),
                      "footprint": footprint, "kind": "pct"})

    k = sum(p["share"] * p["versus"] * (p["rel"] + p["secondary"]) for p in parts)
    return {"k": k, "sigma": sigma, "instant": is_instant, "parts": parts,
            "effective": damage_total * k, "ref_hp": ref_hp}


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
        rows.append((res["k"] * damage_total, fam, res, main, chip, pct))
    rows.sort(reverse=True)

    out = [f"| # | family (Heavy) | avgVersus | reliab | footprint cell² | secondary | "
           f"chip | %-twin | **K** | **effective @ {damage_total:,.0f}** |",
           "|---|---|---|---|---|---|---|---|---|---|"]
    for i, (eff, fam, res, main, chip, pct) in enumerate(rows, 1):
        out.append(
            f"| {i} | {fam} | {main['versus']:.2f} | {main['rel']:.2f} | "
            f"{main['footprint']:.2f} | {main['secondary']:.2f} | "
            f"{'yes' if chip else '—'} | {'yes' if pct else '—'} | "
            f"**{res['k']:.2f}** | **{eff:,.0f}** |")
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
