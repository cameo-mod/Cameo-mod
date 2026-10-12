#!/usr/bin/env python3
"""playtest_stats_from_logs.py — per-seat playtest summary from the PLAYTEST-TELEMETRY-V2
channel (maintainer request 2026-10-12).

Reads `cameo-ai-players-<sha256(game_uid)>.jsonl` files — the every-seat channel
(humans + bots, anonymous seat_N) written by AiPlayerTelemetry — and answers what
the match needs without replay playback:

  * per-seat last sample: harvesters, refineries, conyards, expansions, mcv deploys,
    production buildings, tech tier, buildings/units, idle production ticks
  * army value at the last sample by category (infantry/vehicle/air/naval/defence)
    plus the top unit types
  * event counts per seat: buildings placed/lost, conyard deploys/packs, attack
    waves started, first enemy-territory contact tick, superweapons built/fired

When `cameo-ai-matches.jsonl` sits in the same Logs dir, the seat row is joined
with the match outcome. Prints one table per game (or one row per seat per game
with --csv). Read-only: no games launched, no files written.

Usage:
  python tools/ai/playtest_stats_from_logs.py <support-dir | Logs-dir | jsonl> [...]
  python tools/ai/playtest_stats_from_logs.py <dir> --game-uid <uid> --csv
"""
from __future__ import annotations

import argparse
import collections
import csv
import hashlib
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))

import replay_health as health  # strict_json/read_jsonl/EvidenceError + bounds

MATCH_LOG = "cameo-ai-matches.jsonl"
PLAYERS_GLOB = "cameo-ai-players-*.jsonl"
EVENT_KINDS = sorted(health.PLAYER_EVENT_KINDS)


def player_files(paths):
    out = []
    for p in paths:
        if p.is_file() and p.name.startswith("cameo-ai-players-"):
            out.append(p)
            continue
        logs = p / "Logs" if (p / "Logs").is_dir() else p
        found = sorted(logs.glob(PLAYERS_GLOB))
        if found:
            out.extend(found)
        else:
            sys.exit(f"no {PLAYERS_GLOB} under {p}")
    if len(out) > health.MAX_PLAYER_FILES:
        raise health.EvidenceError("player-telemetry file bound exceeded")
    return out


def read_channel(path):
    """(samples-by-seat, events-by-seat) from one bounded, validated channel file."""
    rows, _ = health.read_jsonl(path)
    samples, events = {}, collections.defaultdict(collections.Counter)
    for row in rows:
        schema = row.get("schema")
        if schema == "cameo-player-sample":
            health.reject_unknown_fields(row, health.PLAYER_SAMPLE_V1_FIELDS, "player-sample v1")
            if row.get("version") != 1:
                raise health.EvidenceError("unsupported player-sample version")
            health.integer(row.get("tick"))
            seat = row.get("seat")
            if not isinstance(seat, str) or not seat.startswith("seat_"):
                raise health.EvidenceError("player-sample seat must be an anonymous seat_N")
            previous = samples.get(seat)
            if previous is not None and row["tick"] <= previous["tick"]:
                raise health.EvidenceError("non-monotonic sample ticks for seat")
            samples[seat] = row
        elif schema == "cameo-player-event":
            health.reject_unknown_fields(row, health.PLAYER_EVENT_V1_FIELDS, "player-event v1")
            if row.get("version") != 1:
                raise health.EvidenceError("unsupported player-event version")
            health.integer(row.get("tick"))
            if "seq" in row:
                health.integer(row["seq"])
            kind = row.get("kind")
            if kind not in health.PLAYER_EVENT_KINDS:
                raise health.EvidenceError(f"unsupported player-event kind: {kind}")
            events[row.get("seat")][kind] += 1
            if kind == "first_unit_enemy_territory":
                events[row["seat"]]["first_contact_tick"] = row["tick"]
        else:
            raise health.EvidenceError(f"unsupported player-telemetry schema: {schema}")
    return samples, events


def match_records(support_dirs):
    """seat -> match outcome rows, keyed by game_uid, from cameo-ai-matches.jsonl."""
    out = collections.defaultdict(dict)
    for p in support_dirs:
        logs = p / "Logs" if (p / "Logs").is_dir() else p
        path = logs / MATCH_LOG
        if not path.exists():
            continue
        rows, _ = health.read_jsonl(path)
        for r in rows:
            uid, player = r.get("game_uid"), r.get("player")
            if isinstance(uid, str) and isinstance(player, dict) and player.get("seat"):
                out[uid][player["seat"]] = r
    return out


