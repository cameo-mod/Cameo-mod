"""mission_story.py (MC2): mission-card grouping, terminal detection, dangling attempts.

The archive is one line per transition (schema mission-card/1, AiMissionLogWriter).
These tests pin the read contract so a writer-format change can't silently corrupt
the story: grouping is (game_uid, mission_id, attempt), order is tick, terminal comes
from the `terminal` flag with the state-name set as fallback for older records.
"""
import contextlib
import io
import json
import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/ai"))

import mission_story


def card(uid, mid, attempt, state, tick, by="Squads", terminal=None, reason=None, type_="raid", bot="hard"):
    r = {
        "schema": "mission-card/1", "game_uid": uid, "map_uid": "m", "map_title": "A Nuclear Winter",
        "player": "Multi0", "faction": "td_gdi", "bot": bot,
        "mission_id": mid, "attempt_id": f"{mid}|A{attempt}", "attempt": attempt,
        "state": state, "by": by, "tick": tick, "type": type_,
    }
    if terminal is not None:
        r["terminal"] = terminal
    if reason:
        r["reason"] = reason
    return r


def run(records, args=()):
    with tempfile.TemporaryDirectory() as td:
        p = pathlib.Path(td) / "batch" / "Logs"
        p.mkdir(parents=True)
        with open(p / "cameo-ai-missions.jsonl", "w", encoding="utf-8") as f:
            for r in records:
                f.write(json.dumps(r) + "\n")
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            rc = mission_story.main([td, *args])
    return rc, out.getvalue()


class MissionStoryTests(unittest.TestCase):
    def test_attempts_group_under_mission_and_order_by_tick(self):
        recs = [
            card("g1", "raid:t:r3", 2, "COMMITTED", 500),
            card("g1", "raid:t:r3", 1, "COMMITTED", 100),
            card("g1", "raid:t:r3", 1, "FAILED", 300, terminal=True, reason="lost_units"),
            card("g1", "raid:t:r3", 2, "SUCCESS", 700, terminal=True, reason="done"),
        ]
        rc, out = run(recs)
        self.assertEqual(rc, 0)
        # A1 prints before A2 regardless of file order; attempt 2 is the retry.
        self.assertLess(out.index("A1 Squads"), out.index("A2 Squads"))
        self.assertIn("1 match(es), 1 mission(s)", out)

    def test_terminal_flag_wins_over_state_name(self):
        # A record with terminal:false on a terminal-named state must read as open —
        # the writer's flag is authoritative, not our guess.
        recs = [card("g1", "m:x", 1, "FAILED", 10, terminal=False)]
        rc, out = run(recs)
        self.assertIn("*open*", out)
        self.assertIn("no terminal line", out)

    def test_state_name_fallback_without_terminal_field(self):
        recs = [card("g1", "m:x", 1, "SUCCESS", 10)]  # no terminal field
        rc, out = run(recs)
        self.assertIn("no dangling attempts", out)

    def test_dangling_reports_executor_and_last_state(self):
        recs = [
            card("g1", "raid:t:r9", 1, "COMMITTED", 100, by="Squads"),
            card("g1", "raid:t:r9", 1, "STALLED", 250, by="Squads", reason="stuck"),
        ]
        rc, out = run(recs)
        self.assertIn("raid:t:r9 A1: last STALLED@250 stuck by Squads", out)

    def test_per_type_success_rate(self):
        recs = [
            card("g1", "capture:a:1", 1, "COMMITTED", 10, type_="capture"),
            card("g1", "capture:a:1", 1, "SUCCESS", 50, terminal=True, type_="capture"),
            card("g1", "capture:a:2", 1, "COMMITTED", 60, type_="capture"),
            card("g1", "capture:a:2", 1, "FAILED", 90, terminal=True, reason="lost_units", type_="capture"),
        ]
        rc, out = run(recs)
        self.assertIn("capture", out)
        self.assertIn("success 1 (50%)", out)
        self.assertIn("failed 1", out)

    def test_match_and_mission_filters(self):
        recs = [
            card("g1", "m:a", 1, "SUCCESS", 10, terminal=True),
            card("g2", "m:b", 1, "FAILED", 10, terminal=True),
        ]
        _, out = run(recs, args=["--match", "g2"])
        self.assertIn("1 match(es), 1 mission(s)", out)
        self.assertIn("m:b", out)
        _, out = run(recs, args=["--mission", "m:a"])
        self.assertIn("m:a", out)
        self.assertNotIn("m:b", out)

    def test_dormant_is_not_an_attempt(self):
        # Fransotto's dormant shelf = a mission with no live attempt. The story
        # must show the attempt chain, not invent a "dormant" attempt.
        recs = [
            card("g1", "raid:t:r3", 1, "DENIED", 100, terminal=True, reason="no_units"),
        ]
        rc, out = run(recs)
        self.assertIn("A1 Squads: DENIED@100 no_units", out)
        self.assertIn("denied 1", out)


if __name__ == "__main__":
    unittest.main()
