from pathlib import Path
import sys,json
sys.path.insert(0,str(Path.cwd()/'tools/audit'))
from miniyaml import Ruleset
root=Path.cwd(); rules=Ruleset(root)
rows=json.loads((root/'review-results/garrison_capacity.json').read_text())
byfile={}; manifest=[]
for row in rows:
    if row['capacity_upper_bound']<=row['ports'] or row['intentional_no_fire']: continue
    actor=rules.actor(row['actor']); path=Path(actor.file)
    byfile.setdefault(path,[]).append((actor.line,row))
for path, entries in byfile.items():
    lines=path.read_text(encoding='utf-8-sig').splitlines(keepends=True)
    for lineno,row in sorted(entries,reverse=True):
        begin=lineno-1; end=begin+1
        while end<len(lines) and (not lines[end].strip() or lines[end][0] in ' \t#'): end+=1
        target=None
        for i in range(begin+1,end):
            if lines[i].startswith('\tAttackGarrisoned:'):
                target=i+1; break
        comment='\t\t# Existing geometry is retained; excess occupants have no firing station.\n\t\tNoFireOverflow: true\n'
        if target is not None: lines[target:target]=[comment]
        else: lines[end:end]=['\tAttackGarrisoned:\n'+comment]
        manifest.append(dict(row,source=path.relative_to(root).as_posix(),line=lineno))
    path.write_text(''.join(lines),encoding='utf-8',newline='\n')
(root/'docs/design/garrison_overflow_manifest.json').write_text(json.dumps({'version':1,'actors':sorted(manifest,key=lambda r:r['actor'])},indent=2)+'\n',encoding='utf-8')
print(f'Acknowledged {len(manifest)} actors in {len(byfile)} files')
