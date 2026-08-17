#!/usr/bin/env python3
"""Calculate reusable hex-shield scale and sequence offset from measured actor bounds.

Actor bounds must come from OpenRA.Utility --measure-actor-sprite-bounds so PNG,
SHP, sequence scale, frame offsets, body-state sequences, and facings use the same
loader as the game. This script owns only the fitting policy and YAML arithmetic.
It reports the audit and can regenerate the centralized fitted-sequence YAML;
actor YAML is never generated or patched one-by-one.
"""

from __future__ import annotations

import argparse
import json
import math
import pathlib
import sys
from dataclasses import asdict, dataclass

from PIL import Image

from cameo_model import Model


@dataclass(frozen=True)
class Profile:
    asset: str
    padding_x: float
    padding_y: float
    minimum_scale: float = 0.0
    image: str = ""
    length: int = 8
    facings: int = 1


PROFILES = {
    # Padding is screen pixels on each side. The dome minimum reproduces the
    # approved Photon Cannon calibration; larger domes grow from model bounds.
    "sphere": Profile("hexshield_sphere.png", 4.0, 4.0,
                      image="hexshield_fit_sphere"),
    "infantry": Profile("hexshield_infantry.png", 2.0, 3.0,
                        image="hexshield_fit_infantry"),
    "oval": Profile("hexshield_directional_oval.png", 10.0, 8.0,
                    image="hexshield_fit_directional_oval", length=4, facings=32),
    "dome": Profile("hexshield_dome_master.png", 15.0, 8.0, 0.369,
                    image="hexshield_fit_dome"),
}


@dataclass
class FitResult:
    actor: str
    geometry: str
    actor_bounds: dict[str, float]
    master_bounds: dict[str, float]
    current_sequence: str
    current_scale: float
    current_offset: list[float]
    current_covers_model: bool
    recommended_scale: float
    recommended_offset: list[float]
    recommended_bounds: dict[str, float]
    excess_width: float
    excess_height: float
    note: str = ""


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("bounds", nargs="*", type=pathlib.Path,
                        help="JSON output from --measure-actor-sprite-bounds")
    parser.add_argument("--actors-out", type=pathlib.Path,
                        help="write every concrete actor routed to a new shield visual")
    parser.add_argument("--json-out", type=pathlib.Path,
                        help="write the complete fit report as JSON")
    parser.add_argument("--sequences-out", type=pathlib.Path,
                        help="write generated per-actor fitted sequences")
    return parser.parse_args()


def load_measurements(paths: list[pathlib.Path]) -> dict[str, dict]:
    actors: dict[str, dict] = {}
    for path in paths:
        payload = json.loads(path.read_text(encoding="utf-8"))
        for actor in payload.get("Actors", []):
            actors[actor["Actor"].lower()] = actor
    return actors


def geometry_for(image: str, sequence: str) -> str | None:
    image = image.lower()
    sequence = sequence.lower()
    if image in {"hexshield_sphere", "hexshield_fit_sphere"}:
        return "sphere"
    if image in {"hexshield_directional_oval", "hexshield_fit_directional_oval"}:
        return "oval"
    if image in {"protoss_structure_hexshields", "hexshield_dome", "hexshield_fit_dome"}:
        return "dome"
    if image == "hexshield_fit_infantry" or (image == "energyshields" and
            (sequence.startswith("fit-") or sequence in {
            "shield-complete-infantry", "shield-starts-infantry"})):
        return "infantry"
    return None


def desired_geometry(actor: str, resolved, image: str, sequence: str) -> str:
    # Shape policy is based on the receiver, not on whichever legacy shield art
    # it happened to reference. Explicit directional art remains an exception.
    current = geometry_for(image, sequence)
    if current is not None:
        return current
    if actor == "steelconsortium_cloudbreaker":
        return "oval"
    if resolved.child("Building") is not None:
        return "dome"
    if resolved.child("WithInfantryBody") is not None:
        return "infantry"
    return "sphere"


def parse_pair(raw: str | None) -> tuple[float, float]:
    if not raw:
        return 0.0, 0.0
    values = [float(v.strip()) for v in raw.split(",")]
    if len(values) < 2:
        raise ValueError(f"expected X,Y pair, got {raw!r}")
    return values[0], values[1]


