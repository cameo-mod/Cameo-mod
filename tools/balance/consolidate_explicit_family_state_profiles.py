#!/usr/bin/env python3
"""Consolidate explicitly identified family profiles with reviewed state expansion.

Unlike the state-inert cohorts, this manifest intentionally makes the selected
Flame/Laser/Chemical/Plasma/Thermobaric family apply to the complete
flat and percentage payload.  Exact source mains, closures, routes, state scope,
percentage units, totals, scales, and non-selected behavior are all pinned.
"""
from __future__ import annotations

import argparse
import collections
import hashlib
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))

from audit_three_way_split import main_warheads  # noqa: E402
from consolidate_final_safe_cohorts import (  # noqa: E402
    HEALTH_VALUES, cleanup_duplicate_template_inherits,
    cleanup_stale_removals, flat_main_nodes, tokens,
)
from consolidate_reviewed_weapon_roots import (  # noqa: E402
    add_compatibility_templates, apply_compatibility_block, block_bounds,
    resolved_flat_total,
)
from consolidate_role_complete_profiles import update_compatibility_block  # noqa: E402
from miniyaml import Ruleset  # noqa: E402
import percentage_damage as pd  # noqa: E402


# name: destination, flat total, folded PercentageScale
SPECS = {
    "ra1_soviets_molotovconscript_conscriptmolotovexplode": ("Flame_Light", 8000, 9988),
    "ra1_soviets_grenadier_grenaderaexplode": ("Flame_Light", 8000, 9988),
    "IncendiaryM1Carbine": ("Flame_Light", 4000, 9975),
    # e4213b6dd (accepted classic-four batch #401) retuned the applied flat
    # total 4000 -> 7208; the recorded scale is unchanged.
    "ra1_soviets_rifleinfantry_carbine_incendiary": ("Flame_Light", 7208, 9975),
    "HeavyPlasmaFlamer": ("Flame_Heavy", 4000, 9975),
    "OIPlasmaFlamer": ("Flame_Heavy", 4000, 9975),
    # 1ae9c7d02 (R17 chip folds, W5 batch-1) folded the laser chip damage into
    # the main verbatim: 48000 -> 49600; the recorded scale is unchanged.
    "PhobosLaser": ("Laser_Heavy", 49600, 1248),
    "d2kCarryallChainGun_upgrade": ("Laser_Heavy", 6000, 9984),
    "d2kChainGun_upgrade": ("Laser_Heavy", 8000, 9988),
    "LMG_ordos_upgrade": ("Laser_Heavy", 6000, 9984),
    "light_inf_lmg_ordos_upgrade": ("Laser_Heavy", 6000, 9984),
    "SteelFighterRailgun": ("Laser_Heavy", 10000, 5990),
    "ra1_soviets_migattackbomber_thermobaricmaverick": ("Thermobaric_Heavy", 48000, 2498),
    "AsianChemicalBombs": ("Chemical_Heavy", 4000, 9975),
    "TSSAPCCoreMissiles": ("Chemical_Light", 24000, 3330),
    "FutureMechPlasma": ("Plasma_Heavy", 30000, 3330),
    "BuggyPlasmaGrenade": ("Plasma_Light", 60000, 3332),
    "PositronGrenade": ("Quantum_Medium", 40000, 498),
}

