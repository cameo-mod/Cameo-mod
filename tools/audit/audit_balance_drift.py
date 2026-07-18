#!/usr/bin/env python3
"""audit_balance_drift.py — Balance Pipeline Phase 6 enforcement.

Re-extracts every faction's raw stats and diffs them against the
committed ledger (docs/balance/*.json). ANY difference means somebody
hand-edited a balance number in yaml (or forgot to re-extract after a
sanctioned `apply_balance.py` run) — a red finding either way.

The sanctioned loop (BALANCE_PIPELINE.md): edit the LEDGER (directly or
via the generated workbook + import_workbook.py), then
`apply_balance.py --confirm`, then re-run `extract_stats.py` and commit
yaml + ledger TOGETHER.
"""
from __future__ import annotations

import difflib
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/balance"))
sys.path.insert(0, str(ROOT / "tools/audit"))

import extract_stats  # noqa: E402
from cameo_model import Model  # noqa: E402

LEDGER = ROOT / "docs/balance"


def main() -> int:
    print("# audit_balance_drift — yaml vs committed balance ledger\n")
    fresh = extract_stats.build_ledgers(Model())
    drifted = []
    for name, doc in sorted(fresh.items()):
        p = LEDGER / f"{name}.json"
        want = extract_stats.serialize(doc)
        have = p.read_text(encoding="utf-8") if p.exists() else ""
        if want != have:
            sample = list(difflib.unified_diff(
                have.split("\n"), want.split("\n"), lineterm="", n=1))[3:15]
            drifted.append((name, sample))
    if not drifted:
        print(f"_clean_ — {len(fresh)} ledgers match the live rules exactly.\n")
        return 0
    print(f"**{len(drifted)} ledger(s) drifted** — balance numbers were "
          "hand-edited in yaml, or a sanctioned apply run was not followed "
          "by re-extraction. Fix via the pipeline, never by hand:\n")
    for name, sample in drifted:
        print(f"## {name}\n")
        print("```diff")
        print("\n".join(sample))
        print("```\n")
    return 1


if __name__ == "__main__":
    sys.exit(main())
