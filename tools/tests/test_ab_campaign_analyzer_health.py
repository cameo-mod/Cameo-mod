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
            self.assertNotIn("WARNING", err.getvalue())
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


if __name__ == "__main__":
    unittest.main()
