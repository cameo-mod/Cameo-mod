#!/usr/bin/env python
"""Review two cliff templates placed at a metadata-derived cell offset."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


TILE = 48
SCALE = 4


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--first", type=Path, required=True)
    parser.add_argument("--second", type=Path, required=True)
    parser.add_argument("--first-label", required=True)
    parser.add_argument("--second-label", required=True)
    parser.add_argument("--temperate-background", type=Path)
    parser.add_argument("--volcanic-background", type=Path)
    parser.add_argument(
        "--second-offset",
        required=True,
        help="Second template origin relative to first, in cells: x,y",
    )
    parser.add_argument("--out-dir", type=Path, required=True)
    args = parser.parse_args()

    offset = tuple(int(value) * TILE for value in args.second_offset.split(","))
    if len(offset) != 2:
        raise ValueError("--second-offset must be x,y")
    first = load_sample(args.first)
    second = load_sample(args.second)
    layout = build_layout(first, second, offset)
    contact = find_contact(layout["first_rect"], layout["second_rect"])

    temperate_background = load_background(args.temperate_background)
    volcanic_background = load_background(args.volcanic_background)
    temperate = compose(
        first["temperate"], second["temperate"], layout, temperate_background
    )
    volcanic = compose(
        first["volcanic"], second["volcanic"], layout, volcanic_background
    )
    source_metrics = seam_metrics(first, second, layout, contact, "temperate")
    volcanic_metrics = seam_metrics(first, second, layout, contact, "volcanic")

    out_dir = args.out_dir.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    temperate.save(out_dir / "layout-temperate.png")
    volcanic.save(out_dir / "layout-volcanic.png")
    write_review(
        out_dir / "connectivity-review.png",
        args.first_label,
        args.second_label,
        temperate,
        volcanic,
    )
    write_seam_review(out_dir / "seam-review.png", temperate, volcanic, contact)
    record = {
        "first": args.first_label,
        "second": args.second_label,
        "second_offset_cells": [offset[0] // TILE, offset[1] // TILE],
        "contact": contact,
        "temperate": source_metrics,
        "volcanic": volcanic_metrics,
    }
    (out_dir / "metrics.json").write_text(json.dumps(record, indent=2), encoding="utf-8")
    print(
        f"{args.first_label} -> {args.second_label}: "
        f"geometry mismatch={source_metrics['rock_geometry_mismatch_pixels']}; "
        f"temperate rock seam={source_metrics['rock_luminance_delta']:.2f}; "
        f"volcanic rock seam={volcanic_metrics['rock_luminance_delta']:.2f}"
    )
    print((out_dir / "connectivity-review.png").resolve())
    print((out_dir / "seam-review.png").resolve())
    return 0


def load_sample(path: Path) -> dict[str, Image.Image]:
    root = path.resolve()
    return {
        "temperate": Image.open(root / "temperate-native.png").convert("RGBA"),
        "volcanic": Image.open(root / "luminance-recolor-native.png").convert("RGBA"),
        "mask": Image.open(root / "rock-mask-native.png").convert("L"),
    }


def load_background(path: Path | None) -> Image.Image | None:
    return Image.open(path.resolve()).convert("RGBA") if path else None


def build_layout(
    first: dict[str, Image.Image],
    second: dict[str, Image.Image],
    offset: tuple[int, int],
) -> dict[str, tuple[int, int] | tuple[int, int, int, int]]:
    fw, fh = first["temperate"].size
    sw, sh = second["temperate"].size
    min_x, min_y = min(0, offset[0]), min(0, offset[1])
    max_x, max_y = max(fw, offset[0] + sw), max(fh, offset[1] + sh)
    first_pos = (-min_x, -min_y)
    second_pos = (offset[0] - min_x, offset[1] - min_y)
    return {
        "size": (max_x - min_x, max_y - min_y),
        "first_pos": first_pos,
        "second_pos": second_pos,
        "first_rect": (*first_pos, first_pos[0] + fw, first_pos[1] + fh),
        "second_rect": (*second_pos, second_pos[0] + sw, second_pos[1] + sh),
    }


def compose(
    first: Image.Image,
    second: Image.Image,
    layout: dict[str, tuple[int, int] | tuple[int, int, int, int]],
    background: Image.Image | None,
) -> Image.Image:
    out = tiled_background(layout["size"], background)
    out.alpha_composite(first, layout["first_pos"])
    out.alpha_composite(second, layout["second_pos"])
    return out


def tiled_background(
    size: tuple[int, int], background: Image.Image | None
) -> Image.Image:
    if background is None:
        return Image.new("RGBA", size, (0, 0, 0, 0))
    out = Image.new("RGBA", size)
    for y in range(0, size[1], background.height):
        for x in range(0, size[0], background.width):
            out.alpha_composite(background, (x, y))
    return out


def find_contact(
    first: tuple[int, int, int, int], second: tuple[int, int, int, int]
) -> dict[str, int | str]:
    fx0, fy0, fx1, fy1 = first
    sx0, sy0, sx1, sy1 = second
    if fx1 == sx0:
        return vertical_contact("first-left", fx1, max(fy0, sy0), min(fy1, sy1))
    if sx1 == fx0:
        return vertical_contact("second-left", sx1, max(fy0, sy0), min(fy1, sy1))
    if fy1 == sy0:
        return horizontal_contact("first-top", fy1, max(fx0, sx0), min(fx1, sx1))
    if sy1 == fy0:
        return horizontal_contact("second-top", sy1, max(fx0, sx0), min(fx1, sx1))
    raise ValueError(f"template rectangles do not share one edge: {first}, {second}")


def vertical_contact(order: str, x: int, start: int, end: int) -> dict[str, int | str]:
    if end <= start:
        raise ValueError("vertical contact has no shared span")
    return {"orientation": "vertical", "order": order, "line": x, "start": start, "end": end}


def horizontal_contact(order: str, y: int, start: int, end: int) -> dict[str, int | str]:
    if end <= start:
        raise ValueError("horizontal contact has no shared span")
    return {"orientation": "horizontal", "order": order, "line": y, "start": start, "end": end}


def seam_metrics(
    first: dict[str, Image.Image],
    second: dict[str, Image.Image],
    layout: dict[str, tuple[int, int] | tuple[int, int, int, int]],
    contact: dict[str, int | str],
    material: str,
) -> dict[str, float | int]:
    image_a, image_b = first[material], second[material]
    mask_a, mask_b = first["mask"], second["mask"]
    pos_a, pos_b = layout["first_pos"], layout["second_pos"]
    all_delta, rock_delta, ground_delta = [], [], []
    occupancy_mismatch = geometry_mismatch = 0
    for world_a, world_b in seam_coordinates(contact):
        a = sample(image_a, world_a, pos_a)
        b = sample(image_b, world_b, pos_b)
        a_visible, b_visible = a[3] > 0, b[3] > 0
        if a_visible != b_visible:
            occupancy_mismatch += 1
        if not a_visible or not b_visible:
            continue
        delta = abs(luminance(a[:3]) - luminance(b[:3]))
        all_delta.append(delta)
        a_rock = sample(mask_a, world_a, pos_a) >= 128
        b_rock = sample(mask_b, world_b, pos_b) >= 128
        if a_rock != b_rock:
            geometry_mismatch += 1
        elif a_rock:
            rock_delta.append(delta)
        else:
            ground_delta.append(delta)
    return {
        "occupancy_mismatch_pixels": occupancy_mismatch,
        "rock_geometry_mismatch_pixels": geometry_mismatch,
        "all_luminance_delta": mean(all_delta),
        "rock_luminance_delta": mean(rock_delta),
        "ground_luminance_delta": mean(ground_delta),
        "rock_samples": len(rock_delta),
        "ground_samples": len(ground_delta),
    }


def seam_coordinates(
    contact: dict[str, int | str]
) -> list[tuple[tuple[int, int], tuple[int, int]]]:
    line, start, end = int(contact["line"]), int(contact["start"]), int(contact["end"])
    order = str(contact["order"])
    if contact["orientation"] == "vertical":
        pairs = [((line - 1, y), (line, y)) for y in range(start, end)]
    else:
        pairs = [((x, line - 1), (x, line)) for x in range(start, end)]
    if order in {"second-left", "second-top"}:
        return [(b, a) for a, b in pairs]
    return pairs


def sample(image: Image.Image, world: tuple[int, int], origin: tuple[int, int]):
    return image.getpixel((world[0] - origin[0], world[1] - origin[1]))


def luminance(color: tuple[int, int, int]) -> float:
    return 0.299 * color[0] + 0.587 * color[1] + 0.114 * color[2]


def mean(values: list[float]) -> float:
    return sum(values) / len(values) if values else 0.0


def write_review(
    path: Path,
    first_label: str,
    second_label: str,
    temperate: Image.Image,
    volcanic: Image.Image,
) -> None:
    width, height = temperate.width * SCALE, temperate.height * SCALE
    header = 28
    sheet = Image.new("RGB", (width * 2, height + header), (73, 86, 99))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    title = f"{first_label} + {second_label}"
    for column, (label, image) in enumerate((("Temperate", temperate), ("Volcanic", volcanic))):
        x = column * width
        draw.text((x + 6, 7), f"{title} - {label}", fill="white", font=font)
        panel = flatten(image).resize((width, height), Image.Resampling.NEAREST)
        sheet.paste(panel, (x, header))
    sheet.save(path)


def write_seam_review(
    path: Path,
    temperate: Image.Image,
    volcanic: Image.Image,
    contact: dict[str, int | str],
) -> None:
    half = 12
    line, start, end = int(contact["line"]), int(contact["start"]), int(contact["end"])
    if contact["orientation"] == "vertical":
        box = (line - half, start, line + half, end)
    else:
        box = (start, line - half, end, line + half)
    strips = [flatten(image.crop(box)) for image in (temperate, volcanic)]
    width, height = strips[0].width * SCALE, strips[0].height * SCALE
    header = 24
    sheet = Image.new("RGB", (width * 2, height + header), (73, 86, 99))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for column, (label, strip) in enumerate(zip(("Temperate seam", "Volcanic seam"), strips)):
        x = column * width
        draw.text((x + 5, 6), label, fill="white", font=font)
        sheet.paste(strip.resize((width, height), Image.Resampling.NEAREST), (x, header))
    sheet.save(path)


def flatten(image: Image.Image) -> Image.Image:
    background = Image.new("RGBA", image.size, (24, 24, 24, 255))
    background.alpha_composite(image.convert("RGBA"))
    return background.convert("RGB")


if __name__ == "__main__":
    raise SystemExit(main())
