#!/usr/bin/env python3
"""band_granularity.py — can a class's members actually FIT the baseband, and at what resolution?

    python tools/balance/band_granularity.py
    python tools/balance/band_granularity.py --md docs/audit/latest/band_granularity.md

WHY THIS EXISTS
---------------
`check_band.py` answers "is this member inside the band?". It cannot answer the two
questions the band law actually rests on:

  1. **Is the band WIDE ENOUGH for the class at all?**  Members are priced as RATIOS to
     the anchor, so moving the anchor SLIDES a class along the band and never NARROWS it.
     A class whose own priced spread exceeds the band's width can therefore never reach
     the ruled occupancy from ANY anchor. That is arithmetic, not tuning.
  2. **How many DISTINCT units does the band hold?**  A band is a resolution budget: at a
     cost step of `s`, a band of width `W` holds `ln(W)/ln(s)` telling-apart-able rungs.

THE BAND LAW IS DERIVED, NOT PREFERRED (BALANCE_PIPELINE §8.1a)
--------------------------------------------------------------
With speed and range held at the anchor's and the HP/DPS multipliers written `h` and `d`,
`formula.class_baseline_price` collapses to a closed form (verified exactly against the
module, `tools/tests/test_band_law.py`):

    price(h, d) = (3*(h + d) + 4*h*d + 2) / 12          # symmetric in h and d
    price(x, x) = (2x + 1)(x + 1) / 6                   # both stats moved together
    x(P)        = (sqrt(1 + 48*P) - 3) / 4              # the inverse

So every ring of the band is the price of a STAT WINDOW, and three of them are exact:

    x = 0.50  ->  price 0.500       FLOOR      half the anchor's HP and DPS
    x = 0.75  ->  price 0.729       SWEET_LO   three quarters
    x = 1.00  ->  price 1.000       the anchor itself
    x = 2.00  ->  price 2.500       SWEET_HI   double  (the maintainer's own derivation)
    x = 2.72  ->  price 4.000       CEIL

⚠ The ceiling is a CURVE, not a box: `3(h+d) + 4hd = 28` is the whole 2.50 iso-cost line,
so 2x HP and 2x DPS, 4x HP and 0.84x DPS, and 1x HP and 3.57x DPS all cost exactly 250%.
That IS the "one stat can be higher if the other is lower" rule, in closed form.

WHAT IT REPORTS, AND THE TRAP IT AVOIDS
---------------------------------------
⛔ A RAW max/min SPREAD IS A LIE ON A ROSTER WITH DATA BUGS. `artillery` reads 324x wide
on the raw spread and **5.9x** on the P10..P90 spread, because ONE member
(`futuretech_athenacannon`, DPS 193,600 — 24x the next artillery in the class) carries the
whole number. Judging the class from the raw figure would have condemned a class that is
actually within striking distance of the band. So this reports BOTH, leads with the
trimmed one, and lists the outliers as a triage queue rather than folding them into a
verdict. It also flags outright data bugs (a NEGATIVE dps means a heal/repair armament is
being counted as damage) which no amount of repricing can fix.
"""
from __future__ import annotations
import argparse, collections, json, math, pathlib, statistics, sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/balance"))
import check_band as cb  # noqa: E402

# The observed cost RESOLUTION of shipped OpenRA mods: the median ratio between adjacent
# distinct costs over 266 gaps in 14 mods (docs/design/ORIGINAL_UNITS_PEER_OPENRA.md,
# tools/reference/peer_cost_grid.py). Two units priced closer than this are not
# distinguishable to a player, so it is the natural rung width.
PEER_STEP = 1.143

# Outlier gates, on the ratio to the class MEDIAN price. Deliberately wide: this is a
# triage flag, not a band verdict, and a real heavy member must not be swept out.
OUT_HI, OUT_LO = 3.0, 1 / 3.0


def stat_window(price: float) -> float:
    """x such that a member with x times the anchor's HP and DPS costs `price` * cost0."""
    return (math.sqrt(1 + 48 * price) - 3) / 4


def collect_classes(anchors):
    rows = collections.defaultdict(list)
    for _fn, actor, u, du in cb.collect({}):
        cls = (u.get("design") or {}).get("class_anchor")
        if not cls or cls not in anchors or u.get("build_limit"):
            continue  # build-limited epics are band-exempt (check_band.py)
        inp = cb.unit_inputs(u, du)
        if inp is None or not all(inp[:4]):
            continue
        rows[cls].append((actor, inp))
    return rows


def priced(cls, rs):
    """Price every member off the FIRST member, at cost0=100. Anchor-invariant:
    the spread of a class does not depend on which member you price it from."""
    a0, i0 = rs[0]
    base = {"hp0": i0[0], "speed0": i0[1], "range0_wdist": i0[2],
            "dps0": i0[3], "cost0": 100.0}
    out = []
    for actor, inp in rs:
        p = cb.price_for(cls, {"spec": base}, inp, 1.0)
        if p and p > 0:
            out.append((p, actor, inp))
    return sorted(out)


