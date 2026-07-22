# Shared Agent Workspace

This repository is the shared source of truth for maintainers and every AI agent. Do not create a second roadmap, audit tree, or design contract outside this repository.

## Source-of-truth map

| Need | Canonical location | Rule |
|---|---|---|
| Lessons learned / start protocol | `docs/LESSONS_LEARNED.md` | Read first before every new task; contains required reading order and accumulated pitfalls. |
| Short project orientation | `docs/PROJECT_CONTEXT.md` | Read after LESSONS_LEARNED; referenced primary documents remain authoritative. |
| Current work queue | `docs/design/ROADMAP.md` | Crashes and player-visible bugs are P0 and always jump the queue. Add new issues here before implementation. |
| Binding rules and conventions | `docs/DESIGN.md` | Read before modifying YAML, assets, naming, weapons, balance, or descriptions. |
| Engine and custom-trait reference | `docs/Cameo_Knowledge_Base_Manual.md` | Consult before changing unfamiliar traits or C#-backed behavior. |
| Audit overview | `docs/audit/SUMMARY.md` | Read first for known issue classes and current audit status. |
| Detailed audit findings | `docs/audit/FINDINGS.md`, `docs/audit/CONSISTENCY_REPORT.md` | Update only from evidence produced by a current audit or engine boot. |
| Audit scripts | `tools/audit/` | Scripts are reusable tooling; do not duplicate them into personal-agent folders. |
| Generated audit output | `docs/audit/latest/` | Regenerate via `tools/audit/run_all.sh`; it is the current evidence set. |
| Baseline audit evidence | `docs/audit/baseline/` | Historical comparison only. |
| Faction reference | `docs/FACTIONS.md`, `docs/factions/MATRIX.md` | Use for display-name, faction-role, roster, and documentation checks. |
| Migration process | `docs/MIGRATION.md` | Use for naming, actor splits, asset movement, and Fluent migrations. |
| External-agent historical evidence | `docs/audit/LEGACY_DEVIN_CABAL.md` | Historical register only. No external output is current until rerun in this repository. |

## Required operating sequence

1. Read `docs/LESSONS_LEARNED.md` first, then `docs/DESIGN.md`, `docs/audit/SUMMARY.md`, and the relevant section of `docs/Cameo_Knowledge_Base_Manual.md` before touching rules or assets.
2. Record a newly discovered crash, regression, or suspected discrepancy in `docs/design/ROADMAP.md` before proposing a fix.
3. Treat release builds, engine logs, resolved-ruleset diffs, and current audit output as evidence. Do not promote an old raw `.txt` result to a live finding without rerunning its audit.
4. For refactors, compare `tools/audit/dump_resolved.py` output before and after. For content changes, run the targeted audit first and the full suite when practical.
5. Before every commit, boot with `launch-game.cmd`, verify the main menu, and confirm no new exception log was created. Stage only the files belonging to the change.

## Audit organization

- `tools/audit/`: executable detectors and shared parsing/model infrastructure.
- `docs/audit/latest/`: generated current reports; overwrite only by running the suite.
- `docs/audit/baseline/`: immutable-ish baseline snapshots.
- `docs/audit/`: human-maintained summaries, decisions, consistency records, and imported historical registers.
- External personal folders: scratch space only. They must link to this file and must not be treated as authoritative.

## Current incident protocol

For an engine crash or player-visible visual regression:

1. Preserve the exact exception and failing asset/actor/sequence.
2. Compare the affected actor, sequence key, asset filename, and tooltip/display references against the last known-good release.
3. Add an evidence-backed P0 item to `docs/design/ROADMAP.md`.
4. Do not modify unrelated palette, template, or naming data while root cause is unproven.
5. Verify with a boot test before considering the incident resolved.
