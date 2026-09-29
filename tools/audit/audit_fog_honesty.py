#!/usr/bin/env python3
"""audit_fog_honesty — ratchet on global-actor-enumeration inside bot modules.

The acceptance contract (AI_ARCHITECTURE §0a, maintainer ruling 2026-09-28)
requires the candidate bot to fight WITHOUT map-wide vision. Nothing else
verifies that mechanically: a `World.Actors` scan in a targeting loop is an
omniscience leak whether or not anyone intended it.

This audit counts global-enumeration sites per bot-module source file and
compares them to a committed manifest
(tools/audit/fog_honesty_manifest.json):

  * a file's count INCREASING means a new unreviewed omniscience site — FAIL;
  * a count DECREASING is noted but does not fail (the manifest wants a
    deliberate `--write` so removals are recorded, not silent);
  * files in the Fransbot donor stack may not contain ANY unmanifested site —
    the donor is the fog-honest candidate in isolation.

What counts as a "site": `World.Actors`, `world.Actors`, `ActorsHavingTrait`,
`ActorsInBox`, `FindActorsInCircle` on a World — enumerations that see every
actor regardless of shroud. Honest mechanisms (`Shroud.IsVisible`,
`CanBeViewedByPlayer`, `FrozenActor` memory, `Player.PlayerActor` scans) do not
match these patterns and are not counted. Sites inside `//` comments are
stripped before counting.

A count-only ratchet cannot tell "enumerating my own units" (honest — own
actors are always visible) from "enumerating enemy targets" (a cheat). It does
not try: the manifest freezes today's totals so any new site gets a human
review. Semantic review lives with the reviewer; the audit's job is to make
omniscience a deliberate act.

Usage:
  python tools/audit/audit_fog_honesty.py                # check
  python tools/audit/audit_fog_honesty.py --write        # re-baseline manifest
"""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
MANIFEST = REPO / "tools" / "audit" / "fog_honesty_manifest.json"

# Bot-module roots, relative to the repo. The engine copy is gitignored but
# present in built worktrees; a missing dir is skipped, not an error.
SCAN_ROOTS = [
    ("OpenRA.Mods.Fransbot/Traits", True),   # donor stack — strictest
    ("OpenRA.Mods.CA/Traits/BotModules", False),
    ("OpenRA.Mods.Cameo/Traits/BotModules", False),
    ("engine/OpenRA.Mods.Common/Traits/BotModules", False),
    ("engine/OpenRA.Mods.AS/Traits/BotModules", False),
]
# Cameo keeps bot traits at Traits/ root rather than BotModules/ — globbed.
SCAN_GLOBS = [
    ("OpenRA.Mods.Cameo/Traits", "*Bot*.cs"),
    ("OpenRA.Mods.Cameo/Traits", "Ai*Log*.cs"),
    ("OpenRA.Mods.Cameo/Traits", "AiSituation*.cs"),
    ("OpenRA.Mods.Cameo/Traits", "BeaconTracker.cs"),
]

PATTERNS = re.compile(
    r"\b(?:World|world)\.Actors\b"
    r"|\bActorsHavingTrait\b"
    r"|\bActorsInBox\b"
    r"|\bFindActorsInCircle\b"
    r"|\bWorld\.FindActorsInCircle\b"
)

LINE_COMMENT = re.compile(r"//.*$")


def iter_sources():
    seen = set()
    for rel, strict in SCAN_ROOTS:
        root = REPO / rel
        if not root.is_dir():
            continue
        for cs in sorted(root.rglob("*.cs")):
            relname = cs.relative_to(REPO).as_posix()
            if relname in seen:
                continue
            seen.add(relname)
            yield relname, strict
    for dirname, pattern in SCAN_GLOBS:
        root = REPO / dirname
        if not root.is_dir():
            continue
        for cs in sorted(root.glob(pattern)):
            relname = cs.relative_to(REPO).as_posix()
            if relname not in seen:
                seen.add(relname)
                yield relname, False


def count_sites(path: pathlib.Path) -> int:
    count = 0
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        code = LINE_COMMENT.sub("", line)
        count += len(PATTERNS.findall(code))
    return count


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--write", action="store_true", help="re-baseline the manifest")
    args = ap.parse_args()

    current: dict[str, int] = {}
    strict_files: set[str] = set()
    for rel, strict in iter_sources():
        n = count_sites(REPO / rel)
        if n:
            current[rel] = n
        if strict:
            strict_files.add(rel)

    if args.write:
        MANIFEST.write_text(json.dumps(current, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(f"wrote {MANIFEST.name}: {len(current)} files, {sum(current.values())} sites")
        return 0

    if not MANIFEST.is_file():
        print(f"FAIL: manifest missing ({MANIFEST}) — run with --write to seed it")
        return 1
    baseline = json.loads(MANIFEST.read_text(encoding="utf-8"))

    failures: list[str] = []
    notes: list[str] = []
    for rel, n in sorted(current.items()):
        base = baseline.get(rel)
        if base is None:
            kind = "NEW FILE (fransbot stack — no unmanifested sites allowed)" if rel in strict_files else "NEW FILE"
            if n > 0:
                failures.append(f"{rel}: {n} enumeration site(s), none manifested - {kind}")
        elif n > base:
            failures.append(f"{rel}: {base} -> {n} (+{n - base}) enumeration site(s) added - review for fog honesty")
        elif n < base:
            notes.append(f"{rel}: {base} -> {n} (sites removed - refresh manifest with --write)")
    for rel in baseline:
        if rel not in current:
            notes.append(f"{rel}: manifest entry stale (file gone or clean) - refresh with --write")

    for note in notes:
        print(f"  note: {note}")
    if failures:
        for f in failures:
            print(f"FAIL: {f}")
        print(f"{len(failures)} file(s) gained unreviewed global-actor enumeration")
        return 1
    print(f"PASS: {len(current)} files, {sum(current.values())} manifested enumeration sites, no new omniscience")
    return 0


if __name__ == "__main__":
    sys.exit(main())
