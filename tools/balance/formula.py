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
        burst: int = 1, burst_delays: float | None = None) -> float:
    """Burst-aware DPS. With burst=1 this is the legacy G/I*H exactly."""
    return damage * max(burst, 1) / eff_reload(reload_delay, burst, burst_delays) * weapon_class


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
    """Formula v2 (DESIGN §12 second iteration): normalized deviation
    from the class anchor. Exact at the anchor."""
    return cost0 * (o / o0 + p / p0 + q / q0) / 3
