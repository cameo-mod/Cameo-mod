#!/usr/bin/env python
"""Transfer AI cliff tones onto an unchanged semantic cliff geometry."""

from __future__ import annotations

import argparse
from pathlib import Path
from statistics import median

from PIL import Image, ImageDraw, ImageFont

from shptd import write_shptd


ROOT = Path(__file__).resolve().parents[2]
TILE = 48
SCALE = 4


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", type=Path, required=True)
    parser.add_argument("--ai-reference", type=Path, required=True)
    parser.add_argument("--donor", type=Path, required=True)
    parser.add_argument("--mask", type=Path, required=True)
    parser.add_argument("--edge-reference", type=Path, required=True)
    parser.add_argument("--edge-reference-mask", type=Path, required=True)
    parser.add_argument("--palette", type=Path, required=True)
    parser.add_argument("--out-dir", type=Path, required=True)
    args = parser.parse_args()

    base = load_native(args.base)
    ai = load_native(args.ai_reference)
    donor = load_native(args.donor)
    mask = load_native(args.mask)
    edge_reference = load_native(args.edge_reference)
    edge_reference_mask = load_native(args.edge_reference_mask)
    if len({base.size, ai.size, donor.size, mask.size}) != 1:
        raise ValueError(f"input size mismatch: {base.size}, {ai.size}, {donor.size}, {mask.size}")

    palette = read_palette(resolve(args.palette))
    cliff = [is_cliff(pixel) for pixel in mask.get_flattened_data()]
    edge_distance = distance_from_non_cliff(cliff, base.width, base.height, 4)
    base_pixels = list(base.get_flattened_data())
    donor_pixels = list(donor.get_flattened_data())

    edge_ramp = reference_edge_ramp(edge_reference, edge_reference_mask, palette)

    out_pixels = list(base_pixels)
    for i, is_rock in enumerate(cliff):
        if not is_rock or edge_distance[i] > 2:
            continue

        x, y = i % base.width, i // base.width
        base_luma = luminance(base_pixels[i])
        inward = neighboring_indices(x, y, base.width, base.height, cliff, edge_distance, minimum_distance=2)
        if not inward:
            continue

        base_inward = median(luminance(base_pixels[n]) for n in inward)
        donor_inward = median(luminance(donor_pixels[n]) for n in inward)
        donor_luma = luminance(donor_pixels[i])
        donor_supports_highlight = donor_luma >= donor_inward + 10
        donor_supports_shadow = donor_luma <= donor_inward - 12
        unsupported_light = (base_luma >= 48 or base_luma >= base_inward + 5) and not donor_supports_highlight
        unsupported_dark = (base_luma <= 26 or base_luma <= base_inward - 12) and not donor_supports_shadow
        if not unsupported_light and not unsupported_dark:
            continue

        target_luma = clamp(base_inward - 2, 30, 50)
        color = min(edge_ramp, key=lambda item: abs(luminance(item) - target_luma))
        out_pixels[i] = (*color, base_pixels[i][3])

    result = Image.new("RGBA", base.size)
    result.putdata(out_pixels)
    out_dir = resolve(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    result.save(out_dir / "native-geometry-transfer.png")
    result.resize((result.width * SCALE, result.height * SCALE), Image.Resampling.NEAREST).save(out_dir / "x4-geometry-transfer.png")
    write_preview_vol(out_dir / "s09-ai-style-geometry-preview.vol", result, palette)
    write_review(out_dir / "geometry-transfer-review.png", donor, base, ai, result)

    outside_changes = sum(
        1 for i, is_rock in enumerate(cliff)
        if not is_rock and out_pixels[i] != base_pixels[i]
    )
    inside_changes = sum(
        1 for i, is_rock in enumerate(cliff)
        if is_rock and out_pixels[i] != base_pixels[i]
    )
    print((out_dir / "geometry-transfer-review.png").resolve())
    print(f"outside changes={outside_changes}; inside changes={inside_changes}")
    return 0


def resolve(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def load_native(path: Path) -> Image.Image:
    image = Image.open(resolve(path)).convert("RGBA")
    if image.width % SCALE == 0 and image.height % SCALE == 0 and image.width > 144:
        image = image.resize((image.width // SCALE, image.height // SCALE), Image.Resampling.NEAREST)
    return image


def read_palette(path: Path) -> list[tuple[int, int, int]]:
    data = path.read_bytes()
    if len(data) != 768:
        raise ValueError(f"expected 768-byte palette, got {len(data)}")
    return [tuple(data[offset + channel] * 4 for channel in range(3)) for offset in range(0, 768, 3)]


def is_cliff(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, _ = pixel
    return g > 220 and r < 40 and b < 40


def is_rust(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, _ = pixel
    return r >= g + 32 and r >= b + 40


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
        raise ValueError("edge reference did not yield any palette colors")
    return sorted(colors, key=luminance)


def clamp(value: float, low: float, high: float) -> float:
    return max(low, min(high, value))


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
    for oy in range(-2, 3):
        for ox in range(-2, 3):
            nx, ny = x + ox, y + oy
            if 0 <= nx < width and 0 <= ny < height:
                i = ny * width + nx
                if cliff[i] and distances[i] >= minimum_distance:
                    found.append(i)
    return found


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


def write_review(path: Path, donor: Image.Image, base: Image.Image, ai: Image.Image, result: Image.Image) -> None:
    panels = [("Temperate donor", donor), ("v9 geometry", base), ("AI concept reference", ai), ("directional edge transfer", result)]
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
