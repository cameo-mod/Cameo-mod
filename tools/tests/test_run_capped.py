import json
import pathlib
import subprocess
import sys
import tempfile
import time
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))
import run_capped


class RunCappedTests(unittest.TestCase):
	def test_zero_exit_is_completed_and_writes_timestamps_and_code(self):
		with tempfile.TemporaryDirectory() as temp:
			status_path = pathlib.Path(temp) / "status.json"
			status = run_capped.run_capped([sys.executable, "-c", "pass"], status_path, 5)
			receipt = json.loads(status_path.read_text(encoding="utf-8"))

		self.assertEqual(status["status"], "COMPLETED")
		self.assertEqual(receipt["status"], "COMPLETED")
		self.assertEqual(receipt["exit_code"], 0)
		self.assertTrue(receipt["started_at_utc"].endswith("Z"))
		self.assertTrue(receipt["ended_at_utc"].endswith("Z"))

	def test_nonzero_exit_is_crashed_and_not_success(self):
		with tempfile.TemporaryDirectory() as temp:
			status_path = pathlib.Path(temp) / "status.json"
			status = run_capped.run_capped([sys.executable, "-c", "raise SystemExit(7)"], status_path, 5)

		self.assertEqual(status["status"], "CRASHED")
		self.assertEqual(status["exit_code"], 7)

	def test_wallcap_kills_parent_and_grandchild_and_is_nonzero(self):
		with tempfile.TemporaryDirectory() as temp:
			root = pathlib.Path(temp)
			status_path = root / "status.json"
			marker = root / "child-survived.txt"
			pid_path = root / "child.pid"
			child = (
				"import pathlib,sys,time; time.sleep(1.4); "
				"pathlib.Path(sys.argv[1]).write_text('survived', encoding='utf-8')"
			)
			parent = (
				"import pathlib,subprocess,sys,time; "
				"p=subprocess.Popen([sys.executable,'-c',sys.argv[1],sys.argv[2]]); "
				"pathlib.Path(sys.argv[3]).write_text(str(p.pid), encoding='ascii'); "
				"time.sleep(30)"
			)
			# Parent process creates a child; the cap must remove both processes.
			started = time.monotonic()
			status = run_capped.run_capped(
				[sys.executable, "-c", parent, child, str(marker), str(pid_path)], status_path, 0.8,
			)
			self.assertLess(time.monotonic() - started, 8)
			self.assertTrue(pid_path.exists(), "fake child started before the wall cap")
			self.assertEqual(status["status"], "INCOMPLETE_WALLCAP")
			self.assertTrue(status["tree_kill_succeeded"])
			self.assertEqual(json.loads(status_path.read_text(encoding="utf-8"))["status"], "INCOMPLETE_WALLCAP")
			time.sleep(1.6)
			self.assertFalse(marker.exists(), "tree kill must prevent the grandchild's delayed side effect")

	def test_cli_wallcap_returns_failure_code(self):
		with tempfile.TemporaryDirectory() as temp:
			status_path = pathlib.Path(temp) / "status.json"
			code = run_capped.main([
				"--status-file", str(status_path), "--timeout", "0.2", "--",
				sys.executable, "-c", "import time; time.sleep(10)",
			])
			self.assertEqual(code, 124)
			self.assertEqual(json.loads(status_path.read_text(encoding="utf-8"))["status"], "INCOMPLETE_WALLCAP")

if __name__ == "__main__":
	unittest.main()
