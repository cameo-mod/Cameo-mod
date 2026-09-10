import pathlib
import sys
import tempfile
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / 'reference'))
from extract_versus import parse_dta_overlay


class DtaOverlayTest(unittest.TestCase):
    def test_overlay_keeps_base_profiles_and_unmodified_armor_fields(self):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            rules = root / 'Rules.ini'
            overlay = root / 'Enhance.ini'
            rules.write_text('[WH]\nModifier.none=100%\nModifier.heavy=50%\n[Other]\nModifier.none=0%\nModifier.light=100%\n')
            overlay.write_text('[WH]\nModifier.heavy=75%\n')
            rows = {r['warhead']: r for r in parse_dta_overlay(rules, overlay)}
        self.assertEqual(rows['WH']['versus'], {'none': 100, 'heavy': 75})
        self.assertEqual(rows['Other']['versus']['none'], 0)

    def test_malformed_named_field_is_not_silently_dropped(self):
        with tempfile.TemporaryDirectory() as directory:
            path = pathlib.Path(directory) / 'Rules.ini'
            path.write_text('[WH]\nBaseSection=Parent\nModifier.none=unknown\nModifier.heavy=50%\n')
            row = parse_dta_overlay(path)[0]
        self.assertNotIn('versus', row)
        self.assertEqual(row['inheritance_status'], 'not resolved')


if __name__ == '__main__':
    unittest.main()
