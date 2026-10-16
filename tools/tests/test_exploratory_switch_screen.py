from __future__ import annotations

import pytest

from tools.ai.exploratory_switch_screen import (
	BJ_DISPOSITION_SHA256,
	CANDIDATES,
	MAPS,
	SEEDS,
	_expected_groups,
	materialize_in_memory,
	plan_cells,
)


SKIP = ["SquadManagerBotModuleCA@classic"]
GROUPS = _expected_groups()
AI_PATH = "mods/cameo/ai/ai.yaml"
FRANSBOT_PATH = "mods/cameo/ai/fransbot.yaml"
FRANSBOT_LISTS_PATH = "mods/cameo/ai/fransbot_lists.yaml"
AI_TEXT = """Player:
	ExpansionPlannerBotModule:
		RequiresCondition: genericbot
		UseCrawlSafety: true
		UseProductionScaling: true
		UseFrontierObservation: false
	BaseBuilderBotModuleCA@generic:
		RequiresCondition: genericbot || classicbot
		SwitchCondition: genericbot
		UseProductionScaling: true
	SquadManagerBotModuleCA@rush:
		RequiresCondition: genericbot && personality-rush
		UseFormationHysteresis: true
		FormationHoldHysteresisCells: 1
	SquadManagerBotModuleCA@classic:
		RequiresCondition: classicbot
		UseFormationHysteresis: true
		FormationHoldHysteresisCells: 7
	UnitBuilderBotModuleCA@generic:
		RequiresCondition: genericbot || classicbot
		SwitchCondition: genericbot
		BankSaturationCash: 0
"""


def fixture_files(ai_text: str = AI_TEXT) -> dict[str, str]:
	return {AI_PATH: ai_text, FRANSBOT_PATH: "", FRANSBOT_LISTS_PATH: ""}


def test_plan_is_24_map_major_cells_with_seed_reuse_and_rotation():
	cells = plan_cells()
	assert len(cells) == 24
	assert len({row["cell_id"] for row in cells}) == 24
	assert list(SEEDS) == [row[0] for row in MAPS]
	for map_index, (map_key, *_rest) in enumerate(MAPS):
		rows = [cell for cell in cells if cell["map_key"] == map_key]
		assert len(rows) == 3
		assert [row["candidate"] for row in rows] == list((
			("BY_crawl_safety", "BZ_production_scaling", "BJ_squad_hysteresis"),
			("BZ_production_scaling", "BJ_squad_hysteresis", "BY_crawl_safety"),
			("BJ_squad_hysteresis", "BY_crawl_safety", "BZ_production_scaling"),
		)[map_index % 3])
		assert {row["seed"] for row in rows} == {SEEDS[map_key]}
		assert {row["execution_authorized"] for row in rows} == {False}
		assert all(row["actual_seats"] is None and row["caps"] is None for row in rows)
	assert len({row["seed"] for row in cells}) == 8


@pytest.mark.parametrize("candidate", CANDIDATES)
def test_payloads_normalize_all_candidates_and_change_only_selected(candidate):
	source = fixture_files()
	pair = materialize_in_memory(source, GROUPS, SKIP, candidate, BJ_DISPOSITION_SHA256.upper())
	assert source[AI_PATH] == AI_TEXT  # input mapping and text are not mutated
	assert pair.control[AI_PATH] != pair.treatment[AI_PATH]
	assert pair.source_file_sha256[AI_PATH]
	assert pair.source_payload_sha256 and pair.control_sha256 and pair.treatment_sha256
	assert pair.control_file_sha256[AI_PATH] and pair.treatment_file_sha256[AI_PATH]
	assert pair.control_semantic_sha256 != pair.treatment_semantic_sha256
	for arm in (pair.control[AI_PATH], pair.treatment[AI_PATH]):
		assert "\t\tUseFrontierObservation: true" in arm
		assert "\t\tBankSaturationCash: 1" in arm
		assert "\t\tSwitchCondition: genericbot" in arm
		assert "\t\tFormationHoldHysteresisCells: 2" in arm
	assert "\t\tUseFormationHysteresis: true\n\t\tFormationHoldHysteresisCells: 7" in pair.control[AI_PATH]
	assert "\t\tUseFormationHysteresis: true\n\t\tFormationHoldHysteresisCells: 7" in pair.treatment[AI_PATH]
	for other in set(CANDIDATES) - {candidate}:
		field = {
			"BY_crawl_safety": "UseCrawlSafety",
			"BZ_production_scaling": "UseProductionScaling",
			"BJ_squad_hysteresis": "UseFormationHysteresis",
		}[other]
		# Candidate switches are false in both arms unless this candidate is selected.
		if other == "BY_crawl_safety":
			assert "UseCrawlSafety: false" in pair.control[AI_PATH]
			assert "UseCrawlSafety: false" in pair.treatment[AI_PATH]
		elif other == "BZ_production_scaling":
			assert pair.control[AI_PATH].count("UseProductionScaling: false") == 2
			assert pair.treatment[AI_PATH].count("UseProductionScaling: false") == 2
		else:
			assert "SquadManagerBotModuleCA@rush:\n\t\tRequiresCondition: genericbot && personality-rush\n\t\tUseFormationHysteresis: false" in pair.control[AI_PATH]
			assert "SquadManagerBotModuleCA@rush:\n\t\tRequiresCondition: genericbot && personality-rush\n\t\tUseFormationHysteresis: false" in pair.treatment[AI_PATH]
	assert field in {"UseCrawlSafety", "UseProductionScaling", "UseFormationHysteresis"}


