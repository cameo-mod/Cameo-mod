#!/usr/bin/env python
"""Generate the first-pass VOLCANIC theater scaffold and procedural tile art."""

from __future__ import annotations

import argparse
import hashlib
import math
import re
import sys
from pathlib import Path
from random import Random

from shptd import read_shptd, write_pal, write_shptd


ROOT = Path(__file__).resolve().parents[2]
BARREN_TILESET = ROOT / "mods/cameo/tilesets/barren.yaml"
BARREN_BITS = ROOT / "mods/cameo/bits/barren"
BARREN_PAL = BARREN_BITS / "barren.pal"
VOLCANIC_TILESET = ROOT / "mods/cameo/tilesets/volcanic.yaml"
VOLCANIC_BITS = ROOT / "mods/cameo/bits/volcanic"
VOLCANIC_PAL = VOLCANIC_BITS / "volcanic.pal"
PREVIEW_DIR = ROOT / ".vs/docs/volcanic-theater-previews"
PREVIEW = PREVIEW_DIR / "mixed.png"
TILE = 48
CLEAR_BASE_SEED = 0xC1EA1200
REMAP_FALLBACKS: list[tuple[str, str]] = []


TERRAIN_COLORS = {
    "Beach": "A36D32",
    "Clear": "393236",
    "ClearTemperat": "284428",
    "Concrete": "514849",
    "Ford": "C17832",
    "River": "E65518",
    "Road": "4A3A34",
    "Rock": "734038",
    "Rough": "4B3837",
    "Tree": "20181A",
    "Wall": "7D6D60",
    "Water": "F05B18",
}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--skip-tileset", action="store_true", help="Only regenerate art from the existing volcanic.yaml")
    parser.add_argument("--no-preview", action="store_true", help="Skip optional PNG preview output")
    args = parser.parse_args()

    VOLCANIC_BITS.mkdir(parents=True, exist_ok=True)

    if not args.skip_tileset:
        write_tileset_yaml()

    templates = parse_templates(VOLCANIC_TILESET)
    source_palette = read_pal(BARREN_PAL)
    palette = build_palette()
    write_pal(VOLCANIC_PAL, palette)

    generated_templates = {image: info for image, info in templates.items() if image.endswith(".vol")}
    for image, info in sorted(generated_templates.items()):
        width, height, frames = make_frames(image, info, source_palette)
        write_shptd(VOLCANIC_BITS / image, width, height, frames)

    verify_assets(generated_templates)

    if not args.no_preview:
        write_previews(palette, generated_templates)

    print(f"Wrote {VOLCANIC_TILESET.relative_to(ROOT)}")
    print(f"Wrote {len(generated_templates)} tile images and volcanic.pal under {VOLCANIC_BITS.relative_to(ROOT)}")
    if REMAP_FALLBACKS:
        print(f"Reference remap fallbacks: {len(REMAP_FALLBACKS)}")
        for image, reason in REMAP_FALLBACKS[:12]:
            print(f"  {image}: {reason}")
        if len(REMAP_FALLBACKS) > 12:
            print(f"  ... {len(REMAP_FALLBACKS) - 12} more")
    if not args.no_preview and PREVIEW.exists():
        print(f"Wrote {PREVIEW.relative_to(ROOT)}")

    return 0


def write_tileset_yaml() -> None:
    text = BARREN_TILESET.read_text()
    text = text.replace("Name: Barren (wasteland)", "Name: Volcanic (prototype)")
    text = text.replace("Id: BARREN", "Id: VOLCANIC")
    text = text.replace(".bar", ".vol")
    text = text.replace("Palette: barren-barren", "Palette: volcanic")

    for terrain, color in TERRAIN_COLORS.items():
        pattern = rf"(TerrainType@{re.escape(terrain)}:\n(?:\t\t.*\n)*?\t\tColor: )[0-9A-Fa-f]+"
        text = re.sub(pattern, rf"\g<1>{color}", text)

    VOLCANIC_TILESET.write_text(text, newline="\n")


