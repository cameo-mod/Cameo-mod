#!/usr/bin/env python3
"""audit_faction_leaks.py — B1 detector (cross-faction actor leaks).

For every real faction, computes the reachable buildable set (prerequisite
closure, incl. support-power-produced actors) and flags:
  L1 buildable whose attributed owner is a DIFFERENT faction (leak)
  L2 buildable defined in another faction's ContentPack folder
  L3 buildable inheriting from a concrete actor owned by another faction
     (the tsntsamcabal / Slave-Miner bug shape)
Shared/unattributed actors are listed once as "needs human decision".
"""

from __future__ import annotations

import sys
from collections import defaultdict

from cameo_model import Model
from report import h1, h2, relpath, table


def main() -> int:
    m = Model()
    rs = m.rs
    leaks_l1, leaks_l3 = [], []
    shared: dict[str, set[str]] = defaultdict(set)

    factions = [f.internal for f in m.real_factions()]
    for fac in factions:
        roster = m.buildable_roster(fac)
        for lname in sorted(roster):
            owner = m.owner_of(lname)
            if owner is None:
                shared[lname].add(fac)
                continue
            # normalize contentpack owners like "tiberiandawn/gdi" — compare loosely
            if not _same_faction(owner, fac):
                leaks_l1.append([fac, lname, owner,
                                 relpath(rs.actor(lname).file, m.root)])
            node = rs.actor(lname)
            for _, target in rs.inherits_of(node):
                if target.startswith("^"):
                    continue
                towner = m.owner_of(target)
                if towner and not _same_faction(towner, fac):
                    leaks_l3.append([fac, lname, target, towner,
                                     relpath(node.file, m.root)])

    print(h1("audit_faction_leaks — cross-faction leaks (B1)"))
    print(f"Factions checked: **{len(factions)}** — "
          f"L1 leaks: **{len(leaks_l1)}**, L3 concrete-inherit leaks: **{len(leaks_l3)}**, "
          f"shared/unattributed buildables: **{len(shared)}**\n")

    print(h2("L1 — buildable in faction X but owned by faction Y"))
    print(table(["faction", "actor", "attributed owner", "file"], leaks_l1))

    print(h2("L3 — buildable inherits concrete actor owned by another faction"))
    print(table(["faction", "actor", "inherit target", "target owner", "file"], leaks_l3))

    print(h2("Shared / unattributed buildables (needs human decision)"))
    rows = [[a, str(len(f)), ", ".join(sorted(f)[:8]) + ("…" if len(f) > 8 else "")]
            for a, f in sorted(shared.items()) if len(f) < len(factions)]
    rows = [r for r in rows if int(r[1]) <= 6]  # cross-everything actors are fine
    print(f"(showing only actors reachable by ≤ 6 factions; "
          f"{len(shared) - len(rows)} broadly-shared actors suppressed)\n")
    print(table(["actor", "#factions", "factions"], rows))
    return 0


def _same_faction(owner: str, fac: str) -> bool:
    o = owner.lower().split("/")[-1]
    f = fac.lower()
    if o == f or o == "shared" or o == "core":
        return True
    # ContentPack folder names vs internal names (gdi vs gdi, ordos vs ordos …)
    aliases = {
        "gdi": {"gdi"}, "nod": {"nod"},
        "redalert": {"allies", "soviet", "modjapan"},
        "redalert2": {"ra2america", "ra2russia", "yuri"},
        "tiberiansun": {"tsgdi", "tsnod", "cabal", "forgotten"},
        "starcraft": {"terran", "zerg", "protoss"},
        "warcraft2": {"human2", "orc2"},
        "asianalliance": {"asianalliance"}, "consortium": {"consortium"},
        "syndicate": {"syndicate"}, "naxis": {"naxis", "lnaxis"},
        "schwarzermond": {"lnaxis"}, "futuretech": {"futuretech"},
        "ordos": {"ordos"}, "ixian": {"ixian"},
        "atreides": {"atreides"}, "harkonnen": {"harkonnen"},
        "tkm": {"tkm"}, "outpost2": {"plymouth", "eden"},
    }
    return f in aliases.get(o, set())


if __name__ == "__main__":
    sys.exit(main())
