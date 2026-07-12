#!/usr/bin/env python
"""Build all sh01-sh54 shores against a candidate clear-lava topology."""

from __future__ import annotations

import argparse
import contextlib
import hashlib
import io
import json
import shutil
import subprocess
import sys
from datetime import datetime
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

from generate_sh04_alpha_beach_prototype import (
    generate_sparse_shore,
    indices_rgb,
    read_palette,
    read_template_spec,
)
from manual_river_delta.prepare_production import quantize
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
TILE = 48
SHORE_TEMPLATES = tuple(f"sh{index:02d}" for index in range(1, 55))
RIVER_DELTAS = frozenset(("sh09", "sh18", "sh30", "sh39"))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--clear-lava-dir", type=Path, required=True)
    parser.add_argument("--projects-root", type=Path, required=True)
    parser.add_argument("--approved-shores-root", type=Path, required=True)
    parser.add_argument("--out-dir", type=Path, required=True)
    parser.add_argument("--install", action="store_true")
    args = parser.parse_args()

    clear_lava_dir = args.clear_lava_dir.resolve()
    projects_root = args.projects_root.resolve()
    approved_root = args.approved_shores_root.resolve()
    out_dir = args.out_dir.resolve()
    candidate_dir = out_dir / "candidate-vols"
    regenerated_dir = out_dir / "regenerated-shore-bases"
    candidate_dir.mkdir(parents=True, exist_ok=True)
    regenerated_dir.mkdir(parents=True, exist_ok=True)

    bits = ROOT / "mods/cameo/bits/volcanic"
    tileset = ROOT / "mods/cameo/tilesets/volcanic.yaml"
    palette = read_palette(bits / "volcanic.pal")
    w1_candidate = clear_lava_dir / "w1-clear-lava-preview.vol"
    w2_candidate = clear_lava_dir / "w2-clear-lava-preview.vol"
    verify_engine_tile(w1_candidate, 1, strict_2x=True)
    verify_engine_tile(w2_candidate, 4, strict_2x=True)
    shutil.copy2(w1_candidate, candidate_dir / "w1.vol")
    shutil.copy2(w2_candidate, candidate_dir / "w2.vol")

    records: list[dict[str, object]] = []
    review_pairs: list[tuple[str, Image.Image, Image.Image]] = []
    candidates: list[tuple[str, Image.Image]] = []
    changed_targets = ["w1.vol", "w2.vol"]
    for ordinal, tile in enumerate(SHORE_TEMPLATES, start=1):
        print(f"[{ordinal:02d}/54] regenerating {tile}", flush=True)
        generate_shore_base(regenerated_dir, tile, w1_candidate)
        spec = read_template_spec(tileset, f"{tile}.vol")
        regenerated = regenerated_dir / f"lava_seepage_composite_{tile}.png"
        candidate = candidate_dir / f"{tile}.vol"
        if tile in RIVER_DELTAS:
            record, before, after = rebuild_river_delta(
                tile,
                projects_root / tile,
                bits / f"{tile}.vol",
                spec,
                regenerated,
                palette,
                candidate,
            )
        else:
            record, before, after = build_standard_shore(
                tile,
                approved_root / f"lava_seepage_composite_{tile}.png",
                regenerated,
                spec,
                palette,
                candidate,
            )
        records.append(record)
        review_pairs.append((tile, before, after))
        candidates.append((tile, after))
        changed_targets.append(f"{tile}.vol")

    clear_pairs = [
        (
            "w1 4x4 repeat",
            repeat_composite(decode_vol(bits / "w1.vol", 1, palette), 4, 4),
            repeat_composite(decode_vol(candidate_dir / "w1.vol", 1, palette), 4, 4),
        ),
        (
            "w2 2x2 blocks",
            repeat_composite(decode_vol(bits / "w2.vol", 2, palette), 2, 2),
            repeat_composite(decode_vol(candidate_dir / "w2.vol", 2, palette), 2, 2),
        ),
    ]
    clear_review = out_dir / "clear_lava_24px_water_review.png"
    mass_review = out_dir / "shoreline_24px_candidate_mass_review_sh01_sh54.png"
    write_pair_review(clear_review, clear_pairs, pairs_per_row=1)
    write_mass_review(mass_review, candidates)
    detail_reviews = write_detail_reviews(out_dir, review_pairs)

    install_records: list[dict[str, object]] = []
    if args.install:
        backup_dir = out_dir / "backups"
        backup_dir.mkdir(parents=True, exist_ok=True)
        stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
        for name in changed_targets:
            source = candidate_dir / name
            target = bits / name
            backup = None
            if target.is_file():
                backup = backup_dir / f"{Path(name).stem}-before-24px-{stamp}.vol"
                shutil.copy2(target, backup)
            shutil.copy2(source, target)
            if target.read_bytes() != source.read_bytes():
                if backup is not None:
                    shutil.copy2(backup, target)
                else:
                    target.unlink(missing_ok=True)
                raise RuntimeError(f"{name}: install verification failed")
            install_records.append(
                {
                    "target": str(target.resolve()),
                    "backup": str(backup.resolve()) if backup else None,
                    "sha256": sha256(target),
                }
            )

    audit = {
        "author_tile_size": 24,
        "engine_tile_size": 48,
        "upscale": "strict nearest-neighbor 2x",
        "installed": args.install,
        "candidate_shores": len(records),
        "changed_targets": changed_targets,
        "river_delta_cutouts_preserved": sorted(RIVER_DELTAS),
        "shores": records,
        "install_records": install_records,
        "reviews": {
            "water": str(clear_review.resolve()),
            "mass": str(mass_review.resolve()),
            "details": [str(path.resolve()) for path in detail_reviews],
        },
    }
    audit_path = out_dir / "shoreline_24px_full_migration_audit.json"
    audit_path.write_text(json.dumps(audit, indent=2) + "\n", encoding="utf-8")
    print(mass_review.resolve())
    for path in detail_reviews:
        print(path.resolve())
    print(audit_path.resolve())
    return 0


