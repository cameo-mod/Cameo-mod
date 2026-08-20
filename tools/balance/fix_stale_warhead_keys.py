#!/usr/bin/env python3
"""fix_stale_warhead_keys.py — repair stale old-name warhead keys left by the 3-way
retrofit's one-pass repair (a grandchild of a converted template kept `Warhead@<old>`
because, at provides-build time, its not-yet-repaired parent still advertised the old
key). Such a key no longer OVERRIDES the inherited warhead — it ADDS a second same-family
warhead (double damage). This renames `Warhead@<old>[suffix]` / `-Warhead@<old>[suffix]`
to the new key IFF the block's resolved parent chain provides the NEW key and NOT the old
one (so the rename restores the intended override; the Damage value is untouched).

Uses CURRENT-state provides (intermediates already repaired), so the detection is exact.
BOM-safe read, LF output. Idempotent. Usage: fix_stale_warhead_keys.py [--apply]
"""
from __future__ import annotations
import argparse, os, re, sys
sys.path.insert(0, os.path.dirname(__file__))
from retrofit_weapon_family import TRIPLE, ROOT, parse_blocks, INH_ANY

OLD2NEW = {old: t[0] for old, t in TRIPLE.items()}
SUFFIXES = ["_Percentage", "_FriendlyFire", "_ExtraDamage", "Percentage", "FriendlyFire", "ExtraDamage", ""]


def split_key(k):
    for s in SUFFIXES:
        if s and k.endswith(s):
            return k[:-len(s)], s
    return k, ""


def build_provides(files):
    own_defs, own_rm, parents = {}, {}, {}
    for p in files:
        lines = open(p, encoding="utf-8-sig").read().split("\n")
        for s, e, name in parse_blocks(lines):
            b = name.lstrip("^")
            pd = own_defs.setdefault(b, set()); pr = own_rm.setdefault(b, set()); par = parents.setdefault(b, [])
            for i in range(s, e):
                m = INH_ANY.match(lines[i])
                if m: par.append(m.group(3))
                m = re.match(r"^\s*Warhead@([\w.]+):", lines[i])
                if m: pd.add(m.group(1))
                m = re.match(r"^\s*-Warhead@([\w.]+):", lines[i])
                if m: pr.add(m.group(1))
    prov = {b: set() for b in own_defs}
    for _ in range(80):
        ch = False
        for b in own_defs:
            acc = set()
            for q in parents.get(b, []):
                acc |= prov.get(q, set())
            acc = (acc - own_rm[b]) | own_defs[b]
            if acc != prov[b]:
                prov[b] = acc; ch = True
        if not ch:
            break
    return prov, parents


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    a = ap.parse_args()
    files = [os.path.join(dp, fn) for dp, _, fs in os.walk(ROOT) for fn in fs if fn.endswith(".yaml")]
    fixes = []
    # iterate to a FIXPOINT: renaming an intermediate exposes its grandchildren, so
    # rebuild provides and sweep again until a pass makes no change. Dry-run reports the
    # first pass only (accurate for a fully-repaired tree, where that pass finds 0).
    while True:
        prov, parents = build_provides(files)
        pass_fixes = []
        for p in files:
            lines = open(p, encoding="utf-8-sig").read().split("\n")
            touched = False
            for s, e, name in parse_blocks(lines):
                b = name.lstrip("^")
                if name.startswith("^") and b in TRIPLE:
                    continue  # never touch an old base definition
                parprov = set()
                for q in parents.get(b, []):
                    parprov |= prov.get(q, set())
                for i in range(s, e):
                    m = re.match(r"^(\t)(-?)Warhead@([\w.]+):(.*)$", lines[i])
                    if not m:
                        continue
                    base, suf = split_key(m.group(3))
                    if base not in OLD2NEW:
                        continue
                    oldk = m.group(3)
                    # new twin keys are underscore-separated (MissileAP_Light_Percentage),
                    # old ones concatenated (LightMissilePercentage) -> normalise the suffix.
                    newk = OLD2NEW[base] + ("_" + suf.lstrip("_") if suf else "")
                    if oldk not in parprov and newk in parprov:
                        lines[i] = f"{m.group(1)}{m.group(2)}Warhead@{newk}:{m.group(4)}"
                        pass_fixes.append((os.path.relpath(p, ROOT), name, oldk, newk))
                        touched = True
            if touched and a.apply:
                open(p, "w", encoding="utf-8", newline="\n").write("\n".join(lines))
        fixes.extend(pass_fixes)
        if not a.apply or not pass_fixes:
            break  # dry-run = single pass; apply = loop until stable
    mode = "APPLIED" if a.apply else "DRY RUN"
    print(f"[{mode}] {len(fixes)} stale warhead-key renames")
    for f, n, o, k in fixes:
        print(f"    {f}: {n}  Warhead@{o} -> Warhead@{k}")


if __name__ == "__main__":
    main()
