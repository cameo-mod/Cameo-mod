"""Bounded streaming observation summaries. Counters describe recorder semantics."""
import collections,csv,json,pathlib
out=pathlib.Path(__file__).parent/'turtle-audit-results';captures=json.loads((out/'capture_orders.json').read_text());summaries=[];situations=[];placements=[];engagements=[]
for cap in captures:
 log=pathlib.Path(cap['log']); seats=collections.defaultdict(lambda:{'engagement_records':0,'placements':collections.Counter(),'placement_reasons':collections.Counter(),'personality_samples':collections.Counter(),'attacks_launched_max':None,'first_attack_tick':None,'last_situation':None})
 for filename in ('cameo-ai-situations.jsonl','cameo-ai-placements.jsonl','cameo-ai-engagements.jsonl'):
  p=log/filename
  if not p.exists():continue
  for line in p.open(encoding='utf8'):
   try:r=json.loads(line)
   except ValueError:continue
   key=(r.get('game_uid'),r.get('seat'));s=seats[key]
   if filename.endswith('situations.jsonl'):
    own=r.get('own',{});tick=r.get('tick');s['personality_samples'][r.get('personality_current','')]+=1
    n=own.get('attacks_launched')
    if n is not None:s['attacks_launched_max']=max(s['attacks_launched_max'] or 0,n)
    first=own.get('first_attack_tick');s['first_attack_tick']=first if first is not None and first>=0 else s['first_attack_tick']
    row={'path':str(p),'game_uid':key[0],'seat':key[1],'tick':tick,'personality':r.get('personality_current'),'urgency':r.get('urgency'),**{k:own.get(k) for k in ('army_value','defence_value','buildings','harvesters','attacks_launched','banked_cash','brownout_ticks','idle_production_ticks','production_queues','production_window','squad_count','squad_units')}}
    situations.append(row);s['last_situation']=row
   elif filename.endswith('placements.jsonl'):
    if r.get('kind')!='placement':continue
    category=r.get('category');s['placements'][category]+=1;s['placement_reasons'][r.get('reason')]+=1
    placements.append({'game_uid':key[0],'seat':key[1],'tick':r.get('tick'),'actor':r.get('actor'),'category':category,'reason':r.get('reason'),'power_cumulative':s['placements']['power'],'defence_cumulative':s['placements']['defence']+s['placements']['defense']})
   elif r.get('record')=='engagement':
    s['engagement_records']+=1;engagements.append({'game_uid':key[0],'seat':key[1],'start_tick':r.get('start_tick'),'end_tick':r.get('end_tick'),'kind':r.get('kind'),'close_reason':r.get('close_reason')})
 for (uid,seat),s in seats.items():summaries.append({'log':str(log),'game_uid':uid,'seat':seat,**s})
(out/'behavior_summary.json').write_text(json.dumps(summaries,indent=2))
for name,rows in [('situations',situations),('placements',placements),('engagements',engagements)]:
 with (out/(name+'_timeline.csv')).open('w',newline='',encoding='utf8') as f:
  w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
print('rows:',len(situations),len(placements),len(engagements))
for s in summaries:
 if s['game_uid']=='354f6de7-dddd-4910-b84b-e7432af2f195':print(json.dumps(s))
