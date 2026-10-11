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

	def test_tick_timeout_finds_world_tick_after_100(self):
		with tempfile.TemporaryDirectory() as temp:
			path = pathlib.Path(temp) / "debug.log"
			path.write_text("[WT 25] sample\n[WT 101] sample\n", encoding="utf-8")
			self.assertEqual(101, guard.latest_world_tick(path))


class FlushLagTests(unittest.TestCase):
	def test_independent_debug_flush_does_not_reject_lagging_lua(self):
		with tempfile.TemporaryDirectory() as temp:
			log = pathlib.Path(temp) / "lua.log"
			reader = guard.MarkerReader()
			deadline = guard.DeploymentDeadline(0, 420)
			evidence = guard.parse("")
			# debug.log has reached tick 500; Lua's five-second flush is pending.
			self.assertFalse(deadline.should_abort(evidence, ["Multi0"], 500, 50))
			log.write_text("AB_CAMPAIGN_DEPLOY seat=Multi0 tick=41 type=td_gdi_constructionyard\nAB_CAMPAIGN_DEPLOYMENT_CHECK tick=100 seat_count=1 expected_count=1\n", encoding="utf-8")
			guard.merge(evidence, guard.parse(reader.read(log)))
			self.assertFalse(deadline.should_abort(evidence, ["Multi0"], 900, 55))
			self.assertTrue(evidence["verified"])

	def test_partial_terminal_line_waits_for_complete_flush(self):
		with tempfile.TemporaryDirectory() as temp:
			log = pathlib.Path(temp) / "lua.log"; reader = guard.MarkerReader()
			log.write_text("AB_CAMPAIGN_DEPLOYMENT_CHECK tick=100 seat_count=1 expected_count=", encoding="utf-8")
			self.assertEqual("", reader.read(log))
			with log.open("a", encoding="utf-8") as stream: stream.write("1\n")
			self.assertEqual(1, guard.parse(reader.read(log))["expected_seat_count"])

	def test_missing_evidence_deadline_cannot_be_renewed(self):
		d = guard.DeploymentDeadline(0, 420); e = guard.parse("")
		self.assertFalse(d.should_abort(e, ["Multi0"], 100, 50))
		self.assertFalse(d.should_abort(e, ["Multi0"], 900, 59))
		self.assertTrue(d.should_abort(e, ["Multi0"], None, 60))

	def test_true_missing_late_or_wrong_seat_still_rejected(self):
		for text in [
			"AB_CAMPAIGN_NO_DEPLOY seat=Multi0 tick=100\n",
			"AB_CAMPAIGN_DEPLOY seat=Multi0 tick=101 type=td_gdi_constructionyard\nAB_CAMPAIGN_DEPLOYMENT_CHECK tick=100 seat_count=1 expected_count=1\n",
			"AB_CAMPAIGN_DEPLOY seat=Other tick=41 type=td_gdi_constructionyard\nAB_CAMPAIGN_DEPLOYMENT_CHECK tick=100 seat_count=1 expected_count=1\n"]:
			with self.subTest(text=text):
				self.assertTrue(guard.DeploymentDeadline(0, 420).should_abort(guard.parse(text), ["Multi0"], 100, 1))

	def test_no_tick_wall_bound_is_not_extended(self):
		d = guard.DeploymentDeadline(0, 420); e = guard.parse("")
		self.assertFalse(d.should_abort(e, ["Multi0"], None, 419))
		self.assertTrue(d.should_abort(e, ["Multi0"], None, 420))


if __name__ == "__main__":
	unittest.main()
