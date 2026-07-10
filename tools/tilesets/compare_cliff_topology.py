#!/usr/bin/env python
"""Compare an AI-rendered cliff silhouette with an approved semantic mask."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
NATIVE_SIZE = (144, 96)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--candidate", type=Path, required=True)
    parser.add_argument("--donor", type=Path, required=True)
    parser.add_argument("--semantic-mask", type=Path, required=True)
    parser.add_argument("--out-dir", type=Path, required=True)
    args = parser.parse_args()

    candidate = load_native(args.candidate)
    donor = load_native(args.donor)
    semantic = load_native(args.semantic_mask)
    original = [is_cliff(pixel) for pixel in semantic.get_flattened_data()]
    tight, inferred = infer_rock(candidate)

    original_mask = binary_image(original, candidate.size, (0, 255, 0, 255))
    tight_mask = binary_image(tight, candidate.size, (0, 180, 255, 255))
    inferred_mask = binary_image(inferred, candidate.size, (0, 220, 255, 255))
    diff = difference_image(original, inferred, candidate.size)
    overlay = difference_overlay(candidate, original, inferred)
    boundary_mask, boundary_overlay, boundary_missing, boundary_added = boundary_correction_images(
        candidate, original, inferred
    )

    out_dir = resolve(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    original_mask.save(out_dir / "original-semantic-mask-native.png")
    tight_mask.save(out_dir / "ai-tight-mask-native.png")
    inferred_mask.save(out_dir / "ai-inferred-mask-native.png")
    diff.save(out_dir / "topology-difference-native.png")
    overlay.save(out_dir / "topology-difference-overlay-native.png")
    boundary_mask.save(out_dir / "boundary-correction-mask-native.png")
    boundary_overlay.save(out_dir / "boundary-correction-overlay-native.png")
    for name, image in (
        ("original-semantic-mask-x4.png", original_mask),
        ("ai-inferred-mask-x4.png", inferred_mask),
        ("topology-difference-x4.png", diff),
        ("topology-difference-overlay-x4.png", overlay),
        ("boundary-correction-mask-x4.png", boundary_mask),
        ("boundary-correction-overlay-x4.png", boundary_overlay),
    ):
        image.resize((576, 384), Image.Resampling.NEAREST).save(out_dir / name)
    write_review(out_dir / "topology-audit-review.png", donor, original_mask, candidate, inferred_mask, diff, overlay)
    write_boundary_review(
        out_dir / "boundary-correction-review.png", candidate, diff, boundary_mask, boundary_overlay
    )
    summary = build_summary(original, inferred, candidate.width, candidate.height)
    summary += f"\nboundary-correction-missing={boundary_missing}\nboundary-correction-added={boundary_added}"
    (out_dir / "topology-summary.txt").write_text(summary, encoding="ascii")
    print((out_dir / "topology-audit-review.png").resolve())
    print(summary)
    return 0


def resolve(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def load_native(path: Path) -> Image.Image:
    image = Image.open(resolve(path)).convert("RGBA")
    return image if image.size == NATIVE_SIZE else image.resize(NATIVE_SIZE, Image.Resampling.NEAREST)


def luminance(pixel: tuple[int, ...]) -> float:
    return 0.299 * pixel[0] + 0.587 * pixel[1] + 0.114 * pixel[2]


def is_cliff(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, _ = pixel
    return g > 220 and r < 40 and b < 40


def infer_rock(image: Image.Image) -> tuple[list[bool], list[bool]]:
    pixels = list(image.get_flattened_data())
    width, height = image.size
    core = []
    for r, g, b, _ in pixels:
        light = luminance((r, g, b))
        core.append(light >= 48 and (r >= g + 4 or light >= 54))

    rock = list(core)
    # Recover dark contour pixels only where they are strongly supported by
    # neighboring rock. This avoids turning cast shadows into geometry.
    for required_neighbors in (3, 4):
        grown = list(rock)
        for y in range(height):
            for x in range(width):
                i = y * width + x
                if rock[i]:
                    continue
                count = sum(
                    1
                    for oy in (-1, 0, 1)
                    for ox in (-1, 0, 1)
                    if (ox or oy)
                    and 0 <= x + ox < width
                    and 0 <= y + oy < height
                    and rock[(y + oy) * width + x + ox]
                )
                if count >= required_neighbors:
                    grown[i] = True
        rock = grown

    # Fill small enclosed holes, retaining large/open ground gaps and clefts.
    background = {(x, y) for y in range(height) for x in range(width) if not rock[y * width + x]}
    for component in connected_components(background):
        touches_edge = any(x in (0, width - 1) or y in (0, height - 1) for x, y in component)
        if not touches_edge and len(component) <= 18:
            for x, y in component:
                rock[y * width + x] = True
    tight = list(rock)
    inclusive = list(tight)
    for y in range(height):
        for x in range(width):
            i = y * width + x
            if tight[i]:
                continue
            if any(
                0 <= x + ox < width
                and 0 <= y + oy < height
                and tight[(y + oy) * width + x + ox]
                for oy in (-1, 0, 1)
                for ox in (-1, 0, 1)
                if ox or oy
            ):
                inclusive[i] = True
    return tight, inclusive


def connected_components(points: set[tuple[int, int]]) -> list[set[tuple[int, int]]]:
    remaining = set(points)
    components = []
    while remaining:
        seed = remaining.pop()
        component = {seed}
        stack = [seed]
        while stack:
            x, y = stack.pop()
            for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                neighbor = (x + ox, y + oy)
                if neighbor in remaining:
                    remaining.remove(neighbor)
                    component.add(neighbor)
                    stack.append(neighbor)
        components.append(component)
    return components


def binary_image(values: list[bool], size: tuple[int, int], color: tuple[int, int, int, int]) -> Image.Image:
    image = Image.new("RGBA", size, (0, 0, 0, 255))
    image.putdata([color if value else (0, 0, 0, 255) for value in values])
    return image


def difference_image(original: list[bool], inferred: list[bool], size: tuple[int, int]) -> Image.Image:
    pixels = []
    for expected, actual in zip(original, inferred):
        if expected and actual:
            pixels.append((92, 92, 92, 255))
        elif actual:
            pixels.append((255, 48, 32, 255))
        elif expected:
            pixels.append((32, 96, 255, 255))
        else:
            pixels.append((0, 0, 0, 255))
    image = Image.new("RGBA", size)
    image.putdata(pixels)
    return image


def difference_overlay(image: Image.Image, original: list[bool], inferred: list[bool]) -> Image.Image:
    pixels = list(image.get_flattened_data())
    for i, (expected, actual) in enumerate(zip(original, inferred)):
        if actual and not expected:
            pixels[i] = (255, 48, 32, 255)
        elif expected and not actual:
            pixels[i] = (32, 96, 255, 255)
    overlay = Image.new("RGBA", image.size)
    overlay.putdata(pixels)
    return overlay


def boundary_correction_images(
    image: Image.Image,
    original: list[bool],
    inferred: list[bool],
) -> tuple[Image.Image, Image.Image, int, int]:
    mask_pixels = [(0, 0, 0, 255)] * (image.width * image.height)
    overlay_pixels = list(image.get_flattened_data())
    missing = 0
    added = 0
    vertical = (0, 47, 48, 95, 96, image.width - 1)
    horizontal = (0, 47, 48, image.height - 1)
    for y in range(image.height):
        for x in range(image.width):
            i = y * image.width + x
            near_boundary = min(abs(x - value) for value in vertical) <= 1 or min(abs(y - value) for value in horizontal) <= 1
            if not near_boundary or original[i] == inferred[i]:
                continue
            if original[i]:
                mask_pixels[i] = (32, 96, 255, 255)
                overlay_pixels[i] = (32, 96, 255, 255)
                missing += 1
            else:
                mask_pixels[i] = (255, 48, 32, 255)
                overlay_pixels[i] = (255, 48, 32, 255)
                added += 1
    mask = Image.new("RGBA", image.size)
    mask.putdata(mask_pixels)
    overlay = Image.new("RGBA", image.size)
    overlay.putdata(overlay_pixels)
    return mask, overlay, missing, added


def build_summary(original: list[bool], inferred: list[bool], width: int, height: int) -> str:
    common = sum(a and b for a, b in zip(original, inferred))
    missing = sum(a and not b for a, b in zip(original, inferred))
    added = sum(b and not a for a, b in zip(original, inferred))
    lines = [
        f"original-rock={sum(original)}",
        f"ai-inferred-rock={sum(inferred)}",
        f"common={common}",
        f"missing-original-rock={missing}",
        f"added-ai-rock={added}",
        f"intersection-over-union={common / max(1, common + missing + added):.4f}",
        "frames:",
    ]
    for cell_y in range(height // TILE):
        for cell_x in range(width // TILE):
            indices = [
                y * width + x
                for y in range(cell_y * TILE, (cell_y + 1) * TILE)
                for x in range(cell_x * TILE, (cell_x + 1) * TILE)
            ]
            frame_missing = sum(original[i] and not inferred[i] for i in indices)
            frame_added = sum(inferred[i] and not original[i] for i in indices)
            lines.append(f"  ({cell_x},{cell_y}) missing={frame_missing} added={frame_added}")
    lines.append("frame-boundary-differences:")
    for label, indices in boundary_indices(width, height).items():
        differences = sum(original[i] != inferred[i] for i in indices)
        lines.append(f"  {label}={differences}")
    return "\n".join(lines)


TILE = 48


def boundary_indices(width: int, height: int) -> dict[str, list[int]]:
    result = {
        "outer-left": [y * width for y in range(height)],
        "outer-right": [y * width + width - 1 for y in range(height)],
        "outer-top": list(range(width)),
        "outer-bottom": [(height - 1) * width + x for x in range(width)],
    }
    for x in (47, 48, 95, 96):
        result[f"vertical-x{x}"] = [y * width + x for y in range(height)]
    for y in (47, 48):
        result[f"horizontal-y{y}"] = [y * width + x for x in range(width)]
    return result


def write_review(
    path: Path,
    donor: Image.Image,
    original_mask: Image.Image,
    candidate: Image.Image,
    inferred_mask: Image.Image,
    diff: Image.Image,
    overlay: Image.Image,
) -> None:
    panels = [
        ("Temperate donor", donor),
        ("approved geometry", original_mask),
        ("selected AI-nearest", candidate),
        ("AI inclusive geometry", inferred_mask),
        ("gray=common red=added blue=missing", diff),
        ("difference overlay", overlay),
    ]
    scale, header = 4, 24
    width, height = donor.width * scale, donor.height * scale
    sheet = Image.new("RGBA", (width * 2, (height + header) * 3), (73, 86, 99, 255))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for i, (label, image) in enumerate(panels):
        x, y = (i % 2) * width, (i // 2) * (height + header)
        draw.text((x + 5, y + 5), label, fill="white", font=font)
        sheet.alpha_composite(image.resize((width, height), Image.Resampling.NEAREST), (x, y + header))
    sheet.save(path)


def write_boundary_review(
    path: Path,
    candidate: Image.Image,
    full_difference: Image.Image,
    boundary_mask: Image.Image,
    boundary_overlay: Image.Image,
) -> None:
    panels = [
        ("selected AI-nearest", candidate),
        ("all topology differences", full_difference),
        ("boundary correction mask", boundary_mask),
        ("boundary-only overlay", boundary_overlay),
    ]
    scale, header = 4, 24
    width, height = candidate.width * scale, candidate.height * scale
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
