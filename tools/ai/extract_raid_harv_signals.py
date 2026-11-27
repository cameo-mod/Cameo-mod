#!/usr/bin/env python3
"""RAID-HARV-01 P3: the DEV extractor of the nine raid signals. One bot game's raid rows in, the exposure records of the shared LEARN-ALL-02 fitter out.

The learner is the shared LEARN-ALL-02 core (tools/ai/fit_learn_all.py): it owns the step (log space, rate 0.04, cap 0.08), the 4-value aggregate and the snapshot. This
script owns ONLY the meaning of one raid game: which observations count, the denominator and clip of each key, and the attribution to the enemy players' factions and to the
observed style of the main target owner. It contains no step arithmetic, no aggregate, no root. It is the python mirror of
OpenRA.Mods.CA/Traits/BotModules/Squads/RaidHarvSignals.cs (the nine signal functions) and the two are kept identical by tools/tests/raid_harv_signals_golden.json, which
test_extract_raid_harv_signals.py and RaidHarvSignalsGoldenTest.cs both replay. Training exists only in DEV: an official build writes no rows and runs no fitter.

Signal. Each key reads ONLY what the bot observed (own squads, visible targets). The result is a relative error in permille, clipped to +-300 (categorical signals are +-200);
the fitter signal is that error / 300, in [-1, 1]. No sample = null (the fitter keeps the previous estimate; an undefined denominator is never read as zero):

  key                    sample (one row each)                                          error                                              denominator / horizon
  raid.lost_recon        reacquired after N ticks | budget used up                      target 1.25 x median N (relative to the current);  episodes, the episode
                                                                                        budget used up = -200, averaged over all episodes
  raid.move_refresh      ELIGIBLE refresh, drift in cells                               late share (drift >= 3) over 50 percent = -200;    eligible refreshes, the raid
                                                                                        wasted share (drift <= 1) over 60 percent = +200; else 0
  raid.req_margin        resolved raid that KILLED its target, package loss permille    loss > 600 = +200, loss < 300 = -200, else 0       resolved raids, the raid
  raid.recon_radius      reacquired at distance d | budget used up AND target seen      target 1.25 x median d; far-seen exhausted = +200  episodes, 600 ticks after release
                         farther than the radius within 600 ticks after release
  raid.incident_loss     retreat: lost half within 600 ticks | clean re-strike          lost half = -200, clean re-strike = +200           incidents, 600 ticks
  raid.incident_window   span from the first loss of a lost raid to the trigger         target 1.5 x median span (relative)                lost raids, the raid
  raid.incident_decay    incident cell at its decay: re-incident within 1500 | passed   re-incident = +200, passed clean = -200           incident cells, 1500 ticks after decay
                         clean before the decay
  raid.waypoint_spacing  leg left the explored lane by more than 2 cells (bool)         any leg left = -200, every leg inside = +200       legs, the leg
  raid.defence_radius    resolved raid WITH losses: an armed enemy first seen outside   took part = +200, none = -200                       resolved raids with losses, the raid
                         the radius took part (own or shared-ally vision only)

Averages are integer means truncated toward zero (C# integer division), the relative errors are clipped BEFORE they are averaged with the categorical samples, exactly as
RaidHarvSignals does.

Input (DEV log, JSON lines; every other line of the file is ignored). Two row kinds, every row of a game has the same game_id:

  raid_harv_seat: ONE row per bot seat per game (the roster the seat plays against; written at game start or end).
  {"event": "raid_harv_seat", "game_id": "<[A-Za-z0-9_.-]+>", "seat": "<bot seat>", "personality": "rush" | null, "own_faction": "gdi" | null,
   "enemies": [{"player": "<player id>", "faction": "nod"}, ...]}
  `enemies` lists EVERY enemy player of the game (every combatant that is not on the seat's team), one entry per player, whether or not a raid ever met it.

  raid_harv_signal: one row per sample, never per tick.
  {"event": "raid_harv_signal", "game_id": ..., "seat": ..., "tick": 1500, "raid_id": "<unique per seat and game>", "target_player": "<an enemy player id of the seat row>",
   "target_style": "boom" | null, "key": "raid.lost_recon", <payload>}
  `target_player` is the owner of the raid's target, `target_style` the style the observation classifier reported for that owner AT THE PICK (null = unseen). The emitter snapshots
  both at the pick and repeats them on every row of the raid for the raid's life; a later row that disagrees only warns (the earliest row of the raid wins), it never refuses the game. A bot never reads
  an enemy's personality or any internal state; the style is only what the classifier observed, and the same classifier works against human players.
  payload by key:
    raid.lost_recon        "kind": "reacquired" (+ "ticks": N) | "exhausted";  "current": the key value in force, whole ticks (>= 1)
    raid.recon_radius      "kind": "reacquired" (+ "distance": d cells) | "exhausted" (+ "far_seen": bool);  "current": whole cells (>= 1)
    raid.move_refresh      "drift": cells, "visible_prev": bool, "visible_now": bool, "phase": "Travel" | "Strike" | ..., "since_change": ticks since the last phase change or the
                           raid's first order. The refresh is eligible only when both visibility flags hold, the phase is Travel or Strike and since_change >= 25.
    raid.req_margin        "killed": bool, "loss_permille": 0..1000          (not killed = not a sample)
    raid.incident_loss     "lost_half_within_600": bool, "restrike_clean": bool  (lost half wins; neither = not a sample)
    raid.incident_window   "span_ticks": ticks, "current": whole ticks (>= 1)
    raid.incident_decay    "reincident_within_1500": bool, "passed_clean_before_decay": bool  (re-incident wins; neither = not a sample)
    raid.waypoint_spacing  "left_lane": bool
    raid.defence_radius    "outside_defender_took_part": bool

Attribution (DESIGNER RULE: every learned value is G x F_own x E_eff x S_obs; design 02 delta 02 with the Opus rulings of 2026-11-27):
  * enemies = EVERY enemy player of the seat row, one entry per player with weight 1 (counted per player, never weighted by raids). The fitter turns them into the per-faction
    enemy shares and E_eff = geomean(aggregate of all enemy players' factions, main target faction).
  * main target = the target owner (player) with the most raids that contributed a counted sample (ties: the owner whose first counted sample is earliest, then the player id).
    `main_target` is its faction.
  * S_obs = the observed style of the main target owner ONLY. It is the `target_style` of that owner's latest counted raid (null = unseen, which the fitter reads as the exact
    three-axis value). No other enemy's style is emitted: in the `enemies` list only the main target owner's entry carries a style. `main_target_style` repeats that style as a
    plain field of the record (the fitter at LA02 79329cc1 reads the style from the `enemies` entry and ignores the extra field).
  * A raid whose rows all fail the counting rules (an ineligible refresh, a raid that did not kill, ...) is not a sample and does not make its owner the main target.
  * One record per seat that produced evidence. The seat id is prefixed `raid:` so it cannot collide with the seats of the wave-role exposure file of the same game.

Output (the fitter's exposure format at LA02 79329cc1, one JSON line per seat):
  {"game_id": g, "seq": <game order>, "seat": "raid:<seat>", "personality": p, "own_faction": f, "main_target": faction | null, "main_target_style": style | null,
   "enemies": [{"faction": ..., "style": ... | null, "weight": 1}], "knobs": [{"knob": "raid.lost_recon", "signal": 0.4 | null}, ... nine, in RaidKey order]}

Pipeline use (post-game step, after EVERY DEV game, in game order, with the same --seq as the wave-role exposure of that game):
  python tools/ai/extract_raid_harv_signals.py --rows <game_dir>/<dev log>.jsonl --seq <n> --out <game_dir>/raid_harv_exposure.jsonl
  python tools/ai/fit_learn_all.py --exposures <game_dir>/learn_all_exposure.jsonl <game_dir>/raid_harv_exposure.jsonl
Exit codes (the same numbers as the fitter): 3 = rows of more than one game (pass --game-id or split the file); 5 = invalid rows. An empty output file is written when the game has
no raid evidence (the fitter then prints "no exposure records" and applies nothing).
"""
import argparse
import json
import re
import sys

