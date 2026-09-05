#!/usr/bin/env python3
"""Which reference sources are the SAME DATA at a different scale — measured, not asserted.

PRIOR ART: `synthesize_reference.py` carries two hand-written lists (`SUPERSEDED`,
`NEAR_DUPLICATES`) and `reference_distribution.py` carries a third (`LINEAGE_MEMBERS`). All three
were typed from judgement, none was measured corpus-wide, and they disagree with each other. This
module does not add a fourth: it MEASURES every pair and becomes the one list the others import.

    python tools/balance/lineage_dedup.py              # the pairwise table + the lineages
    python tools/balance/lineage_dedup.py --pair "Tiberian Sun" "OpenRA Tiberian Sun"
    python tools/balance/lineage_dedup.py --all-pairs  # every pair, not just the duplicates

MAINTAINER ORDER 2026-09-03
---------------------------
*"You need to use all the reference data and checking if the data you have is any duplicates like
for example the original TD and RA1 rules and the OpenRA rules are identical and just scaled
right? ... All data needs to be unique and then used as a geometric mean for the design and then
normalized to the new cameo scale."*

So dedup is step 1 of the synthesis, ahead of the geometric mean — a source that votes twice
weights its lineage twice, and the geometric mean has no defence against that.

THE TEST, AND WHY IT IS SCALE-FREE BY CONSTRUCTION
--------------------------------------------------
Every row is already `×rifle` — the unit's HP over its own source's basic rifleman — so two
rosters that are "identical and just scaled" have IDENTICAL coordinates, not merely proportional
ones. On top of that the pair's MEDIAN ratio is divided out before agreement is scored, so a
source that sits uniformly 1.92× above another still reads as a duplicate. That is deliberate:
"just scaled" is exactly the case the maintainer named, and a test that failed it would fail the
one case it was asked to catch.

    dev_u = (A_u / B_u) / median_v(A_v / B_v)        # 1.0 when the two agree on unit u

  * `w10` — the share of shared units within ±10% of the pair's own median offset.
  * `w25` — the same at ±25%, which controls the TAIL. `w10` alone passes a pair that agrees on
    the bulk and disagrees violently on six units, and that is a rebalance, not a copy.

⛔ HP ONLY, AND THAT IS A LIMIT NOT A CHOICE. `×rifle` is the only coordinate every one of the
three source documents carries for every row. Two mods can share HP and diverge completely on
damage, so a DUPLICATE verdict here means "the same chassis data", not "the same balance". It is
still the right cut for the chassis layer, which is the layer that consumes it.

WHAT A COLLAPSE DOES
--------------------
It SELECTS A REPRESENTATIVE and DROPS the others. It does not relabel and merge — merging pours
rosters at incompatible raw scales into one distribution, which is the failure
`reference_distribution.py`'s own header forbids. The representative rule, from the maintainer's
RA2 precedent (Romanov's Vengeance was elected over four vanilla copies): the LIVE, RESOLVABLE
codebase wins over a hand-extracted table, because it can be re-derived.
"""
from __future__ import annotations

import argparse
import collections
import itertools
import math
import pathlib
import statistics
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import reference_lineages  # noqa: E402  (the rulings — data only, no imports of its own)
import synthesize_reference as syn  # noqa: E402  (parsers reused wholesale — never re-forked)

# ── Thresholds ────────────────────────────────────────────────────────────────────────────────
# Calibrated against the ONE lineage a maintainer has already ruled on: the five RA2-family copies
# agree at 92-100% `w10`, and every pair the maintainer treats as independent sits at 79% or below.
# The gap between those two populations is wide (79 -> 92), so the cut lands in empty space rather
# than through a cluster. MIN_SHARED exists because agreement over 9 units is noise: `Red Alert 1`
# and `Tiberian Dawn` — different games — read 70% over 10 shared names.
MIN_SHARED = 15
DUP_W10 = 0.85
DUP_W25 = 0.90
PARTIAL_W10 = 0.65

# ── The standing maintainer rulings ───────────────────────────────────────────────────────────
# ⛔ A RULING IS NOT A MEASUREMENT, AND THE TWO MUST BE ABLE TO DISAGREE OUT LOUD.
# The rulings live in `reference_lineages.py` so `synthesize_reference` and `reference_distribution`
# read the same list; this module MEASURES and then checks that list against the corpus. The RA2
# ruling elects Romanov's Vengeance — a source this test does NOT place in that lineage, because RV
# is a real rebalance. That is the maintainer's call and it stands; what must never happen is the
# code quietly re-deriving a different answer and nobody noticing.
RULED_LINEAGES = reference_lineages.RULED_LINEAGES

