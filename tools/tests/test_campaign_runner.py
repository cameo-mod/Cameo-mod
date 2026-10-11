import hashlib, json, os, pathlib, subprocess, sys, tempfile, time, unittest
ROOT=pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/"tools"/"ai"))
import campaign_runner as runner
from unittest.mock import patch


class CampaignRunnerTests(unittest.TestCase):
	def test_link_mod_tree_clones_everything_except_skipped_top_level(self):
		# The thin-overlay defect dropped ContentPacks; the clone must carry every
		# entry except the caller-skipped top dirs (real files, no reparse points).
		with tempfile.TemporaryDirectory() as temp:
			src = pathlib.Path(temp) / "src"
			(src / "ai").mkdir(parents=True)
			(src / "ContentPacks" / "Outpost2").mkdir(parents=True)
			(src / "mod.yaml").write_text("id: cameo\n", encoding="utf-8")
			(src / "ai" / "ai.yaml").write_text("Player:\n", encoding="utf-8")
			(src / "ContentPacks" / "Outpost2" / "content.yaml").write_text("Include: x\n", encoding="utf-8")
			dst = pathlib.Path(temp) / "dst" / "cameo"
			runner.link_mod_tree(src, dst, skip_top=("ai",))
			self.assertTrue((dst / "mod.yaml").is_file())
			self.assertTrue((dst / "ContentPacks" / "Outpost2" / "content.yaml").is_file())
			self.assertFalse((dst / "ai").exists())
			# identical content regardless of link-vs-copy transport
			self.assertEqual("id: cameo\n", (dst / "mod.yaml").read_text(encoding="utf-8"))
			# destination must already exist-free or be a fresh dir; cloning into
			# an existing file set never silently merges
			self.assertEqual({"mod.yaml", "ContentPacks"}, {p.name for p in dst.iterdir()})

	def test_campaign_maps_use_declared_support_mapfolder(self):
		support = pathlib.Path("run") / "Support"
		maps = runner.campaign_map_directory(support)
		self.assertEqual(support / pathlib.Path(runner.batch.USER_MAP_DIR), maps)
		self.assertEqual(("maps", "cameo", "{DEV_VERSION}"), maps.relative_to(support).parts)

	def test_engine_version_accepts_ascii_utf8_bom_and_utf16_bom(self):
		with tempfile.TemporaryDirectory() as temp:
			path = pathlib.Path(temp) / "VERSION"
			for payload in (b"git-04f9a38baed23a760973a3313762ff5d4534a733\n",
							"\ufeff04f9a38baed23a760973a3313762ff5d4534a733\n".encode("utf-8"),
							"git-04f9a38baed23a760973a3313762ff5d4534a733\n".encode("utf-16")):
				path.write_bytes(payload)
				self.assertEqual("04f9a38baed23a760973a3313762ff5d4534a733", runner.pilot.read_engine_version(path))

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

	def test_invalid_deployment_parks_unclaimed_queue_cells(self):
		with tempfile.TemporaryDirectory() as temp:
			q = runner.ResumeQueue(pathlib.Path(temp) / "queue.sqlite", "a" * 64)
			q.install([{"cell_id": "one"}, {"cell_id": "two"}])
			self.assertEqual(2, q.park_planned("INVALID_NO_DEPLOY"))
			self.assertEqual({"PARKED_INVALID_NO_DEPLOY": 2}, q.counts())

	def test_monitor_kills_cell_if_tick_100_deployment_evidence_is_missing(self):
		class Process:
			pid = 7
			returncode = None
			def poll(self): return self.returncode
			def kill(self): self.returncode = -9
			def wait(self): return self.returncode
		with tempfile.TemporaryDirectory() as temp:
			root=pathlib.Path(temp); debug=root/"debug.log"; debug.write_text("[WT 100] state\n",encoding="utf-8")
			proc=Process()
			with patch.object(runner, "private_bytes", return_value=1):
				result=runner.monitor_process(proc, driver_log=debug, actor_log=root/"server.log", stall_log=debug,
					required_seats=["Multi0"], deployment_log=root/"lua.log", poll_seconds=0.01,
					clock=lambda: 10.0, sleep=lambda _: None)
			self.assertEqual("INVALID_NO_DEPLOY", result["end_reason"])
			self.assertFalse(result["deployment"]["verified"])
			self.assertEqual(-9, proc.returncode)

	def test_monitor_uses_highest_observed_tick_when_stall_clock_missing(self):
		# Sol finding 1: actor samples prove tick 5000 but debug.log has no [WT N]
		# lines. The monitor must still hit the tick-100 deployment deadline —
		# the missing stall clock must never overwrite a known-advanced game.
		class Process:
			pid = 7
			returncode = None
			def poll(self): return self.returncode
			def kill(self): self.returncode = -9
			def wait(self): return self.returncode
		with tempfile.TemporaryDirectory() as temp:
			root=pathlib.Path(temp)
			debug=root/"debug.log"; debug.write_text("startup only\n",encoding="utf-8")
			actor=root/"actor.log"; actor.write_text("AB_CAMPAIGN_ACTOR_SAMPLE tick=5000 actor_count=42\n",encoding="utf-8")
			proc=Process()
			with patch.object(runner, "private_bytes", return_value=1):
				result=runner.monitor_process(proc, driver_log=debug, actor_log=actor, stall_log=debug,
					required_seats=["Multi0"], deployment_log=root/"lua.log", poll_seconds=0.01,
					clock=lambda: 10.0, sleep=lambda _: None)
			self.assertEqual("INVALID_NO_DEPLOY", result["end_reason"])
			self.assertEqual(-9, proc.returncode)

	def test_monitor_missing_marker_abort_is_bounded_not_generic_timeout(self):
		# Sol finding 1: a live game (stall log still being written) that never
		# emits deployment markers must die at the bounded DEPLOYMENT_WALL_TIMEOUT,
		# not linger to the generic 3000s wall timeout.
		class Process:
			pid = 7
			returncode = None
			def poll(self): return self.returncode
			def kill(self): self.returncode = -9
			def wait(self): return self.returncode
		with tempfile.TemporaryDirectory() as temp:
			root=pathlib.Path(temp)
			debug=root/"debug.log"; debug.write_text("alive\n",encoding="utf-8")
			proc=Process(); now=[0.0]
			def tick(_):
				now[0] += 1
				with debug.open("a", encoding="utf-8") as stream: stream.write("x\n")
			with patch.object(runner, "private_bytes", return_value=1):
				result=runner.monitor_process(proc, driver_log=debug, actor_log=root/"server.log", stall_log=debug,
					required_seats=["Multi0"], deployment_log=root/"lua.log", poll_seconds=0.01,
					clock=lambda: now[0], sleep=tick)
			self.assertEqual("INVALID_NO_DEPLOY", result["end_reason"])
			self.assertGreaterEqual(now[0], runner.DEPLOYMENT_WALL_TIMEOUT)
			self.assertLess(now[0], 3000)
			self.assertEqual(-9, proc.returncode)

	def test_monitor_kill_covers_owned_game_child_not_just_launcher_handle(self):
		# Sol finding 3: a single-PID mock can never prove tree cleanup. Launch a
		# real launcher plus a real child marked owned via its command line; the
		# monitor must terminate the child through the identity-checked path and
		# the launcher through its handle — no survivor either side.
		with tempfile.TemporaryDirectory() as temp:
			root=pathlib.Path(temp)
			support=root/"Support"; support.mkdir()
			(root/"driver.log").write_text("",encoding="utf-8")
			child=subprocess.Popen([sys.executable, "-c", "import time;time.sleep(120)", str(support)])
			launcher=subprocess.Popen([sys.executable, "-c", "import time;time.sleep(120)"])
			try:
				result=runner.monitor_process(launcher, driver_log=root/"driver.log",
					actor_log=root/"driver.log", stall_log=root/"debug.log",
					watch_pid=child.pid, expected_executable=pathlib.Path(sys.executable),
					expected_support=support, poll_seconds=0.05,
					sample_callback=lambda row: "INVALID_NO_DEPLOY")
				deadline=time.time()+15
				while time.time()<deadline and (runner.pid_alive(child.pid) or launcher.poll() is None):
					time.sleep(0.1)
				self.assertFalse(runner.pid_alive(child.pid), "owned game child survived monitor kill")
				self.assertIsNotNone(launcher.poll(), "launcher handle survived monitor kill")
				self.assertTrue(result["pid_scoped_cleanup"])
				self.assertEqual(child.pid, result["watched_pid"])
			finally:
				for p in (child, launcher):
					try:
						if p.poll() is None: p.kill()
					except Exception: pass

	def test_validate_receipt_rejects_duplicate_resolved_seats(self):
		# Sol finding 2: count+membership checks alone let a duplicated resolved
		# seat pass census while another seat stays unproven.
		seat={"seat_id":"Multi0","team":"A","arm":"control","bot_type":"hard_control","faction":"td_gdi",
			"home":"Multi0","spawn":0,"proof_source":"generated map.yaml","outcome":"unknown","metrics":{"timeline":[]}}
		other=dict(seat); other.update({"seat_id":"Multi1","home":"Multi1","team":"B","arm":"treatment","bot_type":"hard_treatment"})
		receipt={"schema_version":1,"campaign_id":"c","manifest_sha256":"m","cell_id":"s|1|0","game_uid":None,
			"status":"INCOMPLETE","seats":[],"seat_proof_source":"s","map_yaml_sha256":"m","control_side":"A",
			"treatment_side":"B","arm_bot_types":{},"cap_marker":False,"cap_tick":None,"support_complete":False,
			"setup":"s","map_sha256":"m","pair":0,"game_in_pair":0,"duration_ticks":None,"peak_memory_bytes":0,
			"baseline_commit":"b","engine_sha256":"e","switch":"BU_harvester_logistics","setup_id":"s","pair_id":"p",
			"seed":1337,"seed_proof":{},"teams":{},"end_class":"INCOMPLETE","end_reason":"INVALID_NO_DEPLOY",
			"winner_team":None,"world_tick":None,
			"cap":{"world_tick_cap":45000,"marker_observed":False,"marker_tick":None},
			"deployment":{"verified":False,"check_tick":None,"reported_seat_count":None,"expected_seat_count":None,"seats":{},"missing":[]},
			"memory":{"process_id":1,"peak_private_bytes":1,"limit_private_bytes":runner.PRIVATE_LIMIT,"samples":[]},
			"runtime":{"started_utc":None,"finished_utc":None,"exit_code":-1,"wall_seconds":1,"stall_seconds":180,"pid_scoped_cleanup":True},
			"artifacts":{},"map":{"required_seats":2,"name":"m","path":"p","sha256":"s"}}
		receipt["resolved_seats"]=[seat, other]
		runner.validate_receipt(dict(receipt, resolved_seats=[seat, other]))
		with self.assertRaisesRegex(ValueError, "unique"):
			runner.validate_receipt(dict(receipt, resolved_seats=[seat, dict(seat)]))

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
		# Ruling: process cap stays 10.5 GiB; admission = measured headless peak + 3 GiB reserve.
		self.assertEqual(int(10.5 * 1024**3), runner.PRIVATE_LIMIT)
		self.assertEqual(runner.pilot.HEADLESS_PEAK_BYTES + 3 * 1024**3, runner.FREE_RAM_FLOOR)
		self.assertEqual(14 * 1024**3, runner.pilot.PARALLEL_RAM_FLOOR_BYTES)
		self.assertEqual(3 * 1024**3, runner.FREE_RAM_HARD_FLOOR)
		self.assertEqual(4 * 1024**3, runner.FREE_RAM_SOFT_FLOOR)
		self.assertEqual(45000, runner.CAP_TICK)
		self.assertEqual(5000, runner.ACTOR_SAMPLE_INTERVAL)
		self.assertEqual(2, runner.SLOT_PARALLEL_SMALL)

	def test_execution_gate_rejects_unapproved_or_missing_a5(self):
		manifest = {"execution_approved": False, "pins": {"source_commit": "a" * 40, "engine_version": "engine"}}
		with self.assertRaisesRegex(ValueError, "not execution-approved"):
			runner.execution_gate(manifest, a5_receipt=None, a5_receipt_sha256=None, source_commit="a" * 40, engine_version="engine")
		manifest["execution_approved"] = True
		with self.assertRaisesRegex(ValueError, "A5 parity receipt required"):
			runner.execution_gate(manifest, a5_receipt=None, a5_receipt_sha256=None, source_commit="a" * 40, engine_version="engine")
		a5 = {"verdict":"PASS", "order_stream_verdict":"IDENTICAL_TAIL_FLUSH", "source_commit":"b"*40, "engine_version":"engine", "seed_pin_verified":True, "run_count":2, "cap_below_natural_end":True}
		with self.assertRaisesRegex(ValueError, "exact-baseline A5 parity receipt required"):
			runner.execution_gate(manifest, a5_receipt=a5, a5_receipt_sha256="d"*64, source_commit="a"*40, engine_version="engine")

	def test_execution_gate_accepts_exact_pinned_tail_flush_a5_receipt(self):
		"""Exercise the gate with the exact external receipt pinned by the candidate manifest."""
		manifest_path = ROOT / "docs" / "audit" / "ab_pilot_manifest_candidate.json"
		manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
		receipt_path = pathlib.Path(manifest["a5_parity_receipt_path"])
		if not receipt_path.is_file():
			self.skipTest(f"pinned A5 receipt is unavailable: {receipt_path}")
		receipt_bytes = receipt_path.read_bytes()
		receipt_sha256 = hashlib.sha256(receipt_bytes).hexdigest()
		self.assertEqual(manifest["a5_parity_receipt_sha256"], receipt_sha256)
		receipt = json.loads(receipt_bytes.decode("utf-8"))
		self.assertTrue(receipt["a5_diagnostic_passed"])
		self.assertEqual("IDENTICAL_TAIL_FLUSH", receipt["order_stream_verdict"])
		self.assertFalse(receipt["campaign_gate_passed"])
		# The checked-in candidate remains unapproved; this in-memory copy exercises
		# the A5 evidence gate without authorizing an execution manifest.
		manifest["execution_approved"] = True
		runner.execution_gate(
			manifest,
			a5_receipt=receipt,
			a5_receipt_sha256=receipt_sha256,
			source_commit=manifest["pins"]["source_commit"],
			engine_version=manifest["pins"]["engine_version"],
		)

	def test_receipt_cap_must_not_claim_winner_and_needs_marker(self):
		receipt = {"schema_version":1,"campaign_id":"c","manifest_sha256":"a"*64,"cell_id":"cell-1","game_uid":"game-1","status":"CENSORED_CAP","seats":[{"home":"M0"}],"baseline_commit":"b"*40,"engine_sha256":"c"*64,"switch":"s","setup_id":"x","pair_id":"p","pair_member":0,"seed":1,"seed_proof":{},"teams":{},"map":{"required_seats":1},"resolved_seats":[{"seat_id":"M0","team":"A","arm":"control","bot_type":"hard","faction":"td_gdi","home":"M0","spawn":0,"proof_source":"map+records","outcome":"lost","metrics":{"timeline":[]}}],"end_class":"CAP","end_reason":"cap","winner_team":None,"world_tick":45000,"cap":{"world_tick_cap":45000,"marker_observed":True,"marker_tick":45000},"memory":{"peak_private_bytes":0,"limit_private_bytes":runner.PRIVATE_LIMIT,"samples":[]},"runtime":{},"artifacts":{}}
		receipt.update({"seat_proof_source":"generated_map.yaml","map_yaml_sha256":"d"*64,"control_side":"A","treatment_side":"B","arm_bot_types":{"control":"hard_control","treatment":"hard_treatment"},"cap_marker":True,"cap_tick":45000,"support_complete":True,"setup":"x","map_sha256":"e"*64,"pair":0,"game_in_pair":0,"duration_ticks":45000,"peak_memory_bytes":0})
		receipt["deployment"]={"verified":True,"check_tick":100,"reported_seat_count":1,"expected_seat_count":1,"seats":{"M0":{"tick":50,"type":"td_gdi_constructionyard"}},"missing":[]}
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
		# the Lua cap marker lands on the first tick AT/AFTER the cap (observed
		# 45001 for cap 45000; A5 receipts showed 16001 for cap 16000) — accept it
		receipt["cap"]["marker_observed"]=True;receipt["cap"]["marker_tick"]=45001
		runner.validate_receipt(receipt)
		receipt["cap"]["marker_tick"]=45002
		with self.assertRaisesRegex(ValueError,"lacks valid cap evidence"):
			runner.validate_receipt(receipt)

	def test_support_bundle_excludes_fullmods_and_stale_bundles(self):
		# FullMods is ~33k hardlinked files (~1 GiB zipped/cell) reproducible from
		# manifest pins — bundling it 64x is waste, not evidence. Stale
		# support_bundle*.zip must not self-bundle on re-processing.
		with tempfile.TemporaryDirectory() as temp:
			root = pathlib.Path(temp)
			keep = root / "Support" / "Logs" / "server.log"
			fullmods = root / "FullMods" / "mods" / "cameo" / "mod.yaml"
			stale = root / "support_bundle.zip"
			for p in (keep, fullmods, stale):
				p.parent.mkdir(parents=True, exist_ok=True); p.write_text("x", encoding="utf-8")
			self.assertTrue(runner._is_support_artifact(root, keep))
			self.assertFalse(runner._is_support_artifact(root, fullmods))
			self.assertFalse(runner._is_support_artifact(root, stale))
			self.assertFalse(runner._is_support_artifact(root, root / "support_bundle_v2.zip"))

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

	def test_match_game_uid_requires_every_row_to_carry_one_uid(self):
		row = {"game_uid":"g-1","player":{"home":"M0"}}
		self.assertEqual("g-1", runner.match_game_uid([dict(row), dict(row)]))
		# Mixed present/missing: the unmarked row may belong to another launch.
		self.assertIsNone(runner.match_game_uid([dict(row), {"player":{"home":"M1"}}]))
		self.assertIsNone(runner.match_game_uid([dict(row), {"game_uid":None}]))
		self.assertIsNone(runner.match_game_uid([dict(row), {"game_uid":""}]))
		self.assertIsNone(runner.match_game_uid([dict(row), {"game_uid":123}]))
		self.assertIsNone(runner.match_game_uid([dict(row), dict(row, game_uid="g-2")]))
		self.assertIsNone(runner.match_game_uid([]))
		self.assertIsNone(runner.match_game_uid([{"player":{"home":"M0"}}]))

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
				self.assertIn("CommandLine", args[-1])
				self.assertIn("OrdinalIgnoreCase", args[-1])
				run.side_effect = runner.subprocess.TimeoutExpired("powershell.exe", 15)
				self.assertFalse(runner.kill_owned_pid(4242, exe, support))


if __name__ == "__main__":
	unittest.main()
