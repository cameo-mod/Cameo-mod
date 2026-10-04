#!/usr/bin/env python3
"""refinery_law_check.py - PASS/FAIL verdict per match against the refinery law (DESIGN 19.1b, REF-1).

Reads placement records (`kind:"placement"`, `category:"refinery"` in cameo-ai-placements.jsonl) and the
top-level `expansion` object of situation snapshots (cameo-ai-situations.jsonl) of one or more match dirs
(a batch support dir, its `Logs/`, or a jsonl file) via ai_log_common.

The law (maintainer ruling 2026-10-04, "REF-1"): at most one refinery per SPREADER anchor (a spreaderless
field gets one at its centre); spreaders whose resources connect form one field; tier 1 = the first
refinery of every unserved field in reach, home first then each newly reached field immediately; tier 2
(extra spreaders on an already-served field) only after tier 1 is empty; placement gap 0 to the resources
(1 only when blocked); genericbot takes NO 'base'-path refineries while the law is active.

Per genericbot player x match:
  refineries_per_anchor_max   most refineries on one spreader anchor (anchor_cell; field_id is the tier-2
                              unit and may lawfully hold several refineries) -> FAIL when > 1; the
                              snapshot field of the same name is folded in
  base_reason_count           refinery placements with reason "base"           -> FAIL when > 0
  resource_gap histogram      placements' resource_gap cells                   -> FAIL when any > 1
  tier_order_violations       tier>=2 placements while fields_in_reach_unserved > 0 at that tick -> FAIL when > 0
  claim_latency_p50/p90/max   ticks from a field first counted in fields_in_reach_unserved to its
                              refinery placement (lower bound when several fields wait; --warn-latency
                              flags a WARN, never a FAIL)
  peak/final coverage_milli   the expansion snapshot's coverage share

Old logs lack field_id/tier/resource_gap and the *_unserved snapshot counters: those metrics print "n/a"
and a match that cannot be fully checked never earns PASS. classic rows are informational and never
fail. Exit 1 when any genericbot match FAILs.

Usage: python tools/ai/refinery_law_check.py <match dirs...> [--json] [--warn-latency TICKS]
"""
from __future__ import annotations

import argparse
import collections
import json
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import ai_log_common as c  # noqa: E402

NON_GENERIC = {"classic", "campaign", "fransbot"}
DEFAULT_WARN_LATENCY = 2500  # ~100 game seconds a field may wait before it counts as a smell


def is_genericbot(bot_type: str) -> bool:
    return bot_type not in NON_GENERIC


def tier_of(p: dict) -> int | None:
    """Placement tier as an int; accepts 2, "2", "tier2". None when absent/unparsable."""
    v = p.get("tier")
    if v is None:
        return None
    try:
        return int(v)
    except (TypeError, ValueError):
        digits = "".join(ch for ch in str(v) if ch.isdigit())
        return int(digits) if digits else None


def anchor_key(p: dict) -> str:
    """The law's unit is the SPREADER anchor, never the field: tier-2 refineries are extra spreaders on an
    already-served field, so a field_id may legitimately appear on several refineries."""
    v = p.get("anchor_cell")
    return str(v) if v not in (None, "") else str(p.get("field_id") or "?")


