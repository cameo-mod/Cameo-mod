#!/usr/bin/env python3
"""Dry-run planner, receipt validator and resume-safe runtime primitives.

Execution is deliberately fail-closed: it requires a separately frozen
executable manifest and exact-baseline A5 parity receipt. A test manifest can
never authorize a game.
"""
from __future__ import annotations

import argparse
import base64
import ctypes
import hashlib
import json
import os
import pathlib
import re
import sqlite3
import subprocess
import sys
import tempfile
import time
from contextlib import contextmanager

import ab_campaign_pilot as pilot

CAP_TICK = 45000
PRIVATE_LIMIT = int(6.5 * 1024**3)
FREE_RAM_FLOOR = 6 * 1024**3
ACTOR_SAMPLE_INTERVAL = 5000
SLOT_PARALLEL_SMALL = 3
SMALL_TEAM_MAX = 2
ACTOR_SAMPLE_RE = re.compile(r"AB_CAMPAIGN_ACTOR_SAMPLE tick=(\d+) actor_count=(\d+)")
RECEIPT_SCHEMA = pathlib.Path(__file__).with_name("ab_campaign_receipt.schema.json")


def sha(path: pathlib.Path) -> str:
	h = hashlib.sha256()
	with path.open("rb") as f:
		for block in iter(lambda: f.read(1024 * 1024), b""):
			h.update(block)
	return h.hexdigest()


def parse_actor_samples(text: str) -> list[dict]:
	return [{"world_tick": int(t), "actor_count": int(n)} for t, n in ACTOR_SAMPLE_RE.findall(text)]


def pid_alive(pid: int | None) -> bool:
	if type(pid) is not int or pid <= 0: return False
	try:
		os.kill(pid, 0); return True
	except PermissionError:
		return True
	except ProcessLookupError:
		return False
	except OSError:
		return False


def kill_owned_pid(pid: int, expected_executable: pathlib.Path, expected_support: pathlib.Path) -> bool:
	"""Terminate only an orphan whose process image is the pinned OpenRA executable."""
	if os.name == "nt":
		payload = base64.b64encode(json.dumps({"pid":pid,"exe":str(expected_executable.resolve()),"support":str(expected_support.resolve())}).encode("utf-16le")).decode("ascii")
		script = "$v=[Text.Encoding]::Unicode.GetString([Convert]::FromBase64String('" + payload + "')) | ConvertFrom-Json; $p=Get-CimInstance Win32_Process -Filter ('ProcessId = ' + [int]$v.pid); if ($p -and $p.ExecutablePath -ieq $v.exe -and $p.CommandLine.Contains($v.support)) { Stop-Process -Id ([int]$p.ProcessId) -Force; exit 0 }; exit 1"
		try:
			result = subprocess.run(["powershell.exe", "-NoProfile", "-NonInteractive", "-Command", script], capture_output=True, timeout=15)
			return result.returncode == 0
		except (OSError, subprocess.SubprocessError):
			return False
	try:
		import psutil
		p = psutil.Process(pid)
		if pathlib.Path(p.exe()).resolve() != expected_executable.resolve(): return False
		cmd = " ".join(p.cmdline()).casefold()
		if str(expected_support.resolve()).casefold() not in cmd: return False
		p.kill(); p.wait(timeout=10); return True
	except Exception:
		return False


def available_ram_bytes() -> int:
	if os.name != "nt":
		try:
			return int(__import__("psutil").virtual_memory().available)
		except Exception as e:
			raise RuntimeError("cannot verify available RAM") from e
	class MEMORYSTATUSEX(ctypes.Structure):
		_fields_ = [("dwLength", ctypes.c_ulong), ("dwMemoryLoad", ctypes.c_ulong),
			("ullTotalPhys", ctypes.c_ulonglong), ("ullAvailPhys", ctypes.c_ulonglong),
			("ullTotalPageFile", ctypes.c_ulonglong), ("ullAvailPageFile", ctypes.c_ulonglong),
			("ullTotalVirtual", ctypes.c_ulonglong), ("ullAvailVirtual", ctypes.c_ulonglong),
			("ullAvailExtendedVirtual", ctypes.c_ulonglong)]
	m = MEMORYSTATUSEX(); m.dwLength = ctypes.sizeof(m)
	if not ctypes.windll.kernel32.GlobalMemoryStatusEx(ctypes.byref(m)):
		raise RuntimeError("GlobalMemoryStatusEx failed")
	return int(m.ullAvailPhys)


