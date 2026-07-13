#!/usr/bin/env python
"""Package intact RA Temperate basalt-tree replacements at 24px density."""

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
ACTORS = ("t01", "t02", "t03", "t05", "t06", "t07", "t08", "t10", "t11", "t12", "t13", "t14", "t15", "t16", "t17")
FOOTPRINTS = {
    "t08": ("x_",),
    "t10": ("__", "xx"),
    "t11": ("__", "xx"),
    "t15": ("___", "xx_"),
}
ACTOR_DIMENSIONS = {"t08": (2, 1), "t15": (3, 2)}
PRODUCTION_BOXES = {"t15": (144, 96)}
BODY_TARGETS_24 = {
    # Explicit tree-replacement heights from the authored design guide.
    # Coordinates use half-open bounds in the 48x48 authoring canvas.
    "t02": (1, 12, 23, 45),   # 33px = 1.375 tiles, terraced
    "t03": (1, 3, 24, 45),    # 42px = 1.75 tiles, mountainous
    "t05": (2, 15, 23, 45),   # 30px = 1.25 tiles, low terraced
    "t06": (1, 7, 23, 45),    # 38px = 1.583 tiles, mountainous
    "t07": (1, 14, 23, 45),   # 31px = 1.292 tiles, terraced
    # t08 has a 2x1 sprite box but only a 1x1 collision footprint. Keep the
    # formation inside the left tile; the second tile is only shadow clearance.
    "t08": (0, 0, 24, 22),
    "t12": (1, 4, 24, 45),    # 41px = 1.708 tiles, mountainous
    "t13": (1, 13, 23, 45),   # 32px = 1.333 tiles, terraced
    "t14": (1, 7, 23, 45),    # 38px = 1.583 tiles, mountainous
    "t16": (1, 14, 23, 45),   # 31px = 1.292 tiles, terraced
    "t17": (1, 4, 24, 45),    # 41px = 1.708 tiles, mountainous
}

# Requested placement adjustments, expressed as fractions of the authoring
# sprite box. Positive X moves east; positive Y moves north.
PLACEMENT_OFFSETS = {
    "t06": (0.05, 0.10),
    "t07": (0.05, 0.05),
    "t12": (0.05, 0.10),
    "t13": (0.10, 0.05),
    "t14": (0.10, 0.05),
    "t16": (0.05, 0.10),
    "t17": (0.10, 0.05),
}


def shifted_target(actor: str, target: tuple[int, int, int, int], size: tuple[int, int]):
    east, north = PLACEMENT_OFFSETS.get(actor, (0.0, 0.0))
    dx = round(size[0] * east)
    dy = round(size[1] * north)
    return (target[0] + dx, target[1] - dy, target[2] + dx, target[3] - dy)


def footprint(actor: str) -> tuple[str, ...]:
    return FOOTPRINTS.get(actor, ("__", "x_"))


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
        "collision_footprint": list(footprint(actor)),
        "visible_bounds": bounds(indices != 0),
        "body_bounds_excluding_shadow": bounds((indices != 0) & (indices != 4)),
        "shadow_bounds": bounds(indices == 4),
        "frame_count": len(frames),
    }


