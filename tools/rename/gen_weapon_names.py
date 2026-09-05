#!/usr/bin/env python3
"""gen_weapon_names.py — derive DESIGN.md §1b weapon ids from the resolved tree.

    python tools/rename/gen_weapon_names.py            # report coverage only
    python tools/rename/gen_weapon_names.py --write    # emit the rename map

Maintainer 2026-08-29: *"the actor name plus the weapon type ... take into
account the warhead and the projectile name ... also take into consideration the
upgrades and upgrade names like the hyper velocity cannons upgrade."*

THE NAME
--------
    <actor_id>_<family>[_<qualifier>][_<variant>]

* ``family``    the base of the weapon's ``Inherits@wh: ^Warhead_<Family>_<Level>``,
                CamelCase-split and lowercased: ``CannonHE`` -> ``cannon``,
                ``MissileAA`` -> ``missile``, ``Sniper`` -> ``sniper``.
* ``qualifier`` the rest of that family, added ONLY to break a tie between two
                weapons of the same base on one actor: ``_he`` vs ``_ap``.
* ``variant``   the upgrade that swaps the weapon in, from the extra ``^`` template
                (``^HVProjectile`` -> ``hypervelocity``) or from the Armament's
                ``RequiresCondition`` naming a ``*_upgrade_*`` token. ``_elite`` is
                reserved for genuine veterancy (DESIGN §16.3).

⛔ THIS TOOL IS GATED ON W27. The family comes from the 3-way split, and only
**49.2%** of live weapons carry ``^Warhead_`` today (806 of 1637); 307 still sit
on legacy templates and 524 on none. Generating the map now would name half the
roster correctly, guess at the rest, and be invalidated the moment W27 rewrites
the inheritance it reads. ``--write`` refuses below ``--min-coverage`` (default
95) for that reason. Run it AFTER the weapon structure pass, not before.

⚠ SHARED WEAPONS ARE NOT ACTOR-SCOPED. 283 of 1637 live weapons (17.3%) are
fired by more than one actor — ``DemoTruckTargeting`` by 40, ``Pistol`` by 14
civilians, ``DefuseKit`` by 15 engineers across factions. "The actor that fires
it" has no answer for those, so they take a ``shared_`` name instead, exactly as
DESIGN §1 already rules for sprites shared between actors. Forcing an actor
prefix onto them would pick one of 40 owners arbitrarily and make every other
reference read as a bug.

⚠ SUPERSEDES ``tools/rename/rename_map_weapons.yaml`` (generator archived at
``tools/archive/gen_weapon_rename_map.py``). That map has 1560 entries, was never
applied — 1061 old names are still live and 0 new ones exist — and its scheme
discards the information this one preserves: it renders ``120mmDualHV`` as
``td_gdi_mammothtank_bullets_2``, losing both the CannonHE family and the
hyper-velocity upgrade.
"""

from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))

from cameo_model import Model  # noqa: E402

OUT = ROOT / "tools" / "rename" / "rename_map_weapons_v2.yaml"

WARHEAD_INHERIT = re.compile(r"^\^Warhead_(?P<family>\w+?)_(?:Light|Medium|Heavy|Super|Trace)\b")
CAMEL = re.compile(r"[A-Z][a-z]*|[A-Z]+(?![a-z])")

# Extra ^templates that mark an upgraded/variant weapon rather than a family.
VARIANT_TEMPLATES = {
    "^HVProjectile": "hypervelocity",
}

# Trailing nouns stripped from an upgrade's name group so the weapon suffix stays
# readable: `..._upgrade_hypervelocitycannons` -> `hypervelocity`, not
# `hypervelocitycannons` on a weapon that is already called `_cannon`.
UPGRADE_NOUNS = ("cannons", "cannon", "bullets", "bullet", "missiles", "missile",
                 "shells", "shell", "targeting", "rounds", "round", "ammo",
                 "lasers", "laser", "guns", "gun", "warheads", "warhead")


