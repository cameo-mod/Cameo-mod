"""RAID-HARV-01 P3: the raid signal extractor replays the golden vectors it shares with the C# signal functions, validates its rows, attributes samples to the enemy players and to
the main target owner's observed style, and feeds the LEARN-ALL-02 fitter (tools/ai/fit_learn_all.py, imported for real: this file fails loudly when the fitter is missing)."""
import contextlib
import copy
import io
import json
import os
import random
import sys
import tempfile
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "ai"))
import extract_raid_harv_signals as x  # noqa: E402
import fit_learn_all as fit  # noqa: E402

with open(os.path.join(HERE, "raid_harv_signals_golden.json"), encoding="utf-8") as _handle:
    GOLDEN = json.load(_handle)

EVALUATE = {
    "lost_recon": lambda i: x.lost_recon(i["current"], i["reacquire"], i["exhausted"]),
    "move_refresh": lambda i: x.move_refresh(i["drifts"]),
    "req_margin": lambda i: x.req_margin([(r[0], r[1]) for r in i["results"]]),
    "recon_radius": lambda i: x.recon_radius(i["current"], i["distances"], i["exhausted_far_seen"]),
    "incident_loss": lambda i: x.incident_loss([(r[0], r[1]) for r in i["retreats"]]),
    "incident_window": lambda i: x.incident_window(i["current"], i["spans"]),
    "incident_decay": lambda i: x.incident_decay([(r[0], r[1]) for r in i["cells"]]),
    "waypoint_spacing": lambda i: x.waypoint_spacing(i["legs"]),
    "defence_radius": lambda i: x.defence_radius(i["outside"]),
}

ROSTER = [{"player": "e1", "faction": "nod"}, {"player": "e2", "faction": "gdi"}]


def row(key, tick=100, raid="r1", seat="s1", game="g1", player="e1", style="boom", **payload):
    r = {"event": x.EVENT, "game_id": game, "seat": seat, "tick": tick, "raid_id": raid, "target_player": player, "target_style": style, "key": key}
    r.update(payload)
    return r


def waypoint(**kwargs):
    return row("raid.waypoint_spacing", left_lane=False, **kwargs)


def seat_row(seat="s1", game="g1", personality="rush", own="gdi", enemies=None):
    return {"event": x.SEAT_EVENT, "game_id": game, "seat": seat, "personality": personality, "own_faction": own, "enemies": copy.deepcopy(ROSTER if enemies is None else enemies)}


def with_seats(rows, enemies=None):
    """The rows plus one default raid_harv_seat row for every seat that has signal rows."""
    games = {}
    for r in rows:
        games.setdefault(r["seat"], r["game_id"])
    return [seat_row(seat=s, game=g, enemies=enemies) for s, g in sorted(games.items())] + list(rows)


def expand(record):
    """A golden expectation as the extractor writes it: the fitter signal is the permille error / 300."""
    out = copy.deepcopy(record)
    out["knobs"] = [{"knob": k["knob"], "signal": None if k["signal_permille"] is None else k["signal_permille"] / 300.0} for k in record["knobs"]]
    return out