def private_bytes(pid: int) -> int:
	if type(pid) is not int or pid <= 0:
		raise ValueError("invalid PID")
	if os.name != "nt":
		try:
			return int(__import__("psutil").Process(pid).memory_info().private)
		except Exception as e:
			raise RuntimeError(f"cannot sample private bytes for PID {pid}") from e
	class PROCESS_MEMORY_COUNTERS_EX(ctypes.Structure):
		_fields_ = [("cb", ctypes.c_ulong), ("PageFaultCount", ctypes.c_ulong),
			("PeakWorkingSetSize", ctypes.c_size_t), ("WorkingSetSize", ctypes.c_size_t),
			("QuotaPeakPagedPoolUsage", ctypes.c_size_t), ("QuotaPagedPoolUsage", ctypes.c_size_t),
			("QuotaPeakNonPagedPoolUsage", ctypes.c_size_t), ("QuotaNonPagedPoolUsage", ctypes.c_size_t),
			("PagefileUsage", ctypes.c_size_t), ("PeakPagefileUsage", ctypes.c_size_t),
			("PrivateUsage", ctypes.c_size_t)]
	PROCESS_QUERY_INFORMATION = 0x0400
	PROCESS_VM_READ = 0x0010
	kernel = ctypes.windll.kernel32
	kernel.OpenProcess.restype = ctypes.c_void_p
	kernel.OpenProcess.argtypes = [ctypes.c_ulong, ctypes.c_bool, ctypes.c_ulong]
	kernel.CloseHandle.argtypes = [ctypes.c_void_p]
	handle = kernel.OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, False, pid)
	if not handle:
		raise RuntimeError(f"OpenProcess failed for PID {pid}")
	try:
		counters = PROCESS_MEMORY_COUNTERS_EX(); counters.cb = ctypes.sizeof(counters)
		get_info = ctypes.windll.psapi.GetProcessMemoryInfo
		get_info.argtypes = [ctypes.c_void_p, ctypes.POINTER(PROCESS_MEMORY_COUNTERS_EX), ctypes.c_ulong]
		get_info.restype = ctypes.c_bool
		ok = get_info(handle, ctypes.byref(counters), counters.cb)
		if not ok:
			raise RuntimeError(f"GetProcessMemoryInfo failed for PID {pid}")
		return int(counters.PrivateUsage)
	finally:
		kernel.CloseHandle(handle)


class ResumeQueue:
	"""SQLite queue: planned cells are claim-once; expired running cells become incomplete."""
	def __init__(self, path: pathlib.Path, manifest_sha256: str):
		self.path = pathlib.Path(path)
		self.path.parent.mkdir(parents=True, exist_ok=True)
		self.manifest_sha256 = manifest_sha256
		with self.connect() as db:
			db.execute("CREATE TABLE IF NOT EXISTS metadata (key TEXT PRIMARY KEY, value TEXT NOT NULL)")
			db.execute("CREATE TABLE IF NOT EXISTS cells (cell_id TEXT PRIMARY KEY, job_json TEXT NOT NULL, status TEXT NOT NULL, runner_pid INTEGER, game_pid INTEGER, updated REAL NOT NULL, receipt_path TEXT)")
			row = db.execute("SELECT value FROM metadata WHERE key='manifest_sha256'").fetchone()
			if row is None:
				db.execute("INSERT INTO metadata VALUES('manifest_sha256',?)", (manifest_sha256,))
			elif row[0] != manifest_sha256:
				raise ValueError("queue is pinned to a different manifest SHA")

	@contextmanager
	def connect(self):
		db = sqlite3.connect(self.path, timeout=30, isolation_level=None)
		db.execute("PRAGMA journal_mode=WAL")
		db.execute("PRAGMA synchronous=FULL")
		try:
			yield db
		finally:
			db.close()

	def install(self, jobs: list[dict]):
		with self.connect() as db:
			db.execute("BEGIN IMMEDIATE")
			for job in jobs:
				cell = job["cell_id"]
				encoded = json.dumps(job, sort_keys=True, separators=(",", ":"))
				old = db.execute("SELECT job_json FROM cells WHERE cell_id=?", (cell,)).fetchone()
				if old and old[0] != encoded:
					db.rollback(); raise ValueError(f"planned job changed for {cell}")
				db.execute("INSERT OR IGNORE INTO cells(cell_id,job_json,status,updated) VALUES(?,?,?,?)", (cell, encoded, "PLANNED", time.time()))
			db.commit()

	def recover_orphans(self, alive=pid_alive, kill_game=None):
		"""Never silently rerun a cell left RUNNING when its owner disappears."""
		with self.connect() as db:
			rows = db.execute("SELECT cell_id,runner_pid,game_pid FROM cells WHERE status='RUNNING'").fetchall()
			for cell, runner_pid, game_pid in rows:
				if not alive(runner_pid):
					if game_pid and alive(game_pid) and kill_game:
						kill_game(cell, game_pid)
					db.execute("UPDATE cells SET status='INCOMPLETE_UNKNOWN',updated=? WHERE cell_id=? AND status='RUNNING'", (time.time(), cell))
			return len(rows)

	def claim(self, cell_id: str, runner_pid: int) -> dict | None:
		with self.connect() as db:
			db.execute("BEGIN IMMEDIATE")
			row = db.execute("SELECT job_json FROM cells WHERE cell_id=? AND status='PLANNED'", (cell_id,)).fetchone()
			if row is None:
				db.rollback(); return None
			db.execute("UPDATE cells SET status='RUNNING',runner_pid=?,updated=? WHERE cell_id=? AND status='PLANNED'", (runner_pid, time.time(), cell_id))
			db.commit(); return json.loads(row[0])

	def set_game_pid(self, cell_id: str, game_pid: int):
		with self.connect() as db:
			cur = db.execute("UPDATE cells SET game_pid=?,updated=? WHERE cell_id=? AND status='RUNNING'", (game_pid, time.time(), cell_id))
			if cur.rowcount != 1: raise ValueError("cell not owned by this queue runner")

	def finish(self, cell_id: str, status: str, receipt_path: pathlib.Path):
		if status not in ("NATURAL", "CAP", "INCOMPLETE"):
			raise ValueError("invalid terminal queue status")
		with self.connect() as db:
			cur = db.execute("UPDATE cells SET status=?,updated=?,receipt_path=? WHERE cell_id=? AND status='RUNNING'", (status, time.time(), str(receipt_path), cell_id))
			if cur.rowcount != 1: raise ValueError("cell not owned by this queue runner")

	def counts(self) -> dict[str, int]:
		with self.connect() as db:
			return dict(db.execute("SELECT status,count(*) FROM cells GROUP BY status").fetchall())


