#!/usr/bin/env python3
"""Documents 2 and 3 of the three-document program (BALANCE_SYNTHESIS §15).

PRIOR ART: `tools/reference/aggregate_archetype.py` pools ONE WEAPON CONCEPT's Versus
profile across mods (W13). This pools UNIT STATS across sources. Different axis, no overlap.
`ORIGINAL_UNITS_RAW.md` (Document 1) is the input and is not regenerated here.

    python tools/balance/synthesize_reference.py            # write both documents
    python tools/balance/synthesize_reference.py --class mbt --dry-run

WHY THIS EXISTS
---------------
The PER-UNIT APPLICATION LAW (`anchor_decisions_log.md`, maintainer 2026-07-31) has three
steps, and only the first two were ever built:

  1. set the BASELINE ACTOR of each class to its ruled stats     (step 2c)
  2. the FORMULA takes its weights from that baseline            (`fit_class`)
  3. each MEMBER's stats come from SYNTHESIS — the old Cameo values, **every relative stat
     from the cross-game/mod data-mining**, and where the unit sits relative to its baseline

Step 3 is the "massive, compute-intensive pass" the law calls *the real 'apply the class'
work*. `BALANCE_SYNTHESIS.md` §15 specifies it as three documents; §16 proves it end-to-end
on the Apocalypse Tank. Document 1 exists. Documents 2 and 3 never got written — §15 ends
with "Next: run this generation over all units (tooling)". This is that tooling.

THE FOUR RULES THAT MAKE IT NOT-A-MEAN (§15.6)
----------------------------------------------
1. **A faithful REMAKE is not an independent vote.** Romanov's Vengeance reproduces vanilla
   RA2's ratios exactly, so counting it separately DOUBLE-COUNTS vanilla. The vanilla family
   (RA2 + Yuri's Revenge + RV) is pooled to ONE vote; the independent voices are the
   rebalances (Mental Omega, CnC Reloaded).
2. **Normalize each source to its OWN rifle before pooling** — every source runs a different
   absolute scale (RV's rifle is 12500 HP, Mental Omega's 205, vanilla's 125). Document 1
   carries the per-source `×rifle` column precisely so this is not re-derived per reader.
3. **Flag wide-gap outliers.** A source running a deliberately wider infantry-vehicle spread
   (§15.6.3 cites Combined Arms at 26× rifle against a 6.4× consensus) must not drag the
   compromise. A vote more than OUTLIER_FACTOR from the median of the votes is reported and
   excluded from the target, never silently averaged in.
4. **The compromise is a judgement, not a mean.** The median of the weighted votes is the
   defensible mechanical stand-in: it survives one bad source, which an arithmetic mean does
   not, and it never invents a value no source proposed.

⚠ WHAT THIS DOES NOT DO. It does not touch Versus or damage magnitude. §15.4 is explicit that
raw Versus is NOT comparable across sources (Westwood routinely >100, DTA ×10 again, every
OpenRA mod its own scale) and must never be numerically averaged — only a warhead's relative
ORDER carries identity, and that is `aggregate_archetype.py`'s job. HP, cost and speed are
pooled here because they normalize cleanly to a rifle.

⚠ IT WRITES NO LEDGER AND NO YAML. Output is two documents. Step 3 of the law is a design
pass and a synthesized target is a PROPOSAL for the maintainer, not an applied number.
"""
import argparse
import collections
import json
import glob
import pathlib
import re
import statistics

ROOT = pathlib.Path(__file__).resolve().parents[2]
LEDGER = ROOT / "docs" / "balance"
DESIGN = ROOT / "docs" / "design"
DOC1 = DESIGN / "ORIGINAL_UNITS_RAW.md"
DOC2 = DESIGN / "ORIGINAL_UNITS_NORMALIZED.md"
DOC3 = DESIGN / "SYNTHESIS_DELTA.md"

# Cameo's normalization anchor — the `scout` class spec in class_anchors.json, which IS the
# basic rifle infantryman. BALANCE_SYNTHESIS §5/§12.2: "Cameo's rifle anchor = 20000 HP = 1.00x".
RIFLE_HP, RIFLE_COST, RIFLE_SPEED = 20000, 100, 60

# §15.6.1 — the vanilla family votes ONCE. RV is a faithful remake and YR is vanilla's own
# expansion pack; neither is an independent rebalance of vanilla's ratios.
VANILLA_FAMILY = {"RA2 vanilla", "Yuri's Revenge", "Romanov's Veng."}
INDEPENDENT = {"Mental Omega", "CnC Reloaded"}

