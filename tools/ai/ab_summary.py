#!/usr/bin/env python3
"""Summarise bot A/B matches: wins per BOT TYPE, with the uncertainty that the sample size allows.

Reads the cameo-ai-matches.jsonl files written by AiMatchLogWriter (one record per bot per match;
tools/ai/run_ai_match_batch.py collects them in <support-dir>/Logs/). The acceptance test
(AI_ARCHITECTURE §0a, maintainer 2026-09-28) is a fog-blind candidate bot BEATING the omniscient
`classic` bot, so the question is "how often does bot type X win", not a faction/personality table.

Usage:
    python tools/ai/ab_summary.py <support-dir-or-jsonl> [...]

A match whose two records both say "lost" is a timeout draw (the fixture's locked time limit).
The 95% interval is Wilson's, so 3 wins of 3 reads as "somewhere between 44% and 100%", which is
the honest statement about three games.
"""
import collections
import json
import math
import pathlib
import sys


def wilson(wins: int, n: int, z: float = 1.96) -> tuple[float, float]:
    if n == 0:
        return (0.0, 1.0)
    p = wins / n
    denom = 1 + z * z / n
    centre = (p + z * z / (2 * n)) / denom
    half = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / denom
    return (max(0.0, centre - half), min(1.0, centre + half))


def records(paths: list[str]):
    for arg in paths:
        p = pathlib.Path(arg)
        files = [p] if p.is_file() else list(p.rglob("cameo-ai-matches.jsonl"))
        for f in files:
            for line in f.read_text(encoding="utf-8").splitlines():
                if line.strip():
                    yield json.loads(line)


def main(argv: list[str]) -> int:
    if not argv:
        print(__doc__)
        return 2

    games = collections.defaultdict(list)
    for r in records(argv):
        games[r.get("game_uid")].append(r)

    stats = collections.defaultdict(lambda: {"won": 0, "lost": 0, "draw": 0, "spawn_wins": collections.Counter(), "ticks": []})
    for uid, recs in games.items():
        if len(recs) != 2:
            continue
        outcomes = [r["player"]["outcome"] for r in recs]
        draw = all(o != "won" for o in outcomes)
        for r in recs:
            bot = r["player"]["bot_type"]
            s = stats[bot]
            if draw:
                s["draw"] += 1
            elif r["player"]["outcome"] == "won":
                s["won"] += 1
                # `spawn` is the lobby choice, 0 for every map-side harness player; the home
                # cell (or, in records older than it, the seat name) is what tells sides apart.
                s["spawn_wins"][r["player"].get("home") or r["player"].get("name", "?")] += 1
            else:
                s["lost"] += 1
            s["ticks"].append(r.get("duration_ticks") or 0)

    print(f"{len(games)} match(es)")
    print("| bot type | won | lost | draw | win rate | 95% interval | wins by side | mean length (ticks) |")
    print("|---|--:|--:|--:|--:|---|---|--:|")
    for bot, s in sorted(stats.items()):
        n = s["won"] + s["lost"] + s["draw"]
        lo, hi = wilson(s["won"], n)
        mean = sum(s["ticks"]) // max(1, len(s["ticks"]))
        print(f"| `{bot}` | {s['won']} | {s['lost']} | {s['draw']} | {100 * s['won'] // max(1, n)}% | "
              f"{100 * lo:.0f}–{100 * hi:.0f}% | {dict(s['spawn_wins'])} | {mean} |")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