def test_unknown_candidate_receipt_and_path_are_refused():
	with pytest.raises(ValueError, match="unknown exploratory candidate"):
		materialize_in_memory(fixture_files(), GROUPS, SKIP, "BV_m13_liveness", BJ_DISPOSITION_SHA256)
	with pytest.raises(ValueError, match="disposition"):
		materialize_in_memory(fixture_files(), GROUPS, SKIP, "BY_crawl_safety", "0" * 64)
	with pytest.raises(ValueError, match="canonical AI path"):
		materialize_in_memory({"mods/cameo/ai/../ai.yaml": AI_TEXT}, GROUPS, SKIP, "BY_crawl_safety", BJ_DISPOSITION_SHA256)


def test_duplicate_candidate_field_and_changed_generic_gate_are_refused():
	duplicate = AI_TEXT.replace("\t\tUseCrawlSafety: true", "\t\tUseCrawlSafety: true\n\t\tUseCrawlSafety: false")
	with pytest.raises(ValueError, match="duplicate switch field"):
		materialize_in_memory(fixture_files(duplicate), GROUPS, SKIP, "BY_crawl_safety", BJ_DISPOSITION_SHA256)
	wrong_gate = AI_TEXT.replace("\t\tSwitchCondition: genericbot", "\t\tSwitchCondition: classicbot", 1)
	with pytest.raises(ValueError, match="genericbot"):
		materialize_in_memory(fixture_files(wrong_gate), GROUPS, SKIP, "BZ_production_scaling", BJ_DISPOSITION_SHA256)


def test_treatment_delta_outside_selected_group_is_refused(monkeypatch):
	from tools.ai import exploratory_switch_screen as screen

	original = screen._replace_group
	def drift(text, group, skip, off):
		result = original(text, group, skip, off)
		if group is GROUPS["BY_crawl_safety"] and not off:
			return result.replace("UseProductionScaling: false", "UseProductionScaling: true", 1)
		return result
	monkeypatch.setattr(screen, "_replace_group", drift)
	with pytest.raises(ValueError, match="outside selected candidate"):
		materialize_in_memory(fixture_files(), GROUPS, SKIP, "BY_crawl_safety", BJ_DISPOSITION_SHA256)


def test_shared_source_drift_is_refused_even_when_both_arms_match(monkeypatch):
	from tools.ai import exploratory_switch_screen as screen

	original = screen._replace_group
	def drift(text, group, skip, off):
		result = original(text, group, skip, off)
		if group is GROUPS["CB_frontier_observe"] and not off:
			return result.replace("RequiresCondition: genericbot", "RequiresCondition: classicbot", 1)
		return result
	monkeypatch.setattr(screen, "_replace_group", drift)
	with pytest.raises(ValueError, match="outside allowed fields"):
		materialize_in_memory(fixture_files(), GROUPS, SKIP, "BY_crawl_safety", BJ_DISPOSITION_SHA256)


def test_plan_and_schema_remain_non_executable_and_unbound():
	import json
	from pathlib import Path

	root = Path(__file__).resolve().parents[2]
	schema = json.loads((root / "tools/ai/exploratory_switch_screen.schema.json").read_text(encoding="utf-8"))
	assert schema["properties"]["execution_authorized"] == {"const": False}
	assert schema["properties"]["caps"] == {"const": "UNBOUND_PENDING_SCREEN_QUALIFICATION"}
	assert len(plan_cells()) == 24
