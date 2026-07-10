#!/usr/bin/env python
"""Apply a reviewed AI edge mask without changing terrain geometry."""

from __future__ import annotations

import argparse
from pathlib import Path
from statistics import median

from PIL import Image, ImageDraw, ImageFont

from shptd import write_shptd


ROOT = Path(__file__).resolve().parents[2]
MAGENTA = (255, 0, 255, 255)
TILE = 48


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", type=Path, required=True)
    parser.add_argument("--correction-mask", type=Path, required=True)
    parser.add_argument("--semantic-mask", type=Path, required=True)
    parser.add_argument("--edge-reference", type=Path, required=True)
    parser.add_argument("--edge-reference-mask", type=Path, required=True)
    parser.add_argument("--palette", type=Path, required=True)
    parser.add_argument("--out-dir", type=Path, required=True)
    args = parser.parse_args()

    base = load_native(args.base, (144, 96))
    correction = load_native(args.correction_mask, base.size)
    semantic = load_native(args.semantic_mask, base.size)
    edge_reference = Image.open(resolve(args.edge_reference)).convert("RGBA")
    edge_reference_mask = load_native(args.edge_reference_mask, edge_reference.size)
    palette = read_palette(resolve(args.palette))

    cliff = [is_cliff(pixel) for pixel in semantic.get_flattened_data()]
    distances = distance_from_non_cliff(cliff, base.width, base.height, 4)
    marked = [is_marked(pixel) for pixel in correction.get_flattened_data()]
    ramp = reference_edge_ramp(edge_reference, edge_reference_mask, palette)
    base_pixels = list(base.get_flattened_data())
    out_pixels = list(base_pixels)

    for i, should_change in enumerate(marked):
        if not should_change or not cliff[i]:
            continue
        x, y = i % base.width, i // base.width
        inward = neighboring_indices(x, y, base.width, base.height, cliff, distances, marked)
        if not inward:
            continue
        target_luma = clamp(median(luminance(base_pixels[n]) for n in inward) - 2, 30, 50)
        color = min(ramp, key=lambda candidate: abs(luminance(candidate) - target_luma))
        out_pixels[i] = (*color, base_pixels[i][3])

    result = Image.new("RGBA", base.size)
    result.putdata(out_pixels)
    out_dir = resolve(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    result.save(out_dir / "native-edge-corrected.png")
    result.resize((576, 384), Image.Resampling.NEAREST).save(out_dir / "x4-edge-corrected.png")
    write_preview_vol(out_dir / "s09-ai-edge-corrected-preview.vol", result, palette)
    write_review(out_dir / "edge-correction-review.png", base, correction, result, edge_reference)

    changed = [i for i, pixel in enumerate(out_pixels) if pixel != base_pixels[i]]
    outside_mask = sum(1 for i in changed if not marked[i])
    outside_cliff = sum(1 for i in changed if not cliff[i])
    print((out_dir / "edge-correction-review.png").resolve())
    print(f"mask marks={sum(marked)}; changed={len(changed)}; outside-mask={outside_mask}; outside-cliff={outside_cliff}")
    return 0


def resolve(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def load_native(path: Path, size: tuple[int, int]) -> Image.Image:
    image = Image.open(resolve(path)).convert("RGBA")
    return image if image.size == size else image.resize(size, Image.Resampling.NEAREST)


def read_palette(path: Path) -> list[tuple[int, int, int]]:
    data = path.read_bytes()
    if len(data) != 768:
        raise ValueError(f"expected 768-byte palette, got {len(data)}")
    return [tuple(data[offset + channel] * 4 for channel in range(3)) for offset in range(0, 768, 3)]


def is_cliff(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, _ = pixel
    return g > 220 and r < 40 and b < 40


def is_marked(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, _ = pixel
    return r > 220 and b > 220 and g < 40


def is_rust(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, _ = pixel
    return r >= g + 32 and r >= b + 40


def luminance(pixel: tuple[int, ...]) -> float:
    return 0.299 * pixel[0] + 0.587 * pixel[1] + 0.114 * pixel[2]


def clamp(value: float, low: float, high: float) -> float:
    return max(low, min(high, value))


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
    marked: list[bool],
) -> list[int]:
    found = []
    for oy in range(-3, 4):
        for ox in range(-3, 4):
            nx, ny = x + ox, y + oy
            if 0 <= nx < width and 0 <= ny < height:
                i = ny * width + nx
                if cliff[i] and distances[i] >= 2 and not marked[i]:
                    found.append(i)
    return found


def reference_edge_ramp(
    image: Image.Image,
    mask: Image.Image,
    palette: list[tuple[int, int, int]],
) -> list[tuple[int, int, int]]:
    cliff = [is_cliff(pixel) for pixel in mask.get_flattened_data()]
    distances = distance_from_non_cliff(cliff, mask.width, mask.height, 2)
    palette_set = set(palette)
    colors = {
        pixel[:3]
        for i, pixel in enumerate(image.get_flattened_data())
        if cliff[i]
        and distances[i] <= 2
        and 24 <= luminance(pixel) <= 58
        and not is_rust(pixel)
        and pixel[:3] in palette_set
    }
    if not colors:
        raise ValueError("edge reference yielded no usable palette colors")
    return sorted(colors, key=luminance)


def write_preview_vol(path: Path, image: Image.Image, palette: list[tuple[int, int, int]]) -> None:
    index_by_color = {color: i for i, color in enumerate(palette)}
    frames = []
    for cell_y in range(image.height // TILE):
        for cell_x in range(image.width // TILE):
            frame = bytearray()
            for y in range(TILE):
                for x in range(TILE):
                    pixel = image.getpixel((cell_x * TILE + x, cell_y * TILE + y))
                    frame.append(index_by_color[pixel[:3]])
            frames.append(bytes(frame))
    write_shptd(path, TILE, TILE, frames)


def write_review(path: Path, base: Image.Image, correction: Image.Image, result: Image.Image, reference: Image.Image) -> None:
    scale, header = 4, 24
    width, height = base.width * scale, base.height * scale
    target_panel = Image.new("RGBA", base.size, (38, 42, 44, 255))
    target = reference
    target_panel.alpha_composite(target, ((base.width - target.width) // 2, (base.height - target.height) // 2))
    panels = [("v9 source", base), ("approved mask", correction), ("corrected preview", result), ("GIMP edge reference", target_panel)]
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
