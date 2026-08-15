#!/usr/bin/env python3
"""SessionStart hook — inject the Cameo must-read + hard-rules checklist into
context every session, so 'read the docs first' and the hard rules don't depend
on the model choosing to read CLAUDE.md. Short and punchy on purpose."""
import json

CHECKLIST = """\
CAMEO — orient before acting this session (verify against the artifacts, don't trust summaries):

MUST-READ, in order: CLAUDE.md · docs/LESSONS_LEARNED.md · docs/AGENT_WORKSPACE.md ·
docs/design/ROADMAP.md · docs/design/BALANCE_PIPELINE_ESTIMATE.md.
For weapon work also: docs/AI_HANDOFF_2026-08-05.md · docs/design/AREADAMAGE_HANDOFF.md ·
docs/design/WEAPON_3WAY_SPLIT.md.

HARD RULES (several are enforced by hooks — see .claude/settings.json):
 1. Never commit without booting to the main menu (perf.log ends
    MenuPostProcessEffect.PostWorldLoaded; no new %APPDATA%/OpenRA/Logs/exception-*.log). [hook-enforced]
 2. Scoped `git add <files>` only — never -A/./--all (maintainer + Devin have live WIP). [hook-enforced]
 3. Don't trust, verify — grep the data / ls the file (incl. ~/Downloads) / run the tool /
    boot-gate before asserting done/pending/blocked/missing. When a summary and the artifact
    disagree, the artifact wins — then fix the stale summary.
 4. Never hand-edit balance numbers — use the pipeline (extract_stats -> ledger ->
    apply_balance --confirm; --confirm needs maintainer order).
 5. Versus lives ONLY in ^Warhead_* templates; never change a warhead / Burst / BurstDelays
    without explicit permission.
 6. Weapon 3-way split: preserve resolved behaviour (Damage verbatim, projectile fields),
    find_empty_warhead.py = 0, boot-gate per batch. Verify with tools/audit/review_resolve_diff.py.
 7. Multi-agent tree (maintainer / Devin / you): one owner per file-set; re-verify others'
    commits before building on them (check mtimes for a live agent first).
 8. Audit reports regen via `bash tools/audit/run_all.sh` ONLY (PowerShell > writes UTF-16).
 9. Underscore-only naming — no hyphens in ids/files/fluent keys.
10. Commit trailer = the ACTUAL author, with your REAL model name:
    Co-Authored-By: Claude <your-model> <noreply@anthropic.com>  (a template, not a
    literal - do not copy a version from a previous commit). Other agents sign as
    themselves, e.g. Co-Authored-By: Devin AI <devin@cognition.ai>.

ENGINE CHANGES — `engine/` IS NOT PART OF THIS REPO. It is .gitignored, has no .git and no
.gitmodules, and `git ls-files engine` returns ZERO files; `git` run from inside it silently
operates on the PARENT repo. Editing engine/**  produces work that CANNOT be committed here
and is DELETED by the next `make all`. It is a build output, not source.
Full procedure: docs/LESSONS_LEARNED.md "The canonical engine update pipeline". In short:
 1. Edit C# in the SEPARATE clone of https://github.com/cameo-mod/OpenRA, branch cameo-engine.
 2. Commit + push to origin/cameo-engine (check `git status` for stray nested-clone entries).
 3. `git rev-parse cameo-engine` for the FULL 40-char hash — never hand-type or truncate it.
 4. Set ENGINE_VERSION="<hash>" in **mod.config** (NOT mod.yaml) in this repo.
 5. `make.cmd all` — VERSION mismatch makes the SDK delete engine/, refetch and rebuild.
 6. Verify engine/VERSION == the hash, build has 0 errors, and recreate any engine/glsl/
    shaders (the fetch WIPES them, e.g. postprocess_nuclearflash.frag).
 7. Boot-gate, then commit mod.config with the docs updates.
Before choosing that route, check whether the mod assembly can just SHADOW the engine trait:
ObjectCreator.FindType takes the FIRST assembly in mod.yaml's Assemblies list holding the
name, and the order is AS, CA, Cameo, Cnc, D2k, Common — so an OpenRA.Mods.Cameo type of the
same name wins with ZERO yaml changes (precedent: ColorPickerColorShift, PlayerColorShift,
SelectionDecorations). PROVE a shadow works by giving the Cameo Info a field the engine one
lacks and booting with that field set — `--docs` lists BOTH types and proves nothing.

Work queue + effort estimate: docs/design/ROADMAP.md + docs/design/BALANCE_PIPELINE_ESTIMATE.md.
"""

print(json.dumps({"hookSpecificOutput": {
    "hookEventName": "SessionStart",
    "additionalContext": CHECKLIST}}))
