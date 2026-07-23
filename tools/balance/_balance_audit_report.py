#!/usr/bin/env python3
"""Detailed balance audit: report every unit whose |delta| > 1.

For each out-of-tolerance row the range-solver is used to find the exact
range that would make class_baseline_price == cost.  If that range is inside
the class band the recommended fix is a precision range/cost tweak; if it is
outside the band the cost/stat/tech combination is inconsistent and the user
must explicitly choose a remedy.
"""
import importlib
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "docs" / "balance" / "BALANCE_AUDIT.md"

sys.path.insert(0, str(ROOT / "tools" / "balance"))

MODULES = {
    "scout": importlib.import_module("scout_rebalance_proposal_final"),
    "closecombat": importlib.import_module("closecombat_rebalance_proposal_final"),
    "special_forces": importlib.import_module("special_forces_rebalance_proposal_final"),
}


def run_pipeline(name, mod):
    rows = mod.build_rows()
    if hasattr(mod, "resolve_dps_uniqueness"):
        mod.resolve_dps_uniqueness(rows)
    mod.spread_duplicates(rows)
    return rows


def row_report(name, mod, r):
    solve = mod.solve_class_range
    HP0 = mod.HP0; SPD0 = mod.SPD0; RNG0 = mod.RNG0; DPS0 = mod.DPS0; COST0 = mod.COST0
    RANGE_MIN = mod.RANGE_MIN; RANGE_MAX = mod.RANGE_MAX
    tech = r.get("tech", 1.0)
    required = solve(r["cost"], r["hp"], r["spd"], r["dps_eff"], tech)
    in_band = RANGE_MIN <= required <= RANGE_MAX
    price_min = mod.formula.class_baseline_price(
        r["hp"], r["spd"], RANGE_MIN, r["dps_eff"],
        HP0, SPD0, RNG0, DPS0, COST0, tech_tier=tech,
    )
    price_max = mod.formula.class_baseline_price(
        r["hp"], r["spd"], RANGE_MAX, r["dps_eff"],
        HP0, SPD0, RNG0, DPS0, COST0, tech_tier=tech,
    )
    return {
        "actor": r["actor"],
        "faction": r["faction"],
        "hp": r["hp"],
        "spd": r["spd"],
        "rng": r["rng"],
        "cost": r["cost"],
        "price": r["price"],
        "delta": r["delta"],
        "required_rng": required,
        "in_band": in_band,
        "price_min": price_min,
        "price_max": price_max,
    }


def main():
    sections = []
    for name, mod in MODULES.items():
        rows = run_pipeline(name, mod)
        bad = [r for r in rows if abs(r["delta"]) > 1]
        sections.append((name, bad, rows))

    lines = ["# Infantry Rebalance Delta Audit", ""]
    lines.append("This audit lists every unit whose formula price does not match its target cost")
    lines.append("within +/- 1 credit.  The range solver is used to identify whether the mismatch")
    lines.append("can be fixed by a simple range adjustment or whether the cost/stat/tech combination")
    lines.append("is inconsistent with the class band.")
    lines.append("")

    for name, bad, all_rows in sections:
        lines.append(f"## {name}")
        lines.append("")
        lines.append(f"- Total units: {len(all_rows)}")
        lines.append(f"- Units with |Δ| > 1: {len(bad)}")
        lines.append("")
        if not bad:
            lines.append("No delta issues.\n")
            continue
        lines.append("| actor | HP | spd | current rng | cost | formula price | Δ | required rng | in band | min price in band | max price in band | recommendation |")
        lines.append("|---|---|---|---|---|---|---|---|---|---|---|---|")
        for r in bad:
            rep = row_report(name, mod, r)
            if rep["in_band"]:
                rec = f"set rng to {int(round(rep['required_rng']))} and cost to {int(round(rep['price']))}"
            else:
                if rep["required_rng"] < rep["rng"]:
                    rec = f"cost too high for band; lower cost to ~{int(round(rep['price_max']))} or raise hp/dps"
                else:
                    rec = f"cost too low for band; raise cost to ~{int(round(rep['price_min']))} or lower hp/dps"
            lines.append(
                f"| `{rep['actor']}` | {rep['hp']} | {rep['spd']} | {rep['rng']} | "
                f"{rep['cost']} | {rep['price']:.0f} | {rep['delta']:+.0f} | "
                f"{rep['required_rng']:.1f} | {rep['in_band']} | {rep['price_min']:.0f} | {rep['price_max']:.0f} | {rec} |"
            )
        lines.append("")

    OUT.write_text("\n".join(lines), encoding="utf-8")
    print(f"wrote {OUT}")


if __name__ == "__main__":
    main()
