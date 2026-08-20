#!/usr/bin/env python3
"""physical_state_price.py — E2: price the heat / cold / corrosion meters.

Maintainer ruling 2026-08-18: *"Cryo seems as strong as Fire IF it is able to completely
freeze a unit BEFORE it dies ... they can reach their full effect before a unit dies at
around 25% HP left ... Then they can be priced the same way with a 1.25x cost multiplier"*
— i.e. the 1.25x is CONDITIONAL on delivery, and `PHYSICAL_STATE_SYSTEM.md` sharpens it to
*"price a partial meter partially"*.

Delivery has THREE independent factors, all measured from the resolved tree, never assumed:

  1. EXPOSURE   — does the target even carry the meter?  `Temperature` is on 98.9% of the
                  roster, `Corrosion` on 45.0%.  A corrosion weapon does nothing at all to
                  the other 55%, and nothing in the price model saw that before.
  2. RACE       — how full is the meter by the time the target dies?
  3. EFFECT     — how much of the axis's maximum effect a PARTIALLY full meter delivers.
                  This is NOT linear and it is NOT the same for the two meters: `Corroding`
                  gates every corrosion effect at HALF the bar, while `Overheating` opens at
                  1% of it.

⛔ THE RACE FORMULA IN THE DOCS WAS WRONG BY 2-4x AND THIS MODULE IS THE CORRECTION.
`PhysicalState.ApplyChange` health-scales through `ScaleChangeToHealth`, which is

    (long)amount * range / health.MaxHP        where  range = MaxValue - MinValue

so the divisor is the meter's RANGE, not the 10000 that `PhysicalStateInfo`'s own [Desc]
advertises ("divided by HP/10000"). That stale Desc is exactly what the old derivation
trusted. Because `Temperature` is SIGNED (-20000..20000) its range is 40000 — DOUBLE
`Corrosion`'s 20000 — so the two meters do not even share a formula:

    ratio = MaxValue * 100 / (Scale * range)        damage-scaled  -> Temperature  50/Scale
                                                                      Corrosion   100/Scale
    ratio = MaxValue * damage / (Amount * range)    discrete apply -> Temperature  D/(2A)
                                                                      Corrosion   D/A

⭐ Target MaxHP and weapon damage BOTH cancel in the damage-scaled form — the race is a
property of the CONSTANT alone, which is why one constant moved every weapon at once.

Relaxation between shots is deliberately NOT in the priced ratio: `RelaxationDelay: 25`
means a weapon reloading faster than 25 ticks loses nothing, and the linear term reintroduces
a MaxHP dependence that would destroy the cancellation above. `--report` measures the
sensitivity separately so the omission is bounded rather than hidden.
"""

from __future__ import annotations

import argparse
import pathlib
import sys

sys.path[:0] = [str(pathlib.Path(__file__).resolve().parents[1] / "audit")]

from cameo_model import Model  # noqa: E402


# The maintainer's bar: the meter is full while the target still has 25% of its life left.
FULL_EFFECT_BAR = 0.75

# Integration resolution for the delivery integral. 2000 steps puts the discretisation
# error below 0.1% of a delivery weight, well under any decision it feeds.
_STEPS = 2000


# --------------------------------------------------------------------------- #
# 1. meter geometry — measured, because `range` is what the engine divides by
# --------------------------------------------------------------------------- #

def meter_geometry(rs) -> dict[str, dict]:
    """{meter: {min, max, range, relative, actors}} over every actor carrying the meter.

    Reads the RESOLVED actors rather than defaults.yaml so an actor that overrides a bound
    shows up as a second shape instead of being silently averaged away.
    """
    shapes: dict[str, dict[tuple, int]] = {}
    for name in rs.actors:
        if name.startswith("^"):
            continue
        node = rs.resolve(name)
        if node is None:
            continue
        for c in node.children:
            if not (c.key == "PhysicalState" or c.key.startswith("PhysicalState@")):
                continue
            meter = (c.get("Name") or "").strip()
            if not meter:
                continue
            mx = _int(c.get("MaxValue"), 100)
            mn = _int(c.get("MinValue"), 0)
            rel = (c.get("RelativeToHealth") or "false").strip().lower() == "true"
            shapes.setdefault(meter, {})
            key = (mn, mx, rel)
            shapes[meter][key] = shapes[meter].get(key, 0) + 1

    out = {}
    for meter, counts in shapes.items():
        (mn, mx, rel), n = max(counts.items(), key=lambda kv: kv[1])
        out[meter] = {"min": mn, "max": mx, "range": mx - mn, "relative": rel,
                      "actors": sum(counts.values()), "shapes": len(counts),
                      "dominant_actors": n}
    return out


