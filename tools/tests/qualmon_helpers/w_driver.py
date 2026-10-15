"""W-case outer driver (JIT-admission only — never invoked by the D test module).

Owns ONE candidate monitor subprocess at a time (serial-by-design), synthesizes
the grant JSON from the real worktree, runs the monitored supervisor on a
bounded helper argv, captures supervisor stdout/stderr OUTSIDE the evidence
leaf, keeps an independent observer log, independently captures descendant
identities from helper marker files, verifies post-kill clearance by OS liveness
(not monitor say-so), and emits a case receipt binding expected vs observed
monitor outcome plus machine-checkable validation assertions and all
helper/driver hashes.

Retained W-case list (post-review remap, two review cycles):
  W1_normal_short              positive control: w_wait exits 0 -> COMPLETED/0
  W1_inner_nonzero_nonpass     negative control: w_wait exits 7 -> non-pass
  W2_normal_descendant_ledger  w_tree parent->mid->grandchild ledger coverage
  W3_early_parent_exit         w_tree --early-parent-exit --mid-orphans-grandchild:
                               grandchild outlives BOTH creators; must still be
                               a job member (Windows membership is not POSIX
                               reparenting) and must appear in monitor evidence
  W4_monitor_interruption      w_tree + kill-monitor seam; driver independently
                               captures descendant pids and verifies they are
                               reaped (KILL_ON_JOB_CLOSE), with recorded
                               per-step external cleanup
  W5_breakaway_attempt         w_breakaway probe; receipt binds flags/API
                               result, child identity/membership/lifetime and
                               proves no live escape at case end
  W6_rapid_spawn_exit          w_rapid 6-child churn — diagnostic-only
  W7_owned_only_stop           inner-timeout seam stops owned tree; unowned
                               sentinel MUST survive (asserted)
  W8_real_sampler_clearance    real sample/clearance evidence under controlled
                               inner-timeout abort; receipt asserts real
                               physical/private values, sample count/gaps,
                               UTC+monotonic fields, and dual clear censuses
                               >=5s apart
  D8_real_stdout_flood         w_flood 1 MiB stdout -> inner_stdout.log evidence

Bounds enforced here and inside each helper (refuse, never clamp; NaN and
infinity are refused by the finite checks): no 8-GiB-scale allocations
(w_alloc hard cap 1 GiB), <=8 helper processes per case, <=1 MiB helper stdout
(w_flood cap), unknown case names rejected, non-finite/absurd timeouts
rejected (--timeout and --inner-timeout hard-capped at the stage's 900 s).
Effective case work <=10s: W7/W8 deliberately over-provision the
inner argv (w_wait --seconds 25) precisely so the inner-timeout seam (8s/10s)
produces the controlled abort — the binding limit is the seam cap enforced by
run_capped, not the requested duration; nominal helpers stay <=10s. Sentinels
are observers bounded <=90s.

The driver never uses taskkill /T on foreign trees and never mutates the
monitored supervisor; external cleanup terminates only pids this driver's
case created and records every cleanup step, error included, without
skipping later steps.
"""

import argparse
import ctypes
from ctypes import wintypes
import hashlib
import json
import math
import os
import pathlib
import subprocess
import sys
import time

HELPERS = pathlib.Path(__file__).parent
REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
MONITOR = REPO_ROOT / "tools" / "ai" / "run_qualification_monitored.py"
RUN_CAPPED = REPO_ROOT / "tools" / "ai" / "run_capped.py"
DRIVER_SHA = hashlib.sha256(pathlib.Path(__file__).read_bytes()).hexdigest()

SENTINEL_SECONDS = 30.0    # observer lifetime: outlives inner-timeout abort
KILL_GRACE_SECONDS = 6.0   # let monitor launch + register before kill-monitor
REAP_TIMEOUT_SECONDS = 20.0  # post-kill: descendants must be gone within this
CLEANUP_TERMINATE_TIMEOUT = 10.0
# Stage ceiling: the outer monitor cap may never exceed the stage's own
# 900 s budget (matrix/skeleton admission bound) — refuse, never clamp.
MAX_TIMEOUT_SECONDS = 900.0

PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
PROCESS_TERMINATE = 0x0001
STILL_ACTIVE = 259
ERROR_INVALID_PARAMETER = 87  # OpenProcess on a nonexistent pid
EXIT_INNER_CAPPED = 5         # run_qualification_monitored exit code


def sha256_file(path: pathlib.Path) -> str:
	return hashlib.sha256(path.read_bytes()).hexdigest()


def git_head(root: pathlib.Path) -> str:
	return subprocess.check_output(
		["git", "rev-parse", "HEAD"], cwd=str(root), text=True).strip()


def _h(name: str) -> str:
	return str(HELPERS / f"{name}.py")


def pid_alive(pid: int) -> bool | None:
	"""Independent OS liveness check (not monitor-reported). False only
	when the OS proves the pid gone (OpenProcess -> ERROR_INVALID_PARAMETER)
	or GetExitCodeProcess returns a real exit code; None when the answer is
	genuinely unknown (non-Windows, access denied, query failure)."""
	if os.name != "nt":
		return None
	k32 = ctypes.windll.kernel32
	handle = k32.OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, False, pid)
	if not handle:
		# ERROR_INVALID_PARAMETER = no such process id -> proven dead.
		# Any other error (e.g. access denied) leaves liveness unknown —
		# callers must fail closed, never treat unknown as dead.
		return False if ctypes.GetLastError() == ERROR_INVALID_PARAMETER else None
	try:
		code = wintypes.DWORD()
		if not k32.GetExitCodeProcess(handle, ctypes.byref(code)):
			return None
		return code.value == STILL_ACTIVE
	finally:
		k32.CloseHandle(handle)


