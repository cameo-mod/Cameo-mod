# Audit summary — current known-issue state

_One page. Live reports: [`latest/`](latest/) · comparison snapshots: [`baseline/`](baseline/) ·
faction map: [`../factions/MATRIX.md`](../factions/MATRIX.md)._

**Evidence date: 2026-08-23**, from `bash tools/audit/run_all.sh` at `e60aab63`, with
`doc_claims` and `gen_sync` re-measured at `519175ae` (both read only tracked files, so they are
trustworthy from any checkout). `level_ladder` was RETIRED on 2026-08-23 — it enforced a
damage-monotonic rule no law states — and replaced by `heaviness_bell`.
Recurring code-health audits and their cadence: [`PERIODIC.md`](PERIODIC.md) +
[`periodic.json`](periodic.json).

⚠ **`latest/` is currently a MIXTURE of two environments and is owed one clean regenerate.**
A dozen audits read `engine/` C# or full git history — neither of which exists in a fresh
clone — and they respond by reporting *less* and still saying PASS (`dead_warhead_fields` 27071
warhead nodes → 7014, `fluent` 5235 messages → 3640). Alternating Windows and container runs
have been overwriting each other's numbers. `run_all` now refuses to write `latest/` from an
incomplete tree (it diverts to the untracked `docs/audit/degraded/`; `--force-latest`
overrides), so this is a one-time cleanup: **run the suite once on a complete tree and commit
the result whole.**

> **How to use this page.** Every number is a count from a report in `latest/`, named in the
> "report" column. If a number here disagrees with that report, **the report wins** — re-run the
> suite and fix this page in the same commit. Never hand-edit a count in without re-running.

```sh
bash tools/audit/run_all.sh          # regenerates every report in latest/ (UTF-8 enforced)
python tools/audit/audit_<name>.py   # one audit, straight to stdout
```

⚠ **Never regenerate reports with a PowerShell `>` redirect** — it writes UTF-16 and corrupts
the file (CLAUDE.md rule 8).

---

## AI personality wiring

`audit_ai_personalities.py` verifies that the five personality-gated
`SquadManagerBotModuleCA` instances retain byte-identical shared fields and
that their consumed conditions exactly match the `GrantRandomCondition`
selector. Personality-specific differences are restricted to an explicit
tuning allow-list.

The implementation removes the stale `RushInterval` and
`RushAttackScanRadius` keys; neither exists in the vendored CA or pinned engine
SquadManager implementation. Steamroller is intentionally documented as
having at most one harasser because the engine always creates the first
guerrilla squad and YAML cannot express zero guerrilla units.

The reusable `ObserverConditionNotification` trait announces each selected
personality once in the chat feed for spectators and replay viewers after its
condition activates. Live players are intentionally excluded so the indicator
does not leak opponent strategy; no live-player UI decoration is intended.

The five personality managers now use optional time-scaled squad-value
thresholds, preserving their early-game flat-bonus values. Other squad-manager
instances retain the flat `SquadValueRandomBonus` path. The ramp and the
actor-value cache have not been observed in a long match; that is an in-game
verification follow-up.

The unit-builder composition consumer is opt-in through `UseCompositions`.
Without an active composition, each personality's `UnitsToBuild` table remains
the fallback. The pilot compositions are limited to TD vehicle queues and are
gated by their respective tech prerequisites; broader composition coverage is
still a follow-up. Explicit unit requests continue to bypass composition
shares.

## ⛔ Known blind spot: O3 dead conditions are counted MOD-GLOBALLY (2026-09-01)

`audit_orphans.py` O3 reports conditions granted-never-consumed and consumed-never-granted across
the **whole mod**. Conditions in OpenRA are **per-actor**, so a grant on actor A satisfying nothing
for a consumer on actor B is dead wiring O3 cannot see. Its own docstring says the check is
approximate (`audit_orphans.py:10-11`); this is the concrete shape of that approximation.

