#!/usr/bin/env python3
"""audit_doc_claims.py — re-measure every numeric claim the design docs rest on.

Maintainer 2026-08-17: *"the documentations are starting to explode in size ... a lot of
information and design decisions are maybe outdated and contradict newer decisions."*

The problem is structural, not sloppiness: **prose cannot be checked**, so a number written
into a document is true only on the day it is written. Five real cases from one session:

  * "Shield is free against 51% of the roster"   -> actually **1.4%** (35x wrong; it counted
    empty shield CAPACITY as a shield)
  * "Shield = top + floor" stated as binding law -> retired weeks earlier, still live in 2 docs
  * "apply_balance --confirm is the pending step" -> it is a **NO-OP** on every faction
  * R1 "veterancy grants HP instead"             -> I advised the opposite and it was accepted
  * "805 of 2053 weapons comply (39%)"           -> 522 of 1495 FIRED weapons (34.9%)

Every one was invisible to every other gate: valid yaml, resolver happy, game boots. A boot
gate cannot see a number that is merely wrong.

So each claim a DECISION rests on gets an entry in `docs/audit/doc_claims.yaml` with a
`measure` snippet. This audit runs them and fails when the document and the tree disagree —
which turns "the docs went stale" from a future misunderstanding into a red audit today.

⚠ This does NOT detect prose contradictions (two docs asserting incompatible LAWS in words).
That still needs a human read; §Review cadence below is the process for it. What this pins is
every claim reducible to a number, which is where the costly errors have actually been.

Exit 1 on any mismatch, on a broken `measure`, or on a claim naming a doc that does not exist.
"""
from __future__ import annotations

import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))
sys.path.insert(0, str(ROOT / "tools" / "audit"))

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

REGISTRY = ROOT / "docs" / "audit" / "doc_claims.yaml"


def load_registry(path: pathlib.Path) -> list[dict]:
    """Minimal reader for the registry's shape.

    Deliberately hand-rolled rather than pulling in PyYAML: the repo's audits run on a bare
    interpreter, and `miniyaml` is built for MiniYaml (tabs, no block scalars) rather than for
    this file's `|` blocks and `[a, b]` flow lists.
    """
    claims: list[dict] = []
    cur: dict | None = None
    key: str | None = None
    block: list[str] | None = None
    block_indent = 0

    for raw in path.read_text(encoding="utf-8").splitlines():
        if block is not None:
            if raw.strip() == "" or raw.startswith(" " * block_indent):
                block.append(raw[block_indent:] if len(raw) >= block_indent else "")
                continue
            cur[key] = "\n".join(block).strip("\n")  # type: ignore[index]
            block = None
            key = None

        line = raw.rstrip()
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        stripped = line.strip()

        if stripped.startswith("- id:"):
            if cur:
                claims.append(cur)
            cur = {"id": stripped.split(":", 1)[1].strip()}
            continue
        if cur is None:
            continue
        if ":" not in stripped:
            continue
        k, _, v = stripped.partition(":")
        k, v = k.strip(), v.strip()
        if v in ("|", ">", "|-", ">-"):
            key = k
            block = []
            block_indent = len(raw) - len(raw.lstrip()) + 2
            continue
        if v.startswith("[") and v.endswith("]"):
            cur[k] = [x.strip() for x in v[1:-1].split(",") if x.strip()]
            continue
        cur[k] = v

    if block is not None and cur is not None and key:
        cur[key] = "\n".join(block).strip("\n")
    if cur:
        claims.append(cur)
    return claims


