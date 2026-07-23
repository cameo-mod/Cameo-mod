#!/usr/bin/env python3
"""scout_rebalance_final.py — scout rebalance proposal with bounded uniqueness fix.

Starts from the v2 target stats, then:
1. Assigns a unique (Damage, ReloadDelay) weapon profile to each unit while
   preserving the original effective DPS via FirepowerMultiplier.
2. Slightly nudges HP (±1000), Speed (±1), and Range (±10) to remove duplicates.
3. Recomputes the formula price and reports any cost delta.

The search is hard-capped at 1000 iterations so it can never hang.
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

PINNED_FACTIONS = {
    "td_gdi", "td_nod", "ts_gdi", "ts_nod", "ra1_allies", "ra1_soviets",
    "ra2_allies", "ra2_soviets",
}

UNITS = [
    # actor, faction, hp, spd, rng, cost, dmg, burst, rl, fp, wc, note
    ("naxis_naxiriflesoldier", "naxis", 30000, 50, 5500, 100, 6000, 1, 75, 0.70, 0.75, "anchor-ish baseline"),
    ("forgotten_mutantsoldier", "forgotten", 40000, 60, 5000, 250, 8000, 1, 50, 1.00, 0.75, "verifier"),
    ("asianalliance_asianmilitia", "asianalliance", 24000, 52, 4570, 100, 6000, 1, 50, 0.70, 0.75, ""),
    ("ixian_lightinfantry", "ixian", 36000, 52, 4500, 150, 4000, 1, 20, 0.54, 0.75, ""),
    ("ordos_lightinfantry", "ordos", 36000, 52, 4500, 150, 4000, 1, 20, 0.54, 0.75, ""),
    ("light_inf", "d2k_shared", 36000, 52, 4500, 150, 4000, 1, 20, 0.54, 0.75, ""),
    ("latinsyndicate_latinmilitia", "latinsyndicate", 26000, 52, 4500, 130, 2000, 3, 22, 0.60, 0.75, ""),
    ("naxis_naxiriflerecruit", "naxis", 20000, 48, 5500, 75, 8000, 1, 100, 0.81, 0.75, ""),
    ("ra1_soviets_ak47conscript", "ra1_soviets", 44000, 71, 4500, 200, 2000, 3, 11, 0.20, 0.875, ""),
    ("ra2_allies_gi", "ra2_allies", 50000, 50, 4500, 200, 2000, 3, 15, 0.33, 0.875, ""),
    ("ra2_soviets_conscript", "ra2_soviets", 26000, 57, 4500, 100, 2000, 1, 18, 0.63, 0.75, ""),
    ("schwarzermond_lunarsoldier", "schwarzermond", 24000, 60, 4500, 120, 6000, 1, 50, 0.83, 0.75, ""),
    ("tkm_rifleman", "tkm", 32000, 60, 5500, 120, 6000, 1, 75, 0.73, 0.75, ""),
    ("tkm_trooper", "tkm", 32000, 60, 5500, 200, 2000, 5, 31, 0.40, 0.875, ""),
    ("td_gdi_minigunner", "td_gdi", 32000, 63, 4750, 100, 2000, 4, 50, 0.30, 0.75, ""),
    ("td_nod_minigunner", "td_nod", 30000, 66, 4500, 100, 2000, 4, 50, 0.27, 0.75, ""),
    ("ra1_allies_rifleinfantry", "ra1_allies", 28000, 57, 5250, 100, 2000, 3, 50, 0.50, 0.75, ""),
    ("ra1_soviets_rifleinfantry", "ra1_soviets", 34000, 54, 4600, 100, 2000, 3, 50, 0.54, 0.75, ""),
]


def build_rows():
    rows = []
    for actor, faction, hp, spd, rng, cost, dmg, burst, rl, fp, wc, note in UNITS:
        dps_eff = formula.dps(dmg, rl, wc, burst) * fp
        price = formula.class_baseline_price(hp, spd, rng, dps_eff, HP0, SPD0, RNG0, DPS0, COST0)
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
            "price": price,
            "delta": price - cost,
            "note": note,
        })
    return rows


def weapon_profile_options(row):
    """Yield (damage, reload, fp) tuples near the original that keep effective DPS."""
    dps_eff = row["dps_eff"]
    wc = row["wc"]
    burst = row["burst"]
    base_dmg = row["dmg"]
    base_rl = row["rl"]
    base_fp = row["fp"]
    out = []
    for dmg_delta in (-2, -1, 0, 1, 2):
        dmg = base_dmg + dmg_delta * DAMAGE_STEP
        if dmg < DAMAGE_STEP:
            continue
        for rl in range(max(1, base_rl - 5), base_rl + 6):
            raw = formula.dps(dmg, rl, wc, burst)
            if raw == 0:
                continue
            fp = dps_eff / raw
            if not (0.05 <= fp <= 2.0):
                continue
            # prefer staying close to original values
            score = (
                abs(dmg - base_dmg) / DAMAGE_STEP
                + abs(rl - base_rl)
                + abs(fp - base_fp) * 100
            )
            out.append((score, dmg, rl, fp))
    out.sort()
    return [(dmg, rl, fp) for _, dmg, rl, fp in out]


def resolve_weapon_profiles(rows):
    """Backtracking assignment of unique (dmg, rl) profiles."""
    options = {r["actor"]: weapon_profile_options(r) for r in rows}
    order = sorted(rows, key=lambda r: len(options[r["actor"]]))
    assigned = {}
    used_pairs = set()

    def recurse(idx):
        if idx == len(order):
            return True
        row = order[idx]
        for dmg, rl, fp in options[row["actor"]]:
            pair = (dmg, rl)
            if pair in used_pairs:
                continue
            assigned[row["actor"]] = (dmg, rl, fp)
            used_pairs.add(pair)
            if recurse(idx + 1):
                return True
            used_pairs.remove(pair)
            del assigned[row["actor"]]
        return False

    ok = recurse(0)
    if not ok:
        raise RuntimeError("Could not assign unique weapon profiles")

    for r in rows:
        dmg, rl, fp = assigned[r["actor"]]
        r["dmg"] = dmg
        r["rl"] = rl
        r["fp"] = fp
        # price does not change because dps_eff is preserved


def nudge_value(row, key, deltas, lo, hi):
    """Try small nudges; return (new_value, deviation_cost) or None."""
    base = row[key]
    best = None
    for d in deltas:
        v = base + d
        if v < lo or v > hi:
            continue
        # cost = how far from original stat (weighted)
        if key == "hp":
            cost = abs(d) / 1000
        elif key == "spd":
            cost = abs(d)
        else:
            cost = abs(d) / 10
        if best is None or cost < best[1]:
            best = (v, cost)
    return best


def resolve_stat_uniqueness(rows, max_iter=1000):
    """Iteratively resolve duplicate HP/Speed/Range values."""
    keys = [("hp", 1000, 1, 200000), ("spd", 1, SPD_MIN, SPD_MAX), ("rng", 10, RANGE_MIN, RANGE_MAX)]
    for _ in range(max_iter):
        changed = False
        for key, step, lo, hi in keys:
            for r in rows:
                vals = [x[key] for x in rows]
                if vals.count(r[key]) <= 1:
                    continue
                # find a nudge that removes the duplicate and minimally affects price
                candidates = [nudge_value(r, key, [-2*step, -step, step, 2*step], lo, hi)]
                best = None
                for cand in candidates:
                    if cand is None:
                        continue
                    new_val, stat_cost = cand
                    # check uniqueness after applying
                    if sum(1 for x in rows if x[key] == new_val) == 0:
                        # also check range band etc already enforced
                        if best is None or stat_cost < best[1]:
                            best = cand
                if best is None:
                    continue
                r[key] = best[0]
                r["price"] = formula.class_baseline_price(
                    r["hp"], r["spd"], r["rng"], r["dps_eff"],
                    HP0, SPD0, RNG0, DPS0, COST0,
                )
                r["delta"] = r["price"] - r["cost"]
                changed = True
        if not changed:
            return True
    return False


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
    # individual HP/SPD/RNG uniqueness
    for key, label in (("hp", "HP"), ("spd", "Speed"), ("rng", "Range")):
        dupes = {}
        for r in rows:
            dupes.setdefault(r[key], []).append(r["actor"])
        dupes = {k: v for k, v in dupes.items() if len(v) > 1}
        if dupes:
            ok = False
            lines.append(f"- **{label} duplicates**: {dupes}")
    # weapon profile uniqueness
    pairs = {}
    for r in rows:
        pairs.setdefault((r["dmg"], r["rl"]), []).append(r["actor"])
    pair_dupes = {k: v for k, v in pairs.items() if len(v) > 1}
    if pair_dupes:
        ok = False
        lines.append(f"- **Damage+Reload profile duplicates**: {pair_dupes}")
    # also report any remaining individual Damage or Reload duplicates as a note
    for key, label in (("dmg", "Damage"), ("rl", "ReloadDelay")):
        dupes = {}
        for r in rows:
            dupes.setdefault(r[key], []).append(r["actor"])
        dupes = {k: v for k, v in dupes.items() if len(v) > 1}
        if dupes:
            lines.append(f"- *{label} shared by multiple units (acceptable under pair-uniqueness)*: {dupes}")
    if ok:
        lines.append("- All uniqueness checks passed.")

    lines += ["", "## Out-of-scope units (maintainer decision needed)", ""]
    lines.append("- `forgotten_mutant` → reclassified to closecombat infantry (was range 3132).")
    lines.append("- Spies, civilian Naxis variants, casters, and units priced outside the scout envelope remain for a future pass.")
    lines.append("- Strict individual Damage uniqueness requires damage values beyond 16000 for 18 scout units; this report uses unique (Damage, ReloadDelay) profiles instead. Confirm whether to accept this relaxation or reclassify more units out of scouts.")

    lines += ["", "## Required YAML edits (per unit)", ""]
    for r in rows:
        changes = [
            f"HP {r['hp']}, Speed {r['spd']}, Range {r['rng']}",
            f"weapon Damage {r['dmg']}, ReloadDelay {r['rl']}, Burst {r['burst']}",
            f"FirepowerMultiplier@Scout {int(round(r['fp'] * 100))}",
        ]
        if abs(r["delta"]) > 5:
            if r["faction"] in PINNED_FACTIONS:
                changes.append(f"price delta {r['delta']:+.0f} (cost is pinned; review)")
            else:
                changes.append(f"Cost {r['cost']} → {int(round(r['price'] / 10) * 10)}")
        lines.append(f"- `{r['actor']}`: {', '.join(changes)}")

    return "\n".join(lines) + "\n"


def main():
    rows = build_rows()
    resolve_weapon_profiles(rows)
    if not resolve_stat_uniqueness(rows):
        print("WARNING: could not fully resolve HP/Speed/Range uniqueness in 1000 iterations", file=sys.stderr)
    path = ROOT / "docs" / "balance" / "proposal_scout_infantry.md"
    path.write_text(render_report(rows), encoding="utf-8")
    print(f"wrote {path.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
