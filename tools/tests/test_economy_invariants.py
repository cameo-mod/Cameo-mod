import hashlib
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
                     "seat_state": "active",
                     "cash": cash, "resources": resources, "capacity": capacity,
                     "net_spent": tick * 10, "gross_spent": None, "gross_spend_complete": False,
                     "queues": [{"queue_id": "1:building", "producer_live": True,
                                 "state": state, "state_since_tick": 0, "reason": "no_site",
                                 "item": "refinery", "item_id": "1:0"}]})
    rows.append({**rows[-1], "kind": "pulse", "tick": duration + 50,
                 "seq": len(rows), "player_active": False, "seat_state": "lost"})
    rows.append({**rows[-1], "kind": "end", "complete": True, "seq": len(rows)})
    return rows


def codes(rows):
    return {f["code"] for f in analyze(rows)["findings"]}


def analyze(rows, game_uid="g", player="p", *, fixture_tick=None):
    # Unit-test metadata stands in for capture-bound receipt evidence.
    if fixture_tick is None:
        fixture_tick = rows[-1]["tick"] if rows else 0
    evidence = {"verified": True, "expected_tick": fixture_tick,
                "source_sha256": "a" * 64, "map_sha256": "b" * 64,
                "capture_game_uid": game_uid, "source_path": "test-fixture.lua"}
    return verifier.analyze(rows, game_uid, player, fixture_evidence=evidence)


