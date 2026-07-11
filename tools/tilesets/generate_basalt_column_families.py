#!/usr/bin/env python
"""Generate tiny through approved-large basalt column families."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from random import Random

import numpy as np
from PIL import Image, ImageDraw

import generate_basalt_pillar_study as pillars
import generate_sh04_alpha_beach_prototype as shore


DEFAULT_OUT_DIR = Path.home() / "Documents/agents/volcanic-theater/shorelines/workbench"
SEED = 0xBA5A17
CENTER_X = 70.0
CENTER_Y = 110.0
COMPANION_COUNTS = {"tiny": 0, "small": 10, "medium": 18, "large": 28}
FAMILY_SEED_SALTS = {"tiny": 0x11, "small": 0x23, "medium": 0x47, "large": 0x89}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    args = parser.parse_args()
    out_dir = args.out_dir.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    families = build_families()
    normal_clean_panels: list[tuple[str, Image.Image]] = []
    normal_glowing_panels: list[tuple[str, Image.Image]] = []
    shorter_clean_panels: list[tuple[str, Image.Image]] = []
    shorter_glowing_panels: list[tuple[str, Image.Image]] = []
    metadata: dict[str, object] = {
        "preview_only": True,
        "native_size": pillars.NATIVE_SIZE,
        "height_scale": pillars.HEIGHT_SCALE,
        "lava_glow_height_pixels": 9.0,
        "families": {},
    }
    for name, columns in families.items():
        variants = {
            "normal": columns,
            "shorter": scale_heights(columns, 0.5),
        }
        metadata["families"][name] = {}
        for variant, variant_columns in variants.items():
            clean, companion_rocks = render_ground_sprite(
                variant_columns,
                family_name=name,
                variant=variant,
            )
            glowing, _, _ = pillars.render_forest(
                variant_columns,
                lava_contact=True,
                include_shadow=True,
            )
            suffix = "" if variant == "normal" else "_shorter"
            clean.save(out_dir / f"basalt_columns_clean_{name}{suffix}.png")
            glowing.save(out_dir / f"basalt_columns_glowing_{name}{suffix}.png")
            target_clean = (
                normal_clean_panels
                if variant == "normal"
                else shorter_clean_panels
            )
            target_glowing = (
                normal_glowing_panels
                if variant == "normal"
                else shorter_glowing_panels
            )
            target_clean.append(
                (
                    f"{name.title()}: ground / {variant}",
                    shore.checker_composite(np.asarray(clean, dtype=np.uint8)),
                )
            )
            target_glowing.append(
                (
                    f"{name.title()}: lava / {variant} / 9px glow",
                    shore.checker_composite(np.asarray(glowing, dtype=np.uint8)),
                )
            )
            metadata["families"][name][variant] = family_metadata(
                variant_columns,
                clean,
                glowing,
            )
            metadata["families"][name][variant]["companion_rock_count"] = len(
                companion_rocks
            )

    review_path = out_dir / "basalt_column_family_clean_glowing_comparison.png"
    shore.write_review_sheet(
        review_path,
        normal_clean_panels + normal_glowing_panels,
        columns=4,
        scale=3,
    )
    shorter_review_path = out_dir / "basalt_column_family_shorter_comparison.png"
    shore.write_review_sheet(
        shorter_review_path,
        shorter_clean_panels + shorter_glowing_panels,
        columns=4,
        scale=3,
    )
    metadata_path = out_dir / "basalt_column_family_metadata.json"
    metadata_path.write_text(json.dumps(metadata, indent=2), encoding="utf-8")
    print(review_path.resolve())
    print(shorter_review_path.resolve())
    print(metadata_path.resolve())
    return 0


def build_families() -> dict[str, list[pillars.Column]]:
    approved = pillars.build_column_forest(SEED)
    main = approved[:-4]
    small = scale_heights(
        radial_subset(main, radius_x=15.0, radius_y=10.0),
        0.5,
    )
    medium = scale_heights(
        radial_subset(main, radius_x=32.0, radius_y=18.0),
        0.5,
    )
    if len(small) < 3 or len(medium) <= len(small):
        raise ValueError("column family selection did not produce a useful hierarchy")
    tiny = scale_geometry(small, footprint_factor=0.5, height_factor=0.5)
    return {
        "tiny": tiny,
        "small": small,
        "medium": medium,
        "large": approved,
    }


def scale_geometry(
    columns: list[pillars.Column],
    *,
    footprint_factor: float,
    height_factor: float,
) -> list[pillars.Column]:
    return [
        pillars.Column(
            x=CENTER_X + (column.x - CENTER_X) * footprint_factor,
            base_y=CENTER_Y + (column.base_y - CENTER_Y) * footprint_factor,
            radius=column.radius * footprint_factor,
            height=column.height * height_factor,
            seed=column.seed,
        )
        for column in columns
    ]


def render_ground_sprite(
    columns: list[pillars.Column],
    *,
    family_name: str,
    variant: str,
) -> tuple[Image.Image, list[pillars.Column]]:
    main, _, _ = pillars.render_forest(
        columns,
        lava_contact=False,
        include_shadow=False,
    )
    rocks = build_companion_rocks(
        columns,
        family_name=family_name,
        variant=variant,
    )
    if not rocks:
        shadow = pillars.resize_native_rgba(
            pillars.forest_shadow(
                columns,
                opacity_scale=pillars.GROUND_CAST_SHADOW_OPACITY,
                alpha_cap=pillars.GROUND_CAST_SHADOW_ALPHA_CAP,
            )
        )
        result = shadow.copy()
        result.alpha_composite(main)
        return result, rocks
    rock_layer, _, _ = pillars.render_forest(
        rocks,
        lava_contact=False,
        include_shadow=False,
        material="ground_rock",
    )
    shadow = pillars.resize_native_rgba(
        pillars.forest_shadow(
            columns + rocks,
            opacity_scale=pillars.GROUND_CAST_SHADOW_OPACITY,
            alpha_cap=pillars.GROUND_CAST_SHADOW_ALPHA_CAP,
        )
    )
    result = shadow.copy()
    result.alpha_composite(main)
    result.alpha_composite(rock_layer)
    return result, rocks


def build_companion_rocks(
    columns: list[pillars.Column],
    *,
    family_name: str,
    variant: str,
) -> list[pillars.Column]:
    count = COMPANION_COUNTS[family_name]
    if count == 0:
        return []
    seed = SEED ^ FAMILY_SEED_SALTS[family_name]
    if variant == "shorter":
        seed ^= 0x5A07
    rng = Random(seed)
    contour = base_bottom_contour(columns)
    valid_x = np.flatnonzero(contour >= 0)
    left = int(valid_x.min())
    right = int(valid_x.max())
    average_radius = sum(column.radius for column in columns) / len(columns)
    maximum_height = max(column.height for column in columns)
    rocks: list[pillars.Column] = []
    for index in range(count):
        segment_left = left + (right - left) * index / count
        segment_right = left + (right - left) * (index + 1) / count
        x = rng.uniform(segment_left, segment_right)
        contour_x = int(np.clip(round(x), left, right))
        y = float(contour[contour_x])
        radius = max(0.85, average_radius * rng.uniform(0.30, 0.48))
        y -= radius * rng.uniform(0.08, 0.42)
        x = float(np.clip(x, 3.0 + radius, pillars.NATIVE_SIZE - 4.0 - radius))
        y = float(np.clip(y, 3.0 + radius, pillars.NATIVE_SIZE - 4.0 - radius))
        height_low = max(0.8, radius * 1.05)
        height_high = min(radius * 2.15, maximum_height * 0.58)
        height_high = max(height_low + 0.15, height_high)
        height = min(rng.uniform(height_low, height_high), maximum_height * 0.72)
        rocks.append(
            pillars.Column(
                x=x,
                base_y=y,
                radius=radius,
                height=height,
                seed=seed + index * 1291,
            )
        )
    return rocks


def base_bottom_contour(columns: list[pillars.Column]) -> np.ndarray:
    image = Image.new("L", (pillars.NATIVE_SIZE, pillars.NATIVE_SIZE), 0)
    draw = ImageDraw.Draw(image)
    for column in columns:
        draw.polygon(
            pillars.hex_points(
                column.x,
                column.base_y,
                column.radius,
                column.seed,
            ),
            fill=255,
        )
    mask = np.asarray(image, dtype=np.uint8) > 0
    contour = np.full(pillars.NATIVE_SIZE, -1, dtype=np.int16)
    for x in range(pillars.NATIVE_SIZE):
        ys = np.flatnonzero(mask[:, x])
        if ys.size:
            contour[x] = int(ys.max())
    return contour


def scale_heights(
    columns: list[pillars.Column],
    factor: float,
) -> list[pillars.Column]:
    return [
        pillars.Column(
            x=column.x,
            base_y=column.base_y,
            radius=column.radius,
            height=column.height * factor,
            seed=column.seed,
        )
        for column in columns
    ]


def radial_subset(
    columns: list[pillars.Column],
    *,
    radius_x: float,
    radius_y: float,
) -> list[pillars.Column]:
    return [
        column
        for column in columns
        if (
            ((column.x - CENTER_X) / radius_x) ** 2
            + ((column.base_y - CENTER_Y) / radius_y) ** 2
            <= 1.0
        )
    ]


def family_metadata(
    columns: list[pillars.Column],
    clean: Image.Image,
    glowing: Image.Image,
) -> dict[str, object]:
    base_left = min(column.x - column.radius for column in columns)
    base_right = max(column.x + column.radius for column in columns)
    base_top = min(column.base_y - column.radius for column in columns)
    base_bottom = max(column.base_y + column.radius for column in columns)
    anchor_x = round((base_left + base_right) * 0.5)
    anchor_y = round(base_bottom)
    return {
        "column_count": len(columns),
        "sprite_anchor": {"x": anchor_x, "y": anchor_y},
        "base_bounds": {
            "left": round(base_left, 2),
            "top": round(base_top, 2),
            "right": round(base_right, 2),
            "bottom": round(base_bottom, 2),
            "width": round(base_right - base_left, 2),
            "height": round(base_bottom - base_top, 2),
        },
        "clean_alpha_bounds": alpha_bounds(clean),
        "glowing_alpha_bounds": alpha_bounds(glowing),
    }


def alpha_bounds(image: Image.Image) -> dict[str, int]:
    alpha = np.asarray(image.convert("RGBA"), dtype=np.uint8)[:, :, 3]
    ys, xs = np.where(alpha > 8)
    if not xs.size:
        return {"left": 0, "top": 0, "right": 0, "bottom": 0}
    return {
        "left": int(xs.min()),
        "top": int(ys.min()),
        "right": int(xs.max()),
        "bottom": int(ys.max()),
    }


if __name__ == "__main__":
    raise SystemExit(main())
