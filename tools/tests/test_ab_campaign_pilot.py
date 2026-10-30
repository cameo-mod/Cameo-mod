import contextlib, copy, hashlib, io, json, pathlib, re, sys, tempfile, unittest
ROOT=pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/"tools"/"ai"))
import ab_campaign_pilot as pilot
import apply_increment_switches as increment

def manifest():
 baseline=ROOT/"tools/tests/fixtures/ab_campaign_baseline.json"
 _,groups,_=increment.load_spec(ROOT/"tools/ai/increment_switches.yaml")
 return {"schema":1,"switch":pilot.SWITCH,"execution_approved":False,"pairs_per_setup":2,"team_size_scope":[1,2,3,4],"arm_bot_types":{"control":"hard_control","treatment":"hard_treatment"},"baseline_manifest_path":"tools/tests/fixtures/ab_campaign_baseline.json","baseline_manifest_sha256":pilot.sha(baseline),"pins":{"source_commit":"964cdb630b1514e1c1a0baed55cbdbc427d5fc11","engine_version":"6da7fce14da541180c6baddd6925118fbef65b94"},"switch_patch_sha256":increment.group_patch_sha256(groups[pilot.SWITCH]),"baseline_ai_payload_sha256":pilot.ai_payload_sha(ROOT),"treatment_ai_payload_sha256":pilot.ai_payload_sha(ROOT,pilot.SWITCH),"tool_sha256":{"driver_analyzer":pilot.sha(ROOT/"tools/ai/ab_campaign_pilot.py"),"campaign_runner":pilot.sha(ROOT/"tools/ai/campaign_runner.py"),"batch_runner":pilot.sha(ROOT/"tools/ai/run_ai_match_batch.py"),"switch_applier":pilot.sha(ROOT/"tools/ai/apply_increment_switches.py")},"receipt_schema_sha256":pilot.sha(ROOT/"tools/ai/ab_campaign_receipt.schema.json"),"binary_sha256":{"bin/OpenRA.exe":"5693f355792ebb68699793f96ef2167b733c5e7756f580858c5090226418c984","bin/OpenRA.dll":"92ee9dacd08869593dc5ad70c2d1ce48f701caf8277ea3bd692acf2224a8ca77","bin/OpenRA.Mods.Cameo.dll":"41f37982c146f73b4a9c07071f34b47e214178cb9413e514f05af9af2d44b3a4"},"round_stages":pilot.ROUND_STAGES,"acceptance_thresholds":pilot.DEFAULT_THRESHOLDS,"slot_policy":dict(pilot.SLOT_POLICY),"seeds":[1337,7331],"maps":[{"name":k,"path":v[0],"sha256":v[1],"required_seats":v[2]} for k,v in pilot.MAPS.items()],"setups":[{"id":n,"team_size":s,"map":m,"team_factions":list(f)} for n,s,m,f in pilot.SETUPS]}

def sha_of(raw): return hashlib.sha256(raw).hexdigest()

PIN_FILES={"analyzer_sha256":"tools/ai/ab_campaign_pilot.py","campaign_runner_sha256":"tools/ai/campaign_runner.py","batch_runner_sha256":"tools/ai/run_ai_match_batch.py",
           "switch_applier_sha256":"tools/ai/apply_increment_switches.py","receipt_schema_sha256":"tools/ai/ab_campaign_receipt.schema.json"}

def exact_pins(manifest_raw):
 """Evidence pins computed here with hashlib, independently of the analyzer's own measurement code."""
 return {"manifest_sha256":sha_of(manifest_raw),**{key:sha_of((ROOT/rel).read_bytes()) for key,rel in PIN_FILES.items()}}

