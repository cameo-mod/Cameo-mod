#!/usr/bin/env python3
"""plan_warhead_collapse.py — W24: propose ONE damage family per multi-warhead weapon.

Maintainer 2026-08-17: *"the many inherits were funny and intentional at the time they were made
but now we want to have a cleaner design with only one inherit for warheads. Try to find the best
fitting one."*

This writes a REVIEW TABLE and never touches yaml. The family choice is a design judgment, so it
has to be reviewable at a glance — which it is, because the question is not "which of 15 legacy
templates wins" but **"what IS this weapon?"**, and a weapon's name, projectile and firing sound
answer that directly. `wc2dragonFireVisible` fires a Missile projectile with `wc2_axe.aud`, is
named "dragon fire", and pulls in 15 templates: it is a FLAME weapon that accumulated copy-paste.

Confidence is reported per weapon so review effort goes where it is needed:

  HIGH   — already inherits exactly one `^Warhead_<Family>_<Level>`; keep it, drop the rest.
  NAME   — the weapon's own name names the family (fire/flame, laser, tesla, cannon, ...).
  LEGACY — no name signal; inferred from the legacy templates, weighted by how specific each is.
  NONE   — nothing decides it. These are the only ones needing a human ruling.

⭐ `--impact` ADDS THE HALF THAT WAS MISSING, and the Hydralisk is why. Naming the right family
is only half a collapse plan; the other half is *what the collapse does to resolved damage*, which
nothing measured. `HydraSpit` carried four mains at an identical 18,000 with four DIFFERENT `Versus`
ladders, so preserving the numeric sum multiplied mean effective damage by 1.46x and moved
individual armors 0.52x-2.78x. `review_resolve_diff.py` certified it as neutral because its own
docstring says new-template `Versus` tables are *"NOT flagged"* (docs/design/W24_COLLAPSE_REVIEW.md).

So `--impact` resolves the PROPOSED family's profile and prints, per weapon:

  shape   BROADCAST      every main shares a damage AND a profile -> the sum really is neutral
          PILEUP         same damage, DIFFERENT profiles -> the sum is not neutral; this is the
                         Hydralisk shape, and the one the naming half cannot see
          MIXED          mains differ in damage too -> a real multi-warhead design, read it
  mean    mean effective damage after / before, over the real armor rows
  min/max the worst and best per-armor ratio, which is what a player actually feels

⚠ HAZMAT and Shield are excluded from every mean here, for the reasons `measure_retrofit_gap.py`
already records: HAZMAT is a flat-50 immunity flag in every family, Shield is the W21 health layer,
and either one in the mean makes the comparison lie.

⚠ The LEVEL is read from the weapon's own legacy templates (`^Light*` -> Light, `^Heavy*` -> Heavy,
else Medium). It is a guess where the templates disagree, and the column says so rather than
hiding it.

Usage:
  python tools/balance/plan_warhead_collapse.py                 # full table -> stdout
  python tools/balance/plan_warhead_collapse.py --unresolved    # only what needs a ruling
  python tools/balance/plan_warhead_collapse.py --impact        # + the resolved per-armor impact
  python tools/balance/plan_warhead_collapse.py --impact --risky # only collapses that move damage
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

from audit_three_way_split import main_warhead_nodes  # noqa: E402
from miniyaml import Ruleset  # noqa: E402
import percentage_damage as pd  # noqa: E402
import statistics  # noqa: E402

# Neither is a rung on the armor ladder and both corrupt a mean — the same exclusion
# `measure_retrofit_gap.py` documents.
IGNORE_ARMORS = {"HAZMAT", "Shield"}

FAMILY_TPL = re.compile(r"^\^Warhead_([A-Za-z]+)_(\w+)$")

# Name tokens -> family. Ordered: the FIRST match wins, so put specific before generic
# ("chaingun" before "gun", "flamethrower" before "flame").
#
# ⚠ `Inferno` is deliberately NOT reachable from a name token. It is not "a fancy flame": the
# board defines it as ("Prism", "Temperature") — a PRISM CHASSIS THAT BURNS, i.e. a heat ray
# (`HeatRayBeam1/2`). Its ladder is FLAT (Scout 121 .. Helicopter 76, span 1.6x) where Flame's
# is SHARP (None 200 .. Concrete 92, span 2.2x), and its Shield row is 263 against Flame's
# 187-203 because it couples to shields as part-energy. A dragon's breath is fuel combustion
# and wants the sharp anti-infantry ladder: Flame. Route a weapon to Inferno by hand, or via an
# explicit `^Warhead_Inferno_*` inherit it already has.
# ⚠⚠ TWO TIERS, AND THE ORDER IS THE WHOLE POINT. A first version had one flat list with
# delivery words mixed in, and it mis-assigned weapons whose name carries BOTH:
#
#   japan_imperialscoutsman_rifle_waveforce -> matched 'rifle'  -> Bullet   (must be Waveforce)
#   ArmoredCarMGWaveforce                   -> matched 'mg'     -> Bullet   (must be Waveforce)
#
# A FAMILY name is a statement about what the weapon does; a delivery word (rifle, cannon, mg)
# only says how it is mounted. So every family token is tried before any delivery token.
NAME_FAMILY_SPECIFIC = [
    ("waveforce", "Waveforce"), ("quantum", "Quantum"), ("plasma", "Plasma"),
    ("prism", "Prism"), ("railgun", "Railgun"),
    ("tesla", "Tesla"), ("lightning", "Tesla"), ("emp", "Tesla"),
    ("laser", "Laser"),
    ("sonic", "Sonic"), ("resonan", "Sonic"),
    ("magic", "Magic"), ("spell", "Magic"), ("arcane", "Magic"),
    ("nuke", "Nuclear"), ("nuclear", "Nuclear"), ("atomic", "Nuclear"),
    ("thermobaric", "Thermobaric"),
    ("cryo", "Cryo"), ("freeze", "Cryo"), ("frost", "Cryo"),
    ("toxic", "Toxic"), ("anthrax", "Toxic"), ("virus", "Toxic"), ("poison", "Toxic"),
    ("chem", "Chemical"), ("acid", "Chemical"), ("corros", "Chemical"),
    ("flamethrow", "Flame"), ("fireball", "Flame"), ("flamer", "Flame"),
    ("napalm", "Flame"), ("incend", "Flame"), ("dragonfire", "Flame"),
    ("flame", "Flame"),
    ("concuss", "Concussion"), ("shrapnel", "Concussion"),
    ("demo", "Demolition"), ("satchel", "Demolition"),
    ("flak", "Flak"),
    ("arrow", "Arrow"), ("catapult", "Arrow"),
    ("sniper", "Sniper"),
]

# Delivery / mounting words. Only consulted when no family token matched.
NAME_FAMILY_GENERIC = [
    ("chaingun", "Bullet"), ("gatling", "Bullet"), ("minigun", "Bullet"),
    ("machinegun", "Bullet"), ("smallarms", "Bullet"), ("shotgun", "Bullet"),
    ("rifle", "Bullet"), ("pistol", "Bullet"), ("mg", "Bullet"),
    ("dragon", "Flame"), ("fire", "Flame"), ("burn", "Flame"),
    ("beam", "Laser"),
    ("grenade", "Concussion"), ("artillery", "Concussion"),
    ("mortar", "Concussion"), ("howitzer", "Concussion"),
    ("melee", "Melee"), ("sword", "Melee"), ("claw", "Melee"), ("axe", "Melee"),
    ("bite", "Melee"), ("punch", "Melee"),
    ("c4", "Demolition"), ("bomb", "Demolition"),
    ("torpedo", "MissileAP"),
    ("cannon", "CannonHE"), ("shell", "CannonHE"),
    ("missile", "MissileHE"), ("rocket", "MissileHE"), ("scud", "MissileHE"),
]

# Decisions already taken, where the NAME is actively misleading. Documented, not guessed.
EXPLICIT = {
    # A photon cannon is not a cannon (memory `cameo-live-dead-weapon-files` / W24 notes):
    # GladiusCannon inherits PhotonCannon and is a Plasma weapon.
    "GladiusCannon": ("Plasma", "photon cannon — Plasma, not a shell-firing cannon"),
}

# Legacy template -> family, for weapons whose NAME says nothing. Specificity weight: a
# template naming one clear family outranks a vague one, so a pileup still resolves.
LEGACY_FAMILY = {
    "^SmallArms": ("Bullet", 3), "^Chaingun": ("Bullet", 3), "^RALightMG": ("Bullet", 2),
    "^RAHeavyMG": ("Bullet", 2),
    "^LaserWeapon": ("Laser", 4), "^TeslaWeapon": ("Tesla", 4),
    "^TeslaChargedWeapon": ("Tesla", 4), "^TSRailgun": ("Railgun", 4),
    "^MagicWeapon": ("Magic", 4), "^SonicWeapon": ("Sonic", 4),
    "^LightFlameWeapon": ("Flame", 4), "^MediumFlameWeapon": ("Flame", 4),
    "^HeavyFlameWeapon": ("Flame", 4),
    "^LightChemicalWeapon": ("Chemical", 4), "^MediumChemicalWeapon": ("Chemical", 4),
    "^HeavyChemicalWeapon": ("Chemical", 4),
    "^NuclearWarhead": ("Nuclear", 5),
    "^TankDestroyerCannon": ("CannonAP", 3), "^MediumCannon": ("CannonHE", 2),
    "^HeavyCannon": ("CannonHE", 2), "^AACannon": ("Flak", 3),
    "^FlakWeapon": ("Flak", 3),
    "^LightMissile": ("MissileHE", 2), "^MediumMissile": ("MissileHE", 2),
    "^HeavyMissile": ("MissileHE", 2), "^HeavyAAWeapon": ("MissileAA", 3),
    "^Grenade": ("Concussion", 2), "^ShrapnelWeapon": ("Concussion", 2),
    "^HeavyBomb": ("Demolition", 2), "^Artillery": ("Concussion", 2),
    "^TSArtilleryWeapon": ("Concussion", 2),
}


def _level_from(legacy, families):
    """Light / Medium / Heavy for the target family, and whether the sources agreed."""
    votes = []
    for name in list(legacy) + list(families):
        low = name.lower()
        if "light" in low:
            votes.append("Light")
        elif "heavy" in low or "super" in low:
            votes.append("Heavy")
        elif "medium" in low:
            votes.append("Medium")
    if not votes:
        return "Medium", False
    top = collections.Counter(votes).most_common()
    return top[0][0], len(set(votes)) == 1


def _family_profile(rs, family, level, _cache={}):
    """The target family's Versus table at one level, or None if it has no such rung."""
    key = (family, level)
    if key in _cache:
        return _cache[key]
    node = rs.resolve_weapon(f"^Warhead_{family}_{level}")
    table = None
    if node is not None:
        for c in node.children:
            if c.key.split("@", 1)[0] == "Warhead":
                t = pd.versus_table(c)
                if t:
                    table = t
                    break
    _cache[key] = table
    return table


