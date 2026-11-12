"""Offline switch budget checks; subprocess tests never launch a game."""
import hashlib
import json
import pathlib
import subprocess
import sys
import tempfile
import unittest
from collections import Counter

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/ai"))
import apply_increment_switches as switches
import tune_build_order


class RestraintBudgetTests(unittest.TestCase):
    def test_reviewed_catalogue_covers_every_group(self):
        _, groups, _ = switches.load_spec(switches.SPEC)
        classes = switches.load_effect_classes(switches.SPEC, groups)
        self.assertEqual(Counter(classes.values()), {"restraint": 35, "capability": 27, "neutral": 13})
        self.assertEqual(classes["TF4_turtle_fallback_expiry"], "restraint")
        self.assertEqual(classes["BP_squad_desire"], "restraint")
        self.assertEqual(classes["AE_army_first"], "restraint")
        self.assertEqual(classes["BT_expansion_prebuild"], "restraint")
        self.assertEqual(classes["BV_m13_liveness"], "restraint")
        self.assertEqual(classes["CB_frontier_observe"], "neutral")
        self.assertEqual(classes["BR_learn_fight"], "restraint")
        self.assertEqual(classes["CD_stalemate_breaker"], "restraint")
        self.assertEqual(classes["CF_agg_core"], "restraint")
        _, _, needs = switches.load_spec(switches.SPEC)
        self.assertEqual(needs["CF_agg_core"], ["CD_stalemate_breaker"])
        self.assertEqual(groups["CF_agg_core"], {"SquadManagerBotModuleCA": {"AggressionEnabled": "true"}})
        self.assertEqual(classes["CE_botpassive_diagnostics"], "neutral")
        self.assertEqual(classes["CG_agg_team"], "capability")
        self.assertEqual(classes["CH_retreat_rally"], "restraint")
        self.assertEqual(needs["CH_retreat_rally"], ["CG_agg_team", "CF_agg_core"])
        self.assertEqual(groups["CH_retreat_rally"], {"SquadManagerBotModuleCA": {"RetreatRallyEnabled": "true"}})
        self.assertEqual(classes["CI_refinery_stall_fix"], "capability")
        self.assertEqual(needs.get("CI_refinery_stall_fix", []), [])
        self.assertEqual(groups["CI_refinery_stall_fix"], {"ExpansionPlannerBotModule": {"RefineryFallbackForceAfterTicks": "1500"}})

    def test_class_metadata_fails_closed(self):
        for text in ("", "effect_classes:\n  a: wrong\n", "effect_classes:\n  b: neutral\n",
                     "effect_classes:\n  a: neutral\n  a: restraint\n",
                     "effect_classes:\n  a: neutral\neffect_classes:\n  a: neutral\n"):
            with self.subTest(text=text), tempfile.TemporaryDirectory() as directory:
                path = pathlib.Path(directory) / "spec.yaml"
                path.write_text(text, encoding="utf-8")
                with self.assertRaises(ValueError):
                    switches.load_effect_classes(path, {"a": {}})

    def test_one_new_restraint_with_capabilities(self):
        result = switches.restraint_budget(["a", "c"], {"a": "restraint", "c": "capability"})
        self.assertTrue(result["ordinary_increment_allowed"])
        self.assertEqual(result["new_restraint_groups"], ["a"])

    def test_two_new_restraints_refused_but_existing_baseline_does_not_count(self):
        classes = {"a": "restraint", "b": "restraint"}
        self.assertFalse(switches.restraint_budget(["a", "b"], classes)["ordinary_increment_allowed"])
        self.assertEqual(switches.restraint_budget(["a", "b"], classes, {"a": "restraint"})["new_restraint_count"], 1)

    def test_reclassification_needs_note_and_counts_as_new(self):
        with self.assertRaises(ValueError):
            switches.restraint_budget(["a"], {"a": "restraint"}, {"a": "capability"})
        result = switches.restraint_budget(["a"], {"a": "restraint"}, {"a": "capability"}, "receipt: reviewed reclassification")
        self.assertEqual(result["new_restraint_count"], 1)
        self.assertEqual(result["reclassified_groups"], ["a"])

    def test_duplicates_unknown_and_invalid_baseline(self):
        for names, baseline in ((["a", "a"], {}), (["missing"], {}), (["a"], {"missing": "neutral"}),
                                (["a"], {"a": "invalid"})):
            with self.subTest(names=names, baseline=baseline), self.assertRaises(ValueError):
                switches.restraint_budget(names, {"a": "neutral"}, baseline)

    def test_generated_learning_arms_keep_strict_metadata(self):
        with tempfile.TemporaryDirectory() as directory:
            path = pathlib.Path(directory) / "generated.yaml"
            path.write_text(tune_build_order.switches_text(["base", "plus"]), encoding="utf-8")
            _, groups, _ = switches.load_spec(path)
            classes = switches.load_effect_classes(path, groups)
            self.assertEqual(classes, {"base": "restraint", "plus": "restraint"})
            self.assertFalse(switches.restraint_budget(list(groups), classes)["ordinary_increment_allowed"])


class ManifestAndCliTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.tree = pathlib.Path(self.temp.name)
        ai = self.tree / "mods/cameo/ai"
        ai.mkdir(parents=True)
        self.ai = ai / "ai.yaml"
        self.ai.write_text("Bot:\n\tExampleTrait:\n\t\tFirst: false\n\t\tSecond: false\n", encoding="utf-8")
        self.spec = self.tree / "spec.yaml"
        self.spec.write_text("skip: []\neffect_classes:\n  a: restraint\n  b: restraint\ngroups:\n  a:\n    ExampleTrait:\n      First: true\n  b:\n    ExampleTrait:\n      Second: true\n", encoding="utf-8")

    def cli(self, *args):
        return subprocess.run([sys.executable, str(ROOT / "tools/ai/apply_increment_switches.py"),
                               str(self.tree), "--spec", str(self.spec), *args], capture_output=True, text=True)

    def manifest_args(self, **changes):
        value = {"version": 1, "approval_receipt": "external-review.md", "baseline_classes": {},
                 "selected_groups": ["a", "b"], "purpose": "combination_test", "campaign_allowed": False}
        value.update(changes)
        self.manifest = self.tree / "manifest.json"
        self.manifest.write_text(json.dumps(value), encoding="utf-8")
        digest = hashlib.sha256(self.manifest.read_bytes()).hexdigest()
        return ["--increment-manifest", str(self.manifest), "--manifest-sha256", digest]

    def test_cli_all_refuses_before_mutation_even_dry_run(self):
        before = self.ai.read_bytes()
        for tail in ([], ["--dry-run"]):
            result = self.cli("--groups", "all", *tail)
            self.assertEqual(result.returncode, 2, result.stderr)
            self.assertIn("restraint budget exceeded", result.stderr)
            self.assertEqual(self.ai.read_bytes(), before)

    def test_single_group_dry_run_preserves_then_apply_changes_only_selected(self):
        before = self.ai.read_bytes()
        result = self.cli("--groups", "a", "--dry-run")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.ai.read_bytes(), before)
        result = self.cli("--groups", "a")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('"campaign_allowed": null', result.stdout)
        self.assertIn("First: true", self.ai.read_text())
        self.assertIn("Second: false", self.ai.read_text())

    def test_exact_combination_manifest_not_campaign_authorization(self):
        result = self.cli("--groups", "a,b", *self.manifest_args())
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('"campaign_allowed": false', result.stdout)
        self.assertIn("Second: true", self.ai.read_text())

    def test_combination_manifest_campaign_true_refused(self):
        before = self.ai.read_bytes()
        result = self.cli("--groups", "a,b", *self.manifest_args(campaign_allowed=True))
        self.assertEqual(result.returncode, 2, result.stderr)
        self.assertEqual(self.ai.read_bytes(), before)

    def test_forged_baseline_cannot_hide_new_restraint(self):
        _, groups, _ = switches.load_spec(self.spec)
        args = self.manifest_args(purpose="ordinary_increment", baseline_classes={"a": "restraint"},
                                  baseline_patch_sha256={"a": switches.group_patch_sha256(groups["a"])})
        result = self.cli("--groups", "a,b", *args)
        self.assertEqual(result.returncode, 2, result.stderr)
        self.assertIn("not already armed", result.stderr)
        self.ai.write_text(self.ai.read_text().replace("First: false", "First: true"))
        result = self.cli("--groups", "a,b", *args)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('"new_restraint_count": 1', result.stdout)

    def test_changed_baseline_policy_pin_refused(self):
        self.ai.write_text(self.ai.read_text().replace("First: false", "First: true"))
        args = self.manifest_args(purpose="ordinary_increment", baseline_classes={"a": "restraint"},
                                  baseline_patch_sha256={"a": "0" * 64})
        before = self.ai.read_bytes()
        result = self.cli("--groups", "a,b", *args)
        self.assertEqual(result.returncode, 2)
        self.assertIn("baseline policy changed", result.stderr)
        self.assertEqual(self.ai.read_bytes(), before)

    def test_unmet_dependency_refuses_before_mutation(self):
        self.spec.write_text(self.spec.read_text() + "needs:\n  a: [b]\n")
        before = self.ai.read_bytes()
        result = self.cli("--groups", "a")
        self.assertEqual(result.returncode, 2)
        self.assertIn("unmet switch dependencies", result.stderr)
        self.assertEqual(self.ai.read_bytes(), before)

    def test_manifest_digest_order_and_duplicates_fail(self):
        args = self.manifest_args()
        result = self.cli("--groups", "b,a", *args)
        self.assertEqual(result.returncode, 2)
        self.manifest.write_text(self.manifest.read_text() + " ")
        with self.assertRaises(ValueError):
            switches.verified_manifest(self.manifest, args[-1])
        self.manifest.write_text('{"version":1,"version":1,"approval_receipt":"review"}')
        with self.assertRaises(ValueError):
            switches.verified_manifest(self.manifest, hashlib.sha256(self.manifest.read_bytes()).hexdigest())

    def test_manifest_requires_both_pin_and_path_and_bounds_read(self):
        for path, digest in ((None, "0" * 64), (self.tree / "missing", None)):
            with self.assertRaises(ValueError):
                switches.verified_manifest(path, digest)
        self.manifest_args()
        self.manifest.write_bytes(b" " * 65537)
        with self.assertRaises(ValueError):
            switches.verified_manifest(self.manifest, hashlib.sha256(self.manifest.read_bytes()).hexdigest())

    def test_missing_class_spec_and_duplicate_selection_refuse(self):
        before = self.ai.read_bytes()
        result = self.cli("--groups", "a,a")
        self.assertEqual(result.returncode, 2)
        self.spec.write_text(self.spec.read_text().replace("  a: restraint\n", ""))
        result = self.cli("--groups", "a")
        self.assertEqual(result.returncode, 2)
        self.assertEqual(self.ai.read_bytes(), before)