def parse_templates(path: Path) -> dict[str, dict[str, object]]:
    templates: dict[str, dict[str, object]] = {}
    image: str | None = None
    tile_types: dict[int, str] = {}
    categories: set[str] = set()
    width = 0
    height = 0
    frame_count = 0
    max_tile = -1

    def flush() -> None:
        nonlocal image, tile_types, categories, width, height, frame_count, max_tile
        if image:
            info = templates.setdefault(image, {"count": 0, "width": 0, "height": 0, "tiles": {}, "types": set(), "categories": set()})
            info["count"] = max(int(info["count"]), frame_count, max_tile + 1)
            info["width"] = max(int(info["width"]), width)
            info["height"] = max(int(info["height"]), height)
            info["tiles"].update(tile_types)
            info["types"].update(tile_types.values())
            info["categories"].update(categories)
        image = None
        tile_types = {}
        categories = set()
        width = 0
        height = 0
        frame_count = 0
        max_tile = -1

    for raw in path.read_text().splitlines():
        if raw.startswith("\tTemplate@"):
            flush()
            continue

        stripped = raw.strip()
        if stripped.startswith("Images:"):
            image = stripped.split(":", 1)[1].strip()
        elif image and stripped.startswith("Size:"):
            size = stripped.split(":", 1)[1].strip()
            width, height = [int(part) for part in size.split(",", 1)]
            frame_count = width * height
        elif image and stripped.startswith("Categories:"):
            categories.update(part.strip() for part in stripped.split(":", 1)[1].split(",") if part.strip())
        elif image and re.match(r"^\d+:", stripped):
            key, value = stripped.split(":", 1)
            max_tile = max(max_tile, int(key))
            tile_type = value.strip()
            if tile_type:
                tile_types[int(key)] = tile_type

    flush()
    return templates


def build_palette() -> list[tuple[int, int, int]]:
    colors = [(0, 0, 0)] * 256
    colors[0] = (0, 0, 0)
    colors[1] = (14, 10, 10)
    colors[2] = (22, 15, 14)
    colors[3] = (32, 22, 20)
    colors[4] = (9, 7, 7)

    fill_gradient(colors, 10, 29, (32, 30, 32), (82, 71, 66))
    fill_gradient(colors, 30, 49, (47, 42, 43), (112, 88, 76))
    fill_gradient(colors, 50, 69, (70, 38, 35), (138, 69, 49))
    fill_gradient(colors, 70, 83, (96, 24, 14), (232, 74, 22))
    fill_gradient(colors, 84, 93, (242, 92, 22), (255, 178, 48))
    fill_gradient(colors, 94, 99, (255, 195, 55), (255, 238, 116))
    fill_gradient(colors, 100, 119, (36, 30, 32), (116, 55, 48))
    fill_gradient(colors, 120, 129, (42, 34, 33), (96, 75, 65))
    fill_gradient(colors, 130, 145, (94, 65, 36), (190, 126, 44))
    fill_gradient(colors, 146, 159, (50, 28, 30), (132, 58, 42))
    fill_gradient(colors, 160, 171, (68, 49, 39), (184, 138, 68))
    fill_gradient(colors, 172, 181, (66, 32, 80), (164, 84, 192))
    fill_gradient(colors, 182, 191, (82, 93, 95), (192, 207, 200))

    for i in range(192, 256):
        t = (i - 192) / 63
        colors[i] = (
            int(26 + 100 * t),
            int(20 + 52 * t),
            int(20 + 42 * t),
        )

    return colors


def read_pal(path: Path) -> list[tuple[int, int, int]]:
    data = path.read_bytes()
    if len(data) != 256 * 3:
        raise ValueError(f"{path} is not a 768-byte palette")
    return [tuple(channel * 4 for channel in data[i * 3 : i * 3 + 3]) for i in range(256)]


def fill_gradient(colors: list[tuple[int, int, int]], start: int, end: int, a: tuple[int, int, int], b: tuple[int, int, int]) -> None:
    span = max(1, end - start)
    for i in range(start, end + 1):
        t = (i - start) / span
        colors[i] = tuple(int(a[c] + (b[c] - a[c]) * t) for c in range(3))


def make_frames(image: str, info: dict[str, object], source_palette: list[tuple[int, int, int]]) -> tuple[int, int, list[bytes]]:
    source = BARREN_BITS / image.replace(".vol", ".bar")
    if should_remap_from_barren(image, info) and source.exists():
        try:
            source_width, source_height, source_frames = read_shptd(source)
            frames = [
                remap_reference_frame(image, frame, info, source_frames, source_width, source_height, source_palette)
                for frame in range(int(info["count"]))
            ]
            return source_width, source_height, frames
        except Exception as exc:
            REMAP_FALLBACKS.append((image, str(exc)))
            pass

    return TILE, TILE, [make_frame(image, frame, info) for frame in range(int(info["count"]))]


def should_remap_from_barren(image: str, info: dict[str, object]) -> bool:
    stem = Path(image).stem.lower()
    types = {str(t) for t in info["types"]}
    categories = {str(t) for t in info.get("categories", set())}
    if stem == "clear1":
        return False
    if bool({"Beach", "Cliffs", "Water Cliffs"} & categories):
        return True
    if bool({"Water", "River", "Beach", "Ford", "Rock", "Rough"} & types):
        return True
    return stem.startswith(("w", "sh", "s", "wc", "cliff", "falls", "fjord", "ford", "f"))


