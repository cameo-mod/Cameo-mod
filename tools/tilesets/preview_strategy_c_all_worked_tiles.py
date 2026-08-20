#!/usr/bin/env python
"""Build preview-only Strategy C reviews for all worked Volcanic families."""

from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

import generate_inland_lava_rivers as rivers
import generate_sh04_alpha_beach_prototype as shore
import generate_volcanic_river_bridges as bridges
import prototype_donor_recolored_bridge_ground as strategy
from shptd import read_shptd


ROOT = Path(__file__).resolve().parents[2]
OUT = Path.home() / "Documents/agents/volcanic-theater/ground/strategy-c-full-family-review-01"
BITS = ROOT / "mods/cameo/bits/volcanic"
VOLCANIC_YAML = ROOT / "mods/cameo/tilesets/volcanic.yaml"
TEMP_YAML = ROOT / "mods/cameo/tilesets/ra_temperat.yaml"
TEMP_BITS = ROOT / "mods/cameo/bits/temp"
TILE = 48

CLIFFS = tuple(f"s{i:02d}" for i in range(1, 39))
SHORES = tuple(f"sh{i:02d}" for i in range(1, 55))
RIVERS = tuple(f"rv{i:02d}" for i in range(1, 16))
FORDS = ("f01", "f02", "f03", "f04", "f05", "f06", "ford1", "ford2", "fjord1", "fjord2")
BRIDGES = tuple(name for name in bridges.BRIDGES if not name.startswith("fjord"))


