"""Actor-level attack cadence must reach Cameo's reference weapon model."""

import json
import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path[:0] = [str(ROOT / "tools" / "audit"), str(ROOT / "tools" / "balance")]

import armament_roles as ar  # noqa: E402
import miniyaml  # noqa: E402
import reference_distribution as rd  # noqa: E402


def armament(weapon="TestZap", damage="100", reload_delay="3"):
    return {"slot": "Armament", "weapon": weapon, "pricing": True,
            "reloaddelay": reload_delay, "burst": "1", "range": "5000",
            "damage_warheads": [{"damage": damage, "tag": "Tesla_Heavy"}]}


class RoleRules:
    def resolve_weapon(self, weapon):
        targets = "Air" if weapon.endswith("_AA") else "Ground, Water"
        return miniyaml.Node(weapon, "", [miniyaml.Node("ValidTargets", targets)])


def actor_record(actor):
    for path in sorted((ROOT / "docs" / "balance").glob("*.json")):
        if "class_anchors" in path.name:
            continue
        try:
            doc = json.loads(path.read_text(encoding="utf-8"))
        except ValueError:
            continue
        for units in (doc.get("sections") or {}).values():
            if isinstance(units, dict) and actor in units:
                return units[actor]
    raise AssertionError("missing ledger actor: " + actor)


def attack_tesla_actors():
    found = set()
    for path in sorted((ROOT / "docs" / "balance").glob("*.json")):
        if "class_anchors" in path.name:
            continue
        try:
            doc = json.loads(path.read_text(encoding="utf-8"))
        except ValueError:
            continue
        for units in (doc.get("sections") or {}).values():
            for actor, rec in (units.items() if isinstance(units, dict) else []):
                if (rec.get("charge_up") or {}).get("v") == "AttackTesla":
                    found.add(actor)
    return found


class ChargeAwareArmamentProfileTests(unittest.TestCase):
    def test_attack_tesla_uses_the_actor_cycle(self):
        charge = {"v": "AttackTesla", "ticks": 25.0,
                  "cycle_reload": 100.0, "burst": 3}
        row, debt, _ = rd.armament_profile([armament()], float, charge)
        self.assertFalse(debt)
        self.assertEqual(3, row["w_burst"])
        self.assertEqual(100.0, row["w_reload"])
        self.assertEqual(300.0, row["w_damage"])
        # 100 trait reload + 3 x (3 - 1) burst gaps + the 25-tick wind-up (maintainer law,
        # 2026-09-14: the attack cycle is reload delay + burst delays + CHARGE delay).
        self.assertAlmostEqual(300 / 131, row["w_dps"])

    def test_charge_level_traits_keep_their_reload_and_still_wait_out_the_charge(self):
        """SUPERSEDES `..._do_not_invent_a_sustained_cycle` (maintainer law, 2026-09-14).

        The old name was right about one thing and wrong about the other: a ChargeLevel trait does
        NOT replace the weapon's reload — `charge_attack_cycle` returns None and `w_reload` stays
        3 — but the unit still stands there winding up, and *"the attack cycle duration is reload
        delay plus charge delay"*. So the cadence is 3 + 25, not 3. Reading `None` as "no charge
        time at all" is the trap this test now guards.
        """
        charge = {"v": "AttackCharges", "ticks": 25.0}
        row, _debt, _ = rd.armament_profile([armament()], float, charge)
        self.assertEqual(1, row["w_burst"])
        self.assertEqual(3.0, row["w_reload"])      # the trait does NOT own the reload
        self.assertEqual(100.0, row["w_damage"])
        self.assertAlmostEqual(100 / 28, row["w_dps"])

    def test_multi_armament_actor_keeps_the_existing_fail_closed_model(self):
        charge = {"v": "AttackTesla", "ticks": 25.0,
                  "cycle_reload": 100.0, "burst": 3}
        row, _debt, _ = rd.armament_profile(
            [armament("A"), armament("B", damage="50", reload_delay="10")], float, charge)
        self.assertFalse(row["weapon_model_eligible"])
        self.assertEqual(1, row["w_burst"])
        self.assertEqual(3.0, row["w_reload"])

    def test_hidden_aa_arm_does_not_make_charge_ownership_look_unique(self):
        charge = {"v": "AttackTesla", "ticks": 25.0,
                  "cycle_reload": 100.0, "burst": 3}
        ground = armament("GroundGun")
        aa = dict(armament("AirGun_AA"), slot="Armament@AA")
        row, _debt, _ = rd.armament_profile([ground, aa], float, charge)
        self.assertTrue(row["weapon_model_eligible"])
        self.assertEqual(1, row["w_burst"])
        self.assertEqual(3.0, row["w_reload"])
        views = ar.cameo_views({"armaments": [ground, aa], "charge_up": charge}, RoleRules())
        self.assertEqual({3.0}, {view["cycle"] for view in views})

    def test_fallback_armament_does_not_receive_a_default_inactive_charge_cycle(self):
        charge = {"v": "AttackTesla", "ticks": 25.0,
                  "cycle_reload": 100.0, "burst": 3}
        deployed = dict(armament(), requires="deployed")
        row, _debt, _ = rd.armament_profile([deployed], float, charge)
        self.assertEqual(1, row["w_burst"])
        self.assertEqual(3.0, row["w_reload"])
        view = ar.cameo_views({"armaments": [deployed], "charge_up": charge}, RoleRules())[0]
        self.assertEqual(3.0, view["cycle"])


class LiveLedgerChargeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = miniyaml.Ruleset(ROOT)
        cls.rows = {row["id"]: row for row in rd.cameo_rows()}

    def test_all_attack_tesla_rows_use_the_same_actor_cycle_in_both_consumers(self):
        expected = {
            "ra1_soviets_teslacoil": (3, 100.0, 131.0, 144000.0),
            "ra2_soviets_teslacoil": (1, 75.0, 95.0, 96000.0),
            "asianalliance_railtower": (5, 120.0, 172.0, 155000.0),
        }
        self.assertEqual(set(expected), attack_tesla_actors())
        for actor, (burst, reload_delay, cycle, damage) in expected.items():
            with self.subTest(actor=actor):
                rec = actor_record(actor)
                row = self.rows[actor]
                baseline = [view for view in ar.cameo_views(rec, self.rules)
                            if view["baseline"]]
                self.assertEqual(1, len(baseline))
                view = baseline[0]
                self.assertEqual(burst, row["w_burst"])
                self.assertEqual(reload_delay, row["w_reload"])
                self.assertEqual(damage, row["w_damage"])
                self.assertAlmostEqual(damage / cycle, row["w_dps"])
                self.assertEqual(burst, view["burst"])
                self.assertEqual(cycle, view["cycle"])
                self.assertEqual(view["damage_per_cycle"], row["w_damage"])
                self.assertAlmostEqual(view["rate"], row["w_dps"])


if __name__ == "__main__":
    unittest.main()
