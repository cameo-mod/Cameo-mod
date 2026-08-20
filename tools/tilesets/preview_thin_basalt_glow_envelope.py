#!/usr/bin/env python
"""Preview a one-source-row unrestricted glow spine for 2x1 basalt formations."""

from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image

import generate_sh04_alpha_beach_prototype as shore
import place_authored_basalt_columns_on_shores as basalt
from shptd import read_shptd


ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = (
    Path.home()
    / "Documents/agents/volcanic-theater/shorelines/authored-basalt-placement/"
    "review-17-brighter-thin-envelope-codex"
)
VARIANTS = ("2x1-a", "2x1-b")


def thin_envelope(
    envelope: Image.Image,
    formation: Image.Image,
    *,
    rows: int,
) -> Image.Image:
    if rows not in (1, 2):
        raise ValueError("thin envelope preview supports one or two rows")
    values = np.asarray(envelope.convert("RGBA"), dtype=np.uint8).copy()
    envelope_alpha = values[:, :, 3].astype(np.uint16)
    formation_alpha = np.asarray(formation.convert("RGBA"), dtype=np.uint8)[:, :, 3]
    visible_strength = envelope_alpha * (255 - formation_alpha.astype(np.uint16))
    keep = np.zeros(envelope_alpha.shape, dtype=bool)
    for x in range(envelope_alpha.shape[1]):
        if np.any(envelope_alpha[:, x] > 0):
            y = int(np.argmax(visible_strength[:, x]))
            keep[y, x] = True
            if rows == 2:
                neighbor = y + 1 if y + 1 < envelope_alpha.shape[0] else y - 1
                if envelope_alpha[neighbor, x] == 0:
                    neighbor = y - 1
                if neighbor >= 0 and envelope_alpha[neighbor, x] > 0:
                    keep[neighbor, x] = True
    values[~keep] = 0
    return Image.fromarray(values, mode="RGBA")


def shift_north_one_source_pixel(image: Image.Image) -> Image.Image:
    values = np.asarray(image.convert("RGBA"), dtype=np.uint8)
    shifted = np.zeros_like(values)
    shifted[:-1] = values[1:]
    return Image.fromarray(shifted, mode="RGBA")


def boost_alpha(image: Image.Image, factor: float) -> Image.Image:
    values = np.asarray(image.convert("RGBA"), dtype=np.uint8).copy()
    values[:, :, 3] = np.minimum(
        255,
        np.rint(values[:, :, 3].astype(np.float64) * factor),
    ).astype(np.uint8)
    return Image.fromarray(values, mode="RGBA")


def lava_background() -> Image.Image:
    palette = shore.read_palette(ROOT / "mods/cameo/bits/volcanic/volcanic.pal")
    width, height, frames = read_shptd(ROOT / "mods/cameo/bits/volcanic/w1.vol")
    if (width, height) != (48, 48) or not frames:
        raise ValueError("unexpected production w1.vol geometry")
    indices = np.frombuffer(frames[0], dtype=np.uint8).reshape(height, width)
    tile = Image.fromarray(shore.indices_rgb(indices, palette), mode="RGB")
    result = Image.new("RGB", (192, 144))
    for y in range(0, result.height, tile.height):
        for x in range(0, result.width, tile.width):
            result.paste(tile, (x, y))
    return result


def compose(background: Image.Image, sprite: Image.Image) -> Image.Image:
    result = background.convert("RGBA")
    x = (result.width - sprite.width) // 2
    y = (result.height - sprite.height) // 2
    result.alpha_composite(sprite, (x, y))
    return result.convert("RGB")


