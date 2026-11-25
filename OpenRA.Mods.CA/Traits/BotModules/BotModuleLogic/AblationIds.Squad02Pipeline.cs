#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-02 squad pipeline (CONSOLIDATE_02_SQUAD_DESIGN.md 4.3). One yaml master switch (SquadPipeline02Enabled); each rule below it has its own DEV row.
	// Row off = the old behaviour of that rule (the master row off = the whole pipeline off).
	public static partial class AblationIds
	{
		// Master: the army plan runs (Info switch SquadPipeline02Enabled).
		public const string SqPipeline02 = "sq_pipeline_02";

		// Q2 Step 1: one role per unit; the harass cap by VALUE; attached squads follow the seat's main squad.
		public const string SqRoles = "sq_roles";

		// Q3 Step 4: one defend arbiter (P0-P5); ProtectOwn, DEFEND-ALLY, TEAM-DEFEND and the AggTeam detachment only draft what it allows.
		public const string SqDefendArbiter = "sq_defend_arbiter";

		// Q4 Step 2: the team order is the only ground launcher (per-bot timer, AggTeam A5, breaker wave, leader squad issue and Idle-state launch bypassed).
		public const string SqLaunchArbiter = "sq_launch_arbiter";

		// Q4: the order stays open through Push and Engaged (leader-updated target, WAVE-MIN reinforcements) until Done.
		public const string SqOrderEngaged = "sq_order_engaged";
	}
}
