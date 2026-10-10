import json, pathlib, sys, tempfile, unittest
ROOT=pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/"tools"/"ai"))
import campaign_runner as runner
from unittest.mock import patch


class CampaignRunnerTests(unittest.TestCase):
	def test_claim_once_and_orphan_is_incomplete(self):
		with tempfile.TemporaryDirectory() as temp:
			q = runner.ResumeQueue(pathlib.Path(temp) / "queue.sqlite", "a" * 64)
			job = {"cell_id": "setup|1|0", "seed": 1}
			q.install([job])
			self.assertEqual(job, q.claim(job["cell_id"], 123))
			q.set_game_pid(job["cell_id"], 456)
			self.assertIsNone(q.claim(job["cell_id"], 456))
			killed=[]
			self.assertEqual(1, q.recover_orphans(lambda pid: pid==456, lambda cell,pid: killed.append((cell,pid))))
			self.assertEqual([(job["cell_id"],456)],killed)
			self.assertEqual({"INCOMPLETE_UNKNOWN": 1}, q.counts())
			self.assertIsNone(q.claim(job["cell_id"], 789))

	def test_manifest_pin_and_job_immutability(self):
		with tempfile.TemporaryDirectory() as temp:
			path = pathlib.Path(temp) / "queue.sqlite"
			q = runner.ResumeQueue(path, "a" * 64)
			with self.assertRaisesRegex(ValueError, "different manifest"):
				runner.ResumeQueue(path, "b" * 64)
			q.install([{"cell_id": "x", "seed": 1}])
			with self.assertRaisesRegex(ValueError, "planned job changed"):
				q.install([{"cell_id": "x", "seed": 2}])

	def test_actor_sample_parser_and_immutable_atomic_receipt(self):
		self.assertEqual([{"world_tick": 5000, "actor_count": 88}], runner.parse_actor_samples("AB_CAMPAIGN_ACTOR_SAMPLE tick=5000 actor_count=88"))
		with tempfile.TemporaryDirectory() as temp:
			path = pathlib.Path(temp) / "r.json"
			obj = {"end_class": "INCOMPLETE", "winner_team": None}
			runner.atomic_receipt(path, obj)
			self.assertEqual(obj, json.loads(path.read_text(encoding="utf-8")))
			runner.atomic_receipt(path, obj)
			with self.assertRaises(FileExistsError):
				runner.atomic_receipt(path, {"end_class": "CAP", "winner_team": None})

	def test_bounds_and_parallel_policy(self):
		self.assertEqual(int(6.5 * 1024**3), runner.PRIVATE_LIMIT)
		self.assertEqual(6 * 1024**3, runner.FREE_RAM_FLOOR)
		self.assertEqual(45000, runner.CAP_TICK)
		self.assertEqual(5000, runner.ACTOR_SAMPLE_INTERVAL)
		self.assertEqual(3, runner.SLOT_PARALLEL_SMALL)

	def test_execution_gate_rejects_unapproved_or_missing_a5(self):
		manifest = {"execution_approved": False, "pins": {"source_commit": "a" * 40, "engine_version": "engine"}}
		with self.assertRaisesRegex(ValueError, "not execution-approved"):
			runner.execution_gate(manifest, a5_receipt=None, a5_receipt_sha256=None, source_commit="a" * 40, engine_version="engine")
		manifest["execution_approved"] = True
		with self.assertRaisesRegex(ValueError, "A5 parity receipt required"):
			runner.execution_gate(manifest, a5_receipt=None, a5_receipt_sha256=None, source_commit="a" * 40, engine_version="engine")
		a5 = {"verdict":"PASS", "order_stream_verdict":"IDENTICAL_TAIL_FLUSH", "source_commit":"b"*40, "engine_version":"engine", "seed_pin_verified":True, "run_count":2, "cap_below_natural_end":True}
		with self.assertRaisesRegex(ValueError, "pin mismatch"):
			runner.execution_gate(manifest, a5_receipt=a5, a5_receipt_sha256="d"*64, source_commit="a"*40, engine_version="engine")

	def test_receipt_cap_must_not_claim_winner_and_needs_marker(self):
		receipt = {"schema_version":1,"campaign_id":"c","manifest_sha256":"a"*64,"baseline_commit":"b"*40,"engine_sha256":"c"*64,"switch":"s","setup_id":"x","pair_id":"p","pair_member":0,"seed":1,"seed_proof":{},"teams":{},"map":{"required_seats":1},"resolved_seats":[{"seat_id":"M0","team":"A","arm":"control","bot_type":"hard","faction":"td_gdi","home":"M0","spawn":0,"proof_source":"map+records","outcome":"lost","metrics":{"timeline":[]}}],"end_class":"CAP","end_reason":"cap","winner_team":None,"world_tick":45000,"cap":{"world_tick_cap":45000,"marker_observed":True,"marker_tick":45000},"memory":{"peak_private_bytes":0,"limit_private_bytes":runner.PRIVATE_LIMIT,"samples":[]},"runtime":{},"artifacts":{}}
		runner.validate_receipt(receipt)
		receipt["winner_team"]="A"
		with self.assertRaisesRegex(ValueError,"cannot name a winner"):
			runner.validate_receipt(receipt)
		receipt["winner_team"]=None;receipt["cap"]["marker_observed"]=False
		with self.assertRaisesRegex(ValueError,"lacks valid cap evidence"):
			runner.validate_receipt(receipt)

	def test_memory_watch_kills_only_given_process_handle(self):
		class FakeProcess:
			pid=4242
			def __init__(self): self.rc=None;self.killed=False
			def poll(self): return self.rc
			def kill(self): self.killed=True;self.rc=-9
			def wait(self): return self.rc
		with tempfile.TemporaryDirectory() as temp:
			p=FakeProcess(); d=pathlib.Path(temp)/"driver.log";a=pathlib.Path(temp)/"actor.log"
			with patch.object(runner,"private_bytes",return_value=runner.PRIVATE_LIMIT):
				result=runner.monitor_process(p,driver_log=d,actor_log=a,sleep=lambda _:None)
		self.assertTrue(p.killed);self.assertEqual("MEMORY_KILL",result["end_reason"])
		self.assertEqual(4242,p.pid);self.assertTrue(result["pid_scoped_cleanup"])


if __name__ == "__main__":
	unittest.main()
