#!/usr/bin/env python
"""Generate transparent liquid-lava layers from exact Temperate shore water."""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

import generate_sh04_alpha_beach_prototype as shore


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUT_DIR = (
    Path.home() / "Documents/agents/volcanic-theater/shorelines/workbench"
)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--template", default="sh18")
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    args = parser.parse_args()
    out_dir = args.out_dir.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    spec = shore.read_template_spec(
        ROOT / "mods/cameo/tilesets/ra_temperat.yaml",
        f"{args.template}.tem",
    )
    donor, domain = shore.read_sparse_composite(
        ROOT / "mods/cameo/bits/temp" / spec.image,
        spec,
    )
    palette = shore.read_palette(
        ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    )
    donor_rgb = shore.indices_rgb(donor, palette)
    donor_rgb[~domain] = shore.BACKGROUND

    water_indices = shore.source_indices(
        ROOT / "mods/cameo/bits/temp/w1.tem"
    ) | shore.source_indices(ROOT / "mods/cameo/bits/temp/w2.tem")
    raw_water = domain & np.isin(donor, list(water_indices))
    water = clean_water_mask(raw_water, domain)
    liquid_rgb = liquid_texture(water, donor_rgb, raw_water)
    alpha = shore.feather_alpha(water, 0.8)
    alpha[~water] = 0
    liquid_rgba = np.zeros((*water.shape, 4), dtype=np.uint8)
    liquid_rgba[:, :, :3] = liquid_rgb
    liquid_rgba[:, :, 3] = alpha

    layer = Image.fromarray(liquid_rgba, mode="RGBA")
    donor_composite = Image.fromarray(donor_rgb, mode="RGB").convert("RGBA")
    donor_composite.alpha_composite(layer)
    donor_composite = donor_composite.convert("RGB")

    layer_path = out_dir / f"temperate_donor_lava_only_transparent_{args.template}.png"
    layer_x4_path = out_dir / f"temperate_donor_lava_only_transparent_4x_{args.template}.png"
    donor_path = out_dir / f"temperate_donor_water_replaced_with_lava_{args.template}.png"
    layer.save(layer_path)
    layer.resize(
        (layer.width * 4, layer.height * 4),
        Image.Resampling.NEAREST,
    ).save(layer_x4_path)
    donor_composite.save(donor_path)
    print(layer_path.resolve())
    print(layer_x4_path.resolve())
    print(donor_path.resolve())
    return 0


def clean_water_mask(raw_mask: np.ndarray, domain: np.ndarray) -> np.ndarray:
    result = ndimage.binary_closing(raw_mask, structure=shore.disk(1)) & domain
    holes = domain & ~result
    labels, count = ndimage.label(holes)
    sizes = np.bincount(labels.ravel())
    fill = np.zeros(count + 1, dtype=bool)
    fill[1:] = sizes[1:] <= 24
    result |= fill[labels]
    result[0, :] |= raw_mask[0, :]
    result[-1, :] |= raw_mask[-1, :]
    result[:, 0] |= raw_mask[:, 0]
    result[:, -1] |= raw_mask[:, -1]
    return result


def liquid_texture(
    mask: np.ndarray,
    donor_rgb: np.ndarray,
    source_mask: np.ndarray,
) -> np.ndarray:
    donor = donor_rgb.astype(np.float32)
    _, nearest = ndimage.distance_transform_edt(~source_mask, return_indices=True)
    filled = mask & ~source_mask
    donor[filled] = donor[nearest[0][filled], nearest[1][filled]]
    luma = (
        donor[:, :, 0] * 0.2126
        + donor[:, :, 1] * 0.7152
        + donor[:, :, 2] * 0.0722
    )
    low = ndimage.gaussian_filter(luma, sigma=2.2)
    woven = np.clip(0.5 + (luma - low) / 34.0, 0.0, 1.0)
    low_q, high_q = np.percentile(luma[mask], (5.0, 96.0))
    normalized = np.clip((luma - low_q) / max(1.0, high_q - low_q), 0.0, 1.0)
    heat = np.clip(0.18 + normalized * 0.53 + woven * 0.29, 0.0, 1.0)

    cool = np.asarray((213.0, 49.0, 5.0), dtype=np.float32)
    middle = np.asarray((255.0, 124.0, 10.0), dtype=np.float32)
    hot = np.asarray((255.0, 220.0, 78.0), dtype=np.float32)
    first = np.clip(heat / 0.58, 0.0, 1.0)[:, :, None]
    second = np.clip((heat - 0.58) / 0.42, 0.0, 1.0)[:, :, None]
    result = cool * (1.0 - first) + middle * first
    result = result * (1.0 - second) + hot * second

    spot_threshold = float(np.quantile(normalized[mask], 0.09))
    spots = mask & (normalized <= spot_threshold) & (
        luma <= ndimage.minimum_filter(luma, size=3)
    )
    spots = ndimage.binary_dilation(spots, structure=shore.disk(1)) & mask
    spot_weight = ndimage.gaussian_filter(spots.astype(np.float32), sigma=0.65)
    spot_weight = np.clip(spot_weight * 1.15, 0.0, 0.72)[:, :, None]
    orange = np.asarray((225.0, 59.0, 5.0), dtype=np.float32)
    result = result * (1.0 - spot_weight) + orange * spot_weight

    distance = ndimage.distance_transform_edt(mask)
    bank_weight = np.clip((3.0 - distance) / 3.0, 0.0, 1.0)[:, :, None]
    bank = np.asarray((196.0, 45.0, 5.0), dtype=np.float32)
    result = result * (1.0 - 0.72 * bank_weight) + bank * (0.72 * bank_weight)
    result[~mask] = 0.0
    return np.clip(np.rint(result), 0, 255).astype(np.uint8)


if __name__ == "__main__":
    raise SystemExit(main())
