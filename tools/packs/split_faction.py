#!/usr/bin/env python3
"""split_faction.py — move one faction out of monolith yaml into a ContentPack.

Usage:
  python tools/packs/split_faction.py --theme TiberianSun --faction forgotten \
      --prefix forgotten_ --rules tiberiansun.yaml --weapons tiberiansun.yaml \
      --sequences tiberiansun.yaml

Moves, byte-preserving, every top-level block whose key starts with the
faction prefix (post-rename ids make ownership trivial):
- rules blocks   -> ContentPacks/<Theme>/<Faction>/rules/<category>.yaml
- weapons used exclusively by the faction -> .../weapons/weapons.yaml
- sequence images owned by the faction    -> .../sequences/sequences.yaml
plus faction-exclusive ^Templates referenced only from moved blocks.

Writes/extends the pack's content.yaml and adds the Include to mod.yaml
(after the theme's existing include). Verify with the audit suite +
registry-equality check afterwards; the move is behavior-neutral because
MiniYAML merges all top-level documents before resolution.
"""

from __future__ import annotations

import argparse
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
MOD = ROOT / "mods/cameo"
sys.path.insert(0, str(ROOT / "tools/audit"))

from cameo_model import Model  # noqa: E402


def blocks_of(path: pathlib.Path) -> list[tuple[str, str]]:
    """[(top_level_key, block_text_incl_trailing_blank)] preserving bytes."""
    text = path.read_text(encoding="utf-8-sig")
    lines = text.split("\n")
    out, cur_key, start = [], None, 0
    for i, line in enumerate(lines + ["<EOF>"]):
        is_top = line and not line[0] in "\t #" and ":" in line
        if is_top or line == "<EOF>":
            if cur_key is not None:
                out.append((cur_key, "\n".join(lines[start:i])))
            elif i > 0:
                out.append(("", "\n".join(lines[start:i])))  # header/comments
            cur_key = line.split(":", 1)[0].strip() if line != "<EOF>" else None
            start = i
    return out


