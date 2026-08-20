#!/usr/bin/env python
"""Convert a raw Temperate river delta into new Volcanic preview art.

This implementation deliberately does not import or consume any prior river,
shoreline, or manual-delta converter/output. It starts from the Temperate SHP
frames and rebuilds every material at 24px authoring density.
"""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
AUTHOR_TILE = 24
OUTPUT_TILE = 48
UPSCALE = 2
BACKGROUND = (67, 78, 88)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--template", default="sh18")
    parser.add_argument(
        "--water-inset",
        type=float,
        default=0.0,
        help="interior shoreline inset in 24px authoring pixels",
    )
    parser.add_argument("--edge-lock", type=float, default=3.0)
    parser.add_argument("--edge-taper", type=float, default=6.0)
    parser.add_argument(
        "--water-reference",
        choices=("w1", "w1+w2"),
        default="w1",
        help="exact Temperate palette-index source used to identify donor water",
    )
    parser.add_argument(
        "--water-mask",
        type=Path,
        help="authoritative 24px-density RGBA mask; nonzero alpha means water",
    )
    parser.add_argument(
        "--ground-mask",
        type=Path,
        help="authoritative 24px-density RGBA mask; nonzero alpha means clear ground",
    )
    parser.add_argument(
        "--generated-islands",
        action=argparse.BooleanOptionalAction,
        default=False,
        help="optionally generate extra cooled islands inside classified water",
    )
    parser.add_argument(
        "--canonical-liquid-w1",
        type=Path,
        help="phase-aligned 48px w1 VOL used as the sole liquid material",
    )
    parser.add_argument(
        "--out-dir",
        type=Path,
        default=Path.home()
        / "Documents/agents/volcanic-theater/river-delta-v2/sh18",
    )
    args = parser.parse_args()
    out_dir = args.out_dir.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    image_name = f"{args.template}.tem"
    columns, rows, occupied = parse_template(
        ROOT / "mods/cameo/tilesets/ra_temperat.yaml", image_name
    )
    donor_palette = read_palette(
        ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    )
    volcanic_palette = read_palette(
        ROOT / "mods/cameo/bits/volcanic/volcanic.pal"
    )

    donor_indices, domain = compose_author_indices(
        ROOT / "mods/cameo/bits/temp" / image_name,
        columns,
        rows,
        occupied,
    )
    donor_rgb = palette_rgb(donor_indices, donor_palette)
    donor_rgb[~domain] = BACKGROUND

    water_indices = source_index_set(ROOT / "mods/cameo/bits/temp/w1.tem")
    if args.water_reference == "w1+w2":
        water_indices |= source_index_set(ROOT / "mods/cameo/bits/temp/w2.tem")
    water_mask_metrics: dict[str, object]
    if args.water_mask is not None:
        mask_path = args.water_mask.resolve()
        mask_rgba = np.asarray(Image.open(mask_path).convert("RGBA"))
        if mask_rgba.shape[:2] != donor_indices.shape:
            raise ValueError(
                f"water mask {mask_rgba.shape[1]}x{mask_rgba.shape[0]} differs "
                f"from donor {donor_indices.shape[1]}x{donor_indices.shape[0]}"
            )
        if not set(np.unique(mask_rgba[:, :, 3])).issubset({0, 255}):
            raise ValueError("authoritative water mask alpha must contain only 0 and 255")
        donor_water = domain & (mask_rgba[:, :, 3] > 0)
        rgb_matches = bool(
            np.array_equal(mask_rgba[:, :, :3][donor_water], donor_rgb[donor_water])
        )
        if not rgb_matches:
            raise ValueError("selected water layer RGB differs from pristine donor pixels")
        selected_indices = sorted(int(value) for value in np.unique(donor_indices[donor_water]))
        water_mask_metrics = {
            "source": "authoritative RGBA alpha mask",
            "path": str(mask_path),
            "selected_palette_index_count": len(selected_indices),
            "selected_palette_indices": selected_indices,
            "selected_rgb_matches_pristine_donor": rgb_matches,
            "tolerance": 0,
        }
    else:
        donor_water = domain & np.isin(
            donor_indices, np.fromiter(water_indices, dtype=np.uint8)
        )
        water_mask_metrics = {
            "source": args.water_reference,
            "palette_index_count": len(water_indices),
            "palette_indices": sorted(water_indices),
            "tolerance": 0,
        }
    if not np.any(donor_water) or np.all(donor_water[domain]):
        raise ValueError(f"{args.template}: donor water classification is degenerate")
    water, inset_metrics = inset_water_mask(
        donor_water,
        args.water_inset,
        args.edge_lock,
        args.edge_taper,
    )

    ground_mask_metrics: dict[str, object] | None = None
    authoritative_ground: np.ndarray | None = None
    if args.ground_mask is not None:
        ground_path = args.ground_mask.resolve()
        ground_rgba = np.asarray(Image.open(ground_path).convert("RGBA"))
        if ground_rgba.shape[:2] != donor_indices.shape:
            raise ValueError(
                f"ground mask {ground_rgba.shape[1]}x{ground_rgba.shape[0]} differs "
                f"from donor {donor_indices.shape[1]}x{donor_indices.shape[0]}"
            )
        if not set(np.unique(ground_rgba[:, :, 3])).issubset({0, 255}):
            raise ValueError("authoritative ground mask alpha must contain only 0 and 255")
        authoritative_ground = domain & (ground_rgba[:, :, 3] > 0)
        if np.any(authoritative_ground & water):
            raise ValueError("authoritative ground and water masks overlap")
        if not np.array_equal(
            ground_rgba[:, :, :3][authoritative_ground], donor_rgb[authoritative_ground]
        ):
            raise ValueError("selected ground layer RGB differs from pristine donor pixels")
        selected_ground_indices = sorted(
            int(value) for value in np.unique(donor_indices[authoritative_ground])
        )
        ground_mask_metrics = {
            "source": "authoritative RGBA alpha mask",
            "path": str(ground_path),
            "selected_palette_index_count": len(selected_ground_indices),
            "selected_palette_indices": selected_ground_indices,
            "selected_rgb_matches_pristine_donor": True,
            "tolerance": 0,
        }

    clear_tile = author_frame(ROOT / "mods/cameo/bits/volcanic/clear1.vol", 0)
    cracked_tile = author_frame(ROOT / "mods/cameo/bits/volcanic/w1.vol", 0)
    clear_indices = repeat_tile(clear_tile, columns, rows)
    cracked_indices = repeat_tile(cracked_tile, columns, rows)
    clear_rgb = palette_rgb(clear_indices, volcanic_palette)
    cracked_hot_rgb = palette_rgb(cracked_indices, volcanic_palette)
    cracked_dry_rgb = dry_cracked(cracked_indices, cracked_hot_rgb)

    canonical_liquid_indices: np.ndarray | None = None
    if args.canonical_liquid_w1 is not None:
        canonical_path = args.canonical_liquid_w1.resolve()
        canonical_tile = author_frame(canonical_path, 0)
        canonical_liquid_indices = repeat_tile(canonical_tile, columns, rows)
        liquid_rgb = palette_rgb(canonical_liquid_indices, volcanic_palette)
        liquid_metrics = {
            "source": "phase-aligned canonical proper-liquid w1",
            "path": str(canonical_path),
            "donor_local_texture_generation": False,
        }
    else:
        liquid_rgb, liquid_metrics = donor_liquid(donor_rgb, water)
    distance_to_water = ndimage.distance_transform_edt(~water)
    nonwater = domain & ~water
    if authoritative_ground is None:
        hot_bank = nonwater & (distance_to_water <= 5.5)
        dry_bank = nonwater & (distance_to_water > 5.5) & (distance_to_water <= 12.5)
    else:
        shoreline = nonwater & ~authoritative_ground
        hot_bank = shoreline & (distance_to_water <= 5.5)
        dry_bank = shoreline & ~hot_bank

    islands = (
        donor_cooled_islands(water, donor_rgb)
        if args.generated_islands
        else np.zeros_like(water)
    )
    water_without_islands = water & ~islands
    hot_bank |= islands

    candidate = clear_rgb.copy()
    candidate[dry_bank] = cracked_dry_rgb[dry_bank]
    candidate[hot_bank] = cracked_hot_rgb[hot_bank]
    candidate[water_without_islands] = liquid_rgb[water_without_islands]
    candidate[~domain] = BACKGROUND

    indexed_image, candidate_indices = quantize(candidate, volcanic_palette)
    if canonical_liquid_indices is not None:
        candidate_indices[water_without_islands] = canonical_liquid_indices[
            water_without_islands
        ]
    candidate_indices[~domain] = 0
    indexed_rgb = palette_rgb(candidate_indices, volcanic_palette)
    indexed_rgb[~domain] = BACKGROUND

    output_indices = upscale(candidate_indices)
    output_domain = upscale(domain.astype(np.uint8)).astype(bool)
    output_rgb = palette_rgb(output_indices, volcanic_palette)
    output_rgb[~output_domain] = BACKGROUND

    frames = split_frames(output_indices, columns, rows, occupied)
    vol_path = out_dir / f"{args.template}-proper-volcanic-delta-preview.vol"
    write_shptd(vol_path, OUTPUT_TILE, OUTPUT_TILE, frames)
    verify_roundtrip(vol_path, frames)

    donor_author = Image.fromarray(donor_rgb, mode="RGB")
    candidate_author = Image.fromarray(indexed_rgb, mode="RGB")
    candidate_output = Image.fromarray(output_rgb, mode="RGB")
    material_map = material_preview(domain, water_without_islands, dry_bank, hot_bank, islands)
    water_mask_image = water_mask_preview(domain, water)
    donor_author.save(out_dir / f"temperate_donor_author24_{args.template}.png")
    material_map.save(out_dir / f"volcanic_material_map_author24_{args.template}.png")
    water_mask_image.save(out_dir / f"water_mask_author24_{args.template}.png")
    candidate_author.save(out_dir / f"volcanic_delta_author24_{args.template}.png")
    candidate_output.save(out_dir / f"volcanic_delta_output48_{args.template}.png")

    review_path = out_dir / f"temperate_to_new_volcanic_delta_review_{args.template}.png"
    write_review(review_path, donor_author, material_map, candidate_author, candidate_output, args.template)
    pair_path = out_dir / f"temperate_vs_new_volcanic_delta_{args.template}.png"
    donor_output = donor_author.resize(candidate_output.size, Image.Resampling.NEAREST)
    write_pair_review(pair_path, donor_output, candidate_output, args.template)

    audit = {
        "template": args.template,
        "preview_only": True,
        "production_assets_modified": False,
        "author_tile_size": AUTHOR_TILE,
        "output_tile_size": OUTPUT_TILE,
        "upscale": "exact 2x nearest-neighbor",
        "strict_2x_blocks": strict_2x(output_indices),
        "columns": columns,
        "rows": rows,
        "frame_count": columns * rows,
        "occupied_frames": sorted(occupied),
        "water_pixels_author24": int(np.count_nonzero(water)),
        "water_reference": {
            **water_mask_metrics,
        },
        "ground_reference": ground_mask_metrics,
        "ground_pixels_author24": (
            int(np.count_nonzero(authoritative_ground))
            if authoritative_ground is not None else None
        ),
        "shoreline_pixels_author24": (
            int(np.count_nonzero(domain & ~water & ~authoritative_ground))
            if authoritative_ground is not None else None
        ),
        "ground_water_overlap_pixels": (
            int(np.count_nonzero(authoritative_ground & water))
            if authoritative_ground is not None else None
        ),
        "water_inset": inset_metrics,
        "dry_bank_pixels_author24": int(np.count_nonzero(dry_bank)),
        "hot_bank_pixels_author24": int(np.count_nonzero(hot_bank & ~islands)),
        "cooled_island_pixels_author24": int(np.count_nonzero(islands)),
        "cooled_island_components": int(ndimage.label(islands)[1]),
        "generated_islands_enabled": args.generated_islands,
        "classified_water_pixels_overridden_as_ground": int(
            np.count_nonzero(water & ~water_without_islands)
        ),
        "canonical_liquid_exact_pixels": (
            int(
                np.count_nonzero(
                    candidate_indices[water_without_islands]
                    == canonical_liquid_indices[water_without_islands]
                )
            )
            if canonical_liquid_indices is not None
            else None
        ),
        "canonical_liquid_total_pixels": (
            int(np.count_nonzero(water_without_islands))
            if canonical_liquid_indices is not None
            else None
        ),
        "liquid": liquid_metrics,
        "provenance": {
            "raw_donor": str((ROOT / "mods/cameo/bits/temp" / image_name).resolve()),
            "manual_cutouts_used": False,
            "approved_delta_pngs_used": False,
            "previous_delta_converter_imported": False,
            "previous_shoreline_converter_imported": False,
            "authoritative_xcf_water_mask_used": args.water_mask is not None,
            "approved_material_references": ["clear1.vol", "w1.vol", "volcanic.pal"],
        },
    }
    (out_dir / f"new_volcanic_delta_audit_{args.template}.json").write_text(
        json.dumps(audit, indent=2) + "\n", encoding="utf-8"
    )
    print(pair_path)
    print(review_path)
    return 0


