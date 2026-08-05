#!/usr/bin/env python3
"""propose_class_rebalance.py — per-class infantry rebalance proposal.

Reads the ledger JSONs, selects all units belonging to a given class
(subtype or explicit class_anchor), keeps their current HP/Speed/Cost,
resolves effective-DPS uniqueness by fine-tuning FirepowerMultiplier,
solves the class-baseline range that makes price == cost, rounds to the
nearest 10, clamps to the class band, and writes a markdown report.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/balance"))
import formula  # noqa: E402

LEDGER_DIR = ROOT / "docs/balance"
ANCHORS_FILE = LEDGER_DIR / "class_anchors.json"

# Actors the maintainer has explicitly retired from class rosters.
EXCLUDED_ACTORS = {"light_inf"}

CLASS_BANDS = {
    # core ladder (contiguous, range-band-defined)
    "scout": (4500, 5500),
    "closecombat": (2500, 4500),
    "special_forces": (5500, 6500),
    "melee": (1250, 1750),
    "mbt": (3500, 6500),
    # role classes (band = clamp window around the anchor range; role, not range,
    # defines membership, so overlaps with the ladder are allowed)
    "grenadier": (5000, 6000),
    "mortar": (9000, 11000),      # long-range indirect fire (~10000), slow
    "heavy_infantry": (4500, 5500),
    "pure_sniper": (9000, 11000),
    "heavy_sniper": (7000, 9000),
    "rocket_trooper": (6000, 7000),
    "archer": (6500, 7500),
    "support": (0, 6000),
    "commando": (7000, 9000),
    "flying_infantry": (4000, 6000),
}


def band_for(cls: str, spec: dict) -> tuple[int, int]:
    """Clamp window for the range solver. Falls back to anchor range ±20%."""
    if cls in CLASS_BANDS:
        return CLASS_BANDS[cls]
    r0 = int(spec.get("range0_wdist") or 5000)
    return (int(r0 * 0.8), int(r0 * 1.2))


def fnum(v):
    try:
        f = float(v)
        return int(f) if f == int(f) else f
    except (TypeError, ValueError):
        return None


def subtype_to_anchor(st: str | None) -> str | None:
    """Map a design.subtype (the ^<Name>Template the actor inherits) to its
    class-anchor key. Membership is template-driven; an explicit
    design.class_anchor override still wins over this (see load_class_rows).
    MortarInfantry → grenadier and AntiTankAntiAir → rocket_trooper are
    provisional (flagged for the maintainer review + the ^AntiTankAntiAir
    split task)."""
    if not st:
        return None
    name = re.sub(r"[^A-Za-z0-9]", "", str(st)).casefold()
    exact = {
        "scoutinfantry": "scout",
        "closecombatinfantry": "closecombat",
        "specialforcesinfantry": "special_forces",
        "grenadierinfantry": "grenadier",
        "mortarinfantry": "mortar",             # separate class (long-range/slow)
        "antitankantiairinfantry": "rocket_trooper",  # provisional (AT+AA launcher)
        "rockettrooperinfantry": "rocket_trooper",
        "heavyinfantry": "heavy_infantry",
        "meleeinfantry": "melee",
        "sniperinfantry": "pure_sniper",
        "heavysniperinfantry": "heavy_sniper",
        "archerinfantry": "archer",
        "supportinfantry": "support",
        "heroinfantry": "commando",
        "flyinginfantry": "flying_infantry",
        "mainbattletank": "mbt",
        "linebreaker": "mbt",
    }
    return exact.get(name)


def load_anchors():
    data = json.loads(ANCHORS_FILE.read_text(encoding="utf-8"))
    return {k: v for k, v in data.items() if isinstance(v, dict) and "spec" in v}


def spread_damages(arm: dict, smallarms_only: bool = False):
    """Effective per-shot damage for pricing = SUM of the weapon's offensive
    SpreadDamage warheads. Thin wrapper over the ONE canonical reducer
    formula.spread_damage_sum (maintainer SUM law 2026-07-22) so the convention
    lives in a single place and MAX can never creep back in."""
    return formula.spread_damage_sum(arm.get("damage_warheads", []), smallarms_only=smallarms_only)


def armament_dps(arm: dict, fp: float, base_only: bool = False, smallarms_only: bool = False, wc: float | None = None):
    rl = fnum(arm.get("reloaddelay"))
    burst = fnum(arm.get("burst")) or 1
    burst_delays = fnum(arm.get("burstdelays")) or 0
    wc = wc if wc is not None else (fnum(arm.get("design_weapon_class")) or 0.75)
    if rl is None or rl <= 0:
        return 0.0
    dmg = spread_damages(arm, smallarms_only=smallarms_only)
    return formula.dps(dmg, rl, wc, int(burst), burst_delays=burst_delays,
                       firepower_multiplier=(1.0 if base_only else fp))


def unit_dps(u: dict, fp: float, base_only: bool = False, smallarms_only: bool = False, wc: float | None = None):
    return sum(armament_dps(arm, fp, base_only=base_only, smallarms_only=smallarms_only, wc=wc)
               for arm in u.get("armaments", [])
               if arm.get("pricing") and arm.get("slot") in ("Armament", "Armament@PRIMARY"))


def resolve_dps_uniqueness(rows, step: float = 0.01) -> None:
    """Tune FirepowerMultiplier so effective DAMAGE-PER-SHOT (Σwarheads × FP)
    is unique across the class — the maintainer 5-stat uniqueness law
    (2026-07-22). Uniqueness keys on damage-per-shot, NOT effective DPS (which
    conflates damage with reload) and NEVER on the FP value itself. Raw
    ReloadDelay is a SEPARATE uniqueness dimension (flagged in the report, never
    auto-nudged — reload is a design choice). dps_eff is still derived here for
    the pricing formula."""
    for i, r in enumerate(rows):
        base_fp = r["fp0"]
        base_dps = r["base_dps"]
        dmg_shot = r.get("dmg_shot0", 0.0)
        if r.get("protected") or dmg_shot < 1.0:
            r["fp"] = base_fp
            r["dmg_eff"] = dmg_shot * base_fp
            r["dps_eff"] = base_dps * base_fp
            continue
        for k in range(0, 400):
            signs = (1, -1) if k > 0 else (1,)
            for sign in signs:
                fp = base_fp + sign * k * step
                if not (0.05 <= fp <= 2.0):
                    continue
                dmg_eff = dmg_shot * fp
                if all(abs(other.get("dmg_eff", -1.0) - dmg_eff) > 0.5 for other in rows[:i]):
                    r["fp"] = fp
                    r["dmg_eff"] = dmg_eff
                    r["dps_eff"] = base_dps * fp
                    break
            else:
                continue
            break
        else:
            raise RuntimeError(f"Could not make effective damage-per-shot unique for {r['actor']}")


def nudge_ranges(rows, lo: int, hi: int):
    for _ in range(100):
        dupes = {}
        for r in rows:
            if r.get("protected"):
                continue
            dupes.setdefault(r["rng"], []).append(r)
        dupes = {k: v for k, v in dupes.items() if len(v) > 1}
        if not dupes:
            break
        for val, group in dupes.items():
            group.sort(key=lambda r: r["actor"])
            for i, r in enumerate(group):
                direction = 1 if i % 2 == 0 else -1
                r["rng"] = max(lo, min(hi, r["rng"] + direction * 10))


def _spd_snap(r, val, lo, hi):
    """Clamp Speed to band and snap to the row's step — a multiple of 5 for
    vehicle-turn-rate units (turn = speed/5), unchanged (step 1) for foot."""
    step = r.get("spd_step", 1)
    val = max(lo, min(hi, val))
    if step > 1:
        val = int(round(val / step)) * step
    return int(max(lo, min(hi, val)))


def nudge_hp_spd(rows, hp_lo=1000, hp_hi=100000, spd_lo=48, spd_hi=72, spd_step=1):
    # HP steps by 1000. Speed steps PER-ROW: 1 for foot infantry, 5 for
    # vehicle-turn-rate units (also snapped to a multiple of 5). maintainer 2026-07-22.
    # --- HP: uniform step 1000 ---
    for l, h, step in ((hp_lo, hp_hi, 1000),):
        groups = {}
        for r in rows:
            groups.setdefault(r["hp"], []).append(r)
        for group in groups.values():
            group = [r for r in group if not r.get("protected")]
            if len(group) <= 1:
                continue
            n = len(group)
            offsets = [i - n // 2 for i in range(n)]
            group.sort(key=lambda r: r["actor"])
            for r, off in zip(group, offsets):
                r["hp"] = max(l, min(h, int(round(r["hp"] + off * step))))
        for _ in range(100):
            dupes = {k: v for k, v in
                     _group_by(rows, "hp").items() if len(v) > 1}
            if not dupes:
                break
            for group in dupes.values():
                group.sort(key=lambda r: r["actor"])
                for i, r in enumerate(group):
                    if r.get("protected"):
                        continue
                    r["hp"] = max(l, min(h, r["hp"] + (1 if i % 2 == 0 else -1) * step))
    # --- Speed: per-row step; snap turn-rate units to a multiple of 5 first ---
    for r in rows:
        if not r.get("protected"):
            r["spd"] = _spd_snap(r, r["spd"], spd_lo, spd_hi)
    for _ in range(300):
        dupes = {k: v for k, v in _group_by(rows, "spd").items() if len(v) > 1}
        if not dupes:
            break
        moved = False
        for group in dupes.values():
            group.sort(key=lambda r: (r.get("protected", False), r["actor"]))
            for i, r in enumerate(group):
                if i == 0 or r.get("protected"):
                    continue  # keep one occupant; move the rest by their own step
                direction = 1 if i % 2 == 1 else -1
                nv = _spd_snap(r, r["spd"] + direction * r.get("spd_step", 1),
                               spd_lo, spd_hi)
                if nv != r["spd"]:
                    r["spd"] = nv
                    moved = True
        if not moved:
            break


def _group_by(rows, key):
    groups = {}
    for r in rows:
        groups.setdefault(r[key], []).append(r)
    return groups


def load_class_rows(cls: str):
    anchors = load_anchors()
    anchor = anchors.get(cls)
    if not anchor:
        raise SystemExit(f"No spec anchor for class {cls}")
    spec = anchor["spec"]
    protected = {anchor.get("anchor_actor"), anchor.get("verifier_actor")}
    protected.discard(None)
    band_lo, band_hi = band_for(cls, spec)
    spd_lo = int(spec["speed0"] * 0.8)
    spd_hi = int(spec["speed0"] * 1.2)
    rows = []
    for path in sorted(LEDGER_DIR.glob("*.json")):
        if path.name == "class_anchors.json":
            continue
        try:
            doc = json.loads(path.read_text(encoding="utf-8-sig"))
        except Exception:
            continue
        if not isinstance(doc, dict):
            continue
        ledger_name = doc.get("ledger") or path.stem
        for section_name, section in doc.get("sections", {}).items():
            if not isinstance(section, dict):
                continue
            for actor, u in section.items():
                if not isinstance(u, dict):
                    continue
                design = u.get("design") or {}
                actor_cls = design.get("class_anchor") or subtype_to_anchor(design.get("subtype"))
                if actor in EXCLUDED_ACTORS or actor_cls != cls:
                    continue
                # Non-buildable units (no Buildable/Queue or ~disabled/~wip) are
                # excluded from balancing entirely (maintainer law 2026-07-22).
                # EXCEPTIONS stay in: anchor/verifier (calibration refs) and units
                # tagged design.balance_include (spawn-together siblings like
                # molotovconscript, or support-power spawns like frank/undead —
                # ~disabled by spawn mechanism, but real balance-relevant units).
                if (not u.get("buildable", True) and actor not in protected
                        and not design.get("balance_include")):
                    continue
                hp = fnum((u.get("hp") or {}).get("v")) or 0
                spd = fnum((u.get("speed") or {}).get("v")) or 0
                cost = fnum((u.get("cost") or {}).get("v")) or 0
                rng = fnum((u.get("range") or {}).get("v")) or 0
                fp_raw = fnum((u.get("firepower_multiplier") or {}).get("v"))
                is_protected = actor in protected
                fp0 = 1.0 if is_protected else (fp_raw if fp_raw is not None else 1.0)
                # Scout low-cost units (<=1.5*C0) are SmallArms-only per FORMULA_V2 §3.
                if cls == "scout" and cost <= spec["cost0"] * 1.5:
                    smallarms_only = True
                    wc = 0.75
                else:
                    smallarms_only = False
                    wc = None
                base_dps = unit_dps(u, fp0, base_only=True, smallarms_only=smallarms_only, wc=wc)
                row = {
                    "actor": actor,
                    "faction": design.get("faction") or ledger_name,
                    "hp": hp,
                    "spd": spd,
                    "rng": rng,
                    "cost": cost,
                    "fp0": fp0,
                    "base_dps": base_dps,
                    "special": fnum(design.get("special")) or 1.0,
                    "tech_tier": fnum(design.get("tech_tier")) or 1.0,
                    "protected": is_protected,
                    "note": "anchor" if actor == anchor.get("anchor_actor") else
                            ("verifier" if actor == anchor.get("verifier_actor") else ""),
                }
                # weapon display: first priced primary armament
                priced = [a for a in u.get("armaments", []) if a.get("pricing") and a.get("slot") in ("Armament", "Armament@PRIMARY")]
                arm = priced[0] if priced else {}
                row["weapon"] = arm.get("weapon", "")
                row["burst"] = fnum(arm.get("burst")) or 1
                row["rl"] = fnum(arm.get("reloaddelay")) or 0
                # Scout low-cost units are SmallArms-only per FORMULA_V2 §3.
                smallarms_only = (cls == "scout" and cost <= spec["cost0"] * 1.5)
                row["dmg"] = int(spread_damages(arm, smallarms_only=smallarms_only))
                # Precise per-shot damage (Σ warheads over ALL priced primary
                # armaments) — the base for the effective-damage-per-shot
                # uniqueness key (Σwarheads × FP), maintainer 5-stat law.
                row["dmg_shot0"] = sum(spread_damages(a, smallarms_only=smallarms_only) for a in priced)
                row["dmg_filter"] = "smallarms" if smallarms_only else "all"
                row["wc"] = 0.750 if smallarms_only else (fnum(arm.get("design_weapon_class")) or 0.75)
                # audit_exempt (soft) units are balanced to Δ≤1 but skipped by the
                # uniqueness enforcement (support-power spawns like frank/undead).
                row["soft"] = bool(design.get("audit_exempt")) and not is_protected
                # Vehicle turn-rate logic (turn = speed/5) → Speed MUST be a
                # multiple of 5. True (foot) infantry turn instantly and may step
                # Speed by 1. Detected by a defined Mobile.TurnSpeed (maintainer
                # 2026-07-22): this covers actual vehicles AND the Cabal cyborgs /
                # FutureTech droids that use vehicle locomotion, while zerglings
                # etc. (chem locomotor but no TurnSpeed) stay foot-stepped.
                row["vehicle_turnrate"] = bool(u.get("turn_speed"))
                row["spd_step"] = 5 if row["vehicle_turnrate"] else 1
                row["arm_rng"] = fnum(arm.get("range")) or 0
                # offensive warheads on the primary weapon (2000-grid split target)
                offensive = [w for w in arm.get("damage_warheads", [])
                             if not str(w.get("tag", "")).lower().endswith(
                                 ("percentage", "extradamage", "friendlyfire"))]
                row["n_wh"] = len(offensive) or 1
                # cross-pack shared weapon → editing its Damage/Range leaks; flag it
                row["weapon_file"] = arm.get("defined_in", "")
                if is_protected:
                    row["rng"] = int(spec["range0_wdist"])
                rows.append(row)
    return rows, spec, band_lo, band_hi, spd_lo, spd_hi


# Classes whose Speed steps by 5 (vehicles/aircraft/ships: turn rate = speed/5).
# Infantry classes step by 1. Extend as vehicle/aircraft/naval anchors land.
VEHICLE_TYPE_CLASSES = {"mbt"}


def _price(spec, hp, spd, rng, dps, special, tech_tier):
    return formula.class_baseline_price(
        hp, spd, rng, dps, spec["hp0"], spec["speed0"], spec["range0_wdist"],
        spec["dps0"], spec["cost0"], special=special, tech_tier=tech_tier)


def solve_target_dps(spec, cost, hp, spd, rng, special, tech_tier):
    """Effective DPS that makes price == cost at (hp, spd, rng). Returns None
    when HP/range alone already over-price the unit (price(0) > cost) — DPS
    can't close it and another stat must give."""
    if _price(spec, hp, spd, rng, 0.0, special, tech_tier) > cost + 1:
        return None
    lo, hi = 0.0, 1.0
    while _price(spec, hp, spd, rng, hi, special, tech_tier) < cost and hi < 1e7:
        hi *= 2
    for _ in range(80):
        mid = (lo + hi) / 2
        if _price(spec, hp, spd, rng, mid, special, tech_tier) > cost:
            hi = mid
        else:
            lo = mid
    return (lo + hi) / 2


