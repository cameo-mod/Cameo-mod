import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))
import a5_memory_parity_probe as probe
import ab_campaign_pilot as pilot


class FixedTickParityTests(unittest.TestCase):
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
            with self.assertRaises(ValueError):
                pilot.install_campaign_monitor(dest, 2, cap_tick=0)


if __name__ == "__main__":
    unittest.main()
