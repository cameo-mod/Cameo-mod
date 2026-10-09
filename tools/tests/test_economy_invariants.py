import importlib.util
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "ai"))
import economy_invariants as verifier


def capture(duration=1500, state="producing", cash=5000, resources=0, capacity=10000):
    rows = []
    for tick in range(0, duration+1, 50):
        rows.append({"schema": 1, "game_uid": "g", "player": "p", "map_uid": "m",
                     "faction": "ra1_allies", "profile": "conventional", "profile_supported": True,
                     "dropped": 0, "kind": "pulse", "tick": tick, "seq": len(rows),
                     "queues_complete": True, "player_active": True,
                     "cash": cash, "resources": resources, "capacity": capacity, "spent": tick * 10,
                     "queues": [{"queue_id": "1:building", "producer_live": True,
                                 "state": state, "state_since_tick": 0, "reason": "no_site",
                                 "item": "refinery", "item_id": "1:0"}]})
    rows.append({**rows[-1], "kind": "end", "complete": True, "seq": len(rows)})
    return rows


def codes(rows):
    return {f["code"] for f in verifier.analyze(rows, "g", "p")["findings"]}


class InvariantTests(unittest.TestCase):
    def test_ready_exact_250_boundary(self):
        self.assertNotIn("READY_BUILDING_UNPLACED", codes(capture(200, "ready")))
        self.assertIn("READY_BUILDING_UNPLACED", codes(capture(250, "ready")))

    def test_idle_exact_1500_boundary(self):
        self.assertNotIn("BUILDING_QUEUE_IDLE", codes(capture(1450, "idle")))
        self.assertIn("BUILDING_QUEUE_IDLE", codes(capture(1500, "idle")))

    def test_dead_producer_not_postmortem_idle_defect(self):
        rows = capture(1500, "idle")
        for row in rows:
            row["queues"][0]["producer_live"] = False
        self.assertNotIn("BUILDING_QUEUE_IDLE", codes(rows))

    def test_normal_band_and_production_observed(self):
        self.assertEqual(verifier.analyze(capture(), "g", "p")["status"], "OBSERVED_HEALTHY")

    def test_zero_full_and_above_band_thresholds(self):
        self.assertIn("ZERO_FUNDS", codes(capture(250, cash=0)))
        self.assertIn("STORAGE_FULL", codes(capture(250, cash=5000, resources=10000)))
        self.assertNotIn("STORAGE_FULL", codes(capture(250, capacity=0)))
        self.assertIn("FUNDS_ABOVE_BAND", codes(capture(1500, cash=15000)))
        self.assertNotIn("FUNDS_ABOVE_BAND", codes(capture(1450, cash=15000)))

    def test_cash_float_with_little_spend(self):
        rows = capture(cash=50000)
        for row in rows:
            row["spent"] = 0
        self.assertIn("CASH_FLOAT_WITH_LOW_SPENDING", codes(rows))

    def test_balance_recovery_resets_duration(self):
        rows = capture(1500, cash=15000)
        rows[10]["cash"] = 5000
        self.assertNotIn("FUNDS_ABOVE_BAND", codes(rows))

    def test_three_cancels_same_item_rolling_window(self):
        rows = capture(250)
        events = []
        for tick in (0, 50, 100):
            events.append({**rows[0], "kind": "cancel", "tick": tick, "queue_id": "1:building",
                           "item": "refinery", "item_id": str(tick), "reason": "no_site"})
        rows = sorted(rows[:-1] + events, key=lambda r: r["tick"]) + [rows[-1]]
        for seq, row in enumerate(rows):
            row["seq"] = seq
        self.assertIn("REPEATED_BUILDING_CANCELLATION", codes(rows))

    def test_missing_truncated_unknown_and_gaps(self):
        self.assertEqual(verifier.analyze([], "g", "p")["status"], "UNKNOWN")
        for mutate in (lambda rows: rows.pop(2), lambda rows: rows[2].update(dropped=1),
                       lambda rows: rows[2].update(profile_supported=False),
                       lambda rows: rows.pop(), lambda rows: rows[2].update(seq=999)):
            rows = capture()
            mutate(rows)
            self.assertEqual(verifier.analyze(rows, "g", "p")["status"], "UNKNOWN")

    def test_malformed_queue_storage_and_future_state_fail_closed(self):
        for mutate in (lambda r: r.update(resources=10001),
                       lambda r: r.update(queues_complete=False),
                       lambda r: r["queues"][0].update(state_since_tick=10000),
                       lambda r: r["queues"][0].update(producer_live=1)):
            rows = capture()
            mutate(rows[0])
            self.assertEqual(verifier.analyze(rows, "g", "p")["status"], "UNKNOWN")


if __name__ == "__main__":
    unittest.main()
