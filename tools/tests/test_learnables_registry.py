"""Contract checks for the offline learnability catalog and committed artifacts."""

from __future__ import annotations

import pathlib
import re
import unittest

import _bootstrap  # noqa: F401

import miniyaml


ROOT = pathlib.Path(__file__).resolve().parents[2]
REGISTRY = ROOT / "tools" / "ai" / "learnables.yaml"
LEARNED_DIR = ROOT / "mods" / "cameo" / "ai" / "learned"
ALLOWED_SCOPES = {
    "faction", "matchup", "doctrine", "temperament", "map_feature_vector",
    "squad_type", "unit_role", "own_role", "enemy_role", "range_band", "phase",
    "threat_role", "target_class", "power_type", "starting_temperament",
    "distance_band",
}
IDENTITY_FIELD = re.compile(
    r"(?i)(?:player_name|player_id|account(?:_id)?|client(?:_id|index)?|"
    r"game_uid|match_id|map_(?:name|uid|id|hash|author)|rating|unique_id)"
)


def child(node: miniyaml.Node, key: str) -> miniyaml.Node | None:
    return next((item for item in node.children if item.key == key), None)


def required(node: miniyaml.Node, key: str) -> str:
    found = child(node, key)
    if found is None or not found.value.strip():
        raise AssertionError(f"{node.key} is missing non-empty {key}")
    return found.value.strip()


def split_bounds(value: str) -> tuple[float, float]:
    low, sep, high = value.partition("..")
    if not sep:
        raise AssertionError(f"invalid bounds {value!r}; expected min..max")
    return float(low), float(high)


def nodes_within(root: miniyaml.Node, predicate) -> list[miniyaml.Node]:
    return [n for n in root.children if predicate(n.key)]


def numeric_leaves(node: miniyaml.Node):
    for child_node in node.children:
        if child_node.children:
            yield from numeric_leaves(child_node)
        elif child_node.value.strip():
            yield child_node


def walk(node: miniyaml.Node):
    yield node
    for child_node in node.children:
        yield from walk(child_node)


def assert_values_in_bounds(case: unittest.TestCase, values: list[str], bounds: str, label: str) -> None:
    low, high = split_bounds(bounds)
    for value in values:
        try:
            number = float(value)
        except ValueError as exc:
            raise AssertionError(f"{label} is not numeric: {value!r}") from exc
        case.assertGreaterEqual(number, low, f"{label} below {low}")
        case.assertLessEqual(number, high, f"{label} above {high}")


def registry_data() -> tuple[list[miniyaml.Node], dict[str, tuple[str, miniyaml.Node]]]:
    roots = miniyaml.load(REGISTRY)
    entries = next((node for node in roots if node.key == "learnables"), None)
    if entries is None:
        raise AssertionError("registry has no learnables mapping")

    records = []
    artifacts = {}
    for entry in entries.children:
        records.append(entry)
        artifact = child(entry, "artifact")
        if artifact is None:
            continue
        path = required(artifact, "path")
        if path in artifacts:
            raise AssertionError(f"artifact is registered more than once: {path}")
        artifacts[path] = (required(artifact, "schema"), artifact)

    return records, artifacts


