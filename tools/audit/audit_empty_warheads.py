#!/usr/bin/env python3
"""audit_empty_warheads.py — boot-crash detector for typeless warhead nodes.

Incident 2026-08-03/04: NullReferenceException at ObjectCreator.CreateObject
<- WeaponInfo.LoadWarheads during Ruleset.LoadDefaults (game never reaches
the main menu).

Why empty warheads crash the boot:

- MiniYaml parses an empty value (`Warhead@Effect:`) as null
  (engine MiniYaml.cs: value.IsEmpty -> null).
- Ruleset.MergeOrDefault constructs a WeaponInfo for EVERY top-level node in
  every manifest Weapons file — unlike actors there is NO template filter,
  so ^templates are instantiated too.
- WeaponInfo.LoadWarheads runs
  Game.CreateObject<IWarhead>(node.Value.Value + "Warhead") for every
  resolved child whose key starts with "Warhead" (ordinal, case-sensitive).
  A null value yields the literal string "Warhead", which resolves to the
  ABSTRACT base class OpenRA.Mods.Common.Warheads.Warhead -> NRE inside
  ObjectCreator.CreateBasic.
- The merge normally rescues empty child values by falling back to the
  parent's value (MergePartial: overrideNodes.Value ?? existingNodes.Value),
  so only warheads with NO typed ancestor actually crash.

Engine load order (MiniYaml.Load -> Merge -> WeaponInfo ctor) is already
replicated by miniyaml.Ruleset: cross-file merging with the null-fallback
rule, top-level removals, and Inherits/Inherits@ resolution. Two engine
boot-crash classes are NOT detectable here and are out of scope (both raise
YamlException BEFORE WeaponInfo construction, so they can be ruled out
whenever the observed crash is the LoadWarheads NRE): dangling top-level
`-Weapon:` removals and missing inheritance parents.

Exit code 1 if any boot-crash site is found.

Usage: python tools/balance/run_with_guard.py tools/audit/audit_empty_warheads.py
"""

from __future__ import annotations

import sys

from miniyaml import Ruleset, find_repo_root

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")


def main() -> int:
    rs = Ruleset(find_repo_root())

    crashes: list[str] = []
    errors: list[str] = []
    suspects: list[str] = []

    # The engine constructs a WeaponInfo for EVERY top-level weapon node
    # (templates included), so check every entry in the merged ruleset.
    checked = 0
    for name in sorted(rs.weapons):
        checked += 1
        resolved = rs.resolve_weapon(name)
        if resolved is None:
            errors.append(
                f"[ERROR] weapon '{name}' has circular inheritance "
                f"(YamlException at boot)")
            continue
        for c in resolved.children:
            if c.key.startswith("Warhead") and not (c.value and str(c.value).strip()):
                crashes.append(
                    f"[CRASH] weapon '{resolved.key}' node '{c.key}' has an "
                    f"empty warhead type — {c.file}:{c.line}")
        proj = resolved.child("Projectile")
        if proj is not None and not (proj.value and str(proj.value).strip()):
            suspects.append(
                f"[SUSPECT] weapon '{resolved.key}' has an empty Projectile "
                f"value — {proj.file}:{proj.line}")

    print(f"weapons constructed by engine: {checked}")
    for line in errors + crashes + suspects:
        print(line)
    print(f"summary: {len(crashes)} warhead crash sites, "
          f"{len(suspects)} suspects, {len(errors)} errors")
    return 1 if crashes or errors else 0


if __name__ == "__main__":
    sys.exit(main())
