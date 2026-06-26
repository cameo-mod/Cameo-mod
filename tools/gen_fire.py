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


def vtile_billow(fs: int, octaves: int, rng: np.random.Generator) -> np.ndarray:
    """Billow (turbulence) noise that wraps vertically, in [0, 1].

    Billow = sum of |signed noise| octaves. Unlike plain value noise (smooth
    gradients), the absolute value creates rounded, overlapping cauliflower-style
    lobes -- the signature voluminous structure of real fire/cloud art (this is
    what the smooth-cone `cluster`/`tongue` styles lack vs the Factorio reference).

    The coarse grid's bottom row is forced equal to its top row before upsampling,
    so the field tiles seamlessly top-to-bottom. Scrolling it upward over the loop
    (see `build`) then reads as turbulence rising and never breaks the loop.
    """
    out = np.zeros((fs, fs), dtype=np.float32)
    amp = 1.0
    amp_total = 0.0
    for o in range(octaves):
        g = max(2, 2 * (2 ** o))
        grid = (rng.random((g + 1, g)).astype(np.float32) * 2.0 - 1.0)
        grid[-1, :] = grid[0, :]                 # wrap vertically (seamless scroll)
        up = Image.fromarray(grid, mode="F").resize((fs, fs), Image.BICUBIC)
        out += amp * np.abs(np.asarray(up, dtype=np.float32))
        amp_total += amp
        amp *= 0.5
    out /= amp_total
    return np.clip(out, 0.0, 1.0)


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


