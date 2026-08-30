import hashlib
import json
import pathlib
import sys
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))

from audit_three_way_split import main_warhead_nodes, main_warheads
from miniyaml import Ruleset
from survey_weapon_structure import inventory


RECOMMENDED_DESIGN_BATCH = {
    "ASDFKamikazeExplosion",
    "TSBusMortar",
    "ConscriptMolotov",
    "tkm_trooper_gp25",
    "AlliedTankDestroyerCannon",
    "NaxiAntiTankCannon",
    "NaxiAntiTankCannon_elite",
    "NaxiHetzerDestroyer",
    "NaxiHetzerDestroyer_elite",
    "AsianHowitzerCannon",
    "AsianHowitzerCannon_elite",
}

PRESERVE_HYBRID = {
    "TSBoatcannon",
    "SheridanCannon",
    "tkmturretcannon",
    "HammerTankCannon",
    "KotinCannon",
    "TigerCannon",
    "Type97Cannon",
}

ISOLATE_FIRST = {
    "AsianHowitzerSplash",
    "RA2Terrorist",
    "SCScourgeDroneExplosion",
    "ScourgeDroneExplosion",
    "SCScourgeExplosion",
    "ScourgeExplosion",
    "TS155mm",
    "TSAux155mm",
    "TSBomb",
    "TSInfantryMortar",
    "GrenadeRA",
}

DEFERRED_COHORT = RECOMMENDED_DESIGN_BATCH | PRESERVE_HYBRID | ISOLATE_FIRST

BLAST_PROFILE = {
    "ASDFKamikazeExplosion",
    "AsianHowitzerSplash",
    "RA2Terrorist",
    "SCScourgeDroneExplosion",
    "SCScourgeExplosion",
    "ScourgeDroneExplosion",
    "ScourgeExplosion",
    "TS155mm",
    "TSAux155mm",
    "TSBoatcannon",
    "TSBomb",
    "TSBusMortar",
    "TSInfantryMortar",
}

STATE_PROFILE = {"ConscriptMolotov", "GrenadeRA", "tkm_trooper_gp25"}

AP_HE_PROFILE = {
    "AlliedTankDestroyerCannon",
    "NaxiAntiTankCannon",
    "NaxiAntiTankCannon_elite",
    "NaxiHetzerDestroyer",
    "NaxiHetzerDestroyer_elite",
    "SheridanCannon",
    "tkmturretcannon",
}

HE_TIER_PROFILE = {
    "AsianHowitzerCannon",
    "AsianHowitzerCannon_elite",
    "HammerTankCannon",
    "KotinCannon",
    "TigerCannon",
    "Type97Cannon",
}

INDIRECT_ONLY = {
    "ASDFKamikazeExplosion",
    "AsianHowitzerSplash",
    "RA2Terrorist",
    "SCScourgeDroneExplosion",
    "SCScourgeExplosion",
}

EXPECTED_MAIN_ORDER = {}
EXPECTED_MAIN_ORDER.update({
    name: ("Demolition_Heavy", "Concussion_Medium")
    for name in {
        "ASDFKamikazeExplosion",
        "RA2Terrorist",
        "SCScourgeDroneExplosion",
        "SCScourgeExplosion",
        "ScourgeDroneExplosion",
        "ScourgeExplosion",
        "TSInfantryMortar",
    }
})
EXPECTED_MAIN_ORDER.update({
    name: ("Concussion_Medium", "Demolition_Heavy")
    for name in BLAST_PROFILE - set(EXPECTED_MAIN_ORDER)
})
EXPECTED_MAIN_ORDER.update({
    name: ("Demolition_Light", "Flame_Light")
    for name in STATE_PROFILE
})
EXPECTED_MAIN_ORDER.update({
    name: ("CannonHE_Medium", "CannonAP_Light")
    for name in AP_HE_PROFILE - {"SheridanCannon", "tkmturretcannon"}
})
EXPECTED_MAIN_ORDER.update({
    name: ("CannonAP_Light", "CannonHE_Medium")
    for name in {"SheridanCannon", "tkmturretcannon"}
})
EXPECTED_MAIN_ORDER.update({
    name: ("CannonHE_Medium", "CannonHE_Heavy")
    for name in {"AsianHowitzerCannon", "AsianHowitzerCannon_elite"}
})
EXPECTED_MAIN_ORDER.update({
    name: ("CannonHE_Heavy", "CannonHE_Medium")
    for name in HE_TIER_PROFILE - {"AsianHowitzerCannon", "AsianHowitzerCannon_elite"}
})

