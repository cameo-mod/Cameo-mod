#!/usr/bin/env python3
"""Map engine acceptance + symmetric spawn proof for the A/B campaign maps (pilot gate).

The BU pilot manifest lists "engine acceptance plus symmetric spawn proof for all eight
maps" as an open launch gate (`ab_campaign_pilot.dry_run`: PENDING_ENGINE_PREFLIGHT).
`ab_campaign_pilot.prove_generated_map` already proves the seats STATICALLY (the generated
map.yaml names the planned bot, faction and HomeLocation for every Multi seat). This tool
closes the runtime half: it renders each planned setup's variant with the same pinned
writer, plays it in the engine to a short fixed-tick cap, and proves from the match
records that the engine actually spawned every seat where the plan says. Dual-arm bot
types load from the same complete hardlinked `mods/cameo` tree used by campaign cells:

  * the engine loads the map and reaches game over at the cap (no exception-*.log, no Fatal Lua Error);
  * both arms' bot types register (no `Invalid bot type` / `YamlException`);
  * every seat deploys a verified MCV by world tick 100;
  * one record per planned seat, joined on the spawn coordinate (`player.home`), with the
    planned bot type and faction;
  * every seat's allies are exactly its planned side and its opponents exactly the other side;
  * per setup, game 0 and game 1 swap the arms between the two spawn sets (each arm plays
    each side once) and both arms field the same faction multiset in each game (mirror).

These are capped PREFLIGHT games, never outcome cells: their records live under --out and
must never be fed to `ab_campaign_pilot analyze`. Default render is headless
(OpenRA.Platforms.Null, LESSONS_LEARNED "Headless bot matches") so a whole sweep fits in RAM
next to other work; `--render fast` reproduces the campaign runner's windowed launch.

Usage:
  python tools/ai/map_engine_acceptance.py --manifest docs/audit/ab_pilot_manifest_candidate.json \
      --out results/map-acceptance-<date> [--cap-tick 1500] [--setups 1v1_nuclear_gdi,4v4_ice] [--games 0,1]
Exit code 0 only when every selected cell and every complete pair is ACCEPTED.
"""
from __future__ import annotations

import argparse
import ctypes
import json
import os
import pathlib
import re
import subprocess
import sys
import time

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))
import ab_campaign_pilot as pilot  # noqa: E402
import campaign_runner as campaign  # noqa: E402
import run_ai_match_batch as batch  # noqa: E402

HEADLESS_PLATFORM = "Null"
HEADLESS_LOG_MARKER = "Null platform: headless window, rendering and textures disabled."

# Maintainer ruling 2026-10-11: 10.5 GiB private cap, 3 GiB emergency host stop.
# The accepted e49 headless A5 pair measured 3,042,615,296 bytes peak private; admission
# for one game is that measured peak plus the 3 GiB hard reserve. Two small games require
# 14 GiB free before launch and large-team games remain exclusive.
PRIVATE_CAP_BYTES = int(10.5 * 1024**3)
BOOTSTRAP_HEADLESS_PEAK_BYTES = pilot.HEADLESS_PEAK_BYTES
ADMISSION_RESERVE_BYTES = 3 * 1024**3
EMERGENCY_STOP_FREE_BYTES = 3 * 1024**3


def admission_decision(free_bytes: int | None, min_free_bytes: float) -> str | None:
	"""None admits the cell; otherwise a NOT_RUN reason. Fails closed when RAM is unmeasurable."""
	if free_bytes is None:
		return f"available RAM unknown < {min_free_bytes / 1024**3:.1f} GiB admission floor (fail-closed on unknown RAM)"
	if free_bytes < min_free_bytes:
		return f"available RAM {free_bytes / 1024**3:.1f} GiB < {min_free_bytes / 1024**3:.1f} GiB admission floor"
	return None


def run_stop_reason(current_private: int | None, available: int | None, *, private_cap: int, emergency_free: int) -> str | None:
	"""None keeps the cell running; otherwise the end_reason to kill it with. Fails closed
	when either reading is unmeasurable, and checks the private cap before the host floor."""
	if current_private is None:
		return "MEMORY_SAMPLE_FAILED"
	if current_private >= private_cap:
		return "MEMORY_KILL"
	if available is None or available < emergency_free:
		return "LOW_FREE_RAM"
	return None


def available_ram_bytes() -> int | None:
	"""Available physical RAM on Windows and Linux."""
	try:
		return campaign.available_ram_bytes()
	except (OSError, RuntimeError):
		return None


