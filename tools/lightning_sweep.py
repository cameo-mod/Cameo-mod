#!/usr/bin/env python3
"""Contact sheet of fractal-lightning looks for picking LightningZap parameters.

Renders one dark-background strip per (roughness, amplitude) combination using the same
fractal generator as gen_lightning.py / the in-game projectile, labels each with its params,
and stacks them into a single PNG so the look can be compared from a still.

Usage:
    python tools/lightning_sweep.py
    python tools/lightning_sweep.py --generations 6 --out tools/lightning_sweep.png
"""
from __future__ import annotations

import argparse
import os
import random
import sys
from argparse import Namespace

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_lightning  # noqa: E402

BG = (18, 22, 32)            # dark scene, like the game terrain, so the additive glow reads
ROUGHNESS = [0.45, 0.55, 0.65]
AMPS = [0.25, 0.40]


def strip(generations, roughness, amp, width, height, seed):
    args = Namespace(
        frames=1, width=width, height=height, amp=amp,
        generations=generations, roughness=roughness, branches=2,
        core_width=2.0, glow_radius=1.6, glow_gain=1.0, opacity=1.0,
        supersample=3, rgb_core=(255, 255, 255), rgb_glow=(110, 170, 255), seed=seed,
    )
    bolt = gen_lightning.build(args)                      # RGBA, transparent
    back = Image.new("RGBA", bolt.size, BG + (255,))
    img = Image.alpha_composite(back, bolt)
    d = ImageDraw.Draw(img)
    d.text((6, 4), f"roughness={roughness:.2f}  amp={amp:.2f}", fill=(210, 225, 255, 255))
    return img


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--out", default="docs/lightning/sweep.png")
    p.add_argument("--generations", type=int, default=6)
    p.add_argument("--width", type=int, default=512)
    p.add_argument("--height", type=int, default=80)
    p.add_argument("--seed", type=int, default=7)
    args = p.parse_args()

    gap = 4
    rows = [(r, a) for r in ROUGHNESS for a in AMPS]
    strips = [strip(args.generations, r, a, args.width, args.height, args.seed + i)
              for i, (r, a) in enumerate(rows)]

    sheet = Image.new("RGBA", (args.width, len(strips) * args.height + (len(strips) - 1) * gap), (0, 0, 0, 255))
    for i, s in enumerate(strips):
        sheet.paste(s, (0, i * (args.height + gap)))

    os.makedirs(os.path.dirname(args.out) or ".", exist_ok=True)
    sheet.convert("RGB").save(args.out)
    print(f"wrote {args.out} ({sheet.width}x{sheet.height}, {len(strips)} variants, "
          f"generations={args.generations})")


if __name__ == "__main__":
    main()
