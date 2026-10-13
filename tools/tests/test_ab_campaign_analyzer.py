import json
import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))
import ab_campaign_analyzer as az
import ab_campaign_pilot as pilot


SHA64 = "a" * 64
SHA40 = "b" * 40


def seat(seat_id, team, arm, faction="td_gdi", outcome="won", earned=1000, spent=800, banked=200,
         army_value=500, kills=5, deaths=2):
    return {
        "seat_id": seat_id, "team": team, "arm": arm, "bot_type": "hard", "faction": faction,
        "home": seat_id, "spawn": 0, "proof_source": "generated map.yaml + match records",
        "outcome": outcome,
        "metrics": {"earned": earned, "spent": spent, "banked": banked, "army_value": army_value,
                    "kills": kills, "deaths": deaths, "timeline": []},
    }


def receipt(*, switch="BU_harvester_logistics", setup_id="1v1_nuclear_gdi", pair_index=0, pair_member,
            control_side, treatment_side, winner_team, end_class="NATURAL", end_reason="natural",
            control_seat_kw=None, treatment_seat_kw=None):
    control_seat_kw = control_seat_kw or {}
    treatment_seat_kw = treatment_seat_kw or {}
    seats = [
        seat(f"Multi{control_side}", control_side, "control",
             outcome="won" if winner_team == control_side else "lost", **control_seat_kw),
        seat(f"Multi{treatment_side}", treatment_side, "treatment",
             outcome="won" if winner_team == treatment_side else "lost", **treatment_seat_kw),
    ]
    homes = [s["home"] for s in seats]
    deployed = end_class != "INCOMPLETE"
    return {
        "schema_version": 1, "campaign_id": "fixture", "manifest_sha256": SHA64, "baseline_commit": SHA40,
        "engine_sha256": SHA64, "switch": switch, "setup_id": setup_id,
        # canonical producer fields required by campaign_runner.validate_receipt:
        "cell_id": f"{setup_id}|1337|{pair_member}", "game_uid": f"uid-{pair_index}-{pair_member}" if deployed else None,
        "status": {"NATURAL": "NATURAL_END", "CAP": "CENSORED_CAP", "INCOMPLETE": "INCOMPLETE"}[end_class],
        "seats": seats, "seat_proof_source": "generated_map.yaml", "map_yaml_sha256": SHA64,
        "control_side": control_side, "treatment_side": treatment_side,
        "arm_bot_types": {"control": "hard_control", "treatment": "hard_treatment"},
        "cap_marker": end_class == "CAP", "cap_tick": 45000 if end_class == "CAP" else None,
        "support_complete": deployed, "setup": setup_id, "map_sha256": SHA64,
        "pair": pair_index, "game_in_pair": pair_member, "duration_ticks": 30000,
        "peak_memory_bytes": 0,
        "deployment": {"verified": deployed, "check_tick": 100,
                       "reported_seat_count": len(homes) if deployed else 0,
                       "expected_seat_count": len(homes),
                       "seats": ({h: {"tick": 50, "type": "td_gdi_constructionyard"} for h in homes} if deployed else {}),
                       "missing": []},
        "map": {"name": "m", "path": "p", "sha256": SHA64, "generated_map_yaml_sha256": SHA64, "required_seats": 2},
        "pair_id": f"{setup_id}:1337:{pair_index}", "pair_member": pair_member, "seed": 1337,
        "seed_proof": {"env_value": "1337", "server_log_pin_line": "x", "server_log_sha256": SHA64},
        "teams": {
            control_side: {"arm": "control", "bot_type": "hard_control", "seat_ids": [f"Multi{control_side}"]},
            treatment_side: {"arm": "treatment", "bot_type": "hard_treatment", "seat_ids": [f"Multi{treatment_side}"]},
        },
        "resolved_seats": seats, "end_class": end_class, "end_reason": end_reason,
        "winner_team": winner_team if end_class == "NATURAL" else None,
        "world_tick": 30000, "end_tick": 30000 if deployed else None,
        "cap": {"world_tick_cap": 45000, "marker_observed": end_class == "CAP", "marker_tick": 45000 if end_class == "CAP" else None},
        "memory": {"process_id": 1, "peak_private_bytes": 0, "limit_private_bytes": runner_private_limit(), "samples": []},
        "runtime": {"started_utc": None, "finished_utc": None, "exit_code": 0, "wall_seconds": 1.0, "stall_seconds": 180, "pid_scoped_cleanup": True},
        "artifacts": {k: {"path": None, "sha256": None, "missing_reason": "fixture"} for k in
                      ("server_log", "driver_log", "matches_jsonl", "replay", "map_yaml", "support_bundle")},
    }


