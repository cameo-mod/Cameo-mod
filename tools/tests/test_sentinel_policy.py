"""SENTINEL-RUNNER-POLICY (rev6.1): opt-in sentinel economy gate unit tests.

Covers the all-seat collector (sentinel_economy_collect), the policy
dispositions (sentinel_policy), the MISSING_SITUATIONS verdict predicate,
the parse-time dispatch refusals, and the scoreboard quarantine. NO engine
launch, NO game runs — every fixture is synthetic JSONL + dicts.
"""
import json
import pathlib
import sys
import tempfile
import unittest
from unittest import mock

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/ai"))

import run_ai_match_batch as batch
import sentinel_policy as pol

UID = "uid-sentinel-1"


def pulse(seq, tick, *, seat, cash=2000, resources=0, capacity=5000,
          uid=UID, faction="td_nod", bot="classic", active=True, seat_state="active"):
    return {"schema": 2, "seq": seq, "tick": tick, "map_uid": "m1", "faction": faction,
            "profile": "p", "profile_supported": True, "dropped": 0,
            "player": seat, "game_uid": uid, "bot_type": bot, "kind": "pulse",
            "player_active": active, "seat_state": seat_state,
            "queues": [], "queues_complete": True,
            "cash": cash, "resources": resources, "capacity": capacity,
            "net_spent": 0, "gross_spent": None, "gross_spend_complete": False}


def end(seq, tick, *, seat, uid=UID, faction="td_nod", bot="classic", state="lost"):
    return {"schema": 2, "seq": seq, "tick": tick, "map_uid": "m1", "faction": faction,
            "profile": "p", "profile_supported": True, "dropped": 0,
            "player": seat, "game_uid": uid, "bot_type": bot, "kind": "end",
            "complete": True, "seat_state": state}


def near_empty_seat(seat, uid=UID, faction="td_nod", bot="classic"):
    """Funds <=100 sustained 250 ticks -> NEAR_EMPTY_FUNDS (DEFECT, BLOCK)."""
    rows = [pulse(i, i * 50, seat=seat, cash=50, uid=uid, faction=faction, bot=bot)
            for i in range(6)]
    rows.append(end(6, 250, seat=seat, uid=uid, faction=faction, bot=bot))
    return rows


def healthy_seat(seat, uid=UID, faction="td_gdi", bot="hard"):
    return [pulse(0, 0, seat=seat, uid=uid, faction=faction, bot=bot),
            pulse(1, 50, seat=seat, uid=uid, faction=faction, bot=bot),
            end(2, 50, seat=seat, uid=uid, faction=faction, bot=bot)]


def mrec(seat, uid=UID, faction="td_nod", bot="classic", home="1,1"):
    return {"record_id": f"{uid}|{seat}", "game_uid": uid,
            "player": {"seat": seat, "faction": faction, "bot_type": bot, "home": home}}


PLANNED = [("seat_0", "td_nod", "classic", "1,1"),
           ("seat_1", "td_gdi", "hard", "2,2")]

TWO_CLASSIC = [("seat_0", "td_nod", "classic", "1,1"),
               ("seat_1", "td_nod", "classic", "2,2")]


def write_leaf(rows):
    leaf = pathlib.Path(tempfile.mkdtemp(prefix="sentinel_leaf_"))
    (leaf / "Logs").mkdir(parents=True)
    (leaf / "Logs" / "cameo-ai-economy-health.jsonl").write_text(
        "".join(json.dumps(r) + "\n" for r in rows), encoding="utf-8")
    return leaf


def collect(leaf, records=None, roster=None):
    records = records if records is not None else [
        mrec("seat_0"), mrec("seat_1", faction="td_gdi", bot="hard", home="2,2")]
    roster = roster if roster is not None else PLANNED
    return batch.sentinel_economy_collect(leaf, records, roster)


