"""W5 helper: attempt CREATE_BREAKAWAY_FROM_JOB and record full evidence.

Not a bare pass/fail string: records the exact creationflags used, the Win32
API result, the child's (pid, creation-filetime) identity, whether the child
actually reports in-a-job after a "successful" breakaway (a child still in
the job never escaped regardless of the spawn call's return), and the child's
lifetime/exit state at helper exit. The driver cross-checks these fields and
the post-case liveness to prove no live escape. Stdlib + ctypes only. Runs
only under admitted JIT stages.
"""

import argparse
import ctypes
from ctypes import wintypes
import json
import os
import pathlib
import subprocess
import sys
import time

CREATE_BREAKAWAY_FROM_JOB = 0x01000000
PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
STILL_ACTIVE = 259


class _FILETIME(ctypes.Structure):
	_fields_ = [("dwLowDateTime", wintypes.DWORD),
				("dwHighDateTime", wintypes.DWORD)]


def _open_limited(pid: int):
	if os.name != "nt":
		return None
	return ctypes.windll.kernel32.OpenProcess(
		PROCESS_QUERY_LIMITED_INFORMATION, False, pid)


def _creation_filetime(handle) -> int | None:
	ct, et, kt, ut = _FILETIME(), _FILETIME(), _FILETIME(), _FILETIME()
	if not ctypes.windll.kernel32.GetProcessTimes(
			handle, ctypes.byref(ct), ctypes.byref(et),
			ctypes.byref(kt), ctypes.byref(ut)):
		return None
	return (ct.dwHighDateTime << 32) | ct.dwLowDateTime


def _in_any_job(handle) -> bool | None:
	"""IsProcessInJob with a NULL job handle = membership in ANY job."""
	result = wintypes.BOOL()
	if not ctypes.windll.kernel32.IsProcessInJob(
			handle, None, ctypes.byref(result)):
		return None
	return bool(result.value)


def _exit_code(handle) -> int | None:
	code = wintypes.DWORD()
	if not ctypes.windll.kernel32.GetExitCodeProcess(
			handle, ctypes.byref(code)):
		return None
	return None if code.value == STILL_ACTIVE else int(code.value)


def main() -> int:
	parser = argparse.ArgumentParser()
	parser.add_argument("--work-dir", required=True)
	parser.add_argument("--seconds", type=float, default=8.0)
	args = parser.parse_args()
	if not 0.5 <= args.seconds <= 10.0:  # bounded args enforced
		return 2
	work = pathlib.Path(args.work_dir)
	work.mkdir(parents=True, exist_ok=True)

	flags = CREATE_BREAKAWAY_FROM_JOB
	result = {"attempted": "CREATE_BREAKAWAY_FROM_JOB",
			  "creationflags_hex": hex(flags),
			  "os": os.name, "platform": sys.platform}
	if os.name != "nt":
		result.update({"attempt_success": False, "reason": "not_windows"})
	else:
		proc = None
		try:
			helper = pathlib.Path(__file__).parent / "w_wait.py"
			proc = subprocess.Popen(
				[sys.executable, str(helper), "--seconds", str(args.seconds),
				 "--work-dir", str(work),
				 "--ready-file", str(work / "breakaway_child_ready.json")],
				cwd=str(work), creationflags=flags)
			result.update({"attempt_success": True, "child_pid": proc.pid})
		except OSError as error:
			# Record the real API failure (e.g. ERROR_ACCESS_DENIED when the
			# job disallows breakaway) — a refused spawn is containment held.
			result.update({"attempt_success": False,
						   "win32_error": getattr(error, "winerror", None),
						   "win32_error_text": str(error)})

		if proc is not None:
			# Identity + membership + lifetime of the spawned child.
			handle = _open_limited(proc.pid)
			if handle:
				try:
					result["child_creation_filetime"] = _creation_filetime(handle)
					result["child_in_any_job_at_spawn"] = _in_any_job(handle)
				finally:
					ctypes.windll.kernel32.CloseHandle(handle)
			spawned_mono = time.monotonic()
			proc.wait()
			handle = _open_limited(proc.pid)
			try:
				result["child_exit_code"] = (
					proc.returncode if proc.returncode is not None
					else (_exit_code(handle) if handle else None))
				if handle:
					result["child_in_any_job_at_exit"] = _in_any_job(handle)
			finally:
				if handle:
					ctypes.windll.kernel32.CloseHandle(handle)
			result["child_lifetime_seconds"] = round(
				time.monotonic() - spawned_mono, 3)
			# Measured, not asserted: proc.wait() returned above, so poll()
			# reflects the real post-wait liveness.
			result["child_alive_at_helper_exit"] = proc.poll() is None

	(work / "breakaway_result.json").write_text(
		json.dumps(result, indent=2), encoding="utf-8")
	return 0


if __name__ == "__main__":
	sys.exit(main())
