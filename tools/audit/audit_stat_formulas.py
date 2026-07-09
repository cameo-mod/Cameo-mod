#!/usr/bin/env python3
"""audit_stat_formulas.py — house stat formulas (project rules, 2026-07).

Reference units that satisfy every rule: TD GDI Archer (gdiarcher) and the
Ordos Raider (raider.ordos).

  F1  Repairable.HpPerStep == Health.HP / 20            (non-infantry)
  F2  ChangesHealth@SelfHealing.Step == HP / 2500       (HP / 1000 for infantry)
  F3  infantry must NOT have Repairable
  F4  upgrade-gated Shielded.RegenAmount == 2 x SelfHealing Step
  F5  defenses: RevealsShroud.Range == max weapon range
  F6  anti-air / advanced defenses: DetectCloaked.Range == weapon range / 2
  F7  defenses: Power.Amount == -(Cost / 20)
  F8  vehicles: Mobile.TurnSpeed == Speed / 5
  F9  turreted vehicles: Turreted.TurnSpeed == Mobile.TurnSpeed
  F10 turretless (AttackFrontal*) vehicles: TurnSpeed == 2 x Speed / 5,
      EXCEPT artillery-template units (plain Speed / 5)
  F11 artillery / fire-support vehicles WITH a turret: must carry the Archer
      firing-slow pattern — GrantConditionOnAttack(firing) with
      RevokeDelay == weapon ReloadDelay / 2 and Speed/TurnSpeed/
      TurretTurnSpeed multipliers at 50
  F12 each faction's anti-air defense tower must be gated by the faction's
      radar-tier (Tier 2) building
  F13 each faction's advanced defense must be gated by the faction's
      tech-tier (Tier 3) building
      F12/F13 exemptions: factions with only one armed defense (Protoss
      photon cannon style), factions without identifiable radar/tech tier
      buildings, and Terran/Zerg (non-tiered tech trees).

Scope: buildable rosters of real factions. Tolerance ±1 on divisions.
"""

from __future__ import annotations

import re
import sys

from audit_outliers import cell_value
from cameo_model import Model
from report import h1, h2, table

ARTY_TEMPLATES = ("ArtilleryTemplate", "FireSupportTemplate", "ArtilleryShipTemplate")


def ivalue(node, *path) -> int | None:
    v = node.get(*path) if node else None
    if v is None:
        return None
    comps = cell_value(v)
    return comps[0] if comps else None


def close(actual, expected, tol=1) -> bool:
    return actual is not None and expected is not None and abs(actual - expected) <= tol


def max_weapon_range(m, res) -> int | None:
    best = None
    for arm in res.children_named("Armament"):
        wname = arm.get("Weapon")
        if not wname:
            continue
        w = m.rs.resolve_weapon(wname)
        r = ivalue(w, "Range") if w else None
        if r is not None and (best is None or r > best):
            best = r
    return best


def primary_reload(m, res) -> int | None:
    for arm in res.children_named("Armament"):
        wname = arm.get("Weapon")
        if wname:
            w = m.rs.resolve_weapon(wname)
            r = ivalue(w, "ReloadDelay") if w else None
            if r is not None:
                return r
    return None


def inherits_template(m, name, needles) -> bool:
    node = m.rs.actor(name)
    seen = set()

    def walk(n):
        if n is None:
            return False
        for _, t in m.rs.inherits_of(n):
            if any(x.lower() in t.lower() for x in needles):
                return True
            if t.lower() not in seen:
                seen.add(t.lower())
                if walk(m.rs.actor(t)):
                    return True
        return False
    return walk(node)


TIER_EXEMPT_FACTIONS = {"terran", "zerg"}   # non-tiered tech trees


