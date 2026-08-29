#!/usr/bin/env python3
"""
Harvester Balance Table Generator
Corrected formulas from engine source code analysis.

⚠ SUPERSEDED for measurement by tools/balance/harvester_income.py, which derives
every parameter from the resolved tree instead of the hardcoded HARVESTERS table
below. That table is accurate for 30 of 33 rows but carries Capacity 100 for
schwarzermond_noidharvester against a yaml value of 50, is missing
EDEN_CARGOTRUCK_EMPTY and PLYMOUTH_CARGOTRUCK_EMPTY, and models neither the
HarvesterBalancer speed boost nor the refinery dock/fleet terms. Kept for its
engine-source notes and its HTML/PDF rendering. See docs/design/HARVESTER_BALANCE.md.

Engine mechanics:
- FullyLoadedSpeed is a PERCENTAGE of base Speed (not absolute).
  Harvester.cs:284-287: GetSpeedModifier() = 100 - (100 - FullyLoadedSpeed) * Fullness / 100
  When full: effective_speed = Speed * FullyLoadedSpeed / 100
  When empty: effective_speed = Speed * 100 / 100 = Speed

- MovementSpeedForCell = Speed * (terrainSpeed / 100) * (speedModifier / 100)
  Mobile.cs:756-761: Util.ApplyPercentageModifiers(Info.Speed, [terrainSpeed, speedModifier, ...])

- Travel time per cell = 1024 / MovementSpeedForCell (with carryover progress)
  Move.cs:471: progress += mobile.MovementSpeedForCell(mobile.ToCell)
  Move.cs:473: if progress >= Distance (half-cell = 512, full cell = 1024)

- Load time: each bale = 1 tick (harvest) + BaleLoadDelay ticks (wait)
  HarvestResource.cs:95-103: RemoveResource + AddResource, then QueueChild(new Wait(BaleLoadDelay))
  T_load = Cap * (BaleLoadDelay + 1)

- Unload time: first batch immediate, then BaleUnloadDelay per batch, +1 final check
  Harvester.cs:182-202: --currentUnloadTicks, then unload, then currentUnloadTicks = BaleUnloadDelay
  T_unload = ceil(Cap / BUA) * BaleUnloadDelay + 1

- ChronoResourceDelivery: return trip = 0 (teleport)
"""

import math
import html
import pathlib

# ⚠ These were absolute Windows paths. On Linux a backslash is an ordinary
# filename character, so the "path" became a single file called
# `c:\Users\...\harvester_balance_table.html` dropped in the repo root, which
# then showed up as an untracked file on every clone. Resolve from this file
# instead so the output lands in docs/audit/ on every platform.
_ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT_HTML = _ROOT / "docs" / "audit" / "harvester_balance_table.html"
OUT_PDF = _ROOT / "docs" / "audit" / "harvester_balance_table.pdf"

# Terrain speeds from world.yaml locomotors (Clear terrain)
TERRAIN_SPEEDS = {
    "foot": 90,
    "swimsuit": 90,
    "chem": 90,
    "wheeled": 80,
    "heavywheeled": 80,
    "lighttracked": 80,
    "tracked": 80,
    "heavytracked": 80,
    "hover": 90,
}

# Cell size in WDist units
CELL_DIST = 1024

# Assumed travel distance (refinery to ore field)
D = 20

# Resource Value (normalized)
RV = 25

