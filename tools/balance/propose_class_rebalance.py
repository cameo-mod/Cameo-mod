#!/usr/bin/env python3
"""propose_class_rebalance.py — per-class infantry rebalance proposal.

Reads the ledger JSONs, selects all units belonging to a given class
(subtype or explicit class_anchor), keeps their current HP/Speed/Cost,
resolves effective-DPS uniqueness on the Damage grid,
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
import tier_chain  # noqa: E402
import class_membership  # noqa: E402

LEDGER_DIR = ROOT / "docs/balance"
ANCHORS_FILE = LEDGER_DIR / "class_anchors.json"

UNIQUENESS_KEYS = {
    "damage": "raw warhead Damage (the 5-stat law as written)",
    "dps": "effective DPS (measurement only — needs a maintainer ruling)",
}
"""What the 5-stat uniqueness law separates, and why there is a second option.

⚠ **`damage` is the law and stays the default.** `dps` writes nothing on its own;
it exists so the cost of the law can be MEASURED instead of argued about.

THE MEASUREMENT (2026-08-29, class `scout`, 22 movable members). With the levers
fixed and ordered correctly, worst |Δ| lands at:

    uniqueness on raw Damage       22.8 credits
    uniqueness on effective DPS     0.7 credits   <- meets the <=1 goal

The whole 22.8 is one effect. Damage moves on the 100 grid, and after the DPS
solve the members' ideal slots COLLIDE: **four** of them want Damage 800
(`td_nod_minigunner`, `ra1_allies_rifleinfantry`, `ra1_soviets_rifleinfantry`,
`latinsyndicate_latinmilitia`). Three must move, and at Damage 800 one step is
12.5% of the unit's whole DPS — far more than the range band (+-1000 on 5000,
i.e. +-3.3% of cost) can absorb. Under DPS-uniqueness those four collide on
NOTHING: their eff-DPS at Damage 800 is 57.1 / 41.4 / 40.0 / 80.0, because their
Burst and ReloadDelay already differ. They are separated by every quantity a
player can observe; only the literal number in the warhead node is shared — and
some of them SHARE THAT WEAPON anyway, so it is not even a per-actor value.

That is the case for the ruling, not the ruling itself. `docs/HANDOFF.md` carries
it as an open item; until it is answered, the default writes Damage-uniqueness.
"""

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


def subtype_to_anchor(st):
    """DELEGATES to `tools/balance/class_membership.py`, the single map.

    ⛔ There were THREE copies of this function and they disagreed: this one and
    `update_ranges.py` knew 5 subtypes, `propose_class_rebalance.py` knew 17, and all three said
    `linebreaker -> mbt` when `line_breaker` is its own class with 30 of 31 members tagged that
    way — so 40 line-breakers were being folded into the MBT population. Kept as a name so the
    call sites do not all have to change at once.
    """
    return class_membership.subtype_to_anchor(st)


def load_anchors():
    data = json.loads(ANCHORS_FILE.read_text(encoding="utf-8"))
    return {k: v for k, v in data.items() if isinstance(v, dict) and "spec" in v}


def spread_damages(arm: dict, smallarms_only: bool = False):
    """Effective per-shot damage for pricing = SUM of the weapon's offensive
    SpreadDamage warheads. Thin wrapper over the ONE canonical reducer
    formula.spread_damage_sum (maintainer SUM law 2026-07-22) so the convention
    lives in a single place and MAX can never creep back in."""
    return formula.spread_damage_sum(arm.get("damage_warheads", []), smallarms_only=smallarms_only)


def armament_dps(arm: dict, fp: float, base_only: bool = False, smallarms_only: bool = False):
    rl = fnum(arm.get("reloaddelay"))
    burst = fnum(arm.get("burst")) or 1
    burst_delays = arm.get("burstdelays")
    if rl is None or rl <= 0:
        return 0.0
    dmg = spread_damages(arm, smallarms_only=smallarms_only)
    # No weapon-class weight (W4): the K coefficient measures weapon quality
    # directly, so the tier weight would double-charge it.
    return formula.dps(dmg, rl, int(burst), burst_delays=burst_delays,
                       firepower_multiplier=(1.0 if base_only else fp))


def unit_dps(u: dict, fp: float, base_only: bool = False, smallarms_only: bool = False):
    return sum(armament_dps(arm, fp, base_only=base_only, smallarms_only=smallarms_only)
               for arm in u.get("armaments", [])
               if arm.get("pricing") and arm.get("slot") in ("Armament", "Armament@PRIMARY"))


def resolve_dps_uniqueness(rows, step: float = 0.01) -> None:
    """⚠ DEAD + SUPERSEDED — tunes a knob that W17 retired. Kept, not deleted,
    only because it is referenced by name in ROADMAP.md and LESSONS_LEARNED.md.

    Nothing in this module calls it. Its one apparent caller,
    `_balance_audit_report.py`, reached for `resolve_dps_uniqueness` on the
    `*_rebalance_proposal_final` modules, which no longer exist — so that script
    could not import, and it was deleted on 2026-08-28 (recover with
    `git show 6e0a273b:tools/balance/_balance_audit_report.py`; its last output is
    archived at `docs/history/balance/BALANCE_AUDIT.md`). The live uniqueness pass
    is `unique_dmg_per_shot`, which nudges Damage on the grid. Do not revive this
    one without W17 in hand.

    Tune FirepowerMultiplier so effective DAMAGE-PER-SHOT (Σwarheads × FP)
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


