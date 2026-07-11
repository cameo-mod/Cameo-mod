#!/usr/bin/env python
"""Preview approved basalt families on exact rectangular YAML Rock components."""

from __future__ import annotations

import argparse
import json
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

import generate_basalt_column_families as family_renderer
import generate_basalt_pillar_study as pillars
import generate_ground_basalt_medium_large_study as ground_family
import generate_molten_pool_study as molten
import generate_sh04_alpha_beach_prototype as shore
import place_basalt_columns_on_shores as legacy_placement


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUT_DIR = Path.home() / "Documents/agents/volcanic-theater/shorelines/workbench/yaml-rock-placement"
APPROVED_LAVA_DIR = Path.home() / "Documents/agents/volcanic-theater/shorelines/workbench"
MASTER_REVIEW = Path.home() / "Documents/agents/volcanic-theater/shorelines/workbench/shoreline_master_river_and_anchor_review_sh01_sh54.json"
TEMPLATES = tuple(f"sh{number:02d}" for number in range(1, 55))
FAMILY_FOR_SIZE = {(1, 1): "small_1x1", (2, 1): "medium_2x1", (2, 2): "large_2x2"}
LEGACY_LAVA_FAMILY = {
    "small_1x1": ("small", 1.0),
    "medium_2x1": ("medium", 0.82),
    "large_2x2": ("large", 0.68),
}
SKIP_TEMPLATES = {"sh39": "leave undecorated by review decision"}
PLACEMENT_OVERRIDES = {
    "sh02": (
        {"left": 2, "top": 4, "size": (1, 1), "treatment": "lava"},
    ),
    "sh05": (
        {"left": 0, "top": 2, "size": (1, 1), "treatment": "lava"},
        {"left": 1, "top": 0, "size": (1, 1), "treatment": "ground"},
        {"left": 1, "top": 1, "size": (2, 1), "treatment": "lava"},
    ),
}
LAVA_NUDGES = (
    (0, 0),
    (-2, 0), (2, 0),
    (-4, 0), (4, 0),
    (-6, 0), (6, 0),
    (0, -2), (0, 2),
    (-4, -2), (4, -2),
)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--templates", nargs="+", default=TEMPLATES)
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    parser.add_argument("--page-size", type=int, default=6)
    args = parser.parse_args()
    if args.page_size < 1:
        raise ValueError("page size must be at least one")
    out_dir = args.out_dir.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    families = build_families()
    master = json.loads(MASTER_REVIEW.read_text(encoding="utf-8"))
    approved_sources = {
        record["template"]: Path(record["volcanic_source"])
        for record in master["templates"]
    }
    temperate_palette = shore.read_palette(
        ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    )
    panels: list[tuple[str, Image.Image]] = []
    audit: dict[str, object] = {
        "preview_only": True,
        "vol_files_written": False,
        "selection_rule": "exact rectangular YAML Rock component only",
        "approved_source_manifest": str(MASTER_REVIEW.resolve()),
        "skipped_templates": SKIP_TEMPLATES,
        "templates": [],
    }
    for template in args.templates:
        base, donor, domain, spec = load_template(
            template, approved_sources[template], temperate_palette
        )
        components = rock_components(spec)
        decorated, placed, unsupported = decorate(
            template, base, domain, spec, components, families
        )
        annotation = rock_annotation(donor, spec, placed, unsupported)
        if template in SKIP_TEMPLATES:
            decorated = base.copy()
            placed = []
        panels.extend(
            (
                (f"{template}: Temperate donor + YAML Rock", annotation),
                (f"{template}: approved volcanic base", base),
                (f"{template}: proposed basalt", decorated),
            )
        )
        audit["templates"].append(
            {
                "template": template,
                "rock_component_count": len(components),
                "placed": placed,
                "unsupported": unsupported,
                "skipped": SKIP_TEMPLATES.get(template),
            }
        )
    for start in range(0, len(args.templates), args.page_size):
        page_templates = args.templates[start : start + args.page_size]
        page = panels[start * 3 : (start + len(page_templates)) * 3]
        page_path = out_dir / f"yaml_rock_basalt_review_{'_'.join(page_templates)}.png"
        shore.write_review_sheet(page_path, page, columns=3, scale=2)
        print(page_path.resolve())
    audit_path = out_dir / "yaml_rock_basalt_placement_audit.json"
    audit_path.write_text(json.dumps(audit, indent=2), encoding="utf-8")
    print(audit_path.resolve())
    return 0