def remap_reference_frame(
    image: str,
    frame_index: int,
    info: dict[str, object],
    source_frames: list[bytes],
    source_width: int,
    source_height: int,
    source_palette: list[tuple[int, int, int]],
) -> bytes:
    tiles = info["tiles"]
    tile_type = tiles.get(frame_index) if isinstance(tiles, dict) else None
    if tile_type is None or frame_index >= len(source_frames):
        return bytes(source_width * source_height)

    stem = Path(image).stem.lower()
    types = {str(t) for t in info["types"]}
    style = classify(stem, types, tile_type)
    source_frame = source_frames[frame_index]
    seed = int.from_bytes(hashlib.blake2s(f"{image}:{frame_index}:remap".encode(), digest_size=8).digest(), "little")
    bank_contact = lava_contact_info(frame_index, info) if style == "lava_bank" else None
    data = bytearray(source_width * source_height)

    for y in range(source_height):
        for x in range(source_width):
            offset = y * source_width + x
            src = source_frame[offset]
            rgb = source_palette[src]
            n = noise(x, y, seed)
            if style == "open_lava":
                data[offset] = remap_lava_index(src, rgb, n)
            elif style == "lava_bank":
                local_x = x * TILE / max(1, source_width)
                local_y = y * TILE / max(1, source_height)
                distance = contact_distance(int(local_x), int(local_y), x, y, seed, bank_contact)
                data[offset] = remap_bank_index(src, rgb, n, distance)
            elif style == "cliff":
                data[offset] = remap_cliff_index(src, rgb, n, x, y, seed)
            else:
                data[offset] = remap_ground_index(src, rgb, n)

    return bytes(data)


def is_barren_water_index(index: int) -> bool:
    return 92 <= index <= 104 or 180 <= index <= 191


def is_barren_stone_index(index: int) -> bool:
    return 128 <= index <= 143 or 249 <= index <= 254


def remap_lava_index(src: int, rgb: tuple[int, int, int], n: int) -> int:
    if is_barren_stone_index(src):
        return remap_stone_index(rgb, n)
    if src == 0:
        return 50 + n % 5
    if not is_barren_water_index(src):
        return remap_ground_index(src, rgb, n)
    luma = luminance(rgb)
    if luma > 126:
        return 94 + n % 5
    if luma > 96:
        return 84 + n % 8
    if luma > 64:
        return 70 + n % 10
    return 50 + n % 12


def remap_bank_index(src: int, rgb: tuple[int, int, int], n: int, contact_distance_value: float | None) -> int:
    if is_barren_stone_index(src):
        return remap_stone_index(rgb, n)
    if contact_distance_value is not None and contact_distance_value <= 1.4:
        return 84 + n % 8
    if contact_distance_value is not None and contact_distance_value <= 3.4 and n > 120:
        return 70 + n % 8
    if contact_distance_value is not None and contact_distance_value <= 6.2 and n > 226:
        return 50 + n % 8
    return remap_ground_index(src, rgb, n, hot_flecks=contact_distance_value is not None and contact_distance_value <= 7.0)


def remap_cliff_index(src: int, rgb: tuple[int, int, int], n: int, x: int, y: int, seed: int) -> int:
    if src == 0:
        return 1 + n % 3
    if is_barren_stone_index(src):
        return remap_stone_index(rgb, n)
    luma = luminance(rgb)
    strata = int(2 * math.sin((x + seed % 29) / 9.0) + math.cos((y + seed % 37) / 11.0))
    if src >= 249 or luma > 132:
        return clamp(116 + n % 4 + strata, 112, 119)
    if luma > 94:
        return clamp(106 + n % 5 + strata, 102, 114)
    if luma > 60:
        return clamp(100 + n % 5 + strata, 100, 108)
    return 2 + n % 3


def remap_ground_index(src: int, rgb: tuple[int, int, int], n: int, hot_flecks: bool = False) -> int:
    if is_barren_stone_index(src):
        return remap_stone_index(rgb, n)
    if hot_flecks and n > 252:
        return 70 + n % 5
    if src == 0:
        return 1 + n % 3
    luma = luminance(rgb)
    if src >= 249 or luma > 132:
        return 30 + n % 10
    if luma > 92:
        return 20 + n % 8
    if luma > 58:
        return 12 + n % 8
    return 2 + n % 4


def remap_stone_index(rgb: tuple[int, int, int], n: int) -> int:
    luma = luminance(rgb)
    if luma > 150:
        return 188 + n % 4
    if luma > 105:
        return 184 + n % 5
    if luma > 70:
        return 180 + n % 6
    return 100 + n % 7


def luminance(rgb: tuple[int, int, int]) -> float:
    return 0.299 * rgb[0] + 0.587 * rgb[1] + 0.114 * rgb[2]


