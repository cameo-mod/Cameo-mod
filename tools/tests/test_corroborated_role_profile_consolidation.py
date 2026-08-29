import pathlib
import sys
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))

from audit_three_way_split import main_warhead_nodes, main_warheads
from consolidate_corroborated_role_profiles import (
    BASELINE,
    ROOTS,
    TARGETS,
    selections,
)
from miniyaml import Ruleset
from percentage_damage import runtime_percentage_hp


class CorroboratedRoleProfileConsolidationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = Ruleset(ROOT)
        cls.selected = selections(cls.rules)

    def test_selected_profiles_resolve_to_one_pinned_main(self):
        self.assertEqual(14, len(self.selected))
        for name, destination in self.selected.items():
            nodes = main_warhead_nodes(self.rules.resolve_weapon(name))
            self.assertEqual(1, len(nodes), name)
            node = nodes[0]
            self.assertEqual(
                f"Warhead@{destination}FlatCompatibility", node.key, name)
            _keys, damage, scale = BASELINE[name]
            self.assertEqual(str(damage), node.get("Damage"), name)
            self.assertEqual(str(scale), node.get("PercentageScale"), name)
            self.assertEqual(TARGETS[name], node.get("ValidTargets"), name)

    def test_selected_roots_keep_exact_inheritance_closures(self):
        selected = selections(self.rules)
        for root, (destination, descendants, _evidence) in ROOTS.items():
            self.assertEqual(destination, selected[root])
            self.assertTrue(descendants <= set(selected))

    def test_pinned_or_contradictory_roles_remain_outside_the_cohort(self):
        excluded = {
            "AtreusMG", "EpigraphMG", "GoliathMG", "GoliathMk2MG",
            "HMG_Duelist_upgrade", "autogun_tank", "Future_MultiMissile",
            "RA2MortarBike", "TSAdatsMissile", "TSChemAdatsMissileAA",
            "TSRPGTowerRail", "VolkovMagneticWeapon", "tkmjuggap",
            "tkmtechnicalmgap",
        }
        self.assertTrue(excluded.isdisjoint(self.selected))
        for name in excluded:
            self.assertGreaterEqual(
                len(main_warheads(self.rules.resolve_weapon(name))), 2, name)

    def test_all_selected_definitions_are_directly_actor_armed(self):
        armed = set()
        for name in self.rules.actors:
            if name.startswith("^"):
                continue
            actor = self.rules.resolve(name)
            if actor is None:
                continue
            for node in actor.children:
                if node.key == "Armament" or node.key.startswith("Armament@"):
                    weapon = str(node.get("Weapon") or "").strip()
                    if weapon:
                        armed.add(weapon)
        self.assertTrue(set(self.selected) <= armed)

    def test_naxis_flak_preserves_counted_allied_damage(self):
        for name in ("NaxFlakAA", "PortableFlak", "PortableFlak_elite"):
            node = self.rules.resolve_weapon(name).child(
                "Warhead@NaxFlakAllyCounted")
            self.assertEqual("1500", node.get("Damage"), name)

    def test_folded_percentage_rounding_delta_is_pinned_and_minimal(self):
        health_values = set()
        for name in self.rules.actors:
            if name.startswith("^"):
                continue
            actor = self.rules.resolve(name)
            health = actor.child("Health") if actor is not None else None
            if health is not None and health.get("HP"):
                health_values.add(int(health.get("HP")))
        health_values.add(200000)

        for applications in (2, 3):
            differences = {
                hp: runtime_percentage_hp(hp, applications * 100, 10000)
                    - applications * runtime_percentage_hp(hp, 100, 10000)
                for hp in health_values
            }
            self.assertEqual(
                {160: 1, 250: 1},
                {hp: delta for hp, delta in differences.items() if delta},
                applications,
            )


if __name__ == "__main__":
    unittest.main()
