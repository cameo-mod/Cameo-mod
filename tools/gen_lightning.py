#!/usr/bin/env python3
"""Procedural horizontal lightning-bolt sprite generator for Cameo-mod tesla zaps.

Generates a realistic *fractal* plasma arc running left->right via recursive midpoint
displacement (the same algorithm as the in-game LightningZap projectile): the channel
starts straight, then each generation splits every segment at its midpoint and nudges it
perpendicular to that local segment, with the nudge shrinking each pass, so every wiggle
sprouts smaller self-similar sub-wiggles. White-hot core, layered electric-blue bloom and
occasional tapering branches. RGBA sprite sheet, one independent flicker per frame.
License-clean procedural art (numpy + Pillow).

This mirrors the in-engine look so it can be eyeballed as a still without a rebuild.

Usage:
    python tools/gen_lightning.py
    python tools/gen_lightning.py --frames 4 --amp 0.22 --generations 6 --roughness 0.55
"""
from __future__ import annotations

import argparse
import math
import os
import random

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from PIL.PngImagePlugin import PngInfo


def bolt_path(x0, x1, yc, generations, roughness, amp, rng, h):
    """Main channel via fractal midpoint displacement. Start with the straight x0->x1 segment,
    then each generation split every segment at its midpoint and nudge it perpendicular to that
    LOCAL segment (so sub-wiggles follow their parent wiggle = self-similar fractal detail). The
    displacement shrinks by `roughness` each pass; the endpoints are never moved."""
    pts = [(x0, yc), (x1, yc)]
    offset = amp * h
    for _ in range(max(1, generations)):
        new = [pts[0]]
        for (ax, ay), (bx, by) in zip(pts[:-1], pts[1:]):
            mx, my = (ax + bx) / 2.0, (ay + by) / 2.0
            sx, sy = bx - ax, by - ay
            slen = math.hypot(sx, sy)
            if slen > 1e-3:
                px, py = -sy / slen, sx / slen
                disp = rng.uniform(-1.0, 1.0) * offset
                mx += px * disp
                my += py * disp
            new.append((mx, my))
            new.append((bx, by))
        pts = new
        offset *= roughness
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


def fractal_segment(p0, p1, generations, roughness, amp, rng):
    """Midpoint-displacement channel between two arbitrary points (same algorithm as the main bolt,
    but free-floating instead of horizontal). Endpoints pinned; each pass splits every segment and
    nudges the midpoint perpendicular to its LOCAL direction, shrinking by roughness."""
    pts = [p0, p1]
    offset = amp
    for _ in range(max(1, generations)):
        new = [pts[0]]
        for (ax, ay), (bx, by) in zip(pts[:-1], pts[1:]):
            mx, my = (ax + bx) / 2.0, (ay + by) / 2.0
            sx, sy = bx - ax, by - ay
            slen = math.hypot(sx, sy)
            if slen > 1e-3:
                px, py = -sy / slen, sx / slen
                disp = rng.uniform(-1.0, 1.0) * offset
                mx += px * disp
                my += py * disp
            new.append((mx, my))
            new.append((bx, by))
        pts = new
        offset *= roughness
    return pts


