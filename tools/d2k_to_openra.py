#!/usr/bin/env python3
"""
Dune 2000 to OpenRA sprite conversion tool.

Converts individual Dune 2000 BMP frames into OpenRA-compatible PNG spritesheets.
Handles:
  1. Pink (255,0,255) background -> transparent alpha
  2. Hue-shifting the player-color ramp to a target hue (default 300 = magenta)
  3. Combining all frames into a single horizontal strip PNG

Usage:
  python tools/d2k_to_openra.py <input_dir> <output_png> [--hue 300] [--prefix KodaBody]

The input directory must contain numbered BMP frames (e.g. KodaBody_0.bmp, KodaBody_1.bmp, ...).
Frames are sorted by their numeric suffix and laid out left-to-right in the spritesheet.

The output PNG is an RGBA horizontal strip, one frame wide per facing.
Frame dimensions are normalized to the largest frame in the set (smaller frames
are centered on a transparent canvas of the max size).

Hue shifting:
  Dune 2000 sprites use a green player-color ramp (hue ~163).
  This script shifts all pixels in that hue range to the target hue (default 300).
  The saturation and value are preserved; only the hue changes.
  Pixels outside the green hue range (roughly 140-190 degrees) are left untouched,
  EXCEPT for the pink background which becomes fully transparent.

  The player-color remap range can be customized with --remap-hue-min and --remap-hue-max
  (defaults: 140 and 190 degrees, covering the D2K green ramp).

  To skip hue shifting entirely (e.g. for chassis frames that have no player color),
  use --no-hue-shift.
"""

import argparse
import os
import re
import sys

try:
    import numpy as np
    from PIL import Image
except ImportError:
    print("numpy and Pillow are required: pip install numpy Pillow", file=sys.stderr)
    sys.exit(1)


def convert_frame(img, target_hue, do_hue_shift, remap_min=140, remap_max=190):
    """Convert a single BMP frame to RGBA with transparency and optional hue shift."""
    rgba = img.convert("RGBA")
    arr = np.array(rgba, dtype=np.float32)  # H x W x 4

    r, g, b = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2]

    # Pink background (255,0,255) -> transparent
    pink_mask = (r == 255) & (g == 0) & (b == 255)
    arr[pink_mask, 3] = 0  # alpha = 0
    arr[pink_mask, :3] = 0  # rgb = 0

    if do_hue_shift:
        # Convert to HSV using vectorized operations
        maxc = np.maximum(np.maximum(r, g), b)
        minc = np.minimum(np.minimum(r, g), b)
        v = maxc / 255.0
        delta = maxc - minc
        s = np.where(maxc > 0, delta / np.maximum(maxc, 1), 0.0)

        # Compute hue
        rc = np.where(delta > 0, (maxc - r) / np.maximum(delta, 1), 0.0)
        gc = np.where(delta > 0, (maxc - g) / np.maximum(delta, 1), 0.0)
        bc = np.where(delta > 0, (maxc - b) / np.maximum(delta, 1), 0.0)

        h = np.where(r == maxc, bc - gc,
            np.where(g == maxc, 2.0 + rc - bc, 4.0 + gc - rc))
        h = (h / 6.0) % 1.0
        hue_deg = h * 360.0

        # Mask for pixels in the remap hue range with sufficient saturation/value
        remap_mask = (s > 0.1) & (v > 0.05) & (hue_deg >= remap_min) & (hue_deg <= remap_max)

        if np.any(remap_mask):
            new_h = np.full_like(h, target_hue / 360.0)
            # Only apply to remap_mask pixels
            h_final = np.where(remap_mask, new_h, h)
            s_final = s
            v_final = v

            # HSV -> RGB
            i = (h_final * 6.0).astype(int)
            f = h_final * 6.0 - i
            p = v_final * (1.0 - s_final)
            q = v_final * (1.0 - s_final * f)
            t = v_final * (1.0 - s_final * (1.0 - f))
            i_mod = i % 6

            nr = np.where(i_mod == 0, v_final,
                 np.where(i_mod == 1, q,
                 np.where(i_mod == 2, p,
                 np.where(i_mod == 3, p,
                 np.where(i_mod == 4, t, v_final)))))
            ng = np.where(i_mod == 0, t,
                 np.where(i_mod == 1, v_final,
                 np.where(i_mod == 2, v_final,
                 np.where(i_mod == 3, q,
                 np.where(i_mod == 4, p, p)))))
            nb = np.where(i_mod == 0, p,
                 np.where(i_mod == 1, p,
                 np.where(i_mod == 2, t,
                 np.where(i_mod == 3, v_final,
                 np.where(i_mod == 4, v_final, q)))))

            # Only apply to remap_mask
            arr[:, :, 0] = np.where(remap_mask, nr * 255, r)
            arr[:, :, 1] = np.where(remap_mask, ng * 255, g)
            arr[:, :, 2] = np.where(remap_mask, nb * 255, b)

    return Image.fromarray(arr.astype(np.uint8), "RGBA")


