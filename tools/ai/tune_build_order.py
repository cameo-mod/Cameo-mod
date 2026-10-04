"""tune_build_order — the offline half of the build-order lab (AI_ARCHITECTURE.md 12.25, DESIGN.md 19.2).

Route 2 ("tuned", coordinate descent over the BO-1 knobs) and route 3 ("chosen", the opening bandit).
Reads match directories (each a `run_ai_match_batch.py --support-dir`, holding `Logs/`) and:

1. **Scores** every 1v1 match of a bot with a `build_order` situation record as **win + margin (+ speed bonus)**:
       score = win + margin + speed
       win    = 1 for a win, 0 for a loss (undecided matches are dropped)
       margin = (killed - lost) / (killed + lost), the destroyed-to-lost value, in [-1, 1]
       speed  = SPEED_WEIGHT x (1 - duration / SPEED_REF_TICKS), clamped to [0, 1], wins only (a faster win scores more)
2. **Reports** per (personality, own faction): the arms (the batch directory name minus its `_N` suffix), their n, win rate and
   mean score, and per opening the n / wins.
3. **Proposes** (`--propose OUT_DIR`) the next paired experiment per (personality, faction): ONE knob +/- DELTA against the
   current best (the committed learned file), as a base arm and one perturbed arm. Each arm is a learned-file copy
   (`<arm>.yaml`) plus a group in `<OUT_DIR>/experiment_switches.yaml`, which `apply_increment_switches.py --spec` arms next to
   `AK_build_order_knobs` (UseLearnedBuildOrder + LearnedFile). The harness names each arm's batch directory after the arm:
       bo__<personality>__<faction>__base
       bo__<personality>__<faction>__<knob>__up<delta>   /   ...__dn<delta>
4. **Writes** (`--write`) `mods/cameo/ai/learned/build_order_knobs.yaml`, bounded and only with enough evidence:
   * knobs: an arm replaces the base only when BOTH arms have >= MIN_MATCHES matches AND the mean-score gain is significant.
     The test is PAIRED when the arm and base matches can be matched one-to-one by experimental cell (same enemy faction, map and
     spawn side; the k-th replicate of a cell in the arm against the k-th in the base) with >= MIN_MATCHES pairs:
     z = mean_d / (sd_d / sqrt(n_pairs)) on the per-pair score differences (this cancels the map / spawn-side effect). Otherwise it
     falls back to Welch z = (mean_arm - mean_base) / sqrt(var_arm/n_arm + var_base/n_base). Each decision says which (`test`).
     z is one-sided, by the normal approximation (the n >= MIN_MATCHES floor keeps that honest). Several candidate arms of one
     (personality, faction) are tested against the SAME base, so the family is Holm-Bonferroni corrected at one-sided alpha =
     1 - Phi(Z_CRIT) (0.025): the k-th best z must reach the critical z of alpha / (m - k); m == 1 is plain Z_CRIT (1.96).
     Per (personality, faction) at most the single best significant arm is accepted per write (coordinate descent: one step, then
     re-measure). The learned multiplier is the old one x (1 +/- delta) clamped to [LEARN_MIN, LEARN_MAX] thousandths: a bounded
     multiplier on the default.
   * openings: Beta(alpha, beta) per (personality, own faction, enemy faction, opening); only matches played at the current-best
     knobs (the base / control arm, never a knob-perturbed experiment arm, whose result is confounded by the knob change) are
     folded in: a matchup with >= MIN_MATCHES such matches adds wins to alpha and losses to beta; when alpha + beta exceeds
     POSTERIOR_CAP both halve (recency discount). Each game is used once (the file keeps the processed game ids; perturbed-arm
     games are recorded there as skipped), so a re-run on the same directories changes nothing.
   Without --write nothing is changed.

Pure python (stdlib only). The yaml is a committed, reviewed data file (DESIGN 19.2: frozen in release, trained on dev).
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import statistics
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import ai_log_common as c  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[2]
LEARNED_PATH = REPO / "mods" / "cameo" / "ai" / "learned" / "build_order_knobs.yaml"

KNOBS = ["tempo", "greed", "production", "tech", "defence", "power_margin", "expansion", "support"]
# `expansion` is published for the expansion planner but no consumer reads it yet, so an experiment on it could only measure noise.
TUNABLE = [k for k in KNOBS if k != "expansion"]
# The objective is shared: ai_log_common.match_score (SCORE_SPEED_WEIGHT/SCORE_SPEED_REF_TICKS) - the
# build-order report displays the same number this gate optimizes (review 2026-10-02 §6.2).
SPEED_WEIGHT = c.SCORE_SPEED_WEIGHT
SPEED_REF_TICKS = c.SCORE_SPEED_REF_TICKS
MIN_MATCHES = 20
Z_CRIT = 1.96
DELTA = 100  # thousandths: one coordinate step is +-10%
LEARN_MIN = 800
LEARN_MAX = 1250
POSTERIOR_CAP = 200
MIN_DURATION_TICKS = 3000  # a match that ended before the bots did anything carries no signal

# ---- tier 4 (SPSA; docs/design/TIER4_SPSA_SPEC.md) ---------------------------
SPSA_GAIN_A, SPSA_GAIN_C, SPSA_STAB = 0.10, 0.08, 10  # a, c, stability constant A (R1 re-run, spec 4.7)
SPSA_SCALE_FULL, SPSA_SCALE_WEAK = 1.0, 0.25          # |z| >= crit -> full step, else damped (ruling R1)
SPSA_MASK_LOG = 0.005                               # clamped effective perturbations below this contribute nothing
EL_WEIGHT = 0.5                                     # composite = match score + 0.5 x mean total_milli/1000 (R3)

ARM_RE = re.compile(r"^bo__(?P<personality>[a-z0-9]+)__(?P<faction>[a-z0-9_]+?)__(?:base|(?P<knob>[a-z_]+?)__(?P<dir>up|dn)(?P<delta>\d+))$")


# ---------------------------------------------------------------- reading the logs

def read_jsonl(path: pathlib.Path) -> list[dict]:
    if not path.exists():
        return []
    rows = []
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        try:
            rows.append(json.loads(line))
        except json.JSONDecodeError:
            pass
    return rows


def arm_of(batch_dir: pathlib.Path) -> str:
    return re.sub(r"_\d+$", "", batch_dir.name)


def score_match(outcome: str, killed: float, lost: float, duration_ticks: int) -> tuple[float, float] | None:
    """(score, win) of one match, or None when it carries no signal (undecided, too short)."""
    if outcome not in ("won", "lost") or duration_ticks < MIN_DURATION_TICKS:
        return None
    s = c.match_score(outcome, killed, lost, duration_ticks)
    return s["score"], s["win"]


def build_order_of(situation: dict) -> dict | None:
    bo = situation.get("build_order")
    if bo is None:
        bo = (situation.get("own") or {}).get("build_order")
    return bo if isinstance(bo, dict) else None


def load_engagement_means(batch_dirs: list[pathlib.Path]) -> dict[tuple[str, str], list]:
    """(game_uid, player) -> [total_milli sum, n] over non-skirmish engagement records (spec 2, R3).

    `score.total_milli` is the per-fight verdict clamped to +-1000 (AI_MATCH_LOG.md); skirmish records carry
    no decision signal and are skipped."""
    means: dict[tuple[str, str], list] = {}
    for d in batch_dirs:
        for r in read_jsonl(d / "Logs" / "cameo-ai-engagements.jsonl"):
            score = r.get("score") or {}
            total = score.get("total_milli")
            if total is None or r.get("skirmish"):
                continue
            s = means.setdefault((r.get("game_uid", ""), r.get("player", "")), [0.0, 0])
            s[0] += total
            s[1] += 1
    return means


def load_engagement_coverage(batch_dirs: list[pathlib.Path]) -> dict[tuple[str, str, str], list]:
    """(arm, game_uid, player) -> [usable, skirmish, total] engagement census for --coverage: `usable` is what
    load_engagement_means counts (score.total_milli present and not skirmish); `skirmish` are flagged records;
    `total` is every record for the pair (usable + skirmish + in-flight telemetry). The arm scopes the key so
    the same (game, player) id in two batch dirs can never merge its EL evidence."""
    census: dict[tuple[str, str, str], list] = {}
    for d in batch_dirs:
        for r in read_jsonl(d / "Logs" / "cameo-ai-engagements.jsonl"):
            c = census.setdefault((arm_of(d), r.get("game_uid", ""), r.get("player", "")), [0, 0, 0])
            c[2] += 1
            score = r.get("score") or {}
            if r.get("skirmish"):
                c[1] += 1
            elif score.get("total_milli") is not None:
                c[0] += 1
    return census


DROP_REASONS = ["not_1v1", "no_build_order", "outcome_undecided", "duration_gate", "no_personality"]
EL_BUCKETS = ["el_ok", "el_skirmish_only", "el_no_score", "el_unscored"]


def side_of(arm: str) -> str:
    """Experiment side for --coverage: an SPSA arm is 'plus'/'minus', a coordinate knob arm is 'arm',
    anything else is 'base' (control / plain batch)."""
    parsed = spsa_arm(arm)
    if parsed is not None:
        return parsed[3]
    return "arm" if is_perturbed_arm(arm) else "base"


def load_matches_detailed(batch_dirs: list[pathlib.Path]) -> tuple[list[dict], dict]:
    """load_matches plus a per-(personality, faction, side) coverage funnel: match records seen, scored, and
    drops by reason, plus how many scored rows actually joined a usable engagement verdict (EL buckets).
    Drop counters attribute to the record's own player personality/faction; a record with neither lands in '?'."""
    el_means = load_engagement_means(batch_dirs)
    el_census = load_engagement_coverage(batch_dirs)
    rows: list[dict] = []
    cov: dict[tuple[str, str, str], dict] = {}

    def bucket(r: dict, arm: str, bo: dict | None) -> dict:
        player = r.get("player") or {}
        personality = (bo or {}).get("personality") or player.get("personality") or "?"
        faction = player.get("faction") or "?"
        return cov.setdefault((personality, faction, side_of(arm)),
                              {"seen": 0, "scored": 0, **{f"drop_{k}": 0 for k in DROP_REASONS},
                               **{k: 0 for k in EL_BUCKETS}})

    for d in batch_dirs:
        records = read_jsonl(d / "Logs" / "cameo-ai-matches.jsonl")
        situations = read_jsonl(d / "Logs" / "cameo-ai-situations.jsonl")
        arm = arm_of(d)

        first_bo: dict[tuple[str, str], dict] = {}
        last_bo: dict[tuple[str, str], dict] = {}
        for s in sorted(situations, key=lambda r: r.get("tick", 0)):
            bo = build_order_of(s)
            if bo is None:
                continue
            key = (s.get("game_uid", ""), s.get("player", ""))
            first_bo.setdefault(key, bo)
            last_bo[key] = bo

        for r in records:
            player = r.get("player") or {}
            opponents = r.get("opponents") or []
            key = (r.get("game_uid", ""), player.get("name", ""))
            b = bucket(r, arm, first_bo.get(key))
            b["seen"] += 1
            if len(opponents) != 1 or r.get("allies"):
                b["drop_not_1v1"] += 1
                continue  # a team result says nothing about one bot's build order
            if key not in first_bo:
                b["drop_no_build_order"] += 1
                continue
            stats = r.get("stats") or {}
            outcome = player.get("outcome", "")
            if outcome not in ("won", "lost"):
                b["drop_outcome_undecided"] += 1
                continue
            if r.get("duration_ticks", 0) < MIN_DURATION_TICKS:
                b["drop_duration_gate"] += 1
                continue
            scored = score_match(outcome, stats.get("kills_cost", 0), stats.get("deaths_cost", 0),
                                 r.get("duration_ticks", 0))
            if scored is None:
                continue
            first, last = first_bo[key], last_bo[key]
            personality = first.get("personality") or player.get("personality") or ""
            if not personality:
                b["drop_no_personality"] += 1
                continue
            b["scored"] += 1
            usable, skirmish, total = el_census.get((arm, *key), [0, 0, 0])
            if usable:
                b["el_ok"] += 1
            elif total == 0:
                b["el_no_score"] += 1
            elif skirmish:
                b["el_skirmish_only"] += 1
            else:
                b["el_unscored"] += 1
            rows.append({
                "game_uid": key[0],
                "arm": arm,
                "personality": personality,
                "faction": player.get("faction", ""),
                "enemy_faction": opponents[0].get("faction", ""),
                "map": r.get("map_uid", "") or "",
                "spawn": player.get("spawn", -1),
                "opening": last.get("opening", "") or "",
                "knobs": {k: first.get(f"base_{k}", 1000) for k in KNOBS},
                "score": scored[0],
                "win": scored[1],
                "el_mean_milli": (el_means.get(key) or [0.0, 0])[0] / max(1, (el_means.get(key) or [0.0, 0])[1]),
                "el_n": (el_means.get(key) or [0.0, 0])[1],
            })
    return rows, cov


