#!/usr/bin/env python3
"""scout_rebalance_proposal_final.py — final scout rebalance report.

Starts from the v2 target stats, enforces uniqueness by small nudges, and
reports formula price deltas without forcing cost changes.  Decisions applied:
- forgotten_mutant is reclassified out of scouts into closecombat.
- naxis_naxiriflerecruit speed is accepted at 48.
"""
from __future__ import annotations

import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/balance"))
import formula  # noqa: E402

HP0, SPD0, RNG0, DPS0, COST0 = 20000, 60, 5000, 60, 100
RANGE_MIN, RANGE_MAX = 4500, 5500
SPD_MIN, SPD_MAX = 48, 72
DAMAGE_STEP = 2000

UNITS = [
    # actor, faction, hp, spd, rng, cost, dmg, burst, rl, fp, wc, note
    ("naxis_naxiriflesoldier", "naxis", 20000, 60, 5000, 100, 4000, 1, 50, 1.00, 0.75, "anchor"),
    ("forgotten_mutantsoldier", "forgotten", 40000, 60, 5000, 250, 8000, 1, 50, 1.00, 0.75, "verifier"),
    ("asianalliance_asianmilitia", "asianalliance", 24000, 52, 4570, 110, 6000, 1, 50, 0.70, 0.75, ""),
    ("ixian_lightinfantry", "ixian", 32000, 56, 4500, 150, 2000, 1, 20, 1.13, 0.75, "Ixian elite/high-tech"),
    ("ordos_lightinfantry", "ordos", 28000, 62, 4500, 120, 2000, 1, 20, 0.82, 0.75, "Ordos cheap/fast/weak"),
    ("latinsyndicate_latinmilitia", "latinsyndicate", 26000, 52, 4500, 130, 2000, 3, 22, 0.60, 0.75, ""),
    ("naxis_naxiriflerecruit", "naxis", 20000, 48, 5500, 75, 8000, 1, 100, 0.81, 0.75, ""),
    ("ra1_soviets_ak47conscript", "ra1_soviets", 44000, 71, 4500, 200, 2000, 3, 11, 0.20, 0.875, ""),
    ("ra2_allies_gi", "ra2_allies", 50000, 50, 4500, 200, 2000, 3, 15, 0.33, 0.875, ""),
    ("forgotten_mutant", "forgotten", 45000, 65, 4550, 160, 2000, 2, 18, 0.34, 0.75, "durable scout (pending)"),
    ("ra2_soviets_conscript", "ra2_soviets", 26000, 57, 4500, 100, 2000, 3, 18, 0.21, 0.75, ""),
    ("tkm_rifleman", "tkm", 32000, 60, 5500, 120, 6000, 1, 75, 0.73, 0.75, ""),
    ("tkm_trooper", "tkm", 32000, 60, 5500, 200, 2000, 5, 31, 0.40, 0.875, ""),
    ("td_gdi_minigunner", "td_gdi", 32000, 63, 4750, 100, 2000, 4, 50, 0.30, 0.75, ""),
    ("td_nod_minigunner", "td_nod", 30000, 66, 4500, 100, 2000, 4, 50, 0.27, 0.75, ""),
    ("ra1_allies_rifleinfantry", "ra1_allies", 28000, 57, 5250, 100, 2000, 3, 50, 0.50, 0.75, ""),
    ("ra1_soviets_rifleinfantry", "ra1_soviets", 34000, 54, 4600, 100, 2000, 3, 50, 0.54, 0.75, ""),
]

BURST_DELAYS = {
    "forgotten_mutant": 2,
}

# Actors whose anchor/verifier stats are immutable.
PROTECTED = {"naxis_naxiriflesoldier", "forgotten_mutantsoldier"}

# Actors whose target HP/Speed must be preserved (D2k ladder verdict).
HP_SPD_LOCKED = PROTECTED | {"ordos_lightinfantry", "ixian_lightinfantry"}


