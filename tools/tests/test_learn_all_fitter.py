"""LEARN-ALL-02 P1c: the python fitter mirrors the C# core (golden vectors) and obeys the pipeline guards."""
import json
import os
import sys
import tempfile
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "ai"))
import fit_learn_all as f  # noqa: E402

GOLDEN = json.load(open(os.path.join(HERE, "learn_all_golden.json"), encoding="utf-8"))


def sig(x):
    return float("nan") if x == "nan" else float("inf") if x == "inf" else x


def seats_of(application, reverse=False):
    seats = [f.Seat(s["personality"], s["own"], [(e["faction"], e["weight"]) for e in s["enemies"]], s["main_target"], s["main_target_style"], s["signal"])
             for s in application["seats"]]
    return list(reversed(seats)) if reverse else seats


class GoldenReplay(unittest.TestCase):
    def test_math(self):
        for v in GOLDEN["geomean"]:
            self.assertEqual(f.geo_mean(v["values"]), v["expect"], v)
        for v in GOLDEN["resolve"]:
            self.assertEqual(f.resolve(v["g"], v["f"], v["e"], v["s"]), v["expect"], v)
        for v in GOLDEN["team_enemy"]:
            self.assertEqual(f.team_enemy(v["per"], v["main"]), v["expect"], v)
        for v in GOLDEN["step"]:
            self.assertEqual(f.step_next(v["current"], sig(v["signal"])), v["expect"], v)

    def replay(self, game, reverse):
        t, changed = f.Table(), []
        for a in game["applications"]:
            changed.append(f.apply_game(t, a["game_id"], a["knob"], seats_of(a, reverse)))
        return t, changed

    def test_games(self):
        for g in GOLDEN["games"]:
            t, changed = self.replay(g, False)
            self.assertEqual(changed, g["changed"], g["name"])
            self.assertEqual(f.write_text(t), g["expect_text"], g["name"])

    def test_seat_order_does_not_change_the_result(self):
        for g in GOLDEN["games"]:
            t, _ = self.replay(g, True)
            self.assertEqual(f.write_text(t), g["expect_text"], g["name"])

    def test_text_round_trip(self):
        for g in GOLDEN["games"]:
            t = f.parse_text(g["expect_text"])
            self.assertEqual(f.write_text(t), g["expect_text"], g["name"])

    def test_damaged_text_is_empty(self):
        for text in (None, "", "Other:\n\tVersion: 1\n", "BotLearnAll:\n\tVersion: 2\n\tKnob@any__k:\n\t\tGlobal: 1500\n", "BotLearnAll:\n\tKnob@any__k:\n\t\tGlobal: 1500\n"):
            self.assertEqual(f.parse_text(text).personalities, {}, text)

    def test_every_seat_counts_not_only_the_first(self):
        t = f.Table()
        a = f.Seat("rush", "gdi", [("nod", 1)], "nod", None, 1.0)
        b = f.Seat("rush", "allies", [("nod", 1)], "nod", None, -1.0)
        self.assertTrue(f.apply_game(t, "g1", "k", [a, b]))
        axes = t.personalities["rush"]["k"]
        self.assertEqual(axes.global_, 1000, "+1 and -1 average out: the second seat was not dropped")
        self.assertEqual(axes.own["gdi"], 1041)
        self.assertEqual(axes.own["allies"], 961)
        self.assertEqual(len(t.applied), 2, "one (game, personality, knob) key per table: rush and any")

    def test_the_style_seed_is_the_pre_step_team_enemy_with_the_actual_main_target(self):
        t = f.Table()
        axes = t.personalities.setdefault("any", {}).setdefault("k", f.Axes())
        axes.enemy["nod"] = 1000
        axes.enemy["allies"] = 4000
        main_nod = f.Seat("any", "gdi", [("nod", 1), ("allies", 1)], "nod", "boom", 0.0)
        f.apply_game(t, "g1", "k", [main_nod])
        # E_eff = geo(geo(1000, 4000) = 2000, main nod = 1000) = 1414; seed = geo(1000, 1000, 1414) = 1122
        self.assertEqual(t.personalities["any"]["k"].style["boom"], f.geo_mean([1000, 1000, 1414]))
        t2 = f.Table()
        axes2 = t2.personalities.setdefault("any", {}).setdefault("k", f.Axes())
        axes2.enemy["nod"] = 1000
        axes2.enemy["allies"] = 4000
        f.apply_game(t2, "g1", "k", [f.Seat("any", "gdi", [("nod", 1), ("allies", 1)], "allies", "boom", 0.0)])
        self.assertEqual(t2.personalities["any"]["k"].style["boom"], f.geo_mean([1000, 1000, 2828]), "main target allies: the seed moves with it")

    def test_a_team_game_gives_the_whole_style_step_to_the_main_target_owner_only(self):
        # 3 enemy seats (2 soviet, 1 allies); the bot observed boom on the main target (allies). The S step is the FULL step, not split by weight.
        t = f.Table()
        f.apply_game(t, "g1", "k", [f.Seat("any", "gdi", [("soviet", 1), ("soviet", 1), ("allies", 2)], "allies", "boom", 1.0)])
        axes = t.personalities["any"]["k"]
        self.assertEqual(axes.style["boom"], 1041, "the whole +1 step: exp(0.04)")
        self.assertEqual(list(axes.style), ["boom"], "no other style exists: the other enemies' styles are never read")
        self.assertEqual(axes.enemy["soviet"], 1020, "E is still split by player count (half the budget)")
        self.assertEqual(axes.enemy["allies"], 1020)

    def test_an_unseen_main_target_style_takes_no_s_step_and_stays_the_three_axis_result(self):
        t = f.Table()
        f.apply_game(t, "g1", "k", [f.Seat("any", "gdi", [("nod", 1), ("allies", 1)], "nod", None, 1.0)])
        axes = t.personalities["any"]["k"]
        self.assertEqual(axes.style, {}, "unseen = no S evidence")
        self.assertEqual(f.resolve(axes.global_, axes.get_own("gdi"), axes.get_enemy("nod"), 0), f.resolve(axes.global_, axes.get_own("gdi"), axes.get_enemy("nod"), axes.style.get("boom", 0)))

    def test_one_vs_one_is_the_same_as_before(self):
        t = f.Table()
        f.apply_game(t, "g1", "k", [f.Seat("any", "allies", [("soviet", 1)], "soviet", "boom", 1.0)])
        self.assertEqual(t.personalities["any"]["k"].style["boom"], 1041)

    def test_seats_that_saw_different_main_target_styles_each_step_their_own_style(self):
        t = f.Table()
        a = f.Seat("rush", "gdi", [("nod", 1), ("allies", 1)], "nod", "boom", 1.0)
        b = f.Seat("turtle", "allies", [("nod", 1), ("allies", 1)], "allies", "turtle", -1.0)
        f.apply_game(t, "g1", "k", [a, b])
        self.assertEqual(t.personalities["any"]["k"].style["boom"], 1041)
        self.assertEqual(t.personalities["any"]["k"].style["turtle"], 961)


