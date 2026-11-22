"""run_ai_match_batch --no-record evidence (Opus RULING MR17 A): logging-off builds write no match record, so a
match passes on tick_time.csv reaching NO_RECORD_MIN_TICKS, zero new exception logs, and an intact replay trailer."""
import pathlib
import struct
import sys
import tempfile
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "ai"))
import run_ai_match_batch as rb  # noqa: E402


def trailer(payload: bytes = b"payload") -> bytes:
    """OpenRA .orarep tail as ReplayMetadata.Write leaves it: -1, version 1, data, data length, -2."""
    return b"HEADER" + struct.pack("<ii", -1, 1) + payload + struct.pack("<i", len(payload)) + struct.pack("<i", -2)


class NoRecordEvidenceTest(unittest.TestCase):
    def setUp(self):
        self.support = pathlib.Path(tempfile.mkdtemp())
        self.logs = self.support / "Logs"
        self.logs.mkdir()

    def write_ticks(self, last: int, name: str = "ai-duel-batch-tick_time.csv") -> None:
        (self.logs / name).write_text(f"1,0.5\n2,0.4\n{last},0.2\n", encoding="utf-8")

    def write_replay(self, data: bytes) -> None:
        d = self.support / "Replays" / "ra" / "1.0"
        d.mkdir(parents=True, exist_ok=True)
        (d / "game.orarep").write_bytes(data)

    def test_replay_trailer(self):
        p = self.support / "a.orarep"
        p.write_bytes(trailer())
        self.assertTrue(rb.replay_intact(p))
        p.write_bytes(trailer(b""))
        self.assertTrue(rb.replay_intact(p))
        p.write_bytes(trailer()[:-1])
        self.assertFalse(rb.replay_intact(p))
        bad_version = b"HEADER" + struct.pack("<ii", -1, 2) + b"x" + struct.pack("<i", 1) + struct.pack("<i", -2)
        p.write_bytes(bad_version)
        self.assertFalse(rb.replay_intact(p))
        p.write_bytes(b"short")
        self.assertFalse(rb.replay_intact(p))

    def test_tick_threshold(self):
        self.write_ticks(2999)
        self.assertEqual(rb.tick_time_last_tick(self.logs), 2999)
        self.write_replay(trailer())
        self.assertEqual(rb.no_record_verdict(self.support, self.logs, [])[0], "norecord")
        self.write_ticks(3000)
        status, evidence = rb.no_record_verdict(self.support, self.logs, [])
        self.assertEqual(status, "ok")
        self.assertEqual(evidence, {"tick_time_last_tick": 3000, "exception_logs": 0,
                                    "replays": 1, "replays_intact": 1})

    def test_exception_log_fails(self):
        self.write_ticks(5000)
        self.write_replay(trailer())
        status, evidence = rb.no_record_verdict(self.support, self.logs, ["exception-1.log"])
        self.assertEqual(status, "norecord")
        self.assertEqual(evidence["exception_logs"], 1)

    def test_missing_replay_or_truncated_fails(self):
        self.write_ticks(5000)
        self.assertEqual(rb.no_record_verdict(self.support, self.logs, [])[0], "norecord")
        self.write_replay(trailer()[:-3])
        self.assertEqual(rb.no_record_verdict(self.support, self.logs, [])[0], "norecord")

    def test_csv_missing_or_ambiguous_fails(self):
        self.write_replay(trailer())
        self.assertEqual(rb.no_record_verdict(self.support, self.logs, [])[0], "norecord")
        self.write_ticks(5000, "ai-duel-batch-tick_time.csv")
        self.write_ticks(5000, "other-tick_time.csv")
        self.assertEqual(rb.tick_time_last_tick(self.logs), 0)
        self.assertEqual(rb.no_record_verdict(self.support, self.logs, [])[0], "norecord")


if __name__ == "__main__":
    unittest.main()
