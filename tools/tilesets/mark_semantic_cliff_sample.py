#!/usr/bin/env python
"""Create reviewed vision masks for selected cliff samples."""

import argparse

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / ".vs/docs/volcanic-theater-previews/semantic-mask-reviews/cliffs"
SCALE = 4
GREEN = (0, 255, 0, 255)
SAMPLES = {
    "0161-s27": {
        "facing": "east-facing",
        "version": 1,
        "polygons": [
            [
                (18, 0), (75, 0), (78, 7), (82, 14), (80, 23), (86, 31),
                (85, 39), (91, 47), (91, 54), (102, 59), (105, 67),
                (114, 75), (118, 84), (127, 92), (127, 95), (68, 95),
                (65, 90), (57, 87), (52, 81), (44, 78), (43, 71),
                (36, 66), (25, 63), (17, 58), (17, 50), (23, 45),
                (18, 42), (17, 35), (22, 31), (18, 25), (18, 16),
                (22, 11), (18, 7),
            ],
            [(130, 72), (137, 73), (143, 78), (143, 84), (138, 86), (132, 83), (128, 78)],
        ],
    },
    "0143-s09": {
        "facing": "mixed ridge; dominant southwest-facing exposed slope",
        "version": 2,
        "polygons": [
            [
                (15, 0), (79, 0), (84, 7), (83, 15), (88, 22),
                (84, 30), (94, 38), (104, 42), (108, 48), (113, 53),
                (117, 60), (115, 67), (121, 73), (114, 77), (117, 87),
                (124, 94), (124, 95),
                (50, 95), (49, 89), (42, 84), (40, 76), (43, 69),
                (48, 64), (43, 58), (47, 50), (51, 45), (43, 40),
                (35, 34), (28, 33), (22, 28), (20, 21), (14, 15),
            ],
            [(14, 34), (21, 31), (28, 34), (33, 41), (31, 49), (24, 52), (14, 49)],
            [(112, 16), (117, 12), (124, 13), (132, 20), (134, 28), (130, 35), (125, 39), (119, 34), (116, 29), (110, 25)],
            [(107, 33), (112, 31), (116, 34), (115, 38), (109, 39), (106, 36)],
            [(129, 55), (136, 54), (143, 59), (143, 67), (139, 71), (132, 68), (127, 62)],
        ],
    },
}


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--sample", choices=sorted(SAMPLES), default="0161-s27")
    args = parser.parse_args()
    sample = SAMPLES[args.sample]
    stem = f"{args.sample}-x4"
    version = sample["version"]
    source_path = ROOT / f".vs/docs/volcanic-theater-previews/ra-temperate-mask-sources/cliffs/{stem}.png"
    snow_path = ROOT / f".vs/docs/volcanic-theater-previews/ra-snow-mask-sources/cliffs/{stem}.png"
    source_x4 = Image.open(source_path).convert("RGBA")
    snow_x4 = Image.open(snow_path).convert("RGBA")
    native = source_x4.resize((source_x4.width // SCALE, source_x4.height // SCALE), Image.Resampling.NEAREST)
    marked = native.copy()
    draw = ImageDraw.Draw(marked)

    for polygon in sample["polygons"]:
        draw.polygon(polygon, fill=GREEN)

    mask_x4 = marked.resize(source_x4.size, Image.Resampling.NEAREST)
    native_pixels = list(marked.get_flattened_data())
    source_native_pixels = list(native.get_flattened_data())
    preview_native = native.copy()
    preview_native.putdata([
        ((0, 190, 0, 255) if pixel == GREEN else original)
        for pixel, original in zip(native_pixels, source_native_pixels)
    ])
    preview = Image.blend(native, preview_native, 0.58).resize(source_x4.size, Image.Resampling.NEAREST)

    OUT.mkdir(parents=True, exist_ok=True)
    mask_path = OUT / f"{stem}-mask-ai-vision-v{version}.png"
    preview_path = OUT / f"{stem}-preview-ai-vision-v{version}.png"
    review_path = OUT / f"{stem}-review-ai-vision-v{version}.png"
    mask_x4.save(mask_path)
    preview.save(preview_path)

    font = ImageFont.load_default()
    header = 24
    panel_w, panel_h = source_x4.size
    sheet = Image.new("RGBA", (panel_w * 2, (panel_h + header) * 2), (73, 86, 99, 255))
    panels = [("temperate source", source_x4), ("snow geometry check", snow_x4), (f"vision mask v{version}", mask_x4), ("overlay", preview)]
    d = ImageDraw.Draw(sheet)
    for i, (title, panel) in enumerate(panels):
        x = (i % 2) * panel_w
        y = (i // 2) * (panel_h + header)
        d.text((x + 5, y + 5), title, fill="white", font=font)
        sheet.alpha_composite(panel, (x, y + header))
    sheet.save(review_path)
    print(f"Facing: {sample['facing']}")
    print(mask_path.resolve())
    print(review_path.resolve())


if __name__ == "__main__":
    main()
