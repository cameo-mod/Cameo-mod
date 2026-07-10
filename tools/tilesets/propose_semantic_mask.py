#!/usr/bin/env python
"""Propose semantic mask PNGs and review sheets for exported mask sources."""

from __future__ import annotations

import argparse
import csv
import random
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SOURCE_ROOT = ROOT / ".vs/docs/volcanic-theater-previews/ra-temperate-mask-sources"
DEFAULT_OUT_ROOT = ROOT / ".vs/docs/volcanic-theater-previews/semantic-mask-reviews"
MASK_COLORS = {
    "lava": (255, 0, 0, 255),
    "cliff": (0, 255, 0, 255),
    "shore": (0, 0, 255, 255),
}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-root", type=Path, default=DEFAULT_SOURCE_ROOT)
    parser.add_argument("--out-root", type=Path, default=DEFAULT_OUT_ROOT)
    parser.add_argument("--sample", type=Path, help="Specific source PNG relative to source root")
    parser.add_argument("--category", choices=("shores", "cliffs", "water-cliffs"), help="Random sample category")
    parser.add_argument("--seed", type=int, help="Random seed for reproducible selection")
    args = parser.parse_args()

    source_root = resolve_repo_path(args.source_root)
    out_root = resolve_repo_path(args.out_root)
    source = choose_source(source_root, args.sample, args.category, args.seed)
    metadata = read_metadata(source_root)
    key = source.relative_to(source_root).as_posix()
    row = metadata.get(key)
    if not row:
        raise SystemExit(f"No metadata for {key}")

    source_image = Image.open(source).convert("RGBA")
    label_overlay = Image.open(source_root / "label-overlays" / key).convert("RGBA")
    mask = propose_mask(source_image, row)
    preview = make_preview(source_image, mask)

    target_dir = out_root / source.parent.name
    target_dir.mkdir(parents=True, exist_ok=True)
    stem = source.stem
    mask_path = target_dir / f"{stem}-mask.png"
    preview_path = target_dir / f"{stem}-preview.png"
    sheet_path = target_dir / f"{stem}-review.png"
    mask.save(mask_path)
    preview.save(preview_path)
    write_review_sheet(sheet_path, source_image, label_overlay, mask, preview, key, row)

    counts = count_mask_pixels(mask)
    print(f"Source: {source}")
    print(f"Mask: {mask_path}")
    print(f"Preview: {preview_path}")
    print(f"Review: {sheet_path}")
    print(f"Pixels: red={counts['red']} green={counts['green']} blue={counts['blue']} transparent={counts['transparent']}")
    return 0


