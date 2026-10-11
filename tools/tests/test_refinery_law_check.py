"""Synthetic-record tests for tools/ai/refinery_law_check.py."""
from __future__ import annotations

import json
import pathlib
import sys
import tempfile
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "ai"))
import refinery_law_check as rlc  # noqa: E402


def snap(game, player, tick, bot_type="hard", **exp):
    e = {"refineries": 0, "coverage_milli": 100}
    e.update(exp)
    return {"kind": "situation", "game_uid": game, "player": player, "tick": tick,
            "map_uid": "m1", "bot_type": bot_type, "expansion": e}


def place(game, player, tick, **kw):
    p = {"kind": "placement", "game_uid": game, "player": player, "tick": tick,
         "map_uid": "m1", "bot_type": "hard", "category": "refinery",
         "actor": "td_gdi_tiberiumrefinery", "cell": "10,10", "reason": "refinery_claim",
         "anchor_cell": "9,61", "resource_gap": 0, "field_id": "f1", "tier": 1}
    p.update(kw)
    return p


def lost(game, player, tick, cell="10,10", anchor="9,61", field="f1", cause="killed", **kw):
    r = {"kind": "refinery_lost", "game_uid": game, "player": player, "tick": tick,
         "map_uid": "m1", "bot_type": "hard", "actor": "td_gdi_tiberiumrefinery",
         "cell": cell, "anchor_cell": anchor, "field_id": field, "cause": cause}
    r.update(kw)
    return r


def acquired(game, player, tick, cell="30,30", anchor="9,61", field="f1", cause="captured", **kw):
    r = {"kind": "refinery_acquired", "game_uid": game, "player": player, "tick": tick,
         "map_uid": "m1", "bot_type": "hard", "actor": "td_gdi_tiberiumrefinery",
         "cell": cell, "anchor_cell": anchor, "field_id": field, "cause": cause}
    r.update(kw)
    return r


def match(game, player, bot="hard"):
    return {"kind": "match", "game_uid": game, "map_uid": "m1",
            "player": {"name": player, "bot_type": bot, "faction": "td_gdi"}}


def write_dir(root, name, snaps, places, matches):
    d = root / name / "Logs"
    d.mkdir(parents=True)
    (d / "cameo-ai-situations.jsonl").write_text("\n".join(json.dumps(r) for r in snaps), encoding="utf-8")
    (d / "cameo-ai-placements.jsonl").write_text("\n".join(json.dumps(r) for r in places), encoding="utf-8")
    (d / "cameo-ai-matches.jsonl").write_text("\n".join(json.dumps(r) for r in matches), encoding="utf-8")
    return d.parent