def nudge_hp_spd(rows, hp_lo=1000, hp_hi=100000, spd_lo=48, spd_hi=72):
    """Make HP and Speed unique, moving each row on ITS OWN legal grid.

    ⚠ Both grids are per-ROW and come from `formula.STAT_GRIDS` — infantry HP
    steps by 1000 and Speed by 1, vehicles/aircraft/ships by 2500 and 5. HP used
    to be hardcoded at 1000 for every class, so every vehicle class was nudged
    onto the infantry grid. There was also a class-level `spd_step` argument, and
    a `VEHICLE_TYPE_CLASSES = {"mbt"}` set feeding it, that NOTHING READ: the
    per-row step always won. Both are gone — a dead knob that looks like it
    enforces a law is worse than no knob at all.
    """
    # ⚠ SNAP FIRST, then de-duplicate. This pass used to only STEP HP by the grid
    # when breaking a tie, so a value that was never tied kept whatever off-grid
    # number it had — 7 of the 28 scout vehicles sat on 22500/27500/37500 against
    # a 1000 grid and the converter left them there. Speed has always snapped
    # (`_spd_snap`); HP never did.
    for r in rows:
        if r.get("protected"):
            continue
        step = r.get("hp_step", 1000)
        r["hp"] = max(hp_lo, min(hp_hi, int(round(r["hp"] / step)) * step))
    for l, h in ((hp_lo, hp_hi),):
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
                step = r.get("hp_step", 1000)
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
                    step = r.get("hp_step", 1000)
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
    # ⚠ THERE IS NO VERIFIER ANY MORE (maintainer ruling 2026-08-29):
    # *"we no longer have to have those verifiers — they should be regular units
    # like anything else and not have those stiff rules."*
    #
    # Only the ANCHOR is protected. It defines cost0, which is what makes the
    # class formula a formula. A second actor used to be frozen alongside it as a
    # 2.5x cost0 calibration point (`BALANCE_PIPELINE.md` §8.1). Three
    # measurements retired it:
    #   * it did not constrain the class — releasing it moved the other members'
    #     worst |Δ| by 0.0 in 17 of 23 classes, and IMPROVED 5;
    #   * it was not where the law put it — only 8 of 23 sat at 2.5x cost0, and
    #     three were CHEAPER than their own baseline, which makes them a second
    #     baseline, not a ceiling;
    #   * it was not verifying anything — its own Δ reached -3779.9
    #     (`dreadnought`), and because `protected` rows are excluded from the
    #     "worst |Δ| among non-anchor members" line, a verifier that far out was
    #     INVISIBLE in the very report that exists to catch bad pricing. Freezing
    #     it did not merely fail to help; it HID the failure.
    # The 2.5x baseband law is unaffected — `check_band.py` enforces it on price
    # RATIOS, which never needed a nominated actor.
    protected = {anchor.get("anchor_actor")}
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
        dfile = LEDGER_DIR / "derived" / path.name
        try:
            ddoc = json.loads(dfile.read_text(encoding="utf-8-sig")) if dfile.is_file() else {}
        except Exception:
            ddoc = {}
        dsec = ddoc.get("sections") or {}
        for section_name, section in doc.get("sections", {}).items():
            if not isinstance(section, dict):
                continue
            for actor, u in section.items():
                if not isinstance(u, dict):
                    continue
                design = u.get("design") or {}
                du = (dsec.get(section_name) or {}).get(actor) or {}
                actor_cls = design.get("class_anchor") or subtype_to_anchor(design.get("subtype"))
                if actor in EXCLUDED_ACTORS or actor_cls != cls:
                    continue
                # Non-buildable units (no Buildable/Queue or ~disabled/~wip) are
                # excluded from balancing entirely (maintainer law 2026-07-22).
                # EXCEPTION stays in: the anchor (the class's calibration ref) and units
                # tagged design.balance_include (spawn-together siblings like
                # molotovconscript, or support-power spawns like frank/undead —
                # ~disabled by spawn mechanism, but real balance-relevant units).
                if (not u.get("buildable", True) and actor not in protected
                        and not design.get("balance_include")):
                    continue
                hp = fnum((u.get("hp") or {}).get("v")) or 0
                spd = fnum((u.get("speed") or {}).get("v")) or 0
                cost = fnum((u.get("cost") or {}).get("v")) or 0
                rng = formula.wdist_value((u.get("range") or {}).get("v"), 0)
                fp_raw = fnum((u.get("firepower_multiplier") or {}).get("v"))
                is_protected = actor in protected
                fp0 = 1.0 if is_protected else (fp_raw if fp_raw is not None else 1.0)
                # Scout low-cost units (<=1.5*C0) are SmallArms-only per FORMULA_V2 §3.
                smallarms_only = cls == "scout" and cost <= spec["cost0"] * 1.5
                base_dps = unit_dps(u, fp0, base_only=True, smallarms_only=smallarms_only)
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
                    "tier_abs": tier_chain.effective_tier(
                        design.get("tech_tier"), du.get("tier_multiplier"), default=1.0),
                    "protected": is_protected,
                    "note": "anchor" if actor == anchor.get("anchor_actor") else "",
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
                # Either trait: `Mobile.TurnSpeed` for ground, `Aircraft.TurnSpeed`
                # for air. Reading only the first made every aircraft look static.
                turn = u.get("turn_speed") or u.get("turn_speed_air")
                row["vehicle_turnrate"] = bool(turn)
                # ⚠ The two grids key off DIFFERENT things — Speed off locomotion
                # (turn rate = speed/5), HP off the unit kind (self-heal = HP/2500
                # or HP/1000). A droid drives like a vehicle and heals like
                # infantry, and takes one grid from each. See formula.STAT_GRIDS.
                row["speed_platform"] = formula.speed_platform(section_name, turn)
                row["hp_platform"] = formula.hp_platform(section_name, actor_cls)
                row["spd_step"] = formula.stat_step("speed", row["speed_platform"])
                row["hp_step"] = formula.stat_step("hp", row["hp_platform"])
                row["arm_rng"] = formula.wdist_value(arm.get("range"), 0)
                # offensive warheads on the primary weapon (100-grid split target)
                offensive = [w for w in arm.get("damage_warheads", [])
                             if not str(w.get("tag", "")).lower().endswith(
                                 ("percentage", "extradamage", "friendlyfire"))]
                row["n_wh"] = len(offensive) or 1
                # cross-pack shared weapon → editing its Damage/Range leaks; flag it
                row["weapon_file"] = arm.get("defined_in", "")
                if is_protected:
                    row["rng"] = int(spec["range0_wdist"])
                rows.append(row)
    # Convert absolute tier multipliers to RELATIVE tier multipliers
    # f(C_unit) / f(C_anchor).  class_baseline_price is anchored at the
    # anchor's absolute price, so it must receive a relative multiplier.
    anchor_tech = fnum(anchor.get("tech_tier")) or 1.0
    anchor_row = next((r for r in rows if r["actor"] == anchor.get("anchor_actor")), None)
    if anchor_row:
        anchor_tech = anchor_row["tier_abs"]
    if not anchor_tech:
        anchor_tech = 1.0
    for r in rows:
        r["tech_tier"] = r["tier_abs"] / anchor_tech
    return rows, spec, band_lo, band_hi, spd_lo, spd_hi


