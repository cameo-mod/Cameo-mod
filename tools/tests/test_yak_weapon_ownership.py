"""Actor-owned Yak guns retain full payloads, including conditional upgrade arms."""
import hashlib
import json
import pathlib
import re
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/audit'))
sys.path.insert(0, str(ROOT / 'tools/balance'))
from miniyaml import Ruleset
from dump_resolved import node_to_obj
import consolidate_named_family_profiles as cohort
import extract_stats

FIXTURE = ROOT / 'tools/tests/fixtures/yak_owned_weapon_baseline_20260910.json'


def digest(payload):
    return hashlib.sha256(json.dumps(payload, sort_keys=True,
                                    separators=(',', ':')).encode()).hexdigest()


class YakOwnershipTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.before = json.loads(FIXTURE.read_text(encoding='utf-8'))
        cls.rules = Ruleset(ROOT)

    def test_ten_owned_definitions_replace_four_shared_ids(self):
        names = [new for route in self.before['routes'].values() for new in route.values()]
        self.assertEqual(len(names), 10)
        self.assertEqual(len({n.casefold() for n in names}), 10)
        for old in self.before['weapon_hashes']:
            self.assertNotIn(old, self.rules.weapons)
        for new in names:
            self.assertIn(new, self.rules.weapons)
            self.assertEqual(len(self.rules.inherits_of(self.rules.weapon(new))), 3)
            self.assertTrue(all(parent.startswith('^') for _, parent in
                                self.rules.inherits_of(self.rules.weapon(new))))

    def test_incendiary_converter_guards_cover_each_new_root(self):
        self.assertEqual(len(cohort.YAK_OWNED_SOURCES), 5)
        for new in cohort.YAK_OWNED_SOURCES:
            self.assertEqual(cohort.ROOTS[new], ('Flame_Light', set(), 8000, 9988))
            self.assertEqual(cohort.descendants(self.rules, new), set())
            self.assertEqual(cohort.resolved_hash(self.rules, new, 'Flame_Light'),
                             cohort.PRESERVED_HASHES[new])

    def test_incendiary_diagnostic_class_is_preserved(self):
        for new in cohort.YAK_OWNED_SOURCES:
            entry = extract_stats.weapon_entry(self.rules, new)
            self.assertEqual(entry['design_weapon_class'], .875)
            self.assertEqual(entry['weapon_class_source'], 'template')
            self.assertEqual(entry['warheads'], ['^Warhead_Flame_Light', '^Warhead_Bullet_Medium'])
            self.assertIn('^Compatibility_IncendiaryYakComposition', entry['versus_templates'])

    def test_each_owned_payload_matches_its_original(self):
        for actor, route in self.before['routes'].items():
            for old, new in route.items():
                with self.subTest(weapon=new):
                    self.assertTrue(new.startswith(actor+'_'))
                    actual = node_to_obj(self.rules.resolve_weapon(new))
                    self.assertEqual(digest(actual), self.before['weapon_hashes'][old])

    def test_all_five_actor_payloads_only_change_weapon_identity(self):
        count = 0
        for actor, route in self.before['routes'].items():
            reverse = {new: old for old, new in route.items()}
            actual = node_to_obj(self.rules.resolve(actor))
            self._reverse_later_su57_missile_names(actor, actual)
            for key, trait in actual.items():
                if key.split('@')[0] == 'Armament' and isinstance(trait, dict):
                    if trait.get('Weapon') in reverse:
                        trait['Weapon'] = reverse[trait['Weapon']]
                        count += 1
            self.assertEqual(digest(actual), self.before['actor_hashes'][actor])
        self.assertEqual(count, 20)

    def _reverse_later_su57_missile_names(self, actor, obj):
        if actor != 'ra1_soviets_su57attackbomber':
            return
        for slot, suffix, old in (
                ('Armament', '_missile', 'Su57Maverick'),
                ('Armament@HE', '_missile_thermobaric', 'Su57MaverickThermobaric')):
            self.assertEqual(obj[slot]['Weapon'], actor + suffix)
            obj[slot]['Weapon'] = old

    def test_later_su57_adapter_preserves_missile_slots(self):
        actor = 'ra1_soviets_su57attackbomber'
        normal, upgraded = actor + '_missile', actor + '_missile_thermobaric'
        obj = {'Armament': {'Weapon': normal}, 'Armament@HE': {'Weapon': upgraded}}
        self._reverse_later_su57_missile_names('other_actor', obj)
        self.assertEqual(obj['Armament']['Weapon'], normal)
        for wrong in ('Su57Maverick', upgraded, 'OtherWeapon'):
            bad = {'Armament': {'Weapon': wrong}, 'Armament@HE': {'Weapon': upgraded}}
            with self.assertRaises(AssertionError):
                self._reverse_later_su57_missile_names(actor, bad)
        with self.assertRaises(KeyError):
            self._reverse_later_su57_missile_names(actor, {'Armament@PRIMARY': {'Weapon': normal}})
        self._reverse_later_su57_missile_names(actor, obj)
        self.assertEqual(obj['Armament']['Weapon'], 'Su57Maverick')
        self.assertEqual(obj['Armament@HE']['Weapon'], 'Su57MaverickThermobaric')

    def test_no_other_actor_borrows_the_new_guns(self):
        owners = {new: actor for actor, route in self.before['routes'].items()
                  for new in route.values()}
        for actor in self.rules.actors:
            if actor.startswith('^'):
                continue
            for arm in self.rules.resolve(actor).children_named('Armament'):
                weapon = arm.get('Weapon')
                self.assertNotIn(weapon, self.before['weapon_hashes'])
                if weapon in owners:
                    self.assertEqual(actor, owners[weapon])

    def test_no_old_executable_reference_in_active_rules_or_weapons(self):
        old = {name.casefold() for name in self.before['weapon_hashes']}
        def walk(node):
            self.assertFalse(old & {token.casefold() for token in
                                   re.split(r'[\s,]+', node.value)}, node.key)
            for child in node.children:
                walk(child)
        for node in list(self.rules.actors.values()) + list(self.rules.weapons.values()):
            walk(node)


if __name__ == '__main__':
    unittest.main()