def collapse_impact(rs, main_nodes, family, level):
    """What preserving the numeric sum does to RESOLVED per-armor damage.

    Returns (shape, mean_ratio, min_ratio, max_ratio) or None when the target family has no
    rung at this level — reported as unknown rather than guessed, because a missing rung is a
    fact about the family, not about this weapon.
    """
    target = _family_profile(rs, family, level) if family else None
    if target is None:
        return None
    parts = []
    for n in main_nodes:
        try:
            dmg = int(str(n.get("Damage")).strip())
        except (TypeError, ValueError):
            continue
        parts.append((dmg, pd.versus_table(n)))
    if len(parts) < 2:
        return None

    armors = sorted({a for _d, v in parts for a in v} | set(target))
    armors = [a for a in armors if a not in IGNORE_ARMORS]
    if not armors:
        return None

    # ⭐ THE DISTINCTION THE NAMING HALF CANNOT MAKE. Equal damage is not equal behaviour:
    # BROADCAST mains share a PROFILE too, so summing them really is neutral. PILEUP mains
    # share only the number — that is HydraSpit, and summing it moved damage 0.52x-2.78x.
    same_damage = len({d for d, _v in parts}) == 1
    same_profile = all(
        all(v.get(a, 100) == parts[0][1].get(a, 100) for a in armors) for _d, v in parts[1:])
    shape = ("BROADCAST" if same_damage and same_profile
             else "PILEUP" if same_damage else "MIXED")

    total = sum(d for d, _v in parts)
    ratios = []
    for a in armors:
        before = sum(d * v.get(a, 100) / 100.0 for d, v in parts)
        after = total * target.get(a, 100) / 100.0
        if before > 0:
            ratios.append(after / before)
    if not ratios:
        return None
    before_mean = statistics.mean(
        sum(d * v.get(a, 100) / 100.0 for d, v in parts) for a in armors)
    after_mean = statistics.mean(total * target.get(a, 100) / 100.0 for a in armors)
    return shape, after_mean / before_mean, min(ratios), max(ratios)


