#!/usr/bin/env python
"""Preview RA Temperate tree sprites with their exact collision footprints."""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import build_basalt_forest_bulk_review as forest
import generate_sh04_alpha_beach_prototype as shore
from shptd import read_shptd


ROOT = Path(__file__).resolve().parents[2]


def decode(actor: str) -> Image.Image:
    width, height, frames = read_shptd(ROOT / f"mods/cameo/bits/temp/{actor}.tem")
    palette = shore.read_palette(ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal")
    indices = np.frombuffer(frames[0], dtype=np.uint8).reshape(height, width)
    rgb = shore.indices_rgb(indices, palette)
    rgb[indices == 4] = (0, 0, 0)
    alpha = np.where(indices != 0, 255, 0).astype(np.uint8)
    alpha[indices == 4] = 105
    return Image.fromarray(np.dstack([rgb, alpha]), mode="RGBA")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", type=Path, required=True)
    args = parser.parse_args()

    actors = ("t10", "t11")
    scale = 3
    margin = 20
    header = 48
    cell = (96 * scale, 96 * scale)
    sheet = Image.new("RGB", (cell[0] * 4 + margin * 5, header + cell[1] + margin * 2), (43, 48, 53))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    draw.rectangle((0, 0, sheet.width, header), fill=(73, 86, 99))
    draw.text((margin, 10), "RA Temperate two-tree row: exact sprite boxes", fill="white", font=font)
    draw.text((margin, 27), "Both use 2x2 actors with collision footprint __ / xx", fill=(215, 223, 229), font=font)

    column = 0
    for actor in actors:
        sprite = decode(actor)
        sprite.save(args.out.parent / f"{actor}_temperate_frame0.png")
        checker = forest.checkerboard(sprite.size)
        checker.alpha_composite(sprite)
        ground = forest.temperate_ground_mosaic(2, 2).convert("RGBA")
        ground.alpha_composite(sprite)
        forest.draw_footprint(ground, ("__", "xx"))
        for label, panel in (("alpha", checker), ("ground + collision", ground)):
            x = margin + column * (cell[0] + margin)
            y = header + margin
            sheet.paste(panel.resize(cell, Image.Resampling.NEAREST).convert("RGB"), (x, y))
            draw.rectangle((x, y, x + cell[0] - 1, y + cell[1] - 1), outline=(230, 230, 230))
            draw.text((x, y + cell[1] + 7), f"{actor}: {label}", fill="white", font=font)
            column += 1

    args.out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(args.out)
    print(args.out.resolve())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
