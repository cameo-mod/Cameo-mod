#!/usr/bin/env python3
"""Extract faction radar insignias from sidebar chrome sheets.

The radar art shown on the sidebar before a player has radar (or while low on
power) is the faction "insignia" region carved out of that faction's sidebar
PNG. chrome.yaml is the source of truth: every `sidebar-<faction>` block names
an `insignia: x, y, w, h` region into a sidebar image resolved through its
`Inherits:` -> `Image:` chain. This script parses those definitions and crops
each insignia out, so the output always matches what the engine actually draws.

Usage: python tools/extract_insignias.py
Output: docs/faction-insignias/<faction>.png  (+ contact sheet + README)
"""

import os
import re
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CHROME = os.path.join(ROOT, "mods", "cameo", "chrome.yaml")
UIBITS = os.path.join(ROOT, "mods", "cameo", "uibits")
OUTDIR = os.path.join(ROOT, "docs", "faction-insignias")


def parse_chrome(path):
    """Return {block_name: {'inherits': [str,...], 'image': str|None,
    'insignia': (x,y,w,h)|None}} for every top-level chrome block.

    `inherits` is the ordered list of parents (Inherits, Inherits@2, ...).
    Per MiniYaml semantics, later parents override earlier ones, and a
    block's own value overrides all parents."""
    blocks = {}
    cur = None
    with open(path, encoding="utf-8") as f:
        for raw in f:
            line = raw.rstrip("\n")
            if not line.strip() or line.lstrip().startswith("#"):
                continue
            # Top-level block header: starts at column 0, ends with ':'
            if not line[0].isspace():
                m = re.match(r"^([^\s:]+):\s*$", line)
                if m:
                    cur = {"inherits": [], "image": None, "insignia": None}
                    blocks[m.group(1)] = cur
                    continue
                cur = None  # column-0 line that isn't a clean header
                continue
            if cur is None:
                continue
            s = line.strip()
            mi = re.match(r"Inherits(?:@\S+)?:\s*(\S+)", s)
            if mi:
                cur["inherits"].append(mi.group(1))
                continue
            mim = re.match(r"Image:\s*(\S+)", s)
            if mim:
                cur["image"] = mim.group(1)
                continue
            mr = re.match(r"insignia:\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)", s)
            if mr:
                cur["insignia"] = tuple(int(x) for x in mr.groups())
                continue
    return blocks


def resolve(name, field, blocks, seen=None):
    """Resolve a field through multi-inheritance: parents applied in order
    (later overrides earlier), then the block's own value wins."""
    seen = seen or set()
    if name in seen or name not in blocks:
        return None
    seen.add(name)
    b = blocks[name]
    val = None
    for parent in b["inherits"]:
        pv = resolve(parent, field, blocks, set(seen))
        if pv is not None:
            val = pv
    if b[field] is not None:
        val = b[field]
    return val


def main():
    os.makedirs(OUTDIR, exist_ok=True)
    blocks = parse_chrome(CHROME)

    # Faction blocks look like `sidebar-<faction>` with a single segment
    # (buttons are `sidebar-button-...`); exclude non-faction chrome.
    exclude = {"button", "bits", "observer"}
    targets = []
    for n in blocks:
        m = re.match(r"^sidebar-([a-z0-9]+)$", n)
        if m and m.group(1) not in exclude:
            targets.append(n)
    targets.append("sidebar")  # the base/default sidebar art

    sheet_cache = {}
    results = []
    for name in sorted(set(targets)):
        region = resolve(name, "insignia", blocks)
        if not region:
            continue
        img_name = resolve(name, "image", blocks)
        if not img_name:
            print(f"  SKIP {name}: no image resolved")
            continue
        sheet_path = os.path.join(UIBITS, img_name)
        if not os.path.exists(sheet_path):
            print(f"  SKIP {name}: missing sheet {img_name}")
            continue
        if sheet_path not in sheet_cache:
            sheet_cache[sheet_path] = Image.open(sheet_path).convert("RGBA")
        sheet = sheet_cache[sheet_path]
        x, y, w, h = region
        crop = sheet.crop((x, y, x + w, y + h))
        # faction key: strip leading "sidebar-"; bare "sidebar" -> "default"
        faction = name[len("sidebar-"):] if name.startswith("sidebar-") else "default"
        out = os.path.join(OUTDIR, f"{faction}.png")
        crop.save(out)
        results.append((faction, img_name, (x, y, w, h), crop))
        print(f"  {faction:<16} <- {img_name} @ {x},{y},{w},{h}")

    build_contact_sheet(results)
    write_readme(results)
    print(f"\nExtracted {len(results)} insignias -> {OUTDIR}")


def build_contact_sheet(results):
    if not results:
        return
    cols = 6
    cell = 128
    pad = 8
    rows = (len(results) + cols - 1) // cols
    W = cols * (cell + pad) + pad
    H = rows * (cell + pad) + pad
    sheet = Image.new("RGBA", (W, H), (24, 24, 24, 255))
    for i, (faction, _img, _r, crop) in enumerate(results):
        thumb = crop.copy()
        thumb.thumbnail((cell, cell), Image.LANCZOS)
        cx = pad + (i % cols) * (cell + pad)
        cy = pad + (i // cols) * (cell + pad)
        ox = cx + (cell - thumb.width) // 2
        oy = cy + (cell - thumb.height) // 2
        sheet.alpha_composite(thumb, (ox, oy))
    sheet.save(os.path.join(OUTDIR, "_contact-sheet.png"))


def write_readme(results):
    lines = [
        "# Faction radar insignias",
        "",
        "Radar art shown on top of the sidebar when a player has no radar yet or",
        "is low on power. Each insignia is the `insignia` region defined in",
        "`mods/cameo/chrome.yaml`, cropped from that faction's sidebar sheet in",
        "`mods/cameo/uibits/`.",
        "",
        "Regenerate with `python tools/extract_insignias.py`.",
        "",
        "![contact sheet](_contact-sheet.png)",
        "",
        "| Faction | Source sheet | Region (x, y, w, h) | Insignia |",
        "| --- | --- | --- | --- |",
    ]
    for faction, img, (x, y, w, h), _crop in results:
        lines.append(
            f"| {faction} | `{img}` | {x}, {y}, {w}, {h} | "
            f"![{faction}]({faction}.png) |"
        )
    lines.append("")
    with open(os.path.join(OUTDIR, "README.md"), "w", encoding="utf-8") as f:
        f.write("\n".join(lines))


if __name__ == "__main__":
    main()
