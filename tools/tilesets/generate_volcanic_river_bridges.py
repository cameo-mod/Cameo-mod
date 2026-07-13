#!/usr/bin/env python
"""Generate preview-only Volcanic river bridges from RA Temperate donors."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

import generate_sh04_alpha_beach_prototype as shore
from manual_river_delta.prepare_production import quantize
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
BRIDGES = (
    "br1a", "br1b", "br1c", "br1x",
    "br2a", "br2b", "br2c", "br2x",
    "br3a", "br3b", "br3c", "br3d", "br3e", "br3f",
    "bridge1", "bridge1d", "bridge1h", "bridge1x",
    "bridge2", "bridge2d", "bridge2h", "bridge2x",
    "sbridge1", "sbridge1d", "sbridge1h", "sbridge1x",
    "sbridge2", "sbridge2d", "sbridge2h", "sbridge2x",
    "sbridge3", "sbridge3d", "sbridge3h", "sbridge3x",
    "sbridge4", "sbridge4d", "sbridge4h", "sbridge4x",
    "sbridge5", "sbridge5d", "sbridge5h", "sbridge5x",
    "fjord1", "fjord2",
)
WATER_INDICES = np.asarray(
    [46, 47, 62, 63, 64, 65, 66, 67, 68, 72, 96, 97, 98, 99, 100, 101, 102, 166, 178],
    dtype=np.uint8,
)
GROUND_INDICES = np.asarray(
    [18, 19, 21, 23, 24, 25, 26, 27, 28, 29, 30, 31, 36, 140, 141],
    dtype=np.uint8,
)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--templates", nargs="+", default=BRIDGES)
    parser.add_argument("--liquid-lava-w1", type=Path, required=True)
    parser.add_argument("--lava-label", default="proper liquid")
    parser.add_argument("--ford-mode", action="store_true")
    parser.add_argument("--cracked-lava-w1", type=Path)
    parser.add_argument("--crack-thickness-multiplier", type=float, default=1.0)
    parser.add_argument("--ford-leak-pixels", type=int, default=0)
    parser.add_argument("--out-dir", type=Path, required=True)
    parser.add_argument("--page-size", type=int, default=5)
    args = parser.parse_args()
    out = args.out_dir.resolve()
    candidates = out / "candidate-vols"
    candidates.mkdir(parents=True, exist_ok=True)

    temperate_palette = shore.read_palette(ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal")
    volcanic_palette = shore.read_palette(ROOT / "mods/cameo/bits/volcanic/volcanic.pal")
    liquid = shore.unique_frame(args.liquid_lava_w1.resolve(), expected_frames=1)
    cracked = (
        shore.unique_frame(args.cracked_lava_w1.resolve(), expected_frames=1)
        if args.cracked_lava_w1 else None
    )
    if args.ford_mode and cracked is None:
        raise ValueError("--ford-mode requires --cracked-lava-w1")
    if cracked is not None and args.crack_thickness_multiplier > 1.0:
        cracked = thicken_cracked_lava(
            cracked,
            volcanic_palette,
            args.crack_thickness_multiplier,
        )
    clear = shore.unique_frame(ROOT / "mods/cameo/bits/volcanic/clear1.vol", expected_frames=16)
    clear_rgb = shore.indices_rgb(clear, volcanic_palette)

    panels: list[tuple[str, Image.Image]] = []
    records = []
    for name in args.templates:
        spec = read_bridge_donor_spec(name)
        donor, domain = shore.read_sparse_composite(
            ROOT / "mods/cameo/bits/temp" / spec.image, spec
        )
        donor_rgb = shore.indices_rgb(donor, temperate_palette)
        donor_rgb[~domain] = shore.BACKGROUND
        ford = ford_cell_mask(spec, domain) if args.ford_mode else np.zeros_like(domain)
        if args.ford_mode:
            water = classify_ford_water(donor, donor_rgb, domain)
            ground = classify_green_ground(donor, domain, spec, water)
        else:
            water = clean_water_regions(domain & np.isin(donor, WATER_INDICES))
            ground = domain & ~water
        cracked_mask = domain & ~water & ~ground if args.ford_mode else np.zeros_like(domain)
        liquid_indices = np.tile(liquid, (spec.rows, spec.columns))
        liquid_rgb = shore.indices_rgb(liquid_indices, volcanic_palette)
        cracked_indices = np.tile(cracked, (spec.rows, spec.columns)) if cracked is not None else None
        cracked_rgb = (
            shore.indices_rgb(cracked_indices, volcanic_palette)
            if cracked_indices is not None else None
        )
        clear_canvas = np.tile(clear_rgb, (spec.rows, spec.columns, 1))

        candidate = recolor_bridge(donor_rgb, clear_canvas, domain, water)
        if args.ford_mode:
            candidate[ground] = clear_canvas[ground]
        candidate[water] = liquid_rgb[water]
        if cracked_rgb is not None:
            candidate[cracked_mask] = cracked_rgb[cracked_mask]
        image, indices = quantize(Image.fromarray(candidate, mode="RGB"), volcanic_palette)
        indices[water] = liquid_indices[water]
        if cracked_indices is not None:
            indices[cracked_mask] = cracked_indices[cracked_mask]
        indices[~domain] = 0
        rgb = shore.indices_rgb(indices, volcanic_palette)
        rgb[~domain] = shore.BACKGROUND
        candidate_image = Image.fromarray(rgb, mode="RGB")
        write_template(candidates / f"{name}.vol", indices, spec)

        donor_image = Image.fromarray(donor_rgb, mode="RGB")
        donor_image.save(out / f"temperate_donor_{name}.png")
        candidate_image.save(out / f"volcanic_candidate_{name}.png")
        water_preview = np.zeros((*water.shape, 4), dtype=np.uint8)
        water_preview[:, :, :3] = liquid_rgb
        if cracked_rgb is not None:
            water_preview[:, :, :3][cracked_mask] = cracked_rgb[cracked_mask]
        water_preview[:, :, 3] = np.where(water | cracked_mask, 255, 0).astype(np.uint8)
        panels.extend((
            (f"{name}: Temperate donor", donor_image),
            (f"{name}: donor-water {args.lava_label}", shore.checker_composite(water_preview).convert("RGB")),
            (f"{name}: Volcanic bridge candidate", candidate_image),
        ))
        records.append({
            "template": name,
            "size": [spec.columns, spec.rows],
            "water_pixels": int(np.count_nonzero(water)),
            "lava_material": args.lava_label,
            "lava_exact_pixels": int(np.count_nonzero(indices[water] == liquid_indices[water])),
            "cracked_ford_pixels": int(np.count_nonzero(cracked_mask)),
            "ford_leak_pixels": args.ford_leak_pixels,
            "cracked_ford_exact_pixels": (
                int(np.count_nonzero(indices[cracked_mask] == cracked_indices[cracked_mask]))
                if cracked_indices is not None else 0
            ),
            "roundtrip_exact": True,
        })

    pages = []
    for start in range(0, len(args.templates), args.page_size):
        names = args.templates[start:start + args.page_size]
        page = panels[start * 3:(start + len(names)) * 3]
        path = out / f"volcanic_river_bridge_review_{'_'.join(names)}.png"
        shore.write_review_sheet(path, page, columns=3, scale=2)
        pages.append(str(path.resolve()))
        print(path.resolve())
    audit = {"preview_only": True, "production_modified": False, "templates": records, "pages": pages}
    audit_path = out / "volcanic_river_bridge_audit.json"
    audit_path.write_text(json.dumps(audit, indent=2) + "\n", encoding="utf-8")
    print(audit_path.resolve())
    return 0


def read_bridge_donor_spec(name: str) -> shore.TemplateSpec:
    """Read RA Temperate geometry, using Volcanic metadata only when needed."""
    try:
        return shore.read_template_spec(
            ROOT / "mods/cameo/tilesets/ra_temperat.yaml", f"{name}.tem"
        )
    except ValueError:
        # Some shared .tem bridge art is not declared by RA Temperate itself.
        # The active Volcanic template supplies only layout and terrain; all
        # visible donor pixels still come from RA Temperate .tem artwork.
        spec = shore.read_template_spec(
            ROOT / "mods/cameo/tilesets/volcanic.yaml", f"{name}.vol"
        )
        return shore.TemplateSpec(
            image=f"{name}.tem",
            columns=spec.columns,
            rows=spec.rows,
            terrain=spec.terrain,
        )


def recolor_bridge(
    donor_rgb: np.ndarray,
    clear_rgb: np.ndarray,
    domain: np.ndarray,
    water: np.ndarray,
) -> np.ndarray:
    source = donor_rgb.astype(np.float32)
    luma = 0.2126 * source[:, :, 0] + 0.7152 * source[:, :, 1] + 0.0722 * source[:, :, 2]
    broad = ndimage.gaussian_filter(luma, sigma=1.2)
    detail = np.clip(luma - broad, -18.0, 22.0)
    land = domain & ~water
    if not np.any(land):
        result = clear_rgb.copy()
        result[~domain] = shore.BACKGROUND
        return result
    low, high = np.percentile(luma[land], (3.0, 98.0))
    form = np.clip((luma - low) / max(1.0, high - low), 0.0, 1.0)
    target = 20.0 + 92.0 * form + detail * 0.38

    # Bridge decks, abutments, and surrounding ground all use the theater's
    # neutral basalt-gray language. Preserve donor luminance geometry without
    # carrying Temperate warmth into the Volcanic material.
    structure = np.stack(
        (target * 0.96, target * 0.91, target * 0.87),
        axis=2,
    )

    result = clear_rgb.astype(np.float32).copy()
    result[land] = structure[land]
    result = np.clip(np.rint(result), 0, 255).astype(np.uint8)
    result[~domain] = shore.BACKGROUND
    return result


def clean_water_regions(mask: np.ndarray) -> np.ndarray:
    labels, count = ndimage.label(mask)
    if not count:
        return mask
    sizes = np.bincount(labels.ravel())
    keep = sizes >= 48
    keep[0] = False
    return keep[labels]


def classify_ford_water(
    donor: np.ndarray,
    donor_rgb: np.ndarray,
    domain: np.ndarray,
) -> np.ndarray:
    rgb = donor_rgb.astype(np.int16)
    blue_water = (
        (rgb[:, :, 2] >= rgb[:, :, 0] + 6)
        & (rgb[:, :, 2] >= rgb[:, :, 1] - 4)
    )
    raw = domain & (np.isin(donor, WATER_INDICES) | blue_water)
    labels, count = ndimage.label(raw)
    if not count:
        return raw
    sizes = np.bincount(labels.ravel())
    keep = sizes >= 12
    keep[0] = False
    return keep[labels]


def classify_green_ground(
    donor: np.ndarray,
    domain: np.ndarray,
    spec: shore.TemplateSpec,
    water: np.ndarray,
) -> np.ndarray:
    approach = np.zeros_like(domain)
    for index, terrain in spec.terrain.items():
        if terrain == "Ford":
            continue
        row, column = divmod(index, spec.columns)
        approach[
            row * shore.TILE:(row + 1) * shore.TILE,
            column * shore.TILE:(column + 1) * shore.TILE,
        ] = True
    approach &= domain & ~water
    if not np.any(approach):
        return np.zeros_like(domain)
    approach_indices = np.unique(donor[approach])
    candidate = domain & ~water & np.isin(donor, approach_indices)
    ground = ndimage.binary_propagation(approach, mask=candidate)
    ground = ndimage.binary_closing(ground, structure=np.ones((3, 3), dtype=bool))
    return ground & domain & ~water


def ford_cell_mask(spec: shore.TemplateSpec, domain: np.ndarray) -> np.ndarray:
    mask = np.zeros_like(domain)
    for index, terrain in spec.terrain.items():
        if terrain != "Ford":
            continue
        row, column = divmod(index, spec.columns)
        mask[
            row * shore.TILE:(row + 1) * shore.TILE,
            column * shore.TILE:(column + 1) * shore.TILE,
        ] = True
    return mask & domain


def thicken_cracked_lava(
    indices: np.ndarray,
    palette: list[tuple[int, int, int]],
    multiplier: float,
) -> np.ndarray:
    rgb = np.asarray(palette, dtype=np.int16)[indices]
    seam = (
        (rgb[:, :, 0] >= 105)
        & (rgb[:, :, 0] >= rgb[:, :, 1] + 28)
        & (rgb[:, :, 0] >= rgb[:, :, 2] + 45)
    )
    radius = max(1, int(round((multiplier - 1.0) * 1.15)))
    tiled_seam = np.tile(seam, (3, 3))
    tiled_indices = np.tile(indices, (3, 3))
    distance, nearest = ndimage.distance_transform_edt(
        ~tiled_seam,
        return_indices=True,
    )
    expanded = tiled_indices.copy()
    grow = (~tiled_seam) & (distance <= radius)
    expanded[grow] = tiled_indices[nearest[0][grow], nearest[1][grow]]
    height, width = indices.shape
    return expanded[height:2 * height, width:2 * width]


def cool_ford_crust(
    indices: np.ndarray,
    palette: list[tuple[int, int, int]],
) -> np.ndarray:
    rgb = np.asarray(palette, dtype=np.float32)[indices].copy()
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    seam = (r >= 85.0) & (r >= g + 22.0) & (r >= b + 38.0)
    heat = np.clip((r - 75.0) / 150.0, 0.0, 1.0)
    cooled = np.stack(
        (
            49.0 + heat * 61.0,
            12.0 + heat * 20.0,
            8.0 + heat * 8.0,
        ),
        axis=2,
    )
    rgb[seam] = cooled[seam]
    _, cooled_indices = quantize(
        Image.fromarray(np.clip(np.rint(rgb), 0, 255).astype(np.uint8), mode="RGB"),
        palette,
    )
    return cooled_indices


def expand_ford_cracks(
    ford: np.ndarray,
    water: np.ndarray,
    domain: np.ndarray,
    leak_pixels: int,
) -> np.ndarray:
    """Leak Ford crust into adjacent land at 24px density, then upscale exactly 2x."""
    ford24 = ford[::2, ::2]
    water24 = water[::2, ::2]
    domain24 = domain[::2, ::2]
    distance = ndimage.distance_transform_edt(~ford24)
    yy, xx = np.indices(ford24.shape)
    # Deterministic low-frequency contour variation without breaking 2x cadence.
    noise = (
        np.sin(xx * 0.71 + yy * 0.19)
        + np.sin(xx * 0.23 - yy * 0.83)
    ) * 0.75
    radius24 = max(1.0, leak_pixels / 2.0)
    expanded24 = domain24 & ~water24 & (distance <= radius24 + noise)
    expanded = np.repeat(np.repeat(expanded24, 2, axis=0), 2, axis=1)
    return expanded[: ford.shape[0], : ford.shape[1]] & domain & ~water


def write_template(path: Path, indices: np.ndarray, spec: shore.TemplateSpec) -> None:
    frames = []
    blank = bytes(shore.TILE * shore.TILE)
    for index in range(spec.columns * spec.rows):
        if index not in spec.terrain:
            frames.append(blank)
            continue
        row, column = divmod(index, spec.columns)
        frames.append(indices[row * shore.TILE:(row + 1) * shore.TILE, column * shore.TILE:(column + 1) * shore.TILE].tobytes())
    write_shptd(path, shore.TILE, shore.TILE, frames)
    width, height, decoded = read_shptd(path)
    if (width, height) != (shore.TILE, shore.TILE) or decoded != frames:
        raise ValueError(f"{path}: roundtrip mismatch")


if __name__ == "__main__":
    raise SystemExit(main())
