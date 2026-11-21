# ENGINE_REPLAY_LIST_01 (STAGGER-ENG-01, Opus ruling (a) replay)

Author: Sonnet. Base: TIMER_TABLE_06 7056CFB7 (frozen, Haiku D53E41D4). Tables stay frozen; this successor only tags the engine-owned rows that are NOT converted in this repo.

Mechanism: ModularBot.RunModules calls BotTick of a module named in ModularBot.EngineReplay Elapsed times per gated visit (Elapsed = 1 at N=1 and for ungated bots, so one call, byte-identical). No engine edit. Cost: no stagger saving for these modules until STAGGER-ENG-02 (convert in the cameo-engine clone + pin update, after STAGGER-06 lands on master).

Replayed module types: PowerDownBotModule, SupportPowerBotASModule, SendUnitToAttackBotModule, LoadCargoBotModule, McvExpansionManagerBotModule, MinelayerBotModule, ResourceMapBotModule. Not replayed: SupportPowerBotModule (Common, classic only, R11 OUT), ExternalBotOrdersManager (OUT rows).

| row id | module | file:line | class | ruling | reason |
|---|---|---|---|---|---|
| F001 | PowerDownBotModule (AS) | engine/OpenRA.Mods.AS/Traits/BotModules/PowerDownBotManager.cs:120-122 | P | default | engine-owned call-counted timer; replayed per elapsed tick |
| F002 | SupportPowerBotASModule (AS; main + @wc2) | engine/OpenRA.Mods.AS/Traits/BotModules/SupportPowerBotASModule.cs:79-80 | C | R2 | engine-owned call-counted timer; replayed per elapsed tick |
| F003 | SupportPowerBotASModule (AS; main + @wc2) | engine/OpenRA.Mods.AS/Traits/BotModules/SupportPowerBotASModule.cs:95 | C | default | engine-owned call-counted timer; replayed per elapsed tick |
| F004 | SupportPowerBotASModule (AS; main + @wc2) | engine/OpenRA.Mods.AS/Traits/BotModules/SupportPowerBotASModule.cs:104 | C | default | engine-owned call-counted timer; replayed per elapsed tick |
| F005 | SupportPowerBotASModule (AS; main + @wc2) | engine/OpenRA.Mods.AS/Traits/BotModules/SupportPowerBotASModule.cs:111 | C | default | engine-owned call-counted timer; replayed per elapsed tick |
| F011 | SendUnitToAttackBotModule (AS; main + @chrono) | engine/OpenRA.Mods.AS/Traits/BotModules/SendUnitToAttackBotModule.cs:129 | P | default | engine-owned call-counted timer; replayed per elapsed tick |
| F012 | SendUnitToAttackBotModule (AS; main + @chrono) | engine/OpenRA.Mods.AS/Traits/BotModules/SendUnitToAttackBotModule.cs:139 | C | R2 | engine-owned call-counted timer; replayed per elapsed tick |
| F013 | SendUnitToAttackBotModule (AS; main + @chrono) | engine/OpenRA.Mods.AS/Traits/BotModules/SendUnitToAttackBotModule.cs:146 | C | R2 | engine-owned call-counted timer; replayed per elapsed tick |
| F014 | SendUnitToAttackBotModule (AS; main + @chrono) | engine/OpenRA.Mods.AS/Traits/BotModules/SendUnitToAttackBotModule.cs:290 | A | default | engine-owned call-counted timer; replayed per elapsed tick |
| F015 | LoadCargoBotModule (AS; @Infantry/@TankBunker/@Battery) | engine/OpenRA.Mods.AS/Traits/BotModules/LoadCargoBotModule.cs:113 | P | default | engine-owned call-counted timer; replayed per elapsed tick |
| F017 | McvExpansionManagerBotModule (Common) | engine/OpenRA.Mods.Common/Traits/BotModules/McvExpansionManagerBotModule.cs:607 | E | R6 | engine-owned call-counted timer; replayed per elapsed tick |
| F018 | McvExpansionManagerBotModule (Common) | engine/OpenRA.Mods.Common/Traits/BotModules/McvExpansionManagerBotModule.cs:622 | P | default | engine-owned call-counted timer; replayed per elapsed tick |
| F019 | McvExpansionManagerBotModule (Common) | engine/OpenRA.Mods.Common/Traits/BotModules/McvExpansionManagerBotModule.cs:630 | P | default | engine-owned call-counted timer; replayed per elapsed tick |
| F020 | McvExpansionManagerBotModule (Common) | engine/OpenRA.Mods.Common/Traits/BotModules/McvExpansionManagerBotModule.cs:636 | C | default | engine-owned call-counted timer; replayed per elapsed tick |
| F021 | McvExpansionManagerBotModule (Common) | engine/OpenRA.Mods.Common/Traits/BotModules/McvExpansionManagerBotModule.cs:639 | C | default | engine-owned call-counted timer; replayed per elapsed tick |
| F022 | McvExpansionManagerBotModule (Common) | engine/OpenRA.Mods.Common/Traits/BotModules/McvExpansionManagerBotModule.cs:664-704 | C | R3 | engine-owned call-counted timer; replayed per elapsed tick |
| F026 | MinelayerBotModule (Common) | engine/OpenRA.Mods.Common/Traits/BotModules/BotModuleLogic/MinelayerBotModule.cs:111-112 | E | R6 | engine-owned call-counted timer; replayed per elapsed tick |
| F027 | MinelayerBotModule (Common) | engine/OpenRA.Mods.Common/Traits/BotModules/BotModuleLogic/MinelayerBotModule.cs:114 | P | default | engine-owned call-counted timer; replayed per elapsed tick |
| F029 | ResourceMapBotModule (Common; 2 instances) | engine/OpenRA.Mods.Common/Traits/BotModules/ResourceMapBotModule.cs:139 | P | default | engine-owned call-counted timer; replayed per elapsed tick |
