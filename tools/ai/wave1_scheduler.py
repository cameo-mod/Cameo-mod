#!/usr/bin/env python
r"""PARITY-WAVE1 scheduler — BASE-only attribution cells.

Staged by Devin-Integrator as Luna fallback. v3 (Devin-Architect,
REVIEW_2026-10-08_wave5_adjudication): comparator PASS requires exit 0 AND a
proof verdict AND a well-formed payload (both replay keys, positive-int record
counts, all-False incomplete flags, zero unparsed tails); every exit != 0 is
wave-stop-invalid; adjudication on exit-1 pairs re-extracts BOTH streams
in-process via the frozen comparator's own extract() — complete unmatched
sets, never the first-diff-frame only_a/only_b subset. Adjudicated-artifact
rule: one-sided extras, all SYNCHASH, all strictly past the shorter stream's
last frame, gated by nonempty identical match records (diagnostic gate, never
proof). Any ORDER/DISCONNECT/UNPARSED/interior/two-sided record stays
DIVERGENT — tail orders are unprovable and are never reclassified.

v4 (Devin-Architect): second-layer Phase-A bounded verdict per pair via
tools/ai/order_stream_diff_bounded.py resolved BESIDE this scheduler file
(NOT inside the frozen WORKTREE — the bounded tool is post-freeze tooling;
the pin covers capture evidence only). Persisted as <pair>.bounded.{stdout,
stderr,payload.json} under RUN_RESULTS/comparator and attached as
c["bounded"] — ADDITIVE reporting only: the strict comparator remains the
mechanical PASS gate; bounded acceptance (IDENTICAL_OUTCOME_BOUNDED_TAIL /
IDENTICAL) awaits dev/Sol/Luna sign-off and coordinator promotion. Tool
sha256 recorded per call for provenance.

Frozen spec (coordinator 2026-10-08):
- Frozen heads: engine devin/bot-determinism-8c2b@0a3f77dbe1a90bc59ff3ef5ad81534e8fbcf7695
  (foundation 8c2b385f82 + RNG pick); mod devin/bot-determinism@a9349d015619c5625cc188142ad72c10b391a16e.
- Seed: committed CameoDevSeed — CAMEO_DEV_SEED env or Cameo.DevSeed=<int> server arg.
  (OPENRA_PARITY_SEED is obsolete.)
- Comparator frozen at delta-approved 32a0f925c (sha below); only scheduler-
  side evidence handling is versioned here.

Policy: one scheduler, MAX_WORKERS concurrent game workers machine-wide
(foreign OpenRA instances count), isolated per-worker support dirs
C:\cameo-wt\parity-wave1-<workerN>, >=6GiB launch gate, whole-wave stop <4GiB,
15min wall backstop/cell. Incomplete captures are invalid, never divergence proof.
"""
import csv, ctypes, glob, json, os, re, subprocess, sys, time
from pathlib import Path

WORKTREE = Path(r"C:\cameo-wt\parity-wave1-bd")   # frozen mod head a9349d015
RESULTS_ROOT = Path(r"C:\cameo-wt\parity-wave1\results")
WORKER_ROOT = Path(r"C:\cameo-wt")                # parity-wave1-<workerN> live here
RUN_ID = time.strftime("%Y%m%dT%H%M%SZ", time.gmtime())
RUN_RESULTS = RESULTS_ROOT / RUN_ID
SEEDS = [1337, 20261006, 8675309]
MAPS = [
    r"mods/cameo/maps/ai_duel_gate_20260928",        # fp c546ed9c26d1
    r"mods/cameo/maps/_ra_a-nuclear-winter.oramap",
]
REPEATS = [1, 2]
LAUNCH_GIB = 6.0
ABORT_GIB = 4.0
MAX_WORKERS = 1  # current reservation cap; earlier 2-instance run hit the RAM floor
CELL_WALL_BACKSTOP_S = 15 * 60
FACTIONS = "td_gdi"
BOT = "hard"
TIME_LIMIT = 1

