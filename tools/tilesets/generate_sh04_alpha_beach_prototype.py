#!/usr/bin/env python
"""Generate a preview-only true-alpha basalt beach prototype for sh04."""

from __future__ import annotations

import argparse
import json
import math
import re
from dataclasses import dataclass
from heapq import heappop, heappush
from pathlib import Path
from random import Random

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

from shptd import read_shptd


ROOT = Path(__file__).resolve().parents[2]
TILE = 48
SIZE = TILE * 3
FEATHER = 2.0
THERMAL_DEPTH = 8.0
GLOW_DEPTH = 6.0
CONTOUR_JITTER = 2.0
DEFAULT_OUT_DIR = Path.home() / "Documents/agents/volcanic-theater/shorelines/workbench"
BACKGROUND = (73, 86, 99)


@dataclass(frozen=True)
class TemplateSpec:
    image: str
    columns: int
    rows: int
    terrain: dict[int, str]


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    parser.add_argument("--template", choices=("sh01", "sh04"), default="sh04")
    args = parser.parse_args()
    out_dir = resolve(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    if args.template == "sh01":
        return generate_sh01(out_dir)

    temperate_bits = ROOT / "mods/cameo/bits/temp"
    volcanic_bits = ROOT / "mods/cameo/bits/volcanic"
    temperate_palette = read_palette(ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal")
    volcanic_palette = read_palette(volcanic_bits / "volcanic.pal")

    donor = read_composite(temperate_bits / "sh04.tem", 3)
    donor_rgb = indices_rgb(donor, temperate_palette)
    ground_indices = source_indices(temperate_bits / "clear1.tem")
    water_indices = source_indices(temperate_bits / "w1.tem") | source_indices(temperate_bits / "w2.tem")

    seed_mask, selected_indices = beach_color_seed(
        donor,
        temperate_palette,
        ground_indices,
        water_indices,
    )
    largest_mask, component_metrics = largest_beach_region(seed_mask)
    alpha = feather_alpha(largest_mask, FEATHER)

    clear1 = unique_frame(volcanic_bits / "clear1.vol", expected_frames=16)
    w1 = unique_frame(volcanic_bits / "w1.vol", expected_frames=1)
    ground_rgb = indices_rgb(np.tile(clear1, (3, 3)), volcanic_palette)
    lava_indices = np.tile(w1, (3, 3))
    lava_rgb = indices_rgb(lava_indices, volcanic_palette)
    ground_region, lava_region, lava_selector, edge_roles = phase_regions(largest_mask)
    base_rgb = np.where(lava_selector[:, :, None], lava_rgb, ground_rgb)

    thermal_rgb, thermal_field_rgb, crack_heat_rgb, thermal_metrics = thermal_shore_texture(
        lava_indices,
        lava_rgb,
        ground_rgb,
        ground_region,
        lava_region,
        largest_mask,
        seed=0x7E4D1A,
    )
    overlay_rgb = base_rgb.copy()
    overlay_rgb[largest_mask] = thermal_rgb[largest_mask]
    overlay_rgba = np.dstack((overlay_rgb, alpha)).astype(np.uint8)
    composite_rgb = linear_alpha_composite(base_rgb, overlay_rgb, alpha)

    # Recreate the immediately preceding prototype in memory so the stable
    # workbench can show a before/after without retaining another versioned
    # output directory.
    baseline_alpha = feather_alpha(largest_mask, 4.0)
    baseline_basalt = basalt_crack_texture(SIZE, SIZE, seed=0xBA5A17)
    baseline_rgb = linear_alpha_composite(base_rgb, baseline_basalt, baseline_alpha)

    if not np.array_equal(composite_rgb[0, :], base_rgb[0, :]):
        raise ValueError("thermal transition altered the top ground edge")
    if not np.array_equal(composite_rgb[-1, :], base_rgb[-1, :]):
        raise ValueError("thermal transition altered the bottom lava edge")

    donor_image = Image.fromarray(donor_rgb, mode="RGB")
    seed_image = mask_image(seed_mask, (224, 151, 73))
    region_image = mask_image(largest_mask, (214, 119, 54))
    alpha_image = Image.fromarray(alpha, mode="L")
    overlay_image = Image.fromarray(overlay_rgba, mode="RGBA")
    checker_image = checker_composite(overlay_rgba)
    base_image = Image.fromarray(base_rgb, mode="RGB")
    composite_image = Image.fromarray(composite_rgb, mode="RGB")
    thermal_material_image = Image.fromarray(thermal_rgb, mode="RGB")
    thermal_field_image = Image.fromarray(thermal_field_rgb, mode="RGB")
    crack_heat_image = Image.fromarray(crack_heat_rgb, mode="RGB")
    baseline_image = Image.fromarray(baseline_rgb, mode="RGB")
    edge_role_image = edge_role_preview(
        ground_region,
        lava_region,
        largest_mask,
        edge_roles,
    )

    donor_image.save(out_dir / "sh04-temperate-donor.png")
    seed_image.save(out_dir / "sh04-beach-color-seed-mask.png")
    region_image.save(out_dir / "sh04-beach-largest-region.png")
    alpha_image.save(out_dir / "sh04-beach-alpha-feather.png")
    overlay_image.save(out_dir / "sh04-basalt-overlay-rgba.png")
    checker_image.save(out_dir / "sh04-basalt-overlay-checker.png")
    base_image.save(out_dir / "sh04-ground-lava-underlay.png")
    composite_image.save(out_dir / "sh04-alpha-composite.png")
    thermal_material_image.save(out_dir / "sh04-thermal-material.png")
    thermal_field_image.save(out_dir / "sh04-thermal-field.png")
    crack_heat_image.save(out_dir / "sh04-crack-heat-map.png")
    edge_role_image.save(out_dir / "sh04-edge-role-map.png")

    write_review_sheet(
        out_dir / "sh04-transition-comparison.png",
        [
            ("Before: independent cold bank + 4px feather", baseline_image),
            ("After: continuous thermal crack field + 2px feather", composite_image),
        ],
        columns=2,
    )

    write_review_sheet(
        out_dir / "sh04-alpha-beach-review.png",
        [
            ("Temperate donor", donor_image),
            ("Largest connected region", region_image),
            ("Edge roles and phase expansion", edge_role_image),
            ("Thermal zones and crack heat", thermal_field_image),
            ("True 2px alpha feather", alpha_image.convert("RGB")),
            ("Expanded ground/lava underlay", base_image),
            ("Continuous crack material on checker", checker_image),
            ("Thermal RGBA overlay composited", composite_image),
        ],
        columns=4,
    )

    metrics = {
        "template": "sh04",
        "preview_only": True,
        "palette_conversion_deferred": True,
        "alpha_feather_pixels": FEATHER,
        "selected_temperate_palette_indices": sorted(selected_indices),
        "seed_mask_pixels": int(np.count_nonzero(seed_mask)),
        "largest_region_pixels": int(np.count_nonzero(largest_mask)),
        "largest_region_fraction": round(float(np.mean(largest_mask)), 6),
        "alpha_nonzero_pixels": int(np.count_nonzero(alpha)),
        "alpha_fully_opaque_pixels": int(np.count_nonzero(alpha == 255)),
        "edge_roles": edge_roles,
        "expanded_ground_pixels": int(np.count_nonzero(ground_region)),
        "expanded_lava_pixels": int(np.count_nonzero(lava_region)),
        "under_mask_lava_pixels": int(np.count_nonzero(lava_selector & largest_mask)),
        **thermal_metrics,
        **component_metrics,
    }
    (out_dir / "metrics.json").write_text(json.dumps(metrics, indent=2), encoding="utf-8")

    print((out_dir / "sh04-alpha-beach-review.png").resolve())
    print((out_dir / "sh04-alpha-composite.png").resolve())
    print((out_dir / "sh04-basalt-overlay-rgba.png").resolve())
    print(json.dumps(metrics, indent=2))
    return 0


def generate_sh01(out_dir: Path) -> int:
    """Generate sh01 using occupied-subtile semantics instead of box edges."""

    temperate_bits = ROOT / "mods/cameo/bits/temp"
    volcanic_bits = ROOT / "mods/cameo/bits/volcanic"
    temperate_palette = read_palette(ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal")
    volcanic_palette = read_palette(volcanic_bits / "volcanic.pal")
    temperate_spec = read_template_spec(
        ROOT / "mods/cameo/tilesets/ra_temperat.yaml",
        "sh01.tem",
    )
    volcanic_spec = read_template_spec(
        ROOT / "mods/cameo/tilesets/volcanic.yaml",
        "sh01.vol",
    )
    if (
        temperate_spec.columns,
        temperate_spec.rows,
        temperate_spec.terrain,
    ) != (
        volcanic_spec.columns,
        volcanic_spec.rows,
        volcanic_spec.terrain,
    ):
        raise ValueError("Temperate and Volcanic sh01 template topology differs")

    donor, domain = read_sparse_composite(temperate_bits / temperate_spec.image, temperate_spec)
    donor_rgb = indices_rgb(donor, temperate_palette)
    ground_indices = source_indices(temperate_bits / "clear1.tem")
    water_indices = source_indices(temperate_bits / "w1.tem") | source_indices(temperate_bits / "w2.tem")
    raw_ground_seed = domain & np.isin(donor, list(ground_indices))
    raw_lava_seed = domain & np.isin(donor, list(water_indices))

    seed_mask, selected_indices = beach_color_seed(
        donor,
        temperate_palette,
        ground_indices,
        water_indices,
    )
    seed_mask &= domain
    largest_mask, component_metrics = largest_semantic_beach_region(
        seed_mask,
        domain,
        raw_ground_seed,
        raw_lava_seed,
        temperate_spec,
    )
    alpha = feather_alpha(largest_mask, FEATHER)
    alpha[~domain] = 0

    ground_edge_seed, lava_edge_seed, phase_source_metrics = exposed_phase_edge_seeds(
        donor,
        largest_mask,
        domain,
        ground_indices,
        water_indices,
        temperate_spec,
        minimum_run=4,
    )
    ground_region, lava_region, lava_selector, phase_metrics = phase_regions_from_subtile_edge_seeds(
        largest_mask,
        domain,
        ground_edge_seed,
        lava_edge_seed,
        raw_ground_seed,
        raw_lava_seed,
    )
    height, width = donor.shape
    clear1 = unique_frame(volcanic_bits / "clear1.vol", expected_frames=16)
    w1 = unique_frame(volcanic_bits / "w1.vol", expected_frames=1)
    ground_indices_image = tile_frame(clear1, width, height)
    lava_indices = tile_frame(w1, width, height)
    ground_rgb = indices_rgb(ground_indices_image, volcanic_palette)
    lava_rgb = indices_rgb(lava_indices, volcanic_palette)
    base_rgb = np.where(lava_selector[:, :, None], lava_rgb, ground_rgb)
    base_rgb[~domain] = BACKGROUND

    thermal_rgb, thermal_field_rgb, crack_heat_rgb, thermal_metrics = thermal_shore_texture(
        lava_indices,
        lava_rgb,
        ground_rgb,
        ground_region,
        lava_region,
        largest_mask,
        seed=0x5017E,
        domain=domain,
    )
    thermal_rgb[~domain] = BACKGROUND
    thermal_field_rgb[~domain] = BACKGROUND
    crack_heat_rgb[~domain] = BACKGROUND
    overlay_rgb = base_rgb.copy()
    overlay_rgb[largest_mask] = thermal_rgb[largest_mask]
    overlay_rgba = np.dstack((overlay_rgb, alpha)).astype(np.uint8)
    composite_rgb = linear_alpha_composite(base_rgb, overlay_rgb, alpha)
    composite_rgb[~domain] = BACKGROUND
    if np.any(alpha[~domain]):
        raise ValueError("alpha leaked into blank sh01 subtiles")
    unmasked_phase = domain & ~largest_mask
    if not np.array_equal(composite_rgb[unmasked_phase], base_rgb[unmasked_phase]):
        raise ValueError("thermal overlay altered unmasked sh01 phase pixels")

    deep_thermal_rgb, deep_field_rgb, deep_crack_heat_rgb, deep_metrics = thermal_shore_texture(
        lava_indices,
        lava_rgb,
        ground_rgb,
        ground_region,
        lava_region,
        largest_mask,
        seed=0x5017E,
        domain=domain,
        glow_depth=16.0,
        core_intensity=0.35,
        shoulder_intensity=0.15,
        plate_intensity=0.06,
        contact_pixels=1.0,
    )
    deep_thermal_rgb[~domain] = BACKGROUND
    deep_field_rgb[~domain] = BACKGROUND
    deep_crack_heat_rgb[~domain] = BACKGROUND
    deep_overlay_rgb = base_rgb.copy()
    deep_overlay_rgb[largest_mask] = deep_thermal_rgb[largest_mask]
    deep_composite_rgb = linear_alpha_composite(base_rgb, deep_overlay_rgb, alpha)
    deep_composite_rgb[~domain] = BACKGROUND
    if not np.array_equal(deep_composite_rgb[unmasked_phase], base_rgb[unmasked_phase]):
        raise ValueError("deep seepage comparison altered unmasked sh01 phase pixels")

    phase_image = semantic_phase_preview(ground_region, lava_region, largest_mask, domain)
    edge_image, edge_signatures, seam_metrics = subtile_edge_preview(
        ground_region,
        lava_region,
        largest_mask,
        domain,
        temperate_spec,
    )
    occupancy_image = template_occupancy_preview(donor_rgb, domain, temperate_spec)
    phase_source_image = phase_source_preview(
        donor_rgb,
        domain,
        ground_edge_seed,
        lava_edge_seed,
        temperate_spec,
    )
    donor_display = donor_rgb.copy()
    donor_display[~domain] = BACKGROUND
    donor_image = Image.fromarray(donor_display, mode="RGB")
    seed_image = mask_image(seed_mask, (224, 151, 73), domain)
    region_image = mask_image(largest_mask, (214, 119, 54), domain)
    alpha_image = Image.fromarray(alpha, mode="L")
    overlay_image = Image.fromarray(overlay_rgba, mode="RGBA")
    checker_image = checker_composite(overlay_rgba)
    base_image = Image.fromarray(base_rgb, mode="RGB")
    composite_image = Image.fromarray(composite_rgb, mode="RGB")
    thermal_material_image = Image.fromarray(thermal_rgb, mode="RGB")
    thermal_field_image = Image.fromarray(thermal_field_rgb, mode="RGB")
    crack_heat_image = Image.fromarray(crack_heat_rgb, mode="RGB")
    deep_composite_image = Image.fromarray(deep_composite_rgb, mode="RGB")
    deep_field_image = Image.fromarray(deep_field_rgb, mode="RGB")
    deep_crack_heat_image = Image.fromarray(deep_crack_heat_rgb, mode="RGB")

    donor_image.save(out_dir / "sh01-temperate-donor.png")
    occupancy_image.save(out_dir / "sh01-subtile-occupancy.png")
    phase_source_image.save(out_dir / "sh01-phase-source-edges.png")
    seed_image.save(out_dir / "sh01-beach-color-seed-mask.png")
    region_image.save(out_dir / "sh01-beach-largest-region.png")
    alpha_image.save(out_dir / "sh01-beach-alpha-feather.png")
    overlay_image.save(out_dir / "sh01-basalt-overlay-rgba.png")
    checker_image.save(out_dir / "sh01-basalt-overlay-checker.png")
    base_image.save(out_dir / "sh01-ground-lava-underlay.png")
    composite_image.save(out_dir / "sh01-alpha-composite.png")
    thermal_material_image.save(out_dir / "sh01-thermal-material.png")
    thermal_field_image.save(out_dir / "sh01-thermal-field.png")
    crack_heat_image.save(out_dir / "sh01-crack-heat-map.png")
    deep_composite_image.save(out_dir / "sh01-seepage-deep-dim.png")
    deep_field_image.save(out_dir / "sh01-seepage-deep-dim-thermal-field.png")
    deep_crack_heat_image.save(out_dir / "sh01-seepage-deep-dim-crack-heat.png")
    phase_image.save(out_dir / "sh01-phase-connectivity.png")
    edge_image.save(out_dir / "sh01-subtile-edge-map.png")

    write_review_sheet(
        out_dir / "sh01-transition-comparison.png",
        [
            ("RA Temperate donor", donor_image),
            ("Volcanic thermal shoreline", composite_image),
        ],
        columns=2,
        scale=2,
    )

    write_review_sheet(
        out_dir / "sh01-seepage-comparison.png",
        [
            ("Current: 6px full-strength beach heat", composite_image),
            ("Proposed: 16px deep, 35% core / 15% shoulder", deep_composite_image),
            ("Current crack-heat reach", crack_heat_image),
            ("Proposed deeper crack-heat reach", deep_crack_heat_image),
        ],
        columns=2,
        scale=2,
    )

    mask_y, mask_x = np.where(largest_mask)
    crop_box = (
        max(0, int(mask_x.min()) - 8),
        max(0, int(mask_y.min()) - 8),
        min(width, int(mask_x.max()) + 9),
        min(height, int(mask_y.max()) + 9),
    )
    write_review_sheet(
        out_dir / "sh01-seepage-closeup-comparison.png",
        [
            ("Current beach heat close-up", composite_image.crop(crop_box)),
            ("16px deep / dim seepage close-up", deep_composite_image.crop(crop_box)),
        ],
        columns=2,
        scale=4,
    )

    write_review_sheet(
        out_dir / "sh01-alpha-beach-review.png",
        [
            ("Temperate donor", donor_image),
            ("Occupied subtiles and terrain", occupancy_image),
            ("Coherent exposed phase sources", phase_source_image),
            ("Selected connected beach", region_image),
            ("Ground/beach/lava components", phase_image),
            ("Per-subtile edge runs", edge_image),
            ("Thermal zones and crack heat", thermal_field_image),
            ("True 2px alpha feather", alpha_image.convert("RGB")),
            ("Semantic ground/lava underlay", base_image),
            ("Connected crack heat", crack_heat_image),
            ("Thermal material on checker", checker_image),
            ("Thermal RGBA overlay composited", composite_image),
        ],
        columns=4,
        scale=2,
    )

    metrics = {
        "template": "sh01",
        "preview_only": True,
        "palette_conversion_deferred": True,
        "template_size_subtiles": [temperate_spec.columns, temperate_spec.rows],
        "occupied_subtiles": sorted(temperate_spec.terrain),
        "subtile_terrain": temperate_spec.terrain,
        "alpha_feather_pixels": FEATHER,
        "selected_temperate_palette_indices": sorted(selected_indices),
        "domain_pixels": int(np.count_nonzero(domain)),
        "seed_mask_pixels": int(np.count_nonzero(seed_mask)),
        "largest_region_pixels": int(np.count_nonzero(largest_mask)),
        "alpha_nonzero_pixels": int(np.count_nonzero(alpha)),
        "alpha_fully_opaque_pixels": int(np.count_nonzero(alpha == 255)),
        "expanded_ground_pixels": int(np.count_nonzero(ground_region)),
        "expanded_lava_pixels": int(np.count_nonzero(lava_region)),
        "under_mask_lava_pixels": int(np.count_nonzero(lava_selector & largest_mask)),
        "blank_subtile_alpha_pixels": int(np.count_nonzero(alpha[~domain])),
        "unmasked_phase_pixels_preserved": True,
        "subtile_edge_signatures": edge_signatures,
        **phase_source_metrics,
        **phase_metrics,
        **seam_metrics,
        **thermal_metrics,
        **component_metrics,
    }
    (out_dir / "sh01-metrics.json").write_text(json.dumps(metrics, indent=2), encoding="utf-8")
    seepage_metrics = {
        "template": "sh01",
        "preview_only": True,
        "accepted_baseline_unchanged": True,
        "current": {
            "glow_fade_pixels": thermal_metrics["glow_fade_pixels"],
            "core_intensity": 1.0,
            "shoulder_intensity": 0.78,
            "plate_intensity": 0.22,
        },
        "deep_dim_proposal": deep_metrics,
    }
    (out_dir / "sh01-seepage-metrics.json").write_text(
        json.dumps(seepage_metrics, indent=2),
        encoding="utf-8",
    )

    print((out_dir / "sh01-alpha-beach-review.png").resolve())
    print((out_dir / "sh01-alpha-composite.png").resolve())
    print((out_dir / "sh01-subtile-edge-map.png").resolve())
    print(json.dumps(metrics, indent=2))
    return 0


def resolve(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def read_palette(path: Path) -> list[tuple[int, int, int]]:
    data = path.read_bytes()
    if len(data) != 768:
        raise ValueError(f"{path}: expected 768-byte palette")
    return [
        tuple(data[offset + channel] * 4 for channel in range(3))
        for offset in range(0, 768, 3)
    ]


def read_composite(path: Path, columns: int) -> np.ndarray:
    width, height, frames = read_shptd(path)
    if (width, height, len(frames)) != (TILE, TILE, columns * columns):
        raise ValueError(f"{path.name}: unexpected geometry")
    result = np.zeros((columns * TILE, columns * TILE), dtype=np.uint8)
    for index, frame in enumerate(frames):
        tile = np.frombuffer(frame, dtype=np.uint8).reshape((TILE, TILE))
        cell_x = index % columns
        cell_y = index // columns
        result[
            cell_y * TILE : (cell_y + 1) * TILE,
            cell_x * TILE : (cell_x + 1) * TILE,
        ] = tile
    return result


def read_template_spec(path: Path, image: str) -> TemplateSpec:
    lines = path.read_text(encoding="utf-8").splitlines()
    target = None
    for index, line in enumerate(lines):
        stripped = line.strip()
        if not stripped.startswith("Images:"):
            continue
        images = [value.strip() for value in stripped.split(":", 1)[1].split(",")]
        if image in images:
            target = index
            break
    if target is None:
        raise ValueError(f"{path}: no template uses {image}")

    start = next(
        index
        for index in range(target, -1, -1)
        if lines[index].strip().startswith("Template@")
    )
    end = len(lines)
    for index in range(start + 1, len(lines)):
        if lines[index].strip().startswith("Template@"):
            end = index
            break
    block = lines[start:end]

    columns = rows = None
    terrain: dict[int, str] = {}
    in_tiles = False
    for line in block:
        stripped = line.strip()
        if stripped.startswith("Size:"):
            values = stripped.split(":", 1)[1].split(",")
            columns, rows = (int(value.strip()) for value in values)
        elif stripped == "Tiles:":
            in_tiles = True
        elif in_tiles:
            match = re.fullmatch(r"(\d+):\s*(\S+)", stripped)
            if match:
                terrain[int(match.group(1))] = match.group(2)

    if columns is None or rows is None or not terrain:
        raise ValueError(f"{path}: incomplete template metadata for {image}")
    if any(index < 0 or index >= columns * rows for index in terrain):
        raise ValueError(f"{path}: occupied tile lies outside {columns}x{rows}")
    return TemplateSpec(image=image, columns=columns, rows=rows, terrain=terrain)


def read_sparse_composite(path: Path, spec: TemplateSpec) -> tuple[np.ndarray, np.ndarray]:
    width, height, frames = read_shptd(path)
    if (width, height, len(frames)) != (TILE, TILE, spec.columns * spec.rows):
        raise ValueError(f"{path.name}: unexpected geometry for {spec.columns}x{spec.rows}")
    result = np.zeros((spec.rows * TILE, spec.columns * TILE), dtype=np.uint8)
    domain = np.zeros_like(result, dtype=bool)
    for index, frame in enumerate(frames):
        tile = np.frombuffer(frame, dtype=np.uint8).reshape((TILE, TILE))
        cell_x = index % spec.columns
        cell_y = index // spec.columns
        ys = slice(cell_y * TILE, (cell_y + 1) * TILE)
        xs = slice(cell_x * TILE, (cell_x + 1) * TILE)
        result[ys, xs] = tile
        if index in spec.terrain:
            if not np.any(tile):
                raise ValueError(f"{path.name}: occupied frame {index} is blank")
            domain[ys, xs] = True
        elif np.any(tile):
            raise ValueError(f"{path.name}: gap frame {index} is not blank")
    return result, domain


def tile_frame(frame: np.ndarray, width: int, height: int) -> np.ndarray:
    rows = math.ceil(height / frame.shape[0])
    columns = math.ceil(width / frame.shape[1])
    return np.tile(frame, (rows, columns))[:height, :width].copy()


def domain_preserving_closing(mask: np.ndarray, domain: np.ndarray, radius: int) -> np.ndarray:
    # Preserve only the outer raster boundary during morphology, then clip to
    # occupied subtiles.  Internal gap frames remain empty barriers instead of
    # becoming an artificial beach rim around the polyomino.
    return edge_preserving_closing(mask & domain, radius) & domain


def exposed_subtile_edge_mask(spec: TemplateSpec, shape: tuple[int, int]) -> np.ndarray:
    result = np.zeros(shape, dtype=bool)
    occupied = set(spec.terrain)
    for index in occupied:
        cell_y, cell_x = divmod(index, spec.columns)
        y0, x0 = cell_y * TILE, cell_x * TILE
        neighbors = {
            "top": index - spec.columns if cell_y > 0 else None,
            "right": index + 1 if cell_x + 1 < spec.columns else None,
            "bottom": index + spec.columns if cell_y + 1 < spec.rows else None,
            "left": index - 1 if cell_x > 0 else None,
        }
        if neighbors["top"] not in occupied:
            result[y0, x0 : x0 + TILE] = True
        if neighbors["right"] not in occupied:
            result[y0 : y0 + TILE, x0 + TILE - 1] = True
        if neighbors["bottom"] not in occupied:
            result[y0 + TILE - 1, x0 : x0 + TILE] = True
        if neighbors["left"] not in occupied:
            result[y0 : y0 + TILE, x0] = True
    return result


def component_subtiles(mask: np.ndarray, spec: TemplateSpec) -> list[int]:
    result = []
    for index in sorted(spec.terrain):
        cell_y, cell_x = divmod(index, spec.columns)
        tile = mask[
            cell_y * TILE : (cell_y + 1) * TILE,
            cell_x * TILE : (cell_x + 1) * TILE,
        ]
        if np.any(tile):
            result.append(index)
    return result


def crossed_internal_subtile_seams(mask: np.ndarray, spec: TemplateSpec) -> list[str]:
    occupied = set(spec.terrain)
    result = []
    for index in sorted(occupied):
        cell_y, cell_x = divmod(index, spec.columns)
        y0, x0 = cell_y * TILE, cell_x * TILE
        if cell_x + 1 < spec.columns and index + 1 in occupied:
            if np.any(mask[y0 : y0 + TILE, x0 + TILE - 1] & mask[y0 : y0 + TILE, x0 + TILE]):
                result.append(f"{index}-right-{index + 1}")
        if cell_y + 1 < spec.rows and index + spec.columns in occupied:
            if np.any(mask[y0 + TILE - 1, x0 : x0 + TILE] & mask[y0 + TILE, x0 : x0 + TILE]):
                result.append(f"{index}-bottom-{index + spec.columns}")
    return result


def largest_semantic_beach_region(
    seed: np.ndarray,
    domain: np.ndarray,
    ground_seed: np.ndarray,
    lava_seed: np.ndarray,
    spec: TemplateSpec,
) -> tuple[np.ndarray, dict[str, object]]:
    exposed = exposed_subtile_edge_mask(spec, seed.shape)
    exposed_seed = seed & exposed
    exposed_count = int(np.count_nonzero(exposed_seed))
    for tolerance in range(2, 11):
        closed = domain_preserving_closing(seed, domain, tolerance)
        labels, count = ndimage.label(closed)
        sizes = np.bincount(labels.reshape(-1))
        candidates = []
        for label_value in range(1, len(sizes)):
            component = labels == label_value
            adjacent = ndimage.binary_dilation(component, structure=disk(2)) & domain
            if not np.any(adjacent & ground_seed) or not np.any(adjacent & lava_seed):
                continue
            subtiles = component_subtiles(component, spec)
            seams = crossed_internal_subtile_seams(component, spec)
            if len(subtiles) < 2 or not seams:
                continue
            covered = int(np.count_nonzero(component & exposed_seed))
            coverage = covered / exposed_count if exposed_count else 1.0
            candidates.append((coverage, int(sizes[label_value]), label_value, covered))
        if not candidates:
            continue

        coverage, pre_fill_size, selected_label, covered = max(candidates)
        if exposed_count and coverage < 0.90:
            continue
        largest = labels == selected_label
        largest = ndimage.binary_fill_holes(largest) & domain
        largest = domain_preserving_closing(largest, domain, 2)
        subtiles = component_subtiles(largest, spec)
        seams = crossed_internal_subtile_seams(largest, spec)
        if np.any(largest & ~domain):
            raise ValueError("beach mask leaked into unoccupied subtiles")
        return largest, {
            "connected_components_after_cleanup": int(count),
            "largest_component_pre_fill_pixels": pre_fill_size,
            "selection_tolerance_pixels": tolerance,
            "exposed_beach_seed_pixels": exposed_count,
            "covered_exposed_beach_seed_pixels": covered,
            "selected_region_subtiles": subtiles,
            "selected_internal_seams_crossed": seams,
            "mask_pixels_outside_occupancy": 0,
        }
    raise ValueError("no semantic beach component spans the occupied subtile graph")


def exposed_phase_edge_seeds(
    donor: np.ndarray,
    beach_mask: np.ndarray,
    domain: np.ndarray,
    ground_indices: set[int],
    lava_indices: set[int],
    spec: TemplateSpec,
    minimum_run: int,
) -> tuple[np.ndarray, np.ndarray, dict[str, object]]:
    """Extract coherent phase sources from exposed occupied-subtile edges."""

    labels = np.full(domain.shape, "U", dtype="<U1")
    labels[domain & np.isin(donor, list(ground_indices))] = "G"
    labels[domain & np.isin(donor, list(lava_indices))] = "L"
    labels[beach_mask] = "B"
    labels[~domain] = "."
    ground_seed = np.zeros_like(domain)
    lava_seed = np.zeros_like(domain)
    occupied = set(spec.terrain)
    signatures: dict[str, str] = {}
    accepted_ground_runs = 0
    accepted_lava_runs = 0

    for index in sorted(occupied):
        cell_y, cell_x = divmod(index, spec.columns)
        y0, x0 = cell_y * TILE, cell_x * TILE
        neighbors = {
            "top": index - spec.columns if cell_y > 0 else None,
            "right": index + 1 if cell_x + 1 < spec.columns else None,
            "bottom": index + spec.columns if cell_y + 1 < spec.rows else None,
            "left": index - 1 if cell_x > 0 else None,
        }
        coordinates = {
            "top": (np.full(TILE, y0), np.arange(x0, x0 + TILE)),
            "right": (np.arange(y0, y0 + TILE), np.full(TILE, x0 + TILE - 1)),
            "bottom": (np.full(TILE, y0 + TILE - 1), np.arange(x0, x0 + TILE)),
            "left": (np.arange(y0, y0 + TILE), np.full(TILE, x0)),
        }
        for side, neighbor in neighbors.items():
            if neighbor in occupied:
                continue
            yy, xx = coordinates[side]
            values = labels[yy, xx]
            signatures[f"{index:02d}.{side}"] = encode_phase_runs(values)
            start = 0
            for offset in range(1, TILE + 1):
                if offset != TILE and values[offset] == values[start]:
                    continue
                run_length = offset - start
                value = values[start]
                if run_length >= minimum_run and value in {"G", "L"}:
                    target = ground_seed if value == "G" else lava_seed
                    target[yy[start:offset], xx[start:offset]] = True
                    if value == "G":
                        accepted_ground_runs += 1
                    else:
                        accepted_lava_runs += 1
                start = offset

    ground_seed &= domain & ~beach_mask
    lava_seed &= domain & ~beach_mask
    if not np.any(ground_seed) or not np.any(lava_seed):
        raise ValueError("exposed subtile edges did not provide both phase sources")
    if np.any(ground_seed & lava_seed):
        raise ValueError("exposed phase sources overlap")
    return ground_seed, lava_seed, {
        "phase_source_method": "coherent exposed subtile edge runs",
        "minimum_phase_source_run_pixels": minimum_run,
        "accepted_ground_source_runs": accepted_ground_runs,
        "accepted_lava_source_runs": accepted_lava_runs,
        "ground_phase_source_pixels": int(np.count_nonzero(ground_seed)),
        "lava_phase_source_pixels": int(np.count_nonzero(lava_seed)),
        "exposed_phase_source_signatures": signatures,
    }


def phase_regions_from_subtile_edge_seeds(
    beach_mask: np.ndarray,
    domain: np.ndarray,
    ground_edge_seed: np.ndarray,
    lava_edge_seed: np.ndarray,
    ground_reference: np.ndarray,
    lava_reference: np.ndarray,
) -> tuple[np.ndarray, np.ndarray, np.ndarray, dict[str, object]]:
    free = domain & ~beach_mask
    labels, count = ndimage.label(free)
    ground = np.zeros_like(domain)
    lava = np.zeros_like(domain)
    unassigned = np.zeros_like(domain)
    components = []
    ground_components = 0
    lava_components = 0
    unassigned_components = 0
    for label_value in range(1, count + 1):
        component = labels == label_value
        ground_sources = int(np.count_nonzero(component & ground_edge_seed))
        lava_sources = int(np.count_nonzero(component & lava_edge_seed))
        if ground_sources and lava_sources:
            raise ValueError(f"free component {label_value} has mixed ground/lava edge sources")
        if ground_sources == lava_sources == 0:
            role = "unassigned"
            unassigned |= component
            unassigned_components += 1
        elif ground_sources:
            role = "ground"
            ground |= component
            ground_components += 1
        else:
            role = "lava"
            lava |= component
            lava_components += 1
        components.append(
            {
                "label": label_value,
                "pixels": int(np.count_nonzero(component)),
                "ground_edge_source_pixels": ground_sources,
                "lava_edge_source_pixels": lava_sources,
                "ground_reference_pixels": int(np.count_nonzero(component & ground_reference)),
                "lava_reference_pixels": int(np.count_nonzero(component & lava_reference)),
                "role": role,
            }
        )

    if not np.any(ground) or not np.any(lava):
        raise ValueError("subtile edge sources did not find both ground and lava")
    if np.any(unassigned):
        distance_to_ground = geodesic_mask_distance(domain, ground)
        distance_to_lava = geodesic_mask_distance(domain, lava)
        lava_choice = unassigned & (distance_to_lava < distance_to_ground)
        lava |= lava_choice
        ground |= unassigned & ~lava_choice

    distance_to_ground = geodesic_mask_distance(domain, ground)
    distance_to_lava = geodesic_mask_distance(domain, lava)
    lava_selector = lava | (beach_mask & (distance_to_lava < distance_to_ground))
    lava_selector &= domain
    if np.any((ground & lava) | ((ground | lava) & beach_mask)):
        raise ValueError("semantic phases overlap")
    if np.any(domain & ~(ground | lava | beach_mask)):
        raise ValueError("semantic phases do not cover the occupied domain")
    return ground, lava, lava_selector, {
        "free_space_components": components,
        "ground_component_count": ground_components,
        "lava_component_count": lava_components,
        "unassigned_component_count": unassigned_components,
        "mixed_source_component_count": 0,
        "phase_assignment_method": "coherent exposed subtile edge runs",
    }


def geodesic_mask_distance(traversable: np.ndarray, seeds: np.ndarray) -> np.ndarray:
    return geodesic_crack_distance(traversable, seeds)


def unique_frame(path: Path, expected_frames: int) -> np.ndarray:
    width, height, frames = read_shptd(path)
    if (width, height, len(frames)) != (TILE, TILE, expected_frames):
        raise ValueError(f"{path.name}: unexpected geometry")
    if len({bytes(frame) for frame in frames}) != 1:
        raise ValueError(f"{path.name}: expected one unique anchor frame")
    return np.frombuffer(frames[0], dtype=np.uint8).reshape((TILE, TILE)).copy()


def source_indices(path: Path) -> set[int]:
    _, _, frames = read_shptd(path)
    return {value for frame in frames for value in frame if value != 0}


def indices_rgb(
    indices: np.ndarray,
    palette: list[tuple[int, int, int]],
) -> np.ndarray:
    table = np.asarray(palette, dtype=np.uint8)
    return table[indices]


def beach_color_seed(
    donor: np.ndarray,
    palette: list[tuple[int, int, int]],
    ground_indices: set[int],
    water_indices: set[int],
) -> tuple[np.ndarray, set[int]]:
    counts = np.bincount(donor.reshape(-1), minlength=256)
    selected = set()
    excluded = ground_indices | water_indices | {0}
    for index, count in enumerate(counts):
        if count < 10 or index in excluded:
            continue
        red, green, blue = palette[index]
        luma = 0.299 * red + 0.587 * green + 0.114 * blue
        blue_dominant = blue > red + 22 and blue > green + 10
        green_dominant = green > red + 24 and green > blue + 6
        if 48 <= luma <= 180 and not blue_dominant and not green_dominant:
            selected.add(index)
    return np.isin(donor, list(selected)), selected


def disk(radius: int) -> np.ndarray:
    yy, xx = np.mgrid[-radius : radius + 1, -radius : radius + 1]
    return xx * xx + yy * yy <= radius * radius


def largest_beach_region(seed: np.ndarray) -> tuple[np.ndarray, dict[str, object]]:
    required = {"left", "right"}
    for tolerance in range(2, 11):
        closed = edge_preserving_closing(seed, tolerance)
        labels, count = ndimage.label(closed)
        sizes = np.bincount(labels.reshape(-1))
        candidates = []
        for label_value in range(1, len(sizes)):
            component = labels == label_value
            touched = set(touched_edges(component))
            if required <= touched:
                candidates.append((int(sizes[label_value]), label_value))
        if not candidates:
            continue

        pre_fill_size, selected_label = max(candidates)
        largest = labels == selected_label
        largest = ndimage.binary_fill_holes(largest)
        largest = edge_preserving_closing(largest, 2)
        touched = touched_edges(largest)
        if not required <= set(touched):
            continue
        return largest, {
            "connected_components_after_cleanup": int(count),
            "largest_component_pre_fill_pixels": pre_fill_size,
            "selection_tolerance_pixels": tolerance,
            "required_touched_edges": sorted(required),
            "selected_region_touched_edges": touched,
        }
    raise ValueError("no beach component spans both required footprint edges")


def edge_preserving_closing(mask: np.ndarray, radius: int) -> np.ndarray:
    # Replicate the footprint boundary before morphology.  Treating outside
    # pixels as empty erodes valid shoreline regions away from the tile edges.
    padding = radius * 2 + 2
    padded = np.pad(mask, padding, mode="edge")
    closed = ndimage.binary_closing(padded, structure=disk(radius), iterations=1)
    return closed[padding:-padding, padding:-padding]


def touched_edges(mask: np.ndarray) -> list[str]:
    edges = []
    if np.any(mask[:, 0]):
        edges.append("left")
    if np.any(mask[:, -1]):
        edges.append("right")
    if np.any(mask[0, :]):
        edges.append("top")
    if np.any(mask[-1, :]):
        edges.append("bottom")
    return edges


def feather_alpha(mask: np.ndarray, feather: float) -> np.ndarray:
    signed = ndimage.distance_transform_edt(mask) - ndimage.distance_transform_edt(~mask)
    t = np.clip((signed + feather) / (2.0 * feather), 0.0, 1.0)
    t = t * t * (3.0 - 2.0 * t)
    return np.rint(t * 255.0).astype(np.uint8)


def phase_regions(
    beach_mask: np.ndarray,
) -> tuple[np.ndarray, np.ndarray, np.ndarray, dict[str, str]]:
    # The spanning beach component is a barrier.  Flood the free space from
    # the footprint edges instead of reclassifying donor water colors.
    free = ~beach_mask
    labels, _ = ndimage.label(free)
    top_labels = {int(value) for value in labels[0, :] if value != 0}
    bottom_labels = {int(value) for value in labels[-1, :] if value != 0}
    overlap = top_labels & bottom_labels
    if overlap:
        raise ValueError("beach mask does not separate ground and lava edge sources")
    ground = np.isin(labels, list(top_labels))
    lava = np.isin(labels, list(bottom_labels))

    unassigned = free & ~(ground | lava)
    if np.any(unassigned):
        distance_to_ground = ndimage.distance_transform_edt(~ground)
        distance_to_lava = ndimage.distance_transform_edt(~lava)
        lava |= unassigned & (distance_to_lava < distance_to_ground)
        ground |= unassigned & ~lava

    distance_to_ground = ndimage.distance_transform_edt(~ground)
    distance_to_lava = ndimage.distance_transform_edt(~lava)
    lava_selector = lava | (beach_mask & (distance_to_lava < distance_to_ground))

    edge_roles = {
        "top": edge_role(ground[0, :], lava[0, :]),
        "right": edge_role(ground[:, -1], lava[:, -1]),
        "bottom": edge_role(ground[-1, :], lava[-1, :]),
        "left": edge_role(ground[:, 0], lava[:, 0]),
    }
    expected = {"top": "ground", "right": "both", "bottom": "lava", "left": "both"}
    if edge_roles != expected:
        raise ValueError(f"unexpected sh04 edge roles: {edge_roles}")
    return ground, lava, lava_selector, edge_roles


def edge_role(ground_edge: np.ndarray, lava_edge: np.ndarray) -> str:
    has_ground = bool(np.any(ground_edge))
    has_lava = bool(np.any(lava_edge))
    if has_ground and has_lava:
        return "both"
    if has_ground:
        return "ground"
    if has_lava:
        return "lava"
    return "beach"


def thermal_shore_texture(
    lava_indices: np.ndarray,
    lava_rgb: np.ndarray,
    ground_rgb: np.ndarray,
    ground_region: np.ndarray,
    lava_region: np.ndarray,
    beach_mask: np.ndarray,
    seed: int,
    domain: np.ndarray | None = None,
    glow_depth: float = GLOW_DEPTH,
    core_intensity: float = 1.0,
    shoulder_intensity: float = 0.78,
    plate_intensity: float = 0.22,
    contact_pixels: float = 0.0,
) -> tuple[np.ndarray, np.ndarray, np.ndarray, dict[str, object]]:
    """Cool the installed clear-lava crack graph across the beach band.

    The approved w1 texture is the master material, so every fissure entering
    the beach has the same centerline as the adjoining lava.  Distance fields
    control temperature and ground integration; the hard semantic mask and
    its edge roles are never warped.
    """

    if lava_indices.shape != beach_mask.shape:
        raise ValueError("lava topology and beach mask geometry differ")

    if domain is None:
        raw_lava_distance = np.maximum(ndimage.distance_transform_edt(~lava_region) - 1.0, 0.0)
        raw_ground_distance = np.maximum(ndimage.distance_transform_edt(~ground_region) - 1.0, 0.0)
        distance_method = "euclidean full footprint"
    else:
        if domain.shape != beach_mask.shape:
            raise ValueError("thermal domain and beach mask geometry differ")
        raw_lava_distance = np.maximum(geodesic_mask_distance(domain, lava_region) - 1.0, 0.0)
        raw_ground_distance = np.maximum(geodesic_mask_distance(domain, ground_region) - 1.0, 0.0)
        raw_lava_distance[~domain] = 0.0
        raw_ground_distance[~domain] = 0.0
        distance_method = "occupied-subtile geodesic"
    lava_noise = coherent_contour_noise(*beach_mask.shape[::-1], seed)
    ground_noise = coherent_contour_noise(*beach_mask.shape[::-1], seed ^ 0x4A1D)
    lava_distance = np.maximum(raw_lava_distance - lava_noise, 0.0)
    ground_distance = np.maximum(raw_ground_distance - ground_noise, 0.0)

    plate_heat = 1.0 - smoothstep_array(0.0, THERMAL_DEPTH, lava_distance)
    ground_merge = 1.0 - smoothstep_array(0.0, THERMAL_DEPTH, ground_distance)

    # Palette ranges are intentional properties of the approved clear-lava
    # texture: 48+ is the red/orange shoulder and 81+ is the persistent hot
    # core.  Cooling this exact topology prevents a second unrelated Voronoi
    # network from appearing in the bank.
    shoulder_mask = lava_indices >= 48
    core_mask = lava_indices >= 81
    crack_support = ndimage.binary_dilation(shoulder_mask, structure=disk(1))
    traversable = crack_support & (lava_region | beach_mask)
    crack_seeds = crack_support & lava_region
    crack_distance = geodesic_crack_distance(traversable, crack_seeds)
    local_glow_depth = np.clip(
        glow_depth + lava_noise,
        max(1.0, glow_depth - 3.0),
        glow_depth + 3.0,
    )
    crack_heat = np.zeros_like(raw_lava_distance, dtype=np.float32)
    connected = np.isfinite(crack_distance)
    crack_heat[connected] = 1.0 - smoothstep_array(
        0.0,
        local_glow_depth[connected],
        crack_distance[connected],
    )
    crack_heat *= beach_mask

    lava_float = lava_rgb.astype(np.float32)
    ground_float = ground_rgb.astype(np.float32)
    luma = 0.299 * lava_float[:, :, 0] + 0.587 * lava_float[:, :, 1] + 0.114 * lava_float[:, :, 2]
    plate_value = np.clip(34.0 + 0.18 * (luma - 36.0), 30.0, 50.0)
    cold_plate = np.stack((plate_value, plate_value - 2.0, plate_value - 2.0), axis=2)

    cold_material = cold_plate.copy()
    shoulder_only = shoulder_mask & ~core_mask
    cold_material[shoulder_only] = (
        0.40 * cold_plate[shoulder_only] + 0.60 * np.array((23.0, 14.0, 12.0))
    )
    cold_material[core_mask] = (13.0, 9.0, 9.0)

    # Intact plates receive only restrained contact warmth.  Crack shoulders
    # cool sooner than their cores, while the approved bright junction/core
    # values survive closest to lava.
    plate_mix = plate_intensity * plate_heat
    shoulder_mix = shoulder_intensity * np.power(crack_heat, 1.55)
    core_mix = core_intensity * np.power(crack_heat, 0.90)
    if contact_pixels > 0.0:
        contact = 1.0 - smoothstep_array(0.0, contact_pixels, raw_lava_distance)
        shoulder_mix = np.maximum(shoulder_mix, 0.55 * contact)
        core_mix = np.maximum(core_mix, contact)
    hot_mix = plate_mix.copy()
    hot_mix[shoulder_only] = np.maximum(plate_mix[shoulder_only], shoulder_mix[shoulder_only])
    hot_mix[core_mask] = np.maximum(plate_mix[core_mask], core_mix[core_mask])
    hot_mix = np.clip(hot_mix, 0.0, 1.0)

    material = lerp_array(cold_material, lava_float, hot_mix[:, :, None])
    material = lerp_array(material, ground_float, ground_merge[:, :, None])
    material = np.clip(np.rint(material), 0, 255).astype(np.uint8)

    field_preview = thermal_field_preview(
        beach_mask,
        ground_merge,
        plate_heat,
        crack_heat,
        shoulder_mask,
        core_mask,
    )
    crack_preview = crack_heat_preview(beach_mask, shoulder_mask, core_mask, crack_heat)

    potential_glow = beach_mask & shoulder_mask & (lava_distance <= glow_depth + CONTOUR_JITTER)
    orphan_glow = potential_glow & ~connected
    metrics = {
        "thermal_transition_pixels": THERMAL_DEPTH,
        "glow_fade_pixels": glow_depth,
        "contour_jitter_pixels": CONTOUR_JITTER,
        "shared_crack_topology": "installed w1.vol",
        "crack_support_pixels_in_beach": int(np.count_nonzero(beach_mask & shoulder_mask)),
        "heated_crack_pixels_in_beach": int(np.count_nonzero(beach_mask & shoulder_mask & (crack_heat > 0.02))),
        "orphan_glow_pixels": int(np.count_nonzero(orphan_glow)),
        "ground_merge_pixels_in_beach": int(np.count_nonzero(beach_mask & (ground_merge > 0.02))),
        "top_edge_pixels_preserved": True,
        "bottom_edge_pixels_preserved": True,
    }
    if domain is not None:
        metrics.pop("top_edge_pixels_preserved")
        metrics.pop("bottom_edge_pixels_preserved")
        metrics["thermal_distance_method"] = distance_method
        metrics["occupied_phase_pixels_preserved"] = True
    if (
        glow_depth != GLOW_DEPTH
        or core_intensity != 1.0
        or shoulder_intensity != 0.78
        or plate_intensity != 0.22
        or contact_pixels != 0.0
    ):
        metrics["seepage_core_intensity_cap"] = core_intensity
        metrics["seepage_shoulder_intensity_cap"] = shoulder_intensity
        metrics["seepage_plate_intensity"] = plate_intensity
        metrics["seepage_contact_pixels"] = contact_pixels
    return material, field_preview, crack_preview, metrics


def coherent_contour_noise(width: int, height: int, seed: int) -> np.ndarray:
    result = np.empty((height, width), dtype=np.float32)
    period = max(width, height)
    for y in range(height):
        for x in range(width):
            broad = periodic_value_noise(x, y, period, 24, seed)
            broader = periodic_value_noise(x + 17, y - 11, period, 48, seed ^ 0x197D)
            result[y, x] = ((0.68 * broad + 0.32 * broader) - 0.5) * (2.0 * CONTOUR_JITTER)
    return ndimage.gaussian_filter(result, sigma=1.1, mode="wrap")


def geodesic_crack_distance(traversable: np.ndarray, seeds: np.ndarray) -> np.ndarray:
    """Return 8-neighbor path distance through the shared fissure support."""

    height, width = traversable.shape
    distance = np.full((height, width), np.inf, dtype=np.float64)
    heap: list[tuple[float, int, int]] = []
    for y, x in np.argwhere(seeds):
        distance[y, x] = 0.0
        heappush(heap, (0.0, int(y), int(x)))

    neighbors = (
        (-1, 0, 1.0),
        (1, 0, 1.0),
        (0, -1, 1.0),
        (0, 1, 1.0),
        (-1, -1, math.sqrt(2.0)),
        (-1, 1, math.sqrt(2.0)),
        (1, -1, math.sqrt(2.0)),
        (1, 1, math.sqrt(2.0)),
    )
    while heap:
        current, y, x = heappop(heap)
        if current > float(distance[y, x]) + 1e-9:
            continue
        for dy, dx, step in neighbors:
            ny, nx = y + dy, x + dx
            if ny < 0 or ny >= height or nx < 0 or nx >= width or not traversable[ny, nx]:
                continue
            candidate = current + step
            if candidate < distance[ny, nx]:
                distance[ny, nx] = candidate
                heappush(heap, (candidate, ny, nx))
    return distance


def thermal_field_preview(
    beach: np.ndarray,
    ground_merge: np.ndarray,
    plate_heat: np.ndarray,
    crack_heat: np.ndarray,
    shoulder: np.ndarray,
    core: np.ndarray,
) -> np.ndarray:
    height, width = beach.shape
    result = np.zeros((height, width, 3), dtype=np.float32)
    result[:, :] = (28.0, 31.0, 35.0)
    result[beach] = (55.0, 55.0, 57.0)
    warm = beach[:, :, None] * plate_heat[:, :, None] * np.array((115.0, 35.0, 8.0))
    result += warm
    ground_color = np.zeros_like(result)
    ground_color[:, :] = (74.0, 104.0, 82.0)
    ground_weight = (beach * ground_merge)[:, :, None]
    result = lerp_array(result, ground_color, 0.72 * ground_weight)
    shoulder_weight = (beach & shoulder)[:, :, None] * crack_heat[:, :, None]
    core_weight = (beach & core)[:, :, None] * crack_heat[:, :, None]
    result = lerp_array(result, np.array((224.0, 91.0, 24.0)), 0.75 * shoulder_weight)
    result = lerp_array(result, np.array((255.0, 204.0, 56.0)), core_weight)
    return np.clip(np.rint(result), 0, 255).astype(np.uint8)


def crack_heat_preview(
    beach: np.ndarray,
    shoulder: np.ndarray,
    core: np.ndarray,
    crack_heat: np.ndarray,
) -> np.ndarray:
    height, width = beach.shape
    result = np.zeros((height, width, 3), dtype=np.float32)
    result[:, :] = (28.0, 31.0, 35.0)
    result[beach] = (45.0, 43.0, 43.0)
    cold_cracks = beach & shoulder
    result[cold_cracks] = (15.0, 10.0, 10.0)
    shoulder_weight = cold_cracks[:, :, None] * crack_heat[:, :, None]
    core_weight = (beach & core)[:, :, None] * crack_heat[:, :, None]
    result = lerp_array(result, np.array((210.0, 65.0, 20.0)), shoulder_weight)
    result = lerp_array(result, np.array((255.0, 200.0, 48.0)), core_weight)
    return np.clip(np.rint(result), 0, 255).astype(np.uint8)


def basalt_crack_texture(width: int, height: int, seed: int) -> np.ndarray:
    sites = make_sites(width, height, 56, seed)
    result = np.zeros((height, width, 3), dtype=np.uint8)
    for y in range(height):
        for x in range(width):
            qx = x + 4.0 * math.sin(math.tau * y / 61.0) + 1.7 * math.sin(math.tau * (x + y) / 83.0)
            qy = y + 3.5 * math.sin(math.tau * x / 67.0 + 0.8) - 1.4 * math.sin(math.tau * (x - y) / 79.0)
            distances = sorted(
                (math.hypot(qx - sx, qy - sy), tone)
                for sx, sy, tone in sites
            )
            gap = distances[1][0] - distances[0][0]
            tone = distances[0][1]
            broad = periodic_value_noise(x, y, 144, 24, seed ^ 0x51A7)
            plate = np.array(
                (
                    47 + int(13 * broad + 5 * tone),
                    44 + int(10 * broad + 3 * tone),
                    43 + int(9 * broad + 2 * tone),
                ),
                dtype=np.float32,
            )
            shoulder = 1.0 - smoothstep(0.75, 2.3, gap)
            core = 1.0 - smoothstep(0.10, 0.72, gap)
            color = plate * (1.0 - 0.23 * shoulder) + np.array((75, 63, 57)) * (0.23 * shoulder)
            color = color * (1.0 - core) + np.array((13, 9, 9)) * core
            result[y, x] = np.clip(np.rint(color), 0, 255).astype(np.uint8)
    return result


def make_sites(
    width: int,
    height: int,
    count: int,
    seed: int,
) -> list[tuple[float, float, float]]:
    rng = Random(seed)
    minimum = 11.0
    sites: list[tuple[float, float, float]] = []
    for _ in range(count):
        for _attempt in range(2000):
            candidate = (
                rng.uniform(-8.0, width + 8.0),
                rng.uniform(-8.0, height + 8.0),
                rng.uniform(-1.0, 1.0),
            )
            if all(math.hypot(candidate[0] - sx, candidate[1] - sy) >= minimum for sx, sy, _ in sites):
                sites.append(candidate)
                break
        else:
            raise ValueError("could not distribute basalt Voronoi sites")
    return sites


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


def smoother(value: float) -> float:
    return value * value * value * (value * (value * 6.0 - 15.0) + 10.0)


def smoothstep(low: float, high: float, value: float) -> float:
    t = max(0.0, min(1.0, (value - low) / (high - low)))
    return t * t * (3.0 - 2.0 * t)


def smoothstep_array(
    low: float | np.ndarray,
    high: float | np.ndarray,
    value: np.ndarray,
) -> np.ndarray:
    denominator = np.asarray(high) - np.asarray(low)
    t = np.clip((value - low) / denominator, 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def lerp_array(a: np.ndarray, b: np.ndarray, t: np.ndarray) -> np.ndarray:
    return a + (b - a) * t


def linear_alpha_composite(
    base: np.ndarray,
    overlay: np.ndarray,
    alpha: np.ndarray,
) -> np.ndarray:
    base_float = srgb_to_linear(base.astype(np.float32) / 255.0)
    overlay_float = srgb_to_linear(overlay.astype(np.float32) / 255.0)
    a = alpha.astype(np.float32)[:, :, None] / 255.0
    mixed = overlay_float * a + base_float * (1.0 - a)
    return np.clip(np.rint(linear_to_srgb(mixed) * 255.0), 0, 255).astype(np.uint8)


def srgb_to_linear(values: np.ndarray) -> np.ndarray:
    return np.where(values <= 0.04045, values / 12.92, ((values + 0.055) / 1.055) ** 2.4)


def linear_to_srgb(values: np.ndarray) -> np.ndarray:
    return np.where(values <= 0.0031308, values * 12.92, 1.055 * values ** (1.0 / 2.4) - 0.055)


def mask_image(
    mask: np.ndarray,
    color: tuple[int, int, int],
    domain: np.ndarray | None = None,
) -> Image.Image:
    data = np.zeros((*mask.shape, 3), dtype=np.uint8)
    data[:, :] = (28, 31, 35)
    data[mask] = color
    if domain is not None:
        data[~domain] = BACKGROUND
    return Image.fromarray(data, mode="RGB")


def semantic_phase_preview(
    ground: np.ndarray,
    lava: np.ndarray,
    beach: np.ndarray,
    domain: np.ndarray,
) -> Image.Image:
    data = np.zeros((*domain.shape, 3), dtype=np.uint8)
    data[:, :] = BACKGROUND
    data[ground] = (70, 96, 78)
    data[lava] = (151, 52, 26)
    data[beach] = (183, 121, 65)
    return Image.fromarray(data, mode="RGB")


def template_occupancy_preview(
    donor_rgb: np.ndarray,
    domain: np.ndarray,
    spec: TemplateSpec,
) -> Image.Image:
    data = donor_rgb.copy()
    data[~domain] = BACKGROUND
    image = Image.fromarray(data, mode="RGB")
    draw = ImageDraw.Draw(image)
    font = ImageFont.load_default()
    abbreviations = {
        "Clear": "G",
        "Rock": "G/R",
        "Beach": "B",
        "Water": "W",
        "River": "W/Rv",
    }
    for index in range(spec.columns * spec.rows):
        cell_y, cell_x = divmod(index, spec.columns)
        x0, y0 = cell_x * TILE, cell_y * TILE
        occupied = index in spec.terrain
        outline = (192, 205, 214) if occupied else (92, 105, 116)
        draw.rectangle((x0, y0, x0 + TILE - 1, y0 + TILE - 1), outline=outline, width=1)
        if occupied:
            label = f"{index:02d} {abbreviations.get(spec.terrain[index], spec.terrain[index][:2])}"
            draw.rectangle((x0 + 2, y0 + 2, x0 + 35, y0 + 12), fill=(24, 28, 32))
            draw.text((x0 + 4, y0 + 3), label, fill="white", font=font)
    return image


def phase_source_preview(
    donor_rgb: np.ndarray,
    domain: np.ndarray,
    ground_source: np.ndarray,
    lava_source: np.ndarray,
    spec: TemplateSpec,
) -> Image.Image:
    data = np.clip(np.rint(donor_rgb.astype(np.float32) * 0.38), 0, 255).astype(np.uint8)
    data[~domain] = BACKGROUND
    ground_display = ndimage.binary_dilation(ground_source, structure=disk(1)) & domain
    lava_display = ndimage.binary_dilation(lava_source, structure=disk(1)) & domain
    data[ground_display] = (98, 220, 132)
    data[lava_display] = (255, 92, 42)
    image = Image.fromarray(data, mode="RGB")
    draw = ImageDraw.Draw(image)
    font = ImageFont.load_default()
    for index in range(spec.columns * spec.rows):
        cell_y, cell_x = divmod(index, spec.columns)
        x0, y0 = cell_x * TILE, cell_y * TILE
        outline = (173, 185, 196) if index in spec.terrain else (92, 105, 116)
        draw.rectangle((x0, y0, x0 + TILE - 1, y0 + TILE - 1), outline=outline, width=1)
        if index in spec.terrain:
            draw.rectangle((x0 + 18, y0 + 18, x0 + 31, y0 + 28), fill=(24, 28, 32))
            draw.text((x0 + 20, y0 + 19), f"{index:02d}", fill="white", font=font)
    return image


def encode_phase_runs(values: np.ndarray) -> str:
    if len(values) == 0:
        return ""
    runs = []
    start = 0
    for offset in range(1, len(values) + 1):
        if offset == len(values) or values[offset] != values[start]:
            runs.append(f"{values[start]}{start}-{offset - 1}")
            start = offset
    return ",".join(runs)


def subtile_edge_preview(
    ground: np.ndarray,
    lava: np.ndarray,
    beach: np.ndarray,
    domain: np.ndarray,
    spec: TemplateSpec,
) -> tuple[Image.Image, dict[str, object], dict[str, object]]:
    labels = np.full(domain.shape, ".", dtype="<U1")
    labels[ground] = "G"
    labels[lava] = "L"
    labels[beach] = "B"
    colors = {
        "G": np.array((98, 220, 132), dtype=np.uint8),
        "B": np.array((229, 156, 78), dtype=np.uint8),
        "L": np.array((255, 92, 42), dtype=np.uint8),
        ".": np.array(BACKGROUND, dtype=np.uint8),
    }
    data = np.zeros((*domain.shape, 3), dtype=np.uint8)
    data[:, :] = BACKGROUND
    data[ground] = (54, 75, 61)
    data[lava] = (111, 39, 25)
    data[beach] = (117, 78, 47)

    signatures: dict[str, object] = {}
    occupied = set(spec.terrain)
    internal_joins = 0
    mismatched_pairs = 0
    direct_ground_lava = 0
    for index in sorted(occupied):
        cell_y, cell_x = divmod(index, spec.columns)
        y0, x0 = cell_y * TILE, cell_x * TILE
        edges = {
            "top": labels[y0, x0 : x0 + TILE],
            "right": labels[y0 : y0 + TILE, x0 + TILE - 1],
            "bottom": labels[y0 + TILE - 1, x0 : x0 + TILE],
            "left": labels[y0 : y0 + TILE, x0],
        }
        signatures[f"{index:02d}"] = {
            "terrain": spec.terrain[index],
            **{side: encode_phase_runs(values) for side, values in edges.items()},
        }

        for offset, value in enumerate(edges["top"]):
            data[y0 : y0 + 2, x0 + offset] = colors[value]
        for offset, value in enumerate(edges["right"]):
            data[y0 + offset, x0 + TILE - 2 : x0 + TILE] = colors[value]
        for offset, value in enumerate(edges["bottom"]):
            data[y0 + TILE - 2 : y0 + TILE, x0 + offset] = colors[value]
        for offset, value in enumerate(edges["left"]):
            data[y0 + offset, x0 : x0 + 2] = colors[value]

        if cell_x + 1 < spec.columns and index + 1 in occupied:
            other = labels[y0 : y0 + TILE, x0 + TILE]
            own = edges["right"]
            internal_joins += 1
            mismatched_pairs += int(np.count_nonzero(own != other))
            direct_ground_lava += int(
                np.count_nonzero(((own == "G") & (other == "L")) | ((own == "L") & (other == "G")))
            )
        if cell_y + 1 < spec.rows and index + spec.columns in occupied:
            other = labels[y0 + TILE, x0 : x0 + TILE]
            own = edges["bottom"]
            internal_joins += 1
            mismatched_pairs += int(np.count_nonzero(own != other))
            direct_ground_lava += int(
                np.count_nonzero(((own == "G") & (other == "L")) | ((own == "L") & (other == "G")))
            )

    if direct_ground_lava:
        raise ValueError(f"{direct_ground_lava} direct ground/lava contacts cross subtile joins")
    if np.any((labels != ".") & ~domain):
        raise ValueError("phase labels leaked into blank subtiles")

    image = Image.fromarray(data, mode="RGB")
    draw = ImageDraw.Draw(image)
    font = ImageFont.load_default()
    for index in sorted(occupied):
        cell_y, cell_x = divmod(index, spec.columns)
        x0, y0 = cell_x * TILE, cell_y * TILE
        draw.rectangle((x0 + 18, y0 + 18, x0 + 31, y0 + 28), fill=(24, 28, 32))
        draw.text((x0 + 20, y0 + 19), f"{index:02d}", fill="white", font=font)
    return image, signatures, {
        "internal_subtile_joins": internal_joins,
        "internal_edge_label_mismatched_pixels": mismatched_pairs,
        "direct_ground_lava_seam_conflicts": direct_ground_lava,
    }


def edge_role_preview(
    ground: np.ndarray,
    lava: np.ndarray,
    beach: np.ndarray,
    roles: dict[str, str],
) -> Image.Image:
    margin = 24
    image = Image.new("RGB", (SIZE + 2 * margin, SIZE + 2 * margin), (28, 31, 35))
    data = np.zeros((SIZE, SIZE, 3), dtype=np.uint8)
    data[:, :] = (36, 39, 43)
    data[ground] = (70, 96, 78)
    data[lava] = (151, 52, 26)
    data[beach] = (183, 121, 65)
    image.paste(Image.fromarray(data, mode="RGB"), (margin, margin))

    draw = ImageDraw.Draw(image)
    font = ImageFont.load_default()
    colors = {"ground": (98, 220, 132), "lava": (255, 92, 42), "both": (211, 111, 255)}
    left, top = margin, margin
    right, bottom = margin + SIZE - 1, margin + SIZE - 1
    draw.line((left, top, right, top), fill=colors[roles["top"]], width=4)
    draw.line((right, top, right, bottom), fill=colors[roles["right"]], width=4)
    draw.line((left, bottom, right, bottom), fill=colors[roles["bottom"]], width=4)
    draw.line((left, top, left, bottom), fill=colors[roles["left"]], width=4)
    draw.text((margin + 48, 6), "GROUND", fill=colors["ground"], font=font)
    draw.text((margin + 57, margin + SIZE + 8), "LAVA", fill=colors["lava"], font=font)
    draw.text((2, margin + 66), "BOTH", fill=colors["both"], font=font)
    draw.text((margin + SIZE + 2, margin + 66), "BOTH", fill=colors["both"], font=font)
    return image


def checker_composite(rgba: np.ndarray) -> Image.Image:
    height, width = rgba.shape[:2]
    yy, xx = np.mgrid[0:height, 0:width]
    checks = ((xx // 8 + yy // 8) % 2).astype(bool)
    base = np.zeros((height, width, 3), dtype=np.uint8)
    base[checks] = (106, 112, 118)
    base[~checks] = (73, 80, 87)
    rgb = linear_alpha_composite(base, rgba[:, :, :3], rgba[:, :, 3])
    return Image.fromarray(rgb, mode="RGB")


def write_review_sheet(
    path: Path,
    panels: list[tuple[str, Image.Image]],
    columns: int = 3,
    scale: int = 3,
) -> None:
    rows = (len(panels) + columns - 1) // columns
    header = 30
    legacy_square = scale == 3
    if legacy_square:
        # Freeze the accepted sh04 review layout byte-for-byte.
        panel_width = SIZE * scale
        panel_height = SIZE * scale
    else:
        source_width = max(image.width for _, image in panels)
        source_height = max(image.height for _, image in panels)
        panel_width = source_width * scale
        panel_height = source_height * scale
    sheet = Image.new(
        "RGB",
        (columns * panel_width, rows * (panel_height + header)),
        BACKGROUND,
    )
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for index, (label, image) in enumerate(panels):
        x = (index % columns) * panel_width
        y = (index // columns) * (panel_height + header)
        draw.text((x + 7, y + 8), label, fill="white", font=font)
        rendered = image.resize(
            (panel_width, panel_height) if legacy_square else (image.width * scale, image.height * scale),
            Image.Resampling.NEAREST,
        )
        offset_x = x + (panel_width - rendered.width) // 2
        offset_y = y + header + (panel_height - rendered.height) // 2
        sheet.paste(rendered, (offset_x, offset_y))
    sheet.save(path)


if __name__ == "__main__":
    raise SystemExit(main())
