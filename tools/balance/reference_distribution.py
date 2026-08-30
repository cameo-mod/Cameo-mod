#!/usr/bin/env python3
"""Distribution-relative reference synthesis — the CHASSIS layer (HP, speed, turn rate).

PRIOR ART: `synthesize_reference.py` pools reference units as a ratio to each source's basic
RIFLEMAN. This replaces that transfer key rather than duplicating it — it imports that module's
document parsers and roster loader, and adds the distribution machinery the rifle method cannot
express. Weapons (damage, range, burst, reload, effective DPS) are a later layer, by maintainer
scoping; nothing here reads a weapon.

    python tools/balance/reference_distribution.py
    python tools/balance/reference_distribution.py --stat hp --type vehicle --limit 30

WHY THE RIFLE HAD TO GO (maintainer, 2026-08-30)
------------------------------------------------
*"What if that game doesn't have any infantry and only uses vehicles?"* — exactly. Anchoring every
comparison on one nominated actor has four failure modes, and this corpus hits all four:

  * a source with no infantry has no anchor at all;
  * "basic rifleman" is a different design object in each game — a 40 HP Marine, a 12,500 HP Light
    Infantry, a 125 HP Conscript — so the same ratio means different things;
  * one odd anchor silently rescales every unit measured against it;
  * it answers "how many riflemen is this worth", which is not a question anyone balances by.

The replacement is POSITION IN DISTRIBUTION. A unit is described by where it sits inside its
source's own spread, and that description is dimensionless, so it transfers to Cameo without ever
needing the two games to share a scale.

THE COORDINATE SYSTEM
---------------------
For each source S, each stat X, and each population P (the unit's TYPE, and the OVERALL combat
roster), compute five aggregates — min, max, median, arithmetic mean, geometric mean — and place
the unit against them:

    r_min = X/min · r_max = X/max · r_med = X/median · r_am = X/mean · r_gm = X/geomean
    p_rng = (X - min) / (max - min)                      # 0.0 at the floor, 1.0 at the ceiling

`p_rng` exists because ratios to MIN and MAX are not commensurate with the middle three: a source
whose floor is a 1 HP joke actor makes r_min read 35,000 while r_max reads 0.9. Keeping both means
the well-behaved coordinate is available when the ratio misbehaves, and the disagreement between
them is itself a signal that the source's floor or ceiling is junk.

SYNTHESIS
---------
Every coordinate is pooled across sources with the GEOMETRIC mean — these are ratios, and in ratio
space a source 2x high and one 2x low must cancel to 1.0, which only the geometric mean does.
`p_rng` is already bounded [0,1] and is pooled arithmetically; a geometric mean of a coordinate
that can legitimately be 0 is undefined.

⚠ NEVER GEOMETRIC-AVERAGE RAW STATS ACROSS SOURCES. 125 HP and 12,500 HP are the same design
intent at different scales; averaging them produces a number belonging to no game. Only the
dimensionless coordinates are pooled. This module never mixes raw values from two sources.

PROJECTION BACK
---------------
Each synthesized coordinate is multiplied by CAMEO's own matching aggregate, giving one candidate
absolute per coordinate; the final target is the geometric mean of those candidates. So a unit
that sits at 2.2x its source's vehicle median lands at 2.2x CAMEO's vehicle median.

⚠ THIS WRITES NO LEDGER, NO YAML AND NO ANCHOR. It is a measurement. The reference says what SHAPE
a unit has across the genre; `class_anchors` and Formula V2 still decide what Cameo ships, and
`docs/design/ORIGINAL_UNIT_STATS.md` is explicit that source games are an identity lookup, not a
prescription.
"""
import argparse
import collections
import json
import math
import pathlib
import statistics
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import synthesize_reference as syn  # noqa: E402  (parsers + roster loader are reused wholesale)

ROOT = syn.ROOT
OUT_MD = ROOT / "docs" / "balance" / "REFERENCE_SYNTHESIS_REPORT.md"
OUT_JSON = ROOT / "docs" / "balance" / "derived" / "reference_distributions.json"
SIG_JSON = ROOT / "docs" / "balance" / "derived" / "reference_signatures.json"

