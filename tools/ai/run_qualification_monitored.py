#!/usr/bin/env python3
"""JIT-owned Windows qualification supervisor for a single capped command.

Implements the reviewed contract in
``DESIGN_2026-10-14_qualification_owned_monitor_sol.md`` (fleet doc, SHA-256
3285df5c…d53). The supervisor launches one exact ``run_capped`` argv inside a
private Job Object before resume, keeps a (PID, creation-FILETIME) lifetime
ledger, samples owned resources and host memory on a bounded cadence, writes
append-only evidence, and proves member clearance with two spaced snapshots.
It grants no authority: the grant file is operator-supplied intent, and
``execution_approved``-style fields are never emitted true. Windows 10+ only
(job-object containment via suspended spawn + AssignProcessToJobObject).
run_capped.py itself is unchanged — this supervisor wraps it without
modification.

Exit codes: 0 inner COMPLETED/0 + evidence complete + clearance proved;
2 preflight refusal (incl. containment-launch failure before resume);
3 HOLD (identity/accounting/query/sample/clearance gap, interrupt);
4 inner non-zero or crashed; 5 wall-cap abort (inner or outer);
6 monitor-internal or receipt-write failure.
"""

from __future__ import annotations

import argparse
import ctypes
from ctypes import wintypes
import hashlib
import json
import math
import os
from pathlib import Path
import subprocess
import sys
import threading
import time
from datetime import datetime, timezone
from typing import Any, Sequence

DESIGN_DOC = "DESIGN_2026-10-14_qualification_owned_monitor_sol.md"
DESIGN_SHA256 = "3285df5c7ef34746a18dda50241c4856be86205215e4ad23695a16ccf80b7d53"

# --- approved policy constants (design §2; fixed for qualification) ----------
MIN_ADMISSION_AVAIL_RAM_BYTES = 9.34 * (1 << 30)
MIN_ADMISSION_FREE_DISK_BYTES = 20 * (1 << 30)
STOP_AVAIL_RAM_BYTES = 4 * (1 << 30)
MAX_OWNED_PRIVATE_BYTES = 8 * (1 << 30)
SAMPLE_INTERVAL_SECONDS = 5.0
MAX_SAMPLE_GAP_SECONDS = 10.0
MIN_CLEARANCE_SPACING_SECONDS = 5.0
ROOT_EXIT_DRAIN_SECONDS = 2.0
# run_capped.py (pinned alongside this supervisor) allows up to 15 seconds for
# taskkill /T /F and then up to 5 seconds to reap its root after the child cap.
# The monitor may then need one maximum sampling gap to observe root exit and
# the bounded root-exit drain before it can begin the separately bounded
# clearance procedure. A shorter inner cap therefore needs this much deadline
# headroom; the independent outer run_capped cap still bounds the full monitor,
# clearance, and receipt path.
RUN_CAPPED_WINDOWS_TREE_KILL_SECONDS = 15.0
RUN_CAPPED_WINDOWS_ROOT_REAP_SECONDS = 5.0
CHILD_TIMEOUT_CLEANUP_HEADROOM_SECONDS = (
    RUN_CAPPED_WINDOWS_TREE_KILL_SECONDS
    + RUN_CAPPED_WINDOWS_ROOT_REAP_SECONDS
    + MAX_SAMPLE_GAP_SECONDS
    + ROOT_EXIT_DRAIN_SECONDS
)
DEFAULT_TIMEOUT_SECONDS = 900.0
JOB_ID_LIST_CAPACITY = 512

EXIT_COMPLETED = 0
EXIT_REFUSAL = 2
EXIT_HOLD = 3
EXIT_INNER_FAILURE = 4
EXIT_INNER_CAPPED = 5
EXIT_MONITOR_ERROR = 6

PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
PROCESS_QUERY_INFORMATION = 0x0400
PROCESS_VM_READ = 0x0010
SYNCHRONIZE = 0x00100000
MEMBER_QUERY_ACCESS = (
    PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_QUERY_INFORMATION | PROCESS_VM_READ | SYNCHRONIZE
)

JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000
JOB_OBJECT_LIMIT_BREAKAWAY_OK = 0x00000800
JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK = 0x00001000
JOB_OBJECT_FORBIDDEN_LIMITS = JOB_OBJECT_LIMIT_BREAKAWAY_OK | JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK

JobObjectBasicAccountingInformation = 1
JobObjectBasicProcessIdList = 3
JobObjectAssociateCompletionPortInformation = 7
JobObjectExtendedLimitInformation = 9

JOB_OBJECT_MSG_NEW_PROCESS = 6
JOB_OBJECT_MSG_EXIT_PROCESS = 7
JOB_OBJECT_MSG_ABNORMAL_EXIT_PROCESS = 8
JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO = 4

PROC_THREAD_ATTRIBUTE_INPUT = 0x00020000
PROC_THREAD_ATTRIBUTE_ADDITIVE = 0x00040000
PROC_THREAD_ATTRIBUTE_HANDLE_LIST = 2 | PROC_THREAD_ATTRIBUTE_INPUT
PROC_THREAD_ATTRIBUTE_JOB_LIST = 13 | PROC_THREAD_ATTRIBUTE_INPUT | PROC_THREAD_ATTRIBUTE_ADDITIVE

CREATE_SUSPENDED = 0x00000004
EXTENDED_STARTUPINFO_PRESENT = 0x00080000
STARTF_USESTDHANDLES = 0x00000100
GENERIC_READ = 0x80000000
GENERIC_WRITE = 0x40000000
FILE_SHARE_READ = 0x00000001
FILE_SHARE_WRITE = 0x00000002
CREATE_ALWAYS = 0x00000002
OPEN_EXISTING = 0x00000003
FILE_ATTRIBUTE_NORMAL = 0x00000080
WAIT_OBJECT_0 = 0x00000000
INVALID_HANDLE_VALUE = ctypes.c_void_p(-1).value
STILL_ACTIVE = 259

_HANDLE = ctypes.c_void_p


class _IO_COUNTERS(ctypes.Structure):
    _fields_ = [
        ("ReadOperationCount", ctypes.c_uint64),
        ("WriteOperationCount", ctypes.c_uint64),
        ("OtherOperationCount", ctypes.c_uint64),
        ("ReadTransferCount", ctypes.c_uint64),
        ("WriteTransferCount", ctypes.c_uint64),
        ("OtherTransferCount", ctypes.c_uint64),
    ]


class _JOBOBJECT_BASIC_LIMIT_INFORMATION(ctypes.Structure):
    _fields_ = [
        ("PerProcessUserTimeLimit", wintypes.LARGE_INTEGER),
        ("PerJobUserTimeLimit", wintypes.LARGE_INTEGER),
        ("LimitFlags", wintypes.DWORD),
        ("MinimumWorkingSetSize", ctypes.c_size_t),
        ("MaximumWorkingSetSize", ctypes.c_size_t),
        ("ActiveProcessLimit", wintypes.DWORD),
        ("Affinity", ctypes.c_size_t),
        ("PriorityClass", wintypes.DWORD),
        ("SchedulingClass", wintypes.DWORD),
    ]


class _JOBOBJECT_EXTENDED_LIMIT_INFORMATION(ctypes.Structure):
    _fields_ = [
        ("BasicLimitInformation", _JOBOBJECT_BASIC_LIMIT_INFORMATION),
        ("IoInfo", _IO_COUNTERS),
        ("ProcessMemoryLimit", ctypes.c_size_t),
        ("JobMemoryLimit", ctypes.c_size_t),
        ("PeakProcessMemoryUsed", ctypes.c_size_t),
        ("PeakJobMemoryUsed", ctypes.c_size_t),
    ]


class _JOBOBJECT_BASIC_ACCOUNTING_INFORMATION(ctypes.Structure):
    _fields_ = [
        ("TotalUserTime", wintypes.LARGE_INTEGER),
        ("TotalKernelTime", wintypes.LARGE_INTEGER),
        ("ThisPeriodTotalUserTime", wintypes.LARGE_INTEGER),
        ("ThisPeriodTotalKernelTime", wintypes.LARGE_INTEGER),
        ("TotalPageFaultCount", wintypes.DWORD),
        ("TotalProcesses", wintypes.DWORD),
        ("ActiveProcesses", wintypes.DWORD),
        ("TotalTerminatedProcesses", wintypes.DWORD),
    ]


class _JOBOBJECT_ASSOCIATE_COMPLETION_PORT(ctypes.Structure):
    _fields_ = [("CompletionKey", ctypes.c_size_t), ("CompletionPort", _HANDLE)]


class _MEMORYSTATUSEX(ctypes.Structure):
    _fields_ = [
        ("dwLength", wintypes.DWORD),
        ("dwMemoryLoad", wintypes.DWORD),
        ("ullTotalPhys", ctypes.c_uint64),
        ("ullAvailPhys", ctypes.c_uint64),
        ("ullTotalPageFile", ctypes.c_uint64),
        ("ullAvailPageFile", ctypes.c_uint64),
        ("ullTotalVirtual", ctypes.c_uint64),
        ("ullAvailVirtual", ctypes.c_uint64),
        ("ullAvailExtendedVirtual", ctypes.c_uint64),
    ]


class _PROCESS_MEMORY_COUNTERS_EX(ctypes.Structure):
    _fields_ = [
        ("cb", wintypes.DWORD),
        ("PageFaultCount", wintypes.DWORD),
        ("PeakWorkingSetSize", ctypes.c_size_t),
        ("WorkingSetSize", ctypes.c_size_t),
        ("QuotaPeakPagedPoolUsage", ctypes.c_size_t),
        ("QuotaPagedPoolUsage", ctypes.c_size_t),
        ("QuotaPeakNonPagedPoolUsage", ctypes.c_size_t),
        ("QuotaNonPagedPoolUsage", ctypes.c_size_t),
        ("PagefileUsage", ctypes.c_size_t),
        ("PeakPagefileUsage", ctypes.c_size_t),
        ("PrivateUsage", ctypes.c_size_t),
    ]