EVENT = "raid_harv_signal"
SEAT_EVENT = "raid_harv_seat"
EVENTS = (EVENT, SEAT_EVENT)
SEAT_PREFIX = "raid:"

# The declared parameters. Mirrors of RaidHarvKeys / RaidHarvSignals (pinned by the golden file).
ERROR_CLIP = 300
CATEGORICAL = 200
REFRESH_SETTLE_TICKS = 25
LATE_DRIFT_CELLS = 3
WASTED_DRIFT_CELLS = 1
ELIGIBLE_PHASES = ("Travel", "Strike")

# The nine keys in RaidKey order (RaidHarvKeys.All ids).
KEYS = (
    "raid.lost_recon",
    "raid.move_refresh",
    "raid.req_margin",
    "raid.recon_radius",
    "raid.incident_loss",
    "raid.incident_window",
    "raid.incident_decay",
    "raid.waypoint_spacing",
    "raid.defence_radius",
)

GAME_ID = re.compile(r"^[A-Za-z0-9_.\-]+\Z")


# ---------------------------------------------------------------- the nine signals (mirror of RaidHarvSignals; None = no sample, else the error in permille)

def tdiv(a, b):
    """C# integer division (truncates toward zero)."""
    q = abs(a) // abs(b)
    return q if (a >= 0) == (b >= 0) else -q