# Harvester data: (actor, faction, cost, cap, bld, bud, bua, speed, fully_loaded_speed, locomotor, chrono)
# fully_loaded_speed: if None, inherits from ^Harvester (100 in defaults.yaml, NOT engine default 85)
# chrono: True if ChronoResourceDelivery (return trip = 0)
HARVESTERS = [
    # Tiberian Dawn — ^TDHARV: Cap=45, BLD=4, BUD=1, BUA=1, FLS=100(inherited), Speed=60, tracked
    ("td_gdi_tiberiumharvester", "TD GDI", 1000, 45, 4, 1, 1, 60, 100, "tracked", False),
    ("td_nod_tiberiumharvester", "TD Nod", 1000, 45, 4, 1, 1, 60, 100, "tracked", False),
    ("td_nod_stealthharvester", "TD Nod", 1000, 30, 2, 1, 1, 75, 100, "tracked", False),
    # Tiberian Sun — inherit ^TDHARV but override Harvester.Capacity/PipCount (NOT real fields, ignored by engine)
    # Actual capacity = StoresResources.Capacity = 45 from ^TDHARV. Speed=60 from ^TDHARV. tracked.
    ("ts_gdi_tiberiumharvester", "TS GDI", 1000, 45, 4, 1, 1, 60, 100, "tracked", False),
    ("ts_nod_tiberiumharvester", "TS Nod", 1000, 45, 4, 1, 1, 60, 100, "tracked", False),
    ("cabal_tiberiumharvester", "CABAL", 1000, 45, 4, 1, 1, 60, 100, "tracked", False),
    ("forgotten_tiberiumharvester", "Forgotten", 1000, 45, 4, 1, 1, 60, 100, "tracked", False),
    # Red Alert 1 — ^RAHARV: Cap=30, BLD=3, BUD=1, BUA=1, FLS=100, Speed=90, tracked
    ("ra1_allies_alliedoretruck", "RA1 Allies", 1000, 30, 3, 1, 1, 90, 100, "tracked", False),
    ("ra1_soviets_oretruck", "RA1 Soviets", 1000, 30, 3, 1, 1, 90, 100, "tracked", False),
    ("ra1_soviets_heavyindustrialminer", "RA1 Soviets", 1200, 40, 2, 1, 1, 80, 100, "tracked", False),
    ("japan_japaneseoretruck", "RA1 Japan", 1000, 20, 2, 1, 1, 120, 100, "tracked", False),
    # Red Alert 2
    ("ra2_allies_chronominer", "RA2 Allies", 1000, 16, 4, 2, 1, 100, 100, "tracked", True),
    ("ra2_soviets_warminer", "RA2 Soviets", 1200, 32, 2, 2, 1, 75, 100, "tracked", False),
    ("YRSLAV", "RA2 Yuri", 250, 8, 8, 4, 1, 60, 80, "chem", False),
    # RA2 Mod factions
    ("asianalliance_droneminer", "Asian Alliance", 250, 4, 4, 4, 1, 125, 100, "chem", False),
    ("steelconsortium_consortiumminer", "Steel Consortium", 1000, 24, 2, 1, 1, 100, 100, "hover", False),
    ("latinsyndicate_collectiontruck", "Latin Syndicate", 1000, 32, 3, 2, 1, 115, 100, "tracked", False),
    ("naxis_slave", "Naxis", 250, 8, 8, 4, 1, 60, 80, "chem", False),
    ("schwarzermond_noidharvester", "Schwarzer Mond", 500, 100, 5, 1, 1, 50, 100, "chem", False),
    ("futuretech_prospector", "FutureTech", 1000, 32, 4, 2, 1, 100, 100, "tracked", False),
    ("futuretech_prospectormk2", "FutureTech", 1200, 32, 4, 2, 1, 120, 100, "hover", False),
    ("tkm_templateharvesterraname", "TKM", 1000, 30, 3, 1, 1, 90, 100, "tracked", False),
    ("tkmworker", "TKM", 250, 8, 8, 4, 1, 60, 80, "chem", False),
    # Dune 2000 — inherit ^Harvester (BLD=4 default), override BUD=1. Speed=45, tracked
    ("ixian_spiceharvester", "D2k Ixians", 500, 45, 4, 1, 1, 45, 100, "tracked", False),
    ("ordos_spiceharvester", "D2k Ordos", 500, 45, 4, 1, 1, 45, 100, "tracked", False),
    # StarCraft — ^SCWorker: Speed=100, swimsuit. Cap=5, BLD=5, BUD=1, BUA=5, FLS=100
    ("protoss_probe", "SC Protoss", 500, 5, 5, 1, 5, 100, 100, "swimsuit", False),
    ("zerg_drone", "SC Zerg", 500, 5, 5, 1, 5, 100, 100, "swimsuit", False),
    ("terran_scv", "SC Terran", 500, 5, 5, 1, 5, 100, 100, "swimsuit", False),
    # Warcraft 2 — ^WC2Worker: Speed=80, Cap=8, BLD=8, BUD=1, BUA=2, FLS=50, foot
    ("wc2_humans_peasant", "WC2 Humans", 500, 8, 8, 1, 2, 80, 50, "foot", False),
    ("wc2_orcs_peon", "WC2 Orcs", 500, 8, 8, 1, 2, 80, 50, "foot", False),
    ("wc2_humans_militiapeasant", "WC2 Humans", 300, 4, 4, 1, 2, 90, 75, "foot", False),
]