def synthetic_game(m,job,manifest_sha,uid,treatment_wins=True,end_class="NATURAL"):
 """One schema-1 receipt and its match rows, built so that they adjudicate (natural win or verified cap)."""
 engine_digest=hashlib.sha256("".join(m["binary_sha256"][key] for key in sorted(m["binary_sha256"])).encode()).hexdigest()
 cap=end_class=="CAP"; resolved=[]; rows=[]
 for seat in job["seat_assignments"]:
  won=(seat["side"]==job["treatment_side"])==treatment_wins
  outcome="unknown" if cap else ("won" if won else "lost")
  resolved.append({"seat_id":seat["home"],"team":seat["side"],"arm":seat["arm"],"bot_type":seat["bot_type"],"faction":seat["faction"],"home":seat["home"],"home_location":f"{20+seat['spawn']},16","spawn":seat["spawn"],"proof_source":"generated map.yaml + cameo-ai-matches.jsonl","outcome":outcome,"metrics":{"earned":None,"spent":None,"banked":None,"army_value":None,"kills":None,"deaths":None,"timeline":[]}})
  rows.append({"game_uid":uid,"player":{"home":f"{20+seat['spawn']},16","bot_type":seat["bot_type"],"faction":seat["faction"],"outcome":outcome},"stats":{"stats_timeline_fields":"tick,earned,spent,banked","stats_timeline":[[5000,10,4,6],[45000,90,40,50]]},"campaign_events":[{"kind":"refinery_placed","tick":7000}]})
 arm=lambda side:"control" if job["control_side"]==side else "treatment"
 team=lambda side:{"arm":arm(side),"bot_type":job["arm_bot_types"][arm(side)],"seat_ids":[s["home"] for s in resolved if s["team"]==side]}
 receipt={"schema_version":1,"campaign_id":"test","manifest_sha256":manifest_sha,"baseline_commit":m["pins"]["source_commit"],"engine_sha256":engine_digest,"switch":job["switch"],"setup_id":job["setup"],"cell_id":job["cell_id"],"game_uid":uid,"map":{"name":job["map"],"path":job["map_path"],"sha256":job["map_sha256"],"generated_map_yaml_sha256":"b"*64,"required_seats":job["required_seats"]},"pair_id":f"{job['setup']}:{job['seed']}:{job['pair']}","pair_member":job["game_in_pair"],"seed":job["seed"],"seed_proof":{"env_value":str(job["seed"]),"server_log_pin_line":f"CAMEO DEV SEED pinned RandomSeed={job['seed']}","server_log_sha256":"c"*64},"teams":{"A":team("A"),"B":team("B")},"resolved_seats":resolved,"end_class":end_class,"end_reason":"cap" if cap else "natural","winner_team":None if cap else (job["treatment_side"] if treatment_wins else job["control_side"]),"world_tick":45000,"cap":{"world_tick_cap":pilot.CAP_TICK,"marker_observed":cap,"marker_tick":pilot.CAP_TICK+1 if cap else None},"memory":{"process_id":1,"peak_private_bytes":0,"limit_private_bytes":pilot.PROCESS_CAP_BYTES,"samples":[]},"runtime":{"started_utc":None,"finished_utc":None,"exit_code":0,"wall_seconds":1,"stall_seconds":180,"pid_scoped_cleanup":True},"artifacts":{key:{"path":None,"sha256":"b"*64 if key=="map_yaml" else None,"missing_reason":"fixture"} for key in ("server_log","driver_log","matches_jsonl","replay","map_yaml","support_bundle")}}
 return receipt,rows

def synthetic_stage(m,manifest_sha,pairs=1):
 """Receipts, match rows and a PASS replay-health map for every cell whose pair index is below `pairs`."""
 receipts=[]; rows=[]; health={}
 for job in pilot.make_jobs(m):
  if job["pair"]>=pairs: continue
  receipt,game_rows=synthetic_game(m,job,manifest_sha,"uid:"+job["cell_id"],treatment_wins=job["game_in_pair"]==0)
  receipts.append(receipt); rows.extend(game_rows); health[receipt["game_uid"]]={"verdict":"PASS","sha256":"d"*64}
 return receipts,rows,health

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

 def test_readiness_record_reports_accepting_and_the_pin_and_integrity_detail(self):
  clean=self.readiness(self.readiness_case())
  self.assertTrue(clean["accepting"]);self.assertEqual([],clean["blocking_reasons"]);self.assertFalse(clean["execution_authorized"])
  self.assertEqual(([],[]),(clean["pin_problems"],clean["integrity_problems"]))
  self.assertIs(True,clean["negative_control"]["self_attested"])
  held=self.readiness(self.readiness_case(),pins_match=False,pin_problems=["pin mismatch campaign_runner_sha256"],
                      evidence_integrity="HOLD",integrity_problems=["replay_health_report cannot be read"])
  self.assertFalse(held["accepting"]);self.assertEqual("HOLD",held["readiness"])
  self.assertEqual(["pin mismatch campaign_runner_sha256"],held["pin_problems"]);self.assertEqual(["replay_health_report cannot be read"],held["integrity_problems"])
  self.assertIn("one or more manifest/analyzer/schema pins mismatch",held["blocking_reasons"])
  self.assertIn("evidence_integrity is not PASS",held["blocking_reasons"])
  self.assertFalse(self.readiness(self.readiness_case(),run_kind=None)["accepting"])

 def test_cap_tick_matches_the_campaign_runner_constant(self):
  runner=(ROOT/"tools/ai/campaign_runner.py").read_text(encoding="utf-8")
  self.assertEqual(int(re.search(r"^CAP_TICK = (\d+)",runner,re.M).group(1)),pilot.CAP_TICK)

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