def write_blocks(path: pathlib.Path, blocks: list[str]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    body = "\n".join(b.rstrip("\n") + "\n" for b in blocks)
    path.write_text(body, encoding="utf-8", newline="\n")


CATEGORY_FILES = {
    "inf": "infantry.yaml", "veh": "vehicles.yaml", "air": "aircraft.yaml",
    "nav": "naval.yaml", "bld": "buildings.yaml", "def": "defenses.yaml",
    "upg": "upgrades.yaml", "husk": "husks.yaml",
}


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--theme", required=True)
    ap.add_argument("--faction", required=True)     # folder name
    ap.add_argument("--prefix", required=True)      # id prefix, e.g. forgotten_
    ap.add_argument("--rules", required=True)       # monolith file names under
    ap.add_argument("--weapons", required=True)     # mods/cameo/{rules,weapons,
    ap.add_argument("--sequences", required=True)   #  sequences}/
    args = ap.parse_args()

    m = Model(ROOT)
    rs = m.rs
    pack = MOD / "ContentPacks" / args.theme / args.faction
    prefix = args.prefix.lower()

    # ---- rules ------------------------------------------------------------- #
    rules_path = MOD / "rules" / args.rules
    blocks = blocks_of(rules_path)
    owned_keys = {k for k, _ in blocks if k.lower().startswith(prefix)}

    # faction-exclusive ^templates: referenced only from owned blocks
    ref_counts: dict[str, set[str]] = {}
    for k, b in blocks:
        for mo in re.finditer(r"Inherits(?:@[\w.\-]+)?:\s*(\^[\w.\-]+)", b):
            ref_counts.setdefault(mo.group(1), set()).add(k)
    for tpl, users in ref_counts.items():
        tkey = tpl.lstrip("^")
        for k, _ in blocks:
            if k == tpl:
                if users <= owned_keys | {tpl}:
                    owned_keys.add(tpl)

    cats: dict[str, list[str]] = {}
    remaining: list[str] = []
    promoted: list[str] = []
    for k, b in blocks:
        if k in owned_keys:
            if k.startswith("^"):
                cats.setdefault("templates.yaml", []).append(b)
                continue
            ut = m.unit_type(k)
            b_lower = b.lower()
            if "queue: promotion" in b_lower:
                fname = "promotions.yaml"
            else:
                fname = CATEGORY_FILES.get(ut, "misc.yaml")
            cats.setdefault(fname, []).append(b)
        else:
            remaining.append(b)
    for fname, bl in sorted(cats.items()):
        write_blocks(pack / "rules" / fname, bl)
        promoted.append(f"rules/{fname} ({len(bl)})")
    write_blocks(rules_path, remaining)

    # ---- weapons: used exclusively by owned actors -------------------------- #
    weap_path = MOD / "weapons" / args.weapons
    wblocks = blocks_of(weap_path)
    WEAPON_FIELDS = ("Weapon", "EmptyWeapon", "ExplosionWeapon",
                     "AirburstWeapon", "ImpactActorWeapon", "DetonationWeapon")
    users: dict[str, set[str]] = {}
    for name in rs.actors:
        res = rs.resolve(name)
        if res is None:
            continue
        tag = "own" if name.lower().startswith(prefix) else "other"
        for c in res.children:
            for f in WEAPON_FIELDS:
                v = c.get(f)
                if v:
                    users.setdefault(v.lower(), set()).add(tag)
    # propagate through weapon->weapon references (warhead sub-weapons and
    # Inherits): a weapon referenced by a shared weapon is shared
    for _ in range(6):
        changed = False
        for wname in rs.weapons:
            w = rs.weapons.get(wname)
            if w is None:
                continue
            tags = users.get(wname.lower(), set())
            if not tags:
                continue
            refs = []
            wres = rs.resolve_weapon(wname)
            for c in (wres.children if wres else []):
                v = c.get("Weapon")
                if v:
                    refs.append(v.lower())
            for _, tgt in rs.inherits_of(w):
                if not tgt.startswith("^"):
                    refs.append(tgt.lower())
            for r in refs:
                before = set(users.get(r, set()))
                users.setdefault(r, set()).update(tags)
                changed |= users[r] != before
        if not changed:
            break
    wmoved, wremain = [], []
    for k, b in wblocks:
        if k and not k.startswith("^") and users.get(k.lower()) == {"own"}:
            wmoved.append(b)
        else:
            wremain.append(b)
    if wmoved:
        write_blocks(pack / "weapons" / "weapons.yaml", wmoved)
        write_blocks(weap_path, wremain)
        promoted.append(f"weapons/weapons.yaml ({len(wmoved)})")

    # ---- sequences: images named after owned actors ------------------------- #
    seq_path = MOD / "sequences" / args.sequences
    sblocks = blocks_of(seq_path)
    smoved, sremain = [], []
    for k, b in sblocks:
        if k.lower().startswith(prefix):
            smoved.append(b)
        else:
            sremain.append(b)
    if smoved:
        write_blocks(pack / "sequences" / "sequences.yaml", smoved)
        write_blocks(seq_path, sremain)
        promoted.append(f"sequences/sequences.yaml ({len(smoved)})")

    # ---- content.yaml + mod.yaml -------------------------------------------- #
    lines = ["Rules:"]
    for fname in sorted(cats):
        lines.append(f"\tContentPacks|{args.theme}/{args.faction}/rules/{fname}")
    if wmoved:
        lines += ["", "Weapons:",
                  f"\tContentPacks|{args.theme}/{args.faction}/weapons/weapons.yaml"]
    if smoved:
        lines += ["", "Sequences:",
                  f"\tContentPacks|{args.theme}/{args.faction}/sequences/sequences.yaml"]
    (pack / "content.yaml").write_text("\n".join(lines) + "\n",
                                       encoding="utf-8", newline="\n")

    mod_yaml = MOD / "mod.yaml"
    t = mod_yaml.read_text(encoding="utf-8-sig")
    inc = f"Include: ContentPacks/{args.theme}/{args.faction}/content.yaml"
    if inc not in t:
        theme_inc = f"Include: ContentPacks/{args.theme}/content.yaml"
        assert theme_inc in t, f"{theme_inc} not in mod.yaml"
        t = t.replace(theme_inc, theme_inc + "\n" + inc)
        mod_yaml.write_text(t, encoding="utf-8", newline="\n")

    print(f"split {args.faction}: " + "; ".join(promoted))
    return 0


if __name__ == "__main__":
    sys.exit(main())
