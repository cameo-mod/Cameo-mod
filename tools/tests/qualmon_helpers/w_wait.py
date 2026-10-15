"""W-case leaf helper: bounded wait with identity/evidence markers.

Used by the outer driver (w_driver.py) under separately admitted JIT stages —
this file is never launched by the deterministic test module.

Behavior:
  * writes a ready file containing pid + creation-independent identity fields
  * loops for --seconds, heartbeating --heartbeat-file if given
  * exits with --exit-code

Python stdlib only; no games, no stress, no network.
"""

import argparse
import json
import os
import pathlib
import sys
import time


def main() -> int:
	parser = argparse.ArgumentParser()
	parser.add_argument("--seconds", type=float, required=True)
	parser.add_argument("--ready-file")
	parser.add_argument("--heartbeat-file")
	parser.add_argument("--exit-code", type=int, default=0)
	parser.add_argument("--work-dir", default=None)
	args = parser.parse_args()

	work = pathlib.Path(args.work_dir or os.getcwd())
	work.mkdir(parents=True, exist_ok=True)

	identity = {
		"pid": os.getpid(),
		"ppid": os.getppid(),
		"argv_tail": sys.argv[1:],
		"ready_utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
	}
	if args.ready_file:
		(pathlib.Path(args.ready_file)).write_text(
			json.dumps(identity, indent=2), encoding="utf-8")

	deadline = time.monotonic() + args.seconds
	beat = 0
	while time.monotonic() < deadline:
		if args.heartbeat_file:
			beat += 1
			(pathlib.Path(args.heartbeat_file)).write_text(
				json.dumps({"pid": os.getpid(), "beat": beat,
							"remaining_seconds": round(deadline - time.monotonic(), 3)}),
				encoding="utf-8")
		time.sleep(min(0.25, max(0.0, deadline - time.monotonic())))

	return args.exit_code


if __name__ == "__main__":
	sys.exit(main())