class PerPlayerSeed(unittest.TestCase):
    """Terra 66B4FF2E finding 2 + no-target parity: the V3 seed counts PLAYERS (an entry with weight w is w players) and no main target = the all-enemy aggregate."""

    def preset(self, nod, allies):
        t = f.Table()
        axes = t.personalities.setdefault("any", {}).setdefault("k", f.Axes())
        axes.enemy["nod"] = nod
        axes.enemy["allies"] = allies
        return t

    def test_a_weighted_entry_counts_once_per_player(self):
        # A (nod) weight 3 = 1000, B (allies) weight 1 = 16000, main = A: all-player geomean = (1000^3 x 16000)^(1/4) = 2000, E_eff = geo(2000, 1000) = 1414.
        t = self.preset(1000, 16000)
        f.apply_game(t, "g1", "k", [f.Seat("any", "gdi", [("nod", 3), ("allies", 1)], "nod", "boom", 0.0)])
        self.assertEqual(t.personalities["any"]["k"].style["boom"], f.geo_mean([1000, 1000, 1414]), "per-player counting, not one entry per record (that would be 4000 -> E_eff 2000)")

    def test_the_same_players_as_separate_entries_give_the_same_seed(self):
        a, b = self.preset(1000, 16000), self.preset(1000, 16000)
        f.apply_game(a, "g1", "k", [f.Seat("any", "gdi", [("nod", 3), ("allies", 1)], "nod", "boom", 0.0)])
        f.apply_game(b, "g1", "k", [f.Seat("any", "gdi", [("nod", 1), ("nod", 1), ("nod", 1), ("allies", 1)], "nod", "boom", 0.0)])
        self.assertEqual(a.personalities["any"]["k"].style, b.personalities["any"]["k"].style)

    def test_no_main_target_is_the_all_enemy_aggregate_not_a_pull_toward_global(self):
        # Opus 2026-11-27 (Workhorse F1): no main target (None or "" = a Random pick) = the aggregate of the enemy players alone; the old convention geo(aggregate, G) pulled toward global.
        t = self.preset(1000, 16000)
        axes = t.personalities["any"]["k"]
        axes.global_ = 500
        enemies = [("nod", 3), ("allies", 1)]
        for main in (None, ""):
            self.assertEqual(f._seat_enemy_eff(axes, enemies, main), 2000, "(1000^3 x 16000)^(1/4), global 500 not involved: " + repr(main))
            self.assertEqual(t.resolve("any", "k", "gdi", ["nod", "nod", "nod", "allies"], main, None), f.resolve(500, 500, 2000, 0), "release reader = trainer: " + repr(main))
        self.assertEqual(f._seat_enemy_eff(axes, [], None), 500, "no enemies and no main target = the global value")
        self.assertEqual(f._seat_enemy_eff(axes, [], "nod"), 1000, "no enemies, a main target = its value")
        self.assertEqual(f._seat_enemy_eff(axes, enemies, "unlearned"), f.team_enemy([1000, 1000, 1000, 16000], 500), "a real faction not learned yet reads as the global value")

    def test_the_seed_of_a_no_target_seat_equals_the_runtime_decision_value(self):
        for main in (None, ""):
            t = self.preset(1000, 4000)
            before = t.resolve("any", "k", "gdi", ["nod", "allies"], main, None)
            f.apply_game(t, "g1", "k", [f.Seat("any", "gdi", [("nod", 1), ("allies", 1)], main, "boom", 0.0)])
            after = t.resolve("any", "k", "gdi", ["nod", "allies"], main, "boom")
            self.assertLessEqual(abs(after - before), 1, "no jump for main=" + repr(main))
            self.assertEqual(t.personalities["any"]["k"].style["boom"], f.seed_style(1000, 1000, 2000))

    def test_the_resolve_of_the_seeding_game_matches_the_seed_the_trainer_used(self):
        # The point of the fix: the pre-step decision value (runtime Resolve with style unseen) equals the decision value right after a zero-signal seeding step.
        t = self.preset(1000, 16000)
        before = t.resolve("any", "k", "gdi", ["nod", "nod", "nod", "allies"], "nod", None)
        f.apply_game(t, "g1", "k", [f.Seat("any", "gdi", [("nod", 3), ("allies", 1)], "nod", "boom", 0.0)])
        after = t.resolve("any", "k", "gdi", ["nod", "nod", "nod", "allies"], "nod", "boom")
        self.assertLessEqual(abs(after - before), 1, "a fresh style must not make the decision jump")