def build_puff_frames(args, rng, fs, stops):
    """Metaball/puff fire: the body is composited from many discrete rounded puffs that
    rise, grow, cool and fade in a column -- the structure real/Factorio fire actually
    has (a cauliflower of 3D-lit lobes), which a single shaped noise field can't make.

    Each puff is a soft Gaussian disc with its own hot-centre/cool-rim temperature. Heat
    accumulates where puffs overlap, so dense regions (the base, and lobe overlaps) go
    white-hot while the sparse crown stays deep red -- the bright-lobe look. The loop is
    seamless because every puff's life `t = (phase + offset) mod 1` wraps, and the life
    envelope fades each puff to zero alpha at both ends so the wrap is invisible.
    """
    k = args.puff_count
    ys, xs = np.mgrid[0:fs, 0:fs].astype(np.float32)
    cx = fs * 0.5

    # Per-puff constants (seeded once, reused every frame). Integer wander frequencies
    # keep the horizontal wander periodic in t so the loop stays seamless.
    offs = ((np.arange(k) + 0.5) / k).astype(np.float32)        # even phases -> full column
    rng.shuffle(offs)
    x0 = (rng.random(k).astype(np.float32) * 2 - 1)             # base column offset [-1,1]
    rad0 = (0.6 + 0.7 * rng.random(k)).astype(np.float32)       # radius variation
    wamp = (0.10 + 0.18 * rng.random(k)).astype(np.float32)     # wander amplitude (frame frac)
    wfreq = rng.integers(1, 3, k).astype(np.float32)            # wander freq (integer -> periodic)
    wphase = (rng.random(k) * 2 * np.pi).astype(np.float32)
    heatk = (0.8 + 0.5 * rng.random(k)).astype(np.float32)      # per-puff heat jitter

    base_px = args.base_y * fs                                   # foot height (pixels from top)
    rise_px = args.tip_height * fs                              # how far a puff climbs
    rad_px = args.puff_radius * fs                              # base puff radius (frame frac)

    frames = []
    n = args.frames
    for f in range(n):
        phase = f / n
        heat = np.zeros((fs, fs), dtype=np.float32)
        cover = np.zeros((fs, fs), dtype=np.float32)
        hot = np.zeros((fs, fs), dtype=np.float32)      # per-puff lit highlights
        for i in range(k):
            t = (phase + offs[i]) % 1.0                         # this puff's life 0..1
            # Eased rise: puffs climb slower early, so they linger low and pack the hot
            # base into a dense bright bed (Factorio's solid white-hot foot).
            trise = t ** args.puff_rise_ease
            cyp = base_px - trise * rise_px
            spread = args.width * fs * (1.0 - 0.35 * trise)
            wander = wamp[i] * fs * np.sin(2 * np.pi * wfreq[i] * t + wphase[i])
            cxp = cx + x0[i] * spread + wander
            # Grows as it rises; fades in at birth and out at death so the wrap is unseen.
            r = rad_px * rad0[i] * (0.55 + 0.8 * trise)
            life = smoothstep(t / args.puff_fadein) * smoothstep((1.0 - t) / args.puff_fadeout)
            # Flat-topped, crisp-rimmed profile (exponent > 1 on the normalised radius):
            # a soft gaussian reads as a fuzzy orb; this keeps each lobe solid in the middle
            # with a defined edge so they stack into a mass, like the painted Factorio puffs.
            d2 = ((xs - cxp) ** 2 + (ys - cyp) ** 2) / (r * r)
            disc = np.exp(-(d2 ** args.puff_sharp))
            # A small bright highlight on the UPPER side of each puff: the fire's own glow
            # catches the top of each lobe, giving it 3D roundness (the painted Factorio
            # cue). Offset up by part of the radius, tighter than the puff itself.
            hlr = (args.puff_hl_size * r) ** 2
            hot += np.exp(-(((xs - cxp) ** 2 + (ys - (cyp - 0.35 * r)) ** 2) / hlr)) * life
            # Temperature is driven by SCREEN HEIGHT (white-hot at the dense foot, cooling
            # to deep red at the crown) -- this is the vertical gradient that reads as fire.
            # Per-puff jitter keeps adjacent lobes at slightly different heats so the body
            # boils instead of banding.
            height_t = np.clip((base_px - cyp) / max(rise_px, 1.0), 0.0, 1.0)
            ptemp = (args.heat_base - (args.heat_base - args.puff_tip_temp) * height_t) * heatk[i]
            heat += disc * life * ptemp
            cover += disc * life
        # Coverage-weighted average temperature (NOT additive -- additive blows the whole
        # mass to white). Overlaps stay as hot as their hottest puff, the foot reads white,
        # the crown red. Alpha is a soft saturating union of coverage (solid heart, soft
        # lobed edges). A small extra brightening where coverage is very high gives the
        # white-hot dense cores without washing out the rest.
        temp = heat / np.maximum(cover, 1e-3)
        temp = (temp + 0.10 * np.clip(cover - 1.0, 0.0, None)
                + args.puff_hl_strength * np.clip(hot, 0.0, 1.5))
        temp = np.clip(temp * args.puff_gain, 0.0, 1.0)
        rgb = color_ramp(temp, stops)
        alpha = (1.0 - np.exp(-cover * args.puff_alpha)) * args.opacity
        alpha = np.clip(alpha * smoothstep(np.clip(temp, 0.0, 1.0) / args.temp_floor), 0.0, 1.0)
        out = np.zeros((fs, fs, 4), dtype=np.float32)
        out[..., :3] = rgb * (alpha[..., None] > 0)
        out[..., 3] = alpha * 255.0
        frames.append(np.clip(out, 0, 255).astype(np.uint8))
    return frames


def _billow_column(args, fs, xs, ys, up, foot, cx_px, height, bill_lo, bill_hi, frac):
    """One slim billow flame column (the `billow`/b6 look) centred at cx_px with its own
    height and scroll phase `frac`. Returns (temp, alpha) arrays. Factored out so the
    `campfire` style can forge several of these into one fire."""
    def scroll(field, speed):
        shift = (frac % 1.0) * fs * speed
        i0 = int(np.floor(shift))
        fr = shift - i0
        return (1.0 - fr) * np.roll(field, -i0, axis=0) + fr * np.roll(field, -(i0 + 1), axis=0)

    turb = 0.42 * scroll(bill_lo, 1.0) + 0.58 * scroll(bill_hi, 1.8)
    turb = np.clip(turb, 0.0, 1.0) ** args.billow_gamma
    up_pos = np.clip(up, 0.0, 1.0)
    swayb = args.sway * fs * (turb - 0.5) * (0.3 + 0.9 * up_pos)
    xcb = (xs - cx_px - swayb) / fs
    prof = np.clip(1.0 - up_pos / np.maximum(height, 1e-3), 0.0, 1.0)
    half = np.clip(args.width * (0.45 + 0.75 * prof), 1e-3, None)
    rx = xcb / half
    widthmod = 0.70 + 0.65 * turb
    body = np.exp(-(rx * rx) / widthmod) * (0.45 + 1.15 * turb)
    thr = args.tongue_thr + args.tongue_rise * up_pos
    dens = smoothstep((body - thr) / args.tongue_soft)
    tip = height + 0.14 * (turb - 0.5)
    dens *= smoothstep((tip - up) / args.tip_soft)
    dens = np.clip(dens * foot, 0.0, 1.0)
    core = np.exp(-(rx * rx) / args.core_width) * np.clip(1.0 - up_pos / args.core_top, 0.0, 1.0)
    mott = (1.0 - args.billow_mottle) + 2.0 * args.billow_mottle * turb
    temp = np.clip((args.heat_base * dens + args.heat_core * core) * mott - args.heat_tip * up_pos, 0.0, 1.0)
    alpha = np.clip(dens * args.opacity, 0.0, 1.0)
    alpha = np.clip(alpha + core * dens * args.core_alpha, 0.0, 1.0) * smoothstep(dens / args.temp_floor)
    return temp, np.clip(alpha, 0.0, 1.0)


