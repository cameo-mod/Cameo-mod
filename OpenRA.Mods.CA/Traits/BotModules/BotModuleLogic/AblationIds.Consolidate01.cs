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
	// CONSOLIDATE-01 rows (CONSOLIDATE_01_DESIGN.md aa041141). Each row off = the previous behaviour.
	public static partial class AblationIds
	{
		public const string RecountInFlight = "bb_recount_in_flight"; // off = in-flight buildings are recounted only at bank >= BuildingProductionMinCashRequirement
		public const string BbPipeline01 = "bb_pipeline_01"; // off = the old pick ladder (ChooseBuildingToBuild) and the TickQueue post-pick vetoes
	}
}
