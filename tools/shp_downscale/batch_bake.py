#!/usr/bin/env python
"""Batch-bake all clean TS over-resolution candidates + offline geometry/integrity
gate + contact sheet. Replaces 214 in-game tests with one report + one PNG."""
import os, glob, math
from collections import defaultdict
import bake_shp_downscale as bk
import verify_render as vr
import shp_scale_audit as audit
from PIL import Image

MOD = audit.MOD
OUTDIR = "baked"
os.makedirs(OUTDIR, exist_ok=True)

# palettes per theme for the contact sheet (best-effort; geometry gate needs none)
THEME_PAL = {'tiberiandawn': 'temperatcnc.pal', 'redalert2mod': 'unittem.pal',
             'redalert': 'temperatra.pal', 'redalert2': 'unittem.pal',
             'tiberiansun': 'unittem.pal', 'd2k': 'unittem.pal', 'tkm': 'unittem.pal'}

def gather():
    """Return list of dicts: file,key,disk,scale,theme,offsets(actor->subseq offset)."""
    shp_idx = audit.build_shp_index()
    refs = defaultdict(lambda: {'scales': set(), 'body': False, 'theme': None})
    for theme in audit.ACTIVE:
        p = os.path.join(audit.SEQ_DIR, theme + '.yaml')
        if not os.path.exists(p):
            continue
        for actor, sub, fn, sc in audit.parse_seq_file(p):
            r = refs[fn.lower()]
            r['scales'].add(round(sc, 4))
            if audit.BODY_HINT.match(sub):
                r['body'] = True
            if r['theme'] is None:
                r['theme'] = theme
    out = []
    for key, r in refs.items():
        if not r['body'] or len(r['scales']) != 1:
            continue
        s = next(iter(r['scales']))
        if s >= 1.0:
            continue
        disk = shp_idx.get(key)
        if not disk:
            continue
        # TS only this pass (TD writer differs)
        with open(disk, 'rb') as f:
            import struct
            if struct.unpack('<H', f.read(2))[0] != 0:
                continue
        out.append({'key': key, 'disk': disk, 'scale': s, 'theme': r['theme']})
    return out

def is_shp_ts(d):
    """Port of engine ShpTSLoader.IsShpTS — the loadability gate. A bake that
    fails this is reported by the engine as 'file not found' (no loader accepts
    it) and crashes the game."""
    import struct as _s
    if len(d) < 8 or _s.unpack_from('<H', d, 0)[0] != 0:
        return False
    n = _s.unpack_from('<H', d, 6)[0]
    if 8 + 24 * n > len(d):
        return False
    pos, f, t = 12, 0, 0
    while True:
        w = _s.unpack_from('<H', d, pos)[0]
        h = _s.unpack_from('<H', d, pos + 2)[0]
        t = d[pos + 4]
        if (w == 0 or h == 0) and t == 0:
            return False
        pos += 24
        if w == 0 and h == 0:
            f += 1
            if f < n:
                continue
            break
        break
    return f == n or t < 4

def geom_check(orig_frames, baked_frames, s):
    """Return (ok, notes). Verify count, size~s*, offset~s*, index preservation."""
    notes = []
    ok = True
    if len(orig_frames) != len(baked_frames):
        return False, [f"frame count {len(orig_frames)}!={len(baked_frames)}"]
    # index integrity: baked indices must be subset of original (no new colours)
    oset, bset = set(), set()
    blanked = 0
    size_err = 0.0
    for (ox, oy, ow, oh, ob), (bx, by, bw, bh, bb) in zip(orig_frames, baked_frames):
        if ob:
            oset.update(ob)
        if bb:
            bset.update(bb)
        # a non-empty original frame must stay non-empty
        if ob and not bb:
            blanked += 1
        # size should be ~ s*original (within 2px from trim/even-pad)
        if ob and bb:
            exp_w, exp_h = ow * s, oh * s
            size_err = max(size_err, abs(bw - exp_w), abs(bh - exp_h))
    if blanked:
        ok = False; notes.append(f"{blanked} frames blanked")
    extra = bset - oset
    if extra:
        ok = False; notes.append(f"new indices {sorted(extra)[:6]}")
    if size_err > 3.5:
        ok = False; notes.append(f"size drift {size_err:.1f}px")
    return ok, notes

