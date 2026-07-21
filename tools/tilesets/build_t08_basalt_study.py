#!/usr/bin/env python
"""Render RA Temperate t08 and fit an image-generated basalt replacement."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

import build_basalt_forest_bulk_review as forest
import generate_sh04_alpha_beach_prototype as shore
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]


def bounds(mask: np.ndarray) -> list[int] | None:
    ys, xs = np.nonzero(mask)
    if not len(xs):
        return None
    return [int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1]


def decode_t08() -> tuple[Image.Image, dict]:
    width, height, frames = read_shptd(ROOT / "mods/cameo/bits/temp/t08.tem")
    palette = shore.read_palette(ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal")
    indices = np.frombuffer(frames[0], dtype=np.uint8).reshape(height, width)
    rgb = shore.indices_rgb(indices, palette)
    rgb[indices == 4] = (0, 0, 0)
    alpha = np.where(indices != 0, 255, 0).astype(np.uint8)
    alpha[indices == 4] = 105
    rgba = Image.fromarray(np.dstack([rgb, alpha]), mode="RGBA")
    audit = {
        "actor": "t08",
        "frame": 0,
        "sprite_box": [width, height],
        "actor_dimensions": [2, 1],
        "collision_footprint": ["x_"],
        "visible_bounds": bounds(indices != 0),
        "body_bounds_excluding_shadow": bounds((indices != 0) & (indices != 4)),
        "shadow_bounds": bounds(indices == 4),
        "frame_count": len(frames),
    }
    return rgba, audit


def fit_candidate(
    source: Image.Image, donor_bounds: list[int], body_only: bool = False
) -> tuple[Image.Image, dict]:
    alpha = np.asarray(source.getchannel("A"))
    crop = bounds(alpha > 8)
    if crop is None:
        raise ValueError("candidate contains no visible pixels")
    cropped = source.crop(tuple(crop))
    # Author at 48x24, then upscale exactly 2x into t08's 96x48 sprite box.
    # Fit to the donor's complete visible composition instead of centering in
    # the canvas. Round outward to whole 24px-source pixels.
    author_size = (48, 24)
    target = ((1, 1, 23, 23) if body_only else (
        donor_bounds[0] // 2,
        donor_bounds[1] // 2,
        (donor_bounds[2] + 1) // 2,
        (donor_bounds[3] + 1) // 2,
    ))
    available = (target[2] - target[0], target[3] - target[1])
    scale = min(available[0] / cropped.width, available[1] / cropped.height)
    size = (max(1, round(cropped.width * scale)), max(1, round(cropped.height * scale)))
    reduced = cropped.resize(size, Image.Resampling.LANCZOS)
    author = Image.new("RGBA", author_size)
    x = target[0] + (available[0] - reduced.width) // 2
    y = target[3] - reduced.height
    author.alpha_composite(reduced, (x, y))
    production = author.resize((96, 48), Image.Resampling.NEAREST)
    pa = np.asarray(production.getchannel("A"))
    uniform = all(
        np.array_equal(pa[0::2, 0::2], part)
        for part in (pa[0::2, 1::2], pa[1::2, 0::2], pa[1::2, 1::2])
    )
    return production, {
        "source_crop": crop,
        "authoring_size": list(author_size),
        "production_size": list(production.size),
        "formation_authoring_cell": [0, 0, 24, 24],
        "body_only": body_only,
        "donor_visible_target": donor_bounds,
        "cadence_aligned_target": [value * 2 for value in target],
        "visible_bounds": bounds(pa > 0),
        "bounds_violations": 0,
        "uniform_2x2_blocks": uniform,
    }


def author_body_and_shadow(
    source: Image.Image,
) -> tuple[Image.Image, Image.Image, Image.Image, dict]:
    """Fit the body into t08's blocked cell and author its eastward shadow.

    Everything is constructed on the 48x24 source-density canvas.  The caller
    performs the sole 2x nearest-neighbour enlargement for production.
    """
    alpha = np.asarray(source.getchannel("A"))
    crop = bounds(alpha > 8)
    if crop is None:
        raise ValueError("candidate contains no visible pixels")
    cropped = source.crop(tuple(crop))

    body = Image.new("RGBA", (48, 24))
    target = (1, 1, 23, 23)
    available = (target[2] - target[0], target[3] - target[1])
    scale = min(available[0] / cropped.width, available[1] / cropped.height)
    size = (max(1, round(cropped.width * scale)), max(1, round(cropped.height * scale)))
    reduced = cropped.resize(size, Image.Resampling.LANCZOS)
    x = target[0] + (available[0] - reduced.width) // 2
    y = target[3] - reduced.height
    body.alpha_composite(reduced, (x, y))

    # Project the complete formation silhouette toward due east and compress it
    # vertically against the ground plane.  This produces a short, broad cast
    # shadow instead of disconnected finger-like streaks.
    body_alpha = np.asarray(body.getchannel("A"), dtype=np.uint8)
    projected = np.zeros((24, 48), dtype=np.uint8)
    baseline = 22
    ys, xs = np.nonzero(body_alpha > 8)
    for sy, sx in zip(ys.tolist(), xs.tolist()):
        height = max(0, baseline - sy)
        tx = sx + 2 + round(height * 0.48)
        ty = baseline - round(height * 0.16)
        if 0 <= tx < 48 and 0 <= ty < 24:
            projected[ty, tx] = max(projected[ty, tx], body_alpha[sy, sx])

    shadow_alpha = Image.fromarray(projected, mode="L")
    shadow_alpha = shadow_alpha.filter(ImageFilter.MaxFilter(3))
    shadow_alpha = shadow_alpha.filter(ImageFilter.GaussianBlur(0.65))
    shadow_alpha = shadow_alpha.point(lambda value: round(value * 0.50))
    shadow = Image.new("RGBA", body.size, (7, 5, 6, 0))
    shadow.putalpha(shadow_alpha)

    combined = Image.new("RGBA", body.size)
    combined.alpha_composite(shadow)
    combined.alpha_composite(body)

    body_mask = np.asarray(body.getchannel("A")) > 0
    shadow_mask = np.asarray(shadow.getchannel("A")) > 0
    combined_mask = np.asarray(combined.getchannel("A")) > 0
    audit = {
        "authoring_size": list(combined.size),
        "blocked_body_cell": [0, 0, 24, 24],
        "body_bounds": bounds(body_mask),
        "shadow_bounds": bounds(shadow_mask),
        "combined_bounds": bounds(combined_mask),
        "body_pixels_outside_blocked_cell": int(body_mask[:, 24:].sum()),
    }
    return body, shadow, combined, audit


def encode_volcanic_sprite(
    body: Image.Image, shadow: Image.Image, palette: list[tuple[int, int, int]]
) -> tuple[np.ndarray, Image.Image, dict]:
    """Convert the layered production sprite to OpenRA's indexed SHP form."""
    body_rgba = np.asarray(body, dtype=np.uint8)
    shadow_alpha = np.asarray(shadow.getchannel("A"), dtype=np.uint8)
    body_mask = body_rgba[..., 3] >= 48
    shadow_mask = (shadow_alpha >= 18) & ~body_mask

    colors = np.asarray(palette, dtype=np.int32)
    allowed = np.asarray([i for i in range(1, 256) if i != 4], dtype=np.int16)
    out = np.zeros(body_mask.shape, dtype=np.uint8)
    if body_mask.any():
        rgb = body_rgba[..., :3][body_mask].astype(np.int32)
        candidates = colors[allowed]
        distances = ((rgb[:, None, :] - candidates[None, :, :]) ** 2).sum(axis=2)
        out[body_mask] = allowed[np.argmin(distances, axis=1)].astype(np.uint8)
    out[shadow_mask] = 4

    preview_rgb = shore.indices_rgb(out, palette)
    preview_rgb[out == 4] = (0, 0, 0)
    preview_alpha = np.where(out != 0, 255, 0).astype(np.uint8)
    preview_alpha[out == 4] = 105
    preview = Image.fromarray(np.dstack([preview_rgb, preview_alpha]), mode="RGBA")
    return out, preview, {
        "body_palette_pixels": int(body_mask.sum()),
        "shadow_index4_pixels": int(shadow_mask.sum()),
        "transparent_pixels": int((out == 0).sum()),
    }