def main() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    palette = shore.read_palette(BITS / "volcanic.pal")
    temp_palette = shore.read_palette(ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal")
    clear_donor, low, high, clear_variants = build_clear_family(temp_palette, palette)
    canonical_ground = clear_variants[0]

    reviews: dict[str, list[str]] = {}
    reviews["ground"] = write_ground_review(clear_donor, clear_variants, palette)
    reviews["cliffs"] = build_pages(
        "cliffs", CLIFFS,
        lambda name: cliff_pair(name, canonical_ground, low, high, palette, temp_palette),
        page_size=8,
    )
    reviews["shorelines"] = build_pages(
        "shorelines", SHORES,
        lambda name: shore_pair(name, canonical_ground, low, high, palette, temp_palette),
        page_size=6,
    )
    reviews["rivers"] = build_pages(
        "rivers", RIVERS,
        lambda name: river_pair(name, canonical_ground, low, high, palette, temp_palette),
        page_size=5,
    )
    reviews["fords_crossings"] = build_pages(
        "fords_crossings", FORDS,
        lambda name: river_pair(name, canonical_ground, low, high, palette, temp_palette),
        page_size=5,
    )
    reviews["bridges"] = build_pages(
        "human_bridges", BRIDGES,
        lambda name: bridge_pair(name, clear_variants, low, high, palette, temp_palette),
        page_size=6,
    )
    reviews["liquid_references"] = write_liquid_review(palette)

    manifest = {
        "preview_only": True,
        "strategy": "C two-material hybrid",
        "ground": "RA Temperate donor texture mapped to subdued Volcanic ramp 11-21",
        "structures": "preserved donor geometry with independent normalization and ramp 10-29",
        "liquid": "unchanged production w1/w2",
        "counts": {
            "clear_variants": 16,
            "cliffs": len(CLIFFS),
            "shorelines": len(SHORES),
            "rivers": len(RIVERS),
            "fords_crossings": len(FORDS),
            "human_bridges": len(BRIDGES),
        },
        "reviews": reviews,
    }
    path = OUT / "strategy_c_full_family_manifest.json"
    path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(path.resolve())
    return 0


def build_clear_family(temp_palette, volcanic_palette):
    _, _, frames = read_shptd(TEMP_BITS / "clear1.tem")
    donor = np.stack([
        shore.indices_rgb(np.frombuffer(frame, dtype=np.uint8).reshape(TILE, TILE), temp_palette)
        for frame in frames
    ])
    values = strategy.luma(donor)
    low, high = np.percentile(values, (3.0, 98.0))
    variants = strategy.recolor_frames(
        donor, volcanic_palette, tuple(range(11, 22)), low, high, 32.0, 60.0
    )
    return donor, float(low), float(high), variants


def write_ground_review(donor, candidate, palette):
    _, _, current_frames = read_shptd(BITS / "clear1.vol")
    current = np.stack([
        shore.indices_rgb(np.frombuffer(frame, dtype=np.uint8).reshape(TILE, TILE), palette)
        for frame in current_frames
    ])
    panels = []
    for start in range(0, 16, 8):
        names = []
        page = []
        for i in range(start, start + 8):
            names.append(f"v{i:02d}")
            page.append((f"clear1 v{i:02d}: Temperate donor", Image.fromarray(donor[i])))
            page.append((f"clear1 v{i:02d}: current", Image.fromarray(current[i])))
            page.append((f"clear1 v{i:02d}: Strategy C", Image.fromarray(candidate[i])))
        path = OUT / f"ground_clear1_{names[0]}_{names[-1]}.png"
        write_page(path, page, columns=3, scale=2)
        panels.append(str(path.resolve()))
    return panels


def current_composite(name, palette):
    spec = shore.read_template_spec(VOLCANIC_YAML, f"{name}.vol")
    try:
        indices, domain = shore.read_sparse_composite(BITS / spec.image, spec)
    except ValueError:
        width, height, frames = read_shptd(BITS / spec.image)
        if (width, height) != (TILE, TILE) or len(frames) != len(spec.terrain):
            raise
        indices = np.zeros((spec.rows * TILE, spec.columns * TILE), dtype=np.uint8)
        domain = np.zeros_like(indices, dtype=bool)
        for frame, index in zip(frames, sorted(spec.terrain)):
            row, column = divmod(index, spec.columns)
            ys = slice(row * TILE, (row + 1) * TILE)
            xs = slice(column * TILE, (column + 1) * TILE)
            indices[ys, xs] = np.frombuffer(frame, dtype=np.uint8).reshape(TILE, TILE)
            domain[ys, xs] = True
    rgb = shore.indices_rgb(indices, palette)
    rgb[~domain] = shore.BACKGROUND
    return spec, indices, domain, rgb


def donor_composite(name, temp_palette):
    spec = shore.read_template_spec(TEMP_YAML, f"{name}.tem")
    indices, domain = shore.read_sparse_composite(TEMP_BITS / spec.image, spec)
    rgb = shore.indices_rgb(indices, temp_palette)
    return spec, indices, domain, rgb


def recolored_donor(rgb, low, high, palette):
    return strategy.recolor_rgb(
        rgb, palette, tuple(range(11, 22)), low, high, 32.0, 60.0
    )


def cliff_pair(name, canonical, low, high, palette, temp_palette):
    _, _, domain, current = current_composite(name, palette)
    donor, donor_domain, _ = rivers.read_donor(name)
    snow, snow_domain, _ = rivers.read_donor(name, theater="snow")
    donor_rgb = shore.indices_rgb(donor, temp_palette)
    snow_palette = shore.read_palette(ROOT / "mods/cameo/bits/rasnow/ra_snow.pal")
    snow_rgb = shore.indices_rgb(snow, snow_palette)
    donor_rgba = np.dstack((donor_rgb, np.where(donor_domain, 255, 0).astype(np.uint8)))
    snow_rgba = np.dstack((snow_rgb, np.where(snow_domain, 255, 0).astype(np.uint8)))
    rock = np.asarray(rivers.classify_river_cliff(donor_rgba, snow_rgba, donor_domain)) >= 128
    candidate = current.copy()
    ground = recolored_donor(donor_rgb, low, high, palette)
    replace = domain & ~rock
    candidate[replace] = ground[replace]
    return Image.fromarray(current), Image.fromarray(candidate)


def shore_pair(name, canonical, low, high, palette, temp_palette):
    _, current_indices, domain, current = current_composite(name, palette)
    _, donor_indices, donor_domain, donor_rgb = donor_composite(name, temp_palette)
    ground_source = shore.source_indices(TEMP_BITS / "clear1.tem")
    raw_ground = donor_domain & np.isin(donor_indices, list(ground_source))
    hot = (current[:, :, 0] > 95) & (current[:, :, 0] > current[:, :, 1] + 24)
    current_ground = np.isin(current_indices, list(range(10, 30)))
    replace = domain & raw_ground & current_ground & ~hot
    candidate = current.copy()
    ground = recolored_donor(donor_rgb, low, high, palette)
    candidate[replace] = ground[replace]
    return Image.fromarray(current), Image.fromarray(candidate)


def river_pair(name, canonical, low, high, palette, temp_palette):
    _, _, domain, current = current_composite(name, palette)
    donor, donor_domain, _ = rivers.read_donor(name)
    donor_rgb = shore.indices_rgb(donor, temp_palette)
    try:
        snow, snow_domain, _ = rivers.read_donor(name, theater="snow")
        snow_palette = shore.read_palette(ROOT / "mods/cameo/bits/rasnow/ra_snow.pal")
        snow_rgb = shore.indices_rgb(snow, snow_palette)
        donor_rgba = np.dstack((donor_rgb, np.where(donor_domain, 255, 0).astype(np.uint8)))
        snow_rgba = np.dstack((snow_rgb, np.where(snow_domain, 255, 0).astype(np.uint8)))
        rock = np.asarray(rivers.classify_river_cliff(donor_rgba, snow_rgba, donor_domain)) >= 128
    except (FileNotFoundError, ValueError):
        rock = np.zeros_like(domain)
    hot = (current[:, :, 0] > 95) & (current[:, :, 0] > current[:, :, 1] + 24)
    replace = domain & ~rock & ~hot
    candidate = current.copy()
    ground = recolored_donor(donor_rgb, low, high, palette)
    candidate[replace] = ground[replace]
    return Image.fromarray(current), Image.fromarray(candidate)


def bridge_pair(name, clear_variants, low, high, palette, temp_palette):
    try:
        _, _, _, current = current_composite(name, palette)
    except FileNotFoundError:
        spec = bridges.read_bridge_donor_spec(name)
        donor, domain = shore.read_sparse_composite(TEMP_BITS / spec.image, spec)
        current = shore.indices_rgb(donor, temp_palette)
        current[~domain] = shore.BACKGROUND
    ground = strategy.tile_mosaic(clear_variants, 4, 4)
    liquid_indices = shore.unique_frame(BITS / "w1.vol", expected_frames=1)
    liquid = shore.indices_rgb(liquid_indices, palette)
    candidate = strategy.render_bridge_on_ground(
        name, ground, temp_palette, palette, liquid, low, high,
        tuple(range(11, 22)), 32.0, 60.0, True,
    )
    return Image.fromarray(current), candidate


def write_liquid_review(palette):
    panels = []
    for name, expected in (("w1", 1), ("w2", 4)):
        width, height, frames = read_shptd(BITS / f"{name}.vol")
        for i, frame in enumerate(frames[:expected]):
            indices = np.frombuffer(frame, dtype=np.uint8).reshape(height, width)
            image = Image.fromarray(shore.indices_rgb(indices, palette))
            panels.append((f"{name} frame {i}: unchanged liquid reference", image))
    path = OUT / "liquid_w1_w2_unchanged_references.png"
    write_page(path, panels, columns=2, scale=4)
    return [str(path.resolve())]


def build_pages(category, names, pair_fn, page_size):
    paths = []
    for start in range(0, len(names), page_size):
        chunk = names[start:start + page_size]
        panels = []
        for name in chunk:
            before, after = pair_fn(name)
            panels.append((f"{name}: current production", before))
            panels.append((f"{name}: Strategy C", after))
        path = OUT / f"{category}_{chunk[0]}_{chunk[-1]}.png"
        write_page(path, panels, columns=2, scale=2)
        paths.append(str(path.resolve()))
        print(path.resolve())
    return paths


def write_page(path, panels, columns, scale):
    margin, header = 12, 24
    cell_w = max(image.width for _, image in panels) * scale
    cell_h = max(image.height for _, image in panels) * scale
    rows = (len(panels) + columns - 1) // columns
    sheet = Image.new(
        "RGB",
        (margin + columns * (cell_w + margin), margin + rows * (cell_h + header + margin)),
        (72, 84, 96),
    )
    draw = ImageDraw.Draw(sheet)
    for i, (label, image) in enumerate(panels):
        row, column = divmod(i, columns)
        x = margin + column * (cell_w + margin)
        y = margin + row * (cell_h + header + margin)
        draw.text((x, y), label, fill=(255, 255, 255))
        resized = image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)
        sheet.paste(resized, (x, y + header))
    sheet.save(path)


if __name__ == "__main__":
    raise SystemExit(main())
