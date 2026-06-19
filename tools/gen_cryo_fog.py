#!/usr/bin/env python3
"""Procedural RGBA cryo-fog sprite-sheet generator for Cameo-mod.

A lightly transparent blue/cyan "chill fog" puff with a few rising snowflake
specks, shown on units and buildings under a cryo (freeze) effect. The colour
matches the in-game cryo overlay (light blue). License-clean: original art from
numpy fractal noise plus procedurally drawn snowflakes (no external assets).

Output is a single PNG sprite sheet (COLS x ROWS frames) loaded via OpenRA's
PngSheet format. Mirrors tools/gen_smoke.py: each particle plays the sheet once
over its lifetime (born small, rises + churns, fades out).

Usage:
    python tools/gen_cryo_fog.py
    python tools/gen_cryo_fog.py --out mods/cameo/bits/effects/smoke_cryo.png --frames 32

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
        gh = max(2, int(np.ceil(h / (2 ** (octaves - o)))) + 1)
        gw = max(2, int(np.ceil(w / (2 ** (octaves - o)))) + 1)
        grid = rng.random((gh, gw)).astype(np.float32)
        up = Image.fromarray(grid, mode="F").resize((w, h), Image.BICUBIC)
        out += amp * np.asarray(up, dtype=np.float32)
        amp_total += amp
        amp *= 0.5
    out /= amp_total
    return np.clip(out, 0.0, 1.0)


def smoothstep(t):
    t = np.clip(t, 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def warp(field: np.ndarray, dx: np.ndarray, dy: np.ndarray) -> np.ndarray:
    """Domain-warp `field` by per-pixel integer displacement (clamped)."""
    h, w = field.shape
    ys, xs = np.mgrid[0:h, 0:w]
    sx = np.clip(xs + dx.astype(np.int32), 0, w - 1)
    sy = np.clip(ys + dy.astype(np.int32), 0, h - 1)
    return field[sy, sx]


def snowflakes(fs: int, life: float, flakes) -> np.ndarray:
    """Additive intensity layer of rising 6-arm dendritic snowflakes for one frame."""
    acc = np.zeros((fs, fs), dtype=np.float32)
    ys, xs = np.mgrid[0:fs, 0:fs].astype(np.float32)
    for (x0, size, phase, sway) in flakes:
        # Rises from near the base to the crown over the particle's life.
        fy = (0.90 - 0.78 * life) * fs
        fx = x0 + np.sin(life * 6.2832 * 1.5 + phase) * (fs * sway)
        twinkle = 0.6 + 0.4 * np.sin(life * 6.2832 * 3.0 + phase)
        env = smoothstep(life / 0.15) * smoothstep((1.0 - life) / 0.5)
        b = twinkle * env
        if b <= 0.0:
            continue

        dx = xs - fx
        dy = ys - fy
        arm_len = size * 1.9
        arm_w = size * 0.13
        star = np.zeros((fs, fs), dtype=np.float32)
        # Six main arms (three crossed axes), each with a symmetric pair of side branches
        # to read as a hexagonal crystal rather than a plain sparkle.
        for k in range(3):
            a = k * np.pi / 3.0
            u = dx * np.cos(a) + dy * np.sin(a)
            v = -dx * np.sin(a) + dy * np.cos(a)
            arm = np.exp(-(u * u) / (2.0 * arm_len ** 2) - (v * v) / (2.0 * arm_w ** 2))
            star = np.maximum(star, arm)

            # Dendrite branches at two points out along each arm (both directions).
            for along in (size * 0.9, size * 1.5):
                for sgn in (-1.0, 1.0):
                    bu = u - sgn * along
                    bw = arm_len * 0.45
                    branch = np.exp(
                        -(bu * bu) / (2.0 * (arm_w * 1.2) ** 2)
                        - (v * v) / (2.0 * bw ** 2))
                    # keep the branch short by masking far from the arm point
                    branch *= np.exp(-(bu * bu) / (2.0 * (size * 0.5) ** 2))
                    star = np.maximum(star, branch * 0.6)

        center = np.exp(-(dx * dx + dy * dy) / (2.0 * (size * 0.45) ** 2))
        acc += b * np.maximum(star, center)

    return np.clip(acc, 0.0, 1.0)


def build(args) -> Image.Image:
    rng = np.random.default_rng(args.seed)
    n = args.frames
    fs = args.frame_size

    rise_total = int(n * args.rise)
    big = fractal_noise(fs + rise_total + 4, fs, args.octaves, rng)
    dispx = (fractal_noise(fs, fs, 3, rng) - 0.5) * 2.0
    dispy = (fractal_noise(fs, fs, 3, rng) - 0.5) * 2.0

    ys, xs = np.mgrid[0:fs, 0:fs].astype(np.float32)
    cx = fs * 0.5
    cy = fs * 0.58

    core = np.array(args.core_rgb, dtype=np.float32)
    edge = np.array(args.edge_rgb, dtype=np.float32)
    snow_rgb = np.array(args.snow_rgb, dtype=np.float32)

    # Fixed snowflake set for this sheet (seeded): x position, size, phase, sway.
    flakes = []
    for _ in range(args.flakes):
        flakes.append((
            rng.uniform(0.28, 0.72) * fs,
            rng.uniform(2.2, 3.4),
            rng.uniform(0.0, 6.2832),
            rng.uniform(0.03, 0.07),
        ))

    frames = []
    for f in range(n):
        life = f / (n - 1) if n > 1 else 0.0

        scroll = rise_total - int(round(f * args.rise))
        layer = big[scroll:scroll + fs, :]

        curl = args.warp * (0.6 + 0.8 * life)
        field = warp(layer, dispx * curl, dispy * curl)
        field = (field - field.min()) / (np.ptp(field) + 1e-6)

        r = fs * (args.radius0 + (args.radius1 - args.radius0) * smoothstep(life))
        rx = (xs - cx) / r
        ry = (ys - cy) / (r * 1.25)
        shape = np.exp(-((rx * rx + ry * ry) ** 1.4))

        vgrad = smoothstep((ys / fs) * 1.4)
        shape *= 0.45 + 0.55 * vgrad

        d = shape + (field - 0.5) * args.roughness
        density = smoothstep((d - args.cutoff) / args.edge_soft) ** args.contrast

        env = smoothstep(life / args.fade_in) if args.fade_in > 0 else 1.0
        env = np.minimum(env, smoothstep((1.0 - life) / args.fade_out))

        topfade = (1.0 - args.top_fade) + args.top_fade * (ys / fs)
        alpha = np.clip(density * env * topfade * args.opacity, 0.0, 1.0)

        t = density[..., None]
        rgb = edge * (1.0 - t) + core * t

        # Composite rising snowflakes on top: bright white-blue, raises alpha.
        snow = snowflakes(fs, life, flakes)
        s = snow[..., None]
        rgb = rgb * (1.0 - s) + snow_rgb * s
        alpha = np.clip(alpha + snow * args.snow_opacity, 0.0, 1.0)

        out = np.zeros((fs, fs, 4), dtype=np.float32)
        out[..., :3] = rgb
        out[..., 3] = alpha * 255.0
        out[..., :3] *= (alpha[..., None] > 0)
        frames.append(np.clip(out, 0, 255).astype(np.uint8))

    cols, rows = args.cols, int(np.ceil(n / args.cols))
    sheet = np.zeros((rows * fs, cols * fs, 4), dtype=np.uint8)
    for i, fr in enumerate(frames):
        r, c = divmod(i, cols)
        sheet[r * fs:(r + 1) * fs, c * fs:(c + 1) * fs] = fr
    return Image.fromarray(sheet, mode="RGBA")


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--out", default="mods/cameo/bits/effects/smoke_cryo.png")
    p.add_argument("--frames", type=int, default=32)
    p.add_argument("--cols", type=int, default=8)
    p.add_argument("--frame-size", type=int, default=64)
    p.add_argument("--seed", type=int, default=13)
    p.add_argument("--octaves", type=int, default=6)
    p.add_argument("--rise", type=float, default=2.0, help="px scrolled up per frame")
    p.add_argument("--warp", type=float, default=9.0, help="domain-warp strength (px)")
    p.add_argument("--radius0", type=float, default=0.18, help="start radius (frac of frame)")
    p.add_argument("--radius1", type=float, default=0.44, help="end radius (frac of frame)")
    p.add_argument("--roughness", type=float, default=0.85, help="edge raggedness (noise push)")
    p.add_argument("--cutoff", type=float, default=0.44, help="metaball boundary threshold")
    p.add_argument("--edge-soft", type=float, default=0.42, help="edge feather width")
    p.add_argument("--contrast", type=float, default=1.0)
    p.add_argument("--opacity", type=float, default=0.5, help="puff is lightly transparent")
    p.add_argument("--fade-in", type=float, default=0.3)
    p.add_argument("--fade-out", type=float, default=0.78)
    p.add_argument("--top-fade", type=float, default=0.5)
    # Snowflakes are a separate sprite/effect now (tools/gen_snowflake.py + CryoSparkleEffect);
    # this generator produces the plain chill-fog puff. Set >0 only for legacy combined sheets.
    p.add_argument("--flakes", type=int, default=0, help="number of rising snowflake specks")
    p.add_argument("--snow-opacity", type=float, default=0.95)
    # Light cyan puff matching the cryo overlay; near-white blue snowflakes.
    p.add_argument("--core-rgb", type=int, nargs=3, default=(196, 230, 255))
    p.add_argument("--edge-rgb", type=int, nargs=3, default=(120, 178, 238))
    p.add_argument("--snow-rgb", type=int, nargs=3, default=(228, 244, 255))
    args = p.parse_args()

    img = build(args)
    os.makedirs(os.path.dirname(args.out), exist_ok=True)

    meta = PngInfo()
    meta.add_text("FrameSize", f"{args.frame_size},{args.frame_size}")
    meta.add_text("FrameAmount", str(args.frames))
    img.save(args.out, pnginfo=meta)
    print(f"wrote {args.out}  ({img.width}x{img.height}, {args.frames} frames, "
          f"{args.cols}x{int(np.ceil(args.frames / args.cols))} grid, FrameSize "
          f"{args.frame_size}x{args.frame_size})")


if __name__ == "__main__":
    main()
