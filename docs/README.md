# Cameo documentation — index & single source of truth

**New here, or resuming after a break? Start at [`HANDOFF.md`](HANDOFF.md).**

This file is the **sole reading-order definition** and the map of what each document is
authoritative for. If any two documents disagree, the precedence order below wins; fix the
loser, never both.

## Precedence (highest wins)

1. **[`CLAUDE.md`](../CLAUDE.md)** (repo root) — project instructions, loaded every session. The top authority.
2. **Binding law** — [`DESIGN.md`](DESIGN.md), [`design/FORMULA_V2.md`](design/FORMULA_V2.md), [`design/ARMOR_SYSTEM.md`](design/ARMOR_SYSTEM.md), [`design/BALANCE_PIPELINE.md`](design/BALANCE_PIPELINE.md).
3. **Current state & queue** — [`HANDOFF.md`](HANDOFF.md) (entry point), [`design/BALANCE_PROGRAM_PLAN.md`](design/BALANCE_PROGRAM_PLAN.md) (balance board + ownership), [`design/ROADMAP.md`](design/ROADMAP.md) (granular queue).
4. **Reference & analysis** — everything else below.
5. **[`history/`](history/)** — dated, non-authoritative snapshots. Never overrides anything current.

**Above all of it: the artifact.** A document is a claim about the tree; the tree is the tree.
When they disagree, run the tool, fix the document.

## Read order (every session, in this order)

1. [`CLAUDE.md`](../CLAUDE.md) — hard rules, mission, gates (root).
2. [`LESSONS_LEARNED.md`](LESSONS_LEARNED.md) — accumulated pitfalls, safe defaults.
3. [`AGENT_WORKSPACE.md`](AGENT_WORKSPACE.md) — workflow, evidence rules, commit gate.
4. [`HANDOFF.md`](HANDOFF.md) — verified current state and the priority-ordered queue.
5. [`DESIGN.md`](DESIGN.md) — the binding design contract. Read the sections your change touches.
6. [`design/ROADMAP.md`](design/ROADMAP.md) — the granular work queue.
7. [`audit/SUMMARY.md`](audit/SUMMARY.md) — current known-issue state by bug class.
8. Then the topic doc for your task (table below).