def firing_actors(rs):
    """{weapon: {actor: requires_condition}} over every Armament in the tree."""
    out = collections.defaultdict(dict)
    for name in rs.actors:
        if name.startswith(("^", "_")):
            continue
        node = rs.resolve(name)
        if node is None:
            continue
        for c in node.children:
            if c.key.split("@")[0] != "Armament":
                continue
            w = c.get("Weapon")
            if w:
                out[w.strip()][name] = (c.get("RequiresCondition") or "").strip()
    return out


# DESIGN.md §1 structural variant suffixes that carry meaning the family token
# cannot: an anti-air twin, an EMP twin, a veterancy-elite twin. They survive the
# rename verbatim — without them `CabalReaperMissiles` and its `_AA` sibling both
# reduce to `cabal_cyborgreaper_missile_he`.
STRUCTURAL_SUFFIXES = ("_AA", "_aa", "_EMP", "_elite")

# Some legacy names glue the AA marker on without the separator
# (`CabalLaserBoatLaserAA`), so the underscore forms alone miss them and the twin
# collides with its ground sibling.
BARE_AA = re.compile(r"(?<=[a-z])AA$")


def normalise(name):
    """A legacy weapon id as a DESIGN §1 name group: lowercase, no separators."""
    return re.sub(r"[^a-z0-9]", "", name.lower())


def structural_suffix(weapon):
    """Trailing DESIGN §1 structural suffixes on the OLD name, normalised."""
    out = ""
    stem = weapon
    while True:
        if BARE_AA.search(stem):
            out = "_aa" + out
            stem = BARE_AA.sub("", stem)
            continue
        for suf in STRUCTURAL_SUFFIXES:
            if stem.endswith(suf) and len(stem) > len(suf):
                out = suf.lower() + out
                stem = stem[: -len(suf)]
                break
        else:
            return out


def family_of(rs, weapon):
    """(base, qualifier) from the ^Warhead_ inherit, or (None, None) if unsplit."""
    node = rs.weapon(weapon)
    if node is None:
        return None, None
    for c in node.children:
        if not c.key.startswith("Inherits"):
            continue
        m = WARHEAD_INHERIT.match((c.value or "").strip())
        if m:
            parts = CAMEL.findall(m.group("family")) or [m.group("family")]
            return parts[0].lower(), "".join(parts[1:]).lower()
    return None, None


def variant_of(rs, weapon, condition):
    """The upgrade token that swaps this weapon in, or ''."""
    node = rs.weapon(weapon)
    if node is not None:
        for c in node.children:
            if c.key.startswith("Inherits"):
                tag = VARIANT_TEMPLATES.get((c.value or "").strip())
                if tag:
                    return tag
    # `<faction>_upgrade_<namegroup>` in the Armament's RequiresCondition.
    #
    # ⚠ A NEGATED condition means this is the BASE weapon, not the upgraded one.
    # An upgrade pair is wired as `!x` on the base and `x` on the replacement, so
    # matching the token without checking the `!` names them BOTH after the
    # upgrade — which is how `120mm` (the plain Battle Tank cannon) came out as
    # `td_gdi_battletank_cannon_he_highvelocity` and then collided with `120mmHV`.
    for m in re.finditer(r"(!?)\s*([A-Za-z0-9_]+)", condition or ""):
        negated, tok = m.group(1), m.group(2)
        if "_upgrade_" not in tok or negated:
            continue
        word = tok.split("_upgrade_")[-1]
        for noun in UPGRADE_NOUNS:
            if word.endswith(noun) and len(word) > len(noun):
                return word[: -len(noun)]
        return word
    return ""


