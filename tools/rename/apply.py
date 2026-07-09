#!/usr/bin/env python3
"""apply.py — mechanically apply a rename map (MASTER_REPORT §9.6 step 3).

Usage:  python tools/rename/apply.py tools/rename/rename_map_<faction>.yaml

- `actors:` entries are replaced as whole identifiers (case-insensitive,
  longest-first, boundaries exclude [A-Za-z0-9_.]) across every YAML/FTL/Lua
  file under mods/cameo (ContentPacks included), loose map files, and the
  members of every .oramap zip. New ids are written lowercase.
- `files:` entries are git-mv'd under mods/cameo/bits and the filename
  strings rewritten in all YAML.

Prints per-area change counts. Run the audit suite afterwards and compare
dump_resolved before/after (with the map applied to the before-names) to
prove behavior preservation.
"""

from __future__ import annotations

import io
import pathlib
import re
import subprocess
import sys
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[2]
MOD = ROOT / "mods/cameo"


def load_map(path: pathlib.Path) -> tuple[dict[str, str], dict[str, str]]:
    actors: dict[str, str] = {}
    files: dict[str, str] = {}
    section = None
    for line in path.read_text(encoding="utf-8").splitlines():
        s = line.strip()
        if not s or s.startswith("#"):
            continue
        if s == "actors:":
            section = actors
            continue
        if s == "files:":
            section = files
            continue
        if section is not None and ": " in s:
            old, _, new = s.partition(": ")
            section[old.strip()] = new.strip()
    return actors, files


def build_replacer(actors: dict[str, str]):
    lower = {k.lower(): v.lower() for k, v in actors.items()}
    alts = sorted(lower, key=len, reverse=True)
    rx = re.compile(
        r"(?<![A-Za-z0-9_.])(" + "|".join(re.escape(a) for a in alts) +
        r")(?![A-Za-z0-9_.])", re.IGNORECASE)

    def sub(text: str) -> tuple[str, int]:
        n = 0

        def repl(mo: re.Match) -> str:
            nonlocal n
            n += 1
            return lower[mo.group(1).lower()]
        return rx.sub(repl, text), n
    return sub


def main() -> int:
    if len(sys.argv) != 2:
        print(__doc__)
        return 2
    map_path = pathlib.Path(sys.argv[1])
    actors, files = load_map(map_path)
    sub = build_replacer(actors)
    print(f"map: {len(actors)} actor ids, {len(files)} files")

    # ---- text replacement across the tree --------------------------------- #
    n_files = n_hits = 0
    targets: list[pathlib.Path] = []
    for pat in ("**/*.yaml", "**/*.ftl", "**/*.lua"):
        targets += [p for p in MOD.glob(pat) if p.is_file()]
    for p in sorted(set(targets)):
        text = p.read_text(encoding="utf-8-sig", errors="replace")
        new, n = sub(text)
        # filename strings (files map) in yaml — boundary-safe so a new name
        # containing the old stem can never be re-matched (idempotent)
        fn = 0
        if p.suffix == ".yaml":
            for old, newf in files.items():
                rx = re.compile(r"(?<![A-Za-z0-9_.])" + re.escape(old)
                                + r"(?![A-Za-z0-9_.])")
                new, k = rx.subn(newf, new)
                fn += k
        if n or fn:
            p.write_text(new, encoding="utf-8", newline="\n")
            n_files += 1
            n_hits += n + fn
    print(f"text: {n_hits} replacements in {n_files} files")

    # ---- .oramap zips ------------------------------------------------------ #
    n_maps = 0
    for zpath in sorted(MOD.glob("maps/**/*.oramap")):
        with zipfile.ZipFile(zpath) as zf:
            members = {i.filename: zf.read(i.filename) for i in zf.infolist()}
        changed = False
        for name, data in list(members.items()):
            if not name.lower().endswith((".yaml", ".lua", ".txt")):
                continue
            try:
                text = data.decode("utf-8-sig")
            except UnicodeDecodeError:
                continue
            new, n = sub(text)
            if n:
                members[name] = new.encode("utf-8")
                changed = True
        if changed:
            buf = io.BytesIO()
            with zipfile.ZipFile(buf, "w", zipfile.ZIP_DEFLATED) as zf:
                for name, data in members.items():
                    zf.writestr(name, data)
            zpath.write_bytes(buf.getvalue())
            n_maps += 1
    print(f"maps: {n_maps} .oramap files rewritten")

    # ---- asset file renames ------------------------------------------------ #
    bits = MOD / "bits"
    on_disk: dict[str, pathlib.Path] = {}
    for p in bits.rglob("*"):
        if p.is_file():
            on_disk.setdefault(p.name.lower(), p)
    n_mv = 0
    for old, new in sorted(files.items()):
        if old.lower() == new.lower():
            continue    # already compliant
        src = on_disk.get(old.lower())
        if src is None:
            print(f"  WARN: {old} not found on disk")
            continue
        dst = src.with_name(new)
        subprocess.run(["git", "mv", str(src), str(dst)], cwd=ROOT, check=True)
        n_mv += 1
    print(f"assets: {n_mv} files git-mv'd")
    return 0


if __name__ == "__main__":
    sys.exit(main())
