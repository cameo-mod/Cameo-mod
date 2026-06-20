#!/usr/bin/env python3
"""Procedural horizontal lightning-bolt sprite generator for Cameo-mod tesla zaps.

Generates a realistic *slithering* plasma arc running left->right: a smooth sinuous
channel (summed low-frequency waves + a touch of high-frequency wobble) with a
white-hot core, layered electric-blue bloom, and occasional tapering branches.
RGBA sprite sheet, one independent flicker per frame. License-clean procedural art
(numpy + Pillow).

This is the look prototype / building block for a more realistic tesla zap.

Usage:
    python tools/gen_lightning.py
    python tools/gen_lightning.py --frames 4 --amp 0.22 --branches 2
"""
from __future__ import annotations

import argparse
import os
import random

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from PIL.PngImagePlugin import PngInfo


def _lerp(a, b, t):
    return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)


def bolt_path(x0, x1, yc, segments, amp, soft_prob, round_frac, rng, h):
    """Main channel: random-height anchors connected by a MIX of hard (sharp) and
    soft (rounded) turns, like real lightning. Each interior anchor is either kept as
    a sharp corner or rounded into a short quadratic arc through its neighbours."""
    n = max(3, segments)
    axs = np.linspace(x0, x1, n + 1)
    for i in range(1, n):                                    # jitter interior anchors along x
        axs[i] += rng.uniform(-0.4, 0.4) * (x1 - x0) / n
    ays = [yc + rng.uniform(-0.1, 0.1) * amp * h]            # endpoints stay near centre
    ays += [yc + rng.uniform(-amp, amp) * h for _ in range(n - 1)]
    ays += [yc + rng.uniform(-0.1, 0.1) * amp * h]
    anchors = list(zip(axs.tolist(), ays))

    pts = [anchors[0]]
    for i in range(1, n):
        prv, p, nxt = anchors[i - 1], anchors[i], anchors[i + 1]
        if rng.random() < soft_prob:                         # soft turn: rounded arc
            a = _lerp(p, prv, round_frac)
            b = _lerp(p, nxt, round_frac)
            steps = 8
            for s in range(steps + 1):
                t = s / steps
                mt = 1.0 - t
                pts.append((mt * mt * a[0] + 2 * mt * t * p[0] + t * t * b[0],
                            mt * mt * a[1] + 2 * mt * t * p[1] + t * t * b[1]))
        else:                                                # hard turn: sharp corner
            pts.append(p)
    pts.append(anchors[n])
    return pts


def slither_branch(sx, sy, ang, length, amp, rng, h):
    """A short tapering offshoot that curves away from the main channel and fades out."""
    n = max(6, int(length / 4))
    t = np.linspace(0.0, 1.0, n)
    dx, dy = np.cos(ang), np.sin(ang)
    bx = sx + t * length * dx
    by = sy + t * length * dy

    px, py = -dy, dx                                         # perpendicular wobble, tapered to the tip
    a = amp * h * 0.5
    f = rng.uniform(2.0, 4.0) * np.pi
    ph = rng.uniform(0, 2 * np.pi)
    wob = a * np.sin(f * t + ph) * (1.0 - t)
    return (bx + px * wob).tolist(), (by + py * wob).tolist()


def draw_polyline(draw, pts, width, value):
    w = max(1, width)
    for a, b in zip(pts[:-1], pts[1:]):
        draw.line([a, b], fill=value, width=w)
    r = w // 2
    if r:
        for x, y in pts:
            draw.ellipse([x - r, y - r, x + r, y + r], fill=value)


def draw_tapered(draw, pts, w0):
    """Branch: width and brightness fall off toward the tip."""
    n = len(pts)
    for i, (a, b) in enumerate(zip(pts[:-1], pts[1:])):
        frac = 1.0 - i / max(1, n - 1)
        draw.line([a, b], fill=int(40 + 160 * frac), width=max(1, int(round(w0 * frac))))


