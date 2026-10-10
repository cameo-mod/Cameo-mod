#!/usr/bin/env python3
"""Fail-closed planner/adjudicator for the maintainer's 64-game BU pilot.

This revision plans and audits evidence only. It never starts OpenRA.
"""
from __future__ import annotations
import argparse, hashlib, json, os, pathlib, random, re, sqlite3, subprocess, sys, time, zipfile
from contextlib import contextmanager
from threading import Lock
import apply_increment_switches as increment

SWITCH = "BU_harvester_logistics"
MAPS = {
 "nuclear_winter": ("mods/cameo/maps/_ra_a-nuclear-winter.oramap", "191efefe3043acb2fd504b2e2e5c974a49f6498f8e016c1380159c9f7efc1c4a", 2),
 "satans_clutch": ("mods/cameo/maps/SatansClutch.oramap", "1bd3bee0bbb38c95015126ba689496c4284c2f4e8f8b6573711a9fdadcbf16cb", 2),
 "red_spice": ("mods/cameo/maps/Red_Spice_2v2_BI-4.4.oramap", "882f865b4e556f93324bbd1abb31397bff30b651489652fa151c031cf02cb65a", 4),
 "terra_cotta": ("mods/cameo/maps/Terracotta-ratls2.oramap", "c1a66fba029ccde5998fbe57b563f87569425ace82617e42e6e42e3fcc9e740d", 4),
 "back_to_basics": ("mods/cameo/maps/back-to-basics.oramap", "ddd071d131895ecb59be66b7d1924b023731c52a04ee01052fa9fc384dd15e6b", 6),
 "winters_end_rich": ("mods/cameo/maps/winters-end-rich.oramap", "a29eea3fef3087d5544c428d2d07d59f4430806c0b23db7eeb605dde7013c94e", 6),
 "great_sahara_2": ("mods/cameo/maps/Great_Sahara_3.oramap", "8b5c089c7cd2f8209819afa04436d6fa15849e125823ddc16bf5e996cd280ddc", 8),
 "ice_cold": ("mods/cameo/maps/ice_cold.oramap", "e097fcca4fa40f5878b4fb7d965529728dbbd34c2a14b8825e1bec0aea6da98b", 8),
}
# Each entry is one faction-mirror setup; the same tuple is installed on each team.
SETUPS = [
 ("1v1_nuclear_gdi",1,"nuclear_winter",("td_gdi",)),("1v1_nuclear_nod",1,"nuclear_winter",("td_nod",)),
 ("1v1_satans_allies",1,"satans_clutch",("ra1_allies",)),("1v1_satans_soviets",1,"satans_clutch",("ra1_soviets",)),
 ("2v2_red_gdi_nod",2,"red_spice",("td_gdi","td_nod")),("2v2_red_gdi_allies",2,"red_spice",("td_gdi","ra1_allies")),("2v2_red_gdi_soviets",2,"red_spice",("td_gdi","ra1_soviets")),
 ("2v2_terra_nod_allies",2,"terra_cotta",("td_nod","ra1_allies")),("2v2_terra_nod_soviets",2,"terra_cotta",("td_nod","ra1_soviets")),("2v2_terra_allies_soviets",2,"terra_cotta",("ra1_allies","ra1_soviets")),
 ("3v3_basics_gna",3,"back_to_basics",("td_gdi","td_nod","ra1_allies")),("3v3_basics_nas",3,"back_to_basics",("td_nod","ra1_allies","ra1_soviets")),
 ("3v3_winter_asg",3,"winters_end_rich",("ra1_allies","ra1_soviets","td_gdi")),("3v3_winter_sgn",3,"winters_end_rich",("ra1_soviets","td_gdi","td_nod")),
 ("4v4_sahara",4,"great_sahara_2",("td_gdi","td_nod","ra1_allies","ra1_soviets")),("4v4_ice",4,"ice_cold",("td_gdi","td_nod","ra1_allies","ra1_soviets")),
]
RAM_FLOOR_BYTES = 6 * 1024**3
PROCESS_CAP_BYTES = 8 * 1024**3
ROUND_STAGES = [16,32,48,64]
ECONOMY_INTERVAL_TICKS = tuple(range(5000, 45001, 5000))
FIX_SAFETY_SWITCHES = frozenset("K_cn2_unit_repair AL_emergency_net_loss AN_combat_veto AG_assault_fanout BJ_squad_hysteresis BK_squad_order_dedup BL_protection_episode_guard BN_squad_pool_fixes BO_squad_move_dedup".split())
_, _ALL_GROUPS, _ = increment.load_spec(pathlib.Path(__file__).with_name("increment_switches.yaml"))
BEHAVIOR_SWITCHES = tuple(name for name in _ALL_GROUPS if name not in FIX_SAFETY_SWITCHES)
DEFAULT_THRESHOLDS = {"early_accept_share":0.75,"early_reject_share":0.25,"win_noninferiority_delta":0.0,"min_earned_spent_improvement":0.0,"min_army_efficiency_improvement":0.0,"min_kill_exchange_improvement":0.0}
SLOT_POLICY={"max_parallel_small_games":3,"small_team_size_max":2,"large_game_exclusive":True,"min_free_ram_bytes":RAM_FLOOR_BYTES,"max_private_bytes":PROCESS_CAP_BYTES}

