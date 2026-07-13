#!/usr/bin/env python
"""Donor-independent palette and procedural helpers for Volcanic tile tools."""

from __future__ import annotations

import math


TILE = 48
CLEAR_BASE_SEED = 0xC1EA1200


def clamp(value: int, low: int, high: int) -> int:
    return max(low, min(high, value))


def fill_gradient(
    colors: list[tuple[int, int, int]],
    start: int,
    end: int,
    a: tuple[int, int, int],
    b: tuple[int, int, int],
) -> None:
    span = max(1, end - start)
    for i in range(start, end + 1):
        t = (i - start) / span
        colors[i] = tuple(int(a[c] + (b[c] - a[c]) * t) for c in range(3))


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
        colors[i] = (int(26 + 100 * t), int(20 + 52 * t), int(20 + 42 * t))

    return colors


def noise(x: int, y: int, seed: int) -> int:
    v = (x * 374761393 + y * 668265263 + seed * 1442695040888963407) & 0xFFFFFFFF
    v ^= v >> 13
    v = (v * 1274126177) & 0xFFFFFFFF
    return (v ^ (v >> 16)) & 0xFF


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
