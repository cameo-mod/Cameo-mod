#!/usr/bin/env python3
"""Pure planner and in-memory arm normalizer for the 24-cell exploratory screen.

This module does not write payloads, manifests, maps, or receipts and has no
launch/runtime entry point. Execution requires a separately reviewed adapter.
"""
from __future__ import annotations

import hashlib
import itertools
import json
import os
import pathlib
import re
from dataclasses import dataclass
from typing import Mapping

from tools.ai import apply_increment_switches as switch_applier
from tools.audit import miniyaml


DESIGN_SHA256 = "2eb55ab553ef264049cef432030c56ede34baf889e90ba4ac0aaafe89cecc0e6a"
PACKET_SHA256 = "98bf91b9aa485d40325d2542807e2f8b18645810f86913dfa62bdd4368e14766"
BINDING_SHA256 = "9753a5a4b646126f94c814005c4bedeece671ccef909248783496eb1a76bf954"
BJ_DISPOSITION_SHA256 = "4feb6f4ef2238da33023da1e12401b89a3a2d40becf36e5acacd7103ddfff681"

CANDIDATES = ("BY_crawl_safety", "BZ_production_scaling", "BJ_squad_hysteresis")
COMMON_GROUP = "CB_frontier_observe"
EXPECTED_AI_FILES = frozenset({
	"mods/cameo/ai/ai.yaml",
	"mods/cameo/ai/fransbot.yaml",
	"mods/cameo/ai/fransbot_lists.yaml",
})
SEEDS = {
	"nuclear_winter": 2026101601,
	"satans_clutch": 2026101602,
	"red_spice": 2026101603,
	"terra_cotta": 2026101604,
	"back_to_basics": 2026101605,
	"winters_end_rich": 2026101606,
	"great_sahara_2": 2026101607,
	"ice_cold": 2026101608,
}

# Order and declared map inventory from the reviewed plan. These values are
# planning metadata; maps still require fresh byte/render acceptance.
MAPS = (
	("nuclear_winter", "1v1_nuclear_gdi", 1, ("td_gdi",), "mods/cameo/maps/_ra_a-nuclear-winter.oramap", "191efefe3043acb2fd504b2e2e5c974a49f6498f8e016c1380159c9f7efc1c4a"),
	("satans_clutch", "1v1_satans_allies", 1, ("ra1_allies",), "mods/cameo/maps/SatansClutch.oramap", "1bd3bee0bbb38c95015126ba689496c4284c2f4e8f8b6573711a9fdadcbf16cb"),
	("red_spice", "2v2_red_gdi_nod", 2, ("td_gdi", "td_nod"), "mods/cameo/maps/Red_Spice_2v2_BI-4.4.oramap", "882f865b4e556f93324bbd1abb31397bff30b651489652fa151c031cf02cb65a"),
	("terra_cotta", "2v2_terra_nod_allies", 2, ("td_nod", "ra1_allies"), "mods/cameo/maps/Terracotta-ratls2.oramap", "c1a66fba029ccde5998fbe57b563f87569425ace82617e42e6e42e3fcc9e740d"),
	("back_to_basics", "3v3_basics_gna", 3, ("td_gdi", "td_nod", "ra1_allies"), "mods/cameo/maps/back-to-basics.oramap", "ddd071d131895ecb59be66b7d1924b023731c52a04ee01052fa9fc384dd15e6b"),
	("winters_end_rich", "3v3_winter_asg", 3, ("ra1_allies", "ra1_soviets", "td_gdi"), "mods/cameo/maps/winters-end-rich.oramap", "a29eea3fef3087d5544c428d2d07d59f4430806c0b23db7eeb605dde7013c94e"),
	("great_sahara_2", "4v4_sahara", 4, ("td_gdi", "td_nod", "ra1_allies", "ra1_soviets"), "mods/cameo/maps/Great_Sahara_3.oramap", "8b5c089c7cd2f8209819afa04436d6fa15849e125823ddc16bf5e996cd280ddc"),
	("ice_cold", "4v4_ice", 4, ("td_gdi", "td_nod", "ra1_allies", "ra1_soviets"), "mods/cameo/maps/ice_cold.oramap", "e097fcca4fa40f5878b4fb7d965529728dbbd34c2a14b8825e1bec0aea6da98b"),
)

