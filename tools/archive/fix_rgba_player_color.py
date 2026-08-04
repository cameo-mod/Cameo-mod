#!/usr/bin/env python
"""Audit and fix player-colour remapping on RGBA (player_rgba) sprites.

OpenRA RGBA sprites that use `PlayerPalette: player_rgba` are recoloured to the
owning player's colour by a PlayerColorShift trait. In this mod that trait keys
on a narrow MAGENTA band, so the sprite's player-colour region must be painted
PURE, FULLY-SATURATED, BRIGHT magenta. A region is remapped correctly only if it
satisfies all three axes:

  1. Hue   - linear-space hue in the gate (0.83, 0.84]. Pure magenta (R=B, G=0)
             sits at hue 0.8333 and nails it. Off-hue pixels are never shifted
             (they stay magenta in game).
  2. Sat   - ~1.0 (the trait's ReferenceSaturation). Under-saturated source
             pixels remap to a washed-out / pale player colour.
  3. Value - highlights must reach the convention brightness (~0.87-0.95, the
             trait's ReferenceValue). A region that never gets bright renders
             DIM even when hue and saturation are correct.

`--audit` is the workhorse: with no argument it discovers the sprites actually
wired to `PlayerPalette: player_rgba` (by reading the rules + sequences) and
flags each as WASHED-OUT, DIM, or ok, so you can find the rest of the backlog in
one pass. Pass explicit PATHs to audit specific files/dirs instead. It is
read-only. Scoping to player_rgba bodies matters - a blanket scan of every PNG
also flags icons, effects and index-palette art whose magenta is just artwork.

`--purify` and `--brighten` are INTERACTIVE single-file helpers - point them at
one sprite and then verify the result in game (launch the mod and eyeball the
unit). They are deliberately not a batch fixer: choosing the player-colour
region vs. intentional dark outlines/detail, and choosing a brightness target,
both need human judgement. A naive blanket "make everything magenta" can tint
intentional outlines and make a sprite worse.

Both write modes re-embed the PNG's `FrameAmount`/`FrameSize` text chunks, which
the engine needs to slice a sprite sheet - a plain PIL save drops them and the
sheet fails to load.

`--preview` simulates the in-game shift (the same hue gate + HSV math as
glsl/combined.frag) for a chosen player colour and writes a before(committed)/
after(working-tree) comparison PNG, so a fix can be confirmed without launching
the game. `--recolor` turns an arbitrary hue range (e.g. a fixed-blue emblem)
into the player colour, keeping only large connected blobs so scattered
same-hue noise elsewhere is left alone.

Usage:
  python tools/fix_rgba_player_color.py --audit [PATH ...]
  python tools/fix_rgba_player_color.py --purify SPRITE.png
  python tools/fix_rgba_player_color.py --brighten SPRITE.png [--target 0.90]
  python tools/fix_rgba_player_color.py --preview SPRITE.png [...] [--player R,G,B]
  python tools/fix_rgba_player_color.py --recolor SPRITE.png [--hue LO,HI]
"""

import argparse
import colorsys
import glob
import io
import os
import re
import subprocess
import sys
from collections import deque

from PIL import Image, ImageDraw
from PIL.PngImagePlugin import PngInfo

# --- convention constants -------------------------------------------------

# Broad magenta window (sRGB hue) used to locate the candidate player region.
REGION_HUE_LO, REGION_HUE_HI = 0.70, 0.95
REGION_MIN_SAT = 0.12

# Tighter window used when SELECTING pixels to rewrite: excludes the ~0.92
# pink/red accents some sprites use, while still covering the magenta body.
SELECT_HUE_LO, SELECT_HUE_HI = 0.78, 0.90

# Linear-space hue gate the engine's PlayerColorShift actually keys on.
GATE_LO, GATE_HI = 0.83, 0.84

# A pixel is considered already-correct magenta when it is in-gate and ~fully
# saturated.
PURE_MIN_SAT = 0.99

# Default brightness target for --brighten (between the d2k ~0.87 and the
# trait's ReferenceValue 0.95).
DEFAULT_TARGET_VALUE = 0.90

# Audit only considers a sprite once its magenta region is large enough to
# plausibly be a player region rather than an incidental accent.
AUDIT_MIN_REGION = 200

