#!/usr/bin/env python
"""Build production Water Cliff connectivity sheets from Volcanic brush geometry."""

from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import generate_sh04_alpha_beach_prototype as shore
from shptd import read_shptd


ROOT = Path(__file__).resolve().parents[2]
TILE = 48
BACKGROUND = (73, 86, 99)


@dataclass(frozen=True)
class Segment:
    name: str
    start: tuple[int, int]
    end: tuple[int, int]


SEGMENTS = {
    # Left-facing shoreline transition, straight variants, shoreline exit.
    "wc01": Segment("wc01", (2, 1), (0, 1)),
    "wc02": Segment("wc02", (2, 2), (0, 1)),
    "wc03": Segment("wc03", (2, 1), (0, 1)),
    "wc04": Segment("wc04", (2, 1), (0, 1)),
    "wc05": Segment("wc05", (2, 1), (0, 1)),
    "wc06": Segment("wc06", (2, 1), (0, 2)),
    "wc07": Segment("wc07", (2, 1), (0, 1)),
    # Up-facing family.
    "wc08": Segment("wc08", (1, 2), (1, 0)),
    "wc09": Segment("wc09", (2, 2), (1, 0)),
    "wc10": Segment("wc10", (1, 2), (1, 0)),
    "wc11": Segment("wc11", (1, 2), (1, 0)),
    "wc12": Segment("wc12", (1, 2), (1, 0)),
    "wc13": Segment("wc13", (1, 2), (2, 0)),
    "wc14": Segment("wc14", (1, 2), (1, 0)),
    # Right-facing family.
    "wc15": Segment("wc15", (0, 1), (2, 1)),
    "wc16": Segment("wc16", (0, 1), (2, 2)),
    "wc17": Segment("wc17", (0, 1), (2, 1)),
    "wc18": Segment("wc18", (0, 1), (2, 1)),
    "wc19": Segment("wc19", (0, 1), (2, 1)),
    "wc20": Segment("wc20", (0, 2), (2, 1)),
    "wc21": Segment("wc21", (0, 1), (2, 1)),
    # Down-facing family.
    "wc22": Segment("wc22", (1, 0), (1, 2)),
    "wc23": Segment("wc23", (2, 0), (1, 2)),
    "wc24": Segment("wc24", (1, 0), (1, 2)),
    "wc25": Segment("wc25", (1, 0), (1, 2)),
    "wc26": Segment("wc26", (1, 0), (1, 2)),
    "wc27": Segment("wc27", (1, 0), (2, 2)),
    "wc28": Segment("wc28", (1, 0), (1, 2)),
    # Clockwise and counter-clockwise turn families.
    "wc29": Segment("wc29", (2, 1), (1, 0)),
    "wc30": Segment("wc30", (1, 2), (2, 1)),
    "wc31": Segment("wc31", (0, 1), (1, 2)),
    "wc32": Segment("wc32", (1, 0), (0, 1)),
    "wc33": Segment("wc33", (1, 0), (2, 1)),
    "wc34": Segment("wc34", (2, 1), (1, 2)),
    "wc35": Segment("wc35", (1, 2), (0, 1)),
    "wc36": Segment("wc36", (0, 1), (1, 0)),
}


