# Cameo-mod

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
3. **`docs/DESIGN.md`** — the binding design contract (naming grammar, stat
   formulas, tech tiers, content-pack layout, description scheme, agent
   operating rules). Read it before touching any yaml.
4. **`docs/design/ROADMAP.md`** — active work queue; crashes always jump
   the queue.
5. **`docs/audit/SUMMARY.md`** — current known-issue state by bug class.
6. `docs/Cameo_Knowledge_Base_Manual.md` — the ENGINE/CODE reference
   (v.0.4): custom traits, assemblies (OpenRA.Mods.Cameo/CA),
   activities, bot modules, UI internals. Consult it for any C#-side
   question (it lists code-derived identifiers!); verify against source
   when in doubt — it is a contributor document, not the binding contract.
7. `docs/MASTER_REPORT.md` — historical long-form analysis, bug taxonomy
   (B1–B12); consult §9/§10/§13 when DESIGN.md is not enough. Not a live
   roadmap — active work belongs in ROADMAP.md.

## Tooling

- `tools/audit/run_all.sh` — full audit suite (run before/after changes;
  single checks: `python tools/audit/audit_<name>.py`).
- `tools/rename/apply.py` + `rename_map_<faction>.yaml` — naming migration.
- `tools/packs/split_faction.py` — ContentPack extraction.
- `tools/audit/dump_resolved.py` — resolved-ruleset snapshots; refactors
  must diff empty.

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