def load_matches(batch_dirs: list[pathlib.Path]) -> list[dict]:
    """One row per scored 1v1 bot match: arm, personality, faction, enemy faction, opening, base knob vector, score, win."""
    rows, _ = load_matches_detailed(batch_dirs)
    return rows


def coverage_json(cov: dict, min_matches: int = MIN_MATCHES) -> dict:
    """Machine-readable --coverage output: one entry per (personality, faction, side), ordinal-sorted."""
    cells = []
    for key in sorted(cov):
        b = cov[key]
        personality, faction, side = key
        cells.append({"personality": personality, "faction": faction, "side": side, **b,
                      "short_of_min": max(0, min_matches - b["scored"])})
    return {"min_matches": min_matches, "drop_reasons": DROP_REASONS, "el_buckets": EL_BUCKETS,
            "cells": cells}


def coverage_report(cov: dict, min_matches: int = MIN_MATCHES) -> str:
    """Deterministic text table for --coverage: one row per (personality, faction, side), ordinal-sorted."""
    headers = ["personality", "faction", "side", "seen", "scored", "short",
               *[f"drop_{k}" for k in DROP_REASONS], *EL_BUCKETS]
    table = [headers]
    for key in sorted(cov):
        personality, faction, side = key
        b = cov[key]
        table.append([personality, faction, side, str(b["seen"]), str(b["scored"]),
                      str(max(0, min_matches - b["scored"])),
                      *[str(b[f"drop_{k}"]) for k in DROP_REASONS], *[str(b[k]) for k in EL_BUCKETS]])
    width = [max(len(row[i]) for row in table) for i in range(len(headers))]
    out = [f"coverage: {sum(b['seen'] for b in cov.values())} match record(s); "
           f"MIN_MATCHES={min_matches} per side (short = matches still needed to measure)"]
    out += ["  ".join(cell.ljust(width[i]) for i, cell in enumerate(row)).rstrip() for row in table]
    return "\n".join(out)


