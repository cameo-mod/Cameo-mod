#!/usr/bin/env python3
"""propose_warhead_family.py — the W23 retrofit plan, ranked by confidence.

    python tools/balance/propose_warhead_family.py
    python tools/balance/propose_warhead_family.py --tier 1 --json out.json

⛔ THIS IS W23, NOT W27. The board's W27 is *"move inline ``Warhead@Effect*``
nodes into ``^Effect_*`` templates"*, owned by Devin. It adds ``^Effect_``
inherits, and the §1b weapon-name coverage metric counts ``^Warhead_`` inherits —
measured, only 13.1% of the coverage-gap weapons even carry an inline effect, so
finishing W27 moves the 49.2% by roughly zero. The item that moves it is **W23**
(retrofit the legacy templates into the ``^Warhead_*`` family system, owner
Claude, set B unlocked 2026-08-15).

WHAT THIS PROPOSES
------------------
For every live weapon with no ``Inherits@wh: ^Warhead_<Family>_<Level>``, the
family it should adopt, with the evidence, in four confidence tiers:

  T1 CERTAIN   the weapon already inherits ``^Compatibility_<Family>_<Level>Flat``.
               63 of those templates exist and they are ZERO-damage placeholders
               (``Damage: 0``) whose only content is the family name — they are
               the retrofit's own breadcrumbs. The target is a direct read.
  T2 HIGH      a legacy template whose name states the family and level
               (``^HeavyCannon``, ``^LightFlameWeapon``, ``^RA2Chaingun``).
  T3 MEDIUM    no naming signal; inferred from the ``Projectile`` type and the
               warhead's own damage type. **Review each one.**
  T4 MANUAL    no signal at all. A human picks the family.

⚠ A PROPOSAL IS NOT A CONVERSION. Retrofitting is `Damage` verbatim, projectile
fields preserved, `find_empty_warhead.py = 0`, `review_resolve_diff.py` clean,
boot-gate per batch (CLAUDE.md rule 5). This tool only says WHICH family; the
conversion itself is a separate, gated edit.
"""

from __future__ import annotations

import argparse
import collections
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "rename"))

from cameo_model import Model  # noqa: E402
import gen_weapon_names as names  # noqa: E402

LEVELS = ("Light", "Medium", "Heavy", "Super", "Trace")

# T1 — the compatibility placeholders state family and level outright.
COMPAT = re.compile(r"^\^Compatibility_(?P<family>[A-Za-z]+)_(?P<level>%s)Flat$"
                    % "|".join(LEVELS))

# T2 — legacy template stems whose NAME carries the family. The level, when the
# stem does not say it, comes from a Light/Medium/Heavy prefix on the template.
LEGACY_FAMILY = {
    "chaingun": "Bullet", "smallarms": "Bullet", "minigun": "Bullet",
    "cannon": "CannonHE", "tankdestroyercannon": "CannonAP",
    "missile": "MissileHE", "rocket": "MissileHE",
    "chemicalweapon": "Chemical", "flameweapon": "Flame",
    "flakweapon": "Flak", "laserweapon": "Laser", "teslaweapon": "Tesla",
    "railgunweapon": "Railgun", "prismweapon": "Prism", "sonicweapon": "Sonic",
    "bomb": "Demolition", "grenade": "Demolition",
    "shrapnelweapon": "Concussion", "repairweapon": None, "healingweapon": None,
}
LEVEL_PREFIX = re.compile(r"^\^(Light|Medium|Heavy|Super)(?P<rest>[A-Za-z]+)$")

# T3 — last-resort inference from the projectile the weapon actually fires.
PROJECTILE_FAMILY = {
    "Bullet": "Bullet", "InstantHit": "Bullet",
    "InstantHitWithFakeBullets": "Bullet", "Missile": "MissileHE",
    "LaserZap": "Laser", "LaserZapCA": "Laser", "Railgun": "Railgun",
    "GravityBomb": "Demolition", "AreaBeam": "Sonic",
    "LightningZap": "Tesla", "RadBeam": "Chemical",
}


def existing_families(rs):
    """{(family, level)} actually defined as ^Warhead_ templates."""
    out = set()
    for name in rs.weapons:
        m = re.match(r"^\^Warhead_(?P<f>\w+?)_(?P<l>%s)$" % "|".join(LEVELS), name)
        if m:
            out.add((m.group("f"), m.group("l")))
    return out


def inherits(rs, weapon):
    node = rs.weapon(weapon)
    if node is None:
        return []
    return [(c.value or "").strip() for c in node.children if c.key.startswith("Inherits")]


