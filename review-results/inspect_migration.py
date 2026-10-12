import sys
from pathlib import Path
r=Path('C:/cameo-wt/codex-multi-turret');sys.path.insert(0,str(r/'tools/audit'));from miniyaml import Ruleset
s=Ruleset(r)
for n in ['japan_oitank','japan_yamatobattleship','japan_exorcistoitank']:
 a=s.actors[n];z=s.resolve(n)
 print(n,a.file,a.line,[(x.key,x.value,[(c.key,c.value) for c in x.children]) for x in z.children_named('AttackTurreted')])