def treatment_baseline(accepted_switches, candidate):
 """Ratchet rule: accepted groups stay in control; treatment adds one candidate."""
 require(candidate in BEHAVIOR_SWITCHES,"candidate is unknown or classified fix/safety")
 require(candidate not in accepted_switches and len(set(accepted_switches))==len(accepted_switches),"candidate already accepted or duplicate baseline group")
 return {"control_groups":list(accepted_switches),"treatment_groups":list(accepted_switches)+[candidate]}

def acceptance_decision(*, rounds, treatment_wins, control_wins, economy_not_worse,
                        economy_better, army_not_worse, earned_spent_delta=None,
                        army_efficiency_delta=None, kill_exchange_delta=None, crashed=False, thresholds=None):
 """Apply predeclared sequential boundaries; no natural games means PARK."""
 t=thresholds or DEFAULT_THRESHOLDS
 require(all(k in t for k in DEFAULT_THRESHOLDS),"acceptance thresholds must be frozen")
 require(0<=t["early_reject_share"]<t["early_accept_share"]<=1,"invalid predeclared win thresholds")
 n=treatment_wins+control_wins
 if crashed: return "REJECT"
 if not n: return "PARK" if rounds>=64 else "CONTINUE"
 share=treatment_wins/n
 if rounds>=16 and share>=t["early_accept_share"] and economy_not_worse and army_not_worse: return "ACCEPT"
 if rounds>=16 and share<=t["early_reject_share"]: return "REJECT"
 if rounds<64: return "CONTINUE"
 if share<0.5+t["win_noninferiority_delta"]: return "REJECT"
 metrics=(earned_spent_delta,army_efficiency_delta,kill_exchange_delta)
 if any(x is None for x in metrics): return "PARK"
 if (earned_spent_delta>t["min_earned_spent_improvement"] and
     army_efficiency_delta>t["min_army_efficiency_improvement"] and
     kill_exchange_delta>t["min_kill_exchange_improvement"]): return "ACCEPT"
 return "REJECT"

def regression_due(accepted_count):
 return accepted_count>0 and accepted_count%5==0

def slots_available(team_size, free_ram_bytes, active_private_bytes=(), active_slots=0, active_team_sizes=(), max_small_slots=3):
 """Admission policy for the amended slot counter; callers still need OS memory monitoring."""
 if free_ram_bytes<RAM_FLOOR_BYTES or any(x>=PROCESS_CAP_BYTES for x in active_private_bytes): return False
 if team_size>=3: return active_slots==0
 return active_slots<max_small_slots and not any(size>=3 for size in active_team_sizes)