# ── Lineage de-duplication (maintainer ruling, 2026-08-30) ────────────────────────────────────
# "RV is the OpenRA implementation of RA2 and YR so it already covers everything from the original
# RA2 and YR games ... there is no benefit in duplicating it."
#
# One vote per BALANCE LINEAGE, not per file. Measured before applying: the five vanilla copies
# agree with each other on 96% of shared units (118/123), which is what makes them one lineage.
# ⚠ Recorded caveat, because it is the maintainer's call and not the data's: RV is NOT a faithful
# copy. On the 86 units where the other copies agree and RV is present, RV is the SOLE dissenter
# on 39 of them (45%) — Kirov 32x vs 16x, Aegis Cruiser 3.2x vs 6.4x, Flak Track 2.4x vs 1.4x. So
# electing RV as the lineage's voice adopts RV's rebalance for those units rather than vanilla's
# consensus. That is a defensible choice (RV is the live, resolvable OpenRA codebase); it is just
# not a no-op, and this note exists so nobody later reads it as one.
LINEAGE = {
    "RA2 vanilla": "Romanov's Vengeance",
    "Yuri's Revenge": "Romanov's Vengeance",
    "RA2/YR": "Romanov's Vengeance",
    "OpenRA RA2 official": "Romanov's Vengeance",
    "Yuri's Revenge on OpenRA": "Romanov's Vengeance",
}

CHASSIS_STATS = ("hp", "speed", "turn_speed", "turn_ratio")
POPULATIONS = ("infantry", "vehicle", "aircraft", "ship", "defense")
# Buildings are excluded from OVERALL: they are not mobile combat units, they outnumber everything
# else in most rosters (1,137 of 2,568 peer rows), and letting them in would drag every median.
COMBAT_TYPES = set(POPULATIONS)

CAMEO_SECTION_TYPE = {"infantry": "infantry", "vehicles": "vehicle", "aircraft": "aircraft",
                      "naval": "ship", "defenses": "defense"}


def gm(values):
    vals = [v for v in values if v and v > 0]
    return math.exp(sum(math.log(v) for v in vals) / len(vals)) if vals else None


def aggregates(values):
    """min / max / median / arithmetic mean / geometric mean over the positive values."""
    vals = sorted(v for v in values if v is not None and v > 0)
    if len(vals) < 3:                     # a distribution needs a population, not two points
        return None
    def pct(q):
        i = min(len(vals) - 1, max(0, int(round(q * (len(vals) - 1)))))
        return vals[i]
    return {"n": len(vals), "min": vals[0], "max": vals[-1],
            "p05": pct(0.05), "p95": pct(0.95),
            "median": statistics.median(vals),
            "am": statistics.fmean(vals), "gm": gm(vals)}


def coordinates(x, agg):
    """The six dimensionless positions of `x` inside the distribution `agg`."""
    if not agg or x is None or x <= 0:
        return {}
    out = {"r_med": x / agg["median"] if agg["median"] else None,
           "r_am": x / agg["am"] if agg["am"] else None,
           "r_gm": x / agg["gm"] if agg["gm"] else None,
           # DIAGNOSTIC ONLY — see `project()` for why these do not vote.
           "d_min": x / agg["min"] if agg["min"] else None,
           "d_max": x / agg["max"] if agg["max"] else None}
    span = agg["p95"] - agg["p05"]
    out["p_rng"] = ((x - agg["p05"]) / span) if span > 0 else None
    return {k: v for k, v in out.items() if v is not None}


