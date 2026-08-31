#!/usr/bin/env python3
"""check_band.py — Balance-pipeline BASEBAND validator (BALANCE_PIPELINE §8.1).

For every ledger unit tagged design.class_anchor = <class>, compute its
class-formula price and the ratio price/cost0, then enforce the baseband law:

  * hard band     50%..350%  of the class baseline cost0   (= x0.50 .. x2.50 stats)
  * TARGET band   75%..250%  of cost0 — target >=80% occupancy
                  (maintainer 2026-08-31: *"the target band should be at 75% to 250% where
                  most units are located"*, *"the 75% referred to the unit price not the
                  stats"*, and *"let's use the full band from cost 50% and stats 50% to
                  cost 3.5x and stats 2.5x"*)

⭐ EVERY ONE OF THOSE NUMBERS IS DERIVED, NOT PREFERRED (BALANCE_PIPELINE §8.1a).
Hold speed and range at the anchor's and write h, d for the HP and DPS multipliers.
`formula.class_baseline_estimators` is then O=(h+1+1+d)/4, P=(h+d)/2, Q=h*d, and the
price collapses to a closed form — verified exactly against the module in
`tools/tests/test_band_law.py`:

    price(h, d) = (3*(h + d) + 4*h*d + 2) / 12       # SYMMETRIC in h and d
    price(x, x) = (2x + 1)(x + 1) / 6                # both stats moved together
    x(P)        = (sqrt(1 + 48*P) - 3) / 4           # the inverse

So each ring of the band is just the price of a STAT WINDOW, and the useful ones are exact:

    x = 0.50  ->  0.500   FLOOR       half the anchor's HP and DPS      EXACT BOTH WAYS
    x = 0.7707 -> 0.750   SWEET_LO    a 75% PRICE, per the ruling
    x = 1.00  ->  1.000   the anchor itself                              EXACT BOTH WAYS
    x = 2.00  ->  2.500   SWEET_HI    double                             EXACT BOTH WAYS
    x = 2.50  ->  3.500   CEIL        two and a half                     EXACT BOTH WAYS

⚠ THE RINGS ARE COST NUMBERS. The maintainer ruled it explicitly -- *"the 75% referred to
the unit price not the stats"* -- and it is the right call: a price is what a player reads
off the build palette, a stat multiplier is not. The stat window is the DERIVED reading,
and it happens to come out round at four of the five rings, which is why this band beats
the 4.00 ceiling it replaced (x2.7231 -- round in neither space).

⚠ AND THE RINGS ARE CURVES, NOT BOXES. `3(h+d) + 4hd = 28` is the entire 250% iso-cost
line, so 2x HP with 2x DPS, 4x HP with 0.84x DPS, and 1x HP with 3.57x DPS all cost
exactly 250%. That IS the maintainer's *"one of the stats can also be higher if the other
one is a bit lower"* — in closed form, and symmetric: HP and DPS are interchangeable here.

  The 250% ceiling is a PRICE RATIO. It used to be described as "baseline..verifier",
  after a nominated second actor pinned at 2.5x cost0 — that verifier was RETIRED on
  2026-08-29 (HANDOFF §3.0j; maintainer: "they should be regular units like anything
  else and not have those stiff rules"). The law is unchanged: this check never read a
  verifier actor, which is exactly why it survived the retirement intact.

Read-only. Emits a report and a nonzero exit if any member is below SWEET_LO (72.9%) or
above 400% (unless it is a BuildLimit:1 epic/hero, which is band-exempt).

Usage: python tools/balance/check_band.py [--class X] [--md docs/audit/latest/band.md]
"""
from __future__ import annotations
import argparse, json, pathlib, sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/balance"))
import formula  # noqa: E402
import tier_chain  # noqa: E402

LEDGER = ROOT / "docs/balance"
ANCHORS = LEDGER / "class_anchors.json"

