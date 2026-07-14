#!/usr/bin/env python3
"""audit_min_range.py — DESIGN.md minimum-range detector.

Every weapon that declares a MinRange must satisfy:

    MinRange = round(Range / 5) rounded to the nearest multiple of 5.

Equivalent calculation: expected = round(Range / 25.0) * 5.
"""

from __future__ import annotations

import re
import sys

from cameo_model import Model
from report import h1, h2, table


_NUM_RE = re.compile(r"^-?\d+")


def parse_number(value: str | None) -> int | None:
    if not value:
        return None
    # Cameo uses plain wdist integers; ignore c-notation suffixes.
    m = _NUM_RE.match(value.strip())
    if not m:
        return None
    return int(m.group())


def expected_min_range(range_val: int) -> int:
    return round(range_val / 25.0) * 5


def main() -> int:
    m = Model()
    rs = m.rs

    violations: list[list[str]] = []

    for name, node in rs.weapons.items():
        resolved = rs.resolve_weapon(name)
        if resolved is None:
            continue
        range_val = parse_number(resolved.get("Range"))
        min_val = parse_number(resolved.get("MinRange"))
        if range_val is None or min_val is None:
            continue
        expected = expected_min_range(range_val)
        if min_val != expected:
            violations.append([name, str(range_val), str(min_val), str(expected)])

    print(h1("Minimum range audit"))

    if violations:
        print(h2("Weapons with MinRange != round(Range/5) to nearest step of 5"))
        print(table(["Weapon", "Range", "MinRange", "Expected MinRange"], violations))
        return 1

    print("All weapon minimum ranges are consistent with Range/5.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
