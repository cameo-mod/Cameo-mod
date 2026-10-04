"""tune_plan_bandits tests (D4, orders 2026-10-03): scope chain, Welford, decay, --armed-only,
idempotent Processed, and malformed-row handling — all on synthetic records."""
import pathlib
import sys

import pytest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "ai"))
import tune_plan_bandits as tb  # noqa: E402


def _engagement(personality_arm="rush", plan_arm="boom", scope="td_gdi__vs__td_nod",
                armed="plan_bandits", total=100, game="g1", player="p1", record="engagement"):
    bandit = {"scope": scope, "personality_arm": personality_arm, "plan_arm": plan_arm, "armed": armed}
    return {"record": record, "game_uid": game, "player": player, "bandit": bandit,
            "score": {"trade_milli": total, "total_milli": total}}


# ---------------------------------------------------------------- scope + stats

def test_scope_chain_matchup_and_bare():
    assert tb.scope_chain("td_gdi__vs__td_nod") == [
        "td_gdi__vs__td_nod", "td_gdi", "family_td", "any"]
    assert tb.scope_chain("td_gdi") == ["td_gdi"]
    assert tb.scope_chain("any") == ["any"]


def test_welford_mean_and_m2():
    stats = [0, 0.0, 0.0]
    for x in (10.0, 20.0, 30.0):
        tb.welford_add(stats, x)
    assert stats[0] == 3
    assert stats[1] == pytest.approx(20.0)
    assert stats[2] == pytest.approx(200.0)  # sum of squared deviations


def test_decay_shrinks_and_drops():
    stats = {("P", "any"): {"a": [10, 100.0, 50.0], "b": [1, 5.0, 0.0]}}
    tb.decay(stats, 0.4)
    assert stats[("P", "any")]["a"] == [4, 40.0, 8.0]  # n*r, mean*r, m2*r^2
    assert "b" not in stats[("P", "any")], "n decaying to 0 drops the arm"
    tb.decay(stats, 1.0)
    assert stats[("P", "any")]["a"] == [4, 40.0, 8.0], "factor >= 1 is a no-op"


# ---------------------------------------------------------------- update

def test_update_folds_every_chain_level():
    learned = tb.empty_learned()
    out = tb.update([_engagement(total=120)], learned, decay_factor=1.0)
    assert out == {"folded": 1, "skipped": 0, "filtered": 0}
    for scope in ("td_gdi__vs__td_nod", "td_gdi", "family_td", "any"):
        assert learned["stats"][("Personality", scope)]["rush"] == [1, 120.0, 0.0]
        assert learned["stats"][("Plan", scope)]["boom"] == [1, 120.0, 0.0]


def test_update_empty_arm_contributes_nothing():
    learned = tb.empty_learned()
    tb.update([_engagement(personality_arm="", plan_arm="boom")], learned, decay_factor=1.0)
    assert {b for (b, _s) in learned["stats"]} == {"Plan"}, "an undrawn arm fabricates no evidence"
    tb.update([_engagement(personality_arm="rush", plan_arm="", game="g2")], learned, decay_factor=1.0)
    assert ("Personality", "any") in learned["stats"]
    assert list(learned["stats"][("Plan", "any")]) == ["boom"], \
        "the plan-less record adds nothing to the Plan bandit"


def test_update_idempotent_processed():
    rows = [_engagement(total=100)]
    learned = tb.empty_learned()
    tb.update(rows, learned, decay_factor=1.0)
    out = tb.update(rows, learned, decay_factor=1.0)
    assert out == {"folded": 0, "skipped": 0, "filtered": 0}
    assert learned["stats"][("Personality", "any")]["rush"][0] == 1, "same game folds once"


def test_update_armed_only_holds_mismatched():
    rows = [_engagement(armed="plan_bandits", game="g1"),
            _engagement(armed="combatveto+plan_bandits", game="g2")]
    learned = tb.empty_learned()
    out = tb.update(rows, learned, decay_factor=1.0, armed_only="plan_bandits")
    assert out == {"folded": 1, "skipped": 0, "filtered": 1}
    assert tb.game_key(rows[1]) not in learned["processed"], "held row stays unprocessed"
    out = tb.update(rows, learned, decay_factor=1.0, armed_only="combatveto+plan_bandits")
    assert out["folded"] == 1, "a later differently-filtered pass still sees the held row"


def test_update_armed_only_missing_armed_counts_as_none():
    row = _engagement()
    del row["bandit"]["armed"]
    learned = tb.empty_learned()
    out = tb.update([row], learned, decay_factor=1.0, armed_only="none")
    assert out["folded"] == 1


def test_update_unattributed_marked_processed_not_folded():
    row = _engagement()
    del row["bandit"]
    learned = tb.empty_learned()
    out = tb.update([row], learned, decay_factor=1.0)
    assert out == {"folded": 0, "skipped": 1, "filtered": 0}
    assert tb.game_key(row) in learned["processed"], "skipped rows don't re-scan next pass"
    assert learned["stats"] == {}


def test_update_non_engagement_ignored():
    learned = tb.empty_learned()
    row = _engagement(record="combat_veto")
    out = tb.update([row], learned, decay_factor=1.0)
    assert out["folded"] == 0 and tb.game_key(row) not in learned["processed"]


# ---------------------------------------------------------------- malformed rows

def test_update_missing_score_and_scope_defaults():
    row = _engagement()
    del row["score"], row["bandit"]["scope"]
    learned = tb.empty_learned()
    out = tb.update([row], learned, decay_factor=1.0)
    assert out["folded"] == 1
    assert learned["stats"][("Personality", "any")]["rush"] == [1, 0.0, 0.0], \
        "missing score folds total 0 under scope 'any'"


def test_update_missing_ids_fail_loud():
    row = _engagement()
    del row["game_uid"]
    with pytest.raises(KeyError):
        tb.update([row], tb.empty_learned(), decay_factor=1.0)


def test_game_key_stable_and_distinct():
    a, b = _engagement(), _engagement(game="g2")
    assert tb.game_key(a) == tb.game_key(dict(a))
    assert tb.game_key(a) != tb.game_key(b)
    assert tb.game_key(a) != tb.game_key(_engagement(player="p2"))


# ---------------------------------------------------------------- learned file

def test_parse_render_roundtrip():
    learned = tb.empty_learned()
    learned["stats"].setdefault(("Personality", "any"), {})["rush"] = [3, 12.5, 7.0]
    learned["stats"].setdefault(("Plan", "td_gdi"), {})["boom"] = [1, -4.0, 0.0]
    learned["processed"] = ["abc", "def", "abc"]
    parsed = tb.parse_learned(tb.render_learned(learned))
    assert parsed["stats"] == learned["stats"]
    assert parsed["processed"] == ["abc", "def"], "render dedupes and sorts; parse reads it back"


def test_parse_learned_ignores_noise():
    text = "# comment\nOther:\n\tX@y:\n\t\ta: 1 2 3\nBotPlanBandits:\n\tPlan@any:\n\t\tboom: 2 1.5 0\n"
    learned = tb.parse_learned(text)
    assert learned["stats"] == {("Plan", "any"): {"boom": [2, 1.5, 0.0]}}
