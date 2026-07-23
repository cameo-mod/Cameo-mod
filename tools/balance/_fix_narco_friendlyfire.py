#!/usr/bin/env python3
import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
p = ROOT / "docs" / "balance" / "redalert2mod_syndicate.json"
d = json.loads(p.read_text(encoding="utf-8-sig"))

u = d["sections"]["infantry"]["latinsyndicate_narco"]
arm = next(a for a in u["armaments"] if a["weapon"] == "NarcoGrenade")
w = next(w for w in arm["warheads"] if w["tag"] == "GrenadeFriendlyFire")
w["damage"] = 35000

p.write_text(json.dumps(d, ensure_ascii=False, indent=1), encoding="utf-8")
print("set latinsyndicate_narco GrenadeFriendlyFire damage back to 35000")
