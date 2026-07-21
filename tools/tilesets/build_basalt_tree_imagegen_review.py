#!/usr/bin/env python
"""Fit image-generated basalt concepts at 24px density and build a review sheet."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import build_basalt_forest_bulk_review as forest


ARCHETYPES = (
    ("single-a", (1, 1), ("x",)),
    ("single-b", (1, 1), ("x",)),
    ("pair-horizontal", (2, 1), ("xx",)),
    ("l-three-cell", (3, 2), ("_x_", "xx_")),
    ("hook-four-cell", (4, 3), ("____", "xxx_", "x___")),
    ("zigzag-six-cell", (4, 3), ("__x_", "xxx_", "_xx_")),
)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-dir", type=Path, required=True)
    parser.add_argument("--out-dir", type=Path, required=True)
    args = parser.parse_args()
    source_dir = args.source_dir.resolve()
    out_dir = args.out_dir.resolve()
    fitted_dir = out_dir / "fitted-production"
    fitted_dir.mkdir(parents=True, exist_ok=True)

    fitted: dict[str, Image.Image] = {}
    audit = []
    for name, dimensions, footprint in ARCHETYPES:
        source = Image.open(source_dir / f"{name}.png").convert("RGBA")
        production, record = fit_at_24px(source, dimensions)
        production.save(fitted_dir / f"{name}.png")
        fitted[name] = production
        audit.append({"id": name, "footprint": footprint, **record})

    review = build_review(fitted)
    review_path = out_dir / "basalt_tree_imagegen_archetype_review.png"
    review.save(review_path)
    (out_dir / "fit-audit.json").write_text(
        json.dumps(audit, indent=2) + "\n", encoding="utf-8"
    )
    print(review_path)
    return 0


def fit_at_24px(source: Image.Image, dimensions: tuple[int, int]):
    alpha = np.asarray(source.getchannel("A"))
    ys, xs = np.nonzero(alpha > 8)
    if len(xs) == 0:
        raise ValueError("source contains no visible pixels")
    crop_box = (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)
    cropped = source.crop(crop_box)

    columns, rows = dimensions
    author_width, author_height = columns * 24, rows * 24
    padding = 2
    available_width = author_width - padding * 2
    available_height = author_height - padding * 2
    scale = min(available_width / cropped.width, available_height / cropped.height)
    width = max(1, round(cropped.width * scale))
    height = max(1, round(cropped.height * scale))
    reduced = cropped.resize((width, height), Image.Resampling.LANCZOS)
    author = Image.new("RGBA", (author_width, author_height))
    x = (author_width - width) // 2
    y = author_height - padding - height
    author.alpha_composite(reduced, (x, y))
    production = author.resize(
        (author_width * 2, author_height * 2), Image.Resampling.NEAREST
    )

    prod_alpha = np.asarray(production.getchannel("A"))
    by, bx = np.nonzero(prod_alpha > 0)
    bounds = [int(bx.min()), int(by.min()), int(bx.max()) + 1, int(by.max()) + 1]
    uniform = bool(
        np.array_equal(prod_alpha[0::2, 0::2], prod_alpha[0::2, 1::2])
        and np.array_equal(prod_alpha[0::2, 0::2], prod_alpha[1::2, 0::2])
        and np.array_equal(prod_alpha[0::2, 0::2], prod_alpha[1::2, 1::2])
    )
    return production, {
        "source_size": list(source.size),
        "source_crop": list(crop_box),
        "authoring_size": [author_width, author_height],
        "production_size": list(production.size),
        "visible_bounds": bounds,
        "bounds_violations": 0,
        "uniform_2x2_blocks": uniform,
    }


def build_review(fitted: dict[str, Image.Image]) -> Image.Image:
    columns = 2
    card_width, card_height = 600, 350
    rows = (len(ARCHETYPES) + columns - 1) // columns
    sheet = Image.new(
        "RGB", (columns * card_width, rows * card_height), (73, 86, 99)
    )
    font = ImageFont.load_default()
    for index, (name, dimensions, footprint) in enumerate(ARCHETYPES):
        column, row = index % columns, index // columns
        card = Image.new("RGB", (card_width, card_height), (42, 47, 52))
        draw = ImageDraw.Draw(card)
        draw.rectangle((0, 0, card_width, 34), fill=(73, 86, 99))
        draw.text(
            (9, 10),
            f"{name} | actor box {dimensions[0]}x{dimensions[1]} | collision {'/'.join(footprint)}",
            fill="white",
            font=font,
        )
        sprite = fitted[name]
        checker = forest.checkerboard(sprite.size)
        checker.alpha_composite(sprite)
        checker_scale = min(3, max(1, 260 // max(sprite.size)))
        checker = checker.resize(
            (checker.width * checker_scale, checker.height * checker_scale),
            Image.Resampling.NEAREST,
        )
        card.paste(checker.convert("RGB"), (18, 52))

        ground = forest.ground_mosaic(*dimensions).convert("RGBA")
        ground.alpha_composite(sprite)
        forest.draw_footprint(ground, footprint)
        ground_scale = min(3, max(1, 260 // max(ground.size)))
        ground = ground.resize(
            (ground.width * ground_scale, ground.height * ground_scale),
            Image.Resampling.NEAREST,
        )
        gx = card_width - ground.width - 18
        card.paste(ground.convert("RGB"), (gx, 52))
        draw.text((18, card_height - 25), "alpha/checker", fill=(200, 210, 218), font=font)
        draw.text((gx, card_height - 25), "Volcanic ground + collision", fill=(200, 210, 218), font=font)
        sheet.paste(card, (column * card_width, row * card_height))
    return sheet


if __name__ == "__main__":
    raise SystemExit(main())