def generate_shore_base(out_dir: Path, tile: str, w1_vol: Path) -> None:
    if tile == "sh04":
        command = [
            sys.executable,
            str(ROOT / "tools/tilesets/generate_sh04_alpha_beach_prototype.py"),
            "--template",
            tile,
            "--w1-vol",
            str(w1_vol),
            "--out-dir",
            str(out_dir),
        ]
        subprocess.run(command, check=True, stdout=subprocess.DEVNULL)
        return
    with contextlib.redirect_stdout(io.StringIO()):
        generate_sparse_shore(out_dir, tile, w1_vol)


def build_standard_shore(
    tile: str,
    approved: Path,
    regenerated: Path,
    spec,
    palette: list[tuple[int, int, int]],
    candidate: Path,
) -> tuple[dict[str, object], Image.Image, Image.Image]:
    before = Image.open(approved).convert("RGB")
    source = Image.open(regenerated).convert("RGB")
    expected = (spec.columns * TILE, spec.rows * TILE)
    if before.size != expected or source.size != expected:
        raise ValueError(f"{tile}: shoreline PNG geometry differs from tileset")
    before_indexed, before_indices = quantize(before, palette)
    after_indexed, after_indices = quantize(source, palette)
    write_template_vol(candidate, after_indices, spec)
    changed = pixel_difference(before_indexed, after_indexed)
    return (
        shore_record(tile, spec, expected, changed, False),
        before_indexed,
        after_indexed,
    )


def rebuild_river_delta(
    tile: str,
    project: Path,
    installed_vol: Path,
    spec,
    regenerated: Path,
    palette: list[tuple[int, int, int]],
    candidate: Path,
) -> tuple[dict[str, object], Image.Image, Image.Image]:
    manifest = json.loads((project / "project.json").read_text(encoding="utf-8"))
    base = Image.open(regenerated).convert("RGBA")
    cutout = Image.open(
        project / manifest["required_exports"]["lava_cutout"]
    ).convert("RGBA")
    expected = (spec.columns * TILE, spec.rows * TILE)
    if base.size != expected or cutout.size != expected:
        raise ValueError(f"{tile}: river-delta geometry differs from tileset")
    recomposed = Image.alpha_composite(base, cutout).convert("RGB")
    after, indices = quantize(recomposed, palette)
    write_template_vol(candidate, indices, spec)
    before = decode_vol(installed_vol, spec.columns, palette)
    changed = pixel_difference(before, after)
    record = shore_record(tile, spec, expected, changed, True)
    record["manual_lava_cutout_preserved"] = True
    return record, before, after


def shore_record(
    tile: str,
    spec,
    expected: tuple[int, int],
    changed: int,
    river_delta: bool,
) -> dict[str, object]:
    return {
        "tile": tile,
        "canvas": list(expected),
        "frame_count": spec.columns * spec.rows,
        "occupied_frames": len(spec.terrain),
        "changed_indexed_pixels": changed,
        "river_delta": river_delta,
        "candidate_roundtrip_exact": True,
    }


def write_template_vol(path: Path, indices: np.ndarray, spec) -> None:
    frames = []
    blank = bytes(TILE * TILE)
    for index in range(spec.columns * spec.rows):
        if index not in spec.terrain:
            frames.append(blank)
            continue
        row, column = divmod(index, spec.columns)
        frames.append(
            indices[
                row * TILE : (row + 1) * TILE,
                column * TILE : (column + 1) * TILE,
            ].tobytes()
        )
    write_shptd(path, TILE, TILE, frames)
    verify_engine_tile(path, len(frames))
    _, _, decoded = read_shptd(path)
    if decoded != frames:
        raise RuntimeError(f"{path.name}: candidate round-trip mismatch")