# ---------------------------------------------------------------- statistics

class Stats:
    """Running mean and variance (Welford)."""

    def __init__(self) -> None:
        self.n = 0
        self.mean = 0.0
        self.m2 = 0.0

    def add(self, x: float) -> None:
        self.n += 1
        d = x - self.mean
        self.mean += d / self.n
        self.m2 += d * (x - self.mean)

    @property
    def var(self) -> float:
        return self.m2 / (self.n - 1) if self.n > 1 else 0.0


def welch_z(arm: Stats, base: Stats) -> float:
    """Welch z of (arm - base) on mean score; +-inf when both variances are zero but the means differ."""
    diff = arm.mean - base.mean
    se = math.sqrt(arm.var / max(1, arm.n) + base.var / max(1, base.n))
    if se == 0:
        return 0.0 if diff == 0 else math.copysign(math.inf, diff)
    return diff / se


def paired_diffs(arm_rows: list[dict], base_rows: list[dict], key=lambda r: r["score"]) -> list[float]:
    """Per-pair score differences (arm - base); a pair is the k-th replicate of one experimental cell -
    (enemy faction, map, spawn side) - in each arm."""
    def by_cell(rows: list[dict]) -> dict[tuple, list[float]]:
        cells: dict[tuple, list[float]] = {}
        for r in rows:
            cells.setdefault((r.get("enemy_faction", ""), r.get("map", ""), r.get("spawn", -1)), []).append(key(r))
        return cells

    a, b = by_cell(arm_rows), by_cell(base_rows)
    return [x - y for cell in sorted(a.keys() & b.keys(), key=repr) for x, y in zip(a[cell], b[cell])]


def _diffs_z(diffs: list[float], min_pairs: int) -> tuple[float, int] | None:
    if len(diffs) < max(2, min_pairs):
        return None
    n = len(diffs)
    mean_d = sum(diffs) / n
    sd = math.sqrt(sum((d - mean_d) ** 2 for d in diffs) / (n - 1))
    if sd == 0:
        return (0.0 if mean_d == 0 else math.copysign(math.inf, mean_d)), n
    return mean_d / (sd / math.sqrt(n)), n


def paired_z(arm_rows: list[dict], base_rows: list[dict], min_pairs: int) -> tuple[float, int] | None:
    """Paired z of (arm - base) on score, or None when fewer than `min_pairs` matches can be paired one-to-one.

    The harness records no per-match seed, so replicates of a cell are exchangeable; pairing removes the cell effect (a
    spawn-side or matchup advantage) that the unpaired Welch test leaves in its variance. Returns (z, n_pairs).
    """
    return _diffs_z(paired_diffs(arm_rows, base_rows), min_pairs)


def holm_critical_z(m: int, z_crit: float = Z_CRIT) -> list[float]:
    """Critical z per rank (best z first) for m comparisons, Holm-Bonferroni at the one-sided alpha 1 - Phi(z_crit).

    m == 1 is exactly `z_crit`. Rank k (0-based) uses alpha / (m - k); reject down the ranking until one fails.
    """
    nd = statistics.NormalDist()
    alpha = 1 - nd.cdf(z_crit)
    return [z_crit if m - k == 1 else nd.inv_cdf(1 - alpha / (m - k)) for k in range(m)]


def arm_stats(rows: list[dict]) -> dict[tuple[str, str, str], Stats]:
    """(personality, faction, arm) -> score stats."""
    out: dict[tuple[str, str, str], Stats] = {}
    for r in rows:
        out.setdefault((r["personality"], r["faction"], r["arm"]), Stats()).add(r["score"])
    return out


# ---------------------------------------------------------------- the learned file

def empty_learned() -> dict:
    return {"knobs": {}, "openings": {}, "processed_knobs": [], "processed_openings": [], "spsa_steps": {}}


