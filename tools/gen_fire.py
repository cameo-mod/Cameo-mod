#!/usr/bin/env python3
"""Procedural RGBA looping-flame sprite-sheet generator for Cameo-mod.

License-clean: generates original art from numpy fractal noise. Replaces the
classic palette burning-fire SHPs (fire1..fire4, 46x46 / 15 frames) with a
modern soft RGBA flame that loops seamlessly.

The loop is made seamless by driving all flicker from temporally periodic noise:
n(x, y, t) = cos(theta) * A(x, y) + sin(theta) * B(x, y), with
theta = 2*pi*t/N + climb*(1 - y/H). Adding a y-dependent phase makes the
flicker travel upward (fire rising) without breaking the period-N loop, so the
last frame wraps cleanly into the first.

Output is a single PNG sprite sheet (COLS x ceil(N/COLS) frames) carrying the
FrameSize / FrameAmount PNG text chunks OpenRA's PngSheet loader reads. Pass
--contact to also dump a flat one-row strip for eyeballing every frame.

Usage:
    python tools/gen_fire.py
    python tools/gen_fire.py --out mods/cameo/bits/effects/fire_rgba.png --contact
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


def streak_noise(fs: int, vfreq: int, hfreq: int, octaves: int,
                 rng: np.random.Generator) -> np.ndarray:
    """Vertically-stretched value noise: smooth in y, detailed in x, so thresholding
    it carves a cone into separate vertical tongues. Returns [0,1]."""
    out = np.zeros((fs, fs), dtype=np.float32)
    amp = 1.0
    amp_total = 0.0
    for o in range(octaves):
        gh = max(2, vfreq * (2 ** o))
        gw = max(2, hfreq * (2 ** o))
        grid = rng.random((gh, gw)).astype(np.float32)
        up = Image.fromarray(grid, mode="F").resize((fs, fs), Image.BICUBIC)
        out += amp * np.asarray(up, dtype=np.float32)
        amp_total += amp
        amp *= 0.55
    out /= amp_total
    return np.clip(out, 0.0, 1.0)


# Temperature -> RGB ramps (cool tip .. hot foot). Lower presets keep more of the
# flame in deep red and shrink the white-hot core.
PALETTES = {
    # original: red -> orange -> yellow -> white. Brightest / hottest look.
    "hot": [
        (0.00, (0, 0, 0)),
        (0.18, (150, 26, 8)),
        (0.42, (240, 92, 18)),
        (0.68, (255, 176, 54)),
        (0.92, (255, 232, 150)),
        (1.00, (255, 248, 224)),
    ],
    # redder: more of the body stays red/deep-orange, small warm core, no white.
    "red": [
        (0.00, (0, 0, 0)),
        (0.22, (110, 12, 6)),
        (0.52, (190, 36, 12)),
        (0.78, (235, 86, 22)),
        (0.93, (255, 150, 56)),
        (1.00, (255, 196, 110)),
    ],
    # deep ember: nearly all red, only a faint orange heart. Darkest / most menacing.
    "ember": [
        (0.00, (0, 0, 0)),
        (0.24, (90, 8, 4)),
        (0.58, (165, 26, 10)),
        (0.82, (210, 56, 16)),
        (0.95, (240, 104, 32)),
        (1.00, (255, 150, 60)),
    ],
}


def color_ramp(temp: np.ndarray, stops) -> np.ndarray:
    """Map temperature in [0,1] to RGB by piecewise-linear interpolation of stops.

    stops is a list of (pos, (r, g, b)); positions must be ascending in [0,1].
    """
    pos = np.array([s[0] for s in stops], dtype=np.float32)
    cols = np.array([s[1] for s in stops], dtype=np.float32)
    r = np.interp(temp, pos, cols[:, 0])
    g = np.interp(temp, pos, cols[:, 1])
    b = np.interp(temp, pos, cols[:, 2])
    return np.stack([r, g, b], axis=-1)


def build(args) -> Image.Image:
    rng = np.random.default_rng(args.seed)
    n = args.frames
    ofs = args.frame_size                 # output frame size
    ss = max(1, args.supersample)
    fs = ofs * ss                         # internal working res (downsampled for soft edges)

    # Two independent noise fields drive the periodic-in-time flicker.
    a = fractal_noise(fs, fs, args.octaves, rng) - 0.5
    b = fractal_noise(fs, fs, args.octaves, rng) - 0.5
    # A finer field breaks up the flame tip into separate tongues.
    a_hi = fractal_noise(fs, fs, args.octaves + 1, rng) - 0.5
    b_hi = fractal_noise(fs, fs, args.octaves + 1, rng) - 0.5
    # Vertically-stretched streak fields carve the cluster style into separate tongues.
    sa = streak_noise(fs, args.streak_v, args.tongues, 3, rng) - 0.5
    sb = streak_noise(fs, args.streak_v, args.tongues, 3, rng) - 0.5
    # Fine high-frequency fields add Factorio-like granular flecks to the body.
    a_f = fractal_noise(fs, fs, args.octaves + 3, rng) - 0.5
    b_f = fractal_noise(fs, fs, args.octaves + 3, rng) - 0.5

    ys, xs = np.mgrid[0:fs, 0:fs].astype(np.float32)
    yn = ys / (fs - 1)          # 0 at top, 1 at bottom
    cx = fs * 0.5
    # The flame foot sits at base_y (a frame fraction, near center) and rises upward,
    # so the sprite can be placed on the actor with only a small Offset like the SHP art.
    base_y = max(args.base_y, 1e-3)
    up = (base_y - yn) / base_y   # 1 at top, 0 at the foot, <0 below the foot
    foot = smoothstep(up / args.foot_soft)  # gate everything below the foot

    stops = PALETTES[args.palette]

    frames = []
    for f in range(n):
        phase = 2.0 * np.pi * f / n
        climb = args.climb
        theta = phase + climb * up
        flick = np.cos(theta) * a + np.sin(theta) * b
        theta_hi = phase * args.tip_speed + climb * 1.6 * up
        flick_hi = np.cos(theta_hi) * a_hi + np.sin(theta_hi) * b_hi
        theta_f = phase * args.tip_speed * 1.3 + climb * 2.1 * up
        flick_f = np.cos(theta_f) * a_f + np.sin(theta_f) * b_f   # fine grain

        # Horizontal sway: the flame column leans left/right per row, more near the top.
        sway = args.sway * fs * flick * (0.25 + 0.75 * up)
        xc = (xs - cx - sway) / fs

        if args.style == "cluster":
            # Factorio-style bonfire: a squat, wide, DENSE body that only breaks into
            # separate wispy licks near the crown. One continuous "fire field" (cone +
            # streaks + fine turbulence) is cut by a height-rising threshold, so the base
            # stays a solid bright bed while the top splinters into thin tongues.
            streak = np.cos(theta) * sa + np.sin(theta) * sb
            # squat wide cone: little vertical taper, broad footprint
            prof = np.clip(1.0 - up / np.maximum(args.tip_height, 1e-3), 0.0, 1.0)
            half = np.clip(args.width * (0.55 + 0.7 * prof), 1e-3, None)
            rx = xc / half
            cone = np.exp(-(rx * rx) ** 1.1)

            # Carve into licks at the crown and outer edges, but keep the hot center
            # dense (no holes punched through the core). edge ~0 at center, ~1 outside.
            up_pos = np.clip(up, 0.0, 1.0)
            edge = 1.0 - np.exp(-(rx * rx) / 0.25)
            carve_w = smoothstep(up_pos / 0.3) * (0.35 + 0.65 * edge)
            field = (cone
                     + streak * args.tongue_contrast * carve_w
                     + flick_hi * 0.18)
            thr = args.tongue_thr + args.tongue_rise * up_pos
            tip = args.tip_height + args.tip_jitter * flick_hi + 0.18 * streak
            vert = smoothstep((tip - up) / args.tip_soft)
            dens = smoothstep((field - thr) / args.tongue_soft) * vert * foot

            # Big hot core fills most of the lower body; mottle speckles the brightness.
            core = np.exp(-(rx * rx) / args.core_width) * np.clip(1.0 - up / args.core_top, 0.0, 1.0)
            # Mottle can boost above 1 (bright speckles, clipped to white) as well as dim;
            # flick_f adds the fine granular flecks that read as many small flames.
            mottle = 0.85 + 0.45 * (flick_hi + 0.8 * streak) + args.grain * flick_f
            temp = np.clip(
                (args.heat_base * dens + args.heat_core * core) * mottle
                - args.heat_tip * up
                + flick_hi * args.heat_jitter,
                0.0, 1.0)
            rgb = color_ramp(temp, stops)
            # Alpha from density (keeps dark-red tips visible); core keeps the heart solid.
            alpha = np.clip(dens * args.opacity, 0.0, 1.0)
            alpha = np.clip(alpha + core * dens * args.core_alpha, 0.0, 1.0)
            alpha *= smoothstep(dens / args.temp_floor)
        else:
            # Per-column flickering tip: each x licks to a different height, breaking the
            # crown into separate tongues that travel upward with the climbing phase.
            tip = args.tip_height + args.tip_jitter * flick_hi + args.tip_break * flick
            vert = smoothstep((tip - up) / args.tip_soft)

            # Width tapers from a wide base (prof~1) to a point at the tip (prof~0).
            prof = np.clip(1.0 - up / np.maximum(args.tip_height, 1e-3), 0.0, 1.0) ** 0.7
            half = np.clip(args.width * (0.22 + 0.95 * prof), 1e-3, None)
            rx = xc / half

            col = np.exp(-(rx * rx) ** 1.3)
            edge = np.clip(1.0 + args.roughness * (flick + 0.6 * flick_hi), 0.0, 1.6)
            dens = np.clip(col * edge, 0.0, 1.0) * vert * foot

            # Temperature: hottest in the lower-core, cooling toward edges and tip.
            core = np.exp(-(rx * rx) / args.core_width) * np.clip(1.0 - up / args.core_top, 0.0, 1.0)
            temp = np.clip(
                args.heat_base * dens
                + args.heat_core * core
                - args.heat_tip * up
                + flick_hi * args.heat_jitter,
                0.0, 1.0)
            temp *= smoothstep(dens / args.temp_floor)

            rgb = color_ramp(temp, stops)
            alpha = np.clip(dens * args.opacity * smoothstep((1.0 - yn) / args.base_fade + 0.15), 0.0, 1.0)
            # Lift alpha where it is hot so the bright core stays solid.
            alpha = np.clip(alpha + core * args.core_alpha, 0.0, 1.0)
            alpha *= smoothstep(temp / 0.08)

        out = np.zeros((fs, fs, 4), dtype=np.float32)
        out[..., :3] = rgb * (alpha[..., None] > 0)
        out[..., 3] = alpha * 255.0
        frame = np.clip(out, 0, 255).astype(np.uint8)
        if ss > 1:
            # Downsample for soft, anti-aliased edges (premultiply so transparent
            # pixels don't bleed dark color into the flame edge).
            fim = Image.fromarray(frame, mode="RGBA")
            pm = np.asarray(fim, dtype=np.float32)
            pm[..., :3] *= pm[..., 3:4] / 255.0
            small = Image.fromarray(np.clip(pm, 0, 255).astype(np.uint8), "RGBA").resize(
                (ofs, ofs), Image.LANCZOS)
            arr = np.asarray(small, dtype=np.float32)
            a8 = arr[..., 3:4]
            arr[..., :3] = np.where(a8 > 0, np.clip(arr[..., :3] * 255.0 / np.maximum(a8, 1e-3), 0, 255), 0)
            frame = np.clip(arr, 0, 255).astype(np.uint8)
        frames.append(frame)
    fs = ofs

    if args.contact:
        strip = np.zeros((fs, n * fs, 4), dtype=np.uint8)
        for i, fr in enumerate(frames):
            strip[:, i * fs:(i + 1) * fs] = fr
        return Image.fromarray(strip, mode="RGBA")

    cols = args.cols
    rows = int(np.ceil(n / cols))
    sheet = np.zeros((rows * fs, cols * fs, 4), dtype=np.uint8)
    for i, fr in enumerate(frames):
        r, c = divmod(i, cols)
        sheet[r * fs:(r + 1) * fs, c * fs:(c + 1) * fs] = fr
    return Image.fromarray(sheet, mode="RGBA")


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--out", default="mods/cameo/bits/effects/fire_rgba.png")
    p.add_argument("--frames", type=int, default=24)
    p.add_argument("--cols", type=int, default=8)
    p.add_argument("--frame-size", type=int, default=48)
    p.add_argument("--supersample", type=int, default=2, help="internal render scale, downsampled for soft edges")
    p.add_argument("--grain", type=float, default=0.4, help="cluster: fine granular fleck strength")
    p.add_argument("--seed", type=int, default=7)
    p.add_argument("--octaves", type=int, default=5)
    p.add_argument("--contact", action="store_true", help="emit a flat 1-row strip instead of a sheet")
    p.add_argument("--style", choices=("tongue", "cluster"), default="cluster",
                   help="cluster = Factorio-style multi-tongue (production); tongue = single soft flame")
    # Cluster style
    p.add_argument("--tongues", type=int, default=9, help="cluster: horizontal streak count (tongue density)")
    p.add_argument("--streak-v", type=int, default=2, help="cluster: vertical streak coherence (lower = taller licks)")
    p.add_argument("--tongue-contrast", type=float, default=1.8, help="cluster: streak strength carving the crown")
    p.add_argument("--tongue-thr", type=float, default=0.34, help="cluster: base cutoff (low = solid bright bed)")
    p.add_argument("--tongue-rise", type=float, default=0.7, help="cluster: crown thinning into separate licks")
    p.add_argument("--tongue-soft", type=float, default=0.16, help="cluster: tongue edge softness")
    # Motion
    p.add_argument("--climb", type=float, default=5.0, help="upward phase travel (flame rise)")
    p.add_argument("--tip-speed", type=float, default=2.0, help="tip flicker speed multiplier")
    p.add_argument("--sway", type=float, default=0.05, help="horizontal lean amplitude (frame frac)")
    p.add_argument("--roughness", type=float, default=0.35, help="noise breakup of the body")
    # Shape
    p.add_argument("--width", type=float, default=0.32, help="half-width of the flame column (frame frac)")
    p.add_argument("--base-y", type=float, default=1.0, help="frame fraction where the flame foot sits")
    p.add_argument("--foot-soft", type=float, default=0.05, help="softness of the flame foot gate")
    p.add_argument("--tip-height", type=float, default=0.85, help="how high the flame reaches (up frac); lower = squatter")
    p.add_argument("--tip-jitter", type=float, default=0.08)
    p.add_argument("--tip-soft", type=float, default=0.22, help="softness of the flame tip")
    p.add_argument("--tip-break", type=float, default=0.6, help="noise breakup into separate tongues")
    # Heat / color
    p.add_argument("--heat-base", type=float, default=0.72)
    p.add_argument("--heat-core", type=float, default=0.9)
    p.add_argument("--heat-tip", type=float, default=0.30)
    p.add_argument("--heat-jitter", type=float, default=0.12)
    p.add_argument("--core-width", type=float, default=0.28)
    p.add_argument("--core-top", type=float, default=0.65)
    p.add_argument("--core-alpha", type=float, default=0.35)
    p.add_argument("--palette", choices=sorted(PALETTES), default="hot",
                   help="color ramp: hot (orig), red, ember (reddest)")
    p.add_argument("--temp-floor", type=float, default=0.22)
    # Alpha
    p.add_argument("--opacity", type=float, default=0.92)
    p.add_argument("--base-fade", type=float, default=0.12, help="soften the very bottom edge")
    args = p.parse_args()

    img = build(args)
    out_dir = os.path.dirname(args.out)
    if out_dir:
        os.makedirs(out_dir, exist_ok=True)
    meta = PngInfo()
    meta.add_text("FrameSize", f"{args.frame_size},{args.frame_size}")
    meta.add_text("FrameAmount", str(args.frames))
    img.save(args.out, pnginfo=meta)
    grid = "1x%d strip" % args.frames if args.contact else \
        "%dx%d grid" % (args.cols, int(np.ceil(args.frames / args.cols)))
    print(f"wrote {args.out}  ({img.width}x{img.height}, {args.frames} frames, {grid}, "
          f"FrameSize {args.frame_size}x{args.frame_size})")


if __name__ == "__main__":
    main()
