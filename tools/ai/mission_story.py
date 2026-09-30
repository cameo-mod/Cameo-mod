#!/usr/bin/env python3
"""Tell each mission's story from the mission-card archive (MC2, AI_MISSION_CARDS §3).

Reads cameo-ai-missions.jsonl written by AiMissionLogWriter (one line per mission-card
transition, schema `mission-card/1`; tools/ai/run_ai_match_batch.py collects it in
<support-dir>/Logs/). For every match it replays each mission as a short story —
proposal -> attempts -> outcome, ticks as game time, the executor named on every line —
then the per-type success rate, and finally the attempts that never reached a terminal
state: those are the ownership bugs (an executor went quiet and the last line says which).

Usage:
    python tools/ai/mission_story.py <support-dir-or-jsonl> [...] [--match <game_uid>] [--mission <id>]

The archive is record-only (DESIGN §21.1): the game never reads it back, so this tool is the
whole read path. Lines carry `terminal` from the writer; the name set below is only the
fallback for records written before that field existed.
"""
import collections
import json
import pathlib
import sys

TERMINAL_STATES = {"DENIED", "SUCCESS", "FAILED", "RELEASED", "ABANDONED"}


def records(paths: list[str]):
    for arg in paths:
        p = pathlib.Path(arg)
        files = [p] if p.is_file() else sorted(p.rglob("cameo-ai-missions.jsonl"))
        for f in files:
            for line in f.read_text(encoding="utf-8").splitlines():
                if line.strip():
                    yield json.loads(line)


def is_terminal(rec: dict) -> bool:
    if "terminal" in rec:
        return bool(rec["terminal"])
    return str(rec.get("state", "")).upper() in TERMINAL_STATES


def story(paths: list[str], match=None, mission=None):
    """Group transition lines into games -> missions -> attempts, ordered by tick."""
    games = collections.defaultdict(list)
    for r in records(paths):
        if match is not None and r.get("game_uid") != match:
            continue
        if mission is not None and r.get("mission_id") != mission:
            continue
        games[r.get("game_uid")].append(r)
    out = []
    for uid, recs in games.items():
        missions = collections.defaultdict(list)
        for r in recs:
            missions[r.get("mission_id")].append(r)
        ordered = []
        for mid, mrecs in missions.items():
            attempts = collections.defaultdict(list)
            for r in mrecs:
                attempts[r.get("attempt")].append(r)
            ordered.append((mid, {a: sorted(rs, key=lambda r: r.get("tick", 0))
                                  for a, rs in sorted(attempts.items(), key=lambda kv: kv[0] or 0)}))
        out.append((uid, recs, ordered))
    return out


def state_line(r: dict) -> str:
    s = f"{r.get('state', '?')}@{r.get('tick', '?')}"
    if r.get("reason"):
        s += f" {r['reason']}"
    return s


def main(argv: list[str]) -> int:
    paths, match, mission = [], None, None
    it = iter(argv)
    for arg in it:
        if arg == "--match":
            match = next(it, None)
        elif arg == "--mission":
            mission = next(it, None)
        else:
            paths.append(arg)
    if not paths:
        print(__doc__)
        return 2

    games = story(paths, match, mission)
    total_attempts = 0
    dangling = []
    type_stats = collections.defaultdict(lambda: collections.Counter())
    print(f"{len(games)} match(es), {sum(len(m) for _, _, m in games)} mission(s)")
    for uid, recs, missions in games:
        meta = recs[0] if recs else {}
        print(f"\n== {meta.get('map_title', '?')} — {meta.get('bot', '?')} ({meta.get('player', '?')}, {meta.get('faction', '?')}) — {str(uid)[:8]}")
        for mid, attempts in missions:
            first = next(iter(attempts.values()))[0]
            tag = mid or "?"
            extra = []
            if first.get("type"):
                extra.append(first["type"])
            if first.get("target_cell"):
                extra.append(f"cell {first['target_cell']}")
            if first.get("region") is not None:
                extra.append(f"region {first['region']}")
            print(f"  {tag}" + (f" ({', '.join(extra)})" if extra else ""))
            for a, trans in attempts.items():
                total_attempts += 1
                last = trans[-1]
                term = is_terminal(last)
                span = last.get("tick", 0) - trans[0].get("tick", 0)
                units = f", {first.get('units')}u" if first.get("units") else ""
                chain = "  ->  ".join(state_line(t) for t in trans)
                print(f"    A{a} {trans[0].get('by', '?')}: {chain}   [{span}t{units}]{'  *open*' if not term else ''}")
                t = first.get("type") or (mid.split(":", 1)[0] if mid else "?")
                if term:
                    type_stats[t][str(last.get("state")).lower()] += 1
                else:
                    type_stats[t]["open"] += 1
                    dangling.append((mid, a, last))

    print("\n== by type ==")
    for t, c in sorted(type_stats.items()):
        resolved = c.get("success", 0) + c.get("failed", 0) + c.get("denied", 0) + c.get("released", 0) + c.get("abandoned", 0)
        rate = f"{100 * c.get('success', 0) / resolved:.0f}%" if resolved else "n/a"
        print(f"  {t:<10} {resolved} resolved — success {c.get('success', 0)} ({rate}), "
              f"failed {c.get('failed', 0)}, denied {c.get('denied', 0)}, released {c.get('released', 0)}, "
              f"abandoned {c.get('abandoned', 0)}" + (f"; {c.get('open', 0)} OPEN" if c.get('open') else ""))

    if dangling:
        print("\n== attempts with no terminal line (ownership bugs — the last line names the layer that went quiet) ==")
        for mid, a, last in dangling:
            print(f"  {mid} A{a}: last {state_line(last)} by {last.get('by', '?')}")
    else:
        print("\nno dangling attempts — every attempt reached a terminal state")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