def build_campfire_frames(args, rng, fs, stops):
    """Campfire: forge `--campfire-count` slim billow flames side by side, feet overlapping,
    centre tongues tallest and outer ones shorter (a tent profile), so they merge into one
    multi-tongued blaze. Each column has its own turbulence fields and scroll phase, so they
    flicker independently. Max-merged (hottest/most-opaque column wins each pixel) so overlaps
    stay bright with no dark seams. Loops seamlessly (every column's scroll wraps)."""
    n = args.campfire_count
    ys, xs = np.mgrid[0:fs, 0:fs].astype(np.float32)
    base_y = max(args.base_y, 1e-3)
    yn = ys / (fs - 1)
    spread = args.campfire_spread * fs

    # Rounded base: the foot follows a DOME (curving up toward the edges) instead of a flat
    # horizontal line, so the bottom of the campfire reads as a rounded mound rather than a
    # cut-off bar (the centre tongues reach the ground, the outer ones stop higher). xnorm is
    # ~ -1..1 across the tongue spread.
    half_spread = max(args.campfire_spread * 0.5, 1e-3)
    xnorm = np.clip(np.abs(xs / (fs - 1) - 0.5) / half_spread, 0.0, 1.0)
    # Raised-cosine base: a GENTLE rounded curve that is flat at the centre AND levels off at the
    # edges (zero slope at both ends), so the base reads as a soft mound -- not a circle arc, which
    # has near-vertical walls at its edges that looked like a cut-off half-pipe.
    arc = 0.5 * (1.0 - np.cos(np.pi * xnorm))
    base_y_x = np.clip(base_y - args.campfire_dome * arc, 0.05, 1.0)
    up = (base_y_x - yn) / np.maximum(base_y_x, 1e-3)
    foot = smoothstep(up / args.foot_soft)

    subs = []
    for j in range(n):
        fx = (j / (n - 1) - 0.5) if n > 1 else 0.0          # -0.5 (left) .. +0.5 (right)
        cxj = fs * 0.5 + fx * spread + (rng.random() - 0.5) * args.campfire_jitter * fs
        tent = 1.0 - (abs(fx) * 2.0) * (1.0 - args.campfire_min_h)   # centre tall, edges short
        hj = args.tip_height * tent
        lo = vtile_billow(fs, args.octaves, rng)
        hi = vtile_billow(fs, args.octaves + 2, rng)
        subs.append((cxj, hj, lo, hi, rng.random()))

    # Unified base bed: one wide, short, soft-edged hot ellipse at the foot. The separate
    # tongues all rise out of it, so their individual bottoms merge into a single smooth
    # CONVEX rounded base (the ellipse bulges gently down) instead of a scalloped/winged edge
    # or a concave valley. This is what gives the "shallow arc" base without a cut or half-pipe.
    bed_cx = fs * 0.5
    bed_cy = base_y * (fs - 1)
    bed_rx = max(args.campfire_bed_w * fs, 1.0)
    bed_ry = max(args.campfire_bed_h * fs, 1.0)
    bed = np.exp(-(((xs - bed_cx) / bed_rx) ** 2 + ((ys - bed_cy) / bed_ry) ** 2))
    bed_temp = np.clip(args.campfire_bed_heat * bed, 0.0, 1.0)
    bed_alpha = np.clip(bed * args.opacity, 0.0, 1.0)

    # Ragged downward-licking fringe at the base (Factorio's signature): many thin tapering
    # flame fingers hang below the mass with dark gaps between them, instead of a smooth edge.
    # Two high-frequency vertical-streak fields define the finger pattern; blending them with a
    # periodic phase makes the fingers flicker/dance over the loop. xnorm_signed limits the
    # fringe to the flame's width.
    fr_a = streak_noise(fs, 1, args.fringe_fingers, 3, rng)
    fr_b = streak_noise(fs, 1, args.fringe_fingers, 3, rng)
    xc_frame = xs / (fs - 1) - 0.5
    fringe_xmask = np.exp(-((xc_frame / max(args.campfire_spread * 0.6, 1e-3)) ** 2))
    yb = (yn - base_y) / max(args.fringe_len, 1e-3)          # 0 at foot, 1 at fringe bottom

    frames = []
    nf = args.frames
    for f in range(nf):
        ph = 2.0 * np.pi * f / nf
        # Gentle breathing so the coals bed isn't dead-static against the flickering tongues.
        flick = 0.9 + 0.1 * np.cos(ph)
        temp_acc = bed_temp * flick
        alpha_acc = bed_alpha * flick
        for (cxj, hj, lo, hi, po) in subs:
            tj, aj = _billow_column(args, fs, xs, ys, up, foot, cxj, hj, lo, hi, (f / nf) + po)
            temp_acc = np.maximum(temp_acc, tj)
            alpha_acc = np.maximum(alpha_acc, aj)

        # Downward fringe: threshold rises with depth so each finger tapers to a point and the
        # gaps widen lower down (the licking, ragged bottom). Flickers via the periodic blend.
        fringe = np.clip(0.5 + np.cos(ph) * (fr_a - 0.5) + np.sin(ph) * (fr_b - 0.5), 0.0, 1.0)
        below = np.clip(yb, 0.0, 1.0)
        inband = ((yb >= -0.05) & (yb <= 1.0)).astype(np.float32)
        # Sharper threshold (crisper fingers, clear dark gaps) that rises steeply with depth so
        # each lick tapers to a point. finger_temp stays in the ORANGE band (not white) so the
        # licks read as flame, not a pale smudge; they cool toward their tips.
        finger = smoothstep((fringe - (0.40 + 0.55 * below)) / 0.07) * inband * fringe_xmask
        finger_temp = np.clip(0.72 - 0.45 * below, 0.0, 1.0)
        finger_alpha = np.clip(finger * args.opacity, 0.0, 1.0)
        temp_acc = np.where(finger_alpha > alpha_acc, finger_temp, temp_acc)
        alpha_acc = np.maximum(alpha_acc, finger_alpha)

        rgb = color_ramp(temp_acc, stops)
        out = np.zeros((fs, fs, 4), dtype=np.float32)
        out[..., :3] = rgb * (alpha_acc[..., None] > 0)
        out[..., 3] = alpha_acc * 255.0
        frames.append(np.clip(out, 0, 255).astype(np.uint8))
    return frames