def runner_private_limit():
    import campaign_runner as runner
    return runner.PRIVATE_LIMIT


def matched_pair(pair_index=0, treatment_wins=(True, True), **kw):
    """Two games, sides swapped between members, treatment_wins[i] controls who wins game i."""
    r0 = receipt(pair_index=pair_index, pair_member=0, control_side="A", treatment_side="B",
                 winner_team=("B" if treatment_wins[0] else "A"), **kw)
    r1 = receipt(pair_index=pair_index, pair_member=1, control_side="B", treatment_side="A",
                 winner_team=("A" if treatment_wins[1] else "B"), **kw)
    return [r0, r1]


class LoadAndValidate(unittest.TestCase):
    def test_structurally_invalid_receipt_is_kept_not_dropped(self):
        bad = matched_pair()[0]
        del bad["end_class"]
        out = az.validate_and_tag(bad)
        self.assertFalse(out["_valid"])
        self.assertIn("end_class", out["_invalid_reason"])

    def test_non_receipt_json_is_ignored_by_loader(self, tmp_path=pathlib.Path(".")):
        # load_receipts filters on resolved_seats/pair_id presence; verify via summarize path instead.
        result = az.summarize([{"schema_version": 1, "not_a_receipt": True}], None)
        self.assertEqual(result["pooled"]["complete_natural_pairs"], 0)


class Pairing(unittest.TestCase):
    def test_consistent_side_swap_detected(self):
        a, b = matched_pair(treatment_wins=(True, False))
        self.assertTrue(az.side_swap_consistent(a, b))

    def test_broken_side_swap_flagged_invalid(self):
        a, b = matched_pair()
        b["teams"]["A"]["arm"] = "control"
        b["teams"]["B"]["arm"] = "treatment"  # same assignment as `a`, no swap
        verdict = az.classify_pair({0: az.validate_and_tag(a), 1: az.validate_and_tag(b)})
        self.assertEqual(verdict["status"], "INVALID")

    def test_natural_pair_win_diff_treatment_sweep(self):
        a, b = matched_pair(treatment_wins=(True, True))
        verdict = az.classify_pair({0: az.validate_and_tag(a), 1: az.validate_and_tag(b)})
        self.assertEqual(verdict["status"], "NATURAL")
        self.assertEqual(verdict["win_diff"], 1)

    def test_natural_pair_win_diff_split(self):
        a, b = matched_pair(treatment_wins=(True, False))
        verdict = az.classify_pair({0: az.validate_and_tag(a), 1: az.validate_and_tag(b)})
        self.assertEqual(verdict["win_diff"], 0)

    def test_cap_member_without_incomplete_classifies_as_cap(self):
        a, b = matched_pair()
        b["end_class"] = "CAP"; b["winner_team"] = None; b["cap"]["marker_observed"] = True; b["cap"]["marker_tick"] = 45000
        verdict = az.classify_pair({0: az.validate_and_tag(a), 1: az.validate_and_tag(b)})
        self.assertEqual(verdict["status"], "CAP")

    def test_incomplete_member_dominates_cap(self):
        a, b = matched_pair()
        b["end_class"] = "INCOMPLETE"; b["winner_team"] = None; b["end_reason"] = "STALL"
        verdict = az.classify_pair({0: az.validate_and_tag(a), 1: az.validate_and_tag(b)})
        self.assertEqual(verdict["status"], "INCOMPLETE")

    def test_missing_member_is_incomplete(self):
        verdict = az.classify_pair({0: az.validate_and_tag(matched_pair()[0])})
        self.assertEqual(verdict["status"], "INCOMPLETE")

    def test_synthetic_no_receipt_paired_with_a_real_natural_is_incomplete_not_invalid(self):
        """Found by running against a real receipt batch + the real execution
        manifest: a NO_RECEIPT placeholder (materialize_missing) has empty `teams`,
        which must not be run through side_swap_consistent (it would fail and get
        misclassified as a data-integrity INVALID instead of INCOMPLETE)."""
        real = az.validate_and_tag(matched_pair()[0])
        placeholder = {"_valid": True, "_invalid_reason": None, "_synthetic": True,
                        "end_class": az.INCOMPLETE, "end_reason": "NO_RECEIPT",
                        "winner_team": None, "teams": {}, "resolved_seats": []}
        verdict = az.classify_pair({0: real, 1: placeholder})
        self.assertEqual(verdict["status"], "INCOMPLETE")

    def test_two_synthetic_no_receipts_are_incomplete_not_invalid(self):
        placeholder = {"_valid": True, "_invalid_reason": None, "_synthetic": True,
                        "end_class": az.INCOMPLETE, "end_reason": "NO_RECEIPT",
                        "winner_team": None, "teams": {}, "resolved_seats": []}
        verdict = az.classify_pair({0: dict(placeholder), 1: dict(placeholder)})
        self.assertEqual(verdict["status"], "INCOMPLETE")

    def test_duplicate_pair_member_flagged_invalid(self):
        a, b = matched_pair()
        b["pair_member"] = 0
        pairs = az.group_pairs([az.validate_and_tag(a), az.validate_and_tag(b)])
        (key, members), = pairs.items()
        self.assertIn("_duplicate_with", members[0])


