#!/usr/bin/env python
"""Fresh medium and large ground-basalt pillar study (preview only)."""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

import generate_basalt_column_families as family
import generate_basalt_pillar_study as pillars
import generate_sh04_alpha_beach_prototype as shore


DEFAULT_OUT_DIR = Path.home() / "Documents/agents/volcanic-theater/shorelines/workbench"
SEED = 0x4D45444C
SPECS = {
    "small_1x1": {
        "tiles": (1, 1), "center_y": 91.0, "rocks": 5,
        "x_scale": 0.42, "base_y_scale": 0.35, "height_scale": 0.55,
        "base_center_y": 100.0, "source_radius_x": 31.0, "source_radius_y": 17.0,
    },
    "medium_2x1": {
        "tiles": (2, 1), "center_y": 91.0, "rocks": 8,
        "x_scale": 0.70, "base_y_scale": 0.30, "height_scale": 0.65,
        "base_center_y": 99.0, "fit_to_box": True,
    },
    "large_2x2": {
        "tiles": (2, 2), "center_y": 83.0, "rocks": 12,
        "x_scale": 0.72, "base_y_scale": 0.58, "height_scale": 1.18,
        "base_center_y": 105.0, "fit_to_box": True,
    },
}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    args = parser.parse_args()
    out_dir = args.out_dir.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    panels: list[tuple[str, Image.Image]] = []
    for ordinal, (name, spec) in enumerate(SPECS.items()):
        tiles_w, tiles_h = spec["tiles"]
        target = target_bounds(
            tiles_w * shore.TILE,
            tiles_h * shore.TILE,
            spec["center_y"],
        )
        columns = scale_approved_forest(spec)
        sprite, rocks, columns = render_fitted_cluster(
            columns, target, name, ordinal, spec
        )
        escaped = pixels_outside_bounds(sprite, target)
        if escaped:
            raise ValueError(f"{name}: {escaped} pixels escaped its bounding box")
        plain = ground_composite(sprite, target, boxed=False)
        boxed = ground_composite(sprite, target, boxed=True)
        sprite.save(out_dir / f"ground_basalt_{name}_fresh.png")
        plain.save(out_dir / f"ground_basalt_{name}_fresh_plain.png")
        boxed.save(out_dir / f"ground_basalt_{name}_fresh_boxed.png")
        panels.append(
            (
                f"{name.replace('_', ' ')} | {len(columns)} pillars + {len(rocks)} rocks",
                paired_render(plain, boxed),
            )
        )

    review = out_dir / "ground_basalt_small_medium_large_fresh_pairs.png"
    shore.write_review_sheet(review, panels, columns=3, scale=2)
    print(review.resolve())
    return 0


def scale_approved_forest(spec: dict[str, float | int | tuple[int, int]]) -> list[pillars.Column]:
    """Adapt the approved standalone forest instead of filling a footprint."""
    source_center_x = 70.0
    source_center_y = 110.0
    target_center_x = 72.0
    source = pillars.build_column_forest(SEED)
    source_radius_x = spec.get("source_radius_x")
    if source_radius_x is not None:
        source_radius_y = float(spec["source_radius_y"])
        source = [
            column
            for column in source
            if ((column.x - source_center_x) / float(source_radius_x)) ** 2
            + ((column.base_y - source_center_y) / source_radius_y) ** 2
            <= 1.0
        ]
    return [
        pillars.Column(
            x=target_center_x + (column.x - source_center_x) * float(spec["x_scale"]),
            base_y=float(spec["base_center_y"])
            + (column.base_y - source_center_y) * float(spec["base_y_scale"]),
            radius=column.radius * float(spec["x_scale"]),
            height=column.height * float(spec["height_scale"]),
            seed=column.seed,
        )
        for column in source
    ]


def target_bounds(width: int, height: int, center_y: float) -> tuple[int, int, int, int]:
    left = round(72.0 - width / 2)
    top = round(center_y - height / 2)
    return left, top, left + width - 1, top + height - 1


