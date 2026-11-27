#!/usr/bin/env python3
"""build_coverage.py: build-coverage guard for .orarep replays (BUILD-COVERAGE-GUARD-01).

Reads every seat's PlaceBuilding orders from the replay order stream. Per seat it reports
defences (count and first tick), first radar/comms tick, first tier-2 tick, building coverage
(distinct building types placed / types the loaded rules make available to the faction),
production buildings, refineries and power placements.

Bot types and seat metadata are NOT in a DEV replay: its trailer lists only the human player.
They come from the game's Logs/cameo-ai-matches.jsonl (schema 3, one record per seat, keyed by
game_uid). The log is found by walking up from the replay, or given with --matches. If it is
missing, the game is NOT_ELIGIBLE and the report says why.

HOLD rules (Hard+ games of at least MIN_TICKS only):
  - any seat with 0 defences. Defences are the "defence" and "aa" (anti-air) categories. Walls and
    gates ("wall_gate") are reported in defence_split and do not count. A metadata seat with no
    placements counts as a 0-defence seat;
  - tier-2 deadline and coverage minimum: the command line applies TIER2_DEADLINE_DEFAULT and
    COVERAGE_MIN_DEFAULT unless --tier2-deadline / --coverage-min are given. evaluate() itself
    applies a threshold only when one is passed. The tier-2 deadline is a HOLD only in games of at
    least TIER2_CHECK_MIN_TICKS; shorter games list it as an advisory.

Defence and building types come from the rules the mod loads: the Rules entries of mod.yaml
and the Include chains of its content packs. Files the mod does not load are not read.
Defence-queue actors whose name carries a NON_DEFENCE_MARKERS term (silos, superweapons, temples,
storage, tier-2, production, refinery, power) are counted as buildings, not defences.

Replay layout and Order serialization follow tools/ai/order_trace.py.

Exit codes: 0 PASS or NOT_ELIGIBLE (calibration: done), 1 HOLD, 2 unreadable input or refused.
"""
import argparse
import collections
import hashlib
import json
import math
import os
import re
import statistics
import struct
import sys

MIN_TICKS = 12000
TIER2_CHECK_MIN_TICKS = 30000     # the tier-2 deadline is a HOLD only in games at least this long

# Built-in gates from the 11-22 calibration on the five identified C29 replays (commit 8c9c7d32).
# main() applies them when no flag is given, so a gate that forgets the flags still runs both checks.
TIER2_DEADLINE_DEFAULT = 30000    # 1.25 x latest good tier-2 tick (23571), rounded up to 1000
COVERAGE_MIN_DEFAULT = 0.4        # 0.8 x lowest good-game seat coverage (0.5)
DEFAULT_NET_INTERVAL = 3          # used only when the replay states no NetFrameInterval

# Bot tiers of the 11-step ladder at or above Hard (mods/cameo/ai/ai.yaml, Bots lists).
HARD_PLUS = ("hard", "veryhard", "brutal", "challenger", "unbeatable", "cameogod")

# Tier-2 markers. handofnod and airstrip are Nod's barracks and war factory (production, not tier 2).
RADAR_COMMS_MARKERS = ("radar", "dome", "comm")
PRODUCTION_MARKERS = ("barracks", "factory", "handofnod", "airstrip")
REFINERY_MARKERS = ("refinery",)
POWER_MARKERS = ("powerplant", "windtrap", "reactor")
# Exact actor names that are power plants without a marker in their name: TD (GDI and Nod) power is "nuke".
POWER_EXACT = ("nuke",)

