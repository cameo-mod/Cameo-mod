# tools/audit — the Cameo audit suite

Implements the detector specifications from `docs/MASTER_REPORT.md`
Appendix A. Every script reads the live ruleset (mod.yaml include graph,
merged + inheritance-resolved) and prints a markdown report to stdout.

## Running

```sh
tools/audit/run_all.sh              # full suite -> docs/audit/latest/
python tools/audit/audit_ai.py      # any single audit
python tools/audit/dump_resolved.py --faction cabal > before.json
```

Blocking-severity findings (dangling references and similar crash-class
issues) make the owning script — and run_all.sh — exit non-zero, so the
suite can gate CI.

## Scripts

| script | bug class | checks |
|---|---|---|
| `audit_inherits.py` | B2 | concrete/cross-faction/dangling/deep inherits, removal abuse |
| `audit_faction_leaks.py` | B1 | buildables owned by another faction; cross-faction concrete inherits in rosters |
| `audit_upgrades.py` | B3 | inverted multiplier directions, dead upgrades, dead wiring (needs `docs/design/upgrades_intent.yaml`) |
| `audit_upgrade_coverage.py` | B4 | roster-wide upgrade coverage gaps |
| `audit_ai.py` | B5 | ai.yaml refs to nonexistent/unloaded actors; unwired combat units |
| `audit_sequences.py` | B6 | missing render images/sequences; orphaned sequence images |
| `audit_metadata.py` | B7 | duplicate/missing tooltips per faction |
| `audit_outliers.py` | B9 | robust-z numeric drift per (trait, field); bounds hard screen |
| `audit_orphans.py` | B10 | orphan weapons; dangling weapon refs; dead conditions |
| `audit_assets.py` | B11 | PNG budget; WAV mono/16-bit/22050 Hz norm |
| `audit_fluent.py` | B12 | unresolved fluent refs; orphaned actor-* keys; coverage |
| `audit_power_budget.py` | R2 | worst-case stacked multipliers > 2.0× |
| `audit_stat_formulas.py` | house rules | HpPerStep=HP/20, SelfHeal=HP/2500 (inf /1000), shield regen=2×heal, defense vision=weapon range + DetectCloaked=range/2 + power=-cost/20, vehicle TurnSpeed=Speed/5 (turretless 2×, artillery Archer firing-slow), AA defense gated by radar tier + advanced defense by tech tier |
| `gen_faction_matrix.py` | §5.1 | regenerates `docs/factions/MATRIX.md` |
| `gen_damage_matrix.py` | §8.1 | armor classes + Versus aggregates |
| `gen_rename_maps.py` | §9.1 | naming compliance; writes `tools/rename/rename_map_<faction>.yaml` |
| `dump_resolved.py` | §10.4 | canonical resolved-ruleset JSON (refactor safety net) |

Shared infrastructure: `miniyaml.py` (parser/merger/inheritance resolver),
`cameo_model.py` (faction registry, prerequisite-closure rosters, ownership
attribution, unit typing), `report.py` (markdown helpers).

## Parser validation

`miniyaml.py` was validated against engine behavior two ways:

1. `make.cmd test` (`utility --check-yaml`) passing on the same tree the
   loader reports as reference-clean (0 dangling inherits, 1 dangling
   weapon ref in unreachable content).
2. Spot checks of resolved actors against hand-verified ground truth
   (inheritance chains, `-Trait` removals, same-actor block re-opening,
   `Inherits@` mixin merging: `tsobl2`, `tsttnkcabal`, `tsshotmut`,
   `tsprobe`, `TSNTHAND2`).

Known approximations (fine for auditing, not for shipping a game):

- indentation is compared by raw leading-whitespace length, not tab stops;
- `Inherits` nodes are expanded at their document position, then later
  same-key nodes merge over earlier ones (matches observed engine behavior);
- faction ownership before the §12 Phase-1 folder migration is heuristic
  (ContentPack folder, `~fact.X` gates, conyard gate tokens) — shared actors
  report as "needs human decision" rather than guessed.
