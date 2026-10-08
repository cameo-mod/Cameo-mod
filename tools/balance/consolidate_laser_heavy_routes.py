#!/usr/bin/env python3
"""Collapse the reviewed Laser_Heavy route-identical remainder cohort.

The two Schwarzer Mond roots carry a second Laser_Heavy-identical ground
warhead through a compatibility template.  Ground descendants need the sum;
air descendants need only the Air-valid canonical slice.  The Tick Tank laser
is ground-only at the weapon contract, so its two ground slices also sum.

This converter fails closed on recursive closures, resolved route contracts,
profile identity, percentage arithmetic, and hashes of all non-selected
behavior before it edits source YAML.
"""
from __future__ import annotations

import argparse
import collections
import hashlib
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))

from audit_three_way_split import main_warheads  # noqa: E402
from consolidate_compatibility_profiles import flat_nodes  # noqa: E402
from consolidate_final_safe_cohorts import percentage_scale  # noqa: E402
from consolidate_reviewed_weapon_roots import block_bounds  # noqa: E402
from miniyaml import Ruleset  # noqa: E402


SELECTED_KEYS = {"Laser_Heavy", "LaserHeavyGroundRemainder"}
BEETLE_GROUND = {
    "NaxiBeetleLaser_elite", "Lunar_AmplifiedBeetleLaser",
    "Lunar_YellowBeetleLaser",
}
BEETLE_AIR = {
    "NaxiBeetleLaser_AA_elite", "Lunar_AmplifiedBeetleLaser_AA",
    "Lunar_YellowBeetleLaser_AA",
}
TANK_GROUND = {
    "NaxiTank2Laser", "Lunar_AmplifiedTank2Laser",
    "Lunar_YellowTank2Laser",
}
TANK_AIR = {
    "NaxiTank2Laser_AA", "Lunar_AmplifiedTank2Laser_AA",
    "Lunar_YellowTank2Laser_AA",
}
GROUND = BEETLE_GROUND | TANK_GROUND | {"TSLaser25mmDep"}
AIR = BEETLE_AIR | TANK_AIR
SELECTED = GROUND | AIR

EXPECTED_CLOSURES = {
    "NaxiBeetleLaser_elite": (BEETLE_GROUND | BEETLE_AIR)
    - {"NaxiBeetleLaser_elite"},
    "NaxiTank2Laser": (TANK_GROUND | TANK_AIR) - {"NaxiTank2Laser"},
    "TSLaser25mmDep": set(),
}

# Hashes exclude only the two selected ordinary mains.  They therefore pin
# projectile/effect/cadence/status metadata and every percentage companion on
# every definition in the recursive closures.
PRESERVED_HASHES = {
    "NaxiBeetleLaser_elite": "5db64889621aab61afe0c0de07a740926ad5c9d9ef9ae28acc8011a3b52958bb",
    "NaxiBeetleLaser_AA_elite": "76b8f694f6ea5d3f3fcfa3d1ab632ebdf384e8b97fc34ac0c3ac506291d32b3d",
    "Lunar_AmplifiedBeetleLaser": "9478ad921dec0d9397ebc0028eea7dd3e03ad58c65b399fe4bb87ef7b7a4fa4d",
    "Lunar_AmplifiedBeetleLaser_AA": "a4010f9ac967edccbade626299d2e0ad0e5872c26de7662506d00f0c7610c664",
    "Lunar_YellowBeetleLaser": "e73e344d876ea0bc68359202b13636d53e22126c1a83abd28b280dc6b7460dae",
    "Lunar_YellowBeetleLaser_AA": "83c126d793bfbee18bd1bd038814bb599248c658be9d6bbfda1a94bc74644029",
    "NaxiTank2Laser": "0b745ff518314ee3164788a9a0df06a204ee263ee21763cf5e4fc2924f78f61d",
    "NaxiTank2Laser_AA": "511566c9f1673bb3a52ebfbb5c5e87d486a5ac70037bc8544b7d79548116cad0",
    "Lunar_AmplifiedTank2Laser": "36783f9d91fecaf9f9e9ccdacc458d2c063dc242e60b15513127b75c02eedeec",
    "Lunar_AmplifiedTank2Laser_AA": "ed7727df7cd7acf17a4d555a8518a4ff44859cd2db924dd41b255e5af17f0aad",
    "Lunar_YellowTank2Laser": "a82c473e6906b854f448d2befb91ea8b3c8f0e69212c95739a36e7014ea5a98c",
    "Lunar_YellowTank2Laser_AA": "0887726aebf57b2c37e6cc52c1b41161c8a46a8f3ab0e801ab6c375d916892dc",
    "TSLaser25mmDep": "19daee6e82f50631c78ade8baefd8dfeec2b2aa4344c8fe09a4976e193cd586d",
}


