#!/usr/bin/env python
"""Generate a large standalone forest of textured hexagonal basalt columns."""

from __future__ import annotations

import argparse
import math
from dataclasses import dataclass
from pathlib import Path
from random import Random

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

import generate_sh04_alpha_beach_prototype as shore


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUT_DIR = Path.home() / "Documents/agents/volcanic-theater/shorelines/workbench"
NATIVE_SIZE = 144
SCALE = 2
RENDER_SIZE = NATIVE_SIZE * SCALE
HEIGHT_SCALE = 0.5
CAST_SHADOW_COLOR = (32, 20, 20)
GROUND_CAST_SHADOW_OPACITY = 2.0
GROUND_CAST_SHADOW_ALPHA_CAP = 230
LAVA_CAST_SHADOW_OPACITY = 1.25
LAVA_CAST_SHADOW_ALPHA_CAP = 180


@dataclass(frozen=True)
class Column:
    x: float
    base_y: float
    radius: float
    height: float
    seed: int


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    args = parser.parse_args()
    out_dir = args.out_dir.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    columns = build_column_forest(seed=0xBA5A17)
    sprite, height_diagnostic, footprint_diagnostic = render_forest(columns)
    lava_sprite, _, _ = render_forest(columns, lava_contact=True)
    clean_columns, _, _ = render_forest(columns, include_shadow=False)
    glowing_columns, _, _ = render_forest(
        columns,
        lava_contact=True,
        include_shadow=False,
    )
    sprite.save(out_dir / "basalt_hex_column_cluster_standalone.png")
    lava_sprite.save(out_dir / "basalt_hex_column_cluster_lava_standalone.png")

    ground = volcanic_ground_3x3()
    lava = volcanic_lava_3x3()
    molten = molten_lava_texture()
    checker = shore.checker_composite(np.asarray(sprite, dtype=np.uint8))
    ground_context = composite_rgba(ground, sprite)
    offshore_background, pool_mask = lava_pool_background(
        lava,
        molten,
        columns,
        darken_rim=False,
    )
    offshore_context = composite_rgba(offshore_background, lava_sprite)
    pool_background, _ = lava_pool_background(
        ground,
        molten,
        columns,
        darken_rim=True,
    )
    pool_context = composite_rgba(pool_background, lava_sprite)
    ground_context.save(out_dir / "basalt_hex_column_cluster_ground_standalone.png")
    offshore_context.save(out_dir / "basalt_hex_column_offshore_standalone.png")
    pool_context.save(out_dir / "basalt_hex_column_lava_pool_standalone.png")
    pool_mask.save(out_dir / "basalt_hex_column_lava_pool_mask_standalone.png")
    molten.save(out_dir / "molten_lava_texture_standalone.png")
    save_gimp_edit_kit(
        out_dir,
        columns,
        clean_columns,
        glowing_columns,
        ground,
        lava,
        molten,
        pool_mask,
        ground_context,
    )

    shore.write_review_sheet(
        out_dir / "basalt_pillar_study_standalone.png",
        [
            ("Large hexagonal basalt column forest", checker),
            ("Column forest on volcanic ground", ground_context),
            ("Per-column height structure", height_diagnostic),
            ("Cluster footprint and edge fragments", footprint_diagnostic),
        ],
        columns=2,
        scale=2,
    )
    shore.write_review_sheet(
        out_dir / "basalt_pillar_context_standalone.png",
        [
            ("Approved volcanic ground", ground),
            ("Large column forest", ground_context),
            ("Offshore with molten lava at the bases", offshore_context),
            ("Column forest over a molten lava pool", pool_context),
        ],
        columns=2,
        scale=2,
    )
    shore.write_review_sheet(
        out_dir / "basalt_pillar_environment_comparison_standalone.png",
        [
            ("Ground: no emissive response", ground_context),
            ("Offshore: molten pool and bright contact glow", offshore_context),
            ("Ground placement with molten lava pool", pool_context),
            ("Lava-pool placement mask", pool_mask.convert("RGB")),
        ],
        columns=2,
        scale=2,
    )

    print((out_dir / "basalt_pillar_study_standalone.png").resolve())
    print((out_dir / "basalt_pillar_context_standalone.png").resolve())
    print((out_dir / "basalt_pillar_environment_comparison_standalone.png").resolve())
    print((out_dir / "basalt_hex_column_cluster_standalone.png").resolve())
    return 0


