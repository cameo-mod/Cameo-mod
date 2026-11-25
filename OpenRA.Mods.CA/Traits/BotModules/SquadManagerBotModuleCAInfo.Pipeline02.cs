#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-02 squad pipeline (CONSOLIDATE_02_SQUAD_DESIGN.md 4). The master switch defaults to false: @classic and every block that does not set it run the old code.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("CONSOLIDATE-02 master switch (release; the DEV row is sq_pipeline_02, each rule has its own row). On: one army plan per seat (roles, one defend arbiter, one launcher " +
			"with the order phases Gather, Push, Engaged, Done). Off = the old squad code, unchanged.")]
		public readonly bool SquadPipeline02Enabled = false;

		[Desc("CONSOLIDATE-02: the army plan runs at most once per this many ticks per bot (Step 1 roles).")]
		public readonly int ArmyPlanTicks = 100;

		[Desc("CONSOLIDATE-02 Q3 defend arbiter: an own-asset answer (P0/P1) takes units until their value is this percent of the attack the defence there does not cover. Range 100-400.")]
		public readonly int ArbiterDefendMarginPercent = 150;

		[Desc("CONSOLIDATE-02 Q3 defend arbiter: an attacker the bot cannot see is answered by at most this many pool units (nearest first) when no home guard stands yet, instead of the whole pool. Range 0-12.")]
		public readonly int ArbiterBlindDraftUnits = 3;

		[Desc("CONSOLIDATE-02 Q4 order phases: a pushed order is Done at the latest this many ticks after its push (its squads finish their fights, the next order gathers). Range 1500-30000.")]
		public readonly int ArmyEngagedMaxTicks = 6000;

		[Desc("CONSOLIDATE-02 Q4 (Opus D4): a stalemate-breaker signal on the team board feeds the leader's all-in trigger for this many ticks. Range 100-6000.")]
		public readonly int ArmyStalemateSignalTicks = 1500;
	}
}
