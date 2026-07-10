#!/usr/bin/env python
"""Export template PNGs for hand-painted volcanic semantic masks."""

from __future__ import annotations

import argparse
import csv
import json
import re
import sys
from pathlib import Path

from shptd import read_shptd


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_TILESET = ROOT / "mods/cameo/tilesets/barren.yaml"
DEFAULT_BITS = ROOT / "mods/cameo/bits/barren"
DEFAULT_PAL = DEFAULT_BITS / "barren.pal"
DEFAULT_OUT_DIR = ROOT / ".vs/docs/volcanic-theater-previews/barren-mask-sources"
TILE = 48
TRANSPARENT = (0, 0, 0, 0)


def resolve_repo_path(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--scale", type=int, default=4, help="Nearest-neighbor export scale")
    parser.add_argument("--tileset", type=Path, default=DEFAULT_TILESET, help="Tileset YAML to export")
    parser.add_argument("--bits", type=Path, default=DEFAULT_BITS, help="Directory containing source theater art")
    parser.add_argument("--palette", type=Path, default=DEFAULT_PAL, help="Source palette file")
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR, help="Output directory")
    args = parser.parse_args()

    if args.scale < 1:
        raise ValueError("--scale must be at least 1")

    try:
        from PIL import Image
    except ImportError as exc:
        raise SystemExit("Pillow is required to export PNGs") from exc

    tileset = resolve_repo_path(args.tileset)
    bits = resolve_repo_path(args.bits)
    palette_path = resolve_repo_path(args.palette)
    out_dir = resolve_repo_path(args.out_dir)

    templates = parse_templates(tileset)
    terrain_types = parse_terrain_types(tileset)
    palette = read_pal(palette_path)
    out_dir.mkdir(parents=True, exist_ok=True)

    exported = []
    metadata = []
    skipped = []
    wanted = {
        "Beach": "shores",
        "Cliffs": "cliffs",
        "Water Cliffs": "water-cliffs",
    }

    for image, info in sorted(templates.items()):
        category = first_matching_category(info["categories"], wanted)
        if not category:
            continue

        source = bits / image
        if not source.exists():
            skipped.append((image, "missing source"))
            continue

        try:
            composed = compose_template(source, palette, info)
        except Exception as exc:
            skipped.append((image, str(exc)))
            continue

        folder = out_dir / wanted[category]
        folder.mkdir(parents=True, exist_ok=True)
        template_id = info["template_id"]
        target = folder / f"{template_id:04d}-{Path(image).stem}-x{args.scale}.png"
        if args.scale != 1:
            composed = composed.resize((composed.width * args.scale, composed.height * args.scale), Image.Resampling.NEAREST)
        composed.save(target)
        exported.append(target)
        metadata.append(make_metadata_record(target, image, info, category, args.scale, terrain_types, out_dir))

        overlay_folder = out_dir / "label-overlays" / wanted[category]
        overlay_folder.mkdir(parents=True, exist_ok=True)
        overlay = make_label_overlay(composed, info, args.scale, terrain_types)
        overlay.save(overlay_folder / target.name)

    write_metadata(metadata, out_dir)
    write_readme(args.scale, exported, skipped, out_dir)

    print(f"Wrote {len(exported)} paintable PNGs under {out_dir}")
    if skipped:
        print(f"Skipped {len(skipped)} assets")
        for image, reason in skipped[:12]:
            print(f"  {image}: {reason}")
        if len(skipped) > 12:
            print(f"  ... {len(skipped) - 12} more")

    return 0


