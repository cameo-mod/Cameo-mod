#!/usr/bin/env python3
"""LEARN-ALL-02 P1c: the DEV fitter. ONE bounded learning step per DEV bot game, applied in game order, never in a batch.

This is the python mirror of the DEV half of the learner, OpenRA.Mods.Cameo.Dev/LearnAllTrainer.cs (LearnAllStep, LearnAllTrainer.ApplyGame) and of the read
side in OpenRA.Mods.Cameo/Traits/BotModules/LearnAll/LearnAllMath.cs. They are kept identical by tools/tests/learn_all_golden.json, which both test suites replay
(tools/tests/test_learn_all_fitter.py and LearnAllGoldenTest.cs). The release build has no training code: it only reads the snapshot this writes.

Learning contract (DESIGNER RULE, 2026-11-14 / 2026-11-19): every learned value is the geometric mean (G x F_own x E_eff x S_obs)^(1/4) of four
always-learning full-scale estimates; after EVERY bot game each takes a small bounded log-space step; nothing is ever reset or pulled toward G;
every own personality keeps its own table, a new personality starts from the global ("any") values; a knob with no evidence keeps its estimate.
Training exists only in DEV: the release build reads the committed snapshot (mods/cameo/ai/learned/learn_all.yaml) and never runs this.

Input (one game, from the DEV exposure writer, JSON lines, one record per (bot seat, game)):
  {"game_id": "<[A-Za-z0-9_.-]+>", "seq": <int game order, the same on every record of the game>, "seat": "<unique per game>", "personality": "rush",
   "own_faction": "gdi", "main_target": "nod" | null,
   "enemies": [{"faction": "nod", "style": "boom" | null, "weight": 1}],
   "knobs": [{"knob": "wave_role.frontline", "signal": 0.4 | null}]}
`signal` is the game's signed, bounded outcome in [-1, 1] (null = no evidence for that knob: the previous estimate is kept). `main_target` is the
faction the decision actually used as the main target. ALL seats of the game are combined in ONE step per axis per table (every personality present,
then the global "any" table); a style seeds from the pre-step team enemy value (per-seat enemy factions + the main target).

Pipeline use (post-game plan step, after EVERY DEV game): python tools/ai/fit_learn_all.py --exposures <game_dir>/learn_all_exposure.jsonl
Guards (exit codes): 3 = records of more than one game (no manual batch); 5 = invalid records (a missing, non-integer or mixed `seq`, a bad or missing
game_id, a duplicate or missing `seat`, a game already applied under another seq); 4 = `seq` not above the snapshot's LastSeq (game order; LastSeq never
decreases). A (game, personality, knob) that was already applied is skipped (idempotent).
"""
import argparse
import json
import math
import os
import re
import sys

ONE = 1000
STORE_MIN = 50
STORE_MAX = 20000
RATE = 0.04
MAX_LOG_STEP = 0.08
ANY = "any"
ROOT = "BotLearnAll"
VERSION = 1
DEFAULT_SNAPSHOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "mods", "cameo", "ai", "learned", "learn_all.yaml")


def clamp_store(v):
    return max(STORE_MIN, min(STORE_MAX, int(v)))


def _root_rounded(product, n):
    lo, hi = STORE_MIN, STORE_MAX
    while lo < hi:
        mid = lo + ((hi - lo + 1) >> 1)
        if mid ** n <= product:
            lo = mid
        else:
            hi = mid - 1
    if lo >= STORE_MAX:
        return STORE_MAX
    return lo + 1 if (product << n) >= (2 * lo + 1) ** n else lo


def geo_mean(values):
    if len(values) == 0:
        return ONE
    if len(values) == 1:
        return clamp_store(values[0])
    product = 1
    for v in values:
        product *= clamp_store(v)
    return _root_rounded(product, len(values))


def resolve(g, f, e, s):
    return geo_mean([g, f, e]) if s <= 0 else geo_mean([g, f, e, s])


def seed_style(g, f, e):
    return geo_mean([g, f, e])


def team_enemy(per_enemy, main):
    if len(per_enemy) == 0:
        return clamp_store(main)
    return geo_mean([geo_mean(per_enemy), main])


def step_next(current, signal):
    cur = clamp_store(current)
    if signal is None or isinstance(signal, bool) or not isinstance(signal, (int, float)) or math.isnan(signal) or math.isinf(signal):
        return cur
    s = max(-1.0, min(1.0, float(signal)))
    step = max(-MAX_LOG_STEP, min(MAX_LOG_STEP, RATE * s))
    return clamp_store(math.floor(cur * math.exp(step) + 0.5))


