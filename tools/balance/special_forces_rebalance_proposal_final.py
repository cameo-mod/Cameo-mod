#!/usr/bin/env python3
"""special_forces_rebalance_proposal_final.py — curated special-forces report."""
from __future__ import annotations

import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/balance"))
import formula  # noqa: E402

HP0, SPD0, RNG0, DPS0, COST0 = 15000, 50, 6000, 240, 200
RANGE_MIN, RANGE_MAX = 5500, 6500
SPD_MIN, SPD_MAX = 20, 100
DAMAGE_STEP = 2000

# actor, faction, hp, spd, cost (0 = auto-compute for range 6000), dmg, burst, rl, fp0, tech, wc, note
UNITS = [
    ("japan_imperialscoutsman", "japan", 15000, 50, 200, 12000, 1, 50, 1.00, 1.0, 1.0, "anchor"),
    ("schwarzermond_lunarsoldier", "schwarzermond", 30000, 50, 500, 24000, 1, 50, 1.00, 1.0, 1.0, "verifier"),
    ("td_nod_lasertrooper", "td_nod", 60000, 50, 750, 48000, 1, 50, 1.00, 0.5, 1.0, ""),
    ("td_nod_stealthsoldier", "td_nod", 25000, 72, 0, 22000, 4, 90, 1.00, 0.75, 1.0, ""),
    ("td_gdi_officer", "td_gdi", 32000, 80, 0, 8000, 4, 20, 1.00, 0.75, 1.0, ""),
    ("ra1_allies_machinegunner", "ra1_allies", 20000, 50, 0, 8000, 5, 48, 1.00, 1.0, 1.0, ""),
    ("ra1_soviets_dragunovantimaterialsniper", "ra1_soviets", 20000, 40, 0, 80000, 1, 85, 1.00, 0.75, 1.0, ""),
    ("ra2_allies_seal", "ra2_allies", 30000, 60, 0, 4000, 4, 10, 1.00, 0.75, 1.0, ""),
    ("ra2_soviets_flaktrooper", "ra2_soviets", 10000, 45, 0, 16000, 1, 17, 1.00, 1.0, 1.0, ""),
    ("yuri_gatlingtrooper", "yuri", 36000, 45, 0, 8000, 1, 15, 1.00, 0.75, 1.0, ""),
    ("cabal_eliminator800", "cabal", 85000, 40, 0, 4000, 1, 5, 1.00, 1.0, 1.0, ""),
    ("ts_gdi_falconenforcer", "ts_gdi", 45000, 60, 0, 8000, 3, 26, 1.00, 1.0, 1.0, ""),
    ("ts_nod_elitecadre", "ts_nod", 20000, 55, 0, 8000, 5, 52, 1.00, 0.75, 1.0, ""),
    ("forgotten_mutantsergeant", "forgotten", 40000, 75, 0, 8000, 1, 8, 1.00, 0.75, 1.0, ""),
    ("terran_ghost", "terran", 45000, 75, 0, 20000, 1, 22, 1.00, 0.75, 1.0, ""),
    ("terran_specter", "terran", 50000, 80, 0, 40000, 1, 33, 1.00, 0.75, 1.0, ""),
    ("terran_marine", "terran", 40000, 60, 0, 12000, 3, 26, 0.33, 1.0, 1.0, ""),
    ("terran_madcap", "terran", 60000, 60, 0, 12000, 1, 25, 1.00, 1.0, 1.0, ""),
    ("zerg_hydralisk", "zerg", 80000, 75, 0, 18000, 1, 15, 1.00, 1.0, 1.0, ""),
    ("steelconsortium_clonetrooper", "steelconsortium", 17000, 57, 0, 2000, 1, 25, 1.00, 1.0, 1.0, ""),
    ("asianalliance_asdf", "asianalliance", 40000, 60, 0, 2000, 3, 16, 1.00, 0.75, 0.875, ""),
    ("latinsyndicate_narco", "latinsyndicate", 28000, 70, 0, 70000, 1, 77, 1.00, 0.75, 1.0, ""),
]

BURST_DELAYS = {
    "asianalliance_asdf": 2,
}

PROTECTED = {"japan_imperialscoutsman", "schwarzermond_lunarsoldier"}
HP_SPD_LOCKED = PROTECTED


def eff_reload(rl, burst, bd=0):
    if burst and burst > 1:
        return rl + (bd or 0) * (burst - 1)
    return rl


