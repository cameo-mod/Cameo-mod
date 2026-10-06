# Uncovered reference declarations — 2026-10-05

Companion to the trait-unification spec. “No counterpart” here means no
loaded same-family declaration in the captured sources, after the explicit old
missile-name mappings. This is an intake list, NOT proof that Cameo lacks an
equivalent capability under another name. Before a port, compare the nearest
existing mechanic and harvest only its missing behavior. USE is zero because
these donor declarations are not loaded. Descriptions below are source contracts;
where a source lacks a description, the parent and field contract are stated.

## AI and squad control

| Donor declaration | What it enables (source description) | Source |
|---|---|---|
| `ref:CA-upstream:SquadPathOverlayInfo` | Renders a debug overlay showing the pathfinding routes of AI squads. Attach this to the world actor. | `CAmod/OpenRA.Mods.CA/Traits/SquadPathOverlay.cs:23` |
| `ref:CN:BotCapabilitiesInfo` | Declares AI capability tags used by CNSquadManagerBotModule for threat detection. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/BotCapabilities.cs:18` |
| `ref:CN:BotPlayerNamesInfo` | Assigns deterministic faction-specific display names to bots. Attach this to the Player actor. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Player/BotPlayerNames.cs:20` |
| `ref:CN:CNBotProfileBotModuleInfo` | Sets the AI's strategic profile and drives adaptive profile switching and tech-stage detection. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/BotModules/CNBotProfileBotModule.cs:71` |
| `ref:CN:CNCliffDemolitionBotModuleInfo` | Shoots destroyable cliffs open. A cliff is worth demolishing in two situations, and the module only ever acts on those: inside a region this bot holds, where the collapse joins up its own ground, and inside enemy-held ground it is currently attacking, where the collapse is a new way in. Reads the region graph from CNTacticalMapBotModule/CNRegionManagerBotModule. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/BotModules/CNCliffDemolitionBotModule.cs:23` |
| `ref:CN:CNGarrisonBotModuleInfo` | Sends spare idle infantry to garrison own buildings tagged with GarrisonCapability (e.g. GAFORT) whenever they have open Cargo capacity, preferring whichever infantry specialization the local threat around that building calls for. Periodically swaps a mismatched passenger out if the local threat changes and none of the current garrison covers it. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/BotModules/CNGarrisonBotModule.cs:25` |
| `ref:CN:CNRegionManagerBotModuleInfo` | Tracks which regions of the map this bot holds, what each one's ground is worth, and what each held region is for (see CNRegionRole). Reads the shared region graph from CNTacticalMapBotModule; owns no terrain analysis of its own. Draw it with the \"cntopo\" chat command. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/BotModules/CNRegionManagerBotModule.cs:102` |
| `ref:CN:CNRepairManagerBotModuleInfo` | Sends damaged idle base units to allied repair facilities. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/BotModules/CNRepairManagerBotModule.cs:24` |
| `ref:CN:CNVeinholeAssaultBotModuleInfo` | Force-fires veinholes down. Veinholes are NoAutoTarget and RequiresForceFire, so nothing shoots one unless it is told to - which makes this module the whole of the behaviour rather than a nudge on top of it. Gate it by faction side: GDI burns the weed out, Nod leaves it standing because its weed harvesters live off it. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/BotModules/CNVeinholeAssaultBotModule.cs:24` |
| `ref:CN:MobSquadSelectionDecorationInfo` | Shows a selection box on this slave actor whenever its MobSpawnerMaster is selected.  Also hosts regular actor decorations (rank, heal, etc.) while the vanilla SelectionDecorations are removed.  Add this trait to the slave actor, not the master. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/MobSpawner/MobSquadSelectionDecoration.cs:21` |
| `ref:CN:RestoresInfantrySquadsInfo` | Restores missing MobSpawner squad members when a repaired squad reaches full health at this host. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/RestoresInfantrySquads.cs:19` |
| `ref:Generals:GeneralCollectorBotModuleInfo` | Put this on the Player actor. Manages bot collector to ensure they always continue collecting as long as there are resources on the map. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/BotModules/GeneralCollectorBotModule.cs:24` |
| `ref:Generals:InitialBaseAndWorkerBotModuleInfo` | Manages the initial base and build dozer on request. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/BotModules/InitialBaseAndWorkerBotModule.cs:21` |
| `ref:SP:UnpackBaseBotModuleInfo` | Make AI move their expansion unit, when there is no more room for placing a new Mcv Factory, or we have plenty of MCV for normal production. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/BotModules/UnpackBaseBotModule.cs:21` |
## Combat, health and actor lifecycle

| Donor declaration | What it enables (source description) | Source |
|---|---|---|
| `ref:CA-upstream:AllyProxyFromSelectionInfo` | Automatically adds proxy actors to selection when their parent ally building is selected. This enables standard traits like RallyPoint on proxy actors to work when ally buildings are selected. | `CAmod/OpenRA.Mods.CA/Traits/World/AllyProxyFromSelection.cs:20` |
| `ref:CA-upstream:ArmamentBurstCounterInfo` | Tracks 1-based shot indexes within a burst for a specific armament. | `CAmod/OpenRA.Mods.CA/Traits/ArmamentBurstCounter.cs:21` |
| `ref:CA-upstream:AutoGuardInfo` | Attach to support unit so that when ordered as part of a group with combat units it will guard those units. | `CAmod/OpenRA.Mods.CA/Traits/AutoGuard.cs:21` |
| `ref:CA-upstream:CampaignProgressTrackerInfo` | Stores campaign progress. | `CAmod/OpenRA.Mods.CA/Traits/Player/CampaignProgressTracker.cs:25` |
| `ref:CA-upstream:CancelActivityOnPickupInfo` | When picked up, cancels any activities. | `CAmod/OpenRA.Mods.CA/Traits/CancelActivityOnPickup.cs:17` |
| `ref:CA-upstream:ConvertibleInfo` | This Actor can be converted into another actor (or actors) through the UnitConverter trait. | `CAmod/OpenRA.Mods.CA/Traits/Convertible.cs:21` |
| `ref:CA-upstream:CreateProxyActorForAlliesInfo` | Creates a proxy actor on creation for each allied player. | `CAmod/OpenRA.Mods.CA/Traits/CreateProxyActorForAllies.cs:23` |
| `ref:CA-upstream:EncyclopediaExtrasInfo` | To override encyclopedia preview. | `CAmod/OpenRA.Mods.CA/Traits/EncyclopediaExtras.cs:16` |
| `ref:CA-upstream:FlatHealthDamageMultiplierInfo` | Modifies the damage taken by the actor to emulate a specified amount of additional health. | `CAmod/OpenRA.Mods.CA/Traits/Multipliers/FlatHealthDamageMultiplier.cs:17` |
| `ref:CA-upstream:HealthCapDamageMultiplierInfo` | Modifies the damage taken by the actor to emulate specified maximum health. | `CAmod/OpenRA.Mods.CA/Traits/Multipliers/HealthCapDamageMultiplier.cs:17` |
| `ref:CA-upstream:IgnoreOutOfRangeAttackOrdersInfo` | Intercepts regular attack orders against out-of-range actor targets and ignores them while enabled. | `CAmod/OpenRA.Mods.CA/Traits/Attack/IgnoreOutOfRangeAttackOrders.cs:19` |
| `ref:CA-upstream:InfiltrateToCreateProxyActorInfo` | Replaces InfiltrateForSupportPower. Allows the spawned proxy actor to inherit the faction of the infiltrated actor, or to be owned by the target. | `CAmod/OpenRA.Mods.CA/Traits/Infiltration/InfiltrateToCreateProxyActor.cs:21` |
| `ref:CA-upstream:InitiallyHuntsInfo` | Hunts on creation. | `CAmod/OpenRA.Mods.CA/Traits/InitiallyHunts.cs:18` |
| `ref:CA-upstream:InterceptorInfo` | Runs support-power aircraft through approach, timed guard, return/exit, and removal. | `CAmod/OpenRA.Mods.CA/Traits/Air/Interceptor.cs:26` |
| `ref:CA-upstream:LaysMinefieldInfo` | This actor places mines around itself, and replenishes them after a while. | `CAmod/OpenRA.Mods.CA/Traits/LaysMinefield.cs:23` |
| `ref:CA-upstream:PeriodicExplosionOnSlavesInfo` | Explodes a weapon at the actor's position when enabled. Reload/BurstDelays are used as explosion intervals. | `CAmod/OpenRA.Mods.CA/Traits/PeriodicExplosionOnSlaves.cs:31` |
| `ref:CA-upstream:PlayerBountyPoolInfo` | Tracks and provides access to player bounty pool. | `CAmod/OpenRA.Mods.CA/Traits/Player/PlayerBountyPool.cs:17` |
| `ref:CA-upstream:PlayerConnectionStatusInfo` | Tracks player connection status. | `CAmod/OpenRA.Mods.CA/Traits/Player/PlayerConnectionStatus.cs:17` |
| `ref:CA-upstream:ReturnsToBaseOnAmmoDepletedInfo` | For aircraft with a Strafe AttackType, which don't return to base due to using an AttackMove. | `CAmod/OpenRA.Mods.CA/Traits/ReturnsToBaseOnAmmoDepleted.cs:20` |
| `ref:CA-upstream:SpawnActorAbilityInfo` | Actor can deploy to be able to target a location and spawn an actor there. | `CAmod/OpenRA.Mods.CA/Traits/SpawnActorAbility.cs:32` |
| `ref:CA-upstream:SpawnActorOnMindControlledInfo` | Spawn another actor immediately upon being mind controlled. | `CAmod/OpenRA.Mods.CA/Traits/SpawnActorOnMindControlled.cs:22` |
| `ref:CA-upstream:SpawnHuskEffectOnDeathInfo` | Spawn projectile as husk upon death. | `CAmod/OpenRA.Mods.CA/Traits/SpawnHuskEffectOnDeath.cs:22` |
| `ref:CA-upstream:SpeedCapSpeedMultiplierInfo` | Modifies the speed of an actor to emulate specified maximum speed. | `CAmod/OpenRA.Mods.CA/Traits/Multipliers/SpeedCapSpeedMultiplier.cs:16` |
| `ref:CA-upstream:TargetSpecificOrderVoiceInfo` | Selects an order voice by the target's enabled target types, with a fallback voice; class Desc is stale. | `CAmod/OpenRA.Mods.CA/Traits/TargetSpecificOrderVoice.cs:19` |
| `ref:CA-upstream:TargetedDiveAbilityInfo` | Allows unit to dive to a targeted location. | `CAmod/OpenRA.Mods.CA/Traits/TargetedDiveAbility.cs:20` |
| `ref:CA-upstream:TargetedLeapAbilityInfo` | Allows unit to leap to a targeted location. | `CAmod/OpenRA.Mods.CA/Traits/TargetedLeapAbility.cs:19` |
| `ref:CA-upstream:UnitConverterInfo` | Allow convertible units to enter and spawn a new actor or actors. | `CAmod/OpenRA.Mods.CA/Traits/UnitConverter.cs:22` |
| `ref:CA-upstream:ValueScalingFirepowerMultiplierInfo` | Modifies the firepower of a given actor according to its value. | `CAmod/OpenRA.Mods.CA/Traits/Multipliers/ValueScalingFirepowerMultiplier.cs:16` |
| `ref:CA-upstream:WithDistortionHaloInfo` | Renders a distorted halo of animated arc segments around the actor. | `CAmod/OpenRA.Mods.CA/Traits/Render/WithDistortionHalo.cs:21` |
| `ref:CA-upstream:WithEjectedCasingsInfo` | Ejects casings when the actor fires weapons. Supports burst firing of casings over time using the casing weapon's Burst and BurstDelays properties, or the BurstOverride and BurstDelayOverride trait properties for custom control. | `CAmod/OpenRA.Mods.CA/Traits/WithEjectedCasings.cs:23` |
| `ref:CA-upstream:WithLinkedRangeCirclePreviewInfo` | Shows matching range circles from existing actors while placing a structure. | `CAmod/OpenRA.Mods.CA/Traits/Render/WithLinkedRangeCirclePreview.cs:19` |
| `ref:CA-upstream:WithPrismLinkVisualizationInfo` | Renders selection boxes on Prism Towers within range. | `CAmod/OpenRA.Mods.CA/Traits/Render/WithPrismLinkVisualization.cs:24` |
| `ref:CA-upstream:WithRadiatingCircleInfo` | Radiating circle overlay with an optional outer circle. | `CAmod/OpenRA.Mods.CA/Traits/Render/WithRadiatingCircle.cs:24` |
| `ref:CA-upstream:WithReloadBarInfo` | . | `CAmod/OpenRA.Mods.CA/Traits/WithReloadBar.cs:20` |
| `ref:CA-upstream:WithSpawnedActorIdentifierInfo` | Draws a marker around actors tracked by SpawnActorAbility while selected. | `CAmod/OpenRA.Mods.CA/Traits/Render/WithSpawnedActorIdentifier.cs:28` |
| `ref:CN:BloomGlowEffectInfo` | Sole writer of WorldTintState's bloom state: when the trait condition is active (typically dusk/dawn/night, granted by DayNightCycle), this publishes the per-frame BloomStrength = Intensity * NightFactor01 to the engine renderer, which then runs RenderGlowBloom each frame. Intensity is constant; NightFactor01 scales smoothly from 0 (noon) to 1 (midnight), so dusk/dawn produce a softer halo than full night. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/BloomGlowEffect.cs:21` |
| `ref:CN:CNCombatSignalReporterInfo` | Reports attack and damage events to CNDynamicMusicController for the dynamic soundtrack. Intended to be inherited on shared unit/building templates alongside ^PlayerHandicaps. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/CNCombatSignalReporter.cs:15` |
| `ref:CN:CNDestroyableCliffInfo` | A chunk of cliff that can be shot away, opening a path through it. Tiberian Sun shipped the \"Destroyable Cliffs\" tile set (dcliff01/dcliff02) without a destroyed counterpart, so the collapsed state is assembled from templates the tile set already has - normally two ramp pieces covering the same footprint. Actors are placed by CNDestroyableCliffLayer, which finds the templates on the map; nothing has to be put down by hand in the editor. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/CNDestroyableCliff.cs:25` |
| `ref:CN:CNDestroyableCliffLayerInfo` | Places a CNDestroyableCliff actor over every destroyable-cliff template found on the map, the same way LegacyBridgeLayer places bridges. Mappers only paint the tiles; every map that already uses the Tiberian Sun \"Destroyable Cliffs\" set gets working cliffs without being touched. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/CNDestroyableCliffLayer.cs:22` |
| `ref:CN:CNDynamicMusicControllerInfo` | Layered dynamic soundtrack: crossfades 4 always-looping stems (Peace/Tension/Combat/BigBattle) per faction based on the followed player's (RenderPlayer, falling back to LocalPlayer) combat situation. Factions without a configured score are left untouched (normal MusicPlaylist jukebox keeps playing). Attach CNCombatSignalReporter to shared unit/building templates to feed combat events into this trait. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/CNDynamicMusicController.cs:22` |
| `ref:CN:CNLeavesPitOnDeathInfo` | Sinks the ground where this actor died into a pit: a flat floor a height level down, ringed by single-cell ramps so the hole is walkable rather than a cut-out. A killed veinhole leaves a real dent in the map, not a decal. The tiles are found in the tile set rather than named here. Tiberian Sun ships 1x1 pieces for all four slope directions more than once - the stock slopes (slope01-slope04) and again as 'Ramp edge fixup' pieces under ids that differ per tile set - so looking them up by their slope data covers every tile set with one rule instead of a table of ids that has to be kept in step with the art. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/CNLeavesPitOnDeath.cs:26` |
| `ref:CN:CallProtectorsOnDamageInfo` | Calls nearby allied actors to attack the source when this actor is damaged. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/CallProtectorsOnDamage.cs:19` |
| `ref:CN:FadeOutInfo` | Fades the actor out over time, then removes it. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/FadeOut.cs:17` |
| `ref:CN:FormationMoveInfo` | When multiple units with this trait are ordered to move simultaneously,  they arrange into a directional grid formation around the target cell.  On large direction changes (> 90°) the most-advanced units in the new  direction are assigned to front slots, flipping the formation automatically. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Player/FormationMove.cs:20` |
| `ref:CN:HeightAdvantageBonusInfo` | Grants weapon range and sight range bonuses when the actor stands on terrain above BaseHeight. Designed for CN's RMG maps where flat ground is at height 2. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/HeightAdvantageBonus.cs:16` |
| `ref:CN:KeepNearActorsInfo` | When idle, move back near the closest matching allied actor. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/KeepNearActors.cs:18` |
| `ref:CN:RandomMapAmbientSoundInfo` | Plays random positional ambient sounds over specified terrain type cells. Attach to the world actor. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/RandomMapAmbientSound.cs:18` |
| `ref:CN:RepairableInBarracksInfo` | Allows infantry to enter barracks for repairs instead of being repaired while standing outside. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/RepairableInBarracks.cs:25` |
| `ref:CN:ScattererInfo` | Makes this unit automatically dodge sideways when an enemy crusher is  approaching and this unit would be in its path. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Scatterer.cs:16` |
| `ref:CN:SecondaryHealthInfo` | Adds a secondary health pool (ablative armor or regenerating shield). When used alongside CNHealth, damage is routed through this layer automatically. Set RegenerateRate to 0 for ablative, or > 0 for shield mode. Supports multiple instances via @ suffixes. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Player/SecondaryHealth.cs:19` |
| `ref:CN:SpawnActorOnDamageInfo` | Spawns one or more actors when this actor is damaged. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/SpawnActorOnDamage.cs:19` |
| `ref:CN:SpawnActorOnTimerInfo` | Periodically spawns actors near this actor. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/SpawnActorOnTimer.cs:19` |
| `ref:CN:SubgroupIconInfo` | Overrides the icon shown in the selection subgroup bar for this actor.  Takes priority over the BuildableInfo icon sequence. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Player/SubgroupIcon.cs:14` |
| `ref:Generals:EmitInfantryOnDeathInfo` | Spawn new actors when sold. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/EmitInfantryOnDeath.cs:20` |
| `ref:Generals:LaysMinefieldInfo` | This actor places mines around itself, and replenishes them after a while. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/LaysMinefield.cs:19` |
| `ref:Generals:RadarIconInfo` | Adds relationship-filtered colored locations to the radar display. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/Radar/RadarIcon.cs:20` |
| `ref:RV:AffectedByTemporalInfo` | This actor can be affected by temporal warheads. | `Romanovs-Vengeance/OpenRA.Mods.RA2/Traits/AffectedByTemporal.cs:20` |
| `ref:RV:CaptureSoundInfo` | Plays a configurable sound on capture, with fog audibility and volume controls. | `Romanovs-Vengeance/OpenRA.Mods.RA2/Traits/Sound/CaptureSound.cs:18` |
| `ref:SP:ArmamentsChargeBarInfo` | Grants condition after weapon fire and show the condition duration. You can use it to disable reloading armnament, because reloading armnament cannot disable itself. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/ArmamentsChargeBar.cs:20` |
| `ref:SP:AutoDemolisherInfo` | Allow unit auto demolish the target. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/AutoDemolisher.cs:20` |
| `ref:SP:DamageOnCreationInfo` | Attach this to actors which should regenerate or lose health points just after creation. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/DamageOnCreation.cs:18` |
| `ref:SP:FirestromSPInfo` | SP style Firestrom, generate a circle of effect and deal damage in a ring area. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/FirestromSP.cs:22` |
| `ref:SP:ForceFireAtLocationInfo` | Hack: used for veinhole. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/ForceFireAtLocation.cs:19` |
| `ref:SP:SpawnActorsOnCorpseInRadiusInfo` | Spawns configured actors from eligible nearby corpses, filtered by type and relationship. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/SpawnActorsOnCorpseInRadius.cs:24` |
| `ref:SP:SpawnCorpseOnDeathInfo` | When killed, this actor notify nearby place who use corpse for some effects, such as  . | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/SpawnCorpseOnDeath.cs:19` |
| `ref:SP:SpawnHuskEffectOnDeathInfo` | Spawn projectile as husk upon death. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/SpawnHuskEffectOnDeath.cs:23` |
| `ref:SP:WithDisposedAnimationInfo` | This actor has animation when disposed by game. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/Render/WithDisposedAnimation.cs:19` |
| `ref:SP:WithMakeExplodeWeaponInfo` | Launch weapon or/and generate sprite effect when created or deploying. Can be affected by  | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/WithMakeExplodeWeapon.cs:20` |
## Conditions, ownership and player feedback

| Donor declaration | What it enables (source description) | Source |
|---|---|---|
| `ref:CA-upstream:DummyConditionConsumerInfo` | Just to prevent YAML errors when a condition isn't used (sometimes cleaner than removing a large number of traits/properties). | `CAmod/OpenRA.Mods.CA/Traits/Conditions/DummyConditionConsumer.cs:16` |
| `ref:CA-upstream:DummyConditionGranterInfo` | Just to prevent YAML errors when a condition is consumed but not granted (sometimes cleaner than removing a large number of traits/properties). | `CAmod/OpenRA.Mods.CA/Traits/Conditions/DummyConditionGranter.cs:16` |
| `ref:CA-upstream:GrantConditionIfOwnerIsNeutralInfo` | Grants a condition if the owner is the Neutral player. | `CAmod/OpenRA.Mods.CA/Traits/Conditions/GrantConditionIfOwnerIsNeutral.cs:16` |
| `ref:CA-upstream:GrantConditionOnEnemiesNearbyInfo` | Grants a condition to the actor when a given number of enemies are within a given range. | `CAmod/OpenRA.Mods.CA/Traits/Conditions/GrantConditionOnEnemiesNearby.cs:18` |
| `ref:CA-upstream:GrantConditionOnLobbyOptionInfo` | Grants a condition to the actor when created if fog is enabled. | `CAmod/OpenRA.Mods.CA/Traits/Conditions/GrantConditionOnLobbyOption.cs:17` |
| `ref:CA-upstream:GrantConditionOnPlayerFundsInfo` | Grants a condition to this actor when the player has stored funds (cash plus resources). | `CAmod/OpenRA.Mods.CA/Traits/Conditions/GrantConditionOnPlayerFunds.cs:18` |
| `ref:CA-upstream:GrantConditionToAttachedInfo` | Grants a condition to any attached actors. | `CAmod/OpenRA.Mods.CA/Traits/Conditions/GrantConditionToAttached.cs:18` |
| `ref:CA-upstream:GrantConditionToSpawnerSlavesInfo` | Grants a condition to any attached actors. | `CAmod/OpenRA.Mods.CA/Traits/Conditions/GrantConditionToSpawnerSlaves.cs:18` |
| `ref:CA-upstream:GrantConditionWhileProducingInfo` | Grants a condition while the actor is producing something. | `CAmod/OpenRA.Mods.CA/Traits/Conditions/GrantConditionWhileProducing.cs:18` |
| `ref:CA-upstream:GrantTimedConditionOnPointDefenseHitInfo` | Gives a condition to the actor for a limited time when point defense destroys an incoming projectile. | `CAmod/OpenRA.Mods.CA/Traits/Conditions/GrantTimedConditionOnPointDefenseHit.cs:18` |
| `ref:CA-upstream:InfiltrateForTimedConditionInfo` | The actor gains a timed condition when infiltrated. | `CAmod/OpenRA.Mods.CA/Traits/Infiltration/InfiltrateForTimedCondition.cs:19` |
| `ref:CA-upstream:LobbyMissionInfoInfo` | Displays additional info in the lobby chat after a map is selected. Requires LobbyMissionInfoLogic widget logic. | `CAmod/OpenRA.Mods.CA/Traits/World/LobbyMissionInfo.cs:20` |
| `ref:CA-upstream:NotificationManagerInfo` | Tracks last notification times. | `CAmod/OpenRA.Mods.CA/Traits/Player/NotificationManager.cs:18` |
| `ref:CA-upstream:NotificationOnDamageInfo` | Plays an audio notification and shows a radar ping when actor is damaged. | `CAmod/OpenRA.Mods.CA/Traits/NotificationOnDamage.cs:18` |
| `ref:CN:AnnounceOnConditionInfo` | Plays a faction-specific speech notification and/or displays a text notification when a condition is granted or revoked. Use RequiresCondition to bind to the condition. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/AnnounceOnCondition.cs:16` |
| `ref:CN:CombatChatterInfo` | Occasionally plays combat-chatter voices when the unit opens fire or takes damage. Self-gating: does nothing unless the unit's VoiceSet defines the voice, so it can be added to a shared infantry template safely. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/CombatChatter.cs:16` |
| `ref:CN:ExploresMapOnOwnerChangeInfo` | Explores the whole map for the owning player when this actor enters the world or changes owner. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/ExploresMapOnOwnerChange.cs:18` |
| `ref:CN:PlayerDefeatedAnnouncerInfo` | Plays a speech notification to all surviving players when an enemy player is defeated. Attach this to the Player actor. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Player/PlayerDefeatedAnnouncer.cs:16` |
| `ref:Generals:GrantConditionWhileCollectingSuppliesInfo` | Grants a condition while the supply collector is collecting. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/Supply/GrantConditionWhileCollectingSupplies.cs:17` |
| `ref:RV:GrantConditionOnOwnerLostInfo` | Gives a condition to the actor after its owner loses the game. | `Romanovs-Vengeance/OpenRA.Mods.RA2/Traits/Conditions/GrantConditionOnOwnerLost.cs:17` |
| `ref:SP:GrantConditionOnExploredMapInfo` | Grant condition when explored map is enabled. Used for mega-wealth well to show itself. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/GrantConditionOnExploredMap.cs:18` |
| `ref:SP:GrantConditionOnShortGameInfo` | Grant condition when short game is enabled. Used for short game is enable on no-base mod. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/GrantConditionOnShortGame.cs:19` |
| `ref:SP:HasConditionInfo` | Hack: tell yaml checker there is the condition, to pass the yaml check. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/DebugHack/HasCondition.cs:18` |
| `ref:SP:RevealsShroudToParentOwnerInfo` | Reveal shroud generated by the ' ' trait, to parent actor's player in ' '. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/RevealsShroudToParentOwner.cs:18` |
## Economy, tech and progression

