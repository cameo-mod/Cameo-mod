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
         # Tier4's PRIORS-CARRY log field: the record's own resolved stats. The fixtures
         # stake the same percents as PRIORS (today's table) -> cell decay = 1.
         "balance": {"fingerprint": "fixture",
                     "versus": {f"{d}|{a}": p for d, vs in PRIORS.items() for a, p in vs.items()}},
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


def _rec3x():
    """A record where BOTH directions observe ~3x the expected losses (obs = 3*exp):
    the base model is uniformly 3x pessimistic, not a per-cell effect."""
    r = _rec()
    r["seen"]["start"]["predicted_enemy_surviving_permille"] = 700   # exp 3000 vs obs 9000
    r["seen"]["start"]["predicted_own_surviving_permille"] = 875     # exp 100 vs obs 300
    r["truth"]["start"]["enemy_unit_value"] = 10000
    r["truth"]["enemy_loss_value"] = 9000
    r["outcome"]["own_lost_unit_value"] = 300
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
    # one genuinely extreme cell must still clamp even though the pooled scale absorbs
    # part of it; obs is census-bounded so the mass comes through truth fields.
    hot = _rec3x()
    hot["seen"]["start"]["composition"]["own_units"] = {"rifle": 1}   # -> Bullet_Light cell
    hot["truth"]["start"]["enemy_unit_value"] = 100000
    hot["truth"]["enemy_loss_value"] = 100000
    hot["seen"]["start"]["predicted_enemy_surviving_permille"] = 990  # exp 1000, obs 100000
    res = _fit([_rec3x()] * 40 + [hot])
    assert all(fp.MIN_MILLI <= v <= fp.MAX_MILLI for v in res["cells"].values())
    assert res["cells"][("Bullet_Light", "None")] == fp.MAX_MILLI


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
    # obs is census-bounded, so the huge value must exist in the truth start census.
    big = _rec()
    big["truth"]["start"]["enemy_unit_value"] = 10**6
    big["truth"]["enemy_loss_value"] = 10**6
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


def _write_ledger(root, name, actor):
    import json
    (root / "docs" / "balance").mkdir(parents=True, exist_ok=True)
    (root / "docs" / "balance" / name).write_text(json.dumps(
        {"ledger": {}, "sections": {"g": {actor["name"]: actor}}}), encoding="utf-8")


def _arm(damage=12000, burst=1, reloaddelay=30, tag="CannonAP_Medium", **kw):
    a = {"damage_warheads": [{"damage": str(damage), "tag": tag}],
         "burst": str(burst), "reloaddelay": str(reloaddelay)}
    a.update(kw)
    return a


def test_firepower_modifier_scales_priced_dpt(tmp_path):
    # armament_firepower(unit, arm): modifier/100 per applicable entry, armament_name-scoped.
    actor = {"name": "buffed", "armaments": [_arm(tag="Bullet_Light"), _arm(tag="CannonHE_Light", armament_name="secondary")],
             "resolved_firepower_modifiers": [{"modifier": 50, "types": []},
                                            {"modifier": 200, "types": ["secondary"]}]}
    _write_ledger(tmp_path, "f.json", actor)
    profiles, _, _ = fp.load_profiles(tmp_path)
    w = dict(profiles["buffed"]["weapons"])
    raw = 12000 / 30
    assert w["Bullet_Light"] == pytest.approx(raw * 0.5)         # global 50 applies
    assert w["CannonHE_Light"] == pytest.approx(raw * 0.5 * 2)   # global 50 + secondary 200


def test_firepower_modifier_does_not_move_the_cell():
    # two identical units, one priced with modifier 50: same record, same outcome -> the
    # delivery cell must read identically; the modifier scales BOTH sides of the ratio.
    def rec_with(u):
        r = _rec()
        r["seen"]["start"]["composition"]["own_units"] = {u: 1}
        return r

    buffed = dict(PROFILES, tankb={"cost": 800, "hp": 400, "armor": "Heavy",
                                   "weapons": [("CannonAP_Medium", 20.0)]})  # dpt halved at pricing
    unbuffed = dict(PROFILES, tankb={"cost": 800, "hp": 400, "armor": "Heavy",
                                     "weapons": [("CannonAP_Medium", 40.0)]})
    cells_b = fp.fit({"engagements": [rec_with("tankb")] * 12, "matches": []}, buffed, PRIORS)["cells"]
    cells_u = fp.fit({"engagements": [rec_with("tankb")] * 12, "matches": []}, unbuffed, PRIORS)["cells"]
    assert cells_b == cells_u


