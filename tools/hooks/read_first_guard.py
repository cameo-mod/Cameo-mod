#!/usr/bin/env python3
"""PreToolUse hook — the DOCS MAXING AUDIT's enforcement half.

TWO GATES, in one file because they read the same evidence:
  TIER 1 — no tool ACTION AT ALL until the seven reading-order documents have been
           opened this session (maintainer order, 2026-08-30). Reads and `git
           status`/`log`/`diff` are exempt; see READ_ONLY_BASH.
  TIER 2 — no EDIT under docs/ or tools/ until the document that OWNS the subject
           has been opened, matched on the edit's own vocabulary.
The tiers themselves live in `tools/audit/audit_docs_maxing.py`, which REPORTS
coverage; this file only enforces. Originally: refuse to edit until the required
docs were READ.

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

# ⭐ ONE SOURCE OF TRUTH. The tiers live in `tools/audit/audit_docs_maxing.py` — the
# DOCS MAXING AUDIT — which reports coverage; this file ENFORCES it. Two copies of a
# reading order is exactly the drift this project keeps paying for, so the tables are
# imported, never restated. Fails OPEN if the import fails: a guard that cannot load
# its own contract must not block the session.
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "audit"))
try:
    from audit_docs_maxing import TIER1 as _T1, TIER2 as _T2
except Exception:      # pragma: no cover - fail open
    _T1, _T2 = (), {}

# `docs/README.md` marks these "read in this order"; CLAUDE.md repeats it. CLAUDE.md
# itself is excluded — the harness injects it, so requiring it to be "opened" is a
# check that could never fail honestly.
ALWAYS = _T1
TOPICAL = _T2

# ⛔ THE TIER-1 GATE (maintainer order, 2026-08-30 — "make it illegal for any AI agent
# to perform any actions before loading the entire documentation"). Below this line the
# guard stops being about edits: until every ALWAYS document has been OPENED, no tool
# call is permitted at all.
#
# Two exemptions, and they are not softenings — without them the gate is unsatisfiable:
#   * READING. You cannot open a document without a tool. Read/Glob/Grep pass, and so
#     do the Bash commands that are how reading is actually done here.
#   * ORIENTATION. `git status` / `log` / `diff` / `show` / `branch` tell you where you
#     are. An agent denied those cannot even report why it is stuck.
# Everything else — every edit, every write, every command that changes state or runs
# a tool — is denied with the exact commands that unblock it.
READ_ONLY_TOOLS = frozenset({
    "Read", "Glob", "Grep", "NotebookRead", "TodoWrite", "TaskCreate", "TaskUpdate",
    "ListMcpResourcesTool", "ReadMcpResourceTool",
})
READ_ONLY_BASH = (
    "cat ", "sed ", "head ", "tail ", "less ", "grep ", "rg ", "wc ", "ls ",
    "find ", "awk ", "cut ", "sort ", "uniq ", "diff ", "file ", "stat ", "tree ",
    "git status", "git log", "git diff", "git show", "git branch", "git remote",
    "python tools/audit/audit_docs_maxing.py",
)


def is_read_only(data):
    """True when this call cannot change anything — so the gate must let it through."""
    tool = data.get("tool_name")
    if tool in READ_ONLY_TOOLS:
        return True
    if tool != "Bash":
        return False
    cmd = str((data.get("tool_input") or {}).get("command") or "").strip()
    if not cmd:
        return False
    # EVERY segment must be read-only: `cat x && rm -rf y` is not a read.
    parts = [p.strip() for p in cmd.replace("&&", "\n").replace(";", "\n")
             .replace("|", "\n").splitlines() if p.strip()]
    # ⚠ The trailing space is deliberate: matching bare "ls" against the prefix "ls"
    # would also pass "lsof" and "ls-and-then-something". Append one so a bare
    # command still matches its own prefix and nothing longer sneaks in.
    return bool(parts) and all((p + " ").startswith(READ_ONLY_BASH) for p in parts)


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
    tp0 = data.get("transcript_path")
    if tp0 and not is_read_only(data):
        opened0 = opened_paths(pathlib.Path(tp0))
        if opened0 is not None:
            unread = [d for d in ALWAYS if not was_opened(d, opened0)]
            if unread:
                deny(
                    "DOCS MAXING AUDIT — TIER 1 NOT SATISFIED.\n\n"
                    "This session has not opened:\n"
                    + "\n".join(f"  - {d}" for d in unread) +
                    "\n\nNo action is permitted until it has. Reading is exempt "
                    "(you cannot open a document without a tool) and so are "
                    "`git status` / `log` / `diff`. Read them, then retry:\n"
                    + "\n".join(f"  sed -n '1,400p' {d}" for d in unread) +
                    "\n\nFull manifest and coverage:\n"
                    "  python tools/audit/audit_docs_maxing.py --transcript "
                    + str(tp0))

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
