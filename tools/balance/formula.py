#!/usr/bin/env python3
"""formula.py — the balance law in code (BALANCE_PIPELINE.md §3, §5).

Single implementation of the price formulas; the workbook builder emits
the SAME math as Excel formulas, and test_formula.py proves this module
against the legacy workbook's own cached cell values.

Units follow the ledger (RAW): range in wdist (5000 = legacy sheet 5.0),
reload in ticks, damage as written. The legacy sheet's Range column is
wdist/1000 — conversion happens HERE and in the sheet's helper column,
never in stored data.

Tiger anchor (DESIGN §12): 100000 HP, 100 speed, 10000 damage,
range 5000, reload 50, all modifiers 1 -> O = P = Q = price = 800.
"""
from __future__ import annotations


def eff_reload(reload_delay: float, burst: int = 1, burst_delays: float | None = None) -> float:
    """Effective ticks per full burst cycle."""
    if burst and burst > 1:
        return reload_delay + (burst_delays or 0) * (burst - 1)
    return reload_delay


def dps(damage: float, reload_delay: float, burst: int = 1,
        burst_delays: float | None = None,
        firepower_multiplier: float = 1.0) -> float:
    """Burst-aware DPS. With burst=1 this is the legacy G/I*H exactly.

    firepower_multiplier is the per-actor FirepowerMultiplier value
    expressed as a factor (1.0 = 100%), used to fine-tune effective
    damage output without leaving the 2000-step Damage grid.

    **`weapon_class` was REMOVED here on 2026-08-11 (W4).** It was a tier weight
    standing in for "how good is this weapon type", back when nothing measured
    that. The K coefficient now measures it directly from the weapon's own
    geometry (`weapon_efficiency.py`), so keeping the tier weight as well would
    charge a weapon twice for the same property. The value still lives in the
    ledger as `design_weapon_class` (design judgment, and the weapon-class gate
    reads it) — it simply no longer multiplies the price."""
    base = damage * max(burst, 1) / eff_reload(reload_delay, burst, burst_delays)
    return base * firepower_multiplier


# Charge-up is an ACTOR property, not a weapon one (maintainer ruling 2026-08-11).
# A charge delay is a large real nerf that DPS alone cannot see: the delay inflates
# the effective reload AND the unit is helpless while charging, so the price drops.
CHARGE_UP_PRICE_MULTIPLIER = 0.75

# Traits that make an actor pay for a charge-up. `AttackCharges` is the Obelisk of
# Light — the case the ruling itself cites as the model — which does NOT use one of
# the three `*Charged` traits, so naming only those would have left the cited
# precedent unpriced.
CHARGE_UP_TRAITS = frozenset({
    "AttackCharged", "AttackTurretedCharged", "AttackFrontalCharged",
    "AttackCharges",
})

# `AttackTesla` charges too, but is deliberately NOT in the set above: the Tesla
# Coil is already priced as a special case (ReloadDelay 100 + InitialChargeDelay 25,
# MaxCharges 3, its own K), so applying the generic 0.75x on top would discount it
# twice. Needs its own maintainer ruling before it joins.
CHARGE_UP_EXCLUDED_TRAITS = frozenset({"AttackTesla"})


def charge_price_multiplier(charge_trait: str | None) -> float:
    """`CHARGE_UP_PRICE_MULTIPLIER` for a charging actor, else 1.0.

    Applied to the PRICE, not to DPS: price is degree 1/2/3 in its inputs
    (O/P/Q), so scaling DPS would not yield a clean 0.75x on the result.
    """
    if not charge_trait:
        return 1.0
    return (CHARGE_UP_PRICE_MULTIPLIER
            if str(charge_trait).split("@", 1)[0] in CHARGE_UP_TRAITS else 1.0)


