"""W-adjacent helper: rapid child spawn/exit churn inside the helper tree.

Spawns up to 6 short-lived w_wait.py children back-to-back (each exits in
<1s), recording every child pid. Used for rapid-discovery and unknown-
accounting probing under admitted JIT stages. Stdlib only, bounded.
"""

import argparse
import json
import os
import pathlib
import subprocess
import sys
import time


def main() -> int:
	parser = argparse.ArgumentParser()
	parser.add_argument("--work-dir", required=True)
	parser.add_argument("--count", type=int, default=6)
	args = parser.parse_args()
	work = pathlib.Path(args.work_dir)
	work.mkdir(parents=True, exist_ok=True)
	count = max(1, min(args.count, 6))  # hard bound
	helper = pathlib.Path(__file__).parent / "w_wait.py"

	pids = []
	procs = []
	for i in range(count):
		p = subprocess.Popen(
			[sys.executable, str(helper), "--seconds", "0.4",
			 "--work-dir", str(work),
			 "--ready-file", str(work / f"rapid_{i}_ready.json")],
			cwd=str(work))
		pids.append(p.pid)
		procs.append(p)
	for p in procs:
		p.wait()
	(work / "rapid_result.json").write_text(json.dumps(
		{"parent_pid": os.getpid(), "child_pids": pids}, indent=2),
		encoding="utf-8")
	return 0


if __name__ == "__main__":
	sys.exit(main())