| Donor declaration | What it enables (source description) | Source |
|---|---|---|
| `ref:CA-upstream:AddsToReclaimableValueInfo` | When killed, this actor adds value to the owner's reclaimable value pool. | `CAmod/OpenRA.Mods.CA/Traits/AddsToReclaimableValue.cs:19` |
| `ref:CA-upstream:AdvancesTimelineInfo` | On creation, adds specified number of ticks to a specified 'ProvidesPrerequisitesOnTimeline' trait. | `CAmod/OpenRA.Mods.CA/Traits/AdvancesTimeline.cs:17` |
| `ref:CA-upstream:ConvertsResourcesInfo` | Gradually converts resources within a given radius. | `CAmod/OpenRA.Mods.CA/Traits/ConvertsResources.cs:21` |
| `ref:CA-upstream:GivesPlayerExperienceOnCaptureInfo` | Grants player XP to capturing player. | `CAmod/OpenRA.Mods.CA/Traits/GivesPlayerExperienceOnCapture.cs:20` |
| `ref:CA-upstream:GrantConditionOnResupplyInfo` | Grants a condition when being resupplied. | `CAmod/OpenRA.Mods.CA/Traits/Conditions/GrantConditionOnResupply.cs:17` |
| `ref:CA-upstream:GrantConditionOnResupplyingInfo` | Grants a condition when resupplying another actor. | `CAmod/OpenRA.Mods.CA/Traits/Conditions/GrantConditionOnResupplying.cs:17` |
| `ref:CA-upstream:InheritsExperienceLevelOfMasterInfo` | Grants conditions based on the current level of this actor's first available master. | `CAmod/OpenRA.Mods.CA/Traits/InheritsExperienceLevelOfMaster.cs:19` |
| `ref:CA-upstream:PlayerExperienceLevelsInfo` | Tracks player experience and sets grants prerequisites based on it. | `CAmod/OpenRA.Mods.CA/Traits/Player/PlayerExperienceLevels.cs:23` |
| `ref:CA-upstream:ProvidesPrerequisiteIfAlliesExistInfo` | Provides a prerequisite if one or more allies exist. | `CAmod/OpenRA.Mods.CA/Traits/Player/ProvidesPrerequisiteIfAlliesExist.cs:19` |
| `ref:CA-upstream:ProvidesPrerequisitesOnCountInfo` | Grants prerequisites at count thresholds, with faction, permanence and notification controls. | `CAmod/OpenRA.Mods.CA/Traits/Player/ProvidesPrerequisitesOnCount.cs:22` |
| `ref:CA-upstream:ProvidesPrerequisitesOnTimelineInfo` | Grants prerequisites along a timeline, with faction and notification controls. | `CAmod/OpenRA.Mods.CA/Traits/Player/ProvidesPrerequisitesOnTimeline.cs:22` |
| `ref:CA-upstream:ProvidesUpgradeInfo` | Provides a prerequisite that is used for upgrades. | `CAmod/OpenRA.Mods.CA/Traits/ProvidesUpgrade.cs:16` |
| `ref:CA-upstream:RearmsToUpgradeInfo` | Use in conjunction with Rearmable and an AmmoPool with large reload delay. Replaces with specified unit after a delay (optionally if the unit is also undamaged). | `CAmod/OpenRA.Mods.CA/Traits/RearmsToUpgrade.cs:20` |
| `ref:CA-upstream:ReclaimableExperiencePoolInfo` | A pool of experience that can be added to and taken from. | `CAmod/OpenRA.Mods.CA/Traits/Player/ReclaimableExperiencePool.cs:20` |
| `ref:CA-upstream:ReclaimableValueProducerInfo` | Produces actors when enough reclaimable value is accumulated for the specified type. | `CAmod/OpenRA.Mods.CA/Traits/Player/ReclaimableValueProducer.cs:23` |
| `ref:CA-upstream:ReclaimsExperienceInfo` | When killed, gives to a reclaimable pool. When created takes from that pool. | `CAmod/OpenRA.Mods.CA/Traits/ReclaimsExperience.cs:18` |
| `ref:CA-upstream:SeedsResourceMultiplierInfo` | Modifies the interval between seeding resources. | `CAmod/OpenRA.Mods.CA/Traits/Multipliers/SeedsResourceMultiplier.cs:16` |
| `ref:CA-upstream:TransferResourcesOnTransformInfo` | Copies stored resource contents into the transformed actor's harvester, capped by its capacity. | `CAmod/OpenRA.Mods.CA/Traits/TransferResourcesOnTransform.cs:18` |
| `ref:CA-upstream:UpgradeableInfo` | Lists actors this actor may be upgraded to. | `CAmod/OpenRA.Mods.CA/Traits/Upgradeable.cs:23` |
| `ref:CA-upstream:UpgradesManagerInfo` | Manages unit upgrades. | `CAmod/OpenRA.Mods.CA/Traits/Player/UpgradesManager.cs:22` |
| `ref:CN:CNHandicapIncomeMultiplierInfo` | Modifies the value of resources delivered to this actor based on the owner's named handicap tier (Easy/Normal/Hard/Brutal). | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Player/CNHandicapIncomeMultiplier.cs:15` |
| `ref:CN:PlacesPavementInfo` | Automatically places pavement terrain tiles under a building when it is constructed. Uses LAT (Land-Art Transition) tiles for smooth edges between pavement and surrounding terrain. Supports multiple inner template IDs for visual variation. Optionally removes pavement when the building is sold or destroyed. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Player/PlacesPavement.cs:24` |
| `ref:CN:RandomTransformsNearResourcesInfo` | Replace with a randomly chosen actor when a resource spawns adjacent. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/RandomTransformsNearResources.cs:240` |
| `ref:CN:RandomTransformsNearResourcesManagerInfo` | Manages resource-triggered random tree transformations. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/RandomTransformsNearResources.cs:18` |
| `ref:Generals:GrantExternalConditionToAssignedCollectorsInfo` | Grants a condition to the SupplyCollectors assigned to this SupplyDock or SupplyCenter. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/Supply/GrantExternalConditionToAssignedCollectors.cs:20` |
| `ref:Generals:ResupplyDockInfo` | Resupplies a supply dock. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/Supply/ResupplyDock.cs:18` |
| `ref:Generals:SupplyCenterInfo` | Accepts typed supply deliveries, with storage, delivery offsets and occupancy controls. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/Supply/SupplyCenter.cs:22` |
| `ref:Generals:SupplyCollectorInfo` | Collects and delivers typed supplies with relationship filters, capacity, timing and dock-facing controls. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/Supply/SupplyCollector.cs:26` |
| `ref:Generals:SupplyDockInfo` | Stores supply for collection, with land/air offsets, reservation checks and occupancy controls. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/Supply/SupplyDock.cs:19` |
| `ref:Generals:WithSupplyDeliveryAnimationInfo` | Plays the delivery body sequence with a configurable wait. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/Render/WithSupplyDeliveryAnimation.cs:17` |
| `ref:RV:WithAcceptDeliveredCashSoundInfo` | Plays a sound when it accepts a cash delivery unit. | `Romanovs-Vengeance/OpenRA.Mods.RA2/Traits/WithAcceptDeliveredCashSound.cs:15` |
| `ref:SP:AddCashCheaterInfo` | Lets the actor make CashCheater generate more cash. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/AddCashCheater.cs:23` |
| `ref:SP:CashCheaterInfo` | Lets the player (AI) generate cash in a set of rules. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/Player/CashCheater.cs:20` |
| `ref:SP:VoiceAnnouncementOnProductionExitInfo` | Plays a voice clip when the actor is built. HACK: production building must have Exit to enable this | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/Sound/VoiceAnnouncementOnProductionExit.cs:20` |
## Movement, deployment and transport

