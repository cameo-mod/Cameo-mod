# M13 tempo arms (BV_m13_liveness) — 2026-10-12

Three default-off increments on one existing switch group, `BV_m13_liveness` (restraint). Nothing here runs
unless that switch arms it in a frozen A/B worktree; master is bit-identical with it off. Classic never sees
any of it: `SquadManagerBotModuleCA@classic` is on the switch spec's skip list and the shared
`UnitBuilderBotModuleCA@generic` instance carries `SwitchCondition: genericbot`, so its switch fields are inert
there. Fransbot uses `FransUnitBuilderBotModule` and is untouched.

All knobs live on the difficulty line as `BotLimits@<tier>` fields (written in every tier, equal-step lines,
`tools/audit/audit_ai_personalities.py` `difficulty_scale_failures` / `tools/tests/test_m13_tier_knobs.py` pin
them). All class defaults are 0/disabled.

## Knobs and tier tables

| Tier | `BankSaturationCash` | `AttackLivenessFirstWaveMaxTicks` | `TempoMinWaveUnits` |
|---|---|---|---|
| easiest | 4200 | 4750 | 4 |
| veryeasy | 3900 | 4500 | 5 |
| easy | 3600 | 4250 | 6 |
| medium | 3300 | 4000 | 7 |
| hard | 3000 | 3750 | 8 |
| veryhard | 2700 | 3500 | 9 |
| brutal | 2400 | 3250 | 10 |
| challenger | 2100 | 3000 | 11 |
| unbeatable | 1800 | 2750 | 12 |
| god (cameogod) | 1500 | 2500 | 13 |

`AttackLivenessFirstWaveMaxTicks` is exactly `AttackLivenessMaxNoLaunchTicks / 2` per tier. `TempoMinWaveUnits`
steps +1 per tier from 4. `BankSaturationCash` is one equal-step line (−300/step, hard 3000 → god 1500), floored
well above `ProductionMinCashRequirement` (1000). Note for the spec author: the brief's asymmetric step
(+500 easier / −250 harder) cannot sit on one equal-step line with both stated endpoints (hard 3000, god 1500) —
the table above is the single line through both endpoints.

## (1) Bank saturation — `ProductionSaturationCA` + `UnitBuilderBotModuleCA`

Arm: switch sets `UnitBuilderBotModuleCA.BankSaturationCash: 1` on `@generic` (flag only); the bar is the tier's
`BotLimits.BankSaturationCash`. Resolved as `switchesActive && Info.BankSaturationCash > 0 && botLimits != null ?
botLimits.Info.BankSaturationCash : 0`.

When `Cash + Resources >= bar` (re-evaluated per category and per queued slot with the same accounting the loop
already used):

- the `MaximiseProductionCashRequirement` sweep break is skipped — every existing queue category gets filled;
- the per-category fill takes up to **two** free queues instead of `providerWidth ?? 1` (a wider provider wins);
- if `ChooseUnitToBuild` returns null only because the proportional-share test rejects every candidate, one
  fallback pass over the same candidates in key order (no extra RNG) skips just the share test.

NOT bypassed: `ProductionMinCashRequirement` floor, all `IBotRequestPauseUnitProduction` votes
(`BotGlobalUnitBudget` cap, base-builder refinery pause), the 30-tick cadence, empty-queue/slot checks,
`UnitsToBuild` membership, `UnitLimits`, `UnitDelays`/`UnitIntervals` (× tier modifier), composition narrowing,
aircraft caps, `ShouldBuild`.

Overlap: `devin/turtle-fix-2-f1f2` (`131bcf628`) is a default-off unit-spend reservation ledger touching the same
two files (different problem — building spend starving unit spend, while this arm spends the bank faster).
Edits here are localized: one Info field, one resolution property, the category loop, `BuildUnit` width arg,
one fallback block in `ChooseUnitToBuild`. Static merge-tree clean (see devlog).

## (2) First-wave cap — `AttackLivenessEvalCA.IntervalCap`

`Observe` gains optional `firstWaveMaxTicks` (0 = off). While the shared `M13PlayerStateCA.Liveness` clock has
`ObservedOffensiveCount == 0` (no accepted offensive wave yet), a newly eligible interval's deadline is
`eligibleSince + min(FirstWave, NoLaunch)`. It can only shorten at creation: never renews an existing deadline,
never exceeds `NoLaunch`, dispatch-lead and restore are untouched, `TryRestore` stays fail-closed (restored
deadlines are data, not recomputed). After the first accepted wave every interval uses the full tier cap.

## (3) Min wave floor — `AttackForceEvalCA.RequiredSize`

When `Info.AttackLivenessEnabled` is armed, the ordinary launch gate uses
`requiredSize' = max(requiredSize, tier.TempoMinWaveUnits)` — the count arm of `ShouldLaunch` only.
`MaxIdleUnits` overflow, the value arm, `ValueOnlyAttackLaunch`, and the M13 urgent/deadline dispatch
(`M13LaunchPolicyCA.Urgent`/`TryCommit`) do not see the floor: a smaller force still launches at the deadline
(pinned by `M13TempoTest.MinWaveFloorNeverDefersTheLivenessDeadline`). The draft itself is still the whole
eligible idle assault roster.
