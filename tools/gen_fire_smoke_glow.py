#!/usr/bin/env python3
"""Procedural RGBA smoke-with-bottom-glow sprite-sheet generator for Cameo-mod.

License-clean: generates original art from numpy fractal noise. The visual goal
is dark rising smoke with a warm flame-lit base baked into the sprite, so the
groundfire smoke reads as hot without requiring a separate glow actor.

Output is a single PNG sprite sheet (COLS x ROWS frames) that OpenRA loads via
the PngSheet format. Each particle plays the sheet once over its lifetime.

Usage:
    python tools/gen_fire_smoke_glow.py
    python tools/gen_fire_smoke_glow.py --out mods/cameo/bits/effects/smoke_fire_glow.png

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

    rise_total = int(n * args.rise)
    big = fractal_noise(fs + rise_total + 4, fs, args.octaves, rng)
    dispx = (fractal_noise(fs, fs, 3, rng) - 0.5) * 2.0
    dispy = (fractal_noise(fs, fs, 3, rng) - 0.5) * 2.0
    glow_noise = fractal_noise(fs, fs, 5, rng)

    ys, xs = np.mgrid[0:fs, 0:fs].astype(np.float32)
    cx = fs * 0.5
    cy = fs * args.smoke_anchor_y

    smoke_core = np.array(args.smoke_core_rgb, dtype=np.float32)
    smoke_edge = np.array(args.smoke_edge_rgb, dtype=np.float32)
    glow_deep = np.array(args.glow_rgb, dtype=np.float32)
    glow_hot = np.array(args.glow_hot_rgb, dtype=np.float32)
    glow_rim = np.array(args.glow_rim_rgb, dtype=np.float32)

    frames = []
    for f in range(n):
        life = f / (n - 1) if n > 1 else 0.0

        scroll = rise_total - int(round(f * args.rise))
        layer = big[scroll:scroll + fs, :]

        curl = args.warp * (0.65 + 0.85 * life)
        field = warp(layer, dispx * curl, dispy * curl)
        field = (field - field.min()) / (np.ptp(field) + 1e-6)

        r = fs * (args.radius0 + (args.radius1 - args.radius0) * smoothstep(life))
        rx = (xs - cx) / r
        ry = (ys - cy) / (r * args.smoke_height)
        shape = np.exp(-((rx * rx + ry * ry) ** 1.35))

        vgrad = smoothstep((ys / fs) * 1.35)
        shape *= 0.42 + 0.58 * vgrad

        d = shape + (field - 0.5) * args.roughness
        density = smoothstep((d - args.cutoff) / args.edge_soft) ** args.contrast

        fade_in = smoothstep(life / args.fade_in) if args.fade_in > 0 else 1.0
        fade_out = smoothstep((1.0 - life) / args.fade_out)
        env = np.minimum(fade_in, fade_out)
        glow_window = 1.0 - smoothstep((life - args.glow_fade_start) / (args.glow_fade_end - args.glow_fade_start))
        glow_env = env * glow_window

        topfade = (1.0 - args.top_fade) + args.top_fade * (ys / fs)
        smoke_alpha = np.clip(density * env * topfade * args.smoke_opacity, 0.0, 1.0)

        gx = (xs - cx) / (fs * args.glow_width)
        gy = (ys - fs * args.glow_y) / (fs * args.glow_height)
        base_blob = np.exp(-(gx * gx + gy * gy))
        glow_texture = warp(glow_noise, dispx * curl * 0.35, dispy * curl * 0.35)
        nx = (xs - cx) / (fs * args.glow_u_width)
        side = np.clip(np.abs(nx), 0.0, 1.0)
        u_curve = side ** args.glow_u_power
        glow_top = (
            args.glow_top
            - u_curve * args.glow_u_lift
            + (glow_texture - 0.5) * args.glow_top_jitter
            + (field - 0.5) * args.glow_top_jitter * 0.45)
        lower_smoke = smoothstep((ys / fs - glow_top) / args.glow_feather)
        ragged = 0.68 + 0.32 * glow_texture
        smoke_attachment = smoothstep((density - args.glow_density_floor) / (1.0 - args.glow_density_floor))
        bottom_basin = np.exp(-(nx * nx / 1.25 + ((ys / fs - args.glow_u_bottom_y) / args.glow_u_bottom_h) ** 2))
        left_arm = np.exp(-(((nx + args.glow_u_arm_x) / args.glow_u_arm_w) ** 2
                            + ((ys / fs - args.glow_u_arm_y) / args.glow_u_arm_h) ** 2))
        right_arm = np.exp(-(((nx - args.glow_u_arm_x) / args.glow_u_arm_w) ** 2
                             + ((ys / fs - args.glow_u_arm_y) / args.glow_u_arm_h) ** 2))
        center_notch = np.exp(-(nx / args.glow_u_notch_w) ** 2) * smoothstep(
            (args.glow_u_notch_y - ys / fs) / args.glow_u_notch_feather)
        u_lobe = np.clip(bottom_basin + args.glow_u_arm * (left_arm + right_arm)
                         - args.glow_u_notch * center_notch, 0.0, 1.0)
        glow_shape = (0.16 * base_blob + 0.84 * u_lobe) * lower_smoke * ragged * smoke_attachment
        glow_shape += args.glow_density_lift * (smoke_attachment ** 1.4) * u_lobe
        glow_mask = smoothstep((glow_shape - args.glow_cutoff) / args.glow_soft)
        glow_alpha = np.clip(glow_mask * glow_env * args.glow_opacity, 0.0, 1.0)

        heat = np.clip(base_blob * (0.7 + 0.3 * density), 0.0, 1.0)
        hot_mix = smoothstep((heat - 0.35) / 0.65)[..., None]
        rim_mix = smoothstep((ys / fs - 0.68) / 0.25)[..., None] * (1.0 - hot_mix)
        glow_rgb = glow_deep * (1.0 - hot_mix) + glow_hot * hot_mix
        glow_rgb = glow_rgb * (1.0 - rim_mix) + glow_rim * rim_mix

        smoke_rgb = smoke_edge * (1.0 - density[..., None]) + smoke_core * density[..., None]
        out_alpha = np.clip(1.0 - (1.0 - smoke_alpha) * (1.0 - glow_alpha), 0.0, 1.0)
        out_rgb = np.zeros((fs, fs, 3), dtype=np.float32)

        rgb_weight = smoke_alpha + glow_alpha * args.glow_rgb_weight
        nonzero = rgb_weight > 1e-6
        out_rgb[nonzero] = (
            smoke_rgb[nonzero] * smoke_alpha[nonzero, None]
            + glow_rgb[nonzero] * glow_alpha[nonzero, None] * args.glow_rgb_weight
        ) / rgb_weight[nonzero, None]

        out = np.zeros((fs, fs, 4), dtype=np.float32)
        out[..., :3] = out_rgb
        out[..., 3] = out_alpha * 255.0
        out[..., :3] *= (out_alpha[..., None] > 0)
        frames.append(np.clip(out, 0, 255).astype(np.uint8))

    cols, rows = args.cols, int(np.ceil(n / args.cols))
    sheet = np.zeros((rows * fs, cols * fs, 4), dtype=np.uint8)
    for i, fr in enumerate(frames):
        r, c = divmod(i, cols)
        sheet[r * fs:(r + 1) * fs, c * fs:(c + 1) * fs] = fr

    return Image.fromarray(sheet, mode="RGBA")


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--out", default="mods/cameo/bits/effects/smoke_fire_glow.png")
    p.add_argument("--frames", type=int, default=32)
    p.add_argument("--cols", type=int, default=8)
    p.add_argument("--frame-size", type=int, default=64)
    p.add_argument("--seed", type=int, default=19)
    p.add_argument("--octaves", type=int, default=6)
    p.add_argument("--rise", type=float, default=2.2, help="px scrolled up per frame")
    p.add_argument("--warp", type=float, default=11.0, help="domain-warp strength (px)")
    p.add_argument("--radius0", type=float, default=0.20)
    p.add_argument("--radius1", type=float, default=0.46)
    p.add_argument("--smoke-anchor-y", type=float, default=0.58)
    p.add_argument("--smoke-height", type=float, default=1.25)
    p.add_argument("--roughness", type=float, default=0.9)
    p.add_argument("--cutoff", type=float, default=0.42)
    p.add_argument("--edge-soft", type=float, default=0.4)
    p.add_argument("--contrast", type=float, default=1.0)
    p.add_argument("--smoke-opacity", type=float, default=0.68)
    p.add_argument("--fade-in", type=float, default=0.28)
    p.add_argument("--fade-out", type=float, default=0.82)
    p.add_argument("--top-fade", type=float, default=0.62)
    p.add_argument("--smoke-core-rgb", type=int, nargs=3, default=(46, 45, 48))
    p.add_argument("--smoke-edge-rgb", type=int, nargs=3, default=(88, 84, 80))
    p.add_argument("--glow-rgb", type=int, nargs=3, default=(216, 72, 16))
    p.add_argument("--glow-hot-rgb", type=int, nargs=3, default=(255, 164, 44))
    p.add_argument("--glow-rim-rgb", type=int, nargs=3, default=(214, 96, 28))
    p.add_argument("--glow-opacity", type=float, default=0.5)
    p.add_argument("--glow-rgb-weight", type=float, default=1.35)
    p.add_argument("--glow-y", type=float, default=0.72)
    p.add_argument("--glow-width", type=float, default=0.34)
    p.add_argument("--glow-height", type=float, default=0.28)
    p.add_argument("--glow-top", type=float, default=0.66,
                   help="top edge of the lower-smoke glow fill as a frame fraction")
    p.add_argument("--glow-u-lift", type=float, default=0.22,
                   help="how far the glow climbs at the sides to make a concave U shape")
    p.add_argument("--glow-u-power", type=float, default=1.8,
                   help="curvature of the concave U glow boundary")
    p.add_argument("--glow-u-width", type=float, default=0.27,
                   help="horizontal size of the explicit U glow lobe")
    p.add_argument("--glow-u-bottom-y", type=float, default=0.79,
                   help="vertical center of the U bottom basin")
    p.add_argument("--glow-u-bottom-h", type=float, default=0.17,
                   help="vertical height of the U bottom basin")
    p.add_argument("--glow-u-arm", type=float, default=0.6,
                   help="extra glow weight along the lifted side arms of the U")
    p.add_argument("--glow-u-arm-x", type=float, default=0.58,
                   help="horizontal center of each U side arm")
    p.add_argument("--glow-u-arm-y", type=float, default=0.64,
                   help="vertical center of each U side arm")
    p.add_argument("--glow-u-arm-w", type=float, default=0.26,
                   help="horizontal width of each U side arm")
    p.add_argument("--glow-u-arm-h", type=float, default=0.21,
                   help="vertical height of each U side arm")
    p.add_argument("--glow-u-notch", type=float, default=0.55,
                   help="center-top suppression that keeps the U from becoming a filled blob")
    p.add_argument("--glow-u-notch-w", type=float, default=0.48,
                   help="horizontal width of the center-top U notch")
    p.add_argument("--glow-u-notch-y", type=float, default=0.66,
                   help="highest vertical position affected by the center-top U notch")
    p.add_argument("--glow-u-notch-feather", type=float, default=0.18,
                   help="vertical softness of the center-top U notch")
    p.add_argument("--glow-top-jitter", type=float, default=0.18,
                   help="noise-driven vertical breakup of the glow top edge")
    p.add_argument("--glow-feather", type=float, default=0.17,
                   help="vertical softness of the lower-smoke glow fill")
    p.add_argument("--glow-cutoff", type=float, default=0.18)
    p.add_argument("--glow-soft", type=float, default=0.28)
    p.add_argument("--glow-density-floor", type=float, default=0.14,
                   help="minimum smoke density before glow strongly attaches to the puff")
    p.add_argument("--glow-density-lift", type=float, default=0.18,
                   help="extra non-horizontal glow from dense smoke around the hot lobe")
    p.add_argument("--glow-fade-start", type=float, default=0.10,
                   help="life fraction where the baked base glow starts fading out")
    p.add_argument("--glow-fade-end", type=float, default=0.34,
                   help="life fraction where the baked base glow is fully gone")
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
