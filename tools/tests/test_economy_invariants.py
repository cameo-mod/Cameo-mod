import importlib.util
from pathlib import Path
import sys
import json
import subprocess
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "ai"))
import economy_invariants as verifier


def capture(duration=1500, state="producing", cash=5000, resources=0, capacity=10000):
    rows = []
    for tick in range(0, duration+1, 50):
        rows.append({"schema": 2, "game_uid": "g", "player": "p", "map_uid": "m",
                     "faction": "ra1_allies", "profile": "conventional", "profile_supported": True,
                     "dropped": 0, "kind": "pulse", "tick": tick, "seq": len(rows),
                     "queues_complete": True, "player_active": True,
                     "cash": cash, "resources": resources, "capacity": capacity,
                     "net_spent": tick * 10, "gross_spent": None, "gross_spend_complete": False,
                     "queues": [{"queue_id": "1:building", "producer_live": True,
                                 "state": state, "state_since_tick": 0, "reason": "no_site",
                                 "item": "refinery", "item_id": "1:0"}]})
    rows.append({**rows[-1], "kind": "end", "complete": True, "seq": len(rows)})
    return rows


def codes(rows):
    return {f["code"] for f in verifier.analyze(rows, "g", "p")["findings"]}


def cancellation_capture(active=True, live=True, classification="production"):
    rows = capture(250)
    events = [{**rows[0], "kind": "cancel", "tick": tick,
               "queue_id": "1:building", "item": "refinery", "item_id": str(tick),
               "reason": "no_site", "player_active": active, "producer_live": live,
               "cancellation_class": classification} for tick in (10, 60, 110)]
    rows = sorted(rows[:-1] + events, key=lambda r: r["tick"]) + [rows[-1]]
    for seq, row in enumerate(rows):
        row["seq"] = seq
    return rows


