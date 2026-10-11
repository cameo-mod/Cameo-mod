"""Offline checks for tools/ai/map_engine_acceptance.py; nothing here launches a game."""
import pathlib
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))
import ab_campaign_pilot as pilot
import map_engine_acceptance as acceptance


def seat(home, loc, side, arm, faction="td_gdi"):
    return {"home": home, "home_location": loc, "side": side, "arm": arm,
            "bot_type": "hard_" + arm, "faction": faction}


def record(planned, me):
    own = next(s for s in planned if s["home_location"] == me)
    ally = [{"home": s["home_location"]} for s in planned if s["side"] == own["side"] and s is not own]
    opp = [{"home": s["home_location"]} for s in planned if s["side"] != own["side"]]
    return {"player": {"home": me, "bot_type": own["bot_type"], "faction": own["faction"], "team": 0},
            "allies": ally, "opponents": opp}


PLANNED = [seat("Multi0", "1,1", "A", "control"), seat("Multi1", "2,2", "A", "control"),
           seat("Multi2", "9,9", "B", "treatment"), seat("Multi3", "8,8", "B", "treatment")]


class SeatProofTests(unittest.TestCase):
    def test_exact_join_on_spawn_coordinates_passes(self):
        records = [record(PLANNED, s["home_location"]) for s in PLANNED]
        self.assertEqual([], acceptance.seat_proof(PLANNED, records))

    def test_wrong_bot_missing_seat_and_crossed_alliance_fail(self):
        records = [record(PLANNED, s["home_location"]) for s in PLANNED]
        records[0]["player"]["bot_type"] = "hard_treatment"
        self.assertTrue(any("engine seated" in p for p in acceptance.seat_proof(PLANNED, records)))
        self.assertTrue(acceptance.seat_proof(PLANNED, records[:3]))
        records = [record(PLANNED, s["home_location"]) for s in PLANNED]
        records[1]["allies"], records[1]["opponents"] = records[1]["opponents"], records[1]["allies"]
        self.assertTrue(any("planned sides" in p for p in acceptance.seat_proof(PLANNED, records)))

    def test_pair_proof_requires_arm_swap_and_mirror_factions(self):
        game0 = {"game_in_pair": 0, "seats": PLANNED}
        swapped = [dict(s, arm="treatment" if s["arm"] == "control" else "control") for s in PLANNED]
        self.assertEqual([], acceptance.pair_proof(game0, {"game_in_pair": 1, "seats": swapped}))
        self.assertTrue(acceptance.pair_proof(game0, {"game_in_pair": 1, "seats": PLANNED}))
        mixed = [dict(s, faction="td_nod") if s["home"] == "Multi3" else s for s in swapped]
        self.assertTrue(any("different factions" in p
                            for p in acceptance.pair_proof(game0, {"game_in_pair": 1, "seats": mixed})))

    def test_preflight_cap_rewrites_the_generated_monitor_only(self):
        with tempfile.TemporaryDirectory() as d:
            variant = pathlib.Path(d)
            (variant / "duel_rules.yaml").write_text("World:\n\tFoo:\n", encoding="utf-8")
            pilot.install_campaign_monitor(variant, 2)
            acceptance.set_monitor_cap(variant, 1500)
            lua = (variant / "ab_campaign_monitor.lua").read_text(encoding="utf-8")
            self.assertIn("local CapTick = 1500\n", lua)
            self.assertIn("local SampleInterval = 500\n", lua)
            (variant / "ab_campaign_monitor.lua").write_text("print(1)\n", encoding="utf-8")
            with self.assertRaises(ValueError):
                acceptance.set_monitor_cap(variant, 1500)


class DualArmRulesTests(unittest.TestCase):
    def _tree(self, d, changed):
        root = pathlib.Path(d) / "root"
        (root / "mods/cameo/ai").mkdir(parents=True)
        (root / "mods/cameo/ai/ai.yaml").write_text("Player:\n\tModularBot@HardAI:\n\t\tType: hard\n", encoding="utf-8")
        (root / "mods/cameo/ai/fransbot.yaml").write_text("World:\n", encoding="utf-8")
        overlay = pathlib.Path(d) / "overlay"
        (overlay / "mods/cameo/ai").mkdir(parents=True)
        (overlay / "mods/cameo/ai/ai.yaml").write_text(changed, encoding="utf-8")
        (overlay / "mods/cameo/ai/fransbot.yaml").write_text("World:\n", encoding="utf-8")
        variant = pathlib.Path(d) / "variant"
        variant.mkdir()
        (variant / "map.yaml").write_text("Title: x\nRules: duel_rules.yaml\nPlayers:\n", encoding="utf-8")
        return root, overlay, variant

    def test_changed_ai_yaml_is_listed_before_the_harness_rules(self):
        with tempfile.TemporaryDirectory() as d:
            arm = "Player:\n\tModularBot@HardAI:\n\t\tType: hard\n\tModularBot@HardControlAI:\n\t\tType: hard_control\n"
            root, overlay, variant = self._tree(d, arm)
            installed = pilot.install_dual_arm_rules(variant, overlay, root)
            self.assertEqual(["campaign_ai.yaml"], list(installed))
            self.assertIn("Rules: campaign_ai.yaml, duel_rules.yaml\n", (variant / "map.yaml").read_text(encoding="utf-8"))
            self.assertEqual(arm, (variant / "campaign_ai.yaml").read_text(encoding="utf-8"))
            with self.assertRaises(ValueError):
                pilot.install_dual_arm_rules(variant, overlay, root)

    def test_removal_or_no_change_is_refused(self):
        with tempfile.TemporaryDirectory() as d:
            root, overlay, variant = self._tree(d, "Player:\n\t-ModularBot@HardAI:\n")
            with self.assertRaises(ValueError):
                pilot.install_dual_arm_rules(variant, overlay, root)
        with tempfile.TemporaryDirectory() as d:
            root, overlay, variant = self._tree(d, "Player:\n\tModularBot@HardAI:\n\t\tType: hard\n")
            with self.assertRaises(ValueError):
                pilot.install_dual_arm_rules(variant, overlay, root)


