import re, glob, os
wf='mods/cameo/weapons/weapons.yaml'
lines=open(wf,encoding='utf-8',errors='ignore').read().split('\n')
divider=next(i for i,l in enumerate(lines,1) if 'DO NOT INHERIT BELOW THIS LINE' in l)
defs=[]
for i,l in enumerate(lines,1):
    if i<=divider: continue
    m=re.match(r'^([\^A-Za-z0-9]+):', l)
    if m: defs.append(m.group(1))
print(f"divider line {divider}; {len(defs)} top-level weapon defs BELOW it")

allyaml=glob.glob('mods/cameo/**/*.yaml',recursive=True)
text_by_file={f:open(f,encoding='utf-8',errors='ignore').read() for f in allyaml}

def pack_of(f):
    ff=f.replace(os.sep,'/')
    if 'ContentPacks/' in ff:
        return '/'.join(ff.split('ContentPacks/')[1].split('/')[:2])
    if ff.endswith('weapons/weapons.yaml'):
        return 'CENTRAL'
    return 'central:'+os.path.basename(ff)

def users_of(name):
    bare=name.lstrip('^')
    pats=[re.compile(rf'Inherits[^:]*:\s*{re.escape(name)}\b'),
          re.compile(rf'Weapon@?\w*:\s*{re.escape(bare)}\b'),
          re.compile(rf'Weapon@?\w*:\s*{re.escape(name)}\b')]
    users=set()
    for f,txt in text_by_file.items():
        for p in pats:
            if p.search(txt):
                users.add(pack_of(f)); break
    return users

unused=[]; single=[]; multi=[]
for d in defs:
    u=users_of(d); u.discard('CENTRAL')
    if not u: unused.append(d)
    elif len(u)==1: single.append((d,list(u)[0]))
    else: multi.append((d,sorted(u)))
print(f"\nUNUSED ({len(unused)}) -> DELETE:")
print('  '+', '.join(unused))
print(f"\nSINGLE-pack ({len(single)}) -> move to that pack:")
for d,p in single: print(f"  {d:<26} -> {p}")
print(f"\nMULTI-pack ({len(multi)}) -> shared/decide:")
for d,ps in multi: print(f"  {d:<26} -> {', '.join(ps)}")
