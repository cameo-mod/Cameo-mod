#!/usr/bin/env python3
"""fit_class.py — Balance Pipeline Phase 5 (Formula v2, DESIGN §12).

Derives a class-anchor entry from a maintainer-picked anchor unit and
validates it across every unit of that class, writing a sign-off report.

Workflow per class (one class at a time, maintainer-driven):
1. Maintainer tags class members: set design.class_anchor = "<class>"
   in the ledger for every unit of the class (or pass --actors).
2. Maintainer picks the ANCHOR unit (a unit whose current cost is
   considered correct).
3.   python tools/balance/fit_class.py --class bomber \
         --anchor ra1_badger_bomber
   -> computes O0/P0/Q0 at the anchor's raw stats, cost0 = its cost,
      writes the candidate into docs/balance/class_anchors.json
      (signed_off: false) and the validation table
      docs/balance/formula_v2_<class>.md: every member's
      class-formula price vs actual cost.
4. Maintainer reviews the table; on approval set "signed_off": true —
   build_workbook.py then prices that class with the anchor formula.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/balance"))
import formula  # noqa: E402

LEDGER = ROOT / "docs/balance"
ANCHORS = LEDGER / "class_anchors.json"


def fnum(v):
    try:
        return float(v)
    except (TypeError, ValueError):
        return None


def unit_inputs(u):
    """(hp, speed, range_wdist, dps, special, unit_class, tech_tier) or None."""
    hp = fnum((u.get("hp") or {}).get("v"))
    speed = fnum((u.get("speed") or {}).get("v") or (u.get("speed_air") or {}).get("v"))
    d = u.get("design") or {}
    # Unconditional per-actor FirepowerMultiplier scales EFFECTIVE damage output;
    # balance prices on effective DPS = raw x FP-mult (cameo-firepower-mult-in-dps). Default 1.0.
    fp = fnum((u.get("firepower_multiplier") or {}).get("v")) or 1.0
    total_dps, best_range = 0.0, 0.0
    for arm in u.get("armaments", []):
        if not arm.get("pricing", True):
            continue
        if arm.get("requires"):   # upgrade-gated / conditional weapon -> NOT part of BASE dps
            continue
        dmg = formula.spread_damage_sum(arm.get("warheads", []))  # SUM law, chips excluded
        reload_ = fnum(arm.get("reloaddelay"))
        if not dmg or not reload_:
            continue
        total_dps += formula.dps(dmg, reload_,
                                 fnum(arm.get("design_weapon_class")) or 1.0,
                                 int(fnum(arm.get("burst")) or 1),
                                 fnum(arm.get("burstdelays")))
        best_range = max(best_range, fnum(arm.get("range")) or 0.0)
    total_dps *= fp   # apply the actor-level FirepowerMultiplier to effective DPS
    if hp is None or speed is None or total_dps == 0:
        return None
    return (hp, speed, best_range, total_dps,
            fnum(d.get("special")) or 1.0, fnum(d.get("unit_class")) or 1.0,
            fnum(d.get("tech_tier")) or 1.0)


def collect_units(cls, actors_filter):
    out = {}
    for jf in sorted(LEDGER.glob("*.json")):
        if jf.name in ("class_anchors.json",):
            continue
        doc = json.loads(jf.read_text(encoding="utf-8"))
        if "sections" not in doc:
            continue
        for sec in doc["sections"].values():
            for actor, u in sec.items():
                if actors_filter:
                    if actor in actors_filter:
                        out[actor] = u
                elif (u.get("design") or {}).get("class_anchor") == cls:
                    out[actor] = u
    return out


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--class", dest="cls", required=True)
    ap.add_argument("--anchor", help="anchor actor id (real-unit anchor)")
    ap.add_argument("--spec", help="virtual anchor `hp,speed,range_wdist,"
                    "damage,reload,cost0` — a round-number model unit that "
                    "need not exist in game (Tiger-style baseline)")
    ap.add_argument("--actors", nargs="*", help="explicit member list "
                    "(otherwise: design.class_anchor == --class)")
    args = ap.parse_args()
    if not args.anchor and not args.spec:
        ap.error("need --anchor or --spec")

    units = collect_units(args.cls, set(args.actors or []) |
                          ({args.anchor} if args.anchor else set()))
    if args.spec:
        hp, speed, rng, dmg, reload_, cost0 = (float(x) for x in args.spec.split(","))
        d0 = formula.dps(dmg, reload_)
        o0, p0, q0 = formula.estimators(hp, speed, rng, d0)
        anchor_id = f"SPEC({args.spec})"
    else:
        if args.anchor not in units:
            print(f"anchor `{args.anchor}` not found in the ledger")
            return 2
        ai = unit_inputs(units[args.anchor])
        if ai is None:
            print(f"anchor `{args.anchor}` lacks hp/speed/weapon stats")
            return 2
        cost0 = fnum((units[args.anchor].get("cost") or {}).get("v"))
        o0, p0, q0 = formula.estimators(*ai)
        anchor_id = args.anchor
    print(f"anchor {anchor_id}: cost0={cost0:.0f} O0={o0:.2f} P0={p0:.2f} Q0={q0:.2f}")

    rows = []
    for actor, u in sorted(units.items()):
        inp = unit_inputs(u)
        cost = fnum((u.get("cost") or {}).get("v"))
        if inp is None or cost is None:
            rows.append((actor, cost, None, None))
            continue
        o, p, q = formula.estimators(*inp)
        v2 = formula.class_anchor_price(o, p, q, o0, p0, q0, cost0)
        rows.append((actor, cost, v2, (v2 - cost) / cost if cost else None))

    rep = LEDGER / f"formula_v2_{args.cls}.md"
    lines = [f"# Formula v2 validation — class `{args.cls}`",
             "", f"anchor: `{args.anchor}` (cost0 {cost0:.0f}, "
             f"O0 {o0:.2f}, P0 {p0:.2f}, Q0 {q0:.2f})", "",
             "| unit | cost (actual) | class-formula price | delta |",
             "|---|---|---|---|"]
    for actor, cost, v2, delta in rows:
        if v2 is None:
            lines.append(f"| `{actor}` | {cost or '?'} | (no combat stats) | |")
        else:
            flag = "" if abs(delta) < 0.10 else (" ⚠" if abs(delta) < 0.30 else " ❗")
            lines.append(f"| `{actor}` | {cost:.0f} | {v2:.0f} | {delta:+.0%}{flag} |")
    rep.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")

    anchors = json.loads(ANCHORS.read_text(encoding="utf-8"))
    anchors[args.cls] = {"anchor_actor": args.anchor or anchor_id, "cost0": cost0,
                         "o0": round(o0, 4), "p0": round(p0, 4),
                         "q0": round(q0, 4), "signed_off": False,
                         "comment": "candidate — maintainer sign-off pending"}
    ANCHORS.write_text(json.dumps(anchors, sort_keys=True, indent=1,
                                  ensure_ascii=False) + "\n",
                       encoding="utf-8", newline="\n")
    print(f"candidate written to class_anchors.json (signed_off: false); "
          f"validation -> {rep.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