def base_dps(dmg, rl, burst, bd, wc):
    rl_eff = eff_reload(rl, burst, bd)
    if rl_eff <= 0:
        return 0.0
    return dmg * max(burst, 1) / rl_eff * wc


def build_rows():
    rows = []
    for actor, faction, hp, spd, cost, dmg, burst, rl, fp0, tech, wc, note in UNITS:
        bd = BURST_DELAYS.get(actor, 0)
        base = base_dps(dmg, rl, burst, bd, wc)
        d0 = base * fp0
        # auto-cost: pinned costs for anchor/verifier/T4 lasertrooper; others solved to price at 6000
        cost_auto = cost <= 0
        if cost_auto:
            cost = int(round(
                formula.class_baseline_price(
                    hp, spd, RNG0, d0,
                    HP0, SPD0, RNG0, DPS0, COST0, tech_tier=tech,
                )
            ))
        rows.append({
            "actor": actor,
            "faction": faction,
            "hp": hp,
            "spd": spd,
            "rng": RNG0,
            "cost": cost,
            "cost_auto": cost_auto,
            "dmg": dmg,
            "burst": burst,
            "burst_delays": bd,
            "rl": rl,
            "fp0": fp0,
            "fp": fp0,
            "wc": wc,
            "tech": tech,
            "base_dps": base,
            "dps_eff": d0,
            "price": 0.0,
            "delta": 0.0,
            "note": note,
            "protected": actor in PROTECTED,
        })
    return rows


def resolve_dps_uniqueness(rows, step: float = 0.01) -> None:
    for i, r in enumerate(rows):
        if r["protected"]:
            r["dps_eff"] = r["base_dps"] * r["fp0"]
            continue
        base_fp = r["fp0"]
        base_d = r["base_dps"]

        best = None
        best_score = (-1.0, 0.0)
        lo = max(0.05, base_fp - 0.5)
        hi = min(2.0, base_fp + 0.5)
        fp = lo
        while fp <= hi + 1e-9:
            dps_eff = base_d * fp
            # keep the solved range inside the class band; otherwise the unit cannot price correctly
            rng_test = solve_class_range(r["cost"], r["hp"], r["spd"], dps_eff, r["tech"])
            if not (RANGE_MIN <= rng_test <= RANGE_MAX):
                fp += step
                continue
            dists = [abs(other["dps_eff"] - dps_eff) for other in rows[:i]]
            min_dist = min(dists) if dists else float("inf")
            score = (min_dist, -abs(fp - base_fp))  # maximize separation, tie-break to base
            if score > best_score:
                best_score = score
                best = (fp, dps_eff)
            fp += step

        if best is None:
            raise RuntimeError(f"Could not make effective DPS unique for {r['actor']}")
        r["fp"], r["dps_eff"] = best


def solve_class_range(cost, hp, spd, dps, tech=1.0):
    return formula.solve_class_baseline_range(
        cost, hp, spd, dps, HP0, SPD0, RNG0, DPS0, COST0, tech_tier=tech
    )


