"""Deterministic focused coverage for run_qualification_monitored (D1-D8 + controls).

Matrix: SPEC_2026-10-14_qualification_monitor_validation_matrix_sol.md §B
(SHA-256 43c00a15fce7c94d1262b257fd09cca071be46d4cdab1a77505289b9a3308e36),
bound to supervisor tip a0fc78ea11b8644e93015110c1255becd899899d (file SHA-256
6174757cafb765d45ef2805b12ff14a1e13669fd4d1ad1888734e18a9ee12397).

Every node injects a scripted Win32Adapter double and a fake monotonic clock;
no real processes are launched and no real memory pressure is created
(matrix §B: injected values are labelled simulated and cannot attest real
host pressure). The real-adapter W1-W8 helper cases live under
tools/tests/qualmon_helpers/ and run only under separate JIT admission.

Known under-review items encoded as contract assertions (see the freeze
receipt for this suite):
* test_d8_tool_hash_drift_is_hold asserts the receipt never reads HOLD while
  the process exits 0 — the reviewed tip computes `code` before the post-run
  drift check in _write_final; a successor must return the final disposition.
* TestMonitor._resolve_source_commit adds commondir following the production
  resolver lacks on linked worktrees (the real qual environment); used ONLY
  to synthesize matching grants. The unresolvable-source refusal itself is
  correct fail-closed behavior.
"""

from __future__ import annotations

import argparse
from collections import deque
import json
import pathlib
import sys
import tempfile
import time
import unittest
import unittest.mock

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))
import run_qualification_monitored as rqm

GIB = 1 << 30
ROOT_CREATION_BASE = 133_000_000_000_000_000


class FakeClock:
	"""Deterministic monotonic source; only advanced by FakeEvent/tests."""

	def __init__(self, start: float = 1_000_000.0) -> None:
		self.t = start

	def __call__(self) -> float:
		return self.t

	def advance(self, seconds: float) -> None:
		self.t += seconds


class FakeEvent:
	"""Pacing stand-in for threading.Event: wait() advances the fake clock."""

	def __init__(self, clock: FakeClock, advance_min: float = 0.0) -> None:
		self.clock = clock
		self.advance_min = advance_min
		self._set = False
		self.wait_requests: list[float] = []

	def wait(self, seconds: float) -> bool:
		self.wait_requests.append(seconds)
		self.clock.advance(max(seconds, self.advance_min))
		return False

	def set(self) -> None:
		self._set = True

	def clear(self) -> None:
		self._set = False

	def is_set(self) -> bool:
		return self._set


class FakeProcess:
	__slots__ = ("pid", "creation", "parent", "exited", "exit_code", "private")

	def __init__(self, pid: int, creation: int, parent: int,
				 private: int = 64 << 20) -> None:
		self.pid = pid
		self.creation = creation
		self.parent = parent
		self.exited = False
		self.exit_code = None
		self.private = private


