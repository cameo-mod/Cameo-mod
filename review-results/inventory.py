import json,sys
from pathlib import Path
root=Path('C:/cameo-wt/codex-multi-turret')
sys.path.insert(0,str(root/'tools/audit'))
from miniyaml import Ruleset
r=Ruleset(root)
rows=[]
for name in sorted(r.actors):
 if name.startswith('^'): continue
 a=r.resolve(name)
 ts=a.children_named('Turreted')
 ats=a.children_named('AttackTurreted')
 if len(ts)>1 and ats:
  arms=a.children_named('Armament')
  rows.append(dict(actor=name,turrets=[t.get('Turret') or 'primary' for t in ts],attacks=[t.key for t in ats],arms=[dict(key=t.key,name=t.get('Name') or 'primary',turret=t.get('Turret') or 'primary',weapon=t.get('Weapon'),condition=t.get('RequiresCondition')) for t in arms]))
p=root/'review-results/multi_turret_inventory.json'
p.write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
print('Resolved multi-turret AttackTurreted actors:',len(rows))
for row in rows:
 if row['actor'] in ['japan_oitank','japan_yamatobattleship']: print(json.dumps(row))