def atomic_receipt(path: pathlib.Path, receipt: dict):
	path = pathlib.Path(path)
	path.parent.mkdir(parents=True, exist_ok=True)
	data = (json.dumps(receipt, sort_keys=True, indent=2) + "\n").encode("utf-8")
	if path.exists():
		if path.read_bytes() == data: return
		raise FileExistsError(f"immutable receipt already exists: {path}")
	fd, temp_name = tempfile.mkstemp(prefix=path.name + ".", suffix=".tmp", dir=path.parent)
	try:
		with os.fdopen(fd, "wb") as stream:
			stream.write(data);stream.flush();os.fsync(stream.fileno())
		os.replace(temp_name, path)
	finally:
		if os.path.exists(temp_name): os.unlink(temp_name)


def validate_receipt(receipt: dict) -> None:
	"""Validate the producer's semantic invariants; JSON Schema is published alongside."""
	if not isinstance(receipt, dict) or receipt.get("schema_version") != 1:
		raise ValueError("receipt schema_version must be 1")
	for key in ("campaign_id", "manifest_sha256", "baseline_commit", "engine_sha256", "switch", "setup_id", "pair_id", "seed", "seed_proof", "teams", "resolved_seats", "end_class", "end_reason", "winner_team", "world_tick", "cap", "memory", "runtime", "artifacts"):
		if key not in receipt:
			raise ValueError("receipt missing required field: " + key)
	if receipt["end_class"] not in ("NATURAL", "CAP", "INCOMPLETE"):
		raise ValueError("invalid end_class")
	if receipt["end_class"] != "NATURAL" and receipt["winner_team"] is not None:
		raise ValueError("censored/incomplete receipt cannot name a winner")
	if receipt["winner_team"] not in (None, "A", "B"):
		raise ValueError("winner_team must be A, B or null")
	if not isinstance(receipt["resolved_seats"], list) or not receipt["resolved_seats"]:
		raise ValueError("resolved_seats must be nonempty")
	if not isinstance(receipt["memory"].get("samples"), list):
		raise ValueError("memory.samples must be a list")
	if receipt["memory"].get("peak_private_bytes", 0) > PRIVATE_LIMIT and receipt["end_reason"] != "MEMORY_KILL":
		raise ValueError("memory limit exceeded without MEMORY_KILL classification")
	if receipt["end_class"] == "CAP" and not (receipt["cap"].get("marker_observed") is True and receipt["cap"].get("marker_tick") == CAP_TICK):
		raise ValueError("CAP classification lacks valid cap evidence")
	if receipt["end_class"] == "NATURAL" and (not isinstance(receipt["end_reason"], str) or receipt["winner_team"] is None):
		raise ValueError("NATURAL classification lacks authoritative winner/end reason")
	if receipt["memory"].get("limit_private_bytes") != PRIVATE_LIMIT:
		raise ValueError("receipt memory threshold differs from the frozen 6.5 GiB limit")
	if receipt["memory"].get("peak_private_bytes", 0) >= PRIVATE_LIMIT and not (receipt["end_class"] == "INCOMPLETE" and receipt["end_reason"] == "MEMORY_KILL"):
		raise ValueError("memory threshold reached without INCOMPLETE MEMORY_KILL")
	if len(receipt["resolved_seats"]) != receipt["map"].get("required_seats"):
		raise ValueError("resolved seat count differs from map proof")
	for seat in receipt["resolved_seats"]:
		if not isinstance(seat, dict) or any(k not in seat for k in ("seat_id", "team", "arm", "bot_type", "faction", "home", "spawn", "proof_source", "outcome", "metrics")):
			raise ValueError("resolved seat is missing required proof/metric fields")
		if seat["team"] not in ("A", "B") or seat["arm"] not in ("control", "treatment"):
			raise ValueError("resolved seat team/arm is invalid")
		if not isinstance(seat["metrics"].get("timeline"), list):
			raise ValueError("seat timeline must be a list")


