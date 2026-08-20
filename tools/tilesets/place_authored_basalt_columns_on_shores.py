#!/usr/bin/env python
"""Place the approved authored basalt-column library on YAML Rock shore cells."""

from __future__ import annotations

import argparse
import hashlib
import json
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

import generate_sh04_alpha_beach_prototype as shore
from manual_river_delta.prepare_production import quantize
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
TILE = 48
BACKGROUND = shore.BACKGROUND
DEFAULT_ASSET_DIR = ROOT / "tools/tilesets/assets/basalt-columns"
DEFAULT_OUT_DIR = (
    Path.home()
    / "Documents/agents/volcanic-theater/shorelines/authored-basalt-placement"
)
SIZE_PRIORITY = ((3, 2), (2, 3), (2, 2), (2, 1), (1, 2), (1, 1))
LAVA_WATER_FRACTION_THRESHOLD = 0.40
REINFORCED_BOUNCE_TEMPLATES = {"sh27", "sh49"}
REINFORCED_BOUNCE_STRENGTH = 2.0
THIN_BRIGHT_ENVELOPE_VARIANTS = {
    "1x1-a", "1x1-b", "2x1-a", "2x1-b", "2x2-a", "2x2-b",
    "1x2-a", "1x2-b", "2x3-a", "2x3-b", "3x2-a", "3x2-b",
}
THIN_BRIGHT_ENVELOPE_ALPHA = 2.0
SKIP_TEMPLATES = {"sh39": "leave undecorated by approved review decision"}
PLACEMENT_OVERRIDES = {
    "sh02": (
        {"left": 2, "top": 4, "size": (1, 1), "treatment": "lava"},
    ),
    "sh05": (
        {"left": 0, "top": 2, "size": (1, 1), "treatment": "lava"},
        {"left": 1, "top": 0, "size": (1, 1), "treatment": "ground"},
        {"left": 1, "top": 1, "size": (2, 1), "treatment": "lava"},
    ),
    "sh49": (
        {
            "left": 0,
            "top": 1,
            "size": (2, 1),
            "variant": "2x1-a",
            "treatment": "lava",
        },
        {
            "left": 1,
            "top": 2,
            "size": (2, 1),
            "variant": "2x1-b",
            "treatment": "lava",
        },
    ),
}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--templates", nargs="+")
    parser.add_argument("--asset-dir", type=Path, default=DEFAULT_ASSET_DIR)
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    parser.add_argument("--template-base-dir", type=Path)
    parser.add_argument("--page-size", type=int, default=6)
    args = parser.parse_args()
    if args.page_size < 1:
        raise ValueError("page size must be at least one")

    asset_dir = resolve(args.asset_dir)
    out_dir = resolve(args.out_dir)
    candidate_dir = out_dir / "candidate-vols"
    candidate_dir.mkdir(parents=True, exist_ok=True)
    volcanic_yaml = ROOT / "mods/cameo/tilesets/volcanic.yaml"
    volcanic_bits = ROOT / "mods/cameo/bits/volcanic"
    template_base_dir = (
        resolve(args.template_base_dir) if args.template_base_dir else volcanic_bits
    )
    volcanic_palette = shore.read_palette(volcanic_bits / "volcanic.pal")
    temperate_palette = shore.read_palette(
        ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    )
    temperate_bits = ROOT / "mods/cameo/bits/temp"
    donor_water_indices = shore.source_indices(
        temperate_bits / "w1.tem"
    ) | shore.source_indices(temperate_bits / "w2.tem")
    library, library_audit = load_library(asset_dir, volcanic_bits / "w1.vol")
    templates = args.templates or templates_with_rock(volcanic_yaml)

    panels: list[tuple[str, Image.Image]] = []
    effect_panels: list[tuple[str, Image.Image]] = []
    records: list[dict[str, object]] = []
    for template in templates:
        spec = shore.read_template_spec(volcanic_yaml, f"{template}.vol")
        rock_subtiles = {
            index for index, terrain in spec.terrain.items() if terrain == "Rock"
        }
        if not rock_subtiles:
            raise ValueError(f"{template}: template has no YAML Rock cells")
        current_indices, base, domain = decode_template(
            template_base_dir / f"{template}.vol",
            spec,
            volcanic_palette,
        )
        donor, donor_indices = donor_preview(template, spec, temperate_palette)
        placements = build_placements(
            template,
            spec,
            rock_subtiles,
            donor_indices,
            donor_water_indices,
        )
        skipped = SKIP_TEMPLATES.get(template)
        if skipped:
            placements = []

        raw_decorated, placed, authored_mask, effect_masks = decorate(
            template,
            base,
            domain,
            spec,
            placements,
            library,
        )
        indexed, candidate_indices = quantize(raw_decorated, volcanic_palette)
        if effect_masks:
            baseline_decorated, _, _, _ = decorate(
                template,
                base,
                domain,
                spec,
                placements,
                library,
                reinforce_internal_bounce=False,
            )
            _, baseline_indices = quantize(
                baseline_decorated,
                volcanic_palette,
            )
            no_glow_decorated, _, _, _ = decorate(
                template,
                base,
                domain,
                spec,
                placements,
                library,
                include_detached_glow=False,
            )
            _, no_glow_indices = quantize(
                no_glow_decorated,
                volcanic_palette,
            )
            palette_array = np.asarray(volcanic_palette, dtype=np.float64)
            post_delta = luminance_array(palette_array[candidate_indices]) - luminance_array(
                palette_array[baseline_indices]
            )
            for effect in effect_masks:
                bounce_mask = effect["bounce_mask"]
                external_mask = effect["external_glow_mask"]
                bounce_delta = post_delta[bounce_mask]
                placed[int(effect["placement_index"])][
                    "palette_quantization_audit"
                ] = {
                    "mean_internal_bounce_luminance_gain": round(
                        float(np.mean(bounce_delta)), 4
                    ),
                    "maximum_internal_bounce_luminance_gain": round(
                        float(np.max(bounce_delta)), 4
                    ),
                    "brighter_internal_bounce_pixels": int(
                        np.count_nonzero(bounce_delta > 0)
                    ),
                    "internal_bounce_pixels": int(np.count_nonzero(bounce_mask)),
                    "changed_external_only_glow_pixels": int(
                        np.count_nonzero(
                            (candidate_indices != baseline_indices) & external_mask
                        )
                    ),
                    "external_pool_pixels": int(
                        np.count_nonzero(external_mask)
                    ),
                    "external_pool_pixels_surviving_palette_conversion": int(
                        np.count_nonzero(
                            (candidate_indices != no_glow_indices) & external_mask
                        )
                    ),
                }
        candidate_rgb = np.asarray(indexed, dtype=np.uint8).copy()
        candidate_rgb[~domain] = BACKGROUND
        indexed = Image.fromarray(candidate_rgb, mode="RGB")

        changed = candidate_indices != current_indices
        changed[~domain] = False
        escaped_changes = int(np.count_nonzero(changed & ~authored_mask))
        if escaped_changes:
            raise ValueError(
                f"{template}: {escaped_changes} indexed pixels changed outside authored footprints"
            )
        escaped_visible = int(np.count_nonzero(authored_mask & ~domain))
        if escaped_visible:
            raise ValueError(
                f"{template}: {escaped_visible} authored pixels escaped the sparse domain"
            )

        candidate_path = candidate_dir / f"{template}.vol"
        write_template_vol(candidate_path, candidate_indices, spec)
        annotation = annotate_rock(donor, spec, placements, skipped)
        panels.extend(
            (
                (f"{template}: Temperate donor + YAML Rock", annotation),
                (f"{template}: current production", base),
                (f"{template}: authored basalt candidate", indexed),
            )
        )
        covered = {
            index for placement in placements for index in placement["subtiles"]
        }
        uncovered = sorted(rock_subtiles - covered)
        if uncovered and not skipped:
            raise ValueError(f"{template}: uncovered Rock subtiles {uncovered}")
        records.append(
            {
                "template": template,
                "canvas": [base.width, base.height],
                "rock_subtiles": sorted(rock_subtiles),
                "placements": placed,
                "covered_rock_subtiles": sorted(covered),
                "uncovered_rock_subtiles": uncovered,
                "skipped": skipped,
                "changed_indexed_pixels": int(np.count_nonzero(changed)),
                "changed_outside_authored_footprints": escaped_changes,
                "visible_pixels_outside_sparse_domain": escaped_visible,
                "candidate_roundtrip_exact": True,
                "candidate_vol": str(candidate_path.resolve()),
            }
        )
        effect_panels.extend(
            build_lava_effect_panels(
                template,
                base,
                indexed,
                placements,
                library,
            )
        )

    page_paths = []
    for start in range(0, len(templates), args.page_size):
        page_templates = templates[start : start + args.page_size]
        page_panels = panels[start * 3 : (start + len(page_templates)) * 3]
        page_path = out_dir / (
            "authored_basalt_shore_review_" + "_".join(page_templates) + ".png"
        )
        shore.write_review_sheet(page_path, page_panels, columns=3, scale=2)
        page_paths.append(page_path)
        print(page_path.resolve())

    audit = {
        "preview_only": True,
        "vol_files_written": True,
        "production_files_modified": False,
        "asset_dir": str(asset_dir),
        "template_base_dir": str(template_base_dir.resolve()),
        "asset_manifest": library_audit,
        "selection_rule": (
            "partition every YAML Rock region into authored legal rectangles; "
            "preserve reviewed sh02/sh05 lava treatments and sh39 skip"
        ),
        "automatic_treatment_rule": (
            "lava composition when at least 40 percent of the lower half of the "
            "Temperate donor footprint is donor water; otherwise combined-ground"
        ),
        "automatic_mirroring": False,
        "ground_asset": "footprint/combined-ground.png",
        "lava_asset": (
            "all variants use the approved one-row unrestricted source-24px envelope, "
            "shifted north one source pixel with alpha multiplied by 2.0; sh27/sh49 "
            "use 2.0x internal bounce and other templates retain 1.0x internal bounce"
        ),
        "pages": [str(path.resolve()) for path in page_paths],
        "templates": records,
    }
    effect_review = None
    if effect_panels:
        effect_review = out_dir / "lava_contact_effect_diagnostic.png"
        shore.write_review_sheet(
            effect_review,
            effect_panels,
            columns=3,
            scale=2,
        )
        audit["lava_contact_effect_diagnostic"] = str(effect_review.resolve())
    audit_path = out_dir / "authored_basalt_shore_placement_audit.json"
    audit_path.write_text(json.dumps(audit, indent=2) + "\n", encoding="utf-8")
    print(audit_path.resolve())
    if effect_review:
        print(effect_review.resolve())
    return 0


