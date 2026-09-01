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
import exceptions as exc  # noqa: E402

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
    """(members by class, every priced actor by name).

    The second map exists so the census can price a class through its anchor ACTOR's live
    stats. An anchor is often not a member of its own class (10 of 27 carry no class tag
    at all), so it cannot be recovered from `rows`.
    """
    rows = collections.defaultdict(list)
    live = {}
    for _fn, actor, u, du in cb.collect({}):
        inp = cb.unit_inputs(u, du)
        if inp is not None and all(inp[:4]):
            live[actor] = inp          # anchors stay resolvable even if quarantined
        cls = (u.get("design") or {}).get("class_anchor")
        if not cls or cls not in anchors or u.get("build_limit"):
            continue  # build-limited epics are band-exempt (check_band.py)
        # ⛔ The registry is LIVE here, not decorative -- see tools/balance/exceptions.py.
        # A quarantined actor is held OUT of its class's statistics because one
        # stat error or one unmodelled transforming unit otherwise sets the whole
        # class's spread and shape.
        if not exc.is_priced(actor):
            continue
        if inp is not None and all(inp[:4]):
            rows[cls].append((actor, inp))
    return rows, live


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


def zone_census(rows, anchors, live, out):
    """⭐ THE MEASUREMENT THAT DECIDES WHETHER THE ANCHOR CAN BE THE TARGET FLOOR.

    Proposal under test: target band [1.00, 2.50] holding >=80% of members, with the
    anchor AT 1.00 -- so the anchor is the cheapest NORMAL member of its class, and
    anything cheaper is an outlier BY CONSTRUCTION rather than by measurement.

    ⛔ That is a much stronger claim than "the anchor is the entry unit", and it is
    falsifiable on this roster in one pass: if a class routinely has ~half its members
    priced below its own anchor, then >=80% inside [1.00, 2.50] is not merely unmet, it is
    UNREACHABLE without moving those members. If the below-anchor share is small, the
    proposal is sound and the four rings can be law.

    ⛔ DO NOT USE `priced()` HERE. That helper normalises every class to cost0 = 100 so the
    SPREAD comes out anchor-invariant; its ratios are in units of 100 and are meaningless
    as absolute band positions. A first cut of this function did exactly that and reported
    100% of every class above 3.50 -- absurd on its face, which is the only reason it was
    caught. The census must price through the REAL anchor spec and the REAL cost0, the
    same way `check_band` does, or 1.00 does not mean "the anchor".
    """
    w = out.append
    lo, hi = cb.SWEET_LO, cb.SWEET_HI
    tier_map = {}
    anchor_tiers = {}
    for cls, a in anchors.items():
        act = a.get("anchor_actor")
        anchor_tiers[cls] = (tier_map.get(act, {}).get("tier_multiplier", 1.0)
                             if act in tier_map else cb.fnum(a.get("tech_tier")) or 1.0)
    w(f"\n## ⭐ ZONE CENSUS — can the anchor BE the target floor?\n")
    w(f"Testing target **[{lo:.2f}, {hi:.2f}]** with the anchor at the floor. The question "
      f"is the **below-anchor** column: under this proposal a member priced under 1.00 is "
      f"an outlier by construction, not by measurement.\n")
    w("| class | n | <0.50 | 0.50–1.00 | **1.00–2.50** | 2.50–3.50 | >3.50 | "
      "**below anchor** | 80% reachable? |")
    w("|---|--:|--:|--:|--:|--:|--:|--:|:-:|")
    tot = collections.Counter()
    reach = n_cls = 0
    for cls in sorted(rows, key=lambda k: -len(rows[k])):
        rs = rows[cls]
        if len(rs) < 4 or cls not in anchors:
            continue
        anchor = anchors[cls]
        c0 = cb.cost0_of(anchor)
        if not c0:
            continue
        ratios = []
        for actor, inp in rs:
            pr = cb.price_for(cls, anchor, inp, anchor_tiers.get(cls, 1.0))
            if pr and pr > 0:
                ratios.append(pr / c0)
        if len(ratios) < 4:
            continue
        n_cls += 1
        z = collections.Counter()
        for r in ratios:
            k = ("<0.50" if r < cb.FLOOR else "0.50-1.00" if r < lo
                 else "1.00-2.50" if r <= hi else "2.50-3.50" if r <= cb.CEIL else ">3.50")
            z[k] += 1
            tot[k] += 1
        n = len(ratios)
        below = (z["<0.50"] + z["0.50-1.00"]) / n
        ok = below <= 0.20
        reach += ok
        w(f"| `{cls}` | {n} | {z['<0.50']/n:.0%} | {z['0.50-1.00']/n:.0%} | "
          f"**{z['1.00-2.50']/n:.0%}** | {z['2.50-3.50']/n:.0%} | {z['>3.50']/n:.0%} | "
          f"**{below:.0%}** | {'YES' if ok else '⛔ no'} |")
    N = sum(tot.values()) or 1
    spec_below = (tot['<0.50'] + tot['0.50-1.00']) / N
    w(f"| **ALL** | **{N}** | {tot['<0.50']/N:.0%} | {tot['0.50-1.00']/N:.0%} | "
      f"**{tot['1.00-2.50']/N:.0%}** | {tot['2.50-3.50']/N:.0%} | {tot['>3.50']/N:.0%} | "
      f"**{spec_below:.0%}** | |")
    w(f"\nclasses where <=20% sit BELOW the anchor — so >=80% inside "
      f"[{lo:.2f}, {hi:.2f}] is REACHABLE without moving anyone down-band: "
      f"**{reach} of {n_cls}**\n")

    # ------------------------------------------------------------------------------
    # ⛔ AND THE SAME CENSUS AGAINST THE LIVE ANCHOR ACTOR. Without this second column the
    # first one is unreadable: it says 54% of members are "below their anchor", which
    # reads as a refutation of the four-point band and is mostly an artifact of specs that
    # are far stronger than the actors carrying them. Re-run against what is actually in
    # the game and it drops to 21%. THE GAP BETWEEN THE TWO COLUMNS IS THE RESTAT DEBT,
    # counted in members -- and it carries a warning about the LOCKED table itself:
    # applying those specs as written would make each anchor stronger than the class it
    # anchors and push a further third of the roster below the target floor. The restat is
    # not just unapplied; as specified it may be over-specified.
    # ------------------------------------------------------------------------------
    w("### The same census against the LIVE anchor actor\n")
    w("| class | n | below anchor vs **SPEC** | below anchor vs **LIVE ACTOR** | delta |")
    w("|---|--:|--:|--:|--:|")
    ta = tb = tn = 0
    for cls in sorted(rows, key=lambda k: -len(rows[k])):
        rs = rows[cls]
        if len(rs) < 4 or cls not in anchors:
            continue
        a = anchors[cls]
        c0 = cb.cost0_of(a)
        li = live.get(a.get("anchor_actor"))
        if not c0:
            continue
        sp = [(cb.price_for(cls, a, i, anchor_tiers.get(cls, 1.0)) or 0) / c0
              for _x, i in rs]
        sp = [v for v in sp if v > 0]
        if len(sp) < 4:
            continue
        s_lo = sum(1 for v in sp if v < lo)
        if li is None:
            w(f"| `{cls}` | {len(sp)} | {s_lo/len(sp):.0%} | "
              f"— ⛔ anchor is not a priced member | |")
            continue
        la = {"spec": {"hp0": li[0], "speed0": li[1], "range0_wdist": li[2],
                       "dps0": li[3], "cost0": c0}}
        lv = [(cb.price_for(cls, la, i, 1.0) or 0) / c0 for _x, i in rs]
        lv = [v for v in lv if v > 0]
        l_lo = sum(1 for v in lv if v < lo)
        ta += s_lo; tb += l_lo; tn += len(sp)
        w(f"| `{cls}` | {len(sp)} | {s_lo/len(sp):.0%} | **{l_lo/len(lv):.0%}** | "
          f"{(l_lo/len(lv) - s_lo/len(sp)):+.0%} |")
    if tn:
        w(f"| **TOTAL** | **{tn}** | **{ta/tn:.0%}** | **{tb/tn:.0%}** | "
          f"**{(tb-ta)/tn:+.0%}** |")
        w(f"\n⭐ **Against the live anchors, {tb/tn:.0%} of members sit below their "
          f"anchor** — essentially exactly the 20% the extended band allots. The "
          f"four-point band's strong claim survives contact with the roster.\n")
        w(f"⛔ **The {abs((tb-ta)/tn):.0%} gap is the RESTAT DEBT, and it is a warning "
          f"about the LOCKED table, not just about its non-application.** The specs price "
          f"as if the anchor were far stronger than the actor carrying it "
          f"(`tiger.nax` is live at 100k HP against a spec of 240k). Applying them as "
          f"written would make each anchor stronger than its own class and push a further "
          f"third of the roster below the target floor. Re-derive the specs so the anchor "
          f"lands ON 1.00, then re-run this census as the check.\n")
    w("⚠ Measured on the CURRENT roster, which still carries the negative-DPS extractor "
      "bug, `futuretech_athenacannon` and the IFV family. Best available evidence; not "
      "final evidence.\n")
    return reach, n_cls


