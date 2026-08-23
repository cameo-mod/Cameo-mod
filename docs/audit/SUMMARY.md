# Audit summary — current known-issue state

_One page, regenerated from evidence. Details: [`FINDINGS.md`](FINDINGS.md) · live reports:
[`latest/`](latest/) · comparison snapshots: [`baseline/`](baseline/) · faction map:
[`../factions/MATRIX.md`](../factions/MATRIX.md)._

**Evidence date: 2026-08-23**, from `bash tools/audit/run_all.sh` at `b888cb3f`.
Recurring code-health audits and their cadence: [`PERIODIC.md`](PERIODIC.md) +
[`periodic.json`](periodic.json).

> **How to use this page.** Every number here is a count from a report in `latest/`, named in
> the "report" column. If a number here disagrees with that report, **the report wins** —
> re-run the suite and fix this page in the same commit. Do not hand-edit a count into this
> table without re-running its audit.

```sh
bash tools/audit/run_all.sh          # regenerates every report in latest/ (UTF-8 enforced)
python tools/audit/audit_<name>.py   # one audit, straight to stdout
```

⚠ **Never regenerate reports with a PowerShell `>` redirect** — it writes UTF-16 and corrupts
the file (CLAUDE.md rule 8). `run_all.sh` forces `PYTHONIOENCODING=utf-8`; `run_all.py` is a
faithful Python port for shells without `sh` and reads its audit list out of `run_all.sh`.

---

## Counts by bug class

| class | what | count (2026-08-23) | report | severity |
|---|---|--:|---|---|
| **B8** | crash-class content | **0** | — | crash |
| B1 | cross-faction leaks | 435 L1 · 20 L3 · 91 shared/unattributed | `faction_leaks.md` | balance |
| B2 | illegal inherits | 281 V1 concrete→concrete · **0 V2** · **0 V3 dangling** · 1863 V4 depth>3 · 95 V5 | `inherits.md` | balance-risk |
| B2b | duplicate inherit paths | 1770 definitions reach a parent by >1 path | `duplicate_inherits.md` | crash-risk |
| B3 | upgrade direction | 594 upgrade items · 103 inverted-direction · **0 dead upgrades** · 19 dead wiring tokens · 568 without an intent entry | `upgrades.md` | balance |
| B4 | upgrade coverage | 23 coverage-tagged upgrades · 21 uncovered unit slots | `upgrade_coverage.md` | balance |
| B5 | AI wiring | 1801 ids referenced · **0 defined nowhere** · **0 unloaded** · **0 pool factions unwired** | `ai.md` | balance |
| B6 | art/sequence refs | 2 missing images · **0 missing sequences** · 595 unreferenced sequence images | `sequences.md` | cosmetic→crash-risk |
| B7 | metadata rot | 32 duplicate-tooltip groups · **0 buildables missing a tooltip name** | `metadata.md` | cosmetic |
| B9 | numeric drift | 178 robust outliers · **0 selection bounds over the 5×5 max** | `outliers.md` | balance-minor |
| B10 | dead content | 374 orphan weapons · **0 dangling weapon refs** · 15 granted-never-consumed conditions | `orphans.md` | hygiene |
| B11 | asset norms | 148 / 2006 PNGs over budget · 1817 / 4390 WAVs off-norm | `assets.md` | hygiene |
| B12 | localization | 1 unresolved fluent ref · 526 orphaned `actor-*` messages · 3633 messages loaded | `fluent.md` | cosmetic |
| R2 | stacked multipliers | 790 units over the 2.0× effective-power budget | `power_budget.md` | balance |
| W | weapon uniqueness (DESIGN §10) | 34 same-faction · 34 cross-faction · 95 carrier-only (informational) | `weapon_uniqueness.md` | design/identity |
| G | garrison weapons (DESIGN §11) | **6 G1** armed garrison-capable infantry without a garrison weapon · 0 G2 · 0 G3 | `garrison_weapons.md` | balance |
| F | house stat formulas | 785 violations across 1910 roster actors | `stat_formulas.md` | balance |
| E | elite / rank wiring | 197 actors with `^GainsExperienceRA2` and no `Armament@*ELITE*` · 21 ELITE blocks without the rank gate · 52 rank-decoration issues | `missing_elite.md`, `elite_gating.md`, `rank_decoration.md` | design |
| Q | build order | 47 prerequisite-order violations across 841 buildable combat actors | `buildable_order.md` | UX |

### Gates that are GREEN and must stay green

| check | state | report |
|---|---|---|
| empty warhead types (boot NRE) | **0** of 2680 weapons | `empty_warhead.md` |
| dangling weapon references | **0** | `orphans.md` |
| dangling inherit targets | **0** | `inherits.md` |
| cross-faction concrete inherits (V2) | **0** | `inherits.md` |
| rename-broken sprite refs / missing voxels | **0** / **0** | `asset_files.md` |
| TS death-palette mismatches | **0** | `ts_death_palette.md` |
| D2k rank decorations | **0** missing | `dune_rank_decoration.md` |
| promotion wiring | clean | `promotion_gating.md` |
| `MinRange` vs `Range/5` | clean | `min_range.md` |
| duplicate uniquely-resolved traits | clean | `unique_traits.md` |
| armor-plating invariants (I1 gaps, upgrade-harm) | clean | `armor_upgrade_harm.md` |
| plating exclusivity | clean | `plating_exclusivity.md` |
| physical-state warheads | PASS | `physical_state_warheads.md` |
| cross-document consistency | 73 passed / **0 failed** | `consistency_report.md` |
| display text | 0 active findings | `display_text.md` |
| documentation structure | **0** control chars / mojibake / broken links / broken anchors / stale doc refs / duplicate DESIGN ids | `doc_health.md` |

