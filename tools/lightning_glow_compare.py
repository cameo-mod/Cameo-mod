#!/usr/bin/env python3
"""Side-by-side still of how the lightning bolt's glow is built, on dark background.

Renders the SAME fractal bolt three ways so the glow *quality* can be compared without launching:
  1. Core only           - the bare white channel, no glow.
  2. Our bolt-glow        - graduated additive line passes drawn FROM the path (what ships now):
                            follows every jag, but the softness is faked by layering widths.
  3. Gaussian post-glow   - a true gaussian blur of the same channel (the post-process look):
                            buttery-smooth bloom, but here applied to the same path for a fair compare.

Uses the same fractal generator (gen_lightning.bolt_path) as the in-game projectile.

Usage:
    python tools/lightning_glow_compare.py
"""
from __future__ import annotations

import argparse
import os
import random
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_lightning  # noqa: E402

W, H, SS = 380, 110, 3
BG = np.array([18, 22, 32], dtype=np.float32) / 255.0
BLUE = np.array([110, 170, 255], dtype=np.float32) / 255.0
WHITE = np.array([255, 255, 255], dtype=np.float32) / 255.0


def make_path(seed):
    rng = random.Random(seed)
    yc = H * SS / 2 + rng.uniform(-H * SS * 0.05, H * SS * 0.05)
    return gen_lightning.bolt_path(3 * SS, W * SS - 3 * SS, yc, 6, 0.55, 0.30, rng, H * SS)


def core_mask(path, width):
    img = Image.new("L", (W * SS, H * SS), 0)
    d = ImageDraw.Draw(img)
    d.line(path, fill=255, width=max(1, int(width * SS)))
    r = int(width * SS) // 2                                  # round caps so corners don't gap
    if r:
        for x, y in path:
            d.ellipse([x - r, y - r, x + r, y + r], fill=255)
    return img


def downscale(img):
    return np.asarray(img.resize((W, H), Image.LANCZOS), dtype=np.float32) / 255.0


def composite(acc):
    return np.clip(BG[None, None, :] + acc, 0.0, 1.0)


def core_only(path):
    acc = downscale(core_mask(path, 1.5))[..., None] * WHITE[None, None, :]
    return composite(acc)


def our_glow(path):
    base, core_w = 3.0, 1.5
    passes = [(base * 1.8, BLUE, 0.4), (base * 1.3, BLUE, 0.7),
              (base, BLUE, 1.0), (base * 0.5, BLUE, 1.0), (core_w, WHITE, 1.0)]
    acc = np.zeros((H, W, 3), dtype=np.float32)
    for width, rgb, alpha in passes:
        acc += downscale(core_mask(path, width))[..., None] * rgb[None, None, :] * alpha
    return composite(acc)


def gaussian_glow(path):
    core = core_mask(path, 1.5)
    g1 = np.asarray(core.filter(ImageFilter.GaussianBlur(2.0 * SS)), dtype=np.float32)
    g2 = np.asarray(core.filter(ImageFilter.GaussianBlur(5.0 * SS)), dtype=np.float32)
    g3 = np.asarray(core.filter(ImageFilter.GaussianBlur(11.0 * SS)), dtype=np.float32)
    glow = Image.fromarray(np.clip(g1 + 0.6 * g2 + 0.35 * g3, 0, 255).astype(np.uint8))
    acc = downscale(glow)[..., None] * BLUE[None, None, :]
    acc += downscale(core)[..., None] * WHITE[None, None, :]
    return composite(acc)


def panel(arr, label):
    img = Image.fromarray((arr * 255).astype(np.uint8), "RGB")
    d = ImageDraw.Draw(img)
    d.text((7, 5), label, fill=(210, 225, 255))
    return img


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--out", default="docs/lightning/glow_compare.png")
    p.add_argument("--seed", type=int, default=4)
    args = p.parse_args()

    path = make_path(args.seed)
    panels = [
        panel(core_only(path), "1. Core only (no glow)"),
        panel(our_glow(path), "2. Our bolt-glow - additive, follows the path (ships now)"),
        panel(gaussian_glow(path), "3. Gaussian post-process glow - smooth bloom"),
    ]

    gap = 4
    sheet = Image.new("RGB", (W, len(panels) * H + (len(panels) - 1) * gap), (0, 0, 0))
    for i, pn in enumerate(panels):
        sheet.paste(pn, (0, i * (H + gap)))

    os.makedirs(os.path.dirname(args.out) or ".", exist_ok=True)
    sheet.save(args.out)
    print(f"wrote {args.out} ({sheet.width}x{sheet.height}, 3 panels)")


if __name__ == "__main__":
    main()
