# Historical Devin CABAL Audit Register

## Status

This document indexes material in the external DevinCameoProject folder that predates this shared workspace. It is **historical provenance, not current audit evidence**. The external folder contains a template README, a second local roadmap, ad-hoc scripts, and raw outputs with no guaranteed relation to the current working tree.

Current audits belong in `docs/audit/latest/` and are generated from `tools/audit/`. The shared operating rules are in `docs/AGENT_WORKSPACE.md`.

## Imported conclusions worth preserving

The external `DEVELOPMENT_LOG.md` records these CABAL conclusions:

- CABAL design intent: self-contained content pack, one class-template mapping per unit, meaningful regular units per tech tier, unique gameplay hooks, and workbook-first numeric changes.
- CABAL Core is the Tier 4 unlock and gates Tier 4 units and related support.
- CABAL cyborg dual-armor uses a primary infantry armor plus a secondary armor with a matching 200% `DamageMultiplier`.
- CABAL upgrade tiers, vehicle/infantry roles, and promotion relationships require a workbook-backed rebalance pass before further stat edits.
- Past CABAL work claimed clean menu boots and full-audit runs, but all claims must be revalidated after subsequent repository changes.

The design source was `CABAL_FACTION_DESIGN.md` (in the external DevinCameoProject folder). Its relevant design rules were partially carried into `docs/DESIGN.md`; if CABAL work resumes, reconcile any remaining differences before changing rules.

## External raw-output inventory

| External family | Historical files | Repository replacement | Required action before use |
|---|---|---|---|
| AI | `audit_ai2.txt` | `tools/audit/audit_ai.py` → `docs/audit/latest/ai.md` | Rerun targeted audit. |
| Assets / sequences | `audit_assets_cabal.txt`, `audit_asset_files_cabal.txt`, `audit_sequences2.txt` | `audit_assets.py`, `audit_asset_files.py`, `audit_sequences.py` | Rerun targeted audits. |
| Balance / formulas / power | `audit_balance_*`, `audit_power_*`, `audit_stat_formulas.txt` | `audit_balance_sheet.py`, `audit_power_budget.py`, `audit_stat_formulas.py` | Rerun after checking workbook lock state. |
| Fluent / metadata | `audit_fluent_*`, `audit_metadata_cabal.txt` | `audit_fluent.py`, `audit_metadata.py` | Rerun targeted audits. |
| Inheritance / orphan refs | `audit_inherits_cabal.txt`, `audit_orphans.txt`, `cabal_orphan_*.txt` | `audit_inherits.py`, `audit_orphans.py`, `audit_faction_leaks.py` | Rerun targeted audits. |
| Upgrades / weapons | `audit_upgrade_*`, `audit_weapon_*`, `cabal_weapons_*.txt` | `audit_upgrades.py`, `audit_upgrade_coverage.py`, `audit_weapon_uniqueness.py` | Rerun targeted audits. |
| Ad-hoc CABAL scripts | `*.py` outside the repo | No automatic replacement | Review one-by-one; migrate only reusable, validated logic into `tools/audit/` or a documented tool. |

## Deliberately not moved

- Raw external `.txt` reports remain in their original folder to avoid pretending they are generated against this checkout.
- One-off mutation scripts remain external until reviewed; copying them into the repository would make stale behavior look supported.
- `ROADMAP.md` in the external folder is superseded by `docs/design/ROADMAP.md`. It must not be updated as a parallel queue.

## Migration rule

When a historical result is needed, rerun the repository audit that replaces it, add the result to `docs/audit/latest/`, and then update `docs/audit/SUMMARY.md`, `docs/audit/FINDINGS.md`, or `docs/design/ROADMAP.md` with only the verified conclusion.
