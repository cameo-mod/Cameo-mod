#!/usr/bin/env python
"""Preview a four-phase volcanic shoreline using approved materials."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

import generate_sh04_alpha_beach_prototype as shore


ROOT = Path(__file__).resolve().parents[2]
BACKGROUND = (67, 78, 88)
MANUAL_DELTA_ROOT = (
    Path.home()
    / "Documents/agents/volcanic-theater/shorelines/manual-river-deltas/projects"
)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--template", default="sh04")
    parser.add_argument(
        "--liquid-w1",
        type=Path,
        default=Path.home()
        / "Documents/agents/volcanic-theater/liquid-lava-water/prototype-01/w1-river-reuse-preview.vol",
    )
    parser.add_argument(
        "--out-dir",
        type=Path,
        default=Path.home()
        / "Documents/agents/volcanic-theater/liquid-lava-water/shoreline-four-phase-sh04",
    )
    args = parser.parse_args()
    out_dir = args.out_dir.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    volcanic_bits = ROOT / "mods/cameo/bits/volcanic"
    temperate_bits = ROOT / "mods/cameo/bits/temp"
    volcanic_palette = shore.read_palette(volcanic_bits / "volcanic.pal")
    temperate_palette = shore.read_palette(
        ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    )
    spec = shore.read_template_spec(
        ROOT / "mods/cameo/tilesets/ra_temperat.yaml", f"{args.template}.tem"
    )
    donor, domain = shore.read_sparse_composite(temperate_bits / spec.image, spec)
    donor_rgb = shore.indices_rgb(donor, temperate_palette)
    ground_source = shore.source_indices(temperate_bits / "clear1.tem")
    water_source = shore.source_indices(temperate_bits / "w1.tem") | shore.source_indices(
        temperate_bits / "w2.tem"
    )
    raw_ground = domain & np.isin(donor, list(ground_source))
    raw_lava = domain & np.isin(donor, list(water_source))
    seed_mask, _ = shore.beach_color_seed(
        donor, temperate_palette, ground_source, water_source
    )
    seed_mask &= domain
    beach, mask_metrics = unsmoothed_semantic_beach_region(
        seed_mask, domain, raw_ground, raw_lava, spec
    )
    free_phase = domain & ~beach
    authoritative_ground = raw_ground & free_phase
    authoritative_lava = raw_lava & free_phase
    if not np.any(authoritative_ground) or not np.any(authoritative_lava):
        raise ValueError(f"{args.template}: donor does not expose both ground and water")
    donor_ground_distance = ndimage.distance_transform_edt(~authoritative_ground)
    donor_lava_distance = ndimage.distance_transform_edt(~authoritative_lava)
    lava = free_phase & (donor_lava_distance < donor_ground_distance)
    ground = free_phase & ~lava
    # Exact donor seeds override the nearest-phase fallback.
    ground[authoritative_ground] = True
    lava[authoritative_ground] = False
    lava[authoritative_lava] = True
    ground[authoritative_lava] = False
    lava_selector = lava | (
        beach & (donor_lava_distance < donor_ground_distance)
    )

    height, width = donor.shape
    clear_indices = shore.tile_frame(
        shore.unique_frame(volcanic_bits / "clear1.vol", expected_frames=16),
        width,
        height,
    )
    cracked_indices = shore.tile_frame(
        shore.unique_frame(volcanic_bits / "w1.vol", expected_frames=1),
        width,
        height,
    )
    liquid_indices = shore.tile_frame(
        shore.unique_frame(args.liquid_w1.resolve(), expected_frames=1),
        width,
        height,
    )
    clear_rgb = shore.indices_rgb(clear_indices, volcanic_palette)
    cracked_hot = shore.indices_rgb(cracked_indices, volcanic_palette)
    liquid_rgb = shore.indices_rgb(liquid_indices, volcanic_palette)
    cracked_dry = dry_cracked_material(cracked_indices, cracked_hot)

    ground_distance = ndimage.distance_transform_edt(~ground)
    lava_distance = ndimage.distance_transform_edt(~lava)
    progress = ground_distance / np.maximum(ground_distance + lava_distance, 1e-6)
    progress = np.clip(progress, 0.0, 1.0)

    # Heat only fissures that can be traced back to the actual liquid phase.
    # A simple distance band lights unrelated Voronoi junctions and creates
    # detached molten pockets, especially where a shoreline touches two edges.
    crack_support = ndimage.binary_dilation(cracked_indices >= 48, structure=shore.disk(1))
    traversable = crack_support & (beach | lava)
    seeds = crack_support & lava
    crack_distance = shore.geodesic_crack_distance(traversable, seeds)
    connected_heat = np.zeros_like(progress, dtype=np.float32)
    connected = np.isfinite(crack_distance)
    connected_heat[connected] = 1.0 - smoothstep(0.0, 14.0, crack_distance[connected])
    connected_heat *= smoothstep(0.40, 0.62, progress)
    connected_heat *= beach
    heated_cracks = blend_stage(cracked_dry, cracked_hot, connected_heat)

    material = clear_rgb.astype(np.float32)
    material = blend_stage(material, cracked_dry, smoothstep(0.05, 0.24, progress))
    material = blend_stage(material, heated_cracks, smoothstep(0.30, 0.48, progress))
    material = blend_stage(material, liquid_rgb, smoothstep(0.80, 0.98, progress))

    result = np.where(lava_selector[:, :, None], liquid_rgb, clear_rgb).astype(np.uint8)
    result[beach] = np.clip(np.rint(material[beach]), 0, 255).astype(np.uint8)
    result[~domain] = BACKGROUND

    hard_material = cracked_dry.copy()
    hard_hot = (
        beach
        & (progress >= 0.48)
        & (progress < 0.82)
        & connected
        & (crack_distance <= 14.0)
        & (cracked_indices >= 48)
    )
    hard_material[hard_hot] = cracked_hot[hard_hot]
    hard_liquid = beach & (progress >= 0.82)
    hard_material[hard_liquid] = liquid_rgb[hard_liquid]
    hard_result = np.where(lava_selector[:, :, None], liquid_rgb, clear_rgb).astype(np.uint8)
    hard_result[beach] = hard_material[beach]
    hard_result[~domain] = BACKGROUND

    island_metrics: dict[str, object] = {"preserved": False}
    manual_project = MANUAL_DELTA_ROOT / args.template
    if (manual_project / "project.json").is_file():
        result, hard_result, island_metrics = preserve_manual_delta_features(
            result,
            hard_result,
            manual_project,
            result.shape[:2],
        )
    donor_rgb[~domain] = BACKGROUND

    phase = phase_preview(domain, beach, progress)
    result_image = Image.fromarray(result, mode="RGB")
    hard_result_image = Image.fromarray(hard_result, mode="RGB")
    donor_image = Image.fromarray(donor_rgb, mode="RGB")
    result_image.save(out_dir / f"four_phase_liquid_lava_shore_{args.template}.png")
    hard_result_image.save(
        out_dir / f"four_phase_liquid_lava_shore_no_feather_{args.template}.png"
    )
    phase.save(out_dir / f"four_phase_transition_map_{args.template}.png")
    comparison = comparison_sheet(
        donor_image, phase, result_image, hard_result_image, args.template
    )
    review_path = out_dir / f"temperate_vs_four_phase_volcanic_{args.template}.png"
    comparison.save(review_path)

    audit = {
        "template": args.template,
        "preview_only": True,
        "production_assets_modified": False,
        "transition": [
            "solid basalt ground",
            "dry cracked basalt using old w1 topology",
            "glowing cracked lava using old w1",
            "proper liquid lava using approved river renderer",
        ],
        "domain_pixels": int(np.count_nonzero(domain)),
        "beach_pixels": int(np.count_nonzero(beach)),
        "shoreline_mask": mask_metrics,
        "connected_heated_crack_pixels": int(np.count_nonzero(connected_heat > 0.02)),
        "detached_hot_junctions_allowed": False,
        "manual_delta_islands": island_metrics,
        "no_feather_variant": True,
        "blank_subtile_pixels_preserved": bool(np.all(result[~domain] == BACKGROUND)),
    }
    (out_dir / f"four_phase_transition_audit_{args.template}.json").write_text(
        json.dumps(audit, indent=2), encoding="utf-8"
    )
    print(review_path)
    return 0


def dry_cracked_material(indices: np.ndarray, hot_rgb: np.ndarray) -> np.ndarray:
    result = hot_rgb.copy()
    shoulder = indices >= 48
    core = indices >= 81
    # Shade cracks from their nearest basalt plate instead of painting them
    # fixed black. This keeps the fissures in the ground's local value range.
    _, nearest = ndimage.distance_transform_edt(
        shoulder,
        return_indices=True,
    )
    nearest_plate = hot_rgb[nearest[0], nearest[1]].astype(np.float32)
    shoulder_rgb = np.clip(
        nearest_plate * 0.74 + np.asarray((4.0, 4.0, 5.0)),
        0.0,
        255.0,
    )
    core_rgb = np.clip(
        nearest_plate * 0.58 + np.asarray((5.0, 5.0, 6.0)),
        0.0,
        255.0,
    )
    result[shoulder] = np.rint(shoulder_rgb[shoulder]).astype(np.uint8)
    result[core] = np.rint(core_rgb[core]).astype(np.uint8)
    return result


def preserve_manual_delta_features(
    blended_candidate: np.ndarray,
    hard_candidate: np.ndarray,
    project: Path,
    expected_shape: tuple[int, int],
) -> tuple[np.ndarray, np.ndarray, dict[str, object]]:
    manifest = json.loads((project / "project.json").read_text(encoding="utf-8"))
    cutout = np.asarray(
        Image.open(project / manifest["required_exports"]["lava_cutout"]).convert("RGBA")
    )
    approved = np.asarray(
        Image.open(project / manifest["inputs"]["approved_base"]).convert("RGB")
    )
    if cutout.shape[:2] != expected_shape or approved.shape[:2] != expected_shape:
        raise ValueError(f"{manifest['tile']}: manual delta geometry differs from candidate")
    liquid = cutout[:, :, 3] > 0
    envelope = ndimage.binary_fill_holes(liquid)
    islands = envelope & ~liquid
    labels, count = ndimage.label(islands)
    sizes = np.bincount(labels.ravel())[1:]

    # Keep the approved cracked formations immediately beside the hand-cut
    # river. Only the broad former water field should become uninterrupted
    # proper liquid lava.
    bank_radius = 24.0
    distance_from_river = ndimage.distance_transform_edt(~liquid)
    protected_bank = (~liquid) & (distance_from_river <= bank_radius)
    blended = blended_candidate.copy()
    hard = hard_candidate.copy()
    blended[protected_bank] = approved[protected_bank]
    hard[protected_bank] = approved[protected_bank]

    alpha = cutout[:, :, 3].astype(np.float32) / 255.0
    lava_rgb = cutout[:, :, :3].astype(np.float32)
    blended = np.clip(
        np.rint(
            blended.astype(np.float32) * (1.0 - alpha[:, :, None])
            + lava_rgb * alpha[:, :, None]
        ),
        0,
        255,
    ).astype(np.uint8)
    hard = np.clip(
        np.rint(
            hard.astype(np.float32) * (1.0 - alpha[:, :, None])
            + lava_rgb * alpha[:, :, None]
        ),
        0,
        255,
    ).astype(np.uint8)
    blended[islands] = approved[islands]
    hard[islands] = approved[islands]
    return blended, hard, {
        "preserved": True,
        "source": "approved cracked base bank plus exact manual lava cutout",
        "protected_riverside_crack_radius_pixels": bank_radius,
        "protected_riverside_crack_pixels": int(np.count_nonzero(protected_bank)),
        "island_pixels": int(np.count_nonzero(islands)),
        "island_components": int(count),
        "island_component_sizes": sorted((int(value) for value in sizes), reverse=True),
    }


def unsmoothed_semantic_beach_region(
    seed: np.ndarray,
    domain: np.ndarray,
    ground_seed: np.ndarray,
    lava_seed: np.ndarray,
    spec: shore.TemplateSpec,
) -> tuple[np.ndarray, dict[str, object]]:
    """Select the raw donor beach component without closing or hole filling."""

    labels, count = ndimage.label(seed)
    sizes = np.bincount(labels.ravel())
    candidates: list[tuple[int, int]] = []
    for label_value in range(1, count + 1):
        component = labels == label_value
        adjacent = ndimage.binary_dilation(component, structure=shore.disk(2)) & domain
        if not np.any(adjacent & ground_seed) or not np.any(adjacent & lava_seed):
            continue
        if len(shore.component_subtiles(component, spec)) < 2:
            continue
        if not shore.crossed_internal_subtile_seams(component, spec):
            continue
        candidates.append((int(sizes[label_value]), label_value))
    if not candidates:
        raise ValueError("no raw donor beach component spans ground and lava")
    selected_size, selected_label = max(candidates)
    raw_selected = labels == selected_label
    selected = ndimage.binary_fill_holes(raw_selected) & domain
    return selected, {
        "selection": "largest raw donor semantic component with enclosed texture holes filled",
        "morphological_closing_pixels": 0,
        "outer_contour_smoothing": False,
        "enclosed_texture_holes_filled": True,
        "raw_connected_components": int(count),
        "selected_raw_pixels": selected_size,
        "selected_filled_pixels": int(np.count_nonzero(selected)),
    }


def blend_stage(base: np.ndarray, target: np.ndarray, weight: np.ndarray) -> np.ndarray:
    return base * (1.0 - weight[:, :, None]) + target * weight[:, :, None]


def smoothstep(low: float, high: float, values: np.ndarray) -> np.ndarray:
    t = np.clip((values - low) / (high - low), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def phase_preview(domain: np.ndarray, beach: np.ndarray, progress: np.ndarray) -> Image.Image:
    result = np.zeros((*domain.shape, 3), dtype=np.uint8)
    result[:] = BACKGROUND
    result[domain] = (34, 33, 34)
    result[beach & (progress < 0.43)] = (82, 78, 78)
    result[beach & (progress >= 0.43) & (progress < 0.80)] = (129, 48, 20)
    result[beach & (progress >= 0.80)] = (242, 116, 18)
    return Image.fromarray(result, mode="RGB")


def comparison_sheet(
    donor: Image.Image,
    phase: Image.Image,
    result: Image.Image,
    hard_result: Image.Image,
    template: str,
) -> Image.Image:
    scale = 4
    header = 34
    panel = donor.width * scale
    canvas = Image.new("RGB", (panel * 4, panel + header), BACKGROUND)
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    entries = (
        (f"Temperate donor {template}", donor),
        ("Strict no-feather material boundaries", hard_result),
        ("Four material zones", phase),
        ("Connected cracks + short blending", result),
    )
    for index, (label, image) in enumerate(entries):
        x = index * panel
        draw.text((x + 8, 10), label, fill="white", font=font)
        canvas.paste(
            image.resize((panel, panel), Image.Resampling.NEAREST),
            (x, header),
        )
    return canvas


if __name__ == "__main__":
    raise SystemExit(main())