# §15.6.3 — how far from the median of the votes a source may sit before it is called an
# outlier and dropped from the target. 2.5x is chosen to catch the deliberate-wide-spread case
# the spec names (26x against a 6.4x consensus is 4.1x out) without discarding ordinary
# disagreement between two rebalances.
OUTLIER_FACTOR = 2.5

# An absolute sanity ceiling on a source row, in multiples of that source's own rifle.
# The relative outlier rule above compares VOTES, so it cannot help when a unit appears in only
# one source — and Document 1 contains at least one junk row: `Virus` at 114,514 HP = 558.6x
# rifle (114514 is a well-known joke number, and the row carries no weapon at all). Left in, it
# proposed an 11,172,000 HP target for `yuri_virus`. The widest DELIBERATE spread the spec names
# is Combined Arms at 26x (§15.6.3) and the epic cap is 6-8x (§12.4), so 60x is far above any
# real design intent and far below the junk.
MAX_XRIFLE = 60.0

# A reference name must be at least this long to match by prefix — below it, a name is too
# generic to be evidence about a specific actor.
MIN_KEY = 4


def norm(name):
    return re.sub(r"[^a-z0-9]", "", (name or "").lower())


def parse_doc1():
    """[(source, {column: cell})] from Document 1."""
    rows, source, header = [], None, None
    for line in DOC1.read_text(encoding="utf-8").splitlines():
        if line.startswith("## "):
            source = line[3:].split("(")[0].strip()
            continue
        if not line.startswith("|") or "---" in line:
            continue
        cells = [c.strip() for c in line.split("|")[1:-1]]
        if cells and cells[0] == "Unit":
            header = cells
            continue
        if header and len(cells) == len(header):
            rows.append((source, dict(zip(header, cells))))
    return rows


def num(cell):
    if cell is None:
        return None
    s = str(cell).strip().strip("`*").split()[0] if str(cell).strip() else ""
    s = s.replace(",", "")
    try:
        return float(s)
    except ValueError:
        return None


def rifle_of(rows, source):
    """Each source's own rifle stats, for §15.6.2 per-source normalization.

    Document 1 already carries `×rifle` for HP, but not for cost or speed, so those are
    normalized here against the source's own basic rifleman — found by the row whose ×rifle
    is 1.0, which is how Document 1 defines the anchor for that source.
    """
    best = None
    for src, r in rows:
        if src != source:
            continue
        if abs((num(r.get("×rifle")) or 0) - 1.0) < 1e-9:
            cost, spd = num(r.get("Cost")), num(r.get("Spd"))
            if cost and spd:
                best = {"cost": cost, "speed": spd}
                break
    return best


def votes_for(per_source, key):
    """§15.6.1 — collapse the vanilla family to one vote, keep each rebalance as its own."""
    fam = [v[key] for s, v in per_source.items() if s in VANILLA_FAMILY and v.get(key)]
    out = {}
    if fam:
        out["vanilla family"] = statistics.median(fam)
    for s, v in per_source.items():
        if s in INDEPENDENT and v.get(key):
            out[s] = v[key]
    for s, v in per_source.items():                      # any source the spec did not name
        if s not in VANILLA_FAMILY and s not in INDEPENDENT and v.get(key):
            out[s] = v[key]
    return out


def compromise(votes):
    """§15.6.3 + §15.6.4 — drop wide-gap outliers, then take the median of what is left.

    Returns (target, [outlier names]). The median is used rather than a mean because a mean
    lets one deliberately-wide source drag a value no source actually proposed.
    """
    if not votes:
        return None, []
    vals = list(votes.values())
    med = statistics.median(vals)
    outliers = [s for s, v in votes.items()
                if med > 0 and (v / med > OUTLIER_FACTOR or med / v > OUTLIER_FACTOR)]
    kept = [v for s, v in votes.items() if s not in outliers]
    return (statistics.median(kept) if kept else med), outliers


def load_roster():
    """{actor: record} plus {actor: class} over every ledger."""
    units, cls = {}, {}
    for f in sorted(glob.glob(str(LEDGER / "*.json"))):
        if "class_anchors" in f:
            continue
        try:
            doc = json.loads(pathlib.Path(f).read_text(encoding="utf-8"))
        except ValueError:
            continue
        for sec in (doc.get("sections") or {}).values():
            if not isinstance(sec, dict):
                continue
            for name, rec in sec.items():
                if isinstance(rec, dict) and "cost" in rec:
                    units[name] = rec
                    ca = (rec.get("design") or {}).get("class_anchor")
                    if ca:
                        cls[name] = ca
    return units, cls


