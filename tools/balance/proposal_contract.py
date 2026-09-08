"""Shared damage-column contract for class proposals and ledger ingestion.

The cell is per-main damage times main count, NOT total damage or a multiplier
relative to the largest old warhead. Old reports must be regenerated.
"""
import re

DAMAGE_COLUMN = "dmg/wh×n"
COLUMNS = ("actor", "faction", "HP", "spd", "rng", "cost", DAMAGE_COLUMN,
           "rl", "burst", "legacy FP%", "eff DPS", "price", "Δ", "flags",
           "dmg_filter", "weapon")


def table_header():
    return "| " + " | ".join(COLUMNS) + " |"


def table_separator():
    return "|" + "|".join("---" for _ in COLUMNS) + "|"


def format_damage(per_main, count):
    return f"{per_main}×{count}"


def parse_damage(value):
    match = re.fullmatch(r"([0-9]+)×([1-9][0-9]*)", value.strip())
    if not match:
        raise ValueError(f"invalid {DAMAGE_COLUMN} target: {value!r}")
    return tuple(map(int, match.groups()))