class _SECURITY_ATTRIBUTES(ctypes.Structure):
    _fields_ = [
        ("nLength", wintypes.DWORD),
        ("lpSecurityDescriptor", ctypes.c_void_p),
        ("bInheritHandle", wintypes.BOOL),
    ]


class _STARTUPINFOW(ctypes.Structure):
    _fields_ = [
        ("cb", wintypes.DWORD),
        ("lpReserved", wintypes.LPWSTR),
        ("lpDesktop", wintypes.LPWSTR),
        ("lpTitle", wintypes.LPWSTR),
        ("dwX", wintypes.DWORD),
        ("dwY", wintypes.DWORD),
        ("dwXSize", wintypes.DWORD),
        ("dwYSize", wintypes.DWORD),
        ("dwXCountChars", wintypes.DWORD),
        ("dwYCountChars", wintypes.DWORD),
        ("dwFillAttribute", wintypes.DWORD),
        ("dwFlags", wintypes.DWORD),
        ("wShowWindow", wintypes.WORD),
        ("cbReserved2", wintypes.WORD),
        ("lpReserved2", ctypes.c_void_p),
        ("hStdInput", _HANDLE),
        ("hStdOutput", _HANDLE),
        ("hStdError", _HANDLE),
    ]


class _STARTUPINFOEXW(ctypes.Structure):
    _fields_ = [("StartupInfo", _STARTUPINFOW), ("lpAttributeList", ctypes.c_void_p)]


class _PROCESS_INFORMATION(ctypes.Structure):
    _fields_ = [
        ("hProcess", _HANDLE),
        ("hThread", _HANDLE),
        ("dwProcessId", wintypes.DWORD),
        ("dwThreadId", wintypes.DWORD),
    ]


class _PROCESSENTRY32W(ctypes.Structure):
    _fields_ = [
        ("dwSize", wintypes.DWORD),
        ("cntUsage", wintypes.DWORD),
        ("th32ProcessID", wintypes.DWORD),
        ("th32DefaultHeapID", ctypes.c_size_t),
        ("th32ModuleID", wintypes.DWORD),
        ("cntThreads", wintypes.DWORD),
        ("th32ParentProcessID", wintypes.DWORD),
        ("pcPriClassBase", ctypes.c_long),
        ("dwFlags", wintypes.DWORD),
        ("szExeFile", wintypes.WCHAR * 260),
    ]


class AdapterError(OSError):
    """A Win32 call failed; the message names the operation for receipts."""