def solve_target_speed(spec, cost, hp, rng, dps, special, tech_tier, lo, hi):
    """Speed giving price == cost at fixed (hp, rng, dps). Price rises with
    speed, so bisect; clamp to [lo, hi]. Used as a fine (±1) Δ lever on foot
    infantry after DPS+range are set."""
    def pr(s):
        return _price(spec, hp, s, rng, dps, special, tech_tier)
    if pr(lo) >= cost:
        return lo
    if pr(hi) <= cost:
        return hi
    a, b = lo, hi
    for _ in range(60):
        mid = (a + b) / 2
        if pr(mid) > cost:
            b = mid
        else:
            a = mid
    return (a + b) / 2


def decompose_dps(target_dps, base_dps, cur_sum, n_wh):
    """Two-stage: coarse per-warhead Damage on the 2000-grid + FirepowerMultiplier
    (1% steps) reproducing target_dps. base_dps = eff-DPS at fp=1 with cur_sum SUM
    (linear in SUM). Returns (per_warhead_damage, fp)."""
    if base_dps <= 0 or cur_sum <= 0 or target_dps <= 0:
        return 2000, 0.05
    per_unit = base_dps / cur_sum               # eff-DPS per unit of SUM at fp=1
    needed = target_dps / per_unit              # required SUM × FP
    D = max(2000, int(round(needed / n_wh / 2000)) * 2000)
    fp = needed / (D * n_wh)
    fp = max(0.05, min(2.0, round(fp, 2)))
    return D, fp