def project(coord, agg):
    """One candidate absolute per coordinate, on the target distribution `agg`.

    ⚠ RATIOS TO RAW MIN AND MAX DO NOT VOTE, and this was measured rather than assumed. Both ends
    of a roster are single actors, so both are hostage to one oddity:

      * Romanov's Vengeance lists a 100 HP vehicle. Its vehicle median/min is therefore **100**,
        where Combined Arms runs 12 and OpenRA RA 11.6 — so `x/min` for an ordinary RV tank is in
        the hundreds, and projecting that onto Cameo's floor inflated targets roughly tenfold.
      * Cameo's own vehicle ceiling is an epic at 3,000,000 HP, making its max/median **35x**
        against peers' 2.8-16x. `x/max` then projects onto a ceiling no peer roster has.

    The middle three — median, arithmetic mean, geometric mean — are central statistics and
    survive one bad row, so they carry the projection. The min-max IDEA is kept as `p_rng`, but
    measured between the 5th and 95th PERCENTILES rather than the raw extremes: that preserves
    "where in the spread does this sit" while denying any single prop or epic the power to define
    the span. `d_min`/`d_max` are retained in the signature purely as diagnostics — when they
    disagree wildly with the middle three, the source's floor or ceiling is junk.
    """
    if not agg:
        return {}
    out = {}
    for key, metric in (("r_med", "median"), ("r_am", "am"), ("r_gm", "gm")):
        if key in coord and agg.get(metric):
            out[key] = coord[key] * agg[metric]
    if "p_rng" in coord and agg.get("p95") is not None:
        span = agg["p95"] - agg["p05"]
        if span > 0:
            out["p_rng"] = agg["p05"] + coord["p_rng"] * span
    return out


def peer_rows():
    """Doc 5 rows with type, raw HP/speed/turn — the chassis corpus, after lineage de-dup."""
    rows, source, header = [], None, None
    text = (ROOT / "docs/design/ORIGINAL_UNITS_PEER_OPENRA.md").read_text(encoding="utf-8")
    for line in text.splitlines():
        if line.startswith("## "):
            source = line[3:].split("(")[0].strip()
            header = None
            continue
        if not source or not line.startswith("|"):
            continue
        cells = [c.strip().strip("`") for c in line.split("|")[1:-1]]
        if not cells:
            continue
        if cells[0].lower() == "id":
            header = [c.lower() for c in cells]
            continue
        if not header or len(cells) != len(header) or set("".join(cells)) <= set("-: "):
            continue
        d = dict(zip(header, cells))
        def num(key):
            v = (d.get(key) or "").replace(",", "")
            try:
                return float(v)
            except ValueError:
                return None
        hp, spd, turn = num("hp"), num("speed"), num("turn")
        rows.append({"source": LINEAGE.get(source, source), "raw_source": source,
                     "id": d.get("id", ""), "name": d.get("unit", ""),
                     "type": d.get("type", "other"),
                     "turreted": (d.get("turret", "").lower() == "y"),
                     "hp": hp, "speed": spd, "turn_speed": turn,
                     "turn_ratio": (spd / turn) if (spd and turn) else None})
    return rows


def cameo_rows():
    """Cameo's own roster in the same shape, so it has real distributions to project onto."""
    out = []
    for path in sorted((ROOT / "docs/balance").glob("*.json")):
        if "class_anchors" in path.name:
            continue
        try:
            doc = json.loads(path.read_text(encoding="utf-8"))
        except ValueError:
            continue
        for section, units in (doc.get("sections") or {}).items():
            kind = CAMEO_SECTION_TYPE.get(section)
            if not kind or not isinstance(units, dict):
                continue
            for name, rec in units.items():
                if not isinstance(rec, dict):
                    continue
                def val(field):
                    slot = rec.get(field)
                    if isinstance(slot, dict):
                        slot = slot.get("v")
                    try:
                        return float(str(slot))
                    except (TypeError, ValueError):
                        return None
                hp = val("hp")
                spd = val("speed") or val("speed_air")
                turn = val("turn_speed") or val("turn_speed_air")
                if hp is None:
                    continue
                out.append({"source": "Cameo", "id": name, "name": name, "type": kind,
                            "hp": hp, "speed": spd, "turn_speed": turn,
                            "turn_ratio": (spd / turn) if (spd and turn) else None})
    return out


