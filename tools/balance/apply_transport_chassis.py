#!/usr/bin/env python3
"""Apply the chassis-only transport slice from the 20260916 map.

Cost is intentionally absent: Cargo.InitialUnits pricing is governed by the
passenger-sum law, and the four current authored loads already resolve to
their current carrier costs. Only HP, Aircraft.Speed/TurnSpeed and the F1/F2
heal/repair companions move here.
"""
from __future__ import annotations

import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import apply_harvester_durability as Editor  # noqa: E402

ROOT = Editor.ROOT
GDI = ROOT / "mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml"
NOD = ROOT / "mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/aircraft.yaml"
ALLIES = ROOT / "mods/cameo/ContentPacks/RedAlert/Allies/yaml/aircraft.yaml"
SOVIETS = ROOT / "mods/cameo/ContentPacks/RedAlert/Soviets/yaml/aircraft.yaml"


def fields(hp: int, speed: int, turn: int, step: int, repair: int):
    return {
        "Aircraft": {"Speed": speed, "TurnSpeed": turn},
        "Health": {"HP": hp},
        "Repairable": {"HpPerStep": repair},
        "ChangesHealth@SelfHealing": {"Step": step},
    }


SPECS = {
    "td_gdi_chinooktransport": (GDI, None, fields(82000, 124, 25, 33, 4100)),
    "td_nod_chinooktransport": (NOD, None, fields(82000, 124, 25, 33, 4100)),
    "ra1_allies_alliedchinooktransport": (ALLIES, None, fields(88000, 120, 24, 35, 4400)),
    "ra1_soviets_hiptransport": (SOVIETS, None, fields(104000, 118, 24, 42, 5200)),
}

EXPECTED_OLD = {
    "td_gdi_chinooktransport": {
        "Aircraft.Speed": "150", "Aircraft.TurnSpeed": "30",
        "Health.HP": "100000", "Repairable.HpPerStep": "5000",
        "ChangesHealth@SelfHealing.Step": "40",
    },
    "td_nod_chinooktransport": {
        "Aircraft.Speed": "150", "Aircraft.TurnSpeed": "30",
        "Health.HP": "100000", "Repairable.HpPerStep": "5000",
        "ChangesHealth@SelfHealing.Step": "40",
    },
    "ra1_allies_alliedchinooktransport": {
        "Aircraft.Speed": "125", "Aircraft.TurnSpeed": "25",
        "Health.HP": "125000", "Repairable.HpPerStep": "6250",
        "ChangesHealth@SelfHealing.Step": "50",
    },
    "ra1_soviets_hiptransport": {
        "Aircraft.Speed": "100", "Aircraft.TurnSpeed": "20",
        "Health.HP": "150000", "Repairable.HpPerStep": "7500",
        "ChangesHealth@SelfHealing.Step": "60",
    },
}


def main() -> int:
    old_expected, old_specs = Editor.EXPECTED_OLD, Editor.SPECS
    try:
        Editor.EXPECTED_OLD = EXPECTED_OLD
        Editor.SPECS = SPECS
        return Editor.main()
    finally:
        Editor.EXPECTED_OLD, Editor.SPECS = old_expected, old_specs


if __name__ == "__main__":
    sys.exit(main())