def make_frame(image: str, frame_index: int, info: dict[str, object]) -> bytes:
    stem = Path(image).stem.lower()
    tiles = info["tiles"]
    types = {str(t) for t in info["types"]}
    tile_type = tiles.get(frame_index) if isinstance(tiles, dict) else None
    style = classify(stem, types, tile_type)
    image_seed = int.from_bytes(hashlib.blake2s(image.encode(), digest_size=8).digest(), "little")
    frame_seed = int.from_bytes(hashlib.blake2s(f"{image}:{frame_index}".encode(), digest_size=8).digest(), "little")
    rng = Random(frame_seed)
    data = bytearray(TILE * TILE)
    bank_contact = lava_contact_info(frame_index, info) if style == "lava_bank" else None
    lava_contact = ground_contact_info(frame_index, info) if style == "open_lava" else None
    is_base_clear = stem == "clear1"
    width = max(1, int(info.get("width") or 1))
    frame_x = frame_index % width
    frame_y = frame_index // width

    if style == "empty":
        return bytes(data)

    for y in range(TILE):
        for x in range(TILE):
            idx = y * TILE + x
            gx = frame_x * TILE + x
            gy = frame_y * TILE + y
            n = noise(gx, gy, image_seed)
            if style == "open_lava":
                data[idx] = open_lava_index(gx, gy, image_seed, n, lava_contact, x, y)
            elif style == "lava_bank":
                data[idx] = lava_bank_index(x, y, gx, gy, image_seed, n, bank_contact)
            elif style == "cliff":
                data[idx] = cliff_index(gx, gy, image_seed, n)
            elif style == "road":
                data[idx] = road_index(x, y, n)
            elif style == "resource":
                data[idx] = resource_index(x, y, n, stem)
            elif style == "debris":
                data[idx] = debris_index(x, y, n)
            elif is_base_clear or style == "clear":
                clear_n = noise(x, y, CLEAR_BASE_SEED)
                data[idx] = base_clear_index(x, y, CLEAR_BASE_SEED, clear_n)
            else:
                data[idx] = clear_index(gx, gy, image_seed, n)

    if style in {"debris", "road", "resource"}:
        add_cracks(data, rng, style)
    if style == "debris":
        add_hot_vents(data, rng, count=1)
    if style == "resource":
        add_resource_specks(data, rng, stem)

    return bytes(data)


def lava_contact_info(frame_index: int, info: dict[str, object]) -> dict[str, object]:
    tiles = info["tiles"]
    if not isinstance(tiles, dict):
        return {"mask": 0, "points": []}

    width = int(info.get("width") or 0)
    height = int(info.get("height") or 0)
    if width <= 0 or height <= 0:
        return {"mask": 0, "points": []}

    cx = frame_index % width
    cy = frame_index // width
    mask = 0
    points = []
    for bit, dx, dy in ((1, -1, 0), (2, 0, -1), (4, 1, 0), (8, 0, 1)):
        nx = cx + dx
        ny = cy + dy
        if 0 <= nx < width and 0 <= ny < height:
            neighbor = tiles.get(ny * width + nx)
            if neighbor in {"Water", "River"}:
                mask |= bit
                if dx < 0:
                    points.append((0.0, TILE / 2))
                elif dx > 0:
                    points.append((TILE - 1.0, TILE / 2))
                elif dy < 0:
                    points.append((TILE / 2, 0.0))
                elif dy > 0:
                    points.append((TILE / 2, TILE - 1.0))

    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if dx == 0 and dy == 0:
                continue

            nx = cx + dx
            ny = cy + dy
            if not (0 <= nx < width and 0 <= ny < height):
                continue

            if tiles.get(ny * width + nx) not in {"Water", "River"}:
                continue

            px = (dx + 1) * (TILE - 1) / 2
            py = (dy + 1) * (TILE - 1) / 2
            points.append((px, py))

    return {"mask": mask, "points": points}


def ground_contact_info(frame_index: int, info: dict[str, object]) -> dict[str, object]:
    tiles = info["tiles"]
    if not isinstance(tiles, dict):
        return {"mask": 0, "points": []}

    width = int(info.get("width") or 0)
    height = int(info.get("height") or 0)
    if width <= 0 or height <= 0:
        return {"mask": 0, "points": []}

    cx = frame_index % width
    cy = frame_index // width
    mask = 0
    points = []
    ground_types = {"Beach", "Ford", "Clear", "Rock", "Rough"}
    for bit, dx, dy in ((1, -1, 0), (2, 0, -1), (4, 1, 0), (8, 0, 1)):
        nx = cx + dx
        ny = cy + dy
        if 0 <= nx < width and 0 <= ny < height and tiles.get(ny * width + nx) in ground_types:
            mask |= bit
            if dx < 0:
                points.append((0.0, TILE / 2))
            elif dx > 0:
                points.append((TILE - 1.0, TILE / 2))
            elif dy < 0:
                points.append((TILE / 2, 0.0))
            elif dy > 0:
                points.append((TILE / 2, TILE - 1.0))

    return {"mask": mask, "points": points}