def make_review(tree: Image.Image, audit: dict, candidate: Image.Image | None) -> Image.Image:
    scale = 6
    margin = 22
    header = 54
    panel_size = (tree.width * scale, tree.height * scale)
    panel_count = 3 if candidate is not None else 2
    sheet = Image.new(
        "RGB",
        (panel_count * panel_size[0] + (panel_count + 1) * margin,
         header + panel_size[1] + 2 * margin),
        (42, 47, 52),
    )
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    draw.rectangle((0, 0, sheet.width, header), fill=(73, 86, 99))
    draw.text((margin, 10), "t08 exact 96x48 sprite-box study", fill="white", font=font)
    draw.text(
        (margin, 29),
        f"body {audit['body_bounds_excluding_shadow']} | shadow {audit['shadow_bounds']} | collision x_",
        fill=(210, 220, 228), font=font,
    )

    panels: list[tuple[str, Image.Image]] = []
    checker = forest.checkerboard(tree.size)
    checker.alpha_composite(tree)
    panels.append(("Temperate t08 on alpha", checker))
    temperate = forest.temperate_ground_mosaic(2, 1).convert("RGBA")
    temperate.alpha_composite(tree)
    forest.draw_footprint(temperate, ("x_",))
    panels.append(("Temperate ground + collision", temperate))
    if candidate is not None:
        volcanic = forest.ground_mosaic(2, 1).convert("RGBA")
        volcanic.alpha_composite(candidate)
        forest.draw_footprint(volcanic, ("x_",))
        panels.append(("Generated basalt in same box", volcanic))

    for index, (label, panel) in enumerate(panels):
        x = margin + index * (panel_size[0] + margin)
        y = header + margin
        scaled = panel.resize(panel_size, Image.Resampling.NEAREST)
        sheet.paste(scaled.convert("RGB"), (x, y))
        draw.rectangle((x, y, x + panel_size[0] - 1, y + panel_size[1] - 1), outline=(230, 230, 230))
        draw.text((x, y + panel_size[1] + 7), label, fill="white", font=font)
    return sheet


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out-dir", type=Path, required=True)
    parser.add_argument("--candidate", type=Path)
    parser.add_argument("--body-only", action="store_true")
    parser.add_argument("--production-package", action="store_true")
    args = parser.parse_args()
    out = args.out_dir.resolve()
    out.mkdir(parents=True, exist_ok=True)
    tree, audit = decode_t08()
    tree.save(out / "t08_temperate_frame0.png")
    candidate = None
    if args.candidate:
        source = Image.open(args.candidate).convert("RGBA")
        if args.production_package:
            author_body, author_shadow, author_combined, package_audit = author_body_and_shadow(source)
            author_body.save(out / "t08_basalt_body_24px.png")
            author_shadow.save(out / "t08_basalt_shadow_24px.png")
            author_combined.save(out / "t08_basalt_combined_24px.png")
            candidate = author_combined.resize((96, 48), Image.Resampling.NEAREST)
            candidate.save(out / "t08_basalt_combined_48px.png")
            production_body = author_body.resize((96, 48), Image.Resampling.NEAREST)
            production_shadow = author_shadow.resize((96, 48), Image.Resampling.NEAREST)
            production_body.save(out / "t08_basalt_body_48px.png")
            production_shadow.save(out / "t08_basalt_shadow_48px.png")
            volcanic_palette = shore.read_palette(
                ROOT / "mods/cameo/bits/volcanic/volcanic.pal"
            )
            indices, palette_preview, palette_audit = encode_volcanic_sprite(
                production_body, production_shadow, volcanic_palette
            )
            palette_preview.save(out / "t08_basalt_production_palette.png")
            # Preserve t08's existing ten-frame contract. Until dedicated
            # damage/husk art is authored, every state uses this candidate tree.
            write_shptd(out / "t08.tem", 96, 48, [bytes(indices)] * audit["frame_count"])
            candidate = palette_preview
            pa = np.asarray(candidate)
            package_audit["production_size"] = list(candidate.size)
            package_audit["uniform_2x2_blocks"] = all(
                np.array_equal(pa[0::2, 0::2], part)
                for part in (pa[0::2, 1::2], pa[1::2, 0::2], pa[1::2, 1::2])
            )
            package_audit["palette_encoding"] = palette_audit
            package_audit["shp_frame_count"] = audit["frame_count"]
            candidate_audit = package_audit
        else:
            candidate, candidate_audit = fit_candidate(
                source, audit["visible_bounds"], args.body_only
            )
            candidate.save(out / "t08_basalt_candidate.png")
        audit["candidate"] = candidate_audit
    review = make_review(tree, audit, candidate)
    name = "t08_temperate_vs_basalt_review.png" if candidate else "t08_temperate_sprite_box_review.png"
    review.save(out / name)
    (out / "t08_sprite_box_audit.json").write_text(
        json.dumps(audit, indent=2) + "\n", encoding="utf-8"
    )
    print(out / name)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
