"""Regression contract for the first closure-isolation redesign batch."""

from __future__ import annotations

import hashlib
import json
import pathlib
import sys
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
REPORT = ROOT / "docs/audit/latest/closure_isolation_comparison.json"
sys.path.insert(0, str(ROOT / "tools" / "audit"))

from audit_three_way_split import main_warhead_nodes, main_warheads
from miniyaml import Ruleset
from reviewed_weapon_history import historical_copy
from owned_weapon_history import historical_weapon_names


CONSOLIDATED = {
    "AsianHowitzerSplash": ("Concussion_Medium", 40000),
    "TS155mm": ("Concussion_Medium", 60000),
    "TSAux155mm": ("Concussion_Medium", 60000),
    "TSInfantryMortar": ("Concussion_Medium", 32000),
}

# ⛔ RE-PINNED 2026-09-13, ON THE MAINTAINER'S EXPLICIT APPROVAL AND NOT BEFORE.
#
# Three of the five moved when R4 retired the 27 `^Warhead_*_Flat` shims. Re-pinning a
# byte-stability guard is exactly what that guard exists to prevent, so the change was held for a
# ruling and the FULL resolved diff was measured against a clean `origin/master` worktree first.
# It is 28 lines across the three weapons, and nothing else:
#
#   ra1_soviets_grenadier_grenaderaexplode
#       Warhead@Flame_LightFlatCompatibility -> Warhead@Flame_Light_Flat   RENAME ONLY, no values
#   ra1_soviets_grenadier_grenadethermobaric  and  ...thermobaricexplode  (they share one warhead)
#       Warhead@Thermobaric_LightFlatCompatibility -> Warhead@Thermobaric_Light
#       COMPOSITE  92 -> 93
#       Shield    179 -> 180
#
# ⭐ AND THE TWO VALUES THAT MOVED CANNOT BREAK §12.0h MEAN-100 BY CONSTRUCTION. The maintainer
# asked the right question — *"make sure the sum of all versus values is the same across all
# warheads, right?"* — and the answer is yes, over the SIXTEEN canonical armor rows, which sum to
# 1602 (mean 100.125) on this warhead. `audit_versus_profile.NON_ARMOR` excludes exactly
# `ARMOR, BLAST, COMPOSITE, HAZMAT, REFLECTOR, Shield` from that sum, so COMPOSITE and Shield are
# precisely the two rows sitting OUTSIDE it. `audit_versus_profile` reports 0 unexpected mean
# violations, and §12.0c gives Shield its own compressed ladder in any case.
#
# ⚠ THE TEST STOPS AT THE FIRST MISMATCH, which hid two of the three from the first report — it
# named only `...thermobaric`. When re-pinning, compute ALL of them and diff the resolved weapons;
# never trust the single name a failure happens to print.
PRESERVED_HASHES = {
    "TS155mm_bluenuke": "6300a8d84077a1ef44fd229487fcc31668c4ee9928d176f34d490b002097ba1d",
    "GrenadeRA": "d49fb3a4778376f009e6ae243614b19c1547942d00a6a3f2161f7061be7d78d6",
    "ra1_soviets_grenadier_grenaderaexplode": "c6fc286078284e776328cf7b9b9e9201522d6419091a4b067e1fa34a22d133fb",
    "ra1_soviets_grenadier_grenadethermobaric": "800b8d422be72e71930daeb3d8d6c0fbba5e2a2374317a9fc0c632eeca10e765",
    "ra1_soviets_grenadier_grenadethermobaricexplode": "44956b5f61e84e1ad0a96afc05a03c06677d02c183d83feaa4b8264b3ea7d3c1",
}

EXPECTED_PERCENTAGE_DELTAS = {
    "TS155mm": [[250, 74, 75]],
    "TSAux155mm": [[250, 74, 75]],
    "TSInfantryMortar": [[20, 2, 3], [160, 24, 25]],
}


def node_payload(node):
    return [node.key, node.value, [node_payload(child) for child in node.children]]


def resolved_hash(node) -> str:
    raw = json.dumps(node_payload(node), separators=(",", ":")).encode()
    return hashlib.sha256(raw).hexdigest()


def children_hash(node) -> str:
    raw = json.dumps(
        [node_payload(child) for child in node.children],
        separators=(",", ":"),
    ).encode()
    return hashlib.sha256(raw).hexdigest()


