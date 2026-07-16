#!/usr/bin/env python3
"""audit_weapon_suffixes.py — DESIGN.md §1 weapon suffix detector.

Checks that weapon IDs follow the suffix conventions documented in
DESIGN.md §1:

  X1 Elite weapons: must end with _elite (legacy E suffix is deprecated)
     — only checks weapons referenced by Armament@*ELITE* blocks gated
     by RequiresCondition: rank-elite. Non-elite weapons that happen to
     end with E (e.g. PrismChargeE, EMP weapons) are NOT flagged.

  X2 EMP weapons: weapons whose name contains EMP (case-insensitive)
     but do NOT end with _EMP. These should be renamed to use the _EMP
     suffix. Excludes weapons that are sub-variants (e.g. _AA, _elite,
     _fire, Fragment, Arc, TeslaFragment) — only the base EMP weapon
     is flagged.

  X3 AA weapons: weapons whose ValidTargets includes Air but whose
     name does not end with _AA (or _aa). Excludes weapons that also
     target Ground/Water (dual-purpose weapons are not AA-only variants)
     and weapons whose base name already contains "AA" or "Flak" or
     "SAM" or "Interceptor" or "Missile" (legacy naming that predates
     the convention).

  X4 Deprecated E suffix: weapons ending with capital E that are NOT
     elite variants (not gated by rank-elite) and NOT EMP weapons.
     These are likely either legacy elite weapons that need _elite
     migration, or coincidental E endings. Reported as informational.
"""

import os
import re
import sys

root = sys.argv[1] if len(sys.argv) > 1 else "mods/cameo"

# Suffixes that indicate a sub-variant of an EMP weapon — not the base
EMP_SUBVARIANT_SUFFIXES = (
    "_AA", "_aa", "_elite", "_fire", "_AG", "_G", "_Structure",
    "_Garrisoned", "_air", "_end", "_anim",
    "Fragment", "Arc", "TeslaFragment", "Missile", "Defender",
    "Patriot", "Mig", "Bomb",
)

# AA name keywords that predate the _AA convention
AA_LEGACY_KEYWORDS = ("aa", "flak", "sam", "interceptor", "patriot",
                      "aagun", "aamissile", "aafortress")

# Weapons that target Air but also Ground/Water are dual-purpose, not AA variants
DUAL_PURPOSE_TARGETS = ("Ground", "Water", "Infantry", "Vehicle", "Building",
                        "Defense", "Structure", "Ship")


def is_emp_weapon(name: str) -> bool:
    """Check if a weapon name contains EMP (case-insensitive)."""
    return "emp" in name.lower()


def is_emp_subvariant(name: str) -> bool:
    """Check if a weapon is a sub-variant of an EMP weapon."""
    for suffix in EMP_SUBVARIANT_SUFFIXES:
        if name.endswith(suffix):
            return True
    # Also check for patterns like TSCABALEMPDisable.anim / .end
    if "." in name:
        return True
    return False