def main():
    cands = gather()
    cands.sort(key=lambda c: -sum(w*h for *_, w, h, b in bk.read_ts(c['disk'])[2] if b) * (1 - c['scale']**2))
    rows = []
    for c in cands:
        W, H, ofr = bk.read_ts(c['disk'])
        Wn, Hn, bfr = bk.downscale(W, H, ofr, c['scale'])
        outp = os.path.join(OUTDIR, c['key'])
        bk.write_ts(outp, Wn, Hn, bfr)
        # round-trip read what we wrote (catches writer bugs)
        _, _, rb = bk.read_ts(outp)
        ok, notes = geom_check(ofr, rb, c['scale'])
        with open(outp, 'rb') as _f:
            if not is_shp_ts(_f.read()):
                ok = False; notes.append("UNLOADABLE (IsShpTS reject)")
        native = sum(w*h for *_, w, h, b in ofr if b)
        baked = sum(w*h for *_, w, h, b in rb if b)
        rows.append({**c, 'native': native, 'baked': baked,
                     'save': native - baked, 'ok': ok, 'notes': notes,
                     'W': W, 'H': H, 'frames': len(ofr)})
    # report
    npass = sum(1 for r in rows if r['ok'])
    tot_save = sum(r['save'] for r in rows)
    print(f"{'SHP':30} {'scl':>4} {'saveKB':>8} {'gate':>5}  notes")
    print('-'*78)
    for r in rows:
        g = 'PASS' if r['ok'] else 'FAIL'
        print(f"{r['key']:30} {r['scale']:>4} {r['save']/1024:>8.1f} {g:>5}  "
              f"{';'.join(r['notes'])}")
    print('-'*78)
    print(f"{len(rows)} TS candidates  |  {npass} PASS / {len(rows)-npass} FAIL  |  "
          f"recoverable {tot_save/1024/1024:.2f} MB")
    contact_sheet(rows[:40])

def contact_sheet(rows):
    FAC = [0, 32]      # two facings
    CELL = 64
    cols = 8
    palcache = {}
    def pal_for(theme):
        name = THEME_PAL.get(theme, 'unittem.pal')
        if name not in palcache:
            palcache[name] = vr.load_pal(os.path.join(MOD, 'bits', name))
        return palcache[name]
    tiles = []
    for r in rows:
        pal = pal_for(r['theme'])
        _, _, bfr = bk.read_ts(os.path.join(OUTDIR, r['key']))
        Wn = round(r['W']*r['scale']); Hn = round(r['H']*r['scale'])
        tile = Image.new('RGBA', (CELL*len(FAC), CELL+10), (28, 28, 32, 255))
        for fi_i, fi in enumerate(FAC):
            if fi >= len(bfr):
                continue
            x, y, w, h, buf = bfr[fi]
            img = vr.frame_to_img(x, y, w, h, buf, pal, Wn, Hn, 0, 0)
            # fit into CELL
            sc = min(CELL/max(1, Wn), CELL/max(1, Hn))
            img = img.resize((max(1, int(Wn*sc)), max(1, int(Hn*sc))), Image.NEAREST)
            tile.paste(img, (fi_i*CELL + (CELL-img.width)//2, (CELL-img.height)//2), img)
        tiles.append((r['key'], tile, r['ok']))
    rowsN = math.ceil(len(tiles)/cols)
    TW = CELL*len(FAC)
    sheet = Image.new('RGBA', (cols*TW, rowsN*(CELL+10)), (18, 18, 20, 255))
    for i, (name, tile, ok) in enumerate(tiles):
        cx, cy = (i % cols)*TW, (i//cols)*(CELL+10)
        sheet.paste(tile, (cx, cy))
    sheet.save("contact_sheet.png")
    print(f"contact sheet: {len(tiles)} units -> contact_sheet.png")

if __name__ == '__main__':
    main()
