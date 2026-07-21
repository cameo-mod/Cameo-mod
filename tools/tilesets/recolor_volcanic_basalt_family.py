#!/usr/bin/env python3
"""Recolor existing Volcanic basalt actor sprites without changing geometry."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image

import generate_sh04_alpha_beach_prototype as shore
from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
ACTORS = (
    "t01", "t02", "t03", "t05", "t06", "t07", "t08",
    "t10", "t11", "t12", "t13", "t14", "t15", "t16", "t17",
    "tc01", "tc02", "tc03", "tc04", "tc05",
)


def luminance(rgb: np.ndarray) -> np.ndarray:
    return 0.2126 * rgb[..., 0] + 0.7152 * rgb[..., 1] + 0.0722 * rgb[..., 2]


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def uniform_2x2(indices: np.ndarray) -> bool:
    if indices.shape[0] % 2 or indices.shape[1] % 2:
        return False
    return all(
        np.array_equal(indices[0::2, 0::2], part)
        for part in (indices[0::2, 1::2], indices[1::2, 0::2], indices[1::2, 1::2])
    )


def build_reference_ramp(path: Path, brightness: float) -> np.ndarray:
    rgba = np.asarray(Image.open(path).convert("RGBA"))
    rgb = rgba[..., :3][rgba[..., 3] > 8].astype(np.float32)
    chroma = rgb.max(axis=1) - rgb.min(axis=1)
    rgb = rgb[chroma <= 32]
    rgb = rgb[np.argsort(luminance(rgb))]
    # As in the approved pilot, avoid isolated cap-glint colors at the very top.
    rgb = rgb[: max(1, round(len(rgb) * 0.92))]
    return np.clip(rgb * brightness, 0, 255)


def palette_lut(palette: np.ndarray, ramp: np.ndarray) -> np.ndarray:
    pal = np.asarray(palette, dtype=np.float32)[:, :3]
    chroma = pal.max(axis=1) - pal.min(axis=1)
    allowed = np.flatnonzero((chroma <= 40) & (np.arange(len(pal)) != 0) & (np.arange(len(pal)) != 4))
    lut = np.zeros(len(ramp), dtype=np.uint8)
    for i, color in enumerate(ramp):
        distance = np.sum((pal[allowed] - color) ** 2, axis=1)
        lut[i] = allowed[int(np.argmin(distance))]
    return lut


def calibrated_lut(current_path: Path, target_path: Path, palette: np.ndarray) -> tuple[np.ndarray, dict]:
    width, height, frames = read_shptd(current_path)
    current = np.frombuffer(frames[0], dtype=np.uint8).reshape(height, width)
    target = np.asarray(Image.open(target_path).convert("RGBA"))
    if target.shape[:2] != current.shape:
        raise ValueError(f"calibration size mismatch: {current.shape} != {target.shape[:2]}")
    pal = np.asarray(palette, dtype=np.float32)[:, :3]
    target_rgb = target[..., :3].astype(np.float32)
    target_indices = np.argmin(
        np.sum((target_rgb[..., None, :] - pal[None, None, ...]) ** 2, axis=3), axis=2
    ).astype(np.uint8)
    body = (current != 0) & (current != 4)
    observed = np.unique(current[body])
    lut = np.arange(len(pal), dtype=np.uint8)
    mapping = {}
    for value in observed:
        choices, counts = np.unique(target_indices[(current == value) & body], return_counts=True)
        valid = (choices != 0) & (choices != 4)
        if np.any(valid):
            choices = choices[valid]
            counts = counts[valid]
            chosen = choices[int(np.argmax(counts))]
        else:
            allowed = np.array([i for i in range(len(pal)) if i not in (0, 4)], dtype=int)
            chosen = allowed[int(np.argmin(np.sum((pal[allowed] - pal[value]) ** 2, axis=1)))]
        lut[value] = chosen
        mapping[int(value)] = int(chosen)

    # Extend the t01-calibrated material transform to palette entries used only
    # by larger formations, choosing the nearest observed source material color.
    body_values = np.array([i for i in range(len(pal)) if i not in (0, 4)], dtype=int)
    for value in body_values:
        if value in observed:
            continue
        nearest = observed[int(np.argmin(np.sum((pal[observed] - pal[value]) ** 2, axis=1)))]
        lut[value] = lut[nearest]
    lut[0] = 0
    lut[4] = 4
    agreement = float(np.mean(lut[current[body]] == target_indices[body]))
    return lut, {"observed_mapping": mapping, "pixel_agreement": agreement}


def balance_body_shadows(indices: np.ndarray, palette: np.ndarray, low_factor: float) -> np.ndarray:
    result = indices.copy()
    body = (indices != 0) & (indices != 4)
    values, counts = np.unique(indices[body], return_counts=True)
    pal = np.asarray(palette, dtype=np.float32)[:, :3]
    value_luminance = luminance(pal[values])
    expanded = np.repeat(value_luminance, counts)
    q35, q70 = np.percentile(expanded, [35, 70])
    chroma = pal.max(axis=1) - pal.min(axis=1)
    allowed = np.flatnonzero((chroma <= 40) & (np.arange(len(pal)) != 0) & (np.arange(len(pal)) != 4))
    for value, level in zip(values, value_luminance):
        mix = np.clip((level - q35) / max(1e-6, q70 - q35), 0.0, 1.0)
        factor = low_factor + (1.0 - low_factor) * mix
        target = pal[value] * factor
        nearest = allowed[int(np.argmin(np.sum((pal[allowed] - target) ** 2, axis=1)))]
        result[indices == value] = nearest
    return result


def recolor_frame(frame: bytes, width: int, height: int, palette: np.ndarray,
                  calibrated_palette_lut: np.ndarray, shadow_low_factor: float) -> tuple[np.ndarray, dict]:
    src = np.frombuffer(frame, dtype=np.uint8).reshape(height, width)
    body = (src != 0) & (src != 4)
    result = src.copy()
    if np.any(body):
        result[body] = calibrated_palette_lut[src[body]]
        result = balance_body_shadows(result, palette, shadow_low_factor)
    return result, {
        "body_pixels": int(np.count_nonzero(body)),
        "geometry_equal": bool(np.array_equal(body, (result != 0) & (result != 4))),
        "shadow_equal": bool(np.array_equal(src == 4, result == 4)),
        "transparent_equal": bool(np.array_equal(src == 0, result == 0)),
        "uniform_2x2_before": uniform_2x2(src),
        "uniform_2x2_after": uniform_2x2(result),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input-dir", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--reference", type=Path, required=True)
    parser.add_argument("--brightness", type=float, default=1.15)
    parser.add_argument("--shadow-low-factor", type=float, default=0.76)
    parser.add_argument("--calibration-current", type=Path, required=True)
    parser.add_argument("--calibration-target", type=Path, required=True)
    parser.add_argument("--actors", nargs="+", default=list(ACTORS))
    args = parser.parse_args()

    source_dir = args.input_dir.resolve()
    output_dir = args.output_dir.resolve()
    output_dir.mkdir(parents=True, exist_ok=True)
    palette = np.asarray(shore.read_palette(ROOT / "mods/cameo/bits/volcanic/volcanic.pal"))
    lut, calibration_audit = calibrated_lut(
        args.calibration_current.resolve(), args.calibration_target.resolve(), palette
    )
    audit = {
        "brightness": args.brightness,
        "shadow_low_factor": args.shadow_low_factor,
        "reference": str(args.reference.resolve()),
        "calibration_current": str(args.calibration_current.resolve()),
        "calibration_target": str(args.calibration_target.resolve()),
        "calibration": calibration_audit,
        "actors": {},
    }

    for actor in args.actors:
        src_path = source_dir / f"{actor}.vol"
        dst_path = output_dir / f"{actor}.vol"
        width, height, frames = read_shptd(src_path)
        converted = []
        frame_audits = []
        for frame in frames:
            result, frame_audit = recolor_frame(
                frame, width, height, palette, lut, args.shadow_low_factor
            )
            converted.append(bytes(result))
            frame_audits.append(frame_audit)
        write_shptd(dst_path, width, height, converted)
        audit["actors"][actor] = {
            "size": [width, height],
            "frames": len(frames),
            "source_sha256": sha256(src_path),
            "output_sha256": sha256(dst_path),
            "all_geometry_equal": all(f["geometry_equal"] for f in frame_audits),
            "all_shadow_equal": all(f["shadow_equal"] for f in frame_audits),
            "all_transparent_equal": all(f["transparent_equal"] for f in frame_audits),
            "all_uniform_2x2_before": all(f["uniform_2x2_before"] for f in frame_audits),
            "all_uniform_2x2_after": all(f["uniform_2x2_after"] for f in frame_audits),
            "frame_audits": frame_audits,
        }

    (output_dir / "recolor-audit.json").write_text(json.dumps(audit, indent=2) + "\n", encoding="utf-8")
    print(output_dir / "recolor-audit.json")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
