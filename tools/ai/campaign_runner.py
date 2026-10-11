#!/usr/bin/env python3
"""Safety primitives for one-game A/B campaign execution.

This module is intentionally not a queue launcher. A reviewed manifest and a
separate launch command are still required. It provides process-scoped memory
sampling/termination and immutable per-game receipt writes for that adapter.
"""
from __future__ import annotations

import ctypes
import copy
import hashlib
import json
import os
import pathlib
import re
import subprocess
import time
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any

SCHEMA_VERSION = 1
GIB = 1024 ** 3
MIN_FREE_BYTES = 6 * GIB
PRIVATE_STOP_BYTES = 6 * GIB + GIB // 2
PRIVATE_HARD_BYTES = 8 * GIB
WORLD_TICK_CAP = 45_000
WALL_TIMEOUT_SECONDS = 3_000
STALL_TIMEOUT_SECONDS = 180


def _utc_now() -> str:
	return datetime.now(timezone.utc).isoformat(timespec="seconds").replace("+00:00", "Z")


def _sha256_file(path: pathlib.Path) -> str:
	digest = hashlib.sha256()
	with path.open("rb") as stream:
		for block in iter(lambda: stream.read(1024 * 1024), b""):
			digest.update(block)
	return digest.hexdigest()


def free_physical_bytes() -> int:
	"""Return system-available physical RAM, using the Windows memory API."""
	if os.name != "nt":
		raise OSError("campaign runner currently requires Windows memory APIs")

	class MEMORYSTATUSEX(ctypes.Structure):
		_fields_ = [("dwLength", ctypes.c_ulong), ("dwMemoryLoad", ctypes.c_ulong),
			("ullTotalPhys", ctypes.c_ulonglong), ("ullAvailPhys", ctypes.c_ulonglong),
			("ullTotalPageFile", ctypes.c_ulonglong), ("ullAvailPageFile", ctypes.c_ulonglong),
			("ullTotalVirtual", ctypes.c_ulonglong), ("ullAvailVirtual", ctypes.c_ulonglong),
			("ullAvailExtendedVirtual", ctypes.c_ulonglong)]

	status = MEMORYSTATUSEX()
	status.dwLength = ctypes.sizeof(status)
	kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
	kernel32.GlobalMemoryStatusEx.argtypes = [ctypes.POINTER(MEMORYSTATUSEX)]
	kernel32.GlobalMemoryStatusEx.restype = ctypes.c_int
	if not kernel32.GlobalMemoryStatusEx(ctypes.byref(status)):
		raise ctypes.WinError()
	return int(status.ullAvailPhys)


def process_private_bytes(pid: int) -> int:
	"""Return exact Windows PROCESS_MEMORY_COUNTERS_EX.PrivateUsage for one PID."""
	if os.name != "nt":
		raise OSError("campaign runner currently requires Windows process counters")
	if type(pid) is not int or pid <= 0:
		raise ValueError("invalid process id")

	class PROCESS_MEMORY_COUNTERS_EX(ctypes.Structure):
		_fields_ = [("cb", ctypes.c_ulong), ("PageFaultCount", ctypes.c_ulong),
			("PeakWorkingSetSize", ctypes.c_size_t), ("WorkingSetSize", ctypes.c_size_t),
			("QuotaPeakPagedPoolUsage", ctypes.c_size_t), ("QuotaPagedPoolUsage", ctypes.c_size_t),
			("QuotaPeakNonPagedPoolUsage", ctypes.c_size_t), ("QuotaNonPagedPoolUsage", ctypes.c_size_t),
			("PagefileUsage", ctypes.c_size_t), ("PeakPagefileUsage", ctypes.c_size_t),
			("PrivateUsage", ctypes.c_size_t)]

	PROCESS_QUERY_INFORMATION = 0x0400
	PROCESS_VM_READ = 0x0010
	kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
	kernel32.OpenProcess.argtypes = [ctypes.c_ulong, ctypes.c_int, ctypes.c_ulong]
	kernel32.OpenProcess.restype = ctypes.c_void_p
	kernel32.CloseHandle.argtypes = [ctypes.c_void_p]
	kernel32.CloseHandle.restype = ctypes.c_int
	psapi = ctypes.WinDLL("psapi", use_last_error=True)
	psapi.GetProcessMemoryInfo.argtypes = [ctypes.c_void_p, ctypes.c_void_p, ctypes.c_ulong]
	psapi.GetProcessMemoryInfo.restype = ctypes.c_int
	handle = kernel32.OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, False, pid)
	if not handle:
		raise ctypes.WinError()
	try:
		counters = PROCESS_MEMORY_COUNTERS_EX()
		counters.cb = ctypes.sizeof(counters)
		if not psapi.GetProcessMemoryInfo(handle, ctypes.byref(counters), counters.cb):
			raise ctypes.WinError()
		return int(counters.PrivateUsage)
	finally:
		kernel32.CloseHandle(handle)


