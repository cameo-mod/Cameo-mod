"""Audit resolved turret bindings and dynamic target priority after migration."""
from __future__ import annotations
import argparse
import json
from pathlib import Path
from miniyaml import Ruleset


def audit_actor(actor):
    errors = []
    turrets = {t.get("Turret") or "primary" for t in actor.children_named("Turreted")}
    for auto in actor.children_named("AutoTarget"):
        if auto.get("DynamicWeaponPriority") != "true":
            errors.append(f"{actor.key}: {auto.key} lacks dynamic weapon priority")
    attacks = actor.children_named("AttackMultiTurreted")
    if attacks and actor.children_named("AttackTurreted"):
        errors.append(f"{actor.key}: shared AttackTurreted survives migration")
    for attack in attacks:
        selected = (attack.get("Turrets") or "primary").replace(" ", "").split(",")
        if len(set(selected)) != len(selected) or not set(selected) <= turrets:
            errors.append(f"{actor.key}: duplicate or missing turret station")
        names = (attack.get("Armaments") or "primary,secondary").replace(" ", "").split(",")
        for arm in actor.children_named("Armament"):
            if (arm.get("Name") or "primary") in names and (arm.get("Turret") or "primary") not in selected:
                errors.append(f"{actor.key}: {arm.key} has no independent station")
    if len(turrets) > 1 and actor.children_named("AttackTurreted"):
        errors.append(f"{actor.key}: multi-turret actor still uses shared targeting")
    return errors


def audit_removals(actor, rules):
    inherited = {t.key for key, parent in rules.inherits_of(actor)
                 for t in rules.resolve(parent).children_named("AttackTurreted")}
    local = {t.key for t in actor.children_named("AttackTurreted")}
    return [f"{actor.key}: removes absent {trait.key[1:]}"
            for trait in actor.children if trait.key.startswith("-AttackTurreted")
            and trait.key[1:] not in inherited | local]


def audit(root):
    rules = Ruleset(root)
    errors, migrated, specialized = [], [], []
    for name in sorted(rules.actors):
        if name.startswith("^"):
            continue
        actor = rules.resolve(name)
        errors.extend(audit_actor(actor))
        if actor.children_named("AttackMultiTurreted"):
            errors.extend(audit_removals(rules.actors[name], rules))
        if actor.children_named("AttackMultiTurreted"):
            migrated.append(name)
        elif len(actor.children_named("Turreted")) > 1:
            # Charged siege tank: mutually exclusive undeployed/deployed turret,
            # not concurrent batteries. Preserve its charge behavior.
            specialized.append(name)
    return dict(errors=errors, migrated=migrated, specialized=specialized)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    result = audit(args.root)
    print(json.dumps(result, indent=2))
    return bool(result["errors"])

if __name__ == "__main__":
    raise SystemExit(main())