# Pinned tool SHA-256 (coordinator-frozen); verified before any launch.
TOOL_SHA256 = {
    "tools/ai/order_stream_diff.py": "88d9be75d73c36233ad9e7d341c505b1ff761507ebe23f58438f6d59e1586014",
    "tools/ai/run_ai_match_batch.py": "ed3aaed6bf713224d7bdd2664a525cd5b37d560f5173d6b0e26245ac35f35071",
    "tools/ai/order_trace.py": "4da5b841a91093b7a50fe4cfae2e72cf26ddfd10a75673e46ff6da729365bf82",
}


class MS(ctypes.Structure):
    _fields_ = [("dwLength", ctypes.c_ulong), ("dwMemoryLoad", ctypes.c_ulong),
                ("ullTotalPhys", ctypes.c_ulonglong), ("ullAvailPhys", ctypes.c_ulonglong),
                ("ullTotalPageFile", ctypes.c_ulonglong), ("ullAvailPageFile", ctypes.c_ulonglong),
                ("ullTotalVirtual", ctypes.c_ulonglong), ("ullAvailVirtual", ctypes.c_ulonglong),
                ("ullAvailExtendedVirtual", ctypes.c_ulonglong)]


WAVE_STOP = False  # set on RAM-floor/backstop abort; queued cells refuse to launch
LOCK_PATH = RESULTS_ROOT / ".wave1_scheduler.lock"


def free_gib():
    m = MS(); m.dwLength = ctypes.sizeof(MS)
    ctypes.windll.kernel32.GlobalMemoryStatusEx(ctypes.byref(m))
    return m.ullAvailPhys / 2 ** 30


def openra_processes():
    """Return OpenRA process rows; fail closed if tasklist cannot verify them."""
    found = []
    for image in ("OpenRA.exe", "OpenRA.WindowsLauncher.exe"):
        result = subprocess.run(
            ["tasklist", "/FI", f"IMAGENAME eq {image}", "/FO", "CSV", "/NH"],
            capture_output=True, text=True, timeout=10)
        if result.returncode != 0:
            raise RuntimeError(f"tasklist failed for {image}: {result.stderr.strip()}")
        for row in csv.reader(result.stdout.splitlines()):
            if not row or row[0].lower() != image.lower():
                continue
            if len(row) < 5:
                raise RuntimeError(f"unparseable tasklist row for {image}: {row!r}")
            try:
                pid = int(row[1])
                # tasklist memory column is locale-formatted ("787,396 K" en-US,
                # "787.396 K" de-DE); strip all non-digits — KiB is integral.
                rss_kib = int(re.sub(r"[^\d]", "", row[4]))
            except (ValueError, IndexError) as e:
                raise RuntimeError(f"unparseable tasklist values for {image}: {row!r}") from e
            found.append({"image": image, "pid": pid, "rss_kib": rss_kib})
    return found


def foreign_openra_count():
    """Count game processes. Errors propagate so launch checks fail closed."""
    return len(openra_processes())


def acquire_scheduler_lock():
    """Acquire an exclusive machine-local scheduler lock; stale locks need review."""
    try:
        fd = os.open(str(LOCK_PATH), os.O_CREAT | os.O_EXCL | os.O_WRONLY)
    except FileExistsError as e:
        raise RuntimeError(f"scheduler lock exists: {LOCK_PATH}; refuse concurrent run") from e
    os.write(fd, f"pid={os.getpid()} time={time.time()}\n".encode())
    return fd


def verify_tools():
    import hashlib
    bad = []
    for rel, want in TOOL_SHA256.items():
        p = WORKTREE / rel
        got = hashlib.sha256(p.read_bytes()).hexdigest() if p.is_file() else "MISSING"
        if got != want:
            bad.append(f"{rel}: {got}")
    return bad


def kill_tree(proc):
    """Kill the batch wrapper AND its spawned game child (wrapper-PID lesson:
    the cmd/python handle exiting does not mean OpenRA.exe exited)."""
    subprocess.run(["taskkill", "/T", "/F", "/PID", str(proc.pid)],
                   capture_output=True, timeout=20)
    try:
        proc.wait(timeout=10)
    except subprocess.TimeoutExpired:
        raise RuntimeError(f"could not confirm worker tree stopped for pid {proc.pid}")