def build_column_forest(seed: int) -> list[Column]:
    rng = Random(seed)
    columns: list[Column] = []
    center_x = 70.0
    center_y = 110.0
    row_spacing = 6.8
    column_spacing = 10.2
    serial = 0

    for row in range(-4, 5):
        for column in range(-6, 7):
            x = center_x + column * column_spacing + (row & 1) * column_spacing * 0.5
            base_y = center_y + row * row_spacing
            normalized = ((x - center_x) / 56.0) ** 2 + ((base_y - center_y) / 28.0) ** 2
            boundary_noise = rng.uniform(-0.18, 0.18)
            if normalized > 1.0 + boundary_noise:
                continue
            if rng.random() < max(0.0, normalized - 0.62) * 0.34:
                continue

            core = max(0.0, 1.0 - normalized)
            height = 25.0 + core * 54.0 + rng.uniform(-12.0, 14.0)
            if core > 0.42 and rng.random() < 0.24:
                height += rng.uniform(18.0, 34.0)
            if normalized > 0.72:
                height *= rng.uniform(0.48, 0.78)
            radius = rng.uniform(5.0, 8.0) * (0.92 + core * 0.12)
            columns.append(
                Column(
                    x=x + rng.uniform(-1.6, 1.6),
                    base_y=base_y + rng.uniform(-1.0, 1.0),
                    radius=radius,
                    height=max(10.0, min(89.0, height * 0.87)) * HEIGHT_SCALE,
                    seed=seed + serial * 977,
                )
            )
            serial += 1

    detached = (
        Column(13.0, 120.0, 6.0, 19.0 * HEIGHT_SCALE, seed ^ 0x101),
        Column(122.0, 122.0, 7.0, 23.0 * HEIGHT_SCALE, seed ^ 0x102),
        Column(115.0, 104.0, 4.5, 14.0 * HEIGHT_SCALE, seed ^ 0x103),
        Column(21.0, 100.0, 4.5, 13.0 * HEIGHT_SCALE, seed ^ 0x104),
    )
    columns.extend(detached)
    return columns


def render_forest(
    columns: list[Column],
    lava_contact: bool = False,
    include_shadow: bool = True,
    material: str = "basalt",
) -> tuple[Image.Image, Image.Image, Image.Image]:
    canvas = Image.new("RGBA", (RENDER_SIZE, RENDER_SIZE), (0, 0, 0, 0))
    if include_shadow:
        shadow_layer = forest_shadow(
            columns,
            opacity_scale=(
                LAVA_CAST_SHADOW_OPACITY
                if lava_contact
                else GROUND_CAST_SHADOW_OPACITY
            ),
            alpha_cap=(
                LAVA_CAST_SHADOW_ALPHA_CAP
                if lava_contact
                else GROUND_CAST_SHADOW_ALPHA_CAP
            ),
        )
        canvas.alpha_composite(shadow_layer)

    for column in sorted(columns, key=lambda item: (item.base_y, item.x)):
        render_column(
            canvas,
            column,
            lava_contact=lava_contact,
            material=material,
        )

    native = resize_native_rgba(canvas)
    return native, height_map(columns), footprint_map(columns)


def resize_native_rgba(image: Image.Image) -> Image.Image:
    native = image.resize((NATIVE_SIZE, NATIVE_SIZE), Image.Resampling.LANCZOS)
    alpha = native.getchannel("A")
    sharpened_rgb = native.convert("RGB").filter(
        ImageFilter.UnsharpMask(radius=0.55, percent=55, threshold=4)
    )
    native = sharpened_rgb.convert("RGBA")
    native.putalpha(alpha)
    native_rgba = np.asarray(native, dtype=np.uint8).copy()
    margin = 2
    native_rgba[:margin, :, :] = 0
    native_rgba[-margin:, :, :] = 0
    native_rgba[:, :margin, :] = 0
    native_rgba[:, -margin:, :] = 0
    return Image.fromarray(native_rgba, mode="RGBA")


