#!/usr/bin/env python3
"""audit_ifv_conditions.py — the passenger-conditioned weapon swap (IFV) audit.

    python tools/audit/audit_ifv_conditions.py

Maintainer 2026-08-29: *"Those things need their own separate audit since they are
so complicated and fucked up."*

An IFV-family vehicle changes weapon based on which infantry rides it. The wiring
is a condition handshake with no schema behind it: an infantry actor grants
`ifv-<type>`, and the vehicle carries one `Armament` per type gated on that
condition. `ra2_allies_ifv` has **42 armaments**, every one condition-gated, none
unconditional.

Nothing checks that handshake, and four things can silently break it:

  F1 UNREACHABLE ARMAMENT — the vehicle gates a weapon on `ifv-x` that NO actor
     grants. Dead content: the weapon can never fire.

  F2 ORPHAN PASSENGER — an actor grants `ifv-x` that NO vehicle consumes. The
     infantry rides, nothing switches, and the player silently gets the default
     weapon instead of the one the unit's whole identity promises.

  F3 INCOMPLETE DEFAULT GUARD — the fallback armament fires when NO specialist
     condition holds, expressed as `!ifv-a && !ifv-b && ...` enumerating every
     other type BY HAND. Miss one and the default fires ALONGSIDE the specialist,
     so the IFV shoots twice. Already inconsistent in the tree: one guard lists 15
     conditions, another 17. This list must be complete on every guard, and adding
     a new `ifv-` type means touching every one of them.

  F4 DIVERGENT VARIANTS — the `_hmg` / `_mg` / `_missile` / `_chrono` variants are
     the same vehicle in another state, so their armament sets should match. Where
     they differ, a player gets a different weapon set depending on how the IFV
     was produced.

⚠ These conditions are hyphenated (`ifv-mg`), against DESIGN section 9's
underscore-only rule. They are OUR names, not engine ones, so they are a rename
candidate — but renaming them touches every guard list above, so it is not a
drive-by. Reported, not fixed.

EXIT CODE: 1 on F1, F3 or F4. F2 is reported only — an unconsumed grant may be
deliberate for an infantry no IFV is meant to carry.
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))

from cameo_model import Model  # noqa: E402

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

TOKEN = re.compile(r"\bifv-[A-Za-z0-9_.-]+")
VARIANT = re.compile(r"_(hmg|mg|missile|chrono|repair|empty|elite)$")


def granted(rs):
    """{condition: [actor]} for every actor granting an ifv-* condition."""
    out = collections.defaultdict(list)
    for name in rs.actors:
        if name.startswith(("^", "_")):
            continue
        node = rs.resolve(name)
        if node is None:
            continue
        for child in node.children:
            for key in ("Condition", "PassengerCondition"):
                val = child.get(key)
                if val and TOKEN.fullmatch(val.strip()):
                    out[val.strip()].append(name)
    return out


def carriers(rs):
    """{vehicle: {condition: weapon}} plus each vehicle's default-guard lists."""
    vehicles, guards = {}, collections.defaultdict(list)
    for name in rs.actors:
        if name.startswith(("^", "_")):
            continue
        node = rs.resolve(name)
        if node is None:
            continue
        wanted, seen_any = {}, False
        for child in node.children:
            if child.key.split("@")[0] != "Armament":
                continue
            cond = (child.get("RequiresCondition") or "").strip()
            toks = TOKEN.findall(cond)
            if not toks:
                continue
            seen_any = True
            weapon = (child.get("Weapon") or "").strip()
            # The DEFAULT armament is the one with no POSITIVE ifv token: it
            # fires when no specialist condition holds. Counting `!` instead
            # misreads `ifv-miss && !x` (a specialist with one exclusion) as a
            # guard and then reports it as negating 1 of 25.
            positive = [t for t in toks
                        if re.search(r"(?<!!)\s*\b" + re.escape(t), cond)
                        and not re.search(r"!\s*" + re.escape(t) + r"\b", cond)]
            if not positive:
                guards[name].append((child.key, set(toks)))
            else:
                for t in positive:
                    wanted.setdefault(t, weapon)
        if seen_any:
            vehicles[name] = wanted
    return vehicles, guards


def main():
    ap = argparse.ArgumentParser()
    args = ap.parse_args()
    rs = Model().rs

    grants = granted(rs)
    vehicles, guards = carriers(rs)
    consumed = {t for w in vehicles.values() for t in w}
    all_tokens = consumed | set(grants)

    print("# IFV / passenger-conditioned weapon audit\n")
    print(f"vehicles that swap weapons on a passenger : {len(vehicles)}")
    print(f"distinct `ifv-*` conditions                : {len(all_tokens)}")
    print(f"conditions granted by some actor           : {len(grants)}")
    print(f"conditions consumed by some vehicle        : {len(consumed)}")

    failures = 0

    unreachable = sorted(consumed - set(grants))
    print(f"\n\n## F1 — unreachable armaments ({len(unreachable)})\n")
    if unreachable:
        failures += len(unreachable)
        print("Gated on a condition no actor grants; the weapon can never fire.\n")
        for tok in unreachable:
            who = [v for v, w in vehicles.items() if tok in w]
            print(f"  `{tok}` — gated by {len(who)} vehicle(s), e.g. "
                  f"`{who[0]}` -> `{vehicles[who[0]][tok]}`")
    else:
        print("_clean_")

    orphan = sorted(set(grants) - consumed)
    print(f"\n\n## F2 — passenger grants nothing consumes ({len(orphan)})\n")
    print("Reported only: the infantry rides and the player silently gets the "
          "default weapon instead of the one its identity promises.\n")
    for tok in orphan:
        print(f"  `{tok}` — granted by {', '.join(sorted(grants[tok])[:3])}"
              f"{' ...' if len(grants[tok]) > 3 else ''}")
    if not orphan:
        print("_clean_")

    print(f"\n\n## F3 — incomplete default guards\n")
    print("A default armament must negate EVERY other condition its vehicle "
          "gates on, or it fires alongside the specialist and the IFV shoots "
          "twice.\n")
    bad = 0
    for vehicle, entries in sorted(guards.items()):
        specialists = set(vehicles.get(vehicle) or {})
        for key, listed in entries:
            missing = specialists - listed
            if missing:
                bad += 1
                failures += 1
                print(f"* `{vehicle}` `{key}` negates {len(listed)} conditions, "
                      f"but {len(missing)} of the {len(specialists)} this vehicle "
                      f"actually gates on are NOT among them — "
                      f"{', '.join('`%s`' % m for m in sorted(missing))}")
    if not bad:
        print("_clean_")

    print(f"\n\n## F4 — variants of one vehicle with different armament sets\n")
    families = collections.defaultdict(list)
    for vehicle in vehicles:
        families[VARIANT.sub("", vehicle)].append(vehicle)
    divergent = 0
    for base, members in sorted(families.items()):
        if len(members) < 2:
            continue
        sets = {v: frozenset(vehicles[v]) for v in members}
        if len(set(sets.values())) > 1:
            divergent += 1
            failures += 1
            union = set().union(*sets.values())
            print(f"* `{base}` — {len(members)} variants disagree:")
            for v in sorted(members):
                gap = union - sets[v]
                print(f"    `{v}`: {len(sets[v])} armaments"
                      + (f", missing {', '.join(sorted(gap))}" if gap else ""))
    if not divergent:
        print("_clean_")

    if failures:
        print(f"\n\n**FAIL** — {failures} findings.")
        return 1
    print("\n\n**PASS**")
    return 0


if __name__ == "__main__":
    sys.exit(main())