class Win32Adapter:
    """Reviewed 64-bit ctypes binding surface. Every kernel interaction funnels
    through this class so focused validation can inject a scripted double."""

    def __init__(self) -> None:
        self.kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
        self.psapi = ctypes.WinDLL("psapi", use_last_error=True)
        k = self.kernel32
        k.CreateJobObjectW.argtypes = [ctypes.c_void_p, wintypes.LPCWSTR]
        k.CreateJobObjectW.restype = _HANDLE
        k.SetInformationJobObject.argtypes = [_HANDLE, ctypes.c_int, ctypes.c_void_p, wintypes.DWORD]
        k.SetInformationJobObject.restype = wintypes.BOOL
        k.QueryInformationJobObject.argtypes = [
            _HANDLE, ctypes.c_int, ctypes.c_void_p, wintypes.DWORD,
            ctypes.POINTER(wintypes.DWORD)]
        k.QueryInformationJobObject.restype = wintypes.BOOL
        k.CreateIoCompletionPort.argtypes = [_HANDLE, _HANDLE, ctypes.c_size_t, wintypes.DWORD]
        k.CreateIoCompletionPort.restype = _HANDLE
        k.GetQueuedCompletionStatus.argtypes = [
            _HANDLE, ctypes.POINTER(wintypes.DWORD), ctypes.POINTER(ctypes.c_size_t),
            ctypes.POINTER(ctypes.c_size_t), wintypes.DWORD]
        k.GetQueuedCompletionStatus.restype = wintypes.BOOL
        k.CreateProcessW.argtypes = [
            wintypes.LPCWSTR, wintypes.LPWSTR, ctypes.c_void_p, ctypes.c_void_p,
            wintypes.BOOL, wintypes.DWORD, ctypes.c_void_p, wintypes.LPCWSTR,
            ctypes.POINTER(_STARTUPINFOEXW), ctypes.POINTER(_PROCESS_INFORMATION)]
        k.CreateProcessW.restype = wintypes.BOOL
        k.AssignProcessToJobObject.argtypes = [_HANDLE, _HANDLE]
        k.AssignProcessToJobObject.restype = wintypes.BOOL
        k.InitializeProcThreadAttributeList.argtypes = [
            ctypes.c_void_p, wintypes.DWORD, wintypes.DWORD, ctypes.POINTER(ctypes.c_size_t)]
        k.InitializeProcThreadAttributeList.restype = wintypes.BOOL
        k.UpdateProcThreadAttribute.argtypes = [
            ctypes.c_void_p, wintypes.DWORD, ctypes.c_size_t,
            ctypes.c_void_p, ctypes.c_size_t, ctypes.c_void_p, ctypes.c_void_p]
        k.UpdateProcThreadAttribute.restype = wintypes.BOOL
        k.DeleteProcThreadAttributeList.argtypes = [ctypes.c_void_p]
        k.ResumeThread.argtypes = [_HANDLE]
        k.ResumeThread.restype = wintypes.DWORD
        k.IsProcessInJob.argtypes = [_HANDLE, _HANDLE, ctypes.POINTER(wintypes.BOOL)]
        k.IsProcessInJob.restype = wintypes.BOOL
        k.OpenProcess.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
        k.OpenProcess.restype = _HANDLE
        k.GetProcessTimes.argtypes = [
            _HANDLE, ctypes.POINTER(wintypes.FILETIME), ctypes.POINTER(wintypes.FILETIME),
            ctypes.POINTER(wintypes.FILETIME), ctypes.POINTER(wintypes.FILETIME)]
        k.GetProcessTimes.restype = wintypes.BOOL
        k.GetExitCodeProcess.argtypes = [_HANDLE, ctypes.POINTER(wintypes.DWORD)]
        k.GetExitCodeProcess.restype = wintypes.BOOL
        k.WaitForSingleObject.argtypes = [_HANDLE, wintypes.DWORD]
        k.WaitForSingleObject.restype = wintypes.DWORD
        k.TerminateJobObject.argtypes = [_HANDLE, wintypes.UINT]
        k.TerminateJobObject.restype = wintypes.BOOL
        k.TerminateProcess.argtypes = [_HANDLE, wintypes.UINT]
        k.TerminateProcess.restype = wintypes.BOOL
        k.GetCurrentProcess.restype = _HANDLE
        k.GetCurrentProcessId.restype = wintypes.DWORD
        k.CloseHandle.argtypes = [_HANDLE]
        k.CloseHandle.restype = wintypes.BOOL
        k.GlobalMemoryStatusEx.argtypes = [ctypes.POINTER(_MEMORYSTATUSEX)]
        k.GlobalMemoryStatusEx.restype = wintypes.BOOL
        k.GetDiskFreeSpaceExW.argtypes = [
            wintypes.LPCWSTR, ctypes.POINTER(ctypes.c_uint64),
            ctypes.POINTER(ctypes.c_uint64), ctypes.POINTER(ctypes.c_uint64)]
        k.GetDiskFreeSpaceExW.restype = wintypes.BOOL
        k.CreateToolhelp32Snapshot.argtypes = [wintypes.DWORD, wintypes.DWORD]
        k.CreateToolhelp32Snapshot.restype = _HANDLE
        k.Process32FirstW.argtypes = [_HANDLE, ctypes.POINTER(_PROCESSENTRY32W)]
        k.Process32FirstW.restype = wintypes.BOOL
        k.Process32NextW.argtypes = [_HANDLE, ctypes.POINTER(_PROCESSENTRY32W)]
        k.Process32NextW.restype = wintypes.BOOL
        k.CreateFileW.argtypes = [
            wintypes.LPCWSTR, wintypes.DWORD, wintypes.DWORD,
            ctypes.POINTER(_SECURITY_ATTRIBUTES), wintypes.DWORD, wintypes.DWORD, _HANDLE]
        k.CreateFileW.restype = _HANDLE
        self.psapi.GetProcessMemoryInfo.argtypes = [
            _HANDLE, ctypes.POINTER(_PROCESS_MEMORY_COUNTERS_EX), wintypes.DWORD]
        self.psapi.GetProcessMemoryInfo.restype = wintypes.BOOL

    @staticmethod
    def _check(ok: bool, op: str) -> None:
        if not ok:
            raise AdapterError(f"{op} failed: win32 error {ctypes.get_last_error()}")

    def supervisor_identity(self) -> tuple[int, int, bool]:
        """(pid, creation filetime, in-job) of this supervisor — the excluded parent."""
        current = self.kernel32.GetCurrentProcess()
        in_job = wintypes.BOOL(False)
        self._check(
            self.kernel32.IsProcessInJob(current, None, ctypes.byref(in_job)),
            "IsProcessInJob(self)")
        creation = self.process_creation_time(current)
        return int(self.kernel32.GetCurrentProcessId()), creation, bool(in_job.value)

    def create_job(self, name: str) -> _HANDLE:
        handle = self.kernel32.CreateJobObjectW(None, name)
        if not handle:
            raise AdapterError(f"CreateJobObjectW failed: win32 error {ctypes.get_last_error()}")
        info = _JOBOBJECT_EXTENDED_LIMIT_INFORMATION()
        info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
        self._check(
            self.kernel32.SetInformationJobObject(
                handle, JobObjectExtendedLimitInformation, ctypes.byref(info),
                ctypes.sizeof(info)),
            "SetInformationJobObject(KILL_ON_JOB_CLOSE)")
        return handle

    def job_limit_flags(self, job: _HANDLE) -> int:
        info = _JOBOBJECT_EXTENDED_LIMIT_INFORMATION()
        self._check(
            self.kernel32.QueryInformationJobObject(
                job, JobObjectExtendedLimitInformation, ctypes.byref(info),
                ctypes.sizeof(info), None),
            "QueryInformationJobObject(limits)")
        return int(info.BasicLimitInformation.LimitFlags)

    def create_completion_port(self) -> _HANDLE:
        port = self.kernel32.CreateIoCompletionPort(INVALID_HANDLE_VALUE, None, 0, 0)
        if not port:
            raise AdapterError(
                f"CreateIoCompletionPort failed: win32 error {ctypes.get_last_error()}")
        return port

    def associate_completion_port(self, job: _HANDLE, port: _HANDLE, key: int) -> None:
        assoc = _JOBOBJECT_ASSOCIATE_COMPLETION_PORT(ctypes.c_size_t(key), port)
        self._check(
            self.kernel32.SetInformationJobObject(
                job, JobObjectAssociateCompletionPortInformation,
                ctypes.byref(assoc), ctypes.sizeof(assoc)),
            "SetInformationJobObject(completion-port)")

    def get_completion(self, port: _HANDLE, timeout_ms: int) -> tuple[int, int] | None:
        """(message_id, payload-pid) for a job notification, or None on timeout."""
        nbytes = wintypes.DWORD(0)
        key = ctypes.c_size_t(0)
        overlapped = ctypes.c_size_t(0)
        ok = self.kernel32.GetQueuedCompletionStatus(
            port, ctypes.byref(nbytes), ctypes.byref(key), ctypes.byref(overlapped), timeout_ms)
        if not ok and overlapped.value == 0:
            return None
        return int(nbytes.value), int(overlapped.value)

    def launch_suspended_in_job(
        self, cmdline: str, job: _HANDLE, cwd: str | None,
        std_handles: tuple[_HANDLE, _HANDLE, _HANDLE],
    ) -> tuple[_HANDLE, _HANDLE, int]:
        """CreateProcessW suspended, then AssignProcessToJobObject while the main
        thread is still suspended: the child executes no user code before the
        assignment, so the containment guarantee matches the §3 job-list design.
        The JOB_LIST startup attribute is dropped because this host rejects
        PROC_THREAD_ATTRIBUTE_JOB_LIST with win32 error 50 in both contexts."""
        size = ctypes.c_size_t(0)
        self.kernel32.InitializeProcThreadAttributeList(None, 1, 0, ctypes.byref(size))
        if size.value == 0:
            raise AdapterError("InitializeProcThreadAttributeList reported zero size")
        attr_buf = ctypes.create_string_buffer(size.value)
        try:
            self._check(
                self.kernel32.InitializeProcThreadAttributeList(
                    attr_buf, 1, 0, ctypes.byref(size)),
                "InitializeProcThreadAttributeList")
            handle_array = (_HANDLE * 3)(*std_handles)
            self._check(
                self.kernel32.UpdateProcThreadAttribute(
                    attr_buf, 0, PROC_THREAD_ATTRIBUTE_HANDLE_LIST,
                    handle_array, ctypes.sizeof(handle_array), None, None),
                "UpdateProcThreadAttribute(HANDLE_LIST)")
            si = _STARTUPINFOEXW()
            si.StartupInfo.cb = ctypes.sizeof(_STARTUPINFOEXW)
            si.StartupInfo.dwFlags = STARTF_USESTDHANDLES
            (si.StartupInfo.hStdInput, si.StartupInfo.hStdOutput,
             si.StartupInfo.hStdError) = std_handles
            si.lpAttributeList = ctypes.addressof(attr_buf)
            pi = _PROCESS_INFORMATION()
            mutable_cmdline = ctypes.create_unicode_buffer(cmdline)
            self._check(
                self.kernel32.CreateProcessW(
                    None, mutable_cmdline, None, None, True,
                    CREATE_SUSPENDED | EXTENDED_STARTUPINFO_PRESENT,
                    None, cwd, ctypes.byref(si), ctypes.byref(pi)),
                "CreateProcessW(suspended)")
        finally:
            self.kernel32.DeleteProcThreadAttributeList(attr_buf)
        try:
            self._check(
                self.kernel32.AssignProcessToJobObject(job, pi.hProcess),
                "AssignProcessToJobObject(root)")
        except AdapterError:
            self.kernel32.TerminateProcess(pi.hProcess, 1)
            self.kernel32.CloseHandle(pi.hThread)
            self.kernel32.CloseHandle(pi.hProcess)
            raise
        return pi.hProcess, pi.hThread, int(pi.dwProcessId)

    def resume_thread(self, thread: _HANDLE) -> None:
        if self.kernel32.ResumeThread(thread) == 0xFFFFFFFF:
            raise AdapterError(f"ResumeThread failed: win32 error {ctypes.get_last_error()}")

    def is_process_in_job(self, process: _HANDLE, job: _HANDLE) -> bool:
        result = wintypes.BOOL(False)
        self._check(
            self.kernel32.IsProcessInJob(process, job, ctypes.byref(result)),
            "IsProcessInJob")
        return bool(result.value)

    def open_process(self, pid: int, access: int = MEMBER_QUERY_ACCESS) -> _HANDLE | None:
        return self.kernel32.OpenProcess(access, False, pid)

    def process_creation_time(self, process: _HANDLE) -> int:
        ctime, etime, ktime, utime = (
            wintypes.FILETIME(), wintypes.FILETIME(),
            wintypes.FILETIME(), wintypes.FILETIME())
        self._check(
            self.kernel32.GetProcessTimes(
                process, ctypes.byref(ctime), ctypes.byref(etime),
                ctypes.byref(ktime), ctypes.byref(utime)),
            "GetProcessTimes")
        return (int(ctime.dwHighDateTime) << 32) | int(ctime.dwLowDateTime)

    def process_exit_state(self, process: _HANDLE) -> tuple[bool, int | None]:
        code = wintypes.DWORD(0)
        self._check(
            self.kernel32.GetExitCodeProcess(process, ctypes.byref(code)),
            "GetExitCodeProcess")
        if int(code.value) == STILL_ACTIVE:
            return False, None
        return True, int(code.value)

    def wait_process(self, process: _HANDLE, timeout_ms: int) -> bool:
        return self.kernel32.WaitForSingleObject(process, timeout_ms) == WAIT_OBJECT_0

    def terminate_job(self, job: _HANDLE, exit_code: int = 0xFFFF) -> None:
        self._check(self.kernel32.TerminateJobObject(job, exit_code), "TerminateJobObject")

    def terminate_process(self, process: _HANDLE, exit_code: int = 0xFFFE) -> None:
        self._check(self.kernel32.TerminateProcess(process, exit_code), "TerminateProcess")

    def job_process_ids(self, job: _HANDLE) -> list[int]:
        capacity = JOB_ID_LIST_CAPACITY
        while True:
            buf_size = 8 + capacity * ctypes.sizeof(ctypes.c_size_t)
            buf = ctypes.create_string_buffer(buf_size)
            returned = wintypes.DWORD(0)
            self._check(
                self.kernel32.QueryInformationJobObject(
                    job, JobObjectBasicProcessIdList, buf, buf_size, ctypes.byref(returned)),
                "QueryInformationJobObject(id-list)")
            count = int.from_bytes(buf.raw[4:8], "little")
            if count <= capacity:
                return [
                    int.from_bytes(buf.raw[8 + i * 8:16 + i * 8], "little")
                    for i in range(count)]
            capacity = count * 2
            if capacity > 65536:
                raise AdapterError("job id-list exceeds sanity bound 65536")

    def job_accounting(self, job: _HANDLE) -> dict[str, int]:
        info = _JOBOBJECT_BASIC_ACCOUNTING_INFORMATION()
        returned = wintypes.DWORD(0)
        self._check(
            self.kernel32.QueryInformationJobObject(
                job, JobObjectBasicAccountingInformation, ctypes.byref(info),
                ctypes.sizeof(info), ctypes.byref(returned)),
            "QueryInformationJobObject(accounting)")
        return {
            "TotalProcesses": int(info.TotalProcesses),
            "ActiveProcesses": int(info.ActiveProcesses),
            "TotalTerminatedProcesses": int(info.TotalTerminatedProcesses),
            "TotalPageFaultCount": int(info.TotalPageFaultCount),
        }

    def private_bytes(self, process: _HANDLE) -> int:
        counters = _PROCESS_MEMORY_COUNTERS_EX()
        counters.cb = ctypes.sizeof(counters)
        self._check(
            self.psapi.GetProcessMemoryInfo(
                process, ctypes.byref(counters), ctypes.sizeof(counters)),
            "GetProcessMemoryInfo")
        return int(counters.PrivateUsage)

    def available_ram_bytes(self) -> tuple[int, int]:
        state = _MEMORYSTATUSEX()
        state.dwLength = ctypes.sizeof(state)
        self._check(
            self.kernel32.GlobalMemoryStatusEx(ctypes.byref(state)), "GlobalMemoryStatusEx")
        return int(state.ullAvailPhys), int(state.ullTotalPhys)

    def free_disk_bytes(self, path: str) -> int:
        anchor = Path(path).resolve().anchor
        avail, total, free = ctypes.c_uint64(0), ctypes.c_uint64(0), ctypes.c_uint64(0)
        self._check(
            self.kernel32.GetDiskFreeSpaceExW(
                anchor, ctypes.byref(avail), ctypes.byref(total), ctypes.byref(free)),
            "GetDiskFreeSpaceExW")
        return int(avail.value)

    def open_inheritable_file(self, path: Path, for_write: bool) -> _HANDLE:
        sa = _SECURITY_ATTRIBUTES()
        sa.nLength = ctypes.sizeof(sa)
        sa.bInheritHandle = True
        handle = self.kernel32.CreateFileW(
            str(path),
            GENERIC_WRITE if for_write else GENERIC_READ,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            ctypes.byref(sa),
            CREATE_ALWAYS if for_write else OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL,
            None)
        if not handle or handle == INVALID_HANDLE_VALUE:
            raise AdapterError(
                f"CreateFileW({path}) failed: win32 error {ctypes.get_last_error()}")
        return handle

    def process_census(self) -> dict[int, int]:
        """pid -> parent_pid for every visible process (read-only census)."""
        snapshot = self.kernel32.CreateToolhelp32Snapshot(0x00000002, 0)  # TH32CS_SNAPPROCESS
        if not snapshot or snapshot == INVALID_HANDLE_VALUE:
            raise AdapterError(
                f"CreateToolhelp32Snapshot failed: win32 error {ctypes.get_last_error()}")
        try:
            entry = _PROCESSENTRY32W()
            entry.dwSize = ctypes.sizeof(entry)
            result: dict[int, int] = {}
            if self.kernel32.Process32FirstW(snapshot, ctypes.byref(entry)):
                while True:
                    result[int(entry.th32ProcessID)] = int(entry.th32ParentProcessID)
                    if not self.kernel32.Process32NextW(snapshot, ctypes.byref(entry)):
                        break
            return result
        finally:
            self.kernel32.CloseHandle(snapshot)

    def close_handle(self, handle: _HANDLE | None) -> None:
        if handle:
            self.kernel32.CloseHandle(handle)


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="milliseconds").replace("+00:00", "Z")


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1 << 20), b""):
            digest.update(chunk)
    return digest.hexdigest()


