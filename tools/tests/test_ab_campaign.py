import hashlib
import json
import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))
import ab_campaign as ab


def manifest():
    return {
        "schema": 1,
        "campaign_id": "fixture-campaign-v1",
        "execution_approved": False,
        "pairs_per_stratum": 20,
        "team_size": 1,
        "bot_type": "hard",
        "seeds": list(range(20261001, 20261021)),
        "switches": list(ab.SWITCHES),
        "map_strata": [{"name": n, "path": p, "sha256": h} for n, (p, h) in ab.MAPS.items()],
        "pins": {"source_commit": "700bb16483f6d92664153e98d6f2560c312acaab",
                 "engine_version": "6da7fce14da541180c6baddd6925118fbef65b94"},
        "world_tick_cap": 45000,
        "wall_timeout_s": 3000,
        "stall_timeout_s": 180,
        "memory_limit_bytes": 8 * 1024**3,
        "serial_workers": 1,
    }


def records(uid="g-1"):
    return [
        {"game_uid": uid, "player": {"home": "2,2", "bot_type": "hard", "faction": "td_gdi", "outcome": "won"}, "stats_timeline": [{"tick": 45000, "earned": 100, "spent": 80, "banked": 20}]},
        {"game_uid": uid, "player": {"home": "20,20", "bot_type": "hard", "faction": "td_gdi", "outcome": "lost"}, "stats_timeline": [{"tick": 45000, "earned": 90, "spent": 70, "banked": 20}]},
    ]


def receipt(status="NATURAL_END", cap=False):
    return {"game_uid": "g-1", "status": status, "end_reason": "natural" if status == "NATURAL_END" else None,
            "map": "small_gate", "map_package_sha256": ab.MAPS["small_gate"][1], "seed": 20261001,
            "invocation_seed": 20261001, "server_seed": 20261001,
            "server_seed_log_line": "CAMEO DEV SEED pinned - RandomSeed=20261001 (parity harness)",
            "manifest_sha256": "b" * 64,
            "seat_proof_source": "generated_map.yaml", "map_yaml_sha256": "a" * 64,
            "seats": [{"home": "2,2", "arm": "treatment", "bot_type": "hard", "faction": "td_gdi"},
                      {"home": "20,20", "arm": "control", "bot_type": "hard", "faction": "td_gdi"}],
            "cap_marker": cap, "cap_tick": 45000 if cap else None, "world_tick_cap": 45000,
            "support_complete": True, "replay_complete": True}