def compute_row(h):
    actor, faction, cost, cap, bld, bud, bua, speed, fls, loco, chrono = h

    terrain_speed = TERRAIN_SPEEDS.get(loco, 80)

    # Load time: each bale = 1 tick harvest + BaleLoadDelay ticks wait
    t_load = cap * (bld + 1)

    # Unload time: ceil(Cap/BUA) batches, first immediate, then BaleUnloadDelay each, +1 final check
    batches = math.ceil(cap / bua)
    t_unload = batches * bud + 1

    # Travel out (empty): speed modifier = 100% (Fullness=0)
    eff_speed_out = speed * terrain_speed // 100
    t_travel_out = D * CELL_DIST / eff_speed_out if eff_speed_out > 0 else 999999

    # Travel return (full): speed modifier = FullyLoadedSpeed%
    eff_speed_ret = speed * fls // 100 * terrain_speed // 100
    if chrono:
        t_travel_ret = 0
    elif eff_speed_ret > 0:
        t_travel_ret = D * CELL_DIST / eff_speed_ret
    else:
        t_travel_ret = 999999

    t_cycle = t_load + t_travel_out + t_unload + t_travel_ret

    cash_per_trip = cap * RV
    cash_per_tick = cash_per_trip / t_cycle
    cash_per_tick_per_cost = cash_per_tick / cost

    return {
        "actor": actor,
        "faction": faction,
        "cost": cost,
        "cap": cap,
        "bld": bld,
        "bud": bud,
        "bua": bua,
        "speed": speed,
        "fls": fls,
        "loco": loco,
        "terrain_speed": terrain_speed,
        "eff_speed_out": eff_speed_out,
        "eff_speed_ret": eff_speed_ret if not chrono else 0,
        "chrono": chrono,
        "t_load": t_load,
        "t_unload": t_unload,
        "t_travel_out": t_travel_out,
        "t_travel_ret": t_travel_ret,
        "t_cycle": t_cycle,
        "cash_per_trip": cash_per_trip,
        "cash_per_tick": cash_per_tick,
        "cash_per_tick_per_cost": cash_per_tick_per_cost,
    }


rows = [compute_row(h) for h in HARVESTERS]
rows.sort(key=lambda r: r["cash_per_tick_per_cost"], reverse=True)

# Generate HTML
html_parts = []
html_parts.append("""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Cameo Mod — Harvester Balance Table (Corrected)</title>
<style>
  @page { size: landscape; margin: 12mm; }
  body { font-family: 'Segoe UI', Tahoma, sans-serif; font-size: 11px; color: #1a1a1a; margin: 0; padding: 20px; background: #f5f5f5; }
  h1 { font-size: 22px; margin: 0 0 4px 0; color: #333; }
  .subtitle { color: #666; font-size: 12px; margin-bottom: 12px; }
  .meta-box { background: #e8eef7; border: 1px solid #b0c4de; border-radius: 4px; padding: 10px 15px; margin-bottom: 15px; font-size: 11px; line-height: 1.7; }
  .meta-box strong { color: #2c3e50; }
  table { border-collapse: collapse; width: 100%; background: #fff; box-shadow: 0 1px 3px rgba(0,0,0,0.12); }
  th, td { border: 1px solid #ccc; padding: 4px 6px; text-align: center; white-space: nowrap; }
  th { background: #4a90d9; color: #fff; font-weight: 600; font-size: 10px; text-transform: uppercase; letter-spacing: 0.3px; }
  th.actor-col { text-align: left; min-width: 220px; }
  td.actor-col { text-align: left; font-family: 'Consolas', monospace; font-size: 10px; }
  tr:nth-child(even) { background: #f9f9f9; }
  tr:hover { background: #e3f0ff; }
  .best { background: #d4edda !important; font-weight: bold; }
  .worst { background: #f8d7da !important; }
  .section-title { font-size: 14px; margin: 20px 0 8px 0; color: #555; border-bottom: 2px solid #4a90d9; padding-bottom: 3px; }
  .notes { font-size: 10px; line-height: 1.6; margin-top: 15px; color: #444; }
  .notes li { margin-bottom: 4px; }
  .notes strong { color: #2c3e50; }
  .formula { font-family: 'Consolas', monospace; background: #f0f0f0; padding: 1px 4px; border-radius: 2px; }
</style>
</head>
<body>

<h1>Cameo Mod &mdash; Harvester Balance Table (Corrected)</h1>
<div class="subtitle">Sorted by Cash/Tick/Cost (descending). Generated 2026-08-03. Formulas verified from engine C# source.</div>

<div class="meta-box">
<strong>Corrected Formulas (from engine source):</strong><br>
<span class="formula">FullyLoadedSpeed</span> is a <strong>percentage of base Speed</strong>, not an absolute speed value.<br>
&emsp;&emsp;<span class="formula">Harvester.cs:284: GetSpeedModifier() = 100 - (100 - FullyLoadedSpeed) * Fullness / 100</span><br>
&emsp;&emsp;When empty: modifier = 100 (100% of Speed). When full: modifier = FullyLoadedSpeed (e.g. 100 = no slowdown).<br>
&emsp;&emsp;Effective speed = Speed &times; terrainSpeed% &times; speedModifier%<br>
<span class="formula">T_load</span> = Cap &times; (BaleLoadDelay + 1) &mdash; each bale: 1 tick harvest + BaleLoadDelay wait (HarvestResource.cs:95-103)<br>
<span class="formula">T_unload</span> = ceil(Cap / BaleUnloadAmount) &times; BaleUnloadDelay + 1 &mdash; first batch immediate, then delay per batch (Harvester.cs:176-202)<br>
<span class="formula">T_travel</span> = D &times; 1024 / MovementSpeedForCell &mdash; progress per tick = effective speed (Move.cs:471)<br>
<span class="formula">T_cycle</span> = T_load + T_travel_out + T_unload + T_travel_ret<br>
<span class="formula">Cash/Trip</span> = Cap &times; RV (RV=25) &nbsp;|&nbsp; <span class="formula">Cash/Tick</span> = Cash/Trip &divide; T_cycle &nbsp;|&nbsp; <span class="formula">Cash/Tick/Cost</span> = Cash/Tick &divide; Cost<br>
<br>
<strong>Parameters:</strong> D = 20 cells | RV = 25 | Terrain = Clear (80% for tracked/wheeled, 90% for foot/swimsuit/chem/hover)<br>
Chrono miners: T_travel_ret = 0 (teleport via ChronoResourceDelivery)
</div>
""")