def sequence_settings(model: Model, actor: str, image: str,
                      sequence: str) -> tuple[float, tuple[float, float]]:
    sequence = sequence.replace("{actor}", actor)
    node = model.rs.sequence_image(image)
    if node is None:
        raise KeyError(f"sequence image {image!r} is not active")
    seq = node.child(sequence)
    if seq is None:
        raise KeyError(f"sequence {image}.{sequence} is not active")
    defaults = node.child("Defaults")
    scale_raw = seq.get("Scale") or (defaults.get("Scale") if defaults else None)
    offset_raw = seq.get("Offset") or (defaults.get("Offset") if defaults else None)
    return float(scale_raw or 1.0), parse_pair(offset_raw)


def visible_master_bounds(path: pathlib.Path) -> dict[str, float]:
    with Image.open(path) as image:
        try:
            frame_w, frame_h = (int(v) for v in image.info["FrameSize"].split(","))
            frame_count = int(image.info["FrameAmount"])
        except (KeyError, ValueError) as exc:
            raise ValueError(f"{path} lacks valid FrameSize/FrameAmount metadata") from exc

        columns = image.width // frame_w
        boxes: list[tuple[float, float, float, float]] = []
        for frame in range(frame_count):
            x = frame % columns * frame_w
            y = frame // columns * frame_h
            box = image.crop((x, y, x + frame_w, y + frame_h)).getbbox()
            if box:
                boxes.append((
                    box[0] - frame_w / 2,
                    box[1] - frame_h / 2,
                    box[2] - frame_w / 2,
                    box[3] - frame_h / 2,
                ))

    if not boxes:
        raise ValueError(f"{path} contains no visible shield pixels")
    left = min(b[0] for b in boxes)
    top = min(b[1] for b in boxes)
    right = max(b[2] for b in boxes)
    bottom = max(b[3] for b in boxes)
    return bounds_dict(left, top, right, bottom)


def visible_master_facing_bounds(path: pathlib.Path, length: int,
                                 facings: int) -> list[dict[str, float]]:
    with Image.open(path) as image:
        frame_w, frame_h = (int(v) for v in image.info["FrameSize"].split(","))
        frame_count = int(image.info["FrameAmount"])
        if frame_count != length * facings:
            raise ValueError(
                f"{path} has {frame_count} frames; expected {length * facings}"
            )

        columns = image.width // frame_w
        result: list[dict[str, float]] = []
        for facing in range(facings):
            boxes: list[tuple[float, float, float, float]] = []
            for animation_frame in range(length):
                frame = facing * length + animation_frame
                x = frame % columns * frame_w
                y = frame // columns * frame_h
                box = image.crop((x, y, x + frame_w, y + frame_h)).getbbox()
                if box:
                    boxes.append((
                        box[0] - frame_w / 2,
                        box[1] - frame_h / 2,
                        box[2] - frame_w / 2,
                        box[3] - frame_h / 2,
                    ))
            if not boxes:
                raise ValueError(f"{path} facing {facing} has no visible pixels")
            result.append(bounds_dict(
                min(box[0] for box in boxes),
                min(box[1] for box in boxes),
                max(box[2] for box in boxes),
                max(box[3] for box in boxes),
            ))
        return result


def bounds_dict(left: float, top: float, right: float, bottom: float) -> dict[str, float]:
    return {
        "left": left,
        "top": top,
        "right": right,
        "bottom": bottom,
        "width": right - left,
        "height": bottom - top,
        "center_x": (left + right) / 2,
        "center_y": (top + bottom) / 2,
    }


def normalize_actor_bounds(raw: dict) -> dict[str, float]:
    return bounds_dict(float(raw["Left"]), float(raw["Top"]),
                       float(raw["Right"]), float(raw["Bottom"]))


def union_bounds(*items: dict[str, float]) -> dict[str, float]:
    return bounds_dict(
        min(item["left"] for item in items),
        min(item["top"] for item in items),
        max(item["right"] for item in items),
        max(item["bottom"] for item in items),
    )


