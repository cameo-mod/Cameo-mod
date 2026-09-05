#!/usr/bin/env python3
"""audit_infantry_class_bands — FORMULA_V2 §6b's range bands, measured against the tree.

    python tools/audit/audit_infantry_class_bands.py
    python tools/audit/audit_infantry_class_bands.py --md docs/audit/latest/infantry_class_bands.md

⛔ THE LAW — `docs/design/FORMULA_V2.md` §6b (maintainer design, rev. 2026-07-19):

  > "CONTIGUOUS half-open range bands: no unit can ever fall between classes again —
  >  the band DEFINES membership."

  | class          | band          | anchor r0 | status |
  |----------------|---------------|-----------|--------|
  | melee          | [1250, 2500)  | 1750      | anchor `asianalliance_alligator` @ 280 |
  | closecombat    | [2500, 4500)  | 3500      | LIVE, `td_gdi_shotgunner` @ 200 |
  | scout          | [4500, 5500)  | 5000      | LIVE, `naxis_naxiriflesoldier` @ 100 |
  | special forces | [5500, 6500]  | 6000      | LIVE, `japan_imperialscoutsman` @ 200 |

  > "Air is the special-forces class trait, baked into the baseline — hitting air is
  >  NEVER a per-unit special."

⚠ THE BOUNDARY AT 5500 IS THE ONE PLACE §6b CONTRADICTS ITSELF. Its table writes scout as the
CLOSED interval [4500, 5500] and special forces as "5500–6500", so 5500 belongs to both. Its own
prose settles it — "Boundary rule: a weapon at exactly 2500 is closecombat; exactly 4500 is scout
(half-open bands)" — so this audit reads every band half-open EXCEPT the top of the ladder, which
has nothing above it to hand 6500 to. Units landing exactly on 5500 are listed separately rather
than being silently assigned, because a boundary an audit invents is a boundary nobody ruled.

⭐ WHY THIS IS A SEPARATE AUDIT FROM `audit_class_templates.py`. That one asks *"does this unit
have exactly one class?"* — a structural question with a yes/no answer. This one asks *"is it the
RIGHT class?"*, which only §6b can answer, and only for the four classes that HAVE a band. Nine
further infantry classes (grenadier, heavy infantry, sniper, heavy sniper, rocket trooper, archer,
support, hero, flying) are listed "TBD" in §6b: a unit there cannot be out of band because there
is no band. They are reported as INTAKE CANDIDATES — measured, never judged.

⚠ SCOPE, and it is deliberately narrow. Only the four banded classes produce findings. A
grenadier at 6000 is not a defect; a SCOUT at 6000 is, because scout's band ends at 5500.

⚠ RANGE IS READ FROM THE RESOLVED WEAPON, THROUGH `miniyaml` (CLAUDE.md rule 8e) — never by
hand-parsing yaml. The armament taken is the actor's `Armament@PRIMARY`, falling back to the first
non-garrison armament that resolves to a weapon with a `Range`. Garrison armaments are skipped:
they are the building's reach, not the unit's.

⚠ AIR CAPABILITY IS `ValidTargets` CONTAINING `Air` ON ANY non-garrison armament, not only the
primary one. A unit whose anti-air is a second armament still hits air.

⛔ AND THE SKIP THAT ALMOST WENT UNREPORTED. A unit carrying TWO class templates has no single
class to check a band against, so it drops out of this measurement — silently, in the first
draft. Six units are in that state and one of them is **`japan_imperialscoutsman`, §6b's own
special-forces BASELINE**: it declares `Inherits@Template: ^SpecialForcesInfantryTemplate` and
also reaches `^ScoutInfantryTemplate` through `^RA1AlliesRifleInfantry`. The anchor of a class
was invisible to the audit of that class. They are now reported in §0 — "the tool could not
look" must never print the same as "the tool looked and found nothing".

ADVISORY in `run_all.sh`: every finding is either a maintainer class ruling or a yaml edit that
needs the boot gate, exactly like `class_redundancy` and `ifv_conditions`.
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

NOT_A_CLASS = {
    "^UpgradeTemplate", "^ResearchedUpgradeTemplate", "^PromotionUpgradeTemplate",
    "^UnitUpgradeTemplate", "^TeamUpgradeTemplate", "^TechUpgradeTemplate", "^DoctrineTemplate",
}
ADD_ON = {"^EpicVehicleTemplate", "^EpicAirUnitTemplate"}

# §6b's four banded classes: (template, low, high). Half-open [low, high) except the top.
BANDED = [
    ("^MeleeInfantryTemplate", "melee", 1250, 2500),
    ("^CloseCombatInfantryTemplate", "closecombat", 2500, 4500),
    ("^ScoutInfantryTemplate", "scout", 4500, 5500),
    ("^SpecialForcesInfantryTemplate", "special_forces", 5500, 6501),
]
BAND_OF = {t: (n, lo, hi) for t, n, lo, hi in BANDED}
# `^DogTemplate` inherits `^MeleeInfantryTemplate`, so its members are melee by the band law.
BAND_PARENT = {"^DogTemplate": "^MeleeInfantryTemplate"}

# §6b lists these with a TBD band. They cannot be out of band; they are measured, not judged.
UNBANDED_INFANTRY = [
    "^GrenadierInfantryTemplate", "^MortarInfantryTemplate", "^AntiTankAntiAirInfantryTemplate",
    "^HeavyInfantryTemplate", "^SniperInfantryTemplate", "^HeavySniperInfantryTemplate",
    "^RocketTrooperInfantryTemplate", "^ArcherInfantryTemplate", "^SupportInfantryTemplate",
    "^HeroInfantryTemplate", "^FlyingInfantryTemplate", "^MedicTemplate", "^MechanicTemplate",
]
INFANTRY = [t for t, _, _, _ in BANDED] + list(BAND_PARENT) + UNBANDED_INFANTRY


def inherit_values(node) -> list[str]:
    """EVERY `Inherits*` key — membership arrives as `Inherits@Template:` (see class_templates)."""
    return [c.value.strip() for c in node.children
            if c.key.split("@", 1)[0] == "Inherits" and isinstance(c.value, str)]


def band_for(rng, lo, hi):
    return rng is not None and lo <= rng < hi


def which_band(rng):
    if rng is None:
        return None
    for _, name, lo, hi in BANDED:
        if lo <= rng < hi:
            return name
    return "below" if rng < 1250 else "above"


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--md")
    args, _ = ap.parse_known_args()

    rs = miniyaml.Ruleset(ROOT)

    unreadable: list[tuple[str, str]] = []
    parents: dict[str, list[str]] = {}
    for name in rs.actors:
        try:
            parents[name] = inherit_values(rs.actor(name))
        except Exception as exc:                       # noqa: BLE001
            # Counted, never swallowed — a bare `except: continue` here reports zero and
            # prints the zero as a measurement (LESSONS_LEARNED, "Three ways I measured zero").
            unreadable.append((name, f"{type(exc).__name__}: {exc}"))
            parents[name] = []

    def tmpl_ancestors(t):
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

    wcache: dict[str, tuple] = {}

    def weapon_stats(wname):
        """(range, hits_air) for a resolved weapon, or (None, False)."""
        if wname in wcache:
            return wcache[wname]
        rng, air = None, False
        try:
            wn = rs.resolve_weapon(wname)
        except Exception as exc:                       # noqa: BLE001
            unreadable.append((wname, f"resolve_weapon: {type(exc).__name__}: {exc}"))
            wn = None
        if wn is not None:
            rc = wn.child("Range")
            if rc is not None and isinstance(rc.value, str):
                try:
                    rng = int(float(rc.value.strip().rstrip("c")))
                except ValueError:
                    rng = None
            vt = wn.child("ValidTargets")
            air = bool(vt is not None and isinstance(vt.value, str)
                       and "air" in vt.value.lower())
        wcache[wname] = (rng, air)
        return wcache[wname]

    def armaments(node):
        """Non-garrison armament weapons, PRIMARY first. Garrison reach is the building's."""
        out = []
        for c in node.children:
            if c.key.split("@", 1)[0] != "Armament":
                continue
            suffix = c.key.split("@", 1)[1] if "@" in c.key else ""
            if "GARRISON" in suffix.upper():
                continue
            nm = c.child("Name")
            if nm is not None and str(nm.value).strip() == "garrisoned":
                continue
            w = c.child("Weapon")
            if w is not None and isinstance(w.value, str) and w.value.strip():
                out.append((suffix, w.value.strip()))
        out.sort(key=lambda x: (0 if x[0].upper().startswith("PRIMARY") else 1))
        return out

    rows = []
    ambiguous: list[tuple[str, list[str]]] = []
    for name in sorted(parents):
        if name.startswith(("^", "-")):
            continue
        try:
            node = rs.resolve(name)
        except Exception as exc:                       # noqa: BLE001
            unreadable.append((name, f"resolve: {type(exc).__name__}: {exc}"))
            continue
        if node is None or node.child("Buildable") is None:
            continue
        anc = ancestors(name)
        if anc & NOT_A_CLASS:
            continue                                   # an upgrade is not a unit
        if node.child("Mobile") is None and node.child("Aircraft") is None:
            continue                                   # buildings are out of scope
        tmpl = {a for a in anc if a.endswith("Template") and a not in NOT_A_CLASS}
        superseded = set()
        for t in tmpl:
            superseded |= (tmpl_ancestors(t) & tmpl)
        tmpl -= superseded                             # most specific only
        full = sorted(tmpl - ADD_ON)
        if len(full) != 1:
            # ⛔ NOT A SILENT SKIP. Two classes means no band to check, but saying nothing is how
            # §6b's own special-forces baseline vanished from the special-forces measurement.
            if any(f in INFANTRY for f in full):
                ambiguous.append((name, full))
            continue
        if full[0] not in INFANTRY:
            continue

        arms = armaments(node)
        rng = weapon = None
        for _, w in arms:
            r, _a = weapon_stats(w)
            if r is not None:
                rng, weapon = r, w
                break
        air = any(weapon_stats(w)[1] for _, w in arms)
        cost = None
        v = node.child("Valued")
        if v is not None and v.child("Cost") is not None:
            try:
                cost = int(str(v.child("Cost").value).strip())
            except (TypeError, ValueError):
                pass
        rows.append(dict(actor=name, tmpl=full[0], cost=cost,
                         weapon=weapon, rng=rng, air=air, n_arm=len(arms)))

    out: list[str] = []
    w = out.append
    w("# audit_infantry_class_bands — FORMULA_V2 §6b measured against the tree\n")
    w("§6b: *\"CONTIGUOUS half-open range bands: no unit can ever fall between classes again —")
    w("the band DEFINES membership.\"* Four infantry classes have a band; the other nine are TBD")
    w("and are measured here without being judged.\n")
    if ambiguous:
        w("## 0. ⛔ Not measurable — two class templates, so no band to check\n")
        w("These carry more than one class template, so `audit_class_templates` already counts")
        w("them as defects. They matter HERE because a unit with two classes has two bands and")
        w("belongs to neither measurement — the first draft of this audit dropped them without")
        w("a word. One of them is §6b's own **special-forces baseline**, which means the anchor")
        w("of a class was invisible to the audit of that class.\n")
        w("| unit | class templates |")
        w("|---|---|")
        for n, full in sorted(ambiguous):
            w(f"| `{n}` | " + " + ".join(f"`{f}`" for f in full) + " |")
        w("")

    w("| class | band | members | in band | out of band |")
    w("|---|---|--:|--:|--:|")

    out_of_band, sf_no_air, air_outside_sf, on_boundary = [], [], [], []
    for tname, cname, lo, hi in BANDED:
        members = [r for r in rows
                   if r["tmpl"] == tname or BAND_PARENT.get(r["tmpl"]) == tname]
        good = [r for r in members if band_for(r["rng"], lo, hi)]
        bad = [r for r in members if r["rng"] is not None and not band_for(r["rng"], lo, hi)]
        out_of_band += [(cname, lo, hi, r) for r in bad]
        hi_label = f"{hi - 1}]" if hi > 6500 else f"{hi})"
        w(f"| `{cname}` | [{lo}, {hi_label} | {len(members)} | {len(good)} | **{len(bad)}** |")
        for r in members:
            if r["rng"] == 5500:
                on_boundary.append(r)
            if cname == "special_forces" and not r["air"]:
                sf_no_air.append(r)
            elif cname != "special_forces" and r["air"]:
                air_outside_sf.append((cname, r))

    w("")
    w("## 1. ⛔ Out of their own class's band")
    w("")
    w("The band **defines** membership, so each of these is a defect with exactly two legal")
    w("fixes: move the unit to the class its range names, or move the range into its class's")
    w("band. Both are maintainer calls; the second is a priced change and must go through the")
    w("pipeline.\n")
    if out_of_band:
        w("| unit | class (template) | band | range | lands in | hits air |")
        w("|---|---|---|--:|---|:-:|")
        for cname, lo, hi, r in sorted(out_of_band, key=lambda x: (x[0], -(x[3]["rng"] or 0))):
            w(f"| `{r['actor']}` | {cname} | [{lo}, {hi}) | {r['rng']:,} | "
              f"**{which_band(r['rng'])}** | {'yes' if r['air'] else 'no'} |")
    else:
        w("None.")

    w("")
    w("## 2. ⛔ Special forces that cannot hit air")
    w("")
    w("§6b: *\"Air is the special-forces class trait, baked into the baseline — hitting air is")
    w("NEVER a per-unit special.\"* A special-forces unit with no air-capable armament")
    w("contradicts the class definition.\n")
    if sf_no_air:
        w("| unit | range | primary weapon | cost |")
        w("|---|--:|---|--:|")
        for r in sf_no_air:
            w(f"| `{r['actor']}` | {r['rng'] or 0:,} | `{r['weapon'] or '—'}` | "
              f"{r['cost'] if r['cost'] is not None else '—'} |")
    else:
        w("None.")

    w("")
    w("## 3. ⚠ Air capability outside special forces")
    w("")
    w("The 2026-07-20 roster sweep moved a list of units *\"→ scout (lose air)\"*. Air is the")
    w("special-forces trait, so a banded non-SF unit that hits air is either a missed move or a")
    w("deliberate exception. Reported, never auto-judged — melee and closecombat were not part")
    w("of that sweep.\n")
    if air_outside_sf:
        w("| unit | class | range | armaments |")
        w("|---|---|--:|--:|")
        for cname, r in sorted(air_outside_sf, key=lambda x: (x[0], x[1]["actor"])):
            w(f"| `{r['actor']}` | {cname} | {r['rng'] or 0:,} | {r['n_arm']} |")
    else:
        w("None.")

    w("")
    w("## 4. Intake candidates — the nine TBD classes, measured")
    w("")
    w("A unit here is **not** a defect: its class has no band. The column that matters is where")
    w("its range lands, because that is the class §6b would give it if its own class stays")
    w("bandless. This is the unapplied half of the 2026-07-21 split — see")
    w("[`../../design/CLASS_MOVES.md`](../../design/CLASS_MOVES.md) §0.\n")
    w("| class (template) | members | melee | closecombat | scout | special forces | above | below | no range |")
    w("|---|--:|--:|--:|--:|--:|--:|--:|--:|")
    for tname in UNBANDED_INFANTRY:
        members = [r for r in rows if r["tmpl"] == tname]
        if not members:
            continue
        c = collections.Counter(which_band(r["rng"]) for r in members)
        w(f"| `{tname}` | {len(members)} | {c['melee']} | {c['closecombat']} | {c['scout']} | "
          f"{c['special_forces']} | {c['above']} | {c['below']} | {c[None]} |")

    w("")
    w("## 5. Measurement gaps")
    w("")
    norange = [r for r in rows if r["rng"] is None]
    w(f"* infantry-class units measured: **{len(rows)}**")
    w(f"* no armament with a resolvable `Range`: **{len(norange)}** — "
      "unarmed support, or a weapon this resolver could not reach")
    if norange:
        w("")
        for r in sorted(norange, key=lambda x: x["actor"]):
            w(f"  * `{r['actor']}` ({r['tmpl']}, {r['n_arm']} armament(s))")
    if on_boundary:
        w("")
        w(f"* ⚠ exactly on the **5500** boundary, which §6b assigns to two classes at once: "
          f"**{len(on_boundary)}** — " + ", ".join(f"`{r['actor']}`" for r in on_boundary))
    if unreadable:
        w("")
        w(f"* ⚠ could not be read: **{len(unreadable)}**")
        for n, why in unreadable[:20]:
            w(f"  * `{n}` — {why}")

    text = "\n".join(out) + "\n"
    if args.md:
        p = pathlib.Path(args.md)
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text, encoding="utf-8")
    else:
        print(text)

    findings = len(out_of_band) + len(sf_no_air)
    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main())
