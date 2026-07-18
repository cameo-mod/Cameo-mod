#!/usr/bin/env python3
"""build_workbook.py — Balance Pipeline Phase 2 (BALANCE_PIPELINE.md §3).

Ledger (docs/balance/*.json) -> docs/design/cameo_balance_v2.xlsx.

The workbook is a WORKBENCH, never a committed source (gitignored):
regenerate at will. Raw stats appear as editable cells; every derived
quantity is a live Excel formula (identical math to formula.py — proven
by test_formula.py's Tiger identity + symbolic equivalence). Layout:

  unit row:    Mod | Name | Actor | HP | Speed | Armor | TechTier |
               UnitClass | Special | ... | O | P | Q | Price | Cost |
               Delta | Delta% | RangeSolver
  weapon rows: indented under the unit — Damage | Reload | Burst |
               BurstDelays | Range(wdist) | WeaponClass | EffReload* |
               DPS*        (* = formula cells)

Editable (unlocked) cells: unit HP/Speed/TechTier/UnitClass/Special,
weapon Damage/Reload/Burst/BurstDelays/Range/WeaponClass, and Cost.
Everything else is locked (sheet protection, no password).

Damage convention: the weapon row's Damage cell is the MAX of the
weapon's raw warhead damages (all of them are listed in a cell
comment); importing a changed Damage scales every warhead
proportionally (Phase 4).

Defense rows with no Speed get the legacy speed=100 convention as an
explicit input value (until the Formula-v2 defense class replaces it).
"""
from __future__ import annotations

import json
import pathlib
import sys

import openpyxl
from openpyxl.comments import Comment
from openpyxl.formatting.rule import ColorScaleRule
from openpyxl.styles import Alignment, Font, PatternFill, Protection
from openpyxl.utils import get_column_letter

ROOT = pathlib.Path(__file__).resolve().parents[2]
LEDGER = ROOT / "docs/balance"
OUTFILE = ROOT / "docs/design/cameo_balance_v2.xlsx"

SECTION_ORDER = ("infantry", "vehicles", "aircraft", "naval", "defenses",
                 "buildings", "upgrades", "promotions", "misc", "faction")

HDR = ["Mod", "Name", "Actor", "HP", "Speed", "Armor", "TechTier",
       "UnitClass", "Special", "Damage", "Reload", "Burst", "BurstDel",
       "Range(wd)", "WeapClass", "EffReload", "DPS",
       "O", "P", "Q", "Price", "Cost", "Delta", "Delta%", "RangeSolve"]
COL = {name: i + 1 for i, name in enumerate(HDR)}
UNLOCKED_UNIT = ("HP", "Speed", "TechTier", "UnitClass", "Special", "Cost")
UNLOCKED_WEAPON = ("Damage", "Reload", "Burst", "BurstDel", "Range(wd)", "WeapClass")

HEAD_FONT = Font(bold=True)
SECTION_FILL = PatternFill("solid", fgColor="DDDDDD")
WEAPON_FONT = Font(italic=True, size=9)


def L(name: str) -> str:
    return get_column_letter(COL[name])


def fnum(v):
    try:
        return float(v)
    except (TypeError, ValueError):
        return None


