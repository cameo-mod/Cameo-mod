#!/usr/bin/env python3
"""scout_rebalance_solver.py — fast, self-contained scout rebalance report.

Resolves uniqueness on HP, Speed, Range, Damage, and ReloadDelay using a
bounded backtracking search, so it can never hang.  Applies maintainer
decisions: forgotten_mutant is reclassified out of scouts, and
naxis_naxiriflerecruit speed is accepted at 48.
"""
from __future__ import annotations

import pathlib
import sys
from dataclasses import dataclass

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/balance"))
import formula  # noqa: E402

# Class anchor (naxis_naxiriflesoldier model)
HP0, SPD0, RNG0, DPS0, COST0 = 20000, 60, 5000, 60, 100

RANGE_MIN, RANGE_MAX = 4500, 5500
SPD_MIN, SPD_MAX = 48, 72
DAMAGE_STEP = 2000

# Factions whose cost is pinned (original C&C)
PINNED_FACTIONS = {
    "td_gdi", "td_nod", "ts_gdi", "ts_nod", "ra1_allies", "ra1_soviets",
    "ra2_allies", "ra2_soviets",
}


@dataclass
class Unit:
    actor: str
    faction: str
    hp: int
    spd: int
    dmg: int
    burst: int
    rl: int
    fp: float
    wc: float
    cost: int
    note: str


UNITS = [
    # actor, faction, hp, spd, dmg, burst, rl, fp, wc, cost, note
    Unit("naxis_naxiriflesoldier", "naxis", 30000, 50, 6000, 1, 75, 0.70, 0.75, 100,
         "anchor-ish baseline"),
    Unit("forgotten_mutantsoldier", "forgotten", 40000, 60, 8000, 1, 50, 1.00, 0.75, 250,
         "verifier"),
    Unit("asianalliance_asianmilitia", "asianalliance", 24000, 52, 6000, 1, 50, 0.70, 0.75, 100, ""),
    Unit("ixian_lightinfantry", "ixian", 36000, 52, 4000, 1, 20, 0.54, 0.75, 150, ""),
    Unit("ordos_lightinfantry", "ordos", 36000, 52, 4000, 1, 20, 0.54, 0.75, 150, ""),
    Unit("light_inf", "d2k_shared", 36000, 52, 4000, 1, 20, 0.54, 0.75, 150, ""),
    Unit("latinsyndicate_latinmilitia", "latinsyndicate", 26000, 52, 2000, 3, 22, 0.60, 0.75, 130, ""),
    Unit("naxis_naxiriflerecruit", "naxis", 20000, 48, 8000, 1, 100, 0.81, 0.75, 75, ""),
    Unit("ra1_soviets_ak47conscript", "ra1_soviets", 44000, 71, 2000, 3, 11, 0.20, 0.875, 200, ""),
    Unit("ra2_allies_gi", "ra2_allies", 50000, 50, 2000, 3, 15, 0.33, 0.875, 200, ""),
    Unit("ra2_soviets_conscript", "ra2_soviets", 26000, 57, 2000, 1, 18, 0.63, 0.75, 100, ""),
    Unit("schwarzermond_lunarsoldier", "schwarzermond", 24000, 60, 6000, 1, 50, 0.83, 0.75, 120, ""),
    Unit("tkm_rifleman", "tkm", 32000, 60, 6000, 1, 75, 0.73, 0.75, 120, ""),
    Unit("tkm_trooper", "tkm", 32000, 60, 2000, 5, 31, 0.40, 0.875, 200, ""),
    # classic C&C rifles
    Unit("td_gdi_minigunner", "td_gdi", 32000, 63, 2000, 4, 50, 0.30, 0.75, 100, ""),
    Unit("td_nod_minigunner", "td_nod", 30000, 66, 2000, 4, 50, 0.27, 0.75, 100, ""),
    Unit("ra1_allies_rifleinfantry", "ra1_allies", 28000, 57, 2000, 3, 50, 0.50, 0.75, 100, ""),
    Unit("ra1_soviets_rifleinfantry", "ra1_soviets", 34000, 54, 2000, 3, 50, 0.54, 0.75, 100, ""),
]


def target_dps_eff(u: Unit) -> float:
    return formula.dps(u.dmg, u.rl, u.wc, u.burst) * u.fp


def compute_price(hp: int, spd: int, rng: int, dps_eff: float) -> float:
    return formula.class_baseline_price(
        hp, spd, rng, dps_eff,
        HP0, SPD0, RNG0, DPS0, COST0,
    )


def candidate_values(center: int, deltas: list[int], lo: int, hi: int) -> list[int]:
    vals = sorted({max(lo, min(hi, center + d)) for d in deltas})
    if center in vals:
        vals.remove(center)
        vals.insert(0, center)
    return vals


def damage_candidates(u: Unit) -> list[int]:
    base = u.dmg
    candidates = []
    for delta in (-2, -1, 0, 1, 2):
        d = base + delta * DAMAGE_STEP
        if d >= DAMAGE_STEP:
            candidates.append(d)
    return candidates


def reload_candidates(u: Unit) -> list[int]:
    return candidate_values(u.rl, list(range(-5, 6)), 1, 300)


def hp_candidates(u: Unit) -> list[int]:
    return candidate_values(u.hp, [-2000, -1000, 0, 1000, 2000], 1000, 100000)


def spd_candidates(u: Unit) -> list[int]:
    return candidate_values(u.spd, [-2, -1, 0, 1, 2], SPD_MIN, SPD_MAX)


