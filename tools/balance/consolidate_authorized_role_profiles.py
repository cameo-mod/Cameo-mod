#!/usr/bin/env python3
"""Apply the safe subset of the maintainer-authorized role redesign batch.

The seven edited roots resolve to twelve stacked concrete definitions because
the two Naxis tank-destroyer roots each have an elite and corrosion descendant.
The Molotov death child is in the guarded closure but already has one main and
must remain behavior-identical.

The Allied Tank Destroyer is deliberately held back: making its base cannon
pure AP would deepen an existing paid-Cryo upgrade regression.  Fixing that
requires a separate decision about the replacement weapon.

This is deliberately not described as behavior-preserving: the selected family
changes armor effectiveness and blast geometry.  The converter instead pins
flat totals, folded percentage output, target/relationship contracts, state
delivery, inheritance closures, and every non-selected resolved field.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import sys


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))

from audit_three_way_split import main_warhead_nodes, main_warheads  # noqa: E402
from consolidate_exact_profile_duplicates import (  # noqa: E402
    lines_for,
    remove_node,
    set_field,
)
from consolidate_reviewed_weapon_roots import block_bounds  # noqa: E402
from miniyaml import Ruleset  # noqa: E402
import percentage_damage as pd  # noqa: E402


# root: destination, reviewed batch members, combined flat damage,
# optional physical-state scale on the final main.
ROOTS = {
    "ASDFKamikazeExplosion": (
        "Demolition_Heavy", set(), 20000, None),
    "TSBusMortar": (
        "Concussion_Medium", set(), 64000, None),
    "ra1_soviets_molotovconscript_conscriptmolotov": (
        "Flame_Light", {"ra1_soviets_molotovconscript_conscriptmolotovexplode"}, 16000, 50),
    "tkm_trooper_gp25": (
        "Demolition_Light", set(), 12000, 50),
    "NaxiAntiTankCannon": (
        "CannonAP_Light",
        {"NaxiAntiTankCannonCorrosion", "NaxiAntiTankCannon_elite"},
        28000,
        None,
    ),
    "NaxiHetzerDestroyer": (
        "CannonAP_Light",
        {"NaxiHetzerDestroyerCorrosion", "NaxiHetzerDestroyer_elite"},
        40000,
        None,
    ),
    "AsianHowitzerCannon": (
        "CannonHE_Heavy", {"AsianHowitzerCannon_elite"}, 40000, None),
}

BASELINE_MAINS = {
    "ASDFKamikazeExplosion": {"Concussion_Medium", "Demolition_Heavy"},
    "TSBusMortar": {"Concussion_Medium", "Demolition_Heavy"},
    "ra1_soviets_molotovconscript_conscriptmolotov": {"Demolition_Light", "Flame_Light"},
    "tkm_trooper_gp25": {"Demolition_Light", "Flame_Light"},
    "NaxiAntiTankCannon": {"CannonAP_Light", "CannonHE_Medium"},
    "NaxiAntiTankCannonCorrosion": {"CannonAP_Light", "CannonHE_Medium"},
    "NaxiAntiTankCannon_elite": {"CannonAP_Light", "CannonHE_Medium"},
    "NaxiHetzerDestroyer": {"CannonAP_Light", "CannonHE_Medium"},
    "NaxiHetzerDestroyerCorrosion": {"CannonAP_Light", "CannonHE_Medium"},
    "NaxiHetzerDestroyer_elite": {"CannonAP_Light", "CannonHE_Medium"},
    "AsianHowitzerCannon": {"CannonHE_Heavy", "CannonHE_Medium"},
    "AsianHowitzerCannon_elite": {"CannonHE_Heavy", "CannonHE_Medium"},
}

DESTINATIONS = {
    "ASDFKamikazeExplosion": "Demolition_Heavy",
    "TSBusMortar": "Concussion_Medium",
    "ra1_soviets_molotovconscript_conscriptmolotov": "Flame_Light",
    "tkm_trooper_gp25": "Demolition_Light",
    "NaxiAntiTankCannon": "CannonAP_Light",
    "NaxiAntiTankCannonCorrosion": "CannonAP_Light",
    "NaxiAntiTankCannon_elite": "CannonAP_Light",
    "NaxiHetzerDestroyer": "CannonAP_Light",
    "NaxiHetzerDestroyerCorrosion": "CannonAP_Light",
    "NaxiHetzerDestroyer_elite": "CannonAP_Light",
    "AsianHowitzerCannon": "CannonHE_Heavy",
    "AsianHowitzerCannon_elite": "CannonHE_Heavy",
}

# Every reviewed descendant was de-parented onto direct template edges by the
# W7 chain-collapse batches (e40001270/9ec114771/8e553991e/76914f1c8/ae67c80fc).
# The gate now asserts no root regains weapon-parent descendants.
EXPECTED_CLOSURES = {root: set() for root in ROOTS}

TOTALS = {
    name: ROOTS[root][2]
    for root in ROOTS
    for name in {root, *ROOTS[root][1]}
    if name != "ra1_soviets_molotovconscript_conscriptmolotovexplode"
}

RUNTIME_UNITS = {
    name: total // 20 for name, total in TOTALS.items()
}

# Hashes omit only the selected old/final main nodes.  They therefore pin
# projectile, effects, delivery, cadence, targeting, relationships, corrosion,
# elite payloads, inferno triggers, and all other resolved behavior.
PRESERVED_HASHES = {
    "ASDFKamikazeExplosion": "15d7a5a6997ace2a0f2eae69e3a12b1fed9b7b569e7b34d9663a482f0f3deb02",
    "TSBusMortar": "a0d944d2dba35617d5b299744c511e38ac9bdf1f6055e3a26af088f7f764c3eb",
    "ra1_soviets_molotovconscript_conscriptmolotov": "f9dd87dfe31f6abf73eadb45cb6a83f8e83a36f3cd056405177941834abb0777",
    "ra1_soviets_molotovconscript_conscriptmolotovexplode": "d4c4546e3152e1a81f2243a632e84b1970f99597af017bdbb0806cd59e09509c",
    "tkm_trooper_gp25": "d6959b2746de94d48642a4db03d1ff10a526c0a52048fa302bc0a5310269fd80",
    "NaxiAntiTankCannon": "75d280345d7872e752e2e06e407d1d88b2572411c61d6f040bd1c7959bc264fa",
    "NaxiAntiTankCannonCorrosion": "c9d682d18c1bea656af526e450c67cb2fb03236b0c6f71e52244e08505193179",
    "NaxiAntiTankCannon_elite": "e4ceab6f54b2419773c968373c6bd45111cdfb850cdace92064d23d1263c084b",
    "NaxiHetzerDestroyer": "1e6ad43b14ac9247535a285159bf99c92e4c82bb6aa2b2e5c58014476ffa767b",
    "NaxiHetzerDestroyerCorrosion": "c72b31c4c046d1e8dc2092b1ba8efc6262c150d0e6edcfd9c100948c4b9b742b",
    "NaxiHetzerDestroyer_elite": "4c96c8c4fbeeeda47756001fce4cd293fe12a94cda1ea39d7c37cf629fbab9f9",
    "AsianHowitzerCannon": "03836a881def3646a958aac0e1d7322755fbd53eafc85fc9e3d5b06dc8ed495a",
    "AsianHowitzerCannon_elite": "7c250c79cc49f08816c1ba28fbddb4d288bd37c9f786c81254936c21f58431f3",
}

EXPECTED_CONTRACT = (
    "Ground, Water",
    "",
    "Ally, Neutral, Enemy",
    "",
    "50",
    "50",
    "",
    "",
    "",
    "Prone75Percent, TriggerProne, ExplosionDeath",
)

# 6978eebec (reference lane R16-R62) retagged the flame family's DamageTypes
# from ExplosionDeath to FireDeath + Incendiary so the base family matches the
# blends derived from it.
EXPECTED_CONTRACT_OVERRIDES = {
    "ra1_soviets_molotovconscript_conscriptmolotov": EXPECTED_CONTRACT[:9] + (
        "Prone75Percent, TriggerProne, FireDeath, Incendiary",),
}

# Complete resolved fingerprints for the selected main nodes.  This makes the
# converter accept exactly the audited before or after state while catching a
# later change to geometry, armor tables, percentage tables, targeting, state
# delivery, or any other field on those otherwise-excluded nodes.
EXPECTED_MAIN_HASHES = {
    "ASDFKamikazeExplosion": {
        False: {
            "Demolition_Heavy": "4a51cfea69f96a2086698bf3ea775bb82f9872ea0ff95a21128c97e4ec7fead1",
            "Concussion_Medium": "1774540468304f185b4b4fd5bfad4e58f2ebb020bfc654ec45e2fd6c28789547",
        },
        True: {"Demolition_Heavy": "f51a47d7692a551d00f7eff6e78193e4fc0409f771712a88ca04f6905c8c7a76"},
    },
    "AsianHowitzerCannon": {
        False: {
            "CannonHE_Medium": "417331a6846fb7fb7f7bf4ca60887f061897d5a62d367cb22f7636316e8d7396",
            "CannonHE_Heavy": "b1636a2732d8e0f198cd9def0dbdad6fca6a09cc16d18a683d00282cdb9c58f4",
        },
        True: {"CannonHE_Heavy": "61862023980e9ca6cb1618cbe0b7bc443fbe0263eec5ef2da7da27946f8cfa3e"},
    },
    "ra1_soviets_molotovconscript_conscriptmolotov": {
        False: {
            "Demolition_Light": "34aa4b8058d40e27c268a4bcf5ed70867c657a78ddf1d1efba119ad0461a3457",
            "Flame_Light": "9fd6bbeb93a6195bac87a6df75939d92c45e717beb9cf672f0bf7d4249c9d651",
        },
        True: {"Flame_Light": "fcfd841c24f76ed5616c25a8c86c71295404d37cef55490911edac5a684b6406"},
    },
    "NaxiAntiTankCannon": {
        False: {
            "CannonHE_Medium": "7916fecec1c54fa3ac12683e803e254c125d68cf0bbc274f51cbb460ff245d65",
            "CannonAP_Light": "9ee5309490214d50296a85f0656ec9dd1662a4a8c2582e003a7b3d1f8dd08aaa",
        },
        True: {"CannonAP_Light": "4f2336e1f57263006b337bb5a042fbe58df28ab757ed7df6092eb971c1dc58f3"},
    },
    "NaxiHetzerDestroyer": {
        False: {
            "CannonHE_Medium": "417331a6846fb7fb7f7bf4ca60887f061897d5a62d367cb22f7636316e8d7396",
            "CannonAP_Light": "f76168057aa417bef3b94a1c7acdb21a3f35d64a57eef6852df70f2d526acce2",
        },
        True: {"CannonAP_Light": "62c98c49f92faf386fb13a4632cf9b431f845bc9d10cde8991f189cbbd8bdbee"},
    },
    "TSBusMortar": {
        False: {
            "Concussion_Medium": "3a5b52dc269daaa1d2f5628538339aafe4d3a46ec5b7b4f779966e84e1a253cd",
            "Demolition_Heavy": "19dfe3224471126df71a317d3d1c666b3fc24412c91c16381030a6c85e465358",
        },
        True: {"Concussion_Medium": "dcec6b0d9cf80d54904fcfcc742fa561354b7e245e67127b14f6d15e96a4ce37"},
    },
    "tkm_trooper_gp25": {
        False: {
            "Demolition_Light": "b71f898d4e04ac5e8702dae1c577c59b42346e8e72300f9b1fe3e54d8151cc16",
            "Flame_Light": "6835836b8f1c93b1a226f4ae660e69a24fbb6ba507152f904401778363116522",
        },
        True: {"Demolition_Light": "00e38509938e238c9412e95cbc1e304208f57861cb58fbe4b0a8f0ad76a405de"},
    },
}


def descendants(rs: Ruleset, root: str) -> set[str]:
    direct: dict[str, set[str]] = {}
    for name, node in rs.weapons.items():
        for _key, parent in rs.inherits_of(node):
            if parent in rs.weapons:
                direct.setdefault(parent, set()).add(name)
    seen: set[str] = set()
    pending = list(direct.get(root, set()))
    while pending:
        name = pending.pop()
        if name in seen:
            continue
        seen.add(name)
        pending.extend(direct.get(name, set()))
    return {name for name in seen if not name.startswith("^")}


def node_payload(node):
    return [node.key, node.value, [node_payload(child) for child in node.children]]


def node_hash(node) -> str:
    raw = json.dumps(node_payload(node), separators=(",", ":")).encode()
    return hashlib.sha256(raw).hexdigest()


def resolved_hash(rs: Ruleset, name: str) -> str:
    excluded_keys = BASELINE_MAINS.get(name, set()) | {DESTINATIONS.get(name, "")}
    if name == "ra1_soviets_molotovconscript_conscriptmolotovexplode":
        excluded_keys |= {"Flame_LightFlatCompatibility", "Flame_Light_Flat"}
    excluded = {f"Warhead@{key}" for key in excluded_keys if key}
    payload = [
        node_payload(child)
        for child in rs.resolve_weapon(name).children
        if child.key not in excluded
    ]
    raw = json.dumps(payload, separators=(",", ":")).encode()
    return hashlib.sha256(raw).hexdigest()


def contract(node) -> tuple[str, ...]:
    return tuple(str(node.get(field) or "") for field in (
        "ValidTargets",
        "InvalidTargets",
        "ValidRelationships",
        "InvalidRelationships",
        "FriendlyFireDamage",
        "FriendlyFireSpread",
        "AffectsParent",
        "TargetActorCenter",
        "UpdatesUnitStatistics",
        "DamageTypes",
    ))


def runtime_units(resolved, keys: set[str]) -> int:
    return sum(
        int(application["runtime_units"])
        for application in pd.percentage_applications(resolved, 200000)
        if application["tag"] in keys
    )


def add_removal(lines: list[str], weapon: str, key: str) -> None:
    marker = f"\t-Warhead@{key}:"
    start, end = block_bounds(lines, weapon)
    if any(lines[index].rstrip("\r\n") == marker
           for index in range(start + 1, end)):
        raise RuntimeError(f"{weapon}: removal already exists for {key}")
    insertion = end
    while insertion > start + 1 and not lines[insertion - 1].strip():
        insertion -= 1
    lines.insert(insertion, marker + "\n")


def inspect(rs: Ruleset, print_hashes: bool = False) -> bool:
    for root in ROOTS:
        actual = descendants(rs, root)
        expected = EXPECTED_CLOSURES[root]
        if actual != expected:
            raise RuntimeError(
                f"{root}: closure changed; added={sorted(actual - expected)}, "
                f"missing={sorted(expected - actual)}")

    if print_hashes:
        for name in PRESERVED_HASHES:
            print(f'    "{name}": "{resolved_hash(rs, name)}",')
        return False

    states: set[bool] = set()
    for name, old_keys in BASELINE_MAINS.items():
        resolved = rs.resolve_weapon(name)
        destination = DESTINATIONS[name]
        mains = set(main_warheads(resolved))
        before = mains == old_keys
        after = mains == {destination}
        if not (before or after):
            raise RuntimeError(f"{name}: unexpected mains {sorted(mains)}")
        states.add(after)

        nodes = {
            node.key.split("@", 1)[1]: node
            for node in main_warhead_nodes(resolved)
        }
        owner = next(
            root for root, (_destination, children, _total, _state_scale) in ROOTS.items()
            if name == root or name in children
        )
        actual_main_hashes = {key: node_hash(node) for key, node in nodes.items()}
        if actual_main_hashes != EXPECTED_MAIN_HASHES[owner][after]:
            raise RuntimeError(f"{name}: selected-main fingerprint changed")
        if any(node.value != "AreaDamage" for node in nodes.values()):
            raise RuntimeError(f"{name}: selected main is not AreaDamage")
        if any(contract(node) != EXPECTED_CONTRACT_OVERRIDES.get(
                name, EXPECTED_CONTRACT) for node in nodes.values()):
            raise RuntimeError(f"{name}: target/relationship contract changed")

        total = sum(int(str(node.get("Damage") or 0)) for node in nodes.values())
        if total != TOTALS[name]:
            raise RuntimeError(f"{name}: flat total changed ({total})")
        if runtime_units(resolved, mains) != RUNTIME_UNITS[name]:
            raise RuntimeError(f"{name}: folded percentage units changed")

        state_nodes = {
            key: (node.get("PhysicalStateName"), node.get("PhysicalStateScale"))
            for key, node in nodes.items()
            if node.get("PhysicalStateName") or node.get("PhysicalStateScale")
        }
        if name in {"ra1_soviets_molotovconscript_conscriptmolotov", "tkm_trooper_gp25"}:
            expected_state = (
                {"Flame_Light": ("Temperature", "100")}
                if before else {destination: ("Temperature", "50")}
            )
            if state_nodes != expected_state:
                raise RuntimeError(f"{name}: Temperature delivery changed unexpectedly")
        elif state_nodes:
            raise RuntimeError(f"{name}: unexpected physical-state binding {state_nodes}")

        if resolved_hash(rs, name) != PRESERVED_HASHES[name]:
            raise RuntimeError(f"{name}: non-selected behavior hash changed")

    death = rs.resolve_weapon("ra1_soviets_molotovconscript_conscriptmolotovexplode")
    if set(main_warheads(death)) != {"Flame_LightFlatCompatibility"}:
        raise RuntimeError("ConscriptMolotovExplode: death payload changed")
    if resolved_hash(rs, "ra1_soviets_molotovconscript_conscriptmolotovexplode") != PRESERVED_HASHES[
            "ra1_soviets_molotovconscript_conscriptmolotovexplode"]:
        raise RuntimeError("ConscriptMolotovExplode: non-main behavior changed")

    if len(states) != 1:
        raise RuntimeError("partial authorized-role batch detected")
    return states == {True}


def apply_changes(rs: Ruleset) -> None:
    changed: dict[pathlib.Path, list[str]] = {}
    for root, (destination, _children, total, state_scale) in ROOTS.items():
        _path, lines = lines_for(changed, rs, root)
        removed = (BASELINE_MAINS[root] - {destination}).pop()
        remove_node(lines, root, f"Warhead@{removed}")
        add_removal(lines, root, removed)
        set_field(lines, root, destination, "Damage", total)
        if state_scale is not None:
            set_field(lines, root, destination, "PhysicalStateName", "Temperature")
            set_field(lines, root, destination, "PhysicalStateScale", state_scale)

    for path, lines in changed.items():
        path.write_text("".join(lines), encoding="utf-8", newline="\n")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--print-hashes", action="store_true")
    args = parser.parse_args()

    rules = Ruleset(ROOT)
    if args.print_hashes:
        inspect(rules, True)
        return 0
    applied = inspect(rules)
    if applied:
        print("Already consolidated 10 authorized definitions (12 resolved stacks)")
        return 0
    print("7 roots; 10 authorized definitions; 12 resolved stacks")
    if not args.apply:
        print("Dry run: closures, totals, percentages, contracts, states, and hashes pass")
        return 0
    apply_changes(rules)
    if not inspect(Ruleset(ROOT)):
        raise RuntimeError("authorized role batch remains unconsolidated")
    print("Applied and validated the authorized role batch")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
