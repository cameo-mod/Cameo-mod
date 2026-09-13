#!/usr/bin/env python3
"""Audit the burst-1 arithmetic signature on peer rows that declare Burst > 1.

Read-only. The signature is not proof burst was ignored: exact inter-shot delays can legitimately
produce the same rate.
"""
from __future__ import annotations

import argparse
import collections
import json
import math
import pathlib
import sys


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))

import reference_distribution as rd  # noqa: E402


def has_burst_one_signature(row) -> bool:
    burst = float(row.get("w_burst") or 1)
    per_shot = row.get("w_damage_per_shot")
    reload_ticks = row.get("w_reload")
    rate = row.get("w_dps")
    if burst <= 1 or not per_shot or not reload_ticks or not rate:
        return False
    expected_burst_one = float(per_shot) / float(reload_ticks)
    return abs(float(rate) - expected_burst_one) <= 1e-9 * max(1.0, abs(float(rate)))


def committed_delays(row):
    """Exact committed delays only; no engine/source defaults are inferred."""
    value = row.get("w_burst_delays")
    cycle = row.get("w_cycle_evidence")
    if not value and isinstance(cycle, dict):
        value = cycle.get("burst_delays")
    if not isinstance(value, list) or not value:
        return None
    delays = []
    try:
        for item in value:
            if isinstance(item, dict):
                lo, mean, hi = item.get("minimum"), item.get("mean"), item.get("maximum")
                if lo is None or mean is None or hi is None or not (lo == mean == hi):
                    return None
                item = mean
            item = float(item)
            if not math.isfinite(item) or item < 0:
                return None
            delays.append(item)
    except (TypeError, ValueError):
        return None
    burst = int(float(row.get("w_burst") or 1))
    gaps = max(0, burst - 1)
    if len(delays) == 1:
        return delays * gaps
    return delays if len(delays) == gaps else None


def exact_cycle_ticks(row):
    """Exact committed cycle including reviewed scheduling adjustments, when available."""
    evidence = row.get("w_cycle_evidence")
    if isinstance(evidence, dict):
        values = [evidence.get(key) for key in ("cycle_min", "cycle_mean", "cycle_max")]
        try:
            values = [float(value) for value in values]
        except (TypeError, ValueError):
            return None
        if all(math.isfinite(value) for value in values) and values[0] == values[1] == values[2] \
                and values[0] > 0:
            return values[0]
        return None
    delays = committed_delays(row)
    if delays is None:
        return None
    try:
        cycle = float(row.get("w_reload")) + sum(delays)
    except (TypeError, ValueError):
        return None
    return cycle if math.isfinite(cycle) and cycle > 0 else None


def evidence_status(row) -> str:
    cycle = exact_cycle_ticks(row)
    if cycle is None:
        return "legacy_requires_cycle_evidence"
    expected = float(row["w_damage_per_shot"]) * float(row["w_burst"]) / cycle
    rate = float(row["w_dps"])
    if abs(rate - expected) <= 1e-9 * max(1.0, abs(rate)):
        return "evidence_consistent_no_recompute"
    return "ready_exact_cycle_recompute"


def build_report() -> dict:
    rows = rd.peer_rows()
    burst_rows = [row for row in rows
                  if float(row.get("w_burst") or 1) > 1
                  and row.get("w_dps") and row.get("w_reload")
                  and row.get("w_damage_per_shot")]
    matches = [row for row in burst_rows if has_burst_one_signature(row)]
    by_source = collections.Counter(row["source"] for row in matches)
    by_evidence = collections.Counter(row.get("w_evidence") or "missing" for row in matches)
    entries = []
    for row in sorted(matches, key=lambda item: (item["source"], str(item.get("id")))):
        delays = committed_delays(row)
        status = evidence_status(row)
        cycle_evidence = row.get("w_cycle_evidence") or {}
        entries.append({
            "source": row["source"],
            "id": row.get("id"),
            "weapon": row.get("weapon"),
            "damage_per_shot": row.get("w_damage_per_shot"),
            "damage_per_cycle": row.get("w_damage"),
            "burst": row.get("w_burst"),
            "reload": row.get("w_reload"),
            "recorded_rate": row.get("w_dps"),
            "evidence": row.get("w_evidence"),
            "expanded_burst_delays": delays,
            "exact_cycle_ticks": exact_cycle_ticks(row),
            "cycle_scope": cycle_evidence.get("scope"),
            "status": status,
        })
    statuses = collections.Counter(entry["status"] for entry in entries)
    return {
        "schema": 2,
        "scope": "loaded v21 peer population; read-only screening and evidence join",
        "counts": {
            "loaded_peer_rows": len(rows),
            "burst_rows_with_rate_inputs": len(burst_rows),
            "burst_one_signature_matches": len(entries),
            "evidence_consistent_no_recompute": statuses["evidence_consistent_no_recompute"],
            "ready_exact_cycle_recompute": statuses["ready_exact_cycle_recompute"],
            "legacy_requires_cycle_evidence": statuses["legacy_requires_cycle_evidence"],
        },
        "by_source": dict(sorted(by_source.items())),
        "by_evidence": dict(sorted(by_evidence.items())),
        "rows": entries,
        "decision": (
            "The burst-1 equality is a screening signature, not proof that the rate ignored burst. "
            "One DTA Enhanced MLRS row has reviewed exact cycle evidence and already satisfies the "
            "universal formula, including its 400-tick emission gap; retain its nominal-model and "
            "runtime limitations. The other 333 legacy rows need exact cycle evidence before any "
            "recomputation. No zero-delay or source-default assumption is emitted."
        ),
    }


def markdown(report: dict) -> str:
    c = report["counts"]
    lines = [
        "# Burst-rate signature audit",
        "",
        "**Read-only. No rate or reference target is changed.**",
        "",
        report["decision"],
        "",
        f"- Loaded peer rows: **{c['loaded_peer_rows']}**",
        f"- Burst rows with complete rate inputs: **{c['burst_rows_with_rate_inputs']}**",
        f"- Burst-1 signature matches: **{c['burst_one_signature_matches']}**",
        f"- Evidence-consistent; no recompute: **{c['evidence_consistent_no_recompute']}**",
        f"- Ready with exact cycle evidence: **{c['ready_exact_cycle_recompute']}**",
        f"- Legacy rows requiring cycle evidence: **{c['legacy_requires_cycle_evidence']}**",
        "",
        "| source | signature matches |",
        "|---|---:|",
    ]
    for source, count in report["by_source"].items():
        lines.append(f"| {source} | {count} |")
    lines += [
        "",
        "The JSON names every row and carries its exact-cycle join, expanded delays, status, and "
        "reviewed scope. The DTA result is a nominal timing model, not verified sustained runtime DPS.",
        "",
    ]
    return "\n".join(lines)


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--json", type=pathlib.Path)
    parser.add_argument("--markdown", type=pathlib.Path)
    args = parser.parse_args(argv)
    report = build_report()
    if args.json:
        (ROOT / args.json).write_text(
            json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    if args.markdown:
        (ROOT / args.markdown).write_text(markdown(report), encoding="utf-8")
    if not args.json and not args.markdown:
        print(markdown(report), end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
