#!/usr/bin/env python3
"""Reject actor-specific shield sizing and invalid resolved shield routes."""

from __future__ import annotations

import math
import re

from cameo_model import Model


ALLOWED_ROUTES = {
    ("hexshield_infantry", "infantry-standard"),
    ("hexshield_sphere", "vehicle-standard"),
    ("hexshield_sphere", "aircraft-standard"),
    ("hexshield_sphere", "naval-standard"),
    ("hexshield_sphere", "large-mobile-standard"),
    ("hexshield_directional_oval", "aircraft-standard"),
}

SHAPE_DIMENSIONS = {
    f"^{x}x{y}shape": (x, y)
    for x, y in (
        (1, 1), (1, 2), (2, 1), (2, 2), (2, 3), (3, 2), (3, 3), (3, 4),
        (3, 5), (4, 2), (4, 3), (4, 4), (4, 5), (5, 3), (5, 4), (5, 5)
    )
}
SHAPE_SEQUENCES = {
    shape: f"dome-{dimensions[0]}x{dimensions[1]}"
    for shape, dimensions in SHAPE_DIMENSIONS.items()
}
ALLOWED_ROUTES.update(("hexshield_dome", sequence) for sequence in SHAPE_SEQUENCES.values())

ALLOWED_PALETTES = {
    ("hexshield25", "hexshield50"),
    ("protosshexshield25", "protosshexshield50"),
    ("ixianhexshield25", "ixianhexshield50"),
    ("yurihexshield25", "yurihexshield50"),
    ("consortiumhexshield25", "consortiumhexshield50"),
}


