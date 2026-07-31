---
trigger: always_on
description: Mandatory start protocol, boot-gate, and engine update pipeline for all tasks
---

# Mandatory start protocol (read before EVERY task)

Before starting ANY task, load these documents into context IN THIS ORDER. Never skip this, even for "small" tasks:

1. `CLAUDE.md` (repo root) — project instructions, loaded every session.
2. `docs/LESSONS_LEARNED.md` — safe defaults, pitfalls, latest incident findings.
3. `docs/AGENT_WORKSPACE.md` — source-of-truth map, operating sequence, git/commit rules.
4. `docs/PROJECT_CONTEXT.md` — project orientation.
5. `docs/DESIGN.md` — binding rules (relevant sections) before touching YAML/assets/naming/balance.
6. `docs/design/ROADMAP.md` — current work queue; P0 crashes jump the queue.
7. `docs/Cameo_Knowledge_Base_Manual.md` — engine/trait reference as needed.
8. `docs/audit/SUMMARY.md` — known issue classes.

If any document conflicts with chat memory or old notes, the repository documents win.

# Boot-gate rule (never forget)

**ALWAYS test the game with `launch-game.cmd` BEFORE committing anything.** Wait for the main menu (perf.log ends with `MenuPostProcessEffect.PostWorldLoaded`), kill the process, check for NEW `exception-*.log` in `%APPDATA%/OpenRA/Logs`. A commit without a passing boot-gate is not acceptable.

- Known blocker: Windows Smart App Control (SAC) in Enforcement mode blocks all locally built engine binaries (see `docs/LESSONS_LEARNED.md` § Smart App Control 2026-07-30). Four options exist to enable boot-gating: (1) EA cache workaround — turn SAC off, launch game once (ISG writes trust EAs), re-enable SAC; (2) SAC Evaluation mode via WinRE — SAC stays active but does not block (recommended for development); (3) VM / SAC-free machine; (4) code signing with a trusted CA. If the boot-gate cannot run for any SAC reason, say so explicitly, record the SAC state in the commit/PR description, and do NOT silently skip or claim it passed.
- `utility.cmd cameo --check-yaml` is a lint tool, NOT a boot-gate substitute.

# Engine update pipeline (uniform, binding)

Engine changes follow the pipeline in `docs/LESSONS_LEARNED.md` § "The canonical engine update pipeline". Summary:

1. Edit engine C# only in the `cameo-engine` dev clone (branch `cameo-engine`).
2. Commit + push to `origin/cameo-engine` (check `git status` for stray gitlinks first).
3. `git rev-parse cameo-engine` for the full 40-char hash — never hand-type or pad a hash.
4. Set `ENGINE_VERSION` in `mod.config` (NOT `mod.yaml`).
5. `make.cmd all` to re-fetch + rebuild; verify `engine/VERSION` matches.
6. Boot-gate with `launch-game.cmd`, then commit `mod.config` with updated docs.

# Documentation discipline

- Update ALL affected docs (`docs/LESSONS_LEARNED.md`, `docs/design/ROADMAP.md`, `docs/DESIGN.md`, `docs/audit/SUMMARY.md`) BEFORE committing.
- Write down every significant finding in the appropriate doc immediately — do not leave knowledge only in chat.
- Never use absolute local file paths in repository documents; use repo-relative paths.