# Twin warheads — NEVER main / NEVER in the damage total (DESIGN.md):
#   *ExtraDamage   Tesla/Nuclear shield chip — ALWAYS excluded from damage
#   *FriendlyFire  own-side splash (50% twin)
#   *Percentage    HealthPercentageDamage (1-per-2000 twin)
_TWIN_SUFFIXES = ("extradamage", "percentage", "friendlyfire")


def _is_main_spread(w, template_names=None) -> bool:
    """A MAIN damage warhead: a `SpreadDamage` node whose name matches an
    inherited weapon-class template (`Warhead@TankDestroyerCannon` ↔
    `^TankDestroyerCannon`). The `*ExtraDamage` / `*FriendlyFire` /
    `*Percentage` twins are NEVER main (ExtraDamage is ALWAYS excluded from
    the damage calculation). When ``template_names`` is None the template
    match is skipped (any non-twin SpreadDamage qualifies)."""
    # AreaDamage is the Cameo drop-in for SpreadDamage (expanding rings + baked
    # FF; at Ticks 1/MaxRadius 0 it is byte-identical) -> treat it as a main too.
    if (w.get("type") or "") not in ("SpreadDamage", "AreaDamage"):
        return False
    tag = (w.get("tag") or "").lower()
    if tag.endswith(_TWIN_SUFFIXES):
        return False
    if template_names:
        return tag in template_names
    return True


def main_spread_warheads(warheads, template_names=None) -> list:
    """The main damage warheads (see ``_is_main_spread``). ``template_names``
    may carry the leading ``^``; it is normalised here. If a template list is
    given but nothing matches — a non-conforming special weapon (nuke rings,
    waveforce, death-explosion) whose warheads are not named after its
    templates — fall back to every non-twin SpreadDamage so a combat weapon
    never silently reads as 0 damage."""
    warheads = warheads or []
    if template_names:
        tn = {str(t).lower().lstrip("^") for t in template_names}
        mains = [w for w in warheads if _is_main_spread(w, tn)]
        if mains:
            return mains
    return [w for w in warheads if _is_main_spread(w)]


def spread_damage_sum(warheads, smallarms_only: bool = False,
                      template_names=None) -> float:
    """Effective per-shot damage = SUM of the MAIN damage warheads
    (maintainer law 2026-07-22; main = template-named SpreadDamage, see
    ``main_spread_warheads``). A multi-warhead weapon deals the ADDED damage
    of all its warheads to a target, so the SUM — never the max — is the price
    driver: pricing on the max would let a 10-warhead weapon deal 10x the
    damage for the price of one. `*ExtraDamage` / `*FriendlyFire` /
    `*Percentage` twins are excluded.

    This is the ONE canonical warhead-damage reducer; every pricing tool MUST
    call it so the MAX convention can never creep back in. ``smallarms_only``
    restricts the sum to SmallArms warheads (cheap scouts <=150% of C0 price
    only their SmallArms warhead)."""
    total = 0.0
    for w in main_spread_warheads(warheads, template_names):
        if smallarms_only and not (w.get("tag") or "").lower().startswith("smallarms"):
            continue
        try:
            total += float(w.get("damage"))
        except (TypeError, ValueError):
            continue
    return total


DAMAGE_STEP = 2000


def snap_damage_step(value: float, step: int = DAMAGE_STEP) -> int:
    """Nearest multiple of the 2000-damage grid (DESIGN.md); a positive
    value never snaps below one step."""
    if value <= 0:
        return 0
    return max(step, int(round(value / step)) * step)