def save_gimp_edit_kit(
    out_dir: Path,
    columns: list[Column],
    clean_columns: Image.Image,
    glowing_columns: Image.Image,
    ground: Image.Image,
    clear_lava: Image.Image,
    molten: Image.Image,
    pool_mask: Image.Image,
    ground_reference: Image.Image,
) -> None:
    clean_rgba = np.asarray(clean_columns.convert("RGBA"), dtype=np.uint8)
    glowing_rgba = np.asarray(glowing_columns.convert("RGBA"), dtype=np.uint8)
    rgb_difference = np.max(
        np.abs(
            glowing_rgba[:, :, :3].astype(np.int16)
            - clean_rgba[:, :, :3].astype(np.int16)
        ),
        axis=2,
    )
    lower_face_mask = (rgb_difference > 3) & (glowing_rgba[:, :, 3] > 0)

    glow_layer = glowing_rgba.copy()
    glow_layer[:, :, 3] = np.where(
        lower_face_mask,
        glowing_rgba[:, :, 3],
        0,
    ).astype(np.uint8)

    pool = np.asarray(pool_mask.convert("L"), dtype=np.uint8) > 0
    pool_alpha = shore.feather_alpha(pool, 2.0)
    molten_rgba = np.zeros((NATIVE_SIZE, NATIVE_SIZE, 4), dtype=np.uint8)
    molten_rgba[:, :, :3] = np.asarray(molten.convert("RGB"), dtype=np.uint8)
    molten_rgba[:, :, 3] = pool_alpha

    shadow = resize_native_rgba(
        forest_shadow(
            columns,
            opacity_scale=GROUND_CAST_SHADOW_OPACITY,
            alpha_cap=GROUND_CAST_SHADOW_ALPHA_CAP,
        )
    )
    column_alpha = Image.fromarray(clean_rgba[:, :, 3], mode="L")
    lower_mask_image = Image.fromarray(
        np.where(lower_face_mask, 255, 0).astype(np.uint8),
        mode="L",
    )

    clean_columns.save(out_dir / "gimp_edit_columns_clean.png")
    shadow.save(out_dir / "gimp_edit_columns_shadow.png")
    Image.fromarray(glow_layer, mode="RGBA").save(
        out_dir / "gimp_edit_contact_glow_layer.png"
    )
    Image.fromarray(molten_rgba, mode="RGBA").save(
        out_dir / "gimp_edit_molten_pool_layer.png"
    )
    ground.save(out_dir / "gimp_edit_ground_background.png")
    clear_lava.save(out_dir / "gimp_edit_clear_lava_background.png")
    ground_reference.save(out_dir / "gimp_edit_ground_reference.png")
    column_alpha.save(out_dir / "gimp_edit_column_mask.png")
    pool_mask.save(out_dir / "gimp_edit_pool_mask.png")
    lower_mask_image.save(out_dir / "gimp_edit_lower_face_mask.png")


def forest_shadow(
    columns: list[Column],
    opacity_scale: float,
    *,
    alpha_cap: int = 125,
) -> Image.Image:
    mask = Image.new("L", (RENDER_SIZE, RENDER_SIZE), 0)
    draw = ImageDraw.Draw(mask)
    for column in columns:
        base = hex_points(column.x, column.base_y, column.radius * 1.04, column.seed)
        projection_x = 3.0 + column.height * 0.58
        projection_y = 0.0
        projected = [
            (x + projection_x, y + projection_y)
            for x, y in base
        ]
        hull = convex_hull(base + projected)
        shadow = [
            (round(x * SCALE), round(y * SCALE))
            for x, y in hull
        ]
        draw.polygon(shadow, fill=min(195, round(74 + column.height * 1.9)))
    blurred = ndimage.gaussian_filter(
        np.asarray(mask, dtype=np.float32),
        sigma=1.35 * SCALE,
    )
    rgba = np.zeros((RENDER_SIZE, RENDER_SIZE, 4), dtype=np.uint8)
    rgba[:, :, :3] = np.asarray(CAST_SHADOW_COLOR, dtype=np.uint8)
    rgba[:, :, 3] = np.clip(
        np.rint(blurred * opacity_scale), 0, alpha_cap
    ).astype(np.uint8)
    return Image.fromarray(rgba, mode="RGBA")


def convex_hull(
    points: list[tuple[float, float]],
) -> list[tuple[float, float]]:
    ordered = sorted(set(points))
    if len(ordered) <= 1:
        return ordered

    def cross(
        origin: tuple[float, float],
        first: tuple[float, float],
        second: tuple[float, float],
    ) -> float:
        return (
            (first[0] - origin[0]) * (second[1] - origin[1])
            - (first[1] - origin[1]) * (second[0] - origin[0])
        )

    lower: list[tuple[float, float]] = []
    for point in ordered:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], point) <= 0:
            lower.pop()
        lower.append(point)
    upper: list[tuple[float, float]] = []
    for point in reversed(ordered):
        while len(upper) >= 2 and cross(upper[-2], upper[-1], point) <= 0:
            upper.pop()
        upper.append(point)
    return lower[:-1] + upper[:-1]