html_parts.append("""<table>
<thead>
<tr>
  <th>#</th>
  <th class="actor-col">Actor</th>
  <th>Faction</th>
  <th>Cost</th>
  <th>Cap</th>
  <th>BLD</th>
  <th>BUD</th>
  <th>BUA</th>
  <th>Spd</th>
  <th>FLS%</th>
  <th>Loco</th>
  <th>EffSpd<br>(out)</th>
  <th>EffSpd<br>(ret)</th>
  <th>T_load</th>
  <th>T_unload</th>
  <th>T_trav<br>(out)</th>
  <th>T_trav<br>(ret)</th>
  <th>T_cycle</th>
  <th>Cash/<br>Trip</th>
  <th>Cash/<br>Tick</th>
  <th>Cash/Tick<br>/Cost</th>
</tr>
</thead>
<tbody>
""")

for i, r in enumerate(rows):
    cls = ""
    if i == 0:
        cls = ' class="best"'
    elif i == len(rows) - 1:
        cls = ' class="worst"'

    chrono_note = " (chrono)" if r["chrono"] else ""

    html_parts.append(
        f"<tr{cls}>"
        f"<td>{i+1}</td>"
        f'<td class="actor-col">{html.escape(r["actor"])}{chrono_note}</td>'
        f"<td>{html.escape(r['faction'])}</td>"
        f"<td>{r['cost']}</td>"
        f"<td>{r['cap']}</td>"
        f"<td>{r['bld']}</td>"
        f"<td>{r['bud']}</td>"
        f"<td>{r['bua']}</td>"
        f"<td>{r['speed']}</td>"
        f"<td>{r['fls']}%</td>"
        f"<td>{r['loco']}</td>"
        f"<td>{r['eff_speed_out']}</td>"
        f"<td>{r['eff_speed_ret'] if not r['chrono'] else 'teleport'}</td>"
        f"<td>{r['t_load']}</td>"
        f"<td>{r['t_unload']}</td>"
        f"<td>{r['t_travel_out']:.1f}</td>"
        f"<td>{r['t_travel_ret']:.1f}</td>"
        f"<td>{r['t_cycle']:.1f}</td>"
        f"<td>{r['cash_per_trip']}</td>"
        f"<td>{r['cash_per_tick']:.2f}</td>"
        f"<td>{r['cash_per_tick_per_cost']:.5f}</td>"
        f"</tr>\n"
    )