class AdmissionAndStopTests(unittest.TestCase):
    """F1 (REVIEW_2026-10-11_ccr_b337_luna.md): admission must fail closed on unknown RAM,
    and the in-run stop must check the private cap before the host emergency floor."""

    def test_unknown_free_ram_fails_closed_at_admission(self):
        reason = acceptance.admission_decision(None, 13.5 * 1024**3)
        self.assertIsNotNone(reason)
        self.assertIn("unknown", reason)

    def test_admission_boundary_is_inclusive_of_the_floor(self):
        floor = 13.5 * 1024**3
        self.assertIsNone(acceptance.admission_decision(int(floor), floor))
        self.assertIsNotNone(acceptance.admission_decision(int(floor) - 1, floor))

    def test_unmeasurable_private_bytes_stops_fail_closed(self):
        self.assertEqual("MEMORY_SAMPLE_FAILED",
            acceptance.run_stop_reason(None, 10 * 1024**3, private_cap=acceptance.PRIVATE_CAP_BYTES, emergency_free=3 * 1024**3))

    def test_private_cap_stops_before_the_host_floor_is_even_checked(self):
        # available RAM here is healthy; only the process's own private-byte cap should trip.
        self.assertEqual("MEMORY_KILL",
            acceptance.run_stop_reason(acceptance.PRIVATE_CAP_BYTES, 20 * 1024**3,
                private_cap=acceptance.PRIVATE_CAP_BYTES, emergency_free=3 * 1024**3))

    def test_host_emergency_floor_stops_even_under_the_private_cap(self):
        self.assertEqual("LOW_FREE_RAM",
            acceptance.run_stop_reason(1 * 1024**3, 2 * 1024**3,
                private_cap=acceptance.PRIVATE_CAP_BYTES, emergency_free=3 * 1024**3))

    def test_healthy_readings_do_not_stop(self):
        self.assertIsNone(acceptance.run_stop_reason(1 * 1024**3, 20 * 1024**3,
            private_cap=acceptance.PRIVATE_CAP_BYTES, emergency_free=3 * 1024**3))

    def test_default_admission_floor_matches_the_bootstrapped_peak_plus_reserve(self):
        self.assertEqual(13.5 * 1024**3, acceptance.BOOTSTRAP_HEADLESS_PEAK_BYTES + acceptance.ADMISSION_RESERVE_BYTES)
        self.assertEqual(10.5 * 1024**3, acceptance.PRIVATE_CAP_BYTES)