def resolve(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def load_library(
    asset_dir: Path,
    production_w1: Path,
) -> tuple[dict[tuple[int, int], list[dict[str, object]]], dict[str, object]]:
    manifest = json.loads((asset_dir / "manifest.json").read_text(encoding="utf-8"))
    complete_audit = json.loads(
        (asset_dir / "reference/complete-refit-ground-lava-audit.json").read_text(
            encoding="utf-8"
        )
    )
    if complete_audit.get("status") != "PASS":
        raise ValueError("authored basalt library audit is not PASS")
    expected_w1 = str(manifest["production_w1_sha256"]).upper()
    actual_w1 = sha256(production_w1).upper()
    if actual_w1 != expected_w1:
        raise ValueError(
            f"production w1 phase differs from authored library: {actual_w1} != {expected_w1}"
        )

    library: dict[tuple[int, int], list[dict[str, object]]] = {}
    baseline_mismatch_pixels = 0
    baseline_max_channel_error = 0
    records = {record["variant"]: record for record in complete_audit["records"]}
    for variant in manifest["variants"]:
        variant_id = str(variant["id"])
        size = tuple(int(value) for value in variant["tiles"])
        directory = asset_dir / str(variant["directory"]) / "footprint"
        ground_path = directory / "combined-ground.png"
        lava_path = directory / "combined-lava.png"
        glow_path = directory / "lava-glow.png"
        envelope_path = (
            asset_dir
            / str(variant["directory"])
            / "source-24px/lava-glow-envelope.png"
        )
        source_formation_path = (
            asset_dir / str(variant["directory"]) / "source-24px/formation.png"
        )
        formation_path = directory / "formation.png"
        bounce_path = directory / "lava-bounce.png"
        soft_light_path = directory / "formation-soft-light.png"
        ground = load_authored_sprite(ground_path, size)
        lava = load_authored_sprite(lava_path, size)
        glow = load_authored_sprite(glow_path, size)
        envelope_24 = load_source_24(envelope_path, size)
        source_formation = load_source_24(source_formation_path, size)
        thin_bright_envelope_24 = (
            build_thin_bright_envelope(envelope_24, source_formation)
            if variant_id in THIN_BRIGHT_ENVELOPE_VARIANTS
            else None
        )
        formation = load_authored_sprite(formation_path, size)
        bounce = load_authored_sprite(bounce_path, size)
        soft_light = load_authored_sprite(soft_light_path, size)
        reproduced_soft_light = apply_soft_light_bounce(
            formation,
            bounce,
            strength=1.0,
        )
        reproduced = np.asarray(reproduced_soft_light, dtype=np.int16)
        approved = np.asarray(soft_light, dtype=np.int16)
        baseline_delta = np.abs(reproduced - approved)
        mismatch_pixels = int(
            np.count_nonzero(np.any(baseline_delta != 0, axis=2))
        )
        max_channel_error = int(baseline_delta.max())
        if mismatch_pixels > 16 or max_channel_error > 1:
            raise ValueError(
                f"{variant_id}: Soft Light reproduction exceeds rounding tolerance"
            )
        baseline_mismatch_pixels += mismatch_pixels
        baseline_max_channel_error = max(
            baseline_max_channel_error,
            max_channel_error,
        )
        if image_bytes(Image.alpha_composite(glow, soft_light)) != image_bytes(lava):
            raise ValueError(f"{variant_id}: editable lava layers do not rebuild combined-lava")
        record = records.get(variant_id)
        if not record or not record.get("passed"):
            raise ValueError(f"{variant_id}: missing passing complete-audit record")
        library.setdefault(size, []).append(
            {
                "id": variant_id,
                "ground": ground,
                "lava": lava,
                "lava_glow": glow,
                "lava_glow_envelope_24": envelope_24,
                "thin_bright_lava_glow_envelope_24": thin_bright_envelope_24,
                "formation": formation,
                "lava_bounce": bounce,
                "formation_soft_light": soft_light,
                "ground_path": ground_path,
                "lava_path": lava_path,
            }
        )
    missing = [size for size in SIZE_PRIORITY if len(library.get(size, [])) != 2]
    if missing:
        raise ValueError(f"authored library does not provide two variants for {missing}")
    return library, {
        "status": complete_audit["status"],
        "variant_count": len(manifest["variants"]),
        "production_w1_sha256": actual_w1,
        "all_footprint_assets_strict_2x": True,
        "soft_light_baseline_reproduction": {
            "formula": "W3C Soft Light",
            "mismatch_pixels_across_all_variants": baseline_mismatch_pixels,
            "maximum_channel_error": baseline_max_channel_error,
            "used_1x1_and_2x1_variants_exact": True,
        },
    }


def load_authored_sprite(path: Path, size: tuple[int, int]) -> Image.Image:
    with Image.open(path) as source:
        image = source.convert("RGBA")
    expected = (size[0] * TILE, size[1] * TILE)
    if image.size != expected:
        raise ValueError(f"{path}: expected {expected}, got {image.size}")
    values = np.asarray(image, dtype=np.uint8)
    if not strict_2x(values):
        raise ValueError(f"{path}: contains nonuniform 2x2 production blocks")
    return image


def load_source_24(path: Path, size: tuple[int, int]) -> Image.Image:
    with Image.open(path) as source:
        image = source.convert("RGBA")
    expected = (size[0] * 24, size[1] * 24)
    if image.size != expected:
        raise ValueError(f"{path}: expected {expected}, got {image.size}")
    return image


def build_thin_bright_envelope(
    envelope: Image.Image,
    formation: Image.Image,
) -> Image.Image:
    """Build approved Option A: one row, north 1px, alpha x2, RGB unchanged."""
    values = np.asarray(envelope.convert("RGBA"), dtype=np.uint8)
    formation_alpha = np.asarray(formation.convert("RGBA"), dtype=np.uint8)[:, :, 3]
    envelope_alpha = values[:, :, 3].astype(np.uint16)
    visible_strength = envelope_alpha * (255 - formation_alpha.astype(np.uint16))
    selected = np.zeros_like(values)
    for x in range(values.shape[1]):
        if np.any(envelope_alpha[:, x] > 0):
            y = int(np.argmax(visible_strength[:, x]))
            selected[y, x] = values[y, x]
    shifted = np.zeros_like(selected)
    shifted[:-1] = selected[1:]
    shifted[:, :, 3] = np.minimum(
        255,
        np.rint(shifted[:, :, 3].astype(np.float64) * THIN_BRIGHT_ENVELOPE_ALPHA),
    ).astype(np.uint8)
    return Image.fromarray(shifted, mode="RGBA")


def strict_2x(values: np.ndarray) -> bool:
    return bool(
        np.array_equal(values[0::2, 0::2], values[1::2, 0::2])
        and np.array_equal(values[0::2, 0::2], values[0::2, 1::2])
        and np.array_equal(values[0::2, 0::2], values[1::2, 1::2])
    )


def templates_with_rock(tileset: Path) -> list[str]:
    result = []
    for number in range(1, 55):
        template = f"sh{number:02d}"
        spec = shore.read_template_spec(tileset, f"{template}.vol")
        if any(terrain == "Rock" for terrain in spec.terrain.values()):
            result.append(template)
    return result


def decode_template(
    path: Path,
    spec: shore.TemplateSpec,
    palette: list[tuple[int, int, int]],
) -> tuple[np.ndarray, Image.Image, np.ndarray]:
    width, height, frames = read_shptd(path)
    expected_frames = spec.columns * spec.rows
    if (width, height, len(frames)) != (TILE, TILE, expected_frames):
        raise ValueError(
            f"{path}: expected 48x48/{expected_frames}, got {width}x{height}/{len(frames)}"
        )
    indices = np.zeros((spec.rows * TILE, spec.columns * TILE), dtype=np.uint8)
    domain = np.zeros(indices.shape, dtype=bool)
    for index, frame in enumerate(frames):
        row, column = divmod(index, spec.columns)
        block = np.frombuffer(frame, dtype=np.uint8).reshape(TILE, TILE)
        indices[
            row * TILE : (row + 1) * TILE,
            column * TILE : (column + 1) * TILE,
        ] = block
        if index in spec.terrain:
            domain[
                row * TILE : (row + 1) * TILE,
                column * TILE : (column + 1) * TILE,
            ] = True
    rgb = shore.indices_rgb(indices, palette)
    rgb[~domain] = BACKGROUND
    return indices, Image.fromarray(rgb, mode="RGB"), domain


def donor_preview(
    template: str,
    volcanic_spec: shore.TemplateSpec,
    palette: list[tuple[int, int, int]],
) -> tuple[Image.Image, np.ndarray]:
    donor_spec = shore.read_template_spec(
        ROOT / "mods/cameo/tilesets/ra_temperat.yaml",
        f"{template}.tem",
    )
    if (donor_spec.columns, donor_spec.rows) != (
        volcanic_spec.columns,
        volcanic_spec.rows,
    ):
        raise ValueError(f"{template}: donor and Volcanic layouts differ")
    indices, domain = shore.read_sparse_composite(
        ROOT / "mods/cameo/bits/temp" / donor_spec.image,
        donor_spec,
    )
    rgb = shore.indices_rgb(indices, palette)
    rgb[~domain] = BACKGROUND
    return Image.fromarray(rgb, mode="RGB"), indices


def rock_components(
    spec: shore.TemplateSpec,
    rock_subtiles: set[int],
) -> list[set[int]]:
    remaining = set(rock_subtiles)
    components = []
    while remaining:
        start = min(remaining)
        remaining.remove(start)
        group = {start}
        queue: deque[int] = deque((start,))
        while queue:
            index = queue.popleft()
            row, column = divmod(index, spec.columns)
            neighbors = (
                index - 1 if column else None,
                index + 1 if column + 1 < spec.columns else None,
                index - spec.columns if row else None,
                index + spec.columns if row + 1 < spec.rows else None,
            )
            for neighbor in neighbors:
                if neighbor is not None and neighbor in remaining:
                    remaining.remove(neighbor)
                    group.add(neighbor)
                    queue.append(neighbor)
        components.append(group)
    return components


def build_placements(
    template: str,
    spec: shore.TemplateSpec,
    rock_subtiles: set[int],
    donor_indices: np.ndarray,
    donor_water_indices: set[int],
) -> list[dict[str, object]]:
    if template in PLACEMENT_OVERRIDES:
        result = []
        for override in PLACEMENT_OVERRIDES[template]:
            placement = placement_record(spec, override)
            if not set(placement["subtiles"]).issubset(rock_subtiles):
                raise ValueError(f"{template}: reviewed override includes non-Rock cells")
            result.append(placement)
        covered = {index for item in result for index in item["subtiles"]}
        if covered != rock_subtiles:
            raise ValueError(
                f"{template}: reviewed overrides cover {sorted(covered)}, expected {sorted(rock_subtiles)}"
            )
        add_donor_phase_metrics(result, donor_indices, donor_water_indices, automatic=False)
        return result

    result = []
    for component in rock_components(spec, rock_subtiles):
        result.extend(partition_component(spec, component))
    add_donor_phase_metrics(result, donor_indices, donor_water_indices, automatic=True)
    return result


def add_donor_phase_metrics(
    placements: list[dict[str, object]],
    donor_indices: np.ndarray,
    donor_water_indices: set[int],
    *,
    automatic: bool,
) -> None:
    for placement in placements:
        left = int(placement["left"]) * TILE
        top = int(placement["top"]) * TILE
        width = int(placement["size"][0]) * TILE
        height = int(placement["size"][1]) * TILE
        lower = donor_indices[
            top + height // 2 : top + height,
            left : left + width,
        ]
        fraction = float(np.mean(np.isin(lower, list(donor_water_indices))))
        placement["donor_lower_water_fraction"] = round(fraction, 4)
        if automatic:
            placement["treatment"] = (
                "lava"
                if fraction >= LAVA_WATER_FRACTION_THRESHOLD
                else "ground"
            )


def partition_component(
    spec: shore.TemplateSpec,
    component: set[int],
) -> list[dict[str, object]]:
    remaining = set(component)
    result = []
    while remaining:
        start = min(remaining)
        top, left = divmod(start, spec.columns)
        chosen = None
        for size in SIZE_PRIORITY:
            width, height = size
            if left + width > spec.columns or top + height > spec.rows:
                continue
            cells = {
                (top + row) * spec.columns + left + column
                for row in range(height)
                for column in range(width)
            }
            if cells.issubset(remaining):
                chosen = {
                    "left": left,
                    "top": top,
                    "size": size,
                    "treatment": "ground",
                    "subtiles": sorted(cells),
                }
                break
        if chosen is None:
            raise RuntimeError(f"could not partition Rock component at {start}")
        remaining.difference_update(chosen["subtiles"])
        result.append(chosen)
    return result


def placement_record(
    spec: shore.TemplateSpec,
    record: dict[str, object],
) -> dict[str, object]:
    left, top = int(record["left"]), int(record["top"])
    width, height = tuple(int(value) for value in record["size"])
    subtiles = [
        (top + row) * spec.columns + left + column
        for row in range(height)
        for column in range(width)
    ]
    placement = {
        "left": left,
        "top": top,
        "size": (width, height),
        "treatment": str(record["treatment"]),
        "subtiles": subtiles,
    }
    if "variant" in record:
        placement["variant"] = str(record["variant"])
    return placement


def decorate(
    template: str,
    base: Image.Image,
    domain: np.ndarray,
    spec: shore.TemplateSpec,
    placements: list[dict[str, object]],
    library: dict[tuple[int, int], list[dict[str, object]]],
    *,
    reinforce_internal_bounce: bool = True,
    include_detached_glow: bool = True,
) -> tuple[
    Image.Image,
    list[dict[str, object]],
    np.ndarray,
    list[dict[str, object]],
]:
    decorated = base.convert("RGBA")
    authored_mask = np.zeros(domain.shape, dtype=bool)
    placed = []
    effect_masks: list[dict[str, object]] = []
    for ordinal, placement in enumerate(placements):
        size = tuple(placement["size"])
        choices = library[size]
        explicit_variant = placement.get("variant")
        if explicit_variant is None:
            choice = choices[variant_index(template, placement, ordinal)]
        else:
            matches = [item for item in choices if item["id"] == explicit_variant]
            if len(matches) != 1:
                raise ValueError(
                    f"{template}: explicit variant {explicit_variant!r} does not match size {size}"
                )
            choice = matches[0]
        treatment = str(placement["treatment"])
        reinforced_bounce = (
            reinforce_internal_bounce
            and treatment == "lava"
            and template in REINFORCED_BOUNCE_TEMPLATES
        )
        pre_quantization_audit = None
        if reinforced_bounce:
            baseline_formation = choice["formation_soft_light"]
            reinforced_formation = apply_soft_light_bounce(
                choice["formation"],
                choice["lava_bounce"],
                strength=REINFORCED_BOUNCE_STRENGTH,
            )
            bounce_mask_local = (
                np.asarray(choice["lava_bounce"], dtype=np.uint8)[:, :, 3] > 0
            )
            pre_delta = luminance_array(
                np.asarray(reinforced_formation, dtype=np.float64)[:, :, :3]
            ) - luminance_array(
                np.asarray(baseline_formation, dtype=np.float64)[:, :, :3]
            )
            pre_quantization_audit = {
                "mean_internal_bounce_luminance_gain": round(
                    float(np.mean(pre_delta[bounce_mask_local])), 4
                ),
                "maximum_internal_bounce_luminance_gain": round(
                    float(np.max(pre_delta[bounce_mask_local])), 4
                ),
            }
        left = int(placement["left"]) * TILE
        top = int(placement["top"]) * TILE
        glow_audit = None
        if treatment == "lava":
            lava_crop = base.crop(
                (
                    left,
                    top,
                    left + size[0] * TILE,
                    top + size[1] * TILE,
                )
            )
            envelope_source = (
                choice["thin_bright_lava_glow_envelope_24"]
                or choice["lava_glow_envelope_24"]
            )
            if envelope_source is not None:
                detached_glow = envelope_source.resize(
                    lava_crop.size,
                    Image.Resampling.NEAREST,
                )
                detached_values = np.asarray(detached_glow, dtype=np.uint8)
                if not strict_2x(detached_values):
                    raise ValueError("unrestricted detached glow is not strict 2x")
                glow_audit = {
                    "mode": (
                        "one-row unrestricted envelope shifted north 1 source pixel; "
                        "alpha multiplied by 2.0"
                        if choice["thin_bright_lava_glow_envelope_24"] is not None
                        else "unrestricted envelope used directly"
                    ),
                    "source_density": 24,
                    "production_density": 48,
                    "additional_crack_masking": False,
                    "rgb_modified": False,
                    "alpha_multiplier": (
                        THIN_BRIGHT_ENVELOPE_ALPHA
                        if choice["thin_bright_lava_glow_envelope_24"] is not None
                        else 1.0
                    ),
                    "source_offset": (
                        [0, -1]
                        if choice["thin_bright_lava_glow_envelope_24"] is not None
                        else [0, 0]
                    ),
                    "strict_nearest_neighbor_2x": True,
                    "visible_pixels": int(
                        np.count_nonzero(detached_values[:, :, 3] > 0)
                    ),
                }
                lava_formation = (
                    reinforced_formation
                    if reinforced_bounce
                    else choice["formation_soft_light"]
                )
                sprite = Image.alpha_composite(
                    (
                        detached_glow
                        if include_detached_glow
                        else Image.new(
                            "RGBA", detached_glow.size, (0, 0, 0, 0)
                        )
                    ),
                    lava_formation,
                )
            else:
                sprite = choice["lava"]
        else:
            sprite = choice["ground"]
        layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
        layer.alpha_composite(sprite, (left, top))
        alpha = np.asarray(layer, dtype=np.uint8)[:, :, 3] > 0
        allowed = np.zeros(domain.shape, dtype=bool)
        allowed[
            top : top + size[1] * TILE,
            left : left + size[0] * TILE,
        ] = True
        if np.any(alpha & ~allowed):
            raise ValueError(f"{template}/{choice['id']}: visible pixels escaped legal box")
        if np.any(alpha & ~domain):
            raise ValueError(f"{template}/{choice['id']}: visible pixels escaped template domain")
        decorated.alpha_composite(layer)
        authored_mask |= allowed
        if reinforced_bounce:
            formation_alpha = (
                np.asarray(choice["formation"], dtype=np.uint8)[:, :, 3] > 0
            )
            glow_alpha = np.asarray(detached_glow, dtype=np.uint8)[:, :, 3] > 0
            bounce_alpha = (
                np.asarray(choice["lava_bounce"], dtype=np.uint8)[:, :, 3] > 0
            )
            bounce_mask = np.zeros(domain.shape, dtype=bool)
            external_glow_mask = np.zeros(domain.shape, dtype=bool)
            bounce_mask[
                top : top + sprite.height,
                left : left + sprite.width,
            ] = bounce_alpha
            external_glow_mask[
                top : top + sprite.height,
                left : left + sprite.width,
            ] = glow_alpha & ~formation_alpha
            effect_masks.append(
                {
                    "placement_index": len(placed),
                    "bounce_mask": bounce_mask,
                    "external_glow_mask": external_glow_mask,
                }
            )
        path_key = "lava_path" if treatment == "lava" else "ground_path"
        placed.append(
            {
                "variant": choice["id"],
                "size": list(size),
                "left": int(placement["left"]),
                "top": int(placement["top"]),
                "subtiles": list(placement["subtiles"]),
                "treatment": treatment,
                "donor_lower_water_fraction": placement[
                    "donor_lower_water_fraction"
                ],
                "asset": str(choice[path_key].resolve()),
                "automatic_mirroring": False,
                "reinforced_internal_bounce": reinforced_bounce,
                "lava_composition": (
                    "authored lava-bounce using W3C Soft Light at "
                    f"{'2.0x' if reinforced_bounce else '1.0x'} alpha; one-row "
                    "unrestricted envelope shifted north one source pixel, alpha "
                    "multiplied by 2.0, and composited once behind"
                    if treatment == "lava"
                    else None
                ),
                "pre_quantization_audit": pre_quantization_audit,
                "detached_glow_audit": glow_audit,
                "destination": [left, top],
            }
        )
    return decorated.convert("RGB"), placed, authored_mask, effect_masks


def variant_index(
    template: str,
    placement: dict[str, object],
    ordinal: int,
) -> int:
    key = (
        f"{template}:{placement['left']}:{placement['top']}:"
        f"{placement['size']}:{ordinal}"
    ).encode()
    return hashlib.blake2s(key, digest_size=1).digest()[0] & 1


def annotate_rock(
    donor: Image.Image,
    spec: shore.TemplateSpec,
    placements: list[dict[str, object]],
    skipped: str | None,
) -> Image.Image:
    result = donor.copy()
    draw = ImageDraw.Draw(result)
    for index, terrain in spec.terrain.items():
        if terrain != "Rock":
            continue
        row, column = divmod(index, spec.columns)
        x0, y0 = column * TILE, row * TILE
        draw.rectangle(
            (x0, y0, x0 + TILE - 1, y0 + TILE - 1),
            outline=(42, 224, 210),
            width=2,
        )
    for placement in placements:
        left = int(placement["left"]) * TILE
        top = int(placement["top"]) * TILE
        label = "L" if placement["treatment"] == "lava" else "G"
        draw.text((left + 4, top + 4), label, fill="white")
    if skipped:
        draw.text((4, 4), "SKIP: approved undecorated", fill=(255, 220, 80))
    return result


def build_lava_effect_panels(
    template: str,
    base: Image.Image,
    indexed_candidate: Image.Image,
    placements: list[dict[str, object]],
    library: dict[tuple[int, int], list[dict[str, object]]],
) -> list[tuple[str, Image.Image]]:
    if template not in REINFORCED_BOUNCE_TEMPLATES:
        return []
    panels = []
    for ordinal, placement in enumerate(placements):
        if placement["treatment"] != "lava":
            continue
        size = tuple(placement["size"])
        choice = library[size][variant_index(template, placement, ordinal)]
        left = int(placement["left"]) * TILE
        top = int(placement["top"]) * TILE
        box = (
            left,
            top,
            left + size[0] * TILE,
            top + size[1] * TILE,
        )
        actual_crop = base.crop(box).convert("RGBA")
        envelope_source = (
            choice["thin_bright_lava_glow_envelope_24"]
            or choice["lava_glow_envelope_24"]
        )
        envelope = envelope_source.resize(
            actual_crop.size,
            Image.Resampling.NEAREST,
        )
        reinforced_formation = apply_soft_light_bounce(
            choice["formation"],
            choice["lava_bounce"],
            strength=REINFORCED_BOUNCE_STRENGTH,
        )
        envelope_preview = Image.alpha_composite(actual_crop, envelope).convert("RGB")
        bounce_only_sprite = Image.alpha_composite(
            Image.new("RGBA", reinforced_formation.size, (0, 0, 0, 0)),
            reinforced_formation,
        )
        bounce_only_preview = Image.alpha_composite(
            actual_crop,
            bounce_only_sprite,
        ).convert("RGB")
        final_sprite = Image.alpha_composite(envelope, reinforced_formation)
        prequant_preview = Image.alpha_composite(actual_crop, final_sprite).convert("RGB")
        indexed_crop = indexed_candidate.crop(box).convert("RGB")
        label = f"{template}/{choice['id']}@{placement['left']},{placement['top']}"
        panels.extend(
            (
                (f"{label}: actual lava crop", actual_crop.convert("RGB")),
                (f"{label}: direct unrestricted envelope", envelope_preview),
                (f"{label}: internal 2x bounce only", bounce_only_preview),
                (f"{label}: final pre-quantization", prequant_preview),
                (f"{label}: final volcanic palette", indexed_crop),
            )
        )
    return panels


def write_template_vol(
    path: Path,
    indices: np.ndarray,
    spec: shore.TemplateSpec,
) -> None:
    frames = []
    blank = bytes(TILE * TILE)
    for index in range(spec.columns * spec.rows):
        if index not in spec.terrain:
            frames.append(blank)
            continue
        row, column = divmod(index, spec.columns)
        frames.append(
            indices[
                row * TILE : (row + 1) * TILE,
                column * TILE : (column + 1) * TILE,
            ].tobytes()
        )
    write_shptd(path, TILE, TILE, frames)
    width, height, decoded = read_shptd(path)
    if (width, height) != (TILE, TILE) or decoded != frames:
        raise ValueError(f"{path}: candidate VOL roundtrip mismatch")


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def image_bytes(image: Image.Image) -> bytes:
    return image.convert("RGBA").tobytes()


def luminance_array(rgb: np.ndarray) -> np.ndarray:
    return 0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]