CHAINS = (
    ("Left family: beach -> WaterCliff.L -> beach", ("wc07", "wc02", "wc03", "wc04", "wc05", "wc06", "wc01")),
    ("Up family: beach -> WaterCliff.U -> beach", ("wc14", "wc09", "wc10", "wc11", "wc12", "wc13", "wc08")),
    ("Right family: beach -> WaterCliff.R -> beach", ("wc15", "wc16", "wc17", "wc18", "wc19", "wc20", "wc21")),
    ("Down family: beach -> WaterCliff.D -> beach", ("wc22", "wc23", "wc24", "wc25", "wc26", "wc27", "wc28")),
    ("Clockwise turn loop", ("wc29", "wc30", "wc31", "wc32")),
    ("Counter-clockwise turn loop", ("wc33", "wc34", "wc35", "wc36")),
)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--out-dir",
        type=Path,
        default=Path.home() / "Documents/agents/volcanic-theater/water-cliffs/connectivity-review-01",
    )
    args = parser.parse_args()
    out = args.out_dir.resolve()
    out.mkdir(parents=True, exist_ok=True)

    palette = shore.read_palette(ROOT / "mods/cameo/bits/volcanic/volcanic.pal")
    yaml_path = ROOT / "mods/cameo/tilesets/volcanic.yaml"
    bits = ROOT / "mods/cameo/bits/volcanic"
    sprites = {
        name: load_template(bits / f"{name}.vol", yaml_path, palette)
        for name in (f"wc{i:02d}" for i in range(1, 39))
    }

    panels = []
    for slug, (label, names) in enumerate(CHAINS, start=1):
        image = render_chain(names, sprites)
        path = out / f"{slug:02d}_{names[0]}_to_{names[-1]}_connectivity.png"
        image.save(path)
        panels.append((label, image))

    standalone = Image.new("RGBA", (TILE * 4, TILE * 2), (0, 0, 0, 0))
    standalone.alpha_composite(sprites["wc37"], (0, 0))
    standalone.alpha_composite(sprites["wc38"], (TILE * 2, 0))
    panels.append(("Unsegmented wc37 and wc38", standalone))
    write_sheet(out / "water_cliff_connectivity_full_review.png", panels)
    print((out / "water_cliff_connectivity_full_review.png").resolve())
    return 0


def load_template(path: Path, yaml_path: Path, palette) -> Image.Image:
    spec = shore.read_template_spec(yaml_path, path.name)
    width, height, frames = read_shptd(path)
    if (width, height, len(frames)) != (TILE, TILE, spec.columns * spec.rows):
        raise ValueError(f"{path.name}: geometry does not match YAML")
    result = Image.new("RGBA", (spec.columns * TILE, spec.rows * TILE), (0, 0, 0, 0))
    for index in spec.terrain:
        row, column = divmod(index, spec.columns)
        indices = np.frombuffer(frames[index], dtype=np.uint8).reshape(TILE, TILE)
        rgb = shore.indices_rgb(indices, palette)
        tile = Image.fromarray(
            np.dstack((rgb, np.full((TILE, TILE), 255, dtype=np.uint8))),
            mode="RGBA",
        )
        result.alpha_composite(tile, (column * TILE, row * TILE))
    return result


def render_chain(names: tuple[str, ...], sprites: dict[str, Image.Image]) -> Image.Image:
    origins = [(0, 0)]
    for previous, current in zip(names, names[1:]):
        px, py = origins[-1]
        prior = SEGMENTS[previous]
        following = SEGMENTS[current]
        origins.append(
            (
                px + (prior.end[0] - following.start[0]) * TILE,
                py + (prior.end[1] - following.start[1]) * TILE,
            )
        )
    min_x = min(x for x, _ in origins)
    min_y = min(y for _, y in origins)
    max_x = max(x + sprites[name].width for (x, _), name in zip(origins, names))
    max_y = max(y + sprites[name].height for (_, y), name in zip(origins, names))
    canvas = Image.new("RGBA", (max_x - min_x, max_y - min_y), (0, 0, 0, 0))
    for (x, y), name in zip(origins, names):
        canvas.alpha_composite(sprites[name], (x - min_x, y - min_y))
    return canvas


def write_sheet(path: Path, panels: list[tuple[str, Image.Image]]) -> None:
    scale = 2
    header = 28
    margin = 12
    width = max(image.width for _, image in panels) * scale
    heights = [image.height * scale + header + margin for _, image in panels]
    sheet = Image.new("RGB", (width, sum(heights)), BACKGROUND)
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    y = 0
    for (label, image), height in zip(panels, heights):
        draw.text((7, y + 8), label, fill="white", font=font)
        checks = np.indices((image.height, image.width)).sum(axis=0) // 8 % 2 == 0
        checker_rgb = np.empty((image.height, image.width, 3), dtype=np.uint8)
        checker_rgb[checks] = (154, 154, 154)
        checker_rgb[~checks] = (102, 102, 102)
        flattened = Image.fromarray(
            np.dstack(
                (checker_rgb, np.full((image.height, image.width), 255, dtype=np.uint8))
            ),
            mode="RGBA",
        )
        flattened.alpha_composite(image)
        rendered = flattened.convert("RGB").resize(
            (image.width * scale, image.height * scale), Image.Resampling.NEAREST
        )
        sheet.paste(rendered, ((width - rendered.width) // 2, y + header))
        y += height
    sheet.save(path)


if __name__ == "__main__":
    raise SystemExit(main())