def run_cell(worker, seed, map_path, repeat):
    """One cell: one batch invocation, one match, one replay."""
    global WAVE_STOP
    if WAVE_STOP:
        return {"cell": f"{worker}/s{seed}-{Path(map_path).name}-r{repeat}",
                "seed": seed, "map": map_path, "repeat": repeat,
                "verdict": "SKIPPED_WAVE_STOP"}
    support = WORKER_ROOT / f"parity-wave1-{RUN_ID}-{worker}"
    cell = support / f"s{seed}-{Path(map_path).name}-r{repeat}"
    if cell.exists():
        rec = {"cell": f"{worker}/s{seed}-{Path(map_path).name}-r{repeat}",
               "seed": seed, "map": map_path, "repeat": repeat,
               "verdict": "SKIPPED_EXISTING_CELL", "path": str(cell)}
        WAVE_STOP = True
        return rec
    cell.mkdir(parents=True, exist_ok=False)
    env = dict(os.environ, CAMEO_DEV_SEED=str(seed))
    env.pop("OPENRA_PARITY_MAX_TICKS", None)   # obsolete mechanism must not leak in
    cmd = [sys.executable, "-u", "tools/ai/run_ai_match_batch.py",
           "--factions", FACTIONS, "--bot-a", BOT, "--bot-b", BOT,
           "--repeats", "1", "--map", map_path, "--time-limit", str(TIME_LIMIT),
           "--support-dir", str(cell), "--stall-timeout", "180",
           # --retries 2: batch-tool's designed mitigation for "external kill or
           # phantom abort" deaths (no records, no exception). 2/4 launches died
           # that way today; retries cannot mask divergence — dead attempts write
           # no replay and every attempt is logged in batch_results.jsonl.
           "--retries", "2", "--keep-variants", "--render", "fast"]
    rec = {"cell": f"{worker}/s{seed}-{Path(map_path).name}-r{repeat}", "seed": seed,
           "map": map_path, "repeat": repeat, "t0": time.time(),
           "ram_min_gib": None, "ram_at_launch_gib": None, "game_rss_peak_gib": None,
           "verdict": None, "replay": None}
    try:
        existing = openra_processes()
    except Exception as e:
        rec["verdict"] = "ABORTED_PROCESS_MONITOR_ERROR"
        rec["error"] = str(e)
        WAVE_STOP = True
        return rec
    if existing:
        rec["verdict"] = "SKIPPED_FOREIGN_GAME"
        rec["foreign_processes"] = existing
        WAVE_STOP = True
        return rec
    free = free_gib()
    rec["ram_at_launch_gib"] = round(free, 2)
    if free < LAUNCH_GIB:
        rec["verdict"] = "SKIPPED_RAM_GATE"
        return rec
    log_path = cell / "batch_stdout.log"
    log_handle = log_path.open("w", encoding="utf-8", errors="replace")
    proc = subprocess.Popen(cmd, cwd=str(WORKTREE), env=env,
                            stdout=log_handle, stderr=subprocess.STDOUT, text=True)
    low = free
    high = free
    ram_samples = []
    game_rss_peak = 0
    owned_game_pids = None
    deadline = time.time() + CELL_WALL_BACKSTOP_S
    while proc.poll() is None:
        time.sleep(5)
        f = free_gib()
        low = min(low, f)
        high = max(high, f)
        try:
            processes = openra_processes()
        except Exception as e:
            rec["verdict"] = "ABORTED_PROCESS_MONITOR_ERROR"
            rec["error"] = str(e)
            WAVE_STOP = True
            kill_tree(proc)
            break
        pids = {p["pid"] for p in processes}
        if pids:
            if owned_game_pids is None:
                owned_game_pids = pids
            elif not pids.issubset(owned_game_pids):
                rec["verdict"] = "ABORTED_UNEXPECTED_GAME_PROCESS"
                rec["unexpected_processes"] = [p for p in processes
                                               if p["pid"] not in owned_game_pids]
                WAVE_STOP = True
                kill_tree(proc)
                break
        game_rss = sum(p["rss_kib"] for p in processes)
        game_rss_peak = max(game_rss_peak, game_rss)
        ram_samples.append({"elapsed_s": int(time.time() - rec["t0"]),
                            "free_gib": round(f, 2), "game_rss_kib": game_rss,
                            "game_processes": processes})
        if f < ABORT_GIB:
            rec["verdict"] = "ABORTED_RAM_FLOOR"
            WAVE_STOP = True
            kill_tree(proc)
            break
        if time.time() > deadline:
            rec["verdict"] = "ABORTED_WALL_BACKSTOP"
            WAVE_STOP = True
            kill_tree(proc)
            break
    rec["ram_min_gib"] = round(low, 2)
    rec["ram_max_gib"] = round(high, 2)
    rec["ram_footprint_gib"] = round(free - low, 2)  # implied game+overhead footprint
    rec["wall_s"] = int(time.time() - rec["t0"])
    if proc.poll() is None:
        proc.wait(timeout=10)
    rec["exit"] = proc.returncode
    log_handle.close()
    rec["game_rss_peak_gib"] = round(game_rss_peak / (1024 * 1024), 3)
    rec["game_pids"] = sorted(owned_game_pids or [])
    (cell / "ram_profile.json").write_text(json.dumps(ram_samples))
    replays = glob.glob(str(cell / "Replays" / "cameo" / "{DEV_VERSION}" / "*.orarep"))
    rec["replay"] = replays[-1] if replays else None
    if rec["verdict"] is None:
        rec["verdict"] = "COMPLETED" if proc.returncode == 0 and rec["replay"] else "NO_REPLAY"
    return rec


