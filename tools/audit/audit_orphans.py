#!/usr/bin/env python3
"""audit_orphans.py — B10 detector (dead content).

Reference-counts weapons and (approximately) conditions from the resolved
live ruleset plus maps/ and Lua scripts; lists zero-reference entries.

  O1 weapons defined in live weapons yaml that no live actor/weapon references
  O2 dangling weapon references (actor references a weapon that doesn't exist)
     — BLOCKING, crash-on-use class
  O3 conditions granted but never consumed / consumed but never granted
     (RequiresCondition-level; token-level dead wiring is in audit_upgrades)
"""

from __future__ import annotations

import re
import sys

from cameo_model import Model
from report import h1, h2, relpath, table

WEAPON_FIELDS = ("Weapon", "EmptyWeapon", "ExplosionWeapon", "AirburstWeapon",
                 "ImpactActorWeapon", "DetonationWeapon")
_ident = re.compile(r"[A-Za-z0-9_.\-]+")


def main() -> int:
    m = Model()
    rs = m.rs
    referenced_weapons: set[str] = set()
    dangling = []
    granted: dict[str, set[str]] = {}
    consumed: dict[str, set[str]] = {}

    grant_fields = ("Condition", "ChargingCondition", "LoadedCondition",
                    "AirborneCondition", "CruisingCondition", "AmmoCondition",
                    "ControllingCondition")

    for name in rs.actors:
        if name.startswith("^"):
            continue
        res = rs.resolve(name)
        if res is None:
            continue
        for trait in res.children:
            for f in WEAPON_FIELDS:
                v = trait.get(f)
                if v:
                    referenced_weapons.add(v.lower())
                    if rs.weapon(v) is None:
                        dangling.append([name, trait.key, v,
                                         relpath(res.file, m.root)])
            for f in grant_fields:
                v = trait.get(f)
                if v:
                    granted.setdefault(v.lower(), set()).add(name)
            for f in ("RequiresCondition", "PauseOnCondition"):
                v = trait.get(f)
                if v:
                    for ident in _ident.findall(v):
                        if not ident.isdigit():
                            consumed.setdefault(ident.lower(), set()).add(name)
            for gc in ("GrantCondition", "GrantConditionOnPrerequisite",
                       "ExternalCondition", "GrantConditionOnDamageState",
                       "GrantConditionWhileAiming", "GrantConditionOnAttack",
                       "GrantPeriodicCondition", "GrantConditionOnDeploy",
                       "GrantConditionOnMovement", "GrantConditionOnTerrain"):
                if trait.key.split("@", 1)[0] == gc:
                    v = trait.get("Condition") or trait.get("DeployedCondition")
                    if v:
                        granted.setdefault(v.lower(), set()).add(name)

    # weapons referencing other weapons (FireFragment / FireRadius warheads)
    for wname in rs.weapons:
        w = rs.resolve_weapon(wname)
        if w is None:
            continue
        for c in w.children:
            v = c.get("Weapon")
            if v:
                referenced_weapons.add(v.lower())

    # map + lua references (string grep, both weapon and actor level)
    map_text = []
    for base in (m.root / "mods/cameo/maps", m.root / "mods/cameo/bits/lua"):
        if not base.exists():
            continue
        for p in base.rglob("*"):
            if p.suffix.lower() in (".yaml", ".lua"):
                try:
                    map_text.append(p.read_text(encoding="utf-8-sig",
                                                errors="replace").lower())
                except OSError:
                    pass
    blob = "\n".join(map_text)

    orphan_weapons = []
    for wname in sorted(rs.weapons):
        if wname.startswith("^"):
            continue
        lw = wname.lower()
        if lw in referenced_weapons:
            continue
        if lw in blob:
            continue
        orphan_weapons.append([wname, relpath(rs.weapons[wname].file, m.root)])

    never_consumed = sorted(set(granted) - set(consumed))
    never_granted_rows = []
    for cond in sorted(set(consumed) - set(granted)):
        # many identifiers in boolean expressions are prerequisites, not
        # conditions; only report ones that look condition-ish and are not
        # provided as prerequisite tokens either
        sample = sorted(consumed[cond])[:4]
        never_granted_rows.append([cond, str(len(consumed[cond])), ", ".join(sample)])

    print(h1("audit_orphans — dead content (B10)"))
    print(f"Live weapons: **{len(rs.weapons)}** — orphans: **{len(orphan_weapons)}**, "
          f"dangling weapon refs (BLOCKING): **{len(dangling)}**, "
          f"conditions granted-never-consumed: **{len(never_consumed)}**\n")
    print(h2("O2 — dangling weapon references (crash-on-use class)"))
    print(table(["actor", "trait", "missing weapon", "file"], dangling))
    print(h2("O1 — orphan weapons (no live actor/weapon/map/lua reference)"))
    print(table(["weapon", "file"], orphan_weapons))
    print(h2("O3a — conditions granted but never consumed (sample)"))
    print(", ".join(never_consumed[:150]) + ("…" if len(never_consumed) > 150 else ""))
    print("\n\n_O3b (identifiers consumed but never granted) is high-noise "
          "because RequiresCondition expressions mix conditions with "
          "prerequisite tokens; see audit_upgrades dead-wiring for the "
          "curated version._\n")
    return 1 if dangling else 0


if __name__ == "__main__":
    sys.exit(main())