ROTATIONS = (
	("BY_crawl_safety", "BZ_production_scaling", "BJ_squad_hysteresis"),
	("BZ_production_scaling", "BJ_squad_hysteresis", "BY_crawl_safety"),
	("BJ_squad_hysteresis", "BY_crawl_safety", "BZ_production_scaling"),
)

@dataclass(frozen=True)
class ArmPayloads:
	"""Raw-YAML normalization pair; this is not effective runtime config proof."""

	control: dict[str, str]
	treatment: dict[str, str]
	source_file_sha256: dict[str, str]
	source_payload_sha256: str
	control_file_sha256: dict[str, str]
	treatment_file_sha256: dict[str, str]
	control_sha256: str
	treatment_sha256: str
	control_semantic_sha256: str
	treatment_semantic_sha256: str
	runtime_inheritance_resolution: str = "UNRESOLVED_PRECONDITION"
	effective_runtime_config_proven: bool = False


def plan_cells(seed_by_map: Mapping[str, int] = SEEDS) -> list[dict]:
	"""Return the exact 24 map-major cells; this is not a launch manifest."""
	if set(seed_by_map) != {row[0] for row in MAPS}:
		raise ValueError("seed map keys must exactly match the eight declared maps")
	for key, seed in seed_by_map.items():
		if type(seed) is not int or not 0 <= seed <= 2**31 - 1:
			raise ValueError(f"invalid signed-32-bit seed for {key}")

	cells = []
	for index, (map_key, setup, team_size, factions, map_path, map_sha) in enumerate(MAPS):
		rotation = ROTATIONS[index % len(ROTATIONS)]
		for candidate in rotation:
			treatment_side = "B" if index % 2 == 0 else "A"
			cells.append({
				"cell_id": f"{candidate}|{map_key}",
				"candidate": candidate,
				"map_key": map_key,
				"setup": setup,
				"team_size": team_size,
				"factions_per_team": list(factions),
				"map_path": map_path,
				"declared_map_sha256": map_sha,
				"seed": seed_by_map[map_key],
				"candidate_order_on_map": list(rotation),
				"treatment_side": treatment_side,
				"control_side": "A" if treatment_side == "B" else "B",
				"actual_seats": None,
				"caps": None,
				"execution_authorized": False,
				"runtime_inheritance_resolution": "UNRESOLVED_PRECONDITION",
				"effective_runtime_config_proven": False,
			})
	if len(cells) != 24 or len({row["cell_id"] for row in cells}) != 24:
		raise AssertionError("internal cell inventory is not exactly 24 unique cells")
	return cells


def _validate_source_files(source_files: Mapping[str, str]) -> dict[str, str]:
	if not isinstance(source_files, Mapping) or not source_files:
		raise ValueError("source AI files must be a nonempty path-to-text mapping")
	validated = {}
	folded_paths = set()
	for raw_path, text in source_files.items():
		if not isinstance(raw_path, str) or not isinstance(text, str):
			raise ValueError("AI file paths and contents must be strings")
		path = pathlib.PurePosixPath(raw_path)
		if path.is_absolute() or ".." in path.parts or path.as_posix() != raw_path:
			raise ValueError(f"non-canonical AI path: {raw_path!r}")
		if len(path.parts) != 4 or path.parts[:3] != ("mods", "cameo", "ai") or path.suffix != ".yaml":
			raise ValueError(f"path outside mods/cameo/ai/*.yaml: {raw_path!r}")
		if raw_path.casefold() in folded_paths:
			raise ValueError(f"duplicate/case-colliding AI path: {raw_path}")
		folded_paths.add(raw_path.casefold())
		validated[raw_path] = text
	if set(validated) != EXPECTED_AI_FILES:
		raise ValueError(f"AI YAML file set differs from reviewed scope: missing={sorted(EXPECTED_AI_FILES-set(validated))}, extra={sorted(set(validated)-EXPECTED_AI_FILES)}")
	return dict(sorted(validated.items()))


