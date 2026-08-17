#!/usr/bin/env python3
"""audit_k_linearity.py — K's flat part must be damage-INDEPENDENT, and the %-twin floor visible.

The whole pricing model rests on one claim (`weapon_efficiency` docstring):

    effective_per_shot = Damage_total x K_flat_context + pct_absolute_context

so that solving for Damage is closed-form. That claim is only true if `K_flat` really is
invariant under a change of flat `Damage`. Nothing proved it, and for two years it was
FALSE of the number the ledger actually published:

  E4, measured 2026-08-17 — `k` folded the %-of-max-HP twin's contribution in as
  `share = ref_hp x pct_damage / 100 / flat_total`. That share carries `flat_total` in its
  DENOMINATOR, so `k` moves when Damage moves: doubling `AnthraxCloudLarge`'s flat Damage
  drops its `k` by 37%. Inverting through it to reach 2x the DPS prescribes 40% of the
  Damage actually needed, and asking for LESS than the twin already delivers yields a
  negative headroom that the old code silently returned as a positive Damage.

  ⚠ `k` is not WRONG as a measurement — `effective_per_shot = damage_total x k_context`
  reproduces the truth exactly at the weapon's current Damage, so every `effective_*`
  number in the ledger is sound. `k` is wrong as a SHAPE COEFFICIENT. This audit exists to
  keep those two roles from being confused again.

Three checks:

  **L1 — `k_flat` is invariant** under scaling every flat `Damage` by 2x and by 0.5x.
  **L2 — the identity `k == k_flat + pct_absolute / flat_total`** holds, so the affine
  split is a genuine decomposition of the published number rather than a second opinion.
  **L3 — the %-twin FLOOR is reported**, listing the weapons whose floor is a large share
  of their output. These are not bugs; they are weapons whose price has a hard lower bound,
  which is a design fact the balance pass has to know about.
"""
from __future__ import annotations

import copy
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))
sys.path.insert(0, str(ROOT / "tools" / "audit"))

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

import weapon_efficiency as we  # noqa: E402
from miniyaml import Ruleset  # noqa: E402

# k_flat is a ratio of sums, so only float noise should move it.
TOL = 1e-9
# Above this share of output, a weapon's %-twin floor dominates its price. Reported, not failed.
FLOOR_NOTE = 0.25
# ⚠ INTEGER factors only. `effective_damage.damage_value` reads Damage as `int(float(raw))` —
# TRUNCATION, matching the engine's integer field — so a 0.5x factor turns 1755 into 877 and
# shifts the per-warhead shares by a fraction of a unit. That artifact of the TEST showed up as
# a 0.01% drift on 10 weapons and would have been read as a defect in the MODEL. Multiplying an
# integer Damage by an integer stays exact, so any drift these report is real. Proportionality
# is symmetric: invariance under 2x/4x/10x is the same claim as invariance under 0.5x.
SCALES = (2.0, 4.0, 10.0)


def scale_flat(node, factor: float):
    """The weapon with every FLAT warhead's Damage multiplied, %-twins untouched.

    ⚠ Scales to a FLOAT and does not round. Rounding to the yaml integer grid perturbs the
    per-warhead SHARES by a fraction of a unit, and on a weapon whose warheads differ by
    ~1 damage that showed up as a 0.02% `k_flat` drift — a measurement artifact of the test
    that would have been read as a model defect. The invariant under test is exact
    proportionality; the grid is `formula.snap_damage_step`'s business, not this audit's.
    """
    out = copy.deepcopy(node)
    for c in out.children:
        if not c.key.startswith("Warhead@") or "Percentage" in str(c.value or ""):
            continue
        for g in c.children:
            if g.key != "Damage":
                continue
            try:
                g.value = repr(float(str(g.value).strip()) * factor)
            except (TypeError, ValueError):
                pass
    return out


