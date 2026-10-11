"""Read-only streaming census. Captures are diagnostic, never campaign approvals."""
import collections, csv, hashlib, json, pathlib, re
ROOT=pathlib.Path(r'C:\cameo-wt')
OUT=pathlib.Path(__file__).parent/'turtle-audit-results';OUT.mkdir(exist_ok=True)
ai=(ROOT/'ab-campaign-port/mods/cameo/ai/ai.yaml').read_text()
def types(key):
    return set(x.strip().lower() for m in re.finditer(r'^\t\t'+key+r': ([^\n]+)',ai,re.M) for x in m[1].split(','))
powers=types('PowerTypes');defenses=types('DefenseTypes')
buildings=set()
for m in re.finditer(r'^\t\tBuildingFractions:\s*\n((?:\t\t\t[^\n]*\n)*)',ai,re.M):
    buildings.update(re.findall(r'^\t\t\t([^:#\s]+):',m[1],re.M))
buildings|=powers|defenses
roots=[p/'results' for p in ROOT.iterdir() if p.is_dir() and ('campaign' in p.name or p.name=='sol-ab-1008') and (p/'results').is_dir()]
roots.extend(p for p in (ROOT/'sol-ab-1008').glob('results-*') if p.is_dir())
def capture_metadata(log):
    flags=collections.Counter()
    cell=log.parent.parent if log.parent.name=='Support' else log.parent
    paths=[log/'debug.log',*log.glob('exception-*.log'),cell/'driver.log']
    for p in paths:
      if not p.exists():continue
      for line in p.open(encoding='utf8',errors='replace'):
        for label,needle in [('unregistered_bot','Invalid bot type: hard_'),('overlay_rejected','ContentPacks/Outpost2/content.yaml'),('geometry_crash','square root of a negative integer'),('botname_crash','Sequence contains no matching element'),('bot_budget','BotBudget ')]:
          if needle in line:flags[label]+=1
    receipt_path=cell.parent/'receipts'/(cell.name+'.json')
    receipt=json.loads(receipt_path.read_text()) if receipt_path.exists() else {}
    ai_path=cell/'FullMods/mods/cameo/ai/ai.yaml'
    if not ai_path.exists():ai_path=cell/'Overlay/mods/cameo/ai/ai.yaml'
    return flags,receipt,{'receipt_path':str(receipt_path) if receipt else None,'receipt_end_class':receipt.get('end_class'),'receipt_baseline_claim':receipt.get('baseline_commit'),'archived_ai_path':str(ai_path) if ai_path.exists() else None,'archived_ai_sha256':hashlib.sha256(ai_path.read_bytes()).hexdigest() if ai_path.exists() else None}

