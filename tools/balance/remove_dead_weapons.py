#!/usr/bin/env python3
"""Delete weapon definitions that are LOADED but that nothing uses.

An obsolete weapon is not harmless. Every tool that reasons about the mod iterates the
resolved ruleset — the Versus census, the armor-exposure weighting, the family surveys,
the K coefficient's prevalence weights. A template that no weapon inherits and no actor
fires still contributes its `Versus` ladder to all of them, so it silently biases the
numbers the whole balance program is built on. Maintainer, 2026-08-16: *"obsolete things
should be removed entirely so they don't affect our unit / weapon balance where we count
versus values from different weapons."*

    python tools/balance/remove_dead_weapons.py --survey
    python tools/balance/remove_dead_weapons.py ^AACannon ^RALightMG --apply

**Safety.** A definition is only deletable when, in the LIVE ruleset (the weapon files
`mod.yaml` actually lists — dead files are commented out and never loaded):

  * nothing `Inherits` it, directly or through any chain, and
  * no actor's `Armament` names it as a `Weapon:`, and
  * no weapon references it as a projectile/warhead sub-weapon.

References from ALREADY-DEAD files are reported, not counted: those files are not loaded,
so they cannot affect balance, and a revived file needs a full conversion anyway.
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))
import miniyaml  # noqa: E402
from retrofit_legacy_template import YamlFile, weapon_files  # noqa: E402

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

# Fields by which one weapon can name another; a hit on any of them is a live use.
WEAPON_REF_FIELDS = ("Weapon", "Weapons", "TriggeredWeapon", "FallbackWeapon")

# The 3-way-split LIBRARY, emitted as a complete matrix by `gen_weapon_template.py`.
# These are unused-on-purpose: the generator ships every family x level so a weapon can
# pick any rung, and `verify_generator_sync.py` requires the full set to exist. "Nothing
# inherits it yet" is the normal state for half of them and is NOT obsolescence —
# deleting them would dismantle the family system and break generator sync.
LIBRARY_PREFIXES = ("^Warhead_", "^Effect_", "^Projectile_")


def live_weapon_files() -> set[str]:
    """Only what `mod.yaml` actually loads."""
    text = (ROOT / "mods" / "cameo" / "mod.yaml").read_text(encoding="utf-8")
    m = re.search(r"^Weapons:\n((?:(?:\t.*)?\n)*)", text, re.M)
    out = set()
    for line in m.group(1).splitlines():
        s = line.strip()
        if not s or s.startswith("#"):
            continue
        out.add(s.split("|", 1)[-1].split("#")[0].strip())
    return out


def usage(rules: miniyaml.Ruleset) -> tuple[dict[str, set[str]], set[str]]:
    """(name -> weapons inheriting it, set of weapon names referenced by any rule)."""
    inherit = collections.defaultdict(set)
    for name, node in rules.weapons.items():
        for c in node.children:
            if c.key.startswith("Inherits") and c.value:
                inherit[c.value.strip()].add(name)

    referenced: set[str] = set()

    def walk(node):
        for c in node.children:
            key = c.key.split("@", 1)[0]
            if key in WEAPON_REF_FIELDS and c.value:
                for part in c.value.split(","):
                    referenced.add(part.strip())
            walk(c)

    for node in rules.actors.values():
        walk(node)
    for node in rules.weapons.values():
        walk(node)
    return inherit, referenced


def find_block(name: str) -> tuple[pathlib.Path, tuple[int, int]] | None:
    for path in weapon_files():
        f = YamlFile(path)
        span = f.block(name)
        if span is not None:
            return path, span
    return None


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("names", nargs="*", help="weapon/template names to delete")
    ap.add_argument("--survey", action="store_true",
                    help="list every loaded-but-unused ^template carrying a Versus table")
    ap.add_argument("--apply", action="store_true", help="write the files")
    args = ap.parse_args()

    rules = miniyaml.Ruleset(ROOT)
    inherit, referenced = usage(rules)
    live = live_weapon_files()

    if args.survey:
        rows, library = [], 0
        for name, node in rules.weapons.items():
            if not name.startswith("^"):
                continue
            if inherit.get(name) or name in referenced:
                continue
            if name.startswith(LIBRARY_PREFIXES):
                library += 1
                continue
            has_versus = any(c.child("Versus") is not None for c in node.children
                             if c.key.startswith("Warhead"))
            rows.append((name, has_versus))
        rows.sort()
        print(f"{len(rows)} obsolete templates — loaded, inherited by nothing, fired by "
              f"nothing ({sum(1 for _, v in rows if v)} carry a Versus ladder that "
              f"biases the census)")
        for name, v in rows:
            print(f"   {name:28s} {'Versus ladder' if v else '-'}")
        print(f"\n(skipped {library} unused `^Warhead_*`/`^Effect_*`/`^Projectile_*` "
              f"library templates — the generator ships the full matrix on purpose)")
        return 0

    if not args.names:
        ap.error("name at least one weapon, or pass --survey")

    deleted = 0
    for name in args.names:
        users = inherit.get(name, set())
        if users or name in referenced:
            print(f"REFUSED {name}: still used by "
                  f"{sorted(users)[:5] or 'an Armament'} — not obsolete")
            continue
        found = find_block(name)
        if found is None:
            print(f"SKIP {name}: no definition found")
            continue
        path, span = found
        rel = path.relative_to(ROOT).as_posix()
        f = YamlFile(path)
        span = f.block(name)
        # The header line sits one above the body span.
        f.cut(span[0] - 1, span[1])
        # Report dangling references from files that are NOT loaded.
        dangling = []
        for other in weapon_files():
            orel = other.relative_to(ROOT).as_posix()
            if orel.replace("mods/cameo/", "") in live or "ContentPacks" in orel:
                continue
            if re.search(rf"^\t*Inherits[^:]*:\s*{re.escape(name)}\s*$",
                         other.read_text(encoding="utf-8"), re.M):
                dangling.append(orel)
        note = f"  (referenced by {len(dangling)} DEAD file(s), not loaded)" if dangling else ""
        print(f"DELETE {name:24s} from {rel}{note}")
        if args.apply:
            f.save()
        deleted += 1

    print(f"\n{deleted} definitions removed" if args.apply
          else f"\n{deleted} would be removed (dry run — pass --apply)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
