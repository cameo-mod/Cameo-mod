#!/usr/bin/env python3
"""build_coverage.py: build-coverage guard for .orarep replays (BUILD-COVERAGE-GUARD-01).

Reads every seat's PlaceBuilding orders from the replay order stream, so it works on
logging-off builds (no bot log needed). Per seat it reports defences placed and the
first defence tick, the first radar/comms tick, the first tier-2 tick, building coverage
(distinct building types placed / types the rules make available to the faction), and
production buildings, refineries and power placements.

HOLD rules, for Hard-only games of at least MIN_TICKS:
  - any seat with 0 defences;
  - any seat with no tier-2 building by TIER2_DEADLINE_TICK;
  - coverage below --coverage-min, only when a threshold is given. No threshold is set
    yet: it is uncalibrated and must be fixed from known-good games before use.

Replay layout and Order serialization follow tools/ai/order_trace.py (ReplayConnection,
Order.Serialize). Defence and building types come from the mod's rules (Queue: Defence,
Queue: Building) under mods/cameo/ContentPacks, not from a hand-written list.

Exit codes: 0 PASS or NOT_ELIGIBLE, 1 HOLD, 2 unreadable input.
"""
import argparse
import collections
import json
import os
import re
import struct
import sys

MIN_TICKS = 12000
TIER2_DEADLINE_TICK = 9000
HARD_PLUS_DEFAULT = ("hard",)

# Proposed tier-2 markers (not yet confirmed by the owner; see the report).
TIER2_MARKERS = ("radar", "dome", "comm", "techcenter", "techcentre", "research",
                 "advancedheavy", "airstrip", "handofnod")
RADAR_COMMS_MARKERS = ("radar", "dome", "comm")
PRODUCTION_MARKERS = ("barracks", "factory", "handofnod", "airstrip")
REFINERY_MARKERS = ("refinery",)
POWER_MARKERS = ("powerplant", "windtrap", "reactor")

_TOP_KEY = re.compile(r"^(\^?[A-Za-z0-9_\-]+):\s*(#.*)?$")
_QUEUE = re.compile(r"^\s+Queue:\s*(.*)$")
_INHERITS = re.compile(r"^\s+Inherits(?:@\w+)?:\s*(\^?[A-Za-z0-9_\-]+)")
_NET_INTERVAL = re.compile(r"NetFrameInterval:\s*(\d+)")
_FINAL_TICK = re.compile(r"FinalGameTick:\s*(\d+)")
_TWO_PART_PREFIXES = ("td", "ra1", "ts", "ra2", "wc2")
# Whole mod tree: templates such as ^BaseBuilding live in mods/cameo/rules/defaults.yaml.
_DEFAULT_RULES = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "mods", "cameo")


def _read_str(b, p):
    """.NET BinaryWriter string: 7-bit-encoded length, then UTF-8."""
    ln = 0
    shift = 0
    while True:
        byte = b[p]
        p += 1
        ln |= (byte & 0x7F) << shift
        if not (byte & 0x80):
            break
        shift += 7
    return b[p:p + ln].decode("utf-8", "replace"), p + ln


def _orders(pkt):
    """Yield (order_string, subject, target_string) for each Fields order in a packet body."""
    p = 4
    out = []
    while p < len(pkt):
        t = pkt[p]
        p += 1
        if t == 0xFE:  # Handshake: two strings
            _, p = _read_str(pkt, p)
            _, p = _read_str(pkt, p)
            continue
        if t != 0xFF:  # not a Fields order: stop on this packet, as order_trace.py does
            break
        order, p = _read_str(pkt, p)
        flags, = struct.unpack_from("<h", pkt, p)
        p += 2
        subject = None
        tstring = None
        if flags & 0x80:
            subject, = struct.unpack_from("<I", pkt, p)
            p += 4
        if flags & 0x01:  # Target
            tt = pkt[p]
            p += 1
            if tt in (1, 3):  # Actor u32+i32 / FrozenActor u32+u32
                p += 8
            elif tt == 2:  # Terrain
                if flags & 0x40:  # TargetIsCell: i32 + u8
                    p += 5
                else:
                    p += 12
                    n, = struct.unpack_from("<h", pkt, p)
                    p += 2
                    if n != -1:
                        p += 12 * n
            else:
                break  # malformed target type: the engine never emits it
        if flags & 0x04:  # TargetString
            tstring, p = _read_str(pkt, p)
        if flags & 0x02:  # ExtraActors
            n, = struct.unpack_from("<i", pkt, p)
            p += 4 + 4 * n
        if flags & 0x10:  # ExtraLocation
            p += 4
        if flags & 0x20:  # ExtraData
            p += 4
        if flags & 0x100:  # Grouped
            n, = struct.unpack_from("<i", pkt, p)
            p += 4 + 4 * n
        out.append((order, subject, tstring))
    return out


