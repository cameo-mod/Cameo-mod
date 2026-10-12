from pathlib import Path
import re
r=Path(r'C:/cameo-wt/codex-context-providers')
s=(r/'OpenRA.Mods.CA/Traits/BotModules/IBotMapFeatures.cs').read_text()
blocks=re.findall(r'Array.AsReadOnly\(new\[\]\s*\{(.*?)\}\);',s,re.S)
vocabs=[tuple(re.findall(r'"([a-z_0-9]+)"', b)) for b in blocks]
p=r/'tools/ai/audit_context_features.py'
p.write_text('''#!/usr/bin/env python3
"""Grammar-closed validator for anonymous context JSONL, before ingestion.

It accepts exactly the v1 wire schema, including missing flags. This is the
context-record grammar only; the integrated AI-log audit must call it for these
record types after the reviewed P0 grammar lands. It never guesses which strings
look like names and never accepts unknown record shapes.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path

MAX_RECORD_BYTES = 16 * 1024
MAX_LEARNED_BYTES = 12 * 1024 * 1024
SCHEMAS = {
'''+''.join(f'    {kind!r}: {v!r},\n' for kind,v in zip(['map_features','opponent_features'],vocabs))+'''}


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("duplicate JSON key")
        result[key] = value
    return result


def validate_line(line: str) -> dict:
    if len(line.encode("utf-8")) > MAX_RECORD_BYTES:
        raise ValueError("context record exceeds wire budget")
    record = json.loads(line, object_pairs_hook=unique_object)
    if type(record) is not dict or set(record) != {"record", "schema", "features"}:
        raise ValueError("unknown context record grammar")
    kind = record["record"]
    if type(kind) is not str or kind not in SCHEMAS or type(record["schema"]) is not int or record["schema"] != 1:
        raise ValueError("unknown context schema")
    fields = record["features"]
    if type(fields) is not dict or set(fields) != set(SCHEMAS[kind]):
        raise ValueError("context fields must match the closed vocabulary")
    for measurement in fields.values():
        if type(measurement) is not dict or set(measurement) != {"value", "present", "confidence_permille"}:
            raise ValueError("unknown feature measurement grammar")
        value, present, confidence = (measurement[k] for k in ("value", "present", "confidence_permille"))
        if type(value) is not int or not -(2**31) <= value < 2**31 or type(present) is not bool:
            raise ValueError("invalid integer measurement or missing bit")
        if type(confidence) is not int or not 0 <= confidence <= 1000:
            raise ValueError("invalid integer confidence")
        if not present and (value != -1 or confidence != 0):
            raise ValueError("missing measurement carries data")
    return record


def check_learned_size(root: Path) -> int:
    # All files count, regardless of suffix. A model cannot escape the cap by renaming.
    total = sum(p.stat().st_size for p in sorted(root.rglob("*")) if p.is_file()) if root.exists() else 0
    if total > MAX_LEARNED_BYTES:
        raise ValueError("learned artifacts exceed 12 MiB")
    return total


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("logs", nargs="*", type=Path)
    parser.add_argument("--learned-root", type=Path)
    args = parser.parse_args()
    records = 0
    for path in sorted(args.logs):
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if not line.strip():
                continue
            try:
                validate_line(line)
            except (ValueError, TypeError) as error:
                parser.exit(1, f"{path}:{number}: {error}\\n")
            records += 1
    try:
        size = check_learned_size(args.learned_root) if args.learned_root else 0
    except ValueError as error:
        parser.exit(1, f"{error}\\n")
    print(f"CONTEXT_AUDIT=PASS records={records} learned_bytes={size}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
''')
(r/'tools/tests/test_context_features.py').write_text('''import importlib.util
import json
from pathlib import Path
import re

import pytest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("context_audit", ROOT / "tools/ai/audit_context_features.py")
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)


def record(kind="map_features"):
    return {"record": kind, "schema": 1, "features": {
        key: {"value": -1, "present": False, "confidence_permille": 0}
        for key in audit.SCHEMAS[kind]
    }}


@pytest.mark.parametrize("kind", audit.SCHEMAS)
def test_runtime_and_audit_vocabularies_match(kind):
    source = (ROOT / "OpenRA.Mods.CA/Traits/BotModules/IBotMapFeatures.cs").read_text()
    blocks = re.findall(r"Array.AsReadOnly\\(new\\[\\]\\s*\\{(.*?)\\}\\);", source, re.S)
    fields = tuple(re.findall(r'"([a-z_0-9]+)"', blocks[list(audit.SCHEMAS).index(kind)]))
    assert fields == audit.SCHEMAS[kind]
    line = json.dumps(record(kind), separators=(",", ":"))
    assert audit.validate_line(line) == record(kind)
    assert len(line.encode()) < audit.MAX_RECORD_BYTES


@pytest.mark.parametrize("key", ["name", "map_uid", "seat_1", "client_id", "features_extra"])
def test_unknown_root_keys_are_rejected(key):
    row = record()
    row[key] = "private identity"
    with pytest.raises(ValueError):
        audit.validate_line(json.dumps(row))


@pytest.mark.parametrize("target", ["features", "measurement"])
def test_nested_identity_has_no_grammar_slot(target):
    row = record()
    if target == "features":
        row["features"]["player_name"] = {"value": 1, "present": True, "confidence_permille": 1000}
    else:
        row["features"]["width_cells"]["identity"] = "private name"
    with pytest.raises(ValueError):
        audit.validate_line(json.dumps(row))


@pytest.mark.parametrize("bad", [True, 1.0, "1", 2**31, -(2**31)-1, None, {"id": 1}])
def test_measurements_are_signed_integers_only(bad):
    row = record()
    row["features"]["width_cells"] = {"value": bad, "present": True, "confidence_permille": 1000}
    with pytest.raises(ValueError):
        audit.validate_line(json.dumps(row))


def test_duplicate_json_names_and_unknown_schema_are_rejected():
    with pytest.raises(ValueError):
        audit.validate_line('{"record":"map_features","record":"opponent_features"}')
    row = record()
    row["schema"] = 999
    with pytest.raises(ValueError):
        audit.validate_line(json.dumps(row))


def test_missing_fields_cannot_smuggle_stale_values():
    row = record()
    row["features"]["width_cells"]["value"] = 99
    with pytest.raises(ValueError):
        audit.validate_line(json.dumps(row))


def test_entire_learned_tree_counts_towards_size_cap(tmp_path):
    assert audit.check_learned_size(tmp_path) == 0
    (tmp_path / "renamed.bin").write_bytes(b"data")
    assert audit.check_learned_size(tmp_path) == 4
    with (tmp_path / "huge.anything").open("wb") as f:
        f.truncate(audit.MAX_LEARNED_BYTES)
    with pytest.raises(ValueError):
        audit.check_learned_size(tmp_path)
''')

