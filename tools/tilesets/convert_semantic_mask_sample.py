#!/usr/bin/env python
"""Convert one paint/review semantic mask sample into volcanic preview art."""

from __future__ import annotations

import argparse
from pathlib import Path
import zlib

from PIL import Image, ImageDraw, ImageFilter, ImageFont

from generate_volcanic_tileset import CLEAR_BASE_SEED, base_clear_index, build_palette, noise, write_pal
from shptd import write_shptd


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SOURCE = ROOT / ".vs/docs/volcanic-theater-previews/ra-temperate-mask-sources/cliffs/0170-s36-x4.png"
DEFAULT_MASK = ROOT / ".vs/docs/volcanic-theater-previews/semantic-mask-reviews/cliffs/0170-s36-x4-mask-handregion-v2.png"
DEFAULT_OUT_DIR = ROOT / ".vs/docs/volcanic-theater-previews/semantic-conversions/cliffs/0170-s36-x4"
TILE = 48
SCALE = 4


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=DEFAULT_SOURCE)
    parser.add_argument("--mask", type=Path, default=DEFAULT_MASK)
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    parser.add_argument(
        "--cliff-variant",
        choices=("red-brown", "cool-basalt", "warm-volcanic", "ash-gray-rust", "ash-gray-rust-shaded", "ash-gray-rust-relief", "native-form-v1", "native-form-v2", "native-form-v3", "native-form-v4", "native-form-v5", "native-form-v6", "native-form-v7", "native-form-v8", "native-form-v9", "native-form-v9-neutral-edge", "native-form-v10"),
        default="red-brown",
        help="Palette ramp used for pixels marked as cliff",
    )
    args = parser.parse_args()

    source_path = resolve(args.source)
    mask_path = resolve(args.mask)
    out_dir = resolve(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    sample_name = source_path.stem.removesuffix("-x4")
    asset_name = sample_name.split("-", 1)[-1]
    sample_seed = stable_sample_seed(source_path.stem)

    source = Image.open(source_path).convert("RGBA")
    mask = Image.open(mask_path).convert("RGBA")
    if source.size != mask.size:
        raise ValueError(f"source/mask size mismatch: {source.size} vs {mask.size}")

    # C&C PAL files store 6-bit channels. Preview with the same quantized RGB
    # values the game will display instead of the higher-precision build ramp.
    palette = [tuple(channel // 4 * 4 for channel in color) for color in build_palette()]
    facet_map = None
    if args.cliff_variant.startswith("native-form-"):
        source_native = source.resize((source.width // SCALE, source.height // SCALE), Image.Resampling.NEAREST)
        mask_native = mask.resize(source_native.size, Image.Resampling.NEAREST)
        native, facet_map = convert_native_form(source_native, mask_native, palette, args.cliff_variant, sample_seed)
        converted = native.resize(source.size, Image.Resampling.NEAREST)
    else:
        converted = convert(source, mask, palette, args.cliff_variant)
        native = converted.resize((converted.width // SCALE, converted.height // SCALE), Image.Resampling.NEAREST)
    frames = slice_frames(native, palette)

    source.save(out_dir / "source.png")
    mask.save(out_dir / "mask.png")
    converted.save(out_dir / "volcanic-converted-x4.png")
    native.save(out_dir / "volcanic-converted-native.png")
    if facet_map is not None:
        facet_map.save(out_dir / "facet-map-native.png")
        facet_map.resize(source.size, Image.Resampling.NEAREST).save(out_dir / "facet-map-x4.png")
    preview_vol = out_dir / f"{asset_name}-preview.vol"
    write_shptd(preview_vol, TILE, TILE, frames)
    write_pal(out_dir / "volcanic-preview.pal", palette)
    write_sheet(out_dir / "review.png", source, mask, converted, native, args.cliff_variant, sample_name)
    if facet_map is not None:
        write_native_form_sheet(out_dir / "native-form-review.png", source, mask, facet_map, converted, sample_name)

    print((out_dir / "review.png").resolve())
    print((out_dir / "volcanic-converted-x4.png").resolve())
    print(preview_vol.resolve())
    return 0


def resolve(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def convert(source: Image.Image, mask: Image.Image, palette: list[tuple[int, int, int]], cliff_variant: str) -> Image.Image:
    out = Image.new("RGBA", source.size, (0, 0, 0, 0))
    sp = source.load()
    mp = mask.load()
    op = out.load()
    seed = 0x1705_3600

    for y in range(source.height):
        for x in range(source.width):
            r, g, b, a = sp[x, y]
            if a == 0:
                continue

            role = classify_mask(mp[x, y])
            luma = luminance(r, g, b)
            n = noise(x, y, seed)

            if role == "cliff":
                idx = cliff_material_index(luma, r, g, b, x, y, n, cliff_variant)
            elif role == "lava":
                idx = lava_material_index(luma, n)
            elif role == "shore":
                idx = shore_material_index(luma, n)
            else:
                idx = ground_material_index(luma, r, g, b, x, y, n)

            op[x, y] = (*palette[idx], 255)

    return out


def classify_mask(pixel: tuple[int, int, int, int]) -> str:
    r, g, b, a = pixel
    if a == 0:
        return "empty"
    if r > 220 and g < 40 and b < 40:
        return "lava"
    if g > 220 and r < 40 and b < 40:
        return "cliff"
    if b > 220 and r < 40 and g < 40:
        return "shore"
    return "ground"


def convert_native_form(
    source: Image.Image,
    mask: Image.Image,
    palette: list[tuple[int, int, int]],
    variant: str,
    seed: int,
) -> tuple[Image.Image, Image.Image]:
    """Preserve broad source lighting while replacing its fine material texture."""
    luma_image = Image.new("L", source.size)
    luma_image.putdata([
        round(luminance(r, g, b)) if a else 0
        for r, g, b, a in source.get_flattened_data()
    ])
    broad_image = luma_image.filter(ImageFilter.GaussianBlur(radius=1.35))
    shadow_image = luma_image.filter(ImageFilter.GaussianBlur(radius=2.0))
    raw_luma = list(luma_image.get_flattened_data())
    broad_luma = list(broad_image.get_flattened_data())
    shadow_luma = list(shadow_image.get_flattened_data())
    source_pixels = list(source.get_flattened_data())
    mask_pixels = list(mask.get_flattened_data())

    cliff_values = [
        raw_luma[i]
        for i, pixel in enumerate(mask_pixels)
        if classify_mask(pixel) == "cliff" and source_pixels[i][3]
    ]
    low = percentile(cliff_values, 5)
    high = max(low + 1, percentile(cliff_values, 96))
    cap_cut = percentile(cliff_values, 64)
    ground_shadow_values = [
        shadow_luma[i]
        for i, pixel in enumerate(mask_pixels)
        if classify_mask(pixel) == "ground" and source_pixels[i][3]
    ]
    ground_shadow_baseline = percentile(ground_shadow_values, 75)

    basalt_indices = [3, *range(10, 50), *range(182, 187)]
    dark_basalt_indices = [3, *range(10, 50)]
    neutral_face_indices = [3, *range(10, 40)]
    ash_ridge_indices = [*range(30, 50), *range(182, 186)]
    rust_indices = [*range(100, 120), *range(50, 70), *range(120, 130)]
    neutral_edge_indices = [
        idx for idx, color in enumerate(palette)
        if 20 <= luminance(*color) <= 95 and max(color) - min(color) <= 4
    ]
    out = Image.new("RGBA", source.size, (0, 0, 0, 0))
    facets = Image.new("RGBA", source.size, (0, 0, 0, 0))
    out_pixels: list[tuple[int, int, int, int]] = []
    facet_pixels: list[tuple[int, int, int, int]] = []
    rust_cut = 0.61
    if variant in {"native-form-v7", "native-form-v8", "native-form-v9", "native-form-v9-neutral-edge", "native-form-v10"}:
        eligible_fields: list[float] = []
        for i, ((_, _, _, a), mask_pixel) in enumerate(zip(source_pixels, mask_pixels)):
            if not a or classify_mask(mask_pixel) != "cliff":
                continue
            x = i % source.width
            y = i // source.width
            detail = raw_luma[i] - broad_luma[i]
            form = clamp_float((broad_luma[i] + detail * 0.52 - low) / (high - low), 0.0, 1.0)
            facet = classify_cliff_facet(raw_luma[i], broad_luma[i], detail, form, variant, low, cap_cut, mask, x, y)
            if facet == "face" and 0.30 < form < 0.78:
                eligible_fields.append(coherent_noise(x, y, seed ^ 0xA57, 7))
        if eligible_fields:
            rust_cut = percentile(eligible_fields, 88)
    for i, ((r, g, b, a), mask_pixel) in enumerate(zip(source_pixels, mask_pixels)):
        if not a:
            out_pixels.append((0, 0, 0, 0))
            facet_pixels.append((0, 0, 0, 0))
            continue

        x = i % source.width
        y = i // source.width
        role = classify_mask(mask_pixel)
        raw = raw_luma[i]
        broad = broad_luma[i]
        detail = raw - broad
        n = noise(x, y, seed)

        if role != "cliff":
            if role == "lava":
                idx = lava_material_index(raw, n)
            elif role == "shore":
                idx = shore_material_index(raw, n)
            else:
                if variant in {"native-form-v8", "native-form-v9", "native-form-v9-neutral-edge", "native-form-v10"}:
                    cliff_distance = distance_to_cliff(mask, x, y, 8)
                    contact_distance = min(cliff_distance, 3) if cliff_distance is not None else None
                    idx = contact_ground_index(x, y, sample_noise=n, distance=contact_distance, seed=seed)
                    if variant == "native-form-v9-neutral-edge" and contact_distance is not None:
                        scatter = noise(x, y, seed ^ 0x9E1_6E)
                        if contact_distance == 1 and scatter < 54:
                            idx = accepted_ground_index(x, y)
                        idx = palette_index_for_luma(palette, neutral_edge_indices, luminance(*palette[idx]))
                    idx = source_shadow_ground_index(
                        idx,
                        shadow_luma[i],
                        ground_shadow_baseline,
                        cliff_distance,
                        palette,
                        taper_to_zero=variant in {"native-form-v9", "native-form-v9-neutral-edge", "native-form-v10"},
                        max_darken=(18.0 if variant == "native-form-v10" else 34.0 if variant == "native-form-v9-neutral-edge" else 30.0),
                        minimum_luma=26.0 if variant == "native-form-v10" else None,
                        shadow_candidates=neutral_edge_indices if variant == "native-form-v9-neutral-edge" else None,
                    )
                elif variant in {"native-form-v6", "native-form-v7"}:
                    cliff_distance = distance_to_cliff(mask, x, y, 3)
                    idx = contact_ground_index(x, y, sample_noise=n, distance=cliff_distance, seed=seed)
                elif variant == "native-form-v5":
                    idx = accepted_ground_index(x, y)
                else:
                    idx = ground_material_index(raw, r, g, b, x, y, n)
            out_pixels.append((*palette[idx], 255))
            facet_pixels.append((55, 62, 58, 255))
            continue

        modern_native = variant in {"native-form-v2", "native-form-v3", "native-form-v4", "native-form-v5", "native-form-v6", "native-form-v7", "native-form-v8", "native-form-v9", "native-form-v9-neutral-edge", "native-form-v10"}
        detail_mix = 0.52 if modern_native else 0.38
        form_luma = broad + detail * detail_mix
        form = clamp_float((form_luma - low) / (high - low), 0.0, 1.0)
        facet = classify_cliff_facet(raw, broad, detail, form, variant, low, cap_cut, mask, x, y)
        edge_distance = distance_to_non_cliff(mask, x, y, 2) if variant == "native-form-v9-neutral-edge" else None

        target_luma = 22 + form * (84 if modern_native else 103)
        if facet == "crevice":
            target_luma *= 0.55
        elif facet == "ridge":
            target_luma = min(126, target_luma + 14)
        elif facet == "talus":
            target_luma = min(86, target_luma * 0.87)
            if variant == "native-form-v9-neutral-edge":
                target_luma = min(target_luma, 45)
        elif facet == "face" and variant == "native-form-v10":
            target_luma += 8
            if touches_cap_luma(mask, broad_luma, source.width, source.height, x, y, cap_cut):
                target_luma += 5
        if edge_distance == 1:
            target_luma = min(target_luma, 56 if facet in {"cap", "ridge"} else 42)

        rust_field = coherent_noise(x, y, seed ^ 0xA57, 7)
        rust_threshold = rust_cut if variant in {"native-form-v7", "native-form-v8", "native-form-v9", "native-form-v9-neutral-edge", "native-form-v10"} else (0.61 if modern_native else 0.67)
        near_edge = edge_distance is not None
        use_rust = facet == "face" and 0.30 < form < 0.78 and rust_field >= rust_threshold
        if variant == "native-form-v9-neutral-edge" and near_edge:
            use_rust = False
        if edge_distance == 1:
            candidates = neutral_edge_indices
        elif facet == "ridge" and variant in {"native-form-v4", "native-form-v5", "native-form-v6", "native-form-v7", "native-form-v8", "native-form-v9", "native-form-v9-neutral-edge", "native-form-v10"}:
            candidates = [*range(40, 50), *range(124, 130)]
            target_luma = min(target_luma, 94)
        elif facet == "ridge" and modern_native:
            candidates = ash_ridge_indices
        elif use_rust:
            candidates = rust_indices
        elif facet == "talus" and variant == "native-form-v9-neutral-edge":
            candidates = neutral_edge_indices
        elif facet == "face" and variant in {"native-form-v7", "native-form-v8", "native-form-v9", "native-form-v9-neutral-edge", "native-form-v10"}:
            candidates = [3, *range(10, 44)] if variant == "native-form-v10" else neutral_face_indices
        elif modern_native:
            candidates = dark_basalt_indices
        else:
            candidates = basalt_indices
        idx = palette_index_for_luma(palette, candidates, target_luma)
        out_pixels.append((*palette[idx], 255))
        facet_pixels.append(facet_color(facet))

    out.putdata(out_pixels)
    facets.putdata(facet_pixels)
    return out, facets


def classify_cliff_facet(
    raw: float,
    broad: float,
    detail: float,
    form: float,
    variant: str,
    low: float,
    cap_cut: float,
    mask: Image.Image,
    x: int,
    y: int,
) -> str:
    modern_native = variant in {"native-form-v2", "native-form-v3", "native-form-v4", "native-form-v5", "native-form-v6", "native-form-v7", "native-form-v8", "native-form-v9", "native-form-v9-neutral-edge", "native-form-v10"}
    if variant in {"native-form-v4", "native-form-v5", "native-form-v6", "native-form-v7", "native-form-v8", "native-form-v9", "native-form-v9-neutral-edge", "native-form-v10"}:
        is_crevice = (detail < -24 and broad < cap_cut * 0.82) or (raw <= low + 3 and broad < cap_cut)
    else:
        is_crevice = raw <= low + 3 or detail < -24

    if is_crevice:
        return "crevice"
    if detail > (24 if modern_native else 17) and form > 0.45 and (variant not in {"native-form-v3", "native-form-v4", "native-form-v5", "native-form-v6", "native-form-v7", "native-form-v8", "native-form-v9", "native-form-v9-neutral-edge", "native-form-v10"} or broad >= cap_cut):
        return "ridge"
    if touches_non_cliff(mask, x, y) and form < 0.58:
        return "talus"
    if broad >= cap_cut:
        return "cap"
    return "face"


def touches_non_cliff(mask: Image.Image, x: int, y: int) -> bool:
    for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
        nx, ny = x + ox, y + oy
        if nx < 0 or ny < 0 or nx >= mask.width or ny >= mask.height:
            return True
        if classify_mask(mask.getpixel((nx, ny))) != "cliff":
            return True
    return False


def touches_cap_luma(
    mask: Image.Image,
    broad_luma: list[int],
    width: int,
    height: int,
    x: int,
    y: int,
    cap_cut: float,
) -> bool:
    for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
        nx, ny = x + ox, y + oy
        if not (0 <= nx < width and 0 <= ny < height):
            continue
        if classify_mask(mask.getpixel((nx, ny))) == "cliff" and broad_luma[ny * width + nx] >= cap_cut:
            return True
    return False


def coherent_noise(x: int, y: int, seed: int, scale: int) -> float:
    gx, gy = x // scale, y // scale
    fx, fy = (x % scale) / scale, (y % scale) / scale
    fx = fx * fx * (3 - 2 * fx)
    fy = fy * fy * (3 - 2 * fy)

    def sample(px: int, py: int) -> float:
        return noise(px, py, seed) / 255.0

    top = sample(gx, gy) * (1 - fx) + sample(gx + 1, gy) * fx
    bottom = sample(gx, gy + 1) * (1 - fx) + sample(gx + 1, gy + 1) * fx
    return top * (1 - fy) + bottom * fy


def palette_index_for_luma(
    palette: list[tuple[int, int, int]], candidates: list[int], target: float
) -> int:
    return min(candidates, key=lambda idx: abs(luminance(*palette[idx]) - target))


def percentile(values: list[int], amount: int) -> float:
    if not values:
        return 0
    ordered = sorted(values)
    position = (len(ordered) - 1) * amount / 100
    lower = int(position)
    upper = min(lower + 1, len(ordered) - 1)
    fraction = position - lower
    return ordered[lower] * (1 - fraction) + ordered[upper] * fraction


def facet_color(facet: str) -> tuple[int, int, int, int]:
    return {
        "crevice": (22, 20, 24, 255),
        "ridge": (224, 220, 194, 255),
        "talus": (166, 132, 75, 255),
        "cap": (73, 136, 196, 255),
        "face": (178, 76, 58, 255),
    }[facet]


def cliff_material_index(luma: float, r: int, g: int, b: int, x: int, y: int, n: int, variant: str) -> int:
    strata = int(2.0 * wave(x, y, 23, 17) + 1.5 * wave(y, x, 31, 19))
    ridge = 7 if ((x * 2 - y + n) % 41) < 5 and luma > 92 else 0
    shade = -7 if ((y * 2 + x + n) % 53) < 7 and luma < 118 else 0
    luma = max(0, min(255, (luma - 82) * 1.55 + 88 + ridge + shade))
    deep_shadow = luma < 30
    if deep_shadow:
        return 3 + n % 4

    if variant == "cool-basalt":
        if luma > 184:
            return clamp(184 + n % 4 + strata // 2, 182, 188)
        if luma > 148:
            return clamp(183 + n % 4 + strata // 2, 182, 187)
        if luma > 104:
            return clamp(32 + n % 8 + strata, 30, 45)
        if luma > 58:
            return 14 + n % 8
        return 4 + n % 6

    if variant == "warm-volcanic":
        if luma > 184:
            return clamp(116 + n % 5 + strata, 112, 123)
        if luma > 148:
            return clamp(108 + n % 6 + strata, 104, 117)
        if luma > 104:
            return clamp(100 + n % 6 + strata, 100, 110)
        if luma > 58:
            return 50 + n % 12
        return 10 + n % 8

    if variant == "ash-gray-rust":
        rust = n > 232 or ((x + y * 2 + n) % 53) < 4
        if luma > 184:
            return clamp(184 + n % 4 + strata // 2, 182, 188)
        if luma > 148:
            return clamp(183 + n % 4 + strata // 2, 182, 187)
        if luma > 104:
            return clamp((100 if rust else 34) + n % 7 + strata, 32 if not rust else 100, 108 if rust else 47)
        if luma > 58:
            return (50 + n % 8) if rust else (16 + n % 8)
        return 5 + n % 7

    if variant == "ash-gray-rust-shaded":
        soft_luma = max(0, min(255, (luma - 82) * 0.78 + 82 + ridge * 0.45 + shade * 0.55))
        rust = n > 238 or ((x + y * 2 + n) % 71) < 3
        if soft_luma > 196:
            return clamp(183 + n % 3 + strata // 3, 182, 186)
        if soft_luma > 164:
            return clamp(124 + n % 5 + strata // 2, 120, 129)
        if soft_luma > 128:
            return clamp((100 if rust else 38) + n % 6 + strata // 2, 36 if not rust else 100, 108 if rust else 49)
        if soft_luma > 88:
            return (50 + n % 7) if rust else (24 + n % 6)
        if soft_luma > 52:
            return 14 + n % 8
        return 5 + n % 6

    if variant == "ash-gray-rust-relief":
        relief_luma = max(0, min(255, (luma - 82) * 1.08 + 86 + ridge * 0.7 + shade * 0.45))
        rust = n > 240 or ((x + y * 2 + n) % 79) < 3
        if relief_luma > 202:
            return clamp(183 + n % 3 + strata // 3, 182, 186)
        if relief_luma > 172:
            return clamp(124 + n % 5 + strata // 2, 120, 129)
        if relief_luma > 132:
            return clamp((100 if rust else 40) + n % 6 + strata // 2, 38 if not rust else 100, 108 if rust else 49)
        if relief_luma > 92:
            return (50 + n % 7) if rust else (26 + n % 6)
        if relief_luma > 54:
            return 14 + n % 8
        return 5 + n % 6

    if luma > 184:
        return clamp(124 + n % 5 + strata, 120, 129)
    if luma > 148:
        return clamp(116 + n % 5 + strata, 112, 123)
    if luma > 104:
        return clamp(104 + n % 6 + strata, 100, 114)
    if luma > 58:
        return 50 + n % 12
    return 10 + n % 8


def ground_material_index(luma: float, r: int, g: int, b: int, x: int, y: int, n: int) -> int:
    greenish = g > r + 8 and g > b + 5
    crack = abs(((x * 7 + y * 11 + n) % 97) - 48) < 2 and n > 180
    if crack:
        return 2 + n % 4
    if greenish and luma > 50:
        return 14 + n % 8
    if luma > 116:
        return 26 + n % 8
    if luma > 72:
        return 18 + n % 8
    return 10 + n % 8


def accepted_ground_index(x: int, y: int) -> int:
    tile_x = x % TILE
    tile_y = y % TILE
    n = noise(tile_x, tile_y, CLEAR_BASE_SEED)
    return base_clear_index(tile_x, tile_y, CLEAR_BASE_SEED, n)


def distance_to_cliff(mask: Image.Image, x: int, y: int, max_distance: int) -> int | None:
    for distance in range(1, max_distance + 1):
        for oy in range(-distance, distance + 1):
            for ox in range(-distance, distance + 1):
                if max(abs(ox), abs(oy)) != distance:
                    continue
                nx, ny = x + ox, y + oy
                if 0 <= nx < mask.width and 0 <= ny < mask.height and classify_mask(mask.getpixel((nx, ny))) == "cliff":
                    return distance
    return None


def distance_to_non_cliff(mask: Image.Image, x: int, y: int, max_distance: int) -> int | None:
    for distance in range(1, max_distance + 1):
        for oy in range(-distance, distance + 1):
            for ox in range(-distance, distance + 1):
                if max(abs(ox), abs(oy)) != distance:
                    continue
                nx, ny = x + ox, y + oy
                if nx < 0 or ny < 0 or nx >= mask.width or ny >= mask.height:
                    return distance
                if classify_mask(mask.getpixel((nx, ny))) != "cliff":
                    return distance
    return None


def contact_ground_index(x: int, y: int, sample_noise: int, distance: int | None, seed: int) -> int:
    base = accepted_ground_index(x, y)
    if distance is None:
        return base

    scatter = noise(x, y, seed ^ 0x7A1_05)
    if distance == 1:
        return 16 + sample_noise % 7
    if distance == 2 and scatter > 72:
        return 14 + sample_noise % 6
    if distance == 3 and scatter > 196:
        return 12 + sample_noise % 5
    return base


def source_shadow_ground_index(
    base_index: int,
    source_shadow_luma: float,
    baseline_luma: float,
    cliff_distance: int | None,
    palette: list[tuple[int, int, int]],
    taper_to_zero: bool = False,
    max_darken: float = 30.0,
    minimum_luma: float | None = None,
    shadow_candidates: list[int] | None = None,
) -> int:
    if cliff_distance is None:
        return base_index

    source_strength = clamp_float((baseline_luma - source_shadow_luma) / 30.0, 0.0, 1.0)
    if taper_to_zero:
        distance_weight = clamp_float((8 - cliff_distance) / 7.0, 0.0, 1.0)
        distance_weight = distance_weight * distance_weight * (3 - 2 * distance_weight)
    else:
        distance_weight = 0.75 + 0.25 * clamp_float((9 - cliff_distance) / 8.0, 0.0, 1.0)
    target_luma = luminance(*palette[base_index]) - source_strength * distance_weight * max_darken
    if minimum_luma is not None:
        target_luma = max(minimum_luma, target_luma)
    if shadow_candidates is None:
        shadow_candidates = [4, 1, 2, 3, *range(10, 18)]
    return palette_index_for_luma(palette, shadow_candidates, target_luma)


def lava_material_index(luma: float, n: int) -> int:
    if luma > 150:
        return 94 + n % 5
    if luma > 95:
        return 84 + n % 8
    if luma > 58:
        return 70 + n % 10
    return 50 + n % 8


def shore_material_index(luma: float, n: int) -> int:
    if n > 246:
        return 70 + n % 7
    if luma > 110:
        return 30 + n % 10
    return 12 + n % 10


def slice_frames(native: Image.Image, palette: list[tuple[int, int, int]]) -> list[bytes]:
    if native.width % TILE or native.height % TILE:
        raise ValueError(f"native image size {native.size} is not divisible by {TILE}")

    frames: list[bytes] = []
    for cell_y in range(native.height // TILE):
        for cell_x in range(native.width // TILE):
            data = bytearray()
            for y in range(TILE):
                for x in range(TILE):
                    r, g, b, a = native.getpixel((cell_x * TILE + x, cell_y * TILE + y))
                    data.append(0 if a == 0 else nearest_palette_index((r, g, b), palette))
            frames.append(bytes(data))
    return frames


def nearest_palette_index(rgb: tuple[int, int, int], palette: list[tuple[int, int, int]]) -> int:
    best = 0
    best_dist = 1 << 62
    for i, (r, g, b) in enumerate(palette):
        dist = (rgb[0] - r) ** 2 + (rgb[1] - g) ** 2 + (rgb[2] - b) ** 2
        if dist < best_dist:
            best = i
            best_dist = dist
    return best


def write_sheet(
    path: Path,
    source: Image.Image,
    mask: Image.Image,
    converted: Image.Image,
    native: Image.Image,
    cliff_variant: str,
    sample_name: str,
) -> None:
    font = ImageFont.load_default()
    native_x4 = native.resize(converted.size, Image.Resampling.NEAREST)
    panels = [("source", source), ("mask", mask), ("converted", converted), ("native x4", native_x4)]
    gutter = 12
    header = 30
    info_height = 34
    w, h = converted.size
    sheet = Image.new("RGBA", (w * 2 + gutter, header * 2 + h * 2 + gutter + info_height), (73, 86, 99, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((6, 6), f"{sample_name} semantic volcanic conversion candidate  cliff={cliff_variant}", fill=(255, 255, 255, 255), font=font)
    positions = [(0, header), (w + gutter, header), (0, header + h + gutter + header), (w + gutter, header + h + gutter + header)]
    for (title, panel), (x, y) in zip(panels, positions):
        draw.text((x + 4, y - 18), title, fill=(255, 255, 255, 255), font=font)
        sheet.alpha_composite(panel, (x, y))
    draw.text((6, sheet.height - info_height + 8), "Preview only: source luminance preserved, material swapped by semantic mask.", fill=(255, 255, 255, 255), font=font)
    sheet.save(path)


def write_native_form_sheet(
    path: Path,
    source: Image.Image,
    mask: Image.Image,
    facet_map: Image.Image,
    converted: Image.Image,
    sample_name: str,
) -> None:
    font = ImageFont.load_default()
    facet_x4 = facet_map.resize(source.size, Image.Resampling.NEAREST)
    panels = [("temperate source", source), ("cliff mask", mask), ("derived facets", facet_x4), ("native-form volcanic", converted)]
    gutter = 12
    header = 30
    footer = 34
    w, h = source.size
    sheet = Image.new("RGBA", (w * 2 + gutter, header * 2 + h * 2 + gutter + footer), (73, 86, 99, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((6, 6), f"{sample_name} native-resolution form/material conversion", fill=(255, 255, 255, 255), font=font)
    positions = [(0, header), (w + gutter, header), (0, header * 2 + h + gutter), (w + gutter, header * 2 + h + gutter)]
    for (title, panel), (x, y) in zip(panels, positions):
        draw.text((x + 4, y - 18), title, fill=(255, 255, 255, 255), font=font)
        sheet.alpha_composite(panel, (x, y))
    draw.text(
        (6, sheet.height - footer + 8),
        "Facets: blue=cap, red=face, pale=ridge, ochre=talus, dark=crevice.",
        fill=(255, 255, 255, 255),
        font=font,
    )
    sheet.save(path)


def luminance(r: int, g: int, b: int) -> float:
    return 0.299 * r + 0.587 * g + 0.114 * b


def wave(a: int, b: int, period_a: int, period_b: int) -> float:
    import math

    return math.sin(a / period_a) + math.cos(b / period_b)


def clamp(value: int, low: int, high: int) -> int:
    return max(low, min(high, value))


def clamp_float(value: float, low: float, high: float) -> float:
    return max(low, min(high, value))


def stable_sample_seed(stem: str) -> int:
    # Preserve the accepted s36 material pattern while giving every other asset
    # a deterministic independent pattern.
    if stem == "0170-s36-x4":
        return 0x1705_36F1
    return zlib.crc32(stem.encode("ascii"))


if __name__ == "__main__":
    raise SystemExit(main())
