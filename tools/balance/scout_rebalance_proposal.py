#!/usr/bin/env python3
"""scout_rebalance_proposal.py — fast standalone proposal for the scout class.

Inputs are the existing v2 proposal stats from docs/balance/formula_v2_scout.md.
No ledger parsing; runs in <1s. Outputs a markdown report with
formula-checked prices and uniqueness warnings resolved by small nudges.
"""
from __future__ import annotations

import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/balance"))
import formula  # noqa: E402

# Anchor: the class baseline spec (naxis_naxiriflesoldier model in v2 table)
HP0, SPD0, RNG0, DPS0, COST0 = 20000, 60, 5000, 60, 100

# Effective DPS = raw_dps * weapon_class * firepower_mult
# Scout weapon class for SA-only is 0.75; SA+CG is 0.875

def eff_dps(dmg, rl, burst, fp, wc):
    return dmg * burst / rl * wc * fp


def price(hp, spd, rng, dps_eff, special=1.0, tier=1.0):
    return formula.class_baseline_price(
        hp, spd, rng, dps_eff,
        HP0, SPD0, RNG0, DPS0, COST0,
        special, tier,
    )


def solve_range_for_cost(cost, hp, spd, dps_eff, special=1.0, tier=1.0):
    # class_baseline_price is not linear in range, but we can binary-search
    lo, hi = 1000, 20000
    for _ in range(60):
        mid = (lo + hi) / 2
        if price(hp, spd, mid, dps_eff, special, tier) < cost:
            lo = mid
        else:
            hi = mid
    return mid


# Existing v2 proposal rows (solved units only).  cost is the target/actual cost.
# wc = weapon class coefficient; fp = FirepowerMultiplier as a fraction.
UNITS = [
    # actor, faction, hp, spd, rng, cost, wc, dmg, burst, rl, fp, note
    ("naxis_naxiriflesoldier", "naxis", 30000, 50, 5500, 100, 0.75, 6000, 1, 75, 0.70, "anchor-ish baseline"),
    ("forgotten_mutant", "forgotten", 36000, 65, 3250, 120, 0.75, 2000, 2, 18, 0.27, "OUTLIER: range below scout band"),
    ("forgotten_mutantsoldier", "forgotten", 40000, 60, 5000, 250, 0.75, 8000, 1, 50, 1.00, "verifier"),
    ("asianalliance_asianmilitia", "asianalliance", 24000, 52, 4570, 100, 0.75, 6000, 1, 50, 0.70, ""),
    ("ixian_lightinfantry", "ixian", 36000, 52, 4500, 150, 0.75, 4000, 1, 20, 0.54, ""),
    ("ordos_lightinfantry", "ordos", 36000, 52, 4500, 150, 0.75, 4000, 1, 20, 0.54, ""),
    ("light_inf", "d2k_shared", 36000, 52, 4500, 150, 0.75, 4000, 1, 20, 0.54, ""),
    ("latinsyndicate_latinmilitia", "latinsyndicate", 26000, 52, 4500, 130, 0.75, 2000, 3, 22, 0.60, ""),
    ("naxis_naxiriflerecruit", "naxis", 20000, 45, 5500, 75, 0.75, 8000, 1, 100, 0.81, ""),
    ("ra1_soviets_ak47conscript", "ra1_soviets", 44000, 71, 4500, 200, 0.875, 2000, 3, 11, 0.20, ""),
    ("ra2_allies_gi", "ra2_allies", 50000, 50, 4500, 200, 0.875, 2000, 3, 15, 0.33, ""),
    ("ra2_soviets_conscript", "ra2_soviets", 26000, 57, 4500, 100, 0.75, 2000, 1, 18, 0.63, ""),
    ("schwarzermond_lunarsoldier", "schwarzermond", 24000, 60, 4500, 120, 0.75, 6000, 1, 50, 0.83, ""),
    ("tkm_rifleman", "tkm", 32000, 60, 5500, 120, 0.75, 6000, 1, 75, 0.73, ""),
    ("tkm_trooper", "tkm", 32000, 60, 5500, 200, 0.875, 2000, 5, 31, 0.40, ""),
    # classic C&C rifles (from formula_v2_scout.md classic section, v2 targets)
    ("td_gdi_minigunner", "td_gdi", 32000, 63, 4750, 100, 0.75, 2000, 4, 50, 0.30, ""),
    ("td_nod_minigunner", "td_nod", 30000, 66, 4500, 100, 0.75, 2000, 4, 50, 0.27, ""),
    ("ra1_allies_rifleinfantry", "ra1_allies", 28000, 57, 5250, 100, 0.75, 2000, 3, 50, 0.50, ""),
    ("ra1_soviets_rifleinfantry", "ra1_soviets", 34000, 54, 4600, 100, 0.75, 2000, 3, 50, 0.54, ""),
]


