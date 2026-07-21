#!/usr/bin/env python
"""Detect isolated dark cliff components without touching real crevices."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
CYAN = (0, 255, 255, 255)
BLACK = (0, 0, 0, 255)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", type=Path, required=True)
    parser.add_argument("--semantic-mask", type=Path, required=True)
    parser.add_argument("--donor", type=Path, required=True)
    parser.add_argument("--out-dir", type=Path, required=True)
    parser.add_argument("--max-component", type=int, default=8)
    args = parser.parse_args()

    base = load_native(args.base)
    semantic = load_native(args.semantic_mask)
    donor = load_native(args.donor)
    cliff = [is_cliff(pixel) for pixel in semantic.get_flattened_data()]
    dark = {
        (x, y)
        for y in range(base.height)
        for x in range(base.width)
        if cliff[y * base.width + x] and luminance(base.getpixel((x, y))) <= 24
    }
    components = connected_components(dark)
    selected: list[set[tuple[int, int]]] = []
    for component in components:
        if len(component) > args.max_component or touches_non_cliff(component, cliff, base.width, base.height):
            continue
        donor_mean = sum(luminance(donor.getpixel(point)) for point in component) / len(component)
        if donor_mean >= 36:
            selected.append(component)

    selected_points = set().union(*selected) if selected else set()
    mask = Image.new("RGBA", base.size, BLACK)
    mask_pixels = list(mask.get_flattened_data())
    overlay = base.copy()
    overlay_pixels = list(base.get_flattened_data())
    for x, y in selected_points:
        i = y * base.width + x
        mask_pixels[i] = CYAN
        overlay_pixels[i] = CYAN
    mask.putdata(mask_pixels)
    overlay.putdata(overlay_pixels)

    out_dir = resolve(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    mask.save(out_dir / "dark-noise-mask-native.png")
    overlay.save(out_dir / "dark-noise-overlay-native.png")
    mask.resize((576, 384), Image.Resampling.NEAREST).save(out_dir / "dark-noise-mask-x4.png")
    overlay.resize((576, 384), Image.Resampling.NEAREST).save(out_dir / "dark-noise-overlay-x4.png")
    write_review(out_dir / "dark-noise-review.png", base, donor, mask, overlay)
    print((out_dir / "dark-noise-review.png").resolve())
    print(f"components={len(selected)}; pixels={len(selected_points)}; sizes={sorted(map(len, selected))}")
    return 0


def resolve(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def load_native(path: Path) -> Image.Image:
    image = Image.open(resolve(path)).convert("RGBA")
    return image if image.size == (144, 96) else image.resize((144, 96), Image.Resampling.NEAREST)


def is_cliff(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, _ = pixel
    return g > 220 and r < 40 and b < 40


def luminance(pixel: tuple[int, ...]) -> float:
    return 0.299 * pixel[0] + 0.587 * pixel[1] + 0.114 * pixel[2]


def connected_components(points: set[tuple[int, int]]) -> list[set[tuple[int, int]]]:
    remaining = set(points)
    components = []
    while remaining:
        seed = remaining.pop()
        component = {seed}
        stack = [seed]
        while stack:
            x, y = stack.pop()
            for oy in (-1, 0, 1):
                for ox in (-1, 0, 1):
                    neighbor = (x + ox, y + oy)
                    if neighbor in remaining:
                        remaining.remove(neighbor)
                        component.add(neighbor)
                        stack.append(neighbor)
        components.append(component)
    return components


def touches_non_cliff(
    component: set[tuple[int, int]],
    cliff: list[bool],
    width: int,
    height: int,
) -> bool:
    for x, y in component:
        for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            nx, ny = x + ox, y + oy
            if nx < 0 or ny < 0 or nx >= width or ny >= height:
                return True
            if not cliff[ny * width + nx]:
                return True
    return False


def write_review(path: Path, base: Image.Image, donor: Image.Image, mask: Image.Image, overlay: Image.Image) -> None:
    panels = [("current preview", base), ("Temperate donor", donor), ("cyan=noise candidate", mask), ("candidate overlay", overlay)]
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