# Classification thresholds (tunable). A region that is mostly desaturated
# remaps washed-out (the Tesla failure); a well-saturated region whose
# highlights never get bright renders dim (the Boxer/Skyshield failure).
WASHED_OUT_SAT_PCT = 50.0   # fully-sat % below this -> washed-out
DIM_VMAX = 0.70             # Vmax below this (when saturated) -> dim

# Where to look when discovering / resolving sprites.
RULES_DIR = "mods/cameo/rules"
SEQUENCES_DIR = "mods/cameo/sequences"
BITS_DIR = "mods/cameo/bits"


def _srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def _linear_to_srgb(c):
    c = max(0.0, min(1.0, c))
    return 12.92 * c if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055


def _linear_hue(r, g, b):
    return colorsys.rgb_to_hsv(
        _srgb_to_linear(r / 255), _srgb_to_linear(g / 255), _srgb_to_linear(b / 255))[0]


def _in_region(h, s):
    return REGION_HUE_LO <= h <= REGION_HUE_HI and s > REGION_MIN_SAT


def _save_preserving_chunks(img, path, source):
    """Save img to path, re-embedding the text chunks (FrameAmount/FrameSize)."""
    meta = PngInfo()
    for key, value in getattr(source, "text", {}).items():
        meta.add_text(key, value)
    img.save(path, pnginfo=meta)


def _region_stats(path):
    """Return (region_px, in_gate_pct, fully_sat_pct, v_max) for the magenta
    player region, or None if the file is not an RGBA sprite worth auditing."""
    try:
        im = Image.open(path)
        if im.mode != "RGBA":
            # Indexed/paletted (.png with a palette) or RGB sprites don't use
            # the RGBA player-colour-shift path.
            return None
        px = list(im.convert("RGBA").getdata())
    except Exception:
        # Unreadable / truncated PNG - skip rather than abort the whole sweep.
        return None
    region = in_gate = fully_sat = 0
    v_max = 0.0
    for r, g, b, a in px:
        if a < 128:
            continue
        h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
        if not _in_region(h, s):
            continue
        region += 1
        if GATE_LO < _linear_hue(r, g, b) <= GATE_HI:
            in_gate += 1
            if v > v_max:
                v_max = v
        if s > PURE_MIN_SAT:
            fully_sat += 1
    if region == 0:
        return None
    return (region,
            100.0 * in_gate / region,
            100.0 * fully_sat / region,
            v_max)


def _iter_pngs(paths):
    for p in paths:
        if os.path.isdir(p):
            for root, _, files in os.walk(p):
                for f in files:
                    if f.lower().endswith(".png"):
                        yield os.path.join(root, f)
        elif p.lower().endswith(".png"):
            yield p


# --- discovery: which sprites are actually wired to player_rgba ----------
#
# Scoping the audit to player_rgba-wired body sprites matters: a blanket scan
# of every PNG flags icons, effects and index-palette art whose magenta is just
# artwork, massively over-reporting. We approximate the wiring statically:
#   rules: actor with `PlayerPalette: player_rgba` under RenderSprites -> image
#   sequences: image node's Filename(s) (Defaults + non-icon frames) -> PNG

def _top_blocks(path):
    """Yield (key, body_lines) for each column-0 `key:` block in a YAML file."""
    lines = open(path, encoding="utf-8").read().splitlines()
    cur, buf = None, []
    for l in lines:
        if l and l[0] not in " \t#" and l.rstrip().endswith(":"):
            if cur is not None:
                yield cur, buf
            cur, buf = l.rstrip()[:-1], []
        elif cur is not None:
            buf.append(l)
    if cur is not None:
        yield cur, buf


def _player_rgba_images():
    """Image names of actors that use PlayerPalette: player_rgba on their body."""
    images = set()
    pat = re.compile(r"RenderSprites:\s*(?:\n[ \t]+.*)*?PlayerPalette: player_rgba")
    for rf in glob.glob(os.path.join(RULES_DIR, "*.yaml")):
        for actor, buf in _top_blocks(rf):
            text = "\n".join(buf)
            if "PlayerPalette: player_rgba" not in text or not pat.search(text):
                continue
            image, in_rs = None, False
            for l in buf:
                s = l.strip()
                if s == "RenderSprites:":
                    in_rs = True
                    continue
                if in_rs and (l.startswith("\t\t") or l.startswith("        ")):
                    m = re.match(r"Image:\s*(\S+)", s)
                    if m:
                        image = m.group(1)
                elif in_rs:
                    in_rs = False
            images.add(image or actor)
    return images