class GoldenSignals(unittest.TestCase):
    def test_the_constants_and_the_key_order_are_the_declared_ones(self):
        c = GOLDEN["constants"]
        self.assertEqual(x.ERROR_CLIP, c["error_clip"])
        self.assertEqual(x.CATEGORICAL, c["categorical"])
        self.assertEqual(x.REFRESH_SETTLE_TICKS, c["refresh_settle_ticks"])
        self.assertEqual(x.LATE_DRIFT_CELLS, c["late_drift_cells"])
        self.assertEqual(x.WASTED_DRIFT_CELLS, c["wasted_drift_cells"])
        self.assertEqual(list(x.KEYS), GOLDEN["keys"])
        self.assertEqual(sorted(GOLDEN["signals"]), sorted(EVALUATE))
        self.assertEqual(sorted("raid." + name for name in EVALUATE), sorted(x.KEYS))

    def test_every_signal_vector_matches(self):
        for name, vectors in GOLDEN["signals"].items():
            for v in vectors:
                self.assertEqual(EVALUATE[name](v["in"]), v["expect"], "%s: %s" % (name, v["name"]))

    def test_every_key_has_a_no_sample_and_a_sample_vector(self):
        for name, vectors in GOLDEN["signals"].items():
            expects = [v["expect"] for v in vectors]
            self.assertIn(None, expects, name)
            self.assertTrue(any(e is not None for e in expects), name)

    def test_a_signal_never_leaves_the_clip(self):
        for name, vectors in GOLDEN["signals"].items():
            for v in vectors:
                e = EVALUATE[name](v["in"])
                self.assertTrue(e is None or -x.ERROR_CLIP <= e <= x.ERROR_CLIP, "%s: %s" % (name, v["name"]))

    def test_c_style_division_truncates_toward_zero(self):
        self.assertEqual([x.tdiv(7, 2), x.tdiv(-7, 2), x.tdiv(7, -2), x.tdiv(-7, -2), x.tdiv(0, 5)], [3, -3, -3, 3, 0])
        self.assertEqual(x.median([5, 1, 3]), 3)
        self.assertEqual(x.median([1, 2]), 1)
        self.assertEqual(x.median([-1, -2]), -1)
        self.assertEqual(x.median([]), 0)

    def test_the_refresh_eligibility_window(self):
        for v in GOLDEN["refresh_eligibility"]:
            r = x.parse_row(row("raid.move_refresh", drift=2, visible_prev=v["visible_prev"], visible_now=v["visible_now"], phase=v["phase"], since_change=v["since_change"]), "vector")
            self.assertEqual(r["counts"], v["eligible"], v)


class GoldenGames(unittest.TestCase):
    def test_whole_games_produce_the_expected_exposure_records(self):
        for g in GOLDEN["games"]:
            records, warnings = x.build_records(g["rows"], g["seq"])
            self.assertEqual(records, [expand(r) for r in g["expect_records"]], g["name"])
            self.assertEqual(len(warnings), g["expect_warnings"], g["name"])

    def test_the_row_order_does_not_change_the_records(self):
        rng = random.Random(7)
        for g in GOLDEN["games"]:
            rows = list(g["rows"])
            rng.shuffle(rows)
            records, warnings = x.build_records(rows, g["seq"])
            self.assertEqual(records, [expand(r) for r in g["expect_records"]], g["name"])
            self.assertEqual(len(warnings), g["expect_warnings"], g["name"])

    def test_every_record_has_the_nine_knobs_in_key_order_and_a_bounded_signal(self):
        for g in GOLDEN["games"]:
            records, _ = x.build_records(g["rows"], g["seq"])
            for rec in records:
                self.assertEqual([k["knob"] for k in rec["knobs"]], list(x.KEYS))
                for k in rec["knobs"]:
                    self.assertTrue(k["signal"] is None or -1.0 <= k["signal"] <= 1.0)
                self.assertTrue(rec["seat"].startswith(x.SEAT_PREFIX))

    def test_a_seat_without_evidence_writes_no_record(self):
        rows = [row("raid.req_margin", killed=False, loss_permille=900), row("raid.move_refresh", drift=0, visible_prev=True, visible_now=False, phase="Travel", since_change=99)]
        self.assertEqual(x.build_records(with_seats(rows), 1), ([], []))
        self.assertEqual(x.build_records([], 1), ([], []))
        self.assertEqual(x.build_records([seat_row()], 1), ([], []))

    def test_the_records_survive_a_json_round_trip(self):
        for g in GOLDEN["games"]:
            records, _ = x.build_records(g["rows"], g["seq"])
            self.assertEqual([json.loads(json.dumps(r)) for r in records], records)


