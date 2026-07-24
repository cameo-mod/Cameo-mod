# Cameo documentation — index & single source of truth

This file is the **sole reading-order definition** and the map of what each
document is authoritative for. If any two documents disagree, the precedence
order below wins; fix the loser, never both.

## Precedence (highest wins)

1. **`CLAUDE.md`** (repo root) — project instructions, loaded every session. The top authority.
2. **Binding law** — `DESIGN.md`, `design/FORMULA_V2.md`, `design/ARMOR_SYSTEM.md`, `design/BALANCE_PIPELINE.md`.
3. **Active work** — `design/ROADMAP.md` (the only live task queue).
4. **Reference & analysis** — everything else below.
5. **`history/`** — dated, non-authoritative snapshots. Never overrides anything current.

## Read order (every session, in this order)

1. `CLAUDE.md` — mission, gates, memory rules (root).
2. `DESIGN.md` — binding design contract (naming, stat formulas, tiers, content-pack layout, agent rules).
3. `design/ROADMAP.md` — active work queue; pick from the top, record new bugs here.
4. `audit/SUMMARY.md` — current known-issue state by bug class.
5. Then the topic docs for your task (see the table).

Crashes and player-visible regressions always jump the queue.

## Where each topic lives (one authoritative doc each)

| Topic | Authoritative document |
|---|---|
| Mission, gates, memory, model/effort rules | `CLAUDE.md` (root) |
| Binding rules: naming, stats, tiers, packs, descriptions | `DESIGN.md` |
| Active work queue & ownership | `design/ROADMAP.md` |
| Balance formula law (per-class, SUM, bands, uniqueness) | `design/FORMULA_V2.md` |
| Balance machinery (ledger ⇄ workbook ⇄ gated apply, drift) | `design/BALANCE_PIPELINE.md` |
| Armor / damage-type system | `design/ARMOR_SYSTEM.md` |
| Faction identity, lore, playstyle (curated compendium) | `FACTIONS.md` |
| Faction BALANCE bias (how units differ within a class, source-cited) | `design/FACTION_IDENTITY.md` |
| Original source-game unit stats (ground-truth matrix for relative balance) | `design/ORIGINAL_UNIT_STATS.md` |
| Engine / custom-trait / C# reference | `Cameo_Knowledge_Base_Manual.md` |
| ContentPack migration runbook | `MIGRATION.md` |
| Accumulated pitfalls & safe defaults | `LESSONS_LEARNED.md` |
| Current known-issue state | `audit/SUMMARY.md` |
| Mandatory workflow / evidence / commit gate | `AGENT_WORKSPACE.md` |

## Reference / historical analysis (consult, don't treat as binding)

- `MASTER_REPORT.md` — long-form bug taxonomy (B1–B12) and roadmap analysis. Consult §9/§10/§13 when DESIGN.md is not enough. Historical analysis unless a section is deliberately promoted into DESIGN/ROADMAP.
- `design/MEGAPLAN.md`, `design/MEGAPLAN_YAML_CLEANUP.md` — non-binding program indexes; defer to ROADMAP for live tasks.
- `design/VISION.md` — north-star vision.
- `PROJECT_CONTEXT.md` — short orientation summary; the docs above are authoritative over it.

## Generated artifacts — do NOT hand-edit

These are produced by tooling and regenerate; editing them by hand is meaningless.

- `audit/latest/` — current audit evidence (`tools/audit/run_all.sh`). `audit/baseline/` — comparison snapshots.
- `balance/*.json` — balance ledgers (source of truth for numbers), refreshed by `tools/balance/extract_stats.py`.
- `balance/proposal_*.md`, `balance/membership_review.md` — per-class conversion proposals (`tools/balance/propose_class_rebalance.py`).
- `factions/MATRIX.md` — generated faction matrix.

## `history/` — archived, non-authoritative

Dated session logs, one-off reports, and superseded recommendations live in `history/`.
They record *what happened*, never *what is true now*. Anything still relevant has
been promoted into the authoritative docs above.