def process_creation_filetime(pid: int) -> int | None:
	"""Creation FILETIME of a live process — the identity half the pid
	cannot supply by itself. Cleanup revalidates this before terminating so
	a pid reused between capture and cleanup is never killed."""
	if os.name != "nt":
		return None
	k32 = ctypes.windll.kernel32
	handle = k32.OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, False, pid)
	if not handle:
		return None
	try:
		created = wintypes.FILETIME()
		exited = wintypes.FILETIME()
		kernel = wintypes.FILETIME()
		user = wintypes.FILETIME()
		if not k32.GetProcessTimes(handle, ctypes.byref(created),
								   ctypes.byref(exited), ctypes.byref(kernel),
								   ctypes.byref(user)):
			return None
		return (created.dwHighDateTime << 32) | created.dwLowDateTime
	finally:
		k32.CloseHandle(handle)


def terminate_pid(pid: int) -> str:
	"""Best-effort terminate of a driver-tracked pid; returns a recorded
	outcome string, never raises."""
	try:
		k32 = ctypes.windll.kernel32
		handle = k32.OpenProcess(PROCESS_TERMINATE, False, pid)
		if not handle:
			return "open_failed"
		try:
			if k32.TerminateProcess(handle, 1):
				return "terminated"
			return f"terminate_failed:{ctypes.GetLastError()}"
		finally:
			k32.CloseHandle(handle)
	except Exception as error:
		return f"error:{error!r}"


def descendant_scan(work: pathlib.Path) -> dict:
	"""Independent descendant identity capture: read the marker files the
	HELPERS wrote (their own declared pids) — not monitor evidence.

	Returns {"pids": {name: pid}, "creation_filetime": {name: int|None},
	"unreadable": [marker names that existed but did not parse]}.
	A present-but-unreadable marker is recorded, never silently skipped —
	an unidentifiable descendant cannot be proven contained or reaped."""
	found: dict[str, int] = {}
	created: dict[str, int | None] = {}
	unreadable: list[str] = []
	names = ["parent_marker", "mid_spawn", "grandchild_spawn",
			 "grandchild_ready", "w1_ready", "w1n_ready", "w7_ready",
			 "w8_ready", "sentinel_ready", "breakaway_child_ready",
			 "w2_ready"]
	names += [path.stem for path in work.glob("rapid_*_ready.json")]
	for name in names:
		path = work / f"{name}.json"
		if not path.is_file():
			continue
		try:
			pid = json.loads(path.read_text()).get("pid")
		except (OSError, json.JSONDecodeError):
			unreadable.append(name)
			continue
		if not isinstance(pid, int):
			unreadable.append(name)
			continue
		found[name] = pid
		created[name] = process_creation_filetime(pid)
	return {"pids": found, "creation_filetime": created,
			"unreadable": unreadable}


def descendant_pids(work: pathlib.Path) -> dict[str, int]:
	"""Pid map for receipt compatibility — see descendant_scan for the
	full identity capture."""
	return descendant_scan(work)["pids"]


def case_spec(case: str, work: pathlib.Path) -> dict:
	"""Per-case inner argv + seam/sentinel/expectation binding. Unknown case
	names are rejected by the caller — this table is the case registry.
	Case ids use the exact approved-matrix/skeleton spellings (F-V1: the
	skeleton row `W8_real_sampler_clearance` IS the driver case name — no
	alias, no drift). `matrix_row` binds each case to its matrix row and is
	emitted in every case receipt."""
	wait = _h("w_wait")
	specs = {
		"W1_normal_short": {
			"argv": [sys.executable, wait, "--seconds", "1.5",
					 "--work-dir", str(work),
					 "--ready-file", str(work / "w1_ready.json")],
			"helpers": ["w_wait"], "seam": "none", "sentinel": None,
			"matrix_row": "W1",
			"expect": "monitor COMPLETED / exit 0"},
		"W1_inner_nonzero_nonpass": {
			"argv": [sys.executable, wait, "--seconds", "1.0",
					 "--work-dir", str(work),
					 "--ready-file", str(work / "w1n_ready.json"),
					 "--exit-code", "7"],
			"helpers": ["w_wait"], "seam": "none", "sentinel": None,
			"matrix_row": "W1",
			"expect": "inner exit 7 -> INNER_FAILURE disposition, exit 7"},
		"W2_normal_descendant_ledger": {
			"argv": [sys.executable, _h("w_tree"), "--work-dir", str(work),
					 "--seconds", "6"],
			"helpers": ["w_tree", "w_wait"], "seam": "none", "sentinel": None,
			"matrix_row": "W2",
			"expect": "ledger records parent/mid/grandchild identities"},
		"W3_early_parent_exit": {
			"argv": [sys.executable, _h("w_tree"), "--work-dir", str(work),
					 "--seconds", "6", "--early-parent-exit",
					 "--mid-orphans-grandchild"],
			"helpers": ["w_tree", "w_wait"], "seam": "none", "sentinel": None,
			"matrix_row": "W3",
			"expect": "grandchild outlives both creators but remains a job "
					  "member in monitor evidence; lingering -> non-pass "
					  "stop, inner status preserved, post-stop censuses empty"},
		"W4_monitor_interruption": {
			"argv": [sys.executable, _h("w_tree"), "--work-dir", str(work),
					 "--seconds", "6"],
			"helpers": ["w_tree", "w_wait"], "seam": "kill-monitor",
			"sentinel": None,
			"matrix_row": "W4",
			"expect": "monitor killed -> KILL_ON_JOB_CLOSE reaps owned tree; "
					  "absent final status is the expected interruption "
					  "outcome; driver independently verifies all "
					  "descendant pids dead"},
		"W5_breakaway_attempt": {
			"argv": [sys.executable, _h("w_breakaway"), "--work-dir",
					 str(work), "--seconds", "8"],
			"helpers": ["w_breakaway", "w_wait"], "seam": "none",
			"sentinel": None,
			"matrix_row": "W5",
			"expect": "breakaway refused or child demonstrably contained; "
					  "unknown identity/membership fails closed; "
					  "no live escaped writer"},
		"W6_rapid_spawn_exit": {
			"argv": [sys.executable, _h("w_rapid"), "--work-dir", str(work),
					 "--count", "6"],
			"helpers": ["w_rapid", "w_wait"], "seam": "none", "sentinel": None,
			"matrix_row": "W6",
			"expect": "diagnostic-only: rapid_result.json child_pids bound; "
					  "ledger membership observed"},
		"W7_owned_only_stop": {
			"argv": [sys.executable, wait, "--seconds", "25",
					 "--work-dir", str(work),
					 "--ready-file", str(work / "w7_ready.json")],
			"helpers": ["w_wait"], "seam": "inner-timeout", "sentinel": "wait",
			"inner_timeout": 8.0,
			"matrix_row": "W7",
			"expect": "inner capped at 8s -> INNER_WALLCAP; owned member "
					  "dead before driver cleanup; sentinel_survived MUST "
					  "be true"},
		"W8_real_sampler_clearance": {
			"argv": [sys.executable, wait, "--seconds", "25",
					 "--work-dir", str(work),
					 "--ready-file", str(work / "w8_ready.json")],
			"helpers": ["w_wait", "w_alloc"], "seam": "inner-timeout",
			"sentinel": "alloc", "inner_timeout": 10.0,
			"matrix_row": "W8",
			"expect": "real samples + dual empty clearance under controlled "
					  "abort; w8_checks populated from evidence files"},
		"D8_real_stdout_flood": {
			"argv": [sys.executable, _h("w_flood"), "--mb", "1"],
			"helpers": ["w_flood"], "seam": "none", "sentinel": None,
			"matrix_row": "D8",
			"expect": "1 MiB (1048576 bytes) inner_stdout.log produced "
					  "inside evidence leaf"},
		}
	return specs[case]


