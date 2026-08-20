#!/usr/bin/env python3
"""Build sidebar-syndicate.png: the Latin Syndicate sidebar sheet.

Starts from the RA2 Soviet sheet (Syndicate previously shared it via
FactionSuffix-syndicate: ra2soviet) and replaces the `insignia` region
(290,67,222,222 -> the no-radar / low-power radar art) with the Syndicate's
skull-with-crossed-AK-and-machete emblem.

The emblem is composited over a dark radial vignette that blends into the
near-black radar frame. A white glow halo is rendered behind the art so its
black weapons stay readable against the dark backdrop.

Source art lives at tools/syndicate-skull.png (RGBA, transparent background).
Re-run: python tools/make_syndicate_insignia.py
"""
import math
import os
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "tools", "syndicate-skull.png")
SHEET = os.path.join(ROOT, "mods", "cameo", "uibits", "sidebar-ra2soviet.png")
OUT = os.path.join(ROOT, "mods", "cameo", "uibits", "sidebar-syndicate.png")
RX, RY, RW, RH = 290, 67, 222, 222
MARGIN = 10


def vignette(center, edge):
    bg = Image.new("RGB", (RW, RH), edge)
    px = bg.load()
    cx, cy = RW / 2, RH / 2
    maxd = math.hypot(cx, cy)
    for y in range(RH):
        for x in range(RW):
            d = math.hypot(x - cx, y - cy) / maxd
            px[x, y] = tuple(int(center[i] * (1 - d) + edge[i] * d) for i in range(3))
    return bg.convert("RGBA")


def main():
    art = Image.open(ART).convert("RGBA")

    # fit within the region (small margin), keep aspect, centre
    mw, mh = RW - 2 * MARGIN, RH - 2 * MARGIN
    s = min(mw / art.width, mh / art.height)
    nw, nh = int(art.width * s), int(art.height * s)
    art = art.resize((nw, nh), Image.LANCZOS)
    ox, oy = (RW - nw) // 2, (RH - nh) // 2

    bg = vignette((28, 28, 32), (2, 2, 2))

    # white glow halo behind the art so black weapons read on the dark backdrop
    glow = Image.new("RGBA", (RW, RH), (0, 0, 0, 0))
    halo = Image.new("RGBA", art.size, (255, 255, 255, 255))
    halo.putalpha(art.split()[3])
    glow.alpha_composite(halo, (ox, oy))
    glow = glow.filter(ImageFilter.GaussianBlur(9))
    for _ in range(2):
        bg.alpha_composite(glow)

    bg.alpha_composite(art, (ox, oy))

    sheet = Image.open(SHEET).convert("RGBA")
    sheet.paste(bg, (RX, RY))  # opaque replace of the insignia region
    sheet.save(OUT)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
