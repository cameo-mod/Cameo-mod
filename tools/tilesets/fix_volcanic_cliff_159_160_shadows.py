#!/usr/bin/env python
"""Fix oversized gray cast-shadow plates in Volcanic templates 159 and 160."""

from __future__ import annotations

import json
from collections import deque
from pathlib import Path

import numpy as np

from shptd import read_shptd, write_shptd


ROOT = Path(__file__).resolve().parents[2]
BITS = ROOT / "mods/cameo/bits/volcanic"
SOURCE = (
    Path.home()
    / "Documents/agents/volcanic-theater/mapgen-adoption"
    / "cliff-shadow-normalization-01/candidate-vols"
)
ASSETS = ("s25", "s26")
GRAY_SHADOW_INDEX = 10
BLACK_SHADOW_INDEX = 2
MIN_COMPONENT_24PX = 80


def main() -> int:
    records = []
    for name in ASSETS:
        path = BITS / f"{name}.vol"
        source_path = SOURCE / f"{name}.vol"
        width, height, frames = read_shptd(source_path)
        if len(frames) != 4:
            raise ValueError(f"{name}: expected four 2x2 template frames")

        authored_frames = []
        for frame in frames:
            source = np.frombuffer(frame, dtype=np.uint8).reshape(height, width)
            blocks = source.reshape(24, 2, 24, 2).transpose(0, 2, 1, 3)
            if np.any(blocks != blocks[:, :, :1, :1]):
                raise ValueError(f"{name}: source does not have strict 2x cadence")
            authored_frames.append(source[0::2, 0::2].copy())

        # Classify the cast shadow on the complete 2x2 template.  Treating each
        # frame independently broke continuous shadows at internal tile seams.
        assembled = np.block([
            [authored_frames[0], authored_frames[1]],
            [authored_frames[2], authored_frames[3]],
        ])
        changed_total, components, changed_by_frame = replace_large_components(assembled)
        authored_frames = [
            assembled[:24, :24], assembled[:24, 24:],
            assembled[24:, :24], assembled[24:, 24:],
        ]

        output = []
        for authored in authored_frames:
            production = np.repeat(np.repeat(authored, 2, axis=0), 2, axis=1)
            output.append(production.tobytes())

        write_shptd(path, width, height, output)
        audit(path, len(output))
        records.append({
            "asset": name,
            "changed_pixels_24px": changed_total,
            "changed_components": components,
            "changed_pixels_by_frame": changed_by_frame,
        })

    audit_path = (
        Path.home()
        / "Documents/agents/volcanic-theater/mapgen-adoption/cliff-159-160-diagnostic"
        / "cliff_159_160_shadow_fix_audit.json"
    )
    audit_path.parent.mkdir(parents=True, exist_ok=True)
    audit_path.write_text(json.dumps({
        "templates": [159, 160],
        "assets": list(ASSETS),
        "source_index": GRAY_SHADOW_INDEX,
        "replacement_index": BLACK_SHADOW_INDEX,
        "minimum_component_pixels_24px": MIN_COMPONENT_24PX,
        "records": records,
    }, indent=2) + "\n", encoding="utf-8")
    print(audit_path)
    return 0


def replace_large_components(image):
    mask = image == GRAY_SHADOW_INDEX
    visited = np.zeros(mask.shape, dtype=bool)
    changed = 0
    components = []
    changed_by_frame = [0, 0, 0, 0]
    for y in range(mask.shape[0]):
        for x in range(mask.shape[1]):
            if not mask[y, x] or visited[y, x]:
                continue
            queue = deque(((y, x),))
            visited[y, x] = True
            points = []
            while queue:
                cy, cx = queue.popleft()
                points.append((cy, cx))
                for ny, nx in ((cy - 1, cx), (cy + 1, cx), (cy, cx - 1), (cy, cx + 1)):
                    if 0 <= ny < mask.shape[0] and 0 <= nx < mask.shape[1] and mask[ny, nx] and not visited[ny, nx]:
                        visited[ny, nx] = True
                        queue.append((ny, nx))
            if len(points) < MIN_COMPONENT_24PX:
                continue
            for py, px in points:
                image[py, px] = BLACK_SHADOW_INDEX
                frame = (py // 24) * 2 + (px // 24)
                changed_by_frame[frame] += 1
            changed += len(points)
            components.append({
                "pixels_24px": len(points),
                "pixels_by_frame": [
                    sum(1 for py, px in points if (py // 24) * 2 + (px // 24) == frame)
                    for frame in range(4)
                ],
            })
    return changed, components, changed_by_frame


def audit(path, expected_frames):
    width, height, frames = read_shptd(path)
    if (width, height, len(frames)) != (48, 48, expected_frames):
        raise ValueError(f"{path.name}: metadata mismatch")
    for frame in frames:
        image = np.frombuffer(frame, dtype=np.uint8).reshape(48, 48)
        blocks = image.reshape(24, 2, 24, 2).transpose(0, 2, 1, 3)
        if np.any(blocks != blocks[:, :, :1, :1]):
            raise ValueError(f"{path.name}: cadence failure")


if __name__ == "__main__":
    raise SystemExit(main())
