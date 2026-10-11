import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))
import campaign_deployment_guard as guard


class DeploymentGuardTests(unittest.TestCase):
	def test_accepts_real_per_seat_markers_at_or_before_tick_100(self):
		evidence = guard.parse("""AB_CAMPAIGN_DEPLOY seat=Multi0 tick=72 type=td_gdi_constructionyard
AB_CAMPAIGN_DEPLOY seat=Multi1 tick=93 type=td_nod_constructionyard
AB_CAMPAIGN_DEPLOYMENT_CHECK tick=100 seat_count=2 expected_count=2
""")
		self.assertTrue(guard.valid(evidence, ["Multi0", "Multi1"]))

	def test_missing_or_unknown_actor_is_invalid(self):
		missing = guard.parse("AB_CAMPAIGN_NO_DEPLOY seat=Multi1 tick=100\nAB_CAMPAIGN_DEPLOYMENT_CHECK tick=100 seat_count=1 expected_count=2")
		self.assertFalse(guard.valid(missing, ["Multi0", "Multi1"]))
		unknown = guard.parse("AB_CAMPAIGN_DEPLOY seat=Multi0 tick=10 type=mcv\nAB_CAMPAIGN_DEPLOYMENT_CHECK tick=100 seat_count=2 expected_count=2")
		self.assertFalse(guard.valid(unknown, ["Multi0", "Multi1"]))

	def test_late_marker_and_wrong_seat_set_fail(self):
		late = guard.parse("AB_CAMPAIGN_DEPLOY seat=Multi0 tick=101 type=td_gdi_constructionyard\nAB_CAMPAIGN_DEPLOYMENT_CHECK tick=100 seat_count=1 expected_count=1")
		self.assertFalse(guard.valid(late, ["Multi0"]))
		extra = guard.parse("AB_CAMPAIGN_DEPLOY seat=Multi0 tick=10 type=td_gdi_constructionyard\nAB_CAMPAIGN_DEPLOY seat=Other tick=11 type=td_nod_constructionyard\nAB_CAMPAIGN_DEPLOYMENT_CHECK tick=100 seat_count=2 expected_count=2")
		self.assertFalse(guard.valid(extra, ["Multi0"]))

	def test_duplicate_markers_fail_even_when_set_and_count_match(self):
		evidence = guard.parse("""AB_CAMPAIGN_DEPLOY seat=Multi0 tick=72 type=td_gdi_constructionyard
AB_CAMPAIGN_DEPLOY seat=Multi0 tick=73 type=td_gdi_constructionyard
AB_CAMPAIGN_DEPLOYMENT_CHECK tick=100 seat_count=1 expected_count=1
""")
		self.assertEqual(["Multi0"], evidence["duplicates"])
		self.assertFalse(guard.valid(evidence, ["Multi0"]))

	def test_duplicate_markers_across_log_chunks_fail(self):
		first = guard.parse("AB_CAMPAIGN_DEPLOY seat=Multi0 tick=72 type=td_gdi_constructionyard\n")
		second = guard.parse("AB_CAMPAIGN_DEPLOY seat=Multi0 tick=73 type=td_gdi_constructionyard\n"
			"AB_CAMPAIGN_DEPLOYMENT_CHECK tick=100 seat_count=1 expected_count=1\n")
		merged = guard.merge(first, second)
		self.assertEqual(["Multi0"], merged["duplicates"])
		self.assertFalse(guard.valid(merged, ["Multi0"]))

	def test_tick_timeout_finds_world_tick_after_100(self):
		with tempfile.TemporaryDirectory() as temp:
			path = pathlib.Path(temp) / "debug.log"
			path.write_text("[WT 25] sample\n[WT 101] sample\n", encoding="utf-8")
			self.assertEqual(101, guard.latest_world_tick(path))


if __name__ == "__main__":
	unittest.main()