class FakeAdapter:
	"""Scripted Win32Adapter double. Handles are opaque tokens keyed to the
	(pid, creation) identity they were opened against, so a recycled PID is
	distinguishable from the old identity — mirroring the real adapter."""

	def __init__(self) -> None:
		self.supervisor = (4242, 132_000_000_000_000_000, False)
		self.procs: dict[tuple[int, int], FakeProcess] = {}
		self.live_pid: dict[int, tuple[int, int]] = {}
		self.job_members: list[tuple[int, int]] = []
		self.total_ever = 0
		self.terminated_ever = 0
		self.notifications: deque[tuple[int, int]] = deque()
		self.avail_series: deque[int] = deque([30 * GIB])
		self.total_phys = 32 * GIB
		self.disk_free = 500 * GIB
		self.fail_on: dict[str, bool] = {}
		self.in_job_override: bool | None = None
		self.id_series: deque[list[int]] | None = None
		self.on_launch = None
		self.private_calls: list[tuple[int, int]] = []
		self.launched = False
		self.resumed = False
		self.job_terminated = False
		self.direct_terminations: list[int] = []
		self._serial = 0
		self._handles: dict[int, tuple[int, int]] = {}
		self._next_key = 7000

	def _maybe_fail(self, name: str) -> None:
		if self.fail_on.get(name):
			raise rqm.AdapterError(f"{name} failed: injected")

	def _token(self, key: tuple[int, int]) -> int:
		self._serial += 1
		self._handles[self._serial] = key
		return self._serial

	def supervisor_identity(self):
		return self.supervisor

	def own_process_in_job(self) -> bool:
		return self.supervisor[2]

	def create_job(self, name: str):
		return 9001

	def job_limit_flags(self, job) -> int:
		return rqm.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE

	def create_completion_port(self):
		return 9002

	def associate_completion_port(self, job, port, key) -> None:
		pass

	def get_completion(self, port, timeout_ms: int):
		self._maybe_fail("get_completion")
		if self.notifications:
			return self.notifications.popleft()
		time.sleep(0.01)  # keep the real event thread from busy-spinning
		return None

	def launch_suspended_in_job(self, cmdline, job, cwd, std_handles):
		self._maybe_fail("launch_suspended_in_job")
		self.launched = True
		self._next_key += 1
		pid = self._next_key
		creation = ROOT_CREATION_BASE + pid
		key = (pid, creation)
		self.procs[key] = FakeProcess(pid, creation, self.supervisor[0])
		self.live_pid[pid] = key
		self.job_members.append(key)
		self.total_ever += 1
		h_proc = self._token(key)
		h_thread = self._token(key)
		if self.on_launch is not None:
			self.on_launch(pid, creation)
		return h_proc, h_thread, pid

	def resume_thread(self, thread) -> None:
		self.resumed = True

	def is_process_in_job(self, process, job) -> bool:
		if self.in_job_override is not None:
			return self.in_job_override
		key = self._handles.get(process)
		# An exited member remains a job-assigned identity for its handle.
		return key in self.job_members or key in self.procs

	def open_process(self, pid: int, access: int = 0):
		self._maybe_fail("open_process")
		key = self.live_pid.get(pid)
		return self._token(key) if key is not None else None

	def process_creation_time(self, process) -> int:
		self._maybe_fail("process_creation_time")
		return self._handles[process][1]

	def process_exit_state(self, process):
		self._maybe_fail("process_exit_state")
		proc = self.procs[self._handles[process]]
		return proc.exited, proc.exit_code

	def wait_process(self, process, timeout_ms: int) -> bool:
		return self.procs[self._handles[process]].exited

	def terminate_job(self, job, exit_code: int = 0xFFFF) -> None:
		self._maybe_fail("terminate_job")
		self.job_terminated = True
		for key in list(self.job_members):
			proc = self.procs[key]
			proc.exited = True
			proc.exit_code = exit_code
			self.live_pid.pop(proc.pid, None)
			self.terminated_ever += 1
		self.job_members.clear()

	def terminate_process(self, process, exit_code: int = 0xFFFE) -> None:
		key = self._handles[process]
		self.direct_terminations.append(key[0])
		proc = self.procs[key]
		proc.exited = True
		proc.exit_code = exit_code
		if key in self.job_members:
			self.job_members.remove(key)
		self.live_pid.pop(proc.pid, None)

	def job_process_ids(self, job):
		self._maybe_fail("job_process_ids")
		if self.id_series:
			return [k[0] for k in self.id_series.popleft()]
		return [k[0] for k in self.job_members]

	def job_accounting(self, job):
		self._maybe_fail("job_accounting")
		return {
			"TotalProcesses": self.total_ever,
			"ActiveProcesses": len(self.job_members),
			"TotalTerminatedProcesses": self.terminated_ever,
			"TotalPageFaultCount": 0,
		}

	def private_bytes(self, process) -> int:
		self._maybe_fail("private_bytes")
		key = self._handles[process]
		self.private_calls.append(key)
		return self.procs[key].private

	def available_ram_bytes(self):
		self._maybe_fail("available_ram_bytes")
		if len(self.avail_series) > 1:
			avail = self.avail_series.popleft()
		else:
			avail = self.avail_series[0]
		return avail, self.total_phys

	def free_disk_bytes(self, path: str) -> int:
		self._maybe_fail("free_disk_bytes")
		return self.disk_free

	def open_inheritable_file(self, path, for_write: bool):
		self._serial += 1
		return self._serial

	def process_census(self) -> dict[int, int]:
		self._maybe_fail("process_census")
		return {p.pid: p.parent for p in self.procs.values() if not p.exited}

	def close_handle(self, handle) -> None:
		pass

	# --- scripting helpers (fake-side state; monitor never sees these) ---------
	def spawn_member(self, parent_key: tuple[int, int], private: int = 32 << 20,
					 notify: bool = True) -> tuple[int, int]:
		self._next_key += 1
		pid = self._next_key
		creation = ROOT_CREATION_BASE + pid
		key = (pid, creation)
		self.procs[key] = FakeProcess(pid, creation, parent_key[0], private)
		self.live_pid[pid] = key
		self.job_members.append(key)
		self.total_ever += 1
		if notify:
			self.notifications.append((rqm.JOB_OBJECT_MSG_NEW_PROCESS, pid))
		return key

	def mark_exited(self, key: tuple[int, int], code: int = 0,
					notify: bool = True) -> None:
		proc = self.procs[key]
		proc.exited = True
		proc.exit_code = code
		self.terminated_ever += 1
		if key in self.job_members:
			self.job_members.remove(key)
		self.live_pid.pop(proc.pid, None)
		if notify:
			self.notifications.append((rqm.JOB_OBJECT_MSG_EXIT_PROCESS, proc.pid))


