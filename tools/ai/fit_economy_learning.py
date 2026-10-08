#!/usr/bin/env python3
"""Fit bounded LEARN-ECON advice from anonymous, credited decision records.

The input contract is deliberately narrow. A row is accepted only when it is
an ``economy_decision/1`` record with a unique event id, a compatible reviewed
balance fingerprint, public faction labels, one bounded candidate, and a
post-decision economy reward. It neither reads nor preserves player, seat,
map, replay, coordinate, or account identity. Map and opponent feature
models are owned by CONTEXT-PROVIDERS; this bootstrap writer emits only safe
faction/matchup parent rows until those providers and schemas land.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
from collections import defaultdict
from dataclasses import dataclass, field

SCHEMA = "economy_decision/1"
MINIMUM = {"expansion": 100, "harvester": 120, "production": 120, "credit": 150}
MINIMUM_ARM = 20
SHRINKAGE_WEIGHT = 50
BOUNDS = {
    "expansion": (1000, 50000), "harvester": (1, 64),
    "production": (0, 50000), "credit": (0, 50000),
}
PREFIX = {
    "expansion": ("ExpansionCashDivisor", "ExpansionEvidence"),
    "harvester": ("HarvesterLimit", "HarvesterEvidence"),
    "production": ("ProductionCashThreshold", "ProductionEvidence"),
    "credit": ("CreditFloat", "CreditFloatEvidence"),
}
REPO = pathlib.Path(__file__).resolve().parents[2]
FINGERPRINT = re.compile(r"[0-9a-fA-F]{8,64}\Z")


@dataclass
class Cell:
    """Candidate -> anonymous reward observations for one policy scope."""
    arms: dict[int, list[int]] = field(default_factory=lambda: defaultdict(list))

    @property
    def n(self) -> int:
        return sum(len(rewards) for rewards in self.arms.values())


def integer(value):
    return value if type(value) is int else None


def canonical_factions() -> set[str]:
    result = set()
    for path in sorted((REPO / "mods" / "cameo").rglob("*.yaml")):
        try:
            result.update(re.findall(r"^\s*InternalName: ([a-z0-9_.-]+)\s*$", path.read_text(encoding="utf-8-sig"), re.M))
        except OSError:
            continue
    return result


def read_records(paths: list[pathlib.Path]):
    for path in paths:
        candidates = sorted(path.rglob("*.jsonl")) if path.is_dir() else [path]
        for candidate in candidates:
            try:
                with candidate.open(encoding="utf-8") as stream:
                    for line in stream:
                        try:
                            row = json.loads(line)
                        except json.JSONDecodeError:
                            continue
                        # JSONL may contain valid JSON values that are not event
                        # objects. Ignore them so one malformed producer row cannot
                        # abort the whole offline fit.
                        if isinstance(row, dict) and row.get("record") == "economy_decision":
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


def facts(row: dict, allowed_factions: set[str]):
    """Return reviewed anonymous facts, or None for a malformed/unsafe row."""
    if not isinstance(row, dict):
        return None
    decision, outcome, balance = row.get("decision"), row.get("outcome"), row.get("balance")
    record_id = row.get("record_id")
    if (row.get("schema") != SCHEMA or not isinstance(record_id, str) or not record_id
            or not isinstance(decision, dict) or not isinstance(outcome, dict)
            or not isinstance(balance, dict)):
        return None
    fingerprint = balance.get("fingerprint")
    if not isinstance(fingerprint, str) or FINGERPRINT.fullmatch(fingerprint) is None:
        return None
    kind = decision.get("parameter")
    candidate = integer(decision.get("candidate"))
    reward = integer(outcome.get("economy_reward_milli"))
    own, enemy = row.get("faction"), row.get("enemy_faction")
    if (not isinstance(kind, str) or not isinstance(own, str)
            or kind not in BOUNDS or candidate is None or reward is None
            or own not in allowed_factions):
        return None
    lower, upper = BOUNDS[kind]
    # Do not turn malformed data into a seemingly valid recommendation. The
    # reward is signed credit assignment and has only an overflow guard.
    if not lower <= candidate <= upper or not -1_000_000 <= reward <= 1_000_000:
        return None
    if (not row.get("enemy_faction_public") or not isinstance(enemy, str)
            or enemy not in allowed_factions):
        enemy = ""
    return record_id, fingerprint.lower(), kind, own, enemy, candidate, reward


def largest_fingerprint(rows):
    counts = defaultdict(int)
    for row in rows:
        counts[row[1]] += 1
    return min(counts, key=lambda item: (-counts[item], item)) if counts else None


def best_candidate(cell: Cell):
    """Select a deterministic lower-confidence winner using integer arithmetic."""
    eligible = []
    for candidate, rewards in cell.arms.items():
        if len(rewards) < MINIMUM_ARM:
            continue
        mean = sum(rewards) // len(rewards)
        # A small, explicit conservative penalty prevents a lucky sparse arm
        # from beating a mature policy arm. It is offline only.
        lcb = mean - 50_000 // len(rewards)
        eligible.append((lcb, len(rewards), candidate))
    return max(eligible, key=lambda item: (item[0], item[1], -item[2]))[2] if eligible else None


def parent_scope(scope: str):
    if "__vs__" in scope:
        return scope.split("__vs__", 1)[0]
    if scope.startswith("family_"):
        return "any"
    if "_" in scope:
        return "family_" + scope.split("_", 1)[0]
    return "any" if scope != "any" else None


def fit(source):
    allowed_factions = canonical_factions()
    unique, seen_ids = [], set()
    for row in source:
        item = facts(row, allowed_factions)
        if item is not None and item[0] not in seen_ids:
            seen_ids.add(item[0])
            unique.append(item)
    fingerprint = largest_fingerprint(unique)
    cells: dict[str, dict[str, Cell]] = defaultdict(lambda: defaultdict(Cell))
    for _, row_fingerprint, kind, own, enemy, candidate, reward in unique:
        if row_fingerprint != fingerprint:
            continue
        for scope in scope_chain(own, enemy):
            cells[kind][scope].arms[candidate].append(reward)

    fitted = {}
    for kind, kind_cells in cells.items():
        eligible = {scope for scope, cell in kind_cells.items()
                    if cell.n >= MINIMUM[kind] and best_candidate(cell) is not None}
        if not eligible:
            continue
        cache = {}

        def smoothed(scope):
            if scope in cache:
                return cache[scope]
            local, parent = best_candidate(kind_cells[scope]), parent_scope(scope)
            if parent not in eligible:
                cache[scope] = local
            else:
                parent_value, n = smoothed(parent), kind_cells[scope].n
                cache[scope] = (local * n + parent_value * SHRINKAGE_WEIGHT) // (n + SHRINKAGE_WEIGHT)
            return cache[scope]

        fitted[kind] = {scope: (kind_cells[scope].n, smoothed(scope)) for scope in sorted(eligible)}
    return fitted


def render(fitted) -> str:
    out = [
        "# GENERATED by tools/ai/fit_economy_learning.py --write; do not edit by hand.",
        "# Anonymous faction/matchup rows only. Sparse or absent rows retain authored behaviour.",
        "BotEconomyLearning:", "\tSchema: 1",
        f"\tExpansionMinimumSamples: {MINIMUM['expansion']}",
        f"\tHarvesterMinimumSamples: {MINIMUM['harvester']}",
        f"\tProductionMinimumSamples: {MINIMUM['production']}",
        f"\tCreditFloatMinimumSamples: {MINIMUM['credit']}",
    ]
    for kind in ("expansion", "harvester", "production", "credit"):
        value_name, evidence_name = PREFIX[kind]
        for scope, (n, value) in fitted.get(kind, {}).items():
            out.append(f"\t{value_name}@{scope}: {value}")
            out.append(f"\t{evidence_name}@{scope}: {n}")
    return "\n".join(out) + "\n"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("paths", nargs="+", type=pathlib.Path)
    parser.add_argument("--write", type=pathlib.Path)
    args = parser.parse_args()
    text = render(fit(read_records(args.paths)))
    if args.write:
        args.write.parent.mkdir(parents=True, exist_ok=True)
        args.write.write_text(text, encoding="utf-8")
    else:
        print(text, end="")


if __name__ == "__main__":
    main()