BASELINE_MAINS = {
    "ra1_soviets_molotovconscript_conscriptmolotovexplode": {"Demolition_Light", "Flame_Light"},
    "ra1_soviets_grenadier_grenaderaexplode": {"Demolition_Light", "Flame_Light"},
    "IncendiaryM1Carbine": {"Bullet_Light", "Flame_Light"},
    "ra1_soviets_rifleinfantry_carbine_incendiary": {"Bullet_Light", "Flame_Light"},
    "HeavyPlasmaFlamer": {"Chemical_Heavy", "Flame_Heavy"},
    "OIPlasmaFlamer": {"Chemical_Heavy", "Flame_Heavy"},
    "PhobosLaser": {"CannonHE_Heavy", "Laser_Heavy"},
    "d2kCarryallChainGun_upgrade": {"Bullet_Medium", "Laser_Heavy"},
    "d2kChainGun_upgrade": {"Bullet_Medium", "Laser_Heavy"},
    "LMG_ordos_upgrade": {"Bullet_Light", "Bullet_Medium", "Laser_Heavy"},
    "light_inf_lmg_ordos_upgrade": {"Bullet_LightFlatCompatibility", "Laser_Heavy"},
    "SteelFighterRailgun": {"CannonHE_Medium", "Laser_Heavy", "MissileAP_Light", "Railgun_Heavy"},
    "ra1_soviets_migattackbomber_thermobaricmaverick": {"MissileAP_Medium", "Thermobaric_Heavy"},
    "AsianChemicalBombs": {"CannonHE_Medium", "Chemical_Heavy"},
    "TSSAPCCoreMissiles": {"Chemical_Light", "MissileHE_Medium"},
    "FutureMechPlasma": {"CannonHE_Heavy", "Plasma_Heavy"},
    "BuggyPlasmaGrenade": {"Demolition_Light", "Plasma_LightFlatCompatibility"},
    "PositronGrenade": {"CannonHE_Medium", "Quantum_MediumFlatCompatibility"},
}

# destination flat damage before -> complete flat total, and destination
# percentage runtime units before -> complete selected-family runtime units.
STATE_EXPANSION = {
    "ra1_soviets_molotovconscript_conscriptmolotovexplode": (4000, 8000, 200, 400),
    "ra1_soviets_grenadier_grenaderaexplode": (4000, 8000, 200, 400),
    "IncendiaryM1Carbine": (2000, 4000, 100, 200),
    "ra1_soviets_rifleinfantry_carbine_incendiary": (2000, 7208, 100, 359),
    "HeavyPlasmaFlamer": (2000, 4000, 100, 200),
    "OIPlasmaFlamer": (2000, 4000, 100, 200),
    "PhobosLaser": (42000, 49600, 0, 310),
    "d2kCarryallChainGun_upgrade": (2000, 6000, 100, 300),
    "d2kChainGun_upgrade": (4000, 8000, 200, 400),
    "LMG_ordos_upgrade": (2000, 6000, 100, 300),
    "light_inf_lmg_ordos_upgrade": (2000, 6000, 100, 300),
    "SteelFighterRailgun": (4000, 10000, 0, 300),
    "ra1_soviets_migattackbomber_thermobaricmaverick": (36000, 48000, 0, 600),
    "AsianChemicalBombs": (2000, 4000, 100, 200),
    "TSSAPCCoreMissiles": (8000, 24000, 400, 400),
    "FutureMechPlasma": (20000, 30000, 0, 500),
    "BuggyPlasmaGrenade": (40000, 60000, 0, 1000),
    "PositronGrenade": (32000, 40000, 0, 100),
}

ROOT_CLOSURES = {
    "ra1_soviets_molotovconscript_conscriptmolotovexplode": set(),
    "ra1_soviets_grenadier_grenaderaexplode": set(),
    "IncendiaryM1Carbine": set(),
    "ra1_soviets_rifleinfantry_carbine_incendiary": set(),
    "HeavyPlasmaFlamer": set(), "OIPlasmaFlamer": set(), "PhobosLaser": set(),
    "d2kCarryallChainGun_upgrade": set(), "d2kChainGun_upgrade": set(),
    "LMG_ordos_upgrade": set(), "light_inf_lmg_ordos_upgrade": set(),
    "SteelFighterRailgun": set(), "ra1_soviets_migattackbomber_thermobaricmaverick": set(),
    "AsianChemicalBombs": set(), "TSSAPCCoreMissiles": set(),
    "FutureMechPlasma": {"FutureMechPlasma_elite"},
    "BuggyPlasmaGrenade": set(),
    "PositronGrenade": {"PositronBounce1", "PositronBounce2"},
}