| Donor declaration | What it enables (source description) | Source |
|---|---|---|
| `ref:CA-upstream:AutoDeployManagerInfo` | Allows the player to issue the orders the AutoDeployer traits trigger. | `CAmod/OpenRA.Mods.CA/Traits/Player/AutoDeployManager.cs:20` |
| `ref:CA-upstream:AutoDeployerInfo` | Allow this actor to automatically issue deploy orders on selected events. Require the AutoDeployManager trait on the player actor. | `CAmod/OpenRA.Mods.CA/Traits/AutoDeployer.cs:30` |
| `ref:CA-upstream:CargoBlockedInfo` | Attach to a transport to override the unload order. | `CAmod/OpenRA.Mods.CA/Traits/CargoBlocked.cs:18` |
| `ref:CA-upstream:CargoClonerInfo` | Continuously produces the passenger actor at no cost. Assumes only one passenger. | `CAmod/OpenRA.Mods.CA/Traits/CargoCloner.cs:21` |
| `ref:CA-upstream:EjectOnTransformInfo` | Eject a ground soldier or a paratrooper while in the air. | `CAmod/OpenRA.Mods.CA/Traits/EjectOnTransform.cs:18` |
| `ref:CA-upstream:FallsDownAndTransformsInfo` | Falls to the ground then transforms into a different actor. | `CAmod/OpenRA.Mods.CA/Traits/Air/FallsDownAndTransforms.cs:19` |
| `ref:CA-upstream:GrantConditionOnDeployTurretedInfo` | Grants a condition when a deploy order is issued. Can be paused with the granted condition to disable undeploying. | `CAmod/OpenRA.Mods.CA/Traits/Conditions/GrantConditionOnDeployTurreted.cs:23` |
| `ref:CA-upstream:ImmobileMultiCellInfo` | Represents stationary actors occupying a rectangular multi-cell footprint with center offset. | `CAmod/OpenRA.Mods.CA/Traits/ImmobileMultiCell.cs:17` |
| `ref:CA-upstream:ImmobilePositionableInfo` | Provides a mutable position and facing for actors that are moved externally, such as attachments. | `CAmod/OpenRA.Mods.CA/Traits/ImmobilePositionable.cs:20` |
| `ref:CA-upstream:ParachuteCargoOnConditionInfo` | Drops cargo at configured intervals and range while enabled, with optional return to base. | `CAmod/OpenRA.Mods.CA/Traits/Conditions/ParachuteCargoOnCondition.cs:16` |
| `ref:CA-upstream:PassengerBlockedInfo` | Attach to a transport to override the unload order. | `CAmod/OpenRA.Mods.CA/Traits/PassengerBlocked.cs:18` |
| `ref:CA-upstream:ScatterOnExitCargoInfo` | When exiting a transport the actor will scatter. | `CAmod/OpenRA.Mods.CA/Traits/ScatterOnExitCargo.cs:20` |
| `ref:CA-upstream:TurretedFloatingInfo` | Turret for where the unit is able to move instantly in any direction, to make the turret unaffected by changes in body facing. | `CAmod/OpenRA.Mods.CA/Traits/TurretedFloating.cs:17` |
| `ref:CA-upstream:UndeployOnStopInfo` | Undeploys an enabled deployed actor on Stop, including the turreted deployment variant. | `CAmod/OpenRA.Mods.CA/Traits/UndeployOnStop.cs:16` |
| `ref:CA-upstream:WaitsForTurretAlignmentOnUndeployInfo` | . | `CAmod/OpenRA.Mods.CA/Traits/WaitsForTurretAlignmentOnUndeploy.cs:19` |
| `ref:CN:CNAircraftFallsToEarthInfo` | CN aircraft crash behaviour with varied glide, curve, spiral and tumble profiles. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Air/CNAircraftFallsToEarth.cs:29` |
| `ref:CN:CNSteeredMobileInfo` | Makes vehicles perform arc maneuvers instead of rotating on the spot. When a required turn exceeds SteeringAngle, the unit drives ForwardCommitCells cells straight ahead, then sweeps SteeringCone arc cells toward the destination — all passed as a single Move so the engine's IsTurn arc-movement fires between consecutive cells. Requires TurnsWhileMoving: true on Mobile for fully gapless rotation. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Movement/CNSteeredMobile.cs:23` |
| `ref:CN:FaceTurretOnOrderInfo` | Keeps visual turrets facing the target of movement and attack orders until the actor becomes idle. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/FaceTurretOnOrder.cs:22` |
| `ref:CN:HuskFacingInfo` | Stores the facing an actor had when it died, for use by immobile husk actors. Reads FacingInit so SpawnActorOnDeath can pass the correct facing through. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/HuskFacing.cs:19` |
| `ref:Generals:PilotChamberInfo` | This trait provide a similar driver & hijacker system like that in TS/General. Disable this trait will set to no pilot. Pause this trait will not eject pilot when actor killed. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/PilotChamber.cs:24` |
| `ref:SP:ChangeSharedPassengerHealthInfo` | Change the health of SharedPassenger actors when they are in typical SharedCargo. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/Player/ChangeSharedPassengerHealth.cs:22` |
| `ref:SP:ExplodesAlsoTransportedInfo` | This actor explodes when killed, also when inside transport/carryall and explode at where it should be. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/ExplodesAlsoTransported.cs:20` |
## Projectiles

| Donor declaration | What it enables (source description) | Source |
|---|---|---|
| `ref:CA-upstream:ProjectileHuskInfo` | Projectile with customisable acceleration vector, recieve dead actor speed by using range modifier, used as aircraft husk. | `CAmod/OpenRA.Mods.CA/Projectiles/ProjectileHusk.cs:24` |
| `ref:CN:ProjectileHuskInfo` | Projectile with customisable acceleration vector, recieve dead actor speed by using range modifier, used as aircraft husk. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Projectiles/ProjectileHusk.cs:21` |
| `ref:SP:ProjetcileHuskInfo` | Projectile with customisable acceleration vector, recieve dead actor speed by using range modifier, used as aircraft husk. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Projectiles/ProjetcileHusk.cs:25` |
## Rendering, weather and terrain

| Donor declaration | What it enables (source description) | Source |
|---|---|---|
| `ref:CA-upstream:EncyclopediaColorPaletteInfo` | Create an encyclopedia preview palette that can be dynamically updated with arbitrary colors. | `CAmod/OpenRA.Mods.CA/Traits/Palettes/EncyclopediaColorPalette.cs:20` |
| `ref:CA-upstream:OverlayColorPickerPaletteInfo` | Create a color picker palette from another palette, using the overlay blend mode which increases contrast. | `CAmod/OpenRA.Mods.CA/Traits/Palettes/OverlayColorPickerPalette.cs:22` |
| `ref:CA-upstream:RenderLineInfo` | . | `CAmod/OpenRA.Mods.CA/Traits/Render/RenderLine.cs:21` |
| `ref:CA-upstream:TriggersProductionDoorOverlayInfo` | Play production door animation on allied building when unit is produced. | `CAmod/OpenRA.Mods.CA/Traits/Render/TriggersProductionDoorOverlay.cs:18` |
| `ref:CA-upstream:WeatherPaletteEffectInfo` | Global palette effect with a fixed color. | `CAmod/OpenRA.Mods.CA/Traits/PaletteEffects/WeatherPaletteEffect.cs:22` |
| `ref:CA-upstream:WithEnterExitWorldOverlayInfo` | Draws an overlay on top of a make animation. | `CAmod/OpenRA.Mods.CA/Traits/Render/WithEnterExitWorldOverlay.cs:18` |
| `ref:CA-upstream:WithFlashEffectInfo` | Flashes the target at a set interval. | `CAmod/OpenRA.Mods.CA/Traits/Render/WithFlashEffect.cs:19` |
| `ref:CA-upstream:WithPreviewDecorationInfo` | Displays a custom UI overlay relative to the actor's mouseover bounds. Also renders on actor previews. | `CAmod/OpenRA.Mods.CA/Traits/Render/WithPreviewDecoration.cs:20` |
| `ref:CA-upstream:WithUnitConverterCountDecorationInfo` | Displays a text overlay relative to the selection box. | `CAmod/OpenRA.Mods.CA/Traits/Render/WithUnitConverterCountDecoration.cs:22` |
| `ref:CN:AlphaGradientPaletteInfo` | Generates a palette with a single RGB color and a linear alpha gradient across all 256 indices. Index 0 = fully opaque, Index 255 = fully transparent (or reversed via Invert). | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Render/AlphaGradientPalette.cs:21` |
| `ref:CN:AtmosphericGradingRendererInfo` | Full-screen atmospheric color grading. Mode is chosen by the Graphics.PostProcessGrading setting. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/AtmosphericGradingRenderer.cs:16` |
| `ref:CN:CNBaseOverlayInfo` | Debug overlay that draws the bases CNBaseBuilderBotModule has clustered: center, build radius, assigned role and which buildings belong to which base. Toggle in-game with the \"cnbase\" chat command. Attach to the world actor. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/CNBaseOverlay.cs:22` |
| `ref:CN:CNOffsetSpriteSequenceShadowInfo` | Offsets split sprite-sequence shadows independently from their main sprite. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Render/CNOffsetSpriteSequenceShadow.cs:22` |
| `ref:CN:CNTacticalMapOverlayInfo` | Debug overlay that draws the chokepoints (bridges, ramps, passages) found by CNTacticalMapBotModule. Toggle in-game with the \"cntopo\" chat command. Attach to the world actor. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/CNTacticalMapOverlay.cs:21` |
| `ref:CN:CNWindSwayInfo` | Tilts a static sprite back and forth to fake wind movement, without needing animated frames. The sprite is rotated around a pivot at its base, so the crown travels while the trunk stays planted. Purely cosmetic: driven by wall-clock time, never by the simulation. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Render/CNWindSway.cs:19` |
| `ref:CN:CharredPaletteInfo` | Creates a desaturated and darkened copy of a source palette, suitable for charred/burned voxel husks. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Render/CharredPalette.cs:22` |
| `ref:CN:CloudShadowRendererInfo` | Renders procedural cloud shadows as a post-process pass over the terrain. Replaces sprite-based cloud actors. Blends between clear and ion-storm states based on WeatherController.Intensity. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/CloudShadowRenderer.cs:19` |
| `ref:CN:CloudSpawnerInfo` | Spawns/pre-spawns drifting cloud actors or effects using wind, speed, altitude, image and palette parameters. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/CloudSpawner.cs:21` |
| `ref:CN:DamageSmokeInfo` | Emits rising smoke puffs from damaged units and buildings, mimicking Tiberian Sun behavior. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Render/DamageSmoke.cs:16` |
| `ref:CN:DayNightCycleInfo` | Tick-based day/night cycle. Produces the clear-weather base ambient tint for the current time of day. Does NOT write TintPostProcessEffect itself - WeatherTintEffect consumes this base and remains the sole tint writer so the ion-storm tint can layer on top. Purely visual; no gameplay effect. The lobby dropdown picks the day length (or 'Frozen' = static at StartHour). | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/DayNightCycle.cs:23` |
| `ref:CN:ForestCoverSourceInfo` | Registers this actor as a forest cover source with ForestCoverSystem. Replaces ProximityExternalCondition for forest cover — more efficient for large forests. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/ForestCoverSystem.cs:18` |
| `ref:CN:ForestCoverSystemInfo` | Manages the forest-cover condition using a cell influence map. Replaces per-tree ProximityExternalCondition with a single centralized system. Requires ForestCoverSource on tree actors and ExternalCondition on unit actors. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/ForestCoverSystem.cs:53` |
| `ref:CN:IonStormDamageInfo` | Fires a weapon at random map positions or actors during ion storms. All effects (damage, visuals, smudges, sound) are defined by the weapon/warheads. Requires WeatherController on the world actor. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/IonStormDamage.cs:19` |
| `ref:CN:PeriodicSpriteEffectInfo` | Repeatedly spawns a one-shot sprite animation at random intervals. Useful for fire flicker/light simulation, periodic sparks, etc. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Render/PeriodicSpriteEffect.cs:17` |
| `ref:CN:ResourceAnimationOverlayInfo` | Plays sparkle animations randomly over Tiberium resource cells. Attach to the world actor. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/ResourceAnimationOverlay.cs:19` |
| `ref:CN:TerrainAnimationOverlayInfo` | Plays animations randomly over specified terrain type cells. Attach to the world actor. Can be used for water waves, lava glow, etc. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/TerrainAnimationOverlay.cs:19` |
| `ref:CN:TerrainDeformationOptionsInfo` | Exposes terrain deformation as a lobby option. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Warheads/TerrainDeformationWarhead.cs:21` |
| `ref:CN:TerrainTileAmbientSoundInfo` | Plays a looping ambient sound at every cell matching the specified tile types and index. One sound instance is started per matching cell. Use Index: 0 to anchor to one cell per template. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/TerrainTileAmbientSound.cs:19` |
| `ref:CN:TerrainTileOverlayInfo` | Displays a continuously playing sprite overlay at every cell matching the specified tile type and index. Renders as IRenderOverlay (terrain pass) so cloud shadows and post-processing appear on top. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/TerrainTileOverlay.cs:18` |
| `ref:CN:TiberiumGlowRendererInfo` | Fullscreen screenspace glow / fog / particles over Tiberium, gated by a resource-layer coverage texture. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/TiberiumGlowRenderer.cs:25` |
| `ref:CN:VoxelDebrisOnDeathInfo` | Spawns explicitly defined voxel debris sequences as ballistic debris when the actor dies. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/VoxelDebrisOnDeath.cs:24` |
| `ref:CN:VoxelDynamicsInfo` | Provides impact-tilt and acceleration-tilt for voxel units.  Add CNWithVoxelBody/Turret/Barrel instead of their vanilla counterparts. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Render/VoxelDynamics.cs:28` |
| `ref:CN:VoxelShadowSmoothingInfo` | Smooths the ground plane used by projected voxel shadows. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Render/VoxelShadowSmoothing.cs:15` |
| `ref:CN:WaterEffectReflectionRendererInfo` | Clones transient world-effect renderables as subtle water reflections. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/WaterEffectReflectionRenderer.cs:18` |
| `ref:CN:WaterOverlayRendererInfo` | Renders a weather-reactive color overlay on water terrain cells. Blends between a clear-state shimmer and an ion-storm energy pulse based on WeatherController.Intensity. Requires WeatherController on the world actor for storm transitions; falls back to clear appearance if absent. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/WaterOverlayRenderer.cs:22` |
| `ref:CN:WaterReflectionRendererInfo` | Draws a lightweight screenspace reflection over water terrain cells. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/WaterReflectionRenderer.cs:18` |
| `ref:CN:WaterSparkleRendererInfo` | Renders small dissolving sparkles on lit water terrain. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/WaterSparkleRenderer.cs:19` |
| `ref:CN:WeatherControllerInfo` | Controls dynamic ion storm lifecycle (Clear/Warning/Storm/Clearing). Reads a lobby option to enable/disable. Grants a condition to all actors with a matching ExternalCondition during storm phases. Plays a music track during the storm. Use WeatherTintEffect for visual tint. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/WeatherController.cs:30` |
| `ref:CN:WeatherProfileInfo` | Static, match-long weather selected via a lobby dropdown. Grants a matching world condition (distinct from 'ionstorm') so gated WeatherOverlay / cloud traits switch on. Visual only; coexists with the ion-storm system. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/WeatherProfile.cs:29` |
| `ref:CN:WeatherTintEffectInfo` | Sole writer of WorldTintState: takes the DayNightCycle base tint and lerps it toward the ion-storm tint by WeatherController intensity. This path is the per-sprite combined-shader tint, so it respects IgnoreWorldTint (both day/night and the storm skip those sprites). Declare AFTER DayNightCycle so it reads a fresh base each tick. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/WeatherTintEffect.cs:19` |
| `ref:CN:WithWaterReflectionInfo` | Clones the actor renderable as a subtle reflection when above water terrain. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/Render/WithWaterReflection.cs:21` |
| `ref:CN:WorldCloudShadowInfo` | Applies a drifting cloud shadow as part of the world tint (combined shader). Darkens terrain and world-tinted sprites; IgnoreWorldTint geometry is skipped. Blends between clear and ion-storm states via WeatherController.Intensity. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Traits/World/WorldCloudShadow.cs:21` |
| `ref:Generals:ConditionIconOverlayInfo` | Applies icon overlays on actors producible from this actor. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/Render/ConditionIconOverlay.cs:20` |
| `ref:Generals:WithSupplyCollectionOverlayInfo` | Plays a configurable sprite overlay during supply collection. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/Render/WithSupplyCollectionOverlay.cs:19` |
| `ref:Generals:WithSupplyCollectorPipsDecorationInfo` | Displays empty/full pips for a supply collector. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/Render/WithSupplyCollectorPipsDecoration.cs:19` |
| `ref:Generals:WithSupplyDeliveryOverlayInfo` | Plays a configurable sprite overlay during supply delivery. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/Render/WithSupplyDeliveryOverlay.cs:19` |
| `ref:Generals:WithTerrainDependantSpriteBodyInfo` | Renders a different sequence depending on terrain actor is on. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/Render/WithTerrainDependantSpriteBody.cs:19` |
| `ref:RV:ColorAlphaFlashPaletteEffectInfo` | The cloak palette effect used by TA. | `Romanovs-Vengeance/OpenRA.Mods.RA2/PaletteEffects/ColorAlphaFlashPaletteEffect.cs:20` |
| `ref:RV:WithIdleRepairOverlayInfo` | Displays an overlay when the building is being repaired by the player. | `Romanovs-Vengeance/OpenRA.Mods.RA2/Traits/Render/WithIdleRepairOverlay.cs:22` |
| `ref:SP:CloudSpawnerInfo` | Spawns/pre-spawns drifting cloud actors or effects using wind, speed, altitude, image and palette parameters. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/World/CloudSpawner.cs:30` |
| `ref:SP:GradientColorsPaletteInfo` | Add this to the World actor definition. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/Palettes/GradientColorsPalette.cs:21` |
| `ref:SP:LoadPaletteWithLightModifiedAndRBGSwappedInfo` | Palette reprocessed with swapping RPG and lightning effect. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/Palettes/LoadPaletteWithLightModifiedAndRBGSwapped.cs:24` |
| `ref:SP:PaletteFromPaletteWithLightModifiedAndRBGSwappedInfo` | Create a palette by swapping RPG and adjust lightning to another palette. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/Palettes/PaletteFromPaletteWithLightModifiedAndRBGSwapped.cs:23` |
| `ref:SP:SpawnSparksInfo` | Support spark weapons spawning or just simply generates an effect after an interval. Cheapest for Perf. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/SpawnSparks.cs:21` |
| `ref:SP:WeaponWeatherInfo` | Create a map-wide weather based on weapons. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/World/WeaponWeather.cs:22` |
| `ref:SP:WithRandomIdleOverlayInfo` | Renders a decorative animation on units and buildings. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/Render/WithRandomIdleOverlay.cs:23` |
## Support powers and intelligence