html_parts.append("""</tbody>
</table>

<div class="section-title">Key Corrections from Engine Source</div>
<ul class="notes">
<li><strong>FullyLoadedSpeed is a percentage</strong> (Harvester.cs:284-287), not an absolute speed. When FullyLoadedSpeed=100 (inherited from ^Harvester in defaults.yaml), the harvester moves at <strong>100% of its base Speed</strong> even when full &mdash; no slowdown. The engine default is 85%.</li>
<li><strong>Load time includes +1 tick per bale</strong> for the actual harvest action (HarvestResource.cs:95-103). Each bale: 1 tick to harvest + BaleLoadDelay ticks wait. T_load = Cap &times; (BaleLoadDelay + 1).</li>
<li><strong>Unload time includes +1 tick</strong> for the final IsEmpty check (Harvester.cs:176-202). First batch is immediate, then BaleUnloadDelay per batch. T_unload = ceil(Cap/BUA) &times; BaleUnloadDelay + 1.</li>
<li><strong>Travel time uses terrain speed</strong> from the locomotor (Mobile.cs:756-761). Tracked=80%, foot=90%, chem=90%, swimsuit=90%, hover=90% on Clear terrain.</li>
<li><strong>Movement is progress-based</strong> (Move.cs:471): each tick adds MovementSpeedForCell to progress; cell completes when progress &ge; 1024. T_travel = D &times; 1024 / eff_speed.</li>
</ul>

<div class="section-title">Key Finding: Asian Alliance Drone Miner vs Schwarzer Mond Noid Harvester</div>
<div class="notes">
<p>Asian Alliance refinery spawns <strong>4 drone miners</strong> free; Schwarzer Mond refinery spawns <strong>2 noid harvesters</strong> free.</p>
<p>Total income per refinery (per tick):</p>
<ul>
<li><strong>Asian Alliance:</strong> Cash/Tick &times; 4 drones</li>
<li><strong>Schwarzer Mond:</strong> Cash/Tick &times; 2 noids</li>
</ul>
<p>The noid's bottleneck: Cap=100 with BaleLoadDelay=5 means T_load = 100 &times; 6 = 600 ticks of stationary harvesting per cycle.</p>
<p><strong>Corrections from YAML verification:</strong></p>
<ul>
<li><strong>chem locomotor</strong> Clear terrain speed = 90% (not 80% as previously assumed).</li>
<li><strong>TS harvesters</strong> capacity = 45 (not 35). Harvester.Capacity is not a real engine field; StoresResources.Capacity from ^TDHARV is used.</li>
<li><strong>Noid Harvester</strong>: Speed=50, Cap=100, BLD=5, BUD=1, locomotor=chem (all corrected from YAML).</li>
<li><strong>Asian Alliance Drone Miner</strong>: Cap=4, locomotor=chem (second Mobile block overrides).</li>
<li><strong>Steel Consortium Miner</strong>: locomotor=hover (second Mobile block overrides).</li>
<li><strong>RA1 Heavy Industrial Miner</strong>: locomotor=tracked (inherits from ^RAHARV, not heavytracked).</li>
</ul>
</div>

</body>
</html>""")

with open(OUT_HTML, "w", encoding="utf-8") as f:
    f.write("".join(html_parts))

# Generate PDF using fpdf2
from fpdf import FPDF

pdf = FPDF(orientation="L", unit="mm", format="A4")
pdf.set_auto_page_break(auto=True, margin=15)
pdf.add_page()

pdf.set_font("Helvetica", "B", 16)
pdf.cell(0, 8, "Cameo Mod - Harvester Balance Table (Corrected)", new_x="LMARGIN", new_y="NEXT")
pdf.set_font("Helvetica", "", 9)
pdf.cell(0, 5, "Sorted by Cash/Tick/Cost (descending). Formulas verified from engine C# source.", new_x="LMARGIN", new_y="NEXT")
pdf.ln(2)

# Formula box
pdf.set_font("Helvetica", "B", 8)
pdf.cell(0, 4, "Formulas: T_load=Cap*(BLD+1) | T_unload=ceil(Cap/BUA)*BUD+1 | T_travel=D*1024/eff_speed", new_x="LMARGIN", new_y="NEXT")
pdf.cell(0, 4, "T_cycle=T_load+T_trav_out+T_unload+T_trav_ret | Cash/Trip=Cap*RV(RV=25)", new_x="LMARGIN", new_y="NEXT")
pdf.set_font("Helvetica", "", 7)
pdf.cell(0, 4, "Cash/Tick=Cash/Trip/T_cycle | Cash/Tick/Cost=Cash/Tick/Cost | D=20 | Terrain=Clear (80% tracked, 90% foot/swimsuit/chem/hover) | Chrono: ret=0", new_x="LMARGIN", new_y="NEXT")
pdf.ln(2)

