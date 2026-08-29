#!/usr/bin/env python3
"""audit_counter_matrix.py — does Cameo DO what counter_matrix.yaml INTENDS?

    python tools/audit/audit_counter_matrix.py
    python tools/audit/audit_counter_matrix.py --class tank_destroyer

`docs/balance/counter_matrix.yaml` states the intended class-vs-class
relationships. This measures the live tree and reports the difference. It changes
nothing and proposes no numbers.

    INTENT (the yaml)  ->  IMPLEMENTATION (family per class)  ->  MEASURED (here)

THREE CHECKS

  C1 FAMILY ASSIGNMENT — does each class's members actually use the warhead
     family its role needs? This is the cheapest and most actionable check,
     because the AP families ALREADY ascend the vehicle ladder: a tank_destroyer
     using `CannonHE` is not missing a number, it is holding the wrong weapon.

  C2 REALIZED MULTIPLIER — the mean `Versus` row an attacker class's weapons write
     against the armor its defender class actually wears, over 100. 1.00 is the
     MEAN-100 baseline, so this reads directly as "x times an average matchup".
     Compared against the band the relation declares.

  C3 MONOTONICITY — where a class declares `monotonic_by_armor`, its families must
     ASCEND that ladder. This is the maintainer's "the heavier the tank, the
     stronger the counter" law, stated as an ORDERING so it survives renumbering.

⚠ DEFENDER ARMOR IS MEASURED, NOT ASSUMED. Each class's armor comes from what its
tagged members actually wear (mbt is 38/39 `Heavy`, high_tech_tank 26/26
`Superheavy`), so the audit cannot drift from the roster.

⚠ COVERAGE IS THIN AND SAID SO OUT LOUD. Only 336 of 1871 buildable units carry a
`design.class_anchor`, and five classes have no members at all. Every row prints
the sample it rests on; a row backed by two units is not evidence.

⚠ THIS IS NOT A PRICING TOOL. A failing row has several possible causes — wrong
family, wrong class assignment, wrong armor, a legacy weapon awaiting W23 — and
"raise the number" is rarely the right one. The audit names the gap; a human picks
the fix.

EXIT CODE: always 0. Every finding is a design question, never a build break.
"""
from __future__ import annotations

import argparse
import collections
import glob
import json
import pathlib
import re
import statistics
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))

from cameo_model import Model  # noqa: E402
import weapon_efficiency as we  # noqa: E402

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

MATRIX = ROOT / "docs" / "balance" / "counter_matrix.yaml"
LEDGER = ROOT / "docs" / "balance"
FAMILY_INHERIT = re.compile(r"^\^Warhead_(?P<family>\w+?)_(Light|Medium|Heavy|Super|Trace)$")


def members():
    """{class: [actor]} from every ledger's design.class_anchor."""
    out = collections.defaultdict(list)
    for path in sorted(glob.glob(str(LEDGER / "*.json"))):
        if "class_anchors" in path:
            continue
        try:
            doc = json.loads(pathlib.Path(path).read_text(encoding="utf-8"))
        except (ValueError, OSError):
            continue
        for section, units in (doc.get("sections") or {}).items():
            if not isinstance(units, dict) or section in ("buildings", "upgrades",
                                                          "promotions"):
                continue
            for name, rec in units.items():
                if isinstance(rec, dict) and rec.get("buildable"):
                    cls = (rec.get("design") or {}).get("class_anchor")
                    if cls:
                        out[cls].append(name)
    return out


def class_armor(rs, actors):
    """What this class actually wears, commonest first."""
    counter = collections.Counter()
    for actor in actors:
        node = rs.resolve(actor)
        if node is None:
            continue
        armor = node.child("Armor")
        if armor is not None and armor.get("Type"):
            counter[armor.get("Type").strip()] += 1
    return counter


def class_families(rs, actors):
    """The warhead families this class's weapons inherit, commonest first."""
    counter = collections.Counter()
    for actor in actors:
        node = rs.resolve(actor)
        if node is None:
            continue
        for child in node.children:
            if child.key.split("@")[0] != "Armament":
                continue
            weapon = (child.get("Weapon") or "").strip()
            wnode = rs.weapon(weapon) if weapon else None
            if wnode is None:
                continue
            for inh in wnode.children:
                if not inh.key.startswith("Inherits"):
                    continue
                m = FAMILY_INHERIT.match((inh.value or "").strip())
                if m:
                    counter[m.group("family")] += 1
    return counter