def render_column(
    canvas: Image.Image,
    column: Column,
    lava_contact: bool,
    material: str,
) -> None:
    base = hex_points(column.x, column.base_y, column.radius, column.seed)
    lean = min(2.2, column.height * 0.018)
    top = hex_points(
        column.x - lean,
        column.base_y - column.height,
        column.radius,
        column.seed,
    )

    if material == "ground_rock":
        visible_faces = (
            (0, (31, 32, 34), 0x21),
            (1, (36, 36, 37), 0x43),
            (2, (42, 41, 40), 0x65),
        )
    elif material == "basalt":
        visible_faces = (
            (0, (32, 33, 35), 0x21),
            (1, (43, 41, 39), 0x43),
            (2, (54, 51, 47), 0x65),
        )
    else:
        raise ValueError(f"unknown column material: {material}")
    for edge, color, salt in visible_faces:
        next_edge = (edge + 1) % 6
        textured_polygon(
            canvas,
            [top[edge], top[next_edge], base[next_edge], base[edge]],
            color,
            column.seed ^ salt,
            vertical=True,
            top_y=min(top[edge][1], top[next_edge][1]),
            bottom_y=max(base[edge][1], base[next_edge][1]),
            lava_contact=lava_contact,
        )

    cap_rng = Random(column.seed ^ 0xCA9)
    if material == "ground_rock":
        cap_base = (
            cap_rng.randint(39, 45),
            cap_rng.randint(39, 44),
            cap_rng.randint(39, 43),
        )
    else:
        cap_base = (
            cap_rng.randint(48, 55),
            cap_rng.randint(47, 54),
            cap_rng.randint(45, 52),
        )
    textured_polygon(
        canvas,
        top,
        cap_base,
        column.seed ^ 0xA57,
        vertical=False,
        top_y=min(y for _, y in top),
        bottom_y=max(y for _, y in top),
        lava_contact=False,
    )
    add_cap_fractures(canvas, top, column.seed)


def textured_polygon(
    canvas: Image.Image,
    points: list[tuple[float, float]],
    base_color: tuple[int, int, int],
    seed: int,
    vertical: bool,
    top_y: float,
    bottom_y: float,
    lava_contact: bool,
) -> None:
    scaled = [(round(x * SCALE), round(y * SCALE)) for x, y in points]
    mask_image = Image.new("L", canvas.size, 0)
    ImageDraw.Draw(mask_image).polygon(scaled, fill=255)
    mask = np.asarray(mask_image, dtype=np.uint8) > 0
    if not np.any(mask):
        return

    yy, xx = np.mgrid[0:RENDER_SIZE, 0:RENDER_SIZE].astype(np.float32)
    rng = np.random.default_rng(seed)
    grain = rng.normal(0.0, 1.0, (RENDER_SIZE, RENDER_SIZE)).astype(np.float32)
    grain = ndimage.gaussian_filter(grain, sigma=0.42 * SCALE)
    grain /= max(1e-6, float(np.std(grain)))
    broad = ndimage.gaussian_filter(grain, sigma=1.8 * SCALE)
    broad /= max(1e-6, float(np.std(broad)))

    if vertical:
        phase = (seed & 255) / 255.0 * math.tau
        grooves = np.sin(xx / SCALE * 1.42 + phase) + 0.46 * np.sin(xx / SCALE * 2.91 + phase * 0.7)
        vertical_position = np.clip(
            (yy / SCALE - top_y) / max(1.0, bottom_y - top_y),
            0.0,
            1.0,
        )
        modulation = grain * 3.2 + broad * 2.0 + grooves * 3.8 - vertical_position * 5.5
    else:
        light_gradient = -(xx + yy) / (RENDER_SIZE * 1.8)
        modulation = grain * 3.3 + broad * 2.2 + light_gradient * 5.0

    edge_distance = ndimage.distance_transform_edt(mask) / SCALE
    modulation -= np.clip(1.35 - edge_distance, 0.0, 1.35) * 6.0
    texture = np.zeros((RENDER_SIZE, RENDER_SIZE, 4), dtype=np.uint8)
    for channel, base in enumerate(base_color):
        texture[:, :, channel] = np.clip(np.rint(base + modulation), 8, 105).astype(np.uint8)
    texture[:, :, 3] = np.where(mask, 255, 0).astype(np.uint8)

    if vertical and lava_contact:
        distance_from_base = bottom_y - yy / SCALE
        contact = np.clip((9.0 - distance_from_base) / 9.0, 0.0, 1.0) * mask
        contact *= 0.96
        hot = np.clip((1.75 - distance_from_base) / 1.75, 0.0, 1.0) * mask
        target = np.zeros((RENDER_SIZE, RENDER_SIZE, 3), dtype=np.float32)
        target[:, :, :] = np.asarray((218.0, 60.0, 8.0), dtype=np.float32)
        target = target * (1.0 - hot[:, :, None] * 0.88) + np.asarray(
            (255.0, 190.0, 46.0), dtype=np.float32
        ) * (hot[:, :, None] * 0.88)
        current = texture[:, :, :3].astype(np.float32)
        current = current * (1.0 - contact[:, :, None]) + target * contact[:, :, None]
        texture[:, :, :3] = np.clip(np.rint(current), 0, 255).astype(np.uint8)

    pit_noise = rng.random((RENDER_SIZE, RENDER_SIZE))
    pits = mask & (pit_noise > 0.991)
    texture[pits, :3] = np.maximum(10, texture[pits, :3].astype(np.int16) - 16).astype(np.uint8)
    canvas.alpha_composite(Image.fromarray(texture, mode="RGBA"))