games=[];seats=[];samples=[];errors=[];seen=set();files=0
for root in roots:
 for path in root.rglob('cameo-ai-matches.jsonl'):
    files+=1;log=path.parent
    flags,receipt,metadata=capture_metadata(log)
    lua=log/'lua.log';text=lua.read_text(errors='replace') if lua.exists() and lua.stat().st_size<2000000 else ''
    cap='AB_CAMPAIGN_CAP' in text or 'COORD_TICK_CAP' in text
    bad_deploy='AB_CAMPAIGN_NO_DEPLOY' in text
    gidset=set();local=[]
    try:
      for line in path.open(encoding='utf8'):
        r=json.loads(line);key=r.get('record_id') or (r.get('game_uid'),r.get('player',{}).get('seat'))
        if key in seen:continue
        seen.add(key);g=r.get('game_uid');gidset.add(g);pl=r.get('player',{});stats=r.get('stats',{});timeline=stats.get('stats_timeline',[])
        zero=bool(timeline) and all(t[1]==0 and t[2]==0 for t in timeline)
        classification='DIAGNOSTIC_CAPTURE_ARM_PROOF_PENDING'
        if flags['unregistered_bot'] or flags['overlay_rejected']:classification='INVALID_OVERLAY_OR_ALIAS'
        elif bad_deploy or zero:classification='INVALID_NO_DEPLOY' if bad_deploy else 'SUSPECT_INACTIVE_ZERO_ECONOMY'
        elif flags['geometry_crash'] or flags['botname_crash']:classification='INCOMPLETE_CRASH'
        elif receipt.get('end_class')=='INCOMPLETE':classification='INCOMPLETE_RECEIPT'
        elif cap:classification='CAP_CENSORED_ARM_PROOF_PENDING'
        arsenal=r.get('arsenal',[])
        row=dict(path=str(path),game_uid=g,map=r.get('map_title'),duration=r.get('duration_ticks'),seat=pl.get('seat') or pl.get('name'),bot=pl.get('bot_type'),faction=pl.get('faction'),home=pl.get('home'),personality=pl.get('personality'),classification=classification,sample_count=len(timeline),created_total=sum(x.get('created',0) for x in arsenal),power_created=sum(x.get('created',0) for x in arsenal if x.get('type','').lower() in powers),defense_created=sum(x.get('created',0) for x in arsenal if x.get('type','').lower() in defenses),building_catalog_created=sum(x.get('created',0) for x in arsenal if x.get('type','').lower() in buildings),other_created=sum(x.get('created',0) for x in arsenal if x.get('type','').lower() not in buildings),peak_army=max((t[3] for t in timeline),default=None),peak_nonarmy_assets=max((t[4]-t[3] for t in timeline),default=None),peak_banked=max((t[7] for t in timeline if len(t)>7),default=None),units_killed=stats.get('units_killed'),units_lost=stats.get('units_lost'),kills_cost=stats.get('kills_cost'),deaths_cost=stats.get('deaths_cost'),production_spend_share=None,attack_launches=None,engagements=None,arsenal=[{k:x[k] for k in ('type','created','lost') if k in x} for x in arsenal],last_sample=timeline[-1] if timeline else None)
        seats.append(row);local.append(row)
        for t in timeline:
          samples.append(dict(game_uid=g,seat=pl.get('seat') or pl.get('name'),tick=t[0],earned=t[1],net_spent=t[2],army=t[3],assets=t[4],nonarmy_assets=t[4]-t[3],army_assets_share=round(t[3]/t[4],4) if t[4] else None,kills_cost=t[5],deaths_cost=t[6],banked=t[7] if len(t)>7 else None))
    except (ValueError,OSError) as e:errors.append({'path':str(path),'error':str(e)})
    if local:games.append(dict(path=str(path),game_uids=sorted(gidset),map=local[0]['map'],seats=len(local),flags=dict(flags),cap_marker=cap,metadata=metadata,classifications=dict(collections.Counter(x['classification'] for x in local))))
out={'match_files':files,'unique_seats':len(seats),'games':games,'seats':seats,'errors':errors,'method':'created counts include starting/captured/transform actors; catalogs describe types, not paid production. nonarmy assets is assets-army, not proven building-only value. Null launch/spend metrics explicitly unavailable.'}
(OUT/'census.json').write_text(json.dumps(out,indent=2))
with (OUT/'timeline.csv').open('w',newline='') as f:
 w=csv.DictWriter(f,fieldnames=list(samples[0]) if samples else ['game_uid']);w.writeheader();w.writerows(samples)
print(json.dumps({'match_files':files,'unique_seats':len(seats),'games':len(games),'samples':len(samples),'errors':len(errors),'classifications':dict(collections.Counter(x['classification'] for x in seats))}))
for g in games:
 if g['map']=='Ice Cold':
  s=[s for s in seats if s['path']==g['path']]
  print(json.dumps({'path':g['path'],'seats':len(s),'class':g['classifications'],'power_created':[x['power_created'] for x in s],'defense_created':[x['defense_created'] for x in s],'peak_army':[x['peak_army'] for x in s],'kills':[x['units_killed'] for x in s]}))