class SqliteSlotCounter:
 """Process-shared lease counter. Runtime must heartbeat and monitor process/RAM continuously."""
 def __init__(self,database_path,lease_seconds=3600):
  self.database_path=pathlib.Path(database_path).resolve();self.lease_seconds=lease_seconds
  require(type(lease_seconds)is int and lease_seconds>0,"lease duration must be positive")
  self.database_path.parent.mkdir(parents=True,exist_ok=True)
  with self._connect() as db: db.execute("CREATE TABLE IF NOT EXISTS active_slots (cell_id TEXT PRIMARY KEY,pid INTEGER NOT NULL,team_size INTEGER NOT NULL,private_bytes INTEGER NOT NULL,expires_at REAL NOT NULL)")
 @contextmanager
 def _connect(self):
  db=sqlite3.connect(self.database_path,timeout=30,isolation_level=None)
  try:
   db.execute("PRAGMA journal_mode=WAL");db.execute("PRAGMA busy_timeout=30000");yield db
  finally: db.close()
 @staticmethod
 def _pid_alive(pid):
  try: os.kill(pid,0);return True
  except ProcessLookupError:return False
  except PermissionError:return True
  except OSError:return False
 def _begin(self,db):
  db.execute("BEGIN IMMEDIATE");now=time.time()
  for cell,pid in db.execute("SELECT cell_id,pid FROM active_slots WHERE expires_at < ?",(now,)).fetchall():
   if not self._pid_alive(pid):db.execute("DELETE FROM active_slots WHERE cell_id=?",(cell,))
 def acquire(self,cell_id,team_size,free_ram_bytes,pid=None):
  pid=os.getpid() if pid is None else pid
  with self._connect() as db:
   self._begin(db);active=db.execute("SELECT team_size,private_bytes FROM active_slots").fetchall()
   allowed=slots_available(team_size,free_ram_bytes,[x[1] for x in active],len(active),[x[0] for x in active])
   duplicate=db.execute("SELECT 1 FROM active_slots WHERE cell_id=?",(cell_id,)).fetchone() is not None
   if not allowed or duplicate:db.rollback();return False
   db.execute("INSERT INTO active_slots VALUES(?,?,?,?,?)",(cell_id,pid,team_size,0,time.time()+self.lease_seconds));db.commit();return True
 def heartbeat(self,cell_id,private_bytes=None):
  if private_bytes is not None: require(type(private_bytes)is int and 0<=private_bytes<PROCESS_CAP_BYTES,"process memory cap reached")
  with self._connect() as db:
   self._begin(db)
   cur=db.execute("UPDATE active_slots SET private_bytes=COALESCE(?,private_bytes),expires_at=? WHERE cell_id=?",(private_bytes,time.time()+self.lease_seconds,cell_id))
   if cur.rowcount!=1:db.rollback();raise ValueError("unknown or expired slot owner")
   db.commit()
 def release(self,cell_id):
  with self._connect() as db:
   self._begin(db);db.execute("DELETE FROM active_slots WHERE cell_id=?",(cell_id,));db.commit()

class SlotCounter:
 """Thread-safe in-process reservation counter; persistence/expiry belongs to runtime adapter."""
 def __init__(self): self._lock=Lock(); self._active={}; self._sizes={}
 def acquire(self,cell_id,team_size,free_ram_bytes,active_private_bytes=()):
  with self._lock:
   if cell_id in self._active or not slots_available(team_size,free_ram_bytes,[*active_private_bytes,*self._active.values()],len(self._active),self._sizes.values()):
    return False
   self._active[cell_id]=0;self._sizes[cell_id]=team_size;return True
 def update_private_bytes(self,cell_id,private_bytes):
  with self._lock:
   require(cell_id in self._active,"unknown slot owner")
   require(0<=private_bytes<PROCESS_CAP_BYTES,"process memory cap reached")
   self._active[cell_id]=private_bytes
 def release(self,cell_id):
  with self._lock: self._active.pop(cell_id,None);self._sizes.pop(cell_id,None)

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def ai_payload_sha(root,switch=None):
 skip,groups,_=increment.load_spec(pathlib.Path(__file__).with_name("increment_switches.yaml"));digest=hashlib.sha256()
 if switch is not None: require(switch in groups,"unknown AI payload switch")
 for path in sorted((root/"mods"/"cameo"/"ai").glob("*.yaml")):
  text=path.read_text(encoding="utf-8")
  if switch is not None:
   for trait,fields in groups[switch].items(): text,_=increment.apply(text,trait,fields,skip)
  rel=path.relative_to(root).as_posix().encode();body=text.encode();digest.update(len(rel).to_bytes(4,"big"));digest.update(rel);digest.update(len(body).to_bytes(8,"big"));digest.update(body)
 return digest.hexdigest()
def require(ok,msg):
 if not ok: raise ValueError(msg)

def normalized_timeline(record):
 """Read dict rows or the canonical flattened stats timeline."""
 timeline=record.get("stats_timeline")
 if isinstance(timeline,list) and all(isinstance(row,dict) for row in timeline): return timeline
 stats=record.get("stats")
 fields=stats.get("stats_timeline_fields") if isinstance(stats,dict) else None
 rows=stats.get("stats_timeline") if isinstance(stats,dict) else None
 if not isinstance(fields,str) or not isinstance(rows,list): return []
 names=fields.split(",")
 return [dict(zip(names,row)) for row in rows if isinstance(row,list) and len(row)==len(names)]

def paired_bootstrap_interval(values,seed):
 """Deterministic percentile CI over independent matched-pair effects."""
 if not values: return None
 rng=random.Random(seed); samples=sorted(sum(rng.choice(values) for _ in values)/len(values) for _ in range(4000))
 return [samples[int(.025*(len(samples)-1))],samples[int(.975*(len(samples)-1))]]

