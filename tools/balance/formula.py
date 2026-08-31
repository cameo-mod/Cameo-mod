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

import re
import math

_COND_TOKEN = re.compile(r"[A-Za-z_][A-Za-z0-9_.\-]*")


def condition_holds_by_default(expr: str | None) -> bool:
    """Is this OpenRA condition expression true for a UNIT AS BUILT?

    Base pricing has to use the weapon a unit fires the moment it rolls off the
    line: no promotions, no researched upgrades, not garrisoned, not deployed.
    So every named condition is evaluated as FALSE and the expression is reduced.

        `!rank-elite`                        -> True   (the BASE weapon)
        `rank-elite`                         -> False  (the promoted weapon)
        `!forgotten_upgrade_chemicalweapons` -> True   (before the upgrade)
        `ifv-miss && !rank-elite`            -> False  (needs a passenger)
        `shieldgen >= 1`                     -> False  (0 by default)
        empty / None                         -> True   (unconditional)

    This replaces the old "any `requires` means skip it" rule, which threw away
    the BASE weapon of every unit that merely has an elite variant — 371 of 863
    actors with priced armaments had zero DPS and dropped out of pricing entirely
    (measured 2026-08-15), including `tiger.nax`, the recorded `mbt` anchor.
    """
    if expr is None:
        return True
    src = expr.strip()
    if not src:
        return True

    # Order matters: `!=` must survive the `!` -> `not` rewrite.
    src = src.replace("!=", "\x00NE\x00")
    src = src.replace("&&", " and ").replace("||", " or ").replace("!", " not ")
    src = src.replace("\x00NE\x00", "!=")

    # Every condition name is 0/False. Keep Python keywords the rewrite produced.
    keep = {"and", "or", "not", "True", "False"}
    src = _COND_TOKEN.sub(lambda m: m.group(0) if m.group(0) in keep else "0", src)

    try:
        return bool(eval(src, {"__builtins__": {}}, {}))  # noqa: S307 - digits/operators only
    except Exception:
        # An expression we cannot parse is NOT silently treated as a base weapon:
        # over-counting DPS inflates a price, and a wrong price is worse than a
        # missing one because it looks authoritative.
        return False


ENGINE_DEFAULT_RELOAD_DELAY = 1.0
ENGINE_DEFAULT_BURST = 1
ENGINE_DEFAULT_BURST_DELAY = 5.0
ENGINE_DEFAULT_RANGE = 0.0
INT32_MIN = -(2 ** 31)
INT32_MAX = 2 ** 31 - 1
_INT32_TEXT = re.compile(r"^[+-]?[0-9]+$")
_WDIST_TEXT = re.compile(
    r"^([+-]?[0-9]+)\s*(?:c\s*([+-]?[0-9]+))?$", re.IGNORECASE)


def parse_int32(raw, field: str = "value", default=None) -> int | None:
    """Parse one C# ``Int32`` field without accepting fractional coercion."""
    if raw is None:
        return default
    if isinstance(raw, bool):
        raise ValueError(f"{field} must be an Int32")
    if isinstance(raw, int):
        value = raw
    elif isinstance(raw, float):
        if not math.isfinite(raw) or not raw.is_integer():
            raise ValueError(f"{field} must be an Int32")
        value = int(raw)
    else:
        text = str(raw).strip()
        if not _INT32_TEXT.fullmatch(text):
            raise ValueError(f"{field} must be an Int32")
        value = int(text)
    if value < INT32_MIN or value > INT32_MAX:
        raise ValueError(f"{field} is outside the Int32 range")
    return value


def parse_bool(raw, field: str = "value", default=None) -> bool | None:
    """Parse one C# ``bool`` field (only true/false, case-insensitive)."""
    if raw is None:
        return default
    if isinstance(raw, bool):
        return raw
    text = str(raw).strip().lower()
    if text == "true":
        return True
    if text == "false":
        return False
    raise ValueError(f"{field} must be true or false")


def _unchecked_int32(value: int) -> int:
    """Wrap an integer like C# arithmetic outside a checked context."""
    return (value - INT32_MIN) % (2 ** 32) + INT32_MIN


def parse_wdist(raw, *, allow_distribution: bool = False) -> int:
    """Parse OpenRA's integer or cell-relative WDist notation.

    Ledger values normally contain raw WDist integers, but OpenRA also accepts
    values such as ``40c0`` (40 cells = 40960 WDist).  Callers modeling a
    projectile's random speed array may opt into reducing a comma list to its
    integer mean; scalar engine fields reject that syntax.
    """
    if isinstance(raw, dict):
        raw = raw.get("v")
    if isinstance(raw, (int, float)) and not isinstance(raw, bool):
        return parse_int32(raw, "WDist")
    text = str(raw).strip()
    if "," in text:
        if not allow_distribution:
            raise ValueError("scalar WDist cannot contain a distribution")
        parts = text.split(",")
        if any(not part.strip() for part in parts):
            raise ValueError("WDist distribution contains an empty value")
        values = [parse_wdist(part) for part in parts]
        return sum(values) // len(values)
    match = _WDIST_TEXT.fullmatch(text)
    if match is None:
        raise ValueError(f"invalid WDist: {raw!r}")
    first = parse_int32(match.group(1), "WDist component")
    remainder = match.group(2)
    if remainder is None:
        return first
    subcell = parse_int32(remainder, "WDist subcell component")
    if first < 0:
        subcell = -subcell
    return _unchecked_int32(1024 * first + subcell)


def wdist_value(raw, default=None):
    """Safe scalar WDist for ledger/report consumers.

    Keep generic numeric parsers for HP, cost, and cadence; ranges need this
    OpenRA-aware path so ``40c0`` cannot silently become zero in one consumer
    while the engine and the rest of the pipeline read 40960.
    """
    if raw is None or (not isinstance(raw, dict) and str(raw).strip() == ""):
        return default
    try:
        return parse_wdist(raw)
    except (TypeError, ValueError, OverflowError):
        return default


def burst_delay_values(raw) -> list[int] | None:
    """Parse the engine's integer BurstDelays field without truncation."""
    if isinstance(raw, dict):
        raw = raw.get("v")
    if raw is None or str(raw).strip() == "":
        return None
    parts = raw if isinstance(raw, (list, tuple)) else str(raw).split(",")
    values = []
    try:
        for part in parts:
            number = float(str(part).strip())
            if not math.isfinite(number) or not number.is_integer():
                return None
            value = int(number)
            if value < -(2 ** 31) or value > 2 ** 31 - 1:
                return None
            values.append(value)
    except (TypeError, ValueError):
        return None
    return values or None


