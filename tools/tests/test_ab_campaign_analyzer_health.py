import contextlib
import io
import json
import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))
sys.path.insert(0, str(ROOT / "tools" / "tests"))
import ab_campaign_analyzer as az
from test_ab_campaign_analyzer import matched_pair, receipt


def health_report(game_uid, player, status="OBSERVED_HEALTHY", policy=az.HEALTH_POLICY):
    return {"policy": policy, "game_uid": game_uid, "player": player, "status": status,
            "findings": [], "metrics": {}, "earliest_block_tick": None}


def health_for(games, statuses=None, seats=2, policy=az.HEALTH_POLICY):
    """Build the loaded-health structure for the given receipts' game_uids."""
    statuses = statuses or {}
    by_game = {}
    for g in games:
        uid = g["game_uid"]
        if uid is None:
            continue
        for n in range(seats):
            status = statuses.get((uid, n), "OBSERVED_HEALTHY")
            if status is None:
                continue
            by_game.setdefault(uid, {}).setdefault(f"seat_{n}", []).append(health_report(uid, f"seat_{n}", status, policy))
    return {"games": by_game, "errors": []}


class HealthJoin(unittest.TestCase):
    def test_all_seats_healthy_counts_the_pair(self):
        games = matched_pair()
        res = az.summarize(games, None, health_for(games))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 1)
        self.assertEqual(res["health"]["mode"], "APPLIED")
        self.assertEqual(res["health"]["verdict_counts"], {"HEALTHY": 2})
        self.assertEqual(res["health"]["excluded_games"], [])

    def test_block_on_one_seat_invalidates_the_game_and_is_never_pooled(self):
        games = matched_pair()
        res = az.summarize(games, None, health_for(games, {("uid-0-0", 1): "BLOCK"}))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(res["invalid_receipt_count"], 1)
        self.assertIn("replay_health BLOCK", res["invalid_receipts"][0]["reason"])
        self.assertEqual(res["health"]["excluded_by_setup"], {"1v1_nuclear_gdi": {"BLOCK": 1}})
        self.assertEqual(res["income_army_metrics"]["control"]["seats_with_metrics"]
                         + res["income_army_metrics"]["treatment"]["seats_with_metrics"], 2,
                         "only the healthy game's two seats are aggregated")
        self.assertTrue(res["sequential_decision"]["crashed"])

    def test_missing_report_is_incomplete_and_excluded_from_metrics(self):
        games = matched_pair()
        res = az.summarize(games, None, health_for(games, {("uid-0-1", 0): None, ("uid-0-1", 1): None}))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(res["pooled"]["status_counts"], {"INCOMPLETE": 1})
        self.assertEqual(res["health"]["excluded_by_setup"], {"1v1_nuclear_gdi": {"MISSING": 1}})
        self.assertEqual(res["invalid_receipt_count"], 0)
        seats = sum(res["income_army_metrics"][a]["seats_with_metrics"] for a in ("control", "treatment"))
        self.assertEqual(seats, 2)

    def test_partial_seat_coverage_is_missing(self):
        games = matched_pair()
        res = az.summarize(games, None, health_for(games, {("uid-0-0", 1): None}))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(res["health"]["excluded_games"][0]["verdict"], "MISSING")

    def test_unknown_status_is_incomplete(self):
        games = matched_pair()
        res = az.summarize(games, None, health_for(games, {("uid-0-0", 0): "UNKNOWN"}))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(res["health"]["excluded_games"][0]["verdict"], "UNKNOWN")
        self.assertEqual(res["pooled"]["status_counts"], {"INCOMPLETE": 1})

    def test_schema_mismatch_is_incomplete(self):
        games = matched_pair()
        res = az.summarize(games, None, health_for(games, policy="startup-economy-v0"))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual({e["verdict"] for e in res["health"]["excluded_games"]}, {"SCHEMA_MISMATCH"})

    def test_conflicting_duplicate_reports_are_not_trusted(self):
        games = matched_pair()
        health = health_for(games)
        health["games"]["uid-0-0"]["seat_0"].append(health_report("uid-0-0", "seat_0", "UNKNOWN"))
        res = az.summarize(games, None, health)
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(res["health"]["excluded_games"][0]["verdict"], "SCHEMA_MISMATCH")

    def test_block_dominates_other_bad_verdicts(self):
        games = matched_pair()
        res = az.summarize(games, None, health_for(games, {("uid-0-0", 0): "UNKNOWN", ("uid-0-0", 1): "BLOCK"}))
        verdicts = {e["verdict"] for e in res["health"]["excluded_games"]}
        self.assertEqual(verdicts, {"BLOCK"})

    def test_no_health_dir_keeps_current_behaviour_with_a_loud_warning(self):
        games = matched_pair()
        res = az.summarize(games, None)
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 1)
        self.assertEqual(res["health"]["mode"], "NOT_APPLIED")
        self.assertIn("WARNING", res["health"]["warning"])

    def test_cli_warns_on_stderr_without_health_dir_and_reports_excluded_counts_with_it(self):
        games = matched_pair()
        with tempfile.TemporaryDirectory() as tmp:
            tmp = pathlib.Path(tmp)
            rdir, hdir = tmp / "receipts", tmp / "health"
            rdir.mkdir()
            hdir.mkdir()
            for i, g in enumerate(games):
                (rdir / f"r{i}.json").write_text(json.dumps(g), encoding="utf-8")
            # only game 0 gets reports; game 1 stays MISSING
            for n in range(2):
                (hdir / f"h0_{n}.json").write_text(json.dumps(health_report("uid-0-0", f"seat_{n}")), encoding="utf-8")
            (hdir / "corrupt.json").write_text("{not json", encoding="utf-8")
            (hdir / "unrelated.json").write_text(json.dumps({"hello": 1}), encoding="utf-8")

            err = io.StringIO()
            with contextlib.redirect_stderr(err), contextlib.redirect_stdout(io.StringIO()):
                az.main(["analyze", "--receipts", str(rdir), "--output", str(tmp / "no_health.json")])
            self.assertIn("WARNING", err.getvalue())

            err = io.StringIO()
            with contextlib.redirect_stderr(err), contextlib.redirect_stdout(io.StringIO()):
                az.main(["analyze", "--receipts", str(rdir), "--health-dir", str(hdir), "--output", str(tmp / "health.json")])
            self.assertNotIn("no --health-dir", err.getvalue())
            self.assertIn("no --econ-health-dir", err.getvalue())
            out = json.loads((tmp / "health.json").read_text(encoding="utf-8"))
            self.assertEqual(out["health"]["mode"], "APPLIED")
            self.assertEqual(out["health"]["excluded_by_setup"], {"1v1_nuclear_gdi": {"MISSING": 1}})
            self.assertEqual(len(out["health"]["load_errors"]), 1)
            self.assertEqual(out["pooled"]["complete_natural_pairs"], 0)


