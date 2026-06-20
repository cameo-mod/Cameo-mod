# Bake a D2k windtrap "zap" overlay into a glow-only, apex-aligned, blip-free PNG spritesheet.
#
# Why: imported D2k zap art is a tan prism shell + teal energy bands, rendered at Scale 1.5.
# The building body ALREADY draws the prism shell + player colour, so the overlay must contribute
# ONLY the teal glow (else the redundant shell mis-registers and "shimmers"). On top of that the
# glow content can drift 1 native px on a frame (oscillation reads as jitter), and stray pixels on
# the "off" frames blip during the idle gap. This tool fixes all three.
#
# Pipeline (Python only, no engine rebuild):
#   1. Engine-bake the native frames at scale 1.0 --preserve-offsets to C:\temp\<house>_zap_native.png:
#        dotnet engine/bin/OpenRA.Utility.dll cameo --bake-sequence power.<house> idle-zaps 1.0 \
#          --preserve-offsets --out C:\temp\<house>_zap_native.png
#      (cwd=engine, env MOD_SEARCH_PATHS=<repo>\mods, ENGINE_DIR=..)
#   2. GLOW colour-mask: keep teal energy (B>R+15 & G>R & B>80); drops tan shell / brown / green
#      player-colour slots, so the body keeps its structure + player colour.
#   3. APEX-ALIGN: pin each active frame's glow apex (first row with >=3 glow px) to a common top in
#      native integer px BEFORE scaling -> removes the 1px content bobble.
#   4. UNIFORM 1.5x NEAREST upscale (identical transform every frame -> no sub-pixel shimmer).
#   5. BLANK off-frames (<ACTIVE_MIN glow px) fully transparent -> no idle 1px blip.
#   6. Save with FrameSize/FrameAmount tEXt chunks (OpenRA PngSheetLoader slices on those).
#
# Usage:  python tools/bake_d2k_zap.py <house> [--write]
#   without --write: only writes a preview to C:\temp\aligned_<house>.png
#   with    --write: also writes mods/cameo/bits/d2k/power_<house>_zaps.png
# Confirmed live (2026-06-21): atreides Offset 7,-2 (cell 63x82), harkonnen 4,-6 (58x82),
# ordos 6,-5 (60x81). Offsets are eyeball-tuned per house after wiring; start from the engine
# bake's printed offset *1.5 and nudge 1-2px.

from PIL import Image
from PIL.PngImagePlugin import PngInfo
import numpy as np, sys

HOUSE = sys.argv[1]
WRITE = "--write" in sys.argv
NATIVE = rf"C:\temp\{HOUSE}_zap_native.png"
OUT = rf"C:\Users\Blackrobe\repo\Cameo-mod\mods\cameo\bits\d2k\power_{HOUSE}_zaps.png"
N = 10
SCALE = 1.5
ACTIVE_MIN = 20  # frames below this many glow px are the "off" part of the cycle -> blank them

im = Image.open(NATIVE).convert("RGBA")
W, H = im.size
fw, fh = W // N, H
arr = np.asarray(im).astype(np.int32)
frames = [arr[:, i * fw:(i + 1) * fw, :].copy() for i in range(N)]


def glowmask(f):
    R, G, B, A = f[:, :, 0], f[:, :, 1], f[:, :, 2], f[:, :, 3]
    return (A > 0) & (B > R + 15) & (G > R) & (B > 80)


# pass 1: glow-trim + measure apex (first row with >=3 glow px) and centroid x
trimmed, kept, tops, cxs = [], [], [], []
for f in frames:
    m = glowmask(f)
    g = f.copy(); g[~m, 3] = 0
    trimmed.append(g); kept.append(int(m.sum()))
    if m.sum() >= ACTIVE_MIN:
        top = int(np.argmax(m.sum(axis=1) >= 3))
        ys, xs = np.where(m)
        tops.append(top); cxs.append(xs.mean())
    else:
        tops.append(None); cxs.append(None)

ref_top = min(t for t in tops if t is not None)
ref_cx = float(np.mean([c for c in cxs if c is not None]))
print(HOUSE, "native", fw, "x", fh, "| kept", kept)
print("  tops", tops, "-> ref_top", ref_top, "| ref_cx %.1f" % ref_cx)


def shift(a, dy, dx):
    out = np.zeros_like(a)
    h, w = a.shape[:2]
    ys0, ys1 = max(0, dy), min(h, h + dy); xs0, xs1 = max(0, dx), min(w, w + dx)
    sy0, sy1 = max(0, -dy), min(h, h - dy); sx0, sx1 = max(0, -dx), min(w, w - dx)
    out[ys0:ys1, xs0:xs1] = a[sy0:sy1, sx0:sx1]
    return out


# pass 2: align active frames to the common anchor; BLANK off-frames
aligned = []
for i, g in enumerate(trimmed):
    if tops[i] is None:
        aligned.append(np.zeros_like(g).astype(np.uint8)); continue  # off-frame: no stray blip
    dy = ref_top - tops[i]
    dx = int(round(ref_cx - cxs[i]))
    aligned.append(shift(g, dy, dx).astype(np.uint8))
    if dy or dx:
        print(f"  f{i}: shift dy={dy} dx={dx}")

sW, sH = round(fw * SCALE), round(fh * SCALE)
scaled = [np.asarray(Image.fromarray(t, "RGBA").resize((sW, sH), Image.NEAREST)) for t in aligned]


def up(a, k): return np.kron(a, np.ones((k, k, 1), dtype=a.dtype))
def over(s, bg):
    base = np.full((sH, sW, 3), bg, np.float64); a = s[:, :, 3:4] / 255.0
    return (s[:, :, :3] * a + base * (1 - a)).astype(np.uint8)
def strip(bg):
    ps = [up(over(scaled[i], bg), 4) for i in range(N)]
    H2, W2 = ps[0].shape[:2]; c = np.full((H2, N * (W2 + 2), 3), bg, np.uint8)
    for j, p in enumerate(ps): c[:, j * (W2 + 2):j * (W2 + 2) + W2] = p
    return c
Image.fromarray(np.vstack([strip(150), strip(60)]), "RGB").save(rf"C:\temp\aligned_{HOUSE}.png")
print("  cell", sW, "x", sH, "-> preview C:\\temp\\aligned_%s.png" % HOUSE)

if WRITE:
    sheet = np.zeros((sH, sW * N, 4), dtype=np.uint8)
    for i, s in enumerate(scaled): sheet[:, i * sW:(i + 1) * sW] = s
    meta = PngInfo(); meta.add_text("FrameSize", f"{sW},{sH}"); meta.add_text("FrameAmount", str(N))
    Image.fromarray(sheet, "RGBA").save(OUT, pnginfo=meta)
    print(f"  WROTE {OUT} FrameSize={sW},{sH}")
