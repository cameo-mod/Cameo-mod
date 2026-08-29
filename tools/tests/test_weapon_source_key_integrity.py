"""Regression checks for active weapon source keys that MiniYAML merges by name."""

from __future__ import annotations

import collections
import pathlib
import sys
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))

from miniyaml import Ruleset


SINGLE_VALUE_FIELDS = {"ValidTargets", "Range", "ReloadDelay", "Report"}

EXPECTED_VALUES = {
    "FLAK-23-AG": {"ReloadDelay": "5"},
    "Fremen_S": {"Report": "MGUN2.WAV"},
    "RA2GrenadePack": {"Report": "toss1.aud"},
    "SteelFortressWeapons": {
        "Report": "vrhiatta.wav, vrhiattb.wav, vrhiattc.wav, vrhiattd.wav",
    },
    "SteelMantaHunterCannons": {"ValidTargets": "Ground, Water"},
    "SteelVulcan": {"Report": "pulseturretfire.wav"},
    "TSAAPCCannon": {"ValidTargets": "Ground, Water, Air"},
    "TSChemMLRSMissile": {"Report": "hovrmis1.aud"},
    "TSChemVanMissile": {"Report": "hovrmis1.aud"},
    "TSDestroyerMissiles": {"Report": "hovrmis1.aud"},
    "TSEngineerPistol": {"ReloadDelay": "25"},
    "TSHoverMissile": {"Report": "hovrmis1.aud"},
    "TSJuggerFlakAA_boat": {"Range": "11234"},
    "TSMLRSMissile": {"Report": "hovrmis1.aud"},
    "TSMutApcCannon": {"ValidTargets": "Ground, Water, Air"},
    "TSVanMissile": {"Report": "hovrmis1.aud"},
    "Vulcan": {"ReloadDelay": "125"},
    "d2k_flame_tank": {"ValidTargets": "Ground, Water"},
    "wc2mageBlizzard": {"ValidTargets": "Ground, Water, Air"},
}

EXPECTED_PARENTS = {
    "12MissilesSpawnerScud": {
        "^Projectile_Flame_Medium", "^Effect_Flame_Medium",
        "^RA2Grenade", "^RA2HeavyMissile",
    },
    "HammerTankCannonThermobaric": {
        "^Projectile_Flame_Medium", "HammerTankCannon",
    },
    "KotinCannonThermobaric": {
        "^Projectile_Flame_Medium", "KotinCannon",
    },
    "SandmarineTuskFire": {"^Warhead_MissileAP_Light", "SandmarineTusk"},
    "TSDestroyerMissiles": {"^FlakWeapon", "^MediumMissile", "^ShrapnelWeapon"},
    "ViperMissilesFire": {"^Warhead_MissileAP_Light", "ViperMissiles"},
    "tkmkatyushalalauncherrocketsfire": {
        "^Effect_Flame_Light", "tkmkatyushalalauncherrockets",
    },
}


class WeaponSourceKeyIntegrityTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = Ruleset(ROOT)
        cls.concrete = {
            name: node for name, node in cls.rules.weapons.items()
            if not name.startswith("^") and cls.rules.resolve_weapon(name) is not None
        }

    def test_concrete_weapons_do_not_repeat_single_value_fields(self):
        duplicates = []
        for name, node in self.concrete.items():
            counts = collections.Counter(
                child.key for child in node.children
                if child.key in SINGLE_VALUE_FIELDS
            )
            duplicates.extend(
                (name, key, count) for key, count in counts.items() if count > 1
            )
        self.assertEqual([], duplicates)

    def test_concrete_weapons_do_not_shadow_inheritance_parents(self):
        duplicates = []
        for name, node in self.concrete.items():
            counts = collections.Counter(
                child.key for child in node.children
                if child.key == "Inherits" or child.key.startswith("Inherits@")
            )
            duplicates.extend(
                (name, key, count) for key, count in counts.items() if count > 1
            )
        self.assertEqual([], duplicates)

    def test_cleanup_keeps_the_previously_effective_scalar_values(self):
        for name, fields in EXPECTED_VALUES.items():
            weapon = self.rules.resolve_weapon(name)
            for field, expected in fields.items():
                self.assertEqual(expected, weapon.get(field), f"{name}/{field}")

    def test_corrected_inheritance_keys_keep_every_authored_parent(self):
        for name, expected in EXPECTED_PARENTS.items():
            local = self.rules.weapon(name)
            actual = {
                str(child.value) for child in local.children
                if child.key == "Inherits" or child.key.startswith("Inherits@")
            }
            self.assertTrue(expected <= actual, (name, expected - actual))


if __name__ == "__main__":
    unittest.main()