def test_halved_dpt_rescales_mixed_force_attribution():
    # Within one record obs and exp share the dpt weight (it cancels). Where pricing matters
    # is pooled across records: two fights, same expected, different overperformance. Halving
    # D2's priced dpt pushes more of both records' credit mass onto D1's cell.
    prof = dict(PROFILES)
    prof["twin1"] = {"cost": 400, "hp": 200, "armor": "Heavy", "weapons": [("CannonAP_Medium", 40.0)]}

    def rec_pair(dpt2):
        p2 = dict(prof, twin2={"cost": 400, "hp": 200, "armor": "Heavy",
                               "weapons": [("CannonHE_Light", dpt2)]})
        recs = []
        for killed in (100, 300):
            r = _rec()
            r["seen"]["start"]["composition"]["own_units"] = {"twin1": 1, "twin2": 1}
            r["seen"]["start"]["predicted_enemy_surviving_permille"] = 700  # expected 90
            r["truth"]["start"]["enemy_unit_value"] = 300   # obs is census-bounded:
            r["truth"]["enemy_loss_value"] = killed         # real start-census deaths
            recs.append(r)
        return fp.fit({"engagements": recs * 10, "matches": []}, p2, PRIORS)["cells"]

    key = ("CannonAP_Medium", "None")
    assert rec_pair(20.0)[key] > rec_pair(40.0)[key]


def test_empty_versus_prior_tags_are_excluded():
    priors = dict(PRIORS)
    del priors["CannonAP_Medium"]  # tank's only delivery now has no resolved Versus table
    res = fp.fit({"engagements": [_rec()] * 10, "matches": []}, PROFILES, priors)
    assert all(k[0] != "CannonAP_Medium" for k in res["cells"])


def test_priorpct_written_next_to_every_cell():
    # F1(b): every DeliveryArmour@ line is immediately followed by its PriorPct@ twin;
    # DefenceState@ carries none (pooled across victim armours - no single resolved prior).
    r = _rec()
    r["seen"]["start"]["composition"]["own_defences"] = {"gunpit": 1}
    res = _fit([r])
    assert res["defence_state"]  # fixture actually emits DefenceState lines
    lines = fp.to_yaml(res, "h").splitlines()
    cell_lines = [l for l in lines if l.startswith("\tDeliveryArmour@")]
    assert len(cell_lines) == len(res["cells"]) > 0
    prior_lines = [l for l in lines if l.startswith("\tPriorPct@")]
    assert len(prior_lines) == len(cell_lines)
    for i, l in enumerate(lines):
        if l.startswith("\tDeliveryArmour@"):
            key = l.split("@", 1)[1].split(":")[0]
            assert lines[i + 1].startswith(f"\tPriorPct@{key}:")
        if l.startswith("\tDefenceState@"):
            assert not lines[i + 1].startswith("\tPriorPct@")


def test_priorpct_equals_resolved_versus_prior():
    res = _fit([_rec()])
    assert res["cell_prior"][("CannonAP_Medium", "None")] == 50   # PRIORS["CannonAP_Medium"]["None"]
    # an armour row the tag's table does not carry -> the default-100 the weight consumed
    priors = {t: dict(v) for t, v in PRIORS.items()}
    del priors["CannonAP_Medium"]["None"]
    res2 = fp.fit({"engagements": [_rec()], "matches": []}, PROFILES, priors)
    assert res2["cell_prior"][("CannonAP_Medium", "None")] == 100


def _write_warhead_repo(root: pathlib.Path, bullet_light_none: int):
    (root / "mods" / "cameo").mkdir(parents=True, exist_ok=True)
    (root / "mods" / "cameo" / "mod.yaml").write_text("Weapons:\n\tw.yaml\n", encoding="utf-8")
    (root / "mods" / "cameo" / "w.yaml").write_text(
        "^Warhead_Bullet_Light:\n"
        "\tWarhead@Bullet_Light: SpreadDamage\n"
        "\t\tVersus:\n"
        f"\t\t\tNone: {bullet_light_none}\n"
        "\t\t\tHeavy: 50\n"
        "^Warhead_CannonAP_Medium:\n"
        "\tWarhead@CannonAP_Medium: SpreadDamage\n"
        "\t\tVersus:\n"
        "\t\t\tNone: 60\n"
        "\t\t\tHeavy: 100\n", encoding="utf-8")