class PersonalityKey(unittest.TestCase):
    """Workhorse F6: the snapshot key is personality__knob, so a personality containing '__' is refused (exit 5), never mis-parsed."""

    def test_a_personality_with_the_separator_is_refused(self):
        rec = {"game_id": "g1", "seq": 1, "seat": "s1", "personality": "ru__sh", "own_faction": "gdi", "main_target": "nod", "main_target_style": None,
               "enemies": [{"faction": "nod", "weight": 1}], "knobs": [{"knob": "k", "signal": 1.0}]}
        game, seq, error = f.check_records([rec])
        self.assertEqual(error[0], 5)
        rec["personality"] = "rush"
        self.assertIsNone(f.check_records([rec])[2])


class SnapshotSafety(unittest.TestCase):
    """Terra 66B4FF2E findings 1 and 4: an existing invalid snapshot is never reset; a closed game is never extended; the write is atomic."""

    def setUp(self):
        self.dir = tempfile.TemporaryDirectory()
        self.snap = os.path.join(self.dir.name, "learn_all.yaml")

    def tearDown(self):
        self.dir.cleanup()

    def record(self, game, seq, knob="wave_role.frontline", signal=1.0, seat="s1"):
        return {"game_id": game, "seq": seq, "seat": seat, "personality": "rush", "own_faction": "gdi", "main_target": "nod", "main_target_style": "boom",
                "enemies": [{"faction": "nod", "weight": 1}], "knobs": [{"knob": knob, "signal": signal}]}

    def exposure(self, game, seq, **kw):
        path = os.path.join(self.dir.name, "exp_%s_%s.jsonl" % (game, kw.get("knob", "k")))
        open(path, "w", encoding="utf-8").write(json.dumps(self.record(game, seq, **kw)) + "\n")
        return path

    def run_fit(self, path):
        return f.main(["--exposures", path, "--snapshot", self.snap])

    def write(self, text):
        open(self.snap, "w", encoding="utf-8", newline="\n").write(text)

    def read(self):
        return open(self.snap, "rb").read()

    def test_a_missing_snapshot_is_the_only_first_use(self):
        self.assertEqual(self.run_fit(self.exposure("g1", 1)), 0)
        self.assertEqual(f.parse_text_strict(self.read().decode("utf-8")).last_seq, 1)

    def test_an_existing_invalid_snapshot_is_refused_and_left_byte_identical(self):
        good = "BotLearnAll:\n\tVersion: 1\n\tLastSeq: 3\n\tGames:\n\t\tg0: 3\n\tApplied:\n\t\tg0|any|wave_role.frontline:\n\tKnob@any__wave_role.frontline:\n\t\tGlobal: 1041\n"
        f.parse_text_strict(good)  # the reference text itself is valid
        bad_texts = {
            "empty": "",
            "whitespace": "  \n",
            "wrong root": good.replace("BotLearnAll:", "SomethingElse:"),
            "other version": good.replace("Version: 1", "Version: 2"),
            "no version": good.replace("\tVersion: 1\n", ""),
            "bad version": good.replace("Version: 1", "Version: x"),
            "truncated value": good.replace("Global: 1041", "Global:"),
            "value outside the store": good.replace("Global: 1041", "Global: 99999"),
            "zero value": good.replace("Global: 1041", "Global: 0"),
            "knob without global": good.replace("\t\tGlobal: 1041\n", "\t\tOwn@gdi: 1100\n"),
            "unknown field": good + "\t\tBogus: 5\n",
            "malformed applied": good.replace("g0|any|wave_role.frontline:", "g0"),
            "unknown top entry": good + "\tMystery: 1\n",
            "negative lastseq": good.replace("LastSeq: 3", "LastSeq: -3"),
            "bad indentation": good.replace("\t\tGlobal", "\t\t\tGlobal"),
            # FIX-3 (Sol 02 finding 2): ambiguous persisted state is refused, never normalised
            "duplicate lastseq": good + "\tLastSeq: 3\n",
            "duplicate games block": good + "\tGames:\n\t\tg0: 3\n",
            "duplicate games entry": good.replace("\t\tg0: 3\n", "\t\tg0: 3\n\t\tg0: 3\n"),
            "negative game sequence": good.replace("g0: 3", "g0: -1").replace("LastSeq: 3", "LastSeq: 0"),
            "lastseq is not the largest game sequence": good.replace("LastSeq: 3", "LastSeq: 2"),
            "lastseq without games": good.replace("\tGames:\n\t\tg0: 3\n", ""),
            "applied game without a games entry": good.replace("g0|any", "g7|any"),
            "duplicate applied block": good + "\tApplied:\n\t\tg0|any|k2:\n",
            "duplicate applied entry": good.replace("\t\tg0|any|wave_role.frontline:\n", "\t\tg0|any|wave_role.frontline:\n\t\tg0|any|wave_role.frontline:\n"),
            "duplicate knob block": good + "\tKnob@any__wave_role.frontline:\n\t\tGlobal: 1200\n",
            "duplicate global": good.replace("\t\tGlobal: 1041\n", "\t\tGlobal: 1041\n\t\tGlobal: 1200\n"),
            "duplicate own axis": good + "\t\tOwn@gdi: 1100\n\t\tOwn@gdi: 1200\n",
            "duplicate style axis": good + "\t\tStyle@boom: 1100\n\t\tStyle@boom: 1200\n",
            "children under a scalar header": good.replace("\tLastSeq: 3\n", "\tLastSeq: 3\n\t\tx: 1\n"),
        }
        for name, text in bad_texts.items():
            self.write(text)
            before = self.read()
            self.assertEqual(self.run_fit(self.exposure("g9", 9)), 6, name)
            self.assertEqual(self.read(), before, name + ": the bytes must not change")
        log = open(os.path.join(self.dir.name, "learn_all_fit.log"), encoding="utf-8").read()
        self.assertEqual(log.count("REFUSED exit=6"), len(bad_texts), "every refusal is logged next to the exposure file")

    def test_the_prior_snapshot_is_kept_and_the_write_is_atomic(self):
        self.assertEqual(self.run_fit(self.exposure("g1", 1)), 0)
        first = self.read()
        self.assertEqual(self.run_fit(self.exposure("g2", 2)), 0)
        self.assertEqual(open(os.path.join(self.dir.name, "learn_all.before.g2.yaml"), "rb").read(), first, "the accepted snapshot before the step is preserved")
        self.assertFalse(os.path.exists(self.snap + ".tmp"))

    def test_an_interrupted_write_leaves_the_accepted_snapshot_untouched(self):
        self.assertEqual(self.run_fit(self.exposure("g1", 1)), 0)
        before = self.read()
        real = f.os.replace
        try:
            def boom(src, dst):
                raise OSError("simulated crash before the rename")
            f.os.replace = boom
            self.assertEqual(self.run_fit(self.exposure("g2", 2)), 6)
        finally:
            f.os.replace = real
        self.assertEqual(self.read(), before)
        self.assertEqual(f.parse_text_strict(before.decode("utf-8")).last_seq, 1)

    def test_a_closed_game_cannot_be_extended_with_a_new_knob(self):
        self.assertEqual(self.run_fit(self.exposure("g1", 1, knob="a")), 0)
        self.assertEqual(self.run_fit(self.exposure("g2", 2, knob="a")), 0)
        before = self.read()
        self.assertEqual(self.run_fit(self.exposure("g1", 1, knob="b")), 7, "old game, new knob, after a newer game")
        self.assertEqual(self.read(), before)

    def test_an_exact_replay_of_a_closed_game_is_a_no_op(self):
        self.assertEqual(self.run_fit(self.exposure("g1", 1, knob="a")), 0)
        self.assertEqual(self.run_fit(self.exposure("g2", 2, knob="a")), 0)
        before = self.read()
        self.assertEqual(self.run_fit(self.exposure("g1", 1, knob="a")), 0)
        self.assertEqual(self.read(), before)

    def test_an_enemy_weight_outside_one_to_sixty_four_is_refused(self):
        for weight in (0, -1, 65, 1.5, "2"):
            rec = self.record("g1", 1)
            rec["enemies"][0]["weight"] = weight
            path = os.path.join(self.dir.name, "w.jsonl")
            open(path, "w", encoding="utf-8").write(json.dumps(rec) + "\n")
            self.assertEqual(self.run_fit(path), 5, repr(weight))
        self.assertFalse(os.path.exists(self.snap), "nothing was written")


