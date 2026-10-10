#!/usr/bin/env python3
"""Bounded diagnostic A5 parity probe; not a campaign outcome launcher.

Runs two serial same-seed baseline-vs-baseline 1v1 cells, samples only the
OpenRA process whose command line names that cell's support directory, and
stops at 6.25 GiB (below the campaign's 6.5 GiB ceiling). It does not create
an execution manifest or authorize campaign jobs.
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

import campaign_runner as runner

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "results" / "campaign-a5-memory-patch-20261010"
BASELINE = "964cdb630b1514e1c1a0baed55cbdbc427d5fc11"
MEMORY_FIX = "173038d7881fe45d012a05d2a37ebd7b17f85ffe"
ENGINE = "6da7fce14da541180c6baddd6925118fbef65b94"
SEED = 1337
EARLY_STOP = int(6.25 * 1024**3)
POLL = 0.5
WALL = 900


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
		str(ROOT / "mods/cameo/maps/ai_duel_gate_20260928"),
		"--time-limit", "1", "--retries", "0", "--stall-timeout", "180",
		"--keep-variants", "--render", "fast", "--support-dir", str(support)]
	env = os.environ.copy()
	env["CAMEO_DEV_SEED"] = str(SEED)
	start = time.monotonic()
	peak = 0
	samples = []
	stop_reason = None
	with stdout_path.open("w", encoding="utf-8") as stdout:
		proc = subprocess.Popen(command, cwd=ROOT, env=env, stdout=stdout, stderr=subprocess.STDOUT)
		while proc.poll() is None:
			time.sleep(POLL)
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
			for row in rows:
				pid = int(row["ProcessId"])
				try:
					private = runner.private_bytes(pid)
				except (OSError, RuntimeError):
					# A naturally exited child can disappear between the CIM query and
					# memory sample; a still-present one makes the probe fail closed.
					if any(int(x["ProcessId"]) == pid for x in process_rows(support)):
						stop_reason = "MEMORY_SAMPLE_FAILED"
						kill_cell_processes(rows, support)
						break
					continue
				peak = max(peak, private)
				samples.append({"utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
					"pid": pid, "private_bytes": private})
			if stop_reason:
				break
			if rows and any(x["private_bytes"] >= EARLY_STOP for x in samples[-len(rows):]):
				stop_reason = "MEMORY_STOP_6_25_GIB"
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
	# Check no OpenRA tied to this support remains before returning or starting the next cell.
	left = process_rows(support)
	if left:
		kill_cell_processes(left, support)
		stop_reason = stop_reason or "ORPHAN_CLEANUP"
	return {"cell": cell, "seed": SEED, "source_head": subprocess.check_output(
		["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
		"baseline_parent": BASELINE, "engine_version": (ROOT / "engine/VERSION").read_text().strip(),
		"exit_code": proc.returncode, "stop_reason": stop_reason or "PROCESS_EXIT",
		"wall_seconds": round(time.monotonic() - start, 2), "peak_private_bytes": peak,
		"stop_threshold_bytes": EARLY_STOP, "ceiling_bytes": runner.PRIVATE_LIMIT,
		"samples": samples, "support": str(support), "driver_stdout": str(stdout_path)}


def main() -> int:
	parser = argparse.ArgumentParser(description=__doc__)
	parser.add_argument("--execute-a5-diagnostic", action="store_true",
		help="explicitly run two bounded A5 qualification cells; never campaign outcomes")
	args = parser.parse_args()
	if not args.execute_a5_diagnostic:
		print(json.dumps({"mode": "NO_LAUNCH_A5_PREFLIGHT", "baseline": BASELINE,
			"memory_fix": MEMORY_FIX, "seed": SEED, "cells": 2,
			"per_process_stop_bytes": EARLY_STOP, "campaign_gate_passed": False}, indent=2))
		return 0
	if subprocess.run(["git", "merge-base", "--is-ancestor", MEMORY_FIX, "HEAD"], cwd=ROOT).returncode:
		raise RuntimeError("memory-fix commit is not an ancestor")
	if subprocess.run(["git", "status", "--porcelain", "--", "mods", "OpenRA.Mods.Cameo", "tools/ai"], cwd=ROOT, capture_output=True, text=True).stdout.strip():
		raise RuntimeError("gameplay/tooling source is dirty; commit the exact probe before qualification")
	if subprocess.run(["git", "merge-base", "--is-ancestor", BASELINE, "HEAD"], cwd=ROOT).returncode:
		raise RuntimeError("pinned baseline is not an ancestor")
	if (ROOT / "engine/VERSION").read_text().strip() != ENGINE:
		raise RuntimeError("engine pin mismatch")
	if runner.available_ram_bytes() < 6 * 1024**3:
		raise RuntimeError("free RAM below 6 GiB floor")
	if process_rows(OUT):
		raise RuntimeError("unexpected OpenRA process already names probe output")
	if OUT.exists():
		raise RuntimeError("probe output exists; preserve and choose a new directory")
	OUT.mkdir(parents=True)
	results = []
	for index in (1, 2):
		if subprocess.run(["powershell.exe", "-NoProfile", "-NonInteractive", "-Command",
			"if (Get-Process OpenRA -ErrorAction SilentlyContinue) { exit 1 } else { exit 0 }"]).returncode:
			raise RuntimeError("foreign OpenRA process exists before launch")
		result = run_cell(index)
		results.append(result)
		(OUT / f"{result['cell']}.receipt.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
		if result["stop_reason"] != "PROCESS_EXIT" or result["exit_code"] != 0:
			break
	# Same-seed, same-tree A5 diagnostic only; compare exactly one replay per run.
	replays = [list((OUT / f"run-{i:02d}").rglob("*.orarep")) for i in (1, 2)]
	if len(results) == 2 and all(len(x) == 1 for x in replays):
		diff = subprocess.run([sys.executable, str(ROOT / "tools/ai/order_stream_diff.py"),
			str(replays[0][0]), str(replays[1][0]), "--json"], cwd=ROOT, capture_output=True, text=True)
		(OUT / "order_stream_diff.json").write_text(diff.stdout + diff.stderr, encoding="utf-8")
		order_verdict = "IDENTICAL_OR_TAIL_FLUSH" if diff.returncode == 0 else "FAIL"
	else:
		order_verdict = "INCOMPLETE_NO_PAIR_OF_REPLAYS"
	processes_left = subprocess.run(["powershell.exe", "-NoProfile", "-NonInteractive", "-Command",
		"@(Get-Process OpenRA -ErrorAction SilentlyContinue).Count"], capture_output=True, text=True)
	receipt = {"purpose": "A5_MEMORY_QUALIFICATION_ONLY_NOT_CAMPAIGN_OUTCOME",
		"baseline_commit": BASELINE, "source_head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
		"engine_version": ENGINE, "seed": SEED, "pair": results,
		"order_stream_verdict": order_verdict, "system_openra_process_count": processes_left.stdout.strip(),
		"campaign_gate_passed": False}
	(OUT / "A5_memory_probe_receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
	print(json.dumps({"output": str(OUT), "pair_count": len(results),
		"order_stream_verdict": order_verdict, "campaign_gate_passed": False}, indent=2))
	return 0 if order_verdict == "IDENTICAL_OR_TAIL_FLUSH" and all(r["peak_private_bytes"] < EARLY_STOP for r in results) else 2


if __name__ == "__main__":
	try:
		raise SystemExit(main())
	except (OSError, ValueError, KeyError, TypeError, RuntimeError, subprocess.SubprocessError) as exc:
		print(f"A5 probe failed closed: {exc}", file=sys.stderr)
		raise SystemExit(2)
