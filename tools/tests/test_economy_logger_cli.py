"""Consumer-fit CLI checks using the C# recorder serializer and bounded file writer.

First run the AiEconomy dotnet test filter to generate new support roots.
This suite executes immutable Git blobs from the approved economy consumer head,
not a mutable working-tree checker. It does not run an OpenRA game or driver.
"""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
PIN = "2a417dc3e20cc408bec6e1818cf5734b0431a516"


class EconomyLoggerCliTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory(prefix="economy-consumer-cli-")
        cls.consumer = Path(cls.temp.name)
        for filename in ("economy_invariants.py", "replay_health.py"):
            blob = subprocess.run(
                ["git", "show", f"{PIN}:tools/ai/{filename}"], cwd=ROOT,
                check=True, capture_output=True, timeout=15).stdout
            (cls.consumer / filename).write_bytes(blob)

    @classmethod
    def tearDownClass(cls):
        cls.temp.cleanup()

    def check_fixture(self, name, status, exit_code):
        pointer = ROOT / f"engine/bin/TestResults/economy-health-{name}-cli-input.txt"
        self.assertTrue(pointer.exists(), "Run the AiEconomy dotnet test filter first")
        support = Path(pointer.read_text(encoding="utf-8-sig"))
        emitted = support / "Logs/cameo-ai-economy-health.jsonl"
        data = emitted.read_bytes()
        digest = hashlib.sha256(data).hexdigest()
        result = subprocess.run(
            [sys.executable, str(self.consumer / "economy_invariants.py"), str(support),
             "--game-uid", "roundtrip", "--player", "hard"],
            capture_output=True, text=True, timeout=15)
        self.assertEqual(result.returncode, exit_code, result.stderr + result.stdout)
        report = json.loads(result.stdout)
        self.assertEqual(report["status"], status)
        self.assertEqual(Path(report["input"]["path"]), emitted)
        self.assertEqual(report["input"]["consumed_sha256"], digest)
        self.assertEqual(emitted.read_bytes(), data)
        return report

    def test_canonical_emitted_file_healthy_cli(self):
        self.check_fixture("schema", "OBSERVED_HEALTHY", 0)

    def test_canonical_emitted_ready_250_cli(self):
        report = self.check_fixture("ready", "BLOCK", 20)
        self.assertIn("READY_BUILDING_UNPLACED", [x["code"] for x in report["findings"]])

    def test_canonical_incomplete_cli(self):
        self.check_fixture("incomplete", "UNKNOWN", 21)

    def test_proven_active_three_cancel_cli(self):
        report = self.check_fixture("cancel-active", "BLOCK", 20)
        self.assertIn("REPEATED_BUILDING_CANCELLATION", [x["code"] for x in report["findings"]])

    def test_inactive_three_cancel_cli(self):
        self.check_fixture("cancel-inactive", "OBSERVED_HEALTHY", 0)

    def test_destruction_three_cancel_cli(self):
        self.check_fixture("cancel-destroyed", "OBSERVED_HEALTHY", 0)

    def test_duplicate_episode_is_unknown(self):
        self.check_fixture("cancel-duplicate", "UNKNOWN", 21)

    def test_unclassified_removed_coverage_is_unknown(self):
        self.check_fixture("removed-unknown", "UNKNOWN", 21)

    def test_raw_filename_cannot_substitute_for_health(self):
        support = self.consumer / "missing-health"
        (support / "Logs").mkdir(parents=True)
        pointer = ROOT / "engine/bin/TestResults/economy-health-schema-cli-input.txt"
        source = Path(pointer.read_text(encoding="utf-8-sig")) / "Logs/cameo-ai-economy-health.jsonl"
        (support / "Logs/cameo-ai-economy-raw.jsonl").write_bytes(source.read_bytes())
        result = subprocess.run(
            [sys.executable, str(self.consumer / "economy_invariants.py"), str(support),
             "--game-uid", "roundtrip", "--player", "hard"],
            capture_output=True, text=True, timeout=15)
        self.assertEqual(result.returncode, 21)
        self.assertEqual(json.loads(result.stdout)["status"], "UNKNOWN")


if __name__ == "__main__":
    unittest.main()