class Axes:
    def __init__(self):
        self.global_ = ONE
        self.own = {}
        self.enemy = {}
        self.style = {}

    def get_own(self, faction):
        return self.own.get(faction, self.global_) if faction is not None else self.global_

    def get_enemy(self, faction):
        return self.enemy.get(faction, self.global_) if faction is not None else self.global_

    def clone(self):
        c = Axes()
        c.global_ = self.global_
        c.own = dict(self.own)
        c.enemy = dict(self.enemy)
        c.style = dict(self.style)
        return c


class Table:
    def __init__(self):
        self.personalities = {}
        self.applied = set()
        self.games = {}  # game id -> seq (python-only bookkeeping, the C# reader ignores it)
        self.last_seq = None

    def find(self, personality, knob):
        if knob is None:
            return None
        if personality is not None and personality in self.personalities and knob in self.personalities[personality]:
            return self.personalities[personality][knob]
        return self.personalities.get(ANY, {}).get(knob)

    def resolve(self, personality, knob, own_faction, enemies, main_target, style):
        axes = self.find(personality, knob)
        if axes is None:
            return ONE
        main = axes.get_enemy(main_target)
        if not enemies:
            enemy_eff = main
        else:
            enemy_eff = team_enemy([axes.get_enemy(x) for x in enemies], main)
        s = axes.style.get(style, 0) if style is not None else 0
        return resolve(axes.global_, axes.get_own(own_faction), enemy_eff, s)

    def is_applied(self, key):
        return key in self.applied


SCALE = 10 ** 9


def _q(x):
    """1e-9 quantization, symmetric half away from zero (mirrors LearnAllTrainer.Q), so seat sums are exact integers and order-free."""
    sign = (x > 0) - (x < 0)
    return sign * int(math.floor(abs(x) * SCALE + 0.5))


def _finite(signal):
    return isinstance(signal, (int, float)) and not isinstance(signal, bool) and not math.isnan(signal) and not math.isinf(signal)


class Seat:
    """One bot seat's view of one game for one knob (mirrors SeatObservation)."""

    def __init__(self, personality, own_faction, enemies, main_target, signal):
        self.personality = personality or None
        self.own_faction = own_faction
        self.enemies = list(enemies)  # [(faction, style, weight)]
        self.main_target = main_target
        self.signal = signal


def _seat_enemy_eff(pre, enemies, main_target):
    per = [pre.get_enemy(f) for (f, _s, _w) in enemies]
    main = pre.get_enemy(main_target) if main_target is not None else geo_mean(per)
    if not per:
        return main if main_target is not None else pre.global_
    return team_enemy(per, main)


def _add(mapping, key, contribution):
    s, n = mapping.get(key, (0, 0))
    mapping[key] = (s + _q(contribution), n + 1)


def _mean(pair):
    return pair[0] / (pair[1] * SCALE)


def _step_table(table, personality, knob, seats):
    existing = table.personalities.get(personality, {}).get(knob)
    if existing is not None:
        pre = existing.clone()
    elif personality != ANY and knob in table.personalities.get(ANY, {}):
        pre = table.personalities[ANY][knob].clone()
    else:
        pre = Axes()
    axes = existing if existing is not None else table.personalities.setdefault(personality, {}).setdefault(knob, pre.clone())

    global_sum = 0
    own_map, enemy_map, style_map, style_seeds = {}, {}, {}, {}
    for seat in seats:
        s = float(seat.signal)
        global_sum += _q(s)
        if seat.own_faction is not None:
            _add(own_map, seat.own_faction, s)
        enemies = [e for e in seat.enemies if e[2] > 0]
        total = sum(e[2] for e in enemies)
        if total == 0:
            continue
        share, style_share = {}, {}
        for (faction, style, w) in enemies:
            part = float(w) / total
            if faction is not None:
                share[faction] = share.get(faction, 0.0) + part
            if style is not None:
                style_share[style] = style_share.get(style, 0.0) + part
        for faction in share:
            _add(enemy_map, faction, s * share[faction])
        if style_share:
            seed = seed_style(pre.global_, pre.get_own(seat.own_faction), _seat_enemy_eff(pre, enemies, seat.main_target))
            for style in style_share:
                _add(style_map, style, s * style_share[style])
                style_seeds.setdefault(style, []).append(seed)

    axes.global_ = step_next(pre.global_, global_sum / (len(seats) * SCALE))
    for k in sorted(own_map):
        axes.own[k] = step_next(pre.get_own(k), _mean(own_map[k]))
    for k in sorted(enemy_map):
        axes.enemy[k] = step_next(pre.get_enemy(k), _mean(enemy_map[k]))
    for k in sorted(style_map):
        current = pre.style[k] if k in pre.style else geo_mean(style_seeds[k])
        axes.style[k] = step_next(current, _mean(style_map[k]))