class CollectorTests(unittest.TestCase):
    def test_all_seats_healthy(self):
        leaf = write_leaf(healthy_seat("seat_0", faction="td_nod", bot="classic")
                          + healthy_seat("seat_1"))
        raw, err, reports, failures = collect(leaf)
        self.assertEqual(raw, "OBSERVED_HEALTHY")
        self.assertIsNone(err)
        self.assertEqual(failures, [])
        self.assertEqual([r["status"] for r in reports], ["OBSERVED_HEALTHY", "OBSERVED_HEALTHY"])

    def test_block_collects_every_seat_no_early_return(self):
        # Legacy economy_health_file_status returns after the first BLOCK; the
        # sentinel collector must still report the second seat.
        rows = near_empty_seat("seat_0") + healthy_seat("seat_1")
        leaf = write_leaf(rows)
        raw, err, reports, failures = collect(leaf)
        self.assertEqual(raw, "BLOCK")
        self.assertEqual(len(reports), 2)
        self.assertEqual(reports[0]["status"], "BLOCK")
        self.assertEqual(reports[0]["findings"][0]["code"], "NEAR_EMPTY_FUNDS")
        self.assertEqual(reports[1]["status"], "OBSERVED_HEALTHY")
        # Same fixture under the legacy function: early-returned first
        # failure, unchanged semantics.
        legacy_status, _legacy_err = batch.economy_health_file_status(
            leaf, [mrec("seat_0"), mrec("seat_1", faction="td_gdi", bot="hard", home="2,2")],
            PLANNED)
        self.assertEqual(legacy_status, "BLOCK")

    def test_missing_file_is_unknown_not_block(self):
        leaf = pathlib.Path(tempfile.mkdtemp(prefix="sentinel_leaf_"))
        raw, err, reports, failures = collect(leaf)
        self.assertEqual(raw, "UNKNOWN")
        self.assertEqual(reports, [])
        self.assertTrue(any("missing expected economy-health" in f for f in failures))

    def test_empty_file_is_unknown(self):
        leaf = pathlib.Path(tempfile.mkdtemp(prefix="sentinel_leaf_"))
        (leaf / "Logs").mkdir(parents=True)
        (leaf / "Logs" / "cameo-ai-economy-health.jsonl").write_text("")
        raw, _err, _r, failures = collect(leaf)
        self.assertEqual(raw, "UNKNOWN")
        self.assertTrue(any("empty" in f for f in failures))

    def test_unterminated_file_is_unknown(self):
        leaf = pathlib.Path(tempfile.mkdtemp(prefix="sentinel_leaf_"))
        (leaf / "Logs").mkdir(parents=True)
        (leaf / "Logs" / "cameo-ai-economy-health.jsonl").write_text(
            json.dumps(healthy_seat("seat_0", bot="classic")[0]) + "\n" + '{"partial": true')
        raw, _err, _r, failures = collect(leaf)
        self.assertEqual(raw, "UNKNOWN")
        self.assertTrue(any("unterminated" in f for f in failures))

    def test_foreign_uid_row_is_unknown(self):
        rows = (healthy_seat("seat_0", faction="td_nod", bot="classic")
                + healthy_seat("seat_1")
                + [pulse(0, 0, seat="seat_0", uid="foreign-uid", bot="classic")])
        leaf = write_leaf(rows)
        raw, _err, _r, failures = collect(leaf)
        self.assertEqual(raw, "UNKNOWN")
        self.assertTrue(any("foreign game UID" in f for f in failures))

    def test_missing_seat_coverage_is_unknown(self):
        leaf = write_leaf(healthy_seat("seat_0", faction="td_nod", bot="classic"))
        raw, _err, reports, failures = collect(leaf)
        self.assertEqual(raw, "UNKNOWN")
        self.assertTrue(any("coverage" in f for f in failures))
        self.assertEqual(len(reports), 1)

    def test_roster_mismatch_is_unknown(self):
        leaf = write_leaf(healthy_seat("seat_0", faction="td_nod", bot="classic")
                          + healthy_seat("seat_1"))
        records = [mrec("seat_0"), mrec("seat_1", faction="td_gdi", bot="brutal", home="2,2")]
        raw, _err, _r, failures = collect(leaf, records=records)
        self.assertEqual(raw, "UNKNOWN")
        self.assertTrue(any("roster" in f for f in failures))

    def test_duplicate_record_identity_is_unknown(self):
        leaf = write_leaf(healthy_seat("seat_0", faction="td_nod", bot="classic")
                          + healthy_seat("seat_1"))
        records = [mrec("seat_0"), mrec("seat_0"),
                   mrec("seat_1", faction="td_gdi", bot="hard", home="2,2")]
        raw, _err, _r, failures = collect(leaf, records=records)
        self.assertEqual(raw, "UNKNOWN")
        self.assertTrue(any("duplicate match record" in f for f in failures))

    def test_multi_uid_records_is_unknown(self):
        leaf = write_leaf(healthy_seat("seat_0", faction="td_nod", bot="classic")
                          + healthy_seat("seat_1"))
        records = [mrec("seat_0"), mrec("seat_1", uid="uid-other", faction="td_gdi",
                                       bot="hard", home="2,2")]
        raw, _err, _r, failures = collect(leaf, records=records)
        self.assertEqual(raw, "UNKNOWN")
        self.assertTrue(any("exactly one match UID" in f for f in failures))