def load_ai_texts(repo_root: pathlib.Path) -> dict[str, str]:
	"""Read direct AI YAML files as exact UTF-8 text without newline translation.

	Symlinks, nested directories, invalid UTF-8 and oversized files are refused.
	No file is created or changed.
	"""
	root = pathlib.Path(repo_root).absolute()
	ai_dir = root / "mods" / "cameo" / "ai"
	if not ai_dir.is_dir() or ai_dir.is_symlink():
		raise ValueError("AI source directory is missing or a symlink")
	if os.path.normcase(str(ai_dir)) != os.path.normcase(str(ai_dir.resolve())):
		raise ValueError("AI source directory resolves through a reparse/symlink path")
	files = {}
	for path in sorted(ai_dir.glob("*.yaml")):
		if path.is_symlink() or not path.is_file():
			raise ValueError(f"AI source is not a regular file: {path.name}")
		if os.path.normcase(str(path)) != os.path.normcase(str(path.resolve())):
			raise ValueError(f"AI source resolves through a reparse/symlink path: {path.name}")
		data = path.read_bytes()
		if len(data) > 4_000_000:
			raise ValueError(f"AI source exceeds size limit: {path.name}")
		try:
			text = data.decode("utf-8")
		except UnicodeDecodeError as exc:
			raise ValueError(f"AI source is not UTF-8: {path.name}") from exc
		relative = f"mods/cameo/ai/{path.name}"
		files[relative] = text
	if not files:
		raise ValueError("no direct AI YAML files found")
	return _validate_source_files(files)


def _expected_groups() -> dict[str, dict[str, dict[str, str]]]:
	return {
		"BY_crawl_safety": {"ExpansionPlannerBotModule": {"UseCrawlSafety": "true"}},
		"BZ_production_scaling": {
			"BaseBuilderBotModuleCA": {"SwitchCondition": "genericbot", "UseProductionScaling": "true"},
			"ExpansionPlannerBotModule": {"UseProductionScaling": "true"},
		},
		"BJ_squad_hysteresis": {
			"SquadManagerBotModuleCA": {"UseFormationHysteresis": "true", "FormationHoldHysteresisCells": "2"},
		},
		"CB_frontier_observe": {"ExpansionPlannerBotModule": {"UseFrontierObservation": "true"}},
	}


def _validate_registry(groups: dict, skip: list[str], bj_receipt_sha256: str) -> None:
	if {name: groups.get(name) for name in (*CANDIDATES, COMMON_GROUP)} != _expected_groups():
		raise ValueError("candidate/common registry patches differ from reviewed design")
	if "SquadManagerBotModuleCA@classic" not in skip:
		raise ValueError("classic squad-manager skip is missing")
	if not isinstance(bj_receipt_sha256, str) or bj_receipt_sha256.lower() != BJ_DISPOSITION_SHA256:
		raise ValueError("exact screen-only BJ disposition receipt is required")


def _replace_group(text: str, group: dict[str, dict[str, str]], skip: list[str], off: bool) -> str:
	result = text
	for trait, fields in group.items():
		values = {}
		for field, value in fields.items():
			if field == "SwitchCondition":
				values[field] = "genericbot"
			elif field in {"UseCrawlSafety", "UseProductionScaling", "UseFormationHysteresis"}:
				values[field] = "false" if off else value
			else:
				# BJ's deadband is a shared constant in both arms.
				values[field] = value
		result, _changes = switch_applier.apply(result, trait, values, skip)
	return result


def _apply_named_instance(text: str, full_key: str, fields: dict[str, str]) -> str:
	"""Apply fields to one exact named instance, skipping every sibling instance."""
	base, _suffix = full_key.split("@", 1)
	pattern = re.compile(rf"^\t({re.escape(base)}(?:@[A-Za-z0-9_]+)?):\s*$", re.M)
	instances = [match.group(1) for match in pattern.finditer(text)]
	if instances.count(full_key) != 1:
		raise ValueError(f"expected exactly one {full_key} instance; found {instances.count(full_key)}")
	if len(instances) != len(set(instances)):
		raise ValueError(f"duplicate {base} instance")
	ignored = [name for name in instances if name != full_key]
	updated, _changes = switch_applier.apply(text, base, fields, ignored)
	return updated


