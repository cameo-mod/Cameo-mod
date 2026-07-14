#!/usr/bin/env python
"""Generate preview-only Volcanic roads through one deterministic palette LUT."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image

import generate_sh04_alpha_beach_prototype as shore
import prototype_donor_recolored_bridge_ground as strategy
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
ROADS = tuple(f"d{i:02d}" for i in range(1, 46))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out-dir", type=Path, required=True)
    parser.add_argument("--page-size", type=int, default=8)
    args = parser.parse_args()
    out = args.out_dir.resolve()
    vols = out / "candidate-vols"
    out.mkdir(parents=True, exist_ok=True)
    vols.mkdir(parents=True, exist_ok=True)

    temperate_palette = np.asarray(shore.read_palette(
        ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    ), dtype=np.uint8)
    volcanic_palette = np.asarray(shore.read_palette(
        ROOT / "mods/cameo/bits/volcanic/volcanic.pal"
    ), dtype=np.uint8)
    lookup, calibration = build_family_lookup(
        temperate_palette, volcanic_palette
    )
    donor_ground_indices = clear_ground_indices()
    production_ground = production_ground_frames()

    panels = []
    audit = []
    for name in ROADS:
        spec = shore.read_template_spec(
            ROOT / "mods/cameo/tilesets/ra_temperat.yaml", name + ".tem"
        )
        donor, domain = shore.read_sparse_composite(
            ROOT / "mods/cameo/bits/temp" / spec.image, spec
        )
        donor_rgb = shore.indices_rgb(donor, temperate_palette)
        donor_rgb[~domain] = shore.BACKGROUND

        old_indices, mixed_blocks, source24 = map_at_24(donor, domain, lookup)
        indices, ground_pixels = integrate_production_ground(
            old_indices,
            source24,
            domain,
            spec,
            donor_ground_indices,
            production_ground,
        )
        path = vols / (name + ".vol")
        write_template(path, indices, spec)

        installed = ROOT / "mods/cameo/bits/volcanic" / f"{name}.vol"
        old_result = read_template(installed, spec)
        old_rgb = shore.indices_rgb(old_result, volcanic_palette)
        old_rgb[~domain] = shore.BACKGROUND
        result_rgb = shore.indices_rgb(indices, volcanic_palette)
        result_rgb[~domain] = shore.BACKGROUND
        donor_image = Image.fromarray(donor_rgb, mode="RGB")
        old_image = Image.fromarray(old_rgb, mode="RGB")
        result_image = Image.fromarray(result_rgb, mode="RGB")
        donor_image.save(out / f"temperate_donor_{name}.png")
        old_image.save(out / f"installed_road_{name}.png")
        result_image.save(out / f"volcanic_road_candidate_{name}.png")
        panels.extend((
            (f"{name}: RA Temperate donor", donor_image),
            (f"{name}: installed global LUT", old_image),
            (f"{name}: production-ground integration", result_image),
        ))
        audit.append({
            "template": name,
            "size": [spec.columns, spec.rows],
            "source_mixed_2x_blocks": mixed_blocks,
            "donor_ground_pixels_replaced": ground_pixels,
            "strict_2x_cadence": cadence_errors(indices) == 0,
            "candidate_vol": str(path.resolve()),
        })

    for start in range(0, len(ROADS), args.page_size):
        names = ROADS[start:start + args.page_size]
        page = panels[start * 3:(start + len(names)) * 3]
        path = out / f"volcanic_roads_review_{names[0]}_{names[-1]}.png"
        shore.write_review_sheet(path, page, columns=3, scale=2)
        print(path.resolve())

    (out / "volcanic_roads_audit.json").write_text(
        json.dumps({
            "method": "one-family-global-palette-lut",
            "ground_integration": "exact RA Temperate clear1 palette-index membership replaced by installed Volcanic clear1 frames",
            "tile_specific_normalization": False,
            "yaml_pixel_recoloring": False,
            "local_shadow_detection": False,
            "calibration": calibration,
            "source_to_target_index": lookup.tolist(),
            "templates": audit,
        }, indent=2) + "\n",
        encoding="utf-8",
    )
    return 0


def build_family_lookup(temperate_palette, volcanic_palette):
    """Map each Temperate index to exactly one Volcanic index.

    Material and shadow decisions depend only on the source palette color and
    family-wide calibration. They never depend on template identity, YAML cell
    labels, local neighborhoods, or per-image percentiles.
    """
    luma_values = []
    warm_values = []
    for name in ROADS:
        spec = shore.read_template_spec(
            ROOT / "mods/cameo/tilesets/ra_temperat.yaml", name + ".tem"
        )
        donor, domain = shore.read_sparse_composite(
            ROOT / "mods/cameo/bits/temp" / spec.image, spec
        )
        rgb = shore.indices_rgb(donor, temperate_palette)
        luma = strategy.luma(rgb)
        source = rgb.astype(np.int16)
        green = (source[:, :, 1] >= source[:, :, 0] + 4) & (
            source[:, :, 1] >= source[:, :, 2] + 2
        )
        luma_values.append(luma[domain])
        warm_values.append(luma[domain & ~green])

    family = np.concatenate(luma_values)
    warm = np.concatenate(warm_values)
    family_low, shadow_threshold, family_high = (
        float(value) for value in np.percentile(family, (3.0, 35.0, 98.0))
    )
    warm_low, warm_high = (
        float(value) for value in np.percentile(warm, (2.0, 98.0))
    )

    allowed = np.asarray(
        list(range(10, 50)) + list(range(192, 221)), dtype=np.uint8
    )
    choices = volcanic_palette[allowed].astype(np.float32)
    lookup = np.zeros(256, dtype=np.uint8)
    for index, color in enumerate(temperate_palette.astype(np.float32)):
        red, green, blue = color
        source_luma = 0.2126 * red + 0.7152 * green + 0.0722 * blue
        is_green = green >= red + 4.0 and green >= blue + 2.0
        if is_green:
            form = np.clip(
                (source_luma - family_low)
                / max(1.0, family_high - family_low),
                0.0,
                1.0,
            )
            target_luma = 32.0 + form * 28.0
            target = np.asarray(
                [target_luma, target_luma * 0.94, target_luma * 0.91],
                dtype=np.float32,
            )
        else:
            form = np.clip(
                (source_luma - warm_low) / max(1.0, warm_high - warm_low),
                0.0,
                1.0,
            )
            target_luma = 34.0 + form * 58.0
            target = np.asarray(
                [target_luma, target_luma * 0.80, target_luma * 0.70],
                dtype=np.float32,
            )

        shadow_weight = np.clip(
            (shadow_threshold - source_luma)
            / max(1.0, shadow_threshold - family_low),
            0.0,
            1.0,
        ) * 0.38
        target = (
            target * (1.0 - shadow_weight)
            + np.asarray([12.0, 8.0, 8.0]) * shadow_weight
        )
        distance = np.sum((choices - target) ** 2, axis=1)
        lookup[index] = allowed[int(np.argmin(distance))]
    lookup[0] = 0
    return lookup, {
        "family_luma_low": family_low,
        "family_shadow_threshold": shadow_threshold,
        "family_luma_high": family_high,
        "warm_luma_low": warm_low,
        "warm_luma_high": warm_high,
        "shadow_strength": 0.38,
    }


def map_at_24(donor, domain, lookup):
    height, width = donor.shape
    blocks = donor.reshape(height // 2, 2, width // 2, 2).transpose(
        0, 2, 1, 3
    ).reshape(height // 2, width // 2, 4)
    mixed = int(np.count_nonzero(np.any(blocks != blocks[:, :, :1], axis=2)))
    source24 = np.zeros(blocks.shape[:2], dtype=np.uint8)
    for row in range(source24.shape[0]):
        for column in range(source24.shape[1]):
            source24[row, column] = np.bincount(
                blocks[row, column], minlength=256
            ).argmax()
    indices = np.repeat(np.repeat(lookup[source24], 2, axis=0), 2, axis=1)
    indices[~domain] = 0
    return indices, mixed, source24


def clear_ground_indices():
    _, _, frames = read_shptd(ROOT / "mods/cameo/bits/temp/clear1.tem")
    return np.unique(np.concatenate([
        np.frombuffer(frame, dtype=np.uint8) for frame in frames
    ]))


def production_ground_frames():
    width, height, frames = read_shptd(
        ROOT / "mods/cameo/bits/volcanic/clear1.vol"
    )
    if (width, height, len(frames)) != (48, 48, 16):
        raise ValueError("production clear1.vol must contain sixteen 48x48 frames")
    return [
        np.frombuffer(frame, dtype=np.uint8).reshape(48, 48)
        for frame in frames
    ]


def integrate_production_ground(
    mapped, source24, domain, spec, donor_ground_indices, ground_frames
):
    result = mapped.copy()
    ground = np.zeros_like(mapped)
    for index in range(spec.columns * spec.rows):
        row, column = divmod(index, spec.columns)
        frame = ground_frames[(index * 7 + spec.columns * 3 + spec.rows) % len(ground_frames)]
        ground[
            row * 48:(row + 1) * 48,
            column * 48:(column + 1) * 48,
        ] = frame

    ground24 = np.isin(source24, donor_ground_indices)
    ground_mask = np.repeat(np.repeat(ground24, 2, axis=0), 2, axis=1) & domain
    result[ground_mask] = ground[ground_mask]
    result[~domain] = 0
    return result, int(ground_mask.sum())


def read_template(path, spec):
    width, height, frames = read_shptd(path)
    if (width, height) != (48, 48):
        raise ValueError(f"unexpected road frame size in {path}")
    result = np.zeros((spec.rows * 48, spec.columns * 48), dtype=np.uint8)
    for index in range(spec.columns * spec.rows):
        if index >= len(frames):
            continue
        row, column = divmod(index, spec.columns)
        result[
            row * 48:(row + 1) * 48,
            column * 48:(column + 1) * 48,
        ] = np.frombuffer(frames[index], dtype=np.uint8).reshape(48, 48)
    return result


def write_template(path, indices, spec):
    frames = []
    for index in range(spec.columns * spec.rows):
        if index not in spec.terrain:
            frames.append(bytes(48 * 48))
            continue
        row, column = divmod(index, spec.columns)
        frames.append(indices[
            row * 48:(row + 1) * 48,
            column * 48:(column + 1) * 48,
        ].tobytes())
    write_shptd(path, 48, 48, frames)


def cadence_errors(indices):
    blocks = indices.reshape(
        indices.shape[0] // 2, 2, indices.shape[1] // 2, 2
    ).transpose(0, 2, 1, 3)
    return int(np.count_nonzero(np.any(
        blocks != blocks[:, :, :1, :1], axis=(2, 3)
    )))


if __name__ == "__main__":
    raise SystemExit(main())