class RowValidation(unittest.TestCase):
    def fails(self, code, rows, seq=1):
        with self.assertRaises(x.ExtractError) as ctx:
            x.build_records(rows, seq)
        self.assertEqual(ctx.exception.code, code)

    def test_a_defective_signal_row_is_refused_with_exit_code_5(self):
        bad = [
            row("raid.nonsense"),
            row(["raid.lost_recon"]),
            waypoint(raid=""),
            dict(waypoint(), raid_id=None),
            dict(waypoint(), tick=True),
            dict(waypoint(), tick=-1),
            dict(waypoint(), seat=""),
            dict(waypoint(), left_lane=1),
            dict(waypoint(), target_player=""),
            dict(waypoint(), target_player=None),
            dict(waypoint(), target_style=""),
            row("raid.lost_recon", kind="reacquired", ticks=100),
            row("raid.lost_recon", kind="reacquired", current=0, ticks=100),
            row("raid.lost_recon", kind="reacquired", current=400),
            row("raid.lost_recon", kind="vanished", current=400),
            row("raid.recon_radius", kind="exhausted", current=8),
            row("raid.req_margin", killed=True, loss_permille=1001),
            row("raid.req_margin", killed=1, loss_permille=10),
            row("raid.move_refresh", drift=1, visible_prev=True, visible_now=True, phase="Travel"),
        ]
        for r in bad:
            self.fails(5, with_seats([waypoint()]) + [r])

    def test_a_defective_seat_row_is_refused_with_exit_code_5(self):
        base = [waypoint()]
        self.fails(5, [dict(seat_row(), enemies="e1")] + base)
        self.fails(5, [dict(seat_row(), enemies=[{"player": "e1"}])] + base)
        self.fails(5, [dict(seat_row(), enemies=[{"player": "", "faction": "nod"}])] + base)
        self.fails(5, [dict(seat_row(), enemies=["e1"])] + base)
        self.fails(5, [seat_row(enemies=[{"player": "e1", "faction": "nod"}, {"player": "e1", "faction": "gdi"}])] + base)
        self.fails(5, [dict(seat_row(), personality="")] + base)
        self.fails(5, [dict(seat_row(), seat="")] + base)
        self.fails(5, [seat_row(), seat_row()] + base)

    def test_a_seat_with_signals_needs_a_roster_and_the_target_must_be_on_it(self):
        self.fails(5, [waypoint()])
        self.fails(5, with_seats([waypoint(player="ghost")]))
        self.fails(5, [seat_row(seat="other")] + [waypoint()])

    def test_rows_of_more_than_one_game_are_refused_with_exit_code_3(self):
        self.fails(3, with_seats([waypoint(game="g1")]) + [seat_row(game="g2", seat="z")])
        self.fails(3, with_seats([waypoint(game="g1"), waypoint(game="g2", seat="z")]))

    def test_a_bad_game_id_or_seq_is_refused(self):
        self.fails(5, with_seats([waypoint(game="bad game")]))
        self.fails(5, with_seats([waypoint()]), seq=-1)
        self.fails(5, with_seats([waypoint()]), seq=True)
        self.fails(5, with_seats([waypoint()]), seq="1")

    def test_select_rows_keeps_only_raid_rows_and_the_asked_game(self):
        raw = [{"event": "squad_event"}, "text", 5, seat_row(game="g1"), waypoint(game="g1"), waypoint(game="g2")]
        self.assertEqual(len(x.select_rows(raw)), 3)
        self.assertEqual([r["game_id"] for r in x.select_rows(raw, "g2")], ["g2"])