# Defence-queue actors that are buildings, not defences. The mod queues several of these under
# Defence (ra1_oresilo, ra1_soviets_missilesilo, td_gdi_advancedcommunicationscenter, ...).
# Matched on the lower-cased actor name. Superweapons inherit ^PrimarySuperweapon, which adds a
# Defence queue; silos are storage or superweapons; the rest are tier-2 and economy buildings.
NON_DEFENCE_MARKERS = (("chronosphere", "gapgenerator", "ironcurtain", "temple", "silo", "storage",
                        "palace", "stealthgenerator", "supercomputer", "weathercontrolcenter",
                        "upgradecenter", "spaceport")
                       + RADAR_COMMS_MARKERS + ("techcenter", "techcentre", "research")
                       + PRODUCTION_MARKERS + REFINERY_MARKERS + POWER_MARKERS)

# Defence categories (reported separately; only walls are kept out of the HOLD count).
WALL_MARKERS = ("wall", "fenc", "barrier", "concrete", "sbag", "brik")
AA_MARKERS = ("aa_", "aagun", "aadefender", "antiair", "flak", "samsite", "samtower", "skyshield")

# MiniYaml: a top-level key has no leading whitespace (names may contain dots: GAP.camea).
_TOP_KEY = re.compile(r"^([^\s#:][^:#]*):\s*(#.*)?$")
_QUEUE = re.compile(r"^\s+Queue:\s*(.*)$")
_INHERITS = re.compile(r"^\s+Inherits(?:@\w+)?:\s*(\^?[A-Za-z0-9_\-]+)")
_NET_INTERVAL = re.compile(r"NetFrameInterval:\s*(\d+)")
_FINAL_TICK = re.compile(r"FinalGameTick:\s*(\d+)")
_GAME_UID = re.compile(r"GameUid:\s*([0-9A-Za-z-]+)")
_TWO_PART_PREFIXES = ("td", "ra1", "ts", "ra2", "wc2")
_MATCHES_REL = os.path.join("Logs", "cameo-ai-matches.jsonl")
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
    """Player@N blocks in the replay trailer (in DEV recordings: the human only)."""
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
    return players


