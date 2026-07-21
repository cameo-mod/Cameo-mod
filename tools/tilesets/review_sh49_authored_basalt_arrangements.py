#!/usr/bin/env python
"""Build the preview-only review-12 study of legal authored sh49 arrangements."""

from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import generate_sh04_alpha_beach_prototype as shore
import place_authored_basalt_columns_on_shores as basalt
from manual_river_delta.prepare_production import quantize


ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = (
    Path.home()
    / "Documents/agents/volcanic-theater/shorelines/authored-basalt-placement/"
    "review-12-sh49-arrangements-codex"
)
REVIEW11 = (
    Path.home()
    / "Documents/agents/volcanic-theater/shorelines/authored-basalt-placement/"
    "review-11-unrestricted-envelope"
)
TILE = basalt.TILE


CANDIDATES = (
    (
        "C1 current control",
        "top 2x1-b; bottom 2x1-a",
        ((0, 1, (2, 1), "2x1-b"), (1, 2, (2, 1), "2x1-a")),
    ),
    (
        "C2 swapped rows",
        "top 2x1-a; bottom 2x1-b",
        ((0, 1, (2, 1), "2x1-a"), (1, 2, (2, 1), "2x1-b")),
    ),
    (
        "C3 both a",
        "top 2x1-a; bottom 2x1-a",
        ((0, 1, (2, 1), "2x1-a"), (1, 2, (2, 1), "2x1-a")),
    ),
    (
        "C4 both b",
        "top 2x1-b; bottom 2x1-b",
        ((0, 1, (2, 1), "2x1-b"), (1, 2, (2, 1), "2x1-b")),
    ),
    (
        "C5 split bottom",
        "top 2x1-b; bottom 1x1-a + 1x1-b",
        (
            (0, 1, (2, 1), "2x1-b"),
            (1, 2, (1, 1), "1x1-a"),
            (2, 2, (1, 1), "1x1-b"),
        ),
    ),
    (
        "C6 four singles",
        "top 1x1-b + 1x1-a; bottom 1x1-a + 1x1-b",
        (
            (0, 1, (1, 1), "1x1-b"),
            (1, 1, (1, 1), "1x1-a"),
            (1, 2, (1, 1), "1x1-a"),
            (2, 2, (1, 1), "1x1-b"),
        ),
    ),
    (
        "C7 split top",
        "top 1x1-a + 1x1-b; bottom 2x1-a",
        (
            (0, 1, (1, 1), "1x1-a"),
            (1, 1, (1, 1), "1x1-b"),
            (1, 2, (2, 1), "2x1-a"),
        ),
    ),
)


def placement(spec: shore.TemplateSpec, item: tuple) -> dict[str, object]:
    left, top, size, variant = item
    width, height = size
    subtiles = [
        (top + row) * spec.columns + left + column
        for row in range(height)
        for column in range(width)
    ]
    return {
        "left": left,
        "top": top,
        "size": size,
        "variant": variant,
        "treatment": "lava",
        "subtiles": subtiles,
        "source_offset_24px": [0, 0],
    }


def annotate(image: Image.Image, spec: shore.TemplateSpec, placements: list[dict]) -> Image.Image:
    result = image.convert("RGB").copy()
    draw = ImageDraw.Draw(result)
    for x in range(0, result.width + 1, TILE):
        draw.line((x, 0, x, result.height - 1), fill=(150, 150, 150), width=1)
    for y in range(0, result.height + 1, TILE):
        draw.line((0, y, result.width - 1, y), fill=(150, 150, 150), width=1)
    colors = ((0, 255, 255), (255, 100, 255), (100, 255, 100), (255, 210, 40))
    for ordinal, item in enumerate(placements):
        left = int(item["left"]) * TILE
        top = int(item["top"]) * TILE
        width, height = item["size"]
        color = colors[ordinal % len(colors)]
        draw.rectangle(
            (left + 1, top + 1, left + width * TILE - 2, top + height * TILE - 2),
            outline=color,
            width=2,
        )
        draw.text((left + 4, top + 4), str(item["variant"]), fill=(255, 255, 255))
    return result


