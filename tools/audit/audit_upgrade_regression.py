#!/usr/bin/env python3
"""audit_upgrade_regression.py — is the UPGRADE actually better than the weapon it replaces?

    python tools/audit/audit_upgrade_regression.py [--armors Light,Medium,Heavy] [--baseline N]

⛔ WHY THIS EXISTS (maintainer, 2026-08-19), on the W24 A2 nuclear collapse:

    *"MonsterTank120mm -> CannonNuke_Heavy, MonsterTank120mmThermobaric -> CannonFire_Heavy
    don't make sense because the thermobaric version is the upgrade so it should be more powerful
    right? … the upgrade feels like a downgrade. We need to check all the upgrades like that so we
    don't accidentally downgrade the weapons."*

An upgrade is a PAIR of armaments on one actor, gated on the same condition — `!cond` fires as
built, `cond` fires once the upgrade lands. Nothing checked that the second is better than the
first. A W24 collapse can move the two halves onto families with OPPOSITE Versus profiles and
still pass every damage check, because the total is preserved on both sides.

MonsterTank is the case that prompted this: both halves land on the same geometry (Spread 800,
identical falloff) and the upgrade carries 1.5x the damage, so no damage guard fires — but
`CannonNuke` vs `CannonFire` is +43 Scout / +37 Light / +27 Medium against −49 None / −60 Flak.
The "upgrade" is +126% against infantry and +4% against the light vehicles a heavy tank exists to
kill. That is a role change sold as an upgrade.

WHAT IT MEASURES: effective per-shot damage `sum(main Damage x Versus[armor] / 100)` and effective
DPS (`x Burst / ReloadDelay`) for each half of the pair, per armor class. Flags an armor class
where the upgrade is WEAKER, and calls the pair ROLE-SHIFTED when it wins on some classes and
loses on others.

⚠ NOT EVERY REGRESSION IS A BUG. A specialist upgrade may trade deliberately — an AA variant
should lose ground damage. This reports for review; `--baseline N` turns on the ratchet.
"""
from __future__ import annotations

import argparse
import pathlib
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from cameo_model import Model  # noqa: E402

# The armor classes a ground attacker is normally judged on. Air/ARMOR/HAZMAT are excluded from
# the VERDICT (an AA or anti-shield upgrade legitimately trades them) but still printed.
CORE = ("Scout", "Light", "Medium", "Heavy", "Superheavy", "Steel", "None", "Concrete", "Wood")


def _f(v, default=0.0) -> float:
    try:
        return float(str(v).strip())
    except (TypeError, ValueError):
        return default


def weapon_profile(rs, name: str) -> dict | None:
    """Effective per-shot damage by armor + the cadence needed for DPS."""
    node = rs.resolve_weapon(name)
    if node is None:
        return None
    per_armor: dict[str, float] = {}
    total = 0.0
    for wh in node.children:
        if not wh.key.startswith("Warhead") or "Concrete" in wh.key:
            continue
        rel = (wh.get("ValidRelationships") or "").strip()
        if "Ally" in rel and "Enemy" not in rel:
            continue
        d = _f(wh.get("Damage"))
        if d <= 0 or "Percentage" in wh.key:
            continue
        total += d
        versus = next((c for c in wh.children if c.key == "Versus"), None)
        table = {v.key: _f(v.value, 100.0) for v in versus.children} if versus else {}
        for armor in set(CORE) | set(table):
            per_armor[armor] = per_armor.get(armor, 0.0) + d * table.get(armor, 100.0) / 100.0
    if total <= 0:
        return None
    reload_ = _f(node.get("ReloadDelay"), 25.0) or 25.0
    burst = _f(node.get("Burst"), 1.0) or 1.0
    return {"total": total, "per_armor": per_armor, "rate": burst / reload_}