def parse_learned(text: str) -> dict:
    """Read the file this script writes (tab-indented MiniYaml, fixed shape); unknown nodes are ignored."""
    learned = empty_learned()
    scope: tuple | None = None
    kind = ""
    in_root = False
    for raw in text.splitlines():
        line = raw.rstrip("\r")
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        depth = len(line) - len(line.lstrip("\t"))
        key, _, value = line.strip().partition(":")
        value = value.strip()
        if depth == 0:
            in_root = key == "BotBuildOrderKnobs"
            scope = None
        elif in_root and depth == 1:
            scope, kind = None, ""
            if key.startswith("Knobs@"):
                personality, _, sc = key[len("Knobs@"):].partition("__")
                scope, kind = (personality, sc), "knobs"
                learned["knobs"].setdefault(scope, {})
            elif key.startswith("Openings@"):
                left, _, enemy = key[len("Openings@"):].partition("__vs__")
                personality, _, own = left.partition("__")
                scope, kind = (personality, own, enemy), "openings"
                learned["openings"].setdefault(scope, {})
            elif key == "ProcessedKnobs":
                learned["processed_knobs"] = [v for v in value.split(",") if v]
            elif key == "ProcessedOpenings":
                learned["processed_openings"] = [v for v in value.split(",") if v]
            elif key == "SpsaSteps":
                kind = "spsa_steps"
        elif in_root and depth == 2:
            if kind == "spsa_steps":
                if key.startswith("Step@"):
                    personality, _, sc = key[len("Step@"):].partition("__")
                    learned["spsa_steps"][(personality, sc)] = int(value)
            elif scope is not None:
                if kind == "knobs":
                    learned["knobs"][scope][key] = int(value)
                else:
                    alpha, beta = value.split()
                    learned["openings"][scope][key] = [int(alpha), int(beta)]
    return learned


def format_learned(learned: dict) -> str:
    lines = [
        "# GENERATED by tools/ai/tune_build_order.py --write - do not edit by hand; regenerate and review.",
        "# Offline build-order knobs (AI_ARCHITECTURE.md 12.25, DESIGN.md 19.2): read at match start, frozen in release.",
        "#   Knobs@<personality>__<scope>: knob: thousandths      scope = <own faction> | family_<family> | any",
        "#   Openings@<personality>__<own faction>__vs__<enemy faction>: <opening>: <alpha> <beta>",
        "BotBuildOrderKnobs:",
    ]
    for (personality, scope), values in sorted(learned["knobs"].items()):
        if not values:
            continue
        lines.append(f"\tKnobs@{personality}__{scope}:")
        for knob in KNOBS:
            if knob in values:
                lines.append(f"\t\t{knob}: {values[knob]}")
    for (personality, own, enemy), values in sorted(learned["openings"].items()):
        if not values:
            continue
        lines.append(f"\tOpenings@{personality}__{own}__vs__{enemy}:")
        for opening, (alpha, beta) in sorted(values.items()):
            lines.append(f"\t\t{opening}: {alpha} {beta}")
    if learned["processed_knobs"]:
        lines.append("\tProcessedKnobs: " + ",".join(sorted(set(learned["processed_knobs"]))))
    if learned["processed_openings"]:
        lines.append("\tProcessedOpenings: " + ",".join(sorted(set(learned["processed_openings"]))))
    spsa_steps = learned.get("spsa_steps") or {}
    if spsa_steps:
        lines.append("\tSpsaSteps:")
        for (personality, scope), step in sorted(spsa_steps.items()):
            lines.append(f"\t\tStep@{personality}__{scope}: {step}")
    return "\n".join(lines) + "\n"


def game_key(row: dict) -> str:
    """A short stable id of one bot's game (the file would otherwise list full game uids)."""
    return hashlib.sha1(f"{row['game_uid']}|{row['personality']}|{row['faction']}".encode()).hexdigest()[:10]


# ---------------------------------------------------------------- tuning

def clamp(value: int, lo: int, hi: int) -> int:
    return max(lo, min(hi, value))


def current_multiplier(learned: dict, personality: str, faction: str, knob: str) -> int:
    """The learned multiplier the game would apply: faction -> family_<family> -> any scopes of the same personality; missing = 1000."""
    for scope in (faction, "family_" + faction.partition("_")[0], "any"):
        v = learned["knobs"].get((personality, scope), {}).get(knob)
        if v is not None:
            return v
    return 1000


