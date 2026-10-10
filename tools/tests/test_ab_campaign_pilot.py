import pathlib, sys, tempfile, unittest
ROOT=pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/"tools"/"ai"))
import ab_campaign_pilot as pilot
import apply_increment_switches as increment

def manifest():
 baseline=ROOT/"tools/tests/fixtures/ab_campaign_baseline.json"
 _,groups,_=increment.load_spec(ROOT/"tools/ai/increment_switches.yaml")
 return {"schema":1,"switch":pilot.SWITCH,"execution_approved":False,"pairs_per_setup":2,"team_size_scope":[1,2,3,4],"arm_bot_types":{"control":"hard_control","treatment":"hard_treatment"},"baseline_manifest_path":"tools/tests/fixtures/ab_campaign_baseline.json","baseline_manifest_sha256":pilot.sha(baseline),"pins":{"source_commit":"700bb16483f6d92664153e98d6f2560c312acaab","engine_version":"6da7fce14da541180c6baddd6925118fbef65b94"},"switch_patch_sha256":increment.group_patch_sha256(groups[pilot.SWITCH]),"baseline_ai_payload_sha256":pilot.ai_payload_sha(ROOT),"treatment_ai_payload_sha256":pilot.ai_payload_sha(ROOT,pilot.SWITCH),"tool_sha256":{"driver_analyzer":pilot.sha(ROOT/"tools/ai/ab_campaign_pilot.py"),"batch_runner":pilot.sha(ROOT/"tools/ai/run_ai_match_batch.py"),"switch_applier":pilot.sha(ROOT/"tools/ai/apply_increment_switches.py")},"binary_sha256":{"bin/OpenRA.exe":"5693f355792ebb68699793f96ef2167b733c5e7756f580858c5090226418c984","bin/OpenRA.dll":"92ee9dacd08869593dc5ad70c2d1ce48f701caf8277ea3bd692acf2224a8ca77","bin/OpenRA.Mods.Cameo.dll":"41f37982c146f73b4a9c07071f34b47e214178cb9413e514f05af9af2d44b3a4"},"round_stages":pilot.ROUND_STAGES,"acceptance_thresholds":pilot.DEFAULT_THRESHOLDS,"slot_policy":dict(pilot.SLOT_POLICY),"seeds":[1337,7331],"maps":[{"name":k,"path":v[0],"sha256":v[1],"required_seats":v[2]} for k,v in pilot.MAPS.items()],"setups":[{"id":n,"team_size":s,"map":m,"team_factions":list(f)} for n,s,m,f in pilot.SETUPS]}