[`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md) is a one-page orientation for a first-time reader; it
is a convenience, not a step, and everything above is authoritative over it.

The always-on rule file [`.windsurf/rules/start-protocol.md`](../.windsurf/rules/start-protocol.md)
and the `SessionStart` hook (`tools/hooks/session_checklist.py`) enforce this order at the IDE
and CLI level. If either conflicts with this file, **this file wins** — and fix the copy.

Crashes and player-visible regressions always jump the queue.

## Where each topic lives (one authoritative doc each)

| Topic | Authoritative document |
|---|---|
| Mission, hard rules, gates, model/effort rules | [`CLAUDE.md`](../CLAUDE.md) (root) |
| **Entry point: verified state + priority queue** | [`HANDOFF.md`](HANDOFF.md) |
| Binding rules: naming, stats, tiers, packs, descriptions | [`DESIGN.md`](DESIGN.md) |
| Balance program **board, ownership, acceptance criteria** (W1–W26) | [`design/BALANCE_PROGRAM_PLAN.md`](design/BALANCE_PROGRAM_PLAN.md) |
| Balance program **phase-map** (the strategic A→G sequence) | [`design/BALANCE_MEGAPLAN.md`](design/BALANCE_MEGAPLAN.md) |
| Granular work queue (individual tasks + commit hashes) | [`design/ROADMAP.md`](design/ROADMAP.md) |
| Balance formula law (per-class, SUM, bands, uniqueness) | [`design/FORMULA_V2.md`](design/FORMULA_V2.md) |
| Balance machinery (ledger ⇄ workbook ⇄ gated apply, drift) | [`design/BALANCE_PIPELINE.md`](design/BALANCE_PIPELINE.md) |
| Effort estimate for the whole balance program | [`design/BALANCE_PIPELINE_ESTIMATE.md`](design/BALANCE_PIPELINE_ESTIMATE.md) |
| Armor / damage-type system | [`design/ARMOR_SYSTEM.md`](design/ARMOR_SYSTEM.md) |
| Armor-plating layer, shields, Integrity (measured mechanics) | [`design/PSEUDO_ARMOR_AND_INTEGRITY.md`](design/PSEUDO_ARMOR_AND_INTEGRITY.md) |
| Spread / damage-falloff per-type profiles | [`design/SPREAD_FALLOFF_PLAN.md`](design/SPREAD_FALLOFF_PLAN.md) |
| Physical-state / status-effect layer (heat/cryo/corrosion, EMP, sonic) | [`design/PHYSICAL_STATE_SYSTEM.md`](design/PHYSICAL_STATE_SYSTEM.md) |
| EMP / Integrity auto-scaling | [`design/EMP_INTEGRITY_SYSTEM.md`](design/EMP_INTEGRITY_SYSTEM.md) |
| Area-integrated `effective_damage` metric (spec) | [`design/EFFECTIVE_DAMAGE.md`](design/EFFECTIVE_DAMAGE.md) |
| Weapon 3-way split (warhead / projectile / effect) | [`design/WEAPON_3WAY_SPLIT.md`](design/WEAPON_3WAY_SPLIT.md) |
| Weapon type classification system | [`design/WEAPON_TYPE_SYSTEM.md`](design/WEAPON_TYPE_SYSTEM.md) |
| Faction identity, lore, playstyle (curated compendium) | [`FACTIONS.md`](FACTIONS.md) |
| Faction BALANCE bias (how units differ within a class) | [`design/FACTION_IDENTITY.md`](design/FACTION_IDENTITY.md) |
| Original source-game unit stats (ground truth for relative balance) | [`design/ORIGINAL_UNIT_STATS.md`](design/ORIGINAL_UNIT_STATS.md) |
| Mod-synthesis balance overhaul plan (sources, laws, methodology) | [`design/BALANCE_SYNTHESIS.md`](design/BALANCE_SYNTHESIS.md) |
| Class-anchor decisions (maintainer-confirmed baselines + verifiers) | [`balance/anchor_decisions_log.md`](balance/anchor_decisions_log.md) |
| Upgrade intent registry (direction, coverage, phase, drawbacks) | [`design/upgrades_intent.yaml`](design/upgrades_intent.yaml) |
| Engine / custom-trait / C# reference | [`Cameo_Knowledge_Base_Manual.md`](Cameo_Knowledge_Base_Manual.md) |
| ContentPack migration runbook | [`MIGRATION.md`](MIGRATION.md) |
| Accumulated pitfalls & safe defaults | [`LESSONS_LEARNED.md`](LESSONS_LEARNED.md) |
| Current known-issue state | [`audit/SUMMARY.md`](audit/SUMMARY.md) |
| Numeric claims that must not rot (with re-measure commands) | [`audit/doc_claims.yaml`](audit/doc_claims.yaml) |
| Recurring code-health audits and their cadence | [`audit/PERIODIC.md`](audit/PERIODIC.md) + [`audit/periodic.json`](audit/periodic.json) |
| Mandatory workflow / evidence / commit gate | [`AGENT_WORKSPACE.md`](AGENT_WORKSPACE.md) |

## Reference / historical analysis (consult, don't treat as binding)

- [`MASTER_REPORT.md`](MASTER_REPORT.md) — long-form bug taxonomy (B1–B12) and structural analysis, dated 2026-07-08. Consult §9/§10/§13 when `DESIGN.md` is not enough. Several artifacts it proposes (`docs/design/damage_model.md`, `unit_classes.yaml`, `faction_registry.yaml`, `asset_budget.md`, `docs/CREDITS.yaml`) were **never created** — do not go looking for them.
- [`design/MEGAPLAN_YAML_CLEANUP.md`](design/MEGAPLAN_YAML_CLEANUP.md) — the zero-errors/zero-warnings `check-yaml` program. Non-binding index; its 2026-07-23 baseline file was never committed.
- [`design/VISION.md`](design/VISION.md) — north-star product intent, explicitly not a queue.
- [`design/GAME_SPECIFIC_WEAPON_BASES.md`](design/GAME_SPECIFIC_WEAPON_BASES.md), [`design/PROJECTILE_EFFECT_SOURCING.md`](design/PROJECTILE_EFFECT_SOURCING.md), [`design/PROJECTILE_TEMPLATES.md`](design/PROJECTILE_TEMPLATES.md) — weapon-layer research and template design.
- [`design/PLATING_COMBINATION.md`](design/PLATING_COMBINATION.md), [`design/PLATING_COMPOSITION_REFINEMENT.md`](design/PLATING_COMPOSITION_REFINEMENT.md), [`design/SHIELD_AND_NORMALISATION_PLAN.md`](design/SHIELD_AND_NORMALISATION_PLAN.md), [`design/SUPERWEAPON_LAYER_DAMAGE.md`](design/SUPERWEAPON_LAYER_DAMAGE.md) — armor/shield analyses behind DESIGN §12.0c–§12.0g.
- [`design/INVENTED_WARHEAD_FAMILIES.md`](design/INVENTED_WARHEAD_FAMILIES.md) — design sheet for the seven DESIGNED families (no cross-mod equivalent). `Toxic` is the eighth family in the sidecar JSON but is measured from Cameo's own gas library, so it is deliberately not in the sheet's table.
- [`design/cabal_rebuild_plan.md`](design/cabal_rebuild_plan.md), [`design/schwarzer_mond_artwork_status.md`](design/schwarzer_mond_artwork_status.md), [`design/shattered_paradise_research.md`](design/shattered_paradise_research.md), [`design/mission_win_lose_research.md`](design/mission_win_lose_research.md), [`design/tier_chain_validation.md`](design/tier_chain_validation.md) — per-topic research.
- [`design/HEX_SHIELD_VISUALS.md`](design/HEX_SHIELD_VISUALS.md) — shield visual decisions.
- [`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md) — one-page orientation; everything above is authoritative over it.

