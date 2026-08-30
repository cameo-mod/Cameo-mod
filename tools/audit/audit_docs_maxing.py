#!/usr/bin/env python3
"""audit_docs_maxing.py — THE DOCS MAXING AUDIT.

Ordered by the maintainer, 2026-08-30:

    "Always load all the documents into your context and memory and make sure it
     will always do that every start, so it should be like a hook. Call it the
     docs maxing audit. Make it illegal for any AI agent to perform any actions
     before loading the entire documentation into the context."

⛔ ONE THING HAS TO BE SAID PLAINLY, BECAUSE A GUARD BUILT ON A FALSE PREMISE IS
WORSE THAN NONE. The authored documentation set is **117 files, ~92,700 lines,
7.6 MB** — on the order of 1.9 MILLION tokens. No model context holds that, so a
hook that demanded it literally would deny every action in every session forever
and be switched off within the hour. What CAN be enforced, and is enforced here,
is the strongest true version of the order:

  TIER 1  THE GATE — the seven documents `docs/README.md` defines as the reading
          order. **No tool action of any kind is allowed until every one of them
          has been opened this session.** Reading is exempt (you cannot open a
          document without a tool), and so is `git status`/`log`/`diff`, which is
          how you find out where you are. Everything else is denied.
  TIER 2  THE TOPIC — the document that owns the subject you are about to touch.
          Blocks an EDIT in that subject, by the vocabulary the edit itself uses.
          Enforced by `tools/hooks/read_first_guard.py`, which imports this
          module's tables so the two cannot drift.
  TIER 3  THE MANIFEST — every remaining authored document, listed with its size
          and its one-line subject, printed at SessionStart. You are expected to
          know THAT it exists and what it owns; you are expected to OPEN it before
          working in its area. This audit reports how much of it a session read.

⭐ WHY A TIER IS NOT A COMPROMISE. The failures this exists for were never "I read
92,000 lines but missed a nuance". Every one was "I never opened the file at all":
a defense formula re-derived when `anchor_decisions_log.md` had ruled it in full;
a spread band re-measured when §9.4 had already ruled 2x-8x with a 4x target; two
reviews asserting `Jumpjet = Plate x Scout` when `ARMOR_LAYERS.md` line 1714 says
`fighter x scout`. Opening the right file is the whole of the failure, and it is
what a hook can actually check.

Usage:
    python tools/audit/audit_docs_maxing.py              # the manifest + tiers
    python tools/audit/audit_docs_maxing.py --transcript <path>   # session coverage
    python tools/audit/audit_docs_maxing.py --json      # machine-readable manifest
"""
from __future__ import annotations

import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]

# --------------------------------------------------------------------------- #
# TIER 1 — the gate. `docs/README.md` is the sole definition of this order and
# CLAUDE.md repeats it; if the two ever disagree, README wins and the copy gets
# fixed. CLAUDE.md itself is NOT listed: the harness injects it every session, so
# requiring it to be "opened" would be a check that can never fail honestly.
# --------------------------------------------------------------------------- #
TIER1 = (
    "docs/README.md",
    "docs/LESSONS_LEARNED.md",
    "docs/AGENT_WORKSPACE.md",
    "docs/HANDOFF.md",
    "docs/DESIGN.md",
    "docs/design/ROADMAP.md",
    "docs/audit/SUMMARY.md",
)

# --------------------------------------------------------------------------- #
# TIER 2 — topic -> the document that OWNS it, and the vocabulary that means you
# are in that topic. Each entry is a document that would have prevented a
# specific, dated failure; the triggers are the words that failure used.
# `read_first_guard.py` imports this.
# --------------------------------------------------------------------------- #
TIER2 = {
    "docs/balance/anchor_decisions_log.md": (
        "anchor", "class_anchors", "baseline", "cost0", "signed_off", "verifier",
        "fit_class", "formula", "dps0", "hp0",
    ),
    "docs/design/WEAPON_HEAVINESS.md": (
        "tilt", "spread", "heaviness", "macro contrast", "spread band", "2x-8x",
        "mean-100", "mean_100", "bell",
    ),
    "docs/design/ARMOR_LAYERS.md": (
        "armor", "versus", "heroic", "jumpjet", "airborne", "plating", "armor ladder",
        "armor type", "shield",
    ),
    "docs/design/BALANCE_PROGRAM_PLAN.md": (
        "w24", "w23", "w27", "order of operations", "structure_debt", "multi-main",
        "three_way", "3-way split", "warhead family",
    ),
    "docs/design/BALANCE_PIPELINE.md": (
        "apply_balance", "extract_stats", "ledger", "workbook", "balance drift",
        "--confirm",
    ),
    "docs/design/WEAPON_3WAY_SPLIT.md": (
        "compatibility", "projectile template", "effect template", "resolve_diff",
        "frankenstein",
    ),
    "docs/MIGRATION.md": (
        "contentpack", "content pack", "faction split", "safe_rename", "rename_map",
    ),
}