def make_jobs(manifest):
 seeds=manifest.get("seeds")
 require(isinstance(seeds,list) and len(seeds)==2 and len(set(seeds))==2 and all(type(x)==int and x>=0 for x in seeds),"pilot needs exactly two distinct nonnegative seeds")
 jobs=[]
 for setup,size,mapname,factions in SETUPS:
  rel,digest,slots=MAPS[mapname]
  for pair,seed in enumerate(seeds):
   for game in range(2):
    ca,ta=("A","B") if game==0 else ("B","A")
    assignments=[]
    for side in ("A","B"):
     homes=range(0,size) if (side=="A")==(game==0) else range(size,2*size)
     rotated=list(factions) if game==0 else list(reversed(factions))
     arm="control" if side==ca else "treatment"
     for offset,(spawn,faction) in enumerate(zip(homes,rotated)):
      assignments.append({"home":f"Multi{spawn}","spawn":spawn,"side":side,"arm":arm,"bot_type":manifest["arm_bot_types"][arm],"faction":faction,"team_slot":offset})
    jobs.append({"cell_id":f"{setup}|{seed}|{game}","switch":SWITCH,"setup":setup,"team_size":size,"map":mapname,"map_path":rel,"map_sha256":digest,"seed":seed,"pair":pair,"game_in_pair":game,"control_side":ca,"treatment_side":ta,"team_factions":list(factions),"required_seats":2*size,"arm_bot_types":manifest["arm_bot_types"],"seat_assignments":assignments})
 return jobs

def validate(manifest,root,engine_root=None):
 require(manifest.get("schema")==1 and manifest.get("switch")==SWITCH,"schema/switch mismatch")
 require(manifest.get("pairs_per_setup")==2 and manifest.get("team_size_scope")==[1,2,3,4],"pilot design mismatch")
 require(manifest.get("arm_bot_types")=={"control":"hard_control","treatment":"hard_treatment"},"distinct control/treatment hard bot bindings required")
 require(manifest.get("round_stages")==ROUND_STAGES and manifest.get("acceptance_thresholds")==DEFAULT_THRESHOLDS,"round stages and exact acceptance thresholds must be frozen")
 require(manifest.get("slot_policy")==SLOT_POLICY,"parallel slot/memory policy must be frozen")
 baseline=manifest.get("baseline_manifest_sha256")
 require(isinstance(baseline,str) and len(baseline)==64 and all(c in "0123456789abcdef" for c in baseline.lower()),"ratchet baseline manifest SHA required")
 basepath=(root/pathlib.PurePosixPath(manifest.get("baseline_manifest_path",""))).resolve()
 require(root.resolve() in basepath.parents and basepath.is_file() and sha(basepath)==baseline,"ratchet baseline manifest file/hash mismatch")
 pins=manifest.get("pins")
 require(isinstance(pins,dict) and pins.get("source_commit")=="700bb16483f6d92664153e98d6f2560c312acaab" and pins.get("engine_version")=="6da7fce14da541180c6baddd6925118fbef65b94","source/engine pin mismatch")
 p=subprocess.run(["git","-C",str(root),"cat-file","-e",pins["source_commit"]+"^{commit}"],capture_output=True,timeout=20)
 require(p.returncode==0,"pinned source commit unavailable")
 require(manifest.get("switch_patch_sha256")==increment.group_patch_sha256(increment.load_spec(pathlib.Path(__file__).with_name("increment_switches.yaml"))[1][SWITCH]),"BU patch SHA mismatch")
 require(manifest.get("baseline_ai_payload_sha256")==ai_payload_sha(root),"baseline AI payload mismatch")
 require(manifest.get("treatment_ai_payload_sha256")==ai_payload_sha(root,SWITCH),"one-switch treatment AI payload mismatch")
 tool_paths={"driver_analyzer":pathlib.Path(__file__),"batch_runner":root/"tools"/"ai"/"run_ai_match_batch.py","switch_applier":root/"tools"/"ai"/"apply_increment_switches.py"}
 require(manifest.get("tool_sha256")=={k:sha(v) for k,v in tool_paths.items()},"tool SHA pins mismatch")
 binaries=manifest.get("binary_sha256")
 require(isinstance(binaries,dict) and len(binaries)==3 and all(len(v)==64 for v in binaries.values()),"engine binary SHA pins missing")
 if engine_root is not None:
  require((engine_root/"VERSION").read_text(encoding="ascii").strip()==pins["engine_version"],"engine VERSION mismatch")
  require({rel:sha((engine_root/pathlib.PurePosixPath(rel))) for rel in binaries}==binaries,"engine binary SHA mismatch")
 expected=[{"name":k,"path":v[0],"sha256":v[1],"required_seats":v[2]} for k,v in MAPS.items()]
 require(manifest.get("maps")==expected,"map path/SHA/seat-count pins mismatch")
 require(manifest.get("setups")==[{"id":n,"team_size":s,"map":m,"team_factions":list(f)} for n,s,m,f in SETUPS],"setup/faction schedule mismatch")
 for entry in expected:
  p=(root/pathlib.PurePosixPath(entry["path"])).resolve()
  require(root.resolve() in p.parents and p.is_file() and sha(p)==entry["sha256"],f"map package missing/hash mismatch: {entry['name']}")
  try:
   with zipfile.ZipFile(p) as z:
    y=z.read("map.yaml").decode("utf-8")
  except Exception as e: raise ValueError(f"invalid .oramap {entry['name']}: {e}")
  seats=len(set(re.findall(r"PlayerReference@Multi(\d+):",y)))
  require(seats>=entry["required_seats"],f"insufficient playable seats: {entry['name']}")
 # Explicitly non-executable: execution approvals and runtime backend do not exist in this tool.
 require(manifest.get("execution_approved") is False,"dry-run manifest must not authorize execution")
 return True

