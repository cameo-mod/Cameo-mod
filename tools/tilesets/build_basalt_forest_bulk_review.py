#!/usr/bin/env python
"""Package approved basalt formations as preview-only map-generator forests."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import generate_sh04_alpha_beach_prototype as shore
from shptd import read_shptd


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_HANDOFF = Path.home() / "Documents/agents/volcanic-theater/basalt-columns-codex/handoff-shoreline-decoration-codex"

TREE_MAPPINGS = (
    # actor, dimensions, footprint rows, authored formation, placement pixels
    ("t01", (2, 2), ("__", "x_"), "1x1-a", (0, 48)),
    ("t02", (2, 2), ("__", "x_"), "1x1-b", (0, 48)),
    ("t03", (2, 2), ("__", "x_"), "1x1-a", (0, 48)),
    ("t05", (2, 2), ("__", "x_"), "1x1-b", (0, 48)),
    ("t06", (2, 2), ("__", "x_"), "1x1-a", (0, 48)),
    ("t07", (2, 2), ("__", "x_"), "1x1-b", (0, 48)),
    ("t08", (2, 1), ("x_",), "1x1-a", (0, 0)),
    ("t10", (2, 2), ("__", "xx"), "2x1-a", (0, 48)),
    ("t11", (2, 2), ("__", "xx"), "2x1-b", (0, 48)),
    ("t12", (2, 2), ("__", "x_"), "1x1-a", (0, 48)),
    ("t13", (2, 2), ("__", "x_"), "1x1-b", (0, 48)),
    ("t14", (2, 2), ("__", "x_"), "1x1-a", (0, 48)),
    ("t15", (3, 2), ("___", "xx_"), "2x1-b", (0, 48)),
    ("t16", (2, 2), ("__", "x_"), "1x1-b", (0, 48)),
    ("t17", (2, 2), ("__", "x_"), "1x1-a", (0, 48)),
    ("tc01", (3, 2), ("___", "xx_"), "2x1-a", (0, 48)),
    ("tc02", (3, 2), ("_x_", "xx_"), "2x2-a", (0, 0)),
    ("tc03", (3, 2), ("_x_", "xx_"), "2x2-b", (0, 0)),
    ("tc04", (4, 3), ("____", "xxx_", "x___"), "3x2-a", (0, 48)),
    ("tc05", (4, 3), ("__x_", "xxx_", "_xx_"), "3x2-b", (0, 48)),
)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--handoff", type=Path, default=DEFAULT_HANDOFF)
    parser.add_argument("--out-dir", type=Path, required=True)
    args = parser.parse_args()
    handoff = args.handoff.resolve()
    out = args.out_dir.resolve()
    out.mkdir(parents=True, exist_ok=True)
    manifest = json.loads((handoff / "manifest.json").read_text(encoding="utf-8"))
    sprites = {
        item["id"]: Image.open(
            handoff / item["directory"] / "footprint/combined-ground.png"
        ).convert("RGBA")
        for item in manifest["variants"]
    }

    family = write_family_grid(sprites)
    family.save(out / "basalt_forest_actor_family.png")
    clusters = write_cluster_review(sprites)
    clusters.save(out / "basalt_forest_density_review.png")
    comparison = write_temperate_volcanic_comparison(sprites)
    comparison.save(out / "temperate_trees_vs_volcanic_basalt_actor_comparison.png")
    candidate = {
        "status": "preview-only actor collection proposal",
        "source": str(handoff),
        "art_rule": "use authored footprint/combined-ground.png without regeneration or mirroring",
        "terrain_role": "Trees collection replacement; rock-like, impassable, nonflammable",
        "collision_rule": "preserve each original Temperate actor footprint; formation image bounds do not define passability",
        "weights": {
            "1x1-a": 120, "1x1-b": 120,
            "2x1-a": 70, "2x1-b": 70, "1x2-a": 70, "1x2-b": 70,
            "2x2-a": 40, "2x2-b": 40,
            "2x3-a": 15, "2x3-b": 15, "3x2-a": 15, "3x2-b": 15,
        },
    }
    (out / "basalt_forest_actor_proposal.json").write_text(
        json.dumps(candidate, indent=2) + "\n", encoding="utf-8"
    )
    print((out / "basalt_forest_actor_family.png").resolve())
    print((out / "basalt_forest_density_review.png").resolve())
    print((out / "temperate_trees_vs_volcanic_basalt_actor_comparison.png").resolve())
    return 0


def write_family_grid(sprites):
    columns = 2
    cell_width, cell_height, header = 168, 168, 22
    rows = (len(sprites) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * cell_width, rows * (cell_height + header)), (73, 86, 99))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for index, (name, sprite) in enumerate(sprites.items()):
        column, row = index % columns, index // columns
        x, y = column * cell_width, row * (cell_height + header)
        draw.text((x + 6, y + 6), name, fill="white", font=font)
        checker = checkerboard((cell_width, cell_height))
        checker.alpha_composite(sprite, ((cell_width - sprite.width) // 2, (cell_height - sprite.height) // 2))
        sheet.paste(checker.convert("RGB"), (x, y + header))
    return sheet


def write_cluster_review(sprites):
    labels = ("Sparse basalt field", "Standard basalt forest", "Dense basalt barrier")
    placements = (
        (("1x1-a", 1, 1), ("2x1-b", 4, 1), ("1x2-a", 8, 3), ("1x1-b", 3, 5)),
        (("2x2-a", 1, 1), ("1x1-b", 4, 1), ("2x1-a", 6, 2), ("1x2-b", 3, 4), ("2x2-b", 7, 5)),
        (("2x3-a", 1, 1), ("3x2-b", 4, 1), ("2x2-a", 7, 3), ("1x2-a", 3, 5), ("2x1-b", 5, 6)),
    )
    width, height, header = 12 * 48, 8 * 48, 26
    sheet = Image.new("RGB", (width, len(labels) * (height + header)), (73, 86, 99))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    ground = ground_mosaic(12, 8).convert("RGBA")
    for row, (label, entries) in enumerate(zip(labels, placements)):
        y = row * (height + header)
        draw.text((7, y + 7), label, fill="white", font=font)
        panel = ground.copy()
        for name, x, py in entries:
            panel.alpha_composite(sprites[name], (x * 48, py * 48))
        sheet.paste(panel.convert("RGB"), (0, y + header))
    return sheet


def write_temperate_volcanic_comparison(sprites):
    """Compare actual RA tree actors with footprint-matched basalt proposals."""
    cards = [write_actor_card(item, sprites) for item in TREE_MAPPINGS]
    columns = 2
    gap = 18
    header = 54
    cell_width = max(card.width for card in cards)
    cell_height = max(card.height for card in cards)
    rows = (len(cards) + columns - 1) // columns
    sheet = Image.new(
        "RGB",
        (
            columns * cell_width + (columns + 1) * gap,
            header + rows * cell_height + (rows + 1) * gap,
        ),
        (28, 32, 36),
    )
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    draw.rectangle((0, 0, sheet.width, header), fill=(73, 86, 99))
    draw.text(
        (gap, 20),
        "Actual RA Temperate tree actors vs footprint-preserving Volcanic basalt proposals",
        fill="white",
        font=font,
    )
    for index, card in enumerate(cards):
        column, row = index % columns, index // columns
        x = gap + column * (cell_width + gap)
        y = header + gap + row * (cell_height + gap)
        sheet.paste(card, (x, y))
    return sheet


def write_actor_card(mapping, sprites):
    actor, dimensions, footprint, formation, placement = mapping
    columns, rows = dimensions
    native_width, native_height = columns * 48, rows * 48
    temperate = temperate_ground_mosaic(columns, rows).convert("RGBA")
    width, height, frames = read_shptd(
        ROOT / "mods/cameo/bits/temp" / f"{actor}.tem"
    )
    if width > native_width or height > native_height:
        raise ValueError(
            f"{actor}: sprite canvas {width}x{height} exceeds "
            f"actor dimensions {native_width}x{native_height}"
        )
    palette = shore.read_palette(
        ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    )
    indices = np.frombuffer(frames[0], dtype=np.uint8).reshape(height, width)
    tree_rgb = shore.indices_rgb(indices, palette)
    # Classic RA terrain sprites store their cast shadow in index 4. OpenRA
    # renders that as translucent black rather than the palette's chroma-key
    # green, so reproduce the engine treatment in the review.
    tree_rgb[indices == 4] = (0, 0, 0)
    alpha = np.where(indices != 0, 255, 0).astype(np.uint8)
    alpha[indices == 4] = 105
    tree_rgba = np.dstack(
        [tree_rgb, alpha]
    )
    temperate.alpha_composite(
        Image.fromarray(tree_rgba, mode="RGBA"),
        ((native_width - width) // 2, (native_height - height) // 2),
    )

    volcanic = ground_mosaic(columns, rows).convert("RGBA")
    volcanic.alpha_composite(sprites[formation], placement)
    draw_footprint(temperate, footprint)
    draw_footprint(volcanic, footprint)

    scale = 2
    view_width, view_height = 192 * scale, 144 * scale
    label_height = 48
    gap = 12
    card = Image.new(
        "RGB", (view_width * 2 + gap, label_height + view_height), (53, 60, 66)
    )
    draw = ImageDraw.Draw(card)
    font = ImageFont.load_default()
    collision = "/".join(footprint)
    note = ""
    if actor in {"tc04", "tc05"}:
        note = "; 3x2 art inside 4x3 actor box"
    draw.text(
        (6, 7),
        f"{actor}: {dimensions[0]}x{dimensions[1]} collision {collision}",
        fill="white",
        font=font,
    )
    draw.text(
        (6, 25),
        f"Temperate actor -> {formation}{note}",
        fill=(210, 220, 228),
        font=font,
    )
    for index, image in enumerate((temperate, volcanic)):
        scaled = image.resize(
            (image.width * scale, image.height * scale), Image.Resampling.NEAREST
        )
        x = index * (view_width + gap) + (view_width - scaled.width) // 2
        y = label_height + (view_height - scaled.height) // 2
        card.paste(scaled.convert("RGB"), (x, y))
    return card


def draw_footprint(image, footprint):
    draw = ImageDraw.Draw(image, "RGBA")
    for row, line in enumerate(footprint):
        for column, value in enumerate(line):
            if value != "x":
                continue
            x0, y0 = column * 48, row * 48
            draw.rectangle(
                (x0, y0, x0 + 47, y0 + 47),
                outline=(0, 230, 220, 255),
                width=1,
            )


def temperate_ground_mosaic(columns, rows):
    palette = shore.read_palette(
        ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    )
    _, _, frames = read_shptd(ROOT / "mods/cameo/bits/temp/clear1.tem")
    canvas = np.zeros((rows * 48, columns * 48, 3), dtype=np.uint8)
    for index in range(rows * columns):
        row, column = divmod(index, columns)
        indices = np.frombuffer(
            frames[index % len(frames)], dtype=np.uint8
        ).reshape(48, 48)
        canvas[
            row * 48:(row + 1) * 48,
            column * 48:(column + 1) * 48,
        ] = shore.indices_rgb(indices, palette)
    return Image.fromarray(canvas, mode="RGB")


def ground_mosaic(columns, rows):
    palette = shore.read_palette(ROOT / "mods/cameo/bits/volcanic/volcanic.pal")
    _, _, frames = read_shptd(ROOT / "mods/cameo/bits/volcanic/clear1.vol")
    canvas = np.zeros((rows * 48, columns * 48, 3), dtype=np.uint8)
    for i in range(rows * columns):
        y, x = divmod(i, columns)
        indices = np.frombuffer(frames[i % len(frames)], dtype=np.uint8).reshape(48, 48)
        canvas[y * 48:(y + 1) * 48, x * 48:(x + 1) * 48] = shore.indices_rgb(indices, palette)
    return Image.fromarray(canvas, mode="RGB")


def checkerboard(size):
    width, height = size
    yy, xx = np.indices((height, width))
    checks = ((xx // 8) + (yy // 8)) % 2 == 0
    rgb = np.empty((height, width, 4), dtype=np.uint8)
    rgb[checks] = (154, 154, 154, 255)
    rgb[~checks] = (102, 102, 102, 255)
    return Image.fromarray(rgb, mode="RGBA")


if __name__ == "__main__":
    raise SystemExit(main())
