# AB_LEDGER_01 (STAGGER-06 step 5, class A and B rows of TIMER_TABLE_06; batch AB-1)

Owner: Frontrunner. Reviewer: Haiku (a different model). Branch `frontrunner/stagger-06-ab`, from origin `frontrunner/stagger-06` at 270164ab9. Static only: unbuilt, unrun (Opus: no build). Rules: N=1 byte-identical; BotRng order and count untouched (no row here draws); a row whose N=1 parity cannot be proven keeps its counted form with a reason; no `engine/` rows; TIMER_TABLE_06 (7056CFB7) is frozen and is not edited, so a table statement this ledger finds not to apply is listed as erratum, not changed.

One file, one owner (agreed with Sonnet 2026-11-21): this batch touches only `ExpansionPlannerBotModule.cs` (all its rows are mine), plus two new files (`OpenRA.Mods.CA/BotCallPeriod.cs`, its test) and this ledger.

## 1. Converted in this batch

| Row | Site (stagger-06 HEAD before this batch) | What changed | Why it is identical at N=1 |
|---|---|---|---|
| C001 and C002 (class P, F3 c0=0, K=ReplanTicks) | ExpansionPlannerBotModule.cs `IBotTick.BotTick`: `if (++ticks % Info.ReplanTicks == 0) Idle(...)` on the idle path (table :2826) and `if (++ticks % Info.ReplanTicks != 0) return;` before `Replan` (table :2844). The two shared ONE counter `ticks` (one increment per BotTick call, either path, never reset, not saved) | `ticks` is removed; one `BotCallPeriod replanCalls` (new, OpenRA.Mods.CA) is called once per BotTick call on either path through `ReplanDue(bot)`. It arms on the first call with BotPeriod.F3(v - 1, 0, K), so call K fires exactly as the modulo did; the gap since this module's own previous call minus the bot's visit gap (Elapsed) is frozen time and moves the due back (R2: the old counter did not advance while the trait was disabled and BotTick was not called) | Test `BotCallPeriodTest`: the legacy modulo against the new struct for K in 1, 2, 25, 150, 250: every call; a disabled stretch of ticks 101-150; three fixed pseudo-random disable patterns; identical firing ticks. First call at tick 10 fires at 259 and 509 in both |

N>1 (declared, as for every class P row): the period stays K=250 world ticks (K is at least N_max=12, so no deadline is ever coalesced); each firing is late by at most N-1 ticks; frozen (disabled) time moves the due back by the skipped time (test `Gated_DisabledVisits_MoveTheDueBackByTheSkippedTime`, N=4: a 100-tick gap moves every later firing by exactly 100). A load re-arms on the first call, as `ticks` restarted at 0.

Edge not changed: `K = Info.ReplanTicks` of 0 or less made the old code throw (`% 0`); `BotPeriod.F3` clamps K to 1. The default is 250.

## 2. Retained, no code change (reason, evidence)

Evidence column: S = I read the site; T = from the table's note, not independently read (Haiku may check).

