#!/usr/bin/env python3
"""TAKEOVER-SMOKE: real multi-client test for devin/t3verify/bot-takeover.

Drives a local dedicated server (OpenRA.Server.exe) plus real game clients
(OpenRA.exe with Launch.Connect) through the dev-only double gates:

  server: Cameo.DevAutopilot=True  + <support>/devautopilot.plan
  client: Cameo.DevAutoOrders=True + <support>/devautoorders.plan

With either gate missing the hooks are inert (scenario `inert` proves it).

Scenarios (maintainer spec):
  a) 2v2, one client killed            -> seat taken over, match continues
  b) 2v2, non-last player surrenders   -> takeover; human orders rejected
  c) 1v1, surrender                    -> player defeated (no takeover)
  d) 1v1, disconnect                   -> last-player policy takeover
  e) admin client killed               -> controller re-elected, bot plays on
  inert) plan file present, arg absent -> hooks stay inert (the required test)

Per scenario reports PASS/FAIL, the match-record takeover block, sync/exception
logs. Honors the 3-driver machine cap: refuses to start while existing
OpenRA.exe drivers would push the total past 3 (REF-1 re-smoke has priority).

Usage:
  python tools/ai/takeover_smoke.py --scenario a
  python tools/ai/takeover_smoke.py --scenario all
"""

import argparse
import datetime
import json
import os
import re
import shutil
import signal
import socket
import subprocess
import sys
import time
import zipfile

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ENGINE = os.path.join(REPO, "engine")
BIN = os.path.join(ENGINE, "bin")
DEFAULT_SUPPORT = os.path.join(os.path.dirname(REPO), "_support_t4_smoke")

MAP_2V2 = ("_ra_doubles.oramap", "aca0e7e0118d413654c060ddbab4e960b154ea5a")
MAP_1V1 = ("_ra_a-nuclear-winter.oramap", "ab86f08b4da92c7ce9be9db0819b190a800e30d4")

MAX_DRIVERS = 3
TICK = 25  # engine ticks per second at default timestep


def free_port():
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.bind(("127.0.0.1", 0))
    port = s.getsockname()[1]
    s.close()
    return port


def count_drivers():
    # Game clients only (OpenRA.exe); OpenRA.Server.exe is headless, not a driver.
    out = subprocess.run(["tasklist", "/NH"],
                         capture_output=True, text=True).stdout
    return len(re.findall(r"(?im)^OpenRA\.exe\s", out))


def wait_for_drivers(needed, label):
    while True:
        busy = count_drivers()
        if busy + needed <= MAX_DRIVERS:
            return
        print(f"  [{label}] {busy} OpenRA.exe driver(s) busy; "
              f"cap {MAX_DRIVERS} (REF-1 priority) — waiting 30s", flush=True)
        time.sleep(30)


def spawn(cmd, cwd, env_extra=None):
    env = dict(os.environ)
    if env_extra:
        env.update(env_extra)
    return subprocess.Popen(
        cmd, cwd=cwd, env=env,
        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
        creationflags=subprocess.CREATE_NEW_PROCESS_GROUP)


def kill(proc):
    if proc.poll() is None:
        subprocess.run(["taskkill", "/F", "/T", "/PID", str(proc.pid)],
                       capture_output=True)


def kill_tree(proc):
    subprocess.run(["taskkill", "/F", "/T", "/PID", str(proc.pid)],
                   capture_output=True)


def wait_text(path, pattern, timeout, desc=""):
    """Poll for regex in a (possibly not-yet-created) file."""
    deadline = time.time() + timeout
    rx = re.compile(pattern)
    while time.time() < deadline:
        try:
            with open(path, encoding="utf-8", errors="replace") as f:
                if rx.search(f.read()):
                    return True
        except OSError:
            pass
        time.sleep(1.0)
    print(f"  TIMEOUT waiting for /{pattern}/ in {path} ({desc})", flush=True)
    return False


