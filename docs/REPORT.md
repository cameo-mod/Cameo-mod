# Cameo Documentation Consistency Audit

Date: 2026-07-24

Status: Complete for the initial documentation-consistency pass

Scope: Read-only review of the authored documentation under `Cameo-mod/docs`. No Cameo repository files are being changed. Generated audit output is treated as evidence, not as an independently authoritative design contract.

## Request being answered

Aedis asked for the Markdown documents to be reviewed for inconsistencies, conflicts, contradictions, and obsolete guidance. The accompanying conversation identifies several priority questions:

- whether actor-stat damage includes burst count;
- how multiple mutually exclusive or simultaneously usable weapons should be represented;
- whether a zero-warning goal can provoke behavior-changing false fixes;
- whether WIP prerequisite markers may safely be replaced with `~disabled`;
- whether Atreides and Harkonnen can be restored as playable WIP factions;
- whether the knowledge-base manual predates and conflicts with ContentPacks.

## Confirmed findings

### F-001 — Existing consistency report does not cover the new review request

Severity: Medium

`docs/audit/CONSISTENCY_REPORT.md` is a dated 2026-07-16 audit focused primarily on naming conventions, audit-script scope, roadmap bookkeeping, suffixes, and faction identifiers. It reports that 21 inconsistencies were fixed. It does not define actor-stat-widget damage semantics, multiple-armament aggregation, warning triage policy, or the safety of rewriting WIP prerequisites.

Consequently, it is useful historical evidence but is not a substitute for the review requested in the conversation.

### F-002 — Canonical-document reading order is internally inconsistent

Severity: Low

The entry documents disagree about which file is read first:

- `docs/README.md` says to read `PROJECT_CONTEXT.md`, then `AGENT_WORKSPACE.md`.
- `docs/AGENT_WORKSPACE.md` says `LESSONS_LEARNED.md` is read first, followed by `PROJECT_CONTEXT.md`.
- `docs/PROJECT_CONTEXT.md` says the order begins with `LESSONS_LEARNED.md`, then `AGENT_WORKSPACE.md`, then `DESIGN.md`, then `README.md`.
- `docs/DESIGN.md` describes itself as a document every session reads “FIRST.”
- `docs/LESSONS_LEARNED.md` provides yet another explicit sequence and places itself first.

This is not a gameplay contradiction, but it defeats the purpose of a deterministic bootstrap protocol. A single canonical order should be stated in `README.md` and referenced rather than redefined elsewhere.

### F-003 — Actor-stat “Damage” has no documented display contract

Severity: High

The binding documentation defines several different quantities:

- effective damage per shot;
- total damage per salvo;
- effective DPS;
- spreadsheet Damage;
- a weapon’s raw `Damage`;
- the sum of offensive warheads;
- the sum of a unit’s baseline armaments.

None of the canonical documents defines which of these the in-game actor-stat label named “Damage” promises to show. `DESIGN.md` only documents the widget’s upgrade-icon list. This leaves the UI implementation without a stable, reviewable contract.

Evidence:

- `docs/DESIGN.md:482-487` documents `ActorStatValues.Upgrades`, but not its Damage stat.
- `docs/LESSONS_LEARNED.md:110-115` defines effective damage per shot independently from raw reload.
- `docs/LESSONS_LEARNED.md:169-176` defines the balance SUM law across offensive warheads.
- `docs/DESIGN.md:979-981` says spreadsheet Damage sums baseline primary armaments.
- `docs/DESIGN.md:1182-1190` says mutually exclusive armaments must not have their DPS added for balance.

Recommendation: define separate names and formulas for at least “Damage per shot,” “Damage per salvo,” and “DPS,” then specify exactly which one the widget exposes.

### F-004 — Actor-stat damage already multiplies burst, but aggregates incompatible weapons

Severity: High

The current implementation in `engine/OpenRA.Mods.AS/Traits/ActorStatValues.cs`:

1. selects all valid, currently enabled armaments;
2. sums every `DamageWarhead` whose `UpdatesUnitStatistics` flag is true;
3. multiplies each warhead by `Weapon.Burst`;
4. divides by `DamageDivisor`;
5. adds the result across all armaments;
6. applies firepower modifiers.

Thus Aedis’s burst request is already implemented for the normal warhead-derived path. A triple-burst weapon contributes three shots’ damage.

However, the same function also sums ground-only and air-only armaments even though they cannot attack the same target. This directly conflicts with `DESIGN.md:1182-1190`, which says mutually exclusive weapons must not have their DPS added. The displayed number can therefore represent an impossible salvo.

It also does not model simultaneous firing explicitly. It simply adds every enabled armament, regardless of target compatibility, attack scheduling, turret/mount restrictions, or whether the weapons can fire together.

