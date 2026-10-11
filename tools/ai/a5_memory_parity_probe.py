#!/usr/bin/env python3
"""Bounded diagnostic A5 parity probe; not a campaign outcome launcher.

Runs two serial same-seed baseline-vs-baseline 1v1 cells, samples only the
OpenRA process whose command line names that cell's support directory, and
stops at the measured headless peak plus 3 GiB by default. The stop is
constrained by the host hard reserve. It does not create an
execution manifest or authorize campaign jobs.
"""
from __future__ import annotations

import hashlib
import json
import os
import pathlib
import subprocess
import sys
import time
import argparse
import ctypes
import re
import shutil
import signal

import campaign_runner as runner
import ab_campaign_pilot as pilot
import campaign_deployment_guard as deployment_guard

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "results" / "campaign-a5-memory-diagnostic-20261010-sol-advice"
BASELINE = "68be89ca0650faaedde2cedd649aa0ac75aa17bf"
MEMORY_FIX = "173038d7881fe45d012a05d2a37ebd7b17f85ffe"
ENGINE = "4f69df005fe0d893526890fcb5bbc42e442d6fd6"
SEED = 1337
CAP_TICK = 16000
POLL = 0.25
WALL = 900
PROCESS_STOP = int(8.24 * 1024**3)
HOST_SOFT_STOP = int(7.0 * 1024**3)
HOST_HARD_FLOOR = 3 * 1024**3
HOST_LAUNCH_HEADROOM = 1 * 1024**3
COMMIT_HEADROOM = 2 * 1024**3
WORLD_TICK_RE = re.compile(r"\[WT\s+(\d+)\]")
COUNTERS_EXE = pathlib.Path(os.environ.get("DOTNET_COUNTERS_EXE") or
	(shutil.which("dotnet-counters") or r"C:\tmp\a5diagtools\dotnet-counters.exe"))
TRACE_EXE = pathlib.Path(os.environ.get("DOTNET_TRACE_EXE") or
	(shutil.which("dotnet-trace") or r"C:\tmp\a5diagtools\dotnet-trace.exe"))
TRACE_TRIGGER = 1 * 1024**3
TRACE_BUFFER_MB = 32
TRACE_MAX_OUTPUT = 64 * 1024**2
TRACE_DURATION = "00:00:00:10"
TRACE_ENABLED = True
ALLOW_STALE_BUILD = False


def binary_hashes() -> dict[str, str]:
	"""Record every campaign-pinned executable/DLL; missing evidence is fatal."""
	return {str(path.relative_to(ROOT)): file_sha256(path) for path in (
		ROOT / "engine/bin/OpenRA.exe", ROOT / "engine/bin/OpenRA.Game.dll",
		ROOT / "engine/bin/OpenRA.Mods.Cameo.dll")}


def admission_ok(available_physical: int, available_commit: int) -> bool:
	return (available_physical >= PROCESS_STOP + HOST_SOFT_STOP + HOST_LAUNCH_HEADROOM
		and available_commit >= PROCESS_STOP + COMMIT_HEADROOM)


def stop_reason(total_private: int, available_physical: int) -> str | None:
	if total_private >= PROCESS_STOP:
		return "PROCESS_PRIVATE_SOFT_STOP"
	if available_physical <= HOST_SOFT_STOP:
		return "HOST_PHYSICAL_SOFT_STOP"
	return None


def counters_command(tool: str, pid: int, output: pathlib.Path) -> list[str]:
	return [tool, "collect", "--process-id", str(pid), "--counters", "System.Runtime",
		"--refresh-interval", "1", "--format", "csv", "--output", str(output)]


def trace_command(tool: str, pid: int, output: pathlib.Path) -> list[str]:
	return [tool, "collect", "--process-id", str(pid), "--profile", "gc-verbose",
		"--buffersize", str(TRACE_BUFFER_MB), "--duration", TRACE_DURATION,
		"--format", "NetTrace", "--output", str(output)]


def verify_censored_cap(log_text: str, match_rows: list[dict], cap_tick: int) -> tuple[bool, int | None]:
	match = re.search(r"AB_CAMPAIGN_CAP tick=(\d+)", log_text)
	observed = int(match.group(1)) if match else None
	valid = (observed is not None and cap_tick <= observed <= cap_tick + 2
		and len(match_rows) == 2
		and all((row.get("player") or {}).get("outcome") == "lost" for row in match_rows)
		and len({row.get("duration_ticks") for row in match_rows}) == 1)
	return valid, observed


def file_sha256(path: pathlib.Path) -> str:
	digest = hashlib.sha256()
	with path.open("rb") as stream:
		for chunk in iter(lambda: stream.read(1024 * 1024), b""):
			digest.update(chunk)
	return digest.hexdigest()


