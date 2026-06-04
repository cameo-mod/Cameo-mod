#!/usr/bin/env python3
"""Procedural RGBA smoke-puff sprite-sheet generator for Cameo-mod.

License-clean: generates original art from numpy fractal noise. Studies the
*technique* of good 2D smoke (soft per-pixel alpha, dark grey not black, a
rising/expanding billow that dissipates) without using anyone's assets.

Output is a single PNG sprite sheet (COLS x ROWS frames) that OpenRA loads via
the PngSheet format. Each particle plays the sheet once over its lifetime: the
puff is born small, grows + churns, then fades its alpha to zero.

Usage:
    python tools/gen_smoke.py
    python tools/gen_smoke.py --out mods/cameo/bits/effects/smoke_dark.png --frames 32

All look knobs are CLI flags so we can retune without editing code.
"""
from __future__ import annotations

import argparse
import os

import numpy as np
from PIL import Image
from PIL.PngImagePlugin import PngInfo


def fractal_noise(h: int, w: int, octaves: int, rng: np.random.Generator) -> np.ndarray:
    """Sum-of-octaves value noise in [0, 1], bicubically upsampled per octave."""
    out = np.zeros((h, w), dtype=np.float32)
    amp = 1.0
    amp_total = 0.0
    for o in range(octaves):
        # Low-res random grid for this octave; finer grids at higher octaves.
        gh = max(2, int(np.ceil(h / (2 ** (octaves - o)))) + 1)
        gw = max(2, int(np.ceil(w / (2 ** (octaves - o)))) + 1)
        grid = rng.random((gh, gw)).astype(np.float32)
        up = Image.fromarray(grid, mode="F").resize((w, h), Image.BICUBIC)
        out += amp * np.asarray(up, dtype=np.float32)
        amp_total += amp
        amp *= 0.5
    out /= amp_total
    return np.clip(out, 0.0, 1.0)


