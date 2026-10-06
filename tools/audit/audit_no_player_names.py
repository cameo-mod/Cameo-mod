#!/usr/bin/env python3
"""Validate anonymous logs and every learned artifact against closed grammars.

Field shapes are independently pinned in anonymous_log_shapes.json; unknown
nested keys, scalar types and reference/value tokens fail closed. Public map
metadata is retained, while player descriptors/references are only seat_N.
"""
from __future__ import annotations
import argparse
from functools import lru_cache
import json
import math
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
SHAPES = json.loads(Path(__file__).with_name('anonymous_log_shapes.json').read_text())
TYPES = json.loads(Path(__file__).with_name('anonymous_log_types.json').read_text())
FEATURES = ('army_value', 'infantry_value', 'vehicle_value', 'air_value', 'naval_value',
            'defence_value', 'building_count', 'harvester_count', 'known_regions')
SEAT = re.compile(r'seat_[1-9][0-9]*\Z')
UID = re.compile(r'(?:[a-fA-F0-9]{32}|[a-fA-F0-9]{8}(?:-[a-fA-F0-9]{4}){3}-[a-fA-F0-9]{12}|legacy-[a-f0-9]{16})\Z')
CELL = re.compile(r'-?[0-9]+,-?[0-9]+\Z')
PERSONALITIES = {'rush','turtle','techrush','expansion','steamroller','guerrilla'}
KNOBS = {'tempo','greed','production','tech','defence','power_margin','expansion','support'}
ROLES = {'Infantry','AntiInfantry','AntiArmor','AntiAir','AntiShip','Artillery','Scout','Detector',
         'Harasser','Siege','Support','Transport','Naval','Air','Tech','Economy','Defence','Production',
         'infantry','vehicle','air','naval','defence','other','anti_infantry','anti_armour','anti_air',
         'artillery','scout','detector','harasser','siege','support','transport','economy','production','tech','rush','idle','guerrilla','harass','air','fighter','gunship','bomber','protection','frontline','unclassified','antiarmor','antiair','antiship'}
METADATA = {'map_title','mod_version','game_uid','map_uid','recorded_utc'}

@lru_cache(maxsize=1)
def vocabulary():
    actors, factions, words, modules = set(), {'', 'unknown'}, {''}, set()
    # Canonical rules/configuration only. Never derive vocabulary from input logs
    # or learned files (which are precisely the untrusted artifacts being checked).
    for base in ('rules','ContentPacks','ai'):
        for p in (ROOT/'mods/cameo'/base).rglob('*.yaml'):
            if 'learned' in p.parts:
                continue
            text = p.read_text(encoding='utf-8-sig')
            actors.update(v.lower() for v in re.findall(r'^([A-Za-z0-9_^.\-]+):(?:\s|$)',text,re.M))
            factions.update(re.findall(r'^\t\tInternalName: ([A-Za-z0-9_.&\-]+)',text,re.M))
            # Recipe keys and scalar code tokens are reviewed public vocabulary.
            words.update(re.findall(r'[A-Za-z_][A-Za-z0-9_.\-]*',text))
    for base in ('OpenRA.Mods.CA','OpenRA.Mods.Cameo','OpenRA.Mods.Fransbot'):
        for p in (ROOT/base/'Traits').rglob('*.cs'):
            text=p.read_text(encoding='utf-8-sig')
            modules.update(re.findall(r'\b(?:class|enum) ([A-Za-z][A-Za-z0-9_]*)',text))
            for literal in re.findall(r'"([^"\r\n]*)"',text):
                words.update(re.findall(r'[A-Za-z_][A-Za-z0-9_.\-]*',literal))
            for body in re.findall(r'\benum\s+\w+[^\{]*\{([^}]+)\}', text):
                for member in re.findall(r'\b([A-Za-z_][A-Za-z0-9_]*)\s*(?:=|,|$)', body):
                    words.update({member, member.lower(), member.upper()})
    words.update(PERSONALITIES|ROLES|modules)
    return actors,factions,words,modules

@lru_cache(maxsize=1)
def bot_types():
    result={''}
    for p in (ROOT/'mods/cameo/ai').glob('*.yaml'):
        active=False
        for line in p.read_text(encoding='utf-8-sig').splitlines():
            if line.startswith('\t') and not line.startswith('\t\t') and line.strip() and not line.lstrip().startswith('#'):
                active=bool(re.match(r'\t(?:ModularBot|ModularBotCA|FransBot)(?:@[^:]+)?:',line))
            if active:
                m=re.fullmatch(r'\t\tType: ([A-Za-z0-9_.-]+)',line)
                if m: result.add(m[1])
    return result