def build_core(args, rng):
    """Bright channel (+ branches) of one bolt, rendered supersampled then downscaled."""
    ss = args.supersample
    w, h = args.width * ss, args.height * ss
    img = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(img)

    yc = h / 2 + rng.uniform(-h * 0.06, h * 0.06)
    pts = bolt_path(2 * ss, w - 2 * ss, yc, args.segments, args.amp, args.soft, args.round_frac, rng, h)
    draw_polyline(d, pts, int(round(args.core_width * ss)), 255)

    for _ in range(args.branches):
        i = rng.randint(len(pts) // 6, len(pts) * 5 // 6)
        sx, sy = pts[i]
        ang = rng.choice([-1, 1]) * rng.uniform(0.5, 1.15)
        length = rng.uniform(0.12, 0.30) * w
        bx, by = slither_branch(sx, sy, ang, length, args.amp, rng, h)
        draw_tapered(d, list(zip(bx, by)), int(round(args.core_width * ss * 0.7)))

    return img.resize((args.width, args.height), Image.LANCZOS)


def colorize(core_img, glow_img, args):
    core = np.asarray(core_img, dtype=np.float32) / 255.0
    glow = np.asarray(glow_img, dtype=np.float32) / 255.0
    intensity = np.clip(core + glow * args.glow_gain, 0.0, 1.0)

    c_glow = np.array(args.rgb_glow, dtype=np.float32)
    c_core = np.array(args.rgb_core, dtype=np.float32)
    t = np.clip(core * 1.3, 0.0, 1.0)[..., None]             # core drives the white-hot centre
    rgb = c_glow[None, None, :] * (1.0 - t) + c_core[None, None, :] * t

    out = np.zeros((args.height, args.width, 4), dtype=np.float32)
    out[..., :3] = rgb
    out[..., 3] = np.clip(intensity * args.opacity * 255.0, 0.0, 255.0)
    out[..., :3] *= (out[..., 3:4] > 0)
    return out.astype(np.uint8)


def build(args):
    rng = random.Random(args.seed)
    frames = []
    for _ in range(args.frames):
        core = build_core(args, rng)
        a = np.asarray(core, np.float32)
        g1 = np.asarray(core.filter(ImageFilter.GaussianBlur(args.glow_radius)), np.float32)
        g2 = np.asarray(core.filter(ImageFilter.GaussianBlur(args.glow_radius * 2.6)), np.float32)
        g3 = np.asarray(core.filter(ImageFilter.GaussianBlur(args.glow_radius * 5.5)), np.float32)
        glow = np.clip(g1 + 0.6 * g2 + 0.35 * g3, 0, 255).astype(np.uint8)
        frames.append(colorize(core, Image.fromarray(glow), args))

    sheet = np.zeros((args.height, args.frames * args.width, 4), dtype=np.uint8)
    for i, fr in enumerate(frames):
        sheet[:, i * args.width:(i + 1) * args.width] = fr
    return Image.fromarray(sheet, "RGBA")


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--out", default="tools/lightning_preview.png")
    p.add_argument("--frames", type=int, default=4)
    p.add_argument("--width", type=int, default=256)
    p.add_argument("--height", type=int, default=64)
    p.add_argument("--amp", type=float, default=0.22, help="turn amplitude (frac of height)")
    p.add_argument("--segments", type=int, default=10, help="number of turns along the channel")
    p.add_argument("--soft", type=float, default=0.3, help="probability a turn is soft/rounded vs hard/sharp (0=all sharp, 1=all rounded)")
    p.add_argument("--round-frac", type=float, default=0.42, dest="round_frac", help="how rounded a soft turn is")
    p.add_argument("--branches", type=int, default=2)
    p.add_argument("--core-width", type=float, default=2.0, dest="core_width")
    p.add_argument("--glow-radius", type=float, default=1.6, dest="glow_radius")
    p.add_argument("--glow-gain", type=float, default=1.0, dest="glow_gain")
    p.add_argument("--opacity", type=float, default=1.0)
    p.add_argument("--supersample", type=int, default=3)
    p.add_argument("--rgb-core", type=int, nargs=3, default=(255, 255, 255), dest="rgb_core")
    p.add_argument("--rgb-glow", type=int, nargs=3, default=(110, 170, 255), dest="rgb_glow")
    p.add_argument("--seed", type=int, default=1)
    args = p.parse_args()

    img = build(args)
    os.makedirs(os.path.dirname(args.out) or ".", exist_ok=True)
    meta = PngInfo()
    meta.add_text("FrameSize", f"{args.width},{args.height}")
    meta.add_text("FrameAmount", str(args.frames))
    img.save(args.out, pnginfo=meta)
    print(f"wrote {args.out} ({img.width}x{img.height}, {args.frames} frames, "
          f"FrameSize {args.width}x{args.height})")


if __name__ == "__main__":
    main()
