"""W-case outer driver (JIT-admission only — never invoked by the D test module).

Owns ONE candidate monitor subprocess at a time (serial-by-design), synthesizes
the grant JSON from the real worktree, runs the monitored supervisor on a
bounded helper argv, captures supervisor stdout/stderr OUTSIDE the evidence
leaf, then emits a case receipt binding expected vs observed monitor outcome.

Options:
  --case NAME        case id (W1_normal_short, W1_inner_nonzero_nonpass, ...)
  --jit-id ID        JIT identifier for the grant/receipt
  --stage N          stage number
  --root DIR         repo/worktree root the monitor runs against
  --evidence-root    parent dir; a fresh leaf per case is created inside
  --timeout SECONDS  inner run_capped timeout (default 900)
  --kill-monitor     W4: terminate the monitor subprocess mid-run, then wait
                     for KILL_ON_JOB_CLOSE to reap the tree
  --sentinel         W7/W8: spawn an UNOWNED helper first; receipt records
                     whether it survived (must never be terminated)

The driver never uses taskkill /T on foreign trees and never mutates the
monitored supervisor; cleanup deletes only what it created when --cleanup is
explicitly passed.
"""

import argparse
import hashlib
import json
import os
import pathlib
import subprocess
import sys
import time

HELPERS = pathlib.Path(__file__).parent
MONITOR = pathlib.Path(__file__).resolve().parents[2] / "tools" / "ai" / "run_qualification_monitored.py"
RUN_CAPPED = pathlib.Path(__file__).resolve().parents[2] / "tools" / "ai" / "run_capped.py"


def sha256_file(path: pathlib.Path) -> str:
	return hashlib.sha256(path.read_bytes()).hexdigest()


def git_head(root: pathlib.Path) -> str:
	return subprocess.check_output(
		["git", "rev-parse", "HEAD"], cwd=str(root), text=True).strip()


def helper_argv(case: str, work: pathlib.Path, timeout: float) -> list[str]:
	"""Bounded per-case helper argv (the monitor's wrapped child)."""
	w = str(HELPERS / "w_wait.py")
	tree = str(HELPERS / "w_tree.py")
	rapid = str(HELPERS / "w_rapid.py")
	brk = str(HELPERS / "w_breakaway.py")
	if case == "W1_inner_nonzero_nonpass":
		return [sys.executable, w, "--seconds", "1.0", "--work-dir", str(work),
				"--ready-file", str(work / "w1n_ready.json"), "--exit-code", "7"]
	if case == "W2_normal_descendant_ledger":
		return [sys.executable, tree, "--work-dir", str(work), "--seconds", "8"]
	if case == "W3_early_parent_exit":
		return [sys.executable, tree, "--work-dir", str(work), "--seconds", "6",
				"--early-parent-exit"]
	if case == "W4_monitor_interruption" or case == "W5_wallcap_contained_tree":
		return [sys.executable, tree, "--work-dir", str(work), "--seconds", "30"]
	if case == "W6_owned_private_limit":
		# ~8.7 GiB committed inside the job: must trip MAX_OWNED_PRIVATE_BYTES.
		return [sys.executable, str(HELPERS / "w_alloc.py"),
				"--commit-mb", "8700", "--seconds", "60",
				"--work-dir", str(work),
				"--ready-file", str(work / "w6_ready.json")]
	if case == "W7_host_ram_stop" or case == "W8_unowned_large_process":
		return [sys.executable, w, "--seconds", "20", "--work-dir", str(work),
				"--ready-file", str(work / "w78_ready.json")]
	# W1_normal_short and default
	return [sys.executable, w, "--seconds", "1.5", "--work-dir", str(work),
			"--ready-file", str(work / "w1_ready.json")]