def unique_dmg_per_shot(rows, step=0.01):
    """Nudge FirepowerMultiplier (1% steps) so effective damage-per-shot
    (Σwarheads × FP) is unique across non-protected, non-soft members. Small FP
    nudges shift Δ only marginally (reported)."""
    placed = []
    def collides(de):
        return any(abs(o["dmg_eff"] - de) <= 0.5 for o in placed
                   if not o["protected"] and not o.get("soft"))
    for r in rows:
        if r["protected"] or r.get("soft"):
            placed.append(r)
            continue
        sumv = r["dmg_shot"]
        base_fp = r["fp"]
        for k in range(0, 200):
            for sign in ((1, -1) if k > 0 else (1,)):
                fp = round(base_fp + sign * k * step, 2)
                if not (0.05 <= fp <= 2.0):
                    continue
                if not collides(sumv * fp):
                    r["fp"] = fp
                    r["dmg_eff"] = sumv * fp
                    r["dps_eff"] = r["per_unit"] * sumv * fp
                    placed.append(r)
                    break
            else:
                continue
            break
        else:
            placed.append(r)


def rebalance_class(cls: str):
    rows, spec, band_lo, band_hi, spd_lo, spd_hi = load_class_rows(cls)
    if not rows:
        return ""
    spd_step = 5 if cls in VEHICLE_TYPE_CLASSES else 1
    nudge_hp_spd(rows, spd_lo=spd_lo, spd_hi=spd_hi, spd_step=spd_step)
    # 1. Range: protected → spec; others → clamp current into band (preserve feel).
    for r in rows:
        if r["protected"]:
            continue
        cur = r["rng"] or r["arm_rng"] or (band_lo + band_hi) // 2
        r["rng"] = max(band_lo, min(band_hi, int(round(cur / 10)) * 10))
    nudge_ranges(rows, band_lo, band_hi)     # range uniqueness (skips protected)
    # 2. Per member: solve target eff-DPS for Δ0 at final (hp,spd,rng); decompose
    #    to 2000-grid warhead Damage + 1% FP (cost pinned, stats trimmed law).
    for r in rows:
        r["per_unit"] = (r["base_dps"] / r["dmg_shot0"]) if r["dmg_shot0"] else 0.0
        if r["protected"]:
            r["fp"] = r["fp0"]
            r["dmg_shot"] = r["dmg_shot0"]
            r["dmg_eff"] = r["dmg_shot0"] * r["fp0"]
            r["dps_eff"] = r["base_dps"] * r["fp0"]
            r["per_wh"] = None
            r["trimmed"] = False
            continue
        tgt = solve_target_dps(spec, r["cost"], r["hp"], r["spd"], r["rng"],
                               r["special"], r["tech_tier"])
        if tgt is None:
            r["per_wh"], r["fp"] = 2000, 0.05
            r["over_priced"] = True
            tgt = 0.0
        else:
            r["over_priced"] = False
        D, fp = decompose_dps(tgt, r["base_dps"], r["dmg_shot0"] or 1, r["n_wh"])
        if r.get("over_priced"):
            D, fp = 2000, 0.05
        r["per_wh"] = D
        r["fp"] = fp
        r["dmg_shot"] = D * r["n_wh"]
        r["dmg_eff"] = r["dmg_shot"] * fp
        r["dps_eff"] = r["per_unit"] * r["dmg_shot"] * fp
        r["trimmed"] = (r["dmg_shot"] != (r["dmg_shot0"] or r["dmg_shot"]))
    # 2b. Range fine-tune: range is a finer (10-step) Δ lever than the 1% FP.
    #     Snap each member to the range that zeroes Δ at its trimmed DPS when
    #     that lands in band; the DPS-trim already handled out-of-band cases.
    for r in rows:
        if r["protected"] or r.get("over_priced"):
            continue
        r0 = formula.solve_class_baseline_range(
            r["cost"], r["hp"], r["spd"], r["dps_eff"],
            spec["hp0"], spec["speed0"], spec["range0_wdist"], spec["dps0"],
            spec["cost0"], special=r["special"], tech_tier=r["tech_tier"])
        r0 = int(round(r0 / 10)) * 10
        if band_lo <= r0 <= band_hi:
            r["rng"] = r0
    nudge_ranges(rows, band_lo, band_hi)   # re-unique after snapping
    # 2c. Speed fine-tune (FOOT infantry only, step 1): squeeze any member still
    #     off Δ0 — the maintainer allows Speed±1 on foot infantry as a fine price
    #     lever. Turn-rate units keep multiples of 5 (too coarse) and are left.
    taken_spd = {r["spd"] for r in rows}
    for r in rows:
        if (r["protected"] or r.get("soft") or r.get("over_priced")
                or r.get("spd_step", 1) != 1):
            continue
        d = _price(spec, r["hp"], r["spd"], r["rng"], r["dps_eff"],
                   r["special"], r["tech_tier"]) - r["cost"]
        if abs(d) <= 1:
            continue
        ideal = int(round(solve_target_speed(
            spec, r["cost"], r["hp"], r["rng"], r["dps_eff"],
            r["special"], r["tech_tier"], spd_lo, spd_hi)))
        # pick nearest unused speed to ideal (keep speed uniqueness)
        taken_spd.discard(r["spd"])
        for cand in sorted(range(spd_lo, spd_hi + 1), key=lambda s: (abs(s - ideal), s)):
            if cand not in taken_spd:
                r["spd"] = cand
                break
        taken_spd.add(r["spd"])
    # 3. Effective-damage-per-shot uniqueness (skips protected + soft)
    unique_dmg_per_shot(rows)
    # 4. Prices / deltas
    for r in rows:
        r["price"] = _price(spec, r["hp"], r["spd"], r["rng"], r["dps_eff"],
                            r["special"], r["tech_tier"])
        r["delta"] = r["price"] - r["cost"]
    return render_report(rows, cls)


