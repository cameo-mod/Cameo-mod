#!/usr/bin/env python3
"""LEARN-ALL-02 P1c: the DEV fitter. ONE bounded learning step per DEV bot game, applied in game order, never in a batch.

This is the python mirror of OpenRA.Mods.Cameo/Traits/BotModules/LearnAll/LearnAllMath.cs (LearnAllStep, LearnTable.Apply/ApplyGame). The two are kept
identical by tools/tests/learn_all_golden.json, which both test suites replay (tools/tests/test_learn_all_fitter.py and LearnAllGoldenTest.cs).

Learning contract (DESIGNER RULE, 2026-11-14 / 2026-11-19): every learned value is the geometric mean (G x F_own x E_eff x S_obs)^(1/4) of four
always-learning full-scale estimates; after EVERY bot game each takes a small bounded log-space step; nothing is ever reset or pulled toward G;
every own personality keeps its own table, a new personality starts from the global ("any") values; a knob with no evidence keeps its estimate.
Training exists only in DEV: the release build reads the committed snapshot (mods/cameo/ai/learned/learn_all.yaml) and never runs this.

Input (one game, from the DEV exposure writer, JSON lines, one record per (bot player, game)):
  {"game_id": "<stable id>", "seq": <int game order>, "personality": "rush", "own_faction": "gdi",
   "enemies": [{"faction": "nod", "style": "boom" | null, "weight": 1}],
   "knobs": [{"knob": "wave_role.frontline", "signal": 0.4 | null}]}
`signal` is the game's signed, bounded outcome in [-1, 1] (null = no evidence for that knob: the previous estimate is kept).

Pipeline use (post-game plan step, after EVERY DEV game): python tools/ai/fit_learn_all.py --exposures <game_dir>/learn_all_exposure.jsonl
Guards: a record whose (game, personality, knob) was already applied is skipped (idempotent); an invocation with records of more than one game is
refused (no manual batch); a game whose `seq` is not above the snapshot's LastSeq is refused (game order).
"""
import argparse
import json
import math
import os
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

    def _ensure(self, personality, knob):
        table = self.personalities.setdefault(personality, {})
        if knob in table:
            return table[knob]
        any_axes = self.personalities.get(ANY, {}).get(knob)
        table[knob] = any_axes.clone() if any_axes is not None else Axes()
        return table[knob]

    @staticmethod
    def _applied_key(game_id, personality, knob):
        return "%s|%s|%s" % (game_id, personality if personality is not None else ANY, knob)

    def apply(self, game_id, personality, knob, own_faction, enemies, signal):
        """enemies: list of (faction, style, weight). Mirrors LearnTable.Apply."""
        if game_id is None or knob is None:
            return False
        if signal is None or isinstance(signal, bool) or not isinstance(signal, (int, float)) or math.isnan(signal) or math.isinf(signal):
            return False
        key = self._applied_key(game_id, personality, knob)
        if key in self.applied:
            return False
        s = float(signal)
        axes = self._ensure(personality if personality is not None else ANY, knob)
        pre = axes.clone()
        axes.global_ = step_next(pre.global_, s)
        if own_faction is not None:
            axes.own[own_faction] = step_next(pre.get_own(own_faction), s)
        if enemies:
            total = 0
            for (_f, _s, w) in enemies:
                total += max(0, w)
            if total > 0:
                share, style_share = {}, {}
                for (faction, style, w) in enemies:
                    if w <= 0:
                        continue
                    part = float(w) / total
                    if faction is not None:
                        share[faction] = share.get(faction, 0.0) + part
                    if style is not None:
                        style_share[style] = style_share.get(style, 0.0) + part
                for faction in sorted(share):
                    axes.enemy[faction] = step_next(pre.get_enemy(faction), s * share[faction])
                main = _main_faction(enemies)
                for style in sorted(style_share):
                    seed = pre.style[style] if style in pre.style else seed_style(pre.global_, pre.get_own(own_faction), pre.get_enemy(main))
                    axes.style[style] = step_next(seed, s * style_share[style])
        self.applied.add(key)
        return True

    def apply_game(self, game_id, personality, knob, own_faction, enemies, signal):
        changed = False
        if personality and personality != ANY:
            changed |= self.apply(game_id, personality, knob, own_faction, enemies, signal)
        changed |= self.apply(game_id, ANY, knob, own_faction, enemies, signal)
        return changed


def _main_faction(enemies):
    best, weight = None, -(2 ** 31)
    for (faction, _style, w) in enemies:
        if w > weight:
            weight, best = w, faction
    return best


# ---------------------------------------------------------------- snapshot text (mirrors LearnAllSnapshot.Write / Parse)

def write_text(table, include_applied=True, last_seq=None):
    out = [ROOT + ":\n", "\tVersion: %d\n" % VERSION]
    if last_seq is not None:
        out.append("\tLastSeq: %d\n" % last_seq)
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
    """Returns (table, last_seq). A damaged / foreign / other-version text is an empty table (never zero)."""
    table, last_seq = Table(), None
    if not text or not text.strip():
        return table, last_seq
    lines = [ln.rstrip("\r\n") for ln in text.split("\n") if ln.strip() and not ln.lstrip().startswith("#")]
    if not lines or lines[0].strip() != ROOT + ":":
        return table, last_seq
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
        return Table(), None
    for head, children in entries:
        if head.startswith("LastSeq:"):
            try:
                last_seq = int(head.split(":", 1)[1].strip())
            except ValueError:
                last_seq = None
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
    return table, last_seq


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


def apply_records(table, records):
    """Applies every record of ONE game. Returns the number of (personality, knob) steps taken."""
    steps = 0
    for rec in records:
        enemies = [(e.get("faction"), e.get("style"), int(e.get("weight", 1))) for e in rec.get("enemies", [])]
        for k in rec.get("knobs", []):
            if table.apply_game(str(rec["game_id"]), rec.get("personality") or None, k["knob"], rec.get("own_faction"), enemies, k.get("signal")):
                steps += 1
    return steps


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--exposures", nargs="+", required=True, help="exposure JSONL of ONE game (the post-game step passes that game's file)")
    ap.add_argument("--snapshot", default=DEFAULT_SNAPSHOT)
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args(argv)

    records = load_records(args.exposures)
    games = {str(r["game_id"]) for r in records}
    if len(games) > 1:
        print("refused: %d games in one invocation (one step per game, never a batch): %s" % (len(games), ", ".join(sorted(games))), file=sys.stderr)
        return 3
    if not games:
        print("no exposure records: nothing to apply", file=sys.stderr)
        return 0

    path = os.path.normpath(args.snapshot)
    text = open(path, "r", encoding="utf-8").read() if os.path.exists(path) else ""
    table, last_seq = parse_text(text)
    seqs = {r.get("seq") for r in records}
    seq = max((s for s in seqs if isinstance(s, int)), default=None)
    game = next(iter(games))
    already = any(k.startswith(game + "|") for k in table.applied)
    if seq is not None and last_seq is not None and seq <= last_seq and not already:
        print("refused: game seq %d is not above the snapshot's LastSeq %d (games are applied in order)" % (seq, last_seq), file=sys.stderr)
        return 4

    steps = apply_records(table, records)
    if steps == 0:
        print("game %s: nothing to apply (already applied or no evidence); snapshot unchanged" % game)
        return 0
    new_text = write_text(table, True, seq if seq is not None else last_seq)
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
