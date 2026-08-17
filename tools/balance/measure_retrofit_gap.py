#!/usr/bin/env python3
"""How far is each legacy template's inline `Versus` ladder from its target family?

This is the number that decides HOW a retrofit is done, not just whether. Toxic's
legacy ladder was **6.26x** below the family profile, so repointing it without
rescaling `Damage` would have made every gas cloud six times as lethal. A template
sitting at ~1.0x can be repointed and left for `apply_balance` to re-price; one
sitting far off has to be paid for in the same commit.

    python tools/balance/measure_retrofit_gap.py
    python tools/balance/measure_retrofit_gap.py --json docs/audit/latest/retrofit_gap.json

Two numbers per template:

- **mean ratio** = mean(new Versus) / mean(old Versus) over the armors BOTH declare.
  This is the DPS multiplier a naive repoint would apply. `Damage` must be divided
  by it to preserve resolved behaviour (CLAUDE.md rule 5).
- **rank correlation** (Spearman) between the two ladders. This is the *shape* check
  and it is the one that catches a wrong family: a cannon repointed to the anti-light
  family instead of the anti-heavy one still has a plausible mean ratio, but its
  correlation goes NEGATIVE. Used here to pick HE vs AP from the data instead of
  from the template's name.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import statistics
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
import miniyaml  # noqa: E402

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

# Neither of these is a rung on the armor ladder, and both would corrupt the ratio.
#
# `HAZMAT` is a damage-type immunity flag pinned at a flat 50 in every family template,
# so including it drags every comparison toward 50.
#
# `Shield` is the W21 health LAYER, and the family profiles deliberately push it much
# higher than the legacy ladders did (Melee: 125 -> 200). Letting that one cell into the
# mean makes the rescale over-pay: on `^SwordWeapon` it divided `Damage` by enough to
# drop every REAL armor 4-8% while the tree-wide statistic looked preserved. Shield
# applies only to shielded targets and must not set the damage of everything else.
IGNORE = {"HAZMAT", "Shield"}

# Where each legacy template is headed. Level comes from `docs/balance/weapon_classes.yaml`
# (the Tier<->WeaponClass law: 0.75 = Light, 1.0 = Medium, 1.25 = Heavy, 1.5 = Super).
# `None` = the family is settled but the level/variant is not, and is chosen by
# correlation against the candidates listed in AMBIGUOUS.
MAPPING = {
    "^SmallArms": "Bullet_Light",
    "^Chaingun": "Bullet_Medium",
    "^ShrapnelWeapon": "Concussion_Medium",
    "^HeavyBomb": "Demolition_Heavy",
    "^FlakWeapon": "Flak_Medium",
    "^HeavyAAWeapon": "MissileAA_Heavy",
    "^TankDestroyerCannon": "CannonAP_Light",
    "^MediumCannon": "CannonHE_Medium",
    "^HeavyCannon": "CannonHE_Heavy",
    "^LaserWeapon": "Laser_Heavy",
    "^RailgunWeapon": "Railgun_Heavy",
    "^TeslaWeapon": "Tesla_Heavy",
    # `docs/balance/weapon_classes.yaml` calls this `^Warhead_TeslaCharged_Super`, but no
    # such template exists — the generator emits `^Warhead_Tesla_Super`. Artifact wins.
    "^TeslaChargedWeapon": "Tesla_Super",
    "^MediumFlameWeapon": "Flame_Medium",
    "^HeavyFlameWeapon": "Flame_Heavy",
    "^LightChemicalWeapon": "Chemical_Light",
    "^MediumChemicalWeapon": "Chemical_Medium",
    "^HeavyChemicalWeapon": "Chemical_Heavy",
    "^SwordWeapon": "Melee_Medium",
    "^ArrowWeapon": "Arrow_Light",
}

# Families whose profile is a DELIBERATE exception, so the shape check cannot judge them
# and a mechanical repoint would silently ship a design change. Both are listed in
# DESIGN.md §12 as exceptions; they need a maintainer ruling, not a script.
#
# `Magic`  — the target profile is FLAT (32 vs every armor: the %-equalizer). The legacy
#            template is a 140->40 ladder, so converting REMOVES its armor discrimination.
# `Nuclear`— HAND_TUNED and deliberately re-ordered to BLD > VEH > AIR > INF; the legacy
#            ladder is anti-heavy (Superheavy 100, Wood 56). Repointing re-roles it.
EXCEPTIONS = {
    "^MagicWeapon": ("Magic_Heavy", "target profile is FLAT (32 vs all) — the %-equalizer "
                                    "design; converting drops the legacy ladder entirely"),
    "^NuclearWarhead": ("Nuclear_Super", "family is hand-tuned to BLD>VEH>AIR>INF; the "
                                         "legacy ladder is anti-heavy, so this re-roles it"),
    # W2 in BALANCE_PROGRAM_PLAN.md carries an explicit maintainer mapping that splits
    # this template across FOUR families (FireMissile / Thermobaric / Laser / Inferno)
    # per weapon — a mechanical one-family repoint would overrule that order. It also
    # still has the P1 `Range: 500` single-value bug (a 1-length effectiveRange makes
    # GetDamageFalloff return 0, so its weapons deal NO damage), and carries
    # ApplyPhysicalState + GroundFire warheads that a warhead-layer repoint doesn't touch.
    "^LightFlameWeapon": ("Flame_Light", "W2 owns it: maintainer split it across four "
                                         "families per weapon, and its Range:500 bug is "
                                         "an open P1"),
}

# Family settled, variant decided by SHAPE (correlation), not by the template's name.
AMBIGUOUS = {
    "^Grenade": ["Concussion_Light", "Demolition_Light"],
    "^LightMissile": ["MissileHE_Light", "MissileAP_Light"],
    "^MediumMissile": ["MissileHE_Medium", "MissileAP_Medium"],
    "^HeavyMissile": ["MissileHE_Heavy", "MissileAP_Heavy"],
    "^MissileWeapon": ["MissileHE_Medium", "MissileAP_Medium"],
}


def versus_of(node) -> dict[str, int] | None:
    """The main damage warhead's `Versus` table, or None if it declares none.

    The main warhead is the first non-percentage, non-friendly-fire `Warhead@*` that
    carries a `Versus` block — percentage twins and FF twins have their own ladders
    and are retrofitted from the main one, never measured against it.
    """
    for c in node.children:
        if not (c.key == "Warhead" or c.key.startswith("Warhead@")):
            continue
        low = c.key.lower()
        if "percentage" in low or "friendlyfire" in low:
            continue
        v = c.child("Versus")
        if v is None:
            continue
        out = {}
        for a in v.children:
            try:
                out[a.key] = int((a.value or "").strip())
            except ValueError:
                pass
        if out:
            return out
    return None


def spearman(a: list[float], b: list[float]) -> float:
    """Rank correlation. Ties get averaged ranks, which matters because the legacy
    ladders were hand-written and repeat values freely."""
    def rank(xs):
        order = sorted(range(len(xs)), key=lambda i: xs[i])
        r = [0.0] * len(xs)
        i = 0
        while i < len(order):
            j = i
            while j + 1 < len(order) and xs[order[j + 1]] == xs[order[i]]:
                j += 1
            avg = (i + j) / 2 + 1
            for k in range(i, j + 1):
                r[order[k]] = avg
            i = j + 1
        return r
    ra, rb = rank(a), rank(b)
    n = len(a)
    if n < 3:
        return 0.0
    ma, mb = statistics.mean(ra), statistics.mean(rb)
    num = sum((x - ma) * (y - mb) for x, y in zip(ra, rb))
    den = (sum((x - ma) ** 2 for x in ra) * sum((y - mb) ** 2 for y in rb)) ** 0.5
    return num / den if den else 0.0


def compare(old: dict[str, int], new: dict[str, int]) -> dict:
    """Compare the ladders the ENGINE actually applies.

    An armor with no `Versus` row is not absent — it resolves to **100**, full damage.
    Comparing only the rows both tables happen to declare therefore measures the wrong
    thing whenever one side is incomplete: `^MissileWeapon` declares just 6 rows, so its
    real ladder is those 6 plus 100 against the other twelve. Scoring it on the 6 would
    hide the largest part of the change.
    """
    armors = sorted((set(old) | set(new)) - IGNORE)
    o = [old.get(k, 100) for k in armors]
    n = [new.get(k, 100) for k in armors]
    shared = armors
    return {
        "armors": len(shared),
        "old_mean": round(statistics.mean(o), 2),
        "new_mean": round(statistics.mean(n), 2),
        "ratio": round(statistics.mean(n) / statistics.mean(o), 3),
        "corr": round(spearman(o, n), 3),
    }


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--json", type=pathlib.Path, help="also write the raw table here")
    args = ap.parse_args()

    rules = miniyaml.Ruleset(ROOT)
    rows = []
    for legacy in list(MAPPING) + list(AMBIGUOUS) + list(EXCEPTIONS):
        node = rules.weapons.get(legacy)
        if node is None:
            rows.append({"legacy": legacy, "error": "template not found"})
            continue
        old = versus_of(node)
        if not old:
            rows.append({"legacy": legacy, "error": "no inline Versus"})
            continue

        if legacy in EXCEPTIONS:
            candidates = [EXCEPTIONS[legacy][0]]
        else:
            candidates = AMBIGUOUS.get(legacy) or [MAPPING[legacy]]
        scored = []
        for cand in candidates:
            fam = rules.weapons.get(f"^Warhead_{cand}")
            if fam is None:
                continue
            new = versus_of(fam)
            if not new:
                continue
            scored.append((cand, compare(old, new)))
        if not scored:
            rows.append({"legacy": legacy, "error": "no target family found"})
            continue
        # Shape wins: the variant whose ladder ORDERS the armors most like the legacy one.
        scored.sort(key=lambda s: -s[1]["corr"])
        best, stats = scored[0]
        row = {"legacy": legacy, "target": f"^Warhead_{best}", **stats}
        if len(scored) > 1:
            row["runner_up"] = f"{scored[1][0]} (corr {scored[1][1]['corr']})"
        if legacy in EXCEPTIONS:
            row["excluded"] = EXCEPTIONS[legacy][1]
        rows.append(row)

    rows.sort(key=lambda r: -abs(r.get("ratio", 1) - 1))
    print(f"{'legacy template':26s} {'-> family':28s} {'ratio':>7s} {'corr':>7s}  armors")
    print("-" * 82)
    for r in rows:
        if "error" in r:
            print(f"{r['legacy']:26s} {'!! ' + r['error']}")
            continue
        flag = "  <-- EXCLUDED" if "excluded" in r else ""
        warn = "  !! SHAPE" if r["corr"] < 0.5 and "excluded" not in r else ""
        print(f"{r['legacy']:26s} {r['target']:28s} {r['ratio']:7.3f} {r['corr']:7.3f}  "
              f"{r['armors']:2d}{flag}{warn}")
        if "runner_up" in r:
            print(f"{'':26s}   runner-up: {r['runner_up']}")
        if "excluded" in r:
            print(f"{'':26s}   {r['excluded']}")

    ok = [r for r in rows if "ratio" in r and "excluded" not in r]
    print("-" * 82)
    print(f"{len(ok)} templates convertible, {len(EXCEPTIONS)} excluded by design; "
          f"{sum(1 for r in ok if abs(r['ratio'] - 1) > 0.05)} need a Damage rescale "
          f"(median {statistics.median([r['ratio'] for r in ok]):.3f}x), "
          f"{sum(1 for r in ok if r['corr'] < 0.5)} questionable shapes")
    if args.json:
        out = args.json if args.json.is_absolute() else ROOT / args.json
        out.write_text(json.dumps(rows, indent=2), encoding="utf-8")
        print(f"wrote {out.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
