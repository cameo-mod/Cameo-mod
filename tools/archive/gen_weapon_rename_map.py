#!/usr/bin/env python3
"""Generate a unit-scoped context-sensitive weapon rename map.

Uses tools/audit/miniyaml.py and cameo_model.py to load the resolved ruleset,
find every weapon referenced by an actor's Armament block, classify the weapon
type, and propose a new name of the form:

    <game>_<faction>_<unit>_<weapontype>[_variant]

For multi-tier/multi-weapon units the unit id is the promotion/upgrade actor id
the weapon is actually attached to, and the type word is pluralised for dual
muzzle offsets.

Output: tools/rename/rename_map_weapons.yaml (ready for apply.py)
"""

from __future__ import annotations

import re
import sys
from collections import defaultdict
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent / "tools/audit"))

from cameo_model import Model, slugify
from miniyaml import Ruleset


ROOT = Path(__file__).resolve().parent

# Game prefix for faction collisions (must match gen_rename_maps.py)
FACTION_SLUG = {
    "gdi": ("td", "gdi"), "nod": ("td", "nod"),
    "tsgdi": ("ts", "gdi"), "tsnod": ("ts", "nod"),
    "cabal": ("", "cabal"), "forgotten": ("", "forgotten"),
    "allies": ("ra1", "allies"), "soviet": ("ra1", "soviet"),
    "modjapan": ("", "japan"),
    "ra2america": ("ra2", "allies"), "ra2russia": ("ra2", "soviet"),
    "yuri": ("", "yuri"),
    "asianalliance": ("", "asianalliance"), "consortium": ("", "steel_consortium"),
    "syndicate": ("", "latin_syndicate"), "naxis": ("", "naxis"),
    "lnaxis": ("", "schwarzer_mond"), "futuretech": ("", "futuretech"),
    "tkm": ("", "tkm"),
    "atreides": ("", "atreides"), "harkonnen": ("", "harkonnen"),
    "ordos": ("", "ordos"), "ixian": ("", "ixian"),
    "terran": ("", "terran"), "zerg": ("", "zerg"), "protoss": ("", "protoss"),
    "human2": ("", "humans"), "orc2": ("", "orcs"),
    "plymouthl": ("", "plymouth"), "edenl": ("", "eden"),
}


def armaments_of(node):
    """Yield Armament/Armament@* nodes for an actor Node."""
    for c in node.children:
        if c.key == "Armament" or c.key.startswith("Armament@"):
            yield c


def weapon_references_of(node):
    """Yield all Weapon: field values in any Armament block of node."""
    for a in armaments_of(node):
        w = a.get("Weapon")
        if w:
            yield w


def collect_armament_refs(rs: Ruleset):
    """weapon -> set(actor ids) for all actors that list Weapon: in an Armament."""
    refs: dict[str, set[str]] = defaultdict(set)
    for name, node in rs.actors.items():
        resolved = rs.resolve(name)
        if resolved is None:
            continue
        for w in weapon_references_of(resolved):
            if w:
                refs[w].add(name)
    return refs


