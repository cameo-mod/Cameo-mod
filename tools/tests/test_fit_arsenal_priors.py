"""fit_arsenal_priors: shrinkage, reference pooling, and one fitted match."""
import json
import pathlib
import sys

import pytest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "ai"))
import capture_provenance  # noqa: E402
import fit_arsenal_priors as fap  # noqa: E402


def _bind(path):
    """Scoreable test provenance: unbound fixtures fail closed at gated sinks."""
    p = pathlib.Path(path)
    uids = set()
    for f in p.rglob("*.jsonl"):
        uids |= capture_provenance.file_game_uids(f)
    capture_provenance.write_provenance(
        p, capture_kind="match_batch", scoreable=True, game_uids=sorted(uids))


def test_small_samples_shrink_toward_an_even_trade():
    assert fap.shrunk_ratio(0, 0) == 1.0
    assert 1.0 < fap.shrunk_ratio(1000, 0) < 1.3
    assert fap.shrunk_ratio(100000, 10000) > 5


def test_reference_bots_pool_across_batches(tmp_path):
    assert fap.variant_of(tmp_path / "ab_r7_active_2", "classic") == "classic"
    assert fap.variant_of(tmp_path / "ab_r7_active_2", "hard") == "ab_r7_active/hard"


def test_one_match_fits_trades_and_elo(tmp_path):
    logs = tmp_path / "ab_x_1" / "Logs"
    logs.mkdir(parents=True)
    def rec(bot, name, outcome, arsenal):
        return {"game_uid": "g", "recorded_utc": "1", "player": {"bot_type": bot, "name": name, "faction": "td_gdi", "outcome": outcome},
                "stats": {}, "arsenal": arsenal}
    rows = [rec("hard", "Multi0", "won", [{"type": "tank", "killed_value": 9000, "lost_value": 1000, "created": 3}]),
            rec("classic", "Multi1", "lost", [{"type": "tank", "killed_value": 1000, "lost_value": 9000, "created": 3}])]
    (logs / "cameo-ai-matches.jsonl").write_text("\n".join(json.dumps(r) for r in rows), encoding="utf-8")
    _bind(tmp_path / "ab_x_1")
    result = fap.fit([tmp_path / "ab_x_1"])
    assert result["matches"] == 1
    assert result["ratings"]["ab_x/hard"] > result["ratings"]["classic"]
    assert result["trades"][("td_gdi", "td_gdi")]["tank"] == [10000, 10000, 6]
    assert "TradePercent@td_gdi__vs__td_gdi" in fap.to_yaml(result)


def _gate_batch(root, marked=None):
    """Batch dir with a two-seat match in Logs; marked None leaves it
    unbound (gate rejects), True sentinel-binds (gate skips), False
    scoreable-binds (gate loads)."""
    logs = root / "Logs"
    logs.mkdir(parents=True)
    def rec(bot, name, outcome):
        return {"game_uid": "g", "recorded_utc": "1",
                "player": {"bot_type": bot, "name": name, "faction": "td_gdi",
                           "outcome": outcome},
                "stats": {}, "arsenal": []}
    rows = [rec("hard", "Multi0", "won"), rec("classic", "Multi1", "lost")]
    (logs / "cameo-ai-matches.jsonl").write_text(
        "\n".join(json.dumps(r) for r in rows), encoding="utf-8")
    if marked is not None:
        capture_provenance.write_provenance(
            root, capture_kind="sentinel_diagnostic" if marked else "match_batch",
            scoreable=not marked, game_uids=["g"])
    return root


def test_fit_gate_sentinel_skips_unbound_rejects_bound_loads(tmp_path):
    # fit()'s per-dir matches+situations ingest gate: sentinel captures never
    # fit, unbound evidence rejects, bound evidence loads.
    d = _gate_batch(tmp_path / "sentinel", marked=True)
    assert fap.fit([d])["matches"] == 0
    with pytest.raises(ValueError):
        fap.fit([_gate_batch(tmp_path / "unbound")])
    d = _gate_batch(tmp_path / "bound", marked=False)
    assert fap.fit([d])["matches"] == 1