def _int(v, default=0) -> int:
    try:
        return int(str(v).strip())
    except (TypeError, ValueError):
        return default


def fill_ratio(kind: str, magnitude: float, damage: float, geom: dict,
               fed_share: float = 1.0) -> float | None:
    """hits_to_fill / hits_to_kill. <= 0.75 means the meter is full while 25% HP remains.

    `kind` is 'scaled' (PhysicalStateScale / a blend's PhysicalStates entry) or 'apply'
    (a discrete ApplyPhysicalState Amount). Returns None when the binding cannot fill.

    ⛔ `fed_share` IS THE TERM THAT MAKES THIS HONEST (W24, maintainer playtest 2026-08-19).
    A 'scaled' binding fills from ONE warhead's damage but the target dies to ALL of them:

        meter/shot = fed x (Scale/100) x range / MaxHP        hits_to_fill = MaxValue / that
        HP/shot    = total                                    hits_to_kill = MaxHP / total

        ratio = MaxValue x 100 / (Scale x range) x (total / fed) = 50/Scale / fed_share

    MaxHP still cancels; weapon damage NO LONGER DOES. The old form silently assumed
    fed == total, i.e. ONE damage warhead — true for 41 of 427 damage-scaled weapons. The
    maintainer found it by playtest: a Chemical Stealth Tank kills a harvester on Shrapnel +
    Missile + Chemical but fills the bar on Chemical alone, so the bar never finishes.
    `apply` is exempt: a flat `Amount` lands per HIT, whatever the damage split is.
    """
    if magnitude <= 0 or geom["range"] <= 0:
        return None
    top = max(abs(geom["max"]), abs(geom["min"]))
    if kind == "scaled":
        if fed_share <= 0:
            return None
        return top * 100.0 / (magnitude * geom["range"] * fed_share)
    if damage <= 0:
        return None
    return top * damage / (magnitude * geom["range"])


# --------------------------------------------------------------------------- #
# 2. effect curve — how much of the axis a PARTIALLY full meter actually delivers
# --------------------------------------------------------------------------- #

# The three consumer traits that turn a meter into a stat change. Cosmetics
# (WithPhysicalStateColoredOverlay / WithIdleOverlay) are excluded on purpose: they are
# feedback, not effect, and counting them would let a tint buy a price multiplier.
EFFECT_TRAITS = ("ChangesHealthProportionalToPhysicalState",
                 "SlowsProportionalToPhysicalState",
                 "DamageMultiplierProportionalToPhysicalState")


