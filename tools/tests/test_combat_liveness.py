import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).resolve().parents[1] / "ai" / "combat_liveness.py"
spec = importlib.util.spec_from_file_location("combat_liveness", SCRIPT)
combat = importlib.util.module_from_spec(spec)
spec.loader.exec_module(combat)


def fixture():
    common = dict(schema="cameo-combat-liveness", schema_version=1,
                  game_uid="game", player="stable-player", map_uid="map",
                  faction="allies", profile="hard", dropped=0,
                  roster_complete=True, eligibility_complete=True,
                  dispatch_complete=True)
    return [dict(common, kind=kind, tick=tick, seq=seq, **extra)
            for seq, (kind, tick, extra) in enumerate([
                ("start", 0, {}), ("eligibility_transition", 10, {}),
                ("dispatch_intent", 20, {}), ("dispatch_observed", 30, {}),
                ("restraint_transition", 40, {}), ("pulse", 50, {}),
                ("end", 100, {"complete": True})])]


class CombatTransportTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.path = Path(self.temp.name) / "cameo-ai-combat-liveness.jsonl"

    def write(self, rows):
        self.path.write_bytes(b"".join(json.dumps(row).encode() + b"\n" for row in rows))

    def analyze(self, rows):
        self.write(rows)
        return combat.analyze(self.path, "game", "stable-player")

    def test_complete_transport_never_certifies_health_or_dispatch(self):
        result = self.analyze(fixture())
        self.assertEqual("TRANSPORT_COMPLETE", result["coverage"])
        self.assertEqual("UNKNOWN", result["status"])
        self.assertEqual("UNKNOWN", result["dispatch_causality"])
        self.assertTrue(result["diagnostic_only"])
        self.assertEqual([], result["findings"])
        self.assertEqual(hashlib.sha256(self.path.read_bytes()).hexdigest(), result["input"]["consumed_sha256"])

    def test_actual_canonical_cli_and_evidence_immutability(self):
        self.write(fixture())
        before = self.path.read_bytes()
        proc = subprocess.run([sys.executable, str(SCRIPT), "--logs", self.temp.name,
                               "--game-uid", "game", "--player", "stable-player"],
                              capture_output=True, text=True, check=False)
        self.assertEqual(21, proc.returncode)
        result = json.loads(proc.stdout)
        self.assertEqual(str(self.path), result["input"]["path"])
        self.assertEqual(before, self.path.read_bytes())
        self.assertEqual("UNKNOWN", result["status"])

    def test_missing_canonical_path_cli_unknown(self):
        proc = subprocess.run([sys.executable, str(SCRIPT), "--logs", self.temp.name,
                               "--game-uid", "game", "--player", "stable-player"],
                              capture_output=True, text=True, check=False)
        self.assertEqual(21, proc.returncode)
        self.assertEqual("UNKNOWN", json.loads(proc.stdout)["coverage"])

    def test_lifecycle_failures_unknown(self):
        mutations = [lambda r: r[0].update(tick=1),
                     lambda r: r[1].update(seq=2),
                     lambda r: r[2].update(tick=5),
                     lambda r: r[5].update(tick=51),
                     lambda r: r.pop(),
                     lambda r: r.append(dict(r[-1], seq=7)),
                     lambda r: r[1].update(kind="start"),
                     lambda r: r[1].update(kind="unknown"),
                     lambda r: r[1].update(profile="easy")]
        for mutate in mutations:
            with self.subTest(mutation=mutate):
                rows = fixture()
                mutate(rows)
                result = self.analyze(rows)
                self.assertEqual("UNKNOWN", result["coverage"])
                self.assertTrue(result["unknown"])

    def test_incomplete_channels_sticky_despite_complete_end(self):
        for key in combat.CHANNELS:
            with self.subTest(channel=key):
                rows = fixture()
                rows[2][key] = False
                result = self.analyze(rows)
                self.assertEqual("UNKNOWN", result["coverage"])
                self.assertIn(key, result["unknown"])

    def test_drop_and_terminal_incomplete(self):
        for mutate in (lambda r: r[1].update(dropped=1),
                       lambda r: r[-1].update(complete=False)):
            rows = fixture()
            mutate(rows)
            self.assertEqual("UNKNOWN", self.analyze(rows)["coverage"])

    def test_strict_boolean_integer_and_identity_contract(self):
        for key, value in [("seq", False), ("tick", True), ("dropped", True),
                           ("dropped", -1), ("schema_version", True),
                           ("roster_complete", 1), ("player", ""),
                           ("map_uid", None), ("profile", "x" * 1025)]:
            with self.subTest(key=key, value=value):
                rows = fixture()
                rows[0][key] = value
                self.assertEqual("UNKNOWN", self.analyze(rows)["coverage"])

    def test_missing_participant_and_other_player_interleaving(self):
        rows = fixture()
        other = [dict(row, player="other") for row in rows]
        self.assertEqual("TRANSPORT_COMPLETE", self.analyze([r for pair in zip(rows, other) for r in pair])["coverage"])
        self.assertEqual("UNKNOWN", self.analyze(other)["coverage"])

    def test_duplicate_keys_nonfinite_malformed_empty_and_no_newline(self):
        for data in [b"", b"{}", b"{bad}\n", b'{"a":1,"a":2}\n', b'{"a":NaN}\n']:
            with self.subTest(data=data):
                self.path.write_bytes(data)
                self.assertEqual("UNKNOWN", combat.analyze(self.path, "game", "stable-player")["coverage"])

    def test_resource_bounds(self):
        self.write(fixture())
        for key, maximum in [("MAX_BYTES", 1), ("MAX_LINE", 1), ("MAX_ROWS", 1)]:
            with self.subTest(bound=key), patch.object(combat, key, maximum):
                self.assertEqual("UNKNOWN", combat.analyze(self.path, "game", "stable-player")["coverage"])

    def test_non_utf8_and_corrupt_foreign_row_unknown(self):
        self.path.write_bytes(b"\xff\n")
        self.assertEqual("UNKNOWN", combat.analyze(self.path, "game", "stable-player")["coverage"])
        rows = fixture()
        rows.insert(1, dict(rows[0], player="other", schema_version=2))
        self.assertEqual("UNKNOWN", self.analyze(rows)["coverage"])


if __name__ == "__main__":
    unittest.main()
