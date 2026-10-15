"""W-adjacent helper: attempt CREATE_BREAKAWAY_FROM_JOB and record the result.

Records attempt_success / child_pid / win32 error so the driver can assert
whether containment held. Stdlib only. Runs only under admitted JIT stages.
"""

import argparse
import ctypes
import json
import os
import pathlib
import subprocess
import sys
import time

CREATE_BREAKAWAY_FROM_JOB = 0x01000000
CREATE_SUSPENDED_FLAG = 0x00000004


def main() -> int:
	parser = argparse.ArgumentParser()
	parser.add_argument("--work-dir", required=True)
	parser.add_argument("--seconds", type=float, default=8.0)
	args = parser.parse_args()
	if not 0.5 <= args.seconds <= 10.0:  # bounded args enforced
		return 2
	work = pathlib.Path(args.work_dir)
	work.mkdir(parents=True, exist_ok=True)

	result = {"attempted": "CREATE_BREAKAWAY_FROM_JOB",
			  "os": os.name, "platform": sys.platform}
	if os.name != "nt":
		result.update({"attempt_success": False, "reason": "not_windows"})
	else:
		try:
			helper = pathlib.Path(__file__).parent / "w_wait.py"
			proc = subprocess.Popen(
				[sys.executable, str(helper), "--seconds", str(args.seconds),
				 "--work-dir", str(work),
				 "--ready-file", str(work / "breakaway_child_ready.json")],
				cwd=str(work), creationflags=CREATE_BREAKAWAY_FROM_JOB)
			result.update({"attempt_success": True, "child_pid": proc.pid})
			proc.wait()
		except OSError as error:
			result.update({"attempt_success": False, "win32_error": str(error)})

	(work / "breakaway_result.json").write_text(
		json.dumps(result, indent=2), encoding="utf-8")
	return 0


if __name__ == "__main__":
	sys.exit(main())
