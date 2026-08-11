#!/usr/bin/env python3
"""scanning.py — shared file-walking helpers for the audit suite.

Every audit that walks source files needs the same two things: the set of
directories that must never be scanned (vendored engine code, build output,
caches, archived experiments) and a filtered ``rglob``. Keeping one copy here
stops the copies from drifting — `audit_code_duplication.py` reports the drift
when they do.
"""

from __future__ import annotations

import functools
import pathlib
import subprocess
from collections.abc import Iterator

# Build output, caches, vendored/archived trees: never audited as source.
SKIP_PARTS = frozenset((
    "archive", "__pycache__", "obj", "bin", ".git", ".vs", "node_modules",
))


@functools.lru_cache(maxsize=None)
def tracked_under(base: str) -> frozenset[str] | None:
    """Resolved paths of the git-TRACKED files under ``base``, or None.

    None means "cannot tell" — not a git checkout, or git is unavailable (a
    source tarball, a CI image without git) — and callers must then scan
    everything, exactly as before.

    Why this exists (2026-08-11): the audits are RATCHETS. If they walk the raw
    filesystem, any scratch file a maintainer or agent leaves in `tools/` counts
    as an untested/duplicated/unsafe module and turns `run_all.sh` red for
    somebody else's uncommitted work — a baseline that depends on what happens to
    be lying in the working tree is not a baseline. Tracked-only makes the counts
    reproducible: CI, a fresh clone and a dirty working tree all agree.
    """
    path = pathlib.Path(base)
    if not path.exists():
        return None
    try:
        out = subprocess.run(
            ["git", "-C", str(path), "ls-files", "-z"],
            capture_output=True, check=True, text=True, timeout=60).stdout
    except (OSError, subprocess.SubprocessError):
        return None
    return frozenset(str((path / rel).resolve())
                     for rel in out.split("\0") if rel)


def iter_files(base: pathlib.Path, suffix: str,
               skip: frozenset[str] = SKIP_PARTS) -> Iterator[pathlib.Path]:
    """Yield ``base``'s TRACKED files ending in ``suffix``, sorted, skipping ``skip``.

    Untracked files are ignored when ``base`` is inside a git checkout; when it is
    not (or git is unavailable) every matching file is yielded — see
    ``tracked_under``.
    """
    if not base.exists():
        return
    tracked = tracked_under(str(base))
    for path in sorted(base.rglob(f"*{suffix}")):
        if any(part in skip for part in path.parts):
            continue
        if tracked is not None and str(path.resolve()) not in tracked:
            continue
        yield path


def iter_dirs(root: pathlib.Path, rel_dirs: tuple[str, ...], suffix: str,
              skip: frozenset[str] = SKIP_PARTS) -> Iterator[pathlib.Path]:
    """``iter_files`` over several repo-relative directories in order."""
    for rel in rel_dirs:
        yield from iter_files(root / rel, suffix, skip)
