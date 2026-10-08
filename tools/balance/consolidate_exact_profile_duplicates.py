#!/usr/bin/env python3
"""Collapse the last mechanically identical ordinary main-damage profiles.

Only full recursive profile duplicates are selected.  Damage and the folded
PercentageScale fields may differ; projectile, effect, percentage, status,
targeting, and descendant behavior are pinned by resolved-tree hashes.
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
from consolidate_compatibility_profiles import flat_nodes, fingerprint  # noqa: E402
from consolidate_final_safe_cohorts import percentage_scale  # noqa: E402
from consolidate_reviewed_weapon_roots import block_bounds, resolved_flat_total  # noqa: E402
from miniyaml import Ruleset  # noqa: E402


RA2120_SELECTED = {
    "RA2120xmm", "RA2120xmm_elite", "RA2120xmm_fire",
    "RA2120xmm_fire_elite", "RA2120xmm_tesla", "RA2120xmm_tesla_elite",
}
RA2120_DESCENDANTS = RA2120_SELECTED - {"RA2120xmm"} | {
    "RA2120xmm_rad", "RA2120xmm_rad_elite",
}
# Live closure after the W7 de-parenting wave: the two AAGunBoat weapons lost
# their weapon-to-weapon edge to RA2FlakTrackGun but remain pinned through
# FLAK_BRANCH_PRESERVED_HASHES and the protected-role assertions.
FLAK_DESCENDANTS = {
    "RA2FlakTrackAAGun", "RA2FlakTrackAAGun_elite", "RA2FlakTrackGun_elite",
}
TESLA_ARMOR = {
    "TeslaArmorDischargeArc", "TeslaArmorDischargeFragment1",
    "TeslaArmorDischargeFragment2",
}

# selected keys, destination key, total, folded PercentageScale
SPECS = {
    **{
        name: (
            {"CannonHE_Heavy", "CannonHE_HeavyFlatCompatibility"},
            "CannonHE_Heavy", 12000, 3325,
        )
        for name in RA2120_SELECTED
    },
    "RA2FlakTrackGun": (
        {"Flak_Medium", "Flak_MediumFlatCompatibility"},
        "Flak_Medium", 8000, 2488,
    ),
    "TSPulseCannon_EMP": (
        {"TeslaWeapon", "TeslaChargedWeapon"},
        "TeslaChargedWeapon", 20000, 0,
    ),
    "TeslaArmorDischargeArc": (
        {"LightMissile", "TeslaWeapon"}, "TeslaWeapon", 24000, 0,
    ),
    "TeslaArmorDischargeFragment1": (
        {"LightMissile", "TeslaWeapon"}, "TeslaWeapon", 12000, 0,
    ),
    "TeslaArmorDischargeFragment2": (
        {"LightMissile", "TeslaWeapon"}, "TeslaWeapon", 8000, 0,
    ),
}

# Hashes exclude only the selected ordinary mains.  They pin every percentage
# companion and every projectile/effect/status/relationship descendant field.
PRESERVED_HASHES = {
    "RA2120xmm": "9aca747d2481825e76a335c6bcaa200908e0849db77005ecd5ac110266f91eda",
    "RA2120xmm_elite": "a5d6eccc30f7149cf8c94acfd8a997bdb8d09fc5bc0f9e30a0932590cb2e2eee",
    "RA2120xmm_fire": "bbbc91f4c0712947fd40c9e7aa569a9e0120d0c1f3900886abbcb7d5151f394f",
    "RA2120xmm_fire_elite": "a96e88666b24619a3b1e44f82d40a056162376ef5cdf9d078dfcce1c1375c34d",
    "RA2120xmm_tesla": "0998a3d8f5347f92e5c7ee1a8e20d015e9e207084f19faa1682c86bba00d7bf2",
    "RA2120xmm_tesla_elite": "9c450162c7f5a805361af33d81fe5c16f32c0418a6940c56b107a9e288921ad4",
    "RA2FlakTrackGun": "6adae7daf20a7040652e87a3a1dd236f7da9c83300a36d97b07387e27a6de983",
    "TSPulseCannon_EMP": "14e7fa57163f8d0dac0d00cbe93c884276c116bd35e6806c998c3266a3438197",
    "TeslaArmorDischargeArc": "548bed3215faaf5c8d43d088f9c802fb6858fe64bc6b804bd4213cb74b2beab2",
    "TeslaArmorDischargeFragment1": "27c67e62918c543f05ae50825fdf6f90f618f8247d0294ed310070717b7911cd",
    "TeslaArmorDischargeFragment2": "1c7196b61858c9bf38aa9687974850b322ca754ccb68c278c58ad33c629db639",
}

# These branches deliberately do not collapse with their parent.  Full resolved
# hashes make the converter restore their routing/profile behavior exactly.
BRANCH_HASHES = {
    "RA2120xmm_rad": "c8df6ab7ef6f39bcba1eb5cdcd05e4e868ece9618048521212bd6adc27138337",
    "RA2120xmm_rad_elite": "f8786f49119270f6945f0fc6c5871b75fc14a749fca15853dd408ee994e59cac",
    "RA2FlakTrackAAGun": "620988f56a51068a1931dbe8df7dd41ed811728ba325d1acaca26c3a3198bc6d",
    "RA2FlakTrackAAGun_elite": "190ce15e42f54a31d35bbc51d1a92b0c7988c8ffc1b35af8e5151ef7d63f1af1",
}

FLAK_BRANCH_PRESERVED_HASHES = {
    "RA2FlakTrackGun_elite": "3733f8194ba951fc012a36d36fc5bb04d8de27a46d88e90232a59274d569c12c",
    "AAGunBoatFlak": "89878bceaab33146e15e29111c81da5585750d2a0c5a56139c86b461e87757d5",
    "AAGunBoatFlak_elite": "5f5d8ecb9f26d48fe860f559a5b56a28238291bfa24d0cf3baa46cffc0369d69",
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


def resolved_hash(rs: Ruleset, name: str, excluded: set[str] | None = None) -> str:
    resolved = rs.resolve_weapon(name)
    if resolved is None:
        raise RuntimeError(f"{name}: missing resolved weapon")
    excluded_keys = {f"Warhead@{key}" for key in (excluded or set())}
    payload = [node_payload(child) for child in resolved.children
               if child.key not in excluded_keys]
    raw = json.dumps(payload, separators=(",", ":")).encode()
    return hashlib.sha256(raw).hexdigest()


def inspect(rs: Ruleset) -> bool:
    if descendants(rs, "RA2120xmm") != RA2120_DESCENDANTS:
        raise RuntimeError("RA2120xmm: descendant closure changed")
    if descendants(rs, "RA2FlakTrackGun") != FLAK_DESCENDANTS:
        raise RuntimeError("RA2FlakTrackGun: descendant closure changed")
    if descendants(rs, "TeslaArmorDischargeArc") != TESLA_ARMOR - {
            "TeslaArmorDischargeArc"}:
        raise RuntimeError("TeslaArmorDischargeArc: descendant closure changed")
    if descendants(rs, "TSPulseCannon_EMP"):
        raise RuntimeError("TSPulseCannon_EMP: unexpected descendants")

    states = set()
    for name, (old_keys, destination, total, scale) in SPECS.items():
        resolved = rs.resolve_weapon(name)
        mains = set(main_warheads(resolved))
        if mains == old_keys:
            states.add(False)
            nodes = flat_nodes(resolved)
            if not old_keys <= set(nodes):
                raise RuntimeError(f"{name}: selected main is not flat damage")
            if len({fingerprint(nodes[key]) for key in old_keys}) != 1:
                raise RuntimeError(f"{name}: selected profiles no longer match")
            if resolved_flat_total(resolved, old_keys) != total:
                raise RuntimeError(f"{name}: selected total changed")
            if percentage_scale(resolved, old_keys, total) != scale:
                raise RuntimeError(f"{name}: folded percentage scale changed")
        elif mains == {destination}:
            states.add(True)
            node = flat_nodes(resolved).get(destination)
            if node is None or int(str(node.get("Damage") or 0)) != total:
                raise RuntimeError(f"{name}: applied destination total changed")
            if scale and int(str(node.get("PercentageScale") or 0)) != scale:
                raise RuntimeError(f"{name}: applied PercentageScale changed")
        else:
            raise RuntimeError(
                f"{name}: expected {sorted(old_keys)} or {destination}; found {sorted(mains)}")

        actual = resolved_hash(rs, name, old_keys)
        if actual != PRESERVED_HASHES[name]:
            raise RuntimeError(f"{name}: preserved behavior hash changed")

    if len(states) != 1:
        raise RuntimeError("partial exact-profile consolidation detected")
    for name, expected in BRANCH_HASHES.items():
        if resolved_hash(rs, name) != expected:
            raise RuntimeError(f"{name}: protected descendant behavior changed")
    flak_keys = {"Flak_Medium", "Flak_MediumFlatCompatibility"}
    for name, expected in FLAK_BRANCH_PRESERVED_HASHES.items():
        if resolved_hash(rs, name, flak_keys) != expected:
            raise RuntimeError(f"{name}: protected non-damage behavior changed")

    expected_flak = {
        "RA2FlakTrackGun_elite": {
            "Flak_MediumFlatCompatibility": (8000, 2488, "Ground, Water")},
        "AAGunBoatFlak": {
            "Flak_Medium": (2000, 10000, "Ground, Water, Air"),
            "Flak_MediumFlatCompatibility": (6000, 0, "Ground, Water")},
        "AAGunBoatFlak_elite": {
            "Flak_Medium": (2000, 10000, "Ground, Water, Air"),
            "Flak_MediumFlatCompatibility": (6000, 0, "Ground, Water")},
    }
    for name, expected in expected_flak.items():
        nodes = flat_nodes(rs.resolve_weapon(name))
        actual = {
            key: (
                int(str(nodes[key].get("Damage") or 0)),
                int(str(nodes[key].get("PercentageScale") or 0)),
                str(nodes[key].get("ValidTargets") or ""),
            )
            for key in expected
        }
        if actual != expected or set(main_warheads(rs.resolve_weapon(name))) != set(expected):
            raise RuntimeError(f"{name}: protected ground/air split changed")
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
              value: int) -> None:
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
    field_rows = [i for i in range(node_start + 1, node_end)
                  if lines[i].lstrip().startswith(f"{field}:")]
    if len(field_rows) > 1:
        raise RuntimeError(f"{name}: duplicate {field}")
    if field_rows:
        lines[field_rows[0]] = f"\t\t{field}: {value}\n"
    else:
        lines.insert(node_end, f"\t\t{field}: {value}\n")


def apply_changes(rs: Ruleset) -> None:
    changed: dict[pathlib.Path, list[str]] = {}

    _path, lines = lines_for(changed, rs, "RA2120xmm")
    remove_line(lines, "RA2120xmm",
                "\tInherits@roleflat: ^Compatibility_CannonHE_HeavyFlat")
    remove_node(lines, "RA2120xmm", "Warhead@CannonHE_HeavyFlatCompatibility")
    set_field(lines, "RA2120xmm", "CannonHE_Heavy", "Damage", 12000)
    set_field(lines, "RA2120xmm", "CannonHE_Heavy", "PercentageScale", 3325)
    _path, lines = lines_for(changed, rs, "RA2120xmm_rad")
    remove_line(lines, "RA2120xmm_rad",
                "\t-Warhead@CannonHE_HeavyFlatCompatibility:")
    set_field(lines, "RA2120xmm_rad", "CannonHE_Heavy", "PercentageScale", 10000)

    _path, lines = lines_for(changed, rs, "RA2FlakTrackGun")
    remove_line(lines, "RA2FlakTrackGun",
                "\tInherits@roleflat: ^Compatibility_Flak_MediumFlat")
    remove_node(lines, "RA2FlakTrackGun", "Warhead@Flak_MediumFlatCompatibility")
    set_field(lines, "RA2FlakTrackGun", "Flak_Medium", "Damage", 8000)
    set_field(lines, "RA2FlakTrackGun", "Flak_Medium", "PercentageScale", 2488)

    _path, lines = lines_for(changed, rs, "RA2FlakTrackGun_elite")
    start, _end = block_bounds(lines, "RA2FlakTrackGun_elite")
    lines.insert(start + 2,
                 "\tInherits@finalmain: ^Compatibility_Flak_MediumFlat\n")
    _path, lines = lines_for(changed, rs, "RA2FlakTrackAAGun")
    remove_line(lines, "RA2FlakTrackAAGun",
                "\t-Warhead@Flak_MediumFlatCompatibility:")

    _path, lines = lines_for(changed, rs, "AAGunBoatFlak")
    start, _end = block_bounds(lines, "AAGunBoatFlak")
    lines.insert(start + 2,
                 "\tInherits@groundflat: ^Compatibility_Flak_MediumFlat\n")
    set_field(lines, "AAGunBoatFlak", "Flak_Medium", "Damage", 2000)
    set_field(lines, "AAGunBoatFlak", "Flak_Medium", "PercentageScale", 10000)
    start, end = block_bounds(lines, "AAGunBoatFlak")
    insertion = end
    while insertion > start + 1 and not lines[insertion - 1].strip():
        insertion -= 1
    lines[insertion:insertion] = [
        "\tWarhead@Flak_MediumFlatCompatibility:\n",
        "\t\tValidTargets: Ground, Water\n",
        "\t\tDamage: 6000\n",
        "\t\tPercentageScale: 0\n",
    ]

    _path, lines = lines_for(changed, rs, "TSPulseCannon_EMP")
    remove_node(lines, "TSPulseCannon_EMP", "Warhead@TeslaWeapon")
    set_field(lines, "TSPulseCannon_EMP", "TeslaChargedWeapon", "Damage", 20000)

    for name, total in (
            ("TeslaArmorDischargeArc", 24000),
            ("TeslaArmorDischargeFragment1", 12000),
            ("TeslaArmorDischargeFragment2", 8000)):
        _path, lines = lines_for(changed, rs, name)
        remove_node(lines, name, "Warhead@LightMissile")
        set_field(lines, name, "TeslaWeapon", "Damage", total)

    for path, lines in changed.items():
        path.write_text("".join(lines), encoding="utf-8", newline="\n")


def validate_result() -> None:
    if not inspect(Ruleset(ROOT)):
        raise RuntimeError("exact-profile duplicate cohort remains unconsolidated")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    rules = Ruleset(ROOT)
    already = inspect(rules)
    if already:
        print(f"Already consolidated {len(SPECS)} concrete definitions")
        return 0
    print(f"4 roots; {len(SPECS)} concrete definitions")
    if not args.apply:
        print("Dry run: exact profiles, closures, percentages, and branch hashes pass")
        return 0
    apply_changes(rules)
    validate_result()
    print("Applied and validated 4 files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
