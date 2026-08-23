#!/usr/bin/env python3
"""audit_level_ladder.py — does a family's level ladder actually RISE?

    python tools/audit/audit_level_ladder.py

Light -> Medium -> Heavy -> Super is supposed to be a ladder: a heavier level hits harder.

⛔ MEASURE THE EFFECTIVE LADDER, NOT THE TEMPLATE. Every one of the 40
`^Warhead_<Family>_<Level>` templates declares the same `Damage: 2000`:

    ^Warhead_Bullet_Light    Damage 2000   Spread  67
    ^Warhead_Bullet_Medium   Damage 2000   Spread 100
    ^Warhead_Bullet_Heavy    Damage 2000   Spread 133

That uniformity is a CONVENTION, not 40 bugs -- the template carries the SHAPE (Spread, Falloff,
Versus) and the weapon carries the MAGNITUDE, set through the WeaponClass scalar (Light 0.75,
Medium 1.0, Heavy 1.25, Super 1.5, `docs/balance/weapon_classes.yaml`). A first version of this
audit ratcheted on the template and duly reported all 40 families as broken, which was measuring
the placeholder.

So the ladder that matters is the EFFECTIVE one: the median damage of the real weapons using each
rung. Measured that way it is genuinely broken in places -- `Flak` falls 32000 -> 8000 from Light
to Medium, `MissileAP` DECREASES the whole way (20000 -> 12000 -> 11000), and `Tesla` Super is
half its Heavy. A heavier level must never hit softer than a lighter one.

This gates the between-tier plan (Light -> LightMedium -> Medium -> MediumHeavy -> Heavy ->
Super): interpolating a new rung between two endpoints that are equal produces nothing, and
between two that are inverted produces nonsense. The ladder has to be sound first.

⚠ Fixing a rung is a BALANCE change: restate it through the pipeline (`extract_stats` -> ledger
-> `apply_balance --confirm`), never by hand-editing yaml.

EXIT CODE: 1 above the ratchet.
"""
from __future__ import annotations

import collections
import pathlib
import statistics
import sys

if hasattr(sys.stdout, "reconfigure"):          # Windows consoles default to cp1252
    sys.stdout.reconfigure(encoding="utf-8")

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from miniyaml import Ruleset  # noqa: E402

# Families whose EFFECTIVE ladder is flat or inverted, measured 2026-08-22. LOWER ONLY.
LADDER_BASELINE = 9

LADDER = ["Light", "Medium", "Heavy", "Super"]
MAIN_DAMAGE_TYPES = {"AreaDamage", "SpreadDamage", "HealthPercentageDamage", "TargetDamage"}
COMPANION_MARKERS = ("Percentage", "ExtraDamage", "ExtraRepair", "Concrete")
MIN_SAMPLES = 2                                  # a rung needs 2+ weapons to have a median


def rung_samples() -> dict[tuple[str, str], list[int]]:
    """{(family, level): [damage, ...]} from weapons using EXACTLY ONE main warhead.

    Single-warhead weapons only: a between-tier mix or a lore hybrid would attribute one weapon's
    damage to two rungs and blur the very ladder being measured.
    """
    rs = Ruleset(pathlib.Path("."))
    out: dict[tuple[str, str], list[int]] = collections.defaultdict(list)
    for name in sorted(rs.weapons):
        if name.startswith("^"):
            continue
        resolved = rs.resolve_weapon(name)
        if resolved is None:
            continue
        mains = []
        for wh in resolved.children:
            if not (wh.key.startswith("Warhead@") or wh.key == "Warhead"):
                continue
            if (wh.value or "").strip() not in MAIN_DAMAGE_TYPES:
                continue
            if any(m in wh.key for m in COMPANION_MARKERS):
                continue
            raw = wh.get("Damage")
            try:
                dmg = int(str(raw).strip()) if raw is not None else None
            except ValueError:
                dmg = None
            if dmg == 0:
                continue
            mains.append((wh.key.replace("Warhead@", ""), dmg))
        if len(mains) != 1 or mains[0][1] is None:
            continue
        parts = mains[0][0].split("_")
        if len(parts) > 1 and parts[-1] in LADDER:
            out[("_".join(parts[:-1]), parts[-1])].append(mains[0][1])
    return out


def main() -> int:
    samples = rung_samples()
    rows, flat, inverted, thin = [], [], [], 0

    for fam in sorted({f for f, _l in samples}):
        rungs = [(lv, statistics.median(samples[(fam, lv)]), len(samples[(fam, lv)]))
                 for lv in LADDER
                 if (fam, lv) in samples and len(samples[(fam, lv)]) >= MIN_SAMPLES]
        if len(rungs) < 2:
            thin += 1
            continue
        vals = [v for _lv, v, _n in rungs]
        if len(set(vals)) == 1:
            verdict = "FLAT"
            flat.append(fam)
        elif any(b < a for a, b in zip(vals, vals[1:])):
            verdict = "INVERTED"
            inverted.append(fam)
        else:
            verdict = "rises"
        rows.append((fam, rungs, verdict))

    broken = len(flat) + len(inverted)
    rise = sum(1 for _f, _r, v in rows if v == "rises")
    print(f"# audit_level_ladder — {len(rows)} families measurable, {broken} with a broken ladder")
    print()
    print(f"  {rise:5d}  rise correctly")
    print(f"  {len(flat):5d}  FLAT — every level deals the same damage")
    print(f"  {len(inverted):5d}  INVERTED — a heavier level hits SOFTER than a lighter one")
    print(f"  {thin:5d}  too thin to judge (fewer than 2 rungs with {MIN_SAMPLES}+ weapons)")
    print()

    print("| family | " + " | ".join(LADDER) + " | verdict |")
    print("|---|" + "---|" * (len(LADDER) + 1))
    for fam, rungs, verdict in rows:
        cells = {lv: f"{v:.0f} _(n={n})_" for lv, v, n in rungs}
        mark = {"rises": "ok", "FLAT": "**FLAT**", "INVERTED": "**INVERTED**"}[verdict]
        print(f"| {fam} | " + " | ".join(cells.get(lv, "—") for lv in LADDER) + f" | {mark} |")

    over = broken > LADDER_BASELINE
    print()
    print(f"{'FAIL' if over else 'WARN'} {broken} broken ladders (ratchet {LADDER_BASELINE})")
    if over:
        print("**A family ladder just went flat or inverted.** A heavier level must never deal "
              "less than a lighter one — restate it through the balance pipeline, never by hand.")
    else:
        print("Lower `LADDER_BASELINE` as the ladders are restated; never raise it.")
    return 1 if over else 0


if __name__ == "__main__":
    sys.exit(main())