class HostRestartMarkers(unittest.TestCase):
    def marker(self, pair_index=0, member=0):
        return receipt(pair_index=pair_index, pair_member=member, control_side="A", treatment_side="B",
                       winner_team=None, end_class="INCOMPLETE", end_reason=az.MARKER_REASON)

    def pair_with_marker_for_member0(self):
        natural = matched_pair()
        return self.marker(), natural

    def test_marker_alone_is_never_pooled(self):
        m = self.marker()
        other = matched_pair()[1]
        res = az.summarize([m, other], None, health_for([other]))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(res["pooled"]["status_counts"], {"INCOMPLETE": 1})
        self.assertEqual(res["health"]["verdict_counts"].get("MARKER"), 1)
        seats = sum(res["income_army_metrics"][a]["seats_with_metrics"] for a in ("control", "treatment"))
        self.assertEqual(seats, 2, "marker metrics are not aggregated")

    def test_natural_healthy_rerun_replaces_the_marker(self):
        pair = matched_pair()
        res = az.summarize([self.marker(), *pair], None, health_for(pair))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 1)
        self.assertEqual(len(res["health"]["marker_reruns_replaced"]), 1)
        self.assertEqual(res["health"]["marker_reruns_rejected"], [])

    def test_unhealthy_rerun_does_not_replace_the_marker(self):
        pair = matched_pair()
        res = az.summarize([self.marker(), *pair], None, health_for(pair, {("uid-0-0", 0): "UNKNOWN"}))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(res["pooled"]["status_counts"], {"INCOMPLETE": 1})
        self.assertEqual(len(res["health"]["marker_reruns_rejected"]), 1)
        self.assertEqual(res["health"]["marker_reruns_replaced"], [])

    def test_capped_rerun_does_not_replace_the_marker(self):
        capped = matched_pair(end_class="CAP", end_reason="cap")
        res = az.summarize([self.marker(), *capped], None, health_for(capped))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(len(res["health"]["marker_reruns_rejected"]), 1)

    def test_two_reruns_are_ambiguous_and_do_not_replace_the_marker(self):
        pair = matched_pair()
        second = dict(pair[0], cell_id="1v1_nuclear_gdi|1337|0|again", game_uid="uid-0-0b")
        res = az.summarize([self.marker(), *pair, second], None, health_for([*pair, second]))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(len(res["health"]["marker_reruns_rejected"]), 1)


