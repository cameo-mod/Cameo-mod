#!/usr/bin/env python
"""Palette-safe SHP(TS) downscaler.

OpenRA minifies world sprite sheets with GL_NEAREST, so a CPU nearest-neighbour
downscale of an over-resolution SHP reproduces exactly what the GPU draws today
when the sequence has Scale<1 -- but precomputed, so the sheet (VRAM) shrinks by
scale^2 and Scale can drop to 1. Because we only subsample palette indices and
never blend them, player-colour (remap) and shadow indices survive untouched:
safe for ANY unit, not just zero-remap ones.

Method (uniform-scale, the d2k anti-shimmer lesson): composite each frame onto a
common canvas at its native (x,y), nearest-downscale the WHOLE canvas by the same
mapping, then re-trim to bbox. Identical mapping every frame => no per-frame
sub-pixel drift.

Usage: python bake_shp_downscale.py <in.shp> <scale> <out.shp>
"""
import struct, sys

def read_ts(path):
    with open(path, 'rb') as f:
        data = f.read()
    assert struct.unpack_from('<H', data, 0)[0] == 0, "not SHP(TS)"
    _, W, H, n = struct.unpack_from('<HHHH', data, 0)
    frames = []
    off = 8
    for i in range(n):
        x, y, fw, fh, fmt = struct.unpack_from('<HHHHB', data, off)
        foff = struct.unpack_from('<I', data, off + 20)[0]
        off += 24
        dw, dh = fw + (fw & 1), fh + (fh & 1)
        if foff == 0:
            frames.append((x, y, 0, 0, None)); continue
        buf = bytearray(dw * dh)
        p = foff
        if fmt == 3:
            for j in range(fh):
                ln = struct.unpack_from('<H', data, p)[0] - 2; p += 2
                _rle_decode(data[p:p+ln], buf, dw * j); p += ln
        elif fmt == 2:
            for j in range(fh):
                ln = struct.unpack_from('<H', data, p)[0] - 2; p += 2
                buf[dw*j:dw*j+ln] = data[p:p+ln]; p += ln
        else:
            for j in range(fh):
                buf[dw*j:dw*j+fw] = data[p:p+fw]; p += fw
        # store using padded dims dw,dh (extra col/row are transparent 0)
        frames.append((x, y, dw, dh, bytes(buf)))
    return W, H, frames

def _rle_decode(src, dst, start):
    di, i = start, 0
    while i < len(src):
        b = src[i]; i += 1
        if b: dst[di] = b; di += 1
        else:
            c = src[i] if i < len(src) else 0; i += 1
            di += c

def _rle_encode_row(row):
    out = bytearray(); i, w = 0, len(row)
    while i < w:
        b = row[i]
        if b:
            out.append(b); i += 1
        else:
            j = i
            while j < w and row[j] == 0:
                j += 1
            run = j - i
            while run > 0:
                k = min(run, 255)
                out.append(0); out.append(k)
                run -= k
            i = j
    return out

def downscale(W, H, frames, s):
    """Composite each frame on the WxH canvas, nearest-downscale to W'xH', trim."""
    Wn, Hn = max(1, round(W * s)), max(1, round(H * s))
    # precompute source index per dest pixel (shared by every frame)
    sx = [min(W - 1, int((ox + 0.5) / s)) for ox in range(Wn)]
    sy = [min(H - 1, int((oy + 0.5) / s)) for oy in range(Hn)]
    out = []
    for (fx, fy, fw, fh, buf) in frames:
        if buf is None:
            out.append((0, 0, 0, 0, None)); continue
        # paint frame onto full canvas (row-major, index 0 = transparent)
        canvas = bytearray(W * H)
        for row in range(fh):
            cy = fy + row
            if 0 <= cy < H:
                seg = buf[row*fw:(row+1)*fw]
                base = cy * W + fx
                # clip to canvas width
                lo = max(0, -fx); hi = min(fw, W - fx)
                if hi > lo:
                    canvas[base+lo:base+hi] = seg[lo:hi]
        # nearest downscale
        small = bytearray(Wn * Hn)
        for oy in range(Hn):
            srow = sy[oy] * W
            drow = oy * Wn
            for ox in range(Wn):
                small[drow + ox] = canvas[srow + sx[ox]]
        # crop to the frame's SCALED NATIVE box (not content bbox): this makes
        # baked size/offset deterministically s*native, preserving the engine's
        # scale*Offset / scale*Size geometry contract exactly. Keeps the shared
        # downscale grid (anti-shimmer) since we crop from the common `small`.
        x0 = max(0, min(Wn, round(fx * s)))
        y0 = max(0, min(Hn, round(fy * s)))
        x1 = max(0, min(Wn, round((fx + fw) * s)))
        y1 = max(0, min(Hn, round((fy + fh) * s)))
        tw, th = max(1, x1 - x0), max(1, y1 - y0)
        # if the scaled box holds no content, frame collapsed (flagged upstream)
        ew, eh = tw + (tw & 1), th + (th & 1)
        tbuf = bytearray(ew * eh)
        any_px = False
        for ry in range(th):
            sy_ = y0 + ry
            if sy_ >= Hn:
                break
            sbase = sy_ * Wn + x0
            seg = small[sbase:sbase + tw]
            if not any_px and any(seg):
                any_px = True
            tbuf[ry*ew:ry*ew+tw] = seg
        if not any_px:
            out.append((0, 0, 0, 0, None)); continue
        out.append((x0, y0, ew, eh, bytes(tbuf)))
    return Wn, Hn, out

def write_ts(path, W, H, frames):
    n = len(frames)
    headers = bytearray()
    body = bytearray()
    data_start = 8 + 24 * n
    for (x, y, w, h, buf) in frames:
        if buf is None or w == 0 or h == 0:
            # empty frames MUST carry a non-zero format byte: IsShpTS rejects
            # the whole file if it scans a zero-sized frame with type==0.
            headers += struct.pack('<HHHHB', x, y, 0, 0, 1)
            headers += b'\x00' * 11
            headers += struct.pack('<I', 0)
            continue
        foff = data_start + len(body)
        # encode rows as format 3 (RLE-zero)
        for row in range(h):
            enc = _rle_encode_row(buf[row*w:(row+1)*w])
            body += struct.pack('<H', len(enc) + 2)
            body += enc
        headers += struct.pack('<HHHHB', x, y, w, h, 3)
        headers += b'\x00' * 11
        headers += struct.pack('<I', foff)
    with open(path, 'wb') as f:
        f.write(struct.pack('<HHHH', 0, W, H, n))
        f.write(headers)
        f.write(body)

def main():
    inp, s, outp = sys.argv[1], float(sys.argv[2]), sys.argv[3]
    W, H, frames = read_ts(inp)
    Wn, Hn, out = downscale(W, H, frames, s)
    write_ts(outp, Wn, Hn, out)
    import os
    a, b = os.path.getsize(inp), os.path.getsize(outp)
    native = sum(w*h for (_,_,w,h,buf) in frames if buf)
    baked = sum(w*h for (_,_,w,h,buf) in out if buf)
    print(f"canvas {W}x{H} -> {Wn}x{Hn}   frames {len(frames)}")
    print(f"sheet bytes (VRAM) {native/1024:.1f}KB -> {baked/1024:.1f}KB  "
          f"({100*(1-baked/native):.0f}% saved)")
    print(f"disk {a/1024:.1f}KB -> {b/1024:.1f}KB")

if __name__ == '__main__':
    main()
