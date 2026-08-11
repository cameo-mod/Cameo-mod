# Cameo-mod

## ⚡ START HERE — read before acting (the rest of this file is the full contract)

**Don't trust, verify.** Before asserting anything is done / pending / blocked / missing,
check the artifact itself — grep the data, `ls` the file (incl. `~/Downloads`), run the tool,
boot-gate the tree. When a summary (ROADMAP line, handoff, memory, status table) disagrees with
the artifact, **the artifact wins — then fix the stale summary.**

**Must-read, in order:** this file → `docs/LESSONS_LEARNED.md` → `docs/AGENT_WORKSPACE.md` →
`docs/design/ROADMAP.md` → `docs/design/BALANCE_PIPELINE_ESTIMATE.md`. For weapon work also:
`docs/AI_HANDOFF_2026-08-05.md`, `docs/design/AREADAMAGE_HANDOFF.md`, `docs/design/WEAPON_3WAY_SPLIT.md`,
`docs/design/SPREAD_FALLOFF_PLAN.md` (Spread/Falloff balancing: radius=(N-1)×Spread, shape=value spacing,
3-axis gameplay/physics/uniqueness).

**Ten hard rules** (rules 1–2 are enforced by hooks in `.claude/settings.json`):
1. **Boot-gate every commit** of engine content — `launch-game.cmd` must reach the main menu
   (`perf.log` ends `MenuPostProcessEffect.PostWorldLoaded`, no new `exception-*.log`). Snapshot the
   log list + cutoff BEFORE launching; menu-proof is grepping `perf.log`, not its last line.
2. **Scoped `git add <files>` only — never `-A` / `.` / `--all`.** The maintainer + Devin have live WIP.
3. **Never hand-edit a balance number** — use the pipeline (`extract_stats` → ledger →
   `apply_balance --confirm`; `--confirm` needs a maintainer order).
4. **`Versus` lives ONLY in `^Warhead_*` templates.** Never change a warhead / `Burst` / `BurstDelays`
   without explicit permission.
5. **Weapon 3-way split:** preserve resolved behaviour (`Damage` verbatim, projectile fields — the
   Frankenstein merge), `find_empty_warhead.py = 0`, boot-gate per batch. Verify a conversion with
   `tools/audit/review_resolve_diff.py` (before/after resolve).
6. **Multi-agent tree** (maintainer / Devin / you): **one owner per file-set.** Check a file's mtime
   for a live agent before editing; re-verify others' commits before building on them; never
   `git checkout -- .` or wide-add someone else's WIP.
7. **Rebuild C# before boot** if `OpenRA.Mods.Cameo/` or `engine/` changed
   (`DOTNET_ROLL_FORWARD=LatestMajor dotnet build -c Release --nologo -p:TargetPlatform=win-x64` → `engine/bin`).