PROOF_VERDICTS = ("IDENTICAL", "IDENTICAL_TAIL_FLUSH")


def payload_is_proof(payload, rep_a, rep_b):
    """PASS shape (REVIEW_2026-10-08_wave5_adjudication): a PASS requires exit 0
    AND explicit proof verdict AND complete well-formed capture metadata.
    All of these must hold for BOTH replays: expected pair keys present in
    records/incomplete/unparsed_tails, record counts positive ints (bool is not
    int), every incomplete flag explicitly False, every unparsed-tail count 0.
    Missing/extra keys, wrong types, or absent fields => not proof."""
    if not isinstance(payload, dict):
        return False
    if payload.get("verdict") not in PROOF_VERDICTS:
        return False
    expected = {str(rep_a), str(rep_b)}
    for key in ("records", "incomplete", "unparsed_tails"):
        sub = payload.get(key)
        if not isinstance(sub, dict) or set(sub.keys()) != expected:
            return False
    if not all(isinstance(n, int) and not isinstance(n, bool) and n > 0
               for n in payload["records"].values()):
        return False
    if not all(v is False for v in payload["incomplete"].values()):
        return False
    if not all(isinstance(t, int) and not isinstance(t, bool) and t == 0
               for t in payload["unparsed_tails"].values()):
        return False
    return True