def parse_template(path: Path, image_name: str) -> tuple[int, int, set[int]]:
    lines = path.read_text(encoding="utf-8").splitlines()
    target = f"Images: {image_name}"
    for image_line, line in enumerate(lines):
        if line.strip() != target:
            continue
        start = image_line
        while start >= 0 and not lines[start].startswith("\tTemplate@"):
            start -= 1
        end = image_line + 1
        while end < len(lines) and not lines[end].startswith("\tTemplate@"):
            end += 1
        block = lines[start:end]
        size_line = next(item for item in block if item.strip().startswith("Size:"))
        columns, rows = (int(value.strip()) for value in size_line.split(":", 1)[1].split(","))
        occupied: set[int] = set()
        in_tiles = False
        for item in block:
            stripped = item.strip()
            if stripped == "Tiles:":
                in_tiles = True
                continue
            if in_tiles:
                if not item.startswith("\t\t\t"):
                    break
                key = stripped.split(":", 1)[0]
                if key.isdigit():
                    occupied.add(int(key))
        return columns, rows, occupied
    raise ValueError(f"template not found: {image_name}")


def read_palette(path: Path) -> np.ndarray:
    data = path.read_bytes()
    if len(data) != 768:
        raise ValueError(f"{path}: expected a 768-byte palette")
    return np.asarray(
        [tuple(data[offset + channel] * 4 for channel in range(3)) for offset in range(0, 768, 3)],
        dtype=np.uint8,
    )