def parse_templates(path: Path) -> dict[str, dict[str, object]]:
    templates: dict[str, dict[str, object]] = {}
    image: str | None = None
    template_id = 0
    tile_types: dict[int, str] = {}
    categories: set[str] = set()
    width = 0
    height = 0
    frame_count = 0
    max_tile = -1

    def flush() -> None:
        nonlocal image, template_id, tile_types, categories, width, height, frame_count, max_tile
        if image:
            templates[image] = {
                "template_id": template_id,
                "count": max(frame_count, max_tile + 1),
                "width": width,
                "height": height,
                "tiles": dict(tile_types),
                "categories": set(categories),
            }
        image = None
        tile_types = {}
        categories = set()
        width = 0
        height = 0
        frame_count = 0
        max_tile = -1

    for raw in path.read_text().splitlines():
        stripped = raw.strip()
        if raw.startswith("\tTemplate@"):
            flush()
            template_id = int(raw.split("@", 1)[1].split(":", 1)[0])
        elif stripped.startswith("Images:"):
            image = stripped.split(":", 1)[1].strip()
        elif image and stripped.startswith("Size:"):
            width, height = [int(part) for part in stripped.split(":", 1)[1].strip().split(",", 1)]
            frame_count = width * height
        elif image and stripped.startswith("Categories:"):
            categories.update(part.strip() for part in stripped.split(":", 1)[1].split(",") if part.strip())
        elif image and re.match(r"^\d+:", stripped):
            key, value = stripped.split(":", 1)
            tile_index = int(key)
            max_tile = max(max_tile, tile_index)
            tile_type = value.strip()
            if tile_type:
                tile_types[tile_index] = tile_type

    flush()
    return templates


def parse_terrain_types(path: Path) -> dict[str, dict[str, str]]:
    terrain: dict[str, dict[str, str]] = {}
    current: str | None = None

    for raw in path.read_text().splitlines():
        stripped = raw.strip()
        if raw.startswith("\tTerrainType@"):
            current = raw.split("@", 1)[1].split(":", 1)[0]
            terrain[current] = {"Type": current, "TargetTypes": ""}
        elif current and stripped.startswith("Type:"):
            terrain[current]["Type"] = stripped.split(":", 1)[1].strip()
        elif current and stripped.startswith("TargetTypes:"):
            terrain[current]["TargetTypes"] = stripped.split(":", 1)[1].strip()
        elif raw.startswith("\tTemplate@"):
            current = None

    return terrain


def first_matching_category(categories: set[str], wanted: dict[str, str]) -> str | None:
    for category in ("Beach", "Cliffs", "Water Cliffs"):
        if category in categories and category in wanted:
            return category
    return None


def read_pal(path: Path) -> list[tuple[int, int, int]]:
    data = path.read_bytes()
    if len(data) != 256 * 3:
        raise ValueError(f"{path} is not a 768-byte palette")
    return [tuple(channel * 4 for channel in data[i * 3 : i * 3 + 3]) for i in range(256)]