# ⛔ RINGS ARE DECLARED IN **COST**, and every one of them is checked back through
# price(x, x) = (2x+1)(x+1)/6. Maintainer ruling 2026-08-31, in two parts:
#   (a) "The 75% referred to the unit PRICE not the stats" -- the rings live in cost space,
#       which is the space players see, and the stat window is the DERIVED reading.
#   (b) "the full band from cost 50% and stats 50% to cost 3.5x and stats 2.5x"
# ⭐ FOUR OF THE FIVE RINGS ARE EXACT IN BOTH SPACES AT ONCE, which is what makes this
# band better than the 4.00 ceiling it replaces (4.00 was x2.7231 stats -- round in
# neither space, and unexplainable to anyone who asked why).
FLOOR      = 0.50   # = x0.50 stats  EXACT BOTH WAYS
SWEET_LO   = 0.75   # = x0.7707 stats -- round in COST only, and that is the ruling:
                    #   "75%" is a price the player reads off the build palette.
                    #   ⚠ Do NOT "fix" this to 35/48 = 0.7292 (the cost of x0.75 stats).
                    #   An earlier revision did exactly that and had to be reverted.
SWEET_HI   = 2.50   # = x2.00 stats  EXACT BOTH WAYS -- the maintainer's own derivation
CEIL       = 3.50   # = x2.50 stats  EXACT BOTH WAYS
SOFT_FLOOR = SWEET_LO   # kept: the old name for the same ring


def fnum(v):
    try: return float(v)
    except (TypeError, ValueError): return None


def unit_inputs(u, du=None):
    """(hp, speed, range_wdist, dps, special, unit_class, tech_tier) or None."""
    hp = fnum((u.get("hp") or {}).get("v"))
    speed = fnum((u.get("speed") or {}).get("v") or (u.get("speed_air") or {}).get("v"))
    d = u.get("design") or {}
    du = du or {}
    total_dps, best_range = 0.0, 0.0
    for arm in u.get("armaments", []):
        if not arm.get("pricing", True):
            continue
        st = arm.get("stats", arm)  # tolerate both nesting styles
        dmg = formula.spread_damage_sum(st.get("damage_warheads", arm.get("damage_warheads", [])))
        reload_ = fnum((st.get("reload_delay") or {}).get("v") if isinstance(st.get("reload_delay"), dict)
                       else st.get("reloaddelay") or (st.get("reload_delay")))
        if not dmg or not reload_:
            continue
        rng = st.get("range")
        rng = formula.wdist_value(rng, 0.0)
        burst = st.get("burst"); burst = int(fnum(burst.get("v") if isinstance(burst, dict) else burst) or 1)
        # Raw ledgers use ``burstdelays``. Keep the underscored fallback only
        # for the older nested fixture shape this reader still accepts.
        bd = st.get("burstdelays", st.get("burst_delays"))
        total_dps += formula.dps(dmg, reload_, burst, bd)
        best_range = max(best_range, rng)
    if hp is None or speed is None or total_dps == 0:
        return None
    tech_tier = tier_chain.effective_tier(
        d.get("tech_tier"), du.get("tier_multiplier"), default=1.0)
    return (hp, speed, best_range, total_dps,
            fnum(d.get("special")) or 1.0, fnum(d.get("unit_class")) or 1.0,
            tech_tier)


def price_for(cls, anchor, inp, anchor_tier: float = 1.0):
    """Price a unit under its class anchor. Handles both anchor forms."""
    hp, speed, rng, dps_v, special, uclass, tier = inp
    spec = anchor.get("spec")
    if spec:  # class-baseline form (infantry classes)
        if not spec.get("range0_wdist") or not spec.get("dps0"):
            return None  # ability-priced class (e.g. support) — not formula-priced
        # class_baseline_price receives the RELATIVE multiplier.
        rel_tier = tier / anchor_tier if anchor_tier else tier
        return formula.class_baseline_price(
            hp, speed, rng, dps_v,
            spec["hp0"], spec["speed0"], spec["range0_wdist"], spec["dps0"], spec["cost0"],
            special=special, tech_tier=rel_tier)
    if all(k in anchor for k in ("o0", "p0", "q0", "cost0")):  # o/p/q form (mbt)
        # class_anchor_price cancels the anchor's own absolute tier.
        o, p, q = formula.estimators(hp, speed, rng, dps_v, special, uclass, tier)
        return formula.class_anchor_price(o, p, q, anchor["o0"], anchor["p0"], anchor["q0"], anchor["cost0"])
    return None


