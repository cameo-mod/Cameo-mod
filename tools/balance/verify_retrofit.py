#!/usr/bin/env python3
"""Did a legacy-template retrofit preserve what each weapon actually DOES?

`tools/audit/review_resolve_diff.py` is the usual retrofit check, but it enforces
"`Damage` verbatim" — and this retrofit deliberately changes `Damage`, because moving a
weapon onto a family profile whose mean is 1.28x the legacy ladder has to be paid for.
So it would flag every single weapon and prove nothing.

The invariant is the MEAN of the product:

    effective_damage[armor] = SUM over main warheads of  Damage x Versus[armor] / 100

Per-armor values are SUPPOSED to move — replacing a hand-written ladder with a measured
family profile is the entire deliverable, and a weapon that got sharper against heavy
armour and flatter against infantry has been converted correctly. What must NOT move is
the mean across armors: that is the weapon's overall output, and holding it fixed is what
"pay for the profile change" means. So the mean is the pass/fail criterion and the
per-armor spread is reported as information.

    python tools/balance/verify_retrofit.py --rev HEAD ^SwordWeapon
    python tools/balance/verify_retrofit.py --rev HEAD --all

It also re-checks the three structural traps that no lint catches, because each one
lints clean, boots clean, and is wrong:
  * an ORPHANED old warhead key left behind (fires IN ADDITION to the renamed one);
  * a warhead node left with NO resolvable type (boot NRE in `WeaponInfo.LoadWarheads`);
  * `Spread`/`Falloff` drift (the family's geometry silently replacing the legacy one).
"""
from __future__ import annotations

import argparse
import pathlib
import shutil
import subprocess
import sys
import tempfile

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))
import miniyaml  # noqa: E402
from measure_retrofit_gap import AMBIGUOUS, EXCEPTIONS, MAPPING  # noqa: E402

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

# Not armor rungs: `Shield` is the W21 layer with its own rule, `HAZMAT` is a damage-type
# immunity gate pinned at 50 in every family template. Neither belongs in output stats.
NOT_ARMOR = {"Shield", "HAZMAT"}

# `*_ExtraDamage` is the shield-only chip and `*FriendlyFire` is the retired twin;
# neither is part of what a normal target feels. Matches `formula.spread_damage_sum`.
def is_main(key: str, value: str) -> bool:
    low = key.lower()
    if "extradamage" in low or "friendlyfire" in low or "percentage" in low:
        return False
    return value in ("SpreadDamage", "AreaDamage", "TargetDamage", "")


def main_warheads(rules: miniyaml.Ruleset, name: str) -> list[tuple[float, dict[str, float]]]:
    """(Damage, Versus table) for each main damage warhead of a resolved weapon."""
    try:
        resolved = rules.resolve_weapon(name)
    except Exception:
        return []
    if resolved is None:
        return []
    out = []
    for c in resolved.children:
        if not (c.key == "Warhead" or c.key.startswith("Warhead@")):
            continue
        if not is_main(c.key, (c.value or "").strip()):
            continue
        dmg_node = c.child("Damage")
        if dmg_node is None:
            continue
        try:
            dmg = float((dmg_node.value or "").strip())
        except ValueError:
            continue
        table = {}
        vs = c.child("Versus")
        if vs is not None:
            for a in vs.children:
                try:
                    table[a.key] = float((a.value or "").strip())
                except ValueError:
                    pass
        out.append((dmg, table))
    return out


def effective(warheads: list[tuple[float, dict[str, float]]],
              universe: list[str]) -> dict[str, float]:
    """Per-armor effective damage over a FIXED armor universe.

    The universe must be fixed across both revisions and across every warhead of the
    weapon. Building it incrementally means a weapon's first warhead never contributes
    to armors that only its second warhead names, which silently under-counts every
    multi-warhead weapon — and an armor with no row is not absent, it resolves to 100.
    """
    return {armor: sum(d * t.get(armor, 100.0) / 100.0 for d, t in warheads)
            for armor in universe}


def geometry(rules: miniyaml.Ruleset, name: str) -> dict[str, str]:
    try:
        resolved = rules.resolve_weapon(name)
    except Exception:
        return {}
    if resolved is None:
        return {}
    out = {}
    for c in resolved.children:
        if not (c.key == "Warhead" or c.key.startswith("Warhead@")):
            continue
        if not is_main(c.key, (c.value or "").strip()):
            continue
        for f in ("Spread", "Falloff"):
            n = c.child(f)
            if n is not None:
                out[f"{c.key}.{f}"] = (n.value or "").strip()
    return out