def render_report(rows, cls):
    s = load_anchors()[cls]["spec"]
    lines = [
        f"# {cls.replace('_', ' ').title()} infantry rebalance proposal",
        "",
        f"Anchor spec: HP={s['hp0']}, Speed={s['speed0']}, Range={s['range0_wdist']}, eff-DPS={s['dps0']}, Cost={s['cost0']}",
        "",
        "Converter law: cost pinned, range clamped to band + made unique, "
        "eff-DPS trimmed to Δ≤1 via 2000-grid warhead Damage × 1% FirepowerMultiplier.",
        "",
        "| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | FP% | eff DPS | price | Δ | flags |",
        "|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|",
    ]
    worst = 0.0
    for r in rows:
        flags = r["note"]
        if r.get("soft"):
            flags = (flags + " soft").strip()
        if r.get("over_priced"):
            flags = (flags + " OVERPRICED@min-dps").strip()
        if "/" in str(r.get("weapon_file", "")) and "weapons/" in str(r.get("weapon_file", "")):
            flags = (flags + " shared-wpn?").strip()
        if not r["protected"]:
            worst = max(worst, abs(r["delta"]))
        dcol = f"{r.get('per_wh') or r['dmg']}×{r.get('n_wh', 1)}"
        lines.append(
            f"| `{r['actor']}` | {r['faction']} | {r['hp']} | {r['spd']} | {r['rng']} | "
            f"{r['cost']} | {dcol} | {r['rl']} | {r['burst']} | {int(round(r['fp']*100))} | "
            f"{r['dps_eff']:.1f} | {r['price']:.0f} | {r['delta']:+.1f} | {flags} |"
        )
    lines += ["", f"**Worst |Δ| among non-anchor members: {worst:.1f}** "
              f"(goal ≤1).", "",
              "## Uniqueness check (5 raw stats — soft/protected excluded)", ""]
    ok = True
    for key, label in (("hp", "HP"), ("spd", "Speed"), ("rng", "Range"), ("rl", "raw ReloadDelay")):
        dupes = {}
        for r in rows:
            if r["protected"] or r.get("soft"):
                continue
            dupes.setdefault(r[key], []).append(r["actor"])
        dupes = {k: v for k, v in dupes.items() if len(v) > 1}
        if dupes:
            ok = False
            note = " (design choice — retune coarse Damage/reload by hand)" if key == "rl" else ""
            lines.append(f"- **{label} duplicates**{note}: {dupes}")
    dmg_dupes = {}
    for r in rows:
        if r["protected"] or r.get("soft"):
            continue
        dmg_dupes.setdefault(round(r.get("dmg_eff", 0.0), 1), []).append(r["actor"])
    dmg_dupes = {k: v for k, v in dmg_dupes.items() if len(v) > 1}
    if dmg_dupes:
        ok = False
        lines.append(f"- **effective damage-per-shot duplicates**: {dmg_dupes}")
    if ok:
        lines.append("- All 5-stat uniqueness checks passed "
                     "(HP, Speed, Range, raw ReloadDelay, effective damage-per-shot).")
    lines += ["", "## Required YAML edits (per unit)", ""]
    for r in rows:
        if r["protected"]:
            continue
        trait = r["actor"].upper().replace("_", "")
        changes = [
            f"HP {r['hp']}, Speed {r['spd']}, Range {r['rng']}",
            f"each offensive warhead Damage {r.get('per_wh')} (×{r.get('n_wh',1)} = "
            f"SUM {r.get('dmg_shot')}), ReloadDelay {r['rl']}, Burst {r['burst']}",
            f"FirepowerMultiplier@{trait} {int(round(r['fp']*100))}",
        ]
        if abs(r["delta"]) > 1:
            changes.append(f"residual Δ {r['delta']:+.1f} (cost pinned at {r['cost']})")
        lines.append(f"- `{r['actor']}`: {', '.join(changes)}")
    return "\n".join(lines) + "\n"


def main():
    valid = sorted(load_anchors().keys())
    ap = argparse.ArgumentParser(
        description="Per-class rebalance proposal. Class must exist in "
                    "class_anchors.json with a spec. Members are units tagged "
                    "design.class_anchor==<class> (or a mapped subtype).")
    ap.add_argument("--class", "-c", dest="cls", required=True, choices=valid,
                    metavar="CLASS", help="one of: " + ", ".join(valid))
    args = ap.parse_args()
    text = rebalance_class(args.cls)
    if not text:
        print(f"no units found for class {args.cls} "
              f"(tag members with design.class_anchor=={args.cls} first)")
        return
    out = ROOT / "docs" / "balance" / f"proposal_{args.cls}_infantry.md"
    out.write_text(text, encoding="utf-8")
    print(f"wrote {out.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