def filetime_to_iso(filetime: int) -> str:
    return datetime.fromtimestamp(
        (filetime - 116444736000000000) / 10_000_000, tz=timezone.utc,
    ).isoformat(timespec="milliseconds").replace("+00:00", "Z")


class AppendOnlyWriter:
    """One writer per file, created fresh in the absent-unique evidence dir,
    flush+fsync per record. A torn final line is incomplete evidence —
    never repaired in place."""

    def __init__(self, path: Path) -> None:
        self.path = path
        self._stream = path.open("x", encoding="utf-8", newline="\n")
        self.records = 0

    def write(self, record: dict[str, Any]) -> None:
        self._stream.write(json.dumps(record, sort_keys=True, separators=(",", ":")))
        self._stream.write("\n")
        self._stream.flush()
        os.fsync(self._stream.fileno())
        self.records += 1

    def close(self) -> None:
        self._stream.close()


class LedgerEntry:
    __slots__ = (
        "pid", "creation", "is_root", "parent", "first_seen_utc", "source",
        "exited", "exit_utc", "exit_code", "exit_verified", "private_bytes",
        "query_error", "linkage",
    )

    def __init__(self, pid: int, creation: int | None, is_root: bool,
                 parent: dict[str, Any] | None, source: str) -> None:
        self.pid = pid
        self.creation = creation
        self.is_root = is_root
        self.parent = parent
        self.first_seen_utc = utc_now()
        self.source = source
        self.exited = False
        self.exit_utc: str | None = None
        self.exit_code: int | None = None
        self.exit_verified = False
        self.private_bytes: int | None = None
        self.query_error: str | None = None
        self.linkage = "defended" if parent is not None else ("root" if is_root else "unknown")

    def key(self) -> str:
        return f"{self.pid}:{self.creation}" if self.creation is not None else f"{self.pid}:gap"

    def to_record(self) -> dict[str, Any]:
        return {
            "pid": self.pid,
            "creation_filetime": self.creation,
            "creation_utc": filetime_to_iso(self.creation) if self.creation is not None else None,
            "ledger_key": self.key(),
            "is_root": self.is_root,
            "parent": self.parent,
            "first_seen_utc": self.first_seen_utc,
            "source": self.source,
            "exited": self.exited,
            "exit_utc": self.exit_utc,
            "exit_code": self.exit_code,
            "exit_verified": self.exit_verified,
            "private_bytes_last": self.private_bytes,
            "linkage": self.linkage,
            "query_error": self.query_error,
        }


