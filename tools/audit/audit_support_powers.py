#!/usr/bin/env python3
"""audit_support_powers.py — the superweapon / support-power audit.

    python tools/audit/audit_support_powers.py

Maintainer 2026-08-29: *"Super weapons are neither units nor defenses so not part
of the balance formula. But they should still be part of a separate superweapon
damage audit."*

Superweapons sit OUTSIDE the DESIGN.md §12 class formula
(`docs/design/balance_exceptions.yaml`), so nothing else in the suite looks at
them. This does. Three checks:

  S1  ORPHANED LEVEL MAPS — a level map (``1:``, ``2:`` ...) indented under a key
      that already carries a scalar value. The author meant to write
      ``Prerequisites:`` above it and the line is missing, so the map hangs off
      the previous key instead. ⛔ The engine DROPS it in silence (CLAUDE.md 8b:
      ``FieldLoader.Load`` walks the TYPE's fields and never reads leftover keys),
      so this costs nothing at boot and everything in play — a superweapon whose
      ``~techlevel.superweapons`` gate evaporated is buildable at every tech level.
      Grep cannot find this: every individual line is valid MiniYAML.

  S2  UNGATED POWERS — a support power that resolves with no ``Prerequisites``
      node at all. Some are legitimate (the ``powerproxy.*`` internals, WC2
      spells that are meant to be free), so this REPORTS rather than fails, and
      only S1 overlap is treated as a defect.

  S3  DAMAGE — every weapon-firing power's weapon, with its resolved damage and
      warhead spread, so superweapon damage can be compared across factions
      without going through the unit formula.

⚠ Read powers through ``miniyaml.Ruleset.resolve`` (CLAUDE.md 8e). A support
power's ``Prerequisites``/``Icons``/``Names``/``Weapons`` are level MAPS, not
scalars: ``node.get('Weapons')`` returns '' for all of them.

EXIT CODE: 1 if S1 finds anything.
"""
from __future__ import annotations

import collections
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from cameo_model import Model  # noqa: E402

if hasattr(sys.stdout, "reconfigure"):          # Windows consoles default to cp1252
    sys.stdout.reconfigure(encoding="utf-8")

# `Power` is the base power-supply trait and `InfiltrateForSupportPower` is a
# spy-target marker; neither is a support power.
NOT_A_POWER = {"Power", "InfiltrateForSupportPower"}

WEAPON_FIRING = {"DetonateWeaponPower", "AirstrikePower", "IonCannonPower",
                 "FireArmamentPower", "NukePower"}
SPAWNING = {"ParatroopersPower", "SpawnActorPower", "ProduceActorPower"}

LEVEL_RE = re.compile(r"^(\t+)(\d+):\s*(\S.*)$")
KEY_VALUE_RE = re.compile(r"^(\t+)([\w@.]+):\s*(\S.*?)\s*$")
INDENT_RE = re.compile(r"^(\t*)")


def powers(rs):
    """{actor: [(trait_key, has_prerequisites)]} for every real support power."""
    out = collections.defaultdict(list)
    for name in rs.actors:
        if name.startswith(("^", "_")):
            continue
        node = rs.resolve(name)
        if node is None:
            continue
        for c in node.children:
            base = c.key.split("@")[0]
            if base.endswith("Power") and base not in NOT_A_POWER:
                out[name].append((c.key, base, c.child("Prerequisites") is not None, c))
    return out


def orphaned_level_maps(root="mods/cameo"):
    """S1 — level-map lines whose parent key already carries a scalar value."""
    found = []
    for f in sorted(pathlib.Path(root).rglob("*.yaml")):
        lines = f.read_text(encoding="utf-8-sig", errors="replace").splitlines()
        for i, line in enumerate(lines):
            m = LEVEL_RE.match(line)
            if not m:
                continue
            indent = len(m.group(1))
            for j in range(i - 1, -1, -1):
                prev = lines[j]
                if not prev.strip() or prev.lstrip().startswith("#"):
                    continue
                if len(INDENT_RE.match(prev).group(1)) >= indent:
                    continue
                kv = KEY_VALUE_RE.match(prev)
                # A trailing comment is a value to the regex but not to the engine.
                if kv and not kv.group(3).startswith("#"):
                    found.append({
                        "file": str(f), "parent_line": j + 1, "parent_key": kv.group(2),
                        "parent_value": kv.group(3), "line": i + 1,
                        "text": line.strip(),
                    })
                break
    return found


# Warhead types that deal no damage themselves but FIRE ANOTHER WEAPON. A
# superweapon's damage usually lives one or two levels down one of these, so a
# scan that stops at the top weapon reports zero for the GDI Ion Cannon.
DELEGATING = {"FireFragment", "FireCluster", "FireShrapnel", "FireRadius",
              "FireReverseRadius", "FireProjectilesToTarget"}


