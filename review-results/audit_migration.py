from pathlib import Path
import sys,json
r=Path('C:/cameo-wt/codex-multi-turret');sys.path.insert(0,str(r/'tools/audit'));from miniyaml import Ruleset
s=Ruleset(r);errors=[];counts={};exception=[]
for name in sorted(s.actors):
 if name.startswith('^'):continue
 a=s.resolve(name)
 for auto in a.children_named('AutoTarget'):
  if auto.get('DynamicWeaponPriority')!='true':errors.append(name+':missing dynamic')
 ts=a.children_named('Turreted');ats=a.children_named('AttackMultiTurreted')
 if ats:
  counts[name]=[t.get('Turret') or 'primary' for t in ts]
  if a.children_named('AttackTurreted'):errors.append(name+':old attack remains')
  for attack in ats:
   selected=(attack.get('Turrets') or 'primary').replace(' ','').split(',')
   armnames=(attack.get('Armaments') or 'primary,secondary').replace(' ','').split(',')
   missing=[(arm.key,arm.get('Turret') or 'primary') for arm in a.children_named('Armament') if (arm.get('Name') or 'primary') in armnames and (arm.get('Turret') or 'primary') not in selected]
   if missing:errors.append(name+':ungrouped:'+str(missing))
 elif len(ts)>1:exception.append((name,[t.key for t in a.children if t.key.startswith('Attack')]))
print('Migrated resolved:',len(counts),'errors:',errors,'other multi:',exception)
(r/'review-results/migration_audit.json').write_text(json.dumps(dict(migrated=counts,errors=errors,other_multi=exception),indent=2)+'\n')
raise SystemExit(bool(errors))