def classify(stem: str, types: set[str], tile_type: object) -> str:
    if tile_type is None:
        return "empty"

    tile = str(tile_type)
    if tile in {"Water", "River"}:
        return "open_lava"
    if tile in {"Beach", "Ford"}:
        return "lava_bank"
    if tile in {"Rock", "Rough"}:
        return "cliff"
    if tile in {"Road", "Bridge"}:
        return "road"
    if tile in {"Ore", "Gems", "Tiberium", "BlueTiberium", "RedTiberium", "GoldTiberium"}:
        return "resource"
    if tile in {"Clear", "ClearTemperat", "Concrete", "Tree", "Wall"}:
        return "clear"

    if stem.startswith(("w", "rw", "falls", "fjord", "ford")):
        return "open_lava"
    if "Beach" in types or stem.startswith(("b", "sh")):
        return "lava_bank"
    if "Road" in types or stem.startswith(("rv", "road", "br")):
        return "road"
    if "Rock" in types or "Rough" in types or stem.startswith(("cliff", "hw", "hill", "rf", "rc")):
        return "cliff"
    if any(t in types for t in ("Ore", "Gems", "Tiberium", "BlueTiberium", "RedTiberium", "GoldTiberium")):
        return "resource"
    if stem.startswith(("d", "cr", "rock", "deca", "decb", "decc", "decd", "dece", "decf", "decg", "dech")):
        return "debris"
    return "clear"


def noise(x: int, y: int, seed: int) -> int:
    v = (x * 374761393 + y * 668265263 + seed * 1442695040888963407) & 0xFFFFFFFF
    v ^= v >> 13
    v = (v * 1274126177) & 0xFFFFFFFF
    return (v ^ (v >> 16)) & 0xFF


def plate_edge(x: int, y: int, cell: int, jitter: int, seed: int) -> float:
    gx = math.floor(x / cell)
    gy = math.floor(y / cell)
    nearest = [10_000.0, 10_000.0]

    for cy in range(gy - 1, gy + 2):
        for cx in range(gx - 1, gx + 2):
            h = noise(cx & 0xFF, cy & 0xFF, seed)
            hx = noise((cx + 37) & 0xFF, (cy + 91) & 0xFF, seed ^ 0xA53A)
            px = cx * cell + cell / 2 + ((h / 255.0) - 0.5) * jitter
            py = cy * cell + cell / 2 + ((hx / 255.0) - 0.5) * jitter
            distance = math.hypot(x - px, y - py)
            if distance < nearest[0]:
                nearest[1] = nearest[0]
                nearest[0] = distance
            elif distance < nearest[1]:
                nearest[1] = distance

    return nearest[1] - nearest[0]


def warped_plate_edge(x: int, y: int, cell: int, jitter: int, seed: int, warp: float) -> float:
    wx = x + math.sin((y + (seed & 31)) / 11.0) * warp + math.sin((x + y + (seed & 63)) / 23.0) * warp * 0.7
    wy = y + math.cos((x + ((seed >> 5) & 31)) / 13.0) * warp + math.sin((x - y + ((seed >> 11) & 63)) / 19.0) * warp * 0.7
    return plate_edge(wx, wy, cell, jitter, seed)


def tileable_noise(x: int, y: int, seed: int) -> int:
    period = TILE - 1
    sx = math.sin(2 * math.pi * x / period)
    cx = math.cos(2 * math.pi * x / period)
    sy = math.sin(2 * math.pi * y / period)
    cy = math.cos(2 * math.pi * y / period)
    a = math.sin(3.1 * sx + 2.7 * cy + (seed & 255) * 0.013)
    b = math.cos(2.3 * cx - 3.4 * sy + ((seed >> 8) & 255) * 0.017)
    c = math.sin(4.0 * (sx + sy) + 1.6 * (cx - cy) + ((seed >> 16) & 255) * 0.011)
    return clamp(int(128 + 35 * a + 28 * b + 18 * c), 0, 255)