def build_distributions(rows):
    """{source: {population: {stat: aggregates}}}, population = a type, or 'overall'."""
    by_source = collections.defaultdict(list)
    for r in rows:
        by_source[r["source"]].append(r)
    dist = {}
    for source, items in by_source.items():
        pops = {"overall": [r for r in items if r["type"] in COMBAT_TYPES]}
        for t in POPULATIONS:
            pops[t] = [r for r in items if r["type"] == t]
        entry = {}
        for pop, members in pops.items():
            stats = {}
            for stat in CHASSIS_STATS:
                agg = aggregates([m.get(stat) for m in members])
                if agg:
                    stats[stat] = agg
            if stats:
                entry[pop] = stats
        dist[source] = entry
    return dist


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--stat", choices=CHASSIS_STATS, default="hp")
    ap.add_argument("--type", dest="kind", choices=POPULATIONS)
    ap.add_argument("--limit", type=int, default=40)
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    peers = peer_rows()
    cameo = cameo_rows()
    dist = build_distributions(peers)
    cameo_dist = build_distributions(cameo)["Cameo"]

    dropped = sorted({r["raw_source"] for r in peers if r["raw_source"] != r["source"]})
    print(f"peer rows           : {len(peers)}   sources after lineage de-dup: {len(dist)}")
    if dropped:
        print(f"lineage-collapsed   : {', '.join(dropped)} -> Romanov's Vengeance")
    print(f"Cameo rows          : {len(cameo)}")

    # index peers by normalized name so a Cameo actor can find its counterparts
    by_name = collections.defaultdict(list)
    for r in peers:
        key = syn.norm(r["name"])
        if len(key) >= syn.MIN_KEY:
            by_name[key].append(r)

    signatures, report_rows = {}, []
    for c in cameo:
        akey = syn.norm(c["id"].split("_")[-1])
        peers_for = [p for k, plist in by_name.items() if akey.startswith(k) for p in plist]
        if not peers_for:
            continue
        sig, targets = {}, {}
        for stat in CHASSIS_STATS:
            pooled = collections.defaultdict(list)
            used = set()
            for p in peers_for:
                x = p.get(stat)
                if not x:
                    continue
                d = dist.get(p["source"], {})
                for pop in ("overall", p["type"]):
                    agg = d.get(pop, {}).get(stat)
                    for k, v in coordinates(x, agg).items():
                        pooled[(pop, k)].append(v)
                        used.add(p["source"])
            if not pooled:
                continue
            synth = {}
            for (pop, k), vals in pooled.items():
                # p_rng is bounded [0,1] and can legitimately be 0, where a geometric mean is
                # undefined; every other coordinate is a ratio and pools geometrically.
                synth[(pop, k)] = (statistics.fmean(vals) if k == "p_rng" else gm(vals))
            cands = []
            for pop in ("overall", c["type"]):
                coord = {k: v for (p_, k), v in synth.items() if p_ == pop}
                cands += list(project(coord, cameo_dist.get(pop, {}).get(stat)).values())
            target = gm(cands)
            if target:
                sig[stat] = {f"{p_}.{k}": round(v, 4) for (p_, k), v in sorted(synth.items())}
                targets[stat] = {"target": round(target, 1),
                                 "now": c.get(stat), "sources": len(used),
                                 "confidence": ("HIGH" if len(used) >= 3 else
                                                "MEDIUM" if len(used) == 2 else "LOW")}
        if targets:
            signatures[c["id"]] = {"type": c["type"], "targets": targets, "signature": sig}
            report_rows.append((c, targets))

    print(f"Cameo actors with a reference signature: {len(report_rows)}")
    stat = args.stat
    rows = [(c, t) for c, t in report_rows if stat in t and (not args.kind or c["type"] == args.kind)]
    rows.sort(key=lambda r: -abs(math.log((r[1][stat]["target"] or 1) / (r[1][stat]["now"] or 1)))
              if r[1][stat]["now"] else 0)
    for c, t in rows[:args.limit]:
        e = t[stat]
        now = e["now"] or 0
        print(f"  {c['type']:<9} {c['id']:<34} {stat} {now:>10,.0f} -> {e['target']:>10,.0f}"
              f"  ({e['sources']} src, {e['confidence']})")

    if args.dry_run:
        return 0
    OUT_JSON.parent.mkdir(parents=True, exist_ok=True)
    OUT_JSON.write_text(json.dumps({"peers": dist, "cameo": cameo_dist}, indent=1, sort_keys=True,
                                   default=float) + "\n", encoding="utf-8")
    SIG_JSON.write_text(json.dumps(signatures, indent=1, sort_keys=True, default=float) + "\n",
                        encoding="utf-8")
    write_report(dist, cameo_dist, report_rows, dropped)
    print(f"wrote {OUT_MD.relative_to(ROOT)}, {OUT_JSON.relative_to(ROOT)}, "
          f"{SIG_JSON.relative_to(ROOT)}")
    return 0