def _walk(nodes: list[miniyaml.Node], prefix: tuple[str, ...] = ()):
	for node in nodes:
		path = prefix + (node.key,)
		yield path, node
		yield from _walk(node.children, path)


def _check_relevant_duplicates(documents: Mapping[str, list[miniyaml.Node]], groups: dict) -> None:
	traits = {trait for group in groups.values() for trait in group} | {"UnitBuilderBotModuleCA"}
	fields_by_trait = {trait: {field for group in groups.values() for t, fields in group.items() if t == trait for field in fields}
		for trait in traits}
	fields_by_trait["UnitBuilderBotModuleCA"].update({"BankSaturationCash", "SwitchCondition"})
	for filename, roots in documents.items():
		for parent_path, parent in _walk(roots):
			for trait in traits:
				matches = [child for child in parent.children if child.key == trait or child.key.startswith(trait + "@")]
				keys = [child.key for child in matches]
				if len(keys) != len(set(keys)):
					raise ValueError(f"duplicate switch trait instance in {filename}:{parent.line}: {trait}")
				for instance in matches:
					for field in fields_by_trait[trait]:
						values = [child for child in instance.children if child.key == field]
						if len(values) > 1:
							raise ValueError(f"duplicate switch field in {filename}:{instance.line}: {instance.key}.{field}")


def _semantic(source_files: Mapping[str, str], groups: dict) -> dict[str, dict[tuple[str, ...], str]]:
	result = {}
	for filename, content in source_files.items():
		doc = miniyaml.load_text(content, filename)
		_check_relevant_duplicates({filename: doc}, groups)
		flat = {}
		for path, node in _walk(doc):
			flat[path] = node.value
		result[filename] = flat
	return result


def _semantic_digest(semantic: Mapping[str, Mapping[tuple[str, ...], str]]) -> str:
	serial = {filename: {"/".join(path): value for path, value in sorted(fields.items())}
		for filename, fields in sorted(semantic.items())}
	return hashlib.sha256(json.dumps(serial, sort_keys=True, separators=(",", ":")).encode("utf-8")).hexdigest()


def _raw_digest(files: Mapping[str, str]) -> str:
	digest = hashlib.sha256()
	for filename, content in sorted(files.items()):
		digest.update(filename.encode("utf-8"))
		digest.update(b"\0")
		digest.update(content.encode("utf-8"))
		digest.update(b"\0")
	return digest.hexdigest()


def _file_digests(files: Mapping[str, str]) -> dict[str, str]:
	return {filename: hashlib.sha256(content.encode("utf-8")).hexdigest()
		for filename, content in sorted(files.items())}


def _semantic_deltas(control: Mapping[str, str], treatment: Mapping[str, str], groups: dict, candidate: str) -> None:
	c = _semantic(control, groups)
	t = _semantic(treatment, groups)
	if set(c) != set(t):
		raise ValueError("control and treatment file sets differ")
	for filename in c:
		all_paths = set(c[filename]) | set(t[filename])
		for path in all_paths:
			if c[filename].get(path) == t[filename].get(path):
				continue
			if len(path) < 2:
				raise ValueError(f"unexpected treatment semantic delta at {filename}:{path}")
			trait_path = path[-2]
			base_trait = trait_path.split("@", 1)[0]
			field = path[-1]
			candidate_traits = groups[candidate]
			if base_trait not in candidate_traits or field not in candidate_traits[base_trait]:
				raise ValueError(f"treatment differs outside selected candidate at {filename}:{path}")
			if field not in {"UseCrawlSafety", "UseProductionScaling", "UseFormationHysteresis"}:
				raise ValueError(f"non-switch candidate field changed between arms at {filename}:{path}")
			if c[filename].get(path) != "false" or t[filename].get(path) != "true":
				raise ValueError(f"candidate field is not false/true at {filename}:{path}")


