"""tune_build_order --coverage: per-(personality, faction, side) funnel of match records seen vs scored,
drops by reason vs MIN_MATCHES, and engagement-verdict (EL) coverage. Synthetic logs only."""

import json
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "ai"))
import tune_build_order as tbo  # noqa: E402


def _mkdir(root, arm):
    logs = root / arm / "Logs"
    logs.mkdir(parents=True)
    return logs


def _match(uid, name, outcome="won", ticks=27000, personality="rush", faction="td_gdi",
           opponents=None, allies=None):
    return {"game_uid": uid, "duration_ticks": ticks,
            "allies": allies if allies is not None else [],
            "opponents": opponents if opponents is not None else [{"faction": "td_nod"}],
            "player": ({"name": name, "faction": faction, "outcome": outcome,
                        "personality": personality} if personality else
                       {"name": name, "faction": faction, "outcome": outcome}),
            "stats": {"kills_cost": 9000, "deaths_cost": 1000}}


def _situation(uid, name, tick, personality="rush", opening="eco", bo=True):
    own = {"build_order": {"personality": personality, "opening": opening}} if bo else {}
    return {"game_uid": uid, "player": name, "tick": tick, "own": own}


def _eng(uid, name, total=None, skirmish=False):
    r = {"game_uid": uid, "player": name}
    if total is not None:
        r["score"] = {"total_milli": total}
    if skirmish:
        r["skirmish"] = True
    return r


def _write(logs, matches, situations, engagements):
    (logs / "cameo-ai-matches.jsonl").write_text("\n".join(json.dumps(r) for r in matches), encoding="utf-8")
    (logs / "cameo-ai-situations.jsonl").write_text("\n".join(json.dumps(r) for r in situations), encoding="utf-8")
    (logs / "cameo-ai-engagements.jsonl").write_text("\n".join(json.dumps(r) for r in engagements), encoding="utf-8")


def test_funnel_counts_every_drop_reason_and_scored(tmp_path):
    logs = _mkdir(tmp_path, "bo__rush__td_gdi__spsa__k0__plus")
    matches = [
        _match("g1", "Multi0"),                                  # scored (usable EL below)
        _match("g2", "Multi0", outcome="undecided"),             # drop: outcome
        _match("g3", "Multi0", ticks=500),                       # drop: duration gate
        _match("g4", "Multi0"),                                  # drop: no build_order (no situation for g4)
        _match("g5", "Multi0", allies=[{"name": "Multi2"}]),     # drop: not 1v1
        _match("g6", "Multi0", personality=None),                # drop: no personality (bo has none either)
    ]
    situations = [_situation("g1", "Multi0", 100), _situation("g2", "Multi0", 100),
                  _situation("g3", "Multi0", 100), _situation("g5", "Multi0", 100),
                  _situation("g6", "Multi0", 100, personality=None)]
    engagements = [_eng("g1", "Multi0", 400)]
    _write(logs, matches, situations, engagements)

    rows, cov = tbo.load_matches_detailed([tmp_path / "bo__rush__td_gdi__spsa__k0__plus"])
    assert len(rows) == 1
    cells = cov
    # the no-personality drop lands in its own '?' cell; the rest share rush/td_gdi/plus
    main = cells[("rush", "td_gdi", "plus")]
    assert main["seen"] == 5 and main["scored"] == 1
    assert main["drop_outcome_undecided"] == 1 and main["drop_duration_gate"] == 1
    assert main["drop_no_build_order"] == 1 and main["drop_not_1v1"] == 1
    assert main["el_ok"] == 1
    missing = cells[("?", "td_gdi", "plus")]
    assert missing["drop_no_personality"] == 1


