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
	// BASE-FIX-02 rows (AUDIT_BASE_01, Opus 11-24). One row per feature; row off = the build without that feature.
	public static partial class AblationIds
	{
		// A READY building held for crawl or front/back placement is placed at a base cell after the watchdog ticks (not only refineries).
		public const string ReadyWatchdogAll = "ready_watchdog_all";

		// A type whose placement failed the full budget is banned from the overflow, cycled, each-once and fraction extras until a new base provider or the ban ends.
		public const string NoSpaceBan = "pb_nospace_ban";

		// The expansion-refinery safety test covers every wanting claim and the fraction roll: an unsafe claim no longer blocks the safe ones.
		public const string RefinerySafetyAllClaims = "pb_refinery_safety_all_claims";
	}
}