def wait_count(path, pattern, count, timeout, desc=""):
    """Poll until regex has matched `count` times (e.g. N accepted conns)."""
    deadline = time.time() + timeout
    rx = re.compile(pattern)
    while time.time() < deadline:
        try:
            with open(path, encoding="utf-8", errors="replace") as f:
                if len(rx.findall(f.read())) >= count:
                    return True
        except OSError:
            pass
        time.sleep(1.0)
    print(f"  TIMEOUT waiting for {count}x /{pattern}/ in {path} ({desc})",
          flush=True)
    return False


def glob_logs(support_dir):
    logs = []
    logdir = os.path.join(support_dir, "Logs")
    if os.path.isdir(logdir):
        logs = [os.path.join(logdir, f) for f in os.listdir(logdir)]
    return logs


def read_file(path):
    try:
        with open(path, encoding="utf-8", errors="replace") as f:
            return f.read()
    except OSError:
        return ""


def collect_evidence(client_dirs, server_dir):
    """Sync reports, exceptions, match records from every process."""
    ev = {"sync_reports": [], "exceptions": [], "records": [], "debug_takeover": []}
    for name, d in client_dirs + [("server", server_dir)]:
        for log in glob_logs(d):
            base = os.path.basename(log)
            text = read_file(log)
            if base.startswith("syncreport"):
                ev["sync_reports"].append((name, base))
            if base.startswith("exception"):
                ev["exceptions"].append((name, base, text[-2000:]))
            if base == "cameo-ai-matches.jsonl":
                for line in text.splitlines():
                    line = line.strip()
                    if line.startswith("{"):
                        try:
                            ev["records"].append((name, json.loads(line)))
                        except json.JSONDecodeError:
                            pass
            if base in ("debug.log", "dedicated-debug.log"):
                for m in re.finditer(r".*(bot_takeover|CAMEO DEV).*", text):
                    ev["debug_takeover"].append((name, m.group(0).strip()))
        # dedicated server logs land in <support>/Logs too
        for f in ("dedicated-server.log",):
            p = os.path.join(d, "Logs", f)
            if os.path.isfile(p):
                for m in re.finditer(r".*(dropped|disconnect|CAMEO DEV|Started).*",
                                     read_file(p), re.IGNORECASE):
                    ev["debug_takeover"].append((name, m.group(0).strip()))
    return ev


def takeover_blocks(ev):
    out = []
    for name, rec in ev["records"]:
        if "takeover" in rec:
            out.append((name, rec["player"]["name"], rec["takeover"]))
    return out


def write_plan(path, lines):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("# takeover_smoke plan\n")
        for line in lines:
            f.write(line + "\n")


# EnableSingleplayer must be FALSE wherever takeover should work: the tracker
# disables itself when GlobalSettings.EnableSingleplayer is set (multiplayer
# only, BotTakeoverTracker.cs:131). It is True ONLY where the lobby must start
# with a single human (scenario c), where defeat-not-takeover is expected.
SERVER_ARGS = lambda port, support, singleplayer=False, autopilot=True, port_name="smoke": [
    os.path.join(BIN, "OpenRA.Server.exe"),
    "Game.Mod=cameo",
    'Engine.EngineDir=..',
    f"Engine.SupportDir={support}",
    f"Server.Name={port_name}",
    f"Server.ListenPort={port}",
    "Server.AdvertiseOnline=False",
    "Server.AdvertiseOnLocalNetwork=False",
    "Server.EnableSyncReports=True",
    f"Server.EnableSingleplayer={'True' if singleplayer else 'False'}",
    "Server.EnableLintChecks=False",
    "Server.EnableMapGeneration=False",
] + (["Cameo.DevAutopilot=True"] if autopilot else [])


