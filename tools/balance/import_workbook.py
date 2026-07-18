#!/usr/bin/env python3
"""import_workbook.py — Balance Pipeline Phase 4a (BALANCE_PIPELINE.md §4).

cameo_balance_v2.xlsx -> ledger (docs/balance/*.json), input cells only.

Reads the designated INPUT cells (everything else is locked in the
generated sheet): unit HP / Speed / TechTier / UnitClass / Special /
Cost and weapon Damage / Reload / Burst / BurstDelays / Range /
WeaponClass. Prints every change; nothing else in the ledger moves.

Damage convention (BALANCE_PIPELINE §3): the sheet's Damage cell is
max(warhead damages); if it changed, ALL of that weapon's warhead
damages scale by the same ratio (rounded to int).
"""
from __future__ import annotations

import json
import pathlib
import sys

import openpyxl

ROOT = pathlib.Path(__file__).resolve().parents[2]
LEDGER = ROOT / "docs/balance"
WB = ROOT / "docs/design/cameo_balance_v2.xlsx"

UNIT_COLS = {"hp": 4, "speed": 5, "tech_tier": 7, "unit_class": 8,
             "special": 9, "cost": 22}
WEAP_COLS = {"damage": 10, "reloaddelay": 11, "burst": 12,
             "burstdelays": 13, "range": 14, "weapon_class": 15}


def fnum(v):
    try:
        f = float(v)
        return int(f) if f == int(f) else f
    except (TypeError, ValueError):
        return None


def main() -> int:
    if not WB.exists():
        print("no workbench found — run build_workbook.py first")
        return 2
    wb = openpyxl.load_workbook(WB, data_only=False)
    changes = 0
    for jf in sorted(LEDGER.glob("*.json")):
        doc = json.loads(jf.read_text(encoding="utf-8"))
        name = doc["ledger"][:31]
        if name not in wb.sheetnames:
            continue
        ws = wb[name]
        units = {}
        for sec in doc["sections"].values():
            units.update(sec)
        touched = False
        r = 2
        while r <= ws.max_row:
            actor = ws.cell(row=r, column=3).value
            if not actor or str(actor).startswith("Armament"):
                r += 1
                continue
            u = units.get(str(actor))
            if u is None:
                r += 1
                continue
            # ---- unit inputs ----
            for field, col in UNIT_COLS.items():
                v = fnum(ws.cell(row=r, column=col).value)
                if v is None:
                    continue
                if field in ("tech_tier", "unit_class", "special"):
                    d = u.setdefault("design", {})
                    if fnum(d.get(field)) != v and not (d.get(field) is None and v == 1):
                        print(f"  {actor}.design.{field}: {d.get(field)} -> {v}")
                        d[field] = v
                        touched = True
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
                    changes += 1
            # ---- weapon rows below ----
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
                                changes += 1
                            continue
                        old = fnum(arm.get(field))
                        if old is not None and old != v:
                            print(f"  {actor}/{slot_key}.{field}: {old} -> {v}")
                            arm[field] = str(v)
                            touched = True
                            changes += 1
                wr += 1
            r = wr
        if touched:
            jf.write_text(json.dumps(doc, sort_keys=True, indent=1, ensure_ascii=False) + "\n",
                          encoding="utf-8", newline="\n")
            print(f"updated {jf.name}")
    print(f"import complete: {changes} stat changes")
    return 0


if __name__ == "__main__":
    sys.exit(main())