def analyse(rs):
    fired = collections.Counter()
    for name in rs.actors:
        if name.startswith("^"):
            continue
        node = rs.resolve(name)
        if node is None:
            continue
        for c in node.children:
            if c.key == "Armament" or c.key.startswith("Armament@"):
                w = c.get("Weapon")
                if w:
                    fired[str(w).strip()] += 1

    rows = []
    for wname in sorted(fired):
        resolved = rs.resolve_weapon(wname)
        local = rs.weapon(wname)
        if resolved is None or local is None:
            continue
        # Use the exact same predicate as the all-concrete split audit.  This
        # table is the direct actor-armament subset of that survey, not a
        # separate flat-damage approximation with accidentally similar totals.
        main_nodes = main_warhead_nodes(resolved)
        mains = [(n.key.replace("Warhead@", ""), int(str(n.get("Damage")).strip()))
                 for n in main_nodes]
        if len(mains) < 2:
            continue

        inherits = [str(c.value).strip() for c in local.children
                    if c.key.split("@")[0] == "Inherits" and c.value]
        families = [FAMILY_TPL.match(i).group(1) for i in inherits if FAMILY_TPL.match(i)]
        legacy = [i for i in inherits if i.startswith("^") and not FAMILY_TPL.match(i)]

        proj = resolved.child("Projectile")
        proj_type = str(proj.value) if proj is not None else ""
        report = str(resolved.get("Report") or "")

        family = None
        confidence = "NONE"
        why = ""
        if wname in EXPLICIT:
            family, reason = EXPLICIT[wname]
            confidence, why = "EXPLICIT", reason
        elif len(set(families)) == 1:
            family, confidence = families[0], "HIGH"
            why = f"already inherits ^Warhead_{family}_*"
        else:
            low = wname.lower()
            for tier, label in ((NAME_FAMILY_SPECIFIC, "family"),
                                (NAME_FAMILY_GENERIC, "delivery")):
                for token, fam in tier:
                    if token in low:
                        family, confidence = fam, "NAME"
                        why = f"{label} word '{token}'"
                        break
                if family:
                    break
        if family is None and legacy:
            scored = collections.Counter()
            for lg in legacy:
                if lg in LEGACY_FAMILY:
                    fam, weight = LEGACY_FAMILY[lg]
                    scored[fam] += weight
            if scored:
                top = scored.most_common()
                # A tie is not a decision — say so rather than picking alphabetically.
                if len(top) > 1 and top[0][1] == top[1][1]:
                    confidence = "NONE"
                    why = f"legacy tie: {top[0][0]}/{top[1][0]} both {top[0][1]}"
                else:
                    family, confidence = top[0][0], "LEGACY"
                    why = f"legacy templates score {family}={top[0][1]}"

        level, level_agreed = _level_from(legacy, inherits)
        impact = collapse_impact(rs, main_nodes, family, level)
        rows.append({"weapon": wname, "uses": fired[wname], "mains": len(mains),
                     "sum": sum(b for _t, b in mains),
                     "uniform": len({b for _t, b in mains}) == 1,
                     "family": family, "confidence": confidence, "why": why,
                     "legacy": legacy, "projectile": proj_type, "report": report,
                     "level": level, "level_agreed": level_agreed, "impact": impact})
    return rows


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--unresolved", action="store_true",
                    help="only the weapons that need a human ruling")
    ap.add_argument("--impact", action="store_true",
                    help="add the RESOLVED per-armor effect of preserving the numeric sum")
    ap.add_argument("--risky", action="store_true",
                    help="with --impact: only collapses that move the mean >5%% or spread "
                         "per-armor damage more than 2x")
    args = ap.parse_args()

    rows = analyse(Ruleset(ROOT))
    by_conf = collections.Counter(r["confidence"] for r in rows)

    print("# W24 collapse plan — one damage family per weapon")
    print()
    print(f"**{len(rows)}** directly actor-armed multi-main weapons. "
          "**No yaml is touched by this tool.**")
    print()
    print("| confidence | weapons | meaning |")
    print("|---|--:|---|")
    for conf, meaning in (
            ("EXPLICIT", "a decision already taken, because the name is misleading"),
            ("HIGH", "already inherits exactly one `^Warhead_*` family — keep it, drop the rest"),
            ("NAME", "the weapon's own name names the family (family words beat delivery words)"),
            ("LEGACY", "inferred from its legacy templates, weighted by specificity"),
            ("NONE", "**needs a ruling**")):
        print(f"| {conf} | {by_conf.get(conf, 0)} | {meaning} |")
    print()
    uniform = sum(1 for r in rows if r["uniform"])
    print(f"⚠ **{uniform} of {len(rows)}** have every main at the SAME damage — a useful "
          "broadcast fingerprint, not conversion authorization. Preserving the numeric sum does "
          "not preserve armor profile, geometry, relationships, damage types, or separately "
          "rounded percentage damage; every selected cohort still needs those guards.")
    print()

    if args.impact:
        shapes = collections.Counter(
            r["impact"][0] if r["impact"] else "unmeasurable" for r in rows)
        print("## Resolved impact of preserving the numeric sum")
        print()
        print("⭐ The naming half of this plan cannot see this, and the Hydralisk is why: four "
              "mains at an identical 18,000 with four DIFFERENT `Versus` ladders, so the "
              "sum-preserving collapse multiplied mean effective damage by 1.46x and moved "
              "individual armors 0.52x-2.78x.")
        print()
        spreads = sorted(r["impact"][3] / r["impact"][2]
                         for r in rows if r["impact"] and r["impact"][2] > 0)
        means = sorted(r["impact"][1] for r in rows if r["impact"])
        if spreads:
            def q(xs, f):
                return xs[min(len(xs) - 1, int(len(xs) * f))]
            print("⛔ **AND THE HEADLINE IS NOT THE MEAN.** Across the measured weapons the mean "
                  f"is essentially preserved (median **{q(means, 0.5):.2f}x**, p25 "
                  f"{q(means, 0.25):.2f}, p75 {q(means, 0.75):.2f}) — but the PER-ARMOR SPREAD "
                  f"(max/min) has median **{q(spreads, 0.5):.2f}x** and reaches "
                  f"**{spreads[-1]:.2f}x**, with "
                  f"**{sum(1 for x in spreads if x > 2)}** weapons over 2x. Sum-preservation is "
                  "mean-neutral and matchup-destroying: every one of those weapons keeps its "
                  "average damage and changes who it beats.")
            print()
        print("| shape | weapons | meaning |")
        print("|---|--:|---|")
        for shape, meaning in (
                ("BROADCAST", "every main shares a damage AND a profile — the sum really is "
                              "neutral, and these are the safe cohort"),
                ("PILEUP", "**same damage, DIFFERENT profiles** — the sum is NOT neutral. "
                           "The Hydralisk shape"),
                ("MIXED", "mains differ in damage too — a real multi-warhead design; read it "
                          "before collapsing"),
                ("unmeasurable", "no family chosen, or that family has no rung at this level")):
            print(f"| {shape} | {shapes.get(shape, 0)} | {meaning} |")
        print()

    show = [r for r in rows if r["confidence"] == "NONE"] if args.unresolved else rows
    if args.impact and args.risky:
        show = [r for r in show if r["impact"] and
                (not 0.95 <= r["impact"][1] <= 1.05
                 or (r["impact"][2] > 0 and r["impact"][3] / r["impact"][2] > 2))]
        print(f"## The {len(show)} whose collapse moves the mean or the matchups")
    elif args.unresolved:
        print(f"## The {len(show)} needing a ruling")
    else:
        print("## Proposal, per weapon")
    print()
    if args.impact:
        def _spread(r):
            return (r["impact"][3] / r["impact"][2]
                    if r["impact"] and r["impact"][2] > 0 else 0)

        print("| weapon | uses | mains | sum | -> family | lvl | conf | shape | mean | min | max | spread |")
        print("|---|--:|--:|--:|---|---|---|---|--:|--:|--:|--:|")
        for r in sorted(show, key=lambda r: (-_spread(r), r["weapon"])):
            fam = r["family"] or "**?**"
            lvl = r["level"] + ("" if r["level_agreed"] else "?")
            if r["impact"]:
                shape, mean, lo, hi = r["impact"]
                cells = (f"{shape} | {mean:.2f} | {lo:.2f} | {hi:.2f} | "
                         f"**{_spread(r):.2f}**")
            else:
                cells = "— | — | — | — | —"
            print(f"| `{r['weapon']}` | {r['uses']} | {r['mains']} | {r['sum']:,} | {fam} | "
                  f"{lvl} | {r['confidence']} | {cells} |")
        print()
        print("⚠ `lvl` with a `?` means the weapon's own templates disagreed about the level and "
              "Medium was assumed. `mean`/`min`/`max` are resolved damage AFTER / BEFORE, over "
              "the real armor rows (HAZMAT and Shield excluded). **`spread` = max/min** — how "
              "far the collapse pulls the weapon's matchups apart, and the column to sort by: a "
              "mean of 1.00 with a spread of 4 keeps the average and rewrites every fight.")
        return 0
    print("| weapon | uses | mains | sum | -> family | conf | why | projectile |")
    print("|---|--:|--:|--:|---|---|---|---|")
    for r in sorted(show, key=lambda r: (r["confidence"] != "NONE", -r["mains"], r["weapon"])):
        fam = r["family"] or "**?**"
        print(f"| `{r['weapon']}` | {r['uses']} | {r['mains']} | {r['sum']:,} | {fam} | "
              f"{r['confidence']} | {r['why']} | {r['projectile']} |")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