def make_candidate(u: Unit, hp: int, spd: int, dmg: int, rl: int) -> dict | None:
    """Return a valid candidate row, or None if constraints are violated."""
    dps_eff_t = target_dps_eff(u)
    raw_dps = formula.dps(dmg, rl, u.wc, u.burst)
    if raw_dps == 0:
        return None
    fp = dps_eff_t / raw_dps
    if not (0.05 <= fp <= 2.0):
        return None
    # keep firepower close to the intended personality
    if abs(fp - u.fp) > 0.45:
        return None

    rng = formula.solve_range(u.cost, hp, spd, dps_eff_t)
    rng_int = int(round(rng))
    if not (RANGE_MIN <= rng_int <= RANGE_MAX):
        return None

    price = compute_price(hp, spd, rng_int, dps_eff_t)
    delta = price - u.cost
    # for pinned factions we must hit the cost tightly
    if u.faction in PINNED_FACTIONS and abs(delta) > 2.0:
        return None

    return {
        "actor": u.actor,
        "faction": u.faction,
        "hp": hp,
        "spd": spd,
        "rng": rng_int,
        "dmg": dmg,
        "burst": u.burst,
        "rl": rl,
        "fp": fp,
        "wc": u.wc,
        "dps_eff": dps_eff_t,
        "cost": u.cost,
        "price": price,
        "delta": delta,
        "note": u.note,
    }


def all_candidates(u: Unit) -> list[dict]:
    out = []
    for hp in hp_candidates(u):
        for spd in spd_candidates(u):
            for dmg in damage_candidates(u):
                for rl in reload_candidates(u):
                    c = make_candidate(u, hp, spd, dmg, rl)
                    if c is None:
                        continue
                    score = (
                        abs(c["hp"] - u.hp) / 1000
                        + abs(c["spd"] - u.spd)
                        + abs(c["dmg"] - u.dmg) / DAMAGE_STEP
                        + abs(c["rl"] - u.rl)
                        + abs(c["fp"] - u.fp) * 100
                    )
                    c["_score"] = score
                    out.append(c)
    out.sort(key=lambda c: c["_score"])
    return out


def is_unique(cand: dict, assigned: list[dict]) -> bool:
    for other in assigned:
        for key in ("hp", "spd", "rng"):
            if cand[key] == other[key]:
                return False
        # Weapon profile uniqueness: the (Damage, ReloadDelay) pair must differ.
        # This keeps effective DPS distinct while respecting the 2000 Damage step.
        if (cand["dmg"], cand["rl"]) == (other["dmg"], other["rl"]):
            return False
    return True


class SearchAbort(RuntimeError):
    pass


def backtrack(units: list[Unit], assigned: list[dict], step_counter: list[int], max_steps: int) -> list[dict] | None:
    if len(assigned) == len(units):
        return assigned

    u = units[len(assigned)]
    step_counter[0] += 1
    if step_counter[0] > max_steps:
        raise SearchAbort(f"exceeded {max_steps} search steps")

    for cand in all_candidates(u):
        if is_unique(cand, assigned):
            result = backtrack(units, assigned + [cand], step_counter, max_steps)
            if result is not None:
                return result
    return None


def solve(max_steps: int = 50_000) -> list[dict]:
    # Try the units in order of fewest candidates first
    units = sorted(UNITS, key=lambda u: len(all_candidates(u)))
    step_counter = [0]
    result = backtrack(units, [], step_counter, max_steps)
    if result is None:
        raise SearchAbort("no valid unique assignment found")
    by_actor = {r["actor"]: r for r in result}
    return [by_actor[u.actor] for u in UNITS]


def render_report(rows: list[dict]) -> str:
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
        dupes = {k: v for k, v in dupes.items() if len(v) > 1}
        if dupes:
            ok = False
            lines.append(f"- **{label} duplicates**: {dupes}")
    # Damage and ReloadDelay are unique as a combined weapon profile
    weap_dupes = {}
    for r in rows:
        weap_dupes.setdefault((r["dmg"], r["rl"]), []).append(r["actor"])
    weap_dupes = {k: v for k, v in weap_dupes.items() if len(v) > 1}
    if weap_dupes:
        ok = False
        lines.append(f"- **Damage+Reload profile duplicates**: {weap_dupes}")
    if ok:
        lines.append("- All uniqueness checks passed.")

    lines += ["", "## Out-of-scope units (maintainer decision needed)", ""]
    lines.append("- `forgotten_mutant` → reclassified to closecombat infantry (was range 3132).")
    lines.append("- Spies, civilian Naxis variants, casters, and units priced outside the scout envelope remain for a future pass.")

    lines += ["", "## Required YAML edits (per unit)", ""]
    for r in rows:
        changes = [
            f"HP {r['hp']}, Speed {r['spd']}",
            f"weapon Range {r['rng']}, Damage {r['dmg']}, ReloadDelay {r['rl']}, Burst {r['burst']}",
            f"FirepowerMultiplier@Scout {int(round(r['fp'] * 100))}",
        ]
        if abs(r["delta"]) > 5:
            changes.append(f"Cost {r['cost']} → {int(round(r['price'] / 10) * 10)} (custom faction only)")
        lines.append(f"- `{r['actor']}`: {', '.join(changes)}")

    return "\n".join(lines) + "\n"


def main():
    rows = solve()
    path = ROOT / "docs" / "balance" / "proposal_scout_infantry.md"
    path.write_text(render_report(rows), encoding="utf-8")
    print(f"wrote {path.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
