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
	}
}
