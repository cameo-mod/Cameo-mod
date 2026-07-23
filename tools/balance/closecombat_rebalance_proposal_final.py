#!/usr/bin/env python3
"""closecombat_rebalance_proposal_final.py — curated close-combat infantry report."""
from __future__ import annotations

import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/balance"))
import formula  # noqa: E402

HP0, SPD0, RNG0, DPS0, COST0 = 50000, 75, 3500, 233.3333, 200
RANGE_MIN, RANGE_MAX = 2500, 4490
SPD_MIN, SPD_MAX = 20, 100
DAMAGE_STEP = 2000

# actor, faction, hp, spd, cost, dmg, burst, rl, fp0, wc, tech, note
UNITS = [
    ("td_gdi_shotgunner", "td_gdi", 50000, 75, 200, 4000, 5, 75, 1.00, 0.875, 1.0, "anchor"),
    ("asianalliance_fanatic", "asianalliance", 100000, 75, 500, 4000, 10, 75, 1.00, 0.875, 1.0, "verifier"),
    ("alien.nax", "naxis", 15000, 40, 110, 8000, 2, 54, 1.00, 1.000, 1.0, ""),
    ("naxis_sssoldier", "naxis", 63000, 55, 240, 4000, 10, 75, 1.36, 0.875, 0.75, ""),
]

BURST_DELAYS = {
    "alien.nax": 3,
    "naxis_sssoldier": 5,
}

PROTECTED = {"td_gdi_shotgunner", "asianalliance_fanatic"}
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


def dps_with_fp(base_d, fp):
    return base_d * fp


def build_rows():
    rows = []
    for actor, faction, hp, spd, cost, dmg, burst, rl, fp0, wc, tech, note in UNITS:
        bd = BURST_DELAYS.get(actor, 0)
        base = base_dps(dmg, rl, burst, bd, wc)
        d = base * fp0
        cost_auto = cost <= 0
        if cost_auto:
            cost = int(round(
                formula.class_baseline_price(
                    hp, spd, RNG0, d,
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
            "dps_eff": d,
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
        "# Closecombat infantry rebalance proposal (curated)",
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
    path = ROOT / "docs" / "balance" / "proposal_closecombat_infantry.md"
    path.write_text(render_report(rows), encoding="utf-8")
    print(f"wrote {path.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