class SeatIdentityBridge(unittest.TestCase):
    """Reports are named seat_N (matches namespace); receipts say Multi0/Multi1. The bridge is
    (home cell, faction, bot_type) from receipt.seats[] to matches JSONL player rows."""

    HOMES = ("11,45", "90,24")

    def bridged(self, tmp, uid_suffix="0-0", mutate=None):
        game = receipt(pair_index=0, pair_member=0, control_side="A", treatment_side="B", winner_team="B")
        uid = game["game_uid"]
        game["seats"] = [
            {"arm": "control", "bot_type": "hard_control", "faction": "td_gdi", "home": "Multi0", "home_location": self.HOMES[0]},
            {"arm": "treatment", "bot_type": "hard_treatment", "faction": "td_gdi", "home": "Multi1", "home_location": self.HOMES[1]},
        ]
        pairs = [("seat_3", self.HOMES[0], "hard_control"), ("seat_4", self.HOMES[1], "hard_treatment")]
        lines = [json.dumps({"game_uid": uid, "player": {"seat": s, "home": h, "faction": "td_gdi", "bot_type": b},
                              "seats": [{"seat": a, "home": c, "faction": "td_gdi"} for a, c, _ in pairs]}) for s, h, b in pairs]
        path = pathlib.Path(tmp) / "cameo-ai-matches.jsonl"
        data = ("\n".join(lines) + "\n").encode("utf-8")
        path.write_bytes(data)
        import hashlib
        game["artifacts"]["matches_jsonl"] = {"path": str(path), "sha256": hashlib.sha256(data).hexdigest(), "missing_reason": None}
        if mutate:
            mutate(game, path)
        return game, uid

    def health(self, uid, players, status="OBSERVED_HEALTHY"):
        return {"games": {uid: {p: [health_report(uid, p, status)] for p in players}}, "errors": []}

    def test_correct_seat_names_are_healthy_and_identity_verified(self):
        with tempfile.TemporaryDirectory() as tmp:
            game, uid = self.bridged(tmp)
            res = az.summarize([game], None, self.health(uid, ["seat_3", "seat_4"]))
        self.assertEqual(res["health"]["verdict_counts"], {"HEALTHY": 1})
        self.assertEqual(res["health"]["identity_unverified_games"], [])

    def test_foreign_seat_name_with_matching_count_is_not_admitted(self):
        with tempfile.TemporaryDirectory() as tmp:
            game, uid = self.bridged(tmp)
            res = az.summarize([game], None, self.health(uid, ["seat_3", "seat_99"]))
        self.assertEqual(res["health"]["excluded_games"][0]["verdict"], "MISSING")
        self.assertIn("seat_4", res["health"]["excluded_games"][0]["detail"])
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)

    def test_extra_foreign_seat_is_a_schema_mismatch(self):
        with tempfile.TemporaryDirectory() as tmp:
            game, uid = self.bridged(tmp)
            res = az.summarize([game], None, self.health(uid, ["seat_3", "seat_4", "seat_99"]))
        self.assertEqual(res["health"]["excluded_games"][0]["verdict"], "SCHEMA_MISMATCH")

    def test_matches_artifact_hash_mismatch_is_a_schema_mismatch(self):
        def tamper(game, path):
            path.write_bytes(path.read_bytes() + b"\n")
        with tempfile.TemporaryDirectory() as tmp:
            game, uid = self.bridged(tmp, mutate=tamper)
            res = az.summarize([game], None, self.health(uid, ["seat_3", "seat_4"]))
        self.assertEqual(res["health"]["excluded_games"][0]["verdict"], "SCHEMA_MISMATCH")
        self.assertIn("sha256", res["health"]["excluded_games"][0]["detail"])

    def test_ambiguous_bridge_is_a_schema_mismatch(self):
        def same_home(game, path):
            game["seats"][1]["home_location"] = game["seats"][0]["home_location"]
            game["artifacts"]["matches_jsonl"]["sha256"] = None
        with tempfile.TemporaryDirectory() as tmp:
            game, uid = self.bridged(tmp, mutate=same_home)
            res = az.summarize([game], None, self.health(uid, ["seat_3", "seat_4"]))
        self.assertEqual(res["health"]["excluded_games"][0]["verdict"], "SCHEMA_MISMATCH")

    def test_unavailable_bridge_falls_back_to_count_and_is_reported(self):
        games = matched_pair()
        res = az.summarize(games, None, health_for(games))
        self.assertEqual(res["health"]["verdict_counts"], {"HEALTHY": 2})
        self.assertEqual(len(res["health"]["identity_unverified_games"]), 2)

    def test_count_fallback_rejects_extra_seats_and_zero_expected(self):
        self.assertEqual(az.health_verdict({f"s{i}": [health_report("g", f"s{i}")] for i in range(3)}, 2)[0], "SCHEMA_MISMATCH")
        self.assertEqual(az.health_verdict({"s0": [health_report("g", "s0")]}, 0)[0], "SCHEMA_MISMATCH")

    def test_original_end_reason_is_kept_for_excluded_games(self):
        games = matched_pair()
        res = az.summarize(games, None, health_for(games, {("uid-0-0", 0): "UNKNOWN"}))
        row = res["health"]["excluded_games"][0]
        self.assertEqual((row["original_end_class"], row["original_end_reason"]), ("NATURAL", "natural"))


