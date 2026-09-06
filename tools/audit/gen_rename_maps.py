#!/usr/bin/env python3
"""gen_rename_maps.py — §9.1 naming-compliance report + rename map generation.

Grammar (RA1-Soviet baseline, see MASTER_REPORT §9.1):
  unit/building id :=  [game_]faction_name[_variant]      (no type words)
  tech item id     :=  [game_]faction_(upgrade|promotion|doctrine)_name
  sequence assets  :=  filenames stem = owning actor id; icons end _icon

For every real faction: checks each faction-exclusive actor id against the
grammar and writes a machine-readable proposal to
tools/rename/rename_map_<faction>.yaml with two sections:
  actors:  old_id: new_id
  files:   old_filename: new_filename       (sequence Filename entries)
DOES NOT apply anything — apply.py (§9.6) consumes these maps later.

Stdout: per-faction compliance percentages + collision warnings + asset
filename compliance (including the _icon suffix rule).
"""

from __future__ import annotations

import pathlib
import re
import sys
from collections import Counter, defaultdict

from cameo_model import Model, slugify
from report import h1, h2, table

# internal faction id -> (game prefix or "", faction slug)  per §9.1/§9.2.
# Game prefix ONLY where the faction name actually collides across games.
# ⛔ THE GAME PREFIX GOES IN THE `game` SLOT **OR** IN THE SLUG — NEVER BOTH.
# `want_prefix` below joins them: ("ra1", "ra1_soviets") produced `ra1_ra1_soviets_`,
# which NOTHING can match, so all eight factions whose slug already carried their game
# prefix reported **0% compliant** and this generator proposed doubling every id
# (`ra1_soviets_btr80` -> `ra1_ra1_soviets_btr80`) and QUADRUPLING sub-sprites
# (`ra1_soviets_btr80_new_btr.shp` ->
#  `ra1_ra1_soviets_btr80_ra1_soviets_btr80_new_btr.shp`).
# That fake 0% was read as a 526-actor renaming backlog for months. Fixed 2026-09-06 by
# emptying the `game` slot wherever the slug already begins with it.
FACTION_SLUG = {
    "td_gdi": ("", "td_gdi"), "td_nod": ("", "td_nod"),
    "ts_gdi": ("", "ts_gdi"), "ts_nod": ("", "ts_nod"),
    "cabal": ("", "cabal"), "forgotten": ("", "forgotten"),
    "ra1_allies": ("", "ra1_allies"), "ra1_soviets": ("", "ra1_soviets"),
    "modjapan": ("", "japan"),
    "ra2_allies": ("", "ra2_allies"), "ra2_soviets": ("", "ra2_soviets"),
    "yuri": ("", "yuri"),
    "asianalliance": ("", "asianalliance"), "steelconsortium": ("", "steelconsortium"),
    "latinsyndicate": ("", "latinsyndicate"), "naxis": ("", "naxis"),
    "schwarzermond": ("", "schwarzermond"), "futuretech": ("", "futuretech"),
    "tkm": ("", "tkm"),
    "atreides": ("", "atreides"), "harkonnen": ("", "harkonnen"),
    "ordos": ("", "ordos"), "ixian": ("", "ixian"),
    "terran": ("", "terran"), "zerg": ("", "zerg"), "protoss": ("", "protoss"),
    "human2": ("", "wc2_humans"), "orc2": ("", "wc2_orcs"),
    "plymouthl": ("", "plymouth"), "edenl": ("", "eden"),
}
VALID_ID = re.compile(r"^[a-z0-9_]+$")


def tech_marker(m: Model, lname: str) -> str | None:
    """'upgrade' | 'promotion' | 'doctrine' for tech items, else None."""
    res = m.rs.resolve(lname)
    if res is None:
        return None
    b = res.child("Buildable")
    queue = (b.get("Queue") or "").lower() if b else ""
    if "promotion" in queue:
        return "promotion"
    if "upgrade" in queue or "research" in queue:
        node = m.rs.actor(lname)
        for _, target in m.rs.inherits_of(node):
            if "doctrine" in target.lower():
                return "doctrine"
        return "upgrade"
    return None


def proposed_id(m: Model, faction: str, lname: str) -> str:
    game, slug = FACTION_SLUG.get(faction, ("", slugify(faction)))
    marker = tech_marker(m, lname)
    disp = m.display_name(lname)
    name = slugify(disp) if disp and not disp.startswith(("actor-", "meta-")) \
        else slugify(lname.replace(".", "_"))
    if disp.startswith("actor-"):
        name = slugify(disp.split(".")[0][len("actor-"):])
    if lname.endswith(".husk") or lname.endswith("husk"):
        if not name.endswith("husk"):
            name += "_husk"
    # display names often repeat the faction ("CABAL Core") — dedupe
    if name.startswith(slug + "_"):
        name = name[len(slug) + 1:]
    # RA1 baseline: the name is ONE group without underscores
    # (ra_heatraytank, ra_upgrade_nuclearshells), variants stay suffixed
    name = name.replace("_", "")
    parts = [p for p in (game, slug, marker, name) if p]
    out = "_".join(parts)
    return re.sub(r"_{2,}", "_", out)