| Donor declaration | What it enables (source description) | Source |
|---|---|---|
| `ref:CA-upstream:AirReinforcementsPowerInfo` | Delivers a configured squad by an off-map air support power with camera and beacon controls. | `CAmod/OpenRA.Mods.CA/Traits/SupportPowers/AirReinforcementsPower.cs:23` |
| `ref:CA-upstream:CashHackPowerInfo` | Transfers a bounded percentage of target funds through a support power, with visibility and notification options. | `CAmod/OpenRA.Mods.CA/Traits/SupportPowers/CashHackPower.cs:23` |
| `ref:CA-upstream:ClassicAirstrikePowerInfo` | Schedules configured aircraft squads and repeated strikes through a stackable directional power. | `CAmod/OpenRA.Mods.CA/Traits/SupportPowers/ClassicAirstrikePower.cs:37` |
| `ref:CA-upstream:DummyGpsPowerInfo` | Animates a GPS launch with sounds and a condition, without being the radar-coverage implementation. | `CAmod/OpenRA.Mods.CA/Traits/SupportPowers/DummyGpsPower.cs:17` |
| `ref:CA-upstream:FrozenUnderFogUpdatedByGpsRadarInfo` | Updates frozen actors of actors that change owners, are sold or die whilst having an active GPS power. | `CAmod/OpenRA.Mods.CA/Traits/FrozenUnderFogUpdatedByGpsRadar.cs:21` |
| `ref:CA-upstream:GpsRadarDotInfo` | Show an indicator revealing the actor underneath the fog when a GpsRadarProvider is activated. | `CAmod/OpenRA.Mods.CA/Traits/GPSRadarDot.cs:19` |
| `ref:CA-upstream:GpsRadarProviderInfo` | This actor provides Radar GPS. | `CAmod/OpenRA.Mods.CA/Traits/SupportPowers/GpsRadarProvider.cs:17` |
| `ref:CA-upstream:GpsRadarWatcherInfo` | Required for GPS Radar related logic to function. Attach this to the player actor. | `CAmod/OpenRA.Mods.CA/Traits/GpsRadarWatcher.cs:19` |
| `ref:CA-upstream:GrantPrerequisiteResourceDrainPowerInfo` | Grants a prerequisite while draining the owner's cash and resources each tick. | `CAmod/OpenRA.Mods.CA/Traits/SupportPowers/GrantPrerequisiteResourceDrainPower.cs:20` |
| `ref:CA-upstream:InfiltratePowerInfo` | Acts like infiltrating a targeted structure. | `CAmod/OpenRA.Mods.CA/Traits/SupportPowers/InfiltratePower.cs:22` |
| `ref:CA-upstream:InterceptorPowerInfo` | Launches a configured interceptor squad to guard a target area for a fixed duration and radius. | `CAmod/OpenRA.Mods.CA/Traits/SupportPowers/InterceptorPower.cs:23` |
| `ref:CA-upstream:MeteorPowerInfo` | Delivers a falling meteor impact weapon with flight, trail, camera, beacon and targeting-circle controls. | `CAmod/OpenRA.Mods.CA/Traits/SupportPowers/MeteorPower.cs:24` |
| `ref:CA-upstream:MissileStrikePowerInfo` | Launches missile actors at filtered targets with launch cadence, count, offsets and active-state controls. | `CAmod/OpenRA.Mods.CA/Traits/SupportPowers/MissileStrikePower.cs:43` |
| `ref:CA-upstream:RangedGpsRadarProviderInfo` | This actor provides Radar GPS. | `CAmod/OpenRA.Mods.CA/Traits/RangedGpsRadarProvider.cs:18` |
| `ref:CA-upstream:RemoveOnPowerActivationInfo` | Removes actors when support power is activated. | `CAmod/OpenRA.Mods.CA/Traits/SupportPowers/RemoveOnPowerActivation.cs:18` |
| `ref:CA-upstream:RevealActorsPowerInfo` | Spawns camera actors at the location of specified actor types that stay for a limited amount of time. | `CAmod/OpenRA.Mods.CA/Traits/SupportPowers/RevealActorsPower.cs:23` |
| `ref:CA-upstream:SendCashPowerInfo` | Transfers money to the owner of the targeted actor. | `CAmod/OpenRA.Mods.CA/Traits/SupportPowers/SendCashPower.cs:24` |
| `ref:CA-upstream:StackableSupportPowerManagerInfo` | Tracks independent stack cooldowns for stackable support powers. | `CAmod/OpenRA.Mods.CA/Traits/Player/StackableSupportPowerManager.cs:20` |
| `ref:CA-upstream:SupportPowerInstanceManagerInfo` | For storing global support power properties e.g. to limit the number of times timers are modified. | `CAmod/OpenRA.Mods.CA/Traits/Player/SupportPowerInstanceManager.cs:18` |
| `ref:CA-upstream:UpdatesSupportPowerTimerInfo` | When trait is enabled the named support power will have its timer updated. | `CAmod/OpenRA.Mods.CA/Traits/UpdatesSupportPowerTimer.cs:24` |
| `ref:Generals:CashHackPowerInfo` | Transfers a bounded percentage of target funds through a support power, with visibility and notification options. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/SupportPowers/CashHackPower.cs:24` |
| `ref:Generals:FakePowerInfo` | Fake power that does nothing but play activation voices/sounds when activated. | `Generals-Alpha/OpenRA.Mods.GenSDK/Traits/SupportPowers/FakePower.cs:18` |
| `ref:RV:WithSupportPowerChargedOverlayInfo` | Displays an overlay when 'SupportPower' is fully charged. | `Romanovs-Vengeance/OpenRA.Mods.RA2/Traits/Render/WithSupportPowerChargedOverlay.cs:20` |
| `ref:SP:WithSupportPowerActivationExplodeWeaponInfo` | Trigger an weapon when a support power is triggered. Mainly for visual effect | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Traits/WithSupportPowerActivationExplodeWeapon.cs:21` |
## Warheads and impacts

