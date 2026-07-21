#!/usr/bin/env python
"""Replace p03/p04 wreckage previews with approved authored basalt art."""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image

import generate_sh04_alpha_beach_prototype as shore
from manual_river_delta.prepare_production import quantize
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
HANDOFF = Path(
    r"C:\Users\Blackrobe\Documents\agents\volcanic-theater"
    r"\basalt-columns-codex\handoff-shoreline-decoration-codex"
)
REPLACEMENTS = {
    "p03": ("1x1-a", 0),
    "p04": ("1x1-b", 1),
    "deca": ("1x1-a", 2),
    "decb": ("1x1-b", 3),
    "decc": ("1x1-a", 4),
    "decd": ("1x1-b", 5),
    "dece": ("1x1-a", 6),
    "decf": ("1x1-b", 7),
    "decg": ("1x1-a", 8),
    "dech": ("1x1-b", 9),
}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out-dir", type=Path, required=True)
    args = parser.parse_args()
    out = args.out_dir.resolve()
    candidates = out / "candidate-vols"
    candidates.mkdir(parents=True, exist_ok=True)

    palette = shore.read_palette(
        ROOT / "mods/cameo/bits/volcanic/volcanic.pal"
    )
    _, _, clear_frames = read_shptd(
        ROOT / "mods/cameo/bits/volcanic/clear1.vol"
    )

    for template, (variant, ground_frame) in REPLACEMENTS.items():
        ground_indices = np.frombuffer(
            clear_frames[ground_frame % len(clear_frames)], dtype=np.uint8
        ).reshape(48, 48)
        ground_rgb = shore.indices_rgb(ground_indices, palette)
        canvas = Image.fromarray(ground_rgb, mode="RGB").convert("RGBA")
        sprite_path = (
            HANDOFF / "variants" / variant / "footprint" / "combined-ground.png"
        )
        sprite = Image.open(sprite_path).convert("RGBA")
        if sprite.size != (48, 48):
            raise ValueError(f"{variant}: expected 48x48, got {sprite.size}")
        composite = Image.alpha_composite(canvas, sprite).convert("RGB")
        indexed, indices = quantize(composite, palette)
        if cadence_errors(indices):
            raise ValueError(f"{template}: replacement lost strict 2x cadence")

        write_shptd(
            candidates / f"{template}.vol",
            48,
            48,
            [indices.tobytes()],
        )
        rgb = shore.indices_rgb(indices, palette)
        Image.fromarray(rgb, mode="RGB").save(
            out / f"volcanic_inland_river_candidate_{template}.png"
        )
        Image.fromarray(rgb, mode="RGB").save(
            out / f"volcanic_basalt_replacement_{template}_{variant}.png"
        )
        print(out / f"volcanic_basalt_replacement_{template}_{variant}.png")
    return 0


def cadence_errors(indices):
    blocks = indices.reshape(24, 2, 24, 2).transpose(0, 2, 1, 3)
    return int(np.count_nonzero(np.any(
        blocks != blocks[:, :, :1, :1], axis=(2, 3)
    )))


if __name__ == "__main__":
    raise SystemExit(main())