def distribute_damage(new_total, warheads, template_names=None) -> dict[str, int]:
    """Turn a design per-shot TOTAL (a spread_damage_sum) into per-warhead
    Damage values by the FIXED law in DESIGN.md ("Damage: 2000-steps"):

      * EVERY main damage warhead (template-named SpreadDamage, see
        ``main_spread_warheads``) carries the IDENTICAL value
        D = new_total / N snapped to the 2000-damage grid — they never
        differ ("all class warheads carry the identical value"). Coarse
        control only; fine-tune the effective damage with the actor's
        FirepowerMultiplier, NEVER by making warheads unequal or off-grid.
      * each ``*FriendlyFire`` SpreadDamage twin = 50% of D (D // 2).
      * each ``*ExtraDamage`` SpreadDamage twin = 50% of D (D // 2): energy
        weapons trade area-of-effect for a shield/bonus chip, so it is
        ALWAYS half the main — yet still EXCLUDED from the damage total.
      * each ``*Percentage`` HealthPercentageDamage twin = 1 per 2000 of D
        (D // 2000, i.e. 16000 -> 8).

    This is the ONE canonical way to write per-warhead Damage, so a single
    design number can NEVER again be broadcast identically onto every
    warhead (the 2026-07-22 over-damage bug). Returns {tag: new_int_damage}
    for every warhead the law assigns a value to.
    """
    warheads = warheads or []
    mains = main_spread_warheads(warheads, template_names)
    if not mains:
        return {}
    main_tags = {(w.get("tag") or "") for w in mains}
    per = snap_damage_step(float(new_total) / len(mains))

    result: dict[str, int] = {}
    for w in warheads:
        tag = w.get("tag") or ""
        low = tag.lower()
        if tag in main_tags:
            result[tag] = per
        elif low.endswith(("friendlyfire", "extradamage")):
            # 50% twin — ExtraDamage may be SpreadDamage OR OpenToppedDamage
            # (e.g. SniperWeaponExtraDamage); the rule is type-agnostic.
            result[tag] = per // 2
        elif low.endswith("percentage"):           # HealthPercentageDamage twin
            result[tag] = per // DAMAGE_STEP
    return result


def estimators(hp: float, speed: float, range_wdist: float, dps_value: float,
               special: float = 1.0, unit_class: float = 1.0,
               tech_tier: float = 1.0) -> tuple[float, float, float]:
    """The legacy O/P/Q price estimators (recovered from the workbook
    cells 2026-07-18), on raw units (range in wdist)."""
    r = range_wdist / 1000.0
    o = (hp / 100000 + speed / 100 + r * special / 5 + dps_value / 200) \
        * 200 * unit_class * tech_tier
    p = ((hp * speed / 25000) + (r * special * dps_value / 2.5)) \
        * unit_class * tech_tier
    q = (hp * speed * r * special * dps_value * unit_class * tech_tier) \
        / 12500000
    return o, p, q


def price(hp, speed, range_wdist, dps_value, special=1.0, unit_class=1.0,
          tech_tier=1.0) -> float:
    o, p, q = estimators(hp, speed, range_wdist, dps_value,
                         special, unit_class, tech_tier)
    return (o + p + q) / 3


def solve_range(cost: float, hp: float, speed: float, dps_value: float,
                special: float = 1.0, unit_class: float = 1.0,
                tech_tier: float = 1.0) -> float:
    """Range (wdist) such that price == cost. The estimator mean is
    LINEAR in range, so the closed form is exact:
    price(r) = A + B*r  ->  r = (cost - A) / B."""
    a_o = (hp / 100000 + speed / 100 + dps_value / 200) * 200 * unit_class * tech_tier
    b_o = (special / 5) * 200 * unit_class * tech_tier
    a_p = (hp * speed / 25000) * unit_class * tech_tier
    b_p = (special * dps_value / 2.5) * unit_class * tech_tier
    a_q = 0.0
    b_q = (hp * speed * special * dps_value * unit_class * tech_tier) / 12500000
    a = (a_o + a_p + a_q) / 3
    b = (b_o + b_p + b_q) / 3
    if b == 0:
        raise ZeroDivisionError("price is range-independent for this unit")
    return (cost - a) / b * 1000.0  # back to wdist


def class_anchor_price(o, p, q, o0, p0, q0, cost0) -> float:
    """Formula v2 draft form (superseded by class_baseline_price):
    normalized deviation from the class anchor. Exact at the anchor."""
    return cost0 * (o / o0 + p / p0 + q / q0) / 3


