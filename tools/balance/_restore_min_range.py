#!/usr/bin/env python3
"""Restore MinRange for weapons that were incorrectly removed."""

from __future__ import annotations

import argparse
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))

from apply_balance import YamlEditor
from cameo_model import Model

# All weapons that had MinRange removed in the linear-pulse sweep EXCEPT NaxDieGlocke.
RESTORE = {
    "AthenaLaser",
    "BowlingTrail",
    "OrniGunC",
    "SiegeEngineCannon",
    "SiegeTankSiegeCannon",
    "USALaserCannonAG",
    "WaveTurretShell",
    "YakTeslaGun",
}

# Values logged when they were removed; used as fallback for new/untracked files.
KNOWN_ORIGINAL = {
    "AthenaLaser": 3200,
    "BowlingTrail": 2460,
    "OrniGunC": 1200,
    "SiegeEngineCannon": 2240,
    "SiegeTankSiegeCannon": 2355,
    "USALaserCannonAG": 1435,
    "WaveTurretShell": 1965,
    "YakTeslaGun": 1400,
}


def _find_original_min_range(actor: str, rel_path: str) -> str | None:
    try:
        raw = subprocess.check_output(
            ["git", "show", f"HEAD:{rel_path}"], cwd=ROOT, text=True
        )
    except subprocess.CalledProcessError:
        return None
    lines = raw.splitlines()
    for i, line in enumerate(lines):
        if line.startswith(actor + ":"):
            break
    else:
        return None
    # find end of block
    for j in range(i + 1, len(lines)):
        if lines[j] and not lines[j].startswith("\t"):
            end = j
            break
    else:
        end = len(lines)
    for k in range(i + 1, end):
        m = re.match(rf"^\tMinRange:\s*(.+)$", lines[k])
        if m:
            return m.group(1).strip()
    return None


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--confirm", action="store_true", help="actually write MinRange lines")
    args = ap.parse_args()

    m = Model()
    rs = m.rs
    editors: dict[pathlib.Path, YamlEditor] = {}
    restored = 0

    for name in sorted(RESTORE):
        wnode = rs.weapon(name)
        if wnode is None:
            print(f"{name}: weapon not found")
            continue
        wfile = wnode.file
        if wfile is None:
            print(f"{name}: no file")
            continue
        wpath = pathlib.Path(wfile)
        if wpath not in editors:
            editors[wpath] = YamlEditor(wpath)
        ed = editors[wpath]

        span = ed._block(name)
        if span is None:
            print(f"{name}: block not found in {wpath}")
            continue
        s, e = span
        # skip if MinRange already present locally
        if any(re.match(r"^\tMinRange:\s*", line) for line in ed.lines[s + 1 : e]):
            print(f"{name}: MinRange already present, skipping")
            continue

        orig = _find_original_min_range(name, wfile)
        if orig is None:
            val = KNOWN_ORIGINAL.get(name)
            if val is None:
                print(f"{name}: original MinRange not known")
                continue
            orig = str(val)

        ed.lines.insert(e, f"\tMinRange: {orig}")
        ed.dirty = True
        print(f"{name}: restored MinRange {orig}")
        restored += 1

    if args.confirm:
        for ed in editors.values():
            ed.save()
        print(f"\nSaved {len(editors)} file(s), restored {restored} MinRange line(s).")
    else:
        print(f"\nDRY RUN: would restore {restored} MinRange line(s) in {len(editors)} file(s).")
        print("Use --confirm to apply.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