def test_sides_and_el_buckets(tmp_path):
    for arm, evs in (("bo__rush__td_gdi__spsa__k0__minus", "skirmish"),
                     ("bo__rush__td_gdi__spsa__k1__plus", "none"),
                     ("bo__rush__td_gdi__tempo__up100", "inflight"),
                     ("plain_td_gdi", "ok")):
        logs = _mkdir(tmp_path, arm)
        _write(logs, [_match("g1", "Multi0")], [_situation("g1", "Multi0", 100)],
               {"skirmish": [_eng("g1", "Multi0", 900, skirmish=True)],
                "none": [],
                "inflight": [_eng("g1", "Multi0")],          # in-flight record, no verdict
                "ok": [_eng("g1", "Multi0", 100)]}[evs])

    _, cov = tbo.load_matches_detailed([tmp_path / a for a in
                                        ("bo__rush__td_gdi__spsa__k0__minus",
                                         "bo__rush__td_gdi__spsa__k1__plus",
                                         "bo__rush__td_gdi__tempo__up100", "plain_td_gdi")])
    assert cov[("rush", "td_gdi", "minus")]["el_skirmish_only"] == 1
    assert cov[("rush", "td_gdi", "plus")]["el_no_score"] == 1
    assert cov[("rush", "td_gdi", "arm")]["el_unscored"] == 1
    assert cov[("rush", "td_gdi", "base")]["el_ok"] == 1


def test_report_is_ordinal_sorted_and_marks_shortfall(tmp_path):
    for arm in ("bo__turtle__td_nod__spsa__k0__plus", "bo__rush__td_gdi__base"):
        _write(_mkdir(tmp_path, arm), [_match("g1", "Multi0", personality="turtle" if "turtle" in arm else "rush")],
               [_situation("g1", "Multi0", 100, personality="turtle" if "turtle" in arm else "rush")],
               [_eng("g1", "Multi0", 0)])
    _, cov = tbo.load_matches_detailed([tmp_path / "bo__rush__td_gdi__base",
                                        tmp_path / "bo__turtle__td_nod__spsa__k0__plus"])
    text = tbo.coverage_report(cov, min_matches=20)
    lines = text.splitlines()
    assert lines[0].startswith("coverage: 2 match record(s); MIN_MATCHES=20")
    assert lines[2].startswith("rush") and lines[3].startswith("turtle")  # ordinal sort
    assert " 19" in lines[3]  # short column: 20 - 1 scored


def test_json_shape_and_load_matches_unchanged(tmp_path):
    logs = _mkdir(tmp_path, "bo__rush__td_gdi__base")
    _write(logs, [_match("g1", "Multi0")], [_situation("g1", "Multi0", 100)], [_eng("g1", "Multi0", 250)])
    rows, cov = tbo.load_matches_detailed([tmp_path / "bo__rush__td_gdi__base"])
    assert rows == tbo.load_matches([tmp_path / "bo__rush__td_gdi__base"])  # wrapper is byte-identical
    data = tbo.coverage_json(cov, min_matches=20)
    assert data["min_matches"] == 20 and len(data["cells"]) == 1
    cell = data["cells"][0]
    assert (cell["personality"], cell["faction"], cell["side"]) == ("rush", "td_gdi", "base")
    assert cell["seen"] == 1 and cell["scored"] == 1 and cell["short_of_min"] == 19
    json.dumps(data)  # must serialize


def test_main_coverage_flag_prints_table_and_exits_zero(tmp_path, monkeypatch, capsys):
    _write(_mkdir(tmp_path, "bo__rush__td_gdi__base"), [_match("g1", "Multi0")],
           [_situation("g1", "Multi0", 100)], [_eng("g1", "Multi0", 250)])
    monkeypatch.setattr(sys, "argv",
                        ["tune_build_order.py", str(tmp_path / "bo__rush__td_gdi__base"), "--coverage"])
    assert tbo.main() == 0
    out = capsys.readouterr().out
    assert "coverage: 1 match record(s)" in out and "rush" in out and "base" in out


def test_main_coverage_json_flag(tmp_path, monkeypatch, capsys):
    _write(_mkdir(tmp_path, "bo__rush__td_gdi__base"), [_match("g1", "Multi0")],
           [_situation("g1", "Multi0", 100)], [])
    monkeypatch.setattr(sys, "argv",
                        ["tune_build_order.py", str(tmp_path / "bo__rush__td_gdi__base"),
                         "--coverage", "--json"])
    assert tbo.main() == 0
    data = json.loads(capsys.readouterr().out)
    assert data["cells"][0]["el_no_score"] == 1 and data["cells"][0]["side"] == "base"