def faction_of(actor: str) -> str:
    return actor.split("_")[0] if "_" in actor else actor.split(".")[-1]


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--md", help="also write the report here")
    ap.add_argument("--min-members", type=int, default=4)
    args = ap.parse_args()

    anchors = {k: v for k, v in json.loads(
        (ROOT / "docs/balance/class_anchors.json").read_text(encoding="utf-8")).items()
        if isinstance(v, dict)}
    rows = collect_classes(anchors)

    # ⚠ Read the rings from check_band, never re-derive them here. An earlier revision
    # hardcoded (2*0.75+1)(0.75+1)/6 and silently kept the OLD floor when the maintainer
    # ruled the rings are COST numbers -- two files then disagreed about the same law.
    sweet_lo, sweet_hi = cb.SWEET_LO, cb.SWEET_HI
    width = sweet_hi / sweet_lo
    rungs = math.log(width) / math.log(PEER_STEP)

    out = []
    w = out.append
    w("# Band granularity — does each class FIT the baseband, and at what resolution?\n")
    w(f"Target band **{sweet_lo:.2f} .. {sweet_hi:.2f}** of `cost0` "
      f"(= the anchor's HP and DPS times **{stat_window(sweet_lo):.3f} .. "
      f"{stat_window(sweet_hi):.2f}**), width **{width:.2f}x**. Hard band "
      f"**{cb.FLOOR:.2f} .. {cb.CEIL:.2f}** (= x{stat_window(cb.FLOOR):.2f} .. "
      f"x{stat_window(cb.CEIL):.2f} stats).\n")
    w(f"At the shipped-mod cost resolution of **{PEER_STEP}x** that band holds "
      f"**{rungs:.1f} distinct rungs**. A class with more members than rungs is NOT "
      f"overcrowded — peers deliberately price several units alike; what matters is that "
      f"the units sharing a rung come from DIFFERENT factions.\n")
    w("⛔ Read the TRIMMED spread, not the raw one. See the outlier list beneath.\n")
    hard_w = cb.CEIL / cb.FLOOR
    w(f"| class | n | factions | n/faction | raw spread | **P10..P90** | fits target "
      f"{width:.2f}x? | fits HARD {hard_w:.1f}x? | rungs used |")
    w("|---|--:|--:|--:|--:|--:|:-:|:-:|--:|")

    bugs, outliers, fits = [], [], 0
    hard_fits = [0]
    for cls in sorted(rows, key=lambda k: -len(rows[k])):
        rs = rows[cls]
        if len(rs) < args.min_members:
            continue
        pr = priced(cls, rs)
        if len(pr) < 2:
            continue
        facs = {faction_of(a) for _p, a, _i in pr}
        raw = pr[-1][0] / pr[0][0]
        lo_i, hi_i = int(0.1 * len(pr)), min(len(pr) - 1, int(0.9 * len(pr)))
        trim = pr[hi_i][0] / pr[lo_i][0]
        ok = trim <= width
        hard_ok = trim <= hard_w
        fits += ok
        hard_fits[0] += hard_ok
        w(f"| `{cls}` | {len(pr)} | {len(facs)} | {len(pr)/len(facs):.1f} | {raw:.1f}x | "
          f"**{trim:.1f}x** | {'YES' if ok else 'no'} | "
          f"{'**YES**' if hard_ok else '⛔ no'} | "
          f"{math.log(trim)/math.log(PEER_STEP):.0f} |")

        med = statistics.median([p for p, _a, _i in pr])
        for p, a, i in pr:
            if i[3] is not None and i[3] < 0:
                bugs.append((cls, a, f"NEGATIVE dps {i[3]:.1f} — a heal/repair armament is "
                                     f"being priced as damage"))
            elif p / med > OUT_HI or p / med < OUT_LO:
                outliers.append((cls, a, p / med, i))

    n_cls = sum(1 for c in rows if len(rows[c]) >= args.min_members)
    w(f"\nclasses whose trimmed spread FITS the **target** band: **{fits}** of {n_cls}")
    w(f"\nclasses whose trimmed spread FITS the **hard** band: "
      f"**{hard_fits[0]}** of {n_cls}\n")
    w("⭐ The gap between those two numbers is the actual work. A class inside the hard "
      "band is a REPRICING job — its members exist at plausible relative values and need "
      "to be pulled toward the anchor. A class outside it is a SCOPE question: those "
      "members may not belong in one class at all.\n")

    if bugs:
        w("## ⛔ DATA BUGS — no repricing can fix these\n")
        w("| class | actor | what |")
        w("|---|---|---|")
        for cls, a, why in bugs:
            w(f"| `{cls}` | `{a}` | {why} |")
        w("")

    if outliers:
        w(f"## Outliers — the triage queue ({len(outliers)} members outside "
          f"{OUT_LO:.2f}x..{OUT_HI:.1f}x of their class median)\n")
        w("| class | actor | x class median | hp | dps | range |")
        w("|---|---|--:|--:|--:|--:|")
        for cls, a, ratio, i in sorted(outliers, key=lambda t: -t[2]):
            w(f"| `{cls}` | `{a}` | **{ratio:.2f}x** | {i[0]:,.0f} | {i[3]:,.1f} | {i[2]:,.0f} |")
        w("")

    text = "\n".join(out)
    print(text)
    if args.md:
        pathlib.Path(args.md).write_text(text + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
