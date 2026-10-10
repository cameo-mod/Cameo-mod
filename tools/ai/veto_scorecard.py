#!/usr/bin/env python3
"""veto_scorecard.py — how the tier-2 veto disposes: DENIED combat_veto cards scored against
the fights the bot actually took (AI_ARCHITECTURE 12.31-12.32, DESIGN 19.13 survivorship note).

The veto blocks commits; a blocked fight produces no engagement record, so every downstream
consumer (bandit posteriors, the fitter) sees a survivorship-filtered stream. This scorecard
is the counterfactual instrument: what the veto predicted for the fights it blocked vs what
the fights it allowed actually scored. `detail` carries the predicted numbers
("trade=-452;ratio=0.71;ownv=3200;foev=5100;defences=2"); cards written before the Detail
field landed still count, just without a predicted trade.

Reads `cameo-ai-missions.jsonl` + `cameo-ai-engagements.jsonl` of one or more match dirs
(a batch support dir, its `Logs/`, or a jsonl file). `posture` rows and skirmishes stay out of taken_trade.

Per bot x personality:
  veto_atk / veto_ret   DENIED cards by reason (below_threshold / cant_outrun)
  vetoed_trade          mean predicted trade of blocked attacks (counterfactual, milli)
  vetoed_value          mean own value at stake on vetoed commits
  taken_n / taken_trade non-skirmish engagements the bot fought / their mean actual trade
  gap                   taken_trade - vetoed_trade: positive = allowed fights beat what was
                        blocked; <= 0 flags a misjudging veto (or an uncalibrated predictor)

Usage: python tools/ai/veto_scorecard.py <match dirs...> [--json]
"""
from __future__ import annotations

import argparse
import collections
import json
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import ai_log_common as c  # noqa: E402

MISSION_LOG = "cameo-ai-missions.jsonl"


def parse_detail(detail: str) -> dict:
    """'trade=-452;ratio=0.71;ownv=3200;foev=5100;defences=2' -> {'trade': -452, ...}."""
    out = {}
    for part in (detail or "").split(";"):
        k, _, v = part.partition("=")
        if not k:
            continue
        try:
            out[k.strip()] = int(v)
        except ValueError:
            try:
                out[k.strip()] = float(v)
            except ValueError:
                out[k.strip()] = v
    return out


def veto_records(mission_records: list[dict]) -> list[dict]:
    return [r for r in mission_records
            if r.get("type") == "combat_veto" and r.get("event") == "DENIED"]


def summarise(mission_records: list[dict], engagement_records: list[dict]) -> dict:
    """Per (bot, personality): veto counts, counterfactual trade, taken trade, gap."""
    # Only `engagement` records count as fights taken: the same jsonl carries `posture`
    # snapshots (every 250 ticks, no score/personality) — unfiltered they inflate taken_n
    # ~2.2x and a posture row seen first pins personality_of[...] to "" via setdefault.
    fights = [r for r in engagement_records if r.get("record") == "engagement"]

    # (game_uid, player) -> personality, from the engagement stream that carries it.
    personality_of = {}
    for r in fights:
        personality_of.setdefault((r.get("game_uid"), r.get("player")), r.get("personality") or "")

    groups = collections.defaultdict(list)
    for r in veto_records(mission_records):
        key = (r.get("bot") or "", personality_of.get((r.get("game_uid"), r.get("player")), ""))
        groups[key].append(r)

    taken = collections.defaultdict(list)
    for r in fights:
        if r.get("skirmish"):
            continue
        key = (r.get("bot_type") or "", r.get("personality") or "")
        taken[key].append(r.get("score", {}).get("trade_milli"))

    out = {}
    for (bot, personality) in sorted(set(groups) | set(taken)):
        vetoes = groups.get((bot, personality), [])
        attacks = [r for r in vetoes if r.get("reason") == "below_threshold"]
        retreats = [r for r in vetoes if r.get("reason") == "cant_outrun"]
        trades = [parse_detail(r.get("detail", "")).get("trade") for r in attacks]
        trades = [t for t in trades if isinstance(t, int)]
        taken_trades = taken.get((bot, personality), [])
        row = {
            "veto_atk": len(attacks),
            "veto_ret": len(retreats),
            "vetoed_trade": c.mean(trades),
            "vetoed_value": c.mean([r.get("value") for r in vetoes]),
            "taken_n": len(taken_trades),
            "taken_trade": c.mean(taken_trades),
        }
        row["gap"] = (row["taken_trade"] - row["vetoed_trade"]) if row["taken_trade"] is not None and row["vetoed_trade"] is not None else None
        out[f"{bot}|{personality}"] = row

    return dict(sorted(out.items()))


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("dirs", nargs="+", type=pathlib.Path)
    ap.add_argument("--json", action="store_true")
    args = ap.parse_args()

    missions, engagements = [], []
    for d in args.dirs:
        logs = c.logs_dir(d)
        missions += c.read_jsonl(logs / MISSION_LOG)
        engagements += c.read_jsonl(logs / c.ENGAGEMENT_LOG)

    summary = summarise(missions, engagements)
    if args.json:
        print(json.dumps(summary, indent=1, sort_keys=True))
        return

    rows = [[k, str(v["veto_atk"]), str(v["veto_ret"]), c.fmt(v["vetoed_trade"]), c.fmt(v["vetoed_value"]),
             str(v["taken_n"]), c.fmt(v["taken_trade"]), c.fmt(v["gap"])]
            for k, v in sorted(summary.items())]
    print(c.table([["bot|personality", "veto_atk", "veto_ret", "vetoed_trade", "vetoed_value",
                    "taken_n", "taken_trade", "gap"]] + rows))


if __name__ == "__main__":
    main()