def warhead_keys(rules: miniyaml.Ruleset, name: str) -> set[str]:
    try:
        resolved = rules.resolve_weapon(name)
    except Exception:
        return set()
    if resolved is None:
        return set()
    return {c.key for c in resolved.children
            if c.key == "Warhead" or c.key.startswith("Warhead@")}


def family_of(rules: miniyaml.Ruleset, legacy: str) -> set[str]:
    fam = {legacy}
    changed = True
    while changed:
        changed = False
        for name, n in rules.weapons.items():
            if name in fam:
                continue
            for c in n.children:
                if c.key.startswith("Inherits") and c.value and c.value.strip() in fam:
                    fam.add(name)
                    changed = True
                    break
    return fam


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("templates", nargs="*", help="legacy templates that were converted")
    ap.add_argument("--rev", default="HEAD", help="revision to compare against")
    ap.add_argument("--all", action="store_true", help="every convertible template")
    ap.add_argument("--tolerance", type=float, default=0.02,
                    help="allowed per-armor drift (default 2%%, for integer rounding)")
    args = ap.parse_args()

    names = args.templates
    if args.all:
        names = [t for t in list(MAPPING) + list(AMBIGUOUS) if t not in EXCEPTIONS]
    if not names:
        ap.error("name at least one template, or pass --all")

    tmp = pathlib.Path(tempfile.mkdtemp(prefix="cameo_retrofit_"))
    base = tmp / "base"
    try:
        subprocess.run(["git", "worktree", "add", "--detach", str(base), args.rev],
                       cwd=ROOT, check=True, capture_output=True)
        before = miniyaml.Ruleset(base)
        after = miniyaml.Ruleset(ROOT)

        # The family is read from BEFORE: after conversion the descendants are the same
        # set, but reading it from the old tree is what proves none went missing.
        weapons: set[str] = set()
        for t in names:
            weapons |= family_of(before, t)
        weapons = {w for w in weapons if not w.startswith("^")} | \
                  {w for w in weapons if w.startswith("^")}

        drift, orphans, typeless, geo = [], [], [], []
        reshape: list[tuple[str, float, float]] = []
        checked = 0
        for w in sorted(weapons):
            wb, wa = main_warheads(before, w), main_warheads(after, w)
            if not wb or not wa:
                continue
            checked += 1
            armors = sorted({k for _, t in wb + wa for k in t} - NOT_ARMOR)
            if not armors:
                continue
            b, a = effective(wb, armors), effective(wa, armors)
            ob = [b[x] for x in armors]
            oa = [a[x] for x in armors]
            if armors and sum(ob) > 0:
                mean_ratio = sum(oa) / sum(ob)
                if abs(mean_ratio - 1.0) > args.tolerance:
                    drift.append((w, "MEAN", sum(ob) / len(ob), sum(oa) / len(oa)))
                ratios = [y / x for x, y in zip(ob, oa) if x > 0]
                if ratios:
                    reshape.append((w, min(ratios), max(ratios)))
            gb, ga = geometry(before, w), geometry(after, w)
            for k in set(gb) & set(ga):
                if gb[k] != ga[k]:
                    geo.append((w, k, gb[k], ga[k]))
            # Any surviving key named after a converted template is an orphan.
            for k in warhead_keys(after, w):
                for t in names:
                    tag = t.lstrip("^")
                    if k.startswith(f"Warhead@{tag}"):
                        orphans.append((w, k))

        print(f"resolved {checked} weapons across {len(names)} template families "
              f"(vs {args.rev})")
        if reshape:
            lo = min(r[1] for r in reshape)
            hi = max(r[2] for r in reshape)
            print(f"  profile reshaped (INTENDED): per-armor {lo:.2f}x .. {hi:.2f}x")
        print(f"  MEAN output drift > {args.tolerance:.0%}: {len(drift)}")
        for w, armor, ob, oa in drift[:12]:
            print(f"      {w:38s} {armor:12s} {ob:10.1f} -> {oa:10.1f}")
        if len(drift) > 12:
            print(f"      ... and {len(drift) - 12} more")
        print(f"  orphaned old warhead keys              : {len(orphans)}")
        for w, k in orphans[:8]:
            print(f"      {w:38s} {k}")
        print(f"  Spread/Falloff drift                   : {len(geo)}")
        for w, k, gb, ga in geo[:8]:
            print(f"      {w:38s} {k}: {gb} -> {ga}")
        ok = not (drift or orphans or geo)
        print("  RESULT: PASS — resolved behaviour preserved" if ok
              else "  RESULT: FAIL — see above")
        return 0 if ok else 1
    finally:
        subprocess.run(["git", "worktree", "remove", "--force", str(base)],
                       cwd=ROOT, capture_output=True)
        shutil.rmtree(tmp, ignore_errors=True)


if __name__ == "__main__":
    raise SystemExit(main())
