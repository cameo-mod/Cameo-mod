#!/usr/bin/env python
"""Prototype a geometry-preserving volcanic recolor from matching RA theaters."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
TILE = 48
SCALE = 4
FAMILY_ROCK_RANGE = (29.196, 150.332)
FAMILY_GROUND_RANGE = (29.196, 70.544)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--temperate", type=Path, required=True)
    parser.add_argument("--snow", type=Path, required=True)
    parser.add_argument("--ai", type=Path)
    parser.add_argument("--palette", type=Path, required=True)
    parser.add_argument(
        "--clear-tile",
        type=Path,
        default=ROOT / "mods/cameo/bits/volcanic/clear1.vol",
    )
    parser.add_argument("--out-dir", type=Path, required=True)
    parser.add_argument(
        "--normalization",
        choices=("family", "per-asset"),
        default="family",
        help="Use one cliff-family luminance curve or normalize each asset independently.",
    )
    args = parser.parse_args()

    temperate = load_native(resolve(args.temperate))
    snow = load_native(resolve(args.snow))
    ai = load_native(resolve(args.ai)) if args.ai else snow.copy()
    if temperate.size != snow.size or temperate.size != ai.size:
        raise ValueError(
            f"input size mismatch: temperate={temperate.size}, snow={snow.size}, ai={ai.size}"
        )

    palette = read_palette(resolve(args.palette))
    clear_frame = read_clear_frame(resolve(args.clear_tile))
    rock_mask = classify_rock(temperate, snow)
    result, indices = recolor(
        temperate, rock_mask, palette, clear_frame, args.normalization
    )

    out_dir = resolve(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    temperate.save(out_dir / "temperate-native.png")
    snow.save(out_dir / "snow-native.png")
    if args.ai:
        ai.save(out_dir / "ai-native.png")
    rock_mask.save(out_dir / "rock-mask-native.png")
    result.save(out_dir / "luminance-recolor-native.png")

    temperate_x4 = temperate.resize(scale_size(temperate.size), Image.Resampling.NEAREST)
    snow_x4 = snow.resize(scale_size(snow.size), Image.Resampling.NEAREST)
    ai_x4 = ai.resize(scale_size(ai.size), Image.Resampling.NEAREST)
    mask_x4 = rock_mask.resize(scale_size(rock_mask.size), Image.Resampling.NEAREST)
    result_x4 = result.resize(scale_size(result.size), Image.Resampling.NEAREST)
    temperate_x4.save(out_dir / "temperate-x4.png")
    snow_x4.save(out_dir / "snow-x4.png")
    if args.ai:
        ai_x4.save(out_dir / "ai-x4.png")
    mask_x4.save(out_dir / "rock-mask-x4.png")
    result_x4.save(out_dir / "luminance-recolor-x4.png")

    asset = resolve(args.temperate).stem.split("-", 1)[-1].removesuffix("-x4")
    vol_path = out_dir / f"{asset}-luminance-preview.vol"
    frames = slice_frames(indices, result.width, result.height, temperate)
    write_shptd(vol_path, TILE, TILE, frames)
    verify_vol(vol_path, len(frames))
    write_review(
        out_dir / "three-way-review.png",
        temperate_x4,
        mask_x4,
        result_x4,
        ai_x4,
        "Current AI redraw" if args.ai else "Snow geometry reference",
    )

    rock_pixels = sum(1 for value in rock_mask.get_flattened_data() if value >= 128)
    total = rock_mask.width * rock_mask.height
    print((out_dir / "three-way-review.png").resolve())
    print(f"rock mask: {rock_pixels}/{total} pixels ({rock_pixels / total:.1%})")
    print(f"normalization: {args.normalization}")
    print(f"preview VOL: {len(frames)} frames, 48x48, palette exact")
    return 0


def resolve(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def load_native(path: Path) -> Image.Image:
    image = Image.open(path).convert("RGBA")
    if image.width % SCALE == 0 and image.height % SCALE == 0 and image.width > 144:
        return image.resize((image.width // SCALE, image.height // SCALE), Image.Resampling.NEAREST)
    return image


def scale_size(size: tuple[int, int]) -> tuple[int, int]:
    return size[0] * SCALE, size[1] * SCALE


def read_palette(path: Path) -> list[tuple[int, int, int]]:
    data = path.read_bytes()
    if len(data) != 768:
        raise ValueError(f"expected 768-byte palette, got {len(data)}")
    return [
        tuple(data[offset + channel] * 4 for channel in range(3))
        for offset in range(0, 768, 3)
    ]


def read_clear_frame(path: Path) -> bytes:
    width, height, frames = read_shptd(path)
    if (width, height) != (TILE, TILE) or not frames:
        raise ValueError(f"invalid clear tile: {path} is {width}x{height} with {len(frames)} frames")
    return frames[0]


def classify_rock(temperate: Image.Image, snow: Image.Image) -> Image.Image:
    values = []
    for temp, cold in zip(
        temperate.get_flattened_data(), snow.get_flattened_data()
    ):
        tr, tg, tb, ta = temp
        sr, sg, sb, sa = cold
        if ta == 0 or sa == 0:
            values.append(0)
            continue
        tl = luminance(temp[:3])
        sl = luminance(cold[:3])
        temp_green = tg >= tr + 5 and tg >= tb + 3
        temp_warm = tr >= tg - 2 and tr >= tb + 5 and tl >= 37
        snow_warm = sr >= sg - 5 and sr >= sb + 3
        snow_face = sl < 137 and sr >= sb - 12 and not temp_green
        rock = temp_warm or (snow_face and (snow_warm or tl < 54))
        values.append(255 if rock else 0)

    mask = Image.new("L", temperate.size)
    mask.putdata(values)
    # Close one-pixel holes inside faces without growing the exterior silhouette.
    closed = mask.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.MinFilter(3))
    return closed


def recolor(
    temperate: Image.Image,
    rock_mask: Image.Image,
    palette: list[tuple[int, int, int]],
    clear_frame: bytes,
    normalization: str,
) -> tuple[Image.Image, list[int]]:
    ground_indices = [1, 2, 3, *range(10, 22), 192, 194, 198]
    rock_indices = [3, *range(10, 50), 192, 194, 198]
    rust_indices = [*range(50, 70), *range(100, 130)]
    ground_indices = unique_sorted_by_luma(ground_indices, palette)
    rock_indices = unique_sorted_by_luma(rock_indices, palette)
    rust_indices = unique_sorted_by_luma(rust_indices, palette)

    mask_values = list(rock_mask.get_flattened_data())
    source_pixels = list(temperate.get_flattened_data())
    source_luma = [luminance(pixel[:3]) for pixel in source_pixels]
    source_alpha = [pixel[3] for pixel in source_pixels]
    broad_luma = list(
        temperate.convert("L").filter(ImageFilter.GaussianBlur(radius=0.8)).get_flattened_data()
    )
    rock_values = [
        value
        for value, mask, alpha in zip(source_luma, mask_values, source_alpha)
        if alpha and mask >= 128
    ]
    ground_values = [
        broad
        for broad, mask, alpha in zip(broad_luma, mask_values, source_alpha)
        if alpha and mask < 128
    ]
    if normalization == "family":
        rock_low, rock_high = FAMILY_ROCK_RANGE
    else:
        rock_low, rock_high = percentile(rock_values, 0.05), percentile(rock_values, 0.95)
    ground_shadow_baseline = percentile(ground_values, 0.75)
    edge_distance = distance_from_ground(mask_values, rock_mask.width, rock_mask.height, 3)
    rock_distance = distance_from_rock(
        mask_values, source_alpha, rock_mask.width, rock_mask.height, 8
    )
    out_indices = []
    out_colors = []
    for i, pixel in enumerate(source_pixels):
        x, y = i % temperate.width, i // temperate.width
        if pixel[3] == 0:
            out_indices.append(0)
            out_colors.append((0, 0, 0, 0))
            continue
        raw = source_luma[i]
        detail = raw - broad_luma[i]
        is_rock = mask_values[i] >= 128
        if is_rock:
            form = normalize(raw, rock_low, rock_high)
            target_luma = 24 + form * 70 + clamp(detail * 0.20, -5, 5)
            target_luma = clamp(target_luma, 21, 94)
            candidates = rock_indices
            if edge_distance[i] >= 3 and 34 <= target_luma <= 82 and rust_field(x, y) > 0.925:
                candidates = rust_indices
            index = closest_luma_index(target_luma, candidates, palette)
        else:
            # Discard vegetation/material texture. Start from exact clear1 pixels,
            # then retain only broad donor shadow close to the cliff contact.
            index = clear_frame[(y % TILE) * TILE + (x % TILE)]
            distance = rock_distance[i]
            if distance <= 8:
                shadow = max(0.0, ground_shadow_baseline - broad_luma[i])
                taper = (9 - distance) / 8
                target_luma = luminance(palette[index]) - min(28.0, shadow * 0.72 * taper)
                index = closest_luma_index(target_luma, ground_indices, palette)
        out_indices.append(index)
        out_colors.append((*palette[index], 255))

    result = Image.new("RGBA", temperate.size)
    result.putdata(out_colors)
    return result, out_indices


def unique_sorted_by_luma(
    indices: list[int], palette: list[tuple[int, int, int]]
) -> list[int]:
    return sorted(set(indices), key=lambda index: (luminance(palette[index]), index))


def closest_luma_index(
    target: float,
    candidates: list[int],
    palette: list[tuple[int, int, int]],
) -> int:
    return min(candidates, key=lambda index: abs(luminance(palette[index]) - target))


def distance_from_ground(
    mask: list[int], width: int, height: int, maximum: int
) -> list[int]:
    distances = [maximum + 1 if value >= 128 else 0 for value in mask]
    for distance in range(1, maximum + 1):
        for y in range(height):
            for x in range(width):
                i = y * width + x
                if distances[i] <= distance:
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


def distance_from_rock(
    mask: list[int], alpha: list[int], width: int, height: int, maximum: int
) -> list[int]:
    distances = [
        0 if value >= 128 else maximum + 1
        for value, visible in zip(mask, alpha)
    ]
    for distance in range(1, maximum + 1):
        for y in range(height):
            for x in range(width):
                i = y * width + x
                if not alpha[i] or distances[i] <= distance:
                    continue
                for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                    nx, ny = x + ox, y + oy
                    if 0 <= nx < width and 0 <= ny < height:
                        if distances[ny * width + nx] == distance - 1:
                            distances[i] = distance
                            break
    return distances


def rust_field(x: int, y: int) -> float:
    # Coherent, deterministic patches; no per-pixel pepper noise.
    coarse_x, coarse_y = x // 3, y // 3
    value = ((coarse_x * 73856093) ^ (coarse_y * 19349663) ^ 0x14511) & 0xFFFF
    return value / 65535.0


def luminance(color: tuple[int, int, int]) -> float:
    return 0.299 * color[0] + 0.587 * color[1] + 0.114 * color[2]


def clamp(value: float, low: float, high: float) -> float:
    return max(low, min(high, value))


def normalize(value: float, low: float, high: float) -> float:
    if high <= low:
        return 0.5
    return clamp((value - low) / (high - low), 0.0, 1.0)


def percentile(values: list[float], fraction: float) -> float:
    if not values:
        raise ValueError("cannot calculate percentile of an empty material region")
    ordered = sorted(values)
    return ordered[round((len(ordered) - 1) * fraction)]


def slice_frames(
    indices: list[int], width: int, height: int, occupancy: Image.Image
) -> list[bytes]:
    if width % TILE or height % TILE:
        raise ValueError(f"image dimensions must be tile-aligned, got {width}x{height}")
    frames = []
    alpha = list(occupancy.getchannel("A").get_flattened_data())
    for cell_y in range(height // TILE):
        for cell_x in range(width // TILE):
            occupied = False
            for y in range(TILE):
                start = (cell_y * TILE + y) * width + cell_x * TILE
                if any(alpha[start : start + TILE]):
                    occupied = True
                    break
            if not occupied:
                continue
            frame = bytearray()
            for y in range(TILE):
                start = (cell_y * TILE + y) * width + cell_x * TILE
                frame.extend(indices[start : start + TILE])
            frames.append(bytes(frame))
    return frames


def verify_vol(path: Path, expected_frames: int) -> None:
    width, height, frames = read_shptd(path)
    if (width, height) != (TILE, TILE) or len(frames) != expected_frames:
        raise ValueError(
            f"invalid preview VOL: {width}x{height}, {len(frames)} frames; expected {expected_frames}"
        )


def write_review(
    path: Path,
    temperate: Image.Image,
    mask: Image.Image,
    recolor: Image.Image,
    ai: Image.Image,
    comparison_label: str,
) -> None:
    width, height = temperate.size
    header = 26
    sheet = Image.new("RGB", (width * 2, (height + header) * 2), (73, 86, 99))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    mask_rgb = Image.merge("RGB", (Image.new("L", mask.size), mask, Image.new("L", mask.size)))
    panels = [
        ("Original RA Temperate", temperate),
        ("Automatic rock mask", mask_rgb),
        ("Luminance-preserving recolor", recolor),
        (comparison_label, ai),
    ]
    for i, (label, panel) in enumerate(panels):
        x = (i % 2) * width
        y = (i // 2) * (height + header)
        draw.text((x + 6, y + 7), label, fill="white", font=font)
        sheet.paste(flatten_for_review(panel), (x, y + header))
    sheet.save(path)


def flatten_for_review(image: Image.Image) -> Image.Image:
    if image.mode != "RGBA":
        return image.convert("RGB")
    background = Image.new("RGBA", image.size, (24, 24, 24, 255))
    background.alpha_composite(image)
    return background.convert("RGB")


if __name__ == "__main__":
    raise SystemExit(main())