def compose_author_indices(
    path: Path, columns: int, rows: int, occupied: set[int]
) -> tuple[np.ndarray, np.ndarray]:
    width, height, frames = read_shptd(path)
    if width != OUTPUT_TILE or height != OUTPUT_TILE:
        raise ValueError(f"{path}: expected 48x48 donor frames")
    if len(frames) < columns * rows:
        raise ValueError(f"{path}: insufficient frames")
    result = np.zeros((rows * AUTHOR_TILE, columns * AUTHOR_TILE), dtype=np.uint8)
    domain = np.zeros_like(result, dtype=bool)
    for index in occupied:
        frame = np.frombuffer(frames[index], dtype=np.uint8).reshape(height, width)
        author = frame[0::UPSCALE, 0::UPSCALE]
        row, column = divmod(index, columns)
        y, x = row * AUTHOR_TILE, column * AUTHOR_TILE
        result[y : y + AUTHOR_TILE, x : x + AUTHOR_TILE] = author
        domain[y : y + AUTHOR_TILE, x : x + AUTHOR_TILE] = True
    return result, domain


def author_frame(path: Path, frame_number: int) -> np.ndarray:
    width, height, frames = read_shptd(path)
    if width != OUTPUT_TILE or height != OUTPUT_TILE:
        raise ValueError(f"{path}: expected 48x48 material frame")
    frame = np.frombuffer(frames[frame_number], dtype=np.uint8).reshape(height, width)
    return frame[0::UPSCALE, 0::UPSCALE]