class LearnabilityRegistryTests(unittest.TestCase):
    def test_registry_has_forty_complete_bounded_rows_and_known_switches(self):
        records, _ = registry_data()
        self.assertEqual(len(records), 40)
        ranks = []
        switch_text = (ROOT / "tools" / "ai" / "increment_switches.yaml").read_text(encoding="utf-8")
        known_switches = set(re.findall(r"(?m)^  ([A-Z]{2}_[a-z0-9_]+):", switch_text))

        required_fields = (
            "rank", "name", "owner", "bounds", "feature_schema",
            "credit_metric", "method", "scope_keys", "fitter",
            "sample_floor", "switch", "state",
        )
        for entry in records:
            for field in required_fields:
                required(entry, field)
            self.assertRegex(required(entry, "owner"), r"\.cs:[A-Za-z_]")
            ranks.append(int(required(entry, "rank")))
            bounds = required(entry, "bounds")
            fields = dict(part.split("=", 1) for part in bounds.split(","))
            self.assertTrue({"min", "max", "safety_floor", "unit"} <= fields.keys())
            low, high = float(fields["min"]), float(fields["max"])
            floor = float(fields["safety_floor"])
            self.assertLessEqual(low, floor)
            self.assertLessEqual(floor, high)
            self.assertGreater(int(required(entry, "sample_floor").split()[0]), 0)

            scopes = {scope.strip() for scope in required(entry, "scope_keys").split(",")}
            self.assertTrue(scopes)
            self.assertTrue(scopes <= ALLOWED_SCOPES, f"{entry.key} has disallowed scope {scopes - ALLOWED_SCOPES}")

            switch = required(entry, "switch")
            self.assertTrue(switch == "unscheduled" or switch in known_switches,
                            f"{entry.key} refers to unknown switch {switch}")

        self.assertEqual(sorted(ranks), list(range(1, 41)))

    def test_manifest_and_learned_artifacts_do_not_use_identity_fields(self):
        roots = miniyaml.load(REGISTRY)
        for root in roots:
            for node in walk(root):
                self.assertIsNone(
                    IDENTITY_FIELD.search(node.key),
                    f"identity-bearing manifest key: {node.key}",
                )
        records, _ = registry_data()
        for entry in records:
            for field in ("scope_keys", "feature_schema"):
                value = required(entry, field)
                match = IDENTITY_FIELD.search(value)
                self.assertIsNone(match, f"{entry.key} has identity feature {match.group(0) if match else ''}")

        for path in LEARNED_DIR.rglob("*.yaml"):
            nodes = miniyaml.load(path)
            for root in nodes:
                for node in walk(root):
                    self.assertIsNone(
                        IDENTITY_FIELD.search(node.key),
                        f"identity-bearing key {node.key!r} in {path.relative_to(ROOT)}",
                    )

    def test_every_learned_yaml_is_registered_and_numeric_values_are_bounded(self):
        records, artifacts = registry_data()
        discovered = {
            path.relative_to(ROOT).as_posix()
            for path in LEARNED_DIR.rglob("*.yaml")
        }
        mapped = {path for path in artifacts if (ROOT / pathlib.Path(path)).is_file()}
        self.assertEqual(discovered, mapped, f"unregistered learned yaml: {discovered - mapped}")

        for relative_path in sorted(discovered):
            schema, artifact = artifacts[relative_path]
            bounds_node = child(artifact, "storage_bounds")
            self.assertIsNotNone(bounds_node, f"{relative_path} has no storage bounds")
            bounds = {node.key: node.value.strip() for node in bounds_node.children}
            file_nodes = miniyaml.load(ROOT / pathlib.Path(relative_path))
            if schema == "arsenal_priors_v1":
                root = next(node for node in file_nodes if node.key == "BotArsenalPriors")
                for node in root.children:
                    if node.key == "VisibilityPercentByPhase":
                        assert_values_in_bounds(self, node.value.split(","), bounds["visibility_percent"], node.key)
                    elif node.key.startswith("TradePercent@"):
                        for leaf in numeric_leaves(node):
                            assert_values_in_bounds(self, [leaf.value], bounds["raw_trade_percent"], leaf.key)
                    else:
                        self.fail(f"unrecognized arsenal-prior field {node.key}")
            elif schema == "build_order_knobs_v1":
                root = next(node for node in file_nodes if node.key == "BotBuildOrderKnobs")
                for node in root.children:
                    if node.key.startswith("Knobs@"):
                        for leaf in numeric_leaves(node):
                            assert_values_in_bounds(self, [leaf.value], bounds["knob_thousandths"], leaf.key)
                    elif node.key.startswith("Openings@"):
                        for leaf in numeric_leaves(node):
                            assert_values_in_bounds(
                                self, leaf.value.split(), bounds["opening_alpha_beta"], leaf.key,
                            )
                    elif node.key == "SpsaSteps":
                        for leaf in numeric_leaves(node):
                            assert_values_in_bounds(self, [leaf.value], bounds["spsa_step"], leaf.key)
                    elif node.key not in {"ProcessedKnobs", "ProcessedOpenings"}:
                        self.fail(f"unrecognized build-order field {node.key}")
            elif schema == "plan_bandits_v1":
                root = next(node for node in file_nodes if node.key == "BotPlanBandits")
                for scope in root.children:
                    if not (scope.key.startswith("Personality@") or scope.key.startswith("Plan@")):
                        if scope.key != "Processed":
                            self.fail(f"unrecognized plan-bandit field {scope.key}")
                        continue
                    for arm in scope.children:
                        values = arm.value.split()
                        self.assertEqual(len(values), 3, f"invalid posterior for {arm.key}")
                        for value, key in zip(values, ("observations", "mean_total_milli", "m2_total_milli")):
                            assert_values_in_bounds(self, [value], bounds[key], f"{scope.key}/{arm.key}/{key}")
            elif schema == "engagement_priors_v1":
                # This catalog row may receive its first YAML in a later fit; keep its parser contract reserved.
                if not file_nodes:
                    continue
                root = next(node for node in file_nodes if node.key == "BotEngagementPriors")
                for node in root.children:
                    if node.key.startswith(("DeliveryArmour@", "DefenceState@")) or node.key in {
                        "IntoDefencesMilli", "GlobalScaleMilli", "AttritionExponentMilli",
                    }:
                        assert_values_in_bounds(
                            self, [node.value], bounds["correction_permille"], node.key,
                        )
                    elif node.key.startswith("PriorPct@"):
                        assert_values_in_bounds(
                            self, [node.value], bounds["resolved_versus_percent"], node.key,
                        )
                    elif node.key in {"Engagements", "IntoDefencesEvidence"} or node.key.startswith(
                        ("Evidence@", "DefenceStateEvidence@"),
                    ):
                        assert_values_in_bounds(self, [node.value], bounds["evidence_count"], node.key)
                    elif node.key.startswith(("AttackTiming@", "Response@")):
                        assert_values_in_bounds(
                            self, node.value.split(","), bounds["timing_ticks"], node.key,
                        )
                    elif node.key.startswith("SuicideIndex@"):
                        assert_values_in_bounds(
                            self, [node.value], bounds["suicide_index"], node.key,
                        )
                    elif node.key == "FitVersion":
                        assert_values_in_bounds(self, [node.value], bounds["fit_version"], node.key)
                    elif node.key == "StalenessTauMilli":
                        assert_values_in_bounds(
                            self, [node.value], bounds["staleness_tau_milli"], node.key,
                        )
                    elif node.key not in {"LedgerHash", "Schema"}:
                        self.fail(f"unrecognized engagement-prior field {node.key}")
            else:
                self.fail(f"unknown artifact schema {schema}")


if __name__ == "__main__":
    unittest.main()
