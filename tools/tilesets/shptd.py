#!/usr/bin/env python
"""Minimal ShpTD + LCW helpers for Cameo terrain prototype art."""

from __future__ import annotations

import struct
from pathlib import Path
from typing import Iterable, Sequence


def lcw_encode(src: bytes) -> bytes:
    out = bytearray()
    offset = 0
    block_start = 0

    while offset < len(src):
        first = src[offset]
        repeat_count = 1
        while offset + repeat_count < len(src) and repeat_count < 0xFFFF:
            if src[offset + repeat_count] != first:
                break
            repeat_count += 1

        if repeat_count >= 4:
            _write_copy_blocks(src, block_start, offset - block_start, out)
            out.extend((0xFE, repeat_count & 0xFF, repeat_count >> 8, first))
            offset += repeat_count
            block_start = offset
        else:
            offset += 1

    _write_copy_blocks(src, block_start, offset - block_start, out)
    out.append(0x80)
    return bytes(out)


def _write_copy_blocks(src: bytes, offset: int, count: int, out: bytearray) -> None:
    while count > 0:
        write_now = min(count, 0x3F)
        out.append(0x80 | write_now)
        out.extend(src[offset : offset + write_now])
        count -= write_now
        offset += write_now


def lcw_decode(src: bytes, expected_size: int, src_offset: int = 0) -> bytes:
    dest = bytearray(expected_size)
    src_index = src_offset
    dest_index = 0

    while True:
        op = src[src_index]
        src_index += 1

        if (op & 0x80) == 0:
            second = src[src_index]
            src_index += 1
            count = ((op & 0x70) >> 4) + 3
            rpos = ((op & 0x0F) << 8) + second
            copy_from = dest_index - rpos
            for _ in range(count):
                dest[dest_index] = dest[copy_from]
                dest_index += 1
                copy_from += 1
        elif (op & 0x40) == 0:
            count = op & 0x3F
            if count == 0:
                return bytes(dest[:dest_index])
            dest[dest_index : dest_index + count] = src[src_index : src_index + count]
            src_index += count
            dest_index += count
        else:
            count3 = op & 0x3F
            if count3 == 0x3E:
                count = src[src_index] | (src[src_index + 1] << 8)
                color = src[src_index + 2]
                src_index += 3
                dest[dest_index : dest_index + count] = bytes([color]) * count
                dest_index += count
            else:
                count = (src[src_index] | (src[src_index + 1] << 8)) if count3 == 0x3F else count3 + 3
                if count3 == 0x3F:
                    src_index += 2
                copy_from = src[src_index] | (src[src_index + 1] << 8)
                src_index += 2
                for _ in range(count):
                    dest[dest_index] = dest[copy_from]
                    dest_index += 1
                    copy_from += 1


def write_shptd(path: str | Path, width: int, height: int, frames: Iterable[bytes]) -> None:
    frame_list = list(frames)
    frame_size = width * height
    if any(len(frame) != frame_size for frame in frame_list):
        raise ValueError(f"all frames must be {frame_size} bytes")

    compressed = [lcw_encode(frame) for frame in frame_list]
    data_offset = 14 + (len(compressed) + 2) * 8

    with Path(path).open("wb") as stream:
        stream.write(struct.pack("<HHHHHI", len(compressed), 0, 0, width, height, 0))

        offset = data_offset
        for frame in compressed:
            stream.write(struct.pack("<IHH", offset | (0x80 << 24), 0, 0))
            offset += len(frame)

        stream.write(struct.pack("<IHH", offset, 0, 0))
        stream.write(struct.pack("<IHH", 0, 0, 0))

        for frame in compressed:
            stream.write(frame)


def read_shptd(path: str | Path) -> tuple[int, int, list[bytes]]:
    data = Path(path).read_bytes()
    count, _, _, width, height, _ = struct.unpack_from("<HHHHHI", data, 0)
    headers_offset = 14
    frame_size = width * height
    frames: list[bytes] = []

    for i in range(count):
        packed_offset, _, _ = struct.unpack_from("<IHH", data, headers_offset + i * 8)
        fmt = packed_offset >> 24
        offset = packed_offset & 0x00FFFFFF
        if fmt != 0x80:
            raise ValueError(f"unsupported ShpTD frame format 0x{fmt:02x}")
        frames.append(lcw_decode(data, frame_size, offset))

    return width, height, frames


def write_pal(path: str | Path, colors: Sequence[tuple[int, int, int]]) -> None:
    if len(colors) != 256:
        raise ValueError("palettes must contain exactly 256 RGB colors")

    out = bytearray()
    for r, g, b in colors:
        out.extend((min(63, max(0, r // 4)), min(63, max(0, g // 4)), min(63, max(0, b // 4))))

    Path(path).write_bytes(out)
