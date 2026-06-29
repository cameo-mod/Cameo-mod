#!/usr/bin/env python
"""Surgical sequence-YAML rewriter for the baked TS batch.

For every sub-sequence that resolves to a baked SHP, set effective Scale to 1
(insert or replace the sub's own `Scale:` line, overriding any Defaults) and
multiply its `Offset:` by the display scale s. Leaves Defaults / muzzle / icon
and all formatting/comments untouched. Line-based edits (no reserialize).

Dry-run by default; pass --apply to write yaml + copy baked SHPs over originals.
"""
import os, sys, shutil, struct, glob
from collections import defaultdict
import bake_shp_downscale as bk
import shp_scale_audit as audit
import batch_bake

OUTDIR = "baked"

def pass_set():
    """Recompute candidates + bake gate, return {key: {'scale','disk','ok'}}."""
    cands = batch_bake.gather()
    res = {}
    for c in cands:
        W, H, ofr = bk.read_ts(c['disk'])
        Wn, Hn, bfr = bk.downscale(W, H, ofr, c['scale'])
        outp = os.path.join(OUTDIR, c['key'])
        if not os.path.exists(outp):
            bk.write_ts(outp, Wn, Hn, bfr)
        _, _, rb = bk.read_ts(outp)
        ok, _ = batch_bake.geom_check(ofr, rb, c['scale'])
        with open(outp, 'rb') as f:
            if not batch_bake.is_shp_ts(f.read()):
                ok = False
        res[c['key']] = {'scale': c['scale'], 'disk': c['disk'], 'ok': ok}
    return res

TAB = '\t'

def indent(line):
    return len(line) - len(line.lstrip('\t'))

def scale_offset(val, s):
    # Offset is "x, y" possibly "x,y,z" floats; multiply each by s, round to int
    parts = [p.strip() for p in val.split(',')]
    out = []
    for p in parts:
        try:
            out.append(str(int(round(float(p) * s))))
        except ValueError:
            out.append(p)
    return ', '.join(out)

def rewrite_file(path, baked):
    """baked: {shpkey: scale}. Return (newlines, edits[])."""
    with open(path, encoding='utf-8') as f:
        lines = f.readlines()
    edits = []
    # First pass: index actor blocks and their sub-sequences with line spans.
    n = len(lines)
    i = 0
    # We walk maintaining: actor name, Defaults Filename, and per-sub spans.
    out = list(lines)
    # Build structure: list of (actor_start). We do a stateful single walk and
    # collect sub-sequence blocks with their resolved filename + scale source.
    actor = None
    d_file = None
    subs = []   # (name, header_idx, prop_start, prop_end) filled as we go
    blocks = [] # finalized actor blocks: dict(actor, d_file, subs=[...])
    cur = None

    def close_actor():
        if actor is not None:
            blocks.append({'actor': actor, 'd_file': d_file, 'subs': subs[:]})

    raw = [l.rstrip('\n') for l in lines]
    for idx, s in enumerate(raw):
        if not s.strip() or s.lstrip().startswith('#'):
            continue
        ind = indent(s)
        body = s.strip()
        if ind == 0 and body.endswith(':'):
            close_actor()
            actor = body[:-1]; d_file = None; subs = []; cur = None
            continue
        if actor is None:
            continue
        if ind == 1 and body.endswith(':'):
            cur = {'name': body[:-1], 'hdr': idx, 'file': None, 'scale': None,
                   'scale_line': None, 'offset_line': None}
            subs.append(cur)
            continue
        if ':' in body and ind >= 2 and cur is not None:
            k, _, v = body.partition(':')
            k = k.strip(); v = v.strip()
            if k == 'Filename': cur['file'] = v
            elif k == 'Scale': cur['scale'] = v; cur['scale_line'] = idx
            elif k == 'Offset': cur['offset_line'] = idx
        elif ':' in body and ind >= 2 and cur is None:
            # property under Defaults (cur is None means we're in Defaults? no)
            pass
        # Defaults block: when cur is the 'Defaults' sub
    close_actor()

    # resolve Defaults Filename per actor (Defaults appears as a sub named Defaults)
    for blk in blocks:
        dfile = None
        dscale = None
        for sub in blk['subs']:
            if sub['name'] == 'Defaults':
                dfile = sub['file']; dscale = sub['scale']
        for sub in blk['subs']:
            if sub['name'] == 'Defaults':
                continue
            fn = (sub['file'] or dfile)
            if not fn:
                continue
            key = fn.lower()
            if key not in baked:
                continue
            s = baked[key]
            # effective scale source: own or defaults
            eff = sub['scale'] if sub['scale'] is not None else dscale
            edits.append({'actor': blk['actor'], 'sub': sub['name'], 'key': key,
                          's': s, 'scale_line': sub['scale_line'],
                          'offset_line': sub['offset_line'], 'hdr': sub['hdr'],
                          'eff': eff})
    return edits, raw

def apply_edits(raw, edits):
    """Return new raw lines with Scale set to 1 and Offset scaled. We process
    edits and produce insertions; do it via a per-line replacement map +
    insert-after map to keep indices stable."""
    replace = {}      # idx -> new text
    insert_after = defaultdict(list)  # idx -> [lines]
    for e in edits:
        if e['scale_line'] is not None:
            replace[e['scale_line']] = '\t\tScale: 1'
        else:
            insert_after[e['hdr']].append('\t\tScale: 1')
        if e['offset_line'] is not None:
            old = raw[e['offset_line']]
            k, _, v = old.strip().partition(':')
            replace[e['offset_line']] = '\t\tOffset: ' + scale_offset(v.strip(), e['s'])
    out = []
    for idx, line in enumerate(raw):
        out.append(replace.get(idx, line))
        for ins in insert_after.get(idx, []):
            out.append(ins)
    return out

def main():
    apply = '--apply' in sys.argv
    ps = pass_set()
    baked = {k: v['scale'] for k, v in ps.items() if v['ok']}
    print(f"PASS baked SHPs: {len(baked)}")
    total_edits = 0
    files_changed = []
    for theme in audit.ACTIVE:
        path = os.path.join(audit.SEQ_DIR, theme + '.yaml')
        if not os.path.exists(path):
            continue
        edits, raw = rewrite_file(path, baked)
        if not edits:
            continue
        files_changed.append((theme, len(edits)))
        total_edits += len(edits)
        new = apply_edits(raw, edits)
        if apply:
            with open(path, 'w', encoding='utf-8', newline='\n') as f:
                f.write('\n'.join(new) + '\n')
        else:
            # show a couple sample edits for this theme
            for e in edits[:2]:
                act = 'replace' if e['scale_line'] is not None else 'insert'
                print(f"  [{theme}] {e['actor']}/{e['sub']}  {e['key']} s={e['s']}"
                      f"  Scale->1 ({act})"
                      + (f"  Offset@{e['offset_line']}" if e['offset_line'] else ""))
    print(f"\n{total_edits} sub-sequence edits across {len(files_changed)} files:")
    for t, c in files_changed:
        print(f"  {t}.yaml: {c}")
    if apply:
        copied = 0
        for k, v in ps.items():
            if not v['ok']:
                continue
            shutil.copy2(os.path.join(OUTDIR, k), v['disk'])
            copied += 1
        print(f"\nAPPLIED: {copied} baked SHPs copied over originals.")
    else:
        print("\n(dry-run; pass --apply to write)")

if __name__ == '__main__':
    main()