def classify_weapon(rs: Ruleset, wname: str) -> str:
    """Return a short type word for a weapon based on its resolved traits."""
    w = rs.resolve_weapon(wname)
    if w is None:
        return "weapon"

    # direct Inherits chain
    inherits = [t for _, t in rs.inherits_of(w)]
    # resolve to get flattened children
    resolved = rs.resolve_weapon(wname)
    if resolved is None:
        resolved = w

    # gather projectile and warhead info from resolved node
    projectile = ""
    warhead_types = []
    for c in resolved.children:
        if c.key == "Projectile" and c.value:
            projectile = c.value.lower()
        if c.key.startswith("Warhead@"):
            warhead_types.append(c.value.lower())
    for c in resolved.children:
        if c.key == "Projectile" and c.value:
            projectile = c.value.lower()

    # also search any child with a Projectile node (some weapons inherit Projectile)
    if not projectile:
        for c in resolved.children:
            if c.key == "Projectile":
                # maybe a child 'Type' or 'Name'
                t = c.get("Type")
                if t:
                    projectile = t.lower()
                break

    # 1. explicit tesla zap
    if "tesla" in wname.lower() or "zap" in wname.lower():
        return "zap"

    # 2. laser / beam
    if "laser" in wname.lower() or "beam" in wname.lower():
        if "laser" in wname.lower():
            return "laser"
        return "beam"

    # 3. projectile-driven
    if projectile == "laserzap":
        return "beam"
    if projectile == "missile":
        return "missile"
    if projectile == "bullet":
        # bullet may be cannon, machinegun, or rifle depending on report/warheads
        # guess by inherited template
        for inh in inherits:
            il = inh.lower()
            if "cannon" in il:
                return "cannon"
            if "mg" in il or "machinegun" in il or "chaingun" in il:
                return "mg"
            if "rifle" in il or "smallarms" in il:
                return "rifle"
            if "sniper" in il:
                return "sniper"
            if "flak" in il:
                return "flak"
        # warhead based fallback
        for wh in warhead_types:
            if "cannon" in wh:
                return "cannon"
            if "smallarms" in wh:
                return "rifle"
            if "machinegun" in wh or "chaingun" in wh:
                return "mg"
            if "flak" in wh:
                return "flak"
        return "bullet"

    if projectile == "instant hit":
        return "zap" if "tesla" in wname.lower() else "beam"

    if projectile in ("ballistic", "ballisticweapon"):
        return "cannon"

    if projectile == "area beam":
        return "beam"

    if projectile == "gravity bomb":
        return "bomb"

    if projectile == "bullet":
        # check warhead
        if any("toxic" in wh or "chemical" in wh for wh in warhead_types):
            return "spray"
        return "rifle"

    if projectile == "missile":
        return "missile"

    # 4. by warhead alone
    for wh in warhead_types:
        whl = wh.lower()
        if "flame" in whl or "fire" in whl:
            return "flame"
        if "chemical" in whl or "toxic" in whl:
            return "spray"
        if "laser" in whl:
            return "laser"
        if "railgun" in whl:
            return "railgun"
        if "tesla" in whl or "electricity" in whl:
            return "zap"

    # 5. by inherited template
    for inh in inherits:
        il = inh.lower()
        if "cannon" in il:
            return "cannon"
        if "missile" in il:
            return "missile"
        if "laser" in il:
            return "laser"
        if "mg" in il or "machinegun" in il or "chaingun" in il:
            return "mg"
        if "rifle" in il or "smallarms" in il:
            return "rifle"
        if "sniper" in il:
            return "sniper"
        if "flak" in il:
            return "flak"
        if "flame" in il:
            return "flame"
        if "grenade" in il:
            return "grenade"
        if "bomb" in il:
            return "bomb"
        if "rocket" in il:
            return "rocket"
        if "railgun" in il:
            return "railgun"
        if "melee" in il:
            return "melee"

    # 6. name heuristics
    if "bomb" in wname.lower():
        return "bomb"
    if "missile" in wname.lower() or "rocket" in wname.lower():
        return "missile"
    if "cannon" in wname.lower():
        return "cannon"
    if "mg" in wname.lower() or "machinegun" in wname.lower() or "minigun" in wname.lower():
        return "mg"
    if "rifle" in wname.lower() or "gun" in wname.lower():
        return "rifle"
    if "sword" in wname.lower() or "slice" in wname.lower() or "punch" in wname.lower() or "claw" in wname.lower():
        return "melee"
    if "arrow" in wname.lower():
        return "arrows"
    if "axe" in wname.lower():
        return "axe"
    if "spear" in wname.lower():
        return "spear"
    if "flame" in wname.lower() or "fire" in wname.lower():
        return "flame"
    if "grenade" in wname.lower():
        return "grenade"
    if "mortar" in wname.lower():
        return "mortar"
    if "torpedo" in wname.lower():
        return "torpedo"

    return "weapon"