PINNED_DESCENDANTS = {
    "FutureMechPlasma_elite", "PositronBounce1", "PositronBounce2",
}
GROUND_ONLY = {
    "ra1_soviets_molotovconscript_conscriptmolotovexplode", "ra1_soviets_grenadier_grenaderaexplode",
    "ra1_soviets_rifleinfantry_carbine_incendiary", "HeavyPlasmaFlamer",
    "OIPlasmaFlamer", "AsianChemicalBombs", "FutureMechPlasma",
    "BuggyPlasmaGrenade", "PositronGrenade",
}
CHILD_OVERRIDES = set()
POSITRON_PINS = {"PositronBounce1", "PositronBounce2"}

PRESERVED_HASHES = {
    "AsianChemicalBombs": "5caeae4cdaa0404694653f053ffd91890ba8abd37cd44bc115b4ba1d0bdb5180",
    "BuggyPlasmaGrenade": "4cfcc927d8a755144998f8fb59965d7c50d466b72348187f8264f83a4152835c",
    "ra1_soviets_molotovconscript_conscriptmolotovexplode": "d4c4546e3152e1a81f2243a632e84b1970f99597af017bdbb0806cd59e09509c",
    "FutureMechPlasma": "f570c6660ea91e78131208c3a963a484bfd1ea9798f951847e3695ab81992038",
    "ra1_soviets_grenadier_grenaderaexplode": "e24763f460ed219842d640d9708eeeb73e7cf2324970915ac21fd435c5793425",
    "HeavyPlasmaFlamer": "c8917cb19a691b7bc58b3f337e413d325a9eb605efbad3c6eff206620a7b0a3f",
    "IncendiaryM1Carbine": "6549fff9de9d2ad25086c30ddf7291edb89611c20c19a973b7e8bd0e6c4fe922",
    "LMG_ordos_upgrade": "c47c0a92deb5168e7930027ee12496a7ece0e218647c1555db610dbee724896a",
    "OIPlasmaFlamer": "7375a13449ca48be29407b3b37e165081feb1f27c0e1e4c2914cac0d10fdf855",
    "PhobosLaser": "73c8238318ab6f1ebade26b64294544fe923f5a9c04527f612f5911e6133f3b0",
    # Strict MiniYAML requires the CannonHE parent to live only on the bounce
    # children.  The root keeps its equivalent top-level fields locally, which
    # changes node ordering but not runtime behavior.
    "PositronGrenade": "3bf7f954a7ca86c7a239e20cc5e67fe4338042a533f41bbf2ec87b192885f17e",
    "SteelFighterRailgun": "e80997a1441738c04406bc0cfe36b286156a5adf42196ff90d7eb312060ff66e",
    "TSSAPCCoreMissiles": "9b4d9f83250ffe3df04be6b4152c2556399a53f81804a99963de01bead0562a3",
    "ra1_soviets_migattackbomber_thermobaricmaverick": "463ffaccc2db0bdf0b822bb1a5a51de26fb3fd740fe6f1eb806a75542d3440bf",
    "d2kCarryallChainGun_upgrade": "a9c0bcbbb215911fe322d4becad9cb46ee92ae9bdb3b872dd5123af3ff9c6a11",
    "d2kChainGun_upgrade": "e8bb457f611ed622cd283d3fbf6a06ab68032a78507318919e0585d970efad52",
    "light_inf_lmg_ordos_upgrade": "d1bdfd546c46345c1d0fde68390abf045f6065d2155f1395712ab7337b8d16da",
    "ra1_soviets_rifleinfantry_carbine_incendiary": "32b894d8a27e6b3a34e85f8902aea8c0fdfe598d0f560f1124011ed34265ccd8",
}
PINNED_HASHES = {
    "FutureMechPlasma_elite": "36df29b0289feeb15fa8517fb59dd8fe674ec7303b379f0f99f86fa4424eca3f",
    "PositronBounce1": "8880ff643dbb764c6b851995accac71f7ca14f6b983fc98f8e44dd0ce0ebb9b1",
    "PositronBounce2": "42db39b70a91a43f793548b4c0eaf6a4c5e4cbf997f3f1325f332cd215f42e0d",
}

CONTRACT_FIELDS = (
    "ValidTargets", "InvalidTargets", "ValidRelationships",
    "InvalidRelationships", "AffectsParent", "TargetActorCenter",
)


