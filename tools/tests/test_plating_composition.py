"""Unit tests for the plating COMPOSITION table in tools/balance/gen_weapon_template.py.

Two laws are pinned here, both maintainer rulings, and both of a kind a well-meaning edit
breaks silently — the generator keeps emitting valid yaml either way:

  U1  EVERY emitted family's plating row is UNIQUE (2026-08-17: *"I want all weapon families
      to be a bit more unique so don't put 3 energy weapons exactly on the same versus
      value"*). Four of these rows were ties as recently as `7252f5be3`: Laser/Prism/Tesla,
      Chemical/Cryo/Flame/Toxic, Concussion/Demolition, Arrow/Bullet/CannonAP/Melee.

  U2  PHYSICS_RANK and COMPOSITION agree about FIELD COUPLING. This is what is left of
      `_rank_blend` after it was retired for over-reaching (it derived one table from the
      other; `Railgun` disproves that they are one axis). The drift it guards against shipped
      twice — `Inferno` and `Cryo` both sat at `thermo 1.00` while ranked as prism-chassis
      focused-energy weapons, so a REFLECTOR plating did nothing against either.

⚠ U1 is a property of the WHOLE SET, not of one entry: the columns are pinned to a common
mean, so changing any family's composition moves every other family's shipped row. That is
why this is a test over the emitted set rather than a per-entry assertion.
"""

from __future__ import annotations

import collections
import pathlib
import sys
import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))

import gen_weapon_template as gen  # noqa: E402


def emitted_families() -> list[str]:
    """Every family the generator actually emits a template for, with multiplicity.

    Mirrors `gen._plating_scales` deliberately: the column means are computed over this exact
    multiset, so a test that used a different one could pass while the shipped rows tie.
    """
    fams: list[str] = []
    for nm, (_b, _d, _a, levels) in gen.WEAPONS.items():
        if nm not in gen.HAND_TUNED:
            fams += [nm] * len(levels)
    for nm, (_p, _n, _s, levels) in gen.INHERIT_FAMILIES.items():
        fams += [nm] * len(levels)
    for nm, (_p, _s, levels) in gen.BLEND_FAMILIES.items():
        fams += [nm] * len(levels)
    fams += ["Storm"] * len(gen.STORM_LEVELS)
    return fams


def rows_by_family() -> dict[str, dict[str, int]]:
    return {f: dict(gen.plating_rows(f)) for f in sorted(set(emitted_families()))}


class UniqueRowsTest(unittest.TestCase):
    """U1 — no two emitted families may share a plating row."""

    def test_no_two_families_share_a_row(self):
        rows = rows_by_family()
        # ARMOR is FLAT 100 for every family by design ("receives 100% damage from
        # everything"), so it can never contribute to distinguishing them and is excluded.
        cols = [p for p in gen.PLATING_CYCLE if p != "ARMOR"]
        seen: dict[tuple[int, ...], list[str]] = collections.defaultdict(list)
        for fam, row in rows.items():
            seen[tuple(row[c] for c in cols)].append(fam)
        ties = {k: v for k, v in seen.items() if len(v) > 1}
        self.assertEqual(
            ties, {},
            "families sharing a plating row (see docs/design/"
            "PLATING_COMPOSITION_REFINEMENT.md — a tie can ONLY be broken by moving mass "
            f"across a group boundary, never by within-group refinement): {ties}")

    def test_the_generic_plating_stays_flat(self):
        """ARMOR is the hedge; a per-family ARMOR value would contradict its definition."""
        vals = {dict(gen.plating_rows(f))["ARMOR"] for f in set(emitted_families())}
        self.assertEqual(len(vals), 1, f"ARMOR must be one flat value, got {vals}")


class RankCompositionAgreementTest(unittest.TestCase):
    """U2 — the shield table and the plating table must not contradict each other."""

    def test_no_conflicts(self):
        self.assertEqual(gen.rank_composition_conflicts(), [])

    def test_the_two_families_that_shipped_the_drift_are_covered(self):
        for fam in ("Inferno", "Cryo"):
            self.assertGreaterEqual(gen.PHYSICS_RANK[fam], gen.ENERGY_COUPLING_RANK, fam)
            self.assertGreater(gen.composition(fam)["energy"], 0.0, fam)

    def test_railgun_is_why_the_tables_are_not_derived_from_each_other(self):
        """A high rank with a nearly pure KINETIC composition: the standing counter-example."""
        self.assertGreaterEqual(gen.PHYSICS_RANK["Railgun"], gen.ENERGY_COUPLING_RANK)
        self.assertGreater(gen.composition("Railgun")["kinetic"],
                           gen.composition("Railgun")["energy"])


class MaintainerRulingsTest(unittest.TestCase):
    """The two named rulings of 2026-08-17, in the form they were given."""

    def test_inferno_is_mostly_thermal(self):
        c = gen.composition("Inferno")
        self.assertGreater(c["thermo"], c["energy"], "*'mostly thermal'*")
        self.assertGreater(c["energy"], 0.0, "*'kind of both thermal and energy'*")

    def test_tesla_is_the_opposite_mostly_energy(self):
        c = gen.composition("Tesla")
        self.assertGreater(c["energy"], c["thermo"], "*'it's mostly energy'*")
        self.assertGreater(c["thermo"], 0.0, "*'and a bit of thermal'*")

    def test_inferno_is_reduced_by_both_overlays_and_more_by_hazmat(self):
        """*"shouldn't it be reduced by both hazmat and reflector ... more by hazmat"*"""
        row = dict(gen.plating_rows("Inferno"))
        self.assertLess(row["HAZMAT"], 100)
        self.assertLess(row["REFLECTOR"], 100)
        self.assertLess(row["HAZMAT"], row["REFLECTOR"])


class CompositionShapeTest(unittest.TestCase):
    def test_every_share_set_is_normalised_and_on_the_known_axes(self):
        for name, raw in list(gen.COMPOSITION.items()) + list(gen.COMPOSITION_OVERRIDE.items()):
            self.assertEqual([a for a in raw if a not in gen.PLATING_AXES], [], name)
            self.assertAlmostEqual(sum(raw.values()), 1.0, places=6, msg=name)

    def test_rank_blend_stayed_retired(self):
        """It cannot come back by copy-paste: it derived a share the maintainer then overruled."""
        src = (ROOT / "tools" / "balance" / "gen_weapon_template.py").read_text(encoding="utf-8")
        self.assertNotIn("_rank_blend(", src)


if __name__ == "__main__":
    unittest.main()
