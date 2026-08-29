#!/usr/bin/env python3
"""anchor_readiness.py — which class anchors can actually be signed off, and why not.

    python tools/balance/anchor_readiness.py
    python tools/balance/anchor_readiness.py --json out.json

WHY THIS IS THE CRITICAL PATH
-----------------------------
Pricing is blocked on class anchors and **0 of 27 are signed off**. Nothing said
WHY. This measures it.

`fit_class.py` validates an anchor by pricing every MEMBER of its class from that
anchor's spec, so an anchor is only signable if (a) it has members and (b) those
members sit near it in the space DESIGN §12 prices in:

    Cost = cost0 * (O/O0 + P/P0 + Q/Q0) / 3

This reports, per class, how far its tagged members actually sit from its anchor.
A class whose members cluster tightly can be validated today; one whose members
are scattered will produce large residuals and needs its anchor or its membership
revisited first. That turns "0 of 27, unknown why" into a work order.

WHAT THE MEASUREMENT FOUND (2026-08-29)
---------------------------------------
* **Median distance from a tagged unit to its OWN anchor: 1.95.**
* **Median distance between two DIFFERENT anchors: 1.21.**

Units are FURTHER from their own class anchor than the anchors are from each
other. So class membership is not recoverable from (hp, dps, range, speed): a
nearest-anchor classifier scores **17.6%** against the 346 known labels, and that
number is reported here as evidence rather than hidden — see `--classifier`.

That is not automatically a defect. It says class is a ROLE judgement, which is
why `fit_class.py` step 1 has the maintainer tag members by hand. But it has two
consequences the pipeline must respect:

* Several anchors are statistically indistinguishable — `anti_air_vehicle` and
  `missile_vehicle` sit **0.024** apart, `archer` and `flying_infantry` 0.048,
  `rocket_trooper` and `special_forces` 0.053. They are separated by what they
  SHOOT AT, not by their stats, so no stat-based check can police their boundary.
* Classes differ enormously in how well their anchor describes them, from
  `support` at 0.24 to `melee` at 12.04. They are NOT equally ready to sign, and
  signing them as one batch would bake the loose ones in.

⚠ Cost is deliberately NOT a feature. Cost is what the formula SOLVES FOR;
feeding it back would let a mispriced unit justify its own class and make the
anchor fit look better than it is.
"""

from __future__ import annotations

import argparse
import collections
import glob
import json
import math
import statistics
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
LEDGER = ROOT / "docs" / "balance"
TICKS_PER_SECOND = 25

FEATURES = ("hp", "dps", "range", "speed")
SPEC_KEY = {"hp": "hp0", "dps": "dps0", "range": "range0_wdist", "speed": "speed0"}


def fnum(v):
    if v is None:
        return None
    try:
        return float(str(v).strip())
    except (TypeError, ValueError):
        return None


def unit_dps(unit):
    """Peak single-armament DPS: damage / reload * ticks. None when unarmed."""
    best = None
    for arm in unit.get("armaments") or []:
        if not arm.get("pricing"):
            continue
        reload_ = fnum(arm.get("reloaddelay"))
        if not reload_:
            continue
        damage = 0.0
        for wh in arm.get("damage_warheads") or []:
            if (wh.get("type") or "") == "AreaDamagePercentage":
                continue                     # a percentage twin, not flat damage
            d = fnum(wh.get("damage"))
            if d:
                damage += d
        if damage <= 0:
            continue
        dps = damage / reload_ * TICKS_PER_SECOND
        best = dps if best is None else max(best, dps)
    return best


def unit_range(unit):
    best = None
    for arm in unit.get("armaments") or []:
        r = fnum(arm.get("range"))
        if r:
            best = r if best is None else max(best, r)
    return best


def features(unit):
    return {"hp": fnum((unit.get("hp") or {}).get("v")),
            "dps": unit_dps(unit),
            "range": unit_range(unit),
            "speed": fnum((unit.get("speed") or {}).get("v"))}


def load_units():
    """[(faction, section, name, record)] over every ledger."""
    out = []
    for path in sorted(glob.glob(str(LEDGER / "*.json"))):
        if "class_anchors" in path:
            continue
        try:
            doc = json.loads(pathlib.Path(path).read_text(encoding="utf-8"))
        except (ValueError, OSError):
            continue
        for section, units in (doc.get("sections") or {}).items():
            if not isinstance(units, dict):
                continue
            for name, rec in units.items():
                if isinstance(rec, dict):
                    out.append((doc.get("ledger", ""), section, name, rec))
    return out