def descendants(rs: Ruleset, root: str) -> set[str]:
    direct: dict[str, set[str]] = collections.defaultdict(set)
    for name, node in rs.weapons.items():
        for _, parent in rs.inherits_of(node):
            if parent in rs.weapons:
                direct[parent].add(name)
    seen, stack = set(), list(direct[root])
    while stack:
        name = stack.pop()
        if name in seen:
            continue
        seen.add(name)
        stack.extend(direct[name])
    return {name for name in seen if not name.startswith("^")}


def node_payload(node):
    return [node.key, node.value, [node_payload(child) for child in node.children]]


def resolved_hash(rs: Ruleset, name: str, destination: str) -> str:
    excluded = {f"Warhead@{key}" for key in BASELINE_MAINS[name]}
    excluded.add(f"Warhead@{destination}FlatCompatibility")
    payload = [node_payload(child) for child in rs.resolve_weapon(name).children
               if child.key not in excluded]
    return hashlib.sha256(json.dumps(payload, separators=(",", ":")).encode()).hexdigest()


def full_hash(rs: Ruleset, name: str) -> str:
    # Top-level ordering can move when a child restates a template removed by
    # its converted parent.  MiniYAML keys remain identical and comparator
    # invariants prove behavior; hash every resolved key/value subtree exactly.
    payload = sorted(
        (node_payload(child) for child in rs.resolve_weapon(name).children),
        key=lambda item: (item[0], str(item[1])),
    )
    return hashlib.sha256(json.dumps(payload, separators=(",", ":")).encode()).hexdigest()


def runtime_units(resolved, keys: set[str]) -> int:
    return sum(int(app["runtime_units"])
               for app in pd.percentage_applications(resolved, 200_000)
               if app["tag"] in keys)


def expected_contract(name: str):
    targets = ("Ground", "Water") if name in GROUND_ONLY else ("Air", "Ground", "Water")
    return CONTRACT_OVERRIDES.get(
        name, (targets, (), ("Ally", "Enemy", "Neutral"), (), (), ()))


# Post-recording waves added explicit InvalidTargets rows the recorded contract
# predates; each entry is the resolved contract the member now carries.
CONTRACT_OVERRIDES = {
    # daf6c497a-era template promotion wrote InvalidTargets: Air onto the
    # weapon's own Warhead@Quantum_Medium node.
    "PositronGrenade": (
        ("Ground", "Water"), ("Air",), ("Ally", "Enemy", "Neutral"), (), (), ()),
}


