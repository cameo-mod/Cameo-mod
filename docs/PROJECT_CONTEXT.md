# Cameo Project Context — Agent Summary

Use this as a short orientation document. It summarizes the repository documentation; the referenced primary documents remain authoritative.

## Project

Cameo is an OpenRA crossover RTS mod. The repository is undergoing a migration toward self-contained faction ContentPacks, consistent actor/asset naming, auditable balance rules, and safer rule changes. The last known-good release used for regression comparisons is:

`C:\Users\AedisToru\AppData\Local\Cameo-IFV\instances\cameo\main`

## Non-negotiable workflow

1. Crashes and player-visible regressions take priority over roadmap work.
2. Before YAML or asset changes, read `DESIGN.md`, `audit/SUMMARY.md`, and relevant engine/custom-trait documentation in `Cameo_Knowledge_Base_Manual.md`.
3. For refactors, compare resolved rulesets before and after with `tools/audit/dump_resolved.py`.
4. Run targeted audits from `tools/audit/`; current reports belong in `audit/latest/`.
5. Before any commit, boot via `launch-game.cmd`, verify the main menu, and check for new exception logs. Stage files explicitly; never stage the full working tree indiscriminately.

## Documentation map

| Document | Purpose |
|---|---|
| `DESIGN.md` | Binding implementation contract: naming, actor/template rules, tech tiers, balance formulas, effects, descriptions, and migration rules. |
| `design/ROADMAP.md` | Only active work queue. New bugs and crashes are logged here first. |
| `audit/SUMMARY.md` | One-page live audit overview and priority order. |
| `audit/FINDINGS.md` | Detailed audit findings and evidence. |
| `audit/CONSISTENCY_REPORT.md` | Regression checks for prior fixes. |
| `MASTER_REPORT.md` | Long-term architecture, bug taxonomy, audit rationale, and migration strategy. |
| `MIGRATION.md` | Rename/split/description workflow; refactors must be resolver-verified. |
| `FACTIONS.md` | Human-facing faction identity, gameplay, rosters, upgrades, and display-name reference. |
| `factions/MATRIX.md` | Generated quantitative roster matrix. |
| `Cameo_Knowledge_Base_Manual.md` | Engine and custom-mod code reference. Consult specific sections rather than reading the full multi-megabyte document for every task. |

## Audit structure

- `tools/audit/` contains the reusable, repository-maintained audit scripts.
- `audit/latest/` is regenerated evidence from the current tree.
- `audit/baseline/` is historical reference.
- `audit/LEGACY_DEVIN_CABAL.md` indexes old external CABAL reports; they are historical only until rerun.

## Current safety focus

- The currently reported TD GDI palette/animation issue is open. Its evidence record is `audit/INCIDENT_TD_GDI_RELEASE_REGRESSION.md`.
- A menu-load crash was observed from two `brik:` sequence entries referencing nonexistent `futuretech_concretebarrier_brik.shp`. Local references were returned to the existing release-compatible TD filenames; a clean boot remains required before resolution.
- Do not change palettes, templates, actor names, or tooltip data merely because a migration looks suspicious. Require an observed mismatch, current audit output, release comparison, or engine exception.

## Multi-agent rule

The repository docs are shared truth. `C:\Users\AedisToru\Documents\DevinCameoProject` is retained as an external historical/scratch folder only. Its roadmap and instructions point back to this repository; do not create or maintain a second active roadmap or audit-output tree there.
