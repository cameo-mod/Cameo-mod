#!/usr/bin/env python
"""Align and constrain an AI-authored cliff edge correction mask."""

from __future__ import annotations

import argparse
from pathlib import Path
from statistics import median

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
MAGENTA = (255, 0, 255, 255)
BLACK = (0, 0, 0, 255)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--ai-mask", type=Path, required=True)
    parser.add_argument("--base", type=Path, required=True)
    parser.add_argument("--donor", type=Path, required=True)
    parser.add_argument("--semantic-mask", type=Path, required=True)
    parser.add_argument("--out-dir", type=Path, required=True)
    args = parser.parse_args()

    base = load_native(args.base)
    donor = load_native(args.donor)
    semantic = load_native(args.semantic_mask)
    ai_source = Image.open(resolve(args.ai_mask)).convert("RGBA")
    ai_native = align_mask(ai_source, base.size)

    cliff = [is_cliff(pixel) for pixel in semantic.get_flattened_data()]
    distances = distance_from_non_cliff(cliff, base.width, base.height, 3)
    base_pixels = list(base.get_flattened_data())
    donor_pixels = list(donor.get_flattened_data())
    ai_pixels = list(ai_native.get_flattened_data())

    raw_pixels = [MAGENTA if is_ai_mark(pixel) else BLACK for pixel in ai_pixels]
    safe_pixels = [BLACK] * len(raw_pixels)
    for i, pixel in enumerate(raw_pixels):
        if pixel != MAGENTA or not cliff[i] or distances[i] > 2:
            continue

        x, y = i % base.width, i // base.width
        inward = neighboring_indices(x, y, base.width, base.height, cliff, distances, 3)
        if not inward:
            inward = neighboring_indices(x, y, base.width, base.height, cliff, distances, 2)
        if not inward:
            continue

        donor_inward = median(luminance(donor_pixels[n]) for n in inward)
        donor_supports_highlight = luminance(donor_pixels[i]) >= donor_inward + 8
        r, g, b, _ = base_pixels[i]
        strongly_warm = r >= g + 20 and r >= b + 28
        if not donor_supports_highlight or strongly_warm:
            safe_pixels[i] = MAGENTA

    raw = Image.new("RGBA", base.size)
    raw.putdata(raw_pixels)
    safe = Image.new("RGBA", base.size)
    safe.putdata(safe_pixels)
    overlay = base.copy()
    overlay_pixels = list(base_pixels)
    for i, pixel in enumerate(safe_pixels):
        if pixel == MAGENTA:
            overlay_pixels[i] = MAGENTA
    overlay.putdata(overlay_pixels)

    out_dir = resolve(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    raw.save(out_dir / "ai-mask-aligned-native.png")
    safe.save(out_dir / "safe-correction-mask-native.png")
    overlay.save(out_dir / "safe-mask-overlay-native.png")
    for name, image in (("ai-mask-aligned-x4.png", raw), ("safe-correction-mask-x4.png", safe), ("safe-mask-overlay-x4.png", overlay)):
        image.resize((base.width * 4, base.height * 4), Image.Resampling.NEAREST).save(out_dir / name)
    write_review(out_dir / "correction-mask-review.png", base, raw, safe, overlay)

    print((out_dir / "correction-mask-review.png").resolve())
    print(f"raw marks={raw_pixels.count(MAGENTA)}; safe marks={safe_pixels.count(MAGENTA)}")
    return 0


def resolve(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def load_native(path: Path) -> Image.Image:
    image = Image.open(resolve(path)).convert("RGBA")
    if image.size != (144, 96):
        image = image.resize((144, 96), Image.Resampling.NEAREST)
    return image


def align_mask(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    target_ratio = size[0] / size[1]
    current_ratio = image.width / image.height
    if current_ratio > target_ratio:
        width = round(image.height * target_ratio)
        left = (image.width - width) // 2
        image = image.crop((left, 0, left + width, image.height))
    elif current_ratio < target_ratio:
        height = round(image.width / target_ratio)
        top = (image.height - height) // 2
        image = image.crop((0, top, image.width, top + height))
    return image.resize(size, Image.Resampling.BOX)


def is_ai_mark(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, _ = pixel
    return r >= 48 and b >= 48 and g <= min(r, b) * 0.45


def is_cliff(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, _ = pixel
    return g > 220 and r < 40 and b < 40


def luminance(pixel: tuple[int, ...]) -> float:
    return 0.299 * pixel[0] + 0.587 * pixel[1] + 0.114 * pixel[2]


def distance_from_non_cliff(cliff: list[bool], width: int, height: int, maximum: int) -> list[int]:
    distances = [maximum + 1] * len(cliff)
    for i, value in enumerate(cliff):
        if not value:
            distances[i] = 0
    for distance in range(1, maximum + 1):
        for y in range(height):
            for x in range(width):
                i = y * width + x
                if not cliff[i] or distances[i] <= distance:
                    continue
                for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                    nx, ny = x + ox, y + oy
                    if nx < 0 or ny < 0 or nx >= width or ny >= height:
                        distances[i] = distance
                        break
                    if distances[ny * width + nx] == distance - 1:
                        distances[i] = distance
                        break
    return distances


def neighboring_indices(
    x: int,
    y: int,
    width: int,
    height: int,
    cliff: list[bool],
    distances: list[int],
    minimum_distance: int,
) -> list[int]:
    found = []
    for oy in range(-3, 4):
        for ox in range(-3, 4):
            nx, ny = x + ox, y + oy
            if 0 <= nx < width and 0 <= ny < height:
                i = ny * width + nx
                if cliff[i] and distances[i] >= minimum_distance:
                    found.append(i)
    return found


def write_review(path: Path, base: Image.Image, raw: Image.Image, safe: Image.Image, overlay: Image.Image) -> None:
    panels = [("v9 source", base), ("AI mask aligned", raw), ("safe directional mask", safe), ("overlay only", overlay)]
    scale, header = 4, 24
    width, height = base.width * scale, base.height * scale
    sheet = Image.new("RGBA", (width * 2, (height + header) * 2), (73, 86, 99, 255))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for i, (label, image) in enumerate(panels):
        x, y = (i % 2) * width, (i // 2) * (height + header)
        draw.text((x + 5, y + 5), label, fill="white", font=font)
        sheet.alpha_composite(image.resize((width, height), Image.Resampling.NEAREST), (x, y + header))
    sheet.save(path)


if __name__ == "__main__":
    raise SystemExit(main())
