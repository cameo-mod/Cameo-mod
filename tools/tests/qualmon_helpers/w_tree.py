"""W2/W3 helper: bounded multi-generation process tree.

Parent -> mid -> grandchild, each writing an identity marker. With
--early-parent-exit the parent exits immediately after spawning mid, leaving
the grandchild lingering (W3 lingering-child case).

Used only by w_driver.py under separately admitted JIT stages. Stdlib only;
subprocesses are children of THIS helper tree, never foreign processes.
"""

import argparse
import json
import os
import pathlib
import subprocess
import sys
import time


def marker(work: pathlib.Path, name: str, pid: int, ppid: int) -> None:
	(work / f"{name}_marker.json").write_text(json.dumps({
		"role": name, "pid": pid, "ppid": ppid,
		"utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
	}, indent=2), encoding="utf-8")


def spawn(seconds: float, work: pathlib.Path, name: str) -> subprocess.Popen:
	return subprocess.Popen(
		[sys.executable, str(pathlib.Path(__file__).parent / "w_wait.py"),
		 "--seconds", str(seconds), "--work-dir", str(work),
		 "--ready-file", str(work / f"{name}_ready.json")],
		cwd=str(work))


def main() -> int:
	parser = argparse.ArgumentParser()
	parser.add_argument("--work-dir", required=True)
	parser.add_argument("--seconds", type=float, default=8.0)
	parser.add_argument("--early-parent-exit", action="store_true")
	args = parser.parse_args()

	work = pathlib.Path(args.work_dir)
	work.mkdir(parents=True, exist_ok=True)
	marker(work, "parent", os.getpid(), os.getppid())

	mid_seconds = max(1.0, args.seconds * 0.5)
	grand_seconds = max(2.0, args.seconds * 1.5)

	# mid is a small wrapper that itself spawns the grandchild
	mid_script = work / "mid_spawn.py"
	mid_script.write_text(
		"import subprocess,sys,pathlib,json,os,time\n"
		f"work=pathlib.Path({str(work)!r})\n"
		"p=subprocess.Popen([sys.executable, "
		f"{str(pathlib.Path(__file__).parent / 'w_wait.py')!r}, "
		f"'--seconds', '{grand_seconds}', '--work-dir', str(work), "
		f"'--ready-file', str(work/'grandchild_ready.json')],cwd=str(work))\n"
		"(work/'grandchild_spawn.json').write_text(json.dumps("
		"{'pid':p.pid,'ppid':os.getpid()}))\n"
		f"time.sleep({mid_seconds});p.wait()\n",
		encoding="utf-8")
	mid = subprocess.Popen([sys.executable, str(mid_script)], cwd=str(work))
	(work / "mid_spawn.json").write_text(
		json.dumps({"pid": mid.pid, "ppid": os.getpid()}), encoding="utf-8")

	if args.early_parent_exit:
		# W3: parent exits with 0 while descendants keep running.
		return 0
	rc = mid.wait()
	return int(rc or 0)


if __name__ == "__main__":
	sys.exit(main())
