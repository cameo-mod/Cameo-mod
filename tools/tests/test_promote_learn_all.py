"""LEARN-ALL-02 P1c-2: snapshot promotion gates (tools/ai/promote_learn_all.py)."""
import json
import os
import sys
import tempfile
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "ai"))
import fit_learn_all as f  # noqa: E402
import promote_learn_all as p  # noqa: E402


class Promotion(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.TemporaryDirectory()
        self.addCleanup(self.dir.cleanup)
        self.work = os.path.join(self.dir.name, "work")
        os.makedirs(self.work)
        self.accepted = os.path.join(self.dir.name, "accepted.yaml")

    def fit(self, game, seq, snapshot, knob="wave_role.frontline"):
        rec = {"game_id": game, "seq": seq, "seat": "seat_0", "personality": "rush", "own_faction": "gdi", "main_target": "nod", "main_target_style": None,
               "enemies": [{"faction": "nod", "weight": 1}], "knobs": [{"knob": knob, "signal": 1.0}]}
        exp = os.path.join(self.work, "exp_%s.jsonl" % game)
        open(exp, "w", encoding="utf-8").write(json.dumps(rec) + "\n")
        self.assertEqual(f.main(["--exposures", exp, "--snapshot", snapshot]), 0)

    def promote(self, candidate, *extra):
        return p.main(["--candidate", candidate, "--accepted", self.accepted] + list(extra))

    def read(self, path):
        return open(path, "rb").read()

    def test_first_use_promotes_and_a_later_candidate_replaces_it_with_a_backup(self):
        cand = os.path.join(self.work, "learn_all.yaml")
        self.fit("g1", 1, cand)
        self.assertEqual(self.promote(cand), 0)
        self.assertEqual(self.read(self.accepted), self.read(cand))
        first = self.read(self.accepted)
        self.fit("g2", 2, cand)
        self.assertEqual(self.promote(cand), 0)
        self.assertEqual(self.read(self.accepted), self.read(cand))
        self.assertEqual(self.read(os.path.join(self.work, "learn_all.accepted.before.1.yaml")), first, "the prior accepted snapshot is kept")
        self.assertTrue(os.path.exists(os.path.join(self.work, "learn_all_promote.log")))

    def test_a_candidate_that_loses_history_never_replaces_the_accepted_snapshot(self):
        newer = os.path.join(self.work, "newer.yaml")
        self.fit("g1", 1, newer)
        self.fit("g2", 2, newer)
        self.assertEqual(self.promote(newer), 0)
        accepted = self.read(self.accepted)
        older = os.path.join(self.work, "older.yaml")
        self.fit("g1", 1, older)
        self.assertEqual(self.promote(older), 3, "LastSeq goes down")
        other = os.path.join(self.work, "other.yaml")
        self.fit("h1", 5, other)
        self.assertEqual(self.promote(other), 3, "a different history: the accepted games are missing")
        self.assertEqual(self.read(self.accepted), accepted)

    def test_nothing_new_empty_and_invalid_candidates_are_refused(self):
        cand = os.path.join(self.work, "learn_all.yaml")
        self.fit("g1", 1, cand)
        self.assertEqual(self.promote(cand), 0)
        accepted = self.read(self.accepted)
        self.assertEqual(self.promote(cand), 4, "identical")
        empty = os.path.join(self.work, "empty.yaml")
        open(empty, "w", encoding="utf-8").write("BotLearnAll:\n\tVersion: 1\n")
        self.assertEqual(self.promote(empty), 4, "an empty snapshot never replaces a learned one")
        bad = os.path.join(self.work, "bad.yaml")
        open(bad, "w", encoding="utf-8").write("not a snapshot")
        self.assertEqual(self.promote(bad), 2)
        self.assertEqual(self.promote(os.path.join(self.work, "missing.yaml")), 2)
        self.assertEqual(self.read(self.accepted), accepted)

    def test_an_invalid_accepted_file_is_left_untouched_and_dry_run_writes_nothing(self):
        cand = os.path.join(self.work, "learn_all.yaml")
        self.fit("g1", 1, cand)
        open(self.accepted, "w", encoding="utf-8").write("garbage")
        self.assertEqual(self.promote(cand), 2)
        self.assertEqual(self.read(self.accepted), b"garbage")
        os.remove(self.accepted)
        self.assertEqual(self.promote(cand, "--dry-run"), 0)
        self.assertFalse(os.path.exists(self.accepted))


if __name__ == "__main__":
    unittest.main()