# Classes whose Speed steps by 5 (vehicles/aircraft/ships: turn rate = speed/5).
# Infantry classes step by 1. Extend as vehicle/aircraft/naval anchors land.
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
    """Per-warhead `Damage` on the flat grid reproducing target_dps, in ONE stage.

    **W17 — `FirepowerMultiplier` is retired as a fine-tuning knob**, so the second
    element of the return is always 1.0. It used to be a solved residual: the old
    code snapped D to the 2000 grid and handed whatever was left to a 1%-step FP,
    clamped to [0.05, 2.0]. The grid is now `formula.DAMAGE_STEP` (100), 20x finer,
    so the residual it leaves is half a step — measured across every FP-carrying
    actor in the roster, 87% of main warheads land EXACTLY on the grid and 92%
    within 1%. That is below the noise the knob existed to absorb.

    The floor is deliberately unchanged: the old dead-end returned `2000, 0.05`
    = 100 effective damage per warhead, and one grid step at fp=1 is also 100.

    ⚠ The prescribed Damage assumes **no FirepowerMultiplier on the actor**
    (`base_dps` is measured with `base_only=True`, i.e. at fp=1). An actor that
    still carries a legacy unconditional FP must have that trait DELETED when the
    new Damage lands, or the multiplier applies a second time. `render_report`
    emits that instruction; `plan_firepower_retirement.py` lists who needs it.

    Returns (per_warhead_damage, 1.0). base_dps = eff-DPS at fp=1 with cur_sum
    SUM (linear in SUM).
    """
    step = formula.DAMAGE_STEP
    if base_dps <= 0 or cur_sum <= 0 or target_dps <= 0:
        return step, 1.0
    per_unit = base_dps / cur_sum               # eff-DPS per unit of SUM at fp=1
    needed = target_dps / per_unit              # required SUM
    return formula.snap_damage_step(needed / n_wh), 1.0