def clip(permille):
    return max(-ERROR_CLIP, min(ERROR_CLIP, permille))


def median(values):
    """The middle value; for an even count the average of the two middle values truncated toward zero. Empty = 0."""
    if not values:
        return 0
    ordered = sorted(values)
    mid = len(ordered) // 2
    return ordered[mid] if len(ordered) % 2 == 1 else tdiv(ordered[mid - 1] + ordered[mid], 2)


def relative(target, current):
    """The relative error of a target against the current value in permille, clipped. A current value of zero or less has no denominator: no sample."""
    return None if current <= 0 else clip(tdiv(1000 * (target - current), current))


def mean_of(total, count):
    return None if count <= 0 else clip(tdiv(total, count))


def lost_recon(current, reacquire_ticks, exhausted):
    s = len(reacquire_ticks)
    f = max(0, exhausted)
    if s + f == 0 or current <= 0:
        return None

    total = -CATEGORICAL * f
    if s > 0:
        total += relative(tdiv(median(reacquire_ticks) * 5, 4), current) * s

    return mean_of(total, s + f)


def move_refresh(drift_cells):
    n = len(drift_cells)
    if n == 0:
        return None

    late = sum(1 for d in drift_cells if d >= LATE_DRIFT_CELLS)
    wasted = sum(1 for d in drift_cells if d <= WASTED_DRIFT_CELLS)
    if late * 100 > n * 50:
        return -CATEGORICAL

    return CATEGORICAL if wasted * 100 > n * 60 else 0


def req_margin(results):
    """results: [(target_killed, package_loss_permille)]; a raid that did not kill its target is not a sample."""
    total = 0
    n = 0
    for killed, loss in results:
        if not killed:
            continue

        n += 1
        if loss > 600:
            total += CATEGORICAL
        elif loss < 300:
            total -= CATEGORICAL

    return mean_of(total, n)


def recon_radius(current, reacquire_distance_cells, exhausted_far_seen):
    s = len(reacquire_distance_cells)
    f = max(0, exhausted_far_seen)
    if s + f == 0 or current <= 0:
        return None

    total = CATEGORICAL * f
    if s > 0:
        total += relative(tdiv(median(reacquire_distance_cells) * 5, 4), current) * s

    return mean_of(total, s + f)


def incident_loss(retreats):
    """retreats: [(lost_half_within_600, restrike_clean)]; lost half wins, neither = not a sample."""
    total = 0
    n = 0
    for lost_half, restrike_clean in retreats:
        if lost_half:
            n += 1
            total -= CATEGORICAL
        elif restrike_clean:
            n += 1
            total += CATEGORICAL

    return mean_of(total, n)


