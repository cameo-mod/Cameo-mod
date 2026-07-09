#!/usr/bin/env python3
"""curate_map.py — turn a generated rename_map draft into an applicable map.

Usage: python tools/rename/curate_map.py <faction> [--slugtag <tag>]

- strips a redundant trailing faction tag from names produced by display
  strings like "Construction Yard (GDI)" (-> ts_gdi_constructionyard)
- adds dotted variants and husks of mapped ids (X.husk -> <new>_husk,
  X.<suffix> -> <new>_<suffix>) when the variant actor exists
- rebuilds the files: section with unique-ownership + suffix-clean naming
- asserts uniqueness and existence; collisions abort with a design question

Display-name collisions are NOT auto-resolved: the tool prints them and
exits non-zero so design can choose names first (DESIGN.md §1).
"""

from __future__ import annotations

import argparse
import os
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/audit"))
sys.path.insert(0, str(ROOT / "tools/rename"))

from apply import load_map  # noqa: E402
from cameo_model import Model  # noqa: E402

VARIANT_SUFFIXES = ("husk", "sp", "r4", "wild", "mk2", "elite", "ai", "water",
                    "bot", "backup")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("faction")
    ap.add_argument("--slugtag", default=None,
                    help="redundant trailing tag to strip from names")
    args = ap.parse_args()

    m = Model(ROOT)
    rs = m.rs
    map_path = ROOT / f"tools/rename/rename_map_{args.faction}.yaml"
    actors, _ = load_map(map_path)

    # 1) strip redundant trailing faction tag
    if args.slugtag:
        tag = args.slugtag.lower()
        for old, new in list(actors.items()):
            prefix, _, name = new.rpartition("_")
            if name.endswith(tag) and len(name) > len(tag):
                actors[old] = f"{prefix}_{name[:-len(tag)]}".rstrip("_")

    # 2) dotted variants & husks of mapped ids
    additions: dict[str, str] = {}
    lower_map = {k.lower(): v for k, v in actors.items()}
    for name in rs.actors:
        ln = name.lower()
        if "." not in ln or ln in lower_map:
            continue
        base, _, suffix = ln.rpartition(".")
        if base in lower_map and suffix:
            sfx = suffix if suffix in VARIANT_SUFFIXES else suffix
            additions[ln] = f"{lower_map[base]}_{sfx}"
    actors.update(additions)

    # 3) collision & existence checks
    missing = [o for o in actors if rs.actor(o) is None]
    assert not missing, f"olds missing: {missing}"
    dupes: dict[str, list[str]] = {}
    for o, n in actors.items():
        dupes.setdefault(n, []).append(o)
    collisions = {n: olds for n, olds in dupes.items() if len(olds) > 1}
    if collisions:
        print("DESIGN DECISION NEEDED — colliding proposals:")
        for n, olds in collisions.items():
            print(f"  {n}: {olds}")
        return 1

    # 4) files section (unique-ownership + suffix-clean, DESIGN.md §1)
    usage: dict[str, int] = {}
    for img, node in rs.sequences.items():
        seen = set()
        def walk(nd):
            for c in nd.children:
                if c.key == "Filename" and c.value:
                    seen.add(c.value.lower())
                walk(c)
        walk(node)
        for f in seen:
            usage[f] = usage.get(f, 0) + 1

    files: dict[str, str] = {}
    claimed: dict[str, str] = {}
    for old, new in sorted(actors.items()):
        res = rs.resolve(old)
        if res is None:
            continue
        img = ((res.get("RenderSprites", "Image") or old)).lower()
        if img != old.lower() and img in {a.lower() for a in actors}:
            continue    # image owned by another mapped actor
        node = rs.sequence_image(img)
        if node is None:
            continue
        body, icons = set(), set()
        for seq in node.children:
            for sub in [seq] + seq.children:
                if sub.key == "Filename" and sub.value \
                        and usage.get(sub.value.lower(), 0) == 1 \
                        and not sub.value.lower().startswith("alt"):
                    (icons if seq.key.lower() == "icon" else body).add(sub.value)
        for f in sorted(icons):
            ext = pathlib.PurePosixPath(f).suffix
            tgt = f"{new}_icon{ext}"
            if claimed.get(f, tgt) == tgt:
                files[f] = tgt; claimed[f] = tgt
        distinct = sorted(body)
        common = os.path.commonprefix(
            [pathlib.PurePosixPath(f).stem for f in distinct]) \
            if len(distinct) > 1 else ""
        for f in distinct:
            ext = pathlib.PurePosixPath(f).suffix
            stem = pathlib.PurePosixPath(f).stem
            if len(distinct) == 1:
                tgt = f"{new}{ext}"
            else:
                suffix = stem[len(common):].strip("_")
                tgt = f"{new}_{suffix}{ext}" if suffix else f"{new}{ext}"
            if claimed.get(f, tgt) == tgt:
                files[f] = tgt; claimed[f] = tgt

    on_disk = {p.name.lower() for p in (ROOT / "mods/cameo/bits").rglob("*")
               if p.is_file()}
    files = {f: t for f, t in files.items() if f.lower() in on_disk}

    out = [f"# rename_map_{args.faction}.yaml — curated (DESIGN.md §1)",
           "actors:"]
    for o, n in sorted(actors.items()):
        out.append(f"\t{o}: {n}")
    out.append("files:")
    for o, n in sorted(files.items()):
        out.append(f"\t{o}: {n}")
    map_path.write_text("\n".join(out) + "\n", encoding="utf-8", newline="\n")
    print(f"curated: {len(actors)} actors ({len(additions)} variant additions), "
          f"{len(files)} files")
    for o, n in sorted(additions.items()):
        print(f"  + {o} -> {n}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