def inspect(rs: Ruleset, print_hashes: bool = False) -> bool:
    for root, expected in ROOT_CLOSURES.items():
        actual = descendants(rs, root)
        if actual != expected:
            raise RuntimeError(
                f"{root}: closure changed; added={sorted(actual - expected)}, "
                f"missing={sorted(expected - actual)}")
    if set(SPECS) != set(BASELINE_MAINS) or set(SPECS) != set(STATE_EXPANSION):
        raise RuntimeError("family-state manifests differ")
    if print_hashes:
        print("PRESERVED_HASHES")
        for name, (destination, _total, _scale) in sorted(SPECS.items()):
            print(f'    "{name}": "{resolved_hash(rs, name, destination)}",')
        print("PINNED_HASHES")
        for name in sorted(PINNED_DESCENDANTS):
            print(f'    "{name}": "{full_hash(rs, name)}",')
        return False

    states = set()
    for name, (destination, total, scale) in SPECS.items():
        resolved = rs.resolve_weapon(name)
        mains = set(main_warheads(resolved))
        compatibility = f"{destination}FlatCompatibility"
        before = mains == BASELINE_MAINS[name]
        # Post-R12 the applied main resolves under the bare destination tag on
        # families that never carried a compatibility payload; under the
        # historical view `*_Flat` restores to `*FlatCompatibility`.
        after = mains == {compatibility} or mains == {destination}
        if not (before or after):
            raise RuntimeError(f"{name}: unexpected mains {sorted(mains)}")
        states.add(after)
        nodes = flat_main_nodes(resolved, mains)
        if set(nodes) != mains:
            raise RuntimeError(f"{name}: selected non-flat main")
        contracts = {
            tuple(tokens(node.get(field)) for field in CONTRACT_FIELDS)
            for node in nodes.values()
        }
        expected_routes = {expected_contract(name)}
        if contracts != expected_routes:
            raise RuntimeError(f"{name}: route/relationship contract changed: {contracts}")
        old_flat, new_flat, old_units, new_units = STATE_EXPANSION[name]
        if new_flat != total:
            raise RuntimeError(f"{name}: state expansion total disagrees with spec")
        if before:
            destination_key = (destination if destination in nodes else compatibility)
            if int(str(nodes[destination_key].get("Damage") or 0)) != old_flat:
                raise RuntimeError(f"{name}: destination state-bearing flat scope changed")
            if resolved_flat_total(resolved, mains) != total:
                raise RuntimeError(f"{name}: source flat total changed")
            if runtime_units(resolved, {destination_key}) != old_units:
                raise RuntimeError(f"{name}: destination percentage units changed")
            if runtime_units(resolved, mains) != new_units:
                raise RuntimeError(f"{name}: complete percentage units changed")
            folded_units = pd.folded_units(total, scale)[1]
            if folded_units != new_units:
                raise RuntimeError(f"{name}: folded percentage units changed")
            for hp in HEALTH_VALUES:
                old_hp = sum(int(app["runtime_hp"])
                             for app in pd.percentage_applications(resolved, hp)
                             if app["tag"] in mains)
                new_hp = pd.runtime_percentage_hp(
                    hp, folded_units, pd.FOLDED_DEFAULT_DENOMINATOR)
                if abs(old_hp - new_hp) > 1:
                    raise RuntimeError(f"{name}: percentage drift exceeds one HP at {hp}")
        else:
            applied_key = compatibility if compatibility in nodes else destination
            node = nodes[applied_key]
            if int(str(node.get("Damage") or 0)) != total:
                raise RuntimeError(f"{name}: applied flat total changed")
            if int(str(node.get("PercentageScale") or 0)) != scale:
                raise RuntimeError(f"{name}: applied percentage scale changed")
            if runtime_units(resolved, {applied_key}) != new_units:
                raise RuntimeError(f"{name}: applied percentage units changed")
        if PRESERVED_HASHES and resolved_hash(rs, name, destination) != PRESERVED_HASHES[name]:
            raise RuntimeError(f"{name}: projectile/effect/cadence/non-selected behavior changed")

    for name in PINNED_DESCENDANTS:
        if PINNED_HASHES and full_hash(rs, name) != PINNED_HASHES[name]:
            raise RuntimeError(f"{name}: pinned descendant changed")
    if len(states) != 1:
        raise RuntimeError("partial family-state consolidation detected")
    return states == {True}


def local_has(lines: list[str], weapon: str, marker: str) -> bool:
    start, end = block_bounds(lines, weapon)
    return any(lines[index].rstrip("\r\n") == marker for index in range(start + 1, end))


def add_removal(lines: list[str], weapon: str, key: str) -> None:
    marker = f"\t-Warhead@{key}:"
    if local_has(lines, weapon, marker):
        return
    start, end = block_bounds(lines, weapon)
    insertion = end
    while insertion > start + 1 and not lines[insertion - 1].strip():
        insertion -= 1
    lines.insert(insertion, marker + "\n")


def remove_removal(lines: list[str], weapon: str, key: str) -> None:
    marker = f"\t-Warhead@{key}:"
    start, end = block_bounds(lines, weapon)
    rows = [i for i in range(start + 1, end)
            if lines[i].rstrip("\r\n") == marker]
    if len(rows) > 1:
        raise RuntimeError(f"{weapon}: duplicate removal for {key}")
    if rows:
        del lines[rows[0]]


def add_inherit(lines: list[str], weapon: str, marker: str) -> None:
    if local_has(lines, weapon, marker):
        return
    start, _end = block_bounds(lines, weapon)
    lines.insert(start + 1, marker + "\n")