def draw_fractal_branch(draw, p0, ang, length, generations, roughness, chance, w0, rng, depth=0):
    """Jagged mini-bolt fork (mirrors the engine DrawFractalBranch): a fractal channel from p0 along
    `ang`, drawn crisp with width+brightness tapering to the tip, optionally spawning a shorter, thinner
    sub-fork from its first half. The wiggle scales with the branch's own length so short forks don't
    thrash; the same `roughness` as the main bolt gives matching self-similar character."""
    p1 = (p0[0] + math.cos(ang) * length, p0[1] + math.sin(ang) * length)
    pts = fractal_segment(p0, p1, generations, roughness, length * 0.18, rng)
    n = len(pts)
    for i, (a, b) in enumerate(zip(pts[:-1], pts[1:])):
        frac = 1.0 - i / max(1, n - 1)
        w = max(1, int(round(w0 * (0.35 + 0.65 * frac))))
        draw.line([a, b], fill=int(120 + 135 * frac), width=w)     # stays bright, only fades near the tip

    if depth < 2 and length > 12 and chance > 0 and rng.random() < chance:
        j = max(1, min(n - 1, n // 4 + int(rng.random() * n / 4)))
        sub_ang = ang + rng.choice([-1, 1]) * rng.uniform(0.4, 0.9)
        draw_fractal_branch(draw, pts[j], sub_ang, length * rng.uniform(0.4, 0.6),
                            generations, roughness, chance, max(1, int(w0 * 0.7)), rng, depth + 1)


def build_core(args, rng):
    """Bright channel (+ branches) of one bolt, rendered supersampled then downscaled."""
    ss = args.supersample
    w, h = args.width * ss, args.height * ss
    img = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(img)

    yc = h / 2 + rng.uniform(-h * 0.06, h * 0.06)
    pts = bolt_path(2 * ss, w - 2 * ss, yc, args.generations, args.roughness, args.amp, rng, h)
    draw_polyline(d, pts, int(round(args.core_width * ss)), 255)

    for _ in range(args.branches):
        i = rng.randint(len(pts) // 6, len(pts) * 5 // 6)
        sx, sy = pts[i]
        # Root on a real path vertex and fork relative to the LOCAL bolt direction there, so the branch
        # stays attached to the visible channel (matching the engine, which roots on main[idx]).
        nx, ny = pts[min(i + 1, len(pts) - 1)]
        base = math.atan2(ny - sy, nx - sx)
        ang = base + rng.choice([-1, 1]) * rng.uniform(0.45, 1.0)
        length = rng.uniform(0.14, 0.30) * w
        draw_fractal_branch(d, (sx, sy), ang, length, args.branch_generations, args.roughness,
                            args.sub_branch_chance, int(round(args.core_width * ss * 0.8)), rng)

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
    p.add_argument("--out", default="docs/lightning/preview.png")
    p.add_argument("--frames", type=int, default=4)
    p.add_argument("--width", type=int, default=256)
    p.add_argument("--height", type=int, default=64)
    p.add_argument("--amp", type=float, default=0.22, help="largest (first-pass) wiggle, frac of height")
    p.add_argument("--generations", type=int, default=6, help="fractal subdivision passes (segments = 2^generations)")
    p.add_argument("--roughness", type=float, default=0.7, help="how fast the wiggle shrinks each pass (0..1); higher=jaggier")
    p.add_argument("--branches", type=int, default=2)
    p.add_argument("--branch-generations", type=int, default=3, dest="branch_generations",
                   help="fractal subdivision passes per branch (mirrors LightningZap BranchGenerations)")
    p.add_argument("--sub-branch-chance", type=float, default=0.25, dest="sub_branch_chance",
                   help="chance a branch spawns a sub-fork (mirrors LightningZap SubBranchChance); 0 disables")
    p.add_argument("--core-width", type=float, default=2.0, dest="core_width")
    p.add_argument("--glow-radius", type=float, default=1.6, dest="glow_radius")
    p.add_argument("--glow-gain", type=float, default=1.0, dest="glow_gain")
    p.add_argument("--opacity", type=float, default=1.0)
    p.add_argument("--supersample", type=int, default=3)
    p.add_argument("--rgb-core", type=int, nargs=3, default=(255, 255, 255), dest="rgb_core")
    p.add_argument("--rgb-glow", type=int, nargs=3, default=(110, 170, 255), dest="rgb_glow")
    p.add_argument("--seed", type=int, default=1)
    p.add_argument("--bg", type=int, nargs=3, default=None, dest="bg",
                   help="composite over this solid RGB background (e.g. 18 22 32) instead of transparent; "
                        "the additive glow only reads against a dark scene like the game terrain")
    args = p.parse_args()

    img = build(args)
    if args.bg is not None:
        back = Image.new("RGBA", img.size, tuple(args.bg) + (255,))
        img = Image.alpha_composite(back, img)
    os.makedirs(os.path.dirname(args.out) or ".", exist_ok=True)
    meta = PngInfo()
    meta.add_text("FrameSize", f"{args.width},{args.height}")
    meta.add_text("FrameAmount", str(args.frames))
    img.save(args.out, pnginfo=meta)
    print(f"wrote {args.out} ({img.width}x{img.height}, {args.frames} frames, "
          f"FrameSize {args.width}x{args.height})")


if __name__ == "__main__":
    main()
