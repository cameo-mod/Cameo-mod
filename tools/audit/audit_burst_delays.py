#!/usr/bin/env python3
"""audit_burst_delays.py — DESIGN.md burst-weapon timing rule.

For any weapon with Burst > 1 and BurstDelays > 0:
    ReloadDelay must be >= Burst * BurstDelays

This guarantees at least one full BurstDelay of idle time between bursts
and prevents overlapping fire cycles.
"""

from __future__ import annotations

import re
import sys

from cameo_model import Model
from report import h1, h2, table


def parse_num(value: str | None) -> int | None:
    if not value:
        return None
    m = re.match(r"-?\d+", str(value).strip())
    return int(m.group()) if m else None


def main() -> int:
    m = Model()
    rs = m.rs
    violations: list[list[str]] = []

    for name in sorted(rs.weapons):
        w = rs.resolve_weapon(name)
        if w is None:
            continue
        burst = parse_num(w.get("Burst"))
        bd = parse_num(w.get("BurstDelays"))
        rd = parse_num(w.get("ReloadDelay"))
        if not burst or burst <= 1 or not bd or bd <= 0 or rd is None:
            continue
        if rd < burst * bd:
            violations.append([
                name, str(rd), str(burst), str(bd), str(burst * bd)
            ])

    print(h1("Burst weapon delay audit"))
    if violations:
        print(h2("ReloadDelay < Burst * BurstDelays"))
        print(table(
            ["Weapon", "ReloadDelay", "Burst", "BurstDelays", "Minimum"],
            violations,
        ))
        return 1

    print("All burst weapons satisfy ReloadDelay >= Burst * BurstDelays.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
