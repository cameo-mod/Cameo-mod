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

    def test_games(self):
        for g in GOLDEN["games"]:
            t, changed = f.Table(), []
            for a in g["applications"]:
                en = [(e["faction"], e["style"], e["weight"]) for e in a["enemies"]]
                changed.append(t.apply_game(a["game_id"], a["personality"] or None, a["knob"], a["own"], en, a["signal"]))
            self.assertEqual(changed, g["changed"], g["name"])
            self.assertEqual(f.write_text(t), g["expect_text"], g["name"])

    def test_text_round_trip(self):
        for g in GOLDEN["games"]:
            t, _seq = f.parse_text(g["expect_text"])
            self.assertEqual(f.write_text(t), g["expect_text"], g["name"])

    def test_damaged_text_is_empty(self):
        for text in (None, "", "Other:\n\tVersion: 1\n", "BotLearnAll:\n\tVersion: 2\n\tKnob@any__k:\n\t\tGlobal: 1500\n", "BotLearnAll:\n\tKnob@any__k:\n\t\tGlobal: 1500\n"):
            t, _ = f.parse_text(text)
            self.assertEqual(t.personalities, {}, text)


class Pipeline(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.TemporaryDirectory()
        self.snap = os.path.join(self.dir.name, "learn_all.yaml")
        open(self.snap, "w", encoding="utf-8", newline="\n").write("BotLearnAll:\n\tVersion: 1\n")

    def tearDown(self):
        self.dir.cleanup()

    def exposure(self, game, seq, signal=1.0):
        path = os.path.join(self.dir.name, "exp_%s.jsonl" % game)
        rec = {"game_id": game, "seq": seq, "personality": "rush", "own_faction": "gdi",
               "enemies": [{"faction": "nod", "style": "boom", "weight": 1}], "knobs": [{"knob": "wave_role.frontline", "signal": signal}]}
        open(path, "w", encoding="utf-8").write(json.dumps(rec) + "\n")
        return path

    def run_fit(self, *paths):
        return f.main(["--exposures", *paths, "--snapshot", self.snap])

    def read(self):
        return open(self.snap, encoding="utf-8").read()

    def test_one_step_per_game_in_order(self):
        self.assertEqual(self.run_fit(self.exposure("g1", 1)), 0)
        t, seq = f.parse_text(self.read())
        self.assertEqual(seq, 1)
        self.assertEqual(t.personalities["rush"]["wave_role.frontline"].global_, 1041)
        self.assertEqual(t.personalities["any"]["wave_role.frontline"].global_, 1041)
        self.assertEqual(self.run_fit(self.exposure("g2", 2)), 0)
        t, seq = f.parse_text(self.read())
        self.assertEqual(seq, 2)
        self.assertEqual(t.personalities["rush"]["wave_role.frontline"].global_, 1083)

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

    def test_an_out_of_order_game_is_refused(self):
        self.run_fit(self.exposure("g5", 5))
        before = self.read()
        self.assertEqual(self.run_fit(self.exposure("g3", 3)), 4)
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
