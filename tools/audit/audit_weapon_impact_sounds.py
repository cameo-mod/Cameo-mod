#!/usr/bin/env python3
"""audit_weapon_impact_sounds.py — list weapons with a CreateEffect warhead but no ImpactSounds."""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from cameo_model import Model


def h1(s: str) -> str:
    return f"# {s}\n"


def h2(s: str) -> str:
    return f"## {s}\n"


def table(headers: list[str], rows: list[list[str]]) -> str:
    out = ["| " + " | ".join(headers) + " |"]
    out.append("| " + " | ".join("---" for _ in headers) + " |")
    for row in rows:
        out.append("| " + " | ".join(str(c) for c in row) + " |")
    return "\n".join(out) + "\n"


def main() -> int:
    m = Model()
    weapons = m.rs.weapons

    rows = []
    for name, node in sorted(weapons.items(), key=lambda x: x[0]):
        if name.startswith("^"):
            continue

        def is_real_warhead(c):
            return c.key.startswith("Warhead") and not c.key.lstrip().startswith("-")

        warheads = [c for c in node.children if is_real_warhead(c)]
        has_create_effect = any(c.value == "CreateEffect" for c in warheads)

        def has_sound(c):
            return c.key == "ImpactSounds" or c.key == "-ImpactSounds"

        has_impact_sounds = any(
            has_sound(c) for c in warheads
        ) or any(
            any(has_sound(c2) for c2 in c.children)
            for c in warheads
        )
        if has_create_effect and not has_impact_sounds:
            rows.append([name, node.file or ""])

    print(h1("audit_weapon_impact_sounds"))
    print(f"Weapons with CreateEffect but no ImpactSounds: **{len(rows)}**\n")
    if rows:
        print(h2("Violations"))
        print(table(["weapon", "file"], rows))
    return 1 if rows else 0


if __name__ == "__main__":
    sys.exit(main())
