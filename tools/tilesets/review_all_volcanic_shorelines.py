#!/usr/bin/env python
"""Render a complete sh01-sh54 Temperate-vs-Volcanic production review."""

from __future__ import annotations

import json
from collections import Counter
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import generate_sh04_alpha_beach_prototype as shore
import place_authored_basalt_columns_on_shores as basalt
from shptd import read_shptd


ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = (
    Path.home()
    / "Documents/agents/volcanic-theater/shorelines/"
    "review-20-all-sh01-sh54-codex"
)
TERRAIN_COLORS = {
    "Clear": (80, 230, 110),
    "Beach": (255, 210, 55),
    "Water": (55, 190, 255),
    "Rock": (40, 235, 220),
    "Rough": (235, 90, 235),
    "River": (80, 150, 255),
}


def annotate_terrain(image: Image.Image, spec: shore.TemplateSpec) -> Image.Image:
    result = image.convert("RGB").copy()
    draw = ImageDraw.Draw(result)
    for index, terrain in spec.terrain.items():
        row, column = divmod(index, spec.columns)
        x0, y0 = column * basalt.TILE, row * basalt.TILE
        color = TERRAIN_COLORS.get(terrain, (235, 235, 235))
        draw.rectangle(
            (x0, y0, x0 + basalt.TILE - 1, y0 + basalt.TILE - 1),
            outline=color,
            width=1,
        )
    return result


def pair_panel(donor: Image.Image, volcanic: Image.Image) -> Image.Image:
    header = 14
    gap = 4
    width = donor.width + gap + volcanic.width
    height = header + max(donor.height, volcanic.height)
    result = Image.new("RGB", (width, height), basalt.BACKGROUND)
    draw = ImageDraw.Draw(result)
    font = ImageFont.load_default()
    draw.text((3, 2), "DONOR", fill="white", font=font)
    draw.text((donor.width + gap + 3, 2), "VOLCANIC", fill="white", font=font)
    result.paste(donor, (0, header))
    result.paste(volcanic, (donor.width + gap, header))
    return result


def main() -> int:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    volcanic_yaml = ROOT / "mods/cameo/tilesets/volcanic.yaml"
    volcanic_bits = ROOT / "mods/cameo/bits/volcanic"
    volcanic_palette = shore.read_palette(volcanic_bits / "volcanic.pal")
    temperate_palette = shore.read_palette(
        ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    )

    panels: list[tuple[str, Image.Image]] = []
    records = []
    for number in range(1, 55):
        template = f"sh{number:02d}"
        spec = shore.read_template_spec(volcanic_yaml, f"{template}.vol")
        path = volcanic_bits / f"{template}.vol"
        width, height, frames = read_shptd(path)
        expected_frames = spec.columns * spec.rows
        if (width, height) != (basalt.TILE, basalt.TILE):
            raise ValueError(f"{template}: unexpected frame geometry {width}x{height}")
        if len(frames) != expected_frames:
            raise ValueError(
                f"{template}: has {len(frames)} frames, expected {expected_frames}"
            )

        _, volcanic, domain = basalt.decode_template(path, spec, volcanic_palette)
        donor, _ = basalt.donor_preview(template, spec, temperate_palette)
        counts = Counter(spec.terrain.values())
        roles = " ".join(f"{key}:{counts[key]}" for key in sorted(counts))
        panels.append(
            (
                f"{template} | {spec.columns}x{spec.rows} | {roles}",
                pair_panel(annotate_terrain(donor, spec), volcanic),
            )
        )
        records.append(
            {
                "template": template,
                "image": spec.image,
                "template_size_cells": [spec.columns, spec.rows],
                "frame_size_pixels": [width, height],
                "expected_frames": expected_frames,
                "actual_frames": len(frames),
                "terrain_counts": dict(sorted(counts.items())),
                "has_rock": counts["Rock"] > 0,
                "visible_domain_pixels": int(np.count_nonzero(domain)),
                "production_vol": str(path.resolve()),
                "sha256": basalt.sha256(path),
                "decode_passed": True,
            }
        )

    pages = []
    page_size = 9
    for start in range(0, len(panels), page_size):
        page_panels = panels[start : start + page_size]
        first = start + 1
        last = min(start + page_size, len(panels))
        page_path = OUT_DIR / f"all-shorelines-sh{first:02d}-sh{last:02d}-codex.png"
        shore.write_review_sheet(page_path, page_panels, columns=3, scale=2)
        pages.append(page_path)
        print(page_path.resolve())

    audit = {
        "owner": "Codex continuation",
        "preview_only": True,
        "production_files_modified": False,
        "scope": "all volcanic shoreline templates sh01 through sh54 without filtering",
        "terrain_outline_legend": TERRAIN_COLORS,
        "template_count": len(records),
        "rock_bearing_count": sum(record["has_rock"] for record in records),
        "non_rock_count": sum(not record["has_rock"] for record in records),
        "pages": [str(path.resolve()) for path in pages],
        "templates": records,
        "result": "PASS",
    }
    audit_path = OUT_DIR / "all-shorelines-sh01-sh54-audit-codex.json"
    audit_path.write_text(json.dumps(audit, indent=2) + "\n", encoding="utf-8")
    print(audit_path.resolve())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
