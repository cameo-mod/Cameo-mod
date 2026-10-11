import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "ai"))
import ab_campaign_pilot as pilot
import campaign_overlay as overlay


class CampaignOverlayTests(unittest.TestCase):
    def build(self, directory):
        path = pathlib.Path(directory) / "Overlay"
        pilot.materialize_dual_arm_ai(ROOT, path, "BU_harvester_logistics")
        proof = overlay.materialize_manifest(ROOT, path)
        return path, proof

    def test_complete_manifest_preserves_includes_and_routes_only_ai_to_overlay(self):
        with tempfile.TemporaryDirectory() as temp:
            path, proof = self.build(temp)
            mod = path / "mods" / "cameo"
            text = (mod / "mod.yaml").read_text(encoding="utf-8")
            source = ROOT / "mods" / "cameo"
            self.assertGreater(len(proof["includes"]), 30)
            for include in proof["includes"]:
                self.assertEqual((source / include).read_bytes(), (mod / include).read_bytes())
            self.assertIn("ContentPacks/Outpost2/content.yaml", proof["includes"])
            self.assertEqual(["ai.yaml", "fransbot.yaml", "fransbot_lists.yaml"], proof["ai_rules"])
            self.assertNotIn("\t\t$cameo: cameo", text)
            self.assertIn("^EngineDir|../mods/cameo: cameo", text)
            self.assertRegex(text, r"(?m)^\t\t\^EngineDir\|[^:]+: campaign_ai$")
            self.assertIn("\tcameo|rules/player.yaml", text)
            self.assertNotRegex(text, r"(?m)^\tcameo\|ai/")
            ai = (mod / "ai" / "ai.yaml").read_text(encoding="utf-8")
            for bot in ("hard_control", "hard_treatment"):
                self.assertIn("Type: " + bot, ai)
            self.assertIn("Bots: hard, hard_control, hard_treatment", ai)
            self.assertIn("RequiresCondition: genericbot || classicbot", ai)

    def test_explicit_overlay_wins_after_search_path_candidates(self):
        with tempfile.TemporaryDirectory() as temp:
            path, _ = self.build(temp)
            args = overlay.launch_mod_arguments(ROOT, ROOT / "engine", path)
            self.assertEqual("Game.Mod=" + str((path / "mods" / "cameo").resolve()), args[0])
            self.assertEqual("Engine.ModSearchPaths=" + str(ROOT / "mods") + "," + str(ROOT / "engine" / "mods"), args[1])
            self.assertEqual("Engine.EngineDir=" + str(ROOT / "engine"), args[2])
            # Game.cs appends explicit mod directories after search-path candidates;
            # InstalledMods then chooses the last valid manifest of the same ID.
            candidates = [ROOT / "mods" / "cameo", pathlib.Path(args[0].split("=", 1)[1])]
            selected = None
            for candidate in candidates:
                if (candidate / "mod.yaml").exists():
                    selected = candidate
            self.assertEqual((path / "mods" / "cameo").resolve(), selected)

    def test_incomplete_payload_and_duplicate_manifest_fail_before_launch(self):
        with tempfile.TemporaryDirectory() as temp:
            path, _ = self.build(temp)
            with self.assertRaisesRegex(ValueError, "must be new"):
                overlay.materialize_manifest(ROOT, path)
            (path / "mods" / "cameo" / "mod.yaml").unlink()
            (path / "mods" / "cameo" / "ai" / "fransbot.yaml").unlink()
            with self.assertRaisesRegex(ValueError, "AI rule missing"):
                overlay.materialize_manifest(ROOT, path)
            with self.assertRaisesRegex(ValueError, "manifest missing"):
                overlay.launch_mod_arguments(ROOT, ROOT / "engine", path)

    def test_missing_source_include_fails_closed(self):
        with tempfile.TemporaryDirectory() as temp:
            root = pathlib.Path(temp) / "source"
            mod = root / "mods" / "cameo"
            mod.mkdir(parents=True)
            (mod / "mod.yaml").write_text("Include: ContentPacks/Outpost2/content.yaml\n", encoding="utf-8")
            dest = pathlib.Path(temp) / "Overlay"
            ai = dest / "mods" / "cameo" / "ai"
            ai.mkdir(parents=True)
            (ai / "ai.yaml").write_text("player:\n", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "missing baseline manifest include"):
                overlay.materialize_manifest(root, dest)
            self.assertFalse((ai.parent / "mod.yaml").exists())



if __name__ == "__main__":
    unittest.main()