def plan(rs):
    """Proposed name per live weapon, plus the reason when none can be derived."""
    fired = firing_actors(rs)
    proposals, undecidable = {}, []
    by_actor = collections.defaultdict(list)

    for weapon, actors in fired.items():
        base, qual = family_of(rs, weapon)
        if base is None:
            undecidable.append((weapon, "no ^Warhead_ inherit — awaiting W27"))
            continue
        if len(actors) > 1:
            # Shared: a shared_ name, never one of the N owners' prefixes.
            #
            # ⚠ Keyed on the weapon's OWN name, not its family. `shared_<family>`
            # reads better but is not unique: 19 different shared weapons are
            # Bullet-family, from `ChainGun` to `NaxiMP40Laser`, and collapsing
            # them onto `shared_bullet` renames nineteen distinct weapons to one
            # id. The old names are already unique, so normalising them is unique
            # by construction — the same reasoning DESIGN §1 uses to leave shared
            # SPRITES under their shared name.
            proposals[weapon] = ("shared", "shared_" + normalise(weapon))
            continue
        actor, condition = next(iter(actors.items()))
        by_actor[actor].append((weapon, base, qual, variant_of(rs, weapon, condition)))

    for actor, entries in by_actor.items():
        bases = collections.Counter(b for _w, b, _q, _v in entries)
        for weapon, base, qual, variant in entries:
            name = f"{actor}_{base}"
            if bases[base] > 1 and qual:          # tie-break on the family qualifier
                name += f"_{qual}"
            if variant:
                name += f"_{variant}"
            name += structural_suffix(weapon)
            proposals[weapon] = ("actor", name)

    # A collision means two weapons still reduce to one name. Resolve it
    # deterministically by appending each weapon's own normalised old name — the
    # old ids are unique, so the result is too — and RETURN the set so a human
    # reviews every one. Silently letting two weapons share a name would merge
    # them on apply, which is unrecoverable.
    seen = collections.defaultdict(list)
    for weapon, (_kind, name) in proposals.items():
        seen[name].append(weapon)
    collisions = {n: sorted(ws) for n, ws in seen.items() if len(ws) > 1}
    for name, weapons in collisions.items():
        for weapon in weapons:
            kind, _old = proposals[weapon]
            proposals[weapon] = (kind + "+tiebreak", f"{name}_{normalise(weapon)}")
    return proposals, undecidable, collisions


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--write", action="store_true", help="emit the rename map")
    ap.add_argument("--min-coverage", type=float, default=95.0,
                    help="refuse --write below this %% derivable (default 95)")
    args = ap.parse_args()

    rs = Model().rs
    proposals, undecidable, collisions = plan(rs)
    total = len(proposals) + len(undecidable)
    coverage = len(proposals) / total * 100 if total else 0.0

    kinds = collections.Counter(k for k, _n in proposals.values())
    print("# Weapon name plan (DESIGN.md §1b)\n")
    print(f"live weapons          : {total}")
    print(f"  derivable           : {len(proposals)} ({coverage:.1f}%)")
    print(f"    actor-scoped      : {kinds['actor']}")
    print(f"    shared_ namespace : {kinds['shared']}")
    print(f"  NOT derivable       : {len(undecidable)}")
    print(f"  name collisions     : {len(collisions)}")

    if collisions:
        print("\n## Collisions — two weapons reduce to one name\n")
        for name, ws in sorted(collisions.items())[:25]:
            print(f"  {name:44} <- {ws}")

    print("\n## Sample of the proposed mapping\n")
    for weapon in sorted(proposals)[:20]:
        kind, name = proposals[weapon]
        print(f"  {weapon:34} -> {name:52} [{kind}]")

    if not args.write:
        print("\n(report only; pass --write to emit the map)")
        return 0

    if coverage < args.min_coverage:
        print(f"\n**REFUSED** — {coverage:.1f}% derivable, below --min-coverage "
              f"{args.min_coverage}%.\n\nThe family token comes from the 3-way "
              "split's `^Warhead_` inherit, so a map generated now names the "
              "split weapons correctly and guesses at the rest — and W27 "
              "rewrites exactly the inheritance this reads. Finish the weapon "
              "structure pass, then run this again.", file=sys.stderr)
        return 1
    if collisions:
        print(f"\n**REFUSED** — {len(collisions)} collisions. Resolve them "
              "before writing a map that renames two weapons to one name.",
              file=sys.stderr)
        return 1

    with OUT.open("w", encoding="utf-8") as fh:
        fh.write("# rename_map_weapons_v2.yaml — generated by "
                 "tools/rename/gen_weapon_names.py\n")
        fh.write("# DESIGN.md §1b. Review before applying; apply with "
                 "tools/rename/safe_rename.py and BOOT-GATE the result.\n")
        fh.write("actors:\n")
        for weapon in sorted(proposals):
            fh.write(f"\t{weapon}: {proposals[weapon][1]}\n")
    print(f"\nwrote {OUT} ({len(proposals)} entries)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