def class_baseline_estimators(hp, speed, range_wdist, dps_value,
                              hp0, speed0, range0_wdist, dps0, cost0,
                              special=1.0, tech_tier=1.0) -> tuple[float, float, float]:
    """Formula v2 FINAL form (maintainer rule 2026-07-18): per-stat
    normalization against the class baseline unit, so that at the
    baseline O = P = Q = cost0 EXACTLY — the rule that must always hold
    for any baseline unit. The global Tiger formula is precisely this
    construction with (100000, 100, 5000, 200, 800) plugged in."""
    h = hp / hp0
    s = speed / speed0
    r = (range_wdist / range0_wdist) * special
    d = dps_value / dps0
    o = (h + s + r + d) * cost0 / 4 * tech_tier
    p = ((h * s) + (r * d)) * cost0 / 2 * tech_tier
    q = (h * s * r * d) * cost0 * tech_tier
    return o, p, q


def class_baseline_price(hp, speed, range_wdist, dps_value,
                         hp0, speed0, range0_wdist, dps0, cost0,
                         special=1.0, tech_tier=1.0) -> float:
    o, p, q = class_baseline_estimators(hp, speed, range_wdist, dps_value,
                                        hp0, speed0, range0_wdist, dps0,
                                        cost0, special, tech_tier)
    return (o + p + q) / 3


def class_baseline_estimators_3(hp, range_wdist, dps_value,
                                hp0, range0_wdist, dps0, cost0,
                                special=1.0, tech_tier=1.0) -> tuple[float, float, float]:
    """Speed-less 3-input form (HP, Range, DPS) for STATIC units (defenses).

    Same construction as the 4-input v2 form but with the elementary-symmetric
    MEANS of THREE normalized ratios, so all three terms apply the SAME logic
    (degree 1 / degree 2 / degree 3) and O = P = Q = cost0 EXACTLY at the
    baseline (maintainer rule 2026-07-26):

        h = hp / hp0 ; r = (range / range0) * special ; d = dps / dps0
        O = (h + r + d) / 3           * cost0   # degree 1: mean of the singles
        P = (h*r + h*d + r*d) / 3     * cost0   # degree 2: mean of the pairs
        Q = (h * r * d)              * cost0    # degree 3: the triple product

    At the baseline (h=r=d=1): O = P = Q = cost0 and price = cost0.
    Price is still LINEAR in r (h, d constant), so solve_range stays closed-form.
    """
    h = hp / hp0
    r = (range_wdist / range0_wdist) * special
    d = dps_value / dps0
    o = (h + r + d) / 3 * cost0 * tech_tier
    p = (h * r + h * d + r * d) / 3 * cost0 * tech_tier
    q = (h * r * d) * cost0 * tech_tier
    return o, p, q


def class_baseline_price_3(hp, range_wdist, dps_value,
                           hp0, range0_wdist, dps0, cost0,
                           special=1.0, tech_tier=1.0) -> float:
    o, p, q = class_baseline_estimators_3(hp, range_wdist, dps_value,
                                          hp0, range0_wdist, dps0,
                                          cost0, special, tech_tier)
    return (o + p + q) / 3


def solve_class_baseline_range_3(cost, hp, dps_value,
                                 hp0, range0_wdist, dps0, cost0,
                                 special=1.0, tech_tier=1.0) -> float:
    """Range (wdist) such that class_baseline_price_3 == cost.

    class_baseline_price_3 is linear in the normalized range term
    r = (range / range0) * special (h, d are constants), so:
        3*price = [(h+d)/3 + h*d/3] * cost0      (the r-free part, A3)
                + [1/3 + (h+d)/3 + h*d] * cost0 * r   (coeff of r, B3)
        r = (3*cost - A3) / B3
    """
    h = hp / hp0
    d = dps_value / dps0
    a3 = ((h + d) / 3 + (h * d) / 3) * cost0 * tech_tier
    b3 = (1.0 / 3 + (h + d) / 3 + h * d) * cost0 * tech_tier
    if b3 == 0:
        raise ZeroDivisionError("class_baseline_price_3 is range-independent for these stats")
    r_norm = (cost * 3 - a3) / b3
    return (r_norm / special) * range0_wdist