def distance(feat, spec):
    """Mean squared log-ratio over the features both sides define."""
    terms = []
    for key in FEATURES:
        got, want = feat.get(key), fnum(spec.get(SPEC_KEY[key]))
        if got is None or not want or got <= 0:
            continue
        terms.append(math.log(got / want) ** 2)
    if not terms:
        return None
    return sum(terms) / len(terms)


def predict(feat, section, anchors, sections_of):
    """(class, distance, runner_up_distance) or (None, ...) when unclassifiable."""
    scored = []
    for cls, entry in anchors.items():
        if cls.startswith("_"):
            continue
        allowed = sections_of.get(cls)
        if allowed and section not in allowed:
            continue                          # never cross an aircraft into a tank class
        d = distance(feat, entry.get("spec") or {})
        if d is not None:
            scored.append((d, cls))
    if not scored:
        return None, None, None
    scored.sort()
    runner = scored[1][0] if len(scored) > 1 else None
    return scored[0][1], scored[0][0], runner


VALIDATION = re.compile(
    r"\|\s*`([^`]+)`\s*\|\s*([\d.]+)\s*\|\s*(.+?)\s*\|\s*([+-]?\d+)%")


def residuals():
    """{class: [abs percent error]} from fit_class.py's validation tables.

    ⚠ THIS, NOT STAT DISTANCE, IS READINESS. A class can cluster tightly in
    (hp, dps, range, speed) and still price badly, because the formula weights
    those axes against an anchor rather than measuring nearness to it —
    `fire_support` has a tight 0.88 median distance and a 35% median pricing
    error. Only the residual says whether an anchor can be signed.
    """
    out, unscored = {}, {}
    for path in sorted(LEDGER.glob("formula_v2_*.md")):
        cls = path.stem.replace("formula_v2_", "")
        if cls == "classes":                   # a design note, not a fit table
            continue
        deltas, nostats = [], 0
        for line in path.read_text(encoding="utf-8").splitlines():
            m = VALIDATION.match(line)
            if m:
                deltas.append(abs(int(m.group(4))))
            elif "(no combat stats)" in line:
                nostats += 1
        if deltas:
            out[cls] = deltas
            unscored[cls] = nostats
    return out, unscored