# Preference order used to SUGGEST a representative for a lineage the measurement found and no
# ruling covers. The rule comes from the RA2 precedent: the live, resolvable codebase wins over a
# hand-extracted table, because it can be re-derived rather than trusted.
REPRESENTATIVE_PREFERENCE = ("Romanov's Vengeance", "OpenRA Tiberian Sun",
                             "OpenRA Tiberian Dawn", "OpenRA Red Alert")


def corpus():
    """{source: {normalised unit: x_hp}} across all three reference documents.

    ⚠ `setdefault` not `[]=`: a source listing the same unit twice (a Classic/Enhanced pair, an
    Allied and a Soviet variant of one name) must vote once, and the FIRST row wins so the result
    does not depend on file order downstream.
    """
    rows = []
    for source, row in syn.parse_doc1():
        rows.append({"source": source, "unit": row.get("Unit"),
                     "x_hp": syn.num(row.get("×rifle"))})
    rows += syn.parse_doc4()
    rows += syn.parse_doc5()
    out = collections.defaultdict(dict)
    for row in rows:
        if row.get("x_hp") and row.get("unit") and row["x_hp"] > 0:
            out[row["source"]].setdefault(syn.norm(row["unit"]), row["x_hp"])
    return dict(out)


def compare(a_rows, b_rows):
    """(n, median_offset, w10, w25, geo_sd) or None when the two share too little to judge."""
    shared = [u for u in set(a_rows) & set(b_rows) if b_rows[u]]
    if len(shared) < MIN_SHARED:
        return None
    ratios = [a_rows[u] / b_rows[u] for u in shared]
    median = statistics.median(ratios)
    dev = [r / median for r in ratios]
    # ⛔ SCORED IN LOG SPACE, AND THAT IS NOT A STYLE CHOICE. Written as `0.9 <= d <= 1/0.9` the
    # band is mathematically symmetric and NUMERICALLY IS NOT: a unit sitting exactly on it (TS
    # Stealth Tank, 1.60/1.44) passes as 0.9 in one direction and fails as 1.1111112 > 1.1111111
    # in the other, so `compare(a, b)` and `compare(b, a)` disagreed — 96% vs 93% on the same
    # pair, one unit of 27. A verdict that depends on argument order is not a measurement.
    EPS = 1e-9
    logs = [abs(math.log(d)) for d in dev]
    w10 = sum(1 for l in logs if l <= math.log(1 / 0.9) + EPS) / len(logs)
    w25 = sum(1 for l in logs if l <= math.log(1.25) + EPS) / len(logs)
    geo_sd = math.exp(statistics.pstdev([math.log(d) for d in dev]))
    return len(shared), median, w10, w25, geo_sd


def verdict(stats):
    if stats is None:
        return "too-few-shared"
    _, _, w10, w25, _ = stats
    if w10 >= DUP_W10 and w25 >= DUP_W25:
        return "DUPLICATE"
    if w10 >= PARTIAL_W10:
        return "partial"
    return "independent"


def lineages(pairs):
    """Union-find the DUPLICATE pairs into lineages.

    ⚠ Transitivity is ASSUMED here and it is not free: A~B and B~C at 85% does not guarantee A~C.
    It is the right assumption for a lineage — descent from one roster is transitive even when the
    measurement of it is noisy — but the per-pair table is printed so a group that only holds
    together through one weak link is visible rather than hidden inside the union.
    """
    parent = {}

    def find(x):
        parent.setdefault(x, x)
        while parent[x] != x:
            parent[x] = parent[parent[x]]
            x = parent[x]
        return x

    for a, b in pairs:
        ra, rb = find(a), find(b)
        if ra != rb:
            parent[ra] = rb
    groups = collections.defaultdict(set)
    for node in parent:
        groups[find(node)].add(node)
    return [sorted(g) for g in groups.values() if len(g) > 1]


def representative(group):
    """The lineage's suggested voice, or None when no preference covers it."""
    for name in REPRESENTATIVE_PREFERENCE:
        if name in group:
            return name
    return None


def audit_rulings(data, dup_pairs):
    """Every way a standing ruling can be wrong, reported separately because the fixes differ."""
    measured = {frozenset(g) for g in lineages(dup_pairs)}
    problems = {"unknown_label": [], "not_measured": [], "unruled": []}
    ruled_members = set()
    for label in reference_lineages.all_labels():
        if label not in data:
            problems["unknown_label"].append(("reference_lineages", label))
    for rep, members in RULED_LINEAGES.items():
        ruled_members |= members | {rep}
        pairs = [(a, b) for a, b in itertools.combinations(sorted(members), 2)]
        for a, b in pairs:
            if a in data and b in data and (a, b) not in dup_pairs and (b, a) not in dup_pairs:
                stats = compare(data[a], data[b])
                problems["not_measured"].append((a, b, verdict(stats), stats))
    for group in measured:
        if not (group & ruled_members):
            problems["unruled"].append(sorted(group))
    return problems


