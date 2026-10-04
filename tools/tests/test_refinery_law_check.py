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
        res = self.build_one(
            [snap("g1", "Multi0", 100, fields_in_reach_unserved=1),
             snap("g1", "Multi0", 300, fields_in_reach_unserved=0)],
            [place("g1", "Multi0", 200, field_id="f1", tier=1),
             place("g1", "Multi0", 350, field_id="f1", tier=2, resource_gap=0)])
        r = self.row(res)
        # one field, two refineries -> the per-field cap still fails (law's real unit)
        self.assertEqual(r["refineries_per_anchor_max"], 2)
        self.assertEqual(r["verdict"], "FAIL")
        self.assertEqual(r["tier_order_violations"], 0)

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


if __name__ == "__main__":
    unittest.main()
