"""Derive recorded truth timelines and time-aligned comparisons; no simulation."""
import json,pathlib,re,csv
out=pathlib.Path(__file__).parent/'turtle-audit-results';c=json.loads((out/'census.json').read_text());truth={};conflicts=0
for g in c['games']:
 for line in pathlib.Path(g['path']).open():
  r=json.loads(line)
  for v in r.get('opponent_signatures',[]):
   t=v.get('truth',{});k=(r['game_uid'],v['seat'],v['tick'])
   if k in truth and truth[k]!=t:conflicts+=1;continue
   truth[k]=t
bound=[{'game_uid':uid,'seat':seat,'tick':tick,**{k:v.get(k) for k in ('army_value','defence_value','building_count','harvester_count')}} for (uid,seat,tick),v in sorted(truth.items())]
with (out/'truth_timeline.csv').open('w',newline='') as f:
 w=csv.DictWriter(f,fieldnames=list(bound[0]));w.writeheader();w.writerows(bound)
for g in c['games']:
 p=g['metadata']['archived_ai_path']
 if p:
  s=pathlib.Path(p).read_text();blocks=re.split(r'(?=^\t[^\t\n #])',s,flags=re.M)
  g['metadata']['switch_bot_lists']={b.splitlines()[0].strip():re.findall(r'^\t\tBots:\s*(.*)',b,re.M) for b in blocks if b.startswith('\tGrantConditionOnBotOwner@') and any(x in b.splitlines()[0] for x in ('scaletargets','armyfirst','squaddesire','m13'))}
  g['metadata']['budget_fields']={k:re.findall(r'^\t\t'+k+r': (.*)',s,re.M) for k in ('GlobalUnitBudget','MaxUnitsPerBot')}
(out/'census.json').write_text(json.dumps(c,indent=2))
rows=list(csv.DictReader((out/'timeline.csv').open()));comp=[]
for seat in c['seats']:
 if seat['map']!='A Nuclear Winter':continue
 for at in (7500,14250):
  rr=[r for r in rows if r['game_uid']==seat['game_uid'] and r['seat']==seat['seat'] and int(r['tick'])<=at]
  if not rr:continue
  r=rr[-1];comp.append({'path':seat['path'],'seat':seat['seat'],'bot':seat['bot'],'faction':seat['faction'],'requested_tick':at,**{k:r[k] for k in ('tick','army','assets','army_assets_share','net_spent','banked')}})
(out/'nuclear_time_comparison.json').write_text(json.dumps(comp,indent=2))
print('truth samples',len(bound),'conflicting repeated truth samples',conflicts,'comparison rows',len(comp))
print(json.dumps([x for x in comp if x['requested_tick']==14250 and ('base-1337-map1-r1' in x['path'] or 'campaign-validation-20261011b' in x['path'])]))
