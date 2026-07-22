#!/usr/bin/env python3
"""import_workbook.py — Balance Pipeline Phase 4a (BALANCE_PIPELINE.md §4).

A generated balance workbook -> ledger (docs/balance/*.json), input cells only.

Reads the designated INPUT cells (everything else is locked in the
generated sheet): unit HP / Speed / TechTier / UnitClass / Special /
Cost and weapon Damage / Reload / Burst / BurstDelays / Range /
WeaponClass. Prints every change; nothing else in the ledger moves.

Damage convention (BALANCE_PIPELINE §3): the sheet's Damage cell is
max(warhead damages); if it changed, ALL of that weapon's warhead
damages scale by the same ratio (rounded to int).
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys

import openpyxl

ROOT = pathlib.Path(__file__).resolve().parents[2]
LEDGER = ROOT / "docs/balance"
FACTION_WB = ROOT / "docs/design/cameo_balance_by_faction.xlsx"
TYPE_WB = ROOT / "docs/design/cameo_balance_by_type.xlsx"
TYPE_SHEETS = ("Infantry", "Tanks", "Vehicles", "Aircraft", "Defenses", "Naval")

UNIT_COLS = {"hp": 7, "speed": 8, "tech_tier": 10, "unit_class": 11,
             "special": 12, "cost": 25}
DESIGN_TEXT_COLS = {"category": 5, "subtype": 6}
WEAP_COLS = {"damage": 13, "reloaddelay": 14, "burst": 15,
             "burstdelays": 16, "range": 17, "weapon_class": 18}


def fnum(v):
    try:
        f = float(v)
        return int(f) if f == int(f) else f
    except (TypeError, ValueError):
        return None


def import_sheet(ws, units) -> tuple[bool, int, set[str]]:
    touched = False
    changes = 0
    changed_actors = set()
    r = 2
    while r <= ws.max_row:
        actor = ws.cell(row=r, column=2).value
        if not actor or str(actor).startswith("↳"):
            r += 1
            continue
        actor = str(actor)
        u = units.get(actor)
        if u is None:
            r += 1
            continue
        d = u.setdefault("design", {})
        for field, col in DESIGN_TEXT_COLS.items():
            v = ws.cell(row=r, column=col).value
            v = str(v).strip() if v is not None else ""
            if v and d.get(field) != v:
                print(f"  {actor}.design.{field}: {d.get(field)} -> {v}")
                d[field] = v
                touched = True
                changed_actors.add(actor)
        for field, col in UNIT_COLS.items():
            v = fnum(ws.cell(row=r, column=col).value)
            if v is None:
                continue
            if field in ("tech_tier", "unit_class", "special"):
                if fnum(d.get(field)) != v and not (d.get(field) is None and v == 1):
                    print(f"  {actor}.design.{field}: {d.get(field)} -> {v}")
                    d[field] = v
                    touched = True
                    changed_actors.add(actor)
                continue
            slot = u.get(field) or u.get("speed_air") if field == "speed" else u.get(field)
            if field == "speed" and u.get("speed") is None and u.get("speed_air") is not None:
                slot = u["speed_air"]
            if slot is None:
                continue
            old = fnum(slot.get("v"))
            if old is not None and old != v:
                print(f"  {actor}.{field}: {old} -> {v}")
                slot["v"] = v
                touched = True
                changed_actors.add(actor)
                changes += 1
        wr = r + 1
        arms = {a["slot"]: a for a in u.get("armaments", [])}
        while wr <= ws.max_row and str(ws.cell(row=wr, column=3).value or "").startswith("Armament"):
            slot_key = str(ws.cell(row=wr, column=3).value)
            arm = arms.get(slot_key)
            if arm is not None:
                for field, col in WEAP_COLS.items():
                    v = fnum(ws.cell(row=wr, column=col).value)
                    if v is None:
                        continue
                    if field == "weapon_class":
                        if fnum(arm.get("design_weapon_class")) != v and not (
                                arm.get("design_weapon_class") is None and v == 1):
                            print(f"  {actor}/{slot_key}.weapon_class: "
                                  f"{arm.get('design_weapon_class')} -> {v}")
                            arm["design_weapon_class"] = v
                            touched = True
                            changed_actors.add(actor)
                        continue
                    if field == "damage":
                        damages = [fnum(w.get("damage")) for w in arm.get("warheads", [])]
                        damages = [x for x in damages if x is not None]
                        if not damages:
                            continue
                        old = max(damages)
                        if old and v != old:
                            ratio = v / old
                            print(f"  {actor}/{slot_key}.damage: {old} -> {v} "
                                  f"(scaling {len(damages)} warheads x{ratio:.4f})")
                            for w in arm["warheads"]:
                                d0 = fnum(w.get("damage"))
                                if d0 is not None:
                                    w["damage"] = str(int(round(d0 * ratio)))
                            touched = True
                            changed_actors.add(actor)
                            changes += 1
                        continue
                    old = fnum(arm.get(field))
                    if old is not None and old != v:
                        print(f"  {actor}/{slot_key}.{field}: {old} -> {v}")
                        arm[field] = str(v)
                        touched = True
                        changed_actors.add(actor)
                        changes += 1
            wr += 1
        r = wr
    return touched, changes, changed_actors


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--workbook", choices=("faction", "type"), default="faction")
    args = ap.parse_args()
    workbook = TYPE_WB if args.workbook == "type" else FACTION_WB
    if not workbook.exists():
        print("no workbench found — run build_workbook.py first")
        return 2
    wb = openpyxl.load_workbook(workbook, data_only=False)
    docs = []
    units = {}
    owners = {}
    for jf in sorted(LEDGER.glob("*.json")):
        doc = json.loads(jf.read_text(encoding="utf-8"))
        if "sections" not in doc:
            continue
        docs.append((jf, doc))
        for sec in doc["sections"].values():
            units.update(sec)
            owners.update({actor: jf for actor in sec})
    total_changes = 0
    changed_docs = set()
    if args.workbook == "type":
        for sheet in TYPE_SHEETS:
            if sheet not in wb.sheetnames:
                continue
            touched, changes, changed_actors = import_sheet(wb[sheet], units)
            if touched:
                total_changes += changes
                changed_docs.update(owners[actor] for actor in changed_actors if actor in owners)
    else:
        for jf, doc in docs:
            name = doc["ledger"][:31]
            if name not in wb.sheetnames:
                continue
            faction_units = {}
            for sec in doc["sections"].values():
                faction_units.update(sec)
            touched, changes, _ = import_sheet(wb[name], faction_units)
            if touched:
                total_changes += changes
                changed_docs.add(jf)
    for jf, doc in docs:
        if jf in changed_docs:
            jf.write_text(json.dumps(doc, sort_keys=True, indent=1, ensure_ascii=False) + "\n",
                          encoding="utf-8", newline="\n")
            print(f"updated {jf.name}")
    print(f"import complete: {total_changes} stat changes")
    return 0


if __name__ == "__main__":
    sys.exit(main())
