#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License as
 * published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Mods.CA.Traits
{
	// A10 RETREAT-RALLY (design DRAFT02 section 5). The one flag is set by the switch group CH_retreat_rally (needs CG_agg_team and
	// CF_agg_core); the AggTeamRally* values are named A9 parameters (the registry tests read every AggTeam* int field's Desc range)
	// and are resolved with AggP(name, enemy). None is read while RetreatRallyEnabled is false.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("A10 RETREAT-RALLY master flag (switch CH_retreat_rally). Off = a retreating ground squad takes the legacy flight home exactly as before: no scoring, no rally state, no telemetry row.")]
		public readonly bool RetreatRallyEnabled = false;

		[Desc("A10: a rally destination farther than this from the retreating squad (cells) is not considered. Range 10-200.")]
		public readonly int AggTeamRallyMaxDistanceCells = 60;

		[Desc("A10 score weight of the distance term. The three weights must not all be zero. Range 0-100.")]
		public readonly int AggTeamRallyWeightDistance = 40;

		[Desc("A10 score weight of the strength term. Range 0-100.")]
		public readonly int AggTeamRallyWeightStrength = 35;

		[Desc("A10 score weight of the need term. Range 0-100.")]
		public readonly int AggTeamRallyWeightNeed = 25;

		[Desc("A10: the strength term is full once the destination army is this percent of the retreating squad's value. Range 50-600.")]
		public readonly int AggTeamRallyStrengthTargetPercent = 200;

		[Desc("A10 need points (0-1000) of a destination that is in a fight or has an open defence request on it. Range 0-1000.")]
		public readonly int AggTeamRallyNeedFightPoints = 1000;

		[Desc("A10 need points of an answered ally defence request. Range 0-1000.")]
		public readonly int AggTeamRallyNeedDefendPoints = 800;

		[Desc("A10 need points of a destination that is attacking or marching. Range 0-1000.")]
		public readonly int AggTeamRallyNeedMarchPoints = 600;

		[Desc("A10 need points of an own idle squad (the squad merges into the pool there). Range 0-1000.")]
		public readonly int AggTeamRallyNeedIdlePoints = 100;

		[Desc("A10: score points taken off an allied (not own) destination. Range 0-500.")]
		public readonly int AggTeamRallyAllyPenaltyPoints = 50;

		[Desc("A10: the best destination must reach this score (0-1000) or the squad flees to the base. Range 0-1000.")]
		public readonly int AggTeamRallyMinScore = 450;

		[Desc("A10 guard G1: the retreating squad plus the friends at the destination must reach this ratio percent against the visible enemies there. Range 50-300.")]
		public readonly int AggTeamRallyMinJoinRatioPercent = 100;

		[Desc("A10 guards G1/G2: radius (cells) in which visible armed enemies count as the danger around a squad or a destination. Range 6-40.")]
		public readonly int AggTeamRallyDangerScanRadiusCells = 15;

		[Desc("A10: the squad stops this many cells in front of the destination (arrival radius) and the friends near the destination are counted within it. Range 4-40.")]
		public readonly int AggTeamRallyDestRadiusCells = 12;

		[Desc("A10 guard G2: a destination must lie at least this many cells farther from the pursuers than the retreating squad. Range 0-20.")]
		public readonly int AggTeamRallyAwayGainCells = 2;

		[Desc("A10: a destination squad needs at least this value (cost). Range 0-20000.")]
		public readonly int AggTeamRallyMinSquadValue = 1000;

		[Desc("A10 guard G7: the chosen destination is locked for this many ticks (no switch). Range 0-3000.")]
		public readonly int AggTeamRallyMinCommitTicks = 250;

		[Desc("A10: ticks between two re-scores and order refreshes while rallying. Range 25-1000.")]
		public readonly int AggTeamRallyRescoreTicks = 100;

		[Desc("A10 guard G7: after the lock a new destination replaces the old one only if its score is this percent above. Range 0-200.")]
		public readonly int AggTeamRallySwitchMarginPercent = 25;

		[Desc("A10: a rally that has not arrived after this many ticks ends and the squad flees to the base. Range 100-6000.")]
		public readonly int AggTeamRallyMaxTicks = 900;

		[Desc("A10: a squad supporting an army ends after this many ticks (0 = arrive and end). Range 0-6000.")]
		public readonly int AggTeamRallySupportMaxTicks = 600;

		[Desc("A10 guard G5: after a rally ends neither the squad nor its destination is used for this many ticks. Range 0-6000.")]
		public readonly int AggTeamRallyCooldownTicks = 750;

		[Desc("A10 guard G10: a squad that began a flight within this many ticks is retreating and is not a destination. Range 50-3000.")]
		public readonly int AggTeamRallyRetreatMarkTicks = 400;

		[Desc("A10 guard G4: at most this many rallies per episode (0 = flee to the base only). Range 0-3.")]
		public readonly int AggTeamRallyMaxHops = 1;

		[Desc("A10 guard G4: the hop count of a squad resets this many ticks after its last rally event. Range 100-12000.")]
		public readonly int AggTeamRallyEpisodeResetTicks = 1500;

		[Desc("A10 guard G6: a destination where the squad downgraded or lost is excluded for this many ticks. Range 0-30000.")]
		public readonly int AggTeamRallyBlacklistTicks = 3000;

		[Desc("A10: depth (cells) of the fallback base cell on the enemy-to-own-base axis; positive = on the enemy-facing side of the own base. Range -20-20.")]
		public readonly int AggTeamRallyFallbackDepthCells = 6;

		[Desc("A10: 1 = units of human allies count toward the friends at a destination (their squads are never destinations). Range 0-1.")]
		public readonly int AggTeamRallyHumanAllies = 0;

		/// <summary>A10: the rally parameter named by its yaml key (the keys of RetreatRallyPolicyCA.Ranges).</summary>
		internal int RetreatRallyValue(string key) => key switch
		{
			nameof(AggTeamRallyMaxDistanceCells) => AggTeamRallyMaxDistanceCells,
			nameof(AggTeamRallyWeightDistance) => AggTeamRallyWeightDistance,
			nameof(AggTeamRallyWeightStrength) => AggTeamRallyWeightStrength,
			nameof(AggTeamRallyWeightNeed) => AggTeamRallyWeightNeed,
			nameof(AggTeamRallyStrengthTargetPercent) => AggTeamRallyStrengthTargetPercent,
			nameof(AggTeamRallyNeedFightPoints) => AggTeamRallyNeedFightPoints,
			nameof(AggTeamRallyNeedDefendPoints) => AggTeamRallyNeedDefendPoints,
			nameof(AggTeamRallyNeedMarchPoints) => AggTeamRallyNeedMarchPoints,
			nameof(AggTeamRallyNeedIdlePoints) => AggTeamRallyNeedIdlePoints,
			nameof(AggTeamRallyAllyPenaltyPoints) => AggTeamRallyAllyPenaltyPoints,
			nameof(AggTeamRallyMinScore) => AggTeamRallyMinScore,
			nameof(AggTeamRallyMinJoinRatioPercent) => AggTeamRallyMinJoinRatioPercent,
			nameof(AggTeamRallyDangerScanRadiusCells) => AggTeamRallyDangerScanRadiusCells,
			nameof(AggTeamRallyDestRadiusCells) => AggTeamRallyDestRadiusCells,
			nameof(AggTeamRallyAwayGainCells) => AggTeamRallyAwayGainCells,
			nameof(AggTeamRallyMinSquadValue) => AggTeamRallyMinSquadValue,
			nameof(AggTeamRallyMinCommitTicks) => AggTeamRallyMinCommitTicks,
			nameof(AggTeamRallyRescoreTicks) => AggTeamRallyRescoreTicks,
			nameof(AggTeamRallySwitchMarginPercent) => AggTeamRallySwitchMarginPercent,
			nameof(AggTeamRallyMaxTicks) => AggTeamRallyMaxTicks,
			nameof(AggTeamRallySupportMaxTicks) => AggTeamRallySupportMaxTicks,
			nameof(AggTeamRallyCooldownTicks) => AggTeamRallyCooldownTicks,
			nameof(AggTeamRallyRetreatMarkTicks) => AggTeamRallyRetreatMarkTicks,
			nameof(AggTeamRallyMaxHops) => AggTeamRallyMaxHops,
			nameof(AggTeamRallyEpisodeResetTicks) => AggTeamRallyEpisodeResetTicks,
			nameof(AggTeamRallyBlacklistTicks) => AggTeamRallyBlacklistTicks,
			nameof(AggTeamRallyFallbackDepthCells) => AggTeamRallyFallbackDepthCells,
			nameof(AggTeamRallyHumanAllies) => AggTeamRallyHumanAllies,
			_ => throw new ArgumentOutOfRangeException(nameof(key), key, "not an A10 rally parameter")
		};
	}
}