def base_clear_index(x: int, y: int, seed: int, n: int) -> int:
    tn = tileable_noise(x, y, seed)
    ash = math.sin(2 * math.pi * (x + y) / (TILE - 1) + (seed & 31) * 0.1)
    if tn > 220 and ash > 0.55:
        return 30 + n % 2
    if tn < 60 and n > 236:
        return 2
    return clamp(11 + (tn // 64) + (n % 2), 10, 16)


def clear_index(x: int, y: int, seed: int, n: int) -> int:
    edge = warped_plate_edge(x, y, 27, 18, seed ^ 0xC1EA12, 3.5)
    if edge < 1.05:
        return 2 + n % 3

    ash = math.sin((x + 8) / 17.0) + math.cos((y - 3) / 19.0)
    if ash > 1.45 and n > 224:
        return clamp(30 + n % 7, 30, 39)

    wave = int(2 * math.sin((x + y) / 23.0) + math.sin((x - y) / 29.0))
    return clamp(10 + n % 8 + wave, 10, 22)


def contact_distance(x: int, y: int, gx: int, gy: int, seed: int, contact: dict[str, object] | None) -> float | None:
    if not contact:
        return None

    mask = int(contact["mask"])
    points = contact["points"]
    distances = []
    if mask & 1:
        distances.append(x + 1.4 * math.sin((gy + seed % 31) / 21.0))
    if mask & 2:
        distances.append(y + 1.4 * math.sin((gx + seed % 31) / 21.0))
    if mask & 4:
        distances.append((TILE - 1 - x) + 1.4 * math.sin((gy + seed % 31) / 21.0))
    if mask & 8:
        distances.append((TILE - 1 - y) + 1.4 * math.sin((gx + seed % 31) / 21.0))

    if not distances and points:
        distances = [math.hypot(x - px, y - py) - 1.5 for px, py in points]

    return min(distances) if distances else None


def open_lava_index(x: int, y: int, seed: int, n: int, contact: dict[str, object] | None = None, local_x: int | None = None, local_y: int | None = None) -> int:
    if local_x is not None and local_y is not None:
        distance = contact_distance(local_x, local_y, x, y, seed, contact)
        if distance is not None:
            if distance < 2.25:
                return 4 if n < 176 else 50 + n % 8
            if distance < 5.0 and n < 224:
                return 50 + n % 11
            if distance < 8.0 and n < 160:
                return 70 + n % 8

    edge = warped_plate_edge(x, y, 18, 16, seed, 4.75)
    glow = 1.2 * math.sin((x + seed % 17) / 13.0) + math.cos((y - seed % 11) / 11.0)

    if edge < 0.28:
        return 94 + n % 6
    if edge < 0.85:
        return 84 + n % 8
    if edge < 1.55 or (glow > 1.95 and n > 210):
        return 70 + n % 10

    return clamp(50 + n % 11, 50, 64)


def lava_bank_index(x: int, y: int, gx: int, gy: int, seed: int, n: int, contact: dict[str, object] | None) -> int:
    contact = contact or {"mask": 0, "points": []}
    mask = int(contact["mask"])
    points = contact["points"]
    distance = contact_distance(x, y, gx, gy, seed, {"mask": mask, "points": points})
    if distance is None:
        clear_n = noise(x, y, CLEAR_BASE_SEED)
        if n > 253:
            return 70 + n % 5
        return base_clear_index(x, y, CLEAR_BASE_SEED, clear_n)

    if distance < 1.35:
        return 94 + n % 5
    if distance < 3.25:
        return 84 + n % 8
    if distance < 5.5:
        return 70 + n % 8
    if distance < 8.5 and n > 196:
        return 50 + n % 8

    clear_n = noise(x, y, CLEAR_BASE_SEED)
    if n > 253:
        return 70 + n % 5
    return base_clear_index(x, y, CLEAR_BASE_SEED, clear_n)


def cliff_index(x: int, y: int, seed: int, n: int) -> int:
    edge = warped_plate_edge(x, y, 22, 15, seed ^ 0xC11FF, 3.25)
    if edge < 0.95:
        return 2 + n % 3
    shade = int(3 * math.sin((x + seed % 41) / 18.0) + 2 * math.cos((y + seed % 37) / 21.0))
    return clamp(102 + shade + n % 5, 100, 114)


def road_index(x: int, y: int, n: int) -> int:
    groove = 2 if abs((x - y) % 19) < 2 else 0
    return clamp(120 + n % 7 + groove, 120, 129)


def resource_index(x: int, y: int, n: int, stem: str) -> int:
    if "gem" in stem or "blue" in stem:
        base = 172
        return base + n % 10
    if "gold" in stem:
        base = 94
        return base + n % 6
    return 160 + n % 12


def debris_index(x: int, y: int, n: int) -> int:
    chip = 6 if (x * 5 + y * 3 + n) % 29 < 4 else 0
    return clamp(30 + n % 12 + chip, 30, 49)


def add_cracks(data: bytearray, rng: Random, style: str) -> None:
    if style in {"open_lava", "empty"}:
        return

    count = 1 if style == "clear" else 3
    for _ in range(count):
        x = rng.randrange(TILE)
        y = rng.randrange(TILE)
        length = rng.randrange(8, 22)
        dx = rng.choice((-1, 0, 1))
        for _ in range(length):
            if 0 <= x < TILE and 0 <= y < TILE:
                data[y * TILE + x] = rng.choice((2, 3, 4, 70 if style != "clear" else 3))
            x += dx
            y += rng.choice((0, 1))


def add_hot_vents(data: bytearray, rng: Random, count: int) -> None:
    for _ in range(count):
        cx = rng.randrange(8, TILE - 8)
        cy = rng.randrange(8, TILE - 8)
        radius = rng.randrange(2, 5)
        for y in range(cy - radius, cy + radius + 1):
            for x in range(cx - radius, cx + radius + 1):
                if 0 <= x < TILE and 0 <= y < TILE and (x - cx) ** 2 + (y - cy) ** 2 <= radius ** 2:
                    data[y * TILE + x] = rng.randrange(84, 95)


def add_resource_specks(data: bytearray, rng: Random, stem: str) -> None:
    bright = range(172, 182) if "gem" in stem or "blue" in stem else range(94, 100)
    for _ in range(90):
        x = rng.randrange(TILE)
        y = rng.randrange(TILE)
        data[y * TILE + x] = rng.choice(tuple(bright))


def clamp(value: int, low: int, high: int) -> int:
    return max(low, min(high, value))


def verify_assets(templates: dict[str, dict[str, object]]) -> None:
    for image, info in templates.items():
        width, height, frames = read_shptd(VOLCANIC_BITS / image)
        if width <= 0 or height <= 0:
            raise ValueError(f"{image} has unexpected size {width}x{height}")
        if len(frames) < int(info["count"]):
            raise ValueError(f"{image} has {len(frames)} frames, expected at least {info['count']}")


def write_previews(palette: list[tuple[int, int, int]], templates: dict[str, dict[str, object]]) -> None:
    try:
        from PIL import Image
    except ImportError:
        return

    mixed = ["clear1.vol", "w1.vol", "sh01.vol", "cliffsl1.vol", "br1a.vol", "d01.vol", "rf01.vol", "b1.vol"]
    write_preview_sheet(PREVIEW, mixed, palette, templates=templates)

    families = {
        "clear": (lambda image, info: image.startswith("clear") and "Clear" in info["types"], {"clear"}),
        "open-lava": (lambda image, info: bool({"Water", "River"} & info["types"]), {"open_lava"}),
        "lava-bank": (lambda image, info: "Beach" in info.get("categories", set()) or bool({"Beach", "Ford"} & info["types"]), {"lava_bank"}),
        "cliff-rough": (lambda image, info: bool({"Cliffs", "Water Cliffs"} & info.get("categories", set())) or bool({"Rock", "Rough"} & info["types"]) or image.startswith(("cliff", "rf", "rc", "hw")), {"cliff"}),
        "road-debris": (lambda image, info: image.startswith(("br", "rv", "d", "cr", "dec")), {"road", "debris"}),
    }

    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    for name, (predicate, preferred_styles) in families.items():
        samples = [image for image, info in sorted(templates.items()) if predicate(Path(image).stem.lower(), info)]
        write_preview_sheet(PREVIEW_DIR / f"{name}.png", samples[:32], palette, cols=8, templates=templates, preferred_styles=preferred_styles)

    shore_samples = [
        image
        for image, info in templates.items()
        if "Beach" in info.get("categories", set()) or (image.startswith("sh") and bool({"Beach", "Ford"} & info["types"]))
    ]
    write_template_stack_preview(PREVIEW_DIR / "lava-bank-editor-order.png", shore_samples, palette, templates)
    write_reference_comparison_preview(PREVIEW_DIR / "lava-bank-barren-vs-volcanic.png", shore_samples, palette, templates)

    cliff_samples = [
        image
        for image, info in templates.items()
        if bool({"Cliffs", "Water Cliffs"} & info.get("categories", set())) or bool({"Rock", "Rough"} & info["types"]) or image.startswith(("cliff", "rf", "rc", "hw"))
    ]
    write_reference_comparison_preview(PREVIEW_DIR / "cliff-barren-vs-volcanic.png", cliff_samples[:80], palette, templates)
    write_repeat_preview(PREVIEW_DIR / "repeat-clear1-frame0.png", "clear1.vol", 0, palette)


def write_reference_comparison_preview(path: Path, samples: list[str], palette: list[tuple[int, int, int]], templates: dict[str, dict[str, object]]) -> None:
    from PIL import Image

    barren_palette = read_pal(BARREN_PAL)
    rows = []
    for sample in samples:
        info = templates.get(sample)
        source = BARREN_BITS / sample.replace(".vol", ".bar")
        target = VOLCANIC_BITS / sample
        if not info or not source.exists() or not target.exists():
            continue

        try:
            left = compose_template_image(source, barren_palette, info)
            right = compose_template_image(target, palette, info)
        except Exception:
            continue

        height = max(left.height, right.height)
        width = left.width + 12 + right.width
        row = Image.new("RGB", (width, height), (73, 86, 99))
        row.paste(left, (0, 0))
        row.paste(right, (left.width + 12, 0))
        rows.append(row)

    if not rows:
        return

    gutter = 10
    sheet_width = max(row.width for row in rows)
    sheet_height = sum(row.height for row in rows) + gutter * (len(rows) - 1)
    sheet = Image.new("RGB", (sheet_width, sheet_height), (73, 86, 99))
    y = 0
    for row in rows:
        sheet.paste(row, (0, y))
        y += row.height + gutter

    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)
    half = sheet.resize((max(1, sheet.width // 2), max(1, sheet.height // 2)), Image.Resampling.BOX)
    half.save(path.with_name(f"{path.stem}-half{path.suffix}"))


def compose_template_image(path: Path, palette: list[tuple[int, int, int]], info: dict[str, object]):
    from PIL import Image

    template_width = int(info.get("width") or 1)
    template_height = int(info.get("height") or 1)
    frame_width, frame_height, frames = read_shptd(path)
    tiles = info["tiles"]
    image = Image.new("RGB", (template_width * TILE, template_height * TILE), (73, 86, 99))

    for i, frame in enumerate(frames):
        if not isinstance(tiles, dict) or i not in tiles or not any(pixel != 0 for pixel in frame):
            continue

        frame_image = Image.new("RGB", (frame_width, frame_height))
        frame_image.putdata([palette[index] for index in frame])
        image.paste(frame_image, ((i % template_width) * TILE, (i // template_width) * TILE))

    return image


def write_template_stack_preview(path: Path, samples: list[str], palette: list[tuple[int, int, int]], templates: dict[str, dict[str, object]]) -> None:
    from PIL import Image

    composed = []
    for sample in samples:
        sample_path = VOLCANIC_BITS / sample
        info = templates.get(sample)
        if not sample_path.exists() or not info:
            continue

        composed.append(compose_template_image(sample_path, palette, info))

    if not composed:
        return

    gutter = 10
    sheet_width = max(tile.width for tile in composed)
    sheet_height = sum(tile.height for tile in composed) + gutter * (len(composed) - 1)
    sheet = Image.new("RGB", (sheet_width, sheet_height), (73, 86, 99))
    y = 0
    for tile in composed:
        sheet.paste(tile, (0, y))
        y += tile.height + gutter

    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)
    half = sheet.resize((max(1, sheet.width // 2), max(1, sheet.height // 2)), Image.Resampling.BOX)
    half.save(path.with_name(f"{path.stem}-half{path.suffix}"))


def write_repeat_preview(path: Path, sample: str, frame_index: int, palette: list[tuple[int, int, int]]) -> None:
    from PIL import Image

    sample_path = VOLCANIC_BITS / sample
    if not sample_path.exists():
        return

    _, _, frames = read_shptd(sample_path)
    if frame_index >= len(frames):
        return

    tile = Image.new("RGB", (TILE, TILE))
    tile.putdata([palette[index] for index in frames[frame_index]])

    cols = 16
    rows = 10
    sheet = Image.new("RGB", (cols * TILE, rows * TILE))
    for y in range(rows):
        for x in range(cols):
            sheet.paste(tile, (x * TILE, y * TILE))

    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)
    half = sheet.resize((max(1, sheet.width // 2), max(1, sheet.height // 2)), Image.Resampling.BOX)
    half.save(path.with_name(f"{path.stem}-half{path.suffix}"))


def write_preview_sheet(
    path: Path,
    samples: list[str],
    palette: list[tuple[int, int, int]],
    cols: int = 4,
    templates: dict[str, dict[str, object]] | None = None,
    preferred_styles: set[str] | None = None,
) -> None:
    from PIL import Image

    scale = 2
    if not samples:
        return

    rows = math.ceil(len(samples) / cols)
    sheet = Image.new("RGB", (cols * TILE * scale, rows * TILE * scale))

    for i, sample in enumerate(samples):
        sample_path = VOLCANIC_BITS / sample
        if not sample_path.exists():
            continue

        frame_width, frame_height, frames = read_shptd(sample_path)
        frame = first_visible_frame(sample, frames, templates, preferred_styles)
        tile = Image.new("RGB", (frame_width, frame_height))
        tile.putdata([palette[index] for index in frame])
        tile = tile.resize((TILE * scale, TILE * scale), Image.Resampling.NEAREST)
        sheet.paste(tile, ((i % cols) * TILE * scale, (i // cols) * TILE * scale))

    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)
    half = sheet.resize((max(1, sheet.width // 2), max(1, sheet.height // 2)), Image.Resampling.BOX)
    half.save(path.with_name(f"{path.stem}-half{path.suffix}"))


def first_visible_frame(
    image: str,
    frames: list[bytes],
    templates: dict[str, dict[str, object]] | None,
    preferred_styles: set[str] | None,
) -> bytes:
    if templates and preferred_styles and image in templates:
        info = templates[image]
        tiles = info["tiles"]
        if isinstance(tiles, dict):
            stem = Path(image).stem.lower()
            types = {str(t) for t in info["types"]}
            for i, frame in enumerate(frames):
                if any(pixel != 0 for pixel in frame):
                    style = classify(stem, types, tiles.get(i))
                    if style in preferred_styles:
                        return frame

    for frame in frames:
        if any(pixel != 0 for pixel in frame):
            return frame
    return frames[0]


if __name__ == "__main__":
    sys.exit(main())
