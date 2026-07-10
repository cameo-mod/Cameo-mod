#!/usr/bin/env python3
"""audit_fluent.py — B12 detector (localization drift).

  F1 fluent references in rules (values like `actor-x.name`,
     `upgrade-y.description`) that no loaded .ftl file defines — these render
     as raw keys in-game (BLOCKING-ish, player-visible)
  F2 `actor-*` / `upgrade-*` fluent messages whose actor no longer exists
     (orphaned keys)
  F3 per-faction literal-vs-fluent Tooltip coverage (info; feeds MATRIX.md)
"""

from __future__ import annotations

import re
import sys
from collections import defaultdict

from cameo_model import Model
from miniyaml import load_fluent_keys
from report import h1, h2, table

_fluent_ref = re.compile(r"^[a-z0-9][a-z0-9_-]*(\.[a-z0-9_-]+)+$")
FLUENT_FIELDS = ("Name", "Description", "ReadyTextNotification", "Label")


def looks_like_fluent(value: str) -> bool:
    return bool(_fluent_ref.fullmatch(value.strip())) and "-" in value


def main() -> int:
    m = Model()
    rs = m.rs
    keys = load_fluent_keys(m.rs.manifest.fluent)

    f1 = []
    literal_by_faction: dict[str, list[int]] = defaultdict(lambda: [0, 0])
    for name in sorted(rs.actors):
        if name.startswith("^"):
            continue
        res = rs.resolve(name)
        if res is None:
            continue
        for trait in res.children:
            for f in FLUENT_FIELDS:
                v = trait.get(f)
                if not v:
                    continue
                if looks_like_fluent(v) and v not in keys:
                    f1.append([name, f"{trait.key}.{f}", v])

    for fac in sorted(f.internal for f in m.real_factions()):
        for lname in m.buildable_roster(fac):
            res = rs.resolve(lname)
            if res is None:
                continue
            tt = res.get("Tooltip", "Name")
            if not tt:
                continue
            slot = literal_by_faction[fac]
            slot[1] += 1
            if looks_like_fluent(tt):
                slot[0] += 1

    actor_keys = [k for k in keys if k.startswith("actor-") and "." not in k]
    f2 = []
    for k in sorted(actor_keys):
        actor_id = k[len("actor-"):]
        if rs.actor(actor_id) is None:
            f2.append([k])

    print(h1("audit_fluent — localization drift (B12)"))
    print(f"Fluent messages loaded: **{len(keys)}** — unresolved fluent refs in "
          f"rules: **{len(f1)}**, orphaned actor-* messages: **{len(f2)}**\n")
    print(h2("F1 — rules reference fluent keys that don't exist (shows raw key in-game)"))
    print(table(["actor", "field", "missing key"], f1))
    print(h2("F2 — fluent actor-* messages for actors that no longer exist"))
    print(table(["orphaned message id"], f2))
    print(h2("F3 — buildable-roster fluent Name coverage per faction"))
    rows = [[fac, f"{v[0]}/{v[1]}", f"{(100*v[0]//v[1]) if v[1] else 0}%"]
            for fac, v in sorted(literal_by_faction.items())]
    print(table(["faction", "fluent/total tooltips", "coverage"], rows))
    return 1 if f1 else 0


if __name__ == "__main__":
    sys.exit(main())
