"""round_trip_check.py: a complete synthetic Logs dir passes; a dangling attempt fails the outcomes layer."""
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/ai"))

import round_trip_check  # noqa: E402


def write_jsonl(path, rows):
    path.write_text("".join(json.dumps(r) + "\n" for r in rows), encoding="utf-8")


def make_dir(tmp_path, last_state, end_tick=9):
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
        {"game_uid": "g1", "record_kind": "mission", "mission_id": "raid:1", "event": "DORMANT", "tick": end_tick},
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
    # The outcomes layer gives an open attempt a 5000-tick grace before it
    # counts as dangling, so the match must run past the attempt's last record.
    d = make_dir(tmp_path, "PROGRESSING", end_tick=6000)
    s = states(round_trip_check.check([d]))
    assert s["outcomes"] == "FAIL"
    assert round_trip_check.main([str(d)]) == 1
    assert "1 dangling" in capsys.readouterr().out


def test_missing_but_present_detects_regression(tmp_path, monkeypatch):
    # M9: a configured learned path logged "missing" while it physically exists in the mod
    # tree means the load-gate's Exists() check regressed — this is the exact asymmetry M9 hit.
    monkeypatch.setattr(round_trip_check, "MOD_ROOT", tmp_path)
    learned = tmp_path / "ai" / "learned"
    learned.mkdir(parents=True)
    (learned / "plan_bandits.yaml").write_text("BotPlanBandits:\n", encoding="utf-8")

    present = round_trip_check.missing_but_present(
        ["AI Multi0: plan-bandit learned: cameo|ai/learned/plan_bandits.yaml missing, priors only"])
    assert present == ["cameo|ai/learned/plan_bandits.yaml"]

    # A path that is genuinely absent from the tree is a normal "no learned data yet" state,
    # not a regression — must not be flagged.
    absent = round_trip_check.missing_but_present(
        ["AI Multi0: TIER1 priors: none (cameo|ai/learned/engagement_priors.yaml missing), neutral"])
    assert absent == []


def test_learned_paths_row_fails_when_logged_missing_but_present(tmp_path, monkeypatch):
    monkeypatch.setattr(round_trip_check, "MOD_ROOT", tmp_path)
    learned = tmp_path / "ai" / "learned"
    learned.mkdir(parents=True)
    (learned / "plan_bandits.yaml").write_text("BotPlanBandits:\n", encoding="utf-8")

    d = make_dir(tmp_path / "match", "SUCCESS")
    debug_log = d / "Logs" / "debug.log"
    debug_log.write_text(
        debug_log.read_text(encoding="utf-8")
        + "AI Multi0: plan-bandit learned: cameo|ai/learned/plan_bandits.yaml missing, priors only\n",
        encoding="utf-8")

    s = states(round_trip_check.check([d]))
    assert s["learned paths"] == "FAIL"
    assert round_trip_check.main([str(d)]) == 1
