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
	// ARMY-FIRST-02 rows (USER P0 11-23, Opus). Owner: Workhorse. One row per feature; row off = the build without that feature.
	public static partial class AblationIds
	{
		// The whole tier-bot army-first voter instance (ai.yaml ArmyFirstBotModule@af02, AblationRow).
		public const string Af2ArmyFirst = "af2_army_first";

		// Every building type's first copy passes the army-first votes.
		public const string Af2FirstCopy = "af2_first_copy";

		// Extra defences follow the threat-scaled cap instead of the stock votes.
		public const string Af2DefenceCap = "af2_defence_cap";

		// Building repair pauses below the cash floor and resumes above it (BaseRepairBotModule.RepairMinCash).
		public const string Af2RepairCash = "af2_repair_cash";
	}
}