def effect_curve(rs, template: str, meter: str, positive: bool):
    """Return (curve, consumers) where curve(x) is the share of the axis's MAXIMUM effect
    delivered at fill fraction x, averaged over that axis's consumer traits.

    Two engine behaviours make this non-obvious, and both are load-bearing:

    * `ChangesHealthProportionalToPhysicalState` normalises over the FULL signed range
      (`(v - MinValue) / range`) and has no `UseDeviationFromRelaxed` option. On the signed
      Temperature meter that puts its floor at v=0, so the burn DoT opens at **half
      strength** the instant `Overheating` is granted. On Corrosion (MinValue 0) it is honest.
    * every consumer is gated by a `GrantConditionOnPhysicalState` window, and those windows
      differ by an order of magnitude: `Overheating` opens at 1% of the bar, `Corroding` at 50%.
    """
    node = rs.resolve(template)
    if node is None:
        return (lambda x: 0.0), []

    conditions = {}
    for c in node.children:
        if c.key.split("@")[0] == "GrantConditionOnPhysicalState":
            if (c.get("PhysicalStateName") or "").strip() != meter:
                continue
            cond = (c.get("Condition") or "").strip()
            if cond:
                conditions[cond] = (_int(c.get("LowerValue")), _int(c.get("UpperValue")))

    geom = meter_geometry(rs).get(meter)
    if geom is None:
        return (lambda x: 0.0), []
    top = max(abs(geom["max"]), abs(geom["min"]))
    sign = 1 if positive else -1

    consumers = []
    for c in node.children:
        base = c.key.split("@")[0]
        if base not in EFFECT_TRAITS:
            continue
        if (c.get("PhysicalStateName") or "").strip() != meter:
            continue
        if base == "SlowsProportionalToPhysicalState":
            only_neg = (c.get("OnlyNegativeValues") or "false").strip().lower() == "true"
            only_pos = (c.get("OnlyPositiveValues") or "false").strip().lower() == "true"
            if (only_neg and positive) or (only_pos and not positive):
                continue

        gate = _gate_fraction(c.get("RequiresCondition"), conditions, top, sign)
        if gate is None:                      # gated by a condition this sign never grants
            continue
        if gate >= 0.999:
            # A consumer that needs a COMPLETELY full meter (`superhot`, `CorrosionMax`) is a
            # bonus ON TOP of full delivery, not half of the axis. Averaging it in as an equal
            # member would halve every partial score for an axis that happens to have one, and
            # `superhot` is 1% of max HP per 25 ticks against `Overheating`'s 150 per tick —
            # a rounding error deciding the shape of the curve.
            continue

        if base == "ChangesHealthProportionalToPhysicalState":
            # share of DamageAtMaximum at fill x, using the engine's own normalisation
            lo = float(_int(c.get("DamageAtMinimum")))
            hi = float(_int(c.get("DamageAtMaximum"), 10))
            span = geom["range"]
            base_off = (0 - geom["min"]) / span if span else 0.0

            def share(x, lo=lo, hi=hi, base_off=base_off, sign=sign, top=top, span=span):
                norm = base_off + sign * x * top / span
                val = lo + (hi - lo) * norm
                peak = lo + (hi - lo) * (base_off + sign * top / span)
                return 0.0 if peak == 0 else max(0.0, min(1.0, val / peak))
        else:
            # Slows / DamageMultiplier interpolate on |deviation from relaxed| / max
            def share(x):
                return x

        consumers.append((c.key, gate, share))

    if not consumers:
        return (lambda x: 0.0), []

    def curve(x: float) -> float:
        return sum(0.0 if x < gate else sh(x) for _, gate, sh in consumers) / len(consumers)

    return curve, [(k, g) for k, g, _ in consumers]


def _gate_fraction(requires: str | None, conditions: dict, top: int, sign: int):
    """Lowest fill fraction at which every condition in `requires` can hold.

    `!cond` is satisfied by being OUTSIDE the window, so a negated deadzone imposes the
    deadzone's own edge as the gate. An unknown token (`hazmatsuits`) is a target property,
    not a meter threshold, and is ignored.
    """
    if not requires:
        return 0.0
    gate = 0.0
    for token in requires.replace("&&", " ").replace("||", " ").split():
        negated = token.startswith("!")
        name = token.lstrip("!")
        if name not in conditions:
            continue
        lower, upper = conditions[name]
        if negated:
            # outside [lower, upper]: for our sign that means past the near edge
            edge = upper if sign > 0 else lower
            gate = max(gate, abs(edge) / top)
            continue
        # inside [lower, upper]: unreachable if the window is on the other side of relaxed
        if sign > 0 and upper <= 0:
            return None
        if sign < 0 and lower >= 0:
            return None
        near = lower if sign > 0 else upper
        gate = max(gate, abs(near) / top)
    return gate


def delivery(ratio: float | None, curve) -> float:
    """Mean effect share over the target's remaining life.

    The meter is at fill min(1, u/ratio) when the target has taken share `u` of its lethal
    damage, so delivery is the average of `curve` along that path. A weapon that fills the
    meter instantly scores 1.0; one that never fills still scores whatever its ramp delivered.
    """
    if ratio is None or ratio <= 0:
        return 1.0 if ratio == 0 else 0.0
    total = 0.0
    for i in range(_STEPS):
        u = (i + 0.5) / _STEPS
        total += curve(min(1.0, u / ratio))
    return total / _STEPS


# --------------------------------------------------------------------------- #
# 3. exposure + the price multiplier
# --------------------------------------------------------------------------- #

