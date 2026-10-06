import json
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))
sys.path.insert(0, str(ROOT / "tools" / "audit"))

from anonymize_legacy_logs import LOG_NAMES, migrate
from audit_no_player_names import validate_logs


def fixtures():
    return {
        LOG_NAMES[0]: [{"schema": 2, "game_uid": "game-x", "record_id": "game-x|Alpha",
                        "player": {"name": "Alpha", "faction": "td_gdi", "bot_type": "hard", "home": "2,3"},
                        "opponents": [{"name": "Bravo", "is_bot": False, "faction": "td_nod", "outcome": "lost"}],
                        "allies": [], "takeover": {"controller_client": 19}, "stats": {}}],
        LOG_NAMES[1]: [{"schema": 2, "kind": "situation", "game_uid": "game-x", "record_id": "game-x|Alpha|20",
                        "player": "Alpha", "tick": 20, "main_target": "Bravo",
                        "enemies": [{"name": "Bravo", "faction": "td_nod", "army_value": 64}]}],
        LOG_NAMES[2]: [{"schema": 1, "kind": "placement", "game_uid": "game-x", "record_id": "game-x|Alpha|20|unit",
                        "player": "Alpha", "tick": 20, "actor": "td_gdi_minigunner", "cell": "5,6", "faction": "td_gdi"}],
        LOG_NAMES[3]: [{"schema": "mission-card/1", "game_uid": "game-x", "player": "Alpha",
                        "mission_id": "raid:Bravo:region_4", "attempt": 1, "attempt_id": "raid:Bravo:region_4|A1",
                        "record_kind": "attempt", "tick": 21}],
        LOG_NAMES[4]: [{"schema": "engagement/1", "game_uid": "game-x", "record_id": "game-x|Alpha|p0",
                        "player": "Alpha", "tick": 21, "engagement_id": "e7", "context": {"dist_own_base": 4}}],
    }


def walk(value):
    if isinstance(value, dict):
        for key, item in value.items():
            yield key
            yield from walk(item)
    elif isinstance(value, list):
        for item in value:
            yield from walk(item)
    elif isinstance(value, str):
        yield value


class AnonymizeLegacyLogsTest(unittest.TestCase):
    def test_rewrites_five_kinds_and_backup_precedes_anonymized_outputs(self):
        with tempfile.TemporaryDirectory() as td:
            support = Path(td)
            for name, rows in fixtures().items():
                (support / name).write_text("".join(json.dumps(r) + "\n" for r in rows), encoding="utf-8")
            result = migrate(support)
            self.assertEqual(result["files_rewritten"], 5)
            self.assertEqual(result["records_rewritten"], 5)
            self.assertTrue(Path(result["backup"], LOG_NAMES[0]).is_file())
            self.assertIn("Alpha", Path(result["backup"], LOG_NAMES[0]).read_text(encoding="utf-8"))
            outputs = []
            for name in LOG_NAMES:
                outputs.extend(json.loads(line) for line in (support / name).read_text(encoding="utf-8").splitlines())
            output_text = "\n".join(map(str, (list(walk(row)) for row in outputs)))
            self.assertNotIn("Alpha", output_text)
            self.assertNotIn("Bravo", output_text)
            self.assertFalse(any(k.lower() in {"name", "client_id", "controller_client", "is_bot"}
                                 for row in outputs for k in walk(row) if isinstance(k, str)))
            self.assertEqual(validate_logs([support / name for name in LOG_NAMES]), [])

    def test_second_run_is_idempotent_and_makes_no_new_backup(self):
        with tempfile.TemporaryDirectory() as td:
            support = Path(td)
            for name, rows in fixtures().items():
                (support / name).write_text("".join(json.dumps(r) + "\n" for r in rows), encoding="utf-8")
            first = migrate(support)
            before = {name: (support / name).read_bytes() for name in LOG_NAMES}
            second = migrate(support)
            self.assertEqual(first["files_rewritten"], 5)
            self.assertEqual(second["files_rewritten"], 0)
            self.assertEqual(second["records_rewritten"], 0)
            self.assertEqual(before, {name: (support / name).read_bytes() for name in LOG_NAMES})
            self.assertEqual(len(list(support.glob("cameo-ai-legacy-backup-*"))), 1)


if __name__ == "__main__":
    unittest.main()