class LoadHealth(unittest.TestCase):
    def test_receipts_and_foreign_json_are_ignored_and_corrupt_files_are_reported(self):
        with tempfile.TemporaryDirectory() as tmp:
            tmp = pathlib.Path(tmp)
            (tmp / "receipt.json").write_text(json.dumps(matched_pair()[0]), encoding="utf-8")
            (tmp / "foreign.json").write_text(json.dumps({"game_uid": "x", "status": "NATURAL_END"}), encoding="utf-8")
            (tmp / "good.json").write_text(json.dumps(health_report("g1", "seat_0")), encoding="utf-8")
            (tmp / "bad.json").write_text("nope", encoding="utf-8")
            loaded = az.load_health([tmp])
        self.assertEqual(list(loaded["games"]), ["g1"])
        self.assertEqual(len(loaded["errors"]), 1)


def econ_health_for(games, statuses=None, seats=2, replay_health=None):
    """Loaded-health structure carrying the economy_invariants family alongside replay_health."""
    health = replay_health if replay_health is not None else health_for(games)
    statuses = statuses or {}
    by_game = {}
    for g in games:
        uid = g["game_uid"]
        if uid is None:
            continue
        for n in range(seats):
            status = statuses.get((uid, n), "OBSERVED_HEALTHY")
            if status is None:
                continue
            by_game.setdefault(uid, {}).setdefault(f"seat_{n}", []).append(
                health_report(uid, f"seat_{n}", status, az.ECON_POLICY))
    health = dict(health)
    health["econ_games"] = by_game
    return health