def source_index_set(path: Path) -> set[int]:
    _, _, frames = read_shptd(path)
    result: set[int] = set()
    for frame in frames:
        result.update(frame)
    return result


def inset_water_mask(
    water: np.ndarray,
    inset: float,
    edge_lock: float,
    edge_taper: float,
) -> tuple[np.ndarray, dict[str, object]]:
    if inset < 0.0 or edge_lock < 0.0 or edge_taper < 0.0:
        raise ValueError("water inset and edge controls must be nonnegative")
    if inset == 0.0:
        return water.copy(), {
            "author_pixels": 0.0,
            "output_pixels": 0.0,
            "edge_lock_author_pixels": edge_lock,
            "edge_taper_author_pixels": edge_taper,
            "water_pixels_removed": 0,
            "ground_pixels_gained": 0,
            "external_edge_contacts_preserved": True,
        }
    height, width = water.shape
    yy, xx = np.indices(water.shape)
    canvas_edge_distance = np.minimum.reduce(
        (xx, yy, width - 1 - xx, height - 1 - yy)
    ).astype(np.float32)
    if edge_taper == 0.0:
        edge_weight = (canvas_edge_distance > edge_lock).astype(np.float32)
    else:
        edge_weight = smoothstep_array(
            edge_lock,
            edge_lock + edge_taper,
            canvas_edge_distance,
        )
    effective_inset = inset * edge_weight
    inside_distance = ndimage.distance_transform_edt(water)
    result = water & (inside_distance > effective_inset)
    # Exact donor contacts are locked on every exposed canvas edge.
    result[0, :] = water[0, :]
    result[-1, :] = water[-1, :]
    result[:, 0] = water[:, 0]
    result[:, -1] = water[:, -1]
    removed = int(np.count_nonzero(water & ~result))
    return result, {
        "author_pixels": inset,
        "output_pixels": inset * UPSCALE,
        "edge_lock_author_pixels": edge_lock,
        "edge_taper_author_pixels": edge_taper,
        "water_pixels_removed": removed,
        "ground_pixels_gained": removed,
        "external_edge_contacts_preserved": bool(
            np.array_equal(result[0, :], water[0, :])
            and np.array_equal(result[-1, :], water[-1, :])
            and np.array_equal(result[:, 0], water[:, 0])
            and np.array_equal(result[:, -1], water[:, -1])
        ),
    }


