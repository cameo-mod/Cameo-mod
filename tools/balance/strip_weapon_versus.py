#!/usr/bin/env python3
"""strip_weapon_versus.py — enforce cameo-versus-only-in-templates: remove nested
`Versus:` blocks from WEAPON-level `Warhead@…` overrides. Versus is legal ONLY inside
the fixed `^Warhead_*` templates.

SAFE condition: a weapon's `Warhead@<key>: …{ Versus }` is stripped ONLY when <key> is a
key defined by a `^Warhead_*` template AND the block's resolved parent chain provides that
key (i.e. the weapon actually inherits the warhead template, which supplies the Versus).
Weapons whose warhead has no backing template are left alone (their Versus is the only
source — reassign them to a proper template instead of blind-stripping).

Reports per-file; strips the `Versus:` line + its deeper-indented value lines only. This is
a BALANCE change (weapons adopt the template Versus). BOM-safe, LF output.
Usage: strip_weapon_versus.py [--apply] [--live-only]
"""
from __future__ import annotations
import argparse, os, re
from retrofit_weapon_family import ROOT, parse_blocks, INH_ANY  # reuse

LIVE_CENTRAL = {"weapons.yaml", "tiberiandawn.yaml", "redalert2mod.yaml", "d2k.yaml",
                "starcraft.yaml", "warcraft2.yaml", "tiberiansun.yaml", "outpost2.yaml"}


def is_live(path):
    rp = os.path.relpath(path, ROOT).replace("\\", "/")
    if rp.startswith("ContentPacks/"):
        return True
    if rp.startswith("weapons/"):
        return os.path.basename(rp) in LIVE_CENTRAL
    return True


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--live-only", action="store_true")
    a = ap.parse_args()
    files = [os.path.join(dp, fn) for dp, _, fs in os.walk(ROOT) for fn in fs if fn.endswith(".yaml")]

    # keys defined by ^Warhead_* templates (legal-Versus keys) + provides graph
    wh_keys = set()
    own_defs, parents = {}, {}
    for p in files:
        lines = open(p, encoding="utf-8-sig").read().split("\n")
        for s, e, name in parse_blocks(lines):
            b = name.lstrip("^")
            pd = own_defs.setdefault(b, set()); par = parents.setdefault(b, [])
            for i in range(s, e):
                m = INH_ANY.match(lines[i])
                if m: par.append(m.group(3))  # group(3)=parent name (group(2)=@tag)
                m = re.match(r"^\tWarhead@([\w.]+):", lines[i])
                if m:
                    pd.add(m.group(1))
                    if name.startswith("^Warhead_"):
                        wh_keys.add(m.group(1))
    prov = {b: set() for b in own_defs}
    for _ in range(80):
        ch = False
        for b in own_defs:
            acc = set(own_defs[b])
            for q in parents.get(b, []): acc |= prov.get(q, set())
            if acc != prov[b]: prov[b] = acc; ch = True
        if not ch: break

    from collections import Counter
    per_file = Counter(); total = 0
    for p in files:
        if a.live_only and not is_live(p):
            continue
        lines = open(p, encoding="utf-8-sig").read().split("\n")
        drop = set(); versus_blocks = 0
        for s, e, name in parse_blocks(lines):
            b = name.lstrip("^")
            if name.startswith("^Warhead_"):
                continue  # the fixed templates KEEP their Versus
            parprov = set()
            for q in parents.get(b, []): parprov |= prov.get(q, set())
            cur = None
            for i in range(s, e):
                m = re.match(r"^\tWarhead@([\w.]+):", lines[i])
                if m:
                    cur = m.group(1); continue
                if re.match(r"^\t\tVersus:", lines[i]) and cur in wh_keys and cur in parprov:
                    drop.add(i); versus_blocks += 1
                    j = i + 1
                    while j < e and re.match(r"^\t\t\t", lines[j]):
                        drop.add(j); j += 1
        if drop:
            per_file[os.path.relpath(p, ROOT)] = versus_blocks
            total += versus_blocks
            if a.apply:
                new = [ln for k, ln in enumerate(lines) if k not in drop]
                open(p, "w", encoding="utf-8", newline="\n").write("\n".join(new))
    mode = "APPLIED" if a.apply else "DRY RUN"
    scope = "LIVE-only" if a.live_only else "ALL (incl. dead)"
    print(f"[{mode}] {scope}: stripped {total} weapon Versus blocks across {len(per_file)} files")
    for f, c in per_file.most_common(): print(f"    {c:4d}  {f}")


if __name__ == "__main__":
    main()
