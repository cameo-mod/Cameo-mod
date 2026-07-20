#!/usr/bin/env python3
"""gen_weapon_template.py — generate a weapon-class template family.

ARMOR_SYSTEM.md law: a template = (armor ORDER, step). This emits the
Light/Medium/Heavy trio for one PROFILE, each a full 17-row Versus table
plus its paired HealthPercentageDamage warhead, by construction:

  main:  Shield = 100 + floor ; order[i] = 100 - i*step
  %:     Shield = %top + %floor ; order[i] = %top - i
  step 6/5/4 -> floor 10/25/40 ; %top 16/20/25 (%floor = %top-15)

Nothing is hand-typed, so every family stays law-conformant. Prints the
YAML; the caller splices it into weapons/weapons.yaml.
"""
from __future__ import annotations

# The full 17-armor ladder is 16 non-Shield types + Shield (special).
LEVELS = {  # name: (step, main_floor, pct_top)
    "Light": (6, 10, 16),
    "Medium": (5, 25, 20),
    "Heavy": (4, 40, 25),
}


def table(order16: list[str], step: int, top: int, floor: int, shield: int) -> list[tuple[str, int]]:
    rows = [("Shield", shield)]
    for i, armor in enumerate(order16):
        rows.append((armor, top - i * step))
    assert rows[-1][1] == floor, (rows[-1], floor)
    return rows


def emit_versus(rows, indent="\t\t\t", hazmat=None):
    out = []
    if hazmat is not None:
        out.append(f"{indent}HAZMAT: {hazmat}")
    for armor, val in rows:
        out.append(f"{indent}{armor}: {val}")
    return "\n".join(out)


def family(profile: str, order16: list[str], *, damage=1000,
           spreads=(400, 600, 800),
           falloffs=("100, 50, 33, 25, 20", "100, 50, 30, 18, 10", "100, 50, 25, 10, 5"),
           damage_types="Prone75Percent, TriggerProne, ExplosionDeath",
           hazmat=50, reload=25, rng=5120) -> str:
    assert len(order16) == 16, len(order16)
    blocks = []
    for li, (level, (step, mfloor, ptop)) in enumerate(LEVELS.items()):
        pfloor = ptop - 15
        main = table(order16, step, 100, mfloor, 100 + mfloor)
        pct = table(order16, 1, ptop, pfloor, ptop + pfloor)
        name = f"^{level}{profile}"
        b = [f"{name}:",
             f"\tValidTargets: Ground, Water",
             f"\tReloadDelay: {reload}",
             f"\tRange: {rng}",
             f"\tTargetActorCenter: true",
             f"\tWarhead@{level}{profile}: SpreadDamage",
             f"\t\tValidRelationships: Neutral, Enemy",
             f"\t\tValidTargets: Ground, Water",
             f"\t\tSpread: {spreads[li]}",
             f"\t\tDamage: {damage}",
             f"\t\tFalloff: {falloffs[li]}",
             f"\t\tVersus:",
             emit_versus(main, hazmat=hazmat),
             f"\t\tDamageTypes: {damage_types}",
             f"\tWarhead@{level}{profile}Percentage: HealthPercentageDamage",
             f"\t\tValidTargets: Ground, Water",
             f"\t\tSpread: {spreads[li] // 2}",
             f"\t\tDamage: 1",
             f"\t\tFalloff: {falloffs[li]}",
             f"\t\tVersus:",
             emit_versus(pct),
             f"\t\tUpdatesUnitStatistics: false"]
        blocks.append("\n".join(b))
    return "\n\n".join(blocks)


# --- the two explosion profiles (maintainer 2026-07-19) ---
DEMOLITION_ORDER = ["Wood", "Concrete", "Steel", "None", "Flak", "Plate",
                    "Heroic", "Scout", "Light", "Medium", "Heavy", "Superheavy",
                    "Fighter", "Bomber", "Helicopter", "Spaceship"]
CONCUSSION_ORDER = ["Scout", "None", "Wood", "Light", "Flak", "Concrete",
                    "Medium", "Plate", "Steel", "Heavy", "Heroic", "Superheavy",
                    "Fighter", "Bomber", "Helicopter", "Spaceship"]

if __name__ == "__main__":
    import sys
    which = sys.argv[1] if len(sys.argv) > 1 else "both"
    if which in ("demolition", "both"):
        print("###### DEMOLITION (anti-structure, soft-priority) ######")
        print(family("Demolition", DEMOLITION_ORDER))
    if which in ("concussion", "both"):
        print("\n###### CONCUSSION (universal, gentle slope) ######")
        print(family("Concussion", CONCUSSION_ORDER,
                     damage_types="Prone75Percent, TriggerProne, ExplosionDeath",
                     spreads=(500, 600, 700)))