def defense_tier_check(m: Model, rows: dict) -> None:
    """F12/F13 — AA defense gated by radar tier; advanced defense by tech tier."""
    rs = m.rs
    for fac in sorted(f.internal for f in m.real_factions()):
        if fac in TIER_EXEMPT_FACTIONS:
            continue
        roster = m.buildable_roster(fac)

        radars, techs, armed_defs = set(), set(), []
        radar_tokens, tech_tokens = set(), set()
        for lname in roster:
            res = rs.resolve(lname)
            if res is None:
                continue
            b = res.child("Buildable")
            queue = (b.get("Queue") or "").lower() if b else ""
            if res.child("Building") is not None:
                toks = m._provider_tokens(lname, res)
                if inherits_template(m, lname, ("RadarBuilding",)):
                    radars.add(lname)
                    radar_tokens |= toks
                if inherits_template(m, lname, ("IsTechnoBuilding",)):
                    techs.add(lname)
                    tech_tokens |= toks
            if ("defence" in queue or "defense" in queue) \
                    and res.children_named("Armament"):
                armed_defs.append(lname)

        # single-armed-defense factions (photon cannon style) are exempt
        if len(armed_defs) <= 1:
            continue

        for lname, needles, tier_tokens, tier_names, key, tier_label in (
            *[(d, ("AntiAirDefense",), radar_tokens, radars, "F12", "radar tier")
              for d in armed_defs],
            *[(d, ("AdvancedDefense",), tech_tokens, techs, "F13", "tech tier")
              for d in armed_defs],
        ):
            if not inherits_template(m, lname, needles):
                continue
            if not tier_names:
                rows[key].append([f"{fac}: {lname}",
                                  f"no {tier_label} building identified",
                                  "needs human decision"])
                continue
            res = rs.resolve(lname)
            prereqs = set(m.positive_prereqs(res))
            if not (prereqs & tier_tokens):
                rows[key].append([f"{fac}: {lname}",
                                  f"prereqs: {', '.join(sorted(prereqs)) or '(none)'}",
                                  f"must include {tier_label}: "
                                  f"{', '.join(sorted(tier_names))}"])