def _players(trailer_text):
    """Parse Player@N blocks from the replay metadata trailer (flat MiniYaml-like text)."""
    players = []
    cur = None
    keys = ("ClientIndex", "Name", "IsBot", "BotType", "FactionId", "Team")
    for line in trailer_text.splitlines():
        if line.startswith("Player@"):
            cur = {}
            players.append(cur)
            continue
        if cur is None:
            continue
        m = re.match(r"^\s*([A-Za-z]+):\s*(.*)$", line)
        if m and m.group(1) in keys and m.group(1) not in cur:
            cur[m.group(1)] = m.group(2).strip()
    for p in players:
        p["IsBot"] = p.get("IsBot", "").lower() == "true"
    return players


def read_stream(path):
    """Return a dict: placements [(frame, subject, name)], last_frame, players, interval, final_tick.

    Packet frames are net frames; world ticks are frame * NetFrameInterval (trailer value,
    1 if absent). FinalGameTick, when present, is the game's last world tick.
    """
    with open(path, "rb") as f:
        data = f.read()
    p = 0
    placements = []
    last_frame = 0
    trailer_start = None
    while p + 8 <= len(data):
        client, = struct.unpack_from("<i", data, p)
        p += 4
        if client == -1:
            trailer_start = p
            break
        plen, = struct.unpack_from("<i", data, p)
        p += 4
        pkt = data[p:p + plen]
        p += plen
        if len(pkt) < 4:
            continue
        frame, = struct.unpack_from("<i", pkt, 0)
        if frame <= 0:
            continue
        if len(pkt) > 4 and pkt[4] in (0xBF, 0x65):  # Disconnect / SyncHash
            continue
        last_frame = max(last_frame, frame)
        for order, subject, tstring in _orders(pkt):
            if order == "PlaceBuilding" and tstring:
                placements.append((frame, subject, tstring))
    trailer = ""
    if trailer_start is not None:
        trailer = data[trailer_start:].decode("utf-8", "replace")
    # NetFrameInterval sits in the replay header; FinalGameTick in the metadata trailer.
    m = _NET_INTERVAL.search(data.decode("latin-1"))
    interval = int(m.group(1)) if m and int(m.group(1)) > 0 else 1
    m = _FINAL_TICK.search(trailer)
    final_tick = int(m.group(1)) if m else None
    return {"placements": placements, "last_frame": last_frame, "players": _players(trailer),
            "interval": interval, "final_tick": final_tick}


def _blocks(lines):
    """Yield (top-level key, indented body lines) for a MiniYaml-style file."""
    name = None
    body = []
    for line in lines:
        m = _TOP_KEY.match(line)
        if m:
            if name is not None:
                yield name, body
            name = m.group(1)
            body = []
        elif name is not None:
            body.append(line)
    if name is not None:
        yield name, body


def load_rules(rules_root):
    """Return (defence_names, building_names) for buildable actors in the mod's yaml.

    Queue values are taken from the actor and from every template it inherits (Inherits:),
    so an actor that gets its Buildable queue from ^Building or ^Defence still counts.
    """
    queues = collections.defaultdict(set)   # key (incl. ^templates) -> Queue values
    inherits = collections.defaultdict(set)  # key -> template names
    for dirpath, _dirs, files in os.walk(rules_root):
        for fn in files:
            if not fn.endswith(".yaml"):
                continue
            with open(os.path.join(dirpath, fn), encoding="utf-8", errors="replace") as f:
                lines = f.read().splitlines()
            for name, body in _blocks(lines):
                for line in body:
                    q = _QUEUE.match(line)
                    if q:
                        queues[name].add(q.group(1).strip().lower())
                    i = _INHERITS.match(line)
                    if i:
                        inherits[name].add(i.group(1))

    memo = {}

    def effective(name, stack=()):
        if name in memo:
            return memo[name]
        if name in stack:
            return set()
        result = set(queues.get(name, ()))
        for parent in inherits.get(name, ()):
            result |= effective(parent, stack + (name,))
        memo[name] = result
        return result

    defences = set()
    buildings = set()
    for name in queues.keys() | inherits.keys():
        if name.startswith("^"):
            continue
        values = effective(name)
        if any("defen" in v for v in values):
            defences.add(name)
        if any("building" in v for v in values):
            buildings.add(name)
    return defences, buildings - defences


def faction_of(name):
    """Faction prefix of an actor name: td_gdi, ra1_soviets, ixian, ... (shared names fall back)."""
    parts = name.split("_")
    if len(parts) >= 3 and parts[0] in _TWO_PART_PREFIXES:
        return parts[0] + "_" + parts[1]
    return parts[0]


def _has(name, markers):
    return any(m in name for m in markers)


def _first(frames):
    return min(frames) if frames else None