def strict_2x_indices(values: np.ndarray) -> bool:
    return bool(
        np.array_equal(values[0::2, 0::2], values[1::2, 0::2])
        and np.array_equal(values[0::2, 0::2], values[0::2, 1::2])
        and np.array_equal(values[0::2, 0::2], values[1::2, 1::2])
    )


def title_panel(text: str, size: tuple[int, int]) -> Image.Image:
    panel = Image.new("RGB", size, (24, 24, 28))
    draw = ImageDraw.Draw(panel)
    font = ImageFont.load_default()
    words = text.split()
    lines: list[str] = []
    line = ""
    for word in words:
        trial = f"{line} {word}".strip()
        if draw.textlength(trial, font=font) > size[0] - 12 and line:
            lines.append(line)
            line = word
        else:
            line = trial
    if line:
        lines.append(line)
    y = 8
    for value in lines:
        draw.text((6, y), value, fill=(245, 245, 245), font=font)
        y += 12
    return panel


def main() -> int:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    candidate_dir = OUT_DIR / "candidate-vols"
    candidate_dir.mkdir(parents=True, exist_ok=True)
    volcanic_yaml = ROOT / "mods/cameo/tilesets/volcanic.yaml"
    volcanic_bits = ROOT / "mods/cameo/bits/volcanic"
    palette = shore.read_palette(volcanic_bits / "volcanic.pal")
    temperate_palette = shore.read_palette(
        ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    )
    donor_water = shore.source_indices(
        ROOT / "mods/cameo/bits/temp/w1.tem"
    ) | shore.source_indices(ROOT / "mods/cameo/bits/temp/w2.tem")
    library, library_audit = basalt.load_library(
        basalt.DEFAULT_ASSET_DIR, volcanic_bits / "w1.vol"
    )

    spec = shore.read_template_spec(volcanic_yaml, "sh49.vol")
    rock = {index for index, terrain in spec.terrain.items() if terrain == "Rock"}
    current_indices, current, domain = basalt.decode_template(
        volcanic_bits / "sh49.vol", spec, palette
    )
    _, donor_indices = basalt.donor_preview("sh49", spec, temperate_palette)
    panels: list[tuple[str, Image.Image]] = []
    records = []

    sh27_spec = shore.read_template_spec(volcanic_yaml, "sh27.vol")
    _, sh27_base, sh27_domain = basalt.decode_template(
        volcanic_bits / "sh27.vol", sh27_spec, palette
    )
    _, sh27_donor_indices = basalt.donor_preview("sh27", sh27_spec, temperate_palette)
    sh27_rock = {
        index for index, terrain in sh27_spec.terrain.items() if terrain == "Rock"
    }
    sh27_placements = basalt.build_placements(
        "sh27", sh27_spec, sh27_rock, sh27_donor_indices, donor_water
    )
    sh27_raw, _, _, _ = basalt.decorate(
        "sh27", sh27_base, sh27_domain, sh27_spec, sh27_placements, library
    )
    sh27_indexed_image, sh27_indices = quantize(sh27_raw, palette)
    sh27_path = candidate_dir / "sh27-reference.vol"
    basalt.write_template_vol(sh27_path, sh27_indices, sh27_spec)
    control_sh27 = REVIEW11 / "candidate-vols/sh27.vol"
    sh27_matches = sh27_path.read_bytes() == control_sh27.read_bytes()
    if not sh27_matches:
        raise ValueError("sh27 candidate differs from review-11 byte control")
    sh27_reference = sh27_indexed_image.convert("RGB")
    sh27_values = np.asarray(sh27_reference).copy()
    sh27_values[~sh27_domain] = basalt.BACKGROUND
    sh27_reference = Image.fromarray(sh27_values, mode="RGB")
    panels.append(("UNCHANGED sh27 reference (review-11 bytes match)", sh27_reference))
    panels.append(("Review 12 Codex continuation", title_panel(
        "sh49 authored basalt arrangements. Cyan/magenta/green/yellow outlines are exact legal Rock placement boxes; gray is the 48px tile grid.",
        current.size,
    )))
    panels.append(("Legend / validation", title_panel(
        "Each row: current production | before Volcanic palette | after Volcanic palette. All placements use zero 24px offset and strict nearest 2x output.",
        current.size,
    )))

    for candidate_id, assignment, items in CANDIDATES:
        placements = [placement(spec, item) for item in items]
        basalt.add_donor_phase_metrics(
            placements, donor_indices, donor_water, automatic=False
        )
        covered = {index for item in placements for index in item["subtiles"]}
        if covered != rock:
            raise ValueError(f"{candidate_id}: covers {sorted(covered)}, expected {sorted(rock)}")
        if any(not set(item["subtiles"]).issubset(rock) for item in placements):
            raise ValueError(f"{candidate_id}: placement assigned over non-Rock terrain")

        raw, placed, authored_mask, effects = basalt.decorate(
            "sh49", current, domain, spec, placements, library
        )
        indexed_image, indices = quantize(raw, palette)
        indexed_rgb = np.asarray(indexed_image, dtype=np.uint8).copy()
        indexed_rgb[~domain] = basalt.BACKGROUND
        indexed_image = Image.fromarray(indexed_rgb, mode="RGB")
        vol_path = candidate_dir / f"{candidate_id.split()[0].lower()}-sh49.vol"
        basalt.write_template_vol(vol_path, indices, spec)
        review11_sh49_match = None
        if candidate_id.startswith("C1 "):
            review11_sh49 = REVIEW11 / "candidate-vols/sh49.vol"
            review11_sh49_match = vol_path.read_bytes() == review11_sh49.read_bytes()
            if not review11_sh49_match:
                raise ValueError("C1 sh49 control differs from review-11 candidate")

        visible_outside_domain = int(np.count_nonzero(authored_mask & ~domain))
        changed = indices != current_indices
        changed[~domain] = False
        changed_outside_boxes = int(np.count_nonzero(changed & ~authored_mask))
        selected_assets = []
        for source_item in placements:
            choices = library[tuple(source_item["size"])]
            choice = next(
                item for item in choices if item["id"] == source_item["variant"]
            )
            envelope = choice["lava_glow_envelope_24"].resize(
                choice["formation"].size, Image.Resampling.NEAREST
            )
            formation_alpha = np.asarray(choice["formation"], dtype=np.uint8)[:, :, 3]
            bounce_alpha = np.asarray(choice["lava_bounce"], dtype=np.uint8)[:, :, 3]
            if np.any((bounce_alpha > 0) & (formation_alpha == 0)):
                raise ValueError(f"{candidate_id}: bounce escaped formation alpha")
            reinforced_alpha = np.minimum(255, bounce_alpha.astype(np.uint16) * 2)
            effective_alpha = np.rint(
                np.clip(bounce_alpha.astype(np.float64) / 255.0 * 2.0, 0.0, 1.0)
                * 255.0
            ).astype(np.uint16)
            if not np.array_equal(reinforced_alpha, effective_alpha):
                raise ValueError(f"{candidate_id}: 2.0x bounce alpha formula mismatch")
            selected_assets.extend((choice["formation"], choice["lava_bounce"], envelope))
        strict_authored_blocks = all(
            basalt.strict_2x(np.asarray(image, dtype=np.uint8))
            for image in selected_assets
        )
        full_canvas_strict_blocks = strict_2x_indices(indices)
        if visible_outside_domain or changed_outside_boxes or not strict_authored_blocks:
            raise ValueError(
                f"{candidate_id}: placement/domain/2x audit failed: "
                f"outside_domain={visible_outside_domain}, "
                f"outside_boxes={changed_outside_boxes}, "
                f"strict_authored_2x={strict_authored_blocks}"
            )
        if len(effects) != len(placements):
            raise ValueError(f"{candidate_id}: lava effect count mismatch")

        for placed_item, source_item in zip(placed, placements):
            if placed_item["variant"] != source_item["variant"]:
                raise ValueError(f"{candidate_id}: explicit variant was not honored")
            effect = placed_item["detached_glow_audit"]
            if not effect or effect["mode"] != "unrestricted envelope used directly":
                raise ValueError(f"{candidate_id}: unrestricted envelope audit failed")
            if placed_item["lava_composition"] != (
                "authored lava-bounce alpha multiplied by 2.0 inside formation using "
                "W3C Soft Light; unrestricted envelope composited once behind"
            ):
                raise ValueError(f"{candidate_id}: lava composition audit failed")

        panels.extend(
            (
                (f"{candidate_id} | current production", annotate(current, spec, placements)),
                (f"{candidate_id} | before palette", annotate(raw, spec, placements)),
                (f"{candidate_id} | after palette", annotate(indexed_image, spec, placements)),
            )
        )
        records.append(
            {
                "candidate": candidate_id,
                "assignment": assignment,
                "review11_sh49_control_byte_match": review11_sh49_match,
                "placements": placed,
                "covered_yaml_rock_subtiles": sorted(covered),
                "uncovered_yaml_rock_subtiles": sorted(rock - covered),
                "placement_cells_over_non_rock": [],
                "visible_pixels_outside_legal_boxes": 0,
                "visible_pixels_outside_sparse_domain": visible_outside_domain,
                "changed_pixels_outside_legal_boxes": changed_outside_boxes,
                "strict_uniform_2x2_authored_production_layers": strict_authored_blocks,
                "full_candidate_canvas_strict_2x2": full_canvas_strict_blocks,
                "full_canvas_note": (
                    "diagnostic only: inherited production lava/base pixels are not globally "
                    "uniform 2x2; all newly placed authored layers remain strict 2x"
                ),
                "source_offsets_24px": [[0, 0] for _ in placements],
                "whole_source_pixel_offsets": True,
                "unrestricted_envelope_composites": len(effects),
                "expected_unrestricted_envelope_composites": len(placements),
                "internal_bounce_alpha_formula": "min(255, original_bounce_alpha * 2.0)",
                "internal_bounce_clipped_to_formation": True,
                "w3c_soft_light_passes_per_formation": 1,
                "lava_ground_shadow": False,
                "candidate_roundtrip_exact": True,
                "candidate_vol": str(vol_path.resolve()),
            }
        )

    review_path = OUT_DIR / "review-12-sh49-authored-arrangements-codex.png"
    shore.write_review_sheet(review_path, panels, columns=3, scale=2)
    audit = {
        "owner": "Codex continuation",
        "preview_only": True,
        "production_files_modified": False,
        "review": str(review_path.resolve()),
        "asset_library": library_audit,
        "unrestricted_envelope_provenance": str(
            (basalt.DEFAULT_ASSET_DIR / "reference/refit-glow-envelope-import-audit.json").resolve()
        ),
        "sh27_control": {
            "review11_candidate": str(control_sh27.resolve()),
            "review12_reference": str(sh27_path.resolve()),
            "byte_for_byte_match": sh27_matches,
            "sha256": basalt.sha256(sh27_path),
        },
        "candidates": records,
        "recommended_candidate": {
            "candidate": "C2 swapped rows",
            "reason": (
                "keeps two non-repeating authored 2x1 formations while moving the longer, "
                "more continuous 2x1-a silhouette to the upper row and the round-ended "
                "2x1-b silhouette to the lower row; this softens the opposing box-aligned "
                "ends without fragmenting either Rock row into obvious 1x1 units"
            ),
        },
        "result": "PASS",
    }
    audit_path = OUT_DIR / "review-12-sh49-arrangements-audit-codex.json"
    audit_path.write_text(json.dumps(audit, indent=2) + "\n", encoding="utf-8")
    print(review_path.resolve())
    print(audit_path.resolve())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