def main() -> int:
	parser = argparse.ArgumentParser()
	parser.add_argument("--case", required=True)
	parser.add_argument("--jit-id", required=True)
	parser.add_argument("--stage", type=int, default=1)
	parser.add_argument("--root", required=True)
	parser.add_argument("--evidence-root", required=True)
	parser.add_argument("--timeout", type=float, default=900.0)
	parser.add_argument("--kill-monitor", action="store_true")
	parser.add_argument("--sentinel", action="store_true")
	parser.add_argument("--cleanup", action="store_true")
	args = parser.parse_args()

	root = pathlib.Path(args.root).resolve()
	case_dir = (pathlib.Path(args.evidence_root) /
				f"{args.case}_{int(time.time())}").resolve()
	work = case_dir / "helper_work"
	work.mkdir(parents=True)
	driver_out = case_dir / "driver"  # outside the monitor evidence leaf
	driver_out.mkdir()
	evidence_leaf = case_dir / "evidence"  # created by the monitor itself

	argv_inner = helper_argv(args.case, work, args.timeout)
	inner_status = evidence_leaf / "inner_run_capped_status.json"
	child_argv = [sys.executable, "tools/ai/run_capped.py",
				  "--status-file", str(inner_status),
				  "--timeout", str(args.timeout), "--"] + argv_inner
	grant = {
		"jit_id": args.jit_id,
		"stage": args.stage,
		"argv": child_argv,
		"cwd": str(root),
		"tool_sha256": {
			"tools/ai/run_capped.py": sha256_file(RUN_CAPPED),
			"tools/ai/run_qualification_monitored.py": sha256_file(MONITOR),
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
	if args.sentinel:
		# Unowned bystander the monitor must never target (W7/W8). W8 uses a
		# genuinely large committed footprint; W7 uses a light sleeper — real
		# host-pressure admission for W7 requires an external controlled rig.
		if args.case == "W8_unowned_large_process":
			sentinel_argv = [sys.executable, str(HELPERS / "w_alloc.py"),
							 "--commit-mb", "8700", "--seconds", "30",
							 "--work-dir", str(work),
							 "--ready-file", str(work / "sentinel_ready.json")]
		else:
			sentinel_argv = [sys.executable, str(HELPERS / "w_wait.py"),
							 "--seconds", "30", "--work-dir", str(work),
							 "--ready-file", str(work / "sentinel_ready.json")]
		sentinel_proc = subprocess.Popen(sentinel_argv, cwd=str(work))

	out_f = open(driver_out / "monitor_stdout.txt", "wb")
	err_f = open(driver_out / "monitor_stderr.txt", "wb")
	mon = subprocess.Popen(monitor_argv, cwd=str(root),
						   stdout=out_f, stderr=err_f)
	killed = False
	if args.kill_monitor:
		time.sleep(6)  # let it launch + register before interruption
		mon.terminate()  # W4: outer harness kills the supervisor itself
		killed = True
	outer_deadline = time.monotonic() + args.timeout * 1.5 + 120
	rc = None
	while time.monotonic() < outer_deadline:
		rc = mon.poll()
		if rc is not None:
			break
		time.sleep(0.5)
	if rc is None:
		mon.kill()
		rc = mon.wait()
	out_f.close()
	err_f.close()
	if sentinel_proc is not None:
		sentinel_alive = sentinel_proc.poll() is None
		if sentinel_alive:
			sentinel_proc.terminate()
			sentinel_proc.wait(timeout=10)

	receipt = {"case": args.case, "jit_id": args.jit_id,
			   "monitor_exit_code": rc,
			   "killed_monitor": killed,
			   "sentinel_survived": (sentinel_alive if sentinel_proc is not None else None),
			   "evidence_dir": str(evidence_leaf),
			   "work_dir": str(work),
			   "driver_dir": str(driver_out),
			   "monitor_sha256": sha256_file(MONITOR),
			   "run_capped_sha256": sha256_file(RUN_CAPPED),
			   "source_commit": grant["source_commit"]}
	status_file = evidence_leaf / "monitor_status.json"
	if status_file.is_file():
		receipt["monitor_status"] = json.loads(status_file.read_text())
		receipt["disposition"] = receipt["monitor_status"].get("disposition")
	for name in ("monitor_stdout.txt", "monitor_stderr.txt"):
		receipt[f"{name}_sha256"] = sha256_file(driver_out / name)
	(case_dir / "case_receipt.json").write_text(
		json.dumps(receipt, indent=2, default=str), encoding="utf-8")
	print(json.dumps(receipt, indent=2, default=str))
	return 0


if __name__ == "__main__":
	sys.exit(main())
