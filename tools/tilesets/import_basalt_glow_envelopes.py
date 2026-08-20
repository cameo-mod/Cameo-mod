#!/usr/bin/env python
"""Import and refit unrestricted basalt lava-glow envelopes at 24px density."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_ASSET_DIR = ROOT / "tools/tilesets/assets/basalt-columns"
TRANSFORMS = {
    "1x1-a": ("approved-family", 21, 21, 0, 1),
    "1x1-b": ("approved-family", 21, 21, 0, 1),
    "2x1-a": ("approved-family", 42, 21, 1, 1),
    "2x1-b": ("approved-family", 42, 21, 1, 1),
    "2x2-a": ("approved-family", 42, 43, 1, 2),
    "2x2-b": ("approved-family", 42, 43, 1, 2),
    "1x2-a": ("expanded-family", 21, 43, 0, 2),
    "1x2-b": ("expanded-family", 21, 43, 0, 2),
    "2x3-a": ("expanded-family", 42, 64, 1, 4),
    "2x3-b": ("expanded-family", 42, 64, 1, 4),
    "3x2-a": ("expanded-family", 64, 43, 1, 2),
    "3x2-b": ("expanded-family", 64, 43, 1, 2),
}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--artist-workspace", type=Path, required=True)
    parser.add_argument("--asset-dir", type=Path, default=DEFAULT_ASSET_DIR)
    args = parser.parse_args()
    workspace = args.artist_workspace.resolve()
    asset_dir = resolve(args.asset_dir)
    manifest = json.loads((asset_dir / "manifest.json").read_text(encoding="utf-8"))
    records = []
    for variant in manifest["variants"]:
        variant_id = str(variant["id"])
        family, scaled_width, scaled_height, offset_x, offset_y = TRANSFORMS[
            variant_id
        ]
        source_dir = workspace / family / variant_id
        original_formation = load_rgba(source_dir / "formation-source.png")
        unrestricted_envelope = load_rgba(
            source_dir / "lava-glow-source-continuous-pool.png"
        )
        target_dir = asset_dir / str(variant["directory"])
        target_formation = load_rgba(target_dir / "source-24px/formation.png")
        if original_formation.size != unrestricted_envelope.size:
            raise ValueError(f"{variant_id}: source formation/envelope canvases differ")
        transformed_formation = transform(
            original_formation,
            (scaled_width, scaled_height),
            (offset_x, offset_y),
        )
        formation_mismatch = pixel_mismatch(
            transformed_formation,
            target_formation,
        )
        if formation_mismatch:
            raise ValueError(
                f"{variant_id}: recovered refit transform differs by "
                f"{formation_mismatch} formation pixels"
            )
        transformed_envelope = transform(
            unrestricted_envelope,
            (scaled_width, scaled_height),
            (offset_x, offset_y),
        )
        source_path = target_dir / "source-24px/lava-glow-envelope.png"
        footprint_path = target_dir / "footprint/lava-glow-envelope.png"
        transformed_envelope.save(source_path)
        footprint = transformed_envelope.resize(
            (transformed_envelope.width * 2, transformed_envelope.height * 2),
            Image.Resampling.NEAREST,
        )
        footprint.save(footprint_path)
        if not strict_2x(np.asarray(footprint, dtype=np.uint8)):
            raise ValueError(f"{variant_id}: footprint envelope is not strict 2x")
        if footprint.size != (
            int(variant["tiles"][0]) * 48,
            int(variant["tiles"][1]) * 48,
        ):
            raise ValueError(f"{variant_id}: footprint envelope geometry differs")
        records.append(
            {
                "variant": variant_id,
                "source_family": family,
                "original_envelope": str(
                    (source_dir / "lava-glow-source-continuous-pool.png").resolve()
                ),
                "original_envelope_sha256": sha256(
                    source_dir / "lava-glow-source-continuous-pool.png"
                ),
                "refit": {
                    "resampling": "LANCZOS",
                    "scaled_size": [scaled_width, scaled_height],
                    "offset": [offset_x, offset_y],
                    "formation_mismatch_pixels": formation_mismatch,
                },
                "source_24px": str(source_path.resolve()),
                "footprint_48px": str(footprint_path.resolve()),
                "source_bbox": list(transformed_envelope.getbbox() or (0, 0, 0, 0)),
                "footprint_bbox": list(footprint.getbbox() or (0, 0, 0, 0)),
                "strict_nearest_neighbor_2x": True,
            }
        )
    audit = {
        "status": "PASS",
        "purpose": (
            "unrestricted pre-crack-mask glow envelopes refitted with the exact "
            "approved formation transform"
        ),
        "authoring_tile_pixels": 24,
        "production_tile_pixels": 48,
        "records": records,
    }
    audit_path = asset_dir / "reference/refit-glow-envelope-import-audit.json"
    audit_path.write_text(json.dumps(audit, indent=2) + "\n", encoding="utf-8")
    print(audit_path.resolve())
    return 0


def resolve(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def load_rgba(path: Path) -> Image.Image:
    with Image.open(path) as source:
        return source.convert("RGBA")


def transform(
    source: Image.Image,
    scaled_size: tuple[int, int],
    offset: tuple[int, int],
) -> Image.Image:
    scaled = source.resize(scaled_size, Image.Resampling.LANCZOS)
    result = Image.new("RGBA", source.size, (0, 0, 0, 0))
    result.alpha_composite(scaled, offset)
    return result


def pixel_mismatch(first: Image.Image, second: Image.Image) -> int:
    a = np.asarray(first.convert("RGBA"), dtype=np.uint8)
    b = np.asarray(second.convert("RGBA"), dtype=np.uint8)
    return int(np.count_nonzero(np.any(a != b, axis=2)))


def strict_2x(values: np.ndarray) -> bool:
    return bool(
        np.array_equal(values[0::2, 0::2], values[1::2, 0::2])
        and np.array_equal(values[0::2, 0::2], values[0::2, 1::2])
        and np.array_equal(values[0::2, 0::2], values[1::2, 1::2])
    )


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


if __name__ == "__main__":
    raise SystemExit(main())