def convert_member(rs: Ruleset, changed, name: str, destination: str,
                   total: int, scale: int, inherit_template: bool) -> None:
    path = pathlib.Path(rs.weapon(name).file)
    lines = changed.setdefault(path, path.read_text(encoding="utf-8-sig").splitlines(True))
    compatibility = f"{destination}FlatCompatibility"
    remove_removal(lines, name, compatibility)
    removals = BASELINE_MAINS[name] - {compatibility}
    if local_has(lines, name, f"\tWarhead@{compatibility}:"):
        update_compatibility_block(lines, name, destination, total, scale, None)
        for key in removals:
            add_removal(lines, name, key)
    else:
        targets = "Ground, Water" if name in GROUND_ONLY else "Ground, Water, Air"
        apply_compatibility_block(
            changed, path, name, destination, removals, total, targets,
            inherit_template=inherit_template)
        update_compatibility_block(lines, name, destination, total, scale, None)


def apply_changes(rs: Ruleset) -> None:
    changed = {}
    add_compatibility_templates(
        changed, rs, {destination for destination, _total, _scale in SPECS.values()},
        ["# Canonical flat profiles for explicit full-family state expansion.\n"])
    for name, (destination, total, scale) in SPECS.items():
        convert_member(rs, changed, name, destination, total, scale,
                       inherit_template=name not in CHILD_OVERRIDES)
    for name in POSITRON_PINS:
        path = pathlib.Path(rs.weapon(name).file)
        lines = changed.setdefault(path, path.read_text(encoding="utf-8-sig").splitlines(True))
        add_inherit(lines, name, "\tInherits@pinnedcannon: ^Warhead_CannonHE_Medium")
    # PositronGrenade's descendants need to pin CannonHE themselves, but strict
    # MiniYAML forbids inheriting the same parent both directly and through the
    # root.  Preserve the root-level fields locally and leave the warhead parent
    # exclusively on the two bounce children.
    positron_path = pathlib.Path(rs.weapon("PositronGrenade").file)
    positron_lines = changed.setdefault(
        positron_path, positron_path.read_text(encoding="utf-8-sig").splitlines(True))
    start, end = block_bounds(positron_lines, "PositronGrenade")
    direct_parent = "\tInherits: ^Warhead_CannonHE_Medium\n"
    if direct_parent in positron_lines[start:end]:
        positron_lines.remove(direct_parent)
    start, end = block_bounds(positron_lines, "PositronGrenade")
    insert_at = next(index for index in range(start + 1, end)
                     if positron_lines[index].startswith("\tReloadDelay:"))
    for field in reversed(("\tValidTargets: Ground, Water\n",
                           "\tTargetActorCenter: true\n")):
        if field not in positron_lines[start:end]:
            positron_lines.insert(insert_at, field)
    start, end = block_bounds(positron_lines, "PositronGrenade")
    obsolete = "\tWarhead@CannonHE_Medium:\n"
    if obsolete in positron_lines[start:end]:
        obsolete_at = positron_lines.index(obsolete, start, end)
        obsolete_end = obsolete_at + 1
        while obsolete_end < end and positron_lines[obsolete_end].startswith("\t\t"):
            obsolete_end += 1
        del positron_lines[obsolete_at:obsolete_end]
    remove_removal(positron_lines, "PositronGrenade", "CannonHE_Medium")
    for path, lines in changed.items():
        path.write_text("".join(lines), encoding="utf-8", newline="\n")
    cleanup_stale_removals(set(SPECS) | {"FutureMechPlasma_elite"})
    cleanup_duplicate_template_inherits(set(SPECS) | {"FutureMechPlasma_elite"})


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--print-hashes", action="store_true")
    args = parser.parse_args()
    rs = Ruleset(ROOT)
    if args.print_hashes:
        inspect(rs, True)
        return 0
    already = inspect(rs)
    if already:
        print("Already consolidated 18 explicit family-state definitions")
        return 0
    print("18 roots; 18 multi-main conversions; 3 exact descendant pins")
    if not args.apply:
        print("Dry run: closures, mains, routes, state scope, percentage units, totals, and hashes pass")
        return 0
    apply_changes(rs)
    if not inspect(Ruleset(ROOT)):
        raise RuntimeError("family-state cohort remains unconsolidated")
    print("Applied and validated explicit family-state cohort")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
