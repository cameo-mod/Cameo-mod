#!/usr/bin/env python3
"""audit_class_redundancy.py — two units of one class a player can build at once.

    python tools/audit/audit_class_redundancy.py
    python tools/audit/audit_class_redundancy.py --faction naxis

Maintainer ruling 2026-08-29:

> *"As long as they are not available at the same time it's fine, because you can
> only have one of them. But if two units of the same class are available in the
> same moment AND they also target the same things, then that is a problem. TD Nod
> light tank upgrading to the light tank Mk2 in the promotions is fine since you
> cannot build them at the same time — one is locked behind the promotion and the
> other one is disabled by it."*

So redundancy needs BOTH conditions. A pair is reported only when it is:

  1. the same faction and the same `design.class_anchor`, AND
  2. **simultaneously available** — no prerequisite token separates them, AND
  3. **aimed at the same things** — their armaments' `ValidTargets` overlap.

⚠ MUTUAL EXCLUSION IS THE FIRST TEST AND IT IS CHEAP. The tree encodes it as one
token appearing negated on one unit and positive on the other:

    td_nod_lighttank       ~td_nod_airstrip, ~!td_nod_promotion_lighttankmkii
    td_nod_lighttankmkii   ~td_nod_airstrip, ~td_nod_promotion_lighttankmkii

`~` marks a prerequisite as hidden, not as negated — strip it BEFORE testing for
`!`, or every hidden prerequisite reads as a negation and no pair is ever
mutually exclusive. That upgrade pair is correct design and must not be reported.

⚠ DIFFERENT TARGETING IS A REAL DIFFERENCE. Two `mbt`-class units where one can
hit air and one cannot are different tools, so the class alone does not decide
the role — targeting is part of it.

EXIT CODE: 1 when a genuinely redundant pair exists.
"""
from __future__ import annotations

import argparse
import collections
import glob
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))

from cameo_model import Model  # noqa: E402

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

LEDGER = ROOT / "docs" / "balance"

# DESIGN.md section 1 structural variant suffixes. Two actors differing ONLY by one
# of these are the same unit in another state, not two units competing for a slot:
# `ra2_allies_ifv` and `ra2_allies_ifv_missile` are one IFV whose weapon depends on
# the infantry riding it, and `_empty` / `_chrono` are cargo and teleport states.
# Without this the RA2 Allies IFV alone reports 15 "redundant" pairs against itself.
VARIANT_SUFFIXES = (
    "_husk", "_sp", "_r4", "_wild", "_mk2", "_elite", "_ai", "_water", "_EMP",
    "_AA", "_upgraded", "_slave", "_air", "_backup", "_segment", "_bomber",
    "_paradrop", "_chrono", "_hmg", "_mg", "_missile", "_repair", "_empty",
    "_plug", "_bot", "_defense", "_deployed",
)


def variant_base(actor):
    """The actor id with trailing structural variant suffixes removed."""
    stem = actor
    changed = True
    while changed:
        changed = False
        for suffix in VARIANT_SUFFIXES:
            if len(stem) > len(suffix) and stem.lower().endswith(suffix.lower()):
                stem = stem[: -len(suffix)]
                changed = True
    return stem.lower()


def tagged_units():
    """{faction: {class: [actor]}} from the ledgers' design.class_anchor."""
    out = collections.defaultdict(lambda: collections.defaultdict(list))
    for path in sorted(glob.glob(str(LEDGER / "*.json"))):
        if "class_anchors" in path:
            continue
        try:
            doc = json.loads(pathlib.Path(path).read_text(encoding="utf-8"))
        except (ValueError, OSError):
            continue
        faction = doc.get("ledger", "?")
        for section, units in (doc.get("sections") or {}).items():
            if not isinstance(units, dict) or section in ("buildings", "upgrades",
                                                          "promotions"):
                continue
            for name, rec in units.items():
                if not isinstance(rec, dict) or not rec.get("buildable"):
                    continue
                cls = (rec.get("design") or {}).get("class_anchor")
                if cls:
                    out[faction][cls].append(name)
    return out


