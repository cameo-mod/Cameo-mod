#!/usr/bin/env python
"""Generate Volcanic building bibs from the RA Temperate bib family."""

from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image

import generate_sh04_alpha_beach_prototype as shore
from generate_volcanic_roads_bulk import (
    build_family_lookup,
    clear_ground_indices,
    production_ground_frames,
)
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
OUT = (
    Path.home()
    / "Documents/agents/volcanic-theater/mapgen-adoption/building-bibs-01"
)
BIBS = ("bib1", "bib2", "bib3")


def main() -> int:
    temp_palette = np.asarray(shore.read_palette(
        ROOT / "mods/cameo/bits/ratemperat/ra_temperat.pal"
    ), dtype=np.uint8)
    volcanic_palette = np.asarray(shore.read_palette(
        ROOT / "mods/cameo/bits/volcanic/volcanic.pal"
    ), dtype=np.uint8)
    lookup, calibration = build_family_lookup(temp_palette, volcanic_palette)
    ground_indices = clear_ground_indices()
    ground_frames = production_ground_frames()
    OUT.mkdir(parents=True, exist_ok=True)

    records = []
    for bib in BIBS:
        source_path = ROOT / f"mods/cameo/bits/temp/{bib}.tem"
        width, height, frames = read_shptd(source_path)
        if (width, height) != (48, 48):
            raise ValueError(f"{bib}: expected 48x48 frames")

        output = []
        donor_panels = []
        wrong_panels = []
        result_panels = []
        ground_total = 0
        for frame_index, frame in enumerate(frames):
            source = np.frombuffer(frame, dtype=np.uint8).reshape(48, 48)
            source24 = downsample_mode(source)
            mapped24 = lookup[source24]
            ground24 = np.isin(source24, ground_indices)
            ground = ground_frames[(frame_index * 7 + len(frames)) % len(ground_frames)]
            ground_source24 = ground[0::2, 0::2]
            mapped24[ground24] = ground_source24[ground24]
            mapped24[source24 == 0] = 0
            result = np.repeat(np.repeat(mapped24, 2, axis=0), 2, axis=1)
            output.append(result.tobytes())
            ground_total += int(np.count_nonzero(ground24))

            donor_panels.append(temp_palette[source])
            wrong_panels.append(volcanic_palette[source])
            result_panels.append(volcanic_palette[result])

        production = ROOT / f"mods/cameo/bits/volcanic/{bib}.vol"
        write_shptd(production, 48, 48, output)
        audit(production, len(frames))
        write_family_review(
            OUT / f"{bib}_temperate_wrong_candidate.png",
            donor_panels,
            wrong_panels,
            result_panels,
        )
        records.append({
            "bib": bib,
            "frames": len(frames),
            "ground_pixels_24px": ground_total,
            "production": str(production.resolve()),
            "strict_2x_cadence": True,
        })

    (OUT / "building_bib_audit.json").write_text(json.dumps({
        "method": "RA Temperate family LUT plus exact clear1 ground replacement",
        "calibration": calibration,
        "records": records,
    }, indent=2) + "\n", encoding="utf-8")
    print(OUT.resolve())
    return 0


def downsample_mode(source: np.ndarray) -> np.ndarray:
    blocks = source.reshape(24, 2, 24, 2).transpose(0, 2, 1, 3).reshape(24, 24, 4)
    result = np.zeros((24, 24), dtype=np.uint8)
    for y in range(24):
        for x in range(24):
            result[y, x] = np.bincount(blocks[y, x], minlength=256).argmax()
    return result


def write_family_review(path, donors, wrong, results):
    rows = []
    for images in (donors, wrong, results):
        rows.append(np.concatenate(images, axis=1))
    sheet = np.concatenate(rows, axis=0)
    Image.fromarray(sheet, mode="RGB").resize(
        (sheet.shape[1] * 2, sheet.shape[0] * 2), Image.Resampling.NEAREST
    ).save(path)


def audit(path: Path, expected_frames: int):
    width, height, frames = read_shptd(path)
    if (width, height, len(frames)) != (48, 48, expected_frames):
        raise ValueError(f"{path.name}: metadata mismatch")
    for frame in frames:
        image = np.frombuffer(frame, dtype=np.uint8).reshape(48, 48)
        blocks = image.reshape(24, 2, 24, 2).transpose(0, 2, 1, 3)
        if np.any(blocks != blocks[:, :, :1, :1]):
            raise ValueError(f"{path.name}: strict 2x cadence failure")


if __name__ == "__main__":
    raise SystemExit(main())
