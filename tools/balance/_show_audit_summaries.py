#!/usr/bin/env python3
import pathlib

REPORTS = [
    "stat_formulas",
    "balance_drift",
    "min_range",
    "outliers",
    "weapon_uniqueness",
    "promotion_gating",
    "balance_sheet",
    "buildable_order",
]

OUT = pathlib.Path("docs/audit/latest")
DEST = OUT / "_infantry_audit_summary.txt"
with DEST.open("w", encoding="utf-8") as out:
    for name in REPORTS:
        path = OUT / f"{name}.md"
        out.write(f"\n=== {name}.md ===\n")
        if not path.exists():
            out.write("(missing)\n")
            continue
        raw = path.read_bytes().replace(b"\x00", b"")
        text = raw.decode("utf-8", "replace")
        out.write(text[:1500])
        out.write("...\n")
print(f"wrote {DEST}")
