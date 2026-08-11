"""Unit tests for tools/audit/audit_recent_changes.py.

Pins the R1 scoping rule (2026-08-11): a balance field only counts when it sits
somewhere a ledger row can exist. Both exclusions are structural, not taste —
an abstract ^Template has no ledger row, and a Range on a GrantExternalCondition
is a condition radius rather than a priced stat.
"""

from __future__ import annotations

import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

import audit_recent_changes as rc


def ctx(yaml_text: str, needle: str) -> bool:
    """priced_context() for the (single) line containing `needle`."""
    lines = yaml_text.split("\n")
    idx = next(i for i, line in enumerate(lines) if needle in line)
    return rc.priced_context(lines, idx)


CONCRETE_WEAPON = """\
SomeRifle:
\tReloadDelay: 25
\tRange: 5120
\tWarhead@Bullet_Light: AreaDamage
\t\tSpread: 400
\t\tDamage: 2000
"""

ABSTRACT_TEMPLATE = """\
^Warhead_Sonic_Light:
\tReloadDelay: 25
\tRange: 5120
\tWarhead@Sonic_Light: AreaDamage
\t\tSpread: 400
\t\tDamage: 2000
\tWarhead@Sonic_Light_Debuff: GrantExternalCondition
\t\tDuration: 50
\t\tRange: 800
"""

CONCRETE_WITH_CONDITION = """\
GDIPredatorBlueLaser:
\tReloadDelay: 11
\tWarhead@2Con: GrantExternalCondition
\t\tCondition: SonicDebuff
\t\tRange: 222
\tWarhead@Effect: CreateEffect
\t\tSpread: 128
"""


class PricedContextTest(unittest.TestCase):
    def test_damage_on_a_concrete_weapon_is_priced(self):
        self.assertTrue(ctx(CONCRETE_WEAPON, "Damage: 2000"))

    def test_weapon_level_range_on_a_concrete_weapon_is_priced(self):
        self.assertTrue(ctx(CONCRETE_WEAPON, "Range: 5120"))

    def test_nothing_inside_an_abstract_template_is_priced(self):
        """A ^Template has no ledger row, so no field in it can have one."""
        self.assertFalse(ctx(ABSTRACT_TEMPLATE, "Damage: 2000"))
        self.assertFalse(ctx(ABSTRACT_TEMPLATE, "Range: 800"))

    def test_condition_radius_on_a_concrete_weapon_is_not_priced(self):
        """`Range` inside GrantExternalCondition is a radius, not a weapon stat."""
        self.assertFalse(ctx(CONCRETE_WITH_CONDITION, "Range: 222"))

    def test_effect_spread_is_not_priced(self):
        self.assertFalse(ctx(CONCRETE_WITH_CONDITION, "Spread: 128"))

    def test_balance_field_regex_matches_a_diff_addition(self):
        self.assertTrue(rc.BALANCE_FIELD.match("+\t\tDamage: 2000"))
        self.assertIsNone(rc.BALANCE_FIELD.match("+\t\tCondition: SonicDebuff"))


class IndentTest(unittest.TestCase):
    def test_tabs_and_spaces_both_count(self):
        self.assertEqual(rc.indent_of("\t\tDamage: 1"), 2)
        self.assertEqual(rc.indent_of("    Damage: 1"), 4)
        self.assertEqual(rc.indent_of("Damage: 1"), 0)


class ProvenanceConfigTest(unittest.TestCase):
    def test_shared_identity_is_configured(self):
        self.assertIn("AedisToru", rc.SHARED_IDENTITY)

    def test_trailer_regex_extracts_the_agent(self):
        match = rc.TRAILER.search("body\n\nCo-Authored-By: Devin AI <devin@cognition.ai>\n")
        self.assertIsNotNone(match)
        self.assertIn("Devin AI", match.group(1))

    def test_strict_trailer_defaults_off(self):
        """Provenance on a shared identity is not mechanically decidable."""
        self.assertFalse(rc.STRICT_TRAILER)


if __name__ == "__main__":
    unittest.main()