def render_fitted_cluster(
    columns: list[pillars.Column],
    target: tuple[int, int, int, int],
    name: str,
    ordinal: int,
    spec: dict[str, float | int | tuple[int, int]],
) -> tuple[Image.Image, list[pillars.Column], list[pillars.Column]]:
    """Fit the uncut formation, rocks, and fitted shadow into its box."""
    for _ in range(5):
        sprite, rocks = family.render_ground_sprite(
            columns,
            family_name=f"fresh_{name}",
            variant="fresh",
            companion_count=int(spec["rocks"]),
            companion_seed_salt=ordinal * 0x8191,
            companion_radius_scale=1.70,
            companion_height_scale=1.45,
            companion_outset=1.55,
            unified_shadow=True,
            body_clip_bounds=None,
            shadow_clip_bounds=target,
        )
        if not bool(spec.get("fit_to_box", False)) or not pixels_outside_bounds(sprite, target):
            return sprite, rocks, columns
        columns = scale_columns_to_target(columns, sprite, target)
    return sprite, rocks, columns


def scale_columns_to_target(
    columns: list[pillars.Column],
    image: Image.Image,
    target: tuple[int, int, int, int],
) -> list[pillars.Column]:
    alpha = np.asarray(image.convert("RGBA"), dtype=np.uint8)[:, :, 3]
    ys, xs = np.where(alpha > 8)
    if not xs.size:
        raise ValueError("cannot fit an empty basalt formation")
    left, top, right, bottom = target
    source_left, source_right = int(xs.min()), int(xs.max())
    source_top, source_bottom = int(ys.min()), int(ys.max())
    source_width = source_right - source_left + 1
    source_height = source_bottom - source_top + 1
    available_width = right - left - 1
    available_height = bottom - top - 1
    scale = min(0.96, available_width / source_width, available_height / source_height)
    source_center_x = (source_left + source_right) * 0.5
    source_center_y = (source_top + source_bottom) * 0.5
    target_center_x = (left + right) * 0.5
    target_center_y = (top + bottom) * 0.5
    return [
        pillars.Column(
            x=target_center_x + (column.x - source_center_x) * scale,
            base_y=target_center_y + (column.base_y - source_center_y) * scale,
            radius=column.radius * scale,
            height=column.height * scale,
            seed=column.seed,
        )
        for column in columns
    ]


def pixels_outside_bounds(
    image: Image.Image,
    bounds: tuple[int, int, int, int],
) -> int:
    alpha = np.asarray(image.convert("RGBA"), dtype=np.uint8)[:, :, 3]
    inside = np.zeros(alpha.shape, dtype=bool)
    left, top, right, bottom = bounds
    inside[top : bottom + 1, left : right + 1] = True
    return int(np.count_nonzero((alpha > 8) & ~inside))


def ground_composite(
    sprite: Image.Image,
    target: tuple[int, int, int, int],
    *,
    boxed: bool,
) -> Image.Image:
    canvas = shore.checker_composite(
        np.asarray(sprite.convert("RGBA"), dtype=np.uint8)
    ).convert("RGBA")
    if boxed:
        draw = ImageDraw.Draw(canvas)
        left, top, right, bottom = target
        for x in range(left, right + 2, shore.TILE):
            draw.line((x, top, x, bottom), fill=(42, 224, 210, 230), width=1)
        for y in range(top, bottom + 2, shore.TILE):
            draw.line((left, y, right, y), fill=(42, 224, 210, 230), width=1)
        draw.rectangle(target, outline=(42, 224, 210, 255), width=2)
    return canvas.convert("RGB")


def paired_render(plain: Image.Image, boxed: Image.Image) -> Image.Image:
    gutter = 3
    pair = Image.new(
        "RGB",
        (plain.width + gutter + boxed.width, max(plain.height, boxed.height)),
        (58, 68, 78),
    )
    pair.paste(plain, (0, 0))
    pair.paste(boxed, (plain.width + gutter, 0))
    return pair


if __name__ == "__main__":
    raise SystemExit(main())
