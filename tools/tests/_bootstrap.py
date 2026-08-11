"""Put tools/audit on sys.path so the audit modules import as top-level names."""

from __future__ import annotations

import pathlib
import sys

REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
AUDIT_DIR = REPO_ROOT / "tools" / "audit"

for candidate in (str(AUDIT_DIR), str(REPO_ROOT)):
    if candidate not in sys.path:
        sys.path.insert(0, candidate)
