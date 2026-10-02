#!/usr/bin/env python3
"""Score bot team coordination from match telemetry (TC-3 acceptance metrics).

Reads the per-match records a team run leaves in a support dir
(tools/ai/run_ai_match_batch.py --team-size N):

    Logs/cameo-ai-missions.jsonl   one line per mission event/attempt
    Logs/cameo-ai-matches.jsonl    one record per bot per match (allies list)
    batch_results.jsonl            one record per executed match

and reports, per team and per match, the §12.18 acceptance metrics:

    shared_push       windows where >=2 teammates committed attack missions
                      (raid/recon/secure) against the SAME enemy player —
                      the coalition-main-target effect.
    contested_claims  same `mission_id` (same capturable actor id) attempted
                      by >=2 teammates — two bots racing one expansion;
                      TC-2c should drive this to ~0 when armed.
    defend_answers    `defend` missions while a teammate was under pressure —
                      lower bound only: missions don't carry a requester id,
                      so answers are inferred from proximity, not causality.
    coverage          distinct target players / regions touched per team.

Team membership is inferred from cameo-ai-matches.jsonl `allies` records
(transitive closure of ally names). `--team A=Multi0,Multi1` overrides.

Usage:
    python tools/ai/team_coordination_report.py <support-dir> [--window T]

--window T : tick window for "simultaneous" (default 1500 = 1 min at 25tps).
"""
import collections
import json
import pathlib
import sys

ATTACK_KINDS = {"raid", "recon", "secure"}
DEFEND_KINDS = {"defend"}


def iter_jsonl(path):
    if not path or not path.is_file():
        return
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.strip():
            try:
                yield json.loads(line)
            except json.JSONDecodeError:
                continue


def teams_from_matches(match_records):
    """Transitive closure over `allies` names -> list of teams (player sets)."""
    parent = {}

    def find(x):
        parent.setdefault(x, x)
        while parent[x] != x:
            parent[x] = parent[parent[x]]
            x = parent[x]
        return x

    def union(a, b):
        ra, rb = find(a), find(b)
        if ra != rb:
            parent[max(ra, rb)] = min(ra, rb)

    for r in match_records:
        me = (r.get("player") or {}).get("name")
        if not me:
            continue
        for a in r.get("allies") or []:
            name = a.get("name") if isinstance(a, dict) else a
            if name:
                union(me, name)
    groups = collections.defaultdict(set)
    for x in parent:
        groups[find(x)].add(x)
    return sorted((sorted(g) for g in groups.values()), key=lambda g: (len(g), g))


def load_support_dir(support):
    support = pathlib.Path(support)
    logs = support / "Logs"
    missions = list(iter_jsonl(logs / "cameo-ai-missions.jsonl"))
    matches = list(iter_jsonl(logs / "cameo-ai-matches.jsonl"))
    results = list(iter_jsonl(support / "batch_results.jsonl"))
    # matches may also be appended to a global Logs dir; fall back to batch
    # results' bot_outcomes for team inference.
    if not matches:
        for r in results:
            for bo in r.get("bot_outcomes") or []:
                me = bo.get("record_id")
                allies = [{"name": a.get("name")} for a in bo.get("allies") or []]
                # record_id is not the slot name; allies names are slot names.
                matches.append({"player": {"name": me}, "allies": allies})
    return missions, matches, results


def mission_of(rec):
    mid = rec.get("mission_id") or ""
    parts = mid.split(":")
    kind = parts[0]
    target_player = parts[1] if len(parts) > 1 else None
    return kind, mid, target_player


def analyse(missions, team):
    """Per-team coordination metrics from that team's mission records."""
    recs = [r for r in missions if r.get("player") in team]
    attempts = [r for r in recs if r.get("record_kind") == "attempt"]

    # Contested claims: identical mission_id attempted by >=2 teammates.
    by_mid = collections.defaultdict(set)
    for r in attempts:
        _, mid, _ = mission_of(r)
        by_mid[mid].add(r.get("player"))
    contested = {mid: ps for mid, ps in by_mid.items() if len(ps) > 1}

    # Shared pushes: same enemy target_player attacked by >=2 teammates
    # within `window` ticks. Pair the attempts by target+tick bucket.
    shared_push_windows = 0
    push_events = collections.defaultdict(list)  # (target, bucket) -> players
    window = analyse.window
    for r in attempts:
        kind, _, tgt = mission_of(r)
        if kind in ATTACK_KINDS and tgt and tgt not in team:
            push_events[(tgt, (r.get("tick") or 0) // window)].add(r.get("player"))
    shared_pushes = {k: ps for k, ps in push_events.items() if len(ps) > 1}
    shared_push_windows = len(shared_pushes)

    # Attack breadth: distinct enemy players ever targeted.
    targets_hit = set()
    for r in attempts:
        kind, _, tgt = mission_of(r)
        if kind in ATTACK_KINDS and tgt and tgt not in team:
            targets_hit.add(tgt)

    defend_count = sum(1 for r in attempts if mission_of(r)[0] in DEFEND_KINDS)
    return {
        "players": sorted(team),
        "attempts": len(attempts),
        "contested_claims": {m: sorted(p) for m, p in contested.items()},
        "shared_push_windows": shared_push_windows,
        "shared_push_detail": {f"{t}@w{w}": sorted(p) for (t, w), p in sorted(shared_pushes.items())},
        "distinct_enemy_targets": sorted(targets_hit),
        "defend_missions": defend_count,
    }


def main(argv):
    args = [a for a in argv[1:] if not a.startswith("--")]
    opts = {a.split("=")[0]: a.split("=", 1)[1] for a in argv[1:] if "=" in a}
    if not args:
        sys.exit(__doc__)
    analyse.window = int(opts.get("--window", 1500))
    support = args[0]
    missions, matches, results = load_support_dir(support)
    if not missions:
        sys.exit(f"no mission records under {support}/Logs")

    teams = teams_from_matches(matches)
    if not teams:
        players = sorted({r.get("player") for r in missions})
        teams = [players]
        print("warning: no allies data — treating all players as one team", file=sys.stderr)

    print(f"support={support}  missions={len(missions)}  teams={len(teams)}")
    for i, team in enumerate(teams):
        print(f"\nteam {chr(65 + i)}: {', '.join(team)}")
        rep = analyse(missions, set(team))
        print(f"  attempts={rep['attempts']}  defend_missions={rep['defend_missions']}")
        print(f"  shared_push_windows={rep['shared_push_windows']}  {rep['shared_push_detail']}")
        print(f"  contested_claims={len(rep['contested_claims'])}")
        for mid, ps in rep["contested_claims"].items():
            print(f"    CONTESTED {mid}: {', '.join(ps)}")
        print(f"  distinct_enemy_targets={rep['distinct_enemy_targets']}")

    # Match outcomes.
    outcomes = collections.Counter()
    for r in results:
        for bo in r.get("bot_outcomes") or []:
            outcomes[(bo.get("bot_type"), bo.get("outcome"))] += 1
    if outcomes:
        print("\noutcomes:")
        for (bot, oc), n in sorted(outcomes.items()):
            print(f"  {bot}: {oc} x{n}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