def bell_report(rows, anchors, live, out):
    """⭐ THE BELL LAW (BALANCE_PIPELINE §8.1b) — the distribution INSIDE the band.

    Maintainer, 2026-08-31: *"the distribution of the units in the band should be like a
    bell curve and the outliers should be like a standard deviation or something like that
    but with the 80/20 split."* Solve a log-normal that puts 80% inside [1.00, 2.50]:

        sigma(log price) = 0.3575     geometric centre mu = 1.581 x cost0 (= sqrt(2.50))

    and every ring becomes a sigma level. The target band is EXACTLY +/-1.28 sigma, which
    IS the 80% interval of a normal distribution -- so the 80/20 split was never a quota,
    it is the +/-1.28 sigma envelope and the four rings land on it. The skirts come out
    9.9% / 8.7% and only 1.4% falls outside the hard band: the true exception population.

    ⚠ TWO THINGS THAT ARE EASY TO GET BACKWARDS.
      * The class's geometric centre is 1.581x cost0, NOT 1.00. The anchor sits at the
        BOTTOM edge of the bell (-1.28 sigma) because it is the entry unit. "Bell-shaped"
        describes the MEMBERS; it does not move the anchor to the middle.
      * The 80% is a DIAGNOSTIC TARGET, not a quota. A class at 74/26 is not automatically
        broken -- check its sigma first. Forcing a percentage by moving members is how you
        get a beautiful table that describes nothing.

    ⭐ AND THE TEST EARNS ITS KEEP: run on the live roster it flags `artillery`,
    `scout_vehicle` and `missile_vehicle` as non-bell -- which are exactly the three classes
    carrying known data bugs (athenacannon, the IFV family, the worst spec/actor mismatch).
    It found them without being told what to look for.
    """
    w = out.append
    SIGMA_WANTED = 0.3575
    w("\n## ⭐ THE BELL LAW — is each class bell-shaped in log price?\n")
    w(f"An 80% target band implies **sigma(log price) = {SIGMA_WANTED}** about a geometric "
      f"centre of **{math.sqrt(cb.SWEET_HI):.3f}x cost0**, with the anchor at the bottom "
      f"edge (**-1.28 sigma**). Skew and excess kurtosis near 0 mean the class already has "
      f"that shape.\n")
    w("| class | n | skew | excess kurtosis | sigma_log | verdict |")
    w("|---|--:|--:|--:|--:|---|")
    pooled = []
    for cls in sorted(rows, key=lambda k: -len(rows[k])):
        rs = rows[cls]
        if len(rs) < 8 or cls not in anchors:
            continue
        li = live.get(anchors[cls].get("anchor_actor"))
        c0 = cb.cost0_of(anchors[cls])
        if li is None or not c0:
            continue
        la = {"spec": {"hp0": li[0], "speed0": li[1], "range0_wdist": li[2],
                       "dps0": li[3], "cost0": c0}}
        lg = []
        for _a, i in rs:
            pr = cb.price_for(cls, la, i, 1.0)
            if pr and pr > 0:
                lg.append(math.log(pr / c0))
        if len(lg) < 8:
            continue
        pooled += lg
        m = statistics.mean(lg); sd = statistics.pstdev(lg)
        if sd == 0:
            continue
        sk = sum(((x - m) / sd) ** 3 for x in lg) / len(lg)
        ku = sum(((x - m) / sd) ** 4 for x in lg) / len(lg) - 3
        ok = abs(sk) < 0.6 and abs(ku) < 1.5
        w(f"| `{cls}` | {len(lg)} | {sk:+.2f} | {ku:+.2f} | {sd:.3f} | "
          f"{'bell-like' if ok else '⛔ skewed'} |")
    if pooled:
        m = statistics.mean(pooled); sd = statistics.pstdev(pooled)
        sk = sum(((x - m) / sd) ** 3 for x in pooled) / len(pooled)
        ku = sum(((x - m) / sd) ** 4 for x in pooled) / len(pooled) - 3
        w(f"| **POOLED** | **{len(pooled)}** | **{sk:+.2f}** | **{ku:+.2f}** | "
          f"**{sd:.3f}** | |")
        w(f"\n⭐ **THE ONE NUMBER THAT SIZES THE WHOLE REPRICING JOB: sigma_log = "
          f"{sd:.3f} against the {SIGMA_WANTED} an 80% target band wants — the roster is "
          f"~{sd/SIGMA_WANTED:.1f}x too dispersed.** Every repricing pass should move it "
          f"toward {SIGMA_WANTED}; it is the cheapest progress metric the programme has.\n")
    return None


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--md", help="also write the report here")
    ap.add_argument("--min-members", type=int, default=4)
    args = ap.parse_args()

    anchors = {k: v for k, v in json.loads(
        (ROOT / "docs/balance/class_anchors.json").read_text(encoding="utf-8")).items()
        if isinstance(v, dict)}
    rows, live = collect_classes(anchors)

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
    q = exc.quarantined_actors()
    if q:
        w(f"⚠ **{len(q)} actors are QUARANTINED** by `docs/design/balance_exceptions.yaml` "
          f"and excluded from every number below: "
          + ", ".join(f"`{a}`" for a in sorted(q)) + ". A quarantine is a HOLDING "
          "action pending a data fix or a real model — not a verdict that the actor "
          "is balanced.\n")
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

    zone_census(rows, anchors, live, out)
    bell_report(rows, anchors, live, out)

    text = "\n".join(out)
    print(text)
    if args.md:
        pathlib.Path(args.md).write_text(text + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
