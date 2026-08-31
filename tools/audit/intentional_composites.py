#!/usr/bin/env python3
"""Exact decision fingerprints for reviewed multi-main weapons.

Review status is classification metadata only.  It must never change the raw
structural inventory.  Every approved weapon is named explicitly and pinned to
its full resolved behavior plus its exact resolved referrers.
"""
from __future__ import annotations

import argparse
import functools
import hashlib
import json
import pathlib
from typing import Callable


ROOT = pathlib.Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "docs" / "audit" / "intentional_weapon_composites.json"

WEAPON_REF_FIELDS = {
    "Weapon", "Weapons", "TriggeredWeapon", "FallbackWeapon",
    "EmptyWeapon", "Explosion", "CasingWeapon", "ImpactWeapon",
    "TeleportWeapon", "DemolishWeapon", "HelixWeapon", "TriggerWeapon",
    "ThumpDamageWeapon", "DetonationWeapon", "MissileWeapon",
}
WEAPON_REF_MAP_FIELDS = {"MissileWeapons"}

# Curated decisions.  Every row expands to exact concrete names; no patterns,
# inheritance templates, or wildcard approval are permitted.
DECISION_GROUPS = {
    "staged superweapon": (
        (("ExecutionerDeath", "HermitExplode", "MiniNova", "MiniNuke",
          "RA2DemoBomb", "ReactorNuke", "ReactorNukeWeak"),
         ("10Dam_areanuke3", "11Dam_areanuke3", "1Dam_impact",
          "4Dam_areanuke1", "7Dam_areanuke2", "8Dam_areanuke2", "Damage")),
        (("CabalMagicNuke", "PulseMissile"),
         ("10Dam_areanuke3", "11Dam_areanuke3", "1Dam_impact",
          "4Dam_areanuke1", "7Dam_areanuke2", "8Dam_areanuke2",
          "Tesla_Heavy", "Tesla_Super")),
        (("supernova_missile_super",),
         ("10Dam_areanukec", "1Dam_impact", "3Dam_areanukea",
          "7Dam_areanukeb")),
        (("Atomic", "NaxiV1Rocket", "RA2Atomic", "RAAtomic"),
         ("Nuclear_Super", "Tesla_Heavy", "Tesla_Super")),
        (("SteelInspectorIonCannon", "SteelInspectorIonCannonDamage",
          "SteelIonCannonDamage", "TDIonCannonDamage"),
         ("IonCannon", "Tesla_Heavy", "Tesla_Super")),
        (("AsianTSIonCannon", "TSIonCannon"),
         ("IonCannon", "TeslaChargedWeapon", "TeslaWeapon",
          "Tesla_Heavy", "Tesla_Super")),
    ),
    "status payload": (
        (("edenTiger_EMP", "edenTiger_EMP_AA", "eden_EMP", "eden_EMP_AA",
          "eden_EMP_GP", "plymouth_EMP", "plymouth_EMP_Tiger"),
         ("TemperatureCompatibility", "Tesla_Super")),
        (("SteelAirTurretEScatter", "SteelAirTurret_EMP", "SteelAirTurret_elite",
          "SteelStalkerRailgunEScatter", "SteelStalkerRailgun_EMP",
          "SteelStalkerRailgun_elite"),
         ("Quantum_HeavyFlatCompatibility", "Tesla_Heavy")),
        (("EMPGrenade",),
         ("TemperatureCompatibility", "TeslaSharedCompatibility", "Tesla_Super")),
        (("EMPGrenadeExplode",),
         ("TemperatureCompatibility", "TeslaAirCompatibility",
          "TeslaSharedCompatibility", "Tesla_Super")),
        (("AsianSniperLockdown", "GhostSniperLockdown", "SpecterSniperLockdown",
          "VonSniperLockdown"),
         ("Bullet_Heavy", "Bullet_Medium", "SniperChaingun", "SniperFlak",
          "SniperSmallArms", "Tesla_Super")),
        (("KodiakCannonSonic",), ("CannonHE_Heavy", "Sonic_Heavy")),
        (("TSGrenadeSonic",), ("Concussion_Light", "Sonic_Light")),
        (("TSBombSonic",), ("Demolition_Heavy", "Sonic_Heavy")),
        (("TSHellfireSonic",), ("MissileAP_Heavy", "Sonic_Medium")),
        (("TSZoneHellfireSonic",), ("MissileAP_Heavy", "Sonic_Heavy")),
        (("AphidCryo_AA", "HellfireCryo"),
         ("CryoBlast_Medium", "MissileCryo_Heavy")),
        (("ChemRockets", "ChemRocketsExplosion"),
         ("ChemRocketCompatibility", "Chemical_Light")),
        (("MigMissiles_rad", "MigMissiles_rad_elite"),
         ("Chemical_Medium", "MissileAP_Medium")),
        (("RA2KirovBomb_rad", "TSCropBombChem", "TSLocustBombChem"),
         ("Chemical_Heavy", "Demolition_Heavy")),
        (("RA2KirovBomb_nuclear", "RA2KirovBomb_nuclear_elite"),
         ("Demolition_Heavy", "Nuclear_Super")),
        (("RA2KirovBomb_tesla",), ("Demolition_Heavy", "Tesla_Super")),
        (("AsianChaosMine",), ("CannonAP_Light", "Chemical_Heavy")),
        (("LaserObeliskBurning",), ("Inferno_Heavy", "Laser_Heavy")),
        (("RA2LasherToxicMortar_elite",),
         ("CannonChem_MediumFlatCompatibility", "CannonHE_Medium")),
    ),
    "target-routed composite": (
        (("FutureEnforcerShotgun", "FutureEnforcerShotgunDeployed",
          "FutureEnforcerShotgunDeployed_elite", "FutureEnforcerShotgun_elite",
          "TSCommandoShotgun", "TSMutShotgun", "TSShotgun"),
         ("CannonHE_Medium", "ShotgunChaingun", "ShotgunShrapnelEnemy",
          "ShotgunSmallArms", "ShotgunTankDestroyer")),
        (("AsianSniper", "GDISniperRifle", "GhostSniper", "SpecterSniper",
          "VonSniper"),
         ("Bullet_Heavy", "SniperChaingun", "SniperSmallArms")),
        (("AsianSniperAP", "VonSniperAP"),
         ("Bullet_Heavy", "Bullet_Medium", "SniperChaingun", "SniperSmallArms")),
        (("CommandoSniper",), ("Bullet_Heavy", "SniperChaingun")),
        (("CommandoM16",), ("Bullet_Medium", "SniperCompatibility")),
        (("td_gdi_commando_sniper_elite",), ("Railgun_Heavy", "Sniper_Light")),
        (("ArcherArtilleryShell", "ArtilleryShellUpgrade", "wc2catapultFire"),
         ("CollapseTargetCompatibility1", "Concussion_Heavy")),
        (("BallistaMultiShot", "BallistaTowerMultiShot"),
         ("Arrow_Medium", "CollapseTargetCompatibility1")),
        (("wc2_dwarf_Rifle",), ("Bullet_Medium", "CollapseTargetCompatibility1")),
        (("D2K_155mm2",), ("CannonHE_Heavy", "CollapseTargetCompatibility1")),
        (("eye_bomberguy",), ("CollapseTargetCompatibility1", "Demolition_Heavy")),
        (("BikeRockets",), ("CollapseTargetCompatibility1", "MissileAP_Medium")),
        (("v1rocketsThermobaric",),
         ("CollapseTargetCompatibility1", "Thermobaric_Heavy")),
        (("DRPlasmaTankWeapon",), ("1Dam", "1DamBuildings")),
        (("PLYMineExplosive",), ("1Dam", "2Dam")),
        (("SaboDeath",), ("1Dam", "EnemyDam")),
        (("SonicZap",), ("ExtraSquidDamage", "Magic_Heavy")),
        (("ATMine",), ("ATMineDemolition_Light", "Demolition_Light")),
        (("TSLaser90mm", "TSLaser90mmDep"),
         ("CannonAP_Medium", "TSLaserCannonAP_Medium", "TSLaserShieldChip")),
        (("TSCABALEnlightedLaser", "TSCABALObeliskLaserFire"),
         ("CabalLaserGroundCompatibility", "Laser_Heavy")),
        (("BlackHandLaser",), ("LaserHeavyGroundRemainder", "Laser_Heavy")),
        (("TSProton",), ("Laser_Heavy", "ProtonLaserGroundCompatibility")),
        (("CabalAscendedRockets",),
         ("MissileHE_Heavy", "MissileHE_HeavyGroundBonus")),
        (("NodTorpTube", "TorpTube"), ("Concussion_Light", "MissileHE_Heavy")),
        (("NodTorpTubeBlackMarket",), ("Demolition_Heavy", "MissileHE_Heavy")),
        (("Fremen_RPG", "mtank_pri"), ("1Dam", "MissileAP_Heavy")),
        (("ordos_airmine",), ("1Dam", "MissileAP_HeavyFlatCompatibility")),
        (("RA2Virusgun2",), ("Flak_Medium", "Sniper_LightFlatCompatibility")),
    ),
    "effect-delivery composite": (
        (("ZeroFighterChainGunWaveforce",),
         ("Bullet_Medium", "Railgun_Heavy", "ZeroFighterBullet_Medium")),
        (("ArmoredCarMGWaveforce",), ("Bullet_Medium", "Railgun_Heavy")),
        (("JapaneseHovercraftFlakAAkWaveforce", "JapaneseHovercraftFlakWaveforce"),
         ("Flak_MediumFlatCompatibility", "Railgun_Heavy")),
        (("WaveforceCannon", "WaveforceCannonChargedLaser"),
         ("MissileHE_Heavy", "Railgun_Heavy")),
        (("WaveArtilleryImpact",), ("Railgun_Heavy", "Tesla_Heavy")),
    ),
}