def descendants(rs: Ruleset, root: str) -> set[str]:
    direct: dict[str, set[str]] = collections.defaultdict(set)
    for name, node in rs.weapons.items():
        for _, parent in rs.inherits_of(node):
            if parent in rs.weapons:
                direct[parent].add(name)
    seen: set[str] = set()
    stack = list(direct[root])
    while stack:
        name = stack.pop()
        if name in seen:
            continue
        seen.add(name)
        stack.extend(direct[name])
    return {name for name in seen if not name.startswith("^")}


def node_payload(node):
    return [node.key, node.value, [node_payload(child) for child in node.children]]


def resolved_hash(rs: Ruleset, name: str) -> str:
    resolved = rs.resolve_weapon(name)
    if resolved is None:
        raise RuntimeError(f"{name}: missing resolved weapon")
    excluded = {f"Warhead@{key}" for key in SELECTED_KEYS}
    payload = [node_payload(child) for child in resolved.children
               if child.key not in excluded]
    raw = json.dumps(payload, separators=(",", ":")).encode()
    return hashlib.sha256(raw).hexdigest()


def profile_signature(node) -> tuple:
    """Identity excluding the three fields this route fold may author."""
    # PercentageSpread/PercentageVersus are inert here because PercentageScale
    # is pinned to zero on both sides of the fold. They remain on the canonical
    # destination but cannot create a folded hit.
    ignored = {
        "Damage", "PercentageScale", "PercentageSpread", "PercentageVersus",
        "ValidTargets",
    }
    children = [
        node_payload(child) for child in node.children if child.key not in ignored
    ]
    return (
        node.value,
        tuple(sorted(children, key=lambda item: json.dumps(item))),
    )


def route(node) -> str:
    return str(node.get("ValidTargets") or "")


def damage(node) -> int:
    return int(str(node.get("Damage") or 0))


def inspect(rs: Ruleset) -> bool:
    for root, expected in EXPECTED_CLOSURES.items():
        actual = descendants(rs, root)
        if actual != expected:
            raise RuntimeError(
                f"{root}: descendant closure changed; expected {sorted(expected)}, "
                f"found {sorted(actual)}")

    states = set()
    for name in sorted(SELECTED):
        resolved = rs.resolve_weapon(name)
        mains = set(main_warheads(resolved))
        nodes = flat_nodes(resolved)
        if mains == SELECTED_KEYS:
            states.add(False)
            if set(nodes) < SELECTED_KEYS:
                raise RuntimeError(f"{name}: selected main is no longer flat damage")
            canonical = nodes["Laser_Heavy"]
            remainder = nodes["LaserHeavyGroundRemainder"]
            if profile_signature(canonical) != profile_signature(remainder):
                raise RuntimeError(f"{name}: heavy-laser profiles no longer match")
            expected_damage = 2000 if name == "TSLaser25mmDep" else 4000
            if damage(canonical) != expected_damage or damage(remainder) != expected_damage:
                raise RuntimeError(f"{name}: selected slice damage changed")
            expected_route = "Air" if name in AIR else (
                "Ground, Water, Air" if name == "TSLaser25mmDep" else "Ground, Water")
            if route(canonical) != expected_route or route(remainder) != "Ground, Water":
                raise RuntimeError(f"{name}: selected route contract changed")
            if percentage_scale(resolved, SELECTED_KEYS, expected_damage * 2) != 0:
                raise RuntimeError(f"{name}: selected percentage arithmetic changed")
        elif mains == {"Laser_Heavy"}:
            states.add(True)
            canonical = nodes.get("Laser_Heavy")
            expected_damage = (
                4600 if name in AIR or name == "TSLaser25mmDep"
                else (8600 if name.startswith("Lunar_") else 8000)
            )
            expected_route = "Air" if name in AIR else "Ground, Water"
            if canonical is None or damage(canonical) != expected_damage:
                raise RuntimeError(f"{name}: applied destination damage changed")
            if route(canonical) != expected_route:
                raise RuntimeError(f"{name}: applied destination route changed")
            if int(str(canonical.get("PercentageScale") or 0)) != 0:
                raise RuntimeError(f"{name}: applied PercentageScale changed")
        else:
            raise RuntimeError(
                f"{name}: expected {sorted(SELECTED_KEYS)} or Laser_Heavy; "
                f"found {sorted(mains)}")

        if resolved_hash(rs, name) != PRESERVED_HASHES[name]:
            raise RuntimeError(f"{name}: non-selected behavior hash changed")

    if len(states) != 1:
        raise RuntimeError("partial Laser_Heavy route consolidation detected")
    return states == {True}


