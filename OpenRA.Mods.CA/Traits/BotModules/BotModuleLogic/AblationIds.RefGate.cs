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
	// REF-GATE-V08F rows (Opus 11-25, REF_PEAK_RCA_01 53f4dbdf). Owner: Sonnet. Each row has its own Info switch; off = the behaviour of V08E.
	public static partial class AblationIds
	{
		// The stuck-park counter counts only replans in which the building queue offered the wanted refinery (a want the stage never reached is not a failed claim).
		public const string RefParkEvaluatedOnly = "ref_park_evaluated_only";

		// A refinery lost under REF-DEMAND is rebuilt after RefineryLostLockTicks unless an enemy combat actor was seen at the lost cell within RefineryLostQuietTicks.
		public const string RefRebuildLost = "ref_rebuild_lost";

		// DEV only: the S2 zone-demand branch records the gate that holds it (bb_refinery_held).
		public const string RefS2HoldLog = "ref_s2_hold_log";

		// DEV only: the situation row's anchor and field counters use the REF-DEMAND verdict.
		public const string RefAnchorFromDemand = "ref_anchor_from_demand";
	}
}