def make_unique(rows, keys, step, name_getter):
    """Iteratively nudge integer-valued keys to remove duplicates."""
    changed = True
    while changed:
        changed = False
        for i, r in enumerate(rows):
            for k in keys:
                vals = [rr[k] for rr in rows]
                if vals.count(r[k]) > 1:
                    # nudge upward or downward depending on position
                    order = sorted(set(vals))
                    idx = order.index(r[k])
                    direction = 1 if idx < len(order) // 2 else -1
                    r[k] += direction * step
                    changed = True
    return rows


def main():
    rows = []
    for actor, faction, hp, spd, rng, cost, wc, dmg, burst, rl, fp, note in UNITS:
        dps_eff = eff_dps(dmg, rl, burst, fp, wc)
        pr = price(hp, spd, rng, dps_eff)
        delta = pr - cost
        rows.append({
            "actor": actor,
            "faction": faction,
            "hp": hp,
            "spd": spd,
            "rng": rng,
            "cost": cost,
            "dmg": dmg,
            "burst": burst,
            "rl": rl,
            "fp": fp,
            "wc": wc,
            "dps_eff": dps_eff,
            "price": pr,
            "delta": delta,
            "note": note,
        })

    # Resolve uniqueness on HP and speed with ±1000 / ±1 nudges
    rows = make_unique(rows, ["hp"], 1000, lambda r: r["actor"])
    rows = make_unique(rows, ["spd"], 1, lambda r: r["actor"])

    # For range: if the v2 range produces the target cost, keep it; otherwise
    # solve for range.  Then ensure uniqueness by tiny ±10 wdist nudges.
    for r in rows:
        if abs(r["delta"]) > 5:
            r["rng"] = round(solve_range_for_cost(r["cost"], r["hp"], r["spd"], r["dps_eff"]) / 10) * 10
            r["price"] = price(r["hp"], r["spd"], r["rng"], r["dps_eff"])
            r["delta"] = r["price"] - r["cost"]
    rows = make_unique(rows, ["rng"], 10, lambda r: r["actor"])

    out = ["# Scout infantry rebalance proposal (corrected for uniqueness)", ""]
    out.append("Anchor spec: HP=%d, Speed=%d, Range=%d, eff-DPS=%d, Cost=%d" % (HP0, SPD0, RNG0, DPS0, COST0))
    out.append("")
    out.append("| actor | faction | HP | spd | rng | cost | dmg | burst | rl | FP% | wc | eff DPS | formula price | Δ | note |")
    out.append("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|")
    for r in rows:
        out.append(
            f"| `{r['actor']}` | {r['faction']} | {r['hp']} | {r['spd']} | {r['rng']} | "
            f"{r['cost']} | {r['dmg']} | {r['burst']} | {r['rl']} | {int(r['fp']*100)} | "
            f"{r['wc']} | {r['dps_eff']:.1f} | {r['price']:.0f} | {r['delta']:+.0f} | {r['note']} |"
        )

    out.append("")
    out.append("## Uniqueness check")
    out.append("")
    for key, label in [("hp", "HP"), ("spd", "Speed"), ("rng", "Range"), ("rl", "Reload")]:
        dupes = {v: [r["actor"] for r in rows if r[key] == v] for v in {r[key] for r in rows}}
        dupes = {k: v for k, v in dupes.items() if len(v) > 1}
        if dupes:
            out.append(f"- **{label} duplicates**: {dupes}")
    out.append("")
    out.append("## Required YAML edits (per unit)")
    out.append("")
    for r in rows:
        changes = []
        if r["actor"] != "forgotten_mutantsoldier":
            changes.append(f"HP {r['hp']}, Speed {r['spd']}, Range {r['rng']}")
            changes.append(f"weapon Damage {r['dmg']}, ReloadDelay {r['rl']}, Burst {r['burst']}")
            changes.append(f"FirepowerMultiplier@Scout {int(r['fp']*100)}")
        out.append(f"- `{r['actor']}`: {', '.join(changes)}")

    path = ROOT / "docs" / "balance" / "proposal_scout_infantry.md"
    path.write_text("\n".join(out) + "\n", encoding="utf-8")
    print(f"wrote {path.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
