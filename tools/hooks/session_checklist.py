#!/usr/bin/env python3
"""SessionStart hook: point every Claude session to the shared Cameo workflow."""
import json

CHECKLIST = """\
CAMEO: read AGENTS.md, then docs/AGENT_WORKSPACE.md for coordination and activation status.
Use docs/TASK_INDEX.md to find the task's exact technical reading route and existing tools.
docs/README.md maps documentation; docs/HANDOFF.md provides project context, not a fresh claim.

Human task instructions remain controlling. The pilot is preparatory until the team activates
it explicitly. Preserve existing reservations until their human owners reconcile them.
Use the documented manual claim and recovery process. No Project field supplies an atomic lock.
Every independent writer needs an isolated checkout. Inspect actual branch/diff state on restart.

Use AGENT_WORKSPACE's scoped validation and engine procedure, including preflight before make all.
Do not run --check-yaml or make test. Launch the game only when authorized.
Use local validation and human review; disabled CI is not a pilot activation prerequisite.
Never bypass OS security settings.
Stage named files, preserve others' work, and distinguish static, build/boot and in-game evidence.
Aedis's designated coordinator agent approves claims and work reviews; Blackrobe is quota fallback.
Record takeover and handback per AGENT_WORKSPACE; agents execute only authorized steps and preserve HOLDs.
"""

print(json.dumps({"hookSpecificOutput": {
    "hookEventName": "SessionStart",
    "additionalContext": CHECKLIST}}))
