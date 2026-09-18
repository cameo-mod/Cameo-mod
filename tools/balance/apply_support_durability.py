#!/usr/bin/env python3
"""apply_support_durability.py — classic-four support & MCV batch (2026-09-18, slice 2).

Applies the 2026-09-16 reference-map targets for the chassis-only rows
DeepSeek's survey surfaced after the harvester batch (four MCVs and
RA1's two unarmed support vehicles). Reuses the harvester batch's
ActorEditor verbatim; the module-level EXPECTED_OLD is swapped for this
batch's table before use.

Grids (accepted-batch conventions with one documented deviation):
    HP  -> 5,000 grid DEViation note: the accepted-batch rule is the
           1,000 grid, but the F2 identity (ChangesHealth@SelfHealing
           .Step == HP/2500) is exact only on multiples of 2,500, and the
           F1 identity (Repairable.HpPerStep == HP/20) on multiples of
           20. 5,000 is the nearest common grid satisfying both; the
           harvester batch could use the 1,000 grid because its targets
           happened to divide exactly.
    Speed -> integer (unsnapped rounded half-up)
    Cost -> 10 grid, half-up on exact 5s (1,705 -> 1,710)
    TurnSpeed follows the audit formula the actor currently obeys:
    MCVs are F8 vehicles (TurnSpeed = round(Speed/5), 15 -> 13 / 14),
    the gap generator and radar jammer currently RESOLVE at
    2 x round(Speed/5) (the turretless-frontal branch), so their turns
    follow the doubled rule for the new speeds (30 / 30).

Batch table (unsnapped map target -> applied):
    td_gdi/nod_mobileconstructionvehicle
        HP 293,935 -> 295,000  speed 64.72 -> 65  turn 15 -> 13
        step 120 -> 118  hpp 15,000 -> 14,750  cost 5,000 -> 4,920
    ra1_allies/soviets mobileconstructionvehicle
        HP 252,864 -> 255,000  speed 69.53 -> 70  turn 15 -> 14
        step 120 -> 102  hpp 15,000 -> 12,750  cost 5,000 -> 4,650
    ra1_allies_mobilegapgenerator
        HP 96,282 -> 95,000  speed 75.63 -> 76  turn 30 -> 30
        step 10 -> 38  hpp 1,250 -> 4,750  cost 5,000 -> 1,640
    ra1_allies_mobileradarjammer
        HP 71,989 -> 70,000  speed 73.62 -> 74  turn 40 -> 30
        step 10 -> 28  hpp 1,250 -> 3,500  cost 5,000 -> 1,710

The four chinook/HIP transports and any passenger-carried actor are
EXCLUDED on purpose: dropping their cost touches the passenger-sum
cargo pricing contract and needs a separate review round.
^MCV is NOT edited: it also feeds MCVs in ~20 other packs.
"""
from __future__ import annotations

import sys
import pathlib

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

import apply_harvester_durability as Editor  # noqa: E402  (shares ActorEditor)

ROOT = Editor.ROOT

TD_VEH = ROOT / "mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml"
TD_NOD_VEH = ROOT / "mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml"
RA_AIR_VEH = ROOT / "mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml"
RA_SOV_VEH = ROOT / "mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml"


def _mcv(hp: int, speed: int, turn: int, cost: int) -> dict[str, dict[str, int]]:
    return {
        "Valued": {"Cost": cost},
        "Mobile": {"Speed": speed, "TurnSpeed": turn},
        "Health": {"HP": hp},
        "Repairable": {"HpPerStep": hp // 20},
        "ChangesHealth@SelfHealing": {"Step": hp // 2500},
    }


SPECS = {
    "td_gdi_mobileconstructionvehicle": (TD_VEH, None, _mcv(295000, 65, 13, 4920)),
    "td_nod_mobileconstructionvehicle": (TD_NOD_VEH, None, _mcv(295000, 65, 13, 4920)),
    "ra1_allies_alliedmobileconstructionvehicle": (RA_AIR_VEH, None, _mcv(255000, 70, 14, 4650)),
    "ra1_soviets_mobileconstructionvehicle": (RA_SOV_VEH, None, _mcv(255000, 70, 14, 4650)),
    "ra1_allies_mobilegapgenerator": (
        RA_AIR_VEH,
        None,
        {"Mobile": {"Speed": 76, "TurnSpeed": 30},
         "Health": {"HP": 95000},
         "Repairable": {"HpPerStep": 4750},
         "ChangesHealth@SelfHealing": {"Step": 38},
         "Valued": {"Cost": 1640}},
    ),
    "ra1_allies_mobileradarjammer": (
        RA_AIR_VEH,
        None,
        {"Mobile": {"Speed": 74, "TurnSpeed": 30},
         "Health": {"HP": 70000},
         "Repairable": {"HpPerStep": 3500},
         "ChangesHealth@SelfHealing": {"Step": 28},
         "Valued": {"Cost": 1710}},
    ),
}

EXPECTED_OLD = {
    "ra1_allies_mobilegapgenerator": {
        "Mobile.Speed": "75",
        "Mobile.TurnSpeed": "30",
        "Health.HP": "25000",
        "Repairable.HpPerStep": "1250",
        "ChangesHealth@SelfHealing.Step": "10",
        "Valued.Cost": "5000",
    },
    "ra1_allies_mobileradarjammer": {
        "Mobile.Speed": "100",
        "Mobile.TurnSpeed": "40",
        "Health.HP": "25000",
        "Repairable.HpPerStep": "1250",
        "ChangesHealth@SelfHealing.Step": "10",
        "Valued.Cost": "5000",
    },
}


def main() -> int:
    Editor.EXPECTED_OLD = EXPECTED_OLD
    Editor.SPECS = SPECS
    rc = Editor.main()
    return rc


if __name__ == "__main__":
    import sys as _sys
    _sys.exit(main())