**A live instance, found 2026-09-01 by reading yaml rather than by any audit:** the bot
passive-income ladder on `^AIConyardCash` (`defaults.yaml:6712`) gates its four lowest rungs on
`normalbot`, which `^AIDifficulties` never grants — the mod's only `normalbot` grant is on a Dark
Reign building. `medium` bots therefore get **zero** insurance income while `easy` gets 3 rungs and
`hard` gets 5. O3 stayed silent because `normalbot` is both granted and consumed *somewhere*.
Details and the fix: [`../design/ROADMAP.md`](../design/ROADMAP.md) "medium BOTS GET ZERO INSURANCE
INCOME"; the count is pinned as `bot_insurance_unreachable_difficulties` in `doc_claims.yaml`.

⭐ **Closed for this ladder, still open in general.** `audit_bot_insurance.py` (new, in
`run_all.sh`) evaluates each rung's `RequiresCondition` per player kind rather than counting
condition names, and enforces two laws: rung count may never decrease as difficulty rises, and
no difficulty may reach zero rungs. The committed `DynamicBotInsurance` replacement instead
checks that every loaded bot type is covered by its `Difficulties` list.
The general per-actor reachability audit is still queued.

## Counts by bug class

| class | what | count | report |
|---|---|--:|---|
| **B8** | crash-class content | **0** | — |
| B1 | cross-faction leaks | 435 L1 · 20 L3 · 91 shared | `faction_leaks.md` |
| B2 | illegal inherits | 281 V1 · **0** V2 · **0** V3 dangling · 1863 V4 depth>3 · 95 V5 | `inherits.md` |
| B2b | duplicate inherit paths | 1770 definitions reach a parent by >1 path | `duplicate_inherits.md` |
| B3 | upgrade direction | 594 items · 103 inverted · **0** dead · 19 dead tokens · 568 without an intent entry | `upgrades.md` |
| B4 | upgrade coverage | 23 tagged upgrades · 21 uncovered unit slots | `upgrade_coverage.md` |
| B5 | AI wiring | 1801 refs · **0** defined nowhere · **0** unloaded · **0** unwired pool factions | `ai.md` |
| B6 | art/sequence refs | 2 missing images · **0** missing sequences · 595 unreferenced images | `sequences.md` |
| B7 | metadata rot | 32 duplicate-tooltip groups · **0** missing tooltip names | `metadata.md` |
| B9 | numeric drift | 176 robust outliers · **0** bounds over the 5×5 max | `outliers.md` |
| B10 | dead content | 374 orphan weapons · **0** dangling refs · 15 dead conditions | `orphans.md` |
| B11 | asset norms | 148 / 2006 PNGs over budget · 1817 / 4390 WAVs off-norm | `assets.md` |
| B12 | localization | 1 unresolved fluent ref · 526 orphaned `actor-*` messages | `fluent.md` |
| R2 | stacked multipliers | 790 units over the 2.0× power budget | `power_budget.md` |
| W | weapon uniqueness (§10) | 34 same-faction · 34 cross-faction · 95 carrier-only | `weapon_uniqueness.md` |
| G | garrison weapons (§11) | **6 G1** · 0 G2 · 0 G3 | `garrison_weapons.md` |
| F | house stat formulas | 615 violations across 1910 roster actors | `stat_formulas.md` |
| E | elite / rank wiring | 197 missing elite armaments · 21 ungated ELITE blocks · 52 decoration issues | `missing_elite.md`, `elite_gating.md`, `rank_decoration.md` |
| Q | build order | prerequisite-order violations across 841 buildables | `buildable_order.md` |
| D | duplicate keys | **88 D1 dropped inherits** · 439 D2 merged duplicates | `duplicate_keys.md` |
| C | infantry class bands (FORMULA_V2 §6b) | 29 of 256 units outside their own class's band · 6 with two class templates | `infantry_class_bands.md` (advisory) |

## Green — and must stay green