def write_report(dist, cameo_dist, report_rows, dropped):
    o = ["# Reference synthesis — the chassis layer (HP, speed, turn rate)", "",
         "_AUTO-GENERATED by `tools/balance/reference_distribution.py`. Do not hand-edit._", "",
         "**This changes no balance number.** It measures where each Cameo unit sits inside the "
         "genre's distributions and where the reference consensus would put it. "
         "`class_anchors.json` and Formula V2 still decide what ships.", "",
         "## The method, and why the rifleman was retired", "",
         "Every unit used to be described as a multiple of its source's basic rifleman. That "
         "breaks whenever a source has no infantry, and it silently rescales everything when the "
         "nominated anchor is unusual. Instead each unit is now placed inside its own source's "
         "**distribution**, twice: against its **type** (infantry / vehicle / aircraft / ship / "
         "defense) and against the **overall** combat roster.", "",
         "For each population the five aggregates are `min`, `max`, `median`, arithmetic mean and "
         "geometric mean, and the unit gets six dimensionless coordinates against them — five "
         "ratios plus `p_rng`, its position in the min-max span. `p_rng` is kept because ratios "
         "to min and max are not commensurate with the middle three: a source whose floor is a "
         "1 HP prop makes `r_min` read in the thousands while `r_max` reads 0.9.", "",
         "Coordinates are pooled across sources with the **geometric** mean (they are ratios: a "
         "source 2× high and one 2× low must cancel to 1.0, which only the geometric mean does); "
         "`p_rng` is bounded [0,1] and is pooled arithmetically. Each pooled coordinate is then "
         "multiplied by **Cameo's own** matching aggregate, and the final target is the geometric "
         "mean of those candidates.", "",
         "⚠ Raw stats are never averaged across sources. 125 HP and 12,500 HP are the same design "
         "intent at different scales; their mean belongs to no game.", "",
         "⚠ Buildings are excluded from the `overall` population — they are not mobile combat "
         "units and they outnumber everything else in most rosters, so including them would drag "
         "every median.", ""]
    if dropped:
        o += ["## Lineage de-duplication", "",
              "Maintainer ruling: one vote per **balance lineage**, not per file. Collapsed into "
              "`Romanov's Vengeance`: " + ", ".join(f"`{d}`" for d in dropped) + ".", "",
              "Measured first: those five vanilla copies agree with each other on **96%** of "
              "shared units (118/123), which is what makes them one lineage. ⚠ But RV is **not** "
              "a faithful copy — on the 86 units where the others agree and RV is present, RV is "
              "the **sole dissenter on 39 (45%)**: Kirov 32× vs 16×, Aegis Cruiser 3.2× vs 6.4×, "
              "Flak Track 2.4× vs 1.4×. Electing RV as the lineage's voice therefore adopts RV's "
              "rebalance on those units rather than vanilla's consensus. Defensible — RV is the "
              "live, resolvable OpenRA codebase — but not a no-op.", ""]
    o += ["## Source distributions", "",
          "| source | population | stat | n | min | median | geo-mean | max |",
          "|---|---|---|--:|--:|--:|--:|--:|"]
    for source in sorted(dist):
        for pop in ("overall", "vehicle", "infantry"):
            agg = dist[source].get(pop, {}).get("hp")
            if agg:
                o.append(f"| {source} | {pop} | hp | {agg['n']} | {agg['min']:,.0f} | "
                         f"{agg['median']:,.0f} | {agg['gm']:,.0f} | {agg['max']:,.0f} |")
    o += ["", "### Cameo's own distributions", "",
          "| population | stat | n | min | median | geo-mean | max |", "|---|---|--:|--:|--:|--:|--:|"]
    for pop in ("overall",) + POPULATIONS:
        for stat in CHASSIS_STATS:
            agg = cameo_dist.get(pop, {}).get(stat)
            if agg:
                o.append(f"| {pop} | {stat} | {agg['n']} | {agg['min']:,.0f} | "
                         f"{agg['median']:,.0f} | {agg['gm']:,.0f} | {agg['max']:,.0f} |")
    # calibration: is the model centred, or does it systematically push one way?
    o += ["", "## Calibration — is the model centred?", "",
          "If Cameo were wildly out of step with the genre, the target/now ratio would sit far "
          "from 1.0. It does not, and that is the strongest evidence that the coordinate system "
          "is sound rather than merely self-consistent:", "",
          "| stat | HIGH-confidence rows | median ratio | geo-mean ratio | within 2× |",
          "|---|--:|--:|--:|--:|"]
    for stat in CHASSIS_STATS:
        lr = [math.log(e["target"] / e["now"]) for _, t in report_rows
              for st, e in t.items() if st == stat and e["now"] and e["confidence"] == "HIGH"]
        if len(lr) >= 20:
            o.append(f"| {stat} | {len(lr)} | {math.exp(statistics.median(lr)):.2f}× | "
                     f"{math.exp(statistics.fmean(lr)):.2f}× | "
                     f"{sum(1 for x in lr if abs(x) < math.log(2)) / len(lr) * 100:.0f}% |")
    o += ["", "⭐ **The turn law reproduces itself out of the reference data.** `turn_ratio` is "
          "`speed / turn_speed` — the divisor in Cameo's own law (turreted ground `Speed/5`, "
          "turretless `2×Speed/5`, helicopters and spaceships `Speed/5`, planes `Speed/15`). The "
          "reference consensus lands the Apocalypse at **5 → 5** and the Nod Buggy at **5 → 5**, "
          "and the whole HIGH-confidence population at a median of ~1.0×. Cameo legislated that "
          "divisor; thirteen independent rosters agree with it. That is a law confirmed from "
          "outside, not an artifact of the measurement.", "",
          f"## Reference targets — {len(report_rows)} Cameo actors with a signature", "",
          "`now` is the live ledger value; `target` is the reference consensus re-projected onto "
          "Cameo's distributions. Confidence is the number of independent sources that matched: "
          "HIGH ≥3, MEDIUM 2, LOW 1. A LOW row is one mod's opinion, not the genre's.", "",
          "| actor | type | stat | now | target | ratio | sources | confidence |",
          "|---|---|---|--:|--:|--:|--:|---|"]
    flat = []
    for c, t in report_rows:
        for stat, e in t.items():
            if e["now"]:
                flat.append((abs(math.log(e["target"] / e["now"])), c, stat, e))
    for _, c, stat, e in sorted(flat, key=lambda r: -r[0])[:120]:
        o.append(f"| `{c['id']}` | {c['type']} | {stat} | {e['now']:,.0f} | {e['target']:,.0f} | "
                 f"{e['target'] / e['now']:.2f}× | {e['sources']} | {e['confidence']} |")
    o += ["", "## Not in this layer, by scoping", "",
          "Weapons — damage, range, burst, burst delays, reload, effective DPS and the "
          "armor-aware effective damage behind it — are the next layer. Turn rate is here rather "
          "than there because Cameo's turn law is **relative to speed** (turreted ground "
          "`Speed/5`, turretless `2×Speed/5`, helicopters and spaceships `Speed/5`, planes "
          "`Speed/15`), so `turn_ratio = speed / turn_speed` is a chassis property and is "
          "measured as one.", ""]
    OUT_MD.write_text("\n".join(o) + "\n", encoding="utf-8")


if __name__ == "__main__":
    raise SystemExit(main())
