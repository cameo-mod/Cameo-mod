#!/usr/bin/env python
"""Normalize production Volcanic cliff shadows with one family calibration."""

from __future__ import annotations

import argparse
import json
import shutil
from pathlib import Path

import numpy as np
from PIL import Image

import generate_sh04_alpha_beach_prototype as shore
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
BITS = ROOT / "mods/cameo/bits/volcanic"
NAMES = tuple(f"s{i:02d}" for i in range(1, 39)) + tuple(
    f"wc{i:02d}" for i in range(1, 39)
)
SHADOW_STRENGTH = 0.38
SHADOW_PERCENTILE = 35.0
SHADOW_TARGET = np.asarray((12.0, 8.0, 8.0), dtype=np.float32)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out-dir", type=Path, required=True)
    parser.add_argument("--install", action="store_true")
    args = parser.parse_args()
    out = args.out_dir.resolve()
    candidates = out / "candidate-vols"
    candidates.mkdir(parents=True, exist_ok=True)

    palette = np.asarray(shore.read_palette(BITS / "volcanic.pal"), dtype=np.uint8)
    sources = {name: load_24(BITS / f"{name}.vol") for name in NAMES}
    low, threshold = family_calibration(sources, palette)

    records = []
    panels = []
    for name, (width, height, frames24) in sources.items():
        output_frames = []
        changed = 0
        for source24 in frames24:
            output24 = normalize(source24, palette, low, threshold)
            changed += int(np.count_nonzero(output24 != source24))
            output48 = np.repeat(np.repeat(output24, 2, axis=0), 2, axis=1)
            output_frames.append(output48.tobytes())

        path = candidates / f"{name}.vol"
        write_shptd(path, width, height, output_frames)
        audit(path, len(output_frames))
        if args.install:
            shutil.copy2(path, BITS / path.name)

        records.append({"asset": name, "frames": len(output_frames), "changed_pixels_24px": changed})
        if name in {"s01", "s08", "s24", "s35", "wc01", "wc11", "wc24", "wc35"}:
            before = render_composite(frames24, palette)
            after24 = [normalize(frame, palette, low, threshold) for frame in frames24]
            after = render_composite(after24, palette)
            panels.extend(((f"{name}: before", before), (f"{name}: shared shadows", after)))

    review = out / "cliff_shared_shadow_calibration_review.png"
    shore.write_review_sheet(review, panels, columns=2, scale=2)
    manifest = {
        "installed": args.install,
        "assets": len(NAMES),
        "land_cliffs": 38,
        "water_cliffs": 38,
        "shared_luma_low": low,
        "shared_shadow_threshold": threshold,
        "shadow_percentile": SHADOW_PERCENTILE,
        "shadow_strength": SHADOW_STRENGTH,
        "shadow_target_rgb": SHADOW_TARGET.tolist(),
        "hot_lava_protected": True,
        "strict_24_to_48_nearest_neighbor": True,
        "records": records,
        "review": str(review),
    }
    (out / "cliff_shared_shadow_calibration_audit.json").write_text(
        json.dumps(manifest, indent=2) + "\n", encoding="utf-8"
    )
    print(review)
    return 0


def load_24(path: Path):
    width, height, frames = read_shptd(path)
    if (width, height) != (48, 48):
        raise ValueError(f"{path.name}: expected 48x48 frames")
    result = []
    for frame in frames:
        image = np.frombuffer(frame, dtype=np.uint8).reshape(48, 48)
        blocks = image.reshape(24, 2, 24, 2).transpose(0, 2, 1, 3)
        if np.any(blocks != blocks[:, :, :1, :1]):
            raise ValueError(f"{path.name}: nonuniform 2x2 production blocks")
        result.append(image[0::2, 0::2].copy())
    return width, height, result


def family_calibration(sources, palette):
    values = []
    for _, _, frames in sources.values():
        for indices in frames:
            rgb = palette[indices]
            visible = indices != 0
            hot = (rgb[:, :, 0] > 95) & (rgb[:, :, 0] > rgb[:, :, 1] + 24)
            luma = luminance(rgb)
            values.append(luma[visible & ~hot])
    family = np.concatenate(values)
    return float(np.percentile(family, 3.0)), float(np.percentile(family, SHADOW_PERCENTILE))


def normalize(indices, palette, low, threshold):
    rgb_u8 = palette[indices]
    rgb = rgb_u8.astype(np.float32)
    luma = luminance(rgb)
    visible = indices != 0
    hot = (rgb[:, :, 0] > 95) & (rgb[:, :, 0] > rgb[:, :, 1] + 24)
    eligible = visible & ~hot & (luma < threshold)
    weight = np.clip((threshold - luma) / max(1.0, threshold - low), 0.0, 1.0)
    weight *= SHADOW_STRENGTH
    target = rgb * (1.0 - weight[:, :, None]) + SHADOW_TARGET * weight[:, :, None]

    result = indices.copy()
    if eligible.any():
        allowed = np.asarray(
            list(range(1, 4)) + list(range(5, 172)) + list(range(192, 256)),
            dtype=np.uint8,
        )
        choices = palette[allowed].astype(np.float32)
        pixels = target[eligible]
        distance = np.sum((pixels[:, None, :] - choices[None, :, :]) ** 2, axis=2)
        result[eligible] = allowed[np.argmin(distance, axis=1)]
    return result


def luminance(rgb):
    source = rgb.astype(np.float32)
    return source[:, :, 0] * 0.2126 + source[:, :, 1] * 0.7152 + source[:, :, 2] * 0.0722


def audit(path, expected_frames):
    width, height, frames = read_shptd(path)
    if (width, height, len(frames)) != (48, 48, expected_frames):
        raise ValueError(f"{path.name}: roundtrip metadata mismatch")
    for frame in frames:
        image = np.frombuffer(frame, dtype=np.uint8).reshape(48, 48)
        blocks = image.reshape(24, 2, 24, 2).transpose(0, 2, 1, 3)
        if np.any(blocks != blocks[:, :, :1, :1]):
            raise ValueError(f"{path.name}: cadence failure after write")


def render_composite(frames24, palette):
    columns = max(1, int(np.ceil(np.sqrt(len(frames24)))))
    rows = int(np.ceil(len(frames24) / columns))
    canvas = np.zeros((rows * 24, columns * 24, 3), dtype=np.uint8)
    canvas[:] = shore.BACKGROUND
    for index, frame in enumerate(frames24):
        row, column = divmod(index, columns)
        rgb = palette[frame]
        visible = frame != 0
        cell = canvas[row * 24:(row + 1) * 24, column * 24:(column + 1) * 24]
        cell[visible] = rgb[visible]
    return Image.fromarray(canvas, mode="RGB")


if __name__ == "__main__":
    raise SystemExit(main())
