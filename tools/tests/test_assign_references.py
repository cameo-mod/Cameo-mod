"""The matching law's mechanics — the clauses that fail silently if they regress.

PRIOR ART: `test_explain_unit.py` covers the routing variants and the vote floors on ONE unit;
`test_lineage_dedup.py` covers source de-duplication. Neither touches the assignment.
"""

from __future__ import annotations

import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))
import assign_references as ar  # noqa: E402


class TheCascadeIsACascadeTest(unittest.TestCase):
    """⛔ THE BUG THIS CLASS EXISTS FOR. A lexicographic tuple whose first key is a near-continuous
    float degenerates into "rank by that key alone" — exact ties never occur, so tier, role and
    cost are computed and then thrown away. Measured before the fix: 38% of assignments had a role
    score under 0.5 while the role step ran on every one of them."""

    def test_the_name_score_is_bucketed_not_continuous(self):
        seen = set()
        for peer_name in ("mammoth", "mammothtank", "mammoth tank mk ii", "grizzly", "zzzz"):
            cam = {"id": "td_gdi_mammothtank", "type": "vehicle"}
            s = ar.score(cam, {}, {"type": "vehicle", "name": peer_name}, None, None, False)
            if s:
                seen.add(s[0])
        self.assertTrue(seen <= {0, 1, 2, 3, 4}, f"name key is not bucketed: {seen}")

    def test_role_breaks_a_tie_inside_a_name_bucket(self):
        cam = {"id": "x_y_scout", "type": "infantry"}
        peer = {"type": "infantry", "name": "zzzzz"}
        good = ar.score(cam, {}, peer, None, None, False, [0.9, 0.9], [0.9, 0.9])
        poor = ar.score(cam, {}, peer, None, None, False, [0.9, 0.9], [0.1, 0.1])
        self.assertEqual(good[0], poor[0], "fixture broke: the two must share a name bucket")
        self.assertGreater(good, poor, "role does not break the tie — the cascade is dead below name")


class TheHardRefusalsTest(unittest.TestCase):
    def test_cross_type_is_refused(self):
        """The TYPE half of the ten relative values is meaningless if the types differ."""
        self.assertIsNone(ar.score({"id": "a_b_c", "type": "infantry"}, {},
                                   {"type": "vehicle", "name": "c"}, None, None, False))

    def test_a_zero_damage_row_never_matches_an_ARMED_unit(self):
        """Clause 5. MO lists an 'Apocalypse' at 620 HP with zero damage — a different device."""
        armed = {"armaments": [{"pricing": True}]}
        self.assertIsNone(ar.score({"id": "a_b_apocalypsetank", "type": "vehicle"}, armed,
                                   {"type": "vehicle", "name": "Apocalypse", "w_damage": 0},
                                   None, None, False))
        self.assertIsNotNone(ar.score({"id": "a_b_apocalypsetank", "type": "vehicle"}, armed,
                                      {"type": "vehicle", "name": "Apocalypse", "w_damage": 130},
                                      None, None, False))


class TheExemptionsTest(unittest.TestCase):
    def test_role_identical_units_are_exempt(self):
        for actor in ("ra2_allies_engineer", "yuri_mobileconstructionvehicle",
                      "asianalliance_droneminer"):
            self.assertIsNotNone(ar.exempt(actor, {}), actor)

    def test_an_ARMED_carrier_is_NOT_exempt(self):
        """⚠ The maintainer's carve-out: the test is the gun, not the name."""
        armed = {"armaments": [{"pricing": True}]}
        self.assertIsNone(ar.exempt("cabal_scarabapctransport", armed))
        self.assertIsNotNone(ar.exempt("x_y_transport", {}))

    def test_shape_similarity_needs_two_axes_to_mean_anything(self):
        self.assertIsNone(ar.shape_similarity([0.5, None], [0.5, None]))
        self.assertAlmostEqual(ar.shape_similarity([0.5, 0.5], [0.5, 0.5]), 1.0)
        self.assertAlmostEqual(ar.shape_similarity([1.0, 1.0], [0.0, 0.0]), 0.0)


if __name__ == "__main__":
    unittest.main()