def decisions(rows: list[dict], learned: dict, min_matches: int = MIN_MATCHES, z_crit: float = Z_CRIT) -> list[dict]:
    """For every (personality, faction): each perturbed arm vs its base arm, with the accept verdict.

    Paired z where the matches pair up by cell, Welch otherwise (`test`); the arms of one scope share the base, so their
    significance is Holm-Bonferroni corrected over the arms that have enough matches (`z_crit_used` is the threshold applied).
    """
    stats = arm_stats(rows)
    by_arm: dict[tuple[str, str, str], list[dict]] = {}
    for r in rows:
        by_arm.setdefault((r["personality"], r["faction"], r["arm"]), []).append(r)
    out = []
    for (personality, faction, arm), s in sorted(stats.items()):
        m = ARM_RE.match(arm)
        if not m or not m.group("knob") or (m.group("personality"), m.group("faction")) != (personality, faction):
            continue
        base_arm = f"bo__{personality}__{faction}__base"
        base = stats.get((personality, faction, base_arm))
        if base is None:
            continue
        knob, direction, delta = m.group("knob"), m.group("dir"), int(m.group("delta"))
        if knob not in KNOBS:
            continue
        enough = s.n >= min_matches and base.n >= min_matches
        z, test, n_pairs = welch_z(s, base), "welch", 0
        paired = paired_z(by_arm[(personality, faction, arm)], by_arm[(personality, faction, base_arm)], min_matches) if enough else None
        if paired is not None:
            z, n_pairs = paired
            test = "paired"
        old = current_multiplier(learned, personality, faction, knob)
        step = delta if direction == "up" else -delta
        new = clamp(old * (1000 + step) // 1000, LEARN_MIN, LEARN_MAX)
        out.append({
            "personality": personality, "faction": faction, "arm": arm, "knob": knob, "direction": direction, "delta": delta,
            "n_arm": s.n, "n_base": base.n, "mean_arm": s.mean, "mean_base": base.mean, "z": z, "test": test, "n_pairs": n_pairs,
            "enough": enough, "significant": False, "z_crit_used": z_crit, "old": old, "new": new,
        })

    scopes: dict[tuple[str, str], list[dict]] = {}
    for d in out:
        if d["enough"]:
            scopes.setdefault((d["personality"], d["faction"]), []).append(d)
    for family in scopes.values():
        family.sort(key=lambda d: -d["z"])
        for d, crit in zip(family, holm_critical_z(len(family), z_crit)):
            d["z_crit_used"] = crit
        for d in family:  # Holm step-down: stop at the first failure
            if d["z"] < d["z_crit_used"]:
                break
            d["significant"] = True
    return out


def accepted(decs: list[dict]) -> dict[tuple[str, str], dict]:
    """The single best significant, actually-changing arm per (personality, faction)."""
    best: dict[tuple[str, str], dict] = {}
    for d in decs:
        if not d["significant"] or d["new"] == d["old"]:
            continue
        key = (d["personality"], d["faction"])
        if key not in best or d["z"] > best[key]["z"]:
            best[key] = d
    return best


def is_perturbed_arm(arm: str) -> bool:
    """True for a knob-experiment arm (`bo__<p>__<f>__<knob>__up/dn<delta>`) or an SPSA arm; base/plain batches are controls."""
    m = ARM_RE.match(arm)
    return bool(m and m.group("knob")) or spsa_arm(arm) is not None


def update_openings(learned: dict, rows: list[dict], min_matches: int = MIN_MATCHES) -> list[tuple]:
    """Fold unprocessed matches into the opening posteriors, one matchup at a time. Returns the matchups updated.

    Only matches at the current-best knobs (the base / control arm) count: a perturbed-arm match mixes the opening's effect with a
    deliberate knob change. Those are recorded as processed (skipped) so they are never reconsidered.
    """
    done = set(learned["processed_openings"])
    by_matchup: dict[tuple[str, str, str], list[dict]] = {}
    for r in rows:
        if not r["opening"] or game_key(r) in done:
            continue
        if is_perturbed_arm(r["arm"]):
            learned["processed_openings"].append(game_key(r))
            done.add(game_key(r))
            continue
        by_matchup.setdefault((r["personality"], r["faction"], r["enemy_faction"]), []).append(r)

    updated = []
    for matchup, group in sorted(by_matchup.items()):
        if len(group) < min_matches:
            continue
        posteriors = learned["openings"].setdefault(matchup, {})
        for r in group:
            alpha, beta = posteriors.get(r["opening"], [1, 1])
            alpha, beta = alpha + int(r["win"]), beta + int(1 - r["win"])
            if alpha + beta > POSTERIOR_CAP:
                alpha, beta = max(1, alpha // 2), max(1, beta // 2)
            posteriors[r["opening"]] = [alpha, beta]
            learned["processed_openings"].append(game_key(r))
        updated.append(matchup)
    return updated


def write_update(rows: list[dict], learned: dict, min_matches: int = MIN_MATCHES, z_crit: float = Z_CRIT) -> dict:
    """Apply the bounded, significance-checked update to `learned` in place; returns what changed."""
    done = set(learned["processed_knobs"])
    fresh = [r for r in rows if game_key(r) not in done]
    decs = decisions(fresh, learned, min_matches, z_crit)
    changes = {"knobs": [], "openings": []}
    for (personality, faction), d in sorted(accepted(decs).items()):
        learned["knobs"].setdefault((personality, faction), {})[d["knob"]] = d["new"]
        changes["knobs"].append(d)
    # Every game of a (personality, faction) that reached a verdict is consumed: its arms were measured against each other.
    # SPSA-arm games are excluded: only a --spsa write can consume them (their verdict lives in write_spsa_update).
    decided = {(d["personality"], d["faction"]) for d in decs if d["enough"]}
    for r in fresh:
        if (r["personality"], r["faction"]) in decided and spsa_arm(r["arm"]) is None:
            learned["processed_knobs"].append(game_key(r))
    changes["openings"] = update_openings(learned, rows, min_matches)
    return changes


def next_experiment(rows: list[dict], learned: dict, personality: str, faction: str, min_matches: int = MIN_MATCHES) -> dict | None:
    """The first (knob, direction) in fixed order whose arm or base is under-sampled; None when every pair is measured."""
    stats = arm_stats(rows)
    base = stats.get((personality, faction, f"bo__{personality}__{faction}__base"))
    for knob in TUNABLE:
        for direction in ("up", "dn"):
            arm = f"bo__{personality}__{faction}__{knob}__{direction}{DELTA}"
            s = stats.get((personality, faction, arm))
            if s is None or s.n < min_matches or base is None or base.n < min_matches:
                old = current_multiplier(learned, personality, faction, knob)
                new = clamp(old * (1000 + (DELTA if direction == "up" else -DELTA)) // 1000, LEARN_MIN, LEARN_MAX)
                if new == old:
                    continue  # at the bound: nothing to test in that direction
                return {"arm": arm, "base_arm": f"bo__{personality}__{faction}__base", "knob": knob, "direction": direction,
                        "old": old, "new": new, "n_arm": s.n if s else 0, "n_base": base.n if base else 0}
    return None


def experiment_files(learned: dict, personality: str, faction: str, exp: dict) -> dict[str, str]:
    """Learned-file text per arm (base = the current file, arm = the one knob moved) at the arm's name."""
    perturbed = {"knobs": {k: dict(v) for k, v in learned["knobs"].items()}, "openings": learned["openings"],
                 "processed_knobs": [], "processed_openings": []}
    perturbed["knobs"].setdefault((personality, faction), {})[exp["knob"]] = exp["new"]
    base = {"knobs": learned["knobs"], "openings": learned["openings"], "processed_knobs": [], "processed_openings": []}
    return {exp["base_arm"]: format_learned(base), exp["arm"]: format_learned(perturbed)}


def switches_text(arms: list[str]) -> str:
    """An increment_switches-style spec: one group per arm, arming the learned file of that arm."""
    lines = ["# GENERATED by tools/ai/tune_build_order.py --propose: one group per experiment arm.",
             "# Arm with: python tools/ai/apply_increment_switches.py <worktree> --spec <this file> --groups AK_build_order_knobs_arm",
             "# (AK_build_order_knobs from the main spec arms the provider; each arm adds its learned file). Copy <arm>.yaml to",
             "# <worktree>/mods/cameo/ai/learned/ first.",
             "skip: [SquadManagerBotModuleCA@classic]", "", "groups:"]
    for arm in arms:
        lines += [f"  {arm}:", "    BuildOrderKnobsBotModule:", "      UseLearnedBuildOrder: true",
                  f"      LearnedFile: ai/learned/{arm}.yaml"]
    return "\n".join(lines) + "\n"


# ---------------------------------------------------------------- tier 4: SPSA (--spsa; docs/design/TIER4_SPSA_SPEC.md)

def spsa_arm(arm: str) -> tuple[str, str, int, str] | None:
    """Parse `bo__<p>__<f>__spsa__k<step>__plus|minus` -> (personality, faction, step, side); None otherwise."""
    parts = arm.split("__")
    if (len(parts) == 6 and parts[0] == "bo" and parts[3] == "spsa" and len(parts[4]) > 1
            and parts[4][0] == "k" and parts[4][1:].isdigit() and parts[5] in ("plus", "minus")):
        return parts[1], parts[2], int(parts[4][1:]), parts[5]
    return None


def spsa_step(learned: dict, personality: str, faction: str) -> int:
    return learned.get("spsa_steps", {}).get((personality, faction), 0)


def spsa_delta(personality: str, faction: str, step: int) -> list[int]:
    """Deterministic +-1 Rademacher vector over TUNABLE for (personality, faction, step): the same step
    regenerates the same proposal on any machine (spec section 3.1)."""
    digest = hashlib.sha256("\0".join(["spsa", personality, faction, str(step)]).encode()).digest()
    return [1 if digest[i] & 1 else -1 for i in range(len(TUNABLE))]


def spsa_perturb(step: int) -> float:  # c_k = c / (k + 1)^0.101
    return SPSA_GAIN_C / (step + 1) ** 0.101


def spsa_gain(step: int) -> float:  # a_k = a / (k + 1 + A)^0.602
    return SPSA_GAIN_A / (step + 1 + SPSA_STAB) ** 0.602


def spsa_arm_name(personality: str, faction: str, step: int, side: str) -> str:
    return f"bo__{personality}__{faction}__spsa__k{step}__{side}"


def spsa_arm_values(learned: dict, personality: str, faction: str, step: int) -> dict[str, dict[str, int]]:
    """{knob: {"plus": milli, "minus": milli}} - the clamped values each arm plays this step (spec 3.2)."""
    c = spsa_perturb(step)
    values = {}
    for knob, d in zip(TUNABLE, spsa_delta(personality, faction, step)):
        lo = math.log(current_multiplier(learned, personality, faction, knob) / 1000.0)
        values[knob] = {"plus": int(clamp(round(1000 * math.exp(lo + c * d)), LEARN_MIN, LEARN_MAX)),
                        "minus": int(clamp(round(1000 * math.exp(lo - c * d)), LEARN_MIN, LEARN_MAX))}
    return values


def next_spsa_experiment(rows: list[dict], learned: dict, personality: str, faction: str,
                         min_matches: int = MIN_MATCHES) -> dict | None:
    """The current step's plus/minus pair while either side is under-sampled; None once measured (a measured
    step waits for --write: re-proposing it would resend identical arms, and k+1 only exists after the write)."""
    step = spsa_step(learned, personality, faction)
    stats = arm_stats(rows)
    counts = {s: stats.get((personality, faction, spsa_arm_name(personality, faction, step, s)), Stats()).n
              for s in ("plus", "minus")}
    if counts["plus"] >= min_matches and counts["minus"] >= min_matches:
        return None
    return {"step": step, "c": spsa_perturb(step),
            "delta": dict(zip(TUNABLE, spsa_delta(personality, faction, step))),
            "values": spsa_arm_values(learned, personality, faction, step),
            "plus_arm": spsa_arm_name(personality, faction, step, "plus"),
            "minus_arm": spsa_arm_name(personality, faction, step, "minus"),
            "n_plus": counts["plus"], "n_minus": counts["minus"]}


def spsa_experiment_files(learned: dict, personality: str, faction: str, exp: dict) -> dict[str, str]:
    """One learned-file copy per arm with the whole perturbed vector stamped; a comment records the step (spec 6)."""
    files = {}
    tag = ",".join(f"{k}{'+' if d > 0 else '-'}" for k, d in exp["delta"].items())
    for side in ("plus", "minus"):
        perturbed = {"knobs": {k: dict(v) for k, v in learned["knobs"].items()}, "openings": learned["openings"],
                     "processed_knobs": [], "processed_openings": []}
        perturbed["knobs"].setdefault((personality, faction), {}).update(
            {knob: exp["values"][knob][side] for knob in TUNABLE})
        other = exp["minus_arm" if side == "plus" else "plus_arm"]
        files[exp[f"{side}_arm"]] = (f"# spsa step {exp['step']} {side} (c={exp['c']:.4f}, delta={tag}), paired with {other}\n"
                                    + format_learned(perturbed))
    return files


def composite_score(row: dict) -> float:
    """What SPSA optimizes: match score + EL_WEIGHT x normalised mean engagement verdict (spec 2, ruling R3)."""
    return row["score"] + EL_WEIGHT * row.get("el_mean_milli", 0.0) / 1000.0


def _score_stats(rows: list[dict], key) -> Stats:
    s = Stats()
    for r in rows:
        s.add(key(r))
    return s


def spsa_decisions(rows: list[dict], learned: dict, min_matches: int = MIN_MATCHES) -> list[dict]:
    """One decision per SPSA step pair: plus-vs-minus paired z on the composite objective (spec 4-5).

    `d_bar` is the mean paired (or unpaired-fallback) difference plus - minus; its sign IS the gradient
    direction (ruling R1). Steps are evaluated lowest-first and an under-sampled step stops the scope - a
    later step was measured at a later point and must not leapfrog."""
    groups: dict[tuple[str, str], dict[int, dict[str, list[dict]]]] = {}
    for r in rows:
        parsed = spsa_arm(r["arm"])
        if parsed is not None:
            p, f, step, side = parsed
            groups.setdefault((p, f), {}).setdefault(step, {}).setdefault(side, []).append(r)
    decs = []
    for (p, f), by_step in sorted(groups.items()):
        for step in sorted(by_step):
            sides = by_step[step]
            plus, minus = sides.get("plus", []), sides.get("minus", [])
            d = {"kind": "spsa", "personality": p, "faction": f, "step": step,
                 "plus_arm": spsa_arm_name(p, f, step, "plus"), "minus_arm": spsa_arm_name(p, f, step, "minus"),
                 "n_plus": len(plus), "n_minus": len(minus), "plus_rows": plus, "minus_rows": minus,
                 "enough": len(plus) >= min_matches and len(minus) >= min_matches,
                 "z": 0.0, "test": "none", "n_pairs": 0, "d_bar": 0.0, "z_crit_used": Z_CRIT, "step_scale": 0.0}
            decs.append(d)
            if not d["enough"]:
                break
            diffs = paired_diffs(plus, minus, composite_score)
            paired = _diffs_z(diffs, min_matches)
            if paired is not None:
                d["z"], d["n_pairs"] = paired
                d["test"] = "paired"
                d["d_bar"] = sum(diffs) / len(diffs)
            else:
                ps, ms = _score_stats(plus, composite_score), _score_stats(minus, composite_score)
                d["z"], d["test"], d["n_pairs"] = welch_z(ps, ms), "welch", len(diffs)
                d["d_bar"] = ps.mean - ms.mean
    return decs


def combined_verdicts(coord_decs: list[dict], spsa_decs: list[dict], z_crit: float = Z_CRIT) -> None:
    """Re-rank each scope's hypotheses under the widened Holm family (spec 5: coordinate arms and SPSA pairs
    share the base, so the family is all of them). Sorts by each hypothesis's own statistic - signed z for
    coordinate arms, |z| for SPSA pairs - and marks coordinate `significant` / SPSA `step_scale` in place."""
    for d in coord_decs:
        d["significant"] = False
    for d in spsa_decs:
        if d["enough"]:
            d["step_scale"] = SPSA_SCALE_WEAK  # a measured pair always updates - full or damped, never zero (R1)
    families: dict[tuple[str, str], list[dict]] = {}
    for d in coord_decs + spsa_decs:
        if d["enough"]:
            families.setdefault((d["personality"], d["faction"]), []).append(d)
    for family in families.values():
        family.sort(key=lambda d: -(abs(d["z"]) if d.get("kind") == "spsa" else d["z"]))
        for d, crit in zip(family, holm_critical_z(len(family), z_crit)):
            d["z_crit_used"] = crit
        for d in family:  # Holm step-down: stop at the first failure
            stat = abs(d["z"]) if d.get("kind") == "spsa" else d["z"]
            if stat < d["z_crit_used"]:
                break
            if d.get("kind") == "spsa":
                d["step_scale"] = SPSA_SCALE_FULL
            else:
                d["significant"] = True


def apply_spsa_step(d: dict, learned: dict) -> dict:
    """One SPSA update at the current learned vector: g_i = d_bar / (2 x eff_i) per unmasked knob, in log space.

    eff_i is the perturbation the games actually played (half the SIGNED plus/minus log gap of the REPORTED
    knob values), so clamping at the bounds shrinks it honestly instead of pretending the full c_k landed.
    The sign matters: eff_i has the sign of Delta_i, so g_i = d_bar / (2 x eff_i) points toward the winning
    arm per knob - an unsigned eff would march every component in the direction of d_bar."""
    personality, faction, step = d["personality"], d["faction"], d["step"]
    a_k = spsa_gain(step)
    moves, masked = [], []
    for knob in TUNABLE:
        lp = [math.log(r["knobs"].get(knob, 1000)) for r in d["plus_rows"]]
        lm = [math.log(r["knobs"].get(knob, 1000)) for r in d["minus_rows"]]
        eff = (sum(lp) / len(lp) - sum(lm) / len(lm)) / 2.0
        if abs(eff) < SPSA_MASK_LOG:
            masked.append(knob)
            continue
        g = d["d_bar"] / (2.0 * eff)
        old = current_multiplier(learned, personality, faction, knob)
        new = int(clamp(round(1000 * math.exp(math.log(old / 1000.0) + d["step_scale"] * a_k * g)), LEARN_MIN, LEARN_MAX))
        if new != old:
            learned["knobs"].setdefault((personality, faction), {})[knob] = new
            moves.append({"knob": knob, "old": old, "new": new, "g": g})
    return {"moves": moves, "masked": masked, "a_k": a_k, "d_bar": d["d_bar"], "step_scale": d["step_scale"]}


def write_spsa_update(rows: list[dict], learned: dict, min_matches: int = MIN_MATCHES, z_crit: float = Z_CRIT) -> dict:
    """--write in SPSA mode: coordinate and SPSA decisions under the widened Holm family, then the updates."""
    done = set(learned["processed_knobs"])
    fresh = [r for r in rows if game_key(r) not in done]
    coord_decs = decisions(fresh, learned, min_matches, z_crit)
    spsa_decs = spsa_decisions(fresh, learned, min_matches)
    combined_verdicts(coord_decs, spsa_decs, z_crit)
    changes = {"knobs": [], "openings": [], "spsa": []}
    for (personality, faction), d in sorted(accepted(coord_decs).items()):
        learned["knobs"].setdefault((personality, faction), {})[d["knob"]] = d["new"]
        changes["knobs"].append(d)
    decided = {(d["personality"], d["faction"]) for d in coord_decs if d["enough"]}
    for r in fresh:
        if (r["personality"], r["faction"]) in decided and spsa_arm(r["arm"]) is None:
            learned["processed_knobs"].append(game_key(r))
    learned.setdefault("spsa_steps", {})
    for d in spsa_decs:
        if not d["enough"]:
            continue  # its games stay unprocessed: the pair is re-proposed until it reaches MIN_MATCHES
        changes["spsa"].append({**{k: d[k] for k in ("personality", "faction", "step", "z", "test", "n_pairs",
                                                     "d_bar", "z_crit_used", "step_scale")},
                                **apply_spsa_step(d, learned)})
        learned["spsa_steps"][(d["personality"], d["faction"])] = d["step"] + 1
        for r in d["plus_rows"] + d["minus_rows"]:
            learned["processed_knobs"].append(game_key(r))
    changes["openings"] = update_openings(learned, rows, min_matches)
    return changes


def spsa_report(rows: list[dict], learned: dict, min_matches: int = MIN_MATCHES, z_crit: float = Z_CRIT) -> str:
    """The SPSA decisions block, shown only in --spsa mode."""
    decs = spsa_decisions(rows, learned, min_matches)
    combined_verdicts(decisions(rows, learned, min_matches, z_crit), decs, z_crit)
    out = [f"\n## SPSA steps (two-sided |z| >= crit -> full step x{SPSA_SCALE_FULL}, else damped x{SPSA_SCALE_WEAK})"]
    for d in decs:
        if not d["enough"]:
            out.append(f"- {d['personality']} {d['faction']} k{d['step']}: plus n={d['n_plus']} minus n={d['n_minus']} -> under-sampled")
            continue
        band = "full" if d["step_scale"] == SPSA_SCALE_FULL else "damped"
        out.append(f"- {d['personality']} {d['faction']} k{d['step']}: plus n={d['n_plus']} minus n={d['n_minus']} "
                   f"z={d['z']:.2f} ({d['test']}) d_bar={d['d_bar']:.3f} -> {band} step (|z| vs {d['z_crit_used']:.2f})")
    if not decs:
        out.append("- none: no arm named bo__<personality>__<faction>__spsa__k<step>__plus|minus in these directories")
    return "\n".join(out)


# ---------------------------------------------------------------- reporting

def report(rows: list[dict], learned: dict, min_matches: int = MIN_MATCHES) -> str:
    out = [f"{len(rows)} scored matches (score = win + margin + speed; speed weight {SPEED_WEIGHT}, ref {SPEED_REF_TICKS} ticks)"]
    stats = arm_stats(rows)
    wins: dict[tuple[str, str, str], int] = {}
    for r in rows:
        wins[(r["personality"], r["faction"], r["arm"])] = wins.get((r["personality"], r["faction"], r["arm"]), 0) + int(r["win"])
    out.append("\n## Arms (personality, faction, arm): n, wins, mean score, mean base knob vector")
    for key, s in sorted(stats.items()):
        group = [r for r in rows if (r["personality"], r["faction"], r["arm"]) == key]
        vec = " ".join(f"{k}={round(sum(r['knobs'][k] for r in group) / len(group))}" for k in KNOBS)
        out.append(f"- {key[0]} {key[1]} {key[2]}: n={s.n} wins={wins[key]} mean={s.mean:.3f}  [{vec}]")
    out.append("\n## Openings (personality, faction vs enemy faction, opening): n, wins")
    per: dict[tuple, list[int]] = {}
    for r in rows:
        p = per.setdefault((r["personality"], r["faction"], r["enemy_faction"], r["opening"] or "none"), [0, 0])
        p[0] += 1
        p[1] += int(r["win"])
    for key, (n, w) in sorted(per.items()):
        out.append(f"- {key[0]} {key[1]} vs {key[2]} {key[3]}: n={n} wins={w}")
    out.append(f"\n## Knob experiments (need >= {min_matches} matches per arm and z >= {Z_CRIT})")
    decs = decisions(rows, learned, min_matches)
    for d in decs:
        verdict = "ACCEPT" if d["significant"] and d["new"] != d["old"] else ("under-sampled" if not d["enough"] else "reject")
        out.append(f"- {d['personality']} {d['faction']} {d['knob']} {d['direction']}{d['delta']}: arm n={d['n_arm']} mean={d['mean_arm']:.3f} "
                   f"vs base n={d['n_base']} mean={d['mean_base']:.3f} z={d['z']:.2f} ({d['test']}) -> {verdict} ({d['old']} -> {d['new']})")
    if not decs:
        out.append("- none: no arm named bo__<personality>__<faction>__<knob>__up|dn<delta> with a base arm in these directories")
    return "\n".join(out)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("batch_dirs", nargs="*", type=pathlib.Path)
    ap.add_argument("--learned", type=pathlib.Path, default=LEARNED_PATH, help="the learned file to read (and write with --write)")
    ap.add_argument("--write", action="store_true", help="apply the bounded, significance-checked update to the learned file")
    ap.add_argument("--propose", type=pathlib.Path, metavar="OUT_DIR", help="write the next paired experiment per --pair to OUT_DIR")
    ap.add_argument("--pair", action="append", default=[], metavar="PERSONALITY:FACTION", help="a (personality, faction) to propose for; repeatable")
    ap.add_argument("--min-matches", type=int, default=MIN_MATCHES)
    ap.add_argument("--z", type=float, default=Z_CRIT)
    ap.add_argument("--spsa", action="store_true",
                    help="tier-4 mode: simultaneous-perturbation pairs (bo__<p>__<f>__spsa__k<step>__plus|minus) "
                         "instead of coordinate descent; coordinate mode stays the default")
    ap.add_argument("--coverage", action="store_true",
                    help="report only: per-(personality, faction, side) funnel of match records seen, scored, "
                         "and drops by reason vs --min-matches, plus engagement-verdict (EL) coverage")
    ap.add_argument("--json", action="store_true", help="with --coverage: emit JSON instead of the text table")
    args = ap.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")

    if args.coverage:  # a report: never writes, never fails
        _, cov = load_matches_detailed([d for d in args.batch_dirs if d.is_dir()])
        print(json.dumps(coverage_json(cov, args.min_matches), indent=1, sort_keys=True)
              if args.json else coverage_report(cov, args.min_matches))
        return 0

    learned = parse_learned(args.learned.read_text(encoding="utf-8")) if args.learned.exists() else empty_learned()
    rows = load_matches([d for d in args.batch_dirs if d.is_dir()])
    print(report(rows, learned, args.min_matches))
    if args.spsa:
        print(spsa_report(rows, learned, args.min_matches, args.z))

    if args.propose:
        args.propose.mkdir(parents=True, exist_ok=True)
        arms: list[str] = []
        for pair in args.pair:
            personality, _, faction = pair.partition(":")
            if args.spsa:
                exp = next_spsa_experiment(rows, learned, personality, faction, args.min_matches)
                if exp is None:
                    print(f"\npropose {pair}: step {spsa_step(learned, personality, faction)} already measured; run --spsa --write")
                    continue
                for arm, text in spsa_experiment_files(learned, personality, faction, exp).items():
                    (args.propose / f"{arm}.yaml").write_text(text, encoding="utf-8", newline="\n")
                    arms.append(arm)
                print(f"\npropose {pair}: spsa step {exp['step']} (c={exp['c']:.4f}); "
                      f"arms {exp['plus_arm']}, {exp['minus_arm']}")
            else:
                exp = next_experiment(rows, learned, personality, faction, args.min_matches)
                if exp is None:
                    print(f"\npropose {pair}: every pair is measured; run --write")
                    continue
                for arm, text in experiment_files(learned, personality, faction, exp).items():
                    (args.propose / f"{arm}.yaml").write_text(text, encoding="utf-8", newline="\n")
                    arms.append(arm)
                print(f"\npropose {pair}: {exp['knob']} {exp['direction']}{DELTA} ({exp['old']} -> {exp['new']}) vs base; arms {exp['base_arm']}, {exp['arm']}")
        (args.propose / "experiment_switches.yaml").write_text(switches_text(arms), encoding="utf-8", newline="\n")
        print(f"\nwrote {args.propose / 'experiment_switches.yaml'}")

    if args.write:
        changes = (write_spsa_update(rows, learned, args.min_matches, args.z) if args.spsa
                   else write_update(rows, learned, args.min_matches, args.z))
        args.learned.parent.mkdir(parents=True, exist_ok=True)
        args.learned.write_text(format_learned(learned), encoding="utf-8", newline="\n")
        print(f"\nwrote {args.learned}: {len(changes['knobs'])} knob change(s), {len(changes['openings'])} matchup posterior update(s)")
        for d in changes["knobs"]:
            print(f"- {d['personality']} {d['faction']} {d['knob']}: {d['old']} -> {d['new']} (z={d['z']:.2f}, n={d['n_arm']}/{d['n_base']})")
        for d in changes.get("spsa", []):
            moved = ", ".join(f"{m['knob']} {m['old']}->{m['new']}" for m in d["moves"]) or "no knob moved"
            print(f"- {d['personality']} {d['faction']} spsa k{d['step']}: z={d['z']:.2f} d_bar={d['d_bar']:.3f} "
                  f"scale={d['step_scale']} -> {moved} (masked: {','.join(d['masked']) or 'none'})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
