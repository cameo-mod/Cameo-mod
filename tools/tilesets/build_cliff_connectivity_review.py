#!/usr/bin/env python
"""Assemble vertically adjacent cliff templates and compare their shared seam."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


SCALE = 4


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--first", type=Path, required=True)
    parser.add_argument("--second", type=Path, required=True)
    parser.add_argument("--first-label")
    parser.add_argument("--second-label")
    parser.add_argument(
        "--orders",
        choices=("first-second", "both"),
        default="both",
        help="Render only first-above-second, or both vertical orders.",
    )
    parser.add_argument("--out-dir", type=Path, required=True)
    args = parser.parse_args()

    first = load_sample(args.first)
    second = load_sample(args.second)
    if first["temperate"].size != second["temperate"].size:
        raise ValueError("pair templates must use matching canvas dimensions")

    out_dir = args.out_dir.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    first_label = args.first_label or args.first.name
    second_label = args.second_label or args.second.name
    orders = [(f"{first_label} above {second_label}", first, second)]
    if args.orders == "both":
        orders.append((f"{second_label} above {first_label}", second, first))
    records = []
    rendered = []
    for label, top, bottom in orders:
        slug = label.lower().replace(" above ", "-").replace(" ", "-")
        temperate = stack(top["temperate"], bottom["temperate"])
        volcanic = stack(top["volcanic"], bottom["volcanic"])
        temperate.save(out_dir / f"{slug}-temperate.png")
        volcanic.save(out_dir / f"{slug}-volcanic.png")
        records.append(
            {
                "order": label,
                "temperate": seam_metrics(
                    top["temperate"], bottom["temperate"], top["mask"], bottom["mask"]
                ),
                "volcanic": seam_metrics(
                    top["volcanic"], bottom["volcanic"], top["mask"], bottom["mask"]
                ),
            }
        )
        rendered.append((label, temperate, volcanic))

    write_review(out_dir / "connectivity-review.png", rendered)
    write_seam_review(out_dir / "seam-review.png", rendered)
    (out_dir / "metrics.json").write_text(json.dumps(records, indent=2), encoding="utf-8")

    for record in records:
        source = record["temperate"]
        volcanic = record["volcanic"]
        print(
            f"{record['order']}: rock geometry mismatch={source['rock_geometry_mismatch_pixels']}; "
            f"temperate rock seam={source['rock_luminance_delta']:.2f}; "
            f"volcanic rock seam={volcanic['rock_luminance_delta']:.2f}"
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


def stack(top: Image.Image, bottom: Image.Image) -> Image.Image:
    out = Image.new("RGBA", (top.width, top.height + bottom.height), (0, 0, 0, 0))
    out.paste(top, (0, 0))
    out.paste(bottom, (0, top.height))
    return out


def seam_metrics(
    top: Image.Image,
    bottom: Image.Image,
    top_mask: Image.Image,
    bottom_mask: Image.Image,
) -> dict[str, float | int]:
    all_delta = []
    rock_delta = []
    ground_delta = []
    geometry_mismatch = 0
    occupancy_mismatch = 0
    for x in range(top.width):
        a = top.getpixel((x, top.height - 1))
        b = bottom.getpixel((x, 0))
        a_visible = a[3] > 0
        b_visible = b[3] > 0
        if a_visible != b_visible:
            occupancy_mismatch += 1
        if not a_visible or not b_visible:
            continue
        delta = abs(luminance(a[:3]) - luminance(b[:3]))
        all_delta.append(delta)
        a_rock = top_mask.getpixel((x, top.height - 1)) >= 128
        b_rock = bottom_mask.getpixel((x, 0)) >= 128
        if a_rock != b_rock:
            geometry_mismatch += 1
        elif a_rock:
            rock_delta.append(delta)
        else:
            ground_delta.append(delta)
    return {
        "rock_geometry_mismatch_pixels": geometry_mismatch,
        "occupancy_mismatch_pixels": occupancy_mismatch,
        "all_luminance_delta": mean(all_delta),
        "rock_luminance_delta": mean(rock_delta),
        "ground_luminance_delta": mean(ground_delta),
        "rock_samples": len(rock_delta),
        "ground_samples": len(ground_delta),
    }


def luminance(color: tuple[int, int, int]) -> float:
    return 0.299 * color[0] + 0.587 * color[1] + 0.114 * color[2]


def mean(values: list[float]) -> float:
    return sum(values) / len(values) if values else 0.0


def write_review(
    path: Path,
    rendered: list[tuple[str, Image.Image, Image.Image]],
) -> None:
    native_width, native_height = rendered[0][1].size
    width, height = native_width * SCALE, native_height * SCALE
    header = 28
    sheet = Image.new("RGB", (width * 2, (height + header) * len(rendered)), (73, 86, 99))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for row, (label, temperate, volcanic) in enumerate(rendered):
        y = row * (height + header)
        panels = ((f"{label} - Temperate", temperate), (f"{label} - Volcanic", volcanic))
        for column, (panel_label, image) in enumerate(panels):
            x = column * width
            draw.text((x + 6, y + 7), panel_label, fill="white", font=font)
            panel = flatten_for_review(image).resize((width, height), Image.Resampling.NEAREST)
            sheet.paste(panel, (x, y + header))
    sheet.save(path)


def write_seam_review(
    path: Path,
    rendered: list[tuple[str, Image.Image, Image.Image]],
) -> None:
    strip_half_height = 12
    native_width = rendered[0][1].width
    strip_width = native_width * SCALE
    strip_height = strip_half_height * 2 * SCALE
    label_width = 110
    header = 24
    sheet = Image.new(
        "RGB",
        (label_width + strip_width * 2, (strip_height + header) * len(rendered)),
        (73, 86, 99),
    )
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for row, (label, temperate, volcanic) in enumerate(rendered):
        y = row * (strip_height + header)
        draw.text((5, y + 7), label, fill="white", font=font)
        for column, (panel_label, image) in enumerate(
            (("Temperate seam", temperate), ("Volcanic seam", volcanic))
        ):
            x = label_width + column * strip_width
            seam = image.height // 2
            strip = image.crop((0, seam - strip_half_height, image.width, seam + strip_half_height))
            draw.text((x + 5, y + 7), panel_label, fill="white", font=font)
            sheet.paste(
                flatten_for_review(strip).resize((strip_width, strip_height), Image.Resampling.NEAREST),
                (x, y + header),
            )
    sheet.save(path)


def flatten_for_review(image: Image.Image) -> Image.Image:
    if image.mode != "RGBA":
        return image.convert("RGB")
    background = Image.new("RGBA", image.size, (24, 24, 24, 255))
    background.alpha_composite(image)
    return background.convert("RGB")


if __name__ == "__main__":
    raise SystemExit(main())