Additional implementation inconsistencies:

- `totalReloadDelay` is calculated but unused, so the value is not DPS.
- An `ActorStatValues.Damage` override bypasses warhead discovery and burst multiplication.
- An `ArmamentInfo.Damage` override is added directly and also bypasses burst multiplication and `DamageDivisor`.
- Burst delays do not affect the displayed number.
- Target armor `Versus` values, percentage damage, conditional extra damage, splash geometry, projectile multiplicity, and target-specific warheads are not represented by one universal scalar.

Conclusion: the current label is best described as an approximate total raw damage across configured armaments, not generally valid combat damage.

### F-005 — The documents conflict on multi-weapon aggregation

Severity: High

Three rules coexist:

- `DESIGN.md:979-981`: spreadsheet Damage is the sum of every baseline primary armament.
- `DESIGN.md:1182-1190`: mutually exclusive ground/air weapons do not add their DPS.
- `LESSONS_LEARNED.md:117-121`: dual weapons are balanced independently as if each weapon were its own actor.

These can be reconciled only by adding a missing classification:

- simultaneous compatible armaments: sum;
- mutually exclusive or target-disjoint armaments: report separately, or show a target-specific value;
- alternate-mode armaments: report the active mode;
- conditional/upgrade armaments: include only when the condition is active;
- carrier/subweapon damage: identify separately from the carrier’s direct armament.

Without that classification, both the balance sheet and widget can obey one sentence while violating another.

### F-006 — “Burst is flavor, not power” is misleading and conflicts with the formula

Severity: Medium

`FORMULA_V2.md:74-77` says “Burst is flavor, not power,” but immediately compensates a burst increase using `FirepowerMultiplier`. That compensation proves burst changes raw output and therefore is power unless damage, reload, or firepower is adjusted.

The intended rule appears to be:

> Burst count is a presentation/weapon-feel constraint; balance the resulting total damage and cycle DPS back to the class target.

That wording would agree with Aedis’s sound-driven triple burst while avoiding the false implication that increasing Burst has no mechanical effect.

### F-007 — The zero-warning plan converts a linter exemption into a design law

Severity: Critical

`docs/design/MEGAPLAN_YAML_CLEANUP.md` states that every `~wip-content`, `~disable`, `~wip`, and `~unbuildable` gate was renamed to exactly `~disabled`, because the prerequisite linter exempts names beginning with `~disabled`. It then declares this the universal rule.

This is an unsafe reasoning inversion:

- The linter exemption explains how to mark something permanently disabled.
- It does not prove that WIP, intentionally unbuildable, missing-provider, and permanently disabled are the same design state.
- Replacing descriptive tokens suppresses diagnostics and destroys why an actor is unavailable.
- `docs/design/ROADMAP.md:53-54` still treats `~disabled`, `~wip`, and related gates as distinguishable categories, so current documentation is inconsistent.

The “zero warnings” goal should be subordinate to behavior and semantic preservation. Each warning category needs a disposition: real defect, intentional exception, tool false positive, unsupported inactive content, or deferred work. Warning-count reduction alone is not validation.

### F-008 — Atreides and Harkonnen documentation disagrees with active state

Severity: High

Current active configuration:

- `mods/cameo/mod.yaml` includes both Atreides and Harkonnen ContentPacks.
- Atreides’ faction definition is commented out, so it is not selectable.
- Harkonnen is defined with `Selectable: false`.
- `docs/factions/MATRIX.md` lists Harkonnen as not selectable and does not list Atreides at all.
- `docs/MASTER_REPORT.md` discusses both as part of the Dune roster.
- `docs/FACTIONS.md` claims to cover every playable faction loaded from `mod.yaml`, yet contains neither an Atreides nor a Harkonnen faction section.
- `docs/DESIGN.md` treats Atreides and Harkonnen as members of the Dune rank-decoration family.

Therefore “included,” “defined,” “selectable,” “playable,” and “documented” are currently conflated. Restoring them requires a readiness review rather than only uncommenting or flipping `Selectable`.

### F-009 — The faction compendium overstates its authority and completeness

Severity: Medium

`docs/FACTIONS.md:3` calls itself the master reference for all playable factions currently loaded and the single source of truth for faction-level design decisions. This conflicts with:

- `README.md`, which describes it as a roster and identity reference rather than the binding rules document;
- `MASTER_REPORT.md:159-180`, which says the authoritative roster must be generated from active rules;
- the generated matrix, which contains active/defined factions absent from `FACTIONS.md`;
- Aedis’s own warning that the compendium may not be fully accurate.

The compendium should be labeled curated lore and gameplay orientation. Active/selectable status should come from generated evidence.

