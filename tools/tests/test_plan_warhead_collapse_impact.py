"""THE HALF OF A COLLAPSE PLAN THAT NAMING CANNOT SEE.

⛔ `plan_warhead_collapse.py` answers *"which family?"*. It never answered *"and what does that do
to resolved damage?"* — so `HydraSpit` (four mains at an identical 18,000, four DIFFERENT `Versus`
ladders) read as a clean LEGACY-confidence collapse while the sum-preserving conversion multiplied
mean effective damage by 1.46x and moved individual armors 0.52x-2.78x. `--impact` is that half.

PRIOR ART: no test existed for `plan_warhead_collapse.py`; this covers the `--impact` addition
only. `test_audit_infantry_class_bands.py` is the range-band audit and shares no code.
`tools/balance/measure_retrofit_gap.py` measures TEMPLATE-to-family mean ratios; this measures a
WEAPON's whole main stack against the family it would collapse onto, which is a different question
with a different answer (a weapon inherits several templates at once).
"""

from __future__ import annotations

import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
TOOL = ROOT / "tools" / "balance" / "plan_warhead_collapse.py"
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))
import plan_warhead_collapse as pwc  # noqa: E402


from miniyaml import Node as YNode  # noqa: E402


def Node(key, damage, versus):
    """A real `miniyaml.Node`, so `percentage_damage.versus_table` runs its real code path.

    ⚠ A hand-rolled stand-in was the first version of this file and it did not have `.child`,
    which meant the test was exercising a shape the reader never sees. Build the node the
    resolver would actually hand it.
    """
    return YNode(key, "SpreadDamage", [
        YNode("Damage", str(damage), []),
        YNode("Versus", "", [YNode(a, str(v), []) for a, v in versus.items()]),
    ])


class ShapeClassificationTest(unittest.TestCase):
    """⭐ THE DISTINCTION THE BOARD'S "BROADCAST" FINGERPRINT DOES NOT MAKE.

    `BALANCE_PROGRAM_PLAN.md` §1b justifies sum-preservation from "576 of 934 have every main at
    the identical damage". Equal damage is not equal behaviour: only mains that also share a
    PROFILE sum neutrally. Measured, exactly **one** directly-armed multi-main weapon in the mod
    is broadcast on both; 43 are the HydraSpit shape.
    """

    def setUp(self):
        self._saved = pwc._family_profile

    def tearDown(self):
        pwc._family_profile = self._saved

    def _impact(self, nodes, target):
        pwc._family_profile = lambda rs, fam, lvl: target
        return pwc.collapse_impact(None, nodes, "AnyFamily", "Medium")

    def test_same_damage_and_same_profile_is_BROADCAST(self):
        prof = {"Light": 100, "Heavy": 50}
        nodes = [Node("Warhead@A", 1000, prof), Node("Warhead@B", 1000, dict(prof))]
        shape, mean, lo, hi = self._impact(nodes, {"Light": 100, "Heavy": 50})
        self.assertEqual(shape, "BROADCAST")
        self.assertAlmostEqual(mean, 1.0, places=6)
        self.assertAlmostEqual(lo, 1.0, places=6)
        self.assertAlmostEqual(hi, 1.0, places=6)

    def test_same_damage_but_different_profiles_is_PILEUP(self):
        """The HydraSpit shape. Identical damage, so the broadcast fingerprint fires — and the
        collapse is still not neutral."""
        nodes = [Node("Warhead@A", 1000, {"Light": 200, "Heavy": 50}),
                 Node("Warhead@B", 1000, {"Light": 50, "Heavy": 200})]
        shape, _mean, lo, hi = self._impact(nodes, {"Light": 200, "Heavy": 50})
        self.assertEqual(shape, "PILEUP")
        self.assertNotAlmostEqual(lo, hi, places=3)

    def test_different_damage_is_MIXED(self):
        prof = {"Light": 100, "Heavy": 100}
        nodes = [Node("Warhead@A", 1000, prof), Node("Warhead@B", 3000, dict(prof))]
        self.assertEqual(self._impact(nodes, dict(prof))[0], "MIXED")

    def test_the_mean_can_be_preserved_while_every_matchup_moves(self):
        """⛔ THE FINDING THAT MATTERS. Median mean-ratio across the roster is 1.00x while the
        median per-armor SPREAD is 2.78x: sum-preservation is mean-neutral and
        matchup-destroying. A checker that only watched the mean would pass all of it."""
        nodes = [Node("Warhead@A", 1000, {"Light": 200, "Heavy": 50}),
                 Node("Warhead@B", 1000, {"Light": 50, "Heavy": 200})]
        _shape, mean, lo, hi = self._impact(nodes, {"Light": 200, "Heavy": 50})
        self.assertAlmostEqual(mean, 1.0, places=6)
        self.assertGreater(hi / lo, 2.0)


class MeasurementContractTest(unittest.TestCase):
    def test_hazmat_and_shield_are_excluded(self):
        """HAZMAT is a flat-50 immunity flag in every family and Shield is the W21 health layer.
        `measure_retrofit_gap.py` already documents that either one in the mean makes the
        comparison lie; this must use the same exclusion or the two tools disagree."""
        self.assertEqual(pwc.IGNORE_ARMORS, {"HAZMAT", "Shield"})

    def test_a_missing_family_rung_is_reported_not_guessed(self):
        """A family with no rung at the inferred level is a fact about the family. Substituting
        another level would invent an impact number nobody can check."""
        saved = pwc._family_profile
        try:
            pwc._family_profile = lambda rs, fam, lvl: None
            nodes = [Node("Warhead@A", 1000, {"Light": 100}),
                     Node("Warhead@B", 1000, {"Light": 100})]
            self.assertIsNone(pwc.collapse_impact(None, nodes, "Nope", "Medium"))
        finally:
            pwc._family_profile = saved

    def test_level_inference_says_when_it_guessed(self):
        """`Medium?` in the output means the weapon's own templates disagreed. Hiding that would
        present an assumption as a measurement."""
        self.assertEqual(pwc._level_from(["^LightChemicalWeapon", "^SmallArms"], []),
                         ("Light", True))
        self.assertEqual(pwc._level_from([], []), ("Medium", False))
        level, agreed = pwc._level_from(["^LightMissile", "^HeavyBomb"], [])
        self.assertFalse(agreed, "disagreeing templates must not report as agreed")
        self.assertIn(level, ("Light", "Heavy"))

    def test_yaml_is_read_through_the_project_readers(self):
        """CLAUDE.md rule 8e — `Versus` comes from `percentage_damage.versus_table`, never from a
        hand parser."""
        src = TOOL.read_text(encoding="utf-8")
        self.assertIn("import percentage_damage as pd", src)
        self.assertIn("pd.versus_table(n)", src)


if __name__ == "__main__":
    unittest.main()
