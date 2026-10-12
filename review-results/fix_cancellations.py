from pathlib import Path
import sys,json
root=Path('C:/cameo-wt/codex-multi-turret');sys.path.insert(0,str(root/'tools/audit'));from miniyaml import Ruleset
rules=Ruleset(root);edits={};removed=[]
for name in json.loads((root/'review-results/migration_audit.json').read_text())['migrated']:
 actor=rules.actors[name]
 inherited={t.key for c in actor.children if c.key.split('@')[0]=='Inherits' for t in rules.resolve(c.value).children_named('AttackTurreted')}
 p=Path(actor.file);lines=edits.setdefault(p,p.read_text(encoding='utf-8-sig').splitlines())
 start=next(i for i,l in enumerate(lines) if l.split(':',1)[0]==name)
 end=next((i for i in range(start+1,len(lines)) if lines[i] and not lines[i][0].isspace() and not lines[i].startswith('#')),len(lines))
 block=lines[start:end];new=[]
 for line in block:
  if line.startswith('\t-AttackTurreted') and line.strip()[1:].split(':')[0] not in inherited: removed.append(name)
  else:new.append(line)
 lines[start:end]=new
for p,lines in edits.items():p.write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('Removed invalid cancellation from:',removed)
