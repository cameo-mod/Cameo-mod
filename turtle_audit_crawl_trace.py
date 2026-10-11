"""Join EX-1 placement diagnostics using exact tick/type/cell, never host client ID."""
import csv,pathlib,re,json,collections
out=pathlib.Path(__file__).parent/'turtle-audit-results';placements=list(csv.DictReader((out/'placements_timeline.csv').open()));uid='354f6de7-dddd-4910-b84b-e7432af2f195'
log=pathlib.Path(r'C:\cameo-wt\ab-campaign-port\results\campaign-validation-polyfix\4v4_ice_2026_0\Support\Logs');keys=collections.defaultdict(set)
for line in (log/'cameo-ai-placements.jsonl').open():
 r=json.loads(line)
 if r.get('kind')=='placement':keys[(r['tick'],r['actor'],r['cell'])].add(r['seat'])
joined=[];ambiguous=0;missing=0
pattern=re.compile(r'EX-1 BaseCrawl (\S+) at (\d+),(\d+) toward field (\d+),(\d+) at tick (\d+)')
for line in (log/'debug.log').open():
 m=pattern.search(line)
 if not m:continue
 actor,x,y,fx,fy,tick=m.groups();seats=keys[(int(tick),actor,f'{x},{y}')]
 if len(seats)!=1:
  ambiguous+=len(seats)>1;missing+=not seats;continue
 joined.append({'game_uid':uid,'seat':next(iter(seats)),'tick':int(tick),'actor':actor,'cell':f'{x},{y}','field_aim':f'{fx},{fy}','placement_to_aim_distance_squared':(int(x)-int(fx))**2+(int(y)-int(fy))**2})
with (out/'ice_crawl_diagnostics.csv').open('w',newline='') as f:
 w=csv.DictWriter(f,fieldnames=list(joined[0]));w.writeheader();w.writerows(joined)
summary=[]
for seat in sorted({r['seat'] for r in joined}):
 rr=[r for r in joined if r['seat']==seat];counts=collections.Counter(r['field_aim'] for r in rr);summary.append({'seat':seat,'joined_crawls':len(rr),'unique_aim_cells':len(counts),'most_repeated_aim':counts.most_common(1)[0],'late_crawls':sum(r['tick']>=30000 for r in rr)})
(out/'ice_crawl_diagnostics_summary.json').write_text(json.dumps({'joined':len(joined),'ambiguous':ambiguous,'unmatched':missing,'seats':summary},indent=2));print(summary)
