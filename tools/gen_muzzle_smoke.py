#!/usr/bin/env python
"""Generate the deterministic Cameo cannon muzzle-smoke spritesheet and QA preview."""

from __future__ import annotations

import argparse
import math
import random
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, PngImagePlugin


FRAME_SIZE = 32
FRAME_COUNT = 12
SCALE = 4


def generate_frame(index: int) -> Image.Image:
    phase = index / (FRAME_COUNT - 1)
    birth = 0.55 + 0.45 * min(1.0, phase / 0.18)
    fade = (1.0 - phase) ** 1.35
    opacity = birth * fade

    mask = Image.new("L", (FRAME_SIZE * SCALE, FRAME_SIZE * SCALE), 0)
    rng = random.Random(4107)
    center_x = FRAME_SIZE / 2
    center_y = 20.5 - 6.0 * phase

    for blob in range(7):
        angle = rng.uniform(0, math.tau)
        spread = (1.3 + blob * 0.22) * (1.0 + phase * 3.2)
        x = center_x + math.cos(angle) * spread + math.sin(phase * 2.7 + blob) * 0.35
        y = center_y + math.sin(angle) * spread * 0.55
        radius = (1.35 + rng.uniform(0.2, 1.0)) * (0.9 + phase * 1.9)
        alpha = int((205 + rng.randrange(40)) * opacity)

        layer = Image.new("L", mask.size, 0)
        draw = ImageDraw.Draw(layer)
        box = tuple(int(v * SCALE) for v in (x - radius, y - radius, x + radius, y + radius))
        draw.ellipse(box, fill=alpha)
        layer = layer.filter(ImageFilter.GaussianBlur((0.45 + phase * 0.45) * SCALE))
        mask = ImageChops.lighter(mask, layer)

    mask = mask.resize((FRAME_SIZE, FRAME_SIZE), Image.Resampling.LANCZOS)
    color = (
        int(206 - phase * 28),
        int(202 - phase * 27),
        int(192 - phase * 24),
    )
    frame = Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE), color + (0,))
    frame.putalpha(mask)
    return frame


def write_sheet(path: Path, frames: list[Image.Image]) -> None:
    sheet = Image.new("RGBA", (FRAME_SIZE * FRAME_COUNT, FRAME_SIZE), (0, 0, 0, 0))
    for index, frame in enumerate(frames):
        sheet.alpha_composite(frame, (index * FRAME_SIZE, 0))

    metadata = PngImagePlugin.PngInfo()
    metadata.add_text("FrameSize", f"{FRAME_SIZE},{FRAME_SIZE}")
    metadata.add_text("FrameAmount", str(FRAME_COUNT))
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path, pnginfo=metadata, optimize=True)


def write_preview(path: Path, frames: list[Image.Image]) -> None:
    scale = 6
    gap = 6
    cell = FRAME_SIZE * scale
    width = FRAME_COUNT * cell + (FRAME_COUNT + 1) * gap
    preview = Image.new("RGB", (width, cell + gap * 2), (31, 33, 35))
    checker = Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(checker)
    for y in range(0, FRAME_SIZE, 4):
        for x in range(0, FRAME_SIZE, 4):
            shade = 52 if (x // 4 + y // 4) % 2 else 42
            draw.rectangle((x, y, x + 3, y + 3), fill=(shade, shade, shade, 255))

    for index, frame in enumerate(frames):
        tile = Image.alpha_composite(checker, frame).resize((cell, cell), Image.Resampling.NEAREST)
        preview.paste(tile.convert("RGB"), (gap + index * (cell + gap), gap))

    path.parent.mkdir(parents=True, exist_ok=True)
    preview.save(path, optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--preview", type=Path, required=True)
    args = parser.parse_args()

    frames = [generate_frame(index) for index in range(FRAME_COUNT)]
    write_sheet(args.output, frames)
    write_preview(args.preview, frames)


if __name__ == "__main__":
    main()