def add_cap_fractures(
    canvas: Image.Image,
    top: list[tuple[float, float]],
    seed: int,
) -> None:
    rng = Random(seed ^ 0xF4C)
    center_x = sum(x for x, _ in top) / len(top)
    center_y = sum(y for _, y in top) / len(top)
    draw = ImageDraw.Draw(canvas)
    crack_color = (25, 24, 24, 175)
    for _ in range(rng.choice((1, 1, 2))):
        vertex = top[rng.randrange(len(top))]
        mid_x = center_x + rng.uniform(-1.2, 1.2)
        mid_y = center_y + rng.uniform(-0.8, 0.8)
        draw.line(
            [
                (round(vertex[0] * SCALE), round(vertex[1] * SCALE)),
                (round(mid_x * SCALE), round(mid_y * SCALE)),
            ],
            fill=crack_color,
            width=1,
        )


def hex_points(
    center_x: float,
    center_y: float,
    radius: float,
    seed: int,
) -> list[tuple[float, float]]:
    rng = Random(seed)
    result: list[tuple[float, float]] = []
    for index in range(6):
        angle = math.radians(index * 60)
        local_radius = radius * rng.uniform(0.88, 1.10)
        result.append(
            (
                center_x + math.cos(angle) * local_radius,
                center_y + math.sin(angle) * local_radius * 0.54,
            )
        )
    return result


def height_map(columns: list[Column]) -> Image.Image:
    image = Image.new("L", (NATIVE_SIZE, NATIVE_SIZE), 0)
    draw = ImageDraw.Draw(image)
    max_height = max(column.height for column in columns)
    for column in sorted(columns, key=lambda item: item.height):
        top = hex_points(
            column.x - min(2.2, column.height * 0.018),
            column.base_y - column.height,
            column.radius,
            column.seed,
        )
        draw.polygon(top, fill=round(column.height / max_height * 255.0))
    return image.convert("RGB")


def footprint_map(columns: list[Column]) -> Image.Image:
    image = Image.new("RGB", (NATIVE_SIZE, NATIVE_SIZE), (18, 18, 18))
    draw = ImageDraw.Draw(image)
    for column in sorted(columns, key=lambda item: item.base_y):
        base = hex_points(column.x, column.base_y, column.radius, column.seed)
        value = max(70, min(220, round(70 + column.height * 1.35)))
        draw.polygon(base, fill=(value, value, value))
    return image