def compare(rep_a, rep_b, cell_tag):
    """Run the frozen comparator; PASS iff exit 0 AND proof verdict AND
    well-formed payload. Full stdout/stderr/parsed payload are persisted per
    pair under RUN_RESULTS — snippets in the summary are diagnostic only."""
    pair_dir = RUN_RESULTS / "comparator"
    pair_dir.mkdir(parents=True, exist_ok=True)
    stem = re.sub(r"[^A-Za-z0-9_.-]+", "_", cell_tag)
    try:
        p = subprocess.run([sys.executable, "tools/ai/order_stream_diff.py", rep_a, rep_b, "--json"],
                           cwd=str(WORKTREE), capture_output=True, text=True, timeout=60)
    except (subprocess.TimeoutExpired, OSError) as e:
        (pair_dir / f"{stem}.exception.txt").write_text(repr(e))
        return {"pair": cell_tag, "verdict": "COMPARATOR_TIMEOUT", "exit": None,
                "ok": False, "raw": str(e)[:2000]}
    (pair_dir / f"{stem}.stdout.txt").write_text(p.stdout, errors="replace")
    (pair_dir / f"{stem}.stderr.txt").write_text(p.stderr, errors="replace")
    payload = None
    for l in p.stdout.strip().splitlines():
        try:
            candidate = json.loads(l)
            if isinstance(candidate, dict) and "verdict" in candidate:
                payload = candidate
        except json.JSONDecodeError:
            continue
    if payload is None:
        return {"pair": cell_tag, "verdict": "COMPARATOR_MALFORMED_OUTPUT",
                "exit": p.returncode, "ok": False,
                "raw": p.stdout[:2000], "stderr": p.stderr[:1000],
                "artifacts": {"stdout": str(pair_dir / f"{stem}.stdout.txt"),
                              "stderr": str(pair_dir / f"{stem}.stderr.txt")}}
    (pair_dir / f"{stem}.payload.json").write_text(
        json.dumps(payload, indent=2, sort_keys=True))
    ok = p.returncode == 0 and payload_is_proof(payload, rep_a, rep_b)
    # Any exit != 0 stays a non-PASS: wave stop + invalid attribution. Exit 0
    # with a proof verdict but malformed capture metadata is UNPROVEN — not
    # PASS, not DIVERGENT: the comparator claimed identity on evidence we
    # cannot fully validate, which itself needs review.
    if p.returncode == 0 and payload.get("verdict") in PROOF_VERDICTS and not ok:
        verdict = "UNPROVEN_EVIDENCE"
    elif p.returncode in (0, 1, 3, 4):
        verdict = payload["verdict"]
    else:
        verdict = "COMPARATOR_ERROR"
    return {"pair": cell_tag, "verdict": verdict, "exit": p.returncode, "ok": ok,
            "incomplete": payload.get("incomplete"),
            "unparsed_tails": payload.get("unparsed_tails"),
            "records": payload.get("records"),
            "sha256_16": payload.get("sha256_16"),
            "artifacts": {"stdout": str(pair_dir / f"{stem}.stdout.txt"),
                          "stderr": str(pair_dir / f"{stem}.stderr.txt"),
                          "payload": str(pair_dir / f"{stem}.payload.json")},
            "raw": p.stdout[:2000], "stderr": p.stderr[:1000]}


def cell_dir_for(seed, map_path, repeat):
    return (WORKER_ROOT / f"parity-wave1-{RUN_ID}-w1"
            / f"s{seed}-{Path(map_path).name}-r{repeat}")


BOUNDED_PASS = {"IDENTICAL", "IDENTICAL_OUTCOME_BOUNDED_TAIL"}


def _variant_map_dir(cell_dir):
    """The cell's --keep-variants map tree: first dir under
    <cell>/maps/**/ containing map.yaml (roster source for the boundary
    comparator)."""
    maps = Path(cell_dir) / "maps"
    if not maps.is_dir():
        return None
    for f in sorted(maps.rglob("map.yaml")):
        return str(f.parent)
    return None


def bounded_compare(rep_a, rep_b, cell_tag, map_dir):
    """Phase-A outcome-bounded comparator (order_stream_diff_bounded.py) —
    SECOND LAYER, additive only. The strict comparator verdict remains the
    mechanical gate; the bounded verdict is recorded per pair for review
    and retro-adjudication and does not itself flip acceptance until the
    coordinator promotes it after dev/Sol/Luna sign-off."""
    pair_dir = RUN_RESULTS / "comparator"
    pair_dir.mkdir(parents=True, exist_ok=True)
    stem = re.sub(r"[^A-Za-z0-9_.-]+", "_", cell_tag)
    # The bounded tool is NEW post-freeze tooling — resolve it beside this
    # scheduler file, not inside the frozen WORKTREE (which pins a9349d015
    # for capture evidence only).
    tool = Path(__file__).resolve().parent / "order_stream_diff_bounded.py"
    import hashlib
    tool_sha = hashlib.sha256(tool.read_bytes()).hexdigest() \
        if tool.is_file() else "MISSING"
    out = {"tool_sha256": tool_sha, "map": map_dir}
    if map_dir is None:
        out.update(verdict="BOUNDED_NO_MAP", exit=None)
        return out
    if not tool.is_file():
        out.update(verdict="BOUNDED_TOOL_MISSING", exit=None)
        return out
    try:
        p = subprocess.run(
            [sys.executable, str(tool),
             rep_a, rep_b, "--map", map_dir, "--json"],
            cwd=str(WORKTREE), capture_output=True, text=True, timeout=120)
    except (subprocess.TimeoutExpired, OSError) as e:
        (pair_dir / f"{stem}.bounded.exception.txt").write_text(repr(e))
        out.update(verdict="BOUNDED_TOOL_ERROR", exit=None)
        return out
    (pair_dir / f"{stem}.bounded.stdout.txt").write_text(
        p.stdout, errors="replace")
    (pair_dir / f"{stem}.bounded.stderr.txt").write_text(
        p.stderr, errors="replace")
    payload = None
    try:
        candidate = json.loads(p.stdout)
        if isinstance(candidate, dict) and "verdict" in candidate:
            payload = candidate
    except json.JSONDecodeError:
        for l in p.stdout.strip().splitlines():
            try:
                candidate = json.loads(l)
                if isinstance(candidate, dict) and "verdict" in candidate:
                    payload = candidate
            except json.JSONDecodeError:
                continue
    if payload is None:
        out.update(verdict="BOUNDED_MALFORMED", exit=p.returncode)
        return out
    (pair_dir / f"{stem}.bounded.payload.json").write_text(
        json.dumps(payload, indent=2, sort_keys=True))
    out.update(verdict=payload["verdict"], exit=p.returncode,
               B=payload.get("B"),
               f_term=(payload.get("a_boundary") or {}).get("f_term"),
               m_term=(payload.get("a_boundary") or {}).get("m_term"),
               tail_extras=payload.get("tail_extras"),
               ok=p.returncode == 0 and payload["verdict"] in BOUNDED_PASS,
               artifacts={"stdout": str(pair_dir / f"{stem}.bounded.stdout.txt"),
                          "payload": str(pair_dir / f"{stem}.bounded.payload.json")})
    return out