class Pipeline(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.TemporaryDirectory()
        self.snap = os.path.join(self.dir.name, "learn_all.yaml")
        open(self.snap, "w", encoding="utf-8", newline="\n").write("BotLearnAll:\n\tVersion: 1\n")

    def tearDown(self):
        self.dir.cleanup()

    def record(self, game, seq, seat="s1", signal=1.0, personality="rush"):
        return {"game_id": game, "seq": seq, "seat": seat, "personality": personality, "own_faction": "gdi", "main_target": "nod", "main_target_style": "boom",
                "enemies": [{"faction": "nod", "weight": 1}], "knobs": [{"knob": "wave_role.frontline", "signal": signal}]}

    def exposure(self, game, seq, signal=1.0, name=None, records=None):
        path = os.path.join(self.dir.name, "exp_%s.jsonl" % (name or game))
        records = records if records is not None else [self.record(game, seq, signal=signal)]
        open(path, "w", encoding="utf-8").write("".join(json.dumps(r) + "\n" for r in records))
        return path

    def run_fit(self, *paths):
        return f.main(["--exposures", *paths, "--snapshot", self.snap])

    def read(self):
        return open(self.snap, encoding="utf-8").read()

    def test_a_malformed_main_target_style_is_refused(self):
        bad = self.record("g1", 1)
        bad["main_target_style"] = "boom now"
        self.assertEqual(self.run_fit(self.exposure("g1", 1, records=[bad])), 5)
        self.assertEqual(f.parse_text(self.read()).personalities, {}, "nothing was applied")

    def test_one_step_per_game_in_order(self):
        self.assertEqual(self.run_fit(self.exposure("g1", 1)), 0)
        t = f.parse_text(self.read())
        self.assertEqual(t.last_seq, 1)
        self.assertEqual(t.games, {"g1": 1})
        self.assertEqual(t.personalities["rush"]["wave_role.frontline"].global_, 1041)
        self.assertEqual(t.personalities["any"]["wave_role.frontline"].global_, 1041)
        self.assertEqual(self.run_fit(self.exposure("g2", 2)), 0)
        t = f.parse_text(self.read())
        self.assertEqual(t.last_seq, 2)
        self.assertEqual(t.personalities["rush"]["wave_role.frontline"].global_, 1083)

    def test_all_seats_of_a_game_are_one_step(self):
        recs = [self.record("g1", 1, "s1", 1.0, "rush"), self.record("g1", 1, "s2", -1.0, "turtle"), self.record("g1", 1, "s3", 1.0, "rush")]
        self.assertEqual(self.run_fit(self.exposure("g1", 1, records=recs)), 0)
        t = f.parse_text(self.read())
        self.assertEqual(t.personalities["rush"]["wave_role.frontline"].global_, 1041, "rush seats agree")
        self.assertEqual(t.personalities["turtle"]["wave_role.frontline"].global_, 961)
        self.assertEqual(t.personalities["any"]["wave_role.frontline"].global_, 1013, "global = mean(+1, -1, +1) = 1/3: 1000 x exp(0.04/3) = 1013.4")

    def test_same_game_again_changes_nothing(self):
        p = self.exposure("g1", 1)
        self.run_fit(p)
        before = self.read()
        self.assertEqual(self.run_fit(p), 0)
        self.assertEqual(self.read(), before)

    def test_a_batch_of_games_is_refused(self):
        a, b = self.exposure("g1", 1), self.exposure("g2", 2)
        before = self.read()
        self.assertEqual(self.run_fit(a, b), 3)
        self.assertEqual(self.read(), before)

    def test_an_out_of_order_game_is_refused_and_last_seq_never_decreases(self):
        self.run_fit(self.exposure("g5", 5))
        before = self.read()
        self.assertEqual(self.run_fit(self.exposure("g3", 3)), 4)
        self.assertEqual(self.run_fit(self.exposure("g5b", 5)), 4, "an equal seq of another game is not above LastSeq either")
        self.assertEqual(self.read(), before)
        self.assertEqual(f.parse_text(self.read()).last_seq, 5)

    def test_a_missing_or_mixed_or_bad_seq_is_refused(self):
        before = self.read()
        no_seq = self.record("g1", 1)
        del no_seq["seq"]
        self.assertEqual(self.run_fit(self.exposure("g1", 1, name="a", records=[no_seq])), 5)
        self.assertEqual(self.run_fit(self.exposure("g1", 1, name="b", records=[self.record("g1", 1, "s1"), self.record("g1", 2, "s2")])), 5, "mixed seq")
        self.assertEqual(self.run_fit(self.exposure("g1", 1, name="c", records=[self.record("g1", "1", "s1")])), 5, "string seq")
        self.assertEqual(self.run_fit(self.exposure("g1", 1, name="d", records=[self.record("g1", True, "s1")])), 5, "bool seq")
        self.assertEqual(self.run_fit(self.exposure("g1", 1, name="e", records=[self.record("g1", -1, "s1")])), 5, "negative seq")
        self.assertEqual(self.run_fit(self.exposure("g1", 1, name="f", records=[self.record("g1", 1, "s1"), self.record("g1", 1, "s1")])), 5, "duplicate seat")
        self.assertEqual(self.run_fit(self.exposure("g1", 1, name="g", records=[self.record("g 1", 1)])), 5, "bad game id")
        self.assertEqual(self.read(), before)

    def test_a_game_cannot_be_reapplied_under_another_seq(self):
        self.run_fit(self.exposure("g1", 1))
        before = self.read()
        self.assertEqual(self.run_fit(self.exposure("g1", 7, name="again")), 5)
        self.assertEqual(self.read(), before)

    def test_no_evidence_keeps_the_snapshot(self):
        self.run_fit(self.exposure("g1", 1))
        before = self.read()
        self.assertEqual(self.run_fit(self.exposure("g2", 2, signal=None)), 0)
        self.assertEqual(self.read(), before)

    def test_dry_run_writes_nothing(self):
        before = self.read()
        self.assertEqual(f.main(["--exposures", self.exposure("g1", 1), "--snapshot", self.snap, "--dry-run"]), 0)
        self.assertEqual(self.read(), before)


if __name__ == "__main__":
    unittest.main()