8. **Audit reports regenerate via `bash tools/audit/run_all.sh` only** (PowerShell `>` writes UTF-16).
9. **Underscore-only naming** — no hyphens in ids / files / fluent keys.
10. **Attribute the ACTUAL author in the commit trailer — never impersonate another agent.**
    Claude Code commits end with `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.
    **Any OTHER agent (Devin, Cascade, etc.) must use its OWN `Co-Authored-By:` line** (e.g.
    `Co-Authored-By: Devin AI <devin@cognition.ai>`) and must NOT append the Claude trailer — the
    git author is a shared repo identity, so the trailer is the only provenance signal and a wrong
    one pollutes history. If you are not Claude, do not sign as Claude.

**Work queue:** `docs/design/ROADMAP.md` (crashes jump the queue). **Effort estimate for the whole
balance program:** `docs/design/BALANCE_PIPELINE_ESTIMATE.md`.

---

## Mission & end goal (never lose sight of this)

Cameo is the ultimate crossover RTS between the classic RTS games and will
keep growing. The architecture goal is **dynamic faction loading**: load
only the factions picked in the lobby / needed by the shellmap, instead of
everything at boot (historical peak: 12 GB RAM — unplayable on 8 GB
machines). Every faction therefore becomes a fully self-contained
ContentPack: rules + weapons + sequences + its own ai.yaml + all assets
(sprites, voxels, icons, sounds) in per-type subfolders, zero cross-pack
dependencies, shared content only in theme Shared/ packs, and unused files
audited and deleted. Current progress + the exact runbook to continue:
**`docs/MIGRATION.md`**.

## Required reading, in order

See `docs/README.md` for the canonical reading order and document authority.
The essential documents, in order:

1. **`docs/LESSONS_LEARNED.md`** — accumulated pitfalls, safe defaults, and
   the required reading order.
2. **`docs/AGENT_WORKSPACE.md`** — mandatory workflow, evidence rules,
   incident protocol, and commit gate.
3. **`docs/PROJECT_CONTEXT.md`** — short project orientation and current
   safety focus.
4. **`docs/DESIGN.md`** — the binding design contract (naming grammar, stat
   formulas, tech tiers, content-pack layout, description scheme, agent
   operating rules). Read it before touching any yaml.
5. **`docs/design/ROADMAP.md`** — active work queue; crashes always jump
   the queue.
6. **`docs/audit/SUMMARY.md`** — current known-issue state by bug class.
7. `docs/Cameo_Knowledge_Base_Manual.md` — the ENGINE/CODE reference
   (v.0.5): custom traits, assemblies (OpenRA.Mods.Cameo/CA),
   activities, bot modules, UI internals. Consult it for any C#-side
   question (it lists code-derived identifiers!); verify against source
   when in doubt — it is a contributor document, not the binding contract.
8. `docs/MASTER_REPORT.md` — historical long-form analysis, bug taxonomy
   (B1–B12); consult §9/§10/§13 when DESIGN.md is not enough. Not a live
   roadmap — active work belongs in ROADMAP.md. (Listed here as essential
   for context; `docs/README.md` classifies it as reference/historical —
   both are correct: read it for background, but don't treat it as binding.)

## Tooling

- `tools/audit/run_all.sh` — full audit suite (run before/after changes;
  single checks: `python tools/audit/audit_<name>.py`).
- `tools/rename/safe_rename.py` + `rename_map_<faction>.yaml` — naming migration
  (replaces the deprecated `apply.py`).
- `tools/packs/split_faction.py` — ContentPack extraction.
- `tools/audit/dump_resolved.py` — resolved-ruleset snapshots; refactors
  must diff empty.
- Recurring code-health audits and freshness policy: `docs/audit/PERIODIC.md`
  and `docs/audit/periodic.json`.

## Commit gate (absolute — no exceptions)

**Never commit without booting the game first.** Run `launch-game.cmd`
and confirm it reaches the main menu with NO new `exception-*.log` in
`%APPDATA%/OpenRA/Logs` (snapshot the log list BEFORE launching; menu
proof: perf.log ends with `MenuPostProcessEffect.PostWorldLoaded`).
The Python resolver does not catch junk trait nodes — only the engine
does, and it parses every faction at boot. If C# sources changed or
were pulled, rebuild first (`dotnet build -c Release --nologo
-p:TargetPlatform=win-x64`); stale DLLs crash the boot with
`Cannot locate type: …Info`. Commit with scoped `git add <files>`,
never `git add -A` — the maintainer usually has live uncommitted edits.

## Balance changes: the pipeline, never by hand

**Never hand-edit a balance number in yaml.** The sanctioned loop
(full spec: `docs/design/BALANCE_PIPELINE.md`):

1. `python tools/balance/extract_stats.py` — refresh the ledger
   (`docs/balance/*.json`, raw stats + provenance).
2. Edit the LEDGER, or generate the workbench
   (`tools/balance/build_workbook.py` →
   `docs/design/cameo_balance_v2.xlsx`, gitignored), edit the unlocked
   input cells there, and read it back with
   `tools/balance/import_workbook.py`.
3. `python tools/balance/apply_balance.py --faction X --confirm` —
   ledger → yaml (dry run without --confirm). **Maintainer order
   required for --confirm.**
4. Re-run `extract_stats.py`, run audits + BOOT GATE, commit yaml and
   ledger TOGETHER.

`audit_balance_drift` (in run_all.sh) fails red whenever yaml and the
committed ledger disagree — hand edits cannot land silently.

The LEGACY workbook `docs/design/cameo_armor_system.xlsx` remains the
reference for design judgments until the Phase-3 discrepancy triage
completes (docs/balance/discrepancies.md). If `~$cameo_armor_system.xlsx`
exists, the workbook is open in Excel: don't write it; queue and say so.

## Memory

Before running any shell command that has a corresponding memory file (build commands, engine sync, git operations), **read that memory file in full before executing**.

## Work queue & token efficiency

- The ordered work queue lives in **`docs/design/ROADMAP.md`** — pick
  from the top (crashes always jump the queue), update it as you go.
- Model/effort cannot be switched by the agent itself (the user picks
  the model). To spend fewer tokens WITHOUT losing quality:
  - batch mechanical sweeps into scripts over the model/registry, never
    file-by-file reading;
  - keep rules in DESIGN.md and plans in ROADMAP.md instead of
    re-deriving them each session; read them FIRST;
  - bundle many small design orders into one implementation pass;
  - verify with the audit suite (cheap) rather than re-reading yaml;
  - subagents on cheaper models are only worth it for self-contained
    batch jobs big enough to amortize their cold-start context.