def val(rec, field):
    slot = rec.get(field)
    if isinstance(slot, dict):
        return num(slot.get("v"))
    return num(slot)


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--class", dest="cls", help="restrict Document 3 to one class")
    ap.add_argument("--dry-run", action="store_true", help="report, write nothing")
    args = ap.parse_args()

    rows = parse_doc1()
    sources = sorted({s for s, _ in rows})
    rifles = {s: rifle_of(rows, s) for s in sources}

    # ---- Document 2: every (unit x source) row on Cameo's scale
    norm_rows, junk = [], []
    for src, r in rows:
        xr = num(r.get("×rifle"))
        if xr and xr > MAX_XRIFLE:
            junk.append(f"{src}: {r.get('Unit')} at {xr:.1f}x rifle")
            continue
        rf = rifles.get(src) or {}
        cost, spd = num(r.get("Cost")), num(r.get("Spd"))
        norm_rows.append({
            "source": src, "unit": r.get("Unit", ""), "kind": r.get("kind", ""),
            "x_hp": xr,
            "hp": (xr * RIFLE_HP) if xr else None,
            "x_cost": (cost / rf["cost"]) if (cost and rf.get("cost")) else None,
            "cost": (cost / rf["cost"] * RIFLE_COST) if (cost and rf.get("cost")) else None,
            "x_speed": (spd / rf["speed"]) if (spd and rf.get("speed")) else None,
            "speed": (spd / rf["speed"] * RIFLE_SPEED) if (spd and rf.get("speed")) else None,
            "role": r.get("Role", ""), "category": r.get("**Category**", ""),
        })

    # ---- pool the same unit across sources
    # A reference name matches an actor when it is a PREFIX of the actor's last segment.
    # Exact equality was too strict and it broke the spec's own worked example: the vanilla,
    # CnC Reloaded and Romanov's Vengeance rows are all named "Apocalypse" while the Cameo actor
    # is `ra2_soviets_apocalypsetank`, so §16's three 6.4x votes were never pooled and the target
    # came only from the two rows that happen to be spelled "Apocalypse Tank". Prefix in this
    # direction — reference ⊆ actor — also keeps the pooling honest the other way: "Apocalypse
    # Prototype" and "Virus Boss Brute" are NOT prefixes of "apocalypsetank"/"virus", so a
    # differently-named unit cannot leak into another unit's vote.
    by_ref = collections.defaultdict(lambda: collections.defaultdict(dict))
    units, classes = load_roster()
    actor_key = {a: norm(a.split("_")[-1]) for a in units}
    ref_names = sorted({norm(nr["unit"]) for nr in norm_rows if len(norm(nr["unit"])) >= MIN_KEY})
    for actor, akey in actor_key.items():
        for rn in ref_names:
            if akey.startswith(rn):
                for nr in norm_rows:
                    if norm(nr["unit"]) == rn:
                        by_ref[actor][nr["source"]].setdefault(rn, nr)

    synth = []
    for actor, per_src in sorted(by_ref.items()):
        # one row per source: if a source names the concept twice, take its median vote
        per_source = {}
        for src, named in per_src.items():
            picks = list(named.values())
            per_source[src] = dict(picks[0])
            for stat in ("hp", "cost", "speed"):
                vals = [p[stat] for p in picks if p.get(stat)]
                per_source[src][stat] = statistics.median(vals) if vals else None
        key = actor_key[actor]
        actors = [actor]
        targets, outliers = {}, {}
        for stat in ("hp", "cost", "speed"):
            t, o = compromise(votes_for(per_source, stat))
            targets[stat], outliers[stat] = t, o
        for actor in actors:
            rec = units[actor]
            synth.append({
                "actor": actor, "key": key,
                "unit": next(iter(per_source.values()))["unit"],
                "class": classes.get(actor, ""),
                "sources": sorted(per_source),
                "targets": targets, "outliers": outliers,
                "now": {"hp": val(rec, "hp"), "cost": val(rec, "cost"),
                        "speed": val(rec, "speed")},
            })

    matched = [s for s in synth if s["class"]]
    if args.cls:
        matched = [s for s in matched if s["class"] == args.cls]

    print(f"Document 1 rows      : {len(rows)} across {len(sources)} sources")
    print(f"junk rows dropped    : {len(junk)}" + (f"  ({junk[0]})" if junk else ""))
    print(f"matched to the roster: {len(synth)}  (class-tagged: {len(matched)})")

    if args.dry_run:
        for s in sorted(matched, key=lambda r: r["class"])[:25]:
            t, n = s["targets"], s["now"]
            print(f"  {s['class']:<18} {s['actor']:<34} "
                  f"hp {n['hp'] or 0:>9,.0f} -> {t['hp'] or 0:>9,.0f}   "
                  f"cost {n['cost'] or 0:>6,.0f} -> {t['cost'] or 0:>6,.0f}")
        return 0

    write_doc2(norm_rows, sources)
    write_doc3(synth, matched, sources)
    print(f"wrote {DOC2.relative_to(ROOT)} and {DOC3.relative_to(ROOT)}")
    return 0


