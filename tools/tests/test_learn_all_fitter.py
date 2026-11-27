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
    seats = [f.Seat(s["personality"], s["own"], [(e["faction"], e["style"], e["weight"]) for e in s["enemies"]], s["main_target"], s["signal"])
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
        a = f.Seat("rush", "gdi", [("nod", None, 1)], "nod", 1.0)
        b = f.Seat("rush", "allies", [("nod", None, 1)], "nod", -1.0)
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
        main_nod = f.Seat("any", "gdi", [("nod", "boom", 1), ("allies", "boom", 1)], "nod", 0.0)
        f.apply_game(t, "g1", "k", [main_nod])
        # E_eff = geo(geo(1000, 4000) = 2000, main nod = 1000) = 1414; seed = geo(1000, 1000, 1414) = 1122
        self.assertEqual(t.personalities["any"]["k"].style["boom"], f.geo_mean([1000, 1000, 1414]))
        t2 = f.Table()
        axes2 = t2.personalities.setdefault("any", {}).setdefault("k", f.Axes())
        axes2.enemy["nod"] = 1000
        axes2.enemy["allies"] = 4000
        f.apply_game(t2, "g1", "k", [f.Seat("any", "gdi", [("nod", "boom", 1), ("allies", "boom", 1)], "allies", 0.0)])
        self.assertEqual(t2.personalities["any"]["k"].style["boom"], f.geo_mean([1000, 1000, 2828]), "main target allies: the seed moves with it")


class Pipeline(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.TemporaryDirectory()
        self.snap = os.path.join(self.dir.name, "learn_all.yaml")
        open(self.snap, "w", encoding="utf-8", newline="\n").write("BotLearnAll:\n\tVersion: 1\n")

    def tearDown(self):
        self.dir.cleanup()

    def record(self, game, seq, seat="s1", signal=1.0, personality="rush"):
        return {"game_id": game, "seq": seq, "seat": seat, "personality": personality, "own_faction": "gdi", "main_target": "nod",
                "enemies": [{"faction": "nod", "style": "boom", "weight": 1}], "knobs": [{"knob": "wave_role.frontline", "signal": signal}]}

    def exposure(self, game, seq, signal=1.0, name=None, records=None):
        path = os.path.join(self.dir.name, "exp_%s.jsonl" % (name or game))
        records = records if records is not None else [self.record(game, seq, signal=signal)]
        open(path, "w", encoding="utf-8").write("".join(json.dumps(r) + "\n" for r in records))
        return path

    def run_fit(self, *paths):
        return f.main(["--exposures", *paths, "--snapshot", self.snap])

    def read(self):
        return open(self.snap, encoding="utf-8").read()

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
