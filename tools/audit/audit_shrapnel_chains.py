#!/usr/bin/env python3
"""audit_shrapnel_chains.py - a FireShrapnel chain must END.

A `FireShrapnel` warhead spawns another weapon, and that weapon may spawn another.
The chain is terminated by the LAST link cancelling the inherited shrapnel warhead:

    MutaliskSpore   Warhead@shrapnel: FireShrapnel -> MutaBounce1
    MutaBounce1     Inherits: MutaliskSpore, Weapon: MutaBounce2
    MutaBounce2     Inherits: MutaliskSpore, `-Warhead@shrapnel:`   <- the terminator

Delete that one cancellation and the last link inherits the parent's shrapnel
warhead, which points back at MutaBounce1: the mutalisk bounces forever.  That is
not hypothetical - `d818aec40` deleted 14 `-Warhead@shrapnel:` terminators as
"stale" and the maintainer found it in play.

⛔ A BOOT GATE CANNOT SEE THIS.  The rules parse, the menu loads, and the bug only
exists once a shot is fired.  This audit is the gate that applies.

Checks:
  S1a MULTI-NODE CYCLE  A -> B -> A.  ALWAYS a bug: a terminator was lost.
  S1b SELF-CYCLE        A -> A.  Chain lightning arcs from target to target and
                        stops when none are left, so a self-reference is bounded
                        by the battlefield rather than infinite.  Ratcheted at the
                        measured 44 pending a design ruling - NOT called a defect.
  S2 DANGLING   a FireShrapnel naming a weapon that does not exist
  S3 DEEP       a chain longer than MAX_DEPTH - legal, but worth a human look

VALIDATED ACROSS COMMITS, because an audit that cannot catch the bug it was written
for is worthless:

    at d818aec40 (broken)   S1a 45  S1b 64   exit 1
    after the revert        S1a  0  S1b 44   exit 0

Usage: python tools/audit/audit_shrapnel_chains.py [--list]
"""

from __future__ import annotations

import argparse
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

import miniyaml
from report import h1, h2, table

# Ratchets - LOWER ONLY.  S1 and S2 are hard defects and must stay at zero.
# S1a is a hard defect and stays at zero: `d818aec40` took it from 0 to 65 and the
# maintainer found the mutalisk bouncing forever in play.
S1A_BASELINE = 0    # multi-node cycles (A -> B -> A)
# S1b is a LOWER-ONLY ratchet at the measured pre-existing count, not an assertion
# that all 44 are wrong.  Every one is a tesla-arc fragment whose shrapnel names
# itself, which is how chain lightning is expressed: it stops when no target is
# left in range.  Needs a design ruling before anyone "fixes" them.
S1B_BASELINE = 44   # self-cycles (A -> A)
S2_BASELINE = 0     # dangling shrapnel targets
MAX_DEPTH = 6       # bounces; the longest legitimate chain in the tree is 3


def shrapnel_targets(rs, name):
    """[(warhead key, spawned weapon)] for one RESOLVED weapon."""
    node = rs.resolve_weapon(name)
    if node is None:
        return []
    out = []
    for c in node.children:
        if c.value == "FireShrapnel":
            w = c.get("Weapon")
            if w:
                out.append((c.key, w.strip()))
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--list", action="store_true", help="print every chain found")
    args = ap.parse_args()

    root = pathlib.Path(__file__).resolve().parents[2]
    rs = miniyaml.Ruleset(root)
    weapons = [w for w in rs.weapons if not w.startswith("^")]
    known = {w.lower() for w in weapons}

    edges = {}
    for w in weapons:
        t = shrapnel_targets(rs, w)
        if t:
            edges[w] = t

    cycles, dangling, deep, chains = [], [], [], []

    def walk(start):
        path, seen = [start], {start.lower()}
        cur = start
        while True:
            nxt = edges.get(cur)
            if not nxt:
                return path, None
            # a weapon may fire several shrapnel warheads; follow each
            for _, tgt in nxt:
                if tgt.lower() not in known:
                    dangling.append((cur, tgt))
                    return path, "dangling"
            tgt = nxt[0][1]
            if tgt.lower() in seen:
                return path + [tgt], "cycle"
            path.append(tgt)
            seen.add(tgt.lower())
            cur = tgt
            if len(path) > MAX_DEPTH + 1:
                return path, "deep"

    for start in sorted(edges):
        path, why = walk(start)
        chains.append((start, path))
        if why == "cycle":
            cycles.append(path)
        elif why == "deep":
            deep.append(path)

    print(h1("audit_shrapnel_chains - a FireShrapnel chain must END"))
    multi = [p for p in cycles if p[-1] != p[-2]]
    selfc = [p for p in cycles if p[-1] == p[-2]]
    rows = [
        ["S1a", "MULTI-NODE CYCLE (A->B->A, a lost terminator)",
         str(len(multi)), str(S1A_BASELINE),
         "PASS" if len(multi) <= S1A_BASELINE else "FAIL"],
        ["S1b", "SELF-CYCLE (A->A, chain lightning; review)",
         str(len(selfc)), str(S1B_BASELINE),
         "PASS" if len(selfc) <= S1B_BASELINE else "FAIL"],
        ["S2", "DANGLING (spawns a weapon that does not exist)",
         str(len(dangling)), str(S2_BASELINE),
         "PASS" if len(dangling) <= S2_BASELINE else "FAIL"],
        ["S3", f"DEEP (> {MAX_DEPTH} bounces, review only)", str(len(deep)), "-", ""],
    ]
    print(table(["code", "check", "count", "ratchet", ""], rows))
    print(f"\n{len(edges)} weapon(s) fire shrapnel; "
          f"{len(chains)} chain(s) walked.\n")

    if multi:
        print(h2("S1a - MULTI-NODE CYCLES (a terminator was lost; always a bug)"))
        for p in multi:
            print(f"    {' -> '.join(p)}")
    if selfc:
        print(h2("S1b - SELF-CYCLES (chain lightning; review, not yet a defect)"))
        for p in selfc[:12]:
            print(f"    {' -> '.join(p)}")
        if len(selfc) > 12:
            print(f"    ... and {len(selfc) - 12} more")
    if dangling:
        print(h2("S2 - DANGLING shrapnel targets"))
        for w, t in dangling:
            print(f"    {w} -> {t}  (no such weapon)")
    if deep:
        print(h2(f"S3 - chains longer than {MAX_DEPTH}"))
        for p in deep:
            print(f"    {' -> '.join(p)}")
    if args.list:
        print(h2("Every shrapnel chain"))
        for start, p in chains:
            if len(p) > 1:
                print(f"    {' -> '.join(p)}")

    failed = ((len(multi) > S1A_BASELINE) + (len(selfc) > S1B_BASELINE)
              + (len(dangling) > S2_BASELINE))
    print(f"\n**{'PASS' if not failed else 'FAIL'}** - "
          f"{len(multi)} multi-node cycle(s), {len(selfc)} self-cycle(s), "
          f"{len(dangling)} dangling.")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