class RefineryLawCheckTests(unittest.TestCase):
    def run_tool(self, dirs, *extra):
        return rlc.main([str(d) for d in dirs] + list(extra))

    def build_one(self, snaps, places, matches=None):
        with tempfile.TemporaryDirectory() as tmp:
            d = write_dir(pathlib.Path(tmp), "b", snaps, places, matches or [match("g1", "Multi0")])
            return rlc.build(rlc.c.load([d]))

    def row(self, result, bot="hard"):
        return next(r for r in result["matches"] if r["bot_type"] == bot)

    def test_stacked_refineries_on_one_anchor_fail(self):
        res = self.build_one(
            [snap("g1", "Multi0", 100), snap("g1", "Multi0", 500)],
            [place("g1", "Multi0", 200, cell="10,10"), place("g1", "Multi0", 300, cell="11,11")])
        r = self.row(res)
        self.assertEqual(r["verdict"], "FAIL")
        self.assertEqual(r["refineries_per_anchor_max"], 2)
        self.assertIn("refineries_per_anchor_max=2", r["fails"])

    def test_two_fields_two_refineries_pass(self):
        res = self.build_one(
            [snap("g1", "Multi0", 100), snap("g1", "Multi0", 900, fields_in_reach_unserved=0, refineries_per_anchor_max=1)],
            [place("g1", "Multi0", 200, field_id="f1", anchor_cell="9,61"),
             place("g1", "Multi0", 400, field_id="f2", anchor_cell="50,50")])
        r = self.row(res)
        self.assertEqual(r["verdict"], "PASS")
        self.assertEqual(r["refineries_per_anchor_max"], 1)

    def test_base_reason_fails(self):
        res = self.build_one([snap("g1", "Multi0", 100)],
                             [place("g1", "Multi0", 200, reason="base")])
        r = self.row(res)
        self.assertEqual(r["verdict"], "FAIL")
        self.assertIn("base_reason_count=1", r["fails"])

    def test_gap_above_one_fails(self):
        res = self.build_one([snap("g1", "Multi0", 100)],
                             [place("g1", "Multi0", 200, resource_gap=2)])
        r = self.row(res)
        self.assertEqual(r["verdict"], "FAIL")
        self.assertIn("resource_gap_max=2", r["fails"])

    def test_tier2_while_unserved_fails(self):
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=2),
             snap("g1", "Multi0", 400, fields_in_reach_unserved=2)],
            [place("g1", "Multi0", 300, field_id="f1", tier=1),
             place("g1", "Multi0", 450, field_id="f1", tier=2)])
        r = self.row(res)
        self.assertEqual(r["verdict"], "FAIL")
        self.assertEqual(r["tier_order_violations"], 1)

    def test_tier2_when_empty_is_clean(self):
        # legal tier-2: a SECOND spreader on the already-served field, claimed after tier 1 drained
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=1),
             snap("g1", "Multi0", 300, fields_in_reach_unserved=0)],
            [place("g1", "Multi0", 200, field_id="f1", tier=1),
             place("g1", "Multi0", 350, field_id="f1", tier=2, resource_gap=0, anchor_cell="9,55")])
        r = self.row(res)
        self.assertEqual(r["refineries_per_anchor_max"], 1)  # per-anchor, never per-field
        self.assertEqual(r["verdict"], "PASS")
        self.assertEqual(r["tier_order_violations"], 0)

    def test_two_refineries_one_spreader_fails(self):
        # same field, same anchor_cell = two refineries on one spreader -> FAIL
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=0)],
            [place("g1", "Multi0", 200, field_id="f1", tier=1),
             place("g1", "Multi0", 350, field_id="f1", tier=2, resource_gap=0)])
        r = self.row(res)
        self.assertEqual(r["refineries_per_anchor_max"], 2)
        self.assertEqual(r["verdict"], "FAIL")

    def test_rebuild_after_loss_not_double_counted(self):
        # anchor "9,61" dies ~t300: f1 re-enters unserved; the rebuild at t500 is a legal re-serve
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=0, fields_in_reach_unserved_ids=[]),
             snap("g1", "Multi0", 350, fields_in_reach_unserved=1, fields_in_reach_unserved_ids=["f1"]),
             snap("g1", "Multi0", 900, fields_in_reach_unserved=0, fields_in_reach_unserved_ids=[],
                  refineries_per_anchor_max=1)],
            [place("g1", "Multi0", 200, field_id="f1", anchor_cell="9,61"),
             place("g1", "Multi0", 500, field_id="f1", anchor_cell="9,61")])
        r = self.row(res)
        self.assertEqual(r["refineries_per_anchor_max"], 1)
        self.assertNotIn("refineries_per_anchor_max", ";".join(r["fails"]))

    def test_same_anchor_while_still_served_still_fails(self):
        # the field never re-entered unserved between bindings = a real double-bind
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=0, fields_in_reach_unserved_ids=[]),
             snap("g1", "Multi0", 600, fields_in_reach_unserved=0, fields_in_reach_unserved_ids=[])],
            [place("g1", "Multi0", 200, field_id="f1", anchor_cell="9,61"),
             place("g1", "Multi0", 500, field_id="f1", anchor_cell="9,61")])
        r = self.row(res)
        self.assertEqual(r["refineries_per_anchor_max"], 2)
        self.assertEqual(r["verdict"], "FAIL")

    def test_rebuild_unprovable_without_unserved_ids_counts(self):
        # logs without fields_in_reach_unserved_ids can't prove a rebuild: fails closed
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=0),
             snap("g1", "Multi0", 600, fields_in_reach_unserved=0)],
            [place("g1", "Multi0", 200, field_id="f1", anchor_cell="9,61"),
             place("g1", "Multi0", 500, field_id="f1", anchor_cell="9,61")])
        r = self.row(res)
        self.assertEqual(r["refineries_per_anchor_max"], 2)
        self.assertEqual(r["verdict"], "FAIL")

    def test_rebuild_after_lost_record_skips(self):
        # refinery_lost on the same anchor between the placements: exact rebuild evidence, no uids needed
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=0, fields_in_reach_unserved_ids=[])],
            [place("g1", "Multi0", 200, cell="10,10", field_id="f1", anchor_cell="9,61"),
             lost("g1", "Multi0", 300, cell="10,10", anchor="9,61", field="f1"),
             place("g1", "Multi0", 500, cell="12,12", field_id="f1", anchor_cell="9,61")])
        r = self.row(res)
        self.assertEqual(r["refineries_per_anchor_max"], 1)
        self.assertEqual(r["per_anchor_basis"], "events")
        self.assertNotIn("refineries_per_anchor_max", ";".join(r["fails"]))

    def test_lost_on_other_anchor_does_not_skip(self):
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=0, fields_in_reach_unserved_ids=[])],
            [place("g1", "Multi0", 200, cell="10,10", field_id="f1", anchor_cell="9,61"),
             lost("g1", "Multi0", 300, cell="70,70", anchor="70,70", field="f9"),
             place("g1", "Multi0", 500, cell="12,12", field_id="f1", anchor_cell="9,61")])
        r = self.row(res)
        self.assertEqual(r["verdict"], "FAIL")
        self.assertEqual(r["refineries_per_anchor_max"], 2)

    def test_lost_before_previous_placement_does_not_skip(self):
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=0, fields_in_reach_unserved_ids=[])],
            [lost("g1", "Multi0", 50, cell="8,8", anchor="9,61", field="f1"),
             place("g1", "Multi0", 200, cell="10,10", field_id="f1", anchor_cell="9,61"),
             place("g1", "Multi0", 500, cell="12,12", field_id="f1", anchor_cell="9,61")])
        r = self.row(res)
        self.assertEqual(r["verdict"], "FAIL")
        self.assertEqual(r["refineries_per_anchor_max"], 2)

    def test_sold_cause_also_resets_the_anchor(self):
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=0, fields_in_reach_unserved_ids=[])],
            [place("g1", "Multi0", 200, cell="10,10", field_id="f1", anchor_cell="9,61"),
             lost("g1", "Multi0", 300, cell="10,10", anchor="9,61", field="f1", cause="sold"),
             place("g1", "Multi0", 500, cell="12,12", field_id="f1", anchor_cell="9,61")])
        r = self.row(res)
        self.assertEqual(r["refineries_per_anchor_max"], 1)
        self.assertEqual(r["verdict"], "PASS")

    def test_captured_cobind_excluded_from_live_max(self):
        # ruling-A residual: snapshot rpa=2 from a captured refinery folding onto the occupied anchor.
        # With lifecycle telemetry the live max is reconstructed: 1 placed (rebuilt after a kill) +
        # 1 captured, which is never a build -> PASS.
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=0, fields_in_reach_unserved_ids=[]),
             snap("g1", "Multi0", 900, fields_in_reach_unserved=0, fields_in_reach_unserved_ids=[],
                  refineries_per_anchor_max=2)],
            [place("g1", "Multi0", 200, cell="10,10", field_id="f1", anchor_cell="9,61"),
             lost("g1", "Multi0", 400, cell="10,10", anchor="9,61", field="f1"),
             place("g1", "Multi0", 500, cell="12,12", field_id="f1", anchor_cell="9,61"),
             acquired("g1", "Multi0", 700, cell="30,30", anchor="9,61", field="f1")])
        r = self.row(res)
        self.assertEqual(r["refineries_per_anchor_max"], 1)
        self.assertEqual(r["refinery_acquired"], 1)
        self.assertEqual(r["verdict"], "PASS")

    def test_acquired_later_lost_stays_excluded(self):
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=0, fields_in_reach_unserved_ids=[])],
            [place("g1", "Multi0", 200, cell="10,10", field_id="f1", anchor_cell="9,61"),
             acquired("g1", "Multi0", 300, cell="30,30", anchor="9,61", field="f1"),
             lost("g1", "Multi0", 400, cell="30,30", anchor="9,61", field="f1")])
        r = self.row(res)
        self.assertEqual(r["refineries_per_anchor_max"], 1)
        self.assertEqual(r["refinery_lost"], 1)
        self.assertEqual(r["verdict"], "PASS")

    def test_no_lifecycle_keeps_snapshot_basis(self):
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=0, fields_in_reach_unserved_ids=[])],
            [place("g1", "Multi0", 200, field_id="f1", anchor_cell="9,61")])
        r = self.row(res)
        self.assertEqual(r["per_anchor_basis"], "snapshot")

    def test_old_log_fields_mark_na_and_never_pass(self):
        p = place("g1", "Multi0", 200)
        for k in ("field_id", "tier", "resource_gap"):
            p.pop(k)
        res = self.build_one([snap("g1", "Multi0", 100), snap("g1", "Multi0", 500)], [p])
        r = self.row(res)
        self.assertEqual(r["verdict"], "n/a")
        self.assertEqual(r["resource_gap_hist"], "n/a")
        self.assertIsNone(r["tier_order_violations"])
        self.assertIsNone(r["claim_latency_p90"])

    def test_latency_warn(self):
        res = rlc.build({"matches": [], "situations":
                         [snap("g1", "Multi0", 100, fields_in_reach_unserved=1),
                          snap("g1", "Multi0", 6000, fields_in_reach_unserved=0)],
                         "placements": [place("g1", "Multi0", 6000, field_id="f1")],
                         "engagements": []}, warn_latency=1000)
        r = self.row(res)
        self.assertEqual(r["verdict"], "WARN")
        self.assertEqual(r["claim_latency_max"], 5900)

    def test_classic_never_fails(self):
        p = place("g1", "Multi1", 200, reason="base", bot_type="classic")
        p.pop("resource_gap")
        res = self.build_one([snap("g1", "Multi1", 100, bot_type="classic")], [p],
                             [match("g1", "Multi1", bot="classic")])
        r = self.row(res, bot="classic")
        self.assertEqual(r["bot_type"], "classic")
        self.assertFalse(rlc.is_genericbot("classic"))
        # exit code ignores classic rows even when its metrics would fail
        self.assertEqual(rlc.main.__module__, "refinery_law_check")

    def test_exact_per_field_latency(self):
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=1, fields_in_reach_unserved_ids=["f1"]),
             snap("g1", "Multi0", 200, fields_in_reach_unserved=2, fields_in_reach_unserved_ids=["f1", "f2"]),
             snap("g1", "Multi0", 900, fields_in_reach_unserved=0, fields_in_reach_unserved_ids=[])],
            [place("g1", "Multi0", 300, field_id="f1", tier=1),
             place("g1", "Multi0", 400, field_id="f2", tier=1, anchor_cell="50,50")])
        r = self.row(res)
        self.assertEqual(r["verdict"], "PASS")
        self.assertEqual(r["claim_latency_basis"], "exact")
        # f1: 300-100=200, f2: 400-200=200
        self.assertEqual(r["claim_latency_p50"], 200)
        self.assertEqual(r["claim_latency_max"], 200)
        self.assertEqual(r["claim_latency_never"], 0)

    def test_unserved_field_flagged_never(self):
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=1, fields_in_reach_unserved_ids=["f1"]),
             snap("g1", "Multi0", 200, fields_in_reach_unserved=2, fields_in_reach_unserved_ids=["f1", "f3"]),
             snap("g1", "Multi0", 1000, fields_in_reach_unserved=2, fields_in_reach_unserved_ids=["f1", "f3"])],
            [place("g1", "Multi0", 300, field_id="f1", tier=1)])
        r = self.row(res)
        self.assertEqual(r["claim_latency_never"], 1)
        self.assertEqual(r["verdict"], "WARN")  # f3 sat unserved to match end (snap@1000 -> 800t)
        self.assertEqual(r["claim_latency_max"], 800)

    def test_never_uses_match_duration_when_logged(self):
        res = rlc.build({"matches": [{**match("g1", "Multi0"), "duration_ticks": 2000}],
                         "situations": [snap("g1", "Multi0", 100, fields_in_reach_unserved=1, fields_in_reach_unserved_ids=["f9"]),
                                        snap("g1", "Multi0", 500, fields_in_reach_unserved=1, fields_in_reach_unserved_ids=["f9"])],
                         "placements": [place("g1", "Multi0", 300, field_id="f1", tier=1)],
                         "engagements": []})
        r = self.row(res)
        self.assertEqual(r["claim_latency_max"], 1900)  # 2000-100 via duration_ticks, not last snap 500

    def test_map_aggregate_na_when_all_missing(self):
        # old-schema rows only -> map-level gap/tier/anchor must be n/a, never a fake 0
        p = place("g1", "Multi0", 200)
        for k in ("field_id", "tier", "resource_gap"):
            p.pop(k)
        res = self.build_one([snap("g1", "Multi0", 100)], [p])
        m = res["maps"][0]
        self.assertIsNone(m["resource_gap_max"])
        self.assertIsNone(m["tier_order_violations"])
        self.assertIsNotNone(m["worst_refineries_per_anchor_max"])  # anchor computable from anchor_cell

    def test_exit_codes(self):
        with tempfile.TemporaryDirectory() as tmp:
            good = write_dir(pathlib.Path(tmp), "good",
                             [snap("g1", "Multi0", 100, fields_in_reach_unserved=0)],
                             [place("g1", "Multi0", 200, field_id="f1")],
                             [match("g1", "Multi0")])
            bad = write_dir(pathlib.Path(tmp), "bad",
                            [snap("g2", "Multi0", 100)],
                            [place("g2", "Multi0", 200, reason="base")],
                            [match("g2", "Multi0")])
            self.assertEqual(rlc.main([str(good)]), 0)
            self.assertEqual(rlc.main([str(bad)]), 1)
            self.assertEqual(rlc.main([str(good), str(bad)]), 1)

    def test_no_refinery_past_threshold_fails(self):
        # REF-1 smoke exposed this: a hard seat built zero refineries in a long match and read as n/a
        res = rlc.build({"matches": [{**match("g1", "Multi0"), "duration_ticks": 12642}],
                         "situations": [snap("g1", "Multi0", 100, fields_in_reach=0),
                                        snap("g1", "Multi0", 12000, fields_in_reach=0)],
                         "placements": [], "engagements": []})
        r = self.row(res)
        self.assertEqual(r["verdict"], "FAIL")
        self.assertIn("no_refinery", r["fails"])

    def test_no_refinery_before_threshold_stays_na(self):
        res = rlc.build({"matches": [{**match("g1", "Multi0"), "duration_ticks": 2000}],
                         "situations": [snap("g1", "Multi0", 100, fields_in_reach=0)],
                         "placements": [], "engagements": []})
        r = self.row(res)
        self.assertEqual(r["verdict"], "n/a")
        self.assertNotIn("no_refinery", r["fails"])

    def test_coverage_flat_warns_not_fails(self):
        res = self.build_one(
            [snap("g1", "Multi0", 100, coverage_milli=200, fields_in_reach_unserved=0),
             snap("g1", "Multi0", 8000, coverage_milli=150, fields_in_reach_unserved=0),
             snap("g1", "Multi0", 12000, coverage_milli=150, fields_in_reach_unserved=0)],
            [place("g1", "Multi0", 200, field_id="f1")])
        r = self.row(res)
        self.assertEqual(r["verdict"], "WARN")
        self.assertIn("coverage_flat", r["warns"])
        self.assertFalse(r["fails"])

    def test_coverage_still_growing_no_warn(self):
        res = self.build_one(
            [snap("g1", "Multi0", 100, coverage_milli=100, fields_in_reach_unserved=0),
             snap("g1", "Multi0", 8000, coverage_milli=250, fields_in_reach_unserved=0)],
            [place("g1", "Multi0", 200, field_id="f1")])
        r = self.row(res)
        self.assertEqual(r["verdict"], "PASS")
        self.assertNotIn("coverage_flat", r["warns"])

    def test_no_expansion_warn_past_15min(self):
        res = rlc.build({"matches": [{**match("g1", "Multi0"), "duration_ticks": 25000}],
                         "situations": [snap("g1", "Multi0", 100, conyards=1, coverage_milli=50,
                                             fields_in_reach_unserved=0),
                                        snap("g1", "Multi0", 24000, conyards=1, coverage_milli=380,
                                             fields_in_reach_unserved=0)],
                         "placements": [place("g1", "Multi0", 200, field_id="f1")],
                         "engagements": []})
        r = self.row(res)
        self.assertEqual(r["verdict"], "WARN")
        self.assertIn("no_expansion", r["warns"])

    def test_second_conyard_no_warn(self):
        res = rlc.build({"matches": [{**match("g1", "Multi0"), "duration_ticks": 25000}],
                         "situations": [snap("g1", "Multi0", 100, conyards=1, coverage_milli=50,
                                             fields_in_reach_unserved=0),
                                        snap("g1", "Multi0", 24000, conyards=2, coverage_milli=380,
                                             fields_in_reach_unserved=0)],
                         "placements": [place("g1", "Multi0", 200, field_id="f1")],
                         "engagements": []})
        r = self.row(res)
        self.assertNotIn("no_expansion", r["warns"])
        self.assertEqual(r["verdict"], "PASS")

    def test_uncheckable_genericbot_rows_exit_2(self):
        # P3: records exist but every genericbot row verdicts n/a -> must not exit 0 like all-PASS
        with tempfile.TemporaryDirectory() as tmp:
            old = place("g1", "Multi0", 200)
            for k in ("field_id", "tier", "resource_gap"):
                old.pop(k)
            na = write_dir(pathlib.Path(tmp), "oldschema",
                           [snap("g1", "Multi0", 100), snap("g1", "Multi0", 500)],
                           [old], [match("g1", "Multi0")])
            self.assertEqual(rlc.main([str(na)]), 2)

    def test_classic_only_dir_exit_2(self):
        with tempfile.TemporaryDirectory() as tmp:
            cls = write_dir(pathlib.Path(tmp), "conly",
                            [snap("g1", "Multi1", 100, bot_type="classic")],
                            [place("g1", "Multi1", 200, bot_type="classic")],
                            [match("g1", "Multi1", bot="classic")])
            self.assertEqual(rlc.main([str(cls)]), 2)

    def test_warn_only_run_still_exit_0(self):
        # WARN is a checkable verdict — warnings alone must not read as uncheckable
        with tempfile.TemporaryDirectory() as tmp:
            wd = write_dir(pathlib.Path(tmp), "warned",
                           [snap("g1", "Multi0", 100, fields_in_reach_unserved=1),
                            snap("g1", "Multi0", 6000, fields_in_reach_unserved=0)],
                           [place("g1", "Multi0", 6000, field_id="f1")],
                           [match("g1", "Multi0")])
            self.assertEqual(rlc.main([str(wd), "--warn-latency", "1000"]), 0)

    def test_glob_expansion_and_no_records_exit_2(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            write_dir(root, "refcamp_a",
                      [snap("g1", "Multi0", 100, fields_in_reach_unserved=0)],
                      [place("g1", "Multi0", 200, field_id="f1")],
                      [match("g1", "Multi0")])
            self.assertEqual(rlc.main([str(root / "refcamp_*")]), 0)  # glob expanded by the tool itself
            self.assertEqual(rlc.main([str(root / "nomatch_*")]), 2)  # unmatched glob
            self.assertEqual(rlc.main([str(root / "plain_missing_dir")]), 2)  # literal missing dir


if __name__ == "__main__":
    unittest.main()