def execution_gate(manifest: dict, *, a5_receipt: dict | None, a5_receipt_sha256: str | None,
				source_commit: str, engine_version: str) -> None:
	"""Refuse execution unless the manifest and exact-baseline A5 proof agree."""
	if manifest.get("execution_approved") is not True:
		raise ValueError("manifest is not execution-approved")
	if manifest.get("pins", {}).get("source_commit") != source_commit:
		raise ValueError("manifest source pin mismatch")
	if manifest.get("pins", {}).get("engine_version") != engine_version:
		raise ValueError("manifest engine pin mismatch")
	if not isinstance(a5_receipt, dict) or a5_receipt.get("verdict") != "PASS" or a5_receipt.get("order_stream_verdict") not in ("IDENTICAL", "IDENTICAL_TAIL_FLUSH"):
		raise ValueError("exact-baseline A5 parity receipt required")
	if a5_receipt.get("source_commit") != source_commit or a5_receipt.get("engine_version") != engine_version:
		raise ValueError("A5 parity receipt pin mismatch")
	if manifest.get("a5_parity_receipt_sha256") != a5_receipt_sha256:
		raise ValueError("A5 parity receipt SHA mismatch")
	if a5_receipt.get("seed_pin_verified") is not True or a5_receipt.get("run_count") != 2 or a5_receipt.get("cap_below_natural_end") is not True:
		raise ValueError("A5 receipt lacks two pinned deterministic capped runs")


def monitor_process(process, *, driver_log: pathlib.Path, actor_log: pathlib.Path,
				stall_log: pathlib.Path | None = None,
				wall_timeout: int = 3000, stall_timeout: int = 180,
				poll_seconds: float = 2.0, sample_callback=None,
				clock=time.monotonic, sleep=time.sleep) -> dict:
	"""Watch one owned process; any stop targets only the supplied Popen handle."""
	if wall_timeout != 3000 or stall_timeout != 180 or poll_seconds <= 0:
		raise ValueError("campaign watchdog bounds must remain 3000s/180s")
	start = clock(); deadline = start + wall_timeout
	stall_log = stall_log or driver_log
	last_log_mtime = stall_log.stat().st_mtime if stall_log.exists() else None
	last_activity = start
	private_samples = []
	peak = 0
	stop_reason = None
	actor_offset = 0
	last_actor = {"world_tick": None, "actor_count": None}
	while process.poll() is None:
		sleep(poll_seconds)
		try:
			mtime = stall_log.stat().st_mtime
			if last_log_mtime is None or mtime > last_log_mtime:
				last_log_mtime = mtime
				last_activity = clock()
		except OSError:
			pass
		try:
			private = private_bytes(process.pid)
		except (OSError, RuntimeError):
			stop_reason = "MEMORY_SAMPLE_FAILED"; break
		peak = max(peak, private)
		if actor_log.exists():
			with actor_log.open("r", encoding="utf-8", errors="replace") as actor_stream:
				actor_stream.seek(actor_offset); chunk = actor_stream.read(); actor_offset = actor_stream.tell()
				new_actors = parse_actor_samples(chunk)
				if new_actors: last_actor = new_actors[-1]
		actor_count = last_actor["actor_count"]
		world_tick = last_actor["world_tick"]
		row = {"utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()), "world_tick": world_tick,
			"private_bytes": private, "actor_count": actor_count}
		private_samples.append(row)
		if sample_callback:
			try:
				requested_stop = sample_callback(row)
			except Exception:
				stop_reason = "MEMORY_SAMPLE_FAILED"; break
			if requested_stop:
				stop_reason = str(requested_stop); break
		if private >= PRIVATE_LIMIT: stop_reason = "MEMORY_KILL"; break
		if clock() - last_activity > stall_timeout: stop_reason = "STALL"; break
		if clock() >= deadline: stop_reason = "WALL_TIMEOUT"; break
	if stop_reason is not None and process.poll() is None:
		process.kill()  # Popen.kill terminates this process handle/PID only.
		process.wait()
	return {"end_reason": stop_reason or "PROCESS_EXIT", "exit_code": process.poll(),
		"wall_seconds": max(0, clock() - start), "peak_private_bytes": peak,
		"samples": private_samples, "pid_scoped_cleanup": process.poll() is not None}