def collect_filenames(m: Model) -> Counter:
    """How many sequence images reference each Filename (shared archives
    are exempt from per-actor naming)."""
    usage: Counter = Counter()
    for img, node in m.rs.sequences.items():
        seen: set[str] = set()
        for c in [node] + node.children:
            for sub in [c] + c.children:
                if sub.key == "Filename" and sub.value:
                    seen.add(sub.value.lower())
        for f in seen:
            usage[f] += 1
    return usage


def sequence_files_of(m: Model, image: str) -> dict[str, list[str]]:
    """sequence name -> [filenames] for one sequences image."""
    node = m.rs.sequence_image(image)
    out: dict[str, list[str]] = defaultdict(list)
    if node is None:
        return out
    for seq in node.children:
        for sub in [seq] + seq.children:
            if sub.key == "Filename" and sub.value:
                out[seq.key].append(sub.value)
    return out


def main() -> int:
    m = Model()
    out_dir = m.root / "tools/rename"
    out_dir.mkdir(parents=True, exist_ok=True)
    file_usage = collect_filenames(m)

    print(h1("gen_rename_maps — §9.1 naming compliance (RA1-Soviet baseline)"))
    rows, icon_rows = [], []
    factions = sorted(f.internal for f in m.real_factions())
    rosters = {fac: m.buildable_roster(fac) for fac in factions}

    for fac in factions:
        game, slug = FACTION_SLUG.get(fac, ("", slugify(fac)))
        if game and slug.startswith(game + "_"):
            raise AssertionError(
                f"FACTION_SLUG[{fac!r}] doubles its game prefix: "
                f"game={game!r} slug={slug!r} would want {game}_{slug}_. "
                "Put the game prefix in the `game` slot OR in the slug, never both.")
        want_prefix = "_".join(p for p in (game, slug) if p) + "_"
        others: set[str] = set()
        for g, r in rosters.items():
            if g != fac:
                others |= r
        owned = sorted(rosters[fac] - others)

        compliant, renames = [], {}
        collisions: dict[str, list[str]] = defaultdict(list)
        file_renames: dict[str, str] = {}
        icon_total = icon_ok = 0

        for lname in owned:
            new = None
            if lname.startswith(want_prefix) and VALID_ID.fullmatch(lname):
                compliant.append(lname)
            else:
                new = proposed_id(m, fac, lname)
                n, i = new, 2
                while n in collisions and lname not in collisions[n]:
                    n = f"{new}_{i}"
                    i += 1
                renames[lname] = n
                collisions[n].append(lname)

            # asset filename checks against the (new or current) id
            target_id = renames.get(lname, lname)
            res = m.rs.resolve(lname)
            image = ((res.get("RenderSprites", "Image") if res else None)
                     or lname).lower()
            for seq, files in sequence_files_of(m, image).items():
                for f in files:
                    lf = f.lower()
                    if file_usage[lf] > 3:
                        continue  # shared archive (DATA.R16 style)
                    stem = pathlib.PurePosixPath(lf).stem
                    ext = pathlib.PurePosixPath(lf).suffix
                    if seq.lower() == "icon":
                        icon_total += 1
                        if stem.endswith("_icon"):
                            icon_ok += 1
                        want = f"{target_id}_icon{ext}"
                        if stem != f"{target_id}_icon":
                            file_renames[f] = want
                    elif not stem.startswith(target_id):
                        file_renames[f] = f"{target_id}{ext}" \
                            if stem == lname else f"{target_id}_{stem}{ext}"

        dupes = {k: v for k, v in collisions.items() if len(v) > 1}
        pct = f"{100 * len(compliant) // len(owned)}%" if owned else "—"
        ipct = f"{100 * icon_ok // icon_total}%" if icon_total else "—"
        rows.append([fac, f"{len(compliant)}/{len(owned)}", pct,
                     str(len(dupes)), str(len(file_renames))])
        icon_rows.append([fac, f"{icon_ok}/{icon_total}", ipct])

        if renames or file_renames:
            path = out_dir / f"rename_map_{fac}.yaml"
            lines = [f"# rename_map_{fac}.yaml — generated by gen_rename_maps.py",
                     "# §9.1 grammar (RA1-Soviet baseline); NOT applied — see §9.6",
                     "actors:"]
            for old, new in sorted(renames.items()):
                lines.append(f"\t{old}: {new}")
            lines.append("files:")
            for old, new in sorted(file_renames.items()):
                lines.append(f"\t{old}: {new}")
            path.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")

    print(h2("Actor-id compliance per faction (faction-exclusive buildables)"))
    print(table(["faction", "compliant", "%", "proposal collisions",
                 "asset files to rename"], rows))
    print(h2("Icon filename compliance (_icon suffix rule)"))
    print(table(["faction", "icons compliant", "%"], icon_rows))
    print("\n_Ownership is data-driven: an actor counts for a faction only if "
          "no other faction's prerequisite closure can build it. Sequence "
          "filenames referenced by more than 3 images are treated as shared "
          "archives and exempted. Rename proposals written to "
          "tools/rename/rename_map_<faction>.yaml (actors: + files: sections); "
          "collisions need manual `_variant` suffixes before applying._\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
