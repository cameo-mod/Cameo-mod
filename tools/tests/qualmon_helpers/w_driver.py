"""W-case outer driver (JIT-admission only — never invoked by the D test module).

Owns ONE candidate monitor subprocess at a time (serial-by-design), synthesizes
the grant JSON from the real worktree, runs the monitored supervisor on a
bounded helper argv, captures supervisor stdout/stderr OUTSIDE the evidence
leaf, keeps an independent observer log, then emits a case receipt binding
expected vs observed monitor outcome plus all helper/driver hashes.

Retained W-case list (post-coverage-review remap):
  W1_normal_short            positive control: w_wait exits 0 -> COMPLETED/0
  W1_inner_nonzero_nonpass   negative control: w_wait exits 7 -> non-pass
  W2_normal_descendant_ledger   w_tree parent->mid->grandchild ledger coverage
  W3_early_parent_exit       w_tree --early-parent-exit lingering-child case
  W4_monitor_interruption    w_tree + kill-monitor seam (KILL_ON_JOB_CLOSE)
  W5_breakaway_attempt       w_breakaway CREATE_BREAKAWAY_FROM_JOB probe
  W6_rapid_spawn_exit        w_rapid 6-child churn (diagnostic-only)
  W7_owned_only_stop         owned tree stopped via inner-timeout seam while
                             an unowned sentinel must survive
  W8_sample_clearance_abort  real sample/clearance evidence under a
                             controlled inner-timeout abort; receipt asserts
                             sample count/gap and dual-census spacing
  D8_real_stdout_flood       w_flood 1 MiB stdout -> inner_stdout.log hashing

Bounds enforced here and inside each helper (refuse, never clamp):
  no 8-GiB-scale allocations (w_alloc hard cap 1 GiB), <=8 helper processes
  per case, <=10s intended helper work (sentinels are observers bounded at
  <=90s), <=1 MiB helper stdout (w_flood cap), unknown case names rejected.

The driver never uses taskkill /T on foreign trees and never mutates the
monitored supervisor; cleanup deletes only what it created when --cleanup is
explicitly passed.
"""

import argparse
import hashlib
import json
import pathlib
import subprocess
import sys
import time

HELPERS = pathlib.Path(__file__).parent
REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
MONITOR = REPO_ROOT / "tools" / "ai" / "run_qualification_monitored.py"
RUN_CAPPED = REPO_ROOT / "tools" / "ai" / "run_capped.py"
DRIVER_SHA = hashlib.sha256(pathlib.Path(__file__).read_bytes()).hexdigest()

SENTINEL_SECONDS = 30.0   # observer lifetime: outlives inner-timeout abort
KILL_GRACE_SECONDS = 6.0  # let monitor launch + register before kill-monitor


def sha256_file(path: pathlib.Path) -> str:
	return hashlib.sha256(path.read_bytes()).hexdigest()


def git_head(root: pathlib.Path) -> str:
	return subprocess.check_output(
		["git", "rev-parse", "HEAD"], cwd=str(root), text=True).strip()


def _h(name: str) -> str:
	return str(HELPERS / f"{name}.py")


def case_spec(case: str, work: pathlib.Path) -> dict:
	"""Per-case inner argv + seam/sentinel/expectation binding. Unknown case
	names are rejected by the caller — this table is the case registry."""
	wait = _h("w_wait")
	specs = {
		"W1_normal_short": {
			"argv": [sys.executable, wait, "--seconds", "1.5",
					 "--work-dir", str(work),
					 "--ready-file", str(work / "w1_ready.json")],
			"helpers": ["w_wait"], "seam": "none", "sentinel": None,
			"expect": "monitor COMPLETED / exit 0"},
		"W1_inner_nonzero_nonpass": {
			"argv": [sys.executable, wait, "--seconds", "1.0",
					 "--work-dir", str(work),
					 "--ready-file", str(work / "w1n_ready.json"),
					 "--exit-code", "7"],
			"helpers": ["w_wait"], "seam": "none", "sentinel": None,
			"expect": "inner nonzero -> non-pass disposition, exit != 0"},
		"W2_normal_descendant_ledger": {
			"argv": [sys.executable, _h("w_tree"), "--work-dir", str(work),
					 "--seconds", "6"],
			"helpers": ["w_tree", "w_wait"], "seam": "none", "sentinel": None,
			"expect": "ledger records parent/mid/grandchild identities"},
		"W3_early_parent_exit": {
			"argv": [sys.executable, _h("w_tree"), "--work-dir", str(work),
					 "--seconds", "6", "--early-parent-exit"],
			"helpers": ["w_tree", "w_wait"], "seam": "none", "sentinel": None,
			"expect": "lingering grandchild reconciled; non-crash outcome"},
		"W4_monitor_interruption": {
			"argv": [sys.executable, _h("w_tree"), "--work-dir", str(work),
					 "--seconds", "6"],
			"helpers": ["w_tree", "w_wait"], "seam": "kill-monitor",
			"sentinel": None,
			"expect": "monitor killed -> KILL_ON_JOB_CLOSE reaps owned tree"},
		"W5_breakaway_attempt": {
			"argv": [sys.executable, _h("w_breakaway"), "--work-dir",
					 str(work), "--seconds", "8"],
			"helpers": ["w_breakaway", "w_wait"], "seam": "none",
			"sentinel": None,
			"expect": "breakaway_result.json attempt_success=False or "
					  "escaped child contained (containment held)"},
		"W6_rapid_spawn_exit": {
			"argv": [sys.executable, _h("w_rapid"), "--work-dir", str(work),
					 "--count", "6"],
			"helpers": ["w_rapid", "w_wait"], "seam": "none", "sentinel": None,
			"expect": "diagnostic-only: rapid_result.json child_pids bound; "
					  "ledger membership observed"},
		"W7_owned_only_stop": {
			"argv": [sys.executable, wait, "--seconds", "25",
					 "--work-dir", str(work),
					 "--ready-file", str(work / "w7_ready.json")],
			"helpers": ["w_wait"], "seam": "inner-timeout", "sentinel": "wait",
			"inner_timeout": 8.0,
			"expect": "inner capped at 8s -> owned tree stopped; sentinel "
					  "sentinel_survived MUST be true"},
		"W8_sample_clearance_abort": {
			"argv": [sys.executable, wait, "--seconds", "25",
					 "--work-dir", str(work),
					 "--ready-file", str(work / "w8_ready.json")],
			"helpers": ["w_wait", "w_alloc"], "seam": "inner-timeout",
			"sentinel": "alloc", "inner_timeout": 10.0,
			"expect": "real samples + dual clearance under controlled abort; "
					  "w8_checks populated from evidence files"},
		"D8_real_stdout_flood": {
			"argv": [sys.executable, _h("w_flood"), "--mb", "1"],
			"helpers": ["w_flood"], "seam": "none", "sentinel": None,
			"expect": "1 MiB inner_stdout.log produced and hashed into "
					  "evidence_sha256"},
		}
	return specs[case]


