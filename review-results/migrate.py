from pathlib import Path
import json,sys,re
root=Path('C:/cameo-wt/codex-multi-turret');sys.path.insert(0,str(root/'tools/audit'))
from miniyaml import Ruleset
rules=Ruleset(root)
rows=json.loads((root/'review-results/multi_turret_inventory.json').read_text())
edits={}
def emit(node,indent):
 out=[indent+node.key+':'+(' '+node.value if node.value else '')]
 for child in node.children: out+=emit(child,indent+'\t')
 return out
for row in rows:
 name=row['actor']; raw=rules.actors[name]; path=Path(raw.file)
 lines=edits.setdefault(path,path.read_text(encoding='utf-8-sig').splitlines())
 start=next(i for i,line in enumerate(lines) if line.split(':',1)[0]==name)
 end=next((i for i in range(start+1,len(lines)) if lines[i] and not lines[i][0].isspace() and not lines[i].startswith('#')),len(lines))
 block=lines[start:end]
 # Remove only actor-local old attack blocks; inherited originals are cancelled explicitly.
 new=[];i=0
 while i<len(block):
  line=block[i]
  if re.match(r'^\t-?AttackTurreted(?:@[^:]+)?:',line):
   i+=1
   while i<len(block) and (block[i].startswith('\t\t') or not block[i].strip()):i+=1
  else:new.append(line);i+=1
 for trait in rules.resolve(name).children_named('AttackTurreted'):
  new.append('\t-'+trait.key+':')
  copied=trait.deep_copy();copied.key=copied.key.replace('AttackTurreted','AttackMultiTurreted',1)
  new+=emit(copied,'\t')
 lines[start:end]=new+['']
for p,lines in edits.items():p.write_text('\n'.join(lines)+'\n',encoding='utf-8')
# Dynamic mode retains target eligibility/stance filters but ignores legacy class priority weights.
count=0; files=0
for p in (root/'mods/cameo').rglob('*.yaml'):
 s=p.read_text(encoding='utf-8-sig');lines=s.splitlines();new=[];changed=False
 for i,line in enumerate(lines):
  new.append(line)
  m=re.match(r'^(\s+)AutoTarget(?:@[^:]+)?:\s*(?:#.*)?$',line)
  if m:
   indent=m[1];j=i+1;already=False
   while j<len(lines) and (not lines[j].strip() or len(lines[j])-len(lines[j].lstrip())>len(indent)):
    if lines[j].strip().startswith('DynamicWeaponPriority:'):already=True
    j+=1
   if not already:new.append(indent+'\tDynamicWeaponPriority: true');count+=1;changed=True
 if changed:p.write_text('\n'.join(new)+'\n',encoding='utf-8');files+=1
print(f'Migrated {len(rows)} multi-turret actors; enabled dynamic priority on {count} declarations in {files} files.')