def rss_bytes(pid: int) -> int | None:
	if os.name == "nt":
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
			return None
		try:
			counters = PROCESS_MEMORY_COUNTERS_EX(); counters.cb = ctypes.sizeof(counters)
			get_info = ctypes.windll.psapi.GetProcessMemoryInfo
			get_info.argtypes = [ctypes.c_void_p, ctypes.POINTER(PROCESS_MEMORY_COUNTERS_EX), ctypes.c_ulong]
			get_info.restype = ctypes.c_bool
			return int(counters.WorkingSetSize) if get_info(handle, ctypes.byref(counters), counters.cb) else None
		finally:
			kernel.CloseHandle(handle)
	try:
		for line in pathlib.Path(f"/proc/{pid}/status").read_text().splitlines():
			if line.startswith("VmRSS:"):
				return int(line.split()[1]) * 1024
	except (OSError, ValueError):
		return None
	return None


def private_bytes(pid: int) -> int | None:
	try:
		return campaign.private_bytes(pid)
	except (OSError, RuntimeError, ValueError):
		return None


def process_rows() -> list[dict]:
	"""Read one process snapshot; callers build ownership only from the Popen PID."""
	if os.name != "nt":
		return []
	p = subprocess.run(["powershell.exe", "-NoProfile", "-NonInteractive", "-Command",
		"Get-CimInstance Win32_Process | Select-Object ProcessId,ParentProcessId,ExecutablePath,CommandLine,CreationDate | ConvertTo-Json -Compress"],
		capture_output=True, text=True, timeout=15)
	if p.returncode:
		raise RuntimeError("process identity query failed: " + p.stderr[-400:])
	if not p.stdout.strip():
		return []
	rows = json.loads(p.stdout)
	return rows if isinstance(rows, list) else [rows]


def owned_tree_rows(root_pid: int, rows: list[dict]) -> list[dict]:
	"""Return only the root process and its current descendants from a process snapshot."""
	owned = {root_pid}
	selected = []
	changed = True
	while changed:
		changed = False
		for row in rows:
			try:
				pid, parent = int(row["ProcessId"]), int(row.get("ParentProcessId") or 0)
			except (KeyError, TypeError, ValueError):
				continue
			if pid not in owned and parent in owned:
				owned.add(pid); changed = True
	for row in rows:
		try:
			if int(row["ProcessId"]) in owned:
				selected.append(row)
		except (KeyError, TypeError, ValueError):
			continue
	return selected


def command_has_argument(command: str, argument: str) -> bool:
	"""Match one exact Windows argv token, allowing subprocess quote wrappers."""
	pattern = r'(?i)(?:^|\s)"?' + re.escape(argument) + r'"?(?=\s|$)'
	return re.search(pattern, command) is not None


def process_identity(row: dict) -> tuple | None:
	"""Stable enough identity to recognize a previously sampled PID without trusting PID reuse."""
	try:
		created = row.get("CreationDate")
		path = pathlib.Path(str(row.get("ExecutablePath") or "")).resolve()
		pid = int(row["ProcessId"])
		parent = int(row.get("ParentProcessId") or 0)
		if not created or not path.is_file():
			return None
		return pid, parent, os.path.normcase(str(path)), str(created)
	except (KeyError, OSError, TypeError, ValueError):
		return None