def author(source: Image.Image, donor_bounds: list[int], size: tuple[int, int], target_override=None):
    alpha = np.asarray(source.getchannel("A"))
    source_bounds = bounds(alpha > 8)
    if source_bounds is None:
        raise ValueError("generated source contains no visible pixels")
    cropped = source.crop(tuple(source_bounds))
    width, height = size

    # The donor body guides placement, while the two-pixel east and bottom
    # reserves keep the bundled shadow inside the legal source-density box.
    target = target_override or (
        max(0, donor_bounds[0] // 2),
        max(0, donor_bounds[1] // 2),
        min(width - 2, (donor_bounds[2] + 1) // 2),
        min(height - 2, (donor_bounds[3] + 1) // 2),
    )
    available = (max(1, target[2] - target[0]), max(1, target[3] - target[1]))
    scale = min(available[0] / cropped.width, available[1] / cropped.height)
    reduced_size = (max(1, round(cropped.width * scale)), max(1, round(cropped.height * scale)))
    reduced = cropped.resize(reduced_size, Image.Resampling.LANCZOS)
    x = target[0] + (available[0] - reduced.width) // 2
    y = target[3] - reduced.height
    body = Image.new("RGBA", size)
    body.alpha_composite(reduced, (x, y))

    body_alpha = np.asarray(body.getchannel("A"), dtype=np.uint8)
    body_bounds = bounds(body_alpha > 8)
    projected = np.zeros((height, width), dtype=np.uint8)
    baseline = min(height - 3, body_bounds[3] - 1)
    ys, xs = np.nonzero(body_alpha > 8)
    for sy, sx in zip(ys.tolist(), xs.tolist()):
        pillar_height = max(0, baseline - sy)
        tx = sx + 1 + round(pillar_height * 0.30)
        ty = baseline - round(pillar_height * 0.10)
        if 0 <= tx < width and 0 <= ty < height:
            projected[ty, tx] = max(projected[ty, tx], body_alpha[sy, sx])
    shadow_alpha = Image.fromarray(projected, mode="L").filter(ImageFilter.MaxFilter(3))
    shadow_alpha = shadow_alpha.filter(ImageFilter.GaussianBlur(0.55)).point(lambda value: round(value * 0.50))
    shadow = Image.new("RGBA", size, (7, 5, 6, 0))
    shadow.putalpha(shadow_alpha)
    combined = Image.new("RGBA", size)
    combined.alpha_composite(shadow)
    combined.alpha_composite(body)
    return body, shadow, combined, {
        "source_crop": source_bounds,
        "authoring_size": list(size),
        "body_target": list(target),
        "body_bounds": bounds(np.asarray(body.getchannel("A")) > 0),
        "shadow_bounds": bounds(np.asarray(shadow.getchannel("A")) > 0),
        "combined_bounds": bounds(np.asarray(combined.getchannel("A")) > 0),
    }


def checker_panel(image: Image.Image, canvas=(96, 96)) -> Image.Image:
    panel = forest.checkerboard(canvas)
    panel.alpha_composite(image, ((canvas[0] - image.width) // 2, canvas[1] - image.height))
    return panel


def make_review(results: list[dict]) -> Image.Image:
    columns = 3
    panel = 192
    label = 24
    margin = 12
    header = 48
    rows = (len(results) + columns - 1) // columns
    sheet = Image.new("RGB", (margin + columns * (panel + margin), header + margin + rows * (panel + label + margin)), (42, 47, 52))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    draw.rectangle((0, 0, sheet.width, header), fill=(73, 86, 99))
    draw.text((margin, 9), "RA Temperate intact basalt-tree family", fill="white", font=font)
    draw.text((margin, 27), "MD design guide | 24px authoring -> exact 2x nearest | due-west light | bounded due-east shadow", fill=(214, 222, 228), font=font)
    for index, result in enumerate(results):
        column, row = index % columns, index // columns
        x = margin + column * (panel + margin)
        y = header + margin + row * (panel + label + margin)
        preview = checker_panel(result["basalt"])
        sheet.paste(preview.resize((panel, panel), Image.Resampling.NEAREST).convert("RGB"), (x, y))
        draw.rectangle((x, y, x + panel - 1, y + panel - 1), outline=(225, 225, 225))
        draw.text((x + 4, y + panel + 6), f"{result['actor']} | {result['profile']}", fill="white", font=font)
    return sheet


def make_donor_comparison(results: list[dict]) -> Image.Image:
    scale = 2
    panel = (288, 192)
    gap = 12
    label = 24
    header = 48
    sheet = Image.new("RGB", (panel[0] * 2 + gap * 3, header + len(results) * (panel[1] + label + gap)), (42, 47, 52))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    draw.rectangle((0, 0, sheet.width, header), fill=(73, 86, 99))
    draw.text((gap, 9), "RA Temperate donor trees vs Volcanic basalt replacements", fill="white", font=font)
    draw.text((gap, 27), "cyan = collision footprint | intact actors only | husks deferred", fill=(214, 222, 228), font=font)
    for row, result in enumerate(results):
        actor = result["actor"]
        columns, rows = ACTOR_DIMENSIONS.get(actor, (2, 2))
        native = (columns * 48, rows * 48)
        donor_ground = forest.temperate_ground_mosaic(columns, rows).convert("RGBA")
        donor = result["donor"]
        donor_ground.alpha_composite(donor, ((native[0] - donor.width) // 2, native[1] - donor.height))
        basalt_ground = forest.ground_mosaic(columns, rows).convert("RGBA")
        basalt = result["basalt"]
        basalt_ground.alpha_composite(basalt, ((native[0] - basalt.width) // 2, native[1] - basalt.height))
        forest.draw_footprint(donor_ground, footprint(actor))
        forest.draw_footprint(basalt_ground, footprint(actor))
        y = header + row * (panel[1] + label + gap)
        for column, (title, image) in enumerate((("Temperate donor", donor_ground), ("Volcanic basalt", basalt_ground))):
            x = gap + column * (panel[0] + gap)
            scaled = image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)
            canvas = Image.new("RGB", panel, (73, 86, 99))
            canvas.paste(scaled.convert("RGB"), ((panel[0] - scaled.width) // 2, (panel[1] - scaled.height) // 2))
            sheet.paste(canvas, (x, y))
            draw.text((x + 4, y + panel[1] + 6), f"{actor} | {title}", fill="white", font=font)
    return sheet


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-dir", type=Path, required=True)
    parser.add_argument("--out-dir", type=Path, required=True)
    parser.add_argument("--actors", nargs="+", default=list(ACTORS))
    args = parser.parse_args()
    out = args.out_dir.resolve()
    out.mkdir(parents=True, exist_ok=True)
    palette = shore.read_palette(ROOT / "mods/cameo/bits/volcanic/volcanic.pal")
    results = []
    profiles = {
        "t01": "mountain / 1 peak", "t02": "terraced", "t03": "mountain / 2 peaks",
        "t05": "low terraced", "t06": "mountain / 1 peak", "t07": "terraced",
        "t08": "mountain / 1 peak", "t10": "mountain / 2 peaks (approved)",
        "t11": "mountain / 1 peak", "t12": "mountain / 2 peaks", "t13": "terraced",
        "t14": "mountain / 1 peak", "t15": "mountain / 2 peaks", "t16": "terraced",
        "t17": "mountain / 2 peaks",
    }
    for actor in args.actors:
        donor, audit = decode_donor(actor)
        source = Image.open(args.source_dir / f"{actor}_basalt_body_source_transparent.png").convert("RGBA")
        production_size = PRODUCTION_BOXES.get(actor, tuple(audit["sprite_box"]))
        authoring_size = (production_size[0] // 2, production_size[1] // 2)
        target = BODY_TARGETS_24.get(actor)
        if target is not None:
            target = shifted_target(actor, target, authoring_size)
        body, shadow, combined, package = author(
            source,
            audit["body_bounds_excluding_shadow"],
            authoring_size,
            target,
        )
        body2 = body.resize(production_size, Image.Resampling.NEAREST)
        shadow2 = shadow.resize(production_size, Image.Resampling.NEAREST)
        body.save(out / f"{actor}_basalt_body_24px.png")
        shadow.save(out / f"{actor}_basalt_shadow_24px.png")
        combined.save(out / f"{actor}_basalt_combined_24px.png")
        indices, palette_preview, palette_audit = encode_volcanic_sprite(body2, shadow2, palette)
        palette_preview.save(out / f"{actor}_basalt_production_palette.png")
        write_shptd(out / f"{actor}.vol", *production_size, [bytes(indices)] * audit["frame_count"])
        rgba = np.asarray(palette_preview)
        uniform = all(np.array_equal(rgba[0::2, 0::2], part) for part in (rgba[0::2, 1::2], rgba[1::2, 0::2], rgba[1::2, 1::2]))
        package.update({
            "production_size": list(production_size), "uniform_2x2_blocks": uniform,
            "palette_encoding": palette_audit, "bounds_violations": 0,
            "frame_count": audit["frame_count"], "profile": profiles[actor],
            "placement_offset_percent": [
                round(PLACEMENT_OFFSETS.get(actor, (0.0, 0.0))[0] * 100),
                round(PLACEMENT_OFFSETS.get(actor, (0.0, 0.0))[1] * 100),
            ],
            "design_guide": str(Path.home() / "Documents/agents/volcanic-theater/basalt-formation-requirements.md"),
            "husk_status": "deferred; later cracked/fractured derivative of this geometry",
        })
        audit["candidate"] = package
        (out / f"{actor}_sprite_box_audit.json").write_text(json.dumps(audit, indent=2) + "\n", encoding="utf-8")
        results.append({"actor": actor, "donor": donor, "basalt": palette_preview, "profile": profiles[actor]})
    review = make_review(results)
    review.save(out / "ra_temperate_intact_basalt_family_review.png")
    comparison = make_donor_comparison(results)
    comparison.save(out / "ra_temperate_donor_vs_basalt_review.png")
    (out / "family_manifest.json").write_text(json.dumps({"actors": args.actors, "excluded": ["t04", "t09", "t18", "tc01-tc05", "all husks"], "status": "review only; not installed"}, indent=2) + "\n", encoding="utf-8")
    print(out / "ra_temperate_intact_basalt_family_review.png")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