def pluralize_type(wtype: str, local_offset: str) -> str:
    """Pluralize if the armament has two offsets (twin muzzle)."""
    if not local_offset:
        return wtype
    # LocalOffset: 6 numbers separated by commas = 2 triplets
    parts = [p for p in re.split(r"[,\\s]+", local_offset) if p]
    return f"{wtype}s" if len(parts) == 6 else wtype


def actor_faction_prefix(actor_name: str, model: Model) -> str:
    """Return the game_faction prefix for the owning faction of actor."""
    owner = model.owner_of(actor_name)
    if owner:
        # owner may be theme/faction, internal faction name is the part after /
        faction = owner.split("/")[-1]
    else:
        # fallback: try to infer from actor name prefix
        parts = actor_name.split("_")
        for p in parts:
            if p in FACTION_SLUG:
                faction = p
                break
        else:
            faction = ""
    if not faction:
        return ""
    game, slug = FACTION_SLUG.get(faction, ("", slugify(faction)))
    return "_".join(p for p in (game, slug) if p)


def proposed_weapon_name(model: Model, rs: Ruleset, wname: str, actor_name: str, local_offset: str) -> str:
    """Propose a new unit-scoped weapon id."""
    prefix = actor_faction_prefix(actor_name, model)
    unit = actor_name.lower()
    # normalize: remove leading prefix if already present
    if prefix and unit.startswith(prefix + "_"):
        unit = unit[len(prefix) + 1:]
    unit = unit.replace(".", "_")
    wtype = classify_weapon(rs, wname)
    wtype = pluralize_type(wtype, local_offset)
    parts = [p for p in (prefix, unit, wtype) if p]
    return "_".join(parts).replace("__", "_")


def main() -> int:
    model = Model(ROOT)
    rs = model.rs
    refs = collect_armament_refs(rs)

    # Per weapon pick the most representative actor and generate a name.
    # If a weapon is referenced by multiple actors (shared), keep it unchanged
    # and warn. The user asked for unit-scoped names, so shared weapons need a
    # shared base name or will remain as shared templates.
    renames: dict[str, str] = {}
    ambiguous: list[tuple[str, list[str]]] = []
    skipped: list[str] = []

    for wname in sorted(rs.weapons):
        actors = refs.get(wname, set())
        if not actors:
            skipped.append(wname)
            continue
        if len(actors) > 1:
            # Multi-actor shared weapons: leave for manual review
            ambiguous.append((wname, sorted(actors)))
            continue

        actor = next(iter(actors))
        resolved_actor = rs.resolve(actor)
        local_offset = ""
        # find the specific Armament's LocalOffset
        if resolved_actor:
            for a in armaments_of(resolved_actor):
                if a.get("Weapon") == wname:
                    lo = a.get("LocalOffset")
                    if lo:
                        local_offset = lo
                    break

        new_name = proposed_weapon_name(model, rs, wname, actor, local_offset)
        # ensure no collisions
        n, i = new_name, 2
        while n in renames.values() and n != wname:
            n = f"{new_name}_{i}"
            i += 1
        renames[wname] = n

    print(f"weapons scanned: {len(rs.weapons)}")
    print(f"referenced by at least one actor: {len(refs)}")
    print(f"single-actor renames: {len(renames)}")
    print(f"multi-actor shared (skipped): {len(ambiguous)}")
    print(f"unreferenced (skipped): {len(skipped)}")

    # write a map file
    out_dir = ROOT / "tools/rename"
    out_dir.mkdir(parents=True, exist_ok=True)
    out_path = out_dir / "rename_map_weapons.yaml"
    lines = ["# rename_map_weapons.yaml — generated by gen_weapon_rename_map.py",
             "# Unit-scoped context-sensitive weapon names. Review before applying.",
             "actors:"]
    for old, new in sorted(renames.items()):
        lines.append(f"\t{old}: {new}")
    # Also include shared weapons in the map but commented for manual review
    if ambiguous:
        lines.append("# shared/multi-actor weapons (manual review required):")
    for w, acts in sorted(ambiguous):
        lines.append(f"#\t{w}:  # carriers: {', '.join(acts)}")
    out_path.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
    print(f"written: {out_path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