class FailingWriter:
	"""Wraps an AppendOnlyWriter; raises OSError after `after` writes."""

	def __init__(self, inner, after: int) -> None:
		self.inner = inner
		self.after = after
		self.calls = 0

	@property
	def path(self):
		return self.inner.path

	@property
	def records(self):
		return self.inner.records

	def write(self, record):
		self.calls += 1
		if self.calls > self.after:
			raise OSError("injected append failure")
		return self.inner.write(record)

	def close(self):
		self.inner.close()


class TestMonitor(rqm.Monitor):
	"""Test seam: scripted inner status (the fake never runs run_capped),
	writer-failure injection, and a commondir-aware resolver used ONLY to
	synthesize a matching grant inside a linked-worktree checkout. The
	production resolver's commondir gap is a separately flagged finding."""

	inner_override = None
	fail_writer_after: dict[str, int] | None = None

	def queue_exit(self, pid: int) -> None:
		"""Deterministically deliver an exit notification on the real drain
		path (monitor._notify_queue), bypassing the event-thread race."""
		with self._notify_lock:
			self._notify_queue.append((rqm.JOB_OBJECT_MSG_EXIT_PROCESS, pid))

	def exit_root_on_launch(self, fake: FakeAdapter, code: int = 0):
		"""on_launch hook: mark the fake root exited + queue its notification."""
		def on_launch(pid, creation):
			fake.mark_exited((pid, creation), code=code, notify=False)
			self.queue_exit(pid)
		return on_launch

	def _read_inner_status(self, evidence_dir):
		if self.inner_override is not None:
			self.inner_status = self.inner_override
			return self.inner_status
		return super()._read_inner_status(evidence_dir)

	def _open_evidence(self, evidence_dir):
		super()._open_evidence(evidence_dir)
		for key, after in (self.fail_writer_after or {}).items():
			self.writers[key] = FailingWriter(self.writers[key], after)

	def _resolve_source_commit(self, repo_root):
		"""Test-side resolver that follows worktree commondir so grants can
		bind inside a linked worktree. Never alters monitor behavior."""
		try:
			gitfile = pathlib.Path(repo_root) / ".git"
			if gitfile.is_file():
				gitdir = pathlib.Path(gitfile.read_text().split(":", 1)[1].strip())
				if not gitdir.is_absolute():
					gitdir = (pathlib.Path(repo_root) / gitdir).resolve()
			else:
				gitdir = gitfile
			commondir = gitdir
			common = gitdir / "commondir"
			if common.is_file():
				commondir = (gitdir / common.read_text().strip()).resolve()
			head = (gitdir / "HEAD").read_text().strip()
			if head.startswith("ref:"):
				ref = head[4:].strip()
				ref_path = commondir / ref
				if ref_path.is_file():
					return ref_path.read_text().strip()
				for line in (commondir / "packed-refs").read_text().splitlines():
					if not line.startswith("#") and line.endswith(f" {ref}"):
						return line.split()[0]
				return None
			return head
		except (OSError, IndexError):
			return None