def read_jsonl(path: pathlib.Path) -> list:
	try:
		return [json.loads(line) for line in
				path.read_text(encoding="utf-8").splitlines() if line.strip()]
	except OSError:
		return []


def _is_utc_z(value) -> bool:
	return isinstance(value, str) and value.endswith("Z") and "T" in value


def w8_checks(evidence_leaf: pathlib.Path) -> dict:
	"""W8 assertions from the monitor's own evidence: real physical/private
	values, sample count/gaps, UTC+monotonic fields, a final sample, member
	query status, ledger exit verification, and two empty clearance
	censuses >=5s apart."""
	checks = {}
	samples = read_jsonl(evidence_leaf / "resource_samples.jsonl")
	checks["sample_count"] = len(samples)
	# Skeleton row W8: "at least two periodic samples" while the helper is
	# alive — the pre-resume record counts as the initial sample.
	checks["sample_count_at_least_2"] = len(samples) >= 2
	monos = [s.get("mono") for s in samples if isinstance(s.get("mono"), (int, float))]
	checks["mono_fields_present"] = len(monos) == len(samples) and len(samples) > 0
	checks["utc_fields_present"] = all(_is_utc_z(s.get("utc")) for s in samples) \
		and len(samples) > 0
	host_ok = all(
		isinstance(s.get("host", {}).get("avail_phys_bytes"), int)
		and isinstance(s.get("host", {}).get("total_phys_bytes"), int)
		and s["host"]["avail_phys_bytes"] > 0
		for s in samples)
	checks["real_host_values"] = host_ok and len(samples) > 0
	member_private = [m.get("private_bytes") for s in samples
					  for m in s.get("members", [])]
	# A positive per-owned-process private-byte observation is REQUIRED —
	# an empty member list can never satisfy it.
	checks["real_private_values"] = any(
		isinstance(v, int) and v > 0 for v in member_private)
	# Every member sample must carry a clean query result and every sample
	# must carry job accounting; an error record is evidence of an
	# unqueried member, not a pass.
	checks["query_status_ok"] = bool(samples) and all(
		not m.get("error") for s in samples for m in s.get("members", [])) \
		and all(s.get("accounting") is not None for s in samples)
	checks["monotonic_mono"] = all(
		b >= a for a, b in zip(monos, monos[1:])) if monos else False
	if len(monos) >= 2:
		gaps = [b - a for a, b in zip(monos, monos[1:])]
		checks["sample_gap_max"] = round(max(gaps), 3)
		checks["sample_gap_within_bound"] = checks["sample_gap_max"] <= 10.0
	else:
		checks["sample_gap_within_bound"] = None
	checks["final_sample_present"] = bool(samples) and \
		samples[-1].get("kind") in ("sample", "pre_resume")
	clearance = read_jsonl(evidence_leaf / "clearance_censuses.jsonl")
	clear_snaps = [c for c in clearance if c.get("kind") == "clearance"]
	checks["clearance_snapshots"] = len(clear_snaps)
	checks["dual_census"] = len(clear_snaps) >= 2
	if len(clear_snaps) >= 2 and all(
			isinstance(c.get("mono"), (int, float)) for c in clear_snaps[-2:]):
		checks["clearance_spacing"] = round(
			clear_snaps[-1]["mono"] - clear_snaps[-2]["mono"], 3)
		checks["spacing_floor_met"] = checks["clearance_spacing"] >= 5.0
	else:
		checks["spacing_floor_met"] = None
	checks["clearance_all_clear"] = len(clear_snaps) >= 2 and all(
		c.get("clear") for c in clear_snaps[-2:])
	# "Two actual empty censuses" — the certified pair: clear flag AND
	# zero observed members AND settled accounting on the last two snaps.
	checks["censuses_empty"] = len(clear_snaps) >= 2 and all(
		not c.get("job_membership_ids") and c.get("active_processes") == 0
		for c in clear_snaps[-2:])
	status_path = evidence_leaf / "monitor_status.json"
	ledger = []
	if status_path.is_file():
		try:
			ledger = json.loads(status_path.read_text()).get("ledger") or []
		except (OSError, json.JSONDecodeError):
			ledger = []
	checks["all_ledger_exits_verified"] = bool(ledger) and all(
		e.get("exited") and e.get("exit_verified") for e in ledger)
	return checks


