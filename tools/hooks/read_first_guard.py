#!/usr/bin/env python3
"""PreToolUse(Write|Edit) hook — refuse to edit until the required docs were READ.

PRIOR ART: `session_checklist.py` (SessionStart) PRINTS the reading order and
`prior_art_guard.py` (Write) blocks duplicate TOOLS. Neither verifies that a
document was actually opened, which is the gap this closes — it checks the
session transcript for evidence, and blocks the edit if the evidence is absent.
`bash_guard.py` guards commits, not edits.

⛔ WHY. `docs/README.md` defines a reading order, `CLAUDE.md` repeats it, and the
SessionStart hook injects it into context every single session. On 2026-08-30 all
three were in front of me and I edited anyway without opening
`docs/AGENT_WORKSPACE.md`. Reading it afterwards found two rules I had already
broken — git rule 1 ("always fetch, pull and merge before any commit"), which is
exactly why the branch drifted 16 commits behind master and came one merge away
from reverting another contributor's work; and rule 3 (update the docs before
committing). The same session missed `docs/balance/anchor_decisions_log.md`
entirely and re-derived a defense formula that had been ruled in full on
2026-07-26, complete with anchors and numbers.

⭐ Instructions I can read and skip are what failed, three times, in one session.
So this does not instruct. It looks at what the session actually did.

HOW. Hooks receive `transcript_path`. Before a Write/Edit under `docs/` or
`tools/`, the transcript is scanned for a tool call that opened each required
document. Missing any -> DENY, naming exactly which and how to read them.

⚠ HONEST LIMIT: this verifies a document was OPENED this session, not that it was
read attentively or in full. It cannot make anyone understand a file. What it can
do is make "I skipped it" impossible to do silently, which is the failure it was
built for.

Fails OPEN — no transcript, unreadable transcript, unparseable payload — because a
guard that blocks blindly gets disabled, and a disabled guard protects nothing.
"""
import json
import pathlib
import sys

# docs/README.md is the canonical reading-order definition; these are the entries
# it marks "read in this order". CLAUDE.md is excluded — the harness injects it.
ALWAYS = (
    "docs/README.md",
    "docs/LESSONS_LEARNED.md",
    "docs/AGENT_WORKSPACE.md",
    "docs/HANDOFF.md",
    "docs/DESIGN.md",
)
# Topic-conditional. README line 129: class_anchors.json is "maintained via"
# the decisions log, which makes the log the source of truth for every baseline.
# ⚠ A GUARD WRITTEN FROM ONE INCIDENT COVERS ONE INCIDENT. This map held a SINGLE entry — the
# anchor log — because that was the failure that prompted it. On 2026-08-30 the same class of
# mistake happened again in a topic the map did not name: a full armor-tilt investigation ran
# without `WEAPON_HEAVINESS.md`, whose §9.4 ALREADY RULED the 2x-8x band with a 4x target and
# already recorded 37 of 42 families in it. Hours went into re-deriving a law, and a measurement
# was reported as a defect when the law it supposedly violated was being met exactly. In the same
# pass two external reviews asserted `Jumpjet = Plate x Scout`; `ARMOR_LAYERS.md` line 1714 says
# `jumpjet = fighter x scout`, and nothing required that file to be open either.
#
# So the entries below are not a list of nice-to-reads. Each one is the document that would have
# prevented a specific, dated failure, and the trigger words are the vocabulary that failure used.
TOPICAL = {
    "docs/balance/anchor_decisions_log.md": (
        "anchor", "class_anchors", "baseline", "cost0", "signed_off", "verifier",
        "fit_class", "formula", "dps0", "hp0",
    ),
    # §9.4 is the spread law: 2x-8x, target 4x. Anyone measuring "tilt" or "spread" is measuring
    # against a band that already exists, and needs to know which of the two metrics they hold.
    "docs/design/WEAPON_HEAVINESS.md": (
        "tilt", "spread", "heaviness", "macro contrast", "spread band", "2x-8x",
        "mean-100", "mean_100", "bell",
    ),
    # The armor vocabulary and the DERIVED/hybrid armors. `Heroic = Plate x Scout / PEAK`,
    # `Jumpjet = Fighter x Scout`. Getting a derived row wrong propagates into every profile.
    "docs/design/ARMOR_LAYERS.md": (
        "armor", "versus", "heroic", "jumpjet", "airborne", "plating", "armor ladder",
        "armor type", "shield",
    ),
    # §0a: weapon STRUCTURE before pricing. Any weapon/warhead work is downstream of it.
    "docs/design/BALANCE_PROGRAM_PLAN.md": (
        "w24", "w23", "w27", "order of operations", "structure_debt", "multi-main",
        "three_way", "3-way split", "warhead family",
    ),
}
GUARDED_ROOTS = ("docs", "tools")
MAX_TRANSCRIPT_BYTES = 64 * 1024 * 1024