def find_frames(input_dir, prefix=None):
    """Find and sort BMP frames by numeric suffix."""
    frames = []
    pattern = re.compile(r"^(.+?)_(\d+)\.bmp$")
    for f in os.listdir(input_dir):
        if not f.endswith(".bmp"):
            continue
        m = pattern.match(f)
        if m:
            name_prefix = m.group(1)
            idx = int(m.group(2))
            if prefix is None or name_prefix == prefix:
                frames.append((idx, os.path.join(input_dir, f)))
    frames.sort(key=lambda x: x[0])
    return [path for _, path in frames]


def make_spritesheet(input_dir, output_path, target_hue=300, prefix=None,
                     do_hue_shift=True, remap_min=140, remap_max=190):
    """Convert all BMP frames in input_dir to a single horizontal PNG spritesheet."""
    frame_paths = find_frames(input_dir, prefix)
    if not frame_paths:
        print(f"Error: no BMP frames found in {input_dir}"
              + (f" with prefix '{prefix}'" if prefix else ""),
              file=sys.stderr)
        sys.exit(1)

    print(f"Found {len(frame_paths)} frames in {input_dir}")

    # Determine max frame dimensions
    max_w, max_h = 0, 0
    for path in frame_paths:
        img = Image.open(path)
        max_w = max(max_w, img.width)
        max_h = max(max_h, img.height)

    print(f"  Max frame size: {max_w}x{max_h}")
    print(f"  Spritesheet size: {max_w * len(frame_paths)}x{max_h}")

    # Create the spritesheet
    sheet = Image.new("RGBA", (max_w * len(frame_paths), max_h), (0, 0, 0, 0))

    for i, path in enumerate(frame_paths):
        img = Image.open(path)
        frame = convert_frame(img, target_hue, do_hue_shift, remap_min, remap_max)
        # Center the frame on a canvas of max_w x max_h
        offset_x = (max_w - frame.width) // 2
        offset_y = (max_h - frame.height) // 2
        sheet.paste(frame, (i * max_w + offset_x, offset_y))

    # Add PNG text chunks so OpenRA can split the strip into individual frames
    from PIL.PngImagePlugin import PngInfo
    frame_count = len(frame_paths)
    frame_size = f"{max_w},{max_h}"
    metadata = PngInfo()
    metadata.add_text("FrameAmount", str(frame_count))
    metadata.add_text("FrameSize", frame_size)
    sheet.save(output_path, "PNG", pnginfo=metadata)

    print(f"  Saved: {output_path} ({sheet.width}x{sheet.height})")
    print(f"  FrameAmount={frame_count}, FrameSize={frame_size}")
    return sheet


def main():
    parser = argparse.ArgumentParser(
        description="Convert Dune 2000 BMP frames to OpenRA PNG spritesheet"
    )
    parser.add_argument("input_dir", help="Directory containing BMP frames")
    parser.add_argument("output_png", help="Output PNG spritesheet path")
    parser.add_argument("--hue", type=float, default=300,
                        help="Target hue for player color (default: 300 = magenta)")
    parser.add_argument("--prefix", default=None,
                        help="Frame filename prefix (e.g. KodaBody). If omitted, all BMPs are used.")
    parser.add_argument("--no-hue-shift", action="store_true",
                        help="Skip hue shifting (for frames without player color)")
    parser.add_argument("--remap-hue-min", type=float, default=140,
                        help="Minimum hue degree to remap (default: 140)")
    parser.add_argument("--remap-hue-max", type=float, default=190,
                        help="Maximum hue degree to remap (default: 190)")
    args = parser.parse_args()

    make_spritesheet(
        args.input_dir,
        args.output_png,
        target_hue=args.hue,
        prefix=args.prefix,
        do_hue_shift=not args.no_hue_shift,
        remap_min=args.remap_hue_min,
        remap_max=args.remap_hue_max,
    )


if __name__ == "__main__":
    main()