def incident_window(current, span_ticks):
    if not span_ticks:
        return None

    return relative(tdiv(median(span_ticks) * 3, 2), current)


def incident_decay(cells):
    """cells: [(reincident_within_1500, passed_clean_before_decay)]; re-incident wins, neither = not a sample."""
    total = 0
    n = 0
    for reincident, passed_clean in cells:
        if reincident:
            n += 1
            total += CATEGORICAL
        elif passed_clean:
            n += 1
            total -= CATEGORICAL

    return mean_of(total, n)


def waypoint_spacing(leg_left_lane):
    if not leg_left_lane:
        return None

    return -CATEGORICAL if any(leg_left_lane) else CATEGORICAL


def defence_radius(outside_defender_took_part):
    n = len(outside_defender_took_part)
    if n == 0:
        return None

    return mean_of(sum(CATEGORICAL if took else -CATEGORICAL for took in outside_defender_took_part), n)


# ---------------------------------------------------------------- rows

class ExtractError(Exception):
    def __init__(self, code, message):
        super().__init__(message)
        self.code = code
        self.message = message


def _fail(message):
    raise ExtractError(5, message)


def _is_int(value):
    return isinstance(value, int) and not isinstance(value, bool)


def _need_int(row, name, where, minimum=0):
    value = row.get(name)
    if not _is_int(value) or value < minimum:
        _fail("%s: %r must be an integer >= %d (got %r)" % (where, name, minimum, value))
    return value


def _need_bool(row, name, where):
    value = row.get(name)
    if not isinstance(value, bool):
        _fail("%s: %r must be true or false (got %r)" % (where, name, value))
    return value


def _need_str(row, name, where):
    value = row.get(name)
    if not isinstance(value, str) or not value:
        _fail("%s: %r must be a non-empty string (got %r)" % (where, name, value))
    return value


def _opt_str(row, name, where):
    value = row.get(name)
    if value is None:
        return None
    if not isinstance(value, str) or not value:
        _fail("%s: %r must be null or a non-empty string (got %r)" % (where, name, value))
    return value


def _kind(row, where, allowed):
    kind = row.get("kind")
    if kind not in allowed:
        _fail("%s: 'kind' must be one of %s (got %r)" % (where, "/".join(allowed), kind))
    return kind


def _payload_lost_recon(row, where):
    kind = _kind(row, where, ("reacquired", "exhausted"))
    payload = {"kind": kind, "current": _need_int(row, "current", where, 1), "counts": True}
    if kind == "reacquired":
        payload["ticks"] = _need_int(row, "ticks", where)
    return payload


def _payload_recon_radius(row, where):
    kind = _kind(row, where, ("reacquired", "exhausted"))
    payload = {"kind": kind, "current": _need_int(row, "current", where, 1)}
    if kind == "reacquired":
        payload["distance"] = _need_int(row, "distance", where)
        payload["counts"] = True
    else:
        payload["counts"] = _need_bool(row, "far_seen", where)
    return payload


def _payload_move_refresh(row, where):
    drift = _need_int(row, "drift", where)
    visible_prev = _need_bool(row, "visible_prev", where)
    visible_now = _need_bool(row, "visible_now", where)
    phase = _need_str(row, "phase", where)
    since_change = _need_int(row, "since_change", where)
    eligible = visible_prev and visible_now and phase in ELIGIBLE_PHASES and since_change >= REFRESH_SETTLE_TICKS
    return {"drift": drift, "counts": eligible}


def _payload_req_margin(row, where):
    killed = _need_bool(row, "killed", where)
    loss = _need_int(row, "loss_permille", where)
    if loss > 1000:
        _fail("%s: 'loss_permille' must be 0..1000 (got %r)" % (where, loss))
    return {"loss": loss, "counts": killed}


