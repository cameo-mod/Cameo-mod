"""Unit tests for the folded physical-state warhead audit."""

from __future__ import annotations

import unittest

import _bootstrap  # noqa: F401 -- sys.path side effect

from audit_physical_state_warheads import folded_percentage_problems  # noqa: E402
from miniyaml import Node  # noqa: E402


def _field(key, value, children=None):
    return Node(key, str(value), children or [])


def _folded_warhead(state="Temperature", scale="100", percentage_scale="10000",
                    *, multiple=False, warhead_type="AreaDamage"):
    children = [_field("PercentageScale", percentage_scale)]
    if multiple:
        children.append(_field("PhysicalStates", "", [_field(state, scale)]))
    else:
        children.extend([
            _field("PhysicalStateName", state),
            _field("PhysicalStateScale", scale),
        ])

    return Node("Warhead@Test", warhead_type, children)


class FoldedPercentageContractTest(unittest.TestCase):
    def test_single_state_form_is_valid(self):
        warhead = _folded_warhead()
        self.assertEqual(folded_percentage_problems(warhead, "Temperature", "100"), [])

    def test_multi_state_form_is_valid(self):
        warhead = _folded_warhead("Corrosion", multiple=True)
        self.assertEqual(folded_percentage_problems(warhead, "Corrosion", "100"), [])

    def test_percentage_damage_must_be_enabled(self):
        for bad in ("0", "-1", "not-a-number"):
            with self.subTest(percentage_scale=bad):
                problems = folded_percentage_problems(
                    _folded_warhead(percentage_scale=bad), "Temperature", "100")
                self.assertIn("main warhead does not enable folded percentage damage", problems)

    def test_expected_state_must_be_present(self):
        problems = folded_percentage_problems(
            _folded_warhead("Corrosion", multiple=True), "Temperature", "100")
        self.assertIn("main warhead does not apply Temperature", problems)

    def test_expected_state_scale_is_exact(self):
        problems = folded_percentage_problems(
            _folded_warhead(scale="50"), "Temperature", "100")
        self.assertIn("main warhead Temperature scale is not 100", problems)

    def test_same_state_cannot_use_both_binding_forms(self):
        warhead = _folded_warhead()
        warhead.children.append(
            _field("PhysicalStates", "", [_field("Temperature", "100")]))
        problems = folded_percentage_problems(warhead, "Temperature", "100")
        self.assertIn(
            "main warhead applies Temperature through both PhysicalStateName and PhysicalStates",
            problems)

    def test_legacy_split_type_is_not_a_folded_main(self):
        problems = folded_percentage_problems(
            _folded_warhead(warhead_type="AreaDamagePercentage"), "Temperature", "100")
        self.assertIn("main warhead is AreaDamagePercentage, expected AreaDamage", problems)


if __name__ == "__main__":
    unittest.main()