def conservative_selection_bounds(resolved, geometry: str) -> dict[str, float]:
    selectable = resolved.child("Selectable")
    raw = None if selectable is None else (
        selectable.get("DecorationBounds") or selectable.get("Bounds"))
    defaults = {
        "sphere": (1024, 1024, 0, 0),
        "infantry": (512, 1024, 0, 0),
        "oval": (2048, 1024, 0, 0),
        "dome": (2048, 1536, 0, -256),
    }
    values = list(defaults[geometry])
    if raw:
        parsed = [int(v.strip()) for v in raw.split(",")]
        values[:len(parsed)] = parsed

    # Cameo's default terrain uses 48 px tiles and a 1024 world-unit tile scale.
    width, height, center_x, center_y = (v * 48 / 1024 for v in values)
    return bounds_dict(center_x - width / 2, center_y - height / 2,
                       center_x + width / 2, center_y + height / 2)


def placed_bounds(master: dict[str, float], scale: float,
                  offset: tuple[float, float]) -> dict[str, float]:
    ox, oy = offset
    return bounds_dict(
        scale * (master["left"] + ox),
        scale * (master["top"] + oy),
        scale * (master["right"] + ox),
        scale * (master["bottom"] + oy),
    )


def covers(outer: dict[str, float], inner: dict[str, float], tolerance: float = 0.01) -> bool:
    return (outer["left"] <= inner["left"] + tolerance
            and outer["top"] <= inner["top"] + tolerance
            and outer["right"] >= inner["right"] - tolerance
            and outer["bottom"] >= inner["bottom"] - tolerance)


def fit(actor: str, actor_bounds: dict[str, float], geometry: str,
        master: dict[str, float], sequence: str, current_scale: float,
        current_offset: tuple[float, float], source_note: str = "") -> FitResult:
    profile = PROFILES[geometry]
    scale = max(
        profile.minimum_scale,
        (actor_bounds["width"] + 2 * profile.padding_x) / master["width"],
        (actor_bounds["height"] + 2 * profile.padding_y) / master["height"],
    )
    # Round upward, never downward: a three-decimal YAML value must not reintroduce leakage.
    scale = math.ceil(scale * 1000 - 1e-9) / 1000
    offset = (
        actor_bounds["center_x"] / scale - master["center_x"],
        actor_bounds["center_y"] / scale - master["center_y"],
    )
    offset = tuple(round(v, 3) for v in offset)
    recommended = placed_bounds(master, scale, offset)
    current = placed_bounds(master, current_scale, current_offset)
    excess_width = recommended["width"] - actor_bounds["width"]
    excess_height = recommended["height"] - actor_bounds["height"]
    note = source_note
    if geometry == "infantry" and (excess_width > actor_bounds["width"] * 0.5
                                    or excess_height > actor_bounds["height"] * 0.5):
        mismatch = "geometry aspect mismatch; consider another master shape"
        note = f"{note}; {mismatch}" if note else mismatch

    return FitResult(
        actor=actor,
        geometry=geometry,
        actor_bounds=actor_bounds,
        master_bounds=master,
        current_sequence=sequence,
        current_scale=current_scale,
        current_offset=[current_offset[0], current_offset[1]],
        current_covers_model=covers(current, actor_bounds),
        recommended_scale=scale,
        recommended_offset=[offset[0], offset[1]],
        recommended_bounds=recommended,
        excess_width=round(excess_width, 3),
        excess_height=round(excess_height, 3),
        note=note,
    )


def directional_actor_facing_bounds(measured: dict) -> list[dict[str, float]] | None:
    for component in measured.get("Components", []):
        if "Body" not in component.get("Trait", ""):
            continue
        facings = component.get("FacingBounds") or []
        if len(facings) > 1 and all(facing.get("HasPixels") for facing in facings):
            return [normalize_actor_bounds(facing) for facing in facings]
    return None