def pairs(rs):
    """(actor, base_weapon, upgrade_weapon, condition) for every gated armament pair."""
    for actor in sorted(rs.actors):
        if actor.startswith("^"):
            continue
        node = rs.resolve(actor)
        if node is None:
            continue
        by_cond: dict[str, dict[str, str]] = {}
        for arm in node.children:
            if not arm.key.startswith("Armament"):
                continue
            weapon = (arm.get("Weapon") or "").strip()
            cond = (arm.get("RequiresCondition") or "").strip()
            if not weapon or not cond or "&&" in cond or "||" in cond:
                continue
            neg = cond.startswith("!")
            key = cond[1:].strip() if neg else cond
            by_cond.setdefault(key, {})["base" if neg else "up"] = weapon
        for cond, half in by_cond.items():
            if "base" in half and "up" in half and half["base"] != half["up"]:
                yield actor, half["base"], half["up"], cond


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--armors", default=",".join(CORE))
    ap.add_argument("--baseline", type=int, default=None)
    a = ap.parse_args()
    armors = [s.strip() for s in a.armors.split(",") if s.strip()]

    rs = Model().rs
    weaker, shifted, marginal, npairs = [], [], [], 0

    for actor, base_w, up_w, cond in pairs(rs):
        b, u = weapon_profile(rs, base_w), weapon_profile(rs, up_w)
        if not b or not u:
            continue
        npairs += 1
        losses, gains = [], []
        for armor in armors:
            bd = b["per_armor"].get(armor, 0.0) * b["rate"]
            ud = u["per_armor"].get(armor, 0.0) * u["rate"]
            if bd <= 0:
                continue
            ratio = ud / bd
            (losses if ratio < 0.995 else gains).append((armor, ratio))
        row = (actor, base_w, up_w, cond, sorted(losses, key=lambda x: x[1]), gains)
        if losses:
            (shifted if gains else weaker).append(row)
            continue
        # No outright loss — but an upgrade that is +4% on the armor the unit exists to fight and
        # +126% on something else has still changed the unit's ROLE and will FEEL like a downgrade.
        # Maintainer 2026-08-19 on MonsterTank: *"the upgrade feels like a downgrade."*
        ratios = [r for _a, r in gains]
        if len(ratios) >= 3 and min(ratios) < 1.10 and max(ratios) / min(ratios) >= 2.0:
            marginal.append((actor, base_w, up_w, cond,
                             sorted(gains, key=lambda x: x[1])[:4], max(ratios)))

    print("# audit_upgrade_regression — is the upgrade better than what it replaces?\n")
    print(f"Gated armament pairs found: **{npairs}**\n")

    def table(rows, title, note):
        print(f"## {title} ({len(rows)})\n")
        print(f"{note}\n")
        if not rows:
            print("_none_\n")
            return
        print("| actor | base → upgrade | condition | worst losses (upgrade DPS ÷ base DPS) |")
        print("|---|---|---|---|")
        for actor, bw, uw, cond, losses, _g in rows[:40]:
            worst = ", ".join(f"{ar} {rt:.2f}x" for ar, rt in losses[:4])
            print(f"| `{actor}` | `{bw}` → `{uw}` | `{cond}` | {worst} |")
        if len(rows) > 40:
            print(f"\n_… {len(rows) - 40} more_")
        print()

    table(weaker, "⛔ STRICTLY WEAKER — the upgrade loses on every core armor it changes",
          "No trade-off to justify these: the player pays for the upgrade and gets less.")
    table(shifted, "⚠ ROLE-SHIFTED — wins on some armor classes, loses on others",
          "Legitimate for a specialist (an AA variant should lose ground damage). A REGRESSION "
          "when the losses land on the armor classes the unit exists to fight — MonsterTank "
          "trading Scout/Light/Medium for infantry is the case that prompted this audit.")

    print(f"## ⚠ THIN MARGIN — wins everywhere, but barely where it counts ({len(marginal)})\n")
    print("The upgrade never loses, so no damage or regression check fires — yet it is worth only a "
          "few percent on some core armor classes while multiplying on others. That is a ROLE change "
          "wearing an upgrade's clothes, and the player feels it as a downgrade in the fight the unit "
          "was built for.\n")
    if marginal:
        print("| actor | base → upgrade | weakest core gains | best |")
        print("|---|---|---|--:|")
        for actor, bw, uw, _c, worst, best in marginal[:40]:
            w = ", ".join(f"{ar} {rt:.2f}x" for ar, rt in worst)
            print(f"| `{actor}` | `{bw}` → `{uw}` | {w} | {best:.2f}x |")
        if len(marginal) > 40:
            print(f"\n_… {len(marginal) - 40} more_")
        print()
    else:
        print("_none_\n")

    total = len(weaker) + len(shifted) + len(marginal)
    print(f"**total findings: {total}** ({len(weaker)} strictly weaker, {len(shifted)} role-shifted, "
          f"{len(marginal)} thin-margin)")
    if a.baseline is None:
        print("\n_no baseline set — reporting only. Measure on a settled tree, then ratchet DOWN._")
        return 0
    if total > a.baseline:
        print(f"\n**FAIL — {total} exceeds the baseline of {a.baseline}.**")
        return 1
    print(f"\n_at or below baseline ({a.baseline})._")
    return 0


if __name__ == "__main__":
    sys.exit(main())
