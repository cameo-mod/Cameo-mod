"""Focused checks for the burst-rate signature evidence ledger."""

import pathlib
import sys


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))

from audit_burst_rate_signature import (  # noqa: E402
    build_report,
    committed_delays,
    evidence_status,
    exact_cycle_ticks,
    has_burst_one_signature,
)


def test_identity_detection_and_exact_delay_gate():
    row = {"w_damage_per_shot": 100, "w_burst": 2, "w_reload": 400, "w_dps": .25}
    assert has_burst_one_signature(row)
    assert committed_delays(row) is None
    assert committed_delays({**row, "w_burst_delays": [5]}) == [5.0]
    assert committed_delays({**row, "w_burst_delays": [2, 5]}) is None
    assert committed_delays({**row, "w_burst_delays": [-1]}) is None
    reviewed = {**row, "w_damage": 200, "w_cycle_evidence": {
        "burst_delays": [{"minimum": 400, "mean": 400, "maximum": 400}],
        "cycle_min": 800, "cycle_mean": 800, "cycle_max": 800,
    }}
    assert committed_delays(reviewed) == [400.0]
    assert exact_cycle_ticks(reviewed) == 800
    assert evidence_status(reviewed) == "evidence_consistent_no_recompute"


def test_current_v21_counts_and_blocker_are_exact():
    report = build_report()
    assert report["counts"] == {
        "loaded_peer_rows": 4367,
        "burst_rows_with_rate_inputs": 621,
        "burst_one_signature_matches": 334,
        "evidence_consistent_no_recompute": 1,
        "ready_exact_cycle_recompute": 0,
        "legacy_requires_cycle_evidence": 333,
    }
    assert sum(report["by_source"].values()) == 334
    assert report["by_evidence"] == {"legacy-unassessed": 333, "nominal_direct": 1}


if __name__ == "__main__":
    test_identity_detection_and_exact_delay_gate()
    test_current_v21_counts_and_blocker_are_exact()
    print("Burst-rate signature audit fixtures: PASS")