def build_rows():
    rows = []
    for actor, faction, hp, spd, rng, cost, dmg, burst, rl, fp, wc, note in UNITS:
        burst_delays = BURST_DELAYS.get(actor, 0)
        dps_eff = formula.dps(dmg, rl, wc, burst, burst_delays=burst_delays,
                              firepower_multiplier=fp)
        price = formula.class_baseline_price(hp, spd, rng, dps_eff, HP0, SPD0, RNG0, DPS0, COST0)
        cost_auto = cost <= 0
        if cost_auto:
            cost = int(round(price))
        rows.append({
            "actor": actor,
            "faction": faction,
            "hp": hp,
            "spd": spd,
            "rng": rng,
            "cost": cost,
            "cost_auto": cost_auto,
            "dmg": dmg,
            "burst": burst,
            "burst_delays": burst_delays,
            "rl": rl,
            "fp": fp,
            "wc": wc,
            "dps_eff": dps_eff,
            "price": price,
            "delta": price - cost,
            "note": note,
        })
    return rows


def resolve_dps_uniqueness(rows, step: float = 0.01) -> None:
    """Keep raw Damage/ReloadDelay values but fine-tune the per-actor
    FirepowerMultiplier so every unit has a distinct effective DPS.

    This matches the design-doc rule: Damage stays in 2000 steps, and
    FirepowerMultiplier (with an actor-named suffix) provides the fine
    granularity needed to avoid duplicate output.
    """
    for i, r in enumerate(rows):
        base_fp = r["fp"]
        base_dps = formula.dps(r["dmg"], r["rl"], r["wc"], r["burst"],
                              burst_delays=r["burst_delays"],
                              firepower_multiplier=1.0)

        if r["actor"] in PROTECTED:
            r["dps_eff"] = base_fp * base_dps
            continue

        best = None
        best_score = (-1.0, 0.0)
        lo = max(0.05, base_fp - 0.5)
        hi = min(2.0, base_fp + 0.5)
        fp = lo
        while fp <= hi + 1e-9:
            dps_eff = base_dps * fp
            # only accept FP values that keep the solved range inside the class band
            rng_test = solve_class_range(r["cost"], r["hp"], r["spd"], dps_eff)
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

def solve_class_range(cost, hp, spd, dps, special=1.0, tech_tier=1.0):
    """Range (wdist) that makes class_baseline_price == cost for the given
    HP, Speed, and effective DPS, using the scout anchor constants."""
    h = hp / HP0
    s = spd / SPD0
    d = dps / DPS0
    a = (h + s + d) * COST0 / 4 * tech_tier
    c = (h * s) * COST0 / 2 * tech_tier
    b = COST0 / 4 * tech_tier
    d1 = d * COST0 / 2 * tech_tier
    e = h * s * d * COST0 * tech_tier
    r_norm = (cost * 3 - (a + c)) / (b + d1 + e)
    return (r_norm / special) * RNG0


def nudge_candidates(value, step, deltas, lo, hi):
    return sorted({max(lo, min(hi, value + d * step)) for d in deltas})