def make_args(tmp: pathlib.Path, timeout: float = 900.0,
			  inner_tail: list[str] | None = None,
			  grant_extra: dict | None = None) -> argparse.Namespace:
	"""Grant + args pair that passes preflight against the test worktree."""
	evidence = tmp / "evidence_case"
	inner_status = (evidence / "inner_run_capped_status.json").resolve()
	tail = inner_tail or [sys.executable, "-c", "pass"]
	child_argv = [
		sys.executable, "tools/ai/run_capped.py",
		"--status-file", str(inner_status),
		"--timeout", str(timeout), "--",
	] + tail
	run_capped_path = ROOT / "tools" / "ai" / "run_capped.py"
	probe = TestMonitor(argparse.Namespace(
		jit_id="probe", stage=1, grant_input="x", evidence_dir=evidence,
		timeout=timeout, child_argv=child_argv), adapter=FakeAdapter())
	commit = probe._resolve_source_commit(ROOT)
	grant = {
		"jit_id": "JT-test",
		"stage": 1,
		"argv": child_argv,
		"cwd": str(ROOT),
		"tool_sha256": {"tools/ai/run_capped.py": rqm.sha256_file(run_capped_path)},
		"source_commit": commit,
		"termination_timeout_seconds": 30,
		"clearance_timeout_seconds": 60,
	}
	grant.update(grant_extra or {})
	grant_path = tmp / "grant.json"
	grant_path.write_text(json.dumps(grant, indent=2), encoding="utf-8")
	return argparse.Namespace(
		jit_id="JT-test", stage=1, grant_input=str(grant_path),
		evidence_dir=evidence, timeout=timeout, child_argv=child_argv)


def build(tmp: pathlib.Path, fake: FakeAdapter, clock: FakeClock,
		  timeout: float = 900.0, inner=None, fail_writer_after=None,
		  pace_min: float = 0.0, grant_extra=None) -> TestMonitor:
	args = make_args(tmp, timeout=timeout, grant_extra=grant_extra)
	monitor = TestMonitor(args, adapter=fake, monotonic=clock)
	monitor.inner_override = inner
	monitor.fail_writer_after = fail_writer_after
	monitor._pace = FakeEvent(clock, advance_min=pace_min)
	return monitor


def prep_state(monitor: TestMonitor, fake: FakeAdapter, tmp: pathlib.Path):
	"""Method-level state: evidence writers + supervisor identity + job token."""
	evidence = tmp / "evidence_method"
	evidence.mkdir(exist_ok=True)
	monitor._open_evidence(evidence)
	monitor.job = fake.create_job("t")
	(monitor._supervisor_pid, monitor._supervisor_creation,
	 monitor._supervisor_in_job) = fake.supervisor_identity()
	return monitor


def read_jsonl(path: pathlib.Path) -> list[dict]:
	return [json.loads(line) for line in path.read_text().splitlines() if line.strip()]


