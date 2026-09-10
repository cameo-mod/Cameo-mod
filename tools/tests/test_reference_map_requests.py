"""Maintainer regressions: original Mammoth ownership and traceable weapon cycles."""
import json
import pathlib
import sys
import unittest
from unittest.mock import patch
ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/balance'))
import assign_references as ar
import build_reference_report as report

class Requests(unittest.TestCase):
    def test_original_mammoth_displaces_siege(self):
        source = 'DTA Enhanced'
        peer = {'id': '4TNK', 'name': 'Soviet Mammoth Tank', 'hp': 7600, 'cost': 1700}
        original, siege = 'ra1_soviets_mammothtank', 'ra1_soviets_siegemammothtank'
        with patch.object(ar, 'REFERENCE_OVERRIDES', {(original, source): ar.REFERENCE_OVERRIDES[(original, source)]}):
            result = ar.apply_overrides({siege: {source: peer}}, {source: [peer]}, {}, False)
        self.assertEqual(result[original][source]['id'], '4TNK')
        self.assertNotIn(source, result[siege])
        saved = json.loads((ROOT / 'docs/balance/derived/reference_assignment.json').read_text())['assignment']
        self.assertEqual(saved[original][source]['id'], '4TNK')
        self.assertNotIn(source, saved[siege])

    def test_reviewed_burst_and_unknown_delays_are_distinct(self):
        profile = {('fixture', 'A'): {'damage': 10, 'reload': 20, 'burst': 3, 'burst_delays': [2, 2], 'dps': 1.25}}
        rows = [{'source': 'fixture', 'id': 'A', 'w_dps': 1.25},
                {'source': 'fixture', 'id': '<B>', 'w_damage': 5, 'w_reload': 10, 'w_burst': 2, 'w_dps': None}]
        with patch('peer_nominal_evidence.load', return_value=profile):
            page = report.weapon_calculation_details(rows)
        self.assertIn('10 × 3 / (20 + 4) = 1.25 damage/tick', page)
        self.assertIn('burst delays (source ticks): unavailable', page)
        self.assertIn('&lt;B&gt;', page)
        self.assertIn('model DPS eligible: no', page)

    def test_hero_projection_uses_separate_population(self):
        row={'id':'hero','type':'infantry','hero':True}
        counts=dict(actors=0,refs=0,none=0,thin=0)
        ordinary, frozen, hero_dist, hero_frozen=object(),object(),object(),object()
        with patch.object(report,'estimate_cell',return_value='estimate') as estimate:
            report.emit([],['hero'],{'hero':row},{},{},set(),ordinary,frozen,counts,{}, {},(hero_dist,hero_frozen))
        self.assertEqual(estimate.call_count,5)
        for call in estimate.call_args_list:
            self.assertIs(call.args[3],hero_dist)
            self.assertIs(call.args[4],hero_frozen)

    def test_shared_commando_override_preserves_both_factions(self):
        sources=('Combined Arms','DTA Enhanced','OpenRA Tiberian Dawn')
        actors=('td_gdi_commando','td_nod_commando')
        pool={source:[{'id':'RMBO','name':'Commando','hp':100,'cost':100}] for source in sources}
        overrides={(actor,source):'RMBO' for actor in actors for source in sources}
        with patch.object(ar,'REFERENCE_OVERRIDES',overrides):
            result=ar.apply_overrides({},pool,{},False)
        for actor in actors:
            self.assertEqual(set(result[actor]),set(sources))
            self.assertTrue(all(r['id']=='RMBO' for r in result[actor].values()))

    def test_sparse_source_is_not_misreported_as_missing_weapon_data(self):
        row={'source':'One hero source','id':'RMBO','type':'infantry','hp':100}
        with patch.object(report.rt,'target_for',return_value=(None,None,0)):
            cell=report.estimate_cell([row],row,'hp',{}, {},1)
        self.assertIn('raw statistic available',cell)
        self.assertIn('minimum three usable rows',cell)

if __name__ == '__main__':
    unittest.main()
