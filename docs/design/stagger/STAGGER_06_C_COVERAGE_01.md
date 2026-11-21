# STAGGER-06-C coverage report

Date: 2026-11-21
Branch: `fullstack/stagger-06-c`
Coverage: 108 actionable class-C rows; each is listed once below as converted, retained in counted form, or already using the required absolute/elapsed-time semantics. The frozen `TIMER_TABLE_06.md` was not edited.

## Exclusions

- R12 / Fransbot: 14 rows, `C105`–`C118`, excluded by Opus ruling; Fransbot remains unconverted in this landing.
- Engine: 9 rows, `F002`, `F003`, `F004`, `F005`, `F012`, `F013`, `F020`, `F021`, `F022`, excluded from this Cameo-owned batch.

The source table contains 131 class-C rows. 131 minus these 23 exclusions equals the 108 rows covered here.

## Converted: 47 rows

`A011`, `A015`, `A016`, `A041`, `A043`, `A058`, `A059`, `A062`, `A064`, `A103`, `A104`, `B004`, `B006`, `B011`, `B023`, `B024`, `B065`, `B066`, `B067`, `B069`, `B079`, `B080`, `C054`, `C058`, `C070`, `C089`, `C095`, `E008`, `E009`, `E016`, `E023`, `E025`, `E032`, `E035`, `E039`, `E048`, `E054`, `E055`, `E056`, `E069`, `E084`, `E103`, `E108`, `E109`, `E112`, `E127`, `E128`.

Changes use `BotCooldown`, `BotPeriod`, or the row-specific absolute-tick/nominal-grid implementation. Reloads remain anchored to the actual event/visit where required; no catch-up was added. BotRng draw order was kept in the reviewed conversion batches. `B004/B006/B023/B024` additionally preserve remaining sweep waits across condition-disabled gaps. `E127/E128` retain the nominal grid anchor while validating observations against actual timestamps.

## Retained in counted form: 26 rows

`A005`, `A088`, `A089`, `A090`, `A091`, `A092`, `A093`, `A094`, `A095`, `A097`, `A099`, `A107`, `A112`, `B016`, `B022`, `B025`, `B026`, `B027`, `B028`, `B029`, `B031`, `B032`, `B042`, `B043`, `D058`, `D059`.

These retain processed-update semantics because exact N=1 parity depends on the visits/observations that actually occurred, or on freeze/attention-denial accounting that cannot be represented by a simple world-tick deadline:

- `A005` — urgent dispatch skips the decrement; opening readiness extends the remaining budget; save/load stores remaining visits. `SquadManagerBotModuleCA.cs:1677,1732,3114-3123,4297,4368-4369`.
- `A088`–`A093` — nested squad-state counters advance only on delivered attack-force updates; attention denial skips those updates. `GroundStatesCA.cs:419,726,750,1687,1804,1815`; `SquadManagerBotModuleCA.cs:3077-3083`.
- `A094` — two counters have separate increment, hold, reset, and escalation branches driven by processed march observations. `MarchEvalCA.cs:39-55`; `GroundStatesCA.cs:777-792`; manager attention denial at `SquadManagerBotModuleCA.cs:3077-3083`.
- `A095` — rear-position/HP observations drive the stall count; no observed rear or skipped update cannot be credited as elapsed stall. `MarchEvalCA.cs:150-166`; `GroundStatesCA.cs:1041-1055`; manager attention denial at `SquadManagerBotModuleCA.cs:3077-3083`.
- `A097` — visible-target visits freeze Backoff; the test checks the counter before decrement, so expiry must follow processed invisible updates. `ProtectionStatesCA.cs:136-145`.
- `A099` and `A112` — Opus ruled these stay processed-update counters; manager attention-denied updates must freeze them. `NavyStatesCA.cs:226,252`; the temporary deadline conversion was reverted at `f951630de22b5db55056734eb5ec1170ef844f4a`.
- `A107` — one EMA step uses the urgency observed by that actual evaluation; replaying the latest urgency across skipped evaluations would alter decisions. `SquadDesireOrders.cs:100`; `SquadDesireBotModule.cs:84-88`; `SquadDesireMemory.cs:32-35`.
- `B016` — cash-persistence accumulation is performed on actual builder-module calls while scaling is enabled. `BaseBuilderBotModuleCA.cs:1886-1888`; `BaseBuilderQueueManagerCA.cs:1852`.
- `B022`, `B023`, `B024`, `B025`–`B029`, `B031`, `B032`, `B042` — queue-manager counters are consumed on manager visits, with early-return/latch branches and a rotating manager cadence. A world deadline would require reproducing the current manager-count cadence plus skipped-visit accounting. The sweep draw remains at `BaseBuilderQueueManagerCA.cs:874`; manager/latch paths are at `:698-823`, `:775-796`, `:825-885`, and `BaseBuilderBotModuleCA.cs:1380-1387`. `B042` is the helper view of the same latch probe as `B025`.
- `B043` — active unit intervals decrement on delivered `UnitBuilderBotModuleCA` ticks; trait-disabled gaps skip the decrement. `UnitBuilderBotModuleCA.cs:413,690-702`.
- `D058`, `D059` — topology counters advance on actual rebuild visits; adoption/rebuild branches can return before the decrement, and moving the checks can change rebuild timing and BotRng draws. `TacticalMapBotModule.cs:381-425,452-465,504-566`.

## Already using absolute-tick or ruled elapsed-time semantics: 35 rows

`A021`, `A067`, `A068`, `A069`, `A123`, `B051`, `B052`, `B054`, `B063`, `D017`, `D018`, `D020`, `D021`, `D032`, `D054`, `D060`, `D061`, `D062`, `D063`, `D065`, `D066`, `D067`, `D068`, `D069`, `D071`, `D072`, `D073`, `D074`, `E058`, `E059`, `E060`, `E062`, `E074`, `E100`, `E115`.

No conversion was needed because these already store/compare world ticks, use the ruled any-tick window, or are covered by the surrounding absolute-tick snapshot cadence:

- `A021` — explicit Opus ruling: keep long absolute deadlines and `M13Cooldowns` save/load unchanged; do not narrow serialized values.
- `A067`, `A068`, `A123` — `NextAttemptTick`/`CooldownUntil` are absolute tick values assigned at the event and checked lazily; the existing `StalemateEndWave` cadence is unchanged.
- `A069` — ruled R4 any-tick window; leave its residue-window selection unchanged.
- `B051`, `B052`, `B054`, `B063` — event/sample stamps are already world-tick values and are compared against the current world tick.
- `D017`, `D018`, `D020`, `D021`, `D032`, `D054` — existing world-tick decision/role stamps; snapshot work is already behind the 150-tick `BotSituation.Rebuild` cadence where applicable.
- `D060`–`D063`, `D065`–`D069`, `D071` — already absolute world-tick refresh/rebuild deadlines (R1 satisfied as-is).
- `D072`–`D074` — event reloads already store world ticks and are checked lazily.
- `E058`, `E059`, `E060`, `E062`, `E074`, `E100` — posture, base refresh, quiet-window, sample, and radar refresh state already use world-tick stamps/deadlines.
- `E115` — keep the live per-call dwell argument and widened `long` elapsed subtraction; `SquadDesireMemory.Evaluate` already compares absolute world ticks. Reviewed at `af462374a87e590b3d4a74294857a7fcaa606fb2`.

## Verification and open gates

Static review only. Sol's individual review pins are recorded in the commits/messages for each converted batch; the whole-branch review is requested against `b6a6e0d24`. Haiku's independent coverage matrix is pending. No build, test, or game was run because the perf-window freeze remains in force; runtime callback ordering remains unverified.
