import importlib.util
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
    blocks = re.findall(r"Array.AsReadOnly\(new\[\]\s*\{(.*?)\}\);", source, re.S)
    fields = tuple(re.findall(r'"([a-z_0-9]+)"', blocks[list(audit.SCHEMAS).index(kind)]))
    assert len(fields) == (57 if kind == "map_features" else 61)
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
