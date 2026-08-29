#!/usr/bin/env python3
"""PreToolUse(Write) hook — refuse to CREATE a tool that already exists.

⛔ WHY THIS EXISTS. On 2026-08-30 I wrote `tools/audit/audit_turn_rate.py` to check
the vehicle/aircraft TurnSpeed law. `tools/audit/audit_stat_formulas.py` had been
enforcing that exact law for months — F8 vehicles, F9 turreted, F10 turretless,
F17 fighters/bombers Speed/15, F19 helicopters/spaceships Speed/5 — it was already
in `run_all.sh`, `gen_derived_stats.py` already FIXED violations from its output,
and all five checks read **0 findings**. My duplicate scoped by "has a Mobile or
Aircraft trait" instead of by unit type plus template inheritance, so it applied
the GROUND law to aircraft in no air template and reported **340 violations against
a roster that has none**. Those false numbers reached DESIGN.md and HANDOFF.md
before anyone ran the real audit.

That was the third time in one session that work already done got redone. The
SessionStart checklist ALREADY says "grep DESIGN.md before designing anything" —
and it did not help, because the failure is not a missing instruction. It is that
I grepped the PHRASE and not the MECHANISM: "TurnSpeed (aircraft)" found one
sentence of a two-part law, while `grep -ri fighter tools/` would have found the
whole thing implemented and passing.

⭐ SO THIS GUARD IS MECHANICAL, NOT ADVISORY. Advice I can read and still skip is
what already failed. This runs the grep FOR me, on the concept tokens in the
filename I am about to create, and blocks the write until the answer is in the
file itself.

WHAT IT DOES. On a Write that would CREATE a new `.py` under `tools/`, it takes the
concept tokens of the new name, finds existing tools whose own name or module
docstring carries those tokens, and DENIES with that list. To proceed, the new
file must carry a `PRIOR ART:` line naming what was checked — either the
overlapping tool and why this one is still needed, or `PRIOR ART: none — <why>`.
The citation stays in the file, so the next reader sees the reasoning too.

It never blocks edits to existing files, and never blocks anything outside `tools/`.
"""
import json
import pathlib
import re
import sys

# Prefixes and words that say what KIND of tool it is, not what it is ABOUT.
GENERIC = {
    "audit", "gen", "test", "propose", "check", "run", "fix", "find", "apply",
    "build", "make", "tool", "tools", "review", "dump", "show", "list", "all",
    "new", "old", "data", "util", "utils", "helper", "main", "py",
}
SEARCH_ROOTS = ("tools/audit", "tools/balance", "tools/rename", "tools/packs", "tools/hooks")
DOCSTRING_CHARS = 2500
MAX_REPORT = 6


def deny(reason):
    print(json.dumps({"hookSpecificOutput": {
        "hookEventName": "PreToolUse",
        "permissionDecision": "deny",
        "permissionDecisionReason": reason}}))
    sys.exit(0)


def concept_tokens(stem):
    """The tokens that say what a tool is ABOUT, longest first."""
    parts = [p for p in re.split(r"[^a-z0-9]+", stem.lower()) if p]
    return sorted({p for p in parts if len(p) >= 4 and p not in GENERIC},
                  key=len, reverse=True)


def carries(blob, token):
    """Does this text use the token as a WORD, not as a random substring?

    ⚠ Substring matching makes this guard useless. `turn` appears inside
    `return`, `rate` inside `generate` and `separate`, so a plain `in` test
    ranked 148 unrelated tools above the one that mattered. A guard that cries
    wolf is worse than no guard: it gets skipped, which is the exact failure it
    exists to prevent. Anchoring at a word START keeps `TurnSpeed` (one word
    beginning with `turn`) and drops `return`.
    """
    return re.search(r"\b" + re.escape(token), blob) is not None


def head_text(path):
    """Filename plus module docstring — what the file says it is for."""
    try:
        text = path.read_text(encoding="utf-8", errors="replace")[:DOCSTRING_CHARS]
    except OSError:
        text = ""
    return (path.name + "\n" + text).lower()


def overlapping(root, tokens, exclude):
    """Existing tools carrying these concepts, best match first."""
    hits = []
    for path in sorted(root.rglob("*.py")):
        if path == exclude or "__pycache__" in path.parts:
            continue
        blob = head_text(path)
        matched = [t for t in tokens if carries(blob, t)]
        if len(matched) == len(tokens):
            # A token in the NAME is a much stronger signal than one in prose.
            weight = sum(2 if carries(path.name.lower(), t) else 1 for t in matched)
            hits.append((weight, len(matched), path, matched))
    hits.sort(key=lambda h: (-h[0], -h[1], str(h[2])))
    return hits


def main():
    try:
        data = json.load(sys.stdin)
    except Exception:
        return                      # unparseable -> allow, never block blindly
    if data.get("tool_name") != "Write":
        return
    raw = (data.get("tool_input") or {}).get("file_path") or ""
    if not raw:
        return

    project = pathlib.Path(data.get("cwd") or ".").resolve()
    target = pathlib.Path(raw)
    if not target.is_absolute():
        target = (project / target)
    target = target.resolve()

    try:
        rel = target.relative_to(project)
    except ValueError:
        return                      # outside the repo -> not ours
    if rel.suffix != ".py" or rel.parts[:1] != ("tools",):
        return
    if target.exists():
        return                      # editing an existing tool is not duplication

    tokens = concept_tokens(rel.stem)
    if not tokens:
        return

    content = (data.get("tool_input") or {}).get("content") or ""
    if re.search(r"^\s*#?\s*PRIOR ART:", content, re.MULTILINE | re.IGNORECASE):
        return                      # the check was done and written down

    hits = []
    for name in SEARCH_ROOTS:
        root = project / name
        if root.is_dir():
            hits += overlapping(root, tokens, target)
    hits.sort(key=lambda h: (-h[0], -h[1], str(h[2])))
    if not hits:
        return                      # genuinely new ground

    lines = [f"  - {h[2].relative_to(project)}  (matches: {', '.join(h[3])})"
             for h in hits[:MAX_REPORT]]
    more = f"\n  ...and {len(hits) - MAX_REPORT} more" if len(hits) > MAX_REPORT else ""
    deny(
        f"PRIOR ART CHECK — `{rel}` does not exist yet, and these tools already "
        f"carry its concepts ({', '.join(tokens)}):\n" + "\n".join(lines) + more +
        "\n\nREAD THEM BEFORE WRITING THIS. On 2026-08-30 `audit_turn_rate.py` was "
        "written to check a law `audit_stat_formulas.py` had already enforced for "
        "months at 0 findings; the duplicate mis-scoped the cohort and reported 340 "
        "violations that do not exist, and those false numbers reached DESIGN.md.\n\n"
        "If one of the above already does this, EXTEND IT instead. If this really is "
        "new, add a line to the new file's docstring recording what you checked:\n"
        "    PRIOR ART: tools/audit/audit_x.py checks A; this checks B, which it does not.\n"
        "or `PRIOR ART: none — <why the overlap is only in the name>`. "
        "The citation belongs in the file so the next reader sees it too.")


if __name__ == "__main__":
    main()
