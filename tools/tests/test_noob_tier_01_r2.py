"""TIER-01 successor of tools/tests/test_noob_tier_01.py (Code Monkey, 2026-11-08). The old file pins FREEZE9 ladders and is
not edited (pinned). This file pins the TABLE_03_R2 079A06F2 ladders applied on tier01-yaml (Opus rulings 4 and 2).

- Noob = the proposal's first value, which is one linear step below easiest where the rule gives a value >= floor.
- Existing tiers (easiest..god) take the proposal values (noted per field below).
"""
from __future__ import annotations

import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))

import audit_ai_personalities as aap  # noqa: E402


def _ai_lines() -> list[str]:
    return (ROOT / "mods" / "cameo" / "ai" / "ai.yaml").read_text(encoding="utf-8").replace("\r\n", "\n").split("\n")


def _block(name: str) -> dict[str, str]:
    lines = _ai_lines()
    fields = aap.field_blocks(aap.lines_for_block(lines, f"BotLimits@{name}"))
    return {k: v.split(":", 1)[1].strip() for k, v in fields.items()}


def test_noob_botlimits_match_the_rule_and_the_audit_check_is_clean():
    assert aap.noob_failures(_ai_lines()) == []


def test_noob_values_are_the_proposal_first_values():
    noob = _block("noob")
    assert noob["HarvesterValueLimit"] == "2000"
    assert noob["HarvesterLimit"] == "0"
    assert noob["RefineryLimit"] == "1"
    assert noob["ProductionTypeLimit"] == "1"
    assert noob["ConstructionYardLimit"] == "1"
    assert noob["CapturerInfantryLimitPct"] == "0"
    assert noob["PersonalityReactionDelay"] == "6000"
    assert noob["RetreatRatioPct"] == "0"
    assert noob["RequiresCondition"] == "noobbot"


def test_easiest_and_god_take_the_proposal_values():
    easiest = _block("easiest")
    assert easiest["HarvesterValueLimit"] == "4000"
    assert easiest["HarvesterLimit"] == "0"
    assert easiest["PersonalityReactionDelay"] == "5500"
    assert easiest["RetreatRatioPct"] == "5"
    god = _block("god")
    assert god["HarvesterValueLimit"] == "22000"
    assert god["HarvesterLimit"] == "0"
    assert god["PersonalityReactionDelay"] == "1000"
    assert god["RetreatRatioPct"] == "50"
    assert god["ConstructionYardLimit"] == "11"
    assert god["BuildingDelayModifier"] == "0"


def test_every_tier_writes_the_new_harvester_value_key():
    for name in aap.DIFFICULTIES:
        assert "HarvesterValueLimit" in _block(name), name


def test_reaction_delay_ladder_is_the_proposal_ladder():
    assert aap.REACTION_DELAYS == (5500, 5000, 4500, 4000, 3500, 3000, 2500, 2000, 1500, 1000)


def test_scale_targets_minute_zero_lines_match_the_botlimits_ladders():
    # section 8: refinery/production/conyard 2000..11000 -> 2..11; harvester 4000..22000 (no BotLimits count, USER RULING)
    assert aap.scale_line(2000, 11000) == [2, 3, 4, 5, 6, 7, 8, 9, 10, 11]
    assert aap.scale_line(4000, 22000) == [4, 6, 8, 10, 12, 14, 16, 18, 20, 22]
    assert aap.scale_line(1225, 3250) == aap.TECH_LINE