def digest(value) -> str:
    payload = json.dumps(
        value, sort_keys=True, separators=(",", ":"), ensure_ascii=True).encode()
    return hashlib.sha256(payload).hexdigest()


def node_payload(node):
    children = [node_payload(child) for child in node.children]
    children.sort(key=lambda value: json.dumps(
        value, sort_keys=True, separators=(",", ":"), ensure_ascii=True))
    return {
        "children": children,
        "key": str(node.key),
        "value": "" if node.value is None else str(node.value),
    }


def walk_with_path(node, path=()):
    for child in node.children:
        child_path = path + (str(child.key),)
        yield child, child_path
        yield from walk_with_path(child, child_path)


def referenced_values(node):
    for child, path in walk_with_path(node):
        field = child.key.split("@", 1)[0]
        values = []
        if field in WEAPON_REF_FIELDS and child.value:
            values.append(child.value)
        elif field in WEAPON_REF_MAP_FIELDS:
            if child.value:
                values.append(child.value)
            values.extend(
                descendant.value for descendant, _ in walk_with_path(child)
                if descendant.value)
        for raw in values:
            for value in str(raw).split(","):
                if value.strip():
                    yield value.strip(), "/".join(path)


def resolved_referrer_index(rules):
    """Resolve every actor/weapon once and index its weapon references."""
    rows = {}
    for kind, definitions, resolver in (
            ("actor", rules.actors, rules.resolve),
            ("weapon", rules.weapons, rules.resolve_weapon)):
        for name in sorted(definitions):
            if name.startswith("^"):
                continue
            resolved = resolver(name)
            if resolved is None:
                continue
            for value, path in referenced_values(resolved):
                if kind == "weapon" and name.lower() == value.lower():
                    continue
                rows.setdefault(value.lower(), []).append(
                    {"kind": kind, "name": name, "path": path})
    return {
        name: sorted(referrers, key=lambda row: (
            row["kind"], row["name"], row["path"]))
        for name, referrers in rows.items()
    }


