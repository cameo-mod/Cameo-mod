"""Band checks must cover derived classes and only the documented exemption."""
import contextlib
import io
import json
import pathlib
import sys
import tempfile
import unittest
from unittest.mock import patch

import _bootstrap
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[2] / 'tools/balance'))
import check_band as cb


class BandMembershipTest(unittest.TestCase):
    def run_report(self, unit):
        with tempfile.TemporaryDirectory() as temp:
            anchors = pathlib.Path(temp) / 'anchors.json'
            anchors.write_text(json.dumps({'mbt': {'spec': {'cost0': 100}}}))
            output = io.StringIO()
            with patch.object(cb, 'ANCHORS', anchors), \
                    patch.object(cb.tier_chain, 'load_derived_map', return_value={}), \
                    patch.object(cb, 'collect', return_value=[('test.json', 'tank', unit, {})]), \
                    patch.object(cb, 'unit_inputs', return_value=(1,) * 7), \
                    patch.object(cb, 'price_for', return_value=400), \
                    patch.object(sys, 'argv', ['check_band']), contextlib.redirect_stdout(output):
                result = cb.main()
            return result, output.getvalue()

    def test_template_class_is_checked_without_manual_tag(self):
        code, output = self.run_report({'design': {'subtype': 'MainBattleTank'}})
        self.assertEqual(code, 1)
        self.assertIn('1 members', output)
        self.assertIn('tank', output)

    def test_only_build_limit_one_is_exempt(self):
        for value, expected in ((None, False), (0, False), (1, True),
                                (2, False), ({'v': 1}, True), ({'v': 5}, False)):
            self.assertEqual(cb.band_exempt({'build_limit': value}), expected)
        for limit, code in ((1, 0), (2, 1)):
            result, _ = self.run_report({'design': {'subtype': 'MainBattleTank'}, 'build_limit': limit})
            self.assertEqual(result, code)

    def test_missing_class_cannot_return_a_false_green(self):
        code, output = self.run_report({'design': {'subtype': 'Vehicle'}})
        self.assertEqual(code, 2)
        self.assertIn('No applicable', output)

    def test_cargo_requires_passenger_valuation_instead_of_combat_formula(self):
        code, output = self.run_report({'design': {'subtype': 'MainBattleTank'},
                                       'speed': {'v': 50}, 'cargo_capacity': {'v': 8}})
        self.assertEqual(code, 1)
        self.assertIn('Unresolved cargo passenger-sum checks (1): tank', output)
        self.assertNotIn('## `mbt`', output)


if __name__ == '__main__':
    unittest.main()