def _a(name: str, result: str, detail="") -> dict:
	return {"assertion": name, "result": result, "detail": detail}


def evaluate_case(case: str, ctx: dict) -> list:
	"""Machine-checkable per-case assertions. result is pass/fail/diagnostic/
	non-pass; a missing monitor status or missing evidence is ALWAYS
	non-pass, never silently absent — except W4, where the deliberately
	killed monitor's absent status is itself the asserted outcome."""
	a = []
	status = ctx.get("monitor_status")
	disposition = ctx.get("disposition")
	rc = ctx.get("monitor_exit_code")
	inner = ctx.get("inner") or {}
	ledger = ctx.get("ledger") or []
	ledger_pids = {e.get("pid") for e in ledger}
	clearance = ctx.get("clearance") or []
	clear_snaps = [c for c in clearance if c.get("kind") == "clearance"]
	last_snap = clear_snaps[-1] if clear_snaps else {}
	samples = ctx.get("samples") or []
	sample_kinds = [s.get("kind") for s in samples]

	if case == "W4_monitor_interruption":
		# The monitor is intentionally killed: no final status is written,
		# so the required outcome is absence + independent post-kill proof —
		# asserted explicitly, never filed under the generic status row.
		a.append(_a("monitor_status_absent_expected",
					"pass" if status is None else "fail",
					"monitor_status.json must be absent after the kill "
					f"(present={status is not None})"))
		a.append(_a("expected_interruption",
					"pass" if (ctx.get("killed_monitor") and status is None
							   and ctx.get("post_kill_clearance")) else "fail",
					f"killed={ctx.get('killed_monitor')} "
					f"post_kill_clearance={ctx.get('post_kill_clearance')}"))
		scan = ctx.get("pre_kill_scan") or {}
		expected_markers = {"parent_marker", "mid_spawn", "grandchild_spawn"}
		found = set(scan.get("pids", {}))
		a.append(_a("pre_kill_markers_complete",
					"pass" if expected_markers <= found
						   and not scan.get("unreadable") else "fail",
					f"missing={sorted(expected_markers - found)} "
					f"unreadable={scan.get('unreadable')}"))
		a.append(_a("post_kill_clearance",
					"pass" if ctx.get("post_kill_clearance") else "fail",
					json.dumps(ctx.get("post_kill_detail", {}))))
		return a

	a.append(_a("monitor_status_present",
				"pass" if isinstance(status, dict) else "non-pass",
				"missing status/evidence is non-pass by rule"))
	a.append(_a("evidence_files_present",
				"pass" if ctx.get("evidence_files") else "non-pass",
				json.dumps(ctx.get("evidence_files", {}))[:400]))

	if case == "W1_normal_short":
		a.append(_a("completed_exit0",
					"pass" if disposition == "COMPLETED" and rc == 0
					else "fail", f"disposition={disposition} rc={rc}"))
		a.append(_a("inner_completed",
					"pass" if inner.get("status") == "COMPLETED"
							  and inner.get("exit_code") == 0 else "fail",
					f"inner={inner}"))
		# Initial / active / final sample coverage, even for a short run.
		a.append(_a("initial_sample",
					"pass" if "pre_resume" in sample_kinds else "fail",
					f"sample_kinds={sample_kinds}"))
		a.append(_a("active_samples",
					"pass" if sample_kinds.count("sample") >= 1 else "fail",
					f"sample_kinds={sample_kinds}"))
		a.append(_a("final_sample",
					"pass" if samples and samples[-1].get("kind") in
							  ("sample", "pre_resume") else "fail",
					f"last={samples[-1].get('kind') if samples else None}"))
		roots = [e for e in ledger if e.get("is_root")]
		a.append(_a("root_identity",
					"pass" if len(roots) == 1
							  and isinstance(roots[0].get("pid"), int)
							  and isinstance(roots[0].get("creation_filetime"),
											 int) else "fail",
					json.dumps(roots)[:300]))
		a.append(_a("accounting_settled",
					"pass" if last_snap.get("active_processes") == 0
							  and (last_snap.get("total_processes") or 0) >= 1
					else "fail", f"last_snap={last_snap}"))
		a.append(_a("clearance_complete",
					"pass" if len(clear_snaps) >= 2
							  and all(c.get("clear") for c in clear_snaps[-2:])
							  and all(not c.get("job_membership_ids")
									  for c in clear_snaps[-2:])
					else "fail", f"clear_snaps={len(clear_snaps)}"))
		evidence_sha = (status or {}).get("evidence_sha256") or {}
		a.append(_a("output_hashes_bound",
					"pass" if {"resource_samples.jsonl",
							   "clearance_censuses.jsonl",
							   "monitor_log.txt"} <= set(evidence_sha)
					else "fail",
					f"hashed={sorted(evidence_sha)}"))
	elif case == "W1_inner_nonzero_nonpass":
		a.append(_a("inner_exit_7",
					"pass" if inner.get("exit_code") == 7 else "fail",
					f"inner={inner} — actual inner code must be the "
					"intended nonzero value"))
		a.append(_a("monitor_nonpass_exit_7",
					"pass" if disposition == "INNER_FAILURE" and rc == 7
					else "fail",
					f"disposition={disposition} rc={rc} — INNER_FAILURE "
					"propagates the inner code"))
		a.append(_a("owned_cleanup_settled",
					"pass" if last_snap.get("active_processes") == 0
							  and not last_snap.get("job_membership_ids")
					else "fail", f"last_snap={last_snap}"))
	elif case == "W2_normal_descendant_ledger":
		descendants = ctx.get("descendants") or {}
		marked = set(descendants.values())
		a.append(_a("descendants_in_ledger",
					"pass" if marked and marked <= ledger_pids else "fail",
					f"marked={sorted(marked)} ledger={sorted(ledger_pids)}"))
		a.append(_a("three_generations",
					"pass" if len(descendants) >= 3 else "fail",
					json.dumps(descendants)))
		# (PID, FILETIME) identity completeness — a pid without a creation
		# filetime is not an identity.
		a.append(_a("pid_filetime_identities",
					"pass" if ledger and all(
						isinstance(e.get("pid"), int)
						and isinstance(e.get("creation_filetime"), int)
						for e in ledger) else "fail",
					f"keys={[e.get('ledger_key') for e in ledger]}"))
		# Parent-link reconciliation: the grandchild's parent record must
		# resolve to the ledgered mid process.
		by_pid = {e.get("pid"): e for e in ledger}
		grand = by_pid.get(descendants.get("grandchild_spawn")) or \
			by_pid.get(descendants.get("grandchild_ready"))
		mid = by_pid.get(descendants.get("mid_spawn"))
		parent_ok = (grand is not None and mid is not None
					 and (grand.get("parent") or {}).get("pid")
					 == mid.get("pid")
					 and (grand.get("parent") or {}).get("link") == "ledger")
		a.append(_a("parent_links_reconciled",
					"pass" if parent_ok else "fail",
					f"grand_parent={ (grand or {}).get('parent') } "
					f"mid_pid={(mid or {}).get('pid')}"))
		exits = [(e.get("pid"), e.get("exited"), e.get("exit_verified"))
				 for e in ledger]
		a.append(_a("handle_verified_exits",
					"pass" if ledger and all(
						e.get("exited") and e.get("exit_verified")
						for e in ledger) else "fail",
					f"exits={exits}"))
		a.append(_a("accounting_reconciled",
					"pass" if (last_snap.get("total_processes") or 0)
							  >= len(ledger)
							  and last_snap.get("active_processes") == 0
					else "fail",
					f"total={last_snap.get('total_processes')} "
					f"ledger_size={len(ledger)}"))
	elif case == "W3_early_parent_exit":
		grand = ctx.get("descendants", {}).get("grandchild_ready") or \
			ctx.get("descendants", {}).get("grandchild_spawn")
		a.append(_a("orphaned_grandchild_in_ledger",
					"pass" if grand in ledger_pids else "fail",
					f"grandchild_pid={grand} ledger={sorted(ledger_pids)} — "
					"job membership is creation-time, not reparented"))
		creators = [ctx["descendants"].get(k) for k in
					("parent_marker", "mid_spawn")]
		creators = [p for p in creators if p]
		a.append(_a("creators_exited",
					"pass" if creators and all(
						pid_alive(p) is False for p in creators) else "fail",
					f"creator pids={creators}"))
		# The lingering live grandchild must force the non-pass owned stop;
		# the inner root itself completed normally.
		a.append(_a("monitor_nonpass",
					"pass" if disposition not in (None, "COMPLETED")
							  and rc not in (None, 0) else "fail",
					f"disposition={disposition} rc={rc}"))
		a.append(_a("lingering_stop_reason",
					"pass" if ctx.get("stop_reason") and (
						"live members" in ctx["stop_reason"]
						or "lingering" in ctx["stop_reason"]) else "fail",
					f"stop_reason={ctx.get('stop_reason')}"))
		a.append(_a("inner_completed_preserved",
					"pass" if inner.get("status") == "COMPLETED"
							  and inner.get("exit_code") == 0 else "fail",
					f"inner={inner}"))
		# Owned stop terminated the job: the two closing censuses must
		# show the grandchild gone.
		membership = [c.get("job_membership_ids") for c in clear_snaps[-2:]]
		a.append(_a("post_stop_censuses_empty",
					"pass" if len(clear_snaps) >= 2 and all(
						not ids for ids in membership) else "fail",
					f"membership={membership}"))
	elif case == "W5_breakaway_attempt":
		br = ctx.get("breakaway_result") or {}
		a.append(_a("attempt_evidence_recorded",
					"pass" if "creationflags_hex" in br
							  and "attempt_success" in br else "fail",
					json.dumps(br)[:300]))
		child = br.get("child_pid")
		if br.get("attempt_success"):
			# Successful spawn ⇒ full child identity is REQUIRED; any
			# unknown field fails closed (identity unknown => HOLD-grade).
			identity_ok = (
				isinstance(child, int) and child > 0
				and isinstance(br.get("child_creation_filetime"), int)
				and br.get("child_in_any_job_at_spawn") is not None
				and isinstance(br.get("child_exit_code"), int)
				and isinstance(br.get("child_lifetime_seconds"),
							   (int, float))
				and isinstance(br.get("child_alive_at_helper_exit"), bool))
			a.append(_a("child_identity_complete",
						"pass" if identity_ok else "fail",
						f"pid={child} "
						f"filetime={br.get('child_creation_filetime')} "
						f"in_job_spawn={br.get('child_in_any_job_at_spawn')} "
						f"exit={br.get('child_exit_code')} "
						f"alive={br.get('child_alive_at_helper_exit')}"))
			in_ledger = child in ledger_pids
			escaped = br.get("child_in_any_job_at_spawn") is False
			alive_now = pid_alive(child)
			a.append(_a("containment_verdict",
						"pass" if in_ledger else "non-pass",
						f"child_in_candidate_ledger={in_ledger} "
						f"in_any_job_at_spawn="
						f"{br.get('child_in_any_job_at_spawn')} — an "
						"out-of-ledger child escaped the candidate job"))
			a.append(_a("no_live_escape",
						"pass" if not (escaped and alive_now is not False)
						else "fail",
						f"escaped={escaped} alive={alive_now} — unknown "
						"liveness cannot prove 'no live escape'"))
		else:
			a.append(_a("breakaway_refused_or_failed",
						"pass" if "win32_error" in br or br.get("reason")
						else "fail",
						f"refusal evidence: {json.dumps(br)[:200]}"))
	elif case == "W6_rapid_spawn_exit":
		rr = ctx.get("rapid_result") or {}
		a.append(_a("diagnostic_only", "diagnostic",
					f"child_pids={rr.get('child_pids')}"))
	elif case == "W7_owned_only_stop":
		a.append(_a("controlled_inner_capped",
					"pass" if disposition == "INNER_WALLCAP"
							  and rc == EXIT_INNER_CAPPED else "fail",
					f"disposition={disposition} rc={rc} — the "
					"inner-timeout seam must produce INNER_WALLCAP/5"))
		# Owned members must be dead BEFORE any driver-side cleanup —
		# post-hoc driver kills must not mask a failed monitor stop.
		pre = ctx.get("pre_cleanup_alive") or {}
		owned = {n: v for n, v in pre.items() if n != "sentinel_ready"}
		a.append(_a("owned_member_stopped_before_cleanup",
					"pass" if owned and all(v is False
											for v in owned.values())
					else "fail",
					f"pre_cleanup_alive={pre}"))
		a.append(_a("sentinel_survived",
					"pass" if ctx.get("sentinel_survived") is True else "fail",
					f"sentinel_survived={ctx.get('sentinel_survived')}"))
		sentinel_pid = (ctx.get("descendants") or {}).get("sentinel_ready")
		a.append(_a("sentinel_outside_job",
					"pass" if isinstance(sentinel_pid, int)
							  and sentinel_pid not in ledger_pids
					else "fail",
					f"sentinel_pid={sentinel_pid} "
					f"ledger={sorted(ledger_pids)}"))
	elif case == "W8_real_sampler_clearance":
		c = ctx.get("w8_checks") or {}
		for key in ("sample_count_at_least_2", "mono_fields_present",
					"utc_fields_present", "real_host_values",
					"real_private_values", "query_status_ok",
					"monotonic_mono", "sample_gap_within_bound",
					"final_sample_present", "dual_census",
					"spacing_floor_met", "clearance_all_clear",
					"censuses_empty", "all_ledger_exits_verified"):
			a.append(_a(f"w8_{key}",
						"pass" if c.get(key) is True else "fail",
						json.dumps({key: c.get(key)})))
		a.append(_a("controlled_abort",
					"pass" if disposition == "INNER_WALLCAP"
							  and rc == EXIT_INNER_CAPPED else "fail",
					f"disposition={disposition} rc={rc} — inner-timeout "
					"seam must produce INNER_WALLCAP/5"))
	elif case == "D8_real_stdout_flood":
		size = ctx.get("inner_stdout_bytes")
		a.append(_a("inner_stdout_bounded_flood",
					"pass" if isinstance(size, int) and size >= 1048576
					else "fail",
					f"inner_stdout_bytes={size} — contract is exactly "
					"1 MiB (1048576 bytes), not ~1,000,000"))
		a.append(_a("inner_stdout_evidence_hashed",
					"pass" if ctx.get("inner_stdout_sha256") else "fail",
					f"sha256={ctx.get('inner_stdout_sha256')}"))
	return a