def descendants(rules, root):
    children = {}
    for name, node in rules.weapons.items():
        for _key, parent in rules.inherits_of(node):
            if parent in rules.weapons:
                children.setdefault(parent, set()).add(name)
    seen = set()
    pending = list(children.get(root, set()))
    while pending:
        name = pending.pop()
        if name in seen:
            continue
        seen.add(name)
        pending.extend(children.get(name, set()))
    return {name for name in seen if not name.startswith("^")}


class ClosureIsolationConsolidationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = Ruleset(ROOT)
        cls.report = json.loads(REPORT.read_text(encoding="utf-8"))

    def test_selected_roots_use_exact_concussion_profiles(self):
        for name, (profile, damage) in CONSOLIDATED.items():
            nodes = main_warhead_nodes(self.rules.resolve_weapon(name))
            self.assertEqual([profile], main_warheads(self.rules.resolve_weapon(name)), name)
            self.assertEqual(damage, int(nodes[0].get("Damage")), name)
            self.assertEqual("10000", nodes[0].get("PercentageScale"), name)

    def test_excluded_descendants_are_byte_stable_after_isolation(self):
        for name, expected in PRESERVED_HASHES.items():
            current = "ra1_soviets_grenadier_grenade" if name == "GrenadeRA" else name
            node = self.rules.resolve_weapon(current).deep_copy()
            node.key = next(iter(historical_weapon_names([name])))
            self.assertEqual(expected, resolved_hash(historical_copy(self, node)), name)
        self.assertEqual({"TSAux155mm"}, descendants(self.rules, "TS155mm"))
        self.assertEqual(set(), descendants(self.rules, "TSInfantryMortar"))
        self.assertEqual(set(), descendants(self.rules, "ra1_soviets_grenadier_grenade"))

    def test_grenade_historical_coupling_guard_remains_strict(self):
        current = self.rules.resolve_weapon("ra1_soviets_grenadier_grenade")
        node = current.deep_copy()
        node.key = "GrenadeRA"
        self.assertEqual("100", node.child("Warhead@Demolition_Light").child("Versus").get("COMPOSITE"))
        past = historical_copy(self, node)
        self.assertEqual("101", past.child("Warhead@Demolition_Light").child("Versus").get("COMPOSITE"))
        node.child("Warhead@Demolition_Light").child("Versus").child("COMPOSITE").value = "103"
        with self.assertRaises(AssertionError):
            historical_copy(self, node)
        self.assertEqual("ra1_soviets_grenadier_grenade", current.key)
        self.assertEqual("100", current.child("Warhead@Demolition_Light").child("Versus").get("COMPOSITE"))

    def test_kirov_uses_the_pinned_canonicalized_splash_payload(self):
        alias = self.rules.resolve_weapon("RA2KirovHowitzerSplash")
        self.assertEqual(
            "2ed75978918f26ca8b46ee69f73e93ed2d1133bc8f8a6068cca967315f46877a",
            children_hash(historical_copy(self, alias)),
        )
        kirov = self.rules.resolve_weapon("RA2KirovBomb_fire")
        trigger = next(child for child in kirov.children if child.key == "Warhead@2Fire")
        self.assertEqual("RA2KirovHowitzerSplash", trigger.get("TriggerWeapon"))

    def test_whole_tree_comparison_is_exactly_the_authorized_scope(self):
        self.assertEqual(["RA2KirovHowitzerSplash"], self.report["added"])
        self.assertEqual([], self.report["removed"])
        self.assertEqual(
            {*CONSOLIDATED, "RA2KirovBomb_fire"},
            set(self.report["changed"]),
        )

        for name in CONSOLIDATED:
            kinds = {change[0] for change in self.report["changed"][name]}
            expected = {"armor_profile", "blast_shape"}
            if name in EXPECTED_PERCENTAGE_DELTAS:
                expected.add("percentage_damage")
                actual = next(
                    change[1] for change in self.report["changed"][name]
                    if change[0] == "percentage_damage"
                )
                self.assertEqual(EXPECTED_PERCENTAGE_DELTAS[name], actual, name)
            self.assertEqual(expected, kinds, name)

        self.assertEqual(
            ["non_damage_warheads"],
            [change[0] for change in self.report["changed"]["RA2KirovBomb_fire"]],
        )


if __name__ == "__main__":
    unittest.main()