def _payload_incident_loss(row, where):
    lost_half = _need_bool(row, "lost_half_within_600", where)
    restrike_clean = _need_bool(row, "restrike_clean", where)
    return {"lost_half": lost_half, "restrike_clean": restrike_clean, "counts": lost_half or restrike_clean}


def _payload_incident_window(row, where):
    return {"span": _need_int(row, "span_ticks", where), "current": _need_int(row, "current", where, 1), "counts": True}


def _payload_incident_decay(row, where):
    reincident = _need_bool(row, "reincident_within_1500", where)
    passed_clean = _need_bool(row, "passed_clean_before_decay", where)
    return {"reincident": reincident, "passed_clean": passed_clean, "counts": reincident or passed_clean}


def _payload_waypoint_spacing(row, where):
    return {"left_lane": _need_bool(row, "left_lane", where), "counts": True}


def _payload_defence_radius(row, where):
    return {"took_part": _need_bool(row, "outside_defender_took_part", where), "counts": True}


PAYLOAD = {
    "raid.lost_recon": _payload_lost_recon,
    "raid.move_refresh": _payload_move_refresh,
    "raid.req_margin": _payload_req_margin,
    "raid.recon_radius": _payload_recon_radius,
    "raid.incident_loss": _payload_incident_loss,
    "raid.incident_window": _payload_incident_window,
    "raid.incident_decay": _payload_incident_decay,
    "raid.waypoint_spacing": _payload_waypoint_spacing,
    "raid.defence_radius": _payload_defence_radius,
}


def parse_row(row, where):
    """Validates one raid_harv_signal row; returns the normalized dict (common fields + payload + `counts`). Raises ExtractError(5) on any defect."""
    key = row.get("key")
    if not isinstance(key, str) or key not in PAYLOAD:
        _fail("%s: unknown key %r" % (where, key))

    parsed = {
        "key": key,
        "game_id": _need_str(row, "game_id", where),
        "seat": _need_str(row, "seat", where),
        "tick": _need_int(row, "tick", where),
        "raid_id": _need_str(row, "raid_id", where),
        "target_player": _need_str(row, "target_player", where),
        "target_style": _opt_str(row, "target_style", where),
    }
    parsed.update(PAYLOAD[key](row, where))
    return parsed


def parse_seat_row(row, where):
    """Validates one raid_harv_seat row: the seat, its personality and faction, and EVERY enemy player (player id -> faction)."""
    enemies = row.get("enemies")
    if not isinstance(enemies, list):
        _fail("%s: 'enemies' must be a list of {player, faction} (got %r)" % (where, enemies))

    players = {}
    for n, entry in enumerate(enemies):
        here = "%s enemies[%d]" % (where, n)
        if not isinstance(entry, dict):
            _fail("%s: must be an object {player, faction} (got %r)" % (here, entry))
        player = _need_str(entry, "player", here)
        if player in players:
            _fail("%s: duplicate enemy player %r" % (here, player))
        players[player] = _need_str(entry, "faction", here)

    return {
        "game_id": _need_str(row, "game_id", where),
        "seat": _need_str(row, "seat", where),
        "personality": _opt_str(row, "personality", where),
        "own_faction": _opt_str(row, "own_faction", where),
        "enemies": players,
    }


# ---------------------------------------------------------------- one seat's nine errors

def _current(rows, key, warnings):
    values = [r["current"] for r in rows]
    if len(set(values)) > 1:
        warnings.append("%s: rows carry different current values %s; the earliest row's value is used" % (key, sorted(set(values))))
    return values[0]