def burst_delays_text(raw) -> str | None:
    """Canonical workbook/YAML text for a valid BurstDelays value."""
    values = burst_delay_values(raw)
    return None if values is None else ", ".join(str(value) for value in values)


def burst_delay_sum(burst: int = 1, burst_delays=None) -> float:
    """Total inter-shot delay in one engine burst.

    ``WeaponInfo.BurstDelays`` defaults to ``[5]``. One configured value is reused
    for every gap; a comma-separated list supplies exactly ``Burst - 1`` gaps.
    Keeping this parser here prevents callers from silently taking only the first
    value or treating a missing field as zero.
    """
    gaps = max(int(burst or 1) - 1, 0)
    if gaps == 0:
        return 0.0
    if burst_delays is None or str(burst_delays).strip() == "":
        values = [ENGINE_DEFAULT_BURST_DELAY]
    else:
        values = burst_delay_values(burst_delays) or [ENGINE_DEFAULT_BURST_DELAY]
    if not values:
        values = [ENGINE_DEFAULT_BURST_DELAY]
    if len(values) == 1:
        return values[0] * gaps
    # The engine rejects any list length other than Burst - 1. Returning the values
    # it would consume keeps diagnostics deterministic on a malformed fixture.
    return sum(values[:gaps])


def eff_reload(reload_delay: float, burst: int = 1, burst_delays=None) -> float:
    """Effective ticks per full burst cycle, including every inter-shot delay."""
    return reload_delay + burst_delay_sum(burst, burst_delays)


