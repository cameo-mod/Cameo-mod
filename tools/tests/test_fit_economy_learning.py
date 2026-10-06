import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "ai"))
import fit_economy_learning as fit


def decision(record_id, *, kind="expansion", candidate=12000, reward=1000,
             faction="td_gdi", enemy="td_nod", public=True, fingerprint="deadbeef"):
    return {
        "schema": "economy_decision/1", "record": "economy_decision", "record_id": record_id,
        "faction": faction, "enemy_faction": enemy, "enemy_faction_public": public,
        "balance": {"fingerprint": fingerprint},
        "decision": {"parameter": kind, "candidate": candidate},
        "outcome": {"economy_reward_milli": reward},
    }


def test_duplicate_or_unreviewed_rows_cannot_reach_a_sample_floor():
    repeated = decision("game|seat|1")
    rows = [repeated] * fit.MINIMUM["expansion"]
    rows += [decision(f"bad-{i}", faction="PrivatePlayerName") for i in range(fit.MINIMUM["expansion"])]
    rows += [dict(decision(f"schema-{i}"), schema="wrong") for i in range(fit.MINIMUM["expansion"])]
    assert fit.fit(rows) == {}


def test_credit_winner_has_arm_support_and_emits_matching_evidence():
    rows = []
    for i in range(fit.MINIMUM["expansion"]):
        rows.append(decision(f"r{i}", candidate=12000 if i < 50 else 18000,
                             reward=1000 if i < 50 else 2000))
    fitted = fit.fit(rows)
    n, choice = fitted["expansion"]["td_gdi__vs__td_nod"]
    assert n == fit.MINIMUM["expansion"]
    assert choice == 18000
    rendered = fit.render(fitted)
    assert "ExpansionCashDivisor@td_gdi__vs__td_nod: 18000" in rendered
    assert "ExpansionEvidence@td_gdi__vs__td_nod: 100" in rendered


def test_hidden_enemy_does_not_create_an_exact_matchup_cell():
    rows = [decision(f"hidden-{i}", public=False) for i in range(fit.MINIMUM["expansion"])]
    fitted = fit.fit(rows)
    assert "td_gdi__vs__td_nod" not in fitted["expansion"]
    assert fitted["expansion"]["td_gdi"][0] == fit.MINIMUM["expansion"]