class Attribution(unittest.TestCase):
    def test_every_enemy_player_is_listed_once_with_weight_one(self):
        roster = [{"player": "e1", "faction": "nod"}, {"player": "e2", "faction": "nod"}, {"player": "e3", "faction": "allies"}, {"player": "e4", "faction": "gdi"}]
        rows = [waypoint(raid="r1", tick=10, player="e1"), waypoint(raid="r2", tick=20, player="e1"), waypoint(raid="r3", tick=30, player="e1")]
        (rec,), _ = x.build_records(with_seats(rows, roster), 1)
        self.assertEqual([(e["faction"], e["weight"]) for e in rec["enemies"]], [("nod", 1), ("nod", 1), ("allies", 1), ("gdi", 1)], "per player, not per raid; the never-raided players stay")

    def test_only_the_main_target_owners_style_is_emitted(self):
        rows = [waypoint(raid="r1", tick=10, player="e1", style="boom"), waypoint(raid="r2", tick=20, player="e2", style="turtle")]
        (rec,), _ = x.build_records(with_seats(rows), 1)
        self.assertEqual((rec["main_target"], rec["main_target_style"]), ("nod", "boom"))
        self.assertEqual([sorted(e) for e in rec["enemies"]], [["faction", "weight"]] * 2, "the other enemy's style is not blended in: no enemy entry carries a style")

    def test_an_unseen_style_is_null_everywhere(self):
        (rec,), _ = x.build_records(with_seats([waypoint(player="e1", style=None)]), 1)
        self.assertEqual((rec["main_target"], rec["main_target_style"]), ("nod", None))
        self.assertEqual([sorted(e) for e in rec["enemies"]], [["faction", "weight"]] * 2)

    def test_the_main_target_is_the_owner_with_the_most_raids_then_the_earliest_then_the_player_id(self):
        rows = [waypoint(raid="a", tick=10, player="e1"), waypoint(raid="b", tick=50, player="e2", style=None), waypoint(raid="c", tick=60, player="e2", style=None)]
        (rec,), _ = x.build_records(with_seats(rows), 1)
        self.assertEqual(rec["main_target"], "gdi", "e2 has two raids")
        rows = [waypoint(raid="a", tick=70, player="e1"), waypoint(raid="b", tick=50, player="e2", style=None)]
        (rec,), _ = x.build_records(with_seats(rows), 1)
        self.assertEqual(rec["main_target"], "gdi", "tie on count: the earlier first sample wins")
        rows = [waypoint(raid="a", tick=50, player="e2", style=None), waypoint(raid="b", tick=50, player="e1")]
        (rec,), _ = x.build_records(with_seats(rows), 1)
        self.assertEqual(rec["main_target"], "nod", "tie on count and tick: the player id")

    def test_a_raid_with_no_counted_sample_does_not_make_its_owner_the_main_target(self):
        rows = [waypoint(raid="r1", player="e1"), row("raid.move_refresh", raid="r9", player="e2", style=None, drift=5, visible_prev=True, visible_now=True, phase="Travel", since_change=10)]
        (rec,), _ = x.build_records(with_seats(rows), 1)
        self.assertEqual(rec["main_target"], "nod")
        self.assertEqual(len(rec["enemies"]), 2, "the player stays in the list: every enemy player counts")

    def test_the_style_is_the_latest_counted_raids_and_a_difference_warns(self):
        rows = [waypoint(raid="r1", tick=10, player="e1", style="boom"), waypoint(raid="r2", tick=50, player="e1", style=None)]
        (rec,), warnings = x.build_records(with_seats(rows), 1)
        self.assertIsNone(rec["main_target_style"])
        self.assertEqual(len(warnings), 1)

    def test_a_later_row_that_disagrees_with_the_pick_only_warns(self):
        rows = [waypoint(raid="r1", tick=10, player="e1", style="boom"), waypoint(raid="r1", tick=20, player="e2", style=None)]
        (rec,), warnings = x.build_records(with_seats(rows), 1)
        self.assertEqual((rec["main_target"], rec["main_target_style"]), ("nod", "boom"))
        self.assertEqual(len(warnings), 1)

    def test_seats_do_not_share_raid_ids_or_rosters(self):
        rows = [waypoint(seat="s1", raid="r1", player="e1"), waypoint(seat="s2", raid="r1", player="e2", style=None)]
        records, _ = x.build_records(with_seats(rows), 1)
        self.assertEqual([(r["seat"], r["main_target"]) for r in records], [("raid:s1", "nod"), ("raid:s2", "gdi")])

    def test_the_seat_row_carries_the_personality_and_the_faction(self):
        rows = [seat_row(personality="turtle", own="allies")] + [waypoint()]
        (rec,), _ = x.build_records(rows, 1)
        self.assertEqual((rec["personality"], rec["own_faction"]), ("turtle", "allies"))
        rows = [seat_row(personality=None, own=None)] + [waypoint()]
        (rec,), _ = x.build_records(rows, 1)
        self.assertEqual((rec["personality"], rec["own_faction"]), (None, None))

    def test_different_current_values_warn_and_the_earliest_row_is_used(self):
        rows = [row("raid.lost_recon", tick=10, kind="reacquired", ticks=300, current=400), row("raid.lost_recon", tick=20, kind="reacquired", ticks=300, current=500)]
        (rec,), warnings = x.build_records(with_seats(rows), 1)
        self.assertEqual(rec["knobs"][0]["signal"], -62 / 300.0)
        self.assertEqual(len(warnings), 1)
        self.assertIn("raid.lost_recon", warnings[0])


