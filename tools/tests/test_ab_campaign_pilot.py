import hashlib, pathlib, sys, tempfile, unittest
ROOT=pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/"tools"/"ai"))
import ab_campaign_pilot as pilot
import apply_increment_switches as increment

def manifest():
 baseline=ROOT/"tools/tests/fixtures/ab_campaign_baseline.json"
 _,groups,_=increment.load_spec(ROOT/"tools/ai/increment_switches.yaml")
 return {"schema":1,"switch":pilot.SWITCH,"execution_approved":False,"pairs_per_setup":2,"team_size_scope":[1,2,3,4],"arm_bot_types":{"control":"hard_control","treatment":"hard_treatment"},"baseline_manifest_path":"tools/tests/fixtures/ab_campaign_baseline.json","baseline_manifest_sha256":pilot.sha(baseline),"pins":{"source_commit":"964cdb630b1514e1c1a0baed55cbdbc427d5fc11","engine_version":"6da7fce14da541180c6baddd6925118fbef65b94"},"switch_patch_sha256":increment.group_patch_sha256(groups[pilot.SWITCH]),"baseline_ai_payload_sha256":pilot.ai_payload_sha(ROOT),"treatment_ai_payload_sha256":pilot.ai_payload_sha(ROOT,pilot.SWITCH),"tool_sha256":{"driver_analyzer":pilot.sha(ROOT/"tools/ai/ab_campaign_pilot.py"),"campaign_runner":pilot.sha(ROOT/"tools/ai/campaign_runner.py"),"batch_runner":pilot.sha(ROOT/"tools/ai/run_ai_match_batch.py"),"switch_applier":pilot.sha(ROOT/"tools/ai/apply_increment_switches.py")},"receipt_schema_sha256":pilot.sha(ROOT/"tools/ai/ab_campaign_receipt.schema.json"),"binary_sha256":{"bin/OpenRA.exe":"5693f355792ebb68699793f96ef2167b733c5e7756f580858c5090226418c984","bin/OpenRA.dll":"92ee9dacd08869593dc5ad70c2d1ce48f701caf8277ea3bd692acf2224a8ca77","bin/OpenRA.Mods.Cameo.dll":"41f37982c146f73b4a9c07071f34b47e214178cb9413e514f05af9af2d44b3a4"},"round_stages":pilot.ROUND_STAGES,"acceptance_thresholds":pilot.DEFAULT_THRESHOLDS,"slot_policy":dict(pilot.SLOT_POLICY),"seeds":[1337,7331],"maps":[{"name":k,"path":v[0],"sha256":v[1],"required_seats":v[2]} for k,v in pilot.MAPS.items()],"setups":[{"id":n,"team_size":s,"map":m,"team_factions":list(f)} for n,s,m,f in pilot.SETUPS]}

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
  self.assertEqual(11.07,result["slot_wall_estimate_hours_at_8_5m_each"])
  self.assertEqual([7.33,14.8],result["slot_wall_estimate_range_hours_5_to_12m_each"])

 def test_generated_map_writer_proves_mixed_faction_4v4_seats_without_launch(self):
  m=manifest();job=next(j for j in pilot.make_jobs(m) if j["team_size"]==4 and j["game_in_pair"]==1)
  with tempfile.TemporaryDirectory() as temp:
   dest=pathlib.Path(temp)/"variant";proof=pilot.prove_generated_map(job,ROOT,dest)
   monitor_text=(dest/"ab_campaign_monitor.lua").read_text(encoding="utf-8")
  self.assertEqual("generated_map.yaml",proof["seat_proof_source"])
  self.assertEqual(8,len(proof["seats"]))
  self.assertEqual({"td_gdi","td_nod","ra1_allies","ra1_soviets"},{x["faction"] for x in proof["seats"]})
  self.assertTrue(all(x["home_location"] for x in proof["seats"]))
  self.assertEqual(45000,proof["runtime_monitor"]["cap_tick"])
  self.assertEqual("AB_CAMPAIGN_ACTOR_SAMPLE",proof["runtime_monitor"]["actor_count_marker"])
  self.assertIn('"Neutral", "Creeps"',monitor_text)
  self.assertIn('AB_CAMPAIGN_CAP tick=',monitor_text)

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

 def test_arm_materialization_isolated_and_matches_pinned_payload(self):
  with tempfile.TemporaryDirectory() as temp:
   base=pathlib.Path(temp)
   control=pilot.materialize_ai_arm(ROOT,base/"control")
   treatment=pilot.materialize_ai_arm(ROOT,base/"treatment",pilot.SWITCH)
   self.assertEqual(pilot.ai_payload_sha(ROOT),control["payload_sha256"])
   self.assertEqual(pilot.ai_payload_sha(ROOT,pilot.SWITCH),treatment["payload_sha256"])
   self.assertGreater(treatment["changed_fields"],0)
   self.assertTrue(treatment["changed_files"])
   self.assertFalse(treatment["runnable"])
   self.assertEqual(["ai"], [p.name for p in (base/"treatment"/"mods"/"cameo").iterdir()])
   with self.assertRaisesRegex(ValueError,"destination must be new"):
    pilot.materialize_ai_arm(ROOT,base/"treatment",pilot.SWITCH)

 def test_dual_arm_ai_aliases_isolate_selected_switch(self):
  with tempfile.TemporaryDirectory() as temp:
   result=pilot.materialize_dual_arm_ai(ROOT,pathlib.Path(temp)/"combined",pilot.SWITCH)
   text=(pathlib.Path(temp)/"combined"/"mods"/"cameo"/"ai"/"ai.yaml").read_text(encoding="utf-8")
   self.assertEqual({"control":"hard_control","treatment":"hard_treatment"},result["arm_types"])
   self.assertIn("Bots: easiest, veryeasy, easy, medium, hard, veryhard, brutal, challenger, unbeatable, cameogod, hard_control, hard_treatment",text)
   self.assertIn("Condition: campaign_treatment\n\t\tBots: hard_treatment",text)
   self.assertIn("HarvesterBotModuleCA@generic:\n\t\tRequiresCondition: genericbot && !campaign_treatment",text)
   self.assertIn("HarvesterBotModuleCA@campaign_treatment:\n\t\tUseHarvesterLogistics: true\n\t\tRequiresCondition: campaign_treatment",text)
   self.assertIn("UseHarvesterLogistics: true",text)

 def test_missing_map_is_rejected(self):
  m=manifest();m["maps"][0]["path"]="mods/cameo/maps/missing.oramap"
  with self.assertRaisesRegex(ValueError,"map path/SHA"):
   pilot.validate(m,ROOT)

 def test_adjudicator_keeps_cap_unknown_and_sums_team_economy(self):
  m=manifest(); job=next(j for j in pilot.make_jobs(m) if j["team_size"]==2 and j["game_in_pair"]==0)
  seats=[{**s,"home_location":f"{20+s['spawn']},16"} for s in job["seat_assignments"]]; rows=[]
  for seat in seats:
   outcome="won" if seat["side"]==job["treatment_side"] else "lost"
   rows.append({"player":{"home":seat["home_location"],"bot_type":seat["bot_type"],"faction":seat["faction"],"outcome":outcome},"stats_timeline":[{"tick":100,"earned":12,"spent":5,"banked":7}]})
  receipt={"cell_id":job["cell_id"],"manifest_sha256":"a"*64,"switch":job["switch"],"setup":job["setup"],"map":job["map"],"map_sha256":job["map_sha256"],"seed":job["seed"],"pair":job["pair"],"game_in_pair":job["game_in_pair"],"control_side":job["control_side"],"treatment_side":job["treatment_side"],"arm_bot_types":job["arm_bot_types"],"status":"NATURAL_END","end_reason":"natural","seat_proof_source":"generated_map.yaml","map_yaml_sha256":"b"*64,"seats":seats}
  got=pilot.adjudicate(rows,receipt,job,"a"*64)
  self.assertEqual("TREATMENT_WIN",got["verdict"]);self.assertEqual(24,got["team_totals"]["control"]["earned"])
  bad_rows=[dict(r,player={**r["player"],"home":"999,999"}) for r in rows]
  self.assertEqual("INVALID_UNKNOWN",pilot.adjudicate(bad_rows,receipt,job,"a"*64)["verdict"])
  receipt.update({"status":"CENSORED_CAP","cap_marker":True,"cap_tick":45001,"support_complete":True})
  censored=pilot.adjudicate(rows,receipt,job,"a"*64)
  self.assertEqual("CENSORED_CAP",censored["verdict"],censored);self.assertEqual(24,censored["team_totals"]["treatment"]["earned"])
  receipt["cap_tick"]=15000
  self.assertEqual("INVALID_UNKNOWN",pilot.adjudicate(rows,receipt,job,"a"*64)["verdict"])
  receipt["cap_tick"]=45001;receipt["seats"][0]["spawn"]+=1
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
   engine_digest=hashlib.sha256("".join(m["binary_sha256"][key] for key in sorted(m["binary_sha256"])).encode()).hexdigest()
   resolved=[]
   for seat in job["seat_assignments"]:
    won=(seat["side"]==job["treatment_side"])==treatment_wins
    resolved.append({"seat_id":seat["home"],"team":seat["side"],"arm":seat["arm"],"bot_type":seat["bot_type"],"faction":seat["faction"],"home":seat["home"],"home_location":f"{20+seat['spawn']},16","spawn":seat["spawn"],"proof_source":"generated map.yaml + cameo-ai-matches.jsonl","outcome":"won" if won else "lost","metrics":{"earned":None,"spent":None,"banked":None,"army_value":None,"kills":None,"deaths":None,"timeline":[]}})
   receipts.append({"schema_version":1,"campaign_id":"test","manifest_sha256":"a"*64,"baseline_commit":m["pins"]["source_commit"],"engine_sha256":engine_digest,"switch":job["switch"],"setup_id":job["setup"],"cell_id":job["cell_id"],"game_uid":uid,"map":{"name":job["map"],"path":job["map_path"],"sha256":job["map_sha256"],"generated_map_yaml_sha256":"b"*64,"required_seats":job["required_seats"]},"pair_id":f"{job['setup']}:{job['seed']}:{job['pair']}","pair_member":job["game_in_pair"],"seed":job["seed"],"seed_proof":{"env_value":str(job["seed"]),"server_log_pin_line":f"CAMEO DEV SEED pinned RandomSeed={job['seed']}","server_log_sha256":"c"*64},"teams":{"A":{"arm":"control" if job["control_side"]=="A" else "treatment","bot_type":job["arm_bot_types"]["control" if job["control_side"]=="A" else "treatment"],"seat_ids":[s["home"] for s in resolved if s["team"]=="A"]},"B":{"arm":"control" if job["control_side"]=="B" else "treatment","bot_type":job["arm_bot_types"]["control" if job["control_side"]=="B" else "treatment"],"seat_ids":[s["home"] for s in resolved if s["team"]=="B"]}},"resolved_seats":resolved,"end_class":"NATURAL","end_reason":"natural","winner_team":job["treatment_side"] if treatment_wins else job["control_side"],"world_tick":45000,"cap":{"world_tick_cap":45000,"marker_observed":False,"marker_tick":None},"memory":{"process_id":1,"peak_private_bytes":0,"limit_private_bytes":pilot.PROCESS_CAP_BYTES,"samples":[]},"runtime":{"started_utc":None,"finished_utc":None,"exit_code":0,"wall_seconds":1,"stall_seconds":180,"pid_scoped_cleanup":True},"artifacts":{key:{"path":None,"sha256":"b"*64 if key=="map_yaml" else None,"missing_reason":"fixture"} for key in ("server_log","driver_log","matches_jsonl","replay","map_yaml","support_bundle")}})
   for seat in job["seat_assignments"]:
    won=(seat["side"]==job["treatment_side"])==treatment_wins
    rows.append({"game_uid":uid,"player":{"home":f"{20+seat['spawn']},16","bot_type":seat["bot_type"],"faction":seat["faction"],"outcome":"won" if won else "lost"},"stats":{"stats_timeline_fields":"tick,earned,spent,banked","stats_timeline":[[5000,10,4,6],[45000,90,40,50]]},"campaign_events":[{"kind":"refinery_placed","tick":7000}]})
  health={r["game_uid"]:{"verdict":"PASS","sha256":"d"*64} for r in receipts}
  result=pilot.analyze(m,"a"*64,receipts,rows,health)
  setup=next(x for x in result["setups"] if x["setup"]==jobs[0]["setup"])
  self.assertEqual(2,setup["complete_natural_pairs"],result["games"])
  self.assertEqual([1.0,-1.0],setup["paired_win_differences"])
  self.assertEqual(0.0,setup["paired_win_effect"])
  self.assertEqual(4,setup["fixed_interval_team_economy"]["treatment"]["5000"]["seat_samples"])
  self.assertEqual(4,setup["campaign_event_metrics"]["treatment"]["counts"]["refinery_placed"])

 def readiness_case(self):
  m=manifest(); jobs=[j for j in pilot.make_jobs(m) if j["pair"]==0]
  receipts=[{"cell_id":j["cell_id"],"game_uid":"uid:"+j["cell_id"],"end_class":"NATURAL"} for j in jobs]
  rows=[{"game_uid":r["game_uid"]} for r in receipts]
  health={r["game_uid"]:{"verdict":"PASS","sha256":"e"*64} for r in receipts}
  negative={"fixture_sha256":"f"*64,"expected_verdict":"INVALID_UNKNOWN","observed_verdict":"INVALID_UNKNOWN"}
  return m,jobs,receipts,rows,health,negative

 def readiness(self, case, **changes):
  m,jobs,receipts,rows,health,negative=case
  args={"checkpoint_stage":16,"producer_slot_id":"01a10d0f-1530-74b1-b45a-d64c9b3ac15a",
        "negative_control":negative,"pins_match":True,"evidence_integrity":"PASS",
        "measured_utc":"2026-10-29T12:00:00Z","run_kind":"pilot"}
  args.update(changes)
  return pilot.build_analyzer_readiness(m,"a"*64,receipts,rows,health,**args)

 def test_readiness_passes_exact_stage_and_scores_only_healthy_natural_pairs(self):
  case=self.readiness_case(); result=self.readiness(case)
  self.assertEqual("PASS_REVIEW_ONLY",result["readiness"])
  self.assertFalse(result["execution_authorized"])
  self.assertEqual(16,len(result["side_swap_pairs"]))
  self.assertEqual(16,result["scoring_round_count"])
  self.assertEqual(0,result["non_scoring_round_count"])
  self.assertTrue(all(len(p["cell_ids"])==2 and p["scores"] for p in result["side_swap_pairs"]))
  self.assertIn("measured_utc",result);self.assertIn("producer_slot_id",result)

 def test_pilot_stage_32_is_valid_and_no_deploy_evidence_holds(self):
  m=manifest(); jobs=pilot.make_jobs(m)
  receipts=[{"cell_id":j["cell_id"],"game_uid":"uid:"+j["cell_id"],"end_class":"NATURAL"} for j in jobs]
  rows=[{"game_uid":r["game_uid"]} for r in receipts]
  health={r["game_uid"]:{"verdict":"PASS","sha256":"e"*64} for r in receipts}
  case=(m,jobs,receipts,rows,health,{"fixture_sha256":"f"*64,"expected_verdict":"INVALID_UNKNOWN","observed_verdict":"INVALID_UNKNOWN"})
  result=self.readiness(case,checkpoint_stage=32)
  self.assertEqual("PASS_REVIEW_ONLY",result["readiness"])
  self.assertEqual(32,result["scoring_round_count"])
  receipts[0]["end_class"]="NO_DEPLOY"
  result=self.readiness(case,checkpoint_stage=32)
  self.assertEqual("HOLD",result["readiness"])
  self.assertEqual(1,result["invalid_no_deploy_count"])

 def test_readiness_holds_for_each_closed_world_integrity_failure(self):
  cases=[]
  case=self.readiness_case(); case[3].append([]); cases.append(("rejected_rows",case,{}))
  case=self.readiness_case(); case[2].append(dict(case[2][0])); cases.append(("duplicate_cell_ids",case,{}))
  case=self.readiness_case(); case[2].append({"cell_id":"unplanned","game_uid":"foreign","end_class":"NATURAL"}); cases.append(("unexpected_cell_ids",case,{}))
  case=self.readiness_case(); case[2].pop(); cases.append(("missing_cell_ids",case,{}))
  case=self.readiness_case(); case[3].append({"game_uid":"unmatched"}); cases.append(("unmatched_uids",case,{}))
  case=self.readiness_case(); case[2][1]["game_uid"]=case[2][0]["game_uid"]; case[3][1]["game_uid"]=case[3][0]["game_uid"]; cases.append(("duplicate_receipt_uid",case,{}))
  case=self.readiness_case(); cases.append(("pins_match",case,{"pins_match":False}))
  case=self.readiness_case(); case[5]["observed_verdict"]="PASS"; cases.append(("negative_control",case,{}))
  case=self.readiness_case(); cases.append(("evidence_integrity",case,{"evidence_integrity":"HOLD"}))
  case=self.readiness_case(); case[4].pop(next(iter(case[4]))); cases.append(("replay_health",case,{}))
  for field,case,changes in cases:
   with self.subTest(field=field):
    self.assertEqual("HOLD",self.readiness(case,**changes)["readiness"])

 def test_readiness_counts_incomplete_or_censored_rounds_without_scoring(self):
  case=self.readiness_case(); case[2][0]["end_class"]="CAP"
  result=self.readiness(case)
  self.assertEqual(15,result["scoring_round_count"])
  self.assertEqual(1,result["non_scoring_round_count"])
  self.assertTrue(all(p["counted"] for p in result["side_swap_pairs"]))

 def test_readiness_rejects_failing_health_and_bad_stage(self):
  case=self.readiness_case(); next(iter(case[4].values()))["verdict"]="UNKNOWN"
  result=self.readiness(case,checkpoint_stage=16)
  self.assertEqual("HOLD",result["readiness"])
  self.assertEqual(15,result["scoring_round_count"])
  result=self.readiness(self.readiness_case(),checkpoint_stage=48)
  self.assertEqual("HOLD",result["readiness"])

 def test_run_kind_controls_checkpoint_stages_and_schedule_coverage(self):
  self.assertEqual("PASS_REVIEW_ONLY",self.readiness(self.readiness_case(),run_kind="campaign")["readiness"])
  result=self.readiness(self.readiness_case(),run_kind="campaign",checkpoint_stage=48)
  self.assertEqual("HOLD",result["readiness"])
  self.assertTrue(any("schedule does not cover" in reason for reason in result["blocking_reasons"]))
  self.assertEqual("HOLD",self.readiness(self.readiness_case(),run_kind="other")["readiness"])

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
  c=pilot.SlotCounter(); floor=pilot.RAM_FLOOR_BYTES
  self.assertTrue(c.acquire("a",1,floor));self.assertFalse(c.acquire("b",2,floor));self.assertFalse(c.acquire("c",1,floor));self.assertFalse(c.acquire("d",2,floor))
  self.assertFalse(c.acquire("large",4,floor));c.release("a")
  self.assertTrue(c.acquire("large",4,floor));self.assertFalse(c.acquire("small",1,floor));
  with self.assertRaises(ValueError): c.update_private_bytes("large",pilot.PROCESS_CAP_BYTES)
  self.assertFalse(pilot.slots_available(1,floor-1))
  with tempfile.TemporaryDirectory() as temp:
   db1=pilot.SqliteSlotCounter(pathlib.Path(temp)/"slots.sqlite")
   db2=pilot.SqliteSlotCounter(pathlib.Path(temp)/"slots.sqlite")
   self.assertTrue(db1.acquire("x",1,floor));self.assertFalse(db2.acquire("y",2,floor))
   self.assertFalse(db1.acquire("z",1,floor));self.assertFalse(db2.acquire("fourth",1,floor))
   self.assertFalse(db2.acquire("exclusive",4,floor));db2.heartbeat("x",1024);db1.release("x")
   self.assertTrue(db2.acquire("exclusive",4,floor));self.assertFalse(db1.acquire("small",1,floor))
   with self.assertRaisesRegex(ValueError,"memory cap"): db2.heartbeat("exclusive",pilot.PROCESS_CAP_BYTES)
   db2.release("exclusive");db2.release("y");db1.release("z")

if __name__=="__main__": unittest.main()
