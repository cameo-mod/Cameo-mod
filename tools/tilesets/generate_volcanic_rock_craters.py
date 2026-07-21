#!/usr/bin/env python
"""Recolor the Dune 2000 rock-crater frames for Volcanic terrain.

The input sheets are 16-frame, 32px-cell PNGs baked from DATA.R16 with
``--bake-sequence ... 1 --preserve-offsets``.  The production sheet retains
the original DATA.R16 frame numbers so it can be selected using a
TilesetFilenames override without changing the existing sequence starts.
"""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont, PngImagePlugin

from shptd import read_shptd


ROOT = Path(__file__).resolve().parents[2]
CELL = 32
RUNTIME_CELL = 48
FRAME_COUNT = 146
CRATER1_START = 114
CRATER2_START = 130

# Dark volcanic stone with restrained warm, oxidized highlights.
RAMP = (
    (0.00, (23, 21, 23)),
    (0.34, (43, 35, 36)),
    (0.70, (75, 59, 54)),
    (1.00, (118, 89, 78)),
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    bits = ROOT / "mods/cameo/bits/volcanic"
    parser.add_argument(
        "--crater1",
        type=Path,
        default=bits / "volcanic_rockcrater1_source_32px.png",
    )
    parser.add_argument(
        "--crater2",
        type=Path,
        default=bits / "volcanic_rockcrater2_source_32px.png",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=bits / "volcanic_rockcraters.png",
    )
    parser.add_argument(
        "--review",
        type=Path,
        default=(
            Path.home()
            / "Documents/agents/volcanic-theater/craters/2026-07-15"
            / "d2k_vs_volcanic_rock_craters.png"
        ),
    )
    return parser.parse_args()


def interpolate_ramp(t: np.ndarray) -> np.ndarray:
    result = np.zeros((*t.shape, 3), dtype=np.float32)
    for index in range(len(RAMP) - 1):
        left_t, left_rgb = RAMP[index]
        right_t, right_rgb = RAMP[index + 1]
        mask = (t >= left_t) & (t <= right_t)
        local = np.clip((t - left_t) / (right_t - left_t), 0.0, 1.0)
        left = np.asarray(left_rgb, dtype=np.float32)
        right = np.asarray(right_rgb, dtype=np.float32)
        result[mask] = (left + (right - left) * local[mask, None])
    result[t < RAMP[0][0]] = RAMP[0][1]
    result[t > RAMP[-1][0]] = RAMP[-1][1]
    return np.clip(np.rint(result), 0, 255).astype(np.uint8)


def recolor(image: Image.Image) -> Image.Image:
    rgba = np.asarray(image.convert("RGBA"), dtype=np.uint8)
    rgb = rgba[:, :, :3].astype(np.float32)
    luma = 0.2126 * rgb[:, :, 0] + 0.7152 * rgb[:, :, 1] + 0.0722 * rgb[:, :, 2]

    # DATA.R16's useful crater range is approximately 44..202.  Keeping this
    # calibration fixed makes both crater families share one coherent ramp.
    normalized = np.clip((luma - 42.0) / 160.0, 0.0, 1.0)
    # Retain shadow separation while preventing the brightest sandstone pixels
    # from dominating the dark Volcanic ground.
    normalized = np.power(normalized, 0.92)
    result = np.dstack((interpolate_ramp(normalized), rgba[:, :, 3]))
    result[rgba[:, :, 3] == 0, :3] = 0
    return Image.fromarray(result, mode="RGBA")


def frames(sheet: Image.Image) -> list[Image.Image]:
    if sheet.height != CELL or sheet.width != 16 * CELL:
        raise ValueError(
            f"Expected a 16-frame {CELL}px sheet, got {sheet.width}x{sheet.height}"
        )
    return [sheet.crop((i * CELL, 0, (i + 1) * CELL, CELL)) for i in range(16)]


def read_palette(path: Path) -> np.ndarray:
    raw = np.frombuffer(path.read_bytes(), dtype=np.uint8).reshape(256, 3)
    return np.minimum(raw.astype(np.uint16) * 4, 255).astype(np.uint8)


def volcanic_ground() -> Image.Image:
    bits = ROOT / "mods/cameo/bits/volcanic"
    palette = read_palette(bits / "volcanic.pal")
    width, height, raw_frames = read_shptd(bits / "clear1.vol")
    indices = np.frombuffer(raw_frames[0], dtype=np.uint8).reshape(height, width)
    return Image.fromarray(palette[indices], mode="RGB").convert("RGBA")


def checkerboard(size: tuple[int, int]) -> Image.Image:
    result = Image.new("RGBA", size, (78, 78, 78, 255))
    draw = ImageDraw.Draw(result)
    block = 8
    for y in range(0, size[1], block):
        for x in range(0, size[0], block):
            if (x // block + y // block) % 2:
                draw.rectangle((x, y, x + block - 1, y + block - 1), fill=(118, 118, 118, 255))
    return result


def runtime_frame(frame: Image.Image) -> Image.Image:
    return frame.resize((RUNTIME_CELL, RUNTIME_CELL), Image.Resampling.NEAREST)


def build_review(
    crater1_source: list[Image.Image],
    crater2_source: list[Image.Image],
    crater1_result: list[Image.Image],
    crater2_result: list[Image.Image],
) -> Image.Image:
    samples = (0, 4, 8, 12, 15)
    panel_w = RUNTIME_CELL * len(samples)
    header = 24
    row_h = RUNTIME_CELL + header
    sheet = Image.new("RGB", (panel_w * 3, row_h * 2), (73, 86, 99))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    ground = volcanic_ground()

    for row, (source_frames, result_frames, name) in enumerate(
        (
            (crater1_source, crater1_result, "rockcrater1"),
            (crater2_source, crater2_result, "rockcrater2"),
        )
    ):
        y = row * row_h
        labels = (
            f"{name}: original D2K",
            f"{name}: Volcanic recolor",
            f"{name}: recolor on clear ground",
        )
        for column, label in enumerate(labels):
            x0 = column * panel_w
            draw.text((x0 + 5, y + 6), label, fill="white", font=font)
            for sample_column, frame_index in enumerate(samples):
                x = x0 + sample_column * RUNTIME_CELL
                if column == 0:
                    panel = checkerboard((RUNTIME_CELL, RUNTIME_CELL))
                    panel.alpha_composite(runtime_frame(source_frames[frame_index]))
                elif column == 1:
                    panel = checkerboard((RUNTIME_CELL, RUNTIME_CELL))
                    panel.alpha_composite(runtime_frame(result_frames[frame_index]))
                else:
                    panel = ground.copy()
                    panel.alpha_composite(runtime_frame(result_frames[frame_index]))
                sheet.paste(panel.convert("RGB"), (x, y + header))
    return sheet


def main() -> int:
    args = parse_args()
    source1 = Image.open(args.crater1).convert("RGBA")
    source2 = Image.open(args.crater2).convert("RGBA")
    result1 = recolor(source1)
    result2 = recolor(source2)

    source_frames1 = frames(source1)
    source_frames2 = frames(source2)
    result_frames1 = frames(result1)
    result_frames2 = frames(result2)

    production = Image.new("RGBA", (FRAME_COUNT * CELL, CELL), (0, 0, 0, 0))
    for index, frame in enumerate(result_frames1):
        production.alpha_composite(frame, ((CRATER1_START + index) * CELL, 0))
    for index, frame in enumerate(result_frames2):
        production.alpha_composite(frame, ((CRATER2_START + index) * CELL, 0))

    metadata = PngImagePlugin.PngInfo()
    metadata.add_text("FrameSize", f"{CELL},{CELL}")
    metadata.add_text("FrameAmount", str(FRAME_COUNT))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    production.save(args.output, pnginfo=metadata, optimize=True)

    review = build_review(
        source_frames1,
        source_frames2,
        result_frames1,
        result_frames2,
    )
    args.review.parent.mkdir(parents=True, exist_ok=True)
    review.save(args.review)
    print(f"Production: {args.output.resolve()}")
    print(f"Review: {args.review.resolve()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