class DamageGridAssignment:
    """Lazy slot-assignment helper for the effective-damage-per-shot uniqueness law.

    THE PROBLEM IT SOLVES. Every non-protected member of a class must end with a
    DISTINCT effective damage-per-shot, and damage only moves on the 100 grid
    (`formula.DAMAGE_STEP`). When two members want the same slot one has to move,
    and one step is a whole shot — so the choice of WHO moves, and WHERE, is a
    pricing decision, not a bookkeeping one.

    It used to be neither: the rows were walked in ledger order and each took the
    first free slot beside its ideal. That is greedy first-fit on a shared grid,
    so an early row could take the slot a later one needed and shove it several
    steps out. Measured on `scout`, worst |Δ| was **15.6 before the uniqueness
    pass and 66.5 after it** — the pass, not the pricing, was the dominant error.
    `forgotten_mutant` was displaced 500 -> 200 and `td_nod_minigunner` 700 ->
    1200 purely because of who came first in the file.

    WHY AN EXACT ANSWER IS AVAILABLE. `class_baseline_price` is LINEAR in DPS and
    DPS is linear in Damage, so each row's |Δ| is a **V** in the slot it takes —
    convex, with its minimum at the ideal the DPS solve already produced. With
    convex costs on a shared 1-D grid an optimal assignment never crosses: if
    row A wants a lower slot than row B, some optimum gives A the lower slot.
    So sorting rows by ideal and walking slots in order loses nothing, and a DP
    over (row, slot) is exact rather than heuristic.

    It runs twice, because the goal is a MINIMAX one and lexicographic tuples do
    not compose through a DP ((5,100) beats (6,1) until both take a 10 and the
    order flips). Pass 1 minimises the worst |Δ| — `max` composes monotonically,
    so that DP is exact. Pass 2 minimises the TOTAL |Δ| with every slot whose cost
    exceeds that worst forbidden, which cannot spoil the worst it inherits.

    LAZY, hence the name: slots and per-(row, slot) deltas are built on demand and
    memoised. The candidate grid is O(rows^2) wide and each delta is a full price
    evaluation, but the DP only ever asks for the band it actually walks, and a
    class whose members never collide never pays for a single one.

    Falls back to `None` from `solve()` when the members do not share one grid —
    effective damage is `per_warhead x n_warheads`, so rows with different warhead
    counts live on grids of different pitch and cannot be interleaved by one DP.
    The caller then uses the greedy path.
    """

    INF = float("inf")

    def __init__(self, rows, step, price_of, reach=None, blocked=()):
        self.rows = list(rows)
        self.step = step
        self.price_of = price_of
        # Slots held by rows this pass may not move (the anchor, soft spawns).
        # They are unavailable, not invisible.
        self.blocked = {float(b) for b in blocked}
        # How far a row may be displaced. One slot per row is always enough to
        # break every tie, so `len(rows)` steps either way can never bind.
        self.reach = len(self.rows) if reach is None else reach
        self._slots = None
        self._delta = {}

    # -- lazy pieces ----------------------------------------------------------
    @property
    def pitch(self):
        """The shared grid pitch, or None when the rows do not share one."""
        counts = {(r.get("n_wh", 1) or 1) for r in self.rows}
        return self.step * counts.pop() if len(counts) == 1 else None

    def ideal(self, i):
        """The effective damage that would price row i at EXACTLY its cost.

        Solved, not searched, and deliberately NOT read off the row's current
        `per_wh`: price is affine in effective damage (`class_baseline_price` is
        linear in DPS, DPS is linear in Damage), so two evaluations give the line
        and the zero of `price − cost` follows. Taking the row's current value
        instead makes this helper's row ORDER depend on the previous pass's
        output, so re-running the trio walks the ideals away from the prices —
        which is exactly the drift that showed up as `scout` settling at 51.0
        instead of 21.8.
        """
        r = self.rows[i]
        pitch = self.pitch or self.step
        a = self.price_of(r, 0.0)
        b = (self.price_of(r, pitch) - a) / pitch
        if abs(b) < 1e-12:
            return max(pitch, (r.get("per_wh") or self.step) * (r.get("n_wh", 1) or 1))
        want = (r["cost"] - a) / b
        return max(pitch, int(round(want / pitch)) * pitch)

    @property
    def slots(self):
        """Every effective-damage value any row could sensibly take, ascending."""
        if self._slots is None:
            pitch = self.pitch
            out = set()
            for i in range(len(self.rows)):
                base = self.ideal(i)
                for k in range(-self.reach, self.reach + 1):
                    v = base + k * pitch
                    if v >= pitch and not any(abs(v - b) <= 0.5 for b in self.blocked):
                        out.add(v)
            self._slots = sorted(out)
        return self._slots

    def delta(self, i, slot):
        """|price − cost| for row i at this effective damage. Memoised."""
        key = (i, slot)
        hit = self._delta.get(key)
        if hit is None:
            r = self.rows[i]
            hit = abs(self.price_of(r, slot) - r["cost"])
            self._delta[key] = hit
        return hit

    # -- the solver -----------------------------------------------------------
    def _walk(self, order, cap):
        """Order-preserving DP. `cap` None => minimise the WORST delta; a number
        => minimise the TOTAL, refusing any slot costing more than `cap`."""
        slots = self.slots
        n, m = len(order), len(slots)
        if n > m:
            return None, None
        best = [[self.INF] * (m + 1) for _ in range(n + 1)]
        take = [[False] * (m + 1) for _ in range(n + 1)]
        for j in range(m + 1):
            best[0][j] = 0.0
        for i in range(1, n + 1):
            for j in range(i, m + 1):
                skip = best[i][j - 1]
                d = self.delta(order[i - 1], slots[j - 1])
                prev = best[i - 1][j - 1]
                if cap is None:
                    use = max(prev, d) if prev < self.INF else self.INF
                else:
                    use = prev + d if (prev < self.INF and d <= cap + 1e-9) else self.INF
                if use < skip - 1e-12:
                    best[i][j], take[i][j] = use, True
                else:
                    best[i][j] = skip
        if best[n][m] == self.INF:
            return None, None
        chosen, i, j = {}, n, m
        while i > 0:
            if take[i][j]:
                chosen[order[i - 1]] = slots[j - 1]
                i -= 1
            j -= 1
        return best[n][m], chosen

    def solve(self):
        """{row index: effective damage} minimising (worst |Δ|, then total |Δ|)."""
        if not self.rows:
            return {}
        if self.pitch is None:
            return None
        order = sorted(range(len(self.rows)), key=lambda i: (self.ideal(i), i))
        worst, chosen = self._walk(order, cap=None)
        if chosen is None:
            return None
        _, tightened = self._walk(order, cap=worst)
        return tightened or chosen