def process_working_set(pid: int) -> int:
	class PROCESS_MEMORY_COUNTERS_EX(ctypes.Structure):
		_fields_ = [("cb", ctypes.c_ulong), ("PageFaultCount", ctypes.c_ulong),
			("PeakWorkingSetSize", ctypes.c_size_t), ("WorkingSetSize", ctypes.c_size_t),
			("QuotaPeakPagedPoolUsage", ctypes.c_size_t), ("QuotaPagedPoolUsage", ctypes.c_size_t),
			("QuotaPeakNonPagedPoolUsage", ctypes.c_size_t), ("QuotaNonPagedPoolUsage", ctypes.c_size_t),
			("PagefileUsage", ctypes.c_size_t), ("PeakPagefileUsage", ctypes.c_size_t),
			("PrivateUsage", ctypes.c_size_t)]
	kernel = ctypes.windll.kernel32
	kernel.OpenProcess.restype = ctypes.c_void_p
	kernel.OpenProcess.argtypes = [ctypes.c_ulong, ctypes.c_bool, ctypes.c_ulong]
	kernel.CloseHandle.argtypes = [ctypes.c_void_p]
	handle = kernel.OpenProcess(0x0400 | 0x0010, False, pid)
	if not handle:
		raise RuntimeError(f"OpenProcess failed for working set PID {pid}")
	try:
		counters = PROCESS_MEMORY_COUNTERS_EX(); counters.cb = ctypes.sizeof(counters)
		get_info = ctypes.windll.psapi.GetProcessMemoryInfo
		get_info.argtypes = [ctypes.c_void_p, ctypes.POINTER(PROCESS_MEMORY_COUNTERS_EX), ctypes.c_ulong]
		get_info.restype = ctypes.c_bool
		if not get_info(handle, ctypes.byref(counters), counters.cb):
			raise RuntimeError(f"GetProcessMemoryInfo failed for PID {pid}")
		return int(counters.WorkingSetSize)
	finally:
		kernel.CloseHandle(handle)


def host_memory_status() -> dict:
	class MEMORYSTATUSEX(ctypes.Structure):
		_fields_ = [("dwLength", ctypes.c_ulong), ("dwMemoryLoad", ctypes.c_ulong),
			("ullTotalPhys", ctypes.c_ulonglong), ("ullAvailPhys", ctypes.c_ulonglong),
			("ullTotalPageFile", ctypes.c_ulonglong), ("ullAvailPageFile", ctypes.c_ulonglong),
			("ullTotalVirtual", ctypes.c_ulonglong), ("ullAvailVirtual", ctypes.c_ulonglong),
			("ullAvailExtendedVirtual", ctypes.c_ulonglong)]
	state = MEMORYSTATUSEX(); state.dwLength = ctypes.sizeof(state)
	if not ctypes.windll.kernel32.GlobalMemoryStatusEx(ctypes.byref(state)):
		raise RuntimeError("GlobalMemoryStatusEx failed")
	return {"available_physical_bytes": int(state.ullAvailPhys),
		"commit_used_bytes": int(state.ullTotalPageFile - state.ullAvailPageFile),
		"commit_limit_bytes": int(state.ullTotalPageFile),
		"commit_available_bytes": int(state.ullAvailPageFile)}


def log_snapshot(support: pathlib.Path) -> dict:
	logs = support / "Logs"
	sizes = {p.name: p.stat().st_size for p in logs.glob("*") if p.is_file()} if logs.exists() else {}
	tick = None
	for name in ("debug.log", "server.log", "perf.log"):
		path = logs / name
		if path.is_file():
			with path.open("rb") as stream:
				stream.seek(max(0, path.stat().st_size - 65536))
				text = stream.read().decode("utf-8", errors="replace")
			found = WORLD_TICK_RE.findall(text)
			if found:
				tick = max(int(x) for x in found)
	return {"world_tick": tick, "log_file_bytes": sizes,
		"log_total_bytes": sum(sizes.values()), "actor_count": None,
		"actor_count_note": "No actor census is emitted by existing logs; not instrumented to avoid changing game behavior."}


def process_rows(support: pathlib.Path) -> list[dict]:
	# Enumerate by immutable per-cell support path. Never match by image name alone.
	script = "$s='" + str(support.resolve()).replace("'", "''") + "'; $e='" + str((ROOT / 'engine/bin/OpenRA.exe').resolve()).replace("'", "''") + "'; Get-CimInstance Win32_Process | Where-Object { $_.ExecutablePath -ieq $e -and $_.CommandLine.Contains($s) } | Select-Object ProcessId,ParentProcessId,ExecutablePath,CommandLine | ConvertTo-Json -Compress"
	p = subprocess.run(["powershell.exe", "-NoProfile", "-NonInteractive", "-Command", script],
		capture_output=True, text=True, timeout=15)
	if p.returncode:
		raise RuntimeError("process identity query failed: " + p.stderr[-400:])
	if not p.stdout.strip():
		return []
	rows = json.loads(p.stdout)
	return rows if isinstance(rows, list) else [rows]


