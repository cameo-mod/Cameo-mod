"""veto_scorecard tests: detail parsing, card filtering, the counterfactual gap — synthetic records."""
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "ai"))
import veto_scorecard as vs  # noqa: E402


def _veto(reason="below_threshold", detail="", value=3000, bot="hard", player="p1", game="g1"):
    return {"type": "combat_veto", "event": "DENIED", "reason": reason, "detail": detail,
            "value": value, "bot": bot, "player": player, "game_uid": game, "tick": 100}


def _engagement(trade=100, skirmish=False, bot="hard", player="p1", game="g1", personality=""):
    return {"record": "engagement", "skirmish": skirmish, "bot_type": bot, "player": player,
            "game_uid": game, "personality": personality,
            "score": {"trade_milli": trade, "total_milli": trade}}


def _posture(player="p1", game="g1"):
    # the 250-tick army snapshot — no record fields beyond the shared header, no score/personality
    return {"record": "posture", "player": player, "game_uid": game, "tick": 250,
            "bot_type": "hard", "army_dist_at_start_cells": 3}


def test_parse_detail():
    assert vs.parse_detail("trade=-452;ratio=0.71;ownv=3200;foev=5100;defences=2") == {
        "trade": -452, "ratio": 0.71, "ownv": 3200, "foev": 5100, "defences": 2}
    assert vs.parse_detail("") == {}
    assert vs.parse_detail(None) == {}
    assert vs.parse_detail("own_min_speed=50;foe_max_speed=60;margin=100")["foe_max_speed"] == 60


def test_filters_only_denied_combat_veto():
    recs = [_veto(), {"type": "combat_veto", "event": "ISSUED", "reason": "below_threshold"},
            {"type": "capture", "event": "DENIED", "reason": "below_threshold"},
            _veto(reason="cant_outrun")]
    assert len(vs.veto_records(recs)) == 2


def test_summarise_gap_and_personality_join():
    missions = [
        _veto(detail="trade=-452", player="p1"),
        _veto(detail="trade=-400", player="p1"),
        _veto(reason="cant_outrun", detail="", player="p1"),
    ]
    engagements = [_engagement(trade=80, personality="rush"), _engagement(trade=120, personality="rush"),
                   _engagement(trade=-900, skirmish=True, personality="rush")]
    out = vs.summarise(missions, engagements)
    row = out["hard|rush"]
    assert row["veto_atk"] == 2
    assert row["veto_ret"] == 1
    assert row["vetoed_trade"] == -426  # mean of -452, -400
    assert row["taken_n"] == 2          # skirmish excluded
    assert row["taken_trade"] == 100
    assert row["gap"] == 100 - (-426)   # allowed fights scored better than blocked ones


def test_posture_rows_do_not_inflate_taken_or_pin_personality():
    # EL-0 writes one posture record per 250 ticks beside engagements (~2.2x the count on
    # real logs): unfiltered they counted toward taken_n, and a posture row seen first
    # pinned personality_of[...] to "" via setdefault.
    missions = [_veto(detail="trade=-400", player="p1")]
    engagements = [_posture()] * 5 + [_engagement(trade=100, personality="rush")]
    out = vs.summarise(missions, engagements)
    row = out["hard|rush"]  # personality join reached through the posture rows
    assert row["taken_n"] == 1
    assert row["taken_trade"] == 100


def test_missing_detail_and_unknown_personality():
    missions = [_veto(detail="", player="p9", game="g2")]
    out = vs.summarise(missions, [])
    row = out["hard|"]
    assert row["veto_atk"] == 1
    assert row["vetoed_trade"] is None  # pre-Detail cards count, but no predicted trade
    assert row["taken_trade"] is None
    assert row["gap"] is None


def test_multiple_bots_separate():
    missions = [_veto(detail="trade=-300", bot="hard"), _veto(detail="trade=-100", bot="classic")]
    out = vs.summarise(missions, [])
    assert set(out) == {"hard|", "classic|"}