def read_stream(path):
    """Read the replay order stream and its header and trailer values.

    Packet frames are net frames; world tick = frame * NetFrameInterval. The interval is taken
    from the header (before the -1 trailer marker), then from the trailer. If neither states it,
    DEFAULT_NET_INTERVAL is used and interval_source is "assumed".
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
        if frame <= 0:  # header and metadata packets carry frame 0
            continue
        if len(pkt) > 4 and pkt[4] in (0xBF, 0x65):  # Disconnect / SyncHash
            continue
        last_frame = max(last_frame, frame)
        for order, subject, tstring in _orders(pkt):
            if order == "PlaceBuilding" and tstring:
                placements.append((frame, subject, tstring))
    if trailer_start is None:
        head, trailer = data.decode("latin-1"), ""
    else:
        head = data[:trailer_start].decode("latin-1")
        trailer = data[trailer_start:].decode("utf-8", "replace")
    m = _NET_INTERVAL.search(head)
    source = "header"
    if not m:
        m = _NET_INTERVAL.search(trailer)
        source = "trailer"
    if m and int(m.group(1)) > 0:
        interval = int(m.group(1))
    else:
        interval, source = DEFAULT_NET_INTERVAL, "assumed"
    gm = _GAME_UID.search(head)
    fm = _FINAL_TICK.search(trailer)
    return {"placements": placements, "last_frame": last_frame, "interval": interval,
            "interval_source": source, "game_uid": gm.group(1) if gm else None,
            "final_tick": int(fm.group(1)) if fm else None,
            "replay_players": _players(trailer)}


def _find_upwards(start_dir, rel, levels=6):
    d = os.path.abspath(start_dir)
    for _ in range(levels):
        cand = os.path.join(d, rel)
        if os.path.isfile(cand):
            return cand
        parent = os.path.dirname(d)
        if parent == d:
            break
        d = parent
    return None


def read_match_seats(replay_path, game_uid, matches_path=None):
    """Return (seats, source). seats is None with source = the reason when unavailable."""
    path = matches_path or _find_upwards(os.path.dirname(os.path.abspath(replay_path)), _MATCHES_REL)
    if not path or not os.path.isfile(path):
        return None, "no cameo-ai-matches.jsonl found beside or above the replay"
    if not game_uid:
        return None, "replay header has no GameUid"
    seats = {}
    with open(path, encoding="utf-8", errors="replace") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                rec = json.loads(line)
            except ValueError:
                continue
            if rec.get("game_uid") != game_uid:
                continue
            pl = rec.get("player") or {}
            seat = pl.get("seat")
            if seat is None or seat in seats:
                continue
            seats[seat] = {"seat": seat, "bot_type": (pl.get("bot_type") or "").lower(),
                           "faction": pl.get("faction") or "", "team": pl.get("team")}
    if not seats:
        return None, "no records for game_uid {} in {}".format(game_uid, path)
    return list(seats.values()), path


def _section_entries(lines):
    """Yield (section, entry) for a MiniYaml-style list file: top-level 'Section:' lines and their
    indented entries. An inline value after a top-level key (Include: path) is an entry too."""
    section = None
    for raw in lines:
        stripped = raw.strip()
        if not stripped or stripped.startswith("#"):
            continue
        if not raw[0].isspace():
            key, _, value = stripped.partition(":")
            section = key.strip()
            value = value.split("#", 1)[0].strip()
            if value:
                yield section, value
        else:
            yield section, stripped.split("#", 1)[0].strip()


def _resolve(entry):
    """'cameo|rules/x.yaml' -> 'rules/x.yaml'; 'ContentPacks|A/b.yaml' -> 'ContentPacks/A/b.yaml'."""
    if "|" in entry:
        prefix, rest = entry.split("|", 1)
        return rest if prefix in ("cameo", "") else prefix + "/" + rest
    return entry


def _loaded_rule_files(mod_root):
    """Return (files, missing): the yaml files mod.yaml loads through Rules entries and Include chains."""
    files = []
    missing = []
    seen = set()
    todo = ["mod.yaml"]
    while todo:
        rel = todo.pop(0)
        if rel in seen:
            continue
        seen.add(rel)
        path = os.path.join(mod_root, *rel.split("/"))
        if not os.path.isfile(path):
            missing.append(rel)
            continue
        with open(path, encoding="utf-8", errors="replace") as f:
            lines = f.read().splitlines()
        for section, entry in _section_entries(lines):
            if not entry.endswith(".yaml"):
                continue
            target = _resolve(entry)
            if section == "Rules" and target not in files:
                files.append(target)
            elif section == "Include":
                todo.append(target)
    return [os.path.join(mod_root, *f.split("/")) for f in files], missing


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


def load_rules(rules_root, info=None):
    """Return (defence_names, building_names) for the buildable actors the mod loads.

    When rules_root holds mod.yaml, only the files it loads are read (Rules entries and Include
    chains). Without mod.yaml (test trees) every .yaml under rules_root is read. info, if given,
    receives the source, the file count and any missing files.
    Queue values are taken from the actor and from every template it inherits (Inherits:).
    """
    if os.path.isfile(os.path.join(rules_root, "mod.yaml")):
        files, missing = _loaded_rule_files(rules_root)
        source = "mod.yaml"
    else:
        files, missing, source = [], [], "tree"
        for dirpath, _dirs, names in os.walk(rules_root):
            files += [os.path.join(dirpath, n) for n in names if n.endswith(".yaml")]
    if info is not None:
        info.update({"source": source, "files": len(files), "missing": missing})

    queues = collections.defaultdict(set)    # key (incl. ^templates) -> Queue values
    inherits = collections.defaultdict(set)  # key -> template names
    semantic_radars = set()
    semantic_centres = set()
    strategic = set()
    role_fields = {
        "SemanticRadarTypes": semantic_radars,
        "SemanticTechCentreTypes": semantic_centres,
        "SemanticSuperweaponTypes": strategic,
    }
    for path in files:
        with open(path, encoding="utf-8", errors="replace") as f:
            lines = f.read().splitlines()
        for name, body in _blocks(lines):
            for line in body:
                field, _, value = line.strip().partition(":")
                if field in role_fields:
                    role_fields[field].update(n.strip() for n in value.split("#", 1)[0].split(",") if n.strip())
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
        is_defence = any("defen" in v for v in values)
        is_building = any("building" in v for v in values)
        if is_defence and not _has(name, NON_DEFENCE_MARKERS):
            defences.add(name)
        elif is_building or (is_defence and _has(name, NON_DEFENCE_MARKERS)):
            buildings.add(name)
    if info is not None:
        info.update({
            "semantic_radar_types": sorted(semantic_radars),
            "semantic_tech_centre_types": sorted(semantic_centres),
            "semantic_superweapon_types": sorted(strategic),
        })
    return defences, buildings


def faction_of(name):
    """Faction prefix of an actor name: td_gdi, ra1_soviets, ixian, ... (shared names fall back)."""
    parts = name.split("_")
    if len(parts) >= 3 and parts[0] in _TWO_PART_PREFIXES:
        return parts[0] + "_" + parts[1]
    return parts[0]


def _has(name, markers):
    low = name.lower()
    return any(m in low for m in markers)


def _category(name):
    """Defence category: 'wall_gate', 'aa' or 'defence' (by name; the queue alone does not say)."""
    if _has(name, WALL_MARKERS):
        return "wall_gate"
    if _has(name, AA_MARKERS):
        return "aa"
    return "defence"


def _first(frames):
    return min(frames) if frames else None


def _seat_row(subject, faction, items, defences, buildings, radar_types, tech_centres, superweapons):
    names = [n for _f, n in items]
    split = {"defence": 0, "aa": 0, "wall_gate": 0}
    for _f, n in items:
        if n in defences:
            split[_category(n)] += 1
    # Walls and gates are reported in defence_split but are not defences for the HOLD rule.
    def_frames = [f for f, n in items if n in defences and _category(n) != "wall_gate"]
    radar_frames = [f for f, n in items if n in radar_types]
    tech_frames = [f for f, n in items if n in tech_centres]
    superweapon_frames = [f for f, n in items if n in superweapons]
    available = sorted(n for n in buildings if faction_of(n) == faction)
    placed = {n for n in names if n in buildings and faction_of(n) == faction}
    coverage = round(len(placed) / len(available), 4) if available else 0.0
    return {
        "subject": subject,
        "faction": faction,
        "defences": len(def_frames),
        "defence_split": split,
        "first_defence_tick": _first(def_frames),
        "first_radar_comms_tick": _first(radar_frames),
        "tier2_first_tick": _first(radar_frames),
        "tier3_first_tick": _first(tech_frames),
        "superweapon_first_tick": _first(superweapon_frames),
        "radar_supported": any(faction_of(name) == faction for name in radar_types) or bool(radar_frames),
        "tier": "tier3" if tech_frames else "tier2" if radar_frames else "tier1",
        "building_types_placed": len(placed),
        "building_types_available": len(available),
        "coverage": coverage,
        "production_buildings": sum(1 for n in names if _has(n, PRODUCTION_MARKERS) and n not in defences),
        "refineries": sum(1 for n in names if _has(n, REFINERY_MARKERS)),
        "power": sum(1 for n in names if _has(n, POWER_MARKERS) or n.lower() in POWER_EXACT),
    }


def _sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def analyse(replay_path, rules_root=_DEFAULT_RULES, matches_path=None):
    rules_info = {}
    defences, buildings = load_rules(rules_root, rules_info)
    roles = tuple(set(rules_info[key]) for key in (
        "semantic_radar_types", "semantic_tech_centre_types", "semantic_superweapon_types"))
    stream = read_stream(replay_path)
    interval = stream["interval"]
    if stream["final_tick"] is not None:
        last_tick, last_tick_source = stream["final_tick"], "FinalGameTick (trailer)"
    else:
        last_tick, last_tick_source = stream["last_frame"] * interval, "last packet frame x interval"
    meta, meta_source = read_match_seats(replay_path, stream["game_uid"], matches_path)
    known = ({s["faction"] for s in meta} if meta
             else {faction_of(n) for n in defences | buildings})

    by_subject = collections.defaultdict(list)
    for frame, subject, name in stream["placements"]:
        by_subject[subject].append((frame * interval, name))

    seats = []
    for subject in sorted(by_subject, key=lambda s: (s is None, s)):
        items = by_subject[subject]
        votes = [faction_of(n) for _f, n in items if faction_of(n) in known]
        faction = collections.Counter(votes).most_common(1)[0][0] if votes else "unknown"
        seats.append(_seat_row(subject, faction, items, defences, buildings, *roles))

    reconcile = []
    bot_types = []
    if meta:
        replay_f = collections.Counter(r["faction"] for r in seats)
        meta_f = collections.Counter(s["faction"] for s in meta)
        for faction, n in (meta_f - replay_f).items():
            for _ in range(n):
                row = _seat_row(None, faction, [], defences, buildings, *roles)
                row["source"] = "metadata seat with no placements"
                seats.append(row)
        if len(meta) != len(by_subject):
            reconcile.append("seat count: replay {} vs metadata {}".format(len(by_subject), len(meta)))
        if replay_f - meta_f:
            reconcile.append("faction mismatch: replay {} vs metadata {}".format(
                dict(replay_f), dict(meta_f)))
        bot_types = sorted({s["bot_type"] for s in meta if s["bot_type"]})

    if meta is None:
        eligibility, reason = "NOT_ELIGIBLE", "no match metadata: " + meta_source
    elif last_tick < MIN_TICKS:
        eligibility, reason = "NOT_ELIGIBLE", "game shorter than {} ticks".format(MIN_TICKS)
    elif not bot_types:
        eligibility, reason = "NOT_ELIGIBLE", "no bot seats in the metadata"
    elif set(bot_types) - set(HARD_PLUS):
        eligibility = "NOT_ELIGIBLE"
        reason = "bot types outside Hard+: " + ", ".join(sorted(set(bot_types) - set(HARD_PLUS)))
    else:
        eligibility, reason = "ELIGIBLE", "Hard+ bots only, at least {} ticks".format(MIN_TICKS)

    return {
        "replay": os.path.basename(replay_path),
        "replay_sha256": _sha256(replay_path),
        "game_uid": stream["game_uid"],
        "net_frame_interval": interval,
        "interval_source": stream["interval_source"],
        "last_tick": last_tick,
        "last_tick_source": last_tick_source,
        "eligibility": eligibility,
        "eligibility_reason": reason,
        "metadata_source": meta_source if meta else None,
        "bot_types": bot_types,
        "replay_players": stream["replay_players"],
        "replay_seats": len(by_subject),
        "metadata_seats": len(meta) if meta else None,
        "reconciliation": reconcile,
        "rules": rules_info,
        "semantic_tier_schema": 1,
        "seats": seats,
        "constants": {
            "min_ticks": MIN_TICKS,
            "tier2_check_min_ticks": TIER2_CHECK_MIN_TICKS,
            "hard_plus": list(HARD_PLUS),
            "tier2_role": "semantic_radar_types",
            "non_defence_markers": list(NON_DEFENCE_MARKERS),
            "wall_markers": list(WALL_MARKERS),
            "aa_markers": list(AA_MARKERS),
            "default_net_interval": DEFAULT_NET_INTERVAL,
        },
    }


def evaluate(report, tier2_deadline=None, coverage_min=None):
    """Return (status, reasons, advisories). Status is PASS, HOLD or NOT_ELIGIBLE.

    Defaults: only the 0-defence rule applies. The tier-2 and coverage rules run only when a
    threshold is passed (calibrated values, see calibrate()). A tier-2 miss in a game shorter than
    TIER2_CHECK_MIN_TICKS is an advisory, not a HOLD."""
    if report["eligibility"] != "ELIGIBLE":
        return "NOT_ELIGIBLE", [], []
    if tier2_deadline is not None and report.get("semantic_tier_schema") == 1 and not report["rules"]["semantic_radar_types"]:
        return "NOT_ELIGIBLE", [], ["semantic radar catalog missing; tier-2 deadline cannot be evaluated"]
    reasons = []
    advisories = []
    t2_enforced = report["last_tick"] >= TIER2_CHECK_MIN_TICKS
    for seat in report["seats"]:
        ident = seat["subject"] if seat["subject"] is not None else "no-placement"
        tag = "seat {} ({})".format(ident, seat["faction"])
        if seat["defences"] == 0:
            reasons.append(tag + " placed 0 defences")
        if tier2_deadline is not None and not seat.get("radar_supported", True):
            advisories.append(tag + " radar milestone unavailable in the semantic catalog")
        elif tier2_deadline is not None:
            t2 = seat["tier2_first_tick"]
            if t2 is None or t2 > tier2_deadline:
                msg = "{} no tier-2 building by tick {}".format(tag, tier2_deadline)
                (reasons if t2_enforced else advisories).append(msg)
        if coverage_min is not None and seat["coverage"] < coverage_min:
            reasons.append("{} coverage {} below {}".format(tag, seat["coverage"], coverage_min))
    return ("HOLD" if reasons else "PASS"), reasons, advisories


def _summary(values):
    if not values:
        return {"n": 0}
    v = sorted(values)
    return {"n": len(v), "min": v[0], "median": statistics.median(v), "max": v[-1]}


def _round_up(value, step=1000):
    """Round up to a multiple of step. round(..., 6) first so float noise cannot add a step."""
    return int(math.ceil(round(value, 6) / step) * step)


def calibrate(paths, rules_root=_DEFAULT_RULES, matches_path=None, tier2_factor=None, coverage_factor=None):
    """Distributions from known-good replays and thresholds from explicit factors (ruled 11-22).

    tier2 deadline = tier2_factor x latest good tier-2 tick, rounded up to 1000 ticks.
    coverage_min   = coverage_factor x lowest good-game seat coverage (one threshold for all seats).
    Factors are never defaulted: without them only the distributions are reported.
    """
    games = []
    problems = []
    for path in paths:
        rep = analyse(path, rules_root, matches_path)
        if rep["eligibility"] != "ELIGIBLE":
            problems.append({"replay": rep["replay"], "reason": rep["eligibility_reason"]})
            continue
        games.append({
            "replay": rep["replay"], "replay_sha256": rep["replay_sha256"], "game_uid": rep["game_uid"],
            "last_tick": rep["last_tick"],
            "defences": [s["defences"] for s in rep["seats"]],
            "defence_split": [s["defence_split"] for s in rep["seats"]],
            "tier2_ticks": [s["tier2_first_tick"] for s in rep["seats"]],
            "coverage": [s["coverage"] for s in rep["seats"]],
        })
    result = {
        "refused": bool(problems) or not games,
        "games": len(games),
        "distinct_game_uids": len({g["game_uid"] for g in games}),
        "problems": problems,
        "factors": {"tier2_factor": tier2_factor, "coverage_factor": coverage_factor},
        "thresholds": {"tier2_deadline_tick": None,
                       "tier2_enforced_from_tick": TIER2_CHECK_MIN_TICKS,
                       "coverage_min": None},
        "per_game": games,
    }
    if result["refused"]:
        result["note"] = "refused: every input must be an ELIGIBLE known-good replay"
        return result
    if result["distinct_game_uids"] < len(games):
        result["warning"] = "inputs share a game uid (repeats of one game): thresholds rest on thin evidence"
    t2_all = [t for g in games for t in g["tier2_ticks"]]
    t2_known = [t for t in t2_all if t is not None]
    cov_all = [c for g in games for c in g["coverage"]]
    result["tier2_ticks"] = dict(_summary(t2_known), missing=len(t2_all) - len(t2_known))
    result["coverage"] = _summary(cov_all)
    by_index = {}
    for g in games:
        for i, c in enumerate(g["coverage"]):
            by_index[i] = min(by_index.get(i, c), c)
    result["coverage_lowest_by_seat_index"] = [by_index[i] for i in sorted(by_index)]
    if tier2_factor is not None:
        if len(t2_known) < len(t2_all):
            result["tier2_note"] = "{} good seats never built tier-2: deadline not set".format(
                len(t2_all) - len(t2_known))
        else:
            result["thresholds"]["tier2_deadline_tick"] = _round_up(tier2_factor * max(t2_known))
    if coverage_factor is not None:
        result["thresholds"]["coverage_min"] = round(coverage_factor * min(cov_all), 4)
    return result


def _parser():
    ap = argparse.ArgumentParser(description="Build-coverage guard for .orarep replays.")
    ap.add_argument("replays", nargs="+", help="one replay, or known-good replays with --calibrate")
    ap.add_argument("--calibrate", action="store_true", help="derive thresholds from known-good replays")
    ap.add_argument("--tier2-factor", type=float, default=None,
                    help="calibrate: deadline = this x latest good tier-2 tick, up to 1000 (no default)")
    ap.add_argument("--coverage-factor", type=float, default=None,
                    help="calibrate: coverage minimum = this x lowest good coverage (no default)")
    ap.add_argument("--tier2-deadline", type=int, default=TIER2_DEADLINE_DEFAULT,
                    help="HOLD if a seat has no tier-2 building by this tick, in games of at least "
                         "TIER2_CHECK_MIN_TICKS (default {})".format(TIER2_DEADLINE_DEFAULT))
    ap.add_argument("--coverage-min", type=float, default=COVERAGE_MIN_DEFAULT,
                    help="HOLD below this coverage (default {})".format(COVERAGE_MIN_DEFAULT))
    ap.add_argument("--matches", default=None,
                    help="cameo-ai-matches.jsonl (default: found beside or above the replay)")
    ap.add_argument("--rules-root", default=_DEFAULT_RULES)
    ap.add_argument("--out", default=None, help="write the JSON report here too")
    return ap


def main(argv=None):
    args = _parser().parse_args(argv)
    try:
        if args.calibrate:
            result = calibrate(args.replays, args.rules_root, args.matches,
                               args.tier2_factor, args.coverage_factor)
            text = json.dumps(result, indent=2, sort_keys=True)
            if args.out:
                with open(args.out, "w", encoding="utf-8", newline="\n") as f:
                    f.write(text + "\n")
            print(text)
            return 2 if result["refused"] else 0
        if len(args.replays) != 1:
            ap.error("one replay unless --calibrate")
        report = analyse(args.replays[0], args.rules_root, args.matches)
    except (OSError, ValueError, IndexError, struct.error) as exc:
        print(json.dumps({"status": "ERROR", "error": str(exc)}))
        return 2
    status, reasons, advisories = evaluate(report, args.tier2_deadline, args.coverage_min)
    report["status"] = status
    report["reasons"] = reasons
    report["advisories"] = advisories
    report["thresholds"] = {"tier2_deadline_tick": args.tier2_deadline,
                            "tier2_enforced_from_tick": TIER2_CHECK_MIN_TICKS,
                            "coverage_min": args.coverage_min}
    text = json.dumps(report, indent=2, sort_keys=True)
    if args.out:
        with open(args.out, "w", encoding="utf-8", newline="\n") as f:
            f.write(text + "\n")
    print(text)
    return 1 if status == "HOLD" else 0


if __name__ == "__main__":
    sys.exit(main())