def measure(snippet: str) -> tuple[float | None, str]:
    """Run a measure snippet in a fresh interpreter at the repo root."""
    try:
        proc = subprocess.run([sys.executable, "-c", snippet], capture_output=True,
                              text=True, cwd=str(ROOT), timeout=600)
    except subprocess.TimeoutExpired:
        return None, "timed out"
    if proc.returncode != 0:
        tail = (proc.stderr or "").strip().splitlines()
        return None, (tail[-1] if tail else f"exit {proc.returncode}")
    out = (proc.stdout or "").strip().splitlines()
    if not out:
        return None, "printed nothing"
    try:
        return float(out[-1].strip()), ""
    except ValueError:
        return None, f"not a number: {out[-1][:60]!r}"


def main() -> int:
    claims = load_registry(REGISTRY)
    print("# audit_doc_claims — do the documents still match the tree?")
    print()
    print(f"Registry: `{REGISTRY.relative_to(ROOT).as_posix()}` — **{len(claims)}** claims.")
    print()
    print("A number in prose is true only on the day it is written. These are the claims a "
          "DECISION rests on, re-measured every run.")
    print()

    failed = 0
    rows = []
    for c in claims:
        cid = c.get("id", "?")
        snippet = c.get("measure")
        try:
            want = float(c.get("value"))
        except (TypeError, ValueError):
            rows.append((cid, None, None, "no `value`"))
            failed = 1
            continue
        tol = float(c.get("tolerance", 0) or 0)
        if not snippet:
            rows.append((cid, want, None, "no `measure`"))
            failed = 1
            continue
        got, err = measure(snippet)
        if got is None:
            rows.append((cid, want, None, err))
            failed = 1
            continue
        limit = abs(want) * tol
        ok = abs(got - want) <= limit + 1e-12
        if not ok:
            failed = 1
        rows.append((cid, want, got, "" if ok else "MISMATCH"))

        # A claim must also name docs that exist, or the pointer rots too.
        for d in c.get("docs", []) or []:
            if not (ROOT / d).exists():
                rows.append((f"{cid} → {d}", None, None, "doc missing"))
                failed = 1

    print("| claim | documented | measured | status |")
    print("|---|--:|--:|---|")
    for cid, want, got, note in rows:
        w = "—" if want is None else f"{want:g}"
        g = "—" if got is None else f"{got:g}"
        status = "✅" if not note else f"**{note}**"
        print(f"| `{cid}` | {w} | {g} | {status} |")
    print()

    if failed:
        print("**FAIL — a document and the tree disagree.**")
        print()
        print("Fix whichever is wrong, and if the tree is right update `value` in "
              "`doc_claims.yaml` **and every doc listed under `docs:`** in the SAME commit. "
              "That co-update is the point: it is how the `Shield = top + floor` duplication "
              "survived in two documents for weeks.")
    else:
        print("_clean_ — every registered claim still matches the tree.")

    print()
    print("## Review cadence (for what a number cannot capture)")
    print()
    print("This audit pins numeric claims. **Prose contradictions — two documents asserting "
          "incompatible LAWS in words — still need a human read.** The failure mode is "
          "specific and worth naming: a ruling gets made, written into one document, and the "
          "older statement is left standing somewhere else. Both then look authoritative.")
    print()
    print("Known instances of exactly that, all found by accident rather than by process:")
    print()
    print("| the newer ruling | what still contradicted it |")
    print("|---|---|")
    print("| Shield ladder is derived (DESIGN §12.0c) | `Shield = top + floor` in DESIGN **and** "
          "ARMOR_SYSTEM |")
    print("| R1 — veterancy grants HP | advice to keep veterancy multipliers, accepted |")
    print("| Platings are layer-SELECTED | \"armor types AVERAGE\" in memory + §A1–A4 |")
    print("| W24 answers the 3-same-family question | W23 still listed as blocked on a ruling |")
    print()
    print("**The rule that would have caught all four:** a ruling is not landed until the "
          "OLD statement is struck in every document that carries it. Grep for the old claim "
          "before writing the new one — `docs:` lists in this registry exist to make that "
          "mechanical for numbers, and the same discipline applies to laws.")
    return failed


if __name__ == "__main__":
    raise SystemExit(main())