def discover_owned_tree(process, engine: pathlib.Path, support: pathlib.Path, *, require_game_child: bool = True,
				direct_game_args: tuple[str, ...] | None = None,
				previously_observed: dict[int, tuple] | None = None) -> dict:
	"""Identify the launched root plus descendants; verify root identity before trusting the tree."""
	if os.name != "nt":
		return {"rows": [{"ProcessId": process.pid, "ParentProcessId": 0,
			"ExecutablePath": str(engine / "bin" / "OpenRA.exe"), "CommandLine": ""}], "child_found": True}
	rows = owned_tree_rows(process.pid, process_rows())
	root = next((r for r in rows if int(r["ProcessId"]) == process.pid), None)
	if root is None:
		if process.poll() is None:
			raise RuntimeError("launched root process is missing from the process snapshot")
		# The launcher may exit before our first poll while OpenRA.Game remains alive. Its
		# descendants are still attributable by ParentProcessId, but only trust/return them
		# when every survivor is an engine binary for this exact cell's SupportDir.
		bin_dir = (engine / "bin").resolve()
		children = [row for row in rows if int(row["ProcessId"]) != process.pid]
		for row in children:
			pid = int(row["ProcessId"])
			identity = process_identity(row)
			observed_match = (identity is not None and previously_observed is not None
				and previously_observed.get(pid) == identity)
			try:
				path = pathlib.Path(str(row.get("ExecutablePath") or "")).resolve()
				path.relative_to(bin_dir)
				same_cell = command_has_argument(str(row.get("CommandLine") or ""),
					f"Engine.SupportDir={support.absolute()}")
			except (OSError, ValueError):
				same_cell = False
			if not same_cell and not observed_match:
				raise RuntimeError(f"orphan PID {row.get('ProcessId')} does not match this cell's engine/SupportDir")
		if require_game_child and not children:
			return {"rows": [], "child_found": False}
		return {"rows": children, "child_found": bool(children)}
	expected = (engine / "bin" / "OpenRA.exe").resolve()
	command = str(root.get("CommandLine") or "").casefold()
	actual_raw = root.get("ExecutablePath")
	if actual_raw:
		actual = pathlib.Path(str(actual_raw)).resolve()
		if os.path.normcase(str(actual)) != os.path.normcase(str(expected)):
			raise RuntimeError(f"owned root executable mismatch: {actual} != {expected}")
	else:
		# CIM may omit ExecutablePath for a visible process. Popen owns this live PID,
		# but accept the root only when both its immutable launch argv and CIM command
		# line independently identify the pinned engine and exact cell arguments.
		args = process.args if isinstance(process.args, (list, tuple)) else []
		launched = os.path.normcase(str(pathlib.Path(args[0]).resolve())) if args else ""
		if process.poll() is not None or launched != os.path.normcase(str(expected)):
			raise RuntimeError("owned root ExecutablePath missing and Popen identity is not verifiable")
		if not command_has_argument(command, f"Engine.SupportDir={support.absolute()}") or not direct_game_args or not all(
			command_has_argument(command, arg.casefold()) for arg in direct_game_args):
			raise RuntimeError("owned root ExecutablePath missing and CIM command line does not pin this cell's Launch arguments")
	if not command_has_argument(command, f"Engine.SupportDir={support.absolute()}"):
		raise RuntimeError("owned root command line does not contain this cell's SupportDir")
	bin_dir = (engine / "bin").resolve()
	children = []
	for row in rows:
		if int(row["ProcessId"]) == process.pid or not row.get("ExecutablePath"):
			continue
		try:
			path = pathlib.Path(row["ExecutablePath"]).resolve()
			path.relative_to(bin_dir)
			children.append(row)
		except (OSError, ValueError):
			continue
	direct_game = bool(direct_game_args) and all(command_has_argument(command, arg.casefold()) for arg in direct_game_args)
	if require_game_child and not children and not direct_game:
		return {"rows": rows, "child_found": False}
	return {"rows": rows, "child_found": bool(children) or direct_game or not require_game_child}


def sample_owned_tree(rows: list[dict]) -> dict:
	"""Sum launcher and child memory. Missing any live-process metric is a hard sample failure."""
	if not rows:
		raise RuntimeError("owned process tree is empty")
	private_total = 0
	working_set_total = 0
	pids = []
	for row in rows:
		pid = int(row["ProcessId"])
		private, working = private_bytes(pid), rss_bytes(pid)
		if private is None or working is None:
			raise RuntimeError(f"required memory sample missing for owned PID {pid}")
		pids.append(pid)
		private_total += private
		working_set_total += working
	return {"pids": sorted(pids), "private_bytes": private_total, "working_set_bytes": working_set_total}


def terminate_owned_tree(process, engine: pathlib.Path, support: pathlib.Path) -> None:
	"""Terminate only the verified Popen-owned process tree and confirm it is gone."""
	try:
		owned = discover_owned_tree(process, engine, support, require_game_child=False)["rows"]
	except Exception:
		# CIM can fail independently of the already-held Popen handle. The launch arguments are
		# immutable and cell-specific, so taskkill remains scoped to this exact root PID tree.
		args = process.args if isinstance(process.args, (list, tuple)) else []
		expected = os.path.normcase(os.path.abspath(str(engine / "bin" / "OpenRA.exe")))
		launched_exe = os.path.normcase(os.path.abspath(str(args[0]))) if args else ""
		cell_marker = os.path.normcase(f"Engine.SupportDir={support.absolute()}")
		if os.name != "nt" or process.poll() is not None or launched_exe != expected or not any(
			cell_marker in os.path.normcase(str(arg)) for arg in args[1:]):
			raise
		p = subprocess.run(["taskkill.exe", "/PID", str(process.pid), "/T", "/F"],
			capture_output=True, text=True, timeout=20)
		if p.returncode:
			raise RuntimeError("PID-scoped fallback taskkill failed: " + p.stderr[-400:])
		process.wait(timeout=20)
		return
	if not owned:
		if process.poll() is None:
			raise RuntimeError("cannot verify owned tree before cleanup")
		return
	owned_pids = [int(row["ProcessId"]) for row in owned]
	if os.name == "nt":
		p = subprocess.run(["taskkill.exe", "/PID", str(process.pid), "/T", "/F"],
			capture_output=True, text=True, timeout=20)
		if p.returncode and process.poll() is None:
			raise RuntimeError("PID-scoped taskkill failed: " + p.stderr[-400:])
	else:
		# The non-Windows acceptance runner has no detached process group; signal only Popen-owned PIDs.
		for pid in reversed(owned_pids):
			try:
				os.kill(pid, 9)
			except ProcessLookupError:
				pass
	process.wait(timeout=20)
	if process.poll() is None:
		raise RuntimeError("Popen root still running after PID-scoped tree cleanup")
	if os.name == "nt":
		ids = ",".join(str(pid) for pid in owned_pids)
		script = f"$ids=@({ids}); Get-CimInstance Win32_Process | Where-Object {{ $ids -contains [int]$_.ProcessId }} | Select-Object -ExpandProperty ProcessId | ConvertTo-Json -Compress"
		p = subprocess.run(["powershell.exe", "-NoProfile", "-NonInteractive", "-Command", script],
			capture_output=True, text=True, timeout=15)
		if p.returncode or p.stdout.strip() not in ("", "null"):
			raise RuntimeError("owned process tree remained after PID-scoped cleanup")