def lines_for(changed: dict[pathlib.Path, list[str]], rs: Ruleset, name: str):
    node = rs.weapon(name)
    if node is None:
        raise RuntimeError(f"{name}: source weapon missing")
    path = pathlib.Path(node.file)
    return path, changed.setdefault(
        path, path.read_text(encoding="utf-8-sig").splitlines(True))


def remove_line(lines: list[str], name: str, exact: str) -> None:
    start, end = block_bounds(lines, name)
    indexes = [i for i in range(start + 1, end)
               if lines[i].rstrip("\r\n") == exact]
    if len(indexes) != 1:
        raise RuntimeError(f"{name}: expected one {exact!r}")
    del lines[indexes[0]]


def remove_node(lines: list[str], name: str, key: str) -> None:
    start, end = block_bounds(lines, name)
    pattern = re.compile(r"^\t" + re.escape(key) + r":")
    indexes = [i for i in range(start + 1, end)
               if pattern.match(lines[i].rstrip("\r\n"))]
    if len(indexes) != 1:
        raise RuntimeError(f"{name}: expected one {key} node")
    first = indexes[0]
    last = end
    for i in range(first + 1, end):
        if lines[i].startswith("\t") and not lines[i].startswith("\t\t") \
                and lines[i].strip():
            last = i
            break
    del lines[first:last]


def set_field(lines: list[str], name: str, node_key: str, field: str,
              value: str | int) -> None:
    start, end = block_bounds(lines, name)
    marker = re.compile(r"^\tWarhead@" + re.escape(node_key) + r":")
    indexes = [i for i in range(start + 1, end)
               if marker.match(lines[i].rstrip("\r\n"))]
    if len(indexes) != 1:
        raise RuntimeError(f"{name}: expected one {node_key} override")
    node_start = indexes[0]
    node_end = end
    for i in range(node_start + 1, end):
        if lines[i].startswith("\t") and not lines[i].startswith("\t\t") \
                and lines[i].strip():
            node_end = i
            break
    rows = [i for i in range(node_start + 1, node_end)
            if lines[i].lstrip().startswith(f"{field}:")]
    if len(rows) > 1:
        raise RuntimeError(f"{name}: duplicate {field}")
    replacement = f"\t\t{field}: {value}\n"
    if rows:
        lines[rows[0]] = replacement
    else:
        lines.insert(node_end, replacement)


def apply_changes(rs: Ruleset) -> None:
    changed: dict[pathlib.Path, list[str]] = {}
    for root in ("NaxiBeetleLaser_elite", "NaxiTank2Laser"):
        _path, lines = lines_for(changed, rs, root)
        remove_line(lines, root,
                    "\tInherits@groundremainder: ^LaserHeavyGroundRemainderCompatibility")
        set_field(lines, root, "Laser_Heavy", "Damage", 8000)

    for child in ("NaxiBeetleLaser_AA_elite", "NaxiTank2Laser_AA"):
        _path, lines = lines_for(changed, rs, child)
        set_field(lines, child, "Laser_Heavy", "Damage", 4000)

    _path, lines = lines_for(changed, rs, "TSLaser25mmDep")
    remove_node(lines, "TSLaser25mmDep", "Warhead@LaserHeavyGroundRemainder")
    set_field(lines, "TSLaser25mmDep", "Laser_Heavy", "Damage", 4000)
    set_field(lines, "TSLaser25mmDep", "Laser_Heavy", "ValidTargets", "Ground, Water")

    for path, lines in changed.items():
        path.write_text("".join(lines), encoding="utf-8", newline="\n")


def validate_result() -> None:
    if not inspect(Ruleset(ROOT)):
        raise RuntimeError("Laser_Heavy route cohort remains unconsolidated")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    rules = Ruleset(ROOT)
    already = inspect(rules)
    if already:
        print(f"Already consolidated {len(SELECTED)} concrete definitions")
        return 0
    print(f"3 roots; {len(SELECTED)} concrete definitions")
    if not args.apply:
        print("Dry run: closures, routes, profiles, percentages, and hashes pass")
        return 0
    apply_changes(rules)
    validate_result()
    print("Applied and validated 2 weapon files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