def check_match(snaps: list[dict], places: list[dict], warn_latency: int = DEFAULT_WARN_LATENCY) -> dict:
    snaps = sorted((s for s in snaps if s.get("expansion")), key=lambda s: s["tick"])
    refs = sorted((p for p in places if p.get("category") == "refinery"), key=lambda p: p["tick"])
    ex = [s["expansion"] for s in snaps]

    # refineries per spreader anchor: computed from placements AND the snapshot's own maximum
    per_field = collections.Counter(anchor_key(p) for p in refs)
    computed_max = max(per_field.values(), default=0)
    snapshot_max = max((e.get("refineries_per_anchor_max", 0) for e in ex), default=0)
    ref_max = max(computed_max, snapshot_max) if (refs or any("refineries_per_anchor_max" in e for e in ex)) else None

    base_count = sum(1 for p in refs if p.get("reason") == "base")

    gaps = [p["resource_gap"] for p in refs if isinstance(p.get("resource_gap"), (int, float))]
    gap_hist = dict(sorted(collections.Counter(int(g) for g in gaps).items()))
    gap_known = len(gaps) == len(refs) and len(refs) > 0

    # tier order: the latest snapshot at-or-before each placement decides whether fields were waiting
    have_unserved = any("fields_in_reach_unserved" in e for e in ex)
    tier_violations = None
    if have_unserved:
        tier_violations = 0
        for p in refs:
            t = tier_of(p)
            if t is None or t < 2:
                continue
            before = [s for s in snaps if s["tick"] <= p["tick"]]
            unserved = before[-1]["expansion"].get("fields_in_reach_unserved", 0) if before else 0
            if unserved > 0:
                tier_violations += 1

    # claim latency: placed_tick minus the first snapshot that counted unserved fields (lower bound;
    # per-field first-seen is not logged). Tier-1 placements only once REF-1 logs tiers — tier-2
    # refineries are lawfully delayed and would skew the figure.
    latencies = None
    if have_unserved:
        first_unserved = next((s["tick"] for s in snaps if s["expansion"].get("fields_in_reach_unserved", 0) > 0), None)
        tier1 = [p for p in refs if tier_of(p) in (None, 1)] if any(tier_of(p) is not None for p in refs) else refs
        latencies = sorted(p["tick"] - first_unserved for p in tier1
                           if first_unserved is not None and p["tick"] >= first_unserved)

    def pct(q):
        if not latencies:
            return None
        i = min(len(latencies) - 1, int(round(q * (len(latencies) - 1))))
        return latencies[i]

    fails = []
    if ref_max is not None and ref_max > 1:
        fails.append(f"refineries_per_anchor_max={ref_max}")
    if base_count:
        fails.append(f"base_reason_count={base_count}")
    if gaps and max(gaps) > 1:
        fails.append(f"resource_gap_max={int(max(gaps))}")
    if tier_violations:
        fails.append(f"tier_order_violations={tier_violations}")

    known = bool(refs) and ref_max is not None
    p90 = pct(0.9)
    verdict = "FAIL" if fails else ("WARN" if p90 is not None and p90 > warn_latency
                                    else "PASS" if known and gap_known and tier_violations is not None else "n/a")
    return {
        "refineries": len(refs),
        "refineries_per_anchor_max": ref_max,
        "base_reason_count": base_count,
        "resource_gap_hist": gap_hist if gap_known else "n/a",
        "resource_gap_max": int(max(gaps)) if gaps else None,
        "tier_order_violations": tier_violations,
        "claim_latency_p50": pct(0.5), "claim_latency_p90": p90, "claim_latency_max": latencies[-1] if latencies else None,
        "claim_latency_n": len(latencies) if latencies else 0,
        "peak_coverage_milli": max((e.get("coverage_milli", 0) for e in ex), default=None),
        "final_coverage_milli": ex[-1].get("coverage_milli") if ex else None,
        "fails": fails,
        "verdict": verdict,
    }


def build(data: dict[str, list[dict]], warn_latency: int = DEFAULT_WARN_LATENCY) -> dict:
    matches = {(m.get("game_uid"), m.get("player", {}).get("name")): m for m in data["matches"]}
    by_snap = collections.defaultdict(list)
    for s in data["situations"]:
        by_snap[(s.get("game_uid"), s.get("player"))].append(s)
    by_place = collections.defaultdict(list)
    for p in data["placements"]:
        by_place[(p.get("game_uid"), p.get("player"))].append(p)

    rows = []
    for key in sorted(set(by_snap) | set(by_place), key=lambda k: (str(k[0]), str(k[1]))):
        snaps, places, match = by_snap.get(key, []), by_place.get(key, []), matches.get(key)
        ref = (snaps or places or [{}])[0]
        bot = ref.get("bot_type") or (match or {}).get("player", {}).get("bot_type", "")
        row = {"game_uid": key[0], "player": key[1], "bot_type": bot,
               "map_uid": ref.get("map_uid") or (match or {}).get("map_uid", ""),
               "map_title": (match or {}).get("map_title") or ref.get("map_title") or "",
               "faction": ref.get("faction") or (match or {}).get("player", {}).get("faction", "")}
        row.update(check_match(snaps, places, warn_latency))
        rows.append(row)

    by_map = collections.defaultdict(list)
    for r in rows:
        if is_genericbot(r["bot_type"]):
            by_map[r["map_title"] or r["map_uid"]].append(r)
    maps = []
    for map_name, members in sorted(by_map.items()):
        verdicts = collections.Counter(m["verdict"] for m in members)
        lat = sorted(v for m in members for v in [m["claim_latency_p90"]] if v is not None)
        maps.append({
            "map": map_name, "map_uids": sorted({m["map_uid"] for m in members}), "matches": len(members),
            "fail": verdicts.get("FAIL", 0), "warn": verdicts.get("WARN", 0),
            "pass": verdicts.get("PASS", 0), "n/a": verdicts.get("n/a", 0),
            "worst_refineries_per_anchor_max": max((m["refineries_per_anchor_max"] or 0 for m in members), default=0),
            "base_reason_count": sum(m["base_reason_count"] for m in members),
            "resource_gap_max": max((m["resource_gap_max"] or 0 for m in members), default=0),
            "tier_order_violations": sum(m["tier_order_violations"] or 0 for m in members),
            "claim_latency_p90": lat[-1] if lat else None,
        })
    return {"matches": rows, "maps": maps}