def resolved_referrers(rules, weapon_name: str):
    return resolved_referrer_index(rules).get(weapon_name.lower(), [])


@functools.lru_cache(maxsize=1)
def load_manifest():
    data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    if data.get("schema_version") != 1 or not isinstance(data.get("entries"), dict):
        raise ValueError("unsupported intentional-composite manifest schema")
    return data


@functools.lru_cache(maxsize=1)
def reviewed_fingerprints():
    return {
        name: tuple(entry["mains"])
        for name, entry in load_manifest()["entries"].items()
    }


def intentional_composite(name: str, mains: list[str]) -> bool:
    return reviewed_fingerprints().get(name) == tuple(sorted(mains))


_VALIDATED_PREDICATES = {}


def validated_reviewed_predicate(rules, main_nodes: Callable):
    """Return a review predicate only after all live decision evidence validates."""
    key = id(rules)
    cached = _VALIDATED_PREDICATES.get(key)
    if cached is not None and cached[0] is rules:
        return cached[1]
    errors = validate_manifest(rules, main_nodes)
    if errors:
        raise ValueError(
            "intentional composite registry is stale or invalid:\n- "
            + "\n- ".join(errors))
    fingerprints = reviewed_fingerprints()

    def predicate(name: str, mains: list[str]) -> bool:
        return fingerprints.get(name) == tuple(sorted(mains))

    _VALIDATED_PREDICATES[key] = (rules, predicate)
    return predicate