def analyse(replay_path, rules_root):
    defences, buildings = load_rules(rules_root)
    stream = read_stream(replay_path)
    interval = stream["interval"]
    players = stream["players"]
    last_tick = (stream["final_tick"] if stream["final_tick"] is not None
                 else stream["last_frame"] * interval)
    known_factions = {p["FactionId"] for p in players if p.get("FactionId")}
    by_subject = collections.defaultdict(list)
    for frame, subject, name in stream["placements"]:
        by_subject[subject].append((frame * interval, name))

    seats = []
    for subject in sorted(by_subject, key=lambda s: (s is None, s)):
        items = by_subject[subject]
        names = [n for _f, n in items]
        # Vote only with names whose prefix is a faction id from the replay metadata, so
        # shared names (e.g. "nuke", "ra1_powerplant") cannot pick the faction.
        votes = [faction_of(n) for n in names if faction_of(n) in known_factions]
        faction = collections.Counter(votes).most_common(1)[0][0] if votes else "unknown"
        available = sorted(n for n in buildings if faction_of(n) == faction)

        def_frames = [f for f, n in items if n in defences]
        radar_frames = [f for f, n in items if _has(n, RADAR_COMMS_MARKERS) and n not in defences]
        t2_frames = [f for f, n in items if _has(n, TIER2_MARKERS) and n not in defences]
        placed = {n for n in names if n in buildings and faction_of(n) == faction}
        coverage = round(len(placed) / len(available), 4) if available else 0.0
        seats.append({
            "subject": subject,
            "faction": faction,
            "defences": len(def_frames),
            "first_defence_tick": _first(def_frames),
            "first_radar_comms_tick": _first(radar_frames),
            "tier2_first_tick": _first(t2_frames),
            "tier": "tier2" if t2_frames else "tier1",
            "building_types_placed": len(placed),
            "building_types_available": len(available),
            "coverage": coverage,
            "production_buildings": sum(1 for n in names if _has(n, PRODUCTION_MARKERS) and n not in defences),
            "refineries": sum(1 for n in names if _has(n, REFINERY_MARKERS)),
            "power": sum(1 for n in names if _has(n, POWER_MARKERS)),
        })

    bot_types = sorted({p["BotType"] for p in players if p.get("IsBot") and p.get("BotType")})
    return {
        "replay": os.path.basename(replay_path),
        "net_frame_interval": interval,
        "last_tick": last_tick,
        "bot_types": bot_types,
        "players": players,
        "seats": seats,
        "constants": {
            "min_ticks": MIN_TICKS,
            "tier2_deadline_tick": TIER2_DEADLINE_TICK,
            "tier2_markers": list(TIER2_MARKERS),
            "tier2_status": "PROPOSED, owner to confirm",
            "coverage_threshold": "UNCALIBRATED",
        },
    }


def evaluate(report, hard_plus=HARD_PLUS_DEFAULT, coverage_min=None):
    """Return (status, reasons). Status is PASS, HOLD or NOT_ELIGIBLE."""
    bot_types = set(report["bot_types"])
    eligible = (report["last_tick"] >= MIN_TICKS and bool(bot_types)
                and bot_types <= set(hard_plus))
    if not eligible:
        return "NOT_ELIGIBLE", []
    reasons = []
    for seat in report["seats"]:
        tag = "seat {} ({})".format(seat["subject"], seat["faction"])
        if seat["defences"] == 0:
            reasons.append(tag + " placed 0 defences")
        t2 = seat["tier2_first_tick"]
        if t2 is None or t2 > TIER2_DEADLINE_TICK:
            reasons.append(tag + " has no tier-2 building by tick {}".format(TIER2_DEADLINE_TICK))
        if coverage_min is not None and seat["coverage"] < coverage_min:
            reasons.append("{} coverage {} below {}".format(tag, seat["coverage"], coverage_min))
    return ("HOLD" if reasons else "PASS"), reasons


def main(argv=None):
    ap = argparse.ArgumentParser(description="Build-coverage guard for an .orarep replay.")
    ap.add_argument("replay")
    ap.add_argument("--rules-root", default=_DEFAULT_RULES)
    ap.add_argument("--hard-plus", default=",".join(HARD_PLUS_DEFAULT),
                    help="comma list of bot types treated as Hard+ (default: hard)")
    ap.add_argument("--coverage-min", type=float, default=None,
                    help="HOLD below this coverage; unset = uncalibrated, not enforced")
    ap.add_argument("--out", default=None, help="write the JSON report here too")
    args = ap.parse_args(argv)
    try:
        report = analyse(args.replay, args.rules_root)
    except (OSError, ValueError, IndexError, struct.error) as exc:
        print(json.dumps({"status": "ERROR", "error": str(exc)}))
        return 2
    status, reasons = evaluate(report, tuple(args.hard_plus.split(",")), args.coverage_min)
    report["status"] = status
    report["reasons"] = reasons
    text = json.dumps(report, indent=2, sort_keys=True)
    if args.out:
        with open(args.out, "w", encoding="utf-8", newline="\n") as f:
            f.write(text + "\n")
    print(text)
    return 1 if status == "HOLD" else 0


if __name__ == "__main__":
    sys.exit(main())