def exposure(rs) -> dict[str, float]:
    """Share of PRICED actors (Health + Valued) that carry each meter.

    A weapon cannot deliver a meter the target does not have, and the two meters are not
    equally present: this is the term that separates Flame from Chemical.
    """
    priced, carriers = 0, {}
    for name in rs.actors:
        if name.startswith("^"):
            continue
        node = rs.resolve(name)
        if node is None:
            continue
        keys = {c.key for c in node.children}
        if "Health" not in keys or "Valued" not in keys:
            continue
        priced += 1
        for c in node.children:
            if c.key == "PhysicalState" or c.key.startswith("PhysicalState@"):
                meter = (c.get("Name") or "").strip()
                if meter:
                    carriers[meter] = carriers.get(meter, 0) + 1
    return {k: v / priced for k, v in carriers.items()} if priced else {}


def delivery_weight(ratio: float | None, curve, meter_exposure: float,
                    reference: float) -> float:
    """The 0..1 weight the 1.25x is scaled by: exposure x delivery, against the reference.

    `reference` is the delivery of a weapon that exactly meets the maintainer's bar on a
    fully-exposed meter — so a weapon that meets the bar scores 1.0 and pays the full 1.25x.
    Clamped at 1.0: a weapon that fills the meter FASTER than the bar is not charged more
    than the ruling allows.
    """
    if reference <= 0:
        return 0.0
    return max(0.0, min(1.0, meter_exposure * delivery(ratio, curve) / reference))


# --------------------------------------------------------------------------- #
# 4. the weapon scan
# --------------------------------------------------------------------------- #

# Which curve a binding is read against. The SIGN of the constant picks it: a negative
# `PhysicalStateScale` / `Amount` cools (Cryo), a positive one heats (Flame).
AXIS_TEMPLATE = {"Temperature": "^CryoFreezable", "Corrosion": "^Corrodible"}


def fired_weapons(rs) -> set[str]:
    """Weapons an actor actually mounts. A template nobody fires delivers nothing."""
    out = set()
    for name in rs.actors:
        if name.startswith("^"):
            continue
        node = rs.resolve(name)
        if node is None:
            continue
        for c in node.children:
            if c.key == "Armament" or c.key.startswith("Armament@"):
                w = (c.get("Weapon") or "").strip()
                if w:
                    out.add(w)
    return out


def weapon_bindings(rs, weapon: str) -> tuple[float, list[tuple[str, str, float]]]:
    """(total flat damage, [(meter, kind, signed magnitude)]) for one resolved weapon.

    ⚠ Enumerates all THREE binding shapes. Keying on `PhysicalStateName` alone is how the
    E2 census read 89 and then 367 instead of 372: a BLEND writes a `PhysicalStates:` dict
    with no name field at all.
    """
    node = rs.resolve_weapon(weapon)
    if node is None:
        return 0.0, []
    damage, bindings = 0.0, []
    for wh in node.children:
        if not wh.key.startswith("Warhead"):
            continue
        rel = (wh.get("ValidRelationships") or "").strip()
        if "Ally" in rel and "Enemy" not in rel:      # friendly-fire twin
            continue
        d = _float(wh.get("Damage"))
        if d > 0 and "Percentage" not in wh.key:
            damage += d
        name = (wh.get("PhysicalStateName") or "").strip()
        if name:
            if (wh.value or "").strip() == "ApplyPhysicalState":
                bindings.append((name, "apply", _float(wh.get("Amount"))))
            else:
                bindings.append((name, "scaled", _float(wh.get("PhysicalStateScale"))))
        multi = next((x for x in wh.children if x.key == "PhysicalStates"), None)
        if multi is not None:
            for s in multi.children:
                bindings.append((s.key.strip(), "scaled", _float(s.value)))
    return damage, [b for b in bindings if b[0] in AXIS_TEMPLATE and b[2] != 0]


def _float(v, default=0.0) -> float:
    try:
        return float(str(v).strip())
    except (TypeError, ValueError):
        return default


def damage_split(rs, weapon: str) -> tuple[float, float]:
    """(total main damage, damage carried by warheads that ALSO feed a meter).

    Friendly-fire twins and `Percentage` warheads are excluded on both sides, matching
    `weapon_bindings`. The ratio of the two is `fed_share` — see `fill_ratio`.
    """
    node = rs.resolve_weapon(weapon)
    if node is None:
        return 0.0, 0.0
    total = fed = 0.0
    for wh in node.children:
        if not wh.key.startswith("Warhead"):
            continue
        rel = (wh.get("ValidRelationships") or "").strip()
        if "Ally" in rel and "Enemy" not in rel:
            continue
        d = _float(wh.get("Damage"))
        if not (d > 0 and "Percentage" not in wh.key):
            continue
        total += d
        named = (wh.get("PhysicalStateName") or "").strip()
        scaled = (named and (wh.value or "").strip() != "ApplyPhysicalState")             or any(x.key == "PhysicalStates" for x in wh.children)
        if scaled:
            fed += d
    return total, fed


