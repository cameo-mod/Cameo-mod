#!/usr/bin/env python3
"""Run a command under a hard wall clock cap and write a fail-closed receipt."""

from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import signal
import subprocess
import sys
import time
from typing import Any, Sequence


DEFAULT_TIMEOUT_SECONDS = 900.0


def utc_now() -> str:
	return datetime.now(timezone.utc).isoformat(timespec="milliseconds").replace("+00:00", "Z")


def write_status(path: Path, status: dict[str, Any]) -> None:
	path.parent.mkdir(parents=True, exist_ok=True)
	temporary = path.with_name(f"{path.name}.{os.getpid()}.tmp")
	try:
		with temporary.open("w", encoding="utf-8", newline="\n") as stream:
			json.dump(status, stream, indent=2, sort_keys=True)
			stream.write("\n")
		os.replace(temporary, path)
	finally:
		try:
			temporary.unlink()
		except FileNotFoundError:
			pass


def kill_process_tree(process: subprocess.Popen[bytes]) -> tuple[bool, str]:
	"""Kill the process tree rooted at process.pid; report whether tree kill succeeded."""
	if process.poll() is not None:
		return True, "already-exited"

	if os.name == "nt":
		try:
			result = subprocess.run(
				["taskkill.exe", "/PID", str(process.pid), "/T", "/F"],
				capture_output=True, text=True, check=False, timeout=15,
			)
			if result.returncode == 0 or process.poll() is not None:
				return True, "taskkill-tree"
			error = result.stderr.strip() or result.stdout.strip() or f"taskkill exit {result.returncode}"
		except (OSError, subprocess.TimeoutExpired) as error:
			error = f"taskkill failed: {error}"
		# Fail closed in the receipt if the tree command failed; still stop our root.
		try:
			process.kill()
		except OSError:
			pass
		return False, error

	try:
		os.killpg(process.pid, signal.SIGKILL)
		return True, "process-group-kill"
	except ProcessLookupError:
		return process.poll() is not None, "already-exited"
	except OSError as error:
		try:
			process.kill()
			error_text = f"process-group kill failed: {error}"
		except OSError as fallback_error:
			error_text = f"process-group kill failed: {error}; root kill failed: {fallback_error}"
		return False, error_text


def run_capped(command: Sequence[str], status_path: Path, timeout_seconds: float = DEFAULT_TIMEOUT_SECONDS,
		cwd: str | os.PathLike[str] | None = None, env: dict[str, str] | None = None) -> dict[str, Any]:
	if not command:
		raise ValueError("command must contain an executable")
	if timeout_seconds <= 0:
		raise ValueError("timeout_seconds must be greater than zero")

	started_wall = time.monotonic()
	deadline = started_wall + timeout_seconds
	status: dict[str, Any] = {
		"schema": 1,
		"status": "CRASHED",
		"command": list(command),
		"timeout_seconds": timeout_seconds,
		"started_at_utc": utc_now(),
		"ended_at_utc": None,
		"elapsed_seconds": None,
		"pid": None,
		"exit_code": None,
	}
	process: subprocess.Popen[bytes] | None = None
	try:
		kwargs: dict[str, Any] = {}
		if os.name == "nt":
			kwargs["creationflags"] = subprocess.CREATE_NEW_PROCESS_GROUP
		else:
			kwargs["start_new_session"] = True
		process = subprocess.Popen(list(command), cwd=cwd, env=env, shell=False, **kwargs)
		status["pid"] = process.pid
		try:
			exit_code = process.wait(timeout=max(0.0, deadline - time.monotonic()))
			status["exit_code"] = exit_code
			status["status"] = "COMPLETED" if exit_code == 0 else "CRASHED"
		except subprocess.TimeoutExpired:
			# Once wait reports the deadline, even a just-exited process is not a
			# proven in-cap completion. Preserve the wall-cap as a non-pass outcome.
			tree_killed, kill_method = kill_process_tree(process)
			try:
				exit_code = process.wait(timeout=5)
			except subprocess.TimeoutExpired:
				try:
					process.kill()
				except OSError:
					pass
				exit_code = process.poll()
			status["exit_code"] = exit_code
			status["status"] = "INCOMPLETE_WALLCAP"
			status["tree_kill_succeeded"] = tree_killed
			status["tree_kill_method"] = kill_method
			if not tree_killed:
				status["tree_kill_error"] = kill_method
	except OSError as error:
		status["status"] = "CRASHED"
		status["error"] = f"launch failed: {error}"
	except KeyboardInterrupt:
		if process is not None and process.poll() is None:
			status["tree_kill_succeeded"], status["tree_kill_method"] = kill_process_tree(process)
			try:
				status["exit_code"] = process.wait(timeout=5)
			except subprocess.TimeoutExpired:
				status["exit_code"] = process.poll()
		status["status"] = "CRASHED"
		status["error"] = "supervisor interrupted"
		raise
	finally:
		status["ended_at_utc"] = utc_now()
		status["elapsed_seconds"] = round(time.monotonic() - started_wall, 3)
		write_status(status_path, status)
	return status


def positive_seconds(value: str) -> float:
	try:
		seconds = float(value)
		if seconds > 0 and seconds < float("inf"):
			return seconds
	except ValueError:
		pass
	raise argparse.ArgumentTypeError("timeout must be a finite number greater than zero")


def main(argv: Sequence[str] | None = None) -> int:
	parser = argparse.ArgumentParser(description=__doc__)
	parser.add_argument("--status-file", required=True, type=Path,
		help="path for the machine-readable JSON outcome")
	parser.add_argument("--timeout", type=positive_seconds, default=DEFAULT_TIMEOUT_SECONDS,
		help=f"hard wall clock cap in seconds (default: {DEFAULT_TIMEOUT_SECONDS:g})")
	parser.add_argument("command", nargs=argparse.REMAINDER, help="command and arguments, after --")
	args = parser.parse_args(argv)
	command = args.command[1:] if args.command[:1] == ["--"] else args.command
	if not command:
		parser.error("a command is required after --")
	try:
		status = run_capped(command, args.status_file, args.timeout)
	except (ValueError, OSError) as error:
		parser.error(str(error))

	if status["status"] == "COMPLETED" and status["exit_code"] == 0:
		return 0
	if status["status"] == "INCOMPLETE_WALLCAP":
		return 124
	return status["exit_code"] if isinstance(status["exit_code"], int) and status["exit_code"] > 0 else 125


if __name__ == "__main__":
	raise SystemExit(main())