class DispositionTests(unittest.TestCase):
    """sentinel_policy.economy_disposition — maintainer F1 subset ruling."""

    def seat(self, name, bot, status, codes=None, severities=None, error=None):
        codes = codes or []
        severities = severities or ["DEFECT"] * len(codes)
        seat = {"player": name,
                "status": status,
                "findings": [{"code": c, "severity": s, "tick": 1}
                             for c, s in zip(codes, severities)]}
        if error is not None:
            seat["error"] = error
        return seat

    def policy(self, raw, seats, failures=(), roster=None):
        return pol.economy_disposition(raw, seats, list(failures),
                                       roster if roster is not None else PLANNED)

    def test_classic_sole_near_empty_is_baseline_exempted(self):
        seats = [self.seat("seat_0", "classic", "BLOCK", ["NEAR_EMPTY_FUNDS"]),
                 self.seat("seat_1", "hard", "OBSERVED_HEALTHY")]
        out = self.policy("BLOCK", seats)
        self.assertEqual(out["disposition"], "baseline_exempted")
        self.assertEqual(out["exempt_findings"], ["NEAR_EMPTY_FUNDS"])

    def test_classic_sole_funds_below_band_is_baseline_exempted(self):
        seats = [self.seat("seat_0", "classic", "BLOCK", ["FUNDS_BELOW_BAND"]),
                 self.seat("seat_1", "hard", "OBSERVED_HEALTHY")]
        self.assertEqual(self.policy("BLOCK", seats)["disposition"], "baseline_exempted")

    def test_both_allowed_findings_are_baseline_exempted(self):
        seats = [self.seat("seat_0", "classic", "BLOCK",
                         ["NEAR_EMPTY_FUNDS", "FUNDS_BELOW_BAND"]),
                 self.seat("seat_1", "hard", "OBSERVED_HEALTHY")]
        self.assertEqual(self.policy("BLOCK", seats)["disposition"], "baseline_exempted")

    def test_mixed_allowed_and_disallowed_code_blocks(self):
        seats = [self.seat("seat_0", "classic", "BLOCK",
                         ["NEAR_EMPTY_FUNDS", "STORAGE_FULL"]),
                 self.seat("seat_1", "hard", "OBSERVED_HEALTHY")]
        self.assertEqual(self.policy("BLOCK", seats)["disposition"], "blocking")

    def test_storage_full_alone_blocks(self):
        seats = [self.seat("seat_0", "classic", "BLOCK", ["STORAGE_FULL"]),
                 self.seat("seat_1", "hard", "OBSERVED_HEALTHY")]
        self.assertEqual(self.policy("BLOCK", seats)["disposition"], "blocking")

    def test_non_fund_defect_code_blocks(self):
        seats = [self.seat("seat_0", "classic", "BLOCK", ["READY_BUILDING_UNPLACED"]),
                 self.seat("seat_1", "hard", "OBSERVED_HEALTHY")]
        self.assertEqual(self.policy("BLOCK", seats)["disposition"], "blocking")

    def test_non_defect_severity_blocks(self):
        seats = [self.seat("seat_0", "classic", "BLOCK", ["NEAR_EMPTY_FUNDS"],
                         severities=["DIAGNOSTIC"]),
                 self.seat("seat_1", "hard", "OBSERVED_HEALTHY")]
        self.assertEqual(self.policy("BLOCK", seats)["disposition"], "blocking")

    def test_empty_findings_blocks(self):
        seats = [self.seat("seat_0", "classic", "BLOCK", []),
                 self.seat("seat_1", "hard", "OBSERVED_HEALTHY")]
        self.assertEqual(self.policy("BLOCK", seats)["disposition"], "blocking")

    def test_exemptible_classic_plus_structural_failure_blocks_as_unknown(self):
        seats = [self.seat("seat_0", "classic", "BLOCK", ["NEAR_EMPTY_FUNDS"]),
                 self.seat("seat_1", "hard", "OBSERVED_HEALTHY")]
        out = self.policy("UNKNOWN", seats, failures=["integrity: foreign uid"])
        self.assertEqual(out["disposition"], "blocking")

    def test_hard_seat_block_blocks(self):
        seats = [self.seat("seat_0", "classic", "OBSERVED_HEALTHY"),
                 self.seat("seat_1", "hard", "BLOCK", ["NEAR_EMPTY_FUNDS"])]
        self.assertEqual(self.policy("BLOCK", seats)["disposition"], "blocking")

    def test_hard_seat_block_alongside_exemptible_classic_blocks(self):
        seats = [self.seat("seat_0", "classic", "BLOCK", ["NEAR_EMPTY_FUNDS"]),
                 self.seat("seat_1", "hard", "BLOCK", ["FUNDS_BELOW_BAND"])]
        self.assertEqual(self.policy("BLOCK", seats)["disposition"], "blocking")

    def test_seat_analyzer_unknown_dominates_as_blocking(self):
        seats = [self.seat("seat_0", "classic", "UNKNOWN", []),
                 self.seat("seat_1", "hard", "OBSERVED_HEALTHY")]
        out = self.policy("UNKNOWN", seats)
        self.assertEqual(out["disposition"], "blocking")

    def test_all_healthy_is_clean_diagnostic(self):
        seats = [self.seat("seat_0", "classic", "OBSERVED_HEALTHY"),
                 self.seat("seat_1", "hard", "OBSERVED_HEALTHY")]
        out = self.policy("OBSERVED_HEALTHY", seats)
        self.assertEqual(out["disposition"], "clean_diagnostic")
        self.assertEqual(out["exempt_findings"], [])

    def test_unplanned_seat_never_exempted(self):
        roster = [("seat_0", "td_nod", "hard", "1,1"),
                  ("seat_1", "td_gdi", "hard", "2,2")]
        seats = [self.seat("seat_0", "hard", "BLOCK", ["NEAR_EMPTY_FUNDS"]),
                 self.seat("seat_1", "hard", "OBSERVED_HEALTHY")]
        self.assertEqual(self.policy("BLOCK", seats, roster=roster)["disposition"],
                         "blocking")

    def test_two_eligible_classic_seats_are_baseline_exempted(self):
        seats = [self.seat("seat_0", "classic", "BLOCK", ["NEAR_EMPTY_FUNDS"]),
                 self.seat("seat_1", "classic", "BLOCK", ["FUNDS_BELOW_BAND"])]
        out = self.policy("BLOCK", seats, roster=TWO_CLASSIC)
        self.assertEqual(out["disposition"], "baseline_exempted")
        self.assertEqual(out["exempt_findings"],
                         ["FUNDS_BELOW_BAND", "NEAR_EMPTY_FUNDS"])

    def test_second_classic_warn_finding_blocks(self):
        # The exempted candidate cannot cover for a second classic seat
        # whose allowed code carries a non-DEFECT severity.
        seats = [self.seat("seat_0", "classic", "BLOCK", ["NEAR_EMPTY_FUNDS"]),
                 self.seat("seat_1", "classic", "BLOCK", ["FUNDS_BELOW_BAND"],
                           severities=["WARN"])]
        self.assertEqual(self.policy("BLOCK", seats, roster=TWO_CLASSIC)["disposition"],
                         "blocking")

    def test_second_classic_non_dict_finding_blocks(self):
        seats = [self.seat("seat_0", "classic", "BLOCK", ["NEAR_EMPTY_FUNDS"]),
                 {"player": "seat_1", "status": "BLOCK", "error": None,
                  "findings": [{"code": "FUNDS_BELOW_BAND", "severity": "DEFECT",
                                "tick": 1},
                               "truncated-finding-row"]}]
        self.assertEqual(self.policy("BLOCK", seats, roster=TWO_CLASSIC)["disposition"],
                         "blocking")

    def test_second_classic_disallowed_code_blocks(self):
        seats = [self.seat("seat_0", "classic", "BLOCK", ["NEAR_EMPTY_FUNDS"]),
                 self.seat("seat_1", "classic", "BLOCK", ["STORAGE_FULL"])]
        self.assertEqual(self.policy("BLOCK", seats, roster=TWO_CLASSIC)["disposition"],
                         "blocking")

    def test_classic_block_with_error_blocks(self):
        # R2 completed/no-error: an errored BLOCK report's findings cannot
        # certify completeness — exemption refuses even on the allowed pair.
        seats = [self.seat("seat_0", "classic", "BLOCK", ["NEAR_EMPTY_FUNDS"],
                         error="terminal/integrity validation failed"),
                 self.seat("seat_1", "hard", "OBSERVED_HEALTHY")]
        self.assertEqual(self.policy("BLOCK", seats)["disposition"], "blocking")

    def test_healthy_co_seat_with_error_blocks(self):
        # Same contract applied capture-wide: an errored OBSERVED_HEALTHY
        # report beside an exemptible classic BLOCK is not a completed seat.
        seats = [self.seat("seat_0", "classic", "BLOCK", ["NEAR_EMPTY_FUNDS"]),
                 self.seat("seat_1", "hard", "OBSERVED_HEALTHY",
                         error="economy stream truncated mid-seat")]
        self.assertEqual(self.policy("BLOCK", seats)["disposition"], "blocking")

    def test_classic_block_with_falsy_error_blocks(self):
        # Strict semantics: even a falsy non-null error disqualifies.
        seats = [self.seat("seat_0", "classic", "BLOCK", ["NEAR_EMPTY_FUNDS"],
                         error=""),
                 self.seat("seat_1", "hard", "OBSERVED_HEALTHY")]
        self.assertEqual(self.policy("BLOCK", seats)["disposition"], "blocking")

    def test_classic_block_with_explicit_none_error_exempted(self):
        # Written-None is the production shape (the collector always emits
        # the key) — it must behave identically to an absent key.
        seat = self.seat("seat_0", "classic", "BLOCK", ["NEAR_EMPTY_FUNDS"])
        seat["error"] = None
        seats = [seat, self.seat("seat_1", "hard", "OBSERVED_HEALTHY")]
        out = self.policy("BLOCK", seats)
        self.assertEqual(out["disposition"], "baseline_exempted")
        self.assertEqual(out["exempt_findings"], ["NEAR_EMPTY_FUNDS"])


