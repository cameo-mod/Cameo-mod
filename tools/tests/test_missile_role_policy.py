"""Domain-selected missile family contracts for the reviewed leaf cohort."""
import pathlib
import sys
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / 'audit'))
from miniyaml import Node, Ruleset
from dump_resolved import node_to_obj
from reviewed_weapon_history import missile_role_changes, missile_parent_role_changes, restore_missile_role


def rebuild(row):
    return Node(row[0], row[1], [rebuild(c) for c in row[2]])


class MissileRolePolicyTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rs = Ruleset(pathlib.Path(__file__).resolve().parents[2])

    def test_role_profiles_preserve_payload_and_all_other_behavior(self):
        records = missile_parent_role_changes() | missile_role_changes()
        self.assertEqual(77, len(records))
        for name, record in records.items():
            with self.subTest(weapon=name):
                before = rebuild(record['before'])
                now = self.rs.resolve_weapon(name)
                targets = set(now.get('ValidTargets').split(', '))
                family = 'MissileAA' if targets == {'Air'} else 'MissileAP' if 'Air' in targets else 'MissileHE'
                mains = [c for c in now.children if c.value == 'AreaDamage' and c.get('Damage')]
                old = [c for c in before.children if c.value == 'AreaDamage' and c.get('Damage')]
                self.assertEqual(len(mains), 1)
                self.assertEqual(len(old), 1)
                main = mains[0]
                self.assertTrue(main.key.startswith('Warhead@' + family + '_'))
                self.assertEqual(main.get('Damage'), old[0].get('Damage'))
                self.assertEqual(
                    {c.key: node_to_obj(c) for c in before.children if c is not old[0]},
                    {c.key: node_to_obj(c) for c in now.children if c is not main})
                template = self.rs.resolve_weapon('^Warhead_' + main.key.split('@')[1]).child(main.key)
                # Already-correct child families keep their existing reviewed
                # profile overrides while an ancestor changes role.
                expected_profile = old[0] if old[0].key.startswith('Warhead@' + family + '_') else template
                for key in ('Versus', 'PercentageVersus', 'Spread', 'Falloff'):
                    self.assertEqual(node_to_obj(main.child(key)), node_to_obj(expected_profile.child(key)))

    def test_history_rejects_unrecorded_live_cadence_change(self):
        live = self.rs.resolve_weapon('TSGDIRedEye').deep_copy()
        old = restore_missile_role(self, live)
        self.assertIsNotNone(old.child('Warhead@MissileAP_Heavy'))
        live.child('ReloadDelay').value = '999'
        with self.assertRaises(AssertionError):
            restore_missile_role(self, live)


if __name__ == '__main__':
    unittest.main()
