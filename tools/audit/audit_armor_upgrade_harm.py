#!/usr/bin/env python3
"""audit_armor_upgrade_harm.py — AN ARMOR UPGRADE MUST NEVER INCREASE INCOMING DAMAGE.

Incident 2026-08-16. The maintainer asked, while reviewing the HAZMAT/REFLECTOR
rework: *"now that I think about it would that mean that averaging can also make
the unit take MORE damage? this is a serious concern"*. It does, and it did — 98
of 1152 cells at the time, worst 1.84x.

**Why this class of bug needs its own guard.** It is invisible to everything else
we run. The yaml is well-formed, every value sits inside the window, the resolver
is happy, `find_empty_warhead` is 0, and the game boots to the menu — a boot gate
cannot see a number that is merely WRONG. It is also invisible by inspection,
because the defect is not in either value but in their INTERACTION: `HAZMAT: 86`
is unremarkable, `Heroic: 32` is unremarkable, and `(86 + 32) / 2 = 59` is a
1.84x self-inflicted damage increase on a unit that just bought an upgrade.

The mechanic
------------
An armor PLATING (`HAZMAT`, `REFLECTOR`, and whatever the plating taxonomy grows
to) is a CONDITIONAL overlay armor granted by an upgrade — the actor carries it
IN ADDITION to its class armor. Cameo's `AreaDamageWarhead.DamageVersus`
combines multiple armors with `MultiArmorCombination`, default **Average**, so

    effective = (class_armor + plating) / 2

which is an INCREASE over `class_armor` alone whenever `plating > class_armor`.
Heavy units are hit hardest, because they are the ones with the low (resistant)
class rows — precisely backwards for an upgrade.

⚠ This audit encodes the INVARIANT, not the current arithmetic. If the
combination rule changes (`docs/design/PSEUDO_ARMOR_AND_INTEGRITY.md` §F offers
five options; `min(base, plating)` and the guarded layer model both make
violations impossible), this audit goes green on its own and stays useful as the
thing that proves it.

Scope: the `^Warhead_*` templates, which is where `Versus` is allowed to live at
all (DESIGN.md — `Versus` lives ONLY in templates). Legacy weapons carrying
inline `Versus` are W23/W24's problem and are counted, not diagnosed.
"""
from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

HEADER = re.compile(r"^\^Warhead_(\w+?)_(\w+):$")

# The overlay armors: granted by an upgrade, carried IN ADDITION to class armor.
# `Shield` is deliberately NOT here — it is a LAYER, and `defaults.yaml` disables
# the base armor while the shield is up (`Armor: RequiresCondition: !shielded`),
# so it replaces rather than averages and cannot produce this defect.
PLATINGS = ("HAZMAT", "REFLECTOR", "BlastProtection", "Composite",
            "Reactive", "Insulated")
NOT_CLASS = set(PLATINGS) | {"Shield"}


def templates(text: str):
    """(family, level) -> {armor: value} for each template's MAIN warhead."""
    cur = None
    inmain = invs = False
    rows: dict[str, int] = {}
    out = []
    for line in text.split("\n"):
        m = HEADER.match(line.rstrip())
        if m:
            if cur and rows:
                out.append((cur, rows))
            cur, rows, inmain, invs = (m.group(1), m.group(2)), {}, False, False
            continue
        if cur is None:
            continue
        s = line.strip()
        if s.startswith("Warhead@"):
            inmain = not s.split(":")[0].endswith(
                ("_Percentage", "_ExtraDamage", "_FriendlyFire"))
            invs = False
            continue
        if inmain and s == "Versus:":
            invs = True
            continue
        if invs:
            if line.startswith("\t\t\t") and ":" in s:
                key, value = s.split(":", 1)
                try:
                    rows[key] = int(value)
                    continue
                except ValueError:
                    pass
            invs = False
    if cur and rows:
        out.append((cur, rows))
    return out


def main() -> int:
    path = ROOT / "mods" / "cameo" / "weapons" / "weapons.yaml"
    found = templates(path.read_text(encoding="utf-8"))

    checked = 0
    bad = []
    for (fam, level), rows in found:
        classes = {a: v for a, v in rows.items() if a not in NOT_CLASS}
        for plating in PLATINGS:
            if plating not in rows:
                continue                       # omitted = the overlay is absent entirely
            p = rows[plating]
            for armor, base in classes.items():
                checked += 1
                effective = (base + p) / 2     # the live MultiArmorCombination
                if effective > base:
                    bad.append((effective / base, f"^Warhead_{fam}_{level}",
                                plating, p, armor, base, round(effective, 1)))

    print("# audit_armor_upgrade_harm — an armor upgrade must never increase damage")
    print()
    print(f"Checked **{checked}** (template, plating, class-armor) combinations across "
          f"**{len(found)}** `^Warhead_*` templates.")
    print()
    if not bad:
        print("_clean_ — no plating increases incoming damage against any class armor.")
        return 0

    bad.sort(reverse=True)
    print(f"## FAIL — {len(bad)} combination(s) where wearing the plating makes it WORSE")
    print()
    print("`effective = (class + plating) / 2`, so any row above the class armor is a")
    print("self-inflicted damage increase on a unit that paid for an upgrade.")
    print()
    print("| ratio | template | plating | row | class armor | base | effective |")
    print("|--:|---|---|--:|---|--:|--:|")
    for ratio, tmpl, plating, p, armor, base, eff in bad[:60]:
        print(f"| **{ratio:.2f}x** | `{tmpl}` | `{plating}` | {p} | `{armor}` | "
              f"{base} | {eff} |")
    if len(bad) > 60:
        print(f"\n_... and {len(bad) - 60} more._")
    print()
    print("Fix: see `docs/design/PSEUDO_ARMOR_AND_INTEGRITY.md` §F. Lowering the row is a")
    print("patch; changing the COMBINATION RULE is the fix, and two of the five options")
    print("there make this defect impossible rather than merely absent.")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
