# ENGINE_REPLAY_LIST_02 (STAGGER-ENG-01; successor of ENGINE_REPLAY_LIST_01; fixes Haiku B1-B3 + LOWs, Opus pre-rulings)

Author: Sonnet. Base: TIMER_TABLE_06 7056CFB7 (frozen, Haiku D53E41D4); tables untouched. Reviewer: Haiku (review 26770d51 of 4efb925fa).

## Mechanism (unchanged)
`ModularBot.RunModules` calls `BotTick` of a module whose type name is in `ModularBot.EngineReplay` `Elapsed` times per gated visit (Elapsed = 1 at N = 1 and for every ungated bot, so one call: byte-identical). No engine edit. Replayed type names: PowerDownBotModule (both the AS and the Common declaration), SupportPowerBotASModule, SendUnitToAttackBotModule, LoadCargoBotModule, McvExpansionManagerBotModule, MinelayerBotModule, ResourceMapBotModule.
Caveat (Haiku LOW, ruled design, kept): at N > 1 the WHOLE BotTick runs Elapsed times back to back inside one visit, not only its counter. Counters, freezes and BotRng draws therefore advance exactly as at N = 1, and the work runs once per period; the cost is that these modules save no stagger work until STAGGER-ENG-02 (convert them in the cameo-engine clone with a pin update, after STAGGER-06 lands on master).

## B1: the count
The earlier "38" was a grep count of lines containing `engine/OpenRA` (it included prose cells). The engine-file rows are exactly the **31 rows F001-F031** of slice F. Every one is accounted for below.

| row | module | disposition |
|---|---|---|
| F001 | PowerDownBotModule (AS) | REPLAYED (P, F2) |
| F002 F003 F004 F005 | SupportPowerBotASModule | REPLAYED (C; R2 freeze row F002 stays exact because the counter steps once per elapsed tick) |
| F006 F007 F008 F009 F010 | SupportPowerBotModule (Common, classic only) | OUT (R11: ClassicAI is not gated; not replayed) |
| F011 F014 | SendUnitToAttackBotModule (AS) | REPLAYED (P, A) |
| F012 F013 | SendUnitToAttackBotModule (AS) | REPLAYED (C, R2 freeze kept; erratum: saved = no is binding) |
| F015 | LoadCargoBotModule (AS) | REPLAYED (P) |
| F016 | ExternalBotOrdersManager (AS) | OUT (not a scheduling timer); declared gating-visible difference, see below |
| F017 | McvExpansionManagerBotModule | REPLAYED module-level (E, R6 absolute until; the engine reads it outside the visit: no change possible without an engine edit, listed for STAGGER-ENG-02) |
| F018 F019 | McvExpansionManagerBotModule | REPLAYED (P) |
| F020 F021 F022 | McvExpansionManagerBotModule | REPLAYED (C; F022 R3 carry-remaining stays exact) |
| F023 F024 F025 | McvExpansionManagerBotModule | OUT rows, covered by module-level replay (not clocks; F025 defines r on the first call, which the replay keeps: the first of the Elapsed calls is that call) |
| F026 | MinelayerBotModule | REPLAYED (E, R6; same note as F017) |
| F027 | MinelayerBotModule | REPLAYED (P) |
| F028 | MinelayerBotModule | OUT row, covered by module-level replay (not a clock) |
| F029 | ResourceMapBotModule | REPLAYED (P; both instances) |
| F030 F031 | ResourceMapBotModule | OUT rows, covered by module-level replay (F030 NullReference risk and F031 first-call spike are outside this audit and unchanged) |

IN-REPO: none of F001-F031 (all 31 are in engine/); the in-repo P/C/E rows are converted by the batches P1-P4 and by fullstack/stagger-06-c.

## B2: the six OUT rows covered by module-level replay
F023, F024, F025 (McvExpansionManager), F028 (Minelayer), F030, F031 (ResourceMap). They are not clocks; they run inside the replayed BotTick, once per replayed call, exactly as today.

## B3: PowerDownBotModule has two declarations
`PowerDownBotManager.cs` declares `PowerDownBotModule` in `OpenRA.Mods.AS` (:32, row F001) and in `OpenRA.Mods.Common` (:37; `toggleTick--` at :115-119; no table row). `mods/cameo/ai/ai.yaml:722` names the trait `PowerDownBotModule` by simple name, and the trait loader binds a simple name by type name, so both classes are bound candidates and neither can be shown unused. Opus ruled (B3): the Common class is also replayed (same timer shape as F001, identity at N = 1 by the replay), as the untabled row below, rule of doubt.

| row | module | file:line | class | disposition |
|---|---|---|---|---|
| FX01 | PowerDownBotModule (Common) | engine/OpenRA.Mods.Common/Traits/BotModules/PowerDownBotManager.cs:115-119 | P (rule of doubt, untabled) | REPLAYED; the type-name match covers it with no extra code |

## LOW fixes and notes
- BotPeriod: a default struct (Period 0) now throws `InvalidOperationException` in `TryFire` instead of dividing by zero; `BotPeriod.F4` throws `ArgumentOutOfRangeException` when `res` is outside [0, K). Tests: `UnarmedPeriodAndBadResidueThrow`.
- F016 (ExternalBotOrdersManager, OUT): the module sets `ManagerRunning` on its first call and serves queued external orders from its BotTick. At N > 1 a queued entry is picked up at the next visit of the bot instead of the next tick (latency up to N - 1 ticks). This is a declared gating-visible change of the first landing; it is not replayed because it has no timer and replay would only repeat the same dequeue.
- RunModules test: goes into T-ENG in the build window (with the BotRng draw count N = 1 vs N = 4 for one replayed module, Opus condition 1).
