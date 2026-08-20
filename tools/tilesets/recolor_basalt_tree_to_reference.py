#!/usr/bin/env python3
"""Preview a geometry-preserving basalt-tree recolor from an approved formation."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image

import generate_sh04_alpha_beach_prototype as shore
from build_t08_basalt_study import encode_volcanic_sprite


ROOT = Path(__file__).resolve().parents[2]


def luminance(rgb: np.ndarray) -> np.ndarray:
    return 0.2126 * rgb[..., 0] + 0.7152 * rgb[..., 1] + 0.0722 * rgb[..., 2]


def recolor(source: Image.Image, reference: Image.Image, brightness: float) -> Image.Image:
    src = np.asarray(source.convert("RGBA")).copy()
    ref = np.asarray(reference.convert("RGBA"))
    src_mask = src[..., 3] > 8
    ref_rgb = ref[..., :3][ref[..., 3] > 8].astype(np.float32)
    # Approved source sheets contain a handful of saturated edge/audit pixels
    # that are not part of the basalt material ramp. Exclude them so a rank
    # transfer cannot promote one into a conspicuous cyan/red highlight.
    ref_chroma = ref_rgb.max(axis=1) - ref_rgb.min(axis=1)
    ref_rgb = ref_rgb[ref_chroma <= 32]
    if not np.any(src_mask) or len(ref_rgb) == 0:
        raise ValueError("source and reference must contain visible pixels")

    # Transfer the reference ramp by luminance percentile. This preserves every
    # source alpha/geometry pixel while adopting the approved material colors.
    ref_rgb = ref_rgb[np.argsort(luminance(ref_rgb))]
    src_rgb = src[..., :3][src_mask].astype(np.float32)
    order = np.argsort(luminance(src_rgb))
    rank = np.empty(len(order), dtype=np.float32)
    rank[order] = np.linspace(0.0, 1.0, len(order), endpoint=True)
    # Stop at the 92nd percentile. The very brightest approved-source pixels
    # are isolated cap glints; mapping a larger tree's brightest pixels to them
    # can quantize into conspicuous non-basalt palette entries.
    indices = np.clip(np.rint(rank * 0.92 * (len(ref_rgb) - 1)), 0, len(ref_rgb) - 1).astype(int)
    transferred = np.clip(ref_rgb[indices] * brightness, 0, 255)
    src[..., :3][src_mask] = np.rint(transferred).astype(np.uint8)
    return Image.fromarray(src, "RGBA")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--body", type=Path, required=True)
    parser.add_argument("--shadow", type=Path, required=True)
    parser.add_argument("--reference", type=Path, required=True)
    parser.add_argument("--brightness", type=float, default=1.0)
    parser.add_argument("--out-dir", type=Path, required=True)
    args = parser.parse_args()

    out = args.out_dir.resolve()
    out.mkdir(parents=True, exist_ok=True)
    body = Image.open(args.body).convert("RGBA")
    shadow = Image.open(args.shadow).convert("RGBA")
    reference = Image.open(args.reference).convert("RGBA")
    result = recolor(body, reference, args.brightness)
    result.save(out / "t01_recolored_body_24px.png")
    shadow.save(out / "t01_unchanged_shadow_24px.png")

    size = (body.width * 2, body.height * 2)
    palette = shore.read_palette(ROOT / "mods/cameo/bits/volcanic/volcanic.pal")
    _, preview, audit = encode_volcanic_sprite(
        result.resize(size, Image.Resampling.NEAREST),
        shadow.resize(size, Image.Resampling.NEAREST),
        palette,
    )
    preview.save(out / "t01_recolored_production_palette.png")
    (out / "audit.json").write_text(json.dumps({
        "geometry_changed": False,
        "shadow_changed": False,
        "reference": str(args.reference.resolve()),
        "brightness": args.brightness,
        "palette_encoding": audit,
    }, indent=2) + "\n", encoding="utf-8")
    print(out / "t01_recolored_production_palette.png")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