def plan_summary(manifest: dict, manifest_sha: str, root: pathlib.Path, engine_root: pathlib.Path | None):
	pilot.validate(manifest, root, engine_root)
	jobs = pilot.make_jobs(manifest)
	return {"manifest_sha256": manifest_sha, "source_commit": manifest["pins"]["source_commit"],
		"planned_games": len(jobs), "pilot_games": len(jobs), "setup_count": len(pilot.SETUPS),
		"small_game_workers_max": SLOT_PARALLEL_SMALL, "large_games_exclusive": True,
		"process_private_limit_bytes": PRIVATE_LIMIT, "free_ram_floor_bytes": FREE_RAM_FLOOR,
		"cap_tick": CAP_TICK, "actor_sample_interval_ticks": ACTOR_SAMPLE_INTERVAL,
		"queue_mode": "RESUME_SAFE_CLAIM_ONCE", "launches": 0,
		"engine_load_test": "PENDING_A5_PARITY_AND_MAP_ENGINE_ACCEPTANCE",
		"jobs": jobs}


def _file_artifact(path: pathlib.Path | None, missing_reason: str | None = None) -> dict:
	if path is None or not path.is_file(): return {"path": None, "sha256": None, "missing_reason": missing_reason or "not produced"}
	return {"path": str(path), "sha256": sha(path), "missing_reason": None}