def fit_directional(actor: str, actor_bounds: dict[str, float],
                    actor_facings: list[dict[str, float]],
                    master: dict[str, float],
                    master_facings: list[dict[str, float]], sequence: str,
                    current_scale: float, current_offset: tuple[float, float],
                    source_note: str = "") -> FitResult:
    profile = PROFILES["oval"]
    shield_count = len(master_facings)
    actor_count = len(actor_facings)
    pairs: list[tuple[dict[str, float], dict[str, float]]] = []
    for facing, actor_facing in enumerate(actor_facings):
        angle = facing * 1024 // actor_count
        step = 1024 // shield_count
        shield_facing = ((angle + step // 2) & 1023) // step
        pairs.append((actor_facing, master_facings[shield_facing]))

    def interval(scale: float, low_key: str, high_key: str,
                 padding: float) -> tuple[float, float]:
        lower = max(
            actor_facing[high_key] + padding - scale * shield_facing[high_key]
            for actor_facing, shield_facing in pairs
        )
        upper = min(
            actor_facing[low_key] - padding - scale * shield_facing[low_key]
            for actor_facing, shield_facing in pairs
        )
        return lower, upper

    def feasible(scale: float) -> bool:
        x_lower, x_upper = interval(scale, "left", "right", profile.padding_x)
        y_lower, y_upper = interval(scale, "top", "bottom", profile.padding_y)
        return x_lower <= x_upper and y_lower <= y_upper

    low = profile.minimum_scale
    high = max(1.0, current_scale, low)
    while not feasible(high):
        high *= 2
    for _ in range(60):
        middle = (low + high) / 2
        if feasible(middle):
            high = middle
        else:
            low = middle
    scale = math.ceil(high * 1000 - 1e-9) / 1000
    x_lower, x_upper = interval(scale, "left", "right", profile.padding_x)
    y_lower, y_upper = interval(scale, "top", "bottom", profile.padding_y)
    offset = (round((x_lower + x_upper) / (2 * scale), 3),
              round((y_lower + y_upper) / (2 * scale), 3))

    current_covers = all(
        covers(placed_bounds(shield_facing, current_scale, current_offset), actor_facing)
        for actor_facing, shield_facing in pairs
    )
    recommended = placed_bounds(master, scale, offset)
    note = f"facing-by-facing fit: {actor_count} actor facings -> {shield_count} shield facings"
    if source_note:
        note = f"{source_note}; {note}"
    return FitResult(
        actor=actor,
        geometry="oval",
        actor_bounds=actor_bounds,
        master_bounds=master,
        current_sequence=sequence,
        current_scale=current_scale,
        current_offset=[current_offset[0], current_offset[1]],
        current_covers_model=current_covers,
        recommended_scale=scale,
        recommended_offset=[offset[0], offset[1]],
        recommended_bounds=recommended,
        excess_width=round(recommended["width"] - actor_bounds["width"], 3),
        excess_height=round(recommended["height"] - actor_bounds["height"], 3),
        note=note,
    )


def fit_sequence_name(actor: str) -> str:
    return "fit-" + actor.lower()


def write_generated_sequences(results: list[FitResult], sequences_path: pathlib.Path) -> None:
    header = [
        "# Generated by tools/audit/fit_hex_shields.py. Do not hand-edit.",
        "# Re-run the shield bounds measurement and fitter when actor art changes.",
        "",
    ]
    sequences = list(header)

    by_geometry: dict[str, list[FitResult]] = {name: [] for name in PROFILES}
    for result in sorted(results, key=lambda r: (r.geometry, r.actor)):
        by_geometry[result.geometry].append(result)

    for geometry, geometry_results in by_geometry.items():
        if not geometry_results:
            continue
        profile = PROFILES[geometry]
        sequences.extend([f"{profile.image}:"])
        for result in geometry_results:
            name = fit_sequence_name(result.actor)
            offset = ",".join(f"{v:g}" for v in result.recommended_offset)
            sequences.extend([
                f"\t{name}:",
                f"\t\tFilename: {profile.asset}",
                f"\t\tLength: {profile.length}",
            ])
            if profile.facings > 1:
                sequences.append(f"\t\tFacings: {profile.facings}")
            sequences.extend([
                "\t\tZOffset: 1023",
                f"\t\tScale: {result.recommended_scale:g}",
                "\t\tTick: 80",
                f"\t\tOffset: {offset}",
            ])
        sequences.append("")

    sequences_path.parent.mkdir(parents=True, exist_ok=True)
    with sequences_path.open("w", encoding="utf-8", newline="\n") as stream:
        stream.write("\n".join(sequences))


def main() -> int:
    args = parse_args()
    model = Model()
    if args.actors_out:
        routed: list[str] = []
        for actor in model.rs.actors:
            if actor.startswith("^"):
                continue
            resolved = model.rs.resolve(actor)
            overlay = resolved.child("WithIdleOverlay@shield1") if resolved else None
            if overlay:
                routed.append(actor.lower())
        args.actors_out.parent.mkdir(parents=True, exist_ok=True)
        args.actors_out.write_text("\n".join(sorted(routed)) + "\n", encoding="utf-8")
        print(f"Wrote {len(routed)} routed actor names to {args.actors_out}")
        if not args.bounds:
            return 0

    if not args.bounds:
        print("ERROR: provide bounds JSON or --actors-out", file=sys.stderr)
        return 2

    measurements = load_measurements(args.bounds)
    shared = model.root / "mods/cameo/bits/shared"
    masters = {
        geometry: visible_master_bounds(shared / profile.asset)
        for geometry, profile in PROFILES.items()
    }
    oval_profile = PROFILES["oval"]
    oval_facing_bounds = visible_master_facing_bounds(
        shared / oval_profile.asset, oval_profile.length, oval_profile.facings
    )

    results: list[FitResult] = []
    errors: list[str] = []
    for actor, measured in sorted(measurements.items()):
        resolved = model.rs.resolve(actor)
        overlay = resolved.child("WithIdleOverlay@shield1") if resolved else None
        if overlay is None:
            errors.append(f"{actor}: no resolved WithIdleOverlay@shield1")
            continue
        image = overlay.get("Image") or ""
        sequence = overlay.get("Sequence") or ""
        geometry = desired_geometry(actor, resolved, image, sequence)
        source_note = ""
        if measured.get("Error"):
            actor_bounds = conservative_selection_bounds(resolved, geometry)
            source_note = f"conservative selectable fallback: {measured['Error']}"
        else:
            actor_bounds = normalize_actor_bounds(measured["Bounds"])
            if measured.get("Warning"):
                actor_bounds = union_bounds(
                    actor_bounds,
                    conservative_selection_bounds(resolved, geometry),
                )
                source_note = (
                    "conservative selectable plus partial sprite fallback: "
                    f"{measured['Warning']}"
                )
        try:
            current_scale, current_offset = sequence_settings(model, actor, image, sequence)
        except (KeyError, ValueError) as exc:
            if not source_note:
                source_note = f"current fit sequence missing; regenerated: {exc}"
            current_scale, current_offset = 0.0, (0.0, 0.0)
        actor_facing_bounds = directional_actor_facing_bounds(measured)
        if geometry == "oval" and actor_facing_bounds:
            results.append(fit_directional(
                actor, actor_bounds, actor_facing_bounds, masters[geometry],
                oval_facing_bounds, sequence, current_scale, current_offset,
                source_note,
            ))
        else:
            results.append(fit(
                actor,
                actor_bounds,
                geometry,
                masters[geometry],
                sequence,
                current_scale,
                current_offset,
                source_note,
            ))

    print("actor,geometry,currentFit,currentScale,recommendedScale,recommendedOffset,excessW,excessH,note")
    for result in results:
        offset = f"{result.recommended_offset[0]:g},{result.recommended_offset[1]:g}"
        print(",".join((
            result.actor,
            result.geometry,
            "PASS" if result.current_covers_model else "LEAK",
            f"{result.current_scale:g}",
            f"{result.recommended_scale:g}",
            offset,
            f"{result.excess_width:g}",
            f"{result.excess_height:g}",
            result.note,
        )))
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)

    if args.json_out:
        payload = {
            "profiles": {name: asdict(profile) for name, profile in PROFILES.items()},
            "results": [asdict(result) for result in results],
            "errors": errors,
        }
        args.json_out.parent.mkdir(parents=True, exist_ok=True)
        args.json_out.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")

    if args.sequences_out:
        write_generated_sequences(results, args.sequences_out)
        print(f"Wrote {len(results)} fitted sequences to {args.sequences_out}")

    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
