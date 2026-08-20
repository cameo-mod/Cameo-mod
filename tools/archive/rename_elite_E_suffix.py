#!/usr/bin/env python3
"""Rename 17 weapons with deprecated trailing 'E' (elite) suffix to '_elite'.

Excludes:
- SUSABurtonSniperHE / SUSABurtonSniperHE_AI (HE = High Explosive, not elite)
- SUSAMLRSHE (HE = High Explosive, not elite)
- EMPGrenade (EMP is prefix, not elite)
- Weapons already using _elite convention

The rename is structural: top-level definition keys, Weapon:/Weapons: field
values, and Inherits: references. Uses word-boundary regex with negative
lookahead to avoid matching substrings.
"""

import re
import sys
from pathlib import Path

# 17 weapons to rename: old_name -> new_name
RENAMES = {
    "RA2FlakTrackGunE":         "RA2FlakTrackGun_elite",
    "RA2FlakTrackAAGunE":       "RA2FlakTrackAAGun_elite",
    "RA2AsianShotgunE":        "RA2AsianShotgun_elite",
    "AsianGrenadeE":           "AsianGrenade_elite",
    "NaxiMP40E":               "NaxiMP40_elite",
    "Lunar_GreenTigerCannonE":  "Lunar_GreenTigerCannon_elite",
    "Lunar_GreenJadgDestroyerE":"Lunar_GreenJadgDestroyer_elite",
    "Lunar_GreenGrilleArtyE":   "Lunar_GreenGrilleArty_elite",
    "RA2NarcoAKME":            "RA2NarcoAKM_elite",
    "RA2Narco60mmE":           "RA2Narco60mm_elite",
    "LatinRusherRocketE":       "LatinRusherRocket_elite",
    "RA2GrenadePackE":         "RA2GrenadePack_elite",
    "LatinSmokerRocketE":      "LatinSmokerRocket_elite",
    "td_gdi_commando_sniperE":  "td_gdi_commando_sniper_elite",
    "CabalHunterKillerLasersE": "CabalHunterKillerLasers_elite",
    "TSSniperE":               "TSSniper_elite",
    "MutAPRifleE":             "MutAPRifle_elite",
}

# Also rename the _AI variant that inherits from SUSABurtonSniperHE's pattern
# (not needed — HE weapons excluded)

MODS_DIR = Path(__file__).resolve().parents[2] / "mods" / "cameo"

# File types to process
EXTENSIONS = {".yaml"}

def build_regex():
    """Build a regex that matches any old name as a whole word."""
    # Sort by length descending so longer names match first
    names = sorted(RENAMES.keys(), key=len, reverse=True)
    escaped = [re.escape(n) for n in names]
    # Word boundary: preceded by start/whitespace/colon/pipe, followed by end/whitespace/colon/pipe/newline
    pattern = r'(?<![A-Za-z0-9_.])(' + '|'.join(escaped) + r')(?![A-Za-z0-9_.])'
    return re.compile(pattern)

def process_file(filepath: Path, rx: re.Pattern, dry_run: bool = False) -> int:
    """Process a single file, return count of replacements."""
    try:
        text = filepath.read_text(encoding='utf-8')
    except Exception:
        return 0

    count = len(rx.findall(text))
    if count == 0:
        return 0

    def replace(match):
        old = match.group(1)
        new = RENAMES[old]
        return new

    new_text = rx.sub(replace, text)

    if not dry_run:
        filepath.write_text(new_text, encoding='utf-8')

    return count

def main():
    dry_run = "--dry-run" in sys.argv

    rx = build_regex()

    total_replacements = 0
    files_modified = []

    for filepath in MODS_DIR.rglob("*"):
        if filepath.suffix not in EXTENSIONS:
            continue
        if filepath.is_dir():
            continue

        count = process_file(filepath, rx, dryRun if False else dry_run)
        if count > 0:
            total_replacements += count
            files_modified.append((filepath, count))

    print(f"Mode: {'DRY RUN' if dry_run else 'APPLY'}")
    print(f"Total replacements: {total_replacements}")
    print(f"Files modified: {len(files_modified)}")
    for f, c in sorted(files_modified, key=lambda x: -x[1]):
        rel = f.relative_to(MODS_DIR)
        print(f"  {rel}: {c} replacements")

    # Verify no old names remain
    if not dry_run:
        remaining = 0
        for filepath in MODS_DIR.rglob("*"):
            if filepath.suffix not in EXTENSIONS or filepath.is_dir():
                continue
            text = filepath.read_text(encoding='utf-8')
            matches = rx.findall(text)
            if matches:
                remaining += len(matches)
                print(f"  WARNING: {filepath.relative_to(MODS_DIR)} still has: {matches}")
        if remaining == 0:
            print("Verification: 0 old names remaining. Clean.")
        else:
            print(f"Verification: {remaining} old names still remaining!")

if __name__ == "__main__":
    main()
