#!/usr/bin/env python3
"""audit_warhead_split.py — guard against the multi-warhead over-damage bug.

Background (2026-07-22 regression, INCIDENT: commit 04de392b3):
Cameo weapons deliberately stack several offensive `Warhead@X: SpreadDamage`
nodes — one per inherited weapon-class template — and the engine detonates
ALL of them on the target, so the weapon's effective per-shot damage is the
SUM (formula.spread_damage_sum). A balance pass wrote a single design number
onto EVERY warhead of such weapons, multiplying their real damage by the
warhead count (e.g. the TD Nod Stealth Soldier's dart went 5 x 2000 -> 5 x
22000 = 110000/shot).

The pipeline now splits an edited TOTAL back across warheads proportionally
(formula.distribute_damage), so it can never broadcast again. This audit is
the belt-and-suspenders that FAILS the suite if the fingerprint ever
reappears from a hand edit or a foreign tool:

  FAIL 1 (broadcast fingerprint): a weapon with >=2 MAIN SpreadDamage
         warheads AND >=1 side warhead (`*FriendlyFire` / `*ExtraDamage`)
         where EVERY SpreadDamage warhead has the identical, non-zero
         Damage. Healthy stacks keep their side warheads BELOW the mains.

  FAIL 2 (own-side splash exceeds the shot): a `*FriendlyFire` SpreadDamage
         warhead whose Damage is greater than the largest main warhead.

It also prints an informational review list of high uniform stacks
(>=3 equal mains at >= REVIEW_DMG each) — allowed, but worth a glance.
"""
from __future__ import annotations

import sys

from cameo_model import Model
from report import h1, h2, table

REVIEW_DMG = 8000

# ---------------------------------------------------------------------------
# FAIL 3 — the BROADCAST FINGERPRINT ITSELF, on a ratchet.
#
# ⚠ Measured 2026-08-17: FAIL 1 requires ">=2 mains AND >=1 SIDE warhead, all identical".
# That side-warhead precondition made it report **4** weapons while the fingerprint it
# describes — every MAIN at one identical value — is present on **874** of the 973 multi-main
# fired weapons. The type filter was never the problem (AreaDamage is counted, above); the
# extra condition was.
#
# Widening FAIL 1 outright would turn the whole suite red on 874 weapons of pre-existing W24
# debt and block every commit until W24 lands, so this is a RATCHET instead: it fails only
# when the count RISES above the recorded baseline. New broadcasts are caught immediately; the
# existing debt stays visible without holding the gate hostage.
#
# ⬇ LOWER THIS as W24 collapses weapons. It must never be raised — a rise means a hand edit or
# a foreign tool reintroduced the bug the pipeline's distribute_damage exists to prevent.
#
# ⚠ 982, not the 874 quoted in the W24 diagnosis: that figure counts only weapons FIRED by a
# concrete actor, while this audit scans EVERY concrete weapon (`rs.weapons`), fired or not. Two
# populations, both correct for their own question — don't reconcile them by changing one.
BROADCAST_BASELINE = 982


def _int(v) -> int:
    try:
        return int(float(v))
    except (TypeError, ValueError):
        return 0


def classify_warheads(resolved):
    """(mains, friendlyfire, extradamage) as lists of (tag, damage) for the
    SpreadDamage warheads; HealthPercentageDamage/`*Percentage` ignored."""
    mains, ff, extra = [], [], []
    for c in resolved.children:
        # AreaDamage is the Cameo drop-in for SpreadDamage (baked FF + rings);
        # classify it identically. AreaDamagePercentage stays ignored like %.
        if not c.key.startswith("Warhead@") or c.value not in ("SpreadDamage", "AreaDamage"):
            continue
        tag = c.key.split("@", 1)[1]
        low = tag.lower()
        d = _int(c.get("Damage"))
        if low.endswith("percentage"):
            continue
        if low.endswith("friendlyfire"):
            ff.append((tag, d))
        elif low.endswith("extradamage"):
            extra.append((tag, d))
        else:
            mains.append((tag, d))
    return mains, ff, extra


