#!/usr/bin/env python3
"""Cluster anonymous, seen-side opponent signatures into offline archetypes.

Reads schema-3 cameo-ai-matches JSONL and writes a deterministic learned file.
This tool has no runtime consumer in phase 0.
"""
from __future__ import annotations

import argparse
import json
import math
from collections import defaultdict
from pathlib import Path

FEATURES = ("army_value", "infantry_value", "vehicle_value", "air_value", "naval_value",
            "defence_value", "building_count", "harvester_count", "known_regions")


def cluster_key(seen: dict) -> tuple:
    faction = str(seen.get("faction", "unknown"))
    # Logarithmic buckets make scale differences meaningful without overfitting to map size.
    bands = tuple(int(math.log2(max(0, int(seen.get(k, 0))) + 1)) for k in FEATURES)
    return faction, bands


def fit(paths: list[Path]) -> dict:
    members = defaultdict(list)
    for path in paths:
        for line in path.read_text(encoding="utf-8").splitlines():
            if not line.strip():
                continue
            row = json.loads(line)
            if row.get("schema") != 3:
                continue
            for signature in row.get("opponent_signatures", []):
                seen = signature.get("seen")
                if isinstance(seen, dict):
                    members[cluster_key(seen)].append({k: int(seen.get(k, 0)) for k in FEATURES})
    clusters = []
    for (faction, bands), rows in sorted(members.items()):
        center = {k: round(sum(r[k] for r in rows) / len(rows)) for k in FEATURES}
        clusters.append({"id": f"sig_{len(clusters) + 1:04d}", "faction": faction,
                         "count": len(rows), "bands": list(bands), "center": center})
    return {"schema": "cameo-opponent-signatures/1", "source_schema": 3, "clusters": clusters}


def dump_yaml(data: dict) -> str:
    lines = [f'schema: "{data["schema"]}"', f'source_schema: {data["source_schema"]}', "clusters:"]
    if not data["clusters"]:
        lines.append("  []")
    for c in data["clusters"]:
        lines += [f'  - id: "{c["id"]}"', f'    faction: "{c["faction"]}"',
                  f'    count: {c["count"]}', "    bands: [" + ", ".join(map(str, c["bands"])) + "]", "    center:"]
        lines += [f"      {k}: {c['center'][k]}" for k in FEATURES]
    return "\n".join(lines) + "\n"


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("logs", nargs="+", type=Path, help="cameo-ai-matches.jsonl files")
    ap.add_argument("--out", type=Path, default=Path("mods/cameo/ai/learned/opponent_signatures.yaml"))
    args = ap.parse_args()
    args.out.write_text(dump_yaml(fit(args.logs)), encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