def repeat_tile(tile: np.ndarray, columns: int, rows: int) -> np.ndarray:
    return np.tile(tile, (rows, columns))


def palette_rgb(indices: np.ndarray, palette: np.ndarray) -> np.ndarray:
    return palette[np.asarray(indices, dtype=np.uint8)]


def donor_liquid(donor_rgb: np.ndarray, water: np.ndarray) -> tuple[np.ndarray, dict[str, object]]:
    donor = donor_rgb.astype(np.float32)
    _, nearest = ndimage.distance_transform_edt(~water, return_indices=True)
    filled = donor.copy()
    filled[~water] = donor[nearest[0][~water], nearest[1][~water]]
    luma = 0.2126 * filled[:, :, 0] + 0.7152 * filled[:, :, 1] + 0.0722 * filled[:, :, 2]
    local = ndimage.gaussian_filter(luma, sigma=1.35, mode="nearest")
    weave = np.clip(0.5 + (luma - local) / 36.0, 0.0, 1.0)
    broad = ndimage.gaussian_filter(luma, sigma=0.75, mode="nearest")
    low, high = np.percentile(broad[water], (4.0, 97.0))
    normalized = np.clip((broad - low) / max(1.0, high - low), 0.0, 1.0)
    heat = np.clip(0.20 + 0.52 * normalized + 0.18 * weave, 0.0, 1.0)
    cool = np.asarray((205.0, 45.0, 5.0), dtype=np.float32)
    middle = np.asarray((255.0, 119.0, 8.0), dtype=np.float32)
    hot = np.asarray((255.0, 218.0, 76.0), dtype=np.float32)
    first = np.clip(heat / 0.57, 0.0, 1.0)[:, :, None]
    second = np.clip((heat - 0.57) / 0.43, 0.0, 1.0)[:, :, None]
    result = cool * (1.0 - first) + middle * first
    result = result * (1.0 - second) + hot * second

    spot_cutoff = float(np.quantile(normalized[water], 0.055))
    spot_candidates = water & (normalized <= spot_cutoff) & (
        luma <= ndimage.minimum_filter(luma, size=3)
    )
    spots = select_sparse_seeds(spot_candidates, normalized, 14, 4.0)
    spot_weight = ndimage.gaussian_filter(spots.astype(np.float32), sigma=0.5)
    spot_weight = np.clip(spot_weight * 1.35, 0.0, 0.65)[:, :, None]
    orange = np.asarray((220.0, 55.0, 4.0), dtype=np.float32)
    result = result * (1.0 - spot_weight) + orange * spot_weight
    result[~water] = 0.0
    return np.clip(np.rint(result), 0, 255).astype(np.uint8), {
        "source": "raw Temperate donor luminance and local woven detail",
        "warm_color": [205, 45, 5],
        "middle_color": [255, 119, 8],
        "hot_color": [255, 218, 76],
        "orange_spot_seeds": int(np.count_nonzero(spots)),
    }