def unit_rows(ws, theme, aid, u, section, row):
    """Write one unit (+ its weapon rows); returns next free row."""
    first = row
    ws.cell(row=row, column=COL["Mod"], value=theme)
    ws.cell(row=row, column=COL["Name"], value=u.get("name") or aid)
    ws.cell(row=row, column=COL["Actor"], value=aid)
    hp = fnum((u.get("hp") or {}).get("v"))
    speed = fnum((u.get("speed") or {}).get("v") or (u.get("speed_air") or {}).get("v"))
    if speed is None and section == "defenses":
        speed = 100.0  # legacy convention until the Formula-v2 defense class
    armor = (u.get("armor") or {}).get("v")
    cost = fnum((u.get("cost") or {}).get("v"))
    ws.cell(row=row, column=COL["HP"], value=hp)
    ws.cell(row=row, column=COL["Speed"], value=speed)
    ws.cell(row=row, column=COL["Armor"], value=armor)
    d = u.get("design") or {}
    ws.cell(row=row, column=COL["TechTier"], value=d.get("tech_tier") or 1)
    ws.cell(row=row, column=COL["UnitClass"], value=d.get("unit_class") or 1)
    ws.cell(row=row, column=COL["Special"], value=d.get("special") or 1)
    ws.cell(row=row, column=COL["Cost"], value=cost)

    wrows = []
    for arm in u.get("armaments", []):
        if arm.get("unresolved"):
            continue
        row += 1
        wrows.append(row)
        ws.cell(row=row, column=COL["Name"],
                value=f"  ↳ {arm.get('weapon')}").font = WEAPON_FONT
        ws.cell(row=row, column=COL["Actor"], value=arm.get("slot")).font = WEAPON_FONT
        damages = [fnum(w.get("damage")) for w in arm.get("warheads", [])]
        damages = [x for x in damages if x is not None]
        cdmg = ws.cell(row=row, column=COL["Damage"],
                       value=max(damages) if damages else None)
        if len(damages) > 1:
            cdmg.comment = Comment(
                "raw warhead damages: " + ", ".join(str(int(x)) for x in damages)
                + "\nimport scales all proportionally", "balance-pipeline")
        ws.cell(row=row, column=COL["Reload"], value=fnum(arm.get("reloaddelay")))
        ws.cell(row=row, column=COL["Burst"], value=fnum(arm.get("burst")) or 1)
        ws.cell(row=row, column=COL["BurstDel"], value=fnum(arm.get("burstdelays")))
        ws.cell(row=row, column=COL["Range(wd)"], value=fnum(arm.get("range")))
        ws.cell(row=row, column=COL["WeapClass"],
                value=fnum(arm.get("design_weapon_class")) or 1)
        r = row
        ws.cell(row=r, column=COL["EffReload"],
                value=f"={L('Reload')}{r}+IF({L('Burst')}{r}>1,"
                      f"N({L('BurstDel')}{r})*({L('Burst')}{r}-1),0)")
        ws.cell(row=r, column=COL["DPS"],
                value=f"=IFERROR({L('Damage')}{r}*MAX({L('Burst')}{r},1)"
                      f"/{L('EffReload')}{r}*{L('WeapClass')}{r},0)")

    if wrows and hp is not None and speed is not None:
        r = first
        dps_sum = "+".join(f"{L('DPS')}{w}" for w in wrows)
        rng = "MAX(" + ",".join(f"{L('Range(wd)')}{w}" for w in wrows) + ")"
        ws.cell(row=r, column=COL["DPS"], value=f"={dps_sum}")
        ws.cell(row=r, column=COL["Range(wd)"], value=f"={rng}")
        H_, S_, T_, U_, K_ = (f"{L('HP')}{r}", f"{L('Speed')}{r}", f"{L('TechTier')}{r}",
                              f"{L('UnitClass')}{r}", f"{L('Special')}{r}")
        RNG = f"({L('Range(wd)')}{r}/1000)"
        DPS_ = f"{L('DPS')}{r}"
        ws.cell(row=r, column=COL["O"],
                value=f"=({H_}/100000+{S_}/100+{RNG}*{K_}/5+{DPS_}/200)*200*{U_}*{T_}")
        ws.cell(row=r, column=COL["P"],
                value=f"=(({H_}*{S_}/25000)+({RNG}*{K_}*{DPS_}/2.5))*{U_}*{T_}")
        ws.cell(row=r, column=COL["Q"],
                value=f"=({H_}*{S_}*{RNG}*{K_}*{DPS_}*{U_}*{T_})/12500000")
        anchors = getattr(build, "_anchors", None) or {}
        cls = (u.get("design") or {}).get("class_anchor")
        a = anchors.get(cls) if cls else None
        if a and a.get("signed_off") and a.get("cost0"):
            # Formula v2: normalized deviation from the class anchor
            ws.cell(row=r, column=COL["Price"],
                    value=f"={a['cost0']}*({L('O')}{r}/{a['o0']}"
                          f"+{L('P')}{r}/{a['p0']}+{L('Q')}{r}/{a['q0']})/3")
        else:
            ws.cell(row=r, column=COL["Price"],
                    value=f"=({L('O')}{r}+{L('P')}{r}+{L('Q')}{r})/3")
        ws.cell(row=r, column=COL["Delta"],
                value=f"=IFERROR({L('Price')}{r}-{L('Cost')}{r},\"\")")
        ws.cell(row=r, column=COL["Delta%"],
                value=f"=IFERROR(ABS({L('Delta')}{r})/MAX({L('Cost')}{r},1),\"\")")
        # closed-form range solver: price(range)=cost  (linear in range)
        A = (f"((({H_}/100000+{S_}/100+{DPS_}/200)*200*{U_}*{T_})"
             f"+(({H_}*{S_}/25000)*{U_}*{T_}))/3")
        B = (f"((({K_}/5)*200*{U_}*{T_})+(({K_}*{DPS_}/2.5)*{U_}*{T_})"
             f"+(({H_}*{S_}*{K_}*{DPS_}*{U_}*{T_})/12500000))/3")
        ws.cell(row=r, column=COL["RangeSolve"],
                value=f"=IFERROR(({L('Cost')}{r}-{A})/{B}*1000,\"\")")
    return row + 1