def weapon_damage(rs, weapon_name, _seen=None, _depth=0):
    """(total damage, max spread) for a weapon, following delegating warheads.

    ⚠ ``FireFragment``/``FireCluster`` warheads carry no ``Damage`` of their own —
    they name a child weapon in ``Weapons:``. ``TDIonCannon`` and
    ``SteelIonCannon`` are built entirely that way, so a non-recursive scan
    reports 0 for them and invites the conclusion that the GDI Ion Cannon is
    broken. It is not; the damage is one level down.
    """
    if _depth > 4:                      # delegation chains are shallow; stop runaway
        return 0, 0
    seen = _seen if _seen is not None else set()
    key = (weapon_name or "").strip().lower()
    if not key or key in seen:
        return 0, 0
    seen.add(key)
    w = rs.resolve_weapon(weapon_name)
    if w is None:
        return None if _depth == 0 else (0, 0)
    damage = 0
    spread = 0
    for c in w.children:
        if c.key.split("@")[0] != "Warhead":
            continue
        if (c.value or "").strip() in DELEGATING:
            child = c.child("Weapons") or c.child("Weapon")
            names = ([k.value for k in child.children] if child is not None
                     and child.children else
                     [child.value] if child is not None else [])
            for n in names:
                for part in (n or "").split(","):
                    d, s = weapon_damage(rs, part, seen, _depth + 1)
                    damage += d
                    spread = max(spread, s)
            continue
        d = c.get("Damage")
        if d:
            try:
                damage += int(str(d).strip())
            except ValueError:
                pass
        s = c.get("Spread")
        if s:
            try:
                spread = max(spread, int(str(s).strip().rstrip("c")))
            except ValueError:
                pass
    return damage, spread


def level_map(trait, key):
    """A support power level map as {level: value}; {} when absent."""
    node = trait.child(key)
    if node is None:
        return {}
    return {c.key: c.value for c in node.children}


def main():
    rs = Model().rs
    by_actor = powers(rs)
    flat = [(a, k, b, has, t) for a, ts in by_actor.items() for k, b, has, t in ts]

    print("# Support power audit\n")
    counts = collections.Counter(b for _a, _k, b, _h, _t in flat)
    print(f"{len(flat)} support power instances on {len(by_actor)} actors:\n")
    for base, n in counts.most_common():
        kind = ("weapon-firing" if base in WEAPON_FIRING
                else "spawning" if base in SPAWNING else "utility")
        print(f"  {base:32} {n:3}  {kind}")

    # ---------------------------------------------------------------- S1 ---
    orphans = orphaned_level_maps()
    print(f"\n\n## S1 — orphaned level maps ({len(orphans)})\n")
    if not orphans:
        print("_clean_")
    else:
        print("A level map indented under a key that already has a value. The "
              "engine drops it silently, so the gating the author wrote is NOT "
              "in effect. Insert the missing header key (usually "
              "`Prerequisites:`) above the map.\n")
        grouped = collections.defaultdict(list)
        for o in orphans:
            grouped[(o["file"], o["parent_line"], o["parent_key"],
                     o["parent_value"])].append(o)
        for (f, pl, pk, pv), group in sorted(grouped.items()):
            print(f"* `{f}:{pl}` — `{pk}: {pv}` swallows "
                  f"{len(group)} level line(s):")
            for o in group[:4]:
                print(f"    - line {o['line']}: `{o['text']}`")
            if len(group) > 4:
                print(f"    - ... and {len(group) - 4} more")

    # ---------------------------------------------------------------- S2 ---
    ungated = [(a, k) for a, k, _b, has, _t in flat if not has]
    print(f"\n\n## S2 — powers with no Prerequisites ({len(ungated)} of {len(flat)})\n")
    print("Reported, not failed: `powerproxy.*` are internal proxies and some "
          "spells are meant to be free. Cross-check against S1 — a power in "
          "BOTH lists lost its gate to the missing-header bug.\n")
    for a, k in sorted(ungated):
        print(f"  {a:44} {k}")

    # ---------------------------------------------------------------- S3 ---
    print("\n\n## S3 — superweapon damage\n")
    rows = []
    for a, k, base, _h, trait in flat:
        if base not in WEAPON_FIRING:
            continue
        weapons = level_map(trait, "Weapons") or level_map(trait, "MissileWeapons")
        single = trait.get("Weapon") or trait.get("MissileWeapon")
        if not weapons and single:
            weapons = {"1": single}
        charge = trait.get("ChargeInterval") or "-"
        if not weapons:
            rows.append((a, k, "-", "(actor-delivered)", "", "", charge))
            continue
        for lvl, wname in sorted(weapons.items()):
            dmg = weapon_damage(rs, (wname or "").strip())
            if dmg is None:
                rows.append((a, k, lvl, wname, "MISSING", "", charge))
            else:
                rows.append((a, k, lvl, wname, dmg[0], dmg[1], charge))
    print(f"{'actor':38}{'power':30}{'lv':>3} {'weapon':30}{'damage':>8}{'spread':>8}{'charge':>8}")
    for a, k, lvl, wname, dmg, spread, charge in sorted(rows):
        print(f"{a:38}{k:30}{lvl:>3} {str(wname):30}{str(dmg):>8}{str(spread):>8}{str(charge):>8}")
    print("\nA blank weapon means the power delivers damage through a SPAWNED "
          "ACTOR (an airstrike plane's armament), not a weapon of its own; those "
          "are priced through the actor, not here.")

    if orphans:
        print(f"\n**FAIL** — {len(orphans)} orphaned level map lines.")
        return 1
    print("\n**PASS**")
    return 0


if __name__ == "__main__":
    sys.exit(main())