def dry_run(m,root,engine_root=None,minutes_per_game=8.5,setup_hours=2.0):
 validate(m,root,engine_root); jobs=make_jobs(m)
 small_games=sum(4 for _,s,_,_ in SETUPS if s<=2); large_games=len(jobs)-small_games
 serial_hours=round(len(jobs)*minutes_per_game/60+setup_hours,2)
 slot_hours=round((small_games/3+large_games)*minutes_per_game/60+setup_hours,2)
 serial_range=[round(len(jobs)*5/60+setup_hours,2),round(len(jobs)*12/60+setup_hours,2)]
 slot_range=[round((small_games/3+large_games)*5/60+setup_hours,2),round((small_games/3+large_games)*12/60+setup_hours,2)]
 return {"mode":"NO_LAUNCH_DRY_RUN","switch":SWITCH,"setups":len(SETUPS),"pairs":32,"games":len(jobs),"jobs":jobs,"launches":0,"execution_authorized":False,"worker_hours_estimate_at_8_5m_each":serial_hours,"parallel_slot_wall_estimate_hours_at_8_5m_each":slot_hours,"parallel_slot_wall_estimate_range_hours_5_to_12m_each":slot_range,"serial_wall_estimate_range_hours_5_to_12m_each":serial_range,"map_engine_acceptance_and_symmetric_spawn_proof":"PENDING_ENGINE_PREFLIGHT","round_stages":ROUND_STAGES,"acceptance_thresholds":m["acceptance_thresholds"],"slot_policy":m["slot_policy"],"ratchet_baseline_manifest_sha256":m["baseline_manifest_sha256"],"missing_campaign_gates":["reviewed integration/source/engine/tool/AI payload hashes frozen in final manifest","actual map acceptance and symmetric spawn proof on pinned engine","runtime launch orchestration: seat-specific bot trees/map.yaml producer, live RSS and wall/stall watchdogs, immutable receipts/resume, PID-scoped cleanup; SQLite leases need live integration","A5 parity and analyzer fixture/negative-control acceptance"]}

