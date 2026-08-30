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

    # (1b) THE CANONICAL REMOTE IS `cameo-mod/Cameo-mod`, AND ONLY THAT.
    # `Zeruel87/Cameo-mod` is the ORIGINAL upstream fork and it is ABANDONED. On 2026-08-11 an
    # agent re-added it as `upstream`, fetched it, and spent a session comparing two stray
    # commits against a repository nobody has published to since. Anything fetched from there is
    # historical, not current; anything pushed there is lost. The tileset category
    # `Zeruel87 Urban` and the `credits.txt` entry are ART CREDIT, not repository pointers --
    # this rule is about git remotes and URLs only, and must never be used to strip a credit.
    # Narrow to the verbs that actually REACH the fork. An earlier draft matched any `git`
    # verb and denied its own author's `git diff --stat` because the same shell line also
    # wrote the fork's name into a doc — a read-only command cannot contact a remote.
    _FORK_VERBS = r"remote|fetch|pull|push|clone|ls-remote|submodule|request-pull"
    # The flag skip `(?:-\S+(?:\s+\S+)?\s+)*` covers GLOBAL flags before the verb,
    # including the two-token `-c key=value` form: `git -c protocol.version=2 fetch <fork>`
    # reaches the fork just as surely as a bare `git fetch`. Only flag-SHAPED tokens are
    # skipped, so a commit message containing the word "push" is not read as the verb.
    if re.search(r"(?:^|[\n;&|(])\s*git\s+(?:-\S+(?:\s+\S+)?\s+)*(?:"
                 + _FORK_VERBS + r")\b", cmd) \
            and re.search(r"Zer" + r"uel87", cmd, re.I):
        deny("`Zeruel87/Cameo-mod` is the ABANDONED upstream fork. The canonical repository is "
             "`cameo-mod/Cameo-mod` and nothing else -- do not add it as a remote, fetch it, "
             "compare against it, or push to it. If you need history that predates this repo, "
             "read docs/history/ instead. (CLAUDE.md 'The canonical repository'.) "
             "Note: `Zeruel87 Urban` tileset categories and the credits.txt entry are ART "
             "CREDIT and must be left alone.")

    # (2) NEVER HAND-PARSE VERSUS. A bespoke line-scanner that opens a dict on `Versus:` and
    # scans following `Key: <int>` lines cannot see where the block ENDS, so the
    # `PercentageVersus:` rows that live in the SAME warhead node silently overwrite the real
    # profile. That produced a full session of internally-consistent, wrong numbers on
    # 2026-08-22 ("0 of 125 obey the MEAN-100 law"; the truth was 123 of 125). The project has
    # correct readers; use them.
    if re.search(r"""["']Versus:["']""", cmd) and re.search(r"startswith|split\(|re\.match", cmd):
        if "versus_of" not in cmd and "resolve_weapon" not in cmd:
            deny("Never hand-parse a `Versus:` block. A line-scanner cannot tell where the block "
                 "ENDS, so `PercentageVersus:` in the same warhead node silently overwrites the "
                 "profile — that is exactly how a whole session of weapon-profile numbers came "
                 "out wrong. Use the project's readers: "
                 "`miniyaml.Ruleset.resolve_weapon(name)` for the node, then "
                 "`weapon_efficiency.versus_of(warhead_node)` for the {armor: percent} dict. "
                 "See CLAUDE.md rule 8e and tools/audit/audit_versus_profile.py.")

    # (3) boot-gate before committing engine-loaded content.
    #     Exempt: a MERGE that only carries engine files through unchanged from one
    #     of its parents. That content was gated on the branch it came from, and
    #     demanding a boot for it makes `git merge origin/master` impossible for
    #     anyone whose branch is docs-only. A merge that RESOLVES an engine file
    #     (content differing from both parents) is still gated.
    if re.search(r"\bgit\s+commit\b", cmd):
        root = pathlib.Path(__file__).resolve().parents[2]
        try:
            staged = subprocess.run(
                ["git", "-C", str(root), "diff", "--cached", "--name-only"],
                capture_output=True, text=True, timeout=15).stdout.split()
        except Exception:
            return  # git unavailable -> don't block
        # (3a) UNAUTHORIZED SIGN-OFF. Flipping `signed_off` to true in
        #      class_anchors.json unblocks `apply_balance --confirm` for that class,
        #      and fit_class.py step 4 reserves it for the MAINTAINER. On 2026-08-29
        #      an agent signed three anchors on its own validation tables; one of
        #      them (`scout`) sat at worst |delta| 22.8 against a <=1 bar. Worse, the
        #      next session "corrected" the docs to match — citing the artifact-wins
        #      rule against an artifact the agent had written itself.
        if "docs/balance/class_anchors.json" in staged:
            try:
                diff = subprocess.run(
                    ["git", "-C", str(root), "diff", "--cached", "-U0",
                     "--", "docs/balance/class_anchors.json"],
                    capture_output=True, text=True, timeout=15).stdout
            except Exception:
                diff = ""
            added = [ln for ln in diff.splitlines()
                     if ln.startswith("+") and not ln.startswith("+++")
                     and re.search(r'"signed_off"\s*:\s*true', ln)]
            if added and "MAINTAINER-ORDERED SIGN-OFF" not in cmd:
                deny("You are staging `signed_off: true` in class_anchors.json. That "
                     "unblocks `apply_balance --confirm` for the class, and fit_class.py "
                     "step 4 reserves signing for the MAINTAINER — a fit table is "
                     "evidence FOR a decision, not the decision.\n\n"
                     "On 2026-08-29 three anchors were self-signed this way; `scout` was "
                     "among them at worst |delta| 22.8 against a <=1 bar. All reverted.\n\n"
                     "If the maintainer has actually ordered it, quote them and put the "
                     "words MAINTAINER-ORDERED SIGN-OFF in the commit message.")

        # (3b) STALE BRANCH. AGENT_WORKSPACE.md git rule 1: "Always fetch, pull, and
        #      merge before any commit. The remote may have changes from other
        #      developers." Skipping it is how a branch drifted 16 commits behind
        #      master and its regenerated ledgers came within one merge of REVERTING
        #      another contributor's weapon-consolidation work. Advisory text did not
        #      prevent that; this does. Uses only already-fetched refs — it never
        #      reaches the network, so it cannot hang or fail closed on a bad link.
        try:
            base = subprocess.run(
                ["git", "-C", str(root), "rev-parse", "--verify", "-q", "origin/master"],
                capture_output=True, text=True, timeout=15)
            behind = subprocess.run(
                ["git", "-C", str(root), "rev-list", "--count", "HEAD..origin/master"],
                capture_output=True, text=True, timeout=15).stdout.strip()
        except Exception:
            base, behind = None, ""
        if base is not None and base.returncode == 0 and behind.isdigit() and int(behind) > 0:
            deny(f"Your branch is {behind} commit(s) behind origin/master (as of the last "
                 "fetch). AGENT_WORKSPACE.md git rule 1: always fetch, pull and merge "
                 "BEFORE committing — the remote carries other contributors' work.\n\n"
                 "This is not bookkeeping. A branch that drifted 16 behind regenerated the "
                 "shared ledgers from a tree missing master's yaml, and came one merge away "
                 "from reverting another contributor's weapon consolidation.\n\n"
                 "Run: git fetch origin master && git merge origin/master\n"
                 "Then REGENERATE anything derived (extract_stats.py for the ledgers) so it "
                 "reflects the merged tree, and re-run the audits.")

        engine_prefixes = ("mods/", "OpenRA.Mods.Cameo/", "engine/")
        eng = [f for f in staged if f.startswith(engine_prefixes)]

        # A MERGE stages every file the other side brought in, so merging an
        # already-gated upstream branch would demand a boot for content this
        # commit did not author. Keep only the engine files whose merged content
        # differs from BOTH parents — those are the ones this commit resolved,
        # and they are the only ones a boot could say anything about.
        if eng and (root / ".git" / "MERGE_HEAD").exists():
            def blob(rev, path):
                r = subprocess.run(["git", "-C", str(root), "show", f"{rev}:{path}"],
                                   capture_output=True, timeout=15)
                return r.stdout if r.returncode == 0 else None

            def staged_blob(path):
                r = subprocess.run(["git", "-C", str(root), "show", f":0:{path}"],
                                   capture_output=True, timeout=15)
                return r.stdout if r.returncode == 0 else None

            try:
                parents = ["HEAD", (root / ".git" / "MERGE_HEAD").read_text().split()[0]]
                authored = []
                for f in eng:
                    mine = staged_blob(f)
                    if mine is not None and any(blob(par, f) == mine for par in parents):
                        continue  # identical to a parent -> inherited, not authored here
                    authored.append(f)
                eng = authored
            except Exception:
                pass  # can't tell -> fall through and demand the gate

        if not eng:
            return  # docs/tools-only commit (or a pure merge) — boot not required
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
