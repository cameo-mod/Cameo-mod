import pathlib, sys, unittest
ROOT=pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/"tools"/"ai"))
import ab_campaign_pilot as pilot

def manifest():
 return {"schema":1,"switch":pilot.SWITCH,"execution_approved":False,"pairs_per_setup":2,"team_size_scope":[1,2,3,4],"arm_bot_types":{"control":"hard_control","treatment":"hard_treatment"},"seeds":[1337,7331],"maps":[{"name":k,"path":v[0],"sha256":v[1],"required_seats":v[2]} for k,v in pilot.MAPS.items()],"setups":[{"id":n,"team_size":s,"map":m,"team_factions":list(f)} for n,s,m,f in pilot.SETUPS]}

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
  result=pilot.dry_run(m,ROOT)
  self.assertEqual(0,result["launches"]);self.assertFalse(result["execution_authorized"])
  self.assertEqual(11.07,result["wall_estimate_hours"])

 def test_map_tamper_and_execution_authority_fail_closed(self):
  m=manifest();m["maps"][0]["sha256"]="0"*64
  with self.assertRaisesRegex(ValueError,"map path/SHA"):
   pilot.validate(m,ROOT)
  m=manifest();m["execution_approved"]=True
  with self.assertRaisesRegex(ValueError,"must not authorize"):
   pilot.validate(m,ROOT)

 def test_missing_map_is_rejected(self):
  m=manifest();m["maps"][0]["path"]="mods/cameo/maps/missing.oramap"
  with self.assertRaisesRegex(ValueError,"map path/SHA"):
   pilot.validate(m,ROOT)

 def test_adjudicator_keeps_cap_unknown_and_sums_team_economy(self):
  m=manifest(); job=next(j for j in pilot.make_jobs(m) if j["team_size"]==2 and j["game_in_pair"]==0)
  seats=[]; rows=[]
  for side,arm,bot,outcome in (("A","control","hard_control","lost"),("B","treatment","hard_treatment","won")):
   for faction in job["team_factions"]:
    home=f"{len(seats)},0"; seats.append({"home":home,"side":side,"arm":arm,"bot_type":bot,"faction":faction})
    rows.append({"player":{"home":home,"bot_type":bot,"faction":faction,"outcome":outcome},"stats_timeline":[{"tick":100,"earned":12,"spent":5,"banked":7}]})
  receipt={"cell_id":job["cell_id"],"manifest_sha256":"a"*64,"switch":job["switch"],"setup":job["setup"],"map":job["map"],"map_sha256":job["map_sha256"],"seed":job["seed"],"pair":job["pair"],"game_in_pair":job["game_in_pair"],"control_side":job["control_side"],"treatment_side":job["treatment_side"],"arm_bot_types":job["arm_bot_types"],"status":"NATURAL_END","end_reason":"natural","seat_proof_source":"generated_map.yaml","map_yaml_sha256":"b"*64,"seats":seats}
  got=pilot.adjudicate(rows,receipt,job,"a"*64)
  self.assertEqual("TREATMENT_WIN",got["verdict"]);self.assertEqual(24,got["team_totals"]["control"]["earned"])
  receipt.update({"status":"CENSORED_CAP","cap_marker":True,"cap_tick":45000,"support_complete":True})
  self.assertEqual("CENSORED_CAP",pilot.adjudicate(rows,receipt,job,"a"*64)["verdict"])
  receipt["cap_tick"]=15000
  self.assertEqual("INVALID_UNKNOWN",pilot.adjudicate(rows,receipt,job,"a"*64)["verdict"])

 def test_analyzer_preserves_all_unrun_planned_games(self):
  result=pilot.analyze(manifest(),"a"*64,[],[])
  self.assertEqual(64,len(result["games"]))
  self.assertEqual(16,len(result["setups"]))
  self.assertTrue(all(x["verdict"]=="INCOMPLETE_UNKNOWN" for x in result["games"]))

if __name__=="__main__": unittest.main()