def adjudicate(records, receipt, job, manifest_sha):
 """One game -> one verdict; incomplete/cap evidence is never converted to a loss."""
 if not isinstance(receipt,dict) or receipt.get("cell_id")!=job["cell_id"] or receipt.get("manifest_sha256")!=manifest_sha:
  return {"verdict":"INVALID_UNKNOWN","reason":"cell or manifest pin mismatch"}
 if any(receipt.get(k)!=job[k] for k in ("switch","setup","map","map_sha256","seed","pair","game_in_pair")):
  return {"verdict":"INVALID_UNKNOWN","reason":"receipt disagrees with planned job"}
 status=receipt.get("status")
 if status in ("CRASH","INCOMPLETE","MEMORY_KILL","WALL_TIMEOUT","STALL","PROCESS_DIED"):
  return {"verdict":"INCOMPLETE_UNKNOWN","reason":status}
 cap=status=="CENSORED_CAP"
 if cap and not (receipt.get("cap_marker") is True and receipt.get("cap_tick")==45000 and receipt.get("support_complete") is True):
  return {"verdict":"INVALID_UNKNOWN","reason":"cap evidence incomplete"}
 if not cap and (status!="NATURAL_END" or receipt.get("end_reason")!="natural"):
  return {"verdict":"INVALID_UNKNOWN","reason":"natural end proof missing"}
 seats=receipt.get("seats")
 if not isinstance(seats,list) or len(seats)!=job["required_seats"] or len(records)!=len(seats):
  return {"verdict":"INVALID_UNKNOWN","reason":"seat/record count mismatch"}
 byhome={s.get("home"):s for s in seats if isinstance(s,dict)}
 if len(byhome)!=len(seats) or any(not isinstance(h,str) or not h for h in byhome):
  return {"verdict":"INVALID_UNKNOWN","reason":"seat homes missing or duplicated"}
 if receipt.get("seat_proof_source")!="generated_map.yaml" or not receipt.get("map_yaml_sha256"):
  return {"verdict":"INVALID_UNKNOWN","reason":"generated map.yaml proof missing"}
 if receipt.get("control_side")!=job["control_side"] or receipt.get("treatment_side")!=job["treatment_side"] or receipt.get("arm_bot_types")!=job["arm_bot_types"]:
  return {"verdict":"INVALID_UNKNOWN","reason":"seat assignment/bot binding differs from planned arms"}
 expected_seats={s["home"]:s for s in job["seat_assignments"]}
 if set(byhome)!=set(expected_seats): return {"verdict":"INVALID_UNKNOWN","reason":"resolved homes differ from planned spawn rotation"}
 for home,seat in byhome.items():
  expected=expected_seats[home]
  if any(seat.get(k)!=expected[k] for k in ("spawn","side","arm","bot_type","faction")):
   return {"verdict":"INVALID_UNKNOWN","reason":"resolved map.yaml seats differ from planned side/spawn/faction assignment"}
 side_outcomes={"A":[],"B":[]}; arm_totals={"control":{"earned":0,"spent":0,"banked":0},"treatment":{"earned":0,"spent":0,"banked":0}}; seat_rows=[]; seen_homes=set()
 for r in records:
  pl=r.get("player") or {}; home=pl.get("home"); seat=byhome.get(home)
  if seat is None or home in seen_homes or pl.get("faction")!=seat.get("faction") or pl.get("bot_type")!=seat.get("bot_type"):
   return {"verdict":"INVALID_UNKNOWN","reason":"record differs from resolved seat proof"}
  seen_homes.add(home)
  arm,side=seat.get("arm"),seat.get("side"); outcome=pl.get("outcome")
  if arm not in arm_totals or side not in side_outcomes or outcome not in ("won","lost"):
   return {"verdict":"INVALID_UNKNOWN","reason":"invalid arm/side/outcome"}
  if seat.get("bot_type")!=job["arm_bot_types"].get(arm):
   return {"verdict":"INVALID_UNKNOWN","reason":"resolved bot type does not prove arm"}
  if arm!=("control" if side==job["control_side"] else "treatment"):
   return {"verdict":"INVALID_UNKNOWN","reason":"resolved arm disagrees with side swap"}
  side_outcomes[side].append(outcome)
  timeline=normalized_timeline(r)
  final=timeline[-1] if timeline else {}
  eco={k:(final.get(k) if type(final.get(k)) is int else None) for k in ("earned","spent","banked")}
  eco["tick"]=final.get("tick") if type(final.get("tick")) is int else None
  eco["intervals"]={str(tick):next(({"earned":row.get("earned"),"spent":row.get("spent"),"banked":row.get("banked")} for row in timeline if row.get("tick")==tick and all(type(row.get(k)) is int for k in ("earned","spent","banked"))),None) for tick in ECONOMY_INTERVAL_TICKS}
  events=r.get("campaign_events")
  eco["campaign_events"]=events if isinstance(events,list) and all(isinstance(e,dict) and isinstance(e.get("kind"),str) and type(e.get("tick")) is int for e in events) else None
  if all(type(eco.get(k)) is int for k in ("earned","spent","banked")):
   for k in ("earned","spent","banked"): arm_totals[arm][k]+=eco[k]
  seat_rows.append({"home":home,"side":side,"arm":arm,"faction":seat.get("faction"),"outcome":outcome,"economy":eco})
 if any(len(side_outcomes[s])!=job["team_size"] for s in ("A","B")):
  return {"verdict":"INVALID_UNKNOWN","reason":"resolved sides lack expected seat count"}
 if sorted(s.get("faction") for s in seats if s.get("side")=="A")!=sorted(job["team_factions"]) or sorted(s.get("faction") for s in seats if s.get("side")=="B")!=sorted(job["team_factions"]):
  return {"verdict":"INVALID_UNKNOWN","reason":"faction mirror lineup differs from planned setup"}
 for arm in arm_totals:
  spent=arm_totals[arm]["spent"]
  arm_totals[arm]["earned_spent_ratio"]=arm_totals[arm]["earned"]/spent if spent else None
 if cap:
  return {"verdict":"CENSORED_CAP","reason":"verified cap","team_totals":arm_totals,"seats":seat_rows,
          "peak_memory_bytes":receipt.get("peak_memory_bytes"),"duration_ticks":receipt.get("duration_ticks"),
          "telemetry_intervals_ticks":list(ECONOMY_INTERVAL_TICKS)}
 winning=[s for s in ("A","B") if all(x=="won" for x in side_outcomes[s])]
 if len(winning)!=1 or any(x=="won" for x in side_outcomes["A"] if winning!=["A"]) or any(x=="won" for x in side_outcomes["B"] if winning!=["B"]):
  return {"verdict":"INVALID_UNKNOWN","reason":"team outcomes are mixed or lack one winner"}
 winner=winning[0]; arm="treatment" if winner==job["treatment_side"] else "control"
 event_totals={a:{} for a in arm_totals}; event_first={a:{} for a in arm_totals}
 for seat in seat_rows:
  events=seat["economy"].get("campaign_events")
  if events is None: continue
  for event in events:
   kind=event["kind"]; event_totals[seat["arm"]][kind]=event_totals[seat["arm"]].get(kind,0)+1
   event_first[seat["arm"]][kind]=min(event["tick"],event_first[seat["arm"]].get(kind,event["tick"]))
 return {"verdict":"TREATMENT_WIN" if arm=="treatment" else "CONTROL_WIN","winner_side":winner,"team_totals":arm_totals,"seats":seat_rows,
         "campaign_events":{"status":"RECORDED" if any(s["economy"].get("campaign_events") is not None for s in seat_rows) else "UNKNOWN","counts":event_totals,"first_tick":event_first},
         "synergy":"NOT_IDENTIFIABLE_FROM_PILOT (team-size strata lack matched individual counterfactuals)"}