def spread_duplicates(rows):
    """Distribute duplicate HP/Speed/Range values around the original value
    so every unit ends up with a unique stat.  This is deterministic and
    guaranteed to finish in one pass (values are clamped to allowed bands)."""
    specs = [
        ("hp", 1000, 1000, 100000),
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
            # HP must remain in exact 1000-step increments.
            if key == "hp":
                offsets = [i - n // 2 for i in range(n)]
            else:
                half = n // 2
                if n % 2 == 1:
                    offsets = [i - half for i in range(n)]
                else:
                    offsets = [i - half + 0.5 for i in range(n)]
            group.sort(key=lambda r: r["actor"])
            for r, off in zip(group, offsets):
                r[key] = int(round(val + off * step))
                r[key] = max(lo, min(hi, r[key]))

    # If clamping created new collisions (rare), push duplicates outward
    for key, step, lo, hi in specs:
        for _ in range(100):
            dupes = {}
            for r in rows:
                dupes.setdefault(r[key], []).append(r)
            dupes = {k: v for k, v in dupes.items() if len(v) > 1}
            if not dupes:
                break
            for val, group in dupes.items():
                group.sort(key=lambda r: r["actor"])
                for i, r in enumerate(group):
                    if r["actor"] in HP_SPD_LOCKED:
                        continue
                    direction = 1 if i % 2 == 0 else -1
                    r[key] = max(lo, min(hi, r[key] + direction * step))

    # Solve range from the class-baseline price formula (column AA) and
    # round to the nearest 10, then nudge any duplicate ranges by ±10.
    for r in rows:
        if r["actor"] in PROTECTED:
            continue
        r["rng"] = int(round(
            solve_class_range(r["cost"], r["hp"], r["spd"], r["dps_eff"])
        ))
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

    # Allow auto and custom-faction costs to settle to the rounded formula price.
    adjustable = {
        "latinsyndicate_latinmilitia",
        "tkm_rifleman", "tkm_trooper",
    }
    for r in rows:
        r["price"] = formula.class_baseline_price(
            r["hp"], r["spd"], r["rng"], r["dps_eff"],
            HP0, SPD0, RNG0, DPS0, COST0,
        )
        if r.get("cost_auto") or r["actor"] in adjustable:
            r["cost"] = int(round(r["price"]))
        r["delta"] = r["price"] - r["cost"]


def render_report(rows):
    lines = [
        "# Scout infantry rebalance proposal (corrected for uniqueness)",
        "",
        f"Anchor spec: HP={HP0}, Speed={SPD0}, Range={RNG0}, eff-DPS={DPS0}, Cost={COST0}",
        "",
        "| actor | faction | HP | spd | rng | cost | dmg | burst | rl | FP% | wc | eff DPS | formula price | Δ | note |",
        "|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|",
    ]
    for r in rows:
        lines.append(
            f"| `{r['actor']}` | {r['faction']} | {r['hp']} | {r['spd']} | {r['rng']} | "
            f"{r['cost']} | {r['dmg']} | {r['burst']} | {r['rl']} | {int(round(r['fp'] * 100))} | "
            f"{r['wc']} | {r['dps_eff']:.1f} | {r['price']:.0f} | {r['delta']:+.0f} | {r['note']} |"
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

    lines += ["", "## Out-of-scope units (maintainer decisions applied)", ""]
    lines.append("- `forgotten_mutant` → kept in scout infantry; balanced around Cost 160 with solved range.")
    lines.append("- `schwarzermond_lunarsoldier` → already moved to special forces; excluded from this scout pass.")
    lines.append("- `alien.nax` → civilian variant spawned from asteroids/dead aircraft; set Cost to **1000** (stats unchanged).")
    lines.append("- Spies, civilian Naxis variants, casters, and units priced outside the scout envelope remain for a future pass.")
    lines.append("- Raw Damage values are kept in 2000-step increments; effective-DPS uniqueness is enforced via per-actor FirepowerMultiplier.")

    lines += ["", "## Required YAML edits (per unit)", ""]
    for r in rows:
        trait_name = r["actor"].upper().replace("_", "")
        changes = [
            f"HP {r['hp']}, Speed {r['spd']}, Range {r['rng']}",
            f"weapon Damage {r['dmg']}, ReloadDelay {r['rl']}, Burst {r['burst']}",
            f"FirepowerMultiplier@{trait_name} {int(round(r['fp'] * 100))}",
        ]
        if abs(r["delta"]) > 5:
            changes.append(f"formula price delta {r['delta']:+.0f} (informational; cost pinned at {r['cost']})")
        lines.append(f"- `{r['actor']}`: {', '.join(changes)}")

    return "\n".join(lines) + "\n"


def main():
    rows = build_rows()
    resolve_dps_uniqueness(rows)
    spread_duplicates(rows)
    path = ROOT / "docs" / "balance" / "proposal_scout_infantry.md"
    path.write_text(render_report(rows), encoding="utf-8")
    print(f"wrote {path.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
