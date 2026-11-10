#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	// IMPL02-AGG-TEAM (A4 team coordination, A5 home-reserve cap). Every value is a named yaml parameter; none is read while
	// AggTeamEnabled is false. The switch group CG_agg_team sets AggTeamEnabled and needs CD_stalemate_breaker (the home-threat
	// provider); the personality values live in ai.yaml and are inert while the flag is off.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("IMPL02-AGG-TEAM master flag (switch CG_agg_team). Off = no intent is published, no squad joins, no reserve cap and no push: behaviour identical to before.")]
		public readonly bool AggTeamEnabled = false;

		// ---- A4: attack intents shared through the team blackboard ----
		[Desc("An allied intent older than this (ticks since it was computed) is ignored. One snapshot interval is 150. Range 150-600.")]
		public readonly int AggTeamIntentMaxAgeTicks = 320;

		[Desc("An intent whose ETA passed more than this many ticks ago is over and cannot be joined. Range 0-600.")]
		public readonly int AggTeamIntentGraceTicks = 200;

		[Desc("An idle squad joins an intent only when it stands within this many cells of the intent's rendezvous point. Range 15-90.")]
		public readonly int AggTeamJoinRangeCells = 45;

		[Desc("The rendezvous point is this many cells back from the target along the publisher's approach. Range 6-30.")]
		public readonly int AggTeamRendezvousBackCells = 14;

		[Desc("Travel-time estimate used for ETA and rendezvous ticks (ticks per cell of straight-line distance). Range 8-40.")]
		public readonly int AggTeamTicksPerCell = 18;

		[Desc("A publisher squad needs at least this many members before its target is published as an intent. Range 1-12.")]
		public readonly int AggTeamIntentMinUnits = 3;

		[Desc("An idle squad needs at least this many members to join an intent (tiny squads keep their rally role). Range 1-12.")]
		public readonly int AggTeamJoinMinUnits = 3;

		[Desc("Percent of the joining squad that must stand within AggTeamAssembleRadiusCells of the rendezvous before it starts. Range 30-100.")]
		public readonly int AggTeamAssemblePercent = 70;

		[Desc("Assembly radius at the rendezvous point, in cells. Range 3-12.")]
		public readonly int AggTeamAssembleRadiusCells = 6;

		[Desc("A joiner never waits more than this many ticks past the intent's rendezvous tick before it starts anyway. Range 50-750.")]
		public readonly int AggTeamStartWaitTicks = 250;

		[Desc("After a failed join (target no longer resolvable) the squad does not try again for this many ticks. Range 100-1500.")]
		public readonly int AggTeamJoinCooldownTicks = 400;

		[Desc("A joiner re-resolves the intent's target among visible enemies within this many cells of the intent target. Range 6-24.")]
		public readonly int AggTeamTargetScanCells = 12;

		// ---- A5: per-personality home reserve and the max-strength push ----
		[Desc("At most this percent of the own combat value stays home while there is no home threat; the rest is sent to attack squads. " +
			"Per personality in ai.yaml (steamroller 15, rush 10, tech 25, turtle 40). Range 0-60.")]
		public readonly int AggTeamReserveSharePercent = 25;

		[Desc("The pool above the reserve launches an attack force once the drafted value reaches this (cost). Range 500-6000.")]
		public readonly int AggTeamMinExcessValue = 1500;

		[Desc("How often the reserve cap is evaluated (ticks). Range 25-300.")]
		public readonly int AggTeamCheckIntervalTicks = 75;

		[Desc("Max-strength push (the turtle rule): launch with no reserve once the own combat value reaches this. 0 = no army trigger. Range 0-100000.")]
		public readonly int AggTeamPushArmyValue = 0;

		[Desc("Max-strength push: launch with no reserve from this world tick on. 0 = no time trigger. Range 0-60000.")]
		public readonly int AggTeamPushLateTick = 0;

		[Desc("A push keeps the reserve at zero for this many ticks. Range 200-3000.")]
		public readonly int AggTeamPushHoldTicks = 600;

		[Desc("Minimum ticks between two pushes. Range 1000-9000.")]
		public readonly int AggTeamPushCooldownTicks = 3000;

		// ---- A7: the finishing push (snowball) ----
		[Desc("A7: all-in once own + allied army value reaches this percent of the remembered enemy army value (200 = twice). Range 120-400.")]
		public readonly int AggTeamFinishRatioPercent = 200;

		[Desc("A7: the all-in is held until the ratio falls below this percent (hysteresis; never above AggTeamFinishRatioPercent). Range 100-300.")]
		public readonly int AggTeamFinishExitRatioPercent = 130;

		[Desc("A7: the ratio trigger needs at least this much own + allied army value (cost), so an empty enemy memory is not a win. Range 2000-30000.")]
		public readonly int AggTeamFinishMinArmyValue = 6000;

		[Desc("A7: during the all-in only this percent of the own combat value stays home (a home threat still overrides). Range 0-5.")]
		public readonly int AggTeamFinishMinReservePercent = 3;

		[Desc("A7: an enemy seat with no remembered CY/production counts as finished only once this many of its buildings are known. Range 2-12.")]
		public readonly int AggTeamFinishNoProductionMinKnownBuildings = 4;

		[Desc("A7: during the all-in an idle squad needs at least this many members to be assigned. Range 1-12.")]
		public readonly int AggTeamFinishJoinMinUnits = 1;

		[Desc("A7: during the all-in the weak-seat bonus of target scoring is multiplied by this (retarget the next weakest seat). Range 1-4.")]
		public readonly int AggTeamFinishWeakSeatMultiplier = 2;

		// ---- A8: target value scoring (score = value x class weight / (expected defence + 1) x distance factor) ----
		[Desc("A8: class weight (percent) of a construction yard or production building. Range 100-600.")]
		public readonly int AggTeamTargetKeyWeightPercent = 300;

		[Desc("A8: class weight (percent) of a refinery (economy building). Range 100-400.")]
		public readonly int AggTeamTargetEconomyWeightPercent = 200;

		[Desc("A8: class weight (percent) of any other target. Range 50-200.")]
		public readonly int AggTeamTargetOtherWeightPercent = 100;

		[Desc("A8: expected defence is the value of enemy armed units and structures within this many cells of the target. Range 5-20.")]
		public readonly int AggTeamTargetDefenceRadiusCells = 10;

		[Desc("A8: distance factor is half at this many cells (factor = half / (half + distance)). Range 20-100.")]
		public readonly int AggTeamTargetDistanceHalfCells = 40;

		[Desc("A8: an undefended CY, production or economy building within this many cells is taken before any defended target. Range 30-120.")]
		public readonly int AggTeamTargetReachCells = 60;

		[Desc("A8: percent added to a target's score for the weakest seat (scaled by how much weaker the seat is than the strongest). Range 0-150.")]
		public readonly int AggTeamTargetWeakSeatBonusPercent = 50;

		[Desc("A8: at most this many candidates (nearest first) are scored per pick. Range 16-96.")]
		public readonly int AggTeamTargetMaxCandidates = 48;

		[Desc("A9 strategic form: each living enemy seat weighs its visible army value plus this floor (a seat is never weightless). Range 0-5000.")]
		public readonly int AggTeamEnemyWeightFloorValue = 100;

		[Desc("A3 merge: a quiet Rush squad with fewer units than this is a remnant and is merged back into the pool after AggTeamTinyMergeTicks. Range 0-20.")]
		public readonly int AggTeamTinyMergeMinUnits = 3;

		[Desc("A3 merge: a quiet Rush squad worth less than this is a remnant. Range 0-20000.")]
		public readonly int AggTeamTinyMergeValue = 1000;

		[Desc("A3 merge: ticks a quiet squad must stay a remnant before it is merged into the pool (one squad per check). Range 0-6000.")]
		public readonly int AggTeamTinyMergeTicks = 600;

		// ---- MCV-SAFE-01: keep expansion MCVs out of enemy defence/artillery range (all inert unless AggTeamEnabled) ----
		[Desc("MCV-SAFE: safety margin added to the exclusion radius of every remembered enemy defence or producer. Range 0-20.")]
		public readonly int AggTeamMcvDefenceMarginCells = 4;

		[Desc("MCV-SAFE: artillery reach added to every remembered enemy defence or producer radius (exclusion radius = own weapon range + this + margin). Range 0-30.")]
		public readonly int AggTeamMcvArtilleryCells = 10;

		[Desc("MCV-SAFE: a visible armed enemy mobile unit whose weapon reaches at least this far is artillery and gets its own zone (range + margin). Range 1-30.")]
		public readonly int AggTeamMcvArtilleryMinRangeCells = 9;

		[Desc("MCV-SAFE: exclusion radius around an enemy start location that this seat has not yet explored (lobby data, so public); 0 disables it. Range 0-60.")]
		public readonly int AggTeamMcvSpawnZoneCells = 30;

		[Desc("MCV-SAFE: a map start location within this many cells of an own or allied player's home is held by us and is not a suspected enemy start. Range 1-20.")]
		public readonly int AggTeamMcvSpawnMatchCells = 8;

		[Desc("MCV-SAFE: exclusion radius around the spot where an expansion MCV was lost. Range 0-40.")]
		public readonly int AggTeamMcvLossZoneCells = 16;

		[Desc("MCV-SAFE: how long a loss zone lasts. Range 0-30000. FREEZE10 DIAG-FIX-01: default 18000 (was 6000).")]
		public readonly int AggTeamMcvLossZoneTicks = 18000;

		[Desc("MCV-SAFE: extra path cost of a cell inside a zone when planning the MCV route (a normal cell costs about 100). Range 0-5000.")]
		public readonly int AggTeamMcvPathCostPerCell = 600;

		[Desc("MCV-SAFE: an MCV on the way aborts when it is inside a zone grown by this many cells. Range 0-20.")]
		public readonly int AggTeamMcvAbortMarginCells = 3;

		[Desc("MCV-SAFE: an MCV on the way aborts when a visible armed enemy is this close. Range 1-30. FREEZE10 DIAG-FIX-01: default 14 (was 8).")]
		public readonly int AggTeamMcvAbortThreatCells = 14;

		[Desc("MCV-SAFE: ticks between en-route checks of the expansion MCVs. Range 5-200.")]
		public readonly int AggTeamMcvGuardIntervalTicks = 25;

		[Desc("MCV-SAFE: an MCV that aborted is not aborted again for this long. Range 0-3000.")]
		public readonly int AggTeamMcvAbortCooldownTicks = 300;

		[Desc("MCV-SAFE: a site this close outside an enemy zone edge gets the forward-contain bonus (build radius denial). Range 0-60.")]
		public readonly int AggTeamMcvForwardBandCells = 14;

		[Desc("MCV-SAFE: forward-contain bonus at the zone edge; falls linearly to 0 at the band width. Range 0-300.")]
		public readonly int AggTeamMcvForwardBonusPercent = 40;

		[Desc("MCV-SAFE: a field with at least this percent of the biggest field's resource cells is a full field. Range 0-100.")]
		public readonly int AggTeamMcvFullFieldPercent = 75;

		[Desc("MCV-SAFE: score bonus of a full field. Range 0-300.")]
		public readonly int AggTeamMcvFullFieldBonusPercent = 25;

		[Desc("MCV-SAFE: ticks between rebuilds of the enemy exclusion zones. Range 25-1000.")]
		public readonly int AggTeamMcvScanIntervalTicks = 100;

		[Desc("MCV-SAFE: a zone around a visible enemy artillery unit lasts this many scan intervals (it is rebuilt from what is seen, so it fades once the unit is out of sight). Range 1-20.")]
		public readonly int AggTeamMcvArtilleryZoneScans = 3;

		[Desc("MCV-SAFE: waypoints of the zone-avoiding route issued to the MCV. Range 1-8.")]
		public readonly int AggTeamMcvWaypoints = 4;

		[Desc("MCV-SAFE: 1 = when no safe far field exists an expansion MCV deploys at the main base instead of the engine fallback (which knows nothing about enemy ranges). Range 0-1.")]
		public readonly int AggTeamMcvFallbackToBase = 1;

		[Desc("MCV-SAFE: a vanished MCV with no own yard within this many cells of where it was last seen counts as lost. Range 1-30.")]
		public readonly int AggTeamMcvLossMatchCells = 8;

		[Desc("DEFENCE-DETACH: the detached defence value must reach this percent of the net home threat. Range 100-500.")]
		public readonly int AggTeamDefendMarginPercent = 130;

		[Desc("DEFENCE-DETACH: at most this percent of the own army value is detached for home defence; the rest keeps pushing. Range 0-100.")]
		public readonly int AggTeamMaxDetachSharePercent = 35;

		[Desc("DEFENCE-DETACH: a net home threat of at least this percent of the own army value recalls the whole push (0 disables the recall). Range 0-300.")]
		public readonly int AggTeamRecallSharePercent = 60;

		[Desc("DEFENCE-DETACH: percent of the value of own static defences near home that is subtracted from the observed enemy value. Range 0-100.")]
		public readonly int AggTeamDefenceSharePercent = 50;

		[Desc("DEFENCE-DETACH: a unit within this many cells of the home point stands at home (detached first, before units in the field). Range 5-40.")]
		public readonly int AggTeamDefenceHomeRadiusCells = 20;

		// ---- A9: the layered registry (Opus ruling D1912510): effective = Global x Personality x OwnFaction x Matchup, clamped to the range ----
		[Desc("A9 personality layer: parameter name -> multiplier in permille (1000 = 1.0), applied on top of this instance's value. Absent = 1.0. Each multiplier is clamped to 100-5000.")]
		public readonly Dictionary<string, int> AggMultPersonality = new();

		[Desc("A9 own-faction layer: '<ownfaction>/<parameter>' -> multiplier in permille (faction = Player.Faction.InternalName, lower case). Absent = 1.0.")]
		public readonly Dictionary<string, int> AggMultFaction = new();

		[Desc("A9 matchup layer: '<ownfaction>><enemyfaction>/<parameter>', else '*><enemyfaction>/<parameter>', else '<ownfaction>>*/<parameter>' (first hit wins) -> multiplier in permille. " +
			"The enemy faction is that of the target or engagement seat; without one the layer is skipped. Absent = 1.0.")]
		public readonly Dictionary<string, int> AggMultMatchup = new();
	}
}
