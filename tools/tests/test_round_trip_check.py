"""round_trip_check.py: a complete synthetic Logs dir passes; a dangling attempt fails the outcomes layer."""
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/ai"))

import round_trip_check  # noqa: E402


def write_jsonl(path, rows):
    path.write_text("".join(json.dumps(r) + "\n" for r in rows), encoding="utf-8")


def make_dir(tmp_path, last_state):
    logs = tmp_path / "batch" / "Logs"
    logs.mkdir(parents=True)
    (logs / "debug.log").write_text(
        "AI Multi0: LC5 ownership watchdog active: 2 squad manager(s)\n"
        "AI (1): LC1 leases: 3 held, claims 4\n"
        "AI Multi0: LEARNED priors: 1 pairs, 2 types\n", encoding="utf-8")
    match = {"game_uid": "g1", "player": {"name": "Multi0", "bot_type": "hard", "faction": "a", "outcome": "won"},
             "duration_ticks": 10, "ownership": {"checks": 5, "double_owner": 0},
             "order_gate": {"refused": 0, "conflicts": 0, "crossed": 0}}
    write_jsonl(logs / "cameo-ai-matches.jsonl", [match])
    write_jsonl(logs / "cameo-ai-situations.jsonl", [{"game_uid": "g1", "player": "Multi0", "tick": 750}])
    write_jsonl(logs / "cameo-ai-missions.jsonl", [
        {"game_uid": "g1", "record_kind": "mission", "mission_id": "raid:1", "event": "PUBLISHED", "tick": 1},
        {"game_uid": "g1", "record_kind": "mission", "mission_id": "raid:1", "event": "DORMANT", "tick": 2},
        {"game_uid": "g1", "record_kind": "attempt", "mission_id": "raid:1", "attempt": 1, "state": "COMMITTED", "tick": 3},
        {"game_uid": "g1", "record_kind": "attempt", "mission_id": "raid:1", "attempt": 1, "state": last_state, "tick": 9},
    ])
    return tmp_path / "batch"


def states(rows):
    return {layer: state for layer, state, _ in rows}


def test_complete_logs_pass(tmp_path):
    d = make_dir(tmp_path, "SUCCESS")
    s = states(round_trip_check.check([d]))
    assert set(s.values()) == {"PASS"}, s
    assert round_trip_check.main([str(d)]) == 0


def test_dangling_attempt_fails(tmp_path, capsys):
    d = make_dir(tmp_path, "PROGRESSING")
    # The outcomes layer counts an open attempt as dangling only when the executor went quiet
    # >5000 ticks before the last mission record — a PROGRESSING row 1 tick from match end is
    # "in flight", not truncation. A later unrelated record makes the attempt go quiet.
    logs = d / "Logs"
    rows = (logs / "cameo-ai-missions.jsonl").read_text(encoding="utf-8").splitlines()
    rows.append(json.dumps({"game_uid": "g1", "record_kind": "mission", "mission_id": "raid:2",
                            "event": "PUBLISHED", "tick": 6010}))
    (logs / "cameo-ai-missions.jsonl").write_text("\n".join(rows) + "\n", encoding="utf-8")
    s = states(round_trip_check.check([d]))
    assert s["outcomes"] == "FAIL"
    assert round_trip_check.main([str(d)]) == 1
    assert "1 dangling" in capsys.readouterr().out


def test_engagements_row(tmp_path):
    # EL-0 log health: scored+closed engagement + posture coverage = PASS; an unscored or
    # unclosed record downgrades to WARN; absent file is PASS (pre-EL-0 build).
    d = make_dir(tmp_path, "SUCCESS")
    logs = d / "Logs"
    s = states(round_trip_check.check([d]))
    assert s["engagements"] == "PASS", "no cameo-ai-engagements.jsonl is pre-EL-0, not a failure"

    write_jsonl(logs / "cameo-ai-engagements.jsonl", [
        {"game_uid": "g1", "record": "engagement", "record_id": "g1|Multi0|e1",
         "close_reason": "resolved", "score": {"total_milli": 120}},
        {"game_uid": "g1", "record": "posture", "record_id": "g1|Multi0|p1"},
    ])
    s = states(round_trip_check.check([d]))
    assert s["engagements"] == "PASS"

    write_jsonl(logs / "cameo-ai-engagements.jsonl", [
        {"game_uid": "g1", "record": "engagement", "record_id": "g1|Multi0|e1",
         "score": {"total_milli": 120}},
    ])
    s = states(round_trip_check.check([d]))
    assert s["engagements"] == "WARN", "missing close_reason and missing posture coverage"