class MissingSituationsTests(unittest.TestCase):
    """Verdict-layer predicate only — checker output is never altered."""

    def report(self, status="UNKNOWN", findings=None):
        return {"policy": "startup-economy-v1", "status": status,
                "player": "seat_0", "findings": findings if findings is not None else [
                    {"code": "MISSING_SITUATIONS", "severity": "DEFECT", "tick": 0}]}

    def test_classic_sole_missing_situations_passes(self):
        self.assertTrue(pol.missing_situations_coverage(self.report(), "classic"))

    def test_hard_seat_does_not_pass(self):
        self.assertFalse(pol.missing_situations_coverage(self.report(), "hard"))

    def test_unknown_planned_seat_does_not_pass(self):
        self.assertFalse(pol.missing_situations_coverage(self.report(), None))

    def test_co_finding_does_not_pass(self):
        rep = self.report(findings=[
            {"code": "MISSING_SITUATIONS", "severity": "DEFECT"},
            {"code": "OTHER", "severity": "DEFECT"}])
        self.assertFalse(pol.missing_situations_coverage(rep, "classic"))

    def test_block_status_does_not_pass(self):
        rep = self.report(status="BLOCK")
        self.assertFalse(pol.missing_situations_coverage(rep, "classic"))

    def test_healthy_status_does_not_pass(self):
        rep = self.report(status="OBSERVED_HEALTHY", findings=[])
        self.assertFalse(pol.missing_situations_coverage(rep, "classic"))


