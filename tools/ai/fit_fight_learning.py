#!/usr/bin/env python3
"""Fit frozen LEARN-FIGHT rows from anonymous P0 engagement records.

This deliberately has no player identity input.  Rows pool from
faction__vs__faction to faction, family and any; only cells with the published
minimum evidence are emitted.  Runtime consumes threshold rows behind BV_learn_fight.
Calibration/value rows are provenance for the existing tier-1 engagement-prior fit:
fit_engagement_priors.py remains the sole delivery×armour correction writer.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
from collections import defaultdict
from dataclasses import dataclass

CALIBRATION_MIN = 200
EFFECTIVE_VALUE_MIN = 250
THRESHOLD_MIN = 150
NEUTRAL = 1000
REPO = pathlib.Path(__file__).resolve().parents[2]
FINGERPRINT = re.compile(r"[0-9a-fA-F]{8,64}\Z")


@dataclass
class Totals:
    n: int = 0
    predicted: int = 0
    actual: int = 0
    trade: int = 0
    own: int = 0
    wins: int = 0


def read_records(paths: list[pathlib.Path]):
    for path in paths:
        if path.is_dir():
            yield from read_records(sorted(path.rglob("cameo-ai-engagements.jsonl")))
            continue
        try:
            with path.open(encoding="utf-8") as stream:
                for line in stream:
                    try:
                        row = json.loads(line)
                    except json.JSONDecodeError:
                        continue
                    if row.get("record") == "engagement":
                        yield row
        except OSError:
            continue


def scope_chain(own: str, enemy: str):
    family = own.split("_", 1)[0] if "_" in own else ""
    if own and enemy:
        yield f"{own}__vs__{enemy}"
    if own:
        yield own
    if family:
        yield f"family_{family}"
    yield "any"


def integer_median(values: list[int]) -> int:
    values = sorted(values)
    return values[(len(values) - 1) // 2] if values else 0


def canonical_factions() -> set[str]:
    result = set()
    for path in sorted((REPO / "mods" / "cameo").rglob("*.yaml")):
        try:
            result.update(re.findall(r"^\s*InternalName: ([a-z0-9_.-]+)\s*$", path.read_text(encoding="utf-8-sig"), re.M))
        except OSError:
            continue
    return result


def integer(value):
    return value if type(value) is int else None


def facts(row: dict, allowed_factions: set[str]):
    seen = row.get("seen") or {}
    start = seen.get("start") or {}
    outcome = row.get("outcome") or {}
    own, enemy = row.get("faction") or "", row.get("enemy_faction") or ""
    balance = row.get("balance") or {}
    record_id = row.get("record_id")
    if row.get("schema") != "engagement/2" or not isinstance(record_id, str) or not record_id:
        return None
    fingerprint = balance.get("fingerprint")
    if not isinstance(fingerprint, str) or FINGERPRINT.fullmatch(fingerprint) is None:
        return None
    # The fitter accepts only faction vocabulary from the reviewed rules. An
    # undisclosed Random enemy is deliberately pooled above exact matchup scope.
    if own not in allowed_factions:
        return None
    if not row.get("enemy_faction_public") or enemy not in allowed_factions:
        enemy = ""
    own_units, own_defences = integer(start.get("own_committed_value")), integer(start.get("own_defence_value"))
    enemy_value, predicted, ratio = integer(start.get("enemy_unit_value")), integer(start.get("predicted_enemy_surviving_permille")), integer(start.get("predicted_ratio_milli"))
    killed, lost = integer(outcome.get("enemy_killed_value")), integer(outcome.get("own_lost_value"))
    if None in (own_units, own_defences, enemy_value, predicted, ratio, killed, lost):
        return None
    own_value = own_units + own_defences
    if own_value <= 0 or enemy_value <= 0 or not 0 <= predicted <= 1000 or not 0 <= ratio <= 2000:
        return None
    expected = max(1, enemy_value * (1000 - predicted) // 1000)
    actual = min(enemy_value, max(0, killed))
    return record_id, fingerprint.lower(), own, enemy, expected, actual, actual - min(own_value, max(0, lost)), own_value, ratio


def fit(records):
    totals = defaultdict(Totals)
    threshold_observations = defaultdict(list)
    allowed_factions = canonical_factions()
    unique, seen_ids = [], set()
    for row in records:
        item = facts(row, allowed_factions)
        if item is None or item[0] in seen_ids:
            continue
        seen_ids.add(item[0])
        unique.append(item)

    # Balance versions are never silently pooled. Select the largest compatible
    # cohort deterministically; offline release tooling may review it as one fit.
    if not unique:
        return {}
    fingerprint = sorted({item[1] for item in unique}, key=lambda f: (-sum(x[1] == f for x in unique), f))[0]
    for _, row_fingerprint, own, enemy, expected, actual, trade, own_value, ratio in unique:
        if row_fingerprint != fingerprint:
            continue
        for scope in scope_chain(own, enemy):
            total = totals[scope]
            total.n += 1
            total.predicted += expected
            total.actual += actual
            total.trade += trade
            total.own += own_value
            if trade >= 0:
                total.wins += 1
            threshold_observations[scope].append(max(1, min(100, ratio // 10)))

    eligible = {scope for scope, total in totals.items() if total.n >= THRESHOLD_MIN}

    def parent(scope):
        if "__vs__" in scope:
            return scope.split("__vs__", 1)[0]
        if scope.startswith("family_"):
            return "any"
        if "_" in scope:
            return "family_" + scope.split("_", 1)[0]
        return "any" if scope != "any" else None

    def smoothed_retreat(scope):
        local = max(1, min(100, integer_median(threshold_observations[scope])))
        ancestor = parent(scope)
        if ancestor not in eligible:
            return local
        # Integer empirical-Bayes shrinkage: exactly-minimum cells borrow 50
        # pseudo-observations from their parent; large cohorts converge locally.
        return (local * totals[scope].n + smoothed_retreat(ancestor) * 50) // (totals[scope].n + 50)

    rows = {}
    for scope, total in sorted(totals.items()):
        if scope not in eligible:
            continue
        calibration = (max(500, min(2000, total.actual * NEUTRAL // max(1, total.predicted)))
                       if total.n >= CALIBRATION_MIN else None)
        # Trade per committed own value, neutral at 1000. Unlike the former
        # win-count formula this has one milli scaling operation and remains useful
        # throughout its range instead of saturating after a single good trade.
        effective = (max(500, min(2000, NEUTRAL + total.trade * NEUTRAL // max(1, total.own)))
                     if total.n >= EFFECTIVE_VALUE_MIN else None)
        retreat = smoothed_retreat(scope)
        # Preserve hysteresis and never reduce the engage threshold below retreat.
        engage = max(100, min(300, 150 + (retreat - 50)))
        rows[scope] = (total.n, calibration, effective, retreat, engage)
    return rows


def render(rows):
    out = [
        "# GENERATED by tools/ai/fit_fight_learning.py --write; do not edit by hand.",
        "# Anonymous scopes only; runtime uses reviewed integer rows and falls back when a row is absent.",
        "BotFightLearning:",
        "\tSchema: 1",
        f"\tCalibrationMinimumSamples: {CALIBRATION_MIN}",
        f"\tThresholdMinimumSamples: {THRESHOLD_MIN}",
    ]
    for scope, (n, calibration, effective, retreat, engage) in rows.items():
        out.append(f"\tEvidence@{scope}: {n}")
        if calibration is not None:
            out.append(f"\tCalibrationMilli@{scope}: {calibration}")
            out.append(f"\tEffectiveValueMilli@{scope}: {effective}")
        out += [f"\tRetreatRatioPct@{scope}: {retreat}", f"\tEngageMarginPct@{scope}: {engage}"]
    return "\n".join(out) + "\n"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("paths", nargs="+", type=pathlib.Path)
    parser.add_argument("--write", type=pathlib.Path)
    args = parser.parse_args()
    rows = fit(read_records(args.paths))
    text = render(rows)
    if args.write:
        args.write.parent.mkdir(parents=True, exist_ok=True)
        args.write.write_text(text, encoding="utf-8")
    else:
        print(text, end="")


if __name__ == "__main__":
    main()
