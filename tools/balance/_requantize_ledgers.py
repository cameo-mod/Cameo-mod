#!/usr/bin/env python3
"""One-shot re-quantization of damage warheads in the balance ledgers.

Reads docs/balance/*.json, rounds every main SpreadDamage/AreaDamage warhead
to the 2000-step grid, recomputes *Percentage and FriendlyFire/ExtraDamage
twins, and updates actor FirepowerMultiplier to keep effective per-shot
damage close to the original.
"""
from __future__ import annotations

import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
# Load the canonical balance law so we never re-implement main warhead logic.
sys.path.insert(0, str(ROOT / "tools" / "balance"))
import formula

LEDGER_DIR = ROOT / "docs" / "balance"

GRID = 2000
MIN_FP = 50
MAX_FP = 150


def to_int(v):
    try:
        return int(float(v))
    except (TypeError, ValueError):
        return 0


def actor_yaml_file(actor: dict, pack: str, section: str) -> str | None:
    for key in ("hp", "cost", "speed", "speed_air", "sight"):
        slot = actor.get(key)
        if isinstance(slot, dict):
            src = slot.get("src", "")
            if src and src != "inherited" and "#" in src:
                return src.split("#", 1)[0]
    return f"{pack}/yaml/{section}.yaml"


def requantize_actor(actor_id, actor, pack, section):
    """Return (blockers, new_firepower_slot, changed)."""
    blockers: list[str] = []
    armaments = actor.get("armaments", [])
    if not armaments:
        return blockers, None, False

    changed = False
    actor_old_total = 0.0
    actor_new_total = 0

    for arm in armaments:
        warheads = arm.get("damage_warheads", [])
        weapon_types = arm.get("warheads") or arm.get("versus_templates") or []
        if not warheads:
            continue

        # Only template-named main warheads count for pricing (ExtraDamage/FF/Pct are twins).
        old_total = formula.spread_damage_sum(warheads, template_names=weapon_types)
        if old_total <= 0:
            continue

        dist = formula.distribute_damage(old_total, warheads, template_names=weapon_types)
        if not dist:
            continue

        mains = formula.main_spread_warheads(warheads, template_names=weapon_types)
        main_tags = {(w.get("tag") or "") for w in mains}
        new_total = sum(dist[t] for t in main_tags if t in dist)

        for w in warheads:
            tag = w.get("tag")
            if tag in dist and to_int(w.get("damage")) != dist[tag]:
                w["damage"] = str(dist[tag])
                changed = True

        actor_old_total += old_total
        actor_new_total += new_total

    if not changed or actor_new_total == 0:
        return blockers, None, False

    # Firepower multiplier only changes if the main-shot totals changed.
    if abs(actor_old_total - actor_new_total) < 1:
        return blockers, None, changed

    fp_slot = actor.get("firepower_multiplier")
    existing_fp = 1.0
    if isinstance(fp_slot, dict):
        try:
            existing_fp = float(fp_slot.get("v", 1.0))
        except (TypeError, ValueError):
            existing_fp = 1.0
    elif isinstance(fp_slot, (int, float)):
        existing_fp = float(fp_slot)
    elif isinstance(fp_slot, str):
        try:
            existing_fp = float(fp_slot)
        except ValueError:
            existing_fp = 1.0

    new_fp = int(round(existing_fp * actor_old_total / actor_new_total * 100))
    if new_fp < MIN_FP or new_fp > MAX_FP:
        blockers.append(
            f"{actor_id}: FP {new_fp}% out of {MIN_FP}-{MAX_FP}% "
            f"(old_total={int(actor_old_total)}, new_total={actor_new_total})"
        )
        return blockers, None, False

    if isinstance(fp_slot, dict):
        fp_slot = dict(fp_slot)
        fp_slot["v"] = new_fp / 100.0
    else:
        src_file = actor_yaml_file(actor, pack, section)
        if src_file is None:
            blockers.append(f"{actor_id}: cannot locate actor YAML for FirepowerMultiplier")
            return blockers, None, False
        fp_slot = {
            "src": f"{src_file}#FirepowerMultiplier.Modifier",
            "trait": "FirepowerMultiplier",
            "v": new_fp / 100.0,
        }

    return blockers, fp_slot, changed


def main() -> int:
    blockers: list[str] = []
    for jf in sorted(LEDGER_DIR.glob("*.json")):
        if jf.name == "class_anchors.json":
            continue
        text = jf.read_text(encoding="utf-8")
        doc = json.loads(text)
        pack = doc.get("pack", "")
        sections = doc.get("sections", {})
        any_change = False
        for section, actors in sections.items():
            for actor_id, actor in actors.items():
                if not isinstance(actor, dict):
                    continue
                actor_blockers, fp_slot, changed = requantize_actor(actor_id, actor, pack, section)
                blockers.extend(actor_blockers)
                if changed and not actor_blockers:
                    if fp_slot is not None:
                        actor["firepower_multiplier"] = fp_slot
                    any_change = True
        if any_change:
            jf.write_text(json.dumps(doc, indent=" ", ensure_ascii=False), encoding="utf-8")

    if blockers:
        print(f"BLOCKERS ({len(blockers)}):")
        for b in blockers[:60]:
            print(b)
        if len(blockers) > 60:
            print(f"... and {len(blockers) - 60} more")
    else:
        print("No blockers.")
    return 1 if blockers else 0


if __name__ == "__main__":
    sys.exit(main())
