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
	}
}
