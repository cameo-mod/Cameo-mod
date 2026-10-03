"""Tier-1 fitter tests (TIER1_FITTER_SPEC §9.1): cell math, shrinkage, sparse fallback, bounds,
determinism, seen-vs-truth separation, ledger hashing, timing/response tables, yaml shape."""
import pathlib
import sys

import pytest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "ai"))
import fit_engagement_priors as fp  # noqa: E402

PROFILES = {
    "rifle": {"cost": 100, "hp": 50, "armor": "None", "weapons": [("Bullet_Light", 10.0)]},
    "tank": {"cost": 800, "hp": 400, "armor": "Heavy", "weapons": [("CannonAP_Medium", 40.0)]},
    "gunpit": {"cost": 500, "hp": 150, "armor": "Wood", "weapons": [("CannonHE_Light", 30.0)]},
}
PRIORS = {
    "Bullet_Light": {"None": 100, "Heavy": 25, "Wood": 50},
    "CannonAP_Medium": {"None": 50, "Heavy": 100, "Wood": 100},
    "CannonHE_Light": {"None": 100, "Heavy": 75, "Wood": 25},
}


def _rec(kind="attack", skirmish=False, composition=True, **kw):
    r = {"record": "engagement", "kind": kind, "skirmish": skirmish,
         "faction": "td_gdi", "enemy_faction": "td_nod", "enemy_faction_public": True,
         "game_uid": "g1", "start_tick": 12000,
         "seen": {"start": {
             "own_committed_value": 800, "own_defence_value": 0,
             "enemy_unit_value": 100, "enemy_defence_value": 0,
             "predicted_own_surviving_permille": 500, "predicted_enemy_surviving_permille": 0}},
         "truth": {"start": {}},
         "outcome": {"own_lost_value": 400, "enemy_killed_value": 100},
         "response": {"response_ticks": 60, "army_dist_at_start_cells": 18},
         "tactics": {"suicide_index_milli": 500, "into_defences_value": 0}}
    if composition:
        r["seen"]["start"]["composition"] = {
            "own_units": {"tank": 1}, "own_defences": {},
            "enemy_units": {"rifle": 1}, "enemy_defences": {}}
    r.update(kw)
    return r


def _fit(recs):
    return fp.fit({"engagements": recs, "matches": []}, PROFILES, PRIORS)


def test_cell_moves_toward_measured_overperformance():
    # own tank killed all 100 enemy value while the predictor expected 0 destroyed
    # (enemy_surviving 1000 -> nothing expected) — measured >> expected, residual > 1000.
    r = _rec()
    r["seen"]["start"]["predicted_enemy_surviving_permille"] = 1000
    res = _fit([r] * 20)
    assert res["fitted"] == 20
    assert res["cells"][("CannonAP_Medium", "None")] > 1000


def test_cell_shrinks_to_prior_when_thin():
    res = _fit([_rec()])  # one record: SHRINK_VALUE pseudo-evidence dominates
    for v in res["cells"].values():
        assert 950 <= v <= 1050


def test_cell_clamped_to_bounds():
    r = _rec()
    r["outcome"]["enemy_killed_value"] = 10**9  # absurd evidence must still clamp
    res = _fit([r] * 50)
    assert all(fp.MIN_MILLI <= v <= fp.MAX_MILLI for v in res["cells"].values())
    assert res["cells"][("CannonAP_Medium", "None")] == fp.MAX_MILLI


def test_skirmish_and_pre_composition_records_skip():
    res = _fit([_rec(skirmish=True), _rec(composition=False), {"record": "posture"}])
    assert res["fitted"] == 0
    assert res["skipped"]["skirmish"] == 1 and res["skipped"]["no_composition"] == 1


def test_truth_composition_replaces_seen_for_victims():
    # seen says one rifle; truth says tanks. Victim armour share must come from truth.
    r = _rec()
    r["truth"]["start"]["composition"] = {"units": {"tank": 1}, "defences": {}}
    res = _fit([r])
    assert ("CannonAP_Medium", "Heavy") in res["cells"]
    assert ("CannonAP_Medium", "None") not in res["cells"]