def dry_cracked(indices: np.ndarray, hot_rgb: np.ndarray) -> np.ndarray:
    result = hot_rgb.copy()
    crack = indices >= 48
    core = indices >= 81
    _, nearest = ndimage.distance_transform_edt(crack, return_indices=True)
    plate = hot_rgb[nearest[0], nearest[1]].astype(np.float32)
    shoulder_color = np.clip(plate * 0.74 + (4.0, 4.0, 5.0), 0.0, 255.0)
    core_color = np.clip(plate * 0.58 + (5.0, 5.0, 6.0), 0.0, 255.0)
    result[crack] = np.rint(shoulder_color[crack]).astype(np.uint8)
    result[core] = np.rint(core_color[core]).astype(np.uint8)
    return result


def donor_cooled_islands(water: np.ndarray, donor_rgb: np.ndarray) -> np.ndarray:
    luma = 0.2126 * donor_rgb[:, :, 0] + 0.7152 * donor_rgb[:, :, 1] + 0.0722 * donor_rgb[:, :, 2]
    cutoff = float(np.quantile(luma[water], 0.018))
    candidates = water & (luma <= cutoff) & (
        luma <= ndimage.minimum_filter(luma, size=5)
    )
    seeds = select_sparse_seeds(candidates, luma, 5, 10.0)
    islands = np.zeros_like(water)
    for y, x in np.argwhere(seeds):
        shape = disk(1).copy()
        y0, y1 = max(0, y - 1), min(water.shape[0], y + 2)
        x0, x1 = max(0, x - 1), min(water.shape[1], x + 2)
        sy0, sx0 = y0 - (y - 1), x0 - (x - 1)
        sy1, sx1 = sy0 + (y1 - y0), sx0 + (x1 - x0)
        islands[y0:y1, x0:x1] |= shape[sy0:sy1, sx0:sx1]
    return islands & water


def select_sparse_seeds(
    candidates: np.ndarray,
    score: np.ndarray,
    maximum: int,
    minimum_distance: float,
) -> np.ndarray:
    selected = np.zeros_like(candidates)
    coordinates = sorted(
        ((float(score[y, x]), int(y), int(x)) for y, x in np.argwhere(candidates)),
        key=lambda item: item[0],
    )
    points: list[tuple[int, int]] = []
    for _, y, x in coordinates:
        if any(math.hypot(x - px, y - py) < minimum_distance for py, px in points):
            continue
        selected[y, x] = True
        points.append((y, x))
        if len(points) >= maximum:
            break
    return selected


def disk(radius: int) -> np.ndarray:
    y, x = np.ogrid[-radius : radius + 1, -radius : radius + 1]
    return (x * x + y * y) <= radius * radius


