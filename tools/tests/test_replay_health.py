import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location("replay_health", Path(__file__).resolve().parents[1] / "ai/replay_health.py")
health = importlib.util.module_from_spec(spec)
spec.loader.exec_module(health)


def snapshot(tick, refs=0, harvesters=0, conyards=1, bank=10000, faction="ra1_allies"):
    return {"schema": 2, "kind": "situation", "game_uid": "g", "map_uid": "m",
            "player": "hard", "faction": faction, "bot_type": "hard", "tick": tick,
            "own": {"harvesters": harvesters, "banked_cash": bank},
            "expansion": {"refineries": refs, "conyards": conyards}}


def placement(tick=1374, player="classic", faction="ra1_allies"):
    return {"schema": 1, "kind": "placement", "game_uid": "g", "map_uid": "m",
            "player": player, "faction": faction, "bot_type": player,
            "tick": tick, "category": "refinery"}


def terminal(faction="ra1_allies"):
    return {"schema": 2, "game_uid": "g", "map_uid": "m", "duration_ticks": 10000,
            "player": {"name": "hard", "faction": faction, "bot_type": "hard", "outcome": "lost"},
            "stats": {"resources_earned": 4000, "resources_spent": 12000,
                      "kills_cost": 1000, "deaths_cost": 2000}}