def deny(reason):
    print(json.dumps({"hookSpecificOutput": {
        "hookEventName": "PreToolUse",
        "permissionDecision": "deny",
        "permissionDecisionReason": reason}}))
    sys.exit(0)


def opened_paths(transcript):
    """Every file path this session actually asked a tool to open.

    Reads the tool INPUTS, not the prose: a path mentioned in conversation is not
    a path that was read. Bash commands count — most reading here is `sed -n`,
    `head`, `grep` — so their whole command string is searched.
    """
    seen = set()
    try:
        if transcript.stat().st_size > MAX_TRANSCRIPT_BYTES:
            return None
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
            src = block.get("input") or {}
            for key in ("file_path", "path", "notebook_path", "command", "pattern"):
                val = src.get(key)
                if isinstance(val, str):
                    seen.add(val.replace("\\", "/"))
    return seen


def was_opened(path, opened):
    return any(path in s for s in opened)


def main():
    try:
        data = json.load(sys.stdin)
    except Exception:
        return
    if data.get("tool_name") not in ("Write", "Edit", "NotebookEdit"):
        return
    inp = data.get("tool_input") or {}
    raw = (inp.get("file_path") or inp.get("notebook_path") or "").replace("\\", "/")
    if not raw:
        return

    project = pathlib.Path(data.get("cwd") or ".").resolve()
    # ⚠ Resolve a RELATIVE path against the PROJECT, not this process's cwd. The
    # hook can be launched from anywhere, and resolving against its own cwd turned
    # `mods/cameo/rules/x.yaml` into `tools/tests/mods/...` — which starts with
    # `tools`, so a yaml edit got guarded as a tooling edit. Caught by its own test.
    target = pathlib.Path(raw)
    target = target if target.is_absolute() else (project / target)
    try:
        rel = target.resolve().relative_to(project).as_posix()
    except ValueError:
        rel = raw
    if not rel.startswith(GUARDED_ROOTS):
        return

    tp = data.get("transcript_path")
    if not tp:
        return                                  # nothing to check against -> allow
    opened = opened_paths(pathlib.Path(tp))
    if opened is None:
        return                                  # unreadable -> fail OPEN, never blind

    required = list(ALWAYS)
    haystack = (rel + " " + str(inp.get("content") or "")
                + " " + str(inp.get("new_string") or "")).lower()
    for doc, triggers in TOPICAL.items():
        if any(t in haystack for t in triggers):
            required.append(doc)

    missing = [d for d in required if not was_opened(d, opened)]
    if not missing:
        return

    listing = "\n".join(f"  - {d}" for d in missing)
    deny(
        f"READ-FIRST — you are editing `{rel}` but this session has not opened:\n"
        + listing +
        "\n\n`docs/README.md` is the sole definition of the reading order and "
        "`CLAUDE.md` repeats it. Both were in context on 2026-08-30 and an edit "
        "went ahead anyway without `docs/AGENT_WORKSPACE.md`; reading it afterwards "
        "surfaced two already-broken rules, one of which (git rule 1 — always fetch "
        "and merge before committing) is why that branch drifted 16 commits behind "
        "master and came one merge from reverting a contributor's work.\n\n"
        "Read them, then retry:\n"
        + "\n".join(f"  sed -n '1,400p' {d}" for d in missing) +
        "\n\nThis checks that the file was OPENED, not that it was understood. "
        "Opening it is the part that keeps being skipped.")


if __name__ == "__main__":
    main()