def build_families() -> dict[str, dict[str, object]]:
    result: dict[str, dict[str, object]] = {}
    legacy_lava_families = family_renderer.build_families()
    for ordinal, (name, spec) in enumerate(ground_family.SPECS.items()):
        tiles_w, tiles_h = spec["tiles"]
        bounds = ground_family.target_bounds(
            tiles_w * shore.TILE, tiles_h * shore.TILE, float(spec["center_y"])
        )
        columns = ground_family.scale_approved_forest(spec)
        sprite, rocks, columns = ground_family.render_fitted_cluster(
            columns, bounds, name, ordinal, spec
        )
        if ground_family.pixels_outside_bounds(sprite, bounds):
            raise ValueError(f"{name}: family is not contained by its nominal box")
        legacy_name, legacy_scale = LEGACY_LAVA_FAMILY[name]
        lava_variants: dict[str, dict[str, Image.Image]] = {}
        for variant in ("normal", "shorter"):
            suffix = "" if variant == "normal" else "_shorter"
            asset_path = APPROVED_LAVA_DIR / f"basalt_columns_glowing_{legacy_name}{suffix}.png"
            with Image.open(asset_path) as source_image:
                approved_sprite = source_image.convert("RGBA")
            variant_columns = (
                legacy_lava_families[legacy_name]
                if variant == "normal"
                else family_renderer.scale_heights(legacy_lava_families[legacy_name], 0.5)
            )
            lava_sprite, base_mask = place_approved_lava_asset(
                approved_sprite, variant_columns, bounds, legacy_scale
            )
            lava_variants[variant] = {
                "sprite": lava_sprite,
                "base_mask": base_mask,
            }
        result[name] = {
            "ground_sprite": sprite,
            "lava_variants": lava_variants,
            "bounds": bounds,
            "rocks": len(rocks),
        }
    return result


def place_approved_lava_asset(
    source: Image.Image,
    columns: list[pillars.Column],
    bounds: tuple[int, int, int, int],
    scale: float,
) -> tuple[Image.Image, Image.Image]:
    """Place an approved lava-family PNG without regenerating its pixels."""
    source_base = Image.new("L", (pillars.NATIVE_SIZE, pillars.NATIVE_SIZE), 0)
    base_draw = ImageDraw.Draw(source_base)
    for column in columns:
        base_draw.polygon(
            pillars.hex_points(column.x, column.base_y, column.radius, column.seed),
            fill=255,
        )
    scaled_size = (
        max(1, round(source.width * scale)),
        max(1, round(source.height * scale)),
    )
    scaled_sprite = source.resize(scaled_size, Image.Resampling.LANCZOS)
    scaled_base = source_base.resize(scaled_size, Image.Resampling.NEAREST)
    left = min(column.x - column.radius for column in columns)
    right = max(column.x + column.radius for column in columns)
    bottom = max(column.base_y + column.radius for column in columns)
    source_anchor = (round((left + right) * 0.5 * scale), round(bottom * scale))
    target_anchor = (round((bounds[0] + bounds[2]) * 0.5), bounds[3] - 1)
    destination = (
        target_anchor[0] - source_anchor[0],
        target_anchor[1] - source_anchor[1],
    )
    sprite = Image.new("RGBA", (pillars.NATIVE_SIZE, pillars.NATIVE_SIZE), (0, 0, 0, 0))
    sprite.alpha_composite(scaled_sprite, destination)
    base_mask = Image.new("L", (pillars.NATIVE_SIZE, pillars.NATIVE_SIZE), 0)
    base_mask.paste(scaled_base, destination)
    return sprite, base_mask