VOLATILE_MATCH_KEYS = {"record_id", "recorded_utc", "game_uid"}


def match_records_identical(cell_dir_a, cell_dir_b):
    """Compare cameo-ai-matches.jsonl for two cells, modulo volatile fields.
    Returns (identical: bool, detail: str). The match record is an independent
    evidence stream: duration_ticks/outcome/stats/arsenal/order_gate all equal
    means the GAME played identically regardless of replay-file flush timing."""
    recs = []
    for cell_dir in (cell_dir_a, cell_dir_b):
        p = Path(cell_dir) / "Logs" / "cameo-ai-matches.jsonl"
        if not p.is_file():
            return False, f"missing {p}"
        lines = []
        for line in p.read_text(encoding="utf-8", errors="replace").splitlines():
            line = line.strip()
            if not line:
                continue
            try:
                d = json.loads(line)
            except json.JSONDecodeError:
                return False, f"unparseable match record in {p}"
            for k in VOLATILE_MATCH_KEYS:
                d.pop(k, None)
            lines.append(d)
        if not lines:
            return False, f"no match records in {p}"  # empty != identical
        recs.append(lines)
    if len(recs[0]) != len(recs[1]):
        return False, f"record count differs: {len(recs[0])} vs {len(recs[1])}"
    key = lambda d: json.dumps(d, sort_keys=True)
    if sorted(map(key, recs[0])) == sorted(map(key, recs[1])):
        return True, f"{len(recs[0])} match records identical modulo volatile fields"
    return False, "match records differ beyond volatile fields"