def downsample_frame(frame, ofs, ss):
    """Premultiplied LANCZOS downsample for soft, anti-aliased edges (premultiply so
    transparent pixels don't bleed dark colour into the flame edge)."""
    if ss <= 1:
        return frame
    fim = Image.fromarray(frame, mode="RGBA")
    pm = np.asarray(fim, dtype=np.float32)
    pm[..., :3] *= pm[..., 3:4] / 255.0
    small = Image.fromarray(np.clip(pm, 0, 255).astype(np.uint8), "RGBA").resize(
        (ofs, ofs), Image.LANCZOS)
    arr = np.asarray(small, dtype=np.float32)
    a8 = arr[..., 3:4]
    arr[..., :3] = np.where(a8 > 0, np.clip(arr[..., :3] * 255.0 / np.maximum(a8, 1e-3), 0, 255), 0)
    return np.clip(arr, 0, 255).astype(np.uint8)


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
    # Billow turbulence fields for the `billow` style (rounded lobed structure).
    # Two layers scrolling at different rates so the body churns instead of rigidly
    # sliding; both wrap vertically for a seamless loop.
    bill_lo = vtile_billow(fs, args.octaves, rng)
    bill_hi = vtile_billow(fs, args.octaves + 2, rng)

    ys, xs = np.mgrid[0:fs, 0:fs].astype(np.float32)
    yn = ys / (fs - 1)          # 0 at top, 1 at bottom
    cx = fs * 0.5
    # The flame foot sits at base_y (a frame fraction, near center) and rises upward,
    # so the sprite can be placed on the actor with only a small Offset like the SHP art.
    base_y = max(args.base_y, 1e-3)
    up = (base_y - yn) / base_y   # 1 at top, 0 at the foot, <0 below the foot
    foot = smoothstep(up / args.foot_soft)  # gate everything below the foot

    stops = PALETTES[args.palette]

    if args.style in ("puff", "campfire"):
        builders = {
            "puff": build_puff_frames,
            "campfire": build_campfire_frames,
        }
        builder = builders[args.style]
        raw = builder(args, rng, fs, stops)
        frames = [downsample_frame(fr, ofs, ss) for fr in raw]
        fs = ofs
        return _assemble(frames, args, fs, n)

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

        if args.style == "billow":
            # Factorio-matched look: the body is BILLOW TURBULENCE (rounded overlapping
            # lobes), not a smooth cone. Animated by a seamless upward scroll of the
            # vertically-tiling billow fields, so the turbulence reads as rising fire and
            # loops perfectly. Two layers scroll at different speeds so the mass churns.
            def scroll(field, speed):
                shift = (f / n) * fs * speed
                i0 = int(np.floor(shift))
                frac = shift - i0
                return ((1.0 - frac) * np.roll(field, -i0, axis=0)
                        + frac * np.roll(field, -(i0 + 1), axis=0))

            turb = 0.42 * scroll(bill_lo, 1.0) + 0.58 * scroll(bill_hi, 1.8)
            # Deepen the valleys between lobes: a gamma > 1 pushes low turbulence toward 0
            # so dark gaps open up between the rounded lobes (Factorio reads as distinct
            # overlapping puffs, not one filled blob).
            turb = np.clip(turb, 0.0, 1.0) ** args.billow_gamma

            up_pos = np.clip(up, 0.0, 1.0)
            # Horizontal sway: lean the column left/right, more near the crown.
            swayb = args.sway * fs * (turb - 0.5) * (0.3 + 0.9 * up_pos)
            xcb = (xs - cx - swayb) / fs

            # Column width envelope: wide dense foot tapering toward the tip.
            prof = np.clip(1.0 - up_pos / np.maximum(args.tip_height, 1e-3), 0.0, 1.0)
            half = np.clip(args.width * (0.45 + 0.75 * prof), 1e-3, None)
            rx = xcb / half

            # Lobed silhouette: turbulence locally WIDENS or PINCHES the column (it scales
            # the Gaussian's falloff rate, not just the brightness), so the outline bulges
            # outward into rounded lobes where turbulence is high and necks in where it is
            # low -- the cauliflower edge. The central peak stays solid (heart never holed);
            # thr rises with height so the base is a bright bed and the crown shreds into
            # separate licks.
            widthmod = 0.70 + 0.65 * turb
            body = np.exp(-(rx * rx) / widthmod) * (0.45 + 1.15 * turb)
            thr = args.tongue_thr + args.tongue_rise * up_pos
            dens = smoothstep((body - thr) / args.tongue_soft)
            tip = args.tip_height + 0.14 * (turb - 0.5)
            dens *= smoothstep((tip - up) / args.tip_soft)      # soft tip cap
            dens = np.clip(dens * foot, 0.0, 1.0)

            # Temperature: white-hot dense base, cooling and darkening toward the tip;
            # turbulence mottles the heart strongly so the core boils into clearly bright
            # and dim lobes (Factorio's adjacent white-hot vs deep-orange puffs) instead of
            # one flat gradient. `billow_mottle` sets how hard that bright/dim swing is.
            core = np.exp(-(rx * rx) / args.core_width) * np.clip(1.0 - up_pos / args.core_top, 0.0, 1.0)
            mott = (1.0 - args.billow_mottle) + 2.0 * args.billow_mottle * turb
            temp = np.clip(
                (args.heat_base * dens + args.heat_core * core) * mott
                - args.heat_tip * up_pos,
                0.0, 1.0)
            rgb = color_ramp(temp, stops)
            alpha = np.clip(dens * args.opacity, 0.0, 1.0)
            alpha = np.clip(alpha + core * dens * args.core_alpha, 0.0, 1.0)
            alpha *= smoothstep(dens / args.temp_floor)
        elif args.style == "cluster":
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
        frame = downsample_frame(frame, ofs, ss)
        frames.append(frame)

    return _assemble(frames, args, ofs, n)