### F-010 — `AGENT_WORKSPACE.md` is a protocol, not a coordination mechanism

Severity: Medium

The file defines canonical locations and operating rules, but it does not implement the “agent meetings” described in the conversation. It has no live claim format, lease/expiry, branch/worktree field, heartbeat, or conflict-resolution mechanism. Ownership notes appear informally inside the roadmap, including fragile statements such as “their session likely has the context” and “being fixed in the maintainer’s OTHER session.”

It is useful as a shared protocol, but claiming that it coordinates concurrent work overstates what it currently provides.

### F-011 — Knowledge-base manual mixes current ContentPack material with stale examples

Severity: High

The manual’s version note says it reflects the 2026-07-16 ContentPack migration, but it repeatedly describes ContentPacks as optional layers that can be toggled at runtime and contains stale examples such as:

- base `mod.yaml` still loading `rules/redalert2mod.yaml` and `rules/d2k.yaml`;
- packs being listed inside a `Rules:` section rather than through current top-level `Include:` entries;
- asset paths like `ContentPacks/TiberianDawn/GDI/sprites/` rather than the current `files/sprites/` layout;
- statements that only one optional pack exists;
- hard-coded `mod.yaml` line numbers that no longer match.

The manual remains valuable for architecture lookup, but its repository-snapshot claims must be verified against current source. It should not be treated as uniformly current merely because its opening version note mentions ContentPacks.

### F-012 — `MASTER_REPORT.md` still presents a competing roadmap

Severity: Medium

`README.md` says active work belongs only in `design/ROADMAP.md`, while `DESIGN.md` says the “long-form analysis and roadmap” live in `MASTER_REPORT.md`. The master report contains its own long-term roadmap and tells readers to keep that phase status current.

This creates two places that appear authorized to express roadmap state. The master report’s roadmap should be explicitly labeled historical strategy, with every actionable status moved or linked to the sole active roadmap.

### F-013 — Agent workflow documents conflict with current project safety rules

Severity: High

The repository documents recommend or require operations that the current project instructions explicitly forbid agents from performing automatically:

- `MASTER_REPORT.md` repeatedly instructs agents and CI to run `utility --check-yaml`.
- The current project instructions say never to run `--check-yaml`.
- `AGENT_WORKSPACE.md` requires a `launch-game.cmd` boot before every commit.
- The current project workflow says Codex must not launch the game unless Blackrobe explicitly asks; runtime/in-game review belongs to Blackrobe by default.

Even within the docs, `FINDINGS.md` admits that `--check-yaml` can pass content which later crashes when exercised. The documentation should distinguish maintainer/manual validation, CI validation, and actions permitted for automated agents.

### F-014 — The YAML cleanup megaplan is a second active work queue

Severity: Medium

`README.md` and `MEGAPLAN.md` state that active work belongs only in `design/ROADMAP.md`. Nevertheless, `design/MEGAPLAN_YAML_CLEANUP.md` contains its own objective, phases, completion marks, pending work, commit log, next steps, and definition of done.

Although the roadmap links the zero-warning initiative, the detailed megaplan independently tracks live status and therefore recreates the split-authority problem the documentation architecture says it eliminated.

### F-015 — Current audit directory violates its own `.safe.md` policy

Severity: Low

`LESSONS_LEARNED.md:127-129` says decoded `*.safe.md` copies are temporary and must never be committed. The current `docs/audit/latest/` tree contains:

- `buildable_order.safe.md`;
- `min_range.safe.md`;
- `stat_formulas.safe.md`;
- `weapon_uniqueness.safe.md`.

At minimum, the documentation and current artifact layout disagree. If these files are intentionally local/untracked scratch artifacts, the policy should say where they belong and tooling should clean or ignore them.

### F-016 — The prior consistency audit verifies regression markers, not global consistency

Severity: Medium

The current `tools/audit/audit_consistency_report.py` passes 73 checks with zero failures. Inspection shows that these checks only confirm that specific strings and files associated with the July 16 fixes still exist or remain absent.

It does not re-run a general contradiction analysis, validate document authority, evaluate formulas, compare current active faction state, or detect the issues in this report. Its successful output—“All consistency fixes ... are still in place”—is accurate but much narrower than the report title can imply.

Validation performed during this review:

```text
Checks passed: 73
Checks failed: 0
```

### F-017 — Primary authored-document links are structurally intact

Severity: Informational

A file-target check across the top-level authored Markdown, `design/`, `balance/`, and principal human-maintained audit reports found no missing relative link targets. This does not validate anchor fragments or the truth of linked content, but it rules out missing-file links in the primary corpus.

### F-018 — The binding documents disagree on infantry Speed granularity

