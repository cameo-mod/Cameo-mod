from __future__ import annotations

import pytest
from tools.ai import exploratory_switch_screen as screen

from tools.ai.exploratory_switch_screen import (
	BJ_DISPOSITION_SHA256,
	CANDIDATES,
	MAPS,
	SEEDS,
	SOURCE_FILE_SHA256,
	_expected_groups,
	_condition_implies_genericbot,
	_file_digests,
	_validate_source_gates,
	materialize_in_memory as _pinned_materialize_in_memory,
	plan_cells,
	validate_plan_cells,
	validate_plan_record,
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


def materialize_in_memory(source_files, groups, skip, candidate, bj_receipt_sha256):
	"""Exercise the public API with isolated fixture bytes in focused unit tests."""
	with pytest.MonkeyPatch.context() as patch:
		patch.setattr(screen, "SOURCE_FILE_SHA256", _file_digests(source_files))
		return _pinned_materialize_in_memory(source_files, groups, skip, candidate, bj_receipt_sha256)


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
		assert all(row["map_path"] == MAPS[map_index][4] and row["declared_map_sha256"] == MAPS[map_index][5] for row in rows)
		assert all(row["runtime_inheritance_resolution"] == "UNRESOLVED_PRECONDITION" for row in rows)
		assert all(row["effective_runtime_config_proven"] is False for row in rows)
	assert len({row["seed"] for row in cells}) == 8
	assert validate_plan_cells(cells, SEEDS, {row[0]: row[5] for row in MAPS})
	assert validate_plan_record({
		"cells": cells,
		"seed_by_map": SEEDS,
		"map_hashes": {row[0]: row[5] for row in MAPS},
	})


def test_mandatory_plan_validator_rejects_inventory_and_cross_field_drift():
	cells = plan_cells()
	map_hashes = {row[0]: row[5] for row in MAPS}
	with pytest.raises(ValueError, match="exact ordered 24-cell inventory"):
		validate_plan_cells(cells[:-1], SEEDS, map_hashes)
	duplicate = list(cells)
	duplicate[-1] = dict(duplicate[0])
	with pytest.raises(ValueError, match="exact ordered 24-cell inventory"):
		validate_plan_cells(duplicate, SEEDS, map_hashes)
	wrong_path = [dict(row) for row in cells]
	wrong_path[0]["map_path"] = MAPS[1][4]
	with pytest.raises(ValueError, match="exact ordered 24-cell inventory"):
		validate_plan_cells(wrong_path, SEEDS, map_hashes)
	wrong_digest = [dict(row) for row in cells]
	wrong_digest[0]["declared_map_sha256"] = "0" * 64
	with pytest.raises(ValueError, match="exact ordered 24-cell inventory"):
		validate_plan_cells(wrong_digest, SEEDS, map_hashes)
	wrong_identity = [dict(row) for row in cells]
	wrong_identity[0]["cell_id"] = "BZ_production_scaling|nuclear_winter"
	with pytest.raises(ValueError, match="exact ordered 24-cell inventory"):
		validate_plan_cells(wrong_identity, SEEDS, map_hashes)
	wrong_seed = [dict(row) for row in cells]
	wrong_seed[0]["seed"] += 1
	with pytest.raises(ValueError, match="exact ordered 24-cell inventory"):
		validate_plan_cells(wrong_seed, SEEDS, map_hashes)
	wrong_map_key = [dict(row) for row in cells]
	wrong_map_key[0]["map_key"] = "satans_clutch"
	with pytest.raises(ValueError, match="exact ordered 24-cell inventory"):
		validate_plan_cells(wrong_map_key, SEEDS, map_hashes)
	wrong_candidate = [dict(row) for row in cells]
	wrong_candidate[0]["candidate"] = "BJ_squad_hysteresis"
	with pytest.raises(ValueError, match="exact ordered 24-cell inventory"):
		validate_plan_cells(wrong_candidate, SEEDS, map_hashes)
	wrong_teams = [dict(row) for row in cells]
	wrong_teams[0]["factions_per_team"] = ["td_nod"]
	with pytest.raises(ValueError, match="exact ordered 24-cell inventory"):
		validate_plan_cells(wrong_teams, SEEDS, map_hashes)
	wrong_sides = [dict(row) for row in cells]
	wrong_sides[0]["control_side"] = "B"
	with pytest.raises(ValueError, match="exact ordered 24-cell inventory"):
		validate_plan_cells(wrong_sides, SEEDS, map_hashes)
	wrong_rotation = [dict(row) for row in cells]
	wrong_rotation[0]["candidate_order_on_map"] = list(reversed(wrong_rotation[0]["candidate_order_on_map"]))
	with pytest.raises(ValueError, match="exact ordered 24-cell inventory"):
		validate_plan_cells(wrong_rotation, SEEDS, map_hashes)
	with pytest.raises(ValueError, match="exact ordered 24-cell inventory"):
		validate_plan_cells(list(reversed(cells)), SEEDS, map_hashes)
	wrong_type = [dict(row) for row in cells]
	wrong_type[0]["team_size"] = True
	with pytest.raises(ValueError, match="exact ordered 24-cell inventory"):
		validate_plan_cells(wrong_type, SEEDS, map_hashes)
	wrong_map_hashes = dict(map_hashes)
	wrong_map_hashes["nuclear_winter"] = "0" * 64
	with pytest.raises(ValueError, match="top-level map_hashes"):
		validate_plan_cells(cells, SEEDS, wrong_map_hashes)
	wrong_seed_map = dict(SEEDS)
	wrong_seed_map["nuclear_winter"] += 1
	with pytest.raises(ValueError, match="independently bound seed table"):
		validate_plan_cells(cells, wrong_seed_map, map_hashes)
	with pytest.raises(ValueError, match="missing map_hashes"):
		validate_plan_record({"cells": cells, "seed_by_map": SEEDS})


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


def test_public_materializer_requires_exact_reviewed_source_hashes():
	from pathlib import Path

	actual = _file_digests(screen.load_ai_texts(Path(__file__).resolve().parents[2]))
	assert actual == SOURCE_FILE_SHA256
	with pytest.raises(ValueError, match="source AI SHA-256 mismatch"):
		_pinned_materialize_in_memory(fixture_files(), GROUPS, SKIP, "BY_crawl_safety", BJ_DISPOSITION_SHA256)


def test_duplicate_root_is_refused_as_structural_ambiguity():
	ambiguous = AI_TEXT + "\nPlayer:\n\tUnrelatedModule:\n\t\tEnabled: true\n"
	with pytest.raises(ValueError, match="duplicate YAML key creates structural ambiguity"):
		materialize_in_memory(fixture_files(ambiguous), GROUPS, SKIP, "BY_crawl_safety", BJ_DISPOSITION_SHA256)


def test_requirescondition_gate_uses_implication_not_substring():
	assert _condition_implies_genericbot("genericbot")
	assert _condition_implies_genericbot("genericbot && personality-rush")
	assert not _condition_implies_genericbot("!genericbot")
	assert not _condition_implies_genericbot("genericbot || othercondition")
	assert not _condition_implies_genericbot("xgenericbot")
	for invalid in ("!genericbot", "genericbot || othercondition"):
		bad = AI_TEXT.replace("RequiresCondition: genericbot", f"RequiresCondition: {invalid}", 1)
		with pytest.raises(ValueError, match="logically imply genericbot"):
			_validate_source_gates(fixture_files(bad), GROUPS)
	valid = AI_TEXT.replace(
		"ExpansionPlannerBotModule:\n\t\tRequiresCondition: genericbot",
		"ExpansionPlannerBotModule:\n\t\tRequiresCondition: genericbot && personality-rush",
		1,
	)
	_validate_source_gates(fixture_files(valid), GROUPS)
	# The exact shared builder form remains accepted only because the instance
	# is explicitly named @generic and has SwitchCondition: genericbot.
	assert "RequiresCondition: genericbot || classicbot" in AI_TEXT
	assert "SwitchCondition: genericbot" in AI_TEXT


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
	assert "validate_plan_record" in schema["description"]
	assert schema["properties"]["caps"] == {"const": "UNBOUND_PENDING_SCREEN_QUALIFICATION"}
	assert schema["properties"]["runtime_inheritance_resolution"] == {"const": "UNRESOLVED_PRECONDITION"}
	assert schema["properties"]["effective_runtime_config_proven"] == {"const": False}
	cell_schema = schema["properties"]["cells"]["items"]
	assert {"map_path", "declared_map_sha256", "treatment_side", "control_side", "team_size", "factions_per_team", "candidate_order_on_map"} <= set(cell_schema["required"])
	assert len(cell_schema["allOf"]) == 8  # map key binds path/hash/setup/side/order
	assert cell_schema["uniqueItems"] is True
	assert "validate_plan_cells" in cell_schema["description"]
	assert len(plan_cells()) == 24