def main() -> int:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    background = lava_background()
    panels = []
    records = []
    asset_root = basalt.DEFAULT_ASSET_DIR / "variants"

    for variant in VARIANTS:
        source_dir = asset_root / variant / "source-24px"
        with Image.open(source_dir / "formation.png") as image:
            formation_24 = image.convert("RGBA")
        with Image.open(source_dir / "lava-bounce.png") as image:
            bounce_24 = image.convert("RGBA")
        with Image.open(source_dir / "lava-glow-envelope.png") as image:
            envelope_24 = image.convert("RGBA")

        thin_one_unshifted_24 = thin_envelope(envelope_24, formation_24, rows=1)
        thin_one_24 = shift_north_one_source_pixel(thin_one_unshifted_24)
        thin_15_24 = boost_alpha(thin_one_24, 1.5)
        thin_20_24 = boost_alpha(thin_one_24, 2.0)
        reinforced_24 = basalt.apply_soft_light_bounce(
            formation_24,
            bounce_24,
            strength=basalt.REINFORCED_BOUNCE_STRENGTH,
        )
        size_48 = (formation_24.width * 2, formation_24.height * 2)
        formation_48 = reinforced_24.resize(size_48, Image.Resampling.NEAREST)
        thin_one_48 = thin_one_24.resize(size_48, Image.Resampling.NEAREST)
        thin_15_48 = thin_15_24.resize(size_48, Image.Resampling.NEAREST)
        thin_20_48 = thin_20_24.resize(size_48, Image.Resampling.NEAREST)
        if not all(
            basalt.strict_2x(np.asarray(image, dtype=np.uint8))
            for image in (formation_48, thin_one_48, thin_15_48, thin_20_48)
        ):
            raise ValueError(f"{variant}: preview layers are not strict nearest-neighbor 2x")

        thin_one_sprite = Image.alpha_composite(thin_one_48, formation_48)
        thin_15_sprite = Image.alpha_composite(thin_15_48, formation_48)
        thin_20_sprite = Image.alpha_composite(thin_20_48, formation_48)
        panels.extend(
            (
                (f"{variant}: selected A baseline alpha", compose(background, thin_one_sprite)),
                (f"{variant}: brighter A, alpha x1.5", compose(background, thin_15_sprite)),
                (f"{variant}: brighter A, alpha x2.0", compose(background, thin_20_sprite)),
            )
        )

        formation_mask = np.asarray(formation_24, dtype=np.uint8)[:, :, 3] > 0
        thin_one_mask = np.asarray(thin_one_24, dtype=np.uint8)[:, :, 3] > 0
        thin_one_external = thin_one_mask & ~formation_mask
        records.append(
            {
                "variant": variant,
                "thin_method": (
                    "retain the original unrestricted-envelope RGBA at only the strongest "
                    "visible row per source column, measured after formation-alpha occlusion"
                ),
                "source_offset_24px": [0, -1],
                "production_offset_48px": [0, -2],
                "one_row_external_pixels_24px": int(np.count_nonzero(thin_one_external)),
                "one_row_translation_exact": bool(
                    np.array_equal(
                        np.asarray(thin_one_24)[:-1],
                        np.asarray(thin_one_unshifted_24)[1:],
                    )
                    and not np.any(np.asarray(thin_one_24)[-1])
                ),
                "selected_visible_band": "one authoring row / two production pixels",
                "brightness_options": [1.0, 1.5, 2.0],
                "brightness_channel": "alpha only; RGB unchanged",
                "strict_nearest_neighbor_2x": True,
                "internal_bounce_alpha_multiplier": 2.0,
                "w3c_soft_light_passes": 1,
                "envelope_composites": 1,
                "ground_shadow": False,
            }
        )

    review_path = OUT_DIR / "thin-envelope-brightness-study-codex.png"
    shore.write_review_sheet(review_path, panels, columns=3, scale=2)
    audit = {
        "owner": "Codex continuation",
        "preview_only": True,
        "production_files_modified": False,
        "purpose": (
            "compare brightness levels for the selected one-row thin unrestricted "
            "bottom envelope shifted north by one authoring pixel, without crack detection"
        ),
        "review": str(review_path.resolve()),
        "records": records,
        "result": "PASS",
    }
    audit_path = OUT_DIR / "thin-bottom-envelope-audit-codex.json"
    audit_path.write_text(json.dumps(audit, indent=2) + "\n", encoding="utf-8")
    print(review_path.resolve())
    print(audit_path.resolve())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
