#!/usr/bin/env python3
"""rename_sonic_debuff.py — BUILD 3: global CommandoDebuff -> SonicDebuff.

PHYSICAL_STATE_SYSTEM.md §6 decision 4: the "commando debuff" (+50% incoming damage,
-25% speed, blue tint) is really the SONIC resonance mark, so it is renamed and then
baked into every ^Warhead_Sonic_* level by the generator.

Renames ONLY the condition/template/warhead-key identifiers:
    ^CommandoDebuff        -> ^SonicDebuff        (defaults.yaml template)
    CommandoDebuff         -> SonicDebuff         (condition name, Warhead@ key)
    @COMMANDODEBUFF        -> @SONICDEBUFF        (trait instance suffixes)
    Inherits@commandodebuff/Inherits@commando (-> ^CommandoDebuff)  -> Inherits@sonicdebuff

NEVER touched (assets, not identifiers):
    2100commandodebuff (.shp image + its sequences), PaletteFromRGBA@commandodebuff /
    Name: commandodebuff  -> the decoration artwork keeps its own name.
Also NOT touched: ^CommandoCall / ^CommandoCallable / CommandoCall (a different system).

Usage: python scratchpad/rename_sonic_debuff.py [--apply]
"""
from __future__ import annotations
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APPLY = "--apply" in sys.argv

# The asset name must survive: protect it before the case-insensitive key rename.
ASSET = "2100commandodebuff"
ASSET_TOKEN = "\x00ASSET\x00"


def convert(text: str) -> str:
    text = text.replace(ASSET, ASSET_TOKEN)
    # Inherits@commando: ^CommandoDebuff  /  Inherits@commandodebuff: ^CommandoDebuff
    text = re.sub(r"Inherits@\w+(?=: \^CommandoDebuff\b)", "Inherits@sonicdebuff", text)
    text = text.replace("@COMMANDODEBUFF", "@SONICDEBUFF")
    # ^CommandoDebuff and the bare CommandoDebuff condition / Warhead@ key.
    text = re.sub(r"\bCommandoDebuff\b", "SonicDebuff", text)
    return text.replace(ASSET_TOKEN, ASSET)


def main() -> int:
    targets = sorted(
        p for p in ROOT.joinpath("mods").rglob("*.yaml")
        if "commandodebuff" in p.read_text(encoding="utf-8").lower()
    )
    changed = 0
    for path in targets:
        src = path.read_text(encoding="utf-8")
        dst = convert(src)
        if src == dst:
            print(f"    (asset-only, untouched) {path.relative_to(ROOT)}")
            continue
        hits = sum(1 for a, b in zip(src.splitlines(), dst.splitlines()) if a != b)
        print(f"{'APPLY' if APPLY else 'would change'} {hits:3d} line(s)  {path.relative_to(ROOT)}")
        changed += hits
        if APPLY:
            path.write_text(dst, encoding="utf-8", newline="\n")
    print(f"\n{changed} line(s) total{'' if APPLY else '  (dry run — pass --apply)'}")
    left = [p.relative_to(ROOT) for p in targets
            if re.search(r"\bCommandoDebuff\b", convert(p.read_text(encoding="utf-8")))]
    print(f"remaining CommandoDebuff identifiers after rename: {len(left)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