class Metrics(unittest.TestCase):
    def test_income_army_metrics_sum_across_games(self):
        games = matched_pair(treatment_wins=(True, False),
                              control_seat_kw={"earned": 100, "spent": 50, "kills": 1, "deaths": 1},
                              treatment_seat_kw={"earned": 200, "spent": 50, "kills": 2, "deaths": 1})
        tagged = [az.validate_and_tag(r) for r in games]
        income = az.income_army_metrics(tagged)
        self.assertEqual(income["control"]["earned"], 200)   # 2 games x 100
        self.assertEqual(income["treatment"]["earned"], 400)  # 2 games x 200
        self.assertAlmostEqual(income["treatment"]["earned_spent_ratio"], 400 / 100)
        self.assertAlmostEqual(income["treatment"]["kill_exchange"], 4 / 2)

    def test_team_metrics_label_synergy_not_identifiable(self):
        games = matched_pair()
        teams = az.team_metrics([az.validate_and_tag(r) for r in games])
        bucket = teams["1v1_nuclear_gdi"]
        self.assertTrue(bucket["control"]["synergy"].startswith("NOT_IDENTIFIABLE_FROM_RECEIPTS"))


class SequentialDecision(unittest.TestCase):
    def _pooled(self, n_pairs, treatment_win_rate):
        wins = round(n_pairs * treatment_win_rate)
        games = []
        for i in range(n_pairs):
            tw = i < wins
            games += matched_pair(pair_index=i, treatment_wins=(tw, tw))
        return games

    def test_early_accept_at_75_percent_with_neutral_economy(self):
        games = self._pooled(16, 0.8125)  # 13/16 ~ 0.8125 >= 0.75
        result = az.summarize(games, None)
        self.assertEqual(result["sequential_decision"]["verdict"], "ACCEPT")

    def test_early_reject_at_25_percent(self):
        games = self._pooled(16, 0.1875)  # 3/16
        result = az.summarize(games, None)
        self.assertEqual(result["sequential_decision"]["verdict"], "REJECT")

    def test_mid_share_continues_before_round_cap(self):
        games = self._pooled(16, 0.5)
        result = az.summarize(games, None)
        self.assertEqual(result["sequential_decision"]["verdict"], "CONTINUE")

    def test_crash_forces_reject_regardless_of_wins(self):
        games = self._pooled(16, 1.0)
        games[0]["end_class"] = "INCOMPLETE"
        games[0]["end_reason"] = "CRASH"
        games[0]["winner_team"] = None
        result = az.summarize(games, None)
        self.assertEqual(result["sequential_decision"]["verdict"], "REJECT")