def main_versus(rs, weapon):
    """Versus of the weapon's MAIN damage warhead, or None.

    ⚠ NOT "the first warhead carrying a Versus". A weapon's percentage twin and
    its chip warheads also carry full profiles, so taking the first one can read
    a 5-damage secondary as the weapon's identity. Pick by Damage.
    """
    node = rs.resolve_weapon(weapon)
    if node is None:
        return None
    best, best_damage = None, -1
    for child in node.children:
        if child.key.split("@")[0] != "Warhead":
            continue
        versus = we.versus_of(child)
        if not versus or len(versus) < 10:
            continue
        raw = child.get("Damage")
        try:
            damage = abs(int(str(raw).strip())) if raw else 0
        except ValueError:
            damage = 0
        if damage > best_damage:
            best, best_damage = versus, damage
    return best


def class_weapons(rs, actors):
    """Every weapon the class's members actually fire, with its firer."""
    out = []
    for actor in actors:
        node = rs.resolve(actor)
        if node is None:
            continue
        for child in node.children:
            if child.key.split("@")[0] != "Armament":
                continue
            weapon = (child.get("Weapon") or "").strip()
            if weapon:
                out.append((actor, weapon))
    return out


def family_profiles(rs):
    """{family: {armor: mean Versus}} over every ^Warhead_ template."""
    rows = collections.defaultdict(lambda: collections.defaultdict(list))
    for name in rs.weapons:
        m = FAMILY_INHERIT.match(name)
        if not m:
            continue
        node = rs.resolve_weapon(name)
        if node is None:
            continue
        for child in node.children:
            if child.key.split("@")[0] != "Warhead":
                continue
            versus = we.versus_of(child)
            if versus and len(versus) >= 10:
                for armor, value in versus.items():
                    try:
                        rows[m.group("family")][armor].append(float(value))
                    except (TypeError, ValueError):
                        pass
                break
    return {fam: {a: statistics.mean(v) for a, v in armors.items()}
            for fam, armors in rows.items()}


