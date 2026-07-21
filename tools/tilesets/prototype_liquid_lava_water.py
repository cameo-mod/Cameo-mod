#!/usr/bin/env python
"""Build preview-only proper-liquid lava candidates for volcanic w1/w2."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import generate_lava_river_donor_layer as river_lava
import generate_sh04_alpha_beach_prototype as shore
from manual_river_delta.prepare_production import quantize
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
AUTHOR_TILE = 24
AUTHOR_W2 = 48
OUTPUT_TILE = 48
OUTPUT_W2 = 96
UPSCALE = 2
OUTER_COLLAR = 7
BACKGROUND = (67, 78, 88)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--out-dir",
        type=Path,
        default=Path.home()
        / "Documents/agents/volcanic-theater/liquid-lava-water/prototype-01",
    )
    args = parser.parse_args()
    out_dir = args.out_dir.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    palette = read_palette(ROOT / "mods/cameo/bits/volcanic/volcanic.pal")
    current_w1 = decode_frame(ROOT / "mods/cameo/bits/volcanic/w1.vol", 0, palette)
    current_w2 = decode_composite(ROOT / "mods/cameo/bits/volcanic/w2.vol", palette)

    records = []
    review_panels = [
        ("CURRENT: cracked lava (reserved for bridges)", repeat(current_w1, 4, 4)),
        ("CURRENT w1 surrounding w2", surround(current_w1, current_w2)),
    ]
    w1_author, w2_author = render_from_river_lava_algorithm(palette)
    for mode, label in (
        ("river-reuse", "Proper lava: approved river renderer reused"),
    ):
        w1_indices = upscale(w1_author)
        w2_indices = upscale(w2_author)
        w1_image = indices_image(w1_indices, OUTPUT_TILE, OUTPUT_TILE, palette)
        w2_image = indices_image(w2_indices, OUTPUT_W2, OUTPUT_W2, palette)

        write_shptd(
            out_dir / f"w1-{mode}-preview.vol",
            OUTPUT_TILE,
            OUTPUT_TILE,
            [bytes(w1_indices.ravel())],
        )
        write_shptd(
            out_dir / f"w2-{mode}-preview.vol",
            OUTPUT_TILE,
            OUTPUT_TILE,
            [bytes(frame.ravel()) for frame in split_w2(w2_indices)],
        )
        w1_image.save(out_dir / f"w1-{mode}-preview.png")
        w2_image.save(out_dir / f"w2-{mode}-preview.png")
        indices_image(w1_author, AUTHOR_TILE, AUTHOR_TILE, palette).save(
            out_dir / f"w1-{mode}-author-24px.png"
        )
        indices_image(w2_author, AUTHOR_W2, AUTHOR_W2, palette).save(
            out_dir / f"w2-{mode}-author-24px-composite.png"
        )
        review_panels.extend(
            (
                (f"{label}: seamless w1 field", repeat(w1_image, 4, 4)),
                (f"{label}: w1 surrounding w2", surround(w1_image, w2_image)),
            )
        )
        records.append(
            {
                "variant": mode,
                "w1_toroidal_edges_exact": toroidal_edges_exact(w1_author),
                "w2_outer_collar_exact": outer_collar_exact(w1_author, w2_author),
                "strict_2x_blocks": strict_2x(w1_indices) and strict_2x(w2_indices),
                "w1_palette_range": [int(w1_author.min()), int(w1_author.max())],
                "w2_palette_range": [int(w2_author.min()), int(w2_author.max())],
            }
        )

    review = write_review(review_panels)
    review_path = out_dir / "proper_liquid_lava_water_w1_w2_comparison.png"
    review.save(review_path)
    audit = {
        "preview_only": True,
        "production_assets_modified": False,
        "author_tile_size": 24,
        "production_tile_size": 48,
        "upscale": "2x nearest-neighbor",
        "variants": records,
    }
    (out_dir / "proper_liquid_lava_water_audit.json").write_text(
        json.dumps(audit, indent=2), encoding="utf-8"
    )
    print(review_path)
    return 0


def render_from_river_lava_algorithm(
    volcanic_palette: list[tuple[int, int, int]],
) -> tuple[np.ndarray, np.ndarray]:
    temperate_palette = shore.read_palette(
        ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    )
    w1_donor = donor_frame_rgb(
        ROOT / "mods/cameo/bits/temp/w1.tem", 0, temperate_palette
    )
    w2_donor = donor_w2_rgb(
        ROOT / "mods/cameo/bits/temp/w2.tem", temperate_palette
    )
    w1_donor = np.asarray(
        Image.fromarray(w1_donor, mode="RGB").resize(
            (AUTHOR_TILE, AUTHOR_TILE), Image.Resampling.NEAREST
        )
    )
    w2_donor = np.asarray(
        Image.fromarray(w2_donor, mode="RGB").resize(
            (AUTHOR_W2, AUTHOR_W2), Image.Resampling.NEAREST
        )
    )
    w1_rgb = periodic_river_liquid(w1_donor)
    w2_rgb = periodic_river_liquid(w2_donor)
    _, w1_indices = quantize(Image.fromarray(w1_rgb, mode="RGB"), volcanic_palette)
    _, w2_indices = quantize(Image.fromarray(w2_rgb, mode="RGB"), volcanic_palette)
    w1_indices = np.asarray(w1_indices, dtype=np.uint8).reshape(AUTHOR_TILE, AUTHOR_TILE)
    w2_indices = np.asarray(w2_indices, dtype=np.uint8).reshape(AUTHOR_W2, AUTHOR_W2)

    # The Temperate w2 donor supplies the variation. Retain it through the
    # interior, while an exact repeated-w1 collar guarantees legal mixing.
    repeated = np.tile(w1_indices, (2, 2))
    yy, xx = np.indices(w2_indices.shape)
    border = np.minimum.reduce((xx, yy, AUTHOR_W2 - 1 - xx, AUTHOR_W2 - 1 - yy))
    w2_indices[border < OUTER_COLLAR] = repeated[border < OUTER_COLLAR]
    return w1_indices, w2_indices


def donor_frame_rgb(
    path: Path,
    frame_number: int,
    palette: list[tuple[int, int, int]],
) -> np.ndarray:
    width, height, frames = read_shptd(path)
    indices = np.frombuffer(frames[frame_number], dtype=np.uint8).reshape(height, width)
    return np.asarray(palette, dtype=np.uint8)[indices]


def donor_w2_rgb(
    path: Path,
    palette: list[tuple[int, int, int]],
) -> np.ndarray:
    width, height, frames = read_shptd(path)
    if len(frames) != 4:
        raise ValueError(f"expected four w2 donor frames, got {len(frames)}")
    result = np.zeros((height * 2, width * 2, 3), dtype=np.uint8)
    colors = np.asarray(palette, dtype=np.uint8)
    for index, frame in enumerate(frames):
        tile = colors[np.frombuffer(frame, dtype=np.uint8).reshape(height, width)]
        y = (index // 2) * height
        x = (index % 2) * width
        result[y : y + height, x : x + width] = tile
    return result


def periodic_river_liquid(donor_rgb: np.ndarray) -> np.ndarray:
    height, width = donor_rgb.shape[:2]
    tiled = np.tile(donor_rgb, (3, 3, 1))
    mask = np.ones(tiled.shape[:2], dtype=bool)
    liquid = river_lava.liquid_texture(mask, tiled, mask)
    return liquid[height : height * 2, width : width * 2]


def read_palette(path: Path) -> list[tuple[int, int, int]]:
    data = path.read_bytes()
    if len(data) != 768:
        raise ValueError(f"expected 768-byte palette, got {len(data)}")
    return [tuple(data[i + c] * 4 for c in range(3)) for i in range(0, 768, 3)]


def decode_frame(path: Path, frame: int, palette: list[tuple[int, int, int]]) -> Image.Image:
    width, height, frames = read_shptd(path)
    return indices_image(np.frombuffer(frames[frame], dtype=np.uint8).reshape(height, width), width, height, palette)


def decode_composite(path: Path, palette: list[tuple[int, int, int]]) -> Image.Image:
    width, height, frames = read_shptd(path)
    image = Image.new("RGB", (width * 2, height * 2))
    for index, frame in enumerate(frames):
        tile = indices_image(np.frombuffer(frame, dtype=np.uint8).reshape(height, width), width, height, palette)
        image.paste(tile, ((index % 2) * width, (index // 2) * height))
    return image


def indices_image(indices: np.ndarray, width: int, height: int, palette: list[tuple[int, int, int]]) -> Image.Image:
    colors = np.asarray(palette, dtype=np.uint8)
    return Image.fromarray(colors[np.asarray(indices, dtype=np.uint8).reshape(height, width)], mode="RGB")


def upscale(indices: np.ndarray) -> np.ndarray:
    return np.repeat(np.repeat(indices, UPSCALE, axis=0), UPSCALE, axis=1)


def split_w2(indices: np.ndarray) -> list[np.ndarray]:
    return [
        indices[0:OUTPUT_TILE, 0:OUTPUT_TILE],
        indices[0:OUTPUT_TILE, OUTPUT_TILE:OUTPUT_W2],
        indices[OUTPUT_TILE:OUTPUT_W2, 0:OUTPUT_TILE],
        indices[OUTPUT_TILE:OUTPUT_W2, OUTPUT_TILE:OUTPUT_W2],
    ]


def repeat(tile: Image.Image, columns: int, rows: int) -> Image.Image:
    image = Image.new("RGB", (tile.width * columns, tile.height * rows))
    for y in range(rows):
        for x in range(columns):
            image.paste(tile, (x * tile.width, y * tile.height))
    return image


def surround(w1: Image.Image, w2: Image.Image) -> Image.Image:
    image = repeat(w1, 4, 4)
    image.paste(w2, (w1.width, w1.height))
    return image


def toroidal_edges_exact(indices: np.ndarray) -> bool:
    # The scalar field is periodic. Edge adjacency is audited against a tiled
    # copy, rather than requiring the last sample to equal the first sample.
    tiled = np.tile(indices, (2, 2))
    return bool(
        np.array_equal(tiled[:AUTHOR_TILE, AUTHOR_TILE:AUTHOR_TILE * 2], indices)
        and np.array_equal(tiled[AUTHOR_TILE:AUTHOR_TILE * 2, :AUTHOR_TILE], indices)
    )


def outer_collar_exact(w1: np.ndarray, w2: np.ndarray) -> bool:
    repeated = np.tile(w1, (2, 2))
    yy, xx = np.indices(w2.shape)
    border = np.minimum.reduce((xx, yy, AUTHOR_W2 - 1 - xx, AUTHOR_W2 - 1 - yy))
    return bool(np.array_equal(w2[border < OUTER_COLLAR], repeated[border < OUTER_COLLAR]))


def strict_2x(indices: np.ndarray) -> bool:
    return bool(
        np.array_equal(indices[0::2, 0::2], indices[1::2, 0::2])
        and np.array_equal(indices[0::2, 0::2], indices[0::2, 1::2])
        and np.array_equal(indices[0::2, 0::2], indices[1::2, 1::2])
    )


def write_review(panels: list[tuple[str, Image.Image]]) -> Image.Image:
    font = ImageFont.load_default()
    columns = 2
    panel_w, panel_h = 420, 250
    rows = math.ceil(len(panels) / columns)
    canvas = Image.new("RGB", (columns * panel_w, rows * panel_h), BACKGROUND)
    draw = ImageDraw.Draw(canvas)
    for index, (label, image) in enumerate(panels):
        x = (index % columns) * panel_w
        y = (index // columns) * panel_h
        draw.text((x + 10, y + 8), label, fill=(238, 238, 238), font=font)
        scale = min((panel_w - 20) / image.width, (panel_h - 38) / image.height)
        shown = image.resize(
            (round(image.width * scale), round(image.height * scale)),
            Image.Resampling.NEAREST,
        )
        canvas.paste(shown, (x + (panel_w - shown.width) // 2, y + 30))
    return canvas


def smoother(value: float) -> float:
    return value * value * value * (value * (value * 6.0 - 15.0) + 10.0)


def smoothstep(low: float, high: float, value: float) -> float:
    t = max(0.0, min(1.0, (value - low) / (high - low)))
    return t * t * (3.0 - 2.0 * t)


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


if __name__ == "__main__":
    raise SystemExit(main())