@dataclass(frozen=True)
class ProcessSample:
	utc: str
	private_bytes: int
	free_physical_bytes: int
	world_tick: int | None
	actor_count: int | None


@dataclass(frozen=True)
class SupervisedResult:
	pid: int
	exit_code: int | None
	status: str
	started_utc: str
	finished_utc: str
	wall_seconds: float
	peak_private_bytes: int
	samples: tuple[ProcessSample, ...]
	pid_scoped_cleanup: bool
	seed_pin_line: str | None
	seed_server_log_sha256: str
	cap_marker_tick: int | None


def _stop_owned_process(process: subprocess.Popen[Any]) -> bool:
	"""Stop exactly the child created here; never search or kill by image name."""
	if process.poll() is not None:
		return True
	process.terminate()
	try:
		process.wait(timeout=10)
		return True
	except subprocess.TimeoutExpired:
		process.kill()
		try:
			process.wait(timeout=10)
			return True
		except subprocess.TimeoutExpired:
			return False


def run_supervised(
	executable: pathlib.Path,
	arguments: list[str],
	*,
	cwd: pathlib.Path,
	stdout_path: pathlib.Path,
	seed: int,
	progress_log: pathlib.Path,
	server_log_path: pathlib.Path,
	wall_timeout_seconds: int = WALL_TIMEOUT_SECONDS,
	stall_timeout_seconds: int = STALL_TIMEOUT_SECONDS,
	sample_interval_seconds: float = 5.0,
	private_stop_bytes: int = PRIVATE_STOP_BYTES,
	free_ram_floor_bytes: int = MIN_FREE_BYTES,
) -> SupervisedResult:
	"""Run one game with a child-only watchdog. No retries are performed."""
	if type(seed) is not int or seed < 0:
		raise ValueError("seed must be a nonnegative integer")
	if not 0 < wall_timeout_seconds <= WALL_TIMEOUT_SECONDS or not 0 < stall_timeout_seconds <= STALL_TIMEOUT_SECONDS:
		raise ValueError("wall/stall watchdog may not exceed the frozen policy bounds")
	if not 0 < sample_interval_seconds <= 5:
		raise ValueError("memory sample interval must be positive and at most five seconds")
	if private_stop_bytes != PRIVATE_STOP_BYTES or free_ram_floor_bytes < MIN_FREE_BYTES:
		raise ValueError("memory and free-RAM limits may only be tightened from the frozen policy")

	executable = pathlib.Path(executable).resolve()
	cwd = pathlib.Path(cwd).resolve()
	stdout_path = pathlib.Path(stdout_path).resolve()
	progress_log = pathlib.Path(progress_log).resolve()
	server_log_path = pathlib.Path(server_log_path).resolve()
	if progress_log.exists() or server_log_path.exists():
		raise FileExistsError("progress and server logs must be fresh paths for this game")
	if free_physical_bytes() < free_ram_floor_bytes:
		raise RuntimeError("free-RAM admission floor not met; no game process started")
	stdout_path.parent.mkdir(parents=True, exist_ok=True)
	output = stdout_path.open("xb")
	env = os.environ.copy()
	env["CAMEO_DEV_SEED"] = str(seed)
	started = _utc_now()
	start_mono = time.monotonic()
	last_progress = start_mono
	last_mtime = None
	samples: list[ProcessSample] = []
	peak = 0
	seed_line: str | None = None
	world_tick: int | None = None
	actor_count: int | None = None
	cap_marker_tick: int | None = None
	log_offset = 0
	log_carry = b""
	status = "PROCESS_DIED"
	cleaned = True
	process: subprocess.Popen[Any] | None = None

	def read_log_markers(final: bool = False) -> None:
		nonlocal log_offset, log_carry, seed_line, world_tick, actor_count, cap_marker_tick
		if not server_log_path.is_file():
			return
		with server_log_path.open("rb") as stream:
			stream.seek(log_offset)
			chunk = stream.read()
			log_offset = stream.tell()
		pending = log_carry + chunk
		parts = pending.split(b"\n")
		lines = parts[:-1]
		log_carry = b"" if pending.endswith(b"\n") else parts[-1]
		if final and log_carry:
			lines.append(log_carry)
			log_carry = b""
		for raw_line in lines:
			line = raw_line.decode("utf-8", "replace").strip()
			if line == f"CAMEO DEV SEED pinned - RandomSeed={seed} (parity harness)":
				seed_line = line
			match = re.search(r"AB_CAMPAIGN_ACTOR_SAMPLE tick=(\d+) actor_count=(\d+)", line)
			if match:
				world_tick, actor_count = int(match.group(1)), int(match.group(2))
			cap_match = re.search(r"AB_CAMPAIGN_CAP tick=(\d+)", line)
			if cap_match:
				cap_marker_tick = int(cap_match.group(1))

	try:
		process = subprocess.Popen([str(executable), *arguments], cwd=cwd, env=env,
			stdout=output, stderr=subprocess.STDOUT,
			creationflags=getattr(subprocess, "BELOW_NORMAL_PRIORITY_CLASS", 0) if os.name == "nt" else 0)
		deadline = start_mono + wall_timeout_seconds
		while process.poll() is None:
			now = time.monotonic()
			try:
				private = process_private_bytes(process.pid)
				available = free_physical_bytes()
			except OSError:
				status = "MONITOR_ERROR"
				break
			read_log_markers()
			samples.append(ProcessSample(_utc_now(), private, available, world_tick, actor_count))
			peak = max(peak, private)
			if private >= PRIVATE_HARD_BYTES:
				status = "MEMORY_KILL"
				break
			if private >= private_stop_bytes:
				status = "MEMORY_KILL"
				break
			if available < free_ram_floor_bytes:
				status = "SYSTEM_MEMORY_STOP"
				break
			try:
				mtime = progress_log.stat().st_mtime_ns
			except OSError:
				mtime = None
			if mtime is not None and mtime != last_mtime:
				last_mtime = mtime
				last_progress = now
			elif now - last_progress >= stall_timeout_seconds:
				status = "STALL"
				break
			if now >= deadline:
				status = "WALL_TIMEOUT"
				break
			time.sleep(min(sample_interval_seconds, max(0.0, deadline - now)))

		read_log_markers(final=True)
		if process.poll() is None:
			cleaned = _stop_owned_process(process)
		elif status == "PROCESS_DIED":
			status = "NATURAL_EXIT" if process.returncode == 0 else "PROCESS_DIED"
		if status == "NATURAL_EXIT" and seed_line is None:
			status = "SEED_UNVERIFIED"
		log_digest = _sha256_file(server_log_path) if server_log_path.is_file() else ""
		return SupervisedResult(process.pid, process.returncode, status, started, _utc_now(),
			time.monotonic() - start_mono, peak, tuple(samples), cleaned, seed_line, log_digest, cap_marker_tick)
	finally:
		if process is not None and process.poll() is None:
			cleaned = _stop_owned_process(process)
		output.flush()
		os.fsync(output.fileno())
		output.close()