def realized(profiles, families, armor):
    """Mean Versus an attacker's families write against `armor`, over 100."""
    vals = []
    total = sum(families.values())
    for family, n in families.items():
        prof = profiles.get(family)
        if prof and armor in prof:
            vals.extend([prof[armor]] * n)
    if not vals or not total:
        return None
    return statistics.mean(vals) / 100.0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--class", dest="cls", help="restrict to one class")
    args = ap.parse_args()

    try:
        import yaml
    except ImportError:
        print("audit_counter_matrix: PyYAML unavailable; the intent lives in "
              f"{MATRIX} and cannot be read without it.", file=sys.stderr)
        return 0

    matrix = yaml.safe_load(MATRIX.read_text(encoding="utf-8"))
    bands = matrix["bands"]
    rs = Model().rs
    by_class = members()
    profiles = family_profiles(rs)

    armor_of = {c: class_armor(rs, a) for c, a in by_class.items()}
    fams_of = {c: class_families(rs, a) for c, a in by_class.items()}

    print("# Counter matrix — intent vs implementation\n")
    print(f"classes with declared intent : {len(matrix['classes'])}")
    print(f"classes with tagged members  : {len(by_class)}")
    print(f"warhead families measured    : {len(profiles)}\n")
    print("⚠ Only 336 of 1871 buildable units are class-tagged. Every row below "
          "prints its sample size; a row backed by two units is not evidence.\n")

    # ------------------------------------------------------------------ C1 ---
    print("\n## C1 — is each class holding the weapon its role needs?\n")
    print("| class | members | expects | actually uses | verdict |")
    print("|---|--:|---|---|---|")
    misassigned = []
    for cls, spec in sorted(matrix["classes"].items()):
        if args.cls and cls != args.cls:
            continue
        expects = spec.get("expects_family") or []
        actors = by_class.get(cls) or []
        fams = fams_of.get(cls) or collections.Counter()
        if not actors:
            verdict = "⛔ no tagged members"
        elif not fams:
            verdict = "⛔ no ^Warhead_ family — legacy, awaiting W23"
        else:
            top = [f for f, _ in fams.most_common(3)]
            hit = [f for f in top if f in expects]
            if hit and top[0] in expects:
                verdict = "✅"
            elif hit:
                verdict = f"⚠ expected family present but not dominant"
                misassigned.append((cls, expects, top))
            else:
                verdict = "❌ none of the expected families in its top 3"
                misassigned.append((cls, expects, top))
        print(f"| `{cls}` | {len(actors)} | {', '.join(expects) or '—'} | "
              f"{', '.join(f for f, _ in fams.most_common(3)) or '—'} | {verdict} |")

    # ------------------------------------------------------------------ C2 ---
    print("\n\n## C2 — realized multiplier vs the declared band\n")
    print("1.00 is the MEAN-100 baseline, so a value reads as "
          "'x times an average matchup'.\n")
    print("| attacker | defender | relation | band | realized | sample | verdict |")
    print("|---|---|---|---|--:|--:|---|")
    for cls, spec in sorted(matrix["classes"].items()):
        if args.cls and cls != args.cls:
            continue
        fams = fams_of.get(cls) or collections.Counter()
        for defender, rel in sorted((spec.get("counters") or {}).items()):
            relation = rel.get("relation", "soft_counter")
            band = bands.get(relation)
            darmor = armor_of.get(defender)
            if not darmor:
                print(f"| `{cls}` | `{defender}` | {relation} | "
                      f"{band or '—'} | — | 0 | ⛔ defender has no tagged members |")
                continue
            armor = darmor.most_common(1)[0][0]
            got = realized(profiles, fams, armor)
            if got is None:
                verdict, shown = "⛔ attacker has no measurable family", "—"
            else:
                shown = f"{got:.2f}"
                if band and band[0] <= got <= band[1]:
                    verdict = "✅"
                elif band and got < band[0]:
                    verdict = f"❌ below band"
                else:
                    verdict = "⚠ above band"
            print(f"| `{cls}` | `{defender}` ({armor}) | {relation} | "
                  f"{band[0]:.2f}–{band[1]:.2f} | {shown} | "
                  f"{sum(darmor.values())} | {verdict} |")

    # ------------------------------------------------------------------ C3 ---
    print("\n\n## C3 — does the counter STRENGTHEN with target weight?\n")
    for cls, spec in sorted(matrix["classes"].items()):
        ladder = spec.get("monotonic_by_armor")
        if not ladder or (args.cls and cls != args.cls):
            continue
        actors = by_class.get(cls) or []
        print(f"**`{cls}`** over {' < '.join(ladder)} — measured on the "
              f"WEAPONS ITS MEMBERS CARRY, not on family templates:\n")
        # ⚠ Measuring families here was wrong and hid the truth. A weapon can be
        # correctly shaped WITHOUT belonging to a canonical family: RA2sabot
        # ascends 119 -> 139 while carrying no `^Warhead_` inherit at all, so a
        # family-based check scored it as contributing nothing and reported the
        # whole class as inverted. Measure what the units actually fire.
        good = bad = unknown = 0
        for actor, weapon in sorted(set(class_weapons(rs, actors))):
            versus = main_versus(rs, weapon)
            if not versus:
                unknown += 1
                continue
            vals = [versus.get(a) for a in ladder]
            if any(v is None for v in vals):
                unknown += 1
                continue
            vals = [float(v) for v in vals]
            ok = all(a < b for a, b in zip(vals, vals[1:]))
            good += ok
            bad += not ok
            print(f"  {weapon:32} " + " -> ".join(f"{v:6.0f}" for v in vals)
                  + f"   {'ascending ✅' if ok else 'INVERTED ❌'}   ({actor})")
        total = good + bad
        if total:
            print(f"\n  {good} of {total} weapons ascend"
                  + (f"; {unknown} carry no readable profile" if unknown else "")
                  + ".")
        print()

    if misassigned:
        print("\n## What to do with C1's misassignments\n")
        print("A class holding the wrong family is not a missing number — the "
              "family it needs already exists and already has the right shape. "
              "The fix is reassignment, which is DESIGN §1b's job.\n")
        for cls, expects, top in misassigned:
            print(f"* `{cls}` wants {expects}, uses {top}")

    print("\n\n_Advisory: every finding here is a design question, never a build "
          "break._")
    return 0


if __name__ == "__main__":
    sys.exit(main())