def smoothstep(t: np.ndarray | float):
    t = np.clip(t, 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def warp(field: np.ndarray, dx: np.ndarray, dy: np.ndarray) -> np.ndarray:
    """Domain-warp `field` by per-pixel integer displacement (clamped)."""
    h, w = field.shape
    ys, xs = np.mgrid[0:h, 0:w]
    sx = np.clip(xs + dx.astype(np.int32), 0, w - 1)
    sy = np.clip(ys + dy.astype(np.int32), 0, h - 1)
    return field[sy, sx]


def build(args) -> Image.Image:
    rng = np.random.default_rng(args.seed)
    n = args.frames
    fs = args.frame_size

    # One tall fractal field we scroll downward through to read as rising smoke.
    rise_total = int(n * args.rise)
    big = fractal_noise(fs + rise_total + 4, fs, args.octaves, rng)

    # Low-frequency displacement fields for curl/turbulence.
    dispx = (fractal_noise(fs, fs, 3, rng) - 0.5) * 2.0
    dispy = (fractal_noise(fs, fs, 3, rng) - 0.5) * 2.0

    ys, xs = np.mgrid[0:fs, 0:fs].astype(np.float32)
    cx = fs * 0.5
    cy = fs * 0.58  # anchor slightly low; puff billows upward

    core = np.array(args.core_rgb, dtype=np.float32)
    edge = np.array(args.edge_rgb, dtype=np.float32)

    frames = []
    for f in range(n):
        life = f / (n - 1) if n > 1 else 0.0

        scroll = rise_total - int(round(f * args.rise))
        layer = big[scroll:scroll + fs, :]

        # Curl grows a touch over life so the top frays as it dissipates.
        curl = args.warp * (0.6 + 0.8 * life)
        field = warp(layer, dispx * curl, dispy * curl)
        field = (field - field.min()) / (np.ptp(field) + 1e-6)

        # Broad, flat-topped radial+vertical shape that expands as it grows.
        r = fs * (args.radius0 + (args.radius1 - args.radius0) * smoothstep(life))
        rx = (xs - cx) / r
        ry = (ys - cy) / (r * 1.25)  # taller than wide
        shape = np.exp(-((rx * rx + ry * ry) ** 1.4))  # flatter top than a gaussian

        # Anchor the base, fray the crown.
        vgrad = smoothstep((ys / fs) * 1.4)  # 0 at top -> 1 at bottom
        shape *= 0.45 + 0.55 * vgrad

        # Metaball: noise pushes the boundary in/out -> solid core, ragged edges.
        d = shape + (field - 0.5) * args.roughness
        density = smoothstep((d - args.cutoff) / args.edge_soft) ** args.contrast

        # Life envelope: quick fade-in, long dissipating fade-out.
        env = smoothstep(life / args.fade_in) if args.fade_in > 0 else 1.0
        env = np.minimum(env, smoothstep((1.0 - life) / args.fade_out))
        alpha = np.clip(density * env * args.opacity, 0.0, 1.0)

        # Mostly dark; a touch of lift only at the very thin fraying edge for form.
        t = density[..., None]
        rgb = edge * (1.0 - t) + core * t

        out = np.zeros((fs, fs, 4), dtype=np.float32)
        out[..., :3] = rgb
        out[..., 3] = alpha * 255.0
        out[..., :3] *= (alpha[..., None] > 0)  # keep transparent pixels clean
        frames.append(np.clip(out, 0, 255).astype(np.uint8))

    cols, rows = args.cols, int(np.ceil(n / args.cols))
    sheet = np.zeros((rows * fs, cols * fs, 4), dtype=np.uint8)
    for i, fr in enumerate(frames):
        r, c = divmod(i, cols)
        sheet[r * fs:(r + 1) * fs, c * fs:(c + 1) * fs] = fr
    return Image.fromarray(sheet, mode="RGBA")


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--out", default="mods/cameo/bits/effects/smoke_dark.png")
    p.add_argument("--frames", type=int, default=32)
    p.add_argument("--cols", type=int, default=8)
    p.add_argument("--frame-size", type=int, default=64)
    p.add_argument("--seed", type=int, default=7)
    p.add_argument("--octaves", type=int, default=6)
    p.add_argument("--rise", type=float, default=2.2, help="px scrolled up per frame")
    p.add_argument("--warp", type=float, default=11.0, help="domain-warp strength (px)")
    p.add_argument("--radius0", type=float, default=0.20, help="start radius (frac of frame)")
    p.add_argument("--radius1", type=float, default=0.46, help="end radius (frac of frame)")
    p.add_argument("--roughness", type=float, default=0.9, help="edge raggedness (noise push)")
    p.add_argument("--cutoff", type=float, default=0.42, help="metaball boundary threshold")
    p.add_argument("--edge-soft", type=float, default=0.4, help="edge feather width")
    p.add_argument("--contrast", type=float, default=1.0)
    p.add_argument("--opacity", type=float, default=1.0)
    # Gradual fade-in over the first ~third of life so puffs bloom in rather than popping.
    p.add_argument("--fade-in", type=float, default=0.32)
    p.add_argument("--fade-out", type=float, default=0.6)
    p.add_argument("--core-rgb", type=int, nargs=3, default=(20, 20, 23))
    p.add_argument("--edge-rgb", type=int, nargs=3, default=(58, 58, 64))
    args = p.parse_args()

    img = build(args)
    os.makedirs(os.path.dirname(args.out), exist_ok=True)

    # OpenRA's PngSheet loader auto-slices a grid when these tEXt chunks exist.
    meta = PngInfo()
    meta.add_text("FrameSize", f"{args.frame_size},{args.frame_size}")
    meta.add_text("FrameAmount", str(args.frames))
    img.save(args.out, pnginfo=meta)
    print(f"wrote {args.out}  ({img.width}x{img.height}, {args.frames} frames, "
          f"{args.cols}x{int(np.ceil(args.frames / args.cols))} grid, FrameSize "
          f"{args.frame_size}x{args.frame_size})")


if __name__ == "__main__":
    main()
