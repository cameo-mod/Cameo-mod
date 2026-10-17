#!/usr/bin/env python3
"""Tests for the PLAYTEST-TELEMETRY-V2 channel readers (replay_health.players_channel
and playtest_stats_from_logs)."""
from __future__ import annotations

import hashlib
import json
import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))

import playtest_stats_from_logs as stats  # noqa: E402
import replay_health as health  # noqa: E402

UID = "11111111-2222-3333-4444-555555555555"


def sample(seat, tick, **kw):
    row = {
        "schema": "cameo-player-sample", "version": 1, "game_uid": UID,
        "record_id": f"{UID}|{seat}|sample|{tick}", "tick": tick, "seat": seat,
        "bot_type": kw.get("bot_type", ""), "faction": kw.get("faction", "td_gdi"),
        "team": kw.get("team", 0), "harvesters": 4, "refineries": 2,
        "conyards": 2, "expansions": 1, "mcv_deploys": 1,
        "production_buildings": 2, "tech_tier": 3, "idle_production_ticks": 750,
        "buildings": 9, "units": 31,
        "army": {"infantry": 500, "vehicle": 2600, "air": 0, "naval": 0,
                 "defence": 900},
        "unit_types": {"e1": 10, "hmmv": 12, "mttnk": 9},
    }
    row.update(kw)
    return row


def event(seat, kind, tick, **kw):
    row = {
        "schema": "cameo-player-event", "version": 1, "game_uid": UID,
        "record_id": f"{UID}|{seat}|{kind}|{tick}|", "tick": tick, "seat": seat,
        "kind": kind,
    }
    row.update(kw)
    return row


def botqueue(seat, tick, **kw):
    row = {
        "schema": "cameo-bot-queue", "version": 1, "game_uid": UID,
        "record_id": f"{UID}|{seat}|queue|{tick}", "tick": tick, "seat": seat,
        "banked": 10800, "spent_delta": 3400,
        "requested": {"unit": 4, "defence": 0, "crawl_link": 0, "tech": 0,
                      "economy": 0},
        "accepted": {"unit": 3, "defence": 1, "crawl_link": 0, "tech": 0,
                     "economy": 0},
        "completed": {"unit": 2, "defence": 0, "crawl_link": 0, "tech": 1,
                      "economy": 0},
        "skipped": {"should_build": 1},
        "spend": {"unit": 2400, "defence": 1000, "crawl_link": 0, "tech": 0,
                  "economy": 0},
        "queues": [
            {"queue": "Vehicle", "group": "vehicles", "state": "idle",
             "reason": "cash_reserved",
             "reserved_by": ["BaseBuilderBotModuleCA"]},
            {"queue": "Infantry", "group": "infantry", "state": "busy",
             "item": "e1", "reason": "producing"},
        ],
    }
    row.update(kw)
    return row


def write_channel(directory, rows):
    logs = pathlib.Path(directory) / "Logs"
    logs.mkdir(parents=True, exist_ok=True)
    name = f"cameo-ai-players-{hashlib.sha256(UID.encode('utf-8')).hexdigest()}.jsonl"
    path = logs / name
    path.write_text("".join(json.dumps(r) + "\n" for r in rows), encoding="utf-8")
    return path


