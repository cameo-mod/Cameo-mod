#!/usr/bin/env python3
"""Test LANE 4 cargo_capacity and open_topped extraction.

Per FLEET_ORDERS_2026-09-08 SS10 LANE 4:
- td_gdi_apc: cargo_capacity=8, open_topped=False
- td_gdi_assaultapc: cargo_capacity=8, open_topped=True
- ra1_allies_alliedapc: cargo_capacity=5, open_topped=False
- ra2_allies_battlefortress: cargo_capacity=6, open_topped=True
"""
import os
import sys

os.chdir(r"C:\tmp\aurora-lane4")
sys.path.insert(0, r"C:\tmp\aurora-lane4\tools\audit")
sys.path.insert(0, r"C:\tmp\aurora-lane4\tools\balance")

from cameo_model import Model
from extract_stats import extract_actor

model = Model()
rs = model.rs

expected = {
    "td_gdi_apc": {"cargo_capacity": "8", "open_topped": False},
    "td_gdi_assaultapc": {"cargo_capacity": "8", "open_topped": True},
    "ra1_allies_alliedapc": {"cargo_capacity": "5", "open_topped": False},
    "ra2_allies_battlefortress": {"cargo_capacity": "6", "open_topped": True},
}

passed = 0
failed = 0

for name, exp in expected.items():
    print(f"\n=== {name} ===")
    u = extract_actor(rs, name, "vehicles")
    if u is None:
        print(f"  FAIL: actor not found")
        failed += 1
        continue

    # Check cargo_capacity
    cc = u.get("cargo_capacity")
    if cc is None:
        print(f"  FAIL: no cargo_capacity field")
        failed += 1
    else:
        actual = str(cc["v"]).strip()
        if actual == exp["cargo_capacity"]:
            print(f"  PASS: cargo_capacity={actual} (src={cc['src']})")
            passed += 1
        else:
            print(f"  FAIL: cargo_capacity={actual}, expected {exp['cargo_capacity']}")
            failed += 1

    # Check open_topped
    ot = u.get("open_topped")
    if exp["open_topped"]:
        if ot is not None and ot["v"] is True:
            print(f"  PASS: open_topped=True (src={ot['src']})")
            passed += 1
        else:
            print(f"  FAIL: open_topped should be True, got {ot}")
            failed += 1
    else:
        if ot is None:
            print(f"  PASS: open_topped not present (False)")
            passed += 1
        else:
            print(f"  FAIL: open_topped should be absent, got {ot}")
            failed += 1

print(f"\n=== Results: {passed} passed, {failed} failed ===")
sys.exit(0 if failed == 0 else 1)
