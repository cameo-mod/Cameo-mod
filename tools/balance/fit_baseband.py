#!/usr/bin/env python3
"""fit_baseband.py — can a class fit the 100%-250% baseband, and where does its baseline go?

MAINTAINER RULING R3: reference -> virtual baseline per class -> fit the 100%-250% band ->
parameterise the formula per class -> only THEN price. And, asked directly which moves:
⭐ **"Move the BASELINE, not the actors."**

This tool answers the two questions that ruling raises, and the first answer is geometric
rather than statistical.

⛔ **`cost0` CANNOT MOVE THE BAND.** The band ratio is `class price / cost0`, and
`class_baseline_estimators` multiplies every estimator BY `cost0`:

    o = (h+s+r+d) * cost0/4      p = (h*s + r*d) * cost0/2      q = h*s*r*d * cost0

so the ratio is `[(h+s+r+d)/4 + (h*s+r*d)/2 + h*s*r*d] / 3`, in which `cost0` has cancelled
out completely. Re-pricing a baseline changes what the class COSTS and moves nothing into the
band. Only the four STAT axes (`hp0`, `speed0`, `range0_wdist`, `dps0`) do that, through
`h = hp/hp0` and its siblings.

⛔ **AND THE BAND IS NOT A WINDOW AROUND THE BASELINE — IT STARTS AT IT.** At the baseline
h=s=r=d=1, so the ratio is exactly 1.000: the baseline sits precisely on the 100% edge. The
250% edge is exactly the 2x-HP / 2x-DPS verifier. So "every member inside 100%-250%" means:

    the baseline must be at or BELOW the weakest member on the combined axes,
    and the whole class must span no more than the verifier envelope.

That is why a MEDIAN-derived baseline can never satisfy the band: it puts half the class
below 100% by construction. `derive_virtual_anchor` proposes reference-backed class MEDIANS,
which is right for "what is typical" and wrong for "where does the band start". The two are
different questions and the pipeline currently asks the first to answer the second.

The decay is steep, which is why the occupancy numbers are so low: a member weaker than the
baseline by just 10% on every axis already prices at **78.9%**, below the 75% floor. There is
no gentle shoulder.

WHAT THIS TOOL COMPUTES, per class:

  span      max_ratio / min_ratio at the CURRENT baseline. Near-invariant under a uniform
            baseline rescale, so it decides FEASIBILITY before any fitting: a class whose
            span exceeds 2.5 cannot fit 100%-250% under ANY baseline, and needs splitting or
            a tech-tier gate on its outliers. Reporting that is the point — it is not a
            failure of the fit.
  k*        the single scale applied to all four stat axes that maximises sweet-spot
            occupancy (ties broken toward the weakest member landing on 100%).
  fitted    occupancy at k*, and the members still outside.

It writes nothing. Choosing a baseline is a maintainer act; this says what each choice costs.

Usage: python tools/balance/fit_baseband.py [--json <band.json>] [--class X]
       (--json defaults to regenerating it via check_band --json into a temp file)
"""
from __future__ import annotations

import argparse
import json
import math
import pathlib
import statistics
import subprocess
import sys
import tempfile

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

import formula  # noqa: E402
import tier_chain  # noqa: E402

SWEET_LO, SWEET_HI = 1.00, 2.50
SOFT_FLOOR, CEIL = 0.75, 3.50
# Grid steps for reporting a fitted baseline, from derive_virtual_anchor.STEPS.
STEPS = dict(hp0=1000, speed0=1, range0_wdist=10, dps0=1)


def ratio_of(inp, spec, anchor_tier, k=1.0):
    """The band ratio for one member, with every baseline STAT axis scaled by `k`.

    `cost0` is deliberately absent: it cancels. Passing it would invite the belief that
    re-pricing a baseline moves the band.
    """
    hp, speed, rng, dps_v, special, tier = inp
    rel = tier / anchor_tier if anchor_tier else tier
    o, p, q = formula.class_baseline_estimators(
        hp, speed, rng, dps_v,
        spec["hp0"] * k, spec["speed0"] * k, spec["range0_wdist"] * k, spec["dps0"] * k,
        1.0, special=special, tech_tier=rel)
    return (o + p + q) / 3