def solve_class_baseline_range(cost, hp, speed, dps_value,
                               hp0, speed0, range0_wdist, dps0, cost0,
                               special=1.0, tech_tier=1.0) -> float:
    """Range (wdist) such that class_baseline_price == cost.

    class_baseline_price is linear in the normalized range term
    r = (range / range0_wdist) * special, so the closed form is exact:

        o = (h+s+r+d) * cost0/4 * tech_tier
        p = ((h*s) + (r*d)) * cost0/2 * tech_tier
        q = (h*s*r*d) * cost0 * tech_tier
        price = (o + p + q) / 3

    Collecting constants and r-coefficients:
        A = o_const + p_const   (terms without r)
        B = o_r + p_r + q_r     (coefficients of r)
        r = (3*cost - A) / B
        range_wdist = (r / special) * range0_wdist
    """
    h = hp / hp0
    s = speed / speed0
    d = dps_value / dps0
    a = (h + s + d) * cost0 / 4 * tech_tier
    c = (h * s) * cost0 / 2 * tech_tier
    b = cost0 / 4 * tech_tier
    d1 = d * cost0 / 2 * tech_tier
    e = h * s * d * cost0 * tech_tier
    denom = b + d1 + e
    if denom == 0:
        raise ZeroDivisionError("class_baseline_price is range-independent for these stats")
    r_norm = (cost * 3 - (a + c)) / denom
    return (r_norm / special) * range0_wdist


def _selftest() -> None:
    """Runnable invariant checks: `python tools/balance/formula.py`."""
    def wh(tag, dmg, typ="SpreadDamage"):
        return {"tag": tag, "damage": str(dmg), "type": typ}

    def mains(res):
        return {t: v for t, v in res.items()
                if not t.lower().endswith(("extradamage", "percentage", "friendlyfire"))}

    # DESIGN.md law: mains identical on the 2000 grid, FF=50%, ExtraDamage
    # =50% (excluded from total), Pct=1/2000. total 10000 / 5 mains -> 2000.
    whs = [wh("A", 22000), wh("Aextradamage", 22000, "SpreadDamage"),
           wh("Apercentage", 1, "HealthPercentageDamage"),
           wh("B", 22000), wh("C", 22000), wh("D", 22000),
           wh("Dfriendlyfire", 22000), wh("E", 22000)]
    r = distribute_damage(10000, whs)
    assert set(mains(r).values()) == {2000}, r          # identical mains
    assert sum(mains(r).values()) == 10000, r
    assert r["Dfriendlyfire"] == 1000, r                # FF = 50%
    assert r["Aextradamage"] == 1000, r                 # ExtraDamage = 50%
    assert r["Apercentage"] == 1, r                     # 1 per 2000
    # ExtraDamage carries a value (50%) but is EXCLUDED from the total
    assert spread_damage_sum(whs) == 22000 * 5          # Aextradamage not summed

    # 16000 main -> Percentage 8 (DESIGN.md example)
    r = distribute_damage(16000, [wh("m", 4000), wh("mpercentage", 1, "HealthPercentageDamage")])
    assert r["m"] == 16000 and r["mpercentage"] == 8, r

    # off-grid total snaps to the 2000 step; mains stay identical (fine-tune=FP)
    r = distribute_damage(9000, [wh("a", 2000), wh("b", 2000), wh("c", 2000)])
    assert len(set(r.values())) == 1, r                 # identical
    assert all(v % 2000 == 0 for v in r.values()), r    # on the 2000 grid

    # Tiger anchor (DESIGN §12) still holds
    assert round(price(100000, 100, 5000, dps(10000, 50))) == 800

    print("formula self-test OK")


if __name__ == "__main__":
    _selftest()
