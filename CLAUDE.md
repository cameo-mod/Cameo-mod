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
   ⚠ **`engine/` IS NOT PART OF THIS REPO** — it is `.gitignore`d, has no `.git`/`.gitmodules`,
   and `git ls-files engine` returns **zero** files (`git` run from inside it silently targets
   the PARENT repo). Editing `engine/**` produces work that **cannot be committed here** and is
   **deleted by the next `make all`**. To change the engine, follow
   **`docs/LESSONS_LEARNED.md` → "The canonical engine update pipeline"**: edit the SEPARATE
   `cameo-engine` clone of `github.com/cameo-mod/OpenRA` → push → `git rev-parse cameo-engine`
   for the full 40-char hash → set `ENGINE_VERSION` in **`mod.config`** → `make.cmd all` →
   verify `engine/VERSION` + recreate `engine/glsl/` shaders → boot-gate → commit `mod.config`.
   **First check whether a mod-side SHADOW avoids all of that:** `ObjectCreator.FindType` takes
   the first assembly in `mod.yaml`'s `Assemblies` list (AS, CA, **Cameo**, Cnc, D2k, Common),
   so an `OpenRA.Mods.Cameo` type of the same name wins with zero yaml changes (precedent:
   `ColorPickerColorShift`, `PlayerColorShift`, `SelectionDecorations`). Prove a shadow with a
   Cameo-only field — `--docs` lists both types and proves nothing.
8. **Audit reports regenerate via `bash tools/audit/run_all.sh` only** (PowerShell `>` writes UTF-16).
8b. **The engine DROPS unknown yaml fields in silence.** `FieldLoader.Load` (FieldLoader.cs:676)
   iterates the TYPE's fields and never reads the leftover keys, and traits + warheads go through
   it (`WeaponInfo.cs:178`). Only `FieldLoader.LoadField` throws — that is the settings/linter
   path, and two Cameo docs used to claim otherwise. A misplaced field therefore costs nothing at
   boot and everything in play: 2059 warheads carried a `Falloff` their type has no field for.
   Run **`audit_dead_warhead_fields.py`** (LOWER-ONLY ratchet). ⚠ Reading C# to build a field set,
   match `public`, **not** `public readonly` — some AS warheads declare mutable public fields.
   The same trap exists one layer up: a weapon inheriting TWO `^Projectile_*` templates merges into
   ONE node whose TYPE is the last template's and whose FIELDS are the union of both.
8e. **NEVER hand-parse yaml — read through `miniyaml.Ruleset.resolve_weapon` / `.resolve`,**
   and pull Versus with `weapon_efficiency.versus_of(node)`. A bespoke line-scanner opened a dict
   on `Versus:` and never CLOSED it, so the `PercentageVersus:` rows the AreaDamage fold added
   INSIDE the same warhead overwrote the profile: every measured mean, spread, ratio and inversion
   count was internally consistent and wrong (reported "0 of 125 obey the MEAN-100 law"; the truth
   was **123 of 125**). A near-miss sibling name is the trap — `PercentageVersus` does not
   `startswith("Versus:")`, so the OPEN guard looked right; the bug was the missing CLOSE. If a
   hand parser is unavoidable, close every block the moment indentation returns to its level.
   Guarded by `audit_versus_profile.py`. And **a result that contradicts a binding law the
   generator implements is a contradiction, not a finding** — check before believing it.
8f. **GREP `docs/DESIGN.md` BEFORE DESIGNING ANYTHING.** It is required reading #4 and it is
   binding. §12.0a (MEAN-100: `K` is SHAPE-ONLY, `Damage` is the sole magnitude knob), §12.0c (the
   Shield ladder) and §12.0d (the CLASS TILT — each level tilts toward one end of every armor
   ladder and *"can never invert"*) were all already ruled and already shipped while days of design
   work re-derived them. A design question that feels novel usually is not.
8c. **A "derive unless overridden" default is invisible when something upstream always overrides.**
   `ScaledBullet` derived shell Inaccuracy/Speed from Range for weeks and reached zero weapons,
   because the templates also wrote literals and an explicit yaml value always wins. Assert the
   DERIVED value on a real resolved weapon, never that the knob is merely present.
8d. **Every warhead family must be unique** — no two may share both a radius and a curve
   (`audit_family_uniqueness.py`). Shape comes from `PHYSICS_SHAPES` in `gen_weapon_template.py`,
   the level scales the radius only, and blends cross their parents' shapes via `blend_shape()`.
   Radius = **(N-1) x Spread**, not N x Spread. Always `splice_templates.py --all`, never a subset:
   adding a family re-ranks the shield-coupling ladder and a partial splice leaves drift.
9. **Underscore-only naming** — no hyphens in ids / files / fluent keys.
10. **Attribute the ACTUAL author in the commit trailer — never impersonate another agent.**
    Sign with **your own** identity, including your real model name:
    `Co-Authored-By: Claude <model> <noreply@anthropic.com>` — e.g. Opus 5 signs
    `Claude Opus 5`, Opus 4.8 signs `Claude Opus 4.8`. **Never copy the trailer from a
    previous commit or from this file** — it is a template, not a literal; a version pinned
    here goes stale the moment the model changes, and copying it makes a newer model
    misreport itself as an older one.
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