class Cli(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.TemporaryDirectory()

    def tearDown(self):
        self.dir.cleanup()

    def write_log(self, name, lines):
        path = os.path.join(self.dir.name, name)
        with open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write("".join(lines))
        return path

    def run_main(self, *argv):
        err = io.StringIO()
        with contextlib.redirect_stderr(err), contextlib.redirect_stdout(io.StringIO()):
            code = x.main(list(argv))
        return code, err.getvalue()

    def read_records(self, path):
        with open(path, encoding="utf-8") as handle:
            return [json.loads(line) for line in handle if line.strip()]

    def game_one_lines(self):
        game = GOLDEN["games"][0]
        lines = ['{"event": "squad_event", "tick": 5}\n', "not json at all\n", "\n"]
        lines += [json.dumps(r) + "\n" for r in game["rows"]]
        return game, lines

    def test_the_cli_writes_the_exposure_file_of_one_game(self):
        game, lines = self.game_one_lines()
        log = self.write_log("dev.jsonl", lines)
        out = os.path.join(self.dir.name, "raid_harv_exposure.jsonl")
        code, _ = self.run_main("--rows", log, "--seq", str(game["seq"]), "--out", out)
        self.assertEqual(code, 0)
        self.assertEqual(self.read_records(out), [expand(r) for r in game["expect_records"]])

    def test_mixed_games_need_a_game_id(self):
        game, lines = self.game_one_lines()
        other = [json.dumps(seat_row(seat="z", game="g9")) + "\n", json.dumps(waypoint(game="g9", seat="z")) + "\n"]
        log = self.write_log("dev.jsonl", lines + other)
        out = os.path.join(self.dir.name, "out.jsonl")
        self.assertEqual(self.run_main("--rows", log, "--seq", "12", "--out", out)[0], 3)
        self.assertEqual(self.run_main("--rows", log, "--seq", "12", "--game-id", "g1", "--out", out)[0], 0)
        self.assertEqual([r["game_id"] for r in self.read_records(out)], ["g1"])

    def test_a_game_without_raid_evidence_writes_an_empty_file(self):
        log = self.write_log("dev.jsonl", ['{"event": "squad_event"}\n'])
        out = os.path.join(self.dir.name, "out.jsonl")
        code, err = self.run_main("--rows", log, "--seq", "1", "--out", out)
        self.assertEqual(code, 0)
        self.assertTrue(os.path.exists(out))
        self.assertEqual(self.read_records(out), [])
        self.assertIn("nothing to apply", err)

    def test_a_malformed_raid_line_is_skipped_with_a_warning(self):
        game, lines = self.game_one_lines()
        log = self.write_log("dev.jsonl", lines + ['{"event": "raid_harv_signal", broken\n'])
        out = os.path.join(self.dir.name, "out.jsonl")
        code, err = self.run_main("--rows", log, "--seq", "12", "--out", out)
        self.assertEqual(code, 0)
        self.assertIn("malformed", err)
        self.assertEqual(self.read_records(out), [expand(r) for r in game["expect_records"]])

    def test_an_invalid_row_exits_with_5(self):
        log = self.write_log("dev.jsonl", [json.dumps(row("raid.nonsense")) + "\n"])
        out = os.path.join(self.dir.name, "out.jsonl")
        self.assertEqual(self.run_main("--rows", log, "--seq", "1", "--out", out)[0], 5)


class FitterCompatibility(unittest.TestCase):
    """The records are the exposure format of tools/ai/fit_learn_all.py (LA02 8d7fa6f4): this runs against the real fitter file, never skips."""

    def records(self, index):
        game = GOLDEN["games"][index]
        records, _ = x.build_records(game["rows"], game["seq"])
        return game, records

    def test_the_records_are_valid_fitter_input_with_the_nine_knobs(self):
        game, records = self.records(0)
        self.assertEqual(fit.check_records(records), ("g1", 12, None))
        grouped = fit.seats_by_knob(records)
        self.assertEqual(sorted(grouped), sorted(x.KEYS))
        seat = grouped["raid.lost_recon"][0]
        self.assertEqual((seat.personality, seat.own_faction, seat.main_target), ("rush", "gdi", "nod"))
        self.assertEqual(seat.enemies, [("nod", 1), ("nod", 1), ("allies", 1), ("gdi", 1)])

    def test_one_game_takes_one_bounded_step_on_the_signal_over_300(self):
        game, records = self.records(0)
        table = fit.Table()
        self.assertGreater(fit.apply_records(table, "g1", records), 0)
        axes = table.personalities["rush"]["raid.lost_recon"]
        self.assertEqual(axes.global_, fit.step_next(1000, -108 / 300.0))
        self.assertEqual(axes.own["gdi"], axes.global_)
        self.assertEqual(sorted(axes.enemy), ["allies", "gdi", "nod"], "every enemy player's faction takes a step, nod twice as a share")
        self.assertEqual(sorted(axes.style), ["boom"], "only the main target owner's observed style is learned; the other enemy's style is never read")
        self.assertEqual(axes.style["boom"], axes.global_, "the whole S step goes to the main target owner's observed style (from the same seed 1000 it equals the global step)")
        self.assertEqual(records[0]["main_target_style"], "boom")

    def test_a_knob_without_a_sample_is_left_alone(self):
        game, records = self.records(1)
        table = fit.Table()
        fit.apply_records(table, "g2", records)
        self.assertNotIn("raid.lost_recon", table.personalities.get("turtle", {}))
        self.assertIn("raid.req_margin", table.personalities["turtle"])

    def test_an_unseen_main_target_style_learns_no_style_axis(self):
        game, records = self.records(1)
        table = fit.Table()
        fit.apply_records(table, "g2", records)
        axes = table.personalities["any"]["raid.waypoint_spacing"]
        self.assertEqual(axes.style, {})
        self.assertEqual(sorted(axes.enemy), ["gdi"])
        self.assertEqual(axes.own, {})

    def test_the_raid_seats_do_not_collide_with_a_wave_role_exposure_of_the_same_game(self):
        game, records = self.records(0)
        wave = {"game_id": "g1", "seq": 12, "seat": "bot1", "personality": "rush", "own_faction": "gdi", "main_target": "nod", "main_target_style": "boom",
                "enemies": [{"faction": "nod", "weight": 1}], "knobs": [{"knob": "wave_role.frontline", "signal": 0.4}]}
        self.assertEqual(fit.check_records([wave] + records), ("g1", 12, None))


if __name__ == "__main__":
    unittest.main()