class DispatchRefusalTests(unittest.TestCase):
    """S6: non-conforming sentinel invocations are refused, never coerced."""

    def run_main(self, extra):
        argv = ["run_ai_match_batch.py", "--sentinel-diagnostic"] + extra
        with mock.patch.object(sys, "argv", argv):
            with self.assertRaises(SystemExit) as cm:
                batch.main()
            self.assertEqual(cm.exception.code, 1)  # batch.fail() -> SystemExit(1)

    def test_multi_faction_refused(self):
        self.run_main(["--factions", "td_gdi,td_nod",
                       "--team-size", "1", "--repeats", "1", "--retries", "0"])

    def test_default_repeats_refused(self):
        # repeats default is 4 — the parse must refuse, not silently correct.
        self.run_main(["--factions", "td_gdi", "--team-size", "1", "--retries", "0"])

    def test_explicit_repeats_two_refused(self):
        self.run_main(["--factions", "td_gdi", "--team-size", "1",
                       "--repeats", "2", "--retries", "0"])

    def test_default_retries_refused(self):
        # retries default is 1 — refuse rather than coerce to 0.
        self.run_main(["--factions", "td_gdi", "--team-size", "1", "--repeats", "1"])

    def test_team_size_two_refused(self):
        self.run_main(["--factions", "td_gdi", "--team-size", "2",
                       "--repeats", "1", "--retries", "0"])

    def test_require_economy_health_refused(self):
        # The combination would silently convert every baseline_exempted
        # BLOCK to UNKNOWN — refused at parse, never coerced.
        self.run_main(["--factions", "td_gdi", "--team-size", "1",
                       "--repeats", "1", "--retries", "0",
                       "--require-economy-health"])


