# TIMER_TABLE_01 auditor flags (companion to TIMER_TABLE_01.md / SAVE_TABLE_01.md)

Source: the six auditors' own risk lists at git 6f1f7dfd6. Each line is a site a reviewer should rule on; ids are the table's. None of these changes a class already in the table; they say where the class is doubtful.

## Needs a ruling (timer form not decidable from code)
- B029/B031/B032 (latch probe, checkForBasesTicks, activeBuildingIntervals): decrement once per `QM.Tick`, which runs once per B_q module calls (BaseBuilderBotModuleCA.cs:877-889). A world-tick RELOAD would change the probe gap (about 3000 ticks at N=1, B_q=2, not 1500).
- B044/B045 (UnitBuilder `ticks % 30`, `openingDefenseTicks`): advance only on unpaused calls and freeze under pause; neither CARRY nor RELOAD reproduces that. Class B.
- B065-B067 (Harvester scans): decrement sits after the `resourceLayer.IsEmpty` return, so class C by the rule of doubt; P only if a freeze while the layer is empty is accepted.
- F002/F006 + two `?` SAVE rows (WaitingPowers): counter freezes while `sp.Disabled`. May expiry pass during Disabled?
- F012/F013 (SendUnitToAttack disguise and assign clocks): run only after `targetPlayer != null`. P with a delayed first due, or C?
- F022 (McvExpansion `conyardRelocationTimeouts`): remaining value is carried to the replacement actor. Absolute expiry or reload?
- A069 (StalemateRotationCA winner by `WorldTick mod seatCount`): a seat visited every N ticks may never win when visited. Class C.

## Equality / exact-grid hazards under a visit gap (need `>=` or a visit-aligned grid)
- A071 (`Stalemate.cs:764`, `Wave.cs:226`): rally deadline hit by `t == RallyDeadline`; a skipped tick ends the wave.
- A072/A073 (`Stalemate.cs:767`, `Wave.cs:264`): breaker wave sample grid anchored at the commit tick C (75-tick grid); an off-grid sample ends the wave as MissingEvidence.
- D026 (`LastSeenTick == tick`): needs one clock value per visit; `BuildRegions` stamps WorldTick directly (BotSituation.cs:2166).
- E091/E092 (OpponentFeatureWindow :81/:96, 750/250): called from ContextFeatureProvider on `tick % 750` / `% 250`; once those residues are carried to visit ticks, a call under K after the last accepted sample is rejected and the gap doubles.

## Read every tick outside the visit (need an absolute `until`, not a per-call counter)
- B064 `respondToAttackCooldown` (per-call decrement, read by the damage callback); F017 `attackrespondcooldown`, F026 `alertedTicks`; A018 squad lease heartbeat; A022 `BaseDangerUntilTick` written from the damage callback; EML:24/29 lease expiries; D-lease rows read by the order gate (ModularBot.cs:270). RFS:458 `dirtyEpoch` written every tick by engine cell callbacks.
- Lease TTLs derived from a scan K (Scout, BeaconResponder 3*K; Deploy, BridgeRepair, UnitRepair, Garrison `max(200, 4*K)`) are safe only while the visit gap N stays well below the TTL; BeaconResponder at 75 is the tightest.

## Counts calls, not ticks (class B / C, not equivalent under a changed update gap)
- B016 `scalingCashHeldTicks`; A088-A099 and A107 (squad state-machine counters count Update calls, one per AttackForce P-run); D058/D059 TacticalMap `recheckTick`/`recomputeTick` (decrement skipped on Adopt-return and `!TopologyReady`; F1 P at K=50 if the rare skips are ignored); D060-D071 (clocks restart at pass completion, depend on per-call chunk quotas).
- E ArmyStaging :214 and BuildOrderKnobs :196 decays use floors or integer truncation per call: not exactly composable across run lengths. E040 CombatAnalysis decay is exact per multiply, but its weights are filled by callbacks outside the visit.

## Skippable pre-test guard on a P row (reviewer may prefer C)
- EngagementLog :242/:252, ArmyStaging :132, RadarContacts, ScaleTargets, SquadDesire (guard `IsTraitDisabled` or `Ready()` before the due test). EngagementLog :385/:394 sit inside the 50-tick sample run; the carry belongs on the outer :242 timer. A011 StageIdlePool gate is C (provider re-resolved every tick can return before the due test). E069 Engineer has a terminal `WinState` return before the due test (harmless after defeat).

## BotRng stream positions (conversion must keep draw order and count)
- A001/A003/A007: rushTicks, protectionForceTicks, protectOwnTicks draws at SquadManager :1674-1677 are dead values but must still be drawn in order. D078-D081: TacticalMap draws 5 per `ResetPerBaseCaches`; the reset count follows bridge-change timing. FR:2230/FR:2236 (Fransbot, draws at FR:1977-1979).
- Hazard outside this audit: Common SupportPowerBotModule (F007-F009) takes its scan interval and shuffle from the unseeded `world.LocalRandom`.

## TimerSim grid additions requested by the auditors
- K = 50, 125, 750 (D); `Eabs K= next0=` cases for absolute-deadline P rows (E); a takeover bot registers at an arbitrary tick r > 0, so the r grid {0,1,7} needs a case (E007); Engineer RouteRecheckTicks needs a concrete K (inert at default 0).

## Coverage gaps
- FransMcvExpansionManagerBotModule.cs (14,149 lines): covered at variable level by grep, not read in full (flagged in each row). Callee bodies behind D-slice pointer rows not audited: BotDirector, BotUtilityAxes, StalemateUrgencyState, StalemateBreakerPolicyCA, TurtleFallbackPolicy (D034 pointer row). RefinerySiteSearch (B080-B086) covered by grep plus about 600 lines of context.
- Design cites `BaseBuilderBotModuleCA.cs:855`; it is `:882` at 6f1f7dfd6.