def main() -> int:
    model = Model()
    errors: list[str] = []
    warnings: list[str] = []
    overlay_actors = 0
    shield_receivers = 0
    dormant_shape_actors = 0
    other_dormant_actors = 0

    for template in ("^BaseBuilding", "^Defense"):
        node = model.rs.actor(template)
        for key in ("WithIdleOverlay@shield1", "WithIdleOverlay@shield_damage"):
            direct = node.child(key) if node else None
            if direct and (direct.get("Image") or direct.get("Sequence") or direct.get("StartSequence")):
                errors.append(f"{template}: building class must not override footprint shield routing")

    dome_root = model.rs.sequence_image("hexshield_dome")
    for shape, (width, height) in SHAPE_DIMENSIONS.items():
        sequence = SHAPE_SEQUENCES[shape]
        resolved_shape = model.rs.resolve(shape)
        idle = resolved_shape.child("WithIdleOverlay@shield1") if resolved_shape else None
        hit = resolved_shape.child("WithIdleOverlay@shield_damage") if resolved_shape else None
        if idle is None or hit is None:
            errors.append(f"{shape}: missing complete shield overlay pair")
            continue
        if (idle.get("Image"), idle.get("Sequence"), idle.get("StartSequence")) != (
                "hexshield_dome", sequence, sequence):
            errors.append(f"{shape}: idle overlay does not resolve {sequence}")
        if (hit.get("Image"), hit.get("Sequence")) != ("hexshield_dome", sequence):
            errors.append(f"{shape}: hit overlay does not resolve {sequence}")
        if idle.get("Palette") or hit.get("Palette"):
            errors.append(f"{shape}: footprint templates must not define palette fields")

        sequence_node = dome_root.child(sequence) if dome_root else None
        if sequence_node is None:
            errors.append(f"{shape}: missing sequence hexshield_dome.{sequence}")
            continue
        expected_scale = math.ceil(max(
            (48 * width + 16) / 261,
            (48 * height + 16) / 222) * 1000) / 1000
        actual_scale = float(sequence_node.get("Scale") or 1)
        if actual_scale != expected_scale:
            errors.append(f"{shape}: scale {actual_scale} != formula result {expected_scale}")
        if sequence_node.get("Offset") != "0.5,-1":
            errors.append(f"{shape}: offset must remain derived center 0.5,-1")

    for actor, node in model.rs.actors.items():
        if actor.startswith("^"):
            continue

        # Concrete actors may choose a different geometry, but sizing must come
        # entirely from their inherited class sequence.
        requires_build_incomplete_guard: set[str] = set()
        for key in ("WithIdleOverlay@shield1", "WithIdleOverlay@shield_damage"):
            direct = node.child(key)
            if direct is None:
                continue
            if direct.get("Sequence") or direct.get("StartSequence"):
                errors.append(f"{actor}: concrete actor defines shield sizing in {key}")
            image = direct.get("Image")
            if image and image != "hexshield_directional_oval":
                errors.append(f"{actor}: unsupported concrete shield geometry {image}")
            if "!build-incomplete" in (direct.get("RequiresCondition") or ""):
                requires_build_incomplete_guard.add(key)

        resolved = model.rs.resolve(actor)
        idle = resolved.child("WithIdleOverlay@shield1") if resolved else None
        hit = resolved.child("WithIdleOverlay@shield_damage") if resolved else None
        if idle is None:
            continue

        overlay_actors += 1
        if hit is None:
            errors.append(f"{actor}: missing hit shield overlay")
            continue
        if resolved.child("RenderSprites") is None:
            errors.append(f"{actor}: shield overlay requires missing RenderSprites")
        if resolved.child("BodyOrientation") is None:
            errors.append(f"{actor}: shield overlay requires missing BodyOrientation")

        if "WithIdleOverlay@shield1" in requires_build_incomplete_guard and \
                "!build-incomplete" not in (idle.get("RequiresCondition") or ""):
            errors.append(f"{actor}: idle overlay lost actor-local !build-incomplete guard")
        if "WithIdleOverlay@shield_damage" in requires_build_incomplete_guard and \
                "!build-incomplete" not in (hit.get("RequiresCondition") or ""):
            errors.append(f"{actor}: hit overlay lost actor-local !build-incomplete guard")

        idle_route = (idle.get("Image") or "", idle.get("Sequence") or "")
        hit_route = (hit.get("Image") or "", hit.get("Sequence") or "")
        if idle_route != hit_route:
            errors.append(f"{actor}: idle/hit route mismatch {idle_route} != {hit_route}")
        if idle_route not in ALLOWED_ROUTES:
            errors.append(f"{actor}: unsupported resolved shield route {idle_route}")

        is_building = resolved.child("Building") is not None
        has_shield = resolved.child("Shielded") is not None
        if is_building:
            if idle_route[0] != "hexshield_dome" or idle_route[1] not in SHAPE_SEQUENCES.values():
                errors.append(f"{actor}: building does not resolve a footprint dome route: {idle_route}")

            # Validate standard ^NxMShape selection boxes against the resolved
            # route. Nonstandard actor-specific bounds remain valid but cannot
            # be translated back to a canonical shape without guessing.
            selectable = resolved.child("Selectable")
            raw_bounds = selectable.get("Bounds") if selectable else None
            values = [int(v) for v in re.findall(r"-?\d+", raw_bounds or "")]
            if len(values) >= 2 and values[0] % 1024 == 0 and values[1] % 1024 == 0:
                shape = f"^{values[0] // 1024}x{values[1] // 1024}shape"
                expected_sequence = SHAPE_SEQUENCES.get(shape)
                if expected_sequence and idle_route != ("hexshield_dome", expected_sequence):
                    warnings.append(
                        f"{actor}: selection-box route mismatch {idle_route} != "
                        f"('hexshield_dome', '{expected_sequence}')")

        if has_shield:
            shield_receivers += 1
        elif is_building:
            dormant_shape_actors += 1
        else:
            other_dormant_actors += 1

        idle_condition = idle.get("RequiresCondition") or ""
        hit_condition = hit.get("RequiresCondition") or ""
        if "shielded" not in idle_condition or "!shieldhit" not in idle_condition:
            errors.append(f"{actor}: idle shield condition lost shielded/!shieldhit gating")
        if "shielded" not in hit_condition or "shieldhit" not in hit_condition:
            errors.append(f"{actor}: hit shield condition lost shielded/shieldhit gating")

        if has_shield:
            palette_pair = (idle.get("Palette") or "", hit.get("Palette") or "")
            if palette_pair not in ALLOWED_PALETTES:
                errors.append(f"{actor}: unsupported shield palettes {palette_pair}")
            if idle.get("IsPlayerPalette") != "false" or hit.get("IsPlayerPalette") != "false":
                errors.append(f"{actor}: shield palettes must be fixed, not player palettes")

        for image, sequence in (idle_route, hit_route):
            root = model.rs.sequence_image(image)
            if root is None or root.child(sequence) is None:
                errors.append(f"{actor}: missing sequence {image}.{sequence}")

    print("# Hex-shield routing audit\n")
    print(f"Resolved shield receivers: **{shield_receivers}**")
    print(f"Dormant non-shield footprint actors: **{dormant_shape_actors}**")
    print(f"Other dormant overlay actors: **{other_dormant_actors}**")
    print(f"Total actors carrying shield overlays: **{overlay_actors}**")
    print(f"Errors: **{len(errors)}**\n")
    for error in errors:
        print(f"- {error}")

    print(f"\nSelection-box consistency warnings: **{len(warnings)}**\n")
    for warning in warnings:
        print(f"- {warning}")

    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