def main() -> int:
    x1_rows = []  # elite weapons not ending _elite
    x2_rows = []  # EMP weapons not ending _EMP
    x3_rows = []  # AA weapons not ending _AA
    x4_rows = []  # deprecated E suffix (informational)

    # Track which weapons are elite-gated (to exclude from X4)
    elite_weapons: set[str] = set()

    for dirpath, _, filenames in os.walk(root):
        for fn in filenames:
            if not fn.endswith(".yaml"):
                continue
            fpath = os.path.join(dirpath, fn)
            try:
                lines = open(fpath, encoding="utf-8").readlines()
            except Exception:
                continue

            # --- Parse weapon definitions ---
            i = 0
            while i < len(lines):
                line = lines[i]
                # Top-level weapon definition (no leading tab, not a comment)
                wm = re.match(r'^([A-Za-z][A-Za-z0-9_.\-]*):\s*$', line)
                if wm and not line.startswith('\t') and not line.strip().startswith('#'):
                    weapon_name = wm.group(1)

                    # Skip templates
                    if weapon_name.startswith('^'):
                        i += 1
                        continue

                    # Parse weapon body
                    j = i + 1
                    valid_targets = []
                    while j < len(lines):
                        bl = lines[j]
                        if re.match(r'^[^\t\s]', bl) and not bl.strip().startswith('#'):
                            break
                        vtm = re.match(r'^\tValidTargets:\s*(.+)', bl)
                        if vtm:
                            valid_targets = [t.strip() for t in vtm.group(1).split(",")]
                        j += 1

                    # X2: EMP weapons not ending _EMP
                    if is_emp_weapon(weapon_name) and not is_emp_subvariant(weapon_name):
                        if not weapon_name.endswith('_EMP') and not weapon_name.endswith('_emp'):
                            short = fpath.replace("\\", "/").replace("mods/cameo/", "")
                            x2_rows.append((short, i + 1, weapon_name))

                    # X3: AA weapons not ending _AA
                    if "Air" in valid_targets:
                        is_dual = any(t in valid_targets for t in DUAL_PURPOSE_TARGETS)
                        has_legacy = any(kw in weapon_name.lower() for kw in AA_LEGACY_KEYWORDS)
                        if not is_dual and not has_legacy:
                            if not weapon_name.endswith('_AA') and not weapon_name.endswith('_aa'):
                                short = fpath.replace("\\", "/").replace("mods/cameo/", "")
                                x3_rows.append((short, i + 1, weapon_name, ", ".join(valid_targets)))

                    # X4: Deprecated E suffix (not elite, not EMP)
                    if weapon_name.endswith('E') and not weapon_name.endswith('_E'):
                        if weapon_name not in elite_weapons and not is_emp_weapon(weapon_name):
                            # Exclude common non-weapon patterns
                            if not weapon_name.endswith(('ChargeE', 'ScatterE')):
                                short = fpath.replace("\\", "/").replace("mods/cameo/", "")
                                x4_rows.append((short, i + 1, weapon_name))

                # --- Parse Armament@*ELITE* blocks for X1 ---
                am = re.match(r'^\tArmament@\w*[Ee][Ll][Ii][Tt][Ee]\w*\s*:', line)
                if am:
                    j = i + 1
                    weapon_ref = None
                    has_rank_elite = False
                    while j < len(lines):
                        bl = lines[j]
                        if re.match(r'^\t\S', bl) or re.match(r'^[^\t\s]', bl):
                            break
                        wref = re.match(r'^\t\tWeapon:\s*(\S+)', bl)
                        if wref:
                            weapon_ref = wref.group(1)
                        if 'rank-elite' in bl.lower():
                            has_rank_elite = True
                        j += 1
                    if weapon_ref and has_rank_elite:
                        elite_weapons.add(weapon_ref)
                        if not weapon_ref.endswith('_elite'):
                            actor_name = "?"
                            for k in range(i - 1, -1, -1):
                                actm = re.match(r'^(\S+):', lines[k])
                                if actm and not lines[k].startswith('\t') and not lines[k].strip().startswith('#'):
                                    actor_name = actm.group(1)
                                    break
                            trait_name = am.group(0).strip().rstrip(':')
                            short = fpath.replace("\\", "/").replace("mods/cameo/", "")
                            x1_rows.append((short, i + 1, actor_name, trait_name, weapon_ref))

                i += 1

    # Re-scan X4 now that we know which weapons are elite-gated
    # (already filtered during first pass via elite_weapons set, but
    #  some elite weapons may be defined after their armament ref)

    print("# Weapon suffix audit (DESIGN.md §1)\n")
    print(f"X1 elite weapons not ending _elite: **{len(x1_rows)}**")
    print(f"X2 EMP weapons not ending _EMP: **{len(x2_rows)}**")
    print(f"X3 AA weapons not ending _AA: **{len(x3_rows)}**")
    print(f"X4 deprecated E suffix (informational): **{len(x4_rows)}**\n")

    if x1_rows:
        print("## X1 — Elite weapons not following _elite convention")
        print("| File | Line | Actor | Trait | Weapon |")
        print("|---|---|---|---|---|")
        for fpath, line, actor, trait, weapon in sorted(x1_rows):
            print(f"| {fpath} | {line} | {actor} | {trait} | {weapon} |")
        print()

    if x2_rows:
        print("## X2 — EMP weapons not following _EMP convention")
        print("| File | Line | Weapon |")
        print("|---|---|---|")
        for fpath, line, weapon in sorted(x2_rows):
            print(f"| {fpath} | {line} | {weapon} |")
        print()

    if x3_rows:
        print("## X3 — AA-only weapons not following _AA convention")
        print("| File | Line | Weapon | ValidTargets |")
        print("|---|---|---|---|")
        for fpath, line, weapon, vt in sorted(x3_rows):
            print(f"| {fpath} | {line} | {weapon} | {vt} |")
        print()

    if x4_rows:
        print("## X4 — Weapons with deprecated E suffix (informational)")
        print("| File | Line | Weapon |")
        print("|---|---|---|")
        for fpath, line, weapon in sorted(x4_rows):
            print(f"| {fpath} | {line} | {weapon} |")
        print()

    return 1 if x1_rows else 0


if __name__ == "__main__":
    sys.exit(main())
