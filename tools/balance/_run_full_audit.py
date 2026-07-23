#!/usr/bin/env python3
"""Windows-friendly equivalent of tools/audit/run_all.sh.

Runs the full Cameo audit suite, writes one markdown report per audit to
docs/audit/latest/, regenerates docs/factions/MATRIX.md, and exits non-zero
if any blocking audit fails.
"""
import os
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "docs" / "audit" / "latest"
OUT.mkdir(parents=True, exist_ok=True)
(ROOT / "docs" / "factions").mkdir(parents=True, exist_ok=True)

PYTHON = sys.executable
failed = 0

AUDITS = [
    "inherits", "faction_leaks", "upgrades", "upgrade_coverage", "ai",
    "sequences", "metadata", "outliers", "orphans", "assets", "fluent",
    "power_budget", "stat_formulas", "weapon_uniqueness", "garrison_weapons",
    "asset_files", "promotion_gating", "min_range", "basebuilder_crates",
    "buildable_order", "display_text", "rename_safety", "elite_naming",
    "missing_elite", "elite_gating", "rank_decoration", "dune_rank_decoration",
    "effect_warhead_names", "weapon_suffixes", "balance_sheet",
    "consistency_report", "packs", "balance_drift", "template_conformance",
    "multiplier_modifiers",
]

EXTRAS = [
    ("createeffect_image", ROOT / "tools" / "audit_createeffect_image.py"),
    ("ce_image_usage", ROOT / "tools" / "audit_ce_image_usage.py"),
]


def run(name, script, extra_args=None):
    global failed
    print(f"== {name}")
    md = OUT / f"{name}.md"
    err = OUT / f"{name}.err"
    cmd = [PYTHON, str(script)] + (extra_args or [])
    with md.open("w", encoding="utf-8") as out, err.open("w", encoding="utf-8") as e:
        result = subprocess.run(cmd, cwd=ROOT, stdout=out, stderr=e, text=True)
    if result.returncode != 0:
        failed = 1
        print(f"   FAILED: {name} (exit {result.returncode})")
    else:
        # remove empty .err files
        if err.stat().st_size == 0:
            err.unlink()


for a in AUDITS:
    run(f"audit_{a}", ROOT / "tools" / "audit" / f"audit_{a}.py")

for name, path in EXTRAS:
    run(name, path)

# Generators
for name, script, dest in [
    ("gen_damage_matrix", ROOT / "tools" / "audit" / "gen_damage_matrix.py", OUT / "damage_matrix.md"),
    ("gen_rename_maps", ROOT / "tools" / "audit" / "gen_rename_maps.py", OUT / "naming.md"),
    ("gen_faction_matrix", ROOT / "tools" / "audit" / "gen_faction_matrix.py", ROOT / "docs" / "factions" / "MATRIX.md"),
]:
    print(f"== {name}")
    with dest.open("w", encoding="utf-8") as out:
        result = subprocess.run([PYTHON, str(script)], cwd=ROOT, stdout=out, text=True)
    if result.returncode != 0:
        failed = 1
        print(f"   FAILED: {name} (exit {result.returncode})")

print(f"reports in {OUT}/ ; matrix in docs/factions/MATRIX.md")
sys.exit(failed)
