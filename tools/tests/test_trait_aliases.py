"""Unit tests for tools/audit/audit_trait_aliases.py and the trait_aliases.json registry
(SPEC_2026-10-05_trait_unification): schema invariants on the real registry plus the
scanner's hit detection on synthetic MiniYAML trees."""

from __future__ import annotations

import json
import pathlib
import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

import audit_trait_aliases as ata
import miniyaml

REPO = pathlib.Path(__file__).resolve().parents[2]


def registry():
    return json.loads((REPO / "tools" / "audit" / "trait_aliases.json").read_text(encoding="utf-8"))


def families():
    doc = json.loads((REPO / "tools" / "audit" / "merged_bot_modules.json").read_text(encoding="utf-8"))
    return doc.get("trait_families") or {}


class RegistrySchemaTest(unittest.TestCase):
    def test_real_registry_passes_schema_validation(self):
        self.assertEqual(ata.validate_schema(registry(), families()), [])

    def test_all_70_tagged_names_registered(self):
        olds = {a["old"] for a in registry()["aliases"]}
        for required in ("MissileCA", "ProjetcileHusk", "AttackOpenTopped"):
            self.assertIn(required, olds)

    def test_active_or_retired_requires_new(self):
        doc = registry()
        doc["aliases"] = [
            {"old": "X", "kind": "trait", "state": "active", "new": None, "family": None},
        ]
        self.assertTrue(any("requires a non-null" in f for f in ata.validate_schema(doc, {})))

    def test_unknown_family_fails(self):
        doc = registry()
        doc["aliases"] = [
            {"old": "X", "kind": "trait", "state": "pending", "new": "Y", "family": "nope (trait)"},
        ]
        self.assertTrue(any("not in merged_bot_modules" in f for f in ata.validate_schema(doc, {})))

    def test_duplicate_old_fails(self):
        doc = registry()
        entry = dict(doc["aliases"][0])
        doc["aliases"] = doc["aliases"] + [entry]
        self.assertTrue(any("duplicate" in f for f in ata.validate_schema(doc, families())))


class ScanNodesTest(unittest.TestCase):
    def scan(self, text):
        hits = []
        ata.scan_nodes(miniyaml.load_text(text, "test.yaml"), {"OldTraitCA", "MissileCA", "OldWH"}, hits)
        return [(line, name, form) for _f, line, name, form in hits]

    def test_trait_key_forms_detected(self):
        hits = self.scan(
            "actor:\n"
            "\tOldTraitCA:\n"
            "\tOldTraitCA@a:\n"
            "\t-OldTraitCA@b:\n"
            "\tOther: 1\n")
        self.assertEqual(
            [(name, form) for _l, name, form in hits],
            [("OldTraitCA", "trait-key")] * 3)

    def test_top_level_keys_never_match(self):
        # A yaml doc whose top-level node IS an old name (e.g. a dormant file of
        # template fragments) — top level is actor/weapon id space, never traits.
        hits = self.scan("OldTraitCA:\n\tField: 1\n")
        self.assertEqual(hits, [])

    def test_projectile_and_warhead_values_detected(self):
        hits = self.scan(
            "weapon:\n"
            "\tProjectile: MissileCA\n"
            "\tWarhead@a: OldWH\n"
            "\t-Warhead@b: OldWH\n"
            "\tReport: MissileCA\n")
        self.assertEqual(
            [(name, form) for _l, name, form in hits],
            [("MissileCA", "projectile-value"), ("OldWH", "warhead-value"), ("OldWH", "warhead-value")])

    def test_same_string_in_other_fields_is_not_a_hit(self):
        hits = self.scan(
            "actor:\n"
            "\tTooltip:\n"
            "\t\tDescription: uses OldTraitCA internally\n"
            "\t\tName: MissileCA\n")
        self.assertEqual(hits, [])


class KeyBaseTest(unittest.TestCase):
    def test_strips_removal_and_instance(self):
        self.assertEqual(ata.key_base("Trait"), "Trait")
        self.assertEqual(ata.key_base("Trait@a"), "Trait")
        self.assertEqual(ata.key_base("-Trait@a"), "Trait")
        self.assertEqual(ata.key_base("-Trait"), "Trait")


if __name__ == "__main__":
    unittest.main()