def apply_supervised_result(receipt: dict[str, Any], result: SupervisedResult, seed: int) -> dict[str, Any]:
	"""Attach watchdog evidence without treating a zero exit as a game outcome."""
	if type(seed) is not int or seed < 0:
		raise ValueError("seed must be a nonnegative integer")
	if not isinstance(result, SupervisedResult):
		raise TypeError("result must be a SupervisedResult")
	validate_receipt(receipt)
	out = copy.deepcopy(receipt)
	limit = out.get("memory", {}).get("limit_private_bytes", PRIVATE_STOP_BYTES)
	out["seed_proof"] = {
		"env_value": str(seed),
		"server_log_pin_line": result.seed_pin_line,
		"server_log_sha256": result.seed_server_log_sha256 or None,
	}
	out["memory"] = {
		"process_id": result.pid,
		"peak_private_bytes": result.peak_private_bytes,
		"limit_private_bytes": limit,
		"samples": [
			{"utc": sample.utc, "world_tick": sample.world_tick, "private_bytes": sample.private_bytes, "actor_count": sample.actor_count}
			for sample in result.samples
		],
	}
	out["runtime"] = {
		"started_utc": result.started_utc,
		"finished_utc": result.finished_utc,
		"exit_code": result.exit_code,
		"wall_seconds": result.wall_seconds,
		"stall_seconds": STALL_TIMEOUT_SECONDS,
		"pid_scoped_cleanup": result.pid_scoped_cleanup,
	}
	out["world_tick"] = result.cap_marker_tick if result.cap_marker_tick is not None else next(
		(sample.world_tick for sample in reversed(result.samples) if sample.world_tick is not None), None)
	out["cap"] = {
		"world_tick_cap": WORLD_TICK_CAP,
		"marker_observed": result.cap_marker_tick is not None,
		"marker_tick": result.cap_marker_tick,
	}
	out["winner_team"] = None
	support = out.get("artifacts", {}).get("support_bundle", {})
	complete_support = isinstance(support, dict) and isinstance(support.get("path"), str) and isinstance(support.get("sha256"), str)
	if (result.status == "NATURAL_EXIT" and result.cap_marker_tick == WORLD_TICK_CAP
			and result.seed_pin_line is not None and complete_support and result.peak_private_bytes < limit):
		out["end_class"] = "CAP"
		out["end_reason"] = "WORLD_TICK_CAP"
	else:
		out["end_class"] = "INCOMPLETE"
		if result.cap_marker_tick == WORLD_TICK_CAP and not complete_support:
			out["end_reason"] = "CAP_SUPPORT_MISSING"
		elif result.status == "NATURAL_EXIT":
			out["end_reason"] = "NATURAL_EXIT_UNADJUDICATED"
		else:
			out["end_reason"] = result.status
	validate_receipt(out)
	return out


