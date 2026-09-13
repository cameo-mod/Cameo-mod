"""Focused checks for the R12 complete-consumer reconciliation."""

import pathlib
import sys


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))

from reconcile_r12_consumer_closure import build_report  # noqa: E402


REPORT = build_report()


def test_current_inventory_includes_the_template_consumer():
    report = REPORT
    assert report["counts"] == {
        "compatibility_templates": 36,
        "all_direct_relationships": 370,
        "concrete_weapon_relationships": 369,
        "template_relationships": 1,
        "distinct_concrete_weapons": 361,
        "distinct_template_consumers": 1,
        "already_inherits_twin_all_consumers": 140,
        "exposed_without_twin_all_consumers": 214,
        "missing_twin_all_consumers": 16,
    }
    assert report["template_consumers"] == [{
        "consumer": "^Warhead_IncendiaryYakComposition",
        "consumer_kind": "template",
        "file": "mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml",
        "line": 1710,
        "compatibility_template": "^Compatibility_Flame_LightFlat",
        "matching_twin": "^Warhead_Flame_Light",
        "classification": "already_inherits_twin",
    }]


def test_literal_chain_proposal_fails_the_order_aware_gate():
    simulation = REPORT["simulation"]
    assert simulation["changed_concrete_weapons_in_full_descendant_closure"] == 290
    assert simulation["warhead_order_changes"] == 287
    assert simulation["pure_warhead_reorders"] == 101
    already = simulation["direct_consumer_groups"]["already_inherits_twin"]
    measures = (already["changed_direct_weapons"], already["warhead_order_changes"],
                already["pure_warhead_reorders"], already["with_field_deltas"])
    assert measures == (30, 30, 27, 3)


def test_pure_rename_preserves_every_resolved_weapon_and_avoids_payload_collisions():
    rename = REPORT["pure_rename_simulation"]
    assert rename["changed_concrete_weapons"] == 0
    assert rename["collision_avoidance"] == {
        "LaserExtraDamageCompatibility": "LaserExtraDamage_Auxiliary",
        "RailgunExtraDamageCompatibility": "RailgunExtraDamage_Auxiliary",
    }


def test_pure_rename_preserves_resolved_fields_and_order_without_key_collisions():
    rename = build_report()["pure_rename_simulation"]
    assert rename["changed_concrete_weapons"] == 0
    assert len(rename["template_renames"]) == 36
    assert len(rename["payload_renames"]) == 36
    assert rename["collision_avoidance"] == {
        "LaserExtraDamageCompatibility": "LaserExtraDamage_Auxiliary",
        "RailgunExtraDamageCompatibility": "RailgunExtraDamage_Auxiliary",
    }


if __name__ == "__main__":
    test_current_inventory_includes_the_template_consumer()
    test_literal_chain_proposal_fails_the_order_aware_gate()
    test_pure_rename_preserves_every_resolved_weapon_and_avoids_payload_collisions()
    test_pure_rename_preserves_resolved_fields_and_order_without_key_collisions()
    print("R12 consumer closure fixtures: PASS")
