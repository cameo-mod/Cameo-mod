#!/usr/bin/env python3
"""audit_chrome_scale_variants.py — a chrome sheet must be exactly the scale it is declared as.

    python tools/audit/audit_chrome_scale_variants.py

⛔ WHY THIS EXISTS. Discord bug report, 2026-09: *"UI scaling above 150% messes up faction icons
and some UI in game"* — reproduced at 1080p and at 2800x1600, but NOT at 1440p.

The cause is not the scaling code. `chrome.yaml` declares each collection's regions ONCE, in 1x
coordinates, and `ChromeProvider` (`OpenRA.Game/Graphics/ChromeProvider.cs`) multiplies them by 2
or 3 to index into the `Image2x` / `Image3x` sheet. That arithmetic is only correct if the sheet
really is 2x / 3x the base. Two of them were not:

    flags.png  512x512   flags_2x.png  1024x1024 (2x, ok)   flags_3x.png  2048x2048 (4x, WRONG)
    glyphs.png 256x256   glyphs_2x.png  512x512  (2x, ok)   glyphs_3x.png 1024x1024 (4x, WRONG)

⭐ AND THE MISREAD EXPLAINS THE SYMPTOM EXACTLY. With the engine reading at 3x from a 4x sheet, a
region at 1x (x, y) is read from (3x, 3y) while its art actually sits at (4x, 4y) — so the error
is PROPORTIONAL TO DISTANCE FROM THE TOP-LEFT CORNER:

    gdi      at 0,0     -> 100% of the correct pixels   (3*0 == 4*0, so the corner looks fine)
    nod      at 0,16    ->  67%
    XCOM     at 352,224 ->   0%   — completely different artwork
    Warcraft at 160,496 ->   0%

which is why it reads as "some UI" rather than "all UI", and why nobody spotted it from the first
few icons. Only the 3x path is affected, so whether a given player sees it depends on which DPI
bucket their resolution and UI scale land in — that is the whole "1440p is fine, 1080p is not".

WHAT IT CHECKS, for every collection in chrome.yaml:
  1. each declared `Image2x` / `Image3x` is exactly 2x / 3x the `Image` dimensions (a variant
     pointing at the SAME file as the base is allowed — that is how a region-less full-screen
     image like ^LoadScreen opts out);
  2. every region fits inside the 1x sheet, since that is the coordinate space they are in.

⚠ Absence is fine and is the normal case — 71 collections here declare only `Image`. A missing
variant makes the engine fall back to a smaller sheet, which renders CORRECT pixels slightly
softer. A wrongly-sized variant renders wrong pixels. Never "fix" a warning here by inventing a
file; either re-export it at the right size or drop the declaration.

EXIT CODE: 1 on any wrongly-sized variant or out-of-bounds region.
"""
from __future__ import annotations

import pathlib
import re
import struct
import sys

if hasattr(sys.stdout, "reconfigure"):          # Windows consoles default to cp1252
    sys.stdout.reconfigure(encoding="utf-8")

CHROME = pathlib.Path("mods/cameo/chrome.yaml")
UIBITS = pathlib.Path("mods/cameo/uibits")


def png_size(path: pathlib.Path) -> tuple[int, int] | None:
    """Width/height straight out of the IHDR chunk — no image library needed."""
    try:
        head = path.read_bytes()[:24]
    except OSError:
        return None
    if len(head) < 24 or head[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    return struct.unpack(">II", head[16:24])


def collections(text: str) -> list[tuple[str, dict[str, str], str]]:
    """(name, {Image/Image2x/Image3x: file}, regions-block) for every top-level node."""
    out = []
    for block in re.split(r"\n(?=\S)", text):
        name = block.split(":", 1)[0].strip()
        if not name or name.startswith("#"):
            continue
        imgs = {k: v for k, v in re.findall(r"^\t(Image(?:2x|3x)?):\s*(\S+)", block, re.M)}
        if not imgs.get("Image"):
            continue
        regions = re.search(r"^\tRegions:\n((?:\t\t.*\n)*)", block, re.M)
        out.append((name, imgs, regions.group(1) if regions else ""))
    return out


def main() -> int:
    if not CHROME.exists():
        print(f"⛔ **FAIL** — {CHROME} not found.")
        return 1

    text = CHROME.read_text(encoding="utf-8")

    # Regions live on the collections that INHERIT a ^Template, so resolve one level of Inherits.
    base_of = dict(re.findall(r"\n(\S+):\n\tInherits: (\^\S+)\n", text))
    sizes: dict[str, tuple[int, int]] = {}

    bad_scale, bad_region, missing = [], [], []

    for name, imgs, _ in collections(text):
        base = png_size(UIBITS / imgs["Image"])
        if base is None:
            missing.append((name, imgs["Image"]))
            continue
        sizes[name] = base
        for key, factor in (("Image2x", 2), ("Image3x", 3)):
            fn = imgs.get(key)
            if not fn:
                continue
            if fn == imgs["Image"]:
                continue                    # deliberate opt-out; only valid without Regions
            got = png_size(UIBITS / fn)
            if got is None:
                missing.append((name, fn))
                continue
            if got[0] != base[0] * factor or got[1] != base[1] * factor:
                bad_scale.append((name, key, fn, base, got,
                                  round(got[0] / base[0], 2) if base[0] else 0, factor))

    for name, imgs, regions in collections(text):
        owner = name
        base = sizes.get(name) or sizes.get(base_of.get(name, ""), None)
        if base is None or not regions:
            continue
        for line in regions.splitlines():
            if ":" not in line:
                continue
            label, rest = line.split(":", 1)
            nums = re.findall(r"-?\d+", rest)
            if len(nums) < 4:
                continue
            x, y, w, h = (int(n) for n in nums[:4])
            if x + w > base[0] or y + h > base[1]:
                bad_region.append((owner, label.strip(), (x, y, w, h), base))

    print("# audit_chrome_scale_variants — is every chrome sheet the scale it claims?\n")
    print(f"Checked **{len(sizes)}** collections in `{CHROME}`.\n")

    if bad_scale:
        print("## ⛔ Wrongly-sized scale variants\n")
        print("| collection | field | file | base | actual | is | declared as |")
        print("|---|---|---|--:|--:|--:|--:|")
        for name, key, fn, base, got, ratio, factor in bad_scale:
            print(f"| {name} | `{key}` | `{fn}` | {base[0]}x{base[1]} | {got[0]}x{got[1]} "
                  f"| {ratio}x | {factor}x |")
        print()

    if bad_region:
        print("## ⛔ Regions outside the 1x sheet\n")
        for owner, label, r, base in bad_region[:20]:
            print(f"- `{owner}` / `{label}` = {r} exceeds {base[0]}x{base[1]}")
        print()

    if missing:
        print("## ⚠ Declared files that could not be read\n")
        for name, fn in missing[:20]:
            print(f"- `{name}` -> `{fn}`")
        print()

    if bad_scale or bad_region:
        print("**FAIL** — see `docs/audit/CHROME_SCALE_BUG.md` for the diagnosis and the fix.")
        return 1

    print("**PASS** — every declared variant is exactly its stated scale, and every region fits.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
