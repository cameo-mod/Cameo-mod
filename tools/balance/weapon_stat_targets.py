#!/usr/bin/env python3
"""Read-only R1 weapon-stat targets with DPS kept as a verifier.

Each target is computed independently through the existing reference vote path. The tool never
decomposes a DPS target into damage/reload, never writes a ledger, and withholds a composed DPS
check when the reference rows do not carry burst-delay evidence.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys
from collections import Counter

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))
import diagnostic_output  # noqa: E402
import reference_distribution as rd  # noqa: E402
import reference_targets as rt  # noqa: E402

INPUT_STATS = ("w_damage", "w_reload", "w_burst")
VERIFIER_STAT = "w_dps"


def _load():
    peers = rd.peer_rows()
    cameo = rd.cameo_rows()
    dist = rd.build_distributions(peers)
    rt.add_cost_distribution(dist, peers)
    cdist = rt.cameo_context()
    assignment = json.loads(
        (ROOT / "docs" / "balance" / "derived" / "reference_assignment.json").read_text(
            encoding="utf-8-sig"))["assignment"]
    attached = rt.expand_families(rt.attach(assignment, rt.peer_index(peers)), peers)
    return peers, cameo, dist, cdist, attached


def build_report():
    _peers, cameo, dist, cdist, attached = _load()
    rows = []
    statuses = Counter()
    for actor in sorted(cameo, key=lambda row: row["id"]):
        name = actor["id"]
        refs = attached.get(name) or []
        if not refs:
            statuses["NO_REFERENCE"] += 1
            continue
        targets = {}
        sources = {}
        for stat in (*INPUT_STATS, VERIFIER_STAT):
            peers_only, with_cameo, count = rt.target_for(refs, actor, stat, dist, cdist)
            targets[stat] = with_cameo
            sources[stat] = count

        missing = [stat for stat in INPUT_STATS if targets[stat] is None]
        # The current peer rows expose w_damage/w_reload/w_burst but no common
        # w_burst_delays coordinate. A verifier that silently substitutes Cameo's
        # delay would compose unlike-for-like inputs, so keep it withheld.
        if missing:
            verifier_status = "WITHHELD_MISSING_SEPARATE_INPUT"
        else:
            verifier_status = "WITHHELD_MISSING_BURST_DELAYS"
        statuses[verifier_status] += 1
        rows.append({
            "actor": name,
            "reference_sources": sources,
            "current": {stat: actor.get(stat) for stat in (*INPUT_STATS, VERIFIER_STAT)},
            "targets": targets,
            "dps_verifier": {
                "reference_target": targets[VERIFIER_STAT],
                "status": verifier_status,
                "basis": "DPS remains an independent verifier; no decomposition writeback",
                "missing": missing + ["w_burst_delays"],
            },
        })
    return {
        "schema": 1,
        "scope": "R1 separate weapon-stat targets; read-only diagnostic",
        "inputs": list(INPUT_STATS),
        "verifier": VERIFIER_STAT,
        "source_rows": len(_peers),
        "rows": rows,
        "status_counts": dict(sorted(statuses.items())),
        "writeback": False,
        "ini_corpus_regenerated": False,
    }


def markdown(report):
    counts = report["status_counts"]
    rows = report["rows"]
    lines = [
        "# R1 weapon-stat targets",
        "",
        "**Read-only diagnostic.** Damage per shot, reload, and burst are reported as separate reference targets. DPS is retained as an independent verifier; it is never decomposed into those inputs and no ledger or YAML value is written.",
        "",
        f"Rows with reference families: **{len(rows)}**; source rows scanned: **{report['source_rows']}**.",
        "",
        "The current reference rows do not expose a common `w_burst_delays` target. Therefore every composed DPS check is explicitly withheld rather than mixing a reference target with Cameo's live burst-delay value.",
        "",
        "## Status counts",
        "",
        "| status | rows |",
        "|---|---:|",
    ]
    for key, value in counts.items():
        lines.append(f"| `{key}` | {value} |")
    lines += ["", "## Sample target rows", "", "| actor | damage | reload | burst | DPS verifier | status |", "|---|---:|---:|---:|---:|---|"]
    for row in rows[:40]:
        t = row["targets"]
        v = row["dps_verifier"]
        fmt = lambda value: "—" if value is None else f"{value:g}"
        lines.append(f"| `{row['actor']}` | {fmt(t['w_damage'])} | {fmt(t['w_reload'])} | {fmt(t['w_burst'])} | {fmt(v['reference_target'])} | `{v['status']}` |")
    return "\n".join(lines) + "\n"


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--out", required=True)
    parser.add_argument("--markdown", required=True)
    args = parser.parse_args(argv)
    out = pathlib.Path(args.out)
    md = pathlib.Path(args.markdown)
    try:
        diagnostic_output.validate_path(ROOT, out)
        diagnostic_output.validate_path(ROOT, md)
    except ValueError as exc:
        parser.error(str(exc))
    report = build_report()
    diagnostic_output.write_outputs(ROOT, {
        out: json.dumps(report, indent=2, ensure_ascii=False) + "\n",
        md: markdown(report),
    })
    print(json.dumps({"rows": len(report["rows"]), "status_counts": report["status_counts"]}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
