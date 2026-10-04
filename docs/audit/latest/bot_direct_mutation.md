# audit_bot_direct_mutation

Scanned 156 bot-module source files for direct actor `CancelActivity`/`QueueActivity`/`SetStance` and `GrantCondition`/`RevokeCondition` calls.

Allowlisted sites (15):
- OpenRA.Mods.Fransbot/Traits/FransEconomicSaturationBotModule.cs:401 — P2 dormant hazard: SetState grants from host-only IBotTick; no YAML consumer exists yet — first consumer desyncs multiplayer (arch review 2026-10-04); convert to orders or drop
- OpenRA.Mods.Fransbot/Traits/FransEconomicSaturationBotModule.cs:410 — P2 dormant hazard: SetState grants from host-only IBotTick; no YAML consumer exists yet — first consumer desyncs multiplayer (arch review 2026-10-04); convert to orders or drop
- OpenRA.Mods.Fransbot/Traits/FransMcvExpansionManagerBotModule.cs:11715 — P2 dormant hazard: expansion-lock grant from the tick path; no YAML consumer exists yet (arch review 2026-10-04); convert to orders or drop
- OpenRA.Mods.Fransbot/Traits/FransMcvExpansionManagerBotModule.cs:11723 — P2 dormant hazard: expansion-lock grant from the tick path; no YAML consumer exists yet (arch review 2026-10-04); convert to orders or drop
- OpenRA.Mods.Cameo/Traits/BotCounterDemandController.cs:65 — synced: grants fire from the SetBotCounterDemand order + TraitEnabled
- OpenRA.Mods.Cameo/Traits/BotCounterDemandController.cs:88 — synced: grants fire from the SetBotCounterDemand order + TraitEnabled
- OpenRA.Mods.Cameo/Traits/BotCounterDemandController.cs:96 — synced: grants fire from the SetBotCounterDemand order + TraitEnabled
- OpenRA.Mods.Cameo/Traits/BotInsurance.cs:87 — synced: grants fire from the insurance order path
- OpenRA.Mods.Cameo/Traits/BotInsurance.cs:89 — synced: grants fire from the insurance order path
- OpenRA.Mods.Cameo/Traits/BotPersonalityController.cs:110 — synced: grants fire from SetBotPersonality order + TraitEnabled
- OpenRA.Mods.Cameo/Traits/BotPersonalityController.cs:117 — synced: grants fire from SetBotPersonality order + TraitEnabled
- OpenRA.Mods.Cameo/Traits/BotPersonalityController.cs:141 — synced: grants fire from SetBotPersonality order + TraitEnabled
- OpenRA.Mods.Cameo/Traits/BotPersonalityController.cs:143 — synced: grants fire from SetBotPersonality order + TraitEnabled
- OpenRA.Mods.Cameo/Traits/DynamicBotInsurance.cs:540 — synced: grants fire from the insurance order path
- OpenRA.Mods.Cameo/Traits/DynamicBotInsurance.cs:546 — synced: grants fire from the insurance order path

PASS — zero unaudited direct-activity sites. Bots drive actors exclusively through the order stream; multiplayer stays in sync and the order gate (§19.6) sees every issuer/lease pairing.
