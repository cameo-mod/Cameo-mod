---
description: Run any Python balance/audit/script through the guard wrapper
---

# Safe Python execution workflow

All Python scripts in this repo must be executed through the guard wrapper so
syntax is checked first and a 60-second timeout prevents runaway processes.

1. Syntax-check the script first: `python -m py_compile <script.py>`
2. Run it through the guard: `python tools/balance/run_with_guard.py <script.py> [args]`
3. If the guard times out, do not re-run blindly — inspect the script for infinite
   loops or expensive operations and fix the root cause.

Examples:
- `python tools/balance/run_with_guard.py tools/balance/extract_stats.py`
- `python tools/balance/run_with_guard.py tools/balance/build_workbook.py`
- `python tools/balance/run_with_guard.py tools/audit/audit_min_range.py`