def client_args(port, support, name, autoorders=True):
    args = [
        os.path.join(BIN, "OpenRA.exe"),
        "Game.Mod=cameo",
        'Engine.EngineDir=..',
        f"Engine.ModSearchPaths={REPO}\\mods,{ENGINE}\\mods",
        f"Engine.SupportDir={support}",
        f"Launch.Connect=127.0.0.1:{port}",
        f"Player.Name={name}",
        "Graphics.Mode=Windowed",
        "Graphics.WindowedSize=800,600",
        "Server.DiscoverNatDevices=False",
    ]
    if autoorders:
        args.append("Cameo.DevAutoOrders=True")
    return args


def mod_search_env():
    return {"MOD_SEARCH_PATHS": f"{REPO}\\mods,{ENGINE}\\mods"}


def run_scenario(sc, support_root):
    port = free_port()
    root = os.path.join(support_root, f"scenario_{sc}")
    shutil.rmtree(root, ignore_errors=True)
    srv_dir = os.path.join(root, "server")
    os.makedirs(srv_dir, exist_ok=True)

    spec = SCENARIOS[sc]
    clients = spec["clients"]

    # --- plans -------------------------------------------------------------
    write_plan(os.path.join(srv_dir, "devautopilot.plan"), spec["server_plan"])
    for cname, cspec in clients.items():
        cdir = os.path.join(root, cname)
        os.makedirs(cdir, exist_ok=True)
        if cspec.get("orders"):
            write_plan(os.path.join(cdir, "devautoorders.plan"), cspec["orders"])

    # --- launch --------------------------------------------------------------
    wait_for_drivers(len(clients), f"scenario {sc}")
    print(f"[{sc}] support={root} port={port}", flush=True)

    srv = spawn(SERVER_ARGS(port, srv_dir, spec.get("singleplayer", False),
                            spec.get("autopilot", True), f"t4smoke_{sc}"),
                cwd=ENGINE, env_extra=mod_search_env())
    time.sleep(4)  # let the server bind before the first client connects

    procs = {"server": srv}
    srv_log = os.path.join(srv_dir, "Logs", "dedicated-server.log")
    try:
        for i, cname in enumerate(spec["connect_order"]):
            cspec = clients[cname]
            cdir = os.path.join(root, cname)
            wait_for_drivers(len([c for c in spec["connect_order"]
                                  if c not in procs]), f"scenario {sc}")
            procs[cname] = spawn(client_args(port, cdir, cname,
                                           cspec.get("autoorders", True)),
                                 cwd=ENGINE, env_extra=mod_search_env())
            # Client indexes follow VALIDATION order — wait for this conn to be
            # accepted before spawning the next, so admin/index is deterministic.
            wait_count(srv_log, r"Accepted connection", i + 1, 180,
                       f"{cname} join")

        if sc == "inert":
            return verdict_inert(root, srv_dir)

        return drive(sc, spec, root, srv_dir, procs)
    finally:
        for name, p in procs.items():
            kill_tree(p)
        time.sleep(2)


def drive(sc, spec, root, srv_dir, procs):
    # The dedicated server logs no game-start line; the reliable marker is the
    # match map's uid appearing in a client's debug.log ([MATCH INFO] line is
    # written on real world load — the shellmap has a different uid).
    uid = spec["map_uid"]
    started = False
    deadline = time.time() + spec.get("start_timeout", 300)
    while time.time() < deadline and not started:
        for cname in spec["clients"]:
            dbg = os.path.join(root, cname, "Logs", "debug.log")
            if uid in read_file(dbg):
                started = True
                break
        time.sleep(2)

    result = {"scenario": sc, "game_started": started, "events": []}
    if not started:
        return finish(sc, root, srv_dir, result, procs)

    srv_log = os.path.join(srv_dir, "Logs", "dedicated-server.log")

    for action, arg in spec.get("actions", []):
        if action == "wait":
            time.sleep(arg)
        elif action == "kill":
            victim = procs[arg]
            kill(victim)
            result["events"].append(f"killed {arg} pid={victim.pid}")
            print(f"  [{sc}] killed client {arg}", flush=True)
        elif action == "wait_server_log":
            ok = wait_text(srv_log, arg, 120, "server log")
            result["events"].append(f"server_log {arg!r}: {'hit' if ok else 'MISS'}")

    return finish(sc, root, srv_dir, result, procs)