class CampaignTests(unittest.TestCase):
    def test_manifest_map_pins_and_plan_size(self):
        m = manifest()
        self.assertTrue(ab.validate_manifest(m, ROOT))
        jobs = ab.make_jobs(m)
        self.assertEqual(360, len(jobs))
        self.assertEqual(180, len({(j["switch"], j["map"], j["pair"]) for j in jobs}))
        self.assertEqual(9, sum(j["game_in_pair"] == 0 and j["pair"] == 0 for j in jobs))
        self.assertEqual(jobs, ab.make_jobs(m))

    def test_manifest_rejects_map_tamper_and_missing_seed(self):
        m = manifest()
        m["map_strata"][0]["sha256"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "map strata"):
            ab.validate_manifest(m, ROOT)
        m = manifest()
        m["seeds"] = m["seeds"][:-1]
        with self.assertRaisesRegex(ValueError, "20 distinct"):
            ab.make_jobs(m)

    def test_manifest_requires_exact_serial_bounded_design(self):
        for field, value in (("serial_workers", 2), ("memory_limit_bytes", 9 * 1024**3),
                             ("world_tick_cap", 15000), ("pairs_per_stratum", 19)):
            m = manifest(); m[field] = value
            with self.subTest(field=field), self.assertRaises(ValueError):
                ab.validate_manifest(m, ROOT)

    def test_natural_game_needs_generated_seat_proof(self):
        r = receipt()
        verdict, _ = ab.classify_game(records(), r)
        self.assertEqual("TREATMENT_WIN", verdict)
        r["seat_proof_source"] = "requested_cli"
        self.assertEqual("INVALID_UNKNOWN", ab.classify_game(records(), r)[0])

    def test_seat_record_disagreement_is_unknown(self):
        rows = records(); rows[0]["player"]["faction"] = "td_nod"
        self.assertEqual("INVALID_UNKNOWN", ab.classify_game(rows, receipt())[0])

    def test_cap_requires_marker_support_and_replay(self):
        self.assertEqual("CENSORED_CAP", ab.classify_game(records(), receipt("CENSORED_CAP", True))[0])
        r = receipt("CENSORED_CAP", True); r["replay_complete"] = False
        self.assertEqual("INVALID_UNKNOWN", ab.classify_game(records(), r)[0])
        r = receipt("CENSORED_CAP", False)
        self.assertEqual("INVALID_UNKNOWN", ab.classify_game(records(), r)[0])

    def test_incomplete_never_scores(self):
        self.assertEqual("INCOMPLETE_UNKNOWN", ab.classify_game(records(), receipt("MEMORY_KILL"))[0])

    def test_seed_and_map_provenance_are_mandatory(self):
        r = receipt(); r["server_seed"] += 1
        self.assertEqual("INVALID_UNKNOWN", ab.classify_game(records(), r)[0])
        r = receipt(); r["map_package_sha256"] = "0" * 64
        self.assertEqual("INVALID_UNKNOWN", ab.classify_game(records(), r)[0])

    def test_pair_effect_requires_and_uses_side_swap(self):
        r0 = receipt(); r0.update({"switch": "BU_harvester_logistics", "map": "small_gate", "seed": 20261001,
                                  "pair": 0, "game_in_pair": 0, "control_side": "A", "treatment_side": "B"})
        r1 = receipt(); r1.update({"game_uid": "g-2", "switch": "BU_harvester_logistics", "map": "small_gate",
                                  "seed": 20261001, "pair": 0, "game_in_pair": 1, "control_side": "B", "treatment_side": "A",
                                  "seats": [{"home": "2,2", "arm": "control", "bot_type": "hard", "faction": "td_gdi"},
                                            {"home": "20,20", "arm": "treatment", "bot_type": "hard", "faction": "td_gdi"}]})
        rows = records() + [
            {"game_uid": "g-2", "player": {"home": "2,2", "bot_type": "hard", "faction": "td_gdi", "outcome": "lost"}, "stats_timeline": []},
            {"game_uid": "g-2", "player": {"home": "20,20", "bot_type": "hard", "faction": "td_gdi", "outcome": "won"}, "stats_timeline": []},
        ]
        result = ab.summarize(rows, {"g-1": r0, "g-2": r1})
        self.assertEqual(1.0, result["strata"][0]["paired_win_effect"])
        self.assertEqual(1, result["strata"][0]["complete_natural_pairs"])
        r1["control_side"], r1["treatment_side"] = "A", "B"
        invalid = ab.summarize(rows, {"g-1": r0, "g-2": r1})
        self.assertEqual(0, invalid["strata"][0]["complete_natural_pairs"])
        self.assertEqual("INVALID_UNKNOWN", invalid["games"][1]["verdict"])

    def test_duplicate_json_keys_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            p = pathlib.Path(d) / "x.json"
            p.write_text('{"schema":1,"schema":1}', encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "duplicate"):
                ab.read_json(p)

    def test_no_launch_dry_run_is_360_games(self):
        with tempfile.TemporaryDirectory() as d:
            d = pathlib.Path(d)
            m = manifest()
            path = d / "manifest.json"
            path.write_text(json.dumps(m, separators=(",", ":")), encoding="utf-8")
            out = d / "dryrun.json"
            import subprocess
            cmd = [sys.executable, str(ROOT / "tools" / "ai" / "ab_campaign.py"), "dry-run",
                   "--manifest", str(path), "--manifest-sha256", hashlib.sha256(path.read_bytes()).hexdigest(),
                   "--repo-root", str(ROOT), "--output", str(out)]
            p = subprocess.run(cmd, capture_output=True, text=True, timeout=30)
            self.assertEqual(0, p.returncode, p.stderr)
            result = json.loads(out.read_text(encoding="utf-8"))
            self.assertEqual("NO_LAUNCH_DRY_RUN", result["mode"])
            self.assertEqual(360, result["games"])
            self.assertEqual(56.0, result["wall_estimate_hours"])
            self.assertEqual(0, result["launches"])
            self.assertFalse(result["execution_authorized"])
            self.assertEqual("fixture_only_not_campaign_executable", result["manifest_state"])
            self.assertEqual(3, len(result["blocking_gates"]))


if __name__ == "__main__":
    unittest.main()