| Donor declaration | What it enables (source description) | Source |
|---|---|---|
| `ref:CA-upstream:AttachActorWarhead` | This warhead can attach an actor to the target. | `CAmod/OpenRA.Mods.CA/Warheads/AttachActorWarhead.cs:22` |
| `ref:CA-upstream:CreateDistortionHaloWarhead` | Creates a temporary distorted halo of animated arc segments at the impact position. Purely visual, no damage. | `CAmod/OpenRA.Mods.CA/Warheads/CreateDistortionHaloWarhead.cs:20` |
| `ref:CA-upstream:CreateFacingEffectWarhead` | Spawn a sprite with sound. Identical to CreateEffectWarhead except it supports sprites with facings. | `CAmod/OpenRA.Mods.CA/Warheads/CreateFacingEffectWarhead.cs:24` |
| `ref:CA-upstream:HealthPercentageSpreadDamageWarhead` | Apply damage in a specified range. | `CAmod/OpenRA.Mods.CA/Warheads/HealthPercentageSpreadDamageWarhead.cs:20` |
| `ref:CA-upstream:InfiltrateWarhead` | Calls infiltration notifications on a valid actor target or actors near a terrain impact; the class Desc 'Does nothing' is stale. | `CAmod/OpenRA.Mods.CA/Warheads/InfiltrateWarhead.cs:21` |
| `ref:CA-upstream:SpawnMultiWeaponImpactWarhead` | Schedules a weapon's impacts over configured/randomized offsets and intervals, with ownership control. | `CAmod/OpenRA.Mods.CA/Warheads/SpawnMultiWeaponImpactWarhead.cs:18` |
| `ref:CA-upstream:SpawnRandomActorWarhead` | Spawn actors upon explosion. Don't use this with buildings. | `CAmod/OpenRA.Mods.CA/Warheads/SpawnRandomActorWarhead.cs:23` |
| `ref:CA-upstream:WarpPercentDamageWarhead` | Affects warp value on the actors with Warpable trait. | `CAmod/OpenRA.Mods.CA/Warheads/WarpPercentDamageWarhead.cs:20` |
| `ref:CN:ExplodeResourceWarhead` | Detonates a weapon on each resource tile of the specified types within the impact area. Use for explosive tiberium variants (blue, red) that chain-react when hit. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Warheads/ExplodeResourceWarhead.cs:19` |
| `ref:CN:SparkBurstWarhead` | Spawns small ballistic spark particles at the impact position. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Warheads/SparkBurstWarhead.cs:21` |
| `ref:CN:TerrainDeformationWarhead` | Changes terrain height around an impact, simulating Tiberian Sun-style terrain deformation. | `crystallized-nexus/.modsdk/OpenRA.Mods.CN/Warheads/TerrainDeformationWarhead.cs:61` |
| `ref:Generals:CashHackWarhead` | Steal cash from the owner of the target actor. Requires CashHackable trait on the target actor. | `Generals-Alpha/OpenRA.Mods.GenSDK/Warheads/CashHackWarhead.cs:25` |
| `ref:RV:LegacySpreadWarhead` | Warhead used to simulate the Red Alert 2 CellSpread damage delivery model. | `Romanovs-Vengeance/OpenRA.Mods.RA2/Warheads/LegacySpreadWarhead.cs:21` |
| `ref:RV:SpawnBuildingOrWeaponWarhead` | Spawn buildings, or if it can't fires a weapon, upon explosion. | `Romanovs-Vengeance/OpenRA.Mods.RA2/Warheads/SpawnBuildingOrWeaponWarhead.cs:25` |
| `ref:RV:TemporalWarhead` | Deals temporal damage to the actors with AffectedByTemporal trait. | `Romanovs-Vengeance/OpenRA.Mods.RA2/Warheads/TemporalDamageWarhead.cs:21` |
| `ref:SP:InfiltratesWarhead` | Hack: This warhead only used for mission to trigger the lua trigger. It only calls victim's INotifyInfiltrated and doesn't directly affect the one who fire it | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Warheads/InfiltratesWarhead.cs:21` |
| `ref:SP:ScrinEssenceHitWarhead` | Fires secondary projectiles at nearby valid actors, prioritizing non-secondary target types and using shared-RNG shuffling. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Warheads/ScrinEssenceHitWarhead.cs:22` |
| `ref:SP:SpreadDamageWithConditionWarhead` | Apply Damage/Condition in a specified range related with hitshape. | `Shattered-Paradise-SDK/OpenRA.Mods.Sp/Warheads/SpreadDamageWithConditionWarhead.cs:23` |
