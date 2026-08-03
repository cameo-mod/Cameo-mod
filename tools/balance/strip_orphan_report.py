#!/usr/bin/env python3
"""strip_orphan_report.py — remove `-Report:` / `-StartReport:` / `-StartBurstReport:`
lines whose resolved parent chain no longer provides that report field.

Context: the weapon 3-way split keeps the default firing sound (`Report`) in the
PROJECTILE template. Families with NO projectile template (Shrapnel/Concussion,
HeavyBomb/Demolition_Heavy, Nuclear) therefore provide no default Report, so any
`-Report:` a converted weapon kept (to silence the old base's Report) now removes
nothing and crashes the boot ("no elements with key `Report` to remove"). This strips
exactly those orphaned removals; legitimate `-Report:` in projectile families (where a
template still provides a Report) are left untouched.

Resolution-based: a report field is provided to a block's children iff the block sets
it (own `Report:`) or inherits it and does not itself remove it. A `-Report:` is
orphaned iff NONE of the block's parents provides that field.

BOM-safe read, LF output. Idempotent. Usage: strip_orphan_report.py [--apply]
"""
from __future__ import annotations
import argparse, os, re

ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "mods", "cameo")
FIELDS = ["Report", "StartReport", "StartBurstReport"]
TOP = re.compile(r"^(﻿?)(\^?[\w.]+):\s*$")
INH_ANY = re.compile(r"^\tInherits(@[\w.]+)?:\s*\^?([\w.]+)\s*(?:#.*)?$")


def parse_blocks(lines):
    idxs = [(i, m.group(2)) for i, ln in enumerate(lines)
            if (m := TOP.match(ln)) and not ln.startswith((" ", "\t"))]
    for j, (i, name) in enumerate(idxs):
        end = idxs[j + 1][0] if j + 1 < len(idxs) else len(lines)
        yield i, end, name


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    a = ap.parse_args()

    files = [os.path.join(dp, fn) for dp, _, fs in os.walk(ROOT)
             for fn in fs if fn.endswith(".yaml")]
    # gather graph across ALL files (report inheritance is global)
    parents, own_set, own_rm = {}, {}, {}
    blockfiles = {}  # bare name -> (path, s, e)
    filelines = {}
    for p in files:
        lines = open(p, encoding="utf-8-sig").read().split("\n")
        filelines[p] = lines
        for s, e, name in parse_blocks(lines):
            bare = name.lstrip("^")
            par, oset, orm = [], set(), set()
            for i in range(s, e):
                m = INH_ANY.match(lines[i])
                if m:
                    par.append(m.group(2))
                for f in FIELDS:
                    if re.match(rf"^\t{f}:", lines[i]):
                        oset.add(f)
                    if re.match(rf"^\t-{f}:", lines[i]):
                        orm.add(f)
            parents[bare] = par
            own_set[bare] = oset
            own_rm[bare] = orm
            blockfiles.setdefault(bare, (p, s, e))

    # fixpoint: provides[b][f] = f in own_set OR (some parent provides f AND f not removed)
    provides = {b: set() for b in parents}
    for _ in range(64):
        changed = False
        for b in parents:
            acc = set()
            for f in FIELDS:
                if f in own_set[b]:
                    acc.add(f)
                elif f not in own_rm[b] and any(f in provides.get(pp, set()) for pp in parents[b]):
                    acc.add(f)
            if acc != provides[b]:
                provides[b] = acc
                changed = True
        if not changed:
            break

    # remove orphaned removals
    to_remove = []  # (path, lineidx)
    for p in files:
        lines = filelines[p]
        for s, e, name in parse_blocks(lines):
            bare = name.lstrip("^")
            parprov = set().union(*(provides.get(pp, set()) for pp in parents[bare])) if parents[bare] else set()
            for i in range(s, e):
                for f in FIELDS:
                    if re.match(rf"^\t-{f}:", lines[i]) and f not in parprov:
                        to_remove.append((p, i, name, f))

    byfile = {}
    for p, i, name, f in to_remove:
        byfile.setdefault(p, []).append((i, name, f))
    for p, items in byfile.items():
        if a.apply:
            drop = {i for i, _, _ in items}
            new = [ln for k, ln in enumerate(filelines[p]) if k not in drop]
            open(p, "w", encoding="utf-8", newline="\n").write("\n".join(new))

    mode = "APPLIED" if a.apply else "DRY RUN"
    print(f"[{mode}] {len(to_remove)} orphaned report-removals across {len(byfile)} files")
    for p, i, name, f in to_remove:
        print(f"    {os.path.relpath(p, ROOT)}: {name}  -{f}:")


if __name__ == "__main__":
    main()
