#!/usr/bin/env python3
"""One-time migration of legacy Cameo AI logs to anonymous seat_N schemas.

Before replacing any input, this tool creates a timestamped rollback directory
containing copies of the original logs. That backup still holds player names
and client identifiers; it is for rollback only, and the user should delete it
once satisfied that the anonymized logs are correct.

Seat labels are assigned per game from the sorted legacy seat references, so
every log kind joins consistently without preserving the original names. The
tool is idempotent: records already on the anonymous schemas are left alone.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import shutil
import tempfile
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path

LOG_NAMES = (
    "cameo-ai-matches.jsonl",
    "cameo-ai-situations.jsonl",
    "cameo-ai-placements.jsonl",
    "cameo-ai-missions.jsonl",
    "cameo-ai-engagements.jsonl",
)
CURRENT = {
    "cameo-ai-matches.jsonl": 3,
    "cameo-ai-situations.jsonl": 3,
    "cameo-ai-placements.jsonl": 2,
    "cameo-ai-missions.jsonl": "mission-card/2",
    "cameo-ai-engagements.jsonl": "engagement/2",
}
OLD = {
    "cameo-ai-matches.jsonl": {1, 2},
    "cameo-ai-situations.jsonl": {1, 2},
    "cameo-ai-placements.jsonl": {1},
    "cameo-ai-missions.jsonl": {"mission-card/1"},
    "cameo-ai-engagements.jsonl": {"engagement/1"},
}
CLIENT_KEYS = {"client", "client_id", "client_index", "controller_client", "controller_client_id", "clientid"}
FEATURE_FIELDS = (
    "army_value", "infantry_value", "vehicle_value", "air_value", "naval_value",
    "defence_value", "building_count", "harvester_count", "known_regions", "last_seen_tick",
)


def game_key(row: dict) -> str:
    uid = str(row.get("game_uid") or "")
    if uid:
        return uid
    old_id = str(row.get("record_id") or "")
    prefix = old_id.split("|", 1)[0]
    if prefix:
        return prefix
    stable = json.dumps(row, sort_keys=True, separators=(",", ":"))
    return "legacy-" + hashlib.sha256(stable.encode("utf-8")).hexdigest()[:16]


def is_legacy(filename: str, row: dict) -> bool:
    return row.get("schema") in OLD[filename]


def identity_values(filename: str, row: dict) -> set[str]:
    values: set[str] = set()
    player = row.get("player")
    if isinstance(player, dict) and isinstance(player.get("name"), str):
        values.add(player["name"])
    elif isinstance(player, str) and player:
        values.add(player)
    for key in ("allies", "opponents", "enemies"):
        for item in row.get(key) or []:
            if isinstance(item, dict) and isinstance(item.get("name"), str) and item["name"]:
                values.add(item["name"])
    target = row.get("main_target")
    if isinstance(target, str) and target:
        values.add(target)
    return values


def id_map(rows: list[tuple[str, dict]]) -> dict[str, dict[str, str]]:
    names: dict[str, set[str]] = defaultdict(set)
    for filename, row in rows:
        if is_legacy(filename, row):
            names[game_key(row)].update(identity_values(filename, row))
    return {
        game: {name: f"seat_{i}" for i, name in enumerate(sorted(values), 1)}
        for game, values in names.items()
    }


def seat_for(row: dict, maps: dict[str, dict[str, str]], old_name: str | None = None) -> str:
    mapping = maps.get(game_key(row), {})
    if old_name in mapping:
        return mapping[old_name]
    if old_name and re.fullmatch(r"seat_[1-9][0-9]*", old_name):
        return old_name
    # A reference missed by a malformed/partial file still receives a stable
    # anonymous seat without copying the reference into the output.
    ordered = sorted(set(mapping.values()))
    return ordered[0] if ordered else "seat_1"


def anonymous_text(value: str, mapping: dict[str, str]) -> str:
    if value in mapping:
        return mapping[value]
    result = value
    for name in sorted(mapping, key=len, reverse=True):
        result = re.sub(rf"(?<![A-Za-z0-9_]){re.escape(name)}(?![A-Za-z0-9_])", mapping[name], result)
    return result


def scrub_nested(value, mapping: dict[str, str], *, root: bool = False):
    if isinstance(value, list):
        return [scrub_nested(item, mapping) for item in value]
    if isinstance(value, dict):
        cleaned = {}
        for key, item in value.items():
            lowered = key.lower()
            if lowered in CLIENT_KEYS or lowered == "is_bot":
                continue
            if key == "name":
                continue
            if key == "player" and not root:
                continue
            cleaned[key] = scrub_nested(item, mapping)
        return cleaned
    if isinstance(value, str):
        return anonymous_text(value, mapping)
    return value


def relation(row: dict, mapping: dict[str, str]) -> dict:
    old_name = row.get("name")
    is_bot = bool(row.get("is_bot"))
    out = {k: scrub_nested(v, mapping) for k, v in row.items()
           if k not in {"name", "is_bot"} and k.lower() not in CLIENT_KEYS}
    out["seat"] = mapping.get(str(old_name), "seat_1")
    out["home"] = scrub_nested(row.get("home", ""), mapping)
    out["faction"] = scrub_nested(row.get("faction", ""), mapping)
    if not is_bot:
        out = {key: out[key] for key in ("seat", "faction", "home", "outcome") if key in out}
    else:
        out["bot_type"] = scrub_nested(row.get("bot_type", ""), mapping)
        if "team" in row:
            out["team"] = row["team"]
        if "handicap" in row:
            out["handicap"] = row["handicap"]
    return out


def normalize_row(filename: str, row: dict, maps: dict[str, dict[str, str]]) -> dict:
    mapping = maps.get(game_key(row), {})
    old_id = row.get("record_id")
    p = row.get("player")
    old_player = p.get("name") if isinstance(p, dict) else p if isinstance(p, str) else None
    seat = seat_for(row, maps, old_player)
    out = scrub_nested(row, mapping, root=True)
    out["game_uid"] = str(row.get("game_uid") or game_key(row))

    if filename == "cameo-ai-matches.jsonl":
        out["schema"] = 3
        player = dict(p or {})
        player.pop("name", None)
        player = scrub_nested(player, mapping)
        player["seat"] = seat
        out["player"] = player
        for rel_key in ("opponents", "allies"):
            out[rel_key] = [relation(x, mapping) for x in row.get(rel_key, []) if isinstance(x, dict)]
        descriptors = {seat: {"seat": seat, "faction": player.get("faction", ""), "home": player.get("home", "")}}
        for key in ("opponents", "allies"):
            for rel in out[key]:
                descriptors[rel["seat"]] = {k: rel.get(k, "") for k in ("seat", "faction", "home")}
        out["seats"] = [descriptors[k] for k in sorted(descriptors)]
        out["opponent_signatures"] = []
        out["record_id"] = f"{out['game_uid']}|{seat}"
    elif filename == "cameo-ai-situations.jsonl":
        out["schema"] = 3
        out["seat"] = seat
        out.pop("player", None)
        target = row.get("main_target")
        out["main_target"] = mapping.get(target, "") if isinstance(target, str) else ""
        enemies = []
        for enemy in row.get("enemies", []):
            if not isinstance(enemy, dict):
                continue
            item = scrub_nested(enemy, mapping)
            enemy_name = enemy.get("name")
            item["seat"] = mapping.get(str(enemy_name), "seat_1")
            item.pop("name", None)
            enemies.append(item)
        out["enemies"] = enemies
        out["record_id"] = f"{out['game_uid']}|{seat}|{row.get('tick', 0)}"
    elif filename == "cameo-ai-placements.jsonl":
        out["schema"] = 2
        out["seat"] = seat
        out.pop("player", None)
        tick = row.get("tick", row.get("placed_tick", 0))
        kind = str(row.get("kind") or "placement")
        actor = str(row.get("actor") or "")
        cell = str(row.get("cell") or "")
        out["record_id"] = f"{out['game_uid']}|{seat}|{tick}|{kind}|{actor}|{cell}"
    elif filename == "cameo-ai-missions.jsonl":
        out["schema"] = "mission-card/2"
        out["seat"] = seat
        out.pop("player", None)
        for key in ("mission_id", "attempt_id", "by", "reason"):
            if isinstance(out.get(key), str):
                out[key] = anonymous_text(out[key], mapping)
        if "attempt" in row:
            out["attempt_id"] = f"{out.get('mission_id', '')}|A{row['attempt']}"
    elif filename == "cameo-ai-engagements.jsonl":
        out["schema"] = "engagement/2"
        out["seat"] = seat
        out.pop("player", None)
        suffix = row.get("engagement_id")
        if suffix is None and isinstance(old_id, str):
            suffix = old_id.rsplit("|", 1)[-1]
        if not suffix:
            suffix = f"p{row.get('tick', 0)}"
        out["record_id"] = f"{out['game_uid']}|{seat}|{anonymous_text(str(suffix), mapping)}"
    return out


def migrate(support_dir: Path) -> dict:
    paths = [support_dir / name for name in LOG_NAMES if (support_dir / name).is_file()]
    parsed: dict[Path, list[dict]] = {}
    legacy_counts: dict[Path, int] = {}
    for path in paths:
        rows = []
        for line_no, line in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), 1):
            if not line.strip():
                continue
            try:
                row = json.loads(line)
            except json.JSONDecodeError as exc:
                raise ValueError(f"{path.name}:{line_no}: invalid JSON; no files changed") from exc
            if not isinstance(row, dict):
                raise ValueError(f"{path.name}:{line_no}: record is not an object; no files changed")
            rows.append(row)
        parsed[path] = rows
        legacy_counts[path] = sum(is_legacy(path.name, row) for row in rows)

    total = sum(legacy_counts.values())
    if total == 0:
        return {"files_rewritten": 0, "records_rewritten": 0, "backup": None, "per_file": {}}

    row_refs = [(path.name, row) for path, rows in parsed.items() for row in rows]
    maps = id_map(row_refs)
    rewritten: dict[Path, str] = {}
    for path, rows in parsed.items():
        if legacy_counts[path] == 0:
            continue
        transformed = [normalize_row(path.name, row, maps) if is_legacy(path.name, row) else row for row in rows]
        rewritten[path] = "".join(json.dumps(row, ensure_ascii=False, separators=(",", ":")) + "\n" for row in transformed)

    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    backup = support_dir / f"cameo-ai-legacy-backup-{stamp}"
    # A collision is unlikely but must never overwrite an earlier rollback copy.
    if backup.exists():
        raise FileExistsError(f"backup path already exists: {backup}")
    backup.mkdir()
    for path in paths:
        shutil.copy2(path, backup / path.name)
    (backup / "manifest.json").write_text(json.dumps({
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "warning": "Rollback copies contain the original player names and client identifiers.",
        "files": sorted(p.name for p in paths),
    }, indent=2) + "\n", encoding="utf-8")

    temporaries = []
    try:
        for path, contents in rewritten.items():
            with tempfile.NamedTemporaryFile("w", encoding="utf-8", newline="\n", dir=support_dir,
                                             prefix=path.name + ".", suffix=".tmp", delete=False) as temp:
                temp.write(contents)
                temporaries.append((Path(temp.name), path))
        for temp, destination in temporaries:
            os.replace(temp, destination)
    finally:
        for temp, _ in temporaries:
            if temp.exists():
                temp.unlink()
    return {
        "files_rewritten": len(rewritten),
        "records_rewritten": total,
        "backup": str(backup),
        "per_file": {p.name: legacy_counts[p] for p in rewritten},
    }


def main() -> int:
    default = Path(os.environ.get("APPDATA", Path.home())) / "OpenRA" / "Logs"
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--support-dir", type=Path, default=default,
                        help="OpenRA Logs directory (default: %%APPDATA%%/OpenRA/Logs)")
    args = parser.parse_args()
    result = migrate(args.support_dir)
    print(f"files rewritten: {result['files_rewritten']}; records rewritten: {result['records_rewritten']}")
    if result["backup"]:
        print(f"backup: {result['backup']}")
    for name, count in sorted(result["per_file"].items()):
        print(f"{name}: {count}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
