#!/usr/bin/env python
"""Generate footprint-fitted molten pools with an outward seepage-color fade."""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

import generate_basalt_pillar_study as pillars
import generate_sh04_alpha_beach_prototype as shore


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUT_DIR = Path.home() / "Documents/agents/volcanic-theater/shorelines/workbench"
WIDTH = 168
HEIGHT = 144
COLUMN_OFFSET_X = 12
BASE_SHIFT_Y = -18
POOL_VARIANTS = (
    ("tight", 2.2, 0.55, 0.5),
    ("balanced", 7.2, 1.15, 1.8),
    ("wide", 9.4, 1.45, 2.4),
)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    parser.add_argument("--shore-preview", type=Path)
    parser.add_argument("--shore-name", default="sh04")
    args = parser.parse_args()
    out_dir = args.out_dir.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    base_footprint, fitted_footprint, smooth_distance = pool_footprint()
    columns = pillars.build_column_forest(0xBA5A17)
    clean_columns, _, _ = pillars.render_forest(
        columns,
        lava_contact=True,
        include_shadow=True,
    )
    pool_panels: list[tuple[str, Image.Image]] = []
    guide_panels: list[tuple[str, Image.Image]] = []
    column_panels: list[tuple[str, Image.Image]] = []
    column_composites: dict[str, Image.Image] = {}
    pool_layers: dict[str, Image.Image] = {}
    for name, outer_depth, smooth_sigma, edge_warp in POOL_VARIANTS:
        pool, heat_distance = pool_geometry(
            fitted_footprint,
            smooth_distance,
            outer_depth=outer_depth,
            smooth_sigma=smooth_sigma,
            edge_warp=edge_warp,
        )
        pool_rgb = seepage_pool_texture(pool, heat_distance, outer_depth)
        alpha = shore.feather_alpha(pool, 0.65)
        layer_rgba = np.dstack((pool_rgb, alpha)).astype(np.uint8)
        layer = Image.fromarray(layer_rgba, mode="RGBA")
        pool_layers[name] = layer
        guide = base_guide_image(pool_rgb, pool, base_footprint)

        layer.save(out_dir / f"molten_pool_footprint_{name}_basalt_columns.png")
        pool_with_columns = Image.new(
            "RGBA",
            (WIDTH, HEIGHT - BASE_SHIFT_Y),
            (0, 0, 0, 0),
        )
        pool_with_columns.alpha_composite(layer, dest=(0, -BASE_SHIFT_Y))
        pool_with_columns.alpha_composite(
            clean_columns,
            dest=(COLUMN_OFFSET_X, 0),
        )
        pool_with_columns.save(
            out_dir / f"molten_pool_with_column_{name}_basalt_columns.png"
        )
        column_composites[name] = pool_with_columns
        pool_panels.append((f"{name.title()}: pool only", shore.checker_composite(layer_rgba)))
        guide_panels.append((f"{name.title()}: true base overlay", guide))
        column_panels.append(
            (
                f"{name.title()}: pool with column",
                shore.checker_composite(np.asarray(pool_with_columns, dtype=np.uint8)),
            )
        )

    shore.write_review_sheet(
        out_dir / "molten_pool_footprint_comparison_basalt_columns.png",
        pool_panels + guide_panels,
        columns=3,
        scale=2,
    )
    shore.write_review_sheet(
        out_dir / "molten_pool_with_column_comparison_basalt_columns.png",
        column_panels,
        columns=3,
        scale=3,
    )
    if args.shore_preview is not None:
        write_shore_placement_review(
            args.shore_preview.resolve(),
            args.shore_name,
            column_composites["tight"],
            pool_layers["tight"],
            clean_columns,
            out_dir,
        )

    print((out_dir / "molten_pool_footprint_comparison_basalt_columns.png").resolve())
    print((out_dir / "molten_pool_with_column_comparison_basalt_columns.png").resolve())
    return 0


