#!/usr/bin/env python
"""Preview manifest-driven basalt column placement on volcanic shores."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

import generate_basalt_column_families as family_generator
import generate_basalt_pillar_study as pillars
import generate_molten_pool_study as molten
import generate_sh04_alpha_beach_prototype as shore
import scan_shore_decoration_anchors as anchor_scanner


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_WORKBENCH = Path.home() / "Documents/agents/volcanic-theater/shorelines/workbench"
FAMILY_SCALES = {"tiny": 1.0, "small": 1.0, "medium": 0.82, "large": 0.68}
PLACEMENT_NUDGE_OFFSETS = (
    (0, 0),
    (-4, 0),
    (4, 0),
    (0, -4),
    (0, 4),
    (-8, 0),
    (8, 0),
    (0, -8),
    (0, 8),
    (-12, 0),
    (12, 0),
    (0, -12),
    (0, 12),
)
FAMILY_DOWNGRADE = {
    "large": ("large", "medium", "small", "tiny"),
    "medium": ("medium", "small", "tiny"),
    "small": ("small", "tiny"),
    "tiny": ("tiny",),
}
ASSET_TO_FAMILY = {
    "single_or_paired_columns": "small",
    "medium_column_cluster": "medium",
    "approved_large_column_cluster": "large",
}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--templates",
        nargs="+",
        default=("sh01", "sh04", "sh05"),
    )
    parser.add_argument(
        "--manifest",
        type=Path,
        default=DEFAULT_WORKBENCH / "donor_decoration_anchor_manifest_sh01_sh09.json",
    )
    parser.add_argument("--approved-dir", type=Path, default=DEFAULT_WORKBENCH)
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_WORKBENCH)
    parser.add_argument("--page-size", type=int, default=4)
    args = parser.parse_args()
    if args.page_size < 1:
        raise ValueError("page size must be at least 1")
    out_dir = args.out_dir.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
    by_template = {record["template"]: record for record in manifest["templates"]}
    families = family_generator.build_families()
    sprites = render_family_sprites(families)

    audit_templates: list[dict[str, object]] = []
    review_panels: list[tuple[str, Image.Image]] = []
    for template in args.templates:
        if template not in by_template:
            raise ValueError(f"{template} is absent from the anchor manifest")
        approved_path = args.approved_dir / f"lava_seepage_composite_{template}.png"
        with Image.open(approved_path) as source:
            approved = source.convert("RGB")
        decorated, audit, donor_overlay = place_template(
            template,
            approved,
            by_template[template],
            families,
            sprites,
        )
        decorated_path = out_dir / f"basalt_placement_preview_{template}.png"
        decorated.save(decorated_path)
        audit_templates.append(audit)
        review_panels.extend(
            (
                (f"{template}: donor anchors", donor_overlay),
                (f"{template}: approved undecorated", approved),
                (f"{template}: proposed basalt placement", decorated),
            )
        )

    range_name = "_".join(args.templates)
    review_path = out_dir / f"basalt_placement_review_{range_name}.png"
    shore.write_review_sheet(review_path, review_panels, columns=3, scale=2)
    page_paths: list[Path] = []
    if len(args.templates) > args.page_size:
        for start in range(0, len(args.templates), args.page_size):
            page_templates = args.templates[start : start + args.page_size]
            panel_start = start * 3
            panel_end = panel_start + len(page_templates) * 3
            page_name = "_".join(page_templates)
            page_path = out_dir / f"basalt_placement_review_{page_name}.png"
            shore.write_review_sheet(
                page_path,
                review_panels[panel_start:panel_end],
                columns=3,
                scale=2,
            )
            page_paths.append(page_path)
    audit = {
        "preview_only": True,
        "vol_files_written": False,
        "manifest": str(args.manifest.resolve()),
        "approved_dir": str(args.approved_dir.resolve()),
        "templates": audit_templates,
    }
    audit_path = out_dir / f"basalt_placement_audit_{range_name}.json"
    audit_path.write_text(json.dumps(audit, indent=2), encoding="utf-8")
    print(review_path.resolve())
    for page_path in page_paths:
        print(page_path.resolve())
    print(audit_path.resolve())
    return 0


def render_family_sprites(
    families: dict[str, list[pillars.Column]],
) -> dict[str, dict[str, dict[str, Image.Image]]]:
    result: dict[str, dict[str, dict[str, Image.Image]]] = {}
    for name, columns in families.items():
        result[name] = {}
        for variant, variant_columns in {
            "normal": columns,
            "shorter": family_generator.scale_heights(columns, 0.5),
        }.items():
            clean, _ = family_generator.render_ground_sprite(
                variant_columns,
                family_name=name,
                variant=variant,
            )
            glowing, _, _ = pillars.render_forest(
                variant_columns,
                lava_contact=True,
                include_shadow=True,
            )
            result[name][variant] = {"clean": clean, "glowing": glowing}
    return result


def place_template(
    template: str,
    approved: Image.Image,
    manifest_record: dict[str, object],
    families: dict[str, list[pillars.Column]],
    sprites: dict[str, dict[str, dict[str, Image.Image]]],
) -> tuple[Image.Image, dict[str, object], Image.Image]:
    spec = shore.read_template_spec(
        ROOT / "mods/cameo/tilesets/ra_temperat.yaml",
        f"{template}.tem",
    )
    donor, domain = shore.read_sparse_composite(
        ROOT / "mods/cameo/bits/temp" / spec.image,
        spec,
    )
    if approved.size != (donor.shape[1], donor.shape[0]):
        raise ValueError(f"{template}: approved preview size differs from donor")
    palette = shore.read_palette(
        ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    )
    donor_rgb = shore.indices_rgb(donor, palette)
    donor_rgb[~domain] = shore.BACKGROUND
    donor_overlay = anchor_scanner.anchor_overlay(
        donor_rgb,
        domain,
        spec,
        list(manifest_record["rock_subtiles"]),
        list(manifest_record["candidates"]),
    )

    background_rgb = np.asarray(approved, dtype=np.uint8)
    occupied = np.zeros(domain.shape, dtype=bool)
    placed: list[dict[str, object]] = []
    rejected: list[dict[str, object]] = []
    candidates = sorted(
        manifest_record["candidates"],
        key=lambda candidate: (
            -family_rank(preferred_family(candidate)),
            -float(candidate["confidence"]),
        ),
    )
    for candidate in candidates:
        chosen = fit_candidate(
            candidate,
            domain,
            background_rgb,
            occupied,
            families,
            sprites,
        )
        if chosen is None:
            rejected.append(
                {
                    "candidate": candidate["id"],
                    "reason": "no family fit passed sparse-domain and overlap limits",
                }
            )
            continue
        placed.append(chosen)
        occupied |= chosen["column_mask"]

    pool_layer = Image.new("RGBA", approved.size, (0, 0, 0, 0))
    for placement in placed:
        if placement["treatment"] == "glowing":
            pool_layer.alpha_composite(
                pool_layer_for_base(placement["base_mask"], domain)
            )
    empty_columns = Image.new("RGBA", approved.size, (0, 0, 0, 0))
    if np.any(np.asarray(pool_layer, dtype=np.uint8)[:, :, 3] > 0):
        decorated = molten.merged_shore_image(
            approved.convert("RGBA"),
            pool_layer,
            empty_columns,
            unified_field=True,
        ).convert("RGBA")
    else:
        decorated = approved.convert("RGBA")
    for placement in sorted(placed, key=lambda item: item["anchor"]["y"]):
        decorated.alpha_composite(placement["layer"])
    decorated_rgb = decorated.convert("RGB")
    decorated_array = np.asarray(decorated_rgb, dtype=np.uint8).copy()
    decorated_array[~domain] = background_rgb[~domain]
    domain_distance = padded_domain_distance(domain)
    outer_domain_edge = domain & (domain_distance <= 1.0)
    decorated_array[outer_domain_edge] = background_rgb[outer_domain_edge]
    changed_domain_edge_pixels = int(
        np.count_nonzero(
            np.any(decorated_array != background_rgb, axis=2)
            & outer_domain_edge
        )
    )
    decorated_rgb = Image.fromarray(decorated_array, mode="RGB")

    audit_placements = []
    for placement in placed:
        audit_placements.append(
            {
                key: value
                for key, value in placement.items()
                if key not in {"layer", "base_mask", "column_mask"}
            }
        )
    return (
        decorated_rgb,
        {
            "template": template,
            "candidate_count": len(manifest_record["candidates"]),
            "placed_count": len(placed),
            "rejected_count": len(rejected),
            "placements": audit_placements,
            "rejected": rejected,
            "changed_outer_domain_edge_pixels": changed_domain_edge_pixels,
        },
        donor_overlay,
    )


def fit_candidate(
    candidate: dict[str, object],
    domain: np.ndarray,
    background_rgb: np.ndarray,
    occupied: np.ndarray,
    families: dict[str, list[pillars.Column]],
    sprites: dict[str, dict[str, dict[str, Image.Image]]],
) -> dict[str, object] | None:
    requested_anchor = candidate["anchor"]
    for nudge_x, nudge_y in PLACEMENT_NUDGE_OFFSETS:
        adjusted = dict(candidate)
        adjusted["anchor"] = {
            "x": int(requested_anchor["x"]) + nudge_x,
            "y": int(requested_anchor["y"]) + nudge_y,
        }
        chosen = fit_candidate_at_anchor(
            adjusted,
            domain,
            background_rgb,
            occupied,
            families,
            sprites,
        )
        if chosen is not None:
            chosen["requested_anchor"] = {
                "x": int(requested_anchor["x"]),
                "y": int(requested_anchor["y"]),
            }
            chosen["anchor_nudge"] = {"x": nudge_x, "y": nudge_y}
            return chosen
    return None


def fit_candidate_at_anchor(
    candidate: dict[str, object],
    domain: np.ndarray,
    background_rgb: np.ndarray,
    occupied: np.ndarray,
    families: dict[str, list[pillars.Column]],
    sprites: dict[str, dict[str, dict[str, Image.Image]]],
) -> dict[str, object] | None:
    preferred = preferred_family(candidate)
    anchor = candidate["anchor"]
    for family_name in FAMILY_DOWNGRADE[preferred]:
        scale = FAMILY_SCALES[family_name]
        for variant in ("normal", "shorter"):
            treatment = resolve_treatment(candidate, background_rgb)
            sprite_kind = "glowing" if treatment == "glowing" else "clean"
            layer, base_mask, column_mask, coverage, edge_clearance = place_family_layer(
                sprites[family_name][variant][sprite_kind],
                families[family_name],
                int(anchor["x"]),
                int(anchor["y"]),
                scale,
                domain,
            )
            visible_pixels = int(np.count_nonzero(column_mask))
            envelope_edge_clearance = edge_clearance
            if treatment == "glowing":
                pool_alpha = np.asarray(
                    pool_layer_for_base(base_mask, domain),
                    dtype=np.uint8,
                )[:, :, 3]
                envelope_mask = column_mask | (pool_alpha > 0)
                envelope_edge_clearance = mask_edge_clearance(
                    envelope_mask,
                    domain,
                )
            required_clearance = 6.0 if treatment == "glowing" else 2.0
            if (
                visible_pixels == 0
                or coverage < 0.90
                or envelope_edge_clearance < required_clearance
            ):
                continue
            overlap_pixels = int(np.count_nonzero(column_mask & occupied))
            overlap_fraction = overlap_pixels / max(1, visible_pixels)
            if overlap_fraction > 0.22:
                continue
            return {
                "candidate": candidate["id"],
                "anchor": {"x": int(anchor["x"]), "y": int(anchor["y"])},
                "requested_family": preferred,
                "family": family_name,
                "variant": variant,
                "scale": scale,
                "treatment": treatment,
                "domain_coverage": round(coverage, 3),
                "domain_edge_clearance": round(edge_clearance, 3),
                "complete_envelope_edge_clearance": round(
                    envelope_edge_clearance,
                    3,
                ),
                "overlap_fraction": round(overlap_fraction, 3),
                "layer": layer,
                "base_mask": base_mask,
                "column_mask": column_mask,
            }
    return None


def place_family_layer(
    sprite: Image.Image,
    columns: list[pillars.Column],
    target_x: int,
    target_y: int,
    scale: float,
    domain: np.ndarray,
) -> tuple[Image.Image, np.ndarray, np.ndarray, float, float]:
    base_source = family_base_mask(columns)
    source_anchor = family_anchor(columns)
    scaled_size = (
        max(1, round(sprite.width * scale)),
        max(1, round(sprite.height * scale)),
    )
    sprite_scaled = sprite.resize(scaled_size, Image.Resampling.LANCZOS)
    base_scaled = base_source.resize(scaled_size, Image.Resampling.NEAREST)
    scaled_anchor = (
        round(source_anchor[0] * scale),
        round(source_anchor[1] * scale),
    )
    destination = (
        target_x - scaled_anchor[0],
        target_y - scaled_anchor[1],
    )
    tile_size = (domain.shape[1], domain.shape[0])
    layer = Image.new("RGBA", tile_size, (0, 0, 0, 0))
    layer.alpha_composite(sprite_scaled, dest=destination)
    base_tile = Image.new("L", tile_size, 0)
    base_tile.paste(base_scaled, destination)
    alpha = np.asarray(layer, dtype=np.uint8)[:, :, 3]
    column_mask = alpha > 24
    source_pixels = int(
        np.count_nonzero(np.asarray(sprite_scaled, dtype=np.uint8)[:, :, 3] > 24)
    )
    coverage = int(np.count_nonzero(column_mask & domain)) / max(1, source_pixels)
    layer_rgba = np.asarray(layer, dtype=np.uint8).copy()
    layer_rgba[~domain, 3] = 0
    layer = Image.fromarray(layer_rgba, mode="RGBA")
    column_mask &= domain
    base_mask = (np.asarray(base_tile, dtype=np.uint8) > 0) & domain
    edge_clearance = mask_edge_clearance(column_mask, domain)
    return layer, base_mask, column_mask, coverage, edge_clearance


def padded_domain_distance(domain: np.ndarray) -> np.ndarray:
    padded = np.pad(domain, 1, mode="constant", constant_values=False)
    return ndimage.distance_transform_edt(padded)[1:-1, 1:-1]


def mask_edge_clearance(mask: np.ndarray, domain: np.ndarray) -> float:
    if not np.any(mask):
        return 0.0
    return float(padded_domain_distance(domain)[mask].min())


def family_base_mask(columns: list[pillars.Column]) -> Image.Image:
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
    return image


def family_anchor(columns: list[pillars.Column]) -> tuple[int, int]:
    left = min(column.x - column.radius for column in columns)
    right = max(column.x + column.radius for column in columns)
    bottom = max(column.base_y + column.radius for column in columns)
    return round((left + right) * 0.5), round(bottom)


def pool_layer_for_base(base_mask: np.ndarray, domain: np.ndarray) -> Image.Image:
    fitted = ndimage.binary_closing(base_mask, structure=shore.disk(1))
    fitted = ndimage.binary_fill_holes(fitted)
    signed = (
        ndimage.distance_transform_edt(~fitted)
        - ndimage.distance_transform_edt(fitted)
    )
    distance = ndimage.gaussian_filter(signed, sigma=0.55, mode="nearest")
    pool = (distance <= 2.2) & domain
    heat_distance = np.maximum(0.0, distance)
    stops = np.asarray((0.0, 0.7, 1.4, 2.2), dtype=np.float32)
    colors = np.asarray(
        (
            (255.0, 218.0, 80.0),
            (250.0, 193.0, 52.0),
            (244.0, 172.0, 41.0),
            (238.0, 150.0, 33.0),
        ),
        dtype=np.float32,
    )
    rgb = np.zeros((*base_mask.shape, 3), dtype=np.float32)
    for channel in range(3):
        rgb[:, :, channel] = np.interp(
            heat_distance,
            stops,
            colors[:, channel],
        )
    rgb[~pool] = 0.0
    alpha = shore.feather_alpha(pool, 0.65)
    alpha[~domain] = 0
    return Image.fromarray(
        np.dstack((np.clip(np.rint(rgb), 0, 255).astype(np.uint8), alpha)),
        mode="RGBA",
    )


def resolve_treatment(
    candidate: dict[str, object],
    background_rgb: np.ndarray,
) -> str:
    rule = str(candidate["placement_treatment"])
    if rule == "clean_basalt_no_pool_no_glow":
        return "clean"
    if rule == "glowing_basalt_9px_with_unified_pool_and_feeders":
        return "glowing"
    anchor = candidate["anchor"]
    bounds = candidate["bounds"]
    half_width = max(5, int(bounds["width"]) // 2)
    x0 = max(0, int(anchor["x"]) - half_width)
    x1 = min(background_rgb.shape[1], int(anchor["x"]) + half_width + 1)
    y0 = max(0, int(anchor["y"]) - 5)
    y1 = min(background_rgb.shape[0], int(anchor["y"]) + 4)
    sample = background_rgb[y0:y1, x0:x1]
    red = sample[:, :, 0].astype(np.float32)
    green = sample[:, :, 1].astype(np.float32)
    blue = sample[:, :, 2].astype(np.float32)
    crack = (red > 90.0) & (red > green * 1.2) & (red > blue * 1.45)
    return "glowing" if float(np.mean(crack)) >= 0.018 else "clean"


def preferred_family(candidate: dict[str, object]) -> str:
    return ASSET_TO_FAMILY[str(candidate["suggested_asset_family"])]


def family_rank(name: str) -> int:
    return {"tiny": 0, "small": 1, "medium": 2, "large": 3}[name]


if __name__ == "__main__":
    raise SystemExit(main())