def load_template(
    template: str,
    approved_source: Path,
    temperate_palette: list[tuple[int, int, int]],
) -> tuple[Image.Image, Image.Image, np.ndarray, shore.TemplateSpec]:
    volcanic_yaml = ROOT / "mods/cameo/tilesets/volcanic.yaml"
    if volcanic_yaml.exists():
        spec = shore.read_template_spec(volcanic_yaml, f"{template}.vol")
    else:
        # Volcanic metadata is mechanically derived from Barren.  This
        # read-only fallback keeps review generation available when GitHub
        # Desktop temporarily removes newly added volcanic files during an
        # upstream update/stash cycle.
        spec = shore.read_template_spec(
            ROOT / "mods/cameo/tilesets/barren.yaml", f"{template}.bar"
        )
    with Image.open(approved_source) as source:
        base = source.convert("RGB")
    expected_size = (spec.columns * shore.TILE, spec.rows * shore.TILE)
    if base.size != expected_size:
        raise ValueError(
            f"{template}: approved source is {base.size}, expected {expected_size}"
        )
    donor_spec = shore.read_template_spec(
        ROOT / "mods/cameo/tilesets/ra_temperat.yaml", f"{template}.tem"
    )
    if (donor_spec.columns, donor_spec.rows) != (spec.columns, spec.rows):
        raise ValueError(f"{template}: Temperate and Volcanic layouts differ")
    donor_indices, donor_domain = shore.read_sparse_composite(
        ROOT / "mods/cameo/bits/temp" / donor_spec.image, donor_spec
    )
    domain = donor_domain
    donor_rgb = shore.indices_rgb(donor_indices, temperate_palette)
    donor_rgb[~donor_domain] = shore.BACKGROUND
    donor = Image.fromarray(donor_rgb, mode="RGB")
    return base, donor, domain, spec


