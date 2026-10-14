"""SENTINEL-RUNNER-POLICY (rev6.1): capture provenance + consumer quarantine tests.

Every supported ingest resolves provenance: a positive binding requires the
actual artifact SHA plus complete UID membership; missing/malformed/
conflicting provenance hard-rejects; a sentinel-bound artifact is skipped,
never scoreable. Synthetic fixtures only — no engine, no game runs.
"""
import hashlib
import json
import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/ai"))
sys.path.insert(0, str(ROOT / "tools/tests"))

import ab_campaign
import ab_campaign_analyzer as az
import ab_campaign_pilot
import ab_increment
import ab_summary
import aggregate_ai_matches
import anonymize_legacy_logs
import army_mix_report
import campaign_runner
import capture_provenance as prov
import fit_fight_learning
import insurance_summary
import playtest_stats_from_logs
import run_ai_match_batch
import run_league
import tune_plan_bandits

UID = "uid-prov-1"


def match_row(uid=UID, seat="seat_0", bot="hard", outcome="won"):
    """Schema-3 cameo-ai-matches record good enough for every consumer."""
    return {"schema": 3, "record_id": f"{uid}|{seat}", "game_uid": uid,
            "duration_ticks": 3000, "timestep": 1,
            "player": {"seat": seat, "faction": "td_gdi", "bot_type": bot,
                       "handicap": 0, "outcome": outcome, "personality": "steady",
                       "home": "1,1"},
            "stats": {"kills_cost": 10, "deaths_cost": 5},
            "opponents": [{"seat": "seat_1", "faction": "td_nod", "outcome": "lost",
                           "bot_type": "classic", "handicap": 0}],
            "allies": []}


def batch_row(uid=UID):
    """One batch_results.jsonl row (result-level, carries bot_outcomes)."""
    return {"variant": "v", "status": "ok", "health_status": "OBSERVED_HEALTHY",
            "fingerprint": "fp1", "records": 2,
            "bot_outcomes": [
                {"record_id": f"{uid}|seat_0", "bot_type": "hard", "outcome": "won",
                 "spawn": 0, "opponent": {"bot_type": "classic"}},
                {"record_id": f"{uid}|seat_1", "bot_type": "classic", "outcome": "lost",
                 "spawn": 1, "opponent": {"bot_type": "hard"}}]}


def sentinel_batch_row(uid=UID):
    row = batch_row(uid)
    row["capture_kind"] = "sentinel_diagnostic"
    row["scoreable"] = False
    return row


def write_jsonl(path, rows):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("".join(json.dumps(r) + "\n" for r in rows), encoding="utf-8")


def bound_support(rows=None, uids=(UID,), marked=False):
    """Support dir: Logs/cameo-ai-matches.jsonl + provenance manifest binding it."""
    rows = rows if rows is not None else [match_row(), match_row(seat="seat_1", bot="classic", outcome="lost")]
    support = pathlib.Path(tempfile.mkdtemp(prefix="prov_support_"))
    write_jsonl(support / "Logs" / "cameo-ai-matches.jsonl", rows)
    prov.write_provenance(
        support,
        capture_kind="sentinel_diagnostic" if marked else "match_batch",
        scoreable=not marked, game_uids=list(uids))
    return support


