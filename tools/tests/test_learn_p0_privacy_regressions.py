"""Regressions for independent LEARN-P0 review findings 2, 3, 5, 7, 8."""
import copy
import json
from pathlib import Path
import sys
from unittest.mock import patch
import pytest
ROOT=Path(__file__).resolve().parents[2]
sys.path[:0]=[str(ROOT/'tools/ai'),str(ROOT/'tools/audit')]
import audit_no_player_names as audit
import anonymize_legacy_logs as legacy
import fit_opponent_signatures as signatures
import fit_arsenal_priors as arsenal
import expansion_report as expansion
import build_order_report as build_order
import run_ai_match_batch as batch
import takeover_smoke


def write(path,rows):
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(''.join(json.dumps(r)+'\n' for r in rows),encoding='utf-8')
    return path


def match(seat,outcome='won'):
    return {'schema':3,'game_uid':'00000000-0000-0000-0000-000000000001','record_id':'00000000-0000-0000-0000-000000000001|'+seat,
            'player':{'seat':seat,'faction':'td_gdi','bot_type':'hard','outcome':outcome,'home':'2,3'},
            'stats':{'stats_timeline_fields':'tick,earned,spent,army_value,assets_value,kills_cost,deaths_cost,banked,idle_queues',
                     'stats_timeline':[[750,0,0,4000,0,0,0,0,0]]},'opponent_signatures':[]}


@pytest.mark.parametrize('mutation',[
    lambda r:r.update(takeover={'controller_client':19}),
    lambda r:r.update(stats={'display_name':'Bravo'}),
    lambda r:r.update(ownership={'examples':[{'name':'Bravo'}]}),
    lambda r:r['player'].update(faction='Bravo'),
    lambda r:r.update(record_id='00000000-0000-0000-0000-000000000001|Bravo'),
    lambda r:r.update(opponent_signatures=[{'seat':'seat_13','seen':{'name':'Bravo'},'truth':{}}]),
])
def test_finding_2_nested_fields_and_values_fail_closed(tmp_path,mutation):
    row=match('seat_4');mutation(row)
    assert audit.validate_logs([write(tmp_path/'bad.jsonl',[row])])


@pytest.mark.parametrize('name,body',[
    ('arsenal_priors.yaml','BotArsenalPriors:\n\tPlayer@Bravo: 19\n'),
    ('build_order_knobs.yaml','BotBuildOrderKnobs:\n\tPlayer@Bravo: 19\n'),
    ('plan_bandits.yaml','BotPlanBandits:\n\tPlayer@Bravo: 19\n'),
    ('opponent_signatures.yaml','schema: "cameo-opponent-signatures/1"\nsource_schema: 3\nclusters:\n  - id: "sig_0001"\n    faction: "Bravo"\n'),
    ('other.txt','Bravo'),
])
def test_finding_2_every_learned_body_and_nested_file_checked(tmp_path,name,body):
    path=tmp_path/'mods/cameo/ai/learned/nested'/name;path.parent.mkdir(parents=True);path.write_text(body)
    assert path in audit.learned_paths(tmp_path)
    assert audit.validate_learned(audit.learned_paths(tmp_path))


def test_finding_2_mission_placement_and_engagement_references_closed(tmp_path):
    rows=[{'schema':'mission-card/2','seat':'seat_4','mission_id':'raid:Bravo:r4'},
          {'schema':2,'kind':'placement','seat':'seat_4','record_id':'00000000-0000-0000-0000-000000000001|Bravo|750'},
          {'schema':'engagement/2','seat':'seat_4','record_id':'00000000-0000-0000-0000-000000000001|seat_4|1','display_name':'Bravo'},
          {'schema':3,'kind':'situation','seat':'seat_4','tick':750,'record_id':'00000000-0000-0000-0000-000000000001|seat_4|750',
           'enemies':[{'seat':'seat_13','client_id':19}]}]
    assert len(audit.validate_logs([write(tmp_path/'bad.jsonl',rows)]))==4