| Row | Site | Why no conversion | Ev |
|---|---|---|---|
| A025 | AttackLivenessEvalCA.cs:185 `ObservedOffensiveCount++` with `LastObservedOffensiveTick = tick` in `ObserveOffensiveActivity` | An EVENT counter (one per accepted offensive wave, deduplicated by `waveId <= lastObservedWave`) and an absolute tick stamp. There is no per-tick or per-call decay at this site, so the table's class A text ("per-tick decay looped Elapsed times") has nothing to apply to. Erratum: the class label for this row is an event counter (class OUT/O), not a decay | S |
| A026 | OffensiveActivityObservationCA.cs:75 (`ReferenceEquals(current, old) && tick > oldTick && position != oldPosition && closer`) | A pairwise comparison of the current sample with the previous one using absolute stamps. No accumulator state, so no Elapsed loop is possible. The only N>1 effect is the sample gap, which is the declared class B difference (decision measured in T2/T5). `++targetCount > 256` is a loop bound (OUT) | S |
| B061 | UnitSpendReservationCA.cs:188 `SettleEntry`: `PaidUnitCost = SaturateAdd(PaidUnitCost, paid)` | An event-driven sum: one add per settled queue entry, no time unit. Weighting by Elapsed would change the amount at N=1. Declared difference: settle instants are staler by up to N-1 ticks | S |
| A098 | ProtectionEpisode.cs:34-35 | R1 exception already in the table (event counter, evidence ProtectionEpisode.cs:34-35) | S (table) |
| D041 | BotFogMemory.cs:360 `tick - record.LastSeenTick > info.ObservationTimeoutTicks` | An expiry test on an absolute stamp, not an accumulator. It runs inside `Rebuild`, which is gated by `BotPeriod.CarryDue(ref nextSnapshotTick, tick, Info.SnapshotInterval)` (BotSituation.cs:1085, K=150): the count of snapshots per game time is the N=1 count while K > N_max-1 (no coalescing), so only the sampling instant moves (by at most N-1) | S |
| D044 | BotInfluenceLayers.cs:230-241 (`EmaToward`, `BlendWithHistory` per snapshot) | A per-snapshot EMA called from `Rebuild` (`influenceLayers.Refresh`, BotSituation.cs:1553); staleness comes from absolute stamps. Step count per game time preserved by the same CARRY at K=150 | S |
| E120, E121, G004 | BotDirector.cs:52-70 `Observe`: `tension += DirectorTensionRisePerSnapshot` or `-= DirectorTensionDecayPerSnapshot`, `idleTicks = tick - lastAttackTick` | Per-snapshot steps (`director.Observe`, BotSituation.cs:1461, inside `Rebuild`); the impatience input is an absolute stamp difference. G004's proposed treatment ("apply the step once per counted snapshot deadline, CARRY counts missed deadlines") is exactly what `CarryDue` does at the caller; nothing more at the accumulator | S |
| E123, G005 | BotUtilityAxes.cs:118-145 `Step(axis, target, decayPercent)` per snapshot | Per-snapshot integer step (`utilityAxes.Observe`, BotSituation.cs:1590, inside `Rebuild`). A loop weighted by Elapsed would run 150 steps per snapshot at N=1 and break identity | S |
| E130 | BotThreatTracker.cs:66-80 `Track`: `dt = g.SeenTick - prev.SeenTick`, velocity = displacement / dt | Already time-based (absolute stamps); called per snapshot (`TrackThreats`, BotSituation.cs:1225, inside `Rebuild`). A longer snapshot gap can lose a match (declared) | S |
| D029, D030 | BotSituation.cs pointer rows to the director and the axes | Same as E120/E121 and E123 | S |
| B047 | AdaptiveCounterProduction.cs:82 | The cadence is kept by CARRY (Sonnet's B046); the step count is preserved, only the sample instant jitters by at most N-1 | T |
| E101 | RadarContactsBotModule.cs:97 | Decision-irrelevant today (phase A has no consumers, per the table note) | T |
| E040 | CombatAnalysisBotModule.cs:414 (`weights[key] = Math.Max(0f, weights[key] * decay)` under `--decayTicks > 0`) | The step is applied ONCE per DecayInterval (1000 ticks), not per tick, so there is no Elapsed loop to apply (table text assumed a per-tick step; erratum). The number of multiplies follows the guard row E039 (class C, FullStack's file), so the rate drift of a RELOAD is that row's declared effect. Revisit only if E039 is converted differently | S |
| C003 | ExpansionPlannerBotModule.cs `income.Enqueue((world.WorldTick, resources.Earned))` per call, window by `WorldTick - Peek().Tick > IncomeWindowTicks` | `CurrentIncomePerTick` divides by the absolute tick span of the window (`span = WorldTick - first.Tick`), so the unit is ticks; at N>1 the window holds fewer samples and its oldest sample can be up to N-1 ticks older (a quantisation of a statistic) | S |
| C043, C044, C045 | ExpansionPlannerBotModule.cs `++NonprogressReplans`, `++claim.Age`, `++ScoutWaitReplans` | Per-replan counters: they step once per `Replan`, and Replan is now the F3 due at K=250, at least N_max, so one step per replan period at every N (the table's own condition "equivalent while Replan is CARRY at K=250 >= N") | S |
| C046, C049, C050, C051, C052 | ExpansionPlannerBotModule.cs frontier shadow and per-replan counters | Same condition as C043 (unit = replan, a per-re-plan counter, in-code comment "a per-re-plan counter" at the frontier freeze) | T |

## 3. Not in this batch (owner order)

- A035 (SquadManagerBotModuleCA.cs), A077 (Stalemate), B044 and B045 (UnitBuilder), D012 and D013 (BotSituation), E004 (ModularBot): next batches, files free (Sonnet's P work is on origin).
- A096, A113, A114 (States), B068 (Harvester), E011 (ArmyStaging), E028 (OwnershipWatchdog), E037 and E038 (BuildOrderKnobsEval), E114 (SquadDesire): FullStack owns C rows in these files; I go after their batch is on origin.
- F014 is an `engine/` row: out of scope by Opus.

## 4. Erratum candidates for the frozen table (not edited)

- A025: the site is an event counter, not a decay (class label).
- E040: the step is per interval, not per tick (R8 text does not apply).
- The seven R2 rows with the "Saved as form R" sentence and saved=no: the saved column binds (already ruled by Opus).