def apply_game(table, game_id, knob, seats):
    """Mirrors LearnAllTrainer.ApplyGame: all eligible seats of the game combined, one step per axis per table (each personality present, then 'any')."""
    if game_id is None or knob is None or not seats:
        return False
    valid = [s for s in seats if _finite(s.signal)]
    if not valid:
        return False
    targets = sorted({s.personality for s in valid if s.personality and s.personality != ANY}) + [ANY]
    changed = False
    for target in targets:
        key = "%s|%s|%s" % (game_id, target, knob)
        if table.is_applied(key):
            continue
        group = valid if target == ANY else [s for s in valid if s.personality == target]
        _step_table(table, target, knob, group)
        table.applied.add(key)
        changed = True
    return changed


# ---------------------------------------------------------------- snapshot text (mirrors LearnAllSnapshot.Write / Parse)

def write_text(table, include_applied=True):
    out = [ROOT + ":\n", "\tVersion: %d\n" % VERSION]
    if table.last_seq is not None:
        out.append("\tLastSeq: %d\n" % table.last_seq)
    if include_applied and table.games:
        out.append("\tGames:\n")
        for game in sorted(table.games):
            out.append("\t\t%s: %d\n" % (game, table.games[game]))
    if include_applied and table.applied:
        out.append("\tApplied:\n")
        for key in sorted(table.applied):
            out.append("\t\t%s:\n" % key)
    for personality in sorted(table.personalities):
        for knob in sorted(table.personalities[personality]):
            axes = table.personalities[personality][knob]
            out.append("\tKnob@%s__%s:\n" % (personality, knob))
            out.append("\t\tGlobal: %d\n" % axes.global_)
            for k in sorted(axes.own):
                out.append("\t\tOwn@%s: %d\n" % (k, axes.own[k]))
            for k in sorted(axes.enemy):
                out.append("\t\tEnemy@%s: %d\n" % (k, axes.enemy[k]))
            for k in sorted(axes.style):
                out.append("\t\tStyle@%s: %d\n" % (k, axes.style[k]))
    return "".join(out)


def parse_text(text):
    """Returns the table (table.last_seq / table.games carry the python bookkeeping). A damaged / foreign / other-version text is an empty table (never zero)."""
    table = Table()
    if not text or not text.strip():
        return table
    lines = [ln.rstrip("\r\n") for ln in text.split("\n") if ln.strip() and not ln.lstrip().startswith("#")]
    if not lines or lines[0].strip() != ROOT + ":":
        return table
    entries, current = [], None
    for ln in lines[1:]:
        depth = len(ln) - len(ln.lstrip("\t"))
        body = ln.strip()
        if depth == 1:
            current = (body, [])
            entries.append(current)
        elif depth >= 2 and current is not None:
            current[1].append(body)
    version = None
    for head, _children in entries:
        if head.startswith("Version:"):
            try:
                version = int(head.split(":", 1)[1].strip())
            except ValueError:
                version = None
    if version != VERSION:
        return Table()
    for head, children in entries:
        if head.startswith("LastSeq:"):
            try:
                table.last_seq = int(head.split(":", 1)[1].strip())
            except ValueError:
                table.last_seq = None
        elif head == "Games:":
            for c in children:
                name, _, value = c.rpartition(":")
                try:
                    table.games[name.strip()] = int(value.strip())
                except ValueError:
                    continue
        elif head == "Applied:":
            for c in children:
                table.applied.add(c[:-1] if c.endswith(":") else c)
        elif head.startswith("Knob@") and head.endswith(":"):
            key = head[len("Knob@"):-1]
            split = key.find("__")
            if split <= 0 or split + 2 >= len(key):
                continue
            axes = table.personalities.setdefault(key[:split], {}).setdefault(key[split + 2:], Axes())
            for c in children:
                name, _, value = c.partition(":")
                try:
                    v = int(value.strip())
                except ValueError:
                    continue
                if v <= 0:
                    continue
                v = clamp_store(v)
                if name == "Global":
                    axes.global_ = v
                elif name.startswith("Own@"):
                    axes.own[name[4:]] = v
                elif name.startswith("Enemy@"):
                    axes.enemy[name[6:]] = v
                elif name.startswith("Style@"):
                    axes.style[name[6:]] = v
    return table