def _load_comparator():
    """Import the frozen comparator module for full-stream diffing."""
    import importlib.util
    spec = importlib.util.spec_from_file_location(
        "order_stream_diff", str(WORKTREE / "tools" / "ai" / "order_stream_diff.py"))
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def adjudicate(pair_cells, cmp_rec, rep_a, rep_b):
    """Decide whether a strict comparator non-identity is a proven capture-layer
    artifact. FULL multiset diff over the whole canonical stream — the strict
    CLI's only_a/only_b cover the first differing frame only, so this recomputes
    complete extras per side via the frozen comparator's own extract().

    Sound rule (Sol + Architect reviewed, REVIEW_2026-10-08_wave5_adjudication):
    exactly one side holds unmatched records AND all extras are pure SYNCHASH
    strictly past the shorter capture's last frame AND match records are
    nonempty and identical modulo volatile fields. Any tail ORDER is unprovable
    (external client orders aren't sim-deterministic) and stays DIVERGENT.
    ADJUDICATED_* labels record the mechanical finding only; they do not by
    themselves assert BASE proof — acceptance requires reviewer sign-off."""
    if cmp_rec.get("exit") != 1:
        return "DIVERGENT", (f"adjudication only applies to a clean divergent "
                             f"run; got exit={cmp_rec.get('exit')} "
                             f"verdict={cmp_rec.get('verdict')}")
    inc = cmp_rec.get("incomplete")
    recs = cmp_rec.get("records")
    if not isinstance(inc, dict) or any(inc.values()):
        return "DIVERGENT", f"incomplete capture flagged: {inc}"
    expected = {str(rep_a), str(rep_b)}
    if (not isinstance(recs, dict) or set(recs.keys()) != expected
            or not all(isinstance(n, int) and not isinstance(n, bool) and n > 0
                       for n in recs.values())):
        return "DIVERGENT", f"missing/nonpositive record counts: {recs}"
    try:
        osd = _load_comparator()
        ra, unp_a, inc_a = osd.extract(rep_a, False, False, False)
        rb, unp_b, inc_b = osd.extract(rep_b, False, False, False)
    except Exception as e:
        return "DIVERGENT", f"full-diff extract failed: {e}"
    if inc_a or inc_b:
        return "DIVERGENT", f"capture incomplete under direct extract: {inc_a}/{inc_b}"
    if unp_a or unp_b:
        return "DIVERGENT", f"unparsed tails: {unp_a}/{unp_b}"
    import collections
    ea = collections.Counter(ra) - collections.Counter(rb)
    eb = collections.Counter(rb) - collections.Counter(ra)
    if ea and eb:
        return "DIVERGENT", "both sides hold unmatched records — real divergence"
    if not ea and not eb:
        return "DIVERGENT", "streams multiset-equal but comparator failed — opaque"
    same, detail = match_records_identical(pair_cells[0], pair_cells[1])
    if not same:
        return "DIVERGENT", f"match-record cross-check failed: {detail}"
    extras = ea or eb
    which = "A" if ea else "B"
    # Canonical records are tuples (client?, frame, kind, payload): r[-3] is
    # the frame and r[-2] the kind regardless of the ignore_client head.
    last_frame = max(r[-3] for r in (rb if ea else ra)) if (rb if ea else ra) else -1
    kinds = {r[-2] for r in extras}
    frames = sorted(r[-3] for r in extras)
    n_extra = sum(extras.values())
    # Narrow rule (Sol/Architect review, REVIEW_2026-10-08_wave5_adjudication):
    # only pure-SYNCHASH extras strictly past the shorter capture's last frame
    # can adjudicate. Any ORDER/DISCONNECT/other in the tail is UNPROVABLE
    # (external client orders are not sim-deterministic) and stays DIVERGENT.
    if kinds != {"SYNCHASH"}:
        return "DIVERGENT", (f"side {which}: tail contains non-sync records "
                             f"{kinds} — unprovable content; {detail}")
    if not all(f > last_frame for f in frames):
        return "DIVERGENT", (f"side {which}: extras not strictly past shorter "
                             f"stream's last frame f{last_frame}; {detail}")
    return "ADJUDICATED_SYNC_DROP", (
        f"side {which}: {n_extra} one-sided SYNCHASH record(s) frames "
        f"{frames[0]}..{frames[-1]} past f{last_frame}; {detail}")