def rock_components(spec: shore.TemplateSpec) -> list[dict[str, object]]:
    rocks = {index for index, terrain in spec.terrain.items() if terrain == "Rock"}
    components: list[dict[str, object]] = []
    while rocks:
        start = rocks.pop()
        group = {start}
        queue: deque[int] = deque((start,))
        while queue:
            index = queue.popleft()
            row, column = divmod(index, spec.columns)
            for neighbor in (
                index - 1 if column else None,
                index + 1 if column + 1 < spec.columns else None,
                index - spec.columns if row else None,
                index + spec.columns if row + 1 < spec.rows else None,
            ):
                if neighbor is not None and neighbor in rocks:
                    rocks.remove(neighbor)
                    group.add(neighbor)
                    queue.append(neighbor)
        rows = [index // spec.columns for index in group]
        columns = [index % spec.columns for index in group]
        left, right = min(columns), max(columns)
        top, bottom = min(rows), max(rows)
        size = (right - left + 1, bottom - top + 1)
        components.append(
            {
                "subtiles": sorted(group),
                "left": left,
                "top": top,
                "size": size,
                "rectangular": len(group) == size[0] * size[1],
            }
        )
    return sorted(components, key=lambda item: (int(item["top"]), int(item["left"])))


def decorate(
    template: str,
    base: Image.Image,
    domain: np.ndarray,
    spec: shore.TemplateSpec,
    components: list[dict[str, object]],
    families: dict[str, dict[str, object]],
) -> tuple[Image.Image, list[dict[str, object]], list[dict[str, object]]]:
    placed: list[dict[str, object]] = []
    unsupported: list[dict[str, object]] = []
    layers: list[tuple[int, Image.Image]] = []
    lava_bases = np.zeros(domain.shape, dtype=bool)
    placements: list[dict[str, object]] = []
    if template in PLACEMENT_OVERRIDES:
        for record in PLACEMENT_OVERRIDES[template]:
            left, top = int(record["left"]), int(record["top"])
            width, height = tuple(record["size"])
            subtiles = [
                (top + row) * spec.columns + left + column
                for row in range(height)
                for column in range(width)
            ]
            if any(spec.terrain.get(index) != "Rock" for index in subtiles):
                raise ValueError(f"{template}: reviewed override includes a non-Rock subtile")
            placements.append({**record, "subtiles": subtiles})
    else:
        for component in components:
            size = tuple(component["size"])
            if not bool(component["rectangular"]) or size not in FAMILY_FOR_SIZE:
                unsupported.append(
                    {"size": size, "subtiles": component["subtiles"], "reason": "unsupported Rock shape"}
                )
                continue
            placements.append(
                {
                    "left": component["left"],
                    "top": component["top"],
                    "size": size,
                    "treatment": "ground",
                    "subtiles": component["subtiles"],
                }
            )

    for ordinal, placement in enumerate(placements):
        size = tuple(placement["size"])
        family_name = FAMILY_FOR_SIZE.get(size)
        if family_name is None:
            unsupported.append(
                {"component": ordinal, "size": size, "subtiles": placement["subtiles"]}
            )
            continue
        family = families[family_name]
        left = int(placement["left"]) * shore.TILE
        top = int(placement["top"]) * shore.TILE
        bounds = family["bounds"]
        destination = (left - bounds[0], top - bounds[1])
        treatment = str(placement["treatment"])
        if treatment == "lava":
            candidates = [
                (variant, data["sprite"], data["base_mask"])
                for variant, data in family["lava_variants"].items()
            ]
        else:
            candidates = [("normal", family["ground_sprite"], None)]
        chosen: tuple[
            str, Image.Image, Image.Image | None, Image.Image, tuple[int, int]
        ] | None = None
        for variant, sprite, candidate_base_mask in candidates:
            nudges = LAVA_NUDGES if treatment == "lava" else ((0, 0),)
            for nudge in nudges:
                candidate_destination = (
                    destination[0] + nudge[0],
                    destination[1] + nudge[1],
                )
                candidate_layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
                candidate_layer.alpha_composite(sprite, candidate_destination)
                alpha = np.asarray(candidate_layer, dtype=np.uint8)[:, :, 3]
                if not np.any((alpha > 8) & ~domain):
                    chosen = (
                        variant,
                        sprite,
                        candidate_base_mask,
                        candidate_layer,
                        candidate_destination,
                    )
                    break
            if chosen is not None:
                break
        if chosen is None:
            unsupported.append(
                {
                    "component": ordinal,
                    "size": size,
                    "subtiles": placement["subtiles"],
                    "reason": "sprite crosses sparse template boundary",
                }
            )
            continue
        variant, _, chosen_base_mask, layer, chosen_destination = chosen
        if treatment == "lava":
            base_tile = Image.new("L", base.size, 0)
            base_tile.paste(chosen_base_mask, chosen_destination)
            lava_bases |= np.asarray(base_tile, dtype=np.uint8) > 0
        layers.append((top + size[1] * shore.TILE, layer))
        placed.append(
            {
                "component": ordinal,
                "family": family_name,
                "size": size,
                "left": int(placement["left"]),
                "top": int(placement["top"]),
                "subtiles": placement["subtiles"],
                "treatment": treatment,
                "variant": variant,
                "nudge": [
                    chosen_destination[0] - destination[0],
                    chosen_destination[1] - destination[1],
                ],
                "rocks": 0 if treatment == "lava" else family["rocks"],
            }
        )
    if np.any(lava_bases):
        pool_layer = legacy_placement.pool_layer_for_base(lava_bases & domain, domain)
        decorated = molten.merged_shore_image(
            base.convert("RGBA"),
            pool_layer,
            Image.new("RGBA", base.size, (0, 0, 0, 0)),
            unified_field=True,
        ).convert("RGBA")
    else:
        decorated = base.convert("RGBA")
    for _, layer in sorted(layers, key=lambda item: item[0]):
        decorated.alpha_composite(layer)
    return decorated.convert("RGB"), placed, unsupported


def rock_annotation(
    base: Image.Image,
    spec: shore.TemplateSpec,
    placed: list[dict[str, object]],
    unsupported: list[dict[str, object]],
) -> Image.Image:
    image = base.convert("RGB")
    draw = ImageDraw.Draw(image)
    placed_subtiles = {
        int(index) for placement in placed for index in placement.get("subtiles", [])
    }
    for index, terrain in spec.terrain.items():
        if terrain != "Rock":
            continue
        row, column = divmod(index, spec.columns)
        x0, y0 = column * shore.TILE, row * shore.TILE
        color = (42, 224, 210) if index in placed_subtiles else (245, 91, 84)
        draw.rectangle(
            (x0, y0, x0 + shore.TILE - 1, y0 + shore.TILE - 1),
            outline=color,
            width=2,
        )
        if index not in placed_subtiles:
            draw.text((x0 + 3, y0 + 3), "?", fill=color)
    for placement in placed:
        label = "L" if placement["treatment"] == "lava" else "G"
        draw.text(
            (int(placement["left"]) * shore.TILE + 3, int(placement["top"]) * shore.TILE + 3),
            label,
            fill=(255, 255, 255),
        )
    return image


if __name__ == "__main__":
    raise SystemExit(main())
