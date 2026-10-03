#!/usr/bin/env python3
"""fit_engagement_priors.py - the tier-1 'measured from logs' fitter (DESIGN 19.13 tier 1,
TIER1_FITTER_SPEC; AI_ARCHITECTURE 12.30/12.34).

Reads engagement/1 records (Logs/cameo-ai-engagements.jsonl) plus the balance ledger
(docs/balance/*.json raw stat ledgers) and fits ~1000 stat-normalised coefficients:

1. Delivery x armour strength residuals: the resolved ^Warhead_* Versus table is the pipeline
   prior; the fit measures how much better or worse each delivery family actually trades against
   each armour class than the Lanchester predictor expected. Cells = damage_warheads[].tag
   (e.g. Bullet_Medium) x resolved Armor.Type - never per-unit ids.
2. Static-defence state strengths: the same residual pooled over cells where the attacker was a
   static defence (defence fire effectiveness), plus a global attacking-into-defences correction.
3. One attrition exponent on the predicted ratio (square law = 1000).
4. Timing and response priors: per-enemy-faction attack-timing quantiles (from our defend
   records - a defend for us is their attack), own response-time and army-distance quantiles,
   per-faction-pair suicide-index medians. Faction-keyed tables pool global -> family -> faction;
   'enemy_faction_public: false' (a Random lobby slot) pools to family/global only (ruling 2).

Fitting is closed-form expected-attribution with pseudo-evidence shrinkage (the same pattern as
fit_arsenal_priors.py's shrunk_ratio): residual[d][a] = (obs + K) / (exp + K), K = SHRINK_VALUE of
pseudo-damage credit, clamped [MIN_MILLI, MAX_MILLI]. stdlib only, deterministic: same logs in,
same file out.

    python tools/ai/fit_engagement_priors.py <batch-dir> [...] [--write mods/cameo/ai/learned/engagement_priors.yaml]

Without --write it prints a report. The yaml is a committed, reviewed data file read frozen at
match start (DESIGN 19.2); no game code reads it until the phase-B consumer lands behind the
default-OFF `AP_tier1_priors` increment group.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import pathlib
import statistics
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "audit"))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "balance"))
import ai_log_common as c  # noqa: E402
from firepower import armament_firepower  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[2]
PHASE_TICKS = 6000
SHRINK_VALUE = 5000   # pseudo-evidence (damage credit) mixed into every cell's ratio
MIN_MILLI = 500       # EngagemantPriors bounds - never invert a fight
MAX_MILLI = 2000
MIN_PAIR_SAMPLES = 8  # faction-pair table needs this many rows before it stops inheriting


# ── type profiles from the raw stat ledgers (never hand-parsed yaml; rule 8e) ───────────────

def cycle_ticks(reload_delay: int, burst: int, burst_delays: list[int]) -> int:
    """Mirror of BotWeaponProfile.CycleTicks: burst-1 gaps, then the reload."""
    cycle = max(1, reload_delay)
    for shot in range(burst - 1):
        if not burst_delays:
            cycle += 0
        elif len(burst_delays) == 1:
            cycle += burst_delays[0]
        else:
            cycle += burst_delays[min(shot, len(burst_delays) - 1)]
    return cycle


def _int(value, default: int = 0) -> int:
    try:
        return int(str(value).split(",")[0])
    except (TypeError, ValueError):
        return default


def load_profiles(repo: pathlib.Path) -> tuple[dict, dict, str]:
    """name.lower() -> {cost, hp, armor, weapons: [(tag, dpt)]}; delivery tag -> Versus table;
    the ledger hash. Built from docs/balance/*.json raw ledgers only."""
    profiles: dict[str, dict] = {}
    collisions: list[str] = []
    ledger_files = []
    for path in sorted((repo / "docs" / "balance").glob("*.json")):
        try:
            doc = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            continue
        if not isinstance(doc, dict) or "ledger" not in doc or "sections" not in doc:
            continue
        ledger_files.append(path)
        for entries in doc["sections"].values():
            if not isinstance(entries, dict):
                continue
            for name, a in entries.items():
                if not isinstance(a, dict):
                    continue
                key = name.lower()
                if key in profiles:
                    collisions.append(key)
                    continue
                weapons = []
                for arm in a.get("armaments") or []:
                    whs = [w for w in (arm.get("damage_warheads") or []) if _int(w.get("damage")) > 0]
                    if not whs:
                        continue
                    main = max(whs, key=lambda w: _int(w.get("damage")))
                    delays = [_int(x) for x in str(arm.get("burstdelays") or "").split(",") if str(x).strip()]
                    burst = max(1, _int(arm.get("burst"), 1))
                    cycle = cycle_ticks(_int(arm.get("reloaddelay"), 1), burst, delays)
                    # dpt carries the actor's resolved unconditional firepower product
                    # (firepower.armament_firepower): a buffed unit's real output differs
                    # from its raw damage and must not leak into the delivery x armour cells.
                    dpt = _int(main.get("damage")) * burst / cycle * armament_firepower(a, arm)
                    weapons.append((main.get("tag") or "", dpt))
                profiles[key] = {
                    "cost": _int((a.get("cost") or {}).get("v")),
                    "hp": _int((a.get("hp") or {}).get("v")),
                    "armor": (a.get("armor") or {}).get("v") or "None",
                    "weapons": [w for w in weapons if w[0]],
                }
    digest = hashlib.sha256()
    for path in ledger_files:
        digest.update(path.name.encode())
        digest.update(path.read_bytes())
    return profiles, {"collisions": collisions, "ledger_files": len(ledger_files)}, digest.hexdigest()


def versus_priors(repo: pathlib.Path, tags: list[str]) -> tuple[dict[str, dict[str, int]], list[str]]:
    """delivery tag -> {armor -> percent}, resolved through miniyaml.Ruleset (rule 8e), plus the
    sorted tags whose table came out EMPTY. Empty-prior tags are excluded from the cell fit
    entirely - fitting them against the default-100 prior would fake residuals (lead review).
    Tags already carry the level (Bullet_Medium); unresolved tags get the family template."""
    from miniyaml import Ruleset
    from percentage_damage import versus_table

    rules = Ruleset(repo)
    priors: dict[str, dict[str, int]] = {}
    excluded: list[str] = []
    for tag in tags:
        table: dict[str, int] = {}
        for name in (f"^Warhead_{tag}", f"^Warhead_{tag.rsplit('_', 1)[0]}"):
            node = rules.resolve_weapon(name)
            if node is None:
                continue
            wh = node.child(f"Warhead@{name[len('^Warhead_'):]}")
            table = versus_table(wh) if wh is not None else {}
            if table:
                break
        if table:
            priors[tag] = table
        else:
            excluded.append(tag)
    return priors, sorted(excluded)


# ── the fit ────────────────────────────────────────────────────────────────────────────────

def armour_share(comp: dict[str, int], profiles: dict) -> dict[str, int]:
    """armour class -> committed value, from a type->count composition."""
    out: dict[str, int] = {}
    for t, n in comp.items():
        p = profiles.get(t.lower())
        if not p:
            continue
        out[p["armor"]] = out.get(p["armor"], 0) + p["cost"] * n
    return out


def family_of(faction: str) -> str:
    """Game family of a faction id (td_gdi -> td, ra1_soviets -> ra1, d2k_atreides -> d2k)."""
    return faction.split("_", 1)[0] if faction else ""


def quantiles(values: list[int], ps=(10, 50, 90)) -> list[int | None]:
    s = sorted(v for v in values if v is not None)
    if not s:
        return [None] * len(ps)
    return [s[min(len(s) - 1, max(0, round(p / 100 * (len(s) - 1))))] for p in ps]


def record_facts(r: dict, profiles: dict, matches_factions: dict) -> dict | None:
    """One engagement/1 record -> the derived facts a single accumulator pass needs, or None
    when the record cannot feed the grid. Purely read-side; the fitter's only write is below."""
    comp = ((r.get("seen") or {}).get("start") or {}).get("composition")
    if not comp:
        return None
    outcome = r.get("outcome") or {}
    start = (r.get("seen") or {}).get("start") or {}
    truth_comp = (((r.get("truth") or {}).get("start") or {}).get("composition")) or {}

    own_units, own_defs = comp.get("own_units") or {}, comp.get("own_defences") or {}
    enemy_units = truth_comp.get("units") or comp.get("enemy_units") or {}
    enemy_defs = truth_comp.get("defences") or comp.get("enemy_defences") or {}
    own_all, enemy_all = {**own_units, **own_defs}, {**enemy_units, **enemy_defs}
    if not own_all or not enemy_all:
        return None
    enemy_share = armour_share(enemy_all, profiles)
    own_share = armour_share(own_all, profiles)
    if not enemy_share or not own_share:
        return None

    enemy_value = max(1, start.get("enemy_unit_value", 0) + start.get("enemy_defence_value", 0))
    own_value = max(1, start.get("own_committed_value", 0) + start.get("own_defence_value", 0))

    # ruling 2: the OFFLINE fit uses the real faction; only in-match consumers honour
    # enemy_faction_public (a Random slot falls back to family/global pools).
    enemy_faction = r.get("enemy_faction") or ""
    if not enemy_faction:  # pre-field logs: the other player's faction in the same match
        for f in matches_factions.get(r.get("game_uid", ""), []):
            if f != r.get("faction"):
                enemy_faction = f

    own_pred = start.get("predicted_own_surviving_permille")
    return {
        # our fire into them (real victims when truth is present), then their fire into us;
        # (mobile units, static defences) stay disjoint so defence fire splits out cleanly
        "dirs": [
            (own_units, own_defs, enemy_share, enemy_value,
             outcome.get("enemy_killed_value", 0), start.get("predicted_enemy_surviving_permille", 0)),
            (enemy_units, enemy_defs, own_share, own_value,
             outcome.get("own_lost_value", 0), start.get("predicted_own_surviving_permille", 0)),
        ],
        "into_defences": ((r.get("tactics") or {}).get("into_defences_value") or 0) > 0,
        "ratio": ((own_pred / 1000.0, max(0.0, 1.0 - outcome.get("own_lost_value", 0) / own_value))
                  if own_pred is not None and 0 < own_pred < 1000 else None),
        "kind": r.get("kind"), "start_tick": r.get("start_tick", 0),
        "faction": r.get("faction", ""), "enemy_faction": enemy_faction,
        "response": (r.get("response") or {}), "suicide_milli": (r.get("tactics") or {}).get("suicide_index_milli"),
        "killed_total": outcome.get("enemy_killed_value", 0), "lost_total": outcome.get("own_lost_value", 0),
    }


def fit(data: dict, profiles: dict, priors: dict) -> dict:
    matches_factions: dict[str, list[str]] = {}
    for m in data.get("matches", []):
        pl = m.get("player") or {}
        if pl.get("faction"):
            matches_factions.setdefault(m.get("game_uid", ""), []).append(pl["faction"])

    skipped = {"skirmish": 0, "no_composition": 0, "unmapped": 0}
    facts = []
    for r in data.get("engagements", []):
        if r.get("record") != "engagement":
            continue
        if r.get("skirmish"):
            skipped["skirmish"] += 1
            continue
        f = record_facts(r, profiles, matches_factions)
        if f is None:
            key = "no_composition" if not ((r.get("seen") or {}).get("start") or {}).get("composition") else "unmapped"
            skipped[key] += 1
            continue
        facts.append(f)

    # bounded estimator (spec §4): one record's damage credit is capped at 4x the median
    # record so a single 50k-value bloodbath cannot dominate a cell.
    totals = sorted(f["killed_total"] + f["lost_total"] for f in facts)
    cap = 4 * totals[len(totals) // 2] if totals else 0

    obs: dict[tuple[str, str], float] = {}
    exp: dict[tuple[str, str], float] = {}
    ds_obs: dict[str, float] = {}
    ds_exp: dict[str, float] = {}
    into_obs = into_exp = 0.0
    ratios: list[tuple[float, float]] = []
    attack_ticks: dict[str, list[int]] = {}
    response: dict[str, list[int]] = {}
    army_dist: dict[str, list[int]] = {}
    suicide: dict[tuple[str, str], list[int]] = {}

    for f in facts:
        for attacker_units, attacker_defs, v_share, v_value, killed, surviving_pm in f["dirs"]:
            killed = min(killed, cap)
            total_v = sum(v_share.values()) or 1
            for a, a_value in v_share.items():
                share = a_value / total_v
                killed_a = killed * share
                expected_a = min(v_value, cap) * (1000 - min(1000, max(0, surviving_pm))) / 1000.0 * share
                # delivery -> [mobile power, defence power] against this armour class
                power: dict[str, list[float]] = {}
                for comp_map, is_def in ((attacker_units, False), (attacker_defs, True)):
                    for t, n in comp_map.items():
                        p = profiles.get(t.lower())
                        if not p:
                            continue
                        for tag, dpt in p["weapons"]:
                            if tag not in priors:  # empty Versus prior: excluded, never fitted
                                continue
                            w = n * dpt * priors.get(tag, {}).get(a, 100) / 100.0
                            e = power.setdefault(tag, [0.0, 0.0])
                            e[1 if is_def else 0] += w
                total_p = sum(sum(pw) for pw in power.values())
                if total_p <= 0:
                    continue
                for d, (unit_w, def_w) in power.items():
                    weight = (unit_w + def_w) / total_p
                    key = (d, a)
                    obs[key] = obs.get(key, 0.0) + killed_a * weight
                    exp[key] = exp.get(key, 0.0) + expected_a * weight
                    if def_w > 0:
                        ds_obs[d] = ds_obs.get(d, 0.0) + killed_a * def_w / total_p
                        ds_exp[d] = ds_exp.get(d, 0.0) + expected_a * def_w / total_p

        if f["into_defences"]:  # the toll our fire paid while their defences were live
            into_obs += min(f["killed_total"], cap)
            _, _, v_share, v_value, _, surviving_pm = f["dirs"][0]
            into_exp += min(v_value, cap) * (1000 - min(1000, max(0, surviving_pm))) / 1000.0

        if f["ratio"]:
            ratios.append(f["ratio"])
        if f["kind"] == "defend":
            if f["enemy_faction"]:
                attack_ticks.setdefault(f["enemy_faction"], []).append(f["start_tick"])
            resp = f["response"]
            if resp.get("response_ticks", -1) >= 0:
                response.setdefault(f["faction"], []).append(resp["response_ticks"])
            if resp.get("army_dist_at_start_cells", -1) >= 0:
                army_dist.setdefault(f["faction"], []).append(resp["army_dist_at_start_cells"])
        elif f["kind"] == "attack" and f["suicide_milli"] is not None and f["enemy_faction"]:
            suicide.setdefault((f["faction"], f["enemy_faction"]), []).append(f["suicide_milli"])

    def shrunk(o, e):
        return (o + SHRINK_VALUE) / (e + SHRINK_VALUE)

    cells = {k: max(MIN_MILLI, min(MAX_MILLI, round(1000 * shrunk(obs.get(k, 0.0), exp.get(k, 0.0)))))
             for k in set(obs) | set(exp)}
    # F1(b): per-cell staleness is the resolved Versus percent the cell was fitted on -
    # exactly what the dpt weight consumed (default 100 when the armour row is absent).
    # The consumer recomputes the current prior per cell and reverts only moved cells.
    cell_prior = {(d, a): priors.get(d, {}).get(a, 100) for (d, a) in cells}
    defence_state = {d: max(MIN_MILLI, min(MAX_MILLI, round(1000 * shrunk(ds_obs.get(d, 0.0), ds_exp.get(d, 0.0)))))
                     for d in set(ds_obs) | set(ds_exp)}
    into_defences_milli = max(MIN_MILLI, min(MAX_MILLI, round(1000 * shrunk(into_obs, into_exp))))

    # attrition exponent: corrected_ratio = ratio ** alpha; grid search the error on surviving
    # fractions (alpha = 1 is square law; only a real improvement moves it off 1000).
    exponent_milli = 1000
    if ratios:
        def cost(alpha: float) -> float:
            total = 0.0
            for pred, realised in ratios:
                frac = pred if alpha == 1 else math.pow(pred, 1 / alpha)
                total += (realised - frac) ** 2
            return total

        base = cost(1.0)
        best_alpha, best = 1.0, base
        for i in range(50, 201):  # 0.50 .. 2.00 in 0.01 steps
            a = i / 100
            c_ = cost(a)
            if c_ < best:
                best, best_alpha = c_, a
        if best < base:
            exponent_milli = round(1000 * best_alpha)

    attack_timing = {f: quantiles(v) for f, v in sorted(attack_ticks.items())}
    response_q = {}
    for f in sorted(set(response) | set(army_dist)):
        p50, p90 = quantiles(response.get(f, []), (50, 90))
        response_q[f] = [p50, p90, (quantiles(army_dist.get(f, []), (50,)))[0]]
    suicide_q = {}
    global_si = [v for vals in suicide.values() for v in vals]
    own_si: dict[str, list[int]] = {}
    for (mine, _), vals in suicide.items():
        own_si.setdefault(mine, []).extend(vals)
    for (mine, theirs), vals in sorted(suicide.items()):
        pool = vals if len(vals) >= MIN_PAIR_SAMPLES else (
            own_si.get(mine, []) if len(own_si.get(mine, [])) >= MIN_PAIR_SAMPLES else global_si)
        if pool:
            suicide_q[(mine, theirs)] = round(statistics.median(pool))

    return {
        "cells": cells, "evidence": {k: (obs.get(k, 0.0), exp.get(k, 0.0)) for k in cells},
        "cell_prior": cell_prior,
        "defence_state": defence_state, "into_defences_milli": into_defences_milli,
        "exponent_milli": exponent_milli,
        "attack_timing": attack_timing, "response": response_q, "suicide": suicide_q,
        "fitted": len(facts), "record_cap": cap, "skipped": skipped,
    }


def report(result: dict) -> str:
    s = result["skipped"]
    meta = result.get("meta") or {}
    out = [f"{result['fitted']} fitted engagements "
           f"({s['skirmish']} skirmish, {s['no_composition']} pre-composition, {s['unmapped']} unmapped skipped)"]
    if result.get("excluded_tags"):
        out.append(f"excluded delivery tags (empty Versus prior, not fitted): {', '.join(result['excluded_tags'])}")
    if meta.get("collisions"):
        out.append(f"profile-name collisions (first ledger wins): {', '.join(meta['collisions'])}")
    out.append(f"attrition exponent: {result['exponent_milli']} milli | into-defences: {result['into_defences_milli']} milli")
    moved = [(k, v, result["evidence"][k]) for k, v in result["cells"].items() if v != 1000]
    moved.sort(key=lambda x: -abs(x[1] - 1000))
    out.append(f"\n## delivery x armour: {len(result['cells'])} cells, {len(moved)} moved")
    for (d, a), milli, (o, e) in moved[:15]:
        out.append(f"- {d} x {a}: {milli} (obs {o:.0f} / exp {e:.0f})")
    out.append(f"\n## defence states: {len(result['defence_state'])}")
    for d, milli in sorted(result["defence_state"].items())[:10]:
        out.append(f"- {d}: {milli}")
    out.append("\n## attack timing (p10/p50/p90 ticks)")
    for f, q in result["attack_timing"].items():
        out.append(f"- {f}: {q}")
    out.append("\n## response (p50/p90 ticks, army dist cells)")
    for f, q in result["response"].items():
        out.append(f"- {f}: {q}")
    out.append("\n## suicide index medians")
    for (m_, t), v in result["suicide"].items():
        out.append(f"- {m_} vs {t}: {v}")
    return "\n".join(out)


def jsonable(result: dict) -> dict:
    """result with tuple-keyed maps flattened to yaml-style string keys for --json output."""
    out = dict(result)
    for k in ("cells", "evidence", "cell_prior"):
        out[k] = {"|".join(map(str, key)): v for key, v in result[k].items()}
    out["suicide"] = {"__vs__".join(map(str, key)): v for key, v in result["suicide"].items()}
    return out


def to_yaml(result: dict, ledger_hash: str) -> str:
    lines = ["# GENERATED by tools/ai/fit_engagement_priors.py - do not edit by hand; regenerate and review.",
             "# Tier-1 offline priors (TIER1_FITTER_SPEC, DESIGN 19.13/19.2): read at match start, frozen.",
             f"# {result['fitted']} fitted engagements."]
    if result.get("excluded_tags"):
        lines.append(f"# excluded delivery tags (empty Versus prior, not fitted): {', '.join(result['excluded_tags'])}")
    lines += ["BotEngagementPriors:", "\tSchema: 1",
             f"\tLedgerHash: {ledger_hash}", f"\tEngagements: {result['fitted']}",
             f"\tAttritionExponentMilli: {result['exponent_milli']}",
             f"\tIntoDefencesMilli: {result['into_defences_milli']}"]
    for (d, a), milli in sorted(result["cells"].items()):
        lines.append(f"\tDeliveryArmour@{d}__x__{a}: {milli}")
        lines.append(f"\tPriorPct@{d}__x__{a}: {result['cell_prior'][(d, a)]}")
    for d, milli in sorted(result["defence_state"].items()):
        lines.append(f"\tDefenceState@{d}: {milli}")
    for f, q in result["attack_timing"].items():
        lines.append(f"\tAttackTiming@{f}: {', '.join(str(x) for x in q)}")
    for f, q in result["response"].items():
        lines.append(f"\tResponse@{f}: {', '.join(str(x) for x in q)}")
    for (m_, t), v in result["suicide"].items():
        lines.append(f"\tSuicideIndex@{m_}__vs__{t}: {v}")
    return "\n".join(lines) + "\n"


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("dirs", nargs="+", type=pathlib.Path)
    ap.add_argument("--write", type=pathlib.Path, help="write the priors yaml here")
    ap.add_argument("--repo", type=pathlib.Path, default=REPO, help="repo root holding docs/balance")
    ap.add_argument("--json", action="store_true", help="print the result as JSON")
    args = ap.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")

    profiles, meta, ledger_hash = load_profiles(args.repo)
    tags = sorted({tag for p in profiles.values() for tag, _ in p["weapons"]})
    priors, excluded = versus_priors(args.repo, tags)
    result = fit(c.load(args.dirs), profiles, priors)
    result["meta"] = meta
    result["excluded_tags"] = excluded
    if args.json:
        print(json.dumps(jsonable(result), indent=2, default=str))
    else:
        print(report(result))
    if args.write:
        args.write.parent.mkdir(parents=True, exist_ok=True)
        args.write.write_text(to_yaml(result, ledger_hash), encoding="utf-8", newline="\n")
        print(f"\nwrote {args.write}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