def cleanup_orphaned_owned_pids(pids: set[int], engine: pathlib.Path, support: pathlib.Path,
								previously_observed: dict[int, tuple] | None = None) -> list[int]:
	"""If the launcher exited first, clean up only previously observed, revalidated game PIDs."""
	if os.name != "nt" or not pids:
		return []
	rows = process_rows()
	by_pid = {int(row["ProcessId"]): row for row in rows if str(row.get("ProcessId", "")).isdigit()}
	left = sorted(pids.intersection(by_pid))
	bin_dir = (engine / "bin").resolve()
	for pid in left:
		row = by_pid[pid]
		identity = process_identity(row)
		observed_match = (identity is not None and previously_observed is not None
			and previously_observed.get(pid) == identity)
		try:
			path = pathlib.Path(str(row.get("ExecutablePath") or "")).resolve()
			path.relative_to(bin_dir)
			same_cell = command_has_argument(str(row.get("CommandLine") or ""),
				f"Engine.SupportDir={support.absolute()}")
		except (OSError, ValueError):
			same_cell = False
		if not same_cell and not observed_match:
			raise RuntimeError(f"refusing to clean PID {pid}: it no longer matches the owned engine/support identity")
	for pid in left:
		p = subprocess.run(["taskkill.exe", "/PID", str(pid), "/T", "/F"],
			capture_output=True, text=True, timeout=20)
		if p.returncode:
			raise RuntimeError(f"PID-scoped orphan cleanup failed for {pid}: {p.stderr[-300:]}")
	if left:
		remaining = {int(row["ProcessId"]) for row in process_rows() if str(row.get("ProcessId", "")).isdigit()}
		left = sorted(set(left).intersection(remaining))
	return left