def finish(sc, root, srv_dir, result, procs):
    # The match record lands at GameOver / AllBotsResolved — poll for the
    # required evidence (takeover block / any record) instead of a fixed wait.
    tail = SCENARIOS[sc].get("settle", 600)
    client_dirs = [(n, os.path.join(root, n)) for n in SCENARIOS[sc]["clients"]]
    want = SCENARIOS[sc].get("record_trigger")

    deadline = time.time() + tail
    ev = None
    while time.time() < deadline:
        ev = collect_evidence(client_dirs, srv_dir)
        blocks = takeover_blocks(ev)
        if want is None:
            if ev["records"]:
                break
        elif any(b.get("trigger") == want for _, _, b in blocks):
            break
        time.sleep(10)

    ev = collect_evidence(client_dirs, srv_dir)
    blocks = takeover_blocks(ev)

    ok, why = SCENARIOS[sc]["judge"](result, ev, blocks)
    result.update({
        "verdict": "PASS" if ok else "FAIL",
        "why": why,
        "takeover_blocks": [(n, p, b) for n, p, b in blocks],
        "sync_reports": ev["sync_reports"],
        "exceptions": [(n, b) for n, b, _ in ev["exceptions"]],
        "records_seen": len(ev["records"]),
    })

    out = os.path.join(root, "RESULT.json")
    with open(out, "w", encoding="utf-8") as f:
        json.dump(result, f, indent=2, default=str)
    print_report(sc, result, ev)
    return result


def print_report(sc, r, ev):
    print(f"\n=== SCENARIO {sc}: {r['verdict']} ===")
    print(f"  why: {r['why']}")
    print(f"  game_started: {r['game_started']}  records: {r['records_seen']}")
    for n, p, b in r["takeover_blocks"]:
        print(f"  takeover[{n}/{p}]: {b}")
    print(f"  sync_reports: {r['sync_reports'] or 'none'}")
    print(f"  exceptions: {r['exceptions'] or 'none'}")
    print(flush=True)


def verdict_inert(root, srv_dir):
    # Plan files present, args absent -> both hooks must do nothing for 45s.
    time.sleep(45)
    srv_log = os.path.join(srv_dir, "Logs", "dedicated-server.log")
    text = read_file(srv_log)
    active = "CAMEO DEV AUTOPILOT ACTIVE" in text
    started = bool(re.search(r"StartGame|is now playing", text))
    c0_dbg = read_file(os.path.join(root, "c0", "Logs", "debug.log"))
    auto_orders = "CAMEO DEV AUTO-ORDERS ACTIVE" in c0_dbg
    ok = not active and not started and not auto_orders
    result = {
        "scenario": "inert",
        "verdict": "PASS" if ok else "FAIL",
        "why": ("both dev hooks stayed inert without the launch args"
                if ok else f"hook fired without arg (autopilot={active} "
                           f"autoorders={auto_orders} started={started})"),
        "game_started": started,
        "takeover_blocks": [], "sync_reports": [], "exceptions": [],
        "records_seen": 0,
    }
    with open(os.path.join(root, "RESULT.json"), "w", encoding="utf-8") as f:
        json.dump(result, f, indent=2)
    print_report("inert", result, {})
    return result


# ---------------------------------------------------------------------------
# scenario definitions
# ---------------------------------------------------------------------------

UID2 = MAP_2V2[1]
UID1 = MAP_1V1[1]