class ChannelFixture(unittest.TestCase):
    def rows(self):
        return [
            event("seat_1", "building_placed", 1200, actor="powr", cell="12,40",
                  start_dist=3.6),
            event("seat_1", "first_unit_enemy_territory", 3900, actor="hmmv",
                  cell="80,10", to_seat="seat_3"),
            event("seat_1", "attack_wave_start", 8000, cell="60,30", units=7,
                  to_seat="seat_3"),
            event("seat_2", "building_placed", 1500, actor="proc", cell="90,20",
                  start_dist=4.2),
            event("seat_2", "conyard_deployed", 12000, actor="naconst",
                  cell="75,35", start_dist=21.4),
            event("seat_2", "superweapon_fired", 20000, actor="nuke"),
            sample("seat_1", 750, bot_type="", faction="td_gdi"),
            sample("seat_1", 1500),
            sample("seat_2", 750, bot_type="cameo_hard", faction="td_nod",
                   team=1),
        ]

    def test_players_channel_validates_and_counts_events(self):
        with tempfile.TemporaryDirectory() as temp:
            write_channel(temp, self.rows())
            receipt, metrics = health.players_channel(
                pathlib.Path(temp) / "Logs", UID, "seat_1")
            self.assertIsNotNone(receipt)
            self.assertEqual(receipt["rows"], 9)
            self.assertEqual(metrics["player_channel_samples"], 3)
            self.assertEqual(metrics["player_channel_last_sample_tick"], 1500)
            self.assertEqual(metrics["player_channel_events"],
                             {"building_placed": 1,
                              "first_unit_enemy_territory": 1,
                              "attack_wave_start": 1})

    def test_missing_channel_is_silent(self):
        with tempfile.TemporaryDirectory() as temp:
            (pathlib.Path(temp) / "Logs").mkdir()
            receipt, metrics = health.players_channel(
                pathlib.Path(temp) / "Logs", UID, "seat_1")
            self.assertIsNone(receipt)
            self.assertEqual(metrics, {})

    def test_unknown_event_kind_and_field_fail_closed(self):
        with tempfile.TemporaryDirectory() as temp:
            bad = event("seat_1", "nuclear_launch", 10)
            write_channel(temp, [bad])
            with self.assertRaisesRegex(health.EvidenceError, "kind"):
                health.players_channel(pathlib.Path(temp) / "Logs", UID, "seat_1")
        with tempfile.TemporaryDirectory() as temp:
            bad = sample("seat_1", 750)
            bad["new_field"] = 1
            write_channel(temp, [bad])
            with self.assertRaisesRegex(health.EvidenceError, "unsupported"):
                health.players_channel(pathlib.Path(temp) / "Logs", UID, "seat_1")

    def test_non_monotonic_seat_samples_fail_closed(self):
        with tempfile.TemporaryDirectory() as temp:
            write_channel(temp, [sample("seat_1", 1500), sample("seat_1", 750)])
            with self.assertRaisesRegex(health.EvidenceError, "monotonic"):
                stats.read_channel(pathlib.Path(temp) / "Logs" /
                    f"cameo-ai-players-{hashlib.sha256(UID.encode()).hexdigest()}.jsonl")

    def test_same_tick_final_sample_keeps_last(self):
        # A game ending on a sample tick emits a same-tick final row;
        # the reader keeps the last snapshot rather than failing.
        with tempfile.TemporaryDirectory() as temp:
            final = sample("seat_1", 1500)
            final["harvesters"] = 7
            write_channel(temp, [sample("seat_1", 750), sample("seat_1", 1500), final])
            samples, _, _ = stats.read_channel(
                pathlib.Path(temp) / "Logs" /
                f"cameo-ai-players-{hashlib.sha256(UID.encode()).hexdigest()}.jsonl")
            self.assertEqual(samples["seat_1"]["harvesters"], 7)

    def test_event_seq_and_actor_id_validate(self):
        with tempfile.TemporaryDirectory() as temp:
            rows = [
                event("seat_1", "building_placed", 1200, actor="powr",
                      seq=1, actor_id=41, cell="12,40", start_dist=3.6),
                event("seat_1", "structure_lost", 5000, actor="powr",
                      seq=2, actor_id=41, cause="killed"),
                event("seat_1", "building_placed", 1200, actor="proc",
                      seq=3, actor_id=42, cell="14,40", start_dist=5.0),
            ]
            write_channel(temp, rows)
            _, metrics = health.players_channel(
                pathlib.Path(temp) / "Logs", UID, "seat_1")
            self.assertEqual(metrics["player_channel_events"],
                             {"building_placed": 2, "structure_lost": 1})
            _, events, _ = stats.read_channel(
                pathlib.Path(temp) / "Logs" /
                f"cameo-ai-players-{hashlib.sha256(UID.encode()).hexdigest()}.jsonl")
            self.assertEqual(events["seat_1"]["structure_lost"], 1)
        with tempfile.TemporaryDirectory() as temp:
            bad = event("seat_1", "building_placed", 10, seq="abc")
            write_channel(temp, [bad])
            with self.assertRaises(health.EvidenceError):
                stats.read_channel(pathlib.Path(temp) / "Logs" /
                    f"cameo-ai-players-{hashlib.sha256(UID.encode()).hexdigest()}.jsonl")

    def test_stats_rows_cover_every_seat(self):
        with tempfile.TemporaryDirectory() as temp:
            write_channel(temp, self.rows())
            samples, events, _ = stats.read_channel(
                pathlib.Path(temp) / "Logs" /
                f"cameo-ai-players-{hashlib.sha256(UID.encode()).hexdigest()}.jsonl")
            self.assertEqual(set(samples), {"seat_1", "seat_2"})
            row = stats.seat_row(UID, "seat_2", samples["seat_2"],
                                 events["seat_2"], None, None)
            self.assertEqual(row["bot"], "cameo_hard")
            self.assertEqual(row["deploys"], 1)
            self.assertEqual(row["exp"], 1)
            self.assertEqual(row["sw_fired"], 1)
            self.assertEqual(row["placed"], 1)
            self.assertEqual(row["army_total"], 4000)
            self.assertIn("hmmv", row["top_units"])
            self.assertIsNone(row["first_contact"])

    def test_bot_queue_row_validates_and_aggregates(self):
        with tempfile.TemporaryDirectory() as temp:
            write_channel(temp, [
                botqueue("seat_2", 250),
                botqueue("seat_2", 500, banked=12000, spent_delta=800),
            ])
            _, metrics = health.players_channel(
                pathlib.Path(temp) / "Logs", UID, "seat_2")
            self.assertEqual(metrics["player_channel_queue_rows"], 2)
            _, _, queues = stats.read_channel(
                pathlib.Path(temp) / "Logs" /
                f"cameo-ai-players-{hashlib.sha256(UID.encode()).hexdigest()}.jsonl")
            agg = queues["seat_2"]
            self.assertEqual(agg["rows"], 2)
            self.assertEqual(agg["banked"], 12000)
            self.assertEqual(agg["spent_total"], 4200)
            self.assertEqual(agg["requested"]["unit"], 8)
            self.assertEqual(agg["accepted"]["defence"], 2)
            self.assertEqual(agg["skipped"]["should_build"], 2)
            self.assertEqual(agg["hold_reasons"]["cash_reserved"], 2)
            row = stats.seat_row(UID, "seat_2", None, {}, agg, None)
            self.assertEqual(row["req"], 8)
            self.assertEqual(row["acc"], 8)
            self.assertEqual(row["done"], 6)
            self.assertEqual(row["spent_total"], 4200)
            self.assertIn("cash_reserved", row["hold_top"])

    def test_bot_queue_bad_shapes_fail_closed(self):
        # An unsupported hold reason must not pass silently.
        with tempfile.TemporaryDirectory() as temp:
            bad = botqueue("seat_2", 250)
            bad["queues"][0]["reason"] = "gremlins"
            write_channel(temp, [bad])
            with self.assertRaisesRegex(health.EvidenceError, "reason"):
                stats.read_channel(pathlib.Path(temp) / "Logs" /
                    f"cameo-ai-players-{hashlib.sha256(UID.encode()).hexdigest()}.jsonl")
        # Unknown skip counters are additive-with-review, never silent.
        with tempfile.TemporaryDirectory() as temp:
            bad = botqueue("seat_2", 250)
            bad["skipped"]["invented_reason"] = 1
            write_channel(temp, [bad])
            with self.assertRaisesRegex(health.EvidenceError, "skip"):
                stats.read_channel(pathlib.Path(temp) / "Logs" /
                    f"cameo-ai-players-{hashlib.sha256(UID.encode()).hexdigest()}.jsonl")
        # Unknown category keys fail the strict allow-list.
        with tempfile.TemporaryDirectory() as temp:
            bad = botqueue("seat_2", 250)
            bad["requested"]["mystery"] = 1
            write_channel(temp, [bad])
            with self.assertRaisesRegex(health.EvidenceError, "requested"):
                stats.read_channel(pathlib.Path(temp) / "Logs" /
                    f"cameo-ai-players-{hashlib.sha256(UID.encode()).hexdigest()}.jsonl")
        # A non-integer bank must fail the integer check.
        with tempfile.TemporaryDirectory() as temp:
            bad = botqueue("seat_2", 250, banked=-5)
            write_channel(temp, [bad])
            with self.assertRaises(health.EvidenceError):
                stats.read_channel(pathlib.Path(temp) / "Logs" /
                    f"cameo-ai-players-{hashlib.sha256(UID.encode()).hexdigest()}.jsonl")


if __name__ == "__main__":
    unittest.main()
