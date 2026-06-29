#!/usr/bin/env python
"""SHP over-resolution audit.

For every SHP referenced at a fractional Scale by an ACTIVE sequence file,
report native texture-sheet bytes vs Scale-adjusted bytes (the recoverable
VRAM if the art were re-baked at its display scale and Scale dropped to 1).

Keyed by SHP FILE: a sheet is uploaded once regardless of how many sequences
use it, so the prize is per-file. Only loose bits/ SHPs are considered
(custom art we can re-bake); base-game .mix art is out of scope.

Remap flag is best-effort: counts pixels in the player-colour index range
(TS unit palette 16-31, TD/RA 80-95). Nonzero => riskier bake, verify in-game.
"""
import os, struct, sys, glob, re
from collections import defaultdict

MOD = r"C:\Users\Blackrobe\repo\Cameo-mod\mods\cameo"
SEQ_DIR = os.path.join(MOD, "sequences")
BITS = os.path.join(MOD, "bits")

# ---- active sequence files (uncommented cameo|sequences/*.yaml in mod.yaml) ----
ACTIVE = """misc shared_effects decorations funpark civilian campaign
tiberiandawn redalert redalert2 redalert2mod d2k starcraft warcraft2 warcraft1
tiberiansun outpost2 tkm""".split()

# body sequences whose art is the unit sprite (vs muzzle/effects we don't rebake)
BODY_HINT = re.compile(r'^(idle|turret|stand|move|run|walk|crawl|aim|fire|'
                       r'death|die|dead|make|deploy|undeploy|open|active|'
                       r'damaged|husk|wreck|build|spawn)', re.I)

# ---------------- SHP parsers ----------------
def parse_shp(path):
    """Return (frames, fmt) where frames = list of (w,h,pixel_bytes_or_None).
    pixel_bytes is the decoded index buffer (len w*h) or None for empty frames."""
    with open(path, 'rb') as f:
        data = f.read()
    if len(data) < 8:
        return None, None
    first = struct.unpack_from('<H', data, 0)[0]
    if first == 0:
        return parse_ts(data), 'TS'
    return parse_td(data), 'TD'

def parse_ts(data):
    _, w, h, n = struct.unpack_from('<HHHH', data, 0)
    frames = []
    off = 8
    for i in range(n):
        x, y, fw, fh, fmt = struct.unpack_from('<HHHHB', data, off)
        foff = struct.unpack_from('<I', data, off + 20)[0]
        off += 24
        dw = fw + (fw & 1)
        dh = fh + (fh & 1)
        if foff == 0:
            frames.append((dw, dh, None))
            continue
        buf = bytearray(dw * dh)
        p = foff
        try:
            if fmt == 3:
                for j in range(fh):
                    ln = struct.unpack_from('<H', data, p)[0] - 2
                    p += 2
                    decode_rle_zero(data[p:p+ln], buf, dw * j)
                    p += ln
            elif fmt == 2:
                for j in range(fh):
                    ln = struct.unpack_from('<H', data, p)[0] - 2
                    p += 2
                    buf[dw*j:dw*j+ln] = data[p:p+ln]
                    p += ln
            else:
                for j in range(fh):
                    buf[dw*j:dw*j+fw] = data[p:p+fw]
                    p += fw
        except Exception:
            pass
        frames.append((dw, dh, bytes(buf)))
    return frames

def decode_rle_zero(src, dst, start):
    di = start
    i = 0
    while i < len(src):
        b = src[i]; i += 1
        if b != 0:
            dst[di] = b; di += 1
        else:
            cnt = src[i] if i < len(src) else 0; i += 1
            di += cnt

def parse_td(data):
    n = struct.unpack_from('<H', data, 0)[0]
    w, h = struct.unpack_from('<HH', data, 6)
    # TD frames are all full WxH; we don't need pixels for byte count, but
    # decode index 0-fill estimate via offsets table is complex. For the audit
    # the sheet cost is n * w * h regardless. Sample pixels for remap flag only
    # if cheaply decodable; otherwise mark unknown.
    return [(w, h, None)] * n

# ---------------- minimal sequence YAML reader ----------------
def indent(line):
    return len(line) - len(line.lstrip('\t'))

