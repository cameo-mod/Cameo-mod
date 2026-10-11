"""Fire-port migration and audit regressions, including packed/dormant rules."""
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile
import _bootstrap
import garrison_fireports as audit
import miniyaml


class MigrationTest(unittest.TestCase):
    def test_alias_suffix_removal_geometry_and_scoped_obsolete_field(self):
        text = "actor:\n\tAttackGarrisonedSP@rear:\n\t\tPortOffsets: 1,2,3\n\t\tPortYaws: 128\n\t\tPortCones: 256\n\t\tMuzzlePalette: custom\n\t\tPerPassengerTargeting: false\n\tOther:\n\t\tPerPassengerTargeting: keep\n\t-AttackOpenTopped@front:\n"
        migrated, changes = audit.migrate_text(text, "fixture.yaml")
        self.assertEqual(len(changes), 3)
        self.assertIn("-AttackGarrisoned@front:", migrated)
        for line in ("PortOffsets: 1,2,3", "PortYaws: 128", "PortCones: 256", "MuzzlePalette: custom", "PerPassengerTargeting: keep"):
            self.assertIn(line, migrated)
        self.assertNotIn("PerPassengerTargeting: false", migrated)
        again, changes = audit.migrate_text(migrated, "fixture.yaml")
        self.assertEqual(again, migrated)
        self.assertEqual(changes, [])

    def test_duplicate_alias_and_canonical_instance_fail_before_write(self):
        with self.assertRaisesRegex(ValueError, "duplicate canonical trait"):
            audit.migrate_text("actor:\n\tAttackOpenTopped:\n\tAttackGarrisoned:\n", "fixture")
        audit.migrate_text("actor:\n\tAttackOpenTopped:\n\t-AttackGarrisoned:\n\tAttackGarrisoned:\n", "fixture")

    def test_dormant_and_packed_overrides_preserve_assets_and_archive_metadata(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); dormant = root / "mods/cameo/dormant/unused.yaml"
            dormant.parent.mkdir(parents=True)
            dormant.write_text("actor:\n\tAttackOpenTopped:\n\t\tPortOffsets: 0,0,0\n", encoding="utf-8")
            packed = root / "mods/cameo/test.oramap"
            with zipfile.ZipFile(packed, "w") as archive:
                archive.comment = b"preserved comment"
                info = zipfile.ZipInfo("map.yaml", date_time=(2020, 1, 2, 3, 4, 6)); info.compress_type = zipfile.ZIP_DEFLATED
                archive.writestr(info, "Rules:\n\tactor:\n\t\tAttackGarrisonedSP@rear:\n")
                archive.writestr("map.bin", bytes(range(256)))
            edits = audit.migrate(root, True)
            self.assertEqual(len(edits), 2)
            self.assertEqual(audit.legacy_uses(root), [])
            self.assertEqual(audit.migrate(root, True), [])
            with zipfile.ZipFile(packed) as archive:
                self.assertEqual(archive.comment, b"preserved comment")
                self.assertEqual(archive.read("map.bin"), bytes(range(256)))
                self.assertEqual(archive.getinfo("map.yaml").date_time, (2020, 1, 2, 3, 4, 6))
                self.assertEqual(archive.getinfo("map.yaml").compress_type, zipfile.ZIP_DEFLATED)
                self.assertIn(b"AttackGarrisoned@rear:", archive.read("map.yaml"))
            self.assertTrue(all("input_sha256" in edit and "output_sha256" in edit for edit in edits))

    def test_idempotent_cli_preserves_first_manifest(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); (root / "mods").mkdir(); manifest = root / "manifest.json"
            original = {"version": 1, "changes": [{"source": "first.yaml", "line": 3}]}
            manifest.write_text(json.dumps(original), encoding="utf-8")
            before = manifest.read_bytes()
            with patch("sys.argv", ["audit", "--root", tmp, "--write", "--manifest", str(manifest)]), patch.object(audit, "mounted_capacity", return_value=([], [])):
                self.assertEqual(audit.main(), 0)
            self.assertEqual(manifest.read_bytes(), before)

    def test_capacity_optional_geometry_and_explicit_no_fire(self):
        class Rules:
            def __init__(self, text): self.actors = {n.key: n for n in miniyaml.load_text(text)}
            def actor(self, name): return self.actors.get(name)
            def resolve(self, name): return self.actors[name]
        base = "actor:\n\tCargo:\n\t\tMaxWeight: 3\n\tAttackGarrisoned:\n\t\tPortOffsets: 0,0,0, 1,2,3\n"
        rows, errors = audit.mounted_capacity(Path('.'), Rules(base))
        self.assertEqual(rows[0]['ports'], 2)
        self.assertTrue(any('capacity 3 exceeds 2 ports' in e for e in errors))
        _, errors = audit.mounted_capacity(Path('.'), Rules(base + "\t\tNoFireOverflow: true\n"))
        self.assertEqual(errors, [])
        _, errors = audit.mounted_capacity(Path('.'), Rules(base + "\t\tPortCones: 128\n"))
        self.assertTrue(any('PortCones length' in e for e in errors))


if __name__ == '__main__':
    unittest.main()