## Generated artifacts — do NOT hand-edit

Produced by tooling and regenerated; hand-editing them is meaningless.

- [`audit/latest/`](audit/latest/) — current audit evidence. Regenerate with `bash tools/audit/run_all.sh` (**bash only** — a PowerShell `>` redirect writes UTF-16). `run_all.py` is a faithful Python port for shells without `sh`; it reads its audit list out of `run_all.sh` so the two cannot drift apart.
- [`audit/baseline/`](audit/baseline/) — comparison snapshots. Historical.
- `balance/*.json` — balance ledgers, the source of truth for numbers. `python tools/balance/extract_stats.py`. **Commit yaml and ledger together.**
- `balance/derived/*.json` — derived sidecars (W3 split them out of the raw ledger).
- `balance/proposal_*.md`, `balance/membership_review.md` — per-class conversion proposals (`tools/balance/propose_class_rebalance.py`).
- [`factions/MATRIX.md`](factions/MATRIX.md) — generated faction matrix.
- `balance/class_anchors.json` — class anchor registry, maintained via `balance/anchor_decisions_log.md`.
- [`design/INVENTED_WARHEAD_FAMILIES.md`](design/INVENTED_WARHEAD_FAMILIES.md) + `design/invented_family_profiles.json` — `tools/balance/design_invented_profiles.py --write`.

⚠ **`design/cameo_balance_v2.xlsx` is TRACKED in git**, despite older notes calling it
"gitignored" — `git check-ignore` says otherwise. Regenerating it with `build_workbook.py`
produces a real diff; commit it or restore it deliberately.

⚠ **Do not drop a one-off artifact into `audit/latest/`.** That directory is regenerated
wholesale, so anything the suite does not produce is deleted on the next run — which is how
`latest/superweapon_audit.yaml` disappeared while three documents still linked to it.

## Things you cannot resolve from this repository

- **`memory <name>` citations** (36 of them, mostly in `design/BALANCE_MEGAPLAN.md`) point at an external, per-agent memory store. **Provenance only, never authority.** Anything binding must be promoted into `DESIGN.md`.
- **Commit hashes older than 2026-08-10** do not resolve in a shallow checkout (cloud/CI). `git fetch --unshallow`, or verify against the artifact instead.
- **`engine/`** is `.gitignore`d and not part of this repository. See `HANDOFF.md` §5.

## `history/` — archived, non-authoritative

Dated session logs, superseded handoffs, closed roadmap sections and one-off reports. They
record *what happened*, never *what is true now*. Anything still relevant has been promoted into
the authoritative docs above.

- [`history/handoffs/`](history/handoffs/) — every handoff superseded by [`HANDOFF.md`](HANDOFF.md).
- [`history/audits/`](history/audits/) — one-off dated audits.
- [`history/ROADMAP_ARCHIVE_2026-07.md`](history/ROADMAP_ARCHIVE_2026-07.md) — closed ROADMAP sections.
- [`../DEVELOPMENT_LOG.md`](../DEVELOPMENT_LOG.md) (repo root) — append-only multi-agent development log. Historical record, still actively appended to.