def parse_seq_file(path):
    """Yield (actor, subseq, filename, scale). Handles Defaults + per-seq
    Filename/Scale overrides via tab indentation. Inherits is ignored (first
    pass); body units almost always define their own Defaults."""
    with open(path, encoding='utf-8', errors='replace') as f:
        lines = [l.rstrip('\n') for l in f]
    actor = None
    cur = None            # current subsequence name (incl 'Defaults')
    d_file = d_scale = None
    subs = {}             # name -> {file,scale}
    def flush():
        if actor is None:
            return
        for name, props in subs.items():
            if name == 'Defaults':
                continue
            fn = props.get('file', d_file)
            sc = props.get('scale', d_scale if d_scale is not None else 1.0)
            if fn:
                yield_buf.append((actor, name, fn, sc))
    yield_buf = []
    for raw in lines:
        if not raw.strip() or raw.lstrip().startswith('#'):
            continue
        ind = indent(raw)
        s = raw.strip()
        if ind == 0 and s.endswith(':'):
            # new actor
            flush()
            actor = s[:-1]
            cur = None; d_file = d_scale = None; subs = {}
            continue
        if actor is None:
            continue
        if ind == 1 and s.endswith(':'):
            cur = s[:-1]
            subs.setdefault(cur, {})
            continue
        # property line (ind >= 2 usually)
        if ':' in s:
            k, _, v = s.partition(':')
            k = k.strip(); v = v.strip()
            tgt_default = (cur == 'Defaults')
            if k == 'Filename':
                if tgt_default: d_file = v
                elif cur: subs[cur]['file'] = v
            elif k == 'Scale':
                try: sv = float(v)
                except ValueError: continue
                if tgt_default: d_scale = sv
                elif cur: subs[cur]['scale'] = sv
            elif k == 'Inherits' and tgt_default:
                pass
    flush()
    return yield_buf

# ---------------- disk index ----------------
def build_shp_index():
    idx = {}
    for p in glob.glob(os.path.join(BITS, '**', '*.shp'), recursive=True):
        idx.setdefault(os.path.basename(p).lower(), p)
    return idx

# ---------------- main ----------------
def main():
    shp_idx = build_shp_index()
    # shp filename(lower) -> {scales:set, actors:set, body:bool}
    refs = defaultdict(lambda: {'scales': set(), 'actors': set(), 'body': False})
    for theme in ACTIVE:
        path = os.path.join(SEQ_DIR, theme + '.yaml')
        if not os.path.exists(path):
            continue
        for actor, sub, fn, sc in parse_seq_file(path):
            key = fn.lower()
            r = refs[key]
            r['scales'].add(round(sc, 4))
            r['actors'].add(actor)
            if BODY_HINT.match(sub):
                r['body'] = True

    rows = []
    for key, r in refs.items():
        # candidate = displayed only at a single fractional scale, is body art,
        # exists as a loose file we can rebake
        if not r['body']:
            continue
        scales = r['scales']
        if len(scales) != 1:
            continue
        s = next(iter(scales))
        if s >= 1.0:
            continue
        disk = shp_idx.get(key)
        if not disk:
            continue
        frames, fmt = parse_shp(disk)
        if not frames:
            continue
        native = sum(w*h for (w, h, _) in frames)
        baked = int(native * s * s)
        saving = native - baked
        # remap flag
        remap = remap_count(frames, fmt)
        rows.append({
            'file': key, 'fmt': fmt, 'frames': len(frames), 'scale': s,
            'native': native, 'baked': baked, 'saving': saving,
            'remap': remap, 'actors': len(r['actors']),
        })

    rows.sort(key=lambda x: -x['saving'])
    tot_native = sum(r['native'] for r in rows)
    tot_saving = sum(r['saving'] for r in rows)
    print(f"{'SHP':32} {'fmt':3} {'frm':>4} {'scl':>4} {'nativeKB':>9} "
          f"{'savingKB':>9} {'remap':>6}  actors")
    print('-'*92)
    for r in rows:
        flag = '' if r['fmt'] == 'TD' and r['remap'] == 'n/a' else r['remap']
        print(f"{r['file']:32} {r['fmt']:3} {r['frames']:>4} {r['scale']:>4} "
              f"{r['native']/1024:>9.1f} {r['saving']/1024:>9.1f} "
              f"{str(r['remap']):>6}  {r['actors']}")
    print('-'*92)
    print(f"candidates: {len(rows)}   "
          f"native total: {tot_native/1024/1024:.2f} MB   "
          f"recoverable: {tot_saving/1024/1024:.2f} MB "
          f"({100*tot_saving/tot_native:.0f}%)" if tot_native else "no candidates")

def remap_count(frames, fmt):
    if fmt == 'TS':
        lo, hi = 16, 31
    else:
        return 'n/a'  # TD pixels not decoded in this pass
    c = 0
    for (_, _, buf) in frames:
        if not buf:
            continue
        for b in buf:
            if lo <= b <= hi:
                c += 1
    return c

if __name__ == '__main__':
    main()