def lava_pool_background(
    background: Image.Image,
    molten: Image.Image,
    columns: list[Column],
    darken_rim: bool,
) -> tuple[Image.Image, Image.Image]:
    base_mask_image = Image.new("L", (NATIVE_SIZE, NATIVE_SIZE), 0)
    draw = ImageDraw.Draw(base_mask_image)
    for column in columns:
        base = hex_points(column.x, column.base_y, column.radius + 2.0, column.seed)
        draw.polygon(base, fill=255)
    base_mask = np.asarray(base_mask_image, dtype=np.uint8) > 0

    distance = ndimage.distance_transform_edt(~base_mask)
    rng = np.random.default_rng(0x1A7A9001)
    edge_noise = rng.random((NATIVE_SIZE, NATIVE_SIZE), dtype=np.float32)
    edge_noise = ndimage.gaussian_filter(edge_noise, sigma=3.2, mode="reflect")
    edge_noise = (edge_noise - edge_noise.min()) / max(
        1e-6,
        float(edge_noise.max() - edge_noise.min()),
    )
    pool = distance <= 11.0 + (edge_noise - 0.5) * 5.0
    pool = ndimage.binary_closing(pool, structure=np.ones((3, 3), dtype=bool))
    pool[:3, :] = False
    pool[-3:, :] = False
    pool[:, :3] = False
    pool[:, -3:] = False

    background_rgb = np.asarray(background.convert("RGB"), dtype=np.uint8).copy()
    molten_rgb = np.asarray(molten.convert("RGB"), dtype=np.uint8)
    if darken_rim:
        soot_rim = ndimage.binary_dilation(pool, iterations=3) & ~pool
        background_rgb[soot_rim] = np.clip(
            np.rint(background_rgb[soot_rim].astype(np.float32) * 0.72),
            0,
            255,
        ).astype(np.uint8)
    alpha = shore.feather_alpha(pool, 2.0)
    combined = shore.linear_alpha_composite(background_rgb, molten_rgb, alpha)
    return (
        Image.fromarray(combined, mode="RGB"),
        Image.fromarray(np.where(pool, 255, 0).astype(np.uint8), mode="L"),
    )


def molten_lava_texture() -> Image.Image:
    yy, xx = np.mgrid[0:NATIVE_SIZE, 0:NATIVE_SIZE].astype(np.float32)
    rng = np.random.default_rng(0xF10A7A)
    broad = rng.random((NATIVE_SIZE, NATIVE_SIZE), dtype=np.float32)
    broad = ndimage.gaussian_filter(broad, sigma=7.0, mode="wrap")
    broad = normalize(broad)
    medium = rng.random((NATIVE_SIZE, NATIVE_SIZE), dtype=np.float32)
    medium = ndimage.gaussian_filter(medium, sigma=2.8, mode="wrap")
    medium = normalize(medium)

    warp_x = xx + (broad - 0.5) * 17.0
    warp_y = yy + (medium - 0.5) * 11.0
    flow = (
        np.sin(warp_x * 0.115 + warp_y * 0.031)
        + 0.62 * np.sin(warp_y * 0.092 - warp_x * 0.027 + 1.7)
        + 0.34 * np.sin((warp_x + warp_y) * 0.061 + 3.1)
    )
    flow = normalize(flow)
    heat = np.clip(0.28 + flow * 0.62 + (medium - 0.5) * 0.18, 0.0, 1.0)

    stops = np.asarray((0.0, 0.38, 0.70, 0.90, 1.0), dtype=np.float32)
    colors = np.asarray(
        (
            (126.0, 18.0, 3.0),
            (190.0, 36.0, 2.0),
            (238.0, 78.0, 4.0),
            (255.0, 145.0, 18.0),
            (255.0, 224.0, 82.0),
        ),
        dtype=np.float32,
    )
    rgb = np.empty((NATIVE_SIZE, NATIVE_SIZE, 3), dtype=np.float32)
    for channel in range(3):
        rgb[:, :, channel] = np.interp(heat, stops, colors[:, channel])

    hot_filaments = heat > 0.91
    rgb[hot_filaments] = np.maximum(rgb[hot_filaments], np.asarray((255.0, 176.0, 35.0)))
    return Image.fromarray(np.clip(np.rint(rgb), 0, 255).astype(np.uint8), mode="RGB")


def normalize(values: np.ndarray) -> np.ndarray:
    low = float(values.min())
    high = float(values.max())
    return (values - low) / max(1e-6, high - low)


def volcanic_ground_3x3() -> Image.Image:
    return volcanic_frame_3x3("clear1.vol", expected_frames=16)


def volcanic_lava_3x3() -> Image.Image:
    return volcanic_frame_3x3("w1.vol", expected_frames=1)


def volcanic_frame_3x3(filename: str, expected_frames: int) -> Image.Image:
    palette = shore.read_palette(ROOT / "mods/cameo/bits/volcanic/volcanic.pal")
    indices = shore.unique_frame(
        ROOT / "mods/cameo/bits/volcanic" / filename,
        expected_frames=expected_frames,
    )
    tiled = np.tile(indices, (3, 3))
    return Image.fromarray(shore.indices_rgb(tiled, palette), mode="RGB")


def composite_rgba(background: Image.Image, foreground: Image.Image) -> Image.Image:
    result = background.convert("RGBA")
    result.alpha_composite(foreground)
    return result.convert("RGB")


if __name__ == "__main__":
    raise SystemExit(main())
