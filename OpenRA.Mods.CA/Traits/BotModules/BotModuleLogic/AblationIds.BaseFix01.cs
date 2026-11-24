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
	// BASE-FIX-01 rows (AUDIT_BASE_01, Opus 11-24). One row per feature; row off = the build without that feature.
	public static partial class AblationIds
	{
		// The user's standard opening order (power, barracks, refinery, war factory, refinery, 2nd production, radar, airfield, tech, 3rd production, passive income)
		// outranks the refinery pick, random extras and sustained cash for every faction.
		public const string OpeningStandardOrder = "opening_standard_order";

		// The tech chain and each building once stay above the sustained-cash production pick.
		public const string SustainedKeepsEssentials = "pb_sustained_keeps_essentials";

		// The overflow, scaling, threshold, naval, silo and radar picks are vote-filtered before they are returned.
		public const string PrefilterPicks = "pb_prefilter_picks";

		// Ground defences from the fraction roll wait for a seen threat.
		public const string Af2DefenceNeedGate = "af2_defence_need_gate";

		// The growing power margin is capped.
		public const string PowerMarginCap = "pb_power_margin_cap";

		// The first copy of a defence type counts toward the AF2 cap and RATIO-2X (exempt only while a threat is seen).
		public const string Af2DefenceFirstCopy = "af2_defence_first_copy";

		// Army-first essential buildings are exempt from the building cash floor.
		public const string FloorEssentialsExempt = "pb_floor_essentials_exempt";
	}
}