def _semantic_source_deltas(source: Mapping[str, str], payload: Mapping[str, str], groups: dict,
						skip: list[str]) -> None:
	source_sem = _semantic(source, groups)
	payload_sem = _semantic(payload, groups)
	if set(source_sem) != set(payload_sem):
		raise ValueError("payload file set differs from source")
	allowed = {
		("ExpansionPlannerBotModule", "UseCrawlSafety"),
		("BaseBuilderBotModuleCA", "UseProductionScaling"),
		("ExpansionPlannerBotModule", "UseProductionScaling"),
		("SquadManagerBotModuleCA", "UseFormationHysteresis"),
		("SquadManagerBotModuleCA", "FormationHoldHysteresisCells"),
		("ExpansionPlannerBotModule", "UseFrontierObservation"),
		("UnitBuilderBotModuleCA", "BankSaturationCash"),
	}
	for filename in source_sem:
		for path in set(source_sem[filename]) | set(payload_sem[filename]):
			if source_sem[filename].get(path) == payload_sem[filename].get(path):
				continue
			if len(path) < 2:
				raise ValueError(f"payload changes source outside allowed fields at {filename}:{path}")
			instance, field = path[-2], path[-1]
			base = instance.split("@", 1)[0]
			if instance in skip or (base, field) not in allowed:
				raise ValueError(f"payload changes source outside allowed fields at {filename}:{path}")
			if instance == "UnitBuilderBotModuleCA@generic" and field == "BankSaturationCash":
				continue
			if base == "UnitBuilderBotModuleCA":
				raise ValueError(f"BankSaturationCash may change only on @generic: {filename}:{path}")


def _assert_state(files: Mapping[str, str], groups: dict, selected: str) -> None:
	semantic = _semantic(files, groups)
	for group_name in CANDIDATES:
		for trait, fields in groups[group_name].items():
			for field, registry_value in fields.items():
				if field == "SwitchCondition":
					expected = "genericbot"
				elif field in {"UseCrawlSafety", "UseProductionScaling", "UseFormationHysteresis"}:
					expected = "true" if group_name == selected else "false"
				else:
					expected = registry_value
				observed = []
				for filename, fields_by_path in semantic.items():
					for path, value in fields_by_path.items():
						if (len(path) >= 2 and path[-2].split("@", 1)[0] == trait
								and path[-2] != "SquadManagerBotModuleCA@classic" and path[-1] == field):
							observed.append((filename, value))
				if not observed or any(value != expected for _filename, value in observed):
					raise ValueError(f"arm field {group_name}/{trait}/{field} expected {expected}, observed {observed}")
	for trait, fields in groups[COMMON_GROUP].items():
		for field, expected in fields.items():
			observed = [(filename, value) for filename, fields_by_path in semantic.items()
				for path, value in fields_by_path.items()
				if len(path) >= 2 and path[-2].split("@", 1)[0] == trait and path[-1] == field]
			if not observed or any(value != expected for _filename, value in observed):
				raise ValueError(f"common field {trait}/{field} expected {expected}, observed {observed}")
	bank = [(filename, value) for filename, fields_by_path in semantic.items()
		for path, value in fields_by_path.items()
		if len(path) >= 2 and path[-2] == "UnitBuilderBotModuleCA@generic" and path[-1] == "BankSaturationCash"]
	if len(bank) != 1 or bank[0][1] != "1":
		raise ValueError(f"generic BankSaturationCash must be exactly 1 in both arms, observed {bank}")
	bank_switch = [(filename, value) for filename, fields_by_path in semantic.items()
		for path, value in fields_by_path.items()
		if len(path) >= 2 and path[-2] == "UnitBuilderBotModuleCA@generic" and path[-1] == "SwitchCondition"]
	if len(bank_switch) != 1 or bank_switch[0][1] != "genericbot":
		raise ValueError(f"BankSaturationCash must remain gated to genericbot, observed {bank_switch}")


