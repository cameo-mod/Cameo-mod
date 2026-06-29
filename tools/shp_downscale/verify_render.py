#!/usr/bin/env python
import struct, sys
from PIL import Image
import bake_shp_downscale as b

BITS = r"C:\Users\Blackrobe\repo\Cameo-mod\mods\cameo\bits"

def load_pal(path):
    with open(path, 'rb') as f:
        d = f.read()
    pal = []
    mx = max(d[:768]) if len(d) >= 768 else 255
    scale = 4 if mx <= 63 else 1
    for i in range(256):
        r, g, b_, = d[i*3], d[i*3+1], d[i*3+2]
        pal.append((min(255, r*scale), min(255, g*scale), min(255, b_*scale)))
    return pal

def frame_to_img(x, y, w, h, buf, pal, canvasW, canvasH, ox, oy):
    """Place a frame's pixels onto a (canvasW,canvasH) RGBA image at (ox+x,oy+y)."""
    img = Image.new('RGBA', (canvasW, canvasH), (0, 0, 0, 0))
    if buf is None or w == 0:
        return img
    px = img.load()
    for j in range(h):
        for i in range(w):
            idx = buf[j*w + i]
            if idx == 0:
                continue
            cx, cy = ox + x + i, oy + y + j
            if 0 <= cx < canvasW and 0 <= cy < canvasH:
                r, g, bl = pal[idx]
                a = 110 if idx in (1, 4) else 255   # shadow indices dim
                px[cx, cy] = (r, g, bl, a)
    return img

def montage(shp, frames_idx, pal, scale_up, title):
    W, H, frames = b.read_ts(shp)
    cells = []
    for fi in frames_idx:
        x, y, w, h, buf = frames[fi]
        img = frame_to_img(x, y, w, h, buf, pal, W, H, 0, 0)
        cells.append(img)
    cw = W * scale_up
    ch = H * scale_up
    out = Image.new('RGBA', (cw * len(cells), ch), (40, 40, 40, 255))
    for i, c in enumerate(cells):
        c = c.resize((cw, ch), Image.NEAREST)
        out.paste(c, (i*cw, 0), c)
    return out, (W, H)

def main():
    palpath = sys.argv[1] if len(sys.argv) > 1 else "temperatcnc.pal"
    import os
    pal = load_pal(os.path.join(BITS, palpath))
    orig = os.path.join(BITS, "td", "gdimammoth3.shp")
    baked = "gdimammoth3_baked.shp"
    idxs = [0, 16, 32, 64, 80]   # idle facings + turret frames
    o, (ow, oh) = montage(orig, idxs, pal, 1, "orig")
    bk, (bw, bh) = montage(baked, idxs, pal, 1, "baked")
    factor = ow / bw
    out = Image.new('RGBA', (o.width, o.height + bk.height + 4), (20, 20, 20, 255))
    out.paste(o, (0, 0))
    bk2 = bk.resize((o.width, int(bk.height * (o.width / bk.width))), Image.NEAREST)
    out.paste(bk2, (0, o.height + 4))
    out.save("compare_mammoth.png")
    print(f"orig canvas {ow}x{oh}  baked {bw}x{bh}  factor {factor:.2f}")
    print("saved compare_mammoth.png  (top row=original, bottom row=baked, nearest-upscaled to match)")

if __name__ == '__main__':
    main()