class QualMonitorDeterministicTests(unittest.TestCase):
	"""D1-D8 deterministic adapter/clock/fault cases plus regression controls."""

	def setUp(self):
		self.tmp = pathlib.Path(tempfile.mkdtemp(prefix="qualmon_t_"))

	# ---- regression controls (W1 positive/negative + no-error path) -------------

	def test_positive_control_completed_exit0(self):
		"""No-error run remains COMPLETED/exit 0 (reviewer positive control)."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = build(self.tmp, fake, clock,
						inner={"status": "COMPLETED", "exit_code": 0})
		fake.on_launch = monitor.exit_root_on_launch(fake, 0)
		code = monitor.run()
		receipt = json.loads(
			(pathlib.Path(monitor.args.evidence_dir) / "monitor_status.json").read_text())
		self.assertEqual(code, rqm.EXIT_COMPLETED)
		self.assertEqual(receipt["disposition"], "COMPLETED")
		self.assertFalse(receipt["evidence_write_failed"])

	def test_w1_negative_inner_nonzero_is_nonpass(self):
		"""Inner non-zero status cannot produce monitor COMPLETED/exit 0."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = build(self.tmp, fake, clock,
						inner={"status": "CRASHED", "exit_code": 7})
		fake.on_launch = monitor.exit_root_on_launch(fake, 7)
		code = monitor.run()
		self.assertNotEqual(code, rqm.EXIT_COMPLETED)
		receipt = json.loads(
			(pathlib.Path(monitor.args.evidence_dir) / "monitor_status.json").read_text())
		self.assertEqual(receipt["disposition"], "INNER_FAILURE")

	# ---- D1 available-memory boundary -------------------------------------------

	def test_d1_available_boundary_equality_then_below(self):
		"""avail == 4 GiB must not trip; one byte below must stop (matrix D1)."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = prep_state(build(self.tmp, fake, clock), fake, self.tmp)
		h, _t, pid = fake.launch_suspended_in_job("x", monitor.job, None, (1, 2, 3))
		key = (pid, ROOT_CREATION_BASE + pid)
		monitor._register_member(pid, "launch", None, is_root=True,
								 handle=h, creation=key[1])
		fake.avail_series = deque([rqm.STOP_AVAIL_RAM_BYTES])  # exact equality
		fake.mark_exited(key, code=0, notify=False)  # exits on first drain
		monitor.queue_exit(pid)
		monitor._monitor_loop()
		self.assertIsNone(monitor.stop_reason)

		fake2, clock2 = FakeAdapter(), FakeClock()
		tmp2 = pathlib.Path(tempfile.mkdtemp(prefix="qualmon_t2_"))
		monitor2 = prep_state(build(tmp2, fake2, clock2), fake2, tmp2)
		h2, _t2, pid2 = fake2.launch_suspended_in_job("x", monitor2.job, None, (1, 2, 3))
		key2 = (pid2, ROOT_CREATION_BASE + pid2)
		monitor2._register_member(pid2, "launch", None, is_root=True,
								  handle=h2, creation=key2[1])
		fake2.avail_series = deque([rqm.STOP_AVAIL_RAM_BYTES - 1])  # below
		monitor2._monitor_loop()
		self.assertIsNotNone(monitor2.stop_reason)
		self.assertIn("available RAM", monitor2.stop_reason)
		samples = read_jsonl(monitor2.writers["samples"].path)
		self.assertIn("avail_phys_bytes", samples[-1]["host"])

	# ---- D2 private-bytes boundary ------------------------------------------------

	def test_d2_private_boundary_equality_then_above(self):
		"""Owned private == 8 GiB does not trip; +1 trips; unowned never sampled."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = prep_state(build(self.tmp, fake, clock), fake, self.tmp)
		key = fake.spawn_member((4242, 1), notify=False)
		entry = monitor._register_member(key[0], "membership_query",
									   fake.process_census())
		fake.procs[key].private = rqm.MAX_OWNED_PRIVATE_BYTES
		rec = monitor._member_sample(entry)
		self.assertIsNone(monitor.stop_reason)
		self.assertEqual(rec["private_bytes"], rqm.MAX_OWNED_PRIVATE_BYTES)
		fake.procs[key].private = rqm.MAX_OWNED_PRIVATE_BYTES + 1
		monitor._member_sample(entry)
		self.assertIsNotNone(monitor.stop_reason)
		self.assertIn("private bytes", monitor.stop_reason)
		# An unrelated large process is never sampled or targeted.
		foreign = FakeProcess(9999, 1, 3, private=rqm.MAX_OWNED_PRIVATE_BYTES * 2)
		fake.procs[(9999, 1)] = foreign
		fake.live_pid[9999] = (9999, 1)
		fake.process_census()
		self.assertNotIn((9999, 1), fake.private_calls)

	# ---- D3 PID reuse -----------------------------------------------------------------

	def test_d3_pid_reuse_distinct_identity(self):
		"""Reused PID + new creation FILETIME is a distinct identity; old exit kept."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = prep_state(build(self.tmp, fake, clock), fake, self.tmp)
		h, _t, pid = fake.launch_suspended_in_job("x", monitor.job, None, (1, 2, 3))
		old_key = (pid, ROOT_CREATION_BASE + pid)
		monitor._register_member(pid, "launch", None, is_root=True,
								 handle=h, creation=old_key[1])
		fake.mark_exited(old_key, code=0, notify=False)
		monitor.queue_exit(pid)
		monitor._drain_notifications()
		old_entry = monitor.ledger[f"{old_key[0]}:{old_key[1]}"]
		monitor._verify_exit(old_entry)
		self.assertTrue(old_entry.exit_verified)
		# Same PID returns with a new creation time inside the job.
		new_key = (pid, old_key[1] + 1)
		fake.procs[new_key] = FakeProcess(pid, new_key[1], old_key[0])
		fake.live_pid[pid] = new_key
		fake.job_members.append(new_key)
		fake.total_ever += 1
		monitor._reconcile_membership()
		new_entry = monitor.ledger[f"{pid}:{new_key[1]}"]
		self.assertNotEqual(new_entry.creation, old_key[1])
		self.assertFalse(new_entry.exited)
		self.assertTrue(old_entry.exit_verified)  # exit proof not inherited

	# ---- D4 discovery loss ----------------------------------------------------------------

	def test_d4_dropped_notification_reconciles_via_membership(self):
		"""Dropped NEW_PROCESS: membership query still registers the member."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = prep_state(build(self.tmp, fake, clock), fake, self.tmp)
		h, _t, pid = fake.launch_suspended_in_job("x", monitor.job, None, (1, 2, 3))
		root_key = (pid, ROOT_CREATION_BASE + pid)
		monitor._register_member(pid, "launch", None, is_root=True,
								 handle=h, creation=root_key[1])
		child_key = fake.spawn_member(root_key, notify=False)  # dropped
		monitor._reconcile_membership()
		entry = monitor.ledger[f"{child_key[0]}:{child_key[1]}"]
		self.assertEqual(entry.source, "membership_query")
		self.assertEqual(entry.linkage, "defended")
		self.assertFalse(monitor.identity_gaps)

	def test_d4_missing_short_lived_identity_is_hold(self):
		"""TotalProcesses > known identities with no openable member → gap → HOLD."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = prep_state(build(self.tmp, fake, clock), fake, self.tmp)
		h, _t, pid = fake.launch_suspended_in_job("x", monitor.job, None, (1, 2, 3))
		root_key = (pid, ROOT_CREATION_BASE + pid)
		monitor._register_member(pid, "launch", None, is_root=True,
								 handle=h, creation=root_key[1])
		fake.total_ever += 1  # unseen short-lived child
		monitor._reconcile_membership()
		monitor._reconcile_accounting()
		self.assertTrue(monitor.identity_gaps)
		self.assertTrue(any(g.get("kind") == "accounting_gap"
							for g in monitor.identity_gaps))
		self.assertEqual(
			monitor._disposition({"status": "COMPLETED", "exit_code": 0}, True),
			"HOLD")

	# ---- D5 query/assignment failure ------------------------------------------------------

	def test_d5_assignment_failure_prevents_launch(self):
		"""Job-list launch failure pre-resume → REFUSED; nothing ever ran."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = build(self.tmp, fake, clock)
		fake.fail_on["launch_suspended_in_job"] = True
		code = monitor.run()
		self.assertEqual(code, rqm.EXIT_REFUSAL)
		self.assertFalse(fake.launched)
		self.assertFalse(monitor.resumed)
		self.assertFalse(fake.job_terminated)

	def test_d5_not_in_job_terminates_suspended_root(self):
		"""Membership check failure post-launch kills only the suspended root."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = build(self.tmp, fake, clock)
		fake.in_job_override = False
		code = monitor.run()
		self.assertEqual(code, rqm.EXIT_REFUSAL)
		self.assertEqual(len(fake.direct_terminations), 1)
		self.assertIn(next(iter(fake.procs))[0], fake.direct_terminations)
		self.assertFalse(monitor.resumed)

	def test_d5_query_failure_is_hold(self):
		"""A membership query error mid-run stops the job and lands HOLD."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = build(self.tmp, fake, clock,
						inner={"status": "COMPLETED", "exit_code": 0})
		fake.fail_on["job_process_ids"] = True
		code = monitor.run()
		self.assertEqual(code, rqm.EXIT_HOLD)
		self.assertTrue(monitor.job_terminated)

	# ---- D6 sampling deadline ----------------------------------------------------------------

	def test_d6_monotonic_deadline_cap(self):
		"""Injected monotonic clock reaching the deadline → wall-cap stop."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = build(self.tmp, fake, clock, timeout=2.0,
						inner={"status": "CRASHED", "exit_code": None},
						pace_min=5.0)
		code = monitor.run()
		self.assertIn("deadline", monitor.stop_reason or "")
		receipt = json.loads(
			(pathlib.Path(monitor.args.evidence_dir) / "monitor_status.json").read_text())
		self.assertEqual(receipt["disposition"], "ABORTED_WALL_CAP")
		self.assertEqual(code, rqm.EXIT_INNER_CAPPED)

	def test_d6_sample_gap_over_10s_is_hold(self):
		"""A >10s monotonic gap between samples is a stop, never a pass."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = build(self.tmp, fake, clock,
						inner={"status": "COMPLETED", "exit_code": 0},
						pace_min=12.0)
		code = monitor.run()
		self.assertIn("sample cadence", monitor.stop_reason or "")
		self.assertEqual(code, rqm.EXIT_HOLD)

	# ---- D7 clearance --------------------------------------------------------------------------

	def test_d7_clearance_requires_two_clear_snapshots(self):
		"""A member in the second census fails clearance → HOLD."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = build(self.tmp, fake, clock,
						inner={"status": "COMPLETED", "exit_code": 0})
		fake.on_launch = monitor.exit_root_on_launch(fake, 0)
		# pop order: monitor-loop reconcile, clearance snap0, clearance snap1
		fake.id_series = deque([[], [], [999_888]])
		code = monitor.run()
		receipt = json.loads(
			(pathlib.Path(monitor.args.evidence_dir) / "monitor_status.json").read_text())
		self.assertEqual(receipt["disposition"], "HOLD")
		self.assertEqual(code, rqm.EXIT_HOLD)
		clearance = read_jsonl(
			pathlib.Path(monitor.args.evidence_dir) / "clearance_censuses.jsonl")
		self.assertEqual(len(clearance), 2)
		self.assertTrue(clearance[0]["clear"])
		self.assertFalse(clearance[1]["clear"])

	def test_d7_clearance_spacing_floor_is_5s(self):
		"""Requested spacing <5s still yields >=5 monotonic seconds apart."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = build(self.tmp, fake, clock,
						inner={"status": "COMPLETED", "exit_code": 0},
						grant_extra={"clearance_spacing_seconds": 1})
		fake.on_launch = monitor.exit_root_on_launch(fake, 0)
		code = monitor.run()
		self.assertEqual(code, rqm.EXIT_COMPLETED)
		clearance = read_jsonl(
			pathlib.Path(monitor.args.evidence_dir) / "clearance_censuses.jsonl")
		self.assertEqual(len(clearance), 2)
		self.assertGreaterEqual(
			clearance[1]["mono"] - clearance[0]["mono"],
			rqm.MIN_CLEARANCE_SPACING_SECONDS)

	# ---- D8 receipt/output failure ----------------------------------------------------------------

	def test_d8_jsonl_write_failure_latches_hold(self):
		"""A samples-writer failure mid-run latches non-pass even with inner 0."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = build(self.tmp, fake, clock,
						inner={"status": "COMPLETED", "exit_code": 0},
						fail_writer_after={"samples": 1})
		fake.on_launch = monitor.exit_root_on_launch(fake, 0)
		code = monitor.run()
		self.assertTrue(monitor.evidence_failed)
		receipt = json.loads(
			(pathlib.Path(monitor.args.evidence_dir) / "monitor_status.json").read_text())
		self.assertTrue(receipt["evidence_write_failed"])
		self.assertEqual(receipt["disposition"], "HOLD")
		self.assertEqual(code, rqm.EXIT_HOLD)

	def test_d8_monitor_log_failure_latches_hold(self):
		"""monitor_log.txt write failure (F-2) latches evidence failure → non-pass."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = prep_state(build(self.tmp, fake, clock), fake, self.tmp)

		class DeadStream:
			def write(self, _):
				raise OSError("injected log failure")
			def flush(self):
				raise OSError("injected log failure")

		monitor._log_stream = DeadStream()
		monitor.log("info", "must latch")
		self.assertTrue(monitor.evidence_failed)
		self.assertIsNotNone(monitor.stop_reason)
		self.assertIn("monitor_log", monitor.stop_reason)
		self.assertTrue(any("monitor_log" in h for h in monitor.hold_reasons))
		self.assertEqual(
			monitor._disposition({"status": "COMPLETED", "exit_code": 0}, True),
			"HOLD")

	def test_d8_tool_hash_drift_is_hold(self):
		"""Post-run tool hash drift forces HOLD; contract also demands non-zero
		exit (reviewed tip computes `code` before the drift check — flagged)."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = build(self.tmp, fake, clock,
						inner={"status": "COMPLETED", "exit_code": 0})
		fake.on_launch = monitor.exit_root_on_launch(fake, 0)
		target = ROOT / "tools" / "ai" / "run_capped.py"
		real_hash = rqm.sha256_file
		monitor._bind_tool_hashes = lambda grant, root: monitor.tool_hashes_pre.update(
			{"tools/ai/run_capped.py": real_hash(target)})

		def drifting(path):
			if pathlib.Path(path).resolve() == target:
				return "0" * 64
			return real_hash(path)

		with unittest.mock.patch.object(rqm, "sha256_file", side_effect=drifting):
			code = monitor.run()
		receipt = json.loads(
			(pathlib.Path(monitor.args.evidence_dir) / "monitor_status.json").read_text())
		self.assertNotEqual(receipt["disposition"], "COMPLETED")
		self.assertTrue(receipt["tool_hash_drift"])
		# Contract: a HOLD receipt must never ride on exit code 0.
		self.assertNotEqual(code, rqm.EXIT_COMPLETED)

	def test_d8_exit_evidence_retained_on_write_failure(self):
		"""Failed exit-evidence write keeps the handle; exit stays unverified."""
		fake, clock = FakeAdapter(), FakeClock()
		monitor = prep_state(build(self.tmp, fake, clock), fake, self.tmp)
		key = fake.spawn_member((4242, 1), notify=False)
		entry = monitor._register_member(key[0], "membership_query",
									   fake.process_census())
		monitor.writers["ledger"] = FailingWriter(monitor.writers["ledger"], after=0)
		fake.procs[key].exited = True
		fake.procs[key].exit_code = 0
		monitor._verify_exit(entry)
		self.assertFalse(entry.exit_verified)
		self.assertIn(entry.key(), monitor.handles)
		self.assertTrue(monitor.evidence_failed)


if __name__ == "__main__":
	unittest.main()
