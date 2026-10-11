import json, pathlib, shutil, sys, tempfile, unittest
ROOT=pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/"tools"/"ai"))
import campaign_runner as runner
import campaign_deployment_guard as deployment_guard
from unittest.mock import patch


class CampaignRunnerTests(unittest.TestCase):
	def test_campaign_maps_use_declared_support_mapfolder(self):
		support = pathlib.Path("run") / "Support"
		maps = runner.campaign_map_directory(support)
		self.assertEqual(support / pathlib.Path(runner.batch.USER_MAP_DIR), maps)
		self.assertEqual(("maps", "cameo", "{DEV_VERSION}"), maps.relative_to(support).parts)

	def test_engine_version_accepts_ascii_utf8_bom_and_utf16_bom(self):
		with tempfile.TemporaryDirectory() as temp:
			path = pathlib.Path(temp) / "VERSION"
			for payload in (b"git-4f69df005fe0d893526890fcb5bbc42e442d6fd6\n",
							"\ufeff4f69df005fe0d893526890fcb5bbc42e442d6fd6\n".encode("utf-8"),
							"git-4f69df005fe0d893526890fcb5bbc42e442d6fd6\n".encode("utf-16")):
				path.write_bytes(payload)
				self.assertEqual("4f69df005fe0d893526890fcb5bbc42e442d6fd6", runner.pilot.read_engine_version(path))

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

	def test_published_schema_covers_analyzer_join_fields(self):
		schema = json.loads(runner.RECEIPT_SCHEMA.read_text(encoding="utf-8"))
		required = set(schema["required"])
		for field in ("cell_id", "game_uid", "status", "seats", "seat_proof_source", "map_yaml_sha256",
				"control_side", "treatment_side", "arm_bot_types", "cap_marker", "cap_tick",
				"support_complete", "setup", "map_sha256", "pair", "game_in_pair",
				"duration_ticks", "peak_memory_bytes", "deployment"):
			self.assertIn(field, required, f"persisted receipt must carry analyzer join field: {field}")

	def test_bounds_and_parallel_policy(self):
		self.assertEqual(int(10.5 * 1024**3), runner.PRIVATE_LIMIT)
		self.assertEqual(runner.pilot.HEADLESS_PEAK_BYTES + 3 * 1024**3, runner.FREE_RAM_FLOOR)
		self.assertEqual(14 * 1024**3, runner.SECOND_SMALL_GAME_RAM_FLOOR)
		self.assertEqual(3 * 1024**3, runner.FREE_RAM_HARD_FLOOR)
		self.assertEqual(4 * 1024**3, runner.FREE_RAM_SOFT_FLOOR)
		self.assertEqual(45000, runner.CAP_TICK)
		self.assertEqual(5000, runner.ACTOR_SAMPLE_INTERVAL)
		self.assertEqual(2, runner.SLOT_PARALLEL_SMALL)

	def test_campaign_overlay_is_a_complete_mod_tree(self):
		with tempfile.TemporaryDirectory() as temp:
			root = pathlib.Path(temp) / "repo"
			base_mod = root / "mods" / "cameo"
			shutil.copytree(ROOT / "mods" / "cameo" / "ai", base_mod / "ai")
			(base_mod / "mod.yaml").parent.mkdir(parents=True, exist_ok=True)
			(base_mod / "mod.yaml").write_text("Metadata:\\n\tTitle: Overlay Test\\n", encoding="utf-8")
			content = base_mod / "ContentPacks" / "Outpost2" / "content.yaml"
			content.parent.mkdir(parents=True)
			content.write_text("Rules:\\n", encoding="utf-8")
			overlay = pathlib.Path(temp) / "campaign-overlay"
			runner.materialize_campaign_overlay(root, overlay, runner.pilot.SWITCH)
			self.assertEqual(content.read_bytes(), (overlay / "mods" / "cameo" / "ContentPacks" / "Outpost2" / "content.yaml").read_bytes())
			self.assertIn("hard_control", (overlay / "mods" / "cameo" / "ai" / "ai.yaml").read_text(encoding="utf-8"))

	def test_generated_campaign_monitor_suppresses_loss_notifications_and_proves_deployments(self):
		with tempfile.TemporaryDirectory() as temp:
			dest=pathlib.Path(temp);(dest/"duel_rules.yaml").write_text("World:\n",encoding="utf-8")
			runner.pilot.install_campaign_monitor(dest,2,seat_names=["Multi0","Multi1"])
			rules=(dest/"duel_rules.yaml").read_text(encoding="utf-8")
			lua=(dest/"ab_campaign_monitor.lua").read_text(encoding="utf-8")
			self.assertIn("SuppressNotifications: true",rules)
			self.assertIn("AB_CAMPAIGN_DEPLOYMENT_CHECK",lua)
			self.assertIn("AB_CAMPAIGN_NO_DEPLOY",lua)
			self.assertIn("DeploymentTick = 100",lua)

	def test_runtime_map_acceptance_gate_rejects_missing_or_incomplete_maps(self):
		with self.assertRaisesRegex(ValueError,"runtime map acceptance receipts are required"):
			runner.pilot.validate_runtime_acceptance({})
		entries=[{"map":name,"verdict":"ACCEPTED","seat_fidelity":True,"geometry":True,"receipt_sha256":"a"*64,"peak_private_bytes":1,"source_commit":"s","engine_version":"e","map_sha256":runner.pilot.MAPS[name][1]} for name in runner.pilot.MAPS]
		manifest={"map_runtime_acceptance":entries,"pins":{"source_commit":"s","engine_version":"e"}}
		self.assertTrue(runner.pilot.validate_runtime_acceptance(manifest))
		entries[-1]["seat_fidelity"]=False
		with self.assertRaisesRegex(ValueError,"not complete"):
			runner.pilot.validate_runtime_acceptance(manifest)

	def test_resolve_game_pid_selects_stable_verified_child_not_launcher(self):
		if runner.os.name != "nt": self.skipTest("Windows launcher child topology")
		class FakeProcess:
			pid=777
			def poll(self): return None
		with tempfile.TemporaryDirectory() as temp:
			exe=pathlib.Path(temp)/"OpenRA.exe";support=pathlib.Path(temp)/"Support"
			row={"ProcessId":888,"ParentProcessId":777,"ExecutablePath":str(exe.resolve()),"CommandLine":f"Engine.SupportDir={support.resolve()} Engine.LaunchPath"}
			with patch.object(runner.subprocess,"run") as run, patch.object(runner,"pid_alive",return_value=True), patch.object(runner.time,"sleep"):
				run.return_value.stdout=json.dumps(row)
				self.assertEqual(888,runner.resolve_game_pid(FakeProcess(),support,exe,timeout_seconds=2))
			self.assertEqual(2,run.call_count)

	def test_watch_child_memory_stop_cleans_verified_child_and_launcher_handle(self):
		class FakeProcess:
			pid=4242
			def __init__(self): self.rc=None;self.killed=False
			def poll(self): return self.rc
			def kill(self): self.killed=True;self.rc=-9
			def wait(self,timeout=None): return self.rc
		with tempfile.TemporaryDirectory() as temp:
			p=FakeProcess();root=pathlib.Path(temp);exe=root/"OpenRA.exe";support=root/"Support"
			with patch.object(runner,"pid_alive",side_effect=[True,True,False]), patch.object(runner,"private_bytes",return_value=runner.PRIVATE_LIMIT) as sample, patch.object(runner,"kill_owned_pid",return_value=True) as kill:
				result=runner.monitor_process(p,driver_log=root/"driver.log",actor_log=root/"actor.log",watch_pid=7070,
				expected_executable=exe,expected_support=support,sleep=lambda _:None)
		self.assertEqual((7070,),sample.call_args.args)
		kill.assert_called_once_with(7070,exe,support)
		self.assertTrue(p.killed)
		self.assertTrue(result["pid_scoped_cleanup"])

	def test_resolve_game_pid_fails_closed_without_verified_identity(self):
		class FakeProcess:
			pid=777
			def poll(self): return None
		with tempfile.TemporaryDirectory() as temp:
			exe=pathlib.Path(temp)/"OpenRA.exe"
			support=pathlib.Path(temp)/"Support"
			rows=json.dumps({"ProcessId":777,"ParentProcessId":1,"ExecutablePath":str(exe.resolve()),"CommandLine":f"Engine.SupportDir={support.resolve()}"})
			with patch.object(runner.subprocess,"run") as run, patch.object(runner.time,"monotonic",side_effect=[0,0,2]), patch.object(runner.time,"sleep"):
				run.return_value.stdout=rows
				with self.assertRaisesRegex(RuntimeError,"could not prove"):
					runner.resolve_game_pid(FakeProcess(),support,exe,timeout_seconds=1)

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
		receipt = {"schema_version":1,"campaign_id":"c","manifest_sha256":"a"*64,"cell_id":"cell-1","game_uid":"game-1","status":"CENSORED_CAP","seats":[{"home":"M0"}],"baseline_commit":"b"*40,"engine_sha256":"c"*64,"switch":"s","setup_id":"x","pair_id":"p","pair_member":0,"seed":1,"seed_proof":{},"teams":{},"map":{"required_seats":1},"resolved_seats":[{"seat_id":"M0","team":"A","arm":"control","bot_type":"hard","faction":"td_gdi","home":"M0","spawn":0,"proof_source":"map+records","outcome":"lost","metrics":{"timeline":[]}}],"end_class":"CAP","end_reason":"cap","winner_team":None,"world_tick":45000,"cap":{"world_tick_cap":45000,"marker_observed":True,"marker_tick":45000},"memory":{"peak_private_bytes":0,"limit_private_bytes":runner.PRIVATE_LIMIT,"samples":[]},"runtime":{"pid_scoped_cleanup":True},"artifacts":{},"deployment":{"verified":True,"check_tick":100,"reported_seat_count":1,"expected_seat_count":1,"seats":{"M0":{"tick":10,"type":"td_gdi_constructionyard"}},"duplicates":[],"missing":[]}}
		receipt.update({"seat_proof_source":"generated_map.yaml","map_yaml_sha256":"d"*64,"control_side":"A","treatment_side":"B","arm_bot_types":{"control":"hard_control","treatment":"hard_treatment"},"cap_marker":True,"cap_tick":45000,"support_complete":True,"setup":"x","map_sha256":"e"*64,"pair":0,"game_in_pair":0,"duration_ticks":45000,"peak_memory_bytes":0})
		runner.validate_receipt(receipt)
		receipt["game_uid"] = None
		with self.assertRaisesRegex(ValueError, "requires nonempty game_uid"):
			runner.validate_receipt(receipt)
		receipt["game_uid"] = "game-1"
		receipt["winner_team"]="A"
		with self.assertRaisesRegex(ValueError,"cannot name a winner"):
			runner.validate_receipt(receipt)
		receipt["winner_team"]=None;receipt["cap"]["marker_observed"]=False
		with self.assertRaisesRegex(ValueError,"lacks valid cap evidence"):
			runner.validate_receipt(receipt)

	def test_receipt_requires_unique_resolved_seat_ids_matching_map_proof(self):
		receipt = {"schema_version":1,"campaign_id":"c","manifest_sha256":"a"*64,"cell_id":"c","game_uid":"g","status":"NATURAL_END","seats":[{"home":"M0"},{"home":"M1"}],"seat_proof_source":"generated_map.yaml","map_yaml_sha256":"b"*64,"control_side":"A","treatment_side":"B","arm_bot_types":{},"cap_marker":False,"cap_tick":None,"support_complete":True,"setup":"x","map_sha256":"c"*64,"pair":0,"game_in_pair":0,"duration_ticks":5,"peak_memory_bytes":0,"baseline_commit":"d"*40,"engine_sha256":"e"*64,"switch":"s","setup_id":"x","pair_id":"p","seed":1,"seed_proof":{},"teams":{},"resolved_seats":[{"seat_id":"M0","team":"A","arm":"control","bot_type":"h","faction":"td_gdi","home":"M0","spawn":0,"proof_source":"map","outcome":"won","metrics":{"timeline":[]}},{"seat_id":"M1","team":"B","arm":"treatment","bot_type":"h","faction":"td_nod","home":"M1","spawn":1,"proof_source":"map","outcome":"lost","metrics":{"timeline":[]}}],"end_class":"NATURAL","end_reason":"natural","winner_team":"A","world_tick":5,"cap":{"world_tick_cap":45000,"marker_observed":False,"marker_tick":None},"memory":{"peak_private_bytes":0,"limit_private_bytes":runner.PRIVATE_LIMIT,"samples":[]},"runtime":{"pid_scoped_cleanup":True},"artifacts":{},"deployment":{"verified":True,"check_tick":100,"reported_seat_count":2,"expected_seat_count":2,"seats":{"M0":{"tick":10,"type":"td_gdi_constructionyard"},"M1":{"tick":12,"type":"td_nod_constructionyard"}},"duplicates":[],"missing":[]}}
		receipt.update({"map":{"required_seats":2},"pair_member":0})
		runner.validate_receipt(receipt)
		receipt["resolved_seats"][1]["seat_id"]="M0"
		with self.assertRaisesRegex(ValueError,"duplicate seat IDs"):
			runner.validate_receipt(receipt)

	def test_deployment_guard_rejects_duplicate_marker_rows(self):
		evidence=deployment_guard.parse("AB_CAMPAIGN_DEPLOY seat=M0 tick=5 type=td_gdi_constructionyard\nAB_CAMPAIGN_DEPLOY seat=M0 tick=6 type=td_gdi_constructionyard\nAB_CAMPAIGN_DEPLOYMENT_CHECK tick=100 seat_count=1 expected_count=1")
		self.assertFalse(deployment_guard.valid(evidence,["M0"]))

	def test_monitor_aborts_at_tick_100_when_deploy_proof_is_missing(self):
		class FakeProcess:
			pid=9494
			def __init__(self): self.rc=None;self.killed=False
			def poll(self): return self.rc
			def kill(self): self.killed=True;self.rc=-9
			def wait(self,timeout=None): return self.rc
		with tempfile.TemporaryDirectory() as temp:
			root=pathlib.Path(temp);debug=root/"debug.log";debug.write_text("",encoding="utf-8")
			actor=root/"actor.log";actor.write_text("AB_CAMPAIGN_ACTOR_SAMPLE tick=5000 actor_count=12\n",encoding="utf-8")
			p=FakeProcess()
			with patch.object(runner,"private_bytes",return_value=1):
				result=runner.monitor_process(p,driver_log=root/"driver.log",actor_log=actor,stall_log=debug,
					required_seats=["M0","M1"],deployment_log=debug,sleep=lambda _:None)
		self.assertEqual("INVALID_NO_DEPLOY",result["end_reason"])
		self.assertTrue(p.killed)

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

	def test_windows_orphan_kill_helper_invokes_identity_checked_powershell(self):
		with tempfile.TemporaryDirectory() as temp:
			exe = pathlib.Path(temp) / "OpenRA.exe"
			support = pathlib.Path(temp) / "Support"
			with patch.object(runner.os, "name", "nt"), patch.object(runner.subprocess, "run") as run:
				run.return_value.returncode = 0
				self.assertTrue(runner.kill_owned_pid(4242, exe, support))
				args = run.call_args.args[0]
				self.assertEqual("powershell.exe", args[0])
				self.assertIn("ExecutablePath -ieq", args[-1])
				self.assertIn("CommandLine.Contains", args[-1])
				run.side_effect = runner.subprocess.TimeoutExpired("powershell.exe", 15)
				self.assertFalse(runner.kill_owned_pid(4242, exe, support))


if __name__ == "__main__":
	unittest.main()