def main() -> int:
    m = Model()
    rs = m.rs

    broadcast_rows = []   # FAIL 1
    ff_rows = []          # FAIL 2
    review_rows = []      # informational
    uniform_mains = []    # FAIL 3 — the fingerprint itself, ratcheted

    for wname in sorted(rs.weapons):
        if wname.startswith("^"):
            continue
        resolved = rs.resolve_weapon(wname)
        if resolved is None:
            continue
        mains, ff, extra = classify_warheads(resolved)
        if len(mains) < 2:
            continue
        main_dmgs = [d for _, d in mains]
        mx = max(main_dmgs)
        sides = ff + extra
        all_sd = main_dmgs + [d for _, d in sides]

        # FAIL 1 — everything broadcast to one identical, non-zero value
        if sides and len(set(all_sd)) == 1 and all_sd[0] > 0:
            broadcast_rows.append([
                wname, str(len(mains)), str(len(sides)), str(all_sd[0])])

        # FAIL 2 — friendly fire louder than the offensive shot
        for tag, d in ff:
            if d > mx:
                ff_rows.append([wname, tag, str(d), str(mx)])

        # FAIL 3 — every MAIN identical, regardless of side warheads or damage size.
        # This is the fingerprint FAIL 1 describes but does not catch.
        if len(set(main_dmgs)) == 1 and main_dmgs[0] > 0:
            uniform_mains.append([wname, str(len(mains)), str(main_dmgs[0]),
                                  str(main_dmgs[0] * len(mains))])

        # review — big uniform stacks (allowed, but flag for a look)
        if len(set(main_dmgs)) == 1 and len(mains) >= 3 and mx >= REVIEW_DMG:
            review_rows.append([
                wname, str(len(mains)), str(mx), str(mx * len(mains))])

    out = [h1("Warhead-split guard (multi-warhead over-damage)")]
    over_baseline = len(uniform_mains) > BROADCAST_BASELINE
    failed = bool(broadcast_rows or ff_rows or over_baseline)

    out.append(h2(f"FAIL 1 — broadcast fingerprint ({len(broadcast_rows)})"))
    if broadcast_rows:
        out.append("Every SpreadDamage warhead (mains + sides) shares one "
                   "identical value — the 2026-07-22 broadcast bug. Fix by "
                   "editing the per-shot TOTAL through the workbook so "
                   "`distribute_damage` splits it, or by restoring the "
                   "intended per-warhead values.\n")
        out.append(table(["weapon", "mains", "sides", "damage"], broadcast_rows))
    else:
        out.append("None. ✅\n")

    out.append(h2(f"FAIL 2 — FriendlyFire louder than the shot ({len(ff_rows)})"))
    if ff_rows:
        out.append(table(["weapon", "warhead", "ff_damage", "max_main"], ff_rows))
    else:
        out.append("None. ✅\n")

    out.append(h2(f"FAIL 3 — every MAIN identical, on a ratchet "
                  f"({len(uniform_mains)} vs baseline {BROADCAST_BASELINE})"))
    out.append(
        "This is the fingerprint FAIL 1 *describes* but cannot catch: FAIL 1 also requires a "
        "SIDE warhead, which is why it reports "
        f"{len(broadcast_rows)} where the fingerprint is on {len(uniform_mains)}.\n")
    if over_baseline:
        out.append(f"**FAIL — {len(uniform_mains)} exceeds the baseline of "
                   f"{BROADCAST_BASELINE}.** A weapon just had one damage number broadcast "
                   "across its mains. Edit the per-shot TOTAL through the workbook so "
                   "`formula.distribute_damage` splits it.\n")
    else:
        out.append(f"_at or below baseline_ — pre-existing **W24** debt "
                   f"({len(uniform_mains)} weapons), not a regression. The ratchet catches new "
                   "broadcasts without blocking every commit on the existing pile. "
                   "**Lower `BROADCAST_BASELINE` as W24 collapses weapons; never raise it.**\n")
    out.append(table(["weapon", "mains", "per_warhead", "total"], uniform_mains[:40]))
    if len(uniform_mains) > 40:
        out.append(f"\n_... and {len(uniform_mains) - 40} more._\n")

    out.append(h2(f"Review — high uniform stacks (informational, {len(review_rows)})"))
    if review_rows:
        out.append(f"Allowed, but {REVIEW_DMG}+ per-warhead x N is a big total "
                   "— confirm it is intended (not flattening residue).\n")
        out.append(table(["weapon", "mains", "per_warhead", "total"],
                         review_rows[:120]))
    else:
        out.append("None.\n")

    sys.stdout.write("\n".join(out) + "\n")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
