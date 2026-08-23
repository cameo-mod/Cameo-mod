#!/usr/bin/env python3
"""Windows-friendly Python equivalent of ``tools/audit/run_all.sh``.

Runs the full Cameo audit suite, writes one markdown report per audit to
docs/audit/latest/, regenerates docs/factions/MATRIX.md, and exits non-zero
if any blocking audit fails.

``run_all.sh`` is the CANONICAL entry point (CLAUDE.md rule 8). This module
exists for shells without ``sh`` and MUST produce byte-identical output:

* the same audit list — parsed straight out of ``run_all.sh`` so the two
  cannot drift, which is how the tree ended up with a duplicate report set
  (``docs/audit/latest/audit_<name>.md`` *and* ``<name>.md``) in the first
  place: this script used to prefix every report with ``audit_``;
* the same report FILENAMES (``<name>.md``, no ``audit_`` prefix).

Never hand-maintain a second audit list here. Add audits to ``run_all.sh``.
"""
import os
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "docs" / "audit" / "latest"
OUT.mkdir(parents=True, exist_ok=True)
(ROOT / "docs" / "factions").mkdir(parents=True, exist_ok=True)

SH = ROOT / "tools" / "audit" / "run_all.sh"

PYTHON = sys.executable
failed = 0

# Force child processes to emit UTF-8 regardless of the OS console codepage
# (Windows defaults to cp1252, which corrupts §, —, etc. in audit output —
# see LESSONS_LEARNED.md). Passing a file object directly as stdout= to
# subprocess.run() writes raw bytes from the child at the OS level, bypassing
# the parent file object's own encoding, so this must be set via env instead.
CHILD_ENV = dict(os.environ, PYTHONIOENCODING="utf-8")


def audits_from_shell() -> list[str]:
    """Read the audit-name list out of the ``for a in … ; do`` loop in run_all.sh.

    Parsing the shell script is deliberate: a hand-copied list in this file is
    exactly the drift that produced the duplicate report set.
    """
    text = SH.read_text(encoding="utf-8")
    m = re.search(r"^for a in (.*?); do$", text, re.MULTILINE | re.DOTALL)
    if not m:
        raise SystemExit(f"cannot find the audit list in {SH}")
    body = m.group(1).replace("\\\n", " ")
    # Drop comment lines that the DOTALL match may have swallowed.
    body = "\n".join(ln for ln in body.splitlines() if not ln.lstrip().startswith("#"))
    return body.split()


# Audits that live outside tools/audit/, mirroring the second loop in run_all.sh.
EXTRAS = [
    ("createeffect_image", ROOT / "tools" / "audit_createeffect_image.py"),
    ("ce_image_usage", ROOT / "tools" / "audit_ce_image_usage.py"),
    ("empty_warhead", ROOT / "tools" / "audit" / "find_empty_warhead.py"),
    ("gen_sync", ROOT / "tools" / "balance" / "verify_generator_sync.py"),
]


def run(name, script, extra_args=None):
    global failed
    print(f"== {name}")
    md = OUT / f"{name}.md"
    err = OUT / f"{name}.err"
    cmd = [PYTHON, str(script)] + (extra_args or [])
    with md.open("w", encoding="utf-8") as out, err.open("w", encoding="utf-8") as e:
        result = subprocess.run(cmd, cwd=ROOT, stdout=out, stderr=e, text=True, env=CHILD_ENV)
    if result.returncode != 0:
        failed = 1
        print(f"   FAILED: {name} (exit {result.returncode})")
    if err.stat().st_size == 0:
        err.unlink()


for a in audits_from_shell():
    run(a, ROOT / "tools" / "audit" / f"audit_{a}.py")

for name, path in EXTRAS:
    run(name, path)

# audit_unconverted_templates writes its OWN report with --write; its stdout is only a
# short summary, so routing it through run() would clobber the real report.
print("== unconverted_templates")
_r = subprocess.run([PYTHON, str(ROOT / "tools" / "audit" / "audit_unconverted_templates.py"),
                     "--write"], cwd=ROOT, capture_output=True, text=True, env=CHILD_ENV)
if _r.returncode != 0:
    failed = 1
    print(f"   FAILED: unconverted_templates (exit {_r.returncode})")

# Staleness gate for the mandatory recurring audits (docs/audit/periodic.json).
# --warn-only: this suite is the PER-COMMIT gate, so a late scheduled scan must
# not turn it red for a reason unrelated to the commit being made. See
# docs/audit/PERIODIC.md.
run("periodic_freshness", ROOT / "tools" / "audit" / "audit_periodic_freshness.py",
    ["--warn-only"])

for name, script, dest in [
    ("gen_damage_matrix", ROOT / "tools" / "audit" / "gen_damage_matrix.py", OUT / "damage_matrix.md"),
    ("gen_rename_maps", ROOT / "tools" / "audit" / "gen_rename_maps.py", OUT / "naming.md"),
    ("gen_faction_matrix", ROOT / "tools" / "audit" / "gen_faction_matrix.py", ROOT / "docs" / "factions" / "MATRIX.md"),
]:
    print(f"== {name}")
    with dest.open("w", encoding="utf-8") as out:
        result = subprocess.run([PYTHON, str(script)], cwd=ROOT, stdout=out, text=True, env=CHILD_ENV)
    if result.returncode != 0:
        failed = 1
        print(f"   FAILED: {name} (exit {result.returncode})")

print(f"reports in {OUT}/ ; matrix in docs/factions/MATRIX.md")
sys.exit(failed)