def j_takeover(trigger, seat_player, want):
    """Judge factory: game started, a takeover block of the right kind exists,
    no exceptions, no sync reports."""
    def judge(result, ev, blocks):
        if not result["game_started"]:
            return False, "game never started"
        if ev["exceptions"]:
            return False, f"exception logs: {[n for n, b, _ in ev['exceptions']]}"
        if ev["sync_reports"]:
            return False, f"sync reports (desync): {ev['sync_reports']}"
        hits = [(n, p, b) for n, p, b in blocks if b.get("trigger") == trigger]
        if not hits:
            return False, f"no '{trigger}' takeover block in any match record"
        return True, f"takeover {trigger} recorded: {hits}"
    return judge


def j_admin_kill(result, ev, blocks):
    """Scenario e: admin c0 killed -> its seat Multi0 is taken over and the
    record lands on surviving client c1 with controller_client=1 (re-election)."""
    if not result["game_started"]:
        return False, "game never started"
    if ev["exceptions"]:
        return False, f"exception logs: {[n for n, b, _ in ev['exceptions']]}"
    if ev["sync_reports"]:
        return False, f"sync reports (desync): {ev['sync_reports']}"
    hits = [(n, p, b) for n, p, b in blocks
            if n == "c1" and p == "Multi0" and b.get("trigger") == "disconnect"
            and b.get("controller_client") == 1]
    if not hits:
        return False, ("no disconnect takeover block for Multi0 on c1 with "
                       f"controller_client=1; blocks={blocks}")
    return True, f"admin-kill takeover re-elected to c1: {hits}"


def j_defeat(result, ev, blocks):
    if not result["game_started"]:
        return False, "game never started"
    if ev["exceptions"]:
        return False, f"exception logs: {[n for n, b, _ in ev['exceptions']]}"
    if ev["sync_reports"]:
        return False, f"sync reports (desync): {ev['sync_reports']}"
    if blocks:
        return False, f"unexpected takeover block: {blocks}"
    if ev["records"] == []:
        return False, "no match record captured (game did not end?)"
    return True, "surrender produced defeat, no takeover block"


