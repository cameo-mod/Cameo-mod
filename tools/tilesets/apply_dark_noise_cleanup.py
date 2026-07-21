#!/usr/bin/env python
"""Replace reviewed isolated dark cliff pixels with local rock tones."""

from __future__ import annotations

import argparse
from pathlib import Path
from statistics import median

from PIL import Image, ImageDraw, ImageFont

from shptd import write_shptd


ROOT = Path(__file__).resolve().parents[2]
TILE = 48


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", type=Path, required=True)
    parser.add_argument("--cleanup-mask", type=Path, required=True)
    parser.add_argument("--semantic-mask", type=Path, required=True)
    parser.add_argument("--palette", type=Path, required=True)
    parser.add_argument("--out-dir", type=Path, required=True)
    args = parser.parse_args()

    base = load_native(args.base)
    cleanup = load_native(args.cleanup_mask)
    semantic = load_native(args.semantic_mask)
    palette = read_palette(resolve(args.palette))
    base_pixels = list(base.get_flattened_data())
    cleanup_pixels = list(cleanup.get_flattened_data())
    cliff = [is_cliff(pixel) for pixel in semantic.get_flattened_data()]
    marked = [is_cyan(pixel) for pixel in cleanup_pixels]
    out_pixels = list(base_pixels)

    for i, should_change in enumerate(marked):
        if not should_change or not cliff[i]:
            continue
        x, y = i % base.width, i // base.width
        neighbors = []
        for oy in range(-3, 4):
            for ox in range(-3, 4):
                nx, ny = x + ox, y + oy
                if not (0 <= nx < base.width and 0 <= ny < base.height):
                    continue
                n = ny * base.width + nx
                pixel = base_pixels[n]
                if cliff[n] and not marked[n] and luminance(pixel) > 24 and not is_rust(pixel):
                    neighbors.append(pixel)
        if not neighbors:
            continue
        target_luma = median(luminance(pixel) for pixel in neighbors)
        local_colors = {pixel[:3] for pixel in neighbors}
        color = min(local_colors, key=lambda candidate: abs(luminance(candidate) - target_luma))
        out_pixels[i] = (*color, base_pixels[i][3])

    result = Image.new("RGBA", base.size)
    result.putdata(out_pixels)
    out_dir = resolve(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    result.save(out_dir / "native-dark-noise-cleaned.png")
    result.resize((576, 384), Image.Resampling.NEAREST).save(out_dir / "x4-dark-noise-cleaned.png")
    write_preview_vol(out_dir / "s09-dark-noise-cleaned-preview.vol", result, palette)
    write_review(out_dir / "dark-noise-cleanup-review.png", base, cleanup, result)

    changed = [i for i, pixel in enumerate(out_pixels) if pixel != base_pixels[i]]
    print((out_dir / "dark-noise-cleanup-review.png").resolve())
    print(f"marks={sum(marked)}; changed={len(changed)}; outside-mask={sum(1 for i in changed if not marked[i])}")
    return 0


def resolve(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def load_native(path: Path) -> Image.Image:
    image = Image.open(resolve(path)).convert("RGBA")
    return image if image.size == (144, 96) else image.resize((144, 96), Image.Resampling.NEAREST)


def read_palette(path: Path) -> list[tuple[int, int, int]]:
    data = path.read_bytes()
    return [tuple(data[offset + channel] * 4 for channel in range(3)) for offset in range(0, 768, 3)]


def is_cliff(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, _ = pixel
    return g > 220 and r < 40 and b < 40


def is_cyan(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, _ = pixel
    return g > 220 and b > 220 and r < 40


def is_rust(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, _ = pixel
    return r >= g + 32 and r >= b + 40


def luminance(pixel: tuple[int, ...]) -> float:
    return 0.299 * pixel[0] + 0.587 * pixel[1] + 0.114 * pixel[2]


def write_preview_vol(path: Path, image: Image.Image, palette: list[tuple[int, int, int]]) -> None:
    index_by_color = {color: i for i, color in enumerate(palette)}
    frames = []
    for cell_y in range(2):
        for cell_x in range(3):
            frame = bytearray()
            for y in range(TILE):
                for x in range(TILE):
                    frame.append(index_by_color[image.getpixel((cell_x * TILE + x, cell_y * TILE + y))[:3]])
            frames.append(bytes(frame))
    write_shptd(path, TILE, TILE, frames)


def write_review(path: Path, base: Image.Image, mask: Image.Image, result: Image.Image) -> None:
    panels = [("before", base), ("reviewed six-pixel mask", mask), ("after", result)]
    scale, header = 4, 24
    width, height = base.width * scale, base.height * scale
    sheet = Image.new("RGBA", (width * 3, height + header), (73, 86, 99, 255))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for i, (label, image) in enumerate(panels):
        x = i * width
        draw.text((x + 5, 5), label, fill="white", font=font)
        sheet.alpha_composite(image.resize((width, height), Image.Resampling.NEAREST), (x, header))
    sheet.save(path)


if __name__ == "__main__":
    raise SystemExit(main())