def apply_soft_light_bounce(
    formation: Image.Image,
    bounce: Image.Image,
    *,
    strength: float,
) -> Image.Image:
    """Apply the authored clipped bounce to the formation with W3C Soft Light."""
    if strength < 0:
        raise ValueError("Soft Light bounce strength cannot be negative")
    base = np.asarray(formation.convert("RGBA"), dtype=np.float64)
    source = np.asarray(bounce.convert("RGBA"), dtype=np.float64)
    backdrop = base[:, :, :3] / 255.0
    blend_source = source[:, :, :3] / 255.0
    effective_alpha = np.clip(
        source[:, :, 3:4] / 255.0 * strength,
        0.0,
        1.0,
    )
    d = np.where(
        backdrop <= 0.25,
        ((16.0 * backdrop - 12.0) * backdrop + 4.0) * backdrop,
        np.sqrt(backdrop),
    )
    blended = np.where(
        blend_source <= 0.5,
        backdrop
        - (1.0 - 2.0 * blend_source) * backdrop * (1.0 - backdrop),
        backdrop + (2.0 * blend_source - 1.0) * (d - backdrop),
    )
    output = base.copy()
    output[:, :, :3] = np.rint(
        (backdrop * (1.0 - effective_alpha) + blended * effective_alpha) * 255.0
    )
    return Image.fromarray(np.clip(output, 0, 255).astype(np.uint8), mode="RGBA")


if __name__ == "__main__":
    raise SystemExit(main())
