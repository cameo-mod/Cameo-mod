"""Transparency and drift contracts for reviewed multi-main weapons."""

from __future__ import annotations

import copy
import hashlib
import pathlib
import sys
import unittest
from unittest import mock


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path[:0] = [str(ROOT / "tools/audit")]

import audit_three_way_split as split  # noqa: E402
from audit_three_way_split import main_warhead_nodes, main_warheads  # noqa: E402
import intentional_composites as reviewed  # noqa: E402
from miniyaml import Ruleset  # noqa: E402
import survey_weapon_structure as survey  # noqa: E402
from survey_weapon_structure import OUT as INVENTORY_REPORT  # noqa: E402
from survey_weapon_structure import inventory, serialized  # noqa: E402


class IntentionalCompositeRegistryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = Ruleset(ROOT)
        cls.manifest = reviewed.load_manifest()
        cls.current = inventory(cls.rules)

    def test_exact_reviewed_name_set_and_schema_are_pinned(self):
        names = sorted(self.manifest["entries"])
        self.assertEqual(112, len(names))
        self.assertEqual(set(reviewed.curated_decisions()), set(names))
        self.assertEqual(
            "7a82f9ab5a98a805bcdb5f29a5f8bf964b3dc3cc4f0876d6cfa13dc269274344",
            hashlib.sha256(("\n".join(names) + "\n").encode()).hexdigest(),
        )
        self.assertEqual([], reviewed.validate_manifest(
            self.rules, main_warhead_nodes))
        self.assertEqual(
            self.manifest,
            reviewed.generated_manifest(self.rules, main_warhead_nodes),
        )

    def test_reviewed_weapon_is_reachable_directly_with_exact_referrer(self):
        entry = self.manifest["entries"]["TSHellfireSonic"]
        self.assertEqual("direct", entry["expected_reachability"])
        self.assertIn(
            "TSHellfireSonic",
            self.current["sets"]["reviewed_direct_actor_armament"],
        )
        self.assertEqual([{
            "kind": "actor",
            "name": "ts_gdi_orcafighter",
            "path": "Armament@Upgrade/Weapon",
        }], entry["referrers"])

    def test_every_entry_matches_its_declared_reachability_and_purpose(self):
        sets = self.current["sets"]
        actual_class = {}
        for name in sets["direct_actor_armament"]:
            actual_class[name] = "direct"
        for name in sets["indirect_weapon_graph"]:
            actual_class[name] = "indirect"
        for name in sets["unreached"]:
            actual_class[name] = "unreached"
        for name, entry in self.manifest["entries"].items():
            self.assertEqual(entry["expected_reachability"], actual_class[name], name)
            self.assertGreater(len(entry["rationale"].strip()), 20, name)
            self.assertTrue(entry["review_reference"].strip(), name)
            self.assertGreater(len(entry["overlap_justification"].strip()), 20, name)
            self.assertEqual(set(entry["mains"]), set(entry["component_purposes"]), name)
            for purpose in entry["component_purposes"].values():
                self.assertGreater(len(purpose.strip()), 20, name)

    def test_evidence_consumers_fail_closed_on_registry_validation_error(self):
        reviewed.clear_validation_cache()
        with mock.patch.object(
                reviewed, "validate_manifest", return_value=["simulated behavior drift"]):
            with self.assertRaisesRegex(ValueError, "simulated behavior drift"):
                reviewed.validated_reviewed_predicate(
                    self.rules, main_warhead_nodes)
        with mock.patch.object(
                survey, "validated_reviewed_predicate",
                side_effect=ValueError("simulated stale registry")):
            with self.assertRaisesRegex(ValueError, "simulated stale registry"):
                survey.inventory(self.rules)
        with mock.patch.object(
                split, "validated_reviewed_predicate",
                side_effect=ValueError("simulated stale registry")):
            with self.assertRaisesRegex(ValueError, "simulated stale registry"):
                split.run(self.rules)
        reviewed.clear_validation_cache()

    def test_same_pair_under_another_name_is_not_reviewed(self):
        mains = main_warheads(self.rules.resolve_weapon("TSHellfireSonic"))
        self.assertTrue(reviewed.intentional_composite("TSHellfireSonic", mains))
        self.assertFalse(reviewed.intentional_composite("CopiedHellfireSonic", mains))

    def test_behavior_and_referrer_digests_reject_drift(self):
        live = reviewed.live_snapshot(
            self.rules, "TSHellfireSonic", main_warhead_nodes)

        mains = [reviewed.node_payload(node) for node in sorted(
            main_warhead_nodes(self.rules.resolve_weapon("TSHellfireSonic")),
            key=lambda node: node.key)]
        changed_main = copy.deepcopy(mains)
        changed_main[0]["children"][0]["value"] += "-changed"
        self.assertNotEqual(live["main_digest"], reviewed.digest(changed_main))

        weapon = reviewed.node_payload(
            self.rules.resolve_weapon("TSHellfireSonic"))
        changed_weapon = copy.deepcopy(weapon)
        changed_weapon["children"][0]["value"] += "-changed"
        self.assertNotEqual(
            live["weapon_digest"], reviewed.digest(changed_weapon))

        changed_referrers = copy.deepcopy(live["referrers"])
        changed_referrers[0]["path"] += "/Changed"
        self.assertNotEqual(
            live["referrer_digest"], reviewed.digest(changed_referrers))

    def test_review_status_cannot_change_raw_structural_inventory(self):
        empty = inventory(self.rules, reviewed_predicate=lambda _name, _mains: False)
        everything = inventory(
            self.rules, reviewed_predicate=lambda _name, mains: len(mains) > 1)
        raw_keys = {
            "stacked_main_all_concrete",
            "stacked_main_direct_actor_armament",
            "stacked_main_indirect_weapon_graph",
            "stacked_main_transitive_weapon_graph",
            "stacked_main_unreached",
            "excess_main_warhead_instances_all_concrete",
            "excess_main_warhead_instances_transitive_weapon_graph",
        }
        for key in raw_keys:
            self.assertEqual(empty["counts"][key], self.current["counts"][key])
            self.assertEqual(everything["counts"][key], self.current["counts"][key])
        self.assertEqual(0, empty["counts"]["reviewed_stacked_main_all_concrete"])
        self.assertEqual(
            self.current["counts"]["stacked_main_all_concrete"],
            everything["counts"]["reviewed_stacked_main_all_concrete"],
        )

    def test_partition_arithmetic_is_complete_for_every_scope(self):
        counts = self.current["counts"]
        for suffix in (
                "all_concrete", "direct_actor_armament",
                "indirect_weapon_graph", "transitive_weapon_graph", "unreached"):
            self.assertEqual(
                counts[f"stacked_main_{suffix}"],
                counts[f"reviewed_stacked_main_{suffix}"]
                + counts[f"unreviewed_stacked_main_{suffix}"],
                suffix,
            )

    def test_generated_reports_are_fresh_and_keep_raw_counts_prominent(self):
        report, status = split.rendered(self.rules)
        self.assertEqual(0, status)
        self.assertEqual(report, split.REPORT.read_text(encoding="utf-8"))
        self.assertIn("391 raw stacked weapons", report)
        self.assertIn("279 remain unreviewed", report)
        self.assertEqual(
            serialized(self.current),
            INVENTORY_REPORT.read_text(encoding="utf-8"),
        )


if __name__ == "__main__":
    unittest.main()