def clear_validation_cache() -> None:
    """Test helper: force the next consumer to revalidate the live registry."""
    _VALIDATED_PREDICATES.clear()


CATEGORY_RATIONALES = {
    "staged superweapon": (
        "retains separately authored damage or effect stages whose timing, radius, "
        "or damage family makes each stage part of one superweapon impact"
    ),
    "status payload": (
        "retains its primary delivery damage and separately authored status or "
        "special-damage payload"
    ),
    "target-routed composite": (
        "retains distinct target, relationship, armor, or physical-state routes "
        "rather than treating the mains as interchangeable damage copies"
    ),
    "effect-delivery composite": (
        "retains a conventional impact together with a separately authored energy "
        "or Waveforce effect payload delivered on the same shot"
    ),
}

CATEGORY_REFERENCES = {
    "staged superweapon": "Resolved family review: authored stage timing and geometry",
    "status payload": "Resolved family review: primary hit plus status payload",
    "target-routed composite": "Resolved family review: target and armor routing",
    "effect-delivery composite": "Resolved family review: overlapping effect delivery",
}

CATEGORY_OVERLAP = {
    "staged superweapon": (
        "Any shared target route is intentional because the separately authored "
        "timing, radius, or damage family makes this a staged impact."
    ),
    "status payload": (
        "Overlap on the struck target is intentional: the delivery hit and status "
        "payload are both meant to apply."
    ),
    "target-routed composite": (
        "This is not a blanket overlap waiver; the exact components are retained "
        "for their distinct target, relationship, armor, or state routes."
    ),
    "effect-delivery composite": (
        "Overlap is intentional: the special energy or Waveforce payload is "
        "delivered together with the conventional impact."
    ),
}


def component_purpose(category: str, main: str) -> str:
    if category == "staged superweapon":
        return f"authored superweapon stage {main} with independently pinned behavior"
    if category == "target-routed composite":
        return f"authored target, armor, relationship, or state route {main}"
    special = (
        "tesla", "cryo", "chem", "nuclear", "sonic", "inferno",
        "temperature", "ioncannon", "railgun", "waveforce", "quantum",
    )
    if any(token in main.lower() for token in special):
        return f"special energy, status, or effect payload {main}"
    return f"primary conventional delivery or impact {main}"


def curated_decisions() -> dict[str, dict[str, object]]:
    """Expand the exact review decisions and reject overlaps or malformed groups."""
    decisions = {}
    for category, groups in DECISION_GROUPS.items():
        if category not in CATEGORY_RATIONALES:
            raise ValueError(f"missing rationale for category {category}")
        for names, mains in groups:
            expected_mains = tuple(sorted(mains))
            if len(expected_mains) < 2 or len(set(expected_mains)) != len(expected_mains):
                raise ValueError(f"invalid mains in {category}: {mains}")
            for name in names:
                if name in decisions:
                    raise ValueError(f"duplicate reviewed weapon {name}")
                decisions[name] = {
                    "category": category,
                    "component_purposes": {
                        main: component_purpose(category, main)
                        for main in expected_mains
                    },
                    "mains": list(expected_mains),
                    "overlap_justification": CATEGORY_OVERLAP[category],
                    "rationale": (
                        f"Exact family decision for {', '.join(names)}: "
                        f"{CATEGORY_RATIONALES[category]}; reviewed components are "
                        f"{', '.join(expected_mains)}."
                    ),
                    "review_reference": CATEGORY_REFERENCES[category],
                }
    return decisions


def live_snapshot(rules, name: str, main_nodes: Callable, referrer_index=None):
    resolved = rules.resolve_weapon(name)
    if resolved is None:
        raise ValueError(f"missing weapon {name}")
    mains = sorted(main_nodes(resolved), key=lambda node: node.key)
    if referrer_index is None:
        referrer_index = resolved_referrer_index(rules)
    referrers = referrer_index.get(name.lower(), [])
    return {
        "main_digest": digest([node_payload(node) for node in mains]),
        "mains": sorted(node.key.replace("Warhead@", "") for node in mains),
        "referrer_digest": digest(referrers),
        "referrers": referrers,
        "weapon_digest": digest(node_payload(resolved)),
    }


