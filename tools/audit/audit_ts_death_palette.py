#!/usr/bin/env python3
"""TS-only death palette audit.

Checks every actor in TiberianSun ContentPacks for mismatches between
DeathSequencePalette and PlayerPalette. Do NOT touch TD, D2k, RA1, RA2, TKM.

Usage:
    python tools/audit/audit_ts_death_palette.py
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TS_PACKS = ROOT / "mods/cameo/ContentPacks/TiberianSun"

ACTOR_RE = re.compile(r"^([a-zA-Z0-9_]+):", re.MULTILINE)
NESTED_RE = re.compile(r"^\t([a-zA-Z0-9_]+):", re.MULTILINE)


def extract_actors(yaml_text: str) -> list[dict]:
    """Parse YAML text into a list of actors with their trait blocks."""
    lines = yaml_text.split("\n")
    actors = []
    current_actor = None
    current_traits = {}

    i = 0
    while i < len(lines):
        line = lines[i]
        # Top-level actor definition (no leading tab)
        if line and not line.startswith("\t") and not line.startswith(" ") and line.rstrip().endswith(":"):
            if current_actor is not None:
                actors.append({"name": current_actor, "traits": current_traits})
            current_actor = line.rstrip()[:-1]
            current_traits = {}
        elif line.startswith("\t") and current_actor is not None:
            # Trait header (one tab indent, ends with :)
            stripped = line.strip()
            if stripped.endswith(":") and not stripped.startswith("-"):
                trait_name = stripped[:-1]
                # Collect trait body
                trait_body = []
                j = i + 1
                while j < len(lines) and lines[j].startswith("\t\t"):
                    trait_body.append(lines[j].strip())
                    j += 1
                current_traits[trait_name] = trait_body
                i = j
                continue
        i += 1

    if current_actor is not None:
        actors.append({"name": current_actor, "traits": current_traits})

    return actors


def get_palette_value(trait_body: list[str], key: str) -> str | None:
    for line in trait_body:
        if line.startswith(f"{key}:"):
            return line.split(":", 1)[1].strip()
    return None


def audit_file(filepath: Path) -> list[str]:
    """Check one YAML file for palette mismatches. Returns list of issues."""
    text = filepath.read_text(encoding="utf-8", errors="replace")
    actors = extract_actors(text)
    issues = []

    for actor in actors:
        name = actor["name"]
        traits = actor["traits"]

        # Skip template actors (start with ^)
        if name.startswith("^"):
            continue

        # Find RenderSprites and WithDeathAnimation traits
        render_traits = {k: v for k, v in traits.items() if k == "RenderSprites"}
        death_traits = {k: v for k, v in traits.items() if k == "WithDeathAnimation"}

        # Also check for RenderSprites@* and WithDeathAnimation@* variants
        for k, v in traits.items():
            if k.startswith("RenderSprites"):
                render_traits[k] = v
            if k.startswith("WithDeathAnimation"):
                death_traits[k] = v

        if not render_traits or not death_traits:
            continue

        # Get PlayerPalette from RenderSprites
        player_palette = None
        for _, body in render_traits.items():
            pp = get_palette_value(body, "PlayerPalette")
            if pp:
                player_palette = pp
                break

        # Get DeathSequencePalette from WithDeathAnimation
        death_palette = None
        for _, body in death_traits.items():
            dp = get_palette_value(body, "DeathSequencePalette")
            if dp:
                death_palette = dp
                break

        if player_palette and death_palette:
            if player_palette != death_palette:
                rel = filepath.relative_to(ROOT)
                issues.append(
                    f"  MISMATCH: {name} in {rel}\n"
                    f"    PlayerPalette: {player_palette}\n"
                    f"    DeathSequencePalette: {death_palette}"
                )
        elif player_palette and not death_palette:
            rel = filepath.relative_to(ROOT)
            issues.append(
                f"  MISSING DeathSequencePalette: {name} in {rel}\n"
                f"    PlayerPalette: {player_palette}"
            )

    return issues


def main() -> int:
    if not TS_PACKS.exists():
        print(f"ERROR: TS ContentPacks directory not found: {TS_PACKS}")
        return 1

    yaml_files = sorted(TS_PACKS.rglob("*.yaml"))
    if not yaml_files:
        print("No YAML files found in TiberianSun ContentPacks.")
        return 1

    all_issues = []
    files_checked = 0

    for yf in yaml_files:
        files_checked += 1
        issues = audit_file(yf)
        all_issues.extend(issues)

    print(f"Checked {files_checked} YAML files in TiberianSun ContentPacks.")
    print(f"Found {len(all_issues)} palette issue(s).")

    if all_issues:
        print("\n--- ISSUES ---")
        for issue in all_issues:
            print(issue)
        return 1
    else:
        print("All TS actors have matching PlayerPalette and DeathSequencePalette.")
        return 0


if __name__ == "__main__":
    sys.exit(main())