def unique_dmg_per_shot(rows, step=None, price_of=None, key="damage"):
    """Nudge per-warhead `Damage` in GRID steps so effective damage-per-shot is
    unique across non-protected, non-soft members.

    **W17:** this used to walk `FirepowerMultiplier` in 1% steps. With the knob
    retired, Damage itself is the only lever — and the 100 grid is fine enough to
    be one, since the collision test needs just 0.5 of separation while one step
    moves a whole shot by `100 x n_warheads`. Damage never nudges below one step.

    ⚠ **The tie-break is PRICE-AWARE, and it has to be.** Who moves, and where,
    decides how far a member ends from its own price. `DamageGridAssignment`
    (above) makes that choice optimally and explains the measurement that forced
    it; this function is the thin wrapper that applies the plan.

    `price_of(row, dmg_shot) -> price` supplies the objective. Without it — a
    caller that only wants uniqueness — the old nearest-free-slot walk still runs,
    and it is also the fallback when the members do not share one damage grid.
    """
    step = formula.DAMAGE_STEP if step is None else step

    def delta(r, dmg_shot):
        if price_of is None:
            return 0.0
        return abs(price_of(r, dmg_shot) - r["cost"])

    def commit(r, per_wh):
        n_wh = r.get("n_wh", 1) or 1
        r["per_wh"] = per_wh
        r["dmg_shot"] = per_wh * n_wh
        r["dmg_eff"] = r["dmg_shot"]
        r["dps_eff"] = r["per_unit"] * r["dmg_shot"]

    movable = [r for r in rows if not (r["protected"] or r.get("soft"))]
    # ⚠ A ROW THAT CANNOT MOVE STILL OCCUPIES ITS SLOT (maintainer ruling
    # 2026-08-30: *"give each of the scouts their own unique damage numbers"*).
    # Protected and soft rows used to be filtered out of the collision set
    # entirely, so a movable member could be assigned the damage the ANCHOR
    # already had — `naxis_naxiriflerecruit` and `naxis_naxiriflesoldier` both sat
    # on 4000, the only collision left in the class, precisely because the second
    # is the anchor. Not moving a row and not seeing it are different things.
    frozen = [r["dmg_eff"] for r in rows
              if (r["protected"] or r.get("soft")) and r.get("dmg_eff")]
    if not movable:
        return

    if key == "dps":
        # MEASUREMENT MODE (see UNIQUENESS_KEYS). Uniqueness on effective DPS
        # instead of the raw Damage field. Every row starts at its price-ideal
        # and only an actual DPS collision moves anyone, so the pricing residual
        # is whatever the 100 grid leaves and nothing more.
        helper = DamageGridAssignment(movable, step, price_of)
        taken_dps = [r["dps_eff"] for r in rows
                     if (r["protected"] or r.get("soft")) and r.get("dps_eff")]
        for i, r in enumerate(movable):
            n_wh = r.get("n_wh", 1) or 1
            base = helper.ideal(i) if price_of is not None else (r.get("per_wh") or step) * n_wh
            for k in range(0, 200):
                for sign in ((1, -1) if k > 0 else (1,)):
                    dmg_shot = base + sign * k * step * n_wh
                    if dmg_shot < step * n_wh:
                        continue
                    dps = r["per_unit"] * dmg_shot
                    if any(abs(o - dps) <= 0.5 for o in taken_dps):
                        continue
                    commit(r, dmg_shot // n_wh)
                    taken_dps.append(dps)
                    break
                else:
                    continue
                break
        return

    if price_of is not None:
        plan = DamageGridAssignment(movable, step, price_of, blocked=frozen).solve()
        if plan is not None:
            for i, dmg_shot in plan.items():
                commit(movable[i], dmg_shot // (movable[i].get("n_wh", 1) or 1))
            return

    # Fallback: nearest free slot, ledger order. Used when no objective was given
    # (uniqueness-only callers) or when the rows do not share one damage grid.
    taken = set(frozen)

    def collides(de):
        return any(abs(o - de) <= 0.5 for o in taken)

    for r in movable:
        n_wh = r.get("n_wh", 1) or 1
        base = r.get("per_wh") or step
        for k in range(0, 200):
            for sign in ((1, -1) if k > 0 else (1,)):
                per_wh = base + sign * k * step
                if per_wh < step or collides(per_wh * n_wh):
                    continue
                commit(r, per_wh)
                taken.add(r["dmg_eff"])
                break
            else:
                continue
            break


def fine_tune_range(rows, spec, band_lo, band_hi):
    """Snap each member to the range that zeroes Δ at its FINAL DPS.

    Range moves on a 10 grid, so it is a much finer price lever than one Damage
    step — which is why it runs LAST, after the coarse Damage grid is fixed.
    Members the DPS solve could not price at all (`over_priced`) are left alone.
    """
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


def fine_tune_speed(rows, spec, spd_lo, spd_hi):
    """FOOT infantry only, step 1: squeeze any member still off Δ0.

    The maintainer allows Speed±1 on foot infantry as a fine price lever. Units
    with a turn rate keep multiples of 5 (too coarse) and are left alone.
    """
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
        # Pick the nearest unused speed to ideal (speed stays unique) — but only
        # if it actually helps. The band is 0.8..1.2 x speed0, so a 21-member
        # class occupies most of it, and "nearest unused" can be nowhere near the
        # ideal. Moving there anyway made `scout` WORSE, not better (worst |Δ|
        # 22.8 -> 47.0, with ra2_allies_gi thrown to -37.2): this lever is only
        # ever an improvement when the slot it can reach is an improvement.
        taken_spd.discard(r["spd"])
        best, best_d = r["spd"], abs(d)
        for cand in sorted(range(spd_lo, spd_hi + 1), key=lambda s: (abs(s - ideal), s)):
            if cand in taken_spd:
                continue
            cd = abs(_price(spec, r["hp"], cand, r["rng"], r["dps_eff"],
                            r["special"], r["tech_tier"]) - r["cost"])
            if cd < best_d - 1e-9:
                best, best_d = cand, cd
        r["spd"] = best
        taken_spd.add(r["spd"])


def polish_residuals(rows, spec, band_lo, band_hi, spd_lo, spd_hi, key, step=None):
    """Joint (Damage, Speed, Range) search for members coordinate descent stranded.

    The three levers run one after another, each optimal on its own — and that is
    exactly how a coordinate descent gets stuck. `ra1_soviets_ak47conscript` wants
    Damage 344; on the 100 grid at its speed the reachable slots are 300 (Δ -15.6)
    and 400 (Δ +15.6), so no single Damage move helps, and with Damage pinned at
    300 no single Speed move helps either. The pair (400, speed 62) prices it
    EXACTLY, and neither lever can find it alone.

    So: for each member still off by more than a credit, walk its Damage slots and
    its Speed band TOGETHER, solving Range in closed form at each pair, and keep
    the best combination that respects every uniqueness rule. Cheap — a dozen
    slots times a 25-wide speed band, only for the rows that are actually stuck.
    """
    step = formula.DAMAGE_STEP if step is None else step

    def price(r, spd, rng, dps):
        return _price(spec, r["hp"], spd, rng, dps, r["special"], r["tech_tier"])

    for r in sorted(rows, key=lambda r: -abs(
            price(r, r["spd"], r["rng"], r["dps_eff"]) - r["cost"])):
        if r["protected"] or r.get("soft") or r.get("over_priced"):
            continue
        cur = abs(price(r, r["spd"], r["rng"], r["dps_eff"]) - r["cost"])
        if cur <= 1:
            continue
        others = [o for o in rows if o is not r]
        busy_spd = {o["spd"] for o in others}
        busy_rng = {o["rng"] for o in others if not o.get("protected")}
        blocked = {(o["dps_eff"] if key == "dps" else o["dmg_eff"])
                   for o in others if not (o["protected"] or o.get("soft"))}
        n_wh = r.get("n_wh", 1) or 1
        base = r.get("per_wh") or step
        best = None
        for k in range(-8, 9):
            per_wh = base + k * step
            if per_wh < step:
                continue
            dmg_shot = per_wh * n_wh
            dps = r["per_unit"] * dmg_shot
            if any(abs(b - (dps if key == "dps" else dmg_shot)) <= 0.5 for b in blocked):
                continue
            spds = ([s for s in range(spd_lo, spd_hi + 1) if s not in busy_spd]
                    if r.get("spd_step", 1) == 1 else [r["spd"]])
            for spd in spds + [r["spd"]]:
                rng = formula.solve_class_baseline_range(
                    r["cost"], r["hp"], spd, dps, spec["hp0"], spec["speed0"],
                    spec["range0_wdist"], spec["dps0"], spec["cost0"],
                    special=r["special"], tech_tier=r["tech_tier"])
                rng = max(band_lo, min(band_hi, int(round(rng / 10)) * 10))
                if rng in busy_rng and rng != r["rng"]:
                    continue
                d = abs(price(r, spd, rng, dps) - r["cost"])
                if best is None or d < best[0] - 1e-9:
                    best = (d, per_wh, spd, rng, dps, dmg_shot)
        if best and best[0] < cur - 1e-9:
            _, per_wh, spd, rng, dps, dmg_shot = best
            r["per_wh"], r["spd"], r["rng"] = per_wh, spd, rng
            r["dmg_shot"] = r["dmg_eff"] = dmg_shot
            r["dps_eff"] = dps


def rebalance_class(cls: str, uniqueness: str = "damage"):
    rows, spec, band_lo, band_hi, spd_lo, spd_hi = load_class_rows(cls)
    if not rows:
        return ""
    nudge_hp_spd(rows, spd_lo=spd_lo, spd_hi=spd_hi)
    # 1. Range: protected → spec; others → clamp current into band (preserve feel).
    for r in rows:
        if r["protected"]:
            continue
        cur = r["rng"] or r["arm_rng"] or (band_lo + band_hi) // 2
        r["rng"] = max(band_lo, min(band_hi, int(round(cur / 10)) * 10))
    nudge_ranges(rows, band_lo, band_hi)     # range uniqueness (skips protected)
    # 2. Per member: solve target eff-DPS for Δ0 at final (hp,spd,rng); decompose
    #    to 100-grid warhead Damage at FP=1 (cost pinned, stats trimmed law).
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
            # No positive DPS prices this unit at its cost: park it at the
            # weakest the grid can express (W17: one step at fp=1, which is the
            # same effective damage the old `2000, 0.05` dead-end produced).
            r["per_wh"], r["fp"] = formula.DAMAGE_STEP, 1.0
            r["over_priced"] = True
            tgt = 0.0
        else:
            r["over_priced"] = False
        D, fp = decompose_dps(tgt, r["base_dps"], r["dmg_shot0"] or 1, r["n_wh"])
        if r.get("over_priced"):
            D, fp = formula.DAMAGE_STEP, 1.0
        r["per_wh"] = D
        r["fp"] = fp
        r["dmg_shot"] = D * r["n_wh"]
        r["dmg_eff"] = r["dmg_shot"] * fp
        r["dps_eff"] = r["per_unit"] * r["dmg_shot"] * fp
        r["trimmed"] = (r["dmg_shot"] != (r["dmg_shot0"] or r["dmg_shot"]))
    # 3. Effective-damage-per-shot uniqueness + the two fine levers, iterated.
    #    ⚠ ORDER MATTERS AND IT USED TO BE BACKWARDS. The COARSE lever (Damage on
    #    the 100 grid) has to be set BEFORE the fine ones (Range on a 10 grid,
    #    Speed by 1), because one Damage step is a whole shot: running uniqueness
    #    last threw away everything the fine-tuners had achieved. Measured on
    #    `scout`, worst |Δ| across that single call was 15.6 -> 66.5.
    #
    #    They are ITERATED rather than run once because the three constrain each
    #    other: uniqueness scores a candidate at the current range/speed, the
    #    fine-tuners then move both, and their own uniqueness rules (nudge_ranges,
    #    the taken-speed set) can refuse the value the score assumed. Scoring
    #    against what the bands COULD absorb instead of what they do is worse, not
    #    better — it assumes an absorption that collides away, and worst |Δ| on
    #    `scout` went to 74.3. So: run the trio, measure, keep the best state.
    def price_at(r, dmg_shot):
        return _price(spec, r["hp"], r["spd"], r["rng"],
                      r["per_unit"] * dmg_shot, r["special"], r["tech_tier"])

    def worst_delta():
        return max((abs(_price(spec, r["hp"], r["spd"], r["rng"], r["dps_eff"],
                               r["special"], r["tech_tier"]) - r["cost"])
                    for r in rows if not r["protected"]), default=0.0)

    def snapshot():
        return [{k: r[k] for k in ("hp", "spd", "rng", "per_wh", "dmg_shot",
                                   "dmg_eff", "dps_eff")} for r in rows]

    def run(passes, use_polish):
        """One descent. Returns (worst |Δ|, state). Keeps the best state it saw:
        the trio is NOT monotone — a pass can trade one member's residual for
        another's and come back better two rounds later (`scout` walks
        51.0 -> 37.2 -> 32.1 -> 22.8), so stopping at the first non-improvement
        freezes it early. Run the whole budget, keep the best."""
        seen, seen_score = snapshot(), None
        for _ in range(passes):
            unique_dmg_per_shot(rows, price_of=price_at, key=uniqueness)
            fine_tune_range(rows, spec, band_lo, band_hi)
            fine_tune_speed(rows, spec, spd_lo, spd_hi)
            fine_tune_range(rows, spec, band_lo, band_hi)
            if use_polish:
                polish_residuals(rows, spec, band_lo, band_hi, spd_lo, spd_hi,
                                 uniqueness)
            score = worst_delta()
            if seen_score is None or score < seen_score - 1e-9:
                seen, seen_score = snapshot(), score
        return seen_score, seen

    # The joint polish is a large win where the levers are stuck in a local
    # optimum and a loss where they are merely over-subscribed: on `scout` it
    # takes DPS-uniqueness from 15.6 to 0.7, and can push damage-uniqueness from
    # 22.8 to 32.1 by perturbing the descent into a worse basin. Which way it
    # goes is not predictable from the class, and it also moves when an unrelated
    # grid changes — switching vehicles to their lawful 2500 HP step re-landed
    # `scout` in the worse basin on its own. So do not pick: run the descents,
    # SEED one from the other's best, and keep whichever state actually measures
    # lowest. Each descent is a few milliseconds.
    start = snapshot()
    best_score, best = None, start

    def consider(score, state):
        nonlocal best_score, best
        if score is not None and (best_score is None or score < best_score):
            best_score, best = score, state

    for seed, polish in ((start, True), (start, False), (None, True), (None, False)):
        if seed is not None:
            for r, saved in zip(rows, seed):
                r.update(saved)
        else:                       # seeded from the best found so far
            for r, saved in zip(rows, best):
                r.update(saved)
        consider(*run(10, use_polish=polish))
    for r, saved in zip(rows, best):
        r.update(saved)
    # 4. Prices / deltas
    for r in rows:
        r["price"] = _price(spec, r["hp"], r["spd"], r["rng"], r["dps_eff"],
                            r["special"], r["tech_tier"])
        r["delta"] = r["price"] - r["cost"]
    return render_report(rows, cls, uniqueness)


def render_report(rows, cls, uniqueness="damage"):
    s = load_anchors()[cls]["spec"]
    lines = [
        f"# {cls.replace('_', ' ').title()} infantry rebalance proposal",
        "",
        f"Anchor spec: HP={s['hp0']}, Speed={s['speed0']}, Range={s['range0_wdist']}, eff-DPS={s['dps0']}, Cost={s['cost0']}",
        "",
        "Converter law: cost pinned, range clamped to band + made unique, "
        "eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.",
        "",
        f"Uniqueness separates **{UNIQUENESS_KEYS[uniqueness]}**. "
        "The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) "
        "— because one Damage step is a whole shot and running it last threw away "
        "everything the fine levers had achieved.",
        "",
        "| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |",
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
        # W17: a legacy unconditional FirepowerMultiplier still in yaml would
        # apply ON TOP of the prescribed Damage, which is solved at fp=1.
        if not r["protected"] and abs(r.get("fp0", 1.0) - 1.0) > 1e-9:
            flags = (flags + " fp-debt").strip()
        if not r["protected"]:
            worst = max(worst, abs(r["delta"]))
        dcol = f"{r.get('per_wh') or r['dmg']}×{r.get('n_wh', 1)}"
        lines.append(
            f"| `{r['actor']}` | {r['faction']} | {r['hp']} | {r['spd']} | {r['rng']} | "
            f"{r['cost']} | {dcol} | {r['rl']} | {r['burst']} | {int(round(r['fp0'] * 100))} | "
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
        changes = [
            f"HP {r['hp']}, Speed {r['spd']}, Range {r['rng']}",
            f"each offensive warhead Damage {r.get('per_wh')} (×{r.get('n_wh',1)} = "
            f"SUM {r.get('dmg_shot')}), ReloadDelay {r['rl']}, Burst {r['burst']}",
        ]
        # W17: Damage is solved at fp=1, so a surviving unconditional
        # FirepowerMultiplier would scale it a SECOND time.
        if abs(r.get("fp0", 1.0) - 1.0) > 1e-9:
            changes.append(
                f"**DELETE the unconditional FirepowerMultiplier "
                f"({int(round(r['fp0'] * 100))}%)** — the Damage above already "
                f"includes it (W17)")
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
    ap.add_argument("--uniqueness", choices=("damage", "dps"), default="damage",
                    help="which quantity the 5-stat uniqueness law separates. "
                         "`damage` (default) is the law as written — the raw "
                         "warhead Damage field. `dps` is a MEASUREMENT of the "
                         "alternative described in UNIQUENESS_KEYS and needs a "
                         "maintainer ruling before anything is written from it.")
    args = ap.parse_args()
    text = rebalance_class(args.cls, uniqueness=args.uniqueness)
    if not text:
        print(f"no units found for class {args.cls} "
              f"(tag members with design.class_anchor=={args.cls} first)")
        return
    # The "_infantry" suffix used to be HARDCODED here, so every class wrote
    # `proposal_<class>_infantry.md` — `proposal_tank_destroyer_infantry.md` for a
    # vehicle class. The suffix dates from when only infantry classes were converted.
    out = ROOT / "docs" / "balance" / f"proposal_{args.cls}.md"
    out.write_text(text, encoding="utf-8")
    print(f"wrote {out.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
