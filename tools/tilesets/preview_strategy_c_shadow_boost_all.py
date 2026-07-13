#!/usr/bin/env python
"""Build the complete Strategy C review with the approved +38% shadow remap."""

from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image

import generate_sh04_alpha_beach_prototype as shore
import preview_strategy_c_all_worked_tiles as full
import prototype_donor_recolored_bridge_ground as strategy
import volcanic_art_utils as art
from manual_river_delta.prepare_production import quantize
from shptd import read_shptd


OUT = Path.home() / "Documents/agents/volcanic-theater/ground/strategy-c-shadow-boost-full-family-review-01"
SHADOW_STRENGTH = art.APPROVED_SHADOW_STRENGTH
SHADOW_PERCENTILE = art.APPROVED_SHADOW_PERCENTILE


def main() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    palette = shore.read_palette(full.BITS / "volcanic.pal")
    temp_palette = shore.read_palette(
        full.ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    )
    donor, low, high, clear_variants = full.build_clear_family(temp_palette, palette)
    canonical = clear_variants[0]

    reviews: dict[str, list[str]] = {}
    reviews["ground"] = write_ground_review(donor, clear_variants, palette)
    reviews["cliffs"] = build_pages(
        "cliffs",
        full.CLIFFS,
        lambda name: full.cliff_pair(name, canonical, low, high, palette, temp_palette),
        palette,
        page_size=8,
    )
    reviews["shorelines"] = build_pages(
        "shorelines",
        full.SHORES,
        lambda name: full.shore_pair(name, canonical, low, high, palette, temp_palette),
        palette,
        page_size=6,
    )
    reviews["rivers"] = build_pages(
        "rivers",
        full.RIVERS,
        lambda name: full.river_pair(name, canonical, low, high, palette, temp_palette),
        palette,
        page_size=5,
    )
    reviews["fords_crossings"] = build_pages(
        "fords_crossings",
        full.FORDS,
        lambda name: full.river_pair(name, canonical, low, high, palette, temp_palette),
        palette,
        page_size=5,
    )
    reviews["bridges"] = build_pages(
        "human_bridges",
        full.BRIDGES,
        lambda name: full.bridge_pair(name, clear_variants, low, high, palette, temp_palette),
        palette,
        page_size=6,
    )
    reviews["liquid_references"] = write_liquid_review(palette)

    manifest = {
        "preview_only": True,
        "strategy": "C two-material hybrid with approved shadow remap",
        "shadow_strength": SHADOW_STRENGTH,
        "shadow_percentile": SHADOW_PERCENTILE,
        "shadow_target_rgb": [12, 8, 8],
        "hot_lava_protected": True,
        "ground": "RA Temperate donor texture mapped to subdued Volcanic ramp 11-21",
        "structures": "preserved donor geometry with independent normalization and ramp 10-29",
        "liquid": "unchanged production w1/w2",
        "counts": {
            "clear_variants": 16,
            "cliffs": len(full.CLIFFS),
            "shorelines": len(full.SHORES),
            "rivers": len(full.RIVERS),
            "fords_crossings": len(full.FORDS),
            "human_bridges": len(full.BRIDGES),
        },
        "reviews": reviews,
    }
    path = OUT / "strategy_c_shadow_boost_full_family_manifest.json"
    path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(path.resolve())
    return 0


def darken_shadows(image: Image.Image, palette) -> Image.Image:
    rgb = np.asarray(image.convert("RGB"), dtype=np.uint8)
    visible = ~np.all(
        rgb == np.asarray(shore.BACKGROUND, dtype=np.uint8), axis=2
    )
    darkened = art.apply_approved_shadow_boost(rgb, visible=visible)
    quantized, _ = quantize(Image.fromarray(darkened, mode="RGB"), palette)
    result = np.asarray(quantized.convert("RGB"), dtype=np.uint8).copy()
    result[~visible] = rgb[~visible]
    return Image.fromarray(result, mode="RGB")


def write_ground_review(donor, candidate, palette):
    _, _, current_frames = read_shptd(full.BITS / "clear1.vol")
    current = np.stack([
        shore.indices_rgb(
            np.frombuffer(frame, dtype=np.uint8).reshape(full.TILE, full.TILE),
            palette,
        )
        for frame in current_frames
    ])
    paths = []
    for start in range(0, 16, 8):
        panels = []
        for i in range(start, start + 8):
            panels.append((f"clear1 v{i:02d}: Temperate donor", Image.fromarray(donor[i])))
            panels.append((f"clear1 v{i:02d}: current", Image.fromarray(current[i])))
            boosted = darken_shadows(Image.fromarray(candidate[i]), palette)
            panels.append((f"clear1 v{i:02d}: Strategy C +38% shadows", boosted))
        path = OUT / f"ground_clear1_v{start:02d}_v{start + 7:02d}.png"
        full.write_page(path, panels, columns=3, scale=2)
        paths.append(str(path.resolve()))
        print(path.resolve())
    return paths


def build_pages(category, names, pair_fn, palette, page_size):
    paths = []
    for start in range(0, len(names), page_size):
        chunk = names[start:start + page_size]
        panels = []
        for name in chunk:
            before, candidate = pair_fn(name)
            panels.append((f"{name}: current production", before))
            panels.append(
                (f"{name}: Strategy C +38% shadows", darken_shadows(candidate, palette))
            )
        path = OUT / f"{category}_{chunk[0]}_{chunk[-1]}.png"
        full.write_page(path, panels, columns=2, scale=2)
        paths.append(str(path.resolve()))
        print(path.resolve())
    return paths


def write_liquid_review(palette):
    panels = []
    for name, expected in (("w1", 1), ("w2", 4)):
        width, height, frames = read_shptd(full.BITS / f"{name}.vol")
        for i, frame in enumerate(frames[:expected]):
            indices = np.frombuffer(frame, dtype=np.uint8).reshape(height, width)
            image = Image.fromarray(shore.indices_rgb(indices, palette))
            panels.append((f"{name} frame {i}: unchanged liquid reference", image))
    path = OUT / "liquid_w1_w2_unchanged_references.png"
    full.write_page(path, panels, columns=2, scale=4)
    print(path.resolve())
    return [str(path.resolve())]


if __name__ == "__main__":
    raise SystemExit(main())