def main_locked():
    global WAVE_STOP
    if not (WORKTREE / "tools/ai/run_ai_match_batch.py").is_file():
        sys.exit(f"frozen worktree missing: {WORKTREE}")
    bad = verify_tools()
    if bad:
        sys.exit("tool hash MISMATCH at frozen head: " + "; ".join(bad))
    RESULTS_ROOT.mkdir(parents=True, exist_ok=True)
    try:
        foreign_count = foreign_openra_count()
    except Exception as e:
        sys.exit(f"cannot verify machine-wide OpenRA count; refuse to launch: {e}")
    if foreign_count:
        sys.exit(f"{foreign_count} foreign OpenRA instance(s) already running — refuse to launch")
    results, compares = [], []
    wave_stop = False
    stop_reason = None
    # Strictly serial repeats ensure a failed first capture sets the whole-wave
    # stop before the next same-BASE cell can be dequeued.
    for s in SEEDS:
        for m in MAPS:
            pair = []
            for r in REPEATS:
                cell_result = run_cell("w1", s, m, r)
                results.append(cell_result)
                pair.append(cell_result)
                if cell_result["verdict"] != "COMPLETED":
                    wave_stop = True
                    WAVE_STOP = True
                    stop_reason = f"cell {cell_result['cell']} {cell_result['verdict']}"
                    break
            if wave_stop:
                break
            c = compare(pair[0]["replay"], pair[1]["replay"],
                        f"s{s}-{Path(m).name}")
            # Phase-A second layer (additive): bounded verdict recorded
            # alongside the strict gate — acceptance promotion pending
            # dev/Sol/Luna sign-off per coordinator sequencing.
            c["bounded"] = bounded_compare(
                pair[0]["replay"], pair[1]["replay"],
                f"s{s}-{Path(m).name}",
                _variant_map_dir(cell_dir_for(s, m, REPEATS[0])))
            print(f"[pair s{s}-{Path(m).name}] bounded: "
                  f"{c['bounded']['verdict']} (B={c['bounded'].get('B')})",
                  flush=True)
            if not c["ok"]:
                if c["exit"] == 1:
                    # Comparator ran clean and claims in-window divergence —
                    # try the narrow capture-artifact adjudication.
                    adj, detail = adjudicate(
                        [cell_dir_for(s, m, REPEATS[0]), cell_dir_for(s, m, REPEATS[1])],
                        c, pair[0]["replay"], pair[1]["replay"])
                    c["adjudication"] = detail
                    c["adjudicated_verdict"] = adj
                    if adj.startswith("ADJUDICATED_"):
                        print(f"[pair s{s}-{Path(m).name}] comparator {c['verdict']} -> "
                              f"{adj} ({detail})", flush=True)
                        compares.append(c)
                        continue
                else:
                    # Timeout, exit 3/4, malformed output, or an exit-0 proof
                    # claim on invalid metadata: unusable evidence — every
                    # exit != 0 propagates as wave-stop-invalid.
                    detail = (f"comparator evidence invalid: {c['verdict']} "
                              f"(exit {c['exit']})")
                    c["adjudicated_verdict"] = "INVALID_EVIDENCE"
                compares.append(c)
                print(f"[pair s{s}-{Path(m).name}] comparator: {c['verdict']} "
                      f"(exit {c['exit']}) — NOT adjudicated: {detail}", flush=True)
                wave_stop = True
                WAVE_STOP = True
                stop_reason = f"same-BASE pair {c['pair']} {c['verdict']}: {detail}"
                print(f"BASE attribution invalid; stopping wave: {stop_reason}", flush=True)
                break
            else:
                print(f"[pair s{s}-{Path(m).name}] comparator: {c['verdict']} "
                      f"(exit {c['exit']})", flush=True)
            compares.append(c)
        if wave_stop:
            break
    summary = {"cells": results, "comparisons": compares, "wave_stop": wave_stop,
               "stop_reason": stop_reason, "max_workers": MAX_WORKERS,
               "matrix": {"seeds": SEEDS, "maps": MAPS, "repeats": REPEATS}}
    (RUN_RESULTS / "wave1_summary.json").write_text(json.dumps(summary, indent=1))
    print(json.dumps(summary, indent=1))
    bad_cmp = [c for c in compares if c["exit"] != 0]
    if bad_cmp or wave_stop:
        print("WAVE1 STOP: capture/comparator invalid or aborted — strict attribution INVALID",
              file=sys.stderr)
        sys.exit(1)
    print("WAVE1 COMPLETE: all repeated BASE cells identical")


def main():
    RESULTS_ROOT.mkdir(parents=True, exist_ok=True)
    lock_fd = None
    try:
        lock_fd = acquire_scheduler_lock()
        RUN_RESULTS.mkdir(parents=True, exist_ok=False)
        return main_locked()
    except RuntimeError as e:
        sys.exit(str(e))
    finally:
        if lock_fd is not None:
            os.close(lock_fd)
            try:
                LOCK_PATH.unlink()
            except FileNotFoundError:
                pass


if __name__ == "__main__":
    main()