`empty_warhead` **0** of 2760 weapons (the boot-NRE class) · dangling weapon refs **0** ·
dangling inherit targets **0** · cross-faction concrete inherits **0** · rename-broken sprite
refs **0** · missing voxels **0** · TS death-palette **0** · D2k rank decorations **0** ·
promotion wiring clean · `MinRange` clean · duplicate uniquely-resolved traits clean ·
armor-plating invariants clean · plating exclusivity clean · physical-state warheads PASS ·
cross-document consistency 73/0 · display text 0 active findings ·
**documentation structure 0** (`doc_health.md`, D1–D8) · **balance-ledger drift 0** ·
**generator sync drift 0** (**139** shared templates, no-op regenerate — 139, not 136, since the
2026-08-30 heaviness-bell switch regenerated the set).

⚠ **`doc claims` is NO LONGER 19 of 19.** It read 11 drifted on 2026-08-30; **6 were resolved and
5 remain**, deliberately.

✅ **Resolved.** `ledgers_drifted` (32 → **0**) by a pipeline re-extract — the ledger was behind
the yaml that actually ships, including real values (`missile_tank` hp 47500→50000, speed 95→64).
`signed_off_class_anchors` / `class_anchors_signed_off` (0 → **8**, maintainer-ordered, verified
against `0ff427712`). `multi_main_fired_weapons` (494 → **472** — a burn-DOWN, so lower is
progress). `warhead_family_reach` (1245 → **1391** — a burn-UP, so higher is progress).
`percentage_denominator_unset` (0 → **11**) — that pin was a TRIPWIRE for "W18 shipped" and it had
tripped unnoticed; W18 is an ancestor of HEAD.

⛔ **Left red ON PURPOSE — 5 pins, because re-pinning them is a judgement, not a measurement:**

| pin | pinned | measures | why it is not just re-pinned |
|---|--:|--:|---|
| `shield_versus_mean` | 189.088 | 174.802 | feeds `shield_hp_factor`, which is a PRICING input |
| `shield_hp_factor` | 0.528855 | 0.572075 | derived from the above — what one shield point is worth as HP |
| `shield_damage_share` | 0.01432 | 0.0156 | roster damage landing on the Shield row |
| `physical_state_fired_weapons` | 460 | 509 | scope of the meter layer; direction of good unrecorded |
| `meters_filling_before_death` | 137 | 239 | a dilution symptom — rising may be the DEFECT, not progress |

⭐ **Blanket re-pinning is how a guard gets switched off.** The three `shield_*` numbers move
prices, and `meters_filling_before_death` may be measuring a regression — a pin whose direction of
good is unrecorded must not be quietly dragged to match the tree. Each needs the owner's call, and
the claim updated with every document under its `docs:` list in the same commit.

## Red right now

| check | state | what to do |
|---|---|---|
| **level ladder** | **WARN — 9 broken, at ratchet 9** (7 inverted, 2 flat) | no longer failing: `a9f31258` fixed `Demolition`. Still blocked on a maintainer ruling. Full measured table + the diagnosis: [`../design/ROADMAP.md`](../design/ROADMAP.md) "BROKEN LADDERS". These are balance numbers: pipeline only, and **never raise the ratchet**. |
| **bot insurance** | **PASS — every loaded bot type is insured or deliberately excluded** | `audit_bot_insurance.py` validates the committed `DynamicBotInsurance` coverage list. |
| duplicate keys D1 | 88 dropped inherits | each one silently drops a template — same family as the `Parent type X was already inherited` boot crash |
| warhead-split ratchet | at baseline | pre-existing W24 debt, not a regression; lower the baseline as W24 lands |

Cleared since the last edition of this page: **doc claims** (was 4 of 19 drifted, now 19 of 19
matching) and **generator sync** (was non-zero, now 0).

## Programme-scale debt

Sequenced on the board, not loose bugs. Status and ownership:
[`../design/BALANCE_PROGRAM_PLAN.md`](../design/BALANCE_PROGRAM_PLAN.md); the order is fixed by
its §0a.

| id | debt | measured |
|---|---|--:|
| W24 | directly fired weapons carrying more than one damage main | **494** |
| W23 | fired weapons reaching a `^Warhead_*` family | **1231** |
| W23 | direct inheritors of the legacy weapon templates | **1162** |
| W26 | live `DamageMultiplier` declarations | **353** |
| W11 | class anchors the maintainer has signed off | **0** — so no price is final |