class PilotPlanTests(unittest.TestCase):
 def test_approved_amendment_expands_to_64_side_swapped_games(self):
  m=manifest();self.assertTrue(pilot.validate(m,ROOT)); jobs=pilot.make_jobs(m)
  self.assertEqual(16,len(pilot.SETUPS));self.assertEqual(64,len(jobs))
  self.assertEqual(32,len({j["cell_id"].rsplit("|",1)[0] for j in jobs}))
  for i in range(0,len(jobs),2): pass
  for setup, *_ in pilot.SETUPS:
   pair=[j for j in jobs if j["setup"]==setup and j["seed"]==1337]
   self.assertEqual({0,1},{j["game_in_pair"] for j in pair})
   self.assertEqual({("A","B"), ("B","A")},{(j["control_side"],j["treatment_side"]) for j in pair})
   self.assertNotEqual([x["spawn"] for x in pair[0]["seat_assignments"]],[x["spawn"] for x in pair[1]["seat_assignments"]])
  result=pilot.dry_run(m,ROOT)
  self.assertEqual(0,result["launches"]);self.assertFalse(result["execution_authorized"])
  self.assertEqual(11.07,result["worker_hours_estimate_at_8_5m_each"])
  self.assertEqual(7.29,result["parallel_slot_wall_estimate_hours_at_8_5m_each"])
  self.assertEqual([5.11,9.47],result["parallel_slot_wall_estimate_range_hours_5_to_12m_each"])

 def test_map_tamper_and_execution_authority_fail_closed(self):
  m=manifest();m["maps"][0]["sha256"]="0"*64
  with self.assertRaisesRegex(ValueError,"map path/SHA"):
   pilot.validate(m,ROOT)
  m=manifest();m["execution_approved"]=True
  with self.assertRaisesRegex(ValueError,"must not authorize"):
   pilot.validate(m,ROOT)
  m=manifest();m["slot_policy"]["max_parallel_small_games"]=4
  with self.assertRaisesRegex(ValueError,"slot/memory policy"):
   pilot.validate(m,ROOT)

 def test_missing_map_is_rejected(self):
  m=manifest();m["maps"][0]["path"]="mods/cameo/maps/missing.oramap"
  with self.assertRaisesRegex(ValueError,"map path/SHA"):
   pilot.validate(m,ROOT)

 def test_adjudicator_keeps_cap_unknown_and_sums_team_economy(self):
  m=manifest(); job=next(j for j in pilot.make_jobs(m) if j["team_size"]==2 and j["game_in_pair"]==0)
  seats=[dict(s) for s in job["seat_assignments"]]; rows=[]
  for seat in seats:
   outcome="won" if seat["side"]==job["treatment_side"] else "lost"
   rows.append({"player":{"home":seat["home"],"bot_type":seat["bot_type"],"faction":seat["faction"],"outcome":outcome},"stats_timeline":[{"tick":100,"earned":12,"spent":5,"banked":7}]})
  receipt={"cell_id":job["cell_id"],"manifest_sha256":"a"*64,"switch":job["switch"],"setup":job["setup"],"map":job["map"],"map_sha256":job["map_sha256"],"seed":job["seed"],"pair":job["pair"],"game_in_pair":job["game_in_pair"],"control_side":job["control_side"],"treatment_side":job["treatment_side"],"arm_bot_types":job["arm_bot_types"],"status":"NATURAL_END","end_reason":"natural","seat_proof_source":"generated_map.yaml","map_yaml_sha256":"b"*64,"seats":seats}
  got=pilot.adjudicate(rows,receipt,job,"a"*64)
  self.assertEqual("TREATMENT_WIN",got["verdict"]);self.assertEqual(24,got["team_totals"]["control"]["earned"])
  receipt.update({"status":"CENSORED_CAP","cap_marker":True,"cap_tick":45000,"support_complete":True})
  censored=pilot.adjudicate(rows,receipt,job,"a"*64)
  self.assertEqual("CENSORED_CAP",censored["verdict"]);self.assertEqual(24,censored["team_totals"]["treatment"]["earned"])
  receipt["cap_tick"]=15000
  self.assertEqual("INVALID_UNKNOWN",pilot.adjudicate(rows,receipt,job,"a"*64)["verdict"])
  receipt["cap_tick"]=45000;receipt["seats"][0]["spawn"]+=1
  self.assertEqual("INVALID_UNKNOWN",pilot.adjudicate(rows,receipt,job,"a"*64)["verdict"])

 def test_analyzer_preserves_all_unrun_planned_games(self):
  result=pilot.analyze(manifest(),"a"*64,[],[])
  self.assertEqual(64,len(result["games"]))
  self.assertEqual(16,len(result["setups"]))
  self.assertTrue(all(x["verdict"]=="INCOMPLETE_UNKNOWN" for x in result["games"]))
  self.assertTrue(all(x["paired_win_effect"] is None and x["paired_bootstrap_95ci"] is None for x in result["setups"]))


 def test_analyzer_pairs_only_natural_games_and_reports_interval_metrics(self):
  m=manifest(); jobs=[j for j in pilot.make_jobs(m) if j["setup"]==pilot.SETUPS[0][0]]
  receipts=[]; rows=[]
  for job in jobs:
   uid="g-"+job["cell_id"]
   treatment_wins=(job["pair"]==0)
   receipts.append({"cell_id":job["cell_id"],"game_uid":uid,"manifest_sha256":"a"*64,"switch":job["switch"],"setup":job["setup"],"map":job["map"],"map_sha256":job["map_sha256"],"seed":job["seed"],"pair":job["pair"],"game_in_pair":job["game_in_pair"],"control_side":job["control_side"],"treatment_side":job["treatment_side"],"arm_bot_types":job["arm_bot_types"],"status":"NATURAL_END","end_reason":"natural","seat_proof_source":"generated_map.yaml","map_yaml_sha256":"b"*64,"seats":job["seat_assignments"]})
   for seat in job["seat_assignments"]:
    won=(seat["side"]==job["treatment_side"])==treatment_wins
    rows.append({"game_uid":uid,"player":{"home":seat["home"],"bot_type":seat["bot_type"],"faction":seat["faction"],"outcome":"won" if won else "lost"},"stats":{"stats_timeline_fields":"tick,earned,spent,banked","stats_timeline":[[5000,10,4,6],[45000,90,40,50]]},"campaign_events":[{"kind":"refinery_placed","tick":7000}]})
  result=pilot.analyze(m,"a"*64,receipts,rows)
  setup=next(x for x in result["setups"] if x["setup"]==jobs[0]["setup"])
  self.assertEqual(2,setup["complete_natural_pairs"])
  self.assertEqual([1.0,-1.0],setup["paired_win_differences"])
  self.assertEqual(0.0,setup["paired_win_effect"])
  self.assertEqual(4,setup["fixed_interval_team_economy"]["treatment"]["5000"]["seat_samples"])
  self.assertEqual(4,setup["campaign_event_metrics"]["treatment"]["counts"]["refinery_placed"])

 def test_ratchet_acceptance_regression_and_slot_rules(self):
  self.assertEqual({"control_groups":["BU_harvester_logistics"],"treatment_groups":["BU_harvester_logistics","U_ut4_expansion_appetite"]},pilot.treatment_baseline(["BU_harvester_logistics"],"U_ut4_expansion_appetite"))
  self.assertEqual("ACCEPT",pilot.acceptance_decision(rounds=16,treatment_wins=12,control_wins=4,economy_not_worse=True,economy_better=False,army_not_worse=True))
  self.assertEqual("CONTINUE",pilot.acceptance_decision(rounds=32,treatment_wins=20,control_wins=12,economy_not_worse=True,economy_better=False,army_not_worse=True))
  self.assertEqual("PARK",pilot.acceptance_decision(rounds=64,treatment_wins=32,control_wins=32,economy_not_worse=True,economy_better=False,army_not_worse=True))
  self.assertEqual("ACCEPT",pilot.acceptance_decision(rounds=64,treatment_wins=32,control_wins=32,economy_not_worse=True,economy_better=True,army_not_worse=True,earned_spent_delta=.01,army_efficiency_delta=.02,kill_exchange_delta=.01))
  self.assertEqual("CONTINUE",pilot.acceptance_decision(rounds=16,treatment_wins=10,control_wins=6,economy_not_worse=False,economy_better=False,army_not_worse=False))
  with self.assertRaisesRegex(ValueError,"fix/safety"):
   pilot.treatment_baseline([],"AG_assault_fanout")
  self.assertTrue(pilot.regression_due(5));self.assertFalse(pilot.regression_due(4))
  c=pilot.SlotCounter(); floor=6*1024**3
  self.assertTrue(c.acquire("a",1,floor));self.assertTrue(c.acquire("b",2,floor));self.assertTrue(c.acquire("c",1,floor));self.assertFalse(c.acquire("d",2,floor))
  self.assertFalse(c.acquire("large",4,floor));c.release("a");c.release("b");c.release("c")
  self.assertTrue(c.acquire("large",4,floor));self.assertFalse(c.acquire("small",1,floor));
  with self.assertRaises(ValueError): c.update_private_bytes("large",8*1024**3)
  self.assertFalse(pilot.slots_available(1,floor-1))
  with tempfile.TemporaryDirectory() as temp:
   db1=pilot.SqliteSlotCounter(pathlib.Path(temp)/"slots.sqlite")
   db2=pilot.SqliteSlotCounter(pathlib.Path(temp)/"slots.sqlite")
   self.assertTrue(db1.acquire("x",1,floor));self.assertTrue(db2.acquire("y",2,floor))
   self.assertTrue(db1.acquire("z",1,floor));self.assertFalse(db2.acquire("fourth",1,floor))
   self.assertFalse(db2.acquire("exclusive",4,floor));db2.heartbeat("x",1024);db1.release("x");db2.release("y");db1.release("z")
   self.assertTrue(db2.acquire("exclusive",4,floor));self.assertFalse(db1.acquire("small",1,floor))
   with self.assertRaisesRegex(ValueError,"memory cap"): db2.heartbeat("exclusive",8*1024**3)
   db2.release("exclusive");db2.release("y");db1.release("z")

 def test_switch_order_covers_the_frozen_sixty_group_catalog(self):
  import apply_increment_switches as switches
  _,groups,_=switches.load_spec(ROOT/"tools"/"ai"/"increment_switches.yaml")
  doc=(ROOT/"SWITCH_ORDER_2026-10-11.md").read_text(encoding="utf-8")
  for name in groups: self.assertIn(f"`{name}`",doc)
  self.assertEqual(60,len(groups))

if __name__=="__main__": unittest.main()