def scope_ok(value):
    factions=vocabulary()[1]
    def atom(s):
        return s in factions-{'','unknown'} or s=='any' or (s.startswith('family_') and s[7:] in {f.split('_')[0] for f in factions if f})
    return isinstance(value,str) and all(atom(s) for s in value.split('__vs__')) and value.count('__vs__')<=1

def mission_ok(value):
    if not isinstance(value,str): return False
    if value=='unknown': return True
    if re.fullmatch(r'(?:recon|raid|secure|defend):(?:seat_[1-9][0-9]*|self|unknown):(?:r-?[0-9]+|region_[0-9]+)',value): return True
    if re.fullmatch(r'(?:frans:[0-9]+|garrison_contest:a[0-9]+|veto:(?:attack|defend|field|retreat|flee|engage):[0-9]+)',value): return True
    parts=value.split(':')
    if len(parts)==3 and parts[0]=='capture': return parts[1] in vocabulary()[0] and parts[2].isdigit()
    if len(parts)==4 and parts[0]=='capture': return (parts[1]=='unknown' or bool(SEAT.fullmatch(parts[1]))) and parts[2] in vocabulary()[0] and parts[3].isdigit()
    return False

def token_text(value):
    if not isinstance(value,str): return False
    if not value: return True
    # Compound diagnostics/events contain only canonical code words and numeric
    # quantities, separated by the emitter's punctuation. No arbitrary prose.
    tokens=re.findall(r'[A-Za-z_][A-Za-z0-9_.\-]*',value)
    remainder=re.sub(r'[A-Za-z_][A-Za-z0-9_.\-]*','',value)
    return all(t in vocabulary()[2] for t in tokens) and re.fullmatch(r'[0-9\s,;:+/#=|().<>%\-]*',remainder) is not None

def scalar_ok(key,value):
    if key in {'staging_cell','army_to_staging_cells'} and value is None: return True
    if key=='game_uid': return value=='' or (isinstance(value,str) and UID.fullmatch(value) is not None)
    if key in METADATA: return isinstance(value,str) and '|' not in value and '\n' not in value and '\r' not in value
    if key in {'bot_type','bot'}: return isinstance(value,str) and value in bot_types()
    if key in {'personality','personality_current','personality_candidate','personality_arm'}: return value=='' or value in PERSONALITIES
    if key=='urgency': return value in {'','normal','pressured','emergency'}
    if key=='record_kind': return value in {'mission','attempt'}
    if key=='record': return value in {'engagement','posture'}
    if key=='seat': return isinstance(value,str) and SEAT.fullmatch(value) is not None
    if key in {'main_target','coalition_main_target'}: return value=='' or (isinstance(value,str) and SEAT.fullmatch(value) is not None)
    if key in {'faction','enemy_faction'}: return isinstance(value,str) and value in vocabulary()[1]
    if key=='scope': return scope_ok(value)
    if key=='mission_id': return mission_ok(value)
    if key=='attempt_id': return isinstance(value,str) and re.fullmatch(r'.+\|A[0-9]+',value) is not None and mission_ok(value.rsplit('|',1)[0])
    if key in {'home','cell','anchor_cell','target_cell','unit_cell','centroid','army_centroid','crawl_target','mcv_site','target','staging_cell'}: return value=='' or (isinstance(value,str) and CELL.fullmatch(value) is not None)
    if key in {'actor','type'}: return isinstance(value,str) and (value in vocabulary()[0] or value in {'recon','raid','secure','defend','capture','attack','retreat','garrison_contest','unknown'})
    if key=='outcome': return value in {'won','lost','undecided'}
    if key=='stats_timeline_fields': return value=='tick,earned,spent,army_value,assets_value,kills_cost,deaths_cost,banked,idle_queues'
    if key=='fingerprint': return isinstance(value,str) and re.fullmatch(r'[A-Fa-f0-9]{8,64}',value) is not None
    if key in TYPES['booleans']: return type(value) is bool
    if key in TYPES['numbers'] or key in {'double_owner','two_squads','held_by_disabled','dead_held','orphan'} or key.startswith(('base_','now_','seen_','target_')):
        return type(value) in (int,float) and math.isfinite(value)
    return token_text(value)

