#!/usr/bin/env python3
"""One-off conservative ShrapnelWeapon -> Concussion_Medium single-family converter.

Resolves the 10-weapon effect-free cluster by replacing the old ^ShrapnelWeapon
inherit with ^Warhead_Concussion_Medium + ^Effect_Concussion_Medium, preserving
RA2*/Steel* and other non-old addons, local projectiles, and custom warheads.
"""
from __future__ import annotations
import os
import re
import sys

ROOT = os.path.join(os.path.dirname(__file__), "..", "mods", "cameo")

TARGETS = [
    ("weapons/redalert2mod.yaml", "NaxGrilleArty"),
    ("weapons/tiberiansun.yaml", "TSGrenade"),
    ("ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml", "RA160mm"),
    ("ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml", "AsianGrenade"),
    ("ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml", "asianalliance_asianmilitia_grenade"),
    ("ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml", "NaxiJadgDestroyer"),
    ("ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml", "LunarNaxiJadgDestroyer"),
    ("ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml", "ViperMissiles"),
    ("ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml", "TS120mmx"),
    ("ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml", "TSScoopDualTur"),
]


def parse_blocks(lines):
    """Return list of (start, end, name) for top-level weapon/template blocks."""
    blocks = []
    start = 0
    name = None
    for i, ln in enumerate(lines):
        if i == 0:
            m = re.match(r"^(?:\xef\xbb\xbf)?([^\s].*?):\s*$", ln)
            if m:
                name = m.group(1)
                start = i
            continue
        if re.match(r"^[^\s].*?:\s*$", ln):
            if name is not None:
                blocks.append((start, i, name))
            name = re.match(r"^([^\s].*?):\s*$", ln).group(1)
            start = i
    if name is not None:
        blocks.append((start, len(lines), name))
    return blocks


def convert_block(lines, s, e):
    out = []
    i = s
    while i < e:
        ln = lines[i]
        # Inherits of the old ^ShrapnelWeapon
        m = re.match(r"^(\t)Inherits(@[\w.]+)?:\s*\^ShrapnelWeapon\s*$", ln)
        if m:
            out.append("\tInherits@wh: ^Warhead_Concussion_Medium")
            out.append("\tInherits@fx: ^Effect_Concussion_Medium")
            i += 1
            continue

        # Main shrapnel warhead
        if re.match(r"^\tWarhead@ShrapnelWeapon:\s*\S", ln):
            # Keep the override block; the type is now inherited from ^Warhead_Concussion_Medium
            out.append("\tWarhead@Concussion_Medium:")
            i += 1
            continue
        if re.match(r"^\tWarhead@ShrapnelWeapon:\s*$", ln):
            out.append("\tWarhead@Concussion_Medium:")
            i += 1
            continue

        # Percentage variant
        mp = re.match(r"^\tWarhead@ShrapnelWeaponPercentage:\s*(.*)$", ln)
        if mp:
            rest = mp.group(1).strip()
            out.append(f"\tWarhead@Concussion_Medium_Percentage: {rest}" if rest else "\tWarhead@Concussion_Medium_Percentage:")
            i += 1
            continue

        # Friendly-fire twin and removal markers -> strip entirely
        if re.match(r"^\t(?:-?)Warhead@ShrapnelWeaponFriendlyFire:", ln) or \
           re.match(r"^\t-Warhead@ShrapnelWeapon", ln):
            i += 1
            while i < e:
                nxt = lines[i]
                # Stop at the next sibling warhead/field (one tab) or the next top-level (no tab)
                if nxt.startswith("\t\t") or nxt.startswith("\t\t") is False and nxt.startswith("\t"):
                    # Careful: nxt.startswith("\t") is true for one-tab siblings; stop
                    if not nxt.startswith("\t\t"):
                        break
                i += 1
            continue

        out.append(ln.rstrip("\n"))
        i += 1
    return out


def main():
    changes = 0
    for rel, wname in TARGETS:
        path = os.path.join(ROOT, rel)
        with open(path, "r", encoding="utf-8-sig") as f:
            text = f.read()
        lines = text.split("\n")
        blocks = parse_blocks(lines)
        for s, e, name in blocks:
            if name != wname:
                continue
            new_block = convert_block(lines, s + 1, e)
            lines = lines[:s + 1] + new_block + lines[e:]
            changes += 1
            break
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write("\n".join(lines))
    print(f"Converted {changes} weapons")
    if changes != len(TARGETS):
        print("WARNING: some target weapons were not found", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
