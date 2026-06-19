#!/usr/bin/env python3
"""Procedural snowflake particle sprite-sheet for Cameo-mod cryo snow.

A single light-cyan dendritic snowflake that gently rotates and fades over its
lifetime, played once per particle by the same emitter mechanism as the chill
fog (CryoFogEmitter -> DeterministicSmokeParticle, sequence "idle"). License-clean
procedural art (numpy). Mirrors tools/gen_cryo_fog.py's sheet format.

Usage:
    python tools/gen_snowflake.py
    python tools/gen_snowflake.py --out mods/cameo/bits/effects/snowflake.png --frames 32
"""
from __future__ import annotations

import argparse
import os

import numpy as np
from PIL import Image
from PIL.PngImagePlugin import PngInfo


def smoothstep(t):
    t = np.clip(t, 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def draw_flake(fs: int, scale: float, rot: float) -> np.ndarray:
    """Single white dendritic 6-arm snowflake centred in an fs x fs frame."""
    ys, xs = np.mgrid[0:fs, 0:fs].astype(np.float32)
    cx = cy = fs / 2.0
    dx = xs - cx
    dy = ys - cy

    size = scale * fs * 0.16
    arm_len = size * 1.5          # shorter, chunkier arms (less web-like than thin filaments)
    arm_w = max(0.9, size * 0.24)  # thicker arms
    star = np.zeros((fs, fs), dtype=np.float32)
    for k in range(3):
        a = rot + k * np.pi / 3.0
        u = dx * np.cos(a) + dy * np.sin(a)
        v = -dx * np.sin(a) + dy * np.cos(a)
        arm = np.exp(-(u * u) / (2.0 * arm_len ** 2) - (v * v) / (2.0 * arm_w ** 2))
        star = np.maximum(star, arm)

        # A single pair of short, stubby tips near the arm ends - reads as a crystal, not a net.
        for sgn in (-1.0, 1.0):
            bu = u - sgn * size * 1.05
            tip = np.exp(
                -(bu * bu) / (2.0 * (arm_w * 1.5) ** 2)
                - (v * v) / (2.0 * (arm_w * 2.2) ** 2))
            star = np.maximum(star, tip * 0.7)

    center = np.exp(-(dx * dx + dy * dy) / (2.0 * (size * 0.62) ** 2))  # bold hex-ish core
    return np.clip(np.maximum(star, center), 0.0, 1.0)


def build(args) -> Image.Image:
    fs = args.frame_size
    n = args.frames
    color = np.array(args.rgb, dtype=np.float32)

    frames = []
    for f in range(n):
        life = f / (n - 1) if n > 1 else 0.0
        rot = args.spin * life                                   # gentle rotation as it drifts
        # Quick fade-in, long fade-out so it appears then melts away (played once over life).
        env = smoothstep(life / args.fade_in) if args.fade_in > 0 else 1.0
        env = np.minimum(env, smoothstep((1.0 - life) / args.fade_out))
        alpha = draw_flake(fs, args.scale, rot) * env * args.opacity

        out = np.zeros((fs, fs, 4), dtype=np.float32)
        out[..., :3] = color
        out[..., 3] = np.clip(alpha * 255.0, 0.0, 255.0)
        out[..., :3] *= (out[..., 3:4] > 0)
        frames.append(out.astype(np.uint8))

    cols, rows = args.cols, int(np.ceil(n / args.cols))
    sheet = np.zeros((rows * fs, cols * fs, 4), dtype=np.uint8)
    for i, fr in enumerate(frames):
        r, c = divmod(i, cols)
        sheet[r * fs:(r + 1) * fs, c * fs:(c + 1) * fs] = fr
    return Image.fromarray(sheet, mode="RGBA")


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--out", default="mods/cameo/bits/effects/snowflake.png")
    p.add_argument("--frames", type=int, default=32)
    p.add_argument("--cols", type=int, default=8)
    p.add_argument("--frame-size", type=int, default=32)
    p.add_argument("--scale", type=float, default=0.55, help="snowflake size (frac of frame)")
    p.add_argument("--spin", type=float, default=0.8, help="total rotation over life, radians")
    p.add_argument("--opacity", type=float, default=0.95)
    p.add_argument("--fade-in", type=float, default=0.15)
    p.add_argument("--fade-out", type=float, default=0.55)
    p.add_argument("--rgb", type=int, nargs=3, default=(224, 242, 255))
    args = p.parse_args()

    img = build(args)
    os.makedirs(os.path.dirname(args.out), exist_ok=True)

    meta = PngInfo()
    meta.add_text("FrameSize", f"{args.frame_size},{args.frame_size}")
    meta.add_text("FrameAmount", str(args.frames))
    img.save(args.out, pnginfo=meta)
    print(f"wrote {args.out}  ({img.width}x{img.height}, {args.frames} frames, "
          f"FrameSize {args.frame_size}x{args.frame_size})")


if __name__ == "__main__":
    main()
