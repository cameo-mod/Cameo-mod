#!/usr/bin/env python3
"""audit_upgrade_coverage.py — B4 detector (roster-wide upgrade coverage gaps).

For every upgrade tagged roster_wide / infantry / vehicles / aircraft in
docs/design/upgrades_intent.yaml: diff the owning faction's combat roster of
that type against the actors actually consuming one of the upgrade's granted
conditions; print the uncovered set.
"""

from __future__ import annotations

import re
import sys

from audit_upgrades import load_intent
from cameo_model import Model
from report import h1, h2, table

_ident = re.compile(r"[A-Za-z0-9_.\-]+")
COMBAT_TYPES = {"inf", "veh", "air", "nav"}
TYPE_FILTER = {"roster_wide": COMBAT_TYPES, "infantry": {"inf"},
               "vehicles": {"veh"}, "aircraft": {"air"}}


def main() -> int:
    m = Model()
    rs = m.rs
    intent = load_intent(m.root)

    rows = []
    total_uncovered = 0
    for uname, entry in sorted(intent.items()):
        coverage = (entry.get("coverage") or "").strip()
        types = TYPE_FILTER.get(coverage)
        if types is None:
            continue
        faction = (entry.get("faction") or "").strip()
        if not faction:
            continue

        # tokens the upgrade grants
        res = rs.resolve(uname)
        if res is None:
            rows.append([uname, faction, coverage, "UPGRADE ACTOR MISSING", ""])
            continue
        toks = {uname}
        for c in res.children_named("ProvidesPrerequisite"):
            toks.add((c.get("Prerequisite") or uname).lower())
        for c in res.children_named("ProvidesTeamProxyActor"):
            proxy = c.get("Actor")
            if proxy:
                pres = rs.resolve(proxy)
                if pres is not None:
                    for pc in pres.children_named("ProvidesPrerequisite"):
                        toks.add((pc.get("Prerequisite") or proxy).lower())

        # conditions those tokens grant, per consuming actor
        def consumes(actor_resolved) -> bool:
            for c in actor_resolved.children:
                if c.key.startswith("GrantConditionOnPrerequisite"):
                    prereqs = {t.strip().lstrip("~!").lower()
                               for t in (c.get("Prerequisites") or "").split(",")}
                    if prereqs & toks:
                        return True
            return False

        roster = m.buildable_roster(faction)
        target, covered = [], []
        for lname in sorted(roster):
            ares = rs.resolve(lname)
            if ares is None:
                continue
            # dummy/camera actors without Health are not combat roster
            if ares.child("Health") is None or lname.startswith("camera."):
                continue
            if m.unit_type(lname) not in types:
                continue
            target.append(lname)
            if consumes(ares):
                covered.append(lname)
        uncovered = sorted(set(target) - set(covered))
        total_uncovered += len(uncovered)
        rows.append([uname, faction, coverage,
                     f"{len(covered)}/{len(target)}",
                     ", ".join(uncovered) if uncovered else "—"])

    print(h1("audit_upgrade_coverage — roster-wide upgrade gaps (B4)"))
    print(f"Coverage-tagged upgrades checked: **{len(rows)}** — "
          f"uncovered unit slots: **{total_uncovered}**\n")
    print(h2("Coverage by upgrade"))
    print(table(["upgrade", "faction", "declared coverage", "covered", "uncovered actors"],
                rows))
    print("\n_Note: 'covered' means the actor carries a "
          "GrantConditionOnPrerequisite hook for the upgrade. Upgrades applied "
          "globally through a shared decoration/rank template count as covered "
          "because the hook is inherited into the resolved actor._\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
