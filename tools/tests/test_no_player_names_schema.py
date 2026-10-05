import json
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "ai"))

from audit_no_player_names import match_ok, validate_learned, validate_logs
from fit_opponent_signatures import dump_yaml, fit


def match_record():
    return {
        "schema": 3, "record_id": "game|seat_1", "player": {"seat": "seat_1"},
        "seats": [{"seat": "seat_1", "faction": "td_gdi", "home": "1,2"}],
        "opponents": [{"seat": "seat_2", "faction": "td_nod", "home": "3,4"}],
        "allies": [], "opponent_signatures": [{"seat": "seat_2", "seen": {"faction": "td_nod", "army_value": 16},
                                                   "truth": {"faction": "td_nod", "outcome": "lost"}}],
    }


class AnonymousLogSchemaTest(unittest.TestCase):
    def test_accepts_anonymous_match_and_signature_grammar(self):
        self.assertTrue(match_ok(match_record()))

    def test_rejects_display_identity_in_relationship_descriptor(self):
        row = match_record()
        row["opponents"][0]["display"] = "captured display text"
        self.assertFalse(match_ok(row))

    def test_rejects_non_anonymous_record_id(self):
        row = match_record()
        row["record_id"] = "game|Multi0"
        self.assertFalse(match_ok(row))

    def test_fitter_clusters_seen_numeric_signatures(self):
        with tempfile.TemporaryDirectory() as td:
            source = Path(td) / "matches.jsonl"
            source.write_text(json.dumps(match_record()) + "\n", encoding="utf-8")
            result = fit([source])
        self.assertEqual(len(result["clusters"]), 1)
        self.assertEqual(result["clusters"][0]["count"], 1)
        self.assertEqual(result["clusters"][0]["faction"], "td_nod")

    def test_fitter_output_conforms_to_learned_file_grammar(self):
        with tempfile.TemporaryDirectory() as td:
            output = Path(td) / "opponent_signatures.yaml"
            source = Path(td) / "matches.jsonl"
            source.write_text(json.dumps(match_record()) + "\n", encoding="utf-8")
            output.write_text(dump_yaml(fit([source])), encoding="utf-8")
            self.assertEqual(validate_learned([output]), [])

    def test_log_reader_rejects_unrecognized_record_grammar(self):
        with tempfile.TemporaryDirectory() as td:
            source = Path(td) / "bad.jsonl"
            row = match_record()
            row["opponents"][0]["display"] = "captured display text"
            source.write_text(json.dumps(row) + "\n", encoding="utf-8")
            self.assertTrue(validate_logs([source]))


if __name__ == "__main__":
    unittest.main()
