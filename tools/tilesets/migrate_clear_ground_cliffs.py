#!/usr/bin/env python
"""Build preview-only cliff VOLs against a replacement clear-ground tile."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
from datetime import datetime
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

from recolor_cliff_luminance import classify_rock, load_native, read_palette
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
TILE = 48
CLIFFS = tuple(f"s{index:02d}" for index in range(1, 39))
BACKGROUND = (73, 86, 99)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--old-clear", type=Path, required=True)
    parser.add_argument("--new-clear", type=Path, required=True)
    parser.add_argument("--temperate-dir", type=Path, required=True)
    parser.add_argument("--snow-dir", type=Path, required=True)
    parser.add_argument(
        "--source-dir",
        type=Path,
        default=ROOT / "mods/cameo/bits/volcanic",
    )
    parser.add_argument(
        "--palette",
        type=Path,
        default=ROOT / "mods/cameo/bits/volcanic/volcanic.pal",
    )
    parser.add_argument("--out-dir", type=Path, required=True)
    parser.add_argument("--install", action="store_true")
    args = parser.parse_args()

    out_dir = resolve(args.out_dir)
    candidate_dir = out_dir / "candidate-vols"
    candidate_dir.mkdir(parents=True, exist_ok=True)
    old_clear = unique_clear(resolve(args.old_clear))
    new_clear = unique_clear(resolve(args.new_clear))
    palette = read_palette(resolve(args.palette))
    temperate_sources = source_map(resolve(args.temperate_dir))
    snow_sources = source_map(resolve(args.snow_dir))
    source_dir = resolve(args.source_dir)

    records: list[dict[str, object]] = []
    panels: list[tuple[str, Image.Image]] = []
    pair_panels: list[tuple[str, Image.Image, Image.Image]] = []
    for tile in CLIFFS:
        if tile not in temperate_sources or tile not in snow_sources:
            raise FileNotFoundError(f"missing donor source for {tile}")
        temperate = load_native(temperate_sources[tile])
        snow = load_native(snow_sources[tile])
        if temperate.size != snow.size:
            raise ValueError(f"{tile}: donor dimensions differ")
        rock_mask = classify_rock(temperate, snow)
        occupied = occupied_cells(temperate)
        width, height, current_frames = read_shptd(source_dir / f"{tile}.vol")
        if (width, height) != (TILE, TILE) or len(current_frames) != len(occupied):
            raise ValueError(
                f"{tile}: source VOL has {width}x{height}/{len(current_frames)}, "
                f"donor has {len(occupied)} occupied cells"
            )

        candidate_frames: list[bytes] = []
        changed = 0
        rock_changes = 0
        for frame, (cell_x, cell_y) in zip(current_frames, occupied):
            current = np.frombuffer(frame, dtype=np.uint8).reshape(TILE, TILE)
            mask = np.asarray(
                rock_mask.crop(
                    (
                        cell_x * TILE,
                        cell_y * TILE,
                        (cell_x + 1) * TILE,
                        (cell_y + 1) * TILE,
                    )
                ),
                dtype=np.uint8,
            )
            replacement = (mask < 128) & (current == old_clear)
            candidate = current.copy()
            candidate[replacement] = new_clear[replacement]
            changed += int(np.count_nonzero(candidate != current))
            rock_changes += int(np.count_nonzero((candidate != current) & (mask >= 128)))
            candidate_frames.append(candidate.tobytes())

        candidate_path = candidate_dir / f"{tile}.vol"
        write_shptd(candidate_path, TILE, TILE, candidate_frames)
        verify_roundtrip(candidate_path, candidate_frames)
        current_composite = compose(current_frames, occupied, temperate.size, palette)
        candidate_composite = compose(candidate_frames, occupied, temperate.size, palette)
        panels.append((tile, candidate_composite))
        pair_panels.append((tile, current_composite, candidate_composite))
        records.append(
            {
                "tile": tile,
                "frames": len(candidate_frames),
                "changed_ground_pixels": changed,
                "changed_rock_pixels": rock_changes,
                "current_size": list(current_composite.size),
                "candidate_roundtrip_exact": True,
            }
        )

    review = out_dir / "cliffs_new_clear_ground_mass_review_s01_s38.png"
    write_mass_review(review, panels)
    pair_review = out_dir / "cliffs_new_clear_ground_before_after_s01_s38.png"
    write_pair_review(pair_review, pair_panels)
    install_records: list[dict[str, object]] = []
    if args.install:
        install_records = install_candidates(
            source_dir,
            resolve(args.new_clear),
            candidate_dir,
            out_dir,
        )

    audit = {
        "old_clear": str(resolve(args.old_clear)),
        "new_clear": str(resolve(args.new_clear)),
        "candidate_cliffs": len(records),
        "installed": args.install,
        "changed_ground_pixels": sum(int(record["changed_ground_pixels"]) for record in records),
        "changed_rock_pixels": sum(int(record["changed_rock_pixels"]) for record in records),
        "cliffs": records,
        "review": str(review.resolve()),
        "before_after_review": str(pair_review.resolve()),
        "install_records": install_records,
    }
    audit_path = out_dir / "cliffs_new_clear_ground_audit.json"
    audit_path.write_text(json.dumps(audit, indent=2) + "\n", encoding="utf-8")
    print(review.resolve())
    print(pair_review.resolve())
    print(audit_path.resolve())
    return 0


def resolve(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def unique_clear(path: Path) -> np.ndarray:
    width, height, frames = read_shptd(path)
    if (width, height, len(frames)) != (TILE, TILE, 16) or len(set(frames)) != 1:
        raise ValueError(f"{path}: expected 48x48/16 with one unique frame")
    return np.frombuffer(frames[0], dtype=np.uint8).reshape(TILE, TILE)


def source_map(directory: Path) -> dict[str, Path]:
    result: dict[str, Path] = {}
    for path in directory.glob("*-s??-x4.png"):
        match = re.fullmatch(r"\d{4}-(s\d{2})-x4\.png", path.name)
        if match and match.group(1) in CLIFFS:
            result[match.group(1)] = path
    return result


def occupied_cells(image: Image.Image) -> list[tuple[int, int]]:
    if image.width % TILE or image.height % TILE:
        raise ValueError(f"donor is not tile aligned: {image.size}")
    alpha = np.asarray(image.getchannel("A"), dtype=np.uint8)
    cells = []
    for cell_y in range(image.height // TILE):
        for cell_x in range(image.width // TILE):
            block = alpha[
                cell_y * TILE : (cell_y + 1) * TILE,
                cell_x * TILE : (cell_x + 1) * TILE,
            ]
            if np.any(block):
                cells.append((cell_x, cell_y))
    return cells


def verify_roundtrip(path: Path, expected: list[bytes]) -> None:
    width, height, frames = read_shptd(path)
    if (width, height) != (TILE, TILE) or frames != expected:
        raise ValueError(f"{path}: roundtrip mismatch")


def install_candidates(
    target_dir: Path,
    clear_candidate: Path,
    candidate_dir: Path,
    out_dir: Path,
) -> list[dict[str, object]]:
    timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    backup_dir = out_dir / "backups" / timestamp
    backup_dir.mkdir(parents=True, exist_ok=True)
    sources = {"clear1.vol": clear_candidate}
    sources.update({f"{tile}.vol": candidate_dir / f"{tile}.vol" for tile in CLIFFS})
    backups: dict[str, Path] = {}
    installed: list[str] = []
    try:
        for name, source in sources.items():
            target = target_dir / name
            if not target.exists():
                raise FileNotFoundError(target)
            backup = backup_dir / name
            shutil.copy2(target, backup)
            backups[name] = backup
            shutil.copy2(source, target)
            installed.append(name)
            if sha256(source) != sha256(target):
                raise RuntimeError(f"{name}: installed hash differs from candidate")
    except Exception:
        for name in installed:
            shutil.copy2(backups[name], target_dir / name)
        raise
    return [
        {
            "target": str((target_dir / name).resolve()),
            "backup": str(backups[name].resolve()),
            "sha256": sha256(target_dir / name),
        }
        for name in sources
    ]


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def compose(
    frames: list[bytes],
    occupied: list[tuple[int, int]],
    size: tuple[int, int],
    palette: list[tuple[int, int, int]],
) -> Image.Image:
    result = Image.new("RGB", size, BACKGROUND)
    colors = np.asarray(palette, dtype=np.uint8)
    for frame, (cell_x, cell_y) in zip(frames, occupied):
        indices = np.frombuffer(frame, dtype=np.uint8).reshape(TILE, TILE)
        rgba = np.zeros((TILE, TILE, 4), dtype=np.uint8)
        rgba[:, :, :3] = colors[indices]
        rgba[:, :, 3] = np.where(indices == 0, 0, 255).astype(np.uint8)
        sprite = Image.fromarray(rgba, mode="RGBA")
        result.paste(sprite, (cell_x * TILE, cell_y * TILE), sprite)
    return result


def write_mass_review(path: Path, panels: list[tuple[str, Image.Image]]) -> None:
    columns = 5
    scale = 2
    header = 24
    cell_width = 3 * TILE * scale
    cell_height = 3 * TILE * scale
    rows = (len(panels) + columns - 1) // columns
    sheet = Image.new(
        "RGB",
        (columns * cell_width, rows * (cell_height + header)),
        BACKGROUND,
    )
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for index, (label, panel) in enumerate(panels):
        x = (index % columns) * cell_width
        y = (index // columns) * (cell_height + header)
        draw.text((x + 6, y + 6), label, fill="white", font=font)
        resized = panel.resize(
            (panel.width * scale, panel.height * scale),
            Image.Resampling.NEAREST,
        )
        paste_x = x + (cell_width - resized.width) // 2
        paste_y = y + header + (cell_height - resized.height) // 2
        sheet.paste(resized, (paste_x, paste_y))
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)


def write_pair_review(
    path: Path,
    panels: list[tuple[str, Image.Image, Image.Image]],
) -> None:
    columns = 3
    scale = 2
    header = 24
    half_width = 3 * TILE * scale
    cell_width = half_width * 2
    cell_height = 3 * TILE * scale
    rows = (len(panels) + columns - 1) // columns
    sheet = Image.new(
        "RGB",
        (columns * cell_width, rows * (cell_height + header)),
        BACKGROUND,
    )
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for index, (label, before, after) in enumerate(panels):
        x = (index % columns) * cell_width
        y = (index // columns) * (cell_height + header)
        draw.text((x + 6, y + 6), f"{label}: CURRENT", fill="white", font=font)
        draw.text((x + half_width + 6, y + 6), "NEW 24px GROUND", fill="white", font=font)
        for offset, image in ((0, before), (half_width, after)):
            resized = image.resize(
                (image.width * scale, image.height * scale),
                Image.Resampling.NEAREST,
            )
            paste_x = x + offset + (half_width - resized.width) // 2
            paste_y = y + header + (cell_height - resized.height) // 2
            sheet.paste(resized, (paste_x, paste_y))
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)


if __name__ == "__main__":
    raise SystemExit(main())