class HarmVeto(unittest.TestCase):
    """Lead ruling 2026-10-11: a per-setup CI entirely below zero overrides a pooled
    ACCEPT, even when the pooled signal alone would clear the 75% bar. Devin-Reviewer
    finding (exact-SHA review of 497d737f6): a 1-pair setup's bootstrap CI is
    degenerate (every resample is that one value), so it must not be veto-eligible —
    see ANALYZER_POLICY_DEFAULTS."""

    def test_harmed_setup_downgrades_pooled_accept_to_park(self):
        good = []
        for i in range(12):
            good += matched_pair(setup_id="1v1_nuclear_gdi", pair_index=i, treatment_wins=(True, True))
        harmed = []
        for i in range(az.ANALYZER_POLICY_DEFAULTS["min_setup_veto_sample"]):
            harmed += matched_pair(setup_id="1v1_nuclear_nod", pair_index=i, treatment_wins=(False, False))
        result = az.summarize(good + harmed, None)
        self.assertEqual(result["sequential_decision"]["pre_veto_verdict"], "ACCEPT")
        self.assertEqual(result["sequential_decision"]["harm_veto_setups"], ["1v1_nuclear_nod"])
        self.assertEqual(result["sequential_decision"]["verdict"], "PARK")

    def test_manifest_acceptance_thresholds_override_the_sample_floor(self):
        """Lead ruling 2026-10-11: the floor must be a frozen manifest/policy value,
        not hard-coded, so a change shows up in the manifest's policy hash."""
        good = []
        for i in range(14):
            good += matched_pair(setup_id="1v1_nuclear_gdi", pair_index=i, treatment_wins=(True, True))
        two_pair_sweep = []
        for i in range(2):
            two_pair_sweep += matched_pair(setup_id="1v1_nuclear_nod", pair_index=i, treatment_wins=(False, False))
        manifest = {"acceptance_thresholds": {**pilot.DEFAULT_THRESHOLDS, "min_setup_veto_sample": 2}}
        result = az.summarize(good + two_pair_sweep, manifest)
        self.assertEqual(result["sequential_decision"]["harm_veto_min_sample"], 2)
        self.assertEqual(result["sequential_decision"]["harm_veto_setups"], ["1v1_nuclear_nod"])
        self.assertEqual(result["sequential_decision"]["verdict"], "PARK")

    def test_manifest_without_the_sample_floor_is_rejected(self):
        """Devin-Reviewer finding (re-review of 8272b271b): a supplied manifest whose
        acceptance_thresholds omits min_setup_veto_sample must fail closed, not
        silently default — otherwise the manifest SHA doesn't bind the veto threshold
        actually used."""
        games = matched_pair()
        manifest_missing_field = {"acceptance_thresholds": {
            k: v for k, v in pilot.DEFAULT_THRESHOLDS.items() if k != "min_setup_veto_sample"}}
        with self.assertRaises(ValueError):
            az.summarize(games, manifest_missing_field)
        manifest_wrong_type = {"acceptance_thresholds": {**pilot.DEFAULT_THRESHOLDS, "min_setup_veto_sample": "4"}}
        with self.assertRaises(ValueError):
            az.summarize(games, manifest_wrong_type)
        manifest_non_positive = {"acceptance_thresholds": {**pilot.DEFAULT_THRESHOLDS, "min_setup_veto_sample": 0}}
        with self.assertRaises(ValueError):
            az.summarize(games, manifest_non_positive)

    def test_single_pair_setup_is_not_veto_eligible(self):
        """Devin-Reviewer's exact regression request: one noisy pair must not veto."""
        good = []
        for i in range(15):
            good += matched_pair(setup_id="1v1_nuclear_gdi", pair_index=i, treatment_wins=(True, True))
        one_pair_sweep = matched_pair(setup_id="1v1_nuclear_nod", pair_index=0, treatment_wins=(False, False))
        result = az.summarize(good + one_pair_sweep, None)
        self.assertEqual(result["sequential_decision"]["harm_veto_setups"], [])
        self.assertEqual(result["sequential_decision"]["harm_veto_ineligible_setups"], ["1v1_nuclear_nod"])
        self.assertEqual(result["sequential_decision"]["pre_veto_verdict"], "ACCEPT")
        self.assertEqual(result["sequential_decision"]["verdict"], "ACCEPT")

    def test_harm_veto_never_upgrades_a_reject(self):
        bad = []
        for i in range(16):
            bad += matched_pair(setup_id="1v1_nuclear_gdi", pair_index=i, treatment_wins=(False, False))
        result = az.summarize(bad, None)
        self.assertEqual(result["sequential_decision"]["pre_veto_verdict"], "REJECT")
        self.assertEqual(result["sequential_decision"]["verdict"], "REJECT")

    def test_no_harmed_setup_leaves_accept_untouched(self):
        games = []
        for i in range(16):
            games += matched_pair(setup_id="1v1_nuclear_gdi", pair_index=i, treatment_wins=(i < 13, i < 13))
        result = az.summarize(games, None)
        self.assertEqual(result["sequential_decision"]["harm_veto_setups"], [])
        self.assertEqual(result["sequential_decision"]["verdict"], "ACCEPT")


class PerSetupDenominators(unittest.TestCase):
    def test_every_pooled_pair_attributed_to_its_setup(self):
        a = matched_pair(setup_id="1v1_nuclear_gdi")
        b = matched_pair(setup_id="1v1_satans_allies", pair_index=1)
        result = az.summarize(a + b, None)
        self.assertEqual(set(result["setups"]), {"1v1_nuclear_gdi", "1v1_satans_allies"})
        self.assertEqual(result["setups"]["1v1_nuclear_gdi"]["natural"], 1)
        self.assertEqual(result["setups"]["1v1_satans_allies"]["natural"], 1)


class InvalidReceiptsNeverDropped(unittest.TestCase):
    def test_invalid_receipt_counted_and_listed(self):
        bad = matched_pair()[0]
        del bad["end_class"]
        bad["_path"] = "fixture/bad.json"
        result = az.summarize([bad], None)
        self.assertEqual(result["invalid_receipt_count"], 1)
        self.assertEqual(result["invalid_receipts"][0]["path"], "fixture/bad.json")


if __name__ == "__main__":
    unittest.main()