def core_window(ratios, span_cap=SWEET_HI):
    """(lo_index, hi_index) of the largest set of members that CAN share one baseline.

    The band ratio is one-dimensional, so sort and take the longest contiguous run whose
    max/min fits the envelope; anything outside it cannot be priced from the same baseline as
    the rest. Contiguity loses nothing here — a member between two in-window members is in
    the window by construction.

    This is what makes the span number actionable. A class that "cannot fit" usually has a
    tight core plus two or three members that do not belong in it at all: `futuretech_blackwidow`
    is in `melee` with a range of 9000, and `corrino_buggy` is in `mbt`. The band is reading
    out a CLASSIFICATION defect, not arguing with the band law.
    """
    rs = sorted(ratios)
    best = (0, 0, 0)
    lo = 0
    for hi in range(len(rs)):
        while rs[hi] / rs[lo] > span_cap:
            lo += 1
        if hi - lo + 1 > best[0]:
            best = (hi - lo + 1, lo, hi)
    return best[1], best[2]


def occupancy(members, spec, anchor_tier, k):
    rs = [ratio_of(m, spec, anchor_tier, k) for m in members]
    return sum(1 for r in rs if SWEET_LO <= r <= SWEET_HI), rs


def best_k(members, spec, anchor_tier):
    """The uniform stat-axis scale maximising sweet-spot occupancy.

    Scanned rather than solved: the ratio is monotone in `k` per member but the COUNT in
    band is a step function of it, so there is no derivative to set to zero. A log-spaced
    scan is exact enough for a decision and cannot mistake a local step for an optimum.
    """
    best = (-1, 1.0, None)
    for i in range(-400, 401):
        k = math.exp(i / 200.0)          # ~0.135x .. ~7.4x
        n, rs = occupancy(members, spec, anchor_tier, k)
        # tie-break: prefer the k that puts the weakest member closest to 100% from above
        slack = min(rs) - SWEET_LO
        key = (n, -abs(slack) if slack >= 0 else -100 + slack)
        if key > (best[0], best[2] if best[2] is not None else -1e9):
            best = (n, k, key[1])
    return best[1]


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--json", type=pathlib.Path)
    ap.add_argument("--class", dest="cls")
    args = ap.parse_args()

    path = args.json
    if path is None:
        path = pathlib.Path(tempfile.gettempdir()) / "fit_baseband_band.json"
        subprocess.run([sys.executable, str(ROOT / "tools/balance/check_band.py"),
                        "--json", str(path)], cwd=ROOT, capture_output=True)
    data = json.loads(path.read_text(encoding="utf-8"))
    anchors = {k: v for k, v in json.loads(
        (ROOT / "docs/balance/class_anchors.json").read_text(encoding="utf-8")).items()
        if isinstance(v, dict)}
    # Same anchor-tier resolution as check_band, or the recomputation does not match it.
    tier_map = tier_chain.load_derived_map(ROOT / "docs/balance")
    anchor_tiers = {}
    for cls, a in anchors.items():
        actor = a.get("anchor_actor")
        if actor and actor in tier_map:
            anchor_tiers[cls] = tier_map[actor].get("tier_multiplier", 1.0)
        else:
            try:
                anchor_tiers[cls] = float(a.get("tech_tier")) or 1.0
            except (TypeError, ValueError):
                anchor_tiers[cls] = 1.0

    by_class: dict[str, list] = {}
    reported: dict[str, list] = {}
    names: dict[str, list] = {}
    for row in data["rows"]:
        if row.get("band_exempt"):
            continue
        cls = row["class"]
        if args.cls and cls != args.cls:
            continue
        i = row["inputs"]
        by_class.setdefault(cls, []).append(
            (i["hp"], i["speed"], i["range"], i["raw_dps"], i["special"], i["tier"]))
        reported.setdefault(cls, []).append(row["ratio"])
        names.setdefault(cls, []).append((row["actor"], i))

    print("# Baseband feasibility and where the baseline has to go\n")
    print("band ratio = class price / cost0.  cost0 CANCELS — only hp0/speed0/range0/dps0 move it.")
    print("the baseline sits at exactly 100%; the 2x-HP/2x-DPS verifier at exactly 250%.\n")

    rows, infeasible, checked, mismatched = [], [], 0, 0
    for cls in sorted(by_class):
        anchor = anchors.get(cls) or {}
        spec = anchor.get("spec")
        if not spec or not spec.get("dps0") or not spec.get("range0_wdist"):
            continue
        # ⚠ The anchor's tier comes from the TIER MAP via `anchor_actor`, and only falls back
        # to the anchor's own `tech_tier` field. Reading the field first made 257 of 404
        # recomputed ratios disagree with check_band — caught by the cross-check below, which
        # is the only reason this tool's numbers can be trusted at all.
        at = anchor_tiers.get(cls, 1.0)
        members = by_class[cls]

        # ⚠ Verify the recomputation against check_band's own numbers before trusting it.
        mine = [ratio_of(m, spec, at, 1.0) for m in members]
        for a, b in zip(mine, reported[cls]):
            checked += 1
            if not math.isclose(a, b, rel_tol=1e-6):
                mismatched += 1

        n0 = sum(1 for r in mine if SWEET_LO <= r <= SWEET_HI)
        span = max(mine) / min(mine)
        k = best_k(members, spec, at)
        nk, rk = occupancy(members, spec, at, k)
        rows.append((cls, len(members), n0, nk, span, k, min(rk), max(rk), spec))
        if span > SWEET_HI:
            infeasible.append((cls, span, len(members)))

    print(f"recomputation cross-check against check_band: {checked - mismatched}/{checked} "
          f"ratios identical" + ("" if not mismatched else f"  ⚠ {mismatched} MISMATCH") + "\n")

    print("| class | members | in band now | best single-k | k* | min% | max% | span | verdict |")
    print("|---|--:|--:|--:|--:|--:|--:|--:|---|")
    for cls, n, n0, nk, span, k, lo, hi, _spec in sorted(rows, key=lambda r: -r[4]):
        verdict = ("CANNOT FIT — span > 2.5" if span > SWEET_HI
                   else "fits fully" if nk == n else "partial")
        print(f"| `{cls}` | {n} | {n0} ({n0/n:.0%}) | {nk} ({nk/n:.0%}) | {k:.3f} | "
              f"{lo*100:.0f}% | {hi*100:.0f}% | {span:.1f}x | {verdict} |")

    tot = sum(r[1] for r in rows)
    now = sum(r[2] for r in rows)
    fit = sum(r[3] for r in rows)
    print(f"\n**{now}/{tot} ({now/tot:.0%}) of priced members sit in the sweet spot today; "
          f"re-scaling each class baseline alone reaches {fit}/{tot} ({fit/tot:.0%}).**")
    print(f"\n**{len(infeasible)} of {len(rows)} classes cannot fit the band as currently "
          f"constituted** — their own spread exceeds the 2.5x baseline-to-verifier envelope.")
    print("But the spread is concentrated, not general: below is each class's CORE (the "
          "largest set that can share one baseline) and the members that cannot join it.\n")

    print("| class | core | core span | members that cannot share the core baseline |")
    print("|---|--:|--:|---|")
    triage_total = 0
    for cls, span, _n in sorted(infeasible, key=lambda r: -r[1]):
        paired = sorted(zip(reported[cls], names[cls]), key=lambda x: x[0])
        lo, hi = core_window([r for r, _ in paired])
        core = paired[lo:hi + 1]
        out = paired[:lo] + paired[hi + 1:]
        triage_total += len(out)
        shown = ", ".join(f"`{a}` ({r * 100:.0f}%)" for r, (a, _i) in out[:5])
        more = "" if len(out) <= 5 else f" +{len(out) - 5} more"
        cspan = core[-1][0] / core[0][0] if core else 0
        print(f"| `{cls}` | {len(core)}/{len(paired)} | {cspan:.1f}x | {shown}{more} |")

    print(f"\n**{triage_total} members across those classes sit outside their own class core.**"
          " Each is one of three things, and only a maintainer can say which:")
    print("  * misclassified — `futuretech_blackwidow` is in `melee` with a range of 9000, "
          "and `corrino_buggy` is in `mbt`;")
    print("  * a legitimate higher tier that needs a tech-tier gate rather than a wider band;")
    print("  * genuinely mis-stated, which is what the pipeline exists to fix.")
    print("\n⛔ Until that triage happens, no baseline for these classes can be signed: the "
          "band would be fitted to a population that does not belong together.")

    print("\n## The fitted baselines, on the grid\n")
    print("| class | hp0 | speed0 | range0_wdist | dps0 | (cost0 unchanged — it cannot move the band) |")
    print("|---|--:|--:|--:|--:|---|")
    for cls, _n, _n0, _nk, _span, k, _lo, _hi, spec in sorted(rows):
        vals = {}
        for key, step in STEPS.items():
            vals[key] = max(step, round(spec[key] * k / step) * step)
        print(f"| `{cls}` | {vals['hp0']:g} | {vals['speed0']:g} | {vals['range0_wdist']:g} | "
              f"{vals['dps0']:g} | {spec['cost0']:g} |")

    return 1 if mismatched else 0


if __name__ == "__main__":
    sys.exit(main())
