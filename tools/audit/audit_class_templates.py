#!/usr/bin/env python3
"""audit_class_templates — every buildable actor must inherit EXACTLY ONE class template.

    python tools/audit/audit_class_templates.py
    python tools/audit/audit_class_templates.py --md docs/audit/latest/class_templates.md

⛔ THE LAW (maintainer, 2026-09-02):

  > "Every buildable actor must have an inherited template so the unit can be classified.
  >  If a buildable unit doesn't have an inherited template it's wrong and a real defect,
  >  and if it inherits multiple templates that's also a real defect -- with the only
  >  exception being the epic vehicle and epic aircraft, which is like an add-on template
  >  but not a full template by itself."

⭐ AND THIS IS THE CLASSIFICATION THE BALANCE PIPELINE MUST USE. Not `design.class_anchor` in
`docs/balance/*.json` -- that tag is a DRIFTED COPY: measured 2026-09-02, only 8 of 27 classes
agreed with the templates, the ledger under-tagging by as much as +48 (`heavy_infantry`: 50
structural, 2 tagged) and over-tagging `special_forces` by 11. The yaml is the taxonomy; the
ledger is a projection of it that has rotted.

⚠ MEMBERSHIP IS A **KEYED** INHERIT, WHICH IS WHY IT IS EASY TO MISS:

    td_gdi_grenadier:
        Inherits: ^Soldier
        Inherits@Template: ^GrenadierInfantryTemplate      <-- the class

A traversal that follows only the bare `Inherits:` key finds NOTHING and reports zero members for
every class in the mod. That happened; see `docs/LESSONS_LEARNED.md` -> "Three ways I measured
zero". This audit walks EVERY `Inherits*` key, transitively.

⚠ UPGRADE TEMPLATES ARE NOT CLASS TEMPLATES. `^UpgradeTemplate`, `^ResearchedUpgradeTemplate`,
`^PromotionUpgradeTemplate`, `^UnitUpgradeTemplate`, `^TeamUpgradeTemplate`, `^TechUpgradeTemplate`
and `^DoctrineTemplate` describe upgrades, not units, and are excluded from the count.

⚠ SCOPE: MOBILE UNITS. Of 2,166 buildable actors, 632 are upgrades/promotions (not units at all)
and 444 are buildings. The law is about classifying UNITS, so the defect count is taken over actors
carrying `Mobile:` or `Aircraft:`. Buildings are reported separately as a SCOPE QUESTION rather
than as defects: defence buildings DO carry templates (`^BasicDefenseTemplate`,
`^AdvancedDefenseTemplate`, `^SuperDefenseTemplate`, `^BunkerTemplate`) while production buildings
carry none, and whether a barracks should be classified is a maintainer ruling, not a bug.

⛔ TEMPLATES INHERIT TEMPLATES, AND COUNTING TRANSITIVELY CALLS THAT A DEFECT.
`^UnarmedTransportHelicopterTemplate` declares `Inherits@Template: ^HelicopterTemplate`, and
`^DogTemplate` declares `Inherits: ^MeleeInfantryTemplate`. A chinook that correctly names ONLY the
transport template therefore has TWO templates in its ancestry, and a transitive count reports it as
a multi-class defect. The first run of this audit did exactly that and reported 18 defects, of which
**12 were this bug**. Only the MOST SPECIFIC template counts: any template that is a strict ancestor
of another template the unit carries is dropped before counting.

⚠ THE ADD-ON EXCEPTION IS EXACTLY TWO TEMPLATES. `^EpicVehicleTemplate` and `^EpicAirUnitTemplate`
layer ON TOP of a full class template, so a unit carrying one of them plus one full template is
CORRECT. A unit carrying an add-on and NOTHING else is still a defect -- it has no class.
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
import miniyaml  # noqa: E402

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

# Templates that describe an UPGRADE rather than a unit class.
NOT_A_CLASS = {
    "^UpgradeTemplate", "^ResearchedUpgradeTemplate", "^PromotionUpgradeTemplate",
    "^UnitUpgradeTemplate", "^TeamUpgradeTemplate", "^TechUpgradeTemplate", "^DoctrineTemplate",
}
# Layered on top of a full class template; never a class on their own.
# ⚠ A READING OF EACH COHORT'S SHARED SIGNATURE, NOT A DECISION. Three of the templates named
# here DO NOT EXIST YET (`^TransportTemplate`, `^SuicideVehicleTemplate`, `^DeployVehicleTemplate`)
# and are marked NEW — the maintainer has ruled that transports and suicide units should each get
# their own template (2026-09-02); the deploy/MCV one is unruled. Anything not listed prints
# "needs a ruling" rather than being guessed into a class.
PROPOSED = {
    ("^Soldier", "^RA2Infantry", "^SelectableSupportUnit"): "`^SupportInfantryTemplate` — engineers",
    ("^Soldier", "^TDRAInfantry", "^SelectableSupportUnit"): "`^SupportInfantryTemplate` — engineers",
    ("^BoatUnit", "^UnarmedCargoVehicle"): "**NEW** `^TransportTemplate` — naval transport (ruled)",
    ("^Helicopter", "^TSRenderVoxel"): "`^UnarmedTransportHelicopterTemplate` (exists)",
    ("^AirstrikePlane", "^CloakedAircraft", "^D2KPaletteRender"): "`^UnarmedTransportHelicopterTemplate` — carryalls",
    ("^Vehicle", "^AutoTargetGroundAssaultMove", "^PrioritizeDefence"): "**NEW** `^SuicideVehicleTemplate` (ruled)",
    ("^Vehicle", "^SelectableSupportUnit"): "`^SupportVehicleTemplate`",
    ("^Tank", "^TSRenderVoxel", "^GenericGroundDetector"): "`^SupportVehicleTemplate` — sensor/stealth",
    ("^WC2Critter",): "⚠ not player-controlled — EXEMPT or a critter class?",
}

ADD_ON = {"^EpicVehicleTemplate", "^EpicAirUnitTemplate"}


def inherit_values(node) -> list[str]:
    """EVERY `Inherits*` key, not just the bare one -- the class arrives as `Inherits@Template:`."""
    return [c.value.strip() for c in node.children
            if c.key.split("@", 1)[0] == "Inherits" and isinstance(c.value, str)]


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--md")
    args, _ = ap.parse_known_args()

    rs = miniyaml.Ruleset(ROOT)

    unreadable = []
    parents: dict[str, list[str]] = {}
    for name in rs.actors:
        try:
            parents[name] = inherit_values(rs.actor(name))
        except Exception as exc:                      # noqa: BLE001
            # ⛔ COUNTED AND REPORTED, NEVER SWALLOWED. A bare `except: continue` here turns every
            # failure into a zero and prints the zero as a measurement.
            unreadable.append((name, f"{type(exc).__name__}: {exc}"))

    def tmpl_ancestors(t):
        """The templates a TEMPLATE itself inherits — a sub-template is not a second class."""
        seen, stack = set(), list(parents.get(t, []))
        while stack:
            x = stack.pop()
            if x in seen:
                continue
            seen.add(x)
            stack.extend(parents.get(x, []))
        return {a for a in seen if a.endswith("Template")}

    def ancestors(name, seen=None, depth=0):
        seen = set() if seen is None else seen
        if depth > 24:
            return seen
        for p in parents.get(name, []):
            if p not in seen:
                seen.add(p)
                ancestors(p, seen, depth + 1)
        return seen

    # Every ^*Template DECLARED in the rules, so a template nothing inherits can be reported.
    declared = {n for n in rs.actors
                if n.startswith("^") and n.endswith("Template") and n not in NOT_A_CLASS}
    used: set[str] = set()

    missing, multiple, addon_only, ok = [], [], [], 0
    n_upgrade = n_building = 0
    buildings_untemplated: list[str] = []
    per_class = collections.Counter()
    for name in sorted(parents):
        if name.startswith(("^", "-")):
            continue
        try:
            node = rs.resolve(name)
        except Exception as exc:                      # noqa: BLE001
            unreadable.append((name, f"resolve: {type(exc).__name__}: {exc}"))
            continue
        if node.child("Buildable") is None:
            continue                                   # not buildable: the law does not reach it

        anc = ancestors(name)
        if anc & NOT_A_CLASS:
            n_upgrade += 1
            continue                                   # an upgrade is not a unit
        # ⚠ Record template USAGE before any scope skip, or a template used only by buildings
        # reports as dead. That bug produced `^BasicDefenseTemplate` in the dead list on the
        # first run, next to the defence buildings that plainly inherit it.
        anc_tmpl = {a for a in anc if a.endswith("Template") and a not in NOT_A_CLASS}
        # Keep only the MOST SPECIFIC: drop any template that another kept template inherits.
        superseded = set()
        for t in anc_tmpl:
            superseded |= (tmpl_ancestors(t) & anc_tmpl)
        anc_tmpl -= superseded
        used |= anc_tmpl
        is_mobile = node.child("Mobile") is not None or node.child("Aircraft") is not None
        if not is_mobile:
            n_building += 1
            if not anc_tmpl:
                buildings_untemplated.append(name)
            continue                                   # scope question, reported separately

        tmpl = anc_tmpl          # already most-specific; see the header
        full = sorted(tmpl - ADD_ON)
        addons = sorted(tmpl & ADD_ON)
        cost = None
        v = node.child("Valued")
        if v is not None and v.child("Cost") is not None:
            try:
                cost = int(v.child("Cost").value)
            except (TypeError, ValueError):
                pass

        # ⭐ THE SIGNATURE IS WHAT MAKES 91 DEFECTS INTO ~9 RULINGS. An untemplated actor still
        # inherits BEHAVIOUR templates (^Soldier, ^BoatUnit, ^Helicopter, ^SelectableSupportUnit
        # ...), and units that need the same class share those. Grouping on the signature turns
        # "67 actors to classify one by one" into a handful of cohorts — which is PRIORITY 0's
        # own instruction: work the top level, not the single unit.
        sig = tuple(v for v in inherit_values(rs.actor(name)) if not v.endswith("Template"))[:3]
        if len(full) == 1:
            ok += 1
            per_class[full[0]] += 1
        elif not full and addons:
            addon_only.append((name, cost, addons))
        elif not full:
            missing.append((name, cost, sig))
        else:
            multiple.append((name, cost, full, addons))

    units = ok + len(missing) + len(multiple) + len(addon_only)
    buildable = units + n_upgrade + n_building
    out = []
    w = out.append
    w("# audit_class_templates — one class template per buildable actor\n")
    w("Maintainer law, 2026-09-02: a buildable actor with **no** class template is a defect, and one")
    w("with **more than one** is a defect. `^EpicVehicleTemplate` and `^EpicAirUnitTemplate` are")
    w("ADD-ONS that layer on top of a full class and do not count as the class themselves.\n")
    w("⭐ This is the classification the **balance pipeline must use** — not `design.class_anchor`")
    w("in the ledgers, which is a drifted copy (8 of 27 classes agreed when measured).\n")
    w(f"* buildable actors: **{buildable}** — of which **{n_upgrade}** upgrades/promotions and")
    w(f"  **{n_building}** buildings are out of scope (see the header), leaving **{units}** units\n")
    w(f"* exactly one class template: **{ok}** ({ok/units:.0%} of units)" if units else "")
    w(f"* ⛔ NO class template: **{len(missing)}**")
    w(f"* ⛔ MORE THAN ONE class template: **{len(multiple)}**")
    w(f"* ⛔ add-on only, no full class: **{len(addon_only)}**")
    if unreadable:
        w(f"* ⚠ actors that could not be read: **{len(unreadable)}** (listed at the end)")
    w("")

    if missing:
        # ⭐ PRIORITY 0 item 2 AS A DECISION LIST, NOT A BACKLOG. The 67 untemplated actors are
        # not 67 rulings: they share BEHAVIOUR templates, and units needing the same class share
        # those. Cohorts below 3 stay in the singles list rather than being padded into a group.
        by_sig = collections.defaultdict(list)
        for n, c, sig in missing:
            by_sig[sig].append((n, c))
        cohorts = sorted(((k, v) for k, v in by_sig.items() if len(v) >= 3),
                         key=lambda kv: -len(kv[1]))
        singles = sorted((x for k, v in by_sig.items() if len(v) < 3 for x in v))
        w(f"## ⭐ The 67, grouped — {len(cohorts)} cohorts and {len(singles)} singles\n")
        w("Grouped on the BEHAVIOUR templates each actor already inherits, so a cohort is a")
        w("single ruling rather than N. ⚠ The `proposal` column is a READING of the shared")
        w("signature, never a decision: three of the templates it names do not exist yet.\n")
        w("| shared inherits | actors | proposal |")
        w("|---|--:|---|")
        for sig, actors in cohorts:
            w(f"| {' + '.join('`'+x+'`' for x in sig) or '—'} | **{len(actors)}** | "
              f"{PROPOSED.get(sig, '⚠ needs a ruling')} |")
        w("")
        for sig, actors in cohorts:
            w(f"**{' + '.join('`'+x+'`' for x in sig) or '—'}** → {PROPOSED.get(sig, '⚠ needs a ruling')}\n")
            w("  " + " · ".join(f"`{n}`" for n, _c in sorted(actors)) + "\n")
        if singles:
            w(f"### ⚠ Singles — {len(singles)}, each its own ruling\n")
            w("  " + " · ".join(f"`{n}`" for n, _c in singles) + "\n")

    if missing:
        w(f"## ⛔ No class template ({len(missing)})\n")
        w("These cannot be classified, so the pipeline cannot price them.\n")
        w("| actor | cost |")
        w("|---|--:|")
        for n, c, _sig in sorted(missing, key=lambda r: (-(r[1] or 0), r[0])):
            w(f"| `{n}` | {c if c is not None else '—'} |")
        w("")

    if multiple:
        w(f"## ⛔ More than one class template ({len(multiple)})\n")
        w("A unit in two classes is priced against two anchors; the class system stops being a")
        w("partition and every per-class distribution double-counts it.\n")
        by_pair = collections.defaultdict(list)
        for n, c, full, add in multiple:
            by_pair[tuple(full)].append((n, c))
        w("⚠ **Grouped by the pair, because the shape of the group is the ruling.** A pair that")
        w("recurs across many unrelated factions is probably a SPECIALISATION that should join")
        w("`^EpicVehicleTemplate` as an add-on; a one-off pair is a genuine mis-tag.\n")
        for pair, actors in sorted(by_pair.items(), key=lambda kv: (-len(kv[1]), kv[0])):
            w(f"**{' + '.join('`'+p+'`' for p in pair)}** — {len(actors)} actor(s)\n")
            for n, c in sorted(actors):
                w(f"* `{n}` ({c if c is not None else '—'})")
            w("")

    if addon_only:
        w(f"## ⛔ Add-on template but no class ({len(addon_only)})\n")
        w("`^EpicVehicleTemplate` / `^EpicAirUnitTemplate` layer on top of a class; alone they are")
        w("not one.\n")
        w("| actor | cost | add-on |")
        w("|---|--:|---|")
        for n, c, add in sorted(addon_only):
            w(f"| `{n}` | {c if c is not None else '—'} | {', '.join('`'+a+'`' for a in add)} |")
        w("")

    if buildings_untemplated:
        w(f"## ⚠ SCOPE QUESTION — buildings with no class template ({len(buildings_untemplated)})\n")
        w("Not counted as defects. Defence buildings already carry `^BasicDefenseTemplate` /")
        w("`^AdvancedDefenseTemplate` / `^SuperDefenseTemplate` / `^BunkerTemplate`; these do not.")
        w("Whether a barracks or a refinery should be classifiable is a maintainer ruling.\n")
        w("First 30: " + ", ".join(f"`{b}`" for b in sorted(buildings_untemplated)[:30]))
        w("")

    # ⚠ A BASE TEMPLATE IS USED BY EVERYTHING THAT USES ITS SUB-TEMPLATES. `used` now holds only
    # the MOST SPECIFIC template per unit, so `^HelicopterTemplate` would report dead the day every
    # helicopter became a transport. Close it upward before subtracting.
    used_closed = set(used)
    for t in list(used):
        used_closed |= tmpl_ancestors(t)
    dead = sorted(declared - used_closed - ADD_ON)
    if dead:
        w(f"## ⛔ Dead class templates ({len(dead)})\n")
        w("Declared in the rules and inherited by **nothing**. A class whose template is dead has")
        w("no structural members at all, however many the ledger tags.\n")
        w("| template | a class anchor points at it? |")
        w("|---|---|")
        anchor_classes = {
            "^ArcherInfantryTemplate": "`archer` — **and it is SIGNED**",
            "^HeavySniperInfantryTemplate": "`heavy_sniper` — **and it is SIGNED**",
            "^RocketTrooperInfantryTemplate": "`rocket_trooper`",
        }
        for t in dead:
            w(f"| `{t}` | {anchor_classes.get(t, 'no')} |")
        w("")

    if per_class:
        w("## The taxonomy as the yaml actually defines it\n")
        w("Buildable actors per class template. **This is the member list the balance pipeline")
        w("should read.**\n")
        w("| class template | buildable members |")
        w("|---|--:|")
        for t, n in sorted(per_class.items(), key=lambda kv: (-kv[1], kv[0])):
            w(f"| `{t}` | {n} |")
        w("")

    if unreadable:
        w(f"## ⚠ Unreadable actors ({len(unreadable)})\n")
        for n, e in unreadable[:40]:
            w(f"* `{n}` — {e}")
        if len(unreadable) > 40:
            w(f"* … and {len(unreadable)-40} more")
        w("")

    bad = len(missing) + len(multiple) + len(addon_only)
    w(f"**{'FAIL' if bad else 'PASS'}** — {bad} buildable actor(s) violate the one-class-template law."
      if bad else "**PASS** — every buildable actor carries exactly one class template.")

    text = "\n".join(out)
    print(text)
    if args.md:
        pathlib.Path(args.md).write_text(text + "\n", encoding="utf-8")
    return 1 if bad else 0


if __name__ == "__main__":
    raise SystemExit(main())