def smoothstep_array(low: float, high: float, values: np.ndarray) -> np.ndarray:
    if high <= low:
        return (values >= high).astype(np.float32)
    t = np.clip((values - low) / (high - low), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def quantize(rgb: np.ndarray, palette: np.ndarray) -> tuple[Image.Image, np.ndarray]:
    source = rgb.astype(np.int32)
    colors = palette.astype(np.int32)
    indices = np.empty(source.shape[:2], dtype=np.uint8)
    for y in range(source.shape[0]):
        delta = source[y, :, None, :] - colors[None, :, :]
        distance = 2 * delta[:, :, 0] ** 2 + 4 * delta[:, :, 1] ** 2 + delta[:, :, 2] ** 2
        indices[y] = np.argmin(distance, axis=1).astype(np.uint8)
    return Image.fromarray(palette[indices], mode="RGB"), indices


def upscale(values: np.ndarray) -> np.ndarray:
    return np.repeat(np.repeat(values, UPSCALE, axis=0), UPSCALE, axis=1)


def split_frames(
    composite: np.ndarray, columns: int, rows: int, occupied: set[int]
) -> list[bytes]:
    blank = bytes(OUTPUT_TILE * OUTPUT_TILE)
    frames: list[bytes] = []
    for index in range(columns * rows):
        if index not in occupied:
            frames.append(blank)
            continue
        row, column = divmod(index, columns)
        frame = composite[
            row * OUTPUT_TILE : (row + 1) * OUTPUT_TILE,
            column * OUTPUT_TILE : (column + 1) * OUTPUT_TILE,
        ]
        frames.append(frame.tobytes())
    return frames


def verify_roundtrip(path: Path, frames: list[bytes]) -> None:
    width, height, decoded = read_shptd(path)
    if (width, height, decoded) != (OUTPUT_TILE, OUTPUT_TILE, frames):
        raise ValueError(f"{path}: SHP roundtrip mismatch")


def strict_2x(values: np.ndarray) -> bool:
    anchor = values[0::2, 0::2]
    return bool(
        np.array_equal(anchor, values[1::2, 0::2])
        and np.array_equal(anchor, values[0::2, 1::2])
        and np.array_equal(anchor, values[1::2, 1::2])
    )


def material_preview(
    domain: np.ndarray,
    water: np.ndarray,
    dry: np.ndarray,
    hot: np.ndarray,
    islands: np.ndarray,
) -> Image.Image:
    result = np.zeros((*domain.shape, 3), dtype=np.uint8)
    result[:] = BACKGROUND
    result[domain] = (40, 39, 40)
    result[dry] = (105, 104, 106)
    result[hot] = (166, 61, 22)
    result[water] = (255, 132, 16)
    result[islands] = (24, 21, 22)
    return Image.fromarray(result, mode="RGB")


def water_mask_preview(domain: np.ndarray, water: np.ndarray) -> Image.Image:
    result = np.zeros((*domain.shape, 3), dtype=np.uint8)
    result[:] = BACKGROUND
    result[domain] = (48, 47, 49)
    result[water] = (255, 128, 12)
    return Image.fromarray(result, mode="RGB")


def write_review(
    path: Path,
    donor: Image.Image,
    material: Image.Image,
    author: Image.Image,
    output: Image.Image,
    template: str,
) -> None:
    panel = 432
    header = 34
    canvas = Image.new("RGB", (panel * 4, panel + header), BACKGROUND)
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    entries = (
        (f"Raw Temperate donor {template} (24px density)", donor),
        ("New semantic materials", material),
        ("New Volcanic delta author 24px", author),
        ("Exact 2x output preview", output),
    )
    for index, (label, image) in enumerate(entries):
        x = index * panel
        draw.text((x + 7, 10), label, fill="white", font=font)
        scale = min(panel / image.width, panel / image.height)
        width, height = round(image.width * scale), round(image.height * scale)
        shown = image.resize((width, height), Image.Resampling.NEAREST)
        canvas.paste(shown, (x + (panel - width) // 2, header + (panel - height) // 2))
    canvas.save(path)


def write_pair_review(
    path: Path,
    donor: Image.Image,
    candidate: Image.Image,
    template: str,
) -> None:
    scale = 4
    header = 34
    panel_width = donor.width * scale
    panel_height = donor.height * scale
    canvas = Image.new("RGB", (panel_width * 2, panel_height + header), BACKGROUND)
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    for index, (label, image) in enumerate(
        (
            (f"Raw Temperate donor {template}", donor),
            (f"New from-scratch Volcanic delta {template}", candidate),
        )
    ):
        x = index * panel_width
        draw.text((x + 7, 10), label, fill="white", font=font)
        canvas.paste(
            image.resize((panel_width, panel_height), Image.Resampling.NEAREST),
            (x, header),
        )
    canvas.save(path)


if __name__ == "__main__":
    raise SystemExit(main())