def write_shore_placement_review(
    shore_path: Path,
    shore_name: str,
    pool_with_columns: Image.Image,
    pool_layer: Image.Image,
    clean_columns: Image.Image,
    out_dir: Path,
) -> None:
    background = Image.open(shore_path).convert("RGBA")
    panels: list[tuple[str, Image.Image]] = []
    for percent in (70, 80, 90):
        scale = percent / 100.0
        size = (
            max(1, round(pool_with_columns.width * scale)),
            max(1, round(pool_with_columns.height * scale)),
        )
        decoration = pool_with_columns.resize(size, Image.Resampling.LANCZOS)
        placed = background.copy()
        x = (placed.width - decoration.width) // 2
        y = placed.height - decoration.height - 2
        placed.alpha_composite(decoration, dest=(x, y))
        placed_rgb = placed.convert("RGB")
        placed_rgb.save(
            out_dir / f"basalt_column_pool_{percent}pct_{shore_name}.png"
        )
        panels.append((f"{percent}% scale on {shore_name}", placed_rgb))

    shore.write_review_sheet(
        out_dir / f"basalt_column_pool_scale_comparison_{shore_name}.png",
        panels,
        columns=3,
        scale=3,
    )
    write_shore_merge_review(
        background,
        shore_name,
        pool_layer,
        clean_columns,
        out_dir,
    )


def write_shore_merge_review(
    background: Image.Image,
    shore_name: str,
    pool_layer: Image.Image,
    clean_columns: Image.Image,
    out_dir: Path,
) -> None:
    pool_tile, columns_tile = fitted_decoration_layers(
        background.size,
        pool_layer,
        clean_columns,
        scale=0.8,
    )
    current = background.copy()
    current.alpha_composite(pool_tile)
    current.alpha_composite(columns_tile)
    feeder = merged_shore_image(
        background,
        pool_tile,
        columns_tile,
        unified_field=False,
    )
    unified = merged_shore_image(
        background,
        pool_tile,
        columns_tile,
        unified_field=True,
    )

    outputs = (
        ("current", current.convert("RGB")),
        ("feeder_cracks", feeder),
        ("unified_molten_field", unified),
    )
    for name, image in outputs:
        image.save(out_dir / f"basalt_pool_{name}_{shore_name}.png")
    shore.write_review_sheet(
        out_dir / f"basalt_pool_merge_comparison_{shore_name}.png",
        [
            ("Current pool edge", outputs[0][1]),
            ("Narrow feeder cracks", outputs[1][1]),
            ("Unified pool and crack field", outputs[2][1]),
        ],
        columns=3,
        scale=3,
    )


def fitted_decoration_layers(
    tile_size: tuple[int, int],
    pool_layer: Image.Image,
    clean_columns: Image.Image,
    *,
    scale: float,
) -> tuple[Image.Image, Image.Image]:
    assembly_size = (WIDTH, HEIGHT - BASE_SHIFT_Y)
    pool_canvas = Image.new("RGBA", assembly_size, (0, 0, 0, 0))
    pool_canvas.alpha_composite(pool_layer, dest=(0, -BASE_SHIFT_Y))
    columns_canvas = Image.new("RGBA", assembly_size, (0, 0, 0, 0))
    columns_canvas.alpha_composite(clean_columns, dest=(COLUMN_OFFSET_X, 0))

    scaled_size = (
        max(1, round(assembly_size[0] * scale)),
        max(1, round(assembly_size[1] * scale)),
    )
    pool_scaled = pool_canvas.resize(scaled_size, Image.Resampling.LANCZOS)
    columns_scaled = columns_canvas.resize(scaled_size, Image.Resampling.LANCZOS)
    position = (
        (tile_size[0] - scaled_size[0]) // 2,
        tile_size[1] - scaled_size[1] - 2,
    )
    pool_tile = Image.new("RGBA", tile_size, (0, 0, 0, 0))
    columns_tile = Image.new("RGBA", tile_size, (0, 0, 0, 0))
    pool_tile.alpha_composite(pool_scaled, dest=position)
    columns_tile.alpha_composite(columns_scaled, dest=position)
    return pool_tile, columns_tile