def main() -> int:
	parser = argparse.ArgumentParser()
	parser.add_argument("--case", required=True)
	parser.add_argument("--jit-id", required=True)
	parser.add_argument("--stage", type=int, default=1)
	parser.add_argument("--root", required=True)
	parser.add_argument("--evidence-root", required=True)
	parser.add_argument("--timeout", type=float, default=900.0,
						help="monitor wallclock bound")
	parser.add_argument("--inner-timeout", type=float, default=None,
						help="run_capped cap; defaults to --timeout. The "
							 "inner-timeout seam sets this far below the "
							 "helper duration for a deterministic owned stop.")
	parser.add_argument("--abort-seam", choices=["none", "kill-monitor",
												 "inner-timeout"],
						default=None, help="override the case-defined seam")
	parser.add_argument("--cleanup", action="store_true")
	args = parser.parse_args()

	# Bounded args enforced — refuse non-finite or absurd values (NaN parses
	# as float and would silently disable timeouts; infinity likewise).
	for name in ("timeout", "inner_timeout"):
		value = getattr(args, name)
		if value is not None and (not math.isfinite(value) or value <= 0
								  or value > MAX_TIMEOUT_SECONDS):
			parser.error(f"--{name.replace('_','-')} must be finite, "
						 f"0 < v <= {MAX_TIMEOUT_SECONDS}")
	if not isinstance(args.stage, int) or not 1 <= args.stage <= 4:
		parser.error("--stage must be 1-4")

	# Reject unknown case names — the case table is the registry.
	try:
		case_spec(args.case, pathlib.Path("placeholder"))
	except KeyError:
		parser.error(f"unknown --case {args.case!r}")

	root = pathlib.Path(args.root).resolve()
	case_dir = (pathlib.Path(args.evidence_root) /
				f"{args.case}_{int(time.time())}").resolve()
	work = case_dir / "helper_work"
	work.mkdir(parents=True)
	driver_out = case_dir / "driver"  # outside the monitor evidence leaf
	driver_out.mkdir()
	evidence_leaf = case_dir / "evidence"  # created by the monitor itself

	spec = case_spec(args.case, work)
	inner_timeout = args.inner_timeout or spec.get("inner_timeout") or args.timeout
	seam = args.abort_seam or spec["seam"]

	# Bind every helper this case uses (plus the driver itself) into the
	# grant's tool_sha256 so the monitor hashes them pre/post run; the same
	# map is emitted in the case receipt for hash binding.
	helper_hashes = {
		f"tools/tests/qualmon_helpers/{name}.py": sha256_file(_h(name))
		for name in spec["helpers"]}
	helper_hashes["tools/tests/qualmon_helpers/w_driver.py"] = DRIVER_SHA

	inner_status = evidence_leaf / "inner_run_capped_status.json"
	child_argv = [sys.executable, "tools/ai/run_capped.py",
				  "--status-file", str(inner_status),
				  "--timeout", str(inner_timeout), "--"] + spec["argv"]
	grant = {
		"jit_id": args.jit_id,
		"stage": args.stage,
		"argv": child_argv,
		"cwd": str(root),
		"tool_sha256": {
			"tools/ai/run_capped.py": sha256_file(RUN_CAPPED),
			"tools/ai/run_qualification_monitored.py": sha256_file(MONITOR),
			**helper_hashes,
		},
		"source_commit": git_head(root),
		"termination_timeout_seconds": 30,
		"clearance_timeout_seconds": 60,
	}
	grant_path = case_dir / "grant.json"
	grant_path.write_text(json.dumps(grant, indent=2), encoding="utf-8")

	monitor_argv = [sys.executable, str(MONITOR),
					"--jit-id", args.jit_id, "--stage", str(args.stage),
					"--grant-input", str(grant_path),
					"--evidence-dir", str(evidence_leaf),
					"--timeout", str(args.timeout), "--"] + child_argv

	sentinel_proc = None
	sentinel_argv = None
	if spec["sentinel"] == "alloc":
		sentinel_argv = [sys.executable, _h("w_alloc"),
						 "--commit-mb", "256", "--seconds",
						 str(SENTINEL_SECONDS), "--work-dir", str(work),
						 "--ready-file", str(work / "sentinel_ready.json")]
	elif spec["sentinel"] == "wait":
		sentinel_argv = [sys.executable, _h("w_wait"),
						 "--seconds", str(SENTINEL_SECONDS),
						 "--work-dir", str(work),
						 "--ready-file", str(work / "sentinel_ready.json")]

	observer_path = driver_out / "driver_observer.jsonl"
	observer = observer_path.open("w", encoding="utf-8", newline="\n")
	t0 = time.monotonic()

	rc = None
	killed = False
	sentinel_survived = None
	driver_error = None
	mon = None
	cleanup_log = []
	pre_kill_scan = {"pids": {}, "creation_filetime": {}, "unreadable": []}
	post_run_scan = {"pids": {}, "creation_filetime": {}, "unreadable": []}
	pre_cleanup_alive = {}
	post_kill_detail = {}

	def observe(event: str, **fields) -> None:
		observer.write(json.dumps(
			{"utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
			 "mono": round(time.monotonic() - t0, 3),
			 "event": event,
			 "monitor_rc": mon.poll() if mon is not None else None,
			 "sentinel_alive": (sentinel_proc.poll() is None
								if sentinel_proc is not None else None),
			 **fields}) + "\n")
		observer.flush()

	out_f = open(driver_out / "monitor_stdout.txt", "wb")
	err_f = open(driver_out / "monitor_stderr.txt", "wb")
	try:
		if sentinel_argv is not None:
			sentinel_proc = subprocess.Popen(sentinel_argv, cwd=str(work))
			observe("sentinel_spawned", argv=sentinel_argv)

		mon = subprocess.Popen(monitor_argv, cwd=str(root),
							   stdout=out_f, stderr=err_f)
		observe("monitor_spawned", argv=monitor_argv)

		if seam == "kill-monitor":
			observe("kill_wait", seconds=KILL_GRACE_SECONDS)
			time.sleep(KILL_GRACE_SECONDS)
			# Independent descendant capture BEFORE the kill (W4) —
			# identities include creation FILETIME for later revalidation.
			pre_kill_scan = descendant_scan(work)
			observe("pre_kill_descendants", pids=pre_kill_scan["pids"],
					creation_filetime=pre_kill_scan["creation_filetime"],
					unreadable=pre_kill_scan["unreadable"],
					alive={k: pid_alive(p)
						   for k, p in pre_kill_scan["pids"].items()})
			mon.terminate()  # W4: outer harness kills the supervisor itself
			killed = True
			observe("monitor_terminated")

		outer_deadline = t0 + args.timeout * 1.5 + 120
		while time.monotonic() < outer_deadline:
			rc = mon.poll()
			if rc is not None:
				break
			time.sleep(0.5)
		observe("monitor_exited" if rc is not None else "monitor_deadline")
		if rc is None:
			mon.kill()
			rc = mon.wait()

		if killed and pre_kill_scan["pids"]:
			# Post-kill clearance: every captured descendant must be dead.
			reap_deadline = time.monotonic() + REAP_TIMEOUT_SECONDS
			while time.monotonic() < reap_deadline:
				post_kill_detail = {
					k: pid_alive(p) for k, p in pre_kill_scan["pids"].items()}
				if all(v is False for v in post_kill_detail.values()):
					break
				time.sleep(0.5)
			post_kill_detail = {k: pid_alive(p)
								for k, p in pre_kill_scan["pids"].items()}
			observe("post_kill_clearance", alive=post_kill_detail)
	except Exception as error:
		driver_error = repr(error)
		observe("driver_error", error=driver_error)
		try:
			if mon is not None and mon.poll() is None:
				mon.kill()
			if mon is not None:
				rc = mon.wait()
		except Exception as cleanup_error:
			driver_error += f"; cleanup: {cleanup_error!r}"
	finally:
		out_f.close()
		err_f.close()
		# Fresh descendant capture + liveness BEFORE any driver-side
		# terminate — this snapshot is the unmasked "did the monitor stop
		# its own tree" evidence (driver cleanup must never supply it).
		post_run_scan = descendant_scan(work)
		pre_cleanup_alive = {name: pid_alive(pid)
							 for name, pid in post_run_scan["pids"].items()}
		observe("pre_cleanup_liveness", alive=pre_cleanup_alive,
				unreadable=post_run_scan["unreadable"])
		if sentinel_proc is not None:
			sentinel_survived = sentinel_proc.poll() is None
			observe("sentinel_verdict", survived=sentinel_survived)
			if sentinel_survived:
				try:
					sentinel_proc.terminate()
					sentinel_proc.wait(timeout=CLEANUP_TERMINATE_TIMEOUT)
					cleanup_log.append({"target": "sentinel",
										"result": "terminated"})
				except Exception as error:
					cleanup_log.append({"target": "sentinel",
										"result": f"error:{error!r}"})
		# External cleanup of every driver-known descendant — every outcome
		# recorded (including dead/unknown/skipped), a failure never skips a
		# later step, and a pid is terminated ONLY after its creation
		# FILETIME revalidates (a reused pid is never killed).
		for name, pid in post_run_scan["pids"].items():
			if name == "sentinel_ready":
				continue  # sentinel handled above
			alive = pre_cleanup_alive.get(name)
			expected_creation = post_run_scan["creation_filetime"].get(name)
			if alive is False:
				outcome = "already_dead"
			elif alive is None:
				outcome = "liveness_unknown_skipped"
			elif expected_creation is None:
				outcome = "creation_unverified_skipped"
			elif process_creation_filetime(pid) != expected_creation:
				outcome = "skipped_creation_mismatch"
			else:
				outcome = terminate_pid(pid)
			cleanup_log.append({"target": name, "pid": pid,
								"creation_filetime": expected_creation,
								"result": outcome})
			observe("external_cleanup", name=name, pid=pid, result=outcome)
		observer.close()

	# ---- receipt ---------------------------------------------------------------

	evidence_files = {}
	if evidence_leaf.is_dir():
		for path in sorted(evidence_leaf.iterdir()):
			if path.is_file():
				evidence_files[path.name] = {
					"size": path.stat().st_size,
					"sha256": sha256_file(path)}

	ctx = {"monitor_exit_code": rc,
		   "killed_monitor": killed,
		   "descendants": post_run_scan["pids"],
		   "marker_scan": post_run_scan,
		   "pre_kill_scan": pre_kill_scan,
		   "pre_cleanup_alive": pre_cleanup_alive,
		   "post_kill_clearance": (post_kill_detail and all(
			   v is False for v in post_kill_detail.values())),
		   "post_kill_detail": post_kill_detail,
		   "sentinel_survived": sentinel_survived,
		   "evidence_files": evidence_files}
	status_file = evidence_leaf / "monitor_status.json"
	monitor_status = None
	if status_file.is_file():
		try:
			monitor_status = json.loads(status_file.read_text())
		except (OSError, json.JSONDecodeError):
			monitor_status = None
	ctx["monitor_status"] = monitor_status
	ctx["disposition"] = (monitor_status or {}).get("disposition")
	ctx["stop_reason"] = (monitor_status or {}).get("stop_reason")
	ctx["inner"] = (monitor_status or {}).get("inner") or {}
	ctx["ledger"] = (monitor_status or {}).get("ledger") or []
	ctx["clearance"] = read_jsonl(evidence_leaf / "clearance_censuses.jsonl")
	ctx["samples"] = read_jsonl(evidence_leaf / "resource_samples.jsonl")
	inner_stdout = evidence_leaf / "inner_stdout.log"
	ctx["inner_stdout_bytes"] = (inner_stdout.stat().st_size
								 if inner_stdout.is_file() else None)
	ctx["inner_stdout_sha256"] = (sha256_file(inner_stdout)
								  if inner_stdout.is_file() else None)
	if args.case == "W8_real_sampler_clearance":
		ctx["w8_checks"] = w8_checks(evidence_leaf)
	for result_name, result_file in (
			("breakaway_result", work / "breakaway_result.json"),
			("rapid_result", work / "rapid_result.json")):
		if result_file.is_file():
			try:
				ctx[result_name] = json.loads(result_file.read_text())
			except (OSError, json.JSONDecodeError):
				ctx[result_name] = None

	assertions = evaluate_case(args.case, ctx)
	receipt = {"case": args.case, "jit_id": args.jit_id,
			   "matrix_row": spec["matrix_row"],
			   "expect": spec["expect"], "abort_seam": seam,
			   "inner_timeout_seconds": inner_timeout,
			   "monitor_exit_code": rc,
			   "killed_monitor": killed,
			   "sentinel_survived": sentinel_survived,
			   "driver_error": driver_error,
			   "cleanup_log": cleanup_log,
			   "descendants_observed": ctx["descendants"],
			   "pre_kill_scan": pre_kill_scan,
			   "pre_cleanup_alive": pre_cleanup_alive,
			   "post_kill_detail": post_kill_detail,
			   "evidence_dir": str(evidence_leaf),
			   "work_dir": str(work),
			   "driver_dir": str(driver_out),
			   "observer_log": str(observer_path),
			   "monitor_sha256": sha256_file(MONITOR),
			   "run_capped_sha256": sha256_file(RUN_CAPPED),
			   "helper_sha256": helper_hashes,
			   "source_commit": grant["source_commit"],
			   "observed_monitor_outcome": {
				   "monitor_exit_code": rc,
				   "disposition": ctx["disposition"],
				   "monitor_status_present": monitor_status is not None,
				   "evidence_files": evidence_files},
			   "validation_assertion_outcome": assertions}
	if monitor_status is not None:
		receipt["monitor_status"] = monitor_status
		receipt["disposition"] = ctx["disposition"]
	if ctx.get("w8_checks") is not None:
		receipt["w8_checks"] = ctx["w8_checks"]
	if ctx.get("breakaway_result") is not None:
		receipt["breakaway_result"] = ctx["breakaway_result"]
	if ctx.get("rapid_result") is not None:
		receipt["rapid_result"] = ctx["rapid_result"]
	for name in ("monitor_stdout.txt", "monitor_stderr.txt"):
		receipt[f"{name}_sha256"] = sha256_file(driver_out / name)
	(case_dir / "case_receipt.json").write_text(
		json.dumps(receipt, indent=2, default=str), encoding="utf-8")
	print(json.dumps(receipt, indent=2, default=str))
	# Assertions are gates, not diagnostics: any fail/non-pass row or a
	# driver-level error makes the case non-zero. 'diagnostic' rows (W6)
	# never fail a case on their own.
	failed = [a for a in assertions if a["result"] in ("fail", "non-pass")]
	return 1 if (failed or driver_error is not None) else 0


if __name__ == "__main__":
	sys.exit(main())