EXPECTED_DESCENDANT_CLOSURE = {
    "AsianHowitzerCannon": {"AsianHowitzerCannon_elite"},
    "ConscriptMolotov": {"ConscriptMolotovExplode"},
    "GrenadeRA": {
        "GrenadeRAExplode",
        "GrenadeThermobaric",
        "GrenadeThermobaricExplode",
    },
    "HammerTankCannon": {"HammerTankCannonThermobaric"},
    "KotinCannon": {"KotinCannonThermobaric"},
    "NaxiAntiTankCannon": {
        "NaxiAntiTankCannonCorrosion",
        "NaxiAntiTankCannon_elite",
    },
    "NaxiHetzerDestroyer": {
        "NaxiHetzerDestroyerCorrosion",
        "NaxiHetzerDestroyer_elite",
    },
    "RA2Terrorist": {
        "GLBarrelExplode",
        "GLBombTruckToxExplosive",
        "GLBombTruckToxExplosive2",
        "GLDemolitionExplode",
        "GLTerroristExplosive",
        "GLTerroristExplosive2",
    },
    "SCScourgeDroneExplosion": {"ScourgeDroneExplosion"},
    "SCScourgeExplosion": {"ScourgeExplosion"},
    "TS155mm": {"TS155mm_bluenuke", "TSAux155mm"},
    "TSInfantryMortar": {"TSInfantryMortarChem"},
}

EXPECTED_MAIN_PROFILE_SHA256 = (
    "45780c46881cd38380b7ba1b7beb07e9777e52557e30144754023fb2e8c3146c"
)


def node_payload(node):
    return [node.key, node.value, [node_payload(child) for child in node.children]]


def main_profile_digest(rules):
    payload = {
        weapon: [node_payload(node) for node in main_warhead_nodes(
            rules.resolve_weapon(weapon))]
        for weapon in sorted(DEFERRED_COHORT)
    }
    raw = json.dumps(payload, sort_keys=True, separators=(",", ":")).encode()
    return hashlib.sha256(raw).hexdigest()


def descendant_closures(rules):
    children = {}
    for name, node in rules.weapons.items():
        for _key, parent in rules.inherits_of(node):
            if parent in rules.weapons:
                children.setdefault(parent, set()).add(name)

    closures = {}
    for root in DEFERRED_COHORT:
        seen = set()
        pending = list(children.get(root, set()))
        while pending:
            child = pending.pop()
            if child in seen:
                continue
            seen.add(child)
            pending.extend(children.get(child, set()))
        if seen:
            closures[root] = seen
    return closures


class DeferredWeaponRedesignBoundaryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = Ruleset(ROOT)
        cls.inventory = inventory(cls.rules)

    def test_decision_buckets_partition_the_exact_cohort(self):
        self.assertFalse(RECOMMENDED_DESIGN_BATCH & PRESERVE_HYBRID)
        self.assertFalse(RECOMMENDED_DESIGN_BATCH & ISOLATE_FIRST)
        self.assertFalse(PRESERVE_HYBRID & ISOLATE_FIRST)
        self.assertEqual(29, len(DEFERRED_COHORT))

    def test_current_composite_profiles_remain_unmodified(self):
        self.assertEqual(DEFERRED_COHORT, set(EXPECTED_MAIN_ORDER))
        for weapon, profiles in EXPECTED_MAIN_ORDER.items():
            self.assertEqual(
                profiles,
                tuple(main_warheads(self.rules.resolve_weapon(weapon))),
                weapon,
            )
            mains = main_warhead_nodes(self.rules.resolve_weapon(weapon))
            self.assertEqual(2, len(mains), weapon)
            self.assertTrue(all(node.value == "AreaDamage" for node in mains), weapon)

        self.assertEqual(EXPECTED_MAIN_PROFILE_SHA256,
                         main_profile_digest(self.rules))

    def test_inheritance_closure_remains_explicit(self):
        self.assertEqual(EXPECTED_DESCENDANT_CLOSURE,
                         descendant_closures(self.rules))

    def test_every_candidate_is_reachable_with_the_reviewed_split(self):
        direct = set(self.inventory["sets"]["direct_actor_armament"])
        indirect = set(self.inventory["sets"]["indirect_weapon_graph"])
        unreached = set(self.inventory["sets"]["unreached"])

        self.assertEqual(INDIRECT_ONLY, DEFERRED_COHORT & indirect)
        self.assertEqual(DEFERRED_COHORT - INDIRECT_ONLY, DEFERRED_COHORT & direct)
        self.assertTrue(DEFERRED_COHORT.isdisjoint(unreached))


if __name__ == "__main__":
    unittest.main()
