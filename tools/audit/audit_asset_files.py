#!/usr/bin/env python3
"""audit_asset_files.py — crash-class detector: renamed refs to missing files.

  A1 sequence `Filename:` values that do not exist as loose files but whose
     reverse-mapped pre-rename name DOES exist -> a rename rewrote the
     reference without moving the file (cabal_dissolver-weapon.shp class)
  A2 voxel entries in sequences/voxels.yaml (explicit or key-derived
     `<key>.vxl`) with no matching loose .vxl (TS voxel class: rename maps
     moved sprites/icons but voxel filenames are implicit in the key)
  A3 informational: file references missing loose with no rename mapping
     (may live inside archives; verify before touching)

Run after EVERY rename pass. A1/A2 findings are launch crashes.
"""

from __future__ import annotations

import pathlib
import re

from report import h1, h2, table

ROOT = pathlib.Path(__file__).resolve().parents[2]
MOD = ROOT / "mods/cameo"


def rename_pairs() -> list[tuple[str, str]]:
    pairs = []
    for mp in (ROOT / "tools/rename").glob("rename_map_*.yaml"):
        in_actors = False
        for ln in mp.read_text(encoding="utf-8-sig").splitlines():
            if re.match(r"^actors:", ln):
                in_actors = True
                continue
            if re.match(r"^[a-z]+:", ln):
                in_actors = False
                continue
            if in_actors:
                mo = re.match(r"\t([\w.\-]+):\s*([\w.\-]+)", ln)
                if mo:
                    pairs.append((mo.group(1).lower(), mo.group(2).lower()))
    pairs.sort(key=lambda p: -len(p[1]))
    return pairs


WORD = set("abcdefghijklmnopqrstuvwxyz0123456789_")


def reverse_name(name: str, pairs) -> str | None:
    low = name.lower()
    for old, new in pairs:
        i = low.find(new)
        if i < 0:
            continue
        before = low[i - 1] if i > 0 else "\0"
        j = i + len(new)
        after = low[j] if j < len(low) else "\0"
        if before not in WORD and after not in WORD:
            return low[:i] + old + low[j:]
    return None


def main() -> int:
    pairs = rename_pairs()
    loose = set()
    for p in MOD.rglob("*"):
        if p.is_file():
            loose.add(p.name.lower())

    a1, a3 = [], []
    seq_files = list((MOD / "sequences").glob("*.yaml"))
    seq_files += list(MOD.glob("ContentPacks/*/*/sequences/*.yaml"))
    for sf in seq_files:
        rel = sf.relative_to(ROOT).as_posix()
        for i, ln in enumerate(
                sf.read_text(encoding="utf-8-sig").split("\n"), 1):
            mo = re.match(r"\s*Filename:\s*([\w.\-]+)\s*$", ln)
            if not mo:
                continue
            ref = mo.group(1)
            if ref.lower() in loose:
                continue
            old = reverse_name(ref, pairs)
            if old and old != ref.lower() and old in loose:
                a1.append([f"{rel}:{i}", ref, old])
            else:
                a3.append([f"{rel}:{i}", ref])

    a2 = []
    vxp = MOD / "sequences/voxels.yaml"
    top = None
    for i, ln in enumerate(
            vxp.read_text(encoding="utf-8-sig").split("\n"), 1):
        if ln and ln[0] not in "\t #" and ":" in ln:
            top = ln.split(":")[0].strip()
        mo = re.match(r"\t([\w.\-]+):\s*([\w.\-]*)\s*$", ln)
        if mo and top:
            name = (mo.group(2) or top).lower()
            if name + ".vxl" not in loose:
                a2.append([f"sequences/voxels.yaml:{i}", top, name + ".vxl"])

    print(h1("Asset file references (rename crash class)"))
    print(f"A1 rename-broken sprite refs: {len(a1)}, "
          f"A2 missing voxels: {len(a2)}, "
          f"A3 missing without rename mapping (informational): {len(a3)}\n")
    print(h2(f"A1 — reference renamed, file not moved ({len(a1)}) — CRASH"))
    print(table(["location", "referenced", "file on disk"], a1))
    print(h2(f"A2 — voxel file missing ({len(a2)}) — CRASH"))
    print(table(["location", "image", "expected file"], a2))
    print(h2(f"A3 — missing loose, no rename mapping ({len(a3)})"))
    print(table(["location", "referenced"], a3))
    return 1 if (a1 or a2) else 0


if __name__ == "__main__":
    import sys
    sys.exit(main())