def main() -> int:
    m = Model()
    rs = m.rs
    rows = {k: [] for k in
            ("F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10",
             "F11", "F12", "F13")}

    names: set[str] = set()
    for f in m.real_factions():
        names |= m.buildable_roster(f.internal)
    inherited_f3: set[str] = set()

    for lname in sorted(names):
        res = rs.resolve(lname)
        if res is None or res.child("Health") is None:
            continue
        ut = m.unit_type(lname)
        hp = ivalue(res, "Health", "HP")
        b = res.child("Buildable")
        queue = (b.get("Queue") or "").lower() if b else ""
        is_def = "defence" in queue or "defense" in queue

        # F1/F2/F3 heal & repair maths
        rep = ivalue(res, "Repairable", "HpPerStep")
        heal = None
        for c in res.children:
            if c.key == "ChangesHealth@SelfHealing":
                comps = cell_value(c.get("Step") or "")
                heal = comps[0] if comps else None
        if hp:
            if ut == "inf":
                if res.child("Repairable") is not None:
                    local = rs.actor(lname).child("Repairable") is not None
                    if local:
                        rows["F3"].append([lname, "infantry declares Repairable locally", ""])
                    else:
                        inherited_f3.add(lname)
                if heal is not None and not close(heal, round(hp / 1000)):
                    rows["F2"].append([lname, f"Step {heal}", f"expected {round(hp/1000)} (HP {hp}/1000)"])
            elif ut in ("veh", "air", "nav"):
                if rep is not None and not close(rep, round(hp / 20)):
                    rows["F1"].append([lname, f"HpPerStep {rep}", f"expected {round(hp/20)} (HP {hp}/20)"])
                if heal is not None and not close(heal, round(hp / 2500)):
                    rows["F2"].append([lname, f"Step {heal}", f"expected {round(hp/2500)} (HP {hp}/2500)"])

        # F4 shields gated on upgrades
        for c in res.children:
            if c.key.split("@")[0] != "Shielded":
                continue
            rc = (c.get("RequiresCondition") or "")
            if not any(t.startswith("up") or "upgrade" in t for t in re.findall(r"[\w.]+", rc)):
                continue
            regen = ivalue(c, "RegenAmount") if False else None
            v = c.get("RegenAmount")
            regen = cell_value(v)[0] if v else None
            if heal is not None and regen is not None and not close(regen, 2 * heal):
                rows["F4"].append([lname, f"RegenAmount {regen}",
                                   f"expected {2*heal} (2 x SelfHealing {heal})"])

        # F5/F6/F7 defenses
        if is_def:
            wr = max_weapon_range(m, res)
            rsr = ivalue(res, "RevealsShroud", "Range")
            if wr is not None and rsr is not None and not close(rsr, wr, tol=32):
                rows["F5"].append([lname, f"RevealsShroud {rsr}", f"weapon range {wr}"])
            if inherits_template(m, lname, ("AntiAirDefense", "AdvancedDefense")):
                dc = ivalue(res, "DetectCloaked", "Range")
                if wr is not None:
                    if dc is None:
                        rows["F6"].append([lname, "DetectCloaked missing", f"expected {wr//2}"])
                    elif not close(dc, wr // 2, tol=32):
                        rows["F6"].append([lname, f"DetectCloaked {dc}", f"expected {wr//2} (range/2)"])
            cost = ivalue(res, "Valued", "Cost")
            pwr = ivalue(res, "Power", "Amount")
            if cost:
                if pwr is None:
                    rows["F7"].append([lname, "Power missing", f"expected {-(cost//20)}"])
                elif not close(pwr, -(cost // 20), tol=2):
                    rows["F7"].append([lname, f"Power {pwr}", f"expected {-(cost//20)} (-Cost/20)"])

        # F8-F11 vehicle turn maths
        if ut == "veh":
            mob = res.child("Mobile")
            spd = ivalue(res, "Mobile", "Speed")
            ts = ivalue(res, "Mobile", "TurnSpeed")
            turret = res.child("Turreted")
            frontal = any(c.key.split("@")[0].startswith("AttackFrontal")
                          for c in res.children)
            arty = inherits_template(m, lname, ARTY_TEMPLATES)
            if spd:
                base = round(spd / 5)
                if turret is None and frontal:
                    want = base if arty else 2 * base
                    label = "Speed/5 (artillery)" if arty else "2 x Speed/5 (turretless)"
                    if ts is not None and not close(ts, want):
                        rows["F10"].append([lname, f"TurnSpeed {ts} (Speed {spd})",
                                            f"expected {want} = {label}"])
                elif ts is not None and not close(ts, base):
                    rows["F8"].append([lname, f"TurnSpeed {ts} (Speed {spd})",
                                       f"expected {base} = Speed/5"])
                if turret is not None:
                    tts = ivalue(res, "Turreted", "TurnSpeed") or ivalue(turret, "TurnSpeed")
                    v = turret.get("TurnSpeed")
                    tts = cell_value(v)[0] if v else None
                    if tts is not None and ts is not None and tts != ts:
                        rows["F9"].append([lname, f"Turreted {tts} vs Mobile {ts}",
                                           "must match"])
            if arty and turret is not None:
                gca = None
                for c in res.children:
                    if c.key.split("@")[0] == "GrantConditionOnAttack":
                        gca = c
                slow = any(c.key.startswith("SpeedMultiplier@") and c.get("Modifier") == "50"
                           and "firing" in (c.get("RequiresCondition") or "")
                           for c in res.children)
                reload_d = primary_reload(m, res)
                if gca is None or not slow:
                    rows["F11"].append([lname, "firing-slow pattern missing",
                                        "see gdiarcher (GrantConditionOnAttack + 50% multipliers)"])
                else:
                    rd = cell_value(gca.get("RevokeDelay") or "")
                    rd = rd[0] if rd else None
                    if reload_d and rd is not None and not close(rd, reload_d // 2, tol=5):
                        rows["F11"].append([lname, f"RevokeDelay {rd}",
                                            f"expected {reload_d//2} (ReloadDelay {reload_d}/2)"])

    defense_tier_check(m, rows)

    total = sum(len(v) for v in rows.values())
    print(h1("audit_stat_formulas — house stat formulas"))
    print(f"Violations: **{total}** across {len(names)} roster actors "
          f"(reference-clean units: gdiarcher, raider.ordos)\n")
    titles = {
        "F1": "F1 — Repairable.HpPerStep ≠ HP/20",
        "F2": "F2 — SelfHealing Step ≠ HP/2500 (inf: HP/1000)",
        "F3": "F3 — infantry with Repairable",
        "F4": "F4 — upgrade shield RegenAmount ≠ 2×SelfHealing Step",
        "F5": "F5 — defense RevealsShroud.Range ≠ weapon range",
        "F6": "F6 — AA/advanced defense DetectCloaked.Range ≠ weapon range/2",
        "F7": "F7 — defense Power.Amount ≠ -Cost/20",
        "F8": "F8 — vehicle TurnSpeed ≠ Speed/5",
        "F9": "F9 — Turreted.TurnSpeed ≠ Mobile.TurnSpeed",
        "F10": "F10 — turretless TurnSpeed ≠ 2×Speed/5 (artillery: Speed/5)",
        "F11": "F11 — turreted artillery missing/incorrect firing-slow (Archer pattern)",
        "F12": "F12 — anti-air defense not gated by the faction's radar tier",
        "F13": "F13 — advanced defense not gated by the faction's tech tier",
    }
    for k in rows:
        print(h2(f"{titles[k]}  ({len(rows[k])})"))
        print(table(["actor", "actual", "expected"], rows[k]))
        if k == "F3" and inherited_f3:
            print(f"\n_{len(inherited_f3)} further infantry inherit Repairable "
                  "from the infantry base template (^DefaultInfantry "
                  "RepairActors: drfghosp… — unloaded Dark Reign hospitals). "
                  "One template-line fix covers them all._\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