def merged_shore_image(
    background: Image.Image,
    pool_tile: Image.Image,
    columns_tile: Image.Image,
    *,
    unified_field: bool,
) -> Image.Image:
    bg_rgb = np.asarray(background.convert("RGB"), dtype=np.uint8)
    pool_rgba = np.asarray(pool_tile, dtype=np.uint8).copy()
    red = bg_rgb[:, :, 0].astype(np.float32)
    green = bg_rgb[:, :, 1].astype(np.float32)
    blue = bg_rgb[:, :, 2].astype(np.float32)
    crack_heat = (
        (red > 100.0)
        & (red > green * 1.25)
        & (green > 12.0)
    )
    pool_mask = pool_rgba[:, :, 3] > 48
    distance_from_pool = ndimage.distance_transform_edt(~pool_mask)
    distance_from_crack = ndimage.distance_transform_edt(~crack_heat)

    bridge = (
        (distance_from_pool + distance_from_crack <= 3.25)
        & (distance_from_pool <= 2.5)
    )
    feeder_mask = (
        (crack_heat | bridge)
        & (distance_from_pool <= 14.0)
        & ~pool_mask
    )
    feeder_alpha = shore.feather_alpha(feeder_mask, 0.35).astype(np.float32) / 255.0
    stops = np.asarray((0.0, 2.5, 5.5, 9.5, 14.0), dtype=np.float32)
    colors = np.asarray(
        (
            (238.0, 150.0, 33.0),
            (238.0, 138.0, 31.0),
            (235.0, 125.0, 29.0),
            (230.0, 107.0, 26.0),
            (224.0, 91.0, 24.0),
        ),
        dtype=np.float32,
    )
    feeder_rgb = np.empty_like(bg_rgb, dtype=np.float32)
    for channel in range(3):
        feeder_rgb[:, :, channel] = np.interp(
            distance_from_pool,
            stops,
            colors[:, channel],
        )
    merged_rgb = (
        bg_rgb.astype(np.float32) * (1.0 - feeder_alpha[:, :, None])
        + feeder_rgb * feeder_alpha[:, :, None]
    )
    merged = Image.fromarray(
        np.clip(np.rint(merged_rgb), 0, 255).astype(np.uint8),
        mode="RGB",
    ).convert("RGBA")

    if unified_field:
        molten_mask = pool_mask | feeder_mask
        molten_alpha = (
            shore.feather_alpha(molten_mask, 0.35).astype(np.float32) / 255.0
        )
        molten_rgb = feeder_rgb.copy()
        molten_rgb[pool_mask] = pool_rgba[pool_mask, :3]
        unified_rgb = (
            bg_rgb.astype(np.float32) * (1.0 - molten_alpha[:, :, None])
            + molten_rgb * molten_alpha[:, :, None]
        )
        merged = Image.fromarray(
            np.clip(np.rint(unified_rgb), 0, 255).astype(np.uint8),
            mode="RGB",
        ).convert("RGBA")
        merged.alpha_composite(columns_tile)
        return merged.convert("RGB")

    merged.alpha_composite(Image.fromarray(pool_rgba, mode="RGBA"))
    merged.alpha_composite(columns_tile)
    return merged.convert("RGB")


def pool_footprint() -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    columns = pillars.build_column_forest(0xBA5A17)
    base_footprint = column_base_mask(
        columns,
        shift_x=COLUMN_OFFSET_X,
        shift_y=BASE_SHIFT_Y,
    )

    # Join the tiny gaps between neighboring bottom hexagons, but retain the
    # actual stepped outline of the column cluster instead of replacing it with
    # a bounding ellipse.
    fitted_footprint = ndimage.binary_closing(
        base_footprint,
        structure=disk_structure(1),
    )
    fitted_footprint = ndimage.binary_fill_holes(fitted_footprint)
    signed_distance = (
        ndimage.distance_transform_edt(~fitted_footprint)
        - ndimage.distance_transform_edt(fitted_footprint)
    ).astype(np.float32)
    return base_footprint, fitted_footprint, signed_distance