### Gates that are RED right now

| check | state | report | what to do |
|---|---|---|---|
| **balance-ledger drift** | **9 ledgers drifted** | `balance_drift.md` | `python tools/balance/extract_stats.py`, then commit the ledgers. See `docs/HANDOFF.md` §"Do this first". |
| **generator sync** | **drift = 10** of 97 templates: `^Warhead_Sniper_Light` not emitted (accepted) + 9 live `^Warhead_Chem*` disagreements introduced by the 2026-08-20 W24 chemical split | `gen_sync.md` | reconcile `tools/balance/gen_weapon_template.py` with `weapons.yaml`, then restate the expected drift in `BALANCE_PROGRAM_PLAN.md` §3 |
| doc claims | 1 mismatch (`ledgers_drifted`) | `doc_claims.md` | falls out with the ledger fix above |
| warhead-split ratchet | 965 at baseline 965 | `warhead_split.md` | pre-existing W24 debt, not a regression; lower the baseline as W24 lands |
| duplicate keys | 89 D1 dropped inherits · 439 D2 merged duplicates | `duplicate_keys.md` | D1 silently drops a template — triage before it bites |

---

## Program-scale debt (tracked on the board, not here)

These are large, deliberately-sequenced programs, not loose bugs. Status and ownership live in
[`../design/BALANCE_PROGRAM_PLAN.md`](../design/BALANCE_PROGRAM_PLAN.md); the order is fixed by
its §0a.

| id | debt | measured 2026-08-23 |
|---|---|--:|
| W24 | fired weapons carrying more than one damage main | **951 of 1497 (63.5%)** |
| W23 | fired weapons reaching a `^Warhead_*` family | **1221 of 1622 (75.3%)** |
| W23 | direct inheritors of the 47 legacy weapon templates | **1238** |
| W26 | live `DamageMultiplier` declarations | **366** |
| W11 | class anchors the maintainer has signed off | **0** — so no price is final |

All five are pinned in [`doc_claims.yaml`](doc_claims.yaml) and re-measured by
`audit_doc_claims.py` on every suite run, so they cannot rot in prose again.

---

## Recommended fix order

1. **Ledger drift** (one command) — it is the only RED gate with a trivial fix, and it blocks
   trusting any balance number until it is clean.
2. **B2b duplicate inherit paths / D1 dropped inherits** — this is the class that produces
   `Parent type X was already inherited` boot crashes and silently-dropped templates. Nothing
   but the boot and `audit_duplicate_inherits` can see it.
3. **B1 cross-faction leaks (435 L1)** — the number grew because the audit's faction coverage
   grew, not (only) because the tree got worse; triage before treating it as 435 bugs.
4. **G1 garrison weapons (6)** and **B6 missing images (2)** — small, bounded, player-visible.
5. **B3/B4 upgrade direction + coverage**, and transcribing the remaining 568
   `upgrades_intent.yaml` entries so `audit_upgrades` can tell an intended drawback from a bug.
6. **B10/B11 hygiene** — orphan purge and per-directory WAV normalisation. Good batch work.
7. **R2 stacked multipliers (790)** — folds into W26; do not touch it separately.

---

## Standing incident notes

These are closed, but each one is a bug class the gates could not see. Read before working in
the same area.

### Empty warhead type = boot NRE (2026-08-04, CLOSED)

A `Warhead@X:` line with **no type value** parses to `null`, `WeaponInfo.LoadWarheads` calls
`Game.CreateObject<IWarhead>(null + "Warhead")`, that resolves to the abstract `Warhead` base
class, and `ObjectCreator.CreateBasic` throws before the main menu. It happens for **every**
top-level weapon node in the resolved ruleset, including unused `^templates`.
**`utility --check-yaml` does not catch this class.** Guard:
`python tools/audit/find_empty_warhead.py` (in the suite as `empty_warhead`) — currently 0 of
2680. Run it after any bulk warhead edit.

### Conditional multipliers ignore `Prerequisites:` (2026-08-04, CLOSED)

Every `ConditionalTrait`-based multiplier (`FirepowerMultiplier`, `DamageMultiplier`,
`SpeedMultiplier`, `RangeMultiplier`, `ReloadDelayMultiplier`, …) has **no `Prerequisites`
field**. A `Prerequisites:` line inside such a block is silently ignored by the loader, which
makes the multiplier **permanently active**. Found via the War Economy speed bug, then swept
mod-wide: 4 instances fixed, 0 remaining. `ProductionCostMultiplier` and
`ProductionTimeMultiplier` legitimately support `Prerequisites` and are unaffected.

### Superweapon documentation audit (2026-07-25, CLOSED)

Full cross-reference of every superweapon/support-power trait against `FACTIONS.md`: 14
findings (1 HIGH, 2 MEDIUM, 8 LOW, 3 INFO), all documentation discrepancies, all fixed in
`FACTIONS.md`. The one substantive finding stands: **Harkonnen Palace** carries
`^PrimarySuperweapon` + `SupportPowerChargeBar` but **no power trait** — the Death Hand Missile
is unimplemented (parked faction, not a regression).

Superweapons also exist in the WIP factions (Warzone 2100, Worms, Win98, Warcraft 1, WH40K).
Document them in `FACTIONS.md` only when those factions go active.

⚠ The raw cross-reference was written to `latest/superweapon_audit.yaml` and **no longer
exists**: `run_all.sh` regenerates `latest/` wholesale. Put one-off artifacts anywhere except
`latest/`.

### TD GDI release regression (2026-07-17, CLOSED)

See [`INCIDENT_TD_GDI_RELEASE_REGRESSION.md`](INCIDENT_TD_GDI_RELEASE_REGRESSION.md).