def test_finding_3_fresh_two_seat_readers_and_hidden_faction_pipeline(tmp_path):
    matches=[match('seat_4'),match('seat_13','lost')]
    snaps=[{'schema':3,'kind':'situation','game_uid':'00000000-0000-0000-0000-000000000001','seat':seat,'tick':750,
            'record_id':f'00000000-0000-0000-0000-000000000001|{seat}|750','enemies':[{'seat':foe,'army_value':1000}],
            'expansion':{'fields_harvested':i,'refineries':i}}
           for i,(seat,foe) in enumerate([('seat_4','seat_13'),('seat_13','seat_4')],1)]
    places=[{'seat':seat,'game_uid':'00000000-0000-0000-0000-000000000001','tick':tick,'actor':'td_gdi_minigunner','category':'refinery','reason':'base'}
            for seat,tick in [('seat_4',100),('seat_13',200)]]
    d=tmp_path/'batch';write(d/'Logs/cameo-ai-matches.jsonl',matches);write(d/'Logs/cameo-ai-situations.jsonl',snaps)
    fitted=arsenal.fit([d]);assert fitted['matches']==1;assert len(fitted['visibility'][0])==2
    data={'matches':matches,'situations':snaps,'placements':places}
    ex=expansion.build(data)['matches'];assert len(ex)==2;assert [x['first_refinery_tick'] for x in ex]==[200,100]
    bo=build_order.build(data)['matches'];assert len(bo)==2;assert sorted(x['outcome']['win'] for x in bo)==[0,1]
    matches[0]['opponent_signatures']=[{'seat':'seat_13','seen':{'faction':'','army_value':100},'truth':None}]
    fitted=signatures.fit([write(tmp_path/'match.jsonl',matches)])
    assert fitted['clusters'][0]['faction']=='unknown'
    output=tmp_path/'opponent_signatures.yaml';output.write_text(signatures.dump_yaml(fitted))
    assert audit.validate_learned([output])==[]


def test_finding_5_truth_features_change_relabel_and_survive_fit_dump_audit(tmp_path):
    row=match('seat_4');seen={k:0 for k in signatures.FEATURES};seen.update(faction='',army_value=100)
    truth={k:0 for k in signatures.FEATURES};truth.update(faction='td_nod',army_value=800)
    row['opponent_signatures']=[{'seat':'seat_13','tick':750,'seen':seen,'truth':truth}]
    path=write(tmp_path/'match.jsonl',[row]);a=signatures.fit([path])
    assert a['relabels'][0]['recognized']!=a['relabels'][0]['actual']
    row['opponent_signatures'][0]['truth']['army_value']=3200
    b=signatures.fit([write(path,[row])]);assert a!=b
    out=tmp_path/'opponent_signatures.yaml';out.write_text(signatures.dump_yaml(b))
    assert audit.validate_learned([out])==[]
    out.write_text(signatures.dump_yaml(b).replace('army_value:', 'player_name:'))
    assert audit.validate_learned([out])


def test_finding_7_mission_only_archive_target_removed_backup_first_and_idempotent(tmp_path):
    path=write(tmp_path/legacy.LOG_NAMES[3],[{'schema':'mission-card/1','player':'Alpha','game_uid':'00000000-0000-0000-0000-000000000001',
                                          'mission_id':'raid:Bravo:r4','attempt':1}])
    original=path.read_bytes();replace=legacy.os.replace
    def guarded(src,dest):
        backups=list(tmp_path.glob('cameo-ai-legacy-backup-*'))
        assert len(backups)==1 and (backups[0]/path.name).read_bytes()==original
        assert 'contain' in (backups[0]/'manifest.json').read_text()
        replace(src,dest)
    with patch.object(legacy.os,'replace',side_effect=guarded): result=legacy.migrate(tmp_path)
    assert result['records_rewritten']==1
    assert 'Alpha' not in path.read_text() and 'Bravo' not in path.read_text()
    assert audit.validate_logs([path])==[]
    first=path.read_bytes();assert legacy.migrate(tmp_path)['records_rewritten']==0;assert path.read_bytes()==first


def test_finding_8_physical_homes_not_neutral_slot_offset_or_zero_spawn():
    text='\tPlayerReference@Multi0:\n\t\tHomeLocation: 2,3\n\tPlayerReference@Multi1:\n\t\tHomeLocation: 50,60\n'
    a=match('seat_4');a['player']['spawn']=0
    b=match('seat_5');b['player'].update(home='50,60',spawn=0)
    assert batch.physical_spawn(a,text)==0;assert batch.physical_spawn(b,text)==1
    b['player']['home']='99,99';assert batch.physical_spawn(b,text) is None


def test_takeover_smoke_accepts_actual_anonymous_seat_after_neutral_slots():
    result={'game_started':True};ev={'exceptions':[],'sync_reports':[]}
    assert takeover_smoke.j_admin_kill(result,ev,[('c1','seat_4',{'trigger':'disconnect'})])[0]
    assert not takeover_smoke.j_admin_kill(result,ev,[('c1','Bravo',{'trigger':'disconnect'})])[0]
