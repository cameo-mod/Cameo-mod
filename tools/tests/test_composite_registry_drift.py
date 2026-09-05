"""A drift queue must not turn unchanged names or fresh JSON into approval."""
import pathlib
import sys
import unittest
from unittest.mock import patch
import contextlib
import io

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / 'audit'))
from report_composite_registry_drift import compare_entry
import report_composite_registry_drift as report


def entry():
    return {'mains': ['A', 'B'], 'main_digest': 'a', 'weapon_digest': 'b',
            'referrers': [], 'referrer_digest': 'c', 'expected_reachability': 'direct'}


class DriftQueueTests(unittest.TestCase):
    def test_writing_a_fresh_blocked_report_does_not_return_success(self):
        data = {'registry_status': 'blocked'}
        with patch.object(report, 'build', return_value=data), \
             patch.object(sys, 'argv', ['report', '--write']), \
             patch.object(pathlib.Path, 'write_text') as write, \
             contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(report.main(), 1)
            write.assert_called_once()

    def test_fresh_blocked_report_remains_failed_without_write(self):
        with patch.object(report, 'build', return_value={'registry_status': 'blocked'}), \
             patch.object(sys, 'argv', ['report']), \
             patch.object(pathlib.Path, 'exists', return_value=True), \
             patch.object(pathlib.Path, 'read_text', return_value='{\n  "registry_status": "blocked"\n}\n'), \
             patch.object(pathlib.Path, 'write_text') as write, \
             contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(report.main(), 1)
            write.assert_not_called()

    def test_unchanged_names_do_not_hide_changed_behavior(self):
        old = entry()
        live = dict(old, main_digest='new', weapon_digest='new')
        row = compare_entry('w', old, {'mains': ['A', 'B']}, live, 'direct')
        self.assertIn('main-behavior-drift-with-unchanged-names', row['categories'])
        self.assertIn('resolved-weapon-behavior-drift', row['categories'])

    def test_manifest_only_name_still_gets_live_comparison(self):
        row = compare_entry('HydraSpit', entry(), None, dict(entry(), mains=['New']), None)
        self.assertIn('manifest-curated-name-disagreement', row['categories'])
        self.assertIn('missing-or-no-longer-stacked', row['categories'])
        self.assertEqual(row['live_mains'], ['New'])

    def test_curated_only_and_missing_weapon_are_not_dropped(self):
        row = compare_entry('missing', None, {'mains': ['A', 'B']}, None, None)
        self.assertIn('manifest-curated-name-disagreement', row['categories'])
        self.assertIn('curated-main-names-differ-from-live', row['categories'])

    def test_referrer_and_reachability_changes_are_separate_from_main_names(self):
        row = compare_entry('w', entry(), {'mains': ['A', 'B']}, entry(), 'unreached')
        self.assertIn('reference-or-reachability-drift', row['categories'])
        self.assertNotIn('manifest-main-names-differ-from-live', row['categories'])

    def test_metadata_and_topology_can_both_disagree(self):
        row = compare_entry('w', entry(), {'mains': ['C', 'D']}, dict(entry(), mains=['X', 'Y']), 'direct')
        self.assertIn('decision-metadata-disagreement', row['categories'])
        self.assertIn('curated-main-names-differ-from-live', row['categories'])
        self.assertIn('manifest-main-names-differ-from-live', row['categories'])

    def test_unchanged_entry_has_no_invented_findings(self):
        row = compare_entry('w', entry(), {'mains': ['A', 'B']}, entry(), 'direct')
        self.assertEqual(row['categories'], [])


if __name__ == '__main__':
    unittest.main()