def scan(rs) -> list[dict]:
    """One record per (weapon, meter, mechanism), with the strongest magnitude of each."""
    geom = meter_geometry(rs)
    exp = exposure(rs)
    curves = {}
    for meter, template in AXIS_TEMPLATE.items():
        for positive in (True, False):
            curves[(meter, positive)] = effect_curve(rs, template, meter, positive)[0]
    reference = delivery(FULL_EFFECT_BAR, curves[("Temperature", False)])

    out = []
    for weapon in sorted(fired_weapons(rs)):
        damage, bindings = weapon_bindings(rs, weapon)
        total_dmg, fed_dmg = damage_split(rs, weapon)
        share = (fed_dmg / total_dmg) if total_dmg > 0 else 1.0
        strongest: dict[tuple, float] = {}
        for meter, kind, mag in bindings:
            key = (meter, kind, mag > 0)
            if abs(mag) > abs(strongest.get(key, 0.0)):
                strongest[key] = mag
        for (meter, kind, positive), mag in strongest.items():
            if meter not in geom:
                continue
            ratio = fill_ratio(kind, abs(mag), damage, geom[meter],
                               fed_share=share if kind == "scaled" else 1.0)
            curve = curves[(meter, positive)]
            weight = delivery_weight(ratio, curve, exp.get(meter, 0.0), reference)
            out.append({"weapon": weapon, "meter": meter, "kind": kind,
                        "magnitude": mag, "damage": damage, "ratio": ratio,
                        "fed_share": share if kind == "scaled" else 1.0,
                        "delivery": delivery(ratio, curve), "weight": weight,
                        "multiplier": 1.0 + 0.25 * weight,
                        "axis": ("heat" if positive else "cryo") if meter == "Temperature"
                                else "corrosion"})
    return out, reference, geom, exp, curves


