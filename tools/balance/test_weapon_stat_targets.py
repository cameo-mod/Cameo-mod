#!/usr/bin/env python3
"""Focused R1 semantics checks for legacy armament-profile coordinates."""
from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import reference_distribution as rd  # noqa: E402


def number(value):
    return float(value) if value is not None else None


def main() -> int:
    simultaneous = [
        {"damage_warheads": [{"damage": "100"}], "burst": "4", "reloaddelay": "20", "burstdelays": "1"},
        {"damage_warheads": [{"damage": "200"}], "burst": "1", "reloaddelay": "20", "burstdelays": ""},
    ]
    profile, debt, _ = rd.armament_profile(simultaneous, number)
    assert not debt
    assert profile["w_damage"] == 600, profile
    # Damage is the summed burst aggregate; the primary burst is the hardest single armament's
    # burst, so these coordinates intentionally describe different dimensions.
    assert profile["w_burst"] == 1, profile

    alternatives = [
        {"damage_warheads": [{"damage": "100"}], "burst": "1", "reloaddelay": "20", "burstdelays": "", "requires": "!upgrade"},
        {"damage_warheads": [{"damage": "900"}], "burst": "1", "reloaddelay": "20", "burstdelays": "", "requires": "upgrade"},
    ]
    profile, debt, _ = rd.armament_profile(alternatives, number)
    assert not debt
    assert profile["w_damage"] == 100, profile
    print("weapon_stat_targets semantics self-test: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