def test_priorpct_detects_cell_level_staleness(tmp_path):
    # two fixture rulesets differing in ONE Versus row -> exactly that cell's PriorPct differs.
    r = _rec()
    r["seen"]["start"]["composition"]["own_units"] = {"tank": 1, "rifle": 1}
    data = {"engagements": [r] * 4, "matches": []}
    priors = {}
    for pct in (25, 40):
        _write_warhead_repo(tmp_path, pct)
        priors[pct], _ = fp.versus_priors(tmp_path, ["Bullet_Light", "CannonAP_Medium"])
    a = fp.fit(data, PROFILES, priors[25])["cell_prior"]
    b = fp.fit(data, PROFILES, priors[40])["cell_prior"]
    assert a[("Bullet_Light", "None")] == 25 != b[("Bullet_Light", "None")] == 40
    assert a[("CannonAP_Medium", "None")] == b[("CannonAP_Medium", "None")] == 60
    assert {k for k in a if a[k] != b[k]} == {("Bullet_Light", "None")}


def test_jsonable_flattens_tuple_keys():
    import json
    res = _fit([_rec()])
    j = fp.jsonable(res)
    json.dumps(j)  # must not raise on tuple keys
    assert j["cell_prior"]["CannonAP_Medium|None"] == 50
    assert ("CannonAP_Medium" + "|" + "None") in j["cells"]


def test_uniform_scale_leaves_cells_neutral():
    # E1b: a global obs/exp scale is NOT a per-cell correction - a uniform 3x must
    # leave every factor at neutral (1000 means "at the global level", reported as g).
    res = _fit([_rec3x()] * 20)
    assert res["global_scale"] == pytest.approx(3.0, abs=0.05)
    for v in res["cells"].values():
        assert v == 1000


def test_one_cell_moves_relative_to_global_scale():
    # E1b: on top of the uniform 3x, ONE cell with a genuinely higher local ratio must
    # move while the uniform cells stay neutral. The anomalous record is small vs the
    # pool (pseudo-evidence K makes single-record cells shrink to the global level).
    hot = _rec3x()
    hot["seen"]["start"]["composition"]["own_units"] = {"gunpit": 1}  # -> CannonHE_Light cell
    hot["truth"]["start"]["enemy_unit_value"] = 80000
    hot["truth"]["enemy_loss_value"] = 64000
    hot["seen"]["start"]["predicted_enemy_surviving_permille"] = 900  # exp 8000, obs 64000
    res = _fit([_rec3x()] * 80 + [hot])
    assert res["global_scale"] == pytest.approx(3.0, abs=0.4)
    assert res["cells"][("CannonHE_Light", "None")] >= 1900
    for k, v in res["cells"].items():
        if k != ("CannonHE_Light", "None"):
            assert 930 <= v <= 1030


def test_profile_collisions_are_named():
    import json
    import tempfile
    with tempfile.TemporaryDirectory() as td:
        root = pathlib.Path(td)
        (root / "docs" / "balance").mkdir(parents=True)
        for ledger in ("a.json", "b.json"):
            (root / "docs" / "balance" / ledger).write_text(json.dumps(
                {"ledger": {}, "sections": {"g": {"dup_unit": {"armaments": []}}}}))
        _, meta, _ = fp.load_profiles(root)
    assert meta["collisions"] == ["dup_unit"]


# ── PRIORS-CARRY (fitter item 2): per-log stats, posterior anchor, decay ─────────────────────

def _legacy(rec):
    """A record without the balance stats block (pre-Tier4 logs)."""
    r = {k: v for k, v in rec.items()}
    del r["balance"]
    return r


def test_legacy_records_are_weighted_down_not_repriced():
    # The same evidence at full weight vs legacy weight: the legacy cell pulls less toward
    # its (identical) ratio because the shrink constant is relatively larger — unverifiable
    # logs count less, they are never silently re-priced at full confidence.
    recs = [_rec3x()] * 40
    hot = _rec3x()
    hot["seen"]["start"]["composition"]["own_units"] = {"rifle": 1}
    hot["truth"]["start"]["enemy_unit_value"] = 100000
    hot["truth"]["enemy_loss_value"] = 100000
    hot["seen"]["start"]["predicted_enemy_surviving_permille"] = 990
    key = ("Bullet_Light", "None")
    own = fp.fit({"engagements": recs + [hot], "matches": []}, PROFILES, PRIORS)["cells"][key]
    leg = fp.fit({"engagements": [_legacy(r) for r in recs + [hot]], "matches": []},
                 PROFILES, PRIORS)["cells"][key]
    assert leg < own and leg > 1000
    res = fp.fit({"engagements": [_legacy(_rec())], "matches": []}, PROFILES, PRIORS)
    assert res["skipped"]["legacy_stats"] == 1