def damage_level(rs, weapon):
    """Level guessed from the weapon's own peak damage, for T3/T4 only."""
    node = rs.resolve_weapon(weapon)
    best = 0
    if node is not None:
        for c in node.children:
            if c.key.split("@")[0] != "Warhead":
                continue
            d = c.get("Damage")
            if d:
                try:
                    best = max(best, abs(int(str(d).strip())))
                except ValueError:
                    pass
    if best >= 30000:
        return "Heavy"
    if best >= 8000:
        return "Medium"
    return "Light"


def propose(rs, weapon, defined):
    """(tier, family, level, evidence) for one weapon."""
    inh = inherits(rs, weapon)

    for value in inh:                                        # T1
        m = COMPAT.match(value)
        if m:
            return 1, m.group("family"), m.group("level"), value

    for value in inh:                                        # T2
        stem = value.lstrip("^")
        level = None
        pm = LEVEL_PREFIX.match(value)
        if pm:
            level = pm.group(1)
            stem = pm.group("rest")
        key = re.sub(r"^(RA2|TS|TD|D2K)", "", stem).lower()
        for token, family in LEGACY_FAMILY.items():
            if key.endswith(token):
                if family is None:
                    return 4, None, None, f"{value} — utility weapon, no damage family"
                return 2, family, level or damage_level(rs, weapon), value

    node = rs.weapon(weapon)                                 # T3
    proj = node.child("Projectile") if node is not None else None
    if proj is not None and (proj.value or "").strip() in PROJECTILE_FAMILY:
        kind = (proj.value or "").strip()
        return 3, PROJECTILE_FAMILY[kind], damage_level(rs, weapon), f"Projectile: {kind}"

    return 4, None, None, "no family signal"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--tier", type=int, help="show only this confidence tier")
    ap.add_argument("--json", help="write the full plan to this path")
    args = ap.parse_args()

    rs = Model().rs
    defined = existing_families(rs)
    fired = names.firing_actors(rs)
    gap = [w for w in fired if names.family_of(rs, w)[0] is None]

    rows = []
    for weapon in sorted(gap):
        tier, family, level, evidence = propose(rs, weapon, defined)
        target = f"^Warhead_{family}_{level}" if family and level else None
        rows.append({"weapon": weapon, "tier": tier, "target": target,
                     "evidence": evidence,
                     "family_exists": bool(family) and (family, level) in defined})

    by_tier = collections.Counter(r["tier"] for r in rows)
    print("# W23 retrofit plan — which ^Warhead_ family each gap weapon should adopt\n")
    print(f"live weapons missing `Inherits@wh: ^Warhead_*`: **{len(rows)}**\n")
    print("| tier | meaning | weapons | target family already defined |")
    print("|---|---|---|---|")
    labels = {1: "CERTAIN — ^Compatibility_ placeholder names it",
              2: "HIGH — legacy template name states it",
              3: "MEDIUM — inferred from Projectile; review each",
              4: "MANUAL — no signal, human picks"}
    for tier in (1, 2, 3, 4):
        have = sum(1 for r in rows if r["tier"] == tier and r["family_exists"])
        print(f"| T{tier} | {labels[tier]} | {by_tier[tier]} | {have} |")

    runnable = by_tier[1] + by_tier[2]
    covered = len(fired) - len(rows)
    after = (covered + runnable) / len(fired) * 100
    print(f"\nCoverage today: **{covered / len(fired) * 100:.1f}%** "
          f"({covered} of {len(fired)}).")
    print(f"Converting T1+T2 alone ({runnable} weapons) would reach "
          f"**{after:.1f}%** — {'past' if after >= 95 else 'still short of'} "
          "the 95% gate the §1b rename needs.")

    for tier in ([args.tier] if args.tier else (1, 2, 3, 4)):
        sel = [r for r in rows if r["tier"] == tier]
        if not sel:
            continue
        print(f"\n## T{tier} — {labels[tier]} ({len(sel)})\n")
        for r in sel[:60 if args.tier else 12]:
            mark = "" if r["family_exists"] or not r["target"] else "  ⚠ family not defined yet"
            print(f"  {r['weapon']:34} -> {str(r['target']):34} [{r['evidence']}]{mark}")
        if not args.tier and len(sel) > 12:
            print(f"  ... {len(sel) - 12} more (use --tier {tier})")

    if args.json:
        pathlib.Path(args.json).write_text(
            json.dumps(rows, indent=1, sort_keys=True), encoding="utf-8")
        print(f"\nwrote {args.json}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