class AnalyzerGateTests(unittest.TestCase):
 """analyze() accepting flag, receipt rejection, analyzer-measured pins and the CLI gate (fixtures only)."""
 @classmethod
 def setUpClass(cls): cls.m=manifest(); cls.sha="a"*64

 def stage(self): return synthetic_stage(self.m,self.sha)

 def analyze(self,receipts,rows,health,**kw):
  return pilot.analyze(self.m,self.sha,receipts,rows,health,checkpoint_stage=16,run_kind="pilot",**kw)

 def test_clean_stage_is_accepting_and_recounted(self):
  receipts,rows,health=self.stage(); out=self.analyze(receipts,rows,health)
  self.assertTrue(out["accepting"],out["hold_reasons"]);self.assertEqual([],out["hold_reasons"])
  self.assertEqual({"expected_cells":32,"received_cells":32,"natural_games":32,"censored_games":0,"incomplete_games":0,"invalid_games":0},out["recount"])
  self.assertEqual(64,len(out["games"]));self.assertEqual([],out["rejected_receipts"])

 def test_censored_cap_is_counted_and_does_not_stop_accepting(self):
  receipts,rows,health=self.stage(); job=pilot.make_jobs(self.m)[0]
  index=next(i for i,r in enumerate(receipts) if r["cell_id"]==job["cell_id"]); uid=receipts[index]["game_uid"]
  receipts[index],cap_rows=synthetic_game(self.m,job,self.sha,uid,end_class="CAP")
  rows=[r for r in rows if r["game_uid"]!=uid]+cap_rows
  out=self.analyze(receipts,rows,health)
  self.assertTrue(out["accepting"],out["hold_reasons"])
  self.assertEqual(("CENSORED_CAP",1,31),(next(g["verdict"] for g in out["games"] if g["cell_id"]==job["cell_id"]),out["recount"]["censored_games"],out["recount"]["natural_games"]))
  setup=next(x for x in out["setups"] if x["setup"]==job["setup"])
  self.assertEqual(1,setup["censored_games"]);self.assertEqual(0,setup["complete_natural_pairs"])

 def test_each_analysis_hold_reason_makes_accepting_false(self):
  beyond=next(j for j in pilot.make_jobs(self.m) if j["pair"]==1)
  def beyond_stage(receipts,rows,health):
   receipt,game_rows=synthetic_game(self.m,beyond,self.sha,"uid:"+beyond["cell_id"]);receipts.append(receipt);rows.extend(game_rows)
  health_unusable="replay_health is missing, failing, or unpinned for "
  cases=[("missing receipt",lambda r,w,h:r.pop(),"INCOMPLETE_UNKNOWN game: "),
         ("duplicate cell",lambda r,w,h:r.append(dict(r[0])),"duplicate cells are present"),
         ("duplicate receipt uid",lambda r,w,h:r[1].update(game_uid=r[0]["game_uid"]),"duplicate receipt UIDs are present"),
         ("unplanned cell",lambda r,w,h:r.append({**r[0],"cell_id":"unplanned|0|0","game_uid":"uid:unplanned"}),"unexpected cells are present"),
         ("row for an unknown uid",lambda r,w,h:w.append({"game_uid":"foreign"}),"one or more game UIDs do not match"),
         ("row without uid",lambda r,w,h:w.append({"player":{}}),"one or more game UIDs do not match"),
         ("rejected row",lambda r,w,h:w.append([]),"rejected_rows is not empty"),
         ("non-object receipt",lambda r,w,h:r.append(None),"receipts were rejected"),
         ("invalid game",lambda r,w,h:r[0].update(resolved_seats=None),"INVALID_UNKNOWN game: "),
         ("incomplete game",lambda r,w,h:r[0].update(end_class="INCOMPLETE",end_reason="MEMORY_KILL",winner_team=None),"INCOMPLETE_UNKNOWN game: "),
         ("health missing",lambda r,w,h:h.pop(r[0]["game_uid"]),health_unusable),
         ("health failing",lambda r,w,h:h[r[0]["game_uid"]].update(verdict="BLOCK"),health_unusable),
         ("health unpinned",lambda r,w,h:h[r[0]["game_uid"]].update(sha256="not-a-hash"),health_unusable),
         ("beyond the stage",beyond_stage,"cells beyond the checkpoint stage are present")]
  for label,spoil,needle in cases:
   with self.subTest(label):
    receipts,rows,health=self.stage();spoil(receipts,rows,health)
    out=self.analyze(receipts,rows,health)
    self.assertFalse(out["accepting"]);self.assertTrue(any(needle in reason for reason in out["hold_reasons"]),out["hold_reasons"])

 def test_null_resolved_seats_is_a_recorded_rejection_not_a_crash(self):
  receipts,rows,health=self.stage(); required=pilot.make_jobs(self.m)[0]["required_seats"]
  for value in (None,"seats",5,{},[None],[[]],[None]*required):
   with self.subTest(resolved_seats=value):
    spoiled=copy.deepcopy(receipts);spoiled[0]["resolved_seats"]=value
    out=self.analyze(spoiled,rows,health)
    game=next(g for g in out["games"] if g["cell_id"]==spoiled[0]["cell_id"])
    self.assertEqual("INVALID_UNKNOWN",game["verdict"]);self.assertTrue(game["reason"])
    self.assertFalse(out["accepting"])

 def test_malformed_top_level_inputs_are_recorded_rejections(self):
  out=pilot.analyze(self.m,self.sha,{"cell_id":"x"},"rows")
  self.assertEqual(1,len(out["rejected_receipts"]));self.assertEqual(1,len(out["rejected_rows"]));self.assertFalse(out["accepting"])
  out=pilot.analyze(self.m,self.sha,[None,5,"x",[],{"cell_id":7}],[None])
  self.assertEqual([0,1,2,3,4],[r["index"] for r in out["rejected_receipts"]]);self.assertEqual(1,len(out["rejected_rows"]));self.assertFalse(out["accepting"])

 def test_without_a_valid_stage_every_planned_cell_is_in_scope(self):
  receipts,rows,health=self.stage()
  out=pilot.analyze(self.m,self.sha,receipts,rows,health)
  self.assertFalse(out["accepting"]);self.assertEqual(64,out["recount"]["expected_cells"]);self.assertEqual(32,out["recount"]["received_cells"])
  for kw in ({"checkpoint_stage":16},{"run_kind":"pilot"},{"checkpoint_stage":48,"run_kind":"pilot"},{"checkpoint_stage":True,"run_kind":"pilot"},{"checkpoint_stage":16,"run_kind":["pilot"]}):
   with self.subTest(kw):
    self.assertEqual(64,pilot.analyze(self.m,self.sha,receipts,rows,health,**kw)["recount"]["expected_cells"])
  self.assertFalse(pilot.analyze(self.m,self.sha,[],[])["accepting"])

 def test_pins_cover_every_manifest_pinned_input_and_nothing_else(self):
  self.assertEqual(set(self.m["tool_sha256"])|{"manifest","receipt_schema"},set(pilot.EVIDENCE_PIN_FILES.values()))
  raw=b'{"x":1}'; measured=pilot.pinned_measurements(ROOT,raw); good=exact_pins(raw)
  check=lambda claims,m=measured:pilot.check_pins(claims,pilot.EVIDENCE_PIN_FILES,m)
  self.assertEqual([],check(good));self.assertEqual([],check({k:v.upper() for k,v in good.items()}))
  for key in good:
   with self.subTest(key):
    wrong=dict(good,**{key:"0"*64});self.assertEqual(["pin mismatch "+key],check(wrong))
    malformed=dict(good,**{key:123});self.assertEqual(["pin mismatch "+key],check(malformed))
    missing=dict(good);del missing[key];self.assertEqual(["missing pin "+key],check(missing))
  self.assertEqual(["unexpected pin campaign_file_sha256"],check(dict(good,campaign_file_sha256="0"*64)))
  self.assertEqual(["pins are missing"],check(None));self.assertEqual(["pins are missing"],check([]))
  with tempfile.TemporaryDirectory() as temp:
   bare=pilot.pinned_measurements(pathlib.Path(temp),raw)
  self.assertEqual({"cannot measure "+k for k in ("campaign_runner_sha256","batch_runner_sha256","switch_applier_sha256","receipt_schema_sha256")},set(check(good,bare)))

 def test_health_report_is_rehashed_and_reparsed(self):
  by_uid={"u1":{"verdict":"PASS","sha256":"d"*64}}
  with tempfile.TemporaryDirectory() as temp:
   base=pathlib.Path(temp); raw=json.dumps({"schema":1,"replay_health_by_uid":by_uid}).encode(); (base/"report.json").write_bytes(raw)
   named={"path":"report.json","sha256":sha_of(raw)}
   rec,got,problems=pilot.load_health_report({"replay_health_report":named},base)
   self.assertEqual(([],by_uid,len(raw),sha_of(raw)),(problems,got,rec["size_bytes"],rec["sha256"]))
   self.assertEqual([],pilot.load_health_report({"replay_health_report":dict(named,path=str(base/"report.json")),"replay_health_by_uid":by_uid},base)[2])
   self.assertEqual(["inline replay_health_by_uid differs from the named report"],pilot.load_health_report({"replay_health_report":named,"replay_health_by_uid":{}},base)[2])
   (base/"bad.json").write_bytes(b"not json");(base/"nomap.json").write_bytes(b'{"schema":1}')
   cases=[("unnamed",{},"replay_health_report path and sha256 are not named"),
          ("path not a string",{"replay_health_report":{"path":5,"sha256":"a"*64}},"replay_health_report path and sha256 are not named"),
          ("sha malformed",{"replay_health_report":{"path":"report.json","sha256":"zz"}},"replay_health_report path and sha256 are not named"),
          ("missing file",{"replay_health_report":{"path":"gone.json","sha256":"a"*64}},"replay_health_report cannot be read"),
          ("hash differs",{"replay_health_report":dict(named,sha256="0"*64)},"replay_health_report hash differs from the named sha256"),
          ("not json",{"replay_health_report":{"path":"bad.json","sha256":sha_of(b"not json")}},"replay_health_report is not valid JSON"),
          ("no map",{"replay_health_report":{"path":"nomap.json","sha256":sha_of(b'{"schema":1}')}},"replay_health_report lacks a replay_health_by_uid object")]
   for label,evidence,needle in cases:
    with self.subTest(label):
     _,got,problems=pilot.load_health_report(evidence,base);self.assertEqual(({},[needle]),(got,problems))

 def test_integrity_needs_matching_input_pins_and_a_pinned_evidence_file(self):
  measured={name:pilot.measure_bytes(name,None,raw) for name,raw in (("matches",b"m"),("receipts",b"r"),("readiness_evidence",b"e"))}
  good={"input_pins":{"matches_sha256":sha_of(b"m"),"receipts_sha256":sha_of(b"r")}}
  self.assertEqual([],pilot.check_integrity(good,measured,sha_of(b"e"),[]))
  self.assertEqual(["pin mismatch receipts_sha256"],pilot.check_integrity({"input_pins":dict(good["input_pins"],receipts_sha256="0"*64)},measured,sha_of(b"e"),[]))
  self.assertEqual(["input_pins are missing"],pilot.check_integrity({},measured,sha_of(b"e"),[]))
  for claimed in (None,"zz",sha_of(b"other")):
   with self.subTest(evidence_sha256=claimed):
    self.assertEqual(["readiness evidence file is not pinned by a matching sha256"],pilot.check_integrity(good,measured,claimed,[]))
  self.assertEqual(["replay_health_report cannot be read"],pilot.check_integrity(good,measured,sha_of(b"e"),["replay_health_report cannot be read"]))

 def cli(self,data=None,after=None,evidence_sha=True,with_evidence=True):
  """Write a clean stage-16 case, optionally spoil it, run the real CLI and return (exit code, output or None).
  `data` changes receipts/rows/health before anything is pinned; `after` changes serialized files/evidence after pinning."""
  with tempfile.TemporaryDirectory() as temp:
   base=pathlib.Path(temp); manifest_raw=json.dumps(self.m).encode("utf-8"); manifest_sha=sha_of(manifest_raw)
   receipts,rows,health=synthetic_stage(self.m,manifest_sha)
   if data: data(receipts,rows,health)
   report_raw=json.dumps({"schema":1,"replay_health_by_uid":health}).encode("utf-8")
   files={"manifest.json":manifest_raw,"report.json":report_raw,"receipts.json":json.dumps(receipts).encode("utf-8"),
          "matches.jsonl":"".join(json.dumps(r)+"\n" for r in rows).encode("utf-8")}
   evidence={"run_kind":"pilot","checkpoint_stage":16,"producer_slot_id":"01a186cb-4c77-7362-873d-b2fe66951365",
             "negative_control":{"fixture_sha256":"f"*64,"expected_verdict":"INVALID_UNKNOWN","observed_verdict":"INVALID_UNKNOWN"},
             "pins":exact_pins(manifest_raw),"input_pins":{"matches_sha256":sha_of(files["matches.jsonl"]),"receipts_sha256":sha_of(files["receipts.json"])},
             "replay_health_report":{"path":"report.json","sha256":sha_of(report_raw)}}
   if after: after(files,evidence)
   files["evidence.json"]=json.dumps(evidence).encode("utf-8")
   for name,raw in files.items(): (base/name).write_bytes(raw)
   argv=["analyze","--manifest",str(base/"manifest.json"),"--manifest-sha256",manifest_sha,"--matches",str(base/"matches.jsonl"),
         "--receipts",str(base/"receipts.json"),"--repo-root",str(ROOT),"--output",str(base/"out.json")]
   if with_evidence: argv+=["--readiness-evidence",str(base/"evidence.json")]
   if with_evidence and evidence_sha: argv+=["--readiness-evidence-sha256",sha_of(files["evidence.json"])]
   with contextlib.redirect_stdout(io.StringIO()),contextlib.redirect_stderr(io.StringIO()): code=pilot.main(argv)
   return code,(json.loads((base/"out.json").read_text(encoding="utf-8")) if (base/"out.json").exists() else None)

 def test_cli_clean_case_exits_0_with_every_input_measured_by_the_analyzer(self):
  code,out=self.cli()
  self.assertEqual(0,code,out and out["hold_reasons"]);self.assertTrue(out["accepting"]);self.assertEqual([],out["hold_reasons"])
  self.assertEqual("PASS_REVIEW_ONLY",out["readiness"]["readiness"]);self.assertEqual("PASS",out["readiness"]["evidence_integrity"]);self.assertTrue(out["readiness"]["pins_match"])
  self.assertFalse(out["readiness"]["execution_authorized"])
  measured={x["name"]:x for x in out["input_measurements"]}
  self.assertEqual({"manifest","driver_analyzer","campaign_runner","batch_runner","switch_applier","receipt_schema","matches","receipts","readiness_evidence","replay_health_report"},set(measured))
  self.assertEqual(sha_of((ROOT/"tools/ai/run_ai_match_batch.py").read_bytes()),measured["batch_runner"]["sha256"])
  self.assertEqual(len((ROOT/"tools/ai/ab_campaign_receipt.schema.json").read_bytes()),measured["receipt_schema"]["size_bytes"])

 def test_cli_file_changed_after_pinning_is_a_hold(self):
  code,out=self.cli(after=lambda files,evidence:files.update({"receipts.json":files["receipts.json"]+b" "}))
  self.assertEqual(2,code);self.assertFalse(out["accepting"]);self.assertIn("integrity: pin mismatch receipts_sha256",out["hold_reasons"])
  self.assertEqual("HOLD",out["readiness"]["evidence_integrity"])
  code,out=self.cli(after=lambda files,evidence:files.update({"matches.jsonl":files["matches.jsonl"]+b"\n"}))
  self.assertEqual(2,code);self.assertIn("integrity: pin mismatch matches_sha256",out["hold_reasons"])

 def test_cli_receipts_file_that_is_not_an_array_is_recorded_not_a_crash(self):
  for raw in (b'{"cell_id":"x"}',b"5",b"null"):
   with self.subTest(raw):
    code,out=self.cli(after=lambda files,evidence,raw=raw:files.update({"receipts.json":raw}))
    self.assertEqual(2,code);self.assertIsNotNone(out);self.assertEqual(1,len(out["rejected_receipts"]));self.assertFalse(out["accepting"])

 def test_cli_claimed_integrity_pass_is_ignored_when_the_report_hash_differs(self):
  def spoil(files,evidence): evidence["evidence_integrity"]="PASS";evidence["replay_health_report"]["sha256"]="0"*64
  code,out=self.cli(after=spoil)
  self.assertEqual(2,code);self.assertFalse(out["accepting"]);self.assertEqual("HOLD",out["readiness"]["evidence_integrity"])
  self.assertIn("integrity: replay_health_report hash differs from the named sha256",out["hold_reasons"])
  code,out=self.cli(after=lambda files,evidence:evidence.pop("replay_health_report"))
  self.assertEqual(2,code);self.assertIn("integrity: replay_health_report path and sha256 are not named",out["hold_reasons"])

 def test_cli_every_tool_pin_is_checked_against_the_analyzers_own_hash(self):
  for key in PIN_FILES:
   with self.subTest(key):
    code,out=self.cli(after=lambda files,evidence,key=key:evidence["pins"].update({key:"0"*64}))
    self.assertEqual(2,code);self.assertFalse(out["readiness"]["pins_match"]);self.assertIn("pin: pin mismatch "+key,out["hold_reasons"])
  code,out=self.cli(after=lambda files,evidence:evidence["pins"].update({"campaign_file_sha256":"0"*64}))
  self.assertEqual(2,code);self.assertIn("pin: unexpected pin campaign_file_sha256",out["hold_reasons"])
  code,out=self.cli(after=lambda files,evidence:evidence["pins"].pop("batch_runner_sha256"))
  self.assertEqual(2,code);self.assertIn("pin: missing pin batch_runner_sha256",out["hold_reasons"])

 def test_cli_without_evidence_or_without_its_pin_is_a_hold(self):
  code,out=self.cli(with_evidence=False)
  self.assertEqual(2,code);self.assertFalse(out["accepting"]);self.assertEqual("HOLD",out["readiness"]["readiness"])
  code,out=self.cli(evidence_sha=False)
  self.assertEqual(2,code);self.assertIn("integrity: readiness evidence file is not pinned by a matching sha256",out["hold_reasons"])

 def test_cli_null_resolved_seats_is_recorded_and_not_accepting(self):
  code,out=self.cli(data=lambda receipts,rows,health:receipts[0].update(resolved_seats=None))
  self.assertEqual(2,code);self.assertIsNotNone(out);self.assertFalse(out["accepting"])
  self.assertEqual(1,out["recount"]["invalid_games"]);self.assertTrue(any(x.startswith("INVALID_UNKNOWN game: ") for x in out["hold_reasons"]))

 def test_cli_censored_cap_game_is_reported_and_still_accepting(self):
  def cap_one(receipts,rows,health):
   job=pilot.make_jobs(self.m)[0]; uid=receipts[0]["game_uid"]
   manifest_sha=receipts[0]["manifest_sha256"]
   receipts[0],cap_rows=synthetic_game(self.m,job,manifest_sha,uid,end_class="CAP")
   rows[:]=[r for r in rows if r["game_uid"]!=uid]+cap_rows
  code,out=self.cli(data=cap_one)
  self.assertEqual(0,code,out and out["hold_reasons"]);self.assertTrue(out["accepting"])
  self.assertEqual((1,31),(out["recount"]["censored_games"],out["recount"]["natural_games"]))

if __name__=="__main__": unittest.main()
