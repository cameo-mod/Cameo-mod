#!/usr/bin/env python
"""Build preview-only 24px-authored candidates from the approved clear ground."""

from __future__ import annotations

import argparse
import json
import sys
from collections import Counter
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
AUTHOR_TILE = 24
OUTPUT_TILE = 48
FRAME_COUNT = 16


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--source",
        type=Path,
        default=ROOT / "mods/cameo/bits/volcanic/clear1.vol",
    )
    parser.add_argument(
        "--palette",
        type=Path,
        default=ROOT / "mods/cameo/bits/volcanic/volcanic.pal",
    )
    parser.add_argument("--out-dir", type=Path, required=True)
    args = parser.parse_args()

    source = resolve(args.source)
    palette_path = resolve(args.palette)
    out_dir = resolve(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    width, height, frames = read_shptd(source)
    if (width, height, len(frames)) != (OUTPUT_TILE, OUTPUT_TILE, FRAME_COUNT):
        raise ValueError(
            f"{source}: expected 48x48/{FRAME_COUNT}, got {width}x{height}/{len(frames)}"
        )
    if len(set(frames)) != 1:
        raise ValueError(f"{source}: expected all clear1 frames to be identical")

    palette = np.asarray(read_palette(palette_path), dtype=np.uint8)
    current_indices = np.frombuffer(frames[0], dtype=np.uint8).reshape(
        OUTPUT_TILE, OUTPUT_TILE
    )
    current_rgb = palette[current_indices]
    allowed_indices = np.asarray(sorted(set(frames[0])), dtype=np.uint8)

    average_author = average_2x2(current_rgb, palette, allowed_indices)
    dominant_author = dominant_2x2(current_indices, current_rgb, palette)
    variants = {
        "average": average_author,
        "dominant": dominant_author,
    }

    panels: list[tuple[str, Image.Image]] = [
        ("CURRENT: direct 48px, 6x6 repeat", repeat_image(current_indices, palette, 6)),
    ]
    audit: dict[str, object] = {
        "source": str(source),
        "author_tile_size": AUTHOR_TILE,
        "output_tile_size": OUTPUT_TILE,
        "upscale": "strict nearest-neighbor 2x",
        "source_unique_frames": len(set(frames)),
        "source_palette_indices": allowed_indices.tolist(),
        "variants": {},
    }

    for name, author_indices in variants.items():
        output_indices = np.repeat(np.repeat(author_indices, 2, axis=0), 2, axis=1)
        output_frame = bytes(output_indices.reshape(-1))
        vol_path = out_dir / f"clear1-24px-{name}-preview.vol"
        write_shptd(vol_path, OUTPUT_TILE, OUTPUT_TILE, [output_frame] * FRAME_COUNT)
        verify_roundtrip(vol_path, output_frame)

        author_image = indices_image(author_indices, palette)
        output_image = indices_image(output_indices, palette)
        author_image.save(out_dir / f"clear1-author-24px-{name}.png")
        output_image.save(out_dir / f"clear1-output-48px-{name}.png")
        panels.append(
            (f"{name.upper()}: 24px -> strict 2x, 6x6 repeat", repeat_image(output_indices, palette, 6))
        )
        audit["variants"][name] = {
            "vol": str(vol_path),
            "strict_2x": strict_2x(output_indices),
            "palette_indices": sorted(int(value) for value in np.unique(output_indices)),
            "mean_rgb_error_from_current": round(
                float(np.abs(output_image_array(output_indices, palette) - current_rgb.astype(np.int16)).mean()),
                4,
            ),
            "horizontal_repeat_seam_delta": round(edge_delta(output_indices, palette, "horizontal"), 4),
            "vertical_repeat_seam_delta": round(edge_delta(output_indices, palette, "vertical"), 4),
        }

    audit["current"] = {
        "horizontal_repeat_seam_delta": round(edge_delta(current_indices, palette, "horizontal"), 4),
        "vertical_repeat_seam_delta": round(edge_delta(current_indices, palette, "vertical"), 4),
    }
    review_path = out_dir / "clear_ground_24px_candidate_review.png"
    write_review(review_path, panels)
    audit_path = out_dir / "clear_ground_24px_candidate_audit.json"
    audit_path.write_text(json.dumps(audit, indent=2), encoding="utf-8")
    print(review_path.resolve())
    print(audit_path.resolve())
    return 0


def resolve(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def read_palette(path: Path) -> list[tuple[int, int, int]]:
    data = path.read_bytes()
    if len(data) != 768:
        raise ValueError(f"{path}: expected a 768-byte C&C palette")
    return [
        tuple(data[offset + channel] * 4 for channel in range(3))
        for offset in range(0, len(data), 3)
    ]


def average_2x2(
    rgb: np.ndarray,
    palette: np.ndarray,
    allowed_indices: np.ndarray,
) -> np.ndarray:
    author_rgb = rgb.reshape(AUTHOR_TILE, 2, AUTHOR_TILE, 2, 3).mean(axis=(1, 3))
    allowed_rgb = palette[allowed_indices].astype(np.float64)
    delta = author_rgb[:, :, None, :] - allowed_rgb[None, None, :, :]
    distance = (
        2.0 * delta[:, :, :, 0] ** 2
        + 4.0 * delta[:, :, :, 1] ** 2
        + delta[:, :, :, 2] ** 2
    )
    return allowed_indices[np.argmin(distance, axis=2)].astype(np.uint8)


def dominant_2x2(
    indices: np.ndarray,
    rgb: np.ndarray,
    palette: np.ndarray,
) -> np.ndarray:
    result = np.empty((AUTHOR_TILE, AUTHOR_TILE), dtype=np.uint8)
    for y in range(AUTHOR_TILE):
        for x in range(AUTHOR_TILE):
            block = indices[y * 2 : y * 2 + 2, x * 2 : x * 2 + 2].reshape(-1)
            counts = Counter(int(value) for value in block)
            maximum = max(counts.values())
            candidates = [index for index, count in counts.items() if count == maximum]
            if len(candidates) == 1:
                result[y, x] = candidates[0]
                continue
            average = rgb[y * 2 : y * 2 + 2, x * 2 : x * 2 + 2].mean(axis=(0, 1))
            candidate_rgb = palette[candidates].astype(np.float64)
            delta = candidate_rgb - average[None, :]
            distance = 2.0 * delta[:, 0] ** 2 + 4.0 * delta[:, 1] ** 2 + delta[:, 2] ** 2
            result[y, x] = candidates[int(np.argmin(distance))]
    return result


def indices_image(indices: np.ndarray, palette: np.ndarray) -> Image.Image:
    return Image.fromarray(palette[indices], mode="RGB")


def output_image_array(indices: np.ndarray, palette: np.ndarray) -> np.ndarray:
    return palette[indices].astype(np.int16)


def repeat_image(indices: np.ndarray, palette: np.ndarray, count: int) -> Image.Image:
    repeated = np.tile(indices, (count, count))
    return indices_image(repeated, palette)


def strict_2x(indices: np.ndarray) -> bool:
    return bool(
        np.array_equal(indices[0::2, 0::2], indices[1::2, 0::2])
        and np.array_equal(indices[0::2, 0::2], indices[0::2, 1::2])
        and np.array_equal(indices[0::2, 0::2], indices[1::2, 1::2])
    )


def edge_delta(indices: np.ndarray, palette: np.ndarray, axis: str) -> float:
    rgb = palette[indices].astype(np.int16)
    if axis == "horizontal":
        first, second = rgb[:, -1], rgb[:, 0]
    elif axis == "vertical":
        first, second = rgb[-1, :], rgb[0, :]
    else:
        raise ValueError(axis)
    return float(np.abs(first - second).mean())


def verify_roundtrip(path: Path, expected_frame: bytes) -> None:
    width, height, frames = read_shptd(path)
    if (width, height, len(frames)) != (OUTPUT_TILE, OUTPUT_TILE, FRAME_COUNT):
        raise ValueError(f"{path}: invalid roundtrip geometry")
    if any(frame != expected_frame for frame in frames):
        raise ValueError(f"{path}: roundtrip pixel mismatch")


def write_review(path: Path, panels: list[tuple[str, Image.Image]]) -> None:
    header = 28
    closeup_scale = 12
    repeat_scale = 2
    panel_width = OUTPUT_TILE * closeup_scale
    closeup_height = OUTPUT_TILE * closeup_scale
    repeat_height = panels[0][1].height * repeat_scale
    sheet = Image.new(
        "RGB",
        (panel_width * len(panels), header + closeup_height + header + repeat_height),
        (73, 86, 99),
    )
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for column, (label, panel) in enumerate(panels):
        x = column * panel_width
        draw.text((x + 8, 8), label.replace(", 6x6 repeat", ""), fill="white", font=font)
        tile = panel.crop((0, 0, OUTPUT_TILE, OUTPUT_TILE))
        sheet.paste(
            tile.resize((panel_width, closeup_height), Image.Resampling.NEAREST),
            (x, header),
        )
        repeat_y = header + closeup_height
        draw.text((x + 8, repeat_y + 8), "6x6 repeat field", fill="white", font=font)
        sheet.paste(
            panel.resize((panel_width, repeat_height), Image.Resampling.NEAREST),
            (x, repeat_y + header),
        )
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)


if __name__ == "__main__":
    raise SystemExit(main())