def test_defence_state_collects_defence_fire_only():
    r = _rec()
    r["seen"]["start"]["composition"]["own_defences"] = {"gunpit": 1}
    res = _fit([r])
    assert "CannonHE_Light" in res["defence_state"]
    assert "CannonAP_Medium" not in res["defence_state"]


def test_one_record_cannot_dominate():
    # A single 1e6-value engagement among ordinary ones is capped at 4x the median.
    big = _rec()
    big["outcome"]["enemy_killed_value"] = 10**6
    small = _rec()
    small["outcome"]["enemy_killed_value"] = 50
    res = _fit([big] + [small] * 30)
    assert res["record_cap"] == 4 * (50 + 400)
    assert res["cells"][("CannonAP_Medium", "None")] < fp.MAX_MILLI


def test_deterministic_output():
    recs = [_rec(), _rec(kind="defend", start_tick=6000)]
    assert fp.to_yaml(_fit(recs), "h") == fp.to_yaml(_fit(recs), "h")  # byte-identical refit
    assert fp.fit({"engagements": recs, "matches": []}, PROFILES, PRIORS)["cells"] == \
        fp.fit({"engagements": recs, "matches": []}, PROFILES, PRIORS)["cells"]


def test_enemy_faction_fallback_from_match_log():
    r = _rec(kind="defend")
    del r["enemy_faction"]
    data = {"engagements": [r], "matches": [{"game_uid": "g1", "player": {"faction": "td_nod"}},
                                           {"game_uid": "g1", "player": {"faction": "td_gdi"}}]}
    res = fp.fit(data, PROFILES, PRIORS)
    assert res["attack_timing"]["td_nod"] == [12000, 12000, 12000]


def test_timing_response_and_suicide_tables():
    recs = [_rec(kind="defend", start_tick=t) for t in (6000, 12000, 18000)]
    recs += [_rec(kind="attack")]
    res = _fit(recs)
    assert res["attack_timing"]["td_nod"] == [6000, 12000, 18000]
    assert res["response"]["td_gdi"] == [60, 60, 18]
    assert res["suicide"][("td_gdi", "td_nod")] == 500  # thin pair -> pooled fallback


def test_yaml_shape_and_neutral_defaults():
    y = fp.to_yaml(_fit([_rec()]), "abc123")
    assert y.startswith("# GENERATED") and "BotEngagementPriors:" in y
    assert "\tSchema: 1" in y and "\tLedgerHash: abc123" in y
    assert "DeliveryArmour@CannonAP_Medium__x__None: " in y
    assert "-" not in "\n".join(l for l in y.splitlines() if not l.startswith("#"))  # rule 9


def test_ledger_hash_changes_with_the_ledgers(tmp_path):
    (tmp_path / "docs" / "balance").mkdir(parents=True)
    f = tmp_path / "docs" / "balance" / "td_gdi.json"
    f.write_text('{"ledger": {}, "sections": {}}')
    _, _, h1 = fp.load_profiles(tmp_path)
    f.write_text('{"ledger": {}, "sections": {"x": {}}}')
    _, _, h2 = fp.load_profiles(tmp_path)
    assert h1 != h2 and len(h1) == 64


def test_cycle_ticks_mirrors_csharp():
    assert fp.cycle_ticks(50, 1, []) == 50
    assert fp.cycle_ticks(50, 3, [10]) == 70  # 2 gaps of the single delay
    assert fp.cycle_ticks(50, 4, [10, 20]) == 100  # gaps 10,20,20 (last repeats)


def test_armour_share_and_quantiles():
    assert fp.armour_share({"tank": 2, "rifle": 1}, PROFILES) == {"Heavy": 1600, "None": 100}
    assert fp.quantiles([1, 2, 3, 4, 5], (50,)) == [3]
    assert fp.quantiles([], (50, 90)) == [None, None]