def test_own_versus_records_price_their_own_cells():
    # A record whose staked Versus differs from today's contributes decayed evidence to
    # that cell (exp(-|ln(rec/now)|*1000/tau) < 1) — verifiable via eff_evidence mass.
    recs = [_rec() for _ in range(10)]
    moved = [_rec() for _ in range(10)]
    for r in moved:
        r["balance"]["versus"]["CannonAP_Medium|None"] = 200   # today: 50 -> 4x move
    key = ("CannonAP_Medium", "None")
    a = fp.fit({"engagements": recs + moved, "matches": []}, PROFILES, PRIORS)
    # the moved records' evidence on that cell decays hard (4x -> ~0.14) — less effective
    # evidence than the same records staked at today's percent
    same = fp.fit({"engagements": recs * 2, "matches": []}, PROFILES, PRIORS)
    assert a["eff_evidence"][key] < same["eff_evidence"][key]


def _prev_yaml(cells, prior, evidence, gs=1000, lh="oldhash", fv=1):
    lines = ["BotEngagementPriors:", f"\tGlobalScaleMilli: {gs}", f"\tLedgerHash: {lh}",
             f"\tFitVersion: {fv}"]
    for (d, a), m in cells.items():
        lines.append(f"\tDeliveryArmour@{d}__x__{a}: {m}")
        lines.append(f"\tPriorPct@{d}__x__{a}: {prior[(d, a)]}")
        lines.append(f"\tEvidence@{d}__x__{a}: {evidence[(d, a)]}")
    return lines


def test_prev_posterior_anchors_cells_with_no_new_evidence(tmp_path):
    # A cell that saw no fights this batch keeps its previous residual, decayed — the
    # maintainer's ask: training data carries over instead of resetting to neutral.
    prev_file = tmp_path / "prev.yaml"
    prev_file.write_text("\n".join(_prev_yaml(
        {("CannonAP_Medium", "None"): 1500}, {("CannonAP_Medium", "None"): 50},
        {("CannonAP_Medium", "None"): 8000})), encoding="utf-8")
    prev = fp.load_prev(prev_file)
    res = fp.fit({"engagements": [_rec()] * 20, "matches": []}, PROFILES, PRIORS,
                 prev=prev, ledger_hash="newhash")
    assert res["cells"][("CannonAP_Medium", "None")] > 1000
    assert res["boundary"] is True and res["fit_version"] == 2


def test_carried_evidence_decays_with_prior_move_and_boundary(tmp_path):
    # Same prev cell under three drift levels: no move, 10% move, 2x move -> the carried
    # anchor mass drops (boundary decay * exp(-|ln d|*1000/tau)).
    prev_file = tmp_path / "prev.yaml"
    key = ("CannonAP_Medium", "None")
    prev_file.write_text("\n".join(_prev_yaml(
        {key: 1500}, {key: 50}, {key: 8000})), encoding="utf-8")
    prev = fp.load_prev(prev_file)
    same = fp.fit({"engagements": [], "matches": []}, PROFILES, PRIORS,
                  prev=prev, ledger_hash="oldhash")
    moved10 = fp.fit({"engagements": [], "matches": []}, PROFILES,
                     dict(PRIORS, **{"CannonAP_Medium": dict(PRIORS["CannonAP_Medium"], **{"None": 55})}),
                     prev=prev, ledger_hash="oldhash")
    moved2x = fp.fit({"engagements": [], "matches": []}, PROFILES,
                     dict(PRIORS, **{"CannonAP_Medium": dict(PRIORS["CannonAP_Medium"], **{"None": 100})}),
                     prev=prev, ledger_hash="oldhash")
    e_same = same["carried"][key][0]
    e_10 = moved10["carried"][key][0]
    e_2x = moved2x["carried"][key][0]
    assert e_same > e_10 > e_2x
    assert e_same == 8000  # no boundary, no move: full carry
    assert moved10["cells"][key] > moved2x["cells"][key] > 1000


def test_dead_delivery_row_carries_nothing(tmp_path):
    # A delivery whose Versus row vanished entirely gets w_carry = 0 — same neutral as the
    # consumer's disappeared-row rule.
    key = ("CannonAP_Medium", "None")
    prev_file = tmp_path / "prev.yaml"
    prev_file.write_text("\n".join(_prev_yaml({key: 1500}, {key: 50}, {key: 8000})),
                         encoding="utf-8")
    prev = fp.load_prev(prev_file)
    priors = {t: dict(v) for t, v in PRIORS.items()}
    del priors["CannonAP_Medium"]
    res = fp.fit({"engagements": [], "matches": []}, PROFILES, priors,
                 prev=prev, ledger_hash="oldhash")
    assert res["carried"][key][2] == 0.0
    assert res["cells"][key] == 1000


