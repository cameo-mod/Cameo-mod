#!/usr/bin/env python3
"""PreToolUse Bash guard for Cameo-mod. Reads the hook JSON on stdin and enforces
two hard rules deterministically (so they don't depend on the model remembering):

  1. SCOPED ADDS ONLY — block `git add -A` / `--all` / `.` (the maintainer + Devin
     have live uncommitted WIP; a wide add captures or clobbers it).
  2. BOOT-GATE BEFORE COMMITTING ENGINE CONTENT — block `git commit` when a staged
     file under mods/ , OpenRA.Mods.Cameo/ , or engine/ is newer than the last
     successful boot (perf.log ending in MenuPostProcessEffect.PostWorldLoaded).
     Docs/tools-only commits are exempt (no engine content parsed at boot).

Emits a PreToolUse permissionDecision. No output = allow.
"""
import sys
import os
import re
import json
import subprocess
import pathlib


def deny(reason):
    print(json.dumps({"hookSpecificOutput": {
        "hookEventName": "PreToolUse",
        "permissionDecision": "deny",
        "permissionDecisionReason": reason}}))
    sys.exit(0)


def main():
    try:
        data = json.load(sys.stdin)
    except Exception:
        return  # can't parse -> allow
    cmd = (data.get("tool_input") or {}).get("command", "") or ""

    # (1) scoped adds only. Anchor at a COMMAND POSITION (start of string, or after a
    # shell separator / newline) so the rule doesn't false-positive when a commit
    # message or other prose merely MENTIONS `git add -A` (e.g. inside backticks).
    if re.search(r"(?:^|[\n;&|(])\s*git\s+add\s+(?:-A\b|--all\b|\.(?:\s|$))", cmd):
        deny("Scoped adds only — never `git add -A/--all/.` (the maintainer and Devin "
             "have live uncommitted WIP that a wide add would capture or clobber). "
             "Stage explicit paths instead: `git add <file> [<file> ...]`.")

    # (2) boot-gate before committing engine-loaded content
    if re.search(r"\bgit\s+commit\b", cmd):
        root = pathlib.Path(__file__).resolve().parents[2]
        try:
            staged = subprocess.run(
                ["git", "-C", str(root), "diff", "--cached", "--name-only"],
                capture_output=True, text=True, timeout=15).stdout.split()
        except Exception:
            return  # git unavailable -> don't block
        engine_prefixes = ("mods/", "OpenRA.Mods.Cameo/", "engine/")
        eng = [f for f in staged if f.startswith(engine_prefixes)]
        if not eng:
            return  # docs/tools-only commit — boot not required
        newest = 0.0
        for f in eng:
            p = root / f
            if p.exists():
                newest = max(newest, p.stat().st_mtime)
        perf = pathlib.Path(os.environ.get("APPDATA", "")) / "OpenRA" / "Logs" / "perf.log"
        if not perf.exists():
            deny("Boot-gate required: you are committing engine content (" + eng[0]
                 + (" +%d more" % (len(eng) - 1) if len(eng) > 1 else "")
                 + ") but no perf.log was found. Run launch-game.cmd to the main menu "
                 "(perf.log must end with MenuPostProcessEffect.PostWorldLoaded), confirm "
                 "no new exception-*.log, then commit.")
        try:
            tail = perf.read_text(errors="ignore").splitlines()[-40:]
        except Exception:
            tail = []
        booted = any("MenuPostProcessEffect.PostWorldLoaded" in ln for ln in tail)
        fresh = perf.stat().st_mtime >= newest - 1  # 1s slack
        if not (booted and fresh):
            why = []
            if not booted:
                why.append("perf.log's last lines don't show MenuPostProcessEffect.PostWorldLoaded")
            if not fresh:
                why.append("perf.log is older than your staged engine changes (the boot is stale)")
            deny("Boot-gate required before committing engine content — " + "; ".join(why)
                 + ". Run launch-game.cmd to the menu, confirm no new exception-*.log, then "
                 "commit. (Docs/tools-only commits are exempt from this check.)")


if __name__ == "__main__":
    main()