def protect(ws, unit_cells, weapon_cells):
    for c in unit_cells:
        c.protection = Protection(locked=False)
    for c in weapon_cells:
        c.protection = Protection(locked=False)
    ws.protection.sheet = True
    ws.protection.enable()


def build():
    wb = openpyxl.Workbook()
    const = wb.active
    const.title = "Constants"
    notes = [
        ("cameo_balance_v2 — generated workbench (build_workbook.py).", None),
        ("NEVER commit this file; regenerate from the ledger.", None),
        ("Formula law (formula.py, Tiger anchor O=P=Q=Cost=800):", None),
        ("DPS", "Damage*Burst/(Reload+BurstDel*(Burst-1))*WeapClass"),
        ("O", "(HP/1e5+Speed/100+Rng*Spec/5+DPS/200)*200*UC*Tier"),
        ("P", "((HP*Speed/25000)+(Rng*Spec*DPS/2.5))*UC*Tier"),
        ("Q", "(HP*Speed*Rng*Spec*DPS*UC*Tier)/12.5e6"),
        ("Price", "(O+P+Q)/3   |   RangeSolve: price(range)=Cost, exact"),
        ("class_tuning", "knob table lands here in Phase 5 (BALANCE_PIPELINE §5b)"),
    ]
    for i, (a, b) in enumerate(notes, start=1):
        const.cell(row=i, column=1, value=a).font = HEAD_FONT if i <= 3 else Font()
        if b:
            const.cell(row=i, column=2, value=b)

    anchors_file = LEDGER / "class_anchors.json"
    build._anchors = {k: v for k, v in (json.loads(
        anchors_file.read_text(encoding="utf-8")) if anchors_file.exists()
        else {}).items() if isinstance(v, dict)}

    for jf in sorted(LEDGER.glob("*.json")):
        doc = json.loads(jf.read_text(encoding="utf-8"))
        if "sections" not in doc:
            continue  # registry files (class_anchors.json etc.), not ledgers
        name = doc["ledger"][:31]
        ws = wb.create_sheet(title=name)
        for c, h in enumerate(HDR, start=1):
            cell = ws.cell(row=1, column=c, value=h)
            cell.font = HEAD_FONT
        ws.freeze_panes = "D2"
        theme = doc["ledger"].split("_")[0]
        row = 2
        unit_unlock, weap_unlock = [], []
        for section in SECTION_ORDER:
            sec = doc["sections"].get(section)
            if not sec:
                continue
            ws.cell(row=row, column=1, value=section.upper()).font = HEAD_FONT
            for c in range(1, len(HDR) + 1):
                ws.cell(row=row, column=c).fill = SECTION_FILL
            row += 1
            for aid in sorted(sec):
                first = row
                row = unit_rows(ws, theme, aid, sec[aid], section, row)
                for col in UNLOCKED_UNIT:
                    unit_unlock.append(ws.cell(row=first, column=COL[col]))
                for wr in range(first + 1, row - 0):
                    if ws.cell(row=wr, column=COL["Actor"]).value and \
                       str(ws.cell(row=wr, column=COL["Actor"]).value).startswith("Armament"):
                        for col in UNLOCKED_WEAPON:
                            weap_unlock.append(ws.cell(row=wr, column=COL[col]))
        ws.conditional_formatting.add(
            f"{L('Delta%')}2:{L('Delta%')}{row}",
            ColorScaleRule(start_type="num", start_value=0, start_color="63BE7B",
                           mid_type="num", mid_value=0.25, mid_color="FFEB84",
                           end_type="num", end_value=0.6, end_color="F8696B"))
        for col, width in (("Name", 28), ("Actor", 30), ("Mod", 10)):
            ws.column_dimensions[L(col)].width = width
        protect(ws, unit_unlock, weap_unlock)

    OUTFILE.parent.mkdir(parents=True, exist_ok=True)
    wb.save(OUTFILE)
    print(f"wrote {OUTFILE.relative_to(ROOT)} ({len(wb.sheetnames)-1} faction tabs)")


if __name__ == "__main__":
    sys.exit(build())