def cost0_of(anchor):
    return (anchor.get("spec") or {}).get("cost0") or anchor.get("cost0")


def collect(tier_map):
    for jf in sorted(LEDGER.glob("*.json")):
        if jf.name == "class_anchors.json":
            continue
        doc = json.loads(jf.read_text(encoding="utf-8"))
        if "sections" not in doc:
            continue
        for sec in doc["sections"].values():
            for actor, u in sec.items():
                yield jf.name, actor, u, tier_map.get(actor, {})


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--class", dest="cls")
    ap.add_argument("--md")
    args = ap.parse_args()
    anchors = {k: v for k, v in json.loads(ANCHORS.read_text(encoding="utf-8")).items()
               if isinstance(v, dict)}
    tier_map = tier_chain.load_derived_map(LEDGER)
    anchor_tiers = {}
    for cls, a in anchors.items():
        if not isinstance(a, dict):
            continue
        if a.get("anchor_actor") and a["anchor_actor"] in tier_map:
            anchor_tiers[cls] = tier_map[a["anchor_actor"]].get("tier_multiplier", 1.0)
        else:
            anchor_tiers[cls] = fnum(a.get("tech_tier")) or 1.0

    per_class = {}
    for fname, actor, u, du in collect(tier_map):
        d = u.get("design") or {}
        cls = d.get("class_anchor")
        if not cls or (args.cls and cls != args.cls) or cls not in anchors:
            continue
        anchor = anchors[cls]; c0 = cost0_of(anchor)
        if not c0:
            continue
        inp = unit_inputs(u, du)
        if inp is None:
            continue
        pr = price_for(cls, anchor, inp, anchor_tiers.get(cls, 1.0))
        if pr is None:
            continue
        epic = bool(u.get("build_limit"))
        per_class.setdefault(cls, []).append((actor, pr, pr / c0, epic))

    out = ["# Baseband validator (BALANCE_PIPELINE §8.1)", "",
           f"band: floor {SOFT_FLOOR:.0%} - sweet {SWEET_LO:.0%}–{SWEET_HI:.0%} - ceil {CEIL:.0%}",
           ""]
    violations = 0
    for cls in sorted(per_class):
        rows = sorted(per_class[cls], key=lambda r: r[2])
        signed = anchors[cls].get("signed_off")
        n = len(rows)
        sweet = sum(1 for _, _, r, _ in rows if SWEET_LO <= r <= SWEET_HI)
        lo = [a for a, _, r, e in rows if r < SOFT_FLOOR and not e]
        hi = [a for a, _, r, e in rows if r > CEIL and not e]
        violations += len(lo) + len(hi)
        out.append(f"## `{cls}` — {n} members - sweet-spot {sweet}/{n} ({sweet/n:.0%}) "
                   f"- signed_off={signed}")
        if lo: out.append(f"  !! below {SOFT_FLOOR:.0%} floor (too weak/price): {', '.join(lo[:12])}")
        if hi: out.append(f"  !! above {CEIL:.0%} ceil (needs tech-tier gate): {', '.join(hi[:12])}")
        out.append("")

    text = "\n".join(out)
    if args.md:
        p = ROOT / args.md
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text + "\n", encoding="utf-8", newline="\n")
        print(f"report -> {args.md}")
    else:
        print(text)
    print(f"[{violations} band violations across {len(per_class)} classes]")
    return 1 if violations else 0


if __name__ == "__main__":
    sys.exit(main())
