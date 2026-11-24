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
	// MCV-REBUILD-01 rows (Opus 11-24, V04 RCA: three seats lost their only construction yard and never queued an MCV). Owner: Sonnet.
	public static partial class AblationIds
	{
		// No yard, no MCV and none queued: an MCV starts at once in any queue that can build one, past the share, idle cap, cash floor, UnitIntervals and UnitDelays.
		public const string McvRebuildFirst = "mcv_rebuild_first";

		// A failed external build request (the engine's MCV request among them) frees its slot after a short run of failed sweeps instead of 250.
		public const string RequestSlot = "pb_request_slot";
	}
}
