#!/usr/bin/env python
"""Preview shared donor-derived Volcanic ground transforms on bridge families."""

from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

import generate_sh04_alpha_beach_prototype as shore
import generate_volcanic_river_bridges as bridges
from shptd import read_shptd


ROOT = Path(__file__).resolve().parents[2]
OUT = Path.home() / "Documents/agents/volcanic-theater/ground/donor-recolor-bridge-blend-01"
REPRESENTATIVES = ("br1a", "sbridge1", "sbridge5")
WATER = bridges.WATER_INDICES


def main() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    temp_palette = shore.read_palette(ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal")
    volcanic_palette = shore.read_palette(ROOT / "mods/cameo/bits/volcanic/volcanic.pal")
    liquid_indices = shore.unique_frame(
        ROOT / "mods/cameo/bits/volcanic/w1.vol", expected_frames=1
    )
    liquid_rgb = shore.indices_rgb(liquid_indices, volcanic_palette)

    _, _, clear_frames = read_shptd(ROOT / "mods/cameo/bits/temp/clear1.tem")
    clear_donor = np.stack(
        [shore.indices_rgb(np.frombuffer(frame, dtype=np.uint8).reshape(48, 48), temp_palette)
         for frame in clear_frames]
    )
    family_luma = luma(clear_donor)
    low, high = np.percentile(family_luma, (3.0, 98.0))

    variants = (
        ("A subdued donor", tuple(range(11, 22)), 32.0, 60.0, False),
        ("B full basalt", tuple(range(10, 30)), 28.0, 80.0, False),
        ("C two-material hybrid", tuple(range(11, 22)), 32.0, 60.0, True),
    )
    sheets = []
    for label, ground_ramp, dark, light, hybrid in variants:
        recolored_clear = recolor_frames(
            clear_donor, volcanic_palette, ground_ramp, low, high, dark, light
        )
        ground_mosaic = tile_mosaic(recolored_clear, 4, 4)
        panels = [(f"{label}: 4x4 donor-derived clear ground", Image.fromarray(ground_mosaic))]
        for name in REPRESENTATIVES:
            panels.append(
                (
                    f"{label}: {name} over shared ground",
                    render_bridge_on_ground(
                        name,
                        ground_mosaic,
                        temp_palette,
                        volcanic_palette,
                        liquid_rgb,
                        low,
                        high,
                        ground_ramp,
                        dark,
                        light,
                        hybrid,
                    ),
                )
            )
        path = OUT / f"{label[0].lower()}_{label[2:].replace(' ', '_')}.png"
        write_sheet(path, panels)
        sheets.append((label, Image.open(path).convert("RGB")))

    comparison = OUT / "donor_ground_strategy_comparison.png"
    write_stacked(comparison, sheets)
    print(comparison.resolve())
    return 0


def recolor_frames(frames, palette, allowed, low, high, dark, light):
    result = []
    for rgb in frames:
        result.append(recolor_rgb(rgb, palette, allowed, low, high, dark, light))
    return np.stack(result)


def recolor_rgb(rgb, palette, allowed, low, high, dark, light):
    source_luma = luma(rgb)
    form = np.clip((source_luma - low) / max(1.0, high - low), 0.0, 1.0)
    target = dark + form * (light - dark)
    target_rgb = np.stack((target, target * 0.94, target * 0.91), axis=2)
    choices = np.asarray([palette[index] for index in allowed], dtype=np.float32)
    distance = np.sum((target_rgb[:, :, None, :] - choices[None, None, :, :]) ** 2, axis=3)
    picked = np.asarray(allowed, dtype=np.uint8)[np.argmin(distance, axis=2)]
    return shore.indices_rgb(picked, palette)


def render_bridge_on_ground(
    name, ground, temp_palette, volcanic_palette, liquid, low, high,
    ground_ramp, dark, light, hybrid,
):
    spec = bridges.read_bridge_donor_spec(name)
    donor, domain = shore.read_sparse_composite(
        ROOT / "mods/cameo/bits/temp" / spec.image, spec
    )
    donor_rgb = shore.indices_rgb(donor, temp_palette)
    water = bridges.clean_water_regions(domain & np.isin(donor, WATER))
    h, w = domain.shape
    base = np.tile(ground, ((h + ground.shape[0] - 1) // ground.shape[0],
                            (w + ground.shape[1] - 1) // ground.shape[1], 1))[:h, :w].copy()
    bridge_rgb = recolor_rgb(donor_rgb, volcanic_palette, ground_ramp, low, high, dark, light)
    if hybrid:
        structure_ramp = tuple(range(10, 30))
        structure = structure_mask(spec, donor_rgb, domain)
        structure_values = luma(donor_rgb)[structure]
        if structure_values.size:
            structure_low, structure_high = np.percentile(
                structure_values, (2.0, 98.0)
            )
        else:
            structure_low, structure_high = low, high
        broad = recolor_rgb(
            donor_rgb,
            volcanic_palette,
            structure_ramp,
            structure_low,
            structure_high,
            28.0,
            80.0,
        )
        bridge_rgb[structure] = broad[structure]
    base[domain] = bridge_rgb[domain]
    tiled_liquid = np.tile(liquid, (spec.rows, spec.columns, 1))
    base[water] = tiled_liquid[water]
    return Image.fromarray(base, mode="RGB")


def structure_mask(spec, donor_rgb, domain):
    cells = np.zeros_like(domain)
    for index, terrain in spec.terrain.items():
        if terrain != "Bridge":
            continue
        row, column = divmod(index, spec.columns)
        cells[row * 48:(row + 1) * 48, column * 48:(column + 1) * 48] = True
    source_luma = luma(donor_rgb)
    chroma = donor_rgb.max(axis=2).astype(np.int16) - donor_rgb.min(axis=2).astype(np.int16)
    highlights = domain & (source_luma >= 68.0) & (chroma <= 56)
    return domain & (cells | highlights)


def tile_mosaic(frames, columns, rows):
    canvas = np.zeros((rows * 48, columns * 48, 3), dtype=np.uint8)
    for index in range(columns * rows):
        y, x = divmod(index, columns)
        canvas[y * 48:(y + 1) * 48, x * 48:(x + 1) * 48] = frames[index % len(frames)]
    return canvas


def luma(rgb):
    source = rgb.astype(np.float32)
    return source[..., 0] * 0.2126 + source[..., 1] * 0.7152 + source[..., 2] * 0.0722


def write_sheet(path, panels):
    margin, header = 12, 28
    width = max(image.width for _, image in panels)
    height = sum(image.height + header + margin for _, image in panels) + margin
    sheet = Image.new("RGB", (width + margin * 2, height), (72, 84, 96))
    draw = ImageDraw.Draw(sheet)
    y = margin
    for label, image in panels:
        draw.text((margin, y), label, fill=(255, 255, 255))
        y += header
        sheet.paste(image, (margin, y))
        y += image.height + margin
    sheet.save(path)


def write_stacked(path, sheets):
    margin = 16
    width = max(image.width for _, image in sheets)
    height = sum(image.height for _, image in sheets) + margin * (len(sheets) + 1)
    canvas = Image.new("RGB", (width + margin * 2, height), (54, 64, 74))
    y = margin
    for _, image in sheets:
        canvas.paste(image, (margin, y))
        y += image.height + margin
    canvas.save(path)


if __name__ == "__main__":
    raise SystemExit(main())