def _sequence_pngs():
    """Map image name -> list of body PNG filenames (skipping icon frames)."""
    seq = {}
    for sf in glob.glob(os.path.join(SEQUENCES_DIR, "*.yaml")):
        for key, buf in _top_blocks(sf):
            fns, sub = [], "Defaults"
            for l in buf:
                s = l.strip()
                if l.startswith("\t") and not l.startswith("\t\t") and s.endswith(":"):
                    sub = s[:-1]
                m = re.match(r"Filename:\s*(\S+\.png)", s)
                if m and sub.lower() != "icon":
                    fns.append(m.group(1))
            if fns:
                seq[key] = fns
    return seq


def discover_player_rgba_sprites():
    """Return {png_path: image_name} for player_rgba body sprites."""
    images = _player_rgba_images()
    seq = _sequence_pngs()
    by_basename = {}
    for p in glob.glob(os.path.join(BITS_DIR, "**", "*.png"), recursive=True):
        by_basename.setdefault(os.path.basename(p).lower(), p)
    out = {}
    for image in images:
        for fn in seq.get(image) or seq.get(image.lower()) or []:
            p = by_basename.get(fn.lower())
            if p:
                out[os.path.normpath(p)] = image
    return out


DEFAULT_REPORT = os.path.join("docs", "rgba_player_color_audit.txt")


def cmd_audit(paths, out=DEFAULT_REPORT):
    if paths:
        targets = {p: None for p in _iter_pngs(paths)}
        scope = "given paths"
    else:
        targets = discover_player_rgba_sprites()
        scope = "player_rgba-wired body sprites"

    rows = []
    for png in targets:
        stats = _region_stats(png)
        if stats is None:
            continue
        region, in_gate_pct, sat_pct, v_max = stats
        if region < AUDIT_MIN_REGION:
            continue
        if sat_pct < WASHED_OUT_SAT_PCT:
            mode = "WASHED-OUT"
        elif v_max < DIM_VMAX:
            mode = "DIM"
        else:
            mode = "ok"
        rows.append((png, region, in_gate_pct, sat_pct, v_max, mode))

    order = {"WASHED-OUT": 0, "DIM": 1, "ok": 2}
    rows.sort(key=lambda r: (order[r[5]], r[3], r[4]))

    lines = ["%-56s %8s %8s %8s %6s  %s" %
             ("sprite", "region", "in-gate", "full-sat", "Vmax", "verdict")]
    counts = {"WASHED-OUT": 0, "DIM": 0, "ok": 0}
    for png, region, ig, sat, vmax, mode in rows:
        counts[mode] += 1
        lines.append("%-56s %8d %7.1f%% %7.1f%% %6.2f  %s" %
                     (os.path.relpath(png), region, ig, sat, vmax, mode))
    lines.append("")
    lines.append("Scope: %s. %d sprite(s) with a magenta region >= %d px."
                 % (scope, len(rows), AUDIT_MIN_REGION))
    lines.append("  WASHED-OUT (full-sat < %.0f%%): %d   DIM (Vmax < %.2f): %d   ok: %d"
                 % (WASHED_OUT_SAT_PCT, counts["WASHED-OUT"],
                    DIM_VMAX, counts["DIM"], counts["ok"]))
    lines.append("These are SCREENING candidates - a small magenta accent can trip "
                 "the filter. Confirm each in game before fixing.")

    report = "\n".join(lines)
    print(report)
    if out:
        with open(out, "w", encoding="utf-8") as fh:
            fh.write(report + "\n")
        print("\n(report written to %s)" % out)


def _select(r, g, b, a):
    if a < 1:
        return False
    h, s, _ = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return SELECT_HUE_LO <= h <= SELECT_HUE_HI and s > REGION_MIN_SAT


def cmd_purify(path):
    """Repaint the selected player region to pure magenta (V, 0, V): fix hue and
    saturation, preserve each pixel's brightness so shading survives."""
    src = Image.open(path)
    im = src.convert("RGBA")
    out = []
    changed = 0
    for r, g, b, a in im.getdata():
        if _select(r, g, b, a):
            v = max(r, g, b)
            out.append((v, 0, v, a))
            changed += 1
        else:
            out.append((r, g, b, a))
    im.putdata(out)
    _save_preserving_chunks(im, path, src)
    print("purified %d px in %s" % (changed, path))
    print("Now verify in game - the unit should track the player's colour.")