if __name__ == "__main__":
    unittest.main()


class RetreatRallyGroupTests(unittest.TestCase):
    """A10 CH_retreat_rally: needs CG_agg_team AND CF_agg_core (design DRAFT02 M1); the applier refuses any arm lacking one."""

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.tree = pathlib.Path(self.temp.name)
        (self.tree / "mods/cameo/ai").mkdir(parents=True)
        (self.tree / "mods/cameo/ai/ai.yaml").write_text("Bot:\n\tSquadManagerBotModuleCA:\n\t\tRetreatRallyEnabled: false\n", encoding="utf-8")

    def cli(self, groups, combination=True, dry=True):
        args = [sys.executable, "-I", "-B", str(ROOT / "tools/ai/apply_increment_switches.py"), str(self.tree), "--groups", ",".join(groups)]
        if combination:
            manifest = self.tree / "manifest.json"
            manifest.write_text(json.dumps({"version": 1, "approval_receipt": "external-review.md", "baseline_classes": {},
                                            "selected_groups": groups, "purpose": "combination_test", "campaign_allowed": False}), encoding="utf-8")
            args += ["--increment-manifest", str(manifest), "--manifest-sha256", hashlib.sha256(manifest.read_bytes()).hexdigest()]
        if dry:
            args.append("--dry-run")
        return subprocess.run(args, capture_output=True, text=True)

    def test_ch_alone_is_refused_for_both_dependencies(self):
        result = self.cli(["CH_retreat_rally"], combination=False)
        self.assertEqual(result.returncode, 2, result.stdout + result.stderr)
        self.assertIn("unmet switch dependencies", result.stderr)
        self.assertIn("CG_agg_team", result.stderr)
        self.assertIn("CF_agg_core", result.stderr)

    def test_ch_without_cf_is_refused(self):
        result = self.cli(["CD_stalemate_breaker", "CG_agg_team", "CH_retreat_rally"])
        self.assertEqual(result.returncode, 2, result.stdout + result.stderr)
        self.assertIn("unmet switch dependencies", result.stderr)
        self.assertIn("CF_agg_core", result.stderr)
        self.assertNotIn("'CG_agg_team'", result.stderr.split("unmet switch dependencies")[1])

    def test_ch_without_cg_is_refused(self):
        result = self.cli(["CD_stalemate_breaker", "CF_agg_core", "CH_retreat_rally"])
        self.assertEqual(result.returncode, 2, result.stdout + result.stderr)
        self.assertIn("unmet switch dependencies", result.stderr)
        self.assertIn("CG_agg_team", result.stderr)
        self.assertNotIn("'CF_agg_core'", result.stderr.split("unmet switch dependencies")[1])

    def test_ch_with_cd_cf_cg_is_accepted_and_sets_only_the_flag(self):
        result = self.cli(["CD_stalemate_breaker", "CF_agg_core", "CG_agg_team", "CH_retreat_rally"])
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertRegex(result.stdout, r"SquadManagerBotModuleCA\.RetreatRallyEnabled: .*false -> true")
        self.assertIn("4 change(s) (dry run)", result.stdout)
        _, groups, _ = switches.load_spec(switches.SPEC)
        self.assertEqual(groups["CH_retreat_rally"], {"SquadManagerBotModuleCA": {"RetreatRallyEnabled": "true"}})

    def test_ch_is_not_in_the_arm07_pair(self):
        # ARM07: ON = CD+CE+CF+CG, OFF = CE. The spec's group order never names CH inside CD..CG; CH is only ever armed on its own request.
        _, groups, needs = switches.load_spec(switches.SPEC)
        for name in ("CD_stalemate_breaker", "CE_botpassive_diagnostics", "CF_agg_core", "CG_agg_team"):
            self.assertNotIn("CH_retreat_rally", needs.get(name, []))
            self.assertNotIn("RetreatRallyEnabled", groups[name].get("SquadManagerBotModuleCA", {}))
