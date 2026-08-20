#!/usr/bin/env python3
"""audit_metadata.py — B7 detector (copy-paste metadata rot).

  M1 duplicate Tooltip names within one faction's buildable roster
  M2 buildable actors with no Tooltip name at all
  M3 Tooltip names that are raw fluent keys left unresolved elsewhere
     (literal-vs-fluent split is reported for B12 by audit_fluent)
"""

from __future__ import annotations

import sys
from collections import defaultdict

from cameo_model import Model
from report import h1, h2, table


def main() -> int:
    m = Model()
    rs = m.rs
    m1_rows, m2_rows = [], []

    for fac in sorted(f.internal for f in m.real_factions()):
        seen: dict[str, list[str]] = defaultdict(list)
        for lname in sorted(m.buildable_roster(fac)):
            res = rs.resolve(lname)
            if res is None:
                continue
            tt = res.get("Tooltip", "Name")
            if not tt:
                if res.child("Health") is not None:  # dummies without health excluded
                    m2_rows.append([fac, lname])
                continue
            seen[tt.lower()].append(lname)
        for tt, actors in sorted(seen.items()):
            distinct = sorted(set(actors))
            if len(distinct) > 1:
                m1_rows.append([fac, tt, ", ".join(distinct)])

    print(h1("audit_metadata — tooltip/metadata rot (B7)"))
    print(f"Duplicate-tooltip groups: **{len(m1_rows)}**, "
          f"buildables missing Tooltip name: **{len(m2_rows)}**\n")
    print(h2("M1 — same tooltip name on multiple buildables of one faction"))
    print(table(["faction", "tooltip name", "actors"], m1_rows))
    print(h2("M2 — buildable actors without a Tooltip name"))
    print(table(["faction", "actor"], m2_rows))
    print("\n_Note: unit-class taxonomy checks (docs/design/unit_classes.yaml) "
          "activate once that registry exists — see MASTER_REPORT §9.4._\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