class Monitor:
    """One grant-declared capped command under job containment (design §2-§5)."""

    def __init__(self, args: argparse.Namespace, adapter: Win32Adapter | None = None,
                 monotonic=time.monotonic) -> None:
        self.args = args
        self.win32 = adapter if adapter is not None else Win32Adapter()
        self.mono = monotonic
        self.started_mono = self.mono()
        self.started_utc = utc_now()
        self.deadline_mono = self.started_mono + args.timeout
        self.hold_reasons: list[str] = []
        self.stop_reason: str | None = None
        # Permanent non-pass latch for append-only evidence write failures;
        # once set it is never cleared, independent of stop_reason strings.
        self.evidence_failed = False
        self.ledger: dict[str, LedgerEntry] = {}
        self.handles: dict[str, _HANDLE] = {}          # keyed by ledger key, never pid
        self.identity_gaps: list[dict[str, Any]] = []
        self.accounting: dict[str, int] | None = None
        self.job: _HANDLE | None = None
        self.port: _HANDLE | None = None
        self.root_handle: _HANDLE | None = None
        self.root_thread: _HANDLE | None = None
        self.root_exit_code: int | None = None
        self.root_exited = False
        self.inner_status: dict[str, Any] | None = None
        self.resumed = False
        self.stopped = False
        self.job_sealed = False
        self.last_sample_mono: float | None = None
        self.evidence_dir: Path | None = None
        self.evidence: dict[str, Path] = {}
        self.writers: dict[str, AppendOnlyWriter] = {}
        self._log_stream = None
        self._notify_queue: list[tuple[int, int]] = []
        self._notify_lock = threading.Lock()
        self._event_stop = threading.Event()
        self._pace = threading.Event()
        # Dedicated clearance-spacing timer; nothing may ever set() it.
        self._spacing = threading.Event()
        self.tool_hashes_pre: dict[str, str] = {}
        self.tool_hashes_post: dict[str, str] = {}
        self._child_handles: list[_HANDLE] = []
        self._supervisor_pid = 0
        self._supervisor_creation = 0
        self._supervisor_in_job = False

    # -- evidence ---------------------------------------------------------------

    def _open_evidence(self, evidence_dir: Path) -> None:
        self.evidence_dir = evidence_dir
        for key, name in (("samples", "resource_samples.jsonl"),
                          ("ledger", "process_ledger.jsonl"),
                          ("clearance", "clearance_censuses.jsonl")):
            self.evidence[key] = evidence_dir / name
            self.writers[key] = AppendOnlyWriter(self.evidence[key])
        self.evidence["log"] = evidence_dir / "monitor_log.txt"
        self._log_stream = self.evidence["log"].open("x", encoding="utf-8", newline="\n")

    def log(self, level: str, message: str, **fields: Any) -> None:
        line = json.dumps(
            {"utc": utc_now(), "level": level, "msg": message, **fields}, sort_keys=True)
        try:
            if self._log_stream is not None:
                self._log_stream.write(line + "\n")
                self._log_stream.flush()
        except (OSError, ValueError) as error:
            # monitor_log.txt is append-only evidence: a failed append latches
            # permanent non-pass and requests cleanup. Set stop_reason directly —
            # never _trigger_stop(), which calls log() and would recurse on a
            # broken stream.
            if self.stop_reason is None:
                self.stop_reason = f"monitor_log write/flush failed: {error}"
            if not self.evidence_failed:
                self.hold_reasons.append(f"monitor_log write/flush failed: {error}")
                self.evidence_failed = True
            self._pace.set()
        try:
            print(line, file=sys.stderr if level in ("error", "hold") else sys.stdout,
                  flush=True)
        except OSError:
            pass  # supervisor stdout is captured by the invoker; loss is external

    def _record(self, key: str, record: dict[str, Any]) -> bool:
        """Append one evidence record; False on write/fsync failure. A failure
        latches permanent non-pass state and requests owned cleanup (§5)."""
        try:
            self.writers[key].write(record)
        except (OSError, ValueError) as error:
            if not self.evidence_failed:
                self.evidence_failed = True
                self.hold_reasons.append(
                    f"append-only evidence write failed on {key}: {error}")
            self._trigger_stop(f"receipt-write failure on {key}: {error}")
            return False
        return True

    # -- ledger ------------------------------------------------------------------

    def _parent_evidence(self, pid: int, census: dict[int, int] | None) -> dict[str, Any] | None:
        """Defensible parent linkage for a new member (design §4)."""
        if census is None:
            return None
        parent_pid = census.get(pid)
        if parent_pid is None:
            return None
        if parent_pid == self._supervisor_pid:
            return {"pid": parent_pid, "creation_filetime": self._supervisor_creation,
                    "link": "supervisor"}
        for entry in self.ledger.values():
            if entry.pid == parent_pid and entry.creation is not None:
                return {"pid": parent_pid, "creation_filetime": entry.creation,
                        "link": "ledger"}
        return {"pid": parent_pid, "creation_filetime": None, "link": "census-unresolved"}

    def _register_member(self, pid: int, source: str, census: dict[int, int] | None,
                         is_root: bool = False, handle: _HANDLE | None = None,
                         creation: int | None = None) -> LedgerEntry:
        if handle is None:
            handle = self.win32.open_process(pid)
        if handle is None:
            known = next((e for e in self.ledger.values() if e.pid == pid), None)
            if known is not None:
                # Dual-signal ordering on the join side: this pid was already
                # registered and has since exited — the queued NEW_PROCESS
                # packet drained late. The member is fully accounted; this is
                # a benign late join record, not an identity gap.
                self._record("ledger", {"kind": "late_join_notification",
                                        "pid": pid, "utc": utc_now()})
                return known
            gap = {"pid": pid, "source": source, "utc": utc_now(),
                   "reason": "exited or inaccessible before creation-time/handle capture"}
            self.identity_gaps.append(gap)
            self._record("ledger", {"kind": "identity_gap", **gap})
            entry = LedgerEntry(pid, None, is_root, None, source)
            entry.exited = True
            entry.query_error = "identity-gap"
            self.ledger[entry.key()] = entry
            return entry
        if creation is None:
            try:
                creation = self.win32.process_creation_time(handle)
            except AdapterError as error:
                self.identity_gaps.append({
                    "pid": pid, "utc": utc_now(),
                    "reason": f"creation-time query failed: {error}"})
                self.win32.close_handle(handle)
                entry = LedgerEntry(pid, None, is_root, None, source)
                entry.query_error = "creation-time-unavailable"
                self.ledger[entry.key()] = entry
                return entry
        # A reused PID carrying a new creation time is a different process (§4)
        # — its ledger key differs, so it never collides with or inherits the old.
        parent = self._parent_evidence(pid, census)
        entry = LedgerEntry(pid, creation, is_root, parent, source)
        if entry.linkage == "unknown" and not is_root:
            self.identity_gaps.append({
                "pid": pid, "creation_filetime": creation, "utc": utc_now(),
                "reason": "parent linkage lacks defensible creation-time evidence"})
        self.ledger[entry.key()] = entry
        self.handles[entry.key()] = handle
        self._record("ledger", {"kind": "member_registered", **entry.to_record()})
        return entry

    def _live_entries_for_pid(self, pid: int) -> list[LedgerEntry]:
        return [e for e in self.ledger.values() if e.pid == pid and not e.exited]

    def _verify_exit(self, entry: LedgerEntry) -> None:
        handle = self.handles.get(entry.key())
        if handle is None:
            return
        try:
            exited, code = self.win32.process_exit_state(handle)
        except AdapterError as error:
            entry.query_error = str(error)
            self._trigger_stop(f"exit-state query failed for {entry.key()}: {error}")
            return
        if exited:
            entry.exited = True
            entry.exit_code = code
            if entry.exit_utc is None:
                entry.exit_utc = utc_now()
            # persist exit evidence BEFORE releasing the handle; if the write
            # fails the handle is retained and the exit stays unverified (§4).
            # The field is stamped True in the record because the record only
            # exists when the write succeeded.
            entry.exit_verified = self._record(
                "ledger", {"kind": "member_exit_verified",
                           **{**entry.to_record(), "exit_verified": True}})
            if entry.exit_verified:
                self.win32.close_handle(handle)
                self.handles.pop(entry.key(), None)

    def _on_exit_notification(self, pid: int) -> None:
        entries = self._live_entries_for_pid(pid)
        if not entries:
            if any(e.pid == pid for e in self.ledger.values()):
                # Dual-signal ordering: _reconcile_exits (or an earlier drain)
                # already verified this registered member's exit — the queued
                # port packet is a benign late duplicate, not an identity gap.
                self._record("ledger", {"kind": "duplicate_exit_notification",
                                        "pid": pid, "utc": utc_now()})
                return
            self.identity_gaps.append({
                "pid": pid, "utc": utc_now(),
                "reason": "exit notification for unregistered member"})
            self._record("ledger", {"kind": "identity_gap",
                                    "pid": pid, "utc": utc_now(),
                                    "reason": "unregistered exit notification"})
            return
        for entry in entries:
            self._verify_exit(entry)
        for entry in entries:
            if entry.is_root and entry.exited:
                self.root_exited = True
                self.root_exit_code = entry.exit_code

    # -- containment ---------------------------------------------------------------

    def _create_containment(self) -> None:
        if not hasattr(sys, "getwindowsversion") or sys.getwindowsversion().major < 10:
            raise AdapterError("job-object containment requires Windows 10 or later")
        (self._supervisor_pid, self._supervisor_creation,
         self._supervisor_in_job) = self.win32.supervisor_identity()
        jit = self.args.jit_id.replace("/", "_").replace("\\", "_")
        self.job = self.win32.create_job(f"cameo-qual-{jit}-{self._supervisor_pid}")
        flags = self.win32.job_limit_flags(self.job)
        if flags & JOB_OBJECT_FORBIDDEN_LIMITS or not flags & JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE:
            raise AdapterError(f"job limit flags invalid: 0x{flags:08x}")
        self.port = self.win32.create_completion_port()
        # Associated while the job is still empty (design §3)
        self.win32.associate_completion_port(self.job, self.port, 1)

    def _launch_root(self, argv: list[str], cwd: str | None) -> None:
        assert self.job is not None and self.evidence_dir is not None
        try:
            std_in = self.win32.open_inheritable_file(Path(os.devnull), for_write=False)
            std_out = self.win32.open_inheritable_file(
                self.evidence_dir / "inner_stdout.log", for_write=True)
            std_err = self.win32.open_inheritable_file(
                self.evidence_dir / "inner_stderr.log", for_write=True)
            self._child_handles = [std_in, std_out, std_err]
            h_process, h_thread, pid = self.win32.launch_suspended_in_job(
                subprocess.list2cmdline(argv), self.job, cwd,
                (std_in, std_out, std_err))
        except AdapterError:
            for handle in self._child_handles:
                self.win32.close_handle(handle)
            self._child_handles = []
            raise
        self.root_handle = h_process
        self.root_thread = h_thread
        if not self.win32.is_process_in_job(h_process, self.job):
            # Suspended and uncontained: clean only this exact handle-owned root
            # (design §3) — the job does not cover it, terminate directly.
            self.win32.terminate_process(h_process)
            raise AdapterError("root not a job member after job-list launch; suspended root terminated")
        creation = self.win32.process_creation_time(h_process)
        self._register_member(pid=pid, source="launch", census=None,
                              is_root=True, handle=h_process, creation=creation)
        root_key = f"{pid}:{creation}"
        self._record("samples", {
            "kind": "pre_resume", "utc": utc_now(), "mono": self.mono(),
            "host": self._host_sample(),
            "members": [self._member_sample(self.ledger[root_key])],
        })

    def _resume_root(self) -> None:
        assert self.root_thread is not None
        self.win32.resume_thread(self.root_thread)
        self.resumed = True
        self._record("ledger", {"kind": "root_resumed",
                                "utc": utc_now(), "mono": self.mono()})

    # -- sampling -----------------------------------------------------------------

    def _host_sample(self) -> dict[str, Any]:
        avail, total = self.win32.available_ram_bytes()
        return {"avail_phys_bytes": avail, "total_phys_bytes": total}

    def _member_sample(self, entry: LedgerEntry) -> dict[str, Any]:
        record: dict[str, Any] = {
            "ledger_key": entry.key(), "pid": entry.pid,
            "is_root": entry.is_root, "exited": entry.exited,
        }
        if entry.exited or entry.creation is None:
            return record
        handle = self.handles.get(entry.key())
        if handle is None:
            record["private_bytes_error"] = "no retained handle"
            return record
        try:
            entry.private_bytes = self.win32.private_bytes(handle)
            record["private_bytes"] = entry.private_bytes
            if entry.private_bytes > MAX_OWNED_PRIVATE_BYTES:
                record["over_limit"] = True
                self._trigger_stop(
                    f"owned private bytes {entry.private_bytes} exceed "
                    f"{MAX_OWNED_PRIVATE_BYTES} for {entry.key()}")
        except AdapterError as error:
            try:
                exited, _ = self.win32.process_exit_state(handle)
            except AdapterError:
                exited = False
            if exited:
                entry.exited = True
                record["exited"] = True
                record["exited_during_measurement"] = True
            else:
                record["private_bytes_error"] = str(error)
                self._trigger_stop(
                    f"memory query failed on live member {entry.key()}: {error}")
        return record

    def _reconcile_membership(self) -> None:
        """Census: accounting brackets the id query; reconcile deltas (§4)."""
        assert self.job is not None
        before = self.win32.job_accounting(self.job)
        ids = self.win32.job_process_ids(self.job)
        after = self.win32.job_accounting(self.job)
        if before["TotalProcesses"] != after["TotalProcesses"]:
            self._record("ledger", {"kind": "accounting_moved_during_census",
                                    "before": before, "after": after,
                                    "utc": utc_now()})
        census = self.win32.process_census()
        for pid in ids:
            if self._live_entries_for_pid(pid):
                continue
            exited_same_pid = [e for e in self.ledger.values()
                               if e.pid == pid and e.exited]
            if not exited_same_pid:
                self._register_member(pid, "membership_query", census)
                continue
            # Possibly a reused PID: distinguish by creation time, never by pid.
            handle = self.win32.open_process(pid)
            if handle is None:
                continue  # racing exit; the id list is already stale
            try:
                creation = self.win32.process_creation_time(handle)
            except AdapterError:
                self.win32.close_handle(handle)
                continue
            if all(e.creation != creation for e in exited_same_pid):
                self._register_member(pid, "membership_query", census,
                                      handle=handle, creation=creation)
            else:
                self.win32.close_handle(handle)

    def _reconcile_accounting(self) -> None:
        """TotalProcesses vs unique identities — unexplained deltas are gaps."""
        assert self.job is not None
        self.accounting = self.win32.job_accounting(self.job)
        unique = len(self.ledger)
        unexplained = self.accounting["TotalProcesses"] - unique
        if unexplained > 0:
            gap_record = {
                "kind": "accounting_gap", "utc": utc_now(),
                "total_processes": self.accounting["TotalProcesses"],
                "known_identities": unique,
                "unexplained": unexplained,
            }
            self.identity_gaps.append(gap_record)
            self._record("ledger", gap_record)

    def _reconcile_exits(self) -> None:
        """Port-independent exit reconciliation (§4): a retained handle's
        exit state is authoritative, and a member absent from the job id
        list has terminated — processes cannot leave a job while alive.
        Covers dropped or undelivered EXIT_PROCESS notifications so the
        wait loop can never sample an already-empty job forever."""
        assert self.job is not None
        live = [e for e in self.ledger.values() if not e.exited]
        if not live:
            return
        for entry in live:
            if self.handles.get(entry.key()) is not None:
                self._verify_exit(entry)
        missing = [e for e in live
                   if not e.exited and e.key() not in self.handles]
        if missing:
            ids = set(self.win32.job_process_ids(self.job))
            for entry in missing:
                if entry.pid not in ids:
                    entry.exited = True
                    if entry.exit_utc is None:
                        entry.exit_utc = utc_now()
                    entry.exit_verified = self._record(
                        "ledger", {"kind": "member_exit_verified",
                                   "verified_via": "job_absence",
                                   **{**entry.to_record(), "exit_verified": True}})
        for entry in live:
            if entry.is_root and entry.exited:
                self.root_exited = True
                self.root_exit_code = entry.exit_code

    def _drain_notifications(self, budget: int = 64) -> None:
        with self._notify_lock:
            pending, self._notify_queue = self._notify_queue, []
        for message, pid in pending[:budget]:
            if message == JOB_OBJECT_MSG_NEW_PROCESS:
                if not self._live_entries_for_pid(pid):
                    try:
                        census = self.win32.process_census()
                    except AdapterError:
                        census = None
                    self._register_member(pid, "job_notification", census)
            elif message in (JOB_OBJECT_MSG_EXIT_PROCESS, JOB_OBJECT_MSG_ABNORMAL_EXIT_PROCESS):
                self._on_exit_notification(pid)
            elif message == JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO:
                self._record("ledger", {"kind": "job_empty_notification",
                                        "utc": utc_now()})
                self._reconcile_exits()
            else:
                self._record("ledger", {"kind": "unrecognized_notification",
                                        "message": message, "pid": pid,
                                        "utc": utc_now()})
        if len(pending) > budget:
            with self._notify_lock:
                self._notify_queue = pending[budget:] + self._notify_queue

    def _event_loop(self) -> None:
        assert self.port is not None
        while not self._event_stop.is_set():
            try:
                item = self.win32.get_completion(self.port, 1000)
            except AdapterError as error:
                self._trigger_stop(f"completion-port query failed: {error}")
                return
            if item is None:
                continue
            with self._notify_lock:
                self._notify_queue.append(item)
            self._pace.set()

    # -- stop / clearance ------------------------------------------------------------

    def _trigger_stop(self, reason: str) -> None:
        if self.stop_reason is None:
            self.stop_reason = reason
            self.log("hold", "stop triggered", reason=reason)
        self._pace.set()

    def _stop_owned(self, reason: str) -> None:
        if self.stopped:
            return
        self.stopped = True
        self._record("ledger", {"kind": "stop_requested", "reason": reason,
                                "utc": utc_now(), "mono": self.mono()})
        if self.job is not None:
            try:
                self.win32.terminate_job(self.job)
            except AdapterError as error:
                self.hold_reasons.append(f"TerminateJobObject failed: {error}")

    def _clearance_snapshot(self, index: int) -> dict[str, Any]:
        assert self.job is not None
        snap: dict[str, Any] = {"kind": "clearance", "index": index,
                                "utc": utc_now(), "mono": self.mono()}
        ok = True
        try:
            ids = self.win32.job_process_ids(self.job)
            accounting = self.win32.job_accounting(self.job)
        except AdapterError as error:
            self.hold_reasons.append(f"clearance query failed: {error}")
            snap["query_error"] = str(error)
            snap["clear"] = False
            self._record("clearance", snap)
            return snap
        snap["job_membership_ids"] = ids
        snap["active_processes"] = accounting["ActiveProcesses"]
        snap["total_processes"] = accounting["TotalProcesses"]
        snap["terminated_processes"] = accounting["TotalTerminatedProcesses"]
        unverified = []
        for entry in self.ledger.values():
            if not entry.exited:
                self._verify_exit(entry)
            if entry.creation is not None and not entry.exit_verified:
                unverified.append(entry.key())
                ok = False
        snap["unverified_exits"] = unverified
        census = self.win32.process_census()
        live_keys = []
        for entry in self.ledger.values():
            if entry.creation is None or entry.pid not in census:
                continue
            handle = self.handles.get(entry.key())
            if handle is None and not entry.exited:
                live_keys.append(entry.key())
                ok = False
        snap["live_ledger_keys"] = live_keys
        snap["identity_gaps"] = len(self.identity_gaps)
        # A latched evidence failure can never certify clearance.
        snap["clear"] = (
            ok and not ids and accounting["ActiveProcesses"] == 0
            and not self.evidence_failed)
        if not self._record("clearance", snap):
            # Clearance evidence itself is unavailable: the snapshot is not
            # proven clear regardless of what it computed.
            snap["clear"] = False
            snap["evidence_unavailable"] = True
            self._record("clearance", {
                "kind": "clearance_record_failed", "index": index,
                "utc": utc_now()})
        return snap

    def _wait_spacing(self, target_mono: float) -> None:
        # Bounded retries on purpose: a broken wait must not hang; the
        # realized-gap check in _clearance fails closed instead.
        for _ in range(3):
            remaining = target_mono - self.mono()
            if remaining <= 0:
                return
            self._spacing.wait(remaining)

    def _clearance(self, spacing_seconds: float, timeout_seconds: float) -> bool:
        """Two owned snapshots ≥ spacing apart, both clear (design §5)."""
        self.job_sealed = True
        spacing = max(spacing_seconds, MIN_CLEARANCE_SPACING_SECONDS)
        deadline = self.mono() + timeout_seconds
        snapshots = []
        for index in (0, 1):
            if index == 1:
                self._wait_spacing(snapshots[0]["mono"] + spacing)
            if self.mono() > deadline:
                self.hold_reasons.append("clearance timeout exceeded")
                return False
            snapshots.append(self._clearance_snapshot(index))
        if snapshots[1]["mono"] < snapshots[0]["mono"] + spacing:
            self.hold_reasons.append(
                f"clearance spacing "
                f"{snapshots[1]['mono'] - snapshots[0]['mono']:.3f}s "
                f"below required {spacing:.3f}s")
            return False
        if not all(s["clear"] for s in snapshots):
            self.hold_reasons.append("clearance snapshot(s) not clear")
            return False
        return True

    # -- grant / preflight -------------------------------------------------------------

    def _load_grant(self) -> dict[str, Any]:
        grant_path = Path(self.args.grant_input)
        if not grant_path.is_file():
            raise ValueError(f"grant input missing: {grant_path}")
        grant = json.loads(grant_path.read_text(encoding="utf-8"))
        required = {
            "jit_id", "stage", "argv", "tool_sha256", "source_commit",
            "termination_timeout_seconds", "clearance_timeout_seconds", "cwd",
        }
        missing = sorted(required - set(grant))
        if missing:
            raise ValueError(f"grant input missing fields: {missing}")
        if grant["jit_id"] != self.args.jit_id:
            raise ValueError("grant jit_id mismatch with --jit-id")
        if int(grant["stage"]) != self.args.stage:
            raise ValueError("grant stage mismatch with --stage")
        if list(grant["argv"]) != list(self.args.child_argv):
            raise ValueError("argv does not match grant-approved argv verbatim")
        if float(grant.get("clearance_timeout_seconds", 0)) <= 0:
            raise ValueError("clearance_timeout_seconds must be positive")
        if float(grant.get("termination_timeout_seconds", 0)) <= 0:
            raise ValueError("termination_timeout_seconds must be positive")
        return grant

    def _resolve_source_commit(self, repo_root: Path) -> str | None:
        try:
            gitfile = repo_root / ".git"
            if gitfile.is_file():
                gitdir = Path(gitfile.read_text().split(":", 1)[1].strip())
                if not gitdir.is_absolute():
                    gitdir = (repo_root / gitdir).resolve()
            else:
                gitdir = gitfile
            # Linked worktrees keep branch refs and packed-refs in the common
            # gitdir, not the worktree's own gitdir — follow commondir when
            # present (without it every linked worktree resolves None).
            refdir = gitdir
            commondir_file = gitdir / "commondir"
            if commondir_file.is_file():
                common = Path(commondir_file.read_text().strip())
                refdir = (common if common.is_absolute()
                          else (gitdir / common).resolve())
            head = (gitdir / "HEAD").read_text().strip()
            if head.startswith("ref:"):
                ref = head[4:].strip()
                ref_path = refdir / ref
                if ref_path.is_file():
                    return ref_path.read_text().strip()
                packed = refdir / "packed-refs"
                if packed.is_file():
                    for line in packed.read_text().splitlines():
                        if not line.startswith("#") and line.endswith(f" {ref}"):
                            return line.split(" ")[0]
                return None
            return head
        except (OSError, IndexError):
            return None

    def _bind_tool_hashes(self, grant: dict[str, Any], repo_root: Path) -> None:
        """Hash the supervisor, the adapter surface (this file), the interpreter,
        the resolved run_capped bytes and every grant-declared tool (design §2)."""
        paths: dict[str, Path] = {"__self__": Path(__file__).resolve()}
        for name in grant["tool_sha256"]:
            paths[name] = Path(name) if os.path.isabs(name) else repo_root / name
        for part in self.args.child_argv:
            if part.endswith("run_capped.py"):
                candidate = Path(part)
                if not candidate.is_absolute():
                    candidate = repo_root / candidate
                paths.setdefault(str(candidate.relative_to(repo_root)
                                   if candidate.is_relative_to(repo_root) else candidate),
                                 candidate)
        for name, path in paths.items():
            if path.is_file():
                self.tool_hashes_pre[name] = sha256_file(path)

    def _preflight(self, grant: dict[str, Any], repo_root: Path,
                   evidence_dir: Path) -> list[str]:
        refusals: list[str] = []
        if evidence_dir.exists():
            refusals.append("evidence dir must be absent-unique; it already exists")
        avail, _total = self.win32.available_ram_bytes()
        if avail < MIN_ADMISSION_AVAIL_RAM_BYTES:
            refusals.append(
                f"available RAM {avail} below admission minimum "
                f"{int(MIN_ADMISSION_AVAIL_RAM_BYTES)}")
        try:
            free = self.win32.free_disk_bytes(str(evidence_dir.parent))
            if free < MIN_ADMISSION_FREE_DISK_BYTES:
                refusals.append(
                    f"free disk {free} below admission minimum "
                    f"{MIN_ADMISSION_FREE_DISK_BYTES}")
        except AdapterError as error:
            refusals.append(f"disk free-space query failed: {error}")
        commit = self._resolve_source_commit(repo_root)
        if commit is None:
            refusals.append("source_commit unresolvable at repo root")
        elif grant["source_commit"] != commit:
            refusals.append(
                f"source_commit mismatch: grant {grant['source_commit']} vs tree {commit}")
        for name, declared in grant["tool_sha256"].items():
            tool_path = Path(name) if os.path.isabs(name) else repo_root / name
            if not tool_path.is_file():
                refusals.append(f"declared tool missing: {name}")
                continue
            if self.tool_hashes_pre.get(name) != declared:
                refusals.append(
                    f"tool hash drift at {name}: {self.tool_hashes_pre.get(name)} != {declared}")
        argv = list(self.args.child_argv)
        if not any(part.endswith("run_capped.py") for part in argv[:2]):
            refusals.append("child argv does not invoke run_capped.py")
        status_at = [i for i, a in enumerate(argv) if a == "--status-file"]
        if len(status_at) != 1 or status_at[0] + 1 >= len(argv):
            refusals.append("child argv must declare exactly one --status-file")
        else:
            status_path = Path(argv[status_at[0] + 1])
            inner = (evidence_dir / "inner_run_capped_status.json").resolve()
            if not status_path.is_absolute():
                status_path = (Path(grant["cwd"]) / status_path).resolve()
            else:
                status_path = status_path.resolve()
            if status_path != inner:
                refusals.append(
                    "child --status-file must be <evidence>/inner_run_capped_status.json")
        timeout_at = [i for i, a in enumerate(argv) if a == "--timeout"]
        if len(timeout_at) != 1 or timeout_at[0] + 1 >= len(argv):
            refusals.append("child argv must declare --timeout <seconds>")
        else:
            try:
                child_timeout = float(argv[timeout_at[0] + 1])
                monitor_timeout = float(self.args.timeout)
                if not math.isfinite(child_timeout) or child_timeout <= 0:
                    refusals.append("child --timeout must be finite and positive")
                elif not math.isfinite(monitor_timeout) or monitor_timeout <= 0:
                    refusals.append("monitor --timeout must be finite and positive")
                elif child_timeout > monitor_timeout:
                    refusals.append("child --timeout must not exceed monitor --timeout")
                elif (child_timeout < monitor_timeout
                      and monitor_timeout - child_timeout
                      < CHILD_TIMEOUT_CLEANUP_HEADROOM_SECONDS):
                    refusals.append(
                        "shorter child --timeout lacks monitor cleanup headroom "
                        f"({CHILD_TIMEOUT_CLEANUP_HEADROOM_SECONDS:g}s required)")
            except ValueError:
                refusals.append("child --timeout is not numeric")
        cwd = Path(grant["cwd"])
        if not cwd.is_dir():
            refusals.append(f"grant cwd does not exist: {cwd}")
        return refusals

    # -- main run -----------------------------------------------------------------------

    def run(self) -> int:
        repo_root = Path(__file__).resolve().parents[2]
        evidence_dir = Path(self.args.evidence_dir)
        disposition = "REFUSED"
        code = EXIT_MONITOR_ERROR
        grant: dict[str, Any] = {}
        try:
            grant = self._load_grant()
        except (ValueError, OSError, json.JSONDecodeError) as error:
            self._finalize_refusal(f"grant invalid: {error}")
            return EXIT_REFUSAL
        try:
            self._bind_tool_hashes(grant, repo_root)
            refusals = self._preflight(grant, repo_root, evidence_dir)
            if refusals:
                self._finalize_refusal("; ".join(refusals))
                return EXIT_REFUSAL
            try:
                evidence_dir.mkdir(parents=True, exist_ok=False)
                self._open_evidence(evidence_dir)
            except OSError as error:
                self._finalize_refusal(f"evidence dir unusable: {error}")
                return EXIT_REFUSAL
            self._create_containment()
            self._event_thread = threading.Thread(
                target=self._event_loop, daemon=True, name="job-events")
            self._event_thread.start()
            self._launch_root(list(self.args.child_argv), grant["cwd"])
            if self.stop_reason is not None:
                # e.g. evidence write failed during pre-resume registration:
                # never let a tainted run start (design §5 fail-closed).
                self._stop_owned(f"pre-resume stop: {self.stop_reason}")
            else:
                # Recheck after containment setup and suspended launch: setup
                # time must not consume the cleanup reserve that preflight
                # proved for a shortened child cap. The suspended root has not
                # executed; a refusal here is safe and remains non-pass.
                timeout_at = [
                    i for i, part in enumerate(self.args.child_argv)
                    if part == "--timeout"]
                child_timeout = float(
                    self.args.child_argv[timeout_at[0] + 1])
                remaining = self.deadline_mono - self.mono()
                if (child_timeout < float(self.args.timeout)
                        and remaining < child_timeout
                        + CHILD_TIMEOUT_CLEANUP_HEADROOM_SECONDS):
                    raise AdapterError(
                        "insufficient monitor deadline headroom before child resume")
                self._resume_root()
                self.log("info", "contained root resumed",
                         jit_id=self.args.jit_id, stage=self.args.stage)
            self._monitor_loop()
            inner = self._read_inner_status(evidence_dir)
            if self.stop_reason is not None:
                self._stop_owned(self.stop_reason)
            cleared = self._clearance(
                float(grant.get("clearance_spacing_seconds",
                                MIN_CLEARANCE_SPACING_SECONDS)),
                float(grant["clearance_timeout_seconds"]))
            disposition = self._disposition(inner, cleared)
            code = self._exit_code_for(disposition, inner)
        except AdapterError as error:
            self.hold_reasons.append(str(error))
            if self.resumed or self.stopped:
                disposition = "HOLD_MONITOR_ERROR"
                code = EXIT_HOLD
                try:
                    self._stop_owned(f"monitor error: {error}")
                    self._clearance(
                        float(grant.get("clearance_spacing_seconds",
                                        MIN_CLEARANCE_SPACING_SECONDS)),
                        float(grant.get("clearance_timeout_seconds", 60.0)))
                except AdapterError as clearance_error:
                    self.hold_reasons.append(
                        f"post-error clearance failed: {clearance_error}")
            else:
                # Failure before resume: nothing uncontained ever ran (§3).
                disposition = "REFUSED"
                code = EXIT_REFUSAL
        except KeyboardInterrupt:
            disposition = "HOLD_INTERRUPTED"
            self._stop_owned("supervisor interrupted")
            try:
                self._clearance(MIN_CLEARANCE_SPACING_SECONDS,
                                float(grant.get("clearance_timeout_seconds", 60.0)))
            except AdapterError:
                self.hold_reasons.append("clearance failed after interrupt")
            code = EXIT_HOLD
        finally:
            self._event_stop.set()
            try:
                if self.evidence_dir is not None:
                    # _write_final may downgrade the persisted disposition
                    # (e.g. post-run tool-hash drift); the exit code must
                    # derive from that same final state, never the earlier one.
                    final_disposition = self._write_final(
                        disposition, grant, repo_root)
                    if final_disposition != disposition:
                        disposition = final_disposition
                        code = self._exit_code_for(
                            disposition, self.inner_status or {})
            except OSError as error:
                print(f"receipt-write failure: {error}", file=sys.stderr)
                code = EXIT_MONITOR_ERROR
            for writer in self.writers.values():
                try:
                    writer.close()
                except OSError:
                    pass
            try:
                self._log_stream.close()
            except (AttributeError, OSError):
                pass
            for handle in self._child_handles:
                self.win32.close_handle(handle)
            self.win32.close_handle(self.root_thread)
            self.win32.close_handle(self.root_handle)
            for handle in list(self.handles.values()):
                self.win32.close_handle(handle)
            self.win32.close_handle(self.port)
            self.win32.close_handle(self.job)
        return code

    def _monitor_loop(self) -> None:
        self.last_sample_mono = self.mono()
        while True:
            now = self.mono()
            if self.stop_reason is not None:
                return
            if now >= self.deadline_mono:
                self._trigger_stop("work deadline reached")
                return
            if self.last_sample_mono is not None and (
                    now - self.last_sample_mono) > MAX_SAMPLE_GAP_SECONDS:
                self._trigger_stop("sample cadence exceeded maximum gap")
                return
            try:
                self._drain_notifications()
                self._reconcile_membership()
                self._reconcile_accounting()
                self._reconcile_exits()
            except AdapterError as error:
                self._trigger_stop(f"membership/accounting query failed: {error}")
                return
            record: dict[str, Any] = {
                "kind": "sample", "utc": utc_now(), "mono": now,
                "host": self._host_sample(),
                "members": [],
                "accounting": self.accounting,
            }
            if record["host"]["avail_phys_bytes"] < STOP_AVAIL_RAM_BYTES:
                self._trigger_stop(
                    f"available RAM {record['host']['avail_phys_bytes']} below "
                    f"stop threshold {STOP_AVAIL_RAM_BYTES}")
            for entry in list(self.ledger.values()):
                record["members"].append(self._member_sample(entry))
            self._record("samples", record)
            self.last_sample_mono = now
            if self.root_exited:
                # Bounded drain so normally-teardown descendants can verify-exit
                # before a lingering judgment (design §5 lingering = non-pass).
                drain_until = now + ROOT_EXIT_DRAIN_SECONDS
                while self.mono() < drain_until:
                    self._drain_notifications()
                    self._reconcile_exits()
                    remaining = [e for e in self.ledger.values()
                                 if not e.exited and not e.is_root]
                    if not remaining:
                        break
                    time.sleep(0.05)
                remaining = [e for e in self.ledger.values()
                             if not e.exited and not e.is_root]
                if remaining:
                    self._trigger_stop(
                        "root exited with live members (lingering children)")
                return
            self._pace.wait(max(0.0, SAMPLE_INTERVAL_SECONDS - (self.mono() - now)))
            self._pace.clear()

    def _read_inner_status(self, evidence_dir: Path) -> dict[str, Any]:
        path = evidence_dir / "inner_run_capped_status.json"
        if not path.is_file():
            self.hold_reasons.append("inner status file absent")
            self.inner_status = {"status": "ABSENT", "exit_code": None}
            return self.inner_status
        try:
            inner = json.loads(path.read_text(encoding="utf-8-sig"))
            inner["_sha256"] = sha256_file(path)
            self.inner_status = inner
            return inner
        except (OSError, json.JSONDecodeError) as error:
            self.hold_reasons.append(f"inner status unreadable: {error}")
            self.inner_status = {"status": "UNREADABLE", "exit_code": None}
            return self.inner_status

    def _disposition(self, inner: dict[str, Any], cleared: bool) -> str:
        if self.stop_reason is not None:
            if "private bytes" in self.stop_reason:
                return "ABORTED_RESOURCE"
            if "deadline" in self.stop_reason:
                return "ABORTED_WALL_CAP"
        # Any non-null stop_reason, latched evidence failure, uncleared job,
        # hold reason, or identity gap is non-pass — none qualify as COMPLETED.
        if (self.evidence_failed or not cleared or self.hold_reasons
                or self.identity_gaps or self.stop_reason is not None):
            return "HOLD"
        if inner.get("status") == "COMPLETED" and inner.get("exit_code") == 0:
            return "COMPLETED"
        if inner.get("status") == "INCOMPLETE_WALLCAP":
            return "INNER_WALLCAP"
        return "INNER_FAILURE"

    def _exit_code_for(self, disposition: str, inner: dict[str, Any]) -> int:
        if disposition == "COMPLETED":
            return EXIT_COMPLETED
        if disposition in ("INNER_WALLCAP", "ABORTED_WALL_CAP", "ABORTED_RESOURCE"):
            return EXIT_INNER_CAPPED
        if disposition == "INNER_FAILURE":
            code = inner.get("exit_code")
            return code if isinstance(code, int) and 0 < code < 128 else EXIT_INNER_FAILURE
        if disposition.startswith("HOLD"):
            return EXIT_HOLD
        return EXIT_MONITOR_ERROR

    def _finalize_refusal(self, reason: str) -> None:
        receipt = {
            "schema": 1,
            "tool": Path(__file__).name,
            "design_doc": DESIGN_DOC,
            "design_sha256": DESIGN_SHA256,
            "jit_id": self.args.jit_id,
            "stage": self.args.stage,
            "disposition": "REFUSED",
            "reason": reason,
            "utc": utc_now(),
            "grant_sha256": (
                sha256_file(Path(self.args.grant_input))
                if Path(self.args.grant_input).is_file() else None),
            "execution_approved": False,
        }
        print(json.dumps(receipt, indent=2, sort_keys=True), file=sys.stderr)
        if self.evidence_dir is not None:
            try:
                status_path = self.evidence_dir / "monitor_status.json"
                temporary = status_path.with_name(
                    f"{status_path.name}.{os.getpid()}.tmp")
                with temporary.open("w", encoding="utf-8", newline="\n") as stream:
                    json.dump(receipt, stream, indent=2, sort_keys=True)
                    stream.write("\n")
                os.replace(temporary, status_path)
            except OSError:
                pass

    def _write_final(self, disposition: str, grant: dict[str, Any],
                     repo_root: Path) -> str:
        """Write the atomic receipt and return the FINAL disposition — callers
        must derive the exit code from the returned value, since post-run
        checks here (tool-hash drift) can downgrade a computed COMPLETED."""
        for name, path in ((n, Path(__file__).resolve() if n == "__self__"
                             else Path(n) if os.path.isabs(n) else repo_root / n)
                           for n in self.tool_hashes_pre):
            try:
                self.tool_hashes_post[name] = sha256_file(path)
            except OSError as error:
                self.tool_hashes_post[name] = f"unreadable: {error}"
                self.hold_reasons.append(f"post-run tool hash unreadable: {name}")
        drift = sorted(k for k in self.tool_hashes_pre
                       if self.tool_hashes_post.get(k) != self.tool_hashes_pre[k])
        if drift:
            self.hold_reasons.append(f"tool hash drift post-run: {drift}")
            if disposition == "COMPLETED":
                disposition = "HOLD"
        evidence_hashes = {}
        for key, path in self.evidence.items():
            if path.is_file():
                evidence_hashes[path.name] = sha256_file(path)
        receipt = {
            "schema": 1,
            "tool": Path(__file__).name,
            "tool_sha256": sha256_file(Path(__file__)),
            "design_doc": DESIGN_DOC,
            "design_sha256": DESIGN_SHA256,
            "jit_id": self.args.jit_id,
            "stage": self.args.stage,
            "disposition": disposition,
            "hold_reasons": self.hold_reasons,
            "stop_reason": self.stop_reason,
            "evidence_write_failed": self.evidence_failed,
            "identity_gaps": self.identity_gaps,
            "inner": self.inner_status,
            "child_argv": list(self.args.child_argv),
            "cwd": grant.get("cwd"),
            "grant_sha256": sha256_file(Path(self.args.grant_input)),
            "source_commit": self._resolve_source_commit(repo_root),
            "python_executable_sha256": sha256_file(Path(sys.executable)),
            "python_version": sys.version.split()[0],
            "supervisor_in_job_at_start": self._supervisor_in_job,
            "tool_hashes_pre": self.tool_hashes_pre,
            "tool_hashes_post": self.tool_hashes_post,
            "tool_hash_drift": drift,
            "ledger_size": len(self.ledger),
            "ledger": [e.to_record() for e in self.ledger.values()],
            "evidence_sha256": evidence_hashes,
            "policy": {
                "min_admission_avail_ram_bytes": int(MIN_ADMISSION_AVAIL_RAM_BYTES),
                "min_admission_free_disk_bytes": MIN_ADMISSION_FREE_DISK_BYTES,
                "stop_avail_ram_bytes": STOP_AVAIL_RAM_BYTES,
                "max_owned_private_bytes": MAX_OWNED_PRIVATE_BYTES,
                "sample_interval_seconds": SAMPLE_INTERVAL_SECONDS,
                "max_sample_gap_seconds": MAX_SAMPLE_GAP_SECONDS,
                "child_timeout_cleanup_headroom_seconds": (
                    CHILD_TIMEOUT_CLEANUP_HEADROOM_SECONDS),
                "timeout_seconds": self.args.timeout,
            },
            "started_utc": self.started_utc,
            "ended_utc": utc_now(),
            "elapsed_seconds": round(self.mono() - self.started_mono, 3),
            "execution_approved": False,
        }
        assert self.evidence_dir is not None
        status_path = self.evidence_dir / "monitor_status.json"
        temporary = status_path.with_name(f"{status_path.name}.{os.getpid()}.tmp")
        try:
            with temporary.open("w", encoding="utf-8", newline="\n") as stream:
                json.dump(receipt, stream, indent=2, sort_keys=True)
                stream.write("\n")
                stream.flush()
                os.fsync(stream.fileno())
            os.replace(temporary, status_path)
        finally:
            try:
                temporary.unlink()
            except FileNotFoundError:
                pass
        return disposition


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--jit-id", required=True, help="operator-issued admission id")
    parser.add_argument("--stage", required=True, type=int, choices=[1, 2, 3, 4])
    parser.add_argument("--grant-input", required=True,
                        help="read-only JSON: approved argv/cwd/pins/resource policy")
    parser.add_argument("--evidence-dir", required=True, type=Path,
                        help="absent-unique leaf; created by the monitor")
    parser.add_argument("--timeout", type=float, default=DEFAULT_TIMEOUT_SECONDS)
    parser.add_argument("child_argv", nargs=argparse.REMAINDER,
                        help="exact run_capped argv after --")
    return parser


def main(argv: Sequence[str] | None = None,
         adapter: Win32Adapter | None = None) -> int:
    if os.name != "nt":
        print("run_qualification_monitored is Windows-only", file=sys.stderr)
        return EXIT_REFUSAL
    parser = build_parser()
    args = parser.parse_args(argv)
    child = args.child_argv
    if child[:1] == ["--"]:
        child = child[1:]
    if not child:
        parser.error("a run_capped argv is required after --")
    if not math.isfinite(args.timeout) or args.timeout <= 0:
        parser.error("--timeout must be finite and positive")
    args.child_argv = child
    return Monitor(args, adapter=adapter).run()


if __name__ == "__main__":
    raise SystemExit(main())
