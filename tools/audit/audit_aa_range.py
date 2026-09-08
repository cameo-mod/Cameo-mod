#!/usr/bin/env python3
"""audit_aa_range.py — C45, LANE 3 (FLEET_ORDERS_2026-09-08 SS10).

Report, per buildable actor with both a live ground and a live AA armament:
its class, its AA÷ground range ratio, and the verdict under the DESIGN.md
AA range law (2026-09-08).

THE LAW (docs/DESIGN.md §"The AA range law"):
  - THREE classes only may carry AA range > ground range: scout_vehicle,
    armed_troop_transport, anti_air_vehicle — at 1.5×.
  - Every other class: same range both domains (ratio 1.0×).
  - mobile_bunker: NO air-capable armament at all.

FAIL when:
  - an allowed class is NOT at 1.5×
  - a disallowed class is above 1.0×
  - any mobile_bunker carries an air-capable armament

⛔ Never hand-parse yaml — read through miniyaml.Ruleset.resolve_weapon.
⛔ Get the class from class_membership.classify() — never the raw
  design.class_anchor field.
⚠ A 0% or 100% row is a bug in the CHECK.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/audit"))
sys.path.insert(0, str(ROOT / "tools/balance"))

from cameo_model import Model
from miniyaml import Ruleset
import class_membership

# --- The law --------------------------------------------------------------- #
ALLOWED_AA_BONUS_CLASSES = {"scout_vehicle", "armed_troop_transport", "anti_air_vehicle"}
AA_RANGE_MULT = 1.5
RATIO_EPSILON = 0.01  # tolerance for float comparison

LEDGER = ROOT / "docs/balance"
RATCHET_FILE = ROOT / "tools/audit/ratchet_aa_range.json"


def _weapon_range(rs: Ruleset, wname: str) -> float | None:
    """Resolved Range for a weapon, or None."""
    w = rs.resolve_weapon(wname)
    if w is None:
        return None
    v = w.get("Range")
    if v is None:
        return None
    try:
        return float(str(v).split(",")[0].strip())
    except (ValueError, TypeError):
        return None


def _weapon_is_aa(rs: Ruleset, wname: str) -> bool:
    """True if the resolved weapon's ValidTargets include Air.

    ⛔ Never hand-parse — read through miniyaml.Ruleset.resolve_weapon.
    Checks both the weapon-level ValidTargets and warhead-level ValidTargets.
    """
    w = rs.resolve_weapon(wname)
    if w is None:
        return False
    # Weapon-level ValidTargets
    vt = w.get("ValidTargets")
    if vt and "Air" in [t.strip() for t in vt.split(",")]:
        return True
    # Warhead-level ValidTargets (resolved)
    for c in w.children:
        if c.key.startswith("Warhead@"):
            wvt = c.get("ValidTargets")
            if wvt and "Air" in [t.strip() for t in wvt.split(",")]:
                return True
    return False


def _actor_armaments(rs: Ruleset, actor_name: str) -> list[dict]:
    """Resolved armaments for an actor: [{slot, weapon, is_aa, range}, ...]."""
    resolved = rs.resolve(actor_name)
    if resolved is None:
        return []
    arms = []
    for c in resolved.children:
        if c.key == "Armament" or c.key.startswith("Armament@"):
            wname = c.get("Weapon")
            if not wname:
                continue
            # Skip condition-gated armaments that are not active
            req = c.get("RequiresCondition")
            if req and ("disabled" in req or "wip" in req):
                continue
            is_aa = _weapon_is_aa(rs, wname)
            rng = _weapon_range(rs, wname)
            arms.append({
                "slot": c.key,
                "weapon": wname,
                "is_aa": is_aa,
                "range": rng,
            })
    return arms


def _is_balance_buildable(rs: Ruleset, actor_name: str) -> bool:
    """Check if actor is balance-buildable (has Buildable + Queue, not disabled)."""
    resolved = rs.resolve(actor_name)
    if resolved is None:
        return False
    buildable = None
    for c in resolved.children:
        if c.key == "Buildable":
            buildable = c
            break
    if buildable is None:
        return False
    queue = buildable.get("Queue")
    if not queue:
        return False
    prereq = buildable.get("Prerequisites") or ""
    disabling = {"~disabled", "~wip", "~disable", "~unavailable", "~notbuildable"}
    for token in prereq.split(","):
        if token.strip().lower() in disabling:
            return False
    return True


def audit_aa_range(model: Model) -> tuple[int, list[dict]]:
    """Run the audit. Returns (fail_count, rows)."""
    rs = model.rs

    # Build a map of actor -> design from the ledgers
    design_map: dict[str, dict] = {}
    for actor, design in class_membership.ledger_rows():
        design_map[actor] = design

    rows: list[dict] = []
    fail_count = 0

    # Get all actors from the ruleset
    for actor_name in sorted(rs.actors.keys()):
        # Exclude editor/critter/internal actors
        if actor_name.startswith("^") or actor_name.startswith("World"):
            continue
        if not _is_balance_buildable(rs, actor_name):
            continue

        arms = _actor_armaments(rs, actor_name)
        if not arms:
            continue

        # Split into AA and ground armaments
        aa_arms = [a for a in arms if a["is_aa"] and a["range"] is not None]
        ground_arms = [a for a in arms if not a["is_aa"] and a["range"] is not None]

        # Only report actors with BOTH a live ground and a live AA armament
        if not aa_arms or not ground_arms:
            # But check mobile_bunker with ANY air-capable armament
            design = design_map.get(actor_name, {})
            cls, _ = class_membership.classify(design)
            if cls == "mobile_bunker" and aa_arms:
                rows.append({
                    "actor": actor_name,
                    "class": cls,
                    "ground_range": max(a["range"] for a in ground_arms) if ground_arms else None,
                    "aa_range": max(a["range"] for a in aa_arms),
                    "ratio": None,
                    "verdict": "FAIL",
                    "reason": "mobile_bunker has air-capable armament (forbidden)",
                    "aa_weapons": [a["weapon"] for a in aa_arms],
                })
                fail_count += 1
            continue

        # Get the class
        design = design_map.get(actor_name, {})
        cls, _ = class_membership.classify(design)

        # Max ranges
        ground_range = max(a["range"] for a in ground_arms)
        aa_range = max(a["range"] for a in aa_arms)

        # Compute ratio
        if ground_range > 0:
            ratio = aa_range / ground_range
        else:
            ratio = None

        # Verdict
        verdict = "PASS"
        reason = ""

        if cls == "mobile_bunker":
            # mobile_bunker: NO air-capable armament at all
            verdict = "FAIL"
            reason = f"mobile_bunker has air-capable armament (forbidden)"
            fail_count += 1
        elif cls in ALLOWED_AA_BONUS_CLASSES:
            # Allowed 1.5× — check it's at 1.5×
            if ratio is not None and abs(ratio - AA_RANGE_MULT) > RATIO_EPSILON:
                verdict = "FAIL"
                reason = f"{cls} should be at {AA_RANGE_MULT}×, got {ratio:.3f}×"
                fail_count += 1
        else:
            # Disallowed bonus — check it's at 1.0×
            if ratio is not None and ratio > 1.0 + RATIO_EPSILON:
                verdict = "FAIL"
                reason = f"{cls or 'unclassified'} should be at 1.0×, got {ratio:.3f}×"
                fail_count += 1

        rows.append({
            "actor": actor_name,
            "class": cls,
            "ground_range": ground_range,
            "aa_range": aa_range,
            "ratio": round(ratio, 3) if ratio is not None else None,
            "verdict": verdict,
            "reason": reason,
            "aa_weapons": [a["weapon"] for a in aa_arms],
        })

    return fail_count, rows


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--verbose", action="store_true", help="print all rows, not just failures")
    args = ap.parse_args()

    model = Model()
    fail_count, rows = audit_aa_range(model)

    # Print results
    if rows:
        print(f"{'Actor':<40} {'Class':<25} {'Ground':>8} {'AA':>8} {'Ratio':>8} {'Verdict':<8} Reason")
        print("-" * 120)
        for r in rows:
            if args.verbose or r["verdict"] == "FAIL":
                gr = f"{r['ground_range']:.0f}" if r["ground_range"] else "-"
                ar = f"{r['aa_range']:.0f}" if r["aa_range"] else "-"
                ratio = f"{r['ratio']:.3f}" if r["ratio"] else "-"
                cls = r["class"] or "unclassified"
                print(f"{r['actor']:<40} {cls:<25} {gr:>8} {ar:>8} {ratio:>8} {r['verdict']:<8} {r['reason']}")

    # Ratchet check (lower-only)
    current = fail_count
    ratchet_val = None
    if RATCHET_FILE.exists():
        try:
            ratchet_val = json.loads(RATCHET_FILE.read_text(encoding="utf-8-sig")).get("fail_count")
        except (json.JSONDecodeError, OSError):
            pass

    print(f"\nAA range audit: {current} failures out of {len(rows)} actors with both ground + AA")
    if ratchet_val is not None:
        if current > ratchet_val:
            print(f"FAIL: {current} > ratchet {ratchet_val}")
            return 1
        else:
            print(f"PASS: {current} <= ratchet {ratchet_val}")
    else:
        # First run — register the ratchet
        RATCHET_FILE.write_text(json.dumps({"fail_count": current}), encoding="utf-8")
        print(f"RATCHET REGISTERED: {current}")

    return 1 if current > 0 and ratchet_val is not None and current > ratchet_val else 0


if __name__ == "__main__":
    sys.exit(main())