All five are pinned in [`doc_claims.yaml`](doc_claims.yaml) and re-measured on every suite run,
so they cannot rot in prose again.

## Recommended fix order

1. **One clean suite run on a complete tree** — cheapest, and until `latest/` stops mixing two
   environments no count on this page can be fully trusted.
2. **The 9 broken level ladders** — a heavier level dealing less damage than a lighter one is
   player-visible nonsense. Back at the ratchet rather than over it, so it no longer fails the
   suite, but nothing about the nine has been ruled on.
3. **B2b duplicate inherit paths / D1 dropped inherits** — the class that produces
   `Parent type X was already inherited` boot crashes and silently-dropped templates. Only the
   boot and `audit_duplicate_inherits` can see it.
4. **G1 garrison weapons (6)** and **B6 missing images (2)** — small, bounded, player-visible.
5. **B1 cross-faction leaks (435 L1)** — the count grew because the audit's faction coverage
   grew, not only because the tree got worse. Triage before treating it as 435 bugs.
6. **B3/B4 upgrade direction and coverage**, plus transcribing the remaining 568
   `upgrades_intent.yaml` entries so the audit can tell an intended drawback from a bug.
7. **B10/B11 hygiene** — orphan purge, per-directory WAV normalisation. Good batch work.
8. **R2 stacked multipliers (790)** — folds into W26; do not touch it separately.

---

## Standing incident notes

Closed, but each is a bug class the ordinary gates cannot see.

### Empty warhead type = boot NRE (2026-08-04, CLOSED)

A `Warhead@X:` line with **no type value** parses to `null`; `WeaponInfo.LoadWarheads` calls
`Game.CreateObject<IWarhead>(null + "Warhead")`, that resolves to the abstract `Warhead` base
class, and `ObjectCreator.CreateBasic` throws before the main menu. It happens for **every**
top-level weapon node in the resolved ruleset, including unused `^templates`.
**`utility --check-yaml` does not catch this class.** Guard:
`python tools/audit/find_empty_warhead.py`. Run it after any bulk warhead edit.

### Conditional multipliers ignore `Prerequisites:` (2026-08-04, CLOSED)

Every `ConditionalTrait`-based multiplier (`FirepowerMultiplier`, `DamageMultiplier`,
`SpeedMultiplier`, …) has **no `Prerequisites` field**. A `Prerequisites:` line inside such a
block is silently ignored by the loader, making the multiplier **permanently active**. Found via
the War Economy speed bug, then swept mod-wide: 4 fixed, 0 remaining.
`ProductionCostMultiplier` / `ProductionTimeMultiplier` legitimately support it.

### Superweapon documentation audit (2026-07-25, CLOSED)

Every superweapon/support-power trait cross-referenced against `FACTIONS.md`: 14 findings, all
documentation discrepancies, all fixed. The one substantive finding stands: **Harkonnen Palace**
carries `^PrimarySuperweapon` + `SupportPowerChargeBar` but **no power trait** — the Death Hand
Missile is unimplemented (parked faction, not a regression). Superweapons also exist in the WIP
factions (Warzone 2100, Worms, Win98, Warcraft 1, WH40K); document them in `FACTIONS.md` only
when those factions go active.

⚠ The raw cross-reference was written to `latest/superweapon_audit.yaml` and **no longer
exists**: `run_all.sh` regenerates `latest/` wholesale. Put one-off artifacts anywhere else.

### TD GDI release regression (2026-07-17, CLOSED)

See [`INCIDENT_TD_GDI_RELEASE_REGRESSION.md`](INCIDENT_TD_GDI_RELEASE_REGRESSION.md).

### The 2026-07-08 baseline audit

The original long-form findings (the B1–B12 taxonomy, per-class tables) are archived at
[`../history/audits/BASELINE_FINDINGS.md`](../history/audits/BASELINE_FINDINGS.md) and
[`../history/MASTER_REPORT_2026-07-08.md`](../history/MASTER_REPORT_2026-07-08.md). Their file
paths predate the ContentPack restructure and their counts predate everything above — read them
for the taxonomy, never for numbers.
