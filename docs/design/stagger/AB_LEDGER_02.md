# AB_LEDGER_02 (STAGGER-06 step 5, class A and B rows of TIMER_TABLE_06; batch AB-2)

Owner: Frontrunner. Reviewer: Haiku. Branch `frontrunner/stagger-06-ab`, on AB-1 (253e69621). Same rules as AB_LEDGER_01: static, unbuilt; N=1 byte-identical; no BotRng draw in any row here; a row whose N=1 parity cannot be proven keeps its counted form with a reason; TIMER_TABLE_06 is not edited. Files touched: `UnitBuilderBotModuleCA.cs` (Sonnet's P work B048 is on origin; B044 and B045 are mine), `BotSituation.cs` (Sonnet's D010 and D011 are on origin), plus the test `BotCallPeriodTest.cs` (mine, from AB-1). Evidence column: S = I read the site, T = from the table note.

## 1. Converted

| Row | Site | What changed | Why it is identical at N=1 |
|---|---|---|---|
| B044 (class B, R2 freeze) | UnitBuilderBotModuleCA.cs `IBotTick.BotTick`: `ticks++; if (ticks % (FeedbackTime + Info.UnitBuilderInterval) == 0)` after the two pause returns | `ticks` is replaced by `BotCallPeriod buildCalls`, called at the same place, K = FeedbackTime + UnitBuilderInterval (30 with the default 0). It counts only the calls that pass both pause guards (the old `ticks++`); a call under a foreign pause or a base-builder pause returns before it, so the gap to the next counted call is frozen time and moves the due later (R2). Not saved: `ticks` restarted at 0 after a load and a default struct re-arms | Test `N1_UnitBuilderPauseStateMachine_MatchesTheOldCounters`: the old two counters and the two structs stepped over a three-state per-tick pattern (free, base-builder pause, foreign pause; runs of 1 to 120 ticks), three seeds, K in 30, 35, 40: identical firing ticks for both counters |
| B045 (class B, R2 freeze) | same method: `if (++openingDefenseTicks % K == 0) TryBuildOpeningDefense(bot);` inside the base-builder pause, `openingDefenseTicks = 0;` on a free call | `openingDefenseTicks` is replaced by `BotCallPeriod openingDefenseCalls`; the pause branch calls it, the free path sets it to `default` (re-arm = the old reset to 0). A foreign-pause call counts for neither, so it is frozen time inside an episode (the table's FREEZE note) | same test |
| D012 (class B) | BotSituation.cs `IBotTick.BotTick`: `brownoutTicks++` when `ExcessPower < 0` | `brownoutTicks += max(1, Elapsed)` (the ticks since this bot's previous visit, the current observation taken as true over the gap) | `Elapsed` is 1 at N=1 and for an ungated bot (`BotVisitClock.Of`, BotTimers.cs:35). The field is published as `BrownoutTicks` and read only by AiSituationLogWriter (telemetry); not saved (BotSituation.cs comment on the counters: a load restarts at 0) |
| D013 (class B) | same: `idleProductionTicks++` per enabled queue with no current item and nothing queued | `idleProductionTicks += max(1, Elapsed)` per such queue | same |

N>1 (declared class B difference): D012 and D013 treat the state seen at a visit as true for the whole gap; the history between visits is unobserved (T2/T5). B044 and B045: the period stays K world ticks (30 or more, at least N_max=12, so a due is never coalesced) and each firing is late by at most N-1 ticks; an episode shorter than N ticks is invisible (a free call between two visits is unobserved).

## 2. Retained, no code change

| Row | Site | Why no conversion | Ev |
|---|---|---|---|
| A077 | SquadManagerBotModuleCA.Stalemate.cs:841-844 (`damaged` is true when `seat.WindowDeaths > 0` or `status.Hp < seat.TargetLastHp`, then `seat.TargetLastHp = status.Hp; seat.WindowDeaths = 0;`) | The window values are FLAGS, not sums: any death since the last sample, or a lower HP than at the last sample. No Elapsed weighting is meaningful. The sample itself sits on a strict grid: `ObserveSample` requires `IsSampleTick(t)` and `t == LastSampleTick + SampleInterval` (StalemateBreakerWaveCA.cs:260-266) or the wave ends MissingEvidence, so the window length is the nominal SampleInterval whatever N is, and the grid rows (A066, A072, A074, R5, Sonnet's) decide the visit that records it. Declared class B difference: a window can include up to N-1 extra ticks | S |
| A035 | SquadManagerBotModuleCA.cs, the squad pass inside the `attackForceTicks.TryFire` block (`var start = squadCursor % Squads.Count`; table :3061 at the audit base) | A fairness CURSOR over the squads advanced once per attack-force run, not a clock and not a decay. The run itself is Sonnet's P row (A004 form), so its unit becomes per run, as the table says. Nothing to scale | S |
| E004 | ModularBot.cs:329 at the audit base (:500 at the merged tip, Haiku C9FB92B1) (`moduleTiming[t] = (acc.Ticks + moduleStopwatch.ElapsedTicks, acc.Count + 1)`) | A DEV timing statistic: Stopwatch ticks are wall clock, not world ticks, and `Count` counts calls (per visit under gating, which is the unit the log should read). R6 satisfied as is (the table's ruling). Sonnet owns ModularBot.cs and I made no edit | S |
| E028 | BotOwnershipWatchdog.cs:102 | Log-only count of passes (FullStack's file; the table note: sub-class C call statistic). Decided with FullStack's batch for that file | T |

## 3. Still to do

- FullStack-shared files, after FullStack's C rows for each file are on origin: A096 (GroundStatesCA), A113 (NavyStatesCA), A114 (ProtectionStatesCA), B068 (Harvester), E011 (ArmyStaging, R8 loop), E037 and E038 (BuildOrderKnobsEval), E114 (SquadDesire), E028.
- Already decided in AB_LEDGER_01: C001 and C002 converted; 26 row IDs retained on 18 table lines (E040 and A025 carry erratum candidates; the earlier "18" is withdrawn, Haiku CE7B8481 item 5).
- F014 is an `engine/` row: out of scope by Opus.