def dps(damage: float, reload_delay: float, burst: int = 1,
        burst_delays=None,
        firepower_multiplier: float = 1.0) -> float:
    """Burst-aware DPS. With burst=1 this is the legacy G/I*H exactly.

    firepower_multiplier is a legacy per-actor FirepowerMultiplier expressed as
    a factor (1.0 = 100%). It still affects actors that carry one, but the balance
    writer no longer creates or fine-tunes this retired knob.

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
#
# The NERF is the charge delay itself — it inflates the effective reload and leaves the
# unit helpless while it winds up. This multiplier is the COMPENSATION for that nerf,
# not more of it: a cheaper unit is BETTER per credit, so 0.75x is a buff in value terms
# that pays the unit back for a weakness DPS alone cannot see.
#
# Which is why it must be applied EXACTLY ONCE. Two mechanisms compensating the same
# delay would leave a charging unit over-paid and cost-efficient — the opposite of the
# intent. See FORMULA_V2 3b: the -0.25 negative-special route is retired for this reason.
CHARGE_UP_PRICE_MULTIPLIER = 0.75

# Traits that make an actor pay for a charge-up. `AttackCharges` is the Obelisk of
# Light — the case the ruling itself cites as the model — which does NOT use one of
# the three `*Charged` traits, so naming only those would have left the cited
# precedent unpriced.
CHARGE_UP_TRAITS = frozenset({
    "AttackCharged", "AttackTurretedCharged", "AttackFrontalCharged",
    "AttackCharges",
    # W16: `AttackTesla` JOINS the set. It was excluded while the discount was a
    # flat 0.75x, because a Tesla Coil charges for a fifth of its cycle and the
    # Obelisk for a third — paying both the same was the double-discount the
    # exclusion existed to prevent. Now that the discount is proportional to the
    # measured share, each earns only what its own wind-up costs it, so there is
    # nothing left to exclude.
    "AttackTesla",
})

# Retired by W16, kept as an empty set so older callers keep working. Membership is
# no longer a binary decision — see charge_price_multiplier.
CHARGE_UP_EXCLUDED_TRAITS = frozenset()

# Where each trait keeps its wind-up, with the ENGINE DEFAULT applied when the key is
# absent. An absent key means DEFAULT, never zero: the RA2 Tesla Coil wrote no
# `InitialChargeDelay` and an earlier draft of W16 read that as "no charge at all",
# when the engine gives it 22.
#
# ⚠ `AttackTesla`'s cycle is a BURST, and is measured with the burst law
# (`eff_reload`): the coil winds up once, then fires `MaxCharges` zaps `ChargeDelay`
# ticks apart, so a full cycle is `ReloadDelay + ChargeDelay x (MaxCharges - 1)` —
# the same shape as a weapon's `ReloadDelay + BurstDelays x (Burst - 1)`.
# Maintainer 2026-08-15: "it shoots 3 times but with a delay of 3 ticks between each
# damage tick so it could be handled like a burst delay in our formula."
# Ignoring the zaps understates a 3-charge coil's cycle by 6 ticks and hands it a
# discount it has not earned.
#
#   trait -> {charge: (field, default), rate/cycle/burst/burst_delay: (field, default)}
CHARGE_FIELDS = {
    "AttackTesla": {
        "charge": ("InitialChargeDelay", 22),
        # ⚠ `AttackTesla` REPLACES the weapon's reload with its own. Maintainer
        # 2026-08-15: "if you have the AttackTesla trait, ReloadDelay is taken from
        # that instead of from the weapon, and the reload delay from the weapon
        # counts as the burst delay". So the WEAPON's reload is the gap between
        # zaps, not `ChargeDelay` — those happen to both be 3 on the two Tesla
        # Coils, which is why an earlier draft using ChargeDelay produced the right
        # answer for them and the WRONG one for the AA railtower (weapon reload 10:
        # a 160-tick cycle, not 132).
        "cycle_reload": ("ReloadDelay", 120),
        "burst": ("MaxCharges", 1),
    },
    # The ChargeLevel family governs no reload of its own — the charge gates the
    # actor's own gun, so the share is measured against the weapon it delays.
    # ChargeLevel is a counter filled at ChargeRate per tick, hence the rate divisor.
    "AttackCharges":         {"charge": ("ChargeLevel", 25), "rate": ("ChargeRate", 1)},
    "AttackCharged":         {"charge": ("ChargeLevel", 25), "rate": ("ChargeRate", 1)},
    "AttackFrontalCharged":  {"charge": ("ChargeLevel", 25), "rate": ("ChargeRate", 1)},
    "AttackTurretedCharged": {"charge": ("ChargeLevel", 25), "rate": ("ChargeRate", 1)},
}


# W16 anchor, as a LAW rather than one building's stats (maintainer 2026-08-15,
# "some nice ratio ... might be more consistent"):
#
#   A unit that spends HALF AGAIN its reload winding up — charge = 50% of reload —
#   earns the full 0.75x discount. As a share of the whole cycle that is
#   0.5 / 1.5 = 1/3.
#
# The Obelisk of Light, the case the ruling was written for, sits at 50/(50+96) =
# 34.2%, so it still anchors at 0.75 (just above the line, clamped) and NOTHING
# moves: measured across the 11 chargers with a real share, spread 0.198 against
# today's 0.199.
#
# ⚠ A 25%-of-reload anchor (share 20%) was measured and REJECTED: it puts 7 of 11
# chargers on the 0.75 floor instead of 5, erasing most of the differentiation this
# item exists to create. Clean is good; clean and flat is not.
#
# Kept as a CONSTANT, never read from the Obelisk at runtime — a balance pass
# retuning that one building must not silently re-price every charging actor.
CHARGE_ANCHOR_SHARE = 1 / 3

# --------------------------------------------------------------------------- #
# Rational tech-tier curve (BALANCE_PIPELINE §7, tier_chain_validation.md)
# --------------------------------------------------------------------------- #
# B = T1 median prerequisite-building-chain cost.
# S = (T4 median chain cost) - B.
TIER_B = 9500.0
TIER_S = 8250.0


def tier_multiplier(C: float | None, B: float = TIER_B, S: float = TIER_S) -> float:
    """Rational tech-tier multiplier f(C) = 1 / (1 + (C - B) / S).

    C is the total Valued.Cost of an actor's prerequisite building chain.  The
    curve is clamped to [0, 1] and returns 1.0 for C <= B, giving a clean T1/T2
    plateau.  Approximate reference points:

        C =  9,500 (B)          -> 1.000   (T1)
        C = 11,600              -> 0.797   (T2)
        C = 15,000              -> 0.600   (T3)
        C = 17,750              -> 0.500   (T4)

    Callers that price with ``class_anchor_price`` should pass the ABSOLUTE
    multiplier f(C): the anchor's own multiplier cancels in the ratio.
    Callers that price with ``class_baseline_price`` should pass the RELATIVE
    multiplier f(C) / f(C_anchor), because ``cost0`` is the anchor's absolute
    price and the formula is not self-normalising.
    """
    if C is None:
        return 1.0
    if C <= B:
        return 1.0
    denom = 1.0 + (C - B) / S
    if denom <= 0.0:
        return 0.0
    return min(1.0, max(0.0, 1.0 / denom))


def charge_share(ticks: float | None, cycle: float | None) -> float:
    """Fraction of an attack cycle the actor spends winding up. 0.0 when unknown."""
    if not ticks or not cycle or ticks <= 0 or cycle <= 0:
        return 0.0
    return ticks / (ticks + cycle)


def charge_attack_cycle(charge, weapon_reload: float | None):
    """(cycle_ticks, shots_per_cycle) for a trait that OVERRIDES the weapon's reload.

    `AttackTesla` is the case: the trait's own `ReloadDelay` is the cycle, it fires
    `MaxCharges` zaps within it, and the WEAPON's reload is the gap between those
    zaps — i.e. exactly a burst, so `eff_reload` applies unchanged.

    Returns None for traits that do NOT override the weapon (the `ChargeLevel`
    family), whose gun keeps its own reload and whose charge merely delays it.

    ⚠ This is a PRICING correction, not a detail. A Tesla Coil's weapon reloads
    every 3 ticks, so reading the weapon alone prices it as firing 20 times a
    second when it really fires 3 zaps per 106 ticks — an 11.8x overstatement of
    its DPS, and DPS drives the price.
    """
    if not isinstance(charge, dict):
        return None
    reload_ = charge.get("cycle_reload")
    if not reload_:
        return None
    burst = int(charge.get("burst") or 1)
    return eff_reload(reload_, burst, weapon_reload), burst


def charge_price_multiplier(charge, reload_fallback: float | None = None) -> float:
    """Price discount for a charging actor, PROPORTIONAL to its real charge burden (W16).

    W4 applied a flat 0.75x to every charging actor, which is too blunt: the
    Obelisk spends 34% of its cycle charging while an Asian Alliance railtower
    spends 9%, and they were paying the same discount. The discount now scales
    linearly with `charge_share`, anchored so the Obelisk gets exactly 0.75 and a
    zero-charge actor gets exactly 1.0, clamped to [0.75, 1.0].

    That is also what lets `AttackTesla` finally join the discount: each actor now
    earns the discount its own charge burden justifies, so there is no longer a
    binary in/out decision to get wrong.

    Applied to the PRICE, not to DPS: price is degree 1/2/3 in its inputs
    (O/P/Q), so scaling DPS would not yield a clean multiplier on the result.

    `charge` is the ledger's `charge_up` record (dict) or, for older callers, the
    bare trait name. A trait with no measurable charge falls back to the flat rate
    rather than to 1.0 — it charges, we just cannot see by how much, and pricing it
    as if it did not charge would be the larger error.
    """
    if not charge:
        return 1.0

    if isinstance(charge, dict):
        trait = charge.get("v")
        ticks = charge.get("ticks")
        # A trait that overrides the weapon's reload supplies its own cycle, built
        # from the weapon reload as the burst delay; everything else measures the
        # wind-up against the gun it delays.
        own = charge_attack_cycle(charge, reload_fallback)
        cycle = own[0] if own else reload_fallback
    else:
        trait, ticks, cycle = charge, None, reload_fallback

    if not trait or str(trait).split("@", 1)[0] not in CHARGE_UP_TRAITS:
        return 1.0

    share = charge_share(ticks, cycle)
    if share <= 0.0:
        return CHARGE_UP_PRICE_MULTIPLIER

    scaled = 1.0 - (1.0 - CHARGE_UP_PRICE_MULTIPLIER) * (share / CHARGE_ANCHOR_SHARE)
    return min(1.0, max(CHARGE_UP_PRICE_MULTIPLIER, scaled))


# E2 — the physical-state (heat / cold / corrosion) meters, maintainer 2026-08-18:
# *"Cryo seems as strong as Fire IF it is able to completely freeze a unit BEFORE it dies
# ... Then they can be priced the same way with a 1.25x cost multiplier"*. The ruling is
# CONDITIONAL, so the constant is a ceiling reached only by full delivery, not a flat rate
# every flame weapon collects.
#
# The direction is the OPPOSITE of the charge-up discount above and the asymmetry is
# deliberate: a charge-up is a weakness DPS cannot see, so it pays the unit back; a status
# meter is a strength DPS cannot see, so it charges the unit more.
PHYSICAL_STATE_PRICE_MULTIPLIER = 1.25


def physical_state_price_multiplier(weight: float) -> float:
    """Price surcharge for a weapon that fills a status meter, scaled by DELIVERY.

    `weight` is the 0..1 delivery weight from `physical_state_price.delivery_weight`:
    exposure (does the target carry the meter at all?) x how much of the axis's effect the
    meter actually delivers before the target dies, measured against a weapon that exactly
    meets the maintainer's bar.

    Kept here rather than in `physical_state_price` so every price constant lives in one
    file — the charge-up multiplier next door is the precedent, and splitting them is how a
    second, contradicting rate gets introduced by accident.
    """
    if not weight or weight <= 0:
        return 1.0
    w = min(1.0, max(0.0, float(weight)))
    return 1.0 + (PHYSICAL_STATE_PRICE_MULTIPLIER - 1.0) * w


# Tagged twins used by the rebalance writer — NEVER main / NEVER in its flat
# damage total (the runtime percentage model discovers nodes by TYPE, not suffix):
#   *ExtraDamage   Tesla/Nuclear shield chip — ALWAYS excluded from damage
#   *FriendlyFire  own-side splash (50% twin)
#   *Percentage    standalone percentage twin, written in its node's own unit
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


# The small-arms families, for the `smallarms_only` pricing rule (FORMULA_V2 §3:
# a cheap scout is priced on its rifle, not on its grenade).
#
# ⛔ THIS USED TO TEST `tag.startswith("smallarms")` AND SILENTLY PRICED 15 OF 24
# SCOUTS AT ZERO DPS. The 3-way split renamed warhead tags to FAMILY names, so a
# rifle that was `SmallArmsWarhead` became `Bullet_Light`; only 120 of 7618 damage
# warheads still carry the legacy string. The filter therefore matched nothing for
# every unit under the 1.5x cost0 threshold, `spread_damage_sum` returned 0, and
# `propose_class_rebalance` priced those units at 32-63 against costs of 100-200.
# The data was always correct — reload 50, damage 4000, right there in the ledger.
#
# Match on the FAMILY, not on a literal that a migration can rename out from under
# it. `Bullet` is the post-split small-arms family; the legacy names are kept so
# the 120 unconverted warheads still price.
SMALLARMS_FAMILIES = frozenset({"smallarms", "bullet", "minigun", "rifle"})


def is_smallarms_tag(tag) -> bool:
    """Is this warhead tag a small-arms family, before or after the 3-way split?"""
    family = (tag or "").split("_")[0].lower()
    return any(family.startswith(f) for f in SMALLARMS_FAMILIES)


# ---------------------------------------------------------------------------
# ⛔ THE TWO CHANNELS: OFFENSIVE DAMAGE vs SUPPORT THROUGHPUT
# ---------------------------------------------------------------------------
# A NEGATIVE `Damage` HEALS. That is the engine's convention, not a Cameo one,
# and `spread_damage_sum` used to add it straight into the offensive total — so
# eight actors priced as if they SHOT BACKWARDS:
#
#     cabal_engineer -650   tkm_battlebus -600   futuretech_repairdroid -508
#     tkm_engineer   -397   ra1_allies_mechanic -357   terran_medic -183
#     ra1_allies_medic -40  ts_gdi_medic -40
#
# `support` and `line_breaker` could not be priced at all, and the two classes
# showed up as the only non-bell-shaped ones in `band_granularity.py`.
#
# ⛔ CLASSIFY BY THE SIGN, NEVER BY THE TAG NAME. The obvious fix is a tag
# whitelist — `HealingWeapon`, `RepairWeapon`, `ExtraHealing`, … — and it is the
# WRONG fix, for exactly the reason spelled out 20 lines above about
# `smallarms`: a literal is something a migration renames out from under you.
# Measured on this tree: of 160 negative warheads, **7 carry a generic tag**
# (`1Dam` on the five WC2 paladin/priest heals, `Percentage` on two Tesla
# charges). A name filter would have priced five healers as combat units. The
# sign cannot be renamed.
#
# ⭐ AND THE ARMAMENT IS THE RIGHT GRAIN. Measured: **0 of 2,561 armaments mix
# positive and negative warheads** — 58 armaments across 37 actors are purely
# supportive. So an armament is unambiguously one channel or the other, and a
# unit that both heals and shoots does it with two separate armaments.

def armament_channel(warheads, template_names=None) -> str:
    """``"offensive"`` | ``"support"`` | ``"empty"`` for one armament's warheads.

    Decided on the SIGN of the main damage warheads, at the ARMAMENT grain —
    see the block above for why both of those choices are load-bearing.
    """
    vals = []
    for w in main_spread_warheads(warheads, template_names):
        try:
            vals.append(float(w.get("damage")))
        except (TypeError, ValueError):
            continue
    if any(v > 0 for v in vals):
        # A mixed armament does not exist in this tree and would be a design
        # question, not a rounding one. Call it offensive and let the sums below
        # keep the channels clean rather than inventing a third category.
        return "offensive"
    if any(v < 0 for v in vals):
        return "support"
    return "empty"


def spread_damage_sum(warheads, smallarms_only: bool = False,
                      template_names=None) -> float:
    """OFFENSIVE effective per-shot damage = SUM of the MAIN damage warheads
    (maintainer law 2026-07-22; main = template-named SpreadDamage, see
    ``main_spread_warheads``). A multi-warhead weapon deals the ADDED damage
    of all its warheads to a target, so the SUM — never the max — is the price
    driver: pricing on the max would let a 10-warhead weapon deal 10x the
    damage for the price of one. `*ExtraDamage` / `*FriendlyFire` /
    `*Percentage` twins are excluded.

    ⛔ **HEALING IS EXCLUDED, AND THE RESULT IS NEVER NEGATIVE.** A negative
    `Damage` heals (see the block above); it belongs to
    ``support_throughput_sum``, not here. Before 2026-08-31 it was summed in
    and eight support actors priced as if they shot backwards. A pure healer
    now reads **0**, which is the truth about its OFFENSIVE output — ask
    ``support_throughput_sum`` for what it actually does.

    This is the ONE canonical warhead-damage reducer; every pricing tool MUST
    call it so the MAX convention can never creep back in. ``smallarms_only``
    restricts the sum to SmallArms warheads (cheap scouts <=150% of C0 price
    only their SmallArms warhead)."""
    total = 0.0
    for w in main_spread_warheads(warheads, template_names):
        if smallarms_only and not is_smallarms_tag(w.get("tag")):
            continue
        try:
            d = float(w.get("damage"))
        except (TypeError, ValueError):
            continue
        if d > 0:            # ⛔ healing is the other channel — never sum it here
            total += d
    return total


def support_throughput_sum(warheads, template_names=None) -> float:
    """SUPPORT throughput per shot = the MAGNITUDE of the healing/repair main
    warheads. **Non-negative by construction**, and 0 for a combat weapon.

    The mirror of ``spread_damage_sum``: together the two partition the main
    warheads by sign, so no warhead is counted twice and none is dropped. A
    class that prices non-combat members must declare which channel it consumes
    — `support` reads THIS one, every combat class reads the other."""
    total = 0.0
    for w in main_spread_warheads(warheads, template_names):
        try:
            d = float(w.get("damage"))
        except (TypeError, ValueError):
            continue
        if d < 0:
            total += -d
    return total


# ---------------------------------------------------------------------------
# THE STAT GRID REGISTRY — one table, with provenance, for every legal step.
# ---------------------------------------------------------------------------
# ⚠ WHY THIS EXISTS. The grids were written as literals in whichever function
# needed them, and three of them had silently drifted from the law by 2026-08-29:
#
#   * HP was quantised at 1000 for EVERY class. DESIGN.md is explicit that
#     vehicles/aircraft/ships step by 2500 and only infantry by 1000, so every
#     vehicle class was being nudged onto the wrong grid.
#   * The Speed step was chosen from a defined `Mobile.TurnSpeed`, which covers
#     vehicles (398 of 403) and ships (48 of 50) but reaches **0 of 168
#     aircraft** — so an aircraft would have been stepped by 1 against a law that
#     says 5. Latent only because no aircraft class exists yet (open item X6);
#     it would have gone live the moment one was added.
#   * `propose_class_rebalance` carried a class-level `spd_step` argument and a
#     `VEHICLE_TYPE_CLASSES = {"mbt"}` set that NOTHING READ — the per-row step
#     always won. A dead knob that looks like it enforces a law is worse than no
#     knob, because it answers the question "is this handled?" with a lie.
#
# The rule that would have caught all three: a step is a LAW, and a law lives in
# one place with a citation. Anything that quantises reads it from here.
#
# ⚠ AND THE KEY IS PER-STAT. Speed's step exists because turn rate is `speed/5`,
# so it follows LOCOMOTION (`speed_platform`). HP's step exists because self-heal
# is HP/2500 or HP/1000, so it follows the unit KIND (`hp_platform`). A FutureTech
# droid drives like a vehicle and heals like infantry, and takes one grid from
# each. Neither is "what class it is priced in" — a class is a pricing construct.

STAT_GRIDS = {
    # stat: {platform: (step, source)}
    "hp": {
        "infantry": (1000, "DESIGN.md 'HP: 2500-steps ... 1000-steps for infantry'"),
        "vehicle": (2500, "DESIGN.md 'HP: 2500-steps for vehicles/aircraft/ships'"),
    },
    "speed": {
        "infantry": (1, "FORMULA_V2.md 3 'Infantry: steps of 1'"),
        "vehicle": (5, "FORMULA_V2.md 3 'Vehicles, aircraft, AND ships: steps of 5"
                       " (turn rate = speed/5)'"),
    },
    "range": {"*": (10, "FORMULA_V2.md 3 'steps of 10'")},
    "damage": {"*": (100, "DESIGN.md grid table, DAMAGE_STEP (W15)")},
    "cost": {"*": (10, "DESIGN.md grid table, maintainer 2026-08-29"
                       " — NOT yet enforced in code (open item X2)")},
}

# Ledger sections whose members move on the vehicle grid. `naval` and `aircraft`
# are in here explicitly BECAUSE the turn-rate probe misses them: no aircraft in
# the tree defines one, and two ships do not either.
VEHICLE_SECTIONS = frozenset({"vehicles", "aircraft", "naval", "ships"})


def speed_platform(section=None, turn_speed=None):
    """Which SPEED grid this unit moves on.

    ⚠ Keyed on LOCOMOTION, because that is where the step comes from: turn rate
    is `speed/5`, so anything with a turn rate must sit on a multiple of 5. Two
    signals, since neither alone is complete — a defined turn rate catches the
    units that move as vehicles whatever section they are filed under (Cabal
    cyborgs and FutureTech droids use vehicle locomotion while sitting in
    `infantry`), and the section catches the rest.

    ⚠ **Aircraft keep their turn rate in the `Aircraft` trait, not `Mobile`**
    (maintainer 2026-08-29) — exactly as they keep `Speed` there. Reading only
    `Mobile.TurnSpeed` made all 168 aircraft in the ledger look like they had NO
    turn rate, and that is what made this probe miss every one of them. They have
    one: **323 actors carry an `Aircraft` trait and 318 define both Speed and
    TurnSpeed**. `extract_stats` now records it as `turn_speed_air`, and callers
    pass whichever is set. The earlier note here said aircraft define no turn
    rate; that was the extractor's blind spot, not the tree.
    """
    if turn_speed:
        return "vehicle"
    return "vehicle" if (section or "").lower() in VEHICLE_SECTIONS else "infantry"


# Classes whose HP moves on a grid their SECTION would not give them. The HP step
# is a design judgement about how finely a class's durability should be tunable,
# not a mechanical consequence of what the unit drives on — so it is the one grid
# a class may override.
#
# `scout_vehicle` -> infantry (maintainer 2026-08-29). ⚠ The tree does not agree
# yet: all 28 tagged scout vehicles sit on the 2500 grid today and SEVEN of them
# are not multiples of 1000 (`ra1_allies_ranger` and `forgotten_raidercar` 22500,
# `tkm_as42` / `tkm_technical` / `ts_gdi_pitbull` / `td_gdi_humvee` 27500,
# `td_gdi_humveemkii` 37500). The converter will move those seven onto the finer
# grid. Recorded here because a ruling the data contradicts must say so out loud.
HP_GRID_BY_CLASS = {
    "scout_vehicle": "infantry",
}


def hp_platform(section=None, class_anchor=None):
    """Which HP grid this unit moves on.

    ⚠ Keyed on the SECTION, NOT on locomotion, and the difference is not
    cosmetic. The HP step exists because of SELF-HEAL — DESIGN.md sets it beside
    "self-heal HP/2500" for vehicles/aircraft/ships and "self-heal HP/1000" for
    infantry. A FutureTech droid drives like a vehicle but heals like infantry,
    so it takes the 1000 grid while still taking the speed-5 grid above.

    Collapsing the two onto one notion of "platform" is a real error and it was
    measurable: it put `futuretech_scoutdroid` on the 2500 HP grid and pushed the
    `scout` class from worst |Δ| 22.8 to 32.1 on its own.

    ⚠ `HP_GRID_BY_CLASS` overrides the section. That is deliberate and it is the
    ONLY grid a class may override — HP is the one whose step is a judgement
    about tuning resolution rather than a mechanical consequence.

    ⚠ NOT derivable from the tree today. `ChangesHealth.Step` is the quantity the
    law is written against, and only **7 actors in the entire tree** define one,
    so self-heal cannot confirm or deny a class's grid. Until that is populated,
    these are design rulings, not measurements.
    """
    if class_anchor and class_anchor in HP_GRID_BY_CLASS:
        return HP_GRID_BY_CLASS[class_anchor]
    return "vehicle" if (section or "").lower() in VEHICLE_SECTIONS else "infantry"


# ⚠ THE TURN-RATE LAW DOES NOT LIVE HERE, AND MUST NOT BE RE-ADDED.
#
# `TurnSpeed` is enforced by `tools/audit/audit_stat_formulas.py` (F8 vehicles, F9
# turreted, F10 turretless, F17 fighters/bombers = Speed/15 with frontal 2x, F19
# helicopters/spaceships = Speed/5) and FIXED by `tools/balance/gen_derived_stats.py`,
# which parses that audit's own output so the checker and the fixer can never
# disagree. All five read **0 findings**: the roster already complies.
#
# A second copy was added here on 2026-08-30 and removed the same day. It was not
# just redundant, it was WRONG: it scoped by "has a Mobile or Aircraft trait"
# instead of by unit type plus template inheritance, so it applied the GROUND law
# to aircraft belonging to no air template and reported 340 violations against a
# roster with none. The real audit scopes `ut == "air"` AND
# `inherits_template(FighterTemplate|BomberTemplate|...)`.
#
# The lesson, for the third time in one session: GREP FOR THE MECHANISM, NOT JUST
# THE PHRASE. "TurnSpeed (aircraft)" found one sentence of a two-part law;
# "fighter" in tools/ would have found the whole thing already implemented.


def stat_step(stat, platform="infantry"):
    """The legal step for `stat` on `platform`. Raises on an unknown stat — a
    quantiser reaching for a grid that does not exist is a bug, not a default."""
    grids = STAT_GRIDS[stat]
    if "*" in grids:
        return grids["*"][0]
    return grids[platform][0]


# The flat-damage grid. 2000 until 2026-08-11, when the maintainer regridded it 20x finer
# (2000 -> 200 -> 100) alongside a percentage twin measured in BASIS POINTS (0.01%).
#
# The law is deliberately one sentence: **100 flat damage == 0.01% of max health**, so one
# step of the flat grid is exactly one step of the percentage grid and the twin can never
# drift from the weapon it belongs to.
DAMAGE_STEP = 100

# Flat damage per ONE WHOLE PERCENT of the twin. Raised 2000 -> 10000 in the same ruling:
# the twin's BASE percentage is now 5x smaller, and the percentage warheads' Versus values
# are 5x larger to compensate (1..17 became multiples of 5 in [5, 100]).
#
# ⚠ THE TWO HALVES ARE ONE CHANGE. Landing this ratio without the Versus x5 makes every
# percentage twin deal a FIFTH of its damage; landing the Versus x5 without this ratio
# makes it deal FIVE TIMES. Neither half is safe alone — see W18.
DAMAGE_PER_PERCENT = 10000

# What a Damage value on a percentage warhead MEANS, as a denominator.
#   100   = whole percent — stock HealthPercentageDamage, and AreaDamagePercentage's
#           default, so untouched weapons keep their current behaviour
#   10000 = basis points, 0.01% steps (AreaDamagePercentage, PercentageDenominator: 10000)
PERCENT_DENOMINATOR = 100
BASIS_POINT_DENOMINATOR = 10000

# Percentage-warhead Versus values are multiples of 5 in [5, 100] (the x5 rebase of the
# old 1..17 band). Which 17-step window a family uses is a W13 profile decision: 5..85
# reproduces today's balance exactly, 20..100 is the deliberately generalist band.
PERCENTAGE_VERSUS_STEP = 5
PERCENTAGE_VERSUS_BOUNDS = (5, 100)


def snap_damage_step(value: float, step: int = DAMAGE_STEP) -> int:
    """Nearest multiple of the flat-damage grid (DESIGN.md); a positive
    value never snaps below one step."""
    if value <= 0:
        return 0
    return max(step, int(round(value / step)) * step)


def percentage_twin(per: float, denominator: int = PERCENT_DENOMINATOR) -> int:
    """The `*Percentage` twin for a main Damage value, in the unit that node uses.

    The design ratio is `DAMAGE_PER_PERCENT`: one whole percent per 10000 flat damage,
    i.e. **0.01% for every 100 flat damage**. `denominator` says how the node WRITES
    that percentage:

        100   -> whole percent (stock HealthPercentageDamage — too coarse to hold the
                                new ratio; 16000 damage rounds to 2%)
        10000 -> basis points  (AreaDamagePercentage with PercentageDenominator: 10000;
                                16000 -> 160, i.e. 1.60%, exactly Damage/100)

    Passing the wrong denominator is a silent 10x error in either direction, which is
    why it is threaded from the resolved node rather than assumed.

    This used to be ``per // DAMAGE_STEP``, an integer division that made the twin a
    step function of Damage: 2000 -> 1, **1999 -> 0**, 3500 -> 1 (same as 2000). A
    twin of 0 is not "a little damage", it is HARD IMMUNITY — the percentage warhead
    silently does nothing — and it arrived purely from rounding, at exactly the Damage
    values a finer grid makes legal. Hence W15 lands before the grid moves.

    Rounding is half-UP and explicit rather than ``round()``, whose banker's rounding
    would send 5000 -> 2 while 7000 -> 4 (both .5 cases, rounded opposite ways). A live
    warhead never rounds below 1: the engine's Damage field is an integer, so one unit
    of the node's own denominator is the finest step it can express.
    """
    if per <= 0:
        return 0
    units_per_damage = denominator / (DAMAGE_PER_PERCENT * PERCENT_DENOMINATOR)
    return max(1, int(per * units_per_damage + 0.5))


def twin_denominator(warhead: dict) -> int:
    """The unit a percentage twin's Damage is written in, from the ledger record.

    Only `AreaDamagePercentage` can carry a `PercentageDenominator` — the stock
    `HealthPercentageDamage` has no such field and is always whole percent. An
    explicit value in the record wins; absence means the engine default.
    """
    value = warhead.get("percentage_denominator")
    if value in (None, ""):
        return PERCENT_DENOMINATOR
    try:
        value = int(value)
    except (TypeError, ValueError):
        return PERCENT_DENOMINATOR
    return value if value > 0 else PERCENT_DENOMINATOR


def distribute_damage(new_total, warheads, template_names=None) -> dict[str, int]:
    """Turn a design per-shot TOTAL (a spread_damage_sum) into per-warhead
    Damage values by the fixed law in DESIGN.md (``DAMAGE_STEP``):

      * EVERY main damage warhead (template-named SpreadDamage, see
        ``main_spread_warheads``) carries the IDENTICAL value
        D = new_total / N snapped to the 100-damage grid — they never
        differ ("all class warheads carry the identical value"). Tune on that
        grid, never by making warheads unequal or reintroducing actor FP.
      * each ``*FriendlyFire`` SpreadDamage twin = 50% of D (D // 2).
      * each ``*ExtraDamage`` SpreadDamage twin = 50% of D (D // 2): energy
        weapons trade area-of-effect for a shield/bonus chip, so it is
        ALWAYS half the main — yet still EXCLUDED from the damage total.
      * each ``*Percentage`` twin tracks 1 whole percent per 10000 flat Damage,
        written in the node's own denominator (16000 -> 2 whole-percent units,
        or 160 basis points), via ``percentage_twin`` — rounded, never floored
        to zero, so an off-grid D does not silently disable it.

    This is the ONE canonical way to write per-warhead Damage, so a single
    design number can NEVER again be broadcast identically onto every
    warhead (the 2026-07-22 over-damage bug). Returns {tag: new_int_damage}
    for every warhead the law assigns a value to.
    """
    warheads = warheads or []

    # ⛔ NEVER WRITE A DAMAGE TOTAL ONTO A SUPPORT ARMAMENT. `spread_damage_sum`
    # reads 0 for a healer (healing is the other channel), so a caller that
    # round-trips "read the total, redistribute it" would silently overwrite
    # `Damage: -2000` with 0 and DELETE the heal. That is a data-loss bug one
    # careless `apply_balance` away, and it is invisible in a diff of numbers.
    # Refuse loudly instead: a support armament is not priced through the damage
    # grid at all, so reaching here means the caller failed to filter.
    if armament_channel(warheads, template_names) == "support":
        raise ValueError(
            "distribute_damage called on a SUPPORT armament (healing/repair). "
            "Its output is support_throughput_sum, not spread_damage_sum; "
            "filter with armament_channel() before pricing.")

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
        elif low.endswith("percentage"):           # %-of-max-health twin
            # The node's own unit, never assumed: HealthPercentageDamage writes
            # whole percent, while AreaDamagePercentage may use basis points.
            result[tag] = percentage_twin(per, twin_denominator(w))
    return result


def estimators(hp: float, speed: float, range_wdist: float, dps_value: float,
               special: float = 1.0, unit_class: float = 1.0,
               tech_tier: float = 1.0) -> tuple[float, float, float]:
    """The legacy O/P/Q price estimators (recovered from the workbook
    cells 2026-07-18), on raw units (range in wdist).

    ``tech_tier`` is an ABSOLUTE multiplier (e.g. the raw f(C) from
    ``tier_multiplier``).  It is appropriate for ``class_anchor_price`` and the
    global ``price()``/``estimators()`` form, where the anchor's own multiplier
    cancels or where no anchor is used.  For ``class_baseline_price`` callers
    must first divide by the anchor's f(C_anchor) and pass the RELATIVE value.
    """
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
    normalized deviation from the class anchor. Exact at the anchor.

    ``o, p, q`` are normally produced by ``estimators()``, which accepts an
    ABSOLUTE ``tech_tier`` multiplier.  The anchor's own multiplier appears in
    both the numerator (unit O/P/Q) and the denominator (o0/p0/q0), so it
    cancels and the price is invariant to the anchor's tier.
    """
    return cost0 * (o / o0 + p / p0 + q / q0) / 3


def class_baseline_estimators(hp, speed, range_wdist, dps_value,
                              hp0, speed0, range0_wdist, dps0, cost0,
                              special=1.0, tech_tier=1.0) -> tuple[float, float, float]:
    """Formula v2 FINAL form (maintainer rule 2026-07-18): per-stat
    normalization against the class baseline unit, so that at the
    baseline O = P = Q = cost0 EXACTLY — the rule that must always hold
    for any baseline unit. The global Tiger formula is precisely this
    construction with (100000, 100, 5000, 200, 800) plugged in.

    ``tech_tier`` must be RELATIVE to the anchor: f(C_unit) / f(C_anchor).
    The anchor's price is ``cost0``, an absolute value, so scaling by the
    anchor's own f(C_anchor) would under- or over-price the baseline.  Use
    ``tier_multiplier`` for the unit and divide by the anchor's multiplier.
    """
    # ⚠ A MISSING BASELINE IS NOT A ZERO BASELINE. A class whose spec omits an axis
    # (`support` carries neither range0_wdist nor dps0 — its members are non-combat)
    # reached here and took the whole proposal down with a bare ZeroDivisionError.
    # Treat a missing baseline as "this axis does not price this class": the ratio is 1,
    # which leaves the term neutral in every degree instead of crashing.
    h = (hp / hp0) if hp0 else 1.0
    s = (speed / speed0) if speed0 else 1.0
    # A class whose spec carries no range baseline (`support`) reached here with
    # range0_wdist == 0 and crashed the whole proposal with a bare ZeroDivisionError.
    # Treat a missing baseline as "range is not a pricing axis for this class": r = 1
    # leaves the term neutral instead of taking the run down.
    r = ((range_wdist / range0_wdist) if range0_wdist else 1.0) * special
    d = (dps_value / dps0) if dps0 else 1.0
    o = (h + s + r + d) * cost0 / 4 * tech_tier
    p = ((h * s) + (r * d)) * cost0 / 2 * tech_tier
    q = (h * s * r * d) * cost0 * tech_tier
    return o, p, q


def class_baseline_price(hp, speed, range_wdist, dps_value,
                         hp0, speed0, range0_wdist, dps0, cost0,
                         special=1.0, tech_tier=1.0) -> float:
    """Class-baseline unit price.  ``tech_tier`` is RELATIVE: f(C)/f(C_anchor).

    Pass 1.0 for the anchor itself (or any unit whose chain cost equals the
    anchor's).  A unit with a higher building chain receives a multiplier < 1.0.
    """
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

    ``tech_tier`` is the RELATIVE tier multiplier f(C_unit) / f(C_anchor).

        h = hp / hp0 ; r = (range / range0) * special ; d = dps / dps0
        O = (h + r + d) / 3           * cost0   # degree 1: mean of the singles
        P = (h*r + h*d + r*d) / 3     * cost0   # degree 2: mean of the pairs
        Q = (h * r * d)              * cost0    # degree 3: the triple product

    At the baseline (h=r=d=1): O = P = Q = cost0 and price = cost0.
    Price is still LINEAR in r (h, d constant), so solve_range stays closed-form.
    """
    # ⚠ A MISSING BASELINE IS NOT A ZERO BASELINE. A class whose spec omits an axis
    # (`support` carries neither range0_wdist nor dps0 — its members are non-combat)
    # reached here and took the whole proposal down with a bare ZeroDivisionError.
    # Treat a missing baseline as "this axis does not price this class": the ratio is 1,
    # which leaves the term neutral in every degree instead of crashing.
    h = (hp / hp0) if hp0 else 1.0
    # A class whose spec carries no range baseline (`support`) reached here with
    # range0_wdist == 0 and crashed the whole proposal with a bare ZeroDivisionError.
    # Treat a missing baseline as "range is not a pricing axis for this class": r = 1
    # leaves the term neutral instead of taking the run down.
    r = ((range_wdist / range0_wdist) if range0_wdist else 1.0) * special
    d = (dps_value / dps0) if dps0 else 1.0
    o = (h + r + d) / 3 * cost0 * tech_tier
    p = (h * r + h * d + r * d) / 3 * cost0 * tech_tier
    q = (h * r * d) * cost0 * tech_tier
    return o, p, q


def class_baseline_price_3(hp, range_wdist, dps_value,
                           hp0, range0_wdist, dps0, cost0,
                           special=1.0, tech_tier=1.0) -> float:
    """3-input (static) class-baseline price.  ``tech_tier`` is RELATIVE."""
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
    # ⚠ A MISSING BASELINE IS NOT A ZERO BASELINE. A class whose spec omits an axis
    # (`support` carries neither range0_wdist nor dps0 — its members are non-combat)
    # reached here and took the whole proposal down with a bare ZeroDivisionError.
    # Treat a missing baseline as "this axis does not price this class": the ratio is 1,
    # which leaves the term neutral in every degree instead of crashing.
    h = (hp / hp0) if hp0 else 1.0
    d = (dps_value / dps0) if dps0 else 1.0
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
    # ⚠ A MISSING BASELINE IS NOT A ZERO BASELINE. A class whose spec omits an axis
    # (`support` carries neither range0_wdist nor dps0 — its members are non-combat)
    # reached here and took the whole proposal down with a bare ZeroDivisionError.
    # Treat a missing baseline as "this axis does not price this class": the ratio is 1,
    # which leaves the term neutral in every degree instead of crashing.
    h = (hp / hp0) if hp0 else 1.0
    s = (speed / speed0) if speed0 else 1.0
    d = (dps_value / dps0) if dps0 else 1.0
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

    # DESIGN.md law: mains identical on the damage grid, FF=50%, ExtraDamage
    # =50% (excluded from total). total 10000 / 5 mains -> 2000.
    whs = [wh("A", 22000), wh("Aextradamage", 22000, "SpreadDamage"),
           wh("Apercentage", 1, "HealthPercentageDamage"),
           wh("B", 22000), wh("C", 22000), wh("D", 22000),
           wh("Dfriendlyfire", 22000), wh("E", 22000)]
    r = distribute_damage(10000, whs)
    assert set(mains(r).values()) == {2000}, r          # identical mains
    assert sum(mains(r).values()) == 10000, r
    assert r["Dfriendlyfire"] == 1000, r                # FF = 50%
    assert r["Aextradamage"] == 1000, r                 # ExtraDamage = 50%
    # ExtraDamage carries a value (50%) but is EXCLUDED from the total
    assert spread_damage_sum(whs) == 22000 * 5          # Aextradamage not summed

    # THE percentage law: 100 flat damage == 0.01% of max health, exactly Damage/100.
    r = distribute_damage(16000, [wh("m", 4000),
                                  {"tag": "mpercentage", "damage": "1",
                                   "type": "AreaDamagePercentage",
                                   "percentage_denominator": BASIS_POINT_DENOMINATOR}])
    assert r["m"] == 16000 and r["mpercentage"] == 160, r          # 1.60%
    assert percentage_twin(DAMAGE_STEP, BASIS_POINT_DENOMINATOR) == 1

    # W15: the twin is continuous in Damage — never floored to a silent 0, and it
    # keeps tracking Damage between grid points (the old // gave 1999->0, 3500->1).
    assert percentage_twin(1999, BASIS_POINT_DENOMINATOR) == 20
    assert percentage_twin(1, BASIS_POINT_DENOMINATOR) == 1
    assert percentage_twin(0, BASIS_POINT_DENOMINATOR) == 0   # no main damage, no twin
    assert all(percentage_twin(d, BASIS_POINT_DENOMINATOR)
               >= percentage_twin(d - 100, BASIS_POINT_DENOMINATOR)
               for d in range(100, 40000, 100))         # monotone

    # Whole percent can no longer HOLD the ratio — 1.60% rounds to 2%. That coarseness
    # is exactly why W18 migrates the stock nodes to the basis-point warhead.
    assert percentage_twin(16000) == 2
    assert percentage_twin(1) == 1                      # but still never a silent zero

    # Off-grid total snaps to the step; mains stay identical.
    r = distribute_damage(9000, [wh("a", 2000), wh("b", 2000), wh("c", 2000)])
    assert len(set(r.values())) == 1, r                 # identical
    assert all(v % DAMAGE_STEP == 0 for v in r.values()), r
    # 9000/3 = 3000 lands EXACTLY on the 100 grid. The old 2000 grid had to snap it to
    # 4000 and hand a 33% remainder to FirepowerMultiplier — that is what finer buys.
    assert set(r.values()) == {3000}, r

    # Tiger anchor (DESIGN §12) still holds
    assert round(price(100000, 100, 5000, dps(10000, 50))) == 800

    print("formula self-test OK")


if __name__ == "__main__":
    _selftest()