def read_jsonl(path: pathlib.Path) -> list:
	try:
		return [json.loads(line) for line in
				path.read_text(encoding="utf-8").splitlines() if line.strip()]
	except OSError:
		return []


def w8_checks(evidence_leaf: pathlib.Path) -> dict:
	"""W8 assertions from the monitor's own evidence: real samples present,
	sample gaps inside the 10s bound, dual clearance snapshots >=5s apart."""
	checks = {}
	samples = read_jsonl(evidence_leaf / "resource_samples.jsonl")
	monos = [s.get("mono") for s in samples if isinstance(s.get("mono"), (int, float))]
	checks["sample_count"] = len(samples)
	if len(monos) >= 2:
		gaps = [b - a for a, b in zip(monos, monos[1:])]
		checks["sample_gap_max"] = round(max(gaps), 3)
		checks["sample_gap_within_bound"] = checks["sample_gap_max"] <= 10.0
	else:
		checks["sample_gap_within_bound"] = None
	clearance = read_jsonl(evidence_leaf / "clearance_censuses.jsonl")
	clear_snaps = [c for c in clearance if c.get("kind") == "clearance"]
	checks["clearance_snapshots"] = len(clear_snaps)
	checks["dual_census"] = len(clear_snaps) >= 2
	if len(clear_snaps) >= 2 and all(
			isinstance(c.get("mono"), (int, float)) for c in clear_snaps[:2]):
		checks["clearance_spacing"] = round(
			clear_snaps[1]["mono"] - clear_snaps[0]["mono"], 3)
		checks["spacing_floor_met"] = checks["clearance_spacing"] >= 5.0
	else:
		checks["spacing_floor_met"] = None
	checks["clearance_all_clear"] = all(c.get("clear") for c in clear_snaps)
	return checks


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

	# Reject unknown case names — the case table is the registry.
	try:
		spec = case_spec(args.case, pathlib.Path("placeholder"))
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

	# Sentinel: UNOWNED bystander (spawned before the monitor, never inside
	# the job). W7 uses a light sleeper; W8 uses a bounded 256 MiB commit.
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

	rc = None
	killed = False
	sentinel_survived = None
	driver_error = None
	mon = None
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
	except Exception as error:
		# Failure-safe: candidate monitor failure must not strand helper or
		# sentinel processes; record and continue to receipt writing.
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
		if sentinel_proc is not None:
			sentinel_survived = sentinel_proc.poll() is None
			observe("sentinel_verdict", survived=sentinel_survived)
			if sentinel_survived:
				try:
					sentinel_proc.terminate()
					sentinel_proc.wait(timeout=10)
				except Exception:
					pass
		observer.close()

	receipt = {"case": args.case, "jit_id": args.jit_id,
			   "expect": spec["expect"], "abort_seam": seam,
			   "inner_timeout_seconds": inner_timeout,
			   "monitor_exit_code": rc,
			   "killed_monitor": killed,
			   "sentinel_survived": sentinel_survived,
			   "driver_error": driver_error,
			   "evidence_dir": str(evidence_leaf),
			   "work_dir": str(work),
			   "driver_dir": str(driver_out),
			   "observer_log": str(observer_path),
			   "monitor_sha256": sha256_file(MONITOR),
			   "run_capped_sha256": sha256_file(RUN_CAPPED),
			   "helper_sha256": helper_hashes,
			   "source_commit": grant["source_commit"]}
	status_file = evidence_leaf / "monitor_status.json"
	if status_file.is_file():
		receipt["monitor_status"] = json.loads(status_file.read_text())
		receipt["disposition"] = receipt["monitor_status"].get("disposition")
	if args.case == "W8_sample_clearance_abort":
		receipt["w8_checks"] = w8_checks(evidence_leaf)
	if args.case == "W5_breakaway_attempt":
		result_file = work / "breakaway_result.json"
		if result_file.is_file():
			receipt["breakaway_result"] = json.loads(result_file.read_text())
	if args.case == "W6_rapid_spawn_exit":
		result_file = work / "rapid_result.json"
		if result_file.is_file():
			receipt["rapid_result"] = json.loads(result_file.read_text())
	for name in ("monitor_stdout.txt", "monitor_stderr.txt"):
		receipt[f"{name}_sha256"] = sha256_file(driver_out / name)
	(case_dir / "case_receipt.json").write_text(
		json.dumps(receipt, indent=2, default=str), encoding="utf-8")
	print(json.dumps(receipt, indent=2, default=str))
	return 0


if __name__ == "__main__":
	sys.exit(main())