def set_monitor_cap(variant: pathlib.Path, cap_tick: int) -> None:
	"""prove_generated_map installs the campaign monitor at the 45,000 campaign cap; a preflight
	only needs the map to load, spawn and reach game over, so the generated Lua gets a short cap."""
	script = variant / "ab_campaign_monitor.lua"
	text = script.read_text(encoding="utf-8")
	sample = min(500, cap_tick // 2)
	text, n_cap = re.subn(r"(?m)^local CapTick = \d+$", f"local CapTick = {cap_tick}", text)
	text, n_sample = re.subn(r"(?m)^local SampleInterval = \d+$", f"local SampleInterval = {sample}", text)
	if n_cap != 1 or n_sample != 1:
		raise ValueError(f"campaign monitor layout changed; cannot set the preflight cap in {script}")
	script.write_text(text, encoding="utf-8")


def seat_proof(planned: list[dict], records: list[dict]) -> list[str]:
	"""Problems joining the planned seats to the engine's records ([] = every seat proven).
	Records carry `team: 0` for every seat (the harness sets alliances in the map, not lobby
	teams), so sides are proven from each record's `allies` / `opponents` spawn homes."""
	problems = []
	by_home: dict[str, list[dict]] = {}
	for record in records:
		by_home.setdefault(str(record.get("player", {}).get("home")), []).append(record)
	if len(records) != len(planned):
		problems.append(f"{len(records)} records for {len(planned)} planned seats")
	side_homes = {side: {s["home_location"] for s in planned if s["side"] == side} for side in ("A", "B")}
	for seat in planned:
		found = by_home.get(seat["home_location"], [])
		if len(found) != 1:
			problems.append(f"{seat['home']} at {seat['home_location']}: {len(found)} records")
			continue
		record = found[0]
		player = record["player"]
		if player.get("bot_type") != seat["bot_type"] or player.get("faction") != seat["faction"]:
			problems.append(f"{seat['home']}: engine seated {player.get('bot_type')}/{player.get('faction')}, "
				f"planned {seat['bot_type']}/{seat['faction']}")
		own = side_homes[seat["side"]]
		other = side_homes["B" if seat["side"] == "A" else "A"]
		allies = {str(a.get("home")) for a in record.get("allies", [])}
		opponents = {str(o.get("home")) for o in record.get("opponents", [])}
		if allies != own - {seat["home_location"]} or opponents != other:
			problems.append(f"{seat['home']}: allies {sorted(allies)} / opponents {sorted(opponents)} do not match "
				f"the planned sides")
	return problems


def pair_proof(game0: dict, game1: dict) -> list[str]:
	"""Each arm plays each spawn set once and both arms field the same factions in each game."""
	problems = []

	def homes(game: dict, arm: str) -> set:
		return {s["home_location"] for s in game["seats"] if s["arm"] == arm}

	if homes(game0, "control") != homes(game1, "treatment") or homes(game0, "treatment") != homes(game1, "control"):
		problems.append("arms do not swap spawn sets between game 0 and game 1")
	for game in (game0, game1):
		factions = {arm: sorted(s["faction"] for s in game["seats"] if s["arm"] == arm) for arm in ("control", "treatment")}
		if factions["control"] != factions["treatment"]:
			problems.append(f"game {game['game_in_pair']}: arms field different factions {factions}")
	return problems


def campaign_mod_search_paths(overlay: dict, root: pathlib.Path, engine: pathlib.Path) -> list[pathlib.Path]:
	"""Put FullMods last because InstalledMods retains the last manifest for each mod id."""
	if not isinstance(overlay, dict) or overlay.get("complete_mod_tree") is not True:
		raise ValueError("campaign acceptance requires a complete mod-tree overlay")
	mod_root = pathlib.Path(overlay.get("mod_root", "")).resolve()
	if not (mod_root / "ContentPacks" / "Outpost2" / "content.yaml").is_file():
		raise ValueError("complete campaign overlay is missing ContentPacks/Outpost2/content.yaml")
	if not (mod_root / "ai" / "ai.yaml").is_file():
		raise ValueError("complete campaign overlay is missing dual-arm ai.yaml")
	return [pathlib.Path(root).resolve() / "mods", pathlib.Path(engine).resolve() / "mods", mod_root.parent]


def materialize_acceptance_mods(root: pathlib.Path, destination: pathlib.Path, switch: str,
								accepted_switches=()) -> dict:
	"""Reproduce campaign FullMods materialization for bounded acceptance cells."""
	root = pathlib.Path(root).resolve(); destination = pathlib.Path(destination).resolve()
	arm = pilot.materialize_dual_arm_ai(root, destination, switch, accepted_switches)
	mod_root = destination / "mods" / "cameo"
	campaign.link_mod_tree(root / "mods" / "cameo", mod_root, skip_top=("ai",))
	return {**arm, "mod_root": str(mod_root), "complete_mod_tree": True}


def run_cell(job: dict, *, root: pathlib.Path, engine: pathlib.Path, overlay: dict, out: pathlib.Path,
			cap_tick: int, render: str, timeout: int) -> dict:
	cell_dir = out / re.sub(r"[^A-Za-z0-9_.-]", "_", job["cell_id"])
	support = cell_dir / "Support"
	logs = support / "Logs"
	maps = support / batch.USER_MAP_DIR  # the mod.yaml MapFolders user entry
	logs.mkdir(parents=True)
	maps.mkdir(parents=True)
	variant = maps / ("acceptance_" + re.sub(r"[^A-Za-z0-9_.-]", "_", job["cell_id"]))
	proof = pilot.prove_generated_map(job, root, variant)
	set_monitor_cap(variant, cap_tick)
	mod_paths = campaign_mod_search_paths(overlay, root, engine)
	args = [str(engine / "bin" / "OpenRA.exe"), "Game.Mod=cameo", "Sound.Device=none", "Engine.EngineDir=..",
		"Engine.ModSearchPaths=" + ",".join(str(path) for path in mod_paths),
		f"Engine.SupportDir={support}", f"Launch.Map={variant.name}", f"Launch.Benchmark={batch.BENCHMARK_PREFIX}",
		"Graphics.VSync=False", "Graphics.Mode=Windowed", "Graphics.WindowedSize=640,480"]
	if render == "headless":
		args.append(f"Game.Platform={HEADLESS_PLATFORM}")
	env = os.environ.copy()
	env["CAMEO_DEV_SEED"] = str(job["seed"])
	direct_game_args = (f"Launch.Map={variant.name}", f"Launch.Benchmark={batch.BENCHMARK_PREFIX}")
	started = time.monotonic()
	peak = 0
	peak_private = 0
	min_host_free = None
	memory_samples = []
	owned_pids = set()
	observed_owned = {}
	child_seen = False
	monitor_error = None
	lua_log = logs / "lua.log"
	debug_log = logs / "debug.log"
	deploy_offset = 0
	deployment = {"check_tick": None, "reported_seat_count": None, "expected_seat_count": None,
		"seats": {}, "duplicates": [], "missing": [], "verified": False}
	planned_homes = [seat["home_location"] for seat in proof["seats"]]
	with (cell_dir / "driver.log").open("w", encoding="utf-8") as output:
		process = subprocess.Popen(args, cwd=engine, env=env, stdout=output, stderr=subprocess.STDOUT)
		end_reason = "PROCESS_EXIT"
		while process.poll() is None:
			try:
				tree = discover_owned_tree(process, engine, support, direct_game_args=direct_game_args)
				owned_pids.update(int(row["ProcessId"]) for row in tree["rows"])
				for row in tree["rows"]:
					pid = int(row["ProcessId"])
					if pid != process.pid:
						identity = process_identity(row)
						if identity is not None:
							observed_owned[pid] = identity
				child_seen |= tree["child_found"]
				if os.name == "nt" and not child_seen and time.monotonic() - started > 30:
					end_reason = "CHILD_PROCESS_NOT_FOUND"
					raise RuntimeError("OpenRA game child was not discovered within 30 seconds")
				metrics = sample_owned_tree(tree["rows"])
				current_private = metrics["private_bytes"]
				current_rss = metrics["working_set_bytes"]
				available = available_ram_bytes()
				if available is None:
					end_reason = "MEMORY_SAMPLE_FAILED"
					raise RuntimeError("host available-RAM sample is missing")
			except Exception as error:
				if end_reason == "PROCESS_EXIT":
					end_reason = "MEMORY_SAMPLE_FAILED"
				monitor_error = str(error)
				try:
					terminate_owned_tree(process, engine, support)
				except Exception as cleanup_error:
					end_reason = "CLEANUP_FAILED"
					monitor_error += f"; cleanup failed: {cleanup_error}"
				break
			peak = max(peak, current_rss)
			peak_private = max(peak_private, current_private)
			min_host_free = available if min_host_free is None else min(min_host_free, available)
			if lua_log.is_file():
				with lua_log.open("r", encoding="utf-8", errors="replace") as stream:
					stream.seek(deploy_offset); chunk = stream.read(); deploy_offset = stream.tell()
				if chunk:
					deployment = campaign.deployment_guard.merge(deployment, campaign.deployment_guard.parse(chunk))
					if deployment["check_tick"] is not None:
						deployment["verified"] = campaign.deployment_guard.valid(deployment, planned_homes)
				world_tick = campaign.deployment_guard.latest_world_tick(debug_log)
				if not deployment["verified"] and ((world_tick is not None and world_tick >= 100)
						or (type(deployment.get("check_tick")) is int and deployment["check_tick"] >= 100)
						or time.monotonic() - started >= campaign.DEPLOYMENT_WALL_TIMEOUT):
					end_reason = "INVALID_NO_DEPLOY"
					try:
						terminate_owned_tree(process, engine, support)
					except Exception as error:
						end_reason = "CLEANUP_FAILED"; monitor_error = str(error)
					break
			memory_samples.append({"utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
				"pids": metrics["pids"], "private_bytes": current_private,
				"working_set_bytes": current_rss, "host_available_bytes": available})
			stop = run_stop_reason(current_private, available, private_cap=PRIVATE_CAP_BYTES, emergency_free=EMERGENCY_STOP_FREE_BYTES)
			if stop is not None:
				end_reason = stop
				try:
					terminate_owned_tree(process, engine, support)
				except Exception as error:
					end_reason = "CLEANUP_FAILED"
					monitor_error = str(error)
				break
			if time.monotonic() - started > timeout:
				end_reason = "WALL_TIMEOUT"
				try:
					terminate_owned_tree(process, engine, support)
				except Exception as error:
					end_reason = "CLEANUP_FAILED"
					monitor_error = str(error)
				break
			time.sleep(1)
	try:
		# Always take one last snapshot: Popen may have exited before the first loop
		# iteration, leaving a same-cell game child absent from the sampled PID set.
		final_tree = discover_owned_tree(process, engine, support, direct_game_args=direct_game_args,
			previously_observed=observed_owned)
		final_children = {int(row["ProcessId"]) for row in final_tree["rows"] if
			int(row["ProcessId"]) != process.pid}
		new_orphaned = final_children - (owned_pids - {process.pid})
		owned_pids.update(final_children)
		child_seen |= final_tree["child_found"]
		orphaned = cleanup_orphaned_owned_pids(owned_pids - {process.pid}, engine, support,
			previously_observed=observed_owned)
		if new_orphaned or orphaned:
			end_reason = "ORPHANED_GAME_CLEANED"
			monitor_error = f"launcher exited while owned game PIDs remained: {sorted(new_orphaned | set(orphaned))}"
	except Exception as error:
		end_reason = "CLEANUP_FAILED"
		monitor_error = str(error)
	exceptions = sorted(p.name for p in logs.glob("exception-*.log"))
	# Read only the final tail; markers consumed in-loop must not be counted twice.
	lua_text = lua_log.read_text(encoding="utf-8", errors="replace") if lua_log.is_file() else ""
	if lua_log.is_file():
		with lua_log.open("r", encoding="utf-8", errors="replace") as stream:
			stream.seek(deploy_offset); tail = stream.read()
		if tail:
			deployment = campaign.deployment_guard.merge(deployment, campaign.deployment_guard.parse(tail))
	deployment["verified"] = campaign.deployment_guard.valid(deployment, planned_homes)
	cap = re.search(r"AB_CAMPAIGN_CAP tick=(\d+)", lua_text)
	server_log = logs / "server.log"
	seed_pinned = server_log.is_file() and f"RandomSeed={job['seed']}" in server_log.read_text(encoding="utf-8", errors="replace")
	records = batch.read_appended_records(logs / "cameo-ai-matches.jsonl", 0)
	problems = []
	if end_reason != "PROCESS_EXIT" or process.returncode != 0:
		problems.append(f"{end_reason}, exit code {process.returncode}")
	if monitor_error:
		problems.append(monitor_error)
	if not memory_samples:
		problems.append("no complete process-tree and host-RAM samples were collected")
	if os.name == "nt" and not child_seen:
		problems.append("owned OpenRA game child was not discovered")
	if exceptions:
		problems.append(f"exception logs: {exceptions}")
	invalid_bots = sorted(set(re.findall(r"Invalid bot type: (\S+)", debug_log.read_text(errors="replace")))) if debug_log.is_file() else []
	if invalid_bots:
		problems.append(f"bots never activated (debug.log 'Invalid bot type'): {invalid_bots}")
	lua_errors = lua_text.count("Fatal Lua Error")
	if lua_errors:
		problems.append(f"{lua_errors} Fatal Lua Error(s) in lua.log")
	if not cap or int(cap.group(1)) < cap_tick:
		problems.append("no AB_CAMPAIGN_CAP marker at the preflight cap")
	if not seed_pinned:
		problems.append(f"server.log has no CAMEO DEV SEED pin for RandomSeed={job['seed']}")
	if not deployment["verified"]:
		problems.append("INVALID_NO_DEPLOY: every planned seat must deploy a verified MCV by world tick 100")
	if render == "headless":
		graphics = logs / "graphics.log"
		if HEADLESS_LOG_MARKER not in (graphics.read_text(errors="replace") if graphics.is_file() else ""):
			problems.append("engine fell back from the headless platform (see graphics.log)")
	problems += seat_proof(proof["seats"], records)
	return {"cell_id": job["cell_id"], "setup": job["setup"], "map": job["map"], "map_sha256": job["map_sha256"],
		"team_size": job["team_size"], "seed": job["seed"], "game_in_pair": job["game_in_pair"],
		"map_yaml_sha256": pilot.sha(variant / "map.yaml"),
		"overlay_ai_yaml_sha256": overlay["ai_yaml_sha256"], "complete_mod_tree": overlay["complete_mod_tree"],
		"deployment": deployment, "seats": proof["seats"],
		"engine_seats": [{**{k: r["player"].get(k) for k in ("home", "bot_type", "faction", "outcome")},
			"allies": sorted(str(a.get("home")) for a in r.get("allies", [])),
			"opponents": sorted(str(o.get("home")) for o in r.get("opponents", []))} for r in records],
		"cap_tick": int(cap.group(1)) if cap else None, "exceptions": exceptions, "exit_code": process.returncode,
		"end_reason": end_reason, "wall_seconds": round(time.monotonic() - started, 1),
		"peak_rss_bytes": peak or None, "peak_working_set_bytes": peak or None,
		"peak_private_bytes": peak_private or None, "minimum_host_available_bytes": min_host_free,
		"memory_samples": memory_samples, "owned_process_ids": sorted(owned_pids),
		"private_limit_bytes": PRIVATE_CAP_BYTES,
		"verdict": "ACCEPTED" if not problems else "REJECTED", "problems": problems}


def main(argv=None) -> int:
	parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
	parser.add_argument("--manifest", type=pathlib.Path, required=True)
	parser.add_argument("--out", type=pathlib.Path, required=True, help="new directory; never reused")
	parser.add_argument("--repo-root", type=pathlib.Path, default=ROOT)
	parser.add_argument("--cap-tick", type=int, default=1500, help="preflight cap in world ticks (1000-45000)")
	parser.add_argument("--render", choices=("headless", "fast"), default="headless")
	parser.add_argument("--setups", default="", help="comma list of setup ids (default: all)")
	parser.add_argument("--games", default="0,1", help="games in pair to run (default both = the swap proof)")
	parser.add_argument("--timeout", type=int, default=1200, help="wall seconds per cell")
	parser.add_argument("--min-free-gib", type=float, default=(BOOTSTRAP_HEADLESS_PEAK_BYTES + ADMISSION_RESERVE_BYTES) / 1024**3,
		help="RAM admission before each serial acceptance cell: bootstrapped measured-peak (10.5 GiB cap) plus 3 GiB reserve")
	args = parser.parse_args(argv)

	root = args.repo_root.resolve()
	manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
	_, engine = batch.load_config()
	engine = engine.resolve()
	if not (engine / "bin" / "OpenRA.exe").is_file():
		parser.error(f"engine executable missing under {engine}; run make.cmd all first")
	if args.render == "headless" and not (engine / "bin" / f"OpenRA.Platforms.{HEADLESS_PLATFORM}.dll").is_file():
		parser.error(f"--render headless needs engine/bin/OpenRA.Platforms.{HEADLESS_PLATFORM}.dll; run make.cmd all first")
	out = args.out.resolve()
	if out.exists():
		parser.error(f"{out} exists; preserve it and choose a new directory")
	wanted = {s for s in args.setups.split(",") if s}
	games = {int(g) for g in args.games.split(",") if g}
	first_seed = manifest["seeds"][0]
	jobs = [j for j in pilot.make_jobs(manifest)
		if j["seed"] == first_seed and j["game_in_pair"] in games and (not wanted or j["setup"] in wanted)]
	if wanted - {j["setup"] for j in jobs}:
		parser.error(f"unknown setups: {sorted(wanted - {j['setup'] for j in jobs})}")
	for job in jobs:
		if pilot.sha(root / job["map_path"]) != job["map_sha256"]:
			parser.error(f"map package hash mismatch: {job['map_path']}")

	out.mkdir(parents=True)
	overlay = materialize_acceptance_mods(root, out / "FullMods", manifest["switch"],
		manifest.get("ratchet_accepted_switches", []))
	receipt = {"tool": "map_engine_acceptance", "tool_sha256": pilot.text_sha(pathlib.Path(__file__)),
		"manifest_sha256": pilot.sha(args.manifest), "campaign_id": manifest.get("campaign_id"),
		"engine_version": pilot.read_engine_version(engine / "VERSION"), "render": args.render,
		"cap_tick": args.cap_tick, "preflight_only_never_outcome_cells": True, "cells": [], "pairs": []}
	path = out / "acceptance_receipt.json"
	for job in jobs:
		free = available_ram_bytes()
		not_admitted = admission_decision(free, args.min_free_gib * 1024**3)
		if not_admitted is not None:
			receipt["cells"].append({"cell_id": job["cell_id"], "setup": job["setup"], "map": job["map"],
				"verdict": "NOT_RUN", "problems": [not_admitted]})
		else:
			print(f"[{len(receipt['cells']) + 1}/{len(jobs)}] {job['cell_id']} ...", flush=True)
			cell = run_cell(job, root=root, engine=engine, overlay=overlay, out=out, cap_tick=args.cap_tick,
				render=args.render, timeout=args.timeout)
			receipt["cells"].append(cell)
			print(f"    {cell['verdict']} in {cell['wall_seconds']}s {'; '.join(cell['problems'])}", flush=True)
		path.write_text(json.dumps(receipt, indent=1) + "\n", encoding="utf-8")
		if receipt["cells"][-1]["verdict"] != "ACCEPTED":
			# A rejection or admission failure invalidates the environment. Preserve
			# remaining jobs as NOT_RUN and stop before launching any further cell.
			for remaining in jobs[len(receipt["cells"]):]:
				receipt["cells"].append({"cell_id": remaining["cell_id"],
					"setup": remaining["setup"], "map": remaining["map"],
					"verdict": "NOT_RUN_AFTER_PRIOR_FAILURE",
					"problems": [f"prior cell {job['cell_id']} was not accepted"]})
			path.write_text(json.dumps(receipt, indent=1) + "\n", encoding="utf-8")
			break
	by_setup: dict[str, dict[int, dict]] = {}
	for cell in receipt["cells"]:
		if cell["verdict"] == "ACCEPTED":
			by_setup.setdefault(cell["setup"], {})[cell["game_in_pair"]] = cell
	for setup, pair in by_setup.items():
		if set(pair) == {0, 1}:
			problems = pair_proof(pair[0], pair[1])
			receipt["pairs"].append({"setup": setup, "map": pair[0]["map"],
				"verdict": "ACCEPTED" if not problems else "REJECTED", "problems": problems})
	accepted_maps = sorted({p["map"] for p in receipt["pairs"] if p["verdict"] == "ACCEPTED"}
		- {c["map"] for c in receipt["cells"] if c["verdict"] != "ACCEPTED"})
	receipt["accepted_maps"] = accepted_maps
	receipt["all_selected_accepted"] = (all(c["verdict"] == "ACCEPTED" for c in receipt["cells"])
		and all(p["verdict"] == "ACCEPTED" for p in receipt["pairs"]))
	path.write_text(json.dumps(receipt, indent=1) + "\n", encoding="utf-8")
	print(f"accepted maps (both games of a pair): {accepted_maps}")
	print(f"receipt: {path}")
	return 0 if receipt["all_selected_accepted"] else 1


if __name__ == "__main__":
	sys.exit(main())