def validate_receipt(receipt: dict[str, Any]) -> None:
	"""Validate the receipt contract, including cross-field evidence invariants."""
	def fail(message: str) -> None:
		raise ValueError(message)

	def exact_object(value: Any, keys: set[str], label: str) -> dict[str, Any]:
		if not isinstance(value, dict):
			fail(f"{label} must be an object")
			return {}
		missing, extra = keys - value.keys(), value.keys() - keys
		if missing or extra:
			fail(f"{label} keys mismatch; missing={sorted(missing)}, extra={sorted(extra)}")
		return value

	def string(value: Any, label: str, *, nullable: bool = False) -> None:
		if nullable and value is None:
			return
		if not isinstance(value, str) or not value.strip():
			fail(f"{label} must be a nonempty string" + (" or null" if nullable else ""))

	def integer(value: Any, label: str, *, nullable: bool = False, minimum: int = 0) -> None:
		if nullable and value is None:
			return
		if type(value) is not int or value < minimum:
			fail(f"{label} must be an integer >= {minimum}" + (" or null" if nullable else ""))

	def number(value: Any, label: str) -> None:
		import math
		if value is not None and (type(value) not in (int, float) or not math.isfinite(value)):
			fail(f"{label} must be a finite number or null")

	def sha(value: Any, label: str, *, nullable: bool = False) -> None:
		if nullable and value is None:
			return
		if not isinstance(value, str) or re.fullmatch(r"[0-9a-f]{64}", value) is None:
			fail(f"{label} must be a lowercase SHA-256" + (" or null" if nullable else ""))

	def utc(value: Any, label: str, *, nullable: bool = False) -> None:
		if nullable and value is None:
			return
		if not isinstance(value, str):
			fail(f"{label} must be an RFC3339 timestamp" + (" or null" if nullable else ""))
		try:
			parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
		except ValueError:
			fail(f"{label} must be an RFC3339 timestamp")
		if parsed.tzinfo is None:
			fail(f"{label} must include a timezone")

	root = exact_object(receipt, {
		"schema_version", "campaign_id", "manifest_sha256", "baseline_commit", "engine_sha256", "switch", "setup_id",
		"map", "pair_id", "pair_member", "seed", "seed_proof", "teams", "resolved_seats", "end_class", "end_reason",
		"winner_team", "world_tick", "cap", "memory", "runtime", "artifacts",
	}, "receipt")
	if type(root["schema_version"]) is not int or root["schema_version"] != SCHEMA_VERSION:
		fail("receipt schema_version must be 1")
	for key in ("campaign_id", "switch", "setup_id", "pair_id"):
		string(root[key], key)
	sha(root["manifest_sha256"], "manifest_sha256")
	sha(root["engine_sha256"], "engine_sha256")
	if not isinstance(root["baseline_commit"], str) or re.fullmatch(r"[0-9a-f]{40}", root["baseline_commit"]) is None:
		fail("baseline_commit must be a lowercase 40-character commit SHA")
	if type(root["pair_member"]) is not int or root["pair_member"] not in (0, 1):
		fail("pair_member must be 0 or 1")
	integer(root["seed"], "seed")
	integer(root["world_tick"], "world_tick", nullable=True)

	map_info = exact_object(root["map"], {"name", "path", "sha256", "generated_map_yaml_sha256", "required_seats"}, "map")
	for key in ("name", "path"):
		string(map_info[key], f"map.{key}")
	if (pathlib.PurePosixPath(map_info["path"]).is_absolute() or ".." in pathlib.PurePosixPath(map_info["path"]).parts
			or "\\" in map_info["path"] or re.match(r"^[A-Za-z]:", map_info["path"])):
		fail("map.path must be repository-relative POSIX path")
	sha(map_info["sha256"], "map.sha256")
	sha(map_info["generated_map_yaml_sha256"], "map.generated_map_yaml_sha256")
	integer(map_info["required_seats"], "map.required_seats", minimum=2)

	seed_proof = exact_object(root["seed_proof"], {"env_value", "server_log_pin_line", "server_log_sha256"}, "seed_proof")
	string(seed_proof["env_value"], "seed_proof.env_value")
	if seed_proof["env_value"] != str(root["seed"]):
		fail("seed_proof.env_value does not match seed")
	string(seed_proof["server_log_pin_line"], "seed_proof.server_log_pin_line", nullable=True)
	sha(seed_proof["server_log_sha256"], "seed_proof.server_log_sha256", nullable=True)
	if seed_proof["server_log_pin_line"] is not None and seed_proof["server_log_pin_line"] != f"CAMEO DEV SEED pinned - RandomSeed={root['seed']} (parity harness)":
		fail("seed_proof server pin line does not match seed")

	teams = exact_object(root["teams"], {"A", "B"}, "teams")
	team_ids: dict[str, list[str]] = {}
	for team_name in ("A", "B"):
		team = exact_object(teams[team_name], {"arm", "bot_type", "seat_ids"}, f"teams.{team_name}")
		if team["arm"] not in ("control", "treatment"):
			fail(f"teams.{team_name}.arm must be control or treatment")
		string(team["bot_type"], f"teams.{team_name}.bot_type")
		if not isinstance(team["seat_ids"], list) or not all(isinstance(x, str) and x for x in team["seat_ids"]):
			fail(f"teams.{team_name}.seat_ids must be a string array")
		team_ids[team_name] = team["seat_ids"]
	if teams["A"]["arm"] == teams["B"]["arm"]:
		fail("the two teams must represent opposite campaign arms")

	if not isinstance(root["resolved_seats"], list) or len(root["resolved_seats"]) != map_info["required_seats"]:
		fail("resolved_seats count must equal map.required_seats")
	seat_ids: set[str] = set()
	seat_homes: set[str] = set()
	seats_by_team: dict[str, list[dict[str, Any]]] = {"A": [], "B": []}
	for index, raw_seat in enumerate(root["resolved_seats"]):
		seat = exact_object(raw_seat, {"seat_id", "team", "arm", "bot_type", "faction", "home", "spawn", "proof_source", "outcome", "metrics"}, f"resolved_seats[{index}]")
		for key in ("seat_id", "bot_type", "faction", "home", "proof_source"):
			string(seat[key], f"resolved_seats[{index}].{key}")
		if seat["team"] not in ("A", "B") or seat["arm"] not in ("control", "treatment"):
			fail(f"resolved_seats[{index}] has invalid team or arm")
		integer(seat["spawn"], f"resolved_seats[{index}].spawn")
		if seat["seat_id"] in seat_ids or seat["home"] in seat_homes:
			fail("duplicate seat_id or home")
		seat_ids.add(seat["seat_id"])
		seat_homes.add(seat["home"])
		if "map.yaml" not in seat["proof_source"].lower() or "match" not in seat["proof_source"].lower():
			fail("proof_source must cite generated map.yaml and match-record evidence")
		team = teams[seat["team"]]
		if (seat["arm"], seat["bot_type"]) != (team["arm"], team["bot_type"]):
			fail(f"resolved seat {seat['seat_id']} disagrees with its team arm/bot_type")
		if seat["outcome"] not in ("won", "lost", "draw", "unknown"):
			fail(f"resolved_seats[{index}].outcome is invalid")
		metrics = exact_object(seat["metrics"], {"earned", "spent", "banked", "army_value", "kills", "deaths", "timeline"}, f"resolved_seats[{index}].metrics")
		for key in ("earned", "spent", "banked", "army_value"):
			number(metrics[key], f"metrics.{key}")
		for key in ("kills", "deaths"):
			integer(metrics[key], f"metrics.{key}", nullable=True)
		if not isinstance(metrics["timeline"], list):
			fail(f"metrics.timeline for {seat['seat_id']} must be an array")
		last_tick = -1
		for row_index, raw_row in enumerate(metrics["timeline"]):
			row = exact_object(raw_row, {"tick", "earned", "spent", "banked", "army_value", "kills", "deaths"}, f"metrics.timeline[{row_index}]")
			integer(row["tick"], "timeline.tick")
			if row["tick"] <= last_tick:
				fail(f"metrics.timeline for {seat['seat_id']} must have unique ascending ticks")
			last_tick = row["tick"]
			for key in ("earned", "spent", "banked", "army_value"):
				number(row[key], f"timeline.{key}")
			for key in ("kills", "deaths"):
				integer(row[key], f"timeline.{key}", nullable=True)
		seats_by_team[seat["team"]].append(seat)
	if set(team_ids["A"]) != {s["seat_id"] for s in seats_by_team["A"]} or len(team_ids["A"]) != len(set(team_ids["A"])):
		fail("teams.A.seat_ids must exactly enumerate team A resolved seats")
	if set(team_ids["B"]) != {s["seat_id"] for s in seats_by_team["B"]} or len(team_ids["B"]) != len(set(team_ids["B"])):
		fail("teams.B.seat_ids must exactly enumerate team B resolved seats")

	end_class = root["end_class"]
	if end_class not in ("NATURAL", "CAP", "INCOMPLETE"):
		fail("invalid end_class")
	string(root["end_reason"], "end_reason", nullable=True)
	if root["winner_team"] not in ("A", "B", None):
		fail("winner_team must be A, B, or null")
	cap = exact_object(root["cap"], {"world_tick_cap", "marker_observed", "marker_tick"}, "cap")
	if type(cap["world_tick_cap"]) is not int or cap["world_tick_cap"] != WORLD_TICK_CAP:
		fail("cap.world_tick_cap must equal the frozen 45000-tick cap")
	if type(cap["marker_observed"]) is not bool:
		fail("cap.marker_observed must be boolean")
	integer(cap["marker_tick"], "cap.marker_tick", nullable=True)
	if cap["marker_observed"] != (cap["marker_tick"] is not None):
		fail("cap marker boolean and tick disagree")

	memory = exact_object(root["memory"], {"process_id", "peak_private_bytes", "limit_private_bytes", "samples"}, "memory")
	integer(memory["process_id"], "memory.process_id", nullable=True, minimum=1)
	integer(memory["peak_private_bytes"], "memory.peak_private_bytes")
	if type(memory["limit_private_bytes"]) is not int or memory["limit_private_bytes"] != PRIVATE_STOP_BYTES:
		fail("memory.limit_private_bytes must equal the frozen 6.5 GiB stop")
	if not isinstance(memory["samples"], list):
		fail("memory.samples must be an array")
	for index, sample_raw in enumerate(memory["samples"]):
		sample = exact_object(sample_raw, {"utc", "world_tick", "private_bytes", "actor_count"}, f"memory.samples[{index}]")
		utc(sample["utc"], f"memory.samples[{index}].utc")
		integer(sample["world_tick"], "memory sample world_tick", nullable=True)
		integer(sample["private_bytes"], "memory sample private_bytes")
		integer(sample["actor_count"], "memory sample actor_count", nullable=True)
	if memory["samples"] and memory["peak_private_bytes"] < max(s["private_bytes"] for s in memory["samples"]):
		fail("memory.peak_private_bytes is below a recorded sample")

	runtime = exact_object(root["runtime"], {"started_utc", "finished_utc", "exit_code", "wall_seconds", "stall_seconds", "pid_scoped_cleanup"}, "runtime")
	utc(runtime["started_utc"], "runtime.started_utc", nullable=True)
	utc(runtime["finished_utc"], "runtime.finished_utc", nullable=True)
	if runtime["exit_code"] is not None and type(runtime["exit_code"]) is not int:
		fail("runtime.exit_code must be an integer or null")
	number(runtime["wall_seconds"], "runtime.wall_seconds")
	if runtime["wall_seconds"] is not None and runtime["wall_seconds"] < 0:
		fail("runtime.wall_seconds cannot be negative")
	if type(runtime["stall_seconds"]) is not int or runtime["stall_seconds"] != STALL_TIMEOUT_SECONDS:
		fail("runtime.stall_seconds must equal the frozen 180-second bound")
	if type(runtime["pid_scoped_cleanup"]) is not bool:
		fail("runtime.pid_scoped_cleanup must be boolean")

	artifacts = exact_object(root["artifacts"], {"server_log", "driver_log", "matches_jsonl", "replay", "map_yaml", "support_bundle"}, "artifacts")
	for name, raw_artifact in artifacts.items():
		artifact = exact_object(raw_artifact, {"path", "sha256", "missing_reason"}, f"artifacts.{name}")
		string(artifact["path"], f"artifacts.{name}.path", nullable=True)
		sha(artifact["sha256"], f"artifacts.{name}.sha256", nullable=True)
		string(artifact["missing_reason"], f"artifacts.{name}.missing_reason", nullable=True)
		if artifact["path"] is None:
			if artifact["sha256"] is not None or artifact["missing_reason"] is None:
				fail(f"artifacts.{name} missing path requires null SHA and a missing_reason")
		elif artifact["sha256"] is None or artifact["missing_reason"] is not None:
			fail(f"artifacts.{name} present path requires SHA and null missing_reason")

	if end_class == "NATURAL":
		string(root["end_reason"], "NATURAL end_reason")
		if root["world_tick"] is None or any(s["outcome"] == "unknown" for s in root["resolved_seats"]):
			fail("NATURAL receipt requires final tick and known outcomes for every seat")
		if runtime["exit_code"] != 0:
			fail("NATURAL receipt requires a zero exit code")
		if cap["marker_observed"]:
			fail("cap marker requires end_class CAP")
		team_results = {team: {s["outcome"] for s in seats_by_team[team]} for team in ("A", "B")}
		if any(not outcomes for outcomes in team_results.values()) or any(len(outcomes) != 1 for outcomes in team_results.values()):
			fail("NATURAL seat outcomes must agree within each team")
		outcomes = {team: next(iter(values)) for team, values in team_results.items()}
		if outcomes["A"] == outcomes["B"]:
			if outcomes["A"] != "draw" or root["winner_team"] is not None:
				fail("NATURAL winner is ambiguous or contradicts seat outcomes")
		elif outcomes == {"A": "won", "B": "lost"}:
			if root["winner_team"] != "A": fail("winner_team contradicts seat outcomes")
		elif outcomes == {"A": "lost", "B": "won"}:
			if root["winner_team"] != "B": fail("winner_team contradicts seat outcomes")
		else:
			fail("NATURAL team outcomes are contradictory")
		if memory["peak_private_bytes"] >= memory["limit_private_bytes"]:
			fail("memory over-limit receipt must be INCOMPLETE memory kill")
		if seed_proof["server_log_pin_line"] is None:
			fail("NATURAL receipt requires server-log seed proof")
	elif end_class == "CAP":
		string(root["end_reason"], "CAP end_reason")
		if root["winner_team"] is not None or not cap["marker_observed"] or cap["marker_tick"] != cap["world_tick_cap"] or root["world_tick"] != cap["world_tick_cap"]:
			fail("CAP receipt requires the configured cap marker/tick and null winner")
		if memory["peak_private_bytes"] >= memory["limit_private_bytes"]:
			fail("memory over-limit receipt must be INCOMPLETE memory kill")
		if artifacts["support_bundle"]["path"] is None:
			fail("CAP receipt requires a complete support bundle")
		if seed_proof["server_log_pin_line"] is None:
			fail("CAP receipt requires server-log seed proof")
	else:
		if root["winner_team"] is not None or root["end_reason"] is None:
			fail("INCOMPLETE receipt requires a concrete reason and null winner")
		if memory["peak_private_bytes"] >= memory["limit_private_bytes"] and "memory" not in root["end_reason"].lower():
			fail("memory over-limit INCOMPLETE receipt must identify a memory kill")
	

def write_immutable_receipt(path: pathlib.Path, receipt: dict[str, Any]) -> str:
	"""Atomically create one JSON receipt; existing receipts are never overwritten."""
	path = pathlib.Path(path).resolve()
	validate_receipt(receipt)
	path.parent.mkdir(parents=True, exist_ok=True)
	if path.exists():
		raise FileExistsError(f"immutable receipt already exists: {path}")
	temp = path.with_name(f".{path.name}.{os.getpid()}.{time.time_ns()}.tmp")
	data = (json.dumps(receipt, sort_keys=True, ensure_ascii=False, separators=(",", ":")) + "\n").encode("utf-8")
	try:
		fd = os.open(temp, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
		with os.fdopen(fd, "wb") as stream:
			stream.write(data)
			stream.flush()
			os.fsync(stream.fileno())
		# On Windows os.rename fails if the immutable destination appeared meanwhile.
		os.rename(temp, path)
	finally:
		try:
			temp.unlink()
		except FileNotFoundError:
			pass
	return hashlib.sha256(data).hexdigest()