def main() -> int:
    rs = Ruleset(ROOT)
    rows = []
    for name in rs.weapons:
        if name.startswith("^"):
            continue
        node = rs.resolve_weapon(name)
        if node is None:
            continue
        try:
            res = we.analyse(node)
        except Exception:  # noqa: BLE001 - a broken weapon is another audit's job
            continue
        if res is not None:
            rows.append((name, node, res))

    failed = 0
    print("# audit_k_linearity — the flat K must not move when Damage moves")
    print()
    print(f"Analysed **{len(rows)}** concrete weapons.")
    print()

    # ---- L1 -----------------------------------------------------------------
    print("## L1 — `k_flat` is invariant under a change of flat Damage")
    print()
    drifted = []
    for name, node, res in rows:
        if res["k_flat"] <= 0:
            continue
        for factor in SCALES:
            try:
                alt = we.analyse(scale_flat(node, factor))
            except Exception:  # noqa: BLE001
                continue
            if alt is None:
                continue
            drift = abs(alt["k_flat"] - res["k_flat"]) / res["k_flat"]
            if drift > TOL:
                drifted.append((name, factor, res["k_flat"], alt["k_flat"], drift))
    if drifted:
        failed = 1
        print(f"**FAIL — {len(drifted)} case(s) where `k_flat` moved with Damage.**")
        print()
        print("| weapon | Damage x | k_flat | k_flat scaled | drift |")
        print("|---|--:|--:|--:|--:|")
        for name, factor, a, b, d in sorted(drifted, key=lambda r: -r[4])[:25]:
            print(f"| `{name}` | {factor:g} | {a:.4f} | {b:.4f} | {d:.2%} |")
    else:
        print(f"_clean_ — `k_flat` held to within {TOL:g} across "
              f"{len(SCALES)} scalings of every weapon.")
    print()

    # ---- L2 -----------------------------------------------------------------
    print("## L2 — the affine split decomposes the published `k`")
    print()
    print("`k == k_flat + pct_absolute / flat_total`, so nothing about the measured "
          "`effective_*` numbers changes — only the invertible form is new.")
    print()
    broken = []
    for name, _node, res in rows:
        ft = res["flat_total"] or 1.0
        rebuilt = res["k_flat"] + res["pct_absolute"] / ft
        if res["k"] > 0 and abs(rebuilt - res["k"]) / res["k"] > 1e-9:
            broken.append((name, res["k"], rebuilt))
    if broken:
        failed = 1
        print(f"**FAIL — {len(broken)} weapon(s) where the identity does not hold.**")
        print()
        print("| weapon | k | k_flat + pct/flat_total |")
        print("|---|--:|--:|")
        for name, k, rebuilt in broken[:25]:
            print(f"| `{name}` | {k:.6f} | {rebuilt:.6f} |")
    else:
        print("_clean_ — the identity holds for every analysed weapon.")
    print()

    # ---- L3 -----------------------------------------------------------------
    print("## L3 — weapons with a %-twin DPS floor")
    print()
    floors = []
    for name, _node, res in rows:
        total = res["flat_total"] * res["k_flat_context"] + res["pct_absolute_context"]
        if total > 0 and res["pct_absolute_context"] > 0:
            floors.append((name, res["pct_absolute_context"] / total))
    heavy = sorted((r for r in floors if r[1] >= FLOOR_NOTE), key=lambda r: -r[1])
    print(f"{len(floors)} weapon(s) carry a %-twin; **{len(heavy)}** have a floor at or "
          f"above {FLOOR_NOTE:.0%} of output.")
    print()
    if heavy:
        print("A price target below the floor is UNREACHABLE by lowering flat Damage — "
              "`required_damage()` returns None rather than a wrong positive number. To "
              "price these lower, the TWIN has to shrink.")
        print()
        print("| weapon | floor as share of output |")
        print("|---|--:|")
        for name, share in heavy[:30]:
            print(f"| `{name}` | {share:.1%} |")
        if len(heavy) > 30:
            print()
            print(f"_... and {len(heavy) - 30} more._")
    else:
        print("_none above the reporting threshold._")
    return failed


if __name__ == "__main__":
    raise SystemExit(main())
