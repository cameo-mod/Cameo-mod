#!/usr/bin/env python
"""Scan Temperate shore donors for possible decoration placement anchors.

This is deliberately placement-only.  Donor pixels are used to refine bounds
and bottom-center anchors, but are never copied into volcanic art.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

import generate_sh04_alpha_beach_prototype as shore


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUT_DIR = Path.home() / "Documents/agents/volcanic-theater/shorelines/workbench"
DEFAULT_SEED_FILE = ROOT / "tools/tilesets/ra_temperate_shore_decoration_anchor_seeds.json"
STONE_SEED_INDICES = tuple(range(128, 144)) + tuple(range(249, 255))
GROUP_RADIUS = 6


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--first", type=int, default=1)
    parser.add_argument("--last", type=int, default=9)
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    parser.add_argument("--anchor-seeds", type=Path, default=DEFAULT_SEED_FILE)
    args = parser.parse_args()
    if args.first < 1 or args.last < args.first or args.last > 54:
        raise ValueError("template range must satisfy 1 <= first <= last <= 54")

    out_dir = args.out_dir.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    learned_indices = learn_rock_palette_indices()
    ground_indices = shore.source_indices(ROOT / "mods/cameo/bits/temp/clear1.tem")
    water_indices = shore.source_indices(ROOT / "mods/cameo/bits/temp/w1.tem")
    water_indices |= shore.source_indices(ROOT / "mods/cameo/bits/temp/w2.tem")
    palette = shore.read_palette(
        ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    )
    reviewed_seeds: dict[str, list[dict[str, object]]] = {}
    if args.anchor_seeds.is_file():
        reviewed_seeds = json.loads(
            args.anchor_seeds.read_text(encoding="utf-8")
        )["templates"]

    templates: list[dict[str, object]] = []
    panels: list[tuple[str, Image.Image]] = []
    total_candidates = 0
    for number in range(args.first, args.last + 1):
        template = f"sh{number:02d}"
        record, overlay = scan_template(
            template,
            learned_indices,
            ground_indices,
            water_indices,
            palette,
            reviewed_seeds.get(template),
        )
        templates.append(record)
        candidate_count = len(record["candidates"])
        total_candidates += candidate_count
        panels.append(
            (
                f"{template}: {candidate_count} possible anchor(s)",
                overlay,
            )
        )

    range_name = f"sh{args.first:02d}_sh{args.last:02d}"
    manifest = {
        "preview_only": True,
        "placement_only": True,
        "donor_pixels_copied": False,
        "coordinate_space": "sparse composite image pixels",
        "anchor_definition": "bottom-center of detected donor decoration bounds",
        "reviewed_seed_file": str(args.anchor_seeds.resolve()),
        "placement_rules": {
            "ground": "clean_basalt_no_pool_no_glow",
            "offshore": "glowing_basalt_9px_with_unified_pool_and_feeders",
            "shoreline": "resolve_from_volcanic_base_support_pixels",
        },
        "group_radius_pixels": GROUP_RADIUS,
        "learned_rock_palette_indices": learned_indices,
        "summary": {
            "template_count": len(templates),
            "possible_anchor_count": total_candidates,
            "templates_with_candidates": sum(
                bool(record["candidates"]) for record in templates
            ),
            "all_candidates_require_human_review": True,
        },
        "templates": templates,
    }
    manifest_path = out_dir / f"donor_decoration_anchor_manifest_{range_name}.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    review_path = out_dir / f"donor_decoration_anchor_review_{range_name}.png"
    shore.write_review_sheet(review_path, panels, columns=3, scale=2)
    print(review_path.resolve())
    print(manifest_path.resolve())
    print(json.dumps(manifest["summary"], indent=2))
    return 0


def scan_template(
    template: str,
    learned_indices: list[int],
    ground_indices: set[int],
    water_indices: set[int],
    palette: list[tuple[int, int, int]],
    reviewed_seeds: list[dict[str, object]] | None,
) -> tuple[dict[str, object], Image.Image]:
    spec = shore.read_template_spec(
        ROOT / "mods/cameo/tilesets/ra_temperat.yaml",
        f"{template}.tem",
    )
    donor, domain = shore.read_sparse_composite(
        ROOT / "mods/cameo/bits/temp" / spec.image,
        spec,
    )
    donor_rgb = shore.indices_rgb(donor, palette)
    donor_rgb[~domain] = shore.BACKGROUND
    rock_subtiles = sorted(
        index for index, terrain in spec.terrain.items() if terrain == "Rock"
    )
    rock_roi = subtile_mask(spec, rock_subtiles, donor.shape) & domain
    core = detect_rock_core(donor, domain, rock_roi, learned_indices)
    auto_candidates = grouped_candidates(
        core,
        rock_roi,
        donor,
        domain,
        spec,
        rock_subtiles,
        ground_indices,
        water_indices,
    )
    if rock_subtiles and not auto_candidates:
        auto_candidates = fallback_candidates(
            spec,
            rock_subtiles,
            donor.shape,
            domain,
        )
    candidates = (
        reviewed_candidates(reviewed_seeds)
        if reviewed_seeds is not None
        else auto_candidates
    )

    overlay = anchor_overlay(
        donor_rgb,
        domain,
        spec,
        rock_subtiles,
        candidates,
    )
    return (
        {
            "template": template,
            "image": spec.image,
            "columns": spec.columns,
            "rows": spec.rows,
            "composite_width": int(donor.shape[1]),
            "composite_height": int(donor.shape[0]),
            "rock_subtiles": rock_subtiles,
            "detected_core_pixels": int(np.count_nonzero(core)),
            "auto_detected_candidate_count": len(auto_candidates),
            "candidate_source": (
                "visual_review_seed"
                if reviewed_seeds is not None
                else "automatic_detector"
            ),
            "candidates": candidates,
        },
        overlay,
    )


def reviewed_candidates(
    seeds: list[dict[str, object]],
) -> list[dict[str, object]]:
    result: list[dict[str, object]] = []
    family = {
        "small": "single_or_paired_columns",
        "medium": "medium_column_cluster",
        "large": "approved_large_column_cluster",
    }
    for index, seed in enumerate(seeds, start=1):
        left, top, right, bottom = [int(value) for value in seed["bounds"]]
        anchor_x, anchor_y = [int(value) for value in seed["anchor"]]
        size_class = str(seed["size_class"])
        result.append(
            {
                "id": f"A{index:02d}",
                "status": "review",
                "source": "visual_review_seed",
                "anchor": {"x": anchor_x, "y": anchor_y},
                "bounds": {
                    "left": left,
                    "top": top,
                    "right": right,
                    "bottom": bottom,
                    "width": right - left + 1,
                    "height": bottom - top + 1,
                },
                "terrain_phase": str(seed["terrain_phase"]),
                "placement_treatment": str(
                    seed.get(
                        "placement_treatment",
                        placement_treatment(str(seed["terrain_phase"])),
                    )
                ),
                "size_class": size_class,
                "suggested_asset_family": family[size_class],
                "confidence": float(seed.get("confidence", 0.78)),
                "note": str(seed.get("note", "")),
            }
        )
    return result


def detect_rock_core(
    donor: np.ndarray,
    domain: np.ndarray,
    rock_roi: np.ndarray,
    learned_indices: list[int],
) -> np.ndarray:
    if not np.any(rock_roi):
        return np.zeros(donor.shape, dtype=bool)
    search_roi = ndimage.binary_dilation(
        rock_roi,
        structure=shore.disk(5),
    ) & domain
    seed = search_roi & np.isin(donor, STONE_SEED_INDICES)
    seed = remove_small_components(seed, minimum_pixels=3)
    seed_distance = ndimage.distance_transform_edt(~seed)
    body = (
        search_roi
        & (seed_distance <= 9.0)
        & np.isin(donor, learned_indices)
    )
    body |= seed
    body = ndimage.binary_closing(body, structure=shore.disk(1))
    core = keep_seeded_components(body, seed, minimum_pixels=12)
    core = ndimage.binary_closing(core, structure=shore.disk(1))
    core = ndimage.binary_fill_holes(core)
    core = remove_small_components(core, minimum_pixels=12)
    return core & domain


def grouped_candidates(
    core: np.ndarray,
    rock_roi: np.ndarray,
    donor: np.ndarray,
    domain: np.ndarray,
    spec: shore.TemplateSpec,
    rock_subtiles: list[int],
    ground_indices: set[int],
    water_indices: set[int],
) -> list[dict[str, object]]:
    if not np.any(core):
        return []
    joined = ndimage.binary_dilation(
        core,
        structure=shore.disk(GROUP_RADIUS),
    )
    labels, count = ndimage.label(
        joined,
        structure=np.ones((3, 3), dtype=np.uint8),
    )
    domain_clearance = ndimage.distance_transform_edt(domain)
    result: list[dict[str, object]] = []
    for component in range(1, count + 1):
        component_mask = core & (labels == component)
        ys, xs = np.where(component_mask)
        if xs.size < 20:
            continue
        left = int(xs.min())
        top = int(ys.min())
        right = int(xs.max())
        bottom = int(ys.max())
        anchor_x = int(round((left + right) * 0.5))
        anchor_y = bottom
        width = right - left + 1
        height = bottom - top + 1
        size_class = decoration_size_class(width, height, int(xs.size))
        semantic_overlap = int(np.count_nonzero(component_mask & rock_roi))
        confidence = 0.64
        if semantic_overlap:
            confidence += 0.16
        if xs.size >= 80:
            confidence += 0.08
        if width > 72:
            confidence -= 0.14
        clearance = float(domain_clearance[anchor_y, anchor_x])
        if clearance < 4.0:
            confidence -= 0.12
        confidence = float(np.clip(confidence, 0.25, 0.95))
        result.append(
            {
                "id": "",
                "status": "review",
                "anchor": {"x": anchor_x, "y": anchor_y},
                "bounds": {
                    "left": left,
                    "top": top,
                    "right": right,
                    "bottom": bottom,
                    "width": width,
                    "height": height,
                },
                "detected_pixels": int(xs.size),
                "rock_semantic_overlap_pixels": semantic_overlap,
                "rock_subtiles_touched": touched_subtiles(
                    component_mask,
                    spec,
                    rock_subtiles,
                ),
                "terrain_phase": support_phase(
                    donor,
                    domain,
                    anchor_x,
                    anchor_y,
                    width,
                    ground_indices,
                    water_indices,
                ),
                "domain_edge_clearance_pixels": round(clearance, 2),
                "size_class": size_class,
                "suggested_asset_family": {
                    "small": "single_or_paired_columns",
                    "medium": "medium_column_cluster",
                    "large": "approved_large_column_cluster",
                }[size_class],
                "confidence": round(confidence, 2),
            }
        )
        result[-1]["placement_treatment"] = placement_treatment(
            str(result[-1]["terrain_phase"])
        )
    result.sort(key=lambda item: (item["anchor"]["y"], item["anchor"]["x"]))
    for index, candidate in enumerate(result, start=1):
        candidate["id"] = f"A{index:02d}"
    return result


def fallback_candidates(
    spec: shore.TemplateSpec,
    rock_subtiles: list[int],
    shape: tuple[int, int],
    domain: np.ndarray,
) -> list[dict[str, object]]:
    result: list[dict[str, object]] = []
    for ordinal, subtile in enumerate(rock_subtiles, start=1):
        cell_x = subtile % spec.columns
        cell_y = subtile // spec.columns
        left = cell_x * shore.TILE
        top = cell_y * shore.TILE
        right = min(shape[1] - 1, left + shore.TILE - 1)
        bottom = min(shape[0] - 1, top + shore.TILE - 1)
        anchor_x = (left + right) // 2
        anchor_y = bottom - 4
        if not domain[anchor_y, anchor_x]:
            valid = np.argwhere(domain[top : bottom + 1, left : right + 1])
            if valid.size:
                anchor_y = int(top + valid[:, 0].max())
                anchor_x = int(left + round(float(np.median(valid[:, 1]))))
        result.append(
            {
                "id": f"A{ordinal:02d}",
                "status": "review",
                "fallback": True,
                "anchor": {"x": anchor_x, "y": anchor_y},
                "bounds": {
                    "left": left,
                    "top": top,
                    "right": right,
                    "bottom": bottom,
                    "width": right - left + 1,
                    "height": bottom - top + 1,
                },
                "rock_subtiles_touched": [subtile],
                "terrain_phase": "unknown",
                "placement_treatment": "resolve_from_volcanic_base_support_pixels",
                "size_class": "medium",
                "suggested_asset_family": "medium_column_cluster",
                "confidence": 0.35,
            }
        )
    return result


def placement_treatment(terrain_phase: str) -> str:
    if terrain_phase == "ground":
        return "clean_basalt_no_pool_no_glow"
    if terrain_phase == "offshore":
        return "glowing_basalt_9px_with_unified_pool_and_feeders"
    return "resolve_from_volcanic_base_support_pixels"


def anchor_overlay(
    donor_rgb: np.ndarray,
    domain: np.ndarray,
    spec: shore.TemplateSpec,
    rock_subtiles: list[int],
    candidates: list[dict[str, object]],
) -> Image.Image:
    image = Image.fromarray(donor_rgb, mode="RGB")
    draw = ImageDraw.Draw(image)
    for subtile in rock_subtiles:
        cell_x = subtile % spec.columns
        cell_y = subtile // spec.columns
        box = (
            cell_x * shore.TILE,
            cell_y * shore.TILE,
            (cell_x + 1) * shore.TILE - 1,
            (cell_y + 1) * shore.TILE - 1,
        )
        draw.rectangle(box, outline=(90, 174, 228), width=1)
    colors = {
        "small": (84, 222, 214),
        "medium": (244, 204, 70),
        "large": (235, 106, 184),
    }
    for candidate in candidates:
        bounds = candidate["bounds"]
        color = colors[candidate["size_class"]]
        draw.rectangle(
            (
                bounds["left"],
                bounds["top"],
                bounds["right"],
                bounds["bottom"],
            ),
            outline=color,
            width=2,
        )
        anchor = candidate["anchor"]
        x = anchor["x"]
        y = anchor["y"]
        draw.line((x - 4, y, x + 4, y), fill=(255, 255, 255), width=1)
        draw.line((x, y - 4, x, y + 4), fill=(255, 255, 255), width=1)
        draw.text((x + 3, max(0, y - 11)), candidate["id"], fill=(255, 255, 255))
    if not candidates:
        draw.text((4, 4), "No decoration anchors", fill=(235, 235, 235))
    return image


def support_phase(
    donor: np.ndarray,
    domain: np.ndarray,
    anchor_x: int,
    anchor_y: int,
    footprint_width: int,
    ground_indices: set[int],
    water_indices: set[int],
) -> str:
    half_width = max(4, min(18, footprint_width // 3))
    left = max(0, anchor_x - half_width)
    right = min(donor.shape[1], anchor_x + half_width + 1)
    top = max(0, anchor_y + 1)
    bottom = min(donor.shape[0], anchor_y + 10)
    support = np.zeros(donor.shape, dtype=bool)
    support[top:bottom, left:right] = True
    support &= domain
    ground = int(np.count_nonzero(support & np.isin(donor, list(ground_indices))))
    water = int(np.count_nonzero(support & np.isin(donor, list(water_indices))))
    if water >= max(3, round(ground * 1.25)):
        return "offshore"
    if ground >= max(3, round(water * 1.25)):
        return "ground"
    return "shoreline"


def decoration_size_class(width: int, height: int, pixels: int) -> str:
    if max(width, height) <= 20 and pixels < 140:
        return "small"
    if max(width, height) <= 48 and pixels < 420:
        return "medium"
    return "large"


def touched_subtiles(
    mask: np.ndarray,
    spec: shore.TemplateSpec,
    rock_subtiles: list[int],
) -> list[int]:
    result: list[int] = []
    for subtile in rock_subtiles:
        cell_x = subtile % spec.columns
        cell_y = subtile // spec.columns
        cell = mask[
            cell_y * shore.TILE : (cell_y + 1) * shore.TILE,
            cell_x * shore.TILE : (cell_x + 1) * shore.TILE,
        ]
        if np.any(cell):
            result.append(subtile)
    return result


def subtile_mask(
    spec: shore.TemplateSpec,
    indices: list[int],
    shape: tuple[int, int],
) -> np.ndarray:
    result = np.zeros(shape, dtype=bool)
    for index in indices:
        cell_x = index % spec.columns
        cell_y = index // spec.columns
        result[
            cell_y * shore.TILE : (cell_y + 1) * shore.TILE,
            cell_x * shore.TILE : (cell_x + 1) * shore.TILE,
        ] = True
    return result


def remove_small_components(mask: np.ndarray, minimum_pixels: int) -> np.ndarray:
    labels, count = ndimage.label(
        mask,
        structure=np.ones((3, 3), dtype=np.uint8),
    )
    if count == 0:
        return mask.copy()
    sizes = np.bincount(labels.ravel())
    keep = sizes >= minimum_pixels
    keep[0] = False
    return keep[labels]


def keep_seeded_components(
    candidate: np.ndarray,
    seed: np.ndarray,
    minimum_pixels: int,
) -> np.ndarray:
    labels, count = ndimage.label(
        candidate,
        structure=np.ones((3, 3), dtype=np.uint8),
    )
    if count == 0:
        return candidate.copy()
    sizes = np.bincount(labels.ravel())
    seeded = np.bincount(
        labels.ravel(),
        weights=seed.ravel(),
        minlength=count + 1,
    ) > 0
    keep = seeded & (sizes >= minimum_pixels)
    keep[0] = False
    return keep[labels]


def learn_rock_palette_indices() -> list[int]:
    tileset = ROOT / "mods/cameo/tilesets/ra_temperat.yaml"
    bits = ROOT / "mods/cameo/bits/temp"
    rock_counts = np.zeros(256, dtype=np.int64)
    other_counts = np.zeros(256, dtype=np.int64)
    rock_pixels = 0
    other_pixels = 0
    for number in range(1, 55):
        template = f"sh{number:02d}"
        spec = shore.read_template_spec(tileset, f"{template}.tem")
        donor, domain = shore.read_sparse_composite(bits / spec.image, spec)
        rock_subtiles = sorted(
            index for index, terrain in spec.terrain.items() if terrain == "Rock"
        )
        rock_roi = subtile_mask(spec, rock_subtiles, donor.shape) & domain
        other_roi = domain & ~rock_roi
        rock_counts += np.bincount(donor[rock_roi], minlength=256)
        other_counts += np.bincount(donor[other_roi], minlength=256)
        rock_pixels += int(np.count_nonzero(rock_roi))
        other_pixels += int(np.count_nonzero(other_roi))

    rock_frequency = (rock_counts + 1) / (rock_pixels + 256)
    other_frequency = (other_counts + 1) / (other_pixels + 256)
    odds = rock_frequency / other_frequency
    learned = {
        index
        for index in range(256)
        if rock_counts[index] >= 20 and odds[index] >= 8.0
    }
    learned.update(STONE_SEED_INDICES)
    return sorted(learned)


if __name__ == "__main__":
    raise SystemExit(main())