def _assemble(frames, args, fs, n) -> Image.Image:
    """Lay the finished frames out as a contact strip (--contact) or a packed grid sheet."""
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
    p.add_argument("--billow-gamma", type=float, default=1.6, help="billow: lobe contrast (>1 deepens dark gaps between lobes)")
    p.add_argument("--billow-mottle", type=float, default=0.5, help="billow: internal bright/dim lobe swing (0=flat, 1=strong)")
    p.add_argument("--seed", type=int, default=7)
    p.add_argument("--octaves", type=int, default=5)
    p.add_argument("--contact", action="store_true", help="emit a flat 1-row strip instead of a sheet")
    p.add_argument("--style", choices=("tongue", "cluster", "billow", "puff", "campfire"), default="cluster",
                   help="campfire = several slim billow tongues forged together; puff = Factorio metaball lobes; billow = turbulence lobes; cluster = streak-carved cone; tongue = single soft flame")
    # Campfire style (forge N slim billow columns)
    p.add_argument("--campfire-count", type=int, default=6, help="campfire: number of slim flame tongues forged together")
    p.add_argument("--campfire-spread", type=float, default=0.46, help="campfire: how far the tongue feet spread (frame frac)")
    p.add_argument("--campfire-min-h", type=float, default=0.5, help="campfire: outer-tongue height as a fraction of the centre tongue")
    p.add_argument("--campfire-jitter", type=float, default=0.04, help="campfire: random horizontal jitter of each tongue foot")
    p.add_argument("--campfire-dome", type=float, default=0.0, help="campfire: curves the foot line up at the edges; 0 = flat (the base bed does the rounding instead)")
    p.add_argument("--campfire-bed-w", type=float, default=0.15, help="campfire: base-bed half-width (frame frac) unifying the tongue feet into one rounded base")
    p.add_argument("--campfire-bed-h", type=float, default=0.05, help="campfire: base-bed half-height (frame frac); small = shallow convex bottom")
    p.add_argument("--campfire-bed-heat", type=float, default=1.0, help="campfire: base-bed temperature (1 = white-hot)")
    p.add_argument("--fringe-fingers", type=int, default=11, help="campfire: number of downward-licking base flame fingers (Factorio ragged base)")
    p.add_argument("--fringe-len", type=float, default=0.16, help="campfire: how far the base fingers lick downward (frame frac); 0 disables")
    # Puff (metaball) style
    p.add_argument("--puff-count", type=int, default=34, help="puff: number of rising puffs (more = denser cauliflower)")
    p.add_argument("--puff-radius", type=float, default=0.16, help="puff: base puff radius (frame frac)")
    p.add_argument("--puff-gain", type=float, default=1.0, help="puff: heat->white-hot gain (higher = brighter cores)")
    p.add_argument("--puff-alpha", type=float, default=2.2, help="puff: coverage->alpha steepness (higher = more solid)")
    p.add_argument("--puff-fadein", type=float, default=0.18, help="puff: birth fade-in fraction of life")
    p.add_argument("--puff-fadeout", type=float, default=0.45, help="puff: death fade-out fraction of life")
    p.add_argument("--puff-tip-temp", type=float, default=0.28, help="puff: temperature at the crown (lower = redder/darker top)")
    p.add_argument("--puff-sharp", type=float, default=1.5, help="puff: lobe edge sharpness (1=soft gaussian, >1=flat-top crisp rim)")
    p.add_argument("--puff-rise-ease", type=float, default=1.3, help="puff: >1 makes puffs linger low (denser hot base)")
    p.add_argument("--puff-hl-size", type=float, default=0.5, help="puff: highlight size relative to puff radius")
    p.add_argument("--puff-hl-strength", type=float, default=0.35, help="puff: highlight brightness boost (3D lit-lobe cue)")
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
