#!/usr/bin/env python3
"""rename_3way_underscore.py — naming-consistency sweep for the weapon 3-way
split (docs/design/WEAPON_3WAY_SPLIT.md). Enforces the underscore-section law
across the three template layers so all three read in parallel:

  warhead     ^Bullet_Light             -> ^Warhead_Bullet_Light
  projectile  ^ProjectileBullet_Light   -> ^Projectile_Bullet_Light
  effect      ^EffectBullet_Light       -> ^Effect_Bullet_Light
  twin keys   Warhead@X{Percentage|FriendlyFire|ExtraDamage}
                                         -> Warhead@X_{...}     (X = a warhead name)

Only the warhead TEMPLATE name gains the `Warhead_` prefix — the warhead KEY
(`Warhead@Bullet_Light`) keeps the bare profile name, otherwise keys would read
`Warhead@Warhead_Bullet_Light`. The `\\^` / `(?=[A-Z])` anchors make every rule
idempotent (safe to re-run). BOM-safe read, LF output. Sweeps mods/cameo/**.yaml.

Usage: rename_3way_underscore.py [--apply]
"""
from __future__ import annotations
import argparse, os, re

ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "mods", "cameo")

# the 55 NEW warhead templates (docs/design/WEAPON_3WAY_SPLIT.md library). The OLD
# full-stack templates (^SmallArms, ^LightMissile, ...) are deleted in Phase 4 and
# intentionally NOT renamed here.
WARHEADS = [
    "Bullet_Light", "Bullet_Medium", "Bullet_Heavy",
    "CannonAP_Light", "CannonAP_Medium", "CannonAP_Heavy",
    "CannonHE_Light", "CannonHE_Medium", "CannonHE_Heavy",
    "MissileAP_Light", "MissileAP_Medium", "MissileAP_Heavy",
    "MissileHE_Light", "MissileHE_Medium", "MissileHE_Heavy",
    "MissileAA_Light", "MissileAA_Medium", "MissileAA_Heavy",
    "Flak_Light", "Flak_Medium", "Flak_Heavy",
    "Laser_Light", "Laser_Medium", "Laser_Heavy",
    "Prism_Light", "Prism_Medium", "Prism_Heavy",
    "Flame_Light", "Flame_Medium", "Flame_Heavy",
    "Chemical_Light", "Chemical_Medium", "Chemical_Heavy",
    "Melee_Light", "Melee_Medium", "Melee_Heavy",
    "Arrow_Light", "Arrow_Medium", "Arrow_Heavy",
    "Magic_Light", "Magic_Medium", "Magic_Heavy",
    "Demolition_Light", "Demolition_Medium", "Demolition_Heavy",
    "Concussion_Light", "Concussion_Medium", "Concussion_Heavy",
    "Sonic_Light", "Sonic_Medium", "Sonic_Heavy",
    "Railgun_Heavy", "Tesla_Heavy", "TeslaCharged_Super", "Nuclear_Super",
]
SUFFIX = r"(Percentage|FriendlyFire|ExtraDamage)"


def transform(text):
    counts = {"warhead_tmpl": 0, "projectile_tmpl": 0, "effect_tmpl": 0, "twin_key": 0}
    # A. warhead TEMPLATE name only (the ^-prefixed positions). `\^` immediately
    #    before the name means the `Warhead@X` KEY is never touched, and re-runs
    #    won't re-prefix `^Warhead_X` (there the caret sits before `Warhead`).
    for w in WARHEADS:
        text, k = re.subn(r"\^" + re.escape(w) + r"(?!\w)", "^Warhead_" + w, text)
        counts["warhead_tmpl"] += k
    # B. projectile template name: ^ProjectileX -> ^Projectile_X
    text, k = re.subn(r"\^Projectile(?=[A-Z])", "^Projectile_", text)
    counts["projectile_tmpl"] += k
    # C. effect template name: ^EffectX -> ^Effect_X
    text, k = re.subn(r"\^Effect(?=[A-Z])", "^Effect_", text)
    counts["effect_tmpl"] += k
    # D. twin-suffix underscore on warhead KEYS (both Warhead@ and -Warhead@; the
    #    main key Warhead@X: has no suffix so it is left alone).
    for w in WARHEADS:
        text, k = re.subn(r"(Warhead@" + re.escape(w) + r")" + SUFFIX + r"(?=:)", r"\1_\2", text)
        counts["twin_key"] += k
    return text, counts


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    a = ap.parse_args()
    total = {"warhead_tmpl": 0, "projectile_tmpl": 0, "effect_tmpl": 0, "twin_key": 0}
    files_changed = 0
    for dp, _, fs in os.walk(ROOT):
        for fn in fs:
            if not fn.endswith(".yaml"):
                continue
            p = os.path.join(dp, fn)
            raw = open(p, encoding="utf-8-sig").read()  # BOM-safe
            new, counts = transform(raw)
            if new != raw:
                files_changed += 1
                for kk in total:
                    total[kk] += counts[kk]
                if a.apply:
                    open(p, "w", encoding="utf-8", newline="\n").write(new)
    mode = "APPLIED" if a.apply else "DRY RUN"
    print(f"[{mode}] {files_changed} files changed")
    for kk, v in total.items():
        print(f"    {kk}: {v}")


if __name__ == "__main__":
    main()
