#!/usr/bin/env python
"""Build full-color GIMP stamp brushes from approved Volcanic basalt trees."""

from __future__ import annotations

import argparse
import json
import shutil
import struct
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import generate_sh04_alpha_beach_prototype as shore
from shptd import read_shptd


ROOT = Path(__file__).resolve().parents[2]
ACTORS = ("t01", "t02", "t03", "t05", "t06", "t07", "t08", "t10", "t11", "t12", "t13", "t14", "t15", "t16", "t17")


def decode_first_frame(actor: str, palette: np.ndarray, source_path: Path | None = None) -> Image.Image:
    source_path = source_path or ROOT / f"mods/cameo/bits/volcanic/{actor}.vol"
    width, height, frames = read_shptd(source_path)
    indices = np.frombuffer(frames[0], dtype=np.uint8).reshape(height, width)
    if width % 2 or height % 2:
        raise ValueError(f"{actor}: production sprite is not divisible by 2: {width}x{height}")

    sampled = indices[0::2, 0::2]
    for dy, dx in ((0, 1), (1, 0), (1, 1)):
        if not np.array_equal(sampled, indices[dy::2, dx::2]):
            raise ValueError(f"{actor}: production sprite does not have strict uniform 2x2 blocks")

    rgb = palette[sampled]
    alpha = np.where(sampled == 0, 0, 255).astype(np.uint8)
    alpha[sampled == 4] = 105
    return Image.fromarray(np.dstack((rgb, alpha)), mode="RGBA")


def write_gbr(path: Path, image: Image.Image, name: str, spacing: int = 100) -> None:
    rgba = image.convert("RGBA")
    encoded_name = name.encode("utf-8") + b"\0"
    header_size = 28 + len(encoded_name)
    header = struct.pack(
        ">7I",
        header_size,
        2,
        rgba.width,
        rgba.height,
        4,
        0x47494D50,
        spacing,
    )
    path.write_bytes(header + encoded_name + rgba.tobytes())


def checkerboard(size: tuple[int, int], cell: int = 6) -> Image.Image:
    image = Image.new("RGBA", size)
    draw = ImageDraw.Draw(image)
    colors = ((174, 174, 174, 255), (220, 220, 220, 255))
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=colors[((x // cell) + (y // cell)) % 2])
    return image


def contact_sheet(images: dict[str, Image.Image]) -> Image.Image:
    scale = 4
    columns = 3
    panel = (320, 232)
    header = 48
    rows = (len(images) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * panel[0], header + rows * panel[1]), (43, 48, 53))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    draw.rectangle((0, 0, sheet.width, header), fill=(73, 86, 99))
    draw.text((12, 9), "Approved Volcanic basalt GIMP stamp brushes", fill="white", font=font)
    draw.text((12, 27), "24px-per-tile authoring density | full RGBA | bundled due-east shadow", fill=(215, 224, 230), font=font)
    for index, (actor, sprite) in enumerate(images.items()):
        col, row = index % columns, index // columns
        x, y = col * panel[0], header + row * panel[1]
        preview = checkerboard((sprite.width, sprite.height))
        preview.alpha_composite(sprite)
        preview = preview.resize((preview.width * scale, preview.height * scale), Image.Resampling.NEAREST)
        px = x + (panel[0] - preview.width) // 2
        py = y + 12 + (panel[1] - 36 - preview.height) // 2
        sheet.paste(preview.convert("RGB"), (px, py))
        draw.text((x + 12, y + panel[1] - 22), f"{actor} | {sprite.width}x{sprite.height}px brush", fill="white", font=font)
    return sheet


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out-dir", type=Path, required=True)
    parser.add_argument("--install-dir", type=Path)
    parser.add_argument(
        "--override-vol",
        action="append",
        default=[],
        metavar="ACTOR=PATH",
        help="Use a review .vol for one actor without replacing production art.",
    )
    args = parser.parse_args()

    overrides: dict[str, Path] = {}
    for value in args.override_vol:
        actor, separator, path = value.partition("=")
        if not separator or actor not in ACTORS:
            raise ValueError(f"invalid --override-vol value: {value}")
        overrides[actor] = Path(path).resolve()

    out = args.out_dir.resolve()
    brushes = out / "brushes"
    previews = out / "previews"
    brushes.mkdir(parents=True, exist_ok=True)
    previews.mkdir(parents=True, exist_ok=True)

    palette = np.asarray(shore.read_palette(ROOT / "mods/cameo/bits/volcanic/volcanic.pal"), dtype=np.uint8)
    images: dict[str, Image.Image] = {}
    manifest = {"density": "24px per tile", "lighting": "due west", "shadow": "bundled due east", "brushes": []}
    for actor in ACTORS:
        source_path = overrides.get(actor)
        sprite = decode_first_frame(actor, palette, source_path)
        images[actor] = sprite
        name = f"Volcanic Basalt {actor.upper()}"
        filename = f"volcanic-basalt-{actor}.gbr"
        write_gbr(brushes / filename, sprite, name)
        sprite.save(previews / f"{actor}-stamp.png")
        manifest["brushes"].append({
            "actor": actor,
            "file": filename,
            "size": list(sprite.size),
            "name": name,
            "source": str(source_path) if source_path else f"production:{actor}.vol",
        })

    sheet = contact_sheet(images)
    sheet.save(out / "volcanic-basalt-gimp-brushes-contact-sheet.png")
    (out / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    (out / "README.md").write_text(
        "# Volcanic Basalt GIMP Stamp Brushes\n\n"
        "These are full-color RGBA stamps made from the approved individual basalt-tree production sprites. "
        "Each brush includes its due-east shadow and is downsampled from strict 2x2 production blocks to the "
        "24px-per-tile authoring density.\n\n"
        "Use the Pencil tool at 100% size and 100% opacity. Click once per formation. Do not drag a stroke, "
        "rotate, mirror, or rescale individual stamps. Compose tc01-tc03 on a 72x48 transparent canvas and "
        "tc04-tc05 on a 96x72 canvas. Upscale the completed composition exactly 2x with nearest-neighbor.\n\n"
        "In GIMP, refresh the Brushes dock after installation. Search for `Volcanic Basalt`.\n",
        encoding="utf-8",
    )

    if args.install_dir:
        install = args.install_dir.resolve()
        install.mkdir(parents=True, exist_ok=True)
        for brush in brushes.glob("*.gbr"):
            shutil.copy2(brush, install / brush.name)
        print(f"Installed {len(images)} brushes in {install}")

    print(out / "volcanic-basalt-gimp-brushes-contact-sheet.png")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