def _validate_source_gates(files: Mapping[str, str], groups: dict) -> None:
	traits = {"ExpansionPlannerBotModule", "SquadManagerBotModuleCA", "BaseBuilderBotModuleCA", "UnitBuilderBotModuleCA"}
	found = {trait: [] for trait in traits}
	for filename, content in files.items():
		doc = miniyaml.load_text(content, filename)
		for _path, node in _walk(doc):
			base = node.key.split("@", 1)[0]
			if base not in traits:
				continue
			found[base].append((filename, node))
	if any(not found[trait] for trait in traits):
		missing = sorted(trait for trait, instances in found.items() if not instances)
		raise ValueError(f"required candidate trait instances missing: {missing}")
	for trait in ("BaseBuilderBotModuleCA", "UnitBuilderBotModuleCA"):
		for filename, instance in found[trait]:
			switch_condition = instance.get("SwitchCondition")
			if switch_condition != "genericbot":
				raise ValueError(f"source {trait} must already be genericbot-gated at {filename}:{instance.line}")
	for trait in ("ExpansionPlannerBotModule", "SquadManagerBotModuleCA"):
		for filename, instance in found[trait]:
			if instance.key == "SquadManagerBotModuleCA@classic":
				continue
			condition = instance.get("RequiresCondition")
			if not condition or not _condition_implies_genericbot(condition):
				raise ValueError(f"source {trait} RequiresCondition must logically imply genericbot at {filename}:{instance.line}")
	for trait in ("BaseBuilderBotModuleCA", "UnitBuilderBotModuleCA"):
		for filename, instance in found[trait]:
			condition = instance.get("RequiresCondition")
			# These two generic-owner modules have a deliberately shared
			# RequiresCondition in the current source. SwitchCondition is the
			# independent generic-only runtime gate for that exact reviewed form.
			if condition == "genericbot || classicbot" and instance.key.endswith("@generic"):
				continue
			if not condition or not _condition_implies_genericbot(condition):
				raise ValueError(f"source {trait} RequiresCondition must imply genericbot (or use the exact reviewed generic-owner form) at {filename}:{instance.line}")


def _parse_condition(expression: str):
	"""Parse the supported boolean subset; unsupported syntax fails closed."""
	token_pattern = re.compile(r"\s*(&&|\|\||!|\(|\)|[A-Za-z_][A-Za-z0-9_-]*)")
	tokens = []
	position = 0
	while position < len(expression):
		match = token_pattern.match(expression, position)
		if not match:
			if expression[position:].strip() == "":
				break
			raise ValueError("unsupported RequiresCondition syntax")
		tokens.append(match.group(1))
		position = match.end()
	if not tokens:
		raise ValueError("empty RequiresCondition")
	index = 0

	def parse_primary():
		nonlocal index
		if index >= len(tokens):
			raise ValueError("incomplete RequiresCondition")
		token = tokens[index]
		if token == "!":
			index += 1
			return ("not", parse_primary())
		if token == "(":
			index += 1
			node = parse_or()
			if index >= len(tokens) or tokens[index] != ")":
				raise ValueError("unclosed RequiresCondition group")
			index += 1
			return node
		if token in {"&&", "||", ")"}:
			raise ValueError("invalid RequiresCondition token order")
		index += 1
		return ("atom", token)

	def parse_and():
		nonlocal index
		node = parse_primary()
		while index < len(tokens) and tokens[index] == "&&":
			index += 1
			node = ("and", node, parse_primary())
		return node

	def parse_or():
		nonlocal index
		node = parse_and()
		while index < len(tokens) and tokens[index] == "||":
			index += 1
			node = ("or", node, parse_and())
		return node

	ast = parse_or()
	if index != len(tokens):
		raise ValueError("trailing RequiresCondition tokens")
	return ast


def _condition_implies_genericbot(expression: str) -> bool:
	try:
		ast = _parse_condition(expression)
	except ValueError:
		return False
	variables = set()
	def collect(node):
		if node[0] == "atom":
			variables.add(node[1])
		elif node[0] == "not":
			collect(node[1])
		else:
			collect(node[1]); collect(node[2])
	collect(ast)
	if "genericbot" not in variables or len(variables) > 12:
		return False
	ordered = sorted(variables)
	def evaluate(node, values):
		if node[0] == "atom":
			return values[node[1]]
		if node[0] == "not":
			return not evaluate(node[1], values)
		if node[0] == "and":
			return evaluate(node[1], values) and evaluate(node[2], values)
		return evaluate(node[1], values) or evaluate(node[2], values)
	for bits in itertools.product((False, True), repeat=len(ordered)):
		values = dict(zip(ordered, bits))
		if evaluate(ast, values) and not values["genericbot"]:
			return False
	return True