def actor_multipliers(rs, rows=None) -> list[dict]:
    """Per-actor surcharge: the STRONGEST delivering armament, applied once.

    Once per actor, like the charge-up discount next to it in `formula.py` — an actor with a
    flamethrower and a machine gun is priced for carrying a flamethrower, not for carrying two
    guns. Taking the max rather than the sum is what keeps a multi-armament unit from paying
    the surcharge twice for one capability.
    """
    if rows is None:
        rows = scan(rs)[0]
    by_weapon: dict[str, dict] = {}
    for r in rows:
        cur = by_weapon.get(r["weapon"])
        if cur is None or r["weight"] > cur["weight"]:
            by_weapon[r["weapon"]] = r

    out = []
    for name in sorted(rs.actors):
        if name.startswith("^"):
            continue
        node = rs.resolve(name)
        if node is None:
            continue
        keys = {c.key for c in node.children}
        if "Health" not in keys or "Valued" not in keys:
            continue
        best = None
        for c in node.children:
            if c.key != "Armament" and not c.key.startswith("Armament@"):
                continue
            w = (c.get("Weapon") or "").strip()
            r = by_weapon.get(w)
            if r and (best is None or r["weight"] > best["weight"]):
                best = r
        if best is None or best["weight"] <= 0:
            continue
        cost = _float(next((c.get("Cost") for c in node.children if c.key == "Valued"), 0))
        out.append({"actor": name, "weapon": best["weapon"], "axis": best["axis"],
                    "weight": best["weight"], "multiplier": best["multiplier"],
                    "cost": cost, "delta": cost * (best["multiplier"] - 1.0)})
    return out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--report", action="store_true", help="write the markdown report")
    ap.add_argument("--actors", action="store_true",
                    help="per-actor surcharge — what adopting this would actually move")
    args = ap.parse_args()

    rs = Model().rs
    rows, reference, geom, exp, curves = scan(rs)

    if args.actors:
        actors = actor_multipliers(rs, rows)
        priced = [a for a in actors if a["cost"] > 0]
        total = sum(a["cost"] for a in priced)
        delta = sum(a["delta"] for a in priced)
        print("# Physical-state surcharge, per actor\n")
        print(f"- actors carrying a delivering meter weapon: **{len(actors)}** "
              f"({len(priced)} with a cost)")
        print(f"- combined cost today: **${total:,.0f}**")
        print(f"- combined cost with the surcharge: **${total + delta:,.0f}** "
              f"(+{delta / total:.2%})\n" if total else "")
        buckets: dict[str, list] = {}
        for a in priced:
            buckets.setdefault(a["axis"], []).append(a)
        print("| axis | actors | mean x | total $ | surcharge $ |")
        print("|---|--:|--:|--:|--:|")
        for axis in sorted(buckets):
            g = buckets[axis]
            print(f"| {axis} | {len(g)} | {sum(x['multiplier'] for x in g) / len(g):.3f} | "
                  f"{sum(x['cost'] for x in g):,.0f} | {sum(x['delta'] for x in g):,.0f} |")
        print("\n## Largest single moves\n")
        print("| actor | weapon | axis | x | cost | +$ |")
        print("|---|---|---|--:|--:|--:|")
        for a in sorted(priced, key=lambda x: -x["delta"])[:15]:
            print(f"| `{a['actor']}` | `{a['weapon']}` | {a['axis']} | {a['multiplier']:.3f} "
                  f"| {a['cost']:,.0f} | {a['delta']:,.0f} |")
        return 0

    print("# Physical-state pricing (E2)\n")
    print("## Meter geometry — measured from the resolved tree\n")
    print("| meter | Min | Max | range | scales to HP | actors | ratio (damage-scaled) |")
    print("|---|--:|--:|--:|---|--:|---|")
    for meter in sorted(AXIS_TEMPLATE):
        g = geom.get(meter)
        if not g:
            continue
        top = max(abs(g["max"]), abs(g["min"]))
        print(f"| `{meter}` | {g['min']} | {g['max']} | {g['range']} | "
              f"{'yes' if g['relative'] else 'no'} | {g['actors']} | "
              f"{top * 100 // g['range']}/Scale |")

    print("\n## Exposure — a meter the target does not carry delivers nothing\n")
    for meter in sorted(AXIS_TEMPLATE):
        print(f"- `{meter}`: **{exp.get(meter, 0):.1%}** of priced actors")

    print("\n## Effect curves — share of the axis delivered at a given fill\n")
    print("| axis | 5% | 25% | 50% | 75% | 100% |")
    print("|---|--:|--:|--:|--:|--:|")
    for label, key in (("heat", ("Temperature", True)), ("cryo", ("Temperature", False)),
                       ("corrosion", ("Corrosion", True))):
        c = curves[key]
        print(f"| {label} | " + " | ".join(f"{c(x):.2f}" for x in
                                           (0.05, 0.25, 0.5, 0.75, 1.0)) + " |")
    print(f"\nReference delivery (the maintainer's bar on the cryo curve): "
          f"**{reference:.4f}**\n")

    print("## Priced bindings\n")
    print(f"- bindings on fired weapons: **{len(rows)}**")
    print(f"- distinct weapons: **{len({r['weapon'] for r in rows})}**")
    full = [r for r in rows if r["weight"] >= 0.999]
    zero = [r for r in rows if r["weight"] <= 0.001]
    print(f"- pay the full 1.25x: **{len(full)}**")
    print(f"- pay nothing (deliver nothing): **{len(zero)}**")
    print(f"- partial: **{len(rows) - len(full) - len(zero)}**\n")

    print("| axis | mechanism | bindings | median ratio | median x |")
    print("|---|---|--:|--:|--:|")
    groups: dict[tuple, list] = {}
    for r in rows:
        groups.setdefault((r["axis"], r["kind"]), []).append(r)
    for key in sorted(groups):
        g = groups[key]
        ratios = sorted(r["ratio"] for r in g if r["ratio"] is not None)
        mults = sorted(r["multiplier"] for r in g)
        med_r = ratios[len(ratios) // 2] if ratios else float("nan")
        print(f"| {key[0]} | {key[1]} | {len(g)} | {med_r:.3f} | "
              f"{mults[len(mults) // 2]:.3f} |")
    return 0


if __name__ == "__main__":
    sys.exit(main())
