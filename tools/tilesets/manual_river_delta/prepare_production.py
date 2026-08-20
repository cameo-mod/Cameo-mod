#!/usr/bin/env python
"""Validate a manual river-delta handoff and prepare indexed preview art."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("project", type=Path)
    parser.add_argument("--palette", type=Path, required=True)
    args = parser.parse_args()
    project = args.project.resolve()
    manifest = json.loads((project / "project.json").read_text(encoding="utf-8"))
    tile = manifest["tile"]
    expected_size = (manifest["canvas"]["width"], manifest["canvas"]["height"])

    base = Image.open(project / manifest["inputs"]["approved_base"]).convert("RGBA")
    cutout = Image.open(project / manifest["required_exports"]["lava_cutout"]).convert("RGBA")
    manual = Image.open(project / manifest["required_exports"]["composite"]).convert("RGBA")
    for label, image in (("base", base), ("cutout", cutout), ("composite", manual)):
        if image.size != expected_size:
            raise ValueError(f"{label} is {image.size}, expected {expected_size}")

    recomposed = Image.alpha_composite(base, cutout).convert("RGB")
    manual_rgb = manual.convert("RGB")
    manual_array = np.asarray(manual_rgb, dtype=np.int16)
    recomposed_array = np.asarray(recomposed, dtype=np.int16)
    difference = np.abs(manual_array - recomposed_array)
    mismatch = np.any(difference != 0, axis=2)

    palette = read_palette(args.palette)
    indexed_preview, indices = quantize(recomposed, palette)
    indexed_image = Image.fromarray(indices, mode="P")
    flat_palette = [channel for color in palette for channel in color]
    indexed_image.putpalette(flat_palette)

    production = project / manifest["production_output_directory"]
    production.mkdir(parents=True, exist_ok=True)
    recomposed.save(production / f"production_rgb_{tile}.png")
    indexed_preview.save(production / f"production_indexed_preview_{tile}.png")
    indexed_image.save(production / f"production_indexed_{tile}.png")

    alpha = np.asarray(cutout, dtype=np.uint8)[:, :, 3]
    edge_contacts = []
    if np.any(alpha[:, 0]):
        edge_contacts.append("left")
    if np.any(alpha[:, -1]):
        edge_contacts.append("right")
    if np.any(alpha[0, :]):
        edge_contacts.append("top")
    if np.any(alpha[-1, :]):
        edge_contacts.append("bottom")
    expected_contacts = manifest.get("expected_lava_edge_contacts", [])
    unexpected_contacts = sorted(set(edge_contacts) - set(expected_contacts))
    missing_contacts = sorted(set(expected_contacts) - set(edge_contacts))

    audit = {
        "preview_only": True,
        "vol_files_written": False,
        "tile": tile,
        "canvas": list(expected_size),
        "source_xcf": manifest["working_file"],
        "cutout_visible_pixels": int(np.count_nonzero(alpha)),
        "cutout_partially_transparent_pixels": int(
            np.count_nonzero((alpha > 0) & (alpha < 255))
        ),
        "actual_lava_edge_contacts": edge_contacts,
        "expected_lava_edge_contacts": expected_contacts,
        "missing_edge_contacts": missing_contacts,
        "unexpected_edge_contacts": unexpected_contacts,
        "manual_vs_recomposed_mismatch_pixels": int(np.count_nonzero(mismatch)),
        "manual_vs_recomposed_max_channel_error": int(difference.max()),
        "manual_vs_recomposed_mean_absolute_error": round(float(difference.mean()), 4),
        "indexed_palette_colors_used": int(len(np.unique(indices))),
        "indexed_palette_path": str(args.palette.resolve()),
        "ready_for_visual_review": not missing_contacts and not unexpected_contacts,
    }
    (production / f"production_audit_{tile}.json").write_text(
        json.dumps(audit, indent=2),
        encoding="utf-8",
    )
    write_review(
        production / f"production_review_{tile}.png",
        manual_rgb,
        recomposed,
        indexed_preview,
        difference,
        tile,
    )
    print(json.dumps(audit, indent=2))
    print((production / f"production_review_{tile}.png").resolve())
    return 0


def read_palette(path: Path) -> list[tuple[int, int, int]]:
    data = path.read_bytes()
    if len(data) != 768:
        raise ValueError(f"{path}: expected 768-byte C&C palette")
    return [
        tuple(data[offset + channel] * 4 for channel in range(3))
        for offset in range(0, 768, 3)
    ]


def quantize(
    image: Image.Image,
    palette: list[tuple[int, int, int]],
) -> tuple[Image.Image, np.ndarray]:
    rgb = np.asarray(image.convert("RGB"), dtype=np.int32)
    colors = np.asarray(palette, dtype=np.int32)
    indices = np.empty(rgb.shape[:2], dtype=np.uint8)
    for y in range(rgb.shape[0]):
        row = rgb[y, :, None, :]
        delta = row - colors[None, :, :]
        distance = 2 * delta[:, :, 0] ** 2 + 4 * delta[:, :, 1] ** 2 + delta[:, :, 2] ** 2
        indices[y] = np.argmin(distance, axis=1).astype(np.uint8)
    preview = colors[indices].astype(np.uint8)
    return Image.fromarray(preview, mode="RGB"), indices


def write_review(
    path: Path,
    manual: Image.Image,
    recomposed: Image.Image,
    indexed: Image.Image,
    difference: np.ndarray,
    tile: str,
) -> None:
    diff_rgb = np.clip(difference * 6, 0, 255).astype(np.uint8)
    diff_image = Image.fromarray(diff_rgb, mode="RGB")
    scale = 4
    header = 22
    panel = (manual.width * scale, manual.height * scale)
    sheet = Image.new("RGB", (panel[0] * 2, (panel[1] + header) * 2), (73, 86, 99))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    entries = (
        ("manual GIMP composite", manual),
        ("recomposed from cutout", recomposed),
        ("Volcanic indexed preview", indexed),
        ("manual/recompose difference x6", diff_image),
    )
    for index, (label, image) in enumerate(entries):
        x = index % 2 * panel[0]
        y = index // 2 * (panel[1] + header)
        draw.text((x + 4, y + 5), f"{tile}: {label}", fill="white", font=font)
        sheet.paste(
            image.resize(panel, Image.Resampling.NEAREST),
            (x, y + header),
        )
    sheet.save(path)


if __name__ == "__main__":
    raise SystemExit(main())