def compose_template(path: Path, palette: list[tuple[int, int, int]], info: dict[str, object]):
    from PIL import Image

    template_width = int(info["width"] or 1)
    template_height = int(info["height"] or 1)
    frame_width, frame_height, frames = read_shptd(path)
    tiles = info["tiles"]
    image = Image.new("RGBA", (template_width * TILE, template_height * TILE), TRANSPARENT)

    for i, frame in enumerate(frames):
        if not isinstance(tiles, dict) or i not in tiles or not any(pixel != 0 for pixel in frame):
            continue

        frame_image = Image.new("RGBA", (frame_width, frame_height), TRANSPARENT)
        frame_image.putdata([(*palette[index], 255) for index in frame])
        image.alpha_composite(frame_image, ((i % template_width) * TILE, (i // template_width) * TILE))

    return image


def make_metadata_record(
    target: Path,
    image: str,
    info: dict[str, object],
    category: str,
    scale: int,
    terrain_types: dict[str, dict[str, str]],
    out_dir: Path,
) -> dict[str, object]:
    tiles = info["tiles"]
    cells = []
    if isinstance(tiles, dict):
        width = int(info["width"] or 1)
        for index in sorted(tiles):
            terrain = str(tiles[index])
            cells.append(
                {
                    "index": index,
                    "x": index % width,
                    "y": index // width,
                    "terrain": terrain,
                    "type": terrain_types.get(terrain, {}).get("Type", terrain),
                    "target_types": terrain_types.get(terrain, {}).get("TargetTypes", ""),
                }
            )

    return {
        "file": str(target.relative_to(out_dir)).replace("\\", "/"),
        "image": image,
        "template_id": int(info["template_id"]),
        "category": category,
        "scale": scale,
        "template_width": int(info["width"] or 1),
        "template_height": int(info["height"] or 1),
        "source_tile_size": TILE,
        "exported_cell_size": TILE * scale,
        "cells": cells,
    }


def make_label_overlay(image, info: dict[str, object], scale: int, terrain_types: dict[str, dict[str, str]]):
    from PIL import ImageDraw, ImageFont

    overlay = image.convert("RGBA")
    draw = ImageDraw.Draw(overlay, "RGBA")
    font = ImageFont.load_default()
    cell = TILE * scale
    width = int(info["width"] or 1)
    height = int(info["height"] or 1)
    tiles = info["tiles"]

    for y in range(height):
        for x in range(width):
            x0 = x * cell
            y0 = y * cell
            x1 = x0 + cell - 1
            y1 = y0 + cell - 1
            draw.rectangle((x0, y0, x1, y1), outline=(255, 255, 255, 120), width=max(1, scale))

    if isinstance(tiles, dict):
        for index, terrain in sorted(tiles.items()):
            x = index % width
            y = index // width
            x0 = x * cell
            y0 = y * cell
            label = str(terrain)
            target_types = terrain_types.get(label, {}).get("TargetTypes", "")
            if target_types:
                label = f"{label}\n{target_types}"

            bbox = draw.multiline_textbbox((x0 + 4 * scale, y0 + 4 * scale), label, font=font, spacing=1)
            pad = 2 * scale
            draw.rectangle((bbox[0] - pad, bbox[1] - pad, bbox[2] + pad, bbox[3] + pad), fill=(0, 0, 0, 150))
            draw.multiline_text((x0 + 4 * scale, y0 + 4 * scale), label, fill=(255, 255, 255, 255), font=font, spacing=1)

    return overlay


def write_metadata(metadata: list[dict[str, object]], out_dir: Path) -> None:
    (out_dir / "metadata.json").write_text(json.dumps(metadata, indent=2) + "\n", newline="\n")

    with (out_dir / "metadata.csv").open("w", newline="") as stream:
        writer = csv.writer(stream)
        writer.writerow(["file", "template_id", "image", "category", "cell_index", "cell_x", "cell_y", "terrain", "type", "target_types"])
        for record in metadata:
            for cell in record["cells"]:
                writer.writerow(
                    [
                        record["file"],
                        record["template_id"],
                        record["image"],
                        record["category"],
                        cell["index"],
                        cell["x"],
                        cell["y"],
                        cell["terrain"],
                        cell["type"],
                        cell["target_types"],
                    ]
                )


def write_readme(scale: int, exported: list[Path], skipped: list[tuple[str, str]], out_dir: Path) -> None:
    lines = [
        "# Barren Mask Sources",
        "",
        f"Scale: {scale}x nearest-neighbor.",
        "",
        "Paint masks using exact flat colors:",
        "",
        "- Lava: #ff0000",
        "- Cliff: #00ff00",
        "- Shore/bank: #0000ff",
        "- Leave unpainted for ground/unchanged background.",
        "- Leave transparent empty areas transparent; they mean no tile / no art.",
        "",
        "Do not resize, crop, blur, or anti-alias after painting.",
        "Save painted masks as PNGs next to the source image or in a sibling folder.",
        "",
        f"Exported: {len(exported)} PNGs.",
        f"Skipped: {len(skipped)} assets.",
        "",
        "Metadata outputs:",
        "",
        "- `metadata.json`: template/cell terrain labels and target types.",
        "- `metadata.csv`: flat cell table.",
        "- `label-overlays/`: source PNGs with grid labels.",
    ]
    if skipped:
        lines.extend(["", "Skipped assets:"])
        lines.extend(f"- {image}: {reason}" for image, reason in skipped)

    (out_dir / "README.md").write_text("\n".join(lines) + "\n", newline="\n")


if __name__ == "__main__":
    sys.exit(main())