def pool_geometry(
    fitted_footprint: np.ndarray,
    signed_distance: np.ndarray,
    *,
    outer_depth: float,
    smooth_sigma: float,
    edge_warp: float,
) -> tuple[np.ndarray, np.ndarray]:
    smooth_distance = ndimage.gaussian_filter(
        signed_distance,
        sigma=smooth_sigma,
        mode="nearest",
    )

    rng = np.random.default_rng(0x5EE9A6E)
    noise = rng.random((HEIGHT, WIDTH), dtype=np.float32)
    noise = ndimage.gaussian_filter(noise, sigma=4.2, mode="reflect")
    noise = normalize(noise)
    warped_distance = smooth_distance + (noise - 0.5) * edge_warp
    pool = (warped_distance <= outer_depth) | fitted_footprint
    pool[:3, :] = False
    pool[-3:, :] = False
    pool[:, :3] = False
    pool[:, -3:] = False
    heat_distance = np.maximum(0.0, warped_distance)
    return pool, heat_distance


def column_base_mask(
    columns: list[pillars.Column],
    shift_x: int,
    shift_y: int,
) -> np.ndarray:
    image = Image.new("L", (WIDTH, HEIGHT), 0)
    draw = ImageDraw.Draw(image)
    for column in columns:
        points = [
            (x + shift_x, y + shift_y)
            for x, y in pillars.hex_points(
                column.x,
                column.base_y,
                column.radius,
                column.seed,
            )
        ]
        draw.polygon(points, fill=255)
    return np.asarray(image, dtype=np.uint8) > 0


def seepage_pool_texture(
    pool: np.ndarray,
    warped_distance: np.ndarray,
    outer_depth: float,
) -> np.ndarray:
    stops = np.asarray(
        (0.0, 0.14, 0.32, 0.55, 0.78, 1.0),
        dtype=np.float32,
    ) * outer_depth
    colors = np.asarray(
        (
            (255.0, 218.0, 80.0),
            (252.0, 206.0, 65.0),
            (250.0, 193.0, 52.0),
            (246.0, 178.0, 43.0),
            (242.0, 164.0, 37.0),
            (238.0, 150.0, 33.0),
        ),
        dtype=np.float32,
    )
    rgb = np.empty((HEIGHT, WIDTH, 3), dtype=np.float32)
    for channel in range(3):
        rgb[:, :, channel] = np.interp(
            warped_distance,
            stops,
            colors[:, channel],
        )

    rng = np.random.default_rng(0xC0102)
    variation = rng.random((HEIGHT, WIDTH), dtype=np.float32)
    variation = ndimage.gaussian_filter(variation, sigma=2.1, mode="reflect")
    variation = normalize(variation) - 0.5
    rgb += variation[:, :, None] * np.asarray((12.0, 7.0, 3.0), dtype=np.float32)
    rgb[~pool] = 0.0
    return np.clip(np.rint(rgb), 0, 255).astype(np.uint8)


def disk_structure(radius: int) -> np.ndarray:
    yy, xx = np.ogrid[-radius : radius + 1, -radius : radius + 1]
    return xx * xx + yy * yy <= radius * radius


def base_guide_image(
    pool_rgb: np.ndarray,
    pool: np.ndarray,
    base_footprint: np.ndarray,
) -> Image.Image:
    result = pool_rgb.copy()
    result[~pool] = np.asarray((20, 20, 20), dtype=np.uint8)
    result[base_footprint] = np.asarray((24, 25, 27), dtype=np.uint8)
    base_edge = base_footprint & ~ndimage.binary_erosion(
        base_footprint,
        structure=np.ones((3, 3), dtype=bool),
    )
    result[base_edge] = np.asarray((108, 114, 118), dtype=np.uint8)
    return Image.fromarray(result, mode="RGB")


def volcanic_background(filename: str, expected_frames: int) -> Image.Image:
    palette = shore.read_palette(ROOT / "mods/cameo/bits/volcanic/volcanic.pal")
    frame = shore.unique_frame(
        ROOT / "mods/cameo/bits/volcanic" / filename,
        expected_frames=expected_frames,
    )
    tiled = np.tile(frame, (3, 3))
    return Image.fromarray(shore.indices_rgb(tiled, palette), mode="RGB")


def composite_layer(background: Image.Image, layer: Image.Image) -> Image.Image:
    result = background.convert("RGBA")
    result.alpha_composite(layer)
    return result.convert("RGB")


def normalize(values: np.ndarray) -> np.ndarray:
    low = float(values.min())
    high = float(values.max())
    return (values - low) / max(1e-6, high - low)


if __name__ == "__main__":
    raise SystemExit(main())
