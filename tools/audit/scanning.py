#!/usr/bin/env python3
"""scanning.py — shared file-walking helpers for the audit suite.

Every audit that walks source files needs the same two things: the set of
directories that must never be scanned (vendored engine code, build output,
caches, archived experiments) and a filtered ``rglob``. Keeping one copy here
stops the copies from drifting — `audit_code_duplication.py` reports the drift
when they do.
"""

from __future__ import annotations

import pathlib
from collections.abc import Iterator

# Build output, caches, vendored/archived trees: never audited as source.
SKIP_PARTS = frozenset((
    "archive", "__pycache__", "obj", "bin", ".git", ".vs", "node_modules",
))


def iter_files(base: pathlib.Path, suffix: str,
               skip: frozenset[str] = SKIP_PARTS) -> Iterator[pathlib.Path]:
    """Yield ``base``'s files ending in ``suffix``, sorted, skipping ``skip`` dirs."""
    if not base.exists():
        return
    for path in sorted(base.rglob(f"*{suffix}")):
        if any(part in skip for part in path.parts):
            continue
        yield path


def iter_dirs(root: pathlib.Path, rel_dirs: tuple[str, ...], suffix: str,
              skip: frozenset[str] = SKIP_PARTS) -> Iterator[pathlib.Path]:
    """``iter_files`` over several repo-relative directories in order."""
    for rel in rel_dirs:
        yield from iter_files(root / rel, suffix, skip)
