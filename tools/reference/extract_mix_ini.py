#!/usr/bin/env python3
"""extract_mix_ini.py — pull INI files out of an unencrypted Westwood MIX archive.

Why this exists: Mental Omega ships its real balance inside `expandmo*.mix`, and the
loose `rulesmd.ini` sitting next to them is **vanilla Yuri's Revenge, byte for byte**
(verified: both files md5 `cf7eb658327aff1fe7e6c4e7400eb87f`, 31061 lines). Harvesting
that loose file gives you vanilla YR counted twice and zero Mental Omega data.

MIX layout (RA2/YR "new" header):

    uint16 zero            # 0 marks the new format
    uint16 flags           # 0x0001 encrypted, 0x0002 checksum
    uint16 count           # number of entries
    uint32 data_size
    count x { int32 id; uint32 offset; uint32 size }   # the index
    ... file data ...

Entries are keyed by a CRC of the uppercased filename, not by the name itself. Rather
than reimplementing Westwood's CRC and guessing filenames, this **sniffs**: every blob
is checked for INI-looking text with the markers we actually care about. That finds the
rules regardless of what they are called inside the archive.

Encrypted MIXes (flags & 1) are refused rather than half-handled — `ra2md.mix` is one,
and it is not where the MO overrides live.

Usage:
    python tools/reference/extract_mix_ini.py <mix> [<mix>...] --out <dir>
    python tools/reference/extract_mix_ini.py <mix> --list      # probe only
"""

from __future__ import annotations

import argparse
import pathlib
import struct
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

# A blob is worth keeping if it looks like an INI AND carries something we want.
MARKERS = (b"Verses=", b"[General]", b"[Warhead", b"Damage=", b"[CombatDamage]")
MAX_SNIFF = 4096          # only the head of a blob is inspected


def read_index(fh) -> tuple[list[tuple[int, int, int]], int]:
    """(entries, data_start). Raises on an encrypted archive."""
    head = fh.read(4)
    if len(head) < 4:
        raise ValueError("file too short")
    zero, flags = struct.unpack("<HH", head)
    if zero != 0:
        # Old-format archive: count/size sit at offset 0.
        count, _size = struct.unpack("<HI", head + fh.read(2))
        body = 6
    else:
        if flags & 0x0001:
            raise ValueError(f"encrypted archive (flags 0x{flags:04x}) — not supported")
        count, _size = struct.unpack("<HI", fh.read(6))
        body = 10
    entries = []
    for _ in range(count):
        raw = fh.read(12)
        if len(raw) < 12:
            break
        entries.append(struct.unpack("<iII", raw))
    return entries, body + count * 12


def looks_like_ini(blob: bytes) -> bool:
    head = blob[:MAX_SNIFF]
    if b"\x00\x00\x00" in head[:64]:          # binary asset
        return False
    return any(m in head for m in MARKERS)


def extract(path: pathlib.Path, out_dir: pathlib.Path | None, list_only: bool) -> int:
    with path.open("rb") as fh:
        try:
            entries, data_start = read_index(fh)
        except ValueError as exc:
            print(f"{path.name}: SKIP — {exc}")
            return 0
        print(f"{path.name}: {len(entries)} entries, data at {data_start}")
        found = 0
        for ident, offset, size in entries:
            if size < 512 or size > 40 * 1024 * 1024:
                continue
            fh.seek(data_start + offset)
            blob = fh.read(min(size, MAX_SNIFF))
            if not looks_like_ini(blob):
                continue
            fh.seek(data_start + offset)
            full = fh.read(size)
            verses = full.count(b"Verses=")
            name = f"{path.stem}_{ident & 0xFFFFFFFF:08x}.ini"
            print(f"  INI blob {name}  {size:>9,} bytes  Verses={verses}")
            found += 1
            if not list_only and out_dir is not None:
                out_dir.mkdir(parents=True, exist_ok=True)
                (out_dir / name).write_bytes(full)
        return found


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("mix", nargs="+")
    ap.add_argument("--out", help="directory to write extracted INIs into")
    ap.add_argument("--list", action="store_true", dest="list_only")
    args = ap.parse_args()

    out_dir = pathlib.Path(args.out) if args.out else None
    total = 0
    for name in args.mix:
        path = pathlib.Path(name)
        if not path.exists():
            print(f"{name}: MISSING")
            continue
        total += extract(path, out_dir, args.list_only)
    print(f"\n{total} INI blob(s) found")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