def numeric_map(value,keys):
    return isinstance(value,dict) and value.keys()<=keys and all(type(v) in (int,float) and math.isfinite(v) for v in value.values())

def object_ok(value,path):
    if not isinstance(value,dict) or path not in SHAPES or not value.keys()<=set(SHAPES[path]): return False
    for key,item in value.items():
        child=path+'.'+key
        if key in {'schema','record_id'} and path in {'match','situation','placement','mission','engagement'}: continue
        if child in SHAPES:
            if item is None and key in {'seen','mission','mission_assignment'}: continue
            if isinstance(item,list):
                if not all(object_ok(v,child) for v in item): return False
            elif not object_ok(item,child): return False
        elif key=='killed_by_victim' or (path.endswith('.composition') and key in {'own_units','own_defences','enemy_units','enemy_defences','units','defences'}):
            if not numeric_map(item,vocabulary()[0]): return False
        elif key in {'losses_by_role','away_losses_by_role','own_lost_by_role'}:
            if not numeric_map(item,ROLES): return False
        elif key=='versus':
            if not isinstance(item,dict) or any(not isinstance(k,str) or len(k.split('|'))!=2 or any(t not in vocabulary()[2] for t in k.split('|')) or type(v) not in (int,float) or not math.isfinite(v) for k,v in item.items()): return False
        elif key=='stats_timeline':
            if not isinstance(item,list) or any(not isinstance(row,list) or len(row)!=9 or any(type(n) is not int for n in row) for row in item): return False
        elif key=='fields_in_reach_unserved_ids':
            if not isinstance(item,list) or any(type(n) is not int for n in item): return False
        elif not scalar_ok(key,item): return False
    return True

def record_ok(row):
    if not isinstance(row,dict): return False
    schema=row.get('schema')
    kind='match' if schema==3 and isinstance(row.get('player'),dict) else 'situation' if schema==3 and row.get('kind')=='situation' else 'placement' if schema==2 and row.get('kind') in {'placement','refinery_lost','refinery_acquired'} else 'mission' if schema=='mission-card/2' else 'engagement' if schema=='engagement/2' else None
    if not kind or not object_ok(row,kind): return False
    seat=(row.get('player') or {}).get('seat') if kind=='match' else row.get('seat')
    if not isinstance(seat,str) or not SEAT.fullmatch(seat): return False
    if kind=='mission': return mission_ok(row.get('mission_id'))
    ident=row.get('record_id');uid=row.get('game_uid','')
    if not isinstance(ident,str): return False
    parts=ident.split('|')
    if len(parts)<2 or not UID.fullmatch(parts[0]) or parts[1]!=seat or (uid and parts[0]!=uid): return False
    if kind=='match': return len(parts)==2
    if kind=='situation': return len(parts)==3 and parts[2]==str(row.get('tick'))
    if kind=='engagement': return len(parts)==3 and re.fullmatch(r'(?:p|e)?[0-9]+',parts[2]) is not None
    if not parts[2].isdigit(): return False
    suffix=parts[3:]
    return all(v in vocabulary()[0] or v in {'placement','refinery_lost','refinery_acquired','unknown'} or CELL.fullmatch(v) for v in suffix)

match_ok=record_ok

def validate_logs(paths):
    errors=[]
    for path in paths:
        if not path.is_file():
            errors.append(f'{path}: missing log input'); continue
        for n,line in enumerate(path.read_text(encoding='utf-8-sig').splitlines(),1):
            if not line.strip(): continue
            try: ok=record_ok(json.loads(line))
            except (ValueError,TypeError): ok=False
            if not ok: errors.append(f'{path}:{n}: record does not conform to anonymous-seat schema')
    return errors

def learned_paths(root):
    return sorted(p for p in (root/'mods/cameo/ai/learned').rglob('*') if p.is_file())