def resolve_repo_path(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def choose_source(source_root: Path, sample: Path | None, category: str | None, seed: int | None) -> Path:
    if sample:
        source = sample if sample.is_absolute() else source_root / sample
        if not source.exists():
            raise SystemExit(f"Sample does not exist: {source}")
        return source

    folders = [category] if category else ["shores", "cliffs", "water-cliffs"]
    choices: list[Path] = []
    for folder in folders:
        choices.extend(sorted((source_root / folder).glob("*.png")))
    if not choices:
        raise SystemExit(f"No source PNGs under {source_root}")

    rng = random.Random(seed)
    return rng.choice(choices)


def read_metadata(source_root: Path) -> dict[str, dict[str, object]]:
    metadata: dict[str, dict[str, object]] = {}
    with (source_root / "metadata.csv").open(newline="") as stream:
        for row in csv.DictReader(stream):
            key = row["file"]
            item = metadata.setdefault(
                key,
                {
                    "file": key,
                    "template_id": row["template_id"],
                    "image": row["image"],
                    "category": row["category"],
                    "cells": [],
                },
            )
            item["cells"].append(row)
    return metadata


def propose_mask(image: Image.Image, metadata: dict[str, object]) -> Image.Image:
    category = normalize_category(str(metadata["category"]))
    cells = metadata["cells"]
    terrain_by_cell = {(int(cell["cell_x"]), int(cell["cell_y"])): str(cell["terrain"]) for cell in cells}
    template_width = max((x for x, _ in terrain_by_cell), default=0) + 1
    template_height = max((y for _, y in terrain_by_cell), default=0) + 1
    cell_width = image.width // max(1, template_width)
    cell_height = image.height // max(1, template_height)
    mask_pixels = []

    for index, (r, g, b, a) in enumerate(image.getdata()):
        if a == 0:
            mask_pixels.append((0, 0, 0, 0))
            continue

        x = index % image.width
        y = index // image.width
        terrain = terrain_by_cell.get((x // cell_width, y // cell_height), "")
        role = classify_pixel(r, g, b, x, y, image.width, image.height, terrain, category)
        if role:
            mask_pixels.append(MASK_COLORS[role])
        else:
            mask_pixels.append((r, g, b, a))

    mask = image.copy()
    mask.putdata(mask_pixels)
    return mask


def normalize_category(category: str) -> str:
    return category.lower().replace(" ", "-")


def classify_pixel(r: int, g: int, b: int, x: int, y: int, width: int, height: int, terrain: str, category: str) -> str | None:
    luma = 0.299 * r + 0.587 * g + 0.114 * b
    chroma = max(r, g, b) - min(r, g, b)
    blueish = b > r + 18 and b >= g - 12 and 48 < luma < 165
    pale = luma > 130 and chroma < 58
    dark = luma < 50
    brown = r >= b + 10 and g >= b - 8 and 38 <= luma <= 135
    gray_rock = 48 <= luma <= 120 and chroma < 48

    if terrain in {"Water", "River"}:
        return "lava"
    if terrain in {"Beach", "Ford"}:
        if blueish and category in {"shores", "water-cliffs"}:
            return "lava"
        return "shore"

    if category == "water-cliffs":
        if blueish and y < height * 0.48:
            return "lava"
        if (brown or gray_rock or dark) and not pale:
            return "cliff"
        # Snow attached to the main rock mass is intentionally left to human/vision review for now.
        return None

    if category == "cliffs":
        if terrain in {"Rock", "Rough"} and (brown or gray_rock or dark):
            return "cliff"
        return None

    if category == "shores":
        if blueish:
            return "lava"
        if terrain in {"Beach", "Ford"}:
            return "shore"
        return None

    return None


def make_preview(source: Image.Image, mask: Image.Image) -> Image.Image:
    overlay = Image.new("RGBA", source.size, (0, 0, 0, 0))
    overlay_pixels = []
    for pixel in mask.getdata():
        if pixel == MASK_COLORS["lava"]:
            overlay_pixels.append((255, 0, 0, 130))
        elif pixel == MASK_COLORS["cliff"]:
            overlay_pixels.append((0, 255, 0, 130))
        elif pixel == MASK_COLORS["shore"]:
            overlay_pixels.append((0, 0, 255, 130))
        else:
            overlay_pixels.append((0, 0, 0, 0))
    overlay.putdata(overlay_pixels)
    return Image.alpha_composite(source, overlay)


def write_review_sheet(path: Path, source: Image.Image, labels: Image.Image, mask: Image.Image, preview: Image.Image, key: str, metadata: dict[str, object]) -> None:
    font = ImageFont.load_default()
    panels = [("source", source), ("labels", labels), ("mask", mask), ("preview", preview)]
    gutter = 12
    header = 30
    info_height = 42
    panel_width = max(panel.width for _, panel in panels)
    panel_height = max(panel.height for _, panel in panels)
    sheet = Image.new("RGBA", (panel_width * 2 + gutter, header * 2 + panel_height * 2 + gutter + info_height), (73, 86, 99, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((6, 6), f"{key}  template={metadata['template_id']} image={metadata['image']} category={metadata['category']}", fill=(255, 255, 255, 255), font=font)
    positions = [(0, header), (panel_width + gutter, header), (0, header + panel_height + gutter + header), (panel_width + gutter, header + panel_height + gutter + header)]
    for (title, panel), (x, y) in zip(panels, positions):
        draw.text((x + 4, y - 18), title, fill=(255, 255, 255, 255), font=font)
        sheet.alpha_composite(panel, (x, y))
    draw.text((6, sheet.height - info_height + 8), "Mask colors: red=lava, green=cliff, blue=shore/bank, unpainted=ground/unchanged, transparent=empty", fill=(255, 255, 255, 255), font=font)
    sheet.save(path)


def count_mask_pixels(mask: Image.Image) -> dict[str, int]:
    counts = {"red": 0, "green": 0, "blue": 0, "transparent": 0}
    for pixel in mask.getdata():
        if pixel == MASK_COLORS["lava"]:
            counts["red"] += 1
        elif pixel == MASK_COLORS["cliff"]:
            counts["green"] += 1
        elif pixel == MASK_COLORS["shore"]:
            counts["blue"] += 1
        elif pixel[3] == 0:
            counts["transparent"] += 1
    return counts


if __name__ == "__main__":
    sys.exit(main())
