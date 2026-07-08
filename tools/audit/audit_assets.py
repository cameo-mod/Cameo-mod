#!/usr/bin/env python3
"""audit_assets.py — B11 detector (asset format regressions; RAMpage norms).

  P1 PNG dimension budget per category (sprite sheets can be wide/tall but a
     single frame budget can't be checked without frame metadata — so the
     screen uses generous whole-file byte + dimension caps)
  W1 WAV format: mono / 16-bit / 22050 Hz per the RAMpage norm; report
     non-conforming files with the exact conversion command.

Reads only file headers — fast enough for pre-commit.
"""

from __future__ import annotations

import struct
import sys

from cameo_model import Model
from report import h1, h2, relpath, table

PNG_MAX_BYTES = 8 * 1024 * 1024      # generous whole-file cap
PNG_MAX_DIM = 8192
WAV_NORM = (1, 16, 22050)             # channels, bits, rate


def png_size(path) -> tuple[int, int] | None:
    try:
        with open(path, "rb") as f:
            head = f.read(26)
        if head[:8] != b"\x89PNG\r\n\x1a\n":
            return None
        w, h = struct.unpack(">II", head[16:24])
        return w, h
    except OSError:
        return None


def wav_format(path) -> tuple[int, int, int] | None:
    try:
        with open(path, "rb") as f:
            riff = f.read(12)
            if riff[:4] != b"RIFF" or riff[8:12] != b"WAVE":
                return None
            while True:
                hdr = f.read(8)
                if len(hdr) < 8:
                    return None
                cid, size = hdr[:4], struct.unpack("<I", hdr[4:])[0]
                if cid == b"fmt ":
                    fmt = f.read(min(size, 16))
                    channels = struct.unpack("<H", fmt[2:4])[0]
                    rate = struct.unpack("<I", fmt[4:8])[0]
                    bits = struct.unpack("<H", fmt[14:16])[0]
                    return channels, bits, rate
                f.seek(size + (size & 1), 1)
    except OSError:
        return None


def main() -> int:
    m = Model()
    bits = m.root / "mods/cameo/bits"
    png_rows, wav_rows = [], []
    n_png = n_wav = 0

    for p in bits.rglob("*.png"):
        n_png += 1
        size = p.stat().st_size
        dims = png_size(p)
        if size > PNG_MAX_BYTES or (dims and max(dims) > PNG_MAX_DIM):
            png_rows.append([relpath(str(p), m.root),
                             f"{size//1024} KiB",
                             f"{dims[0]}x{dims[1]}" if dims else "?"])
    for p in list(bits.rglob("*.wav")) + list(bits.rglob("*.WAV")):
        n_wav += 1
        fmt = wav_format(p)
        if fmt is None:
            continue
        if fmt != WAV_NORM:
            fix = (f"ffmpeg -i \"{p.name}\" -ac 1 -ar 22050 "
                   f"-acodec pcm_s16le \"{p.name}\"")
            wav_rows.append([relpath(str(p), m.root),
                             f"{fmt[0]}ch/{fmt[1]}bit/{fmt[2]}Hz", fix])

    print(h1("audit_assets — asset format norms (B11, RAMpage)"))
    print(f"PNGs scanned: **{n_png}** (over budget: **{len(png_rows)}**), "
          f"WAVs scanned: **{n_wav}** (non-conforming: **{len(wav_rows)}**)\n")
    print(h2(f"P1 — PNGs over budget (> {PNG_MAX_BYTES//1024//1024} MiB or > {PNG_MAX_DIM}px)"))
    print(table(["file", "size", "dimensions"], png_rows))
    print(h2("W1 — WAV norm compliance by directory (mono/16-bit/22050 Hz)"))
    by_dir: dict[str, int] = {}
    for row in wav_rows:
        d = "/".join(row[0].split("/")[:4])
        by_dir[d] = by_dir.get(d, 0) + 1
    print(table(["directory", "non-conforming WAVs"],
                sorted(([d, str(c)] for d, c in by_dir.items()),
                       key=lambda r: -int(r[1]))))
    print(h2("W1 — sample violations with conversion commands"))
    print(table(["file", "format", "conversion"], wav_rows[:25]))
    print(f"\n_Total non-conforming: {len(wav_rows)}. A count this large means "
          "the RAMpage WAV norm has not yet been applied tree-wide; treat as "
          "one batch-conversion task per directory, not per-file bugs._\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