def seat_errors(rows, warnings):
    """rows: the parsed signal rows of ONE seat, ordered by tick. Returns {key: error permille or None}. Only rows that count (eligible / killed / far-seen / observed) are read."""
    counted = {key: [r for r in rows if r["key"] == key and r["counts"]] for key in KEYS}
    errors = {}

    mine = counted["raid.lost_recon"]
    errors["raid.lost_recon"] = lost_recon(
        _current(mine, "raid.lost_recon", warnings), [r["ticks"] for r in mine if r["kind"] == "reacquired"], sum(1 for r in mine if r["kind"] == "exhausted")) if mine else None

    errors["raid.move_refresh"] = move_refresh([r["drift"] for r in counted["raid.move_refresh"]])
    errors["raid.req_margin"] = req_margin([(True, r["loss"]) for r in counted["raid.req_margin"]])

    mine = counted["raid.recon_radius"]
    errors["raid.recon_radius"] = recon_radius(
        _current(mine, "raid.recon_radius", warnings), [r["distance"] for r in mine if r["kind"] == "reacquired"], sum(1 for r in mine if r["kind"] == "exhausted")) if mine else None

    errors["raid.incident_loss"] = incident_loss([(r["lost_half"], r["restrike_clean"]) for r in counted["raid.incident_loss"]])

    mine = counted["raid.incident_window"]
    errors["raid.incident_window"] = incident_window(_current(mine, "raid.incident_window", warnings), [r["span"] for r in mine]) if mine else None

    errors["raid.incident_decay"] = incident_decay([(r["reincident"], r["passed_clean"]) for r in counted["raid.incident_decay"]])
    errors["raid.waypoint_spacing"] = waypoint_spacing([r["left_lane"] for r in counted["raid.waypoint_spacing"]])
    errors["raid.defence_radius"] = defence_radius([r["took_part"] for r in counted["raid.defence_radius"]])
    return errors


def _attribution(seat, rows, enemy_factions, warnings):
    """
    The enemies and the main target of one seat. rows: the seat's parsed signal rows ordered by tick; enemy_factions: {player id: faction} of the seat row.
    Returns (enemies, main_target, main_target_style): EVERY enemy player once with weight 1, the main target owner's faction, and the observed style of that owner only.
    """
    raids = {}
    for r in rows:
        # The emitter snapshots the owner and the observed style AT THE PICK and repeats them on every row of the raid, so a style change in mid-raid cannot reach the rows.
        # A row that still disagrees (an older writer) never refuses the game: the earliest row of the raid names its owner and style, the rest only warn.
        owner = (r["target_player"], r["target_style"])
        info = raids.get(r["raid_id"])
        if info is None:
            if owner[0] not in enemy_factions:
                _fail("seat %s raid %s: target player %r is not an enemy player of the seat row" % (seat, r["raid_id"], owner[0]))
            info = raids[r["raid_id"]] = {"owner": owner, "first": None, "warned": False}
        elif info["owner"] != owner and not info["warned"]:
            info["warned"] = True
            warnings.append("seat %s raid %s: later rows report %r instead of the pick's %r; the pick's owner and style are used" % (seat, r["raid_id"], owner, info["owner"]))
        if r["counts"] and info["first"] is None:
            info["first"] = r["tick"]

    by_owner = {}
    for raid_id, info in raids.items():
        if info["first"] is not None:
            by_owner.setdefault(info["owner"][0], []).append((info["first"], raid_id, info["owner"][1]))

    main_player = min(by_owner, key=lambda p: (-len(by_owner[p]), min(first for first, _, _ in by_owner[p]), p)) if by_owner else None
    main_style = None
    if main_player is not None:
        owner_raids = sorted(by_owner[main_player])
        main_style = owner_raids[-1][2]
        if len({style for _, _, style in owner_raids}) > 1:
            warnings.append("seat %s: the raids against %s report different observed styles; the latest counted raid's style is used" % (seat, main_player))

    enemies = [{"faction": enemy_factions[p], "style": main_style if p == main_player else None, "weight": 1} for p in sorted(enemy_factions)]
    return enemies, (enemy_factions[main_player] if main_player is not None else None), main_style


# ---------------------------------------------------------------- one game

def select_rows(raw_rows, game_id=None):
    """The raid_harv_seat / raid_harv_signal rows (optionally of one game) out of any parsed JSON lines."""
    rows = [r for r in raw_rows if isinstance(r, dict) and r.get("event") in EVENTS]
    if game_id is not None:
        rows = [r for r in rows if r.get("game_id") == game_id]
    return rows