def validate_manifest(rules, main_nodes: Callable) -> list[str]:
    from survey_weapon_structure import inventory

    data = load_manifest()
    errors = []
    referrer_index = resolved_referrer_index(rules)
    curated = curated_decisions()
    if set(data["entries"]) != set(curated):
        errors.append(
            "reviewed name set differs from curated decisions: "
            f"missing={sorted(set(curated) - set(data['entries']))}, "
            f"extra={sorted(set(data['entries']) - set(curated))}")
    raw = inventory(rules, reviewed_predicate=lambda _name, _mains: False)
    reachability = {}
    for name in raw["sets"]["direct_actor_armament"]:
        reachability[name] = "direct"
    for name in raw["sets"]["indirect_weapon_graph"]:
        reachability[name] = "indirect"
    for name in raw["sets"]["unreached"]:
        reachability[name] = "unreached"
    required = {
        "category", "component_purposes", "expected_reachability",
        "main_digest", "mains", "rationale", "referrer_digest",
        "referrers", "review_reference", "overlap_justification",
        "weapon_digest",
    }
    for name, entry in sorted(data["entries"].items()):
        missing = required - set(entry)
        extra = set(entry) - required
        if missing or extra:
            errors.append(
                f"{name}: schema mismatch missing={sorted(missing)} extra={sorted(extra)}")
            continue
        if name.startswith("^") or rules.weapon(name) is None:
            errors.append(f"{name}: reviewed entry must be one concrete weapon")
            continue
        decision = curated.get(name)
        if decision is None:
            continue
        for field in (
                "category", "component_purposes", "mains", "rationale",
                "review_reference", "overlap_justification"):
            if entry[field] != decision[field]:
                errors.append(f"{name}: {field} differs from curated decision")
        if entry["expected_reachability"] != reachability.get(name):
            errors.append(f"{name}: stale expected_reachability")
        if set(entry["component_purposes"]) != set(entry["mains"]):
            errors.append(f"{name}: every main needs exactly one declared purpose")
        live = live_snapshot(rules, name, main_nodes, referrer_index)
        for field in ("mains", "main_digest", "weapon_digest",
                      "referrers", "referrer_digest"):
            if entry[field] != live[field]:
                errors.append(f"{name}: stale {field}")
    return errors


def generated_manifest(rules, main_nodes: Callable) -> dict[str, object]:
    """Build a manifest only when every curated decision matches current rules."""
    from survey_weapon_structure import inventory

    decisions = curated_decisions()
    referrer_index = resolved_referrer_index(rules)
    raw = inventory(rules, reviewed_predicate=lambda _name, _mains: False)
    reachability = {}
    for name in raw["sets"]["direct_actor_armament"]:
        reachability[name] = "direct"
    for name in raw["sets"]["indirect_weapon_graph"]:
        reachability[name] = "indirect"
    for name in raw["sets"]["unreached"]:
        reachability[name] = "unreached"

    entries = {}
    for name, decision in sorted(decisions.items()):
        if name not in reachability:
            raise ValueError(f"{name}: curated weapon is not a current raw stack")
        if reachability[name] == "unreached":
            raise ValueError(f"{name}: curated decision must be reachable")
        snapshot = live_snapshot(rules, name, main_nodes, referrer_index)
        if snapshot["mains"] != decision["mains"]:
            raise ValueError(
                f"{name}: curated mains {decision['mains']} do not match "
                f"resolved mains {snapshot['mains']}")
        entries[name] = {
            **decision,
            "expected_reachability": reachability[name],
            **snapshot,
        }
    return {"entries": entries, "schema_version": 1}


def main() -> int:
    parser = argparse.ArgumentParser()
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--snapshot", action="store_true")
    mode.add_argument("--write", action="store_true")
    args = parser.parse_args()

    from audit_three_way_split import main_warhead_nodes
    from miniyaml import Ruleset

    rules = Ruleset(ROOT)
    if args.write:
        data = generated_manifest(rules, main_warhead_nodes)
        MANIFEST.parent.mkdir(parents=True, exist_ok=True)
        MANIFEST.write_text(
            json.dumps(data, indent=2, sort_keys=True) + "\n",
            encoding="utf-8",
        )
        print(f"wrote {MANIFEST.relative_to(ROOT)} ({len(data['entries'])} entries)")
        return 0
    data = load_manifest()
    if args.snapshot:
        referrer_index = resolved_referrer_index(rules)
        rows = {
            name: live_snapshot(rules, name, main_warhead_nodes, referrer_index)
            for name in sorted(data["entries"])
        }
        print(json.dumps(rows, indent=2, sort_keys=True))
        return 0
    errors = validate_manifest(rules, main_warhead_nodes)
    if errors:
        print("FAIL intentional composite manifest")
        for error in errors:
            print(f"- {error}")
        return 1
    print(f"PASS {len(data['entries'])} intentional composite fingerprints")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
