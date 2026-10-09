#!/usr/bin/env python3
"""One-off audit: enumerate ProvidesPrerequisite@swlimit providers, their
global-swlimit gating, and the items that consume each token via !token
in Buildable.Prerequisites. Classifies standalone SW buildings vs
plug-hosts vs other. Read-only; prints a table for the Track-B
extension ruling (spec section 5.1)."""
import os
import re
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'mods', 'cameo')

ACTOR_RE = re.compile(r'^([A-Za-z0-9_\^\.]+):\s*$')
TRAIT_RE = re.compile(r'^\t([A-Za-z0-9_@\.\-]+):\s*$')
FIELD_RE = re.compile(r'^\t\t([A-Za-z0-9_]+):\s*(.*)$')
NESTED_RE = re.compile(r'^\t{3,}')

def indent_of(line):
    n = 0
    for c in line:
        if c == '\t':
            n += 1
        else:
            break
    return n

def main():
    # actor -> list of (instance, fields dict)
    actors = {}          # name -> {'swlimit': [fields], 'buildable_prereqs': str, 'has_plug': bool, 'has_pluggable': bool}
    order = []

    for dirpath, _dirs, files in os.walk(ROOT):
        for fn in files:
            if not fn.endswith('.yaml'):
                continue
            path = os.path.join(dirpath, fn)
            rel = os.path.relpath(path, ROOT)
            actor = None
            cur_trait = None
            cur_fields = None
            try:
                lines = open(path, encoding='utf-8').read().splitlines()
            except OSError:
                continue
            for raw in lines:
                line = raw.rstrip()
                if not line or line.lstrip().startswith('#'):
                    continue
                ind = indent_of(line)
                m = ACTOR_RE.match(line)
                if ind == 0 and m:
                    actor = m.group(1)
                    if actor not in actors:
                        actors[actor] = {'swlimit': [], 'buildable_prereqs': '', 'has_plug': False, 'has_pluggable': False}
                        order.append(actor)
                    cur_trait = None
                    continue
                if actor is None:
                    continue
                if ind == 1:
                    tm = TRAIT_RE.match(line)
                    cur_trait = tm.group(1) if tm else None
                    cur_fields = {}
                    if cur_trait == 'ProvidesPrerequisite@swlimit':
                        actors[actor]['swlimit'].append(cur_fields)
                    elif cur_trait == 'Plug':
                        actors[actor]['has_plug'] = True
                    elif cur_trait and cur_trait.startswith('Pluggable'):
                        actors[actor]['has_pluggable'] = True
                    continue
                if ind == 2 and cur_trait is not None:
                    fm = FIELD_RE.match(line)
                    if fm and cur_fields is not None:
                        cur_fields[fm.group(1)] = fm.group(2).strip()
                    if cur_trait == 'Buildable' and fm and fm.group(1) == 'Prerequisites':
                        actors[actor]['buildable_prereqs'] = fm.group(2).strip()

    # token -> provider info
    tokens = {}
    for name in order:
        for f in actors[name]['swlimit']:
            tok = f.get('Prerequisite', '')
            if not tok:
                continue
            tokens.setdefault(tok, []).append({
                'actor': name,
                'cond': f.get('RequiresCondition', ''),
                'gated': 'global-swlimit' in f.get('RequiresPrerequisites', ''),
            })

    # token -> consumers (items whose Buildable.Prerequisites contains !token)
    consumers = {t: [] for t in tokens}
    for name in order:
        pr = actors[name]['buildable_prereqs']
        if not pr:
            continue
        for tok in tokens:
            if re.search(r'(?:^|,\s*|~)!' + re.escape(tok) + r'(?:\s*,|\s*$)', pr):
                consumers[tok].append(name)

    gated_n = sum(1 for t in tokens for p in tokens[t] if p['gated'])
    total_n = sum(len(v) for v in tokens.values())
    print(f'@swlimit providers: {total_n} instances, {len(tokens)} tokens, {gated_n} gated')
    print()
    print(f"{'token':28} {'gated':5} {'providers':34} {'cond':18} consumers")
    for tok in sorted(tokens):
        ps = tokens[tok]
        gated = all(p['gated'] for p in ps)
        provs = ','.join(f"{p['actor']}{'*' if not p['gated'] else ''}" for p in ps)
        conds = ','.join(sorted({p['cond'] for p in ps if p['cond']}))
        cons = ','.join(consumers[tok]) or '-'
        print(f"{tok:28} {str(gated):5} {provs:34} {conds:18} {cons}")

    print()
    print('Classification (consumers):')
    for tok in sorted(tokens):
        prov_actors = {p['actor'] for p in tokens[tok]}
        for c in consumers[tok]:
            kind = ('SELF/standalone' if c in prov_actors
                    else 'PLUG' if actors[c]['has_plug']
                    else 'other')
            print(f"  {tok:28} -> {c:34} {kind}")
        if not consumers[tok]:
            print(f"  {tok:28} -> (no consumer)")

if __name__ == '__main__':
    sys.exit(main())