class OwnedProcessMonitoringTests(unittest.TestCase):
    def test_owned_tree_includes_children_and_excludes_unrelated_processes(self):
        rows = [
            {"ProcessId": 10, "ParentProcessId": 1},
            {"ProcessId": 1, "ParentProcessId": 0},
            {"ProcessId": 12, "ParentProcessId": 10},
            {"ProcessId": 13, "ParentProcessId": 99},
        ]
        self.assertEqual({1, 10, 12}, {int(r["ProcessId"]) for r in acceptance.owned_tree_rows(1, rows)})

    def test_sample_aggregates_launcher_and_game_and_fails_closed(self):
        rows = [{"ProcessId": 1}, {"ProcessId": 2}]
        with patch.object(acceptance, "private_bytes", side_effect=[100, 200]), \
             patch.object(acceptance, "rss_bytes", side_effect=[30, 40]):
            self.assertEqual({"pids": [1, 2], "private_bytes": 300, "working_set_bytes": 70},
                acceptance.sample_owned_tree(rows))
        with patch.object(acceptance, "private_bytes", side_effect=[100, None]), \
             patch.object(acceptance, "rss_bytes", side_effect=[30, 40]):
            with self.assertRaisesRegex(RuntimeError, "required memory sample missing for owned PID 2"):
                acceptance.sample_owned_tree(rows)

    def test_windows_child_discovery_requires_engine_descendant(self):
        with tempfile.TemporaryDirectory() as d:
            root = pathlib.Path(d) / "engine"
            (root / "bin").mkdir(parents=True)
            support = pathlib.Path(d) / "cell/Support"
            support.mkdir(parents=True)
            rows = [
                {"ProcessId": 1, "ParentProcessId": 0, "ExecutablePath": str(root / "bin/OpenRA.exe"),
                 "CommandLine": f"OpenRA.exe Engine.SupportDir={support}"},
                {"ProcessId": 2, "ParentProcessId": 1, "ExecutablePath": str(root / "bin/OpenRA.Game.exe"),
                 "CommandLine": "OpenRA.Game.exe"},
            ]

            class Popen:
                pid = 1
                def poll(self): return None

            with patch.object(acceptance.os, "name", "nt"), patch.object(acceptance, "process_rows", return_value=rows):
                result = acceptance.discover_owned_tree(Popen(), root, support)
            self.assertTrue(result["child_found"])
            self.assertEqual({1, 2}, {int(r["ProcessId"]) for r in result["rows"]})

    def test_windows_missing_game_child_is_not_accepted_as_discovered(self):
        with tempfile.TemporaryDirectory() as d:
            root = pathlib.Path(d) / "engine"
            (root / "bin").mkdir(parents=True)
            support = pathlib.Path(d) / "cell/Support"
            support.mkdir(parents=True)
            rows = [{"ProcessId": 1, "ParentProcessId": 0, "ExecutablePath": str(root / "bin/OpenRA.exe"),
                "CommandLine": f"OpenRA.exe Engine.SupportDir={support}"}]

            class Popen:
                pid = 1
                def poll(self): return None

            with patch.object(acceptance.os, "name", "nt"), patch.object(acceptance, "process_rows", return_value=rows):
                result = acceptance.discover_owned_tree(Popen(), root, support)
            self.assertFalse(result["child_found"])

    def test_exited_launcher_still_discovers_and_pid_scoped_cleans_same_cell_child(self):
        with tempfile.TemporaryDirectory() as d:
            root = pathlib.Path(d) / "engine"
            (root / "bin").mkdir(parents=True)
            support = pathlib.Path(d) / "cell/Support"
            support.mkdir(parents=True)
            child = {"ProcessId": 2, "ParentProcessId": 1,
                "ExecutablePath": str(root / "bin/OpenRA.Game.exe"),
                "CommandLine": f"OpenRA.Game.exe Engine.SupportDir={support}"}

            class Popen:
                pid = 1
                def poll(self): return 0  # exited before the first monitor sample

            calls = []
            def run(args, **kwargs):
                calls.append(args)
                return type("Result", (), {"returncode": 0, "stdout": "", "stderr": ""})()

            with patch.object(acceptance.os, "name", "nt"), \
                 patch.object(acceptance, "process_rows", side_effect=[[child], [child], []]), \
                 patch.object(acceptance.subprocess, "run", side_effect=run):
                tree = acceptance.discover_owned_tree(Popen(), root, support)
                self.assertTrue(tree["child_found"])
                self.assertEqual([2], [int(row["ProcessId"]) for row in tree["rows"]])
                remaining = acceptance.cleanup_orphaned_owned_pids({2}, root, support)

            self.assertEqual([], remaining)
            self.assertEqual([["taskkill.exe", "/PID", "2", "/T", "/F"]], calls)

    def test_tree_cleanup_uses_root_pid_and_tree_scope(self):
        with tempfile.TemporaryDirectory() as d:
            root = pathlib.Path(d) / "engine"
            (root / "bin").mkdir(parents=True)
            support = pathlib.Path(d) / "cell/Support"
            support.mkdir(parents=True)
            rows = [
                {"ProcessId": 1, "ParentProcessId": 0, "ExecutablePath": str(root / "bin/OpenRA.exe"),
                 "CommandLine": f"OpenRA.exe Engine.SupportDir={support}"},
                {"ProcessId": 2, "ParentProcessId": 1, "ExecutablePath": str(root / "bin/OpenRA.Game.exe"),
                 "CommandLine": f"OpenRA.Game.exe Engine.SupportDir={support}"},
            ]

            class Popen:
                pid = 1
                returncode = None
                def poll(self): return self.returncode
                def wait(self, timeout=None): self.returncode = -9

            proc = Popen()
            calls = []
            def run(args, **kwargs):
                calls.append(args)
                return type("Result", (), {"returncode": 0, "stdout": "", "stderr": ""})()
            with patch.object(acceptance.os, "name", "nt"), \
                 patch.object(acceptance, "process_rows", side_effect=[rows, []]), \
                 patch.object(acceptance.subprocess, "run", side_effect=run):
                acceptance.terminate_owned_tree(proc, root, support)
            self.assertEqual(["taskkill.exe", "/PID", "1", "/T", "/F"], calls[0])


if __name__ == "__main__":
    unittest.main()
