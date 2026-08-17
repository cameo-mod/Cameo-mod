#!/usr/bin/env python3
"""Tests for formula.condition_holds_by_default — which armament is the BASE weapon.

This decides whether a unit's weapon counts towards its price, so a wrong answer
silently misprices the roster in one direction or the other. The rule under test:
a unit AS BUILT has no promotions, no researched upgrades, no passengers and is
not deployed, so every named condition evaluates to FALSE.
"""
import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/balance"))

import formula  # noqa: E402

holds = formula.condition_holds_by_default


class TestUnconditional(unittest.TestCase):
    def test_none_is_base(self):
        self.assertTrue(holds(None))

    def test_empty_is_base(self):
        self.assertTrue(holds(""))
        self.assertTrue(holds("   "))


class TestVeterancy(unittest.TestCase):
    """The case that motivated this: 125 armaments gated `!rank-elite`."""

    def test_not_elite_is_the_base_weapon(self):
        self.assertTrue(holds("!rank-elite"))

    def test_elite_is_not_the_base_weapon(self):
        self.assertFalse(holds("rank-elite"))

    def test_hyphenated_names_parse(self):
        # `rank-elite` contains a hyphen, which must not be read as minus.
        self.assertTrue(holds("!rank-veteran"))
        self.assertFalse(holds("rank-veteran"))


class TestUpgrades(unittest.TestCase):
    def test_pre_upgrade_variant_is_base(self):
        self.assertTrue(holds("!forgotten_upgrade_chemicalweapons"))

    def test_upgraded_variant_is_not_base(self):
        self.assertFalse(holds("forgotten_upgrade_chemicalweapons"))

    def test_dotted_names_parse(self):
        self.assertTrue(holds("!up_resurrection.nax"))


class TestCompound(unittest.TestCase):
    def test_and_requires_every_term(self):
        # A transport weapon: needs a passenger, so it is never the base weapon.
        self.assertFalse(holds("ifv-miss && !rank-elite"))

    def test_and_of_negations_is_base(self):
        self.assertTrue(holds("!rank-elite && !schwarzermond_upgrade_crystallens"))

    def test_or_is_base_when_either_side_is(self):
        self.assertTrue(holds("rank-elite || !rank-elite"))

    def test_or_of_positives_is_not_base(self):
        self.assertFalse(holds("rank-elite || rank-veteran"))

    def test_parentheses(self):
        self.assertTrue(holds("!(rank-elite || deployed)"))
        self.assertFalse(holds("(rank-elite || deployed) && !disabled"))

    def test_real_expression_from_the_ledger(self):
        expr = "ifv-miss && !rank-elite && !ra2_allies_upgrade_thunderboltmissiles"
        self.assertFalse(holds(expr))


class TestComparisons(unittest.TestCase):
    """Stacking conditions compare against a count, which defaults to 0."""

    def test_ge_one_is_false_by_default(self):
        self.assertFalse(holds("shieldgen >= 1"))

    def test_equals_zero_is_true_by_default(self):
        self.assertTrue(holds("shieldgen == 0"))

    def test_not_equals_survives_the_bang_rewrite(self):
        # `!=` must not be mangled into `not =` by the `!` -> `not` rewrite.
        self.assertFalse(holds("shieldgen != 0"))
        self.assertTrue(holds("shieldgen != 1"))


class TestUnparseable(unittest.TestCase):
    def test_garbage_is_not_treated_as_base(self):
        """Fail CLOSED: a wrong price looks authoritative, a missing one does not."""
        self.assertFalse(holds("&& ||"))
        self.assertFalse(holds("("))


if __name__ == "__main__":
    unittest.main()