def run_one(manifest: dict, manifest_sha: str, job: dict, *, root: pathlib.Path,
			engine_root: pathlib.Path, a5_receipt: dict, a5_receipt_sha256: str, support_root: pathlib.Path,
			slot_db: pathlib.Path) -> dict:
	"""Run exactly one claim-once game and atomically write its immutable receipt."""
	root = root.resolve(); engine_root = engine_root.resolve(); support_root = support_root.resolve()
	executable = engine_root / "bin" / "OpenRA.exe"
	pilot.validate(manifest, root, engine_root, execution=True)
	execution_gate(manifest, a5_receipt=a5_receipt, a5_receipt_sha256=a5_receipt_sha256,
		source_commit=manifest["pins"]["source_commit"], engine_version=manifest["pins"]["engine_version"])
	if available_ram_bytes() < FREE_RAM_FLOOR: raise ValueError("available RAM below 6 GiB slot floor")
	queue = ResumeQueue(support_root / "campaign_queue.sqlite", manifest_sha)
	queue.install(pilot.make_jobs(manifest))
	queue.recover_orphans(kill_game=lambda cell, pid: kill_owned_pid(pid, executable,
		support_root / re.sub(r"[^A-Za-z0-9_.-]", "_", cell) / "Support"))
	slot_counter = pilot.SqliteSlotCounter(slot_db)
	if not slot_counter.acquire(job["cell_id"], job["team_size"], available_ram_bytes()):
		raise ValueError("campaign slot unavailable under team-size/RAM policy")
	claimed = queue.claim(job["cell_id"], os.getpid())
	if claimed != job:
		slot_counter.release(job["cell_id"])
		raise ValueError("cell is already claimed/completed or immutable job differs")
	started = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
	game_root = support_root / re.sub(r"[^A-Za-z0-9_.-]", "_", job["cell_id"])
	game_root.mkdir(parents=True, exist_ok=False)
	game_support = game_root / "Support"; game_support.mkdir()
	logs = game_support / "Logs"; logs.mkdir()
	maps = game_support / "UserMaps"; maps.mkdir()
	overlay = game_root / "Overlay"; pilot.materialize_dual_arm_ai(root, overlay, job["switch"], manifest.get("ratchet_accepted_switches", []))
	shutil.copyfile(root / "mods" / "cameo" / "mod.yaml", overlay / "mods" / "cameo" / "mod.yaml")
	variant = maps / ("campaign_" + re.sub(r"[^A-Za-z0-9_.-]", "_", job["cell_id"]))
	proof = pilot.prove_generated_map(job, root, variant)
	map_yaml = variant / "map.yaml"; map_yaml_sha = sha(map_yaml)
	match_log = logs / "cameo-ai-matches.jsonl"; before = match_log.stat().st_size if match_log.exists() else 0
	server_log = logs / "server.log"; driver_log = game_root / "driver.log"
	if not executable.is_file(): raise ValueError("pinned OpenRA executable missing")
	args = [str(executable), "Game.Mod=cameo", "Sound.Device=none", "Engine.EngineDir=..",
		f"Engine.ModSearchPaths={overlay / 'mods'},{root / 'mods'},{engine_root / 'mods'}",
		f"Engine.SupportDir={game_support}", f"Launch.Map={variant.name}", "Launch.Benchmark=ai-duel-batch-",
		"Graphics.VSync=False", "Graphics.Mode=Windowed", "Graphics.WindowedSize=640,480"]
	env = os.environ.copy(); env["CAMEO_DEV_SEED"] = str(job["seed"])
	with driver_log.open("w", encoding="utf-8") as output:
		process = subprocess.Popen(args, cwd=engine_root, env=env, stdout=output, stderr=subprocess.STDOUT)
	queue.set_game_pid(job["cell_id"], process.pid)
	def heartbeat(row):
		if row["private_bytes"] < PRIVATE_LIMIT:
			slot_counter.heartbeat(job["cell_id"], row["private_bytes"])
		return "LOW_FREE_RAM" if available_ram_bytes() < FREE_RAM_FLOOR else None
	monitor_result = monitor_process(process, driver_log=driver_log, actor_log=server_log, stall_log=logs / "debug.log",
		sample_callback=heartbeat)
	slot_counter.release(job["cell_id"])
	finished = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
	log_text = server_log.read_text(encoding="utf-8", errors="replace") if server_log.exists() else ""
	match_rows = batch.read_appended_records(match_log, before)
	cap_match = re.search(r"AB_CAMPAIGN_CAP tick=(\d+)", log_text)
	seed_line = next((line for line in log_text.splitlines() if f"RandomSeed={job['seed']}" in line and "CAMEO DEV SEED pinned" in line), None)
	map_yaml_text = map_yaml.read_text(encoding="utf-8")
	resolved = proof["seats"]
	known_incomplete = {"MEMORY_KILL", "MEMORY_SAMPLE_FAILED", "LOW_FREE_RAM", "STALL", "WALL_TIMEOUT"}
	internal_status = monitor_result["end_reason"] if monitor_result["end_reason"] in known_incomplete else "INCOMPLETE"
	end_reason = monitor_result["end_reason"]
	if monitor_result["end_reason"] == "PROCESS_EXIT" and monitor_result["exit_code"] == 0 and seed_line and len(match_rows) == job["required_seats"]:
		if cap_match and int(cap_match.group(1)) >= CAP_TICK: internal_status="CENSORED_CAP"; end_reason="cap"
		else: internal_status="NATURAL_END"; end_reason="natural"
	internal = {"cell_id":job["cell_id"], "manifest_sha256":manifest_sha, "switch":job["switch"], "setup":job["setup"], "map":job["map"], "map_sha256":job["map_sha256"], "seed":job["seed"], "pair":job["pair"], "game_in_pair":job["game_in_pair"], "control_side":job["control_side"], "treatment_side":job["treatment_side"], "arm_bot_types":job["arm_bot_types"], "status":internal_status, "end_reason":end_reason, "cap_marker":bool(cap_match), "cap_tick":int(cap_match.group(1)) if cap_match else None, "support_complete":len(match_rows)==job["required_seats"], "seat_proof_source":"generated_map.yaml", "map_yaml_sha256":map_yaml_sha, "seats":resolved, "peak_memory_bytes":monitor_result["peak_private_bytes"], "duration_ticks":int(cap_match.group(1)) if cap_match else None}
	classification = pilot.adjudicate(match_rows, internal, job, manifest_sha)
	end_class = {"CENSORED_CAP":"CAP", "TREATMENT_WIN":"NATURAL", "CONTROL_WIN":"NATURAL"}.get(classification["verdict"], "INCOMPLETE")
	if end_class == "INCOMPLETE": end_reason = classification.get("reason") or end_reason
	match_by_home = {(r.get("player") or {}).get("home"): r for r in match_rows}
	resolved_out = []
	for seat in resolved:
		r = match_by_home.get(seat["home"], {}); pl = r.get("player") or {}; timeline = pilot.normalized_timeline(r); last = timeline[-1] if timeline else {}
		metrics = {key: last.get(key) for key in ("earned", "spent", "banked", "army_value", "kills", "deaths")}
		metrics["timeline"] = [{key: row.get(key) for key in ("tick", "earned", "spent", "banked", "army_value", "kills", "deaths")} for row in timeline]
		resolved_out.append({"seat_id": seat["home"], "team": seat["side"], "arm": seat["arm"], "bot_type": seat["bot_type"], "faction": seat["faction"], "home": seat["home"], "spawn": seat["spawn"], "proof_source":"generated map.yaml + cameo-ai-matches.jsonl", "outcome": pl.get("outcome", "unknown"), "metrics": metrics})
	team_obj = {side:{"arm":"control" if side==job["control_side"] else "treatment", "bot_type":job["arm_bot_types"]["control" if side==job["control_side"] else "treatment"], "seat_ids":[s["home"] for s in resolved_out if s["team"]==side]} for side in ("A", "B")}
	logs.mkdir(exist_ok=True)
	artifact_paths = list(game_root.rglob("*"))
	bundle = game_root / "support_bundle.zip"
	with zipfile.ZipFile(bundle, "w", compression=zipfile.ZIP_DEFLATED) as z:
		for file in artifact_paths:
			if file.is_file() and file != bundle: z.write(file, file.relative_to(game_root))
	engine_digest = hashlib.sha256("".join(manifest["binary_sha256"][k] for k in sorted(manifest["binary_sha256"])).encode()).hexdigest()
	receipt = {"schema_version":1,"campaign_id":manifest.get("campaign_id", "ab-campaign-2026-10-11-pilot"), "manifest_sha256":manifest_sha,
		"baseline_commit":manifest["pins"]["source_commit"], "engine_sha256":engine_digest, "switch":job["switch"], "setup_id":job["setup"],
		"map":{"name":job["map"],"path":job["map_path"],"sha256":job["map_sha256"],"generated_map_yaml_sha256":map_yaml_sha,"required_seats":job["required_seats"]},
	"pair_id":f"{job['setup']}:{job['seed']}:{job['pair']}","pair_member":job["game_in_pair"],"seed":job["seed"],
		"seed_proof":{"env_value":str(job["seed"]),"server_log_pin_line":seed_line,"server_log_sha256":sha(server_log) if server_log.is_file() else None},
		"teams":team_obj,"resolved_seats":resolved_out,"end_class":end_class,"end_reason":end_reason,
		"winner_team":classification.get("winner_side") if end_class=="NATURAL" else None,"world_tick":monitor_result["samples"][-1]["world_tick"] if monitor_result["samples"] else None,
		"cap":{"world_tick_cap":CAP_TICK,"marker_observed":bool(cap_match),"marker_tick":int(cap_match.group(1)) if cap_match else None},
		"memory":{"process_id":process.pid,"peak_private_bytes":monitor_result["peak_private_bytes"],"limit_private_bytes":PRIVATE_LIMIT,"samples":monitor_result["samples"]},
		"runtime":{"started_utc":started,"finished_utc":finished,"exit_code":monitor_result["exit_code"],"wall_seconds":monitor_result["wall_seconds"],"stall_seconds":180,"pid_scoped_cleanup":monitor_result["pid_scoped_cleanup"]},
		"artifacts":{"server_log":_file_artifact(server_log),"driver_log":_file_artifact(driver_log),"matches_jsonl":_file_artifact(match_log),"replay":_file_artifact(next(iter(game_root.rglob("*.orarep")), None)),"map_yaml":_file_artifact(map_yaml),"support_bundle":_file_artifact(bundle)}}
	validate_receipt(receipt)
	receipt_path = support_root / "receipts" / (re.sub(r"[^A-Za-z0-9_.-]", "_", job["cell_id"]) + ".json")
	atomic_receipt(receipt_path, receipt)
	queue.finish(job["cell_id"], end_class, receipt_path)
	return receipt