def kill_cell_processes(rows: list[dict], support: pathlib.Path) -> None:
	for row in rows:
		pid = int(row["ProcessId"])
		if not runner.kill_owned_pid(pid, ROOT / "engine/bin/OpenRA.exe", support):
			raise RuntimeError(f"could not verify PID-scoped stop for {pid}")


def run_cell(index: int) -> dict:
	cell = f"run-{index:02d}"
	support = OUT / cell
	support.mkdir(parents=True, exist_ok=False)
	stdout_path = support / "driver_stdout.log"
	command = [sys.executable, str(ROOT / "tools/ai/run_ai_match_batch.py"),
		"--factions", "td_gdi", "--bot-a", "hard", "--bot-b", "hard",
		"--team-size", "1", "--repeats", "1", "--map",
		str(OUT / "a5_fixed_tick_map"),
		"--allow-temporary-template",
		"--time-limit", "1", "--retries", "0", "--stall-timeout", "180",
		"--keep-variants", "--render", "headless", "--support-dir", str(support)]
	if ALLOW_STALE_BUILD:
		command.append("--allow-stale-build")
	env = os.environ.copy()
	env["CAMEO_DEV_SEED"] = str(SEED)
	start = time.monotonic()
	peak = 0
	samples = []
	counters = {}
	counter_stderr = {}
	counter_errors = {}
	traces = {}
	trace_stderr = {}
	trace_files = {}
	trace_errors = {}
	stop_reason = None
	valid_memory_sample_count = 0
	deployment_reader = deployment_guard.MarkerReader()
	deployment_deadline = deployment_guard.DeploymentDeadline(start, runner.DEPLOYMENT_WALL_TIMEOUT)
	deployment = {"verified": False, "check_tick": None, "reported_seat_count": None,
		"expected_seat_count": None, "seats": {}, "missing": []}
	with stdout_path.open("w", encoding="utf-8") as stdout:
		proc = subprocess.Popen(command, cwd=ROOT, env=env, stdout=stdout, stderr=subprocess.STDOUT)
		while proc.poll() is None:
			time.sleep(POLL)
			chunk = deployment_reader.read(support / "Logs" / "lua.log")
			deployment = deployment_guard.merge(deployment, deployment_guard.parse(chunk))
			world_tick = deployment_guard.highest_world_tick(deployment, support / "Logs" / "debug.log")
			if deployment_deadline.should_abort(deployment, ["BotA", "BotB"], world_tick, time.monotonic()):
				stop_reason = "INVALID_NO_DEPLOY"
			if stop_reason == "INVALID_NO_DEPLOY":
				proc.terminate()
				try: proc.wait(timeout=15)
				except subprocess.TimeoutExpired: proc.kill(); proc.wait()
				try: kill_cell_processes(process_rows(support), support)
				except Exception as cleanup_error: stop_reason += ":CLEANUP_FAILED:" + str(cleanup_error)
				break
			try:
				rows = process_rows(support)
			except (OSError, RuntimeError, subprocess.SubprocessError, json.JSONDecodeError) as exc:
				stop_reason = "PROCESS_IDENTITY_SAMPLE_FAILED"
				proc.terminate()
				try:
					proc.wait(timeout=15)
				except subprocess.TimeoutExpired:
					proc.kill(); proc.wait()
				# Query once more after the batch wrapper stops so any exact-support
				# game process can be PID-scoped terminated by immutable identity.
				try:
					kill_cell_processes(process_rows(support), support)
				except Exception as cleanup_error:
					stop_reason += ":CLEANUP_FAILED:" + str(cleanup_error)
				break
			try:
				host = host_memory_status()
			except (OSError, RuntimeError, ValueError):
				# Host-RAM telemetry is required while a game is live. Recheck the
				# cell-scoped process set to distinguish a genuine exit race from a
				# missing sample on a running game; failed identity reads fail closed.
				try:
					still_running = bool(process_rows(support))
				except (OSError, RuntimeError, subprocess.SubprocessError, json.JSONDecodeError):
					still_running = True
				if (not still_running and deployment_guard.sampling_failure_is_exit_race(
							stop_reason, process_exited=True, valid_sample_count=valid_memory_sample_count)):
					break
				if stop_reason is None:
					stop_reason = "MEMORY_SAMPLE_FAILED"
				if rows:
					kill_cell_processes(rows, support)
				break
			if not rows:
				samples.append({"utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
					"pid": None, **host, **log_snapshot(support)})
			for row in rows:
				pid = int(row["ProcessId"])
				try:
					private = runner.private_bytes(pid)
					working_set = process_working_set(pid)
				except (OSError, RuntimeError):
					# The game may exit between the process snapshot and memory query.
					# Re-snapshot before classifying; a failed identity query is not
					# evidence that the process exited.
					try:
						still_present = any(int(x["ProcessId"]) == pid for x in process_rows(support))
					except (OSError, RuntimeError, subprocess.SubprocessError, json.JSONDecodeError):
						still_present = True
					if (not still_present and deployment_guard.sampling_failure_is_exit_race(
							stop_reason, process_exited=True, valid_sample_count=valid_memory_sample_count)):
						continue
					if stop_reason is None:
						stop_reason = "MEMORY_SAMPLE_FAILED"
					kill_cell_processes(rows, support)
					break
				peak = max(peak, private)
				valid_memory_sample_count += 1
				if pid not in counters:
					counter_path = support / f"System.Runtime-{pid}.csv"
					stderr_path = support / f"System.Runtime-{pid}.stderr.log"
					try:
						counter_stderr[pid] = stderr_path.open("w", encoding="utf-8")
						counters[pid] = subprocess.Popen(
							counters_command(str(COUNTERS_EXE), pid, counter_path), cwd=ROOT,
							stdout=subprocess.DEVNULL, stderr=counter_stderr[pid])
					except (OSError, subprocess.SubprocessError) as exc:
						stop_reason = "RUNTIME_COUNTERS_START_FAILED:" + str(exc)
						kill_cell_processes(rows, support)
						break
				if TRACE_ENABLED and private >= TRACE_TRIGGER and pid not in traces:
					trace_path = support / f"gc-verbose-{pid}.nettrace"
					stderr_path = support / f"gc-verbose-{pid}.stderr.log"
					try:
						trace_stderr[pid] = stderr_path.open("w", encoding="utf-8")
						traces[pid] = subprocess.Popen(
							trace_command(str(TRACE_EXE), pid, trace_path), cwd=ROOT,
							stdout=subprocess.DEVNULL, stderr=trace_stderr[pid],
							creationflags=subprocess.CREATE_NEW_PROCESS_GROUP)
						trace_files[pid] = trace_path
					except (OSError, subprocess.SubprocessError) as exc:
						stop_reason = "GC_VERBOSE_TRACE_START_FAILED:" + str(exc)
						kill_cell_processes(rows, support)
						break
				samples.append({"utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
					"pid": pid, "private_bytes": private,
					"private_working_set_bytes": working_set,
					**host, **log_snapshot(support)})
			for pid, monitor in counters.items():
				if monitor.poll() is not None and monitor.returncode != 0:
					stop_reason = f"RUNTIME_COUNTERS_FAILED_PID_{pid}_EXIT_{monitor.returncode}"
					kill_cell_processes(rows, support)
					break
			for pid, trace in traces.items():
				if trace_files[pid].is_file() and trace_files[pid].stat().st_size > TRACE_MAX_OUTPUT:
					stop_reason = f"GC_VERBOSE_TRACE_OUTPUT_LIMIT_PID_{pid}"
					kill_cell_processes(rows, support)
					break
			if stop_reason:
				break
			if rows and any(x["private_bytes"] >= PROCESS_STOP for x in samples[-len(rows):]):
				stop_reason = "PROCESS_PRIVATE_SOFT_STOP"
				kill_cell_processes(rows, support)
				break
			if host["available_physical_bytes"] <= HOST_SOFT_STOP:
				stop_reason = "HOST_PHYSICAL_SOFT_STOP"
				if rows:
					kill_cell_processes(rows, support)
				break
			if host["commit_available_bytes"] <= COMMIT_HEADROOM:
				stop_reason = "HOST_COMMIT_SOFT_STOP"
				if rows:
					kill_cell_processes(rows, support)
				break
			if time.monotonic() - start >= WALL:
				stop_reason = "WALL_TIMEOUT_900S"
				if rows:
					kill_cell_processes(rows, support)
				break
		if stop_reason:
			try:
				proc.wait(timeout=30)
			except subprocess.TimeoutExpired:
				proc.kill(); proc.wait()
		else:
			proc.wait()
	if valid_memory_sample_count == 0 and stop_reason is None:
		# Host-only snapshots do not prove that the game itself was ever measured.
		stop_reason = "MEMORY_SAMPLE_FAILED"
	for pid, trace in traces.items():
		try:
			trace.wait(timeout=12)
		except subprocess.TimeoutExpired:
			try:
				trace.send_signal(signal.CTRL_BREAK_EVENT)
				trace.wait(timeout=5)
			except (OSError, subprocess.SubprocessError, subprocess.TimeoutExpired):
				trace.terminate()
				try:
					trace.wait(timeout=5)
				except subprocess.TimeoutExpired:
					trace.kill(); trace.wait()
		trace_stderr[pid].close()
		trace_path = trace_files[pid]
		trace_files[pid] = {"path": str(trace_path), "bytes": trace_path.stat().st_size if trace_path.is_file() else 0,
			"sha256": file_sha256(trace_path) if trace_path.is_file() else None,
			"return_code": trace.returncode, "trigger_private_bytes": next(
				(x["private_bytes"] for x in samples if x.get("pid") == pid and x["private_bytes"] >= TRACE_TRIGGER), None)}
		if trace.returncode not in (0, None):
			trace_errors[str(pid)] = f"dotnet-trace exit {trace.returncode}"
	for pid, monitor in counters.items():
		if monitor.poll() is None:
			monitor.terminate()
		try:
			monitor.wait(timeout=5)
		except subprocess.TimeoutExpired:
			monitor.kill(); monitor.wait()
		counter_stderr[pid].close()
		if monitor.returncode not in (0, None):
			counter_errors[str(pid)] = f"dotnet-counters exit {monitor.returncode}"
	# Check no OpenRA tied to this support remains before returning or starting the next cell.
	left = process_rows(support)
	if left:
		kill_cell_processes(left, support)
		stop_reason = stop_reason or "ORPHAN_CLEANUP"
	log_root = support / "Logs"
	server_log = log_root / "server.log"
	lua_log = log_root / "lua.log"
	match_log = log_root / "cameo-ai-matches.jsonl"
	summary_path = support / "batch_summary.json"
	summary = json.loads(summary_path.read_text(encoding="utf-8")) if summary_path.is_file() else {}
	match_rows = [json.loads(line) for line in match_log.read_text(encoding="utf-8").splitlines() if line.strip()] if match_log.is_file() else []
	replays = list((support / "Replays").rglob("*.orarep")) if (support / "Replays").is_dir() else []
	log_text = server_log.read_text(encoding="utf-8", errors="replace") if server_log.is_file() else ""
	lua_text = lua_log.read_text(encoding="utf-8", errors="replace") if lua_log.is_file() else ""
	deployment = deployment_guard.merge(deployment, deployment_guard.parse(lua_text))
	deployment["verified"] = deployment_guard.valid(deployment, ["BotA", "BotB"])
	if not deployment["verified"] and stop_reason is None: stop_reason = "INVALID_NO_DEPLOY"
	seed_pin_verified = f"CAMEO DEV SEED pinned - RandomSeed={SEED} (parity harness)" in log_text
	records_verified = len(match_rows) == 2 and len({r.get("game_uid") for r in match_rows}) == 1 and all((r.get("player") or {}).get("outcome") in ("won", "lost") for r in match_rows)
	duration_ticks = match_rows[0].get("duration_ticks") if records_verified else None
	natural_below_cap = type(duration_ticks) is int and duration_ticks < 60000
	batch_result = summary.get("results", [{}])[0] if summary.get("results") else {}
	censored_cap_verified, observed_cap_tick = verify_censored_cap(lua_text, match_rows, CAP_TICK)
	valid_game = (summary.get("matches") == 1 and summary.get("completed") == 1
		and batch_result.get("status") == "ok" and batch_result.get("exit_code") == 0 and batch_result.get("attempts") == 1
		and not summary.get("new_exceptions") and seed_pin_verified and records_verified
		and censored_cap_verified and deployment["verified"] and len(replays) == 1 and stop_reason is None and peak < PROCESS_STOP)
	if not valid_game and stop_reason is None:
		stop_reason = "A5_RECEIPT_VALIDATION_FAILED"
	return {"cell": cell, "seed": SEED, "source_head": subprocess.check_output(
		["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
		"baseline_parent": BASELINE, "engine_version": pilot.read_engine_version(ROOT / "engine/VERSION"),
		"binary_sha256": binary_hashes(),
		"render_mode": command[command.index("--render") + 1],
		"render_mode_derived_from": {"command_render_argument": command[command.index("--render") + 1]},
		"exit_code": proc.returncode, "stop_reason": stop_reason or "PROCESS_EXIT",
		"wall_seconds": round(time.monotonic() - start, 2), "peak_private_bytes": peak,
		"stop_threshold_bytes": PROCESS_STOP, "host_soft_stop_bytes": HOST_SOFT_STOP,
		"host_hard_floor_bytes": HOST_HARD_FLOOR, "ceiling_bytes": runner.PRIVATE_LIMIT,
		"samples": samples, "support": str(support), "driver_stdout": str(stdout_path),
		"runtime_counter_files": [str(support / f"System.Runtime-{pid}.csv") for pid in counters],
		"runtime_counter_errors": counter_errors,
		"allocation_trace_files": list(trace_files.values()),
		"allocation_trace_errors": trace_errors,
		"allocation_trace_policy": {"trigger_private_bytes": TRACE_TRIGGER,
			"buffersize_mb": TRACE_BUFFER_MB, "max_output_bytes": TRACE_MAX_OUTPUT,
			"duration": TRACE_DURATION, "enabled": TRACE_ENABLED, "poll_seconds": POLL},
		"fingerprint_id": summary.get("fingerprint_id"), "seed_pin_verified": seed_pin_verified,
		"records_verified": records_verified, "duration_ticks": duration_ticks,
		"cap_tick": CAP_TICK, "observed_cap_tick": observed_cap_tick, "censored_cap_verified": censored_cap_verified,
		"natural_below_cap": natural_below_cap, "deployment": deployment,
		"replay_count": len(replays), "valid_game": valid_game}


def aggregate_run_config(receipt: dict, runs: list[dict]) -> dict:
	"""Derive guardrail fields from complete matching run records, never defaults."""
	if len(runs) != 2: raise ValueError("re-aggregation requires exactly two run records")
	configs = []
	for run in runs:
		for key in ("render_mode", "ceiling_bytes", "host_hard_floor_bytes", "stop_threshold_bytes", "allocation_trace_policy"):
			if key not in run: raise ValueError(f"run record missing {key}")
		policy = run["allocation_trace_policy"]
		if not isinstance(policy, dict) or type(policy.get("enabled")) is not bool:
			raise ValueError("run record missing allocation trace enabled flag")
		if run["render_mode"] != "headless": raise ValueError("A5 run is not headless")
		for key in ("ceiling_bytes", "host_hard_floor_bytes", "stop_threshold_bytes"):
			if type(run[key]) is not int or run[key] <= 0: raise ValueError(f"invalid run {key}")
		if run["stop_threshold_bytes"] > run["ceiling_bytes"]: raise ValueError("stop exceeds hard ceiling")
		configs.append((run["render_mode"], run["ceiling_bytes"], run["host_hard_floor_bytes"], run["stop_threshold_bytes"], policy["enabled"]))
	if configs[0] != configs[1]: raise ValueError("A5 run configurations disagree")
	result = dict(receipt)
	result.update(render_mode=configs[0][0], process_private_cap_bytes=configs[0][1],
		host_emergency_floor_bytes=configs[0][2], process_stop_threshold_bytes=configs[0][3],
		allocation_trace_enabled=configs[0][4], pair=runs)
	return result


def reaggregate(source: pathlib.Path, expected_sha256: str, destination: pathlib.Path) -> dict:
	"""Offline reconstruction. Source receipts/logs are immutable hashed inputs."""
	if destination.exists(): raise ValueError("re-aggregate output exists; preserve existing evidence")
	if not re.fullmatch(r"[0-9a-fA-F]{64}", expected_sha256 or ""):
		raise ValueError("source raw receipt SHA256 is required")
	actual_sha = file_sha256(source)
	if actual_sha != expected_sha256.lower(): raise ValueError("source raw receipt SHA256 mismatch")
	raw = json.loads(source.read_text(encoding="utf-8-sig"))
	if not isinstance(raw.get("pair"), list) or len(raw["pair"]) != 2:
		raise ValueError("raw receipt lacks the two run records")
	runs = []; inputs = []
	for index, embedded in enumerate(raw["pair"], 1):
		cell = f"run-{index:02d}"
		path = source.parent / f"{cell}.receipt.json"
		run = json.loads(path.read_text(encoding="utf-8-sig"))
		if run != embedded or run.get("cell") != cell:
			raise ValueError("raw aggregate and per-run receipt differ")
		proof = {"run_receipt": str(path.resolve()), "sha256": file_sha256(path)}
		# Legacy emitter lacked a renderer field. The actual engine graphics
		# record is authoritative; never infer this from today's CLI defaults.
		if "render_mode" not in run:
			graphics = source.parent / cell / "Logs" / "graphics.log"
			if not graphics.is_file() or "Null platform: headless window" not in graphics.read_text(encoding="utf-8", errors="strict"):
				raise ValueError("run record lacks authoritative headless graphics evidence")
			run = dict(run, render_mode="headless", render_mode_derived_from={"graphics_log_sha256": file_sha256(graphics)})
			proof.update(graphics_record=str(graphics.resolve()), graphics_sha256=file_sha256(graphics))
		runs.append(run); inputs.append(proof)
	result = aggregate_run_config(raw, runs)
	result["reaggregation"] = {"mode": "NO_LAUNCH_FROM_EXISTING_RECORDS", "source_raw_receipt_sha256": actual_sha,
		"source_raw_receipt": str(source.resolve()), "input_records": inputs,
		"emitter_sha256": file_sha256(pathlib.Path(__file__))}
	destination.parent.mkdir(parents=True, exist_ok=True)
	with destination.open("x", encoding="utf-8") as stream:
		stream.write(json.dumps(result, indent=2) + "\n")
	return result


def main() -> int:
	global OUT, PROCESS_STOP, HOST_SOFT_STOP, HOST_HARD_FLOOR, TRACE_ENABLED, CAP_TICK, ALLOW_STALE_BUILD
	parser = argparse.ArgumentParser(description=__doc__)
	parser.add_argument("--reaggregate-source", type=pathlib.Path, help="existing raw aggregate; strictly offline, no launch")
	parser.add_argument("--source-raw-sha256", help="required raw aggregate SHA256 for offline reconstruction")
	parser.add_argument("--reaggregate-output", type=pathlib.Path, help="new output receipt; never overwrite source")
	parser.add_argument("--execute-a5-diagnostic", action="store_true",
		help="explicitly run two bounded A5 qualification cells; never campaign outcomes")
	parser.add_argument("--output-dir", type=pathlib.Path, default=OUT,
		help="new, non-existing artifact directory; existing evidence is never overwritten")
	parser.add_argument("--process-stop-gib", type=float, default=8.24,
		help="PID-scoped stop threshold: measured headless peak plus 3 GiB")
	parser.add_argument("--host-soft-stop-gib", type=float, default=7.0,
		help="stop before the hard free-RAM floor")
	parser.add_argument("--host-hard-floor-gib", type=float, default=3.0,
		help="minimum free physical RAM; user-authorized reserve")
	parser.add_argument("--no-allocation-trace", action="store_true",
		help="disable gc-verbose profiler for parity runs; System.Runtime counters remain enabled")
	parser.add_argument("--tick-cap", type=int, default=16000,
		help="fixed world-tick cap for A5 parity; keeps the qualification below natural match end")
	parser.add_argument("--allow-stale-build", action="store_true",
		help="forward to the batch harness when this worktree's mod sources are "
			"newer than the pinned engine/bin dlls only by checkout timestamp "
			"(e.g. hardlink-materialized engine); receipt records binary sha256")
	args = parser.parse_args()
	if args.reaggregate_source is not None:
		if args.execute_a5_diagnostic or args.reaggregate_output is None:
			raise ValueError("re-aggregate requires new output and forbids execute mode")
		reaggregate(args.reaggregate_source, args.source_raw_sha256, args.reaggregate_output)
		print(json.dumps({"mode": "NO_LAUNCH_REAGGREGATE", "output": str(args.reaggregate_output)}))
		return 0
	if args.source_raw_sha256 or args.reaggregate_output:
		raise ValueError("re-aggregate arguments require an existing source receipt")
	if not 1000 <= args.tick_cap < 17000:
		raise RuntimeError("A5 parity cap must be in [1000, 17000) to stay below observed natural end")
	CAP_TICK = args.tick_cap
	if not (3.0 <= args.process_stop_gib <= 8.24):
		raise RuntimeError("process stop must not exceed the measured headless peak plus 3 GiB")
	if not (3.0 <= args.host_hard_floor_gib < args.host_soft_stop_gib):
		raise RuntimeError("host hard floor must be >=3 GiB and strictly below host soft stop")
	PROCESS_STOP = int(args.process_stop_gib * 1024**3)
	HOST_HARD_FLOOR = int(args.host_hard_floor_gib * 1024**3)
	HOST_SOFT_STOP = int(args.host_soft_stop_gib * 1024**3)
	TRACE_ENABLED = not args.no_allocation_trace
	ALLOW_STALE_BUILD = args.allow_stale_build
	output_dir = args.output_dir if args.output_dir.is_absolute() else ROOT / args.output_dir
	OUT = output_dir.resolve()
	if not args.execute_a5_diagnostic:
		print(json.dumps({"mode": "NO_LAUNCH_A5_PREFLIGHT", "baseline": BASELINE,
			"memory_fix": MEMORY_FIX, "seed": SEED, "cells": 2, "tick_cap": CAP_TICK,
			"per_process_stop_bytes": PROCESS_STOP, "host_soft_stop_bytes": HOST_SOFT_STOP,
			"host_hard_floor_bytes": HOST_HARD_FLOOR, "poll_seconds": POLL,
			"allocation_trace_enabled": TRACE_ENABLED, "campaign_gate_passed": False}, indent=2))
		return 0
	if subprocess.run(["git", "merge-base", "--is-ancestor", MEMORY_FIX, "HEAD"], cwd=ROOT).returncode:
		raise RuntimeError("memory-fix commit is not an ancestor")
	if subprocess.run(["git", "status", "--porcelain", "--", "mods", "OpenRA.Mods.Cameo", "tools/ai"], cwd=ROOT, capture_output=True, text=True).stdout.strip():
		raise RuntimeError("gameplay/tooling source is dirty; commit the exact probe before qualification")
	if subprocess.run(["git", "merge-base", "--is-ancestor", BASELINE, "HEAD"], cwd=ROOT).returncode:
		raise RuntimeError("pinned baseline is not an ancestor")
	if pilot.read_engine_version(ROOT / "engine/VERSION") != ENGINE:
		raise RuntimeError("engine pin mismatch")
	host = host_memory_status()
	if not admission_ok(host["available_physical_bytes"], host["commit_available_bytes"]):
		raise RuntimeError(f"diagnostic launch admission failed: {host}")
	if not COUNTERS_EXE.is_file():
		raise RuntimeError(f"System.Runtime counter tool missing: {COUNTERS_EXE}")
	if not TRACE_EXE.is_file():
		raise RuntimeError(f"gc-verbose trace tool missing: {TRACE_EXE}")
	binary_hashes()  # Validate all three binaries before any launch.
	if process_rows(OUT):
		raise RuntimeError("unexpected OpenRA process already names probe output")
	if OUT.exists():
		raise RuntimeError("probe output exists; preserve and choose a new directory")
	OUT.mkdir(parents=True)
	map_dir = OUT / "a5_fixed_tick_map"
	shutil.copytree(ROOT / "mods/cameo/maps/ai_duel_gate_20260928", map_dir)
	map_monitor = pilot.install_campaign_monitor(map_dir, 2, cap_tick=CAP_TICK, sample_interval=2000,
		rules_filename="rules.yaml", seat_names=["BotA", "BotB"])
	results = []
	for index in (1, 2):
		host = host_memory_status()
		if not admission_ok(host["available_physical_bytes"], host["commit_available_bytes"]):
			(OUT / "admission_failure.json").write_text(json.dumps(host, indent=2) + "\n", encoding="utf-8")
			break
		if subprocess.run(["powershell.exe", "-NoProfile", "-NonInteractive", "-Command",
			"if (Get-Process OpenRA -ErrorAction SilentlyContinue) { exit 1 } else { exit 0 }"]).returncode:
			raise RuntimeError("foreign OpenRA process exists before launch")
		result = run_cell(index)
		results.append(result)
		(OUT / f"{result['cell']}.receipt.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
		if not result["valid_game"]:
			break
	# Same-seed, same-tree A5 diagnostic only; compare exactly one replay per run.
	replays = [list((OUT / f"run-{i:02d}").rglob("*.orarep")) for i in (1, 2)]
	if len(results) == 2 and all(len(x) == 1 for x in replays):
		diff = subprocess.run([sys.executable, str(ROOT / "tools/ai/order_stream_diff.py"),
			str(replays[0][0]), str(replays[1][0]), "--json"], cwd=ROOT, capture_output=True, text=True)
		(OUT / "order_stream_diff.json").write_text(diff.stdout + diff.stderr, encoding="utf-8")
		try:
			order_json = json.loads(diff.stdout.splitlines()[-1])
			order_verdict = order_json.get("verdict", "UNPARSED") if diff.returncode == 0 else "FAIL"
		except (IndexError, json.JSONDecodeError):
			order_verdict = "UNPARSED"
	else:
		order_verdict = "INCOMPLETE_NO_PAIR_OF_REPLAYS"
	processes_left = subprocess.run(["powershell.exe", "-NoProfile", "-NonInteractive", "-Command",
		"@(Get-Process OpenRA -ErrorAction SilentlyContinue).Count"], capture_output=True, text=True)
	receipt = {"purpose": "A5_FIXED_TICK_PARITY_QUALIFICATION_ONLY_NOT_CAMPAIGN_OUTCOME",
		"cap_tick": CAP_TICK, "map_monitor": map_monitor,
		"baseline_commit": BASELINE, "source_head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
		"binary_sha256": binary_hashes(),
		"dotnet_counters_sha256": file_sha256(COUNTERS_EXE),
		"engine_version": ENGINE, "seed": SEED, "pair": results,
		"order_stream_verdict": order_verdict, "system_openra_process_count": processes_left.stdout.strip(),
		"a5_diagnostic_passed": len(results) == 2 and all(r["valid_game"] for r in results) and
			results[0].get("fingerprint_id") == results[1].get("fingerprint_id") and order_verdict in ("IDENTICAL", "IDENTICAL_TAIL_FLUSH"),
		"campaign_gate_passed": False}
	if len(results) == 2: receipt = aggregate_run_config(receipt, results)
	(OUT / "A5_memory_probe_receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
	print(json.dumps({"output": str(OUT), "pair_count": len(results),
		"order_stream_verdict": order_verdict, "campaign_gate_passed": receipt["campaign_gate_passed"]}, indent=2))
	return 0 if receipt["a5_diagnostic_passed"] else 2


if __name__ == "__main__":
	try:
		raise SystemExit(main())
	except (OSError, ValueError, KeyError, TypeError, RuntimeError, subprocess.SubprocessError) as exc:
		print(f"A5 probe failed closed: {exc}", file=sys.stderr)
		raise SystemExit(2)
