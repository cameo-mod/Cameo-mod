"""Virtual anchors reuse final Formula V2 and never imply gameplay approval."""
import contextlib
import io
import json
import pathlib
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "balance"))
import derive_virtual_anchor as tool
import fit_class
import formula


def member(actor, hp=100000, faction="tiberiandawn_gdi"):
    unit = dict(hp={"v": hp}, speed={"v": 100}, cost={"v": 800}, armaments=[
        dict(pricing=True, range=5000, reloaddelay=20, burst=1,
             damage_warheads=[dict(type="AreaDamage", damage=600)])])
    return dict(actor=actor, cls="mbt", faction=faction, hp=hp, speed=100,
                range_wdist=5000, cost=800, unit=unit)


class VirtualAnchorTests(unittest.TestCase):
    def test_final_identity_and_verifier_not_legacy_estimators(self):
        spec = fit_class.virtual_spec("240000,95,5500,600,20,800")
        inputs = (240000, 95, 5500, 30, 1, 1, 1)
        self.assertEqual(fit_class.virtual_estimators(inputs, spec), (800, 800, 800))
        self.assertNotEqual(formula.estimators(*inputs), (800, 800, 800))
        verifier = (480000, 95, 5500, 60, 1, 1, 1)
        self.assertEqual(fit_class.virtual_price({}, {}, verifier, spec), 2000)

    def test_invalid_model_inputs(self):
        for value in ("1,2", "1,2,3,4,0,6", "1,2,3,4,-1,6", "1,2,3,4,5,nan", "1,2,3,4,5,inf"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                fit_class.virtual_spec(value)

    def test_reference_preference_and_no_dps_target(self):
        members = [member("a", 100000), member("b", 200000), member("c", 900000)]
        assignments = {"a": {"game": dict(id="TANK1", confidence="STRONG")},
                       "b": {"game": dict(id="TANK2", confidence="FAIR")},
                       "c": {"game": dict(id="TANK3", confidence="WEAK")}}
        result = tool.derive("mbt", members, assignments, model_damage=600, model_reload=20)
        self.assertEqual(result["fields"]["hp"]["value"], 150000)
        self.assertEqual(result["fields"]["hp"]["actors"], ["a", "b"])
        self.assertEqual(result["estimators"], [800, 800, 800])
        self.assertEqual(result["verifier"]["cost"], 2000)
        self.assertEqual(result["verifier"]["hp"], 300000)
        self.assertIn("UNAPPROVED", result["status"])
        self.assertNotIn("dps0", json.dumps(result))

    def test_no_source_does_not_borrow_other_class(self):
        result = tool.derive("dreadnought", [member("a")], {})
        self.assertEqual(result["status"], ["NO SOURCE"])
        self.assertNotIn("command", result)
        self.assertIsNone(result["verifier"])

    def test_bias_against_full_roster(self):
        members = [member("source", 900000)] + [member(str(i), 100000, "other") for i in range(20)]
        result = tool.derive("mbt", members, {})
        self.assertTrue(any("BIASED hp" in flag for flag in result["status"]))
        self.assertIn("THIN hp", result["status"])

    def test_constant_population_is_not_a_tail(self):
        result = tool.derive("mbt", [member(str(i)) for i in range(4)], {})
        self.assertEqual(result["fields"]["hp"]["percentile"], 50)
        self.assertFalse(any("BIASED" in flag for flag in result["status"]))

    def test_cost_is_on_coarse_nice_grid(self):
        row = member("a")
        row["cost"] = 837
        result = tool.derive("mbt", [row], {})
        self.assertEqual(result["fields"]["cost"]["value"], 800)

    def test_residuals_use_same_derived_modifiers_as_fit(self):
        row = member("a")
        row["derived"] = dict(tier_multiplier=.5, physical_state_weight=1)
        result = tool.derive("mbt", [row], {}, model_damage=600, model_reload=20)
        self.assertAlmostEqual(result["residuals"]["median"], -.375)

    def test_no_arbitrary_dps_model_or_prices_by_default(self):
        result = tool.derive("mbt", [member("a")], {})
        self.assertTrue(any("NO MODEL" in flag for flag in result["status"]))
        self.assertEqual(result["fields"]["hp"]["value"], 100000)
        self.assertNotIn("command", result)
        self.assertNotIn("residuals", result)
        self.assertIsNone(result["verifier"])

    def test_partial_model_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "both"):
            tool.derive("mbt", [member("a")], {}, model_damage=600)

    def test_missing_weapon_range_emits_no_combat_verifier(self):
        row = member("support")
        row["range_wdist"] = None
        result = tool.derive("mbt", [row], {})
        self.assertIn("MISSING range_wdist", result["status"])
        self.assertIsNone(result["verifier"])

    def test_support_never_gets_a_synthetic_weapon(self):
        row = member("apc")
        row["cls"] = "support"
        result = tool.derive("support", [row], {})
        self.assertNotIn("range_wdist", result["fields"])
        self.assertNotIn("model", result)
        self.assertIsNone(result["verifier"])

    def test_committed_population_matches_virtual_fit(self):
        members = tool.load_members(tool.ROOT / "docs/balance")
        for cls in ("mbt", "scout_vehicle", "support"):
            units, _ = fit_class.collect_units(cls, set())
            expected = {actor for actor, unit in units.items() if fit_class.eligible_virtual_member(unit)}
            self.assertEqual({m["actor"] for m in members if m["cls"] == cls}, expected)

    def test_derived_membership_and_air_speed(self):
        with tempfile.TemporaryDirectory() as directory:
            ledger = pathlib.Path(directory)
            unit = member("a")["unit"]
            unit.update(design={"subtype": "MainBattleTank"}, speed_air={"v": 150})
            del unit["speed"]
            (ledger / "test.json").write_text(json.dumps({"sections": {"units": {"a": unit}}}), encoding="utf-8")
            with patch.object(tool.class_membership, "classify", return_value=("mbt", "derived")) as classify:
                rows = tool.load_members(ledger)
            classify.assert_called_once_with(unit["design"])
            self.assertEqual(rows[0]["speed"], 150)

    def test_spawn_sibling_inclusion(self):
        with tempfile.TemporaryDirectory() as directory:
            ledger = pathlib.Path(directory)
            unit = member("a")["unit"]
            unit.update(buildable=False, design={"class_anchor": "mbt", "balance_include": True})
            (ledger / "test.json").write_text(json.dumps({"sections": {"units": {"a": unit}}}), encoding="utf-8")
            self.assertEqual(len(tool.load_members(ledger)), 1)

    def test_spec_cli_does_not_write_anchor_table(self):
        with tempfile.TemporaryDirectory() as directory:
            ledger = pathlib.Path(directory)
            anchors = ledger / "class_anchors.json"
            anchors.write_bytes(b"original\r\n")
            with patch.object(fit_class, "LEDGER", ledger), patch.object(fit_class, "ROOT", ledger), \
                    patch.object(fit_class, "ANCHORS", anchors), \
                    patch.object(fit_class, "collect_units", return_value=({}, {})), \
                    patch.object(sys, "argv", ["fit_class", "--class", "mbt", "--spec", "240000,95,5500,600,20,800"]), \
                    contextlib.redirect_stdout(io.StringIO()) as output:
                self.assertEqual(fit_class.main(), 0)
            self.assertIn("O0=800.00 P0=800.00 Q0=800.00", output.getvalue())
            self.assertEqual(anchors.read_bytes(), b"original\r\n")

    def test_virtual_k_requires_real_anchor(self):
        with patch.object(sys, "argv", ["fit_class", "--class", "mbt", "--spec", "1,1,1,100,1,1", "--use-k"]), \
                contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            fit_class.main()
