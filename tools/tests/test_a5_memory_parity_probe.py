import pathlib
import sys
import tempfile
import unittest
from unittest import mock

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))
import a5_memory_parity_probe as probe
import ab_campaign_pilot as pilot
import run_ai_match_batch as batch


class FixedTickParityTests(unittest.TestCase):
    def test_qualification_launch_is_headless_and_seeded(self):
        with tempfile.TemporaryDirectory() as temp:
            with mock.patch.object(probe, "OUT", pathlib.Path(temp)):
                with mock.patch.object(probe.subprocess, "Popen", side_effect=RuntimeError("launch intercepted")) as launch:
                    with self.assertRaisesRegex(RuntimeError, "launch intercepted"):
                        probe.run_cell(1)
        command = launch.call_args.args[0]
        self.assertEqual(command[command.index("--render") + 1], "headless")
        self.assertEqual(launch.call_args.kwargs["env"]["CAMEO_DEV_SEED"], str(probe.SEED))

    def test_temporary_map_template_requires_opt_in_and_fixed_layout(self):
        with tempfile.TemporaryDirectory() as temp:
            dest = pathlib.Path(temp)
            for name in ("map.bin", "map.png"):
                (dest / name).write_bytes(b"fixture")
            (dest / "map.yaml").write_text("PlayerReference@BotA:\nPlayerReference@BotB:\n", encoding="utf-8")
            (dest / "rules.yaml").write_text("World:\n\tTimeLimitDefault: 1\n", encoding="utf-8")
            self.assertIn("only the bundled", batch.legacy_template_dir_error(dest))
            self.assertIsNone(batch.legacy_template_dir_error(dest, allow_temporary=True))
            (dest / "rules.yaml").write_text("World:\n", encoding="utf-8")
            self.assertIn("TimeLimitDefault", batch.legacy_template_dir_error(dest, allow_temporary=True))

    def test_censored_cap_requires_marker_and_complete_mutual_loss(self):
        rows = [{"duration_ticks": 16002, "player": {"outcome": "lost"}} for _ in range(2)]
        self.assertEqual((True, 16001), probe.verify_censored_cap("AB_CAMPAIGN_CAP tick=16001", rows, 16000))
        self.assertFalse(probe.verify_censored_cap("", rows, 16000)[0])
        self.assertFalse(probe.verify_censored_cap("AB_CAMPAIGN_CAP tick=16001", rows[:1], 16000)[0])
        mixed = [rows[0], {"duration_ticks": 16002, "player": {"outcome": "won"}}]
        self.assertFalse(probe.verify_censored_cap("AB_CAMPAIGN_CAP tick=16001", mixed, 16000)[0])

    def test_monitor_accepts_bounded_parity_cap_and_rejects_bad_bounds(self):
        with tempfile.TemporaryDirectory() as temp:
            dest = pathlib.Path(temp)
            (dest / "rules.yaml").write_text("World:\n", encoding="utf-8")
            receipt = pilot.install_campaign_monitor(dest, 2, cap_tick=16000, sample_interval=2000,
                rules_filename="rules.yaml", seat_names=["BotA", "BotB"])
            self.assertEqual(16000, receipt["cap_tick"])
            lua = (dest / "ab_campaign_monitor.lua").read_text(encoding="utf-8")
            self.assertIn("local CapTick = 16000", lua)
            self.assertIn('"BotA", "BotB", "Neutral", "Creeps"', lua)
            self.assertIn("local DeploymentTick = 100", lua)
            self.assertIn("if not Deployments[name] then", lua)
            self.assertIn("VerifiedConstructionYards[actor.Type]", lua)
            self.assertIn('AB_CAMPAIGN_DEPLOYMENT_CHECK tick=', lua)
            self.assertIn('AB_CAMPAIGN_NO_DEPLOY seat=', lua)
            with self.assertRaises(ValueError):
                pilot.install_campaign_monitor(dest, 2, cap_tick=0)


if __name__ == "__main__":
    unittest.main()