def _f(v, spec=",.0f"):
    return format(v, spec) if isinstance(v, float) else "—"


def write_doc2(norm_rows, sources):
    out = ["# Document 2 — Original units, NORMALIZED to Cameo's scale", "",
           "_AUTO-GENERATED by `tools/balance/synthesize_reference.py` from "
           "[`ORIGINAL_UNITS_RAW.md`](ORIGINAL_UNITS_RAW.md) (Document 1). "
           "Spec: [`BALANCE_SYNTHESIS.md`](BALANCE_SYNTHESIS.md) §15.2. Do not hand-edit._", "",
           "Every source unit with each stat put on **Cameo's** scale, so a source unit and a "
           "Cameo unit can be read in the same column. The anchor is the basic rifleman: "
           f"**{RIFLE_HP:,} HP / {RIFLE_COST} credits / speed {RIFLE_SPEED}** — the `scout` class "
           "spec in `class_anchors.json`.", "",
           "Each source is normalized to **its own** rifle first (§15.6.2): the sources do not "
           "share a scale — Romanov's Vengeance runs a 12,500 HP rifle, Mental Omega 205, "
           "vanilla RA2 125 — so an un-normalized comparison between them is meaningless.", "",
           "⚠ **No damage or Versus column, deliberately.** §15.4: raw Versus is not comparable "
           "across sources and must never be numerically averaged; only a warhead's relative "
           "ORDER carries identity. That is `tools/reference/aggregate_archetype.py`'s job.", ""]
    for src in sources:
        rs = [r for r in norm_rows if r["source"] == src]
        rf = "yes" if any(r["x_cost"] for r in rs) else "**no rifle row found — HP only**"
        out += [f"## {src}  ({len(rs)} units · cost/speed normalized: {rf})", "",
                "| unit | kind | ×rifle HP | HP (Cameo) | ×rifle cost | cost (Cameo) | "
                "×rifle spd | spd (Cameo) | role |",
                "|---|---|--:|--:|--:|--:|--:|--:|---|"]
        for r in sorted(rs, key=lambda x: -(x["x_hp"] or 0)):
            out.append(f"| {r['unit']} | {r['kind']} | {_f(r['x_hp'], '.2f')} | "
                       f"{_f(r['hp'])} | {_f(r['x_cost'], '.2f')} | {_f(r['cost'])} | "
                       f"{_f(r['x_speed'], '.2f')} | {_f(r['speed'], '.0f')} | {r['role']} |")
        out.append("")
    DOC2.write_text("\n".join(out) + "\n", encoding="utf-8")