def validate_learned(paths):
    errors=[]
    roots={'arsenal_priors.yaml':'BotArsenalPriors:', 'build_order_knobs.yaml':'BotBuildOrderKnobs:', 'plan_bandits.yaml':'BotPlanBandits:'}
    for path in paths:
        lines=[x for x in path.read_text(encoding='utf-8-sig').splitlines() if x.strip() and not x.lstrip().startswith('#')]
        ok=bool(lines)
        if path.name=='opponent_signatures.yaml':
            ok=signature_yaml_ok(lines)
        elif path.name in roots and lines and lines[0]==roots[path.name]:
            parent=None
            for line in lines[1:]:
                m=re.fullmatch(r'(\t{1,2})([^:]+):\s*(.*)',line)
                if not m: ok=False; break
                depth,key,val=len(m[1]),m[2],m[3]
                if depth==1:
                    parent=None
                    if path.name=='arsenal_priors.yaml':
                        if key=='VisibilityPercentByPhase': ok=bool(re.fullmatch(r'[0-9, ]*',val))
                        else:
                            scope=key.removeprefix('TradePercent@');ok=key.startswith('TradePercent@') and scope_ok(scope) and not val;parent='actor'
                    elif path.name=='build_order_knobs.yaml':
                        head,sep,scope=key.partition('@');pers,sep2,scope=scope.partition('__');ok=head in {'Knobs','Openings'} and bool(sep and sep2) and pers in PERSONALITIES and scope_ok(scope) and not val;parent=head
                    else:
                        head,sep,scope=key.partition('@');ok=head in {'Personality','Plan'} and bool(sep) and scope_ok(scope) and not val;parent=head
                        if key=='Processed': ok=bool(re.fullmatch(r'[a-f0-9]{10}(?:,[a-f0-9]{10})*',val))
                else:
                    if parent=='actor': ok=key in vocabulary()[0] and bool(re.fullmatch(r'[0-9]+',val))
                    elif parent=='Knobs': ok=key in KNOBS and bool(re.fullmatch(r'[0-9]+',val))
                    elif parent=='Openings': ok=key in vocabulary()[2] and bool(re.fullmatch(r'[0-9]+ [0-9]+',val))
                    elif parent in {'Personality','Plan'}: ok=(key in PERSONALITIES if parent=='Personality' else key in vocabulary()[2]) and bool(re.fullmatch(r'[0-9]+ -?[0-9]+(?:\.[0-9]+)? [0-9]+(?:\.[0-9]+)?',val))
                    else: ok=False
                if not ok: break
        else: ok=False
        if not ok: errors.append(f'{path}: unknown or invalid learned-file grammar')
    return errors

def signature_yaml_ok(lines):
    # Deterministic output has fixed layout; parsing it as generic YAML would
    # permit arbitrary nested payloads. Validate every line and closed token.
    if len(lines)<3 or lines[:3]!=['schema: "cameo-opponent-signatures/1"','source_schema: 3','clusters:']: return False
    state='clusters';features=set();bands=None
    for line in lines[3:]:
        if line=='relabels:':
            if features and features!=set(FEATURES): return False
            state='relabels';continue
        if line=='  []': continue
        if state=='clusters':
            if re.fullmatch(r'  - id: "sig_[0-9]{4}"',line):
                if features and features!=set(FEATURES): return False
                features=set();continue
            m=re.fullmatch(r'    faction: "([^"\n]*)"',line)
            if m:
                if m[1] not in vocabulary()[1]-{''}: return False
                continue
            if re.fullmatch(r'    count: [0-9]+',line) or line=='    center:': continue
            m=re.fullmatch(r'    bands: \[([0-9, ]*)\]',line)
            if m:
                if len(m[1].split(','))!=len(FEATURES): return False
                continue
            m=re.fullmatch(r'      ([a-z_]+): [0-9]+',line)
            if m and m[1] in FEATURES and m[1] not in features: features.add(m[1]);continue
            return False
        elif not re.fullmatch(r'(?:  - recognized: "sig_[0-9]{4}"|    actual: "sig_[0-9]{4}"|    count: [0-9]+)',line): return False
    return not features or features==set(FEATURES)

def main():
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--repo',type=Path,default=ROOT);ap.add_argument('--logs',type=Path,nargs='*',default=[]);args=ap.parse_args()
    errors=validate_logs(args.logs)+validate_learned(learned_paths(args.repo))
    if errors: print('\n'.join(errors),file=sys.stderr);return 1
    print(f'anonymous log grammar: PASS ({len(args.logs)} log inputs, {len(learned_paths(args.repo))} learned files)');return 0

if __name__=='__main__': raise SystemExit(main())