def prereq_tokens(rs, actor):
    """{token: positive?} from Buildable.Prerequisites.

    `~` means HIDDEN, not negated, and it can precede `!` (`~!token`). Strip it
    first; testing for `!` on the raw string makes `~!x` and `~x` both look
    positive and the whole mutual-exclusion test silently passes everything.
    """
    node = rs.resolve(actor)
    if node is None:
        return {}
    buildable = node.child("Buildable")
    if buildable is None:
        return {}
    out = {}
    for raw in (buildable.get("Prerequisites") or "").split(","):
        tok = raw.strip().lstrip("~").strip()
        if not tok:
            continue
        out[tok.lstrip("!")] = not tok.startswith("!")
    return out


def mutually_exclusive(rs, a, b):
    """True when one token is required by one unit and forbidden by the other."""
    ta, tb = prereq_tokens(rs, a), prereq_tokens(rs, b)
    for token, positive in ta.items():
        if token in tb and tb[token] != positive:
            return token
    return None


def targets(rs, actor):
    """The union of ValidTargets across the actor's armament weapons."""
    node = rs.resolve(actor)
    if node is None:
        return frozenset()
    out = set()
    for c in node.children:
        if c.key.split("@")[0] != "Armament":
            continue
        weapon = (c.get("Weapon") or "").strip()
        if not weapon:
            continue
        w = rs.resolve_weapon(weapon)
        if w is None:
            continue
        vt = w.get("ValidTargets")
        if vt:
            out |= {t.strip() for t in vt.split(",") if t.strip()}
    return frozenset(out)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--faction", help="restrict to one ledger")
    args = ap.parse_args()

    rs = Model().rs
    by_faction = tagged_units()

    redundant, excused = [], []
    for faction, classes in sorted(by_faction.items()):
        if args.faction and faction != args.faction:
            continue
        for cls, actors in sorted(classes.items()):
            if len(actors) < 2:
                continue
            for i, a in enumerate(sorted(actors)):
                for b in sorted(actors)[i + 1:]:
                    if variant_base(a) == variant_base(b):
                        excused.append((faction, cls, a, b,
                                        "same unit, different structural variant"))
                        continue
                    token = mutually_exclusive(rs, a, b)
                    if token:
                        excused.append((faction, cls, a, b,
                                        f"mutually exclusive on `{token}`"))
                        continue
                    ta, tb = targets(rs, a), targets(rs, b)
                    shared = ta & tb
                    if not shared:
                        excused.append((faction, cls, a, b,
                                        "no shared ValidTargets"))
                        continue
                    redundant.append((faction, cls, a, b, sorted(shared)))

    print("# Class redundancy audit\n")
    print("A pair is redundant only when it is the same class, **buildable at the "
          "same time**, AND aimed at the same targets (maintainer 2026-08-29).\n")
    print(f"pairs excused : {len(excused)}")
    print(f"pairs REDUNDANT: {len(redundant)}\n")

    if redundant:
        by_fac = collections.defaultdict(list)
        for faction, cls, a, b, shared in redundant:
            by_fac[faction].append((cls, a, b, shared))
        for faction, rows in sorted(by_fac.items()):
            print(f"\n## {faction} ({len(rows)})\n")
            for cls, a, b, shared in rows:
                print(f"* `{cls}` — `{a}` vs `{b}`")
                print(f"    both hit: {', '.join(shared)}")

    if excused:
        reasons = collections.Counter(r[4].split(" on ")[0] for r in excused)
        print("\n\n## Excused pairs (correct design, not findings)\n")
        for reason, n in reasons.most_common():
            print(f"  {n:4}  {reason}")
        print("\nSample of upgrade/promotion pairs, which are the intended shape:\n")
        for faction, cls, a, b, why in excused[:8]:
            if "mutually exclusive" in why:
                print(f"  {a:34} vs {b:34} {why}")

    if redundant:
        print(f"\n**FAIL** — {len(redundant)} redundant pairs.")
        return 1
    print("\n**PASS**")
    return 0


if __name__ == "__main__":
    sys.exit(main())