class InvariantTests(unittest.TestCase):
    def test_late_start_renumbered_capture_is_unknown(self):
        rows = [r for r in capture(6500) if r["tick"] >= 5000]
        for seq, row in enumerate(rows):
            row["seq"] = seq
        self.assertEqual(verifier.analyze(rows, "g", "p")["status"], "UNKNOWN")

    def test_malformed_input_persists_unknown_to_safe_new_report(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            support = root / "support"
            (support / "Logs").mkdir(parents=True)
            (support / "Logs/cameo-ai-economy-health.jsonl").write_text("{invalid\n")
            output = root / "unknown.json"
            result = subprocess.run([sys.executable, verifier.__file__, str(support),
                                     "--game-uid", "g", "--player", "p", "--output", str(output)],
                                    capture_output=True, text=True)
            self.assertEqual(result.returncode, 21)
            self.assertEqual(json.loads(output.read_text())["status"], "UNKNOWN")
            self.assertEqual(json.loads(result.stdout)["status"], "UNKNOWN")

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

    def test_near_empty_full_and_above_band_thresholds(self):
        self.assertIn("NEAR_EMPTY_FUNDS", codes(capture(250, cash=0)))
        self.assertIn("NEAR_EMPTY_FUNDS", codes(capture(250, cash=100)))
        self.assertNotIn("NEAR_EMPTY_FUNDS", codes(capture(250, cash=101)))
        self.assertIn("STORAGE_FULL", codes(capture(250, cash=5000, resources=10000)))
        self.assertNotIn("STORAGE_FULL", codes(capture(250, capacity=0)))
        self.assertIn("FUNDS_ABOVE_BAND", codes(capture(1500, cash=15000)))
        self.assertNotIn("FUNDS_ABOVE_BAND", codes(capture(1450, cash=15000)))

    def test_passive_income_does_not_hide_sustained_low_funds(self):
        rows = capture(1500, cash=1)
        for row in rows:
            row["cash"] = 1 + row["tick"] % 100
        self.assertIn("FUNDS_BELOW_BAND", codes(rows))

    def test_inactive_player_not_postmortem_queue_defect(self):
        for state in ("ready", "idle"):
            rows = capture(1500, state)
            for row in rows:
                row["player_active"] = False
            self.assertEqual(codes(rows), set())

    def test_cash_float_spending_unverified_diagnostic(self):
        rows = capture(cash=50000)
        for row in rows:
            row["net_spent"] = 0
        report = verifier.analyze(rows, "g", "p")
        self.assertNotIn("CASH_FLOAT_WITH_LOW_SPENDING", codes(rows))
        self.assertEqual(report["diagnostics"][0]["severity"], "DIAGNOSTIC")
        self.assertIn("FUNDS_ABOVE_BAND", codes(rows))

    def test_signed_net_spend_refunds_do_not_invalidate_capture(self):
        rows = capture()
        for row in rows:
            row["net_spent"] = 500 - row["tick"]
        report = verifier.analyze(rows, "g", "p")
        self.assertEqual(report["status"], "OBSERVED_HEALTHY")
        self.assertEqual(report["spending"]["net_spent"], -1000)
        self.assertIsNone(report["spending"]["gross_spent"])

    def test_legacy_or_unsupported_spend_evidence_unknown(self):
        for mutate in (lambda r: r.update(schema=1), lambda r: r.update(net_spent=True),
                       lambda r: r.update(gross_spent=100),
                       lambda r: r.update(gross_spend_complete=True),
                       lambda r: r.pop("gross_spent"), lambda r: r.pop("net_spent")):
            rows = capture()
            mutate(rows[0])
            self.assertEqual(verifier.analyze(rows, "g", "p")["status"], "UNKNOWN")

    def test_balance_recovery_resets_duration(self):
        rows = capture(1500, cash=15000)
        rows[10]["cash"] = 5000
        self.assertNotIn("FUNDS_ABOVE_BAND", codes(rows))

    def test_three_cancels_same_item_rolling_window(self):
        self.assertIn("REPEATED_BUILDING_CANCELLATION", codes(cancellation_capture()))

    def test_inactive_and_destruction_cleanup_not_retry_defect(self):
        for args in ((False, True, "production"), (True, False, "production"),
                     (True, True, "destruction"), (False, False, "elimination")):
            rows = cancellation_capture(*args)
            report = verifier.analyze(rows, "g", "p")
            self.assertEqual(report["status"], "OBSERVED_HEALTHY")
            self.assertNotIn("REPEATED_BUILDING_CANCELLATION", codes(rows))

    def test_event_activity_changes_between_pulses(self):
        rows = cancellation_capture()
        events = [r for r in rows if r["kind"] == "cancel"]
        # All pulses remain active: the exact event must stop the retry window.
        events[1]["player_active"] = False
        self.assertNotIn("REPEATED_BUILDING_CANCELLATION", codes(rows))
        # Conversely, stale inactive pulses cannot hide three live active events
        # occurring between two samples.
        rows = cancellation_capture()
        for r in rows:
            if r["kind"] == "pulse":
                r["player_active"] = False
            elif r["kind"] == "cancel":
                r["tick"] = {10: 10, 60: 20, 110: 30}[r["tick"]]
        rows.sort(key=lambda r: (r["tick"], r["kind"] == "end"))
        for seq, row in enumerate(rows):
            row["seq"] = seq
        self.assertIn("REPEATED_BUILDING_CANCELLATION", codes(rows))

    def test_cancel_missing_or_unknown_event_state_is_unknown(self):
        for key in ("player_active", "producer_live", "cancellation_class"):
            for value in (None, "unknown", 1):
                rows = cancellation_capture()
                event = next(r for r in rows if r["kind"] == "cancel")
                event[key] = value
                self.assertEqual(verifier.analyze(rows, "g", "p")["status"], "UNKNOWN")
            rows = cancellation_capture()
            del next(r for r in rows if r["kind"] == "cancel")[key]
            self.assertEqual(verifier.analyze(rows, "g", "p")["status"], "UNKNOWN")

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
