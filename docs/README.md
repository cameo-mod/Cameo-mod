# Cameo Documentation

Read `PROJECT_CONTEXT.md`, then `AGENT_WORKSPACE.md`, before starting work. The repository is the shared source of truth; personal notes and external-agent output are historical until verified here.

## Start here

| Need | Canonical document |
|---|---|
| Project orientation and current safety focus | [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md) |
| Required agent workflow, evidence, and commit gate | [AGENT_WORKSPACE.md](AGENT_WORKSPACE.md) |
| Binding implementation and content rules | [DESIGN.md](DESIGN.md) |
| Active work and ownership | [design/ROADMAP.md](design/ROADMAP.md) |
| ContentPack migration process | [MIGRATION.md](MIGRATION.md) |
| Faction identity and roster reference | [FACTIONS.md](FACTIONS.md) |
| Engine and custom-trait reference | [Cameo_Knowledge_Base_Manual.md](Cameo_Knowledge_Base_Manual.md) |

## Evidence and generated artifacts

- **Current audit evidence:** `audit/latest/` — regenerate with `tools/audit/run_all.sh`; do not hand-edit.
- **Audit baselines:** `audit/baseline/` — comparison snapshots, not live status.
- **Faction matrix:** `factions/MATRIX.md` — generated; do not hand-edit.
- **Balance ledgers and workbook:** `balance/*.json`, `balance/class_anchors.json`, and `balance/cameo_balance_v2.xlsx` — pipeline-owned; keep their paths unchanged.

## Document ownership

- **Rules:** `DESIGN.md`, `design/FORMULA_V2.md`, and `design/ARMOR_SYSTEM.md`.
- **Active work:** `design/ROADMAP.md` only.
- **Balance process:** `design/BALANCE_PIPELINE.md`, `design/MEGAPLAN.md`, and `balance/formula_v2_<class>.md` class logs.
- **Audit triage:** `audit/SUMMARY.md` and `audit/FINDINGS.md`.
- **History:** `history/` contains dated, non-authoritative context. It never overrides current documents.
