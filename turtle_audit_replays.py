"""Streaming replay order counts; host client is NOT a bot-seat identity."""
import collections, json, pathlib, struct, sys
from turtle_audit_inventory import roots, OUT, capture_metadata
sys.path.insert(0,r'C:\cameo-wt\ab-campaign-port\tools\ai')
import order_stream_diff as parser
captures=[]
for root in roots:
 for log in root.rglob('Logs'):
  if not log.is_dir() or not (log/'debug.log').exists():continue
  if not ((log/'server.log').exists() or (log/'cameo-ai-matches.jsonl').exists()):continue
  flags,receipt,metadata=capture_metadata(log);lastwt=0
  missions=collections.Counter();commits=collections.defaultdict(set);uid=None
  mf=log/'cameo-ai-missions.jsonl'
  if mf.exists():
   for line in mf.open():
    try:r=json.loads(line)
    except ValueError:continue
    uid=r.get('game_uid',uid);lastwt=max(lastwt,r.get('tick',0));missions[(r.get('seat'),r.get('by'),r.get('state') or r.get('event'))]+=1
    if r.get('by')=='Squads' and r.get('state')=='COMMITTED':commits[r.get('seat')].add(r.get('attempt_id') or r.get('mission_id'))
  support=log.parent; rp=support/'Replays'
  replaylist=list(rp.rglob('*.orarep')) if rp.exists() else []
  counts=collections.Counter();buckets=collections.Counter();unparsed=0;incomplete=False;frames=0
  for replay in replaylist:
   ended=False
   with replay.open('rb') as f:
    while True:
     h=f.read(4)
     if len(h)!=4:incomplete=True;break
     client=struct.unpack('<i',h)[0]
     if client==-1:ended=True;break
     length=f.read(4)
     if len(length)!=4:incomplete=True;break
     n=struct.unpack('<i',length)[0]
     if n<0 or n>16*1024*1024:incomplete=True;break
     packet=f.read(n)
     if len(packet)!=n:incomplete=True;break
     if n<4:unparsed+=1;continue
     frame=struct.unpack_from('<i',packet)[0];frames=max(frames,frame)
     if frame<=0 or n==4 or packet[4] in (parser.PKT_SYNCHASH,parser.PKT_DISCONNECT):continue
     try:orders,tails=parser.parse_orders(packet)
     except (ValueError,IndexError,struct.error):unparsed+=1;continue
     unparsed+=tails
     for order in orders:
      if order[0]!='O':continue
      name=order[1].decode('utf8','replace') if isinstance(order[1],bytes) else order[1]
      counts[name]+=1;buckets[(frame//5000*5000,name)]+=1
   if not ended:incomplete=True
  classification='DIAGNOSTIC_UNVERIFIED'
  if flags['unregistered_bot'] or flags['overlay_rejected']:classification='INVALID_OVERLAY_OR_ALIAS'
  elif flags['geometry_crash'] or flags['botname_crash']:classification='INCOMPLETE_CRASH'
  elif receipt.get('end_class')=='INCOMPLETE':classification='INCOMPLETE_RECEIPT'
  elif incomplete:classification='INCOMPLETE_REPLAY'
  elif receipt.get('end_class')=='CAP':classification='CAP_CENSORED_ARM_PROOF_PENDING'
  captures.append(dict(log=str(log),game_uid=uid,metadata=metadata,flags=dict(flags),classification=classification,match_records_present=(log/'cameo-ai-matches.jsonl').exists(),replays=[str(p) for p in replaylist],replay_incomplete=incomplete,unparsed=unparsed,last_order_frame=frames,last_mission_tick=lastwt,orders=dict(counts),order_buckets=[{'frame_bucket':t,'order':o,'count':n} for (t,o),n in sorted(buckets.items())],squad_commit_intents_by_seat={s:len(v) for s,v in commits.items()}))
(OUT/'capture_orders.json').write_text(json.dumps(captures,indent=2))
print('capture roots',len(captures),'classes',dict(collections.Counter(c['classification'] for c in captures)))
for c in captures:
 if '4v4_ice' in c['log']:print(json.dumps({k:c[k] for k in ('log','classification','game_uid','last_order_frame','replay_incomplete','unparsed','squad_commit_intents_by_seat')}|{'offensive_orders':{k:v for k,v in c['orders'].items() if k in ('Attack','AttackMove','AssaultMove')}}))