def seat_row(uid, seat, sample, events, match):
    army = (sample or {}).get("army") or {}
    types = (sample or {}).get("unit_types") or {}
    top = ", ".join(f"{k}x{v}" for k, v in
                   sorted(types.items(), key=lambda kv: (-kv[1], kv[0]))[:4])
    return {
        "game_uid": uid[:12], "seat": seat,
        "bot": (sample or {}).get("bot_type", ""),
        "faction": (sample or {}).get("faction", ""),
        "team": (sample or {}).get("team"),
        "outcome": ((match or {}).get("player") or {}).get("outcome", "?"),
        "tick": (sample or {}).get("tick"),
        "harv": (sample or {}).get("harvesters"),
        "ref": (sample or {}).get("refineries"),
        "cony": (sample or {}).get("conyards"),
        "exp": (sample or {}).get("expansions"),
        "deploys": (sample or {}).get("mcv_deploys"),
        "prod": (sample or {}).get("production_buildings"),
        "tier": (sample or {}).get("tech_tier"),
        "idle_ticks": (sample or {}).get("idle_production_ticks"),
        "army_total": sum(army.values()) if isinstance(army, dict) else None,
        "top_units": top,
        "placed": events.get("building_placed", 0),
        "lost": events.get("structure_lost", 0),
        "waves": events.get("attack_wave_start", 0),
        "first_contact": events.get("first_contact_tick"),
        "sw_built": events.get("superweapon_built", 0),
        "sw_fired": events.get("superweapon_fired", 0),
    }


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("paths", nargs="+", type=pathlib.Path)
    ap.add_argument("--game-uid", help="only this game")
    ap.add_argument("--csv", action="store_true", help="one CSV row per seat")
    args = ap.parse_args(argv)

    files = player_files(args.paths)
    matches = match_records(args.paths)
    rows_out = []
    for path in files:
        samples, events = read_channel(path)
        # The file name is sha256(game_uid); recover game_uid from any row.
        game_uid = None
        if samples:
            game_uid = next(iter(samples.values())).get("game_uid")
        if game_uid is None:
            continue
        if args.game_uid and game_uid != args.game_uid:
            continue

        def seat_index(name):
            try:
                return int(name.partition("_")[2]) if name.startswith("seat_") else 999
            except ValueError:
                return 999

        seats = sorted(samples.keys() | events.keys(), key=seat_index)
        game_rows = []
        for seat in seats:
            row = seat_row(game_uid, seat, samples.get(seat), events.get(seat),
                           matches.get(game_uid, {}).get(seat))
            game_rows.append(row)
            rows_out.append(row)
        if args.csv or not game_rows:
            continue
        print(f"\n## game {game_uid[:12]}… ({len(game_rows)} seats)")
        print("| seat | bot | faction | outcome | tick | harv | ref | cony | exp |"
              " deploys | prod | tier | idle | army | placed | lost | waves |"
              " first_contact | sw b/f | top units |")
        print("|---|---|---|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--|---|")
        for r in game_rows:
            army = f"{r['army_total']}" if r["army_total"] is not None else "?"
            contact = r["first_contact"] if r["first_contact"] is not None else "-"
            print(f"| {r['seat']} | {r['bot']} | {r['faction']} | {r['outcome']} |"
                  f" {r['tick']} | {r['harv']} | {r['ref']} | {r['cony']} | {r['exp']} |"
                  f" {r['deploys']} | {r['prod']} | {r['tier']} | {r['idle_ticks']} |"
                  f" {army} | {r['placed']} | {r['lost']} | {r['waves']} |"
                  f" {contact} | {r['sw_built']}/{r['sw_fired']} | {r['top_units']} |")
    if args.csv:
        writer = csv.DictWriter(sys.stdout, fieldnames=list(rows_out[0].keys()) if rows_out
                                else ["empty"])
        writer.writeheader()
        writer.writerows(rows_out)
    if not rows_out and not args.csv:
        print("no player-telemetry rows found")
    return 0


if __name__ == "__main__":
    sys.exit(main())
