"""Focused proof checks for the R12 pure-rename writer and landed cohort."""

import json
import pathlib
import sys
import tempfile
from unittest import mock


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))

import rename_r12_compatibility_cohort as writer  # noqa: E402
from rename_r12_compatibility_cohort import resolved_dump, stale_source_references  # noqa: E402
from miniyaml import Ruleset  # noqa: E402


PROOF = json.loads((
    ROOT / "docs" / "audit" / "latest" / "r12_pure_rename_proof_20260913.json"
).read_text(encoding="utf-8"))


def test_frozen_maps_are_complete_unique_and_collision_free():
    templates = PROOF["template_renames"]
    payloads = PROOF["payload_renames"]
    assert len(templates) == len(set(templates.values())) == 36
    assert len(payloads) == len(set(payloads.values())) == 36
    assert payloads["LaserExtraDamageCompatibility"] == "LaserExtraDamage_Auxiliary"
    assert payloads["RailgunExtraDamageCompatibility"] == "RailgunExtraDamage_Auxiliary"
    assert PROOF["counts"]["source_replacements"] == 1027
    assert PROOF["resolved_dump"]["byte_identical_after_baseline_name_map"] is True


def test_current_tree_contains_only_the_renamed_cohort():
    rs = Ruleset(str(ROOT))
    templates = PROOF["template_renames"]
    payloads = PROOF["payload_renames"]
    assert not (set(templates) & set(rs.weapons))
    assert set(templates.values()) <= set(rs.weapons)
    assert stale_source_references(writer.active_sources(rs), templates, payloads) == []


def test_canonical_dump_covers_templates_and_concrete_weapons():
    rs = Ruleset(str(ROOT))
    dump = resolved_dump(rs, {})
    assert dump.startswith(b'{"')
    assert len(rs.weapons) > 2448
    assert b'Wraith_ToxinMissiles' in dump


def transaction_patches(path, *, dirty=(), rewrite_side_effect=None):
    return (
        mock.patch.object(writer, "Ruleset", return_value=object()),
        mock.patch.object(writer, "rename_maps", return_value=(
            {"^Compatibility_X": "^Warhead_X"}, {"XCompatibility": "X"}, {})),
        mock.patch.object(writer, "active_sources", return_value=[path]),
        mock.patch.object(writer, "affected_sources", return_value=[path]),
        mock.patch.object(writer, "dirty_paths", return_value=list(dirty)),
        mock.patch.object(writer, "rewrite", side_effect=rewrite_side_effect),
    )


def test_interrupted_write_restores_affected_file():
    with tempfile.TemporaryDirectory() as directory:
        path = pathlib.Path(directory) / "active.yaml"
        path.write_bytes(b"original")

        def interrupt(_paths, _replacements):
            path.write_bytes(b"partial rename")
            raise KeyboardInterrupt()

        patches = transaction_patches(path, rewrite_side_effect=interrupt)
        with patches[0], patches[1], patches[2], patches[3], patches[4], patches[5]:
            try:
                writer.run(apply=False)
            except KeyboardInterrupt:
                pass
            else:
                raise AssertionError("KeyboardInterrupt should escape after rollback")
        assert path.read_bytes() == b"original"


def test_dirty_file_refuses_before_rewrite():
    with tempfile.TemporaryDirectory() as directory:
        path = pathlib.Path(directory) / "active.yaml"
        path.write_bytes(b"original")
        patches = transaction_patches(path, dirty=(" M active.yaml",))
        with patches[0], patches[1], patches[2], patches[3], patches[4], patches[5] as rewrite:
            try:
                writer.run(apply=False)
            except RuntimeError as exc:
                assert "dirty" in str(exc)
            else:
                raise AssertionError("dirty affected source must refuse")
            rewrite.assert_not_called()


def test_proof_output_cannot_overlap_active_source():
    with tempfile.TemporaryDirectory() as directory:
        path = pathlib.Path(directory) / "active.yaml"
        path.write_bytes(b"original")
        patches = transaction_patches(path)
        with patches[0], patches[1], patches[2], patches[3], patches[4], patches[5] as rewrite:
            try:
                writer.run(apply=False, proof_path=path)
            except RuntimeError as exc:
                assert "overlaps active source" in str(exc)
            else:
                raise AssertionError("proof/source overlap must refuse")
            rewrite.assert_not_called()


if __name__ == "__main__":
    test_frozen_maps_are_complete_unique_and_collision_free()
    test_current_tree_contains_only_the_renamed_cohort()
    test_canonical_dump_covers_templates_and_concrete_weapons()
    test_interrupted_write_restores_affected_file()
    test_dirty_file_refuses_before_rewrite()
    test_proof_output_cannot_overlap_active_source()
    print("R12 rename proof fixtures: PASS")