def cmd_brighten(path, target):
    """Scale the pure-magenta player pixels so their brightest highlight reaches
    `target`, preserving the relative shading gradient. Leaves dark outlines and
    non-player detail untouched."""
    src = Image.open(path)
    im = src.convert("RGBA")
    px = list(im.getdata())

    def is_pure(r, g, b):
        h, s, _ = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
        return (REGION_HUE_LO <= h <= REGION_HUE_HI and s > 0.9
                and GATE_LO < _linear_hue(r, g, b) <= GATE_HI)

    v_max = max((max(r, g, b) / 255 for r, g, b, a in px if a >= 128 and is_pure(r, g, b)),
                default=0.0)
    if v_max == 0.0:
        print("no pure-magenta player pixels found in %s - run --purify first?" % path)
        return
    scale = target / v_max
    out = []
    changed = 0
    for r, g, b, a in px:
        if a >= 128 and is_pure(r, g, b):
            v = min(max(r, g, b) / 255 * scale, 1.0)
            m = round(v * 255)
            out.append((m, 0, m, a))
            changed += 1
        else:
            out.append((r, g, b, a))
    im.putdata(out)
    _save_preserving_chunks(im, path, src)
    print("brightened %d px in %s by x%.2f (Vmax %.2f -> %.2f)" %
          (changed, path, scale, v_max, target))
    print("Now verify in game - the unit should match the brightness of its peers.")


# --- preview: simulate the in-game shift so a fix can be checked visually --

DEFAULT_PLAYER = (200, 35, 35)   # a clearly-saturated red
DEFAULT_PREVIEW = os.path.join("docs", "rgba_preview.png")


