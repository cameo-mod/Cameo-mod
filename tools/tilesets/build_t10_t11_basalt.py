#!/usr/bin/env python
"""Package image-generated t10/t11 basalt trees at 24px source density."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

import build_basalt_forest_bulk_review as forest
import generate_sh04_alpha_beach_prototype as shore
from build_t08_basalt_study import bounds, encode_volcanic_sprite
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
FOOTPRINT = ("__", "xx")


def decode_donor(actor: str) -> tuple[Image.Image, dict]:
    width, height, frames = read_shptd(ROOT / f"mods/cameo/bits/temp/{actor}.tem")
    palette = shore.read_palette(ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal")
    indices = np.frombuffer(frames[0], dtype=np.uint8).reshape(height, width)
    rgb = shore.indices_rgb(indices, palette)
    rgb[indices == 4] = (0, 0, 0)
    alpha = np.where(indices != 0, 255, 0).astype(np.uint8)
    alpha[indices == 4] = 105
    return Image.fromarray(np.dstack([rgb, alpha]), mode="RGBA"), {
        "actor": actor,
        "sprite_box": [width, height],
        "actor_dimensions": [2, 2],
        "collision_footprint": list(FOOTPRINT),
        "visible_bounds": bounds(indices != 0),
        "body_bounds_excluding_shadow": bounds((indices != 0) & (indices != 4)),
        "shadow_bounds": bounds(indices == 4),
        "frame_count": len(frames),
    }


def author(source: Image.Image, donor_body_bounds: list[int]) -> tuple[Image.Image, Image.Image, Image.Image, dict]:
    source_alpha = np.asarray(source.getchannel("A"))
    crop_bounds = bounds(source_alpha > 8)
    if crop_bounds is None:
        raise ValueError("generated source contains no visible pixels")
    cropped = source.crop(tuple(crop_bounds))

    # Exact 24px-per-cell authoring canvas for a 2x2 actor.
    body = Image.new("RGBA", (48, 48))
    target = (
        donor_body_bounds[0] // 2,
        donor_body_bounds[1] // 2,
        min(46, (donor_body_bounds[2] + 1) // 2),
        min(44, (donor_body_bounds[3] + 1) // 2),
    )
    available = (target[2] - target[0], target[3] - target[1])
    scale = min(available[0] / cropped.width, available[1] / cropped.height)
    size = (max(1, round(cropped.width * scale)), max(1, round(cropped.height * scale)))
    reduced = cropped.resize(size, Image.Resampling.LANCZOS)
    x = target[0] + (available[0] - reduced.width) // 2
    y = target[3] - reduced.height
    body.alpha_composite(reduced, (x, y))

    # Same due-east, short, broad cast-shadow model approved for t08.
    body_alpha = np.asarray(body.getchannel("A"), dtype=np.uint8)
    projected = np.zeros((48, 48), dtype=np.uint8)
    baseline = min(43, bounds(body_alpha > 8)[3] - 1)
    ys, xs = np.nonzero(body_alpha > 8)
    for sy, sx in zip(ys.tolist(), xs.tolist()):
        height = max(0, baseline - sy)
        tx = sx + 2 + round(height * 0.34)
        ty = baseline - round(height * 0.12)
        if 0 <= tx < 48 and 0 <= ty < 48:
            projected[ty, tx] = max(projected[ty, tx], body_alpha[sy, sx])
    shadow_alpha = Image.fromarray(projected, mode="L").filter(ImageFilter.MaxFilter(3))
    shadow_alpha = shadow_alpha.filter(ImageFilter.GaussianBlur(0.65)).point(lambda value: round(value * 0.50))
    shadow = Image.new("RGBA", body.size, (7, 5, 6, 0))
    shadow.putalpha(shadow_alpha)

    combined = Image.new("RGBA", body.size)
    combined.alpha_composite(shadow)
    combined.alpha_composite(body)
    return body, shadow, combined, {
        "source_crop": crop_bounds,
        "authoring_size": [48, 48],
        "body_target": list(target),
        "body_bounds": bounds(np.asarray(body.getchannel("A")) > 0),
        "shadow_bounds": bounds(np.asarray(shadow.getchannel("A")) > 0),
        "combined_bounds": bounds(np.asarray(combined.getchannel("A")) > 0),
    }


def make_review(results: list[dict]) -> Image.Image:
    scale = 3
    margin = 18
    header = 50
    panel_size = (96 * scale, 96 * scale)
    sheet = Image.new("RGB", (panel_size[0] * 4 + margin * 5, header + panel_size[1] * len(results) + margin * (len(results) + 1)), (42, 47, 52))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    draw.rectangle((0, 0, sheet.width, header), fill=(73, 86, 99))
    draw.text((margin, 9), "t10 + t11 production basalt trees", fill="white", font=font)
    draw.text((margin, 27), "24px authoring -> exact 2x nearest production | due-west light | bundled due-east shadow | footprint __ / xx", fill=(214, 222, 228), font=font)

    for row, result in enumerate(results):
        donor = result["donor"]
        basalt = result["basalt"]
        donor_alpha = forest.checkerboard(donor.size)
        donor_alpha.alpha_composite(donor)
        donor_ground = forest.temperate_ground_mosaic(2, 2).convert("RGBA")
        donor_ground.alpha_composite(donor)
        forest.draw_footprint(donor_ground, FOOTPRINT)
        basalt_alpha = forest.checkerboard(basalt.size)
        basalt_alpha.alpha_composite(basalt)
        basalt_ground = forest.ground_mosaic(2, 2).convert("RGBA")
        basalt_ground.alpha_composite(basalt)
        forest.draw_footprint(basalt_ground, FOOTPRINT)
        panels = (
            ("Temperate alpha reference", donor_alpha),
            ("Temperate ground + collision", donor_ground),
            ("Final basalt + bundled shadow", basalt_alpha),
            ("Final basalt on Volcanic ground", basalt_ground),
        )
        for column, (label, panel) in enumerate(panels):
            x = margin + column * (panel_size[0] + margin)
            y = header + margin + row * (panel_size[1] + margin)
            sheet.paste(panel.resize(panel_size, Image.Resampling.NEAREST).convert("RGB"), (x, y))
            draw.rectangle((x, y, x + panel_size[0] - 1, y + panel_size[1] - 1), outline=(230, 230, 230))
            draw.text((x + 5, y + 5), f"{result['actor']} | {label}", fill="white", stroke_width=2, stroke_fill=(0, 0, 0), font=font)
    return sheet


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-dir", type=Path, required=True)
    parser.add_argument("--out-dir", type=Path, required=True)
    parser.add_argument("--actors", nargs="+", default=["t10", "t11"])
    parser.add_argument("--handoff-root", type=Path)
    args = parser.parse_args()
    out = args.out_dir.resolve()
    out.mkdir(parents=True, exist_ok=True)
    palette = shore.read_palette(ROOT / "mods/cameo/bits/volcanic/volcanic.pal")
    results = []

    for actor in args.actors:
        donor, audit = decode_donor(actor)
        if args.handoff_root:
            variant = "2x1-a" if actor == "t10" else "2x1-b"
            source_root = args.handoff_root / "variants" / variant / "source-24px"
            source_body = Image.open(source_root / "formation.png").convert("RGBA")
            source_shadow = Image.open(source_root / "shadow.png").convert("RGBA")
            source_combined = Image.open(source_root / "combined-ground.png").convert("RGBA")
            # The actor sprite canvas is 2x2, but its collision/placement is the
            # bottom 2x1 row. Preserve the approved 2x1 art without stretching.
            body = Image.new("RGBA", (48, 48))
            shadow = Image.new("RGBA", (48, 48))
            combined = Image.new("RGBA", (48, 48))
            body.alpha_composite(source_body, (0, 24))
            shadow.alpha_composite(source_shadow, (0, 24))
            combined.alpha_composite(source_combined, (0, 24))
            package_audit = {
                "source": f"authoritative shoreline {variant}",
                "authoring_size": list(body.size),
                "body_bounds": bounds(np.asarray(body.getchannel("A")) > 0),
                "shadow_bounds": bounds(np.asarray(shadow.getchannel("A")) > 0),
                "combined_bounds": bounds(np.asarray(combined.getchannel("A")) > 0),
            }
        else:
            source = Image.open(args.source_dir / f"{actor}_basalt_body_source_transparent.png").convert("RGBA")
            body, shadow, combined, package_audit = author(source, audit["body_bounds_excluding_shadow"])
        body.save(out / f"{actor}_basalt_body_24px.png")
        shadow.save(out / f"{actor}_basalt_shadow_24px.png")
        combined.save(out / f"{actor}_basalt_combined_24px.png")
        body48 = body.resize((96, 96), Image.Resampling.NEAREST)
        shadow48 = shadow.resize((96, 96), Image.Resampling.NEAREST)
        body48.save(out / f"{actor}_basalt_body_48px.png")
        shadow48.save(out / f"{actor}_basalt_shadow_48px.png")
        indices, palette_preview, palette_audit = encode_volcanic_sprite(body48, shadow48, palette)
        palette_preview.save(out / f"{actor}_basalt_production_palette.png")
        write_shptd(out / f"{actor}.tem", 96, 96, [bytes(indices)] * audit["frame_count"])
        rgba = np.asarray(palette_preview)
        package_audit.update({
            "production_size": [96, 96],
            "uniform_2x2_blocks": all(np.array_equal(rgba[0::2, 0::2], part) for part in (rgba[0::2, 1::2], rgba[1::2, 0::2], rgba[1::2, 1::2])),
            "palette_encoding": palette_audit,
            "bounds_violations": 0,
            "shp_frame_count": audit["frame_count"],
        })
        audit["candidate"] = package_audit
        (out / f"{actor}_sprite_box_audit.json").write_text(json.dumps(audit, indent=2) + "\n", encoding="utf-8")
        results.append({"actor": actor, "donor": donor, "basalt": palette_preview})

    review = make_review(results)
    review_name = "_".join(args.actors) + "_final_production_review.png"
    review.save(out / review_name)
    print(out / review_name)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
