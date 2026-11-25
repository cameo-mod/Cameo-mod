import pathlib
import sys
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "ai"))
import economy_invariants as verifier
from replay_health import EvidenceError


def raw(seq, player="p0", uid="g1", schema=2):
    return {"schema": schema, "seq": seq, "tick": seq * 50, "kind": "spend_ledger", "game_uid": uid, "player": player}


class RawSequenceCheckTest(unittest.TestCase):
    def test_contiguous_rows_pass_and_count(self):
        self.assertEqual(verifier.raw_sequence_check([raw(0), raw(1), raw(2)], "g1", "p0"), 3)

    def test_gap_fails_closed(self):
        with self.assertRaises(EvidenceError):
            verifier.raw_sequence_check([raw(0), raw(2)], "g1", "p0")

    def test_duplicate_fails_closed(self):
        with self.assertRaises(EvidenceError):
            verifier.raw_sequence_check([raw(0), raw(0)], "g1", "p0")

    def test_wrong_schema_fails_closed(self):
        with self.assertRaises(EvidenceError):
            verifier.raw_sequence_check([raw(0, schema=1)], "g1", "p0")

    def test_other_players_rows_are_ignored(self):
        rows = [raw(0, player="p1"), raw(0), raw(5, player="p1"), raw(1)]
        self.assertEqual(verifier.raw_sequence_check(rows, "g1", "p0"), 2)

    def test_other_games_rows_are_ignored(self):
        rows = [raw(0, uid="g2"), raw(0)]
        self.assertEqual(verifier.raw_sequence_check(rows, "g1", "p0"), 1)

    def test_zero_rows_for_the_player_fail_closed(self):
        with self.assertRaises(EvidenceError):
            verifier.raw_sequence_check([raw(0, player="p1")], "g1", "p0")
        with self.assertRaises(EvidenceError):
            verifier.raw_sequence_check([], "g1", "p0")


if __name__ == "__main__":
    unittest.main()