def verify_engine_tile(
    path: Path,
    expected_frames: int,
    *,
    strict_2x: bool = False,
) -> None:
    width, height, frames = read_shptd(path)
    if (width, height) != (TILE, TILE) or len(frames) != expected_frames:
        raise ValueError(f"{path}: unexpected engine geometry")
    if strict_2x:
        for index, frame in enumerate(frames):
            pixels = np.frombuffer(frame, dtype=np.uint8).reshape(TILE, TILE)
            restored = np.repeat(
                np.repeat(pixels[::2, ::2], 2, axis=0), 2, axis=1
            )
            if not np.array_equal(pixels, restored):
                raise ValueError(f"{path}: frame {index} is not a strict 2x upscale")


def decode_vol(
    path: Path,
    columns: int,
    palette: list[tuple[int, int, int]],
) -> Image.Image:
    width, height, frames = read_shptd(path)
    rows = (len(frames) + columns - 1) // columns
    indices = np.zeros((rows * height, columns * width), dtype=np.uint8)
    for index, frame in enumerate(frames):
        row, column = divmod(index, columns)
        indices[
            row * height : (row + 1) * height,
            column * width : (column + 1) * width,
        ] = np.frombuffer(frame, dtype=np.uint8).reshape(height, width)
    return Image.fromarray(indices_rgb(indices, palette), mode="RGB")


def repeat_composite(image: Image.Image, columns: int, rows: int) -> Image.Image:
    result = Image.new("RGB", (image.width * columns, image.height * rows))
    for row in range(rows):
        for column in range(columns):
            result.paste(image, (column * image.width, row * image.height))
    return result


def pixel_difference(first: Image.Image, second: Image.Image) -> int:
    return int(
        np.count_nonzero(
            np.any(
                np.asarray(first, dtype=np.uint8)
                != np.asarray(second, dtype=np.uint8),
                axis=2,
            )
        )
    )


def write_mass_review(path: Path, panels: list[tuple[str, Image.Image]]) -> None:
    columns = 6
    panel = 192
    header = 22
    rows = (len(panels) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * panel, rows * (panel + header)), (73, 86, 99))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for index, (label, image) in enumerate(panels):
        x = (index % columns) * panel
        y = (index // columns) * (panel + header)
        draw.text((x + 5, y + 5), label, fill="white", font=font)
        sheet.paste(fit_panel(image, panel), (x, y + header))
    sheet.save(path)


def write_detail_reviews(
    out_dir: Path,
    pairs: list[tuple[str, Image.Image, Image.Image]],
) -> list[Path]:
    paths = []
    for start in range(0, len(pairs), 18):
        subset = pairs[start : start + 18]
        first = start + 1
        last = start + len(subset)
        path = out_dir / f"shoreline_24px_before_after_sh{first:02d}_sh{last:02d}.png"
        write_pair_review(path, subset, pairs_per_row=3)
        paths.append(path)
    return paths


def write_pair_review(
    path: Path,
    pairs: list[tuple[str, Image.Image, Image.Image]],
    *,
    pairs_per_row: int,
) -> None:
    panel = 192
    header = 22
    rows = (len(pairs) + pairs_per_row - 1) // pairs_per_row
    sheet = Image.new(
        "RGB",
        (pairs_per_row * panel * 2, rows * (panel + header)),
        (73, 86, 99),
    )
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for index, (label, before, after) in enumerate(pairs):
        pair_x = (index % pairs_per_row) * panel * 2
        y = (index // pairs_per_row) * (panel + header)
        draw.text((pair_x + 5, y + 5), f"{label} before", fill="white", font=font)
        draw.text(
            (pair_x + panel + 5, y + 5),
            f"{label} 24px",
            fill="white",
            font=font,
        )
        sheet.paste(fit_panel(before, panel), (pair_x, y + header))
        sheet.paste(fit_panel(after, panel), (pair_x + panel, y + header))
    sheet.save(path)


def fit_panel(image: Image.Image, panel: int) -> Image.Image:
    canvas = Image.new("RGB", (panel, panel), (73, 86, 99))
    scale = min(panel / image.width, panel / image.height)
    size = (round(image.width * scale), round(image.height * scale))
    resized = image.resize(size, Image.Resampling.NEAREST)
    canvas.paste(resized, ((panel - size[0]) // 2, (panel - size[1]) // 2))
    return canvas


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


if __name__ == "__main__":
    raise SystemExit(main())
