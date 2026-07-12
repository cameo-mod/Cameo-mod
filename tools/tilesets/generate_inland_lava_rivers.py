#!/usr/bin/env python
"""Generate preview-only volcanic conversions for Temperate inland rivers."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

import generate_lava_river_donor_layer as lava_donor
import generate_sh04_alpha_beach_prototype as shore
import recolor_cliff_luminance as cliff
from manual_river_delta.prepare_production import quantize
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUT_DIR = Path.home() / "Documents/agents/volcanic-theater/inland-rivers/workbench"
RIVERS = tuple(f"rv{number:02d}" for number in range(1, 16))
EXTENDED_WATER_INDICES = np.asarray(
    [46, 47, 62, 63, 64, 65, 66, 67, 68, 72, 96, 97, 98, 99, 100, 101, 102, 166, 178],
    dtype=np.uint8,
)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--templates", nargs="+", default=RIVERS)
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    parser.add_argument("--page-size", type=int, default=5)
    parser.add_argument(
        "--water-mask-dir",
        type=Path,
        help="optional RGBA masks named <template>.png; nonzero alpha is water",
    )
    parser.add_argument(
        "--canonical-liquid-w1",
        type=Path,
        required=True,
        help="approved one-frame 48px proper-liquid VOL used phase-aligned everywhere",
    )
    parser.add_argument(
        "--omit-generated-shadows",
        action="store_true",
        help="skip the Gaussian/local-darkness cast-shadow reconstruction pass",
    )
    args = parser.parse_args()
    out_dir = args.out_dir.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    candidate_dir = out_dir / "candidate-vols"
    candidate_dir.mkdir(parents=True, exist_ok=True)

    temperate_palette = shore.read_palette(
        ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    )
    volcanic_palette = shore.read_palette(
        ROOT / "mods/cameo/bits/volcanic/volcanic.pal"
    )
    clear = shore.unique_frame(
        ROOT / "mods/cameo/bits/volcanic/clear1.vol", expected_frames=16
    )
    clear_rgb = shore.indices_rgb(clear, volcanic_palette)
    canonical_liquid = shore.unique_frame(
        args.canonical_liquid_w1.resolve(), expected_frames=1
    )
    snow_palette = shore.read_palette(
        ROOT / "mods/cameo/bits/rasnow/ra_snow.pal"
    )
    canonical_liquid_rgb = shore.indices_rgb(canonical_liquid, volcanic_palette)
    water_indices = shore.source_indices(
        ROOT / "mods/cameo/bits/temp/w1.tem"
    ) | shore.source_indices(ROOT / "mods/cameo/bits/temp/w2.tem")

    panels: list[tuple[str, Image.Image]] = []
    audit: dict[str, object] = {
        "preview_only": True,
        "vol_files_written": False,
        "templates": [],
    }
    for template in args.templates:
        donor, domain, spec = read_donor(template)
        donor_rgb = shore.indices_rgb(donor, temperate_palette)
        donor_rgb[~domain] = shore.BACKGROUND
        snow, snow_domain, _ = read_donor(template, theater="snow")
        if not np.array_equal(domain, snow_domain):
            raise ValueError(f"{template}: Temperate and Snow sparse domains differ")
        snow_rgb = shore.indices_rgb(snow, snow_palette)
        donor_rgba = np.dstack(
            [donor_rgb, np.where(domain, 255, 0).astype(np.uint8)]
        )
        snow_rgba = np.dstack(
            [snow_rgb, np.where(domain, 255, 0).astype(np.uint8)]
        )
        rock_mask_image = classify_river_cliff(donor_rgba, snow_rgba, domain)
        rock_mask = np.asarray(rock_mask_image, dtype=np.uint8) >= 128
        rock_mask_image = Image.fromarray(
            np.where(rock_mask, 255, 0).astype(np.uint8), mode="L"
        )
        recolor_donor_rgba = donor_rgba
        if template in {"f02", "f03"}:
            recolor_donor_rgba = repair_horizontal_connector_edge(
                recolor_donor_rgba, domain, edge="top"
            )
        if template in {"f01", "f02"}:
            recolor_donor_rgba = repair_horizontal_connector_edge(
                recolor_donor_rgba, domain, edge="bottom"
            )
        if template in {"f05", "f06"}:
            recolor_donor_rgba = repair_vertical_connector_edge(
                recolor_donor_rgba, domain, edge="left"
            )
        if template in {"f04", "f05"}:
            recolor_donor_rgba = repair_vertical_connector_edge(
                recolor_donor_rgba, domain, edge="right"
            )
        cliff_image, _ = cliff.recolor(
            Image.fromarray(recolor_donor_rgba, mode="RGBA"),
            rock_mask_image,
            volcanic_palette,
            clear.tobytes(),
            "family",
            preserve_ground_shadows=not args.omit_generated_shadows,
        )
        cliff_rgb = np.asarray(cliff_image.convert("RGB"), dtype=np.uint8).copy()
        clear_canvas = repeat_rgb_tile(clear_rgb, spec.columns, spec.rows)
        cliff_rgb[domain & ~rock_mask] = clear_canvas[domain & ~rock_mask]
        if args.water_mask_dir is not None:
            mask_path = args.water_mask_dir.resolve() / f"{template}.png"
            rgba = np.asarray(Image.open(mask_path).convert("RGBA"))
            if rgba.shape[:2] != domain.shape:
                raise ValueError(f"{template}: water mask geometry differs")
            raw_water = domain & (rgba[:, :, 3] > 0)
            water = lava_donor.clean_water_mask(raw_water, domain)
        else:
            raw_water = domain & np.isin(donor, list(water_indices))
            # Automatic masks obey the authoritative Temperate water-index set
            # exactly. Do not grow into chromatically similar ground pixels.
            water = raw_water.copy()
        water = remove_small_components(water, minimum_pixels=48)
        if template.startswith("f"):
            water = remove_ford_liquid_edge_spots(water, domain)
        liquid_indices = repeat_tile(
            canonical_liquid, spec.columns, spec.rows
        )
        liquid = shore.indices_rgb(liquid_indices, volcanic_palette)
        plain_candidate = volcanic_candidate(
            donor_rgb, domain, water, liquid, clear_rgb
        )
        candidate = cliff_aware_candidate(
            cliff_rgb,
            donor_rgb,
            domain,
            water,
            rock_mask,
            liquid,
            restore_generated_shadows=not args.omit_generated_shadows,
        )
        indexed_image, candidate_indices = quantize(
            Image.fromarray(candidate, mode="RGB"), volcanic_palette
        )
        candidate_indices[water] = liquid_indices[water]
        candidate_indices[~domain] = 0
        indexed_rgb = shore.indices_rgb(candidate_indices, volcanic_palette)
        indexed_rgb[~domain] = shore.BACKGROUND
        candidate_image = Image.fromarray(indexed_rgb, mode="RGB")
        candidate_path = candidate_dir / f"{template}.vol"
        write_template_vol(candidate_path, candidate_indices, spec)
        transparent = np.zeros((*water.shape, 4), dtype=np.uint8)
        transparent[:, :, :3] = liquid
        transparent[:, :, 3] = np.where(water, 255, 0).astype(np.uint8)

        donor_image = Image.fromarray(donor_rgb, mode="RGB")
        lava_only = shore.checker_composite(transparent).convert("RGB")
        plain_image = Image.fromarray(plain_candidate, mode="RGB")
        donor_image.save(out_dir / f"temperate_donor_{template}.png")
        Image.fromarray(transparent, mode="RGBA").save(
            out_dir / f"lava_only_transparent_{template}.png"
        )
        candidate_image.save(out_dir / f"volcanic_inland_river_candidate_{template}.png")
        panels.extend(
            (
                (f"{template}: Temperate donor", donor_image),
                (f"{template}: plain-bank liquid", plain_image),
                (f"{template}: cliff-aware liquid", candidate_image),
            )
        )
        audit["templates"].append(
            {
                "template": template,
                "size": [spec.columns, spec.rows],
                "water_pixels": int(np.count_nonzero(water)),
                "rock_pixels": int(np.count_nonzero(rock_mask & domain & ~water)),
                "canonical_liquid_exact_pixels": int(
                    np.count_nonzero(candidate_indices[water] == liquid_indices[water])
                ),
                "candidate_vol": str(candidate_path.resolve()),
                "candidate_roundtrip_exact": True,
                "edge_contacts": edge_contacts(water),
            }
        )

    for start in range(0, len(args.templates), args.page_size):
        names = args.templates[start : start + args.page_size]
        page = panels[start * 3 : (start + len(names)) * 3]
        path = out_dir / f"inland_lava_river_review_{'_'.join(names)}.png"
        shore.write_review_sheet(path, page, columns=3, scale=2)
        print(path.resolve())
    audit_path = out_dir / "inland_lava_river_preview_audit.json"
    audit_path.write_text(json.dumps(audit, indent=2), encoding="utf-8")
    print(audit_path.resolve())
    return 0


def repeat_tile(tile: np.ndarray, columns: int, rows: int) -> np.ndarray:
    return np.tile(tile, (rows, columns))


def repeat_rgb_tile(tile: np.ndarray, columns: int, rows: int) -> np.ndarray:
    return np.tile(tile, (rows, columns, 1))


def write_template_vol(
    path: Path,
    indices: np.ndarray,
    spec: shore.TemplateSpec,
) -> None:
    frames = []
    blank = bytes(shore.TILE * shore.TILE)
    for index in range(spec.columns * spec.rows):
        if index not in spec.terrain:
            frames.append(blank)
            continue
        row, column = divmod(index, spec.columns)
        frames.append(
            indices[
                row * shore.TILE : (row + 1) * shore.TILE,
                column * shore.TILE : (column + 1) * shore.TILE,
            ].tobytes()
        )
    write_shptd(path, shore.TILE, shore.TILE, frames)
    width, height, decoded = read_shptd(path)
    if (width, height) != (shore.TILE, shore.TILE) or decoded != frames:
        raise ValueError(f"{path}: candidate VOL roundtrip mismatch")


def read_donor(
    template: str,
    theater: str = "temperate",
) -> tuple[np.ndarray, np.ndarray, shore.TemplateSpec]:
    if theater == "temperate":
        yaml_path = ROOT / "mods/cameo/tilesets/ra_temperat.yaml"
        extension = ".tem"
        bits = ROOT / "mods/cameo/bits/temp"
    elif theater == "snow":
        yaml_path = ROOT / "mods/cameo/tilesets/ra_snow.yaml"
        extension = ".sno"
        bits = ROOT / "mods/cameo/bits/snow"
    else:
        raise ValueError(f"unsupported donor theater: {theater}")
    spec = shore.read_template_spec(
        yaml_path, f"{template}{extension}"
    )
    donor, domain = shore.read_sparse_composite(
        bits / spec.image, spec
    )
    return donor, domain, spec


def cliff_aware_candidate(
    cliff_rgb: np.ndarray,
    donor_rgb: np.ndarray,
    domain: np.ndarray,
    water: np.ndarray,
    rock: np.ndarray,
    liquid: np.ndarray,
    restore_generated_shadows: bool = True,
) -> np.ndarray:
    result = cliff_rgb.astype(np.float32).copy()
    land = domain & ~water
    distance = ndimage.distance_transform_edt(~water)

    # Prevent the binary mask boundary from becoming a near-black ink outline.
    exterior = rock & ndimage.binary_dilation(~rock, iterations=1)
    luma = (
        result[:, :, 0] * 0.2126
        + result[:, :, 1] * 0.7152
        + result[:, :, 2] * 0.0722
    )
    too_dark = exterior & (luma < 43.0)
    edge_basalt = np.asarray((57.0, 49.0, 45.0), dtype=np.float32)
    result[too_dark] = (
        result[too_dark] * 0.35 + edge_basalt * 0.65
    )

    # A restrained mid-dark rubble/contact band replaces the former black halo.
    ground_contact = (
        land
        & ~rock
        & (ndimage.distance_transform_edt(~rock) <= 1.5)
    )
    contact_basalt = np.asarray((48.0, 42.0, 40.0), dtype=np.float32)
    result[ground_contact] = (
        result[ground_contact] * 0.72 + contact_basalt * 0.28
    )

    # Restore real donor cast shadows without restoring vegetation texture.
    if restore_generated_shadows:
        donor = donor_rgb.astype(np.float32)
        donor_luma = (
            donor[:, :, 0] * 0.2126
            + donor[:, :, 1] * 0.7152
            + donor[:, :, 2] * 0.0722
        )
        broad_luma = ndimage.gaussian_filter(donor_luma, sigma=3.0)
        local_darkness = np.clip(
            (broad_luma - donor_luma - 2.0) / 24.0, 0.0, 1.0
        )
        rock_distance = ndimage.distance_transform_edt(~rock)
        absolute_darkness = np.clip((43.0 - donor_luma) / 23.0, 0.0, 1.0)
        directional_reach = east_southeast_projection(rock, maximum=10)
        shadow_region = (
            land
            & ~rock
            & directional_reach
            & (rock_distance > 0.0)
            & (rock_distance <= 10.0)
            & ((local_darkness >= 0.12) | (absolute_darkness >= 0.12))
        )
        labels, count = ndimage.label(shadow_region)
        if count:
            sizes = np.bincount(labels.ravel())
            keep = sizes >= 7
            keep[0] = False
            shadow_region = keep[labels]
        shadow_strength = np.maximum(local_darkness, absolute_darkness * 0.82)
        shadow_strength *= shadow_region
        shadow_basalt = np.asarray((20.0, 18.0, 20.0), dtype=np.float32)
        weight = (0.72 * shadow_strength)[:, :, None]
        result = result * (1.0 - weight) + shadow_basalt * weight

    # Heat the ordinary bank without erasing donor cliff geometry.
    bank = land & ~rock
    bank_heat = np.clip((7.0 - distance) / 6.0, 0.0, 1.0) * bank
    bank_target = np.asarray((91.0, 43.0, 27.0), dtype=np.float32)
    result = result * (1.0 - 0.20 * bank_heat[:, :, None]) + bank_target * (
        0.20 * bank_heat[:, :, None]
    )

    # Restrained lava bounce on preserved cliff faces closest to the channel.
    cliff_heat = np.clip((5.0 - distance) / 4.0, 0.0, 1.0) * land * rock
    bounce = np.asarray((134.0, 59.0, 27.0), dtype=np.float32)
    result = result * (1.0 - 0.16 * cliff_heat[:, :, None]) + bounce * (
        0.16 * cliff_heat[:, :, None]
    )
    result[water] = liquid[water]
    result = np.clip(np.rint(result), 0, 255).astype(np.uint8)
    result[~domain] = shore.BACKGROUND
    return result


def east_southeast_projection(rock: np.ndarray, maximum: int) -> np.ndarray:
    """Return ground positions plausibly shadowed by due-west illumination."""
    height, width = rock.shape
    projected = np.zeros_like(rock)
    for dx in range(1, maximum + 1):
        # A short vertical spread preserves donor shadows beneath irregular faces.
        spread = max(1, int(round(dx * 0.45)))
        for dy in range(-1, spread + 1):
            source_y0 = max(0, -dy)
            source_y1 = min(height, height - dy)
            target_y0 = max(0, dy)
            target_y1 = min(height, height + dy)
            if source_y0 >= source_y1 or dx >= width:
                continue
            projected[target_y0:target_y1, dx:] |= rock[
                source_y0:source_y1, : width - dx
            ]
    return projected


def classify_river_cliff(
    temperate_rgba: np.ndarray,
    snow_rgba: np.ndarray,
    domain: np.ndarray,
) -> Image.Image:
    temp = temperate_rgba[:, :, :3].astype(np.float32)
    snow = snow_rgba[:, :, :3].astype(np.float32)
    tr, tg, tb = temp[:, :, 0], temp[:, :, 1], temp[:, :, 2]
    sr, sg, sb = snow[:, :, 0], snow[:, :, 1], snow[:, :, 2]
    tl = 0.2126 * tr + 0.7152 * tg + 0.0722 * tb
    sl = 0.2126 * sr + 0.7152 * sg + 0.0722 * sb

    vegetation = (tg >= tr + 5.0) & (tg >= tb + 3.0)
    warm_face = (tr >= tg - 3.0) & (tr >= tb + 5.0) & (tl >= 43.0)
    snow_face = (
        (sr >= sg - 5.0)
        & (sr >= sb + 2.0)
        & (sl >= 55.0)
        & (sl < 145.0)
        & (tl >= 48.0)
        & (np.abs(tr - tg) <= 18.0)
    )
    seed = domain & ~vegetation & (warm_face | snow_face)

    # Retain darker face/shadow pixels only when they touch a credible rock face.
    near_seed = ndimage.binary_dilation(seed, iterations=1)
    shadow_face = domain & ~vegetation & (tl >= 26.0) & (sl < 125.0)
    mask = seed | (near_seed & shadow_face)
    # SciPy otherwise treats pixels beyond the image as False during erosion,
    # which falsely strips rock classification from every outer edge.
    padded = np.pad(mask, 1, mode="edge")
    padded = ndimage.binary_closing(
        padded,
        structure=np.ones((3, 3), dtype=bool),
    )
    mask = padded[1:-1, 1:-1] & domain

    labels, count = ndimage.label(mask)
    if count:
        sizes = np.bincount(labels.ravel())
        keep = sizes >= 32
        keep[0] = False
        mask = keep[labels]
    return Image.fromarray(np.where(mask, 255, 0).astype(np.uint8), mode="L")


def volcanic_candidate(
    donor_rgb: np.ndarray,
    domain: np.ndarray,
    water: np.ndarray,
    liquid: np.ndarray,
    clear_rgb: np.ndarray,
) -> np.ndarray:
    height, width = domain.shape
    base = np.tile(
        clear_rgb,
        (
            (height + clear_rgb.shape[0] - 1) // clear_rgb.shape[0],
            (width + clear_rgb.shape[1] - 1) // clear_rgb.shape[1],
            1,
        ),
    )[:height, :width].copy()
    donor = donor_rgb.astype(np.float32)
    luma = (
        donor[:, :, 0] * 0.2126
        + donor[:, :, 1] * 0.7152
        + donor[:, :, 2] * 0.0722
    )
    land = domain & ~water
    low, high = np.percentile(luma[land], (4.0, 97.0))
    normalized = np.clip((luma - low) / max(1.0, high - low), 0.0, 1.0)
    detail = luma - ndimage.gaussian_filter(luma, sigma=1.5)
    normalized = np.clip(normalized + detail / 95.0, 0.0, 1.0)
    shadow = np.asarray((20.0, 19.0, 20.0), dtype=np.float32)
    highlight = np.asarray((82.0, 74.0, 65.0), dtype=np.float32)
    bank_rgb = shadow + normalized[:, :, None] * (highlight - shadow)

    distance = ndimage.distance_transform_edt(~water)
    bank_weight = np.clip((11.0 - distance) / 8.0, 0.0, 1.0)
    bank_weight *= land
    bank_weight = ndimage.gaussian_filter(bank_weight, sigma=0.55)
    result = (
        base.astype(np.float32) * (1.0 - bank_weight[:, :, None])
        + bank_rgb * bank_weight[:, :, None]
    )
    result[water] = liquid[water]
    result = np.clip(np.rint(result), 0, 255).astype(np.uint8)
    result[~domain] = shore.BACKGROUND
    return result


def remove_small_components(mask: np.ndarray, minimum_pixels: int) -> np.ndarray:
    labels, count = ndimage.label(mask)
    if count == 0:
        return mask
    sizes = np.bincount(labels.ravel())
    keep = sizes >= minimum_pixels
    keep[0] = False
    return keep[labels]


def remove_ford_liquid_edge_spots(
    water: np.ndarray,
    domain: np.ndarray,
) -> np.ndarray:
    """Fill small dark remnants trapped in Ford side-entry liquid contours."""
    closed = ndimage.binary_closing(
        water,
        structure=np.ones((5, 5), dtype=bool),
    )
    edge_band = np.zeros_like(water)
    width = min(12, water.shape[1] // 4, water.shape[0] // 4)
    edge_band[:, :width] = True
    edge_band[:, -width:] = True
    edge_band[:width, :] = True
    edge_band[-width:, :] = True
    repaired = water | (closed & edge_band & domain)
    return repaired


def repair_horizontal_connector_edge(
    rgba: np.ndarray,
    domain: np.ndarray,
    edge: str,
) -> np.ndarray:
    """Replace artificial dark connector rows from nearby donor interior."""
    repaired = rgba.copy()
    if edge == "top":
        targets = ((0, 3), (1, 3))
    elif edge == "bottom":
        last = rgba.shape[0] - 1
        targets = ((last - 1, last - 3), (last, last - 3))
    else:
        raise ValueError(f"unsupported connector edge: {edge}")
    for target_y, source_y in targets:
        valid = domain[target_y] & domain[source_y]
        repaired[target_y, valid, :3] = rgba[source_y, valid, :3]
    return repaired


def repair_vertical_connector_edge(
    rgba: np.ndarray,
    domain: np.ndarray,
    edge: str,
) -> np.ndarray:
    """Replace artificial dark west-east connector columns from the interior."""
    repaired = rgba.copy()
    if edge == "left":
        targets = ((0, 3), (1, 3))
    elif edge == "right":
        last = rgba.shape[1] - 1
        targets = ((last - 1, last - 3), (last, last - 3))
    else:
        raise ValueError(f"unsupported connector edge: {edge}")
    for target_x, source_x in targets:
        valid = domain[:, target_x] & domain[:, source_x]
        repaired[valid, target_x, :3] = rgba[valid, source_x, :3]
    return repaired


def extended_ford_water(
    donor: np.ndarray,
    donor_rgb: np.ndarray,
    domain: np.ndarray,
) -> np.ndarray:
    rgb = donor_rgb.astype(np.int16)
    blue = (
        (rgb[:, :, 2] >= rgb[:, :, 0] + 6)
        & (rgb[:, :, 2] >= rgb[:, :, 1] - 4)
    )
    return domain & (np.isin(donor, EXTENDED_WATER_INDICES) | blue)


def edge_contacts(mask: np.ndarray) -> dict[str, int]:
    return {
        "top": int(np.count_nonzero(mask[0, :])),
        "right": int(np.count_nonzero(mask[:, -1])),
        "bottom": int(np.count_nonzero(mask[-1, :])),
        "left": int(np.count_nonzero(mask[:, 0])),
    }


if __name__ == "__main__":
    raise SystemExit(main())