def _first_frame(img):
    """Crop frame 0 of a horizontal sprite sheet using its FrameAmount chunk."""
    fa = int(getattr(img, "text", {}).get("FrameAmount", "1") or 1)
    rgba = img.convert("RGBA")
    w, h = rgba.size
    return rgba.crop((0, 0, max(1, w // fa), h))


def _shift_to_player(frame, player_rgb):
    """Apply the engine's PlayerColorShift to a frame for a given player colour,
    mirroring glsl/combined.frag (hue gate + HSV shift, all in linear space).
    Lets us preview how a sprite's magenta region will actually remap."""
    pr, pg, pb = (_srgb_to_linear(c / 255) for c in player_rgb)
    ph, ps, pv = colorsys.rgb_to_hsv(pr, pg, pb)
    sh_h, sh_s, sh_v = ph - 0.835, ps - 1.0, pv / 0.95   # Reference Hue/Sat/Value
    out = frame.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a < 8:
                continue
            h, s, v = colorsys.rgb_to_hsv(
                _srgb_to_linear(r / 255), _srgb_to_linear(g / 255), _srgb_to_linear(b / 255))
            if GATE_LO < h <= GATE_HI:
                nh = (h + sh_h) % 1.0
                ns = max(0.0, min(1.0, s + sh_s))
                nv = v * min(sh_v, 1.0)
                nr, ng, nb = colorsys.hsv_to_rgb(nh, ns, nv)
                px[x, y] = (round(_linear_to_srgb(nr) * 255),
                            round(_linear_to_srgb(ng) * 255),
                            round(_linear_to_srgb(nb) * 255), a)
    return out


def _git_head_image(path):
    """Load the committed (HEAD) version of a PNG, or None if not tracked."""
    rel = path.replace(os.sep, "/")
    data = subprocess.run(["git", "show", "HEAD:" + rel], capture_output=True).stdout
    if not data:
        return None
    try:
        return Image.open(io.BytesIO(data))
    except Exception:
        return None


def cmd_preview(paths, player_rgb, out=DEFAULT_PREVIEW):
    """For each sprite render BEFORE (committed) vs AFTER (working tree), both
    run through the player-colour shift, so a fix can be eyeballed without
    launching the game. Frame 0 only."""
    S, GAP, LBL, COLS = 150, 8, 16, 2
    cells = []
    for p in paths:
        cur = Image.open(p)
        before = _git_head_image(p)
        a = _shift_to_player(_first_frame(cur), player_rgb)
        b = _shift_to_player(_first_frame(before), player_rgb) if before else None
        cells.append((os.path.basename(p), b, a))

    cw = 2 * S + GAP + 16
    rows = (len(cells) + COLS - 1) // COLS
    sheet = Image.new("RGBA", (COLS * cw, rows * (S + LBL) + 24), (60, 60, 60, 255))
    d = ImageDraw.Draw(sheet)
    d.text((8, 6), "player %r   each unit: LEFT=before  RIGHT=after" % (player_rgb,),
           fill=(255, 255, 255, 255))

    def fit(im):
        im = im.copy()
        im.thumbnail((S, S))
        return im

    for i, (name, b, a) in enumerate(cells):
        cx, cy = (i % COLS) * cw + 8, (i // COLS) * (S + LBL) + 24
        if b is not None:
            fb = fit(b)
            sheet.alpha_composite(fb, (cx, cy + (S - fb.height) // 2))
        fa = fit(a)
        sheet.alpha_composite(fa, (cx + S + GAP, cy + (S - fa.height) // 2))
        d.line((cx + S + GAP // 2, cy, cx + S + GAP // 2, cy + S), fill=(120, 120, 120, 255))
        d.text((cx, cy + S + 1), name[:26], fill=(255, 255, 0, 255))
    sheet.convert("RGB").save(out)
    print("wrote preview of %d sprite(s) to %s" % (len(cells), out))


# --- recolor: turn an arbitrary hue region into the player colour ----------

def cmd_recolor(path, hue_lo, hue_hi, min_component=40):
    """Convert a hue range (e.g. a fixed-blue emblem) into the player-colour
    magenta (V, 0, V), keeping only connected blobs of at least `min_component`
    px. The size filter keeps a coherent emblem and ignores scattered noise of
    the same hue elsewhere on the sprite."""
    src = Image.open(path)
    im = src.convert("RGBA")
    w, h = im.size
    px = im.load()

    sel = set()
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < 128:
                continue
            hue, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if hue_lo <= hue <= hue_hi and s > 0.18 and v > 0.10:
                sel.add((x, y))

    kept = set()
    seen = set()
    for start in sel:
        if start in seen:
            continue
        comp, q = [], deque([start])
        seen.add(start)
        while q:
            cx, cy = q.popleft()
            comp.append((cx, cy))
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (cx + dx, cy + dy)
                if n in sel and n not in seen:
                    seen.add(n)
                    q.append(n)
        if len(comp) >= min_component:
            kept.update(comp)

    for (x, y) in kept:
        r, g, b, a = px[x, y]
        m = max(r, g, b)
        px[x, y] = (m, 0, m, a)
    _save_preserving_chunks(im, path, src)
    print("recolored %d px (kept), left %d scattered px alone, in %s"
          % (len(kept), len(sel) - len(kept), path))
    print("Now verify in game - that region should track the player's colour.")


def main(argv):
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--audit", nargs="*", metavar="PATH",
                   help="audit player_rgba body sprites (default), or the given PATH(s)")
    g.add_argument("--purify", metavar="SPRITE", help="repaint region to pure magenta")
    g.add_argument("--brighten", metavar="SPRITE", help="raise magenta highlights to --target")
    g.add_argument("--preview", nargs="+", metavar="SPRITE",
                   help="render before(committed)/after(working) shift simulation to a PNG")
    g.add_argument("--recolor", metavar="SPRITE",
                   help="convert --hue range into the player colour (largest blobs only)")
    ap.add_argument("--target", type=float, default=DEFAULT_TARGET_VALUE,
                    help="brightness target for --brighten (default %.2f)" % DEFAULT_TARGET_VALUE)
    ap.add_argument("--hue", default="0.50,0.72",
                    help="hue range LO,HI for --recolor (default 0.50,0.72 = blue)")
    ap.add_argument("--player", default="200,35,35",
                    help="player colour R,G,B for --preview (default 200,35,35 = red)")
    ap.add_argument("--out", default=DEFAULT_REPORT,
                    help="output file: audit report, or --preview PNG (default per mode)")
    args = ap.parse_args(argv)

    if args.audit is not None:
        cmd_audit(args.audit, args.out)
    elif args.purify:
        cmd_purify(args.purify)
    elif args.brighten:
        cmd_brighten(args.brighten, args.target)
    elif args.preview:
        player = tuple(int(c) for c in args.player.split(","))
        out = args.out if args.out != DEFAULT_REPORT else DEFAULT_PREVIEW
        cmd_preview(args.preview, player, out)
    elif args.recolor:
        lo, hi = (float(c) for c in args.hue.split(","))
        cmd_recolor(args.recolor, lo, hi)


if __name__ == "__main__":
    main(sys.argv[1:])
