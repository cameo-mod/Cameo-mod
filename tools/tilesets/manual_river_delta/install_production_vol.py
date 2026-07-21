#!/usr/bin/env python
"""Install an approved indexed river-delta composite into a SHP(TD) tile file."""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import sys
from datetime import datetime
from pathlib import Path

import numpy as np
from PIL import Image


TILE = 48


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("project", type=Path)
    parser.add_argument("--repo", type=Path, required=True)
    parser.add_argument("--target-vol", type=Path, required=True)
    parser.add_argument("--palette", type=Path, required=True)
    args = parser.parse_args()
    project = args.project.resolve()
    repo = args.repo.resolve()
    target = args.target_vol.resolve()
    palette_path = args.palette.resolve()
    sys.path.insert(0, str(repo / "tools/tilesets"))
    from shptd import read_shptd, write_shptd

    manifest = json.loads((project / "project.json").read_text(encoding="utf-8"))
    tile = manifest["tile"]
    indexed_path = project / manifest["production_output_directory"] / f"production_indexed_{tile}.png"
    with Image.open(indexed_path) as source:
        if source.mode != "P":
            raise ValueError(f"{indexed_path} must be an indexed P-mode PNG")
        indices = np.asarray(source, dtype=np.uint8).copy()
    expected_size = (manifest["canvas"]["width"], manifest["canvas"]["height"])
    if (indices.shape[1], indices.shape[0]) != expected_size:
        raise ValueError("indexed production image geometry differs from project manifest")
    if indices.shape[0] % TILE or indices.shape[1] % TILE:
        raise ValueError("production image dimensions must be divisible by 48")

    original_bytes = target.read_bytes()
    original_width, original_height, original_frames = read_shptd(target)
    frames = slice_frames(indices)
    if (original_width, original_height) != (TILE, TILE):
        raise ValueError("target .vol does not contain 48x48 frames")
    if len(original_frames) != len(frames):
        raise ValueError(
            f"target has {len(original_frames)} frames; production image requires {len(frames)}"
        )

    backup_dir = project / manifest["production_output_directory"] / "backups"
    backup_dir.mkdir(parents=True, exist_ok=True)
    timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    backup = backup_dir / f"{tile}_before_manual_river_{timestamp}.vol"
    shutil.copy2(target, backup)

    write_shptd(target, TILE, TILE, frames)
    installed_width, installed_height, installed_frames = read_shptd(target)
    roundtrip_ok = (
        (installed_width, installed_height) == (TILE, TILE)
        and installed_frames == frames
    )
    if not roundtrip_ok:
        shutil.copy2(backup, target)
        raise RuntimeError("round-trip verification failed; original .vol restored")

    palette = read_palette(palette_path)
    roundtrip_indices = compose_frames(installed_frames, indices.shape[1], indices.shape[0])
    roundtrip_rgb = np.asarray(palette, dtype=np.uint8)[roundtrip_indices]
    roundtrip_path = project / manifest["production_output_directory"] / f"installed_roundtrip_{tile}.png"
    Image.fromarray(roundtrip_rgb, mode="RGB").save(roundtrip_path)

    installed_bytes = target.read_bytes()
    audit = {
        "tile": tile,
        "target_vol": str(target),
        "backup_vol": str(backup.resolve()),
        "frame_width": installed_width,
        "frame_height": installed_height,
        "frame_count": len(installed_frames),
        "decoded_frames_match_production_indices": roundtrip_ok,
        "roundtrip_preview": str(roundtrip_path.resolve()),
        "original_size_bytes": len(original_bytes),
        "installed_size_bytes": len(installed_bytes),
        "original_sha256": hashlib.sha256(original_bytes).hexdigest(),
        "installed_sha256": hashlib.sha256(installed_bytes).hexdigest(),
        "vol_written": True,
    }
    audit_path = project / manifest["production_output_directory"] / f"install_audit_{tile}.json"
    audit_path.write_text(json.dumps(audit, indent=2), encoding="utf-8")
    print(json.dumps(audit, indent=2))
    return 0


def slice_frames(indices: np.ndarray) -> list[bytes]:
    frames = []
    for cell_y in range(indices.shape[0] // TILE):
        for cell_x in range(indices.shape[1] // TILE):
            frame = indices[
                cell_y * TILE : (cell_y + 1) * TILE,
                cell_x * TILE : (cell_x + 1) * TILE,
            ]
            frames.append(frame.tobytes())
    return frames


def compose_frames(frames: list[bytes], width: int, height: int) -> np.ndarray:
    result = np.zeros((height, width), dtype=np.uint8)
    columns = width // TILE
    for index, frame in enumerate(frames):
        cell_y, cell_x = divmod(index, columns)
        result[
            cell_y * TILE : (cell_y + 1) * TILE,
            cell_x * TILE : (cell_x + 1) * TILE,
        ] = np.frombuffer(frame, dtype=np.uint8).reshape((TILE, TILE))
    return result


def read_palette(path: Path) -> list[tuple[int, int, int]]:
    data = path.read_bytes()
    if len(data) != 768:
        raise ValueError(f"{path}: expected a 768-byte C&C palette")
    return [
        tuple(data[offset + channel] * 4 for channel in range(3))
        for offset in range(0, 768, 3)
    ]


if __name__ == "__main__":
    raise SystemExit(main())
