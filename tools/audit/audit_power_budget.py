#!/usr/bin/env python3
"""audit_power_budget.py — R2 detector (stacked multiplier budget).

For every buildable combat unit of every real faction, computes the
WORST-CASE stacked multiplier relative to its FRESH self:

  damage output  = Π beneficial FirepowerMultiplier × Π (100/ReloadDelayMultiplier)
  survivability  = Π (100/DamageMultiplier)            (beneficial = takes less)
  effective power = damage output × survivability

Only PERMANENTLY ATTAINABLE conditions count: those the unit itself grants
via GrantConditionOnPrerequisite (research/promotion tokens) or reaches via
GainsExperience rank conditions. Unconditional multipliers are the fresh
baseline; aura/external/battle-state conditions are temporary and excluded.
Flags products > 2.0 (MASTER_REPORT §7.2 R2).
"""

from __future__ import annotations

import re
import sys

from cameo_model import Model
from miniyaml import Node
from report import h1, h2, table

_ident = re.compile(r"[A-Za-z0-9_.\-]+")


def permanent_conditions(res: Node, faction_tokens: set[str]) -> set[str]:
    """Conditions this unit can permanently hold once upgrades/ranks land,
    limited to prerequisites the owning faction can actually attain."""
    out: set[str] = set()
    for trait in res.children:
        base = trait.key.split("@", 1)[0]
        if base == "GrantConditionOnPrerequisite":
            c = trait.get("Condition")
            prereqs = [t.strip().lstrip("~").lower()
                       for t in (trait.get("Prerequisites") or "").split(",")
                       if t.strip() and not t.strip().lstrip("~").startswith("!")]
            attainable = all(
                p in faction_tokens or p.startswith(Model.OPTION_TOKEN_PREFIXES)
                for p in prereqs)
            if c and attainable:
                out.add(c.lower())
        elif base == "GainsExperience":
            conds = trait.child("Conditions")
            if conds is not None:
                for lvl in conds.children:
                    out.add(lvl.value.lower())
    return out


def usable(cond_expr: str | None, permanent: set[str]) -> bool:
    """Count a multiplier only if its condition expression is satisfiable by
    permanent conditions alone (no negations of them, no external states)."""
    if not cond_expr:
        return False        # unconditional = fresh baseline, not stack growth
    e = cond_expr.lower()
    if "!" in e or "|" in e.replace("||", "|"):
        idents = set(_ident.findall(e))
        return bool(idents & permanent) and not (idents - permanent)
    idents = set(_ident.findall(e))
    idents = {i for i in idents if not i.isdigit()}
    return bool(idents) and idents <= permanent


def main() -> int:
    m = Model()
    rs = m.rs
    rows = []
    for fac in sorted(f.internal for f in m.real_factions()):
        fac_tokens = m.faction_tokens(fac)
        for lname in sorted(m.buildable_roster(fac)):
            res = rs.resolve(lname)
            if res is None or res.child("Health") is None:
                continue
            if m.unit_type(lname) not in {"inf", "veh", "air", "nav", "def"}:
                continue
            permanent = permanent_conditions(res, fac_tokens)
            fp = 1.0    # firepower product
            rof = 1.0   # rate-of-fire product
            surv = 1.0  # survivability product
            parts = []
            # Exclusive families: traits gated on `counter == N` (veterancy
            # style) can only have ONE member active — take the best, don't
            # multiply the whole ladder.
            best_exclusive: dict[tuple[str, str], tuple[int, str]] = {}
            independent: list[tuple[str, int, str]] = []
            for trait in res.children:
                base = trait.key.split("@", 1)[0]
                if base not in ("FirepowerMultiplier", "ReloadDelayMultiplier",
                                "DamageMultiplier"):
                    continue
                mod = trait.get("Modifier")
                if not mod:
                    continue
                try:
                    v = int(str(mod).split(",")[0])
                except ValueError:
                    continue
                cond = trait.get("RequiresCondition")
                if not usable(cond, permanent):
                    continue
                meq = re.fullmatch(r"\s*([\w.\-]+)\s*==\s*\d+\s*", cond or "")
                if meq:
                    fam = (base, meq.group(1).lower())
                    prev = best_exclusive.get(fam)
                    better = (prev is None
                              or (base == "FirepowerMultiplier" and v > prev[0])
                              or (base != "FirepowerMultiplier" and v < prev[0]))
                    if better:
                        best_exclusive[fam] = (v, trait.key)
                else:
                    independent.append((base, v, trait.key))

            chosen = independent + [(fam[0], v, key)
                                    for fam, (v, key) in best_exclusive.items()]
            for base, v, key in chosen:
                if base == "FirepowerMultiplier" and v > 100:
                    fp *= v / 100
                    parts.append(f"{key}={v}")
                elif base == "ReloadDelayMultiplier" and 0 < v < 100:
                    rof *= 100 / v
                    parts.append(f"{key}={v}")
                elif base == "DamageMultiplier" and 0 < v < 100:
                    # Modifier 0 = scripted invulnerability state, not power curve
                    surv *= 100 / v
                    parts.append(f"{key}={v}")
            damage = fp * rof
            power = damage * surv
            if power > 2.0:
                rows.append([fac, lname, f"{damage:.2f}", f"{surv:.2f}",
                             f"{power:.2f}", "; ".join(parts[:8])])

    rows.sort(key=lambda r: -float(r[4]))
    print(h1("audit_power_budget — worst-case stacked multipliers (R2)"))
    print(f"Units above the 2.0× effective-power budget: **{len(rows)}**\n")
    print(h2("Breaches (damage× × surv× = power×), largest first"))
    print(table(["faction", "unit", "damage×", "surv×", "power×", "contributing multipliers"],
                rows))
    print("\n_Worst-case assumes every research upgrade purchased and max "
          "veterancy reached, relative to the fresh unit. Temporary states "
          "(auras, damage states, support-power effects) are excluded, so "
          "real in-battle peaks can be even higher._\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