def materialize_in_memory(source_files: Mapping[str, str], groups: dict, skip: list[str], candidate: str,
						bj_receipt_sha256: str) -> ArmPayloads:
	"""Create control/treatment text in memory and prove the sole intended delta.

	No path is written. Callers receive mappings that this API cannot persist.
	Inheritance and ruleset resolution are not performed: the returned record
	keeps runtime_inheritance_resolution=UNRESOLVED_PRECONDITION and
	effective_runtime_config_proven=False. This is a textual normalization
	check, never proof of the loaded/effective runtime traits.
	"""
	if candidate not in CANDIDATES:
		raise ValueError("unknown exploratory candidate")
	_validate_registry(groups, skip, bj_receipt_sha256)
	source = _validate_source_files(source_files)
	_validate_source_gates(source, groups)
	control = dict(source)
	treatment = dict(source)
	for group_name in CANDIDATES:
		for filename, content in source.items():
			control[filename] = _replace_group(control[filename], groups[group_name], skip, off=True)
			treatment[filename] = _replace_group(treatment[filename], groups[group_name], skip, off=True)
	for filename in source:
		control[filename] = _replace_group(control[filename], groups[COMMON_GROUP], skip, off=False)
		treatment[filename] = _replace_group(treatment[filename], groups[COMMON_GROUP], skip, off=False)
	for filename in source:
		treatment[filename] = _replace_group(treatment[filename], groups[candidate], skip, off=False)
	# BankSaturationCash is fixed only on the explicit generic bot instance; a
	# base-trait-wide patch could silently alter a classic or another owner.
	bank_key = "UnitBuilderBotModuleCA@generic"
	bank_matches = sum(
		1 for content in source.values()
		for _match in re.finditer(r"^\tUnitBuilderBotModuleCA@generic:\s*$", content, re.M)
	)
	if bank_matches != 1:
		raise ValueError(f"expected exactly one {bank_key}; found {bank_matches}")
	for filename in source:
		if re.search(r"^\tUnitBuilderBotModuleCA@generic:\s*$", source[filename], re.M):
			control[filename] = _apply_named_instance(control[filename], bank_key, {"BankSaturationCash": "1"})
			treatment[filename] = _apply_named_instance(treatment[filename], bank_key, {"BankSaturationCash": "1"})
	# Parse both arms and validate the single treatment delta before returning.
	control_sem = _semantic(control, groups)
	treatment_sem = _semantic(treatment, groups)
	_semantic_deltas(control, treatment, groups, candidate)
	_assert_state(control, groups, "")
	_assert_state(treatment, groups, candidate)
	_semantic_source_deltas(source, control, groups, skip)
	_semantic_source_deltas(source, treatment, groups, skip)
	return ArmPayloads(control, treatment, _file_digests(source), _raw_digest(source),
					_file_digests(control), _file_digests(treatment), _raw_digest(control), _raw_digest(treatment),
					_semantic_digest(control_sem), _semantic_digest(treatment_sem))


def load_pinned_registry(repo_root: pathlib.Path, expected_sha256: str) -> tuple[list[str], dict]:
	"""Load only an exact-hash registry; a changed registry requires fresh review."""
	path = pathlib.Path(repo_root) / "tools/ai/increment_switches.yaml"
	data = path.read_bytes()
	if (not isinstance(expected_sha256, str) or not re.fullmatch(r"[0-9a-fA-F]{64}", expected_sha256)
			or len(data) > 2_000_000 or hashlib.sha256(data).hexdigest() != expected_sha256.lower()):
		raise ValueError("switch registry hash mismatch")
	skip, groups, _needs = switch_applier.load_spec(path)
	_validate_registry(groups, skip, BJ_DISPOSITION_SHA256)
	return skip, groups