def main(argv=None):
	p = argparse.ArgumentParser(description=__doc__)
	sub = p.add_subparsers(dest="cmd", required=True)
	d = sub.add_parser("dry-run");d.add_argument("--manifest", type=pathlib.Path, required=True);d.add_argument("--manifest-sha256", required=True);d.add_argument("--repo-root", type=pathlib.Path, required=True);d.add_argument("--engine-root", type=pathlib.Path);d.add_argument("--output", type=pathlib.Path, required=True)
	e = sub.add_parser("execute-one", help="launch one claim-once cell only with an execution manifest and exact-baseline A5 proof")
	e.add_argument("--execute", action="store_true", required=True);e.add_argument("--manifest", type=pathlib.Path, required=True);e.add_argument("--manifest-sha256", required=True)
	e.add_argument("--a5-receipt", type=pathlib.Path, required=True);e.add_argument("--repo-root", type=pathlib.Path, required=True)
	e.add_argument("--engine-root", type=pathlib.Path, required=True);e.add_argument("--cell-id", required=True)
	e.add_argument("--support-root", type=pathlib.Path, required=True);e.add_argument("--slot-db", type=pathlib.Path, required=True)
	args = p.parse_args(argv)
	if args.cmd == "dry-run":
		raw = args.manifest.read_bytes();actual = hashlib.sha256(raw).hexdigest()
		if actual != args.manifest_sha256: raise ValueError("manifest SHA mismatch")
		m = json.loads(raw)
		result = plan_summary(m, actual, args.repo_root, args.engine_root)
		with tempfile.TemporaryDirectory(prefix="campaign-runner-dryrun-") as temp:
			proofs = pilot.preflight_generated_maps(m, args.repo_root, pathlib.Path(temp) / "maps")
			runtime = pilot.materialize_dual_arm_ai(args.repo_root, pathlib.Path(temp) / "dual-arm", m["switch"], m.get("ratchet_accepted_switches", []))
		result["generated_variant_count"] = len(proofs)
		result["proved_seats"] = sum(len(p["seats"]) for p in proofs)
		result["runtime_arm_preflight"] = runtime
		cost = pilot.dry_run(m, args.repo_root, args.engine_root)
		result["estimated_worker_hours"] = cost["worker_hours_estimate_at_8_5m_each"]
		result["estimated_slot_wall_hours"] = cost["parallel_slot_wall_estimate_hours_at_8_5m_each"]
		result["slot_wall_range_hours"] = cost["parallel_slot_wall_estimate_range_hours_5_to_12m_each"]
		result["serial_wall_range_hours"] = cost["serial_wall_estimate_range_hours_5_to_12m_each"]
		result["missing_campaign_gates"] = cost["missing_campaign_gates"]
		result["map_engine_acceptance_and_symmetric_spawn_proof"] = cost["map_engine_acceptance_and_symmetric_spawn_proof"]
		result["launches"] = 0
		args.output.parent.mkdir(parents=True, exist_ok=True);args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
		print(f"NO_LAUNCH games={result['planned_games']} variants={result['generated_variant_count']} seats={result['proved_seats']} worker_hours={result['estimated_worker_hours']} slot_wall_hours={result['estimated_slot_wall_hours']} launches=0")
		return 0
	if args.cmd == "execute-one":
		raw = args.manifest.read_bytes(); actual = hashlib.sha256(raw).hexdigest()
		if actual != args.manifest_sha256: raise ValueError("manifest SHA mismatch")
		manifest = json.loads(raw)
		a5 = json.loads(args.a5_receipt.read_text(encoding="utf-8"))
		jobs = pilot.make_jobs(manifest); matches = [j for j in jobs if j["cell_id"] == args.cell_id]
		if len(matches) != 1: raise ValueError("cell_id is not uniquely present in frozen campaign manifest")
		receipt = run_one(manifest, actual, matches[0], root=args.repo_root, engine_root=args.engine_root,
			a5_receipt=a5, a5_receipt_sha256=hashlib.sha256(args.a5_receipt.read_bytes()).hexdigest(),
			support_root=args.support_root, slot_db=args.slot_db)
		print(f"GAME {receipt['end_class']} cell={args.cell_id} receipt={args.support_root / 'receipts'}")
		return 0 if receipt["end_class"] != "INCOMPLETE" else 2
	return 2


if __name__ == "__main__":
	try:
		raise SystemExit(main())
	except (OSError, ValueError, KeyError, TypeError) as e:
		print(f"error: {e}", file=sys.stderr);raise SystemExit(2)
