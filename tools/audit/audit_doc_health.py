#!/usr/bin/env python3
"""audit_doc_health.py — the documentation's own gate.

`audit_doc_claims.py` checks whether the NUMBERS in the docs still match the tree.
This checks whether the docs are structurally sound at all. Both classes of rot were
found by hand on 2026-08-23 and neither had a detector:

  D1 literal control characters in a tracked document — four of them (BEL, FF) sat in
     `BALANCE_PROGRAM_PLAN.md`'s W24 row, left by a `\\a` / `\\f` escape, and had turned
     `audit_physical_state_warheads` into `udit_physical_state_warheads` on screen.
  D2 mojibake — UTF-8 bytes once decoded as cp1252 and re-encoded, so an em dash reads
     as `â€"`. Two docs carried it, one of them a machine-read registry.
  D3 a markdown link pointing at a file that does not exist.
  D4 a same-file `](#anchor)` with no matching heading — silent in every renderer.
  D5 a reference to a document this repository moved or deleted.
  D6 two sections sharing one id in DESIGN.md. `§12.0a` named two different BINDING
     laws at once, and code cites `§12.0a` — so the citation was ambiguous.

D1, D2 and D6 are BLOCKING: they are corruption or an ambiguous law. D3–D5 are
reported and also block, because a dead pointer is how a reader ends up in the wrong
document. Nothing here needs the engine, so it runs anywhere.

Exceptions live in ALLOW_MOJIBAKE / GONE below and are deliberately narrow — several
documents QUOTE mojibake while explaining the bug, and must not be "fixed".
"""
from __future__ import annotations

import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]

# Documents that legitimately contain mojibake because they document the bug class.
ALLOW_MOJIBAKE = {
    # Each of these QUOTES the mojibake it is describing — the `Kübelwagen` weapon-name
    # incident, where `ü` became `Ã¼` and the weapon reference silently stopped matching.
    # "Fixing" the quote would delete the evidence.
    "docs/LESSONS_LEARNED.md",
    "docs/design/ROADMAP.md",
    "docs/history/ROADMAP_ARCHIVE_2026-07.md",
    "docs/history/handoffs/AI_AGENT_HANDOFF_2026-07-25.md",
}

# Huge generated reference; its thousands of image links are a known separate issue.
SKIP_LINKS = {"docs/Cameo_Knowledge_Base_Manual.md"}

# Documents this repository moved or removed. A live reference to one is stale.
GONE = {
    "docs/AI_HANDOFF_2026-08-05.md": "docs/history/handoffs/AI_HANDOFF_2026-08-05.md",
    "docs/design/AREADAMAGE_HANDOFF.md": "docs/history/handoffs/AREADAMAGE_HANDOFF_2026-08-04.md",
    "docs/design/CLAUDE_HANDOFF_2026-08-11.md": "docs/history/handoffs/CLAUDE_HANDOFF_2026-08-11.md",
    "docs/design/DEVIN_REPLY_2026_08_11.md": "docs/history/handoffs/DEVIN_REPLY_2026-08-11.md",
    "docs/design/DEVIN_HANDOFF_SP_RESEARCH_2026_08_11.md": "docs/history/handoffs/DEVIN_HANDOFF_SP_RESEARCH_2026-08-11.md",
    "docs/design/SESSION_CHECKPOINT_2026-08-03.md": "docs/history/handoffs/SESSION_CHECKPOINT_2026-08-03.md",
    "docs/design/MEGAPLAN.md": "docs/history/MEGAPLAN_2026-08-08.md",
    "docs/history/AI_AGENT_HANDOFF.md": "docs/history/handoffs/AI_AGENT_HANDOFF_2026-07-25.md",
    "docs/balance/LESSONS_LEARNED.md": "docs/LESSONS_LEARNED.md",
}

CTRL = re.compile(r"[\x00-\x08\x0b\x0c\x0e-\x1f]")
MOJIBAKE = re.compile("[ÂÃâ][-ÿ]{1,3}")
LINK = re.compile(r"\[[^\]]*\]\(([^)]+)\)")
ANCHOR = re.compile(r"\]\(#([^)]+)\)")
HEADING = re.compile(r"^#{1,6} (.+)$", re.M)
DESIGN_ID = re.compile(r"^#{2,4} (\d+(?:\.\d+)?[a-z]?)\.? ", re.M)


def tracked(*globs: str) -> list[pathlib.Path]:
    out = subprocess.run(["git", "ls-files", *globs], cwd=ROOT,
                         capture_output=True, text=True).stdout.split("\n")
    return [pathlib.Path(p) for p in out if p]


