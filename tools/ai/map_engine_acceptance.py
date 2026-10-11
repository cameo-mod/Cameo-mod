#!/usr/bin/env python3
"""Map engine acceptance + symmetric spawn proof for the A/B campaign maps (pilot gate).

The BU pilot manifest lists "engine acceptance plus symmetric spawn proof for all eight
maps" as an open launch gate (`ab_campaign_pilot.dry_run`: PENDING_ENGINE_PREFLIGHT).
`ab_campaign_pilot.prove_generated_map` already proves the seats STATICALLY (the generated
map.yaml names the planned bot, faction and HomeLocation for every Multi seat). This tool
closes the runtime half: it renders each planned setup's variant with the same pinned
writer, plays it in the engine to a short fixed-tick cap, and proves from the match
records that the engine actually spawned every seat where the plan says:

  * the engine loads the map and reaches game over at the cap (no exception-*.log, no Fatal Lua Error);
  * both arms' bots activate (no `Invalid bot type` in debug.log: the arms ride in the map's Rules,
    `ab_campaign_pilot.install_dual_arm_rules`);
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

HEADLESS_PLATFORM = batch.HEADLESS_PLATFORM

# Maintainer ruling 2026-10-11 04:50Z (reaffirmed in the F1 takeover): 10.5 GiB private
# cap, 3 GiB emergency host stop. No measured Windows headless peak exists yet for this
# acceptance tool, so admission bootstraps the "measured peak" to the cap itself until a
# real measurement replaces it; review REVIEW_2026-10-11_ccr_b337_luna.md F1.
PRIVATE_CAP_BYTES = int(10.5 * 1024**3)
BOOTSTRAP_HEADLESS_PEAK_BYTES = PRIVATE_CAP_BYTES
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
	try:
		for line in pathlib.Path(f"/proc/{pid}/status").read_text().splitlines():
			if line.startswith("VmRSS:"):
				return int(line.split()[1]) * 1024
	except OSError:
		return None
	return None


def private_bytes(pid: int) -> int | None:
	try:
		return campaign.private_bytes(pid)
	except (OSError, RuntimeError, ValueError):
		return None


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


def run_cell(job: dict, *, root: pathlib.Path, engine: pathlib.Path, overlay: pathlib.Path, out: pathlib.Path,
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
	dual_arm_rules = pilot.install_dual_arm_rules(variant, overlay, root)  # the arms ride in the map's Rules
	args = [str(engine / "bin" / "OpenRA.exe"), "Game.Mod=cameo", "Sound.Device=none", "Engine.EngineDir=..",
		f"Engine.ModSearchPaths={root / 'mods'},{engine / 'mods'}",
		f"Engine.SupportDir={support}", f"Launch.Map={variant.name}", f"Launch.Benchmark={batch.BENCHMARK_PREFIX}",
		"Graphics.VSync=False", "Graphics.Mode=Windowed", "Graphics.WindowedSize=640,480"]
	if render == "headless":
		args.append(f"Game.Platform={HEADLESS_PLATFORM}")
	env = os.environ.copy()
	env["CAMEO_DEV_SEED"] = str(job["seed"])
	started = time.monotonic()
	peak = 0
	peak_private = 0
	with (cell_dir / "driver.log").open("w", encoding="utf-8") as output:
		process = subprocess.Popen(args, cwd=engine, env=env, stdout=output, stderr=subprocess.STDOUT)
		end_reason = "PROCESS_EXIT"
		while process.poll() is None:
			peak = max(peak, rss_bytes(process.pid) or 0)
			current_private = private_bytes(process.pid)
			if current_private is not None:
				peak_private = max(peak_private, current_private)
			available = available_ram_bytes()
			stop = run_stop_reason(current_private, available, private_cap=PRIVATE_CAP_BYTES, emergency_free=EMERGENCY_STOP_FREE_BYTES)
			if stop is not None:
				process.kill()
				process.wait()
				end_reason = stop
				break
			if time.monotonic() - started > timeout:
				process.kill()
				process.wait()
				end_reason = "WALL_TIMEOUT"
				break
			time.sleep(1)
	exceptions = sorted(p.name for p in logs.glob("exception-*.log"))
	lua_log = logs / "lua.log"  # Lua print() output: the campaign monitor's AB_CAMPAIGN_* markers
	lua_text = lua_log.read_text(encoding="utf-8", errors="replace") if lua_log.is_file() else ""
	cap = re.search(r"AB_CAMPAIGN_CAP tick=(\d+)", lua_text)
	server_log = logs / "server.log"
	seed_pinned = server_log.is_file() and f"RandomSeed={job['seed']}" in server_log.read_text(encoding="utf-8", errors="replace")
	records = batch.read_appended_records(logs / "cameo-ai-matches.jsonl", 0)
	problems = []
	if end_reason != "PROCESS_EXIT" or process.returncode != 0:
		problems.append(f"{end_reason}, exit code {process.returncode}")
	if exceptions:
		problems.append(f"exception logs: {exceptions}")
	debug_log = logs / "debug.log"
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
	if render == "headless":
		graphics = logs / "graphics.log"
		if batch.HEADLESS_LOG_MARKER not in (graphics.read_text(errors="replace") if graphics.is_file() else ""):
			problems.append("engine fell back from the headless platform (see graphics.log)")
	problems += seat_proof(proof["seats"], records)
	return {"cell_id": job["cell_id"], "setup": job["setup"], "map": job["map"], "map_sha256": job["map_sha256"],
		"team_size": job["team_size"], "seed": job["seed"], "game_in_pair": job["game_in_pair"],
		"map_yaml_sha256": pilot.sha(variant / "map.yaml"), "dual_arm_rules_sha256": dual_arm_rules, "seats": proof["seats"],
		"engine_seats": [{**{k: r["player"].get(k) for k in ("home", "bot_type", "faction", "outcome")},
			"allies": sorted(str(a.get("home")) for a in r.get("allies", [])),
			"opponents": sorted(str(o.get("home")) for o in r.get("opponents", []))} for r in records],
		"cap_tick": int(cap.group(1)) if cap else None, "exceptions": exceptions, "exit_code": process.returncode,
		"end_reason": end_reason, "wall_seconds": round(time.monotonic() - started, 1),
		"peak_rss_bytes": peak or None, "peak_private_bytes": peak_private or None,
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
	overlay = out / "overlay"
	pilot.materialize_dual_arm_ai(root, overlay, manifest["switch"], manifest.get("ratchet_accepted_switches", []))
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
