#!/usr/bin/env python
"""Generate preview-only seamless open-lava tiles for volcanic w1/w2."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from random import Random

from PIL import Image, ImageDraw, ImageFont

from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
AUTHOR_TILE = 24
OUTPUT_TILE = 48
UPSCALE = OUTPUT_TILE // AUTHOR_TILE
BASE_PERIOD = AUTHOR_TILE
VARIANT_PERIOD = AUTHOR_TILE * 2
OUTPUT_VARIANT_PERIOD = OUTPUT_TILE * 2
BASE_SEED = 0xC1EA4A7A
VARIANT_SEED = 0xB45A17
AUTHOR_OUTER_MATCH_BAND = 8
OUTPUT_OUTER_MATCH_BAND = AUTHOR_OUTER_MATCH_BAND * UPSCALE
DEFAULT_JUNCTION_WIDENING = 2.25


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--palette",
        type=Path,
        default=ROOT / "mods/cameo/bits/volcanic/volcanic.pal",
    )
    parser.add_argument(
        "--current-w1",
        type=Path,
        default=ROOT / "mods/cameo/bits/volcanic/w1.vol",
    )
    parser.add_argument(
        "--current-w2",
        type=Path,
        default=ROOT / "mods/cameo/bits/volcanic/w2.vol",
    )
    parser.add_argument(
        "--stage",
        choices=("proof", "deformed"),
        default="proof",
        help="proof repeats the toroidal w1 topology exactly; deformed bends it inside w2",
    )
    parser.add_argument(
        "--junction-widening",
        type=float,
        default=DEFAULT_JUNCTION_WIDENING,
        help="additional Voronoi fissure width near three-way vertices, in 24x24 authoring pixels",
    )
    parser.add_argument(
        "--bright-cores",
        action=argparse.BooleanOptionalAction,
        default=True,
        help="brighten only the hottest fissure cores and junction centers",
    )
    parser.add_argument("--out-dir", type=Path, required=True)
    args = parser.parse_args()

    palette = read_palette(resolve(args.palette))
    out_dir = resolve(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    base_sites = make_sites(BASE_PERIOD, 7, BASE_SEED)
    baseline_w1_source = render_base(base_sites, 0.0, False)
    junction_only_w1_source = render_base(base_sites, args.junction_widening, False)
    w1_source = render_base(base_sites, args.junction_widening, args.bright_cores)
    repeated_w1_source = repeat_indices(w1_source, VARIANT_PERIOD)
    w2_source = (
        repeated_w1_source
        if args.stage == "proof"
        else render_variant(
            base_sites,
            w1_source,
            args.junction_widening,
            args.bright_cores,
        )
    )

    baseline_w1_indices = upscale_indices(baseline_w1_source, BASE_PERIOD, UPSCALE)
    junction_only_w1_indices = upscale_indices(junction_only_w1_source, BASE_PERIOD, UPSCALE)
    w1_indices = upscale_indices(w1_source, BASE_PERIOD, UPSCALE)
    repeated_w1_indices = upscale_indices(repeated_w1_source, VARIANT_PERIOD, UPSCALE)
    w2_indices = upscale_indices(w2_source, VARIANT_PERIOD, UPSCALE)

    w1_path = out_dir / "w1-clear-lava-preview.vol"
    w2_path = out_dir / "w2-clear-lava-preview.vol"
    write_shptd(w1_path, OUTPUT_TILE, OUTPUT_TILE, [bytes(w1_indices)])
    write_shptd(
        w2_path,
        OUTPUT_TILE,
        OUTPUT_TILE,
        slice_frames(w2_indices, OUTPUT_VARIANT_PERIOD),
    )
    verify_vol(w1_path, 1)
    verify_vol(w2_path, 4)
    _, _, roundtrip_w2_frames = read_shptd(w2_path)
    roundtrip_w2_indices = compose_frame_indices(roundtrip_w2_frames, 2)

    w1 = indices_image(w1_indices, OUTPUT_TILE, OUTPUT_TILE, palette)
    w2 = indices_image(
        w2_indices,
        OUTPUT_VARIANT_PERIOD,
        OUTPUT_VARIANT_PERIOD,
        palette,
    )
    w1.save(out_dir / "w1-preview.png")
    w2.save(out_dir / "w2-preview.png")
    w1_author = indices_image(
        w1_source,
        AUTHOR_TILE,
        AUTHOR_TILE,
        palette,
    )
    w2_author = indices_image(
        w2_source,
        VARIANT_PERIOD,
        VARIANT_PERIOD,
        palette,
    )
    w1_author.save(out_dir / "w1-author-24px.png")
    w2_author.save(out_dir / "w2-author-48px-composite.png")

    current_w1, current_w1_tiles = decode_composite(resolve(args.current_w1), 1, palette)
    current_w2, current_w2_tiles = decode_composite(resolve(args.current_w2), 2, palette)
    preview_w2_tiles = split_tiles(w2)

    w1_mask = crack_mask_image(w1_indices, OUTPUT_TILE, OUTPUT_TILE)
    w2_mask = crack_mask_image(
        w2_indices,
        OUTPUT_VARIANT_PERIOD,
        OUTPUT_VARIANT_PERIOD,
    )

    current_repeat, current_mixed = build_repeat_layouts(current_w1, current_w2)
    preview_repeat, preview_mixed = build_repeat_layouts(w1, w2)
    baseline_w1 = indices_image(
        baseline_w1_indices,
        OUTPUT_TILE,
        OUTPUT_TILE,
        palette,
    )
    baseline_w2 = indices_image(
        repeat_output_indices(baseline_w1_indices, OUTPUT_VARIANT_PERIOD),
        OUTPUT_VARIANT_PERIOD,
        OUTPUT_VARIANT_PERIOD,
        palette,
    )
    baseline_repeat, _ = build_repeat_layouts(baseline_w1, baseline_w2)
    junction_only_w1 = indices_image(
        junction_only_w1_indices,
        OUTPUT_TILE,
        OUTPUT_TILE,
        palette,
    )
    junction_only_w2 = indices_image(
        repeat_output_indices(junction_only_w1_indices, OUTPUT_VARIANT_PERIOD),
        OUTPUT_VARIANT_PERIOD,
        OUTPUT_VARIANT_PERIOD,
        palette,
    )
    junction_only_repeat, _ = build_repeat_layouts(junction_only_w1, junction_only_w2)
    current_repeat.save(out_dir / "current-w1-repeat.png")
    current_mixed.save(out_dir / "current-w1-around-w2.png")
    preview_repeat.save(out_dir / "preview-w1-repeat.png")
    preview_mixed.save(out_dir / "preview-w1-around-w2.png")

    mask_repeat, mask_mixed = build_repeat_layouts(w1_mask, w2_mask)
    mask_repeat.save(out_dir / "connectivity-mask-repeat.png")
    mask_mixed.save(out_dir / "connectivity-mask-w1-around-w2.png")

    write_continuity_review(
        out_dir / "continuity-review.png",
        [("w1 seamless repeat", preview_repeat), ("w1 surrounding w2", preview_mixed)],
    )
    write_continuity_review(
        out_dir / "current-vs-preview.png",
        [
            ("Current w1 repeat", current_repeat),
            ("Preview w1 repeat", preview_repeat),
            ("Current w1 around w2", current_mixed),
            ("Preview w1 around w2", preview_mixed),
        ],
        columns=2,
    )
    write_continuity_review(
        out_dir / "connectivity-mask-review.png",
        [
            ("Canonical w1 crack-mask repeat", mask_repeat),
            ("w1 surrounding w2 crack mask", mask_mixed),
        ],
    )
    write_continuity_review(
        out_dir / "junction-width-comparison.png",
        [
            ("Original Voronoi junction width", baseline_repeat),
            (f"Widened junctions +{args.junction_widening:.2f}", junction_only_repeat),
        ],
    )
    write_continuity_review(
        out_dir / "lava-brightness-comparison.png",
        [
            ("Current lava colors", junction_only_repeat),
            ("Brighter fissure and junction cores", preview_repeat),
        ],
    )
    write_seam_strip_review(
        out_dir / "seam-strip-audit.png",
        seam_strip_panels(w1, preview_w2_tiles),
    )

    all_seam_windows = seam_window_metrics(w1, preview_w2_tiles)
    required_seam_windows = {
        label: result
        for label, result in all_seam_windows.items()
        if args.stage == "proof" or "internal" not in label
    }
    internal_reference_differences = {
        label: result
        for label, result in all_seam_windows.items()
        if "internal" in label
    }
    metrics = {
        "stage": args.stage,
        "junction_widening": args.junction_widening,
        "bright_cores": args.bright_cores,
        "pixels_changed_by_core_brightening": len(w1_indices)
        - exact_pixels(w1_indices, junction_only_w1_indices),
        "bright_palette_pixels_before": sum(index >= 92 for index in junction_only_w1_indices),
        "bright_palette_pixels_after": sum(index >= 92 for index in w1_indices),
        "crack_mask_pixels_changed_by_brightening": sum(
            (before >= 48) != (after >= 48)
            for before, after in zip(junction_only_w1_indices, w1_indices)
        ),
        "w1_pixels_changed_from_baseline": len(w1_indices) - exact_pixels(w1_indices, baseline_w1_indices),
        "baseline_crack_pixels_removed": sum(
            before >= 48 and after < 48
            for before, after in zip(baseline_w1_indices, w1_indices)
        ),
        "basalt_pixels_added_to_crack_mask": sum(
            before < 48 and after >= 48
            for before, after in zip(baseline_w1_indices, w1_indices)
        ),
        "current": continuity_metrics(current_w1_tiles[0], current_w2_tiles),
        "preview": continuity_metrics(w1, preview_w2_tiles),
        "required_seam_window_audit": required_seam_windows,
        "internal_windows_vs_undeformed_reference": internal_reference_differences,
        "internal_frame_source": "single 96x96 render sliced only after completion",
        "w2_frame_roundtrip_exact_pixels": exact_pixels(
            w2_indices,
            roundtrip_w2_indices,
        ),
        "w2_frame_roundtrip_total_pixels": len(w2_indices),
        "deformation_audit": (
            deformation_metrics()
            if args.stage == "deformed"
            else {"applied": False}
        ),
        "full_w2_exact_repeat_pixels": exact_pixels(w2_indices, repeated_w1_indices),
        "full_w2_total_pixels": len(w2_indices),
        "mixed_layout_exact_repeat_pixels": exact_image_pixels(preview_mixed, preview_repeat),
        "mixed_layout_total_pixels": preview_mixed.width * preview_mixed.height,
        "mixed_mask_exact_repeat_pixels": exact_image_pixels(mask_mixed, mask_repeat),
        "mixed_mask_total_pixels": mask_mixed.width * mask_mixed.height,
        "author_tile_size": AUTHOR_TILE,
        "output_tile_size": OUTPUT_TILE,
        "nearest_neighbor_upscale": UPSCALE,
        "outer_match_band": OUTPUT_OUTER_MATCH_BAND,
        "w2_outer_band_exact_pixels": outer_band_exact(
            w1_indices,
            w2_indices,
            OUTPUT_VARIANT_PERIOD,
            OUTPUT_OUTER_MATCH_BAND,
        ),
        "w2_outer_band_total_pixels": outer_band_total(
            OUTPUT_VARIANT_PERIOD,
            OUTPUT_OUTER_MATCH_BAND,
        ),
        "palette_exact": True,
        "w1_frames": 1,
        "w2_frames": 4,
    }
    (out_dir / "metrics.json").write_text(json.dumps(metrics, indent=2), encoding="utf-8")

    print((out_dir / "continuity-review.png").resolve())
    print((out_dir / "connectivity-mask-review.png").resolve())
    print((out_dir / "junction-width-comparison.png").resolve())
    print((out_dir / "lava-brightness-comparison.png").resolve())
    print((out_dir / "seam-strip-audit.png").resolve())
    print((out_dir / "current-vs-preview.png").resolve())
    print(json.dumps(metrics, indent=2))
    return 0


def resolve(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def read_palette(path: Path) -> list[tuple[int, int, int]]:
    data = path.read_bytes()
    if len(data) != 768:
        raise ValueError(f"expected 768-byte palette, got {len(data)}")
    return [
        tuple(data[offset + channel] * 4 for channel in range(3))
        for offset in range(0, 768, 3)
    ]


def make_sites(period: int, count: int, seed: int) -> list[tuple[float, float, float]]:
    rng = Random(seed)
    minimum = period / math.sqrt(count) * 0.52
    sites: list[tuple[float, float, float]] = []
    for _ in range(count):
        for _attempt in range(500):
            candidate = (
                rng.uniform(0, period),
                rng.uniform(0, period),
                rng.uniform(-1.0, 1.0),
            )
            if all(
                math.hypot(
                    toroidal_delta(candidate[0], existing[0], period),
                    toroidal_delta(candidate[1], existing[1], period),
                ) >= minimum
                for existing in sites
            ):
                sites.append(candidate)
                break
        else:
            raise ValueError(f"could not place {count} separated sites in period {period}")
    return sites


def render_base(
    sites: list[tuple[float, float, float]],
    junction_widening: float,
    bright_cores: bool,
) -> list[int]:
    return [
        field_index(
            x,
            y,
            BASE_PERIOD,
            BASE_SEED,
            sites,
            junction_widening,
            bright_cores,
        )
        for y in range(BASE_PERIOD)
        for x in range(BASE_PERIOD)
    ]


def repeat_indices(indices: list[int], width: int) -> list[int]:
    if len(indices) != AUTHOR_TILE * AUTHOR_TILE or width % AUTHOR_TILE:
        raise ValueError("repeat source must be one 24x24 author tile and width a tile multiple")
    return [
        indices[(y % AUTHOR_TILE) * AUTHOR_TILE + (x % AUTHOR_TILE)]
        for y in range(width)
        for x in range(width)
    ]


def repeat_output_indices(indices: list[int], width: int) -> list[int]:
    if len(indices) != OUTPUT_TILE * OUTPUT_TILE or width % OUTPUT_TILE:
        raise ValueError("repeat source must be one 48x48 output tile and width a tile multiple")
    return [
        indices[(y % OUTPUT_TILE) * OUTPUT_TILE + (x % OUTPUT_TILE)]
        for y in range(width)
        for x in range(width)
    ]


def upscale_indices(indices: list[int], width: int, factor: int) -> list[int]:
    if len(indices) != width * width:
        raise ValueError("upscale source dimensions do not match its pixel count")
    return [
        indices[(y // factor) * width + (x // factor)]
        for y in range(width * factor)
        for x in range(width * factor)
    ]


def render_variant(
    base_sites: list[tuple[float, float, float]],
    base_indices: list[int],
    junction_widening: float,
    bright_cores: bool,
) -> list[int]:
    result = []
    for y in range(VARIANT_PERIOD):
        for x in range(VARIANT_PERIOD):
            border = min(x, y, VARIANT_PERIOD - 1 - x, VARIANT_PERIOD - 1 - y)
            if border < AUTHOR_OUTER_MATCH_BAND:
                result.append(
                    base_indices[(y % AUTHOR_TILE) * AUTHOR_TILE + (x % AUTHOR_TILE)]
                )
                continue

            weight = deformation_weight(float(x), float(y))
            dx, dy = deformation(x, y, weight)
            heat, crust, tone, junction_pool = field_values(
                (x + dx) % BASE_PERIOD,
                (y + dy) % BASE_PERIOD,
                BASE_PERIOD,
                BASE_SEED,
                base_sites,
                junction_widening,
            )
            broad = periodic_value_noise(
                x,
                y,
                VARIANT_PERIOD,
                16,
                VARIANT_SEED ^ 0x5A17,
            )
            crust = lerp(crust, broad, 0.22 * weight)
            result.append(values_index(heat, crust, tone, junction_pool, bright_cores))
    return result


def deformation(x: float, y: float, weight: float) -> tuple[float, float]:
    # One smooth 96x96 displacement field. It is exactly zero near the outer
    # boundary, so w1 cracks continue into w2 before gradually bending inside.
    dx = (
        2.5 * math.sin(math.tau * y / VARIANT_PERIOD + 0.35)
        + 1.2 * math.sin(math.tau * (x + y) / VARIANT_PERIOD)
    ) * weight
    dy = (
        2.1 * math.sin(math.tau * x / VARIANT_PERIOD + 1.1)
        - 1.05 * math.sin(math.tau * (x - y) / VARIANT_PERIOD)
    ) * weight
    return dx, dy


def deformation_weight(x: float, y: float) -> float:
    border = min(x, y, VARIANT_PERIOD - 1 - x, VARIANT_PERIOD - 1 - y)
    return smoothstep(
        AUTHOR_OUTER_MATCH_BAND,
        AUTHOR_OUTER_MATCH_BAND + 8,
        border,
    )


def displacement_at(x: float, y: float) -> tuple[float, float]:
    return deformation(x, y, deformation_weight(x, y))


def deformation_metrics() -> dict[str, float | int | bool]:
    determinants = []
    for y in range(1, VARIANT_PERIOD - 1):
        for x in range(1, VARIANT_PERIOD - 1):
            ux1, uy1 = displacement_at(x + 0.5, y)
            ux0, uy0 = displacement_at(x - 0.5, y)
            vx1, vy1 = displacement_at(x, y + 0.5)
            vx0, vy0 = displacement_at(x, y - 0.5)
            dux_dx = ux1 - ux0
            duy_dx = uy1 - uy0
            dux_dy = vx1 - vx0
            duy_dy = vy1 - vy0
            determinants.append(
                (1.0 + dux_dx) * (1.0 + duy_dy) - dux_dy * duy_dx
            )

    adjacent_steps = []
    for y in range(VARIANT_PERIOD):
        for x in range(VARIANT_PERIOD - 1):
            ax, ay = displacement_at(x, y)
            bx, by = displacement_at(x + 1, y)
            adjacent_steps.append(math.hypot(bx - ax, by - ay))
    for y in range(VARIANT_PERIOD - 1):
        for x in range(VARIANT_PERIOD):
            ax, ay = displacement_at(x, y)
            bx, by = displacement_at(x, y + 1)
            adjacent_steps.append(math.hypot(bx - ax, by - ay))

    vertical_internal_steps = []
    horizontal_internal_steps = []
    for coordinate in range(VARIANT_PERIOD):
        ax, ay = displacement_at(AUTHOR_TILE - 1, coordinate)
        bx, by = displacement_at(AUTHOR_TILE, coordinate)
        vertical_internal_steps.append(math.hypot(bx - ax, by - ay))
        ax, ay = displacement_at(coordinate, AUTHOR_TILE - 1)
        bx, by = displacement_at(coordinate, AUTHOR_TILE)
        horizontal_internal_steps.append(math.hypot(bx - ax, by - ay))

    collar_displacements = []
    for y in range(VARIANT_PERIOD):
        for x in range(VARIANT_PERIOD):
            border = min(x, y, VARIANT_PERIOD - 1 - x, VARIANT_PERIOD - 1 - y)
            if border < AUTHOR_OUTER_MATCH_BAND:
                dx, dy = displacement_at(x, y)
                collar_displacements.append(math.hypot(dx, dy))

    minimum = min(determinants)
    maximum_step = max(adjacent_steps)
    return {
        "applied": True,
        "minimum_jacobian_determinant": round(minimum, 6),
        "maximum_jacobian_determinant": round(max(determinants), 6),
        "nonpositive_jacobian_samples": sum(value <= 0.0 for value in determinants),
        "orientation_preserving": minimum > 0.0,
        "maximum_adjacent_displacement_step": round(maximum_step, 6),
        "vertical_internal_seam_displacement_step": round(max(vertical_internal_steps), 6),
        "horizontal_internal_seam_displacement_step": round(max(horizontal_internal_steps), 6),
        "internal_seam_has_no_reset": (
            max(vertical_internal_steps) <= maximum_step + 1e-9
            and max(horizontal_internal_steps) <= maximum_step + 1e-9
        ),
        "outer_collar_maximum_displacement": round(max(collar_displacements), 6),
        "outer_collar_is_identity": max(collar_displacements) == 0.0,
    }


def field_index(
    x: int,
    y: int,
    period: int,
    seed: int,
    sites: list[tuple[float, float, float]],
    junction_widening: float,
    bright_cores: bool,
) -> int:
    return values_index(
        *field_values(x, y, period, seed, sites, junction_widening),
        bright_cores,
    )


def field_values(
    x: int,
    y: int,
    period: int,
    seed: int,
    sites: list[tuple[float, float, float]],
    junction_widening: float,
) -> tuple[float, float, float, float]:
    qx = (
        x
        + 1.35 * math.sin(math.tau * y / period)
        + 0.6 * math.sin(math.tau * (x + y) / period)
    ) % period
    qy = (
        y
        + 1.1 * math.sin(math.tau * x / period + 0.7)
        - 0.55 * math.sin(math.tau * (x - y) / period)
    ) % period
    distances = []
    for index, (sx, sy, tone) in enumerate(sites):
        dx = toroidal_delta(qx, sx, period)
        dy = toroidal_delta(qy, sy, period)
        distances.append((math.hypot(dx, dy), index, tone))
    distances.sort(key=lambda item: item[0])
    first, second, third = distances[0], distances[1], distances[2]
    gap = second[0] - first[0]

    # Three Voronoi regions meet where the three nearest sites are almost
    # equidistant. Expand the fissure smoothly there while leaving its
    # centerline and every cell boundary unchanged.
    third_gap = third[0] - first[0]
    widening = max(0.0, junction_widening)
    junction = 1.0 - smoothstep(0.09, 1.60, third_gap)
    fissure_width = 0.775 + widening * junction * junction

    # The Voronoi boundary is the magma fissure. Keep a one-pixel hot core,
    # a thin orange shoulder, and a restrained dark-red heat halo.
    heat = 1.0 - smoothstep(0.05, fissure_width, gap)
    junction_pool = 0.0
    if widening > 0.0:
        # A direct, softly graded molten pocket makes the vertex expansion
        # survive 48x48 rasterization and palette quantization.  The pocket is
        # driven only by three-site proximity, so ordinary two-way fissures do
        # not become uniformly thicker.
        junction_pool = 1.0 - smoothstep(0.09, 0.09 + widening, third_gap)
        heat = max(heat, junction_pool)
    crust = (
        0.57 * periodic_value_noise(x, y, period, period // 4, seed ^ 0x13579)
        + 0.28 * periodic_value_noise(x, y, period, period // 6, seed ^ 0x2468A)
        + 0.15 * periodic_value_noise(x, y, period, max(2, period // 12), seed ^ 0xACE1)
    )
    center_shade = min(1.0, first[0] / max(1.0, period * 0.22))
    crust = clamp(0.72 * crust + 0.28 * center_shade, 0.0, 1.0)
    return heat, crust, first[2], junction_pool


def values_index(
    heat: float,
    crust: float,
    tone: float,
    junction_pool: float,
    bright_cores: bool,
) -> int:
    if bright_cores and heat >= 0.965:
        if junction_pool >= 0.80:
            return 96 + min(3, int(crust * 4))
        return 92 + min(3, int(crust * 4))
    if heat >= 0.93:
        return 87 + min(4, int(crust * 5))
    if heat >= 0.74:
        return 81 + min(6, int(crust * 7))
    if heat >= 0.43:
        return 72 + min(8, int(crust * 9))
    if heat >= 0.16:
        return 48 + min(12, int(crust * 13))

    # Broad, readable basalt plates: charcoal at the centers and subtly warm
    # near some facets. Avoid per-pixel speckles that obscure the cell shapes.
    bias = 2 if tone > 0.45 else -1 if tone < -0.45 else 0
    return clamp(11 + int(crust * 10) + bias, 10, 23)


def periodic_value_noise(
    x: float,
    y: float,
    period: int,
    cell: int,
    seed: int,
) -> float:
    cells = max(1, period // cell)
    fx, fy = x / cell, y / cell
    x0, y0 = math.floor(fx), math.floor(fy)
    tx, ty = smoother(fx - x0), smoother(fy - y0)
    a = lattice(x0, y0, cells, seed)
    b = lattice(x0 + 1, y0, cells, seed)
    c = lattice(x0, y0 + 1, cells, seed)
    d = lattice(x0 + 1, y0 + 1, cells, seed)
    return lerp(lerp(a, b, tx), lerp(c, d, tx), ty)


def lattice(x: int, y: int, cells: int, seed: int) -> float:
    x %= cells
    y %= cells
    value = (x * 0x1F123BB5) ^ (y * 0x5F356495) ^ seed
    value ^= value >> 15
    value = (value * 0x2C1B3C6D) & 0xFFFFFFFF
    value ^= value >> 12
    return (value & 0xFFFF) / 65535.0


def toroidal_delta(a: float, b: float, period: int) -> float:
    delta = abs(a - b)
    return min(delta, period - delta)


def smoother(value: float) -> float:
    return value * value * value * (value * (value * 6 - 15) + 10)


def smoothstep(low: float, high: float, value: float) -> float:
    if high <= low:
        return 1.0 if value >= high else 0.0
    t = clamp((value - low) / (high - low), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def clamp(value: int | float, low: int | float, high: int | float):
    return max(low, min(high, value))


def slice_frames(indices: list[int], width: int) -> list[bytes]:
    frames = []
    for cell_y in range(width // OUTPUT_TILE):
        for cell_x in range(width // OUTPUT_TILE):
            frame = bytearray()
            for y in range(OUTPUT_TILE):
                start = (
                    (cell_y * OUTPUT_TILE + y) * width
                    + cell_x * OUTPUT_TILE
                )
                frame.extend(indices[start : start + OUTPUT_TILE])
            frames.append(bytes(frame))
    return frames


def compose_frame_indices(frames: list[bytes], columns: int) -> list[int]:
    if not frames or len(frames) % columns:
        raise ValueError("frame count must be a nonzero multiple of the column count")
    rows = len(frames) // columns
    width = columns * OUTPUT_TILE
    result = [0] * (width * rows * OUTPUT_TILE)
    for index, frame in enumerate(frames):
        if len(frame) != OUTPUT_TILE * OUTPUT_TILE:
            raise ValueError(f"frame {index} is not 48x48")
        cell_x = index % columns
        cell_y = index // columns
        for y in range(OUTPUT_TILE):
            source = y * OUTPUT_TILE
            target = (
                (cell_y * OUTPUT_TILE + y) * width
                + cell_x * OUTPUT_TILE
            )
            result[target : target + OUTPUT_TILE] = frame[
                source : source + OUTPUT_TILE
            ]
    return result


def verify_vol(path: Path, expected_frames: int) -> None:
    width, height, frames = read_shptd(path)
    if (width, height) != (OUTPUT_TILE, OUTPUT_TILE) or len(frames) != expected_frames:
        raise ValueError(
            f"{path.name}: decoded {width}x{height}/{len(frames)} frames, expected 48x48/{expected_frames}"
        )


def indices_image(
    indices: list[int],
    width: int,
    height: int,
    palette: list[tuple[int, int, int]],
) -> Image.Image:
    image = Image.new("RGB", (width, height))
    image.putdata([palette[index] for index in indices])
    return image


def crack_mask_image(indices: list[int], width: int, height: int) -> Image.Image:
    image = Image.new("RGB", (width, height))
    image.putdata([
        (255, 255, 255) if index >= 48 else (0, 0, 0)
        for index in indices
    ])
    return image


def decode_composite(
    path: Path,
    columns: int,
    palette: list[tuple[int, int, int]],
) -> tuple[Image.Image, list[Image.Image]]:
    width, height, frames = read_shptd(path)
    tiles = []
    for frame in frames:
        tiles.append(indices_image(list(frame), width, height, palette))
    rows = (len(tiles) + columns - 1) // columns
    composite = Image.new("RGB", (columns * width, rows * height))
    for index, tile in enumerate(tiles):
        composite.paste(tile, ((index % columns) * width, (index // columns) * height))
    return composite, tiles


def split_tiles(image: Image.Image) -> list[Image.Image]:
    return [
        image.crop((x, y, x + OUTPUT_TILE, y + OUTPUT_TILE))
        for y in range(0, image.height, OUTPUT_TILE)
        for x in range(0, image.width, OUTPUT_TILE)
    ]


def build_repeat_layouts(w1: Image.Image, w2: Image.Image) -> tuple[Image.Image, Image.Image]:
    repeat = Image.new("RGB", (OUTPUT_TILE * 4, OUTPUT_TILE * 4))
    for y in range(0, repeat.height, OUTPUT_TILE):
        for x in range(0, repeat.width, OUTPUT_TILE):
            repeat.paste(w1, (x, y))
    mixed = repeat.copy()
    mixed.paste(w2, (OUTPUT_TILE, OUTPUT_TILE))
    return repeat, mixed


def continuity_metrics(w1: Image.Image, w2: list[Image.Image]) -> dict[str, float]:
    return {
        "w1_repeat_vertical": vertical_delta(w1, w1),
        "w1_repeat_horizontal": horizontal_delta(w1, w1),
        "w2_internal_vertical_top": vertical_delta(w2[0], w2[1]),
        "w2_internal_vertical_bottom": vertical_delta(w2[2], w2[3]),
        "w2_internal_horizontal_left": horizontal_delta(w2[0], w2[2]),
        "w2_internal_horizontal_right": horizontal_delta(w2[1], w2[3]),
        "w2_outer_left_top": vertical_delta(w1, w2[0]),
        "w2_outer_left_bottom": vertical_delta(w1, w2[2]),
        "w2_outer_right_top": vertical_delta(w2[1], w1),
        "w2_outer_right_bottom": vertical_delta(w2[3], w1),
        "w2_outer_top_left": horizontal_delta(w1, w2[0]),
        "w2_outer_top_right": horizontal_delta(w1, w2[1]),
        "w2_outer_bottom_left": horizontal_delta(w2[2], w1),
        "w2_outer_bottom_right": horizontal_delta(w2[3], w1),
    }


def vertical_delta(left: Image.Image, right: Image.Image) -> float:
    return sum(
        abs(
            luminance(left.getpixel((OUTPUT_TILE - 1, y)))
            - luminance(right.getpixel((0, y)))
        )
        for y in range(OUTPUT_TILE)
    ) / OUTPUT_TILE


def horizontal_delta(top: Image.Image, bottom: Image.Image) -> float:
    return sum(
        abs(
            luminance(top.getpixel((x, OUTPUT_TILE - 1)))
            - luminance(bottom.getpixel((x, 0)))
        )
        for x in range(OUTPUT_TILE)
    ) / OUTPUT_TILE


def luminance(color: tuple[int, int, int]) -> float:
    return 0.299 * color[0] + 0.587 * color[1] + 0.114 * color[2]


def outer_band_exact(
    shared: list[int],
    candidate: list[int],
    width: int,
    band: int,
) -> int:
    exact = 0
    for y in range(width):
        for x in range(width):
            border = min(x, y, width - 1 - x, width - 1 - y)
            if border >= band:
                continue
            expected = shared[
                (y % OUTPUT_TILE) * OUTPUT_TILE + (x % OUTPUT_TILE)
            ]
            exact += int(candidate[y * width + x] == expected)
    return exact


def outer_band_total(width: int, band: int) -> int:
    inner = width - 2 * band
    return width * width - inner * inner


def join_strip(
    first: Image.Image,
    second: Image.Image,
    orientation: str,
    band: int = OUTPUT_OUTER_MATCH_BAND,
) -> Image.Image:
    if orientation == "vertical":
        strip = Image.new("RGB", (band * 2, OUTPUT_TILE))
        strip.paste(
            first.crop(
                (OUTPUT_TILE - band, 0, OUTPUT_TILE, OUTPUT_TILE)
            ),
            (0, 0),
        )
        strip.paste(second.crop((0, 0, band, OUTPUT_TILE)), (band, 0))
        return strip
    if orientation == "horizontal":
        strip = Image.new("RGB", (OUTPUT_TILE, band * 2))
        strip.paste(
            first.crop(
                (0, OUTPUT_TILE - band, OUTPUT_TILE, OUTPUT_TILE)
            ),
            (0, 0),
        )
        strip.paste(second.crop((0, 0, OUTPUT_TILE, band)), (0, band))
        return strip
    raise ValueError(f"unknown seam orientation: {orientation}")


def seam_pairs(
    w1: Image.Image,
    w2: list[Image.Image],
) -> list[tuple[str, str, Image.Image, Image.Image]]:
    tl, tr, bl, br = w2
    return [
        ("V canonical w1 | w1", "vertical", w1, w1),
        ("V internal TL | TR", "vertical", tl, tr),
        ("V internal BL | BR", "vertical", bl, br),
        ("V outer w1 | TL", "vertical", w1, tl),
        ("V outer w1 | BL", "vertical", w1, bl),
        ("V outer TR | w1", "vertical", tr, w1),
        ("V outer BR | w1", "vertical", br, w1),
        ("H canonical w1 / w1", "horizontal", w1, w1),
        ("H internal TL / BL", "horizontal", tl, bl),
        ("H internal TR / BR", "horizontal", tr, br),
        ("H outer w1 / TL", "horizontal", w1, tl),
        ("H outer w1 / TR", "horizontal", w1, tr),
        ("H outer BL / w1", "horizontal", bl, w1),
        ("H outer BR / w1", "horizontal", br, w1),
    ]


def seam_strip_panels(
    w1: Image.Image,
    w2: list[Image.Image],
) -> list[tuple[str, Image.Image]]:
    return [
        (label, join_strip(first, second, orientation))
        for label, orientation, first, second in seam_pairs(w1, w2)
    ]


def seam_window_metrics(
    w1: Image.Image,
    w2: list[Image.Image],
) -> dict[str, dict[str, int]]:
    canonical = {
        "vertical": join_strip(w1, w1, "vertical"),
        "horizontal": join_strip(w1, w1, "horizontal"),
    }
    metrics = {}
    for label, orientation, first, second in seam_pairs(w1, w2):
        candidate = join_strip(first, second, orientation)
        total = candidate.width * candidate.height
        exact = exact_image_pixels(candidate, canonical[orientation])
        metrics[label] = {
            "exact_pixels": exact,
            "total_pixels": total,
            "mismatches": total - exact,
        }
    return metrics


def exact_pixels(first: list[int], second: list[int]) -> int:
    if len(first) != len(second):
        raise ValueError("pixel arrays differ in length")
    return sum(a == b for a, b in zip(first, second))


def exact_image_pixels(first: Image.Image, second: Image.Image) -> int:
    if first.size != second.size:
        raise ValueError("images differ in size")
    return sum(a == b for a, b in zip(first.getdata(), second.getdata()))


def write_seam_strip_review(
    path: Path,
    panels: list[tuple[str, Image.Image]],
) -> None:
    scale = 5
    columns = 2
    header = 24
    cell_width = OUTPUT_TILE * scale
    cell_height = OUTPUT_TILE * scale
    rows = (len(panels) + columns - 1) // columns
    sheet = Image.new(
        "RGB",
        (columns * cell_width, rows * (cell_height + header)),
        (73, 86, 99),
    )
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for index, (label, strip) in enumerate(panels):
        cell_x = (index % columns) * cell_width
        cell_y = (index // columns) * (cell_height + header)
        draw.text((cell_x + 5, cell_y + 6), label, fill="white", font=font)
        resized = strip.resize(
            (strip.width * scale, strip.height * scale),
            Image.Resampling.NEAREST,
        )
        paste_x = cell_x + (cell_width - resized.width) // 2
        paste_y = cell_y + header + (cell_height - resized.height) // 2
        sheet.paste(resized, (paste_x, paste_y))
        if "V " == label[:2]:
            seam_x = paste_x + OUTPUT_OUTER_MATCH_BAND * scale
            draw.line((seam_x, paste_y, seam_x, paste_y + resized.height - 1), fill=(0, 255, 255))
        else:
            seam_y = paste_y + OUTPUT_OUTER_MATCH_BAND * scale
            draw.line((paste_x, seam_y, paste_x + resized.width - 1, seam_y), fill=(0, 255, 255))
    sheet.save(path)


def write_continuity_review(
    path: Path,
    panels: list[tuple[str, Image.Image]],
    columns: int = 2,
) -> None:
    scale = 4
    header = 28
    panel_width = OUTPUT_TILE * 4 * scale
    panel_height = OUTPUT_TILE * 4 * scale
    rows = (len(panels) + columns - 1) // columns
    sheet = Image.new(
        "RGB",
        (columns * panel_width, rows * (panel_height + header)),
        (73, 86, 99),
    )
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for index, (label, image) in enumerate(panels):
        x = (index % columns) * panel_width
        y = (index // columns) * (panel_height + header)
        draw.text((x + 6, y + 7), label, fill="white", font=font)
        sheet.paste(
            image.resize((panel_width, panel_height), Image.Resampling.NEAREST),
            (x, y + header),
        )
    sheet.save(path)


if __name__ == "__main__":
    raise SystemExit(main())
