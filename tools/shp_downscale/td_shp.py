#!/usr/bin/env python
"""SHP(TD) read (LCW + XOR-delta decode) / trim / write (LCW encode), ports of
OpenRA's ShpTDLoader + LCWCompression + XORDeltaCompression.

Note: TD frames are stored full WxH on disk but the engine TRIMS them in VRAM,
so the meaningful (VRAM) size of a frame is its trimmed bbox, not W*H."""
import struct

def _u16(d, o): return d[o] | (d[o+1] << 8)
def _u32(d, o): return d[o] | (d[o+1]<<8) | (d[o+2]<<16) | (d[o+3]<<24)

def lcw_decode(src, dest, srcoff):
    i, di, n = srcoff, 0, len(dest)
    while True:
        b = src[i]; i += 1
        if (b & 0x80) == 0:
            second = src[i]; i += 1
            count = ((b & 0x70) >> 4) + 3
            rpos = ((b & 0xf) << 8) + second
            if di + count > n: return di
            s = di - rpos
            for k in range(count):
                dest[di+k] = dest[di-1] if (di - s) == 1 else dest[s+k]
            di += count
        elif (b & 0x40) == 0:
            count = b & 0x3F
            if count == 0: return di
            dest[di:di+count] = src[i:i+count]; i += count; di += count
        else:
            c3 = b & 0x3F
            if c3 == 0x3E:
                count = src[i] | (src[i+1] << 8); i += 2
                color = src[i]; i += 1
                for _ in range(count):
                    dest[di] = color; di += 1
            else:
                count = (src[i] | (src[i+1] << 8)) if c3 == 0x3F else c3 + 3
                if c3 == 0x3F: i += 2
                sidx = src[i] | (src[i+1] << 8); i += 2
                for _ in range(count):
                    dest[di] = dest[sidx]; sidx += 1; di += 1

def xor_decode(src, dest, srcoff):
    i, di = srcoff, 0
    while True:
        b = src[i]; i += 1
        if (b & 0x80) == 0:
            count = b & 0x7F
            if count == 0:
                count = src[i]; i += 1
                val = src[i]; i += 1
                for _ in range(count):
                    dest[di] ^= val; di += 1
            else:
                for _ in range(count):
                    dest[di] ^= src[i]; i += 1; di += 1
        else:
            count = b & 0x7F
            if count == 0:
                count = src[i] | (src[i+1] << 8); i += 2
                if count == 0: return di
                if (count & 0x8000) == 0:
                    di += count & 0x7FFF
                elif (count & 0x4000) == 0:
                    for _ in range(count & 0x3FFF):
                        dest[di] ^= src[i]; i += 1; di += 1
                else:
                    val = src[i]; i += 1
                    for _ in range(count & 0x3FFF):
                        dest[di] ^= val; di += 1
            else:
                di += count

def read_td(path):
    with open(path, 'rb') as f:
        d = f.read()
    count = _u16(d, 0); W = _u16(d, 6); H = _u16(d, 8)
    headers = []
    pos = 14
    for _ in range(count):
        v = _u32(d, pos); ref_off = _u16(d, pos+4); ref_fmt = _u16(d, pos+6); pos += 8
        headers.append([v & 0xffffff, v >> 24, ref_off, ref_fmt, None])
    off_map = {h[0]: idx for idx, h in enumerate(headers)}

    def decode(idx):
        h = headers[idx]
        if h[4] is not None: return h[4]
        foff, fmt = h[0], h[1]
        buf = bytearray(W * H)
        if fmt == 0x80:
            lcw_decode(d, buf, foff)
        elif fmt == 0x20:
            buf[:] = decode(idx - 1); xor_decode(d, buf, foff)
        elif fmt == 0x40:
            buf[:] = decode(off_map[h[2]]); xor_decode(d, buf, foff)
        else:
            raise ValueError(f"bad TD format {fmt:#x}")
        h[4] = bytes(buf); return h[4]
    frames = [decode(k) for k in range(count)]
    return W, H, frames   # frames are FULL W*H index buffers

def trim(buf, W, H):
    top, bottom, left, right = H-1, 0, W-1, 0
    i = 0; found = False
    for y in range(H):
        row = buf[y*W:(y+1)*W]
        for x in range(W):
            if row[x]:
                found = True
                if y < top: top = y
                if y > bottom: bottom = y
                if x < left: left = x
                if x > right: right = x
    if not found:
        return (0, 0, 0, 0, None)
    tw, th = right-left+1, bottom-top+1
    if (tw - W) % 2 != 0:
        if left > 0: left -= 1
        else: right += 1
        tw += 1
    if (th - H) % 2 != 0:
        if top > 0: top -= 1
        else: bottom += 1
        th += 1
    tbuf = bytearray(tw*th)
    for y in range(th):
        s = (y+top)*W + left
        tbuf[y*tw:(y+1)*tw] = buf[s:s+tw]
    return (left, top, tw, th, bytes(tbuf))

# ---- LCW encode (port of LCWCompression.Encode) + TD writer ----
def _count_same(src, off, maxc):
    maxc = min(len(src) - off, maxc)
    if maxc <= 0: return 0
    first = src[off]; count = 1; o = off + 1
    while count < maxc and src[o] == first:
        count += 1; o += 1
    return count

def _write_copy(src, off, count, out):
    while count > 0:
        w = min(count, 0x3F)
        out.append(0x80 | w)
        out += src[off:off+w]
        count -= w; off += w

def lcw_encode(src):
    out = bytearray(); off = 0; n = len(src); bstart = 0
    while off < n:
        rc = _count_same(src, off, 0xFFFF)
        if rc >= 4:
            _write_copy(src, bstart, off - bstart, out)
            out.append(0xFE); out.append(rc & 0xFF); out.append((rc >> 8) & 0xFF)
            out.append(src[off]); off += rc; bstart = off
        else:
            off += 1
    _write_copy(src, bstart, off - bstart, out)
    out.append(0x80)
    return bytes(out)

def write_td(path, W, H, full_frames):
    comp = [lcw_encode(bytes(f)) for f in full_frames]
    data_off = 14 + (len(comp) + 2) * 8
    headers = bytearray(); body = bytearray(); off = data_off
    for f in comp:
        headers += struct.pack('<IHH', off | (0x80 << 24), 0, 0)
        off += len(f); body += f
    headers += struct.pack('<IHH', off, 0, 0)   # EOF header (format 0)
    headers += struct.pack('<IHH', 0, 0, 0)     # all-zero header
    with open(path, 'wb') as fh:
        fh.write(struct.pack('<HHHHHI', len(comp), 0, 0, W, H, 0))
        fh.write(headers); fh.write(body)