class ProvenanceCoreTests(unittest.TestCase):
    def test_bound_file_allows(self):
        support = bound_support()
        verdict, _why = prov.assess_file(support / "Logs" / "cameo-ai-matches.jsonl")
        self.assertEqual(verdict, prov.ALLOW)

    def test_sentinel_manifest_marks_file(self):
        support = bound_support(marked=True)
        verdict, _why = prov.assess_file(support / "Logs" / "cameo-ai-matches.jsonl")
        self.assertEqual(verdict, prov.SENTINEL)

    def test_detached_bare_file_rejects(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_bare_"))
        f = tmp / "cameo-ai-matches.jsonl"
        write_jsonl(f, [match_row()])
        verdict, why = prov.assess_file(f)
        self.assertEqual(verdict, prov.REJECT)
        self.assertIn("no resolvable association", why)

    def test_sha_mismatch_rejects(self):
        support = bound_support()
        f = support / "Logs" / "cameo-ai-matches.jsonl"
        f.write_text(f.read_text() + " ", encoding="utf-8")  # perturb bytes
        verdict, why = prov.assess_file(f)
        self.assertEqual(verdict, prov.REJECT)
        self.assertIn("conflicting", why)

    def test_malformed_manifest_rejects(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_bad_"))
        f = tmp / "x.jsonl"
        write_jsonl(f, [match_row()])
        (tmp / "capture_provenance.json").write_text("{not json", encoding="utf-8")
        verdict, _why = prov.assess_file(f)
        self.assertEqual(verdict, prov.REJECT)

    def test_manifest_missing_uid_membership_rejects(self):
        # Manifest binds the artifact bytes but not the UID it carries.
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_uid_"))
        f = tmp / "Logs" / "cameo-ai-matches.jsonl"
        write_jsonl(f, [match_row(uid="uid-A"), match_row(uid="uid-B", seat="seat_1")])
        prov.write_provenance(tmp, capture_kind="match_batch", scoreable=True,
                              game_uids=["uid-A"])
        verdict, why = prov.assess_file(f)
        self.assertEqual(verdict, prov.REJECT)
        self.assertIn("not bound", why)

    def test_mixed_uid_fully_bound_allows(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_mix_"))
        f = tmp / "Logs" / "cameo-ai-matches.jsonl"
        write_jsonl(f, [match_row(uid="uid-A"), match_row(uid="uid-B", seat="seat_1")])
        prov.write_provenance(tmp, capture_kind="match_batch", scoreable=True,
                              game_uids=["uid-A", "uid-B"])
        verdict, _why = prov.assess_file(f)
        self.assertEqual(verdict, prov.ALLOW)

    def test_legacy_manifest_allows(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_legacy_"))
        f = tmp / "old" / "cameo-ai-matches.jsonl"
        write_jsonl(f, [match_row()])
        prov.write_provenance(tmp / "old", capture_kind="legacy_import",
                              scoreable=True, game_uids=[UID])
        verdict, _why = prov.assess_file(f)
        self.assertEqual(verdict, prov.ALLOW)

    def test_promoted_sibling_marks(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_promo_"))
        dest = tmp / "match-0001-cameo-ai-insurance.jsonl"
        write_jsonl(dest, [{"game_uid": UID}])
        prov.write_promoted_sibling(dest, game_uid=UID)
        verdict, _why = prov.assess_file(dest)
        self.assertEqual(verdict, prov.SENTINEL)

    def test_copy_stripped_sibling_rejects(self):
        # Copy the artifact WITHOUT its sibling: the copy is detached.
        src = pathlib.Path(tempfile.mkdtemp(prefix="prov_src_"))
        dest_dir = pathlib.Path(tempfile.mkdtemp(prefix="prov_dst_"))
        dest = src / "x.jsonl"
        write_jsonl(dest, [{"game_uid": UID}])
        prov.write_promoted_sibling(dest, game_uid=UID)
        import shutil
        stripped = dest_dir / "x.jsonl"
        shutil.copy2(dest, stripped)
        verdict, _why = prov.assess_file(stripped)
        self.assertEqual(verdict, prov.REJECT)

    def test_marked_ancestor_summary_quarantines_descendant(self):
        support = pathlib.Path(tempfile.mkdtemp(prefix="prov_summ_"))
        write_jsonl(support / "batch_results.jsonl", [sentinel_batch_row()])
        (support / "batch_summary.json").write_text(json.dumps(
            {"capture_kind": "sentinel_diagnostic", "scoreable": False,
             "sentinel_uids": [UID], "results": []}), encoding="utf-8")
        f = support / "deep" / "cameo-ai-matches.jsonl"
        write_jsonl(f, [match_row()])
        verdict, _why = prov.assess_file(f)
        self.assertEqual(verdict, prov.SENTINEL)

    def test_marker_free_summary_is_not_positive_binding(self):
        support = pathlib.Path(tempfile.mkdtemp(prefix="prov_clean_"))
        (support / "batch_summary.json").write_text(json.dumps(
            {"results": [], "completed": 1}), encoding="utf-8")
        f = support / "Logs" / "cameo-ai-matches.jsonl"
        write_jsonl(f, [match_row()])
        verdict, _why = prov.assess_file(f)
        self.assertEqual(verdict, prov.REJECT)

    def test_marked_batch_results_row_scopes_by_uid(self):
        support = pathlib.Path(tempfile.mkdtemp(prefix="prov_row_"))
        write_jsonl(support / "batch_results.jsonl",
                    [sentinel_batch_row(), batch_row(uid="uid-other")])
        hit = support / "Logs" / "cameo-ai-matches.jsonl"
        write_jsonl(hit, [match_row()])
        self.assertEqual(prov.assess_file(hit)[0], prov.SENTINEL)
        other = support / "Logs" / "other.jsonl"
        write_jsonl(other, [{"game_uid": "uid-other", "record_id": "uid-other|seat_0"}])
        self.assertEqual(prov.assess_file(other)[0], prov.REJECT)

    def test_support_root_sidecar_covers_logs_dir(self):
        # Sol #2: the sidecar resolves at the SUPPORT ROOT, not the file's dir.
        support = pathlib.Path(tempfile.mkdtemp(prefix="prov_root_"))
        f = support / "Logs" / "deep" / "cameo-ai-matches.jsonl"
        write_jsonl(f, [match_row()])
        prov.write_provenance(support, capture_kind="sentinel_diagnostic",
                              scoreable=False, game_uids=[UID])
        self.assertEqual(prov.assess_file(f)[0], prov.SENTINEL)


class AggregateSinkTests(unittest.TestCase):
    def test_sentinel_file_skipped(self):
        support = bound_support(marked=True)
        skips = aggregate_ai_matches.Skips()
        records = aggregate_ai_matches.load_records(
            [support / "Logs" / "cameo-ai-matches.jsonl"], skips)
        self.assertEqual(records, [])
        self.assertTrue(any("sentinel" in k for k in skips.counts))

    def test_unbound_file_hard_errors(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_agg_"))
        f = tmp / "cameo-ai-matches.jsonl"
        write_jsonl(f, [match_row()])
        with self.assertRaises(ValueError):
            aggregate_ai_matches.load_records([f], aggregate_ai_matches.Skips())

    def test_bound_file_loads(self):
        support = bound_support()
        skips = aggregate_ai_matches.Skips()
        records = aggregate_ai_matches.load_records(
            [support / "Logs" / "cameo-ai-matches.jsonl"], skips)
        self.assertEqual(len(records), 2)


class AbSummarySinkTests(unittest.TestCase):
    def test_records_skips_sentinel(self):
        support = bound_support(marked=True)
        self.assertEqual(list(ab_summary.records([str(support)])), [])

    def test_records_rejects_unbound(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_sum_"))
        write_jsonl(tmp / "cameo-ai-matches.jsonl", [match_row()])
        with self.assertRaises(ValueError):
            list(ab_summary.records([str(tmp)]))

    def test_records_direct_file_arg(self):
        support = bound_support()
        f = support / "Logs" / "cameo-ai-matches.jsonl"
        self.assertEqual(len(list(ab_summary.records([str(f)]))), 2)

    def test_arm_fingerprints_skips_marked_rows(self):
        support = bound_support()
        write_jsonl(support / "batch_results.jsonl",
                    [batch_row(), sentinel_batch_row(uid="uid-2")])
        prov.write_provenance(support, capture_kind="match_batch",
                              scoreable=True, game_uids=[UID, "uid-2"])
        arms = {fp: e for _f, arms0 in ab_summary.arm_fingerprints([str(support)])
                for fp, e in arms0.items()}
        self.assertIn("fp1", arms)
        self.assertEqual(arms["fp1"]["results"], 1)  # sentinel row never counted

    def test_arm_fingerprints_sentinel_file_skipped(self):
        support = bound_support()
        write_jsonl(support / "batch_results.jsonl", [sentinel_batch_row()])
        # Manifest must be written AFTER the artifact it binds.
        prov.write_provenance(support, capture_kind="sentinel_diagnostic",
                              scoreable=False, game_uids=[UID])
        self.assertEqual(list(ab_summary.arm_fingerprints([str(support)])), [])


class LeagueSinkTests(unittest.TestCase):
    def make_cell(self, marked_summary=False, marked_manifest=False):
        cell = pathlib.Path(tempfile.mkdtemp(prefix="prov_cell_"))
        summary = {"completed": 1, "stalled": 0, "timed_out": 0, "died": 0,
                   "norecord": 0, "unknown": 0, "new_exceptions": [], "aborted": None,
                   "results": []}
        if marked_summary:
            summary["capture_kind"] = "sentinel_diagnostic"
            summary["scoreable"] = False
        (cell / "batch_summary.json").write_text(json.dumps(summary), encoding="utf-8")
        write_jsonl(cell / "batch_results.jsonl",
                    [sentinel_batch_row() if marked_summary else batch_row()])
        prov.write_provenance(
            cell,
            capture_kind="sentinel_diagnostic" if marked_manifest else "match_batch",
            scoreable=not marked_manifest, game_uids=[UID])
        return cell

    def acc(self):
        return {"won": 0, "lost": 0, "spawn": {}, "per_member": {},
                "completed": 0, "stalled": 0, "timed_out": 0, "died": 0,
                "norecord": 0, "unknown": 0, "exceptions": [],
                "cells_missing": [], "cells_aborted": []}

    def test_marked_summary_cell_contributes_nothing(self):
        cell_dir = self.make_cell(marked_summary=True, marked_manifest=True)
        acc = self.acc()
        run_league.merge_cell(cell_dir, {"name": "c"}, "hard", "classic", acc)
        self.assertEqual(acc["won"] + acc["lost"] + acc["completed"], 0)

    def test_sentinel_manifest_cell_contributes_nothing(self):
        cell_dir = self.make_cell(marked_manifest=True)
        acc = self.acc()
        run_league.merge_cell(cell_dir, {"name": "c"}, "hard", "classic", acc)
        self.assertEqual(acc["won"] + acc["lost"] + acc["completed"], 0)

    def test_unbound_cell_hard_errors(self):
        cell_dir = pathlib.Path(tempfile.mkdtemp(prefix="prov_cellbare_"))
        (cell_dir / "batch_summary.json").write_text(json.dumps(
            {"completed": 1, "results": []}), encoding="utf-8")
        with self.assertRaises(ValueError):
            run_league.merge_cell(cell_dir, {"name": "c"}, "hard", "classic", self.acc())

    def test_bound_cell_scores(self):
        cell_dir = self.make_cell()
        acc = self.acc()
        run_league.merge_cell(cell_dir, {"name": "c"}, "hard", "classic", acc)
        self.assertEqual(acc["won"], 1)
        self.assertEqual(acc["completed"], 1)


class AbIncrementSinkTests(unittest.TestCase):
    def shard(self, marked=False):
        support = pathlib.Path(tempfile.mkdtemp(prefix="prov_shard_"))
        write_jsonl(support / "batch_results.jsonl",
                    [sentinel_batch_row() if marked else batch_row()])
        s = ab_increment.Shard(arm="a", faction="td_gdi", kind="full", planned=1,
                               support=support, argv=[], cwd=support, label="s")
        return s

    def test_shard_progress_skips_sentinel(self):
        s = self.shard(marked=True)
        prov.write_provenance(s.support, capture_kind="sentinel_diagnostic",
                              scoreable=False, game_uids=[UID])
        self.assertEqual(ab_increment.shard_progress(s, "hard"), (0, 0))

    def test_shard_progress_unbound_hard_errors(self):
        s = self.shard()
        with self.assertRaises(ValueError):
            ab_increment.shard_progress(s, "hard")

    def test_shard_progress_bound_counts(self):
        s = self.shard()
        prov.write_provenance(s.support, capture_kind="match_batch",
                              scoreable=True, game_uids=[UID])
        self.assertEqual(ab_increment.shard_progress(s, "hard"), (1, 1))

    def test_shard_fingerprints_skips_marked_rows(self):
        s = self.shard()
        write_jsonl(s.support / "batch_results.jsonl",
                    [batch_row(), sentinel_batch_row(uid="uid-2")])
        prov.write_provenance(s.support, capture_kind="match_batch",
                              scoreable=True, game_uids=[UID, "uid-2"])
        self.assertEqual(ab_increment.shard_fingerprints(s), ["fp1"])

    def test_iter_match_records_skips_sentinel(self):
        support = bound_support(marked=True)
        self.assertEqual(list(ab_increment.iter_match_records([support])), [])

    def test_iter_match_records_rejects_unbound(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_iter_"))
        write_jsonl(tmp / "Logs" / "cameo-ai-matches.jsonl", [match_row()])
        with self.assertRaises(ValueError):
            list(ab_increment.iter_match_records([tmp]))

    def test_smoke_verified_fails_on_sentinel(self):
        support = bound_support(marked=True)
        write_jsonl(support / "batch_results.jsonl", [sentinel_batch_row()])
        # Rebind: manifest must cover batch_results.jsonl bytes too.
        prov.write_provenance(support, capture_kind="sentinel_diagnostic",
                              scoreable=False, game_uids=[UID])
        ok, detail = ab_increment.smoke_verified(support)
        self.assertFalse(ok)
        self.assertIn("sentinel", detail)


class InsuranceSinkTests(unittest.TestCase):
    def test_sentinel_insurance_file_absent(self):
        support = pathlib.Path(tempfile.mkdtemp(prefix="prov_ins_"))
        write_jsonl(support / "Logs" / "cameo-ai-insurance-a.jsonl",
                    [{"status": "RECORDED", "game_uid": UID, "slot": 0}])
        prov.write_provenance(support, capture_kind="sentinel_diagnostic",
                              scoreable=False, game_uids=[UID])
        out = list(insurance_summary.summaries([support]))
        self.assertEqual(out, [{"status": "UNKNOWN",
                                "path": "insurance telemetry absent"}])

    def test_unbound_insurance_raises(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_insbare_"))
        write_jsonl(tmp / "cameo-ai-insurance-b.jsonl", [{"status": "RECORDED"}])
        with self.assertRaises(ValueError):
            list(insurance_summary.summaries([tmp]))

    def test_bound_insurance_reads_file(self):
        support = pathlib.Path(tempfile.mkdtemp(prefix="prov_insok_"))
        ins = support / "cameo-ai-insurance-c.jsonl"
        write_jsonl(ins, [{"status": "RECORDED", "game_uid": UID, "slot": 0}])
        prov.write_provenance(support, capture_kind="match_batch",
                              scoreable=True, game_uids=[UID])
        out = list(insurance_summary.summaries([support]))
        # The file IS consumed (strict row validation yields the file's own
        # UNKNOWN row, not the absent-telemetry marker).
        self.assertEqual(len(out), 1)
        self.assertIn("cameo-ai-insurance-c.jsonl", out[0]["path"])


class CampaignSinkTests(unittest.TestCase):
    def test_load_jsonl_skips_sentinel(self):
        support = bound_support(marked=True)
        self.assertEqual(ab_campaign._load_jsonl([support]), [])

    def test_load_jsonl_rejects_unbound(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_camp_"))
        write_jsonl(tmp / "cameo-ai-matches.jsonl", [match_row()])
        with self.assertRaises(ValueError):
            ab_campaign._load_jsonl([tmp])

    def test_classify_rejects_marked_receipt(self):
        receipt = {"capture_kind": "sentinel_diagnostic", "scoreable": False}
        verdict, _why = ab_campaign.classify_game([match_row()], receipt)
        self.assertEqual(verdict, "INVALID_UNKNOWN")

    def test_classify_rejects_either_marker_alone(self):
        for receipt in ({"capture_kind": "sentinel_diagnostic"},
                        {"scoreable": False}):
            verdict, _why = ab_campaign.classify_game([match_row()], receipt)
            self.assertEqual(verdict, "INVALID_UNKNOWN")

    def test_classify_rejects_provenance_uid_mismatch(self):
        receipt = {"capture_kind": "match_batch", "scoreable": True,
                   "capture_provenance": {"capture_kind": "match_batch",
                                          "scoreable": True,
                                          "game_uids": ["uid-other"]}}
        verdict, _why = ab_campaign.classify_game([match_row()], receipt)
        self.assertEqual(verdict, "INVALID_UNKNOWN")

    def test_load_receipts_skips_sentinel_bound(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_rcpt_"))
        r = {"game_uid": UID, "capture_kind": "sentinel_diagnostic",
             "scoreable": False}
        (tmp / "cell_receipt.json").write_text(json.dumps(r), encoding="utf-8")
        prov.write_provenance(tmp, capture_kind="sentinel_diagnostic",
                              scoreable=False, game_uids=[UID])
        self.assertEqual(ab_campaign.load_receipts([tmp]), {})


class AnalyzerSinkTests(unittest.TestCase):
    def test_marked_receipt_is_invalid(self):
        from test_ab_campaign_analyzer import receipt
        r = receipt(pair_member=0, control_side="A", treatment_side="B",
                    winner_team="B")
        r["capture_kind"] = "sentinel_diagnostic"
        r["scoreable"] = False
        tagged = az.validate_and_tag(r)
        self.assertFalse(tagged["_valid"])
        self.assertIn("sentinel", tagged["_invalid_reason"])

    def test_scoreable_false_alone_is_invalid(self):
        from test_ab_campaign_analyzer import receipt
        r = receipt(pair_member=0, control_side="A", treatment_side="B",
                    winner_team="B")
        r["scoreable"] = False
        self.assertFalse(az.validate_and_tag(r)["_valid"])

    def test_consistent_provenance_stays_valid(self):
        from test_ab_campaign_analyzer import receipt
        r = receipt(pair_member=0, control_side="A", treatment_side="B",
                    winner_team="B")
        r["capture_kind"] = "match_batch"
        r["scoreable"] = True
        r["capture_provenance"] = {"capture_kind": "match_batch", "scoreable": True,
                                   "game_uids": [r["game_uid"]],
                                   "artifacts": {}}
        self.assertTrue(az.validate_and_tag(r)["_valid"])

    def test_provenance_uid_mismatch_is_invalid(self):
        from test_ab_campaign_analyzer import receipt
        r = receipt(pair_member=0, control_side="A", treatment_side="B",
                    winner_team="B")
        r["capture_provenance"] = {"capture_kind": "match_batch", "scoreable": True,
                                   "game_uids": ["uid-other"], "artifacts": {}}
        tagged = az.validate_and_tag(r)
        self.assertFalse(tagged["_valid"])
        self.assertIn("capture provenance", tagged["_invalid_reason"])

    def test_loader_skips_unmarked_receipt_in_sentinel_dir(self):
        # An unmarked receipt inside a sentinel-bound tree is quarantined at
        # load even though the receipt itself carries no markers.
        from test_ab_campaign_analyzer import receipt
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_anl_"))
        r = receipt(pair_member=0, control_side="A", treatment_side="B",
                    winner_team="B")
        (tmp / "cell_receipt.json").write_text(json.dumps(r), encoding="utf-8")
        prov.write_provenance(tmp, capture_kind="sentinel_diagnostic",
                              scoreable=False, game_uids=[r["game_uid"]])
        self.assertEqual(az.load_receipts([tmp]), [])

    def test_loader_accepts_unbound_normal_receipt(self):
        # Receipts are self-describing: no manifest in a normal campaign dir.
        from test_ab_campaign_analyzer import receipt
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_anlok_"))
        r = receipt(pair_member=0, control_side="A", treatment_side="B",
                    winner_team="B")
        (tmp / "cell_receipt.json").write_text(json.dumps(r), encoding="utf-8")
        self.assertEqual(len(az.load_receipts([tmp])), 1)

    def test_loader_raises_on_corrupt_provenance(self):
        from test_ab_campaign_analyzer import receipt
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_anlbad_"))
        r = receipt(pair_member=0, control_side="A", treatment_side="B",
                    winner_team="B")
        (tmp / "cell_receipt.json").write_text(json.dumps(r), encoding="utf-8")
        (tmp / "capture_provenance.json").write_text("{corrupt", encoding="utf-8")
        with self.assertRaises(ValueError):
            az.load_receipts([tmp])

    def _health_report(self, uid, seat, status, findings):
        return {"policy": az.HEALTH_POLICY, "game_uid": uid, "player": seat,
                "status": status, "findings": findings}

    def test_unattributed_missing_situations_stays_unknown(self):
        # The seat bridge cannot attribute the report -> the exception must
        # NOT fire; the verdict stays UNKNOWN and the game is excluded.
        from test_ab_campaign_analyzer import matched_pair
        games = matched_pair()
        health = {"games": {
            "uid-0-0": {"seat_0": [self._health_report("uid-0-0", "seat_0", "UNKNOWN",
                        [{"code": "MISSING_SITUATIONS", "severity": "DEFECT"}])],
                        "seat_1": [self._health_report("uid-0-0", "seat_1",
                                                       "OBSERVED_HEALTHY", [])]},
            "uid-0-1": {"seat_0": [self._health_report("uid-0-1", "seat_0",
                                                       "OBSERVED_HEALTHY", [])],
                        "seat_1": [self._health_report("uid-0-1", "seat_1",
                                                       "OBSERVED_HEALTHY", [])]}},
            "errors": []}
        res = az.summarize(games, None, health)
        excluded = {g["game_uid"]: g["verdict"] for g in res["health"]["excluded_games"]}
        self.assertEqual(excluded["uid-0-0"], "UNKNOWN")
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)

    def test_classic_seat_missing_situations_becomes_coverage_diagnostic(self):
        # Granted exception: bridged classic seat + sole MISSING_SITUATIONS
        # relabels to COVERAGE_DIAGNOSTIC at the verdict layer — still
        # excluded, never pooled; the raw report is unchanged.
        from test_ab_campaign_analyzer import receipt
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_cov_"))
        log = tmp / "cameo-ai-matches.jsonl"
        write_jsonl(log, [
            {"game_uid": "uid-0-0", "player": {"seat": "seat_0", "home": "h0",
                                               "faction": "td_gdi", "bot_type": "classic"}},
            {"game_uid": "uid-0-0", "player": {"seat": "seat_1", "home": "h1",
                                               "faction": "td_gdi", "bot_type": "hard"}}])
        r0 = receipt(pair_member=0, control_side="A", treatment_side="B",
                     winner_team="B")
        r0["resolved_seats"] = [dict(s) for s in r0["seats"]]  # decouple: seats is free-form, resolved_seats is strict
        r0["seats"][0]["home_location"] = "h0"
        r0["seats"][0]["bot_type"] = "classic"
        r0["seats"][1]["home_location"] = "h1"
        r0["artifacts"]["matches_jsonl"] = {
            "path": str(log), "missing_reason": None,
            "sha256": hashlib.sha256(log.read_bytes()).hexdigest()}
        r1 = receipt(pair_member=1, control_side="B", treatment_side="A",
                     winner_team="A")
        health = {"games": {
            "uid-0-0": {"seat_0": [self._health_report("uid-0-0", "seat_0", "UNKNOWN",
                        [{"code": "MISSING_SITUATIONS", "severity": "DEFECT"}])],
                        "seat_1": [self._health_report("uid-0-0", "seat_1",
                                                       "OBSERVED_HEALTHY", [])]},
            "uid-0-1": {"seat_0": [self._health_report("uid-0-1", "seat_0",
                                                       "OBSERVED_HEALTHY", [])],
                        "seat_1": [self._health_report("uid-0-1", "seat_1",
                                                       "OBSERVED_HEALTHY", [])]}},
            "errors": []}
        res = az.summarize([r0, r1], None, health)
        excluded = {g["game_uid"]: g["verdict"] for g in res["health"]["excluded_games"]}
        self.assertEqual(excluded["uid-0-0"], "COVERAGE_DIAGNOSTIC")
        self.assertEqual(res["pooled"]["complete_natural_pairs"], 0)


class CampaignRunnerTests(unittest.TestCase):
    def cell(self, marked=False, uid=UID):
        root = pathlib.Path(tempfile.mkdtemp(prefix="prov_cellroot_"))
        cell = root / "cell_0"
        support = cell / "Support" / "Logs"
        write_jsonl(support / "cameo-ai-matches.jsonl", [match_row(uid=uid)])
        if marked is not None:
            prov.write_provenance(
                cell,
                capture_kind="sentinel_diagnostic" if marked else "match_batch",
                scoreable=not marked, game_uids=[uid])
        return root, cell, cell / "Support", support / "cameo-ai-matches.jsonl"

    def test_unmarked_cell_returns_none(self):
        root, cell, support, log = self.cell(marked=None)
        kind, doc = campaign_runner._cell_capture(cell, support, root, log, UID)
        self.assertIsNone(kind)
        self.assertIsNone(doc)

    def test_sentinel_cell_returns_marked(self):
        root, cell, support, log = self.cell(marked=True)
        kind, doc = campaign_runner._cell_capture(cell, support, root, log, UID)
        self.assertEqual(kind, "sentinel_diagnostic")
        self.assertFalse(doc["scoreable"])

    def test_scoreable_binding_returns_match_batch(self):
        root, cell, support, log = self.cell(marked=False)
        kind, doc = campaign_runner._cell_capture(cell, support, root, log, UID)
        self.assertEqual(kind, "match_batch")
        self.assertTrue(doc["scoreable"])

    def test_sha_mismatch_raises(self):
        root, cell, support, log = self.cell(marked=False)
        log.write_text(log.read_text() + " ", encoding="utf-8")
        with self.assertRaises(ValueError):
            campaign_runner._cell_capture(cell, support, root, log, UID)

    def test_conflicting_manifests_raise(self):
        root, cell, support, log = self.cell(marked=False)
        # A second sentinel manifest covering the same cell contradicts the
        # scoreable one -> fail closed, no receipt.
        prov.write_provenance(cell / "Support",
                              capture_kind="sentinel_diagnostic", scoreable=False,
                              game_uids=[UID])
        with self.assertRaises(ValueError):
            campaign_runner._cell_capture(cell, support, root, log, UID)

    def test_malformed_manifest_raises(self):
        root, cell, support, log = self.cell(marked=None)
        (cell / "capture_provenance.json").write_text("{bad", encoding="utf-8")
        with self.assertRaises(ValueError):
            campaign_runner._cell_capture(cell, support, root, log, UID)


class BundleProvenanceTests(unittest.TestCase):
    """Addendum §E: the support_bundle carries its own fail-closed binding."""

    def _zipped_tree(self):
        root = pathlib.Path(tempfile.mkdtemp(prefix="prov_bundle_"))
        f = root / "Logs" / "cameo-ai-matches.jsonl"
        write_jsonl(f, [match_row()])
        return {"Logs/cameo-ai-matches.jsonl": f}

    def test_doc_is_none_when_unbound(self):
        zipped = self._zipped_tree()
        self.assertIsNone(
            campaign_runner._bundle_provenance_doc(zipped, None, None, UID))

    def test_doc_binds_zipped_files_not_provenance(self):
        root = pathlib.Path(tempfile.mkdtemp(prefix="prov_bundle2_"))
        f = root / "Logs" / "cameo-ai-matches.jsonl"
        write_jsonl(f, [match_row()])
        prov_file = root / "capture_provenance.json"
        prov_file.write_text("{}", encoding="utf-8")
        zipped = {"Logs/cameo-ai-matches.jsonl": f,
                  "capture_provenance.json": prov_file}
        doc = campaign_runner._bundle_provenance_doc(
            zipped, prov.SENTINEL_KIND, {"scoreable": False}, UID)
        self.assertEqual(doc["capture_kind"], "sentinel_diagnostic")
        self.assertFalse(doc["scoreable"])
        self.assertEqual(doc["game_uids"], [UID])
        # The manifest never binds itself (self-ref rule).
        self.assertEqual(list(doc["artifacts"]), ["Logs/cameo-ai-matches.jsonl"])
        self.assertEqual(doc["artifacts"]["Logs/cameo-ai-matches.jsonl"],
                         prov.sha256_file(f))

    def test_unzipped_bundle_stays_quarantined(self):
        import zipfile
        src = pathlib.Path(tempfile.mkdtemp(prefix="prov_bsrc_"))
        f = src / "Logs" / "cameo-ai-matches.jsonl"
        write_jsonl(f, [match_row()])
        zipped = {"Logs/cameo-ai-matches.jsonl": f}
        doc = campaign_runner._bundle_provenance_doc(
            zipped, prov.SENTINEL_KIND, {"scoreable": False}, UID)
        bundle = src / "support_bundle.zip"
        with zipfile.ZipFile(bundle, "w") as z:
            for rel, p in zipped.items():
                z.write(p, rel)
            z.writestr("capture_provenance.json",
                       json.dumps(doc, indent=2, sort_keys=True) + "\n")
        dest = pathlib.Path(tempfile.mkdtemp(prefix="prov_bdst_"))
        with zipfile.ZipFile(bundle) as z:
            z.extractall(dest)
        verdict, _why = prov.assess_file(
            dest / "Logs" / "cameo-ai-matches.jsonl")
        self.assertEqual(verdict, prov.SENTINEL)

    def test_scoreable_bundle_stays_bound(self):
        import zipfile
        src = pathlib.Path(tempfile.mkdtemp(prefix="prov_bsrc2_"))
        f = src / "Logs" / "cameo-ai-matches.jsonl"
        write_jsonl(f, [match_row()])
        zipped = {"Logs/cameo-ai-matches.jsonl": f}
        doc = campaign_runner._bundle_provenance_doc(
            zipped, "match_batch", {"scoreable": True}, UID)
        bundle = src / "support_bundle.zip"
        with zipfile.ZipFile(bundle, "w") as z:
            for rel, p in zipped.items():
                z.write(p, rel)
            z.writestr("capture_provenance.json",
                       json.dumps(doc, indent=2, sort_keys=True) + "\n")
        dest = pathlib.Path(tempfile.mkdtemp(prefix="prov_bdst2_"))
        with zipfile.ZipFile(bundle) as z:
            z.extractall(dest)
        verdict, _why = prov.assess_file(
            dest / "Logs" / "cameo-ai-matches.jsonl")
        self.assertEqual(verdict, prov.ALLOW)


class ReceiptSchemaTests(unittest.TestCase):
    def test_schema_allows_optional_capture_fields(self):
        schema = json.loads(
            (ROOT / "tools/ai/ab_campaign_receipt.schema.json").read_text(encoding="utf-8"))
        props = schema["properties"]
        for key in ("capture_kind", "scoreable", "capture_provenance"):
            self.assertIn(key, props)
        self.assertNotIn("capture_kind", schema["required"])
        self.assertFalse(schema["additionalProperties"])


class ProvenanceStateTests(unittest.TestCase):
    """provenance_state four-way + write_file_sibling/rebind_artifact (CAT-3)."""

    def test_state_bound(self):
        support = bound_support()
        self.assertEqual(
            prov.provenance_state(support / "Logs" / "cameo-ai-matches.jsonl"),
            "bound")

    def test_state_sentinel(self):
        support = bound_support(marked=True)
        self.assertEqual(
            prov.provenance_state(support / "Logs" / "cameo-ai-matches.jsonl"),
            "sentinel")

    def test_state_absent(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_state_"))
        f = tmp / "cameo-ai-matches.jsonl"
        write_jsonl(f, [match_row()])
        self.assertEqual(prov.provenance_state(f), "absent")

    def test_state_corrupt_on_sha_mismatch(self):
        support = bound_support()
        f = support / "Logs" / "cameo-ai-matches.jsonl"
        f.write_text(f.read_text() + " ", encoding="utf-8")
        self.assertEqual(prov.provenance_state(f), "corrupt")

    def test_derived_sibling_binds_new_bytes(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_sib_"))
        f = tmp / "cameo-ai-matches.jsonl"
        write_jsonl(f, [match_row()])
        prov.write_file_sibling(f, game_uids=[UID],
                                capture_kind=prov.LEGACY_KIND, scoreable=True)
        verdict, _why = prov.assess_file(f)
        self.assertEqual(verdict, prov.ALLOW)

    def test_rebind_updates_covering_manifest(self):
        support = bound_support()
        f = support / "Logs" / "cameo-ai-matches.jsonl"
        f.write_text(f.read_text() + " ", encoding="utf-8")
        verdict, _why = prov.assess_file(f)
        self.assertEqual(verdict, prov.REJECT)  # stale manifest = conflict
        updated = prov.rebind_artifact(f)
        self.assertTrue(updated)
        verdict, _why = prov.assess_file(f)
        self.assertEqual(verdict, prov.ALLOW)

    def test_rebind_never_touches_quarantine(self):
        support = bound_support(marked=True)
        f = support / "Logs" / "cameo-ai-matches.jsonl"
        f.write_text(f.read_text() + " ", encoding="utf-8")
        prov.rebind_artifact(f)
        verdict, _why = prov.assess_file(f)
        self.assertEqual(verdict, prov.SENTINEL)


def engagement_support(marked=False, uid=UID):
    """Support dir with Logs/cameo-ai-engagements.jsonl + provenance."""
    support = pathlib.Path(tempfile.mkdtemp(prefix="prov_eng_"))
    write_jsonl(support / "Logs" / "cameo-ai-engagements.jsonl",
                [{"record": "engagement", "schema": "engagement/2",
                  "game_uid": uid, "seat": "seat_0", "tick": 10}])
    prov.write_provenance(
        support,
        capture_kind="sentinel_diagnostic" if marked else "match_batch",
        scoreable=not marked, game_uids=[uid])
    return support


class LearnerGateTests(unittest.TestCase):
    """Learned-artifact ingest is provenance-gated (checklist rows 13-16b)."""

    def test_fit_fight_learning_skips_sentinel(self):
        support = engagement_support(marked=True)
        self.assertEqual(list(fit_fight_learning.read_records([support])), [])

    def test_fit_fight_learning_rejects_unbound(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_ffl_"))
        write_jsonl(tmp / "Logs" / "cameo-ai-engagements.jsonl",
                    [{"record": "engagement", "game_uid": UID}])
        with self.assertRaises(ValueError):
            list(fit_fight_learning.read_records([tmp]))

    def test_fit_fight_learning_bound_yields(self):
        support = engagement_support()
        rows = list(fit_fight_learning.read_records([support]))
        self.assertEqual(len(rows), 1)

    def test_fit_fight_learning_recursive_gate(self):
        """A sentinel file nested below an arg dir is still skipped."""
        root = pathlib.Path(tempfile.mkdtemp(prefix="prov_fflr_"))
        support = root / "batch_0"
        (support / "Logs").mkdir(parents=True)
        write_jsonl(support / "Logs" / "cameo-ai-engagements.jsonl",
                    [{"record": "engagement", "game_uid": UID}])
        prov.write_provenance(support, capture_kind="sentinel_diagnostic",
                              scoreable=False, game_uids=[UID])
        self.assertEqual(list(fit_fight_learning.read_records([root])), [])

    def test_tune_plan_bandits_skips_sentinel(self):
        support = engagement_support(marked=True)
        self.assertEqual(tune_plan_bandits.load_engagement_rows([support]), [])

    def test_tune_plan_bandits_rejects_unbound(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_tpb_"))
        write_jsonl(tmp / "Logs" / "cameo-ai-engagements.jsonl",
                    [{"record": "engagement", "game_uid": UID}])
        with self.assertRaises(ValueError):
            tune_plan_bandits.load_engagement_rows([tmp])

    def test_tune_plan_bandits_bound_yields(self):
        support = engagement_support()
        self.assertEqual(len(tune_plan_bandits.load_engagement_rows([support])), 1)


class ReportGateTests(unittest.TestCase):
    """CSV/verdict producers gate their telemetry ingest like every CAT-1."""

    def test_player_files_skips_sentinel(self):
        support = pathlib.Path(tempfile.mkdtemp(prefix="prov_ps_"))
        write_jsonl(support / "Logs" / "cameo-ai-players-abc.jsonl",
                    [{"schema": "cameo-player-sample", "version": 1,
                      "tick": 0, "seat": "seat_0"}])
        prov.write_provenance(support, capture_kind="sentinel_diagnostic",
                              scoreable=False, game_uids=[UID])
        self.assertEqual(playtest_stats_from_logs.player_files([support]), [])

    def test_player_files_rejects_unbound(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_psb_"))
        write_jsonl(tmp / "cameo-ai-players-abc.jsonl", [{"schema": "x"}])
        with self.assertRaises(ValueError):
            playtest_stats_from_logs.player_files([tmp / "cameo-ai-players-abc.jsonl"])

    def test_player_files_bound_collects(self):
        support = pathlib.Path(tempfile.mkdtemp(prefix="prov_psok_"))
        write_jsonl(support / "Logs" / "cameo-ai-players-abc.jsonl",
                    [{"schema": "cameo-player-sample", "version": 1,
                      "tick": 0, "seat": "seat_0"}])
        prov.write_provenance(support, capture_kind="match_batch",
                              scoreable=True, game_uids=[UID])
        self.assertEqual(len(playtest_stats_from_logs.player_files([support])), 1)

    def test_match_records_skips_sentinel(self):
        support = bound_support(marked=True)
        self.assertEqual(dict(playtest_stats_from_logs.match_records([support])), {})

    def test_army_mix_skips_sentinel(self):
        support = bound_support(marked=True)
        self.assertEqual(army_mix_report.record_files([support]), [])

    def test_army_mix_rejects_unbound(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="prov_amx_"))
        write_jsonl(tmp / "Logs" / "cameo-ai-matches.jsonl", [match_row()])
        with self.assertRaises(ValueError):
            army_mix_report.record_files([tmp])

    def test_army_mix_bound_collects(self):
        support = bound_support()
        self.assertEqual(len(army_mix_report.record_files([support])), 1)


class PilotGateTests(unittest.TestCase):
    """adjudicate() is the library-entry quarantine (runner + CLI share it)."""

    def test_marked_receipt_invalid_unknown(self):
        receipt = {"cell_id": "c1", "manifest_sha256": "m",
                   "capture_kind": "sentinel_diagnostic"}
        out = ab_campaign_pilot.adjudicate([], receipt, {"cell_id": "c1"}, "m")
        self.assertEqual(out["verdict"], "INVALID_UNKNOWN")
        self.assertIn("sentinel", out["reason"])

    def test_embedded_provenance_uid_mismatch(self):
        receipt = {"cell_id": "c1", "manifest_sha256": "m",
                   "game_uid": UID,
                   "capture_provenance": {"game_uids": ["other-uid"]}}
        out = ab_campaign_pilot.adjudicate([], receipt, {"cell_id": "c1"}, "m")
        self.assertEqual(out["verdict"], "INVALID_UNKNOWN")
        self.assertIn("capture provenance", out["reason"])

    def test_marked_match_record_invalid_unknown(self):
        receipt = {"cell_id": "c1", "manifest_sha256": "m"}
        out = ab_campaign_pilot.adjudicate(
            [{"capture_kind": "sentinel_diagnostic"}], receipt,
            {"cell_id": "c1"}, "m")
        self.assertEqual(out["verdict"], "INVALID_UNKNOWN")
        self.assertIn("sentinel", out["reason"])


class AnalyzerLinkedArtifactTests(unittest.TestCase):
    """Health sidecars and receipt-linked matches_jsonl are gated too."""

    def test_load_health_sentinel_becomes_error_row(self):
        support = pathlib.Path(tempfile.mkdtemp(prefix="prov_lh_"))
        (support / "health.json").write_text(json.dumps(
            {"game_uid": UID, "player": "seat_0", "findings": []}),
            encoding="utf-8")
        prov.write_provenance(support, capture_kind="sentinel_diagnostic",
                              scoreable=False, game_uids=[UID])
        out = az.load_health([support])
        self.assertEqual(out["games"], {})
        self.assertTrue(any("sentinel" in e["reason"] for e in out["errors"]))

    def test_load_health_corrupt_raises(self):
        support = bound_support()
        health_file = support / "health.json"
        health_file.write_text(json.dumps(
            {"game_uid": UID, "player": "seat_0", "findings": []}),
            encoding="utf-8")
        prov.write_provenance(support, capture_kind="match_batch",
                              scoreable=True, game_uids=[UID])
        health_file.write_text(health_file.read_text() + " ", encoding="utf-8")
        with self.assertRaises(ValueError):
            az.load_health([support])

    def test_seat_map_rejects_sentinel_linked_matches(self):
        support = bound_support(marked=True)
        linked = support / "Logs" / "cameo-ai-matches.jsonl"
        receipt = {"game_uid": UID,
                   "artifacts": {"matches_jsonl": {"path": str(linked)}},
                   "seats": [{"home_location": "1,1", "faction": "td_gdi",
                              "bot_type": "hard"}]}
        seat_map, status, why = az.expected_health_seat_map(receipt)
        self.assertIsNone(seat_map)
        self.assertEqual(status, "SCHEMA_MISMATCH")
        self.assertIn("sentinel", why)

    def test_seat_map_corrupt_linked_matches_raises(self):
        support = bound_support()
        linked = support / "Logs" / "cameo-ai-matches.jsonl"
        linked.write_text(linked.read_text() + " ", encoding="utf-8")
        receipt = {"game_uid": UID,
                   "artifacts": {"matches_jsonl": {"path": str(linked)}},
                   "seats": [{"home_location": "1,1", "faction": "td_gdi",
                              "bot_type": "hard"}]}
        with self.assertRaises(ValueError):
            az.expected_health_seat_map(receipt)


LEGACY_MATCH_ROW = {"schema": 2, "game_uid": "game-x", "record_id": "game-x|Alpha",
                    "player": {"name": "Alpha", "faction": "td_gdi",
                               "bot_type": "hard", "home": "2,3"},
                    "opponents": [{"name": "Bravo", "is_bot": False,
                                   "faction": "td_nod", "outcome": "lost"}],
                    "allies": [],
                    "takeover": {"controller_client": 19}, "stats": {}}


class AnonymizeGateTests(unittest.TestCase):
    """CAT-3: refuse sentinel/marked/corrupt; bound inputs get derived
    provenance that keeps the rewritten file positively bound."""

    def _legacy_support(self):
        support = pathlib.Path(tempfile.mkdtemp(prefix="prov_anon_"))
        write_jsonl(support / "cameo-ai-matches.jsonl", [LEGACY_MATCH_ROW])
        return support

    def test_refuses_sentinel_input_before_mutation(self):
        support = self._legacy_support()
        prov.write_provenance(support, capture_kind="sentinel_diagnostic",
                              scoreable=False, game_uids=["game-x"])
        before = (support / "cameo-ai-matches.jsonl").read_bytes()
        with self.assertRaises(ValueError):
            anonymize_legacy_logs.migrate(support)
        self.assertEqual((support / "cameo-ai-matches.jsonl").read_bytes(), before)

    def test_refuses_marker_row_before_mutation(self):
        support = self._legacy_support()
        row = dict(LEGACY_MATCH_ROW, capture_kind="sentinel_diagnostic")
        write_jsonl(support / "cameo-ai-matches.jsonl", [row])
        before = (support / "cameo-ai-matches.jsonl").read_bytes()
        with self.assertRaises(ValueError):
            anonymize_legacy_logs.migrate(support)
        self.assertEqual((support / "cameo-ai-matches.jsonl").read_bytes(), before)

    def test_refuses_corrupt_provenance(self):
        support = self._legacy_support()
        prov.write_provenance(support, capture_kind="match_batch",
                              scoreable=True, game_uids=["game-x"])
        f = support / "cameo-ai-matches.jsonl"
        f.write_text(f.read_text() + " ", encoding="utf-8")
        with self.assertRaises(ValueError):
            anonymize_legacy_logs.migrate(support)

    def test_bound_input_rebinds_to_new_bytes(self):
        support = self._legacy_support()
        prov.write_provenance(support, capture_kind="match_batch",
                              scoreable=True, game_uids=["game-x"])
        result = anonymize_legacy_logs.migrate(support)
        self.assertEqual(result["files_rewritten"], 1)
        f = support / "cameo-ai-matches.jsonl"
        verdict, why = prov.assess_file(f)
        self.assertEqual(verdict, prov.ALLOW, why)

    def test_unbound_input_stays_unbound(self):
        support = self._legacy_support()
        result = anonymize_legacy_logs.migrate(support)
        self.assertEqual(result["files_rewritten"], 1)
        verdict, _why = prov.assess_file(support / "cameo-ai-matches.jsonl")
        self.assertEqual(verdict, prov.REJECT)  # fail-closed downstream


class SentinelMarkerByteTests(unittest.TestCase):
    """Checklist #18 seam: default mode is an exact identity — no marker
    keys exist in the serialized record/summary, hence byte-identical."""

    def test_default_result_is_identity(self):
        result = {"variant": "v", "status": "ok", "records": 2}
        self.assertIs(run_ai_match_batch.stamp_sentinel_result(result, None),
                      result)
        self.assertNotIn("capture_kind", result)
        self.assertNotIn("scoreable", result)
        self.assertNotIn("health_policy", result)

    def test_default_summary_is_identity(self):
        summary = {"matches": 1, "ok": 1, "results": []}
        self.assertIs(run_ai_match_batch.stamp_sentinel_summary(summary, None),
                      summary)
        self.assertNotIn("capture_kind", summary)
        self.assertNotIn("scoreable", summary)
        self.assertNotIn("sentinel_uids", summary)

    def test_sentinel_result_markers(self):
        result = run_ai_match_batch.stamp_sentinel_result(
            {"status": "ok"}, {"disposition": "clean_diagnostic"})
        self.assertEqual(result["capture_kind"], "sentinel_diagnostic")
        self.assertFalse(result["scoreable"])
        self.assertEqual(result["health_policy"]["disposition"],
                         "clean_diagnostic")

    def test_sentinel_summary_markers(self):
        summary = run_ai_match_batch.stamp_sentinel_summary({}, [UID])
        self.assertEqual(summary["capture_kind"], "sentinel_diagnostic")
        self.assertFalse(summary["scoreable"])
        self.assertEqual(summary["sentinel_uids"], [UID])

    def test_serialized_bytes_unchanged_default(self):
        """Fixed-content fixture: the default path serializes byte-identical
        because no marker keys are introduced by the stamp helpers."""
        result = {"variant": "v", "status": "ok", "records": 2,
                  "fingerprint": "fp1", "health_status": "OBSERVED_HEALTHY"}
        before = json.dumps(result, indent=2, sort_keys=True)
        run_ai_match_batch.stamp_sentinel_result(result, None)
        self.assertEqual(json.dumps(result, indent=2, sort_keys=True), before)
        summary = {"matches": 1, "ok": 1, "results": [dict(result)]}
        before = json.dumps(summary, indent=2)
        run_ai_match_batch.stamp_sentinel_summary(summary, None)
        self.assertEqual(json.dumps(summary, indent=2), before)


if __name__ == "__main__":
    unittest.main()