def write_doc3(synth, matched, sources):
    out = ["# Document 3 — Synthesis and delta", "",
           "_AUTO-GENERATED by `tools/balance/synthesize_reference.py`. "
           "Spec: [`BALANCE_SYNTHESIS.md`](BALANCE_SYNTHESIS.md) §15.3, worked end-to-end on the "
           "Apocalypse Tank in §16. Do not hand-edit._", "",
           "**This is step 3 of the PER-UNIT APPLICATION LAW** (`anchor_decisions_log.md`, "
           "maintainer 2026-07-31): the class baseline actor is set by step 2c and the formula "
           "takes its weights from it, but each MEMBER's stats come from *synthesis* — the "
           "cross-game reference, not a copy of the baseline's ratios. The law's stated goal is "
           "**more uniqueness between units of a class**, so these targets are deliberately a "
           "SPREAD, not a convergence.", "",
           "⚠ **PROPOSALS, not applied numbers.** Nothing here is written to a ledger or to yaml. "
           "A synthesized target is an input to the maintainer's judgement (§15.6.4: *\"the "
           "compromise is a judgement, not a mean\"*), and pricing still runs through "
           "`fit_class` → sign-off → `apply_balance --confirm` → boot gate.", "",
           "## How each target is reached", "",
           "1. Each source is normalized to **its own** rifle (§15.6.2).",
           "2. The **vanilla family** — RA2, Yuri's Revenge and Romanov's Vengeance — is pooled "
           "to **one vote** (§15.6.1: a faithful remake is not an independent vote; RV "
           "reproduces vanilla's ratios exactly, so counting it separately double-counts "
           "vanilla). The independent voices are the rebalances: Mental Omega, CnC Reloaded.",
           f"3. A vote more than **{OUTLIER_FACTOR}×** from the median of the votes is called an "
           "outlier, listed in its own column, and **excluded** from the target (§15.6.3).",
           "4. The target is the **median** of the surviving votes — it survives one bad source, "
           "which a mean does not, and it never invents a value no source proposed.", ""]

    n_out = sum(1 for s in matched if any(s["outliers"].values()))
    out += ["## Coverage", "",
            f"* Document 1 sources: **{len(sources)}** — {', '.join(sources)}",
            f"* reference units matched to a Cameo actor: **{len(synth)}**",
            f"* of those, carrying a class tag: **{len(matched)}**",
            f"* carrying at least one flagged outlier vote: **{n_out}**", "",
            "⚠ **The reference corpus is RA2-family only.** All five sources are RA2 or an RA2 "
            "mod, so a Tiberian Sun, Dune 2000, StarCraft or Warcraft unit gets a target only "
            "where the same unit concept also appears in an RA2 mod. Extending Document 1 to the "
            "other source games is what widens this — §15.5 already carries the VERIFIED cost "
            "conversion for StarCraft (`credits = 4×minerals + 8×vespene`, three exact fits) and "
            "the Warcraft one by symmetry, so those two are unblocked whenever their CSVs land.",
            ""]

    for cls in sorted({s["class"] for s in matched}):
        rs = [s for s in matched if s["class"] == cls]
        out += [f"## `{cls}` — {len(rs)} member(s) with reference data", "",
                "| actor | source unit | sources | HP now | HP target | Δ HP | cost now | "
                "cost target | Δ cost | outliers |",
                "|---|---|--:|--:|--:|--:|--:|--:|--:|---|"]
        for s in sorted(rs, key=lambda r: r["actor"]):
            t, n = s["targets"], s["now"]
            dhp = (t["hp"] - n["hp"]) if (t["hp"] and n["hp"]) else None
            dc = (t["cost"] - n["cost"]) if (t["cost"] and n["cost"]) else None
            o = "; ".join(f"{k}: {', '.join(v)}" for k, v in s["outliers"].items() if v) or "—"
            out.append(f"| `{s['actor']}` | {s['unit']} | {len(s['sources'])} | "
                       f"{_f(n['hp'])} | {_f(t['hp'])} | {_f(dhp, '+,.0f')} | "
                       f"{_f(n['cost'])} | {_f(t['cost'])} | {_f(dc, '+,.0f')} | {o} |")
        out.append("")

    ranked = sorted((s for s in matched if s["targets"]["hp"] and s["now"]["hp"]),
                    key=lambda s: -abs(s["targets"]["hp"] / s["now"]["hp"] - 1))
    out += ["## Ranked — how far each Cameo unit sits from its synthesized HP target", "",
            "The §15.3 payoff report. A large gap is not automatically a defect: Cameo "
            "deliberately allows more spread than the tight sources (§12.4), and a unit may have "
            "been moved on purpose. It is a list of places where the reference and the roster "
            "disagree enough to deserve a look.", "",
            "| # | actor | class | HP now | HP target | ratio |", "|--:|---|---|--:|--:|--:|"]
    for i, s in enumerate(ranked[:60], 1):
        r = s["targets"]["hp"] / s["now"]["hp"]
        out.append(f"| {i} | `{s['actor']}` | `{s['class']}` | {_f(s['now']['hp'])} | "
                   f"{_f(s['targets']['hp'])} | {r:.2f}× |")
    out.append("")
    DOC3.write_text("\n".join(out) + "\n", encoding="utf-8")


if __name__ == "__main__":
    raise SystemExit(main())