def render(result: dict, warn_latency: int) -> str:
    gen = [r for r in result["matches"] if is_genericbot(r["bot_type"])]
    classic = [r for r in result["matches"] if not is_genericbot(r["bot_type"])]
    head = ["verdict", "map", "game/player", "refs", "per_anchor_max", "base", "gap_hist", "tier_viol",
            "lat_p50", "lat_p90", "lat_max", "peak_cov", "final_cov", "fails"]
    rows = [head]
    for r in gen:
        rows.append([r["verdict"], (r["map_title"] or r["map_uid"] or "?")[:14], f'{r["game_uid"][:8]}/{r["player"]}',
                     r["refineries"], r["refineries_per_anchor_max"] if r["refineries_per_anchor_max"] is not None else "n/a",
                     r["base_reason_count"], r["resource_gap_hist"],
                     r["tier_order_violations"] if r["tier_order_violations"] is not None else "n/a",
                     c.fmt(r["claim_latency_p50"]), c.fmt(r["claim_latency_p90"]), c.fmt(r["claim_latency_max"]),
                     c.fmt(r["peak_coverage_milli"]), c.fmt(r["final_coverage_milli"]), ";".join(r["fails"]) or "-"])
    out = [f"refinery law check: {len(gen)} genericbot player-match(es), warn-latency={warn_latency}t",
           "FAIL = per_anchor_max>1 | base>0 | gap>1 | tier-2 while fields unserved; n/a = field absent from old logs",
           "", "genericbot", c.table(rows)]
    if classic:
        crows = [head]
        for r in classic:
            crows.append(["info", (r["map_title"] or r["map_uid"] or "?")[:14], f'{r["game_uid"][:8]}/{r["player"]}',
                          r["refineries"], r["refineries_per_anchor_max"] or "-", r["base_reason_count"],
                          r["resource_gap_hist"], r["tier_order_violations"] if r["tier_order_violations"] is not None else "n/a",
                          c.fmt(r["claim_latency_p50"]), c.fmt(r["claim_latency_p90"]), c.fmt(r["claim_latency_max"]),
                          c.fmt(r["peak_coverage_milli"]), c.fmt(r["final_coverage_milli"]), "-"])
        out += ["", "classic / non-generic (information only, never fails)", c.table(crows)]
    mrows = [["map", "matches", "FAIL", "WARN", "PASS", "n/a", "worst_per_anchor", "base", "gap_max", "tier_viol", "lat_p90"]]
    for m in result["maps"]:
        mrows.append([(m["map"] or "?")[:14], m["matches"], m["fail"], m["warn"], m["pass"], m["n/a"],
                      m["worst_refineries_per_anchor_max"], m["base_reason_count"], m["resource_gap_max"],
                      m["tier_order_violations"], c.fmt(m["claim_latency_p90"])])
    out += ["", "per map (genericbot)", c.table(mrows)]
    return "\n".join(out)


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("dirs", nargs="+", type=pathlib.Path)
    ap.add_argument("--json", action="store_true", help="print the result as JSON")
    ap.add_argument("--warn-latency", type=int, default=DEFAULT_WARN_LATENCY,
                    help=f"mark latency p90 above this as WARN in the output (default {DEFAULT_WARN_LATENCY} ticks)")
    args = ap.parse_args(argv)
    result = build(c.load(args.dirs), args.warn_latency)
    if not result["matches"]:
        print("no situation or placement records found", file=sys.stderr)
        return 1
    print(json.dumps(result, indent=2) if args.json else render(result, args.warn_latency))
    return 1 if any(r["verdict"] == "FAIL" and is_genericbot(r["bot_type"]) for r in result["matches"]) else 0


if __name__ == "__main__":
    sys.exit(main())
