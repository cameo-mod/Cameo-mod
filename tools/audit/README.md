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
| `audit_ai_personalities.py` | AI wiring | personality selector/consumer condition parity and byte-identical shared squad-manager fields |
| `audit_sequences.py` | B6 | missing render images/sequences; orphaned sequence images |
| `audit_metadata.py` | B7 | duplicate/missing tooltips per faction |
| `audit_outliers.py` | B9 | robust-z numeric drift per (trait, field); bounds hard screen |
| `audit_orphans.py` | B10 | orphan weapons; dangling weapon refs; dead conditions |
| `audit_assets.py` | B11 | PNG budget; WAV mono/16-bit/22050 Hz norm |
| `audit_fluent.py` | B12 | unresolved fluent refs; orphaned actor-* keys; coverage |
| `audit_power_budget.py` | R2 | worst-case stacked multipliers > 2.0× |
| `audit_stat_formulas.py` | house rules | HpPerStep=HP/20, SelfHeal=HP/2500 (inf /1000), shield regen=2×heal, defense vision=weapon range + DetectCloaked=range/2 + power=-cost/20, vehicle TurnSpeed=Speed/5 (turretless 2×, artillery Archer firing-slow), fighter/bomber TurnSpeed=Speed/15, AA defense gated by radar tier + advanced defense by tech tier, StartingUnits existence + light(~2000)/heavy(~10000) composition at 5:1 inf:veh, AA weapons must have Air-capable damage warheads |
| `audit_weapon_uniqueness.py` | §10 | actors sharing the same weapon (violates per-actor weapon ownership) |
| `audit_garrison_weapons.py` | §11 | garrisonable actors missing garrison weapon overrides |
| `audit_asset_files.py` | §1, §8 | asset filenames not matching actor id convention |
| `audit_promotion_gating.py` | §15 | promotion units not strictly stronger than base |
| `audit_min_range.py` | §3 | weapons with range below minimum threshold |
| `audit_basebuilder_crates.py` | B5 | crate action references to nonexistent actors |
| `audit_buildable_order.py` | §5 | build palette ordering and tech tier inference |
| `audit_display_text.py` | B7 | display names containing raw actor IDs or stale references |
| `audit_rename_safety.py` | §1 | pre-rename safety checks (shared assets, voice sets) |
| `audit_missing_elite.py` | §16.3 | RA2-styled actors (`^GainsExperienceRA2`) missing elite armaments |
| `audit_elite_gating.py` | §16.3 | elite armaments missing `RequiresCondition: rank-elite` |
| `audit_rank_decoration.py` | §16.2 | `^GainsExperienceTD` actors missing/wrong `^*RankDecoration` |
| `audit_dune_rank_decoration.py` | §16.2 | D2k actors specifically missing `^DuneRankDecoration` |
| `audit_effect_warhead_names.py` | §8 | CreateEffect warhead naming violations |
| `audit_nuclear_flash_bindings.py` | visual regression | active RA1, Ixian, and CABAL launchers retain the directional flash warhead and approved tuning |
| `audit_empty_warheads.py` | crash | resolved `Warhead*` nodes without a type value (boot NRE in `WeaponInfo.LoadWarheads`); run after bulk warhead/weapon edits |
| `audit_weapon_suffixes.py` | §1 | weapon suffix conventions: `_elite`, `_EMP`, `_AA` |
| `audit_balance_sheet.py` | §12 | cross-reference cameo_armor_system.xlsx vs in-game stats |
| `audit_createeffect_image.py` *(in tools/)* | §8 | CreateEffect warheads carrying explicit `Image:` field |
| `audit_ce_image_usage.py` *(in tools/)* | §8 | classifies CE-only vs shared images |
| `audit_consistency_report.py` | meta | verifies fixes from `docs/audit/CONSISTENCY_REPORT.md` are not regressed |
| `gen_faction_matrix.py` | §2 | regenerates `docs/factions/MATRIX.md` |
| `gen_damage_matrix.py` | §8, ARMOR_SYSTEM.md | armor classes + Versus aggregates |
| `gen_rename_maps.py` | §1 | naming compliance; writes `tools/rename/rename_map_<faction>.yaml` |
| `dump_resolved.py` | §10 | canonical resolved-ruleset JSON (refactor safety net) |

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