def analyze(m, manifest_sha, receipts, rows):
 jobs={j["cell_id"]:j for j in make_jobs(m)}; by_cell={}; uid_receipts={}
 for r in receipts:
  require(isinstance(r,dict) and isinstance(r.get("cell_id"),str),"receipt requires cell_id")
  require(r["cell_id"] not in by_cell,"duplicate cell receipt")
  by_cell[r["cell_id"]]=r
  if isinstance(r.get("game_uid"),str):
   require(r["game_uid"] not in uid_receipts,"duplicate receipt game_uid")
   uid_receipts[r["game_uid"]]=r
 grouped={}
 for r in rows:
  uid=r.get("game_uid");require(isinstance(uid,str),"match row missing game_uid")
  grouped.setdefault(uid,[]).append(r)
 result=[]
 for cell,job in jobs.items():
  receipt=by_cell.get(cell)
  if receipt is None: verdict={"verdict":"INCOMPLETE_UNKNOWN","reason":"planned cell has no receipt"}
  else: verdict=adjudicate(grouped.get(receipt.get("game_uid"),[]),receipt,job,manifest_sha)
  result.append({"cell_id":cell,"setup":job["setup"],"team_size":job["team_size"],"map":job["map"],"seed":job["seed"],"pair":job["pair"],"game_in_pair":job["game_in_pair"],"duration_ticks":receipt.get("duration_ticks") if receipt else None,"peak_memory_bytes":receipt.get("peak_memory_bytes") if receipt else None,**verdict})
 extra=sorted(set(by_cell)-set(jobs))
 extra_uids=sorted(set(grouped)-set(uid_receipts))
 setups=[]
 for setup, size, mapname, _ in SETUPS:
  group=[r for r in result if r["setup"]==setup]
  pairs=[]
  for pair in (0,1):
   pg=[r for r in group if r["pair"]==pair]
   if len(pg)==2 and {r["game_in_pair"] for r in pg}=={0,1} and all(r["verdict"] in ("TREATMENT_WIN","CONTROL_WIN") for r in pg):
    t=sum(r["verdict"]=="TREATMENT_WIN" for r in pg); pairs.append((t-(2-t))/2)
  totals={arm:{metric:sum((r.get("team_totals") or {}).get(arm,{}).get(metric,0) for r in group if r.get("team_totals")) for metric in ("earned","spent","banked")} for arm in ("control","treatment")}
  for arm in totals:
   spent=totals[arm]["spent"];totals[arm]["earned_spent_ratio"]=totals[arm]["earned"]/spent if spent else None
  interval_metrics={arm:{str(tick):{"earned":0,"spent":0,"banked":0,"seat_samples":0} for tick in ECONOMY_INTERVAL_TICKS} for arm in ("control","treatment")}
  event_metrics={arm:{"status":"UNKNOWN","reported_seats":0,"missing_seats":0,"counts":{},"first_tick":{}} for arm in ("control","treatment")}
  for game in group:
   for seat in game.get("seats",[]):
    arm=seat.get("arm"); eco=seat.get("economy",{})
    if arm not in interval_metrics: continue
    for tick,values in eco.get("intervals",{}).items():
     if values is None: continue
     for metric in ("earned","spent","banked"): interval_metrics[arm][tick][metric]+=values[metric]
     interval_metrics[arm][tick]["seat_samples"]+=1
    if eco.get("campaign_events") is None: event_metrics[arm]["missing_seats"]+=1
    else:
     event_metrics[arm]["status"]="RECORDED";event_metrics[arm]["reported_seats"]+=1
    for event in (eco.get("campaign_events") or []):
     kind=event["kind"]; event_metrics[arm]["counts"][kind]=event_metrics[arm]["counts"].get(kind,0)+1
     event_metrics[arm]["first_tick"][kind]=min(event["tick"],event_metrics[arm]["first_tick"].get(kind,event["tick"]))
  setups.append({"setup":setup,"team_size":size,"map":mapname,"planned_games":4,"complete_natural_pairs":len(pairs),"paired_win_differences":pairs,
                 "paired_win_effect":sum(pairs)/len(pairs) if pairs else None,"paired_bootstrap_95ci":paired_bootstrap_interval(pairs,f"{setup}:{len(pairs)}"),
                 "natural_games":sum(r["verdict"] in ("TREATMENT_WIN","CONTROL_WIN") for r in group),
                 "censored_games":sum(r["verdict"]=="CENSORED_CAP" for r in group),
                 "incomplete_games":sum(r["verdict"]=="INCOMPLETE_UNKNOWN" for r in group),
                 "invalid_games":sum(r["verdict"]=="INVALID_UNKNOWN" for r in group),
                 "team_economy_totals":totals,"fixed_interval_team_economy":interval_metrics,"campaign_event_metrics":event_metrics,
                 "synergy":"NOT_IDENTIFIABLE_FROM_PILOT (team-size strata lack matched individual counterfactuals)"})
 return {"schema":1,"switch":SWITCH,"games":result,"setups":setups,"unplanned_receipt_cells":extra,"unplanned_game_uids":extra_uids,"telemetry_intervals_ticks":list(ECONOMY_INTERVAL_TICKS),"note":"One game is one observation; seats are clustered. Natural win effect and paired bootstrap interval require complete side-swapped pairs. Synergy is not estimated because individual and team setups differ in map/roster context."}