# Directories whose .md files are GENERATED or ARCHIVED. They are never required
# reading: regenerate the first, and the second is what happened, not what is true.
EXCLUDED_DIRS = (
    "docs/history/",
    "docs/audit/latest/",
    "docs/audit/degraded/",
    "docs/audit/baseline/",
)


def authored_docs():
    """Every authored `.md` under docs/, newest-relevant order irrelevant — sorted."""
    out = []
    for p in sorted(ROOT.joinpath("docs").rglob("*.md")):
        rel = p.relative_to(ROOT).as_posix()
        if rel.startswith(EXCLUDED_DIRS):
            continue
        out.append(rel)
    return out


def subject(rel):
    """The document's first heading — its own one-line claim about what it owns."""
    try:
        for line in ROOT.joinpath(rel).read_text(encoding="utf-8",
                                                 errors="replace").splitlines():
            if line.startswith("#"):
                return line.lstrip("# ").strip()
    except OSError:
        pass
    return ""


def sizes(rel):
    try:
        text = ROOT.joinpath(rel).read_text(encoding="utf-8", errors="replace")
        return text.count("\n") + 1, len(text)
    except OSError:
        return 0, 0


def opened_paths(transcript: pathlib.Path):
    """Paths this session actually asked a tool to open (inputs, never prose)."""
    seen = set()
    try:
        lines = transcript.read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError:
        return None
    for line in lines:
        try:
            rec = json.loads(line)
        except ValueError:
            continue
        content = ((rec.get("message") or {}).get("content")) or []
        if not isinstance(content, list):
            continue
        for block in content:
            if not isinstance(block, dict) or block.get("type") != "tool_use":
                continue
            for key in ("file_path", "path", "notebook_path", "command", "pattern"):
                val = (block.get("input") or {}).get(key)
                if isinstance(val, str):
                    seen.add(val.replace("\\", "/"))
    return seen


def was_opened(rel, opened):
    return any(rel in s for s in opened)


def main():
    argv = sys.argv[1:]
    docs = authored_docs()
    if "--json" in argv:
        print(json.dumps({"tier1": list(TIER1), "tier2": {k: list(v) for k, v in TIER2.items()},
                          "manifest": docs}, indent=2))
        return 0

    total_lines = sum(sizes(d)[0] for d in docs)
    total_bytes = sum(sizes(d)[1] for d in docs)
    print("# docs maxing audit\n")
    print(f"authored documentation: **{len(docs)} files, {total_lines:,} lines, "
          f"{total_bytes / 1e6:.1f} MB** (~{total_bytes // 4:,} tokens)\n")
    print("⛔ That does not fit a context window, and a gate that demanded it would deny "
          "every action forever. The gate is TIER 1; the rest is enforced by topic and "
          "reported here.\n")

    print("## TIER 1 — the gate (no tool action until every one is opened)\n")
    for d in TIER1:
        ln, _ = sizes(d)
        print(f"  {ln:>6,} lines  {d}")
    print(f"\n  total {sum(sizes(d)[0] for d in TIER1):,} lines\n")

    print("## TIER 2 — the topic owners (block an edit in their subject)\n")
    for d, trig in TIER2.items():
        ln, _ = sizes(d)
        print(f"  {ln:>6,} lines  {d}\n{'':>14}triggers: {', '.join(trig)}")
    print()

    rest = [d for d in docs if d not in TIER1 and d not in TIER2]
    print(f"## TIER 3 — the manifest ({len(rest)} further authored documents)\n")
    for d in rest:
        ln, _ = sizes(d)
        print(f"  {ln:>6,} lines  {d:<52s} {subject(d)[:70]}")
    print()

    if "--transcript" in argv:
        tp = pathlib.Path(argv[argv.index("--transcript") + 1])
        opened = opened_paths(tp)
        if opened is None:
            print("transcript unreadable — no coverage to report")
            return 0
        for label, group in (("TIER 1", TIER1), ("TIER 2", tuple(TIER2)),
                             ("TIER 3", tuple(rest))):
            hit = [d for d in group if was_opened(d, opened)]
            pct = 100 * len(hit) / len(group) if group else 100
            print(f"{label:7s} opened {len(hit):>3}/{len(group):<3} ({pct:5.1f}%)")
        missing = [d for d in TIER1 if not was_opened(d, opened)]
        if missing:
            print("\nTIER 1 NOT SATISFIED — every tool action is denied until these are opened:")
            for d in missing:
                print(f"  sed -n '1,400p' {d}")
            return 1
        print("\nTIER 1 satisfied.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
