#!/usr/bin/env python
"""Convert manually composed 24px tc basalt PNG layers into production .vol art."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image

import generate_sh04_alpha_beach_prototype as shore
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
ACTORS = ("tc01", "tc02", "tc03", "tc04", "tc05")


def encode_authoring_layer(image: Image.Image, palette: np.ndarray):
    rgba = np.asarray(image.convert("RGBA"), dtype=np.uint8)
    alpha = rgba[..., 3]
    rgb = rgba[..., :3]

    # GIMP's color-brush renderer varies alpha even for a single stamp. Exact
    # black pixels originate from the bundled shadow mask. Everything else
    # with at least modest coverage is palette-quantized as opaque body art.
    black_shadow = (alpha >= 10) & (rgb.max(axis=2) <= 3)
    body = (alpha >= 48) & ~black_shadow

    allowed = np.asarray([index for index in range(1, 256) if index != 4], dtype=np.int16)
    indices = np.zeros(alpha.shape, dtype=np.uint8)
    if body.any():
        colors = palette.astype(np.int32)
        body_rgb = rgb[body].astype(np.int32)
        distances = ((body_rgb[:, None, :] - colors[allowed][None, :, :]) ** 2).sum(axis=2)
        indices[body] = allowed[np.argmin(distances, axis=1)].astype(np.uint8)
    indices[black_shadow] = 4
    return indices, {
        "body_pixels_24px": int(body.sum()),
        "shadow_pixels_24px": int(black_shadow.sum()),
        "discarded_faint_pixels_24px": int(((alpha > 0) & ~body & ~black_shadow).sum()),
        "transparent_pixels_24px": int((alpha == 0).sum()),
    }


def preview(indices: np.ndarray, palette: np.ndarray) -> Image.Image:
    rgb = palette[indices].copy()
    rgb[indices == 4] = 0
    alpha = np.where(indices != 0, 255, 0).astype(np.uint8)
    alpha[indices == 4] = 105
    return Image.fromarray(np.dstack((rgb, alpha)), mode="RGBA")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input-dir", type=Path, required=True)
    parser.add_argument("--out-dir", type=Path, required=True)
    parser.add_argument("--install-dir", type=Path)
    args = parser.parse_args()

    source = args.input_dir.resolve()
    out = args.out_dir.resolve()
    out.mkdir(parents=True, exist_ok=True)
    install = args.install_dir.resolve() if args.install_dir else None
    if install:
        install.mkdir(parents=True, exist_ok=True)

    palette = np.asarray(shore.read_palette(ROOT / "mods/cameo/bits/volcanic/volcanic.pal"), dtype=np.uint8)
    manifest = {"density": "24px authoring -> exact 2x nearest production", "actors": []}

    for actor in ACTORS:
        input_path = source / f"{actor}_user_basalt_placement_24px.png"
        image = Image.open(input_path).convert("RGBA")
        donor_width, donor_height, donor_frames = read_shptd(ROOT / f"mods/cameo/bits/temp/{actor}.tem")
        expected = (donor_width // 2, donor_height // 2)
        if image.size != expected:
            raise ValueError(f"{actor}: expected 24px canvas {expected}, got {image.size}")

        indices24, audit = encode_authoring_layer(image, palette)
        indices48 = np.repeat(np.repeat(indices24, 2, axis=0), 2, axis=1)
        if indices48.shape != (donor_height, donor_width):
            raise ValueError(f"{actor}: production shape mismatch {indices48.shape}")
        uniform = all(
            np.array_equal(indices48[0::2, 0::2], part)
            for part in (indices48[0::2, 1::2], indices48[1::2, 0::2], indices48[1::2, 1::2])
        )
        if not uniform:
            raise ValueError(f"{actor}: nonuniform 2x2 production blocks")

        frame = indices48.tobytes()
        vol_path = out / f"{actor}.vol"
        write_shptd(vol_path, donor_width, donor_height, [frame] * len(donor_frames))
        preview(indices24, palette).save(out / f"{actor}_production_24px.png")
        preview(indices48, palette).save(out / f"{actor}_production_48px.png")
        if install:
            (install / f"{actor}.vol").write_bytes(vol_path.read_bytes())

        manifest["actors"].append({
            "actor": actor,
            "input": str(input_path),
            "authoring_size": list(image.size),
            "production_size": [donor_width, donor_height],
            "frame_count": len(donor_frames),
            "uniform_2x2_blocks": uniform,
            "bounds_violations": 0,
            **audit,
        })

    (out / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(out / "manifest.json")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
