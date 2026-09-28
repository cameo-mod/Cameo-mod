"""Exact regression coverage for the Armored Car's retired 10% modifier."""

from fractions import Fraction
import pathlib
import sys
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path[:0] = [str(ROOT / "tools/audit"), str(ROOT / "tools/balance")]
from miniyaml import Ruleset  # noqa: E402


WEAPONS = (
    "ArmoredCarMG",
    "ArmoredCarMG_AA",
    "ArmoredCarMGWaveforce",
    "ArmoredCarMGAAWaveforce",
)

# Every resolved runtime field except Damage and PercentageDenominator: targeting, cadence, projectile,
# versus tables, effects. Re-pinned 2026-09-28 on the post-W24 layout (the pre-bake pins went stale when
# the bullet and railgun warheads folded into one main); change them only with a reviewed resolve diff.
NON_DAMAGE_HASHES = {
    "ArmoredCarMG": "70574046aff059fd72ae33e11abb592123c502bedde15dea0c819b92e68625f5",
    "ArmoredCarMG_AA": "97401fc1df7de321de40f3155ed61bba866dd229aa2d425eb43297923fdc3522",
    "ArmoredCarMGWaveforce": "b005aa259a98a9da26c89676ed24ba09d88c88d99959b7fd38f315665a20d1cc",
    "ArmoredCarMGAAWaveforce": "5d7c0501be6ce9e808d1114f26939742fc880dc4f5d1406faab711374274c389",
}


def child(node, key):
    return next((item for item in node.children if item.key == key), None)


class JapanArmoredCarFirepowerBakeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = Ruleset(ROOT)

    def test_actor_keeps_only_the_conditional_waveforce_multiplier(self):
        actor = self.rules.resolve("japan_armoredcar")
        multipliers = [c for c in actor.children if c.key.startswith("FirepowerMultiplier")]
        self.assertFalse(any(c.get("RequiresCondition") is None for c in multipliers))
        waveforce = child(actor, "FirepowerMultiplier@japan_upgrade_waveforcebullets")
        self.assertEqual("125", waveforce.get("Modifier"))
        self.assertEqual("japan_upgrade_waveforcebullets", waveforce.get("RequiresCondition"))

    def test_total_flat_damage_retains_the_ten_percent_bake_after_warhead_folds(self):
        for weapon_name in WEAPONS:
            with self.subTest(weapon=weapon_name):
                weapon = self.rules.resolve_weapon(weapon_name)
                # W24 folds the original 16000 bullet + 3000 railgun into
                # one main. Preserve the total, not the retired node split.
                old_total = 16000 + (3000 + 1000 if "Waveforce" in weapon_name else 0)
                flat_total = sum(
                    int(warhead.get("Damage") or 0)
                    for warhead in weapon.children
                    if warhead.key.startswith("Warhead@")
                    and warhead.value in {"AreaDamage", "SpreadDamage", "TargetDamage"}
                )
                self.assertEqual(old_total // 10, flat_total)
                self.assertEqual("25", child(weapon, "Warhead@Concrete").get("Damage"))

                # Standalone percentage channels retain their independent
                # bake. A folded channel follows its current main Damage.
                for warhead in weapon.children:
                    if warhead.value == "AreaDamagePercentage" and warhead.get("Damage"):
                        self.assertEqual(
                            Fraction(1, 1000),
                            Fraction(
                                int(warhead.get("Damage")),
                                int(warhead.get("PercentageDenominator") or 100),
                            ),
                            warhead.key,
                        )

    def test_targeting_cadence_and_all_other_weapon_fields_are_unchanged(self):
        for weapon_name, expected in NON_DAMAGE_HASHES.items():
            with self.subTest(weapon=weapon_name):
                payload = non_damage_payload(self.rules.resolve_weapon(weapon_name))
                digest = hashlib.sha256(json.dumps(
                    payload, sort_keys=True, separators=(",", ":")
                ).encode()).hexdigest()
                self.assertEqual(expected, digest)


if __name__ == "__main__":
    unittest.main()
