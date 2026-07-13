#!/usr/bin/env python
"""Build/install the approved RA Temperate Strategy C Volcanic treatment.

The visual transform is evaluated from the approved 48px review pipeline, then
collapsed to 24px authoring density, palette-quantized, and upscaled exactly 2x
with nearest-neighbor. Existing liquid and authored decorations participate in
that same 24px production raster instead of receiving a second independent
filter pass.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
from pathlib import Path

import numpy as np
from PIL import Image

import generate_sh04_alpha_beach_prototype as shore
import preview_strategy_c_all_worked_tiles as full
import preview_strategy_c_shadow_boost_all as approved
import volcanic_art_utils as art
from manual_river_delta.prepare_production import quantize
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUT = (
    Path.home()
    / "Documents/agents/volcanic-theater/ground/strategy-c-production-review-01"
)
AUTHOR_TILE = 24
PRODUCTION_TILE = 48


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT)
    parser.add_argument(
        "--install",
        action="store_true",
        help="Copy audited candidate VOL files into mods/cameo/bits/volcanic",
    )
    args = parser.parse_args()

    out = args.out_dir.resolve()
    candidates = out / "candidate-vols"
    candidates.mkdir(parents=True, exist_ok=True)

    production_bits = ROOT / "mods/cameo/bits/volcanic"
    baseline_bits = out / "baseline-vols"
    prepare_baseline(production_bits, baseline_bits)
    # All source comparisons are anchored to the immutable pre-install
    # baseline. This keeps repeated candidate builds and installs idempotent.
    full.BITS = baseline_bits

    palette = shore.read_palette(full.BITS / "volcanic.pal")
    temp_palette = shore.read_palette(
        full.ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    )
    donor, low, high, clear_variants = full.build_clear_family(temp_palette, palette)
    canonical = clear_variants[0]

    liquid_before = {name: sha256(production_bits / f"{name}.vol") for name in ("w1", "w2")}
    records: list[dict[str, object]] = []
    reviews: dict[str, list[str]] = {}

    clear_pairs = build_clear(
        clear_variants, palette, candidates, production_bits, args.install, records
    )
    reviews["ground"] = write_pages(out, "ground", clear_pairs, 8)
    reviews["ground_continuity"] = [
        str(write_ground_continuity(out, candidates, palette).resolve())
    ]

    families = (
        (
            "cliffs",
            full.CLIFFS,
            lambda name: full.cliff_pair(
                name, canonical, low, high, palette, temp_palette
            ),
            8,
        ),
        (
            "shorelines",
            full.SHORES,
            lambda name: full.shore_pair(
                name, canonical, low, high, palette, temp_palette
            ),
            6,
        ),
        (
            "rivers",
            full.RIVERS,
            lambda name: full.river_pair(
                name, canonical, low, high, palette, temp_palette
            ),
            5,
        ),
        (
            "fords_crossings",
            full.FORDS,
            lambda name: full.river_pair(
                name, canonical, low, high, palette, temp_palette
            ),
            5,
        ),
        (
            "human_bridges",
            full.BRIDGES,
            lambda name: full.bridge_pair(
                name, clear_variants, low, high, palette, temp_palette
            ),
            6,
        ),
    )

    for category, names, pair_fn, page_size in families:
        pairs = []
        for name in names:
            before, strategy_c = pair_fn(name)
            spec = production_spec(name)
            indices = author_composite_at_24(strategy_c, spec, palette)
            candidate_path = candidates / f"{name}.vol"
            write_template(candidate_path, indices, spec)
            audit_candidate(candidate_path, spec)
            if args.install:
                shutil.copy2(candidate_path, production_bits / candidate_path.name)

            after_rgb = shore.indices_rgb(indices, palette)
            domain = cell_domain(spec)
            after_rgb[~domain] = shore.BACKGROUND
            after = Image.fromarray(after_rgb, mode="RGB")
            pairs.append((name, before, after))
            records.append(
                record(
                    category,
                    name,
                    spec,
                    before,
                    after,
                    candidate_path,
                )
            )
        reviews[category] = write_pages(out, category, pairs, page_size)

    liquid_after = {name: sha256(production_bits / f"{name}.vol") for name in ("w1", "w2")}
    if liquid_before != liquid_after:
        raise ValueError("Strategy C must not modify production w1.vol or w2.vol")

    phase_audit = audit_phase_aligned_liquid(
        baseline_bits, candidates, palette
    )
    if phase_audit["changed_pixels"]:
        raise ValueError("phase-aligned liquid interiors changed during Strategy C")

    violations = [r for r in records if r["nonuniform_2x_blocks"] != 0]
    if violations:
        raise ValueError(f"strict 2x cadence failed for {len(violations)} assets")

    manifest = {
        "strategy": "approved Strategy C two-material hybrid with shadow boost",
        "installed": args.install,
        "immutable_baseline": str(baseline_bits),
        "donor": "RA Temperate",
        "authoring_density": [AUTHOR_TILE, AUTHOR_TILE],
        "production_density": [PRODUCTION_TILE, PRODUCTION_TILE],
        "upscale": "exact 2x nearest-neighbor",
        "ground_palette_indices": list(range(11, 22)),
        "structure_palette_indices": list(range(10, 30)),
        "shadow_strength": approved.SHADOW_STRENGTH,
        "shadow_percentile": approved.SHADOW_PERCENTILE,
        "shadow_target_rgb": [12, 8, 8],
        "hot_lava_protected_during_shadow_remap": True,
        "liquid_reference_sha256_before": liquid_before,
        "liquid_reference_sha256_after": liquid_after,
        "phase_aligned_liquid_audit": phase_audit,
        "asset_count": len(records),
        "cadence_violations": len(violations),
        "records": records,
        "reviews": reviews,
    }
    manifest_path = out / "strategy_c_production_manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(manifest_path.resolve())
    return 0


def build_clear(clear_variants, palette, candidates, production_bits, install, records):
    current_width, current_height, current_frames = read_shptd(full.BITS / "clear1.vol")
    if (current_width, current_height, len(current_frames)) != (48, 48, 16):
        raise ValueError("clear1.vol must contain sixteen 48x48 frames")

    frames: list[bytes] = []
    pairs = []
    for index, candidate_rgb in enumerate(clear_variants):
        authored = author_rgb_at_24(candidate_rgb, palette)
        frames.append(authored.tobytes())
        before_indices = np.frombuffer(current_frames[index], dtype=np.uint8).reshape(48, 48)
        before = Image.fromarray(shore.indices_rgb(before_indices, palette), mode="RGB")
        after = Image.fromarray(shore.indices_rgb(authored, palette), mode="RGB")
        pairs.append((f"clear1 v{index:02d}", before, after))
        records.append(
            {
                "category": "ground",
                "asset": f"clear1 frame {index}",
                "frames": 1,
                "changed_pixels": int(np.count_nonzero(authored != before_indices)),
                "nonuniform_2x_blocks": count_nonuniform_blocks(authored),
            }
        )

    path = candidates / "clear1.vol"
    write_shptd(path, 48, 48, frames)
    width, height, decoded = read_shptd(path)
    if (width, height, decoded) != (48, 48, frames):
        raise ValueError("clear1 candidate roundtrip mismatch")
    if install:
        shutil.copy2(path, production_bits / path.name)
    return pairs


def prepare_baseline(production_bits: Path, baseline_bits: Path) -> None:
    marker = baseline_bits / "baseline-manifest.json"
    if marker.exists():
        return
    baseline_bits.mkdir(parents=True, exist_ok=True)
    targets = {
        "clear1",
        "w1",
        "w2",
        *full.CLIFFS,
        *full.SHORES,
        *full.RIVERS,
        *full.FORDS,
        *full.BRIDGES,
    }
    copied = []
    missing = []
    for name in sorted(targets):
        source = production_bits / f"{name}.vol"
        if source.exists():
            shutil.copy2(source, baseline_bits / source.name)
            copied.append(source.name)
        else:
            missing.append(source.name)
    shutil.copy2(production_bits / "volcanic.pal", baseline_bits / "volcanic.pal")
    marker.write_text(
        json.dumps(
            {
                "purpose": "immutable pre-Strategy-C production baseline",
                "copied": copied,
                "missing_before_install": missing,
            },
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )


def production_spec(name: str) -> shore.TemplateSpec:
    return shore.read_template_spec(full.VOLCANIC_YAML, f"{name}.vol")


def author_composite_at_24(
    image: Image.Image,
    spec: shore.TemplateSpec,
    palette: list[tuple[int, int, int]],
) -> np.ndarray:
    rgb = np.asarray(image.convert("RGB"), dtype=np.uint8)
    expected = (spec.rows * 48, spec.columns * 48, 3)
    if rgb.shape != expected:
        raise ValueError(f"{spec.image}: candidate is {rgb.shape}, expected {expected}")
    indices = author_rgb_at_24(rgb, palette)
    indices[~cell_domain(spec)] = 0
    return indices


def author_rgb_at_24(
    rgb: np.ndarray,
    palette: list[tuple[int, int, int]],
) -> np.ndarray:
    if rgb.shape[0] % 2 or rgb.shape[1] % 2:
        raise ValueError(f"image is not divisible by 2: {rgb.shape}")
    source = rgb.astype(np.float32)
    authored = source.reshape(
        source.shape[0] // 2, 2, source.shape[1] // 2, 2, 3
    ).mean(axis=(1, 3))
    authored_image = darken_shadows_24(
        Image.fromarray(np.clip(np.rint(authored), 0, 255).astype(np.uint8), mode="RGB"),
        palette,
    )
    _, indices24 = quantize(authored_image, palette)
    return np.repeat(np.repeat(indices24, 2, axis=0), 2, axis=1)


def darken_shadows_24(
    image: Image.Image,
    palette: list[tuple[int, int, int]],
) -> Image.Image:
    rgb = np.asarray(image.convert("RGB"), dtype=np.uint8)
    visible = ~np.all(
        rgb == np.asarray(shore.BACKGROUND, dtype=np.uint8), axis=2
    )
    result = art.apply_approved_shadow_boost(rgb, visible=visible)
    return Image.fromarray(result, mode="RGB")


def cell_domain(spec: shore.TemplateSpec) -> np.ndarray:
    domain = np.zeros((spec.rows * 48, spec.columns * 48), dtype=bool)
    for index in spec.terrain:
        row, column = divmod(index, spec.columns)
        domain[row * 48 : (row + 1) * 48, column * 48 : (column + 1) * 48] = True
    return domain


def write_template(path: Path, indices: np.ndarray, spec: shore.TemplateSpec) -> None:
    frames = []
    blank = bytes(48 * 48)
    for index in range(spec.columns * spec.rows):
        if index not in spec.terrain:
            frames.append(blank)
            continue
        row, column = divmod(index, spec.columns)
        frames.append(
            indices[row * 48 : (row + 1) * 48, column * 48 : (column + 1) * 48].tobytes()
        )
    write_shptd(path, 48, 48, frames)


def audit_candidate(path: Path, spec: shore.TemplateSpec) -> None:
    width, height, frames = read_shptd(path)
    if (width, height, len(frames)) != (48, 48, spec.columns * spec.rows):
        raise ValueError(f"{path}: unexpected geometry")
    for index, frame in enumerate(frames):
        tile = np.frombuffer(frame, dtype=np.uint8).reshape(48, 48)
        if index not in spec.terrain and np.any(tile):
            raise ValueError(f"{path}: gap frame {index} is not blank")
        if count_nonuniform_blocks(tile):
            raise ValueError(f"{path}: frame {index} violates strict 2x cadence")


def record(category, name, spec, before, after, path):
    before_rgb = np.asarray(before.convert("RGB"), dtype=np.uint8)
    after_rgb = np.asarray(after.convert("RGB"), dtype=np.uint8)
    _, _, frames = read_shptd(path)
    return {
        "category": category,
        "asset": name,
        "frames": len(frames),
        "size_tiles": [spec.columns, spec.rows],
        "changed_pixels": int(np.count_nonzero(np.any(before_rgb != after_rgb, axis=2))),
        "nonuniform_2x_blocks": sum(
            count_nonuniform_blocks(
                np.frombuffer(frame, dtype=np.uint8).reshape(48, 48)
            )
            for frame in frames
        ),
        "sha256": sha256(path),
    }


def count_nonuniform_blocks(indices: np.ndarray) -> int:
    blocks = indices.reshape(indices.shape[0] // 2, 2, indices.shape[1] // 2, 2)
    reference = blocks[:, :1, :, :1]
    return int(np.count_nonzero(np.any(blocks != reference, axis=(1, 3))))


def write_pages(out: Path, category: str, pairs, page_size: int) -> list[str]:
    paths = []
    for start in range(0, len(pairs), page_size):
        chunk = pairs[start : start + page_size]
        panels = []
        for name, before, after in chunk:
            panels.append((f"{name}: before", before))
            panels.append((f"{name}: production Strategy C", after))
        first = chunk[0][0].replace(" ", "_")
        last = chunk[-1][0].replace(" ", "_")
        path = out / f"{category}_{first}_{last}.png"
        full.write_page(path, panels, columns=2, scale=2)
        paths.append(str(path.resolve()))
        print(path.resolve())
    return paths


def write_ground_continuity(out: Path, candidates: Path, palette) -> Path:
    width, height, frames = read_shptd(candidates / "clear1.vol")
    order = np.random.default_rng(20260713).integers(0, len(frames), size=(8, 8))
    canvas = np.zeros((8 * height, 8 * width, 3), dtype=np.uint8)
    for row in range(8):
        for column in range(8):
            indices = np.frombuffer(
                frames[int(order[row, column])], dtype=np.uint8
            ).reshape(height, width)
            canvas[
                row * height : (row + 1) * height,
                column * width : (column + 1) * width,
            ] = shore.indices_rgb(indices, palette)
    path = out / "ground_clear1_8x8_mixed_variant_continuity.png"
    Image.fromarray(canvas, mode="RGB").resize(
        (canvas.shape[1] * 2, canvas.shape[0] * 2),
        Image.Resampling.NEAREST,
    ).save(path)
    return path


def audit_phase_aligned_liquid(baseline: Path, candidates: Path, palette):
    _, _, w1_frames = read_shptd(baseline / "w1.vol")
    w1 = np.frombuffer(w1_frames[0], dtype=np.uint8).reshape(48, 48)
    colors = np.asarray(palette, dtype=np.uint8)
    checked = 0
    preserved = 0
    skipped = []
    names = (*full.SHORES, *full.RIVERS, *full.FORDS, *full.BRIDGES)
    for name in names:
        before_path = baseline / f"{name}.vol"
        after_path = candidates / f"{name}.vol"
        if not before_path.exists():
            skipped.append(name)
            continue
        spec = production_spec(name)
        before, domain = shore.read_sparse_composite(before_path, spec)
        after, _ = shore.read_sparse_composite(after_path, spec)
        tiled = np.tile(w1, (spec.rows, spec.columns))
        rgb = colors[before]
        hot = (rgb[:, :, 0] > 95) & (
            rgb[:, :, 0] > rgb[:, :, 1] + 24
        )
        exact = (before == tiled) & hot & domain
        blocks = exact.reshape(
            exact.shape[0] // 2, 2, exact.shape[1] // 2, 2
        )
        full_blocks = np.all(blocks, axis=(1, 3))
        mask = np.repeat(np.repeat(full_blocks, 2, axis=0), 2, axis=1)
        checked += int(np.count_nonzero(mask))
        preserved += int(np.count_nonzero((after == tiled) & mask))
    return {
        "full_2x_interior_pixels_checked": checked,
        "pixels_preserved_exactly": preserved,
        "changed_pixels": checked - preserved,
        "baseline_missing_and_skipped": skipped,
    }


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


if __name__ == "__main__":
    raise SystemExit(main())
