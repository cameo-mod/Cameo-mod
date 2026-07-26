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


def dps(damage: float, reload_delay: float, weapon_class: float = 1.0,
        burst: int = 1, burst_delays: float | None = None,
        firepower_multiplier: float = 1.0) -> float:
    """Burst-aware DPS. With burst=1 this is the legacy G/I*H exactly.

    firepower_multiplier is the per-actor FirepowerMultiplier value
    expressed as a factor (1.0 = 100%), used to fine-tune effective
    damage output without leaving the 2000-step Damage grid."""
    base = damage * max(burst, 1) / eff_reload(reload_delay, burst, burst_delays) * weapon_class
    return base * firepower_multiplier


def spread_damage_sum(warheads, smallarms_only: bool = False) -> float:
    """Effective per-shot damage = SUM of every offensive SpreadDamage warhead
    (maintainer law 2026-07-22). A multi-warhead weapon deals the ADDED damage
    of all its warheads to a target, so the SUM — never the max — is the price
    driver: pricing on the max would let a 10-warhead weapon deal 10x the damage
    for the price of one. Excluded from the sum:
      * ``*ExtraDamage``   shield-only chip damage (intentional, priced elsewhere)
      * ``*Percentage``    HealthPercentageDamage (not SpreadDamage anyway)
      * ``*FriendlyFire``  own-side splash, not offensive value
    This is the ONE canonical warhead-damage reducer; every pricing tool MUST
    call it so the MAX convention can never creep back in.
    ``smallarms_only`` restricts the sum to SmallArms warheads (cheap scouts
    <=150% of C0 price only their SmallArms warhead)."""
    total = 0.0
    for w in warheads or []:
        if (w.get("type") or "") != "SpreadDamage":
            continue
        tag = (w.get("tag") or "").lower()
        if tag.endswith(("extradamage", "percentage", "friendlyfire")):
            continue
        if smallarms_only and not tag.startswith("smallarms"):
            continue
        try:
            total += float(w.get("damage"))
        except (TypeError, ValueError):
            continue
    return total


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