def spread_duplicates(rows):
    specs = [
        ("hp", 1000, 10000, 100000),
        ("spd", 1, SPD_MIN, SPD_MAX),
    ]
    for key, step, lo, hi in specs:
        groups = {}
        for r in rows:
            groups.setdefault(r[key], []).append(r)
        for val, group in groups.items():
            group = [r for r in group if r["actor"] not in HP_SPD_LOCKED]
            if len(group) <= 1:
                continue
            n = len(group)
            offsets = [i - n // 2 for i in range(n)]
            group.sort(key=lambda r: r["actor"])
            for r, off in zip(group, offsets):
                r[key] = int(round(val + off * step))
                r[key] = max(lo, min(hi, r[key]))

    for _ in range(100):
        for key, step, lo, hi in specs:
            dupes = {}
            for r in rows:
                dupes.setdefault(r[key], []).append(r)
            dupes = {k: v for k, v in dupes.items() if len(v) > 1}
            if not dupes:
                continue
            for val, group in dupes.items():
                group.sort(key=lambda r: r["actor"])
                for i, r in enumerate(group):
                    if r["actor"] in HP_SPD_LOCKED:
                        continue
                    direction = 1 if i % 2 == 0 else -1
                    r[key] = max(lo, min(hi, r[key] + direction * step))

    for r in rows:
        if r["protected"]:
            continue
        r["rng"] = int(round(solve_class_range(r["cost"], r["hp"], r["spd"], r["dps_eff"], r["tech"])))
        r["rng"] = max(RANGE_MIN, min(RANGE_MAX, r["rng"]))

    for _ in range(100):
        dupes = {}
        for r in rows:
            dupes.setdefault(r["rng"], []).append(r)
        dupes = {k: v for k, v in dupes.items() if len(v) > 1}
        if not dupes:
            break
        for val, group in dupes.items():
            group.sort(key=lambda r: r["actor"])
            for i, r in enumerate(group):
                if r["actor"] in PROTECTED:
                    continue
                direction = 1 if i % 2 == 0 else -1
                r["rng"] = max(RANGE_MIN, min(RANGE_MAX, r["rng"] + direction * 1))

    for r in rows:
        r["price"] = formula.class_baseline_price(
            r["hp"], r["spd"], r["rng"], r["dps_eff"],
            HP0, SPD0, RNG0, DPS0, COST0, tech_tier=r["tech"],
        )
        if r["cost_auto"]:
            r["cost"] = int(round(r["price"]))
        r["delta"] = r["price"] - r["cost"]


def render_report(rows):
    lines = [
        "# Special Forces infantry rebalance proposal (curated)",
        "",
        f"Anchor spec: HP={HP0}, Speed={SPD0}, Range={RNG0}, eff-DPS={DPS0}, Cost={COST0}",
        "",
        "| actor | faction | HP | spd | rng | cost | dmg | burst | rl | FP% | wc | tech | eff DPS | formula price | Δ | note |",
        "|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|",
    ]
    for r in rows:
        lines.append(
            f"| `{r['actor']}` | {r['faction']} | {r['hp']} | {r['spd']} | {r['rng']} | "
            f"{r['cost']} | {r['dmg']} | {r['burst']} | {r['rl']} | {int(round(r['fp'] * 100))} | "
            f"{r['wc']} | {r['tech']} | {r['dps_eff']:.1f} | {r['price']:.0f} | {r['delta']:+.0f} | {r['note']} |"
        )

    lines += ["", "## Uniqueness check", ""]
    ok = True
    for key, label in (("hp", "HP"), ("spd", "Speed"), ("rng", "Range")):
        dupes = {}
        for r in rows:
            dupes.setdefault(r[key], []).append(r["actor"])
        dupes = {k: [a for a in v if a not in PROTECTED]
                 for k, v in dupes.items() if len([a for a in v if a not in PROTECTED]) > 1}
        if dupes:
            ok = False
            lines.append(f"- **{label} duplicates**: {dupes}")
    dps_dupes = {}
    for r in rows:
        dps_dupes.setdefault(round(r["dps_eff"], 1), []).append(r["actor"])
    dps_dupes = {k: [a for a in v if a not in PROTECTED]
                 for k, v in dps_dupes.items() if len([a for a in v if a not in PROTECTED]) > 1}
    if dps_dupes:
        ok = False
        lines.append(f"- **effective DPS duplicates**: {dps_dupes}")
    if ok:
        lines.append("- All uniqueness checks passed (HP, Speed, Range, effective DPS).")

    lines += ["", "## Required YAML edits (per unit)", ""]
    for r in rows:
        trait_name = r["actor"].upper().replace(".", "").replace("_", "")
        changes = [
            f"HP {r['hp']}, Speed {r['spd']}, Range {r['rng']}",
            f"weapon Damage {r['dmg']}, ReloadDelay {r['rl']}, Burst {r['burst']}",
            f"FirepowerMultiplier@{trait_name} {int(round(r['fp'] * 100))}",
            f"tech tier {r['tech']}",
        ]
        if abs(r["delta"]) > 5:
            changes.append(f"formula price delta {r['delta']:+.0f} (informational; cost pinned at {r['cost']})")
        lines.append(f"- `{r['actor']}`: {', '.join(changes)}")

    return "\n".join(lines) + "\n"


def main():
    rows = build_rows()
    resolve_dps_uniqueness(rows)
    spread_duplicates(rows)
    path = ROOT / "docs" / "balance" / "proposal_special_forces_infantry.md"
    path.write_text(render_report(rows), encoding="utf-8")
    print(f"wrote {path.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