class ScoreboardQuarantineTests(unittest.TestCase):
    """Marked result rows can never enter either scoreboard."""

    def marked_result(self):
        return {"status": "ok", "health_status": "OBSERVED_HEALTHY",
                "capture_kind": "sentinel_diagnostic", "scoreable": False,
                "runtime_engine": {}, "runtime_engine_attempts": [],
                "bot_outcomes": [
                    {"record_id": f"{UID}|seat_0", "bot_type": "hard",
                     "outcome": "won", "spawn": 0,
                     "opponent": {"bot_type": "classic"}},
                    {"record_id": f"{UID}|seat_1", "bot_type": "classic",
                     "outcome": "lost", "spawn": 1,
                     "opponent": {"bot_type": "hard"}}]}

    def test_ab_scoreboard_skips_marked(self):
        self.assertEqual(batch.ab_scoreboard([self.marked_result()]), {})

    def test_team_scoreboard_skips_marked(self):
        self.assertEqual(batch.team_scoreboard([self.marked_result()]), {})

    def test_either_marker_alone_rejects(self):
        for key in ("capture_kind", "scoreable"):
            result = self.marked_result()
            del result["capture_kind" if key == "scoreable" else "scoreable"]
            self.assertEqual(batch.ab_scoreboard([result]), {}, f"marker {key} alone")


class ResultShapeTests(unittest.TestCase):
    """The sentinel result dict carries the full quarantine + policy payload."""

    def test_marked_result_fields(self):
        # The runner writes these three keys on every sentinel result; a
        # constructed equivalent must satisfy the record_marked predicate.
        import capture_provenance
        result = {"status": "ok", "health_status": "BLOCK",
                  "capture_kind": "sentinel_diagnostic", "scoreable": False,
                  "health_policy": {"mode": "sentinel_diagnostic",
                                    "disposition": "baseline_exempted",
                                    "exempt_findings": ["NEAR_EMPTY_FUNDS"],
                                    "seats": [], "seat_reports": [],
                                    "structural_failures": []}}
        self.assertTrue(capture_provenance.record_marked(result))
        self.assertEqual(result["health_policy"]["disposition"], "baseline_exempted")


if __name__ == "__main__":
    unittest.main()
