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
10. Commit messages end with: Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>

Work queue + effort estimate: docs/design/ROADMAP.md + docs/design/BALANCE_PIPELINE_ESTIMATE.md.
"""

print(json.dumps({"hookSpecificOutput": {
    "hookEventName": "SessionStart",
    "additionalContext": CHECKLIST}}))