# Table header
headers = ["#", "Actor", "Faction", "Cost", "Cap", "BLD", "BUD", "BUA", "Spd", "FLS%", "Loco", "EffOut", "EffRet", "T_load", "T_unld", "T_tr_o", "T_tr_r", "T_cycle", "$/Trip", "$/Tick", "$/Tick/$"]
col_widths = [6, 48, 22, 10, 8, 8, 8, 8, 8, 10, 12, 10, 10, 10, 10, 10, 10, 12, 10, 10, 14]

pdf.set_font("Helvetica", "B", 7)
pdf.set_fill_color(74, 144, 217)
pdf.set_text_color(255, 255, 255)
for j, h in enumerate(headers):
    pdf.cell(col_widths[j], 6, h, border=1, fill=True, align="C")
pdf.ln()

# Table rows
pdf.set_text_color(0, 0, 0)
for i, r in enumerate(rows):
    if i == 0:
        pdf.set_fill_color(212, 237, 218)
    elif i == len(rows) - 1:
        pdf.set_fill_color(248, 215, 218)
    elif i % 2 == 0:
        pdf.set_fill_color(249, 249, 249)
    else:
        pdf.set_fill_color(255, 255, 255)

    pdf.set_font("Helvetica", "B" if i == 0 else "", 7)
    vals = [
        str(i + 1),
        r["actor"][:38],
        r["faction"][:16],
        str(r["cost"]),
        str(r["cap"]),
        str(r["bld"]),
        str(r["bud"]),
        str(r["bua"]),
        str(r["speed"]),
        f'{r["fls"]}%',
        r["loco"],
        str(r["eff_speed_out"]),
        "teleport" if r["chrono"] else str(r["eff_speed_ret"]),
        str(r["t_load"]),
        str(r["t_unload"]),
        f'{r["t_travel_out"]:.0f}',
        f'{r["t_travel_ret"]:.0f}' if not r["chrono"] else "0",
        f'{r["t_cycle"]:.0f}',
        str(r["cash_per_trip"]),
        f'{r["cash_per_tick"]:.2f}',
        f'{r["cash_per_tick_per_cost"]:.5f}',
    ]
    for j, v in enumerate(vals):
        pdf.cell(col_widths[j], 5, v, border=1, fill=True, align="L" if j == 1 else "C")
    pdf.ln()

pdf.add_page()
pdf.set_font("Helvetica", "B", 10)
pdf.cell(0, 6, "Key Corrections from YAML Verification", new_x="LMARGIN", new_y="NEXT")
pdf.set_font("Helvetica", "", 8)
corrections = [
    "chem locomotor Clear terrain speed = 90% (not 80% as previously assumed).",
    "TS harvesters capacity = 45 (not 35). Harvester.Capacity is not a real engine field; StoresResources.Capacity from ^TDHARV is used.",
    "Noid Harvester: Speed=50, Cap=100, BLD=5, BUD=1, locomotor=chem (all corrected from YAML).",
    "Asian Alliance Drone Miner: Cap=4, locomotor=chem (second Mobile block overrides).",
    "Steel Consortium Miner: locomotor=hover (second Mobile block overrides).",
    "RA1 Heavy Industrial Miner: locomotor=tracked (inherits from ^RAHARV, not heavytracked).",
]
for c in corrections:
    pdf.cell(0, 4, f"- {c}", new_x="LMARGIN", new_y="NEXT")

pdf.output(str(OUT_PDF))
print("PDF generated: docs/audit/harvester_balance_table.pdf")

# Also print a text summary
print(f"{'#':>2} {'Actor':<40} {'Faction':<18} {'Cost':>5} {'Cap':>4} {'T_load':>7} {'T_unload':>8} {'T_tr_out':>8} {'T_tr_ret':>8} {'T_cycle':>8} {'$/Trip':>7} {'$/Tick':>7} {'$/Tick/$':>10}")
print("-" * 145)
for i, r in enumerate(rows):
    print(f"{i+1:>2} {r['actor']:<40} {r['faction']:<18} {r['cost']:>5} {r['cap']:>4} {r['t_load']:>7} {r['t_unload']:>8} {r['t_travel_out']:>8.1f} {r['t_travel_ret']:>8.1f} {r['t_cycle']:>8.1f} {r['cash_per_trip']:>7} {r['cash_per_tick']:>7.2f} {r['cash_per_tick_per_cost']:>10.5f}")