def fixture_bundle(root, script_text, game_uid="g", player="p"):
    support = root / "support"
    support.mkdir(parents=True, exist_ok=True)
    map_root = root / "loaded-map"
    map_root.mkdir(parents=True, exist_ok=True)
    script = map_root / "ab_campaign_monitor.lua"
    script.write_text(script_text, encoding="utf-8")
    script_sha = hashlib.sha256(script.read_bytes()).hexdigest()
    map_sha = verifier._map_tree_sha256(map_root)
    (support / "cap_status.json").write_text(json.dumps({
        "status": "COMPLETED", "exit_code": 0,
        "command": ["python", "tools/ai/run_ai_match_batch.py", "--map", str(map_root),
                    "--support-dir", str(support)]}), encoding="utf-8")
    (support / "batch_summary.json").write_text(json.dumps({
        "support_dir": str(support), "fingerprint": {"map_sha256": map_sha}}), encoding="utf-8")
    (support / "batch_results.jsonl").write_text(json.dumps({
        "bot_outcomes": [{"record_id": f"{game_uid}|seat_3"}]}), encoding="utf-8")
    evidence = verifier.read_fixture_evidence(script, script_sha, support, game_uid, player)
    return support, map_root, script, script_sha, evidence


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
        self.assertEqual(analyze(rows, "g", "p")["status"], "UNKNOWN")

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
        self.assertEqual(analyze(capture(), "g", "p")["status"], "OBSERVED_HEALTHY")

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
                if row["kind"] == "pulse" and row["tick"] >= 100:
                    row["player_active"] = False
                    row["seat_state"] = "lost"
            self.assertNotIn("READY_BUILDING_UNPLACED", codes(rows))
            self.assertNotIn("BUILDING_QUEUE_IDLE", codes(rows))

    def test_cash_float_spending_unverified_diagnostic(self):
        rows = capture(cash=50000)
        for row in rows:
            row["net_spent"] = 0
        report = analyze(rows, "g", "p")
        self.assertNotIn("CASH_FLOAT_WITH_LOW_SPENDING", codes(rows))
        self.assertEqual(report["diagnostics"][0]["severity"], "DIAGNOSTIC")
        self.assertIn("FUNDS_ABOVE_BAND", codes(rows))

    def test_signed_net_spend_refunds_do_not_invalidate_capture(self):
        rows = capture()
        for row in rows:
            row["net_spent"] = 500 - row["tick"]
        report = analyze(rows, "g", "p")
        self.assertEqual(report["status"], "OBSERVED_HEALTHY")
        self.assertEqual(report["spending"]["net_spent"], -1050)
        self.assertIsNone(report["spending"]["gross_spent"])

    def test_legacy_or_unsupported_spend_evidence_unknown(self):
        for mutate in (lambda r: r.update(schema=1), lambda r: r.update(net_spent=True),
                       lambda r: r.update(gross_spent=100),
                       lambda r: r.update(gross_spend_complete=True),
                       lambda r: r.pop("gross_spent"), lambda r: r.pop("net_spent")):
            rows = capture()
            mutate(rows[0])
            self.assertEqual(analyze(rows, "g", "p")["status"], "UNKNOWN")

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
            report = analyze(rows, "g", "p")
            self.assertEqual(report["status"], "OBSERVED_HEALTHY")
            self.assertNotIn("REPEATED_BUILDING_CANCELLATION", codes(rows))

    def test_event_activity_is_authoritative_and_terminal_stops_later_events(self):
        rows = cancellation_capture()
        events = [r for r in rows if r["kind"] == "cancel"]
        # All pulses remain active: the exact event must stop the retry window.
        events[1]["player_active"] = False
        self.assertNotIn("REPEATED_BUILDING_CANCELLATION", codes(rows))
        # A terminal pulse closes the window: later event records are not
        # retroactively treated as pre-terminal cancellation defects.
        rows = cancellation_capture()
        for r in rows:
            if r["kind"] == "pulse" and r["tick"] >= 50:
                r["player_active"] = False
                r["seat_state"] = "lost"
            elif r["kind"] == "end":
                r["seat_state"] = "lost"
        rows.sort(key=lambda r: (r["tick"], r["kind"] == "end"))
        for seq, row in enumerate(rows):
            row["seq"] = seq
        report = analyze(rows, "g", "p")
        self.assertEqual(report["status"], "OBSERVED_HEALTHY")
        self.assertNotIn("REPEATED_BUILDING_CANCELLATION", codes(rows))

    def test_cancel_missing_or_unknown_event_state_is_unknown(self):
        for key in ("player_active", "producer_live", "cancellation_class"):
            for value in (None, "unknown", 1):
                rows = cancellation_capture()
                event = next(r for r in rows if r["kind"] == "cancel")
                event[key] = value
                self.assertEqual(analyze(rows, "g", "p")["status"], "UNKNOWN")
            rows = cancellation_capture()
            del next(r for r in rows if r["kind"] == "cancel")[key]
            self.assertEqual(analyze(rows, "g", "p")["status"], "UNKNOWN")

    def test_missing_truncated_unknown_and_gaps(self):
        self.assertEqual(analyze([], "g", "p")["status"], "UNKNOWN")
        for mutate in (lambda rows: rows.pop(2),
                       lambda rows: rows[2].update(profile_supported=False),
                       lambda rows: rows.pop(), lambda rows: rows[2].update(seq=999)):
            rows = capture()
            mutate(rows)
            self.assertEqual(analyze(rows, "g", "p")["status"], "UNKNOWN")

    def test_missed_scheduled_pulse_gap_is_unknown(self):
        # Producer cadence is one pulse per 50 world ticks (run C: 0/50/../13150).
        # Dropping one scheduled pulse leaves a >50 gap the coverage guard rejects.
        rows = capture(200)
        rows.pop(2)
        for seq, row in enumerate(rows):
            row["seq"] = seq
        report = analyze(rows, "g", "p")
        self.assertEqual(report["status"], "UNKNOWN")
        self.assertIn("pulse coverage gap", report["error"])

    def test_terminal_pulse_off_boundary_is_legal(self):
        # Run C (uid 25060589): last scheduled pulse at 13150, terminal observe at
        # 13191 emits one extra off-boundary pulse plus the end record. The checker
        # intentionally accepts this terminal-only off-boundary pulse; no strict
        # modulo rule applies.
        rows = capture(150)
        end = rows.pop()
        rows.pop()  # Replace the helper's terminal pulse with the off-boundary event.
        rows.append({**rows[-1], "kind": "pulse", "tick": 191,
                     "player_active": False, "seat_state": "lost"})
        rows.append({**end, "tick": 191, "seat_state": "lost"})
        for seq, row in enumerate(rows):
            row["seq"] = seq
        report = analyze(rows, "g", "p")
        self.assertEqual(report["status"], "OBSERVED_HEALTHY")
        self.assertEqual(report["pulses"], 4)

    def test_terminal_watermark_uncovered_is_unknown(self):
        rows = capture(150)
        rows[-1]["tick"] = 300
        report = analyze(rows, "g", "p")
        self.assertEqual(report["status"], "UNKNOWN")
        self.assertIn("terminal health watermark", report["error"])

    def test_record_after_terminal_is_unknown(self):
        rows = capture(150)
        end = rows.pop()
        rows.append(end)
        rows.append({**rows[-2], "tick": end["tick"]})
        for seq, row in enumerate(rows):
            row["seq"] = seq
        report = analyze(rows, "g", "p")
        self.assertEqual(report["status"], "UNKNOWN")
        self.assertIn("record after terminal", report["error"])

    def test_incomplete_terminal_is_unknown(self):
        rows = capture(150)
        rows[-1]["complete"] = False
        report = analyze(rows, "g", "p")
        self.assertEqual(report["status"], "UNKNOWN")
        self.assertIn("incomplete terminal", report["error"])

    def test_missing_terminal_is_unknown(self):
        report = analyze(capture(150)[:-1], "g", "p")
        self.assertEqual(report["status"], "UNKNOWN")
        self.assertIn("no complete terminal", report["error"])

    def test_clean_capture_requires_full_completeness_tuple(self):
        # Post-lift acceptance needs the whole tuple, not just dropped=0:
        # dropped=0 on every record, queues_complete=true on pulses,
        # complete=true on the end record.
        rows = capture(150)
        for row in rows:
            self.assertEqual(row["dropped"], 0)
            if row["kind"] == "pulse":
                self.assertIs(row["queues_complete"], True)
            elif row["kind"] == "end":
                self.assertIs(row["complete"], True)
        report = analyze(rows, "g", "p")
        self.assertEqual(report["status"], "OBSERVED_HEALTHY")
        self.assertNotIn("error", report)
        self.assertEqual(report["pulses"], 4)

    def test_malformed_queue_storage_and_future_state_fail_closed(self):
        for mutate in (lambda r: r.update(resources=10001),
                       lambda r: r.update(queues_complete=False),
                       lambda r: r["queues"][0].update(state_since_tick=10000),
                       lambda r: r["queues"][0].update(producer_live=1)):
            rows = capture()
            mutate(rows[0])
            self.assertEqual(analyze(rows, "g", "p")["status"], "UNKNOWN")

    def test_defeated_seat_is_explicit_terminal_verdict(self):
        # A fixture-forced terminal defeat closes a healthy pre-terminal window.
        rows = capture(150)
        rows[-1]["seat_state"] = "lost"
        report = analyze(rows, "g", "p")
        self.assertEqual(report["status"], "OBSERVED_HEALTHY")
        self.assertTrue(report["defeated"])
        self.assertEqual(report["terminal"]["kind"], "FIXTURE_TERMINAL")
        self.assertEqual(report["terminal"]["tick"], 200)
        self.assertEqual(report["terminal"]["seat_state"], "lost")

    def test_economy_after_first_terminal_pulse_is_outside_health_window(self):
        rows = capture(1500, cash=0)
        for row in rows:
            if row["tick"] >= 100:
                row["player_active"] = False
                row["seat_state"] = "lost"
        report = analyze(rows, "g", "p")
        self.assertEqual(report["status"], "OBSERVED_HEALTHY")
        self.assertEqual(report["terminal"]["tick"], 100)
        self.assertEqual(report["pulses"], 2)

    def test_defect_before_defeat_still_blocks(self):
        # An in-window finding stays hard failure when defeat ends the window.
        rows = capture(1500, cash=15000)
        rows[-1]["seat_state"] = "lost"
        report = analyze(rows, "g", "p")
        self.assertEqual(report["status"], "BLOCK")
        self.assertTrue(report["defeated"])
        self.assertIn("FUNDS_ABOVE_BAND", codes(rows))

    def test_dropped_telemetry_is_hard_block(self):
        rows = capture(1500)
        rows[10]["dropped"] = 1
        report = analyze(rows, "g", "p")
        self.assertEqual(report["status"], "BLOCK")
        self.assertIn("DROPPED_TELEMETRY", [x["code"] for x in report["findings"]])

    def test_missing_terminal_evidence_is_unknown(self):
        rows = capture(150)
        rows[-1].pop("seat_state")
        report = analyze(rows, "g", "p")
        self.assertEqual(report["status"], "UNKNOWN")
        self.assertIn("terminal seat state", report["error"])

    def test_active_end_without_terminal_outcome_is_unknown(self):
        rows = capture(150)
        terminal_pulse = next(r for r in rows if r["kind"] == "pulse" and r["tick"] == 200)
        terminal_pulse["player_active"] = True
        terminal_pulse["seat_state"] = "active"
        rows[-1]["seat_state"] = "active"
        report = analyze(rows)
        self.assertEqual(report["status"], "UNKNOWN")
        self.assertIn("missing terminal seat state", report["error"])

    def test_missing_fixture_provenance_is_generic_game_terminal(self):
        report = verifier.analyze(capture(150), "g", "p")
        self.assertEqual(report["status"], "OBSERVED_HEALTHY")
        self.assertEqual(report["terminal"]["kind"], "GAME_TERMINAL")

    def test_only_pinned_cap_tick_can_be_labelled_fixture_terminal(self):
        rows = capture(150)
        report = analyze(rows, fixture_tick=200)
        self.assertEqual(report["status"], "OBSERVED_HEALTHY")
        self.assertEqual(report["terminal"]["kind"], "FIXTURE_TERMINAL")
        natural = analyze(rows, fixture_tick=0)
        self.assertEqual(natural["status"], "OBSERVED_HEALTHY")
        self.assertEqual(natural["terminal"]["kind"], "GAME_TERMINAL")

    def test_early_terminal_is_game_terminal_at_minus_one_and_minus_fifty(self):
        rows = capture(150)
        for expected in (201, 250):
            report = analyze(rows, fixture_tick=expected)
            self.assertEqual(report["status"], "OBSERVED_HEALTHY")
            self.assertEqual(report["terminal"]["kind"], "GAME_TERMINAL")

    def test_won_at_cap_and_lost_at_cap_plus_sample_are_terminal_labels_only(self):
        won = capture(150)
        for row in won:
            if row["kind"] in ("pulse", "end") and row["tick"] >= 200:
                row["seat_state"] = "won"
                if row["kind"] == "pulse":
                    row["player_active"] = False
        report = analyze(won, fixture_tick=200)
        self.assertEqual(report["terminal"]["kind"], "FIXTURE_TERMINAL")
        self.assertEqual(report["status"], "OBSERVED_HEALTHY")

        sampled = capture(200)
        report = analyze(sampled, fixture_tick=200)
        self.assertEqual(report["terminal"]["kind"], "FIXTURE_TERMINAL")
        self.assertEqual(report["terminal"]["tick"], 250)
        self.assertEqual(report["status"], "OBSERVED_HEALTHY")

    def test_complete_game_without_fixture_is_game_terminal(self):
        report = verifier.analyze(capture(150), "g", "p")
        self.assertEqual(report["status"], "OBSERVED_HEALTHY")
        self.assertEqual(report["terminal"]["kind"], "GAME_TERMINAL")

    def test_complete_game_without_fixture_cli_is_healthy(self):
        with tempfile.TemporaryDirectory() as directory:
            support = Path(directory)
            logs = support / "Logs"
            logs.mkdir()
            (logs / "cameo-ai-economy-health.jsonl").write_text(
                "".join(json.dumps(row) + "\n" for row in capture(150)), encoding="utf-8")
            result = subprocess.run(
                [sys.executable, verifier.__file__, str(support), "--game-uid", "g",
                 "--player", "p"], capture_output=True, text=True, timeout=15)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            report = json.loads(result.stdout)
            self.assertEqual(report["status"], "OBSERVED_HEALTHY")
            self.assertEqual(report["terminal"]["kind"], "GAME_TERMINAL")

    def test_fixture_lua_evidence_is_sha_pinned_and_objective_scoped(self):
        source = ("local CampaignSeatNames = {\"p\"}\n"
                  "local CapTick = 200\n"
                  "local function FailPrimaryObjectives(player)\n"
                  "  player.MarkFailedObjective(1)\nend\n"
                  "WorldLoaded = function()\n"
                  "  Trigger.AfterDelay(CapTick, function()\n"
                  "    print(\"AB_CAMPAIGN_CAP tick=\" .. DateTime.GameTime)\n"
                  "    for _, name in ipairs(CampaignSeatNames) do local p = Player.GetPlayer(name); "
                  "if p then FailPrimaryObjectives(p) end end\n"
                  "  end)\nend\n")
        with tempfile.TemporaryDirectory() as directory:
            _, map_root, script, digest, evidence = fixture_bundle(Path(directory), source)
            self.assertEqual(evidence["expected_tick"], 200)
            self.assertEqual(evidence["source_sha256"], digest)
            self.assertEqual(evidence["map_sha256"], verifier._map_tree_sha256(map_root))
            self.assertIsNone(verifier.read_fixture_evidence(script, "0" * 64,
                                                            Path(directory) / "support", "g", "p"))
            foreign = Path(directory) / "foreign.lua"
            foreign.write_bytes(script.read_bytes())
            foreign_evidence = verifier.read_fixture_evidence(
                foreign, digest, Path(directory) / "support", "g", "p")
            self.assertIsNone(foreign_evidence)
            foreign_report = verifier.analyze(capture(150), "g", "p",
                                              fixture_evidence=foreign_evidence)
            self.assertEqual(foreign_report["status"], "OBSERVED_HEALTHY")
            self.assertEqual(foreign_report["terminal"]["kind"], "GAME_TERMINAL")
            self.assertIsNone(verifier.read_fixture_evidence(script, digest,
                                                            Path(directory) / "support", "other", "p"))

    def test_foreign_fixture_falls_back_to_same_game_terminal_verdict(self):
        rows = capture(150)
        report = verifier.analyze(rows, "g", "p", fixture_evidence=None)
        self.assertEqual(report["status"], "OBSERVED_HEALTHY")
        self.assertEqual(report["terminal"]["kind"], "GAME_TERMINAL")

    def test_post_terminal_tail_still_requires_complete_and_valid_pulse_shape(self):
        mutations = (
            lambda r: r.update(queues_complete=False),
            lambda r: r.pop("queues"),
            lambda r: r.update(cash="bad"),
            lambda r: r.update(resources=r["capacity"] + 1),
            lambda r: r["queues"].append(dict(r["queues"][0])),
        )
        for mutate in mutations:
            rows = capture(150, cash=0)
            terminal = next(r for r in rows if r["kind"] == "pulse" and r["tick"] == 200)
            mutate(terminal)
            report = analyze(rows)
            self.assertEqual(report["status"], "UNKNOWN", report)

    def test_dropped_telemetry_in_terminal_tail_remains_block(self):
        rows = capture(150)
        terminal = next(r for r in rows if r["kind"] == "pulse" and r["tick"] == 200)
        terminal["dropped"] = 1
        report = analyze(rows)
        self.assertEqual(report["status"], "BLOCK")
        self.assertIn("DROPPED_TELEMETRY", [x["code"] for x in report["findings"]])

    def test_post_terminal_cancellation_identity_and_duplicates_are_validated(self):
        rows = capture(150)
        for row in rows:
            if row["kind"] == "pulse" and row["tick"] >= 100:
                row["player_active"] = False
                row["seat_state"] = "lost"
        event = {**rows[0], "kind": "cancel", "tick": 125, "queue_id": "q",
                 "item": "refinery", "item_id": "ep1", "reason": "test",
                 "player_active": False, "producer_live": False,
                 "cancellation_class": "elimination"}
        rows.insert(-1, event)
        rows.sort(key=lambda r: (r["tick"], r["kind"] == "end"))
        for seq, row in enumerate(rows):
            row["seq"] = seq
        report = analyze(rows)
        self.assertEqual(report["status"], "OBSERVED_HEALTHY")
        rows.insert(-1, {**event, "tick": 150})
        rows.sort(key=lambda r: (r["tick"], r["kind"] == "end"))
        for seq, row in enumerate(rows):
            row["seq"] = seq
        report = analyze(rows)
        self.assertEqual(report["status"], "UNKNOWN")
        self.assertIn("duplicate cancellation", report["error"])

    def test_legacy_stream_missing_seat_state_is_unknown(self):
        for drop in ("pulses_only", "all"):
            rows = capture(150)
            for row in rows:
                if drop == "all" or row["kind"] == "pulse":
                    row.pop("seat_state", None)
            self.assertEqual(analyze(rows, "g", "p")["status"], "UNKNOWN")

    def test_seat_state_mutation_or_disagreement_is_unknown(self):
        # Reversion to active after a terminal seat state is corrupt evidence.
        rows = capture(150)
        rows[2]["seat_state"] = "lost"
        rows[2]["player_active"] = False
        self.assertEqual(analyze(rows, "g", "p")["status"], "UNKNOWN")
        # player_active and seat_state disagreeing on one row is corrupt evidence.
        rows = capture(150)
        rows[1]["seat_state"] = "lost"
        self.assertEqual(analyze(rows, "g", "p")["status"], "UNKNOWN")
        # A terminal record that disagrees with defeated pulses is corrupt evidence.
        rows = capture(150)
        for row in rows:
            row["player_active"] = False
            row["seat_state"] = "lost"
        rows[-1]["seat_state"] = "won"
        self.assertEqual(analyze(rows, "g", "p")["status"], "UNKNOWN")


if __name__ == "__main__":
    unittest.main()
