#!/usr/bin/env python3
"""route_parity_floor.py — keep a paid weapon route's local Versus rows at parity with its base.

REGREEN R3 (REGREEN_2026-10-06_regressions.md): `ra1_soviets_hammertank_cannon_thermobaric`
carries a materialized local `Versus` on `Warhead@Thermobaric_Heavy` whose Scout/Light/Medium
rows are a *route-parity floor* — the smallest row that keeps paid Damage x Versus at or above
the base route's product:

    floor[armor] = ceil(base_versus[armor] x base_damage / paid_damage)

The block was calibrated when the old composite collapsed (dac4e0ec2, PR #320): the then-current
base ladder was Scout 130 / Light 120 / Medium 114, so the rows were pinned at 98 / 90 / 86.
R16 (8e20af41e, every Versus profile normalized to a geometric mean of 100) retuned the base
`CannonHE_Heavy` ladder to 146 / 132 / 122; the pinned floor never followed, and the paid route
regressed below the base route on the core vehicle armors (98 < 110 etc.). Hand rows do not
track a moving ladder — the floor is DERIVED data, so this tool owns the derivation and the
next retune re-materializes instead of silently regressing.

Desired local row per declared armor:

    max(ceil(base_versus x base_damage / paid_damage), inherited_row)

where `inherited_row` is what the paid warhead resolves for that armor without the local
override (the `^Warhead_<family>` template the warhead inherits). The `max` keeps the pin from
ever dropping below the destination profile if a retune raises it past the floor.

After `--write`, run `derive_versus_columns.py --write` so the materialized GEO_DERIVED rows
that read these armors as parents (FlyingInfantry, ShipLight, …) re-derive from the new floor.

Usage:
    python tools/balance/route_parity_floor.py            # dry run: report, exit 1 on drift
    python tools/balance/route_parity_floor.py --write    # materialize the floors
"""
from __future__ import annotations

import argparse
import math
import pathlib
import re
import sys
from typing import NamedTuple

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))
import miniyaml  # noqa: E402
from derive_versus_columns import as_int_table, read_text  # noqa: E402


class Route(NamedTuple):
    """A paid weapon whose local Versus floor must track its base route."""

    base_weapon: str
    base_warhead: str
    paid_weapon: str
    paid_warhead: str
    armors: tuple[str, ...]


# Upgrade routes whose paid weapon carries a materialized parity floor. Extend only when a
# new paid route needs a pinned floor — the armors are the ones the floor must cover.
ROUTES = (
    Route("ra1_soviets_hammertank_cannon", "Warhead@CannonHE_Heavy",
          "ra1_soviets_hammertank_cannon_thermobaric", "Warhead@Thermobaric_Heavy",
          ("Scout", "Light", "Medium")),
)


def versus_rows(node) -> dict[str, int]:
    """Resolved `Versus` armor rows of a warhead node ({} when absent)."""
    v = node.child("Versus") if node else None
    return as_int_table(v) if v else {}


def floor_rows(rs, route: Route) -> dict[str, int]:
    """The local Versus values the paid route must materialize for `route.armors`."""
    base = rs.resolve_weapon(route.base_weapon).child(route.base_warhead)
    paid = rs.resolve_weapon(route.paid_weapon).child(route.paid_warhead)
    inherited_parent = "^Warhead_" + route.paid_warhead.split("@", 1)[1]
    parent = rs.resolve_weapon(inherited_parent).child(route.paid_warhead)
    base_rows, parent_rows = versus_rows(base), versus_rows(parent)
    base_damage, paid_damage = int(base.get("Damage")), int(paid.get("Damage"))
    return {armor: max(math.ceil(base_rows[armor] * base_damage / paid_damage),
                       parent_rows.get(armor, 0))
            for armor in route.armors}


def plan(rs, route: Route):
    """[(line_index_0based, kind, key, value, ref_line_0based)] edits for one route."""
    source = rs.weapon(route.paid_weapon)
    if source is None:
        raise SystemExit(f"{route.paid_weapon} does not resolve")
    warhead = source.child(route.paid_warhead)
    local = warhead.child("Versus") if warhead else None
    local_rows = {c.key: c for c in local.children} if local else {}
    edits = []
    for armor, value in floor_rows(rs, route).items():
        have = local_rows.get(armor)
        if have is not None:
            if have.value is None or have.value.strip() != str(value):
                edits.append((have.line - 1, "set", armor, value, None))
        else:
            if local is None or not local.children:
                raise SystemExit(
                    f"{route.paid_weapon}/{route.paid_warhead} has no local Versus block to extend")
            edits.append((max(c.line for c in local.children) - 1, "add", armor, value,
                          local.children[-1].line - 1))
    return edits, pathlib.Path(source.file)


def apply(path: pathlib.Path, edits) -> int:
    text, enc, bom = read_text(path)
    crlf = "\r\n" in text
    lines = text.split("\r\n" if crlf else "\n")
    adds: dict[int, list[str]] = {}
    for idx, kind, name, value, ref in edits:
        if kind == "set":
            m = re.match(r"^(\s*)([^:]+):", lines[idx])
            lines[idx] = f"{m.group(1)}{m.group(2)}: {value}"
        else:
            indent = re.match(r"^(\s*)", lines[ref]).group(1)
            adds.setdefault(idx, []).append(f"{indent}{name}: {value}")
    for idx in sorted(adds, reverse=True):
        lines[idx + 1:idx + 1] = adds[idx]
    out = ("\r\n" if crlf else "\n").join(lines)
    data = out.encode("utf-8" if enc == "utf-8-sig" else enc)
    if bom and not data.startswith(b"\xef\xbb\xbf"):
        data = b"\xef\xbb\xbf" + data
    path.write_bytes(data)
    return len(edits)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--write", action="store_true")
    args = ap.parse_args()
    rs = miniyaml.Ruleset(ROOT)
    total = 0
    for route in ROUTES:
        edits, path = plan(rs, route)
        rel = path.relative_to(ROOT).as_posix()
        detail = ", ".join(f"{e[2]}={e[3]}" for e in edits) or "at floor"
        print(f"{route.paid_weapon} ({rel}): {detail}")
        if edits and args.write:
            apply(path, edits)
        total += len(edits)
    print(f"{'written' if args.write else 'pending'}: {total} parity rows")
    return 0 if (args.write or total == 0) else 1


if __name__ == "__main__":
    sys.exit(main())
