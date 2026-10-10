import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("mcv_health", ROOT / "tools/ai/mcv_health.py")
MCV = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MCV)


def row(seq, tick, kind, actors=None):
    return {"schema": "cameo-mcv-health", "schema_version": 1, "game_uid": "game", "player": "player",
            "seq": seq, "world_tick": tick, "kind": kind, "player_active": True, "supported": True,
            "observation_complete": True, "dropped": 0, "intent_complete": True, "order_complete": True,
            "hold_complete": True, "transform_complete": True, "complete": True,
            "role_map": {"configured-vehicle": "construction_mcv"}, "actors": actors or []}


def actor(tick, hold="none", activity="idle"):
    return {"actor_id": 1, "actor_type": "configured-vehicle", "role": "construction_mcv",
            "x": 0, "y": 0, "live": True, "in_world": True, "activity": activity, "hold": hold,
            "observation_tick": tick, "idle_since_tick": 0 if activity == "idle" else None,
            "last_cell_change_tick": 0, "order_requested": False, "order_accepted": False,
            "site_intent": False, "proven_transform_actor_id": None}


class McvHealthTest(unittest.TestCase):
    def check_rows(self, rows, idle_ticks=100):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "mcv.jsonl"
            data = "".join(json.dumps(r) + "\n" for r in rows).encode()
            path.write_bytes(data)
            report = MCV.analyze(path, "game", "player", idle_ticks)
            self.assertEqual(path.read_bytes(), data)
            self.assertEqual(report["input"]["consumed_sha256"], hashlib.sha256(data).hexdigest())
            self.assertTrue(report["diagnostic_only"])
            return report

    def test_explicit_no_actor_census_is_not_inferred_deployment(self):
        r = self.check_rows([row(0, 0, "start"), row(1, 50, "end")])
        self.assertEqual(r["status"], "NO_OBSERVED_SYMPTOM")
        self.assertEqual(r["findings"], [])

    def test_idle_is_diagnostic_and_intentional_hold_is_retained(self):
        rows = [row(i, t, kind, [actor(t, hold="policy_hold")])
                for i, (t, kind) in enumerate([(0, "start"), (50, "pulse"), (100, "end")])]
        r = self.check_rows(rows)
        self.assertEqual(r["status"], "DIAGNOSTIC")
        self.assertEqual(r["findings"][0]["hold"], "policy_hold")
        self.assertIsNone(r["findings"][0]["causal_conclusion"])

    def test_single_idle_busy_and_inactive_do_not_make_idle_symptom(self):
        for active, activity in [(True, "idle"), (True, "deploying"), (False, "idle")]:
            rows = [row(0, 0, "start", [actor(0, activity=activity)]),
                    row(1, 50, "end", [actor(50, activity=activity)])]
            for r in rows:
                r["player_active"] = active
            self.assertEqual(self.check_rows(rows)["findings"], [])

    def test_missing_hooks_are_unknown_even_with_long_observed_idle(self):
        rows = [row(i, t, kind, [actor(t, hold="unknown")])
                for i, (t, kind) in enumerate([(0, "start"), (50, "pulse"), (100, "end")])]
        for r in rows:
            r["order_complete"] = False
        report = self.check_rows(rows)
        self.assertEqual(report["status"], "UNKNOWN")
        self.assertEqual(len(report["findings"]), 1)

    def test_late_gap_truncated_duplicate_and_bad_codes_fail_unknown(self):
        base = [row(0, 0, "start", [actor(0)]), row(1, 50, "end", [actor(50)])]
        cases = []
        late = copy.deepcopy(base); late[0]["world_tick"] = 1; cases.append(late)
        gap = copy.deepcopy(base); gap[1]["world_tick"] = 100; cases.append(gap)
        cases.append(base[:1])
        duplicate = copy.deepcopy(base); duplicate[1]["seq"] = 0; cases.append(duplicate)
        bad = copy.deepcopy(base); bad[0]["actors"][0]["activity"] = "teleported"; cases.append(bad)
        overflow = copy.deepcopy(base); overflow[0]["actors"] *= 65; cases.append(overflow)
        unknown = copy.deepcopy(base); unknown[0]["role_map"]["configured-vehicle"] = "unknown"; cases.append(unknown)
        for rows in cases:
            self.assertEqual(self.check_rows(rows)["status"], "UNKNOWN")

    def test_disappearance_and_unproven_deployed_are_distinct(self):
        report = self.check_rows([row(0, 0, "start", [actor(0)]), row(1, 50, "end")])
        self.assertEqual(report["findings"], [])
        deployed = actor(50, activity="deployed")
        report = self.check_rows([row(0, 0, "start", [actor(0)]), row(1, 50, "end", [deployed])])
        self.assertEqual(report["status"], "UNKNOWN")

    def test_actual_csharp_path_cli_unknown_and_immutable(self):
        pointer = ROOT / "engine/bin/TestResults/mcv-health-cli-input.txt"
        self.assertTrue(pointer.exists(), "Run McvHealthObservationTest first")
        support = Path(pointer.read_text(encoding="utf-8-sig"))
        path = support / "Logs/cameo-ai-mcv-health.jsonl"
        data = path.read_bytes()
        result = subprocess.run([sys.executable, str(ROOT / "tools/ai/mcv_health.py"), str(support),
                                 "--game-uid", "game", "--player", "player", "--idle-ticks", "100"],
                                capture_output=True, text=True, timeout=15)
        self.assertEqual(result.returncode, 21, result.stderr)
        report = json.loads(result.stdout)
        self.assertEqual(report["status"], "UNKNOWN")
        self.assertEqual(len(report["findings"]), 1)
        self.assertEqual(Path(report["input"]["path"]), path)
        self.assertEqual(report["input"]["consumed_sha256"], hashlib.sha256(data).hexdigest())
        self.assertEqual(path.read_bytes(), data)

    def test_missing_canonical_path_cli_returns_unknown21(self):
        with tempfile.TemporaryDirectory() as tmp:
            result = subprocess.run([sys.executable, str(ROOT / "tools/ai/mcv_health.py"), tmp,
                                     "--game-uid", "game", "--player", "player"],
                                    capture_output=True, text=True, timeout=15)
            self.assertEqual(result.returncode, 21)
            self.assertEqual(json.loads(result.stdout)["status"], "UNKNOWN")


if __name__ == "__main__":
    unittest.main()