def test_evidence_rows_roundtrip_through_load_prev(tmp_path):
    out = tmp_path / "p.yaml"
    res = _fit([_rec()] * 10)
    out.write_text(fp.to_yaml(res, "h"), encoding="utf-8")
    back = fp.load_prev(out)
    key = ("CannonAP_Medium", "None")
    assert back["cells"][key] == res["cells"][key]
    assert back["cell_prior"][key] == 50
    assert back["evidence"][key] == res["eff_evidence"][key] > 0
    assert back["ledger_hash"] == "h" and back["fit_version"] == res["fit_version"]


def test_yaml_emits_carry_keys():
    y = fp.to_yaml(_fit([_rec()]), "h")
    assert "\tFitVersion: 1" in y
    assert "\tStalenessTauMilli: 350" in y
    for l in y.splitlines():
        if l.startswith("\tPriorPct@"):
            key = l.split("@", 1)[1].split(":")[0]
            assert f"\tEvidence@{key}:" in y


def test_family_pool_borrows_from_sibling_cells():
    # A thin cell in a family with hot siblings borrows the family-measured level instead
    # of sitting at its own thin observation (hierarchical pooling cell -> family -> global).
    # Control: the same data with the delivery renamed into a family of one -> the thin
    # cell keeps only the global anchor and stays near neutral.
    fam_priors = dict(PRIORS)
    fam_priors["Bullet_Heavy"] = {"None": 100, "Heavy": 25, "Wood": 50}
    prof = dict(PROFILES)
    prof["hmg"] = {"cost": 200, "hp": 60, "armor": "None",
                   "weapons": [("Bullet_Heavy", 20.0)]}
    hot = _rec()
    hot["seen"]["start"]["composition"]["own_units"] = {"rifle": 40}
    hot["truth"]["start"]["enemy_unit_value"] = 400
    hot["truth"]["enemy_loss_value"] = 2000          # clipped to the 400 census -> obs 400
    hot["seen"]["start"]["predicted_enemy_surviving_permille"] = 800   # exp 80 -> 5x sibling
    thin = _rec()
    thin["seen"]["start"]["composition"]["own_units"] = {"hmg": 1}
    thin["truth"]["start"]["enemy_unit_value"] = 400
    thin["truth"]["enemy_loss_value"] = 400          # ~1x -> thin cell at global level
    recs = [_rec() for _ in range(40)] + [hot] * 8 + [thin] * 3
    res = fp.fit({"engagements": recs, "matches": []}, prof, fam_priors)
    iso_priors = dict(fam_priors)
    iso_priors["Zap_Heavy"] = iso_priors.pop("Bullet_Heavy")
    iso_prof = dict(prof)
    iso_prof["hmg"] = dict(prof["hmg"], weapons=[("Zap_Heavy", 20.0)])
    iso = fp.fit({"engagements": recs, "matches": []}, iso_prof, iso_priors)
    fam_cell = res["cells"][("Bullet_Heavy", "None")]
    iso_cell = iso["cells"][("Zap_Heavy", "None")]
    assert fam_cell > iso_cell
    assert fam_cell > 1000 >= iso_cell - 100


def test_global_scale_milli_key_emitted_and_clamped():
    # Adopted ruling (was: header comment only): consumers multiply g back into FITTED
    # cells while stale/missing cells stay at neutral 1000, so mixed tables need g as a
    # real key - clamped to the same [MIN_MILLI, MAX_MILLI] bounds as factor cells.
    res = _fit([_rec3x()] * 20)
    assert res["global_scale_milli"] == fp.MAX_MILLI   # g ~3.0 -> 3000 clamps to 2000
    assert "\tGlobalScaleMilli: 2000" in fp.to_yaml(res, "h").splitlines()

    half = _rec3x()
    half["truth"]["enemy_loss_value"] = 4500       # dir-0 obs 4500 vs exp 3000
    half["outcome"]["own_lost_unit_value"] = 150   # dir-1 obs 150 vs exp 100 -> g = 1.5
    res2 = _fit([half] * 20)
    assert res2["global_scale_milli"] == 1500
    assert "\tGlobalScaleMilli: 1500" in fp.to_yaml(res2, "h").splitlines()

    cold = _rec3x()
    cold["truth"]["enemy_loss_value"] = 500        # obs 800 total
    # expected loss = full min(census, record-cap) when surviving is 0 -> g ~0.24
    cold["seen"]["start"]["predicted_enemy_surviving_permille"] = 0
    res3 = _fit([cold] * 20)
    assert res3["global_scale_milli"] == fp.MIN_MILLI