def anchor_spread(anchors):
    """Pairwise distance between anchor SPECS — how separable the classes are."""
    specs = {k: (v.get("spec") or {}) for k, v in anchors.items()
             if not k.startswith("_")}
    pairs = []
    for a in specs:
        for b in specs:
            if a >= b:
                continue
            feat = {k: fnum(specs[a].get(SPEC_KEY[k])) for k in FEATURES}
            d = distance(feat, specs[b])
            if d is not None:
                pairs.append((d, a, b))
    return sorted(pairs)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--json", help="write the readiness table here")
    ap.add_argument("--classifier", action="store_true",
                    help="also self-score a nearest-anchor classifier on the "
                         "known labels (the 17.6% evidence)")
    args = ap.parse_args()

    anchors = json.loads((LEDGER / "class_anchors.json").read_text(encoding="utf-8"))
    units = load_units()
    tagged = [(f, s, n, r) for f, s, n, r in units
              if (r.get("design") or {}).get("class_anchor")]

    buildable = sum(1 for _f, _s, _n, r in units if r.get("buildable"))
    classes = [c for c in anchors if not c.startswith("_")]
    signed = [c for c in classes if anchors[c].get("signed_off")]

    print("# Class anchor readiness\n")
    print(f"classes defined      : {len(classes)}")
    print(f"signed off           : **{len(signed)}**")
    print(f"buildable units      : {buildable}")
    tagged_buildable = sum(1 for _f, _s, _n, r in units
                           if r.get("buildable")
                           and (r.get("design") or {}).get("class_anchor"))
    print(f"tagged with a class  : {tagged_buildable} of the buildable "
          f"({tagged_buildable / buildable * 100:.1f}%); {len(tagged)} including "
          "non-buildable\n")

    # --- per-class fit ------------------------------------------------------ #
    members = collections.defaultdict(list)
    for _f, _s, _n, rec in tagged:
        cls = (rec.get("design") or {})["class_anchor"]
        d = distance(features(rec), (anchors.get(cls) or {}).get("spec") or {})
        if d is not None:
            members[cls].append(d)

    resid, unscored = residuals()
    rows = []
    for cls in classes:
        ds = members.get(cls) or []
        rs_ = resid.get(cls) or []
        # A class "scored" only on members the formula could actually price, and
        # a table containing nothing but its own anchor proves nothing: the
        # anchor prices itself at exactly 0% by construction.
        scored = len(rs_)
        rows.append({
            "class": cls,
            "members": len(ds),
            "scored": scored,
            "median_error_pct": round(statistics.median(rs_)) if rs_ else None,
            "within_10pct": round(sum(1 for d in rs_ if d <= 10) / scored * 100)
                            if scored else None,
            "worst_error_pct": max(rs_) if rs_ else None,
            "unpriceable": unscored.get(cls, 0),
            "signed_off": bool(anchors[cls].get("signed_off")),
        })
    rows.sort(key=lambda r: (r["median_error_pct"] is None,
                             r["median_error_pct"] if r["median_error_pct"] is not None else 0,
                             -(r["scored"] or 0)))

    print("## Sign-off queue — ranked by PRICING error, not stat distance\n")
    print("`median |Δ|` is how far the class formula's price sits from the unit's "
          "actual cost, from `fit_class.py`'s validation table. **A class needs at "
          "least 3 scored members to mean anything** — an anchor prices itself at "
          "0% by construction.\n")
    print("| class | scored | median \\|Δ\\| | within 10% | worst | verdict |")
    print("|---|--:|--:|--:|--:|---|")
    ready = blocked = empty = 0
    for r in rows:
        if r["median_error_pct"] is None:
            verdict = "⛔ not fitted — no anchor, or the anchor has no stats"
            empty += 1
        elif r["scored"] < 3:
            verdict = f"⚠ only {r['scored']} scored — too few to judge"
            blocked += 1
        elif r["median_error_pct"] <= 10:
            verdict = "✅ **SIGN THIS ONE** — the anchor prices its class"
            ready += 1
        elif r["median_error_pct"] <= 25:
            verdict = "⚠ close — review the outliers, then sign"
            blocked += 1
        else:
            verdict = "⛔ the anchor does not describe its members"
            blocked += 1
        cells = [f"`{r['class']}`", str(r["scored"]),
                 f"{r['median_error_pct']}%" if r["median_error_pct"] is not None else "—",
                 f"{r['within_10pct']}%" if r["within_10pct"] is not None else "—",
                 f"{r['worst_error_pct']}%" if r["worst_error_pct"] is not None else "—",
                 verdict]
        print("| " + " | ".join(cells) + " |")

    alld = [d for ds in members.values() for d in ds]
    pairs = anchor_spread(anchors)
    print(f"\n**{ready} classes are ready to SIGN today**, {blocked} need review "
          f"first, {empty} could not be fitted.\n")
    if alld and pairs:
        own = statistics.median(alld)
        between = statistics.median([d for d, _a, _b in pairs])
        print(f"Median distance to OWN anchor: **{own:.2f}**. Median distance "
              f"BETWEEN anchors: **{between:.2f}**.")
        if own > between:
            print("\n⚠ Units sit FURTHER from their own class anchor than the "
                  "anchors sit from each other, so the class boundaries are not "
                  "recoverable from stats — they are role judgements. Any check "
                  "that tries to police membership numerically will be wrong.")

    print("\n## Anchors that are statistically indistinguishable\n")
    print("Separated by what they SHOOT AT, not by their stats. No stat-based "
          "check can police these boundaries.\n")
    for d, a, b in pairs[:8]:
        print(f"  {d:6.3f}  `{a}` <-> `{b}`")

    if args.classifier:
        hit = miss = skipped = 0
        sections_of = collections.defaultdict(set)
        for _f, section, _n, rec in tagged:
            sections_of[(rec.get("design") or {})["class_anchor"]].add(section)
        confusion = collections.Counter()
        for _f, section, _n, rec in tagged:
            truth = (rec.get("design") or {})["class_anchor"]
            got, _d, _r = predict(features(rec), section, anchors, sections_of)
            if got is None:
                skipped += 1
            elif got == truth:
                hit += 1
            else:
                miss += 1
                confusion[(truth, got)] += 1
        total = hit + miss
        print(f"\n## Classifier self-score (the evidence for 'role, not stats')\n")
        print(f"A nearest-anchor classifier re-predicting the {total} known labels "
              f"scores **{hit / total * 100:.1f}%** top-1.\n")
        for (truth, got), n in confusion.most_common(6):
            print(f"  {truth:22} -> {got:22} {n}")

    if args.json:
        pathlib.Path(args.json).write_text(
            json.dumps({"rows": rows,
                        "closest_anchor_pairs": [
                            {"a": a, "b": b, "d": round(d, 4)}
                            for d, a, b in pairs[:12]]},
                       indent=1, sort_keys=True), encoding="utf-8")
        print(f"\nwrote {args.json}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