class HealthTests(unittest.TestCase):
    def test_ra_failure_blocks_in_live_prefix_without_final(self):
        result = health.analyze([snapshot(t) for t in (3000, 3750, 4500)], [placement()], [], "g", "hard", live=True)
        self.assertEqual(result["status"], "BLOCK")
        self.assertEqual(result["earliest_block_tick"], 4500)
        self.assertEqual(result["findings"][0]["evidence"]["cause"].split(";")[0], "unknown")

    def test_ordinary_loss_and_credit_float_do_not_block(self):
        result = health.analyze([snapshot(t, 1, 1, bank=30000) for t in (3000, 3750, 4500)],
                                [placement(player="hard")], [terminal()], "g", "hard")
        self.assertEqual(result["status"], "OBSERVED_HEALTHY")
        self.assertIn("CREDIT_FLOAT", [f["code"] for f in result["findings"]])
        self.assertIn("NEGATIVE_VALUE_EXCHANGE", [f["code"] for f in result["findings"]])

    def test_refinery_destroyed_later_not_startup_stall(self):
        rows = [snapshot(1500, 1, 1)] + [snapshot(t) for t in (3000, 3750, 4500)]
        result = health.analyze(rows, [placement(), placement(1370, "hard")], [terminal()], "g", "hard")
        self.assertNotEqual(result["status"], "BLOCK")

    def test_prior_placement_between_snapshots_prevents_false_stall(self):
        result = health.analyze([snapshot(t) for t in (3000, 3750, 4500)],
                                [placement(), placement(1300, "hard")], [terminal()], "g", "hard")
        self.assertNotEqual(result["status"], "BLOCK")

    def test_refinery_lifecycle_rows_valid_and_not_placement(self):
        lost = placement(1400, "hard")
        lost["kind"] = "refinery_lost"
        result = health.analyze([snapshot(t) for t in (3000, 3750, 4500)],
                                [placement(), lost], [terminal()], "g", "hard")
        self.assertNotEqual(result["status"], "BLOCK")
        self.assertIsNone(result["metrics"]["first_refinery_placement_tick"])

    def test_conyard_lost_resets_persistence(self):
        rows = [snapshot(3000), snapshot(3750, conyards=0), snapshot(4500)]
        self.assertEqual(health.analyze(rows, [placement()], [terminal()], "g", "hard")["status"], "UNKNOWN")

    def test_missing_snapshots_not_assumed_healthy(self):
        self.assertEqual(health.analyze([], [], [terminal()], "g", "hard")["status"], "UNKNOWN")

    def test_sparse_snapshots_cannot_establish_persistence(self):
        rows = [snapshot(3000), snapshot(6000), snapshot(9000)]
        self.assertNotEqual(health.analyze(rows, [placement()], [terminal()], "g", "hard")["status"], "BLOCK")

    def test_no_reference_same_faction_success_is_unknown(self):
        rows = [snapshot(t) for t in (3000, 3750, 4500)]
        self.assertEqual(health.analyze(rows, [placement(faction="td_nod")], [terminal()], "g", "hard")["status"], "UNKNOWN")

    def test_unrelated_game_evidence_does_not_join(self):
        other = placement()
        other["game_uid"] = "other"
        self.assertEqual(health.analyze([snapshot(t) for t in (3000, 3750, 4500)], [other], [terminal()], "g", "hard")["status"], "UNKNOWN")

    def test_unknown_faction_not_cleared_by_normal_refinery_rule(self):
        self.assertEqual(health.analyze([snapshot(4500, faction="yuri")], [], [], "g", "hard", live=True)["status"], "UNKNOWN")

    def test_schema_missing_value_identity_drift_and_duplicate_tick(self):
        for mutation in (lambda r: r.update(schema=999), lambda r: r["own"].pop("harvesters"),
                         lambda r: r.update(map_uid="other")):
            a, b = snapshot(3000), snapshot(3750)
            mutation(b)
            with self.assertRaises(health.EvidenceError):
                health.analyze([a, b], [], [], "g", "hard")
        with self.assertRaises(health.EvidenceError):
            health.analyze([snapshot(3000), snapshot(3000)], [], [], "g", "hard")

    def test_invalid_policy_and_negative_values_fail_closed(self):
        for latency in (-1, 0, True):
            with self.assertRaises(health.EvidenceError):
                health.analyze([snapshot(3000)], [], [], "g", "hard", persistence=latency)
        with self.assertRaises(health.EvidenceError):
            health.analyze([snapshot(3000, harvesters=-1)], [], [], "g", "hard")

    def test_terminal_identity_drift_and_duplicate_fail_closed(self):
        with self.assertRaises(health.EvidenceError):
            health.analyze([snapshot(4500, 1, 1)], [], [terminal("td_nod")], "g", "hard")
        with self.assertRaises(health.EvidenceError):
            health.analyze([snapshot(4500, 1, 1)], [], [terminal(), terminal()], "g", "hard")

    def test_partial_live_tail_deferred_not_final_or_malformed_line(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "log.jsonl"
            path.write_bytes(b'{"good":1}\n{"partial":')
            rows, receipt = health.read_jsonl(path, live=True)
            self.assertEqual(rows, [{"good": 1}])
            self.assertTrue(receipt["partial_tail_deferred"])
            with self.assertRaises(health.EvidenceError):
                health.read_jsonl(path, live=False)
            path.write_bytes(b'broken\n')
            with self.assertRaises(health.EvidenceError):
                health.read_jsonl(path, live=True)

    def test_short_final_or_missing_final_not_health_pass(self):
        self.assertEqual(health.analyze([snapshot(1500, 1, 1)], [], [terminal()], "g", "hard")["status"], "UNKNOWN")
        self.assertEqual(health.analyze([snapshot(4500, 1, 1)], [], [], "g", "hard")["status"], "UNKNOWN")

    def test_spending_stall_is_review_warning_not_bug_proof(self):
        final = terminal()
        final["stats"]["stats_timeline_fields"] = "tick,earned,spent,army_value,assets_value,kills_cost,deaths_cost,banked,idle_queues"
        final["stats"]["stats_timeline"] = [[t, 50000, 10000, 1000, 5000, 0, 0, 40000, 2]
                                                 for t in range(3000, 6001, 750)]
        result = health.analyze([snapshot(6000, 1, 1)], [], [final], "g", "hard")
        self.assertEqual(result["status"], "OBSERVED_HEALTHY")
        self.assertIn("SPENDING_STALL_SUSPECTED", [f["code"] for f in result["findings"]])
        final["stats"]["stats_timeline"][-1][0] = 3000
        with self.assertRaises(health.EvidenceError):
            health.analyze([snapshot(6000, 1, 1)], [], [final], "g", "hard")


if __name__ == "__main__":
    unittest.main()