class DualGate(unittest.TestCase):
    def test_both_gates_healthy_counts_the_pair(self):
        games = matched_pair()
        res = az.summarize(games, None, econ_health_for(games))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 1)
        self.assertEqual(res["health"]["mode"], "APPLIED")
        self.assertEqual(res["health"]["econ_armed"], True)
        self.assertEqual(res["health"]["econ_policy"], az.ECON_POLICY)
        self.assertEqual(res["health"]["verdict_counts"], {"HEALTHY": 2})
        self.assertEqual(res["health"]["excluded_games"], [])

    def test_econ_missing_excludes_the_game_as_unknown(self):
        games = matched_pair()
        res = az.summarize(games, None, econ_health_for(
            games, {("uid-0-0", 0): None, ("uid-0-0", 1): None}))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(res["health"]["excluded_by_setup"], {"1v1_nuclear_gdi": {"UNKNOWN": 1}})

    def test_econ_partial_seat_coverage_is_unknown(self):
        games = matched_pair()
        res = az.summarize(games, None, econ_health_for(games, {("uid-0-0", 1): None}))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(res["health"]["excluded_games"][0]["verdict"], "UNKNOWN")

    def test_econ_block_invalidates_the_game_and_names_the_family(self):
        games = matched_pair()
        res = az.summarize(games, None, econ_health_for(games, {("uid-0-0", 0): "BLOCK"}))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(res["invalid_receipt_count"], 1)
        self.assertIn("economy_invariants BLOCK", res["invalid_receipts"][0]["reason"])

    def test_econ_block_dominates_replay_missing(self):
        games = matched_pair()
        res = az.summarize(games, None, econ_health_for(
            games, {("uid-0-1", 0): "BLOCK"},
            replay_health=health_for(games, {("uid-0-1", 0): None, ("uid-0-1", 1): None})))
        self.assertEqual({e["verdict"] for e in res["health"]["excluded_games"]}, {"BLOCK"})

    def test_econ_unknown_is_incomplete(self):
        games = matched_pair()
        res = az.summarize(games, None, econ_health_for(games, {("uid-0-0", 0): "UNKNOWN"}))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(res["health"]["excluded_games"][0]["verdict"], "UNKNOWN")
        self.assertEqual(res["pooled"]["status_counts"], {"INCOMPLETE": 1})

    def test_econ_conflicting_duplicates_are_not_trusted(self):
        games = matched_pair()
        health = econ_health_for(games)
        health["econ_games"]["uid-0-0"]["seat_0"].append(
            health_report("uid-0-0", "seat_0", "UNKNOWN", az.ECON_POLICY))
        res = az.summarize(games, None, health)
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(res["health"]["excluded_games"][0]["verdict"], "SCHEMA_MISMATCH")

    def test_econ_seat_identity_mismatch_is_schema_mismatch(self):
        games = matched_pair()
        health = econ_health_for(games)
        health["econ_games"]["uid-0-0"]["seat_7"] = [health_report("uid-0-0", "seat_7", policy=az.ECON_POLICY)]
        res = az.summarize(games, None, health)
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(res["health"]["excluded_games"][0]["verdict"], "SCHEMA_MISMATCH")

    def test_econ_reports_in_replay_dir_do_not_silently_arm_the_family(self):
        games = matched_pair()
        # An econ report that leaks into --health-dir without --econ-health-dir stays foreign evidence.
        health = health_for(games)
        health["games"]["uid-0-0"]["seat_0"].append(
            health_report("uid-0-0", "seat_0", policy=az.ECON_POLICY))
        res = az.summarize(games, None, health)
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(res["health"]["excluded_games"][0]["verdict"], "SCHEMA_MISMATCH")
        self.assertEqual(res["health"]["econ_armed"], False)

    def test_replay_missing_and_econ_healthy_is_missing(self):
        games = matched_pair()
        res = az.summarize(games, None, econ_health_for(
            games, replay_health=health_for(games, {("uid-0-0", 0): None, ("uid-0-0", 1): None})))
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)
        self.assertEqual(res["health"]["excluded_games"][0]["verdict"], "MISSING")

    def test_cli_econ_dir_arms_the_dual_gate_and_warns_when_absent(self):
        games = matched_pair()
        with tempfile.TemporaryDirectory() as tmp:
            tmp = pathlib.Path(tmp)
            rdir, hdir, edir = tmp / "receipts", tmp / "health", tmp / "econ"
            for d in (rdir, hdir, edir):
                d.mkdir()
            for i, g in enumerate(games):
                (rdir / f"r{i}.json").write_text(json.dumps(g), encoding="utf-8")
                for n in range(2):
                    (hdir / f"h{i}_{n}.json").write_text(
                        json.dumps(health_report(g["game_uid"], f"seat_{n}")), encoding="utf-8")
                    (edir / f"e{i}_{n}.json").write_text(
                        json.dumps(health_report(g["game_uid"], f"seat_{n}", policy=az.ECON_POLICY)),
                        encoding="utf-8")

            err = io.StringIO()
            with contextlib.redirect_stderr(err), contextlib.redirect_stdout(io.StringIO()):
                az.main(["analyze", "--receipts", str(rdir), "--health-dir", str(hdir),
                         "--output", str(tmp / "single.json")])
            self.assertIn("no --econ-health-dir", err.getvalue())
            single = json.loads((tmp / "single.json").read_text(encoding="utf-8"))
            self.assertEqual(single["health"]["econ_armed"], False)
            self.assertEqual(single["pooled"]["complete_natural_pairs"], 1)

            err = io.StringIO()
            with contextlib.redirect_stderr(err), contextlib.redirect_stdout(io.StringIO()):
                az.main(["analyze", "--receipts", str(rdir), "--health-dir", str(hdir),
                         "--econ-health-dir", str(edir), "--output", str(tmp / "dual.json")])
            self.assertNotIn("WARNING", err.getvalue())
            dual = json.loads((tmp / "dual.json").read_text(encoding="utf-8"))
            self.assertEqual(dual["health"]["econ_armed"], True)
            self.assertEqual(dual["health"]["econ_policy"], az.ECON_POLICY)
            self.assertEqual(dual["pooled"]["complete_natural_pairs"], 1)


if __name__ == "__main__":
    unittest.main()
