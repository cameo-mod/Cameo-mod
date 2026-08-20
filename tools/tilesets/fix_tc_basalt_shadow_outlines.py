#!/usr/bin/env python
"""Remove invalid north-facing cast-shadow fringes from Volcanic tc clusters."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import generate_sh04_alpha_beach_prototype as shore
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
ACTORS = ("tc01", "tc02", "tc03", "tc04", "tc05")
SHADOW_INDEX = 4


def remove_north_shadow_fringe(indices: np.ndarray, depth: int) -> tuple[np.ndarray, np.ndarray]:
    """Remove shadow pixels that sit directly north of opaque formation pixels.

    Volcanic basalt shadows face due east.  A shadow pixel with formation body
    immediately below it is therefore an overlap artifact from a GIMP stamp,
    not valid ground shadow.
    """
    body = (indices != 0) & (indices != SHADOW_INDEX)
    shadow = indices == SHADOW_INDEX
    body_below = np.zeros_like(body)
    for distance in range(1, depth + 1):
        body_below[:-distance] |= body[distance:]

    removed = shadow & body_below
    result = indices.copy()
    result[removed] = 0
    return result, removed


def rgba(indices: np.ndarray, palette: np.ndarray) -> Image.Image:
    rgb = palette[indices].copy()
    rgb[indices == SHADOW_INDEX] = 0
    alpha = np.where(indices != 0, 255, 0).astype(np.uint8)
    alpha[indices == SHADOW_INDEX] = 105
    return Image.fromarray(np.dstack((rgb, alpha)), mode="RGBA")


def ground_mosaic(width: int, height: int, palette: np.ndarray) -> Image.Image:
    tw, th, frames = read_shptd(ROOT / "mods/cameo/bits/volcanic/clear1.vol")
    tile = np.frombuffer(frames[0], dtype=np.uint8).reshape(th, tw)
    tile_rgb = Image.fromarray(palette[tile], mode="RGB")
    result = Image.new("RGB", (width, height))
    for y in range(0, height, th):
        for x in range(0, width, tw):
            result.paste(tile_rgb, (x, y))
    return result.convert("RGBA")


def checkerboard(size: tuple[int, int], cell: int = 6) -> Image.Image:
    result = Image.new("RGBA", size)
    draw = ImageDraw.Draw(result)
    colors = ((174, 174, 174, 255), (220, 220, 220, 255))
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=colors[(x // cell + y // cell) % 2])
    return result


def panel(indices: np.ndarray, palette: np.ndarray, background: str) -> Image.Image:
    sprite = rgba(indices, palette)
    base = ground_mosaic(sprite.width, sprite.height, palette) if background == "ground" else checkerboard(sprite.size)
    base.alpha_composite(sprite)
    return base.resize((sprite.width * 3, sprite.height * 3), Image.Resampling.NEAREST).convert("RGB")


def diagnostic(indices: np.ndarray, removed: np.ndarray, palette: np.ndarray) -> Image.Image:
    image = rgba(indices, palette)
    data = np.asarray(image).copy()
    data[removed] = (255, 0, 255, 255)
    return Image.fromarray(data, mode="RGBA").resize(
        (image.width * 3, image.height * 3), Image.Resampling.NEAREST
    ).convert("RGB")


def transparent_diagnostic(indices: np.ndarray, palette: np.ndarray) -> Image.Image:
    """Highlight every non-opaque runtime pixel, including empty background."""
    image = rgba(indices, palette)
    data = np.asarray(image).copy()
    data[(indices == 0) | (indices == SHADOW_INDEX)] = (255, 0, 255, 255)
    return Image.fromarray(data, mode="RGBA").resize(
        (image.width * 3, image.height * 3), Image.Resampling.NEAREST
    ).convert("RGB")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out-dir", type=Path, required=True)
    parser.add_argument("--depth", type=int, default=2)
    parser.add_argument(
        "--remove-all-translucent",
        action="store_true",
        help="Turn every index-4 partially transparent pixel fully transparent.",
    )
    parser.add_argument("--install-dir", type=Path)
    args = parser.parse_args()

    out = args.out_dir.resolve()
    out.mkdir(parents=True, exist_ok=True)
    install = args.install_dir.resolve() if args.install_dir else None
    palette = np.asarray(shore.read_palette(ROOT / "mods/cameo/bits/volcanic/volcanic.pal"), dtype=np.uint8)
    records = []
    rows = []
    font = ImageFont.load_default()

    for actor in ACTORS:
        source = ROOT / f"mods/cameo/bits/volcanic/{actor}.vol"
        width, height, frames = read_shptd(source)
        production = np.frombuffer(frames[0], dtype=np.uint8).reshape(height, width)
        authoring = production[0::2, 0::2]
        if not all(np.array_equal(authoring, production[dy::2, dx::2]) for dy, dx in ((0, 1), (1, 0), (1, 1))):
            raise ValueError(f"{actor}: production lacks strict 2x2 cadence")

        if args.remove_all_translucent:
            removed = authoring == SHADOW_INDEX
            fixed = authoring.copy()
            fixed[removed] = 0
        else:
            fixed, removed = remove_north_shadow_fringe(authoring, args.depth)
        fixed_production = np.repeat(np.repeat(fixed, 2, axis=0), 2, axis=1)
        write_shptd(out / f"{actor}.vol", width, height, [fixed_production.tobytes()] * len(frames))
        if install:
            install.mkdir(parents=True, exist_ok=True)
            (install / f"{actor}.vol").write_bytes((out / f"{actor}.vol").read_bytes())

        rgba(fixed, palette).save(out / f"{actor}_fixed_24px.png")
        records.append({
            "actor": actor,
            "shadow_pixels_before": int((authoring == SHADOW_INDEX).sum()),
            "shadow_pixels_removed": int(removed.sum()),
            "shadow_pixels_after": int((fixed == SHADOW_INDEX).sum()),
            "body_pixels_changed": int(np.count_nonzero((authoring != fixed) & (authoring != SHADOW_INDEX))),
            "strict_2x2": True,
            "frame_count": len(frames),
        })

        images = [
            panel(authoring, palette, "ground"),
            panel(fixed, palette, "ground"),
            panel(authoring, palette, "checker"),
            transparent_diagnostic(authoring, palette),
            diagnostic(authoring, removed, palette),
        ]
        header = 28
        row = Image.new("RGB", (sum(i.width for i in images), header + max(i.height for i in images)), (73, 86, 99))
        draw = ImageDraw.Draw(row)
        labels = (
            f"{actor} current ground",
            "candidate ground",
            "current transparent",
            "ALL transparent pixels (magenta)",
            "candidate removes (magenta)",
        )
        x = 0
        for label, image in zip(labels, images):
            draw.text((x + 5, 8), label, fill="white", font=font)
            row.paste(image, (x, header))
            x += image.width
        rows.append(row)

    sheet = Image.new("RGB", (max(r.width for r in rows), sum(r.height for r in rows)), (43, 48, 53))
    y = 0
    for row in rows:
        sheet.paste(row, (0, y))
        y += row.height
    sheet.save(out / "tc01-tc05_shadow_outline_before_after.png")
    (out / "audit.json").write_text(json.dumps({
        "mode": "remove-all-translucent" if args.remove_all_translucent else "remove-north-fringe",
        "depth": args.depth,
        "actors": records,
    }, indent=2) + "\n", encoding="utf-8")
    print(out / "tc01-tc05_shadow_outline_before_after.png")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