# ---------------------------------------------------------------- pipeline

def load_records(paths):
    records = []
    for path in paths:
        with open(path, "r", encoding="utf-8") as handle:
            for n, line in enumerate(handle, 1):
                line = line.strip()
                if not line:
                    continue
                try:
                    records.append(json.loads(line))
                except json.JSONDecodeError as exc:
                    raise SystemExit("%s:%d: not JSON (%s)" % (path, n, exc))
    return records


GAME_ID = re.compile(r"^[A-Za-z0-9_.\-]+$")


def _is_int(value):
    return isinstance(value, int) and not isinstance(value, bool)


def check_records(records):
    """Strict validation of ONE game's records. Returns (game_id, seq, error); error is None or (exit code, message)."""
    ids = {r.get("game_id") for r in records}
    if len(ids) != 1:
        return None, None, (3, "refused: %d games in one invocation (one step per game, never a batch): %s" % (len(ids), ", ".join(sorted(map(str, ids)))))
    game = next(iter(ids))
    if not isinstance(game, str) or not GAME_ID.match(game):
        return None, None, (5, "refused: game_id %r is missing or not [A-Za-z0-9_.-]+" % (game,))
    seqs = {r["seq"] if _is_int(r.get("seq")) and r["seq"] >= 0 else None for r in records}
    if None in seqs or len(seqs) != 1:
        return game, None, (5, "refused: game %s has a missing, non-integer, negative or mixed seq (every record needs the same integer seq)" % game)
    seats = [r.get("seat") for r in records]
    if any(not isinstance(x, str) or not x for x in seats) or len(set(seats)) != len(seats):
        return game, None, (5, "refused: game %s has a missing or duplicate seat id" % game)
    return game, next(iter(seqs)), None


def seats_by_knob(records):
    """Groups the records of one game by knob into Seat lists (every seat of the game, in one step)."""
    result = {}
    for rec in records:
        enemies = [(e.get("faction"), e.get("style"), int(e.get("weight", 1))) for e in rec.get("enemies", [])]
        for k in rec.get("knobs", []):
            result.setdefault(k["knob"], []).append(Seat(rec.get("personality"), rec.get("own_faction"), enemies, rec.get("main_target"), k.get("signal")))
    return result


def apply_records(table, game, records):
    """Applies every knob of ONE (validated) game. Returns the number of (personality, knob) steps taken."""
    steps = 0
    grouped = seats_by_knob(records)
    for knob in sorted(grouped):
        before = len(table.applied)
        if apply_game(table, game, knob, grouped[knob]):
            steps += len(table.applied) - before
    return steps


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--exposures", nargs="+", required=True, help="exposure JSONL of ONE game (the post-game step passes that game's file)")
    ap.add_argument("--snapshot", default=DEFAULT_SNAPSHOT)
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args(argv)

    records = load_records(args.exposures)
    if not records:
        print("no exposure records: nothing to apply", file=sys.stderr)
        return 0

    game, seq, error = check_records(records)
    if error is not None:
        print(error[1], file=sys.stderr)
        return error[0]

    path = os.path.normpath(args.snapshot)
    text = open(path, "r", encoding="utf-8").read() if os.path.exists(path) else ""
    table = parse_text(text)
    known = table.games.get(game)
    already = known is not None or any(k.startswith(game + "|") for k in table.applied)
    if known is not None and known != seq:
        print("refused: game %s was applied with seq %d, now seq %d" % (game, known, seq), file=sys.stderr)
        return 5
    if not already and table.last_seq is not None and seq <= table.last_seq:
        print("refused: game seq %d is not above the snapshot's LastSeq %d (games are applied in order)" % (seq, table.last_seq), file=sys.stderr)
        return 4

    steps = apply_records(table, game, records)
    if steps == 0:
        print("game %s: nothing to apply (already applied or no evidence); snapshot unchanged" % game)
        return 0
    table.games[game] = seq
    table.last_seq = seq if table.last_seq is None else max(table.last_seq, seq)
    new_text = write_text(table)
    if args.dry_run:
        sys.stdout.write(new_text)
        return 0
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(new_text)
    os.replace(tmp, path)
    print("game %s: %d steps applied -> %s" % (game, steps, path))
    return 0


if __name__ == "__main__":
    sys.exit(main())