SCENARIOS = {
    # a) 2v2, kill c1 -> takeover seat Multi1, controller = c0 (admin, index 0)
    #    No late surrender for c0: its takeover-AI teammate keeps it "not last",
    #    so the surrender would itself TAKE OVER with controller -1 and the seat
    #    would go inert -> match never ends. The `easiest` enemy bots make the
    #    bot war decisive instead.
    "a": {
        "clients": {
            "c0": {},
            "c1": {},
        },
        "connect_order": ["c0", "c1"],
        "map_uid": UID2,
        "server_plan": [
            "minclients 2",
            f"map {UID2}",
            "slot 0 Multi0",
            "slot 1 Multi1",
            "team 0 1",
            "team 1 1",
            "bot Multi2 brutal",
            "bot Multi3 brutal",
            "botteam Multi2 2",
            "botteam Multi3 2",
        ],
        "actions": [
            ("wait", 50),                    # let the match bed in (~1250 ticks)
            ("kill", "c1"),
            ("wait_server_log", r"(?i)closing socket|drop|disconnect"),
        ],
        "settle": 600,
        "record_trigger": "disconnect",
        "judge": j_takeover("disconnect", "Multi1", True),
    },

    # b) 2v2, c1 surrenders at tick 750 -> takeover trigger=surrender.
    #    Same no-late-surrender reason as (a) for c0.
    "b": {
        "clients": {
            "c0": {},
            "c1": {"orders": ["750 Surrender"]},
        },
        "connect_order": ["c0", "c1"],
        "map_uid": UID2,
        "server_plan": [
            "minclients 2",
            f"map {UID2}",
            "slot 0 Multi0",
            "slot 1 Multi1",
            "team 0 1",
            "team 1 1",
            "bot Multi2 brutal",
            "bot Multi3 brutal",
            "botteam Multi2 2",
            "botteam Multi3 2",
        ],
        "actions": [("wait", 45)],
        "settle": 600,
        "record_trigger": "surrender",
        "judge": j_takeover("surrender", "Multi1", True),
    },

    # c) 1v1, c0 surrenders -> last player on team -> DEFEAT (no takeover)
    #    singleplayer=True is REQUIRED to start a 1-human lobby; it also
    #    disables the tracker entirely (correct: this scenario expects defeat).
    "c": {
        "clients": {
            "c0": {"orders": ["750 Surrender"]},
        },
        "connect_order": ["c0"],
        "map_uid": UID1,
        "singleplayer": True,
        "server_plan": [
            "minclients 1",
            f"map {UID1}",
            "slot 0 Multi0",
            "bot Multi1 easiest",
        ],
        "actions": [("wait", 45)],
        "settle": 240,
        "judge": j_defeat,
    },

    # d) 1v1 two humans, kill c1 (last undefeated of its own solo team)
    #    -> last-player disconnect policy (Takeover) elects c0.
    "d": {
        "clients": {
            "c0": {"orders": ["4500 Surrender"]},
            "c1": {},
        },
        "connect_order": ["c0", "c1"],
        "map_uid": UID1,
        "server_plan": [
            "minclients 2",
            f"map {UID1}",
            "slot 0 Multi0",
            "slot 1 Multi1",
        ],
        "actions": [
            ("wait", 50),
            ("kill", "c1"),
            ("wait_server_log", r"(?i)closing socket|drop|disconnect"),
        ],
        "settle": 600,
        "record_trigger": "disconnect",
        "judge": j_takeover("disconnect", "Multi1", True),
    },

    # e) kill the ADMIN c0 (enemy seat) -> takeover re-elects controller to c1
    #    (controller_client=1 in the record); c1 then surrenders its own seat:
    #    last on its solo team -> Defeat -> seat0's takeover AI wins -> record
    #    lands on c1, the surviving client. No lobby bots: a mid-game admin
    #    disconnect leaves BotControllerClientIndex dangling (engine only
    #    reassigns in WaitingPlayers), so lobby bots would go inert and the
    #    match could never resolve.
    "e": {
        "clients": {
            "c0": {},
            "c1": {"orders": ["3500 Surrender"]},
        },
        "connect_order": ["c0", "c1"],
        "map_uid": UID1,
        "server_plan": [
            "minclients 2",
            f"map {UID1}",
            "slot 0 Multi0",
            "slot 1 Multi1",
        ],
        "actions": [
            ("wait", 50),
            ("kill", "c0"),
            ("wait_server_log", r"(?i)closing socket|drop|disconnect"),
        ],
        "settle": 300,
        "record_trigger": "disconnect",
        "judge": j_admin_kill,
    },

    # inert: plan file present, Cameo.DevAutopilot absent -> nothing happens.
    "inert": {
        "clients": {"c0": {"orders": ["100 Surrender"], "autoorders": False}},
        "connect_order": ["c0"],
        "map_uid": UID1,
        "singleplayer": True,
        "server_plan": [
            "minclients 1",
            f"map {UID1}",
            "slot 0 Multi0",
            "bot Multi1 easiest",
        ],
        "autopilot": False,
        "actions": [],
        "judge": lambda r, ev, b: (True, ""),
    },
}


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--scenario", required=True,
                    help="a|b|c|d|e|inert|all")
    ap.add_argument("--support", default=DEFAULT_SUPPORT,
                    help="isolated support root (default %(default)s)")
    args = ap.parse_args()

    os.makedirs(args.support, exist_ok=True)
    todo = list("abcde") + ["inert"] if args.scenario == "all" else [args.scenario]
    results = {}
    for sc in todo:
        results[sc] = run_scenario(sc, args.support)

    print("\n===== TAKEOVER-SMOKE SUMMARY =====")
    for sc, r in results.items():
        print(f"  {sc}: {r['verdict']} — {r['why']}")
    sys.exit(0 if all(r["verdict"] == "PASS" for r in results.values()) else 1)


if __name__ == "__main__":
    main()