def slug(heading: str) -> str:
    """GitHub's heading -> anchor transform, close enough for our headings."""
    s = heading.strip().lower().replace("`", "")
    s = re.sub(r"\*\*|\*|_", "", s)
    s = "".join(c for c in s if c.isalnum() or c in " -")
    return re.sub(r"\s", "-", s)


def read(path: pathlib.Path) -> str | None:
    try:
        return (ROOT / path).read_text(encoding="utf-8")
    except (UnicodeDecodeError, OSError):
        return None


def main() -> int:
    docs = tracked("*.md")
    for extra in ("docs/HANDOFF.md", "docs/history/ROADMAP_ARCHIVE_2026-07.md"):
        p = pathlib.Path(extra)
        if (ROOT / p).exists() and p not in docs:
            docs.append(p)

    d1: list[str] = []
    d2: list[str] = []
    d3: list[str] = []
    d4: list[str] = []
    d5: list[str] = []
    d6: list[str] = []

    for f in docs:
        text = read(f)
        if text is None:
            d1.append(f"`{f}` — not valid UTF-8")
            continue
        rel = str(f).replace("\\", "/")

        for m in CTRL.finditer(text):
            line = text.count("\n", 0, m.start()) + 1
            d1.append(f"`{rel}`:{line} — control character {hex(ord(m.group()))}")

        if rel not in ALLOW_MOJIBAKE:
            hits = sorted(set(MOJIBAKE.findall(text)))
            if hits:
                d2.append(f"`{rel}` — {len(hits)} distinct sequence(s), e.g. {hits[:3]}")

        if rel not in SKIP_LINKS:
            for m in LINK.finditer(text):
                target = m.group(1).split("#")[0].strip()
                if not target or target.startswith(("http://", "https://", "mailto:")):
                    continue
                if not (ROOT / f.parent / target).exists():
                    d3.append(f"`{rel}` → `{target}`")

            heads = {slug(m.group(1)) for m in HEADING.finditer(text)}
            for m in ANCHOR.finditer(text):
                if m.group(1) not in heads:
                    d4.append(f"`{rel}` → `#{m.group(1)}`")

    # D5 — a live document naming a path this repo moved. History keeps its own
    # period-correct references on purpose.
    for f in tracked("*.md", "*.py", "*.json", "*.yaml", "*.sh"):
        rel = str(f).replace("\\", "/")
        if rel.startswith("docs/history/"):
            continue
        text = read(f)
        if text is None:
            continue
        for old, new in GONE.items():
            if old in text:
                d5.append(f"`{rel}` names `{old}` — moved to `{new}`")

    design = read(pathlib.Path("docs/DESIGN.md"))
    if design:
        ids = DESIGN_ID.findall(design)
        for i in sorted({i for i in ids if ids.count(i) > 1}):
            d6.append(f"`DESIGN.md` §{i} is used {ids.count(i)} times")

    print("# audit_doc_health — is the documentation structurally sound?\n")
    print(f"Documents scanned: **{len(docs)}**\n")
    print("`audit_doc_claims.py` checks whether the NUMBERS are still true. "
          "This checks whether the documents themselves are intact.\n")
    print("| code | what | count |")
    print("|---|---|--:|")
    for code, what, rows in (
        ("D1", "literal control characters", d1),
        ("D2", "mojibake (UTF-8 read as cp1252)", d2),
        ("D3", "markdown link to a missing file", d3),
        ("D4", "same-file anchor with no heading", d4),
        ("D5", "reference to a moved/removed document", d5),
        ("D6", "duplicate section id in DESIGN.md", d6),
    ):
        print(f"| {code} | {what} | {len(rows)} |")

    findings = 0
    for code, what, rows in (
        ("D1", "Control characters", d1),
        ("D2", "Mojibake", d2),
        ("D3", "Broken links", d3),
        ("D4", "Broken anchors", d4),
        ("D5", "Stale document references", d5),
        ("D6", "Duplicate DESIGN section ids", d6),
    ):
        print(f"\n\n## {code} — {what} ({len(rows)})\n")
        if rows:
            findings += len(rows)
            for r in sorted(set(rows)):
                print(f"- {r}")
        else:
            print("_clean_")

    print()
    if findings:
        print(f"\n**FAIL — {findings} finding(s).** Fix the document; none of these are "
              "cosmetic. D1/D2 are corruption, D6 makes a cited law ambiguous, and "
              "D3–D5 send a reader to the wrong place.")
        return 1
    print("\n**PASS** — no structural defects.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
