import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))
import a5_memory_parity_probe as probe


class A5MemoryProbeTests(unittest.TestCase):
	def test_host_admission_keeps_process_and_reserve_headroom(self):
		process = probe.PROCESS_STOP
		soft = probe.HOST_SOFT_STOP
		headroom = probe.HOST_LAUNCH_HEADROOM
		self.assertTrue(probe.admission_ok(process + soft + headroom, process + probe.COMMIT_HEADROOM))
		self.assertFalse(probe.admission_ok(process + soft + headroom - 1, 10**12))
		self.assertFalse(probe.admission_ok(10**12, process + probe.COMMIT_HEADROOM - 1))

	def test_soft_stop_reasons_are_independent(self):
		self.assertEqual("PROCESS_PRIVATE_SOFT_STOP", probe.stop_reason(probe.PROCESS_STOP, 20 * 1024**3))
		self.assertEqual("HOST_PHYSICAL_SOFT_STOP", probe.stop_reason(0, probe.HOST_SOFT_STOP))
		self.assertIsNone(probe.stop_reason(1, probe.HOST_SOFT_STOP + 1))

	def test_runtime_counter_command_is_one_second_csv(self):
		cmd = probe.counters_command("dotnet-counters.exe", 42, pathlib.Path("heap.csv"))
		self.assertEqual(["dotnet-counters.exe", "collect", "--process-id", "42",
			"--counters", "System.Runtime", "--refresh-interval", "1", "--format", "csv",
			"--output", "heap.csv"], cmd)

	def test_log_snapshot_reports_growth_without_inventing_actor_counts(self):
		with tempfile.TemporaryDirectory() as temp:
			support = pathlib.Path(temp)
			logs = support / "Logs"
			logs.mkdir()
			(logs / "debug.log").write_text("[WT 10] first\n[WT 20] second\n", encoding="utf-8")
			result = probe.log_snapshot(support)
			self.assertEqual(20, result["world_tick"])
			self.assertEqual((logs / "debug.log").stat().st_size, result["log_total_bytes"])
			self.assertIsNone(result["actor_count"])
			self.assertIn("not instrumented", result["actor_count_note"])


if __name__ == "__main__":
	unittest.main()