def measure():
    data = corpus()
    rows = []
    for a, b in itertools.combinations(sorted(data), 2):
        stats = compare(data[a], data[b])
        if stats is None:
            continue
        rows.append((a, b, stats, verdict(stats)))
    dup_pairs = [(a, b) for a, b, _, v in rows if v == "DUPLICATE"]
    return data, rows, dup_pairs, lineages(dup_pairs)


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--all-pairs", action="store_true",
                    help="print every comparable pair, not only DUPLICATE and partial")
    ap.add_argument("--pair", nargs=2, metavar=("A", "B"),
                    help="unit-by-unit detail for one pair")
    args = ap.parse_args()

    data, rows, dup_pairs, groups = measure()

    if args.pair:
        a, b = args.pair
        for name in (a, b):
            if name not in data:
                print(f"⛔ no source named {name!r}. Known: {', '.join(sorted(data))}")
                return 2
        shared = sorted(set(data[a]) & set(data[b]),
                        key=lambda u: -abs(math.log(data[a][u] / data[b][u])))
        stats = compare(data[a], data[b])
        print(f"{a}  vs  {b}   verdict: {verdict(stats)}")
        if stats:
            n, med, w10, w25, gsd = stats
            print(f"  shared {n} · median offset {med:.3f}× · within 10% {w10:.0%} · "
                  f"within 25% {w25:.0%} · geo-SD {gsd:.2f}")
        print(f"\n  {'unit':<30}{a[:12]:>12}{b[:12]:>12}{'ratio':>8}")
        for u in shared:
            r = data[a][u] / data[b][u]
            mark = "" if 0.9 <= r <= 1 / 0.9 else "   <-- differs"
            print(f"  {u[:30]:<30}{data[a][u]:>12.2f}{data[b][u]:>12.2f}{r:>8.2f}{mark}")
        return 0

    print(f"reference corpus: {len(data)} source labels, "
          f"{sum(len(v) for v in data.values())} rows carrying ×rifle HP\n")
    shown = rows if args.all_pairs else [r for r in rows if r[3] in ("DUPLICATE", "partial")]
    shown.sort(key=lambda r: (r[3] != "DUPLICATE", -r[2][2]))
    print(f"{'n':>4}{'offset':>9}{'w10':>7}{'w25':>7}{'geoSD':>8}  verdict     pair")
    for a, b, (n, med, w10, w25, gsd), v in shown:
        print(f"{n:>4}{med:>9.2f}{w10:>7.0%}{w25:>7.0%}{gsd:>8.2f}  {v:<11} {a}  ~  {b}")
    if not args.all_pairs:
        hidden = len(rows) - len(shown)
        print(f"\n({hidden} further comparable pairs read 'independent' — --all-pairs to see them)")

    sizes = {k: len(v) for k, v in data.items()}
    print("\n── MEASURED lineages: sources this test says are one roster ──")
    for group in sorted(groups, key=lambda g: -len(g)):
        print(f"\n  {' + '.join(group)}")
        rep = representative(group)
        ruled = [r for r, m in RULED_LINEAGES.items() if set(group) & (m | {r})]
        if ruled:
            print(f"    covered by the standing ruling -> {ruled[0]}")
        elif rep:
            print(f"    ⛔ NO RULING COVERS THIS LINEAGE. Suggested representative: {rep} "
                  f"({sizes[rep]} rows), dropping {', '.join(g for g in group if g != rep)}.")
        else:
            print("    ⛔ NEEDS A MAINTAINER RULING — no member is a recorded preference.")
            print(f"       candidates by roster size: "
                  f"{', '.join(f'{g} ({sizes[g]})' for g in sorted(group, key=lambda g: -sizes[g]))}")
    if not groups:
        print("  none")

    print("\n── The standing rulings, checked against the measurement ──")
    problems = audit_rulings(data, dup_pairs)
    for rep, label in problems["unknown_label"]:
        print(f"  ⛔ ruling {rep!r} names {label!r}, which is NOT a source label in the corpus — "
              f"that member has never been collapsed.")
    for a, b, v, stats in problems["not_measured"]:
        n = stats[0] if stats else 0
        print(f"  ⚠ ruled together but measures {v}: {a} ~ {b} (n={n})")
    for group in problems["unruled"]:
        print(f"  ⛔ measured as one lineage and covered by NO ruling: {' + '.join(group)}")
    if not any(problems.values()):
        print("  the rulings and the measurement agree.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
