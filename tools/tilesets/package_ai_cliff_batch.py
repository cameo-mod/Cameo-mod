#!/usr/bin/env python
"""Package generated volcanic cliff concepts into review PNGs and preview VOLs."""

from __future__ import annotations

import argparse
import json
import shutil
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
TILE = 48
SCALE = 4


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", type=Path, required=True)
    args = parser.parse_args()

    manifest_path = resolve(args.manifest)
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    batch_root = manifest_path.parent
    palette = read_palette(resolve(Path(manifest["palette"])))
    results = []

    for item in manifest["samples"]:
        results.append(package_sample(batch_root, item, palette))

    write_candidate_sheet(batch_root / "batch-candidates.png", results)
    write_comparison_sheet(batch_root / "batch-review.png", results)
    (batch_root / "verification.json").write_text(
        json.dumps(results, indent=2), encoding="utf-8"
    )

    print((batch_root / "batch-review.png").resolve())
    print((batch_root / "batch-candidates.png").resolve())
    print(f"packaged {len(results)} samples")
    return 0


def resolve(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def read_palette(path: Path) -> list[tuple[int, int, int]]:
    data = path.read_bytes()
    if len(data) != 768:
        raise ValueError(f"expected 768-byte palette, got {len(data)}")
    return [
        tuple(data[offset + channel] * 4 for channel in range(3))
        for offset in range(0, len(data), 3)
    ]


def package_sample(
    batch_root: Path,
    item: dict[str, object],
    palette: list[tuple[int, int, int]],
) -> dict[str, object]:
    sample = str(item["sample"])
    out_dir = batch_root / sample
    out_dir.mkdir(parents=True, exist_ok=True)

    temperate_path = resolve(Path(str(item["temperate"])))
    snow_path = resolve(Path(str(item["snow"])))
    ai_path = out_dir / "ai-source.png"
    if not ai_path.exists():
        raise FileNotFoundError(f"missing generated image: {ai_path}")

    shutil.copy2(temperate_path, out_dir / "donor-temperate-x4.png")
    shutil.copy2(snow_path, out_dir / "donor-snow-x4.png")

    target = (int(item["native_width"]), int(item["native_height"]))
    x4_target = (target[0] * SCALE, target[1] * SCALE)
    source = Image.open(ai_path).convert("RGB")
    crop = center_crop_to_aspect(source, target[0] / target[1])
    crop.save(out_dir / "ai-crop.png")

    # This deliberately matches the accepted s09 workflow: sample the AI image
    # directly at native resolution, then preserve those pixels for previews.
    sampled = crop.resize(target, Image.Resampling.NEAREST)
    native, indices = quantize_to_palette(sampled, palette)
    x4 = native.resize(x4_target, Image.Resampling.NEAREST)
    native.save(out_dir / "native.png")
    x4.save(out_dir / "x4.png")

    frames = slice_frames(indices, target[0], target[1])
    asset = sample.split("-", 1)[1]
    vol_path = out_dir / f"{asset}-preview.vol"
    write_shptd(vol_path, TILE, TILE, frames)
    vol_width, vol_height, decoded = read_shptd(vol_path)

    temperate = Image.open(temperate_path).convert("RGB")
    snow = Image.open(snow_path).convert("RGB")
    write_sample_review(
        out_dir / "review.png", sample, temperate, snow, crop, x4
    )

    palette_set = set(palette)
    off_palette = sum(1 for pixel in native.getdata() if pixel not in palette_set)
    expected_frames = (target[0] // TILE) * (target[1] // TILE)
    if target[0] % TILE or target[1] % TILE:
        raise ValueError(f"{sample}: native dimensions are not tile-aligned: {target}")
    if (vol_width, vol_height) != (TILE, TILE):
        raise ValueError(f"{sample}: preview VOL dimensions are {vol_width}x{vol_height}")
    if len(decoded) != expected_frames or len(frames) != expected_frames:
        raise ValueError(
            f"{sample}: expected {expected_frames} frames, wrote {len(frames)}, read {len(decoded)}"
        )
    if off_palette:
        raise ValueError(f"{sample}: {off_palette} pixels are outside volcanic.pal")

    return {
        "sample": sample,
        "native_width": target[0],
        "native_height": target[1],
        "frames": expected_frames,
        "palette_colors_used": len(set(indices)),
        "off_palette_pixels": off_palette,
        "preview_vol": str(vol_path.resolve()),
        "review": str((out_dir / "review.png").resolve()),
        "candidate": str((out_dir / "x4.png").resolve()),
    }


def center_crop_to_aspect(image: Image.Image, target_aspect: float) -> Image.Image:
    source_aspect = image.width / image.height
    if abs(source_aspect - target_aspect) < 1e-9:
        return image.copy()
    if source_aspect > target_aspect:
        width = max(1, round(image.height * target_aspect))
        left = (image.width - width) // 2
        return image.crop((left, 0, left + width, image.height))
    height = max(1, round(image.width / target_aspect))
    top = (image.height - height) // 2
    return image.crop((0, top, image.width, top + height))


def quantize_to_palette(
    image: Image.Image,
    palette: list[tuple[int, int, int]],
) -> tuple[Image.Image, list[int]]:
    cache: dict[tuple[int, int, int], int] = {}
    indices: list[int] = []
    colors: list[tuple[int, int, int]] = []
    for pixel in image.getdata():
        index = cache.get(pixel)
        if index is None:
            index = min(
                range(len(palette)),
                key=lambda candidate: color_distance(pixel, palette[candidate]),
            )
            cache[pixel] = index
        indices.append(index)
        colors.append(palette[index])
    result = Image.new("RGB", image.size)
    result.putdata(colors)
    return result, indices


def color_distance(a: tuple[int, int, int], b: tuple[int, int, int]) -> int:
    # Slightly favor luminance fidelity; cliff form matters more than hue error.
    dr, dg, db = a[0] - b[0], a[1] - b[1], a[2] - b[2]
    return 2 * dr * dr + 4 * dg * dg + db * db


def slice_frames(indices: list[int], width: int, height: int) -> list[bytes]:
    frames = []
    for cell_y in range(height // TILE):
        for cell_x in range(width // TILE):
            frame = bytearray()
            for y in range(TILE):
                start = (cell_y * TILE + y) * width + cell_x * TILE
                frame.extend(indices[start : start + TILE])
            frames.append(bytes(frame))
    return frames


def write_sample_review(
    path: Path,
    sample: str,
    temperate: Image.Image,
    snow: Image.Image,
    ai_crop: Image.Image,
    volcanic: Image.Image,
) -> None:
    panel_size = volcanic.size
    header = 24
    sheet = Image.new("RGB", (panel_size[0] * 2, (panel_size[1] + header) * 2), (73, 86, 99))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    panels = [
        ("Temperate donor", temperate),
        ("Snow geometry reference", snow),
        ("AI full edit", ai_crop.resize(panel_size, Image.Resampling.NEAREST)),
        ("Volcanic palette + NEAREST", volcanic),
    ]
    for i, (label, panel) in enumerate(panels):
        x = (i % 2) * panel_size[0]
        y = (i // 2) * (panel_size[1] + header)
        draw.text((x + 5, y + 5), f"{sample}: {label}", fill="white", font=font)
        sheet.paste(panel.resize(panel_size, Image.Resampling.NEAREST), (x, y + header))
    sheet.save(path)


def write_candidate_sheet(path: Path, results: list[dict[str, object]]) -> None:
    columns = 5
    cell_width, cell_height, header = 384, 384, 24
    rows = (len(results) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * cell_width, rows * (cell_height + header)), (73, 86, 99))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for i, result in enumerate(results):
        image = Image.open(str(result["candidate"])).convert("RGB")
        image.thumbnail((cell_width, cell_height), Image.Resampling.NEAREST)
        x = (i % columns) * cell_width
        y = (i // columns) * (cell_height + header)
        draw.text((x + 5, y + 5), str(result["sample"]), fill="white", font=font)
        px = x + (cell_width - image.width) // 2
        py = y + header + (cell_height - image.height) // 2
        sheet.paste(image, (px, py))
    sheet.save(path)


def write_comparison_sheet(path: Path, results: list[dict[str, object]]) -> None:
    labels = ("Temperate", "Snow", "Volcanic")
    thumb = 192
    label_width, header = 92, 24
    row_height = thumb + header
    sheet = Image.new("RGB", (label_width + thumb * 3, row_height * len(results)), (73, 86, 99))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for row, result in enumerate(results):
        sample = str(result["sample"])
        folder = Path(str(result["candidate"])).parent
        paths = (
            folder / "donor-temperate-x4.png",
            folder / "donor-snow-x4.png",
            folder / "x4.png",
        )
        y = row * row_height
        draw.text((5, y + 5), sample, fill="white", font=font)
        for column, (label, image_path) in enumerate(zip(labels, paths)):
            x = label_width + column * thumb
            draw.text((x + 5, y + 5), label, fill="white", font=font)
            image = Image.open(image_path).convert("RGB")
            image.thumbnail((thumb, thumb), Image.Resampling.NEAREST)
            px = x + (thumb - image.width) // 2
            py = y + header + (thumb - image.height) // 2
            sheet.paste(image, (px, py))
    sheet.save(path)


if __name__ == "__main__":
    raise SystemExit(main())