def main(argv=None):
 p=argparse.ArgumentParser(description=__doc__); s=p.add_subparsers(dest="cmd",required=True)
 d=s.add_parser("dry-run");d.add_argument("--manifest",type=pathlib.Path,required=True);d.add_argument("--manifest-sha256",required=True);d.add_argument("--repo-root",type=pathlib.Path,required=True);d.add_argument("--engine-root",type=pathlib.Path);d.add_argument("--output",type=pathlib.Path,required=True);d.add_argument("--minutes-per-game",type=float,default=8.5);d.add_argument("--setup-hours",type=float,default=2.0)
 a=s.add_parser("analyze");a.add_argument("--manifest",type=pathlib.Path,required=True);a.add_argument("--manifest-sha256",required=True);a.add_argument("--matches",type=pathlib.Path,required=True);a.add_argument("--receipts",type=pathlib.Path,required=True);a.add_argument("--repo-root",type=pathlib.Path,required=True);a.add_argument("--output",type=pathlib.Path,required=True)
 a=p.parse_args(argv)
 try:
  m=json.loads(a.manifest.read_text(encoding="utf-8"))
  if a.cmd=="dry-run":
   raw=a.manifest.read_bytes();actual=hashlib.sha256(raw).hexdigest();require(actual==a.manifest_sha256,"manifest SHA mismatch")
   out=dry_run(m,a.repo_root,a.engine_root,a.minutes_per_game,a.setup_hours);out["manifest_sha256"]=actual;a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(out,indent=2)+"\n",encoding="utf-8");print(f"NO_LAUNCH games={out['games']} worker_hours={out['worker_hours_estimate_at_8_5m_each']} slot_wall_hours={out['parallel_slot_wall_estimate_hours_at_8_5m_each']} launches=0");return 0
  validate(m,a.repo_root); raw=a.manifest.read_bytes(); actual=hashlib.sha256(raw).hexdigest();require(actual==a.manifest_sha256,"manifest SHA mismatch")
  def jsonl(path): return [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]
  out=analyze(m,actual,json.loads(a.receipts.read_text(encoding="utf-8")),jsonl(a.matches));a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(out,indent=2)+"\n",encoding="utf-8");print(f"ANALYZED games={len(out['games'])} unplanned_receipts={len(out['unplanned_receipt_cells'])}");return 2 if out["unplanned_receipt_cells"] or out["unplanned_game_uids"] or any(g["verdict"]=="INVALID_UNKNOWN" for g in out["games"]) else 0
 except (OSError,ValueError,KeyError,TypeError) as e: print(f"error: {e}",file=sys.stderr);return 2
if __name__=="__main__": raise SystemExit(main())