def build_records(rows, seq):
    """The exposure records (fitter input) of ONE game's raid rows. Returns (records, warnings); raises ExtractError (3 = more than one game, 5 = invalid rows)."""
    if not _is_int(seq) or seq < 0:
        _fail("seq must be a non-negative integer (got %r)" % (seq,))

    signals = []
    contexts = {}
    games = set()
    for index, row in enumerate(rows):
        where = "row %d" % index
        event = row.get("event")
        if event == EVENT:
            p = parse_row(row, where)
            p["index"] = index
            signals.append(p)
            games.add(p["game_id"])
        elif event == SEAT_EVENT:
            c = parse_seat_row(row, where)
            if c["seat"] in contexts:
                _fail("%s: a second raid_harv_seat row for seat %s" % (where, c["seat"]))
            contexts[c["seat"]] = c
            games.add(c["game_id"])

    if not games:
        return [], []

    if len(games) != 1:
        raise ExtractError(3, "refused: %d games in one invocation (one step per game, never a batch): %s" % (len(games), ", ".join(sorted(games))))

    game = next(iter(games))
    if not GAME_ID.match(game):
        _fail("game_id %r is not [A-Za-z0-9_.-]+" % (game,))

    seats = {}
    for p in sorted(signals, key=lambda r: (r["tick"], r["index"])):
        seats.setdefault(p["seat"], []).append(p)

    warnings = []
    records = []
    for seat in sorted(seats):
        context = contexts.get(seat)
        if context is None:
            _fail("seat %s has raid_harv_signal rows but no raid_harv_seat row (the roster of enemy players)" % seat)

        errors = seat_errors(seats[seat], warnings)
        if all(error is None for error in errors.values()):
            continue

        enemies, main_target, main_style = _attribution(seat, seats[seat], context["enemies"], warnings)
        records.append({
            "game_id": game,
            "seq": seq,
            "seat": SEAT_PREFIX + seat,
            "personality": context["personality"],
            "own_faction": context["own_faction"],
            "main_target": main_target,
            "main_target_style": main_style,
            "enemies": enemies,
            "knobs": [{"knob": key, "signal": None if errors[key] is None else errors[key] / ERROR_CLIP} for key in KEYS],
        })

    return records, warnings


# ---------------------------------------------------------------- pipeline

def load_rows(paths):
    """The raid_harv_* lines of the DEV logs (any other line is skipped without being parsed). Returns (rows, malformed line count)."""
    rows = []
    malformed = 0
    for path in paths:
        with open(path, "r", encoding="utf-8") as handle:
            for line in handle:
                line = line.strip()
                if not line or not any(event in line for event in EVENTS):
                    continue
                try:
                    rows.append(json.loads(line))
                except json.JSONDecodeError:
                    malformed += 1
    return rows, malformed


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--rows", nargs="+", required=True, help="DEV log JSONL file(s) of ONE game holding the raid_harv_seat and raid_harv_signal rows (other lines are ignored)")
    ap.add_argument("--seq", type=int, required=True, help="the game order, the same value as on the wave-role exposure of this game")
    ap.add_argument("--game-id", default=None, help="keep only the rows of this game (required when the files mix games)")
    ap.add_argument("--out", default=None, help="the exposure JSONL to write (default: stdout)")
    args = ap.parse_args(argv)

    raw, malformed = load_rows(args.rows)
    if malformed:
        print("warning: skipped %d malformed raid_harv line(s)" % malformed, file=sys.stderr)

    try:
        records, warnings = build_records(select_rows(raw, args.game_id), args.seq)
    except ExtractError as exc:
        print(exc.message, file=sys.stderr)
        return exc.code

    for warning in warnings:
        print("warning: " + warning, file=sys.stderr)

    text = "".join(json.dumps(record) + "\n" for record in records)
    if args.out is None:
        sys.stdout.write(text)
    else:
        with open(args.out, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)

    if not records:
        print("no raid evidence in this game: nothing to apply", file=sys.stderr)

    return 0


if __name__ == "__main__":
    sys.exit(main())