Severity: Medium

`DESIGN.md:824` states without qualification that Speed uses steps of 5. `FORMULA_V2.md:65-69` and `LESSONS_LEARNED.md:72` instead state that infantry use steps of 1 while vehicles, aircraft, and ships use steps of 5.

Because `DESIGN.md` and Formula V2 both claim binding authority, this is a direct rule conflict. The more specific, newer rule appears to be infantry step 1 and non-infantry step 5, but `DESIGN.md` must say so explicitly.

### F-019 — A binding class log retains the superseded uniqueness definition

Severity: Low

`MEGAPLAN.md` labels `docs/balance/formula_v2_<class>.md` conversion logs as binding records. `formula_v2_scout.md:27` says uniqueness checks cover HP, Speed, Range, and effective DPS. The current binding rule in `FORMULA_V2.md:191-203` and `LESSONS_LEARNED.md:105-115` instead defines five comparison fields and explicitly separates effective damage per shot from raw `ReloadDelay`.

Historical class logs should be labeled snapshots or updated with a supersession note so that old successful checks are not mistaken for the current invariant.

## Executive assessment

The documentation corpus is useful and unusually rich, but it currently mixes four different kinds of material:

1. binding design law;
2. current work/status tracking;
3. generated evidence;
4. historical analysis and session records.

Most high-risk inconsistencies occur when a document crosses those boundaries without changing its authority label.

The five issues that matter most operationally are:

1. Actor-stat Damage lacks a defined UI contract and currently sums mutually exclusive weapons.
2. Multi-weapon aggregation rules conflict until armaments are classified by simultaneous versus exclusive use.
3. The zero-warning plan turns `~disabled` linter suppression into a semantic rewrite rule.
4. Atreides/Harkonnen status differs between active configuration, generated matrix, faction compendium, and design references.
5. The knowledge-base manual contains current architecture mixed with stale repository snapshots.

## Recommended resolution order

### P0 — Prevent behavior-changing cleanup

- Suspend any blanket prerequisite renaming based solely on warning reduction.
- Require each unresolved prerequisite warning to be classified before modification.
- Preserve WIP/legacy/unbuildable intent in explicit metadata even if the engine-facing gate must use `~disabled`.

### P1 — Define combat-stat semantics

- Decide whether the UI should show per-shot damage, per-salvo damage, DPS, or multiple target-specific values.
- Classify armaments as simultaneous, mutually exclusive, alternate mode, conditional, carrier, or utility.
- Define how bursts, burst delays, reload, damage warheads, `Versus`, percentage damage, and firepower modifiers contribute.
- Rename the label if the metric remains approximate.

### P1 — Normalize authority

- Make `README.md` the sole reading-order definition.
- Keep binding law in `DESIGN.md`, `FORMULA_V2.md`, and `ARMOR_SYSTEM.md`.
- Keep current tasks only in `ROADMAP.md`.
- Label `MASTER_REPORT.md`, class conversion logs, and session reports as historical analysis unless a section is deliberately promoted.

### P2 — Repair faction-state documentation

- Generate the active/defined/selectable/playable status table from the actual rules.
- Treat `FACTIONS.md` as curated lore and gameplay description.
- Add explicit Atreides/Harkonnen readiness entries before making them selectable.

### P2 — Version the knowledge base

- Add a prominent “architecture versus snapshot” distinction.
- Replace hard-coded line numbers and stale file examples.
- Mark chapters that still describe pre- or mid-migration ContentPack layouts.

## Validation performed

- Read the canonical entry documents and authority declarations.
- Reviewed the previous `CONSISTENCY_REPORT.md`.
- Inspected and ran `audit_consistency_report.py` after syntax compilation: 73 passed, 0 failed.
- Traced actor-stat Damage from the Cameo widget into `ActorStatValues.CalculateDamage`.
- Checked active `mod.yaml` ContentPack includes before examining D2k faction files.
- Compared Atreides and Harkonnen faction definitions with `FACTIONS.md`, `MATRIX.md`, `DESIGN.md`, and `MASTER_REPORT.md`.
- Checked primary authored Markdown for missing relative file targets: 0 missing.
- Searched binding balance documents for damage, burst, multi-weapon, SUM/MAX, stat-grid, and authority statements.

## Scope limitations

- Generated tables were sampled and used as evidence; they were not manually re-audited row by row.
- Spreadsheet cell formulas were not inspected in this documentation-only pass.
- No game runtime or framebuffer validation was performed.
- No repository files were changed.

## Findings under investigation

None for this pass. Future reviews can deepen individual areas—especially spreadsheet formulas and runtime actor-stat behavior—without reopening the complete documentation corpus.
