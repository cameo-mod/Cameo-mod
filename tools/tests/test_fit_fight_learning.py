import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "ai"))
import fit_fight_learning as fit


def engagement(record_id, *, faction="td_gdi", enemy="td_nod", public=True, fingerprint="deadbeef", killed=60, lost=40):
    return {
        "schema": "engagement/2", "record": "engagement", "record_id": record_id,
        "faction": faction, "enemy_faction": enemy, "enemy_faction_public": public,
        "balance": {"fingerprint": fingerprint},
        "seen": {"start": {"own_committed_value": 100, "own_defence_value": 0,
                              "enemy_unit_value": 100, "predicted_enemy_surviving_permille": 500,
                              "predicted_ratio_milli": 500}},
        "outcome": {"enemy_killed_value": killed, "own_lost_value": lost},
    }


def test_duplicate_private_and_wrong_schema_rows_do_not_make_an_exact_fit():
    one = engagement("g|seat_1|1")
    rows = [one] * fit.THRESHOLD_MIN
    rows += [engagement(f"p{i}", faction="PrivatePlayerName") for i in range(fit.THRESHOLD_MIN)]
    rows += [dict(engagement(f"bad{i}"), schema="wrong") for i in range(fit.THRESHOLD_MIN)]
    assert fit.fit(rows) == {}


def test_public_rows_need_independent_ids_and_effective_value_has_a_250_floor():
    rows = [engagement(f"g|seat_1|{i}") for i in range(fit.CALIBRATION_MIN)]
    fitted = fit.fit(rows)
    assert fitted["td_gdi__vs__td_nod"][1] is not None
    assert fitted["td_gdi__vs__td_nod"][2] is None

    rows.extend(engagement(f"g|seat_1|{i}") for i in range(fit.CALIBRATION_MIN, fit.EFFECTIVE_VALUE_MIN))
    fitted = fit.fit(rows)
    assert 500 <= fitted["td_gdi__vs__td_nod"][2] <= 2000


def test_hidden_enemy_is_pooled_above_exact_matchup():
    rows = [engagement(f"g|seat_1|{i}", public=False) for i in range(fit.THRESHOLD_MIN)]
    fitted = fit.fit(rows)
    assert "td_gdi__vs__td_nod" not in fitted
    assert fitted["td_gdi"][0] == fit.THRESHOLD_MIN
